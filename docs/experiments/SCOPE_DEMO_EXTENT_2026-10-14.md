# SCOPE: WIDEN THE DEMO EXTENT? (offline, 2026-10-14; asked by the STP-1097 supervisor for an operator decision)

STATUS: SCOPE ONLY - no live run, nothing changed. The extent stays RL-20261005-02's until the operator rules.
Fable cold review 2026-10-14: HOLDS WITH CHANGES (method, option 4, the geometry bullets, the cache precondition) - applied.

## Method

The pre-run's COA 1 export (25 tasks; runs/launch52/prerun-976/export-coa1, not in the repo), run through an emulation of
the interface's own demo-extent rule: the points RouteExtentPolicy.DemoExtentPoints tests - the performer's init
position, the route TaskGeometryResolver assembles (a line whose near end is > 5 km from the route is left unchained and
is neither tested nor driven), one destination, and the area rings; a Point graphic counts its first coordinate only. A
task runs only if it AND its STREND predecessors are in. AGREEMENT: 25 of 25 - the emulation reproduces every one of the
pre-run's 21 OUT OF DEMO EXTENT tallies and farthest distances, and the 4 admitted. The scripts (seat scratch
extent_scope.py, reviewer scratch extent_emul.py) read customer data and stay out of the repo.

## Options (authored box; the interface tests it + 2 km)

| Option | Authored box S / W / N / E | Size | Tasks admitted | Tasks that EXECUTE |
|---|---|---|---|---|
| 0 today | 53.940 / 23.108 / 54.119 / 23.414 | 20 x 20 km, 400 km2 | 4 (T6-T8 1-112 IN, T25 11 CAB) | 3 - 11 CAB refused by ruling (RL-20261004-06) |
| 1 west | 53.918 / 23.050 / 54.119 / 23.414 | 24 x 22 km, 535 km2 | 7 (+T22-T24 113 SB) | 6 |
| 2 breach | 53.813 / 23.050 / 54.217 / 23.764 | 47 x 45 km, 2,100 km2 | 9 (+T16 48 IBCT, T1 56 SBCT) | 8 |
| 3 +FAB | 53.813 / 23.050 / 54.450 / 23.764 | 47 x 71 km, 3,300 km2 | 10 (+T21 169 FAB) | 9 |
| 4 full | 53.813 / 22.854 / 55.000 / 24.140 | 83 x 132 km, 11,000 km2 | 25 | 20 - 55 MEB (T17-T20) and 11 CAB refused by ruling |

- 55 MEB (T17-T20) needs the box 13 km further west (W 22.854): it adds only refused containers. Option 4 without it:
  W 23.036, 72 x 132 km, ~9,450 km2.
- 278 ACR (T2-T5) and 116 ABCT (T9-T15) need option 4's north edge. 278 ACR's chain box is S54.190 W23.070 N54.953
  E23.992 (bound by T4's breach at 54.95 N); 116 ABCT's is S54.000 W23.036 N55.000 E24.140 (DOUBLEDAY 54.88 N, T14's
  breach 54.87 N, LANCASTER 24.14 E). The 116 ABCT chain starts with T9 '28ID_W_LIMIT', which drives the 2-vertex
  division boundary (54.0 -> 55.0 N at 23.1 E) as a 111 km route - the resolver treats every Line of >= 2 vertices as a
  path. That is a geometry question for STP (STP-962 family), not an extent one.
- The unbounded order (no extent set, GUI on) is what VOIDED REHEARSAL_WAYB 2026-10-05: terrain feature_source paging
  (tfs / mvt / dynamic feature set) and planner MAK_OBSTACLE area jobs backed up minutes (median queue wait 71 s, p95
  192 s) out to ~24.4 E - which is why RL-20261005-02 exists.

## The warm terrain-cache test (what it would take)

Vendor basis: streamed terrain tiles "are cached locally. Subsequent requests for those tiles are accessed from the
cache" (MAK ONE 2025 Adding Content p155). That passage is about streamed TILES; it says nothing about the feature paging
or the obstacle-area jobs that backed up. Terrain page-in areas (p156) are documented for paged IVE and MetaFlight
terrains only, so they are not a planned lever for the MAK Earth (online) streaming terrain.
- PRECONDITION, checked 2026-10-14: the cache must survive between the two launches. The demo launches with the
  run-owned appData C:\C2SIM\vrf-appdata-unattended\appData (LaunchVrf52 log); its cache\vrfsim holds 28,070 files /
  510 MB written from 2026-09-28 through today's pre-run (19:39Z), and its elevation / imagery caches date from 10-04/05 -
  so it persists across runs as long as that tree is reused, not reseeded, between the two launches.
- WHAT: one throw-away run of the widened order (population + dispatch) to fill the cache, then the scored run of the
  same order. Measured: the vendor log's feature_source queue waits (counts only, as the 10-05 addendum did), the
  interface's SIM/WALL ratio and liveness (no BACK END LOST), members moving, the cache's growth, plus a working-set
  sampler (V6 runaway).
- WHAT IT DECIDES: whether the backlog was FETCH (the cache removes it) or feature / obstacle PROCESSING (recomputed
  every run; the cache does not help). Only the first makes a wider extent safe.
- TIME: ~2.5 h of seat time (two launches of ~40 min each, teardowns, analysis); ~12 ledger app numbers.
- RISK: RTI - the holders and rtiexec are never touched; a back end that backs up or stops refuses a graceful close
  and is force-stopped (exit 6, as today), which can leave a stale federate that the next join checks (RUNBOOK sec 0).
  Machine - a V6-style stop allocates ~2.2 GB/min, hence the sampler and an abort threshold. Disk and network - the
  vendor's own cache grows and fetches from MAK's server, as every new area does.

## Recommendation

Option 2 if the operator wants the breach / FPOL story (T1, T16: the STP-962 items) on the map; otherwise stay at 0
(option 1 adds only three sustainment tasks). Option 2 only after the warm-cache test passes ON option 2's box, run
in the same session as the BACK END LOST repro (PRERUN_STP976_2026-10-15, offline analysis): both need VR-Forces live,
and option 2 admits the first long planner route (T1, ~60 km: 56 SBCT's start to the lane's start point) plus river and
lake crossings - and the stop happened inside a 48 IBCT re-task from a start in water. Not option 4 for this demo.
