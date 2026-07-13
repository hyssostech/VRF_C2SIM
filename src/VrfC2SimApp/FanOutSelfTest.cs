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

        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
