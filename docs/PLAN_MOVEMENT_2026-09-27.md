# PLAN: ground movement per the vendor's model - two profiles, one pre-flight (2026-09-27)

Ruling: RL-20260927-01 ("Go" on the decision brief of 2026-09-27). Basis: docs/experiments/FINDING_GROUND_MOVEMENT_
PRACTICE_2026-09-27.md (vendor citations, the stops read against them, the aggregate mobility table). Frame: Y-15
(docs/VRF_5.2_DECISION_EVIDENCE.md) - EntityLevel for company-and-below orders, AggregateTacticalLevel for battalion-
and-above; RL-20260920-02 "A and the B sounds right" for Iron Storm. This file is the living handoff for the effort:
status changes here the same turn they happen; a phase that closes collapses to a line plus a pointer.

## 0. The frame in four lines

- Entity level: a LONE PLATFORM is driven by Move To per STP vertex (the vendor's planning task: roads / nav mesh /
  round feature obstacles, with recovery; UG52 23.1-23.2, MG 2.4). Units keep the unit Move Along Route (plans per
  vertex for every member, UG52 30.22). The literal executor (entity Move Along Route) is no longer given to a lone vehicle.
- Aggregate level: Iron Storm's brigades and divisions become aggregate units on the vendor's model set; movement is the
  aggregate Move Along Route on STP's vertices (UG52 35.5.7), governed by the mobility table (road 1.0; hills/cultivated
  0.65; forest/town/mountain 0.25; water/river/alpine 0 = stop). No obstacle avoidance, no nav mesh.
  CORRECTED 2026-09-27 (A1, AGGREGATE_CATALOGUE_2026-09-27): the catalogue has NO simulated (movable) unit above battalion
  for any nation; its BDE/DIV/CORPS entries are CONTAINERS, which the vendor fills with battalion and company units (Road
  to Kaunas: 51 of its 81 simulated units inside 17 containers - corrected 2026-09-27 by the C1 design lane). So a brigade is either (a) a company/battalion-sized simulated
  aggregate standing in for it (PROXY, moves, reports; the first arm) or (b) a container the interface populates with
  sub-units (the vendor's shape; package C1, later). Iron Storm on the map A1 built: 0 EXACT, 40 PROXY.
- One pre-flight, both profiles: STP's vertices and legs are checked against the OSM water and buildings the sim reads;
  a bad vertex is nudged to the nearest clear ground and reported; a wet leg is detoured by the existing shifter or
  reported; a river crossing is reported as an STP authoring defect (a road is the only way across).
- Kept as ruled: stall = report + TASKABRT (RL-20260913-03, RL-20260914-01); completion by the temporary position
  (RL-20260921-09) on telemetry arrival; route shift ON for any run (RL-20260920-01 item 3) as the no-nav-mesh fallback.

## 1. Work packages

| id | what | branch | lane | status 2026-09-27 |
|---|---|---|---|---|
| M1 | Move To per STP vertex for lone platforms; setting Vrf:PlatformMoveToPerVertex (default ON); chain on task completion; vacuous completions logged | feat/movement-moveto-per-vertex | executor (Opus) | MERGED 2026-09-27 (47da73e). Follow-ups M1b: runner report-evidence and the offline analysis tools still key on "MoveAlongRoute issued" lines; a native "continue" flag for intermediate vertices |
| M2 | C# pre-flight port of the OSM water + building readers; vertex nudge + report; per-profile leg rules keyed on Vrf:ModelSet; river report (same water at both band ends, not just any water); drivable road bridges count as dry | feat/preflight-osm-features | executor (Opus) | MERGED 2026-09-27 (0849334) after E1 passed. Findings on the real tiles: T14's destination was dry before and after (e) - the lake is on the LEG; both T14 lines flag for water and end in NO CLEARED LINE (every offset up to 175 m hits the lake chain), so the authored line is dispatched WITH a report on either model set. G1 must warm the OSM tile sets with an ONLINE interface run first (0-byte tiles from the python fetcher read as UNKNOWN) |
| A1 | Aggregate profile prep: .magx survey, data/unit-type-map-52-aggregate.json, fixture IronStorm_Centre_52_Aggregate (not deployed), Vrf:ModelSet switch, G1 draft checklist | feat/aggregate-profile | executor (Opus) | MERGED 2026-09-27 (3e5ab14) |
| D1 | A memberless simulated aggregate counts as ONE position for arrival evidence and the stall watchdog, decided by Vrf:ModelSet (UnitPositionPolicy); EntityLevel unchanged | fix/aggregate-leaf-position | M1 executor, resumed | MERGED 2026-09-27 (b817609). Open (owner's call at G1): a vacuous VENDOR completion of an aggregate's route task still sends TASKCMPLT - the vertex-distance guard could withhold it |
| M1b | Runner report-evidence anchors a chained platform on its LAST vertex; tools/analysis/applog_chain.py is the one parser for the per-vertex lines | with D1 | M1 executor | MERGED 2026-09-27 (b817609) |
| C1 | Populate the containers (RL-20260927-02: "going back to populating the containers is what needs to be gone while we still remember it"): a brigade/division/corps = an Aggregate Container (UG52 72.2.1 p1419) holding warfare-model units, recursively down to units the catalogue simulates. Composition sources, in order: the STP TO (corps -> division -> brigade: 15 Superior relations; 4 battalions), the catalogue's configured sub-units (few), then an authored composition table per unit type (DESIGN_ORBAT_TO_VRF sec 5 (b); USA armour/mech/infantry stop at CO in the aggregate catalogue, RUS has 38 BNs). Tasking a container = the vendor's higher-unit Move Along Route (MG Table 1); arrival/stall on its sub-units or its own centroid (D1). RL-20260927-03 (C13 restated): ALL units are created at init as empty containers at their authored positions (visible on the map); ONLY a tasked unit is populated, when the order arrives - populate in place (create sub-units + addToOrganization), never delete-and-recreate the shell | feat/aggregate-containers | design lane after E1, then code | NEXT - before G1 |
| E1 | Registered entity-level run: cut A, T14's ORIGINAL line (derive without (i); (k) stays unmerged), lone platforms on Move To per vertex | run/ironstorm-cuta-e1 | lane E1; build 5e8d6f1 (src = 3e5ab14: M1 + D1 + A1). M2 was held back so that the planner saw the authored line; that hold is now released | SCORED 2026-09-27 (run 20260927T182140Z; PREREG_IRONSTORM_CUTA_E1_2026-09-27.md Result): every HIGH held; T14 on Move To ARRIVED 6.9 m from its destination on its ORIGINAL line, no stall; MEDIUM miss P11b (early, not late). Open: the vendor judged T14's destination point outside the nav area (unexplained); the vertex k -> k+1 continuation is not yet seen live |
| E2 | Registered entity-level run: the RULED cut-A order (with (i)) on main with M2 - T14 a 2-vertex chain, M2's pre-flight live | run/ironstorm-cuta-e2 | lane E2; build 53215e9 (src = 0849334: M1 + D1/M1b + A1 + M2) | STOPPED 2026-09-27 at P4 (run 20260927T205652Z; PREREG_IRONSTORM_CUTA_E2_2026-09-27.md Result): the STREND gate skipped T02 and T14 0.8 s before the interface recognised their CNFPSL holds' end time, so neither lone platform moved - the frame was NOT tested. Cause (code + log): the end timer counts from its first sweep, the gate from the dispatch stamp; here >= 45 SIM s apart against a 60 s margin. T10 and M2's stage on T10's route exactly as predicted (0 fetches). Re-run = E2-2 after the seat rules on the timer origin |
| G1 | Registered aggregate run: one brigade on the same T14 line, no hand waypoints | run/ironstorm-agg-g1 | after M2 + A1 merge, fixture deploy | QUEUED |
| G2 | Cut A on the aggregate profile; then the full 23-task order in phases | - | after G1 | QUEUED |
| V1 | STP verbs onto the aggregate task set (attack-by-fire, breach, fire support) | - | after G2 | LATER |

| C1 decisions | RULED 2026-09-27 (RL-20260927-04, "As recommended" + D-2 revised): D-1 28ID = HQ only; D-3 company-level HQ; D-4 Mech CO for a US rifle company until authored; D-5 performer only; D-6 withhold a short/vacuous container completion; D-7 TO twins display-only (ask STP why each formation is exported twice); D-8 flat containers. D-2: compose to doctrine (FM 3-96) and AUTHOR the missing US unit types (engineer, support, others) - package C2 | design/aggregate-containers (43b4232) | - | DESIGN DONE; code lane next |
| C2 | Author the US unit types the aggregate catalogue lacks, as new unit types in the derived model set (recipe pattern of tools/sms/Deploy-C2SimSms.ps1: vendor files never committed, our edits recorded): Infantry BN (IBCT), FA BN, Brigade Engineer BN, Brigade Support BN, division HQ; from the catalogue's own engineer/support/infantry units of other nations (Engineering BN (POL), Engineer BN (RUS, Mech), Logistics BN (LTU), CSS CO (USA)) with US nation code and equipment; UG52 72.2 parameters (Attack, Vulnerability, Supplies, Engineering systems), 27.2.3. The aggregate breach/obstacle tasks (Breach_Obstacles, Improve_Breach, Destroy/Improve_Obstacle) become available to the full order's BREACH task. Fixture on the derived SMS | feat/aggregate-authored-units | executor (Opus), parallel to the C1 code lane | BUILT OFFLINE 2026-09-27, NOT live-proven (docs/experiments/AGGREGATE_AUTHORED_UNITS_2026-09-27.md): 7 types - Infantry BN (IBCT), FA BN (IBCT, ABCT), Brigade Engineer BN (IBCT, ABCT), Brigade Support BN, Division HQ - deployed under C:\C2SIM\vrf-sms by tools/sms/Deploy-C2SimAggregateSms.ps1 (recipe committed, no vendor file); composition variant "authored" beside "catalogue" (still the default); fixture IronStorm_Centre_52_Aggregate_C2SIM (not deployed). Owed: the C1 code lane selects a variant and refuses "authored" off the derived SMS; owner items O-1..O-7 in the record (foot vs motorized infantry, combat-power scaling, FSCs) |

| Q1a | RL-20260927-05: when the STREND gate's window expires, an unfinished predecessor WITH a destination counts as OVERDUE and the gate extends to the backstop (closes the >60 s clock-step race); holds unchanged; no-Duration mover unchanged | fix/gate-late-predecessor | timer executor, resumed | RUNNING; merges after E2-2 has run on the registered build |
| D2 | RL-20260927-06: automatic model-set choice by the highest echelon among the TASKED units (order performers), not the init: above BN -> AggregateTacticalLevel only (refuse EntityLevel with a clear message); BN-and-below -> EntityLevel by default, overridable to aggregate by Vrf:ModelSet / runner -ModelSet. Builds on A1's -ModelSet and Test-ModelSetPairing; the fixture and type map follow the choice | - | after C1/C2 | NEXT |

## 2. Order and gates

| step | gate | note |
|---|---|---|
| M1, M2, A1 build offline in parallel | PLAN (done: RL-20260927-01) | worktrees; M1 owns the dispatch branch, M2 owns Preflight/*, A1 owns tools/data/scripts |
| Merge M1 -> main; suite; build + deploy the eleven consumers (RUNBOOK sec 9) | - | deploy is the interface's own output dirs, not C:\MAK |
| E1 | PREREG | DONE 2026-09-27: holder 5170 (pid 45600, holds to ~02:20Z 09-28), runner block 5174-5184, marker 5185 |
| Merge M2, A1 -> main; suite; deploy | - | fixture deploy = the one sanctioned file under C:\MAK |
| E2 | PREREG | RUN 2026-09-27, STOPPED at P4 (see row E2): holder 5170 reused (pid 45600), runner block 5185-5195, marker 5196. E2-2 needs a new registration and new numbers |
| G1 | PREREG | RULED 2026-09-27 (RL-20260927-02): hostile side RUS; representation = POPULATED CONTAINERS (C1), not proxies - G1 runs after C1. Prerequisites: C1 merged; D1 merged; M2's aggregate water rules; the fixture deploy (one sanctioned file); CreationPolicy=AtInit with ComposeHierarchy=false (materialization deletes and re-creates, and the EntityLevel-rooted resolver can expand an aggregate type onto the empty base abstract); NO --pre-order-gate nav-area (the aggregate model loads no nav data) |
| G2, V1 | PREREG each | - |

## 3. Pre-registered predictions (the falsifiers of the frame)

- E1: HELD 2026-09-27 (PREREG_IRONSTORM_CUTA_E1_2026-09-27.md Result). T14 on Move To passed the lake on its west shore,
  the hamlet and the second water body on their east side, and ended 6.9 m from its destination; no stall (the MISS did
  not fire); T02 0.9 m; T10 as in -2; no vacuous completion.
- E2: NOT TESTED 2026-09-27 (PREREG_IRONSTORM_CUTA_E2_2026-09-27.md Result): STOPPED at P4 before any lone-platform
  dispatch; the falsifier did not fire. The E2 predictions (T14 vertex 1 of 2 -> vertex 2 of 2, two move-to completions,
  arrival past the -2 hamlet, M2 moving no vertex and flagging no T14 leg) stand for E2-2.
- G1: the pre-flight detours the wet leg and the brigade arrives; OR it reports "no cleared line" and the unit stops AT the
  lake edge with a report. MISS = a silent stop, or a stop with no water within ~50 m. M2's offline reading of the (e)
  line says branch 2: "VERTEX CHECK ... 0 moved", "OSM WATER ON THE LINE" (lake 197345448 from ~0.84 km), "EXPECTED SLOW",
  "NO ROUTE SHIFT - NO CLEARED LINE ... 14 candidate(s) ran into OSM water", 3 ObservationReports, then a stop near
  54.0268, 23.3172. A RIVER CROSSING report on that leg, any vertex moved, or the unit crossing the lake would indict M2.

## 4. What stays, what retires

- Stays: the nav mesh (input to the entity planner), route shift ON, the stall watchdog, telemetry arrival (C15), the
  offline tools (leg_check with --osm-water/--osm-buildings, corridor_gate as a diagnostic), the engage doctrine.
- Retires after E1 and G1 are live-proven: hand waypoints (i), (j) and (k) in the cut-A derivation; the corridor gate as a
  launch bar; Y-10's "keep MoveAlongRoute" wording for lone platforms (a dated note now, a rewrite then).

## 5. Open questions

- RULED 2026-09-27 (RL-20260927-02): hostile side RUS; populated containers before G1 (C1). Open inside C1: the source of the
  brigade -> battalion/company composition where neither the STP TO nor the catalogue gives it (an authored table per unit
  type is the candidate; its provenance - vendor TO&E in Road to Kaunas, doctrine - is the design lane's to propose).
- What a Move Along Route does at speed factor 0 (runs forever, fails, or ends), the Disaggregated create of a simulated
  leaf, footprint-overlap slowing, and how often an aggregate's position is published while moving: G1 observes them.
- Rivers: report only for now; the vendor's road planner per vertex is the candidate if STP's lines keep crossing them.
- Whether the aggregate abstraction satisfies the demo audience: G1 answers it in one look.

## 6. Not claimed

Rivers solved; combat/attack verbs at aggregate level; timing generalisation (n = 1 per run); the per-vertex continuation
(vertex k -> k+1: E1 drove 1-vertex chains; E2 stopped before any). Move To planning on the maple nav area for a lone
M577A2 is OBSERVED once (E1).
