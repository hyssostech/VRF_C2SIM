# SUPERVISED RECOVERY PLAN (2026-07-16) - the standing plan of record

Written by the supervisor (Fable seat) after the fresh-context adversarial audit (5 Opus
investigators + 16 per-claim adversarial refuters, ~3.1M tokens, every load-bearing claim
below independently re-verified against primary sources before acceptance). Supersedes the
"immediate next task" ordering in START_HERE and the older OPUS_EXECUTION_PLAN backlog for
everything movement-related. ASCII-only. Keep current AS work lands.

Full audit evidence: docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md parts
7/7b/8/9 + the workflow journal (session transcript dir). Run matrix: 23 live runs cataloged;
5 major contradiction pairs, each with its deciding probe (below).

## 0. Mission and definition of done

A coa-gpt COA (COA-STP1-shaped data) pushed to the C2SIM server executes in VR-Forces:
units created, tasked, MOVE and ARRIVE at ordered destinations (telemetry-verified, not
completion-claimed), truthful reports back. DONE means: on a healthy backend, every
route-bearing task for a taskable unit produces displacement-verified arrival within
documented tolerances, zero runaways, zero false DAG advancement - measured by WatchVrf,
never by TASKCMPLT counting.

## 1. Verified state of knowledge (what a week of churn actually established)

THREE INDEPENDENT PROBLEMS, previously conflated (audit-verified decomposition):

- (A) ALTITUDE / the one PROVEN LEVER: the interface (both pristine C++ and port parity
  mode) hardcodes every ground route vertex to 100 m MSL; at Mojave (~1100 m terrain) that
  is ~1000 m underground, unguarded (the C++ guards only below-MSL, never below-terrain).
  `GroundWaypointAltitudeMode=Live` is the ONLY change that ever produced a clean,
  single-variable, telemetry-verified freeze->correct-arrival flip (RUN A vs RUN B:
  1222.MechPlt from degenerate-never-resolved to a full ~1.16 km route ending 8 m from its
  final waypoint). Mechanism NOT settled (Live also flipped the aggregate's position
  RESOLUTION from ECEF-origin sentinel to real coords - it gates more than route geometry).
  KEEP the fix; do not over-read it as sufficient.
- (B) COA-STP1-DATA-SPECIFIC failure: COA-STP1's own units freeze even at the golden Sweden
  region where golden units march (tier1-reverse + DIS-probe). Falsified as causes:
  DIS-type, formation names, nav data, terrain page-in, stacked-pile-size. The ONLY
  remaining untested categorical difference: FORCE SIDE (COA-STP1 problem units are
  WASA/hostile "SH..."; golden are NATO/blue "SF...").
- (C) SESSION/ENVIRONMENT-LEVEL movement-execution block: ALL 2026-07-15/16 TropicTortoise
  runs are confounded by a per-session state that froze EVERY object including the plain
  entity 1.BdeHQ (which had moved cleanly at Mojave on 2026-07-13, R9-A). Proven per-session,
  not per-config: apps 3419 vs 3429 ran IDENTICAL config/data on different backend sessions
  with opposite outcomes for 1222.MechPlt. Sits below task dispatch and below clock
  advancement; survives Freeze-Movement=No; duration/multiplier ruled out by computation.

CROSS-CUTTING VERIFIED FACTS:
- The pristine original C++ interface is NOT a working baseline: RUN C end-state = ZERO
  correct arrivals out of 42 tasks (3 movers ran 49/~90/135 km past/away from their routes
  and terminated PERMANENTLY STATIC and UNDERGROUND, 2 of 3 outside the AO; 6 frozen units
  never moved; 0 of 1,732 objects moving at end-state). "The original works better" is
  falsified in both directions: original = more motion, zero correctness; port+Live = less
  motion, the only correct execution ever recorded at these coordinates.
- VACUOUS COMPLETIONS ARE VRF-SOURCED AND ORIGINAL: the pristine C++ received exactly ONE
  TaskComplete in RUN C - fired 325 ms after dispatch, for a unit that NEVER MOVED, and it
  falsely advanced the DAG (T23 -> T24). The port faithfully relays the same VRF event class
  (RUN B: 114.MechCoy + 1.BdeHQ). Two signatures: at real coords and at degenerate coords.
  CONSEQUENCE: completions must NEVER advance the DAG or be reported outward without a
  displacement check. This is a fix WE must build; VR-Forces will keep lying.
- THE PILE SPLIT IS VRF-INTERNAL: all 9 first-wave-tasked units in RUN C - the 3 movers AND
  the 6 frozen - were initialized at the pixel-identical mega-pile coordinate (54/128 units
  share it), indistinguishable on echelon, DIS type, route origin, route shape, and dispatch
  order (T1 moved, T31 froze - identical on every measured field). H-stack-as-discriminator
  is FALSIFIED; which stacked unit escapes appears nondeterministic inside VR-Forces. One
  unchecked candidate: per-aggregate member counts / formation resolution (offline probe
  below). The pile remains the shared SETTING of the freezes - de-stacking + fan-out remain
  pragmatic mitigations; coa-gpt data feedback (dispersed positions) remains mandatory.
- CODE IS AT PARITY ON THE ENTITY PATH: port orchestration mirrors the C++ executeTask;
  leaf controller calls are param-identical; RUN A emitted the identical calls the C++
  emits and froze anyway. Do NOT edit tasking code to chase (C). Known port-only deviations
  to CONTROL in comparisons: AggregateFormation=auto (aggregate-gated; exonerated for (C)
  by the entity freeze, and held constant across RUN A/B), PredecessorTimeoutPolicy
  (default skip DROPS successor subtrees - 31 of COA-STP1's 42 tasks are STREND-gated;
  the C++ waits forever), and the R10/R11 opt-ins.
- KILLED HYPOTHESES (do not revive): overlap-footprint slowdown (parameter exists only in
  the AggregateLevel model set, not the C2simEx/EntityLevel chain we load; bounded 0.5-1.0;
  min-not-product; cannot zero speed); nav data; terrain page-in; DIS type; formation names
  as sole cause; stacked-pile-size as sufficient cause; template quality (RUN B success
  INVERTED it: generic-fallback platoon succeeded, well-templated company + exact-match
  entity froze); echelon; entity-vs-aggregate immunity; task-duration/multiplier.
- OPERATIONAL: the pristine baseline CANNOT clean-stop against the current server
  (main.cxx:36 hardcodes c2simVersion "1.0.1"; C2SIMinterface.cpp:1686 discards mismatched
  STOMP messages, so it never sees UNINITIALIZED; open puzzle: orders pass the same gate).
  Plan every pristine-C++ run to end with VRF close + user-approved process kill + rtiexec
  restart. The port does not have this defect (REST GetStatus).

## 2. Operating model

- SUPERVISOR (this seat): designs and gates every probe, adjudicates evidence, keeps this
  doc + the investigation doc + the ledger current, coordinates the user's in-the-loop
  steps, never lets an unverified claim anchor the next step.
- EXECUTORS (Opus agents): reading, code, offline analysis, run preparation, live-run
  execution under a written runbook per probe. Findings enter the record only after an
  adversarial refuter pass (the audit's verify layer stays standing policy).
- USER-IN-LOOP: VR-Forces launches/reloads (agent-launch is CONFIRMED UNSAFE), GUI-native
  probes, process-kill approvals.
- PROBE DISCIPLINE (non-negotiable, all pre-registered in the investigation doc BEFORE the
  run): one variable per probe; written prediction + falsifier; telemetry window sized to
  the task DAG (>=15 min for COA-STP1); displacement is the only movement oracle; fresh
  appNo per join (ledger: NEXT FREE 3435); never force-kill a joined federate without
  approval; RUNBOOK secs 0/0.5/0.6 stand.

## 3a. PROBE RESULTS (2026-07-16 afternoon/evening session - THE BREAKTHROUGH)

- **P-C1 DONE - prediction P1 confirmed:** Live-altitude runs twice back-to-back on one fresh
  TT session (apps 3435-3441): 1222.MechPlt marched its full ~1.16 km route and arrived 8 m
  from its final waypoint BOTH times (run 2 = full transit captured, members arrived with it).
  The 2026-07-15 "universal session block" did NOT reproduce - those sessions were anomalous;
  problem (C) is DEPRIORITIZED. The stable reproducible split emerged: platoon arrives /
  114.MechCoy position never publishes / 1.BdeHQ entity frozen at init.
- **P-C2 DONE - route geometry falsified for the entity freeze** (first-leg-zero route on the
  live frozen entity: still frozen).
- **P1a DONE - ROOT CAUSE FOUND (user-executed GUI probes):** native VR-Forces tasking ALSO
  failed on the frozen entity (task Active, speed 0) -> interface tasking exonerated. All
  state clean (AI on, not frozen, fuel 100%). THEN: dragging the entity to a fresh spot and
  re-tasking -> IT MOVES; re-tasking it along the interface's OWN TK1 route -> works fine.
  **VERDICT: ground units are BORN UNDERGROUND** - the interface creates them at fixed-MSL
  altitudes (create pos = ElevationAgl default 1000 MSL; post-create SetAltitude = 1001 MSL;
  route vertices 100 MSL under Fixed100) that sit ABOVE terrain at sea-level Bogaland (clamp
  fixes them) and ~130-1030 m BELOW terrain at 1131 m Mojave. The authors' own README
  documents the consequence: an object at elevation AGL <= 0 "will not execute a route" -
  accepts tasks, shows Active, never moves. Code-verified end to end (VrfFacade.cpp:90-93
  passes altMeters as geodetic MSL; UnitTranslator.cs:101; zero ElevationAgl fields in ALL
  init files so the 1000 default always applies). Full record: investigation doc parts 13/13b.

## 3b. THE IMMEDIATE WORK ITEM - implement the create-time terrain-clamp fix (NOT yet built)

Supervisor-specified design (executor implements; adversarial review before merge):
- **UnitTranslator.cs stays BYTE-PARITY UNTOUCHED** (it is the ported oracle; selftests pin it).
- Implement in the SERVICE layer, gated on the existing `Vrf:GroundWaypointAltitudeMode`:
  - `Fixed100`: behavior EXACTLY as today (byte-parity: create at ElevationAgl MSL, deferred
    SetAltitude ElevationAgl+1, routes at 100) - the golden-parity escape hatch.
  - `Live` (make this THE DEFAULT now): for ground units, do NOT push fixed-MSL altitudes.
    DESIGN SETTLED BY HEADER EVIDENCE (2026-07-16 fix session; investigation doc part 13c):
    setAltitude's third arg is `bool aboveGroundLevel` (vrfRemoteController.h:1390-1392) and
    the facade passes TRUE - the deferred SetAltitude(1001, TRUE) is 1001 m AGL, which
    cannot bury anything and runs identically at working Bogaland; that suspect is DEAD.
    createEntity ground-clamps by DEFAULT (groundClamp = true, header :1295/:1310; the
    facade call takes the default) but a clamp can only DROP an above-terrain birth
    (Bogaland-proven), not raise a below-terrain one; createAggregate has no clamp param.
    PRIMARY TARGET: the CREATE call's own pos.AltMeters - under Live, ground units are
    created at a guaranteed-above-terrain altitude (safe-high MSL constant, configurable,
    default 10000 - above all Earth terrain) and the clamp places them; the deferred
    SetAltitude is SKIPPED for ground units under Live (logged). Air units keep parity
    behavior in both modes. FALLBACK if live acceptance still freezes: programmatic
    drag-mimic (SetLocation re-place at the reflected clamped geodetic via
    TryGetEntityGeodetic, guarded against the known ~30 s degenerate-position transient on
    aggregates - never re-place to a degenerate reading).
- OFFLINE ACCEPTANCE: build 0 errors; all 8 selftests green and UNCHANGED (Fixed100 parity
  intact); code review confirms Live-mode create path never emits a below-terrain altitude.
- LIVE ACCEPTANCE (supervisor-gated, needs user VRF): re-run the R9 lean init + order with
  the fix, NO manual drag - ALL units (entity + both aggregates) must move without
  intervention; telemetry-verified arrivals. Fresh appNos from the Appendix B ledger.
- STATUS 2026-07-16 (fix session): IMPLEMENTED offline per this spec (Opus executor +
  supervisor refuter pass on the diff). VrfSettings: default mode now "Live" +
  CreateAltitudeSafeMslMeters=10000. Service create path: gated on the mode + the route
  path's own SIDC[2]=='G' ground predicate (null-hardened); Live+ground = born 10000 MSL,
  parity SetAltitude skipped (logged); Fixed100 falls to the byte-identical parity branch;
  UnitTranslator + native untouched; appsettings carries no mode pin so the default applies.
  OFFLINE GATES GREEN (build 0 errors; 8/8 selftests unchanged, re-run independently by the
  supervisor). **LIVE-ACCEPTED (2026-07-16 evening, FIX-ACCEPT-1, apps 3443-3447,
  prediction P1): ALL THREE units moved with NO drag** - entity 1.BdeHQ marched 1157 m and
  parked 0.5 m from its final waypoint (the frozen class is CURED); platoon third textbook
  8 m arrival; company PUBLISHED + marched 701 m on-axis, halting on the documented
  leading-edge completion (center 412 m short by design, not a freeze). Both supervisor-
  noted live risks cleared (no floating units, no runaway, no 10 km route vertices).
  Full record: investigation doc part 14 RESULT. Vacuous completions unchanged - the
  sec-4 truthful-arrival gate is now the top open work item. Open item (a) 114.MechCoy
  never-publishes DID NOT REPRODUCE under the fix (downgraded, one more reproduction to
  close). R9-A lean-vs-full anomaly moot for the fix.

## 3c. 2026-07-16 FIX-SESSION RESULTS + RESHUFFLED NEXT WORK (supersedes the sec-3 queue
## ordering below for movement work; full records: investigation doc parts 14/15/16)

- FIX-ACCEPT-1 (part 14): sec-3b fix LIVE-ACCEPTED, prediction P1 - all three lean-set
  units moved with NO drag; entity parked 0.5 m from its waypoint. Entity-freeze CURED.
- COA-DEMO-1 (part 15): full COA-STP1 under the fix - 38 movers (RUN C: 3) but E4
  FALSIFIED: runaway class persists at scale (541 km top, underground/offshore
  terminations). Yellow warning badges on most units - UNIDENTIFIED (docs dig queued).
- CPP-ALT-1 (part 16, user-directed): PRISTINE C++ + one constant (birth 1000->10000 MSL,
  branch b96688b) - 6 tasked units marched 18+ km on-terrain (Q1: root cause confirmed
  code-independent), 5 stayed frozen at the pile (split is altitude-independent; MAK
  question sharpened), runaway/warp class present in the pristine too (Q2: altitude
  exonerated for runaway in BOTH codebases), and a NEW signature: all marchers stopped
  mid-route on a common ~18.4 km radius (tile-boundary vs waypoint-stall - own probe).

NEXT WORK, in order:
1. TRUTHFUL-ARRIVAL GATE (sec 4 item 1) - unchanged top priority; completions proven
   erratic in BOTH directions this session (instant-vacuous on the lean run; 1-in-13-min
   at scale).
2. RUNAWAY MECHANISM FORENSICS + CONTAINMENT (sec 4 item 2b) - now cross-codebase,
   altitude-exonerated; fold in the tile-boundary question (marcher stop radius AND
   runaway terminations) and the warp signatures (54-73 km instantaneous out-and-back).
   Offline first: per-taskee census from the archived 3450/3453/3454 CSVs; identify the
   fast-cluster objects; map the paged-tile extent vs the 18.4 km stop radius.
3. Mid-route stop discriminator (cheap): page-in state at the stop coordinates / larger
   paged area re-run.
4. VRFLAUNCHER SELF-LAUNCH RECIPE (user-directed 2026-07-16): probe-gated first trial
   (launch via vrfLauncher.exe, then ResetVrf --dry-run must NOT 0xC0000005) - removes
   the human-launch dependency. RUNBOOK sec 0.5 has the seed recipe.
5. MAK support question (sec 6 draft): now well-earned - the pile mover/frozen split
   reproduces in both codebases with above-ground births and all offline discriminators
   falsified.
6. P-B force-side flip + P1b strict A/B: as queued below, when the above land.

## 3. Probe queue (ordered; each names its decision)

- P-C1 (FIRST, decides problem C, needs user, ~10 min): on the next VRF launch, run the
  SAME lean init/order with Live clearance=50 TWICE back-to-back on ONE backend session
  (apps from 3435). If run 1 moves and run 2 moves: (C) was specific to the dead sessions;
  proceed. If frozen: capture per-object "AI Enabled" + the Start/Resume PDU setting, then
  P1a GUI-native tasking (pre-registered, part 8) on the frozen units - its four outcomes
  discriminate interface-tasking vs pile vs defective-object vs platform.
- P-C2 (cheap, same session): give the frozen entity an order whose FIRST waypoint is AT
  its current position (RUN B correlation: the mover's first waypoint was ~60 m from init;
  the frozen units' were 575 m / 1.7 km away). Single variable: first-waypoint distance.
- P1b (THE decisive comparison, one session): port vs pristine C++ on the SAME backend +
  SAME data (lean Mojave set preferred). Port controls for parity: AggregateFormation="",
  GroundWaypointAltitudeMode=Fixed100 first (strict parity), PredecessorTimeoutPolicy=force.
  Then a second port arm with Live altitude. Decides: does the port add/remove failures vs
  the oracle, and is (A)'s lever reproducible on a healthy session.
- P-A (after C cleared): the clean altitude A/B - Fixed100 vs Live, same session,
  back-to-back. First genuinely valid test of (A)'s mechanism scope.
- P-B (independent, Sweden, anytime): force-side flip - take proven-marcher 1222.MechPlt
  and flip ONLY Side to WASA/hostile. Freeze => force-side implicated; move => (B) is
  something subtler in COA-STP1's records (next: field-by-field bisection of one COA-STP1
  unit vs one golden unit).
- P-OFF1 (offline, executor, no VRF needed, run NOW): in the RUN C 42MB log + init XML,
  audit per-aggregate member counts / subordinate creation / formation resolution for the
  3 movers vs 6 frozen - the forensics' one unchecked discriminator candidate.
- P-OFF2 (offline, running): mine the original authors' own docs (C2SIM-VRForcesv2.26.pdf,
  VRFadditionalFiles inventory vs installed SMS, protocol-version guidance) - agent
  in flight.
- P-RUNAWAY (piggybacks on any mover): log the exact final route vertex XY/Z handed to
  VRF; verify at-distance satisfiability; watch for AO-exit. Decides whether runaway is
  unreachable-endpoint or off-terrain-no-clamp (or both).

## 4. Fix pipeline (build after probes; some can start now)

1. TRUTHFUL-ARRIVAL GATE (start NOW - independent of all probes): completions from VRF
   never advance the sequencer or produce an outward TASKCMPLT unless center displacement
   confirms arrival within documented tolerances (entity at-distance ~1 m/near 25 m; unit
   in-position 0.2 m; leading-edge prematurity compensated). Kills the false-cascade class
   (RUN C's only DAG advance was a lie) and makes every future run self-measuring.
2. ROUTE-VERTEX TERRAIN GUARD (start NOW): never emit a below-terrain vertex; clamp to
   live ground + clearance (generalizes the proven Live fix; add an explicit guard +
   warning log). Plus runaway containment: abort/halt a mover that exits a configured
   AO radius or exceeds route length by a factor.
3. Pile strategy (after P1a/P-OFF1): R10 fan-out remains the proven Mojave mover for
   stacked data; de-stack remains creation hygiene; coa-gpt feedback memo gains the
   verified "54 units at one coordinate produce nondeterministic VRF-internal gridlock"
   item with RUN C evidence.
4. (B)'s fix per P-B outcome.
5. Demo script + acceptance run: full COA-STP1 on a healthy backend, port, all fixes on,
   >=15 min window, scored ONLY by displacement-verified arrivals / zero runaways / zero
   false advancement.

## 5. Current operational state (2026-07-16 evening, at session handoff)

- VR-Forces: user-launched TT session was UP at handoff, with **port app 3439 possibly STILL
  RUNNING and its units alive** (the user was live-experimenting with 1.BdeHQ). NEXT SESSION
  STEP 0: check for a running VrfC2SimApp process; if present, StopIface (clean stop - note
  Solution A will DELETE the created units on stop, so confirm the user is done with them
  first). C2SIM server state was RUNNING.
- Ledger: apps 3200-3441 consumed (Appendix B current). NEXT FREE: 3442.
- Repos: C++ master = pristine 191933a (Phase-1 branch in sibling worktree); port main =
  current work, fix NOT yet implemented; all session evidence archived under
  docs/experiments/ (parts 7-13b of the investigation doc are this session's record).
- New data file: data/PC2_EntityFirstWaypoint_Order.xml (P-C2 probe order, reusable).
- License expires 2026-09-15. Headless VRF launch remains CONFIRMED UNSAFE.

## Decisions log

- 2026-07-16: user directed full review + supervised multi-agent operating model ("you in a
  supervisory role, deploying other agents"). Audit ran (2 waves); this plan supersedes
  prior next-task orderings for movement work. Probe queue above is the authoritative order.
