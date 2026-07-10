using System.Collections.Concurrent;

namespace VrfC2SimApp;

/// <summary>Outcome of waiting at a task's start gate.</summary>
public enum GateResult
{
    Proceed,            // predecessor done (or none) and any delay elapsed - dispatch now
    PredecessorTimeout, // the startAfterTaskUuid predecessor did not complete in time
}

/// <summary>
/// Sequences C2SIM task starts, replacing executeTask's busy-waits (C2SIMinterface.cpp:
/// 2087-2154) with async gating. A task with a <c>startAfterTaskUuid</c> waits for that
/// predecessor to complete (signalled by <see cref="CompleteTask"/> off OnVrfTaskCompleted)
/// WITH A TIMEOUT (the fix for the C++ infinite busy-wait, PORT.md sec 6); a task with a
/// start delay waits that long. Pure (no bridge / MAK) so it is unit-testable offline.
///
/// Parity notes: the C++ waits predecessor-first, then the delay - reproduced. It scales
/// delays by the sim time-multiple and (via a doubled wait loop) actually waits TWICE the
/// delay; NEITHER is reproduced here (the time-multiple scaling is a later refinement, the
/// double-wait is a bug). All golden-trace orders carry zero timing, so these are
/// behavior-neutral for the golden trace.
/// </summary>
public sealed class TaskSequencer
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _completions = new();

    /// <summary>Signal that the task with this uuid has completed, releasing any waiters.</summary>
    public void CompleteTask(string taskUuid)
    {
        if (string.IsNullOrEmpty(taskUuid)) return;
        Tcs(taskUuid).TrySetResult();
    }

    /// <summary>
    /// Wait at a task's start gate: first for its predecessor to complete (bounded by
    /// <paramref name="predecessorTimeout"/>), then for its start delay. Returns
    /// <see cref="GateResult.Proceed"/> when the task should dispatch, or
    /// <see cref="GateResult.PredecessorTimeout"/> if the predecessor never completed.
    /// </summary>
    public async Task<GateResult> WaitForStartAsync(string startAfterTaskUuid, long simulationStartMs,
        long relativeDelayMs, TimeSpan predecessorTimeout, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(startAfterTaskUuid))
        {
            var pred = Tcs(startAfterTaskUuid).Task;
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var done = await Task.WhenAny(pred, Task.Delay(predecessorTimeout, timeoutCts.Token))
                                 .ConfigureAwait(false);
            timeoutCts.Cancel(); // stop whichever timer is still pending (no lingering delay)
            if (done != pred)
            {
                ct.ThrowIfCancellationRequested(); // shutdown -> propagate, not a "timeout"
                return GateResult.PredecessorTimeout;
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

    // Lazily create-or-get the completion source for a task uuid, so the waiter and the
    // completer race-freely rendezvous regardless of which arrives first.
    private TaskCompletionSource Tcs(string taskUuid)
        => _completions.GetOrAdd(taskUuid,
            _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
}
