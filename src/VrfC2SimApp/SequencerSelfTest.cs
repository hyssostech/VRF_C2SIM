using System.Diagnostics;

namespace VrfC2SimApp;

/// <summary>
/// Offline check of TaskSequencer (no bridge, no MAK, no VR-Forces):
/// `VrfC2SimApp --sequencer-selftest`. Exercises the four gate behaviors that replace
/// executeTask's busy-waits: predecessor gating, predecessor-already-done (completer-first
/// race), start delay, and predecessor timeout.
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

        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
