using System.Globalization;

namespace VrfC2SimApp;

/// <summary>
/// STP-795: RECOVER THE C2SIM NOTIFICATION STREAM INSTEAD OF RUNNING BLIND. PURE - every decision and every sentence is
/// here, so `--stomp-recovery-selftest` drives the real rule.
///
/// WHY. Run 20260914T120444Z (G7 attempt 1): a pushed order carried a blank line inside a multi-line XML comment. A
/// blank line is the STOMP frame terminator, and the SDK's frame reader (C2SIMClientLib/C2SIMClientSTOMPLib.cs, the
/// body loop after the headers) stops reading a body at the first blank line instead of honouring content-length, so
/// it handed XElement.Parse a truncated body (XmlException "Unexpected end of file while parsing Comment") and the
/// rest of that body was left in the stream to be read as the NEXT frame. The SDK pump survives the throw (it raises
/// its Error event and continues - C2SIMSSDK.cs, the catch in Connect's pump), but the order is LOST and the stream's
/// framing is suspect. The interface logged "C2SIM error: ... Restart recommended" and ran its whole window with no
/// order dispatched and nothing anywhere else saying so.
///
/// THE RULE. An SDK Error that is a NOTIFICATION-STREAM fault (<see cref="Classify"/>: a malformed / truncated frame,
/// a frame error relayed from the reader thread, or a broken connection) starts ONE recovery: a loud line saying an
/// inbound C2SIM message was LOST, then up to <see cref="DefaultTries"/> reconnects of the notification connection
/// (Disconnect, then Connect - a new socket starts on a clean frame boundary), backing off 2 / 4 / 8 s; on success one
/// ObservationReport to the server naming the loss (the operator / STP must re-push the message - STOMP keeps no
/// history, and re-reading the shared initialization automatically could re-create units mid-run); on the last
/// failure the interface STOPS with exit code <see cref="ExitCode"/>, so a runner or an operator sees it. Faults
/// that arrive during a recovery are absorbed by it. Anything else on the Error event is logged exactly as before.
/// Vrf:StompRecoveryTries = 0 turns recovery OFF (today's behaviour; the fail-first arm of the selftest).
/// The SDK-side fix (read the body by content-length / the NUL terminator) belongs in the shared C2SIMSDK library and
/// is a separate ticket.
/// </summary>
public static class StompRecovery
{
    public const string SettingKey = "Vrf:StompRecoveryTries";
    public const int DefaultTries = 3;
    public const int ExitCode = 7;
    public const string Prefix = "C2SIM NOTIFICATION STREAM";
    public const string ReportName = "C2SIM notification stream";

    public enum Kind { Other = 0, StreamFault = 1 }

    /// <summary>A notification-stream fault, from the ROOT exception the SDK hands its Error event. XmlException = a
    /// malformed or truncated frame body; the SDK's C2SIMClientException "Error caught in background thread" = a frame
    /// error relayed from the reader; IO / socket / disposed-stream = the connection itself.</summary>
    public static Kind Classify(string rootTypeName, string message)
    {
        string t = rootTypeName ?? "", m = message ?? "";
        if (t == "XmlException") return Kind.StreamFault;
        if (t is "IOException" or "SocketException" or "ObjectDisposedException" or "EndOfStreamException")
            return Kind.StreamFault;
        if (t == "C2SIMClientException" && (m.Contains("background thread", StringComparison.OrdinalIgnoreCase)
                                            || m.Contains("STOMP", StringComparison.OrdinalIgnoreCase)))
            return Kind.StreamFault;
        return Kind.Other;
    }

    /// <summary>Recover? Only a stream fault, with recovery on, and not while one is already running.</summary>
    public static bool ShouldRecover(Kind kind, int tries, bool recoveryRunning)
        => kind == Kind.StreamFault && tries > 0 && !recoveryRunning;

    /// <summary>Wall seconds to wait before reconnect attempt k (1-based): 2, 4, 8, ... capped at 30.</summary>
    public static double BackoffSeconds(int attempt) => Math.Min(30.0, Math.Pow(2, Math.Max(1, attempt)));

    /// <summary>
    /// The reconnect loop the service runs, with its I/O injected so the selftest drives THIS code: before attempt k,
    /// wait BackoffSeconds(k); close the old connection (a failure to close is ignored - it may already be dead); open
    /// a new one. Returns at the first success, or after <paramref name="tries"/> failures.
    /// </summary>
    public static async Task<(bool Ok, int Attempts)> ReconnectAsync(Func<Task> disconnect, Func<Task> connect,
        Func<TimeSpan, Task> delay, int tries, Action<int, string> onAttemptFailed)
    {
        for (int a = 1; a <= tries; a++)
        {
            await delay(TimeSpan.FromSeconds(BackoffSeconds(a)));
            try
            {
                try { await disconnect(); } catch { /* the old connection may already be gone */ }
                await connect();
                return (true, a);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) { onAttemptFailed?.Invoke(a, e.Message); }
        }
        return (false, tries);
    }

    private static string S(double v) => v.ToString("F0", CultureInfo.InvariantCulture);

    public static string LostLine(string rootTypeName, string message, int tries)
        => $"{Prefix}: an inbound C2SIM message could NOT BE READ and is LOST ({rootTypeName}: {message}). The STOMP frame " +
           $"framing is suspect (a blank line inside a message body ends the SDK's frame read - STP-795), so the " +
           $"notification connection is re-opened now (up to {tries} attempt(s), {SettingKey}). Whatever was pushed (an " +
           "initialization or an order) must be PUSHED AGAIN - STOMP keeps no history - and with every BLANK LINE inside " +
           "it REMOVED first: the same body fails the same way until the SDK reader is fixed (STP-1165).";

    public static string ReconnectedLine(int attempt, double wallSeconds)
        => $"{Prefix}: RECONNECTED on attempt {attempt} after {S(wallSeconds)} WALL s - new messages are received again; " +
           "the lost one is NOT recovered (re-push it).";

    public static string AttemptFailedLine(int attempt, int tries, string why)
        => $"{Prefix}: reconnect attempt {attempt} of {tries} FAILED ({why}).";

    public static string GiveUpLine(int tries)
        => $"{Prefix}: {tries} reconnect attempt(s) FAILED - the interface cannot receive C2SIM messages and STOPS with " +
           $"exit code {ExitCode} rather than run blind.";

    /// <summary>The ObservationReport marking sent once after a successful reconnect.</summary>
    public static string ObservationMarking(string isoLost, string isoBack)
        => $"C2SIM message LOST at {isoLost} (unreadable frame); notification stream re-opened at {isoBack} - re-push " +
           "the last initialization / order WITHOUT blank lines inside it (STP-1165)";

    public static string StartupLine(int tries)
        => tries > 0
            ? $"{Prefix} RECOVERY ON ({SettingKey}={tries}): an unreadable inbound message is reported LOST, the notification " +
              $"connection is re-opened (backoff 2/4/8 s) and one ObservationReport names the loss; after {tries} failed " +
              $"reconnect(s) the interface stops with exit code {ExitCode} (STP-795)."
            : $"{Prefix} RECOVERY OFF ({SettingKey}=0): an unreadable inbound message is only logged (\"Restart recommended\").";
}
