# TYPE GAP ADJUDICATION (Phase 2.1 input; resume item (c))

Purpose: the three CONTENT gaps from VRF_GROUND_TRUTH.md 0.0-#4 and 0.1.7 that need a
USER decision (military-semantics calls, not code calls), plus the one near-miss that is
a code decision. For each: the exact COA-STP1 population affected, what it mis-maps to
today, 2-4 REAL installed candidate templates (every path + DIS enum + composition read
directly from the .entity file this pass, not copied from the catalog), and an executor
RECOMMENDATION awaiting your adjudication.

Scope note (load-bearing): our loaded chain is C2simEx -> EntityLevel -> base (all
entity-level). Candidates tagged IN-CHAIN resolve today. Candidates tagged OUT-OF-CHAIN
exist on disk under AggregateLevel.sms, which our scenarios do NOT load; using them means
either adding that .sms to the include chain OR switching to aggregate-level modeling.
Aggregate-level units CAN NEVER DISAGGREGATE and therefore cannot model combat/attrition
(VRF_GROUND_TRUTH 0.2 sec 2), and their movement is modulated by terrain/posture (posture
transitions can take "several hours" of sim time). That semantic cost is why they are a
user call, not a default.

Path root (all rows): `C:\MAK\vrforces5.0.2\data\simulationModelSets\`
DIS objectType field order: superType:Kind:Domain:Country:Category:Subcat:Specific:Extra
Movement column (VRF_GROUND_TRUTH 0.0-#2 - the controller split may drive live behavior):
  LF = ground-disaggregated.sysdef -> lead-follow-in-formation-controller (platoon-class)
  HU = ground-higherUnit-disaggregated.sysdef -> move-along-controller (company/bn-class)
  HM = human-disaggregated.sysdef -> move-along-controller (dismounted)

---

## GAP 1 - No engineer aggregate in the loaded chain

COA-STP1 need: UCE (Engineer) E(COY)=6, F(BN)=2; UCEC (Combat Engineer) E(COY)=1, F(BN)=1.
Total 10 units (7 companies, 3 battalions). Today (echelon-only dispatch): the 7 COY ->
`Tank Company (USA)` (armor, breach-less); the 3 BN -> generic `Ground_Aggregate` (4
anonymous Cat-4 vehicles). No engineer/sapper/bridging semantics reach VRF at all.

| candidate | path (under root) | objectType / matchType | ech | mv | subordinate composition (from file) | tradeoff for engineer role |
|-----------|-------------------|------------------------|-----|----|-------------------------------------|----------------------------|
| Tank Breach Company (USA) IN-CHAIN | `EntityLevel\vrfSim\Tank Breach Company (USA).entity` | `3:11:1:225:5:2:0:78` / exact `3:11:1:225:5:2:0:78` | Co | HU | 1x Tank HQ Section (3:11:1:225:14:2:1:0); 2x Tank Platoon USA (3:11:1:225:3:2:0:0); 1x Tank Platoon USA Mine Plows (3:11:1:225:3:2:0:78) | It is an ARMOR company with one mine-plow platoon - a breach capability, not a sapper/bridging/obstacle unit. gui-categories does list "Engineering". Closest in-chain engineer flavor; still 75% tanks. |
| M1977 Common Bridge Transport (CBT) IN-CHAIN | `EntityLevel\vrfSim\M1977 Common Bridge Transport (CBT).entity` | Kind=1 PLATFORM (not a unit aggregate) | - | - | single vehicle (bridge layer) | Real engineer EQUIPMENT but only a lone entity, no unit structure; would map an engineer BN/COY to one bridge truck. Mention only. |
| BN, Engineer OUT-OF-CHAIN (aggregate-level) | `AggregateLevel\vrfSim\BN, Engineer.entity` | `3:11:1:-1:6:10:1:0` / exact | BN | agg-lvl | pseudo-aggregate; gui-can-create False | The ONLY thing named an engineer unit on disk. Aggregate-level: cannot disaggregate/fight; not in our chain. Cat 6 = battalion echelon, Subcat 10 = engineer branch (vs armor Subcat 2). |

RECOMMENDATION (executor; awaiting user adjudication): for the 7 engineer COMPANIES use
`Tank Breach Company (USA)` IN-CHAIN as the least-wrong proxy (real breach platoon, ground
movement works today), and flag it as a proxy in the mapping table; for the 3 engineer
BATTALIONS there is no in-chain aggregate - either accept the same Breach Company proxy or
author an entity-level engineer template. Do NOT pull AggregateLevel in for one branch.
USER CALL: accept armor-breach proxy vs author a real sapper/bridging entity-level unit.

---

## GAP 2 - No composed USA mechanized-infantry company

COA-STP1 need: UCIZ (Mechanized Infantry) E(COY)=13 (9 hostile SH, 4 friendly SF); also
one UCIZ D mech platoon. Today all 13 COY -> `Tank Company (USA)` (wrong BRANCH: pure
armor, no IFV-mounted infantry). Verified this pass: `Mechanized Company` and `Mechanized
Company IFV` are ABSTRACT (Country 0, gui-can-create False, ZERO subordinates) - they
resolve to nothing useful. No composed USA mech-inf company exists in the chain.

| candidate | path (under root) | objectType / matchType | ech | mv | subordinate composition (from file) | tradeoff for mech-inf company role |
|-----------|-------------------|------------------------|-----|----|-------------------------------------|-----------------------------------|
| aggregate-Co-Infantry-Friendly IN-CHAIN | `EntityLevel\vrfSim\aggregate-Co-Infantry-Friendly.entity` | `3:11:1:225:5:3:1:0` / exact | Co | HU | 1x Inf HQ Section (3:11:1:225:14:3:1:127); 3x Infantry Platoon (3:11:1:225:3:3:0:0) | Real COMPOSED company, correct echelon/movement, but DISMOUNTED infantry - no Bradley/Stryker mounts. Hostile mirror `aggregate-Co-Infantry-Hostile` `3:11:1:222:5:3:1:0`. |
| Mechanized Platoon (USA) IFV (Deprecated) IN-CHAIN | `EntityLevel\vrfSim\Mechanized Platoon (USA) IFV (Deprecated).entity` | `3:11:1:225:3:4:0:0` / exact | PLT | LF | 4x M2A2 Bradley IFV (1:1:1:225:2:1:1:0); 3x Mechanized Squad (3:11:1:225:13:4:0:0) | The only IFV-MOUNTED composed mech-inf, but PLATOON not company, gui-can-create False, vendor-DEPRECATED. Would need 3x assembled under an HQ to fake a company. |
| Stryker Rifle Platoon (USA Army) IN-CHAIN | `EntityLevel\vrfSim\Stryker Rifle Platoon (USA Army).entity` | `3:11:1:225:3:4:0:127` / exact | PLT | HU | 1x Stryker Rifle HQ Sec; 3x rifle squad (3:11:1:225:13:4:0:127); 1x weapons squad (...:0:59); 4x Stryker ICV platforms | Real mounted (Stryker, not Bradley) infantry, but PLATOON not company and gui-can-create False. No Stryker Rifle COMPANY exists on disk. |
| Mech Inf CO US OUT-OF-CHAIN (aggregate-level) | `AggregateLevel\vrfSim\Mech Inf CO US.entity` | `3:11:1:225:5:4:1:1` / exact | CO | agg-lvl | full mech-inf company (aggregate-level) | The RIGHT unit semantically (Cat5 company / Sub4 mech), gui-can-create True - but aggregate-level: cannot disaggregate/fight, not in our chain. RUS mirror `Mech Inf CO RU`, entity-level pseudo-aggregate variant `MECH CO US PA`. |

RECOMMENDATION (executor; awaiting user adjudication): if dismounted-vs-mounted fidelity is
acceptable, use `aggregate-Co-Infantry-Friendly` / `-Hostile` IN-CHAIN (real composed
company, correct echelon/movement, works today) as the interim mech-inf mapping and record
the "dismounted, not IFV-mounted" caveat. If IFV mounts are required, no in-chain company
exists - the honest options are (A) author an entity-level mech-inf company from the
Deprecated IFV platoon / Mechanized Squad parts, or (B) accept aggregate-level `Mech Inf
CO US` and its no-disaggregation cost. USER CALL: dismounted proxy now vs author IFV company
vs switch this branch to aggregate-level.

---

## GAP 3 - No mortar and no rocket aggregate

COA-STP1 need: UCFM (Mortar) D(PLT)=7, E(COY)=1 -> 8 units; UCFR (Rocket/MLRS) E(COY)=1,
F(BN)=1 -> 2 units. Total 10. Today: UCFM D(7)->`Ground_Aggregate` (generic), UCFM E(1) and
UCFR E(1) -> `Tank Company (USA)` (armor), UCFR F(1)->`Ground_Aggregate`. Verified this
pass: NO aggregate template in the chain is a mortar or a rocket/MLRS unit; mortar and MLRS
exist only as lone platform ENTITIES. Nearest in-chain aggregate is tube field artillery.

| candidate | path (under root) | objectType / matchType | ech | mv | subordinate composition (from file) | tradeoff for mortar/rocket role |
|-----------|-------------------|------------------------|-----|----|-------------------------------------|---------------------------------|
| Field Artillery Platoon (USA) M109 IN-CHAIN | `EntityLevel\vrfSim\Field Artillery Platoon (USA) M109.entity` | `3:11:1:225:3:8:0:0` / `3:11:1:225:3:8:-1:-1` | PLT | LF | HQ + FDC platforms; 4x FA Section M109 (3:11:1:225:14:8:0:0), each an M109A5 SP howitzer | TUBE SP artillery, not a mortar. Right echelon (PLT) and indirect-fire role for UCFM-D; wrong weapon class (155mm SP vs 81/120mm mortar). |
| Field Artillery Platoon (USA) M777 IN-CHAIN | `EntityLevel\vrfSim\Field Artillery Platoon (USA) M777.entity` | `3:11:1:225:3:7:0:0` / `3:11:1:225:3:7:-1:-1` | PLT | LF | HQ + FDC; 4x FA Section M777 (3:11:1:225:14:7:0:0) | Towed 155mm tube - same indirect-fire proxy logic as the M109; still not a mortar. |
| Field Artillery Battery (USA) M109 IN-CHAIN | `EntityLevel\vrfSim\Field Artillery Battery (USA) M109.entity` | `3:11:1:225:4:8:0:0` / `3:11:1:225:4:8:-1:-1` | BTY | HU | 1x FA HQ Section (3:11:1:225:14:7:1:0); 2x FA Platoon M109 (3:11:1:225:3:8:0:0) | Battery echelon for the UCFM/UCFR E(COY) and F(BN) rows; tube proxy, HU move (company-class). |
| M1064 Mortar Carrier / M252 Mortar IN-CHAIN | `EntityLevel\vrfSim\M1064 Mortar Carrier.entity` (`1:1:1:225:2:9:4:0`); `M252_Mortar.entity` (`1:1:1:225:10:8:0:0`) | Kind=1 PLATFORMS | - | - | single mortar system each | The only REAL mortar systems on disk, but lone entities - a mortar platoon would become one carrier, no unit structure. |
| M270 MLRS / M142 HIMARS IN-CHAIN | `EntityLevel\vrfSim\M270 MLRS.entity` (`1:1:1:225:4:1:0:1`); `M142 HIMARS.entity` (`1:1:1:225:4:24:0:0`) | Kind=1 PLATFORMS | - | - | single launcher each | The only REAL rocket/MLRS systems on disk for UCFR; lone entities, no unit aggregate. |
| Mortar PLT US / MLRS BTY US OUT-OF-CHAIN (aggregate-level) | `AggregateLevel\vrfSim\Mortar PLT US.entity` (`3:11:1:225:3:7:0:1`); `Mortar SP PLT US.entity` (`3:11:1:225:3:8:0:1`); `MLRS BTY US.entity` (`3:11:1:225:4:8:1:2`) | aggregate-level units | PLT/BTY | agg-lvl | composed mortar / MLRS units | The RIGHT unit types by name, gui-can-create True - but aggregate-level (no disaggregate/combat) and not in our chain. |

RECOMMENDATION (executor; awaiting user adjudication): map UCFM mortar PLATOONS to
`Field Artillery Platoon (USA) M109` and mortar/rocket COMPANY/BATTALION rows to
`Field Artillery Battery (USA) M109` IN-CHAIN as indirect-fire proxies (real composed
fire units, correct echelon and movement today), tagging "tube-artillery proxy, not a
mortar/MLRS" in the mapping table. Reserve the lone M270/M142/M1064 entities for cases
where the caller wants a single weapon rather than a unit. Only consider the aggregate-level
`Mortar PLT US` / `MLRS BTY US` if true mortar/rocket unit identity outweighs losing
disaggregation. USER CALL: tube-artillery proxy vs lone-weapon entity vs aggregate-level.

---

## DECISION ITEM 4 - ArmorCoHQ near-miss (a CODE decision, presented for the same review)

Our `ArmorCoHQ` factory (echelon F, all 26 BN-echelon armor/HQ units) publishes objectType
`3:11:1:225:5:20:0:0`. It falls to generic `Ground_Aggregate` because the intended template
`aggregate-Company-HQ-Friendly` has matchType `3:11:1:225:5:20:1:0` with Specific=1 NOT
wildcarded - our Specific=0 differs in that one field, so it is a non-match. Verified both
files this pass.

| option | what changes | lands template | template facts (from file) | implication |
|--------|--------------|----------------|----------------------------|-------------|
| A - adjust our matchType emission (1 field) | emit Specific=1: `3:11:1:225:5:20:1:0` | `aggregate-Company-HQ-Friendly` `EntityLevel\vrfSim\aggregate-Company-HQ-Friendly.entity`, objectType/matchType exact `3:11:1:225:5:20:1:0`, ech Co, mv LF | subordinates = 4x generic dismounted soldier life-form (1:3:1:225:1:1:0:0). A REAL exact-match leaf instead of generic fallback, but the composition is 4 anonymous foot soldiers - a thin, non-armor "company HQ". Hostile mirror `aggregate-Company-HQ-Hostile` (222). One-line code change. |
| B - nearest-type fallback (retarget factory) | emit `3:11:1:225:14:2:1:x` (Cat 5->14, Sub 20->2) | `Tank Headquarters Section (USA)` `EntityLevel\vrfSim\Tank Headquarters Section (USA).entity`, objectType `3:11:1:225:14:2:1:0`, matchType `3:11:1:225:14:2:1:-1`, ech "HQ Sec", mv LF, gui-can-create True | subordinates = CDR+XO 2x M1A2 (1:1:1:225:1:1:3:0); FSO 1x M3A2 Bradley CFV (1:1:1:225:2:1:2:0); AUX M577 CP + 2x HMMWV. The MILITARILY CORRECT armor CoHQ composition, but requires changing MORE than one field and reclassifies the unit from a company (Co) to an HQ section (HQ Sec) echelon. |

RECOMMENDATION (executor; awaiting user adjudication): Option B (`Tank Headquarters Section
(USA)`) is the militarily correct armor battalion/CoHQ composition and is preferable on
semantics; Option A is the cheapest code fix and at least escapes the generic fallback but
yields 4 generic dismounts. Because these 26 F-units are today ALL falling to generic, either
option is a strict improvement. USER CALL: correct-composition retarget (B) vs one-line
match fix (A); note the echelon label changes under B.

---

## Open questions for the user (beyond the 4 decision items above)

1. Country side: hostile (WASA) units still emit Country 225 (USA) today, so hostile armor
   gets USA templates not the RUS (222) mirrors that exist. Do we want the mapping to switch
   Country by force side (VRF_GROUND_TRUTH 0.1.6)? This affects every gap's hostile rows.
2. Modeling-world policy: is switching any branch to AggregateLevel.sms acceptable at all,
   given aggregate-level units cannot disaggregate or model combat? If a hard NO, GAP 1
   (engineer BN), GAP 2 (IFV company), and GAP 3 (true mortar/rocket) reduce to "proxy now
   or author entity-level content later" - no third path.
3. Proxy-labeling: when a unit is mapped to a least-wrong proxy (breach-for-engineer,
   dismounted-for-mech, tube-for-mortar), should the port surface that substitution
   (name suffix / console note) so downstream C2SIM consumers know the type was approximated?
4. Authoring appetite: for the genuine gaps (engineer sapper/bridging, IFV mech company,
   mortar/rocket units), is authoring new entity-level .entity templates in scope for this
   effort, or must we live entirely within installed content?

---

## USER RULINGS (2026-07-17; recorded by supervisor from the user's answers verbatim-in-spirit)

- **Q1 (country by side): NARROWED, pick still open.** User asked "is there a chinese
  code?" FACTUAL ANSWER (verified on disk this pass): DIS Country 45 = China; the
  installed content has 11 Chinese-lineage PLATFORM entities (Chengdu J-7/J-10C/J-20,
  JF-17, Harbin Z-9 / SH-5, HQ-2 launcher, HQ-9 TEL, Type 054A frigate, DJI S1000,
  Fajr Houdong boat) - air / naval / air-defense only. ZERO Chinese ground-combat
  platforms (no tank, IFV, APC) and ZERO Chinese aggregates of any kind. A Chinese
  hostile force is therefore not assemblable from installed content; it would mean
  authoring both unit templates AND ground platform entries (DIS-typed 45) with no
  matching visual models on disk. RUS (222) remains the only hostile country with
  composed ground aggregates installed. OPTIONS: (a) keep USA-225 for hostile (as
  today), (b) RUS-222 mirrors where they exist, (c) author Country-45 content
  (heaviest; platforms + units). AWAITING the user's pick.
- **Q2 (aggregate-level modeling): RULED NO - stay entity-level.** User principle:
  "we favor the obvious: the ability to actually perform the tasks." Factual basis
  re-verified verbatim: "In aggregate-level scenarios, most preconfigured units cannot
  be disaggregated" [Modeling\UnitCreation\vrf_createAggregates.htm] and every
  EntityLevel.sms unit CAN disaggregate [Modeling\EntityLevel\
  vrf_entityLevelAggregateConcepts.htm]. (Correction of record: this doc's scope note
  and ground truth said "can never disaggregate" - the doc's actual wording is
  "most ... cannot"; the ruling is unaffected since task-performability excludes that
  content either way.) CONSEQUENCE: every OUT-OF-CHAIN (AggregateLevel) candidate row
  in gaps 1-3 is eliminated; per the doc's own note, gaps reduce to "proxy now or
  author entity-level."
- **By the same principle (task-performability), the Phase-3 aggregation-STATE policy
  (TASK_VOCABULARY_V2.md open question 1) is recorded as: run entity-level units
  DISAGGREGATED where the tasked verb requires combat/defense behaviors.** (Supervisor
  reading of the user's principle - flag if over-read.)
- **Q3 (surface proxy substitutions): RULED YES** - "surface." The port will mark
  proxy-mapped units so downstream C2SIM consumers see the approximation.
- **Q4 (authoring): RULED IN SCOPE** - "given the limitations you are already
  encountering on a small scenario, authoring seems inescapable." CONSEQUENCE for
  gaps 1-3: the default resolution becomes AUTHOR proper entity-level templates
  (engineer company/BN, IFV mech-infantry company, mortar platoon/company + rocket
  battery) composed from installed platforms (M2A2, M1064, M252, M270, M142 all exist
  on disk), with in-chain proxies acceptable as INTERIM mappings until authored
  templates land. Decision item 4 (ArmorCoHQ) remains an A-vs-B code choice; option B
  (Tank Headquarters Section) is already militarily correct, so authoring adds no
  obvious option C there.
