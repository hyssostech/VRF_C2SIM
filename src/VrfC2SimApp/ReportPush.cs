namespace VrfC2SimApp;

/// <summary>What kind of report is being pushed - it decides whether a failure is retried.</summary>
public enum ReportKind
{
    /// <summary>A TaskStatus (TASKSTRT / TASKCMPLT / TASKABRT). There is exactly ONE of these per
    /// task per outcome, and nothing re-sends it, so a lost one is lost for the run: retried.</summary>
    TaskStatus,

    /// <summary>A position report or bundle. The next poll carries the same information a few
    /// seconds later, so a retry would queue stale fixes behind fresh ones: NOT retried.</summary>
    Position,

    /// <summary>An R-SURFACE-PROXY type-substitution observation. Informational, emitted at
    /// creation time: not retried.</summary>
    Observation,
}

/// <summary>
/// Pushing a report and KNOWING WHETHER IT ARRIVED (B2, 2026-09-14).
///
/// The service used to do `try { await _sdk.PushReportMessage(xml); } catch { log }` - which misses
/// the two ways a push fails:
///   - the server answers with an ERROR: PushMessage returns a C2SIMServerResponse whose Status is
///     OK or ERROR (C2SIMServerResponse.cs:31, IsSuccess at :73) and does NOT throw on ERROR
///     (C2SIMSSDK.cs:505-528 only throws on a client exception), so an ERROR reply was discarded;
///   - the push throws: 129 pushes failed in run G6 with "The response ended prematurely" and
///     nothing counted them - the run log said the reports had been sent.
///
/// This is the pure retry/inspection policy, injected with its transport so it can be tested with
/// no SDK and no server (--report-selftest). It never throws: a transport exception is one failed
/// attempt.
///
/// DELIVERY SEMANTICS: AT-LEAST-ONCE, NOT EXACTLY-ONCE (review finding 5, 2026-09-14). The retry
/// re-sends the SAME report xml - same ReportID, same TimeOfObservation - and a failed attempt does
/// not prove the server rejected the message. The SDK POSTs first and parses the answer afterwards
/// (C2SIMSSDK.cs:501-528), so an answer this side cannot read, or a connection that dies while the
/// response is coming back, is indistinguishable from a message that never arrived; the server may
/// already hold it. A TaskStatus can therefore reach the bus TWICE under the SAME ReportID. That is
/// the deliberate trade: there is one TaskStatus per task per outcome and nothing else re-sends it,
/// so a report silently dropped is lost for the run, whereas a duplicate is idempotent for any
/// consumer that de-duplicates on ReportID (which the identical id makes possible) and at worst
/// repeats a status STP already has. TWO CONSEQUENCES TO KNOW: it breaks the "0 duplicate ReportID"
/// property the G6 capture analysis relies on (REPORTING_ASSESSMENT_2026-09-14.md sec 7), and STP's
/// behaviour on a duplicate ReportID is NOT in the record - it is question 6 of B10
/// (DRAFT_STP_QUESTIONS_2026-09-14.md). Position and Observation pushes are never retried, so they
/// are unaffected.
/// </summary>
public static class ReportPush
{
    /// <summary>Outcome of one report push.</summary>
    /// <param name="Ok">The server accepted it.</param>
    /// <param name="Attempts">How many times the transport was called (1 = first try succeeded).</param>
    /// <param name="LastMessage">The server's message, or the exception text, of the last attempt.</param>
    public readonly record struct PushOutcome(bool Ok, int Attempts, string LastMessage);

    /// <summary>What the transport reports back: the server's OK/ERROR and its message.</summary>
    public readonly record struct PushResult(bool Ok, string Message);

    /// <summary>What <see cref="Interpret"/> reports when the server sent no body at all.</summary>
    public const string EmptyBodyMessage =
        "the server returned an EMPTY body - no <result>/<status>, so delivery is UNCONFIRMED";

    /// <summary>
    /// Turn the SDK's answer into a push result (review finding 6, 2026-09-14).
    ///
    /// WHAT AN EMPTY BODY IS. C2SIMSDK.ToC2SIMObject returns default(T) - a NULL response object -
    /// only when the xml it was given is null or whitespace (C2SIMSSDK.cs:807-811), i.e. when the
    /// server's body was EMPTY. That is reachable: C2SIMClientRESTLib.SendTrans returns
    /// resp.Content.ReadAsStringAsync() with NO status-code check at all
    /// (C2SIMClientRestLib.cs:377-401), so an HTTP 4xx/5xx with an empty body - or a 200 with one -
    /// comes back as "", survives BmlRequest untouched (GetElementValue returns "" for empty xml,
    /// :344-350; the "&lt;status&gt;Error&lt;/status&gt;" test does not match), and reaches
    /// PushMessage's ToC2SIMObject as "" (C2SIMSSDK.cs:508-510). An empty body is the server saying
    /// NOTHING, which is not the same as the server saying OK.
    ///
    /// WHAT THE G6 SYMPTOM WAS, MEASURED. "The response ended prematurely" is NOT this case: in
    /// runs/20260914T002716Z_run/vrfc2simapp.log it arrives as
    /// System.Net.Http.HttpRequestException "An error occurred while sending the request", inner
    /// System.Net.Http.HttpIOException "The response ended prematurely. (ResponseEnded)", thrown out
    /// of _httpClient.SendAsync at C2SIMClientRestLib.cs:389 and rethrown by SendTrans's
    /// catch(HttpRequestException) as a C2SIMClientException. It is an EXCEPTION, which SendAsync
    /// below already counts as a failed attempt and retries.
    ///
    /// THE RULE. A TaskStatus is counted FAILED on an empty body - it is retried, and if the server
    /// really did accept the first copy the duplicate is the at-least-once trade documented above.
    /// A Position or Observation is NOT: it is never retried, the next poll carries the same
    /// information, and turning every empty body into a LOUD lost-report line would bury the log at
    /// one line per unit per cycle for a condition we cannot act on.
    /// </summary>
    public static PushResult Interpret(bool responseAbsent, bool serverOk, string message, ReportKind kind)
        => responseAbsent
            ? new PushResult(kind != ReportKind.TaskStatus, EmptyBodyMessage)
            : new PushResult(serverOk, message ?? "");

    /// <summary>1 s, 2 s, 4 s ... - doubling from <paramref name="baseMs"/>, capped at 30 s so a
    /// pathological setting cannot park a report for minutes.</summary>
    public static TimeSpan BackoffFor(int attempt, int baseMs = 1000)
    {
        double ms = Math.Max(1, baseMs) * Math.Pow(2, Math.Max(0, attempt - 1));
        return TimeSpan.FromMilliseconds(Math.Min(ms, 30000));
    }

    /// <summary>
    /// Push one report, inspecting the server's answer and retrying up to <paramref name="maxTries"/>
    /// times (1 = no retry). Returns how it went; the caller does the counting and the loud line.
    /// </summary>
    /// <param name="transport">Sends the xml and reports the server's answer. May throw.</param>
    /// <param name="delayFor">Backoff before attempt n+1 (n is 1-based).</param>
    /// <param name="sleep">How to wait - injected so a test runs instantly.</param>
    /// <param name="onAttemptFailed">Called with a one-line reason after each failed attempt that
    /// will be retried.</param>
    public static async Task<PushOutcome> SendAsync(
        Func<string, Task<PushResult>> transport,
        string xml,
        int maxTries,
        Func<int, TimeSpan> delayFor,
        Func<TimeSpan, Task> sleep,
        Action<string> onAttemptFailed,
        CancellationToken ct = default)
    {
        int tries = Math.Max(1, maxTries);
        string last = "";
        for (int attempt = 1; attempt <= tries; attempt++)
        {
            try
            {
                var r = await transport(xml);
                if (r.Ok) return new PushOutcome(true, attempt, r.Message ?? "");
                last = $"server answered ERROR: {r.Message}";
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return new PushOutcome(false, attempt, "cancelled (service stopping)");
            }
            catch (Exception e)
            {
                last = $"push threw: {C2SIM.C2SIMSDK.GetRootException(e).Message}";
            }
            if (attempt >= tries) break;
            var wait = delayFor(attempt);
            onAttemptFailed?.Invoke($"attempt {attempt} of {tries} failed ({last}); retrying in {wait.TotalSeconds:F0} s");
            try { await sleep(wait); }
            catch (OperationCanceledException) { return new PushOutcome(false, attempt, "cancelled (service stopping)"); }
        }
        return new PushOutcome(false, tries, last);
    }
}
