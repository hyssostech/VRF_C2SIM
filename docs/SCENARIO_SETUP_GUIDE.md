# Scenario setup guide - running coa-gpt scenarios in VR-Forces at arbitrary regions

Living config guide (started 2026-07-14). Captures WHAT MUST BE SET UP to run a coa-gpt /
Tropic-Tortoise-style C2SIM scenario at a new geographic region in VR-Forces, and WHY. Update
as findings land. ASCII-only.

*** THIS GUIDE PREDATES THE HEADLESS RE-GROUNDING (2026-07-18). The product is HEADLESS:
C2SIM in -> run -> verified from telemetry, ZERO humans in the UI (VRF_GROUNDWORK_PLAN.md
sec 1a). ANY GUI WORKFLOW BELOW IS DIAGNOSTIC ONLY and may NEVER be a prerequisite for a
scored run. Where a GUI step is described, look for the headless equivalent first -
e.g. sim rate is set by tools/SetSimRate, NOT by the GUI toolbar. ***

## The stack (fixed)
- **Terrain**: `MAK Earth Space (online).mtf` - a whole-earth STREAMING terrain (VR-TheWorld /
  tile access required). Both Sweden (Bogaland) and Mojave (Tropic Tortoise) use it; the region
  is just a lat/lon on the same terrain.
- **Model set**: `C2simEx.sms` (the C2SIM-tailored set; aggregate-capable). NOT plain
  "Entity Level" (no aggregates) or "Aggregate Level" (lacks C2SIM type mappings). This is what
  Bogaland2 uses and what our Sweden runs proved.
- **Scenario**: a `.scnx` on that terrain + model set (see "Programmatic generation" below).

## Per-region setup checklist (a scenario at a NEW lat/lon)
1. **Terrain Page-In Area** over the AO - forces the streaming terrain to page in (back-end).
   Needed so the terrain surface/features exist there. NOTE: necessary for terrain load, but
   NOT sufficient for aggregate movement (page-in alone was FALSIFIED, 2026-07-14 - see
   docs/experiments/terrain_pagein_investigation_2026-07-14.txt).
2. **Navigation data (nav mesh)** - **FALSIFIED as the aggregate-movement fix (2026-07-14).** DO
   NOT generate nav data expecting it to unfreeze Mojave aggregates. Evidence: Bogaland2 (Sweden)
   and TropicTortoise (Mojave) use the IDENTICAL terrain (`MAK Earth Space (online).mtf`), model
   set (`C2simEx.sms`), and page-in area; NEITHER region has nav data. Sweden aggregates march
   5+ km via the genuine leader-path (SubordinateFanOut OFF), Mojave freezes. Same terrain, same
   code, no nav data either place -> nav data is not the differentiator. The "aggregate leader-path
   needs a NavMesh" theory does not survive the Sweden control (Sweden path-plans with no mesh).
   See docs/experiments/navdata_FALSIFIED_bogaland_vs_tt_2026-07-14.txt. The REAL cause is
   region-specific: member OFFSET-ROUTE generation returns EMPTY at Mojave (0 routes) vs 45 at
   Sweden - a terrain/route-planning failure on the streaming terrain, next-investigation. The
   nav-data GENERATION + LOADING mechanics below are correct VR-Forces facts (kept for reference)
   but are NOT a per-region setup requirement.
   Workflow (GUI, front-end): Settings > Terrain > Navigation Areas > New:
   - **Coordinate format is DEGREES:MINUTES** (deg:decimal-min), NOT decimal degrees. A decimal
     like 34.56 deg is entered as `34:33.6` (0.56*60 = 33.6'); 116.74 W as `116:44.4`. Entering
     "116.74" reads as 116 deg 74 min = INVALID (>59') and disables Create.
   - Rectangular, N/S-aligned, <= 20x20 km at default raster precision (0.2 m). Sectorize (auto).
     Prune No-Go on. **Profile: ground-platform** (vehicles). AVOID lifeform (cover-point calc =
     hours to days). An 18x18 km ground-platform area generated ~80,000 .NavData files in minutes.
   - Create -> select the area in the list -> **Generate** (prompts to save terrain -> Yes) ->
     wait -> **File > Terrain > Save Terrain**.
   - **CRITICAL - getting the mesh onto the BACK-END.** The docs say "close & reopen the scenario"
     loads it on the back-end, but a GUI reopen ALONE may NOT (the back-end keeps its terrain
     loaded from startup and won't re-read nav data - the nav area shows Loaded-on-BE EMPTY and any
     run is a NO-OP, aggregates freeze as if there were no mesh). A **vrfSim RESTART reliably loads
     it** (a fresh terrain load reads the mesh). VERIFY before trusting any run: nav-area info
     dialog (Settings > Terrain > Navigation Areas > select > info) must show **Navigation Status =
     Generated** AND **Loaded on BE(s) = the back-end** (e.g. `1:3001`).
   - **GOTCHA: `Loaded on BE(s): 1:3001 (Disabled)` means LOADED + ACTIVE.** The `(Disabled)` is
     only the Nav-Lab debug-PORT display being off (per vrf_displayInfoAboutNavi.htm; turn on via
     `enableNavigationLabDebugging 1` in vrfSim.mtl). It does NOT mean the nav data is disabled.
   - Nav data saves to `userData/navData/<terrain>/NavArea-...`. REUSABLE once generated.
   - **NAV-DATA TEST - confounds to avoid (learned the hard way 2026-07-14):**
     (a) BE must show Loaded-on-BE (not empty) BEFORE the run - else no-op.
     (b) Use the COMMITTED (clean) app exe - the lean-creation build regressed tasking ("no current
         tasks"), so any run on it is invalid (rebuild from the committed source).
     (c) Fresh sim state - a huge accumulated sim time / stale post-restart state made even the
         ENTITY vacuous-complete its move at 0 m. Restart VR-Forces clean (sim clock reset).
     (d) SANITY CHECK FIRST: the entity 1.BdeHQ must actually MOVE (~1.16 km) before any aggregate
         number means anything. Telemetry (WatchVrf displacement), never completions (R11).
     (e) Sim rate: our Vrf:TimeMultiplier may not survive a restart. Set it REMOTELY with
         `tools/SetSimRate <multiplier> <freshLedgeredAppNo>` (D1 ruling 2026-07-18). The GUI
         toolbar is NOT the mechanism (it also caps at 15). There is NO readback for the
         multiplier: confirm it from the WatchVrf displacement rate - an Nx multiplier shows
         as Nx displacement per real second - NOT by looking at the GUI clock.
         [CORRECTED 2026-07-18: used to say "set the rate in the GUI toolbar (15x) and confirm
         the sim clock is actually ticking" - a human GUI step on the run path.]
3. **ClientId** = the init's SystemName (COA-STP1 = `C2SIM`; golden STP = `STP`).
4. **Vrf:AggregateFormation=auto** (query-driven create-time formation repair; R5-verified).
5. **Vrf:TimeMultiplier=20** (fast clock).
6. **Vrf:DeStackCreates=true** if the init has stacked coords (COA-STP1 has a 54-unit mega-pile).
7. **Vrf:SubordinateFanOut=true** as the entity-level movement FALLBACK where the aggregate
   leader-path fails (entities move via standard nav; R10-proven at Mojave).
8. BREAK-GLASS: setNavigationEnabled(false) = per-entity disable of path planning (direct/blind
   move; the C++ escape). Facade feature TODO; unrealistic, last resort.

## Bloat / lean creation (2026-07-14 measurement)
- COA-STP1: **128 units, only 11 tasked** (8.6%); **~1785 live reflected objects** (scale run),
  ~93% idle. 10 stacked groups incl. a **54-unit mega-pile** at the AO center (34.68,-116.72).
- RISK: the VR-Forces docs name "many objects over a wide area OR fast-moving objects" as what
  DEFEATS predictive terrain paging (-> empty routes) + heavy sim/HLA load at 20x. So bloat
  plausibly AGGRAVATES the paging/nav problem at full scale (NOT proven to be the root cause -
  R9 at 49 units also froze; nav-data-as-cause was FALSIFIED 2026-07-14 (see item 2) - the real
  cause is region-specific EMPTY member offset-route generation at Mojave, 0 routes vs Sweden 45).
- MITIGATION 1 (BUILT, but HELD - live regression): "lean creation" - create only order-referenced
  units on the TASKING side (drop idle friendly context); KEEP the full threat (OPFOR/neutral -
  engages, part of the COP; do NOT thin the threat). Default on. Built + offline-green 2026-07-14
  (defer-creation-to-order design; 78 keep / 50 drop on COA-STP1, all selftests green). BUT it
  REGRESSED live tasking even in eager mode ("no current tasks" - units never received their move
  tasks). STASHED (git stash on the port working tree); needs the deferred-creation lifecycle
  debugged + a live smoke run before it can land. Do NOT run live experiments on that build.
- MITIGATION 2 (candidate, GATED): echelon-dedup for co-located PILES. The app creates a VRF
  object per C2SIM echelon (UnitTranslator maps by SIDC echelon char: platoon/company/battalion
  each -> its own aggregate), and 72/128 units are stacked (incl. the 54-pile). Idea: at a stacked
  location keep the HIGHEST echelon, drop the co-located disaggregated subordinates. CAVEAT: each
  echelon currently gets its OWN default member roster, so dropping subordinates LOSES their forces
  UNLESS they are redundant sub-representations of the kept parent (same physical force at multiple
  command echelons). REQUIRES first determining whether coa-gpt emits redundant command trees
  (dedup-safe) vs distinct per-echelon vehicles (dedup = force/threat loss). Most useful for the
  KEPT hostile threat (shrink the pile without removing the threat). Verify before implementing.

## Programmatic scenario generation (a KEEPER capability)
- `.scnx` is a ZIP of pure-text S-expression parts. Generate a region variant from Bogaland2:
  - The page-in / nav area is a `local-vrf-object` with an ECEF `position` + `orientation-tait-
    bryan` + local `body-vertices`.
  - Ground-flat area orientation at (lat,lon): **yaw = lon - pi, pitch = lat - pi/2, roll = pi**
    (radians; verified by exact round-trip against Bogaland's values).
  - ECEF via WGS84 (a=6378137, f=1/298.257223563). Replace the position/orientation strings in
    the .oob; rename parts; repackage the zip. Example artifact: `data/TropicTortoise_pagein.scnx`.

## Live run recipe (short)
- Preflight (OPUS_EXECUTION_PLAN Appendix A): loopback 127.0.0.1:61613 < 1 s; server
  127.0.0.1:8080 = 200; RTI 4.6.1 on PATH; license from Machine scope; cwd = C:\MAK\vrforces5.0.2\bin64.
- Fresh appNo per join (ledger: Appendix B). PushInit (NO app running) -> WatchVrf (observer) ->
  app (--contentRoot=<exe dir>) -> PushOrder -> observe telemetry -> StopIface (clean).
- Do NOT run ResetVrf when a scenario's page-in / nav area must persist (it deletes reflected
  objects). Telemetry (WatchVrf displacement) is the movement oracle - completions LIE (R11).

## Key findings behind this guide
- NAV DATA is FALSIFIED as the aggregate-movement cause/fix (2026-07-14; see checklist item 2):
  Sweden aggregates leader-path and march 5+ km on the SAME streaming terrain with NO NavMesh
  while Mojave freezes, so the "aggregate leader-path requires a NavMesh" theory is dead. Do NOT
  generate nav data expecting it to unfreeze Mojave (docs/experiments/navdata_FALSIFIED_bogaland_vs_tt_2026-07-14.txt).
- The REAL, still-UNSOLVED cause is region-specific: member OFFSET-ROUTE generation returns EMPTY
  at Mojave (0 routes vs Sweden 45; `moveAlong() - empty route`). Entities march at Mojave via
  STANDARD nav (R10 SubordinateFanOut is the proven interim mover); the aggregate leader-path is
  the specific failure.
- Terrain Page-In Area alone does NOT fix aggregate movement (falsified 2026-07-14).
- Fort Irwin/NTC is a stock MAK terrain and the Tropic Tortoise AO (34.68,-116.72) is ~65 km S of
  the NTC core in GENERIC Mojave - but that is NOT why aggregates freeze (nav data falsified;
  cause is the empty offset-route generation above).
