# PREREG COA-STP1 on VR-Forces 5.2 - run 1 (a MEASUREMENT, rung-1 style)

Date: 2026-09-06. Tier: HEAVY (first scale run on 5.2; every number below is a prediction
written before the launch; a missed HIGH prediction is a STOP, not an adjustment).
Plan lineage: velvet-tickling-hamster Phase 3 ("one unmodified full-order run; census; company
sub-route table vs the 5.0.2 runs"). 5.0.2 record: PREREG_COASTP1_RUNG2 / QPAIR (sec 9 adjudicated:
Tank-Company non-determinism SUPPORTED on 5.0.2; -q not a cause).

## 1. Inputs (read from the files, not recalled)
- data/COA-STP1_Initialization.xml: 128 units, ALL C2SIM SystemName, hostility HO 67 / FR 61,
  echelons COY 64 / BN 26 / PLT 23 / NOS 12 / SECT 2 / BDE 1; the ORBAT is FLAT - zero
  <Subordinate> lists, zero <Superior> links (python over the file, 2026-09-06). Location:
  Mojave (34.3-34.7 N, 116.5-117.0 W) - the same area as R9, so the empty 5.2 fixture serves.
- data/COA-STP1_Order.xml: 42 tasks, 11 distinct taskees, verbs ATTACK 10, SECURE 4, FIX 3,
  OCCUPY 3, BREACH 3, SCREEN 3, PENTRT 2, BLOCK 2, DESTRY 2, DISRPT 2, DEFEND 2, MOVE 1, ESCRT 1,
  GUARD 1, SEIZE 1, RETAIN 1, CLRLND 1. All 17 are Recognized by VerbMapping; Layer-2 is not
  wired, so every task executes as BARE MOVEMENT (route from the task points) - same as 5.0.2.
- Type map: data/unit-type-map-52.json (123 rows; --typemap-selftest 783 checks). BN rows map to
  the "Tank Headquarters Section (USA)" CP PROXY (fidelity PROXY by design); COY rows to company
  templates that expand-to-compose (C5) into HQ + platoons; PLT rows to platoon templates.
  Coverage of the 128 units (readiness audit wf_1352995f, refuter-checked, 2026-09-06): every
  unit carries SISOEntityType 0.0.0.0.0.0.0, so all resolve by SIDC function + echelon (key b);
  EXACT 28 / PROXY 100 / unmapped-or-pending 0 -> all 128 are created. Shapes: 64 COY -> 7 Tank
  Company (USA) + 6 Tank Company (RUS) + 6 Tank Breach Company + 4 aggregate-Company-HQ-Friendly
  (pure higher-units: EXPAND); 13 FR + 9 HO -> "Mechanized Platoon (USA) IFV (Deprecated)" /
  "(RUS) (Deprecated)" and 6 -> "Mechanized Platoon (USA Army M2)" (MIXED templates: vehicles +
  squads - created as templates by the rule added today, NOT expanded); the rest (CSS platoon,
  infantry platoon, ADA, FA battery, fire-support team) are platoon-class templates. 26 BN -> the
  "Tank Headquarters Section (USA/RUS)" CP PROXY (6 vehicles incl. 2 M998; vehicle subs ->
  template). RISK named before launch: the two "(Deprecated)" platoon templates carry
  gui-can-create=False (22 units) - whether a REMOTE create instantiates them is unverified;
  a failure shows as missing ObjectCreated for those names (P1 counts them).
- Fixture: R9_Mojave_Empty_52 (MAK Earth (online), EntityLevel.sms, FFRTC frame-time 0.033333).
  clientId: `-ClientId C2SIM` (new runner switch; validated against the init's SystemName).

## 2. Configuration (one line = one deliberate choice)
```
pwsh -NoProfile -File scripts\RunC2SimScenario.ps1 -VrfProfile 5.2 -NoGui -Scenario R9_Mojave_Empty_52 `
     -Init data\COA-STP1_Initialization.xml -Order data\COA-STP1_Order.xml -ClientId C2SIM `
     -RunSecs 2700 -SampleSecs 10
env: Vrf__ObjectConsoleNotifyLevel=4 (OUR objects only: units + composed sub-units, ~60-200 rows
     each per task); Vrf__ObjectConsoleMemberNotifyLevel unset (-1: members at the vendor default -
     at scale their chatter is gigabytes); Vrf__PositionReportSeconds=10 (R1, if run P passed);
     compose ON by default (N1); no AggregateFormation; no -StopWhenComplete (rule 4 is blind on
     5.2 until R1 is proven - and this run is a fixed-window measurement anyway).
```
Wall: 2700 s cap = the 5.0.2 budget. The 5.2 sim/wall ratio at this scale is UNKNOWN (5.0.2:
0.27x; 5.2 R9 with 6-23 objects: 7.6-10.5x, sim_ratio.py); this run measures it with
`tools/analysis/sim_ratio.py` (frame_gaps.py is blind on 5.2, REBASELINE sec 6 item 6) - a
measurement, not a prediction. Load-bound expectation from the 5.0.2 record: well below 1x.

## 3. Predictions (before the launch; H = high confidence = stop if missed)
- P1 (H) Creation: 128 "PLACEMENT: UNIT" lines; every COY leaf logs "EXPAND coarse leaf ... ->
  N doctrinal sub-units" and "composed - N/N declared children attached"; BN leaves are created
  as HQ-section templates (no expand line); 0 "DROPPING TASK"; 0 unmapped-type failures beyond
  the audit's AuthoredPending list.
- P2 (H) The interface survives the init at scale: app alive at the order push, WatchVrf POS
  count grows through the window, runner exit 0.
- P3 (M) Every tasked COMPANY (the 11 taskees include 856/HHC, B/5-20, C/1-35 ...) shows on its
  own console (level 4) the C1b gate opening: "Disagg mv into form: task complete msg rcvd from
  <each sub-unit>" then "Move into formation complete" and offset routes `<name>_R<n>` on the
  sub-units' consoles (run_census subRoutes: 4 per company whose route has 4 legs; the 5.0.2
  table for 856/HHC, B/5-20, C/1-35 = 4 / 4 / 4). A company whose gate never opens is the T
  signature - its console says why (C11); no mechanism is inferred from positions alone.
- P4 (M) net_km of the 11 taskees from WatchVrf POS over the window: the 5.0.2 "six movers"
  reached 6.5-7.4 km in 2700 s wall at 0.27x; on 5.2 the distance scales with the measured
  ratio - PREDICT: all movers advance along their task bearing, no runaway member > 5 km off
  its unit, no member-vs-unit split (the whole point of compose).
- P5 (H, instrument) run_census.py runs on the 5.2 run dir: everReal > 128, subRoutes non-empty
  for at least one tasked company (console-derived), reports = R1 position reports (if on).
- FALSIFIERS / STOP: P1 or P2 missed -> stop, read the console/vendor log, no code change first;
  a company that stalls with all sub-units reporting complete -> NEW mechanism, console first.

## 4. Results - run 1, 20260906T174427Z (read mid-run at t+5 min; final numbers after teardown)
P1 MISSED, and the prereg itself carried an error. Read from the logs, in order:
1. CREATION IS FAST, NOT SLOW: 128 units + 64 expansions = 369 creates; the sim answered the
   whole batch inside one 10-s window (428 ObjectCreated echoes at wall t=100-110 s; WatchVrf:
   1,732 objects with real coordinates at t=144 s - the same population as the 5.0.2 runs).
   The sim's live log holds only cosmetic model-cache warnings.
2. EVERY COMPOSITION EXPIRED ANYWAY (64 x "parent shell ... was never created within 15s"):
   the deadline was set at PLANNING (CompositionTimeoutSeconds=15 from registration), the
   creates left only after the 10-s terrain-profile wait ("Terrain profile request 1 ... got
   no reply within 10 s - creating at the FALLBACK altitudes") and 369 tick-paced issues, so
   every parent's 15 s were spent before its own create had returned. Fixed in code the same
   hour: ExpireCompositions is now activity-based (quiet for CompositionTimeoutSeconds since
   the creates were handed to the bridge AND since the last ObjectCreated).
3. PREREG ERROR (mine): sec 1's coverage line assumed the 5.2 fidelity table, but the table is
   read ONLY under Vrf:TypeMappingMode=FidelityTable (VrfSettings.cs:77; the type-map live
   gate, PREREG_TYPEMAP_LIVE_GATE_2026-09-02, still pending) and the run used the default
   RealTemplates = the 5.0.2 parity dispatch: 64 COY -> Tank Company (USA) (hostile included,
   exactly as the 5.0.2 rung-2 vendor log shows for 3/7159), 26 BN -> 11.1.225.5.20.0.0,
   23 PLT -> Tank Platoon (USA), 15 -> M1A2 entities (`--parse-init` reproduces the four
   shapes offline). So the MIXED-template guard could not fire (0 lines) and the audit's
   per-row targets never applied. Every 5.2 run before this one ran RealTemplates types too;
   the R9 inits (all tanks) could not distinguish the modes.
4. Terrain-profile query timed out at scale (369 points, 10 s) -> fallback altitudes
   (AGL 0 post-create; ground units, so movement is unaffected). Raise to 30 s at scale.
Decision: run 1 continues to its cap as a MEASUREMENT (creation census, sim/wall ratio at
scale, BN/PLT movement under RealTemplates); its P3/P4 verdicts are void for the 64 orphaned
companies. Run 2 is registered below.
FINAL (teardown, runner exit 0): creation census everReal 1,732 (= rung 2 / quiet on 5.0.2),
poleOnly 66; R1 at scale: 282 rounds x 128 units, 35,072 PositionReportContent captured for 128
uuids (reports/net_km instrument live at scale). MOVEMENT: 9 tasks issued (the first wave; 3
successors skipped upstream), 0 completed, 0 TASKCMPLT, all 11 taskees net_km 0.00 over 2,700 s
wall, subRoutes {} - nothing moved. sim_ratio: 3 samples (no task narrative on any console),
so the scale ratio is NOT measured by run 1. The tasked units' consoles are read in sec 4a.

### 4a. The nine tasked units, in the sim's words (console level 4 on the UNITS; members at
the vendor default by design, so no per-member narrative in this run)
All nine received their task at wall t=113 s (sim time 4.47-4.63 s at that moment - the sim
clock was ~110 s wall behind: Run() was queued at init dispatch, ~t=85). Three companies
(B/5-20, 856/HHC, C/1-35 - the empty shells of the expired compositions) started
`move-into-formation` (keep-existing-formation) and then said nothing more for 2,600 s: a
formation with no members never becomes valid (the C1b gate with zero subordinates). Six
BN-echelon units (4-27/2/1_A, 40/2/1_AD, 5-20/2/1_A, 1-35/2/1_A, 1-6/2/1_AD - the RealTemplates
11.1.225.5.20.0.0 aggregates - and 1-1/2/1_AD via `base-system.movement.move-along`, an
ENTITY controller) started `maneuver-along` and then said nothing more either; net 0.00 km.
What their type resolved to in the 5.2 catalog is checked in sec 4b. The platoon-level
sub-unit consoles (the expanded TANK/HQ shells' children) carried ~8,600 "Subordinate ...
maintain-speed" rows through t=575, so the sim clock was running; its RATE at this load is
not measured (no sim-stamped task lines after t=113) - run 2's composed members will stamp it.

### 4b. Why the RealTemplates BN units could not move (read from the catalog, not inferred)
RealTemplates emits 11.1.225.5.20.0.0 for a BN. The only 5.2 EntityLevel template in that
family is `aggregate-Company-HQ-Friendly.entity`, objectType 11:1:225:5:20:1:0 with an EXACT
matchType 11:1:225:5:20:1:0 (no wildcard) - 5:20:0:0 matches nothing, so the sim built a
subordinate-less abstract aggregate: `maneuver-along` on zero members, silent forever. On
5.0.2 (C2simEx.sms) the same type resolved to a populated template; on 5.2 it does not. This
is RealTemplates-specific and moot under FidelityTable (BN -> HQ-section CP proxy), but it is
one more reason the 5.2 profile must run the table: RealTemplates on 5.2 silently creates
EMPTY units for every BN. Recorded for the DEMO defaults (appsettings.Demo.json: FidelityTable).

## 5. Run 2 - registered 2026-09-06 (after run 1's teardown; build carries the fixes)
Config = run 1 + `Vrf__TypeMappingMode=FidelityTable` (the type-map live gate, at last) +
`Vrf__TerrainProfileTimeoutSeconds=30` + the activity-based composition deadline +
IsPureHigherUnit (mixed templates stay templates). Three deliberate changes over run 1, each
named; run 1 is not a baseline for them (its companies were orphaned).
Predictions (H unless marked):
- Mode line "Type-mapping mode = FidelityTable (123 rows from ...unit-type-map-52.json)"; the
  app does NOT refuse to start (a missing/invalid table is fatal by design).
- 128 PLACEMENT lines; shapes per the audit: EXPAND lines for the pure higher-unit companies
  (Tank Company (USA) 7, Tank Company (RUS) 6, Tank Breach Company 6, aggregate-Company-HQ-
  Friendly 4 if its 4 subs are units - else template) = 19-23; "MIXED template" lines = 22
  (the three mech-platoon templates); no EXPAND for BN (HQ-section CP proxies, vehicle subs).
- 0 "was never created within" lines; every expanded parent logs "composed - N/N declared
  children attached" (M); 0 DROPPING TASK from composition.
- 128 units in the R1 rounds ("128 sent, 0 skipped" once all exist); console level 4
  requested for 128 + 4 x (expanded parents) objects.
- P3/P4 of sec 3 apply to run 2 as written; sim/wall ratio measured by sim_ratio.py.
- RISK carried: the "(Deprecated)" platoon templates (gui-can-create=False, 22 units) - a
  remote create that fails shows as those names never reaching ObjectCreated (counted).
### 6. Run 2 result - 20260906T183612Z: THE SIM CRASHED at the first lifeform (STOP)
Runner exit 3 (oracle gate: 761 POS rows, 36 uuids, 0 with a real coordinate). The mode took
effect ("Type-mapping mode = FidelityTable (123 rows ...)", 28 Exact + 100 Proxy TYPE MAP
lines, 33 EXPAND, 61 MIXED-template lines, terrain reply for 255 points inside the 30 s). The
app received 35 ObjectCreated - all 35 CONTROL AREAS - and not one unit. The sim's own crash
record (C:\MAK\logs\vrfSimHLA1516e5.2d-20260906-143636-...callstack.log, sharable; the .dmp
1.3 MB beside it; the main .log holds the environment and is NOT shared):
```
[objects]: DtLocalObjectManager::processCreateVrfObject() : created object named 2MTR/7154~PXY.FIRES2
Error Code - 0xC0000005
0x...: DtDiGuyController::determineInitialHandItem(504) in vrfmodel.dll
0x...: DtDiGuyController::preFirstTickInit(608) in vrfmodel.dll
0x...: DtSimComponent::timedTick(225) ... DtLocalObject::tick(3633) ... DtVrfCallbackQueue::runOne(598)
```
So: the first object whose members are DI-Guy HUMANS (a fires/mortar section synthesized from
the mortar company's template) killed the 5.2d headless sim in the DI-Guy controller's first
tick. Run 1 (RealTemplates: tank companies/platoons only) and every R9 run never created a
lifeform; the fidelity table maps infantry / mortar / CSS / fire-support units to templates
with human crews, so this is the first time our path instantiated one on any build.
Adversarial review: competing hypothesis = a random 5.2 crash unrelated to lifeforms (the
startup crash class of PREREG_52_CRASH_BISECT is --logFileName-triggered and happens BEFORE
READY; this sim was READY, joined, and created 35 areas). The callstack is DI-Guy-specific and
the last created object is a human-crewed sub-unit; a second run that crashes at a different
non-lifeform object would falsify this. Verified: mode, counts, callstack, last object.
Assumed: that ALL lifeform templates trigger it (only the first was reached).
Also found: the runner did not notice the crash for 180 s (it waited for the oracle gate) -
a mid-run back-end liveness check belongs in the runner (LIGHT).
The compositions' "never created within 15s" lines are the correct behaviour of the new
activity-based expiry when NOTHING is ever created (no ObjectCreated to keep them alive).
Mitigation is decided from the docs (DI-Guy options), not guessed - sec 7.

### 7. After the docs: no vrfSim switch disables DI-Guy; isolate first (run L1, registered)
UG52: `--nodiguy` belongs to translationFileCreate.exe (Table 3, p111), NOT to vrfSim; vrfSim
has only `--diGuyAnimationsFile` / `--diGuyCharacterDataFile` (Table 11) and vrfSim.mtl
`diGuyAnimationsFile`; DI-Guy performance settings are GUI display settings (6.16). Per
template: `di-guy-enabled` (the human .entity: True; the platoon aggregate: False) and
`use-random-hand-item-upon-creation` (True on "Infantry Platoon (USA Army)" with an EMPTY
`hand-items-for-random-selection` list; the human "USAR Sergeant M4": False, hand-item
M4A1Carbine). Editing the catalog under C:\MAK is out (prohibited, and a fidelity change).
RUN L1 (one variable vs run E 165015Z): the R9 Lean init under `Vrf__TypeMappingMode=
FidelityTable`. Its SIDCs (SFGPUCIZ---D/E---) map to the deprecated mech-platoon template
(4 IFVs + 3 infantry squads = ~12 DI-Guy humans per platoon), so this is the smallest possible
"remote-created lifeform" test with a known 3/3 movement baseline, console level 4 on, R1 on.
PREDICTIONS: (a) if the crash is general to remote-created lifeforms on 5.2d headless, the
sim dies at the first squad's first tick with the same DtDiGuyController callstack, before any
unit reaches ObjectCreated (runner exit 3, oracle gate); (b) if it runs, the units are created
(mode line FidelityTable; PLACEMENT 6; MIXED-template lines for the platoons) and the
crash is specific to something in run 2's object (the mortar/FA section) - then the next
isolation is that single template. Either outcome is a STOP for the scale run until the
lifeform path is safe; a crash with this minimal reproduction + callstack is MAK-ticket
material (the user's call - the licence is a demo one).
Internet check 2026-09-06 (research bias): docs.mak.com/support lists VR-Forces release notes
up to 5.2 only (no 5.2.1 / 5.3, no "known issues" document); web search for the callstack
symbol returns nothing VR-Forces-related. No vendor fix to cite.

RUN L1 RESULT - 20260906T184832Z: CRASH, prediction (a). Mode FidelityTable took effect; the
four platoons hit "Mechanized Platoon (USA) IFV (Deprecated)" (three via key (a): the init's
declared 3:11:1:225:3:4:0:0 is honoured; 1222 via SIDC), the MIXED-template guard logged all
four; 5 PLACEMENT lines went out, 0 ObjectCreated ever returned, WatchVrf saw 0 real
coordinates, and C:\MAK\logs got a new .dmp + .callstack.log for the sim's pid (119160) with
the IDENTICAL frames: 0xC0000005 in DtDiGuyController::determineInitialHandItem(504) <-
preFirstTickInit(608) <- DtSimComponent::timedTick. So a 6-unit init with ~48 DI-Guy humans
kills the 5.2d headless sim exactly like the 128-unit one: the crash is GENERAL to remote-
created lifeform templates, at least for this template.
Catalog discriminator (read, not inferred): the deprecated chain's squads/fire teams are
"(Deprecated)" templates; the current "Infantry Platoon (USA Army)" chain resolves every human
to a DI-Guy entity with an explicit hand-item (US_Army_M4 'M4A1Carbine', US_Army_M249
'M249SAW', US_Army_M16-M203 'M16_M203', US_Army_M240, US_Army_Javelin 'javelin'; the squad
aggregates carry use-random-hand-item-upon-creation=True with an EMPTY selection list). The
crashing frame is determineInitialHandItem, so "deprecated humans with no resolvable hand
item" vs "any DI-Guy human" is the next split.
RUN L2 (registered): data/L2_Infantry_Initialization.xml = the R9 Lean init with every
SISOEntityType zeroed (so key (a) cannot re-select the deprecated type) and SIDCs
SFGPUCI----D/E--- -> F-UCI-D/E "Infantry Platoon (USA Army)" (EXACT/PROXY), BdeHQ -> F-GEN-H
M577. One variable vs L1: the lifeform TEMPLATE (current vs deprecated). PREDICTIONS:
(a) L2 runs (units created, the four platoons walk the R9 routes, 114.MechCoy composed 3/3)
-> the trigger is the deprecated humans' hand item; FIX = the type map must not reference
"(Deprecated)" templates (rows F-UCIZ-D/E, H-UCIZ-D/E/F, H-GEN-B -> the current equivalents),
a fidelity-neutral change the vendor's own deprecation supports; (b) L2 crashes with the same
frames -> every DI-Guy lifeform crashes the 5.2d headless sim under remote create; then the
lifeform rows need an interim vehicle-only proxy (a fidelity regression = user ruling) and the
minimal repro + callstack + .dmp go to MAK (user's call).

- Run 2 is also the TYPE-MAP LIVE GATE on 5.2 (PREREG_TYPEMAP_LIVE_GATE_2026-09-02, registered
  for 5.0.2 and never run there): its safety properties apply verbatim - P2 no generic / empty
  unit created (a Country-0 or zero-subordinate abstract is a STOP); P3 the mode line proves
  the mode took effect; P4 every PROXY substitution surfaces (the substitution report / marking
  tag) - the audit expects 100 PROXY units; P5 tasking still resolves and the units still move
  under proxies; P7 hygiene. The 5.2 vendor log carries no creation lines (REBASELINE sec 6
  item 3), so the gate's "six creation lines" evidence comes from the console echoes + the app's
  PLACEMENT lines + the trace instead. The table is NOT adjusted to fit a miss.
