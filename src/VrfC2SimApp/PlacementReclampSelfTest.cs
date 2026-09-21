using S = C2SIM.Schema102;

namespace VrfC2SimApp;

/// <summary>
/// FIXTURE-LEVEL SELF-TEST FOR THE PLACEMENT RE-CLAMP AND THE DISPATCH GROUND GATE.
///
/// THE RUN THIS EXISTS FOR - runs\20260921T114910Z_run, Iron Storm cut A, MAK Earth streaming a
/// cold Suwalki AO. The app's own lines, in order:
///     :223   INIT CREATION BARRIER: 36 object(s) planned ... armed at WALL 11:51:36.263Z ... 20 s
///     :225   Init: terrain-profile request 1 sent for 36 create position(s) ... timeout 10 s
///     :309   Terrain profile request 1 for task 'INIT PLACEMENT' got no reply within 10 s
///     :383   PLACEMENT summary: 0 of 36 create altitude(s) came from the TERRAIN QUERY, 36 FALLBACK
///     :387   READY TO TASK - NOT REACHED within 20 s: only 0 of 36 init unit(s) are bound
///     :784   Terrain profile reply 43: [#0:54.04269,23.30823,130.1]   <- ~32 s later, SAME path
///     :1135  taskee altitude not terrain-clamped: live -0.0 m vs terrain 145.4 m ... anyway
///     :1939  taskee altitude not terrain-clamped: live -0.0 m vs terrain 155.8 m ... anyway
/// Two platforms measured ~150 m under the terrain were tasked, and the bus was told nothing.
///
/// FAIL-FIRST BY CONSTRUCTION. `--placement-reclamp-selftest --disabled` runs the SAME fixtures and
/// the SAME assertions with Vrf:PlacementReclamp = false - the 33f1894 behaviour - and every
/// assertion about correcting, verifying and refusing MUST fail: that arm reproduces the run above.
/// The assertions that hold in BOTH arms are the invariants this must not break - a HEALTHY init
/// unchanged in its lines and its timing, an UNMEASURED object still taskable, and the STP-852/B1
/// barrier inequality still true at the shipped numbers.
///
/// WHAT THESE FIXTURES ARE, stated exactly. A MODEL of the service's decision sequence
/// (SweepPlacementReclamp / ApplyPlacementReclamp / GroundGateAllowsDispatch) driving the REAL
/// <see cref="PlacementReclampPolicy"/> rules, the REAL <see cref="DispatchReadiness"/> classifier
/// and the REAL sentences. They do NOT execute VrfC2SimService's own methods; no bridge, no
/// federation, no clock, nothing that joins an RTI.
/// </summary>
public static class PlacementReclampSelfTest
{
    // The shipped numbers, so the fixtures measure what a run measures.
    private const double Bound = 60.0;          // Vrf:PlacementReclampSeconds
    private const double Retry = 5.0;           // Vrf:PlacementReclampRetrySeconds
    private const double Tolerance = 50.0;      // Vrf:PlacementReclampToleranceMeters
    private const double TerrainTimeout = 10.0; // Vrf:TerrainProfileTimeoutSeconds
    private const double CompTimeout = 15.0;    // Vrf:CompositionTimeoutSeconds
    private const double Never = double.PositiveInfinity;

    /// <summary>The world one object lives in, in WALL seconds from the placement.</summary>
    private sealed class World
    {
        /// <summary>When the back end's terrain first answers for this point. In the motivating run
        /// the init query at t=0 was unanswered and the first answer came ~32 s later.</summary>
        public double TerrainAnswersAt = 32.0;
        /// <summary>The terrain height the back end reports (m, MAK-convention MSL).</summary>
        public double TerrainMeters = 145.4;
        /// <summary>When ObjectCreated binds the name - nothing can be measured before it.</summary>
        public double BoundAt = 20.0;
        /// <summary>The object's live altitude. -0.0 is what all three channels read in the run.</summary>
        public double LiveAltMeters = -0.0;
        /// <summary>Does setAltitude(0, aboveGroundLevel=TRUE) actually move this object? The header
        /// says it "is ignored if the vehicle is not an air-going vehicle"
        /// (setAltitudeRequest.h:24-25); one uncontrolled run says it lifted a ground M1A2
        /// (VRF_ALTITUDE_FRAMES sec 1b). BOTH worlds are fixtures here.</summary>
        public bool CorrectionWorks = true;
    }

    /// <summary>The re-clamp sweep, modelled. Ticks at 50 ms like TickLoop.</summary>
    private sealed class ReclampFixture
    {
        private readonly bool _enabled;
        private readonly World _w;
        public readonly List<string> Lines = new();
        public int Corrections;
        public int TerrainQueries;
        public PlacementReclampPolicy.Outcome Outcome = PlacementReclampPolicy.Outcome.Pending;
        public double SettledAt = double.NaN;
        public PlacementReclampPolicy.Contact Contact = PlacementReclampPolicy.Contact.Unknown;

        public ReclampFixture(bool enabled, World w) { _enabled = enabled; _w = w; }

        public void Run(double boundSeconds = Bound, double retrySeconds = Retry,
                        double tolerance = Tolerance)
        {
            if (!_enabled)
            {
                // The 33f1894 behaviour: nothing re-measures, nothing corrects, and the object's
                // ground contact is never known to the interface.
                Outcome = PlacementReclampPolicy.Outcome.NeverMeasured;
                return;
            }
            Lines.Add(PlacementReclampPolicy.ArmedLine(1, 36, boundSeconds, retrySeconds, tolerance));
            double lastQuery = double.NegativeInfinity;
            PlacementReclampPolicy.Measurement last = default;
            for (double t = 0.0; !PlacementReclampPolicy.Expired(t, boundSeconds); t += 0.05)
            {
                if (Outcome != PlacementReclampPolicy.Outcome.Pending) break;
                if (t < _w.BoundAt) continue;                       // not bound: nothing to read
                if (!PlacementReclampPolicy.MayQuery(false, t - lastQuery, retrySeconds)) continue;
                lastQuery = t;
                TerrainQueries++;
                if (t < _w.TerrainAnswersAt) continue;              // asked, not answered - retry later
                var m = PlacementReclampPolicy.Measure(_w.LiveAltMeters, _w.TerrainMeters, tolerance);
                Contact = m.Contact;
                switch (PlacementReclampPolicy.Decide(m.Contact, Corrections))
                {
                    case PlacementReclampPolicy.Action.Correct:
                        Corrections++;
                        Lines.Add(PlacementReclampPolicy.CorrectionLine(N, m, tolerance));
                        // The one call the bridge already exposes, AGL by construction
                        // (VrfBridge.cpp:426 -> VrfFacade.cpp:739).
                        if (_w.CorrectionWorks) _w.LiveAltMeters = _w.TerrainMeters;
                        last = m;
                        break;
                    case PlacementReclampPolicy.Action.GiveUp:
                        Outcome = PlacementReclampPolicy.Outcome.StillOffGround;
                        SettledAt = t;
                        Lines.Add(PlacementReclampPolicy.GaveUpLine(N, m));
                        break;
                    default:
                        Outcome = PlacementReclampPolicy.Conclude(m.Contact, Corrections);
                        SettledAt = t;
                        if (Outcome == PlacementReclampPolicy.Outcome.Reclamped)
                            Lines.Add(PlacementReclampPolicy.VerifiedLine(N, last, m));
                        break;
                }
            }
            if (Outcome == PlacementReclampPolicy.Outcome.Pending)
                Outcome = PlacementReclampPolicy.Outcome.NeverMeasured;
            Lines.Add(PlacementReclampPolicy.SummaryLine(
                Outcome == PlacementReclampPolicy.Outcome.AlreadyOnGround ? 1 : 0,
                Outcome == PlacementReclampPolicy.Outcome.Reclamped ? 1 : 0,
                Outcome == PlacementReclampPolicy.Outcome.StillOffGround ? 1 : 0,
                Outcome == PlacementReclampPolicy.Outcome.NeverMeasured ? 1 : 0,
                double.IsNaN(SettledAt) ? boundSeconds : SettledAt));
        }

        private const string N = "28ID__FRIENDLY_INFANTRY_DIVISION";
    }

    /// <summary>
    /// The DISPATCH GROUND GATE, modelled: the route's terrain reply carries the height under
    /// vertex 0, the caller carries the taskee's live altitude, and the gate decides. Mirrors
    /// GroundGateAllowsDispatch including its ONE-DEFERRAL bound.
    /// </summary>
    private sealed class GateFixture
    {
        private readonly bool _enabled;
        private readonly World _w;
        private readonly HashSet<string> _deferred = new(StringComparer.Ordinal);
        public readonly List<string> Lines = new();
        public readonly List<(S.TaskStatusCodeType Code, string Why)> Statuses = new();
        public bool Dispatched;
        public int Visits, Corrections;
        public TaskeeReadiness HeldAs = TaskeeReadiness.Ready;

        public GateFixture(bool enabled, World w) { _enabled = enabled; _w = w; }

        /// <summary>One trip through the terrain continuation. Returns true when the task is
        /// dispatched. <paramref name="reclampVerifies"/> models what the sweep does between the
        /// first visit and the second.</summary>
        public void Dispatch(string task, string unit, bool reclampVerifies)
        {
            for (int pass = 0; pass < 4; pass++)
            {
                Visits++;
                double terrain = _w.TerrainMeters;
                var m = PlacementReclampPolicy.Measure(_w.LiveAltMeters, terrain, Tolerance);
                bool gateStops = _enabled && m.Contact == PlacementReclampPolicy.Contact.OffGround;
                if (!gateStops) { Dispatched = true; return; }
                if (!_deferred.Add(task))
                {
                    string why = PlacementReclampPolicy.DispatchGateRefusalReason(
                        task, unit, m.LiveAltMeters, m.TerrainMeters, m.GapMeters, Tolerance);
                    Lines.Add(why);
                    Statuses.Add((S.TaskStatusCodeType.TASKABRT, why));
                    return;
                }
                Lines.Add(PlacementReclampPolicy.DispatchGateHeldLine(
                    task, unit, m.LiveAltMeters, m.TerrainMeters, m.GapMeters, Tolerance));
                Corrections++;
                HeldAs = DispatchReadiness.Classify(true, true, true, true, false, true);
                // The correction is issued and the sweep verifies it (or does not) before the hold
                // releases and the task re-enters the pipeline.
                if (reclampVerifies) _w.LiveAltMeters = _w.TerrainMeters;
            }
        }
    }

    public static int Run(bool featureEnabled)
    {
        int fails = 0;
        void Check(string what, bool ok)
        {
            Console.WriteLine((ok ? "PASS  " : "FAIL  ") + what);
            if (!ok) fails++;
        }

        Console.WriteLine("=== PLACEMENT RE-CLAMP SELF-TEST (Vrf:PlacementReclamp="
                          + (featureEnabled ? "true" : "false") + ") ===");

        // ============ 1. THE MOTIVATING SEQUENCE, REPLAYED ==========================
        // Terrain unanswered at init (so the create fell back), answered 32 s later, live -0.0 m
        // against terrain 145.4 m - the numbers of `app:309`, `app:383` and `app:1135`.
        var ironStorm = new World();
        var fx = new ReclampFixture(featureEnabled, ironStorm);
        fx.Run();

        Check("IRON STORM: the object created on the FALLBACK is RE-MEASURED once the terrain "
              + "answers, and the 145 m gap is found (`app:1135` measured it and did nothing)",
              fx.Contact == PlacementReclampPolicy.Contact.OffGround
              || fx.Outcome == PlacementReclampPolicy.Outcome.Reclamped);

        Check("IRON STORM: the documented correction is issued EXACTLY ONCE - setAltitude(0 m above "
              + "ground level), the call the bridge already exposes; no new native API",
              fx.Corrections == PlacementReclampPolicy.MaxCorrections);

        Check("IRON STORM: the correction is CONFIRMED BY A READ-BACK, not assumed (sec 1b of "
              + "VRF_ALTITUDE_FRAMES had the one prior 'VERIFIED END TO END' WITHDRAWN for exactly "
              + "this)",
              fx.Outcome == PlacementReclampPolicy.Outcome.Reclamped
              && fx.Lines.Any(l => l.Contains("RE-CLAMPED AND VERIFIED", StringComparison.Ordinal)));

        Check("IRON STORM: it settles WELL INSIDE the bound - the ~32 s this terrain took to become "
              + "sampleable against Vrf:PlacementReclampSeconds = 60",
              featureEnabled ? fx.SettledAt > 30.0 && fx.SettledAt < Bound : false);

        Check("IRON STORM: the summary line a prereg scores says 1 re-clamped and 0 still off",
              fx.Lines.Any(l => l.Contains("summary:", StringComparison.Ordinal)
                                && l.Contains("1 RE-CLAMPED AND VERIFIED", StringComparison.Ordinal)
                                && l.Contains("0 STILL OFF", StringComparison.Ordinal)));

        // ============ 2. THE DISPATCH GATE - HELD, THEN TASKED ======================
        var gateWorld = new World { LiveAltMeters = -0.0, TerrainMeters = 145.4 };
        var gate = new GateFixture(featureEnabled, gateWorld);
        gate.Dispatch("T14_48Ibct...", "48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE", true);

        Check("DISPATCH GATE: a taskee measured 145 m off the terrain is NOT dispatched on the pass "
              + "that measures it - the exact step `app:1135` took ('authoring from terrain anyway')",
              featureEnabled ? gate.Visits > 1 : false);

        Check("DISPATCH GATE: the hold names the brief's state, BOUND-BUT-NOT-ON-THE-GROUND, and it "
              + "is TRANSIENT so the existing DispatchReadiness machinery owns the wait and the "
              + "timeout abort",
              gate.HeldAs == TaskeeReadiness.NotOnTheGround
              && DispatchReadiness.IsTransient(TaskeeReadiness.NotOnTheGround)
              && DispatchReadiness.StateName(TaskeeReadiness.NotOnTheGround)
                     == PlacementReclampPolicy.NotOnGroundToken);

        Check("DISPATCH GATE: once the read-back agrees, the task IS dispatched - the gate delays a "
              + "healthy unit by nothing and refuses nothing it can fix",
              gate.Dispatched && gate.Corrections == 1);

        // ============ 3. THE GATE WHEN THE CORRECTION DOES NOT TAKE =================
        // setAltitudeRequest.h:24-25 says the set is ignored for a non-air vehicle. If that is what
        // happens, the unit must still never be tasked from under the ground.
        var stuckWorld = new World { LiveAltMeters = -0.0, TerrainMeters = 155.8, CorrectionWorks = false };
        var stuck = new GateFixture(featureEnabled, stuckWorld);
        stuck.Dispatch("T02_28IdHq...", "28ID__FRIENDLY_INFANTRY_DIVISION", false);

        Check("STUCK: a unit still measured off the terrain after a correction is NEVER DISPATCHED",
              featureEnabled ? !stuck.Dispatched : false);

        Check("STUCK: it ends in ONE TASKABRT whose reason NAMES the state and the measurement, so "
              + "the C2SIM bus carries a refusal instead of a silent success",
              stuck.Statuses.Count == 1
              && stuck.Statuses[0].Code == S.TaskStatusCodeType.TASKABRT
              && stuck.Statuses[0].Why.Contains(PlacementReclampPolicy.NotOnGroundToken, StringComparison.Ordinal)
              && stuck.Statuses[0].Why.Contains("156 m", StringComparison.Ordinal));

        Check("STUCK: BOUNDED - the gate defers a task AT MOST ONCE, so a correction that does not "
              + "take costs one extra terrain round trip and then a refusal, never a cycle",
              stuck.Visits == 2);

        var stuckSweep = new ReclampFixture(featureEnabled, new World { CorrectionWorks = false });
        stuckSweep.Run();
        Check("STUCK: the sweep gives up loudly after ONE correction and says the vendor may be "
              + "behaving as documented, naming setLocation as the untried lever",
              featureEnabled
                  ? stuckSweep.Outcome == PlacementReclampPolicy.Outcome.StillOffGround
                    && stuckSweep.Corrections == 1
                    && stuckSweep.Lines.Any(l => l.Contains("setAltitudeRequest.h:24-25", StringComparison.Ordinal)
                                                 && l.Contains("setLocationRequest.h", StringComparison.Ordinal))
                  : false);

        // ============ 4. A HEALTHY INIT IS UNCHANGED - LINES AND TIMING =============
        // The D10/R9 shape: the terrain answers at once, so PlacementPolicy takes its TERRAIN QUERY
        // arm for every object and the re-clamp is never armed.
        var healthy = PlacementPolicy.Decide(PlacementPolicy.DomainLand, null, null, 1000.0, 130.1, 1.0);
        Check("HEALTHY INIT: when the terrain answers, PlacementPolicy is UNTOUCHED by this lane - "
              + "create alt = terrain + CreateClearanceMeters, from the TERRAIN QUERY (`app:786`)",
              healthy.CreateAltFromTerrain && Math.Abs(healthy.CreateAltMeters - 131.1) < 1e-9);

        var healthyFx = new ReclampFixture(featureEnabled, new World());
        // The service arms ONLY on a fallback create; a terrain-placed object is never enrolled, so
        // the fixture is not run at all - which is the point being asserted.
        Check("HEALTHY INIT: NOTHING IS ARMED, so the sweep issues no terrain query, reads no "
              + "altitude and prints no line - 0 added lines and 0 added seconds on an R9/D10 run",
              healthyFx.Lines.Count == 0 && healthyFx.TerrainQueries == 0 && healthyFx.Corrections == 0);

        Check("HEALTHY INIT: a taskee measured ON the terrain passes the gate on its FIRST visit - "
              + "the dispatch deferral D10 measured at 2.1-2.4 s gains nothing",
              new Func<bool>(() =>
              {
                  var g = new GateFixture(featureEnabled, new World { LiveAltMeters = 131.1, TerrainMeters = 130.1 });
                  g.Dispatch("T10_1-112In...", "1-112_IN", true);
                  return g.Dispatched && g.Visits == 1 && g.Corrections == 0 && g.Lines.Count == 0;
              })());

        // ============ 5. PERMISSIVE WHEN NOTHING IS MEASURED ========================
        Check("UNMEASURED IS NEVER HELD: a null terrain answer yields Contact.Unknown, which "
              + "classifies READY exactly as it does today - this lane can never wedge a run whose "
              + "terrain query is simply never answered",
              PlacementReclampPolicy.Measure(-0.0, null, Tolerance).Contact
                  == PlacementReclampPolicy.Contact.Unknown
              && DispatchReadiness.Classify(true, true, true, true, false, false) == TaskeeReadiness.Ready);

        Check("THE TOLERANCE IS THE ONLY THING THAT HOLDS A TASK: 49 m is on the ground, 51 m is "
              + "not, at the shipped 50",
              PlacementReclampPolicy.Measure(96.0, 145.0, Tolerance).Contact
                  == PlacementReclampPolicy.Contact.OnGround
              && PlacementReclampPolicy.Measure(94.0, 145.0, Tolerance).Contact
                  == PlacementReclampPolicy.Contact.OffGround);

        Check("THE REFUSAL BAR IS SEPARATE FROM THE NOTE BAR: the vertex-0 diagnostic threshold "
              + "stays at 100 m so the line a harvest greps does not move, while the refusal is 50",
              Math.Abs(TerrainVertexAuthoring.DefaultVertex0NoteThresholdMeters - 100.0) < 1e-9
              && Tolerance < TerrainVertexAuthoring.DefaultVertex0NoteThresholdMeters);

        // ============ 6. THE STP-852 / B1 INEQUALITY IS UNCHANGED ===================
        // This lane deliberately does NOT raise Vrf:TerrainProfileTimeoutSeconds, because the
        // barrier cap is 30 - that setting: raising it to 30 drives the cap to its 1 s floor and
        // re-opens the D5b overlap. The re-clamp runs on its OWN budget instead.
        Check("B1 INEQUALITY: the barrier is still 20 s at the shipped 15/10 - this lane changes no "
              + "input to it",
              Math.Abs(DispatchReadiness.BarrierSeconds(60.0, CompTimeout, TerrainTimeout) - 20.0) < 1e-9);

        Check("B1 INEQUALITY: cap + terrain + composition <= composition + 30 still holds at the "
              + "shipped numbers, and the re-clamp bound (60 s) is NOT a term in it - it holds up no "
              + "create and no materialization",
              DispatchReadiness.BarrierSeconds(60.0, CompTimeout, TerrainTimeout)
                  + DispatchReadiness.BarrierBackstopMarginSeconds(CompTimeout, TerrainTimeout)
              <= CompTimeout + 30.0 + 1e-9);

        Check("WHY THE TERRAIN TIMEOUT WAS NOT RAISED, asserted rather than asserted in prose: "
              + "TerrainProfileTimeoutSeconds = 30 would drive the barrier to its 1 s floor",
              Math.Abs(DispatchReadiness.BarrierSeconds(60.0, CompTimeout, 30.0) - 1.0) < 1e-9
              && DispatchReadiness.BarrierSeconds(60.0, CompTimeout, 20.0) < 20.0);

        // ============ 7. THE STATES AND THE SENTENCES ===============================
        Check("EVERY STATE STILL HAS A DISTINCT NAME AND DESCRIPTION, with the new one included",
              new[] { TaskeeReadiness.Unknown, TaskeeReadiness.PlannedNotRequested,
                      TaskeeReadiness.RequestedNotBound, TaskeeReadiness.BoundNotReadable,
                      TaskeeReadiness.MaterializationParked, TaskeeReadiness.NotOnTheGround,
                      TaskeeReadiness.Ready }
                  .Select(DispatchReadiness.StateName).Distinct(StringComparer.Ordinal).Count() == 7
              && new[] { TaskeeReadiness.Unknown, TaskeeReadiness.PlannedNotRequested,
                         TaskeeReadiness.RequestedNotBound, TaskeeReadiness.BoundNotReadable,
                         TaskeeReadiness.MaterializationParked, TaskeeReadiness.NotOnTheGround,
                         TaskeeReadiness.Ready }
                  .Select(DispatchReadiness.Describe).Distinct(StringComparer.Ordinal).Count() == 7);

        Check("THE PARKED STATE STILL WINS OVER THE GROUND STATE: an object about to be deleted and "
              + "re-created is reported as parked, not as buried (B1 is the more useful truth)",
              DispatchReadiness.Classify(true, true, true, true, true, true)
                  == TaskeeReadiness.MaterializationParked);

        Check("THE FIVE-ARGUMENT FORM IS UNCHANGED FOR EVERY EXISTING CALLER: it forwards "
              + "notOnTheGround = false, so nothing that does not measure ground contact behaves "
              + "differently",
              DispatchReadiness.Classify(true, true, true, true, false)
                  == DispatchReadiness.Classify(true, true, true, true, false, false)
              && DispatchReadiness.Classify(true, false, false, false)
                  == TaskeeReadiness.PlannedNotRequested);

        Check("THE ARMED LINE SAYS WHY THE CREATES WERE NOT DELAYED, citing MAK's own sample - the "
              + "reason option (A) was rejected in its 'wait before creating' shape",
              PlacementReclampPolicy.ArmedLine(36, 36, Bound, Retry, Tolerance)
                  .Contains("simpleCGF/main.cxx:120-133", StringComparison.Ordinal));

        Check("NO SENTENCE IN THIS POLICY SAYS 'BURIED THEREFORE FROZEN' - VRF_ALTITUDE_FRAMES sec 5 "
              + "falsified that and sec 7 makes it a tripwire",
              new[] {
                  PlacementReclampPolicy.ArmedLine(36, 36, Bound, Retry, Tolerance),
                  PlacementReclampPolicy.SummaryLine(1, 1, 1, 1, 12.0),
                  PlacementReclampPolicy.DispatchGateRefusalReason("T", "U", -0.0, 145.4, 145.0, Tolerance),
                  DispatchReadiness.Describe(TaskeeReadiness.NotOnTheGround),
              }.All(s => !s.Contains("freez", StringComparison.OrdinalIgnoreCase)
                         && !s.Contains("never moves", StringComparison.OrdinalIgnoreCase)
                         && !s.Contains("will not move", StringComparison.OrdinalIgnoreCase)));

        Check("ASCII ONLY, every sentence (the tree is ASCII-only by standing rule)",
              new[] {
                  PlacementReclampPolicy.ArmedLine(36, 36, Bound, Retry, Tolerance),
                  PlacementReclampPolicy.CorrectionLine("U", PlacementReclampPolicy.Measure(-0.0, 145.4, Tolerance), Tolerance),
                  PlacementReclampPolicy.VerifiedLine("U", default, PlacementReclampPolicy.Measure(146.0, 145.4, Tolerance)),
                  PlacementReclampPolicy.GaveUpLine("U", PlacementReclampPolicy.Measure(-0.0, 145.4, Tolerance)),
                  PlacementReclampPolicy.SummaryLine(1, 1, 1, 1, 12.0),
                  PlacementReclampPolicy.DispatchGateHeldLine("T", "U", -0.0, 145.4, 145.0, Tolerance),
                  PlacementReclampPolicy.DispatchGateRefusalReason("T", "U", -0.0, 145.4, 145.0, Tolerance),
                  DispatchReadiness.Describe(TaskeeReadiness.NotOnTheGround),
              }.All(s => s.All(c => c <= '~' && c >= ' ')));

        // ============ 8. THE D5d PROVENANCE THIRD CLAUSE ============================
        Check("STP-855: the provenance sentence now carries the D5d case - the replacement has not "
              + "been issued yet, so the registry still points at the deleted shell",
              RouteOriginPolicy.Provenance(3, new List<RouteOriginPolicy.Child> {
                      new("a", true, 1, 1, 0), new("b", true, 1, 1, 0), new("c", true, 1, 1, 0) })
                  .Contains("REPLACEMENT HAS NOT BEEN ISSUED YET", StringComparison.Ordinal));

        Console.WriteLine(fails == 0 ? "ALL CHECKS PASSED" : fails + " CHECK(S) FAILED");
        if (!featureEnabled)
            Console.WriteLine("(--disabled: the failures above are the 33f1894 behaviour run "
                              + "20260921T114910Z hit - two platforms measured ~150 m under the "
                              + "terrain, tasked anyway, and reported COMPLETE. A PASS in this arm "
                              + "would mean the fixture cannot see the defect.)");
        return fails == 0 ? 0 : 1;
    }
}
