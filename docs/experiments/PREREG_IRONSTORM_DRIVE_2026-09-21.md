# PREREG - IRON STORM CUT A: THE 0.82-0.90 CONNECTIVITY DIAGNOSTIC DRIVE

REGISTERED by the seat 2026-09-21, BEFORE the run, under the user's ruling of 2026-09-21 ('2. A.': ONE pre-registered
diagnostic drive across the never-driven 0.82-0.90 nav-connectivity band; deploying the fixture with its KNOWN gate FAIL
is authorised for it; the 0.9 bar itself stays; fallback = the T14-only cut; the demo clock is FAST). Drafted by an Opus
executor from the docs, the data and offline tool runs - nothing was launched to write it. The executor's UPDATE 2
corrections (the wrapper runs FidelityTable + AtOrder) are IN the text below; where a line says DRAFT read REGISTERED.
The seat's additions are the last section. A missed HIGH limb in sec 6a is a STOP, never a patch; sec 6b is REPORTED.

STATUS: **DRAFT. NOT REGISTERED.** Written 2026-09-21 by the PREPARATION executor, before any
live run. The seat registers it (naming the BUILD and the run id) and only then launches.

## 0. WHAT THIS DRIVE IS, AND WHAT IT IS NOT

**THE QUESTION, one sentence:** can a ground unit drive through navigation-mesh sectors whose
abstract-graph connectivity ratio reads 0.82-0.90 without freezing?

**THIS IS A DIAGNOSTIC, NOT A DEMO REHEARSAL AND NOT A CONFIRMING RUN.** The band has NEVER
been driven in this project's record. Every ratio ever tied to an OBSERVED drive sits either
far below 0.5 (1-35 at 0.11 froze in six runs; N2d 0.29; V1 0.07; one at 0.02) or at exactly
1.00 (the clean AO20 runs that worked). The 0.9 bar is a MARGIN set above the observed
failures, not a measured cliff at 0.9. **Nothing in the record predicts the outcome here, and
this prereg must not pretend otherwise.** Predictions below are split into

- **SCORED** - things an instrument in this run can resolve, registered with a HIT/MISS rule;
- **REPORT, NOT SCORED** - things the run will measure but that no honest band exists for.

The seat has mis-set four prediction bands this week (D6 P2, D7 P3, D8 P4, D9 N14). When in
doubt a limb goes in the second list.

**THE USER'S RULING BEHIND IT (2026-09-21, "2. A."):** the 0.9 bar STAYS; the fixture is
deployed WITH ITS KNOWN GATE FAIL for this one pre-registered diagnostic; the fallback if it
freezes is a thin T14-only cut. The clock is FAST (run-to-complete); no real-time fixture.

## 1. THE ARTEFACTS UNDER TEST (fill the build line before registering)

    scenario  IronStorm_Centre_52_Nav_AG   (C:\MAK\vrforces5.2d\userData\scenarios)
              sha256 d379dd68ae964f599d6ee687367d2d14176723218e0652a559935d77adeaa4bb  6,561 B
    terrain   tools\navdata\out\MAK Earth (online) + IronStormCentre.mtf
              sha256 db43917d95db9c8ae3ccd412c321fa4c3cc8e42cdbd34a6c436e6e9d16ffee7d  3,007,761 B
              (the shipped MAK Earth (online).mtf + ONE navData record, 9 -> 10)
    nav data  C:\C2SIM\vrf-nav\ironstorm-centre-2026-09-20\  GENERATION 1, raster-precision
              0.200000 (the vendor default), 4,764 files / 298,035,892 B. NOT REGENERATED.
    sms       C:\C2SIM\vrf-sms\C2SIM_EntityLevel_AbstractGraphs.sms  (useAbstractGraphs = true)
    init      data\IRONSTORM_CUTA_Initialization.xml  sha256 2000e856...c93eec  155,020 B
    order     data\IRONSTORM_CUTA_Order.xml           sha256 5dbe8b0b...b980d1   71,210 B
    BUILD     __________ (the seat names it: 4f1f149 today; STP-855's route-origin fix is in
              review and will be merged and rebuilt first. If the build CHANGES, limb P7
              changes with it - see there.)

## 2. WHAT IS BEING DRIVEN, AND OVER WHAT GROUND

Cut A keeps 5 tasks on 3 taskees. The other 33 creatable units are created and stand idle.

| task | verb (TaskActionCode -> TaskIntent) | taskee | what it does |
|---|---|---|---|
| T01 | ExecutePlanPhase -> HoldInPlace | 28ID | NO VR-Forces task is issued; nothing moves |
| T02 | ATTACK -> Attack | 28ID | drives 5,341 m |
| T10 | CRESRV -> HoldObjective | 1-112 IN | drives 2,617 m, then holds and scans |
| T13 | ExecutePlanPhase -> HoldInPlace | 48 IBCT | NO VR-Forces task; nothing moves |
| T14 | ATTACK -> Attack | 48 IBCT | drives 2,426 m (the NUDGED, dry line) |

**THE SUB-0.9 SECTORS THE DRIVE CROSSES** (generation 1, p=0.2, change (e) applied; corridor
sampled every 2 m; from the prep report PART 4):

| leg | corridor sectors | sub-0.9 | worst | route metres inside sub-0.9 |
|---|---|---|---|---|
| T02 28ID | 16 | **3** - (14,13) 0.8947 / 100 m, (16,14) 0.8868 / 422 m, (17,15) 0.8800 / 318 m | **0.8800** | 840 m of 5,351 (15.7 %) |
| T10 1-112 IN | 8 | **4** - (27,22) 0.8475 / 60 m, (27,23) 0.8868 / 478 m, (28,21) 0.8182 / 476 m, (28,22) 0.8571 / 418 m | **0.8182** | 1,433 m of 2,620 (54.7 %) |
| T14 48 IBCT | 8 | **0** (worst 0.9107) | 0.9107 | 0 m (0.0 %) |

**T14 IS THE CONTROL.** It is gate-clean on the same nav data, in the same run, under the same
build. If T14 drives and T02/T10 do not, the difference is the sub-0.9 ground and not the
fixture, the terrain, the build or the day. If T14 ALSO fails, the cause is something else and
this run says nothing about the band - that is a stop, not a finding about connectivity.

Across all 29 corridor sectors: **0 below 0.5, 0 without an abstract graph**, band 0.8182 to
1.0000. This is not the shattered regime (AO20R 0.02-0.10, NavData to 1,535 kB) the 0.9 bar
was calibrated against; the worst sector here is 0.8182 with 73.2 kB.

**WATER:** 0 of 3 driven legs crosses water. Re-verified this session with the current
`leg_check.py` (which now resolves MapGraphicIDs): T02 0 wet, T10 0 wet, T14 0 wet; the prior
lane verified T14 dry at 25/10/5/2 m strides (1,213 samples at 2 m, zero water). T01's and
T13's resolved graphic geometry DOES cross water, and that is harmless: neither issues a
VR-Forces task.

## 3. THE TYPE RESOLUTION  *** REVISED, UPDATE 2 - THIS WHOLE SECTION IS REPLACED ***

**WITHDRAWN:** this section previously claimed the run uses `RealTemplates`, on the grounds that
`RunC2SimScenario.ps1` never sets `Vrf__TypeMappingMode`. **That was WRONG**, and the error was
reading the ps1 only. `scripts\RunScenario.sh:302-303` exports
**`Vrf__TypeMappingMode=FidelityTable`** and **`Vrf__CreationPolicy=AtOrder`** before it calls
the ps1, and the ps1's `AppEnv52` sets `Vrf__TypeMapFile` to `data\unit-type-map-52.json`. The
2026-09-21 live run log says so at start-up:

> `Type-mapping mode = FidelityTable (123 rows from ...\data\unit-type-map-52.json);`
> `FriendlyNation=USA, OpposingNation=RUS; SurfaceProxySubstitutions=True.`

FidelityTable is the CHOSEN mode under the standing "fidelity over oracle shortcuts" ruling, not
a default anyone fell into. **The PART 1 prereg draft was RIGHT all along.**

**THE CORRECTED TABLE** (`UnitTypeMap.Lookup` key order via `leg_check.TypeMap`, which cites
`UnitTypeMap.cs:217-262`; members from the installed 5.2 template catalogue):

| taskee | SIDC / fid / ech | rule | row | fidelity | template | kind | members |
|---|---|---|---|---|---|---|---|
| **28ID** (T02) | SFGPUCI----I--- / `UCI` / `I` | `e:catchAll` | `F-GEN-ANY` | **PROXY** | `M1A2_Abrams_MBT` | **ENTITY (platform)** | **1** |
| **1-112 IN** (T10) | SFGPUCI----F--- / `UCI` / `F` | `b:functionId+sidcEchelon` | `F-UCI-F` | **PROXY** | `Tank Headquarters Section (USA)` | **AGGREGATE** | **6** (2x M1A2, 1x M3A2 Bradley CFV, 1x M577A2 Command Post, 2x M998 HMMWV) |
| **48 IBCT** (T14) | SFGPUCI----H--- / `UCI` / `H` | `d:sidcEchelon` | `F-GEN-H` | **PROXY** | `M577A2_Command_Post` | **ENTITY (platform)** | **1** |

**ALL THREE ARE PROXY ROWS. NONE IS EXACT**, and each says why in its own `proxyNote`:
"catch-all: no row for this function ID or echelon; a single MBT (the port's historical
default)"; "battalion CP: no Country-225 Category-6 template exists; HQ-Section substitution
(survey F6)"; "echelon-only fallback: no Category-8 template exists; a single command-post
track". **No dismounts anywhere, so STP-845 cannot apply.**

**WHOLE-FILE CENSUS, all 40 units:** `PROXY d:sidcEchelon x20`, `PROXY e:catchAll x17`,
`PROXY b:functionId+sidcEchelon x3`. **0 EXACT. 0 units with NO ROW.** `isAggregate`
**36 False / 4 True**. Key (a) `initSISOEntityType` CANNOT fire here - every `SISOEntityType` in
the file is all-zero, so no row covers it and the SIDC-derived row is used (the app's "coverage
backstop", JC-1). **The app NEVER refuses or skips a unit for want of a row:** rule (e)
`F-GEN-ANY` is a real row and is the backstop.

**`Vrf:CreationPolicy=AtOrder` (C13), also missed before.** Aggregates are created as EMPTY
SHELLS at their authored positions; **their members are created only when an order first
references them**; platforms are created in full. Of the 4 aggregates in the file **only
1-112 IN is order-referenced**, so it is the only one that ever grows members.

**THE C16 CONSEQUENCE SURVIVES THE CORRECTION - this is the reconciliation the seat asked for.**
28ID and 48 IBCT are **still single platforms**, just an M1A2 and an M577A2 command-post track
instead of two M1A2s. The watchdog decides on MEMBER displacement, so **it plainly applies to
T10 (6 members); its behaviour on T02 and T14 remains the open question** (P10).
**Member / jam denominators: T02 = 1, T10 = 6, T14 = 1.**

Pre-flight slope limits were the FidelityTable ones ALL ALONG (`leg_check` defaults to
`unit-type-map-52.json`): min max-slope **0.94** for the M1A2 (T02) and the HQ Section (T10),
**1.00** for the M577A2 (T14) - exactly the `limit_raw` values in sec 2's leg table, which is
therefore unaffected by this correction.

## 4. THE CLOCK, AND THE ONE THING THAT WILL TRUNCATE THE RUN

`Vrf:TaskClock = sim` (default). `--duration-scale 0.25` scales the Duration that ENDS a task
and the StartTime delay that HOLDS one back - **not movement**. Schedule, in SIM seconds:

| | scaled start | scaled Duration | armed end | travel at 10 m/s | arrival |
|---|---|---|---|---|---|
| T01 / T13 | 0 | 300 s | 300 s | - | - (holds) |
| T02 | 360 s (pred. end 300 + `TaskPredecessorEndMarginSeconds` 60) | 300 s | **660 s** | 534 s | **894 s** |
| T10 | 300 s (authored `PT20M` delay x 0.25) | 450 s | 750 s | 262 s | 562 s |
| T14 | 360 s | 300 s | 660 s | 243 s | 603 s |

**T02's ARMED END (660 s) FALLS BEFORE ITS ARRIVAL (894 s).** At 0.25 the scaled Duration is
SHORTER than the drive. Two consequences, both load-bearing:

1. **TASKCMPLT IS NOT EVIDENCE OF ARRIVAL for T02.** `TIMED COMPLETION (R4)` pushes TASKCMPLT at
   the armed end. The UNIT KEEPS DRIVING - the code releases `_inFlight` only for the
   `hold-in-place` kind; "every other kind is left alone: the vendor completion still owns it"
   (`VrfC2SimService.cs` ~6068). **Arrival must be scored from position reports and the vendor
   completion line, never from TASKCMPLT.**
2. **`--stop-when-complete` IS ON BY DEFAULT AND WOULD END THE RUN AT ~sim 660 s**, while 28ID
   is roughly 3.0 km along a 5.34 km leg - i.e. before it reaches (16,14) and (17,15), two of
   the three sectors this drive exists to observe. The runner closes the window once EVERY order
   task has a TERMINAL report (TASKCMPLT **or** TASKABRT), and a timed TASKCMPLT is terminal.
   **THE COMMAND IN SEC 5 THEREFORE PASSES `--no-stop-when-complete`.**

Robustness of that claim: T02's timer beats its arrival for any cruise speed below ~17.8 m/s
(64 km/h). A nav-mesh path is LONGER than the authored line, which strengthens it. The claim
fails only if the unit cruises faster than 17.8 m/s - itself a MISS worth recording.

**WALL DURATION.** `fixed-frame-run-to-complete` runs as fast as the load allows and the ratio
is LOAD-DEPENDENT and monotone (D8 RESULT N11: 2.63x at peak load to ~4.7x idle **on R9, a
near-empty scenario**; COA-STP1 at 128 units once measured **0.27x**). Iron Storm cut A creates
**36 objects**, of which 1 is a disaggregated aggregate and 3 ever move. That sits between the
two measured points and nothing in the record pins it.

    sim time to last arrival   ~15 min (894 s) + nav-mesh detour, call it 15-21 sim min
    ASSUMED sim/wall ratio     0.5x .. 3.0x   (NOT measured for this scenario - see P9)
    => movement phase          5 .. 42 wall min
    + launch / join / init     ~5 .. 8 wall min before the order is pushed

**State a RANGE, never a point.** The instrument that settles it is the app's own per-minute
`SIM/WALL RATIO` line - read it, never divide one wall figure by another (that is how D6 came to
record 1.00 for a scenario D7 measured at 3.00x).

## 5. THE EXACT LIVE COMMAND (Way A, one command)

Prerequisites the seat owns and this executor did NOT touch: `rtiexec` up and a HOLDER federate
joined (RUNBOOK 9c, STP-825 - creates fail in clusters); `c2sim-server-vrf` up on 18080/61614;
no leftover `vrfSim*` back end (`-AllowExistingVrf` is the false-READY trap); the RTI dialog
answered once for this boot.

```bash
bash scripts/RunScenario.sh --gui \
  --scenario IronStorm_Centre_52_Nav_AG \
  --init  data/IRONSTORM_CUTA_Initialization.xml \
  --order data/IRONSTORM_CUTA_Order.xml \
  --client-id "Not Set" \
  --duration-scale 0.25 \
  --object-console 3 --member-console 3 \
  --no-stop-when-complete \
  --run-secs 2400 \
  --env Vrf__StallDetection=true \
  --env Vrf__StallClock=sim \
  --vrf-appdata-dir 'C:\C2SIM\vrf-appdata-unattended\appData' \
  --log runs/ironstorm-drive.log
```

Verified by DRY RUN on the deployed build (runner exit 0, every input source resolved as
intended, `init SystemName [Not Set] matches app clientId [Not Set]`).

*** UPDATE 2 - THE WRAPPER'S OWN EXPORTS (`RunScenario.sh:302-311`), which are part of this
command whether or not they appear on the line above. Do NOT add any of them by hand. ***

| export | value | did this prereg assume it? |
|---|---|---|
| `Vrf__TypeMappingMode` | **FidelityTable** | **NO - MISSED. Sec 3 is rewritten.** |
| `Vrf__CreationPolicy` | **AtOrder** | **NO - MISSED. P1, P7, P10, P14 revised.** |
| `Vrf__TaskPredecessorTimeoutSeconds` | **7200** | **NO - I quoted appsettings' 600.** Harmless here: the gate is a FLOOR, the chain is 2 deep and neither `startAfter` uuid is dangling, so nothing reaches the timeout either way. |
| `Vrf__DeStackRotationDeg` | 0 | not named before; no effect - no taskee is in a ring (P8) |
| `Vrf__DeStackCreates` / `Vrf__DeStackSpacingMeters` | true / 700 | YES - and they match appsettings, so P8 is unaffected |
| `Vrf__DropOriginVertexMeters` | 100 | YES - the same 100 m origin drop used to resolve T14's line |
| `Vrf__ObjectConsoleNotifyLevel` / `...MemberNotifyLevel` | from `--object-console` / `--member-console` = 3 / 3 | YES |
| `Vrf__PositionReportSeconds` | 10 | YES - the dry run echoes `positionReport=10s` |

NOT exported by the wrapper, so the deployed `appsettings.json` value stands:
`PreflightRouteShift` true, `PreflightOffline` false, `PreflightCacheDir` empty,
`TaskClock` sim, `TimedCompletion` true, `TaskPredecessorEndMarginSeconds` 60,
`RouteExtentCheck` true, `DeStackComposedSiblings` true (0 groups, so inert).

Why each non-obvious flag:

- `--client-id "Not Set"` - STP's connector ships `SystemName` = the literal `Not Set`
  (STP-847). Without this, `VrfC2SimService.cs:1164` skips all 40 units and ZERO are created.
- `--object-console 3 --member-console 3` - the nav READY row (`New Primary nav area`) and the
  vendor's own movement chatter need notify level >= 3. A silent channel at level 1 is the
  CONFIGURED outcome, not evidence.
- `--no-stop-when-complete` - sec 4, consequence 2. **Without it the drive truncates.**
- `--run-secs 2400` - a CAP, not a length. Sized for the slow end of the ratio range.
- `--env Vrf__StallDetection=true` - C16 is OFF by default, is in NEITHER appsettings file and
  has NO script switch (RUNBOOK sec 10). A freeze diagnostic without it has no freeze instrument.
- `--env Vrf__StallClock=sim` - under FFRTC a 240 s WALL window spans 240 x ratio SIM seconds
  (500-1,000 s), longer than T02's whole 534 s drive, so it could never fire. The calibrated SIM
  window is 360 SIM s. **Register that 360 s is still 67 % of T02's drive: this instrument can
  catch a freeze that starts early and will MISS one that starts late. That is a resolution
  limit, not a result.**
- NO nav or SMS switch is needed: the nav area rides in the fixture's terrain `.mtf` and the SMS
  is named by absolute path inside the `.scnx`. The unattended appData sets
  `loadAllNavigationDataOnTerrainLoad 1`, so nav data loads WITH the scenario rather than lazily.

Tile cache: `Vrf:PreflightCacheDir` is empty, so the app uses `<exe dir>\preflight-cache`, which
holds **7 tiles, all Mojave L13** - COLD for Suwalki. `Vrf:PreflightOffline` is FALSE, so the app
FETCHES what it needs over HTTP at dispatch; the skip-warning path fires only when Offline is
TRUE. Measured size of a warm Suwalki set for the whole cut-A geometry: **338 tiles** (317 x
land-cover 59/L14, 7 x elevation 149/L12, 14 others). **No pre-warm is required for correctness:
no leg is flagged (ratios 0.05-0.11 against a 0.92 threshold), so no route shift can fire
whatever the cache holds.** Optional, to remove HTTP latency from dispatch:
`--env Vrf__PreflightCacheDir=<a warm dir>`. Registering it as optional, not doing it, and NOT
changing `Vrf:PreflightElevationLevel` (13, cascading to 12 where Suwalki is served) - the
cascade already resolves every sample at L12 and moving it would decouple this run from D1-D9.

## 6. PREDICTIONS

### 6a. SCORED - an instrument in this run resolves these

| # | prediction | confidence | MISS = |
|---|---|---|---|
| **P1** | *** REVISED UPDATE 2 *** **36 units created** of 40 (4 have no position), 0 lifeforms, under `ClientId "Not Set"` - the count is IDENTICAL in both type-mapping modes (`--destack-selftest` prints `36 placeable` under `[RealTemplates]` AND `[FidelityTable]`). Under `CreationPolicy=AtOrder` the app's own line should read **aggregates as EMPTY SHELLS, platforms in full**, and only **1-112 IN** should ever grow members (6), because it is the only order-referenced aggregate. Peak simulated objects therefore ~36 + 6 = **42**, not 36 fully-populated units. | HIGH | any other creation count, or members appearing on an un-referenced aggregate |
| **P2** | **All 5 tasks DISPATCH. 0 TASKABRT at dispatch time.** STP-833 cannot refuse any of them: the longest resolved leg in the cut is 19,706 m (T01/T13 graphic geometry) against `MaxRouteLegKm` 50 and `MaxVertexFromTaskeeKm` 100. STP-852's READY-TO-TASK defer may hold an early task briefly and release it - a HOLD line is a HIT, an ABORT is a MISS. | HIGH | any dispatch-time TASKABRT |
| **P3** | **T01 and T13 issue NO VR-Forces task** and their taskees never move before their successors dispatch. `ExecutePlanPhase -> HoldInPlace`; the `TIMED COMPLETION: <unit> is idle again` line should appear for both. | HIGH | either taskee moves on T01/T13 |
| **P4** | **NO ROUTE SHIFT FIRES on any of the 3 driven legs.** The shift triggers only on a FLAGGED leg; measured ratios are 0.11 / 0.08 / 0.05 against the 0.92 threshold - clear by a factor of eight. The app must still log `LATERAL ROUTE SHIFT ON` (the setting is ON by user ruling) and then decline each leg. | HIGH | any leg detoured |
| **P5** | **NO WEAPON FIRES and no unit moves to contact.** `AffectedEntity == PerformingEntity` on both ATTACK tasks -> `SelfIsObjective` -> `ResolveAffectedTarget` returns null -> all five `FireAtTarget` sites are guarded; `ROEHold` on every task -> `Roe.HoldFire` pushed before the move. | HIGH | any `DtFireAtTargetTask` |
| **P6** | **T14 (the control) COMPLETES on arrival evidence**, not on its timer: 243 s of travel against a 300 s armed Duration. Its vendor completion and its arrival both land before sim 660 s. | MEDIUM | T14 fails to arrive |
| **P7** | *** REVISED UPDATE 2, and WEAKENED *** **T02 and T14 cannot exhibit STP-855: their taskees are PLATFORMS** (`isAggregate` False), and the race is a property of an AGGREGATE parent whose published position is read inside a re-compose transient. This also reconciles with the route-origin reviewer, who read these taskees as platforms - he was right. Iron Storm composes nothing from the init (`0 composed child(ren) held with their parent`). **BUT T10 IS NOT CLEARLY SAFE, and the earlier flat "STP-855 does not bite" is withdrawn:** under `CreationPolicy=AtOrder`, 1-112 IN's 6 members are created **at the moment the order first references it** - a create-then-task transient of the same family as the one STP-855 describes. **T10's `ROUTE ORIGIN` line is the one to watch.** | T02/T14 HIGH; **T10 moved to REPORT-NOT-SCORED** | a route origin > 50 m off the authored coordinate for T02 or T14 |
| **P8** | **No taskee is displaced at init.** De-stack is ON (`DeStackCreates` true, 700 m, `DeStackRotationDeg` 0) and moves 12 units, but `--destack-selftest` measures: *"9 of 9 taskee(s) found in the init, and NONE of them is moved by the 700 m de-stack - every taskee is the anchor of its own coordinate"*. The 12 moved records are DIFFERENT unit entries sharing the 53.992385/23.211255 pile. *** UPDATE 2: this HOLDS UNDER FidelityTable *** - the selftest runs the file in BOTH modes and reports the identical `36 placeable / 2 co-located group(s) / 12 moved / 0 composed child(ren)` in each, and 0 composed-sibling groups in both. **This upgrades the PART 1 prereg's ASSUMED "de-stack moves 28ID by at most 700 m" to VERIFIED-NOT-MOVED**, and it is what keeps sec 2's sector table valid. | HIGH | any of the 3 taskees starts off its authored coordinate |

### 6b. REPORT, NOT SCORED - measured and written down, with no band

- **P9 - the SIM/WALL RATIO.** Report every per-minute window with the PHASE it was measured in
  (creation / three-movers / tail / idle). **No band.** R9's 2.6-4.7x is a different scenario and
  COA-STP1's 0.27x is another; 36 objects is between them and nothing pins it. Do NOT divide wall
  by wall.
- **P10 - what the C16 watchdog does on a SINGLE PLATFORM.** *** REVISED UPDATE 2: the
  correction to sec 3 does NOT dissolve this. *** It decides on MEMBER displacement
  (`StallMoveMeters` 50 m net per member over the window, `StallMinMembersWithData`). Under
  FidelityTable **T10 has 6 members and is plainly covered; T02 (one M1A2) and T14 (one M577A2)
  are single platforms with none.** Whether the watchdog judges a memberless platform at all is
  UNKNOWN to this executor. A `STALL WATCHDOG: NO UNIT IS BEING JUDGED` line is a finding about
  the instrument. **Consequence worth stating plainly: the freeze instrument may cover only the
  leg with the WORST connectivity (T10, min 0.8182) and not T02 - so a T02 freeze might have to
  be read off frozen position reports against an advancing sim clock instead.**
- **P14 - the AtOrder member-creation transient on T10.** New with UPDATE 2. Report when
  1-112 IN's 6 members appear relative to its dispatch, and the `ROUTE ORIGIN` offset at that
  moment (see P7).
- **P11 - the driven path length against the authored line.** The mesh detours; by how much on
  this ground has never been measured. Report metres driven vs 5,341 / 2,617 / 2,426.
- **P12 - cruise speed on Baltic terrain.** The 10 m/s used throughout is a Mojave/Bogaland
  figure. Derive the real one on the SIM clock, never on wall time.
- **P13 - THE HEADLINE: do T02 and T10 cross their sub-0.9 sectors?** Deliberately unscored.
  There is no evidence either way in the record and inventing a band here would be the fifth
  band defect of the week. Report, per leg: metres driven, where the unit was when it stopped (if
  it stopped), and which sector that position falls in.

## 7. WHAT A FREEZE LOOKS LIKE, AND WHAT ELSE IT COULD BE

**FREEZE (the thing under test)** - ALL of:
- `STALL: unit <name> task <task>: no member moved more than 50 m in the last 360 SIM s` and a
  TASKABRT pushed with *"STALLED (C16 progress watchdog) - report only"*; and/or
- position reports repeating the SAME coordinate while the **sim clock advances** (read the
  `SIM/WALL RATIO` line to prove the clock is alive - a paused scenario is NOT a freeze and the
  task clock holds through it by design, Q5);
- the vendor's own console (level 3) showing the unit's move task still `TaskRunning`;
- `Giving up on movement task` / `BlockedBy*` rows from the vendor - **note that the vendor's
  base give-up test ALWAYS RETURNS FALSE by design, so its ABSENCE is not evidence of health.**

**NOT A FREEZE - the competing hypotheses, and what separates them:**

| hypothesis | discriminator |
|---|---|
| **Back-end death (STP-853)** - the interface reports a dead sim as healthy and keeps dispatching | `SIM/WALL RATIO` goes to exactly **1.000** (the task clock has fallen back to wall seconds), `BACK END` lines, `STALL WATCHDOG: STANDING DOWN - the back end is LOST`. **A ratio of exactly 1.000 under FFRTC is a DEATH SIGNATURE, not real time.** |
| **Scenario paused** | the sim clock is flat AND `BackendControlState` says paused; ratio 0.000; task time HOLDS and nothing completes early (Q5). Resume and the drive continues. |
| **Water** | 0 of 3 driven legs is wet at any stride tested. If a unit stops on water, the MESH routed it there - a finding about the mesh, not about the order. Record the CLCplus class at the stopping point. |
| **Nav data never loaded** | no `New Primary nav area` row at object console 3. Then the run measures the OFF-MESH fallback (a 1-part straight feature path) and says NOTHING about connectivity - **stop and re-launch**, do not interpret. |
| **Task ended on its timer, unit still driving** | `TIMED COMPLETION (R4)` for that task. Expected for T02 (sec 4). **Not a freeze and not an arrival.** |
| **STP-825 create failure** | the federation create was refused; `Start()==false` (STP-832). Nothing joined; re-launch behind the holder. |

**An unexplained symptom is a falsifier, not a footnote.** If T14 - the control - also fails to
drive, this run says NOTHING about the 0.82-0.90 band and the report must say so plainly.

## 8. STOP CRITERIA AND THE RUN CAP

- **ONE RUN.** No re-roll, no second value, no "try it again and see". If the run is spoiled by
  an environment fault (create refused, back end dead at start, nav data absent), that is a
  SPOILED run, not a result: fix the environment and re-register.
- **STOP the run** if: no `New Primary nav area` row by the time the init finishes; or the sim
  clock is unreadable / ratio pinned at 1.000 for two consecutive windows (STP-853); or the
  back-end process dies.
- **Cap** `--run-secs 2400`, plus the runner's own detached watchdog. Do not extend it live.
- **The 0.9 bar is NOT being reopened by this run.** Whatever it measures, changing the bar is
  the user's call. A session that disagrees writes one dissent line and proceeds.

## 9. WHAT EACH OUTCOME MEANS FOR THE DEMO

| outcome | reading | demo consequence |
|---|---|---|
| **All three legs drive to their destinations** (T14 control clean) | the 0.82-0.90 band is drivable on this ground | **FULL CUT A is viable.** The 0.9 bar was a margin, not a cliff, on this terrain - evidence for the user's decision, not a licence to move the bar. |
| **T14 drives; T02 and/or T10 freeze in a sub-0.9 sector** | the band is NOT drivable and the bar is doing its job | **FALLBACK: the T14-ONLY cut.** One M1A2 driving 2,426 m. Thin, and say so. |
| **T14 drives; T02/T10 stop somewhere that is NOT a sub-0.9 sector** | some other mechanism | the band question stays OPEN; diagnose the actual stop. |
| **T14 also fails** | the fixture, the build, the nav load or the environment is at fault | **no conclusion about connectivity.** Re-register after the cause is found. |
| **Nothing dispatches** | an intake/config fault (client id, server, create) | environment; re-run, not a finding. |

## 10. ASSUMPTIONS CARRIED FROM MOJAVE - every one flagged

1. **10 m/s cruise, applied to all three movers alike.** Measured on Mojave/Bogaland, NEVER on
   Baltic terrain. Every minute figure in sec 4 scales linearly with it and all are FLOORS
   (straight-line, before the mesh). *** UPDATE 2: this is now a THREE-WAY assumption, because
   the three movers are three different things *** - an M1A2 MBT (T02), a 6-vehicle Tank
   Headquarters Section whose slowest leaf sets the pace (T10), and a single M577A2 command-post
   track (T14). One speed for all three is a simplification, and T02's timed-completion
   arithmetic in sec 4 is the place it matters. Derive all three separately from the run (P12).
2. **The sim/wall ratio.** The 0.5-3.0x range is an INTERPOLATION between R9 (2.6-4.7x, near
   empty) and COA-STP1 (0.27x, 128 units). Not measured for 36 objects.
3. **The 0.92 slope threshold** was calibrated at elevation level L13. Suwalki resolves at L12,
   whose postings are coarser, so the 40 m sustained window is averaged over fewer of them: a
   real face reads LOWER and a flag can be MISSED (never invented). `leg_check` says so itself.
4. **The nav mesh routes sensibly on sub-0.9 ground.** Never observed. This is the whole question.
5. **`prune-no-go-areas True` keeps the lakes out of the mesh** at p=0.200000. Argued from the
   generation-2 contrast (at p=0.400000 the generator MESHES OVER the lakes - 11 sectors that are
   >= 95 % water went from 0 nodes to a mean 8.2 nodes and ratio 0.9773), not directly observed at
   p=0.2. Generation 1 is the fidelity-sound artefact; this run uses it.
6. **The C16 watchdog fires on the units in this run.** See P10 - unknown for single entities.

## 11. KNOWN DEFECTS SHIPPED DELIBERATELY IN THIS RUN

- **STP-847** - `SystemName` is the literal `Not Set`. Worked around on the CONSUMER side with
  `--client-id "Not Set"`; the defect stays visible in the data on purpose.
- **STP-846** - `ATTACK` is the connector's generic fallthrough. **Both** ATTACK tasks in the cut
  hit it: T02 (really "transitions to consolidation and security", live STP type
  `ReceiveOrderableActivity`) and T14 (really `FollowAndSupportFriendlyUnit`). Both become
  `TaskIntent.Attack`, whose composition LABEL reads "move-to-contact + fireAtTarget(affected)"
  and whose actual dispatch is `CreateRoute` + `MoveAlongRoute`. **Do not narrate either as an
  attack.**
- **The nav gate FAIL is the point of the run**, not an oversight: 7 corridor sectors below the
  0.9 bar, and the area-wide fragmented fraction 2.07 % against the ~1 % bar (that tail is
  lake-driven: fragmented sectors are 66 % wet, graph-less ones 96 %, the clean control 0.4 %).
- **The one recorded defect in the nav artefact:** the generator wrote the JUNCTION path into its
  own `.navRuntimeConfig` -
  `(nav-data-path "C:\Users\PAULOB~1\Temp\navi\navData\...")`. The junction EXISTS and resolves
  (verified this session: `C:\Users\PauloBarthelmess\Temp\navi` -> the durable directory) and the
  durable path is only 180 characters at its longest, well inside MAX_PATH. **Confirm the junction
  is alive immediately before launching.** If it is gone, the nav data will not load and the run
  measures the off-mesh fallback instead.

---

## THE SEAT'S ADDITIONS AT REGISTRATION

- BUILD: 1.0.0+git.51d59c0.Release-5.2 (= main 388cb71, the STP-852 + STP-855 build, plus one ledger-only commit; build
  report scratch validation/388cb71_build_report.md: GO, the Iron Storm dry run exits 0 with every option resolved and
  '[OK] init SystemName [Not Set] matches app clientId [Not Set]'). Verified live on R9 the same day: D10 (Way A) and the
  Way B re-check D5d. Iron Storm has NO composed parent (32 entities + 4 aggregates), so the STP-855 code is inert here:
  expect 36 planned, 4 empty shells, 32 platforms, and ZERO 'ROUTE ORIGIN' lines except possibly T10's (sec 6 P7 / P14).
- FIXTURE: C:\MAK\vrforces5.2d\userData\scenarios\IronStorm_Centre_52_Nav_AG.scnx, 6,561 B, sha256 d379dd68...aa4bb
  (the ONE file deployed under C:\MAK); nav data rides in tools\navdata\out\'MAK Earth (online) + IronStormCentre.mtf'.
- PRE-LAUNCH GATES (the seat's): the nav junction one-liner returns True; persistent holder RtiProbe 87616 (appNo 5065)
  joined; c2sim-server-vrf up on 18080/61614; no vrfSim* / vrfGui / VrfC2SimApp / observer / rtiAssistant; exe sha256 +
  ProductVersion before and after; another session's dotnet processes RECORDED, never touched; internet up (MAK Earth
  streams; the preflight tile cache for this AO is COLD, so HTTP tile fetches at dispatch are EXPECTED and the dispatch
  deferral is reported, not scored).
- CORRECTION to sec 9's table: the T14-only fallback is ONE M577A2 (48 IBCT under FidelityTable), not an M1A2.
- 'Vrf:StallClock=sim' has NEVER been exercised live (build report). If the watchdog misbehaves on the sim clock that is
  a finding about the instrument; the freeze reading then falls back to frozen position reports against an advancing
  sim clock, exactly as sec 7 already allows.
- One run. The harvest reader's verdict goes to the records; the seat's live reads are provisional.

---

## RESULT (run 20260921T114910Z_run, launch 11:49:09Z, RUN COMPLETE 12:33:31Z) - NOT SPOILED, BUT DID NOT ANSWER THE BAND QUESTION

Headline: the run is not spoiled by any sec 7 environment hypothesis - nav data loaded, the back end lived and ran fast
throughout, tiles fetched clean - but it did not measure what it was for. Two of the three driven legs, T02 (28ID) and
T14 (48 IBCT, the control), accepted their move-along tasks and then never moved a single metre in 40 minutes. The
cause is settled and it is NOT connectivity: all 36 init objects were created at FALLBACK altitude 0 because the
terrain-profile query went unanswered (MAK Earth had not streamed), leaving both platforms ~150 m under the ground,
and the vendor's movement controller silently declines to plan from there. The one unit that drove, T10 (1-112 IN), is
the only one deleted and re-created at order time, by which point the terrain had answered.

### Scored table (sec 6a)

| # | prediction | verdict | evidence |
|---|---|---|---|
| P1 | 36 created of 40, 4 empty shells, 32 platforms, only 1-112 IN grows members | **HIT** | C13 "4 unit(s) created as EMPTY shells ... 32 platform(s) created in full"; barrier "36 object(s) planned ... (4 empty shell(s))"; 6 members on 1-112 IN only; peak 42 objects |
| P2 | all 5 tasks dispatch, 0 dispatch-time TASKABRT | **HIT** | 5 DISPATCHED lines, 5 TASKSTRT, zero TASKABRT at dispatch (the barrier timed out instead - anomaly, sec below) |
| P3 | T01/T13 issue no VR-Forces task, `is idle again` for both | **HIT** | verb EXECUTEPLANPHASE -> HoldInPlace, "NO VR-Forces task is issued" and "is idle again" for both |
| P4 | no route shift fires on any of the 3 driven legs | **HIT** | `LATERAL ROUTE SHIFT ON`, then three declines, "ROUTE SHIFT - no leg flagged" |
| P5 | no weapon fires, no move to contact | **HIT** | zero `FireAtTarget`/`DtFireAtTargetTask` strings; "THE TARGET IS THE OBJECTIVE ... No fire ... is issued" x2 |
| P6 | T14 (the control) completes on arrival evidence, not its timer | **MISS** | T14 never moved; its only terminal report is TIMED COMPLETION at 326 s of a 300 s Duration - no arrival evidence exists |
| P7 | T02/T14 route origin within 50 m of authored; zero ROUTE ORIGIN lines | **HIT** | zero ROUTE ORIGIN lines anywhere; T02/T14/T10 origins all 0 m off authored |
| P8 | no taskee displaced at init | **HIT** | 28ID is the de-stack anchor kept in place (proven to 1e-14 by its own route origin); all three taskees start on their authored coordinates |

7 HIT / 1 MISS. The single MISS is the control leg, and under sec 7 an unexplained symptom is a falsifier, not a
footnote - the cause chain below explains it.

### The headline per leg

- **T02 (28ID, M1A2, authored 5,341 m): 0 m driven.** Dispatched, route built, origin 0 m off authored, but position
  identical across all 243 reports, speed 0.0 throughout. TIMED COMPLETION at sim ~663 s. Never reached any of its
  three sub-0.9 sectors. This leg tells us nothing about the band.
- **T14 (48 IBCT, M577A2, authored 2,426 m, THE CONTROL): 0 m driven.** Same shape as T02 - 0 m, speed 0.0, TIMED
  COMPLETION, STALL. The control failed; under the prereg's own sec 2 rule that is a stop, not a finding about
  connectivity - and its sectors were all >= 0.9107 anyway, so its failure is unrelated to the band by construction.
- **T10 (1-112 IN, 6-member Tank HQ Section, authored 2,617 m): THE ONLY DRIVE.** Aggregate track 2,666 m for 2,615 m
  net displacement (+1.9% over the authored line, no detour); arrived 2.9 m from the last vertex, 32/48-style arrival
  evidence closing at 3,207.7 SIM s after dispatch (vendor completion at 3,831 SIM s). 54.7% of the 2,617 m leg lies
  inside four sub-0.9 sectors (0.8182-0.8868) and the unit crossed every one of them without freezing - the first
  drive in this project's record anywhere between 0.5 and 1.00 connectivity. IT CRAWLED: 13.31 m/s peak on the
  opening cruise, then 48 consecutive reports at a sustained 0.22-0.30 m/s (median 0.28), task average 0.83 m/s - a
  factor of 2.0 above the C16 stall threshold (0.139 m/s). Cause of the crawl NOT adjudicated: 31 `BlockedByVehicle`
  rows across the six-vehicle column is the simplest candidate; one `Global Replan` event near the end is a second;
  aggregate size alone is refuted by D10's own comparators (a 48-member company on Bogaland ran faster, 1.83 m/s,
  than this 6-member section's 0.83 m/s).

### The cause chain for T02 and T14 - CAUSE SETTLED, STP-856

Condition VERIFIED, vendor-internal mechanism ASSUMED. The chain, every link read rather than inferred: the init's
terrain-profile request for all 36 create positions got no reply within 10 s, so every object was created at the
FALLBACK altitude with no terrain height available; the PLACEMENT summary confirms 0 of 36 create altitudes came
from the terrain query; READY TO TASK was NOT REACHED (0 of 36 bound when the 20 s cap expired), and the order was
pushed anyway. At dispatch the app measures the consequence itself: T14's live altitude reads -0.0 m against 145.4 m
of terrain (a 145 m gap); T02's reads -0.0 m against 155.8 m (a 156 m gap) - both platforms sit roughly 150 m under
the ground. The vendor accepts the move-along task, logs six lines of setup, and then emits total silence for the
rest of the run - no plan-path node, no BlockedBy, no Giving-up, nothing. The contrast inside the same run: T10 (the
one order-referenced aggregate) is deleted and re-created at order time, by when the terrain answered, so it was
placed at 131.1 m from the terrain query and its six members drove normally.

Four alternatives were checked against a direct discriminator and each is FALSIFIED: (a) that a move-along on a
single platform proxy is rejected on this fixture/build - refuted twice, once by D10's own single-platform taskee
driving and arriving on the same binary the same morning, and once inside this very run by the same two vehicle
templates (M577A2, M1A2) driving as members of T10; (b) that the platforms' start points sit in a disconnected mesh
island - both received "New Primary nav area" after their move-along was issued, so they are inside the nav area;
(c) that the origin-vertex drop left a degenerate route - both routes measure to their authored lengths, 0 m off
authored, zero ROUTE ORIGIN lines; (d) that they did move but their reports are frozen - three independent channels
(WatchVrf trace, C2SIM position reports, the C16 watchdog) all read exactly zero while the same channels show T10
moving 2.6 km at up to 13.31 m/s. Back-end death and a paused scenario are also excluded (no 1.000 ratio, no LOST,
no dmp/callstack; the sim clock ran continuously 0 -> 24,028 s). **Filed as STP-856**: units created under the
terrain on a cold streamed area, then tasked anyway, silently never move; fix lane fix/terrain-readiness-and-reclamp
in progress. The symptom that remains genuinely unexplained: the vendor emits no diagnostic at all for a buried
platform - no refusal, no "no path". The condition is verified; the internal mechanism by which VR-Forces silently
declines to plan from such a point is assumed, not evidenced by any vendor line in this run.

**OPERATIONAL WARNING:** the registered fallback in sec 9 (the T14-only cut, one platform driving 2,426 m) is ALSO
BROKEN by STP-856, because 48 IBCT is precisely one of the two init-created platforms that never moved. A T14-only
cut on today's launch path would show the audience a stationary vehicle.

### P9-P14 (sec 6b, reported without bands)

- **P9, the clock, needs a correction to the seat's mid-run read.** Movement phase (dispatch to last mover stopping):
  ratio 6.61-8.58x, median ~7.14x. Creation phase: 8.58x. Idle tail (after all tasks end): 8.42-11.41x, median ~10.8x.
  The seat's live "~11x" was the idle tail, not the drive - every sim-clock duration in the movement phase converts
  at ~7.1x, not 10.9x. The registered assumption of 0.5-3.0x was low by a factor of 2.4 to 3.8: 36 mostly-idle
  objects run FASTER than R9. Record this as the SECOND measured clock profile in RUNBOOK 11f (fast, variable,
  load-dependent, same family as R9's - the user has ruled FAST for the demo clock generally).
- **P10, settled: C16 DOES judge a single memberless platform.** Both T02 and T14 got their own STALL verdict
  ("no member moved more than 50 m in the last 360 SIM s, max 0.0 m") - there is no "NO UNIT IS BEING JUDGED" line.
  `Vrf:StallClock=sim` worked correctly on its first live outing, measured on the simulation clock as designed.
  **NEW FINDING, filed as STP-857:** both stall TASKABRTs were SUPPRESSED by the emission rules ("already reported
  for this task, or the task has already completed") because the armed-end timer had already pushed TASKCMPLT
  first. The C2SIM bus therefore told STP that both units COMPLETED their tasks and was never told they had
  stalled - for a unit that moved 0 m of a 5,341 m leg, the bus carries only a success. This needs a RULING on
  completion semantics (do not let a timer beat and silence a stall verdict); options are in the ticket.
- **P11, driven path vs authored.** T10: +1.9% aggregate (2,666 m of 2,617 m), +4.0% to +10.6% per member; no
  detour observed. T02 and T14: 0 m of 5,341 m and 2,426 m respectively.
- **P12, cruise speed on Baltic terrain.** The registered 10 m/s is right only for T10's opening burst (13.31 m/s
  peak, even higher). Sustained speed on the crawl: 0.28 m/s; task average 0.83 m/s. For T02 and T14: undefined,
  they never moved - the prereg's sec 4 arithmetic is void for this ground.
- **P13, the headline (deliberately unscored): does T02/T10 cross their sub-0.9 sectors?** T10 crossed all four of
  its sub-0.9 sectors (0.8182-0.8868) end to end without freezing. T02 never reached any of its three sub-0.9
  sectors, so it says nothing either way.
- **P14, the AtOrder member-creation transient on T10.** Members reflected 28.3 wall s before dispatch; route
  origin offset 0 m, no ROUTE ORIGIN line printed - the STP-855-family transient the prereg worried about did NOT
  bite here, and this same order-time materialization is what incidentally saved T10's leg from STP-856 (it got a
  real terrain altitude precisely because it was re-created at order time, unlike the two init-created platforms).

### Outcome row (sec 9)

This run lands in **"T14 also fails -> the fixture, the build, the nav load or the environment is at fault; no
conclusion about connectivity; re-register after the cause is found."** The cause is now found: the environment/
placement path (STP-856), not the nav data and not the build. The 0.9 bar is NOT reopened by this run.

### CAN claim

A 6-vehicle aggregate drove a 2,617 m leg, 54.7% of it inside sectors of connectivity 0.8182-0.8868, entered and
exited all four, took no detour (+1.9% path) and arrived within 2.9 m of its destination. The 0.82-0.89 band did
not stop it - the first drive in this project's record anywhere between 0.5 and 1.00 connectivity.

### CANNOT claim

Anything about T02's three sub-0.9 sectors (it never reached them); anything about single platforms on sub-0.9
ground (neither single platform moved); anything at all from the control, which is what the design relied on. The
band question remains OPEN.

### NEXT

Worth its cost, one run one variable: make the init creates get terrain altitudes (pre-warm MAK Earth for the AO
before the init is pushed, gated on the app's own "PLACEMENT summary: N of N ... from the TERRAIN QUERY" line
reading 36 of 36; or raise the init terrain-profile timeout above 10 s; or hold the order until READY TO TASK
reports units bound). Pre-register one scored limb: "PLACEMENT summary: 36 of 36 ... from the TERRAIN QUERY" and
"READY TO TASK - 36 of 36 bound". If met and T02/T14 still do not move, STP-856's hypothesis is falsified and the
vendor case changes shape; if met and they DO move, the run simultaneously delivers T02's leg (5,341 m with 840 m
of sub-0.9 including the 0.8800 sector) and the single-platform control the prereg was built around - two
registered questions for one run. A WASTE: re-running cut A unchanged (it would reproduce this exactly);
regenerating the nav data (the mesh worked, 22-33 point paths planned); re-tiling; anything aimed at the 0.9 bar.

### VERIFIED vs ASSUMED (the harvest's closing section, in sense)

VERIFIED: nav data loaded for 19 objects including both non-movers, with the abstract-graph planner producing
22-33 point paths for the movers; the back end alive end to end with no 1.000 ratio, no LOST, no fault artefact;
the clock by phase (8.58x creation, 6.61-8.58x movement, 8.42-11.41x idle tail); 0 tile hits / 20 fetches / 0 given
up; P1-P5/P7/P8 HIT, P6 MISS, exact creation census; T02 and T14 moved exactly 0 m on three independent channels;
both created at fallback altitude 0 of 36, both reading -0.0 m against 145.4 m and 155.8 m of terrain at dispatch;
T10 re-created at order time at 131.1 m from the terrain query and driving normally; all four competing hypotheses
for T02/T14 falsified by a direct discriminator, one of them twice; C16 judging single platforms with
`StallClock=sim` correct on its first outing, and both stall TASKABRTs suppressed by the already-completed timer;
T10 crossing its four sub-0.9 sectors end to end, +1.9% path, arriving within 2.9 m, one stop over 30 SIM s,
sustained speed 0.28 m/s, task average 0.83 m/s; 31 BlockedByVehicle rows and one Global Replan event on the
movers; clean teardown, hashes unchanged, the foreign build load outside the movement window.

ASSUMED, not evidenced by this run: the internal mechanism by which VR-Forces silently declines to plan a path for
an entity ~150 m below the terrain surface - the condition is verified, the vendor names no such line, and it is
possible some other correlated property of an init-created object is the real blocker; only the NEXT run separates
them. That the T10 crawl is caused by inter-vehicle blocking rather than the sub-0.9 ground or aggregate handling -
three candidates listed, none adjudicated. The exact sector identity of the crawl's onset (reported as a distance
and coordinate, not a sector name, because the prep report's grid could not be reproduced from the published
lat/lon box). D10's single-platform/platoon/company speed comparators bound the order of magnitude on different
terrain and a different fixture; they are not an exact expectation for this ground.

Also recorded: the persistent holder was re-armed at 11:26Z (RtiProbe 87616, appNo 5065, joined on attempt 1;
5066-5068 burned); an overlapping re-arm attempt is currently REFUSED by name by
`scripts\StartFederationHolder52.ps1` while another RtiProbe exists - queued as a follow-up, an explicit switch so
a re-arm can JOIN instead of attempting a CREATE; the fixture deploy manifest (one file under C:\MAK, sha256
d379dd68...aa4bb) matches the prep report exactly.
