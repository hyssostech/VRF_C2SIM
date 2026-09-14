using System.Collections.Concurrent;
using S = C2SIM.Schema102;

namespace VrfC2SimApp;

/// <summary>
/// WHEN A TASK IS OVER BECAUSE ITS TIME IS UP (R4, user ruling 2026-09-14: "completion is given
/// by the end time"). Pure state machine - no clock is read here, no bridge, no report - so the
/// whole rule is decidable offline (`--rulings-selftest`).
///
/// WHY IT EXISTS. Most of COA-STP1 is hold-type work: SECURE / OCCUPY / DEFEND / RETAIN / BLOCK /
/// FIX / SCREEN / GUARD, plus fires and air defence. Doctrine gives those tasks NO state a
/// simulation can evaluate (FM 3-90 App. B; DOCTRINE_FOR_TASKING_RULINGS_2026-09-14 sec 4, groups
/// B and C) - they end when the order says they end, or when a higher commander ends them. The
/// order says: every one of the 42 tasks carries a Duration (32 x PT1H20M, 10 x PT2H), and the
/// end time is dispatch + Duration. Without this, a hold task never completed, so its STREND
/// successors sat at the gate until TaskPredecessorTimeoutSeconds and were then SKIPPED - which
/// is how run G6 executed 9 of 42 tasks and completed none.
///
/// THE RULE, and why each part of it:
///   - ONE TIMER PER TASK, ARMED AT DISPATCH. Register is first-dispatch-wins: a re-entered
///     dispatch (the TerrainProfile second pass) must not restart the clock.
///   - THE TIMER MEASURES ELAPSED CLOCK, NOT A DEADLINE STAMP. Each Advance adds the clock's
///     FORWARD movement since the previous one. That is what makes the policy correct on the
///     simulation clock, which is the clock this interface prefers to measure on: a PAUSED
///     scenario stops the clock dead (no ageing), and DtVrfRemoteController::rollbackToSnapshot
///     steps it BACKWARDS (vrfRemoteController.h:605) - a deadline stamp would fire the moment a
///     rollback happened to land past it, and would treat a pause as time served.
///   - A CLOCK-MODE CHANGE RE-ANCHORS. The sim reading can disappear mid-run (VrfFacade::
///     SimTimeSeconds returns -1.0 with no back end reporting), and the interface then falls back
///     to the wall clock - a time base whose numbers have nothing to do with the previous ones.
///     The first Advance in a new mode therefore contributes ZERO and only re-anchors; the task
///     keeps the time it has already served and serves the rest on the new clock.
///   - THE TIMER IS CANCELLED BY ANY REAL END (see <see cref="CancelsTimer"/>): an arrival-evidence
///     or vendor TASKCMPLT wins - an evaluable verb may finish early and must not be reported
///     twice - and a TASKABRT (refusal, skipped successor, stall watchdog, vendor success=false)
///     means the task is not running at all, so there is nothing left to time out.
///   - A TASK WITH NO DURATION GETS NO TIMER. It completes on its own evidence or not at all;
///     inventing an end time for it would be manufacturing a decision.
///
/// The emission itself is NOT here: the due entry is handed to the service, which pushes exactly
/// one TASKCMPLT through the single emit point (PushTaskStatus -> TaskStatusPolicy) and releases
/// the STREND gate (TaskSequencer.CompleteTask) - the same two things every other completion does.
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
    }

    private readonly ConcurrentDictionary<string, Pending> _pending = new(StringComparer.Ordinal);

    /// <summary>How many tasks are waiting for their end time (for the status line).</summary>
    public int Count => _pending.Count;

    /// <summary>
    /// Arm the timer for a dispatched task. FIRST DISPATCH WINS: a re-entered dispatch returns
    /// false and leaves the running timer alone. A non-positive or non-finite duration arms
    /// nothing (returns false) - that task has no end time.
    /// </summary>
    public bool Register(string taskUuid, string taskeeUuid, string taskName, string unitName,
                         double durationSeconds)
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
        });
    }

    /// <summary>Drop a task's timer. True when there was one (so the caller can log it once).</summary>
    public bool Cancel(string taskUuid)
        => !string.IsNullOrEmpty(taskUuid) && _pending.TryRemove(taskUuid, out _);

    /// <summary>
    /// Walk every pending task forward to this clock reading and return the ones whose time is
    /// up (removed from the set, so each fires exactly once). A non-finite reading judges
    /// nothing. See the class header for the pause / rollback / mode-change rules.
    /// </summary>
    public IReadOnlyList<Pending> Advance(double clockNow, bool usingSim)
    {
        if (!double.IsFinite(clockNow) || _pending.IsEmpty) return Array.Empty<Pending>();
        List<Pending> due = null;
        foreach (var kv in _pending)
        {
            var p = kv.Value;
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
            }
            // Remove-then-report, so a concurrent Cancel (an arrival completing on another
            // thread at the same instant) can only win once: whoever removes it acts on it.
            if (_pending.TryRemove(new KeyValuePair<string, Pending>(kv.Key, p)))
                (due ??= new List<Pending>()).Add(p);
        }
        return (IReadOnlyList<Pending>)due ?? Array.Empty<Pending>();
    }

    /// <summary>
    /// Does this TaskStatus END the task, and therefore cancel its timed end? TASKCMPLT (the task
    /// finished on its own evidence - arrival, or the vendor's own completion) and TASKABRT (it is
    /// not going to run at all) do. TASKSTRT is the dispatch that ARMS the timer, and TASKINPRG is
    /// a progress note in the middle of an advance-then-engage task - neither ends anything.
    /// </summary>
    public static bool CancelsTimer(S.TaskStatusCodeType code)
        => code == S.TaskStatusCodeType.TASKCMPLT || code == S.TaskStatusCodeType.TASKABRT;
}
