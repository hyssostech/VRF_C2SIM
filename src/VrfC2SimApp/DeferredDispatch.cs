namespace VrfC2SimApp;

/// <summary>
/// THE ENDING A DISPATCH GETS WHEN IT DIES ON THE VR-FORCES TICK THREAD (D1, cold-start review
/// of `8db033e`, pass 3 - the same defect `b76c9c7` closed for one enqueue and left open on the
/// one the demo actually takes).
///
/// WHY THIS EXISTS. Bridge work is marshalled onto the tick thread as an enqueued action, and the
/// drain's only handler (VrfC2SimService.TickLoop) catches a throw, LOGS it and returns: it tells
/// the sequencer nothing and STP nothing. A task whose dispatch died there reported NO TASKSTRT
/// and NO TASKABRT, and its successors' phase-1 gate - which since A1 has no configured bound for
/// a predecessor that is a task in the SAME order - then waited the whole
/// Vrf:TaskChainBackstopSeconds (86,400 task-clock seconds) before being skipped. A1 was supposed
/// to make a healthy deep chain work, not make a dead one 18x slower; that is exactly the trade a
/// backstop must not make.
///
/// WHY ONE ENQUEUE WAS NOT ENOUGH. `b76c9c7` guarded the FIRST dispatch pass. In the DEFAULT
/// configuration that pass dispatches nothing: Vrf:GroundWaypointAltitudeMode defaults to
/// "TerrainProfile" (VrfSettings.cs) and neither settings file overrides it, so for a ground unit
/// with route points the first pass asks the back end for terrain heights under the vertices and
/// RETURNS, having marked nothing. The REAL dispatch is the re-entry from the terrain reply (or
/// from the timeout sweep), and it runs strictly MORE code than the guarded pass: it creates the
/// route, calls the bridge and runs MarkDispatched. So the ending lives here, once, and every
/// deferred dispatch runs through it:
///   - an ERROR naming the task, WHERE it died and the exception;
///   - TaskSequencer.NotifyAbandoned, so the successors fail FAST (PredecessorAbandoned at 0 s of
///     clock) instead of waiting out the backstop;
///   - ONE TASKABRT through the service's single emit point, so STP is not left waiting for a
///     status it will never get.
///
/// NOTHING HERE ANNOUNCES A START, and nothing here needs to know whether the throw happened
/// before or after MarkDispatched. If MarkDispatched had already run, its TASKSTRT has been sent
/// and TaskStatusPolicy.ShouldEmitStart refuses a second one for the same execution; the TASKABRT
/// is exactly one either way, because ShouldEmitAbort is one-per-task and is blocked after a
/// TASKCMPLT. NotifyAbandoned is a TrySetResult and is idempotent.
///
/// PURE, AND IT TAKES ITS COLLABORATORS AS ARGUMENTS. The service glue around it needs a bridge,
/// a tick thread and a C2SIM server and cannot be driven offline; this can, so `--rulings-selftest`
/// drives a THROWING continuation through THIS code with the real TaskSequencer and the real
/// TaskStatusPolicy instead of re-implementing the ending in the test (pass-3 N5's complaint about
/// the glue re-implementations is not repeated here).
/// </summary>
public static class DeferredDispatch
{
    /// <summary>Where the re-entry that really dispatches a ground move runs - the pass that
    /// creates the route, calls the bridge and runs MarkDispatched.</summary>
    public const string TerrainContinuation = "the terrain-profile continuation";

    /// <summary>Where the first pass runs - which in the default TerrainProfile mode only asks for
    /// terrain heights and defers.</summary>
    public const string FirstPass = "the first dispatch pass";

    /// <summary>Where the ROUTE SHIFT re-entry runs - the pass that carries the route with its
    /// inserted waypoints back into the dispatch (Vrf:PreflightRouteShift; STP-804/806).</summary>
    public const string RouteShiftContinuation = "the route-shift continuation";

    /// <summary>Where a dispatch runs after its READINESS HOLD released it (D5b, 2026-09-21;
    /// VrfC2SimService.HoldThenDispatchAsync) - the pass that would have been the first one had the
    /// taskee been taskable when the task was ready.</summary>
    public const string ReadinessRelease = "the dispatch released by the taskee-readiness hold";

    /// <summary>
    /// The sentence a dispatch that threw on the tick thread reports to C2SIM. The exception TYPE
    /// is in it as well as the message: a MissingMethodException from a stale native deploy (the
    /// case TickLoop's own comment records) and a NullReferenceException from a missing route read
    /// the same otherwise, and they are not the same problem. WHERE is in it too, because the two
    /// enqueues fail for different reasons and only one of them has already created a route.
    /// </summary>
    public static string AbortReason(string taskName, string where, Exception ex)
        => $"ABANDONED: dispatching task '{taskName}' threw on the VR-Forces tick thread "
         + $"({where}: {ex?.GetType().Name ?? "unknown"}: {ex?.Message})";

    /// <summary>
    /// Run one deferred dispatch continuation on the tick thread under that ending.
    /// </summary>
    /// <param name="continuation">The dispatch work. A null continuation is not a failure.</param>
    /// <param name="taskUuid">The C2SIM task this work dispatches. Empty/null means there is no
    /// task to abandon (the shared terrain plumbing's OTHER consumer is the init placement query),
    /// and then the ending is the ERROR alone.</param>
    /// <param name="sequencer">Told the task is abandoned, so its successors' gate fails FAST
    /// instead of running out the chain backstop. Null = nobody to tell (the pre-fix ending).</param>
    /// <param name="reportAbort">The service's PushTaskStatus, which applies TaskStatusPolicy and
    /// is the ONE place a TaskStatus leaves this interface, so the one-per-task guarantee is not
    /// re-implemented here. Null = nobody to tell (the pre-fix ending).</param>
    /// <param name="logError">Says what happened. Given the EXCEPTION, not the sentence, so the
    /// caller words it for its own consumer.</param>
    /// <returns>The exception the continuation threw, or null when it completed.</returns>
    public static Exception Run(Action continuation, string taskUuid, string taskName, string where,
                                TaskSequencer sequencer, Action<string> reportAbort,
                                Action<Exception> logError)
    {
        if (continuation == null) return null;
        try
        {
            continuation();
            return null;
        }
        catch (Exception ex)
        {
            // The order every other dead end in the service uses: say it, release the successors,
            // then report to STP. Each step is guarded on its own - an ending that needs an ending
            // is how the swallow this fixes got there in the first place.
            Safely(logError, ex);
            if (sequencer != null) Safely(() => sequencer.NotifyAbandoned(taskUuid));
            if (reportAbort != null && !string.IsNullOrEmpty(taskUuid))
                Safely(() => reportAbort(AbortReason(taskName, where, ex)));
            return ex;
        }
    }

    private static void Safely(Action a)
    {
        try { a(); } catch { /* deliberately swallowed: this IS the last-resort ending */ }
    }

    private static void Safely(Action<Exception> a, Exception ex)
    {
        try { a?.Invoke(ex); } catch { /* deliberately swallowed: this IS the last-resort ending */ }
    }
}
