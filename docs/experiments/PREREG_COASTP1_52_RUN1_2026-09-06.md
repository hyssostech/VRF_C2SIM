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

## 4. Results
(pending)
