# CORRECTIONS LOG

Provenance of claims that were once stated and later found wrong. Only the two ENTRY docs -
RESUME_PROMPT.md and HANDOFF - were rewritten clean (2026-07-21) and state current truth
with no retraction history. RUNBOOK and VRF_GROUNDWORK_PLAN are large accreted files that
STILL carry in-line retraction / READ-FIRST blocks; treat any sentence in them as current
only if it is not inside a superseded fence. This file is where the history lives; consult
it only to answer "was X ever believed, and why is it not believed now". ASCII only.

Each entry: the claim, why it was wrong, and the evidence that settled it.

## Movement of 1222.MechPlt

- CLAIMED (through 2026-07-19 early): "moves ~174 m of a ~1155 m route, then STOPS - a
  reproducible defect." WRONG. The unit was still moving, not decelerating, when telemetry
  ended. ~174 m is a REAL MEASURED displacement over RPT's ~124 s coverage, not a stopping
  point and not the observation-window length (that would predict ~203 m at 1.4 m/s).
  Evidence: RPT final-leg speeds 1.45/1.49/1.48 m/s across three runs, no deceleration; a
  POS sample at t=159.9 (run 161438Z) 4.6 m from the concurrent RPT fix.
- CLAIMED (2026-07-20): "still ACCELERATING when observation ended." OVER-READ. Three RPT
  fixes = two legs, and leg 1 begins at task issue so it contains spin-up from rest; two
  legs cannot distinguish acceleration from reaching cruise ~1.48 m/s. Current wording:
  "still moving, not slowing."
- CLAIMED (2026-07-20, round 6): "RPT stops reporting ~23 s before the POS trace does
  (t=157.1 vs 180.3) - a new open gap." FABRICATED. No gap: RPT's period is ~62 s, the next
  fix was due at t~219 s, and the interface resigned at ~182 s. The figure also compared one
  unit's last fix (157.1) against the global POS collapse (180.3) - different quantities.
  Deleted round 7.

## The t=180 readable collapse

- CLAIMED (briefly, round 4 cold-read): the 53->2 readable collapse at t~180 is an oracle
  fault. WRONG. It is the interface RESIGNING at teardown; the two survivors are the two
  baseline uuids present before creation. Evidence: collapse tracks VrfC2SimApp process exit;
  trace keeps sampling to t~679.
- CLAIMED: "tracks the app exit to within 0.3 s." Own numbers give deltas up to 0.5 s, and
  the app-exit column was not reproducible without a fitted per-run offset. Current evidence
  is the survivor-uuid identity, which needs no clock alignment.

## Baseline objects "positionless"

- CLAIMED (RUNBOOK 0.5.7, for days): "the TropicTortoise baseline objects are POSITIONLESS -
  that is simply how they reflect." WRONG as a statement about reflected values, and
  corrected wrongly FOUR times before the counted census stuck. Verified census:
  d39a55ad (GlblTerrDmg) 0 samples, never reflects; f864e51f (GlobalEnv) 1388 samples, forms
  NaN,-90,NaN and 0.0,-90,6.4e72, never its authored 9e-6; cde66adc (Page-In Area) 1390
  samples, four forms incl. altitudes 1.02e15 and 6.4e72. BOTH readable objects are
  cast-corrupted; neither's true position has ever been read.

## VrfFacade RTTI / the aggregate cast

- CLAIMED (RUNBOOK sec 7, since 2026-07-10): "dynamic_cast<DtReflectedAggregate*> fails due
  to RTTI across the MAK DLL boundary." FALSE. Under DtHLA the class deliberately derives from
  DtReflectedObject (reflectedExtAggregate.h:15-19), so a null cast is correct. The blind
  static_cast worked on aggregates only by accidental vtable-slot alignment, and is UB on
  control objects - the cause of the 0xC0000005 crash.
- CLAIMED (2026-07-19, briefly): "the static_cast is removed; aggregates resolve via the
  typed list; resolveStateRep." That native change was REVERTED (commit 5d14eda) because it
  broke object creation. resolveStateRep has zero hits in tracked source; the blind cast is
  STILL at VrfFacade.cpp:735.

## Tooling

- CLAIMED: StopIface acts with no arguments (it drove a live server RUNNING->UNINITIALIZED
  during a usage probe). FIXED 2026-07-19: requires <restUrl> <stompUrl> --yes, no defaults.
- CLAIMED: the runner/RESUME support -ConsoleLogDir / --console-log-dir. The flag went out
  with revert 5d14eda; WatchVrf rejects it with exit 2, killing the run. Disarmed in the
  runner, removed from docs.
- CLAIMED: RAW / BCON / CONARM trace record types and LogObjectConsoleToFile /
  SetObjectNotifyLevel exist. All went out with revert 5d14eda; WatchVrf emits POS/CON/TSK/RPT.
- CLAIMED (RUNBOOK sec 7): recover a stale federate by "reloading the scenario in the GUI."
  FALSE - recovery is automated (tools/ResetVrf, sec 8). No GUI step on any scored path.
- CLAIMED: the Session Status modal "fires on EVERY clean teardown." INTERMITTENT - named in
  zero of six stopvrf logs (the search cannot see it); four teardowns completed cleanly. The
  nested-dialog fix is UNVERIFIED (never exercised by a real occurrence).

## Run accounting

- CLAIMED variously: "four separate runs", "5 of 6 fallback", "4 of 4 fallback", "one
  teardown failed", "the four-run table" (five rows). Verified: THREE fully unattended runs
  (161438Z, 202349Z, 222134Z); TWO teardowns failed (144109Z, 193252Z); the back-end
  graceful fallback fired on ALL FIVE runs that had the feature; the bridge validation table
  has FIVE rows.

## Model-set default behaviour

- CLAIMED (2026-07-19): "RULED OUT - taskRules/ and scriptedObjectMovement/ are empty."
  WRONG layer. Empty only in C2simEx; C2simEx.sms includes EntityLevel.sms, whose taskRules/
  holds default-task-rules.tsk + doctrines.dct and whose scriptedObjectMovement/ holds 19
  files. None opened. NOT ruled out.

## Birth altitude / the "underground birth" freeze hypothesis

- CONTEXT: the probe branch probe/create-altitude-above-ground (oracle commit b96688b) and the
  port's "Create-altitude mode=Live" raise unit birth from 1000 to 10000 MSL so VRF's
  create-time ground clamp drops each unit onto the terrain surface, curing the historical
  buried-birth.
- ESTABLISHED (2026-07-21, re-derived from run artifacts by the supervisor): this fix was
  ALREADY ACTIVE in the three Jul-19 scored runs, and the frozen units froze anyway. So
  "underground birth" is FALSIFIED as the CURRENT freeze cause.
  Evidence:
  * runs/20260719T161438Z_run/vrfc2simapp.log lines 22-32: all six units incl. 114.MechCoy and
    1.BdeHQ "created at safe MSL 10000 m (original create alt 1000 m); parity post-create
    SetAltitude SKIPPED (born-above-terrain + VRF ground clamp places it on the surface)".
  * watchvrf-trace.csv (161438Z): units clamp to three distinct terrain-following surface
    altitudes - 1222.MechPlt 1040.6 m, 114.MechCoy 1116.7 m, 1.BdeHQ 1131.4 m; zero samples at
    10000, zero negative; altitude tracks sub-meter lon offsets = a real ground clamp. Units are
    on the SIM surface, not buried and not airborne.
  * DISCRIMINATOR TEST: all three taskees got identical treatment (vrfc2simapp.log 48-58:
    CreateRoute 3 pts -> Route created -> MoveAlongRoute issued) at the same birth altitude, yet
    1222.MechPlt moved while 114.MechCoy and 1.BdeHQ froze bit-exact. Same altitude, divergent
    outcome => birth altitude is not the discriminator.
  * Independent corroboration: the 2026-07-16 alt1 experiment (COA-STP1, C++ oracle, apps
    3452/3453/3454) at 10000 MSL birth clamped all units to terrain (~1137 m) and 124/128 still
    froze; only units with executing routes moved; tank 1-1/2/1_AD got MoveAlongRoute + a
    TaskComplete yet stayed bit-exact frozen (its route logged a garbled ~100 MSL start).
- STILL OPEN (the real primary defect): what makes a tasked, surface-clamped ground unit
  execute vs ignore its MoveAlongRoute. Leading un-examined surfaces: ROUTE/WAYPOINT altitude
  (not birth altitude) and the never-opened model-set defaults (see "Model-set default
  behaviour"). The movement model is documented to re-clamp MOVING ground vehicles to the
  surface, which competes with the route-altitude reading and is not yet reconciled.
- RESIDUALS: (a) sim terrain sits ~75 m below real USGS 3DEP terrain at these coords
  (terrain-DB fidelity; does not affect in-sim freeze); (b) the port's primary XML deserializer
  fails on both init and order (Schema102 "error in XML document (1,2)"), a fallback rescues it
  - a separate latent defect; (c) the exact config knob file was not confirmed (a reader cited
  VrfSettings.cs CreateAltitudeSafeMslMeters=10000.0; grep did not find it at that path) - the
  runtime log confirms the behaviour regardless.

## Process

- The single-auditor repair loop (rounds 1-7) did not converge: like-for-like orchestrated
  audits found 26 then 29 defects, because each repair pass added correction layers that were
  themselves defect-prone (mis-scoped fences, corrections after the text they retract,
  headlines outliving bodies, one fabricated finding). The entry points were rewritten clean
  2026-07-21 to break that loop. LESSON: state the current truth in the live doc; keep
  provenance HERE; do not stack retractions in a document a fresh reader must act on.

## The entity freeze / "nav data ruled out" (resolved 2026-09-01)

- CLAIMED (2026-07-14, nav-data falsification; repeated in UNIT_MOVEMENT_RESEARCH sec 6
  and MOJAVE_ROOTCAUSE): "nav data is NOT the Mojave cause - Sweden marches with none."
  TRUE for MISSING nav data, but the same 2026-07-14 session GENERATED a 120,002-tile
  NavArea over the Mojave AO and left it in SharedData/16/latest/TerrainData/navData/.
  From 2026-07-15 every run loaded it, and ground units whose movement consulted it
  waited forever ("Waiting for nav data to load", Info-level, invisible at
  objectConsoleNotifyLevel 1). The freeze the falsification left "unexplained" was
  CREATED BY the falsification session's own instrument. Evidence: P1 RUN 2
  (20260901T191004Z: 12,100 waiting lines from 1.BdeHQ, bit-static) and P1c
  (20260901T194029Z: artifact moved aside -> the entity DROVE ITS ROUTE AND COMPLETED).
  The artifact now sits in navData/_disabled_20260901/ (restorable).
- CLAIMED (HANDOFF_2026-07-23): "no entity move has ever been proven through the
  interface." FALSE - 1.BdeHQ reached its Mojave route end on 2026-07-13 (pre-artifact)
  and at Sweden repeatedly; corrected in the handoff's read-first note 2026-09-01.
- CLAIMED (2026-09-01 morning, RESEARCH_MECHANISMS sec 4b): "the entity freeze
  correlates with GroundWaypointAltitudeMode=Live" (H-ENT-1). FALSIFIED the same day by
  P1 RUN 2 (frozen under Fixed100, all gates met). The Live default and the NavArea
  artifact landed on the same day (2026-07-14/15) - a textbook confound.

## The region hypothesis / "the leader path plan is EMPTY at Mojave" (retracted 2026-09-02)

- CLAIMED (2026-07-13, R9 region swap; evidence docs/experiments/R9_region_swap_2026-07-13.txt:32-35;
  written up in UNIT_MOVEMENT_RESEARCH sec 4c): "at the COA-STP1 Mojave region VR-Forces cannot plan
  unit movement paths - the back end logs `moveAlong() - empty route -- not sending move along to
  subordinate` three times per aggregate and creates ZERO member Offset Route objects, against 45 in
  the same-day Sweden control; so the REGION / streamed terrain content is the aggregate blocker, and
  it is NOT an interface defect." RETRACTED. The region is not the cause.
- FALSIFIED 2026-07-22 by docs/experiments/PREREG_FIXTURE_REGION_VS_STRUCTURE_2026-07-22.md. An
  AUTHORED, structurally complete Tank Platoon loaded from a .scnx at the SAME Mojave AO drove its
  route: ":203-205 Disaggregated-move MECHANISM engaged: reflected 9 -> 13 at onset = 4 new
  offset-route/control transients - the SAME buildOffsetRoute path R9 reported EMPTY (0 offset routes)
  for our REMOTE-CREATED units at this same AO"; ":209-211 INTERPRETATION - the region hypothesis
  (Branch A) is FALSIFIED. Mojave terrain does NOT fundamentally break disaggregated movement for an
  authored, structurally-complete Tank Platoon. R9's '0 offset routes at Mojave' is therefore NOT a
  property of the terrain; it is a property of what our interface CREATES/TASKS there." The
  below-terrain-waypoint control ran in the same experiment and killed the other environmental
  candidate: ":287-289 CONSEQUENCE: WAYPOINT ALTITUDE (below-terrain clamp-up) is FALSIFIED as a cause
  of the R9 freeze. Both environmental hypotheses for the empty-offset-route freeze are now DEAD:
  REGION (Mojave terrain) and WAYPOINT ALTITUDE."
- WHY IT WAS WRONG (mechanism, not just the counter-example): R9 was observed with the TYPE-MAPPING
  layer still in place. Remote-created aggregates emitted 11.1.225.1.1.3.0, which has no Kind-11 leaf,
  fell back to Ground_Aggregate, and therefore had NO MEMBER SET for buildOffsetRoute to build routes
  for. The empty offset route was a property of the object this interface created, not of the ground
  under it. Type mapping was fixed 2026-07-22 (UnitTranslator RealTemplates, now the default); three
  further layers were peeled 2026-09-01/02 (this project's own generated NavArea artifact, the
  cosmetic HQ formation-name warning, the route-vertex altitude frame). See
  HANDOFF_2026-09-01_R9_COMPLETE.md "THE FOUR-LAYER BLOCKER STACK". Every July-era falsification is
  LAYER-RELATIVE (lesson L9) and must be re-adjudicated on the clean state before its fence is trusted.
- THE RETRACTION DID NOT PROPAGATE for six weeks. As of 2026-09-02 the region story was still stated
  as CURRENT in six live docs; each now carries an inline
  "[RETRACTED 2026-07-22 - the REGION cause is FALSIFIED; see docs/CORRECTIONS_LOG.md.]" pointer at
  the cited passage:
    docs/COA_GPT_FEEDBACK.md Item 4 (a full RETRACTION NOTICE at the head of the item; INTERNAL - the
      user decides whether coa-gpt is ever told)
    docs/SCENARIO_SETUP_GUIDE.md :35, :90, :130
    docs/PORT.md :10, :625, :757-765
    docs/UNIT_MOVEMENT_RESEARCH.md :297, :562, :608
    docs/START_HERE.md :191, :346-370, :638
    docs/NEXT_SESSION_GUIDANCE.md :60-61
  (line numbers as they stood before the tags were inserted). The bodies are NOT rewritten - they are
  history and must read as history.
- WHAT IS **NOT** SETTLED. Retracting the region CAUSE does not assert the SYMPTOM is gone. Whether
  COA-STP1's own remote-created aggregates now build member offset routes at Mojave under the clean
  state is an OPEN question, pre-registered as prediction P3 of
  docs/experiments/PREREG_COASTP1_RUNG1_BOUNDED_2026-09-02.md. Nothing here licenses a claim that
  aggregates march at Mojave until that run is scored.

## COA-STP1 order/init arithmetic (corrected 2026-09-02, all re-verified from the XML)

- CLAIMED (OPUS_EXECUTION_PLAN.md:724 and :739, PLAN_DERISK_NOTES.md:81,
  UNIT_MOVEMENT_RESEARCH.md:437): the order has "32 temporal deps". WRONG: 31. data/COA-STP1_Order.xml
  contains exactly 42 ManeuverWarfareTask and 31 ActionTemporalRelationship elements, all with
  ActionTemporalAssociationCode STREND. 42 tasks - 11 chain heads = 31 dependent tasks, each with
  exactly one predecessor: 10 performers carry chains of 4, one (510/40) carries a chain of 2.
  Heads: T1, T5, T9, T13, T15, T19, T23, T27, T31, T35, T39. Those four sites are corrected; the two
  remaining "32" mentions (PORT.md:602, START_HERE.md:500) describe what a PAST RUN did and are left
  as written history - the order they describe still had 31.
- CLAIMED (HANDOFF_2026-09-01_R9_COMPLETE.md:126): COA-STP1 scale means "13-40 km routes". Understated
  at both ends. Measured by haversine over each task's inline Location list: longest SINGLE route
  T17 = 42.37 km; longest CHAINED total for one performer 1-6/2/1_AD = 77.92 km (T15 35.55 + T17
  42.37). Full census of tasks with a route: T17 42.37, T39 40.20, T15 35.55, T23 28.71, T31 28.71,
  T35 28.71, T1 28.53, T19 28.53, T32 23.60, T13 0.63, T36 0.63. The remaining 31 tasks carry 0 or 1
  Location and have no route length; the 9 with ZERO Locations are T8, T9, T10, T16, T21, T24, T34,
  T37, T38. Corrected in the handoff.
- INCOMPLETE (NEXT_SESSION_GUIDANCE.md:158): "T13/T19 are not even temporally gated". True - both are
  chain heads - but T13 carries the order's ONLY start delay,
  StartTime/SimulationTime/DelayTimeAmount/IsoTimeDuration = P00Y00M00DT03H20M00S (12,000 s), at
  data/COA-STP1_Order.xml:504. All ten other heads carry P00Y00M00DT00H00M00S and no task carries a
  nonzero RelativeTime. So T13 cannot dispatch inside any run window shorter than 3 h 20 m at
  TimeMultiplier 1; a T13 that does not dispatch in a 45-minute window is EXPECTED, not a miss. The
  guidance line now says so.
- RE-VERIFIED and CORRECT as written (no change needed): data/COA-STP1_Initialization.xml has 128
  Unit elements and 35 TacticalArea elements; 67 units are hostile (SIDC char 1 = 'H') and 61 friendly
  ('F'); all 128 are ground (SIDC char 2 = 'G') and all SISOEntityType leaves are zero. SIDC echelon
  char (index 11) census: 'E' 64 -> ArmorCompany (aggregate), 'F' 26 -> ArmorCoHQ (aggregate), 'D' 23
  -> ArmorPlatoon (aggregate, the only branch the 2026-07-22 type fix touched), 15 others ('-' 12,
  'C' 2, 'H' 1) -> the lone-Tank default. So 113 aggregates + 15 entities. 54 units - including all 11
  order performers - share the single spawn coordinate 34.67998497, -116.72479854.
