using System.Collections.Concurrent;
using S = C2SIM.Schema102;

namespace VrfC2SimApp;

/// <summary>
/// WHEN A TASK WITH A DURATION IS OVER - the owner's TEMPORARY position on completion
/// (docs/RULINGS.md RL-20260921-09, 2026-09-21, "Mark this is as temporary"): completion is based
/// on start time + Duration; a unit that arrives after that "complete[s] immediatelly"; follow-on
/// tasks still get their whole Duration even when they start late. Implemented 2026-09-25 as the
/// completion unit whose scope the owner approved that day (RL-20260925-01). Pure state machine -
/// no clock is read here, no bridge, no report - so the whole rule is decidable offline
/// (`--rulings-selftest`, the RL-20260921-09 section).
///
/// WHAT THIS REPLACES. From 746c091 (2026-09-14) until this unit, every dispatched task with a
/// Duration was reported TASKCMPLT when its timer expired, whether or not the unit had arrived,
/// and the real arrival was then suppressed as a duplicate. That rested on reading the 2026-09-14
/// answer "4 given by the end time" as covering every task; docs/CORRECTIONS_LOG.md F-1 withdrew
/// that reading - the question put that day was about SECURE / OCCUPY / DEFEND (RL-20260914-02),
/// not about movement - and the owner then set the temporary position this class implements.
///
/// THE RULE, clause by clause:
///   - THE END TIME IS DISPATCH + DURATION (x Vrf:DurationScale). ONE TIMER PER TASK, ARMED AT
///     DISPATCH; Register is first-dispatch-wins, so a re-entered dispatch (the TerrainProfile
///     second pass) does not restart it. Each task is armed at its OWN dispatch, so a follow-on that
///     starts late still gets its full Duration.
///   - AN EARLY FINISH IS HELD. A unit that arrives, or a VR-Forces task that ends successfully,
///     before the end time is recorded (<see cref="MarkFinished"/> returns Hold) and reported
///     complete AT the end time, not before.
///   - A LATE UNIT IS NOT COMPLETE AT THE END TIME. A task that HAS A DESTINATION and has not
///     finished when its time is up is handed out ONCE as OverdueAwaitingArrival and stays in the
///     table; the service logs one OVERDUE line, reports nothing and releases nothing. When the
///     unit arrives (the interface's arrival evidence, or a successful vendor completion),
///     MarkFinished returns EmitNow and the TASKCMPLT is sent at once.
///   - A TASK WITH NO DESTINATION (hold in place, defend, fire, follow, patrol and the like) ends
///     at its end time, as before. "Has a destination" is purely whether the dispatch had one - no
///     verb list. Whether a task's desired effect was achieved is IGNORED (the temporary position;
///     the research question, not a gap).
///   - A STUCK UNIT is the stall watchdog's business (RL-20260914-01: TASKABRT is the code STP
///     sees). Its abort is REPORT-ONLY and does NOT cancel the timer (<see cref="CancelsTimer(S.TaskStatusCodeType, bool)"/>):
///     a unit that stalls, recovers and arrives before its end time must still not complete early,
///     and one that arrives after it still reports complete. That a later arrival reports complete
///     after the abort is a supervisor position (RL-20260914-01 covers the code only).
///   - EVERY OTHER REAL END CANCELS THE TIMER: a TASKABRT for a refusal, a skipped successor, a
///     supersede, a vendor failure or a back-end loss means the task is not running at all.
///   - THE TIMER MEASURES ELAPSED CLOCK, NOT A DEADLINE STAMP. Each Advance adds the clock's
///     FORWARD movement since the previous one: a PAUSED scenario stops it dead, and
///     DtVrfRemoteController::rollbackToSnapshot steps the sim clock BACKWARDS
///     (vrfRemoteController.h:605) - a deadline stamp would fire the moment a rollback landed past
///     it, and would treat a pause as time served.
///   - A CLOCK-MODE CHANGE RE-ANCHORS. The sim reading can disappear mid-run (VrfFacade::
///     SimTimeSeconds returns -1.0 with no back end reporting) and the interface falls back to the
///     wall clock; the first Advance in a new mode contributes ZERO and only re-anchors.
///   - A TASK WITH NO DURATION GETS NO TIMER. It completes on its own evidence or not at all;
///     inventing an end time for it would be manufacturing a decision.
///
/// The emission itself is NOT here: the service pushes the TaskStatus through its single emit
/// point (PushTaskStatus -> TaskStatusPolicy) and releases the STREND gate
/// (TaskSequencer.CompleteTask). <see cref="CompletionCode"/> and <see cref="ReleasesSuccessorsNow"/>
/// are the two decisions it takes from here on every completion, so the offline flow and the
/// service cannot drift apart.
/// </summary>
public sealed class TimedCompletionPolicy
{
    /// <summary>One task waiting for its end time. Mutable: Advance walks it forward.</summary>
    public sealed class Pending
    {
        public string TaskUuid = "";
        public string TaskeeUuid = "";
        public string TaskName = "";
        public string UnitName = "";
        /// <summary>The task's Duration, in seconds on whichever clock is in use, AFTER
        /// Vrf:DurationScale (the service applies the scale; the policy never sees the raw value).</summary>
        public double DurationSeconds;
        /// <summary>Clock time served so far. Only FORWARD movement is ever added.</summary>
        public double Elapsed;
        /// <summary>The clock reading at the previous Advance, or NaN before the first one
        /// (and after a mode change) - the anchor this entry measures from.</summary>
        public double LastClock = double.NaN;
        /// <summary>The clock mode the anchor belongs to (true = the simulation clock).</summary>
        public bool UsingSim;
        /// <summary>The dispatch gave the unit a destination (the service passes dest != null).</summary>
        public bool HasDestination;
        /// <summary>The unit arrived, or the VR-Forces task ended successfully, BEFORE the end
        /// time: the completion is held for it.</summary>
        public bool Finished;
        /// <summary>The end time passed with the unit still travelling; handed out once.</summary>
        public bool Overdue;
        /// <summary>What <see cref="Advance"/> handed this entry out as.</summary>
        public DueKind Kind = DueKind.CompleteNow;
    }

    /// <summary>What a due entry means to the service.</summary>
    public enum DueKind
    {
        /// <summary>Report TASKCMPLT now and release the follow-ons (no destination, or the unit
        /// already finished). The entry has left the table.</summary>
        CompleteNow,
        /// <summary>The end time passed and the unit has not arrived: log it, report nothing,
        /// release nothing. The entry STAYS until the arrival or a terminal abort.</summary>
        OverdueAwaitingArrival,
    }

    /// <summary>What a successful completion (arrival evidence or a vendor success) should do.</summary>
    public enum FinishVerdict
    {
        /// <summary>No timer is armed for this task (no Duration, Vrf:TimedCompletion off, or it
        /// already ended): today's evidence-only completion applies.</summary>
        NotTimed,
        /// <summary>The end time is still ahead: hold the TASKCMPLT and the follow-on release
        /// for the timer.</summary>
        Hold,
        /// <summary>The task was overdue: the unit has now arrived - report it complete at once.</summary>
        EmitNow,
    }

    private readonly ConcurrentDictionary<string, Pending> _pending = new(StringComparer.Ordinal);

    /// <summary>How many tasks are waiting for their end time or, once overdue, for their unit
    /// (for the status line).</summary>
    public int Count => _pending.Count;

    /// <summary>
    /// Arm the timer for a dispatched task. FIRST DISPATCH WINS: a re-entered dispatch returns
    /// false and leaves the running timer alone. A non-positive or non-finite duration arms
    /// nothing (returns false) - that task has no end time.
    /// </summary>
    /// <param name="hasDestination">The dispatch gave the unit somewhere to go. Only such a task
    /// can be OVERDUE; one without a destination always ends at its end time.</param>
    public bool Register(string taskUuid, string taskeeUuid, string taskName, string unitName,
                         double durationSeconds, bool hasDestination = false)
    {
        if (string.IsNullOrEmpty(taskUuid)) return false;
        if (!double.IsFinite(durationSeconds) || durationSeconds <= 0.0) return false;
        return _pending.TryAdd(taskUuid, new Pending
        {
            TaskUuid = taskUuid,
            TaskeeUuid = taskeeUuid ?? "",
            TaskName = taskName ?? "",
            UnitName = unitName ?? "",
            DurationSeconds = durationSeconds,
            HasDestination = hasDestination,
        });
    }

    /// <summary>Drop a task's timer. True when there was one (so the caller can log it once).</summary>
    public bool Cancel(string taskUuid)
        => !string.IsNullOrEmpty(taskUuid) && _pending.TryRemove(taskUuid, out _);

    /// <summary>
    /// A SUCCESSFUL completion arrived for this task (arrival evidence, a vendor success, a fan-out
    /// quorum). Before the end time it is HELD (the entry is marked and the timer reports it at the
    /// end time); after it, on an overdue task, the entry is removed and the caller reports
    /// TASKCMPLT now. Atomic against <see cref="Advance"/>: both decide under the entry's lock, so a
    /// completion racing the end time is reported exactly once, by one side or the other.
    /// </summary>
    public FinishVerdict MarkFinished(string taskUuid)
    {
        if (string.IsNullOrEmpty(taskUuid) || !_pending.TryGetValue(taskUuid, out var p))
            return FinishVerdict.NotTimed;
        lock (p)
        {
            if (!p.Overdue)
            {
                p.Finished = true;
                return FinishVerdict.Hold;
            }
        }
        // Overdue: whoever removes the entry acts on it (a concurrent terminal abort may win).
        return _pending.TryRemove(new KeyValuePair<string, Pending>(taskUuid, p))
            ? FinishVerdict.EmitNow : FinishVerdict.NotTimed;
    }

    /// <summary>
    /// THE INTERFACE ITSELF STOPPED THIS TASK'S MOVE (the ATTACK / BREACH engage fallback replaces
    /// the approach move with the engage after Vrf:EngageFallbackSeconds). The unit is then no
    /// longer travelling anywhere, and under RL-20260921-09 the only exception to "ends at start
    /// time + Duration" is a unit still travelling - so the task is treated as having NO
    /// destination from here on: before its end time it completes AT the end time (Hold); if it is
    /// already OVERDUE the entry is removed and the caller reports TASKCMPLT now (EmitNow). It is
    /// NOT marked Finished: its engage is still in flight, so a back-end loss aborts it through the
    /// in-flight set, not through <see cref="HeldAfterFinish"/>. No armed timer -> NotTimed.
    /// </summary>
    public FinishVerdict DropDestination(string taskUuid)
    {
        if (string.IsNullOrEmpty(taskUuid) || !_pending.TryGetValue(taskUuid, out var p))
            return FinishVerdict.NotTimed;
        lock (p)
        {
            if (!p.Overdue)
            {
                p.HasDestination = false;
                return FinishVerdict.Hold;
            }
        }
        return _pending.TryRemove(new KeyValuePair<string, Pending>(taskUuid, p))
            ? FinishVerdict.EmitNow : FinishVerdict.NotTimed;
    }

    /// <summary>
    /// The tasks whose unit has finished early and which are waiting only for their end time. They
    /// are no longer in flight, so a back-end loss that aborts the in-flight set would otherwise
    /// miss them and a dead back end would still get a TASKCMPLT at their end time.
    /// </summary>
    public IReadOnlyList<Pending> HeldAfterFinish()
    {
        var held = new List<Pending>();
        foreach (var kv in _pending)
            lock (kv.Value)
                if (kv.Value.Finished) held.Add(kv.Value);
        return held;
    }

    /// <summary>
    /// Walk every pending task forward to this clock reading and return the ones whose time is
    /// up. A CompleteNow entry is removed (so it fires exactly once); an OverdueAwaitingArrival
    /// entry is handed out once and stays. A non-finite reading judges nothing. See the class
    /// header for the pause / rollback / mode-change rules.
    /// </summary>
    public IReadOnlyList<Pending> Advance(double clockNow, bool usingSim)
    {
        if (!double.IsFinite(clockNow) || _pending.IsEmpty) return Array.Empty<Pending>();
        List<Pending> due = null;
        foreach (var kv in _pending)
        {
            var p = kv.Value;
            bool completeNow;
            lock (p)
            {
                if (double.IsNaN(p.LastClock) || p.UsingSim != usingSim)
                {
                    // First sighting, or a different time base: anchor only, serve nothing.
                    p.LastClock = clockNow;
                    p.UsingSim = usingSim;
                    continue;
                }
                double step = clockNow - p.LastClock;
                p.LastClock = clockNow;
                if (step > 0.0) p.Elapsed += step;      // a pause adds 0; a rollback adds 0
                if (p.Elapsed < p.DurationSeconds) continue;
                if (p.Overdue) continue;                // already said once; waiting for the unit
                completeNow = !p.HasDestination || p.Finished;
                if (!completeNow)
                {
                    p.Overdue = true;
                    p.Kind = DueKind.OverdueAwaitingArrival;
                    (due ??= new List<Pending>()).Add(p);
                    continue;
                }
                p.Kind = DueKind.CompleteNow;
            }
            // Remove-then-report, so a concurrent Cancel (a terminal abort on another thread at
            // the same instant) can only win once: whoever removes it acts on it.
            if (_pending.TryRemove(new KeyValuePair<string, Pending>(kv.Key, p)))
                (due ??= new List<Pending>()).Add(p);
        }
        return (IReadOnlyList<Pending>)due ?? Array.Empty<Pending>();
    }

    /// <summary>
    /// Does this TaskStatus END the task, and therefore cancel its timed end? TASKCMPLT does (the
    /// late arrival's report removes an overdue entry; every other TASKCMPLT has already left the
    /// table), and a TERMINAL TASKABRT does (refusal, skipped successor, supersede, vendor failure,
    /// back-end loss - the task is not running). TASKSTRT is the dispatch that ARMS the timer, and
    /// TASKINPRG is a progress note in the middle of an advance-then-engage task - neither ends
    /// anything.
    /// </summary>
    public static bool CancelsTimer(S.TaskStatusCodeType code)
        => code == S.TaskStatusCodeType.TASKCMPLT || code == S.TaskStatusCodeType.TASKABRT;

    /// <summary>
    /// The same, for an abort that is REPORT-ONLY: the progress watchdog's stall TASKABRT leaves
    /// the task in flight in VR-Forces, so it must NOT cancel the timer - a recovered unit that
    /// arrives before its end time is held to it like any other, and one that arrives late still
    /// reports complete.
    /// </summary>
    public static bool CancelsTimer(S.TaskStatusCodeType code, bool reportOnlyAbort)
        => !(reportOnlyAbort && code == S.TaskStatusCodeType.TASKABRT) && CancelsTimer(code);

    /// <summary>
    /// THE code a completion sends, given what <see cref="MarkFinished"/> said (NotTimed for a
    /// failure, which is never marked). Null = send nothing now.
    ///   - a FAILURE is TASKABRT, whatever the timer says;
    ///   - Hold: the move half of an advance-then-engage task still reports TASKINPRG (the engage
    ///     is issued); any other early finish sends nothing - the timer reports it at the end time;
    ///   - EmitNow: TASKCMPLT, INCLUDING for the late move half of an ATTACK / BREACH - its parked
    ///     engage is still issued, and the task is reported complete on that arrival (the owner's
    ///     answer of 2026-09-25, RL-20260925-01);
    ///   - NotTimed: today's evidence-only codes (TaskStatusPolicy.CodeForCompletion).
    /// </summary>
    public static S.TaskStatusCodeType? CompletionCode(bool success, bool taskContinues, FinishVerdict verdict)
        => !success ? S.TaskStatusCodeType.TASKABRT
         : verdict == FinishVerdict.Hold
             ? (taskContinues ? S.TaskStatusCodeType.TASKINPRG : (S.TaskStatusCodeType?)null)
         : verdict == FinishVerdict.EmitNow ? S.TaskStatusCodeType.TASKCMPLT
         : TaskStatusPolicy.CodeForCompletion(success, taskContinues);

    /// <summary>The progress watchdog's verdict on a unit at the moment the engage fallback fires,
    /// judged by the SAME criterion as its periodic check (StallPolicy: the calibrated window and
    /// Vrf:StallMoveMeters). Unknown = no verdict is possible (detection off, clock not usable, or
    /// the window not yet full).</summary>
    public enum StallAtFallback { Unknown, Moving, Stalled }

    /// <summary>What the engage fallback does.</summary>
    public enum EngageFallbackPlan
    {
        /// <summary>The watchdog has ALREADY reported this move stuck: it stays aborted (its
        /// follow-ons are already abandoned), the destination is kept, the engage is not issued.</summary>
        KeepStuck,
        /// <summary>Judged stalled at the fallback: report it stuck now (the stall path), keep the
        /// destination, do not issue the engage.</summary>
        ReportStuckNow,
        /// <summary>Judged moving, or no verdict possible: the interface stops a travelling unit -
        /// issue the engage (D4) and drop the destination (<see cref="DropDestination"/>).</summary>
        DropAndEngage,
        /// <summary>M3-1 (2026-09-25): the move is no longer the unit's current in-flight task - a
        /// newer task superseded it between the fallback timer and the tick that decides. Its engage
        /// is dead: nothing is issued, nothing is dropped, nothing is reported.</summary>
        Superseded,
    }

    /// <summary>The plan, with the M3-1 guard first: a move that is no longer the unit's current
    /// task gets no fallback action at all.</summary>
    public static EngageFallbackPlan PlanEngageFallback(bool moveIsCurrent, bool stallReportedForThisMove,
                                                        StallAtFallback verdict)
        => !moveIsCurrent ? EngageFallbackPlan.Superseded
         : PlanEngageFallback(stallReportedForThisMove, verdict);

    /// <summary>
    /// M3-2 (2026-09-25): THE STAYS-PUT TEST, for when the watchdog can give no verdict (Vrf:
    /// StallDetection off - the shipped default outside the demo profile - its window not yet full,
    /// or its clock unusable). The owner's caveat on STP-857: "you should not expect every task to
    /// require a movement, and abort in case the unit stays put" (RL-20260921-07). The criterion is
    /// the watchdog's own - StallPolicy.Decide at Vrf:StallMoveMeters and
    /// Vrf:StallMinMembersWithData, no new threshold - applied to each member's displacement SINCE
    /// DISPATCH instead of over the watchdog's window: no readable member moved that far = STAYED
    /// PUT (Stalled); any did = Moving; no or too few readable members = Unknown.
    /// </summary>
    public static StallAtFallback StaysPutVerdict(IReadOnlyList<double> displacementsSinceDispatch, int totalMembers,
                                                  double moveMeters, int minMembersWithData)
    {
        int withData = displacementsSinceDispatch?.Count ?? 0;
        if (withData == 0 || totalMembers <= 0 || withData < Math.Max(1, minMembersWithData))
            return StallAtFallback.Unknown;
        return StallPolicy.Decide(displacementsSinceDispatch, totalMembers, moveMeters, minMembersWithData).Stalled
            ? StallAtFallback.Stalled : StallAtFallback.Moving;
    }

    /// <summary>The watchdog's verdict wins; the stays-put test is consulted only when it has none.</summary>
    public static StallAtFallback CombineWithStaysPut(StallAtFallback watchdog, StallAtFallback staysPut)
        => watchdog != StallAtFallback.Unknown ? watchdog : staysPut;

    /// <summary>
    /// NEW-1 of the 2026-09-25 re-review. A STUCK unit is never completed through the engage
    /// fallback: under RL-20260921-09 the effect of a task is ignored, but being stuck is not a
    /// completion ("The notion that geting stuck midway is a complete is completelly illogical",
    /// RL-20260921-05; a never-arriving unit is a stuck unit, RL-20260921-09 S569). Only a unit
    /// that was still MOVING when the interface replaced its move - or one no verdict can be had
    /// on (the residual, stated where the service calls this) - has its destination dropped.
    /// </summary>
    public static EngageFallbackPlan PlanEngageFallback(bool stallReportedForThisMove, StallAtFallback verdict)
        => stallReportedForThisMove ? EngageFallbackPlan.KeepStuck
         : verdict == StallAtFallback.Stalled ? EngageFallbackPlan.ReportStuckNow
         : EngageFallbackPlan.DropAndEngage;

    /// <summary>Does this completion release the task's follow-ons NOW? Only a success that is
    /// not being held (a held one is released by the timer at the end time).</summary>
    public static bool ReleasesSuccessorsNow(bool success, FinishVerdict verdict)
        => success && verdict != FinishVerdict.Hold;
}
