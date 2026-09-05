# PREREG - F-DIVERGE: why does the COMPANY (higher-unit) move-along fail on 5.2?

Date 2026-09-05. Tier HEAVY (a cause/diagnosis claim about a broken tasking path). Written
BEFORE the probe run. Supersedes PREREG_R9_52's "next probe" note. Docs-first: this rests on
the F-DIVERGE research workflow (wf_52b70722: 5 read-only research angles + 2 adversarial
refuters, 2026-09-05) plus my own re-verification of the load-bearing facts.

## 0. The symptom (from run 20260905T214240Z_run, all facts re-verified from primary files)
On 5.2, one order tasked three units with MoveAlongRoute. The ENTITY (1.BdeHQ) and the
PLATOON (1222.MechPlt) advanced along their DUE-NORTH routes and completed. The COMPANY
114.MechCoy (Tank Company, USA) FAILED unit-wide: of 22 members, 18 drove exactly due SOUTH
(~412 m, bearing 180) to a backward-extended route start and STALLED; 2 ran away NNW
(e14e25d4 13.7 km brg ~331, a53993df 2.05 km brg ~327 - a53 went SOUTH with the pack first,
then REVERSED to ~331 mid-move); 0 advanced north; centroid moved 304 m WNW. Route T_R5_CO1
is due north (constant lon -116.693388). 331 deg = the north route rotated ~29 deg CCW. The
two divergers were still moving at window close, so all-subordinates completion never fired.

## 1. What is VERIFIED vs ASSUMED (do not blur)
VERIFIED (re-read this session and/or by refuters from primary files):
- Company vs Platoon use DISTINCT controller wiring: Company move-along-controller =
  `aggregate-move-along-controller`, NO adapter, NO maneuver-along-controller, NO
  isUnitMovementExhausted fail-safe (ground-higherUnit-disaggregated-movement.sysdef:177-203);
  Platoon = `aggregate-move-along-adapter-controller` + `aggregate-maneuver-along-controller`
  (ground-disaggregated-movement.sysdef:176-221). Only the Company failed.
- 114.MechCoy maps to "Tank Company (USA)" EXACT composed/nested template
  (unit-type-map-52.json F-UCA-E) - a true higher echelon that takes the distinct path.
- Route due north; both divergers share ~329-331 deg (a common rotation, computed from the
  raw trace by both refuters); timeMult=1 (vrfc2simapp.log:37).
- STACKED birth: 114.MechCoy + siblings 1141/1142/1143 born at ONE identical point
  (34.64763,-116.69339, terrain 1260.1; vrfc2simapp.log:41-53), DeStackCreates OFF; the
  completing 1222.MechPlt born ALONE (line 43). VrfBridge.cpp:304-316 creates every aggregate
  Disaggregated with createSubordinates=true.
- B1 SET+REORGANIZE fired at creation before the move (vrfc2simapp.log:87 then :119).
- current='' formation reads on BOTH the completing platoon and the failing company, pre- and
  post-move - a QUERY ARTIFACT, distinguishes nothing (refuter-verified).
ASSUMED (supported, not proven): e14e25d4/a53993df are 114.MechCoy members (by elimination -
they are movers in the MechCoy birth cluster while every untasked-platoon object is static;
no explicit aggregate->member map in the run manifest). The .cpp bodies
(extendRouteStart/clipRouteEnd/buildOffsetRoute, the 13.7 km magnitude) are NOT shipped, so
the offset-construction mechanism is inferred from headers, not read.

## 2. Ranked causes (from the workflow; rank1/rank2 are the live contest)
R1 (substrate, VERIFIED): the Company no-fail-safe move-along path. Correctly explains
   NON-COMPLETION (still-moving runaway + no backstop + all-subordinates rule). Both refuters:
   this SURVIVES as the substrate but is WEAKENED as THE cause of the DIVERGENCE.
R2 (divergence cause, BETTER-SUPPORTED): per-subordinate offset/working routes built against
   an UNRESOLVED/TRANSIENT aggregate formation-ORIENTATION at task time -> body-frame offsets
   rotated ~29 deg CCW to a common NNW bearing. Accounts for (a) the identical ~29 deg
   rotation of both divergers and (b) a53's south-then-reverse mid-move destination change (a
   re-resolution signature). Likely TRIGGER: the stacked zero-spread co-located birth.
R3 autonomy/obstacle detour - FALSIFIED (straight monotonic wrong-bearing, not curving/
   rejoining; 18 others on identical terrain did not wander).
R4 single-member pathfinding - REFUTED (2 correlated + 18 no-advance is not isolated).
R5 our route authoring/geometry - FALSIFIED (platoon+entity used identically-authored routes
   and completed; 13.7 km >> our 1.1 km northernmost vertex).

## 3. The decisive probe (MEASURE_ONLY - change nothing that affects behaviour)
GOAL: (a) DETERMINISM - re-run the identical R5 order 2-3x, byte-identical init/order/config/
posture, and read whether the SAME members diverge on the SAME bearings (deterministic
Company-path geometry -> R1) or diverger identity/direction varies or a run completes (race
-> R2). This is the single observation both refuters name and needs NO instrument change.
(b) ROUTE VERTICES (if cheaply available) - determine whether the backend already logs the
per-subordinate working/offset routes at the harvested notify level; if not, RECORD that the
observable is missing (needs a higher notify level or publishFormationRoutes) and do NOT
change it inside this probe - that is a separate instrument decision.
PROCEDURE: same runner invocation as run 20260905T214240Z (RunC2SimScenario.ps1 -VrfProfile
5.2 -NoGui -Scenario R9_Mojave_Empty_52, Vrf__AggregateFormation=auto, -RunSecs 360), 3x
sequentially; harvest each watchvrf-trace.csv + vrfc2simapp.log + the backend log to
runs/launch52. Score each with movement_check.py PLUS a per-member bearing/onset analysis for
the MechCoy cluster (reuse the workflow's method). The runner's stage-2c + oracle gate guard
the RTI-wedge risk (these are the 3rd-5th launcher-fixtures on the preserved rtiexec; a wedge
fails FATAL before scoring, which is itself a datum).

## 4. Predictions - each names its falsifier
D1 DETERMINISM (this is the crux). Prediction (lean R2, MEDIUM): across 3 repeats the
   diverger IDENTITY and/or bearing will VARY, or at least one repeat will complete cleanly.
   Falsifier: all 3 repeats fail with the SAME ~2 members diverging on the SAME ~331 deg and
   18 due-south-stall -> deterministic Company-path geometry (R1), not a race.
D2 COMPANY-ONLY (HIGH): every repeat completes 1.BdeHQ + 1222.MechPlt and fails 114.MechCoy.
   Falsifier: the platoon or entity ever fails, or the company ever completes -> the
   company/echelon framing is wrong.
D3 SPEED ANOMALY (record): every diverger sustains > M1A2 governed off-road speed at
   timeMult=1. Falsifier: divergers cap at the governed speed -> the >35 m/s was a one-run
   artifact. (Open regardless of R1/R2 - carry it.)
D4 ROUTE-VERTEX OBSERVABILITY (record): the harvested backend log does NOT contain the
   per-subordinate working-route vertices at the current notify level. Falsifier: it does ->
   read them and classify the divergers' target bearings directly (that would settle R1 vs R2
   outright).
SUCCESS of THIS probe = D1 answered (deterministic vs race) AND D2 holds. It DIAGNOSES; it
does not fix.

## 5. Candidate FIXES (RECORD ONLY - decided AFTER the diagnosis, never before)
Do not implement any of these until section 4 settles R1 vs R2:
- If R2 (orientation race / stacked birth): gate the unit move on a SETTLED reorganize (not
  just the name->uuid map), and/or turn DeStackCreates ON so the company + siblings are not
  born zero-spread at one point.
- If R1 (deterministic Company-path geometry): bypass the higher-unit path - task the company
  as per-entity MoveAlongRoute-by-uuid over its subordinates, or set startAtClosest, or task
  at platoon echelon. (Company-level tasking may simply be unsupported for reliable move on
  this build - a real capability limit to surface, not paper over.)

## 6. Result
(filled after the probe)
