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
  to Kaunas: 81 simulated units inside 17 containers). So a brigade is either (a) a company/battalion-sized simulated
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
| M2 | C# pre-flight port of the OSM water + building readers; vertex nudge + report; per-profile leg rules keyed on Vrf:ModelSet; river report | feat/preflight-osm-features | executor (Opus) | RUNNING |
| A1 | Aggregate profile prep: .magx survey, data/unit-type-map-52-aggregate.json, fixture IronStorm_Centre_52_Aggregate (not deployed), Vrf:ModelSet switch, G1 draft checklist | feat/aggregate-profile | executor (Opus) | DONE 2026-09-27 (6910e8e); merge held until E1's build is registered |
| D1 | A memberless simulated aggregate counts as ONE position for arrival evidence and the stall watchdog (today both read member positions and skip it: VrfC2SimService.cs :7306, :6807, :7899) - without it a leaf's stop is silent at task level | fix/aggregate-leaf-position | M1 executor, resumed | RUNNING |
| M1b | Runner report-evidence and the offline analysis tools key on "MoveAlongRoute issued" lines; teach them the per-vertex lines | with D1 | M1 executor | RUNNING |
| C1 | Populate a container brigade with sub-units at creation (the vendor's shape for BDE/DIV); needed for fidelity, not for movement | - | after G2 | LATER |
| E1 | Registered entity-level run: cut A, T14's ORIGINAL line (derive without (i); (k) stays unmerged), lone platforms on Move To per vertex | run/ironstorm-cuta-e1 | after M1 merge + build/deploy | QUEUED |
| G1 | Registered aggregate run: one brigade on the same T14 line, no hand waypoints | run/ironstorm-agg-g1 | after M2 + A1 merge, fixture deploy | QUEUED |
| G2 | Cut A on the aggregate profile; then the full 23-task order in phases | - | after G1 | QUEUED |
| V1 | STP verbs onto the aggregate task set (attack-by-fire, breach, fire support) | - | after G2 | LATER |

## 2. Order and gates

| step | gate | note |
|---|---|---|
| M1, M2, A1 build offline in parallel | PLAN (done: RL-20260927-01) | worktrees; M1 owns the dispatch branch, M2 owns Preflight/*, A1 owns tools/data/scripts |
| Merge M1 -> main; suite; build + deploy the eleven consumers (RUNBOOK sec 9) | - | deploy is the interface's own output dirs, not C:\MAK |
| E1 | PREREG | prediction below; holder first (the 09-27 holder expired ~08:30Z); appNumbers from the ledger marker (5170) |
| Merge M2, A1 -> main; suite; deploy | - | fixture deploy = the one sanctioned file under C:\MAK |
| G1 | PREREG + RULE | RULE (owner): hostile side RUS (107 simulated units, branch-correct, the vendor's Baltic choice; the map's default) or BLR (24; proxies); and the representation for the first arm: company/battalion-sized PROXY aggregates (recommended for G1/G2) vs populated containers (C1). Prerequisites: D1 merged; M2's aggregate water rules; the fixture deploy (one sanctioned file); CreationPolicy=AtInit with ComposeHierarchy=false (materialization deletes and re-creates, and the EntityLevel-rooted resolver can expand an aggregate type onto the empty base abstract); NO --pre-order-gate nav-area (the aggregate model loads no nav data) |
| G2, V1 | PREREG each | - |

## 3. Pre-registered predictions (the falsifiers of the frame)

- E1: T14 (one M577A2) on its original line reaches within 100 m of its destination; T02 arrives; T10 as in run -2.
  MISS = T14 stalls >= 60 sim s with no OSM feature within ~10 m ahead -> the frame is wrong; STOP and ask.
  Also recorded: the planner's path shape (does it skirt the lake/hamlet), vacuous-completion warnings, per-vertex log lines.
- G1: the pre-flight detours the wet leg and the brigade arrives; OR it reports "no cleared line" and the unit stops AT the
  lake edge with a report. MISS = a silent stop, or a stop with no water within ~50 m.

## 4. What stays, what retires

- Stays: the nav mesh (input to the entity planner), route shift ON, the stall watchdog, telemetry arrival (C15), the
  offline tools (leg_check with --osm-water/--osm-buildings, corridor_gate as a diagnostic), the engage doctrine.
- Retires after E1 and G1 are live-proven: hand waypoints (i), (j) and (k) in the cut-A derivation; the corridor gate as a
  launch bar; Y-10's "keep MoveAlongRoute" wording for lone platforms (a dated note now, a rewrite then).

## 5. Open questions

- Hostile-side aggregate mapping (owner's): RUS recommended - see the G1 gate row.
- Representation at aggregate level (owner's): proxies now, populated containers (C1) for fidelity later - recommended.
- What a Move Along Route does at speed factor 0 (runs forever, fails, or ends), the Disaggregated create of a simulated
  leaf, footprint-overlap slowing, and how often an aggregate's position is published while moving: G1 observes them.
- Rivers: report only for now; the vendor's road planner per vertex is the candidate if STP's lines keep crossing them.
- Whether the aggregate abstraction satisfies the demo audience: G1 answers it in one look.

## 6. Not claimed

Rivers solved; combat/attack verbs at aggregate level; timing generalisation (n = 1 per run); that Move To plans on the
maple nav area for a lone M577A2 (documented, not yet observed - E1 is the observation).
