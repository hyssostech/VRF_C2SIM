using Microsoft.Extensions.Configuration;
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
///   V4b "what those points MEAN, per verb" - TaskGeometryInterpretation: a bare C2SIM Location
///      list read as a Route, an ObjectiveArea or a Point, and the census of the real 42-task
///      order it produces.
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
        Console.WriteLine("=== RL-20260921-09: completion on the temporary position (start time + Duration) ===");
        TemporaryPosition(ref failures);
        Console.WriteLine("=== R2: a task without geometry uses the performing unit's position ===");
        R2(ref failures);
        Console.WriteLine("=== R3: the target IS the objective ===");
        R3(ref failures);
        Console.WriteLine("=== R1 (transition): MapGraphicID -> the graphic created at init ===");
        R1(ref failures);
        Console.WriteLine("=== V4b: what the embedded Location's points MEAN, per verb ===");
        V4b(ref failures);
        Console.WriteLine("=== The TASK CLOCK: an unsteady or frozen sim reader must not stop the order ===");
        TaskClockChecks(ref failures);
        Console.WriteLine("=== The STREND CHAIN: a gate is a GRAPH, and its predecessor has a lead time ===");
        ChainTopology(ref failures);
        Console.WriteLine("=== C14 + STP-837 as amended by the user ruling of 2026-09-21 (the SHIPPED defaults) ===");
        EchelonAndTraversalRulings(ref failures);
        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    // --------------------------------------------------------------- V4b ----
    /// <summary>
    /// V4b - what the embedded Location's points MEAN, per verb. The rule is
    /// <see cref="TaskGeometryInterpretation"/>; these checks are the rule's evidence, and the
    /// census half of them is measured against `data/COA-STP1_Order.xml` on disk rather than
    /// against a fixture, so the order the port exists for cannot drift away from the reading
    /// without a check failing.
    /// </summary>
    private static void V4b(ref int failures)
    {
        // A SQUARE ring around 34.5 / -116.5, authored the way the init authors its 12 multi-vertex
        // tactical areas: the first vertex repeated as the last (measured: 0 m on all 12).
        var ring = new List<(double Lat, double Lon, double? Elev)>
        {
            (34.4, -116.6, null), (34.6, -116.6, null), (34.6, -116.4, null), (34.4, -116.4, null),
            (34.4, -116.6, null),
        };
        // T32's REAL points, copied from data/COA-STP1_Order.xml: a SEIZE - an area verb - whose
        // four points are a 23.6 km axis of advance. This is the case that falsifies a verb-only
        // rule, so it is a fixture in its own right.
        var seizeAxis = new List<(double Lat, double Lon, double? Elev)>
        {
            (34.679985, -116.724799, null), (34.620600, -116.696722, null),
            (34.524809, -116.641791, null), (34.488408, -116.614922, null),
        };

        // (f1) A CLOSED RING under an area verb is an OBJECTIVE AREA: the move goes to the centroid,
        //      the ring's corners are what the control area is made of, and the closing duplicate is
        //      NOT counted twice.
        {
            var r = TaskGeometryInterpretation.Interpret("SEIZE", ring, GeometrySource.EmbeddedLocation);
            bool centroid = r.Points.Count == 1
                            && Math.Abs(r.Points[0].Lat - 34.5) < 1e-9
                            && Math.Abs(r.Points[0].Lon + 116.5) < 1e-9;
            Check(ref failures, r.Kind == GeometryKind.ObjectiveArea && centroid,
                  $"(f1) a CLOSED RING under SEIZE reads as an ObjectiveArea and moves to its centroid " +
                  $"(kind {r.Kind}, {r.Points.Count} point(s))");
            Check(ref failures, r.AreaVertices.Count == 4,
                  $"... the area is built from the 4 distinct corners, not 5 (the closing vertex is dropped " +
                  $"before the centroid, got {r.AreaVertices.Count})");
            Check(ref failures, r.CreateObjectiveArea,
                  "... and it IS created on the fly, because the ring came from the embedded Location");
            Check(ref failures, r.Log.StartsWith("embedded Location read as ObjectiveArea (5 points, verb SEIZE)",
                                                 StringComparison.Ordinal),
                  $"... and the line says so verbatim: \"{r.Log.Split(" - ")[0]}\"");
        }

        // (f2) THE AMBIGUITY RULE, STATED: an ATTACK with a closed ring is an OBJECTIVE, not a lap of
        //      the perimeter. Doctrine: an attack's minimum control measures are an LD, a time and
        //      "the objective" (FM 3-90 5-8). The ring branch is verb-INDEPENDENT.
        {
            var attack = TaskGeometryInterpretation.Interpret("ATTACK", ring, GeometrySource.EmbeddedLocation);
            var move = TaskGeometryInterpretation.Interpret("MOVE", ring, GeometrySource.EmbeddedLocation);
            Check(ref failures, attack.Kind == GeometryKind.ObjectiveArea && attack.Points.Count == 1,
                  $"(f2) an ATTACK with a CLOSED RING is an ObjectiveArea, not a route (kind {attack.Kind})");
            Check(ref failures, move.Kind == GeometryKind.ObjectiveArea && move.Points.Count == 1
                             && Math.Abs(move.Points[0].Lat - 34.5) < 1e-9,
                  $"... and a MOVE to a ring goes to the CENTROID, it does not drive the ring (kind {move.Kind}, " +
                  $"{move.Points.Count} point(s))");
        }

        // (f3) THE MEASUREMENT THAT FORCED THE SHAPE TEST: T32's real points are a SEIZE (area verb)
        //      on a 23.6 km axis. Closure ratio 0.998 - it never turns back - so it stays a ROUTE and
        //      the battalion keeps driving it.
        {
            var r = TaskGeometryInterpretation.Interpret("SEIZE", seizeAxis, GeometrySource.EmbeddedLocation);
            double ratio = TaskGeometryInterpretation.ClosureRatio(seizeAxis);
            Check(ref failures, r.Kind == GeometryKind.Route && r.Points.Count == 4 && !r.CreateObjectiveArea,
                  $"(f3) T32's real SEIZE axis (4 points, closure ratio {ratio:F3}) stays a Route - a verb-only " +
                  $"rule would have collapsed 23.6 km to a centroid (kind {r.Kind})");
            Check(ref failures, ratio > TaskGeometryInterpretation.RingClosureRatio,
                  $"... because its closure ratio {ratio:F3} is above the {TaskGeometryInterpretation.RingClosureRatio:F2} " +
                  "ring threshold");
            Check(ref failures, r.Note != null && r.Note.Contains("AREA verb") && r.Note.Contains("STP-801"),
                  "... and the disagreement between the verb and the shape is REPORTED, not swallowed");
        }

        // (f4) AN OPEN LINE UNDER MOVE IS A ROUTE, unchanged, point for point. This is the whole
        //      COA-STP1 movement path and the golden-trace fixtures: V4b must not touch it.
        {
            var line = new List<(double Lat, double Lon, double? Elev)>
                { (34.0, -116.0, null), (34.1, -116.1, 100.0), (34.2, -116.2, null) };
            var r = TaskGeometryInterpretation.Interpret("MOVE", line, GeometrySource.EmbeddedLocation);
            Check(ref failures, r.Kind == GeometryKind.Route && r.Points.Count == 3
                             && Math.Abs(r.Points[1].Lat - 34.1) < 1e-9 && r.Points[1].Elev == 100.0
                             && !r.CreateObjectiveArea && r.Note == null,
                  $"(f4) an OPEN LINE under MOVE is a Route, unchanged, altitudes included (kind {r.Kind}, " +
                  $"{r.Points.Count} point(s))");
        }

        // (f5) ONE POINT is a Point; and so are TWO IDENTICAL points, which is what COA-STP1's T2 and
        //      T3 actually export (measured: 0 m apart).
        {
            var one = new List<(double Lat, double Lon, double? Elev)> { (34.488408, -116.614922, null) };
            var twice = new List<(double Lat, double Lon, double? Elev)>
                { (34.488408, -116.614922, null), (34.488408, -116.614922, null) };
            var r1 = TaskGeometryInterpretation.Interpret("OCCUPY", one, GeometrySource.EmbeddedLocation);
            var r2 = TaskGeometryInterpretation.Interpret("FIX", twice, GeometrySource.EmbeddedLocation);
            Check(ref failures, r1.Kind == GeometryKind.Point && r1.Points.Count == 1 && !r1.CreateObjectiveArea,
                  $"(f5) a ONE-POINT task is a Point objective, and no area is invented for it (kind {r1.Kind})");
            Check(ref failures, r2.Kind == GeometryKind.Point && r2.Points.Count == 1,
                  $"... T2/T3's TWO IDENTICAL points collapse to ONE place (kind {r2.Kind}, {r2.Points.Count} point(s))");
            Check(ref failures, r2.Log.StartsWith("embedded Location read as Point (2 points, verb FIX)",
                                                  StringComparison.Ordinal),
                  "... and the line still reports the 2 points the order carried");
        }

        // (f6) A DEGENERATE "ring" - out and back along one leg - is NOT an area. Three points with
        //      the first repeated as the last are TWO corners, and createControlArea cannot make a
        //      polygon of two vertices.
        {
            var outAndBack = new List<(double Lat, double Lon, double? Elev)>
                { (34.0, -116.0, null), (34.1, -116.1, null), (34.0, -116.0, null) };
            var r = TaskGeometryInterpretation.Interpret("SECURE", outAndBack, GeometrySource.EmbeddedLocation);
            Check(ref failures, r.Kind == GeometryKind.Route && !r.CreateObjectiveArea,
                  $"(f6) an out-and-back 3-point list is NOT an objective area - two corners are not a polygon " +
                  $"(kind {r.Kind})");
            Check(ref failures, r.Note != null && r.Note.Contains("distinct corners")
                             && !r.Note.Contains("do not close"),
                  "... and the note says WHY (two corners), not the false claim that the figure does not close");
        }

        // (f10) NO GEOMETRY AT ALL is not a reading of anything. Caught reviewing this change: the
        //       MapGraphicID passthrough used to swallow GeometrySource.None and log a sentence about
        //       a uuid that is not there - on the NINE COA-STP1 tasks that carry neither.
        {
            var r = TaskGeometryInterpretation.Interpret("DEFEND", new List<(double, double, double?)>(),
                                                         GeometrySource.None);
            Check(ref failures, r.Kind == GeometryKind.None && r.Points.Count == 0 && !r.CreateObjectiveArea
                             && r.Log.Contains("no task geometry to read") && !r.Log.Contains("MapGraphicID"),
                  $"(f10) a task with NO geometry reads as None and the line says so without inventing a " +
                  $"MapGraphicID (kind {r.Kind})");
        }

        // (f7) MapGraphicID PRECEDENCE. When the order NAMES its graphic, the init already created it
        //      under that uuid: the resolver's first branch wins, the points pass through as the
        //      graphic authored them, and V4b creates NOTHING.
        {
            const string objMadison = "11111111-2222-3333-4444-555555555555";
            var graphics = new Dictionary<string, TaskGraphic>(StringComparer.Ordinal)
            {
                [objMadison] = new TaskGraphic(objMadison, "OBJ_MADISON", TaskGraphic.KindArea,
                    new[] { (34.4, -116.6, (double?)null), (34.6, -116.6, (double?)null),
                            (34.6, -116.4, (double?)null), (34.4, -116.4, (double?)null) }),
            };
            var task = new OrderTask
            {
                TaskName = "T_Seize_Madison",
                TaskUuid = "task-uuid-1",
                ActionCode = "SEIZE",
                MapGraphicUuids = new[] { objMadison },
                Points = new List<(double, double, double?)>(ring),
            };
            var resolved = TaskGeometryResolver.Resolve(task, graphics);
            var r = TaskGeometryInterpretation.Interpret(task.ActionCode, resolved.Points, resolved.Source);
            Check(ref failures, resolved.Source == GeometrySource.MapGraphic && !r.CreateObjectiveArea,
                  $"(f7) with a MapGraphicID that resolves, NO on-the-fly area is created - the init made that " +
                  $"object (source {resolved.Source}, create {r.CreateObjectiveArea})");
            Check(ref failures, r.Points.Count == 1 && Math.Abs(r.Points[0].Lat - 34.5) < 1e-9,
                  "... and the points are the graphic's own centroid, not a re-reading of the embedded ring");
            Check(ref failures, r.Log.Contains("MapGraphicID used as the graphic authored it"),
                  "... and the line says the interpretation did NOT apply");
        }

        // (f8) EXACTLY ONCE PER TASK. ExecuteTaskOnTick is re-entered for the SAME task by the
        //      TerrainProfile reply - the DEFAULT ground path - and an order can be delivered twice.
        //      This is the production decision function, driven on a real dictionary.
        {
            var created = new System.Collections.Concurrent.ConcurrentDictionary<string, byte>();
            bool first = TaskGeometryInterpretation.ShouldCreateObjectiveArea(created, "task-uuid-1", "T32_Seize");
            bool terrainReentry = TaskGeometryInterpretation.ShouldCreateObjectiveArea(created, "task-uuid-1", "T32_Seize");
            bool secondDelivery = TaskGeometryInterpretation.ShouldCreateObjectiveArea(created, "task-uuid-1", "T32_Seize");
            bool otherTask = TaskGeometryInterpretation.ShouldCreateObjectiveArea(created, "task-uuid-2", "T33_Secure");
            Check(ref failures, first && !terrainReentry && !secondDelivery,
                  $"(f8) the objective area is created ONCE per task - the terrain re-entry and a duplicate " +
                  $"delivery create nothing (first {first}, re-entry {terrainReentry}, redelivery {secondDelivery})");
            Check(ref failures, otherTask && created.Count == 2,
                  $"... and a DIFFERENT task still gets its own ({created.Count} key(s))");
            Check(ref failures, TaskGeometryInterpretation.ObjectiveAreaName("T32_Seize") == "T32_Seize OBJECTIVE"
                             && TaskGeometryInterpretation.ObjectiveAreaKey("task-uuid-1", "T32_Seize")
                                == "taskarea:task-uuid-1",
                  "... under the '<TaskName> OBJECTIVE' name and the task's own uuid");
        }

        // (f9) THE CENSUS, measured on the real order rather than asserted in a comment. The counts
        //      are the ones this pass measured independently (scratchpad census.py): 9 with no
        //      geometry, 22 one place, 11 routes, and - because STP linearises the FIRST task graphic
        //      and that graphic is an axis, a task symbol or a point - NOT ONE ring.
        {
            string file = FindCoaStp1Order();
            var order = file == null ? null : OrderParser.Parse(File.ReadAllText(file));
            Check(ref failures, order != null && order.Tasks.Count == 42,
                  $"(f9) data/COA-STP1_Order.xml still parses to 42 tasks " +
                  $"(got {(order == null ? "NOT FOUND" : order.Tasks.Count.ToString())})");
            if (order != null && order.Tasks.Count == 42)
            {
                var kinds = order.Tasks
                    .Select(t => TaskGeometryInterpretation.Classify(t.ActionCode, t.Points)).ToList();
                int none = kinds.Count(k => k == GeometryKind.None);
                int point = kinds.Count(k => k == GeometryKind.Point);
                int route = kinds.Count(k => k == GeometryKind.Route);
                int area = kinds.Count(k => k == GeometryKind.ObjectiveArea);
                Check(ref failures, none == 9 && point == 22 && route == 11 && area == 0,
                      $"... the 42 tasks read as 9 None / 22 Point / 11 Route / 0 ObjectiveArea " +
                      $"(got {none}/{point}/{route}/{area})");
                Check(ref failures, none == order.Tasks.Count(t => t.Points.Count == 0),
                      "... every None is a task with no points at all (R2's nine), and nothing else");

                // The two 3-point tasks are TASK-MISSION SYMBOLS: their point lists match the init's
                // TaskGraphics __FRIEN_16 / __FRIEN_13 vertex for vertex, and FM 3-90 B-8 / B-17 say
                // what those symbols are. They are driven as routes - and SAID to be symbols.
                var symbols = order.Tasks.Where(t => t.Points.Count == 3).ToList();
                var readings = symbols
                    .Select(t => TaskGeometryInterpretation.Interpret(t.ActionCode, t.Points, GeometrySource.EmbeddedLocation))
                    .ToList();
                Check(ref failures, symbols.Count == 2
                                 && readings.All(r => r.Kind == GeometryKind.Route)
                                 && readings.All(r => r.Note != null && r.Note.Contains("task-mission SYMBOL")),
                      $"... the 2 three-point tasks (T13 BREACH, T36 CLRLND) are driven as routes and REPORTED " +
                      $"as task-mission symbols ({symbols.Count} found)");
                Check(ref failures, symbols.All(t => TaskGeometryInterpretation.Spread(t.Points) < 400.0),
                      "... which is the measurement behind that note: both span under 400 m");

                // And the nine axes: 4 points each, closure ratio at the far end of the scale.
                var axes = order.Tasks.Where(t => t.Points.Count == 4).ToList();
                Check(ref failures, axes.Count == 9
                                 && axes.All(t => TaskGeometryInterpretation.ClosureRatio(t.Points) > 0.9)
                                 && axes.All(t => TaskGeometryInterpretation.Classify(t.ActionCode, t.Points)
                                                  == GeometryKind.Route),
                      $"... and all {axes.Count} four-point tasks are axes of advance (closure ratio > 0.9), " +
                      "read as routes");
            }
        }
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

        // (b2) REWRITTEN 2026-09-25 for RL-20260921-09 (the owner's temporary position: completion
        //      is start time + Duration). It used to assert that an EARLIER arrival cancels the
        //      timer and completes the task at once; under the temporary position an early finish
        //      is HELD to the end time. What still cancels: the TASKCMPLT a LATE arrival sends,
        //      which removes the overdue entry so it can never fire a second one.
        {
            var p = new TimedCompletionPolicy();
            p.Register("T2", "taskee-2", "T2_Move", "1-35 AR", dur, hasDestination: true);
            p.Advance(0.0, usingSim: false);
            p.Advance(100.0, usingSim: false);
            Check(ref failures, p.MarkFinished("T2") == TimedCompletionPolicy.FinishVerdict.Hold && p.Count == 1,
                  "an EARLIER arrival is HELD to the end time - the timer stays armed (RL-20260921-09)");
            Check(ref failures, p.Advance(dur - 1.0, usingSim: false).Count == 0,
                  "... nothing is due before the end time");
            var dueB2 = p.Advance(dur, usingSim: false);
            Check(ref failures, dueB2.Count == 1 && dueB2[0].Kind == TimedCompletionPolicy.DueKind.CompleteNow,
                  "... and at the end time it completes, exactly once");
            Check(ref failures, TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKCMPLT),
                  "a TASKCMPLT still cancels the timed end (the late arrival's report removes the overdue entry)");
            Check(ref failures, p.Advance(100.0 + dur * 2, usingSim: false).Count == 0,
                  "a consumed timer never fires a second TASKCMPLT");
        }

        // (b3) A TERMINAL TASKABRT (refusal, skipped successor, supersede, vendor failure, back-end
        //      loss) cancels it. The progress watchdog's REPORT-ONLY stall abort does NOT (the
        //      completion unit's scope, approved 2026-09-25, RL-20260925-01): a stuck unit that recovers and arrives must not complete
        //      early, and one that arrives late still reports complete.
        {
            var p = new TimedCompletionPolicy();
            p.Register("T3", "taskee-3", "T3_Attack", "1-6 IN", dur);
            p.Advance(0.0, usingSim: false);
            Check(ref failures, TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKABRT),
                  "a TASKABRT cancels the timed end (the task is not going to run)");
            Check(ref failures, !TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKABRT, reportOnlyAbort: true)
                             && TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKABRT, reportOnlyAbort: false),
                  "the stall watchdog's REPORT-ONLY TASKABRT does NOT cancel it; a terminal one does");
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
            seq.NotifyDispatched("PRED", TaskClock.Wall.Now());
            var successor = seq.WaitForStartAsync("PRED", 0, 0, 5.0, TaskClock.Wall,
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

        // (b8) M1 - THE GATE MUST OUTLIVE THE END TIME IT WAITS FOR. This is the one check in this
        //      file that runs the REAL TaskSequencer and the REAL TimedCompletionPolicy against a
        //      common clock, because the defect it covers is an interaction between the two and
        //      not a property of either: the gate expired at dispatch + configured timeout while
        //      the completion fires at dispatch + Duration, so on COA-STP1 (600 s configured,
        //      4,800 s Duration) all 31 gated tasks were SKIPPED with TASKABRT.
        {
            const double predDuration = 4800.0;   // COA-STP1's PT1H20M, in seconds
            const double configured = 600.0;      // the shipped Vrf:TaskPredecessorTimeoutSeconds
            const double margin = 60.0;           // the shipped Vrf:TaskPredecessorEndMarginSeconds

            double preFixWindow = configured;     // what the branch used before M1
            double derivedWindow = TaskDispatchPolicy.PredecessorTimeoutSeconds(
                configured, predDuration, margin);
            Check(ref failures, preFixWindow < predDuration && derivedWindow >= predDuration + margin,
                  $"the PRE-FIX window ({preFixWindow:F0} s) is shorter than the predecessor's end time " +
                  $"({predDuration:F0} s); the derived one ({derivedWindow:F0} s) is not");

            // FAIL-FIRST CONTROL: the pre-fix window, on the same clock, still SKIPS the successor.
            Check(ref failures, GateOutcome(preFixWindow, predDuration) == GateResult.PredecessorTimeout,
                  "FAIL-FIRST: with the flat configured window the successor times out while its " +
                  "predecessor is still running (the behaviour M1 replaces)");

            // FIXED: the derived window outlives the end time, and the timed completion releases it.
            Check(ref failures, GateOutcome(derivedWindow, predDuration) == GateResult.Proceed,
                  "with the window derived from the predecessor's ARMED END TIME the successor " +
                  "dispatches AT the timed completion, and is not skipped");

            // A predecessor with NO Duration leaves the configured floor exactly as it was.
            Check(ref failures,
                  TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, 0.0, margin) == configured
                  && TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, double.NaN, margin) == configured,
                  "a predecessor with no armed end time leaves Vrf:TaskPredecessorTimeoutSeconds alone");

            // A COMPRESSED demo must keep the relation: the gate still outlives the scaled end
            // time, because both sides are computed from the SAME scaled number.
            double demoEnd = TaskDispatchPolicy.ScaleOrderMs(4800000L, 0.01) / 1000.0;
            Check(ref failures,
                  TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, demoEnd, margin) == configured
                  && demoEnd < configured,
                  $"a compressed demo (scale 0.01 -> {demoEnd:F0} s) stays under the configured floor, so the " +
                  $"floor is what applies - the relation holds at both ends of the scale");

            // A NEGATIVE or absurd margin cannot shorten the gate below the predecessor's end time.
            // E5 (pass-3 review): the old wording here said a negative margin is "treated as zero -
            // never as a reason to expire early", which reads as a guarantee that ZERO is safe. It
            // is not, and the three checks now say which half is which: the clamp is a floor
            // against nonsense, and zero is refused at start-up instead of being blessed.
            Check(ref failures,
                  TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, predDuration, -500.0) == predDuration
                  && TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, predDuration, double.NaN) == predDuration,
                  "a negative or non-finite margin is CLAMPED TO ZERO, so the derived window is never SHORTER " +
                  "than the predecessor's end time");
            Check(ref failures,
                  !TaskDispatchPolicy.IsUsablePredecessorEndMargin(0.0)
                  && !TaskDispatchPolicy.IsUsablePredecessorEndMargin(-1.0)
                  && !TaskDispatchPolicy.IsUsablePredecessorEndMargin(double.NaN)
                  && !TaskDispatchPolicy.IsUsablePredecessorEndMargin(double.PositiveInfinity)
                  && TaskDispatchPolicy.IsUsablePredecessorEndMargin(1.0)
                  && TaskDispatchPolicy.IsUsablePredecessorEndMargin(
                         TaskDispatchPolicy.DefaultPredecessorEndMarginSeconds),
                  "(E5) ... but ZERO is NOT a usable margin, and Vrf:TaskPredecessorEndMarginSeconds is refused " +
                  "at start-up when it is not greater than zero - the run then proceeds at the shipped 60 s");
            Check(ref failures,
                  TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, predDuration, 0.0) == predDuration
                  && TaskDispatchPolicy.PredecessorTimeoutSeconds(
                         configured, predDuration, TaskDispatchPolicy.DefaultPredecessorEndMarginSeconds)
                     == predDuration + TaskDispatchPolicy.DefaultPredecessorEndMarginSeconds,
                  $"(E5) ... and the difference is the whole point: at margin 0 the gate expires at " +
                  $"{predDuration:F0} s, EXACTLY when the predecessor is due to complete - and the completion " +
                  $"is observed up to ~3 x (sim ratio) s late, so the two race. At the shipped 60 s it outlives it");
        }

        // (b9) Vrf:DurationScale ARITHMETIC (review item 8). One function scales both halves of the
        //      order's clock; if it ever disagreed with itself, M1's relation would break silently.
        Check(ref failures,
              TaskDispatchPolicy.ScaleOrderMs(4800000L, 1.0) == 4800000L
              && TaskDispatchPolicy.ScaleOrderMs(4800000L, 0.01) == 48000L
              && TaskDispatchPolicy.ScaleOrderMs(7200000L, 0.5) == 3600000L,
              "Vrf:DurationScale scales an authored time as written (1.0, 0.01, 0.5)");
        Check(ref failures,
              TaskDispatchPolicy.ScaleOrderMs(0L, 0.5) == 0L
              && TaskDispatchPolicy.ScaleOrderMs(-5L, 1.0) == 0L,
              "a zero or negative authored time is not a time: 0 in, 0 out, at any scale");

        // (b10) ... AND ITS BOUNDS (m8). Zero, negative, NaN and the infinities are configuration
        //       errors, not instructions - one scale must not mean "no end time" on one half of the
        //       order's clock and "dispatch now" on the other.
        Check(ref failures,
              !TaskDispatchPolicy.IsUsableDurationScale(0.0)
              && !TaskDispatchPolicy.IsUsableDurationScale(-1.0)
              && !TaskDispatchPolicy.IsUsableDurationScale(double.NaN)
              && !TaskDispatchPolicy.IsUsableDurationScale(double.PositiveInfinity)
              && !TaskDispatchPolicy.IsUsableDurationScale(double.NegativeInfinity)
              && TaskDispatchPolicy.IsUsableDurationScale(1.0)
              && TaskDispatchPolicy.IsUsableDurationScale(0.001),
              "Vrf:DurationScale must be finite and greater than zero (0, negative, NaN, +/-Inf are refused)");
        Check(ref failures,
              TaskDispatchPolicy.ScaleOrderMs(4800000L, 0.0) == 4800000L
              && TaskDispatchPolicy.ScaleOrderMs(4800000L, double.NaN) == 4800000L,
              "... and a refused scale leaves the authored time UNCHANGED - the order as written, " +
              "never a task that ends at once");

        // (b11) THE ORDER'S DURATION FORMATS (review item 8). findTotalIsoMs is a strict
        //       fixed-shape decoder, and R4 now decides completion on its output, so what it
        //       ACCEPTS and what it REFUSES both matter.
        Check(ref failures, OrderParser.FindTotalIsoMs("P00Y00M00DT01H20M00S") == 4800000L
                         && OrderParser.FindTotalIsoMs("P00Y00M00DT02H00M00S") == 7200000L,
              "the two COA-STP1 Durations decode to 4,800 s and 7,200 s");
        Check(ref failures, OrderParser.FindTotalIsoMs("P00Y01M00DT00H00M00S") == 2592000000L,
              $"m4: ONE MONTH is 30 DAYS ({OrderParser.FindTotalIsoMs("P00Y01M00DT00H00M00S") / 3600000.0:F0} h), " +
              "not the C++'s 30 hours - R4 decides completion on this number now");
        Check(ref failures, OrderParser.FindTotalIsoMs("P00Y00M01DT00H00M00S") == 86400000L
                         && OrderParser.FindTotalIsoMs("P01Y00M00DT00H00M00S") == 31536000000L,
              "the day and year terms are unchanged (nominal 24 h and 365 d)");
        Check(ref failures, OrderParser.FindTotalIsoMs("PT1H20M") == -1L,
              "the SHORT ISO-8601 form PT1H20M is REFUSED (-1), not silently read as zero");
        Check(ref failures, OrderParser.FindTotalIsoMs("P00Y00M00D01H20M00S") == -1L,
              "a Duration with no 'T' separator is REFUSED (-1)");
        Check(ref failures, OrderParser.FindTotalIsoMs("P00Y00M00DT01H20M00.5S") == -1L,
              "FRACTIONAL seconds are REFUSED (-1) rather than truncated");
        Check(ref failures, OrderParser.FindTotalIsoMs(null) == -1L
                         && OrderParser.FindTotalIsoMs("") == -1L
                         && OrderParser.FindTotalIsoMs("1H20M") == -1L,
              "null, empty and a string that does not start with 'P' are REFUSED (-1)");
        Check(ref failures, OrderParser.FindTotalIsoMs("P00Y00M00DT-01H00M00S") < 0L,
              "a NEGATIVE term yields a negative total, which the parser clamps and warns about");

        // (b12) ... and the PARSER's contract on top of it: an unreadable Duration must not pass
        //       as "no Duration", because those two have different consequences (a warning and no
        //       end time, vs a task that is supposed to have one).
        {
            var bad = OrderParser.Parse(TimedOrderXml(duration: "PT1H20M", startIso: null));
            Check(ref failures, bad.Tasks.Count == 1 && bad.Tasks[0].DurationMs == 0
                             && bad.Warnings.Any(w => w.Contains("PT1H20M")),
                  $"a Duration that is PRESENT but unreadable gives DurationMs=0 AND a warning naming it " +
                  $"({bad.Warnings.Count} warning(s))");
            var good = OrderParser.Parse(TimedOrderXml(duration: "P00Y00M00DT01H20M00S", startIso: null));
            Check(ref failures, good.Tasks[0].DurationMs == 4800000L && good.Warnings.Count == 0,
                  "... and a readable one gives the authored milliseconds with no warning");
        }

        // (b13) THE ABSOLUTE StartTime (review item 8). STP exports the SimulationTime delay form,
        //       so this path has never run on a real order - which is exactly why it is checked
        //       here: an order from another producer that DATES its tasks must not be dispatched
        //       immediately.
        {
            var parsed = OrderParser.Parse(TimedOrderXml(duration: "P00Y00M00DT01H20M00S",
                                                         startIso: "2026-09-14T12:00:00Z"));
            var t = parsed.Tasks[0];
            Check(ref failures,
                  t.AbsoluteStartUtc is DateTime abs
                  && abs == new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc)
                  && abs.Kind == DateTimeKind.Utc
                  && t.SimulationStartMs == 0,
                  $"an ABSOLUTE StartTime is lifted as a UTC instant (got {t.AbsoluteStartUtc:O}) and leaves " +
                  $"the relative delay at {t.SimulationStartMs} ms");
            var noStart = OrderParser.Parse(TimedOrderXml(duration: "P00Y00M00DT01H20M00S", startIso: null));
            Check(ref failures, noStart.Tasks[0].AbsoluteStartUtc is null,
                  "... and a task with no StartTime has none (it dispatches when its gate opens)");
        }
    }

    // ------------------------------------------------ RL-20260921-09 ----
    /// <summary>
    /// THE OWNER'S TEMPORARY POSITION ON COMPLETION (RL-20260921-09), as scoped for the completion
    /// unit and approved on 2026-09-25 (RL-20260925-01): every task with a Duration ends at its
    /// dispatch time plus that Duration; an early finish is HELD until then; a unit still
    /// travelling at that moment is reported complete when it ARRIVES; a stuck unit is the stall
    /// abort (RL-20260914-01), which the timer no longer hides; each follow-on gets its full
    /// Duration from its own start. One check per clause.
    ///
    /// <see cref="CompletionFlow"/> is the service's decision sequence (SynthesizeUnitCompletion,
    /// MaybeCompleteTimedTasks, PushTaskStatus, MaybeCheckStalls, the back-end-loss branch) over the
    /// REAL TimedCompletionPolicy, TaskStatusPolicy and TaskSequencer, and the codes it sends come
    /// from the same static helpers the service calls - so the rule cannot change on one side only.
    /// </summary>
    private static void TemporaryPosition(ref int failures)
    {
        const double D = 100.0;

        // (t1) A task with NO destination (hold, defend, fire ...) whose sim task ends early is
        //      still reported complete at its end time, once.
        {
            var f = new CompletionFlow();
            f.Dispatch("NODEST", D, hasDestination: false);
            f.Tick(50.0);
            f.Finish("NODEST");                                   // e.g. a fire task's vendor completion
            Check(ref failures, f.Count("NODEST", S.TaskStatusCodeType.TASKCMPLT) == 0,
                  "(t1) a no-destination task whose sim task ends EARLY is not reported complete early");
            f.Tick(D - 1.0);
            Check(ref failures, f.Count("NODEST", S.TaskStatusCodeType.TASKCMPLT) == 0,
                  "(t1) ... nor one second before its end time");
            f.Tick(D);
            f.Tick(D * 3);
            Check(ref failures, f.Count("NODEST", S.TaskStatusCodeType.TASKCMPLT) == 1,
                  "(t1) ... and exactly ONE TASKCMPLT at its end time");
        }

        // (t2) A destination task that ARRIVES EARLY emits nothing until its end time, then exactly
        //      one TASKCMPLT, and its follow-on is released only then.
        {
            var f = new CompletionFlow();
            f.Dispatch("EARLY", D, hasDestination: true);
            var next = f.Seq.WaitForStartAsync("EARLY", 0, 0, D + 60.0, f.Clock.AsTaskClock(),
                                               CancellationToken.None, double.NaN, 86400.0);
            f.Tick(30.0);
            f.Finish("EARLY");                                    // arrival evidence at 30 s
            f.Tick(D - 1.0);
            Thread.Sleep(FakeClock.PollMs * 3);
            Check(ref failures, f.Count("EARLY", S.TaskStatusCodeType.TASKCMPLT) == 0 && !next.IsCompleted,
                  "(t2) an EARLY arrival sends no TASKCMPLT and does not release the follow-on before the end time");
            f.Tick(D);
            bool rel = next.Wait(TimeSpan.FromSeconds(3));
            Check(ref failures, f.Count("EARLY", S.TaskStatusCodeType.TASKCMPLT) == 1
                             && rel && next.Result == GateResult.Proceed,
                  "(t2) ... at the end time exactly ONE TASKCMPLT, and the follow-on is released then");
        }

        // (t3) A destination task NOT FINISHED at its end time emits nothing and does NOT release its
        //      follow-on, whose gate does NOT time out at end + margin; the late arrival emits one
        //      TASKCMPLT at once and releases it. Real TaskSequencer on a fake clock, like (b8).
        {
            var f = new CompletionFlow();
            f.Dispatch("LATE", D, hasDestination: true);
            var next = f.Seq.WaitForStartAsync("LATE", 0, 0, D + 60.0, f.Clock.AsTaskClock(),
                                               CancellationToken.None, double.NaN, 86400.0);
            f.Tick(D);                                            // end time, unit still travelling
            Check(ref failures, f.Count("LATE", S.TaskStatusCodeType.TASKCMPLT) == 0
                             && f.OverdueLines.Contains("LATE") && f.Timed.Count == 1,
                  "(t3) a unit still travelling at its end time is NOT reported complete - one OVERDUE line, " +
                  "the timer entry stays");
            f.Tick(D + 60.0 + 240.0);                             // well past end + margin
            Thread.Sleep(FakeClock.PollMs * 5);
            Check(ref failures, !next.IsCompleted,
                  "(t3) ... and its follow-on's gate does NOT time out at end + margin while it waits");
            f.Tick(D + 400.0);
            f.Finish("LATE");                                     // arrives 400 s late
            bool rel = next.Wait(TimeSpan.FromSeconds(3));
            Check(ref failures, f.Count("LATE", S.TaskStatusCodeType.TASKCMPLT) == 1
                             && rel && next.Result == GateResult.Proceed && f.Timed.Count == 0,
                  "(t3) ... the LATE arrival sends exactly ONE TASKCMPLT at once and releases the follow-on");
            f.Tick(D * 20);
            Check(ref failures, f.Count("LATE", S.TaskStatusCodeType.TASKCMPLT) == 1,
                  "(t3) ... and nothing fires again afterwards");
        }

        // (t4) A STALL abort on an overdue task is SENT, not suppressed, and a later arrival still
        //      sends TASKCMPLT (abort-then-complete - a supervisor position, RL-20260914-01 covers the
        //      stalled-unit CODE only).
        {
            var f = new CompletionFlow();
            f.Dispatch("STUCK", D, hasDestination: true);
            f.Tick(D);                                            // overdue
            f.Tick(D + 360.0);
            f.Stall("STUCK");
            Check(ref failures, f.Count("STUCK", S.TaskStatusCodeType.TASKABRT) == 1
                             && f.Count("STUCK", S.TaskStatusCodeType.TASKCMPLT) == 0,
                  "(t4) the stall TASKABRT on an overdue task is SENT - no timed TASKCMPLT got there first");
            f.Tick(D + 900.0);
            f.Finish("STUCK");
            Check(ref failures, f.Count("STUCK", S.TaskStatusCodeType.TASKCMPLT) == 1,
                  "(t4) ... and the unit that later arrives still reports TASKCMPLT (abort-then-complete)");
        }

        // (t5) A stall abort does NOT cancel the timer: a unit that stalls, recovers and arrives
        //      BEFORE its end time completes AT the end time, not early.
        {
            var f = new CompletionFlow();
            f.Dispatch("RECOVER", D, hasDestination: true);
            f.Tick(40.0);
            f.Stall("RECOVER");
            f.Tick(70.0);
            f.Finish("RECOVER");
            Check(ref failures, f.Count("RECOVER", S.TaskStatusCodeType.TASKCMPLT) == 0 && f.Timed.Count == 1,
                  "(t5) a stall abort leaves the timer armed: the recovered unit's early arrival is HELD");
            f.Tick(D);
            Check(ref failures, f.Count("RECOVER", S.TaskStatusCodeType.TASKCMPLT) == 1,
                  "(t5) ... and it completes AT its end time, once");
        }

        // (t6) D2 (RL-20260925-01): the stall abort ABANDONS the stuck unit's follow-ons, each with
        //      its own abort, as every other abort does.
        {
            var f = new CompletionFlow();
            f.Dispatch("STALLPRED", D, hasDestination: true);
            var next = f.Seq.WaitForStartAsync("STALLPRED", 0, 0, D + 60.0, f.Clock.AsTaskClock(),
                                               CancellationToken.None, double.NaN, 86400.0);
            f.Tick(D);
            f.Stall("STALLPRED");
            bool done = next.Wait(TimeSpan.FromSeconds(3));
            Check(ref failures, done && next.Result == GateResult.PredecessorAbandoned,
                  "(t6) the follow-on of a unit reported STUCK is ABANDONED at once (it then sends its own TASKABRT)");
        }

        // (t7) The follow-on's timer is armed with its FULL Duration from its own (late) dispatch.
        {
            var p = new TimedCompletionPolicy();
            p.Register("FOLLOW", "taskee-f", "T_Follow", "1-35 AR", D, hasDestination: false);
            p.Advance(500.0, usingSim: true);                     // dispatched at 500, after a late predecessor
            Check(ref failures, p.Advance(500.0 + D - 1.0, usingSim: true).Count == 0
                             && p.Advance(500.0 + D, usingSim: true).Count == 1,
                  "(t7) a follow-on dispatched late still serves its FULL Duration from its own dispatch");
        }

        // (t8) A VENDOR FAILURE is still an immediate TASKABRT, its follow-ons abandoned, and no
        //      TASKCMPLT at the end time.
        {
            var f = new CompletionFlow();
            f.Dispatch("FAILED", D, hasDestination: true);
            var next = f.Seq.WaitForStartAsync("FAILED", 0, 0, D + 60.0, f.Clock.AsTaskClock(),
                                               CancellationToken.None, double.NaN, 86400.0);
            f.Tick(20.0);
            f.Finish("FAILED", success: false);
            bool done = next.Wait(TimeSpan.FromSeconds(3));
            f.Tick(D * 3);
            Check(ref failures, f.Count("FAILED", S.TaskStatusCodeType.TASKABRT) == 1
                             && f.Count("FAILED", S.TaskStatusCodeType.TASKCMPLT) == 0
                             && done && next.Result == GateResult.PredecessorAbandoned && f.Timed.Count == 0,
                  "(t8) a vendor failure is an immediate TASKABRT, the follow-on is abandoned, no timed TASKCMPLT follows");
        }

        // (t9) BACK-END LOSS cancels the timers of tasks HELD after an early finish (they are no
        //      longer in flight, so the in-flight snapshot misses them): each gets its TASKABRT and
        //      no TASKCMPLT arrives at the Duration from a dead back end.
        {
            var f = new CompletionFlow();
            f.Dispatch("HELD", D, hasDestination: true);
            f.Tick(10.0);
            f.Finish("HELD");                                     // arrived early, held
            f.BackendLost();
            f.Tick(D * 2);
            Check(ref failures, f.Count("HELD", S.TaskStatusCodeType.TASKABRT) == 1
                             && f.Count("HELD", S.TaskStatusCodeType.TASKCMPLT) == 0 && f.Timed.Count == 0,
                  "(t9) back-end loss aborts a task HELD after an early finish and cancels its timer");
        }

        // (t10) D4 (RL-20260925-01): an ATTACK/BREACH whose move arrives AFTER its Duration still
        //       gets its parked engage, and the task is reported complete on that arrival.
        {
            var v = TimedCompletionPolicy.FinishVerdict.EmitNow;
            Check(ref failures,
                  TimedCompletionPolicy.CompletionCode(attributed: true, true, taskContinues: true, v) == S.TaskStatusCodeType.TASKCMPLT
                  && TimedCompletionPolicy.ReleasesSuccessorsNow(true, v),
                  "(t10) a LATE move half of an attack reports TASKCMPLT (the engage is still issued) and releases the follow-on");
            var h = TimedCompletionPolicy.FinishVerdict.Hold;
            Check(ref failures,
                  TimedCompletionPolicy.CompletionCode(attributed: true, true, taskContinues: true, h) == S.TaskStatusCodeType.TASKINPRG
                  && TimedCompletionPolicy.CompletionCode(attributed: true, true, taskContinues: false, h) is null
                  && !TimedCompletionPolicy.ReleasesSuccessorsNow(true, h),
                  "(t10) an EARLY move half still reports TASKINPRG; an early plain finish reports nothing and releases nothing");
            var n = TimedCompletionPolicy.FinishVerdict.NotTimed;
            Check(ref failures,
                  TimedCompletionPolicy.CompletionCode(attributed: true, true, taskContinues: false, n) == S.TaskStatusCodeType.TASKCMPLT
                  && TimedCompletionPolicy.CompletionCode(attributed: true, true, taskContinues: true, n) == S.TaskStatusCodeType.TASKINPRG
                  && TimedCompletionPolicy.CompletionCode(attributed: true, false, taskContinues: false, n) == S.TaskStatusCodeType.TASKABRT
                  && TimedCompletionPolicy.ReleasesSuccessorsNow(true, n) && !TimedCompletionPolicy.ReleasesSuccessorsNow(false, n),
                  "(t10) a task with NO armed timer keeps today's evidence-only codes (no Duration, or TimedCompletion off)");
        }

        // (t12) S1 of the lane M review: THE ENGAGE FALLBACK STOPS THE MOVE. After
        //       Vrf:EngageFallbackSeconds the interface itself replaces an ATTACK/BREACH approach move
        //       with the engage, so the unit is no longer travelling anywhere. Under RL-20260921-09 the
        //       only exception to "ends at start time + Duration" is a unit STILL TRAVELLING, so the
        //       task must end at its end time - not sit OVERDUE waiting for an arrival nothing watches.
        {
            var f = new CompletionFlow();
            f.Dispatch("ATTACK1", D, hasDestination: true);
            f.ParkEngage("ATTACK1");
            var next = f.Seq.WaitForStartAsync("ATTACK1", 0, 0, D + 60.0, f.Clock.AsTaskClock(),
                                               CancellationToken.None, double.NaN, 86400.0);
            f.Tick(30.0);
            f.EngageFallback("ATTACK1");                          // before the end time
            Check(ref failures, f.Count("ATTACK1", S.TaskStatusCodeType.TASKCMPLT) == 0,
                  "(t12) an engage fallback BEFORE the end time reports nothing early");
            f.Tick(D);
            bool rel = next.Wait(TimeSpan.FromSeconds(3));
            Check(ref failures, f.Count("ATTACK1", S.TaskStatusCodeType.TASKCMPLT) == 1
                             && !f.OverdueLines.Contains("ATTACK1") && f.Timed.Count == 0
                             && rel && next.Result == GateResult.Proceed,
                  "(t12) ... and the task completes AT its end time (no OVERDUE), releasing its follow-on - the move " +
                  "was stopped by the interface, so the unit is not 'still travelling' (RL-20260921-09)");
        }

        // (t13) S1, the late half: the fallback fires AFTER the task already went OVERDUE (end time
        //       shorter than the fallback): it completes at once.
        {
            var f = new CompletionFlow();
            f.Dispatch("ATTACK2", D, hasDestination: true);
            f.ParkEngage("ATTACK2");
            var next = f.Seq.WaitForStartAsync("ATTACK2", 0, 0, D + 60.0, f.Clock.AsTaskClock(),
                                               CancellationToken.None, double.NaN, 86400.0);
            f.Tick(D);                                            // overdue, move still running
            f.Tick(D + 200.0);
            f.EngageFallback("ATTACK2");                          // the interface stops the move now
            bool rel = next.Wait(TimeSpan.FromSeconds(3));
            Check(ref failures, f.Count("ATTACK2", S.TaskStatusCodeType.TASKCMPLT) == 1
                             && f.Timed.Count == 0 && rel && next.Result == GateResult.Proceed,
                  "(t13) an engage fallback on an OVERDUE task completes it AT ONCE and releases its follow-on");
            f.Tick(D * 10);
            f.Finish("ATTACK2");                                  // the engage's own vendor completion, later
            Check(ref failures, f.Count("ATTACK2", S.TaskStatusCodeType.TASKCMPLT) == 1,
                  "(t13) ... and the engage's own later completion adds no second TASKCMPLT");
        }

        // (t14) S5: SUPERSEDE under the non-default Vrf:SupersededTaskCode=TASKCMPLT marks the old task
        //       FINISHED (the service's supersede branch): before its end time it completes AT the end
        //       time; once overdue it completes at once. Without it, a superseded task with a destination
        //       would wait for an arrival that can never come.
        {
            var f = new CompletionFlow();
            f.Dispatch("SUP1", D, hasDestination: true);
            f.Tick(20.0);
            f.SupersedeKeepCompletion("SUP1");
            Check(ref failures, f.Count("SUP1", S.TaskStatusCodeType.TASKCMPLT) == 0,
                  "(t14) superseded (TASKCMPLT setting) before its end time: nothing is sent at the supersede");
            f.Tick(D);
            Check(ref failures, f.Count("SUP1", S.TaskStatusCodeType.TASKCMPLT) == 1 && !f.OverdueLines.Contains("SUP1"),
                  "(t14) ... it completes AT its end time and never goes OVERDUE");
            var g = new CompletionFlow();
            g.Dispatch("SUP2", D, hasDestination: true);
            g.Tick(D);                                            // overdue
            g.SupersedeKeepCompletion("SUP2");
            Check(ref failures, g.Count("SUP2", S.TaskStatusCodeType.TASKCMPLT) == 1 && g.Timed.Count == 0,
                  "(t14) superseded (TASKCMPLT setting) while OVERDUE: TASKCMPLT at once");
        }

        // (t16) NEW-1 of the lane M2 re-review, case (a) WATCHDOG FIRST: a STUCK attacker is judged by
        //       the watchdog (stall TASKABRT, follow-ons abandoned, D2), THEN the engage fallback
        //       fires. It must NOT be completed at its end time: "The notion that geting stuck midway
        //       is a complete is completelly illogical" (RL-20260921-05); a unit that never arrives
        //       is a stuck unit (RL-20260921-09 S569). Before this fix the fallback dropped the
        //       destination and a TASKCMPLT followed at the end time - abort-then-complete on a unit
        //       that never moved.
        {
            const double LongD = 1000.0;
            var f = new CompletionFlow();
            f.Dispatch("STUCKATK", LongD, hasDestination: true);
            f.ParkEngage("STUCKATK");
            var next = f.Seq.WaitForStartAsync("STUCKATK", 0, 0, LongD + 60.0, f.Clock.AsTaskClock(),
                                               CancellationToken.None, double.NaN, 86400.0);
            f.Tick(245.0);
            f.Stall("STUCKATK");                                  // the watchdog judges first
            f.Tick(300.0);
            f.EngageFallback("STUCKATK", TimedCompletionPolicy.StallAtFallback.Unknown);
            f.Tick(LongD);
            f.Tick(LongD * 5);
            bool done = next.Wait(TimeSpan.FromSeconds(3));
            Check(ref failures, f.Count("STUCKATK", S.TaskStatusCodeType.TASKABRT) == 1
                             && f.Count("STUCKATK", S.TaskStatusCodeType.TASKCMPLT) == 0
                             && done && next.Result == GateResult.PredecessorAbandoned
                             && !f.EngagesIssued.Contains("STUCKATK"),
                  "(t16) watchdog first, then the engage fallback: the stuck attacker stays ABORTED - no TASKCMPLT at or " +
                  "after its end time, its follow-on stays abandoned, and no engage is issued from where it is stuck");
            f.Finish("STUCKATK");                                 // it does reach the objective after all
            Check(ref failures, f.Count("STUCKATK", S.TaskStatusCodeType.TASKCMPLT) == 1,
                  "(t16) ... and only a REAL later arrival completes it (abort-then-complete on an arrival)");
        }

        // (t17) NEW-1 case (b) FALLBACK FIRST, judged STALLED at the fallback: the stall path is taken
        //       there and then (TASKABRT, D2 abandon), no engage, no TASKCMPLT at the end time.
        {
            const double LongD = 1000.0;
            var f = new CompletionFlow();
            f.Dispatch("STUCKB", LongD, hasDestination: true);
            f.ParkEngage("STUCKB");
            var next = f.Seq.WaitForStartAsync("STUCKB", 0, 0, LongD + 60.0, f.Clock.AsTaskClock(),
                                               CancellationToken.None, double.NaN, 86400.0);
            f.Tick(300.0);
            f.EngageFallback("STUCKB", TimedCompletionPolicy.StallAtFallback.Stalled);
            f.Tick(LongD);
            f.Tick(LongD * 5);
            bool done = next.Wait(TimeSpan.FromSeconds(3));
            Check(ref failures, f.Count("STUCKB", S.TaskStatusCodeType.TASKABRT) == 1
                             && f.Count("STUCKB", S.TaskStatusCodeType.TASKCMPLT) == 0
                             && done && next.Result == GateResult.PredecessorAbandoned
                             && !f.EngagesIssued.Contains("STUCKB"),
                  "(t17) fallback first, judged STALLED at the fallback: TASKABRT, follow-on abandoned, no engage, and " +
                  "no TASKCMPLT at its end time");
        }

        // (t18) A unit judged MOVING at the fallback (or not judgeable - the residual) is the one the
        //       interface stopped while travelling: engage issued, completes at its end time (t12).
        {
            const double LongD = 1000.0;
            var f = new CompletionFlow();
            f.Dispatch("MOVINGATK", LongD, hasDestination: true);
            f.ParkEngage("MOVINGATK");
            f.Tick(300.0);
            f.EngageFallback("MOVINGATK", TimedCompletionPolicy.StallAtFallback.Moving);
            f.Tick(LongD);
            var g = new CompletionFlow();
            g.Dispatch("UNKNATK", LongD, hasDestination: true);
            g.ParkEngage("UNKNATK");
            g.Tick(300.0);
            g.EngageFallback("UNKNATK", TimedCompletionPolicy.StallAtFallback.Unknown);
            g.Tick(LongD);
            Check(ref failures, f.Count("MOVINGATK", S.TaskStatusCodeType.TASKCMPLT) == 1 && f.EngagesIssued.Contains("MOVINGATK")
                             && f.Count("MOVINGATK", S.TaskStatusCodeType.TASKABRT) == 0
                             && g.Count("UNKNATK", S.TaskStatusCodeType.TASKCMPLT) == 1 && g.EngagesIssued.Contains("UNKNATK"),
                  "(t18) judged MOVING (or no verdict possible) at the fallback: engage issued, one TASKCMPLT at the end time");
        }

        // (t19) The plan itself, called directly (the static the service calls).
        Check(ref failures,
              TimedCompletionPolicy.PlanEngageFallback(true, TimedCompletionPolicy.StallAtFallback.Unknown)
                  == TimedCompletionPolicy.EngageFallbackPlan.KeepStuck
              && TimedCompletionPolicy.PlanEngageFallback(true, TimedCompletionPolicy.StallAtFallback.Moving)
                  == TimedCompletionPolicy.EngageFallbackPlan.KeepStuck
              && TimedCompletionPolicy.PlanEngageFallback(false, TimedCompletionPolicy.StallAtFallback.Stalled)
                  == TimedCompletionPolicy.EngageFallbackPlan.ReportStuckNow
              && TimedCompletionPolicy.PlanEngageFallback(false, TimedCompletionPolicy.StallAtFallback.Moving)
                  == TimedCompletionPolicy.EngageFallbackPlan.DropAndEngage
              && TimedCompletionPolicy.PlanEngageFallback(false, TimedCompletionPolicy.StallAtFallback.Unknown)
                  == TimedCompletionPolicy.EngageFallbackPlan.DropAndEngage,
              "(t19) the fallback plan: already reported stuck -> keep it stuck; judged stalled now -> report stuck; " +
              "judged moving or no verdict -> engage and drop the destination");

        // (t20) M3-1 of the lane M3 re-review: A SUPERSEDE BETWEEN THE FALLBACK TIMER AND THE TICK.
        //       The pending engage is removed on the pool thread and the decision waits for the next
        //       tick; a newer task dispatched in between (the default Vrf:SupersededTaskCode=TASKABRT
        //       aborts the old one) must not get the OLD engage fired over it, and the old task must
        //       get no destination drop and no TASKCMPLT.
        {
            const double LongD = 1000.0;
            var f = new CompletionFlow();
            f.Dispatch("OLDATK", LongD, hasDestination: true);
            f.ParkEngage("OLDATK");
            f.Tick(300.0);
            f.SupersedeAbort("OLDATK");                           // superseded by a newer task
            f.EngageFallback("OLDATK", TimedCompletionPolicy.StallAtFallback.Unknown, moveIsCurrent: false);
            f.Tick(LongD * 3);
            Check(ref failures, !f.EngagesIssued.Contains("OLDATK")
                             && f.Count("OLDATK", S.TaskStatusCodeType.TASKCMPLT) == 0,
                  "(t20) a supersede between the fallback timer and the tick: the OLD engage is NOT issued over the newer " +
                  "task, no destination is dropped and no TASKCMPLT follows for the old task");
        }

        // (t21) M3-2: STALL DETECTION OFF (no watchdog verdict possible) and the unit STAYED PUT since
        //       dispatch (no member moved Vrf:StallMoveMeters): the owner's caveat, "abort in case the
        //       unit stays put" (RL-20260921-07) -> the stuck path, not a completion.
        {
            const double LongD = 1000.0;
            var f = new CompletionFlow();
            f.Dispatch("PUTATK", LongD, hasDestination: true);
            f.ParkEngage("PUTATK");
            var next = f.Seq.WaitForStartAsync("PUTATK", 0, 0, LongD + 60.0, f.Clock.AsTaskClock(),
                                               CancellationToken.None, double.NaN, 86400.0);
            f.Tick(300.0);
            var put = TimedCompletionPolicy.StaysPutVerdict(new[] { 0.0, 3.5, 12.0 }, 4, 50.0, 1);
            f.EngageFallback("PUTATK", TimedCompletionPolicy.StallAtFallback.Unknown, staysPut: put);
            f.Tick(LongD);
            f.Tick(LongD * 3);
            bool done = next.Wait(TimeSpan.FromSeconds(3));
            Check(ref failures, f.Count("PUTATK", S.TaskStatusCodeType.TASKABRT) == 1
                             && f.Count("PUTATK", S.TaskStatusCodeType.TASKCMPLT) == 0
                             && !f.EngagesIssued.Contains("PUTATK")
                             && done && next.Result == GateResult.PredecessorAbandoned,
                  "(t21) detection OFF, stayed put since dispatch: TASKABRT, follow-on abandoned, no engage, no TASKCMPLT " +
                  "(RL-20260921-07: abort in case the unit stays put)");
        }

        // (t22) M3-2: detection OFF and the unit DID move since dispatch: the interface stops a
        //       travelling unit - engage issued, completes at its end time.
        {
            const double LongD = 1000.0;
            var f = new CompletionFlow();
            f.Dispatch("MOVEDATK", LongD, hasDestination: true);
            f.ParkEngage("MOVEDATK");
            f.Tick(300.0);
            var moved = TimedCompletionPolicy.StaysPutVerdict(new[] { 20.0, 140.0, 95.0 }, 4, 50.0, 1);
            f.EngageFallback("MOVEDATK", TimedCompletionPolicy.StallAtFallback.Unknown, staysPut: moved);
            f.Tick(LongD);
            Check(ref failures, f.EngagesIssued.Contains("MOVEDATK")
                             && f.Count("MOVEDATK", S.TaskStatusCodeType.TASKCMPLT) == 1
                             && f.Count("MOVEDATK", S.TaskStatusCodeType.TASKABRT) == 0,
                  "(t22) detection OFF, moved since dispatch: engage issued, one TASKCMPLT at the end time");
        }

        // (t23) The pure pieces t20-t22 rest on, called directly.
        Check(ref failures,
              TimedCompletionPolicy.PlanEngageFallback(false, false, TimedCompletionPolicy.StallAtFallback.Moving)
                  == TimedCompletionPolicy.EngageFallbackPlan.Superseded
              && TimedCompletionPolicy.PlanEngageFallback(false, true, TimedCompletionPolicy.StallAtFallback.Stalled)
                  == TimedCompletionPolicy.EngageFallbackPlan.Superseded
              && TimedCompletionPolicy.PlanEngageFallback(true, false, TimedCompletionPolicy.StallAtFallback.Moving)
                  == TimedCompletionPolicy.EngageFallbackPlan.DropAndEngage,
              "(t23) a move that is no longer the unit's current task gets NO fallback action at all");
        Check(ref failures,
              TimedCompletionPolicy.StaysPutVerdict(new[] { 0.0, 49.9 }, 2, 50.0, 1) == TimedCompletionPolicy.StallAtFallback.Stalled
              && TimedCompletionPolicy.StaysPutVerdict(new[] { 0.0, 50.0 }, 2, 50.0, 1) == TimedCompletionPolicy.StallAtFallback.Moving
              && TimedCompletionPolicy.StaysPutVerdict(Array.Empty<double>(), 2, 50.0, 1) == TimedCompletionPolicy.StallAtFallback.Unknown
              && TimedCompletionPolicy.StaysPutVerdict(new[] { 0.0 }, 4, 50.0, 2) == TimedCompletionPolicy.StallAtFallback.Unknown,
              "(t23) the stays-put test is StallPolicy.Decide on displacement SINCE DISPATCH at Vrf:StallMoveMeters: below it " +
              "on every readable member = stayed put; at it = moved; no or too few readable members = no verdict");
        Check(ref failures,
              TimedCompletionPolicy.CombineWithStaysPut(TimedCompletionPolicy.StallAtFallback.Moving,
                                                        TimedCompletionPolicy.StallAtFallback.Stalled)
                  == TimedCompletionPolicy.StallAtFallback.Moving
              && TimedCompletionPolicy.CombineWithStaysPut(TimedCompletionPolicy.StallAtFallback.Unknown,
                                                           TimedCompletionPolicy.StallAtFallback.Stalled)
                  == TimedCompletionPolicy.StallAtFallback.Stalled,
              "(t23) the stays-put test is consulted ONLY when the watchdog has no verdict");

        // (t24) M4-1 of the lane M4 review: A REAL ARRIVAL PROCESSED IN THE WINDOW BETWEEN THE FALLBACK
        //       TIMER AND ITS TICK ACTION. The arrival path (tick thread) must still find the parked
        //       engage and issue it (D4: "Issue it"), and the fallback's later tick action must then
        //       find nothing to do - no second engage, no drop.
        {
            const double LongD = 1000.0;
            var f = new CompletionFlow();
            f.Dispatch("RACEATK", LongD, hasDestination: true);
            f.ParkEngage("RACEATK");
            f.Tick(300.0);
            f.FallbackTimerFires("RACEATK");                      // pool thread: the delay ends
            f.Arrive("RACEATK");                                  // tick: the unit's arrival is processed first
            f.EngageFallback("RACEATK", TimedCompletionPolicy.StallAtFallback.Moving);   // then the queued action
            f.Tick(LongD);
            Check(ref failures, f.EngagesIssued.Count(t => t == "RACEATK") == 1
                             && f.Count("RACEATK", S.TaskStatusCodeType.TASKCMPLT) == 1
                             && f.Count("RACEATK", S.TaskStatusCodeType.TASKABRT) == 0
                             && f.FallbackFoundNothing.Contains("RACEATK"),
                  "(t24) an arrival processed between the fallback timer and its tick action still gets its engage (D4), " +
                  "exactly once, and one TASKCMPLT at the end time - the fallback's action finds nothing to do");
        }

        // (t25) A1 of run NAV_STALL_FALLBACK-2026-09-26-1 (runs\20260926T181639Z_run): the engage
        //       fallback issued the fire, VR-Forces REFUSED it at once ("No controller ... unable to
        //       carry out task ... Fire Weapon Task", app log L479763; "fire-at-target (success=False)",
        //       L479765) and the task was aborted (TASKABRT, L479769) - which removes the unit's in-flight
        //       record. The MOVE the fire never replaced ran on and completed 2,143 SIM s later
        //       (L1786309); with no in-flight record that completion was reported as a TASKCMPLT with NO
        //       task uuid (L1786313 "task=(none)"; on the bus as REPORT #9842, <CurrentTask />). A
        //       terminal report that names no task tells STP nothing it can attribute and contradicts the
        //       TASKABRT it already has: an UNATTRIBUTED completion sends NOTHING.
        {
            var f = new CompletionFlow();
            f.Dispatch("FIREREFUSED", D * 18, hasDestination: true);
            f.ParkEngage("FIREREFUSED");
            f.Tick(30.0);
            f.EngageFallback("FIREREFUSED");                      // the fire is issued (replaces nothing)
            f.Finish("FIREREFUSED", success: false);              // the fire's vendor failure
            f.Tick(D * 5);
            f.Finish("FIREREFUSED", success: true);               // the un-replaced move's vendor completion
            f.Tick(D * 30);
            Check(ref failures, f.Count("FIREREFUSED", S.TaskStatusCodeType.TASKABRT) == 1
                             && f.Count("FIREREFUSED", S.TaskStatusCodeType.TASKCMPLT) == 0
                             && f.Sent.Count(s => s.Task == "") == 0,
                  "(t25) a refused engage aborts the task ONCE, and the surviving move's later completion - which no " +
                  "in-flight record attributes - sends NOTHING (no TASKCMPLT with an empty task uuid)");
            var g = new CompletionFlow();
            g.Finish("NEVERDISPATCHED", success: false);          // an unattributed FAILURE
            g.Finish("NEVERDISPATCHED", success: true);           // and an unattributed success
            Check(ref failures, g.Sent.Count == 0,
                  "(t25) an unattributed vendor completion sends no TaskStatus, success or failure");
        }

        // (t15) The two policy calls t12-t14 rest on, called directly (no mirror).
        {
            var p = new TimedCompletionPolicy();
            p.Register("DD", "tk", "DD", "u", D, hasDestination: true);
            p.Advance(0.0, usingSim: true);
            var v1 = p.DropDestination("DD");
            var due = p.Advance(D, usingSim: true);
            Check(ref failures, v1 == TimedCompletionPolicy.FinishVerdict.Hold && due.Count == 1
                             && due[0].Kind == TimedCompletionPolicy.DueKind.CompleteNow && p.Count == 0
                             && p.HeldAfterFinish().Count == 0,
                  "(t15) DropDestination before the end time: Hold, then CompleteNow at the end time (and it is NOT " +
                  "listed as finished-early, because its engage is still in flight)");
            var q = new TimedCompletionPolicy();
            q.Register("MF", "tk", "MF", "u", D, hasDestination: true);
            q.Advance(0.0, usingSim: true);
            var od = q.Advance(D, usingSim: true);
            Check(ref failures, od.Count == 1 && od[0].Kind == TimedCompletionPolicy.DueKind.OverdueAwaitingArrival
                             && q.DropDestination("MF") == TimedCompletionPolicy.FinishVerdict.EmitNow && q.Count == 0
                             && q.DropDestination("MF") == TimedCompletionPolicy.FinishVerdict.NotTimed,
                  "(t15) DropDestination on an OVERDUE task: EmitNow once, the entry removed, then NotTimed");
            var r = new TimedCompletionPolicy();
            r.Register("SP", "tk", "SP", "u", D, hasDestination: true);
            r.Advance(0.0, usingSim: true);
            Check(ref failures, r.MarkFinished("SP") == TimedCompletionPolicy.FinishVerdict.Hold
                             && r.Advance(D, usingSim: true).Count == 1 && r.Count == 0,
                  "(t15) MarkFinished (the TASKCMPLT supersede) on a running task: Hold, then CompleteNow at the end time");
        }

        // (t11) FAIL-FIRST CONTROL for (t3)'s gate: a caller that passes NO overdue backstop gets the
        //       pre-2026-09-25 behaviour - the follow-on of a late unit times out at end + margin.
        {
            var f = new CompletionFlow();
            f.Dispatch("LATE2", D, hasDestination: true);
            var next = f.Seq.WaitForStartAsync("LATE2", 0, 0, D + 60.0, f.Clock.AsTaskClock(),
                                               CancellationToken.None);
            f.Tick(D);
            for (double t = D + 20.0; t <= D + 300.0 && !next.IsCompleted; t += 20.0)
            {
                f.Tick(t);
                Thread.Sleep(FakeClock.PollMs * 2);
            }
            bool done = next.Wait(TimeSpan.FromSeconds(3));
            Check(ref failures, done && next.Result == GateResult.PredecessorTimeout,
                  "(t11) CONTROL: with no overdue backstop the late unit's follow-on is skipped at end + margin " +
                  "(what the service did before this unit)");
        }
    }

    /// <summary>
    /// The service's completion decision sequence, one method per service site, over the REAL
    /// policies and sequencer (see <see cref="TemporaryPosition"/>). Each method names the service
    /// method it mirrors; the codes and the release decision come from the static helpers the
    /// service itself calls.
    /// </summary>
    private sealed class CompletionFlow
    {
        public readonly FakeClock Clock = new();
        public readonly TimedCompletionPolicy Timed = new();
        public readonly TaskStatusPolicy Status = new();
        public readonly TaskSequencer Seq = new();
        public readonly List<(string Task, S.TaskStatusCodeType Code)> Sent = new();
        public readonly List<string> OverdueLines = new();
        private readonly HashSet<string> _inFlight = new(StringComparer.Ordinal);

        /// <summary>MarkDispatched: record in flight, TASKSTRT, arm the end time (anchored now).</summary>
        public void Dispatch(string task, double durationSeconds, bool hasDestination)
        {
            _inFlight.Add(task);
            Seq.NotifyDispatched(task, Clock.Now);
            Push(task, S.TaskStatusCodeType.TASKSTRT);
            Timed.Register(task, "taskee-" + task, task, "unit-" + task, durationSeconds, hasDestination);
            Timed.Advance(Clock.Now, usingSim: true);
        }

        /// <summary>PushTaskStatus: the timer cancel, then the emission rules.</summary>
        public void Push(string task, S.TaskStatusCodeType code, bool reportOnlyAbort = false)
        {
            if (TimedCompletionPolicy.CancelsTimer(code, reportOnlyAbort)) Timed.Cancel(task);
            if (Status.ShouldEmit(code, task)) Sent.Add((task, code));
        }

        /// <summary>SynthesizeUnitCompletion: every successful or failed completion. The completion is
        /// ATTRIBUTED only when the unit has an in-flight record (the service's _inFlight.TryComplete);
        /// a vendor completion that arrives after that record is gone carries NO task uuid, and the
        /// service's report for it goes out with an empty task (the "" entry in Sent).</summary>
        public void Finish(string task, bool success = true, bool taskContinues = false)
        {
            string uuid = _inFlight.Remove(task) ? task : null;
            var verdict = success && uuid != null ? Timed.MarkFinished(uuid) : TimedCompletionPolicy.FinishVerdict.NotTimed;
            if (TimedCompletionPolicy.ReleasesSuccessorsNow(success, verdict)) Seq.CompleteTask(uuid);
            else if (!success) Seq.NotifyAbandoned(uuid);
            var code = TimedCompletionPolicy.CompletionCode(uuid != null, success, taskContinues, verdict);
            if (code is S.TaskStatusCodeType c) Push(uuid ?? "", c);
        }

        /// <summary>MaybeCompleteTimedTasks at this task-clock reading.</summary>
        public void Tick(double clock)
        {
            double step = clock - Clock.Now;
            if (step > 0) Clock.Advance(step);
            foreach (var p in Timed.Advance(Clock.Now, usingSim: true))
            {
                if (p.Kind == TimedCompletionPolicy.DueKind.OverdueAwaitingArrival)
                {
                    OverdueLines.Add(p.TaskUuid);
                    Seq.NotifyOverdue(p.TaskUuid);
                    continue;
                }
                Push(p.TaskUuid, S.TaskStatusCodeType.TASKCMPLT);
                Seq.CompleteTask(p.TaskUuid);
            }
        }

        /// <summary>MaybeCheckStalls: the report-only abort, then (D2) the follow-ons abandoned.</summary>
        public void Stall(string task)
        {
            StallReported.Add(task);
            Push(task, S.TaskStatusCodeType.TASKABRT, reportOnlyAbort: true);
            Seq.NotifyAbandoned(task);
        }

        public readonly HashSet<string> StallReported = new(StringComparer.Ordinal);
        public readonly List<string> EngagesIssued = new();
        public readonly HashSet<string> PendingEngages = new(StringComparer.Ordinal);
        public readonly List<string> FallbackFoundNothing = new();

        /// <summary>DeferEngageUntilMoveCompletes: the engage is parked on the move.</summary>
        public void ParkEngage(string task) => PendingEngages.Add(task);

        /// <summary>SynthesizeUnitCompletion for an ATTACK/BREACH move half: a parked engage still on
        /// the list is taken and issued (re-recorded in flight under the same uuid).</summary>
        public void Arrive(string task)
        {
            bool cont = PendingEngages.Remove(task);
            Finish(task, true, cont);
            if (cont) { EngagesIssued.Add(task); _inFlight.Add(task); }
        }

        /// <summary>MarkDispatched's supersede branch (default TASKABRT): abort, abandon, and the
        /// engage parked on the old move is cancelled.</summary>
        public void SupersedeAbort(string task)
        {
            _inFlight.Remove(task);
            PendingEngages.Remove(task);
            Push(task, S.TaskStatusCodeType.TASKABRT);
            Seq.NotifyAbandoned(task);
        }

        /// <summary>The tick-thread start of EngageFallbackOnTick (M4-1): the fallback takes the engage
        /// off the list HERE, on the tick thread; if the arrival path or a supersede took it first,
        /// the fallback does nothing.</summary>
        private bool FallbackTookEngage(string task)
        {
            if (PendingEngages.Remove(task)) return true;
            FallbackFoundNothing.Add(task);
            return false;
        }

        /// <summary>EngageFallbackAsync, the POOL-thread part, when the Vrf:EngageFallbackSeconds delay
        /// ends: since M4-1 it only ENQUEUES the tick action - it takes nothing off the list.</summary>
        public void FallbackTimerFires(string task) { }

        /// <summary>The service's EngageFallbackOnTick: the plan (the same static the service
        /// calls) decides between keeping a stuck unit aborted, reporting it stuck now, or - for a
        /// unit judged moving or not judgeable - issuing the engage and dropping the destination.</summary>
        public void EngageFallback(string task,
            TimedCompletionPolicy.StallAtFallback verdict = TimedCompletionPolicy.StallAtFallback.Moving,
            bool? moveIsCurrent = null,
            TimedCompletionPolicy.StallAtFallback staysPut = TimedCompletionPolicy.StallAtFallback.Unknown)
        {
            if (!FallbackTookEngage(task)) return;
            var combined = TimedCompletionPolicy.CombineWithStaysPut(verdict, staysPut);
            var plan = TimedCompletionPolicy.PlanEngageFallback(moveIsCurrent ?? _inFlight.Contains(task),
                                                                StallReported.Contains(task), combined);
            if (plan == TimedCompletionPolicy.EngageFallbackPlan.Superseded) return;
            if (plan == TimedCompletionPolicy.EngageFallbackPlan.KeepStuck) { PendingEngages.Add(task); return; }
            if (plan == TimedCompletionPolicy.EngageFallbackPlan.ReportStuckNow) { Stall(task); PendingEngages.Add(task); return; }
            EngagesIssued.Add(task);
            var v = Timed.DropDestination(task);
            if (v != TimedCompletionPolicy.FinishVerdict.EmitNow) return;
            Seq.CompleteTask(task);
            Push(task, S.TaskStatusCodeType.TASKCMPLT);
        }

        /// <summary>MarkDispatched's supersede branch under Vrf:SupersededTaskCode=TASKCMPLT.</summary>
        public void SupersedeKeepCompletion(string task)
        {
            _inFlight.Remove(task);
            if (Timed.MarkFinished(task) != TimedCompletionPolicy.FinishVerdict.EmitNow) return;
            Seq.CompleteTask(task);
            Push(task, S.TaskStatusCodeType.TASKCMPLT);
        }

        /// <summary>The back-end-loss branch: in-flight tasks AND tasks held after an early finish.</summary>
        public void BackendLost()
        {
            foreach (var t in _inFlight.ToList())
            {
                Push(t, S.TaskStatusCodeType.TASKABRT);
                Seq.NotifyAbandoned(t);
            }
            foreach (var h in Timed.HeldAfterFinish())
            {
                Push(h.TaskUuid, S.TaskStatusCodeType.TASKABRT);
                Seq.NotifyAbandoned(h.TaskUuid);
            }
        }

        public int Count(string task, S.TaskStatusCodeType code)
            => Sent.Count(s => s.Task == task && s.Code == code);
    }

    /// <summary>A minimal, schema-shaped order carrying a Duration and, optionally, an ABSOLUTE
    /// StartTime (TimeInstantType/DateTime/IsoDateTime) - the form STP does not export.</summary>
    private static string TimedOrderXml(string duration, string startIso) =>
        "<OrderBody xmlns=\"http://www.sisostds.org/schemas/C2SIM/1.1\">"
        + "<OrderID>rulings-selftest-timing</OrderID>"
        + "<Task><ManeuverWarfareTask>"
        + "<Name>T_Timed</Name>"
        + "<UUID>77777777-7777-7777-7777-777777777777</UUID>"
        + "<PerformingEntity>88888888-8888-8888-8888-888888888888</PerformingEntity>"
        + "<TaskActionCode>SECURE</TaskActionCode>"
        + "<Duration><IsoTimeDuration>" + duration + "</IsoTimeDuration></Duration>"
        + (startIso == null ? ""
           : "<StartTime><DateTime><IsoDateTime>" + startIso + "</IsoDateTime></DateTime></StartTime>")
        + "</ManeuverWarfareTask></Task>"
        + "</OrderBody>";

    /// <summary>
    /// Run ONE STREND gate against the real TaskSequencer and the real TimedCompletionPolicy on a
    /// fake clock, and report what the gate decided. The predecessor is dispatched, armed with
    /// <paramref name="predDurationSeconds"/>, and the clock is walked forward in 60 s steps; the
    /// timed completion is fed to CompleteTask exactly as MaybeCompleteTimedTasks does.
    /// </summary>
    private static GateResult GateOutcome(double windowSeconds, double predDurationSeconds)
    {
        var clock = new FakeClock();
        var seq = new TaskSequencer();
        var timed = new TimedCompletionPolicy();
        const string pred = "PRED-M1";
        seq.NotifyDispatched(pred, clock.Now);
        timed.Register(pred, "taskee-m1", "T_Secure", "1-35 AR", predDurationSeconds);
        timed.Advance(clock.Now, usingSim: true);     // the anchoring walk MarkDispatched leaves behind
        var gate = seq.WaitForStartAsync(pred, 0, 0, windowSeconds, clock.AsTaskClock(),
                                         CancellationToken.None);
        for (double served = 0.0; served <= predDurationSeconds + 600.0 && !gate.IsCompleted; served += 60.0)
        {
            clock.Advance(60.0);
            foreach (var d in timed.Advance(clock.Now, usingSim: true)) seq.CompleteTask(d.TaskUuid);
            Thread.Sleep(FakeClock.PollMs * 2);       // let the gate's poller observe the step
        }
        return gate.Wait(TimeSpan.FromSeconds(5)) ? gate.Result : GateResult.PredecessorTimeout;
    }

    /// <summary>A clock the test drives by hand. Monotone, in seconds, exactly what the service's
    /// own task-clock axis is - so a delay taken on it is a delay in SIMULATED time.</summary>
    private sealed class FakeClock
    {
        public const int PollMs = 20;
        private readonly object _lock = new();
        private double _seconds;
        public double Now { get { lock (_lock) return _seconds; } }
        public void Advance(double seconds) { lock (_lock) _seconds += seconds; }
        public TaskClock AsTaskClock() => new(() => Now, DelayAsync);
        private async Task DelayAsync(double seconds, CancellationToken ct)
        {
            double start = Now;
            while (Now - start < seconds)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(PollMs, ct).ConfigureAwait(false);
            }
        }
    }

    // ------------------------------------------------------- THE TASK CLOCK ----
    // M3 + M4 of the cold-start review of 5c67d41. Both defects are the SAME failure mode from two
    // directions: the R4 walk stops serving time, no task ever completes, nothing is logged, and
    // every successor is then skipped at the predecessor gate. Both are checked here against the
    // REAL SimClockTracker, the REAL TaskClockAxis and the REAL TimedCompletionPolicy, with the
    // pre-fix behaviour kept beside each as the fail-first control.
    private static void TaskClockChecks(ref int failures)
    {
        const double dur = 100.0;

        // (e1) M3 - A READER ALTERNATING -1 / >= 0 AT THE SAMPLE CADENCE. This is the documented
        //      "back end briefly out of the list" case (vrfBackendListener.h:154-163) the watchdog
        //      was hardened against and the timed walk was not.
        Func<int, double> flapping = i => (i % 2 == 0) ? 1000.0 + i : -1.0;

        var preFix = WalkTaskClock(flapping, useConfirmedMode: false, honourStale: true, dur, 400);
        Check(ref failures, !preFix.Completed && preFix.AxisSeconds == 0.0,
              $"FAIL-FIRST (M3): on the RAW mode a flapping reader leaves the axis at " +
              $"{preFix.AxisSeconds:F0} s after 400 samples - the task never completes and nothing is logged");

        var fixedFlap = WalkTaskClock(flapping, useConfirmedMode: true, honourStale: true, dur, 400);
        Check(ref failures, fixedFlap.Completed && fixedFlap.ModeFlips == 0,
              $"with the HYSTERESIS-CONFIRMED mode the same reader completes the task " +
              $"({fixedFlap.Samples} samples) and never flips the clock mode ({fixedFlap.ModeFlips} flips)");

        // (e2) M4 - A FROZEN READER. VrfFacade::SimTimeSeconds gates on backends().count() > 0 and
        //      a deactivated back end is NOT removed, so the reader returns its last value for the
        //      rest of the run. Read as a pause, no Duration ever elapses again.
        Func<int, double> frozen = _ => 5000.0;

        var preFixFrozen = WalkTaskClock(frozen, useConfirmedMode: true, honourStale: false, dur, 400);
        Check(ref failures, !preFixFrozen.Completed && preFixFrozen.AxisSeconds == 0.0,
              "FAIL-FIRST (M4): with no stale detection a frozen sim clock freezes every end time - " +
              "no TASKCMPLT is ever emitted, and nothing says so");

        var fixedFrozen = WalkTaskClock(frozen, useConfirmedMode: true, honourStale: true, dur, 400,
                                        backEndPresent: false);
        Check(ref failures, fixedFrozen.Completed && fixedFrozen.StaleTransitions == 1,
              $"a frozen sim clock with NO back end is detected ONCE ({fixedFrozen.StaleTransitions} " +
              $"transition(s)) and the task completes on the WALL fallback ({fixedFrozen.Samples} samples)");

        // (e2b) Q5 (USER RULING 2026-09-14): A PAUSED SCENARIO DOES NOT AGE A TASK. The same frozen
        //       reader, but a back end is still present - which is what a PAUSE looks like from
        //       here. M4's wall fallback burned a coffee break off every armed Duration; the axis
        //       must now hold instead. This drives the BackendCount-only rule - the three-argument
        //       overload, which since STP-809 is the FALLBACK for a bridge or a reader that says
        //       nothing. Its blindness (a deactivated back end counts exactly like a paused one) is
        //       what (e2e) and (e2f) below measure and then fix.
        {
            var paused = WalkTaskClock(frozen, useConfirmedMode: true, honourStale: true, dur, 600,
                                       backEndPresent: true);
            Check(ref failures, !paused.Completed && paused.AxisSeconds == 0.0 && paused.Samples == 600,
                  $"(Q5) a flat sim clock with a back end still present ages a {dur:F0} s task by NOTHING " +
                  $"across 600 wall seconds (axis {paused.AxisSeconds:F0} s, completed={paused.Completed})");
            Check(ref failures,
                  StallPolicy.TaskClockAction(heldOnSim: true, stale: true, backEndPresent: true)
                      == StallPolicy.TaskClockOnFlat.HoldOnSim
                  && StallPolicy.TaskClockAction(heldOnSim: true, stale: true, backEndPresent: false)
                      == StallPolicy.TaskClockOnFlat.FallBackToWall
                  && StallPolicy.TaskClockAction(heldOnSim: true, stale: false, backEndPresent: false)
                      == StallPolicy.TaskClockOnFlat.ServeSim
                  && StallPolicy.TaskClockAction(heldOnSim: false, stale: false, backEndPresent: true)
                      == StallPolicy.TaskClockOnFlat.FallBackToWall,
                  "(Q5) HOLD only while a back end is there; no back end falls to WALL, a running clock is " +
                  "served, and Vrf:TaskClock=wall is unaffected");
        }

        // (e2c) ... and the HOLD is not a freeze: once the scenario runs again the task completes
        //       on the SIM clock, having aged by nothing in between.
        {
            // Flat for 200 s (well past the 60 s stale window), then advancing again.
            Func<int, double> pausedThenRunning = i => i < 200 ? 5000.0 : 5000.0 + (i - 199);
            var resumed = WalkTaskClock(pausedThenRunning, useConfirmedMode: true, honourStale: true,
                                        dur, 600, backEndPresent: true);
            Check(ref failures, resumed.Completed && resumed.Samples == 200 + (int)dur,
                  $"(Q5) ... and when the scenario runs again the task completes after its {dur:F0} SIM " +
                  $"seconds, not counting the pause (completed at sample {resumed.Samples})");
        }

        // (e2d) E1 (pass-3 review): THE WAY OUT OF A HOLD IS NOT ALWAYS A RECOVERY, and the service
        //       said it was. Its "the simulation clock is readable and advancing again" branch fires
        //       on (already warned) && !taskSimStale, and taskSimStale is heldOnSim && obs.Stale -
        //       so it fires just as surely when heldOnSim goes FALSE, which is the hysteresis path
        //       ONTO the wall clock. Those are opposite events. The sequence that reaches it is
        //       below, on the real SimClockTracker and the real StallPolicy; what the service says
        //       at each of the two exits is a log line and is checked by reading.
        {
            var tracker = new SimClockTracker();
            double wall = 1.7e9;
            SimClockTracker.Observation obs = default;
            for (int i = 0; i < 120; i++, wall += 1.0)          // readable, FLAT, back end present
                obs = tracker.Observe(5000.0, wall, StallPolicy.ModeSwitchConfirmations,
                                      StallPolicy.StaleClockWarnSeconds);
            bool heldOnSim = obs.ReadableConfirmed;
            Check(ref failures,
                  heldOnSim && obs.Stale
                  && StallPolicy.TaskClockAction(heldOnSim, heldOnSim && obs.Stale, backEndPresent: true)
                         == StallPolicy.TaskClockOnFlat.HoldOnSim,
                  $"(e2d) E1: {obs.FlatForWallSeconds:F0} wall seconds of a readable-but-flat clock with a " +
                  $"back end present is a HOLD - the state the service warns about and then has to get OUT of");

            for (int i = 0; i < StallPolicy.ModeSwitchConfirmations; i++, wall += 1.0)   // the reader goes
                obs = tracker.Observe(-1.0, wall, StallPolicy.ModeSwitchConfirmations,
                                      StallPolicy.StaleClockWarnSeconds);
            heldOnSim = obs.ReadableConfirmed;
            bool taskSimStale = heldOnSim && obs.Stale;
            Check(ref failures,
                  !heldOnSim && !taskSimStale
                  && StallPolicy.TaskClockAction(heldOnSim, taskSimStale, backEndPresent: true)
                         == StallPolicy.TaskClockOnFlat.FallBackToWall,
                  "(e2d) E1: ... and LOSING THE READER clears taskSimStale exactly as a recovery does, while " +
                  "the axis goes to the WALL clock - so one sentence for both announced \"readable and " +
                  "advancing again\" at the tick every task time left the sim clock. The service now branches " +
                  "on heldOnSim and says which of the two happened");
        }

        // (e2e) STP-809: THE BACK-END CONTROL STATE, AND THE ROW THAT USED TO BE DECIDED WRONG.
        //       Q5's predicate had exactly one input - VrfFacade::BackendCount, which is
        //       backends().count() - and the vendor's list KEEPS a back end that has missed its
        //       status timeout (DtVrfBackendListener::doTimeouts() deactivates the entry instead of
        //       removing it, vrfBackendListener.h:161-163). So a back end that died IN PLACE looked
        //       exactly like a paused one and froze every armed Duration for the rest of the run,
        //       with one WARNING a minute as the only symptom (pass-3 review E2). The facade now
        //       exposes the two readers the vendor does have - backendsControlState() and an ACTIVE
        //       count over backendList() - and the truth table below is decided on those.
        {
            const bool held = true, staleNow = true;
            const StallPolicy.BackendControl paused = StallPolicy.BackendControl.Paused;
            const StallPolicy.BackendControl running = StallPolicy.BackendControl.Running;
            const StallPolicy.BackendControl none = StallPolicy.BackendControl.NoBackend;
            const StallPolicy.BackendControl unknown = StallPolicy.BackendControl.Unknown;
            const StallPolicy.BackendControl unreadable = StallPolicy.BackendControl.Unreadable;

            // FAIL-FIRST, the row that was wrong: DEACTIVATED-ALL. The PRE-STP-809 rule is the
            // three-argument overload, and it is still here - a back end in the list is a back end,
            // so it HOLDS, whatever the vendor thinks of it.
            Check(ref failures,
                  StallPolicy.TaskClockAction(held, staleNow, backEndPresent: true)
                      == StallPolicy.TaskClockOnFlat.HoldOnSim,
                  "FAIL-FIRST (E2 / STP-809): on the BackendCount signal ALONE a back end that has been "
                + "DEACTIVATED for missing its status timeout is indistinguishable from a paused one, so "
                + "the task clock HOLDS - every armed Duration, every gate and every start delay frozen "
                + "for the rest of the run");
            // ... and the SAME row, decided on the vendor's own answers, falls back to the WALL clock:
            // the control state still says Running (it is the last one a status message carried), but
            // NOT ONE known back end is simulatable or in transition.
            Check(ref failures,
                  StallPolicy.TaskClockAction(held, staleNow, backEndPresent: true, running, activeBackends: 0)
                      == StallPolicy.TaskClockOnFlat.FallBackToWall,
                  "(STP-809) DEACTIVATED-ALL: a cached RUNNING control state with ZERO active back ends is "
                + "a back end that has DIED, not a pause - the axis falls back to the WALL clock");

            // RUNNING + FLAT is NOT a death: the status message and the simTime sample refresh on
            // different cadences (C6's blind period), so the existing hysteresis - not a flat clock -
            // is what takes a genuinely lost reader to the wall clock.
            Check(ref failures,
                  StallPolicy.TaskClockAction(held, staleNow, backEndPresent: true, running, activeBackends: 1)
                      == StallPolicy.TaskClockOnFlat.HoldOnSim,
                  "(STP-809) RUNNING + flat with an ACTIVE back end HOLDS - a flat clock alone is the C6 "
                + "blind period, and only the hysteresis may switch clocks");

            // PAUSED + FLAT is Q5's own case, now a positive reading rather than an inference.
            Check(ref failures,
                  StallPolicy.TaskClockAction(held, staleNow, backEndPresent: true, paused, activeBackends: 1)
                      == StallPolicy.TaskClockOnFlat.HoldOnSim,
                  "(STP-809) PAUSED + flat with an ACTIVE back end HOLDS - the back end SAYS it is paused "
                + "(DtPauseControlType), which is Q5's ruling confirmed rather than assumed");

            // NO BACK END: the vendor's DtUnknownControlType with an empty list. Same outcome as
            // BackendCount = 0 always gave, and now reachable through the facade for real.
            Check(ref failures,
                  StallPolicy.TaskClockAction(held, staleNow, backEndPresent: false, none, activeBackends: 0)
                      == StallPolicy.TaskClockOnFlat.FallBackToWall,
                  "(STP-809) NO BACK END at all falls back to the WALL clock, as the count rule did");

            // UNKNOWN / UNREADABLE: nothing was learned, so NOTHING CHANGES - the BackendCount rule
            // decides, including its documented blindness. This is the check that says the new signal
            // can only ever ADD information.
            Check(ref failures,
                  StallPolicy.TaskClockAction(held, staleNow, backEndPresent: true, unknown, activeBackends: -1)
                      == StallPolicy.TaskClockOnFlat.HoldOnSim
                  && StallPolicy.TaskClockAction(held, staleNow, backEndPresent: false, unknown, activeBackends: -1)
                      == StallPolicy.TaskClockOnFlat.FallBackToWall
                  && StallPolicy.TaskClockAction(held, staleNow, backEndPresent: true, unreadable, activeBackends: -1)
                      == StallPolicy.TaskClockAction(held, staleNow, backEndPresent: true)
                  && StallPolicy.TaskClockAction(held, staleNow, backEndPresent: false, unreadable, activeBackends: -1)
                      == StallPolicy.TaskClockAction(held, staleNow, backEndPresent: false),
                  "(STP-809) an UNKNOWN or UNREADABLE control state with no active-count reading is the "
                + "pre-STP-809 rule EXACTLY - a bridge that predates this change, or a reader that throws, "
                + "cannot change an outcome");

            // PRECEDENCE, stated as a test because it is the one judgement call: a ZERO active count
            // beats a Paused control state. That state is the last one a status message carried, and a
            // live paused back end still heartbeats and still has objects created on it - so it is
            // simulatable and never reaches this row.
            Check(ref failures,
                  StallPolicy.TaskClockAction(held, staleNow, backEndPresent: true, paused, activeBackends: 0)
                      == StallPolicy.TaskClockOnFlat.FallBackToWall,
                  "(STP-809) a ZERO ACTIVE COUNT beats a cached PAUSED state - a back end that died while "
                + "paused is dead, and the control state it left behind cannot say otherwise");

            // The new inputs may not reach past the two gates that come first.
            Check(ref failures,
                  StallPolicy.TaskClockAction(held, stale: false, backEndPresent: false, none, activeBackends: 0)
                      == StallPolicy.TaskClockOnFlat.ServeSim
                  && StallPolicy.TaskClockAction(heldOnSim: false, stale: true, backEndPresent: true, paused, 1)
                      == StallPolicy.TaskClockOnFlat.FallBackToWall,
                  "(STP-809) a clock that is NOT stale is still served, and Vrf:TaskClock=wall is still "
                + "unaffected - the back-end state is read only inside the stale branch");

            // THE OPERATOR-FACING SENTENCE. One clause per outcome, and the two that matter at a demo
            // must not read alike: a confirmed pause and a back end that died in place.
            string pausedClause = StallPolicy.BackendStateClause(paused, activeBackends: 1, backendCount: 1);
            string deadClause = StallPolicy.BackendStateClause(running, activeBackends: 0, backendCount: 1);
            string fallbackClause = StallPolicy.BackendStateClause(unreadable, activeBackends: -1, backendCount: 1);
            Check(ref failures,
                  pausedClause.Contains("REPORTS PAUSED") && pausedClause.Contains("active=1")
                  && deadClause.Contains("DIED") && deadClause.Contains("active=0")
                  && fallbackClause.Contains("back-end COUNT") && fallbackClause.Contains("active=unreadable"),
                  "(STP-809) the TASK CLOCK line says WHICH state it decided on - a confirmed PAUSE, a back "
                + "end that DIED in place, or a fallback to the count - and carries both numbers");
        }

        // (e2f) ... and the same row driven through the WHOLE walk, not just the predicate: the real
        //       SimClockTracker, the real axis and the real TimedCompletionPolicy, on a frozen reader
        //       with a back end that is still LISTED but no longer active. This is the case N4 said the
        //       suite could not produce - (e2) had to fake it with backEndPresent:false, a state the
        //       facade could not return - and STP-809 makes it real.
        {
            Func<int, double> frozenReader = _ => 5000.0;
            var preFixDead = WalkTaskClock(frozenReader, useConfirmedMode: true, honourStale: true,
                                           dur, 600, backEndPresent: true);
            Check(ref failures,
                  !preFixDead.Completed && preFixDead.AxisSeconds == 0.0 && preFixDead.Samples == 600,
                  $"FAIL-FIRST (STP-809): with only the back-end COUNT to go on, a DEAD back end that is "
                + $"still in the vendor's list ages a {dur:F0} s task by NOTHING across 600 wall seconds "
                + $"(axis {preFixDead.AxisSeconds:F0} s) - indistinguishable from the pause of (e2b)");
            var fixedDead = WalkTaskClock(frozenReader, useConfirmedMode: true, honourStale: true,
                                          dur, 600, backEndPresent: true,
                                          StallPolicy.BackendControl.Running, activeBackends: 0);
            Check(ref failures, fixedDead.Completed && fixedDead.StaleTransitions == 1,
                  $"(STP-809) ... and with the ACTIVE count reading zero the same walk detects it ONCE "
                + $"({fixedDead.StaleTransitions} transition(s)) and completes the task on the WALL "
                + $"fallback ({fixedDead.Samples} samples) - the run is no longer frozen");
            // The pause must still hold, through the same walk, with the state the vendor gives for one.
            var stillPaused = WalkTaskClock(frozenReader, useConfirmedMode: true, honourStale: true,
                                            dur, 600, backEndPresent: true,
                                            StallPolicy.BackendControl.Paused, activeBackends: 1);
            Check(ref failures, !stillPaused.Completed && stillPaused.AxisSeconds == 0.0,
                  $"(STP-809) ... while a back end that REPORTS PAUSED and is still active holds the same "
                + $"task for the full 600 samples (axis {stillPaused.AxisSeconds:F0} s) - Q5 is unchanged "
                + $"for the case the user ruled on");
        }

        // (e3) The axis itself: it never invents time, whatever the reader does.
        {
            var axis = new TaskClockAxis();
            axis.Advance(1000.0, usingSim: true);          // anchor
            axis.Advance(1060.0, usingSim: true);          // 60 s of scenario
            double afterRun = axis.Seconds;
            axis.Advance(1060.0, usingSim: true);          // paused
            axis.Advance(900.0, usingSim: true);           // rollbackToSnapshot
            Check(ref failures, axis.Seconds == afterRun && afterRun == 60.0,
                  $"the axis adds a pause and a rollback as ZERO (60 s served, still {axis.Seconds:F0} s)");
            axis.Advance(1.7e9, usingSim: false);          // fall back to the wall clock
            Check(ref failures, axis.Seconds == afterRun,
                  "a fall back to the WALL clock adds nothing across the change - the two epochs " +
                  "are not comparable, and the task keeps the time it has served");
            axis.Advance(1.7e9 + 30.0, usingSim: false);
            Check(ref failures, axis.Seconds == afterRun + 30.0,
                  "... and the rest of the Duration is then served in wall seconds");
        }

        // (e4) A SUSTAINED change is still adopted - the hysteresis must not blind the interface to
        //      a back end that really has gone.
        {
            var tracker = new SimClockTracker();
            double wall = 1.7e9;
            tracker.Observe(1000.0, wall, StallPolicy.ModeSwitchConfirmations, StallPolicy.StaleClockWarnSeconds);
            int adoptedAt = -1;
            for (int i = 1; i <= 6 && adoptedAt < 0; i++)
            {
                var o = tracker.Observe(-1.0, wall += 1.0, StallPolicy.ModeSwitchConfirmations,
                                        StallPolicy.StaleClockWarnSeconds);
                if (o.ModeChanged) adoptedAt = i;
            }
            Check(ref failures, adoptedAt == StallPolicy.ModeSwitchConfirmations && !tracker.ReadableConfirmed,
                  $"a SUSTAINED loss of the sim reader IS adopted, on reading " +
                  $"{StallPolicy.ModeSwitchConfirmations} (got {adoptedAt})");
        }

        // (e5) A backwards step is reported as a ROLLBACK and never as a stale clock - the two
        //      have opposite remedies (re-anchor and carry on, vs stop serving on this clock).
        {
            var tracker = new SimClockTracker();
            double wall = 1.7e9;
            tracker.Observe(1000.0, wall, StallPolicy.ModeSwitchConfirmations, StallPolicy.StaleClockWarnSeconds);
            var back = tracker.Observe(500.0, wall + 1.0, StallPolicy.ModeSwitchConfirmations,
                                       StallPolicy.StaleClockWarnSeconds);
            Check(ref failures,
                  back.Step == StallPolicy.SimClockStep.RolledBack && !back.Stale
                  && back.PreviousSimSeconds == 1000.0,
                  $"a backwards step is a ROLLBACK (1000 s -> 500 s), not a stale clock (got {back.Step})");
            var flat = tracker.Observe(500.0, wall + 1.0 + StallPolicy.StaleClockWarnSeconds,
                                       StallPolicy.ModeSwitchConfirmations, StallPolicy.StaleClockWarnSeconds);
            Check(ref failures, flat.Stale && flat.Step == StallPolicy.SimClockStep.Flat,
                  "... and the stale clock is only what stays FLAT for the whole stale window");
        }

        // ===== N7 (D7 harvest): THE SIM/WALL RATIO, MEASURED INSTEAD OF ASSUMED =====
        // D6's report recorded "sim/wall 1.00" by comparing the app's own
        // (DateTime.UtcNow - DispatchedUtc) against wall capture stamps - the SAME CLOCK on both
        // sides, so the check was circular. D7 then regressed the level-3 console rows against the
        // observer's trace clock and measured 3.0053. The interface can simply state the number;
        // SimWallRatio is that rule, and these arms are it.
        Console.WriteLine("  -- N7: SimWallRatio (the number D6 recorded without measuring)");
        {
            // A scenario running at exactly D7's measured 3.00x, sampled once a WALL second.
            var meter = new SimWallRatio();
            double wall = 1.7e9, sim = 100.0;
            var reported = new List<double>();
            for (int i = 0; i < 180; i++)
            {
                double r = meter.Observe(sim, wall);
                if (!double.IsNaN(r)) reported.Add(r);
                wall += 1.0; sim += 3.0;
            }
            Check(ref failures, reported.Count == 2 && reported.All(r => Math.Abs(r - 3.0) < 1e-9),
                  $"a scenario at 3.00x over 180 WALL s reports the ratio {reported.Count} time(s) " +
                  $"(one per {SimWallRatio.MinWindowWallSeconds:F0} s window) and every one of them is " +
                  $"3.000 - D7's measured figure, stated by the app instead of regressed out of a trace");
            Check(ref failures, double.IsNaN(new SimWallRatio().Observe(100.0, 1.7e9)),
                  "the first sample reports NOTHING - it only anchors the window");
        }
        {
            // REAL TIME is 1.000, and a PAUSED scenario is 0.000 - not "no reading". An operator
            // needs to be able to tell a paused scenario from an unreadable clock.
            var real = new SimWallRatio();
            var paused = new SimWallRatio();
            double w = 1.7e9, s = 0.0;
            double realOut = double.NaN, pausedOut = double.NaN;
            for (int i = 0; i <= 61; i++)
            {
                double a = real.Observe(s, w);
                double b = paused.Observe(500.0, w);
                if (!double.IsNaN(a)) realOut = a;
                if (!double.IsNaN(b)) pausedOut = b;
                w += 1.0; s += 1.0;
            }
            Check(ref failures, Math.Abs(realOut - 1.0) < 1e-9 && Math.Abs(pausedOut) < 1e-12,
                  $"real time reports 1.000 (got {realOut:F3}) and a PAUSED scenario reports 0.000 " +
                  $"(got {pausedOut:F3}) - a flat clock has a ratio and it is zero, which is exactly " +
                  "what an operator must be able to see");
        }
        {
            // An UNREADABLE stretch has no ratio at all, and a ROLLBACK must not manufacture a
            // negative one: both abandon the window rather than price it.
            var meter = new SimWallRatio();
            double w = 1.7e9;
            bool reportedAcrossGap = false;
            for (int i = 0; i < 30; i++) { if (!double.IsNaN(meter.Observe(100.0 + i, w))) reportedAcrossGap = true; w += 1.0; }
            for (int i = 0; i < 5; i++) { if (!double.IsNaN(meter.Observe(double.NaN, w))) reportedAcrossGap = true; w += 1.0; }
            for (int i = 0; i < 40; i++) { if (!double.IsNaN(meter.Observe(200.0 + i, w))) reportedAcrossGap = true; w += 1.0; }
            Check(ref failures, !reportedAcrossGap,
                  "a window broken by an UNREADABLE stretch is abandoned, not priced - 35 s of readings " +
                  "either side of a 5 s gap report nothing, because the sim seconds across the gap are " +
                  "not evidence of a rate");
            var rb = new SimWallRatio();
            double w2 = 1.7e9;
            bool reportedAcrossRollback = false;
            for (int i = 0; i < 40; i++) { if (!double.IsNaN(rb.Observe(1000.0 + i, w2))) reportedAcrossRollback = true; w2 += 1.0; }
            for (int i = 0; i < 40; i++) { if (!double.IsNaN(rb.Observe(500.0 + i, w2))) reportedAcrossRollback = true; w2 += 1.0; }
            Check(ref failures, !reportedAcrossRollback,
                  "and a ROLLBACK (1000 s -> 500 s, DtVrfRemoteController::rollbackToSnapshot) abandons " +
                  "the window too - a negative ratio would be an artefact of a new timeline, not a rate");
        }
        {
            // The windows are DISJOINT, so a rate CHANGE shows up instead of being averaged away.
            var meter = new SimWallRatio();
            double w = 1.7e9, s = 0.0;
            var seen = new List<double>();
            for (int i = 0; i < 61; i++) { double r = meter.Observe(s, w); if (!double.IsNaN(r)) seen.Add(r); w += 1.0; s += 6.0; }
            for (int i = 0; i < 61; i++) { double r = meter.Observe(s, w); if (!double.IsNaN(r)) seen.Add(r); w += 1.0; s += 1.0; }
            Check(ref failures, seen.Count == 2 && Math.Abs(seen[0] - 6.0) < 0.2 && Math.Abs(seen[1] - 1.0) < 0.2,
                  $"the windows are DISJOINT: a run at 6x for a minute then 1x for a minute reports " +
                  $"[{string.Join(", ", seen.Select(x => x.ToString("F2")))}] and not one averaged " +
                  "figure - a running average would hide exactly the change worth seeing");
        }
    }

    /// <summary>
    /// One run of the service's OWN timed-completion loop, on a scripted sim reader: the sampler
    /// (SimClockTracker), the axis (TaskClockAxis) and the walk (TimedCompletionPolicy), sampled
    /// once per WALL second exactly as SampleTaskClock does. <paramref name="useConfirmedMode"/>
    /// false and <paramref name="honourStale"/> false reproduce the PRE-FIX behaviours of M3 and M4.
    /// </summary>
    /// <param name="backEndPresent">Q5: what VrfFacade::BackendCount would say. True = a back end
    /// is still there, so a flat clock is a PAUSE and the axis holds.</param>
    /// <param name="control">STP-809: what VrfFacade::BackendControlState would say. The default,
    /// Unreadable, together with activeBackends -1 is the PRE-STP-809 walk exactly - the
    /// BackendCount rule and nothing else - so every check written before STP-809 still measures
    /// what it was written to measure.</param>
    /// <param name="activeBackends">STP-809: what VrfFacade::ActiveBackendCount would say; -1 is
    /// no reading, 0 is "not one known back end is simulatable or in transition".</param>
    private static (bool Completed, int Samples, double AxisSeconds, int ModeFlips, int StaleTransitions)
        WalkTaskClock(Func<int, double> reader, bool useConfirmedMode, bool honourStale,
                      double durationSeconds, int maxSamples, bool backEndPresent = false,
                      StallPolicy.BackendControl control = StallPolicy.BackendControl.Unreadable,
                      int activeBackends = -1)
    {
        var tracker = new SimClockTracker();
        var axis = new TaskClockAxis();
        var timed = new TimedCompletionPolicy();
        timed.Register("T-CLOCK", "taskee-clock", "T_Secure", "1-35 AR", durationSeconds);
        double wall = 1.7e9;            // a plausible wall epoch, in seconds
        int modeFlips = 0, staleTransitions = 0, samples = 0;
        bool lastStale = false, completed = false;
        for (; samples < maxSamples && !completed; samples++, wall += 1.0)
        {
            var obs = tracker.Observe(reader(samples), wall, StallPolicy.ModeSwitchConfirmations,
                                      StallPolicy.StaleClockWarnSeconds);
            // The FIRST resolution (mode 0 -> readable/not) is adopted at once and is not a
            // flip - nothing has been served yet. Only later changes can starve the axis.
            if (obs.ModeChanged && samples > 0) modeFlips++;
            bool stale = honourStale && obs.Stale;
            if (stale != lastStale) { staleTransitions++; lastStale = stale; }
            bool heldOnSim = useConfirmedMode ? obs.ReadableConfirmed : obs.Readable;
            if (heldOnSim && !obs.Readable) continue;      // nothing to read this sample
            // The SERVICE's own decision (Q5), not a copy of it: SampleTaskClock calls this.
            var action = StallPolicy.TaskClockAction(heldOnSim, stale, backEndPresent,
                                                     control, activeBackends);
            bool usingSim = action != StallPolicy.TaskClockOnFlat.FallBackToWall;
            axis.Advance(usingSim ? obs.SimSeconds : wall, usingSim);
            if (timed.Advance(axis.Seconds, usingSim: true).Count > 0) completed = true;
        }
        return (completed, samples, axis.Seconds, modeFlips, staleTransitions);
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

        // (c4b) Q4 (USER RULING 2026-09-14): NO DURATION **AND** NO GEOMETRY IS MALFORMED, AND IS
        //       REFUSED. The supervisor default invented Vrf:DefaultHoldSeconds (60 s) so the chain
        //       would proceed; the user ruled that a number which is not in the order is not ours
        //       to invent. The rule is scoped to the kind that issues NO vendor task at all.
        {
            Check(ref failures,
                  TaskDispatchPolicy.IsMalformedZeroGeometryTask(ZeroGeometryAction.ExecuteInPlace, 0L)
                  && TaskDispatchPolicy.IsMalformedZeroGeometryTask(ZeroGeometryAction.ExecuteInPlace, -1L)
                  && !TaskDispatchPolicy.IsMalformedZeroGeometryTask(ZeroGeometryAction.ExecuteInPlace, 4800_000L),
                  "Q4: a zero-geometry task with no Duration is MALFORMED; the same task WITH a Duration is not");
            Check(ref failures,
                  !TaskDispatchPolicy.IsMalformedZeroGeometryTask(ZeroGeometryAction.EngageInPlace, 0L)
                  && !TaskDispatchPolicy.IsMalformedZeroGeometryTask(ZeroGeometryAction.BreachInPlace, 0L)
                  && !TaskDispatchPolicy.IsMalformedZeroGeometryTask(ZeroGeometryAction.Refuse, 0L),
                  "Q4: an ENGAGE or BREACH in place is NOT malformed without a Duration - it has a resolved " +
                  "target, so the vendor reports when it is done");
            Check(ref failures,
                  TaskDispatchPolicy.MalformedZeroGeometryRefusal
                      == "MALFORMED: the order gives this task NEITHER a Duration NOR any geometry (no " +
                         "MapGraphicID and no Location), so nothing in the order could ever end it and " +
                         "nothing in the simulation could ever evidence it",
                  "Q4: the refusal names BOTH missing elements, so the order can be fixed from the report");

            // FAIL-FIRST + the new behaviour, on the real sequencer: the OLD code armed an invented
            // 60 s hold and released the successor at it; the NEW code abandons the task, and the
            // successor is skipped at once with its own TASKABRT.
            var seqOld = new TaskSequencer();
            var timedOld = new TimedCompletionPolicy();
            const string malformed = "T_MALFORMED";
            seqOld.NotifyDispatched(malformed, TaskClock.Wall.Now());   // the axis the gate below reads
            Check(ref failures,
                  timedOld.Register(malformed, "taskee", malformed, "A/6-56 ADA", 60.0),
                  "Q4 FAIL-FIRST: the pre-ruling code armed an INVENTED 60 s end time for a malformed task");
            var oldSuccessor = seqOld.WaitForStartAsync(malformed, 0, 0, 600.0, TaskClock.Wall,
                                                        CancellationToken.None, 86400.0);
            timedOld.Advance(0.0, usingSim: false);
            foreach (var d in timedOld.Advance(60.0, usingSim: false)) seqOld.CompleteTask(d.TaskUuid);
            Check(ref failures,
                  oldSuccessor.Wait(TimeSpan.FromSeconds(2)) && oldSuccessor.Result == GateResult.Proceed,
                  "Q4 FAIL-FIRST: ... and its successor then dispatched on a completion the ORDER never " +
                  "authorised - the chain ran on an invented number");

            var seq = new TaskSequencer();
            var successor = seq.WaitForStartAsync(malformed, 0, 0, 600.0, TaskClock.Wall,
                                                  CancellationToken.None, 86400.0);
            Thread.Sleep(50);
            Check(ref failures, !successor.IsCompleted, "Q4: the successor waits while nothing has happened");
            seq.NotifyAbandoned(malformed);          // what the refusal now does, beside the TASKABRT
            Check(ref failures,
                  successor.Wait(TimeSpan.FromSeconds(2))
                  && successor.Result == GateResult.PredecessorAbandoned,
                  "Q4: the refusal ABANDONS the malformed task, so its successor is skipped immediately " +
                  "with its own TASKABRT rather than waiting out the gate");
        }

        // (c5) THE SUCCESSOR IS NOT SKIPPED. The in-place task is dispatched (not abandoned) and
        //      closed by R4's end time, so the STREND gate releases exactly as for a moving task.
        {
            var seq = new TaskSequencer();
            var timed = new TimedCompletionPolicy();
            const string inPlace = "T9";
            seq.NotifyDispatched(inPlace, TaskClock.Wall.Now());  // what MarkDispatched does
            timed.Register(inPlace, "taskee-9", "T9_ProvideAirDefenseCoverage", "A/6-56 ADA", 300.0);
            var successor = seq.WaitForStartAsync(inPlace, 0, 0, 5.0, TaskClock.Wall,
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

    // ---------------------------------------------------------------- R3 ----
    private static void R3(ref int failures)
    {
        // (d1) THE DISCRIMINATING CHECK. STP sets AffectedEntity to the performing unit on all 42
        //      COA-STP1 tasks (C2SimXmlBuilder.cs:427-429). That is not an error and not "no
        //      target": the task's geometry is the objective.
        var self = TaskDispatchPolicy.ForTarget(hasAffectedEntity: true, resolved: true, isSelf: true);
        Check(ref failures, self == TargetResolution.SelfIsObjective,
              $"an ATTACK whose AffectedEntity IS the taskee resolves to the OBJECTIVE (got {self})");
        Check(ref failures, !TaskDispatchPolicy.RefusesForTarget(self)
                         && TaskDispatchPolicy.FallsBackToGeometry(self),
              "... is dispatched (never refused) and routed to the task's own geometry");

        // (d2) WHERE EACH RESOLUTION SENDS THE TASK. The old check here asserted that
        //      RefusesForTarget is false for every enum value while the method body IS
        //      "return false" - it could not fail, and n8 of the cold-start review of 5c67d41
        //      called it out. What is worth checking is the MAPPING, which has four arms and can
        //      be got wrong: exactly ONE resolution names an entity to VR-Forces, and the other
        //      three route the task to its own geometry.
        var toGeometry = Enum.GetValues<TargetResolution>()
                             .Where(TaskDispatchPolicy.FallsBackToGeometry).ToArray();
        Check(ref failures,
              toGeometry.Length == 3
              && toGeometry.Contains(TargetResolution.SelfIsObjective)
              && toGeometry.Contains(TargetResolution.Unresolved)
              && toGeometry.Contains(TargetResolution.NoTarget)
              && !toGeometry.Contains(TargetResolution.DistinctEntity),
              $"exactly one target resolution (DistinctEntity) is named to VR-Forces; the other " +
              $"{toGeometry.Length} route the task to its own geometry");

        // (d3) A self-targeted ATTACK that ALSO carries no geometry executes in place (R2 + R3
        //      together) - the T9-T12 shape, which used to be a refusal AND a chain abandon.
        Check(ref failures,
              TaskDispatchPolicy.ForZeroGeometry(performerResolved: true,
                                                 hasAttackTarget: false, hasBreachTarget: false)
                  == ZeroGeometryAction.ExecuteInPlace,
              "a self-targeted ATTACK with no geometry executes in place, not refused");

        // (d4) A genuinely distinct target is unchanged: it is still named to VR-Forces.
        Check(ref failures,
              TaskDispatchPolicy.ForTarget(true, resolved: true, isSelf: false) == TargetResolution.DistinctEntity
              && !TaskDispatchPolicy.FallsBackToGeometry(TargetResolution.DistinctEntity),
              "a DISTINCT resolved target is still engaged as an entity (FireAtTarget / Breach)");

        // (d5) An out-of-scope or absent target routes to the location form at the objective
        //      rather than producing a degraded-capability warning about a missing entity.
        Check(ref failures,
              TaskDispatchPolicy.ForTarget(true, resolved: false, isSelf: false) == TargetResolution.Unresolved
              && TaskDispatchPolicy.ForTarget(false, resolved: false, isSelf: false) == TargetResolution.NoTarget
              && TaskDispatchPolicy.FallsBackToGeometry(TargetResolution.Unresolved)
              && TaskDispatchPolicy.FallsBackToGeometry(TargetResolution.NoTarget),
              "an unresolved or absent target falls back to the task's geometry, not to a refusal");
    }

    // ---------------------------------------------------------------- R1 ----
    private static void R1(ref int failures)
    {
        // The init's graphics, as the service registers them: one AREA (a square around
        // 34.5 / -116.5, centroid exactly at its centre) and one LINE.
        const string objMadison = "11111111-2222-3333-4444-555555555555";
        const string plBlue = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
        const string cpTango = "12121212-3434-5656-7878-909090909090";
        var graphics = new Dictionary<string, TaskGraphic>(StringComparer.Ordinal)
        {
            [objMadison] = new TaskGraphic(objMadison, "OBJ_MADISON", TaskGraphic.KindArea,
                new[] { (34.4, -116.6, (double?)null), (34.6, -116.6, (double?)null),
                        (34.6, -116.4, (double?)null), (34.4, -116.4, (double?)null) }),
            // M5: the init's 41 LINEs and 317 POINTs are registered for R1 resolution too, and
            // INDEPENDENTLY of Vrf:CreateInitLines / Vrf:CreateInitPoints - this map holds
            // authored points, not VR-Forces objects.
            [plBlue] = new TaskGraphic(plBlue, "PL_BLUE", TaskGraphic.KindLine,
                new[] { (35.0, -117.0, (double?)null), (35.1, -117.1, (double?)null) }),
            [cpTango] = new TaskGraphic(cpTango, "CP_TANGO", TaskGraphic.KindPoint,
                new[] { (36.25, -118.75, (double?)null) }),
        };
        var embedded = new List<(double Lat, double Lon, double? Elev)>
            { (33.0, -115.0, null), (33.1, -115.1, null) };

        // (e1) A MapGraphicID that matches an init graphic resolves to THAT graphic's geometry -
        //      an area to its centroid - and says so.
        {
            var task = new OrderTask
            {
                TaskName = "T2_PL_OBJ_MADISON",
                MapGraphicUuid = objMadison,
                MapGraphicUuids = new[] { objMadison },
                Points = new List<(double, double, double?)>(embedded),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            bool centroid = r.Points.Count == 1
                            && Math.Abs(r.Points[0].Lat - 34.5) < 1e-9
                            && Math.Abs(r.Points[0].Lon + 116.5) < 1e-9;
            Check(ref failures, r.Source == GeometrySource.MapGraphic && centroid,
                  $"a MapGraphicID matching an init AREA resolves to its centroid " +
                  $"(source {r.Source}, {r.Points.Count} point(s))");
            // The line still names the id and the graphic. SF9 re-worded it: an AREA is now
            // labelled what it IS - the route's DESTINATION - instead of an anonymous "geometry
            // from". The assertion is TIGHTER than before (it pins the role as well as the names).
            Check(ref failures, r.Log.Any(l => l.Contains("destination from MapGraphicID")
                                            && l.Contains(objMadison) && l.Contains("OBJ_MADISON")
                                            && l.Contains("area")),
                  "... and logs which id supplied it and in what ROLE (\"destination from MapGraphicID " +
                  "<uuid> -> <name> (area, ...)\")");
        }

        // (e2) SEVERAL MapGraphicIDs ON ONE TASK - SF9 (cold-start review of 9d67f97).
        //
        //      THIS CHECK USED TO ASSERT THE DEFECT. It pinned "the sequence of their geometries"
        //      in MapGraphicID DOCUMENT ORDER, which on the real STP export produced routes that
        //      drive out, back through the taskee's own start, and out again - T12 at 147.2 km for
        //      a ~50 km advance. The rule that replaces it, with its schema and doctrine citations,
        //      is TaskGeometryResolver.AssembleRoute: LINES supply the path, POINTS and AREAS
        //      supply the destination and go LAST, and lines are chained by continuity, never by
        //      document order. The assertion below is correspondingly STRONGER - it pins the ROLES
        //      and the resulting order, not just a vertex count.
        {
            var task = new OrderTask
            {
                TaskName = "T_Multi",
                MapGraphicUuids = new[] { objMadison, plBlue },   // an AREA named BEFORE a LINE
                Points = new List<(double, double, double?)>(),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.MapGraphic && r.Points.Count == 3
                             && Math.Abs(r.Points[0].Lat - 35.0) < 1e-9
                             && Math.Abs(r.Points[1].Lat - 35.1) < 1e-9
                             && Math.Abs(r.Points[2].Lat - 34.5) < 1e-9,
                  $"several MapGraphicIDs resolve BY KIND, not by document order: the LINE's vertices are the " +
                  $"path and the AREA - named FIRST in the order - is the DESTINATION and lands LAST " +
                  $"(got {r.Points.Count} vertices, last at {(r.Points.Count > 0 ? r.Points[^1].Lat : 0):F2})");
        }

        // (e3) NO MapGraphicID - every COA-STP1 task today - uses the embedded Location, which is
        //      valid C2SIM and stays supported, and the line says which path was taken.
        {
            var task = new OrderTask
            {
                TaskName = "T1_AOA_SE_1-35_AR",
                Points = new List<(double, double, double?)>(embedded),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.EmbeddedLocation && r.Points.Count == 2
                             && Math.Abs(r.Points[0].Lat - 33.0) < 1e-9,
                  $"no MapGraphicID -> the embedded Location, unchanged (source {r.Source})");
            Check(ref failures, r.Log.Any(l => l.Contains("geometry from embedded Location")
                                            && l.Contains("STP-801")),
                  "... and logs \"geometry from embedded Location (no MapGraphicID - STP-801)\"");
        }

        // (e4) A MapGraphicID that matches NOTHING falls back to the embedded Location, says the
        //      id matched nothing, and still carries the STP-801 marker.
        {
            var task = new OrderTask
            {
                TaskName = "T_Unknown_Graphic",
                MapGraphicUuids = new[] { "00000000-0000-0000-0000-000000000000" },
                Points = new List<(double, double, double?)>(embedded),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.EmbeddedLocation && r.Points.Count == 2,
                  $"an unmatched MapGraphicID falls back to the embedded Location (source {r.Source})");
            // The wording changed 2026-09-20 with the map: it is no longer "in the initialization",
            // because the ORDER may publish the graphic too (and the real STP export publishes ALL
            // of them there). One warning naming every unresolved id, not one line per id.
            Check(ref failures, r.Warnings.Count == 1
                             && r.Warnings[0].Contains("matched NO registered graphic")
                             && r.Warnings[0].Contains("DANGLING")
                             && r.Log.Any(l => l.Contains("geometry from embedded Location")
                                            && l.Contains("STP-801")),
                  $"... and WARNS ONCE that the id matched nothing (M5), naming it, with the STP-801 " +
                  $"marker on the fallback ({r.Warnings.Count} warning(s))");
        }

        // (e7) M5 - A MapGraphicID NAMING A LINE resolves to that line's vertices, in order, with
        //      no area centroid collapse: a phase line or an axis of advance is a PATH.
        {
            var task = new OrderTask
            {
                TaskName = "T_Move_Along_PL_BLUE",
                MapGraphicUuids = new[] { plBlue },
                Points = new List<(double, double, double?)>(),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.MapGraphic && r.Points.Count == 2
                             && Math.Abs(r.Points[0].Lat - 35.0) < 1e-9
                             && Math.Abs(r.Points[1].Lat - 35.1) < 1e-9
                             && r.Warnings.Count == 0,
                  $"a MapGraphicID naming a LINE resolves to its {r.Points.Count} vertices in order " +
                  $"(source {r.Source})");
            Check(ref failures, r.Log.Any(l => l.Contains("PL_BLUE") && l.Contains(TaskGraphic.KindLine)),
                  "... and names the line and its kind in the log");
        }

        // (e8) M5 - A MapGraphicID NAMING A POINT resolves to that one authored position.
        {
            var task = new OrderTask
            {
                TaskName = "T_Occupy_CP_TANGO",
                MapGraphicUuids = new[] { cpTango },
                Points = new List<(double, double, double?)>(),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.MapGraphic && r.Points.Count == 1
                             && Math.Abs(r.Points[0].Lat - 36.25) < 1e-9
                             && Math.Abs(r.Points[0].Lon + 118.75) < 1e-9,
                  $"a MapGraphicID naming a POINT resolves to its authored position (source {r.Source}, " +
                  $"{r.Points.Count} point(s))");
        }

        // (e9) M5 - THE SILENT CASE THE WARNING EXISTS FOR: an unmatched id and NO embedded
        //      Location. Before M5 this fell through to R2 in place with an Information line; the
        //      unit holds still for the whole Duration and the order looks executed.
        {
            var task = new OrderTask
            {
                TaskName = "T_Only_An_Unknown_Graphic",
                MapGraphicUuids = new[] { "00000000-0000-0000-0000-000000000000" },
                Points = new List<(double, double, double?)>(),
            };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.None
                             && r.Warnings.Any(l => l.Contains("matched NO registered graphic"))
                             && r.Warnings.Any(l => l.Contains("executed IN PLACE")),
                  $"an unmatched MapGraphicID with no embedded Location WARNS that the task will be " +
                  $"executed in place ({r.Warnings.Count} warning(s))");
        }

        // (e5) NO geometry at all is still no geometry - R2 executes it in place.
        {
            var task = new OrderTask { TaskName = "T9_ProvideAirDefenseCoverage" };
            var r = TaskGeometryResolver.Resolve(task, graphics);
            Check(ref failures, r.Source == GeometrySource.None && r.Points.Count == 0,
                  "a task with neither a MapGraphicID nor a Location has no geometry (R2 takes it)");
        }

        // (e6) THE PARSER lifts every MapGraphicID the schema allows, not just the first.
        {
            var parsed = OrderParser.Parse(TwoGraphicOrderXml(objMadison, plBlue));
            Check(ref failures, parsed.Tasks.Count == 1 && parsed.Tasks[0].MapGraphicUuids.Count == 2
                             && parsed.Tasks[0].MapGraphicUuids[0] == objMadison
                             && parsed.Tasks[0].MapGraphicUuids[1] == plBlue
                             && parsed.Tasks[0].MapGraphicUuid == objMadison,
                  "OrderParser lifts ALL MapGraphicIDs in document order (and keeps the first)");
        }
    }

    /// <summary>A minimal, schema-shaped order carrying two MapGraphicIDs on one task.</summary>
    private static string TwoGraphicOrderXml(string firstUuid, string secondUuid) =>
        "<OrderBody xmlns=\"http://www.sisostds.org/schemas/C2SIM/1.1\">"
        + "<OrderID>rulings-selftest</OrderID>"
        + "<Task><ManeuverWarfareTask>"
        + "<MapGraphicID>" + firstUuid + "</MapGraphicID>"
        + "<MapGraphicID>" + secondUuid + "</MapGraphicID>"
        + "<Name>T_TwoGraphics</Name>"
        + "<UUID>99999999-9999-9999-9999-999999999999</UUID>"
        + "<PerformingEntity>88888888-8888-8888-8888-888888888888</PerformingEntity>"
        + "<TaskActionCode>ATTACK</TaskActionCode>"
        + "</ManeuverWarfareTask></Task>"
        + "</OrderBody>";


    // ------------------------------------------- THE STREND CHAIN TOPOLOGY ----
    // A1 (cold-start review of `0c96f50`, pass 2). A GATE IS A GRAPH, NOT A PAIR. Every other
    // check in this file looks at ONE gate whose predecessor is ALREADY DISPATCHED, so phase 1
    // of the gate - "the predecessor must dispatch at all" - was never exercised and a defect
    // worth half the order survived a green suite. COA-STP1's 42 tasks are 11 SERIAL CHAINS up
    // to four tasks deep; `HandleOrder` fires every task's orchestration in ONE loop, so all 42
    // gates start waiting at ORDER RECEIPT. Phase 1's window was the same window phase 2 uses -
    // derived from the predecessor's own DURATION - and it therefore knew nothing about the
    // predecessor's LEAD TIME: a d2 task demanded that its predecessor dispatch within 4,860 s
    // while that predecessor was itself waiting out a 7,200 s root, so 21 of the 42 tasks were
    // skipped with TASKABRT at every shipped setting.
    //
    // THE RULE THESE CHECKS LOCK: when the predecessor NAMES A TASK IN THIS ORDER, phase 1 has
    // no timeout of its own - every dispatch dead end calls NotifyAbandoned, so a successor
    // still fails FAST on a real one - and only a generous absolute backstop
    // (Vrf:TaskChainBackstopSeconds) bounds it. A DANGLING predecessor reference keeps the
    // configured window, because nothing will ever abandon a task that does not exist.
    //
    // Everything below runs the REAL TaskSequencer, TimedCompletionPolicy and TaskDispatchPolicy
    // over the WHOLE graph on one monotone clock, reproducing MarkDispatched's order
    // (NotifyDispatched, then Register, then the anchoring walk). The PRE-FIX rule - phase 1
    // measured from order receipt on the completion window - is kept beside each case as the
    // FAIL-FIRST control, because it is exactly what the sequencer does when the caller hands it
    // the same number twice.
    private static void ChainTopology(ref int failures)
    {
        const double configured = 600.0;       // the shipped Vrf:TaskPredecessorTimeoutSeconds
        const double demoConfigured = 7200.0;  // appsettings.Demo.json's overlay
        const double margin = 60.0;            // the shipped Vrf:TaskPredecessorEndMarginSeconds
        const double backstop = 86400.0;       // the shipped Vrf:TaskChainBackstopSeconds
        // Every authored time in COA-STP1 (4,800 s, 7,200 s, 12,000 s) is a whole multiple of
        // 1,200 s, and of 120 s once scaled by 0.05, so a walk in those steps lands EXACTLY on
        // every end time instead of observing it late - the walk's granularity never eats the
        // gate's margin.
        const double step = 1200.0;
        const double compressedStep = 120.0;

        // (f1) A FOUR-DEEP CHAIN, every gate started at order receipt. This is COA-STP1's shape:
        //      a PT2H root and three PT1H20M successors on one taskee.
        {
            var chain = SerialChain(7200_000L, 4800_000L, 4800_000L, 4800_000L);
            var preFix = WalkChain(chain, configured, margin, 1.0, backstop, step, preFixPhase1: true);
            Check(ref failures,
                  preFix.Dispatched == 2 && preFix.SkippedCount == 2
                  && preFix.Result("T3") == GateResult.PredecessorNeverDispatched,
                  $"FAIL-FIRST (A1): with phase 1 measured from order receipt on the COMPLETION window, " +
                  $"the same chain dispatches only {preFix.Dispatched} of 4 - T3's 4,860 s window expires " +
                  $"2,340 s before T2 dispatches, and T4 dies on T3's abandon " +
                  $"({preFix.Times("T1", "T2", "T3", "T4")})");

            var run = WalkChain(chain, configured, margin, 1.0, backstop, step);
            Check(ref failures,
                  run.Dispatched == 4 && run.SkippedCount == 0
                  && run.At("T1") == 0.0 && run.At("T2") == 7200.0
                  && run.At("T3") == 12000.0 && run.At("T4") == 16800.0,
                  $"(i) A1: a 4-deep chain whose gates all start at ORDER RECEIPT dispatches all four " +
                  $"at their predecessors' TIMED completions - expected 0 / 7200 / 12000 / 16800, got " +
                  $"{run.Times("T1", "T2", "T3", "T4")} ({run.SkippedCount} skipped)");

            var demo = WalkChain(chain, demoConfigured, margin, 1.0, backstop, step);
            Check(ref failures, demo.Dispatched == 4 && demo.SkippedCount == 0,
                  $"(i) ... and the same chain under the Demo overlay's 7200 s floor: " +
                  $"{demo.Dispatched} of 4 dispatched, {demo.SkippedCount} skipped");
        }

        // (f2) A DELAYED ROOT. COA-STP1's T13 carries a PT3H20M start delay (12,000 s) and one
        //      successor, T14, whose window is derived from T13's 4,800 s Duration - 7,140 s
        //      before T13 dispatches at all.
        {
            var pair = new[] { new ChainTask("T13", "", 4800_000L, 12000_000L),
                               new ChainTask("T14", "T13", 4800_000L, 0L) };
            var preFix = WalkChain(pair, configured, margin, 1.0, backstop, step, preFixPhase1: true);
            Check(ref failures,
                  preFix.Dispatched == 1 && preFix.Result("T14") == GateResult.PredecessorNeverDispatched,
                  $"FAIL-FIRST (A1): with the pre-fix rule T14's 4,860 s window expires 7,140 s before T13 " +
                  $"dispatches ({preFix.Times("T13", "T14")})");

            var run = WalkChain(pair, configured, margin, 1.0, backstop, step);
            Check(ref failures,
                  run.Dispatched == 2 && run.SkippedCount == 0
                  && run.At("T13") == 12000.0 && run.At("T14") == 16800.0,
                  $"(ii) A1: T13's 12,000 s start delay does not skip T14 - expected 12000 / 16800, got " +
                  $"{run.Times("T13", "T14")} ({run.SkippedCount} skipped)");
        }

        // (f3) A DANGLING predecessor reference - a uuid no task in this order carries. NOTHING
        //      will ever dispatch or abandon it, so this is the ONE case the configured window
        //      still has to bound, and it must still expire at exactly that value.
        {
            var dangling = new[] { new ChainTask("T1", "no-such-task-uuid", 4800_000L, 0L) };
            var run = WalkChain(dangling, configured, margin, 1.0, backstop, 60.0);
            Check(ref failures,
                  run.Dispatched == 0 && run.SkippedCount == 1
                  && run.Result("T1") == GateResult.PredecessorNeverDispatched
                  && run.SkippedAtSeconds("T1") == configured,
                  $"(iii) a DANGLING predecessor still times out at the configured " +
                  $"{configured:F0} s (got {run.Times("T1")} at {run.SkippedAtSeconds("T1"):F0} s)");
        }

        // (f4) AN ABANDONED predecessor must still fail its successor FAST. This is what pays for
        //      dropping phase 1's timeout: every dispatch dead end in the service calls
        //      NotifyAbandoned, so the backstop is never what ends a chain that really died.
        {
            var clock = new StepClock();
            var seq = new TaskSequencer();
            var gate = seq.WaitForStartAsync("PRED-A1", 0, 0, backstop, clock.AsTaskClock(),
                                             CancellationToken.None);
            Thread.Sleep(50);
            Check(ref failures, !gate.IsCompleted,
                  "(iv) a gate whose predecessor has NOT dispatched waits (it does not proceed on its own)");
            seq.NotifyAbandoned("PRED-A1");
            bool done = gate.Wait(TimeSpan.FromSeconds(5));
            Check(ref failures, done && gate.Result == GateResult.PredecessorAbandoned && clock.Now == 0.0,
                  $"(iv) ... and an ABANDONED predecessor fails it FAST - 0 s of clock spent, not the " +
                  $"{backstop:F0} s backstop (got {(done ? gate.Result.ToString() : "still waiting")} at " +
                  $"{clock.Now:F0} s)");
        }

        // (f6) A SUPERSEDED predecessor kills its successors AT THE SUPERSEDE POINT (B1 of the
        //      pass-2 review; Q1, USER RULING 2026-09-14). VR-Forces runs one task per unit, so a
        //      new dispatch REPLACES the running one and the interface reports TASKABRT for it -
        //      and until this fix said nothing to the gate, so the successors of a task it had
        //      just declared NOT PERFORMED waited out the whole derived window (up to 7,260 s)
        //      before being skipped anyway.
        {
            Check(ref failures,
                  TaskDispatchPolicy.SupersedeAbandonsSuccessors("TASKABRT")
                  && TaskDispatchPolicy.SupersedeAbandonsSuccessors(null)
                  && TaskDispatchPolicy.SupersedeAbandonsSuccessors("")
                  && !TaskDispatchPolicy.SupersedeAbandonsSuccessors("TASKCMPLT")
                  && !TaskDispatchPolicy.SupersedeAbandonsSuccessors("  taskcmplt  "),
                  "(vi) the DEFAULT supersede code abandons the successors; TASKCMPLT - the reading that " +
                  "keeps the armed end time - does not, at any casing or spacing");

            var clock = new StepClock();
            var seq = new TaskSequencer();
            var timed = new TimedCompletionPolicy();
            const string pred = "PRED-SUPERSEDED";
            seq.NotifyDispatched(pred, clock.Now);
            timed.Register(pred, "taskee", pred, "1-35 AR", 4800.0);
            timed.Advance(clock.Now, usingSim: true);
            double window = TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, 4800.0, margin);
            var gate = seq.WaitForStartAsync(pred, 0, 0, window, clock.AsTaskClock(),
                                             CancellationToken.None, backstop);
            Thread.Sleep(50);
            Check(ref failures, !gate.IsCompleted && window >= 4860.0,
                  $"(vi) FAIL-FIRST: while the supersede says nothing to the gate, the successor is still " +
                  $"waiting out its {window:F0} s window (the pre-B1 behaviour)");

            // What MarkDispatched now does at the supersede point, in order: report TASKABRT for
            // the old task (which cancels its armed end time) and tell the sequencer it is dead.
            Check(ref failures, TimedCompletionPolicy.CancelsTimer(S.TaskStatusCodeType.TASKABRT)
                             && timed.Cancel(pred),
                  "(vi) the supersede TASKABRT cancels the old task's armed end time");
            seq.NotifyAbandoned(pred);
            bool done = gate.Wait(TimeSpan.FromSeconds(5));
            Check(ref failures, done && gate.Result == GateResult.PredecessorAbandoned && clock.Now == 0.0,
                  $"(vi) ... and the successor is skipped IMMEDIATELY - PredecessorAbandoned with 0 s of " +
                  $"clock spent, which the service reports as its own TASKABRT (got " +
                  $"{(done ? gate.Result.ToString() : "still waiting")} at {clock.Now:F0} s)");
            Check(ref failures, timed.Advance(clock.Now + 100000.0, usingSim: true).Count == 0,
                  "(vi) ... and the superseded task never reports a later timed TASKCMPLT");
        }

        // (f7) B4: THE LONG WINDOW IS DERIVED FROM AN END TIME THAT MUST ACTUALLY EXIST. With
        //      Vrf:TimedCompletion OFF - the documented evidence-only escape hatch - nothing is
        //      ever armed, so a window derived from the predecessor's Duration only made the
        //      eventual skip eight times slower and quieter than the operator had configured.
        {
            Check(ref failures,
                  TaskDispatchPolicy.PredecessorEndSeconds(false, true, 4800_000L, 1.0) == 0.0
                  && TaskDispatchPolicy.PredecessorEndSeconds(true, true, 4800_000L, 1.0) == 4800.0
                  && TaskDispatchPolicy.PredecessorTimeoutSeconds(
                         configured, TaskDispatchPolicy.PredecessorEndSeconds(false, true, 4800_000L, 1.0),
                         margin) == configured,
                  "(vii) B4: with Vrf:TimedCompletion OFF the predecessor has no armed end time, so the gate " +
                  "is the configured floor; with it ON the Duration raises it");

            var chain = SerialChain(7200_000L, 4800_000L, 4800_000L, 4800_000L);
            var preB4 = WalkChain(chain, configured, margin, 1.0, backstop, 60.0,
                                  timedCompletion: false, derivesFromDuration: true);
            Check(ref failures,
                  preB4.Dispatched == 1 && preB4.SkippedAtSeconds("T2") == 7260.0,
                  $"(vii) FAIL-FIRST (B4): deriving the window anyway makes the skip wait {preB4.SkippedAtSeconds("T2"):F0} s " +
                  $"for a completion that can never come ({preB4.Dispatched} of 4 dispatched)");

            var run = WalkChain(chain, configured, margin, 1.0, backstop, 60.0, timedCompletion: false);
            Check(ref failures,
                  run.Dispatched == 1 && run.SkippedAtSeconds("T2") == configured
                  && run.Result("T2") == GateResult.PredecessorTimeout
                  && run.Result("T3") == GateResult.PredecessorAbandoned,
                  $"(vii) ... and with the fix the same run gives up at the CONFIGURED " +
                  $"{configured:F0} s (got {run.SkippedAtSeconds("T2"):F0} s), which is what the operator asked for");
        }

        // (f8) B7: THE LOG HAS TO SAY WHICH TIMEOUT IT WAS. Live gate 5 is specified as a log
        //      check, so the wording is part of the contract - and one sentence for both timeouts
        //      reported every A1 skip against a dispatch that had never happened, quoting a window
        //      that was not the one that expired.
        {
            Check(ref failures,
                  TaskDispatchPolicy.GateFailureReason(GateResult.PredecessorNeverDispatched, 86400.0, 4860.0)
                      == "never dispatched within 86400s of order receipt",
                  "(viii) a PHASE-1 timeout says the predecessor NEVER DISPATCHED, and quotes the DISPATCH " +
                  "window measured from order receipt");
            Check(ref failures,
                  TaskDispatchPolicy.GateFailureReason(GateResult.PredecessorTimeout, 86400.0, 4860.0)
                      == "did not complete within 4860s of its dispatch",
                  "(viii) a PHASE-2 timeout says it did not COMPLETE, and quotes the COMPLETION window " +
                  "measured from its dispatch");
            Check(ref failures,
                  TaskDispatchPolicy.GateFailureReason(GateResult.PredecessorAbandoned, 86400.0, 4860.0)
                      == "was skipped/abandoned upstream",
                  "(viii) an ABANDONED predecessor is neither, and names no window at all");
            Check(ref failures,
                  TaskDispatchPolicy.GateFailureReason(GateResult.PredecessorNeverDispatched, 86400.0, 4860.0)
                      != TaskDispatchPolicy.GateFailureReason(GateResult.PredecessorTimeout, 86400.0, 4860.0),
                  "(viii) ... and the two timeouts can never print the same sentence");
        }

        // (f9) D1 (pass-3 review of `8db033e`): A DISPATCH THAT DIES ON THE TICK THREAD MUST STILL
        //      END ITS TASK. The bridge work is enqueued onto the VR-Forces tick thread, and the
        //      drain's only handler logs and returns - so a throw there told the sequencer nothing
        //      and STP nothing, and since A1 the successors then sat on the 86,400 s chain
        //      backstop rather than the 4,860 s window. `b76c9c7` gave the FIRST pass the proper
        //      ending; the re-entry that really dispatches a ground move in the DEFAULT
        //      TerrainProfile mode did not have it.
        //
        //      WHAT IS DRIVEN HERE: the production DeferredDispatch.Run, the production
        //      TaskSequencer and the production TaskStatusPolicy. The one stand-in is
        //      PushTaskStatus, which needs a C2SIM server - so `Abort` below is its ONE deciding
        //      line (consult TaskStatusPolicy.ShouldEmit, emit only when it says yes) and nothing
        //      else. The service glue that assembles them cannot be driven offline; the runner it
        //      calls was extracted so that it can be.
        {
            const string pred = "PRED-D1";
            Action throwing = () => throw new InvalidOperationException("the bridge call failed");

            // (a) THE THROW HAPPENS BEFORE MarkDispatched - the ordinary case, and the one where
            //     STP has been told nothing at all about this task.
            {
                var clock = new StepClock();
                var seq = new TaskSequencer();
                var status = new TaskStatusPolicy();
                var aborts = new List<string>();
                int starts = 0;
                void Emit(S.TaskStatusCodeType code, string uuid, string why)
                {
                    if (!status.ShouldEmit(code, uuid)) return;
                    if (code == S.TaskStatusCodeType.TASKABRT) aborts.Add(why);
                    else if (code == S.TaskStatusCodeType.TASKSTRT) starts++;
                }

                // The successor's gate, opened at t = 0 with NOTHING dispatched - HandleOrder's own
                // topology, and the phase-1 window A1 gives an in-order predecessor.
                double window = TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, 4800.0, margin);
                double phase1 = TaskDispatchPolicy.PredecessorDispatchTimeoutSeconds(true, window, backstop);
                var gate = seq.WaitForStartAsync(pred, 0, 0, window, clock.AsTaskClock(),
                                                 CancellationToken.None, phase1);

                // FAIL-FIRST (D1): the PRE-FIX ending. The bare lambda's only handler was the tick
                // drain's catch, which logs and tells NOBODY - the same runner with no sequencer and
                // no report action is exactly that.
                var preFix = DeferredDispatch.Run(throwing, pred, "T_D1", DeferredDispatch.TerrainContinuation,
                                                  sequencer: null, reportAbort: null, logError: _ => { });
                Thread.Sleep(50);
                Check(ref failures,
                      preFix is InvalidOperationException && aborts.Count == 0 && !gate.IsCompleted,
                      $"FAIL-FIRST (D1): with the pre-fix ending - the tick drain logs the throw and tells " +
                      $"nobody - the task reports NO TASKABRT (got {aborts.Count}) and its successor is STILL " +
                      $"at the gate with {phase1:F0} s of backstop left to wait");

                var thrown = DeferredDispatch.Run(throwing, pred, "T_D1", DeferredDispatch.TerrainContinuation,
                                                  seq, why => Emit(S.TaskStatusCodeType.TASKABRT, pred, why),
                                                  logError: _ => { });
                bool done = gate.Wait(TimeSpan.FromSeconds(5));
                Check(ref failures,
                      thrown is InvalidOperationException && aborts.Count == 1 && starts == 0,
                      $"(ix) D1: a continuation that THROWS reports exactly ONE TASKABRT (got {aborts.Count}) " +
                      $"and NO TASKSTRT (got {starts}) - a task that never started must not report started");
                Check(ref failures,
                      done && gate.Result == GateResult.PredecessorAbandoned && clock.Now == 0.0,
                      $"(ix) ... and its successor fails FAST - PredecessorAbandoned at 0 s of clock, not the " +
                      $"{phase1:F0} s backstop (got {(done ? gate.Result.ToString() : "still waiting")} at " +
                      $"{clock.Now:F0} s)");
                Check(ref failures,
                      aborts.Count == 1 && aborts[0].Contains("InvalidOperationException")
                      && aborts[0].Contains(DeferredDispatch.TerrainContinuation),
                      $"(ix) ... and the one TASKABRT names the exception TYPE and WHERE it died " +
                      $"(got \"{(aborts.Count == 1 ? aborts[0] : "-")}\")");

                // The reply and the timeout sweep can both reach a continuation; a second ending for
                // the same task must not produce a second report.
                DeferredDispatch.Run(throwing, pred, "T_D1", DeferredDispatch.TerrainContinuation,
                                     seq, why => Emit(S.TaskStatusCodeType.TASKABRT, pred, why),
                                     logError: _ => { });
                Check(ref failures, aborts.Count == 1,
                      $"(ix) ... and a SECOND failure of the same task adds no second TASKABRT (got {aborts.Count})");
            }

            // (b) THE THROW HAPPENS AFTER MarkDispatched - the route was created, the bridge was
            //     called, TASKSTRT went out, and then something threw. The ending must not announce
            //     a second start, and must still be exactly one TASKABRT.
            {
                const string half = "PRED-D1-HALF";
                var clock = new StepClock();
                var seq = new TaskSequencer();
                var status = new TaskStatusPolicy();
                var aborts = new List<string>();
                int starts = 0;
                void Emit(S.TaskStatusCodeType code, string uuid, string why)
                {
                    if (!status.ShouldEmit(code, uuid)) return;
                    if (code == S.TaskStatusCodeType.TASKABRT) aborts.Add(why);
                    else if (code == S.TaskStatusCodeType.TASKSTRT) starts++;
                }
                double window = TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, 4800.0, margin);
                double phase1 = TaskDispatchPolicy.PredecessorDispatchTimeoutSeconds(true, window, backstop);
                var gate = seq.WaitForStartAsync(half, 0, 0, window, clock.AsTaskClock(),
                                                 CancellationToken.None, phase1);

                // MarkDispatched's own order: NotifyDispatched, then TASKSTRT - then the throw.
                Action halfDispatched = () =>
                {
                    seq.NotifyDispatched(half, clock.Now);
                    Emit(S.TaskStatusCodeType.TASKSTRT, half, "dispatched");
                    throw new InvalidOperationException("the bridge call failed after MarkDispatched");
                };
                DeferredDispatch.Run(halfDispatched, half, "T_D1_HALF", DeferredDispatch.TerrainContinuation,
                                     seq, why => Emit(S.TaskStatusCodeType.TASKABRT, half, why),
                                     logError: _ => { });
                bool done = gate.Wait(TimeSpan.FromSeconds(5));
                Check(ref failures, starts == 1 && aborts.Count == 1,
                      $"(ix) D1: a throw AFTER MarkDispatched leaves the one TASKSTRT it had already sent " +
                      $"(got {starts}) and adds exactly one TASKABRT (got {aborts.Count}) - never a second start");
                Check(ref failures, !status.ShouldEmit(S.TaskStatusCodeType.TASKSTRT, half),
                      "(ix) ... and the task cannot announce a second start for the same execution: an ABORT " +
                      "does not re-arm TASKSTRT, only a completion does");
                Check(ref failures,
                      done && gate.Result == GateResult.PredecessorAbandoned && clock.Now == 0.0,
                      $"(ix) ... and the successor of a HALF-dispatched task is abandoned at 0 s of clock too " +
                      $"(got {(done ? gate.Result.ToString() : "still waiting")} at {clock.Now:F0} s)");
            }
        }

        // (f10) E3 (pass-3 review; SUPERVISOR DECISION extending Q4). A CYCLIC OR SELF-REFERENCING
        //       STREND CHAIN IS MALFORMED. It is the one dead end nothing ever abandons - every
        //       task on the loop waits for another task on the loop - and since A1 it is also the
        //       most expensive: predecessorInThisOrder is TRUE, so phase 1 takes the 86,400 s
        //       backstop where before A1 the same order failed in 600 s.
        {
            // FAIL-FIRST (E3): what a cycle COSTS when nothing refuses it. Walked on the real
            // sequencer under the A1 rule, nothing dispatches and nothing is skipped until the
            // chain backstop has run out - a sim day at the default - and the sentence the tasks
            // finally get is "never dispatched within 86400s of order receipt", which is true about
            // the wrong thing. The steps are coarse on purpose: the point is WHERE it ends.
            //
            // A SELF-cycle for the exact outcome (one task, so nothing can race), and the 2-cycle
            // for the cost. In the 2-cycle the two gates expire at the same reading and whichever
            // the pool resolves first abandons the other, so which of the two sentences each task
            // gets is not deterministic - only that at least one is the phase-1 timeout and that
            // BOTH land after the backstop. Asserting more than that would be asserting a race.
            {
                var self = new[] { new ChainTask("A", "A", 4800_000L, 0L) };
                var preFix = WalkChain(self, configured, margin, 1.0, backstop, 30000.0);
                Check(ref failures,
                      preFix.Dispatched == 0 && preFix.SkippedCount == 1
                      && preFix.Result("A") == GateResult.PredecessorNeverDispatched
                      && preFix.SkippedAtSeconds("A") >= backstop,
                      $"FAIL-FIRST (E3): with nothing refusing it a SELF-referencing task dispatches " +
                      $"{preFix.Dispatched} of 1 and is skipped only after the {backstop:F0} s chain backstop " +
                      $"(at {preFix.SkippedAtSeconds("A"):F0} s, as {preFix.Result("A")}) - before A1 the same " +
                      $"order failed in {configured:F0} s");

                var cycle2 = new[] { new ChainTask("A", "B", 4800_000L, 0L),
                                     new ChainTask("B", "A", 4800_000L, 0L) };
                var two = WalkChain(cycle2, configured, margin, 1.0, backstop, 30000.0);
                Check(ref failures,
                      two.Dispatched == 0 && two.SkippedCount == 2
                      && two.SkippedAtSeconds("A") >= backstop && two.SkippedAtSeconds("B") >= backstop
                      && (two.Result("A") == GateResult.PredecessorNeverDispatched
                          || two.Result("B") == GateResult.PredecessorNeverDispatched),
                      $"FAIL-FIRST (E3): ... and a 2-cycle costs the same - {two.Dispatched} of 2 dispatched, " +
                      $"both skipped no earlier than the backstop (at {two.SkippedAtSeconds("A"):F0} / " +
                      $"{two.SkippedAtSeconds("B"):F0} s, as {two.Result("A")} / {two.Result("B")})");
            }

            Check(ref failures,
                  TaskDispatchPolicy.FindPredecessorCycles(
                      new Dictionary<string, string> { ["A"] = "A" }).SetEquals(new[] { "A" }),
                  "(x) E3: a task whose startAfterTaskUuid names ITSELF is on a cycle");
            Check(ref failures,
                  TaskDispatchPolicy.FindPredecessorCycles(
                      new Dictionary<string, string> { ["A"] = "B", ["B"] = "A" })
                      .SetEquals(new[] { "A", "B" })
                  && TaskDispatchPolicy.FindPredecessorCycles(
                      new Dictionary<string, string> { ["A"] = "B", ["B"] = "C", ["C"] = "A" })
                      .SetEquals(new[] { "A", "B", "C" }),
                  "(x) E3: a 2-cycle and a 3-cycle put EVERY task on the loop on it - both are refused at once");
            Check(ref failures,
                  TaskDispatchPolicy.FindPredecessorCycles(
                      new Dictionary<string, string> { ["T1"] = "", ["T2"] = "T1", ["T3"] = "T2", ["T4"] = "T3" })
                      .Count == 0
                  && TaskDispatchPolicy.FindPredecessorCycles(
                      new Dictionary<string, string> { ["T1"] = "no-such-task-uuid" }).Count == 0,
                  "(x) E3: a healthy 4-deep chain and a DANGLING reference are NOT cycles - the dangling one " +
                  "is the case the configured window still bounds, and refusing it would be wrong");
            // A task that POINTS AT a loop without being on it must not be refused as malformed -
            // it is a healthy task with a dead predecessor, and the existing cascade covers it.
            {
                var entrant = new Dictionary<string, string> { ["A"] = "B", ["B"] = "A", ["E"] = "A" };
                var cyc = TaskDispatchPolicy.FindPredecessorCycles(entrant);
                Check(ref failures, cyc.SetEquals(new[] { "A", "B" }),
                      $"(x) E3: a task GATED ON a loop is not itself on it (found [{string.Join(",", cyc)}])");
                var seq = new TaskSequencer();
                var clock = new StepClock();
                var gate = seq.WaitForStartAsync("A", 0, 0, 4860.0, clock.AsTaskClock(),
                                                 CancellationToken.None, backstop);
                foreach (var u in cyc) seq.NotifyAbandoned(u);       // what the refusal does
                bool done = gate.Wait(TimeSpan.FromSeconds(5));
                Check(ref failures, done && gate.Result == GateResult.PredecessorAbandoned && clock.Now == 0.0,
                      $"(x) E3: ... and the refusal cascades to it through the SAME PredecessorAbandoned path " +
                      $"every other dead end uses, at 0 s of clock (got " +
                      $"{(done ? gate.Result.ToString() : "still waiting")} at {clock.Now:F0} s)");
            }
            Check(ref failures,
                  TaskDispatchPolicy.DescribePredecessorCycle(
                      "A", new Dictionary<string, string> { ["A"] = "B", ["B"] = "A" }) == "A -> B -> A"
                  && TaskDispatchPolicy.DescribePredecessorCycle(
                      "A", new Dictionary<string, string> { ["A"] = "A" }) == "A -> A",
                  "(x) E3: the ERROR names the LOOP, not just the task - an order with 42 tasks cannot be " +
                  "fixed from \"this one is on a cycle\"");
        }

        // (f5) THE WHOLE COA-STP1 GRAPH, end to end, from the order on disk. This is the branch's
        //      own live gate 2 ("42 dispatches, not 9") decided OFFLINE.
        {
            string file = FindCoaStp1Order();
            var order = file == null ? null : OrderParser.Parse(File.ReadAllText(file));
            Check(ref failures, order != null && order.Tasks.Count == 42,
                  $"(v) data/COA-STP1_Order.xml parses to 42 tasks (got " +
                  $"{(order == null ? "NOT FOUND" : order.Tasks.Count.ToString())})");
            if (order != null && order.Tasks.Count == 42)
            {
                var graph = new List<ChainTask>();
                foreach (var t in order.Tasks)
                    graph.Add(new ChainTask(t.TaskUuid, t.StartAfterTaskUuid, t.DurationMs,
                                            Math.Max(t.SimulationStartMs, t.RelativeDelayMs)));

                // (x) E3 NO FALSE POSITIVES. The refusal is loud and terminal, so the one thing it
                //     must never do is fire on the order this port exists for.
                var realPred = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var t in order.Tasks) realPred[t.TaskUuid] = t.StartAfterTaskUuid;
                var realCycles = TaskDispatchPolicy.FindPredecessorCycles(realPred);
                Check(ref failures, realCycles.Count == 0,
                      $"(x) E3: the real 42-task COA-STP1 graph carries NO cycle - the refusal fires on " +
                      $"{realCycles.Count} of its tasks (must be 0)");

                // (xi) E4 + E6: THE NUMBER THE BACKSTOP IS JUSTIFIED BY, MEASURED. The comment on
                //      Vrf:TaskChainBackstopSeconds claimed COA-STP1's deepest chain is 26,400 s;
                //      the order says 16,800 s to the last DISPATCH and 21,600 s to the last END
                //      (ten PT2H roots + three PT1H20M successors, and T13's 12,000 s delay + one
                //      PT1H20M then T14). The service logs these at order receipt, so they are the
                //      operator's numbers and not just a comment's.
                double realLead = TaskDispatchPolicy.LongestChainLeadSeconds(AsChainNodes(graph), 1.0);
                double realEnd = TaskDispatchPolicy.LongestChainEndSeconds(AsChainNodes(graph), 1.0);
                Check(ref failures, realLead == 16800.0 && realEnd == 21600.0,
                      $"(xi) E6: COA-STP1's longest DISPATCH lead is 16,800 s and its last task ENDS at " +
                      $"21,600 s (measured {realLead:F0} / {realEnd:F0}) - not the 26,400 s the backstop's " +
                      $"justification used to claim");
                Check(ref failures, realLead < backstop && realEnd < backstop,
                      $"(xi) E4: ... and both fit inside Vrf:TaskChainBackstopSeconds={backstop:F0} s " +
                      $"({backstop / realLead:F1}x headroom on the lead), so this order logs the INFO line " +
                      $"and NOT the 'chain deeper than the backstop' WARNING");
                Check(ref failures,
                      TaskDispatchPolicy.LongestChainLeadSeconds(AsChainNodes(graph), 0.05) == 16800.0 * 0.05
                      && TaskDispatchPolicy.LongestChainLeadSeconds(AsChainNodes(graph), 10.0) >= backstop,
                      $"(xi) E4: the lead is measured AFTER Vrf:DurationScale - 0.05 compresses it to " +
                      $"{16800.0 * 0.05:F0} s, and a scale of 10 pushes it past the backstop, which is the " +
                      $"case the WARNING exists for");

                var preFix = WalkChain(graph, configured, margin, 1.0, backstop, step, preFixPhase1: true);
                Check(ref failures, preFix.Dispatched == 21 && preFix.SkippedCount == 21,
                      $"FAIL-FIRST (A1): the pre-fix rule dispatches {preFix.Dispatched} of the order's 42 tasks " +
                      $"and skips {preFix.SkippedCount} with TASKABRT - the roots and their first successors run, " +
                      $"the two deeper levels and T14 do not");

                var asWritten = WalkChain(graph, configured, margin, 1.0, backstop, step);
                Check(ref failures, asWritten.Dispatched == 42 && asWritten.SkippedCount == 0,
                      $"(v) the whole COA-STP1 graph at Vrf:DurationScale=1.0 and the SHIPPED " +
                      $"{configured:F0} s floor: {asWritten.Dispatched} dispatches, " +
                      $"{asWritten.SkippedCount} skipped (must be 42 / 0)");

                var demo = WalkChain(graph, demoConfigured, margin, 1.0, backstop, step);
                Check(ref failures, demo.Dispatched == 42 && demo.SkippedCount == 0,
                      $"(v) ... and under the Demo overlay's {demoConfigured:F0} s floor: " +
                      $"{demo.Dispatched} dispatches, {demo.SkippedCount} skipped");

                // DETERMINISM. The compressed profile is where the pre-fix rule put a task on the
                // exact boundary between its window and its predecessor's dispatch, and a demo
                // that fails one run in seven is worse than one that fails every time.
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < 20; i++)
                {
                    var r = WalkChain(graph, configured, margin, 0.05, backstop, compressedStep);
                    seen.Add($"{r.Dispatched}/{r.SkippedCount}");
                }
                Check(ref failures, seen.Count == 1 && seen.Contains("42/0"),
                      $"(v) 20 repetitions at Vrf:DurationScale=0.05 all give 42 dispatches and 0 skips " +
                      $"(distinct outcomes seen: {string.Join(", ", seen)})");
            }
        }
    }

    /// <summary>One task as the gate graph sees it: its uuid, its STREND predecessor (empty for a
    /// root), its authored Duration and its authored start delay, both in milliseconds.</summary>
    private sealed record ChainTask(string Uuid, string Pred, long DurationMs, long StartDelayMs);

    /// <summary>A serial chain T1 -> T2 -> ... with the given authored Durations.</summary>
    private static List<ChainTask> SerialChain(params long[] durationsMs)
    {
        var chain = new List<ChainTask>();
        for (int i = 0; i < durationsMs.Length; i++)
            chain.Add(new ChainTask("T" + (i + 1), i == 0 ? "" : "T" + i, durationsMs[i], 0L));
        return chain;
    }

    /// <summary>What a walk of the graph produced: when each task dispatched, or why it did not.</summary>
    private sealed class ChainOutcome
    {
        public readonly Dictionary<string, double> DispatchedAt = new(StringComparer.Ordinal);
        public readonly Dictionary<string, (GateResult Result, double At)> Skipped = new(StringComparer.Ordinal);
        public int Dispatched => DispatchedAt.Count;
        public int SkippedCount => Skipped.Count;
        public double At(string uuid) => DispatchedAt.TryGetValue(uuid, out var t) ? t : double.NaN;
        public GateResult Result(string uuid) => Skipped.TryGetValue(uuid, out var s) ? s.Result : GateResult.Proceed;
        public double SkippedAtSeconds(string uuid) => Skipped.TryGetValue(uuid, out var s) ? s.At : double.NaN;
        public string Times(params string[] uuids)
        {
            var parts = new List<string>();
            foreach (var u in uuids)
                parts.Add(DispatchedAt.TryGetValue(u, out var t) ? t.ToString("F0")
                        : Skipped.TryGetValue(u, out var s) ? s.Result.ToString() : "-");
            return string.Join(" / ", parts);
        }
    }

    /// <summary>
    /// Walk a whole gate graph on one monotone clock, exactly as the service orchestrates one:
    /// every task's gate starts at t = 0 (HandleOrder's single foreach), a gate that opens is
    /// dispatched with MarkDispatched's own ordering, and a gate that does not open abandons its
    /// task so its successors fail fast. Returns when every task has dispatched or been skipped.
    /// </summary>
    /// <param name="preFixPhase1">The FAIL-FIRST control for A1: give phase 1 the same window
    /// phase 2 gets, which is what the branch did before A1.</param>
    /// <param name="timedCompletion">Vrf:TimedCompletion. OFF means MarkDispatched arms no end
    /// time at all, so nothing in the walk ever completes - the documented evidence-only mode.</param>
    /// <param name="derivesFromDuration">The FAIL-FIRST control for B4: derive the long completion
    /// window from the predecessor's Duration even though no timer will arm it. Null follows
    /// <paramref name="timedCompletion"/>, which is what the service now does.</param>
    private static ChainOutcome WalkChain(IReadOnlyList<ChainTask> tasks, double configured,
                                          double margin, double scale, double backstop,
                                          double stepSeconds, bool preFixPhase1 = false,
                                          bool timedCompletion = true, bool? derivesFromDuration = null)
    {
        var clock = new StepClock();
        var seq = new TaskSequencer();
        var timed = new TimedCompletionPolicy();
        var byUuid = new Dictionary<string, ChainTask>(StringComparer.Ordinal);
        foreach (var t in tasks) byUuid[t.Uuid] = t;
        var gates = new Dictionary<string, Task<GateResult>>(StringComparer.Ordinal);
        var outcome = new ChainOutcome();

        foreach (var t in tasks)
        {
            bool predFound = !string.IsNullOrEmpty(t.Pred) && byUuid.ContainsKey(t.Pred);
            double predEnd = TaskDispatchPolicy.PredecessorEndSeconds(
                derivesFromDuration ?? timedCompletion, predFound,
                predFound ? byUuid[t.Pred].DurationMs : 0L, scale);
            double window = TaskDispatchPolicy.PredecessorTimeoutSeconds(configured, predEnd, margin);
            double phase1 = preFixPhase1 ? window
                          : TaskDispatchPolicy.PredecessorDispatchTimeoutSeconds(predFound, window, backstop);
            gates[t.Uuid] = seq.WaitForStartAsync(t.Pred,
                                                  TaskDispatchPolicy.ScaleOrderMs(t.StartDelayMs, scale),
                                                  0L, window, clock.AsTaskClock(), CancellationToken.None,
                                                  phase1);
        }

        double horizon = LongestLeadSeconds(tasks, scale) + configured + margin + 4.0 * stepSeconds;
        bool signalled = true;      // t = 0: every root's gate is open before the walk starts
        while (true)
        {
            Settle(tasks, gates, outcome, seq, timed, clock, scale, timedCompletion, signalled);
            if (outcome.Dispatched + outcome.SkippedCount >= tasks.Count) break;
            if (clock.Now >= horizon) break;
            int released = clock.AdvanceTo(clock.Now + stepSeconds);
            var due = timed.Advance(clock.Now, usingSim: true);
            foreach (var d in due) seq.CompleteTask(d.TaskUuid);
            signalled = released > 0 || due.Count > 0;
        }
        return outcome;
    }

    /// <summary>
    /// Let every gate that can move at THIS clock reading move, and do not return until nothing
    /// has moved for a while. The clock never advances inside a settle, so a continuation the
    /// thread pool runs late cannot change WHEN a task dispatched - only how long this loop takes
    /// to notice it (and a phase-2 wait registered late is self-correcting: its window is measured
    /// from the predecessor's DISPATCH, an absolute anchor, not from the registration). A step at
    /// which nothing was signalled costs yields only; a step that released a waiter or completed a
    /// task also buys coarse ticks, because that is when the thread pool has something to run.
    /// </summary>
    /// <param name="signalled">The walk released a clock waiter or completed a task at this
    /// reading, so a continuation IS expected.</param>
    private static void Settle(IReadOnlyList<ChainTask> tasks, Dictionary<string, Task<GateResult>> gates,
                               ChainOutcome outcome, TaskSequencer seq, TimedCompletionPolicy timed,
                               StepClock clock, double scale, bool timedCompletion, bool signalled)
    {
        bool moved = SpinDrain(tasks, gates, outcome, seq, timed, clock, scale, timedCompletion);
        if (!signalled && !moved) return;
        for (int round = 0; round < 8; round++)
        {
            Thread.Sleep(1);
            if (!SpinDrain(tasks, gates, outcome, seq, timed, clock, scale, timedCompletion)) return;
        }
    }

    /// <summary>Drain until nothing has moved for 128 yields; true when anything moved at all. A
    /// yield covers a thread-pool continuation that is already queued - what it cannot cover is a
    /// pool that has to inject a worker, which is what Settle's coarse ticks are for.</summary>
    private static bool SpinDrain(IReadOnlyList<ChainTask> tasks, Dictionary<string, Task<GateResult>> gates,
                                  ChainOutcome outcome, TaskSequencer seq, TimedCompletionPolicy timed,
                                  StepClock clock, double scale, bool timedCompletion)
    {
        bool movedEver = false;
        long lastRegistrations = -1;
        for (int quiet = 0; quiet < 128; quiet++)
        {
            long registrations = Volatile.Read(ref clock.Registrations);
            bool moved = Drain(tasks, gates, outcome, seq, timed, clock, scale, timedCompletion);
            if (moved || registrations != lastRegistrations)
            {
                lastRegistrations = registrations;
                movedEver |= moved;
                quiet = -1;
                continue;
            }
            Thread.Yield();
        }
        return movedEver;
    }

    /// <summary>Dispatch every gate that has opened and abandon every gate that has not. Returns
    /// true when anything changed. The dispatch reproduces MarkDispatched: NotifyDispatched with
    /// the task-clock reading, THEN the end-time Register, THEN the anchoring walk.</summary>
    private static bool Drain(IReadOnlyList<ChainTask> tasks, Dictionary<string, Task<GateResult>> gates,
                              ChainOutcome outcome, TaskSequencer seq, TimedCompletionPolicy timed,
                              StepClock clock, double scale, bool timedCompletion)
    {
        bool moved = false;
        foreach (var t in tasks)
        {
            if (outcome.DispatchedAt.ContainsKey(t.Uuid) || outcome.Skipped.ContainsKey(t.Uuid)) continue;
            var gate = gates[t.Uuid];
            if (!gate.IsCompleted) continue;
            moved = true;
            double now = clock.Now;
            if (gate.Result == GateResult.Proceed)
            {
                outcome.DispatchedAt[t.Uuid] = now;
                seq.NotifyDispatched(t.Uuid, now);
                // MarkDispatched arms the end time only under Vrf:TimedCompletion (B4).
                if (timedCompletion)
                {
                    timed.Register(t.Uuid, "taskee-" + t.Uuid, t.Uuid, "unit-" + t.Uuid,
                                   TaskDispatchPolicy.ScaleOrderMs(t.DurationMs, scale) / 1000.0);
                    foreach (var d in timed.Advance(now, usingSim: true)) seq.CompleteTask(d.TaskUuid);
                }
            }
            else
            {
                outcome.Skipped[t.Uuid] = (gate.Result, now);
                seq.NotifyAbandoned(t.Uuid);
            }
        }
        return moved;
    }

    /// <summary>The deepest chain's LEAD, in clock seconds: how long the last task of the longest
    /// chain waits before it can dispatch at all. Bounds the walk.
    /// E4/E6 (pass-3 review): this WAS a private copy of the arithmetic. It is now the production
    /// TaskDispatchPolicy.LongestChainLeadSeconds - the same function the service logs at order
    /// receipt and the same one the backstop's justification quotes - so the suite's horizon and
    /// the operator's warning cannot drift apart (one re-implementation fewer; N5's family).</summary>
    private static double LongestLeadSeconds(IReadOnlyList<ChainTask> tasks, double scale)
        => TaskDispatchPolicy.LongestChainLeadSeconds(AsChainNodes(tasks), scale);

    /// <summary>The suite's chain shape as the production arithmetic takes it.</summary>
    private static List<TaskDispatchPolicy.ChainNode> AsChainNodes(IReadOnlyList<ChainTask> tasks)
    {
        var nodes = new List<TaskDispatchPolicy.ChainNode>(tasks.Count);
        foreach (var t in tasks)
            nodes.Add(new TaskDispatchPolicy.ChainNode(t.Uuid, t.Pred, t.DurationMs, t.StartDelayMs));
        return nodes;
    }

    /// <summary>Walk up from the executable and from the working directory until COA-STP1's order
    /// is in sight - the same search --preflight-selftest and --initgraphics-selftest use.</summary>
    private static string FindCoaStp1Order()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            for (var d = new DirectoryInfo(start); d != null; d = d.Parent)
            {
                string candidate = Path.Combine(d.FullName, "data", "COA-STP1_Order.xml");
                if (File.Exists(candidate)) return candidate;
            }
        return null;
    }

    /// <summary>
    /// The chain walk's clock. Unlike <see cref="FakeClock"/> it is SIGNALLED rather than polled:
    /// a waiter registers the reading it is due at and is released the instant the walk reaches
    /// it, so a gate expires at EXACTLY its window and a 42-gate graph can be walked in whole
    /// minutes without buying a poll interval per step. Monotone, in seconds, exactly what the
    /// service's own task-clock axis is.
    /// </summary>
    private sealed class StepClock
    {
        private readonly object _lock = new();
        private readonly List<(double Due, TaskCompletionSource Tcs)> _waiters = new();
        private double _seconds;
        /// <summary>Every DelayAsync bumps this, so a settle loop can tell "a gate moved on to its
        /// next wait" from "nothing happened".</summary>
        public long Registrations;

        public double Now { get { lock (_lock) return _seconds; } }
        public TaskClock AsTaskClock() => new(() => Now, DelayAsync);

        private Task DelayAsync(double seconds, CancellationToken ct)
        {
            Interlocked.Increment(ref Registrations);
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!(seconds > 0.0)) { tcs.TrySetResult(); return tcs.Task; }
            lock (_lock) _waiters.Add((_seconds + seconds, tcs));
            if (ct.CanBeCanceled)
                ct.Register(() =>
                {
                    lock (_lock) _waiters.RemoveAll(w => ReferenceEquals(w.Tcs, tcs));
                    tcs.TrySetCanceled();
                });
            return tcs.Task;
        }

        /// <summary>Move the clock forward to this reading and release everything it reaches.
        /// Returns how many waiters were released, so the walk knows whether to expect a
        /// continuation at all.</summary>
        public int AdvanceTo(double seconds)
        {
            List<TaskCompletionSource> due = null;
            lock (_lock)
            {
                _seconds = Math.Max(_seconds, seconds);
                for (int i = _waiters.Count - 1; i >= 0; i--)
                    if (_waiters[i].Due <= _seconds)
                    {
                        (due ??= new List<TaskCompletionSource>()).Add(_waiters[i].Tcs);
                        _waiters.RemoveAt(i);
                    }
            }
            if (due == null) return 0;
            foreach (var tcs in due) tcs.TrySetResult();
            return due.Count;
        }
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }

    // ------------------------------------------------- C14 / STP-837, 2026-09-21 ----
    /// <summary>
    /// THE TWO RULINGS THIS BUILD CHANGES, CHECKED WHERE THE OTHER SELF-TESTS DO NOT LOOK: at the
    /// SHIPPED DEFAULTS. --destack-selftest and --arrival-selftest prove the rules; this proves
    /// that a run started with no configuration at all, and a run started from the shipped json,
    /// get the rules the user ruled on - and that each has the comparability switch the RUNBOOK
    /// names, because the D1-D6 series has to remain reproducible.
    ///
    ///   C14 (2026-09-07, amended 2026-09-21 option C): co-located units are spread at init;
    ///     INDEPENDENT ones at the ruled 700 m, COMPOSED SIBLINGS at their own echelon's scale
    ///     around a parent that does not move.
    ///   STP-837 (2026-09-15, amended 2026-09-21 option A): arrival needs traversal, and the
    ///     traversal each member owes is its own share of its own approach.
    /// </summary>
    private static void EchelonAndTraversalRulings(ref int failures)
    {
        var d = new VrfSettings();
        Check(ref failures, d.DeStackComposedSiblings,
              "Vrf:DeStackComposedSiblings initialises to TRUE - a run with no configuration file " +
              "spreads composed siblings (user ruling 2026-09-21, option C)");
        Check(ref failures, d.DeStackEchelonFallbackMeters == 0.0,
              "Vrf:DeStackEchelonFallbackMeters initialises to 0 - a group whose echelon the table " +
              "cannot size is NOT spread at a number nobody derived; the documented fallback is to " +
              "leave it with its parent");
        Check(ref failures, d.DeStackEchelonSpacingMeters.Length == 0,
              "Vrf:DeStackEchelonSpacingMeters is empty by default - the shipped table is the " +
              "MEASURED one, and an override has to be asked for");
        Check(ref failures, d.DeStackSpacingMeters == 50.0 && d.ArrivalRadiusMeters == 500.0
                         && d.ArrivalMemberFraction == 0.5 && d.ArrivalMinTravelMeters == 100.0,
              "nothing else moved: DeStackSpacingMeters, ArrivalRadiusMeters, ArrivalMemberFraction " +
              "and ArrivalMinTravelMeters keep the values they had before this change");
        Check(ref failures, d.ArrivalApproachFraction == 0.5,
              "Vrf:ArrivalApproachFraction initialises to 0.5 - the per-member traversal bar is ON " +
              "by default (user ruling 2026-09-21, option A)");

        // The INDEPENDENT lane still spreads at the RULED 700 m, and the echelon table never
        // reaches it: the table covers platoon and below, the ruling covers company and above.
        Check(ref failures, EchelonSpacing.SpacingFor("", 700.0) == 700.0
                         && !EchelonSpacing.TableMeters.ContainsKey("COMPANY"),
              "the echelon table does not cover company and above, so the independent lane keeps " +
              "the 700 m the user ruled on 2026-09-07 - this lane re-sizes only what had no " +
              "spacing at all");

        // THE COMPARABILITY SWITCHES, through the real configuration stack (both json files as the
        // Host layers them, then the environment), because that is how a run turns them off.
        string repo = FindRulingsRepoRoot();
        string appSettings = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.json");
        string demoSettings = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.Demo.json");
        Check(ref failures, appSettings != null && File.Exists(appSettings) && File.Exists(demoSettings),
              $"both shipped settings files are on disk ({appSettings})");
        if (appSettings == null || !File.Exists(appSettings) || !File.Exists(demoSettings)) return;

        var shipped = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                          .AddJsonFile(appSettings, optional: false)
                          .Build().GetSection("Vrf").Get<VrfSettings>() ?? new VrfSettings();
        Check(ref failures, shipped.DeStackCreates && shipped.DeStackSpacingMeters == 700.0
                         && shipped.DeStackComposedSiblings && shipped.ArrivalApproachFraction == 0.5,
              "appsettings.json SAYS it: de-stack on at 700 m for independent units, composed " +
              "siblings on, approach fraction 0.5 - the defaults are written down, not only compiled in");
        var demo = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                       .AddJsonFile(appSettings, optional: false)
                       .AddJsonFile(demoSettings, optional: false)
                       .Build().GetSection("Vrf").Get<VrfSettings>() ?? new VrfSettings();
        Check(ref failures, demo.DeStackComposedSiblings && demo.ArrivalApproachFraction == 0.5,
              "the DEMO overlay keeps both - the audience's run and the runner's run behave alike");

        var off = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                      .AddJsonFile(appSettings, optional: false)
                      .AddInMemoryCollection(new Dictionary<string, string>
                      {
                          ["Vrf:DeStackComposedSiblings"] = "false",
                          ["Vrf:ArrivalApproachFraction"] = "0",
                      }).Build().GetSection("Vrf").Get<VrfSettings>();
        Check(ref failures, off != null && !off.DeStackComposedSiblings && off.ArrivalApproachFraction == 0.0,
              "and BOTH turn off by key - Vrf:DeStackComposedSiblings=false + " +
              "Vrf:ArrivalApproachFraction=0 is the configuration that reproduces a D1-D6 run " +
              "(RUNBOOK 11e comparability warning)");

        const string EnvKey = "Vrf__DeStackComposedSiblings";
        string saved = Environment.GetEnvironmentVariable(EnvKey);
        try
        {
            Environment.SetEnvironmentVariable(EnvKey, "false");
            var offByEnv = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                               .AddJsonFile(appSettings, optional: false)
                               .AddJsonFile(demoSettings, optional: false)
                               .AddEnvironmentVariables()
                               .Build().GetSection("Vrf").Get<VrfSettings>();
            Check(ref failures, offByEnv != null && !offByEnv.DeStackComposedSiblings,
                  $"{EnvKey}=false turns it off over BOTH json files - the runner's escape hatch");
            Environment.SetEnvironmentVariable(EnvKey, "true");
            var onByEnv = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                              .AddJsonFile(appSettings, optional: false)
                              .AddEnvironmentVariables()
                              .Build().GetSection("Vrf").Get<VrfSettings>();
            Check(ref failures, onByEnv != null && onByEnv.DeStackComposedSiblings,
                  $"{EnvKey}=true leaves it on (proof the key is read at all, not passing by being ignored)");
        }
        finally { Environment.SetEnvironmentVariable(EnvKey, saved); }
    }

    /// <summary>Walk up from the executable until data/COA-STP1_Order.xml appears.</summary>
    private static string FindRulingsRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "data", "COA-STP1_Order.xml"))) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
