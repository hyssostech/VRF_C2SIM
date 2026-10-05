# IRONSTORM FULL - the whole narrative, what was derived, and what each task will do

`data/IRONSTORM_FULL_Order.xml` is DERIVED, not authored. It is produced by
`tools/scenario/derive_ironstorm_cuta.py --variant full` (2026-10-04, lane FD; RL-20261004-04 "demo first")
from the user's authoritative STP export

    data/STP-IRON-STORM-SYNTHETIC_Initialization.xml   sha256 2000e856cb00314064ab6d40c7f7df64cea098b3466fe70d7614f3d26dc93eec
    data/STP-IRON-STORM-SYNTHETIC_Order.xml            sha256 33dc899c734861f8cbae95ef3d36b77b15ba1081ee3c7ef823b80c597d99a5b4

and reproduces byte for byte:

    data/IRONSTORM_FULL_Order.xml                      sha256 cf3d0df83e4ab0523156d2eb53d3c57c923448330b9a8f1f9491f53c9dba7e9d
                                                       (115,959 B, the CRLF working-tree file `--check` reads)

NO INIT FILE IS WRITTEN for it. The init needs no change (it carries no IsoTimeDuration and no task; its
SystemName stays "Not Set", change (d)), so the run uses the export's init - or, identically,
`data/IRONSTORM_CUTA_Initialization.xml`, which is a byte-identical copy (both 2000e856...).

`--check` verifies this file beside the cut-A pair and the E1 variant on every run; adding the full variant
left all three cut-A hashes unchanged (2000e856..., 7a986137..., 7d5b4034...). The script ALSO asserts on
every run that the full order EXTENDS cut A: with every `<Task>` block removed the two orders are
byte-identical, and each of cut A's five task blocks occurs verbatim, once, in the full order. So everything
recorded in `data/IRONSTORM_CUTA_CHANGES.md` about T01, T02, T10, T13, T14 and the graphics holds here
unchanged. Read that file for the long-form evidence behind (a)-(j); this one says what differs.

THE ORDER: all 23 tasks of the export EXCEPT T12 - 22 tasks, 8 taskees. All 33 export graphics are kept
and the 3 cut-A graphics ((h), (i), (j)) added: 36 `Entity` blocks, 38 MapGraphicIDs (export 35, minus T12's
2, plus (b)'s 2 and (h)/(i)/(j)'s 3). Root element, namespace, declaration and CRLF are the export's.

--------------------------------------------------------------------------------
## The complete change list

### (a) Duration format - connector bug STP-848 - ALL 44 values

Every `IsoTimeDuration` is rewritten from the export's short ISO form to the C2SIM 1.1 pattern form. NO
VALUE CHANGES - only the spelling.

| export | derived | count |
|---|---|---|
| `PT0S` | `P00Y00M00DT00H00M00S` | 18 |
| `PT10M` | `P00Y00M00DT00H10M00S` | 1 (T06's start delay) |
| `PT20M` | `P00Y00M00DT00H20M00S` | 22 |
| `PT30M` | `P00Y00M00DT00H30M00S` | 2 (T10, T19 Durations) |
| `PT50M` | `P00Y00M00DT00H50M00S` | 1 (T18 Duration) |

44 = the export's 46 minus T12's two (asserted). WHY: the schema's `IsoTimeDurationBaseType` pattern
requires every field, two digits wide (`C2SIM_SMX_LOX_CWIX2024.xsd:17-24`); `OrderParser.FindTotalIsoMs`
refuses the short form, so as exported EVERY task has no Duration and every hold is MALFORMED - the survey's
RAW column below shows what that costs. The fix belongs in STP's connector.
NOTE 2026-10-04 (RL-20261004-06 (3)): the interface now ALSO accepts the short form - `FindTotalIsoMs` decodes
`PT20M` to the same milliseconds as its pattern twin, with ONE order warning naming the values non-conforming
C2SIM 1.1 (STP-848). "Refuses" and the RAW column describe the decoder before that change; the raw export now
decodes 46 of 46. Change (a) stays: this derived order remains schema-valid and decodes as before (44 of 44).

### (b) Two MapGraphicID references added - unchanged from cut A

T02 -> `c8cde9b6` PassagePoint_28ID_SLOT0 and T10 -> `cc23071f` PassagePoint_48_IBCT_SLOT0. Both tasks
carry no geometry in the export and would execute in place. No new geometry. The inserted lines keep their
`ADDED BY CUT A` comments ON PURPOSE: it is the same change, and it keeps the cut-A task blocks verbatim.

### (c) The order reduced - REPLACED: every task but T12

Cut A kept 5 tasks. The full variant keeps 22. Chain integrity is asserted exactly as for cut A: every kept
task's predecessor is kept (T12 is a chain root with no successor). The script also asserts the export still
carries exactly the 23 task uuids it lists (`EXPORT_TASKS`), in order, before deriving anything.

**DROPPED - T12** `c6913c98-...`, 116 ABCT, DESTRY. WHY: a 52.6 km route (first leg 44.9 km, farthest
vertex 48.8 km) to Alytus over terrain tiles that are NOT CACHED (survey DR1, `cacheFrac` 0.0 on every leg) -
tier 2. It is the only mover the survey could not predict (UNKNOWN(terrain)), it would populate a 26-member
brigade, and no task depends on it. 116 ABCT stays in the init and stays T14's second AffectedEntity (f).

### (d) SystemName LEFT AS EXPORTED - connector bug STP-847

`Not Set`; the run passes `--client-id "Not Set"` (the runner's Iron Storm default does it). Unchanged.

### (e) T14's destination 799 m west, out of a lake - unchanged

The pair still occurs exactly twice (the FollowAndSupport graphic vertex and T14's embedded Location); no
other kept task names it. USER RULING 2026-09-21.

### (f) T14 ATTACK -> FOLSPT, + 116 ABCT as a second AffectedEntity - unchanged

OWNER DIRECTION 2026-09-26. STP-846 family case mismatch.

### (g) ExecutePlanPhase -> CNFPSL on T01, T13 AND T03

OWNER DECISION 2026-09-26 ("Patch + Jira") for T01 and T13. **T03 is added here, and (g)'s reasoning covers
it:** T03's STP source (`STP-IRON-STORM-SYNTHETIC_Narrative1.op`, task `0b957957`, 56 SBCT) is the THIRD
`ConductFwdPassageOfLines` task, with exactly the same `how: 'PASSAGE_OF_LINES'`, `what: 'NOT_SPECIFIED'` as
T01's and T13's - read from the `.op` this session (the byte copy in `D:\C2SIM-preserve\ironstorm-narrative1\`).
The connector's `How.ToLower()` vs upper-case case label (C2SimTask.cs :609 / :645-646 / :668) turns all three
into ExecutePlanPhase. Cut A only left T03 out because it dropped the task.
AT RUN TIME this changes nothing observable: the deployed interface dispatches CNFPSL exactly as
ExecutePlanPhase - held in place, no vendor task, completion by time (`TaskDispatchPolicy.cs:61-62`, owner
RL-20260926-01 A6, STP-866). (The cut-A file's "until the CNFPSL verb lands ... must not go to a live run"
warning is out of date on that point: the verb is mapped, `VerbMapping.cs:167`.)

### (h), (i), (j) T10's reroute and pond waypoint, T14's lake waypoint - unchanged

The three added Line/Route graphics and their references, byte-identical to cut A. T10 drives 2,775 m,
T14 4,169 m (resolved by `leg_check.py --dump-resolved` on this file: 2.8 km / 4.2 km).

### Not done - compositions

NO composition rows are added (tier 2, the owner's call is pending). Five taskees therefore have none - see
the table. [2026-10-04 TIER 2A, RL-20261004-06: three of the five are now composed (56 SBCT, 278 ACR, 169
FAB); 55 MEB and 11 CAB stay refused - see "TIER 2A" at the end. The order file is unchanged.]

--------------------------------------------------------------------------------
## Verification (2026-10-04, offline only - nothing launched)

- `derive_ironstorm_cuta.py --check`: init, cut-A order, E1 order, FULL order all match; gates (ASCII, CRLF,
  STP-830 comment scan, well-formed, root, namespace, duration pattern) pass; full-extends-cut-A asserted.
  `--selftest` PASSED (new FULL section included).
- lxml against `C2SIM_SMX_LOX_CWIX2024.xsd`: export order 46 violations (all IsoTimeDuration); FULL order
  **0 - VALID** (cut-A order also 0).
- Deployed `VrfC2SimApp.exe --parse-order` (build of 2026-10-04 15:16): **Tasks: 22**, no WARN line,
  **durations present: 22 of 22** (19 x 1,200,000 ms, 2 x 1,800,000, 1 x 3,000,000), start delays 3 non-zero
  (T06 600,000 ms; T10 and T23 1,200,000 ms), verbs as derived (3 x CNFPSL, FOLSPT on T14), every STREND
  `startAfter` as in the export. `--parse-init` on the export init with clientId "Not Set": 40 units,
  **36 would be created** (the 4 without a location are skipped - as for cut A).
- `composition_check.py` with its coverage gate pointed at this order (the tool has no `--order` switch; a
  scratch wrapper changed only `gate_coverage`'s default `order_path`): FAILS coverage, as predicted, on
  exactly 5 performers in both variants - 56 SBCT `F-UCAW-H`, 278 ACR `F-UCRVA-G`, 55 MEB `F-UCE-H`,
  169 FAB `F-UCF-H`, 11 CAB `F-UCVRA-H`. Composed: 28ID, 1-112 IN, 48 IBCT (+ 116 ABCT, affected only).
  `typemap_check.py` (takes the init, not the order): PASS.
- Route extents from the resolved geometry against `Vrf:MaxRouteLegKm` 50 / `Vrf:MaxVertexFromTaskeeKm` 100
  (appsettings defaults): T15 first leg **98.8 km**, farthest vertex **102.7 km** (from T14's (e)
  destination); T23 first leg **74.2 km** (farthest 78.0 km). Every other mover is inside both limits.

--------------------------------------------------------------------------------
## Predicted outcome per task (survey DR1, 2026-10-04, against the deployed build)

Timing at `Vrf:DurationScale` 0.25 (sim seconds). REPORTED = refused at dispatch with a TASKABRT and the
reason logged; skipped = its STREND predecessor was refused, so the gate reports it TASKABRT
"skipped/abandoned upstream". RAW = the export as is (no (a)).

| T | taskee (echelon) | verb as derived | timing | geometry driven | composition | PREDICTED | reason reported | RAW |
|---|---|---|---|---|---|---|---|---|
| 01 | 28ID (DIV) | CNFPSL -> hold | 0-300 | 4 graphics, not driven | 1 (HQ) | EXECUTES (hold) | - | REFUSED malformed |
| 02 | 28ID | ATTACK -> advance (STP-846) | after 01 | (b) 5.3 km | 1 | DEGRADED | moves; on-screen verb says ATTACK, no fire (ROEHold, self-target) | REFUSED malformed |
| 03 | 56 SBCT (BDE) | CNFPSL -> hold | 0-300 | 29.6 km, not driven | NONE | EXECUTES (hold) | - | REFUSED malformed |
| 04 | 56 SBCT | FIX -> advance | after 03 | 36.7 km, max leg 23.5 | NONE | **REPORTED** | no composition: container stays EMPTY, MOVE refused (RL-20260927-03) | skipped |
| 05 | 56 SBCT | RETAIN, no geometry | after 04 | - | NONE | skipped | predecessor T04 refused | skipped |
| 06 | 278 ACR (RGT) | SCREEN -> patrol | delay 150 | 44.3 km, max leg 19.7 | NONE | **REPORTED** | no composition | REFUSED (no comp) |
| 07 | 278 ACR | SCREEN, no geometry | after 06 | - | NONE | skipped | predecessor T06 refused | skipped |
| 08 | 278 ACR | BREACH | after 07 | 12.7 km | NONE | skipped | predecessor chain refused | skipped |
| 09 | 278 ACR | SCREEN | after 08 | 35.7 km | NONE | skipped | predecessor chain refused | skipped |
| 10 | 1-112 IN (BN) | CRESRV -> bare move | delay 300, dur 450 | (b,h,j) 2.8 km | 5 | DEGRADED | moves as a bare move (no reserve verb) | REFUSED malformed |
| 11 | 1-112 IN | CRESRV, no geometry -> in place | after 10, 300 | - | 5 | EXECUTES (in place) | - | skipped |
| 13 | 48 IBCT (BDE) | CNFPSL -> hold | 0-300 | 29.6 km, not driven | 17 | EXECUTES (hold) | - | REFUSED malformed |
| 14 | 48 IBCT | FOLSPT (f) | after 13 | (e,i) 4.2 km | 17 | EXECUTES (G1-5) | - | skipped |
| 15 | 48 IBCT | CLRLND -> bare move | after 14 | leg 98.8 km, vertex 102.7 km | 17 | **REPORTED** | STP-833 route extent (leg > 50 km and vertex > 100 km) | skipped |
| 16 | 48 IBCT | SEIZE | after 15 | 19.9 km | 17 | skipped | predecessor T15 refused | skipped |
| 17 | 48 IBCT | RETAIN, no geometry | after 16 | - | 17 | skipped | predecessor chain refused | skipped |
| 18 | 55 MEB (BDE) | RETAIN, no geometry -> in place | 0-750 | - | NONE | EXECUTES (in place) | - | REFUSED malformed |
| 19 | 55 MEB | SECURE -> bare move | after 18 (750) | 2.5 km | NONE | **REPORTED** | no composition | skipped |
| 20 | 55 MEB | SECURE, same point | after 19 | 0 m | NONE | skipped | predecessor T19 refused | skipped |
| 21 | 55 MEB | SECURE, same point | after 20 | 0 m | NONE | skipped | predecessor chain refused | skipped |
| 22 | 169 FAB (BDE) | OCCUPY area | 0 | 14.1 km closed loop | NONE | **REPORTED** | no composition | REFUSED (no comp) |
| 23 | 11 CAB (BDE) | DESTRY | delay 300 | leg 74.2 km | NONE | **REPORTED** | STP-833 route extent (leg > 50 km); 11 CAB also has no composition | REFUSED |

Totals: 6 execute (3 holds, 2 in place, T14) + 2 degraded movers (T02, T10) = 8 run; 6 REPORTED (T04, T06,
T15, T19, T22, T23); 8 skipped (T05, T07-T09, T16, T17, T20, T21). Members populated: 23 (1 + 5 + 17), the same as cut A - T12's 26 leave with it.
The only units that MOVE are cut A's three; the rest of the narrative shows as holds and reported refusals.
NOT VERIFIED offline: which of T23's two reasons the log names first (the survey says STP-833); the outcomes
themselves are the survey's reading of the code, confirmed here only as far as `--parse-order`, the
composition gate and the route extents reach.

### Timeline at 0.25 (travel at an ASSUMED ~10 m/s, as cut A)
0 s: T01/T03/T13 holds and T18 start; T22 reported. 150 s: T06 reported (T07-T09 skipped). 300 s: holds end;
T02, T10, T14 move; T04 reported (T05 skipped); T23 reported. ~580-750 s: T10 ends, T11 holds 300 s.
750 s: T18 ends, T19 reported (T20, T21 skipped). ~820-835 s: T14 and T02 arrive; T15 reported (T16, T17
skipped). Last: T11, about **880-1,050 s (15-17.5 min)** - against cut A's 13.9 min.

### Demo command line
Cut A's G1-5 line with ONE swap: `--order data/IRONSTORM_FULL_Order.xml`. `--init
data/IRONSTORM_CUTA_Initialization.xml` stays (byte-identical to the export's). `--run-secs 2700` stays: a CAP,
and the sim span is ~17.5 min at 0.25. `--stop-when-complete` now waits for 8 taskees / 22 terminal reports;
every refused or skipped task sends a TASKABRT, so it still closes. Scenario `IronStorm_Centre_52_Aggregate`
stays: the init (hence every created unit) is the same as cut A's, and the only movers are cut A's three,
inside the Centre area. [Before tier 2a - no longer true: see TIER 2A below.]

--------------------------------------------------------------------------------
## TIER 2A - 2026-10-04 (RL-20261004-06, owner Q1858) - three compositions added; the table above is kept as the pre-tier-2a prediction

WHAT CHANGED: `data/unit-composition-52-aggregate.json` only (plus `composition_check.py --order`); the order
and init files are byte-unchanged. Rows from public doctrine (FM 3-96 Jan 2021; ATP 3-09.24 Mar 2022), never
the real units' 2026 MBCT conversions:

- 278 ACR (`F-UCRVA-G`, container RGT, Reconnaissance PA) - the ABCT pattern: HQ + 2 Armor CAB Group + the
  mech-heavy CAB + cavalry squadron; authored adds FA BN (ABCT), BEB (ABCT), BSB. 26 / 29 members.
- 56 SBCT (`F-UCAW-H`) - HQ + 3 x Motorized BN (POL, Rosomak) as a LABELLED STAND-IN for the Stryker infantry
  battalions + Stryker Cavalry SQDN (USA); authored adds FA BN (IBCT) and BEB (ABCT) - both labelled
  stand-ins - and the BSB. 5 / 8 members.
- 169 FAB (`F-UCF-H`) - HHB + ONE cannon BN + ONE HIMARS BN (three batteries of MRL BTY PA (USA, HIMARS)).
  TWO battalions: ATP 3-09.24 1-35 allows one to five, 1-6 gives a National Guard FAB cannon as well as
  rocket battalions; one of each is the fewest that shows both, at the lowest load. Cannon = FA BN (USA,
  ABCT) authored / 3 x FA BTY PA (USA, 155mm) catalogue; authored adds the BSB. 19 / 12 members.
- 55 MEB and 11 CAB stay REFUSED by ruling (reasons in the composition file's notes: the MEB has no fixed
  structure, FM 3-81 2-2/2-25; the catalogue has no US Army helicopter unit, FM 3-04 2-9).

TASK BY TASK against the table above (offline: `composition_check.py --order data/IRONSTORM_FULL_Order.xml`
covers 6 of 8 performers; before tier 2a 3 of 8):

| T | taskee | table above | after tier 2a | what decides it now |
|---|---|---|---|---|
| 03 | 56 SBCT | EXECUTES (hold), no comp | EXECUTES (hold), populated | - |
| 04 | 56 SBCT | REPORTED no composition | PASSES the guard - moves 36.7 km | route wholly off the cached tiles: UNKNOWN (terrain) |
| 05 | 56 SBCT | skipped | follows T04 | in place after T04 |
| 06 | 278 ACR | REPORTED no composition | PASSES - patrol 44.3 km | off-cache: UNKNOWN (terrain) |
| 07-09 | 278 ACR | skipped | follow T06 (T08 BREACH 12.7 km, T09 35.7 km) | off-cache |
| 22 | 169 FAB | REPORTED no composition | PASSES - OCCUPY, 14.1 km loop | off-cache |
| 19 | 55 MEB | REPORTED | REPORTED - no composition (ruled) | - |
| 20, 21 | 55 MEB | skipped | skipped | - |
| 23 | 11 CAB | REPORTED | REPORTED - STP-833 extent + no composition (ruled) | - |

Every other row of the table is unchanged. NOT PREDICTED offline: how the off-cache moves end (no tiles, no
pre-flight) and how a patrol (T06) ends. MEMBERS POPULATED (full order): catalogue variant 23 -> 73 (+5 SBCT,
+26 ACR, +19 FAB); authored 63 (1 + 5 + 8 + 8 + 29 + 12).

ROUTES OUTSIDE THE DEPLOYED PREFLIGHT-CACHE (53.93-54.12 N, 23.09-23.42 E; DR1 resolver output): ALL of the
newly enabled ones - T04 54.22-54.40 N, 23.76-24.04 E; T06 54.19-54.36 N, 23.59-23.90 E; T08 54.31-54.36 N,
23.71-23.75 E; T09 54.29-54.39 N, 23.62-23.88 E; T22 54.13-54.17 N, 23.07-23.13 E. The three taskees are
also created outside it (56 SBCT 54.217 N 23.764 E, 278 ACR 54.190 N 23.585 E, 169 FAB 54.150 N 23.100 E), so
their member rings are placed outside the cached water tiles too. One box covers all: 54.13-54.41 N,
23.06-24.05 E (the tile fetch is a later step).

CHECKS (2026-10-04, offline): `composition_check.py` PASS; `--selftest` PASS (54 checks, was 42: 9 new pins,
the stand-in WRONG NATION control, 55 MEB and 11 CAB refused on the full order); `typemap_check.py` PASS. The
DEPLOYED exe (LBL pin, 2a5406e5...) on this data: `--parse-order` 22 tasks, 22 of 22 durations;
`--populate-selftest` 222 PASS / 3 FAIL - the row-id lists and the identity hash it pins, which this commit's
`ContainerSelfTest.cs` updates; the same source built to a scratch output: 230 PASS / 0 FAIL (authored 233 /
0). NEEDS A MANAGED REBUILD before the deployed selftest is green again.
