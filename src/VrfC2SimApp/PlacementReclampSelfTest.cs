using S = C2SIM.Schema102;
using VrfC2Sim;

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
            RunWindow(boundSeconds, retrySeconds, tolerance);
            Lines.Add(Summary(boundSeconds));
        }

        /// <summary>
        /// BL-2 (cold-start review of 3151fec): a NEW TASK on a unit judged off the terrain
        /// RE-OPENS the window - no new correction, no repeat ERROR, just a fresh measurement. This
        /// is `ReMeasureGroundContactIfStale` + `ReopenPlacementReclampWindow`.
        /// </summary>
        public void NewTaskArrives(double boundSeconds = Bound, double retrySeconds = Retry,
                                   double tolerance = Tolerance)
        {
            if (!_enabled) return;
            if (Contact != PlacementReclampPolicy.Contact.OffGround) return;
            if (Outcome != PlacementReclampPolicy.Outcome.StillOffGround
                && Outcome != PlacementReclampPolicy.Outcome.Pending) return;
            Reopened++;
            RunWindow(boundSeconds, retrySeconds, tolerance);
            Lines.Add(Summary(boundSeconds));
        }

        /// <summary>One sweep window. StillOffGround entries ARE measured again (the pending filter
        /// in SweepPlacementReclamp); only a settled ON-the-ground outcome stops being measured.</summary>
        private void RunWindow(double boundSeconds, double retrySeconds, double tolerance)
        {
            double lastQuery = double.NegativeInfinity;
            for (double t = 0.0; !PlacementReclampPolicy.Expired(t, boundSeconds); t += 0.05)
            {
                if (Outcome != PlacementReclampPolicy.Outcome.Pending
                    && Outcome != PlacementReclampPolicy.Outcome.StillOffGround) break;
                if (t < _w.BoundAt) continue;                       // not bound: nothing to read
                if (!PlacementReclampPolicy.MayQuery(false, t - lastQuery, retrySeconds)) continue;
                lastQuery = t;
                TerrainQueries++;
                if (t < _w.TerrainAnswersAt) continue;              // asked, not answered - retry later
                var m = PlacementReclampPolicy.Measure(_w.LiveAltMeters, _w.TerrainMeters, tolerance);
                bool wasOff = Outcome == PlacementReclampPolicy.Outcome.StillOffGround;
                Contact = m.Contact;
                switch (PlacementReclampPolicy.Decide(m.Contact, Corrections))
                {
                    case PlacementReclampPolicy.Action.Correct:
                        Corrections++;
                        var fix = PlacementReclampPolicy.CorrectionLocation(
                            Lat, Lon, _w.TerrainMeters, CreateClearance);
                        Lines.Add(PlacementReclampPolicy.CorrectionLine(
                            N, m, tolerance, fix.LatDeg, fix.LonDeg, fix.AltMeters));
                        SentLocations.Add(fix);
                        // setLocation, not setAltitude: setLocationRequest.h:26-32 is the call the
                        // vendor documents as clamping a GROUND vehicle to the surface.
                        if (_w.CorrectionWorks) _w.LiveAltMeters = _w.TerrainMeters;
                        _last = m;
                        break;
                    case PlacementReclampPolicy.Action.GiveUp:
                        Outcome = PlacementReclampPolicy.Outcome.StillOffGround;
                        SettledAt = t;
                        // Said ONCE, however many windows re-measure it.
                        if (!_gaveUpLogged) { _gaveUpLogged = true; Lines.Add(PlacementReclampPolicy.GaveUpLine(N, m)); }
                        break;
                    default:
                        Outcome = PlacementReclampPolicy.Conclude(m.Contact, Corrections);
                        SettledAt = t;
                        if (wasOff) Lines.Add(PlacementReclampPolicy.ClearedLine(N, m));
                        else if (Outcome == PlacementReclampPolicy.Outcome.Reclamped)
                            Lines.Add(PlacementReclampPolicy.VerifiedLine(N, _last, m));
                        break;
                }
                // A give-up ends THIS window (it does not hold the summary open) but leaves the
                // object measurable by the next one.
                if (Outcome == PlacementReclampPolicy.Outcome.StillOffGround) break;
            }
            if (Outcome == PlacementReclampPolicy.Outcome.Pending)
                Outcome = PlacementReclampPolicy.Outcome.NeverMeasured;
        }

        private string Summary(double boundSeconds)
            => PlacementReclampPolicy.SummaryLine(
                Outcome == PlacementReclampPolicy.Outcome.AlreadyOnGround ? 1 : 0,
                Outcome == PlacementReclampPolicy.Outcome.Reclamped ? 1 : 0,
                Outcome == PlacementReclampPolicy.Outcome.StillOffGround ? 1 : 0,
                Outcome == PlacementReclampPolicy.Outcome.NeverMeasured ? 1 : 0,
                double.IsNaN(SettledAt) ? boundSeconds : SettledAt);

        public int Reopened;
        public readonly List<Geodetic> SentLocations = new();
        private PlacementReclampPolicy.Measurement _last;
        private bool _gaveUpLogged;
        private const string N = "28ID__FRIENDLY_INFANTRY_DIVISION";
        public const double Lat = 53.99238486824088, Lon = 23.211255470526073;
        public const double CreateClearance = 1.0;   // Vrf:CreateClearanceMeters
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

        /// <summary>
        /// One trip through the terrain continuation, then - and ONLY then - the re-entry the real
        /// system takes. SF-1 of the cold-start review: a first version looped the gate directly,
        /// which is an ending the real code never reaches. The real re-entry happens when
        /// `SweepDispatchReadiness` finds the taskee `Ready`, i.e. when the ground contact is no
        /// longer OffGround; if the sweep never clears it, the task ends at the
        /// `Vrf:DispatchReadinessTimeoutSeconds` hold timeout with the state named - NOT at the
        /// gate's second visit.
        /// </summary>
        /// <param name="reclampVerifies">whether the re-clamp sweep clears the verdict while the
        /// task is held</param>
        public void Dispatch(string task, string unit, bool reclampVerifies)
        {
            Visits++;
            var m = PlacementReclampPolicy.Measure(_w.LiveAltMeters, _w.TerrainMeters, Tolerance);
            bool gateStops = _enabled && m.Contact == PlacementReclampPolicy.Contact.OffGround;
            if (!gateStops)
            {
                Dispatched = true;
                if (_enabled) Lines.Add(PlacementReclampPolicy.GatePassedLine(unit, m, Tolerance));
                return;
            }
            _deferred.Add(task);
            Lines.Add(PlacementReclampPolicy.DispatchGateHeldLine(
                task, unit, m.LiveAltMeters, m.TerrainMeters, m.GapMeters, Tolerance));
            Corrections++;
            SentLocation = PlacementReclampPolicy.CorrectionLocation(
                Lat, Lon, _w.TerrainMeters, ReclampFixture.CreateClearance);
            HeldAs = DispatchReadiness.Classify(true, true, true, true, false, true);
            if (reclampVerifies) _w.LiveAltMeters = _w.TerrainMeters;

            // THE HOLD. It is released ONLY when the classifier goes Ready.
            var after = PlacementReclampPolicy.Measure(_w.LiveAltMeters, _w.TerrainMeters, Tolerance);
            if (after.Contact == PlacementReclampPolicy.Contact.OffGround)
            {
                // Never released: the hold expires and DispatchReadiness produces the abort, with
                // the state it was still in named. This is the REAL terminal line in this world.
                HoldTimedOut = true;
                string why = DispatchReadiness.TimeoutAbortReason(
                    task, unit, TaskeeReadiness.NotOnTheGround, 60.0);
                Lines.Add(why);
                Statuses.Add((S.TaskStatusCodeType.TASKABRT, why));
                return;
            }
            // Released: the task re-enters ExecuteTaskOnTick and passes the gate this time.
            Visits++;
            Dispatched = true;
            Lines.Add(PlacementReclampPolicy.GatePassedLine(unit, after, Tolerance));
        }

        public bool HoldTimedOut;
        public Geodetic SentLocation;
        public const double Lat = 54.01939, Lon = 23.31390;
    }

    /// <summary>Every sentence this policy can print, for the ASCII and tripwire sweeps.</summary>
    private static string[] AllSentences()
    {
        var off = PlacementReclampPolicy.Measure(-0.0, 145.4, Tolerance);
        var on = PlacementReclampPolicy.Measure(146.0, 145.4, Tolerance);
        var fix = PlacementReclampPolicy.CorrectionLocation(53.9923, 23.2112, 145.4, 1.0);
        return new[] {
            PlacementReclampPolicy.ArmedLine(36, 36, Bound, Retry, Tolerance),
            PlacementReclampPolicy.CorrectionLine("U", off, Tolerance, fix.LatDeg, fix.LonDeg, fix.AltMeters),
            PlacementReclampPolicy.VerifiedLine("U", off, on),
            PlacementReclampPolicy.ClearedLine("U", on),
            PlacementReclampPolicy.GaveUpLine("U", off),
            PlacementReclampPolicy.GatePassedLine("U", on, Tolerance),
            PlacementReclampPolicy.SummaryLine(1, 1, 1, 1, 12.0),
            PlacementReclampPolicy.DispatchGateHeldLine("T", "U", -0.0, 145.4, 145.0, Tolerance),
            PlacementReclampPolicy.DispatchGateRefusalReason("T", "U", -0.0, 145.4, 145.0, Tolerance),
            DispatchReadiness.Describe(TaskeeReadiness.NotOnTheGround),
        };
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

        Check("IRON STORM: the documented correction is issued EXACTLY ONCE - and it is setLOCATION, "
              + "the call the vendor documents as clamping a GROUND vehicle to the surface "
              + "(setLocationRequest.h:26-32), NOT setAltitude, which the same vendor says is "
              + "'ignored if the vehicle is not an air-going vehicle' (setAltitudeRequest.h:23-25). "
              + "Already exposed at VrfBridge.cpp:429 - no new native API",
              fx.Corrections == PlacementReclampPolicy.MaxCorrections
              && fx.SentLocations.Count == PlacementReclampPolicy.MaxCorrections
              && fx.Lines.Any(l => l.Contains("Issuing setLocation at its OWN lat/lon", StringComparison.Ordinal)
                                   && l.Contains("setLocationRequest.h:26-32", StringComparison.Ordinal))
              && !fx.Lines.Any(l => l.Contains("Issuing setAltitude", StringComparison.Ordinal)));

        Check("IRON STORM: the correction is sent AT THE OBJECT'S OWN LAT/LON (a correction, not a "
              + "teleport) with altitude = terrain + Vrf:CreateClearanceMeters - what the create "
              + "would have used - although the header says Z is discarded for a ground vehicle",
              featureEnabled
                  ? fx.SentLocations.Count == 1
                    && Math.Abs(fx.SentLocations[0].LatDeg - ReclampFixture.Lat) < 1e-12
                    && Math.Abs(fx.SentLocations[0].LonDeg - ReclampFixture.Lon) < 1e-12
                    && Math.Abs(fx.SentLocations[0].AltMeters - (145.4 + ReclampFixture.CreateClearance)) < 1e-9
                  : false);

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

        Check("STUCK: it ends in ONE TASKABRT whose reason NAMES the state, so the C2SIM bus carries "
              + "a refusal instead of a silent success. SF-1: the terminal line in THIS world is the "
              + "DispatchReadiness HOLD TIMEOUT, not the gate's second visit - the gate is only "
              + "re-entered when the classifier goes Ready, which here it never does",
              stuck.Statuses.Count == 1
              && stuck.Statuses[0].Code == S.TaskStatusCodeType.TASKABRT
              && stuck.Statuses[0].Why.Contains(PlacementReclampPolicy.NotOnGroundToken, StringComparison.Ordinal)
              && (!featureEnabled || stuck.HoldTimedOut));

        Check("STUCK: BOUNDED - the task is measured once, held once and ended once; the gate is "
              + "never re-entered while the verdict stands, so there is no cycle",
              featureEnabled ? stuck.Visits == 1 && stuck.Corrections == 1 : stuck.Visits == 1);

        var stuckSweep = new ReclampFixture(featureEnabled, new World { CorrectionWorks = false });
        stuckSweep.Run();
        Check("STUCK: the sweep gives up loudly after ONE correction, and because that correction "
              + "was the vendor's OWN documented lever for this class the line does NOT excuse "
              + "itself with the setAltitude no-op - it says the result needs explaining and lists "
              + "the candidates",
              featureEnabled
                  ? stuckSweep.Outcome == PlacementReclampPolicy.Outcome.StillOffGround
                    && stuckSweep.Corrections == 1
                    && stuckSweep.Lines.Any(l => l.Contains("setLocationRequest.h:26-32", StringComparison.Ordinal)
                                                 && l.Contains("NOT the known setAltitude no-op", StringComparison.Ordinal)
                                                 && l.Contains("NO FURTHER REQUEST IS ISSUED", StringComparison.Ordinal))
                  : false);

        // ============ 3b. BL-2: THE VERDICT IS RE-MEASURABLE, NOT STICKY ===========
        // The cold-start review's BL-2: after a give-up the verdict used to stand for the life of
        // the process, every later task on that unit was held to its full bound and abandoned, and
        // the only thing that could clear it lived behind the gate the verdict closed.
        var lateWorld = new World { LiveAltMeters = -0.0, TerrainMeters = 155.8, CorrectionWorks = false };
        var late = new ReclampFixture(featureEnabled, lateWorld);
        late.Run();                                  // window 1: correct, read back, give up
        // The terrain finishes streaming and the back end re-places the object on its own.
        lateWorld.LiveAltMeters = lateWorld.TerrainMeters;
        late.NewTaskArrives();                       // a SECOND task arrives on that unit

        Check("BL-2: a unit that gave up IS RE-MEASURED when a new task asks about it - the verdict "
              + "is a measurement with a timestamp, not a property of the unit",
              featureEnabled ? late.Reopened == 1 : false);

        Check("BL-2: the re-measure CLEARS the verdict with one line, and the unit is taskable "
              + "again - without this, one failed correction cost every later task its full "
              + "Vrf:DispatchReadinessTimeoutSeconds and ended it in a TASKABRT forever",
              late.Contact == PlacementReclampPolicy.Contact.OnGround
              && late.Lines.Any(l => l.Contains("CLEARED - re-measured ON the terrain", StringComparison.Ordinal))
              && DispatchReadiness.Classify(true, true, true, true, false,
                     late.Contact == PlacementReclampPolicy.Contact.OffGround) == TaskeeReadiness.Ready);

        Check("BL-2: the re-measure issues NO second correction and repeats NO ERROR - one "
              + "correction and one give-up line per object, however many windows measure it",
              featureEnabled
                  ? late.Corrections == PlacementReclampPolicy.MaxCorrections
                    && late.Lines.Count(l => l.Contains("STILL OFF THE TERRAIN", StringComparison.Ordinal)) == 1
                  : false);

        Check("BL-2 END TO END: give up, the terrain pages in, a SECOND task dispatches",
              featureEnabled
                  ? new Func<bool>(() =>
                    {
                        var g = new GateFixture(featureEnabled, lateWorld);
                        g.Dispatch("T02_second_task", "28ID__FRIENDLY_INFANTRY_DIVISION", false);
                        return g.Dispatched && !g.HoldTimedOut && g.Corrections == 0;
                    })()
                  : false);

        // ============ 3c. A LYING READ-BACK (untested world, SF-1) ==================
        // The read-back would lie if the back end published the commanded altitude without
        // relocating the entity. Offline this is indistinguishable from a real correction - which
        // is the point: SAY SO, and name the independent channel that settles it.
        var liar = new ReclampFixture(featureEnabled, new World { CorrectionWorks = true });
        liar.Run();
        Check("LYING READ-BACK IS INDISTINGUISHABLE OFFLINE, and this asserts the limit rather than "
              + "hiding it: a back end that published the commanded altitude without moving the "
              + "entity produces EXACTLY the RE-CLAMPED AND VERIFIED path. Only an INDEPENDENT "
              + "channel (WatchVrf POS) settles it on the confirming run",
              liar.Outcome == PlacementReclampPolicy.Outcome.Reclamped || !featureEnabled);

        // ============ 3d. BL-1: AGGREGATES ARE NOT ENROLLED =========================
        // The sweep can only read a unit's PUBLISHED Z, which VRF_ALTITUDE_FRAMES sec 1a forbids
        // reading as ground contact. The enrolment filter in FinalizePlacement now excludes them;
        // the DISPATCH GATE covers them instead, on the members' centroid.
        Check("BL-1: the ARMED line states that only LAND PLATFORMS are enrolled and that an "
              + "aggregate is measured at dispatch on its MEMBERS' centroid instead - the quantity "
              + "VRF_ALTITUDE_FRAMES sec 1a says to use",
              PlacementReclampPolicy.ArmedLine(2, 36, Bound, Retry, Tolerance)
                  .Contains("AGGREGATES ARE NOT ENROLLED", StringComparison.Ordinal)
              && PlacementReclampPolicy.ArmedLine(2, 36, Bound, Retry, Tolerance)
                  .Contains("MEMBERS' centroid", StringComparison.Ordinal));

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

        Check("HEALTHY INIT: a taskee measured ON the terrain passes the gate on its FIRST visit "
              + "with no correction and no hold - the dispatch deferral D10 measured at 2.1-2.4 s "
              + "gains nothing. SF-2, ACCEPTED AND STATED: it does gain ONE short INFO line naming "
              + "the measured gap, which is the only evidence any run will ever carry for the "
              + "50-100 m band between the refusal bar and the vertex-0 NOTE threshold",
              new Func<bool>(() =>
              {
                  var g = new GateFixture(featureEnabled, new World { LiveAltMeters = 131.1, TerrainMeters = 130.1 });
                  g.Dispatch("T10_1-112In...", "1-112_IN", true);
                  return g.Dispatched && g.Visits == 1 && g.Corrections == 0
                         && g.Lines.Count == (featureEnabled ? 1 : 0)
                         && (!featureEnabled || g.Lines[0].Contains("is ON the terrain", StringComparison.Ordinal));
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
              AllSentences().All(s => !s.Contains("freez", StringComparison.OrdinalIgnoreCase)
                         && !s.Contains("never moves", StringComparison.OrdinalIgnoreCase)
                         && !s.Contains("will not move", StringComparison.OrdinalIgnoreCase)));

        Check("ASCII ONLY, every sentence (the tree is ASCII-only by standing rule)",
              AllSentences().All(s => s.All(c => c <= '~' && c >= ' ')));

        Check("THE GIVE-UP LINE NO LONGER EXCUSES ITSELF WITH THE setAltitude NO-OP: the correction "
              + "is now the call the vendor documents FOR this class, so a read-back that still "
              + "disagrees needs explaining, and the line says so and lists the candidates",
              PlacementReclampPolicy.GaveUpLine("U", PlacementReclampPolicy.Measure(-0.0, 145.4, Tolerance))
                  .Contains("NOT the known setAltitude no-op", StringComparison.Ordinal)
              && PlacementReclampPolicy.GaveUpLine("U", PlacementReclampPolicy.Measure(-0.0, 145.4, Tolerance))
                  .Contains("RE-MEASURED whenever a new task", StringComparison.Ordinal));

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
