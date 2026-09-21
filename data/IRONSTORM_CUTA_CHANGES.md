# IRONSTORM CUT A - what was derived, and what it costs

`data/IRONSTORM_CUTA_Initialization.xml` and `data/IRONSTORM_CUTA_Order.xml` are
DERIVED, not authored. They are produced by
`tools/scenario/derive_ironstorm_cuta.py` from the user's authoritative STP export

    data/STP-IRON-STORM-SYNTHETIC_Initialization.xml   sha256 2000e856cb00314064ab6d40c7f7df64cea098b3466fe70d7614f3d26dc93eec
    data/STP-IRON-STORM-SYNTHETIC_Order.xml            sha256 33dc899c734861f8cbae95ef3d36b77b15ba1081ee3c7ef823b80c597d99a5b4

and reproduce byte for byte:

    data/IRONSTORM_CUTA_Initialization.xml             sha256 2000e856cb00314064ab6d40c7f7df64cea098b3466fe70d7614f3d26dc93eec
    data/IRONSTORM_CUTA_Order.xml                      sha256 1cd89c40bcce5d66bfb9966a3e5933d2e1957563b636d660c0d8e12cc3956636

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
coordinate is moved, no unit is renamed, no `TaskActionCode` is altered, no graphic is
added or removed. Root element, namespace, declaration and CRLF line endings are the
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

**BONUS, measured against the generated navigation area:** the nudge also moved T14 off
the two worst-connected sectors it used to cross - (30,22) ratio 0.7736 and (31,22) ratio
0.8000. The nudged leg lies **entirely** in sectors reading >= 0.9 (worst 0.9107):
**0 of 2,429 m in a sub-0.9 sector**, against 100 % of its old bad ground. Moving the
route fixed both the water and the connectivity.

--------------------------------------------------------------------------------
## STP-846 - T02 IS the ATTACK fallthrough, and here is exactly what it does

**READ THIS BEFORE THE DEMO.** T02's `TaskActionCode` is `ATTACK`. The live STP scenario
gives the same task `task_type: "ReceiveOrderableActivity"`, and its own `Name` says what
it really is:

> *T2_28IdHqTransitionsFromOffensiveOperationsToConsolidationAndSecurity,
> Re-EstablishesAoBoundaries,AndCoordinatesWith29IdAndFollow-OnForces...*

A "receive / transition to consolidation and security" task is exported as `ATTACK`.
`ATTACK` is the connector's generic fallthrough for any task type it has no C2SIM verb
for - T14 is a second instance, from `FollowAndSupportFriendlyUnit`. **The derived file
keeps the code exactly as exported.**

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

--------------------------------------------------------------------------------
## What the cut does at run time

### Geometry the interface will drive
MapGraphic wins over the embedded `Location` (`TaskGeometryResolver.cs:170`), and
`Vrf:DropOriginVertexMeters` (100 m) drops a first vertex that sits on the taskee.

| task | taskee | from | to | driven |
|---|---|---|---|---|
| T02 | 28ID | 53.992385, 23.211255 | 54.028874, 23.264401 | **5,341 m** |
| T10 | 1-112 IN | 54.042688, 23.308235 | 54.019389, 23.313902 | **2,617 m** |
| T14 | 48 IBCT | 54.019389, 23.313902 | 54.040348, 23.336445 | **2,757 m** (origin vertex dropped at 0.0 m) |

T01 and T13 carry four MapGraphicIDs each, but `HoldInPlace` issues NO VR-Forces task
(`VrfC2SimService.cs:3018-3031`) and logs that the geometry was not driven, so **their
39-45 km of resolved zig-zag is never driven and never needs nav coverage.**

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

**0 of 3 legs cross water; 0 flagged for grade; 0 NO VERDICT; terrain 120.6-164.1 m.** No
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
