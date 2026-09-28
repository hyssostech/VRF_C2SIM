using System.Collections.Concurrent;

namespace VrfC2SimApp;

/// <summary>
/// THE CLOCK A TASK'S TIMING IS MEASURED ON (cold-start review of 5c67d41, M2; supervisor ruling
/// 2026-09-14, Q2 default: "a C2SIM Duration, the StartTime delay and the predecessor gate are all
/// measured on the SIMULATION clock with a wall fallback when unreadable").
///
/// WHY IT IS INJECTED RATHER THAN READ HERE. Before this, the Duration was served on the clock
/// Vrf:StallClock selected while the start delay and the predecessor gate were pure
/// <c>Task.Delay</c> - WALL. At COA-STP1's measured sim ratios (0.27x-0.73x) 4,800 SIM seconds is
/// 6,600-17,800 WALL seconds, so a gate measured in wall seconds expires long before the Duration
/// it is waiting for has been served, and every successor is skipped. One clock, injected, is what
/// makes "the gate outlives the end time it waits for" a property rather than a coincidence.
///
/// <paramref name="Now"/> must be MONOTONE NON-DECREASING and expressed in seconds of that clock.
/// The service hands in its own task-clock axis (VrfC2SimService.TaskClockSeconds), which
/// accumulates FORWARD movement only, so a paused scenario adds nothing, a rollback adds nothing,
/// and a fall back to the wall clock does not restart anybody's wait. Tests hand in a fake.
/// </summary>
public sealed record TaskClock(Func<double> Now, Func<double, CancellationToken, Task> DelayAsync)
{
    /// <summary>The wall clock in seconds - the behaviour every caller had before the injection.
    /// Used by the offline self-tests and as the defensive fallback for a null clock.</summary>
    public static readonly TaskClock Wall = new(
        () => DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond,
        (seconds, ct) => Task.Delay(TimeSpan.FromSeconds(Math.Max(0.0, seconds)), ct));
}

/// <summary>
/// Outcome of waiting at a task's start gate. The two TIMEOUTS are deliberately distinct (B7 of
/// the pass-2 review): they are different failures with different remedies, and the run log is
/// where an operator has to tell them apart. A phase-1 timeout means the predecessor NEVER
/// STARTED - look upstream, at the chain or at the order; a phase-2 timeout means it started and
/// did not finish - look at the unit. Reporting both as "did not complete within Ns of its
/// dispatch" mis-stated the cause of every A1 failure, for a predecessor that had never dispatched
/// at all.
/// </summary>
public enum GateResult
{
    Proceed,                    // predecessor done (or none) and any delay elapsed - dispatch now
    PredecessorNeverDispatched, // PHASE 1: the predecessor never even started within its window
    PredecessorTimeout,         // PHASE 2: it started, and did not complete within its window
    PredecessorAbandoned,       // the predecessor was skipped/abandoned - it will never complete
}

/// <summary>
/// Sequences C2SIM task starts, replacing executeTask's busy-waits (C2SIMinterface.cpp:
/// 2087-2154) with async gating. A task with a <c>startAfterTaskUuid</c> waits for that
/// predecessor to complete (signalled by <see cref="CompleteTask"/> off OnVrfTaskCompleted)
/// WITH A TIMEOUT (the fix for the C++ infinite busy-wait, PORT.md sec 6); a task with a
/// start delay waits that long. Pure (no bridge / MAK) so it is unit-testable offline.
///
/// P0.2 (NEXT_SESSION_GUIDANCE.md sec 3, DEFECT B): the predecessor-COMPLETION window is
/// measured from the predecessor's DISPATCH (<see cref="NotifyDispatched"/>), NOT from
/// order arrival - previously every gated task's clock started when the order arrived, so
/// they all timed out together and burst-retasked units mid-route. The wait is two-phase:
///   1. the predecessor must DISPATCH (or complete/abandon) within the window - bounds the
///      wait when it never runs at all;
///   2. once dispatched, it gets a FRESH full window (from its dispatch time) to complete.
/// A predecessor that is skipped/abandoned (<see cref="NotifyAbandoned"/>) fails its
/// waiters FAST (PredecessorAbandoned) instead of letting them run out the clock.
///
/// A1 (cold-start review of `0c96f50`, pass 2). THE TWO PHASES ARE TWO QUESTIONS, SO THEY TAKE
/// TWO WINDOWS. The accepted limitation recorded here - "phase 1's window runs from wait-start,
/// so a healthy chain deeper than one timeout-length per link can still phase-1-time-out; the
/// real orders carry single-level chains only" - rested on a premise that is FALSE for the order
/// this port exists for: COA-STP1 is 11 SERIAL CHAINS up to four tasks deep, and measured on
/// these very classes it lost 21 of its 42 tasks at every shipped setting. Phase 1 now takes its
/// own window (<paramref name="dispatchTimeoutSeconds"/>), which the caller derives from
/// TaskDispatchPolicy.PredecessorDispatchTimeoutSeconds: effectively unbounded for a predecessor
/// that exists in the order (a real dead end ABANDONS it, which is instant), the configured
/// value for a DANGLING reference that nothing will ever speak for.
///
/// M1 (cold-start review of 5c67d41). THE PHASE-2 WINDOW MUST OUTLIVE THE END TIME IT IS WAITING FOR.
/// The window used to be the flat Vrf:TaskPredecessorTimeoutSeconds while the predecessor's
/// completion is given by its C2SIM Duration (R4) - 4,800 s or 7,200 s on COA-STP1 against a
/// 600 s default - so the gate expired FIRST, by construction, and all 31 gated tasks were
/// skipped with TASKABRT. The caller now derives the window from the predecessor's own armed end
/// time (TaskDispatchPolicy.PredecessorTimeoutSeconds) and hands it in; this class only obeys it.
///
/// Parity notes: the C++ waits predecessor-first, then the delay - reproduced. It scales
/// delays by the sim time-multiple and (via a doubled wait loop) actually waits TWICE the
/// delay; NEITHER is reproduced here (the time-multiple scaling is a later refinement, the
/// double-wait is a bug). All golden-trace orders carry zero timing, so these are
/// behavior-neutral for the golden trace.
///
/// STP-850 (2026-09-28): "predecessor-first, then the delay" is NO LONGER the default for a
/// SimulationTime offset. The schema's offset is measured from the scenario start and STP exports
/// absolute slot offsets plus a same-unit STREND lower bound, so serving the offset after the
/// predecessor grew a unit's chain quadratically past the chain backstop. The gate outcomes above
/// are unchanged; what changed is only what the offset is measured from - the caller's
/// startAnchorClock (order receipt under Vrf:StartTimeAnchor=Receipt, the default), giving
/// start = max(predecessor completion, receipt + offset). NaN keeps the oracle's order
/// (Vrf:StartTimeAnchor=PredecessorCompletion). The relative delay keeps it always.
/// </summary>
public sealed class TaskSequencer
{
    private sealed class TaskState
    {
        public readonly TaskCompletionSource Completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Dispatched = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Abandoned = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Overdue = new(TaskCreationOptions.RunContinuationsAsynchronously);
        // The TASK CLOCK reading at dispatch (NaN = never stamped), written before Dispatched
        // fires (happens-before via the await). It is a reading of the caller's monotone axis,
        // NOT a wall timestamp: phase 2 subtracts it from the same axis, so the window a
        // successor gets is measured in the same seconds its predecessor's Duration is.
        public double DispatchedAtClock = double.NaN;
    }

    private readonly ConcurrentDictionary<string, TaskState> _tasks = new();

    /// <summary>Signal that the task with this uuid has completed, releasing any waiters.</summary>
    public void CompleteTask(string taskUuid)
    {
        if (string.IsNullOrEmpty(taskUuid)) return;
        State(taskUuid).Completed.TrySetResult();
    }

    /// <summary>
    /// Signal that the task with this uuid was actually dispatched to VR-Forces. Restarts
    /// its successors' completion window (P0.2: the clock runs from dispatch, not arrival).
    /// </summary>
    /// <param name="atClock">The task clock (seconds) at the moment of dispatch - the same axis
    /// <see cref="WaitForStartAsync"/> measures phase 2 on. NaN means "not stamped", and the
    /// successor then gets the full window.</param>
    public void NotifyDispatched(string taskUuid, double atClock)
    {
        if (string.IsNullOrEmpty(taskUuid)) return;
        var st = State(taskUuid);
        st.DispatchedAtClock = atClock;
        st.Dispatched.TrySetResult();
    }

    /// <summary>
    /// Signal that the task with this uuid will NEVER run (skipped by the timeout policy,
    /// unresolvable taskee, no route points, ...). Its waiters fail fast with
    /// <see cref="GateResult.PredecessorAbandoned"/> instead of running out their clock.
    /// </summary>
    public void NotifyAbandoned(string taskUuid)
    {
        if (string.IsNullOrEmpty(taskUuid)) return;
        State(taskUuid).Abandoned.TrySetResult();
    }

    /// <summary>
    /// Signal that the task with this uuid reached its END TIME with its unit still travelling
    /// (TimedCompletionPolicy.DueKind.OverdueAwaitingArrival). Under the owner's temporary position
    /// (RL-20260921-09) such a task is reported complete when the unit ARRIVES, so its waiters must
    /// keep waiting past their normal window - see the phase-2 extension in
    /// <see cref="WaitForStartAsync"/>.
    /// </summary>
    public void NotifyOverdue(string taskUuid)
    {
        if (string.IsNullOrEmpty(taskUuid)) return;
        State(taskUuid).Overdue.TrySetResult();
    }

    /// <summary>
    /// Wait at a task's start gate: first for its predecessor (two-phase, see class doc),
    /// then for its start delay. Returns <see cref="GateResult.Proceed"/> when the task
    /// should dispatch, or a Predecessor* result when it never became ready.
    /// </summary>
    /// <param name="predecessorTimeoutSeconds">PHASE 2: how long the predecessor has to COMPLETE
    /// once it has dispatched, measured from its dispatch.</param>
    /// <param name="dispatchTimeoutSeconds">PHASE 1 (A1): how long the predecessor has to
    /// DISPATCH AT ALL, measured from this gate's own wait-start - which for every task in an
    /// order is order receipt. NaN (the default) means "the same window as phase 2", the
    /// pre-A1 behaviour, kept so a caller that has no chain context is unchanged; the service
    /// passes TaskDispatchPolicy.PredecessorDispatchTimeoutSeconds.</param>
    /// <param name="overdueBackstopSeconds">PHASE 2 EXTENSION (2026-09-25): when phase 2's window
    /// expires on a predecessor that has been signalled OVERDUE (<see cref="NotifyOverdue"/>), keep
    /// waiting until this many seconds after ITS dispatch. The service passes
    /// Vrf:TaskChainBackstopSeconds. NaN (the default) = no extension.</param>
    /// <param name="treatPredecessorAsOverdue">RL-20260927-05 (2026-09-27, the owner's "Q1 a"): asked ONCE,
    /// when phase 2's window expires on a predecessor that has NOT been signalled OVERDUE (and has neither
    /// completed nor been abandoned). True = treat it as OVERDUE: the wait extends to
    /// <paramref name="overdueBackstopSeconds"/> exactly as for a signalled one. The service answers from
    /// TimedCompletionPolicy.IsUnfinishedMover - a predecessor with a destination and no TASKCMPLT or
    /// TASKABRT yet. Null (the default) = never asked, the pre-RL-20260927-05 behaviour; a question that
    /// throws counts as false (that behaviour), never as a faulted gate.</param>
    /// <param name="startAnchorClock">STP-850: the task-clock reading the SimulationTime offset
    /// (<paramref name="simulationStartMs"/>) is measured FROM - order receipt, stamped once per order
    /// (Vrf:StartTimeAnchor=Receipt). The task then dispatches at max(predecessor completion,
    /// anchor + offset). NaN (the default) = the offset is a delay served AFTER the predecessor
    /// completes (Vrf:StartTimeAnchor=PredecessorCompletion, the pre-STP-850 behaviour). The
    /// relative delay is never anchored: it stays a delay after the predecessor. Every gate
    /// outcome above the delay is unchanged either way.</param>
    public async Task<GateResult> WaitForStartAsync(string startAfterTaskUuid, long simulationStartMs,
        long relativeDelayMs, double predecessorTimeoutSeconds, TaskClock clock, CancellationToken ct,
        double dispatchTimeoutSeconds = double.NaN, double overdueBackstopSeconds = double.NaN,
        Func<bool> treatPredecessorAsOverdue = null, double startAnchorClock = double.NaN)
    {
        clock ??= TaskClock.Wall;
        double timeoutSeconds = Math.Max(0.0, predecessorTimeoutSeconds);
        double dispatchSeconds = double.IsFinite(dispatchTimeoutSeconds)
                               ? Math.Max(0.0, dispatchTimeoutSeconds) : timeoutSeconds;
        if (!string.IsNullOrEmpty(startAfterTaskUuid))
        {
            var pred = State(startAfterTaskUuid);

            // Phase 1: the predecessor must at least DISPATCH within ITS OWN window (A1) - the
            // one that knows whether anything will ever speak for that predecessor at all.
            using (var cts1 = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                await Task.WhenAny(pred.Completed.Task, pred.Dispatched.Task, pred.Abandoned.Task,
                                   clock.DelayAsync(dispatchSeconds, cts1.Token)).ConfigureAwait(false);
                cts1.Cancel(); // stop the timer if a signal won (no lingering delay)
                if (!pred.Completed.Task.IsCompleted)
                {
                    ct.ThrowIfCancellationRequested(); // shutdown -> propagate, not a "timeout"
                    if (pred.Abandoned.Task.IsCompleted) return GateResult.PredecessorAbandoned;
                    // B7: this is NOT the same failure as phase 2's. The predecessor never
                    // started, so nothing about ITS dispatch can be quoted at the operator.
                    if (!pred.Dispatched.Task.IsCompleted) return GateResult.PredecessorNeverDispatched;
                }
            }

            // Phase 2: once dispatched, a FRESH full window - measured from the dispatch
            // time - to complete (P0.2: don't punish a successor for its predecessor's own
            // long gate wait).
            if (!pred.Completed.Task.IsCompleted)
            {
                double served = double.IsNaN(pred.DispatchedAtClock)
                              ? 0.0 : Math.Max(0.0, clock.Now() - pred.DispatchedAtClock);
                double remaining = Math.Max(0.0, timeoutSeconds - served);
                using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(ct);
                await Task.WhenAny(pred.Completed.Task, pred.Abandoned.Task,
                                   clock.DelayAsync(remaining, cts2.Token)).ConfigureAwait(false);
                cts2.Cancel();
                // RL-20260921-09 (completion unit, 2026-09-25): A LATE UNIT HOLDS ITS TASK OPEN. The
                // window above is the predecessor's end time + margin; a predecessor whose unit is
                // still travelling at its end time is OVERDUE and is completed when it ARRIVES, so
                // skipping its follow-on here would abort a chain the owner said is merely delayed
                // ("follow on tasks are delayed by the slow progress on a leg"). Keep waiting on its
                // completion or abandonment, bounded by the chain backstop measured from ITS
                // dispatch. NaN (the default) = no extension, the pre-2026-09-25 behaviour.
                // RL-20260927-05 (2026-09-27, the owner's "Q1 a"): AND THE EXTENSION DOES NOT HANG ON A
                // RACE. The OVERDUE signal comes from the timed walk (tick thread, at most once a WALL
                // second) while this window expires on the pool the moment the task clock reaches
                // end + margin; one task-clock step larger than the margin let the window win, and the
                // follow-on of a mover that then ARRIVED was skipped (TimerAnchorSelfTest t4). So a
                // predecessor not yet signalled is ASKED: one that has a destination and has not
                // finished (no TASKCMPLT, no TASKABRT) is treated as OVERDUE. A predecessor with no
                // destination (a hold) or no timer at all answers false and keeps the skip - a hold
                // ends by its timer, so a skip here means its timer had not completed it by
                // end + margin. A STUCK unit's follow-ons are ABANDONED (RL-20260925-01 Q2), and the
                // abandonment is tested first, here and in the extension's own wait.
                if (!pred.Completed.Task.IsCompleted && !pred.Abandoned.Task.IsCompleted
                    && double.IsFinite(overdueBackstopSeconds)
                    && (pred.Overdue.Task.IsCompleted || Ask(treatPredecessorAsOverdue)))
                {
                    ct.ThrowIfCancellationRequested();
                    double servedNow = double.IsNaN(pred.DispatchedAtClock)
                                     ? 0.0 : Math.Max(0.0, clock.Now() - pred.DispatchedAtClock);
                    double more = Math.Max(0.0, Math.Max(0.0, overdueBackstopSeconds) - servedNow);
                    using var cts3 = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    await Task.WhenAny(pred.Completed.Task, pred.Abandoned.Task,
                                       clock.DelayAsync(more, cts3.Token)).ConfigureAwait(false);
                    cts3.Cancel();
                }
                if (!pred.Completed.Task.IsCompleted)
                {
                    ct.ThrowIfCancellationRequested();
                    return pred.Abandoned.Task.IsCompleted ? GateResult.PredecessorAbandoned
                                                           : GateResult.PredecessorTimeout;
                }
            }
        }

        // Absolute (SimulationTime) delay takes precedence over the relative one, matching
        // executeTask's if/else-if (:2099 / :2128).
        long delayMs = simulationStartMs > 0 ? simulationStartMs
                     : relativeDelayMs > 0 ? relativeDelayMs : 0;
        // M2: the start delay is served on the SAME clock as the Duration and the gate above.
        // An order's delay is a statement about the scenario, not about the operator's afternoon.
        if (delayMs > 0)
        {
            double delaySeconds = delayMs / 1000.0;
            // STP-850: under Vrf:StartTimeAnchor=Receipt the SimulationTime offset is measured from the
            // anchor (order receipt), so only what is LEFT of it after the predecessor gate is served:
            // start = max(predecessor completion, anchor + offset). A relative delay is not anchored.
            if (simulationStartMs > 0 && double.IsFinite(startAnchorClock))
                delaySeconds = Math.Max(0.0, startAnchorClock + delaySeconds - clock.Now());
            if (delaySeconds > 0.0)
                await clock.DelayAsync(delaySeconds, ct).ConfigureAwait(false);
        }

        return GateResult.Proceed;
    }

    // RL-20260927-05: the caller's question, asked at most once per gate. A throw is the pre-ruling
    // answer (false: no extension), never a faulted gate - the caller logs its own failures.
    private static bool Ask(Func<bool> question)
    {
        if (question is null) return false;
        try { return question(); }
        catch (Exception) { return false; }
    }

    // Lazily create-or-get the state for a task uuid, so waiters and signallers
    // race-freely rendezvous regardless of which arrives first.
    private TaskState State(string taskUuid) => _tasks.GetOrAdd(taskUuid, _ => new TaskState());
}
