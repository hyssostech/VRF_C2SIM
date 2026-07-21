# SESSION-JUMP HANDOFF (2026-07-21)

Paste this into a fresh session. It is the CURRENT truth as of 2026-07-21 evening and
SUPERSEDES the STATE / NEXT ACTION of docs/RESUME_PROMPT.md (which still describes the
pre-baseline "frozen units, run 900 s" plan - now overtaken). Provenance of superseded
claims lives in docs/CORRECTIONS_LOG.md. ASCII only.

Resume in SUPERVISOR MODE: you supervise, gate, adjudicate; executor agents read/analyze/code.
RE-VERIFY every load-bearing claim below with your own hands before trusting it - this handoff
was itself adversarially audited, but a fresh reader must still re-derive numbers from
artifacts (git, run traces, the model-set files), not trust prose.

## WHAT THIS IS (unchanged)
A HEADLESS C2SIM -> VR-Forces interface. A C2SIM init+order goes in; units are created, tasked,
run, and scored from telemetry. ONE command, zero humans in the GUI. GUI use is DIAGNOSTIC ONLY.

## THE BREAKTHROUGH THIS SESSION: a working, zero-C2SIM-code baseline
After many days of tail-chasing, we established "something that actually works":
- Built tools/RunSim (committed e8a56be): a ~180-line file (~130 code lines) modeled on
  tools/SetSimRate that calls the ALREADY-EXPOSED managed VrfBridge.Run() (VrfBridge.cpp:206 -
  controller->run()). NO native rebuild, pure `dotnet build`. It starts the sim clock.
- Ran stock MAK HawaiiGround.scnx fully HEADLESS with ZERO C2SIM code:
  LaunchVrf -Scenario HawaiiGround -> WatchVrf observes -> RunSim starts the clock -> StopVrf.
- RESULT: 22 of 148 objects showed net window displacement 50-1863 m (top clean mover 1862.8 m;
  WatchVrf simTime 3->58 s). POS is dead-reckoned and shows multi-km single-step teleport
  artifacts, so per-unit distances and "route-following" are INDICATIVE, not measured. The
  load-bearing fact: objects STATIC while paused (pre-RunSim trace.csv, every uuid one coord)
  MOVED once RunSim started the clock (post-RunSim trace2.csv, 22 movers). Clean teardown (GUI
  quit carried the back-end; RTI preserved). Artifacts persisted at
  runs/2026-07-21_baseline_hawaii/; writeup docs/experiments/BASELINE_HAWAII_MOVES_2026-07-21.md.
- WHAT IT PROVES: VR-Forces moves authored units; the freeze is NOT a broken sim/observer.
  RunSim and WatchVrf are validated instruments; the load->run->observe pipeline is reusable.

## STATE OF THE FREEZE (what is true, precisely)
- The frozen-unit defect is real BUT its framing changed. Birth altitude is FALSIFIED as the
  current cause: units birth at 10000 MSL with ground-clamp (222134Z_run/vrfc2simapp.log:22-32)
  and clamp to the surface, yet 114.MechCoy + 1.BdeHQ froze bit-exact while 1222.MechPlt moved
  (161438Z_run/watchvrf-trace.csv; CORRECTIONS_LOG "Birth altitude").
- The load-bearing symptom is the WEEK-OLD R9 finding (not new this session): VR-Forces'
  disaggregated-unit LEADER/OFFSET-ROUTE builder returns EMPTY at the Mojave AO
  ("moveAlong() - empty route -- not sending move along to subordinate"; 0 member offset routes
  vs 45 at Sweden, byte-identical terrain/model/code - R9_region_swap_2026-07-13.txt:32-35).
  Documented mechanism: disaggregated move = buildOffsetRoute (pure offset geometry, NOT A*;
  the A* metric is dead code in 5.0.2) + per-vertex ground clamp; a failed clamp drops the
  vertex -> empty offset route; ENTITIES tolerate a failed clamp (entity-works/unit-fails
  asymmetry). Leading region-specific cause candidate: below-terrain waypoint/clamp interaction.
- The "type mapping (leaf Ground_Aggregate vs HigherAggregate) is THE cause" story is
  OVER-CONFIDENT and was retracted same-day: it is a real structural INPUT that selects the
  movement controller, but NOT proven sufficient (R9 logs an empty route for the LEAF platoon
  too, and "1222 moved" rests on the misreporting POS oracle). See FREEZE_ROOTCAUSE_AGGREGATE
  correction section.

## NEXT ACTION: the Mojave region-vs-structure audit (design PENDING - read the CAVEATS)
Question the baseline leaves open: our C2SIM-created units freeze at Mojave; does a
STRUCTURALLY-COMPLETE, AUTHORED aggregate move at Mojave? R9 never controlled for authored-vs-
remote structure. This is the decisive experiment - BUT the design has real confounds (below).
Do NOT run it as a clean 3-way decision without the extra controls.

PLAN (file-surgery, user-chosen 2026-07-21; GUI-author was the alternative, declined):
1. AUTHOR one minimal .scnx fixture: a Tank Platoon (USA) aggregate (object-type 3
   (11 1 225 3 2 0 0)), Disaggregated, 4 M1A2 members (object-type 1 (1 1 225 1 1 3 0)), a short
   route, and its OWN top-level auto-running plan (plan-name = aggregate UUID, single task in the
   top-level Block, NO triggers -> auto-runs). BUILD FROM THE MODEL-SET Tank Platoon (USA) OPD
   (the .entity that ships the 4-M1A2 composition), NOT by cloning a HawaiiGround aggregate -
   none of HawaiiGround's aggregates is a Tank Platoon (3:11:1:225:3:2), so a "clone" would still
   have to rewrite object-type + member set + formation table. PIN THE MODEL SET in the .scnx
   (.sms/.omp): Tank-Platoon-decomposes-to-4-members is model-set dependent; use the SAME model
   set the C2SIM pipeline uses so the test is faithful (baseline HawaiiGround ran EntityLevel.sms).
2. Terrain = "$(SHARED_DATA_DIR)\TerrainData\TerrainConfiguration\MAK Earth Space (online).mtf"
   (the whole-Earth streaming globe the C2SIM pipeline actually uses; NO local Mojave .mtf -
   "Mojave" is a lat/lon ~34.61,-116.60 = ECEF -2353028.662,-4698889.659,3602341.757, re-derived
   to 0.000 m).
3. GATE FIRST: confirm a Tank Platoon (USA) marches at SWEDEN/Bogaland. The Sweden "aggregates
   march" evidence (R9: 45 offset routes vs 0 at Mojave) is for MECHANIZED aggregates generally,
   NOT a Tank Platoon specifically. If the Tank Platoon freezes at Sweden, the Mojave experiment
   is inconclusive - fix the fixture before running Mojave.
4. Then run the SAME file at TWO locations - Sweden (expect movement) and Mojave. Observable: do
   the 4 disaggregated members get offset routes and move?
   - Moves-Sweden / freezes-Mojave => REGION/terrain (empty offset-route is location-specific).
   - Moves-both => authored structure works; BUT this does NOT yet prove our remote-created units
     are structurally deficient - the fixture uses hand-picked ABOVE-TERRAIN waypoints while the
     C2SIM units had order-derived, possibly-below-terrain waypoints. To isolate structure, ALSO
     run the fixture with the SAME order-derived waypoints the C2SIM units had (the confound
     control).
   - Freezes-both => could be a malformed file OR a BENIGN failure: the plan did not auto-start
     (verify plan-name == aggregate UUID and the task is in the top-level Block) OR the built
     state-repository carried non-empty suspended-task-list / task-status-list / baked
     formation-clamp state. Diagnose auto-start binding + state-repo cleanliness BEFORE concluding
     the tank-platoon move is broken.
5. Run via the validated pipeline (ledger appNos -> LaunchVrf -Scenario <fixture> -> WatchVrf ->
   RunSim -> StopVrf). Do NOT touch the port until reality selects the hypothesis.

WHY A POSITIVE CONTROL IS MANDATORY: on Hawaii, only 1 of 16 aggregates moved in the 58 s window
- Convoy 1 (uuid c1ff551e), the only aggregate whose own plan auto-runs an UNTRIGGERED movement
task from its top-level Block; 15 froze at 0.0 m (their member entities also all froze). NOTE: 3
other aggregates (Inf FT 1/2, Inf SQD 1) also own their own plan yet froze - their plans are
trigger-gated (RegisterTrigger + wait). Plan-ownership alone is NOT the discriminator; an
untriggered top-level movement task is.

CAVEAT ON THE PRECEDENT: Convoy 1 moves via task-type "convoy-to-task" (a ROAD-convoy controller
with a control-object + path-plan), NOT the disaggregated "move-along"/buildOffsetRoute path R9
found empty; and a Convoy is non-disaggregatable and convoy-task-only. So "a Hawaii aggregate
moved" is only WEAK evidence that an authored disaggregated MOVE-ALONG aggregate moves - the
fixture mirrors Convoy 1 in plan STRUCTURE only, not mechanism. This is exactly why the Sweden
gate (step 3) is mandatory.

## OPERATIONAL FACTS (this session)
- RunSim/WatchVrf/SetSimRate MUST run with cwd = C:\MAK\vrforces5.0.2\bin64 (they load
  vrfLegion.lua + the FDD relative to cwd). A wrong-cwd RunSim fails with CouldNotOpenFDD and
  BURNS its appNo (that is how 3550 was lost).
- Run pipeline that worked: ledger N fresh appNos -> LaunchVrf.ps1 -Scenario <name>
  -BackendAppNumber a -FrontendAppNumber b (returns when READY, VRF stays up) -> start WatchVrf.exe
  <appNo> <durSecs> <sampleSecs> CWIX-2024 (cwd=bin64, background) -> RunSim.exe <appNo> [mult]
  CWIX-2024 (cwd=bin64) -> analyze trace -> StopVrf.ps1.
- LaunchVrf -Scenario <name> loads userData/scenarios/<name>.scnx (TOP-LEVEL only; not subfolders).
  A fixture we author must be placed there as a top-level .scnx.
- WatchVrf terminated at ~t58 s in the baseline with an unhandled System.IO.IOException (DISK
  FULL) thrown while logging a caught exception (runs/2026-07-21_baseline_hawaii/trace2.err;
  WatchRunner.cs:223) - it still wrote 1635 POS rows first. NOT a .NET SEH and NOT the artillery
  decode (the "Error decoding VRF Object Message type 1" lines are non-fatal warnings). GOTCHA:
  watch free disk on the scratch volume during a run - a fill silently kills the observer.
- Coordinates in .oob/.pln are WGS84 ECEF geocentric XYZ. Mojave (34.61,-116.60,h=0) =
  (-2353028.662, -4698889.659, 3602341.757) - re-derived by the supervisor to 0.000 m error.

## APPNO LEDGER
Marker = 3553 (docs/OPUS_EXECUTION_PLAN.md Appendix B, the single "*** NEXT FREE:" marker).
This session consumed 3547-3552 (3550 BURNED - wrong-cwd RunSim). Every appNo comes from that
marker, ledgered BEFORE any join; unconsumed = burned, never recycled.

## DO NOT RE-CHASE (tried and failed, with evidence in UNIT_MOVEMENT_RESEARCH.md / experiments/)
- SubordinateFanOut (R10): works as a workaround but brings runaways (53.8 km) + vacuous
  completions - not a fix. AggregatePlanAndMove, MoveIntoFormation: opt-in probes, off by default.
- Altitude fix: works for the LEAF platoon only; falsified as the current cause.
- De-stacking (R8), real DIS SISOEntityType swap, NavMesh generation, terrain page-in, route-
  geometry transplant: ALL live-falsified.
- Birth-altitude / underground: falsified for the still-frozen units.

## STILL OPEN
- Region vs structure at Mojave (the NEXT ACTION resolves this).
- The two-channel oracle contradiction: RPT (VRF's own reports) vs POS (WatchVrf dead-reckoned)
  disagree on the ONE moving unit's DIRECTION; unresolved; it shadows every POS-based movement
  claim. (Frozen units agree on both channels, so freezes are solid.)
- Why 15/16 Hawaii aggregates did not move in 58 s (short window; not resolved).
- The AUTHORING fix-half is entirely untried-live (.sogx re-stamp, loadScenario, MSDL import);
  only the ScnxDiff diagnostic is built.

## NON-NEGOTIABLES
- NEVER force-kill a JOINED federate; NEVER kill rtiAssistant / rtiexec / rtiForwarder.
- ASCII only in tracked files (PowerShell byte-scan; grep -P false-cleans on this locale).
- The C++ repo c2simVRFinterfacev2.36 is a FROZEN ORACLE - never develop there. Work in the PORT
  repo VRF_C2SIM (origin hyssostech/VRF_C2SIM, branch main; commit gated green work directly).
- Aggregation-state model (corrected this session): entity-level units CAN disaggregate; an
  AGGREGATED unit models movement only (no combat); to fight you TASK the aggregate then
  DISAGGREGATE it (a runtime STATE switch, e.g. via a disaggregation area). Fighting needs the
  disaggregated STATE, not disaggregated tasking.

## START BY REPORTING
git log --oneline -5 and git status -sb of the PORT repo (HEAD should be e8a56be or later;
clean bar the untracked .code-workspace); the "*** NEXT FREE:" value (expect 3553); a process
inventory. Then build the Mojave fixture per NEXT ACTION and get a go before the live run.
