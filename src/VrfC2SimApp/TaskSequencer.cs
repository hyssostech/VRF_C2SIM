using System.Collections.Concurrent;

namespace VrfC2SimApp;

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
/// LIMITATION (accepted): phase 1's window runs from wait-start, so a healthy chain deeper
/// than one timeout-length per link can still phase-1-time-out; the real orders carry
/// single-level chains only.
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
        public DateTime DispatchedAtUtc; // written before Dispatched fires (happens-before via the await)
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
    public void NotifyDispatched(string taskUuid)
    {
        if (string.IsNullOrEmpty(taskUuid)) return;
        var st = State(taskUuid);
        st.DispatchedAtUtc = DateTime.UtcNow;
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
    public async Task<GateResult> WaitForStartAsync(string startAfterTaskUuid, long simulationStartMs,
        long relativeDelayMs, TimeSpan predecessorTimeout, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(startAfterTaskUuid))
        {
            var pred = State(startAfterTaskUuid);

            // Phase 1: the predecessor must at least DISPATCH within the window.
            using (var cts1 = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                await Task.WhenAny(pred.Completed.Task, pred.Dispatched.Task, pred.Abandoned.Task,
                                   Task.Delay(predecessorTimeout, cts1.Token)).ConfigureAwait(false);
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
                var remaining = predecessorTimeout - (DateTime.UtcNow - pred.DispatchedAtUtc);
                if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
                using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(ct);
                await Task.WhenAny(pred.Completed.Task, pred.Abandoned.Task,
                                   Task.Delay(remaining, cts2.Token)).ConfigureAwait(false);
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
        if (delayMs > 0)
            await Task.Delay(TimeSpan.FromMilliseconds(delayMs), ct).ConfigureAwait(false);

        return GateResult.Proceed;
    }

    // Lazily create-or-get the state for a task uuid, so waiters and signallers
    // race-freely rendezvous regardless of which arrives first.
    private TaskState State(string taskUuid) => _tasks.GetOrAdd(taskUuid, _ => new TaskState());
}
