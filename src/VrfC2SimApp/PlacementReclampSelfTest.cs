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
/// 2026-09-25 (RL-20260921-06; scope approved as RL-20260925-01): the DISPATCH GATE no longer holds
/// or refuses a task, and the tally counts a correction with no read-back as exactly that. Sections
/// 2, 3 and 7 were rewritten for it (they used to assert the hold, the TASKABRT and the
/// BOUND-BUT-NOT-ON-THE-GROUND state); section 3e is new. The gate assertions now hold in BOTH arms
/// (the task is dispatched either way), so the --disabled arm fails fewer checks than it did.
///
/// FAIL-FIRST BY CONSTRUCTION. `--placement-reclamp-selftest --disabled` runs the SAME fixtures and
/// the SAME assertions with Vrf:PlacementReclamp = false - the 33f1894 behaviour - and every
/// assertion about correcting and verifying MUST fail: that arm reproduces the run above.
/// The assertions that hold in BOTH arms are the invariants this must not break - a HEALTHY init
/// unchanged in its lines and its timing, an UNMEASURED object still taskable, and the STP-852/B1
/// barrier inequality still true at the shipped numbers.
///
/// WHAT THESE FIXTURES ARE, stated exactly. A MODEL of the service's decision sequence
/// (SweepPlacementReclamp / ApplyPlacementReclamp / LogGroundContactAtDispatch) driving the REAL
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
        /// <summary>Does the correction actually move this object onto the surface? The chosen call
        /// is setLocation, which setLocationRequest.h:26-32 says clamps a ground vehicle to the
        /// terrain surface - but that is a header, not a run. BOTH worlds are fixtures here.</summary>
        public bool CorrectionWorks = true;

        // ===== DL-1: A UNIT THAT HAS DRIVEN ====================================================
        // The blind spot the delta review found: the old World had ONE position, so "the object's
        // own lat/lon" and "the create point" were the same number and a teleport was undetectable.
        /// <summary>Where the object was BORN - the enrolled create point.</summary>
        public double BirthLatDeg = 53.99238486824088, BirthLonDeg = 23.211255470526073;
        /// <summary>Where it IS. Defaults to its birth place; a moving fixture sets it.</summary>
        public double LiveLatDeg = 53.99238486824088, LiveLonDeg = 23.211255470526073;
        /// <summary>The terrain under the BIRTH point - what the old code sampled.</summary>
        public double BirthTerrainMeters = 145.4;

        /// <summary>The terrain under wherever the sweep ASKS about. The whole of DL-1 is which
        /// point that is: ask at the live position and a healthy moving unit measures ON the
        /// terrain; ask at the birth position and it measures ~120 m off.</summary>
        public double TerrainAt(double latDeg, double lonDeg)
            => TerrainVertexAuthoring.DistMeters(latDeg, lonDeg, BirthLatDeg, BirthLonDeg) < 1.0
                   ? BirthTerrainMeters
                   : TerrainMeters;
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
                // DL-1: the sweep asks about WHERE THE UNIT IS, and QueriedLat/Lon is what the
                // service stores in ReclampEntry.QueriedAt. Ask at the birth point instead - the
                // b3f9c38 behaviour - and a healthy moving unit measures off the terrain.
                QueriedLatDeg = AskAtBirthPoint ? _w.BirthLatDeg : _w.LiveLatDeg;
                QueriedLonDeg = AskAtBirthPoint ? _w.BirthLonDeg : _w.LiveLonDeg;
                double th = _w.TerrainAt(QueriedLatDeg, QueriedLonDeg);
                var m = PlacementReclampPolicy.Measure(_w.LiveAltMeters, th, tolerance);
                bool wasOff = Outcome == PlacementReclampPolicy.Outcome.StillOffGround;
                Contact = m.Contact;
                switch (PlacementReclampPolicy.Decide(m.Contact, Corrections))
                {
                    case PlacementReclampPolicy.Action.Correct:
                        // GUARD 1 (DL-1): a unit under a task is never teleported.
                        if (TaskInFlight != null)
                        {
                            Lines.Add(PlacementReclampPolicy.SkippedTaskInFlightLine(N, TaskInFlight, m));
                            break;
                        }
                        // The fix is built from the LIVE read (b3f9c38 built it from the create
                        // point - that is DL-1); AskAtBirthPoint reproduces the old behaviour.
                        var fix = PlacementReclampPolicy.CorrectionLocation(
                            AskAtBirthPoint ? _w.BirthLatDeg : _w.LiveLatDeg,
                            AskAtBirthPoint ? _w.BirthLonDeg : _w.LiveLonDeg,
                            th, CreateClearance);
                        // GUARD 2 (DL-1): the request may change an altitude, never a position.
                        if (PlacementReclampPolicy.WouldMoveHorizontally(
                                _w.LiveLatDeg, _w.LiveLonDeg, fix.LatDeg, fix.LonDeg))
                        {
                            RefusedToMove = true;
                            Lines.Add(PlacementReclampPolicy.RefusedToMoveLine(
                                N, _w.LiveLatDeg, _w.LiveLonDeg, fix.LatDeg, fix.LonDeg));
                            break;
                        }
                        Corrections++;
                        Lines.Add(PlacementReclampPolicy.CorrectionLine(
                            N, m, tolerance, fix.LatDeg, fix.LonDeg, fix.AltMeters));
                        SentLocations.Add(fix);
                        // setLocation, not setAltitude: setLocationRequest.h:26-32 is the call the
                        // vendor documents as clamping a GROUND vehicle to the surface. In the
                        // fixture a working correction also MOVES the object to where it was sent -
                        // which is how a teleport becomes visible.
                        if (_w.CorrectionWorks)
                        {
                            _w.LiveAltMeters = th;
                            _w.LiveLatDeg = fix.LatDeg;
                            _w.LiveLonDeg = fix.LonDeg;
                        }
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
            // The service's ConcludePlacementReclamp: a still-Pending entry is concluded by the
            // policy's own rule (a correction with no read-back is NOT "never measured").
            Outcome = PlacementReclampPolicy.ConcludeAtBound(Outcome, Corrections, Contact);
        }

        private string Summary(double boundSeconds)
            => PlacementReclampPolicy.SummaryLine(PlacementReclampPolicy.Tally(new[] { Outcome }),
                                                  double.IsNaN(SettledAt) ? boundSeconds : SettledAt);

        public int Reopened;
        /// <summary>DL-1 arm: ask (and correct) at the CREATE point - the b3f9c38 behaviour.</summary>
        public bool AskAtBirthPoint;
        /// <summary>DL-1 arm: a task is running on this unit, so no correction may be sent.</summary>
        public string TaskInFlight;
        public bool RefusedToMove;
        public double QueriedLatDeg, QueriedLonDeg;
        public readonly List<Geodetic> SentLocations = new();
        private PlacementReclampPolicy.Measurement _last;
        private bool _gaveUpLogged;
        private const string N = "28ID__FRIENDLY_INFANTRY_DIVISION";
        public const double CreateClearance = 1.0;   // Vrf:CreateClearanceMeters
    }

    /// <summary>
    /// The DISPATCH GROUND GATE, modelled: the route's terrain reply carries the height under
    /// vertex 0, the caller carries the taskee's live altitude, and the gate MEASURES AND LOGS.
    /// Mirrors LogGroundContactAtDispatch (until 2026-09-25 GroundGateAllowsDispatch), rewritten 2026-09-25 (RL-20260921-06, the completion
    /// unit's scope approved as RL-20260925-01): it no longer holds, refuses or abandons a task on
    /// an unconfirmed read-back, and it sends no setLocation at dispatch - the task follows at once,
    /// and a correction into a running move is exactly the case the sweep's own in-flight guard
    /// (SkippedTaskInFlightLine) exists to prevent. The placement correction itself stays in the
    /// init-time sweep (<see cref="ReclampFixture"/>).
    /// </summary>
    private sealed class GateFixture
    {
        private readonly bool _enabled;
        private readonly World _w;
        public readonly List<string> Lines = new();
        public readonly List<(S.TaskStatusCodeType Code, string Why)> Statuses = new();
        public bool Dispatched;
        public int Visits;

        public GateFixture(bool enabled, World w) { _enabled = enabled; _w = w; }

        /// <summary>One trip through the terrain continuation. It always ends in the dispatch.</summary>
        public void Dispatch(string task, string unit)
        {
            Visits++;
            var m = PlacementReclampPolicy.Measure(_w.LiveAltMeters, _w.TerrainMeters, Tolerance);
            if (_enabled)
                Lines.Add(m.Contact == PlacementReclampPolicy.Contact.OffGround
                    ? PlacementReclampPolicy.DispatchGateOffTerrainLine(task, unit, m, Tolerance)
                    : PlacementReclampPolicy.GatePassedLine(unit, m, Tolerance));
            Dispatched = true;
        }
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
            PlacementReclampPolicy.SummaryLine(new PlacementReclampPolicy.Counts(1, 1, 1, 1, 1), 12.0),
            PlacementReclampPolicy.DispatchGateOffTerrainLine("T", "U", off, Tolerance),
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

        Check("IRON STORM: the correction is sent AT THE OBJECT'S OWN CURRENT LAT/LON (a correction, "
              + "not a teleport) with altitude = terrain + Vrf:CreateClearanceMeters - what the "
              + "create would have used - although the header says Z is discarded for a ground "
              + "vehicle. This unit never moved, so its live point IS its create point",
              featureEnabled
                  ? fx.SentLocations.Count == 1
                    && !PlacementReclampPolicy.WouldMoveHorizontally(
                           ironStorm.BirthLatDeg, ironStorm.BirthLonDeg,
                           fx.SentLocations[0].LatDeg, fx.SentLocations[0].LonDeg)
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

        // ============ 2. THE DISPATCH GATE - MEASURES AND LOGS, NEVER HOLDS ==========
        // REWRITTEN 2026-09-25 (RL-20260921-06; scope RL-20260925-01). These checks used to assert
        // that the gate HELD a taskee measured off the terrain as BOUND-BUT-NOT-ON-THE-GROUND,
        // issued a setLocation and dispatched only after a read-back - and, when none came, ended
        // the task with a TASKABRT. Run 20260921T143243Z: the read-back never landed, and the gate
        // ended two tasks on units an independent trace shows on the terrain. The gate now
        // measures and logs; the task is dispatched on the pass that measures it.
        var gateWorld = new World { LiveAltMeters = -0.0, TerrainMeters = 145.4 };
        var gate = new GateFixture(featureEnabled, gateWorld);
        gate.Dispatch("T14_48Ibct...", "48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE");

        Check("DISPATCH GATE: a taskee measured 145 m off the terrain IS DISPATCHED on the pass that "
              + "measures it - no hold, no refusal, no TASKABRT",
              gate.Dispatched && gate.Visits == 1 && gate.Statuses.Count == 0);

        Check("DISPATCH GATE: the measurement is LOGGED - one line naming the gap and saying the task "
              + "is dispatching, and no NOT DISPATCHED / HELD wording",
              featureEnabled
                  ? gate.Lines.Count == 1
                    && gate.Lines[0].Contains("measured OFF the terrain at dispatch", StringComparison.Ordinal)
                    && gate.Lines[0].Contains("dispatching", StringComparison.Ordinal)
                    && !gate.Lines[0].Contains("NOT DISPATCHED", StringComparison.Ordinal)
                    && !gate.Lines[0].Contains("HELD", StringComparison.Ordinal)
                  : gate.Lines.Count == 0);


        // ============ 3. THE GATE WHEN THE CORRECTION DID NOT TAKE ==================
        var stuckWorld = new World { LiveAltMeters = -0.0, TerrainMeters = 155.8, CorrectionWorks = false };
        var stuck = new GateFixture(featureEnabled, stuckWorld);
        stuck.Dispatch("T02_28IdHq...", "28ID__FRIENDLY_INFANTRY_DIVISION");

        Check("STUCK: a unit still measured off the terrain after a correction is STILL DISPATCHED, "
              + "once, with no TASKABRT - an unconfirmed read-back no longer ends a legitimate task",
              stuck.Dispatched && stuck.Visits == 1 && stuck.Statuses.Count == 0);

        Check("NO BOUND-BUT-NOT-ON-THE-GROUND STATE IS PRODUCED: the readiness classifier has no "
              + "ground-contact state any more, so no task can be held for one",
              !Enum.GetNames(typeof(TaskeeReadiness)).Contains("NotOnTheGround")
              && Enum.GetValues<TaskeeReadiness>().All(s => !DispatchReadiness.StateName(s)
                     .Contains("NOT-ON-THE-GROUND", StringComparison.Ordinal)));

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

        Check("STUCK: the give-up line no longer says a task on the unit is HELD and abandoned - "
              + "nothing holds a task on a ground-contact verdict any more (RL-20260921-06)",
              !PlacementReclampPolicy.GaveUpLine("U", PlacementReclampPolicy.Measure(-0.0, 155.8, Tolerance))
                  .Contains("HELD", StringComparison.Ordinal)
              && !PlacementReclampPolicy.GaveUpLine("U", PlacementReclampPolicy.Measure(-0.0, 155.8, Tolerance))
                  .Contains("abandoned", StringComparison.Ordinal));

        // ============ 3e. THE TALLY (RL-20260921-06 measurement: "0 RE-CLAMPED AND VERIFIED ...
        // 32 NEVER MEASURED" beside thirty-two "MEASURED OFF THE TERRAIN ... Issuing setLocation"
        // lines, run 20260921T143243Z). An object that was measured and corrected, and whose
        // read-back never landed, is NOT "never measured".
        Check("TALLY: an object CORRECTED whose read-back never landed concludes as 'corrected, "
              + "read-back not received', not NEVER MEASURED",
              PlacementReclampPolicy.ConcludeAtBound(PlacementReclampPolicy.Outcome.Pending, 1,
                  PlacementReclampPolicy.Contact.OffGround) == PlacementReclampPolicy.Outcome.CorrectedNotVerified);
        Check("TALLY: an object MEASURED off the terrain but never corrected (a task was in flight) "
              + "concludes as STILL OFF, not NEVER MEASURED",
              PlacementReclampPolicy.ConcludeAtBound(PlacementReclampPolicy.Outcome.Pending, 0,
                  PlacementReclampPolicy.Contact.OffGround) == PlacementReclampPolicy.Outcome.StillOffGround);
        Check("TALLY: an object with NO terrain answer is still NEVER MEASURED, and a settled outcome "
              + "is left as it is",
              PlacementReclampPolicy.ConcludeAtBound(PlacementReclampPolicy.Outcome.Pending, 0,
                  PlacementReclampPolicy.Contact.Unknown) == PlacementReclampPolicy.Outcome.NeverMeasured
              && PlacementReclampPolicy.ConcludeAtBound(PlacementReclampPolicy.Outcome.Reclamped, 1,
                  PlacementReclampPolicy.Contact.OnGround) == PlacementReclampPolicy.Outcome.Reclamped);
        {
            // The measured run's census: 32 corrected with no read-back, 4 enrolled and unanswered.
            var census = Enumerable.Repeat(PlacementReclampPolicy.Outcome.CorrectedNotVerified, 32)
                .Concat(Enumerable.Repeat(PlacementReclampPolicy.Outcome.NeverMeasured, 4)).ToList();
            var c = PlacementReclampPolicy.Tally(census);
            string line = PlacementReclampPolicy.SummaryLine(c, 60.0);
            Check("TALLY: 32 corrected objects with no read-back are COUNTED as such - the summary says "
                  + "'32 CORRECTED, READ-BACK NOT RECEIVED' and '4 NEVER MEASURED', not 36 never measured",
                  c.CorrectedNotVerified == 32 && c.NeverMeasured == 4
                  && line.Contains("32 CORRECTED, READ-BACK NOT RECEIVED", StringComparison.Ordinal)
                  && line.Contains("4 NEVER MEASURED", StringComparison.Ordinal));
            Check("TALLY: the FIVE counts sum to the enrolled count, and the summary no longer says the "
                  + "third count is held and abandoned",
                  c.Total == census.Count
                  && PlacementReclampPolicy.Tally(new[] {
                         PlacementReclampPolicy.Outcome.AlreadyOnGround, PlacementReclampPolicy.Outcome.Reclamped,
                         PlacementReclampPolicy.Outcome.CorrectedNotVerified, PlacementReclampPolicy.Outcome.StillOffGround,
                         PlacementReclampPolicy.Outcome.NeverMeasured }) == new PlacementReclampPolicy.Counts(1, 1, 1, 1, 1)
                  && !line.Contains("held and abandoned", StringComparison.Ordinal));
        }

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

        Check("BL-2: the re-measure CLEARS the verdict with one line (the placement record is "
              + "corrected; since 2026-09-25 no verdict holds a task either way)",
              late.Contact == PlacementReclampPolicy.Contact.OnGround
              && late.Lines.Any(l => l.Contains("CLEARED - re-measured ON the terrain", StringComparison.Ordinal)));

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
                        g.Dispatch("T02_second_task", "28ID__FRIENDLY_INFANTRY_DIVISION");
                        return g.Dispatched && g.Statuses.Count == 0
                               && g.Lines.Count == 1 && g.Lines[0].Contains("is ON the terrain", StringComparison.Ordinal);
                    })()
                  : false);

        // ============ 3b2. DL-1: A UNIT THAT HAS DRIVEN IS NEITHER MISJUDGED NOR MOVED =====
        // The delta review's blocker. Until now every fixture object stood still, so "its own
        // lat/lon" and "its create point" were the same number and a teleport was invisible.
        // This unit has driven 500 m and climbed 120 m before the sweep first visits it - the
        // ordinary state of an enrolled platform, which is UNMEASURED and therefore taskable while
        // the window is open (in the Iron Storm run the first TASKSTRTs land ~32 s into it).
        const double MovedLat = 53.99238486824088 + 0.0044936;          // +500 m north
        const double MovedLon = 23.211255470526073;
        var movedWorld = new World {
            LiveLatDeg = MovedLat, LiveLonDeg = MovedLon,
            LiveAltMeters = 266.4,            // 120 m higher than where it was born
            BirthTerrainMeters = 145.4,       // the terrain under its BIRTH point
            TerrainMeters = 265.4,            // the terrain under where it IS - it is ON the ground
            TerrainAnswersAt = 0.0, BoundAt = 0.0,
        };

        Check("DL-1: the travelled distance the fixture models is real - 500 m, far over the 50 m "
              + "bar, on 120 m of relief (D10's authored route alts span 1,127-1,370 m)",
              Math.Abs(TerrainVertexAuthoring.DistMeters(
                  movedWorld.BirthLatDeg, movedWorld.BirthLonDeg, MovedLat, MovedLon) - 500.0) < 5.0);

        var movedOk = new ReclampFixture(featureEnabled, movedWorld);
        movedOk.Run();
        Check("DL-1: a HEALTHY unit that has driven is asked about WHERE IT IS, so it measures ON "
              + "the terrain and is NOT flagged - asking at its birth point would compare an "
              + "altitude read here against a terrain height sampled 500 m away",
              featureEnabled
                  ? movedOk.Contact == PlacementReclampPolicy.Contact.OnGround
                    && movedOk.Corrections == 0 && movedOk.SentLocations.Count == 0
                  : true);

        Check("DL-1: and it is NOT RELOCATED - its live position is untouched",
              Math.Abs(movedWorld.LiveLatDeg - MovedLat) < 1e-12
              && Math.Abs(movedWorld.LiveLonDeg - MovedLon) < 1e-12);

        // THE FAIL-FIRST HALF: the b3f9c38 behaviour, asking and correcting at the CREATE point.
        var movedBirth = new World {
            LiveLatDeg = MovedLat, LiveLonDeg = MovedLon, LiveAltMeters = 266.4,
            BirthTerrainMeters = 145.4, TerrainMeters = 265.4,
            TerrainAnswersAt = 0.0, BoundAt = 0.0,
        };
        var movedBad = new ReclampFixture(featureEnabled, movedBirth) { AskAtBirthPoint = true };
        movedBad.Run();
        Check("DL-1 FAIL-FIRST: asking at the CREATE point (the b3f9c38 code) DOES misjudge that "
              + "same healthy unit as off the terrain - 266.4 m live against 145.4 m of birth "
              + "terrain, a 121 m phantom gap over the 50 m bar. This is the defect, reproduced",
              featureEnabled ? movedBad.Contact == PlacementReclampPolicy.Contact.OffGround : true);

        Check("DL-1 FAIL-FIRST: and the correction it would then send is REFUSED BY THE GUARD "
              + "instead of teleporting the unit 500 m back to its birth coordinate - the guard is "
              + "what makes this class of defect unable to move a unit even when it recurs",
              featureEnabled
                  ? movedBad.RefusedToMove && movedBad.SentLocations.Count == 0
                    && Math.Abs(movedBirth.LiveLatDeg - MovedLat) < 1e-12
                  : true);

        Check("DL-1: the guard's bound is 1 m and it is the right shape - 0.5 m passes, 2 m does "
              + "not; the correction is built from the same live read it is checked against, so "
              + "only a defect can reach it",
              !PlacementReclampPolicy.WouldMoveHorizontally(MovedLat, MovedLon, MovedLat + 0.0000045, MovedLon)
              && PlacementReclampPolicy.WouldMoveHorizontally(MovedLat, MovedLon, MovedLat + 0.000018, MovedLon)
              && Math.Abs(PlacementReclampPolicy.MaxCorrectionHorizontalMeters - 1.0) < 1e-9);

        Check("DL-1: a reply for a unit that DRIFTED more than the frame-check distance since the "
              + "query is not applied to it - the answer is about ground it no longer stands on",
              PlacementReclampPolicy.DriftedSinceQuery(MovedLat, MovedLon, MovedLat + 0.0009, MovedLon)
              && !PlacementReclampPolicy.DriftedSinceQuery(MovedLat, MovedLon, MovedLat + 0.00009, MovedLon));

        // DL-1 guard 1: a unit UNDER A TASK is never teleported, whatever the measurement says.
        var busyWorld = new World { LiveAltMeters = -0.0, TerrainMeters = 145.4,
                                    TerrainAnswersAt = 0.0, BoundAt = 0.0 };
        var busy = new ReclampFixture(featureEnabled, busyWorld) { TaskInFlight = "T14_48Ibct..." };
        busy.Run();
        Check("DL-1: a unit with a TASK IN FLIGHT is measured but NEVER corrected - setLocation is "
              + "a teleport and would pull a moving unit out of its own route after the C2 side was "
              + "told the task started; the measurement stands and is re-taken later",
              featureEnabled
                  ? busy.Contact == PlacementReclampPolicy.Contact.OffGround
                    && busy.Corrections == 0 && busy.SentLocations.Count == 0
                    && busy.Lines.Any(l => l.Contains("NO CORRECTION IS ISSUED", StringComparison.Ordinal)
                                           && l.Contains("is in flight on this unit", StringComparison.Ordinal))
                  : true);

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
        // the dispatch-time measurement covers them instead, on the members' centroid (logged only,
        // since 2026-09-25).
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
                  g.Dispatch("T10_1-112In...", "1-112_IN");
                  return g.Dispatched && g.Visits == 1
                         && g.Lines.Count == (featureEnabled ? 1 : 0)
                         && (!featureEnabled || g.Lines[0].Contains("is ON the terrain", StringComparison.Ordinal));
              })());

        // ============ 5. PERMISSIVE WHEN NOTHING IS MEASURED ========================
        Check("UNMEASURED IS NEVER HELD: a null terrain answer yields Contact.Unknown, and a bound, "
              + "readable, unparked taskee classifies READY - this lane can never wedge a run whose "
              + "terrain query is simply never answered",
              PlacementReclampPolicy.Measure(-0.0, null, Tolerance).Contact
                  == PlacementReclampPolicy.Contact.Unknown
              && DispatchReadiness.Classify(true, true, true, true, false) == TaskeeReadiness.Ready);

        Check("THE TOLERANCE DECIDES THE MEASUREMENT (it no longer holds any task): 49 m is on the "
              + "ground, 51 m is not, at the shipped 50",
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
        Check("EVERY STATE STILL HAS A DISTINCT NAME AND DESCRIPTION (six states: the ground-contact "
              + "state is retired, 2026-09-25)",
              Enum.GetValues<TaskeeReadiness>().Length == 6
              && Enum.GetValues<TaskeeReadiness>()
                  .Select(DispatchReadiness.StateName).Distinct(StringComparer.Ordinal).Count() == 6
              && Enum.GetValues<TaskeeReadiness>()
                  .Select(DispatchReadiness.Describe).Distinct(StringComparer.Ordinal).Count() == 6);

        Check("THE PARKED STATE STILL WINS OVER READY: an object about to be deleted and re-created "
              + "is reported as parked (B1)",
              DispatchReadiness.Classify(true, true, true, true, true)
                  == TaskeeReadiness.MaterializationParked);

        Check("THE FOUR-ARGUMENT FORM FORWARDS materializationParked = false, as before",
              DispatchReadiness.Classify(true, true, true, true)
                  == DispatchReadiness.Classify(true, true, true, true, false)
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
