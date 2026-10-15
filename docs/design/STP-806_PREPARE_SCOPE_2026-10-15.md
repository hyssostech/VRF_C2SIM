# STP-806 SCOPE - one-command prepare + READY gate (post-demo; first slice built offline)

STATUS: 2026-10-15, branch feat/stp-806-prepare off post-demo. Slice 1 (AO intake) is BUILT and selftested offline;
nothing else is built. Nothing here touches main, the demo configuration or C:\MAK. Supervisor directive: offline only,
then PAUSE.

Ticket ask (STP-806, under epic STP-802): `prepare --ao <bbox|init>` orchestrates Prep 1-3 (STP-803 nav data, STP-804
terrain/fixture/config, STP-805 tile warm-up), is idempotent per AO, reports progress (hours allowed the first time),
and ends in a verified READY state; then `push order` runs immediately and repeatably. Failure modes surfaced, never
silent: licence, missing DI-Guy data, terrain cache cold, AO larger than the 20 x 20 km default-precision limit (UG52
66.2) -> sectorised generation. Requirement record: docs/PLAN_PARALLEL_LANES_2026-09-14.md lane L14.

## Where each prep step stands today

| Step | Ticket | State | Tool today |
|---|---|---|---|
| AO intake (bbox or init -> box) | 806 | BUILT on this branch (slice 1) | `--ao-from-init INIT.xml [--margin-km K]` |
| Tile warm-up | 805 | BUILT, merged to post-demo (0433283) | `--warm-preflight-cache S,W,N,E DIR [--dry-run]` |
| Nav data generation + connectivity gate | 803 | MANUAL procedure, not productized | tools/navdata (vrfNavGenerator command line, nav_gate.py, corridor_gate.py, make_nav_terrain.py) |
| Terrain copy + fixture + SMS + appData | 804 | MANUAL procedure, not productized | tools/FixtureGen (build_fixture.py, validate_fixture.py); APPDATA relocation per RUNBOOK |
| READY gate | 806 | NOT BUILT | pieces exist: "New Primary nav area" evidence (RUNBOOK sec on the nav-area gate), oracle gate, join |

## Slice 1 - AO intake (BUILT here)

- `Preflight/AoExtent.cs`: the AO of an initialization = every unit position (unparseable or empty ones SKIPPED and
  counted, never read as 0,0) and every vertex of every area, line, point and task graphic, widened by a margin
  (111.32 km/deg, longitude scaled by cos(mid-latitude); the same arithmetic as the demo-extent margin).
- It NAMES what sets each edge. Reason, found on the first real file: data/COA-STP1_Initialization.xml gives a
  244 x 280 km AO whose S/W edges are point IRVINE and whose N/E edges are point LAS_VEGAS - two reference points, not
  the fight. The CLI then prints "TOO BIG for one warm-up" (14268 L13 tiles > 4096) and names those edges, instead of
  starting hours of fetching.
- `--ao-selftest`: 11 checks (synthetic extents, skipped units, edges, margin arithmetic, null on no position, box
  text round-trips through the warm-up's parser, the real committed init end to end).

## Proposed slices after the pause (each its own branch off post-demo)

| # | What | Off/Live | Gate |
|---|---|---|---|
| 2 | AO filtering: exclude named items (or point graphics) from the AO, so IRVINE/LAS_VEGAS-type references do not set it | Off | RULE: which items define the AO (units only? units + areas/lines?) - owner/supervisor |
| 3 | Sector plan: split the AO into <= 20 x 20 km generation cells (UG52 66.2) and emit the per-cell navGenConfig + command lines; dry-run only | Off | none |
| 4 | Nav generation runner (803): drive vrfNavGenerator per cell, nav_gate.py on every log, record terrain state per area; resumable | Live, long (3.3 h for 41 x 54 km) | SPEND-like: box time; PREREG |
| 5 | Fixture/config (804): FixtureGen into a SCRATCH out-dir; the deploy into C:\MAK stays the one sanctioned write, done by a separate explicit step | Off + 1 deploy | RULE: sanctioned deploy path for a generated fixture |
| 6 | `prepare` orchestrator + per-AO manifest (idempotent: skip a step whose output hash matches) + READY gate | Off + Live | PLAN |
| 7 | RUNBOOK section + demo start script picks the prepared profile by name | Off | none |

## Open questions (not decided here)

- Which items define the AO (slice 2). Default proposal: units plus areas and lines; points and task graphics only
  when inside the unit extent widened by 10 km.
- Where generated nav data and fixtures live outside C:\MAK, and who approves each deploy (slice 5).
- Naming collision to clear up: RUNBOOK sec 12 and DeferredDispatch.cs label the ROUTE SHIFT "STP-804/806", while
  Jira STP-804/806 are Prep 2/Prep 4 of STP-802. No code depends on it; a doc fix after the demo.
