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
CORRECTION before L2's result (catalog re-read by the humans' OWN .entity files, not by
files that merely mention the type): the deprecated squads' humans are US_Army_M16
(hand-item 'M16AssaultRifle') and US_Army_AT4 ('at4') - explicit hand items too. The
"empty hand item" split is dead. L2 still splits deprecated-vs-current TEMPLATE, but the
stronger candidate is now on the MACHINE: the vendor log warns "DtOsgFileCache could not
load file 'c:/MAK/SharedData/19/latest/ModelData/Vehicles/...'" for model files, and DI-Guy
hand items resolve against DI-Guy character data under SharedData - if that data is absent
or unloadable on this install, determineInitialHandItem dereferences nothing (checked in the
next lines).
CHECKED 2026-09-06 18:57Z: `C:\MAK\SharedData\19\latest\ModelData\Lifeforms\` contains only
`Animals\`, FootPrints.rgb and FootPrints_rgb.meif - NO `DIGuy` directory, which UG52 74.1.1
(p1465) names as the DI-Guy character-data home ("./SharedData/19/latest/ModelData/Lifeforms/
DIGuy and its subdirectories"); `C:\MAK\vrforces5.2d\data\Lifeforms\DIGuy` does not exist
either, while the sim's plugin table reads "vrfDiGuy | 1 | diguy | True" (plugin enabled) and
the FOM carries MAK-DIGuy-7. The vendor log's 200+ "DtOsgFileCache could not load file
.../SharedData/19/latest/ModelData/Vegetation/Trees/*.medf" warnings say the same thing about
the vegetation models: SharedData 19 on this machine is PARTIAL. LEADING CAUSE (machine, not
code): the DI-Guy content package is not installed, the DI-Guy plugin is on, and the first
DI-Guy human's controller resolves its hand item against nothing. Falsifier = L2 running
(then the current templates do not need the data, and the deprecated ones do).
Two corrections to the line above, from the record: (1) "vrfDiGuy | 1 | diguy | True" is a
CONSOLE CHANNEL row (channelSettings.mtl), not a plugin table - DI-Guy is built into
vrfmodel.dll, there is nothing to unload; (2) the ROOT is a 2026-09-02 install ruling
(memory vrf-52d-package-and-data): makData19 was installed as the MINIMAL set - Core +
Terrain 1-4 + TestTerrain - with "DI-Guy-20260108 (6.33 GB, GUI lifeform visuals, optional)"
deliberately left out. That ruling was wrong for the headless sim: the sim engine itself
needs the DI-Guy character data the moment a DI-Guy human ticks, GUI or not. On 5.0.2 the
same ruling never bit because the parity types were all vehicles. The fix is a DATA
INSTALL (the makData19 DI-Guy package, user action: SharePoint download + the data
installer as on 2026-09-02), not code; the interim alternative is a type-map policy that
maps lifeform rows to vehicle-only proxies, a fidelity regression that needs the user's
ruling. L2's result decides nothing about the fix any more - it only measures whether the
current templates hit the same frame (expected: yes).
RUN L2 RESULT - 20260906T185519Z: CRASH, prediction (b) - the current "Infantry Platoon (USA
Army)" template (every human with an explicit hand item) dies the same way: 0 ObjectCreated,
new .dmp for the sim's pid 122232, the 5 compositions expired with nothing ever created.
Three runs, three templates (mortar/FA section, deprecated mech platoon, current infantry
platoon), one frame. Adversarial review: the competing hypothesis "template-specific data
defect" is refuted by L2; "any DI-Guy human" + "DI-Guy data absent" stands. What would
falsify the data-absence cause now is a crash AFTER the package is installed - that is the
next observation, and it is the user's install.

### 8. PROBE S3 (registered): the scale measurement WITHOUT lifeforms, repo map untouched
Purpose: the numbers run 1/2 could not give (composed companies at scale, BN CP proxies,
sub-routes, sim/wall ratio, R1 at 128) while the DI-Guy package is pending. Mechanism: the
runner's `Vrf__TypeMapFile` points at a SCRATCH copy of the 5.2 map (outside the repo:
scratchpad unit-type-map-52-nolifeform.json) in which the 23 rows whose template carries a
DI-Guy human are redirected to same-side vehicle-only proxies (aggregates -> Tank Platoon
(USA)/(RUS); teams -> M577A2 / MT-LBu command vehicles), verified by walking every row's
template to zero humans. This is a PROBE: it changes no repo file, states no fidelity policy,
and its results are labelled "no-lifeform proxy map". Config otherwise = run 2 (FidelityTable,
activity deadline, terrain 30 s, console 4 on our objects, R1 10 s, -ClientId C2SIM, 2700 s).
PREDICTIONS: no crash; 128 created (mode line FidelityTable, 123 rows from the scratch path);
EXPAND for the pure higher-unit companies (Tank Company USA/RUS, Tank Breach, ~19) with 0
"never created" (the activity-based deadline's first scale test); 26 BN CP proxies created;
the 11 taskees' units move (companies via offset routes - subRoutes > 0 for tasked companies;
CP proxies via the platoon-class sysdef); 128 in every R1 round; sim_ratio.py returns a
ratio (samples from the composed platoons' task lines). A crash here would be a NEW class
(no lifeforms are created) and a STOP.
S3 RESULT - 20260906T190427Z: CRASHED, same frame - and NOT a new class: the app log shows
4 units still planned as "Mechanized Platoon (USA) IFV (Deprecated)" (rows F-UCIZ-D/E), the
very template that killed L1. My template walker had scanned EntityLevel\vrfSim only; that
platoon's squads ("Infantry Squad (USA) (Deprecated)", 11:1:225:13:4:0:0) live in the BASE
SMS (simulationModelSets\base\vrfSim, included by EntityLevel.sms), so the walk saw no humans
and left those rows alone. Instrument, not sim. The runner's new liveness check fired during
the gate wait ("BACK-END pid 133136 CRASHED and is PARKED ... Crash record: ...133136.
callstack.log") - the first crash reported for the right reason. The walker's real defect was
its RESOLUTION, not its directory list: it took the FIRST matchType hit (Ground_Aggregate's
wide wildcard) instead of the MOST SPECIFIC one, as the sim (and ObjectTypeResolver.Score)
does; "Infantry Squad (USA) (Deprecated)" publishes objectType 11:1:225:13:3:0:1 with
matchType 11:1:-1:13:-1:-1:-1 and is reached only by specificity scoring. Fixed walk
(EntityLevel + base, most-specific match): the deprecated USA mech platoon now shows 4
humans, 25 rows are redirected (F-UCIZ-D/E added), zero rows or proxy targets carry a human.
S4 (190900Z-ish) = S3 re-run on that v3 map; a crash there would again be instrument-first
(re-walk), sim-second.

### 9. S4 RESULT - 20260906T191037Z (v3 scratch map): the first COA-STP1 run on 5.2 to
survive its window. Runner exit 0, 2,700 s wall.
CREATION (all predictions met): 234 creates (128 + 106 synthesized); 29 EXPAND, 25 MIXED-
template lines, 0 "(Deprecated)"; 29 of 29 compositions attached (20 x 4/4, 9 x 3/3), 0
expired - the activity-based deadline's first scale pass; 279 ObjectCreated; no crash.
Census: everReal 1,333 (vehicle-only proxies are lighter than the 5.0.2 tank-company
templates' 1,732). R1: 276 rounds, 127 sent / 1 skipped, 34,671 position reports for 127
uuids (one unit never reflected - identified below).
RATIO: sim_ratio.py 0.729x (44 samples, wall 46-1,942 s, sim 12-1,389 s, resid sd 2.7 s) -
2.7x the 5.0.2 figure (0.27x) at the same 128-unit load and the same FFRTC fixture.
MOVEMENT (the open part): 9 first-wave tasks issued, 0 completed within 1,389 sim-s; taskee
net displacement 0.09-0.87 km (two 0.00, the same two as on 5.0.2). Sub-routes: only C/1-35
(4). The consoles, unit by unit (level 4 on our objects):
- C/1-35 (composed Tank Company, HQ1 + TANK2-4): the gate OPENED - "task complete msg rcvd"
  from TANK3 at sim 92 s, TANK4 153 s, TANK2 185 s, HQ1 323 s, "Move into formation
  complete" at 323.5 s, offset routes issued, "sub C/1-35.TANK4 completed move along task" at
  sim 1,388.6 s (the window's end). The company mechanism of run C reproduces at scale; the
  HQ section (Tank HQ Section template, 2 HMMWVs) is the slow subordinate, 3.5x the platoons.
- 1-1/2/1_AD, B/5-20, 856/HHC (Tank Platoon proxies): maneuver-along running - their consoles
  count their tanks down ("Subs still moving: 3" at t=312, "2" at t=423) between weapon-list
  responses; the tanks ARE moving. Population-wide only 39 of 1,333 entities moved > 100 m and
  12 moved > 1 km (max 3.2 km), and EVERY one of the 12 was born at (34.6802,-116.7243) - the
  54-unit STACK the parse-check flags ("54 units at 34.679985,-116.724799"), i.e. the R8
  create-time de-stacking pathology (UNIT_MOVEMENT_RESEARCH sec 4: identical spawn coordinates
  gridlock disaggregated-unit geometry; Vrf:DeStackCreates is the existing opt-in lever). At
  1-3 km in 23 sim-min the movers crawl at ~1-2 m/s; 5.2's obstacle avoidance is ON by
  default (Migration Guide 2.4), which a jam of hundreds of co-located vehicles exercises.
  Probe S5 (sec 10) tests that lever, one variable.
- 1-35/2/1_A, 4-27/2/1_A, 40/2/1_AD, 5-20/2/1_A, 1-6/2/1_AD (BN -> Tank Headquarters Section
  CP proxies): "maneuver-along" started at sim 12 s and NOTHING after it for 1,376 sim-s -
  9-12 rows total, no subordinate commands; net 0.39-0.45 km; four sim-created HMMWVs warned
  "Entity not embarked on same object as target" at t=49.5 s. The Tank HQ Section template
  does NOT move as a tasked unit on 5.2 (the same HMMWV pair the C1b company waited on).
NEXT (movement, console-first): (a) the crawl - probe S5 below; (b) the CP proxy stall - one
CP unit alone with MEMBER console level 4 (data/L3_CpProxy_Initialization.xml, R9 fixture,
10-min run); (c) the BN->HQ-section proxy is a type-map question for the user once (b) is read.

### 10. PROBE S5 (registered): S4 + Vrf__DeStackCreates=true (one variable)
The R8 lever spreads units that share identical create coordinates onto deterministic hex
rings (DeStacker.cs, --destack-selftest; DeStackSpacingMeters 50) BEFORE creation; the 54-unit
stack at (34.679985,-116.724799) becomes 54 slots ~50 m apart. Everything else = S4 (v3 scratch
map, FidelityTable, terrain 30 s, console 4, R1 10 s, 2,700 s).
WITHDRAWN 2026-09-06 20:1xZ, minutes after launch (run 200343Z stopped at Stage 3b, its
back-end stopped with StopVrf52, RTI untouched) - the user asked whether this was "the
de-stacking BS you ruled out ages ago". It was. The record I did not re-read: START_HERE
"2026-07-12/13 night - R8 LIVE-VERIFIED; STACK HYPOTHESIS FALSIFIED"; UNIT_MOVEMENT_RESEARCH
sec 4b (same); ANALYSIS_COASTP1_RUNG1_FREEZE H3 "physical boxing-in on the de-stacked pile:
REFUTED"; and the 5.0.2 rung-2 run itself moved 6-7 km from this same 54-unit stack. The
"evidence" I used - the 12 movers were all born at the stack point - is trivial: the tasked
units live there. Sec 9's crawl stays OPEN with NO mechanism claimed. Correct next split
(C11, console-first): L3 = the CP proxy alone with MEMBER console level 4 (10 min, R9
fixture); S6 = S4 with Vrf__ObjectConsoleMemberNotifyLevel=3 so the TASKED units' member
tanks narrate their own move (planner outcomes, speed, blockage) at scale.

### 11. L3 RESULT - 20260906T201623Z (one CP proxy alone, member consoles at 4): IT MOVES
1222.MechPlt as BN -> F-UCI-F "Tank Headquarters Section (USA)" (M1A2 x2, M3, HMMWV x2,
M577), tasked with the R9 platoon route: move-along-controller -> maneuver-along at sim
94.7 s, "Completed" at sim 275.3 s; app "VRF task complete: 1222.MechPlt~PXY / move-along".
All six members: maneuver-in-formation -> ground-vehicle-move-to -> move_to_position_and_
heading -> turn-to-heading, every subtask Completed, 0 warnings (HMMWV 1 and 2 included -
no "not embarked" line this time). So the template is NOT intrinsically unusable as a tasked
unit; the five CP proxies that sat silent for 1,376 sim-s in S4 stalled for a reason that
exists at scale (1,333 objects) and not on the 6-unit fixture. Mechanism NOT claimed.
S6 (next) puts the member consoles on the tasked units at scale, which is the only channel
that can say what a tank at S4's load was waiting for.
L3 also completed 3/3 under FidelityTable (1.BdeHQ -> M577A2 proxy entity; 114.MechCoy
composed 3/3 from Tank Platoon (USA) children; the CP proxy), 16-entity cluster, spread
556 m, 0 dropped - the TYPE-MAP LIVE GATE's P3 (mode took effect), P4 (proxies surface:
"~PXY" markings + TYPE MAP Proxy lines), P5 (tasking resolves, units move under proxies)
and P7 (hygiene) PASS on the small fixture. P2 (no generic/empty unit) holds for the units
created. The gate at scale is what S4/S6 measure.

### 12. S6 - 20260906T202713Z, first 2 minutes (members of the 9 tasked units at console 3)
Read, not inferred (the tanks' own lines):
- Every member plans its path ("Job Plan path success", "Planned path has 1 parts") and then
  "Saving ordered speed 3 mps" - 35 of 35 in S6 AND 19 of 19 in L3. 3 m/s is the platform's
  off-road ordered speed here; L3 completed its 900 m route at ~3.3 m/s, so 3 m/s alone is
  NOT the crawl. The crawl is that S4's movers made 0.7-2 m/s effective and the aggregates
  0.1-0.9 km.
- 28 "Entity not embarked on same object as target [Route N]. Ending task" failures in S6's
  first ~2 min (tanks, not only HMMWVs: M1A2 96/98/52/54/10 ...), each followed by "Attempt
  Global Replanning Once / Global Replan / Loop to stall for replanning". 0 in L3. EVERY
  failing member was born at the 54-unit spawn stack with 54 other objects at 0.0 m. The
  message is DtGroundAutoControllerComponent::decideToGiveUpTask ("if the entity is ATTACHED
  and the target of the task is not"); UG52 14.3 (p382) documents placement that
  "automatically embark[s] simulation objects" when placed on one; the M998 template carries
  can-embark / can-be-embarked-upon True.
DISSENT LINE (per the anchor rule; NOT a probe, NOT a reopening): the R8 ruling (2026-07-13,
5.0.2: de-stacking did not make aggregates march) stands. NEW evidence for the user's
judgement: on 5.2d the sim itself reports co-located births as ATTACHED entities whose moves
FAIL and stall in replanning - a mechanism the 5.0.2 A/B never showed (its units moved 6-7 km
from the same stack). Whether that reopens create-time de-stacking FOR 5.2 is the user's call.
Falsifier for the attachment reading: a "not embarked" failure on a member NOT born co-located
(none in the first 28), or vendor evidence that the message means something else.
VENDOR READING (2026-09-06 21:0xZ, after the user asked for it) - the "attachment" reading is
NOT backed:
- UG52 15.1.1 "Automatic embarkation" is a GUI CREATE-PANEL option ("Automatic Embarkation
  check box", "Embark in Slot if Available", a green box when the cursor is over a host,
  p409). Nothing in the docs says the SIM embarks an object because of where it is created.
- vrfRemoteController.h: `attachTo` / `keepExistingAttachment` belong to OVERLAY objects
  (createOverlayObject :1177, sendVrfOverlayObjectModifyMsg :1215); `ComponentAttachmentRule`
  is a loadScenario parameter (:538). Our createEntity/createAggregate (VrfFacade.cpp:697/707)
  carry no embark or attach argument. Not an API misuse on our side.
- Shipped Lua ground-vehicle-move-to.lua:11-16, 134-155: "If the entity is embarked, the task
  will not do any path planning ... If entity is embarked, attaches route to same parent
  (set-attached attach_to = parent)". The C++ give-up text compares the ENTITY's attachment
  with the TARGET route's. Which of the two is attached in S6 is NOT observable on the console.
- COUNTER-EVIDENCE already in the record: every R9 run creates 114.MechCoy's three platoons at
  IDENTICAL coordinates (terrain reply #1/#2/#4 all 34.64763,-116.69339) and none of their 12
  tanks has ever printed "not embarked"; co-located birth alone is therefore NOT sufficient.
- Ordered speed 3 m/s: move_to_position_and_heading.lua "Save Ordered Speed" merely SAVES
  this:getOrderedSpeed() before disabling navigation; the value is set upstream by the
  maneuver-in-formation controller (C++), identical in L3, so it is the vendor's formation-
  approach speed, not a defect.
- Web: nothing indexed for the message text.
S6 OUTCOME (run 20260906T202713Z, completed 21:2xZ, runner exit 0, no crash): ratio 0.437x
(S4 0.73x; the member consoles at level 3 on the tasked units are the added load); TASKCMPLT 1
(856/HHC~PXY move-along - one CP proxy completed at scale, which S4 never did); moved > 1 km:
none; member-console signatures over the whole run: "not embarked on same object" 38, "Loop to
stall for replanning" 176, "Global Replan" 509, "Job Plan path success" 472, "Saving ordered
speed" 50. Per-unit distances not re-computed (score_run's taskee join found 0 uuids on both
S4 and S6 - the order names taskees by UUID; instrument gap, noted, not fixed tonight).
FRAME FINDING THE SAME EVENING (user-led, 21:00-21:40Z): COA-STP1 is an aggregate-level
scenario (128 units, 0 entities, 64 COY / 26 BN / 23 PLT / 12 none / 2 SECT / 1 BDE; every
SISO field zero) run in EntityLevel.sms with every type-map row on an EntityLevel container
template - the 5.0.2 oracle's approach, against our own Y-15 ruling (2026-09-03: battalion+
or crawl-scale entity counts -> AggregateTacticalLevel). The same aggregate code
11:1:225:5:2:0:0 is "Tank Company (USA)" (6 subordinate entity types) in EntityLevel and
"Tank CO (USA, M1A2)" (0 subordinates, a leaf unit) in AggregateTacticalLevel: codes pick the
template, the SMS picks the atom. No further EntityLevel scale run is registered; the next
object is the user's ruling on applying Y-15 to COA-STP1.
USER CORRECTION 2026-09-06 ~21:50Z (supersedes the scale framing above): "You are overblowing
the coa-stp1 scenario. There are just 11 taskees. These are the only ones that need to be
simulated. You are confusing the ORBAT for the whole Corps, likely, with the specific elements
that are part of the COA proper." VERIFIED against the files: the order's 42 tasks reference
exactly 11 units (PerformingEntity), and every AffectedEntity is the performing unit itself -
no other unit in the init is referenced by any task. The 11 = 5 BN (1-35/2/1_A, 4-27/2/1_A,
1-6/2/1_AD, 40/2/1_AD, 5-20/2/1_A), 4 COY (856/HHC, B/5-20, C/1-35, 510/40), 2 without
echelon (A/6-56/HHC, 1-1/2/1_AD); all 11 born at the one 54-unit coordinate; none has a
declared subordinate in the init (flat). The other 117 units are ORBAT context that no order
touches, and 43 of the 54 units on that coordinate are context. EVERY COA-STP1 run since
2026-07 (5.0.2 and 5.2) created all 128 and expanded them to 1,333-1,732 entities - the
oracle's behaviour, carried forward unexamined. The scale problem is therefore mostly
self-inflicted: the COA proper is 11 units. The Y-15 model-set question survives only as a
FIDELITY question for the 5 battalions (no EntityLevel template -> CP proxy today), no longer
as a survival question. Policy to rule: register the whole ORBAT from the init, simulate only
the units the orders reference (materialize at order time), with context units either absent
or as empty organizational shells for the GUI audience.
STATUS: the S6 failures are an OBSERVATION with no vendor-backed mechanism. The one
observation that would decide it is the EMBARKATION STATE of a failing tank read from the
sim (UG52 15.1: embarkation is published over RPR FOM 2 - the Embarkation View / the object's
Information dialog / the FOM attribute), which no current instrument reads. No probe is
registered on this until that is read. The R8 ruling stands; the dissent line above is the
only record of the S6 failures.

- Run 2 is also the TYPE-MAP LIVE GATE on 5.2 (PREREG_TYPEMAP_LIVE_GATE_2026-09-02, registered
  for 5.0.2 and never run there): its safety properties apply verbatim - P2 no generic / empty
  unit created (a Country-0 or zero-subordinate abstract is a STOP); P3 the mode line proves
  the mode took effect; P4 every PROXY substitution surfaces (the substitution report / marking
  tag) - the audit expects 100 PROXY units; P5 tasking still resolves and the units still move
  under proxies; P7 hygiene. The 5.2 vendor log carries no creation lines (REBASELINE sec 6
  item 3), so the gate's "six creation lines" evidence comes from the console echoes + the app's
  PLACEMENT lines + the trace instead. The table is NOT adjusted to fit a miss.
