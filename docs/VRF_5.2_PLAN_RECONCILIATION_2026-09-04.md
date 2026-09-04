# 5.2 plan vs research - full reconciliation, 2026-09-04

Purpose: the research (DIFF rows A-H, DECISION_EVIDENCE Y-1..Y-17, assessment, cold-start map)
contains rulings that never reached the live plan. This pass walks every ruling and asks three
questions: is it DONE, is it IN THE PLAN, or is it MISSING. Prompted by the user after three
separate cases in two days where a settled ruling sat in the evidence file while the plan said
nothing (C8 altitude, D1 ground movement, Y-13 road preference).

Sources read for this pass: VRF_5.2_MIGRATION_DIFF.md (all sections incl. G ledger),
VRF_5.2_DECISION_EVIDENCE.md (Y-7..Y-16), HANDOFF NEXT + 5.2 section, plus disk checks named
below. Nothing here rests on recollection.

## A. RULED ALREADY, MISSING FROM THE PLAN (no new research needed - just not queued)

| # | Ruling | State on disk | Why it matters now |
|---|---|---|---|
| A1 | **Y-9 repeatability knobs**: golden/prereg runs use `blockOnAsynchronousOperations` ON + a pinned seed, always stated in the run header | **NOT WIRED** - 0 hits for blockOnAsync/seed in scripts/, src/, tools/, config/ | PREREG_R9_52 is a golden run. Without this it is not repeatable BY THE PROJECT'S OWN RULING, and 5.2 adds async terrain/path-planning that the knob exists to fence |
| A2 | **Y-3 knob delivery**: use `--notifyLevel` and `--settingsFile <repo file>`; edit C:\MAK only for a knob with no CLI path | **NOT WIRED** - no `--settingsFile` anywhere in scripts/ | This is the mechanism A1 rides on, and the ruled way to avoid touching C:\MAK at all |
| A3 | **Y-7 terrain PROFILES**: four (1 online / 2 offline-cached / 3 offline-authored / 4 shipped USGS N34W117); each is its own baseline, NEVER compare traces across profiles; offline is a REQUIREMENT in some deployments | Only profile 1 exists. FixtureGen has no profile concept | Any trace comparison must name its profile. Offline capability is a deliverable, not a nicety |
| A4 | **Y-8 substitutes** for the three types with no 5.2d equivalent, recorded with the source file cited | AR Scout DONE (6 rows in unit-type-map-52.json). **Mobile Irregular = 0, Mobile Light Infantry = 0** | 2 of 3 never recorded. Not blocking today (neither appears in data/), but the type-map live gate is incomplete until they are |
| A5 | **Y-14 SQLite logging** evaluated in Phase 2 (batch mode REJECTED - "Batch mode is read-only") | Not started - 0 hits for sqlite/databaseConfig | This is the MACHINE-READABLE verification channel. THE GOAL is headless verification without human reading of logs; we are still scraping text |
| A6 | **Y-15 unit representation**: hybrid, two profiles by echelon (EntityLevel + authored doctrinal Lua for company-and-below; AggregateTacticalLevel for battalion+). Authoring order: attack-to-objective family first. No runtime aggregate/disaggregate exists (UG52 13.7) | Not in the NEXT list at all | The largest single body of remaining work, and it decides what a "unit" IS on 5.2 |
| A7 | **Y-17 retire the MSL birth** (2026-09-04) | Queued as NEXT 6b today | Done this pass |

## B. NEEDS RESEARCH BEFORE THE NEXT RUN (read, do not probe)

**B1. NAV DATA DOES NOT COVER OUR AOI - the highest-value unread item.**
DISK CHECK 2026-09-04: `SharedData\19\latest\TerrainData\navData\MAK Earth (online)\` ships
exactly FOUR nav areas - Ala Moana, Kilo2, Range220, Thun (Hawaii / Switzerland). **There is no
nav area at the R9 Mojave AOI (34.615, -116.55).** DIFF C4 says "Ground planning keys off nav
data; online terrain nav coverage is unverified -> Phase 2 gate", and 5.2 turned autonomous path
planning ON by default (Y-12) and flipped armour to off-road (Y-13) - so planning matters more on
5.2 than it did on 5.0.2, in a region with no mesh.
COUNTER-EVIDENCE ALREADY IN HAND, which is why this is a READ and not a probe: the vendor says
Move Along Route "does not use road movement, nor does the entity plan paths before moving"
(help EntityMovementGroundVehicleOverview.htm). Our MOVE is MoveAlongRoute (Y-10). So the
absence of a nav mesh may be irrelevant to our path and matter only to dynamic obstacle
avoidance.
QUESTION TO ANSWER FROM THE DOCS, with citations, BEFORE the R9 5.2 run: (a) does ground
movement require a navigation mesh, or degrade gracefully without one? (b) does dynamic obstacle
avoidance depend on nav data? (c) what happens to a Move To (planning) task outside any nav
area? Sources: UG52 navigation/nav-area chapters, help ConceptsEntityLevel/GroundVehMove/
vrf_groundMovementDynamicObstacleAvoid.htm and vrf_groundMovementNavigationPreferences.htm,
RN p34/p73 (nav data regenerated), navigationProfiles.mtl.
NOTE the 5.0.2 history: the project-generated 120k-tile NavArea WAS the Jul-Sep freeze and is
disabled. Do not regenerate nav data as a reflex - find out first whether it is needed.

**B2. Does the R9 route touch roads at all?** Y-13's off-road flip bites only if the golden route
followed roads. Checkable from the fixture/order geometry against the terrain's road layer - no
run needed. Decides how much of the 5.0.2-vs-5.2 divergence to expect on the FIRST run.

**B3. E3: "Read the debugger doc before the next behavioural probe"** - MAK Remote Debugger and
Tracy are new in 5.2 and never opened. They may replace log scraping for behaviour work.

**B4. Y-9's real cost on ONLINE terrain.** The ruling says the knob makes golden runs slower to
START (tile fetch), not slower to simulate, and that the 0.27x COA-STP1 ratio was entity load.
Both are reasoned, neither is measured on 5.2. Needed for run budgets; measure when A1 lands.

## C. DONE AND PROVEN (closed; listed so nobody re-opens them)
Y-1 launch (LaunchVrf52, independent mode) - Y-2 federation identity (config-file join, C#
FomModules emptied) - Y-5 (folded into Y-1) - Y-6 remoteControlInit + Start/Tick on the 5.2d
sample loop - Y-10 keep MoveAlongRoute (probe RETIRED, class deleted) - Y-11 Maneuver To accepted
- Y-12 autonomy left ON - Y-13 recorded as a prereg constraint (2026-09-04) - Y-16 HLA 4 deferred
to its own phase - 7-field ObjectTypeResolver fix - the rtiexec posture - the --logFileName crash
- the AGL verification.

## D. STALE ENTRIES FOUND DURING THIS PASS (fixed in the same commit)
- **G ledger, Y-4**: read "--logFileName keeps bin64\vrfSim.log". CONTRADICTED by
  PREREG_52_CRASH_BISECT: passing `--logFileName` crashes the sim ~1 launch in 3 and we now never
  pass it, harvesting C:\MAK\logs instead. A reader of the ledger would have re-introduced the
  crash. Corrected.
- (Earlier in this same pass: D1's "must be probed" cell and C8's "N (later)" - both already
  fixed and committed.)

## E. WHAT THIS PASS SAYS ABOUT THE PROCESS
Three rulings in two days were found sitting in the evidence file while the plan said nothing,
and this pass found four more (A1, A2, A3, A5) plus one actively wrong ledger line (D). The
common failure is not research quality - the research is good and correctly cited. It is that
**a ruling was recorded where it was DECIDED and never carried to where work is CHOSEN.**
Standing fix: when a ruling lands, it goes into the NEXT list or CLAUDE.md the same turn, or it
does not count as recorded. A ruling with no queue entry is a ruling nobody will act on.
