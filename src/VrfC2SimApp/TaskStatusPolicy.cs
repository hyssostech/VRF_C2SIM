using System.Collections.Concurrent;
using S = C2SIM.Schema102;

namespace VrfC2SimApp;

/// <summary>
/// WHICH TaskStatus a task may still emit, and how many times (B1, 2026-09-14). Pure state
/// machine, one entry per task uuid - no bridge, no server, no clock.
///
/// The schema offers five codes (TaskStatusCodeType, C2SIM_SMX_LOX_CWIX2024.cs:11077-11093:
/// TASKABRT / TASKCMPLT / TASKINPRG / TASKPEND / TASKSTRT). Before B1 the interface emitted two:
/// TASKCMPLT on completion (C15) and TASKABRT from the progress watchdog (C16). It now also says
/// when a task STARTS, and says TASKABRT for the tasks it will never run at all:
///   - a task the interface REFUSES at dispatch ("NO LOCATION GIVEN - CAN'T EXECUTE TASK");
///   - a successor SKIPPED because its predecessor was abandoned ("policy=skip ... NOT dispatched");
///   - a task the SIM reports as failed (DtTaskCompleteReport::success()==false - see the service).
/// Supervisor ruling 2026-09-14: the user ruled TASKABRT for stalled units, and a task that is
/// refused or skipped is likewise not going to be executed. The user may override.
///
/// THE RULES, and why each exists:
///   - ONE TASKSTRT PER DISPATCH. A dispatch that is merely RE-ENTERED (the TerrainProfile path
///     runs ExecuteTaskOnTick twice for one task, once per terrain reply) must not announce a
///     second start; a genuine RE-TASK - a dispatch after the task has completed or aborted -
///     re-arms and does announce, because that is a new execution of the same C2SIM task.
///   - ONE TASKCMPLT PER TASK. The completion paths (vendor callback, R10 fan-out quorum,
///     straggler timer, arrival evidence) each have their own de-duplication; this is the backstop
///     that makes the guarantee a property of the REPORT stream rather than of four call sites.
///   - A TASKABRT NEVER SUPPRESSES A LATER TASKCMPLT (C16 ruling: a unit that reported a stall and
///     then arrives still reports completion) - but A TASKCMPLT DOES SUPPRESS A LATER TASKABRT,
///     because a task that is finished cannot subsequently fail.
///   - An EMPTY task uuid is never recorded and never suppressed: the interface emits those for
///     unattributed completions, and collapsing two of them would lose a report.
/// </summary>
public sealed class TaskStatusPolicy
{
    private sealed class TaskState
    {
        public bool Started;
        public bool Completed;
        public bool Aborted;
    }

    private readonly ConcurrentDictionary<string, TaskState> _byTask = new(StringComparer.Ordinal);

    /// <summary>TASKSTRT at dispatch: once per dispatch, re-armed by a completion or an abort.</summary>
    public bool ShouldEmitStart(string taskUuid)
    {
        if (string.IsNullOrEmpty(taskUuid)) return true;
        var st = _byTask.GetOrAdd(taskUuid, _ => new TaskState());
        lock (st)
        {
            if (st.Started && !st.Completed && !st.Aborted) return false;   // same dispatch, re-entered
            st.Started = true;
            st.Completed = false;
            st.Aborted = false;
            return true;
        }
    }

    /// <summary>TASKCMPLT: once per task; an earlier TASKABRT does not block it.</summary>
    public bool ShouldEmitComplete(string taskUuid)
    {
        if (string.IsNullOrEmpty(taskUuid)) return true;
        var st = _byTask.GetOrAdd(taskUuid, _ => new TaskState());
        lock (st)
        {
            if (st.Completed) return false;
            st.Completed = true;
            return true;
        }
    }

    /// <summary>TASKABRT: once per task, and never after that task's TASKCMPLT.</summary>
    public bool ShouldEmitAbort(string taskUuid)
    {
        if (string.IsNullOrEmpty(taskUuid)) return true;
        var st = _byTask.GetOrAdd(taskUuid, _ => new TaskState());
        lock (st)
        {
            if (st.Aborted || st.Completed) return false;
            st.Aborted = true;
            return true;
        }
    }

    /// <summary>The code a VR-Forces task-complete report carries: the vendor sets success=false to
    /// say "the task has FAILED and is no longer being processed" (vrftasks/taskCompleteReport.h
    /// :84-90), which is an abort, not a completion.</summary>
    public static S.TaskStatusCodeType CodeForCompletion(bool success)
        => success ? S.TaskStatusCodeType.TASKCMPLT : S.TaskStatusCodeType.TASKABRT;
}
