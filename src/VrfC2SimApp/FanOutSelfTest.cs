namespace VrfC2SimApp;

/// <summary>
/// Offline check of FanOutTracker (R10 subordinate fan-out member-completion
/// aggregation; no bridge, no MAK): `VrfC2SimApp --fanout-selftest`.
/// </summary>
public static class FanOutSelfTest
{
    public static int Run()
    {
        int failures = 0;
        var t = new FanOutTracker();

        // 1. Unknown member -> false (normal completions flow through the caller's path).
        Check(ref failures, !t.TryCompleteMember("M1A2 1", out _, out _, out _, out _),
              "unknown member is not a fan-out completion");

        // 2. Register 3 members; complete one at a time; only the LAST flags allDone.
        Check(ref failures, t.Register("B/40", "task-1", new[] { "M1A2 1", "M1A2 2", "M1A2 3" }) == 3,
              "register returns the member count");
        Check(ref failures,
              t.TryCompleteMember("M1A2 2", out var u1, out var tk1, out int r1, out bool d1)
              && u1 == "B/40" && tk1 == "task-1" && r1 == 2 && !d1,
              "first member completion: unit/task attributed, 2 remaining, not done");
        Check(ref failures,
              t.TryCompleteMember("M1A2 1", out _, out _, out int r2, out bool d2) && r2 == 1 && !d2,
              "second member completion: 1 remaining, not done");
        Check(ref failures,
              t.TryCompleteMember("M1A2 3", out var u3, out _, out int r3, out bool d3)
              && u3 == "B/40" && r3 == 0 && d3,
              "last member completion: allDone");

        // 3. After allDone the fan-out is gone: repeats and stragglers are NOT members.
        Check(ref failures, !t.TryCompleteMember("M1A2 3", out _, out _, out _, out _),
              "repeat completion after allDone -> not a fan-out member");

        // 4. Duplicate completion of the SAME member mid-flight is a no-op (false).
        t.Register("B/40", "task-2", new[] { "M1A2 1", "M1A2 2" });
        Check(ref failures, t.TryCompleteMember("M1A2 1", out _, out _, out _, out _),
              "member completes once");
        Check(ref failures, !t.TryCompleteMember("M1A2 1", out _, out _, out _, out _),
              "the same member completing twice is a no-op");

        // 5. Re-registering the unit REPLACES the active fan-out (retask supersedes):
        //    the old pending member no longer completes; the new set does.
        t.Register("B/40", "task-3", new[] { "M1A2 5" });
        Check(ref failures, !t.TryCompleteMember("M1A2 2", out _, out _, out _, out _),
              "old fan-out's member is cancelled by re-register");
        Check(ref failures,
              t.TryCompleteMember("M1A2 5", out _, out var tk5, out _, out bool d5) && tk5 == "task-3" && d5,
              "new fan-out completes under the new task uuid");

        // 6. Two units track independently.
        t.Register("B/40", "task-4", new[] { "A1", "A2" });
        t.Register("3/7159", "task-5", new[] { "B1", "B2" });
        Check(ref failures,
              t.TryCompleteMember("B1", out var ub, out _, out _, out _) && ub == "3/7159",
              "member attributes to ITS unit");
        Check(ref failures,
              t.TryCompleteMember("A1", out var ua, out _, out int ra, out _) && ua == "B/40" && ra == 1,
              "the other unit's fan-out is unaffected");

        // 7. Cancel drops the fan-out; its members stop completing.
        Check(ref failures, t.Cancel("B/40"), "cancel returns true for an active fan-out");
        Check(ref failures, !t.TryCompleteMember("A2", out _, out _, out _, out _),
              "cancelled fan-out's member no longer completes");
        Check(ref failures, !t.Cancel("B/40"), "cancel is false when nothing is active");

        // 8. Empty/blank member lists register nothing.
        Check(ref failures, t.Register("X", "task-6", Array.Empty<string>()) == 0,
              "empty member list registers 0");
        Check(ref failures, t.Register("X", "task-7", new[] { "", "" }) == 0,
              "blank member names register 0");

        // ---- Step 2: completion QUORUM + straggler TIMEOUT + late-straggler SWALLOW ----

        // 9. QUORUM at 3/4 (fraction 0.75): ceil(4*0.75)=3, so the 3rd of 4 members synthesizes
        //    (allDone). The record LIVES (Synthesized) with 1 member still pending.
        Check(ref failures, t.Register("Q/1", "task-q", new[] { "Q1", "Q2", "Q3", "Q4" }, 0.75) == 4,
              "quorum 0.75: register 4 members");
        Check(ref failures,
              t.TryCompleteMember("Q1", out _, out _, out int qr1, out bool qd1, out bool qs1)
              && qr1 == 3 && !qd1 && !qs1,
              "quorum 0.75: 1st of 4 - 3 remaining, not done, not a straggler");
        Check(ref failures,
              t.TryCompleteMember("Q2", out _, out _, out int qr2, out bool qd2, out bool qs2)
              && qr2 == 2 && !qd2 && !qs2,
              "quorum 0.75: 2nd of 4 - 2 remaining, not done");
        Check(ref failures,
              t.TryCompleteMember("Q3", out var qu3, out var qtk3, out int qr3, out bool qd3, out bool qs3)
              && qu3 == "Q/1" && qtk3 == "task-q" && qr3 == 1 && qd3 && !qs3,
              "quorum 0.75: 3rd of 4 MEETS the quorum - allDone, 1 still pending, not yet swallowed");

        // 10. LATE-STRAGGLER SWALLOW: the 4th completion after the quorum returns the swallow
        //     result (alreadySynthesized) and does NOT flag allDone - the caller must not re-report.
        Check(ref failures,
              t.TryCompleteMember("Q4", out var qu4, out _, out _, out bool qd4, out bool qs4)
              && qu4 == "Q/1" && qs4 && !qd4,
              "quorum 0.75: 4th (late straggler) is SWALLOWED - alreadySynthesized true, allDone false");
        Check(ref failures, !t.TryCompleteMember("Q4", out _, out _, out _, out _, out _),
              "after the last straggler drains, the record is gone (repeat -> not a member)");

        // 11. SUPERSESSION WHILE SYNTHESIZED: a new Register for the unit clears the Synthesized
        //     record; the OLD straggler then no-ops; the new fan-out completes on its own uuid.
        t.Register("S/1", "task-s1", new[] { "S1", "S2", "S3", "S4" }, 0.75);
        t.TryCompleteMember("S1", out _, out _, out _, out _, out _);
        t.TryCompleteMember("S2", out _, out _, out _, out _, out _);
        Check(ref failures,
              t.TryCompleteMember("S3", out _, out _, out _, out bool sd3, out bool ss3) && sd3 && !ss3,
              "supersession setup: quorum synthesized on the 3rd (S4 left pending, record Synthesized)");
        t.Register("S/1", "task-s2", new[] { "S5" }, 0.75);   // supersedes; clears the Synthesized record
        Check(ref failures, !t.TryCompleteMember("S4", out _, out _, out _, out _, out _),
              "supersession clears the Synthesized record: the stale straggler S4 no-ops");
        Check(ref failures,
              t.TryCompleteMember("S5", out _, out var stk5, out _, out bool sd5, out _)
              && stk5 == "task-s2" && sd5,
              "the superseding fan-out completes normally under its own task uuid");

        // 12. FRACTION 1.0 == LEGACY (regression guard): synthesize ONLY on the LAST member;
        //     no early synthesis; ceil(Total*1.0) == Total; no lingering Synthesized state.
        t.Register("L/1", "task-l", new[] { "L1", "L2", "L3" }, 1.0);
        Check(ref failures,
              t.TryCompleteMember("L1", out _, out _, out int lr1, out bool ld1, out bool ls1)
              && lr1 == 2 && !ld1 && !ls1,
              "fraction 1.0: 1st of 3 - not done (NO early synthesis)");
        Check(ref failures,
              t.TryCompleteMember("L2", out _, out _, out int lr2, out bool ld2, out bool ls2)
              && lr2 == 1 && !ld2 && !ls2,
              "fraction 1.0: 2nd of 3 - not done");
        Check(ref failures,
              t.TryCompleteMember("L3", out _, out _, out int lr3, out bool ld3, out bool ls3)
              && lr3 == 0 && ld3 && !ls3,
              "fraction 1.0: LAST member synthesizes (legacy behavior, NOT a straggler swallow)");
        Check(ref failures, !t.TryCompleteMember("L3", out _, out _, out _, out _, out _),
              "fraction 1.0: record removed on the last member (no lingering Synthesized state)");

        // 13. TIMEOUT synthesis: TrySynthesizeByTimeout fires once (idempotent) with counts,
        //     then swallows the remaining members.
        t.Register("T/1", "task-t", new[] { "T1", "T2", "T3", "T4" }, 1.0);
        t.TryCompleteMember("T1", out _, out _, out _, out _, out _);   // 1/4 done; quorum (1.0) not met
        Check(ref failures,
              t.TrySynthesizeByTimeout("T/1", "task-t", out int tc, out int tt) && tc == 1 && tt == 4,
              "timeout synthesizes a stuck fan-out (1/4 done) and reports completed/total");
        Check(ref failures, !t.TrySynthesizeByTimeout("T/1", "task-t", out _, out _),
              "timeout is idempotent: a second call after synthesis returns false");
        Check(ref failures,
              t.TryCompleteMember("T2", out _, out _, out _, out bool td2, out bool ts2) && ts2 && !td2,
              "after a timeout synthesis, a member completion is SWALLOWED (alreadySynthesized)");

        // 14. TIMEOUT uuid mismatch (supersession replaced the fan-out) -> false; the CURRENT
        //     uuid fires.
        t.Register("U/1", "task-u1", new[] { "U1", "U2" }, 1.0);
        t.Register("U/1", "task-u2", new[] { "U3", "U4" }, 1.0);   // supersedes; new uuid
        Check(ref failures, !t.TrySynthesizeByTimeout("U/1", "task-u1", out _, out _),
              "timeout no-ops on the SUPERSEDED uuid (the load-bearing uuid guard)");
        Check(ref failures,
              t.TrySynthesizeByTimeout("U/1", "task-u2", out int uc, out int ut) && uc == 0 && ut == 2,
              "timeout fires for the CURRENT fan-out uuid (0/2 done)");

        // 15. TIMEOUT after ALL members completed -> false (record removed; the timer no-ops).
        t.Register("V/1", "task-v", new[] { "V1", "V2" }, 1.0);
        t.TryCompleteMember("V1", out _, out _, out _, out _, out _);
        t.TryCompleteMember("V2", out _, out _, out _, out _, out _);   // last member -> record removed
        Check(ref failures, !t.TrySynthesizeByTimeout("V/1", "task-v", out _, out _),
              "timeout no-ops after all members already completed (record gone)");

        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
