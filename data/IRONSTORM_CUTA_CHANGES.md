# IRONSTORM CUT A - what was derived, and what it costs

`data/IRONSTORM_CUTA_Initialization.xml` and `data/IRONSTORM_CUTA_Order.xml` are
DERIVED, not authored. They are produced by
`tools/scenario/derive_ironstorm_cuta.py` from the user's authoritative STP export

    data/STP-IRON-STORM-SYNTHETIC_Initialization.xml   sha256 2000e856cb00314064ab6d40c7f7df64cea098b3466fe70d7614f3d26dc93eec
    data/STP-IRON-STORM-SYNTHETIC_Order.xml            sha256 33dc899c734861f8cbae95ef3d36b77b15ba1081ee3c7ef823b80c597d99a5b4

and reproduce byte for byte:

    data/IRONSTORM_CUTA_Initialization.xml             sha256 2000e856cb00314064ab6d40c7f7df64cea098b3466fe70d7614f3d26dc93eec
    data/IRONSTORM_CUTA_Order.xml                      sha256 7a9861372f07702fc136f91f63b8bf971650fcc91c20aa62803d73cfd869f5c7
    data/IRONSTORM_CUTA_E1_Order.xml                   sha256 7d5b4034eef0ecf0f33946c6f30b60aaff35e640a86b6b16c9c24f02cb3ddb9a
                                                       (the VARIANT "--without i" for run E1 - section "E1" below)

(2026-09-26, twice. First corrected: this line said 1cd89c40bcce5d66...3956636, a stale hash; the record
does not say which derivation produced it. `derive_ironstorm_cuta.py --check` on main 8cdca96 reports
5dbe8b0b...b980d1, the hash of the committed file and the one PREREG_IRONSTORM_DRIVE_2026-09-21.md:53 cites -
that is the order BEFORE (f) and (g). Then changes (f) and (g) below were applied, and `--check` on the result
reports 1ef43198...c79429. Any record that cites 5dbe8b0b describes the pre-(f)/(g) order.
Then change (h) (T10 reroute, 2026-09-26, lane I1d) was applied; `--check` reports 3801c71b...79fefe8. A record
citing 1ef43198 describes the pre-(h) order. Then change (i) (T14 routed round a lake, 2026-09-27, lane T14)
was applied; `--check` reports 5d6bbae4...49fc9a. A record citing 3801c71b describes the pre-(i) order - the one
IRONSTORM_CUTA_LIVE-2026-09-27-1's pre-warm ran. Then change (j) (T10's (h) leg 2 kept off an OSM pond,
2026-09-27, lane T10) was applied; `--check` reports 7a986137...69f5c7. A record citing 5d6bbae4 describes
the pre-(j) order. The hashes are of the CRLF working-tree files `--check` reads.)

Re-run the script after a re-export (`python tools/scenario/derive_ironstorm_cuta.py`),
or `--check` to prove the files on disk still match the derivation. The script FAILS
rather than guesses if a uuid, a task or an element order it depends on has moved.

THE CUT: one ~20 x 20 km navigation area ("IRONSTORM-CENTRE"), three taskees moving,
about 14 minutes of wall clock at `Vrf:DurationScale = 0.25`, GUI on. It is the
opening phase of the narrative - the forward passage of lines - and nothing else.

--------------------------------------------------------------------------------
## The complete change list

Nothing outside this list is touched. Verified mechanically: with every `<Task>` block
removed from both files, the remainder of the derived order is BYTE-IDENTICAL to the
remainder of the export. All 33 order graphics and all 33 `Entity` blocks are kept. No
coordinate is moved, no unit is renamed, no graphic is added or removed. (Since
2026-09-26 two exceptions to that sentence are on the list: (e) moves one coordinate and
(f)/(g) alter three `TaskActionCode`s and add one `AffectedEntity`; with every `<Task>`
block removed the remainder is still byte-identical, because all of those sit inside
task blocks or the one nudged graphic vertex.) Since 2026-09-26 (h) also ADDS ONE graphic (a two-vertex
Line/Route, T10's reroute) and one MapGraphicID in T10; that graphic's <Entity> is the only non-task text
added. Since 2026-09-27 (i) adds a SECOND such graphic (T14's waypoint) and one MapGraphicID in T14, and (j)
a THIRD (T10's second waypoint) and a second added MapGraphicID in T10. Root element, namespace, declaration and CRLF line endings are the
export's.

### (a) Duration format - connector bug STP-848

Every `IsoTimeDuration` value is rewritten from the canonical-ISO short form the export
writes to the C2SIM 1.1 pattern form. 10 values in the kept tasks:

| export | derived | count |
|---|---|---|
| `PT0S` | `P00Y00M00DT00H00M00S` | 3 |
| `PT20M` | `P00Y00M00DT00H20M00S` | 6 |
| `PT30M` | `P00Y00M00DT00H30M00S` | 1 |

(The export carries 46 across all 23 tasks; the other 36 leave with the dropped tasks.)

WHY. `IsoTimeDurationBaseType` is an `xs:string` restricted by the pattern

    [P]{1}[0-9]{2}[Y]{1}[0-9]{2}[M]{1}[0-9]{2}[D]{1}T{1}[0-9]{2}[H]{1}[0-9]{2}[M]{1}[0-9]{2}[S]{1}

(`C2SIM_SMX_LOX_CWIX2024.xsd:17-24`) - every field present, two digits wide. `PT20M` is
valid ISO 8601 and is NOT valid C2SIM 1.1. `OrderParser.FindTotalIsoMs` refuses it
(`OrderParser.cs:295`, returns -1 -> `DurationMs` 0), so as exported:

- T01 and T13 (`ExecutePlanPhase` -> `HoldInPlace`) are REFUSED as MALFORMED - the verb
  issues no VR-Forces task, so only a Duration could ever end them (Q4)
  (`VrfC2SimService.cs:2999-3013`);
- their STREND successors T02 and T14 then wait out the gate and never dispatch.

**NO VALUE CHANGES - only the spelling.** This is a producer-side defect and the fix
belongs in STP's connector; it is applied here so the cut can run at all.

### (b) Two MapGraphicID references added

Both name a graphic the order ALREADY carries. **No new geometry is authored.** The
element is inserted immediately before `Name`, where the schema's `ActionGroup` sequence
puts it (`ActionTemporalRelationship, Location*, MapGraphicID*, Name, UUID`).

| task | taskee | added MapGraphicID | graphic |
|---|---|---|---|
| T02 `696fbb33-...` | 28ID | `c8cde9b6-17f7-f253-800e-4de0cae84db3` | `PassagePoint_28ID_SLOT0` Point @ 54.028874, 23.264401 |
| T10 `9aab7fe6-...` | 1-112 IN | `cc23071f-aa60-9f52-884c-11505051cc99` | `PassagePoint_48_IBCT_SLOT0` Point @ 54.019389, 23.313902 |

Both tasks carry NO geometry whatsoever in the export (no `Location`, no
`MapGraphicID`), so both execute in place under R2 and the cut has nothing to watch.
With one reference each they resolve to `GeometrySource.MapGraphic` and drive.

> **CORRECTION TO THE RECORD.** `scratchpad\validation\ironstorm_export_report.md`
> PART 2 sec P2.2 states that `cc23071f` sits at 54.066723, 23.186398 and gives T10 an
> 8.39 km move. **That coordinate is wrong.** Read from the export this session, the
> graphic's `CurrentState/PhysicalState/Location` is **54.019388734463774,
> 23.313901568645093** - which is 48 IBCT's own start position. The uuid and the Name
> are exactly as PART 2 gives them; only the position and the derived distance are not.
> T10's real move is **2,617 m**, not 8,390 m. The consequence is benign for the cut
> (see the timeline below) but it means **1-112 IN drives to the point 48 IBCT is
> standing on**, so the two units end the run close together. Worth one caption, or a
> different graphic if the user would rather they separated.

### (c) The order reduced to the cut

**KEPT - 5 tasks.** Chain integrity is asserted by the script, not assumed: every kept
task's `ActionTemporalRelationship/TemporalAssociationWithAction` must name a kept task.

| id | uuid | taskee | code | role |
|---|---|---|---|---|
| T01 | `f7b52ba4-...` | 28ID | `ExecutePlanPhase` | chain root; PREDECESSOR of T02 - kept only for that |
| T02 | `696fbb33-...` | 28ID | `ATTACK` | **mover**, 5,341 m |
| T10 | `9aab7fe6-...` | 1-112 IN | `CRESRV` | chain root; **mover**, 2,617 m |
| T13 | `37677c40-...` | 48 IBCT | `ExecutePlanPhase` | chain root; PREDECESSOR of T14 - kept only for that |
| T14 | `1075b583-...` | 48 IBCT | `ATTACK` | **mover**, 2,757 m |

**DROPPED - 18 tasks**, with the reason each is out:

| dropped | taskee | why |
|---|---|---|
| T03, T04, T05 | 56 SBCT | outside the area. T04's own footprint is 20.6 x 18.1 km and its first leg 24.6 km; no 20 km box holds it with margin |
| T06, T07, T08, T09 | 278 ACR | T06 is the chain ROOT and is REFUSED by STP-833 (68.1 km leg against `Vrf:MaxRouteLegKm` 50), which abandons T07-T09 behind it |
| T11 | 1-112 IN | a SUCCESSOR of T10, not a predecessor; nothing in the cut depends on it, and it adds only a second in-place hold |
| T12 | 116 ABCT | 13.6 x 46.9 km footprint; its resolved route doubles back through its own start for 147.2 km |
| T15, T16 | 48 IBCT | SUCCESSORS of T14, both REFUSED by STP-833 (104.9 km and 102.2 km first vertices) |
| T17 | 48 IBCT | successor of T16, already abandoned |
| T18, T19, T20, T21 | 55 MEB | 25 km west of the cut's units; T19/T20/T21 share ONE destination through three uuids, so 55 MEB yields one move, not three |
| T22 | 169 FAB | 20 km north-west of the cut; a 14.1 km closed loop that returns to its own start, so STP-837 will not close it on arrival evidence |
| T23 | 11 CAB | REFUSED by STP-833 (78.0 km first vertex), and an attack-helicopter brigade has no air template at BDE echelon in the 5.2 fidelity table |

No kept task depends on a dropped one. T02 -> T01 and T14 -> T13 are the only two
dependencies in the cut and both predecessors are kept.

### (d) SystemName LEFT AS EXPORTED - connector bug STP-847

`SystemEntityList/SystemName` is the literal string `Not Set` and stays that way.
`VrfC2SimService.cs:1164` skips every unit whose `SystemName` does not equal
`Vrf:ClientId`, so with the shipped `"ClientId": "STP"` **zero of 40 units are created**.

**The run passes `--client-id "Not Set"`.** That is a workaround on the CONSUMER side and
it is deliberately not papered over in the data: the defect belongs to STP's connector
(the value is plainly an unset default, not a chosen one), and this file is the evidence.

### (e) APPLIED 2026-09-21 - T14's destination moved 799 m due west, out of a lake

**USER RULING 2026-09-21 ("as recommended"): apply the 800 m-west destination nudge in
the DERIVED file; STP will not re-author it.** This is the ONLY coordinate this derivation
moves.

| | latitude | longitude |
|---|---|---|
| **BEFORE** (as exported) | `54.04034819271516` | `23.336444730300467` |
| **AFTER** (derived) | `54.040348` | `23.324206` |

799 m due west; the leg shortens from 2,757 m to 2,426 m.

**THE REASON.** The authored leg crosses OPEN WATER. CLCplus (dataset 59, level 14) class
100, `preset="Water"`, from **752 m to 2,346 m along the 2,757 m leg** - 72 of 276 samples
at a 10 m stride, about 1,565 m of the drive. `deep-water` is acceleration-factor
**0.000000** in `ground-tracked.sysdef`: a ground vehicle driven onto it STOPS and the
vendor reports `TaskRunning` for ever. The lateral route shift cannot save it - the shift
only fires on a leg FLAGGED for grade, and this leg's ratio is 0.10.

**WHICH COPY WAS CHANGED, and why both.** The interface drives the **MapGraphic**, not the
embedded `Location` (`TaskGeometryResolver` returns `GeometrySource.MapGraphic` as soon as
a graphic resolves). T14 names ONE graphic, `7351f662-f857-e05a-b533-f9a46e0fb095`
`FollowAndSupport_48_IBCT_SLOT1`, a `TaskGraphic` with two vertices; under main's
role-based resolver that is a LINE, its first vertex IS 48 IBCT's own position and is
dropped at `OriginCoincidenceMeters` (100 m), and the **second vertex is the destination**.
So the graphic's second vertex is what had to move. T14's embedded `Location` carries the
same pair and was moved **with** it, so the driven route and the embedded route cannot
disagree. The pair occurs **exactly twice** in the derived order and the script asserts
that count, asserts the target longitude is not already present, and re-checks afterwards
that **both** the graphic block and the task block carry the new value - a half-applied
nudge raises rather than ships. No other task references that graphic.

**VERIFIED DRY AT FOUR STRIDES** after the move - 25 m, 10 m, 5 m and 2 m, the last being
**1,213 samples with zero water**, and the endpoint itself sampled. The original proposal
was found with a 25 m stride, which is coarse enough to step over a narrow inlet; a dry
verdict that depends on the stride is not a verdict.

> **CORRECTION 2026-09-27 (lane T14).** "Verified dry" above was true of CLCplus ONLY. The
> vendor composite puts OSM inland water ON TOP of CLCplus, and the nudged centreline enters
> OSM way 197345448 (natural=water, the lake Jezioro Wiersnie) at 834 m - deep-water,
> acceleration-factor 0.0. In the 2026-09-27 pre-warm 48 IBCT stopped dead 0.5 m outside that
> polygon at 850 m along. Fixed by change (i).

**BONUS, measured against the generated navigation area:** the nudge also moved T14 off
the two worst-connected sectors it used to cross - (30,22) ratio 0.7736 and (31,22) ratio
0.8000. The nudged leg lies **entirely** in sectors reading >= 0.9 (worst 0.9107):
**0 of 2,429 m in a sub-0.9 sector**, against 100 % of its old bad ground. Moving the
route fixed both the water and the connectivity.

### (f) APPLIED 2026-09-26 - T14 ATTACK -> FOLSPT, plus 116 ABCT as a second AffectedEntity

**OWNER DIRECTION 2026-09-26: "hack the xml you are using to have the correct codes while
stp itself is patched".** The STP source (`STP-IRON-STORM-SYNTHETIC_Narrative1.op`, task
`FollowAndSupportFriendlyUnit`) gives T14 `what: 'FOLLOW_AND_SUPPORT'` and `supported:`
116 ABCT (`884518d7-5b82-a455-98de-64aae833d633`).

**STP DEFECT WORKED AROUND (STP-846 family):** `C2SimTask.MWTaskCode` switches on
`What.ToLower()` (STP HEAD `BridgingAgents/C2SimBridge/C2SimBridge/C2SimTask.cs:131`) =
`"follow_and_support"`, but its case label is `"follow and support"` (:297-299, which returns
FOLSPT), so it falls through to `return TaskActionCodeType.ATTACK` (:670).

- `<TaskActionCode>ATTACK</TaskActionCode>` -> `FOLSPT` (C2SIM order schema
  `TaskActionCodeType`, `C2SIM_SMX_LOX_ASX_v1.0.1_Order_flat.xsd:1854`; in the SDK enum).
- ONE line added right after the exported (self) `AffectedEntity`:
  `<AffectedEntity>884518d7-...</AffectedEntity><!--116_ABCT/28ID ADDED BY CUT A (f)-->`.
  `AffectedEntity` is maxOccurs unbounded (xsd:2759). The script asserts 116 ABCT is a unit
  of the init (`--parse-init`: it is, in the 12-unit stack at 53.992385, 23.211255). The
  self reference stays FIRST, and the interface reads only the first (`OrderParser.cs`
  FirstOrEmpty), so the interface does not see 116 ABCT - it is carried for data fidelity.
  No TaskFunctionalRelation is added.

### (g) APPLIED 2026-09-26 - T01 and T13 ExecutePlanPhase -> CNFPSL

**OWNER DECISION 2026-09-26: "Patch + Jira".** Both STP source tasks
(`ConductFwdPassageOfLines`, 28ID and 48 IBCT) have `what: 'NOT_SPECIFIED'` and
`how: 'PASSAGE_OF_LINES'`.

**STP DEFECT WORKED AROUND (same class as (f)):** the connector switches on `How.ToLower()`
(C2SimTask.cs:609), but the case label is the UPPER-case `"PASSAGE_OF_LINES"` (:645-646,
which returns CNFPSL), so it never matches and falls to
`return TaskActionCodeType.ExecutePlanPhase` (:668). CNFPSL is in `TaskActionCodeType`
(`C2SIM_SMX_LOX_ASX_v1.0.1_Order_flat.xsd:1789`) and in the SDK enum.

- `<TaskActionCode>ExecutePlanPhase</TaskActionCode>` -> `CNFPSL` in T01 and T13. Nothing
  else in those blocks changes: Duration, MapGraphicIDs, AffectedEntity and relations are
  as exported.
- **NOT PATCHED: T02.** Its source task `ReceiveOrderableActivity` has `what: 'RECEIVE'`;
  STP has no mapping (`//case "RECEIVE,` is commented out at :545) and the schema has no
  receive code, so there is no agreed code to restore. It stays `ATTACK` (STP-846).
- **T10** needs nothing: `CONSTITUTE_RESERVE` -> `CRESRV` in both the `.op` and the export.

**CONSEQUENCE AT RUN TIME - read before any live run of this order.** On the interface as of
this commit neither CNFPSL nor FOLSPT is in `VerbMapping` (`src/VrfC2SimApp/VerbMapping.cs`),
and `Classify` sends an unlisted verb to the bare-move fallback with `Recognized: false`
(:159-167). So:
- T14 (FOLSPT) drives its route as a bare move, as it did under `ATTACK` minus the attack
  path (which never fired - see STP-846 below).
- **T01 and T13 (CNFPSL) STOP BEING HOLDS and would DRIVE their four-graphic assemblies** -
  leg_check scores them at 40.1 km (T01) and 29.6 km (T13), with deep water on T01 leg 2
  (16 samples) and on T13 legs 1 and 2 (32 each). The "never driven" paragraph under
  *Geometry the interface will drive* below held for `ExecutePlanPhase` only.
The CNFPSL verb is being added by another lane; until it lands and says what CNFPSL does,
this order must not go to a live run as-is.
> **SINCE MAPPED (noted 2026-10-04, lane FD).** The paragraph above is historical: FOLSPT advances along the graphic
> and holds (`VerbMapping.cs:164`), and CNFPSL is HELD IN PLACE exactly like ExecutePlanPhase - no vendor task,
> completion by time (`VerbMapping.cs:167`, `TaskDispatchPolicy.cs:61-62`; RL-20260926-01 A6, STP-866). T01 and T13
> do not drive their four-graphic assemblies. (data/IRONSTORM_FULL_CHANGES.md (g) applies the same patch to T03.)

--------------------------------------------------------------------------------
## STP-846 - T02 IS the ATTACK fallthrough, and here is exactly what it does

**READ THIS BEFORE THE DEMO.** T02's `TaskActionCode` is `ATTACK`. The live STP scenario
gives the same task `task_type: "ReceiveOrderableActivity"`, and its own `Name` says what
it really is:

> *T2_28IdHqTransitionsFromOffensiveOperationsToConsolidationAndSecurity,
> Re-EstablishesAoBoundaries,AndCoordinatesWith29IdAndFollow-OnForces...*

A "receive / transition to consolidation and security" task is exported as `ATTACK`.
`ATTACK` is the connector's generic fallthrough for any task type it has no C2SIM verb
for - T14 was a second instance, from `FollowAndSupportFriendlyUnit`, and is now `FOLSPT`
by change (f). **For T02 the derived file keeps the code exactly as exported.**

**WHAT THE INTERFACE ACTUALLY DOES WITH IT - read from the code, not assumed:**

1. `VerbMapping` classifies `ATTACK` as `TaskIntent.Attack`, composition
   *"move-to-contact + fireAtTarget(affected)"* (`VerbMapping.cs:102`). The composition
   string is a LABEL; what runs is the code below.
2. `ResolveAffectedTarget` is called (`VrfC2SimService.cs:2920`). T02's `AffectedEntity`
   IS its `PerformingEntity` (28ID, `200d3a3f-...`) - true on all 23 tasks in this
   export. That resolves to `TargetResolution.SelfIsObjective`, and
   `ResolveAffectedTarget` **returns null** after logging R3's
   *"No fire against a named entity is issued"* (`VrfC2SimService.cs:3603-3610`).
3. `attackTargetVrf` is therefore null, so **every `FireAtTarget` call site is skipped**
   (`:3065`, `:3419`, `:3442`, `:3493`, `:3556` are all guarded by
   `attackTargetVrf != null`). No `DtFireAtTargetTask` is ever issued.
4. `RuleOfEngagement/MipWeaponUseROE/WeaponROECode/WeaponRuleOfEngagementCode` is
   **`ROEHold` on all 23 tasks** in the export, and `VrfC2SimService.cs:3384-3386` maps
   `ROEHold` -> `Roe.HoldFire`, which is pushed to the object before the move.
5. There is no move-to-contact behaviour in the code at all - the dispatch is
   `CreateRoute` + `MoveAlongRoute` on the task's own geometry.

**VERDICT: the unit does NOT open fire and does NOT move to contact.** 28ID drives to
`PassagePoint_28ID_SLOT0` under weapons-hold and stops. No hostile unit is referenced by
any task in this order (all 11 WASA units are init context only), so there is nothing to
engage even if the ROE were free. Shipping T02 as exported is safe.

**What it costs in honesty:** the audience is watching a command post displace forward
through a passage point. It is NOT an attack, the on-screen verb says `ATTACK`, and the
narration must say so. The right fix is on the producer side - export the Receive task as
`RETAIN`/`DEFEND` and the Follow-and-Support task as `ESCRT` (which would make T14 a real
`DtFollowEntityTask` on 116 ABCT instead of a self-targeted move).

--------------------------------------------------------------------------------
## Schema validation

Validated with `lxml` against
`Software/Library/CS/C2SIMSDK/C2SIMSDK/schemas/C2SIM_SMX_LOX_CWIX2024.xsd`.

| file | violations |
|---|---|
| export order | **46** - every one the `IsoTimeDuration` pattern |
| **derived order** | **0 - VALID** |
| export init | **1** - `APP6C-SIDC` value `-F-P-----------` at line 592 (`Headquarters_and_Headquarters_Brigade,_III_Corps`) does not match the APP6C pattern |
| **derived init** | **1 - the SAME one, byte-identical** |

**Zero violations introduced.** The init's one violation is the export's own, it is NOT
fixed here (that would be authoring), and it is harmless to the cut: that unit is not a
taskee and `VrfC2SimService.cs:1177-1183` skips it anyway for want of a position.

### (h) APPLIED 2026-09-26 - T10 rerouted round nav sectors (28,21) and (28,22)

**OWNER SELECTION 2026-09-26: "Maples + reroute T10's leg around the two sectors (Recommended)".**

WHY. The owner's bar: every navigation-area sector a driven route crosses must read abstract-graph
connectivity >= 0.9 (`tools/navdata/corridor_gate.py`). On the IRONSTORM-CENTRE area generated on the
red-maple terrain (biome 04 Sycamore / White Oak -> RedMapleSpring; area `NavArea-ground-platform
IRONSTORM-CENTRE_maple`, docs/experiments/PREREG_IRONSTORM_TREES_2026-09-26.md Part 2) T02 and T14 pass,
and T10's straight leg crosses (28,21) 0.8485 and (28,22) 0.8857 - lake-edge sectors (water 0.34 / 0.22)
that no tree lever cleared (Parts 2-3). The route goes round them. The interface's lateral route shift
was NOT used: it fires only on a leg flagged for grade (see (e)), so it cannot target a named sector.

WHAT. ONE new waypoint, 54.029734 / 23.305499, carried by ONE added `Line/Route` graphic
`c8d9cd1a-b808-5b8d-97be-7beb98393a62` ("T10_Reroute_1-112_IN__CUT_A_H_...", axis-of-advance SIDC
`GFGPOLAGM-----X`, uuid5 of a fixed name so the derivation stays reproducible) with two vertices: 1-112 IN's
own init position (asserted equal to the init) and the waypoint. T10 references it BEFORE the (b)
PassagePoint reference. By the interface's route assembly (TaskGeometryResolver SF9: lines supply the
path, points the destination, a line vertex within 100 m of the taskee is dropped) T10 drives

    54.042688, 23.308235 (start) -> 54.029734, 23.305499 -> 54.019389, 23.313902 (PassagePoint_48_IBCT_SLOT0)

1,453 m + 1,276 m = 2,729 m (the straight leg was 2,620 m; +109 m). `leg_check.py --dump-resolved` resolves
T10 to exactly [waypoint, PassagePoint] (its Python port of the same rule); `VrfC2SimApp --parse-order`
lists both MapGraphicIDs on T10.

HOW THE WAYPOINT WAS CHOSEN. A 10 m grid search over waypoints in columns 27-28 (scratch
laneI1d search) kept the ones whose two legs cross only sectors >= 0.9 on the maple area and stay >= 100 m
from (28,21) / (28,22); among those whose worst sector is >= 0.95 the shortest path was taken, then checked
dry. The waypoint lies in sector (27,21) (ratio 1.000), 166 m clear of the two avoided sectors.

CHECKS on the derived order:
- corridor_gate `--preset ironstorm-cuta-h` on the maple area log (frame = its runtime config): **PASS**,
  29 distinct sectors, 0 below 0.9, min 0.9000 at T02's (17,15) (exactly on the bar - T02 is untouched);
  T10 leg 1 worst 0.9649, leg 2 worst 0.9545. On the vendor-terrain baseline area the same legs still FAIL
  (5 sectors, all T02/forest) - the reroute is for the maple area only.
- `leg_check.py --step 2 --no-chain`: T10 leg 1 1,451 m ratio 0.11 ok, leg 2 1,275 m ratio 0.09 ok, **0 water
  samples** on either; T02 and T14 unchanged (dry, ok).
- The waypoint and both legs are inside the IRONSTORM-CENTRE area.

T02, T14, T01 and T13 are not touched by (h).

### (i) APPLIED 2026-09-27 - T14 routed round the lake Jezioro Wiersnie

**Lane T14, 2026-09-27, from the IRONSTORM_CUTA_LIVE-2026-09-27-1 pre-warm.** Record:
docs/experiments/FINDING_IRONSTORM_T14_STOP_2026-09-27.md. Not an owner ruling: the lane brief
said to apply it if the stop's cause was confirmed offline, which it was.

WHY. In the pre-warm (runs/20260927T003120Z_run) 48 IBCT drove 850 m of the (e) leg and stopped
dead at 54.026779, 23.317195 for the rest of the run, console silent, task never completed. That
point is 0.5 m outside OSM way 197345448 (natural=water). The vendor chain, all read from the
installed files: OSM water is the TOP online layer of the land-cover composite
(`biomes.landcover.coverage.online.xml:58`, sim-enabled by `biome.config.online.xml:13`); natural=water
without a water= tag takes selectStyle's default -> coverage value 80 -> preset Water -> BM_WATER
(`layer.OSM.water.LOD14.online.xml`, `presets.xml:36`) -> deeplake (`landCoverDataSurfChar.map:346`)
-> deep-water (UG52 Table 26) -> acceleration-factor 0.000000 (`ground-tracked.sysdef:813-815`).
CLCplus under the stop is 53 (herbaceous) and CLCplus water (100) starts 25 m east, which is why
(e)'s CLCplus-only check read the leg dry.

WHAT. ONE new waypoint, **54.014600 / 23.331500** (south-east of the lake), carried by ONE added
`Line/Route` graphic `7ff48b93-5a1e-5a9a-813f-8dda1df7e5dd` ("T14_Waypoint_48_IBCT__CUT_A_I_...",
SIDC `GFGPOLAGM-----X`, uuid5 of a fixed name) with two vertices: 48 IBCT's own init position
(asserted equal to the init) and the waypoint. T14 references it BEFORE its FollowAndSupport
graphic `7351f662`, whose vertices are asserted to be [48 IBCT's position, the (e) destination].
The resolver drops both first vertices (the taskee's own position) and chains nearest-first:

    54.019389, 23.313902 (start) -> 54.014600, 23.331500 -> 54.040348, 23.324206 (UNCHANGED destination)

1,267 m + 2,902 m = 4,169 m (the (e) leg was 2,426 m; +1,743 m). `leg_check.py --dump-resolved`
resolves T14 to exactly [waypoint, destination].

SEMANTICS - STATED, because it changes. T14 stays FOLSPT: "advance along the task's graphic to its
end and hold" (`TaskDispatchPolicy.cs:57`). The HOLD POINT is unchanged. The PATH is no longer the
FollowAndSupport graphic's straight line: it first runs 1.3 km south-east, AWAY from the
destination, to a waypoint that is not on that graphic. At ~8 m/s the move takes ~520 s against
the 300 s (scaled) duration, so T14 is expected to go OVERDUE and complete on arrival (the late
path), as T02 did in the pre-warm.

HOW THE WAYPOINT WAS CHOSEN (scratch lane T14 search). A 20 m grid of single waypoints, both legs
>= 40 m from water of ANY source (CLCplus water soils, OSM water of every class incl. marsh, 5 m
lattice), then the maple-area corridor on both legs. The WEST side of the lake was rejected: every
dry west route either crosses nav sectors (28,21)/(28,22) (0.8485, the sectors (h) avoids) or runs
head-on down T10's own corridor, where T10's seven members were still crawling at 54.035-54.037 N
at the end of the pre-warm. Among the east routes the waypoint was refined on a 0.0001 deg grid
for >= 60 m clearance on both legs at the shortest length.

CHECKS on the derived order (sha256 5d6bbae4...49fc9a):
- Water, 2 m stride at lateral offsets 0 / +-25 / +-50 m, CLCplus + OSM: **0 of 3,175** (leg 1) and
  **0 of 7,265** (leg 2) samples wet; clearance to the nearest water 60 m / 61 m.
- `leg_check.py --step 2 --no-chain --osm-water <z14 osm-water tiles>`: T14 leg 1 1,267 m ratio
  0.14 ok, leg 2 2,902 m ratio 0.06 ok, no water; the SAME run on the pre-(i) order reports T14
  "WATER ON THE LINE - 78 of 1214 sample(s) ... deep-water (OSM water z14)". L12 elevation (the
  finest served here; L13-L15 absent), steepest 10 m window 0.153 up / 0.176 down against the
  M577A2's max-slope 1.0.
- `corridor_gate.py --leg` on the maple area log (frame = its runtime config): **PASS**, 10 sectors,
  0 below 0.9, min 0.9167 at (30,19). `--preset ironstorm-cuta-h` (T02 + T10, unchanged): **PASS**,
  29 sectors, min 0.9000 at T02's (17,15), as recorded for (h).

**NOT FIXED HERE - T10 (h) leg 2 grazes OSM water.** The same OSM-aware check flags 1 of 639
centreline samples on T10's leg 2 at 0.76 km (54.02359, 23.31049), and 81 / 102 samples at 25 / 50 m
to the right of travel (702-880 m along). T10 is a seven-member aggregate that moves in formation, so
its right-hand members may drive into that water. (h) is an owner selection; re-routing it is the
owner's call. **Since fixed by (j) below** (a waypoint added; (h)'s own waypoint is unchanged).

T02, T10, T01 and T13 are not touched by (i).

### (j) APPLIED 2026-09-27 - T10's (h) leg 2 kept off an OSM pond

**Lane T10, 2026-09-27, from lane T14's OSM-aware check (the (i) note above).** Not an owner ruling:
the lane brief said to keep T10 dry with margin without moving its destination unless unavoidable.
It was avoidable; the destination and (h)'s waypoint are unchanged.

WHY. (h) leg 2 (54.029734 / 23.305499 -> PassagePoint_48_IBCT_SLOT0) runs down the east edge of an
OSM natural=water pond (value 80 -> deep-water, the chain under (i)): 1 of 639 centreline samples at
54.02359 / 23.31049, and 81 / 102 samples at 25 / 50 m RIGHT of travel (702-880 m along). 1-112 IN
is a six-to-seven member aggregate moving in formation, so its right-hand members would reach it.

WHAT. ONE more waypoint, **54.024000 / 23.313000** (east of the pond), carried by ONE added
`Line/Route` graphic `51a59f89-799e-5ed0-8c05-1e63b01e8069` ("T10_Waypoint_1-112_IN__CUT_A_J_...",
SIDC `GFGPOLAGM-----X`, uuid5 of a fixed name) with two vertices: 1-112 IN's own init position
(asserted) and the waypoint. T10 references it AFTER the (h) graphic and immediately BEFORE the (b)
PassagePoint (asserted adjacent). The resolver drops both lines' first vertex (the taskee's own
position) and chains nearest-first from the taskee; the (h) waypoint (1,453 m away) is nearer than
the (j) one (2,104 m), which the derivation asserts, so T10 drives

    54.042688, 23.308235 (start) -> 54.029734, 23.305499 -> 54.024000, 23.313000 -> 54.019389, 23.313902 (UNCHANGED)

1,453 m + 805 m + 517 m = 2,775 m (the (h) route was 2,729 m; +46 m). `leg_check.py --dump-resolved`
resolves T10 to exactly [(h) waypoint, (j) waypoint, PassagePoint].

HOW THE WAYPOINT WAS CHOSEN (scratch lane T10). With (h)'s leg 1 kept, a 20 m grid of single extra
waypoints whose two new legs are >= 60 m from water of ANY source (CLCplus water soils and muck,
OSM water of every class; 5 m lattice) and >= 100 m from sectors (28,21)/(28,22), then the maple
corridor on all three legs (785 candidates; the 30 shortest all PASS). The shortest had only 60 m;
a 0.0001 deg refinement east of the pond traded +40 m of path for >= 150 m of clearance. The
alternative - moving (h)'s waypoint west of the pond, one waypoint - was 3,045 m (+316 m) and
would have rewritten the owner-selected (h).

CHECKS on the derived order (sha256 7a986137...69f5c7):
- Water, 2 m stride at lateral offsets 0 / +-25 / +-50 m, CLCplus + OSM: **0 of 3,635** (leg 1),
  **0 of 2,015** (leg 2), **0 of 1,295** (leg 3) samples wet; nearest water of any source 110 m /
  155 m / 150 m.
- `leg_check.py --step 2 --no-chain --osm-water <z14 osm-water tiles>`: T10 leg 1 1,451 m ratio
  0.11, leg 2 804 m ratio 0.01, leg 3 516 m ratio 0.08, all ok, no water (the same run on the
  pre-(j) order reports T10 leg 2 "WATER ON THE LINE - 1 of 639"). L12 steepest 10 m window
  0.113 up / 0.101 down (leg 1), 0.012 (leg 2), 0.073 / 0.042 (leg 3), against 0.921 derated.
- `corridor_gate.py --preset ironstorm-cuta-j` (NEW: T02, T10's three legs, T14's two (i) legs)
  on the maple area log (frame = its runtime config): **PASS**, 33 sectors, 0 below 0.9, min
  0.9000 at T02's (17,15) (unchanged). T10 leg 1 worst 0.9649, leg 2 0.9200 at (28,20), leg 3
  0.9565; T14 legs 0.9259 / 0.9167. The new legs stay >= 164 m from (28,21)/(28,22).
- T14 (c): T10's new legs are >= 521 m from T14's (i) leg 1 except where both meet at the
  PassagePoint (T14's start, T10's end - by design since (b)), and >= 1,022 m from T14's leg 2,
  across the lake. T10 now arrives heading 173 deg; T14 leaves heading 115 deg, so the two rays
  from the PassagePoint are 122 deg apart (not head-on); 200 m out they are 349 m apart. Both
  start at 5.0 min (scale 0.25); T14 has left the PassagePoint long before T10's ~2.8 km arrive.

T02 (OSM water, checked here for the first time): **dry** - 0 wet samples at 0 / +-25 / +-50 m
(and +-75 / +-100 m) over its 5,341 m; the nearest OSM water is a river (value 82, deep-water)
100-150 m right of travel near 2.34 km along (10 samples wet at +150 m). Not changed.

T02, T14, T01 and T13 are not touched by (j).

### E1 - 2026-09-27: the VARIANT without change (i), `data/IRONSTORM_CUTA_E1_Order.xml`

**Run E1 (docs/PLAN_MOVEMENT_2026-09-27.md row E1; ruling RL-20260927-01), lane E1.** Written by
`derive_ironstorm_cuta.py --without i`; sha256 **7d5b4034eef0ecf0f33946c6f30b60aaff35e640a86b6b16c9c24f02cb3ddb9a**
(74,959 B, the CRLF working-tree file `--check` reads). The init is unchanged (2000e856...). This is a second
ORDER FILE beside the ruled one, not an edit of it: `IRONSTORM_CUTA_Order.xml` still carries (a)-(j).

PURPOSE. Since RL-20260927-01 a lone ground platform is driven by Move To per STP vertex, and Move To PLANS each
leg - roads, the nav mesh, round feature obstacles - and recovers from a blockage (UG52 23.1-23.2), where the
Move Along Route T14 used to get planned nothing (UG52 23.3). Change (i) is a hand-placed waypoint that did the
planner's job. E1 takes it away on purpose: **the PLANNER, not a hand waypoint, must take T14 round the lake.**
T14's original line still enters OSM way 197345448 (Jezioro Wiersnie, deep-water) - that is the point of the run,
not a defect of the variant.

WHAT. (a)-(h) and (j) exactly as above; (i) LEFT OUT. T14 names only its FollowAndSupport graphic 7351f662 again:

    54.019389, 23.313902 (start) -> 54.040348, 23.324206 (the (e) destination), 2,426 m

**T14's DESTINATION KEEPS CHANGE (e)** - the 799 m west nudge out of the lake (RL-20260921-01) stays. A
Move To cannot end in water either: the planner's only endpoint adjustment is onto a road shoulder, never out of
water, and a vertex it cannot plan to aborts the task (FINDING_GROUND_MOVEMENT_PRACTICE_2026-09-27 sec 5.2,
ground-vehicle-move-to.lua :423-460, :1401-1404). T10 keeps (h) and (j): it is a unit, whose Move Along Route
already plans per vertex for every member (UG52 30.22), and its route is not what E1 varies. Change (k) (T14 round
the hamlet it stopped in on the (i) route, branch fix/ironstorm-t14-hamlet) is unmerged and is not in this
derivation; `--without` refuses every letter except `i`.

CHECKS (2026-09-27, lane E1):
- The script derives BOTH orders on every run, gates both, and proves the E1 order is the ruled order minus
  exactly (i)'s graphic `<Entity>` and its one MapGraphicID line (`git diff --no-index`: 43 deletions, 0
  insertions). It ASSERTS the original line: T14 names only 7351f662, whose vertices are [48 IBCT's init
  position, the (e) destination], and 7ff48b93 is nowhere in the file. `--check` verifies the init and both orders.
- `VrfC2SimApp --parse-order` (build b984945): 5 tasks, T14 `mapGraphic: 7351f662-...` only, durations 4 x 1,200,000
  ms and 1 x 1,800,000 ms (T10), T02 / T14 start after T01 / T13, no warning.
- `leg_check.py --step 2 --no-chain --no-starts --osm-water <z14 osm-water tiles>`: T14 leg 1, 2,426 m, **"WATER ON
  THE LINE - 78 of 1214 sample(s) classify as deep-water (OSM water z14 ...), first at 0.83 km along (54.02659,
  23.31744)"**, ratio 0.05 (not flagged for grade - so the lateral route shift, which flags on grade, will not
  touch it). T02 and T10 as under (j): dry, ok. `--dump-resolved`: T14 resolves to [54.040348, 23.324206] alone.

NOT CLAIMED HERE: that the planner clears the lake. That is what E1 observes
(docs/experiments/PREREG_IRONSTORM_CUTA_E1_2026-09-27.md).

--------------------------------------------------------------------------------
## What the cut does at run time

### Geometry the interface will drive
MapGraphic wins over the embedded `Location` (`TaskGeometryResolver.cs:170`), and
`Vrf:DropOriginVertexMeters` (100 m) drops a first vertex that sits on the taskee.

| task | taskee | from | to | driven |
|---|---|---|---|---|
| T02 | 28ID | 53.992385, 23.211255 | 54.028874, 23.264401 | **5,341 m** |
| T10 | 1-112 IN | 54.042688, 23.308235 | 54.019389, 23.313902 | **2,617 m** straight; since (h) via 54.029734, 23.305499, 2,729 m; since (j) also via 54.024000, 23.313000, 2,775 m |
| T14 | 48 IBCT | 54.019389, 23.313902 | 54.040348, 23.336445 | **2,757 m** (origin vertex dropped at 0.0 m); since (e) to 54.040348, 23.324206, 2,426 m; since (i) via 54.014600, 23.331500, 4,169 m |

T01 and T13 carry four MapGraphicIDs each, but `HoldInPlace` issues NO VR-Forces task
(`VrfC2SimService.cs:3018-3031`) and logs that the geometry was not driven, so **their
39-45 km of resolved zig-zag is never driven and never needs nav coverage.**
**SUPERSEDED 2026-09-26 by change (g):** that held while they were `ExecutePlanPhase`.
As `CNFPSL` they are unmapped today and take the bare-move fallback - see (g).

### Timeline
`Vrf:DurationScale` scales the order's clock - both the Duration that ends a task and the
StartTime delay that holds one back (`TaskDispatchPolicy.ScaleOrderMs`) - but it does
**not** scale movement. Travel at the project's measured 10 m/s cruise (ASSUMED for
Suwalki; never measured on Baltic terrain).

Note T10's `StartTime/SimulationTime/DelayTimeAmount` is `PT20M`, **not zero** - a root
task with a real 20-minute authored delay, which only becomes visible once (a) makes it
decodable. T02's and T14's `StartTime/RelativeTime/DelayTimeAmount` are NOT read
(`OrderParser.TimingOf` handles `SimulationTime` and `DateTime` only), so they start when
their predecessor's Duration ends.

| scale | T01/T13 hold ends | T02 | T10 | T14 | last |
|---|---|---|---|---|---|
| 1.0 | 20.0 min | 20.0-28.9 | 20.0-24.4 | 20.0-24.6 | 28.9 min |
| **0.25** | **5.0 min** | **5.0-13.9** | **5.0-9.4** | **5.0-9.6** | **13.9 min** |
| 0.10 | 2.0 min | 2.0-10.9 | 2.0-6.4 | 2.0-6.6 | 10.9 min |

`0.25` is the recommended value: three units move at once from 5 minutes in, and the run
ends at about 14 minutes. `Vrf:DurationScale` must be > 0 and finite - **zero is
rejected** at start-up (`TaskDispatchPolicy.IsUsableDurationScale`) and the run falls back
to 1.0.

### Terrain - AFTER change (e)

`tools/preflight/leg_check.py` over the three DRIVEN legs (elevation dataset 149, cascade
L13 -> L11, **every sample resolved at L12**; land cover led by CLCplus 10 m, dataset 59
level 14):

| leg | length | sustained 40 m | limit | ratio | CLCplus classes | water |
|---|---|---|---|---|---|---|
| T02 28ID | 5,341 m | 0.097 hard-packed | 0.921 | 0.11 ok | 21 x32, 22 x1, 51 x6, 52 x2, 60 x13 | **0** |
| T10 1-112 IN | 2,617 m | 0.072 hard-packed | 0.921 | 0.08 ok | 21 x8, 22 x1, 33 x2, 51 x8, 52 x5, 60 x3 | **0** |
| T14 48 IBCT | **2,426 m** | **0.044** hard-packed | 0.980 | **0.05 ok** | 21 x4, 22 x2, 31 x3, 33 x1, 40 x1, 51 x4, 52 x2, 53 x1, 60 x7 | **0** |

**0 of 3 legs cross water; 0 flagged for grade; 0 NO VERDICT; terrain 120.6-164.1 m.**
(CLCplus water only - see the CORRECTION under (e): OSM water was not read, and the T14 row is
superseded by (i).) No
urban class on any leg. The nudge also *improved* T14's grade - the new line is gentler
(0.044 sustained against 0.096) as well as dry - and shortened it by 331 m.

The pre-change figures are kept below as the record of what (e) removed.

--- BEFORE change (e) ---

| leg | length | sustained 40 m | limit | ratio | CLCplus classes | water |
|---|---|---|---|---|---|---|
| T02 28ID | 5,341 m | 0.097 hard-packed | 0.921 | 0.11 ok | 21 x32, 22 x1, 51 x6, 52 x2, 60 x13 | 0 |
| T10 1-112 IN | 2,617 m | 0.072 hard-packed | 0.921 | 0.08 ok | 21 x8, 22 x1, 33 x2, 51 x8, 52 x5, 60 x3 | 0 |
| T14 48 IBCT | 2,757 m | 0.096 hard-packed | 0.980 | 0.10 ok | **100 x6**, 21 x6, 22 x1, 31 x6, 51 x5, 52 x4 | **29 of 112** |

0 legs flagged for grade; no leg got NO VERDICT; terrain reads 120.6-164.1 m.

> **T14 CROSSES OPEN WATER.** CLCplus class 100 (`preset="Water"`) covers the middle of
> the leg: **first wet at 770 m along (54.02524, 23.32020), last at 2,335 m** - about
> 1,565 m of a 2,757 m leg. Both endpoints are dry. `deep-water` is
> acceleration-factor **0.000000** in `ground-tracked.sysdef`, so a ground vehicle driven
> onto it STOPS and the vendor reports `TaskRunning` for ever - the silent freeze a demo
> cannot afford. The lateral route shift will NOT save it: the shift only fires on a leg
> FLAGGED for grade, and T14's ratio is 0.10.

Of the three remedies proposed on 2026-09-20, the user chose the second - move the
authored destination 800 m due west - and it is change (e) above. The other two are
recorded as not taken: an inserted mid-leg waypoint at 54.028330, 23.317987 (new
geometry), and relying on the navigation mesh to route around the lake (free, but never
observed on this terrain, and worthless if a run proceeds without nav data, because the
off-mesh fallback is a 1-part STRAIGHT feature path straight through the lake).

--------------------------------------------------------------------------------
## Known limitation of the offline pre-flight

`tools/preflight/leg_check.py` reads a task's embedded `<Location>` elements ONLY - it
carries **no MapGraphicID handling at all** (zero occurrences in the file). The C#
interface has resolved order-borne and `TaskGraphic` MapGraphicIDs since `9d67f97` and
PREFERS them over the embedded `Location`. So run directly on this order the tool reports
*"no route points in the order"* for T02 and T10 and scores the WRONG line for T14.

The table above was produced by rewriting each task's embedded geometry to the vertices
the interface will actually drive, in a SCRATCH order (never under `data/`), and running
the tool on that. The tool also CHAINS a unit's later tasks onto the end of its previous
task's route with no switch to turn it off, which is wrong for `HoldInPlace`
predecessors - T01 and T13 never move - so the movers were scored in isolation.

**Both are real gaps in the instrument and belong to the pre-flight lane, not here.**
(Since closed in the tool: MapGraphicID resolution and `--no-chain`. A third gap, found
2026-09-27: the tool read CLCplus water only and not the OSM inland water the vendor composite
puts on top of it - the cause of the T14 pre-warm stop. `--osm-water DIR` now reads it; it is
opt-in, and without it the text output says water is CLCplus-only.)
