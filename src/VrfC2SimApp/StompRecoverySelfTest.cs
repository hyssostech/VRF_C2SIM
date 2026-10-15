namespace VrfC2SimApp;

/// <summary>
/// `--stomp-recovery-selftest [--disabled]` - OFFLINE proof of STP-795's recovery (StompRecovery). No server, no SDK
/// connection: the classification is driven with the exception the 2026-09-14 run logged, and the reconnect loop with
/// fakes. FAIL-FIRST BY CONSTRUCTION: `--disabled` runs the SAME assertions with Vrf:StompRecoveryTries = 0 - today's
/// build, which only logs "Restart recommended" - and MUST fail.
/// </summary>
public static class StompRecoverySelfTest
{
    public static int Run(bool featureEnabled = true)
    {
        int fails = 0;
        void Check(string what, bool cond)
        { Console.WriteLine((cond ? "  [PASS] " : "  [FAIL] ") + what); if (!cond) fails++; }

        int tries = featureEnabled ? StompRecovery.DefaultTries : 0;
        Console.WriteLine(featureEnabled
            ? $"stomp-recovery-selftest: {StompRecovery.SettingKey}={tries} (recovery ON)"
            : $"stomp-recovery-selftest: --disabled - {StompRecovery.SettingKey}=0, TODAY'S BUILD. The recovery assertions " +
              "MUST fail in this arm; that failure is run 20260914T120444Z running blind.");

        // ===== 1. The 2026-09-14 fault, as the SDK delivers it to the Error event =====
        var k = StompRecovery.Classify("XmlException", "Unexpected end of file while parsing Comment has occurred. Line 1, position 769.");
        Check("G7: the truncated-frame XmlException is a NOTIFICATION-STREAM fault", k == StompRecovery.Kind.StreamFault);
        Check("G7: ... and it starts a recovery", StompRecovery.ShouldRecover(k, tries, recoveryRunning: false));
        Check("relay: the SDK's \"Error caught in background thread\" is a stream fault",
              StompRecovery.Classify("C2SIMClientException", "Error caught in background thread") == StompRecovery.Kind.StreamFault);
        Check("connection: an IOException / SocketException is a stream fault",
              StompRecovery.Classify("IOException", "x") == StompRecovery.Kind.StreamFault
              && StompRecovery.Classify("SocketException", "x") == StompRecovery.Kind.StreamFault);
        Check("other: an unrelated error (a handler's own NullReferenceException) is NOT - logged as before",
              StompRecovery.Classify("NullReferenceException", "Object reference not set") == StompRecovery.Kind.Other
              && !StompRecovery.ShouldRecover(StompRecovery.Kind.Other, tries, false));
        Check("burst: a second fault while a recovery runs is ABSORBED (one recovery, not three)",
              !StompRecovery.ShouldRecover(StompRecovery.Kind.StreamFault, tries, recoveryRunning: true));

        // ===== 2. The reconnect loop (the real code, fake I/O) =====
        {
            int connects = 0, disconnects = 0; var waits = new List<double>(); var failed = new List<int>();
            var r = StompRecovery.ReconnectAsync(
                () => { disconnects++; throw new InvalidOperationException("already closed"); },
                () => { connects++; if (connects < 2) throw new IOException("refused"); return Task.CompletedTask; },
                d => { waits.Add(d.TotalSeconds); return Task.CompletedTask; }, tries, (a, _) => failed.Add(a)).GetAwaiter().GetResult();
            Check("loop: a refused first connect, then success -> RECONNECTED on attempt 2",
                  featureEnabled ? r.Ok && r.Attempts == 2 && failed.SequenceEqual(new[] { 1 }) : r.Ok);
            Check("loop: the old connection is closed before each attempt, and a failing close is ignored",
                  disconnects == connects && disconnects >= 1);
            Check("loop: it backs off 2 s then 4 s", waits.Take(2).SequenceEqual(new[] { 2.0, 4.0 }));
        }
        {
            int connects = 0;
            var r = StompRecovery.ReconnectAsync(() => Task.CompletedTask,
                () => { connects++; throw new IOException("server down"); },
                _ => Task.CompletedTask, tries, null).GetAwaiter().GetResult();
            Check($"give up: every attempt fails -> NOT ok after {StompRecovery.DefaultTries} attempts (the service then stops " +
                  $"with exit code {StompRecovery.ExitCode})", !r.Ok && connects == StompRecovery.DefaultTries);
        }
        Check("backoff: 2, 4, 8 s, capped at 30", StompRecovery.BackoffSeconds(1) == 2 && StompRecovery.BackoffSeconds(3) == 8
              && StompRecovery.BackoffSeconds(10) == 30);

        // ===== 3. The sentences =====
        string lost = StompRecovery.LostLine("XmlException", "Unexpected end of file", tries);
        Check("lines: the LOST line says the message is lost, must be re-pushed, and names STP-795 and the setting",
              lost.Contains("LOST") && lost.Contains("PUSHED AGAIN") && lost.Contains("STP-795") && lost.Contains(StompRecovery.SettingKey));
        Check("lines: ... and that blank lines must be REMOVED before the re-push (the same body fails again until STP-1165)",
              lost.Contains("BLANK LINE") && lost.Contains("REMOVED") && lost.Contains("STP-1165"));
        Check("lines: the give-up line names the exit code", StompRecovery.GiveUpLine(3).Contains($"exit code {StompRecovery.ExitCode}"));
        Check("lines: the startup line says ON / OFF", StompRecovery.StartupLine(3).Contains("RECOVERY ON")
              && StompRecovery.StartupLine(0).Contains("RECOVERY OFF"));
        string mk = StompRecovery.ObservationMarking("2026-10-15T12:00:00Z", "2026-10-15T12:00:02Z");
        Check("report: the ObservationReport marking names the loss, the re-push and the blank lines",
              mk.Contains("LOST") && mk.Contains("re-push") && mk.Contains("WITHOUT blank lines"));
        string body = BackendLivenessPolicy.BuildStateChangeReport(mk, "2026-10-15T12:00:02Z", "rpt-1", StompRecovery.ReportName);
        Check("report: it builds as a C2SIM ReportBody under the stream's own name, not the back end's",
              body.Contains(StompRecovery.ReportName) && !body.Contains("VR-Forces back end") && body.Contains("re-push"));
        Check("lines: every sentence is ASCII", new[] { lost, StompRecovery.ReconnectedLine(2, 6), StompRecovery.GiveUpLine(3),
              StompRecovery.AttemptFailedLine(1, 3, "x"), StompRecovery.StartupLine(3), mk }.All(s => s.All(c => c < 128)));

        Console.WriteLine(fails == 0 ? "stomp-recovery-selftest: ALL CHECKS PASSED" : $"stomp-recovery-selftest: {fails} CHECK(S) FAILED");
        return fails == 0 ? 0 : 1;
    }
}
