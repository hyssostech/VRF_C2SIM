# START HERE - the C2SIM VR-Forces -> .NET port

*** STOP. THIS FILE IS BUILD / REPO REFERENCE ONLY, AND ITS STATUS BLOCK IS FROZEN AT
2026-07-16. IT IS NOT THE CURRENT STATE AND NOT THE ENTRY POINT. ***

  - CURRENT STATE, MISSION AND NEXT ACTION: docs/RESUME_PROMPT.md
  - GOVERNING STATEMENT: docs/VRF_GROUNDWORK_PLAN.md sec 1a (THE HEADLESS MANDATE)
  - THE NEXT ACTION: docs/HEADLESS_RUN_PLAN.md

*** WHAT WE ARE BUILDING: a HEADLESS interface. A C2SIM document goes in; units are
initialized, tasked and run in VR-Forces; the outcome is verified FROM TELEMETRY. One
button. ZERO humans in the UI - no tasking by hand, no clicking on terrain, and no agent
driving the GUI either. Verification is arithmetic on the WatchVrf POS trace; it is not
a human review step and never was. ***

ANY TEXT BELOW THAT DESCRIBES A HUMAN AT THE GUI, A HUMAN LAUNCHING VR-FORCES, OR A
"native-GUI baseline" AS CURRENT PRACTICE IS HISTORICAL AND RETIRED. VR-Forces launches
AND tears down unattended (scripts/LaunchVrf.ps1 / scripts/StopVrf.ps1, 0.4 PASSED
2026-07-18). On 2026-07-18 a supervisor read exactly this kind of stale line, concluded
live testing needed a human, and burned a work block building GUI automation to route
around it. Do not repeat that.

If you are picking this up in a fresh session with no prior context, read
docs/RESUME_PROMPT.md first - NOT this file. This file remains useful for build
commands, repo layout and historical evidence. ASCII-only.

## What this is

Porting the GMU `c2simVRFinterface` (C++, wraps MAK VR-Forces via
`DtVrfRemoteController`) to .NET on top of the HyssosTech C2SIM .NET SDK.

Three locations are in play:
- THIS repo `VRF_C2SIM` - the .NET port and its HOME. Nested submodule under the
  fork at `Software/Interfaces/VRF_C2SIM`. All port docs + products live here now.
  This is the SINGLE SOURCE OF TRUTH.
- The DEPRECATED C++ interface `c2simVRFinterfacev2.36` (separate repo at
  `C:\Users\PauloBarthelmess\Source\Repos\C2SIM\c2simVRFinterfacev2.36`). Ported
  FROM. Kept as the FROZEN parity oracle + the only rig that can regenerate a
  golden trace. Do NOT develop the port there.
- The SDK `OpenC2SIM.github.io` at `Software/Library/CS/C2SIMSDK`, branch
  `dev/sdk-fixes` (NOT merged, NOT pushed). The C2SIM half of the port rides on it.

## Read in this order

1. `docs/PORT.md` - the master reference. Every settled decision WITH its evidence
   (feasibility, architecture, toolchain, environment, golden-trace baseline,
   interface bugs, SDK changes, decisions log). Trust it over any summarized
   recollection. ESP. sec 8 (phase status) + sec 10 (the two-layer semantic map).
2. `docs/APP.md` - the .NET app (`src/VrfC2SimApp`): architecture, data flow, what is
   DONE vs the Phase 4 parity-port TODO. THE CURRENT WORK.
3. `docs/PHASE2_BRIDGE.md` - the C++/CLI bridge in `src/` (DONE): proven build config,
   the native/managed split, the callback mechanism.
4. `docs/RUNBOOK.md` - operational runtime procedure (needed only to run the C++ parity
   rig or, later, the .NET app against live VR-Forces).
5. `docs/NEXT_SESSION_GUIDANCE.md` - the 2026-07-12 deep-review deliverable (verified
   corrections + the aggregate experiment ladder). READ LAST; where it conflicts with
   older docs IT WINS.
6. This file for repo state, build/run commands, and where artifacts live.
7. History/reference as needed: `docs/PHASE1_REWIRE.md`, `docs/TASK_EXPANSION_PLAN.md`.

## Current status (2026-07-16)

> **BREAKTHROUGH (2026-07-16 evening): THE ENTITY-FREEZE ROOT CAUSE IS FOUND, LIVE-VERIFIED,
> CODE-VERIFIED, AND AUTHOR-CORROBORATED - ground units are created UNDERGROUND at
> high-elevation terrain** (fixed-MSL create/SetAltitude/route constants that are above-ground
> at sea-level Bogaland and ~130-1030 m below terrain at 1131 m Mojave; VR-Forces then accepts
> tasks but never executes movement, per the original authors' own documented rule). Proven by
> the user's live GUI probes: native tasking failed identically on the frozen entity until a
> GUI drag re-placed it on the surface - after which it even ran the interface's OWN route
> cleanly. The Live-altitude route fix was separately proven REPRODUCIBLE (two back-to-back
> textbook arrivals). **THE FIX IS IMPLEMENTED AND LIVE-ACCEPTED (2026-07-16 fix session,
> FIX-ACCEPT-1, prediction P1: ALL THREE units moved with NO drag)**: GroundWaypointAltitudeMode
> =Live is now the DEFAULT; under Live, ground units are created at CreateAltitudeSafeMslMeters
> (10000 MSL, above all terrain - VRF's create ground clamp places them on the surface) and the
> parity post-create SetAltitude is skipped (it is AGL anyway per the MAK header - part 13c);
> Fixed100 remains the byte-parity escape hatch. LIVE RESULT: the frozen entity class is CURED
> (1157 m march, parked 0.5 m from its waypoint); platoon third textbook 8 m arrival; the
> never-publishes company PUBLISHED + marched (leading-edge halt is the documented completion
> semantics, not a freeze). Vacuous completions unchanged - the TRUTHFUL-ARRIVAL GATE (plan
> sec 4 item 1) is now the TOP open work item. See docs/SUPERVISED_RECOVERY_PLAN.md sec 3b
> (spec + status), investigation doc parts 13/13b/13c (evidence chain) and part 14
> (pre-registration + RESULT). Remaining open: 114.MechCoy
> position-never-publishes (distinct signature), truthful-arrival gate (completions lie in
> BOTH interfaces - VRF-sourced), runaway containment, force-side probe (Sweden), P1b
> port-vs-C++ A/B.
>
> **2026-07-16 (later): PHASE 0 CORE OF THE GROUNDWORK PLAN IS DONE** - see
> docs/VRF_GROUND_TRUTH.md (read sec 0.0 first). Headlines: the yellow unit badges are
> UNREAD VRF Object Console WARNINGS (a remote capture callback exists - the 0.6 tool is
> the first build item); platoon vs company templates wire DIFFERENT movement
> controllers (matches the live per-class split); COA-STP1 type fidelity quantified
> (~64/128 mis-mapped, ~49 generic fallback); remote saveScenario-to-scnx confirmed.
> Lessons learned registered in the groundwork plan. Session-jump prompt:
> docs/RESUME_PROMPT.md.
>
> **2026-07-16 (late) - NEW PLAN OF RECORD FOR MOVEMENT WORK: docs/VRF_GROUNDWORK_PLAN.md**
> (user-directed reset after the fix session: the buried-birth fix is accepted in both
> codebases, but runaway/warp and the pile split proved CODE-INDEPENDENT - the remaining
> problems live on the VR-Forces side of the boundary. The groundwork plan: learn native
> VRF (content catalog, docs curriculum, API audit, scnx spec-diffing), establish a
> native-GUI baseline at Mojave [METHOD RETIRED 2026-07-18 - see sec 1a; a native
> control arm, if ever needed, comes from a GUI-authored .scnx loaded programmatically,
> NOT from a human session], then rebuild the creation/mapping layer onto REAL
> installed unit types with a truthful-arrival gate. Read it FIRST.)
>
> **The prior plan, docs/SUPERVISED_RECOVERY_PLAN.md** (2026-07-16) -
> written after a fresh-context adversarial audit (5 Opus investigators + 16 refuters; every
> claim independently re-verified). It supersedes this file's "immediate next task" sec 1 and
> the OPUS_EXECUTION_PLAN backlog for everything movement-related. Read it FIRST. Headlines:
> the "Mojave freeze" is THREE independent problems (A: hardcoded-100m altitude - the Live
> fix is the one PROVEN lever, sole clean arrival ever recorded; B: COA-STP1-data-specific,
> force-side untested; C: a per-SESSION movement-execution block that confounded all
> 2026-07-15/16 TropicTortoise runs). The pristine original C++ was rebuilt and run on full
> COA-STP1 (RUN C): ZERO correct arrivals - 3 movers ran 49-135 km away and terminated
> underground/offshore, rest never moved; vacuous completions are VRF-SOURCED and ORIGINAL
> (one fired 325 ms after dispatch on a frozen unit and falsely advanced the DAG). Killed
> hypotheses (do not revive): overlap-footprint, nav data, page-in, DIS type, formation
> names, pile-size-as-sufficient, template quality, echelon, entity-immunity, duration.
> Evidence: investigation doc parts 7/7b/8/9 + the run matrix (23 runs).

> REALITY CHECK (do not oversell): the end-to-end product goal - a coa-gpt scenario at MOJAVE with
> correct aggregate movement - does NOT work. Mojave aggregates FREEZE; root cause UNSOLVED (nav data
> was FALSIFIED as the cause). The "SUCCESS/COMPLETE" bullets below are component / control-region
> results (Sweden movement, semantic dispatch, offline selftests) with known live warts (unreliable
> completion events; F1 runaways + F2b vacuous completions in the Mojave scale run; lean-creation
> stashed after a tasking regression). Read them as component milestones, not as "the product works".
> UPDATE (2026-07-15): the freeze is NOT purely a region effect either - COA-STP1's OWN unit data
> (coordinate-transplanted onto the golden Sweden region, genuine leader-path, no fan-out) ALSO
> froze there, TWICE (unspecified DIS type, then a real DIS type borrowed from a golden unit -
> DIS-TYPE HYPOTHESIS FALSIFIED, identical failure both times). A live altitude-mode probe
> (`GroundWaypointAltitudeMode=Live`) at Mojave was finally run twice (clearance 0 and 50) after a
> real infrastructure crash was found + ROOT-CAUSED (NOT Mojave content - a headless-CLI-launch
> gap; RUNBOOK sec 0.5) - but BOTH altitude runs are INCONCLUSIVE: even 1.BdeHQ, the entity that
> has moved in every prior Mojave test, was completely frozen in both, an unprecedented new
> failure with no missing-control test yet to explain it.
> **UPDATE (2026-07-15, later same day, twice-corrected) - documentation-grounded, CONVERGES
> WITH Thread A rather than replacing it**: our unit-creation DIS type codes for echelon D/E/F
> (ArmorPlatoon/ArmorCompany/ArmorCoHQ - i.e. MOST of both the golden and COA-STP1 populations)
> do not match any SPECIFIC VR-Forces unit template, but DO match a real (if generic/anonymous)
> intermediate fallback, `EntityLevel\vrfSim\Ground_Aggregate.entity` (an earlier draft of this
> finding wrongly called this a "content-free empty shell" based on a truncated grep result - it
> is not; it has real formations and real, if generic, subordinates - CORRECTED). Read directly:
> that template wires `aggregate-lead-follow-in-formation-controller` with `ground-clamp True` -
> the EXACT controller Thread A identified from binary analysis as the `moveAlong() - empty
> route` source, now independently confirmed from the actual sysdef config. Golden units hit this
> SAME fallback and DO work at Sweden (verified: a full-population SIDC echelon count, not a spot
> check, shows both golden and COA-STP1 are ~80-88% D/E/F, i.e. mostly the same code path) - so
> this finding explains general fragility (why formation needs a runtime patch, why subordinates
> are 4 anonymous vehicles not the unit's real composition) but NOT the Sweden/Mojave split by
> itself; Thread A's fixed-100m-MSL-waypoint-vs-real-terrain-height mechanism remains the best
> explanation for THAT, now with an extra, independent confirmation (ground-clamp really is
> deliberately enabled on this exact path). Separately, directly verified (not inferred) that
> 1.BdeHQ's SIDC echelon is 'H' (Brigade, unhandled by the B/D/E/F switch) so it defaults to a
> REAL platform DIS type matching VR-Forces' own documented M1A2 example - explaining why it
> alone has always moved reliably, everywhere. Also directly re-diffed the Bogaland2 vs
> TropicTortoise `.scnx` scenario configs (all 11 constituent files, not trusting the 2026-07-14
> claim on faith) - confirms them equivalent; no overlooked scenario-config gap found. See
> docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md "2026-07-15 (fresh session, part
> 3) - CORRECTION to part 2" for the full, corrected evidence chain. STILL OPEN: why COA-STP1's
> own units fail even at Sweden (force-side/hostility still untested); the altitude-probe
> confound (1.BdeHQ freezing under vrfLauncher is a SEPARATE mystery from all of the above, since
> it's the real-entity path, not the aggregate/ground-clamp path).

- **Crash root-cause + altitude probe (inconclusive) + XML gotchas (2026-07-15, LIVE, apps
  3386-3420)**: see the investigation doc's "2026-07-15 SESSION SYNTHESIS" section (top of this
  status block links there) for the full, load-bearing account - do not rely on this bullet
  alone. Headline facts: (1) DIS-type hypothesis FALSIFIED live, twice; (2) a real vrfSim crash
  (`0xC0000005` in `VrfFacade::Tick()`) was reproduced 3x, ROOT-CAUSED to this session's headless
  `vrfSimHLA1516e.exe` CLI launch (RUNBOOK sec 0.5, now marked UNSAFE) rather than to Mojave/
  TropicTortoise content - a human must launch VR-Forces via the GUI/`vrfLauncher.exe` until a
  headless recipe is worked out [RETIRED 2026-07-18: 0.4 PASSED. VR-Forces launches UNATTENDED
  via scripts/LaunchVrf.ps1 and tears down via scripts/StopVrf.ps1, both EXIT=0 with zero human
  interaction. The human-launch dependency NO LONGER EXISTS - RUNBOOK sec 0.5]; (3) the altitude
  probe itself (clearance 0 vs 50) produced NO
  telemetry-confirmed movement for ANY unit in either run, including the entity that has always
  worked before - CONFOUNDED, not a valid test of the altitude hypothesis yet; (4) RUNBOOK sec
  0.6 documents two real XML-comment gotchas that silently break server pushes (a prolog comment
  breaks init; ANY block comment breaks order STOMP delivery) - read before authoring any new
  data/*.xml file. AppNos next free: 3421.
- **Mojave aggregate-movement cause - NAV-DATA HYPOTHESIS FALSIFIED (2026-07-14)**: the
  terrain-page-in -> nav-data investigation was a DETOUR; **nav data is NOT the cause and
  generating/loading a nav mesh does NOT fix it. Do not restart that thread.** Decisive comparison
  (user-prompted): Bogaland2 (Sweden, moves) and TropicTortoise (Mojave, freezes) use the IDENTICAL
  streaming terrain (`MAK Earth Space (online).mtf`), model set (`C2simEx.sms`), and page-in area;
  NEITHER region has nav data. Sweden aggregates march 5+ km via the genuine leader-path
  (SubordinateFanOut OFF, no NavMesh); Mojave freezes. Same terrain, same code, no nav data either
  place -> nav data is not the differentiator; the "leader-path needs a NavMesh" theory dies on the
  Sweden control. Sub-findings that STAND (verified): terrain page-in FALSIFIED as the fix (apps
  3378-3379); programmatic .scnx generation works (a KEEPER); vrfSim.log shows the BE never loads
  nav data on scenario open for this streaming terrain. REAL cause reverts to the R9 finding and is
  UNRESOLVED at the root: region-specific route/offset planning failure at Mojave - member
  OFFSET-ROUTE generation returns EMPTY (0 routes vs Sweden's 45; `moveAlong() - empty route`).
  Root cause (BE terrain-paging depth vs VR-TheWorld data at the AO vs an aggregate-movement
  setting) NOT yet found = a fresh investigation, and it is NOT nav data - do not present a fix as
  known. INTERIM PROVEN MOVER: SubordinateFanOut marches member entities at Mojave (R10). Evidence:
  docs/experiments/navdata_FALSIFIED_bogaland_vs_tt_2026-07-14.txt (supersedes the nav-data saga +
  sec 6). AppNos next free: 3386.
- **SEMANTIC Units 2/5 Run 1 (2026-07-14, LIVE, apps 3368-3372) - SUCCESS**: task (c) from the
  RESUME_PROMPT. Behavior-verified the two-layer semantic map's Unit 2 (BREACH -> DtBreachTask) +
  Unit 5 (SCREEN/Reconnoiter -> DtPatrolRouteTask; ESCRT/Escort -> DtFollowEntityTask) LIVE at the
  golden SWEDEN region via a synthetic distinct-target order (synthetic_semantic_sweden.xml) -
  because COA-STP1 self-targets every verb, these DISTINCT-AffectedEntity paths were never
  exercisable there. All 3 dispatched to their distinct vrftask AND telemetry-backed (WatchVrf):
  14.MechBn breach-marched 5318 m + breach fired (via the 300s fallback - aggregate move-complete
  event fired unreliably, F2-adjacent) + TASKCMPLT; 1222.MechPlt patrol 5634 m (no completion, by
  design); 1.BdeHQ followed 14.MechBn 9016 m + TASKCMPLT (zero-offset wart reproduced - cosmetic).
  Config-only + synthetic XML - NO code change (all wired; the scoping pass confirmed Units 2/4/5
  are category-A, no native rebuild). AggregateFormation=auto confirmed working (the backend
  "Column-Left invalid formation" line is the harmless birth-default, overridden - the 5+ km marches
  prove it; NOT a regression). Clean stop (55 deleted) + ResetVrf swept 2 race leftovers + confirm
  dry-run clean. Full record SEMANTIC_MAPPING.md sec 7; evidence
  docs/experiments/semantic_units245_run1_2026-07-14.txt.
- **SEMANTIC Unit 4 Run 2 (2026-07-14, LIVE, apps 3373-3377) - SUCCESS; TASK (c) COMPLETE**:
  Unit 4 (MoveIntoFormation -> DtMoveIntoFormationTask) behavior-verified at Sweden with
  Vrf:MoveIntoFormation=wedge (LOWERCASE - R5 ground truth; the VrfSettings.cs "Title-Case" comment
  is stale). Order synthetic_moveinformation_sweden.xml (1 MOVE, aggregate 14.MechBn -> dest
  58.705,16.43). The config-gated Unit-4 path fired (NOT moveAlongRoute; 'wedge' accepted); 14.MechBn
  moved 3990 m and ARRIVED 4 m from the destination (telemetry - NOT the R11 vacuous-completion trap)
  + TASKCMPLT. CAVEAT: the completion event fired ~40s EARLY (F2-adjacent) though the move was
  real+precise; per-member wedge geometry not separately measured. Clean stop (53 deleted) + ResetVrf
  swept 1 leftover. This COMPLETES task (c) - Units 2 (Breach), 4 (MoveIntoFormation), 5 (Reconnoiter,
  Escort) all behavior-verified at Sweden. CROSS-CUTTING follow-up: aggregate move-completion events
  are unreliable (moveAlong's never fires; MoveIntoFormation's fires early) - both MOVE correctly, only
  the TASKCMPLT arrival-timing is off; harden if C2SIM report arrival-accuracy matters. Evidence
  docs/experiments/semantic_unit4_moveinformation_run2_2026-07-14.txt. AppNos next free: 3378.
- **F3 PROBE (2026-07-13 evening, LIVE, apps 3363-3367) - F3 CONFIRMED**: the post-backlog
  follow-up (RESUME_PROMPT candidate (a)). Re-ran the FULL COA-STP1 42-task order at Mojave
  with ONE variable family changed vs the scale run - FanOutStragglerSeconds 600 -> 900 (above
  real 20x march times) and TaskPredecessorTimeoutSeconds 600 -> 1200 (so the 900 s straggler
  wins the race); all else identical. RESULT: predecessor-timeout skips 15 -> 2, dispatched
  tasks 15 -> 23, 22 TASKCMPLT. Of the scale run's 15 predecessor-timeout skips, 8 became
  DISPATCHES + 2 advanced to no-loc + 3 reclassified to cascade; the 2 remaining are ORTHOGONAL
  to F3 (T14 orphaned by the T13 3h20m delay task; T4 a unit-BUSY re-task). The straggler-below-
  predecessor lever now UNBLOCKS successors (was the F3 dead-heat). P4a held (0 x 10048 over
  ~51 min); clean stop (186 deletes); post-run sweep clean. MOVEMENT QUALITY UNCHANGED (as
  designed - F3 is orchestration, not terrain): F1 runaways (top members 181/165/94 km) + the
  F2b vacuous class (4-27, 5-20, B/5-20, same 3 units) persist. (Partial quorum, FanOutCompletionFraction < 1.0, was floated as the next lever
  here but is NOT pursued and would NOT help this F2b class - those units already report FULL
  quorum (4/4, 18/18) with zero telemetry arrival, so lowering the fraction changes nothing; it
  targets stuck-stragglers, a different class. The real blocker is the empty member offset-route
  generation at Mojave; see the top 2026-07-14 bullet. The SHIPPED Step-2 quorum/straggler code
  stays as-is.) Full record UNIT_MOVEMENT_RESEARCH.md sec 4c; evidence
  docs/experiments/F3_probe_2026-07-13.txt. NOTE: config-only run - NO code change (the fixed
  900/1200 already delivers the win; route-length scaling is now OPTIONAL). AppNos next free: 3368.
- **Step 3 (P4b position-report bundling) LANDED offline (2026-07-13, execution) - opt-in,
  DEFAULT OFF, live pass PENDING**: `ReportBuilder.BuildPositionReportBundle` builds ONE
  ReportBody carrying N PositionReportContent blocks with one ReportID minted at flush
  (C++-parity, textIf.cxx:435-544). Bundle `ReportingEntity` = the FIRST fix's subject uuid:
  the C++ oracle's bundle envelope reporting-entity is INCONSISTENT across its three flush
  paths (count->last fix, size-overflow->the overflowing fix, reminder-timer->first fix), so
  per plan 3.1 the first fix's uuid was chosen (it matches the C++ timer flush and is closest
  to single-report semantics). Service: a `_posBundle` accumulator (guarded by `_posBundleLock`;
  snapshot-under-lock, build+push OUTSIDE the lock) drained by the count/size trigger in
  `OnVrfTextReport`, a periodic `PositionBundleFlushLoopAsync` (`BundleFlushMs`, gated on
  `_stoppingToken`), and a final flush-on-stop placed with the clean-stop cleanup BEFORE the
  SDK disconnect / bridge resign. Size is a SECONDARY guard (a running per-fix byte estimate);
  count is PRIMARY. TASKCMPLT is NEVER bundled. Four opt-in settings (`Vrf:BundlePositionReports`
  default FALSE, `BundleMaxReports=10`, `BundleMaxBytes=10240`, `BundleFlushMs=2000`) -
  DEFAULT-OFF is byte-for-byte today's one-report-per-POSITION path. `--report-selftest` 9 -> 16
  (+7 bundle checks: 3-content round-trip, per-content uuid/lat/lon + time, one ReportID,
  envelope ReportingEntity, 1-fix == single-content shape). App builds 0 errors; all eight
  offline selftests green. LIVE pass PASSED (2026-07-13 evening, apps 3360-3362, golden
  Sweden scenario, ~60 min at 20x): 5,524 bundles / 46,889 fixes pushed with ZERO failures;
  the c2sim-server 4.8.4.9 INGESTS multi-content envelopes and re-broadcast them INTACT
  (ListenReports captured 1,669 envelopes / 14,121 ReportContent blocks, one ReportID each;
  the first-fix ReportingEntity survived the wire); zero 10048 (P4a holding under bundle
  load); all three flush triggers observed live (count 71%, timer 1-9-fix partials,
  flush-on-stop's final bundle during cleanup). The TASKCMPLT-never-bundled property stays
  offline-proven (no completion fired - golden-parity unrepaired aggregate, known R5-arc
  behavior; piggyback a live sample on a future bundling-ON run). Evidence
  docs/experiments/P4B_live_pass_2026-07-13.txt. NOTE: 2 Solution-A-race leftovers remain
  on the federation (the ResetVrf sweep on 3363 was permission-denied); the next session's
  standard pre-run sweep clears them.
- **Step 5 (COA-STP1 FULL 42-task LIVE scale run) RAN (2026-07-13, execution) -
  pipeline PASS at scale; P4a + Step-2 PASS live; movement MIXED**: 128 units +
  35 areas, mega-pile de-stacked (54 units), formation repair 113/113, 14
  fan-outs; the order fully drained (15 dispatched-and-completed / 5 no-location
  / 21 skip-gated / 1 delay-gated T13 = 42); clean stop (Solution A 178 deletes)
  + ResetVrf swept 1 leftover; appNos 3355-3359. P4a discriminator: ZERO 10048 /
  "Connection error:" over ~50 min at 20x (every pre-P4a run had them). Step-2:
  5 straggler syntheses + 9 quorum syntheses, ZERO "NO in-flight task recorded".
  Telemetry (WatchVrf, 1785 objects): 856/HHC 24.1 km full-quorum march (the
  showcase), 1-35 + 40 28.5 km arrivals, C/1-35 40.2 km (+6.2 km overshoot) -
  but ~half the 15 TASKCMPLTs are NOT displacement-backed: F1 runaway (1-6 drove
  53.8 km, 18.4 km past its route end), F2 vacuous completion on the one unfanned
  aggregate move (1-1 sat at spawn, completion fired), F2b NEW member-level
  vacuous completions (4-27 x3, B/5-20, 5-20 legs 2-3: quorums, zero arrivals),
  F3 the 600==600 straggler-vs-predecessor race (skip won every time; straggler
  timeout must scale with route length and sit BELOW the predecessor timeout).
  Full record UNIT_MOVEMENT_RESEARCH.md sec 4c; evidence
  docs/experiments/COA-STP1_scale_2026-07-13.txt.
- **Step 4 (coa-gpt memo) LANDED (2026-07-13, execution)**: docs/COA_GPT_FEEDBACK.md - the
  outward-facing data-quality memo for the coa-gpt team (4 evidence-backed items: distinct
  AffectedEntity for engagement verbs; timing hygiene; dispersed positions, nuanced by R8;
  region validation - the strongest item). Every claim verified against its cited source;
  supervisor-reviewed. Pointer also in PORT.md sec 10.
- **Step 2 (fan-out robustness) LANDED (2026-07-13, execution)**: completion QUORUM +
  straggler TIMEOUT + late-straggler SWALLOW added to the R10
  fan-out path. `FanOutTracker` gains a per-fan-out `Synthesized` state and captured `Fraction`:
  `TryCompleteMember` now synthesizes the unit TASKCMPLT once `completed >= ceil(Total*Fraction)`
  and, once synthesized, SWALLOWS the remaining member completions (new `alreadySynthesized`
  out) so a late straggler no longer emits a spurious empty-uuid TASKCMPLT; new
  `TrySynthesizeByTimeout(unit, expectedTaskUuid, ...)` with a LOAD-BEARING uuid supersession
  guard. Service: `SynthesizeUnitCompletion` factored out of `OnVrfTaskCompleted` (no direct
  `_bridge.*` call - deferred engages still go through `_tickActions`) and driven from BOTH the
  quorum branch and a detached `FanOutStragglerAsync` hard-cap timer. Two opt-in settings
  (`Vrf:FanOutCompletionFraction` default 1.0, `Vrf:FanOutStragglerSeconds` default 0) - DEFAULTS
  ARE A NO-OP (fraction 1.0 reproduces today's last-member-only behavior; the timer is off).
  `--fanout-selftest` 17 -> 36 (regression guard proves 1.0 == legacy). App builds 0 errors;
  all eight offline selftests green. Step 2.4 (fan out the single-point MoveToLocation path) was
  CUT (Appendix E lean-to-cut, supervisor-confirmed).
- **Step 1 (P4a) LANDED (2026-07-13, execution)**: SDK shared static HttpClient committed
  on the fork `dev/sdk-fixes` (`ae09fd5`; ClientLib 4.8.3.2 -> 4.8.3.3). Offline gates green
  (both TFMs 0 errors; eight selftests; SDK tests 36 passed/3 env-skipped). Detail: PORT.md sec 7.
- **Plan REVIEWED - execution is next (2026-07-13, evening)**: `docs/OPUS_EXECUTION_PLAN.md`
  passed a fresh review pass (under Fable 5); fixes applied: the sec-0.2 selftest exe path
  gains its real win-x64 RID subfolder (verified on disk - the old path would have failed the
  first gate), the Step-2 TrySynthesizeByTimeout signature now carries the LOAD-BEARING
  supersession guard (expectedTaskUuid - without it a superseded task's timer could complete
  the NEW task prematurely), cwd conventions made explicit (fork-root vs port-root paths),
  Step 1 now mandates the PORT.md sec-7 cross-reference commit, Step 5 sizes the window for
  the predecessor timeout (45-60 min; knob set explicitly), the preflight loopback check uses
  a raw TcpClient (Test-NetConnection overhead false-fails), and Step 4 gained its missing
  STOP-AND-ESCALATE. Settled in-plan decisions: P4b implemented but EXCLUDED from the first
  scale run; straggler TIMEOUT (not <1.0 quorum) is the Step-5 lever; Step 2.4 lean-to-cut.
  RESUME_PROMPT.md now carries the EXECUTION session-jump prompt.
- **Planning deliverable landed (2026-07-13, afternoon)**: `docs/OPUS_EXECUTION_PLAN.md` -
  the step-by-step, supervised execution plan for the six ready backlog items (P4a SDK shared
  HttpClient; fan-out quorum/straggler-timeout robustness; P4b position-report bundling; the
  coa-gpt data memo; the COA-STP1 42-task live scale run; the 6-csproj relative-path
  housekeeping). Each step carries exact files + code specs, build/test commands, acceptance +
  telemetry-gated verification, rollback, and STOP-AND-ESCALATE. Built on the verified inputs in
  docs/PLAN_DERISK_NOTES.md.
- **Latest (2026-07-13, late morning - R10 FAN-OUT LIVE-VERIFIED; COA-STP1 UNBLOCKED)**:
  `Vrf:SubordinateFanOut` (opt-in) tasks an aggregate's member ENTITIES directly and
  synthesizes the unit TASKCMPLT when all members complete - the working mitigation for
  path-plan-dead regions. LIVE at Mojave: R9 probe 3/3 (platoon 4/4 members, company
  18/18 via the sub-aggregate recursion in GetAggregateMembers, control), member marches
  telemetry-verified (1.1-1.3 km cohorts). THE MONEY SHOT - COA-STP1's OWN units at its
  OWN location with de-stack + fan-out: **5/7 unit completions (both platoons, BOTH
  companies incl. mega-pile-center B/40, control) where R5c scored 0/6**; the 2 CoHQs
  each finished 3/4 members (one GndV straggler each - fan-out quorum/timeout is the
  follow-up). R11 NEGATIVE + TRAP: DtPlanAndMoveToTask completes VACUOUSLY (no movement)
  at path-dead regions - false TASKCMPLTs; experiment-only. Full records
  UNIT_MOVEMENT_RESEARCH.md sec 4c; evidence docs/experiments/R10_R11_fanout_2026-07-13.txt.
- **2026-07-13, morning - R9 REGION SWAP: GEOGRAPHY CONFIRMED + MECHANISM**:
  the golden unit set transplanted to the COA-STP1 Mojave region (data/R9_Mojave_*.xml,
  ground geometry preserved) FAILS 1/3 (entity only; platoon frozen at 8 m, company
  410 m wrong-way then frozen) while the same-day SWEDEN CONTROL (original golden
  files, same code, same 20x) completes 3/3 in ~4 min - so neither code drift nor the
  multiplier is a factor. MECHANISM (vrfSim.log): at Mojave the backend logs
  `moveAlong() - empty route -- not sending move along to subordinate` and creates
  ZERO member Offset Routes (Sweden: 45) - unit leader-path planning returns EMPTY at
  that location on the whole-earth online terrain. NOT an interface defect. The
  practical unlock candidates are R10 subordinate fan-out (entity moves are PROVEN at
  Mojave) and an R11 DtPlanAndMoveToTask probe; coa-gpt feedback item #4 = validate
  the region before generating COAs there. Full record UNIT_MOVEMENT_RESEARCH.md
  sec 4c; evidence docs/experiments/R9_region_swap_2026-07-13.txt.
- **2026-07-12/13, night - R8 LIVE-VERIFIED; STACK HYPOTHESIS FALSIFIED**:
  the R8 live A/B ran (exact R5c probe, only `Vrf:DeStackCreates=true` toggled; full
  record UNIT_MOVEMENT_RESEARCH.md sec 4b). R8 WORKS (54-unit mega-pile spread; entity
  control completed 4x faster ~3.5 min vs ~13; CoHQ creation now CLEAN - no creation
  scatter) - recommended ON for stacked scenarios - BUT still 0/6 aggregates marched:
  companies DRIVE away 31-124 km past their 1.1 km routes (the E1 runaway re-expressed;
  R5c's "runaway eliminated" was mega-pile GRIDLOCK suppression, not a fix), CoHQs
  scatter 76-93 km ON TASKING (member warp), platoons shuffle ~60 m. Stacked
  coordinates are FALSIFIED as the sufficient blocker; the surviving hypothesis is
  GEOGRAPHY/terrain content at the Mojave region (both regions run the same whole-earth
  "MAK Earth Space (online)" terrain - it is the location content that differs).
  NEXT: R9 region swap (golden R5 unit set at the Mojave coordinates, same probe).
- **2026-07-12, late night - R8 IMPLEMENTED**: opt-in
  create-time de-stacking BUILT + OFFLINE-VERIFIED (UNIT_MOVEMENT_RESEARCH.md sec 4).
  `Vrf:DeStackCreates` (default off) + `Vrf:DeStackSpacingMeters` (default 50): units
  sharing identical init coordinates spread onto deterministic hex rings before creation
  (first unit keeps its spot; pure `DeStacker.cs`; new `--destack-selftest`, 20 checks;
  `--parse-init` now prints stacked-coordinate groups). KEY OFFLINE FINDING that REFINES
  R5c: the GOLDEN init is ALSO stacked (10 groups, 48/49 creatable units, max pile 13 -
  via the parser's superior-coordinate inheritance cascade) and R5 marched 3/3 OUT OF
  those piles; COA-STP1's distinguishing pathology is its single 54-unit MEGA-pile, so
  the hypothesis refined to pile SIZE - then the live A/B above falsified that too.
- **2026-07-12, night - R5 BREAKTHROUGH**: the research-derived create-time
  sequence SOLVES the stuck-aggregate problem on dispersed scenarios
  (UNIT_MOVEMENT_RESEARCH.md sec 4). With `Vrf:AggregateFormation=auto` the app now, on
  every aggregate creation: QUERIES the unit's own formation list (new
  `RequestAvailableFormations` facade/bridge round-trip), then on the reply SETS a valid
  lowercase name from that list + `ReorganizeAggregate` (new facade/bridge call). R5 live
  on the golden STP init: **3/3 TASKCMPLT** - ArmorPlatoon-type 1222.MechPlt (a type that
  NEVER moved before), ArmorCompany-type 114.MechCoy, and the entity control - with
  `tools/WatchVrf` (new member-telemetry tool) showing clean on-axis marches ending ON the
  final route point. Ground truth: live formation lists are ALL lowercase (static .entity
  analysis misleads - always query); currentFormation reads back '' even when set (trust
  the list). R5c (COA-STP1, stacked coordinates, same code): repair applied 113/113, the
  E1 runaway ELIMINATED, but 0/6 aggregates marched (control-only, ~13 min stack-escape)
  - the same-day A/B pins COA-STP1's STACKED unit coordinates as the blocking data
  pathology (R6 coa-gpt feedback now evidence-backed; REFINED to pile-SIZE by the R8
  offline finding, see the bullet above). CoHQ creation-scatter is a separate open
  failure mode. R8 create-time de-stacking was APPROVED and is now IMPLEMENTED (bullet
  above); the R5c-probe re-run closes or reopens the stack hypothesis. Then CoHQ
  scatter, then E2 re-test.
- **2026-07-12, evening - RESEARCH**: after E1's negative, a three-agent sweep of
  the MAK 5.0.2 vendor docs/headers/content produced **docs/UNIT_MOVEMENT_RESEARCH.md** -
  READ IT before any aggregate work. Verdict: our move task was always correct; the missing
  preconditions are creation-time formation STATE (Aggregate.ope units are born
  uninitialized; members spawn by-formation with offsets 0,0,0), an established LEAD
  subordinate (lead-follow controller + auto-promote OFF; `reorganizeAggregate` is the
  lever), and sane member geometry (set-formation SNAPS a disaggregated unit; stacked
  spawns/working-formation explain the scatter/runaway). Revised plan R1-R7 in that doc
  supersedes the guidance sec 4 ladder from E2 down.
- **2026-07-12, PM - LIVE session**: the P0 fixes were LIVE-VERIFIED via a golden
  re-run (49+4 created, 1.BdeHQ moved + TASKCMPLT with the CORRECT task uuid while a second
  task was in flight - the P0.1 attribution working; clean stop, Solution A deleted all 55
  objects), and guidance **experiment E1 RAN** (per-type formations via
  `Vrf:AggregateFormation=auto` + the de-confounded data/E1_Formation_Order.xml). E1 outcome
  (full record PORT.md sec 10 "E1 RUN"): every per-type name ACCEPTED, entity control moved +
  completed, but NO aggregate route-marched - companies RAN AWAY 150+ km (so the old "Wedge
  moved ~3/32" is now a suspect runaway artifact), platoons shuffled in place, CoHQs were
  subordinate-scattered at creation. Formation names alone are FALSIFIED as the fix; next is
  E1b (repeat on the golden STP init, whose dispersed 14.MechBn genuinely marched) - the
  leading hypothesis is COA-STP1's STACKED/identical unit coordinates (a third coa-gpt
  data-quality item). Also found live: position-report pushing exhausts ephemeral ports
  (bumps the P4 report-bundling priority) and certain server broadcasts truncate at ~2500
  chars (probe orders A2/A; a 9 KB order passes - unexplained, non-blocking). VR-Forces GUI
  (vrfGui) is currently HUNG (backend healthy) - no visual channel until it recovers.
- **2026-07-12 (AM)**: the deep multi-agent review landed `docs/NEXT_SESSION_GUIDANCE.md`
  (READ IT - it corrects several previously "settled" negatives) and its P0 ORCHESTRATION
  FIXES are IMPLEMENTED + offline-verified (all six selftests green): P0.1 per-unit completion
  attribution (`InFlightTracker` - TASKCMPLT names the RIGHT task; superseded tasks' gates stay
  closed), P0.2 predecessor-timeout policy (`Vrf:PredecessorTimeoutPolicy` skip|force|whenIdle,
  default SKIP - no more retask bursts; the completion window runs from predecessor DISPATCH;
  abandoned tasks fail successors fast), P0.3 completion-gated advance-then-engage (fire/breach
  issued when the move COMPLETES; `Vrf:EngageFallbackSeconds`), plus FIFO route-name matching,
  a duplicate-init guard, a loud 0-units-matched-ClientId error, and order-parse warnings.
  KEY CORRECTIONS (guidance sec 2) [the ROOT-CAUSE claim here was SUPERSEDED 2026-07-14 - see the
  top REALITY CHECK: E1 RAN and formation-names alone were FALSIFIED; R5 Vrf:AggregateFormation=auto
  already repairs the birth "column-left"; the real, still-unsolved cause is empty member
  offset-route generation at Mojave]: the 2026-07-12 read was that the stuck-aggregate cause is
  per-unit-type, CASE-INCONSISTENT formation names (aggregates start on invalid "column-left", 128
  vrfSim.log hits). The Unit-4 MoveIntoFormation "RULED OUT" verdict was RETRACTED (confounded
  experiment) and later behavior-verified at Sweden. ALL 42 COA-STP1 tasks self-target, so
  Breach/Escort need SYNTHETIC orders - no COA-STP1 re-run can exercise them.
- **2026-07-11**: two-layer semantic mapping UNDERWAY - Layer-1 verb classifier +
  Unit 3 fires (ATTACK) DONE + live-verified; **Solution A (delete-on-stop) DONE**; **ResetVrf
  (hard reset) DONE + LIVE-VERIFIED** (reaches ORPHANS Solution A misses - and it DID: a live run
  left 2 route/graphic orphans that ResetVrf cleared). **Layer-2 Units 2/4/Reconnoiter/Escort wired
  + built + live-dispatched** (commit faa4398). Unit 4 MoveIntoFormation moved 0 aggregates in its
  run - see the 2026-07-12 correction above (verdict reopened, experiment confounded). Details in
  docs/SEMANTIC_MAPPING.md sec 5 + RUNBOOK sec 8.
- **Phase 1** (C++ facade extraction + rewire): DONE + verified in the C++ repo.
- **Migration**: port products (`bridge-spikes/`, `tools/`, `docs/`+`golden-trace/`)
  COPIED into THIS repo. C++ originals retained pending review, then deletion
  (migration "step 1", DEFERRED). `VrfFacade.{h,cpp}` lives in BOTH deliberately:
  the C++ repo keeps its FROZEN parity copy; the port has its own evolving copy in
  `src/VrfFacade/` (they are MEANT to diverge - parity is the golden trace, not source).
- **Phase 2** (managed bridge `VrfBridge`): DONE + verified. `src/VrfFacade/*.cpp`
  compiles NATIVE + `src/VrfBridge/VrfBridge.cpp` compiles `/clr:netcore` -> `VrfBridge.dll`
  under the full HLA1516e MAK set (0/0). FULL facade surface exposed; the 4 inbound
  callbacks -> managed events (via gcroot thunks). RUNTIME-LOAD SMOKE PASSES: the DLL
  + MAK stack load in-process and the native facade constructs/disposes clean (the
  smoke lives in `src/SmokeTest`). See PHASE2_BRIDGE.md.
- **Phase 3** (the .NET app `src/VrfC2SimApp`): host + BackgroundService wiring the
  C2SIM SDK <-> VrfBridge, full lifecycle + a single-threaded tick-command queue. See APP.md.
- **Phase 4** (parity port of the C++ glue): IN PROGRESS.
  - `OnInitialization` DONE + verified: `InitParser` DESERIALIZES the init into the
    SDK's XSD-generated schema types (C2SIM.Schema102 via ToC2SIMObject) - schema-driven,
    not hand-parsed - and `UnitTranslator` faithfully ports extractC2simInit's dispatch +
    all 11 create* factories. Offline-verified: the STP init -> 80 units, 49 creatable,
    4 areas (matches the golden trace's 49 + 4).
  - `OnOrder` (bare movement) DONE + offline-verified (2026-07-10): `OrderParser`
    deserializes the order via C2SIM.Schema102 (same as InitParser) and `OnOrder` ports
    the bare-movement body of executeTask - resolve taskee (PerformingEntity) -> live
    point 0 -> inline route points -> ROE + parity-no-op SetTarget -> CreateRoute +
    deferred MoveAlongRoute. `--parse-order` matches ALL golden orders. Deferred: reports,
    the two-layer vrftask map, the formation spike, delay/predecessor sequencing.
  - Facade aggregate `TryGetEntityGeodetic` reconcile DONE (2026-07-10): resolves point 0
    from an entity OR an aggregate (aggregateStateRep), so the golden 11.MechBn aggregate-
    move no longer abandons. Builds 0/0; live confirmation pends the run.
  - Reports out DONE + offline-verified (2026-07-10): `ReportBuilder` builds C2SIM
    TaskStatus (TASKCMPLT) + PositionReport bodies via the SDK schema types (serialize,
    not the C++ malformed strings). `OnVrfTaskCompleted`/`OnVrfTextReport` correlate +
    PushReportMessage. `--report-selftest` builds + round-trips both, plus the P4b position
    BUNDLE (16/16). Deferred: health/dedup, TaskCompletionSource/timeout + delay sequencing.
  - Task sequencing DONE + offline-verified (2026-07-10): `TaskSequencer` replaces the C++
    busy-waits with async gating - a task awaits its predecessor (via OnVrfTaskCompleted)
    + start delay before dispatch, with a timeout (fixes the sec-6 infinite wait).
    `--sequencer-selftest` 5/5.
- **Phase 5** (LIVE run vs VR-Forces): DONE + verified (2026-07-10). The .NET port runs the
  FULL golden-trace pipeline live against VR-Forces HLA + a freshly-redeployed c2sim-server
  4.8.4.9: deploy -> HLA join (RTI 4.6.1) -> late-join (49 units + 4 areas) -> order over
  STOMP -> parse -> taskee resolve -> CreateRoute + MoveAlongRoute (entity 1.BdeHQ AND
  disaggregated aggregate 14.MechBn) -> sim runs -> unit MOVES -> COMPLETES -> TASKCMPLT
  report pushed (+ position reports) -> clean stop (no stale federate). Six bugs found+fixed
  LIVE (all in RUNBOOK sec 7): no late-join (JoinSession); parsers assumed <MessageBody>
  root vs the SDK's bare-body live events; empty status body (GetStatus trigger); missing
  Run() (sim clock never started); disaggregated-aggregate geodetic (static_cast fallback);
  RTI-4.6.1/license/cwd/FOM launch env. New tools: `StopIface` (clean stop), `StompProbe`.
- **Aggregate movement (Phase 4+ enrichment)**: `Vrf:AggregateFormation` (opt-in, default
  "" = golden parity) sets a valid formation ("Wedge") before the move. LIVE-VERIFIED:
  14.MechBn - the canonical frozen aggregate - MOVED its full route. COA-STP1 live run
  (128 units, 42-task order, clientId C2SIM) validated the pipeline AT SCALE (0 abandons,
  sequencer gated 32 temporal deps, 32 aggregates formation-set+moved) BUT only ~3 of 32
  aggregates completed - "some move, most stuck": Wedge is NECESSARY but NOT SUFFICIENT for
  the COA-STP1 aggregate types (deeper subordinate/per-type/vrftask condition; PORT.md sec 10).

Net: the port reproduces the full C2SIM<->VR-Forces loop live and moves aggregates. What
remains is quality/parity polish + the two-layer semantic-mapping arc (see "next task" below).

## Repo state (git log is authoritative - do NOT trust pinned hashes in prose)

- THIS repo `VRF_C2SIM` (branch `main`): PUSHED to github.com/hyssostech/VRF_C2SIM
  (current as of 2026-07-12; run `git log --oneline -5` and `git status -sb` for the tip).
  Key history: the 2026-07-12 P0 orchestration fixes + guidance doc sit atop the
  2026-07-11 semantic-map/Solution-A/ResetVrf work (faa4398, e0db15b, 7ee0fa8, 5f34d5b)
  and the Phase 4/5 arc (03e3a09, ff4705c, 8ed890e, 80e4b15).
- The fork `OpenC2SIM.github.io` (`dev/sdk-fixes`): tracks the submodule pointer, bumped +
  PUSHED alongside every port push (push the PORT first - the fork's pointer references it).
- The C++ repo `c2simVRFinterfacev2.36`: **CORRECTED 2026-07-16** - `master` had been carrying
  this project's own Phase 1 facade-extraction commits directly (should have been a branch per
  standing instruction). Fixed: all Phase 1 work (`191933a`..`014dd00`, 18 commits) preserved
  intact on branch `phase1-vrffacade-extraction`, checked out in its own isolated worktree at
  `C:\Users\PauloBarthelmess\Source\Repos\C2SIM\c2simVRFinterfacev2.36-phase1-vrffacade-extraction`
  (HEAD `014dd00`). `master` in the main worktree is now reset to `191933a` ("Baseline: working
  c2simVRF interface (v2.36, golden-trace instrumented)") - the true pre-port, pre-Phase-1
  original interface state. The old Wedge spike still lives on branch
  `spike/aggregate-formation-wedge` (`4f0d00f`, off the old master history, superseded - do not
  merge). An untracked `tools/` directory (orphaned local build output - `bin/`/`obj/` only, no
  source - from a `dotnet build` run against the now-removed Phase-1-added tools) is sitting in
  the main worktree; harmless, not cleaned up pending the user's confirmation. NO GIT REMOTE -
  the only golden-trace rig exists on one disk (private-remote decision pending with the user).
  **If future work needs the Phase 1 facade/VrfFacade C++ code again, it now lives on
  `phase1-vrffacade-extraction`, not `master` - do not assume master has it.**
- The SDK (`dev/sdk-fixes`): `f738edf` (static-state fixes + tests), `3b7cd33` (net10),
  `ae09fd5` (P4a shared HttpClient, ClientLib 4.8.3.3).

`build/` `bin/` `obj/` are gitignored (rebuild them); `docs/golden-trace/*.log` is
force-tracked (parity oracle). `data/` (user-provided post-gold scenarios: COA-STP1,
VRF-All-entities) is now TRACKED (2026-07-12) so the repo is self-contained (offline
--parse-init / --parse-order on the real scenarios; cloud checkouts have them).

## Where everything lives (all in THIS repo)

- `src/VrfFacade/` - port-owned native facade (`VrfFacade.{h,cpp}` + verbatim-MAK
  `remoteControlInit.{h,cxx}`). Carries SetAggregateFormation, `FireAtTarget` (Unit 3),
  and `DeleteObject` (Solution A).
- `src/VrfBridge/` - the `/clr:netcore` managed bridge (wraps VrfFacade; the only managed TU).
- `src/VrfC2SimApp/` - the .NET app (net10 host):
  - `VrfC2SimService.cs` - the interface: SDK events <-> bridge commands/events (init,
    order, reports, late-join, Run, clean-stop, aggregate formation, ATTACK dispatch,
    delete-on-stop cleanup).
  - `InitParser.cs` / `OrderParser.cs` - schema-typed init/order deserialization
    (C2SIM.Schema102; root-robust: envelope OR bare live-event body).
  - `UnitTranslator.cs` - the create* dispatch/factories (pure, verified).
  - `VerbMapping.cs` - Layer-1 semantic-map verb->intent classifier (pure).
  - `ReportBuilder.cs` - schema-typed TASKCMPLT + PositionReport builder (serialize).
  - `TaskSequencer.cs` - async predecessor/delay gating (replaces the C++ busy-waits).
  - `InitModels.cs`, `OrderModels.cs`, `VrfSettings.cs`, `appsettings.json`.
  - offline selftests: `TranslatorSelfTest.cs`, `InitParseCheck.cs`, `OrderParseCheck.cs`,
    `ReportSelfTest.cs`, `SequencerSelfTest.cs`, `VerbMappingSelfTest.cs` (see Run below).
- `bridge-spikes/` - the proven C++/CLI spikes + native probe (the bridge's templates).
- `docs/golden-trace/` - the PARITY ORACLE. `data/` (untracked) - post-gold scenarios.
- `tools/` - helpers. .NET SDK: `PushInit`, `PushOrder`, `ListenReports`, `SdkVerify`,
  `StopIface` (clean stop = STOP+RESET -> UNINITIALIZED), `StompProbe` (subscribe + log
  every inbound event - the STOMP-receive diagnostic). VR-Forces (bridge, no C2SIM):
  `ResetVrf` (hard reset - join, discover EVERY reflected object, DeleteObject each; clears
  ORPHANS from crashes/force-kills that Solution A can't; `--dry-run` = discover-only; RUNBOOK sec 8);
  `WatchVrf` (member-level position telemetry - join as observer, sample EVERY reflected
  object incl. unit members as CSV; the GUI-independent movement oracle; args
  [appNo] [durationSecs] [sampleSecs] [federation]; UNIT_MOVEMENT_RESEARCH.md plan R3).

## Build

Bridge (HLA1516e) with the VS18 (net10-capable) MSBuild - NOT VS2019 BuildTools
(PORT.md sec 3):
```
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
    src\VrfBridge\VrfBridge.vcxproj /p:Configuration=Release /p:Platform=x64 /m
```
-> `src/VrfBridge/build/Release/VrfBridge.dll` (+ Ijwhost.dll).

App (net10): `dotnet build src/VrfC2SimApp -c Release`.

## Run / verify

OFFLINE checks (no VR-Forces; the app exe needs the MAK bin dirs on PATH only because
it loads the bridge assembly for value types):
- `VrfC2SimApp.exe --translator-selftest` - 18-case parity check of UnitTranslator.
- `VrfC2SimApp.exe --parse-init docs\golden-trace\STP-...Initialization.xml STP`
  - expect 80 units, 49 creatable, 4 areas.
- `VrfC2SimApp.exe --parse-order docs\golden-trace\orders\1_VRF_Move_Order.xml`
  - expect 1 MOVE task T1_1_4_A, taskee 670cfe3a..., ROE ROETight, 2 inline points.
- `VrfC2SimApp.exe --report-selftest` - builds + round-trips a TASKCMPLT + a position
  report + the P4b position BUNDLE via the SDK schema types; expect 16/16 checks pass.
- `VrfC2SimApp.exe --sequencer-selftest` - task-start gating (predecessor / delay /
  timeout) + the P0 orchestration fixes (dispatch-relative window, abandon fast-fail,
  in-flight completion attribution); expect 12 checks, ALL CHECKS PASSED.
- `VrfC2SimApp.exe --verb-selftest` - Layer-1 semantic-map verb->intent classification
  (VerbMapping); expect ALL CHECKS PASSED (28+).
- `VrfC2SimApp.exe --destack-selftest` - R8 create-time de-stacking (DeStacker grouping
  + ring geometry + determinism); expect ALL CHECKS PASSED (20 checks).
- `VrfC2SimApp.exe --fanout-selftest` - R10 member-completion aggregation (FanOutTracker) +
  Step-2 completion quorum / straggler timeout / late-straggler swallow; expect ALL CHECKS
  PASSED (36 checks).
Build with `DOTNET_CLI_USE_MSBUILD_SERVER=false ... --disable-build-servers` (concurrent
dotnet builds deadlock the shared build server).
PATH for the exe: `C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6b\bin`.

NOTE the offline PATH above uses `makRti4.6b` (fine - it only LOADS the bridge DLLs). A
LIVE run MUST use `makRti4.6.1` (match VR-Forces' federation RTI) - see RUNBOOK sec 7.

LIVE run - the FULL recipe is RUNBOOK sec 7 (read it; the golden run + COA-STP1 followed
it exactly). In short: redeploy c2sim-server if gone (from Downloads/Docker.zip); launch env
= RTI 4.6.1 on PATH + `MAKLMGRD_LICENSE_FILE` from Machine scope + cwd=`C:\MAK\vrforces5.0.2\bin64`
+ `--contentRoot=<exe dir>` + a FRESH `Vrf__ApplicationNumber`; PushInit FIRST then start the
app (it late-joins); clean-stop with `tools/StopIface`. Useful env knobs: `Vrf__ClientId`
(STP / C2SIM / VRF per the init's SystemName), `Vrf__TimeMultiplier` (e.g. 20 = fast clock),
`Vrf__AggregateFormation` (**auto = the R5-verified query-driven create-time repair,
RECOMMENDED for aggregate scenarios**; a literal name = legacy move-time set for
experiments; "" = golden parity),
`Vrf__TaskPredecessorTimeoutSeconds` (default 600 - make experiment overrides EXPLICIT),
`Vrf__PredecessorTimeoutPolicy` (skip|force|whenIdle; default skip - P0.2),
`Vrf__EngageFallbackSeconds` (default 300 - P0.3), `Vrf__DeStackCreates` (**R8** - true
spreads identically-positioned units onto hex rings at create; default false) +
`Vrf__DeStackSpacingMeters` (default 50), `Vrf__SubordinateFanOut` (**R10** - true fans
an aggregate's along-route move out to its member entities; unit TASKCMPLT synthesized
when all members complete; default false), `Vrf__AggregatePlanAndMove` (**R11** probe -
aggregate moves become waypoint + DtPlanAndMoveToTask; default false). Reload the VR-Forces scenario (or run
tools/ResetVrf) between heavy runs (entities accumulate -> creates stop reflecting).

## The immediate next task

Phase 1-5 are DONE (the port runs the full C2SIM<->VR-Forces loop live + moves aggregates).
Remaining work, roughly by priority (details: docs/APP.md TODO, PORT.md sec 6/10):

1. **Aggregate movement at Mojave** (THE central open problem - the product does not work there):
   Mojave aggregates FREEZE. FALSIFIED as causes (do NOT restart any of these threads):
   2026-07-12 formation-names (E1 RAN, did not fix it - R5 `Vrf:AggregateFormation=auto` already
   repairs birth formation, so formation is not the blocker); nav-data and terrain-page-in
   (2026-07-14); DIS entity type (2026-07-15, tested live twice - see below); "it's purely a
   region effect" (2026-07-15 - COA-STP1's own units freeze at Sweden too). STILL UNSOLVED:
   member OFFSET-ROUTE generation returns EMPTY at Mojave for golden units (0 routes vs Sweden
   45; `moveAlong() - empty route`), AND COA-STP1's own units freeze even at Sweden by a
   different, still-unidentified mechanism - these may be two separate problems, not one.
   INTERIM PROVEN MOVER: `Vrf:SubordinateFanOut` (R10) marches member entities at Mojave,
   bypassing the empty-offset-route path. A `GroundWaypointAltitudeMode=Live` fix was built and
   finally tested live 2026-07-15 but the test was CONFOUNDED (see next-session priority below) -
   not yet a valid result either way. **UPDATE (2026-07-15, later same day, documentation
   research pass, no live run)**: the user-flagged 101 doc (`vrf_createScenario.htm`) plus ~10
   more targeted VR-Forces user-guide pages were read - NEGATIVE result, this is NOT a missed
   basic-setup step (the offset-route/ground-clamp mechanism sits below the User's Guide,
   compiled-implementation only). BUT a NEW, untested, nearly-free-to-check lead surfaced: VR-
   Forces has a documented per-entity "Freeze Movement" property (`vrf_setFreezeMovement.htm`)
   whose symptom ("tasks that require movement PAUSE while set") matches 1.BdeHQ's 2026-07-15
   freeze exactly (confirmed dispatch + running clock + zero displacement) - plus a related
   scenario-level "Send Standard Start/Resume and Stop/Freeze PDUs" setting that gates whether
   EXTERNAL (non-VR-Forces) participants like our app honor run/pause state. See the investigation
   doc's "2026-07-15 (fresh session) - DOCUMENTATION RESEARCH PASS" section for full detail and
   the adversarial caveats. **NEXT SESSION, IN ORDER**: (i) check Freeze Movement / the Start-
   Resume-Stop-Freeze PDU setting on the next live VR-Forces session (cheap - GUI inspection, no
   new appNo needed) before re-running anything; (ii) if that doesn't resolve it, re-run the
   ORIGINAL Fixed100 (parity-default) config against a GUI/vrfLauncher-launched Mojave backend as
   a same-session control - this was never done and is needed before the altitude A/B can mean
   anything (see docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md "2026-07-15
   SESSION SYNTHESIS" for the full reasoning). A MAK support message is NOT yet warranted - see
   the research-pass section for why and what a well-formed question would look like once the
   confound is resolved.
2. **Report-stream parity polish**: EntityHealthStatus (needs the bridge to surface health),
   aggregate-component de-dup, multi-content bundling. Position reports work but are chatty.
3. **Deferred C++-bug fixes** (PORT.md sec 6): distinct C2SimUuid/VrfUuid types (setTarget
   no-op); aggregate health/heading. And the `OnObjectInitialization` STUB (needed only for
   orders that task via a named map-graphic Route, not inline points).
4. **Two-layer semantic mapping** (the big value-add, PORT.md sec 10). Plan +
   re-grounding: **docs/SEMANTIC_MAPPING.md** (the port-grounded plan; supersedes
   TASK_EXPANSION_PLAN.md, whose "first wins" - EMBARK/DEBARK/FOLLOW - are NOT in the real
   orders). Map C2SIM `TaskActionCode` -> the right VR-Forces vrftask (breachTask,
   fireAtTargetTask, moveIntoFormationTask, ...) instead of collapsing every verb to
   moveAlongRoute.
   - **Unit 1 DONE + offline-verified (2026-07-11)**: Layer-1 verb classifier
     `VerbMapping` (VerbMapping.cs) + `--verb-selftest` (28/28). Table grounded on the ACTUAL
     COA-STP1 / VRF-Approved verbs (ATTACK is the most common; only BREACH is native-1:1).
     Confirmed the parser's `TaskActionCode.ToString()` emits the exact table keys (all 17
     COA-STP1 verbs recognized). The executor now CONSULTS the classifier and logs the mapped
     intent + intended composition, but STILL executes bare movement for every verb - ZERO
     behavior/golden-trace change. The port already dissolves the plan's "uuid-resolution
     blocker" (via `_unitByC2SimUuid` -> `_vrfUuidByName`).
   - **Unit 3 (fires/ATTACK) DONE - build + offline + FULL LIVE (2026-07-11), commit 5f34d5b**:
     facade `FireAtTarget` (DtFireAtTargetTask) + bridge + dispatch for ATTACK/DESTRY/FIX/DISRPT/
     PENTRT (resolve affected via TryResolveVrfUuid -> advance -> fire deferred after
     MoveAlongRoute; self-target guard). Live-verified end to end via a SYNTHETIC distinct-target
     order (scratchpad/synthetic_attack_order.xml): "FireAtTarget A -> B after MoveAlongRoute".
     KEY DATA FINDING: COA-STP1 self-targets EVERY attack verb (AffectedEntity==PerformingEntity),
     so the real orders give the fires path no target - a coa-gpt data-quality issue to feed back.
   - **Solution A (delete-on-stop) DONE + LIVE-VERIFIED (2026-07-11), commit 7ee0fa8**: the app
     deletes every object it created on clean-stop (VrfFacade::DeleteObject -> controller->
     deleteObject), so runs SELF-CLEAN - no more manual VR-Forces reloads between runs. Verified:
     164 objects deleted, GUI empty after. Opt-out Vrf:CleanupCreatedOnStop=false.
   - **ResetVrf (hard reset) DONE + LIVE-VERIFIED (2026-07-11)**: `tools/ResetVrf` (Option 1
     delete-all-reflected, file-free) joins the federation, discovers EVERY reflected object via the
     UUID network manager's change callbacks (facade `BeginTrackingReflectedObjects`/
     `GetAllReflectedUuids`), and DeleteObject's each (nil uuids skipped) - reaching ORPHANS Solution A
     cannot. Verified by a controlled discover->delete->re-discover: dry-run left 2 objects intact,
     the real reset deleted them, a fresh federate then discovered 0 (RUNBOOK sec 8). `--dry-run` =
     discover-only.
   - DONE (2026-07-14): task (c) - Units 2/4/5 are wired+built AND behavior-verified at the golden
     SWEDEN control region (apps 3368-3377; see Current status above). COA-STP1 could not exercise
     them (all 42 tasks self-target). This is a COMPONENT / control-region result, NOT a product
     result - the remaining gap is the Mojave aggregate FREEZE (empty offset-route generation, root
     cause unsolved; see next-task #1).
5. **Formal golden-trace message diff** (byte-level parity, not just behavioral).
6. **Housekeeping**: branches are PUSHED (port main + fork dev/sdk-fixes, 2026-07-12; `data/`
   tracked). Remaining: private remote for the C++ repo (USER decision - it is the only
   golden-trace rig, zero off-disk copies); delete the retained C++ originals (migration
   step 1); make the 6 tools/*.csproj SDK ProjectReference paths relative (guidance sec 2.6);
   decouple the SDK ProjectReference (published nuget).

Keep `docs/PORT.md` + `docs/APP.md` current AS you work; after any context compaction
re-read them before deciding anything.
