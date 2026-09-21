using S = C2SIM.Schema102;

namespace VrfC2SimApp;

/// <summary>
/// D5b FIXTURE-LEVEL SELF-TEST: an order that races the initialization.
///
/// THE RUN THIS EXISTS FOR (runs\20260921T072530Z_wayb, the Way B rehearsal). The order reached
/// the bus 0.31 s after the init. The interface log reads:
///     :120  Init dispatched: 6 units ... queued for creation.
///     :122  C2SIM Order received (3521 bytes).
///     :150  DROPPING TASK 'T_R5_TK1' BECAUSE UNIT ... (1.BdeHQ~PXY) WAS NOT CREATED.
///     :162  PLACEMENT: PLATFORM 1.BdeHQ~PXY ... created at authored lat/lon
/// The unit was created ONE SECOND AFTER its task was dropped for not existing, and T_R5_PL1 was
/// REFUSED because a live location "could not be read". The Way A control (20260921T052350Z_run)
/// is the same build, init and order, and differs in nothing but the order of two messages.
///
/// FAIL-FIRST BY CONSTRUCTION. `--dispatch-readiness-selftest --disabled` runs the SAME fixtures
/// and the SAME assertions with Vrf:DispatchReadinessTimeoutSeconds = 0 - the 1d0fb69 build - and
/// the deferral assertions MUST fail: that arm reproduces the D5b lines verbatim and IS the
/// defect. The assertions that hold in BOTH arms are the invariants the fix must not break: an
/// unknown taskee refused promptly, and a task that was never held unchanged in its lines and its
/// timing.
///
/// Offline: no bridge, no federation, no server, no clock. WHAT THESE FIXTURES ARE, stated exactly
/// (cold-start review of 945e054): they are a MODEL of the service's decision sequence -
/// TryDispatchOrHold / HoldThenDispatchAsync, the composition await's backstop, and the init
/// barrier that gates an order-time materialization - driving the REAL <see cref="DispatchReadiness"/>
/// rules and the REAL sentences. What they PROVE is that those rules and sentences are right, and
/// that the sequence built on them has the properties claimed. They do NOT execute
/// VrfC2SimService's own methods; the report lists what is therefore left to the confirming run.
/// </summary>
public static class DispatchReadinessSelfTest
{
    private const double Timeout = 60.0;      // the shipped Vrf:DispatchReadinessTimeoutSeconds
    private const double Tick = 0.05;         // VrfC2SimService.TickLoop's Thread.Sleep(50)
    private const double SimRatio = 3.0;      // an arbitrary sim/wall ratio, so the two labels differ
    private const double Never = double.PositiveInfinity;

    /// <summary>When each observation about one taskee becomes true, in WALL seconds from the
    /// initialization. Never = it does not happen in this scenario.</summary>
    private sealed class Timeline
    {
        public bool PlannedAtInit = true;
        public double RequestedAt = Never;    // the create was issued (init terrain reply landed)
        public double BoundAt = Never;        // ObjectCreated bound the requested name
        public double ReadableAt = Never;     // TryGetEntityGeodetic first succeeded
    }

    /// <summary>
    /// One task's trip through the dispatch decision. Models TryDispatchOrHold (tick thread: one
    /// classification, then either ExecuteTaskOnTick in the same action or a hold) and
    /// HoldThenDispatchAsync (the bounded wait, re-evaluated every tick and on a back-end loss).
    /// </summary>
    private sealed class DispatchFixture
    {
        private readonly double _timeout;
        private readonly Timeline _tl;
        private readonly double _backendLostAt;

        public readonly List<string> Lines = new();
        public readonly List<(S.TaskStatusCodeType Code, string Why)> Statuses = new();
        public readonly List<string> Abandoned = new();
        public double DispatchedAt = double.NaN;
        public int HoldLines, ReleaseLines;
        public TaskeeReadiness HoldStartState = TaskeeReadiness.Ready;

        public DispatchFixture(double timeout, Timeline tl, double backendLostAt = Never)
        { _timeout = timeout; _tl = tl; _backendLostAt = backendLostAt; }

        private TaskeeReadiness StateAt(double t) => DispatchReadiness.Classify(
            _tl.PlannedAtInit, t >= _tl.RequestedAt, t >= _tl.BoundAt, t >= _tl.ReadableAt);

        /// <summary>The task reaches its dispatch (past the start delay, the STREND gate and the
        /// composition gate) at <paramref name="arriveAt"/>.</summary>
        public void Dispatch(string taskName, string unitName, double arriveAt)
        {
            var start = StateAt(arriveAt);
            if (!DispatchReadiness.ShouldHold(start, _timeout, arriveAt >= _backendLostAt))
            {
                Execute(taskName, unitName, arriveAt, start);     // the UNCHANGED path
                return;
            }
            HoldStartState = start;
            HoldLines++;
            Lines.Add(DispatchReadiness.HoldLine(taskName, unitName, start, _timeout));
            double deadline = arriveAt + _timeout;
            var last = start;
            for (double t = arriveAt; t <= deadline; t += Tick)
            {
                last = StateAt(t);
                if (last == TaskeeReadiness.Ready)
                {
                    ReleaseLines++;
                    Lines.Add(DispatchReadiness.ReleasedLine(taskName, unitName, start, t - arriveAt,
                                                             (t - arriveAt) * SimRatio, "sim"));
                    Execute(taskName, unitName, t, last);
                    return;
                }
                if (t >= _backendLostAt)
                {
                    Abort(taskName, DispatchReadiness.BackendLostAbortReason(
                        taskName, unitName, last, t - arriveAt));
                    return;
                }
            }
            Abort(taskName, DispatchReadiness.TimeoutAbortReason(taskName, unitName, last, _timeout));
        }

        /// <summary>ExecuteTaskOnTick, in the only two shapes this fixture can reach: the dispatch,
        /// and the two aborts at VrfC2SimService.cs :3117 and :3313 - the D5b lines themselves.</summary>
        private void Execute(string taskName, string unitName, double t, TaskeeReadiness state)
        {
            if (state == TaskeeReadiness.Ready) { DispatchedAt = t; return; }
            Abort(taskName, state == TaskeeReadiness.BoundNotReadable
                ? $"REFUSED at dispatch: task '{taskName}' has no performing unit to execute it - "
                  + $"{unitName}'s live location could not be read from the simulation"
                : state == TaskeeReadiness.Unknown
                    ? $"REFUSED: taskee of task '{taskName}' is not in the C2SIM initialization - "
                      + "this interface has no unit to task"
                    : $"DROPPED: unit {unitName} has no VR-Forces object bound to its name - it was "
                      + "never created, or its created object could not be correlated to the name we "
                      + "requested");
        }

        private void Abort(string taskName, string why)
        {
            Abandoned.Add(taskName);                       // TaskSequencer.NotifyAbandoned
            Lines.Add(why);
            Statuses.Add((S.TaskStatusCodeType.TASKABRT, why));
        }
    }

    /// <summary>
    /// THE OVERLAP (brief item 2). The back end's command stream, in the order the interface issues
    /// it, with the init's own creates and the order-driven materialization's delete/re-creates on
    /// the SAME timeline - which is the only way to see them interleave.
    /// </summary>
    private sealed class BarrierFixture
    {
        private readonly bool _barrierOn;
        private readonly double _barrierSeconds;
        private readonly string[] _names;
        private readonly double[] _bindAt;
        private readonly double _createsIssuedAt;

        public readonly List<(double T, string Kind, string Name)> Issued = new();
        public readonly List<string> Lines = new();
        public bool Settled;
        public double SettledAt = double.NaN;

        private readonly Dictionary<string, double> _heldSince = new(StringComparer.Ordinal);

        public BarrierFixture(bool barrierOn, double barrierSeconds, string[] names, double[] bindAt,
                              double createsIssuedAt)
        {
            _barrierOn = barrierOn; _barrierSeconds = barrierSeconds;
            _names = names; _bindAt = bindAt; _createsIssuedAt = createsIssuedAt;
        }

        private int BoundBy(double t) { int n = 0; foreach (var b in _bindAt) if (t >= b) n++; return n; }

        /// <summary>OnOrder -> MaterializeUnit for one aggregate taskee, at <paramref name="t"/>.</summary>
        public void Materialize(double t, string unit)
        {
            int outstanding = _names.Length - BoundBy(t);
            if (_barrierOn && !Settled && outstanding > 0)
            {
                _heldSince[unit] = t;
                Lines.Add(DispatchReadiness.MaterializeHeldLine(unit, "task performer", outstanding, _names.Length));
                return;
            }
            RunMaterialize(t, unit);
        }

        private void RunMaterialize(double t, string unit)
        {
            Issued.Add((t, "DELETE", unit));            // MaterializeUnit case 3
            Issued.Add((t, "CREATE-TEMPLATE", unit));
        }

        /// <summary>The tick: the init's creates go out, names bind, and the barrier settles or
        /// expires - SweepDispatchReadiness.</summary>
        public void Run(double horizon)
        {
            bool createsIssued = false;
            for (double t = 0.0; t <= horizon; t += Tick)
            {
                if (!createsIssued && t >= _createsIssuedAt)
                {
                    createsIssued = true;
                    foreach (var n in _names) Issued.Add((t, "CREATE-INIT", n));
                }
                if (Settled) continue;
                int bound = BoundBy(t);
                bool settled = bound == _names.Length;
                bool expired = t >= _barrierSeconds;
                if (!settled && !expired) continue;
                Settled = true; SettledAt = t;
                Lines.Add(settled
                    ? DispatchReadiness.ReadyToTaskLine(bound, _names.Length, 4, t)
                    : DispatchReadiness.NotReadyToTaskLine(
                          bound, _names.Length,
                          string.Join(", ", _names.Where((n, i) => t < _bindAt[i])), t));
                foreach (var kv in _heldSince) RunMaterialize(t, kv.Key);
                if (_heldSince.Count > 0) _heldSince.Clear();
            }
        }

        /// <summary>How many order-driven commands landed between the first and the last of the
        /// init's own creates - THE OVERLAP, counted rather than argued about.</summary>
        public int OverlapCount()
        {
            var init = Issued.Where(x => x.Kind == "CREATE-INIT").ToList();
            if (init.Count == 0) return 0;
            double firstInit = init.Min(x => x.T);
            double lastInitBind = _bindAt.Max();
            return Issued.Count(x => x.Kind != "CREATE-INIT" && x.T >= firstInit && x.T < lastInitBind);
        }
    }

    /// <summary>
    /// B1 (cold-start review of 945e054): the 45-vs-60 interaction, modelled end to end because it
    /// is an ORDERING defect and nothing smaller than the ordering can show it.
    ///
    /// The chain the review found: MaterializeUnit parks the materialization and registers
    /// _compositionReady[name]; RunTaskAsync's composition await expires at
    /// Vrf:CompositionTimeoutSeconds + 30 = 45 s, logs "dispatching anyway" and FALLS THROUGH; the
    /// init shell is bound and readable so the task drives the EMPTY SHELL; and at 60 s the barrier
    /// expires, drains, and MaterializeUnit case 3 DELETES that object underneath the live task.
    ///
    /// TWO DEFENCES, and this fixture can switch them on independently, so each is shown to carry
    /// its own weight: the CAP (DispatchReadiness.BarrierSeconds) and the STRUCTURAL pair
    /// (TaskeeReadiness.MaterializationParked, and the refusal to delete an object a dispatched
    /// task is steering).
    /// </summary>
    private sealed class B1Fixture
    {
        private readonly bool _parkedCheck;      // TaskeeReadiness.MaterializationParked exists
        private readonly bool _deleteGuard;      // MaterializeUnit refuses to delete under a dispatch
        private readonly double _barrier;
        private readonly double _composeBackstop;
        private double _boundAt, _readableAt;    // the init shell; moved by a re-create

        public readonly List<string> Lines = new();
        public double DispatchedAt = double.NaN;
        public double DeletedAt = double.NaN;
        public bool RefusedDelete;
        public bool ComposeBackstopFired;
        public int HoldLines;
        public TaskeeReadiness HoldState = TaskeeReadiness.Ready;

        public B1Fixture(bool parkedCheck, bool deleteGuard, double barrier, double composeBackstop,
                         double boundAt, double readableAt)
        {
            _parkedCheck = parkedCheck; _deleteGuard = deleteGuard;
            _barrier = barrier; _composeBackstop = composeBackstop;
            _boundAt = boundAt; _readableAt = readableAt;
        }

        /// <summary>One init-planned name NEVER binds, so the barrier can only ever EXPIRE. The
        /// order arrives at <paramref name="orderAt"/> and its taskee's materialization is parked.</summary>
        public void Run(string taskName, string unitName, double orderAt, double horizon)
        {
            bool parked = true;
            bool dispatched = false;
            double gateOpened = double.NaN;
            Lines.Add(DispatchReadiness.MaterializeHeldLine(unitName, "task performer", 1, 6));
            for (double t = orderAt; t <= horizon; t += Tick)
            {
                // --- SweepDispatchReadiness: the barrier expires (it can never settle here) ------
                if (parked && t >= _barrier)
                {
                    parked = false;
                    Lines.Add(DispatchReadiness.NotReadyToTaskLine(5, 6, "1143.MechPlt", t));
                    // DrainHeldMaterializations -> MaterializeUnit -> case 3
                    if (_deleteGuard && dispatched)
                    {
                        RefusedDelete = true;
                        Lines.Add(DispatchReadiness.RefusedDeleteUnderDispatchedTask(unitName, taskName));
                    }
                    else
                    {
                        DeletedAt = t;
                        _boundAt = t + 0.30;        // ExpectRebind + the re-created object's ObjectCreated
                        _readableAt = t + 0.50;     // ... and ReleaseReflected
                    }
                }
                // --- RunTaskAsync's composition await -------------------------------------------
                if (double.IsNaN(gateOpened))
                {
                    if (!parked) gateOpened = t;                     // the materialization ran: gate released
                    else if (t >= orderAt + _composeBackstop)
                    {
                        gateOpened = t;
                        ComposeBackstopFired = true;
                        Lines.Add("Task '" + taskName + "': composition of " + unitName
                                  + " not signalled within " + _composeBackstop.ToString("F0")
                                  + "s - dispatching anyway (move may drive an incomplete unit).");
                    }
                    else continue;
                }
                // --- TryDispatchOrHold ----------------------------------------------------------
                if (dispatched) continue;
                var state = DispatchReadiness.Classify(true, true, t >= _boundAt, t >= _readableAt,
                                                       _parkedCheck && parked);
                if (DispatchReadiness.ShouldHold(state, Timeout, false))
                {
                    if (HoldLines == 0)
                    {
                        HoldLines++; HoldState = state;
                        Lines.Add(DispatchReadiness.HoldLine(taskName, unitName, state, Timeout));
                    }
                    continue;
                }
                if (state != TaskeeReadiness.Ready) continue;
                DispatchedAt = t; dispatched = true;
            }
        }

        /// <summary>THE DEFECT, in one number: was the object deleted AFTER a task was dispatched
        /// onto it?</summary>
        public bool DeletedUnderADispatchedTask
            => !double.IsNaN(DeletedAt) && !double.IsNaN(DispatchedAt) && DeletedAt > DispatchedAt;
    }

    public static int Run(bool featureEnabled = true)
    {
        int fails = 0;
        void Check(string what, bool cond)
        { Console.WriteLine((cond ? "  [PASS] " : "  [FAIL] ") + what); if (!cond) fails++; }

        double timeout = featureEnabled ? Timeout : 0.0;
        Console.WriteLine(featureEnabled
            ? "dispatch-readiness-selftest: Vrf:DispatchReadinessTimeoutSeconds=" + Timeout
              + " (the feature ON)"
            : "dispatch-readiness-selftest: --disabled - Vrf:DispatchReadinessTimeoutSeconds=0, THE "
              + "1d0fb69 BUILD. The deferral assertions MUST fail in this arm; that failure is the "
              + "defect run D5b hit.");

        // ============ 1. D5b's T_R5_TK1: the order beat the init's own creates =================
        // The init's creation is deferred to the terrain-profile reply, so at order time NO create
        // has been requested. Timeline from the run: order on the bus +0.31 s, terrain reply and
        // the six creates about +1.0 s, binding and first reflection shortly after.
        {
            var tl = new Timeline { RequestedAt = 1.00, BoundAt = 1.30, ReadableAt = 1.60 };
            var fx = new DispatchFixture(timeout, tl);
            fx.Dispatch("T_R5_TK1", "1.BdeHQ~PXY", 0.31);

            Check("D5b/DROPPED: the task DISPATCHES instead of being dropped for a unit that is "
                  + "created one second later", !double.IsNaN(fx.DispatchedAt));
            Check("D5b/DROPPED: NOTHING is reported TASKABRT and no successor is abandoned",
                  fx.Statuses.Count == 0 && fx.Abandoned.Count == 0);
            Check("D5b/DROPPED: the 1d0fb69 sentence \"has no VR-Forces object bound to its name\" "
                  + "appears NOWHERE", !fx.Lines.Any(l => l.Contains("has no VR-Forces object bound", StringComparison.Ordinal)));
            Check("D5b/DROPPED: ONE loud hold line and ONE release line - not one per tick",
                  fx.HoldLines == 1 && fx.ReleaseLines == 1);
            Check("D5b/DROPPED: the hold line names the STATE, and it is the one D5b was in "
                  + "(planned, create not yet requested)",
                  fx.HoldStartState == TaskeeReadiness.PlannedNotRequested
                  && fx.Lines.Any(l => l.StartsWith(DispatchReadiness.HoldPrefix, StringComparison.Ordinal)
                                       && l.Contains("[PLANNED-BUT-NOT-REQUESTED]", StringComparison.Ordinal)
                                       && l.Contains("task 'T_R5_TK1' held", StringComparison.Ordinal)
                                       && l.Contains("unit 1.BdeHQ~PXY", StringComparison.Ordinal)));
            Check("D5b/DROPPED: the release line reports BOTH clocks, each labelled, and they are "
                  + "not the same number",
                  fx.Lines.Any(l => l.Contains("RELEASED", StringComparison.Ordinal)
                                    && l.Contains("s of WALL clock", StringComparison.Ordinal)
                                    && l.Contains("s of TASK CLOCK (Vrf:TaskClock=sim)", StringComparison.Ordinal)
                                    && l.Contains("1.30 s of WALL", StringComparison.Ordinal)
                                    && l.Contains("3.90 s of TASK CLOCK", StringComparison.Ordinal)));
            Check("D5b/DROPPED: it dispatches when the unit becomes READABLE, not when it merely "
                  + "binds (1.60 s, not 1.30 s)",
                  !double.IsNaN(fx.DispatchedAt) && fx.DispatchedAt >= 1.60 && fx.DispatchedAt < 1.70);
        }

        // ============ 2. D5b's T_R5_PL1: bound, but the location could not be read =============
        {
            var tl = new Timeline { RequestedAt = 0.00, BoundAt = 0.20, ReadableAt = 2.50 };
            var fx = new DispatchFixture(timeout, tl);
            fx.Dispatch("T_R5_PL1", "1141.MechPlt", 0.31);

            Check("D5b/REFUSED: the task DISPATCHES once the object reflects, instead of being "
                  + "refused at dispatch", !double.IsNaN(fx.DispatchedAt));
            Check("D5b/REFUSED: the 1d0fb69 sentence \"live location could not be read\" appears NOWHERE",
                  !fx.Lines.Any(l => l.Contains("live location could not be read", StringComparison.Ordinal)));
            Check("D5b/REFUSED: the hold names BOUND-BUT-NOT-READABLE, which is a different state "
                  + "from the dropped case and says so",
                  fx.HoldStartState == TaskeeReadiness.BoundNotReadable
                  && fx.Lines.Any(l => l.Contains("[BOUND-BUT-NOT-READABLE]", StringComparison.Ordinal)));
            Check("D5b/REFUSED: no TASKABRT, no abandon", fx.Statuses.Count == 0 && fx.Abandoned.Count == 0);
        }

        // ============ 3. AN UNKNOWN TASKEE IS STILL REFUSED PROMPTLY ===========================
        // Holds in BOTH arms: a data error must not become a silent 60 s wait.
        //
        // HONEST LABEL (cold-start review of 945e054): the service refuses an unknown taskee in
        // OnOrder, BEFORE RunTaskAsync is ever started ("taskee {Uuid} is not in the C2SIM
        // INITIALIZATION - CANNOT EXECUTE TASK"), so TaskeeReadiness.Unknown is UNREACHABLE from
        // TryDispatchOrHold - plannedAtInit is always true by the time it runs. That is a stronger
        // guarantee than a rule that has to fire, not a weaker one. This arm therefore pins two
        // things and claims no more: the modelled refusal happens at the instant the task arrives
        // with no hold, and the rule would refuse to hold the state even if it could see it.
        {
            var tl = new Timeline { PlannedAtInit = false, RequestedAt = Never, BoundAt = Never, ReadableAt = Never };
            var fx = new DispatchFixture(timeout, tl);
            fx.Dispatch("T_GHOST", "NotInTheInit", 0.31);

            Check("UNKNOWN TASKEE (refused in OnOrder, before orchestration): refused AT ONCE - no "
                  + "hold line, and the refusal is recorded at the instant the task arrived",
                  fx.HoldLines == 0 && fx.Statuses.Count == 1 && fx.Abandoned.Count == 1);
            Check("UNKNOWN TASKEE: the rule refuses to hold it at any bound - the BACKSTOP behind "
                  + "the structural refusal, not the mechanism that delivers it",
                  DispatchReadiness.MustRefusePromptly(TaskeeReadiness.Unknown)
                  && !DispatchReadiness.ShouldHold(TaskeeReadiness.Unknown, 60.0, false)
                  && !DispatchReadiness.ShouldHold(TaskeeReadiness.Unknown, 86400.0, false));
        }

        // ============ 4. THE TIMEOUT PATH ======================================================
        {
            var tl = new Timeline { RequestedAt = 1.00, BoundAt = Never, ReadableAt = Never };
            var fx = new DispatchFixture(timeout, tl);
            fx.Dispatch("T_NEVER", "1222.MechPlt", 0.31);

            Check("TIMEOUT: the task IS abandoned in the end - a hold is a bound wait, not a leak",
                  double.IsNaN(fx.DispatchedAt) && fx.Abandoned.Count == 1
                  && fx.Statuses.Count(x => x.Code == S.TaskStatusCodeType.TASKABRT) == 1);
            Check("TIMEOUT: the TASKABRT NAMES THE STATE it was still in, and how long it waited",
                  fx.Statuses.Any(x => x.Why.Contains("[REQUESTED-BUT-NOT-BOUND]", StringComparison.Ordinal)
                                       && x.Why.Contains("waiting 60.0 s for the back end", StringComparison.Ordinal)
                                       && x.Why.Contains("Vrf:DispatchReadinessTimeoutSeconds", StringComparison.Ordinal)));
            Check("TIMEOUT: the successors fail FAST - NotifyAbandoned is called, exactly as every "
                  + "other dispatch dead end does", fx.Abandoned.Contains("T_NEVER"));
        }

        // ============ 5. A DEAD BACK END DOES NOT GET THE FULL BOUND ===========================
        // STP-822's signal, not a naked timer: a stopped simulator produces BOUND-BUT-NOT-READABLE
        // for as long as anyone waits.
        {
            var tl = new Timeline { RequestedAt = 0.0, BoundAt = 0.2, ReadableAt = Never };
            var fx = new DispatchFixture(timeout, tl, backendLostAt: 5.0);
            fx.Dispatch("T_R5_CO1", "114.MechCoy~PXY", 0.31);

            Check("BACK END LOST: the hold ends on the LIVENESS SIGNAL, not at the bound",
                  fx.Statuses.Count == 1
                  && fx.Statuses[0].Why.Contains("BACK END HAS BEEN DECLARED LOST", StringComparison.Ordinal));
            Check("BACK END LOST: and it says how long it waited, which is far short of the bound",
                  fx.Statuses.Any(x => x.Why.Contains("after waiting 4.7", StringComparison.Ordinal)
                                       || x.Why.Contains("after waiting 4.6", StringComparison.Ordinal)));
            Check("BACK END LOST: a task arriving while the loss stands is never held at all",
                  !DispatchReadiness.ShouldHold(TaskeeReadiness.BoundNotReadable, 60.0, backendLost: true));
        }

        // ============ 6. A TASK THAT WAS NEVER HELD IS UNCHANGED ===============================
        // Holds in BOTH arms, and it is the assertion that makes the whole change safe: the Way A
        // control run must be byte-for-byte what it is today.
        {
            var tl = new Timeline { RequestedAt = 0.0, BoundAt = 0.0, ReadableAt = 0.0 };
            var fx = new DispatchFixture(timeout, tl);
            fx.Dispatch("T_R5_TK1", "1.BdeHQ~PXY", 3.75);      // Way A's measured init -> order gap

            Check("NEVER HELD: a ready taskee dispatches AT THE SAME INSTANT the task arrived - no "
                  + "tick of latency is added",
                  !double.IsNaN(fx.DispatchedAt) && Math.Abs(fx.DispatchedAt - 3.75) < 1e-9);
            Check("NEVER HELD: NOT ONE new log line - no hold, no release, nothing",
                  fx.Lines.Count == 0 && fx.HoldLines == 0 && fx.ReleaseLines == 0);
            Check("NEVER HELD: no status, no abandon", fx.Statuses.Count == 0 && fx.Abandoned.Count == 0);
            Check("NEVER HELD: the rule says so directly - a READY taskee is never held, at any bound",
                  !DispatchReadiness.ShouldHold(TaskeeReadiness.Ready, 60.0, false)
                  && !DispatchReadiness.IsTransient(TaskeeReadiness.Ready));
        }

        // ============ 7. THE OVERLAP, AND THE READY TO TASK LINE ===============================
        // D5b's shape: six init creates land together, and the order-driven delete/re-creates went
        // out ONE SECOND LATER, while the init's own creations were still outstanding.
        {
            var names = new[] { "1222.MechPlt", "114.MechCoy~PXY", "1143.MechPlt", "1.BdeHQ~PXY",
                                "1141.MechPlt", "1142.MechPlt" };
            var bindAt = new[] { 1.20, 1.25, 1.30, 1.35, 1.40, 2.60 };   // the last one lags
            var fx = new BarrierFixture(featureEnabled, DispatchReadiness.BarrierSeconds(timeout, 15.0),
                                        names, bindAt, createsIssuedAt: 1.00);
            fx.Materialize(1.26, "114.MechCoy~PXY");     // the order arrived at 0.31 and this shell just bound
            fx.Run(10.0);

            Check("OVERLAP: NO order-driven create/delete is issued while the init's own creations "
                  + "are still outstanding", fx.OverlapCount() == 0);
            Check("OVERLAP: the held materialization is NOT lost - it runs once the init settles",
                  fx.Issued.Count(x => x.Kind == "DELETE") == 1
                  && fx.Issued.Count(x => x.Kind == "CREATE-TEMPLATE") == 1
                  && fx.Issued.Where(x => x.Kind == "DELETE").All(x => x.T >= 2.60));
            Check("OVERLAP: the hold says WHY and how much is outstanding",
                  fx.Lines.Any(l => l.Contains("MATERIALIZE 114.MechCoy~PXY", StringComparison.Ordinal)
                                    && l.Contains("HELD", StringComparison.Ordinal)
                                    && l.Contains("still outstanding", StringComparison.Ordinal)));
            Check("READY TO TASK: printed ONCE, with the count, and it tells the operator it is safe "
                  + "to push the order",
                  fx.Lines.Count(l => l.StartsWith(DispatchReadiness.ReadyToTaskPrefix, StringComparison.Ordinal)) == 1
                  && fx.Lines.Any(l => l.Contains("6 of 6 init unit(s) bound", StringComparison.Ordinal)
                                       && l.Contains("It is safe to push the order", StringComparison.Ordinal)));
            Check("READY TO TASK: the AtOrder variant is HONEST - it says what is ready (the shells) "
                  + "and what is created later (their members)",
                  fx.Lines.Any(l => l.Contains("EMPTY SHELLS", StringComparison.Ordinal)
                                    && l.Contains("members are created when an order first names them",
                                                  StringComparison.Ordinal)));
            Check("READY TO TASK lands when the LAST unit binds, not when the first does",
                  fx.Settled && fx.SettledAt >= 2.60 && fx.SettledAt < 2.70);
        }

        // ============ 8. THE BARRIER CANNOT WEDGE A RUN ========================================
        // A create that never round-trips must cost the bound and not the run.
        {
            var names = new[] { "A", "B", "C" };
            var bindAt = new[] { 1.0, 1.1, Never };
            var fx = new BarrierFixture(featureEnabled, DispatchReadiness.BarrierSeconds(timeout, 15.0),
                                        names, bindAt, createsIssuedAt: 0.5);
            fx.Materialize(1.2, "B");
            fx.Run(DispatchReadiness.BarrierSeconds(timeout, 15.0) + 5.0);

            Check("NO WEDGE: the barrier EXPIRES and the held materialization runs anyway",
                  fx.Settled && fx.Issued.Any(x => x.Kind == "DELETE" && x.Name == "B"));
            Check("NO WEDGE: and it says plainly that READY TO TASK was NOT reached, with what is "
                  + "missing and what that means for an order pushed now",
                  fx.Lines.Any(l => l.Contains("NOT REACHED", StringComparison.Ordinal)
                                    && l.Contains("only 2 of 3", StringComparison.Ordinal)
                                    && l.Contains("Missing: [C]", StringComparison.Ordinal)
                                    && l.Contains("may still be dropped", StringComparison.Ordinal)));
        }

        // ============ 8b. B1 - THE BARRIER MUST NOT OUTLIVE THE COMPOSITION BACKSTOP ===========
        // The cold-start review's blocker, modelled end to end. Shipped numbers:
        // Vrf:CompositionTimeoutSeconds = 15 -> composition backstop 45 s; the barrier wants 60 s
        // and is CAPPED to 40 s. One init-planned name never binds, so the barrier can only expire;
        // the order arrives at +1 s, which is the demo-day Way B posture.
        const double ComposeBackstop = 15.0 + 30.0;
        {
            double barrier = featureEnabled
                ? DispatchReadiness.BarrierSeconds(Timeout, 15.0)     // 40 s, capped
                : Timeout;                                            // 945e054: 60 s, uncapped
            var fx = new B1Fixture(parkedCheck: featureEnabled, deleteGuard: featureEnabled,
                                   barrier: barrier, composeBackstop: ComposeBackstop,
                                   boundAt: 0.5, readableAt: 0.8);
            fx.Run("T_R5_CO1", "114.MechCoy~PXY", orderAt: 1.0, horizon: 90.0);

            Check("B1: the object a task was dispatched onto is NEVER deleted underneath it",
                  !fx.DeletedUnderADispatchedTask);
            Check("B1: and the task is not sacrificed to get that - it still dispatches, after the "
                  + "materialization has run",
                  !double.IsNaN(fx.DispatchedAt) && !double.IsNaN(fx.DeletedAt)
                  && fx.DispatchedAt > fx.DeletedAt);
            Check("B1: the CAP is what keeps the composition backstop out of it entirely - the "
                  + "\"dispatching anyway (move may drive an incomplete unit)\" line never appears",
                  !fx.ComposeBackstopFired);
            Check("B1: the cap is derived, not a magic number - min(bound, "
                  + "Vrf:CompositionTimeoutSeconds + 30 - margin), 40 s at the shipped 15",
                  Math.Abs(DispatchReadiness.BarrierSeconds(60.0, 15.0) - 40.0) < 1e-9
                  && DispatchReadiness.BarrierSeconds(60.0, 15.0) < ComposeBackstop
                  && Math.Abs(DispatchReadiness.BarrierSeconds(10.0, 15.0) - 10.0) < 1e-9
                  && DispatchReadiness.BarrierSeconds(60.0, 0.0) >= 1.0);
        }
        {
            // DEFENCE 2 ON ITS OWN. The backstop is made SHORT so it fires while the
            // materialization is still parked - the exact fall-through the review traced. The
            // parked classification must HOLD the task; nothing may be dispatched onto the shell.
            var fx = new B1Fixture(parkedCheck: featureEnabled, deleteGuard: featureEnabled,
                                   barrier: DispatchReadiness.BarrierSeconds(Timeout, 15.0),
                                   composeBackstop: 5.0, boundAt: 0.5, readableAt: 0.8);
            fx.Run("T_R5_CO1", "114.MechCoy~PXY", orderAt: 1.0, horizon: 90.0);

            Check("B1/PARKED: when the composition backstop DOES fall through, the parked "
                  + "materialization still holds the task - a bound, readable EMPTY SHELL is not READY",
                  fx.ComposeBackstopFired && fx.HoldLines == 1
                  && fx.HoldState == TaskeeReadiness.MaterializationParked);
            Check("B1/PARKED: nothing is dispatched before the materialization has run",
                  !double.IsNaN(fx.DispatchedAt) && !double.IsNaN(fx.DeletedAt)
                  && fx.DispatchedAt > fx.DeletedAt && !fx.DeletedUnderADispatchedTask);
            Check("B1/PARKED: the hold line names the state, so the log says WHY the task waited",
                  fx.Lines.Any(l => l.Contains("[MATERIALIZATION-PARKED]", StringComparison.Ordinal)
                                    && l.Contains("EMPTY SHELL", StringComparison.Ordinal)));
        }
        {
            // DEFENCE 3 ON ITS OWN. Suppose the parked check is not there (945e054) but the delete
            // guard is: the task IS dispatched onto the shell at the fall-through, and the guard
            // must then refuse to delete it and say so.
            var fx = new B1Fixture(parkedCheck: false, deleteGuard: featureEnabled,
                                   barrier: DispatchReadiness.BarrierSeconds(Timeout, 15.0),
                                   composeBackstop: 5.0, boundAt: 0.5, readableAt: 0.8);
            fx.Run("T_R5_CO1", "114.MechCoy~PXY", orderAt: 1.0, horizon: 90.0);

            Check("B1/GUARD: with the task already dispatched, the materialization REFUSES to "
                  + "delete its object - nothing is deleted at all",
                  fx.RefusedDelete && double.IsNaN(fx.DeletedAt) && !fx.DeletedUnderADispatchedTask);
            Check("B1/GUARD: and it is LOUD and honest - it says the task keeps an EMPTY SHELL and "
                  + "that reaching this line means a defence failed",
                  fx.Lines.Any(l => l.Contains("REFUSING TO MATERIALIZE", StringComparison.Ordinal)
                                    && l.Contains("EMPTY SHELL", StringComparison.Ordinal)
                                    && l.Contains("should be unreachable", StringComparison.Ordinal)));
        }

        // ============ 9. THE RULE ITSELF =======================================================
        // These exercise DispatchReadiness directly, so they hold in BOTH arms: the class behaves
        // either way - the D5b defect is that nothing ever consulted it.
        Check("CLASSIFY: the full table, including the two observations that are only consulted when "
              + "they can be",
              DispatchReadiness.Classify(false, true, true, true) == TaskeeReadiness.Unknown
              && DispatchReadiness.Classify(true, false, false, false) == TaskeeReadiness.PlannedNotRequested
              && DispatchReadiness.Classify(true, true, false, false) == TaskeeReadiness.RequestedNotBound
              && DispatchReadiness.Classify(true, true, true, false) == TaskeeReadiness.BoundNotReadable
              && DispatchReadiness.Classify(true, true, true, true) == TaskeeReadiness.Ready
              // a bound name whose create we never recorded requesting is still bound
              && DispatchReadiness.Classify(true, false, true, true) == TaskeeReadiness.Ready);

        Check("CLASSIFY/B1: PARKED beats READY and nothing else - a not-yet-bound unit still "
              + "reports the more proximate truth about itself, and both hold the task anyway",
              DispatchReadiness.Classify(true, true, true, true, true) == TaskeeReadiness.MaterializationParked
              && DispatchReadiness.Classify(true, true, true, false, true) == TaskeeReadiness.BoundNotReadable
              && DispatchReadiness.Classify(true, true, false, false, true) == TaskeeReadiness.RequestedNotBound
              && DispatchReadiness.Classify(false, true, true, true, true) == TaskeeReadiness.Unknown
              // the 4-argument overload is the 5-argument one with parked=false
              && DispatchReadiness.Classify(true, true, true, true)
                 == DispatchReadiness.Classify(true, true, true, true, false));

        Check("TRANSIENCE: every state between planned and ready is transient, PARKED included",
              DispatchReadiness.IsTransient(TaskeeReadiness.PlannedNotRequested)
              && DispatchReadiness.IsTransient(TaskeeReadiness.RequestedNotBound)
              && DispatchReadiness.IsTransient(TaskeeReadiness.BoundNotReadable)
              && DispatchReadiness.IsTransient(TaskeeReadiness.MaterializationParked)
              && !DispatchReadiness.IsTransient(TaskeeReadiness.Unknown)
              && !DispatchReadiness.IsTransient(TaskeeReadiness.Ready));

        Check("S2: the timeout TASKABRT does not blame the data when the simulator may simply be "
              + "gone - it names STP-853 and tells the reader to check the back end",
              DispatchReadiness.TimeoutAbortReason("T", "U", TaskeeReadiness.RequestedNotBound, 60.0)
                  .Contains("CHECK THAT THE BACK END IS STILL RUNNING", StringComparison.Ordinal)
              && DispatchReadiness.TimeoutAbortReason("T", "U", TaskeeReadiness.RequestedNotBound, 60.0)
                  .Contains("STP-853", StringComparison.Ordinal)
              && DispatchReadiness.TimeoutAbortReason("T", "U", TaskeeReadiness.RequestedNotBound, 60.0)
                  .Contains("WITHOUT RESIGNING", StringComparison.Ordinal));

        Check("ZERO IS TODAY'S BEHAVIOUR: at Vrf:DispatchReadinessTimeoutSeconds = 0 (or negative) "
              + "NOTHING is ever held",
              !DispatchReadiness.ShouldHold(TaskeeReadiness.PlannedNotRequested, 0.0, false)
              && !DispatchReadiness.ShouldHold(TaskeeReadiness.BoundNotReadable, -1.0, false)
              && DispatchReadiness.ShouldHold(TaskeeReadiness.PlannedNotRequested, 0.001, false));

        Check("THE OBSERVATION STILL RUNS WITH THE FEATURE OFF: the READY TO TASK line has a window "
              + "of its own, because the operator is told to wait for it",
              Math.Abs(DispatchReadiness.BarrierSeconds(0.0, 1000.0) - DispatchReadiness.ObservationWindowSeconds) < 1e-9
              && Math.Abs(DispatchReadiness.BarrierSeconds(90.0, 1000.0) - 90.0) < 1e-9);

        Check("EVERY STATE HAS A DISTINCT NAME AND A DISTINCT DESCRIPTION - a log line that cannot "
              + "tell two states apart is the defect this fixes",
              new[] { TaskeeReadiness.Unknown, TaskeeReadiness.PlannedNotRequested,
                      TaskeeReadiness.RequestedNotBound, TaskeeReadiness.BoundNotReadable,
                      TaskeeReadiness.Ready }
                  .Select(DispatchReadiness.StateName).Distinct(StringComparer.Ordinal).Count() == 5
              && new[] { TaskeeReadiness.Unknown, TaskeeReadiness.PlannedNotRequested,
                         TaskeeReadiness.RequestedNotBound, TaskeeReadiness.BoundNotReadable,
                         TaskeeReadiness.Ready }
                  .Select(DispatchReadiness.Describe).Distinct(StringComparer.Ordinal).Count() == 5);

        Check("THE MERGED STATE SAYS SO: RequestedNotBound's own description names BOTH readings - "
              + "the create that has not round-tripped AND the object that could not be attributed",
              DispatchReadiness.Describe(TaskeeReadiness.RequestedNotBound)
                  .Contains("has not round-tripped", StringComparison.Ordinal)
              && DispatchReadiness.Describe(TaskeeReadiness.RequestedNotBound)
                  .Contains("NAME REBIND REFUSED", StringComparison.Ordinal));

        Check("AN ORDER THAT BEAT THE INITIALIZATION GETS A LINE SAYING SO, and it points at the "
              + "line a scripted push should gate on",
              DispatchReadiness.OrderBeforeReadyLine(2, 6)
                  .Contains("ORDER BEFORE READY TO TASK", StringComparison.Ordinal)
              && DispatchReadiness.OrderBeforeReadyLine(2, 6)
                  .Contains("2 of 6", StringComparison.Ordinal)
              && DispatchReadiness.OrderBeforeReadyLine(2, 6)
                  .Contains("Nothing is dropped for it", StringComparison.Ordinal));

        Console.WriteLine(fails == 0
            ? "ALL CHECKS PASSED"
            : fails + " CHECK(S) FAILED");
        if (!featureEnabled)
            Console.WriteLine("(--disabled: the failures above are the 1d0fb69 behaviour run D5b hit. "
                              + "A PASS in this arm would mean the fixture cannot see the defect.)");
        return fails == 0 ? 0 : 1;
    }
}
