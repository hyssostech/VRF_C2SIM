# RUNAWAY / WARP / PILE CENSUS (Phase 2.4 offline forensics) - 2026-07-17

Groundwork plan docs/VRF_GROUNDWORK_PLAN.md Phase 2.4 ("Offline forensics debt, now
purposeful: runaway/warp census from the archived 3450/3453/3454 CSVs ... per-taskee
census of CPP-ALT-1"). Offline only; no live processes. All analysis by one-off Python
scripts kept OUT of the repo. ASCII only.

Movement oracle: WatchVrf POS telemetry displacement (the standing rule). App logs
(app3449 port / app3452 C++) supply the uuid -> name -> template-class -> task join;
data/COA-STP1_Initialization.xml and data/COA-STP1_Order.xml supply init positions and
route-end coordinates.

## 0. The three archived runs (what they are)

| run (WatchVrf) | interface | scenario | TimeMult | window | sample | note |
|---|---|---|---|---|---|---|
| 3450 COA-DEMO-1 | PORT (.NET, altitude-fix) | COA-STP1, TropicTortoise/Mojave | **20x** | 600 s wall | 15 s wall | de-stack ON |
| 3453 CPP-ALT-1  | pristine C++ (birth alt 1000->10000) | COA-STP1, TropicTortoise | **1x** | 600 s wall | 15 s wall | no de-stack (pile stays) |
| 3454 CPP-ALT-1 end | pristine C++ (same run as 3453) | " | **1x** | 300 s wall | 15 s wall | end-state window of the 3453 run |

3453 and 3454 are TWO WINDOWS OF ONE C++ RUN (same app 3452, same VRF session, same
uuids); this census MERGES them into one continuous track per object. 3450 is a single
window. COA-STP1 has 128 units incl. a 54-unit mega-pile at 34.679985,-116.724799.

## 1. CRITERIA (stated numerically BEFORE computing) and clock calibration

Distances are haversine (R=6,371,000 m). For each object, from its POS samples:
`net` (first->last), `max_from_start` (max over samples of distance to first sample),
`total_path` (sum of consecutive segments), `max_step_speed` (max inter-sample implied
ground speed, sim-time corrected), `final_alt`.

CLOCK CALIBRATION IS THE CRUX. Implied speed is computed on SIM time, not wall time:
`sim_dt = wall_dt * TimeMultiplier`, `speed_kmh = (segment_km) / (sim_dt/3600)`. Because
sim time is what advances the movement model, a legitimate mover shows its TRUE ground
speed at BOTH multipliers, and every threshold below is multiplier-independent.

| quantity | at 20x (3450) | at 1x (3453/3454) |
|---|---|---|
| sim time per 15 s wall sample | 300 s | 15 s |
| distance a legit 30 km/h mover covers per sample | 2.50 km | 0.125 km |
| distance a legit 70 km/h mover covers per sample | 5.83 km | 0.29 km |
| that 70 km/h mover's IMPLIED speed (sim-corrected) | 70 km/h | 70 km/h |

Classification (precedence order top-down):

1. **stationary-never-moved**: `max_from_start < 50 m`.
2. **warp**: `max_step_speed >= 200 km/h` (>=1 inter-sample jump implying non-physical
   ground speed). 200 km/h is ~3x any tracked-vehicle top speed (~70 km/h) and, being a
   SPEED, cannot be tripped by a legitimate mover at either 1x or 20x.
3. **runaway**: not warp, and `max_from_start >= 50 km` (past the 45.4 km FARTHEST route
   end in the whole order - so a legitimate arrival at any COA-STP1 route cannot land
   here).
4. **stalled/moved-stopped**: 1-50 km, no warp, halted (last segment < 5 km/h).
5. **drifter**: 50 m - 50 km, no warp, not clearly halted.
6. **arrived**: reserved for a mover that HALTED at its route end (route-end distance from
   final position <~ a few km). Certifying "arrived" offline needs the unit's route
   geometry; see the stop-radius and per-taskee sections for why this bucket is EMPTY in
   both runs (it is a real zero, not a threshold artifact - matches the investigation's
   parts 15/16: zero certified arrivals in either run).

Degenerate samples (lat=90/lon=-90 sentinel, 0/0) are dropped; an object needs >=2 valid
samples to be classified.

## 2. Join coverage (reported honestly)

| join | port 3450 | C++ 3453/3454 |
|---|---|---|
| distinct uuids in POS stream | 1790 | 1794 |
| valid (>=2 non-degenerate samples) | 1738 | 1742 |
| app-log NAMED objects (created aggregates/entities) | 113 aggregates | 128 (all) |
| -> named coverage of the full POS stream | ~6% | ~7% |
| dispatched taskee uuids present in POS | 9/9 (all, 39 samples ea.) | 8/8 (all, 40 samples ea.) |
| explicit template CLASS per created unit in app log | NONE | 128/128 |

The ~1660 unnamed objects per run are internal MEMBER/subordinate entities VR-Forces
creates on disaggregation; their uuids are not in any app log and cannot be mapped to a
parent offline. Named coverage of the full stream is therefore low (~7%), but coverage of
the objects this census is about - the tasked/dispatched units - is 100%. The C++ log
(app3452) tags every created unit with its resolved template class (ArmorCoHQ /
ArmorCompany / ArmorPlatoon / TANK); the PORT log (app3449) carries NO class string, so
port units are class-tagged by NAME-transfer from the C++ resolution (same COA-STP1
dataset, same deterministic SIDC/echelon dispatch) - an assumption, flagged in Limitations.

Join validated against the investigation doc's independently-derived numbers (see sec 8):
the parse reproduces the part-15 "38 movers", "541 km top / 289 km net" excursion, and the
"-1305 / -1680 m" underground terminations to the reported precision.

## 3. Per-run census (compressed; identical classes grouped, exceptions named)

### 3450 COA-DEMO-1 (PORT, 20x) - 1738 valid objects

| class | count | who |
|---|---|---|
| stationary-never-moved | 1700 | the pile + all untasked units and their members |
| warp | 24 | 3 tasked LF aggregates (1-35, 1-6, 40) + 21 member entities |
| moved-stopped (1-50 km) | 12 | member entities that halted mid-field |
| runaway (>=50 km, no warp) | 1 | one member entity (sustained fast travel, no single teleport) |
| drifter | 1 | C/1-35 aggregate (drifted 15.1 km, alt 739 m) |
| arrived | 0 | (real zero) |

38 movers total (= 24+12+1+1) - reproduces investigation part 15 exactly. Worst object
(member 8a059ed3): cumulative path 1104 km, net 288 km, peak 539 km from start, altitude
spiking to 6356 m mid-warp. Tasked LF aggregates 1-6 and 40 terminated UNDERGROUND at
-1681 m and -1306 m.

### 3453+3454 CPP-ALT-1 (pristine C++, 1x, merged) - 1742 valid objects

| class | count | who |
|---|---|---|
| stationary-never-moved | 1727 | the pile + all untasked/mute units and members |
| warp | 12 | member entities of the 3 marching LF aggregates (out-and-back teleports) |
| moved-stopped | 1 | 1-35/2/1_A aggregate icon (halted 18.33 km out) |
| drifter | 2 | 1-6/2/1_A + 40/2/1_AD aggregate icons (halted ~18.4 km, last step just >5 km/h) |
| runaway | 0 | (real zero at 1x - see note) |
| arrived | 0 | (real zero) |

15 movers total. All 15 belong to just THREE tasked units: the LF-class ArmorCoHQ
aggregates 1-35/2/1_A, 1-6/2/1_AD, 40/2/1_AD (their icons) plus ~12 of their member
entities. Every other tasked unit (all ArmorCompany and both TANK entities) is stationary
at the pile. Note the "runaway (>=50 km, no warp)" bucket is empty at 1x because in a
600/300 s window 50 km demands an average > 240 km/h, which trips the warp test first -
this zero is expected, not a miss.

## 4. Stop-radius: is the 18.1-18.4 km band a real cluster?

Final distance FROM THE PILE (birth origin), movers only, MERGED C++ run:

```
 final-from-pile   count
   10-11 km          6      first (nearer) stall cluster
   12-13 km          1
   18-19 km          8      <-- the 18.1-18.4 km band
   19-50 km          0      NOTHING stops between the band and the runaway regime
```

Exactly 6 objects land in 18.13-18.37 km (values 18.13, 18.20, 18.20, 18.33, 18.35,
18.37); 8 in 17.5-19.0 km (adds 18.42, 18.43). The band is a TIGHT, REAL cluster and it is
TERMINAL - no mover halts between 19 km and the 50 km runaway floor. **VERDICT: the
18.1-18.4 km band is a real cluster (6 exact / 8 wide), and it is absent in units that
went further because there ARE none between 19 and 50 km.** There is also a second, nearer
stall cluster at ~10.5 km (6-7 member entities). The band members are the 3 LF ArmorCoHQ
aggregate icons (18.33/18.42/18.37 km) plus their marching subordinates.

These stalls are 10-17 km SHORT of the ordered route ends (from data/COA-STP1_Order.xml,
last route vertex; T1 end validated at 34.570,-117.006 = the investigation's documented T1
end): T1 (1-35) route end 28.5 km -> stalled 10.2 km short; T19 (40) end 28.5 km -> 10.1
km short; T15 (1-6) end 35.5 km -> 17.1 km short. So the band units are STALLED-MID-ROUTE,
not arrived - consistent with investigation part 16.

**In the PORT run (3450, 20x) the band is ABSENT.** Its movers do not stall at 18 km; the
LF aggregates warp PAST it to 41-83 km from the pile and terminate underground/offshore,
and the one HU company that moved (C/1-35) drifted to 15 km. At 20x the marchers outrun the
stall into the warp regime. (A larger paged area / terrain page-in area is the first thing
to try live, per ground-truth 0.2 subject 6: the stop-radius and the runaway terminations
share the "outran the paged terrain" candidate mechanism, still undocumented by MAK.)

## 5. Controller-class split test (the pre-registered hypothesis, ground truth 0.0 item 2)

Template class comes straight from the C++ app3452 log. Controller wiring per the 0.1.3b
catalog: HQ-section and platoon templates wire the **LF** (aggregate-lead-follow-in-
formation) controller whose members are ENTITIES that plot and march offset routes;
company templates wire the **HU** (aggregate-move-along) controller whose members are
sub-UNITS. So ArmorCoHQ=LF, ArmorPlatoon=LF, ArmorCompany=HU, TANK=lone entity.

Movement classification of CPP-ALT-1 TASKED units, by class (all 11 started AT the pile,
first_from_pile = 0.00 km, so the pile is not the discriminator - some escaped, some did not):

| class (controller) | tasked | MOVED | MUTE | units |
|---|---|---|---|---|
| ArmorCoHQ (LF, lead-follow) | 5 | **3** | 2 | moved: 1-35, 1-6, 40; mute: 4-27, 5-20 |
| ArmorCompany (HU, move-along) | 4 | **0** | 4 | mute: 510, 856, B/5-20, C/1-35 |
| TANK (lone entity) | 2 | **0** | 2 | mute: 1-1 (the known frozen entity), A/6-56 |
| ArmorPlatoon (LF) | 0 | - | - | NONE tasked in COA-STP1 |

**VERDICT: SUPPORTS (directionally).** Every mover is LF-class; not one HU-class unit and
not one lone entity moved. The mover/mute split lands exactly on the LF/HU controller
boundary. The same three LF ArmorCoHQ units are also the movers in the PORT run (3450):
they warp rather than march there, but the SAME unit set moves and the SAME HU/entity set
stays mute - so the split is code-independent.

Falsification checks run (none overturned the verdict, but they bound it):
- Competing hypothesis "it is dispatch order / luck, not class": restricting to the 7
  DISPATCHED C++ units, the split still holds - dispatched LF 3/4 moved, dispatched
  HU+entity 0/3 moved. Dispatch alone does not explain it.
- ECHELON is perfectly confounded with controller here: ArmorCoHQ = SIDC echelon F,
  ArmorCompany = echelon E. Offline data CANNOT separate "controller class" from "echelon".
  The de-confounding probe named in ground truth 0.0 item 2 refinement (same-echelon Tank
  Platoon (LF) vs Stryker Rifle Platoon (HU), tasked identically) remains the required live
  test; this census cannot substitute for it.
- Not deterministic, and LF is not monolithic: 2 of 5 LF units (4-27, 5-20) stayed mute
  despite LF class, so LF is NECESSARY-BUT-NOT-SUFFICIENT for movement in this data. (The
  catalog also notes the empty-offset-route freeze symptom lives on the LF controller - an
  LF platoon froze that way in R9 - so "LF-class" predicts neither guaranteed march nor
  guaranteed freeze; success appears to also need a lead subordinate established.)
- The platoon arm of the hypothesis is UNTESTED: COA-STP1 tasks no platoon-class unit.
- Template-class -> controller mapping (ArmorCoHQ -> HQ-section -> LF) is inferred from the
  catalog by name, not from the C++ resolver source.

## 6. Warp anatomy

| | port 3450 (20x) | C++ 3453 (1x) | C++ 3454 (1x) |
|---|---|---|---|
| objects classed warp | 24 | 12 | 9 |
| biggest single-step jump | 339 km @ 4074 km/h | 93.6 km @ 22472 km/h | (similar) |
| max DISTINCT objects warping in ONE 15 s step | **15 (at t=78 s)** | **12 (at t=78 s)** | 9 |
| distinct objects warping simultaneously, sustained | 9-12 at many steps | 9 at many steps | 8-9 at many steps |
| altitude behaviour | spikes to 6356 m, terminates underground (-1306, -1681 m) & offshore | out-and-back, returns to 10-18 km, on-terrain | same |

**Warps CLUSTER IN TIME.** Up to 15 distinct objects (port) / 12 (C++) jump in the SAME
single 15 s sample step, and 9-12 distinct objects warp together at many steps across the
run. Simultaneous multi-object teleports are the FRAME-STALL signature (ground truth 0.2
subject 8 hypothesis): a stalled integration frame advances a large sim-time step in which
every moving object's dead-reckoned position leaps at once. This clustering is present at
BOTH clocks (a 93.6 km jump in 15 s at 1x = 22,472 km/h is a genuine teleport, not fast
driving), so warp is NOT unique to 20x and NOT a per-unit defect.

What DIFFERS by clock is magnitude and destructiveness: the port's 20x warps reach 339-539
km, fling altitude to 6+ km, and terminate the tasked LF aggregates permanently underground
(-1306, -1681 m); the C++ 1x warps are out-and-back excursions (54-93 km) that RETURN to
the 10-18 km stall clusters on-terrain. Warps hit mostly the MEMBER entities (unnamed); the
C++ LF-CoHQ aggregate ICONS themselves march cleanly to the 18.4 km stall without a warp.

## 7. Tasked-vs-moved cross-tab (CPP-ALT-1 per-taskee census; the pile split)

All 11 tasked units START co-located at the 54-unit mega-pile. Among these pile members:

| unit | class | dispatched (moveAlongRoute)? | moved? | end state |
|---|---|---|---|---|
| 1-35/2/1_A | ArmorCoHQ (LF) | YES | **YES** | icon+members marched, stalled 18.3 km (10.2 km short) |
| 1-6/2/1_AD | ArmorCoHQ (LF) | YES | **YES** | marched, stalled 18.4 km (17.1 km short) |
| 40/2/1_AD | ArmorCoHQ (LF) | YES | **YES** | marched, stalled 18.4 km (10.1 km short) |
| 4-27/2/1_A | ArmorCoHQ (LF) | YES | no | frozen at pile |
| 1-1/2/1_AD | TANK entity | YES | no | frozen at pile (the known frozen entity) |
| 856/HHC | ArmorCompany (HU) | YES | no | frozen at pile |
| C/1-35 | ArmorCompany (HU) | YES | no | frozen at pile |
| 5-20/2/1_A | ArmorCoHQ (LF) | no (predecessor-gated) | no | frozen at pile |
| 510/40 | ArmorCompany (HU) | no | no | frozen at pile |
| A/6-56/HHC | TANK entity | no | no | frozen at pile |
| B/5-20 | ArmorCompany (HU) | no | no | frozen at pile |

Headline: **11 tasked, 7 dispatched, 3 moved.** All 3 movers are LF ArmorCoHQ; the
15 movers of sec 3 are these 3 aggregate icons + ~12 member entities that co-cluster on the
LF routes (member->parent attribution INFERRED, not hard-joined - see Limitations 1). ZERO
HU-company and ZERO entity units moved - icon or member. The pile mover/frozen
split is: the LF-class HQ aggregates escaped and marched (then stalled at 18.4 km); every
HU-class company and every lone tank stayed frozen in the pile.

PORT (3450) counterpart: 9 dispatched, 4 moved - the same 3 LF ArmorCoHQ (warped 41-83 km,
2 underground) plus one HU company (C/1-35) that drifted 15 km. The only cross-run
difference is C/1-35 drifting a little at 20x vs frozen at 1x; the LF/HU/entity split is
otherwise identical across codebases.

## 8. Adversarial review (what it caught, and the fixes)

- **Window-split artifact (major).** First pass analyzed 3453 and 3454 separately and
  measured displacement from each window's FIRST sample. That HID the 18.4 km band (0
  objects in band) and made the marching LF aggregates look like they barely moved (~0.9
  km). Cause: 3453/3454 are two windows of ONE run with a gap; the march straddles the gap.
  FIX: merge the windows and measure distance from the PILE/birth. The band then appears (6
  exact) and the LF aggregates show their true 18.4 km march. All sec 3-7 numbers use the
  merged/pile-referenced metric.
- **Controller verdict nearly inverted.** The raw split (CoHQ moved, Company mute) first
  read as CONTRADICTS ("both are company-ish"). Checking the 0.1.3b catalog wiring
  (HQ-section = LF, company = HU) flipped it to SUPPORTS. Falsification gate then run: the
  "dispatch/luck" competing hypothesis was checked and rejected (split holds within
  dispatched units); the echelon confound was found and is reported as an unresolved
  limitation, not papered over.
- **Threshold defensibility at both clocks.** Warp is a SPEED test (200 km/h, sim-time
  corrected), so a legit 20x mover (max ~70 km/h implied) cannot trip it - verified by the
  calibration table. RUNAWAY_KM raised from 40 to 50 km after computing the farthest route
  end (45.4 km) so a legitimate arrival at the longest route is not mislabeled runaway.
- **Zero-member buckets checked.** "arrived" = 0 in both runs is REAL (matches investigation
  parts 15/16: zero certified arrivals). "runaway (no warp)" = 0 in the C++ run is REAL
  (50 km in a 600 s 1x window would require >240 km/h, which trips warp first).
- **Join verified against independent numbers.** The uuid->name join reproduces the
  investigation's separately-derived figures: 38 movers, 539 km/288 km top excursion,
  -1306/-1681 m underground terminations. (The investigation's example "1-6/2/1_AD drove
  53.8 km" is from the 2026-07-13 F1 scale run, a DIFFERENT app not in this dataset; the
  reproducible in-dataset cross-checks above stand in for it. In THIS dataset 1-6/2/1_AD
  marches 18.4 km at 1x and warps 60.6 km underground at 20x.)

Adversarial review note: the strongest competing account of the mover/mute split is
"echelon, not controller". It is not refuted - it is CONFOUNDED with controller in this
data and cannot be separated offline. The verdict is therefore "supports the controller
hypothesis" only in the weak sense that the data is consistent with it and inconsistent
with class being irrelevant; the echelon-vs-controller de-confounding still requires the
live same-echelon LF-vs-HU probe.

## 9. Limitations

1. **Member->parent unmappable offline.** ~93% of POS objects are unnamed member entities
   with VRF-assigned uuids absent from all app logs; "which aggregate did this member
   belong to" cannot be resolved. Member-mover attributions in sec 3/7 are inferred from
   co-clustering with the LF routes, not from a hard join.
2. **Intra-window blind spot.** 15 s WALL samples at 20x are 300 s SIM windows; any motion
   inside a window - a warp out and back, an arrival then departure - is invisible. A unit
   could complete and abandon a route between two 3450 samples and read as stationary.
3. **3453/3454 gap.** The real elapsed time between the two C++ windows is unknown (assumed
   ~600 s). This affects ONLY the single cross-window segment's implied speed, not any
   position-based metric (max_from_start, final_from_pile, classification).
4. **Echelon/controller confound** (sec 5) - unseparable offline.
5. **Port class-by-name-transfer** - port units carry no class string; class assigned from
   the C++ resolution of the same-named unit. Robust for the shared dataset but an
   assumption.
6. **Route ends** taken as the last order Location per task (the interface generates 5-pt
   routes; the order gives the objective vertices). Validated on T1 (34.570,-117.006) vs
   the investigation's documented T1 end; "arrived vs stalled" uses route-end distance as a
   proxy and cannot certify arrival at a tolerance the order does not specify.
7. **Single-scenario, single-terrain.** Both runs are COA-STP1 at Mojave/TropicTortoise;
   nothing here generalizes to other datasets/terrains without re-running the census.

## 10. Headlines for the supervisor

- Per-run movers: **port 3450 = 38** (24 warp, 12 moved-stopped, 1 runaway, 1 drifter);
  **C++ 3453+3454 = 15** (12 warp, 3 marching LF-CoHQ aggregate icons). "arrived" = 0 in
  both (real).
- Controller-class split: **SUPPORTS (directionally).** LF 3/5 moved, HU 0/4 moved, entity
  0/2 moved; all movers LF-class; reproduces across both codebases. Confounded with echelon;
  2/5 LF units mute; no platoon tasked - the live de-confounding probe is still required.
- 18.1-18.4 km band: **real, tight cluster** - 6 objects exact (18.13-18.37 km), 8 in the
  wide band, nothing stopped 19-50 km. Present in C++ (1x), ABSENT in port (20x movers warp
  past it underground).
- Warp anatomy: **clustered in time** - up to 15 (port) / 12 (C++) distinct objects warp in
  ONE 15 s step; frame-stall signature; present at BOTH clocks; 20x makes warps far larger
  (up to 539 km) and terminal-underground vs the 1x out-and-back that returns on-terrain.
- Tasked-vs-moved (C++): **11 tasked, 7 dispatched, 3 moved** - the 3 LF ArmorCoHQ escaped
  the pile; every HU company and every lone tank stayed frozen in it.
- Join coverage: dispatched taskees **100%** joined (9/9 port, 8/8 C++); named coverage of
  the full POS stream **~7%**; C++ class tags **128/128**, port class by name-transfer.

---

## 11. SUPERVISOR GATE ADDENDUM (2026-07-17) - warp reinterpretation

Written at the acceptance gate after an independent re-analysis of the raw jump events
(destination-clustering script over 3453 and 3450; E7's numbers above are unchanged and
reproduce). Two observations the census did not surface:

1. **Warps are LOCKSTEP GROUP events.** The jump events are not independent per-object
   teleports: groups of co-located member entities jump in the SAME sample step with
   near-identical from- AND to-coordinates (e.g. 3453 t=499.1->514.2: six distinct uuids
   all jump ~58.3 km from ~(34.628,-116.880) to ~(34.825,-116.288); 3450 t=243.5/258.6:
   three uuids jump together twice). One displacement vector applied to a co-moving
   formation, ping-ponging NE then SW far beyond the route geometry, then RETURNING to
   the coherent marching track.
2. **The frame-stall mechanism does not survive arithmetic at 1x.** A stalled frame
   advances sim time, and dead-reckoning a ~70 km/h vehicle over even a 300 s stalled
   step yields <= ~6 km of leap - not 93.6 km in a 15 s 1x sample. The census's
   "frame-stall signature" explains the SIMULTANEITY but not the MAGNITUDE.

COMPETING HYPOTHESIS (registered, not settled - now the leading candidate for the
TRANSIENT warps): **observer-side dead-reckoning artifact.** WatchVrf reads VR-Link
REFLECTED positions, which are DR-extrapolated from the last received entity state at
read time. A corrupted / wrong-frame / thrashing velocity vector in the published member
state would extrapolate to enormous straight-line excursions (altitude leaving the
terrain surface - matching the observed 3417-6356 m spikes mid-excursion) that snap back
on each real state update - coherently across a formation whose members share the same
velocity state. On this reading the transient warps are an OBSERVATION-LAYER artifact,
not backend motion.

DECOMPOSITION that follows (replaces the single "warp" reading of sec 6):
- **Transient out-and-back mega-jumps** (both clocks, lockstep groups, altitude spikes,
  snap-back to the marching track): DR/reflection-artifact CANDIDATE. Member-entity warp
  classifications in sec 3 are therefore observation-suspect; member telemetry should not
  be trusted for warp claims until the discriminator below runs.
- **Persistent displaced end-states** (the port 20x LF aggregates sitting at -1306/-1681 m
  underground, 41-83 km out, constant across endstate samples): NOT explainable as
  transient DR overshoot - a persistent position IS the reflected state. This is the real
  runaway/termination class, and it remains port-20x-specific in this data.
- UNAFFECTED by this addendum: the controller-split verdict (keyed on aggregate icons +
  taskee displacement), the 18.1-18.4 km stall band (persistent final positions), the
  tasked-vs-moved cross-tab, and arrived=0.

LIVE DISCRIMINATOR (queued as a WatchVrf enhancement candidate): log the RAW last-received
entity state (position + timestamp + velocity) alongside the DR-extrapolated read for a
few member uuids. DR-artifact predicts raw positions stay sane while DR reads excurse;
backend-real predicts both agree. Also relevant: GT 0.2 sec 8 documents DR mismatch for
cross-federate reads around fast-than-real-time operation.
