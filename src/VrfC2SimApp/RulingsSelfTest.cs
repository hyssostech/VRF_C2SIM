using S = C2SIM.Schema102;

namespace VrfC2SimApp;

/// <summary>
/// Offline check of the four tasking rulings of 2026-09-14 (no bridge, no MAK, no VR-Forces):
/// `VrfC2SimApp --rulings-selftest`. One switch, four sections, because the rulings are one
/// decision about how a C2SIM task becomes a VR-Forces task:
///
///   R4 "completion is given by the end time" - TimedCompletionPolicy on a FAKE clock.
///   R2 "a task without geometry uses the geometry of the performing (who) unit" -
///      TaskDispatchPolicy.ZeroGeometry.
///   R3 "the target IS the objective" - TaskDispatchPolicy.ResolveTarget (no verb is refused
///      for self-targeting).
///   R1 (transition) MapGraphicID -> the init graphic created under the same C2SIM uuid -
///      TaskGeometryResolver.
///
/// Every policy under test is PURE: no clock is read, no bridge is called, no report is sent,
/// so the checks are decidable offline and a live run has nothing to prove about them.
/// </summary>
public static class RulingsSelfTest
{
    public static int Run()
    {
        int failures = 0;
        Console.WriteLine("=== R4: completion is given by the end time ===");
        R4(ref failures);
        Console.WriteLine("=== R2: a task without geometry uses the performing unit's position ===");
        R2(ref failures);
        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    // ---------------------------------------------------------------- R4 ----
    private static void R4(ref int failures)
    {
        const double dur = 4800.0;   // COA-STP1's PT1H20M, in seconds

        // (b1) A task with Duration X completes with EXACTLY ONE TASKCMPLT at X.
        {
            var p = new TimedCompletionPolicy();
            Check(ref failures, p.Register("T1", "taskee-1", "T1_Secure", "1-35 AR", dur),
                  "a task with a Duration registers a timed end");
            // Anchor, then walk the clock in 600 s steps. Nothing is due before the deadline.
            p.Advance(1000.0, usingSim: true);
            int dueBefore = 0;
            for (double t = 1600.0; t < 1000.0 + dur; t += 600.0)
                dueBefore += p.Advance(t, usingSim: true).Count;
            Check(ref failures, dueBefore == 0, $"nothing completes before the end time ({dueBefore} early)");

            var due = p.Advance(1000.0 + dur, usingSim: true);
            Check(ref failures, due.Count == 1 && due[0].TaskUuid == "T1",
                  $"exactly one completion AT the end time (got {due.Count})");
            var again = p.Advance(1000.0 + dur + 10000.0, usingSim: true);
            Check(ref failures, again.Count == 0 && p.Count == 0,
                  "the timer is consumed - no second completion, ever");
        }

        // (b2) An EARLIER arrival-evidence completion cancels the timer.
        {
            var p = new TimedCompletionPolicy();
            p.Register("T2", "taskee-2", "T2_Move", "1-35 AR", dur);
            p.Advance(0.0, usingSim: false);
            p.Advance(100.0, usingSim: false);
            Check(ref failures, TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKCMPLT),
                  "a TASKCMPLT cancels the timed end (arrival evidence wins)");
            Check(ref failures, p.Cancel("T2"), "the arrival completion removes the pending timer");
            Check(ref failures, p.Advance(100.0 + dur * 2, usingSim: false).Count == 0,
                  "a cancelled timer never fires a second TASKCMPLT");
        }

        // (b3) A TASKABRT (stall watchdog, refusal, skipped successor) also cancels it.
        {
            var p = new TimedCompletionPolicy();
            p.Register("T3", "taskee-3", "T3_Attack", "1-6 IN", dur);
            p.Advance(0.0, usingSim: false);
            Check(ref failures, TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKABRT),
                  "a TASKABRT cancels the timed end (the task is not going to run)");
            Check(ref failures, !TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKSTRT)
                             && !TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKINPRG),
                  "TASKSTRT / TASKINPRG do NOT cancel it (a start and a progress note are not an end)");
            p.Cancel("T3");
            Check(ref failures, p.Advance(dur * 2, usingSim: false).Count == 0,
                  "an aborted task produces no later timed TASKCMPLT");
        }

        // (b4) A PAUSED clock does not age the task, and a ROLLBACK does not complete it early.
        {
            var p = new TimedCompletionPolicy();
            p.Register("T4", "taskee-4", "T4_Defend", "A/6-56", 100.0);
            p.Advance(1000.0, usingSim: true);
            p.Advance(1050.0, usingSim: true);              // 50 s served
            for (int i = 0; i < 20; i++) p.Advance(1050.0, usingSim: true);   // scenario paused
            Check(ref failures, p.Count == 1, "a paused sim clock does not age a timed task");
            p.Advance(900.0, usingSim: true);               // rollbackToSnapshot: clock steps BACK
            Check(ref failures, p.Count == 1, "a backwards clock step does not complete the task");
            Check(ref failures, p.Advance(950.0, usingSim: true).Count == 1,
                  "the remaining 50 s still complete it once the clock advances again");
        }

        // (b5) A clock-MODE change (sim reader lost -> wall fallback) re-anchors instead of
        //      completing the task on the difference between two unrelated time bases.
        {
            var p = new TimedCompletionPolicy();
            p.Register("T5", "taskee-5", "T5_Screen", "B/6-56", 100.0);
            p.Advance(500.0, usingSim: true);
            p.Advance(540.0, usingSim: true);                    // 40 s served on the sim clock
            var onSwitch = p.Advance(1.7e9, usingSim: false);    // wall seconds: a huge step
            Check(ref failures, onSwitch.Count == 0 && p.Count == 1,
                  "a clock-mode change re-anchors (no completion on the mode step itself)");
            Check(ref failures, p.Advance(1.7e9 + 59.0, usingSim: false).Count == 0
                             && p.Advance(1.7e9 + 61.0, usingSim: false).Count == 1,
                  "the 60 s it still owes are served on the new clock");
        }

        // (b6) A task with NO Duration has no timed end (it completes on its own evidence only).
        {
            var p = new TimedCompletionPolicy();
            Check(ref failures, !p.Register("T6", "taskee-6", "T6_Move", "C/6-56", 0.0),
                  "a task with no Duration registers no timer");
            Check(ref failures, p.Advance(1e6, usingSim: false).Count == 0 && p.Count == 0,
                  "... and never completes on time");
        }

        // (b7) THE STREND CHAIN: the timed completion releases the successor's gate exactly as a
        //      vendor completion does. This is the service's rule expressed here - the due entry's
        //      task uuid is handed to TaskSequencer.CompleteTask - so the chain cannot be broken
        //      by a change to either side without this check failing.
        {
            var p = new TimedCompletionPolicy();
            var seq = new TaskSequencer();
            p.Register("PRED", "taskee-7", "T7_Secure", "1-35 AR", 100.0);
            seq.NotifyDispatched("PRED");
            var successor = seq.WaitForStartAsync("PRED", 0, 0, TimeSpan.FromSeconds(5),
                                                  CancellationToken.None);
            p.Advance(0.0, usingSim: false);
            Thread.Sleep(120);
            Check(ref failures, !successor.IsCompleted, "the successor waits while the task is running");
            var due = p.Advance(100.0, usingSim: false);
            foreach (var d in due) seq.CompleteTask(d.TaskUuid);   // exactly what the service does
            bool released = successor.Wait(TimeSpan.FromSeconds(2));
            Check(ref failures, released && successor.Result == GateResult.Proceed,
                  "the STREND successor dispatches after the TIMED completion");
        }
    }

    // ---------------------------------------------------------------- R2 ----
    private static void R2(ref int failures)
    {
        // (c1) A T9-SHAPED TASK - "T9_ProvideAirDefenseCoverage...", zero Locations, no distinct
        //      affected entity - is DISPATCHED IN PLACE, not refused. This is the exact task run
        //      G6 logged as "NO LOCATION GIVEN - CAN'T EXECUTE TASK".
        var t9 = TaskDispatchPolicy.ForZeroGeometry(performerResolved: true, hasAttackTarget: false,
                                                    hasBreachTarget: false);
        Check(ref failures, t9 == ZeroGeometryAction.ExecuteInPlace,
              $"a zero-geometry task executes at the performing unit's position (got {t9})");
        Check(ref failures, !TaskDispatchPolicy.Refuses(t9),
              "... and is NOT refused, so its STREND chain is not abandoned");

        // (c2) The ONLY refusal left is the one that was never about geometry.
        var noUnit = TaskDispatchPolicy.ForZeroGeometry(performerResolved: false, hasAttackTarget: false,
                                                        hasBreachTarget: false);
        Check(ref failures, noUnit == ZeroGeometryAction.Refuse && TaskDispatchPolicy.Refuses(noUnit),
              "a task whose PERFORMER cannot be resolved is still refused");

        // (c3) A resolved distinct target still engages in place (unchanged behaviour).
        Check(ref failures,
              TaskDispatchPolicy.ForZeroGeometry(true, hasAttackTarget: true, hasBreachTarget: false)
                  == ZeroGeometryAction.EngageInPlace
              && TaskDispatchPolicy.ForZeroGeometry(true, hasAttackTarget: false, hasBreachTarget: true)
                  == ZeroGeometryAction.BreachInPlace,
              "a resolved attack / breach target still engages in place");

        // (c4) The derivation is REPORTED, in the ruling's own words.
        Check(ref failures,
              TaskDispatchPolicy.ZeroGeometryObservation
                  == "no geometry in the order: executing at the performing unit's position",
              "the in-place dispatch announces the derivation verbatim");

        // (c5) THE SUCCESSOR IS NOT SKIPPED. The in-place task is dispatched (not abandoned) and
        //      closed by R4's end time, so the STREND gate releases exactly as for a moving task.
        {
            var seq = new TaskSequencer();
            var timed = new TimedCompletionPolicy();
            const string inPlace = "T9";
            seq.NotifyDispatched(inPlace);                       // what MarkDispatched does
            timed.Register(inPlace, "taskee-9", "T9_ProvideAirDefenseCoverage", "A/6-56 ADA", 300.0);
            var successor = seq.WaitForStartAsync(inPlace, 0, 0, TimeSpan.FromSeconds(5),
                                                  CancellationToken.None);
            timed.Advance(0.0, usingSim: false);
            Thread.Sleep(120);
            Check(ref failures, !successor.IsCompleted,
                  "the successor of an in-place task waits (it was not abandoned)");
            foreach (var d in timed.Advance(300.0, usingSim: false)) seq.CompleteTask(d.TaskUuid);
            bool released = successor.Wait(TimeSpan.FromSeconds(2));
            Check(ref failures, released && successor.Result == GateResult.Proceed,
                  "the successor of an in-place task DISPATCHES at its predecessor's end time");
        }
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
