# UNIT TYPE MAPPING - FIDELITY PASS (2026-09-02)

Doc-only, read-only survey. No code, no live VR-Forces, nothing written under `C:\MAK`.
ASCII only. Supersedes the *recommendations* in `docs/TYPE_GAP_ADJUDICATION.md` and
`docs/TYPE_MAPPING_TABLE.md` where they conflict; their VERIFIED template facts are reused
and re-checked, and two of their conclusions are CORRECTED here (sec 3.6, sec 8).

## 0. Governing rulings, scope, and evidence convention

USER RULINGS in force (recorded by the supervisor, 2026-09-02):

- **R-FIDELITY.** "The ported C++ interface took many shortcuts... I want the best possible
  fidelity using available VRF resources. Map units to their actual correct representation
  in VRF. Go find the DIS if you have to." The C++ oracle's type mapping (which
  `src/VrfC2SimApp/UnitTranslator.cs` ports 1:1) is therefore NOT the reference. The
  installed VR-Forces catalog + the DIS enumerations are.
- **R-HOSTILE-NATION.** The hostile force nation is a **configuration option**, not a fixed
  choice. Both PRC (DIS country 45) and RUS (DIS country 222) must be fully supported as
  selectable opposing nations (European customers pick RUS, INDOPACOM picks PRC). Friendly
  stays USA (225) unless the init says otherwise.
- **R-ENTITY-LEVEL** (2026-07-17, `docs/TYPE_GAP_ADJUDICATION.md` "USER RULINGS"): stay
  entity-level; `AggregateLevel.sms` content is out of bounds because aggregate-level units
  mostly cannot disaggregate and so cannot perform combat tasks.
- **R-SURFACE-PROXY** (2026-07-17): proxy substitutions must be surfaced to downstream
  C2SIM consumers.
- **R-AUTHORING-IN-SCOPE** (2026-07-17): authoring new entity-level templates is in scope.

Evidence convention used throughout:

- **VERIFIED** = read this pass from a named file (repo file, `C:\MAK` file, or a cited URL).
- **INFERRED** = derived by an algorithm or by reasoning over VERIFIED facts; the derivation
  is stated so it can be attacked.
- **UNVERIFIED** = carried from an earlier repo doc and not re-read this pass.

Method note (per the supervisor's steer): this pass starts from the existing repo research
(`docs/TYPE_GAP_ADJUDICATION.md`, `docs/TYPE_MAPPING_TABLE.md`, `docs/VRF_GROUND_TRUTH.md`
sec 0.1) and from the vendor's own documentation of the resolution rules, then re-derives
only what the mapping needs. It is not a walk through the catalog: the catalog was indexed
mechanically (sec 3.1) and the resolution rule was implemented and validated (sec 2.3)
rather than sampled.

---

## 1. What the fixture actually loads (the frame every claim below sits in)

VERIFIED, from the fixture itself:

| fact | value | source |
|------|-------|--------|
| fixture scenario | `TropicTortoise_FFRTC.scnx` | `runs/20260902T143638Z_run/run-manifest.json` (`"scenario": "TropicTortoise_FFRTC"`) |
| its simulation model set | `$(DATA_DIR)\simulationModelSets\C2simEx.sms` | `TropicTortoise_FFRTC.scn`, line `(Simulation-Model-Set-Files ...)` inside the `.scnx` zip |
| frame mode | `fixed-frame-run-to-complete`, frame-time 0.033333 | same file |
| SMS include chain | `C2simEx.sms` -> `EntityLevel.sms` -> `base.sms` | `C:\MAK\vrforces5.0.2\data\simulationModelSets\C2simEx.sms` line `(include "..\data\simulationModelSets\EntityLevel.sms")`; `EntityLevel.sms` `(include "..\data\simulationModelSets\base.sms")`; `base.sms` has no `(include ...)` |
| terrain | `MAK Earth Space (online).mtf` | same `.scn` |

So the **entire mapping is constrained to the three directories**
`C:\MAK\vrforces5.0.2\data\simulationModelSets\{C2simEx,EntityLevel,base}\vrfSim\`.
`AggregateLevel.sms` is a sibling set the fixture never loads, and R-ENTITY-LEVEL forbids
adding it. Every "IN-CHAIN" claim below means: resolvable by the fixture as it stands.

---

## 2. The vendor mechanics this mapping must obey (the 101, from the docs)

### 2.1 The eight-field object type

VERIFIED, `C:\MAK\vrforces5.0.2\doc\help\Content\SimulationModels\ObjectParameterDatabase\ObjectTypes.htm`:

> "VR-Forces uses an eight-digit enumeration scheme for specifying objects. The enumerations
> are based on the seven-digit enumerations used by DIS and the RPR FOM. The additional field
> distinguishes between individual and unit objects."

> "For more information about the object enumeration scheme, see the SISO Enumerations
> Document (SISO Reference for Enumerations for Simulation Interoperability)."

Field order: `superType : Kind : Domain : Country : Category : Subcategory : Specific : Extra`.

Super-type values (same file, Table 63): 0 Other, 1 Individual (platform entities), 2 Unit,
**3 Disaggregated unit (a unit composed of other simulation objects)**, 4 Aggregated unit
(a unit that does not have subordinate simulation objects).

Our C# `EntityTypeSpec` carries only the 7 DIS fields; the facade prepends 3 for aggregates
and 1 for entities (VERIFIED `src/VrfC2SimApp/UnitTranslator.cs:152-153` `Spec(...)`, and
the resulting types match the on-disk `objectType=` strings exactly - see sec 2.3).

### 2.2 Published type vs matching type - and why our emitted type is only a SELECTOR

VERIFIED, same `ObjectTypes.htm`:

> "The published object type is used as follows: When the front-end sends a create simulation
> object message to the back-end, the back-end uses the object type to determine which model
> to create. When the back-end publishes a simulation object on the network it uses the
> published object type as the object enumeration. Each field of a published object type must
> have a specific value."

> "The matching object type is used as follows: When the back-end receives a request to create
> a simulation object and it cannot find an exact match for the object type that it is sent,
> it finds the best match among matching object types to determine which model to create...
> The matching type can be the same as the published object type or it can have wildcards (-1)."

**Load-bearing consequence, INFERRED from those two paragraphs:** the object type our
interface emits is a *selector against `matchType`*. What goes out on the wire is the
selected template's own `objectType`. So "the DIS enumeration the unit should carry" is
**not something we choose** - it is a property of the template we land. Our design freedom
is exactly: pick the template whose published enumeration and composition are correct. This
reframes the whole exercise: the deliverable is a template choice, and the emitted type is
whatever hits it exactly.

### 2.3 The best-match method - implemented and validated, not sampled

VERIFIED, same file:

> "When VR-Forces looks up an object type, it begins at the root of the tree, working its way
> down until no better matches are found (the best match method.)"

with the worked A-10 example: a discovered `1:1:2:225:2:11:0:0` matches "any object", "any
fixed-wing", "any U.S. attack fixed-wing", but **not** the A-10 matchType, because "Although
the first five fields match, the sixth is different and is not wild carded."

I implemented that rule over every `matchType` in the three in-chain directories (a candidate
matches iff each field is `-1` or equal; best = deepest run of leading specific fields, then
most specific fields overall) and **validated it against the six resolutions
`docs/VRF_GROUND_TRUTH.md` sec 0.1.5 states as VERIFIED**:

| query | resolver output | 0.1.5 expects | |
|-------|-----------------|---------------|---|
| `3:11:1:225:5:2:0:0` | Tank Company (USA) | Tank Company (USA) | OK |
| `3:11:1:225:1:1:3:0` | Ground_Aggregate | Ground_Aggregate | OK |
| `3:11:1:225:5:20:0:0` | Ground_Aggregate | Ground_Aggregate | OK |
| `3:11:1:225:2:1:1:0` | Ground_Aggregate | Ground_Aggregate | OK |
| `3:11:1:0:13:34:0:1` | Mobile Irregular | Mobile Irregular | OK |
| `3:11:1:225:3:2:0:0` | Tank Platoon (USA) | Tank Platoon (USA) | OK |

6/6. Every "resolves to" claim later in this document is that validated resolver's output
(INFERRED by algorithm, over VERIFIED file data). Residual risk: `docs/VRF_GROUND_TRUTH.md`
sec 0.1.8 item 1 records one *live* observation (2026-07-15, `114.MechCoy` formation list
came back lowercase = the `Ground_Aggregate` signature) that has never been reconciled with
the static rule. That is the single outstanding falsifier for the whole static method; sec 7
makes reading a live `vrfSim.log` creation line the first verification step.

### 2.4 What the unit fields MEAN (the DIS aggregate enumerations)

VERIFIED from the vendor's own appendix, which is the authority on what VR-Forces means by
these fields:

**Category (field 5) = echelon** -
`C:\MAK\vrforces5.0.2\doc\help\Content\Appendixes\Parameters\vrf_aggregateSubcategory.htm`,
"Unit Category" table: 0 Other, 1 IndividualVehicle, 2 Element, 3 **Platoon**, 4 **Battery**,
5 **Company**, 6 **Battalion**, 7 Regiment, 8 **Brigade**, 9 Division, 10 Corps, 11 Force,
12 **Team**, 13 **Squad**, 14 **Section**.

**Subcategory (field 6) = branch / "Echelon Type"** - same file, "Unit Subcategory" table:
0 Other, 1 CavalryTroop, 2 **Armor**, 3 **Infantry**, 4 **MechanizedInfantry**, 5 Cavalry,
6 **ArmoredCavalry**, 7 **Artillery**, 8 **SelfPropelledArtillery**, 9 CloseAirSupport,
10 **Engineer**, 11 **AirDefenseArtillery**, 12 **AntiTank**, 13 ArmyAviationFixedWing,
14 ArmyAviationRotaryWing, 15 ArmyAttackHelicopter, 16 AirCavalry, 17 ArmorHeavyTaskForce,
18 MotorizedRifle, 19 MechanizedHeavyTaskForce, 20 **CommandPost**, 21 CEWI, 22 TankOnly,
23 ForceFriendly, 24 ForceOpposing.
  *Doc defect noted:* the value cell for **Cavalry** is EMPTY in the shipped HTML table; 5 is
  the only unused slot between MechanizedInfantry (4) and ArmoredCavalry (6), so **Cavalry=5
  is INFERRED by position**, not read. Nothing in this mapping depends on it.

**Specific (field 7) = does the unit contain a headquarters** -
`.../Parameters/vrf_aggregateSpecific.htm`: "The Specific field enumerations for Kind field =
Military Hierarchy specify whether the unit contains a headquarters": **0 = NoHeadquarters,
1 = ContainsHeadquarters**. This single sentence explains `TYPE_GAP_ADJUDICATION.md` DECISION
ITEM 4 completely: `aggregate-Company-HQ-Friendly` is `...:5:20:**1**:0` because it is a
company-echelon command post that *contains* an HQ; our `ArmorCoHQ` factory emits Specific=0,
i.e. "no headquarters", which is semantically wrong as well as a non-match.

**Kind (field 2) = 11 MilitaryHierarchy** - `.../Parameters/vrf_aggregateKind.htm`: "The
enumeration for DtAggregateKind should start with 0, but VR-Forces starts with 10... Other 10,
MilitaryHierarchy 11, CommonType 12, CommonMission 13, SimilarCapabilities 14,
CommonLocation 15."

**MAK extensions beyond the documented table (INFERRED from disk):** the in-chain catalog
uses Subcategory values 27 (fire-support / observer teams: `COLT Team`, `Fire Support Team`),
30 (`AR Scout`), 31 (combat service support), 32 (paratrooper), 34 (irregular/civilian) and
Extra values used as variant discriminators (78 = mine plow, 127 = Stryker, 49 = BMP-2,
58/59 = weapons). These are not in the shipped appendix table; they are read from the
`.entity` files. Treat them as vendor extensions, not standard DIS.

**Country codes.** SISO-REF-010 is the upstream authority
(https://www.mixr.dev/assets/pages/interop/siso-ref-010-v28.pdf , SISO-REF-010-2020 v28;
also https://cdn.ymaws.com/www.sisostandards.org/resource/resmgr/reference_documents_/siso-ref-010.1-2019_operatio.pdf ).
I could not extract the specific numeric country table from those PDFs in this pass, so the
three codes this mapping depends on are **VERIFIED on disk instead**, by cross-referencing
each template's `objectType` Country field against its own `gui-deployable-countries` string:

| DIS country | ISO on disk | evidence file |
|---|---|---|
| 225 | `"US"` | `EntityLevel\vrfSim\Tank Company (USA).entity` (`3:11:1:225:5:2:0:0`, `gui-deployable-countries "US"`) |
| 222 | `"RU"` (+ the Soviet-export list) | `EntityLevel\vrfSim\2A18 D-30 Howitzer.entity` (`1:1:1:222:5:4:0:0`, deployable list includes `"RU"`) ; `Tank Platoon (RUS).entity` `3:11:1:222:3:2:0:0` / `"RU"` |
| 45 | `"CN"` | `EntityLevel\vrfSim\Type_99_MBT.entity` (`1:1:1:45:1:9:1:0`, `gui-deployable-countries "CN"`) ; `Chengdu J-10C.entity` (`1:1:2:45:1:5:6:0`, `"CN"`) |

(Also observed, not needed: 71 -> FR/DE/ES/GB group via `A400M Atlas.entity`; 153 -> `"NL"`
via `De Zeven Provincien Class Frigate.entity`; 224 -> UK/allies via `Aardvark JSFU.entity`.)

### 2.5 How a unit template is composed - and the correction on "organization files"

The brief anticipated "organization files". **There are none in this SMS**: a
`find C:\MAK\vrforces5.0.2\data -iname "*organization*"` returns nothing, and every in-chain
unit template is a single `.entity` XML file whose composition is an inline
`<subordinates>` list. VERIFIED example,
`EntityLevel\vrfSim\Tank Company (USA).entity`:

```
<simObject objectType="3:11:1:225:5:2:0:0" matchType="3:11:1:225:5:2:-1:-1"
           platform="@(platforms-dir)/HigherAggregate.ope">
   ...
   <string paramName="echelon-level">Co</string>
   <string paramName="gui-deployable-countries">"US"</string>
   <subordinates paramName="subordinates">
      <subordinate objectType="3:11:1:225:14:2:1:0" ... functionHandle="HQ"/>
      <subordinate objectType="3:11:1:225:3:2:0:0"  ... functionHandle="TANK"/>
      <subordinate objectType="3:11:1:225:3:2:0:0"  ... functionHandle="TANK"/>
      <subordinate objectType="3:11:1:225:3:2:0:0"  ... functionHandle="TANK"/>
   </subordinates>
</simObject>
```

**Each `<subordinate objectType>` is itself resolved by the same best-match rule** - which is
how a defective template can silently contain generic units (sec 3.6).

Vendor documentation of the authoring workflow (VERIFIED, `doc\help\Content\SimulationModels\`):
- `EntityLevelScenariosUnits\EntityLevelScenarioUnitsCreate.htm`: "Follow the procedures for
  creating a new entity in 'Creating a New Simulation Object'. Add subordinates, as described
  in 'Adding Subordinates'. Optionally, configure formations."
- `EntityLevelScenariosUnits\UnitsEditingIntro.htm`: "in entity-level scenarios, VR-Forces
  models the individual entities of a unit... when they engage in combat, they disaggregate
  into their leaf-level entities and interact at the individual entity level" - the fact
  R-ENTITY-LEVEL rests on.
- `EntityLevelScenariosUnits\SubordinateFunction.htm`: subordinate `functionHandle`s are
  "text-based handles that identify a role or function that the subordinate performs in that
  unit, which are then used in unit behaviors."
- `EntityLevelScenariosUnits\UnitCompositionEdit.htm`: "Some units are configured as 'empty'
  units - they have no subordinates. These empty units are still available to the list of
  choices in the Aggregate As dialog box and **for mapping to units when you import MSDL
  scenarios**." This is the vendor's own statement of what the abstract Country-0 templates
  are for; it is also why landing one is a silent failure for us (sec 3.5).
- `ObjectParameterDatabase\UnitsConfigure.htm`: "In entity-level scenarios, most functionality
  resides in entities and units have relatively few individual parameters. The principle
  function of the various elements is to specify controllers and formations, and to specify
  the echelon-level for the different levels of units."

The 4.10 Developer's Guide chapters (5.x dropped them; memory note `mak-developer-docs-urls`)
confirm the runtime side, not a file format:
- https://docs.mak.com/api/vrforces4.10/classref/vrf_the_organization_manager.html -
  "It maintains the organizational hierarchy for all organized simulation objects";
  "Organized simulation objects in VR-Forces exist in an organization tree beneath a
  force-level superior"; echelon IDs are assigned at runtime and "The echelon ID for a
  simulation object is not available immediately upon creation."
- https://docs.mak.com/api/vrforces4.10/classref/entitymodels_aggregates.html -
  "Each entry in the list specifies the DIS/RPR FOM entity type of the subordinate to create,
  and its initial position and heading"; "Aggregates may have individual (platform-level)
  subordinates, aggregate subordinates, or a combination of both."

**Conclusion (INFERRED):** "authoring an organization" for our purposes = writing one more
`.entity` XML file in an SMS directory, with an `objectType`/`matchType` pair that nothing
else claims and a `<subordinates>` list of existing leaves. It is a text edit, not a GUI-only
operation - which matters because the product is headless (memory `headless-goal-no-gui`).
The GUI Simulation Object Editor is one way to produce that file, not the only way.

---

## 3. The VR-Forces catalog for the modelling world in use

### 3.1 Index method

Every `.entity` under the three in-chain `vrfSim` directories was parsed for its
`<simObject objectType= matchType= >` attributes plus `echelon-level`, `gui-can-create`,
`gui-deployable-countries` and its `<subordinate>` count. Result: **115 in-chain ground unit
templates** (`objectType` prefix `3:11:1`). This is a complete enumeration, not a sample.
(`docs/TYPE_MAPPING_TABLE.md` sec 4 listed 25 of them; all 25 re-verified here with identical
objectType/matchType.)

### 3.2 The USA (225) in-chain ground unit templates - complete

| objectType | matchType | ech | create | nsub | template file (`EntityLevel\vrfSim\`) |
|---|---|---|---|---|---|
| 3:11:1:225:3:0:0:1 | exact | Plt | - | 4 | aggregate-Plt-M1A2.entity |
| 3:11:1:225:3:2:0:0 | 3:11:1:225:3:2:-1:-1 | Plt | True | 4 | Tank Platoon (USA).entity |
| 3:11:1:225:3:2:0:78 | exact | Plt | True | 4 | Tank Platoon (USA) Mine Plows.entity |
| 3:11:1:225:3:3:1:0 | exact | PLT | True | 5 | Infantry Platoon (USA Army).entity |
| 3:11:1:225:3:4:0:0 | exact | PLT | False | 7 | Mechanized Platoon (USA) IFV (Deprecated).entity |
| 3:11:1:225:3:4:0:127 | exact | PLT | False | 9 | Stryker Rifle Platoon (USA Army).entity |
| 3:11:1:225:3:7:0:0 | 3:11:1:225:3:7:-1:-1 | PLT | True | 6 | Field Artillery Platoon (USA) M777.entity |
| 3:11:1:225:3:8:0:0 | 3:11:1:225:3:8:-1:-1 | PLT | True | 6 | Field Artillery Platoon (USA) M109.entity |
| 3:11:1:225:3:11:0:0 | 3:11:1:225:3:11:-1:-1 | PLT | True | 4 | Air Defense Artillery Platoon (USA).entity |
| 3:11:1:225:3:20:1:0 | exact | Plt | - | 4 | aggregate-Plt-HQ-Friendly.entity |
| 3:11:1:225:3:31:0:0 | 3:11:1:225:3:31:-1:-1 | PLT | True | 4 | Combat Service Support Platoon (USA).entity |
| 3:11:1:225:4:7:0:0 | 3:11:1:225:4:7:-1:-1 | BTY | True | 3 | Field Artillery Battery (USA) M777.entity |
| 3:11:1:225:4:8:0:0 | 3:11:1:225:4:8:-1:-1 | BTY | True | 3 | Field Artillery Battery (USA) M109.entity |
| 3:11:1:225:5:0:0:1 | exact | Co | - | 3 | aggregate-Co-M1A2.entity |
| 3:11:1:225:5:2:0:0 | 3:11:1:225:5:2:-1:-1 | Co | True | 4 | Tank Company (USA).entity |
| 3:11:1:225:5:2:0:78 | exact | Co | True | 4 | Tank Breach Company (USA).entity |
| 3:11:1:225:5:3:1:0 | exact | Co | - | 4 | aggregate-Co-Infantry-Friendly.entity **(defective - sec 3.6)** |
| 3:11:1:225:5:20:1:0 | exact | Co | - | 4 | aggregate-Company-HQ-Friendly.entity |
| 3:11:1:225:12:3:0:{0,1,2,3,58,75,126,127} | exact | FT | mixed | 2-4 | Mechanized/Rifle/Infantry Fire Team, Machinegun Team M240, Vehicle Crew/Team |
| 3:11:1:225:12:3:1:0 | exact | - | True | 4 | Infantry Headquarters Team (USA Army).entity |
| 3:11:1:225:12:27:0:0 | 3:11:1:225:12:27:-1:-1 | Team | True | 2 | COLT Team (USA).entity |
| 3:11:1:225:12:27:0:1 | exact | Team | True | 4 | Fire Support Team (USA).entity |
| 3:11:1:225:12:32:0:4 | exact | FT | True | 4 | Paratrooper Fire Team (USA).entity |
| 3:11:1:225:13:3:0:0 | exact | SQD | True | 3 | Mechanized Squad (USA Army).entity |
| 3:11:1:225:13:3:0:1 | **3:11:1:-1:13:-1:-1:-1** | - | False | 4 | Infantry Squad (USA) (Deprecated).entity - *the catch-all for every Cat-13 squad of any country* |
| 3:11:1:225:13:3:0:2 / :3 / :4 / :58 | exact | SQD | mixed | 3-5 | Rifle Squad (USA Army) / (USA Marines) / No-Leader / Weapons Squad |
| 3:11:1:225:13:4:0:59 / :127 | exact | SQD | False | 6 / 4 | Stryker Weapons Squad / Strkyer Rifle Squad (sic) |
| 3:11:1:225:14:2:1:0 | 3:11:1:225:14:2:1:-1 | HQ Sec | True | 6 | Tank Headquarters Section (USA).entity |
| 3:11:1:225:14:3:0:0 | exact | Sectn | - | 4 | aggregate-DI-Sectn-Friendly.entity |
| 3:11:1:225:14:3:1:127 | exact | - | False | 4 | Stryker Rifle Headquarters Section (USA Army).entity |
| 3:11:1:225:14:7:0:0 / 14:7:1:0 / 14:8:0:0 | wild Extra | SEC | True | 1-2 | FA Section M777 / FA HQ Section / FA Section M109 |
| 3:11:1:225:14:12:0:0 | exact | FT | True | 2 | Antitank Team (USA Army) Javelin.entity |
| 3:11:1:225:14:30:0:1 | exact | Plt | True | 3 | **AR Scout.entity** `[C2simEx]` - 3x M3A2 Bradley CFV |
| 3:11:1:225:14:31:0:0 | exact | SEC | True | 2 | Supply Section (USA).entity |

**USA gaps at the Category level (VERIFIED by absence in the complete index):** there is **no
Country-225 template at Category 6 (Battalion), 8 (Brigade) or 9 (Division)** - the only
battalion/brigade/division entries in the chain are Country-0 abstracts with zero
subordinates. There is **no Country-225 Subcategory-10 (Engineer)** template at any echelon,
and **no Subcategory-6 (ArmoredCavalry) / recon** template for the USA except the C2simEx
`AR Scout` (3x Bradley CFV, Subcat 30).

Key USA compositions (VERIFIED, expanded from the files two levels deep):

- **Tank Company (USA)** `3:11:1:225:5:2:0:0` = 1x Tank HQ Section (USA) `[HQ]` + 3x Tank
  Platoon (USA) `[TANK]`. HQ Section = 2x M1A2 (CDR, XO) + 1x M3A2 Bradley CFV (FSO) +
  M577A2 Command Post + 2x M998 HMMWV. Each Tank Platoon = 4x M1A2. Movement
  `ground-higherUnit-disaggregated-movement.sysdef`.
- **Tank Breach Company (USA)** `3:11:1:225:5:2:0:78` = Tank HQ Section + 2x Tank Platoon +
  1x Tank Platoon (USA) Mine Plows (2x M1A2 SEP V2 Mineroller + 2x Mineplow).
- **Field Artillery Battery (USA) M109** `3:11:1:225:4:8:0:0` = FA HQ Section (M1068 CP +
  M1068 SICPS FDC) + 2x FA Platoon M109; each platoon = M1068 CP + M1068 SICPS + 4x FA
  Section M109, each section = 1x M109A5 SP Howitzer + 1x M992A2 FAASV. Total 8 tubes.
- **Infantry Platoon (USA Army)** `3:11:1:225:3:3:1:0` = Infantry HQ Team (4x US_Army_M4) +
  3x Rifle Squad (USA Army) + 1x Weapons Squad (USA Army) (2x M240 MG team + 2x Javelin AT
  team). A genuinely well-composed dismounted platoon.
- **Mechanized Platoon (USA) IFV (Deprecated)** `3:11:1:225:3:4:0:0` = 4x M2A2 Bradley IFV +
  3x `3:11:1:225:13:4:0:0`; that squad type has no exact leaf and best-matches
  `Infantry Squad (USA) (Deprecated)` via the `3:11:1:-1:13:-1:-1:-1` wildcard (4 dismounts).
  So the deprecated platoon really does deliver 4 Bradleys + 12 dismounts. `gui-can-create`
  False and the name says Deprecated, but it is the only IFV-mounted USA mech-inf unit in
  the chain.
- **Air Defense Artillery Platoon (USA)** `3:11:1:225:3:11:0:0` = 4x HMMWV_with_Avenger.
- **Combat Service Support Platoon (USA)** `3:11:1:225:3:31:0:0` = 2x M977 HEMTT Cargo +
  2x M978 HEMTT Fuel.
- **Antitank Team (USA Army) Javelin** `3:11:1:225:14:12:0:0` = US_Army_Javelin + US_Army_M4.
- **Fire Support Team (USA)** `3:11:1:225:12:27:0:1` = 4x US_Army_M4; **COLT Team (USA)**
  `3:11:1:225:12:27:0:0` = 2x DI_Lasing_(US).
- **aggregate-Company-HQ-Friendly** `3:11:1:225:5:20:1:0` = 4x `1:3:1:225:1:1:0:0`, which
  best-matches `generic-lifeform-platform` - four anonymous foot figures. Thin, but a real
  exact-match leaf.

### 3.3 The RUS (222) in-chain ground unit templates - complete

| objectType | matchType | ech | create | nsub | file |
|---|---|---|---|---|---|
| 3:11:1:222:3:0:0:1 | exact | Plt | - | 3 | aggregate-Plt-T80.entity |
| 3:11:1:222:3:2:0:0 | 3:11:1:222:3:2:-1:-1 | Plt | True | 3 | Tank Platoon (RUS).entity - 3x T-80 |
| 3:11:1:222:3:3:0:0 | 3:11:1:222:3:3:-1:-1 | PLT | False | 2 | Infantry Platoon (RUS) (Deprecated).entity - 2x Infantry Squad (RUS) |
| 3:11:1:222:3:3:1:0 | exact | Plt | - | 4 | aggregate-Plt-Infantry-Hostile.entity - 3x DI-Sectn + Plt-HQ |
| 3:11:1:222:3:4:0:0 | 3:11:1:222:3:4:-1:-1 | Plt | False | 6 | Mechanized Platoon (RUS) (Deprecated).entity - 4x BMP-2 + 2x Infantry Squad (RUS) |
| 3:11:1:222:3:6:0:49 | exact | Plt | True | 3 | Recon Vehicle Platoon (RUS BMP2).entity - 3x BMP-2 |
| 3:11:1:222:3:11:0:0 | 3:11:1:222:3:11:-1:-1 | PLT | True | 4 | Air Defense Artillery Platoon (RUS).entity - 4x SA-9 Gaskin |
| 3:11:1:222:3:20:1:0 | exact | Plt | - | 4 | aggregate-Plt-HQ-Hostile.entity |
| 3:11:1:222:3:31:0:0 | 3:11:1:222:3:31:-1:-1 | PLT | True | 3 | Combat Service Support Platoon (RUS).entity - 3x ZIL-135 |
| 3:11:1:222:5:0:0:1 | exact | Co | - | 4 | aggregate-CO-T80.entity |
| 3:11:1:222:5:2:0:0 | 3:11:1:222:5:2:-1:-1 | Co | True | 4 | Tank Company (RUS).entity - HQ Sec + 3x Tank Plt (9x T-80 + 2x T-80 CDR/XO) |
| 3:11:1:222:5:3:1:0 | exact | Co | - | 4 | aggregate-Co-Infantry-Hostile.entity **(partly defective - sec 3.6)** |
| 3:11:1:222:5:20:1:0 | exact | Co | - | 4 | aggregate-Company-HQ-Hostile.entity - 4 generic life forms |
| 3:11:1:222:12:27:0:0 | 3:11:1:222:12:27:-1:-1 | Team | True | 2 | COLT Team (RUS).entity - 2x DI_Lasing_(CIS) |
| 3:11:1:222:13:3:0:0 | 3:11:1:222:13:3:-1:-1 | Squad | False | 4 | Infantry Squad (RUS) (Deprecated).entity - 2x RPG + 2x AK-47 |
| 3:11:1:222:14:2:1:0 | 3:11:1:222:14:2:1:-1 | Co HQ | True | 6 | Tank Headquarters Section (RUS).entity - 2x T-80 + BMP-2 + GAZ-69 x2 + ZIL-135 |
| 3:11:1:222:14:3:0:0 | exact | Sectn | - | 4 | aggregate-DI-Sectn-Hostile.entity |
| 3:11:1:222:14:3:1:127 | exact | - | True | 4 | Infantry Headquarters Section (RUS Army).entity (note: its `gui-deployable-countries` says `"US"` - a vendor data defect) |

**RUS gaps (VERIFIED by absence):** no Country-222 artillery of any kind (no Cat 4, no
Subcat 7/8), no engineer (Subcat 10), no anti-tank team (Subcat 12), no target-acquisition
beyond the COLT team, no battalion/brigade echelon, and no armored-cavalry company.

**RUS platform leaves are abundant** (69 Country-222 ground platforms indexed), including
tanks T-55/T-62/T-72/T-80/T-90M/T-14, IFVs BMP-1/2/2M/3, BMD-2/3, T-15, K-17 Bumerang,
APCs BTR-60/80/90, MT-LB, recon BRDM-2 KPVT, SP artillery 2S1 Gvozdika / 2S19 Msta-S /
2S31 Vena / 2S35 Koalitsiya, towed D-20 and 2A18 D-30, rocket BM-21 2B17 and TOS-1 Buratino,
ATGM carrier GAZ-233114 Tigr Kornet-EM, SHORAD SA-9/13/15/19/22 and ZSU-23-4, and trucks
Ural 4320/5323, GAZ-66, ZIL-135. **Authoring RUS units is well supplied.**

### 3.4 The PRC (45) content - the decisive finding for R-HOSTILE-NATION

**There are ZERO Country-45 unit templates in the chain.** A full scan of every `simObject`
in the three in-chain directories found 35 Country-45 entries and **every one is `Kind=1`
(a platform)**; none is `3:11:...` (a unit). Confirmed twice: once by filtering the complete
115-row unit index (no `45` rows), once by listing all Country-45 objects.

The **PRC ground (Domain 1) platform leaves that DO exist** - the authoring raw material -
VERIFIED from the files (all `gui-can-create True`):

| objectType | template file (`EntityLevel\vrfSim\`) | gui-deployable-countries | role |
|---|---|---|---|
| 1:1:1:45:1:9:1:0 | Type_99_MBT.entity | "CN" | modern MBT |
| 1:1:1:45:1:2:1:0 | T-69_MBT.entity | "BD" "CN" "IQ" "MM" "PK" "LK" "SD" "TH" "ZW" | legacy MBT |
| 1:1:1:45:2:2:0:0 | Type 85 (YW 531H) APC.entity | "BD" "CN" "MM" "LK" "TH" | tracked APC |
| 1:1:1:45:2:6:0:0 | WZ551 APC.entity | "AR" ... "CN" ... (19 nations) | wheeled APC |
| 1:1:1:45:4:20:0:0 | PLZ-45 Howitzer.entity | "DZ" "CN" "KW" "SA" | 155 mm SP howitzer |
| 1:1:1:45:4:50:0:0 | PHZ 89.entity | "CN" | 122 mm SP MRL |
| 1:1:1:45:4:43:0:0 | PHL 03 (Type 03) MLRS.entity | "JO" "RO" "SG" "AE" "US" *(vendor data defect - CN missing)* | 300 mm MLRS |
| 1:1:1:45:5:4:0:0 | Type 66 Howitzer.entity | "AL" "AO" "CN" "LK" | towed 152 mm |
| 1:1:1:45:28:7:2:0 | HQ-9 Self Propelled TEL.entity | "DZ" "CN" "MA" "PK" "TM" "UZ" | long-range SAM TEL |
| 1:1:1:45:28:1:2:0 | HQ-2 Launcher.entity | "DZ" "CN" "MA" "PK" "TM" "UZ" | legacy SAM launcher |

Not present for PRC: any life form / dismounted soldier (a scan for `1:3:...:45:...` found
none), any IFV proper (the two PRC vehicles are APCs, not IFVs), any dedicated recon vehicle,
any engineer vehicle, any utility/cargo truck, any command-post vehicle.

**CORRECTION OF RECORD.** The project memory note `c2sim-vrf-port` / the 2026-07-17
`TYPE_GAP_ADJUDICATION.md` "USER RULINGS" entry states the installed Chinese content is
"air / naval / air-defense only" with "ZERO Chinese ground-combat platforms (no tank, IFV,
APC)". **That is FALSIFIED**: Type 99 MBT, T-69 MBT, Type 85 APC and WZ551 APC are installed
Country-45 ground-combat platforms, plus three PRC artillery/MRL systems. The "zero Chinese
aggregates" half of that claim is CONFIRMED. The practical difference is large: a PRC force
is now assemblable from installed content by authoring **unit** templates only, with no need
to author platforms or find 3D models - except for dismounted infantry, which has no PRC leaf.

### 3.5 The silent-failure trap: what a Country-45 request does TODAY

INFERRED (validated resolver, sec 2.3) - this is why PRC cannot be reached by simply
switching the Country field:

| we emit | resolves to | what the operator sees |
|---|---|---|
| `3:11:1:45:5:2:0:0` (PRC armor company) | `Tank Company.entity` (Country 0 abstract, `gui-can-create False`, **nsub=0**) | an EMPTY unit |
| `3:11:1:45:3:2:0:0` (PRC armor platoon) | `Tank Platoon.entity` (abstract, nsub=0) | an EMPTY unit |
| `3:11:1:45:5:4:0:0` (PRC mech company) | `Mechanized Company.entity` (abstract, nsub=0) | an EMPTY unit |
| `3:11:1:45:3:4:0:0` (PRC mech platoon) | `Mechanized Platoon.entity` (abstract, nsub=0) | an EMPTY unit |
| `3:11:1:45:5:3:0:0` / `3:11:1:45:3:3:0:0` | `Infantry Company` / `Infantry Platoon` (abstract, nsub=0) | an EMPTY unit |
| `3:11:1:45:{5:20, 14:2, 5:10, 4:8, 3:8, 3:11, 3:6, 14:12, 12:27, 3:31}...` | `Ground_Aggregate` | 4 anonymous vehicles |

Those Country-0 abstracts are exactly the "empty units" the vendor documents as MSDL-import
mapping targets (`UnitCompositionEdit.htm`, quoted in sec 2.5). Landing one is **worse than
today's generic fallback**, because `Ground_Aggregate` at least ships four vehicles. So:
**PRC support = 100% authoring. There is no proxy path that is not a lie.**

### 3.6 Two vendor templates that are defective - CORRECTS the prior repo recommendation

`docs/TYPE_MAPPING_TABLE.md` sec 2.3 and `docs/TYPE_GAP_ADJUDICATION.md` GAP 2 both
recommend `aggregate-Co-Infantry-Friendly` as a "real COMPOSED company... HQ Sec +
3x Infantry Platoon". **Expanding the file two levels shows that is wrong.**

VERIFIED, `EntityLevel\vrfSim\aggregate-Co-Infantry-Friendly.entity` subordinates:
`3:11:1:225:14:3:1:127` `[HQ]` + 3x `3:11:1:225:3:3:0:0` `[INF]`.
INFERRED (resolver): the HQ resolves to `Stryker Rifle Headquarters Section (USA Army)`
(4x US_Army_M4 - fine), but **`3:11:1:225:3:3:0:0` matches nothing**: the real
`Infantry Platoon (USA Army)` is `3:11:1:225:3:3:**1**:0` with an *exact* matchType, so
Specific 0 != 1 is a non-match; the RUS infantry platoon is Country 222; the Cat-13 catch-all
needs Category 13. It therefore falls to `Ground_Aggregate`.

**So `aggregate-Co-Infantry-Friendly` actually creates 4 riflemen plus three generic
4-anonymous-vehicle blobs.** It must not be used as the US infantry company.

Same check on the hostile mirror: `aggregate-Co-Infantry-Hostile` = `3:11:1:222:14:3:1:0`
`[HQ]` + 3x `3:11:1:222:3:3:0:0` `[INF]`. The three INF subordinates DO resolve
(`Infantry Platoon (RUS) (Deprecated)`, 2 squads of 4 each), but the HQ type
`3:11:1:222:14:3:1:0` matches nothing (`aggregate-DI-Sectn-Hostile` is Specific 0;
`Infantry Headquarters Section (RUS Army)` is Extra 127) and falls to `Ground_Aggregate`.
**Partly defective**: 3 real RUS infantry platoons + 1 generic blob as the HQ.

This is the general lesson: **a template's own subordinate list is subject to the same
best-match rule, so "it is a real composed unit" must be verified transitively.** Every
composition in sec 3.2/3.3 above was expanded and resolved, not read at face value.

---

## 4. The unit census

### 4.1 What the C2SIM message carries - and the primary-source question

VERIFIED from the schema
`.../Library/CS/C2SIMSDK/C2SIMSDK/schemas/C2SIM_SMX_LOX_CWIX2024.xsd`:

- `SISOEntityTypeType` (line 3560) - "Simulation Entity Type as specified by SISO-REF-010-2016
  ... version 27 (2019). Used to define the type of an Entity for its representation and
  modeling in simulations." All seven DIS fields are `minOccurs="1"`. **This is the standard's
  own per-unit channel for the DIS type, including `DISCountry`** ("Provides a Country Code
  from the DIS standard IEEE 1516-2010", line 240).
- `ForceSideType` (line 4650) carries **no** country/nationality element - only
  `AbstractObjectGroup` + `ForceSideRelation`.
- `AbstractOrganizationType` (line 1342) carries an optional `CountryCode` ("A unique
  three-letter code for each nation on Earth, defined in ISO 3166-1"), and
  `EntityDescriptorType` (line 2038) carries `AffiliatedWith` (0..n) - "The isAffiliatedWith
  property defines organizations that this entity is affiliated with." So a *second*,
  indirect nationality channel exists: Unit -> EntityDescriptor/AffiliatedWith ->
  AbstractOrganization/CountryCode.
- `UnitType` (line 4960) carries `EchelonCode` (1..1).

**And the SIDC itself carries a country code.** MIL-STD-2525C SIDC positions 13-14 are a
two-letter country code and position 12 is the echelon/mobility modifier
(https://nasaworldwind.github.io/WorldWindJava/gov/nasa/worldwind/symbology/milstd2525/SymbolCode.html
- "The echelon code is associated with a MIL-STD-2525 symbol at SIDC position 12";
the same JavaDoc's field list includes a country-code field, though it does not restate the
position numbers, so **positions 13-14 = country is UNVERIFIED against the primary standard
this pass**). In COA-STP1 those positions are `-`.

Now the fixture facts (VERIFIED by parsing the init files):

| init | units | SISOEntityType content |
|---|---|---|
| `data/COA-STP1_Initialization.xml` | 128 `<Unit>` blocks (537 `SISOEntityType` blocks incl. graphics) | **every field 0 in all 537 blocks** - no DIS type at all |
| `data/R9_Mojave_Lean_Initialization.xml` | 6 units | **non-zero and meaningful**: 3x `11:1:225:3:4:0:0`, 1x `11:1:153:3:4:0:0`, 1x `11:1:71:3:4:0:0`, 1x `11:1:225:5:4:1:0` |

**This is the single most important census finding.** For the R9 fixture the C2SIM message
**already carries the primary source** - Kind 11 (MilitaryHierarchy), Domain 1, Category 3
(Platoon) / 5 (Company), Subcategory 4 (MechanizedInfantry), Specific 1 (ContainsHeadquarters)
on the company - and the port **ignores it**: `UnitTranslator.Plan` reads `u.DisEntityType`
only for the air branch (`:58`) and `u.DisDomain` only for the boat branch (`:65`), then
dispatches on the SIDC echelon character (`:67-71`). Resolving the R9 init's own declared
types gives `3:11:1:225:3:4:0:0` -> **Mechanized Platoon (USA) IFV (Deprecated)** - an exact,
branch-correct, IFV-mounted match - where the port today emits `3:11:1:225:3:2:0:0`
(Tank Platoon USA). The init is right and the translator overrides it with armor.

(The two odd countries in R9: 153 = NL, 71 = the FR/DE/ES/GB group - see sec 2.4. Both
resolve to the empty `Mechanized Platoon` abstract, i.e. the same silent-failure trap as
sec 3.5. So "trust the init's DIS type" needs the coverage table of sec 5 as a backstop.)

**Ruling asked for in sec 9 (JC-1): when the init supplies a non-zero `SISOEntityType`, is it
authoritative over the SIDC?**

### 4.2 COA-STP1 census - 44 distinct groups, 128 units

Grouping key = (SIDC affiliation, SIDC function ID = positions 5-10, SIDC echelon char =
position 12, `EchelonCode`). `SF` = friendly (Side UUID ...0001 = "NATO Coalition"),
`SH` = hostile (Side ...0002 = "WASA"). Side counts VERIFIED: 61 friendly, 67 hostile.
Function-ID decodes are from `docs/VRF_GROUND_TRUTH.md` sec 0.1.7 (which cites symbol.army's
2525C list plus unit-name corroboration) - **UNVERIFIED against the primary MIL-STD-2525C
document; sec 0.1.8 item 5 already flags UCAA=AntiArmor / UCD=AirDefense as counter-intuitive
and worth re-checking before a mapping is frozen.**

"today" = the validated resolver applied to what `UnitTranslator.Plan` emits for that group.

| # | group (aff / funcID / SIDC ech / EchelonCode) | n | sample names | today: factory -> template landed |
|---|---|---|---|---|
| 1 | SF UCA E COY (armor company) | 7 | HQ/1-35, A/1-35, HHC_TAC | ArmorCompany -> **Tank Company (USA)** |
| 2 | SH UCA E COY | 6 | 1/7154, 2/7154 | ArmorCompany -> Tank Company (USA) (wrong nation) |
| 3 | SF UCA F BN | 2 | 1-35/2/1_A, 1-35_MAIN | ArmorCoHQ -> **Ground_Aggregate** |
| 4 | SH UCA F BN | 1 | 7154/HQ_71 | ArmorCoHQ -> Ground_Aggregate |
| 5 | SF UCA H BDE | 1 | 2/1_AD/25_ | Tank -> single M1A2 entity |
| 6 | SF UCIZ E COY (mech inf) | 4 | HQ/5-20, A/5-20 | ArmorCompany -> Tank Company (USA) (wrong branch) |
| 7 | SH UCIZ E COY | 9 | 1/7152, 3/7153 | ArmorCompany -> Tank Company (USA) |
| 8 | SH UCIZ D PLT | 1 | 2/7151 | ArmorPlatoon -> Tank Platoon (USA) |
| 9 | SF UCIZ F BN | 1 | 5-20/2/1_A | ArmorCoHQ -> Ground_Aggregate |
| 10 | SH UCIZ F BN | 3 | 7151/HQ, 7152/HQ, 7153/HQ | ArmorCoHQ -> Ground_Aggregate |
| 11 | SF UCI E COY (dismounted inf) | 4 | A/1-6, B/1-6, C/1-6, HQ/1-6 | ArmorCompany -> Tank Company (USA) |
| 12 | SF UCI F BN | 2 | 1-6/2/1_AD, 1-6_MAIN | ArmorCoHQ -> Ground_Aggregate |
| 13 | SF UCR - NOS (cav sqn + troops) | 6 | 1-1/2/1_AD, HQ/1-1, A/1-1 | Tank -> single M1A2 |
| 14 | SH UCRVA D PLT (armored cav) | 3 | REC-7151..7153 | ArmorPlatoon -> Tank Platoon (USA) |
| 15 | SH UCRVA E COY | 2 | 2/7157, REC-7154/7 | ArmorCompany -> Tank Company (USA) |
| 16 | SH UCRVA F BN | 1 | 7157/HQ_71 | ArmorCoHQ -> Ground_Aggregate |
| 17 | SF UCE E COY (engineer) | 6 | A/40, B/40, 510/40 | ArmorCompany -> Tank Company (USA) |
| 18 | SF UCE F BN | 2 | 40/2/1_AD, 40_MAIN | ArmorCoHQ -> Ground_Aggregate |
| 19 | SH UCEC E COY (combat engineer) | 1 | 715EN/HQ_7 | ArmorCompany -> Tank Company (USA) |
| 20 | SH UCEC F BN | 1 | 8122/HQ_71 | ArmorCoHQ -> Ground_Aggregate |
| 21 | SF UCF - NOS (FA batteries) | 4 | A/4-27, B/4-27, C/4-27, HQ/4-27 | Tank -> single M1A2 |
| 22 | SF UCF F BN | 2 | 4-27/2/1_A, 4-27_MAIN | ArmorCoHQ -> Ground_Aggregate |
| 23 | SH UCFHE E COY (SP how) | 3 | 1/7158, 2/7158, 3/7158 | ArmorCompany -> Tank Company (USA) |
| 24 | SH UCFHE F BN | 3 | 7158/HQ, 7911/HQ, 7912/HQ | ArmorCoHQ -> Ground_Aggregate |
| 25 | SH UCFM D PLT (mortar) | 7 | 1MTR/7151, 2MTR/7153 | ArmorPlatoon -> Tank Platoon (USA) |
| 26 | SH UCFM E COY | 1 | 2MTR/7154 | ArmorCompany -> Tank Company (USA) |
| 27 | SH UCFR E COY (rocket/MLRS) | 1 | 4/7158 | ArmorCompany -> Tank Company (USA) |
| 28 | SH UCFR F BN | 1 | 7913/HQ_71 | ArmorCoHQ -> Ground_Aggregate |
| 29 | SF UCFT - NOS (fire support / obs) | 1 | TA/HQ/4-27 | Tank -> single M1A2 |
| 30 | SH UCFTR C SECT (target-acq radar) | 2 | 1/TA/7158, 2/TA/7158 | Tank -> single M1A2 |
| 31 | SH UCFTR E COY | 1 | RDR/8072 | ArmorCompany -> Tank Company (USA) |
| 32 | SH UCD D PLT (air defense) | 5 | AD/7151..7154, 1/1/8072 | ArmorPlatoon -> Tank Platoon (USA) |
| 33 | SH UCD E COY | 4 | 1/7159, 2/7159, 3/7159, 1/8072 | ArmorCompany -> Tank Company (USA) |
| 34 | SH UCD F BN | 2 | 7159/HQ, 8072/HQ | ArmorCoHQ -> Ground_Aggregate |
| 35 | SF UCD - NOS | 1 | A/6-56/HHC | Tank -> single M1A2 |
| 36 | SH UCAA D PLT (anti-armor) | 7 | WPN/7151..7154, 3/5/7158 | ArmorPlatoon -> Tank Platoon (USA) |
| 37 | SF US E COY (CSS / FSC) | 9 | FSC/F/4-27, HQ/47, FSC/D/1-1 | ArmorCompany -> Tank Company (USA) |
| 38 | SF US F BN | 3 | 47_CTCP, 47/2/1_AD, 47_REAR | ArmorCoHQ -> Ground_Aggregate |
| 39 | SF USX E COY (maintenance) | 1 | B_FLD/47 | ArmorCompany -> Tank Company (USA) |
| 40 | SF USXO E COY (ordnance) | 1 | 756/HHC | ArmorCompany -> Tank Company (USA) |
| 41 | SF UULM E COY (mil intelligence) | 1 | 856/HHC | ArmorCompany -> Tank Company (USA) |
| 42 | SF UUAC E COY (CBRN) | 1 | 369/HHC | ArmorCompany -> Tank Company (USA) |
| 43 | SF (blank funcID) E COY (HHC) | 2 | A/411/HHC, 303/HHC | ArmorCompany -> Tank Company (USA) |
| 44 | SH UCV F BN (aviation) | 2 | UNK-AVN-1/, UNK-AVN-2/ | ArmorCoHQ -> Ground_Aggregate (out of ground scope) |

Sum = 128. VERIFIED against the file. Note the R9 type-mapping fix (`TypeMapping.RealTemplates`,
`UnitTranslator.cs:124-129`) means echelon-D groups now land the real Tank Platoon (USA)
rather than Ground_Aggregate - so today's picture is "wrong branch, real template" for D and E,
"generic" for F, "single tank" for NOS/SECT/BDE.

### 4.3 R9 lean init census - 3 groups, 6 units

| group | n | names | init's own SISOEntityType | today: factory -> template landed |
|---|---|---|---|---|
| SF UCIZ D PLT | 4 | 1141/1142/1143/1222.MechPlt | 3x `11:1:225:3:4:0:0`, 1x `11:1:71:3:4:0:0` | ArmorPlatoon -> Tank Platoon (USA) |
| SF UCIZ E COY | 1 | 114.MechCoy | `11:1:225:5:4:1:0` | ArmorCompany -> Tank Company (USA) |
| SF UCIZ H BDE | 1 | 1.BdeHQ | `11:1:153:3:4:0:0` (a platoon type on a BDE unit - init data defect) | Tank -> single M1A2 |

SIDCs VERIFIED: `SFGPUCIZ--EH---` (BdeHQ), `SFGPUCIZ---E---` (MechCoy),
`SFGPUCIZ---D---` x4. This is the cheap probe fixture (sec 7).

---

## 5. The proposed mapping

Reading the columns:

- **doctrinal identity** - from the SIDC function ID (see the caveat in sec 4.2).
- **DIS enumeration** - the enumeration the created unit will carry. Per sec 2.2 this is the
  landed template's own `objectType`, not our request; where they differ the request is shown
  as "emit".
- **emit** - the objectType to publish so the intended template is hit. Verified against the
  target's `matchType` wildcards with the sec 2.3 resolver: every row below was run.
- **USA / RUS / PRC** - coverage verdict per nation: **EXACT** (a real composed template of
  the right branch and echelon), **PROXY** (real composed template, wrong branch or echelon -
  must be surfaced per R-SURFACE-PROXY), **NONE** (nothing better than a generic or empty
  fallback; authoring required).

### 5.1 Friendly (USA) rows

| # | census groups | doctrinal identity | emit | lands | composition the operator sees | verdict |
|---|---|---|---|---|---|---|
| F1 | 1 (7) | armor company | `3:11:1:225:5:2:0:0` | Tank Company (USA) | HQ Sec (2x M1A2, M3A2 CFV, M577 CP, 2x HMMWV) + 3x Tank Plt (4x M1A2 ea) | **EXACT** (unchanged) |
| F2 | 8, 6-as-platoons | mech-inf platoon | `3:11:1:225:3:4:0:0` | Mechanized Platoon (USA) IFV (Deprecated) | 4x M2A2 Bradley IFV + 3x 4-man squad | **EXACT** (branch-correct, IFV-mounted). Caveat: vendor-named "Deprecated", `gui-can-create False` |
| F3 | 6 (4) | mech-inf company | *author* `Mechanized Infantry Company (USA)` `3:11:1:225:5:4:1:0` = 1x Tank HQ Sec (or authored mech HQ) + 3x `3:11:1:225:3:4:0:0` | - | 3 IFV platoons + a real HQ | **NONE** in-chain -> AUTHOR. Interim proxy = F2 at platoon echelon (surfaced) |
| F4 | 11 (4) | dismounted infantry company | *author* `Infantry Company (USA)` `3:11:1:225:5:3:1:1` = 1x Infantry HQ Team `3:11:1:225:12:3:1:0` + 3x Infantry Platoon (USA Army) `3:11:1:225:3:3:1:0` | - | 4-man HQ + 3 full rifle platoons (3 rifle sqds + weapons sqd each) | **NONE** in-chain -> AUTHOR. **Do NOT use `aggregate-Co-Infantry-Friendly` (sec 3.6).** Interim proxy = `3:11:1:225:3:3:1:0` at platoon echelon |
| F5 | 21 (4) | FA battery (SP how) | `3:11:1:225:4:8:0:0` | Field Artillery Battery (USA) M109 | FA HQ Sec + 2x FA Plt = 8x M109A5 + 8x M992 FAASV | **EXACT** |
| F6 | 22 (2), 3 (2), 9 (1), 12 (2), 18 (2), 38 (3) - i.e. every friendly BN | battalion main / TAC CP | `3:11:1:225:14:2:1:0` | Tank Headquarters Section (USA) | 2x M1A2 (CDR/XO) + M3A2 CFV (FSO) + M577A2 CP + 2x HMMWV | **PROXY** (echelon relabels Co -> HQ Sec). The militarily correct heavy-BN main CP composition; **no Country-225 Cat-6 template exists at all** (sec 3.2). Alternative: `3:11:1:225:5:20:1:0` -> aggregate-Company-HQ-Friendly = 4 anonymous foot figures. Authoring a real `Battalion HQ (USA)` `3:11:1:225:6:*:1:*` is the fidelity answer |
| F7 | 5 (1) | brigade / division HQ | entity `1:1:1:225:3:11:0:0` | M577A2_Command_Post (single platform) | one command-post track | **NONE** (no Cat-8 template any country). Least-wrong; today it is an M1A2 tank |
| F8 | 13 (6) | cavalry squadron + troops | `3:11:1:225:14:30:0:1` | **AR Scout** `[C2simEx]` | 3x M3A2 Bradley CFV | **PROXY** (real scout vehicles, but a 3-vehicle "platoon" for a squadron/troop). Better than the armor proxy the prior doc proposed. Authoring `Cavalry Troop (USA)` from M3A2 CFV + M1A2 leaves is cheap |
| F9 | 29 (1) | fire support / observer team | `3:11:1:225:12:27:0:1` | Fire Support Team (USA) | 4x US_Army_M4 | **EXACT** |
| F10 | 35 (1) | SHORAD platoon | `3:11:1:225:3:11:0:0` | Air Defense Artillery Platoon (USA) | 4x HMMWV Avenger | **EXACT** |
| F11 | 37 (9), 39 (1), 40 (1) | forward support / maint / ord company | `3:11:1:225:3:31:0:0` | Combat Service Support Platoon (USA) | 2x M977 HEMTT cargo + 2x M978 fuel | **PROXY** (echelon downgrade Co -> Plt; no CSS company template) |
| F12 | 17 (6) | engineer company | *author* `Engineer Company (USA)` `3:11:1:225:5:10:1:0` from installed engineer leaves: `M9_Ace` `1:1:1:225:3:6:0:0`, `M58_MICLIC` `1:1:1:225:6:4:0:0`, `M1977 Common Bridge Transport` `1:1:1:225:7:19:13:0`, `Bulldozer` `1:1:1:225:27:0:0:59`, M1A2 SEP V2 Mineplow/Mineroller `1:1:1:225:1:1:15:{1,2}` | - | a real sapper/breach company | **NONE** in-chain (no Subcat-10 template any country) -> AUTHOR. Interim proxy = `3:11:1:225:5:2:0:78` -> Tank Breach Company (USA) = 2 tank plts + 1 mine-plow plt (armor with a breach capability, NOT a sapper unit; must be surfaced) |
| F13 | 18 (2) | engineer battalion | *author* (F12 + F6 pattern) | - | - | **NONE** -> AUTHOR; interim = F6 |
| F14 | 41 (1), 42 (1), 43 (2) | MI / CBRN / HHC company | `3:11:1:225:5:20:1:0` | aggregate-Company-HQ-Friendly | 4 anonymous foot figures | **PROXY** (no MI/CBRN content of any kind). Thin but exact-match and correctly typed as a company command post (Subcat 20 = CommandPost, Specific 1 = ContainsHeadquarters) |

### 5.2 Hostile rows - PRC and RUS side by side (R-HOSTILE-NATION)

Every hostile group gets both columns. `OpposingNation=RUS` is the only configuration that
works on 5.0.2 without authoring; `OpposingNation=PRC` requires the authoring column.

| # | census groups | doctrinal identity | **RUS (222)** - emit / lands / verdict | **PRC (45)** - verdict + authoring recipe from installed leaves |
|---|---|---|---|---|
| H1 | 2 (6) | armor company | `3:11:1:222:5:2:0:0` -> **Tank Company (RUS)** = Tank HQ Sec (2x T-80, BMP-2, GAZ-69 x2, ZIL-135) + 3x Tank Plt (3x T-80 ea). **EXACT** | **NONE.** Author `Tank Company (PRC)` `3:11:1:45:5:2:1:0` = 1x authored `Tank HQ Section (PRC)` (2x Type 99 + 1x WZ551) + 3x authored `Tank Platoon (PRC)` `3:11:1:45:3:2:0:0` (3-4x `Type_99_MBT` `1:1:1:45:1:9:1:0`). Leaves all installed. 2 new files + 1 = 3 files |
| H2 | 4 (1), 10 (3), 16 (1), 20 (1), 24 (3), 28 (1), 34 (2) - every hostile BN | battalion CP | `3:11:1:222:14:2:1:0` -> **Tank Headquarters Section (RUS)**. **PROXY** (echelon); no Cat-6 RUS template | **NONE.** Author `Battalion HQ (PRC)` from WZ551 APC + Type 99 |
| H3 | 7 (9) | mech-inf company | `3:11:1:222:3:4:0:0` -> **Mechanized Platoon (RUS) (Deprecated)** = 4x BMP-2 + 2x Infantry Squad (RUS). **PROXY** (echelon downgrade). Authoring `Mech Inf Company (RUS)` `3:11:1:222:5:4:1:0` from that platoon + a HQ is trivial. Do NOT use `aggregate-Co-Infantry-Hostile` (sec 3.6) | **NONE.** Author `Mech Inf Company (PRC)` from `Type 85 (YW 531H) APC` `1:1:1:45:2:2:0:0` (tracked) or `WZ551 APC` `1:1:1:45:2:6:0:0` (wheeled). **Blocker: no PRC dismounted life form exists**, so the mounted infantry must either be vehicles-only or borrow a non-PRC soldier leaf - a fidelity compromise the user must rule on (JC-3) |
| H4 | 8 (1) | mech-inf platoon | `3:11:1:222:3:4:0:0` -> Mechanized Platoon (RUS). **EXACT** | **NONE** - as H3 |
| H5 | 14 (3) | armored-cavalry / recon platoon | `3:11:1:222:3:6:0:49` -> **Recon Vehicle Platoon (RUS BMP2)** = 3x BMP-2. **EXACT** | **NONE.** Author `Recon Platoon (PRC)` from 3x WZ551 APC (no PRC recon vehicle exists) - PROXY even after authoring |
| H6 | 15 (2), 31 (1) | cav troop / radar company | RUS: `3:11:1:222:5:2:0:0` (armor proxy) or COLT Team (RUS) for the radar company. **PROXY** | **NONE** |
| H7 | 23 (3) | SP-howitzer battery | **NONE for RUS** - there is no Country-222 artillery template at all. Author `Artillery Battery (RUS)` `3:11:1:222:4:8:1:0` from installed leaves `2S19 Msta-S` `1:1:1:222:4:26:0:0` / `2S1 Gvozdika` `1:1:1:222:4:2:0:0` + `MT-LBu Battery Command Vehicle (1V14M)` `1:1:1:222:2:7:8:0`. Interim proxy = `3:11:1:225:4:8:0:0` (US M109 battery - wrong nation, surfaced) | **NONE.** Author from `PLZ-45 Howitzer` `1:1:1:45:4:20:0:0` (+ `Type 66 Howitzer` `1:1:1:45:5:4:0:0` for towed). Leaves installed |
| H8 | 25 (7), 26 (1) | mortar platoon / company | **NONE** - no mortar unit anywhere in the chain, and **no RUS mortar platform leaf either**. Author from `BM-21 2B17` (wrong weapon) or accept the US `M1064 Mortar Carrier` `1:1:1:225:2:9:4:0` / `M252_Mortar` `1:1:1:225:10:8:0:0` leaves under a RUS unit (mixed-nation composition). Interim proxy = `3:11:1:225:3:8:0:0` FA Platoon M109 (tube artillery, not a mortar - surfaced) | **NONE**, and no PRC mortar leaf. Nearest PRC indirect fire = `PHZ 89` `1:1:1:45:4:50:0:0` (122 mm SP MRL) |
| H9 | 27 (1), 28 (1) | rocket / MLRS | **NONE** as a unit; leaves exist: `BM-21 2B17` `1:1:1:222:4:13:0:0`, `TOS-1 Buratino` `1:1:1:222:4:61:0:0`. Author `MRL Battery (RUS)` `3:11:1:222:4:8:1:2` from 4-6x BM-21 | **NONE** as a unit; leaves exist: `PHL 03 (Type 03) MLRS` `1:1:1:45:4:43:0:0`, `PHZ 89` `1:1:1:45:4:50:0:0`. Author `MRL Battery (PRC)` |
| H10 | 32 (5), 33 (4) | SHORAD platoon / company | `3:11:1:222:3:11:0:0` -> **Air Defense Artillery Platoon (RUS)** = 4x SA-9 Gaskin. **EXACT** for the platoons, **PROXY** (echelon) for the companies | **NONE** as a unit; leaves exist: `HQ-9 Self Propelled TEL` `1:1:1:45:28:7:2:0`, `HQ-2 Launcher` `1:1:1:45:28:1:2:0` (both strategic SAM, not SHORAD). Author `SAM Battery (PRC)`; note the PRC SHORAD gap |
| H11 | 36 (7) | anti-armor (weapons) platoon | **NONE for RUS** as a unit; leaf exists: `GAZ-233114 Tigr Kornet EM` `1:1:1:222:6:29:4:0`. Author `Antitank Platoon (RUS)` `3:11:1:222:3:12:0:0` from 3-4x Tigr Kornet. Interim proxy = `3:11:1:225:14:12:0:0` Antitank Team (USA Javelin) - wrong nation AND echelon | **NONE**, and no PRC ATGM leaf at all |
| H12 | 30 (2) | target-acquisition section | `3:11:1:222:12:27:0:0` -> **COLT Team (RUS)** = 2x DI_Lasing_(CIS). **EXACT** at team echelon | **NONE**, no PRC life form; would have to borrow |
| H13 | 19 (1), 20 (1) | combat engineer coy/bn | **NONE** any nation (no Subcat-10 template exists). RUS has no engineer platform leaf either | **NONE**; no PRC engineer leaf |
| H14 | 44 (2) | aviation battalion | out of ground scope | out of ground scope |

### 5.3 Coverage arithmetic

Counting the 44 census groups (128 units), aviation excluded from the mapping (group 44,
2 units):

| verdict | friendly groups (units) | hostile, `OpposingNation=RUS` | hostile, `OpposingNation=PRC` |
|---|---|---|---|
| EXACT (real composed, right branch+echelon) | 5 groups / 17 units (F1,F2-as-plt,F5,F9,F10) | 4 groups / 15 units (H1,H4,H5,H10-platoons,H12) | 0 |
| PROXY (real composed, wrong branch or echelon, must be surfaced) | 5 groups / 25 units (F6-friendly-BN,F8,F11,F14,F7) | 6 groups / 27 units | 0 |
| NONE -> authoring required | 4 groups / 18 units (F3,F4,F12,F13) | 5 groups / 23 units (H7,H8,H9,H11,H13) | **13 groups / 65 units - all of them** |

INFERRED from the tables above; the unit counts are the census `n` values summed per row.
The headline: **on 5.0.2, RUS is ~60% covered by installed content and PRC is 0% covered but
~70% authorable from installed PRC platform leaves** (the gaps being dismounted infantry,
recon vehicles, ATGM, engineer, and mortars).

---

## 6. VR-Forces 5.2b affordances

The user is about to receive 5.2 (`docs/HANDOFF_2026-09-01_R9_COMPLETE.md:174-182`, "VR-FORCES
5.2b UPGRADE CHECKLIST"). What changes for this mapping:

| 5.2 item | source | bearing on this mapping |
|---|---|---|
| "**Expanded Aggregate Simulation Model Set** ... includes new units for **NATO and Russian formations** to extend VR-Forces' constructive simulation capabilities" | https://www.mak.com/learn/blog?view=article&id=491%3Avr-forces-5-2&catid=16%3Asocial-blog and the VR-Forces capabilities pages | **RUS content grows; PRC is not mentioned.** Two cautions: (a) it is the **Aggregate** SMS, which R-ENTITY-LEVEL excludes - so unless MAK also extended `EntityLevel.sms`, this does not reach our chain; (b) note that `AggregateLevel.sms` on 5.0.2 already uses **Country 260, not 222**, for its Russian units (VERIFIED: `AggregateLevel\vrfSim` contains `Tank CO RU` `3:11:1:260:5:2:1:1`, `Artillery SP BTY RU` `3:11:1:260:4:8:1:1`, `MECH BN RU solo` `3:11:1:260:6:4:1:20`), so a nation-keyed mapping must not assume 222 across model sets. **Action for 5.2b: re-run the sec 3.1 index against 5.2's `EntityLevel` and `C2simEx` directories and diff.** |
| "Ground vehicle movement redesigned with **vector-based terrain data**"; "Vehicles now utilize the MAK Behavior Tree System for obstacle reactions"; "Improved **platoon-level tasking** for small unit coordination" | same | Touches exactly the route/clamp machinery this project has been fighting (`docs/HANDOFF_2026-09-01_R9_COMPLETE.md:176-177`). It does not change type resolution, but it changes what a landed template *does*, so any movement evidence gathered on 5.0.2 must be re-taken on 5.2. |
| "Perceived vs Ground Truth Location"; "IFF Manipulation in LUA"; "Multi-Hit Actuator"; ATGM top-attack/direct-attack modes | same | Not mapping-relevant; ATGM modes matter once anti-armor units (H11) are authored. |
| HLA 4 (IEEE 1516-2025), C++20 API, HLA Object Transfer, MySQL export, Operator Text Chat, Procedural Earth 2, CDB perf, Terrain Paging Diagnostics | same | Not mapping-relevant. Migration notes at https://docs.mak.com/api/vrforces5.2/classref/vrf_migration50.html and `vrf_migration51.html` (named in the handoff). |
| No announcement of a change to **object-type matching**, the OPD, or unit/organization authoring | absence in the 5.2 announcement | **INFERRED:** the sec 2.2/2.3 resolution rules and the sec 2.5 authoring path should carry over unchanged to 5.2. Verify by re-reading `ObjectTypes.htm` in the 5.2 install. |
| No mention of C2SIM or MSDL import changes | absence | The MSDL "empty unit" mapping behaviour (sec 2.5) presumably persists. |

Nothing found on 5.2 adds **PRC** content. If PRC support is wanted, authoring is required
on both 5.0.2 and 5.2. Cross-version note: authored `.entity` files live in an SMS directory
under `C:\MAK\...\data\simulationModelSets\`; the 5.2b checklist already flags that
`SharedData` moves, so authored content needs an explicit migration step and must live in a
**project-owned SMS that includes `C2simEx`**, never as edits to vendor files (see sec 7).

---

## 7. Implementation plan (no code this pass)

### 7.1 Replace echelon-letter dispatch with a data table

Today (VERIFIED `src/VrfC2SimApp/UnitTranslator.cs:53-74`): the dispatch is a chain of `if`s
on SIDC character positions and hostility, ending in five hard-coded factories each with a
literal `Spec(...)`. The function ID is never read; the init's DIS type is read only for the
air/boat branches.

Proposed shape:

1. **A data file, not code branches.** `data/unit-type-map.json` (or `.csv`) with one row per
   `(functionId, echelon, nation-role)` -> `{ isAggregate, objectType, templateName,
   fidelity: EXACT|PROXY|AUTHORED, proxyNote }`. Rationale: the table is ~50 rows today and
   grows per nation; and it is the artifact the user reviews line by line, which is exactly
   what `docs/VRF_GROUNDWORK_PLAN.md` Phase 2.1 asked for. Code becomes a lookup with a
   documented fallback chain.
2. **Lookup key order** (most specific first):
   a. the init's own `SISOEntityType` when non-zero (see JC-1);
   b. `(functionId[5..10], echelonChar[12], nationRole)`;
   c. `(functionId, EchelonCode, nationRole)` - the C2SIM field, as a cross-check;
   d. `(echelonChar, nationRole)` - today's behaviour, as the last resort;
   e. `Ground_Aggregate` never as an intentional target - if the table has no row, log it
      loudly rather than silently emitting something that falls through.
3. **`nationRole`** resolves as: friendly -> `FriendlyNation` (default `USA`); hostile ->
   `OpposingNation` (config, `RUS` | `PRC`); neutral -> civilian branch as today.
4. **Emit a validated selector.** The table's `objectType` must be checked against the target
   template's `matchType` by the sec 2.3 resolver **at build time, as a unit test** over the
   installed SMS. That test is the whole adversarial review of this mapping: if a row no
   longer lands its named template, the build fails.
5. **Surface proxies (R-SURFACE-PROXY).** For any row with `fidelity != EXACT`, append the
   substitution to the created object's marking/name and emit it in the report stream, so
   downstream C2SIM consumers see the approximation.
6. **Keep `TypeMappingMode`** (`src/VrfC2SimApp/VrfSettings.cs:42`) as the escape hatch and
   add a third value, e.g. `GoldenParity | RealTemplates | FidelityTable`, so the new mapping
   can be A/B'd against R9 evidence without a rebuild.

### 7.2 Configuration surface

`src/VrfC2SimApp/appsettings.json` / `VrfSettings.cs`, alongside the existing string options:

```
"Vrf": {
  "TypeMappingMode": "FidelityTable",
  "FriendlyNation": "USA",       // DIS 225
  "OpposingNation": "RUS",       // "RUS" (DIS 222) | "PRC" (DIS 45)
  "TypeMapFile": "data/unit-type-map.json",
  "SurfaceProxySubstitutions": true
}
```

`OpposingNation` **default is a user call (JC-2)**. My recommendation: default `RUS`, because
it is the only value that works on stock 5.0.2 content (sec 5.3), and make `PRC` refuse to
start unless the authored PRC SMS is present - a loud failure instead of the silent empty-unit
trap of sec 3.5.

### 7.3 What the C2SIM message could carry instead of a config setting

VERIFIED from the schema (sec 4.1), in order of preference:

1. **`Unit/SISOEntityType/DISCountry`** - per-unit, mandatory in the schema, already parsed by
   the port. If the init sets it, honour it and log that config was overridden. This is the
   standard's intended channel and the R9 lean init already uses it.
2. **`EntityDescriptor/AffiliatedWith` -> `AbstractOrganization/CountryCode`** (ISO 3166-1
   alpha-3) - the ontology's explicit organization-affiliation channel. Needs a small
   ISO-3166 -> DIS-country table (USA->225, RUS->222, CHN->45).
3. **SIDC positions 13-14** (2525C country code) - present in every SIDC, `-` in COA-STP1;
   the position claim is UNVERIFIED against the primary standard (sec 4.1) so verify before
   relying on it.
4. `ForceSide` carries **no** nationality (VERIFIED) - do not look for it there.

So the config setting is the *default*, and any of channels 1-3 present in the message
overrides it. That satisfies both customers without a rebuild and without a private extension.

### 7.4 Authoring plan (for the NONE rows)

Deliverable per authored unit: one `.entity` XML file, structured exactly like
`Tank Company (USA).entity` (sec 2.5), with:
- an `objectType` that is unique and semantically correct per sec 2.4 (Category = echelon,
  Subcategory = branch, Specific = 1 iff it contains an HQ, Country = the nation);
- `matchType` equal to the `objectType` (exact) so nothing else can steal it and it cannot
  steal from anything else - **and the sec 2.3 resolver run over the whole SMS to prove it**;
- `<subordinates>` referencing only leaves verified to resolve (the sec 3.6 trap);
- `platform="@(platforms-dir)/HigherAggregate.ope"` for company/battalion units,
  `Aggregate.ope`/the platoon pattern for platoon units, and the matching movement sysdef
  (`ground-higherUnit-disaggregated-movement.sysdef` for units-of-units,
  `ground-disaggregated-movement.sysdef` for units-of-entities,
  `human-disaggregated-movement.sysdef` for dismounted) per `docs/VRF_GROUND_TRUTH.md` 0.1.3.a;
- `echelon-level`, `gui-label`, `gui-deployable-countries`, and a unique `gui-unique-id`.

**Where they live (a hard requirement).** Never edit files under `C:\MAK\...\EntityLevel\` -
it is `read-only True` in its own `.sms` and vendor-owned. Create a project SMS, e.g.
`C2SIM_Fidelity.sms`, whose `(include ...)` points at `C2simEx.sms`, holding only our
authored `.entity` files, and repoint the fixture `.scnx`'s `(Simulation-Model-Set-Files ...)`
at it. That keeps the vendor tree pristine and makes the 5.2 migration a copy.

Ordered authoring queue (highest unit-count payoff first):

| order | file | covers | leaves (all installed) |
|---|---|---|---|
| 1 | `Infantry Company (USA)` | F4 (4 units) | Infantry HQ Team + Infantry Platoon (USA Army) |
| 2 | `Mechanized Infantry Company (USA)` | F3 (4) | Mechanized Platoon (USA) IFV + Tank HQ Sec |
| 3 | `Battalion HQ (USA)` Cat-6 | F6 (12 friendly BN) | M577A2 CP, M1068, M1A2, HMMWV |
| 4 | `Engineer Company (USA)` | F12/F13 (8) | M9 ACE, M58 MICLIC, M1977 CBT, Bulldozer, Mineplow |
| 5 | RUS: `Artillery Battery`, `MRL Battery`, `Antitank Platoon`, `Mech Inf Company`, `Battalion HQ` | H7,H9,H11,H3,H2 (~20) | 2S19/2S1, BM-21, Tigr Kornet, BMP-2, T-80 |
| 6 | PRC set: `Tank Platoon/Company`, `Tank HQ Section`, `Mech Inf Platoon/Company`, `Artillery Battery`, `MRL Battery`, `SAM Battery`, `Battalion HQ` | all PRC hostile rows | Type 99, T-69, Type 85, WZ551, PLZ-45, PHZ 89, PHL 03, Type 66, HQ-9, HQ-2 |

### 7.5 Verification

1. **Offline, cheap, and first: the resolver as a test.** Assert every table row lands its
   named template against the installed SMS. Catches the sec 3.6 class of bug forever.
2. **Composition assertion.** For each row, assert the transitive subordinate expansion
   contains no `Ground_Aggregate` and no zero-subordinate abstract - the sec 3.5/3.6 traps.
3. **Live, on the R9 lean fixture (6 units, the cheapest probe).** Push
   `data/R9_Mojave_Lean_Initialization_NoComments.xml` at the `TropicTortoise_FFRTC` fixture
   and read **`C:\MAK\vrforces5.0.2\bin64\vrfSim.log` for the creation lines naming the
   resolved template**. Per memory `lessons-vendor-diagnostics-first`, read the vendor log and
   its verbosity knob before probing anything. This is also the outstanding cross-check that
   `docs/VRF_GROUND_TRUTH.md` 0.1.8 item 1 has been asking for since 2026-07-15 - **do it
   before trusting any static claim in this document**, mine included.
4. **Then COA-STP1 at scale**, comparing per-unit landed templates against the table.

### 7.6 Risks

- **R1 - the live-vs-static falsifier is still open.** Everything here is static best-match
  arithmetic. One live formation query in 2026-07-15 disagreed. If the live matcher differs,
  the emit column changes wholesale. Mitigated by making step 7.5.3 the gate.
- **R2 - modelling worlds cannot mix.** The fixture's SMS is `C2simEx.sms` ->
  `EntityLevel.sms` -> `base.sms` (VERIFIED sec 1). **Every template proposed in sec 5.1/5.2
  lives in that chain** (checked row by row against the sec 3.1 index) except the authored
  ones, which will live in a new SMS that *includes* `C2simEx` - an extension of the same
  entity-level chain, not a mix. No proposal reaches into `AggregateLevel.sms`. R-ENTITY-LEVEL
  is not violated by any row.
- **R3 - function-ID decodes are second-hand.** UCD/UCAA/UCFHE etc. come from symbol.army via
  `VRF_GROUND_TRUTH` 0.1.7, not the primary MIL-STD-2525C. A wrong decode silently maps a
  branch wrong. Cheap fix: check the decode table against the primary standard once.
- **R4 - vendor data defects exist and will bite.** Three found this pass:
  `aggregate-Co-Infantry-Friendly` (sec 3.6), `Infantry Headquarters Section (RUS Army)`
  tagged `gui-deployable-countries "US"`, `PHL 03 (Type 03) MLRS` (a Chinese system) tagged
  `"JO" "RO" "SG" "AE" "US"`. Do not trust `gui-*` metadata; trust `objectType` + expanded
  subordinates.
- **R5 - `Deprecated` and `gui-can-create False` templates.** F2/H3 rely on templates the
  vendor marks deprecated and hides from the palette. They resolve today; MAK could remove
  them in 5.2. Mitigation: the 7.5.1 test will fail loudly on 5.2 if they vanish, and the
  authored replacements (7.4 order 2 and 5) remove the dependency.
- **R6 - authored content and version migration.** Authored `.entity` files are outside the
  repo (they live in the MAK tree). They must be version-controlled in `data/` and deployed by
  a script, or the next MAK install silently loses the whole mapping.

---

## 8. Corrections this pass makes to the existing repo record

1. **`aggregate-Co-Infantry-Friendly` is not a usable infantry company** - its three infantry
   platoons resolve to `Ground_Aggregate` (sec 3.6). `docs/TYPE_MAPPING_TABLE.md` sec 2.3 and
   `docs/TYPE_GAP_ADJUDICATION.md` GAP 2 recommend it; that recommendation is withdrawn.
   `aggregate-Co-Infantry-Hostile` is partly defective the same way (its HQ falls generic).
2. **"ZERO Chinese ground-combat platforms" is FALSIFIED** (sec 3.4). Ten PRC ground platforms
   are installed, including two MBTs, two APCs, and three artillery/MRL systems. The memory
   note `c2sim-vrf-port` and the `TYPE_GAP_ADJUDICATION.md` Q1 answer both need this
   correction; the "zero Chinese aggregates" half stands.
3. **Emitting a Country-45 (or any unstocked-country) aggregate type is worse than the generic
   fallback**, because the Country-0 abstracts have zero subordinates (sec 3.5). Any
   nation-keyed mapping must be gated on a coverage table.
4. **There is no Country-225 or Country-222 template at Category 6/8/9** (battalion, brigade,
   division) anywhere in the chain (sec 3.2/3.3). `TYPE_GAP_ADJUDICATION.md` DECISION ITEM 4
   framed the 26 BN units as an A-vs-B code choice; the real answer is that the battalion
   echelon does not exist in this modelling world and must be authored (F6/H2).
5. **`Specific` means ContainsHeadquarters** (vendor appendix, sec 2.4) - so DECISION ITEM 4's
   "Option A: emit Specific=1" is not a hack, it is the semantically correct value for a
   command post. It just lands a thin template.
6. **There are no "organization files"** in this SMS (sec 2.5); composition is the
   `<subordinates>` list inside the `.entity` file, and the "Organization Manager" in the 4.10
   Developer's Guide is a runtime hierarchy service, not a file format.

---

## 9. The three judgement calls I want the user to rule on

**JC-1 - Is the init's own `SISOEntityType` authoritative?**
The R9 lean init declares real DIS types (`3:11:1:225:3:4:0:0` = MechanizedInfantry platoon)
that resolve to an exact, branch-correct template, and the port throws them away in favour of
an armor default (sec 4.2). COA-STP1 declares all zeros. Proposal: **when non-zero, the init's
`SISOEntityType` wins over the SIDC-derived row, with the coverage table as a backstop** (so a
declared `3:11:1:71:3:4:0:0` does not silently create an empty unit). The alternative -
"always use our table, treat the init's DIS as advisory" - is more predictable but discards
the message's own primary source, which R-FIDELITY argues against. Your call, because it
decides whose data wins when a coalition partner sends a type we did not anticipate.

**JC-2 - What is the default `OpposingNation`, and what happens when its coverage is empty?**
On stock 5.0.2, `RUS` is ~60% covered by installed content and `PRC` is 0% (sec 5.3, sec 3.4).
Proposal: **default `RUS`; `PRC` refuses to start until the authored PRC SMS is present**
(loud failure over the silent empty-unit trap). The alternative - let `PRC` fall back to RUS
or USA templates per row - ships something for every unit but puts T-80s and M1A2s in a PLA
order of battle unless every row is read. Which failure mode do you want?

**JC-3 - PRC dismounted infantry has no leaf. What do we do?**
There is no Country-45 life form anywhere in the chain (sec 3.4), so an authored PRC
mechanized-infantry company can have its APCs but not its riflemen. Options: (a) vehicles-only
PRC mech units (honest, visibly incomplete); (b) borrow a non-PRC soldier leaf (e.g. the
generic life form, or `DI_AK-47` `1:3:1:222:1:205:1:0`) and surface the substitution per
R-SURFACE-PROXY; (c) author a PRC DI-Guy character, which needs visual content we do not have.
I recommend (b) with the substitution surfaced, but it puts a Russian-typed soldier inside a
Chinese unit on the wire, which some customers will object to - so it is your call, not mine.

---

## 10. Primary sources read this pass

Repo: `docs/START_HERE.md` (nav), `docs/TYPE_GAP_ADJUDICATION.md` (whole),
`docs/TYPE_MAPPING_TABLE.md` (whole), `docs/VRF_GROUND_TRUTH.md` sec 0.1.0-0.1.9,
`docs/SCENARIO_SETUP_GUIDE.md`, `docs/HANDOFF_2026-09-01_R9_COMPLETE.md:174-182`,
`src/VrfC2SimApp/UnitTranslator.cs` (whole), `src/VrfC2SimApp/VrfSettings.cs`,
`data/COA-STP1_Initialization.xml` (parsed, 128 units), `data/R9_Mojave_Lean_Initialization.xml`
(parsed, 6 units), `runs/20260902T143638Z_run/run-manifest.json`,
`../../Library/CS/C2SIMSDK/C2SIMSDK/schemas/C2SIM_SMX_LOX_CWIX2024.xsd` (lines 240-247,
1342-1356, 1666-1670, 2038-2057, 3560-3576, 4650-4670, 4960-4973).

MAK install (read-only): `C:\MAK\vrforces5.0.2\userData\scenarios\TropicTortoise_FFRTC.scnx`
(`.scn` part); `data\simulationModelSets\{C2simEx,EntityLevel,base}.sms`;
all `.entity` files under `data\simulationModelSets\{C2simEx,EntityLevel,base}\vrfSim\`
(indexed; 115 ground unit templates, 35 Country-45 objects, 69 Country-222 ground platforms
enumerated); `data\simulationModelSets\AggregateLevel\vrfSim\` (175 simObjects indexed, for
the 5.2 note only); `doc\help\Content\SimulationModels\ObjectParameterDatabase\ObjectTypes.htm`
and `UnitsConfigure.htm`; `doc\help\Content\SimulationModels\EntityLevelScenariosUnits\*.htm`;
`doc\help\Content\Appendixes\Parameters\vrf_aggregate{Kind,Subcategory,Specific}.htm`.

Web: https://docs.mak.com/api/vrforces4.10/classref/vrf_the_organization_manager.html ;
https://docs.mak.com/api/vrforces4.10/classref/entitymodels_aggregates.html ;
https://www.mak.com/learn/blog?view=article&id=491%3Avr-forces-5-2&catid=16%3Asocial-blog ;
https://www.mak.com/vr-forces/capabilities ;
https://www.mixr.dev/assets/pages/interop/siso-ref-010-v28.pdf (SISO-REF-010-2020 v28) ;
https://cdn.ymaws.com/www.sisostandards.org/resource/resmgr/reference_documents_/siso-ref-010.1-2019_operatio.pdf ;
https://nasaworldwind.github.io/WorldWindJava/gov/nasa/worldwind/symbology/milstd2525/SymbolCode.html ;
https://github.com/open-dis/dis-enumerations .

---

## 11. IMPLEMENTATION (2026-09-02, offline executor) - what landed

Sec 7.1, 7.2 and 7.5 items 1-2 are implemented. Sec 7.5 item 3 (the LIVE gate) is
**OUTSTANDING** and queued for the VR-Forces holder - see `docs/NEXT_TYPE_MAPPING_LIVE_GATE.md`.
Nothing under `C:\MAK` was written; no C++ change was needed.

### 11.1 Files

| file | what |
|---|---|
| `data/unit-type-map.json` | 123 rows, one per `(functionId, echelon, nationRole, nation)`, each carrying `isAggregate`, the 8-field `objectType`, `templateName`, `fidelity` and `proxyNote`. Populated from sec 5.1 (USA, 49 rows), 5.2 RUS (37) and 5.2 PRC (37 - 36 `AUTHORED_PENDING` plus one catch-all, so the coverage gap is IN the table, not hidden). Overall: 20 EXACT, 67 PROXY, 36 AUTHORED_PENDING. |
| `src/VrfC2SimApp/UnitTypeMap.cs` | the table model, its loader, the sec-7.1 key order, and the JC-2 refuse-to-start check. Pure - no bridge, no MAK. |
| `src/VrfC2SimApp/ObjectTypeResolver.cs` | offline index of the installed SMS chain (follows the `(include ...)` chain) + the vendor's best-match rule + transitive subordinate expansion. Test-only; nothing in the live path calls it. |
| `src/VrfC2SimApp/TypeMapSelfTest.cs` | `--typemap-selftest`, four parts (table / resolver / composition / lookup). |
| `src/VrfC2SimApp/UnitTranslator.cs` | `TypeMapping.FidelityTable` + the `FromTable`/`FromRow` path. The air, sea and neutral branches keep their parity behaviour; the table replaces the GROUND echelon-letter if-chain only. |
| `src/VrfC2SimApp/VrfSettings.cs`, `appsettings.json` | `TypeMappingMode` gains `FidelityTable`; new `FriendlyNation`, `OpposingNation`, `TypeMapFile`, `SurfaceProxySubstitutions`, `ProxyMarkingTag`. |
| `src/VrfC2SimApp/InitModels.cs`, `InitParser.cs` | `InitUnit.EchelonCode` (lookup key c). |
| `src/VrfC2SimApp/ReportBuilder.cs` | `BuildTypeSubstitutionReport` - ObservationReport / NameObservation. |
| `src/VrfC2SimApp/VrfC2SimService.cs` | table load + pre-flight refusal, the per-unit dispatch, the loud skip, marking annotation and report emission. |

**`TypeMappingMode` default is still `RealTemplates`.** `GoldenParity` and `RealTemplates`
are byte-for-byte unchanged (asserted by two self-test rows), so `FidelityTable` is A/B-able
against the R9 evidence by config alone, with no rebuild - as sec 7.1 item 6 asked.

### 11.2 Judgement calls, applied as PROVISIONAL (supervisor, 2026-09-02 - the user may overturn)

- **JC-1 YES.** A non-zero init `SISOEntityType` WINS over the SIDC-derived row, with this
  table as the coverage backstop: the declared 8-field type is honoured only if some row
  already proves it lands a real template. On the R9 lean init that means
  `11:1:225:3:4:0:0` now creates the branch-correct `Mechanized Platoon (USA) IFV` instead of
  the armor default (the exact defect sec 4.2 identified), while the declared Country-153 and
  Country-71 types - which resolve to EMPTY abstracts - fall back to the SIDC row with a WARN.
- **JC-2 default `OpposingNation=RUS`; `PRC` REFUSES TO START.** `CheckOpposingNationSupported`
  runs before `VrfBridge.Start` and logs a `LogCritical` naming the missing content (zero
  Country-45 unit templates in the chain), then stops the host. It does not silently create
  empty units.
- **JC-3 DEFERRED.** No PRC content was authored this pass. The PRC dismount blocker is
  recorded in the `proxyNote` of every affected row.

### 11.3 Verification actually performed (sec 7.5 items 1-2)

`--typemap-selftest`: **780 checks, 0 failures**, against the real
`C:\MAK\vrforces5.0.2` install (1495 simObjects indexed from `C2simEx -> EntityLevel -> base`, of
which **115 are ground unit templates with the `3:11:1` prefix - exactly the count sec 3.1
reports**, an independent reproduction of that index by a second implementation).

1. The resolver is re-validated **6/6 against `docs/VRF_GROUND_TRUTH.md` sec 0.1.5 BEFORE it is
   trusted for anything else** - a broken resolver cannot silently pass the rows.
2. **Every row's `objectType` lands the template the row names.** A vendor duplicate is
   accepted anywhere in the tied set (see 11.4).
3. **Every aggregate row's transitive expansion** contains no `Ground_Aggregate`, no unmatched
   subordinate and no zero-subordinate abstract.
4. Lookup key order (a)-(e), the JC-1 backstop, the sec 7.3 `DISCountry` override, the JC-2
   refusal, and the two legacy modes' invariance.
5. **The tests were proven able to fail.** Two rows were deliberately broken and the run was
   recorded before restoring them:
   `[FAIL] F-UCA-E: 3:11:1:225:5:2:0:0 -> Tank Platoon (USA)  <- landed Tank Company (USA)` and
   `[FAIL] F-UCI-D: aggregate-Co-Infantry-Friendly composition is real  <- d1 3:11:1:225:3:3:0:0 -> Infantry Platoon (zero-subordinate abstract)`,
   `SELF-TEST FAILED (3 of 780 checks)`, exit 1.
   If `C:\MAK` is absent the resolver and composition parts SKIP with a loud banner that says
   the rows are UNVERIFIED - they never silently pass.

Offline dry-run of the table over the fixtures (lookup only; the live gate is still owed):

| init | opposing | units | EXACT | PROXY | AUTHORED_PENDING | key used |
|---|---|---|---|---|---|---|
| `COA-STP1_Initialization.xml` | RUS | 128 | 28 | 100 | 0 | (b) for all 128 |
| `COA-STP1_Initialization.xml` | PRC | 128 | 13 | 48 | **67** | (b) for all 128 |
| `R9_Mojave_Lean_Initialization.xml` | RUS | 6 | 4 | 2 | 0 | (b) for all 6 |

Every function ID and echelon in both fixtures has an EXPLICIT row: the echelon-only and
catch-all fallbacks are never reached, and nothing lands `Ground_Aggregate`. The 67 PRC rows
are exactly why `OpposingNation=PRC` refuses to start.

### 11.4 Findings that differ from, or add to, this document's own claims

1. **sec 3.6 - the landing is different, the defect is the same.** The survey says
   `aggregate-Co-Infantry-Friendly`'s three `3:11:1:225:3:3:0:0` subordinates "fall to
   `Ground_Aggregate`". They do not: they best-match the Country-0 **zero-subordinate abstract
   `Infantry Platoon`** (`3:11:1:0:3:3:0:0`, `gui-can-create False`, nsub=0) - the sec-3.5 trap,
   not the sec-3.2 generic. The conclusion (unusable; do not use it as the US infantry company)
   is unchanged and the composition test catches both.
2. **A vendor DUPLICATE at `3:11:1:225:12:27:0:1`.** Two files -
   `EntityLevel\vrfSim\Fire Support Team (USA).entity` and
   `EntityLevel\vrfSim\Fire_Support_Team.entity` - carry the SAME `objectType`, the SAME exact
   `matchType` and the SAME four `1:3:1:225:1:41:1:0` subordinates. Which one VR-Forces picks is
   undetermined but they are functionally identical, so the resolver test accepts the named
   template anywhere in the tied set. No other row in the table ties.
3. **`Ground_Aggregate`'s gui-label is "Ground Unit".** The survey names templates by FILE base
   name; the code does the same, deliberately, because the label is not unique and not stable.
4. **Four `matchType` fields on 5.0.2 use RANGES, not just wildcards** (`12-22`, `86-87`,
   `32-33`, and one 7-field `1:2:-1:90:11-12:-1:-1`). All four are civil AIRCRAFT; no ground unit
   uses a range. The resolver handles ranges and ignores malformed (non-8-field) matchTypes.
5. **A hard cap on the proxy marking.** The back end resolves marking-text references through a
   35-byte blob (`include\vrfutil\rwUUID.h:412`), the same mechanism behind the 2026-09-02
   route-uuid failure. A proxy tag is therefore appended to the created object's name only when
   the result stays within 34 characters; when it does not, the name is left alone and the
   substitution is still reported and logged. **This is the residual risk the live gate must
   close** - see the falsifiers in `docs/NEXT_TYPE_MAPPING_LIVE_GATE.md`.
6. **sec 4.3 has one transcription slip.** `1.BdeHQ`'s declared type in
   `data/R9_Mojave_Lean_Initialization_NoComments.xml` is `11:1:153:**5**:4:0:0` (Category 5 =
   Company), not `11:1:153:3:4:0:0`. It changes nothing - Country 153 is uncovered either way, so
   the JC-1 backstop fires - but the self-test now uses the verbatim fixture value.
7. **`EchelonCode` has no `Specified` flag** in the generated schema types, so an absent element
   deserializes as the enum's first member (`AG`). Harmless for key (c) - no row uses `AG` - but
   it must not be promoted to a primary key without a raw-XML presence guard.

### 11.5 What is still OUTSTANDING

- **sec 7.5 item 3 - the live gate.** Everything above is static best-match arithmetic plus an
  offline dry-run. `docs/VRF_GROUND_TRUTH.md` 0.1.8 item 1 (2026-07-15, `114.MechCoy`'s
  lowercase formation list) is still the one unreconciled falsifier for the whole static method.
  Read a real `bin64\vrfSim.log` creation line before trusting any row - the brief is
  `docs/NEXT_TYPE_MAPPING_LIVE_GATE.md`.
- **sec 7.4 authoring** (orders 1-6). Nothing authored this pass; the queue is unchanged.
- **R3 - the function-ID decodes are still second-hand** (symbol.army via `VRF_GROUND_TRUTH`
  0.1.7, not the primary MIL-STD-2525C). A wrong decode now maps a whole table row's branch
  wrong, which is a bigger blast radius than before. Cheap fix, still not done.
