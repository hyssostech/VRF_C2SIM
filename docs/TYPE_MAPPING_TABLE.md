# TYPE MAPPING TABLE (Phase 2.1 deliverable)

Owner: executor E5, 2026-07-16/17. Groundwork plan docs/VRF_GROUNDWORK_PLAN.md Phase 2.1:
"every COA-STP1 + golden-init unit -> nearest REAL installed template ... the mapping table
the user reviews line-by-line - 'as close to their real types as possible' made concrete."
Doc-only, offline. ASCII only.

Inputs (all read this pass):
- data/COA-STP1_Initialization.xml (128 Unit blocks).
- docs/golden-trace/STP-TC-small-6-12-24_Initialization.xml (80 Unit blocks; the "golden STP
  init", 49 units + 4 areas live per docs/experiments/P4B_live_pass_2026-07-13.txt; canonical
  --parse-init target per docs/OPUS_EXECUTION_PLAN.md:70). NOTE the plan brief said the golden
  init lives "in data/"; it is actually under docs/golden-trace/ - the file named by the repo
  docs. data/COA-STP1_Sweden_Initialization.xml and data/R9_Mojave_Lean_Initialization.xml are
  relocated derivatives of the same golden force, not the canonical file.
- docs/VRF_GROUND_TRUTH.md 0.1 (installed content catalog), especially 0.1.3.b roster (the
  movement column LF/HU/HM), 0.1.3.c compositions, 0.1.5 fallback logic, 0.1.6/0.1.7.
- docs/TYPE_GAP_ADJUDICATION.md (the pending user decisions - reproduced, not re-decided).
- src/VrfC2SimApp/UnitTranslator.cs (the live dispatch, reproduced exactly to compute the
  "emits today" column - see method note).

## Method - how "emits today" was computed (not guessed)

The port's dispatch (UnitTranslator.Plan, verified src/VrfC2SimApp/UnitTranslator.cs:39-67)
keys almost entirely off the SIDC, NOT the C2SIM EchelonCode field and NOT the init's DIS
type:
- SIDC char index 2 == 'A' -> air entity (Rw/Mq1); index 2 == 'S' (or DIS domain 3) -> Boat
  entity; index 1 == 'N' -> Civilian.
- else by SIDC ECHELON char (index 11): 'B'->Scout/MobileIrregular, 'D'->ArmorPlatoon,
  'E'->ArmorCompany, 'F'->ArmorCoHQ, anything else ('-','C','H',...) -> Tank (single M1A2).
- The 2525C echelon char at SIDC index 11 is: C=section, D=platoon, E=company, F=battalion,
  H=brigade, '-'=none. The five aggregate/entity factories then publish a fixed objectType
  that the back-end resolves by the OPD best-match rule (0.1.2/0.1.5):
  - ArmorCompany  -> objectType 3:11:1:225:5:2:0:0  -> **Tank Company (USA)** (real)
  - ArmorPlatoon  -> objectType 3:11:1:225:1:1:3:0  -> **Ground_Aggregate** (generic; Cat1 has
                     no Kind11 leaf)
  - ArmorCoHQ     -> objectType 3:11:1:225:5:20:0:0 -> **Ground_Aggregate** (generic; the real
                     aggregate-Company-HQ-Friendly needs Specific=1, ours is 0 -> non-match)
  - Tank (default)-> entity 1:1:225:1:1:3:0         -> **single M1A2** entity
  - Force (friendly/hostile) sets only the VR-Forces side, never DIS Country - so hostile
    armor still gets the Country-225 USA composition (this is policy question Q1).

I reproduced this dispatch in a script over both init files; the per-class "today" columns
below are its output, not a paraphrase of 0.1.7.

CAVEAT carried from ground truth 0.1.5/0.1.8 (not re-settled this pass): the "COY/E ->
Tank Company (USA)" resolution rests on the static OPD best-match arithmetic. A 2026-07-15
LIVE formation query for 114.MechCoy came back with the lowercase Ground_Aggregate formation
signature, NOT the Title-Case Tank Company signature the static rule predicts. So it is
possible some/all COY units actually resolve to Ground_Aggregate live, not Tank Company. This
does NOT change any mapping proposal (a mech/engineer/artillery COY is mis-mapped either way,
armor OR generic) but it means the "64 -> Tank Company (USA)" today-figure is
static-analysis-derived and awaits one live vrfSim.log OPD-resolve line (or a 0.5 scnx save)
to confirm. The delta framing in 1.2 is robust to this: whichever it is (armor mis-map or
generic fallback), the 51 non-armor COY are wrong today and get a branch-correct template
under this mapping.

## Column legend

- **class** = the distinct (C2SIM function-ID x EchelonCode x side) group. Function ID =
  APP6C SIDC positions 5-10; EchelonCode = the C2SIM <EchelonCode> field (BN/COY/PLT/NOS/
  SECT/BDE); side = <Side> UUID (NATO Coalition = friendly, WASA = hostile).
- **mv** = the PROPOSED template's movement controller class copied from ground truth 0.1.3.b
  (load-bearing, 0.0 item 2): LF = ground-disaggregated lead-follow (platoon-class),
  HU = ground-higherUnit move-along (company/bn-class), HM = human-disaggregated (dismounted),
  entity = single platform (no aggregate controller).
- **disp** = disposition: EXACT (already the right real template), NEAR (real template, correct
  or near branch, minor caveat), PEND (a user adjudication is open - candidates listed, winner
  NOT picked), LONE (nearest real type is a single platform entity), AVN (out-of-scope
  aviation).
- Country policy Q1 (TYPE_GAP_ADJUDICATION.md open question 1) is unresolved: hostile rows
  show the as-today USA-225 template AND the RUS-222 mirror where one exists, tagged "Q1".
  Where no 222 mirror exists on disk, the row says "Q1 moot".

---

## 1. Summary

### 1.1 COA-STP1 (128 units) - disposition counts

| disposition | units | notes |
|-------------|-------|-------|
| EXACT-match | 7  | the real friendly armor companies (already -> Tank Company USA) |
| NEAR-match  | 64 | real branch template now, no decision needed (country/echelon caveats) |
| PENDING-USER (gap) | 54 | GAP1 engineer=10, GAP2 mech-inf=14, GAP3 mortar/rocket=10, Decision-4 CoHQ (remaining BN)=20 |
| LONE-entity | 1  | brigade/division HQ (no brigade aggregate exists) |
| out-of-scope AVIATION | 2 | the 2 UCV aviation battalions |
| **total** | **128** | |

Partition arithmetic (rows x counts; the 44 distinct classes sum to 128 - see full table):
```
  EXACT 7  + NEAR 64 + PEND 54 + LONE 1 + AVN 2  = 128
  PEND 54  = GAP1 10 + GAP2 14 + GAP3 10 + Decision4-pure 20
  by echelon today:  COY 64 + BN 26 + PLT 23 + NOS 12 + SECT 2 + BDE 1 = 128
  by resolve today:  Tank Company(USA) 64 + Ground_Aggregate 49 + single M1A2 15 = 128
```

Counting convention (so the sub-buckets are reproducible - the 26 BN units are the crux):
- Every BN-echelon unit goes through the ArmorCoHQ factory, so Decision-4 (CoHQ) governs all
  26 BN as a CODE matter. For the DISPOSITION counts, each unit is assigned to ONE bucket: a
  branch content-gap takes precedence over the generic CoHQ decision. So the 3 engineer BN are
  counted in GAP1 and the 1 rocket BN in GAP3 (matching TYPE_GAP_ADJUDICATION.md's own totals:
  GAP1 "10 units = 7 coy + 3 bn", GAP3 "10 units" incl the rocket BN). "Decision4-pure 20" =
  26 BN - 3 engineer BN (GAP1) - 1 rocket BN (GAP3) - 2 aviation BN (AVN). All 24 pending BN
  (20 + 3 + 1) still hit the CoHQ decision; each is just shown once, under its most-specific
  gap.
- The 2 UCV aviation BN are among the 26 ArmorCoHQ units the Decision-4 code fix touches, but
  are dispositioned AVN here because aviation is out of the ground-mapping scope.

### 1.2 COA-STP1 - delta vs what ships today

- **Tank Company (USA) mis-maps eliminated: 51.** Today all 64 COY units become Tank Company
  (USA); that is correct only for the 13 real armor companies (UCA COY: 7 friendly + 6 hostile).
  The other **51** COY (mech-inf, infantry, engineer, artillery, air-defense, target-acq,
  recon, CSS and HHC companies) are wrongly given an armor company. Under this mapping all 51
  leave the armor mis-map: 29 get a branch-correct real template immediately (NEAR), 22 get
  branch-appropriate PENDING candidates (engineer/mech/mortar). The 13 real armor companies
  stay Tank Company (USA) (EXACT).
- **Ground_Aggregate fallbacks eliminated: 49.** Today BN(26) + PLT(23) fall to the generic
  4-anonymous-vehicle Ground_Aggregate. Under this mapping 15 PLT get a real template
  immediately (air-defense, anti-armor, recon); the remaining 34 (8 PLT + 24 BN pending + 2
  aviation BN) get real candidates once the CoHQ / content decisions land. None stay generic.
- **Lone M1A2 entities: 15 -> 1.** Today NOS(12) + SECT(2) + BDE(1) become a single M1A2 tank.
  Under this mapping 14 become real recon/artillery/air-defense/target-acquisition aggregates
  or teams; only the single brigade/division HQ stays a lone entity (and is upgraded from an
  M1A2 tank to an M577 command post).
- Net: **71 of 128** units get a correct/near-correct real type with NO user decision (7 EXACT
  already correct + 64 NEAR); **54** await the four adjudications; **1** lone HQ entity; **2**
  aviation are out of ground scope.

### 1.3 Golden STP init (80 units) - disposition counts, shown separately

The golden init is a different (Sweden/STP) scenario with lower-quality placeholder data
(10 units literally named "Diesel"; EchelonCode disagrees with the SIDC echelon char for 8
units; 15 units carry DISDomain=3 which forces the Boat-entity branch regardless of a ground
SIDC). Its ground classes reuse the COA-STP1 template proposals; its heavy aviation/naval
content is out of ground scope.

| disposition | units | notes |
|-------------|-------|-------|
| EXACT-match | 0 | no real armor company in the golden force |
| NEAR-match  | 9 | artillery batteries (3) + hostile infantry co (4) + hostile inf plt (2) |
| PENDING-USER (gap) | 43 | GAP2 mech-inf=31, GAP3 mortar=2, Decision-4 CoHQ (BN)=10 |
| LONE-entity | 2 | 1.MechBde + 1.BdeHQ (brigade echelon, no aggregate) |
| out-of-scope AVIATION | 11 | MFQ UAS(6) + UCV/UCVRA/UCVRUH aviation(5) |
| out-of-scope NAVAL / DISDomain=3 data-quirk | 15 | 8 real watercraft/amphib craft + 2 amphibious coy + 5 "Diesel" ground-SIDC units mis-domained to naval |
| **total** | **80** | |

Golden partition arithmetic:
```
  NEAR 9 + PEND 43 + LONE 2 + AVN 11 + NAVAL/quirk 15 = 80
  PEND 43 = GAP2 31 (UCIZ PLT 21 + UCIZ COY 10) + GAP3 2 + Decision4 10 (UCIZ BN 8 + UCFH BN 1 + UCI BN 1)
  by resolve today: Ground_Aggregate 34 + Tank Company(USA) 23 + Boat 15 + AH-64/CH-47 air 6 + single M1A2 2 = 80
```
Reconciliation note: static dispatch over the 80 Unit blocks yields 57 aggregates + 23
entities; the live record reports "49 units + 4 areas". The gap is app-level filtering /
area handling I did NOT fully trace this pass (flagged as an open question); it does not
affect the type-mapping proposals, which key off the per-unit SIDC/DIS regardless.

---

## 2. COA-STP1 main table (44 distinct classes, grouped by branch family)

Movement (mv) is the PROPOSED template's controller class. "emits today" is the port's current
factory + what it resolves to. Members are compressed samples; full membership is derivable
from data/COA-STP1_Initialization.xml.

### 2.1 Armor (UCA)

| class | side | n | members (sample) | today: emits -> resolves | PROPOSED template (objectType) | mv | disp | rationale / caveat |
|-------|------|---|------------------|--------------------------|--------------------------------|----|----|--------------------|
| UCA COY | fr | 7 | HQ/1-35, A/1-35, B/1-35 +4 | ArmorCompany -> Tank Company (USA) | **Tank Company (USA)** 3:11:1:225:5:2:0:0 | HU | EXACT | already correct; real armor co = HQ Sec + 3x Tank Platoon (USA). No change. |
| UCA COY | ho | 6 | 1/7154, 2/7154, 3/7154 +3 | ArmorCompany -> Tank Company (USA) | as-today **Tank Company (USA)** 3:11:1:225:5:2:0:0 + Q1 mirror **Tank Company (RUS)** 3:11:1:222:5:2:0:0 | HU | NEAR | correct branch+echelon; only the DIS Country is wrong today. Q1 pending: keep USA-225 or switch to RUS-222. |
| UCA BN | fr | 2 | 1-35/2/1_A, 1-35_MAIN | ArmorCoHQ -> Ground_Aggregate | PEND: Decision-4 A **aggregate-Company-HQ-Friendly** 3:11:1:225:5:20:1:0 / B **Tank HQ Section (USA)** 3:11:1:225:14:2:1:0 | LF | PEND | armor battalion HQ. See TYPE_GAP_ADJUDICATION.md DECISION ITEM 4 (A=1-field match fix, 4 dismounts; B=correct armor CoHQ composition, echelon relabels to HQ Sec). |
| UCA BN | ho | 1 | 7154/HQ_71 | ArmorCoHQ -> Ground_Aggregate | PEND: Decision-4 A **aggregate-Company-HQ-Hostile** 3:11:1:222:5:20:1:0 / B Tank HQ Section (USA) | LF | PEND | Decision 4 + Q1 (222 HQ mirror exists). |
| UCA BDE | fr | 1 | 2/1_AD/25_ | Tank (default) -> single M1A2 | LONE **M577A2 Command Post** 1:1:1:225:3:11:0:0 (alt: aggregate-Company-HQ-Friendly) | entity | LONE | no brigade/division aggregate in the chain; keep a single HQ entity but a command post, not a tank. |

### 2.2 Mechanized infantry (UCIZ) - GAP 2

| class | side | n | members (sample) | today: emits -> resolves | PROPOSED template (objectType) | mv | disp | rationale / caveat |
|-------|------|---|------------------|--------------------------|--------------------------------|----|----|--------------------|
| UCIZ COY | fr | 4 | HQ/5-20, A/5-20, B/5-20, C/5-20 | ArmorCompany -> Tank Company (USA) | PEND GAP2: **aggregate-Co-Infantry-Friendly** 3:11:1:225:5:3:1:0 (dismounted) / 3x **Mech Plt (USA) IFV (Dep)** 3:11:1:225:3:4:0:0 / **Stryker Rifle Plt** / agg-lvl **Mech Inf CO US** | HU | PEND | wrong branch today (armor). See TYPE_GAP_ADJUDICATION.md GAP 2 (no composed USA IFV mech-inf company in chain). |
| UCIZ COY | ho | 9 | 1/7151, 2/7153, 3/7153 +6 | ArmorCompany -> Tank Company (USA) | PEND GAP2 + Q1: **aggregate-Co-Infantry-Hostile** 3:11:1:222:5:3:1:0 / -Friendly 225 / candidates as above | HU | PEND | GAP 2 + Q1 (222 dismounted mirror exists). |
| UCIZ PLT | ho | 1 | 2/7151 | ArmorPlatoon -> Ground_Aggregate | PEND GAP2 + Q1: **Mech Plt (USA) IFV (Dep)** 3:11:1:225:3:4:0:0 / **Tank Platoon (RUS)** 3:11:1:222:3:2:0:0 | LF | PEND | the "one UCIZ D mech platoon" GAP 2 calls out; only composed IFV mech platoon is USA/deprecated. |
| UCIZ BN | fr | 1 | 5-20/2/1_A | ArmorCoHQ -> Ground_Aggregate | PEND: Decision-4 (A aggregate-Company-HQ-Friendly / B Tank HQ Section) | LF | PEND | mech-inf battalion HQ -> the CoHQ decision. |
| UCIZ BN | ho | 3 | 7151/HQ, 7152/HQ, 7153/HQ | ArmorCoHQ -> Ground_Aggregate | PEND: Decision-4 + Q1 (aggregate-Company-HQ-Hostile 222) | LF | PEND | Decision 4 + Q1. |

### 2.3 Infantry, dismounted (UCI)

| class | side | n | members (sample) | today: emits -> resolves | PROPOSED template (objectType) | mv | disp | rationale / caveat |
|-------|------|---|------------------|--------------------------|--------------------------------|----|----|--------------------|
| UCI COY | fr | 4 | A/1-6, B/1-6, C/1-6, HQ/1-6 | ArmorCompany -> Tank Company (USA) | **aggregate-Co-Infantry-Friendly** 3:11:1:225:5:3:1:0 | HU | NEAR | real composed dismounted infantry company (HQ Sec + 3x Infantry Platoon); correct branch/echelon. Port must emit Specific=1 to land it (exact matchType). |
| UCI BN | fr | 2 | 1-6/2/1_AD, 1-6_MAIN | ArmorCoHQ -> Ground_Aggregate | PEND: Decision-4 (branch alt: an infantry BN HQ has no dedicated template) | LF | PEND | infantry battalion HQ -> the CoHQ decision. |

### 2.4 Reconnaissance / cavalry (UCR, UCRVA)

| class | side | n | members (sample) | today: emits -> resolves | PROPOSED template (objectType) | mv | disp | rationale / caveat |
|-------|------|---|------------------|--------------------------|--------------------------------|----|----|--------------------|
| UCR NOS | fr | 6 | 1-1/2/1_AD, HQ/1-1, A/1-1 +3 | Tank (default) -> single M1A2 | **Tank Platoon (USA)** 3:11:1:225:3:2:0:0 (armored-cav proxy; troop-sized -> Tank Company USA) | LF | NEAR | 1-1 Cav Sqn (HQ + A/B/C/D troops). No USA composed recon/cav aggregate exists (only RUS); armored proxy; NOS echelon ambiguous - user may prefer troop=company. |
| UCRVA PLT | ho | 3 | REC-7151, REC-7152, REC-7153 | ArmorPlatoon -> Ground_Aggregate | **Recon Vehicle Platoon (RUS BMP2)** 3:11:1:222:3:6:0:49 | LF | NEAR | the ONLY composed recon aggregate on disk; hostile/RUS-appropriate (3x BMP-2). Q1 moot: no USA recon mirror. |
| UCRVA COY | ho | 2 | 2/7157, REC-7154/7 | ArmorCompany -> Tank Company (USA) | as-today **Tank Company (USA)** 3:11:1:225:5:2:0:0 + Q1 **Tank Company (RUS)** 3:11:1:222:5:2:0:0 (armored-cav proxy) | HU | NEAR | no composed cavalry troop (Armored Cavalry Troop is abstract/no subs); armor proxy at troop echelon. Q1 pending. |
| UCRVA BN | ho | 1 | 7157/HQ_71 | ArmorCoHQ -> Ground_Aggregate | PEND: Decision-4 + Q1 | LF | PEND | cavalry squadron HQ -> the CoHQ decision + Q1. |

### 2.5 Engineer (UCE, UCEC) - GAP 1

| class | side | n | members (sample) | today: emits -> resolves | PROPOSED template (objectType) | mv | disp | rationale / caveat |
|-------|------|---|------------------|--------------------------|--------------------------------|----|----|--------------------|
| UCE COY | fr | 6 | A/40, B/40, C/40, D/40 +2 | ArmorCompany -> Tank Company (USA) | PEND GAP1: **Tank Breach Company (USA)** 3:11:1:225:5:2:0:78 (armor+mine-plow proxy) / author sapper | HU | PEND | no engineer aggregate in chain. See TYPE_GAP_ADJUDICATION.md GAP 1 (breach-proxy vs author). |
| UCE BN | fr | 2 | 40/2/1_AD, 40_MAIN | ArmorCoHQ -> Ground_Aggregate | PEND GAP1 (no in-chain engineer BN) + Decision-4 CoHQ | HU | PEND | GAP 1 explicitly: engineer BN has no in-chain aggregate; also hits the CoHQ decision. |
| UCEC COY | ho | 1 | 715EN/HQ_7 | ArmorCompany -> Tank Company (USA) | PEND GAP1 + Q1: Tank Breach Company (USA) (no RUS breach mirror) | HU | PEND | GAP 1; Q1 moot (no 222 breach company). |
| UCEC BN | ho | 1 | 8122/HQ_71 | ArmorCoHQ -> Ground_Aggregate | PEND GAP1 + Decision-4 + Q1 | HU | PEND | combat-engineer BN: GAP 1 + CoHQ decision + Q1. |

### 2.6 Field artillery / mortar / rocket (UCF, UCFHE, UCFM, UCFR) - mortar/rocket = GAP 3

| class | side | n | members (sample) | today: emits -> resolves | PROPOSED template (objectType) | mv | disp | rationale / caveat |
|-------|------|---|------------------|--------------------------|--------------------------------|----|----|--------------------|
| UCFHE COY | ho | 3 | 1/7158, 2/7158, 3/7158 | ArmorCompany -> Tank Company (USA) | **Field Artillery Battery (USA) M109** 3:11:1:225:4:8:0:0 | HU | NEAR | correct branch: SP-howitzer battery (UCFHE = SP howitzer/gun). No RUS FA in chain -> Q1 moot; echelon battery ~ company. |
| UCFHE BN | ho | 3 | 7158/HQ, 7911/HQ, 7912/HQ | ArmorCoHQ -> Ground_Aggregate | PEND: Decision-4 + Q1 (branch alt: FA Battery M109) | HU | PEND | artillery BN HQ -> CoHQ decision. |
| UCF NOS | fr | 4 | A/4-27, B/4-27, C/4-27, HQ/4-27 | Tank (default) -> single M1A2 | **Field Artillery Battery (USA) M109** 3:11:1:225:4:8:0:0 (alt: FA Platoon M109 3:11:1:225:3:8:0:0) | HU | NEAR | batteries of 4-27 FA; SP-howitzer battery = HQ Sec + 2x FA Platoon M109. |
| UCF BN | fr | 2 | 4-27/2/1_A, 4-27_MAIN | ArmorCoHQ -> Ground_Aggregate | PEND: Decision-4 (branch alt FA Battery) | HU | PEND | FA battalion HQ -> CoHQ decision. |
| UCFM PLT | ho | 7 | 1MTR/7151, 2MTR/7151 +5 | ArmorPlatoon -> Ground_Aggregate | PEND GAP3 + Q1: **FA Platoon (USA) M109** 3:11:1:225:3:8:0:0 (tube proxy) / lone **M1064 Mortar Carrier** / agg-lvl **Mortar PLT US** | LF | PEND | mortar platoon. See TYPE_GAP_ADJUDICATION.md GAP 3 (no mortar aggregate; tube-artillery proxy vs lone weapon vs aggregate-level). |
| UCFM COY | ho | 1 | 2MTR/7154 | ArmorCompany -> Tank Company (USA) | PEND GAP3 + Q1: FA Battery (USA) M109 3:11:1:225:4:8:0:0 (tube proxy) | HU | PEND | GAP 3 mortar company. |
| UCFR COY | ho | 1 | 4/7158 | ArmorCompany -> Tank Company (USA) | PEND GAP3 + Q1: FA Battery M109 (tube proxy) / lone **M270 MLRS** entity | HU | PEND | GAP 3 rocket/MLRS company; no rocket aggregate in chain. |
| UCFR BN | ho | 1 | 7913/HQ_71 | ArmorCoHQ -> Ground_Aggregate | PEND GAP3 + Decision-4 + Q1 | HU | PEND | rocket BN HQ: GAP 3 + CoHQ decision + Q1. |

### 2.7 Target acquisition (UCFT, UCFTR)

| class | side | n | members (sample) | today: emits -> resolves | PROPOSED template (objectType) | mv | disp | rationale / caveat |
|-------|------|---|------------------|--------------------------|--------------------------------|----|----|--------------------|
| UCFT NOS | fr | 1 | TA/HQ/4-27 | Tank (default) -> single M1A2 | **Fire Support Team (USA)** 3:11:1:225:12:27:0:1 (alt **COLT Team (USA)** 3:11:1:225:12:27:0:0) | HM | NEAR | fire-support/observer team; only target-acq content is a team; NOS ~ team. |
| UCFTR COY | ho | 1 | RDR/8072 | ArmorCompany -> Tank Company (USA) | as-today none real; **COLT Team (RUS)** 3:11:1:222:12:27:0:0 + Q1 mirror **COLT Team (USA)** 3:11:1:225:12:27:0:0 | HM | NEAR | radar/target-acq; only content is a team (echelon downgrade from company). Q1 pending. |
| UCFTR SECT | ho | 2 | 1/TA/7158, 2/TA/7158 | Tank (default) -> single M1A2 | **COLT Team (RUS)** 3:11:1:222:12:27:0:0 + Q1 **COLT Team (USA)** 3:11:1:225:12:27:0:0 | HM | NEAR | target-acq section ~ team. Q1 pending. |

### 2.8 Air defense (UCD)

| class | side | n | members (sample) | today: emits -> resolves | PROPOSED template (objectType) | mv | disp | rationale / caveat |
|-------|------|---|------------------|--------------------------|--------------------------------|----|----|--------------------|
| UCD PLT | ho | 5 | AD/7151, AD/7152, AD/7153 +2 | ArmorPlatoon -> Ground_Aggregate | **Air Defense Artillery Platoon (RUS)** 3:11:1:222:3:11:0:0 + Q1 mirror **(USA)** 3:11:1:225:3:11:0:0 | LF | NEAR | correct branch SHORAD (RUS=4x SA-9, USA=4x Avenger). Q1 pending. |
| UCD COY | ho | 4 | 1/7159, 2/7159, 3/7159, 1/8072 | ArmorCompany -> Tank Company (USA) | **Air Defense Artillery Platoon (RUS)** 3:11:1:222:3:11:0:0 + Q1 **(USA)** 3:11:1:225:3:11:0:0 | LF | NEAR | no ADA company aggregate; platoon proxy (echelon downgrade). Q1 pending. |
| UCD BN | ho | 2 | 7159/HQ, 8072/HQ | ArmorCoHQ -> Ground_Aggregate | PEND: Decision-4 + Q1 (branch alt ADA Platoon) | LF | PEND | air-defense BN HQ -> CoHQ decision + Q1. |
| UCD NOS | fr | 1 | A/6-56/HHC | Tank (default) -> single M1A2 | **Air Defense Artillery Platoon (USA)** 3:11:1:225:3:11:0:0 | LF | NEAR | SHORAD platoon (4x Avenger). (0.1.7 counted this unit under UCD-F; it is EchelonCode NOS - see note in sec 5.) |

### 2.9 Anti-armor (UCAA)

| class | side | n | members (sample) | today: emits -> resolves | PROPOSED template (objectType) | mv | disp | rationale / caveat |
|-------|------|---|------------------|--------------------------|--------------------------------|----|----|--------------------|
| UCAA PLT | ho | 7 | WPN/7151, WPN/7152, WPN/7153 +4 | ArmorPlatoon -> Ground_Aggregate | **Antitank Team (USA Army) Javelin** 3:11:1:225:14:12:0:0 | HM | NEAR | the ONLY anti-armor template on disk is a USA fire team, not a platoon: echelon AND country mismatch (no RUS AT, no AT platoon). Weapons-platoon proxy; flag as approximated. |

### 2.10 Combat service support / maintenance / ordnance / MI / CBRN / HHC (US, USX, USXO, UULM, UUAC, blank)

| class | side | n | members (sample) | today: emits -> resolves | PROPOSED template (objectType) | mv | disp | rationale / caveat |
|-------|------|---|------------------|--------------------------|--------------------------------|----|----|--------------------|
| US COY | fr | 9 | FSC/F/4-27, HQ/47, FSC/D/1-1 +6 | ArmorCompany -> Tank Company (USA) | **Combat Service Support Platoon (USA)** 3:11:1:225:3:31:0:0 (alt **Supply Section (USA)** 3:11:1:225:14:31:0:0) | LF | NEAR | forward support companies; CSS platoon/section (echelon downgrade from company). |
| US BN | fr | 3 | 47_CTCP, 47/2/1_AD, 47_REAR | ArmorCoHQ -> Ground_Aggregate | PEND: Decision-4 (branch alt CSS Platoon) | LF | PEND | support battalion HQ -> CoHQ decision. |
| USX COY | fr | 1 | B_FLD/47 | ArmorCompany -> Tank Company (USA) | **Combat Service Support Platoon (USA)** 3:11:1:225:3:31:0:0 | LF | NEAR | no maintenance-branch aggregate; CSS proxy. |
| USXO COY | fr | 1 | 756/HHC | ArmorCompany -> Tank Company (USA) | **Combat Service Support Platoon (USA)** 3:11:1:225:3:31:0:0 (alt generic company HQ) | LF | NEAR | no ordnance-branch aggregate; CSS/HQ proxy. |
| UULM COY | fr | 1 | 856/HHC | ArmorCompany -> Tank Company (USA) | **aggregate-Company-HQ-Friendly** 3:11:1:225:5:20:1:0 | LF | NEAR | no military-intelligence aggregate; generic company HQ (4 dismounts). |
| UUAC COY | fr | 1 | 369/HHC | ArmorCompany -> Tank Company (USA) | **aggregate-Company-HQ-Friendly** 3:11:1:225:5:20:1:0 | LF | NEAR | no chemical/CBRN aggregate; generic company HQ. |
| (no func) COY | fr | 2 | A/411/HHC, 303/HHC | ArmorCompany -> Tank Company (USA) | **aggregate-Company-HQ-Friendly** 3:11:1:225:5:20:1:0 | LF | NEAR | HHC with a blank function ID; generic company HQ. |

Note on the four HHC NEAR rows (UULM, UUAC, blank): they land on the SAME template as
Decision-4 Option A (aggregate-Company-HQ-Friendly). That is coincidental, not a vote on
Decision 4 - those units are COY-echelon, independent of the 26 BN-echelon ArmorCoHQ units the
decision governs. If the user rejects the generic-company-HQ altogether, these fall back to
"no branch template; approximate as staff company".

### 2.11 Aviation (UCV) - out of ground scope

| class | side | n | members (sample) | today: emits -> resolves | PROPOSED | mv | disp | rationale / caveat |
|-------|------|---|------------------|--------------------------|----------|----|----|--------------------|
| UCV BN | ho | 2 | UNK-AVN-1/, UNK-AVN-2/ | ArmorCoHQ -> Ground_Aggregate | OUT OF SCOPE (aviation) | - | AVN | SIDC battle-dim is 'G' so the port treats these as ground today (-> generic). Real mapping is an aviation battalion (0.1.3.d, air aggregate), out of the ground-force mapping scope. |

---

## 3. Golden STP init table (20 distinct classes, ground-relevant rows mapped)

Ground classes reuse the section-2 template proposals. Aviation/naval/data-quirk classes are
bucketed, not force-mapped.

| class | side | n | members (sample) | today: emits -> resolves | PROPOSED template (objectType) | mv | disp | rationale / caveat |
|-------|------|---|------------------|--------------------------|--------------------------------|----|----|--------------------|
| UCIZ PLT | fr | 21 | 1121.MechPlt, 1122.MechPlt +19 | ArmorPlatoon -> Ground_Aggregate | PEND GAP2: Mech Plt (USA) IFV (Dep) 3:11:1:225:3:4:0:0 / Stryker Rifle Plt / Tank Platoon (USA) | LF | PEND | mech-inf platoon; GAP 2. |
| UCIZ COY | fr | 13 (10 mapped) | 112.MechCoy, 113.MechCoy +8 | ArmorCompany -> Tank Company (USA) | PEND GAP2: aggregate-Co-Infantry-Friendly 3:11:1:225:5:3:1:0 / IFV candidates | HU | PEND | GAP 2. NOTE 3 of the 13 are "Diesel" units with DISDomain=3 (-> Boat today); those 3 are in the naval/quirk bucket, not here. |
| UCIZ BN | fr | 8 | 11.MechBn, 111.MechBnHq +6 | ArmorCoHQ -> Ground_Aggregate | PEND: Decision-4 CoHQ | LF | PEND | mech-inf battalions + BnHq. |
| UCIZ BDE | fr | 2 | 1.MechBde, 1.BdeHQ | Tank (default) -> single M1A2 | LONE M577A2 Command Post 1:1:1:225:3:11:0:0 | entity | LONE | brigade echelon; no brigade aggregate. |
| UCFH COY | fr | 3 | 151.ArtCoy, 152.ArtCoy, 153.ArtCoy | ArmorCompany -> Tank Company (USA) | **Field Artillery Battery (USA) M109** 3:11:1:225:4:8:0:0 | HU | NEAR | SP-howitzer battery (UCFH = SP how/gun). |
| UCFH BN | fr | 1 | 15.Artybn | ArmorCoHQ -> Ground_Aggregate | PEND: Decision-4 CoHQ | HU | PEND | artillery battalion HQ. |
| UCFM COY | fr | 2 (1 mapped) | 135.MortarCoy | ArmorCompany -> Tank Company (USA) | PEND GAP3: FA Battery M109 (tube proxy) | HU | PEND | mortar company; GAP 3. The 2nd member is a "Diesel" DISDomain=3 unit (naval/quirk bucket). |
| UCFM NOS | fr | 1 | 125.MortarCoy | Tank (default) -> single M1A2 | PEND GAP3: FA Platoon/Battery M109 (tube proxy) | LF/HU | PEND | mortar; GAP 3. |
| UCI COY | ho | 4 | Z1.InfCoy, Z2.InfCoy +2 | ArmorCompany -> Tank Company (USA) | **aggregate-Co-Infantry-Hostile** 3:11:1:222:5:3:1:0 + Q1 -Friendly 225 | HU | NEAR | real composed dismounted inf company; correct branch. Q1 pending. |
| UCI BN | ho | 1 | Z.Battalion | ArmorCoHQ -> Ground_Aggregate | PEND: Decision-4 + Q1 | LF | PEND | infantry battalion HQ. |
| UCI NOS | ho | 3 (2 mapped) | Z51.InfPlt, Z52.InfPlt | ArmorPlatoon -> Ground_Aggregate | **aggregate-Plt-Infantry-Hostile** 3:11:1:222:3:3:1:0 (alt Infantry Platoon (USA Army) 3:11:1:225:3:3:1:0) | LF | NEAR | hostile dismounted inf platoons (SIDC echelon 'D' despite EchelonCode NOS). 3rd member is a "Diesel" DISDomain=3 unit (naval/quirk). |
| MFQ NOS | fr | 6 | 1161.UAS, 1162.UAS +4 | air (index2 'A') -> AH-64/CH-47 | OUT OF SCOPE (UAS/aviation) | - | AVN | drone/UAS. |
| UCV BN | fr | 1 | 16.Aviation_Bn | ArmorCoHQ -> Ground_Aggregate | OUT OF SCOPE (aviation) | - | AVN | aviation battalion. |
| UCVRA NOS | fr | 2 | 161.AviationQRF, 162.AviationAttack | Tank (default) -> single M1A2 | OUT OF SCOPE (aviation) | - | AVN | attack/QRF aviation. |
| UCVRA NOS | ho | 1 | Z7.AviationAttack | Tank (default) -> single M1A2 | OUT OF SCOPE (aviation) | - | AVN | attack aviation. |
| UCVRUH NOS | fr | 1 | 163.AviationLift | Tank (default) -> single M1A2 | OUT OF SCOPE (aviation) | - | AVN | lift aviation. |
| CA NOS | fr | 4 | Diesel x4 | sea (index2 'S') -> Boat | OUT OF SCOPE (naval) | - | NAVAL | watercraft (all named "Diesel"). |
| CA NOS | ho | 4 | Z61-Z64.AmphibCraft | sea (index2 'S') -> Boat | OUT OF SCOPE (naval) | - | NAVAL | amphibious craft. |
| UCIN COY | fr | 1 | 181.AmphibiousCoy | DIS domain 3 -> Boat | OUT OF SCOPE (naval/amphibious) | - | NAVAL | amphibious infantry company; DISDomain=3. |
| UCIA COY | fr | 1 | Diesel | DIS domain 3 -> Boat | OUT OF SCOPE (naval/quirk) | - | NAVAL | ground SIDC but DISDomain=3 -> boat; "Diesel" data quirk. |

Golden DISDomain=3 data-quirk detail (the naval/quirk bucket = 15): 8 genuine watercraft
(SIDC index-2 'S': 4 friendly "Diesel", 4 hostile AmphibCraft) + 2 amphibious companies
(UCIN, UCIA) + 5 "Diesel" ground-SIDC units whose DISDomain=3 forces the Boat branch
(3 in UCIZ COY, 1 in UCFM COY, 1 in UCI NOS). These 5 are almost certainly bad init data,
not real naval units.

---

## 4. Templates verified on disk THIS pass

Path root: `C:\MAK\vrforces5.0.2\data\simulationModelSets\`. Each objectType and matchType
below was read directly from the named `.entity` file this pass (grep of the objectType=/
matchType= attributes). "cat*" = starred in ground truth 0.1.3.b/0.1.3.c (existence + type
re-confirmed); "unstarred" = not starred in the catalog (objectType AND matchType verified
from the file, per the Phase 2.1 brief).

EntityLevel/vrfSim/ aggregates (all superType 3 disaggregated units):

| template file | objectType (on disk) | matchType (on disk) | catalog |
|---------------|----------------------|---------------------|---------|
| Tank Company (USA).entity | 3:11:1:225:5:2:0:0 | 3:11:1:225:5:2:-1:-1 | cat* |
| Tank Company (RUS).entity | 3:11:1:222:5:2:0:0 | 3:11:1:222:5:2:-1:-1 | cat* |
| Tank Platoon (USA).entity | 3:11:1:225:3:2:0:0 | 3:11:1:225:3:2:-1:-1 | cat* |
| Tank Platoon (RUS).entity | 3:11:1:222:3:2:0:0 | 3:11:1:222:3:2:-1:-1 | cat* |
| aggregate-Co-Infantry-Friendly.entity | 3:11:1:225:5:3:1:0 | 3:11:1:225:5:3:1:0 (exact) | cat* |
| aggregate-Co-Infantry-Hostile.entity | 3:11:1:222:5:3:1:0 | 3:11:1:222:5:3:1:0 (exact) | cat* |
| Infantry Platoon (USA Army).entity | 3:11:1:225:3:3:1:0 | 3:11:1:225:3:3:1:0 (exact) | cat* |
| Recon Vehicle Platoon (RUS BMP2).entity | 3:11:1:222:3:6:0:49 | 3:11:1:222:3:6:0:49 (exact) | cat* |
| Field Artillery Battery (USA) M109.entity | 3:11:1:225:4:8:0:0 | 3:11:1:225:4:8:-1:-1 | cat* |
| Field Artillery Platoon (USA) M109.entity | 3:11:1:225:3:8:0:0 | 3:11:1:225:3:8:-1:-1 | cat* |
| Air Defense Artillery Platoon (USA).entity | 3:11:1:225:3:11:0:0 | 3:11:1:225:3:11:-1:-1 | cat* |
| Air Defense Artillery Platoon (RUS).entity | 3:11:1:222:3:11:0:0 | 3:11:1:222:3:11:-1:-1 | cat* |
| aggregate-Company-HQ-Friendly.entity | 3:11:1:225:5:20:1:0 | 3:11:1:225:5:20:1:0 (exact) | cat* |
| Tank Headquarters Section (USA).entity | 3:11:1:225:14:2:1:0 | 3:11:1:225:14:2:1:-1 | cat* |
| Tank Breach Company (USA).entity | 3:11:1:225:5:2:0:78 | 3:11:1:225:5:2:0:78 (exact) | cat* |
| aggregate-Company-HQ-Hostile.entity | 3:11:1:222:5:20:1:0 | 3:11:1:222:5:20:1:0 (exact) | unstarred (verified) |
| aggregate-Plt-Infantry-Hostile.entity | 3:11:1:222:3:3:1:0 | 3:11:1:222:3:3:1:0 (exact) | unstarred (verified) |
| Combat Service Support Platoon (USA).entity | 3:11:1:225:3:31:0:0 | 3:11:1:225:3:31:-1:-1 | unstarred (verified) |
| Supply Section (USA).entity | 3:11:1:225:14:31:0:0 | 3:11:1:225:14:31:0:0 (exact) | unstarred (verified) |
| Antitank Team (USA Army) Javelin.entity | 3:11:1:225:14:12:0:0 | 3:11:1:225:14:12:0:0 (exact) | unstarred (verified) |
| Fire Support Team (USA).entity | 3:11:1:225:12:27:0:1 | 3:11:1:225:12:27:0:1 (exact) | unstarred (verified) |
| COLT Team (USA).entity | 3:11:1:225:12:27:0:0 | 3:11:1:225:12:27:-1:-1 | unstarred (verified) |
| COLT Team (RUS).entity | 3:11:1:222:12:27:0:0 | 3:11:1:222:12:27:-1:-1 | unstarred (verified) |
| Mechanized Platoon (USA) IFV (Deprecated).entity | 3:11:1:225:3:4:0:0 | 3:11:1:225:3:4:0:0 (exact) | unstarred (verified) |
| Ground_Aggregate.entity (the generic fallback) | 3:11:1:0:0:0:0:0 | 3:11:1:-1:-1:-1:-1:-1 | unstarred (verified) |

EntityLevel/vrfSim/ platform entities (superType 1) used above:

| template file | objectType (on disk) | role |
|---------------|----------------------|------|
| M577A2_Command_Post.entity | 1:1:1:225:3:11:0:0 | LONE brigade-HQ proposal |
| M1A2_Abrams_MBT.entity | 1:1:1:225:1:1:3:0 | today's default Tank entity (baseline) |

Templates cited only as PENDING candidates from TYPE_GAP_ADJUDICATION.md (Stryker Rifle
Platoon, Mech Inf CO US, Mortar PLT US, MLRS BTY US, M270 MLRS, M1064 Mortar Carrier) were
already read from disk by E4 in that doc this cycle and are NOT re-verified here; the winner
is the user's call, so their exact objectType is not load-bearing for this table.

---

## 5. Reconciliation with 0.1.7 (what I confirmed and where I differ)

Confirmed exactly (tightened 0.1.7's "~" to exact counts): 64 COY -> Tank Company (USA),
correct for 13 (UCA COY 7 fr + 6 ho), wrong for 51; 49 (BN 26 + PLT 23) -> Ground_Aggregate;
15 (NOS 12 + SECT 2 + BDE 1) -> single M1A2. Function-x-echelon counts for UCA, UCIZ, UCE/UCEC,
UCFHE, UCFM, UCFR, UCFTR, UCF, US, UCRVA, UCR, UCAA, UCV, USX/USXO/UULM/UUAC all match 0.1.7.

Differences (minor, corrective):
1. 0.1.7 lists air defense as "UCD ... D(5)/E(4)/F(3)". Parsed reality: UCD PLT 5, COY 4,
   BN 2, NOS 1. The 12-unit total matches, but one UCD unit (A/6-56/HHC) is EchelonCode NOS
   (SIDC echelon char '-', so it becomes a lone M1A2 today), not an F/battalion. So UCD F is 2,
   not 3. Table sec 2.8 reflects this.
2. 0.1.7 folds two blank-function HHC (A/411/HHC, 303/HHC) and the UCFT NOS observer
   (TA/HQ/4-27) into narrative "HHC/HQ" / target-acq text; I broke them out as their own
   classes (sec 2.10, 2.7).
3. I did not contradict any 0.1.7 template recommendation; I extended them with the on-disk
   objectType/matchType and the movement class, and reproduced (did not resolve) the four
   TYPE_GAP_ADJUDICATION.md decisions.

---

## 6. Open questions carried forward

- The four adjudications in TYPE_GAP_ADJUDICATION.md (GAP1 engineer, GAP2 mech-inf, GAP3
  mortar/rocket, Decision-4 ArmorCoHQ) plus its four policy questions (Q1 country by side is
  the load-bearing one here) remain the user's calls; all PEND rows above point to them.
- The single friendly-recon gap (UCR NOS, sec 2.4): no composed USA recon/cavalry aggregate
  exists in the chain (only the RUS BMP-2 recon platoon). NOT in the adjudication doc; flagged
  here as a fifth soft content gap - armored proxy now, or author a US recon template later.
- Anti-armor (UCAA, sec 2.9): the only anti-armor content on disk is a USA Javelin fire team;
  mapping a hostile weapons PLATOON to it is an echelon+country approximation. Also a soft gap.
- Golden static-vs-live count (57 aggregates + 23 entities statically vs the reported "49 units
  + 4 areas" live): the app-level filtering that reconciles them was not traced this pass.
