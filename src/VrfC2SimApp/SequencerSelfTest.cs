using System.Diagnostics;

namespace VrfC2SimApp;

/// <summary>
/// Offline check of TaskSequencer + InFlightTracker (no bridge, no MAK, no VR-Forces):
/// `VrfC2SimApp --sequencer-selftest`. Exercises the gate behaviors that replace
/// executeTask's busy-waits - predecessor gating, completer-first race, start delay,
/// predecessor timeout - plus the P0 orchestration fixes: the dispatch-relative
/// completion window, fast-fail on an abandoned predecessor (P0.2), and per-unit
/// completion attribution with supersession (P0.1).
/// </summary>
public static class SequencerSelfTest
{
    public static int Run()
    {
        int failures = 0;

        // 1. Predecessor gate: a task waiting on "A" must NOT proceed until A completes.
        {
            var seq = new TaskSequencer();
            var wait = seq.WaitForStartAsync("A", 0, 0, TimeSpan.FromSeconds(5), CancellationToken.None);
            Thread.Sleep(150);
            Check(ref failures, !wait.IsCompleted, "waits while predecessor pending");
            seq.CompleteTask("A");
            var ok = wait.Wait(TimeSpan.FromSeconds(2));
            Check(ref failures, ok && wait.Result == GateResult.Proceed, "proceeds once predecessor completes");
        }

        // 2. Completer-first race: predecessor completed BEFORE the wait starts -> proceed fast.
        {
            var seq = new TaskSequencer();
            seq.CompleteTask("B");
            var sw = Stopwatch.StartNew();
            var r = seq.WaitForStartAsync("B", 0, 0, TimeSpan.FromSeconds(5), CancellationToken.None)
                       .GetAwaiter().GetResult();
            sw.Stop();
            Check(ref failures, r == GateResult.Proceed && sw.ElapsedMilliseconds < 500,
                  $"already-complete predecessor proceeds immediately ({sw.ElapsedMilliseconds} ms)");
        }

        // 3. Start delay: no predecessor, ~200 ms delay -> proceeds after roughly that long.
        {
            var seq = new TaskSequencer();
            var sw = Stopwatch.StartNew();
            var r = seq.WaitForStartAsync("", 200, 0, TimeSpan.FromSeconds(5), CancellationToken.None)
                       .GetAwaiter().GetResult();
            sw.Stop();
            Check(ref failures, r == GateResult.Proceed && sw.ElapsedMilliseconds >= 180,
                  $"start delay elapses before proceeding ({sw.ElapsedMilliseconds} ms >= ~200)");
        }

        // 4. Predecessor timeout: a predecessor that never completes -> PredecessorTimeout.
        {
            var seq = new TaskSequencer();
            var sw = Stopwatch.StartNew();
            var r = seq.WaitForStartAsync("never", 0, 0, TimeSpan.FromMilliseconds(200), CancellationToken.None)
                       .GetAwaiter().GetResult();
            sw.Stop();
            Check(ref failures, r == GateResult.PredecessorTimeout && sw.ElapsedMilliseconds >= 180,
                  $"predecessor timeout fires (not infinite wait) ({sw.ElapsedMilliseconds} ms)");
        }

        // 5. P0.2 dispatch-relative window: the predecessor-COMPLETION clock restarts at
        //    the predecessor's DISPATCH, not at order arrival (the old arrival-relative
        //    clock made all gated tasks time out together -> retask burst, DEFECT B).
        {
            var seq = new TaskSequencer();
            var wait = seq.WaitForStartAsync("P", 0, 0, TimeSpan.FromMilliseconds(600), CancellationToken.None);
            Thread.Sleep(400);            // most of the arrival-relative window elapses undispatched
            seq.NotifyDispatched("P");    // the completion window (re)starts HERE
            Thread.Sleep(400);            // t ~800 ms: the old arrival-relative clock expired at 600
            Check(ref failures, !wait.IsCompleted,
                  "completion window restarts at predecessor DISPATCH (no arrival-relative timeout)");
            seq.CompleteTask("P");
            var ok = wait.Wait(TimeSpan.FromSeconds(2));
            Check(ref failures, ok && wait.Result == GateResult.Proceed,
                  "proceeds when predecessor completes inside the dispatch-relative window");
        }

        // 6. P0.2 abandoned predecessor fails FAST (no clock run-out): a skipped/abandoned
        //    predecessor releases its waiters immediately with PredecessorAbandoned.
        {
            var seq = new TaskSequencer();
            var sw = Stopwatch.StartNew();
            var wait = seq.WaitForStartAsync("Q", 0, 0, TimeSpan.FromSeconds(5), CancellationToken.None);
            Thread.Sleep(100);
            seq.NotifyAbandoned("Q");
            var ok = wait.Wait(TimeSpan.FromSeconds(2));
            sw.Stop();
            Check(ref failures, ok && wait.Result == GateResult.PredecessorAbandoned && sw.ElapsedMilliseconds < 2000,
                  $"abandoned predecessor fails fast ({sw.ElapsedMilliseconds} ms, not the 5 s window)");
        }

        // 7. P0.1 completion attribution: dispatch A then B to the SAME unit; ONE completion
        //    arrives -> it attributes to B (the in-flight task), NOT A; A's gate stays closed.
        {
            var seq = new TaskSequencer();
            var tracker = new InFlightTracker();
            var now = DateTime.UtcNow;
            var sup0 = tracker.RecordDispatch("1-1 AR", new InFlightTracker.InFlight("A", "task A", "move-along", now));
            var supA = tracker.RecordDispatch("1-1 AR", new InFlightTracker.InFlight("B", "task B", "move-along", now));
            Check(ref failures, sup0 == null && supA?.TaskUuid == "A",
                  "second dispatch supersedes the first (A superseded by B)");

            var waitOnA = seq.WaitForStartAsync("A", 0, 0, TimeSpan.FromSeconds(5), CancellationToken.None);
            var waitOnB = seq.WaitForStartAsync("B", 0, 0, TimeSpan.FromSeconds(5), CancellationToken.None);

            bool got = tracker.TryComplete("1-1 AR", out var fin);
            Check(ref failures, got && fin.TaskUuid == "B", "completion attributes to the in-flight task (B)");
            seq.CompleteTask(fin.TaskUuid);

            var ok = waitOnB.Wait(TimeSpan.FromSeconds(2));
            Thread.Sleep(100); // give A's waiter a chance to (wrongly) complete before asserting it did not
            Check(ref failures, ok && waitOnB.Result == GateResult.Proceed && !waitOnA.IsCompleted,
                  "B's successor gate releases; A's gate is NOT released");
            Check(ref failures, !tracker.TryComplete("1-1 AR", out _),
                  "unit is idle after the completion (no double attribution)");
        }

        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
