# Scenario setup guide - running coa-gpt scenarios in VR-Forces at arbitrary regions

Living config guide (started 2026-07-14). Captures WHAT MUST BE SET UP to run a coa-gpt /
Tropic-Tortoise-style C2SIM scenario at a new geographic region in VR-Forces, and WHY. Update
as findings land. ASCII-only.

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
2. **Navigation data (nav mesh)** generated over the AO - REQUIRED for aggregate lead-follow
   leader-path planning. VR-Forces: entities use STANDARD navigation (work without a mesh, so
   they march anywhere), but the AGGREGATE leader-path uses ADVANCED navigation (NavMesh), which
   needs generated nav data for the area. This is why entities march at un-nav-data'd regions
   but aggregates FREEZE. [OUTCOME OF THE NAV-DATA FIX: PENDING the R9 re-test 2026-07-14.]
   Workflow (GUI, front-end): Settings > Terrain > Navigation Areas > New:
   - **Coordinate format is DEGREES:MINUTES** (deg:decimal-min), NOT decimal degrees. A decimal
     like 34.56 deg is entered as `34:33.6` (0.56*60 = 33.6'); 116.74 W as `116:44.4`. Entering
     "116.74" reads as 116 deg 74 min = INVALID (>59') and disables Create.
   - Rectangular, N/S-aligned, <= 20x20 km at default raster precision (0.2 m). Sectorize (auto)
     for large areas. Prune No-Go on.
   - **Profile: ground-platform** (vehicles). AVOID lifeform (cover-point calc = hours to days).
   - Create -> select the area in the list -> **Generate** (prompts to save terrain -> Yes) ->
     wait (progress dialog; streaming terrain pages the box in first) -> **File > Terrain > Save
     Terrain** -> **close & reopen the scenario** (back-end loads nav data only on reload).
   - Nav data saves to `userData/navData/<terrain>/NavArea-...`. REUSABLE once generated.
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
  R9 at 49 units also froze; nav-data is the lead cause).
- MITIGATION (feature TODO): a "lean creation" mode - create only order-referenced units (+ their
  affected targets), ~22 vs 128 -> ~7x fewer objects -> much lighter pager/sim load. Realistic
  (do not spawn 117 idle units for a movement run).

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
- Aggregate leader-path = ADVANCED nav (NavMesh) -> per-region nav data required. Entities =
  STANDARD nav -> march without it. (Explains entities-move / aggregates-freeze at Mojave.)
- Terrain Page-In Area alone does NOT fix aggregate movement (falsified 2026-07-14).
- Fort Irwin/NTC is a stock MAK terrain; the Tropic Tortoise AO (34.68,-116.72) is ~65 km S of
  the NTC core, in GENERIC Mojave, on no curated/nav-data'd terrain - hence the need to generate
  nav data there.
