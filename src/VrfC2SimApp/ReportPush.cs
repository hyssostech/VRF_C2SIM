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
