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

/// <summary>Outcome of waiting at a task's start gate.</summary>
public enum GateResult
{
    Proceed,              // predecessor done (or none) and any delay elapsed - dispatch now
    PredecessorTimeout,   // the startAfterTaskUuid predecessor did not complete in time
    PredecessorAbandoned, // the predecessor was skipped/abandoned - it will never complete
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
/// </summary>
public sealed class TaskSequencer
{
    private sealed class TaskState
    {
        public readonly TaskCompletionSource Completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Dispatched = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Abandoned = new(TaskCreationOptions.RunContinuationsAsynchronously);
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
    public async Task<GateResult> WaitForStartAsync(string startAfterTaskUuid, long simulationStartMs,
        long relativeDelayMs, double predecessorTimeoutSeconds, TaskClock clock, CancellationToken ct,
        double dispatchTimeoutSeconds = double.NaN)
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
                    if (!pred.Dispatched.Task.IsCompleted) return GateResult.PredecessorTimeout;
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
            await clock.DelayAsync(delayMs / 1000.0, ct).ConfigureAwait(false);

        return GateResult.Proceed;
    }

    // Lazily create-or-get the state for a task uuid, so waiters and signallers
    // race-freely rendezvous regardless of which arrives first.
    private TaskState State(string taskUuid) => _tasks.GetOrAdd(taskUuid, _ => new TaskState());
}
