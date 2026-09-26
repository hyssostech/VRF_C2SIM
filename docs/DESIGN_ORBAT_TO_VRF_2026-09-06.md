# DESIGN - representing a C2SIM ORBAT of mixed echelon depth in VR-Forces

## CLOSED - tripwires (REGROUP 2026-09-06, supervisor). Do NOT reopen without NEW live evidence.
Anchors: the vendor sample (commandLineRemoteController.cxx:717-775 build, :1520-1554 attach), the
installed UG52 (22.3 -> 18.1.1 compose; 18.1 create-flat-then-reparent + order fixes leader), and
the DETERMINISTIC runs. A doc-inferred "gap" that a passing run contradicts is NOT a gap.
C1 (CORRECTED 2026-09-06 audit) the company move failure had TWO INDEPENDENT causes superimposed -
   the earlier "root cause = hierarchy-blind create" CONFLATED them:
   C1a STRUCTURE/LOADING bug: createSubordinates=true on a unit that ALSO has declared children ->
       ~48 template phantoms + the declared platoons orphaned at 0 m. A fidelity error (wrong objects
       moved). Verified by the trace. Fixed by compose (represents the declared structure).
   C1b MOVEMENT bug, INDEPENDENT of C1a: a template higher-unit created via remote createAggregate
       does not move - it scatters - with OR without a post-create formation settle (G-A 112702Z clean
       template, auto off; 131748Z auto on; same signature). FALSIFIER of "C1a caused the move
       failure": fixing the structure alone (G-A) did NOT fix movement. Fixed by compose /
       expand-to-compose. MECHANISM CLOSED 2026-09-06 BY THE SIM'S OWN CONSOLE (PREREG_CONSOLE_
       CHANNEL sec 6.1, run T 160640Z at object notify level 4): the higher-unit's move-along
       controller starts a move-into-formation subtask first (VRF-8977: "waits until a unit's
       formation is considered valid before initiating the movement") which fans a formation-slot
       Move-To to every DIRECT member; the 3 sub-platoons report complete at t=51 but ONE
       template-created HQ-section vehicle (M998 HMMWV "AUX", Tank Headquarters Section (USA).entity
       :65/:67) never completes its slot move - an internally generated move-along drives it 12-15 km
       off in a straight line - so the formation is never valid, the gate never opens, the route move
       never starts. NOT a formation-at-creation problem (no formation/leader/organization complaint
       on the company's console; birth geometry not stacked). Compose works because its members are
       the DECLARED platoons (+ our one HQ entity), never the template's HMMWV pair. CONTROL run C
       (161714Z, composed, console 4): identical controller path, all three platoons report
       move-into-formation complete by t=51.3, "Move into formation complete", the company issues
       offset routes 114.MechCoy_R0/R1/R2 (UG52 25.2), each platoon's maneuver-along completes,
       company move-along Completed t=77.7, 3/3 TASKCMPLT. "Unresolved formation", "orientation
       race", "settle" were all wrong; initialFormation-at-create (C10) cannot help a member that
       never arrives.
   Compose fixes BOTH, which is why it is the answer and why the session reached the right FIX from
   a conflated causal story. Not a vendor bug (the vendor sample = compose); not a race per se (the
   non-determinism was C1b's symptom).
C2 THE FIX = COMPOSE per the vendor sample: empty shell (createSubordinates=false) + create members
   + addToOrganization in the object-created callback + task the parent. 3/3 DETERMINISTIC with ZERO
   formation calls (V 104042Z/105344Z/110323Z). PREREG_COMPOSE_A. UG52 22.3: ORBAT units are built
   this way (combine existing = Aggregate As).
C3 A TEMPLATE higher-unit via remote createAggregate SCATTERS - with AggregateFormation OFF (G-A
   112702Z) AND ON (131748Z), same signature. Do not retry the template path for company+.
C4 AggregateFormation='auto' (SetAggregateFormation+Reorganize) is NOT needed for compose (V ran with
   it OFF) and does NOT rescue a template. Keep OFF. The earlier "B1 harmful" / "B1 is the missing
   settle" flip-flops were confounds (phantom context; then falsified by 131748Z).
C5 COARSE LEAF (no declared children) at company+ = EXPAND-to-compose: read the mapped template's
   .entity doctrinal sub-units (ObjectTypeResolver), create each as a platoon template, compose in
   DECLARED order (HQ first) - full TO&E moved + completed (124700Z; DETERMINISTIC 3/3 more:
   PREREG_N3_EXPAND_3X X1/X2/X3 170113Z/171105Z/172132Z, identical). LEAF PLATOON = template
   (1222 4/4). Regiment+ leaf exceeds the EntityLevel ceiling -> Y-15. RECURSIVE since N4
   (2026-09-06): a synthesized sub-unit that is itself coarse is expanded too (MaxExpandDepth 3),
   so no template COMPANY is ever created under a battalion leaf (C1b). EXPAND ONLY PURE HIGHER-UNITS
   (every subordinate a unit): a MIXED template (mech platoon = IFVs + squads) stays a template,
   because synthesizing its unit subs would drop its vehicles (readiness audit 2026-09-06; 22 COA-STP1
   units; ComposeOrder.IsPureHigherUnit, --compose-selftest). NOTE the 5.2 type map sends
   BN rows to the "Tank Headquarters Section (USA)" CP PROXY (data/unit-type-map-52.json F-*-F),
   whose subs are vehicles - so for COA-STP1 the recursion does not fire; the proxy is a type-map
   fidelity ruling, not a compose question.
C6 NO post-attach reorganize is needed (addToOrganization resolves the formation - designators +
   promotion map) and NO client-side formation-validity wait is needed (VRF-8977: the app waits).
   Both were listed as "gaps" by the requirements audit and are REFUTED by V. Do not add them.
C7 LEADER IDENTITY does not gate movement (V: platoon leader; expand: HQ leader; both completed).
   Honor the C2SIM declared <Subordinate> order for FIDELITY (UG52 18.1.1) - a small fix, not a
   ruling. There is no "HQ vs maneuver" policy decision.
C8 VRF objects are addressed by their REAL VRF_UUID (from ObjectCreated), NEVER a name-as-DtUUID -
   SETTLED 2026-09-02 (PREREG_ROUTE_UUID_FIX; rwUUID.h 35-char marking blob; VrfC2SimService.cs:
   1908-1916). The name is only the in-app map KEY; all VRF calls use the real uuid (:1375). The
   audit's "G5 name-correlation gap" re-discovered this settled rule and is NOT a gap.
C9 There is NO vendor sample/source for the MSDL/ORBAT importer (headers only; importOrbat builds a
   DATA tree and does not create objects). The vendor's only worked aggregate sample = compose (C2).
C10 sendVrfObjectCreateMsg + initialFormation is a real API but NOT a compose replacement for a
    declared ORBAT (template subs get VRF-assigned uuids -> violates one-object-per-UUID, UG52 22.1);
    at most a coarse-leaf probe. DROPPED 2026-09-06: C1b's mechanism is a member that never reaches
    its slot, which an initial formation cannot fix. Do not build it.
C11 THE FIRST INSTRUMENT for any VRF behavior question is the object's OWN CONSOLE at notify level 4
    (UG52 21.9.1 p483; remote API setObjectNotifyLevel vrfRemoteController.h:1953; app setting
    Vrf:ObjectConsoleNotifyLevel; WatchVrf CON rows = the complete capture, the app callback gets
    only a subset by an unestablished rule). One run at level 4 closed C1b after a week of inference. Silence at the
    vendor default level 1 is the configured outcome, not evidence (lessons-vendor-diagnostics-first).
    The template company path is unusable in this build not because of our create call but because
    the vendor's Tank/Mech HQ-section templates carry M998 HMMWVs whose generated slot move never
    completes; compose/expand (C2/C5) sidestep it and are the design regardless (fidelity, C1a).
C12 LIFEFORMS CRASH THE 5.2d HEADLESS SIM until the DI-Guy DATA is installed (2026-09-06, three runs
    183612Z / 184832Z / 185519Z, identical callstack DtDiGuyController::determineInitialHandItem <-
    preFirstTickInit; C:\MAK\SharedData\19\latest\ModelData\Lifeforms\DIGuy ABSENT - the 2026-09-02
    minimal-data ruling skipped the 6.33 GB DI-Guy package as "GUI visuals"). Not our code, not the
    type map: the fidelity table is the first thing on any build to instantiate a DI-Guy human.
    FIX = install the package (user). No vrfSim switch disables DI-Guy (--nodiguy is
    translationFileCreate's). Interim vehicle-only proxies for lifeform rows = fidelity regression
    = user ruling (no owner words on file - RL-UNVERIFIED-DIGUY01). PREREG_COASTP1_52_RUN1 sec 6-7; DEMO_READINESS row 17.
RESEARCH AUDIT (2026-09-06, supervisor) - the ORBAT thread's workflows, graded on what survived
the NEXT live run. Sound core throughout = the vendor sample + installed UG52 + deterministic runs;
every specific MECHANISM the workflows produced was later falsified or left open:
  wf_52b70722 F-DIVERGE mechanism -> "higher-unit controller / orientation race": characterised
    C1b's SYMPTOM (non-deterministic scatter) but missed the template-vs-compose split; superseded.
  wf_5004b243 aggregate-tasking -> "B1 harmful; double-creation not the fault (rank5)": B1 verdict
    was context-confounded (later: auto neither needed nor rescuing); rank5 was right about C1b,
    wrong to dismiss C1a (the trace then found it).
  wf_e5cb1379 compose feasibility -> CORRECT; validated 3/3 (C2).
  wf_16e3e97f template-vs-composed -> "unresolved formation, settle fixes it": the settle (auto)
    did NOT rescue the template (131748Z) - mechanism INCOMPLETE; CLOSED 2026-09-06 by the sim's
    own console (C1b/C11): a formation GATE, but the blocker is a template HQ-section vehicle that
    never reaches its slot - not an "unresolved formation".
  wf_1f29a1ad vendor catalog-leaf -> "auto is the missing settle" FALSIFIED by 131748Z; "the MSDL
    importer IS the recursion (verified)" OVERSTATED (data model only, no sample; C9).
  wf_80d19fed loading mandate -> manufactured gaps G3/G4 (refuted by V) + G5 (settled 2026-09-02,
    C8); G1 real (compose default); G2 fidelity-only.
  Pattern: doc/header inference + agent synthesis, in isolation from the repo's settled notes and
  the passing runs, produced confident causes that the next run falsified. The (A)/(B) conflation
  in C1 survived INTO the first regroup until this audit. Rule (feedback-anchor-vendor-and-own-
  notes): anchor order sample > installed docs > deterministic runs > headers > agents.
DISSENT LOG: a session that disagrees writes ONE line here naming the NEW evidence; reopening is
the user's call.
C13 THE INIT IS THE ORBAT (CONTEXT), THE ORDERS DEFINE THE SIMULATED SET - user ruling 2026-09-06 (RL-20260906-02)
    ~21:50Z ("There are just 11 taskees. These are the only ones that need to be simulated. You are
    confusing the ORBAT for the whole Corps with the specific elements that are part of the COA
    proper"). Verified: COA-STP1's 42 tasks reference exactly 11 units (5 BN, 4 COY, 2 no echelon),
    every AffectedEntity is the performer itself, the other 117 units are context. Every COA-STP1
    run since July created all 128 (1,333-1,732 entities) - the oracle's behaviour, unexamined;
    the "scale" crawl was self-inflicted. Do NOT size a run by the init's unit count. Policy
    (pending the user's choice on context units): register the ORBAT, materialize a unit when an
    order first references it. Record: PREREG_COASTP1_52_RUN1 sec 12; memory coa-is-not-the-orbat.
    BUILT AND VERIFIED 2026-09-06 22:12Z (Vrf:CreationPolicy=AtOrder, demo default; review
    wf_dcad86e3 fixes in): control (AtInit) 0 of 9 tasked units beyond 1 km, max 586 m; treatment
    (AtOrder) 5 of 9, max 10.5 km, 368 objects instead of ~1,500 - PREREG_ORDER_TIME_MATERIALIZATION
    sec 3. The machine was never overwhelmed (sim pinned at its thread budget, machine ~25 %).
    Runs 2 and 3 of AtOrder: 8 of 9 beyond 1 km, ratios 1.84x / 2.82x (full ORBAT: 0.44-0.73x).
    RESIDUAL (verified by the unit console, sec 3.4): a COMPOSED unit's move-along waits on
    move-into-formation from every sub-unit (C1b's gate); at the 11-unit start-point pile one
    sub-unit can stall in the vendor script's replan loop and the unit never leaves; TEMPLATE
    units have no such gate. Which unit it hits varies by run.
C14 CO-LOCATED UNITS ARE SPREAD AT STARTUP - user ruling 2026-09-07 ~00:20Z (RL-20260907-02) ("bite my tongue and
    accept trying to space out the entities at startup, given the evidence of the triangular
    position you now cite"). Supersedes R8's 2026-07-13 ruling FOR 5.2 (that one was on 5.0.2, no
    vehicle-vehicle avoidance). Anchors: the vendor sample offsets even a platoon's tanks by 10 m
    (commandLineRemoteController.cxx:756-770); UG52 23.2.3 (avoidance does not plan through
    entities, vehicles "could become trapped"); the shipped Armor-Co formations span up to 630 m.
    Lever: the existing DeStacker rings with Vrf:DeStackSpacingMeters=700 (> the longest company
    formation). Record: PREREG_ASSEMBLY_LAYOUT_2026-09-07.
C15 A UNIT'S TASK COMPLETION IS REPORTED FROM THE UNIT'S OWN ARRIVAL EVIDENCE - user ruling (RL-20260907-01)
    2026-09-07 ~11:00Z ("report a unit's completion from the unit's own arrival evidence is
    fine"). Why: on 5.2 the vendor holds a unit's Move Along Route completion until EVERY member
    reports arrival ("Subs still moving: N"); in the spaced run 003457Z seven of nine units stood
    at the ends of their legs for eight sim-hours behind one straggling member (an M3 7 km back,
    an M577 1.3 km back), and one leader drove 33 km alone. Rule (ArrivalPolicy.cs): TASKCMPLT
    when MORE THAN ArrivalMemberFraction (0.5) of the members are within ArrivalRadiusMeters
    (500 = the shipped Armor-Co formations' half-length) of the last vertex - a MAJORITY rule,
    never leader-only (the lone-leader case must not complete). The vendor's later completion
    is swallowed once. Settings Vrf:Arrival* (VrfSettings); --arrival-selftest.
C16 PROGRESS WATCHDOG (REPORT-ONLY) - user approval 2026-09-13 ("2 as recommended"), BUILT on
    branch worktree-agent-afc0b0f7b8d49ef63, NOT deployed. Why: VR-Forces 5.2 never reports a
    unit that stops making progress while its move task runs - the base give-up test
    (vrfobjcore/singleTaskControllerComponent.h:192-205) "always returns false",
    ground-vehicle-move-to.lua has no progress test (only the stop-before-replan precondition
    :1350-1353 and MAX_REPLANS=3 :47, which count blockage replans), and the shipped
    examples/decideToGiveUpTask hands the test to the integrator as a SIM-SIDE PLUGIN we cannot
    install from an HLA client (FINDING_EARLY_STOPS_2026-09-13 sec 6a). So the interface detects
    it. POLICY (StallPolicy.cs, pure, --stall-selftest 66/66): the unit is STALLED when EVERY
    member with a readable position has net displacement < StallMoveMeters over the last
    StallWindowSeconds and at least StallMinMembersWithData members were readable. Net, not path
    length (the 1-35 signature is a ~2 m limit cycle held for 480 s). A moving LEADER with still
    followers is NOT a stall; one runaway member with five still ones is NOT a stall (that is
    C15's straggler case). DEFAULTS: Vrf:StallDetection=false (OFF - deployed behaviour
    unchanged), Vrf:StallClock=wall, StallWindowSeconds=0 (= the clock's calibrated window:
    240 WALL s or 360 SIM s), StallMoveMeters=50, StallMinSecondsSinceDispatch=60,
    StallCheckSeconds=5, StallMinMembersWithData=1.
    WHICH CLOCK THE WINDOW RUNS ON - built 2026-09-13 on branch feat/sim-clock; this SUPERSEDES
    the earlier "neither VrfFacade.h nor VrfBridge.cpp exports a sim-clock reader ... the reader
    that would close the gap EXISTS one layer down - DtClock::simTime()" paragraph, which was
    wrong in both halves. The reader was built, and it is NOT DtClock::simTime(): that LOCAL
    VR-Link clock is the one VrfFacade.cpp:587-589 drives from elapsedRealTime() under
    #if !VRF_API_52 - wall time on 5.0.2, unset on 5.2. The usable clock is the BACK END's,
    DtVrfRemoteController::simTime(), declared at vrfcontrol/vrfRemoteController.h:355-356 on 5.2d
    (:351-352 on 5.0.2): "Returns the simulation time of the specified back end. If no back end
    specified, returns the first back ends simulation time". The vendor sample prints it as "Sim
    time from sim engine status" and prints the local DtClock separately as "Local sim time"
    (examples/remoteControl/commandLineRemoteController.cxx:1247-1256). It is fed by back-end
    STATUS messages (DtIfStatus::simTime, vrfmsgs/ifStatus.h:85-87, cached per back end in
    DtBackend::mySimTime, vrfutil/backend.h:273-274) and runs fast under
    fixed-frame-run-to-complete. Exported as VrfFacade::SimTimeSeconds -> VrfBridge.SimTimeSeconds(),
    -1.0 for "no reading" (no controller, or no back end discovered), on which the watchdog falls
    back to wall seconds instead of going blind.
    CALIBRATION - THE WINDOW BELONGS TO THE CLOCK (supersedes the "RE-CALIBRATION OWED" note; the
    re-calibration is DONE, docs/experiments/RECAL_STALL_SIMSECONDS_2026-09-13.md). Vrf:StallClock
    still DEFAULTS TO WALL - that is the mode measured live so far - and StallWindowSeconds now
    defaults to 0, meaning "the window calibrated for the clock actually in use": 240 WALL seconds
    or 360 SIM seconds. An explicit value is used as given, and a mid-run fallback to the wall
    clock falls back to the wall window with it. The two numbers are NOT a conversion of each
    other: P11's sim/wall ratio swings 1.10x-1.99x WITHIN that one run, so 240 wall s covers
    264-478 sim s depending on load - which is why a single ratio was never going to give the sim
    default. HOW 360 WAS DERIVED: the POS rows were re-stamped onto the sim axis by
    piecewise-linear interpolation over the (wall, sim) pairs the object console's own line
    prefixes carry (not one least-squares slope, which hides the load variation; hold-out p95
    0.07/0.07/0.34 sim s, zero monotonicity violations), the replay REPRODUCED EVERY DOCUMENTED
    WALL NUMBER first - the 120 s quartet and the 160 s triple to the second, the 160/170 boundary,
    the 240 s fires, the 74/78 m true-negative minima, the 35-70 m clean band - and the sweep over
    100-500 sim s then put the pooled false-alarm boundary at 50 m at 250 sim s (P11 250, G3 200,
    G5 none, set by P11's 4-27 and G3's 856/HHC). 250 x 1.41, the wall default's own margin
    convention, is 353 -> 360 on the sweep grid. At 360 sim s the clean threshold band is 35-75 m
    (50 m mid-band, as at 240 wall s), the true-negative separation is 76-103 m against firing
    maxima of 34-49 m, and ALL FIVE true positives fire EARLIER IN WALL TIME than the 240 wall s
    window does (G5 135 s vs 385 s, P11 375 vs 419 and 1,813 vs 1,824, G3 396 vs 431 and 709 vs
    742) - the freezes happen while the sim is running fastest, which is exactly what a sim-second
    window is for. LIMITS, from that record: one order, one terrain, three runs, two true positives
    and seven true negatives, and the sim boundary rests on two crawling units where the wall one
    rested on four; a scenario whose crawl floor is slower than ~0.23 m/s of sim time would push
    the boundary up and 360 would not hold. Vrf:StallClock is also VALIDATED - anything that is not
    exactly "sim" or "wall", trimmed and case-insensitive, logs one line and runs on WALL. Row 19's
    original "N = 120 SIM-seconds, must fire by sim ~500" is a criterion only under
    StallClock=sim; at the shipped default the row reads in wall seconds.
    COLD-START REVIEW OF THE SIM-CLOCK COMMIT (1616614 -> feat/sim-clock, --stall-selftest 52/52
    [that was the count AT 1805ee3, 2026-09-13; it is 62/62 after the pass-3 fixes and 66/66 after
    the pass-4 fixes - see the two paragraphs below]).
    The clock choice and every vendor citation held; the RING built around it did not, and four
    defects were fixed in the same branch. (1) and (2) the sliding window pruned only at the
    FRONT, and that rule fires only when the two OLDEST stamps are equal - true only when the
    pause or the rollback hits an empty or degenerate ring, which is the shape the self-test used
    and NOT the shape a run produces. A pause beginning AFTER the ring filled grew it without
    bound (249 entries after 400 s of run plus 1,000 wall s paused, each a member-position
    dictionary), and a snapshot rollback (DtVrfRemoteController::rollbackToSnapshot) left
    PRE-rollback samples that the re-opened window then compared against POST-rollback positions.
    StallPolicy.Admit now prunes at BOTH ends: a sample that does not advance the clock REPLACES
    its predecessor [REFUTED - pass 2 F1 showed that replace MANUFACTURES a TASKABRT on a ring of
    one entry, and pass 3 reversed it: such a sample is now DISCARDED, and the replace survives
    ONLY on the rollback branch. This clause describes 1805ee3 and becomes live again only if that
    fix is rolled back - see the pass-2 paragraph below], and a backwards step drops every entry
    from the abandoned timeline and re-arms the watch. (3) the grace anchor had moved from the
    in-flight record's own DispatchedUtc to the watch's first sample, and on the DEFAULT aggregate
    move path (MarkDispatched at VrfC2SimService.cs:2146, dest = the route's last vertex) and the
    R11
    plan-move path (:2049) a NEW task is recorded while ClearStallState still waits for the
    route-created callback - so the previous task's ring could produce a TASKABRT stamped with the
    NEW task uuid, unboundedly if that callback never arrives (the silent-freeze mode this
    watchdog exists for). MarkDispatched now drops the unit's stall SAMPLES whenever the new task
    carries a destination (the one-report flag still clears at ClearStallState, the conservative
    direction), and the old wall grace is restored as an additional AND [NARROWED - pass 2 F8:
    ANDing that 60 s WALL floor onto the SIM clock let the floor, not the calibrated window, set
    the detection time above ratio ~6x (at 60x: wall 60 s / sim 3,600 s, ten times the window). It
    is ANDed on the WALL path ONLY; MinRingDepth covers the two-sample case it guarded].
    (4) with the window on the sim clock and the cadence on wall time, a high ratio let the gate
    open on TWO position reads over a span 25 % wider than the configured window; the gate now
    also requires
    StallPolicy.MinRingDepth (4) samples inside the window and StallMinSecondsSinceDispatch of
    WALL time since dispatch [pass 2 narrowed that floor to the WALL path - see below], and the
    CADENCE FOLLOWS THE CLOCK (StallPolicy.NextCheckSeconds: sample often enough, down to a 1 s
    floor, that one step advances the sim clock by at most window / MinRingDepth) so the depth
    floor stays reachable instead of becoming a silent OFF switch. KNOWN LIMIT: above sim/wall
    ratio window / ((MinRingDepth - 1) x 1 s) - 120x at the 360 s sim window, NOT the 90x this
    paragraph first claimed - the 1 s cadence floor binds and the depth floor cannot be cleared, so
    the watchdog cannot judge; pass 2 found this was also not logged, and it now is. Also fixed:
    the clock mode needs THREE consecutive readings before it
    switches (an unsteady reader was clearing every ring on every flip and logging a line each
    time) and the mode line is rate-limited; and a sim clock that has not advanced for 60 wall
    seconds while a move task is in flight now WARNS once and suspends judging - a back end that
    misses its status timeout is DEACTIVATED, not removed (vrfBackendListener.h:161-163 against
    :154-155), so backends().count() stays > 0 and the reader would otherwise return its last
    cached value for the rest of the run and be read as a pause.
    COLD-START REVIEW PASS 2 OF 1805ee3, FIXES IN PASS 3 (same branch, --stall-selftest 62/62 at
    08146a2; 66/66 after pass 4, below).
    Pass 2's verdict was MERGE WITH FIXES: the WALL path - the shipped default - reproduced
    51d78a5's decision sequence exactly, Decide was byte-identical, and the pass-1 fixes did what
    they claimed; but the SIM path, the point of the branch, carried three defects a preregistered
    StallClock=sim run would have hit, plus one that reached the wall path. All are fixed here and
    each carries a self-test that FAILS against the logic it replaced (proved by reverting,
    rebuilding and running). F1: a sample that did not advance the clock REPLACED the newest ring
    entry's payload under its OLD stamp, and a ring of one entry is at both ends at once - so the
    window was measured from an old stamp against positions read up to a flat-clock interval later,
    the false-positive direction. A unit crawling at 0.23 m/s of SIM time (the RECAL doc's own
    tightest true negative) was reported STALLED after a 40-wall-second flat reading at 6.21x, on
    35.7 m over a nominal 360 s window whose true displacement was 82.8 m; the 60 s stale hold
    lands 1-20 s too late to cover it. Such a sample is now DISCARDED (the rollback branch still
    replaces, where the stored payload really is stale). F2: `simSeconds < 0.0` is FALSE for NaN,
    so a NaN reading became the clock while StallPolicy.UsingSimClock - the next predicate, same
    value, same method - called it no reading; Admit then appended the NaN and BOTH disjuncts of
    ShouldDropOldest went false, so the front prune stopped for the rest of the run (1,195 entries,
    window 8,955 sim s against a configured 360). The service now uses that one predicate and
    StallPolicy.SelectClock instead of an inlined copy, and Admit refuses a non-finite clock. F3:
    _stallSimClockLast was a HIGH-WATER mark, so after rollbackToSnapshot the clock was genuinely
    advancing below it, the stale detector fired 60 wall s later and suspended judging for EVERY
    unit until it climbed back - 420 wall s for a 475 sim s rollback - under the text "the scenario
    is PAUSED, or the back end has stopped answering". A backwards step is now a CHANGE
    (StallPolicy.ClassifyClockStep) with its own rate-limited line. F4, the one that reached the
    WALL path: nothing bounded Vrf:StallCheckSeconds against the window, so above window /
    (MinRingDepth - 1) - 80 s at the shipped 240 s wall window - the ring never filled and the
    watchdog judged NOTHING, silently, where 51d78a5 fired at 240 s (measured at StallCheckSeconds
    120 and 240). The cadence is now CLAMPED to that ceiling with one startup line - clamped, not
    refused, because the watchdog is report-only and a mis-set knob must not stop a run - and in
    sim mode a rate-limited line says so when the cadence sits at its 1 s floor with the ring below
    MinRingDepth for a whole window. F6: the one-report guard tested ContainsKey(NAME) while the
    map is documented as name -> task UUID, so a unit that stalled once and was then RE-TASKED went
    unwatched for the rest of its life in the in-flight set; it now compares the uuid. F8: the 60 s
    wall floor was ANDed on BOTH clocks, and 360 sim s is ~58 wall s at 6.21x, so above ratio ~6x
    the floor - not the calibrated window - set the detection time (at 60x: wall 60 s / sim 3,600 s,
    ten times the window), negating the "fires EARLIER in wall time" property the 360 s default was
    chosen for. It is applied on the WALL path only; MinRingDepth, which 51d78a5 did not have,
    covers the two-sample case it incidentally guarded. F7/F9 were comment repairs: two clauses
    still said wall was "the only CALIBRATED mode - see RE-CALIBRATION OWED", which this same
    branch refuted - wall is the default because it is the mode MEASURED LIVE so far - and
    StallWindowSeconds 0 (and negative) now means the calibrated default where it used to mean a
    ONE-SECOND window. WALL PATH UNCHANGED at shipped settings: the seven synthetic feeds pass 2
    used (frozen; 2 m/s; crawl 0.20 and 0.21 m/s either side of the 50 m/240 s line; move-600-then-
    freeze; a +/-2 m limit cycle; creep-then-stop) fire at identical times against 51d78a5. Off the
    defaults the clamp moves two fire times EARLIER, to the window edge (StallCheckSeconds 81:
    243 -> 240; 100: 300 -> 240), where 1805ee3 was dormant. UNTESTED, still: the finding-3 change
    itself - MarkDispatched dropping _stallSamples on a re-task with a destination - has no test at
    the service level, because --stall-selftest cannot reach the service; it is modelled offline
    only, and so is F6. THE TWO LIVE UNKNOWNS BELOW ARE UNCHANGED by any of this, and F1's trigger
    is the second of them: if the reader extrapolates, a paused reading is a sawtooth rather than
    the flat line that produced the false TASKABRT.
    COLD-START REVIEW PASS 3 OF 08146a2, FIXES IN PASS 4 (same branch, --stall-selftest 66/66).
    Pass 3's verdict was MERGE WITH FIXES: every pass-2 finding is closed or correctly narrowed,
    the WALL path is still decision-identical to 51d78a5 across TEN synthetic feeds (the seven of
    pass 2 plus a 300 s reader outage, a 60 m step at the window edge, and exactly 50.0 m per
    240 s), and Decide is still byte-identical to 51d78a5. Two defects, one of them NEW and made
    reachable by the pass-3 fixes themselves. P1 (MAJOR, introduced by pass 3): the F2 fix taught
    Admit to REFUSE a non-finite clock, but the caller's gate stayed `simSeconds >= 0.0`, which is
    TRUE for +Infinity - so +Inf became clockNow, Admit returned without appending, and either the
    ring was left EMPTY for `var oldest = ring[0]` to index (IndexOutOfRangeException on the
    vrf-tick thread, which has no handler and terminates the process) or, on a ring with entries,
    both window terms went trivially true against +Inf and the gate opened on whatever depth
    existed - a TASKABRT on the SAME TICK, on 6.9 / 13.8 / 20.7 m at wall 20 / 40 / 60, on the
    RECAL doc's own tightest true negative. FIXED: UsingSimClock requires double.IsFinite,
    SelectClock routes through it, and the caller tests ring.Count before indexing. P4: the new
    dormancy line was armed on ONE CAUSE - the cadence at its 1 s floor with every ring below
    MinRingDepth - which is the high-ratio cause only. MODE THRASH is the measured miss: a reader
    out for 3 or more CONSECUTIVE checks flips the clock mode for real, every flip drops every
    ring and swaps the window, and at 1.5x the cadence never leaves 5 s - 3 consecutive misses in
    every 10 at 1.5x, frozen unit, 3,000 wall s gave 0 verdicts, 0 judgeable checks, 40 mode lines
    and NOTHING saying no unit was being watched. FIXED: the line is armed on the OBSERVABLE
    condition (nothing satisfied JudgeReady for DormancyWindows = 2 whole windows while samples
    were being taken), measured on the watchdog's own monotone un-judged axis so it survives the
    thrash it reports; the high-ratio explanation stays as a hint when the cadence is at its floor.
    Both fixes carry a self-test that FAILS against the logic it replaces (4 FAILED on the revert,
    ALL CHECKS PASSED restored). Also in pass 4: P7 - the rollback and dormancy lines now carry the
    suppressed-count suffix the mode line already had, and the operator-facing text quotes the
    MEASURED 150x-200x boundary rather than the conservative analytic 120x; P8 - the four checks
    that are invariants or new-API tables rather than discriminators are now labelled
    (INVARIANT ...) / (NEW API ...), and the honest claim is that each FINDING, not each check,
    carries a discriminating test; P11 - the 0c pre-flight prints BOTH clocks' numbers when
    Vrf:StallClock=sim (sim 360 s / cadence C, wall fallback 240 s / cadence C') and says that
    which pair is in effect is not known until the first check; P2/P3 - the last three code texts
    and three doc sentences asserting refuted claims were repaired in place.
    RESIDUALS FROM PASS 3, recorded and NOT fixed. (P5) The one-report guard compares the task
    uuid, so a re-task with a NEW uuid is watched again - but a re-task carrying the SAME uuid
    leaves _stallReported matching while MarkDispatched has dropped the samples, and the unit is
    skipped for the rest of the run with nothing in the log; narrow, because four of the six
    dispatch sites call ClearStallState in the same block and only :2049 and :2146 defer it to the
    route-created callback. (P5, inherited from 51d78a5) ClearStallState also clears
    _stallReported, so if that route-created callback lands MORE THAN ONE WINDOW after
    MarkDispatched the same task uuid can be reported TWICE, against the "one report per
    unit-task, ever" promise. (P6) F8's removal of the wall floor on the sim clock is entirely
    outside the calibrated ratio range (RECAL: P11 1.10x-1.99x, G5 6.21x): the first verdict now
    lands about window/ratio WALL seconds after a watch opens - 240 s at 1.5x, 60 s at 6.21x, 8 s
    at 60x, 7 s at 120x - so at high ratios a TASKABRT can rest on four position reads one second
    apart, with MinRingDepth carrying the whole load against an unrefreshed HLA reflection. The
    number is now in the 0c line; the behaviour stands. (P7, partly) The dormancy line is still
    rate-limited at 60 wall s, so a two-hour run stuck at 200x emits ~120 warnings - now each
    carrying the count it suppressed. (P9) EVERY backwards step is diagnosed as
    rollbackToSnapshot, so a JITTERING sim reading produces a stream of false rollback lines and
    can SUPPRESS A TRUE DETECTION: on a reading oscillating about a rising 1.5x trend with a
    frozen unit over 6,000 wall s, amplitudes 5 s and 200 s never reported the unit (92-100 false
    rollback warnings) while 30 / 100 / 400 / 1000 s did fire. That amplitude sweep is
    NON-MONOTONIC and the reviewer could not fully explain it - traced to the phase relationship
    between the square wave, the 5 s cadence and the front prune, but not characterised; recorded
    here as an UNEXPLAINED observation, not a footnote. Falsifier: evidence that simTime() is
    monotone between genuine rollbacks, which is part of the instrumented run below. (P10) The
    sim-blackout early return happens BEFORE the dead-unit prune, so during a blackout
    _stallSamples and _stallReported retain entries for units that have completed; bounded by unit
    count, but the "the buffer must not grow across a whole run" comment does not cover it.
    STILL UNTESTED at the service level, unchanged: MarkDispatched dropping _stallSamples on a
    re-task with a destination, and F6 - --stall-selftest cannot reach the service, so both are
    modelled offline only.
    TWO LIVE UNKNOWNS, both settled by ONE instrumented run (log SimTimeSeconds() every check for
    60 s running and 60 s paused, and compare one reading against a CON row's own sim prefix at the
    same wall instant): (a) whether DtVrfRemoteController::simTime() reports THE SAME CLOCK the
    object console prints as its own sim prefix - that prefix is the only sim stamp in the capture
    and therefore the axis the 360 was calibrated on, so if the reader returned a different
    quantity (exercise time rather than scenario time, say) the sim window would need re-deriving;
    (b) whether DtBackend::simTime() EXTRAPOLATES between back-end status messages - the member
    layout (mySimTimeToRealTimeRatio, myLastSimTimeUpdated "wall-clock time elapsed since last sim
    time was updated", vrfutil/backend.h:410-419) suggests it may, which would make a paused
    reading a sawtooth and weaken the headline "a paused scenario can no longer trip the watchdog".
    Lesser residual: the back-end STATUS PERIOD, i.e. the reader's resolution - if it is coarser
    than StallCheckSeconds, consecutive samples share a stamp (the back-prune bounds the ring, but
    the measured displacement is still biased downward by up to one sampling interval).
    THE SAMPLER IS SHARED WITH C15 (TryReadMemberPositions) so the two policies can never judge
    different samples. It keys member positions by VRF uuid, and the member count it is judged
    against therefore counts DISTINCT non-empty uuids: VrfFacade::collectMembers recurses to
    depth 3 WITHOUT de-duplicating, so a member published under two sub-aggregates would
    otherwise weigh twice against arrival while contributing one position - strictly harder than
    the pre-C16 sampler, which weighted the duplicate consistently on both sides. Guarded by a
    decision table in --arrival-selftest (main / un-deduplicated / fixed over all 32 near-far
    arrangements of a duplicated member). Two DELIBERATE divergences from the pre-C16 arithmetic,
    both in the direction of "one vehicle is one vote": a duplicated ABSENT member no longer
    counts twice against arrival, and a member whose uuid is EMPTY (never observed) is left out
    of the count entirely rather than counted as an unreadable member.
    WHAT IT NEVER DOES: no VR-Forces command, no re-task, no change to the in-flight record, the
    sequencer or the pending-engage map. It sends ONE C2SIM TaskStatus with TASKABRT per
    unit-task through the SAME ReportBuilder path as TASKCMPLT (BuildTaskStatusReport now takes
    the code; --report-selftest has the TASKABRT round-trip) and logs "STALL: unit ... TASKABRT
    reported". State is cleared wherever the C15 arrival swallow is cleared (a new task) AND at
    the top of SynthesizeUnitCompletion, which EVERY completion path reaches under the UNIT name;
    OnVrfTaskCompleted's own clear keys on e.UnitMarking, which under R10 fan-out is a MEMBER
    name and clears nothing. The per-unit window and the one-report flag are both pruned for
    units that have left the in-flight set, so neither map grows across a run.
    ABORT-THEN-COMPLETE IS INTENDED (supervisor position 2026-09-13; the user may override). A unit
    that has already reported TASKABRT and then moves and ARRIVES still sends TASKCMPLT for the
    SAME task uuid: the abort was the interface's judgement at the time, the arrival is evidence,
    and STP sees the truthful sequence. TASKABRT NEVER suppresses a later TASKCMPLT. The converse
    DOES hold - a TASKCMPLT suppresses any later TASKABRT for that task - because completing pops
    the in-flight record and the watchdog only ever looks at in-flight move tasks (and the C15
    arrival gate skips the unit besides).
    CORRECTION 2026-09-25: this is a SUPERVISOR reading (2026-09-13), not an owner ruling - RL-20260914-01 covers only the TASKABRT code; see its scope note. The current TEMPORARY completion position is RL-20260921-09.
    SILENCE IS NOT STALLING: a unit whose members stop being REFLECTED has no data, not a stall -
    Decide() returns "not stalled" whenever fewer than StallMinMembersWithData members had a
    readable position at both ends of the window, so a reflection gap is never reported as a unit
    standing still. Deliberate for a report-only watchdog: a false TASKABRT costs STP more than a
    missed one.
    THE SEQUENCER IS NOT TOLD: a TASKABRT does not call _sequencer.CompleteTask, so the
    successors of an aborted task stay gated until their own predecessor timeout. After an abort
    STP's view (this task is aborted, move on) and the interface's view (the task is still in
    flight, its successors still wait) DIVERGE - by design for report-only, and noted for the
    user as the first thing to revisit if the watchdog is ever allowed to act.
    REPLAY VALIDATION (tools/analysis/stall_replay.py, the same rule over each run's
    watchvrf-trace.csv POS rows; C15's arrival rule is replayed too, so a fire after arrival is
    suppressed as it is in the product). THE EXACT INVOCATIONS, all re-run 2026-09-13 after the
    review fixes; runs/ is read-only and was not modified:
      G5  stall_replay.py runs/20260913T185936Z_run --order data/COA-STP1_Order.xml
            --expect-fire 1-35          (the order actually pushed was a scratchpad copy holding
                                         T1 alone, whose vertices are identical to T1 in the repo
                                         order; only 1-35 is tasked, so only 1-35 can fire)
      G3  stall_replay.py runs/20260913T174516Z_run --order data/COA-STP1_Order.xml
            --expect-fire 1-35,1-6
      P11 stall_replay.py runs/20260907T150643Z_run --order data/COA-STP1_Order.xml
            --expect-fire 1-35,1-6
    | run | ratio | fires: wall s, max moved in the window, distance still to go | true neg | false alarms |
    |---|---|---|---|---|
    | G5 20260913T185936Z | 6.21x | 1-35 @ 385 s, 49.6 m, 24,304 m short | n/a (1 unit tasked) | 0 |
    | G3 20260913T174516Z | 1.60x | 1-35 @ 431 s, 43.9 m, 24,131 m short; 1-6 @ 742 s, 49.5 m, 30,098 m short | 7 of 9 | 0 |
    | P11 20260907T150643Z | 1.46x | 1-35 @ 419 s, 43.4 m, 24,337 m short; 1-6 @ 1824 s, 48.6 m, 30,300 m short | 7 of 9 | 0 |
    The "distance still to go" is the replay's end-dist column (added on review): the nearest
    member's distance to the task destination at the END of the trace. It is the evidence behind
    each label - a unit that never fires and ends kilometres short is a real early stop the rule
    missed, whatever --expect-fire says.
    THE WINDOW IS 240 s BECAUSE 120 s FALSE-ALARMS ON CRAWLERS: at 120 s the rule also fires on
    P11's 4-27 (@1519), 40 (@3475), 856/HHC (@2520) and C/1-35 (@3699), each of which then covers
    203-1,191 m more - they creep at 0.4-0.5 m/s for thousands of seconds and dip below 50 m/120 s
    only transiently. AT THE 120 s WINDOW a bigger THRESHOLD cannot separate them (there the
    crawlers' per-window minima, 41-50 m, overlap the frozen units', 22-49 m); only persistence
    can. Measured boundary: window 160 still leaves three false alarms in P11 (40 @4150, 856/HHC
    @4070, C/1-35 @4119) and window 170 leaves none in either run - the last false alarm
    disappears between 160 s and 170 s, so the shipped 240 s keeps a 1.41x margin (240/170).
    AT THE 240 s WINDOW the threshold separation the 120 s window lacked is there: the tightest
    true negative is 74 m per window (P11's 40, 856/HHC and C/1-35; 4-27 at 78 m) against frozen
    per-window maxima of 43-50 m. MEASURED BAND over all three runs: move thresholds from 35 m to
    70 m are clean AND still catch both frozen units. Not lower - at 30 m G3's 1-6 is MISSED (its
    per-window minimum is 34 m) - and not 74 m or above, where the P11 crawlers start firing. The
    shipped 50 m sits in the middle of that band.
    G3's 856/HHC - the one row whose LABEL the trace does not decide. It stops 3,448 m short of
    its destination: after wall 1,456 s no member is ever again more than 50 m from where it ends,
    and the POS rows end at 1,591 s - 135 s later. The 120 s window fires on it at 1,512 s, 79 s
    before the trace ends, and because 856/HHC is not in --expect-fire the tool prints FALSE ALARM
    for it there. The 240 s window cannot fire at all: 135 s of stillness cannot fill a 240 s
    window (its tightest per-window max is 60 m at window 160 and 65 m at 170, and over the LAST
    240 s of the trace three of its four members still covered 660-670 m). WHETHER IT IS A TRUE
    POSITIVE IS UNDECIDABLE FROM THIS TRACE: 135 s of stillness is exactly the transient dip the
    P11 crawlers show, and the SAME unit in P11 is a crawler that ends 307 m short still moving
    (per-window minimum 74 m). The 2026-09-13 review called it a real early stop and asked for it
    to be relabelled a true positive; the measurements above support neither label, so it is
    recorded UNDECIDED for the user to rule on and --expect-fire is deliberately left unchanged so
    the window settings stay comparable. It is certainly NOT the 1-35 / 1-6 signature, which holds
    under 50 m for the whole remaining trace and ends 24-30 km short of the destination.
NEXT (the only real work): N1 DONE 2026-09-06 (PREREG_N1_COMPOSE_DEFAULT: default ON verified by
run D 162958Z 3/3 with no env; flag-off run L 164022Z reproduces the legacy 38-phantom / 2-of-3
signature - the switch is the regression control). N2 DONE 2026-09-06 (PREREG_N2_DECLARED_ORDER:
ComposeOrder.ByDeclared + --compose-selftest 5/5; run E 165015Z attaches [1141, 1142, 1143] in the
authored order, 3/3). N3 DONE 2026-09-06 (PREREG_N3_EXPAND_3X: 3/3 identical; flag-off covered by
L). NEXT = COA-STP1 at scale on 5.2 (readiness: REBASELINE_52_INSTRUMENTS sec 6 - census reads 5.2
sub-routes from the console; position reports need slice R1 = the oracle's periodic poll; runner
-ClientId C2SIM; NO -StopWhenComplete on 5.2 until R1 lands). THEN (user ruling 2026-09-06)
DEMO-READY = the interface as a STANDALONE deployment without the harness (docs/DEMO_READINESS_
2026-09-06.md, 15-item gap list) BEFORE the real STP task vocabulary. Everything else below is
history.

2026-09-06. Generalizes the compose recipe (PREREG_COMPOSE_A, validated V) from one company to an
arbitrary C2SIM ORBAT where units are declared to DIFFERENT depths (some to platoon/platform, some
only to battalion). Plan/design - not yet implemented beyond what COMPOSE_A already ships. All
echelon/catalog facts below were read from the installed 5.2d catalog this session.

## 0. What C2SIM gives us (the ORBAT), and the two vendor primitives
The init is a TREE: each unit has a Superior (parent) uuid (InitParser captures it), an EchelonCode
(BDE/BN/COY/PLT/SECT/...), a SIDC (echelon char at index 11: D=platoon, E=company, F=battalion,
G=regiment, H=brigade), a type/function, and a position. The tree's DEPTH VARIES per branch: one
branch may bottom out at platoon, another at battalion, another at a single platform.

Two VR-Forces primitives, both vendor-verified (commandLineRemoteController.cxx:717-775,1520-1554;
UG52 18.1, 13.3; createAggregate createSubordinates flag cgf.h:617-621; addToOrganization
vrfRemoteController.h:1334-1339):
- TEMPLATE unit  = createAggregate(createSubordinates=TRUE). VR-Forces instantiates the unit's
  TYPICAL doctrinal formation for that echelon/type (a Tank Company template -> HQ + 3 platoons +
  vehicles). Use for a unit the ORBAT does NOT decompose (a LEAF).
- COMPOSED unit  = createAggregate(createSubordinates=FALSE) empty shell + addToOrganization(child,
  parent) per declared child. Use for a unit the ORBAT DOES decompose (an INTERNAL node); the shell
  needs NO template at its echelon.
- ENTITY (platform) = createEntity. Use for an ORBAT leaf that is a single platform.

## G-A RESULT 2026-09-06 (run 20260906T112702Z) - "leaf -> template" is FALSE for company+
G-A tested a CLEAN leaf template COMPANY (114.MechCoy with its declared platoons REMOVED ->
createSubordinates=true, NO double-creation, no B1). It SCATTERED like the phantom: company cluster
23 movers / 0 still, 19 SOUTH-staged + 3 RUNAWAYS (16.7 / 13.6 / 4.7 km), only 4 north, company did
NOT complete (only 1.BdeHQ + 1222 completed); 37 objects (clean, no double-creation). This
FALSIFIES two beliefs: (1) the phantom failure was the double-creation - NO, a clean template
scatters too; (2) leaf->template works above platoon - NO. The real distinction is COMPOSE vs
TEMPLATE: composed company = 3/3 deterministic PASS; template company = 7/8 runs scatter (phantom
4/4 + B1-off 2/3 + this clean 1/1), same signature. Only TEMPLATE PLATOONS work (1222 4/4; the
platoon controller, not the higher-unit one). MECHANISM (VERIFIED 2026-09-06, wf_16e3e97f, refuter
SURVIVES, all cites opened): task-time is byte-identical (both send only CreateRoute+MoveAlongRoute
on the company uuid) - the fork is entirely CREATE-TIME. The company higher-unit sysdef's ONLY mover
is aggregate-move-along-controller (no adapter, no maneuver-along, NO isUnitMovementExhausted
fail-safe, no take_formation; ground-higherUnit-disaggregated-movement.sysdef:177-203;
disaggregatedMoveAlongController.h:56,422,449) and builds member routes purely from DtFormationState
offsets. createSubordinates=true batch-instantiates 1 HQ + 3 platoons all at offset (0,0,0) with an
UNRESOLVED formation state -> lookupFollowOffset returns (0,0,0)/default offsets (formationState.h:
52-53,61-66,150-152) -> members staged backward + flung km away, no fail-safe -> scatter (birth
dispersal 738 m vs composed 79 m). COMPOSE fixes it: addToOrganization makes the org controller
assign distinct leader-first designators + the command net -> VALID promotion map
(aggregateOrganizationController.h:96,103,108) -> valid offsets. Template PLATOONS work only because
the platoon sysdef adds the adapter + maneuver-along controller (leader + fail-safe;
ground-disaggregated-movement.sysdef:176-221). ASSUMED (one inferred link): the template's formation
state is invalid specifically AT TASK TIME (batch-instantiation path not in the headers) - but every
candidate sub-trigger routes through the same fail-safe-less controller, so expand-to-compose
sidesteps it by construction.
=> REVISED RULE (sec 1): a LEAF at COMPANY+ echelon must NOT be a template; EXPAND it into its
doctrinal sub-units (platoon templates, which work) and COMPOSE (the proven path). Leaf platoon/
platform unchanged. See sec 1 (revised) + sec 5.

## 1. The mapping rule (recursive, bottom-up) - REVISED after G-A
Classify each ORBAT node by whether ANOTHER surviving unit names it as Superior, AND by echelon:
- LEAF at PLATOON or below (platoon, section, platform): represent by the VR-Forces TEMPLATE for
  its echelon+type (a platform -> entity). VERIFIED works (1222 platoon 4/4). The template's
  members run on the PLATOON controller, which is reliable.
- LEAF at COMPANY or above: do NOT use a template - G-A proved a template higher-unit scatters
  (the higher-unit controller). Instead EXPAND the unit into its DOCTRINAL sub-units (the sub-units
  the catalog template would contain - e.g. a Tank Company = HQ + 3 tank platoons) and COMPOSE it
  from those (each sub-unit a platoon template, which works) via the proven empty-shell +
  addToOrganization path. Source the doctrinal composition from the catalog (.entity file / a
  composition table), NOT from the broken createSubordinates=true template.
- INTERNAL (has declared children): create an EMPTY shell (no template) and COMPOSE it from its
  declared children via addToOrganization. Recurse: children are LEAF or INTERNAL. Create bottom-up
  (leaves first), attach up the tree, attach in the ORBAT's declared order (UG52 18.1.1: order
  fixes leader/echelon).
So COMPOSE is the reliable higher-unit path in BOTH the internal case (children from the ORBAT) and
the coarse-leaf case (children from doctrine); TEMPLATE is used ONLY at platoon-and-below.
COMPOSE_A validated INTERNAL=company / LEAF=platoon (V: 3/3 platoons attached, company moved+
completed, deterministic). The coarse-leaf expand-to-compose is HIGH-confidence (it is exactly what
V did, sourcing platoons from doctrine instead of the ORBAT) but NOT yet run - validation owed.
Current code (ApplyHierarchyComposition) does internal-compose + leaf-template; the coarse-leaf
EXPAND step is NEW and not yet implemented.

## 2. The template ceiling (VERIFIED from the installed catalog) - the real constraint
EntityLevel (our scenario SMS) .entity templates exist at: Platoon (37), Section (23), Company
(15), Squadron (8), Battalion (7 - e.g. "Mechanized Battalion (US Army M2)", "Infantry Battalion").
NO Regiment/Brigade in EntityLevel. AggregateTacticalLevel holds Battalion Tactical Group /
Regiment templates. Y-15 (settled): SMS is PER-SCENARIO, no mixing (UG52 13.7).
Consequences for a LEAF (a template is required only for leaves; internal nodes are composed and
need no template):
- Leaf at PLATOON..BATTALION echelon -> native EntityLevel template exists. Representable.
- Leaf at REGIMENT+ -> NO EntityLevel template -> exceeds the ceiling. Options (decision, not the
  recipe): (a) run that ORBAT in AggregateTacticalLevel (Y-15 SMS choice; coarser for everything);
  (b) COMPOSE the regiment from doctrinal battalions - but those are NOT in the ORBAT, so this
  INVENTS structure (violates feedback-fidelity unless a doctrinal composition table is authored);
  (c) FAIL LOUDLY (fidelity default: never silently mis-echelon). Recommend (c) now, (a) as the
  scale path.
- INTERNAL node at ANY echelon (incl. regiment/brigade WITH declared children) -> composed from
  its children, NO template needed -> the ceiling does NOT bite it. So a brigade that decomposes to
  battalions that decompose to companies is representable in EntityLevel by compose all the way down
  to leaves that are within the ceiling.

## 3. VALIDATION GATES (honest unknowns - a live run each, before relying on the general case)
G-A (PIVOTAL): does a LEAF template at COMPANY/BATTALION echelon TASK and MOVE correctly? This is
  the higher-unit move-along controller path. VERIFIED works: leaf PLATOON template (1222.MechPlt
  4/4) and COMPOSED company (V). NOT verified: a clean leaf COMPANY/BATTALION template (createSub-
  ordinates=true, no declared children, no double-creation) tasked. The ONLY higher-unit template
  we ever tasked was the phantom company - and it scattered, but that was CONFOUNDED by double-
  creation (template company + separately-created declared platoons stacked at one point).
  Hypothesis (not asserted): a clean template company/battalion moves like the composed company
  (structurally both are aggregate-of-platoon-aggregates; the composed run already moved template
  platoons under the higher-unit controller). Test: init one leaf "Mechanized Company"/"Mechanized
  Battalion" (no children), task a move; pass = its members move + it completes, like V.
G-B: multi-level compose (depth 3: battalion -> companies -> platoons). ApplyHierarchyComposition
  handles a node that is both parent and child (TryAdvanceComposition advances both), but only
  depth-2 is validated. Test: a 3-echelon ORBAT; pass = leaves move, every echelon composes, top
  completes.
G-C: TYPE-MAP coverage at higher echelons. data/unit-type-map-52.json currently maps echelons
  ~D/E/F to platoon/company/HQ templates; a BATTALION-echelon leaf has no row -> it would FAIL type
  mapping (AuthoredPending/Failed) and not be created. Extend the fidelity table with battalion (and
  squadron/troop) echelon rows -> the EntityLevel battalion templates (feedback-fidelity: map to the
  correct catalog template, do not proxy down an echelon).
G-D: HQ elements + leader order. Many real units carry an HQ section (type map shows "Tank
  Headquarters Section"). Decide: is the HQ a DECLARED child in the ORBAT (compose it in, first =
  leader) or part of the template? Attach order must put the intended leader/HQ first (UG52 18.1.1).

## 4. Edge cases to handle (mostly already guarded in ApplyHierarchyComposition)
- A child that fails type-mapping is dropped -> its parent composes from the survivors + warns
  (already). - A parent whose children ALL fail -> no composition, parent falls back to template
  (already). - A parent that is not an aggregate but has children -> warn, children standalone
  (already). - Mixed force/domain members in one composed unit: UG52 18.1.1 allows heterogeneous
  members but tasks do not affect non-VR-Forces entities; no same-force constraint found - revisit
  if a future ORBAT mixes. - Partial/duplicate ORBAT delivery: existing duplicate-init guard.

## 5. Recommended sequencing (REVISED after G-A)
1. G-A: DONE 2026-09-06 - FAILED (a clean template company scatters). Conclusion: coarse leaves
   need EXPAND-to-compose, not template. Everything below hangs on this pivot.
2. EXPAND-to-compose design + doctrinal-composition SOURCE - how does a coarse leaf (company/
   battalion) get its sub-unit list? Options (docs-first, decide before coding): (a) parse the
   catalog .entity file for that unit type (it lists the template's sub-units - authoritative
   doctrine); (b) a small authored composition table (unit-type -> sub-unit templates), keyed like
   unit-type-map-52.json; (c) create the template once, GetAggregateMembers (already recursive) to
   READ its composition, delete it, re-create members as composable aggregates (uses the broken
   template only to read - wasteful/risky). Lean (a) or (b).
3. VALIDATE expand-to-compose: one leaf company (expanded -> 3 platoon templates -> composed) move.
   Pass = same as V (platoons move north, company completes). High confidence (it is V with
   doctrine-sourced platoons) but a live gate is owed.
4. G-B: depth-3 ORBAT compose (battalion of companies of platoons).
5. G-C: type-map echelon coverage so company/battalion units resolve to a type (to be expanded).
6. Ceiling/Y-15: regiment+ leaf policy.
Internal-compose CODE is done + validated (COMPOSE_A); the EXPAND step (items 2-3) is NEW.

## EXPAND VALIDATION RESULT 2026-09-06 (run 20260906T124700Z) - PASS (recipe-faithful, full TO&E)
Coarse leaf 114.MechCoy (childless, GA_LeafCompany init), Vrf:ComposeHierarchy=true: live catalog
loaded (C:\MAK\vrforces5.2d, EntityLevel, 1912 templates); 114.MechCoy resolved to "Tank Company
(USA)"; EXPANDED to 4 doctrinal sub-units [HQ1, TANK2, TANK3, TANK4] in the .entity's DECLARED order
(HQ first, full TO&E per the user ruling), 4/4 attached via AddToOrganization; company TASKCMPLT
(139aa71b), DROP=0; MechCoy cluster 23 movers / 0 still, ALL NORTH, 0 runaway, maxdisp 1040 m (route
legs ~550-1100 m); 59 objects. The HQ-as-leader hazard did NOT materialize (HQ attached first, unit
moved clean) - which is why the earlier maneuver-first reordering was removed as an unneeded
invention. Members are catalog templates + the vendor addToOrganization recipe; the only new code is
the compose orchestration + reusing the existing ObjectTypeResolver to read the .entity composition.
1 run; the compose path itself is 3/3 deterministic - confirm 3/3 if this ships.
OPEN (user 2026-09-06): is a SIMPLER vendor-DIRECT catalog-leaf bind possible (createSubordinates=
true + a settle / reorganizeAggregate / formation-validity wait) that makes a template higher-unit
taskable, replacing the synthesize-and-compose expand? Research wf_1f29a1ad running. Expand stands
as the verified fallback.

## RESEARCH wf_1f29a1ad (refuter SURVIVES) - reframes G-A; VENDOR-DIRECT bind is now on live test
Verified from docs/headers (independent of any run):
- The vendor has TWO first-class create paths, both taskable at any echelon: COMPOSE (empty shell +
  addToOrganization; the only worked sample, commandLineRemoteController.cxx:717-775,1520-1554) and
  PRECONFIGURED CATALOG TYPE (createSubordinates=true; the GUI/ORBAT/MSDL DEFAULT, entityCreationData
  .h:33, cgf.h:617-621). createSubordinates=true is NOT inherently broken.
- The remote createAggregate(createSubordinates) overload has NO initialFormation param
  (vrfRemoteController.h:1295-1306); the GUI create data DOES (entityCreationData.h:33). So a REMOTE
  template create skips the initial geometry the GUI supplies - the app must settle it via
  AggregateFormation='auto' (SetAggregateFormation+ReorganizeAggregate). THIS WAS OFF IN BOTH G-A
  RUNS. Composed passed WITHOUT it (addToOrganization resolves geometry); template scattered WITHOUT
  it. So G-A did NOT test the vendor-canonical direct path, and my "B1/auto is harmful" was
  CONFOUNDED (that verdict came from the double-creation phantom + top-only application; auto
  completed a battalion 3/3 on GOLDEN terrain 2026-07-14).
- The user's rule IS the vendor MSDL/ORBAT importer recursion (explicit subordinates -> create
  members with parent; leaf -> catalog type at any echelon; "empty" unit types for explicit-children
  nodes, UG52 7.2.2). Move Along works at any echelon, forwarded to the lead subordinate
  (moveAlongTasks.h:32-37, Migration 2.4.1). CORRECTION: the 5.2 formation-validity wait is VRF-8977,
  NOT VRF-8968 (our docs say 8968 - fix on the next pass).
- Also noted (unused): sendVrfObjectCreateMsg (vrfRemoteController.h:1577-1595) carries createSubObjects
  AND initialFormation - a more-GUI-like create the port does not use.
DECISIVE LIVE TEST (running, bvw2w4q44): G-A leaf-company, createSubordinates=true (direct catalog
bind, ComposeHierarchy OFF) + AggregateFormation='auto' (the settle that was off). PASS => the
coarse leaf binds to the catalog DIRECTLY (+ auto) and expand is unnecessary (simpler, more vendor-
native, matches the user rule). Caveat (refuter): Mojave has an empty-leader-path failure mode where
only SubordinateFanOut has worked, so auto-alone may not suffice -> then + fan-out; if still not,
compose stays. NOTE: my prior "B1 harmful / do not use auto" record is UNDER REVIEW pending this test.

### DIRECT-BIND + auto RESULT 2026-09-06 (run 20260906T131748Z) - FAILS; direct catalog bind is out
G-A leaf-company, createSubordinates=true (direct template, ComposeHierarchy OFF) + AggregateFormation
='auto'. The settle FIRED (app log: "114.MechCoy - set formation 'column' (from its own list) +
reorganize"). It STILL SCATTERED: 19 south + 3 runaways (16.6/15.1/5.0 km), 4 north, 114.MechCoy NOT
completed (TASKCMPLT=2); 37 objects - essentially identical to G-A (auto off). So the settle made NO
difference. CONCLUSIONS (now un-confounded, both arms tested):
- The DIRECT catalog-template bind does NOT work at company level on 5.2/Mojave, WITH OR WITHOUT the
  vendor settle (auto). The research's "auto is the missing step" hypothesis is FALSIFIED here.
- COMPOSE is the working path (V 3/3; expand full-TO&E 124700) and is the vendor's own only worked
  aggregate recipe. "resolve to the catalog at the leaf" must mean COMPOSE from the catalog's
  declared composition (= expand-to-compose), NOT template-instantiate it.
- B1/auto reconciled: it does NOT rescue a template company (tested); it is neither the fix nor
  needed for the working (compose) path (V + expand passed with auto OFF). So "do not rely on auto
  for a higher-unit" stands - now for a tested reason, not the earlier confounded one.
- MECHANISM still not fully closed (carried, not asserted): SetFormation+Reorganize resolves
  designators but the template's per-member follow-offsets stay degenerate on this build - reason
  unverified.
UNTESTED (declined - not simpler, not "direct"): template + auto + SubordinateFanOut (the flagged
Mojave mitigation); even a pass would be more moving parts than compose and would task members, not
the unit. => DECISION: ship EXPAND-TO-COMPOSE (validated) for coarse leaves; template higher-units
are not a usable direct bind on this build.

### INSTALLED 5.2 USERS GUIDE - READ DIRECTLY 2026-09-06 (had been skipping these for this Q)
The vendor's documented ORBAT/unit build, from the installed VRFUsersGuide.pdf (page = PDF page):
- Ch 22 "Creating and Using an Order of Battle" (p489-495): an ORBAT is a hierarchy of sim objects
  each with a UNIQUE UUID (unlike the palette, which is a template for many instances); you build it
  (add members, aggregate into units), INSTANTIATE members into a scenario, and import/export it.
- 22.3 "Creating Units in the Order of Battle Panel" (p493): units in an ORBAT are built "the SAME as
  18.1.1 Creating a Unit by Combining Existing Simulation Objects" -> i.e. COMPOSE ("Aggregate As"),
  NOT preconfigured templates. So the vendor's documented ORBAT-unit build IS COMPOSE - exactly the
  user rule + our expand-to-compose.
- 18.1 (p438): two create methods - combine existing (compose, "Aggregate As") vs preconfigured unit
  from the palette (template). "You cannot create units that are subordinates of an existing unit...
  once you create a unit, you can subordinate it" = create-flat-then-reparent (= addToOrganization).
- 18.1.1 / 13.3.1 (p438/p365): subordinate ORDER at create fixes the LEADER (= designator 1),
  unchangeable after; aggregate assigns leader designator 1 then successive. Matches our declared-
  order attach (HQ-first worked in the expand run).
- AddingContent 13.11 (p327) "Importing a Hierarchy File" is about ELEMENT DEFINITIONS (.hier/.leaf
  visual/definition catalog), NOT instantiating an ORBAT of units - not the create-and-task path.
NET: the installed docs GROUND compose as the vendor-canonical ORBAT-unit build. Expand-to-compose
is that method with the leaf's composition read from the catalog. The direct preconfigured-template
is the GUI's other option; its REMOTE equivalent needs initialFormation (sendVrfObjectCreateMsg,
vrfRemoteController.h:1577-1600) which our createAggregate overload lacks - the reason the remote
direct bind scattered. DECISION UNCHANGED: expand-to-compose (validated + documented). sendVrfObject
CreateMsg + initialFormation is the documented remote direct-bind alternative, untested, native add.

### CORRECTION 2026-09-06 - "the MSDL importer IS the recursion (VERIFIED)" was OVERSTATED
There is NO vendor SAMPLE for the MSDL/ORBAT importer (checked: no examples/ MSDL|ORBAT project - only
scenarioMetrics; every "source" grep hit is a compiled .dll; no msdl/orbat .cxx/.cpp anywhere; the
symbols live only in headers). The importer is a built-in GUI/core feature and its importOrbat()
"simply creates the structures. Does not do anything with the imported data"
(vrfMsdlOrbatImporterExporter.h:57) - it builds an ORBAT DATA tree (DtOrbatManagerNode: parentUUID +
orbatOrderedList + childAdded/Removed, orbatManagerNode.h), it does NOT create or task entities.
createFromMSDLItem is protected with no shipped .cpp. So the importer is verified only as a DATA
STRUCTURE (parent/ordered-children tree - the SHAPE of the user rule), NOT as a runnable create-and-
task recipe, and the entity-create step is the GUI's (not shipped; it carries initialFormation the
remote API lacks - the same gap that scatters the remote template). NET: the ONLY runnable vendor
sample for a taskable aggregate is commandLineRemoteController = COMPOSE. The importer gives no
shortcut. This firms the decision (expand-to-compose) rather than changing it.

## 6. Adversarial review
Strongest competing view: "leaf=template just works at every echelon, no gate needed." Not safe -
the one higher-echelon template we tasked failed (phantom), and although that is best explained by
double-creation (a clean template has none), it is not VERIFIED clean; G-A is cheap and decisive, so
gate it. Corrected assumption: I first believed EntityLevel topped out at COMPANY (so battalion
needed AggregateTacticalLevel) - FALSIFIED by the catalog (EntityLevel has 7 battalion templates);
the true ceiling is REGIMENT. VERIFIED: the two primitives + their APIs; the EntityLevel echelon
inventory; that internal nodes need no template; that ApplyHierarchyComposition already encodes
leaf-vs-internal. ASSUMED (gated): G-A/G-B behaviours; that the type map can be extended cleanly to
battalion templates; that VRF template internals (HQ placement) do not break the leader-order rule.
