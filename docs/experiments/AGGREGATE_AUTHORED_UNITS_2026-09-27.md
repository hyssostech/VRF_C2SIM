# AUTHORED US unit types for the aggregate-level profile - package C2 (2026-09-27)

Lane feat/aggregate-authored-units (from main 0fe0734); HEAVY; offline - no VR-Forces launch, nothing written under
C:\MAK. Under RL-20260927-04 (D-2 revised: compose to doctrine and AUTHOR the US unit types the catalogue lacks) and
RL-20260927-02 (populated containers, RUS hostile). [V] = read at the cited file/page or reproduced by a tool here;
[A] = assumed, not provable offline. NOT live-proven: no scenario has loaded these types yet.

## 1. What exists now

- Seven AUTHORED US Army unit types (kind 11, domain 1, country 225) in a DERIVED model set,
  C:\C2SIM\vrf-sms\C2SIM_AggregateTacticalLevel.sms, which INCLUDES the shipped AggregateTacticalLevel.sms unchanged.
- The RECIPE, not the files (the repo is public; every .entity/.leaf/.magx/.sms/.opd is MAK's):
  tools/sms/aggregate_authored_design.json (the decisions, each with its reason) -> tools/aggregate/authored_units.py
  --write -> tools/sms/C2SIM_AggregateTacticalLevel.recipe.json (literal, asserted edits; every output's sha256)
  -> tools/sms/Deploy-C2SimAggregateSms.ps1 (asserts, builds in memory, writes only outside C:\MAK).
- Data: data/unit-type-map-52-aggregate.json "authoredRows" (7, fidelity AUTHORED, not lookup rows);
  data/unit-composition-52-aggregate.json variants - "catalogue" (the C1 draft, kept) and "authored" (48 IBCT = HQ +
  3 Infantry BN + Cavalry SQDN + FA BN + BEB + BSB, flat per D-8; 28ID = Division HQ; 116 ABCT + FA, BEB, BSB).
- Fixture tools/FixtureGen/frame_variants/IronStorm_Centre_52_Aggregate_C2SIM.scnx on the derived set (not deployed).

## 2. The authored types

Values as deployed (health and strengths are the aggregate model's abstract scale, UG52 72.7 p1431; personnel is
officers/WOs/NCOs/enlisted, reporting only).

| type (label) | DIS 11:1:225:... | donor (AggregateTacticalLevel\vrfSim) | health | AP/AT/HE/AA strength | footprint m | max/ordered m/s | movement |
|---|---|---|---|---|---|---|---|
| Infantry BN (USA, IBCT) | 6:3:1:201 | Motorized BN (POL, Rosomak) | 18760 | 240/1944/76/0 | 2000 | 2.22/1.39 | infantry (foot) |
| FA BN (USA, IBCT) | 6:7:1:202 | FA BN (LTU, 105mm) | 4840 | 15/0/0/0 | 120 | 19.4/12.5 | motorized |
| Brigade Engineer BN (USA, IBCT) | 6:10:1:203 | Engineering BN (POL) | 17580 | 33/39/18/0 | 2000 | 19.4/12.5 | motorized |
| Brigade Support BN (USA) | 6:31:1:204 | Logistics BN (LTU) | 11640 | 0/0/0/0 | 2000 | 19.4/12.5 | motorized |
| Division HQ (USA) | 6:20:1:205 | Stryker  HHT (USA) | 9360 | 16/0/0/0 | 1000 | 26.9/15.3 | motorized |
| FA BN (USA, ABCT) | 6:8:1:206 | SP Artillery BN (POL, K9) | 16640 | 108/0/0/0 | 1500 | 18.6/12.5 | mech (tracked) |
| Brigade Engineer BN (USA, ABCT) | 6:10:1:207 | Engineer BN (RUS, Mech) | 38940 | 54/210/36/0 | 1500 | 16.7/6.94 | mech (tracked) |

DIS type scheme [V]: category = echelon (UG52 D.3.4 p1689-1690; 6 = Battalion), subcategory = branch (D.3.4 p1690,
"the Echelon Type of the unit": 3 Infantry, 7 Artillery (towed, as FA BN (LTU)), 8 SelfPropelledArtillery, 10 Engineer,
20 CommandPost; 31 is not in that list - it is what the vendor's own US CSS units carry, CSS CO (USA)
11:1:225:5:31:1:1), specific 1 = ContainsHeadquarters (D.3.5 p1690), extra 201-207 = unique ids
("the extra enumerated field provides a way to uniquely identify all objects", D.3.1 p1688). matchType = objectType
exactly, so an authored template answers only its own type. The Division HQ is category 6 because the unit IS a
battalion (FM 3-94 5-22, 5-23: the division headquarters and headquarters battalion), the vendor's HQ units sit below
their formation (BN HQ at platoon, BDE HQ at company) and UG52 40.80 p902 requires the division container above it.

## 3. Provenance per type (the full reason of every edit is in the design file and the recipe)

- Infantry BN (USA, IBCT). Doctrine: FM 3-96 1-6 (three Infantry battalions are the IBCT's primary maneuver
  forces), 1-7 (HHC, three rifle companies, weapons company; HHC mortar platoon 120-mm), 1-8 (three rifle platoons of
  three rifle squads + a weapons squad; mortar section of two 60-mm), 1-9 (weapons company: four assault platoons,
  two ITAS vehicles each plus MK19/M2 vehicles and a leader's vehicle, four Soldiers per squad). Donor: a NATO
  battalion whose systems are exactly an infantry battalion's (120-mm mortar tube, ground-unit-tactics-enabler, simple
  obstacle destroyer, reports, IR/engineering/NBC sensors; no SAM, no engineer systems; battalion footprint). Mech CO
  (USA, M2) scaled was rejected: company footprint and postures, tracked, no battalion tactics set. Assemblies: 36 x
  Rifle SQD US, 6 x Mortar 60mm, 4 x Mortar 120mm (four tubes = the vendor's US mortar platoon, Mortar PLT (USA,
  M1064)), 8 HMMWV-TOW, 4 HMMWV-Mk19, 4 HMMWV-.50cal, 4 HMMWV, plus personnel assemblies (platoon leaders, weapons
  company crews per 1-9, mortar platoon, BN staff). Changed: movement system -> the vendor's US foot system
  (infantry-aggregated-movement, grafted from Rifle PLT (USA, USMC)) and its speeds; diesel -> 0 (the authored
  vehicles roll up motor gas); can-be-embarked-upon False. Scale check: health 18760 = 9 vendor USMC rifle platoons
  (2262 each) within 10 pct.
- FA BN (USA, IBCT). Doctrine: FM 3-96 1-12 (HHB, two 105-mm batteries of six M119, one 155-mm battery of six M777,
  target acquisition platoon). Donor: the vendor's towed 105-mm battalion (motorized, 105-mm howitzer system,
  artillery-automatic-attack enabler). The 155-mm howitzer and FASCAM systems are GRAFTED from FA PLT (USA, 155mm).
  Assemblies: 12 x Howitzer Towed 105mm, 6 x Howitzer 155mm, 18 HMMWV, 6 Truck (the vendor US FA pattern: a HMMWV and
  a truck per gun), 1 AN/TPQ-36, BN staff. Set: AA/AT/HE strength 0 (the donor's MANPADS, RPG-7, AGS-17; FM 3-96 1-12
  has no organic air defense; the vendor's US FA PLT carries 0/0/0); personnel 30/249/246 (the vendor US FA
  platoon's per-gun strength x 18 + staff; reporting only). Scale: health 4840 vs the donor's 6510 for 18 guns.
- Brigade Engineer BN (USA, IBCT). Doctrine: FM 3-96 1-13 (HHC, two engineer companies, signal, MI with TUAS, CBRN
  recon), 1-17 (breach, obstacles, survivability, route clearance), 1-18, 1-19 ("The IBCT currently does not have a
  bridging capability"). Donor: the vendor's WHEELED engineer battalion with the systems the aggregate engineering
  tasks need: engineering-obstacle-destroyer (its breach controller serves Breach_Obstacles / Improve_Breach, destroy
  serves Destroy_Obstacle - AggregateLevelBase\vrfSim\systems\other\engineering-obstacle-destroyer.sysdef [V]),
  obstacle-builder (Construct_Abatis/Barbed_Wire/Berm, Improve_Obstacle), mine-dispenser, fortification-builder,
  ditch-digger, and its Construction Material / Explosives / Mine Canisters / Concertina supplies (UG52 27.2.3 p536,
  72.7.10 p1438). Changed: bridge-builder DELETED and Bridge Material DELETED (1-19); equipment -> US vehicles. NO
  roll-up: the SMS has no US engineer assembly, so the donor's own roll-up stands (three engineer companies + staff
  for two engineer companies + signal/MI/CBRN/HHC).
- Brigade Support BN (USA). Doctrine: FM 3-96 1-27 (six FSCs in direct support of the battalions, distribution,
  field maintenance and Role 2 medical companies; the ABCT/SBCT BSB has the same general configuration). Donor: the
  vendor's battalion-level logistics unit - the resupplier system (UG52 27.3.2 p539: a supply unit "never runs out").
  Assemblies: CSS CO (the vendor's US CSS company), 4 M579 Repair, 4 M806 Recovery, 4 M113 Ambulance, BN staff. The
  six FSCs are in NO leaf (open item O-4). One type serves both brigades (1-27).
- Division HQ (USA). Doctrine: FM 3-96 chapter 1 introduction p1-1 (the division is the tactical headquarters over
  two to five BCTs); FM 3-94 5-22 (main, tactical and rear command posts, mobile command group), 5-23 (the HHB: a
  complete battalion staff, HQ and support company, signal/intelligence/sustainment company). Donor: the vendor's US
  wheeled HQ unit; the ground-unit-headquarters-enabler (manage-fire-support) is GRAFTED from Tank BN HQ (USA, M1A2).
  Assemblies: 6 M577 (two per command post), 4 JLTV (mobile command group), 12 HMMWV, 8 Truck, BN staff. Set:
  footprint 1000 (the vendor's brigade HQ units).
- FA BN (USA, ABCT). Doctrine: FM 3-96 1-44 (three batteries of six M109 Paladin; AN/TPQ-53 x2, TPQ-50 x4). Donor:
  the vendor's NATO 155-mm SP battalion (tracked; 155-mm howitzer + FASCAM; automatic-attack and tactics enablers);
  its 9K38 SAM system DELETED (no organic air defense in 1-44). Assemblies: 18 M109A6, 18 M992 (the vendor pairs
  them; not in FM 3-96), 2 AN/TPQ-36, 12 HMMWV, BN staff. Set: AT/HE strength 0; personnel as the IBCT FA BN.
  Scale: health 16640 vs the donor's 18200.
- Brigade Engineer BN (USA, ABCT). Doctrine: FM 3-96 1-13, 1-18 (Armored BCTs: rapidly emplaced bridges, Volcano),
  1-19 (ABCT breach section = bridging), 1-39. Donor: the vendor's TRACKED engineer battalion, kept whole (bridge-
  builder, mine-dispenser, breach). Equipment -> US vehicles. NO roll-up (as the IBCT BEB).

Equipment keys are the SMS's own (assemblyData.xml equipment tables); each maps to an entity-level US vehicle that
exists on disk (design "equipmentToEntityLevel", checked by --selftest): e.g. Engr, ACE -> M9_Ace; Engr, Asslt Brchr
Veh -> M1150 ABV Abrams; Engr, AVLB -> M104 Wolverine; Engr, Buffalo -> Buffalo MRAP III; Howitzer, Towed -> M119A3;
M777 -> M777 Howitzer; M109A6 -> M109A5_SP_Howitzer. Five keys have no entity-level twin (JLTV, SEE, Volcano, M579,
M806).

## 4. The construction method, and why it is on the vendor's scale

- THE ROLL-UP IS THE VENDOR'S [V]. "When you roll up assemblies, the resources and modifiers in the assemblies
  overwrite the current values for the unit ... Any parameters that are not part of an assembly or roll up rules do
  not change" (UG52 72.5 p1428). The rules are data (AggregateLevelBase\gui\assemblyData.xml rollUpRules): SUM (48
  variables: health, strengths, personnel, supplies, usages), MAXIMUM (16: ranges, defense factors), AVERAGE weighted
  by Base-Health (the 5 vulnerability modifiers), COLLECT (equipment, weapons, ammunition, systems), IGNORE (the 3
  primary-equipment fields). The port reproduces the vendor's rolled units:
  Engineering BN (POL) on all 47 rolled variables and its three collected maps; Mech CO (USA, M2) on health, the
  AT/AP/HE strengths, the AT range, diesel and its weapon and ammunition maps; and - DISCRIMINATING - Tank BN HQ
  (USA, M1A2) (AT vulnerability 0.7; averaging over ALL its assemblies, the ones that do not define it counted as 0,
  would give 0.594) and Mortar PLT (USA, M1064): the average runs over the assemblies that define the modifier
  (authored_units.py --selftest).
- "The initial health for a unit is set by the SMS designer ... It is not computed based on the equipment" and "a
  unit with 10 tanks would have twice the health of a unit with 5" (UG52 27.1.1 p529) [V]: the roll-up is the
  designer's tool, not a law; health and strength should "fit with the existing simulation objects" (72.7 p1431).
- SYSTEMS ARE NOT ROLLED UP (a decision): each of the vendor's nine howitzer battalions carries ONE howitzer system of
  its calibre whatever its tube count (18-24), beside FASCAM and/or a SAM (FA BN (LTU, 105mm), SP Artillery BN (POL,
  K9): strength 800, sheaf 75-80) [V]; so the donor's systems stay, and whole blocks are deleted or grafted where FM
  3-96 requires it.
- ENDURANCE RULE (a decision): a supply's stock and per-second usage are a pair; when the roll-up supplies one half,
  the other is rescaled to keep the DONOR's days of supply (Mech CO (USA, M2) carries 1 day of food, Engineering BN
  (POL) 3.6). Without it the Infantry BN's rolled food (636) at its donor's eating rate (0.012376/s) lasts 14.3
  hours; with it, the donor's 3.55 days.
- A variable no authored assembly supplies keeps the donor's value (the vendor's rule); authored_units.py --report
  lists every such KEPT value per type, and each one that was wrong for the US unit is SET with a reason.
- Personnel and equipment are reporting only - "not used by the sim engine in the simulation" (UG52 72.7.9 p1438;
  27.1 p528) [V]. They UNDER-count an MTOE (O-3).

## 5. What the aggregate model does with each parameter group (UG52 72.2 p1418-1419, 72.7 p1430-1439, 27.1-27.3)

| group | used by the sim for | here |
|---|---|---|
| General | identity, symbol, palette | new DIS type, label, gui-unique-id, short-name, US deployable country |
| Physical | health (killed at 0), footprint radius - damage within it, posture-scaled, overlap slows MAX speed (27.1.2-27.1.4) | health rolled up; footprint = donor's (Division HQ set 1000) |
| Movement | the movement system's terrain classes (featureconfig.txt: MAK_WATERWAY is IMPASSABLE for infantry, motor and mech; the amphibious system makes it restricted L2, speed factor 0.12), max/ordered speed, posture and MOPP modifiers | donor's, except the Infantry BN: foot |
| Attack | strength per domain = attrition rate per second on the target, range per domain, ammunition usage per minute, weapon systems (indirect fire), posture/sector/CID/MOPP modifiers (27.1.1) | rolled up; indirect-fire SYSTEMS from the donor or grafted |
| Defense | vulnerability per domain multiplies the attacker's strength; defense factors for guided munitions; kill thresholds | vulnerability rolled up (health-weighted); defense factors donor's |
| EW | EW defense, comms dependence | donor's |
| Personnel and Equipment | reporting only (72.7.9) | rolled up / US vehicles |
| Supplies | fuel/food/water stocks and usage; Other Supplies are consumed by engineering objects (72.7.10; 27.2.3) | rolled up + endurance rule; BEB construction supplies kept |
| Engineering | obstacle creation, breach, destroy (the sysdef controllers above) | BEB: donor's, IBCT without bridging |

## 6. The SMS mechanism and the recipe run

- An SMS's object types ARE the .entity files in its vrfSim directory ("copy the files into the vrfSim directory in
  the SMS", UG52 68.8 p1326; a promoted model is "added to it" as a .entity on save, 68.3.8 p1315) - there is no list
  to register them in. The including set has the higher priority and needs no copy of what it does not change (68.3.1
  p1310, 68.3.3 p1312, 68.3.5 p1314: "every SMS must have vrfSim.opd"). Per-SMS unit symbols live in
  gui\visuals\Unit (68.4 p1316; 68.5.4 p1320), one .leaf + one .magx per type, the layout AggregateTacticalLevel uses.
- smsChecksum*.cksm: NOT needed. The SMS upgrade tool "compares these checksums to checksums for the files in the
  standard SMS in the old version of VR-Forces and to the files in the standard SMS in the version ... you are
  upgrading to" (UG52 68.5.2 p1319, Table 56); the .cksm files are those per-file checksums (one "file,checksum" line
  per file, one file per release) and only the five standard sets carry them - the vendor's own including set MAKTest
  ships none, nor do the C2SIM entity-level sets that ran.
- The .sms body is the vendor AggregateTacticalLevel.sms: model-set-directory changed, include retargeted to itself;
  its HLA validator-string and "HLA 1516 Evolved RPR 2.0 Extended Aggregates" connection are ASSERTED (the aggregate
  warfare model needs HLA Evolved, UG52 27.1 p528).
- Run evidence 2026-09-27: -WhatIf exit 0 - the 5 asserted .sms lines, its 2 edits and every donor edit found, 23
  outputs ASCII + parse + equal to their recorded sha256, "nothing written"; -SelfTest PASS - CLEAN build + idempotent
  rerun, and SIX DIRTY controls each exit 3 with 0 files written (an edited donor line, a line inside an edited block
  (sha256), a deleted block's first line, a line inside a GRAFTED source block, an asserted .sms line, a donor line no
  edit touches (the output hash)); -Dest under C:\MAK and a relative -Dest refused (exit 2). Real deploy exit 0: 23
  files under C:\C2SIM\vrf-sms, read back (manifest: tools/sms/README.md); a later -WhatIf reports all 23
  "(unchanged)". The PowerShell build and the Python generator are independent implementations that agree byte for
  byte on all 23 outputs (the deploy gates each output on the sha256 the generator recorded).
- Validation on the deployed chain [V]: each authored type lands its own warfare-model UNIT from the derived set and
  its .magx maps it; the derived chain resolves EVERY vendor template, every map row and every US init-shell /
  container candidate type (11:1:225:5..10:0..34:1:0|1) exactly as the vendor chain does - the set only adds
  (typemap_check.py; its DIRTY control answers one candidate with an authored template and must fail); the
  EntityLevel twin of every authored type is the MIXED Ground_Aggregate (4 subordinates: no EXPAND, the same case-3
  path as every C1 container).

## 7. Data rows and the pairing

- Type map: 7 authoredRows, fidelity AUTHORED, never lookup rows - the app reads "rows" only (UnitTypeMap.Parse) and
  would parse AUTHORED as Failed while Lookup ignores fidelity; typemap_check.py refuses AUTHORED in "rows".
- Composition: each row has a variant; each variant is gated as a whole on its own chain. The catalogue rows stay
  FIRST (a variant-unaware resolver taking the first row of a key gets the catalogue composition). defaultVariant =
  catalogue, because the sequencing recorded under RL-20260927-04 - the seat's, not an owner decision - has G1 prove
  the container mechanism with catalogue units first; flipping the default is the lead's call once the authored set
  is live-proven.
- THE HAZARD [V]: on the SHIPPED set the authored types land EMPTY generic containers (BN_Light Infantry, BN,
  Artillery, BN, Engineer, BN, CSS PA) or the base abstract (Division HQ, FA BN (ABCT)) - composition_check.py
  --selftest's pairing control fails the authored variant on the vendor chain. The authored variant must only ever
  run with the fixture on the derived set.
- Gates: typemap_check.py PASS and --selftest PASS (8 new DIRTY controls); composition_check.py PASS (both variants,
  --tree --init-census --twins --vendor) and --selftest PASS (pinned: 48 IBCT authored 8 leaves / 0 containers, 28ID
  1/0, 116 ABCT 29/6; 7 new DIRTY controls); survey_magx.py --selftest PASS; authored_units.py --check and --selftest
  PASS.

## 8. The fixture

IronStorm_Centre_52_Aggregate_C2SIM: the IronStorm_Centre_52_Aggregate recipe with --sms aggregate-c2sim (the
derived set by absolute path) and --terrain aggregate. .scnx sha256
67dbc5953a8d4af234be861f0965109945fa2dcce105ae45779d831ea33d61ad (rebuilds identically; the existing aggregate
fixture still rebuilds to 804e2c39...). validate_fixture.py ALL FIXTURES: OK (derived set opened: includes
AggregateTacticalLevel.sms, HLA validator-string kept, vrfSim.opd present, 7 authored types, none shadowing a vendor
file, paired with the aggregate terrain). Negative controls: the builder REFUSES the derived set without the
aggregate terrain; the fixture validated as the shipped set FAILS. NOT deployed to C:\MAK.

## 9. Open decisions for the owner

- O-1 Infantry mobility: FOOT (doctrine, FM 3-96 1-1, 1-6; water impassable) or motorized (FM 3-96 1-2: augmentation
  "can include wheeled assets"). On foot the Infantry BNs order 1.39 m/s against the motorized leaves' 12.5 m/s, and a
  container's move ends with its members' (DESIGN_AGGREGATE_CONTAINERS sec 6: a memberless container's move "would end
  at once"), so the brigade takes about nine times as long.
- O-2 Combat-power scaling: roll-up of the vendor's US assemblies (here) vs the donor's values. The Infantry BN is
  18% of its Polish donor's health (no armored vehicles) and has AT 1944 (Javelin + TOW): fragile in the open, potent
  against armor. The BEBs keep their donors' values (no US engineer assemblies exist).
- O-3 Personnel (reporting only): the roll-up under-counts an MTOE (Infantry BN 394, BSB 103); set MTOE figures?
- O-4 The six FSCs (FM 3-96 1-27): in no leaf now. Fold each into its battalion, add them to the BSB, or leave out.
- O-5 BCT HQ and the ABCT cavalry squadron stay PROXY (Stryker HHT; Stryker Cavalry SQDN for an M3/M1 squadron).
  Authoring them is the same recipe (donors Stryker HHT + HQ enabler; Armored Cavalry SQDN (GBR) + M3A3/M1A2).
- O-6 IBCT BEB kept its booby-trap-setter (FM 3-96 1-17 does not list booby traps; not removed without doctrine).
- O-7 The IBCT FA BN carries TWO howitzer systems (105 + 155); which one the automatic fire support picks is [A]
  (vendor precedent for two indirect-fire calibres in one battalion: MRL Battalion (RUS, BM-27), 220 + 122 mm).

## 10. For the C1 code lane (not done here: src/ is theirs)

- The resolver must SELECT a variant (setting) and REFUSE "authored" unless the fixture's SMS is the derived set
  (the runner already classifies it as the aggregate family: RunnerLib.ps1 Get-ModelSetFromSms follows its include).
- AUTHORED leaves are created by objectType like any catalogue leaf; nothing else differs.

## 11. Adversarial review

How an authored type could break the aggregate model, strongest first:
(i) WRONG SMS - on the shipped set every authored type lands an EMPTY container or the base abstract (sec 7): a
populated brigade would hold nothing that moves. Guarded offline (variant pairing, composition_check pairing control,
typemap_check hazard lines, fixture pairing); the app-side guard is the C1 lane's (sec 10). (ii) ATTRITION IS NOT
KEYED BY EQUIPMENT - the aggregate model fights on per-domain strength x vulnerability (UG52 27.1.1); equipment and
personnel are reporting only (72.7.9), so a wrong or unknown equipment key cannot change combat - but AMMUNITION keys
feed ammunition usage per domain (72.7.5), which is why the maps are the vendor's own keys, collected by the roll-up
(no invented key: --selftest refuses one). (iii) UNKNOWN ENUMERATIONS - extras 201-207 are new; category/subcategory
are the vendor's own values; subcategory 20 CommandPost on a GROUND unit has no vendor precedent (the vendor uses 20
only on domain-2 helicopter units) - a consumer that renders by DIS type may draw it oddly [A]; the .magx names our
element, so the VR-Forces GUI does not guess. (iv) GUI PALETTE AND SYMBOLS - the per-SMS .leaf/.magx are copies of
vendor files with only the element name / type changed (the vendor's own .leaf files share UUIDs - three of them sit in
344 of its 399 - so copying them is the vendor's pattern). Our own symbols are in the top set, where a derived SMS
reads its model and element definitions (UG52 68.4 p1316). OPEN [A]: whether vrfGui then still finds
AggregateTacticalLevel's 395 mapped symbols for the INCLUDED vendor types. For: gui files "are included the same way
entity-specific files are" (68.3.5 p1314), and the vendor's own AggregateTacticalLevel (visual-data-in-shared-data
False) maps only 1 of the 45 AggregateLevelBase unit types it includes (RGT, Engineer) - if those containers draw
their own symbols under the shipped set, the GUI already looks past the top set. Against: no vendor set stacks a
False set on a False set, as ours does. If a live look shows vendor units with generic symbols, the recipe copies
AggregateTacticalLevel's gui\visuals into the derived set - a deploy change, not a design change. (v) DONOR
INHERITANCE - every unedited parameter is the donor's; the output-hash check stops a deploy when a donor changes
anywhere, and --report shows every KEPT value. An SOE "Roll Up" on an authored type would overwrite the SET
decisions ("your changes are lost", UG52 72.5 p1428): the deploy script is the only writer of the derived set.
(vi) ROLL-UP SEMANTICS - proven on four vendor units, the supplier-only average discriminated on Tank BN HQ (USA,
M1A2); systems deliberately not rolled.
Assumed about the .entity format: it is XML whose parameters are single <int|real|string|bool paramName> lines and
<DtRwMap>/<componentSystem> blocks closed at their own indentation (true for all 7 donors and 3 graft sources - the
build refuses otherwise); element text keeps raw quotes; line endings are per file (LF .entity, CRLF .leaf/.magx/.sms)
and kept; a componentSystem's systemName needs no rename (vendor units keep their donors' names too, e.g. "BDE HQ
(RUS, T-90)-weapon" inside Mech BDE HQ (RU, BMP-3)). Competing designs rejected: generating .entity files from
scratch (every parameter an invention); editing AggregateTacticalLevel in place (C:\MAK, lost on reinstall); authoring
NEW assemblies (the roll-up is an editor action, UG52 72.5 p1428 - the sim reads only the rolled values - and a
derived gui\assemblyData.xml would replace the vendor's 5.7 MB file as a whole, 68.3.5 [A]).

## 12. Not claimed

That VR-Forces loads the derived set, creates or moves any authored type, renders its symbol, or that its combat
results are realistic - G1 (or a smaller live check) owes each. Doctrine counts FM 3-96 does not give (vehicle
counts, personnel) are estimates and say so.
