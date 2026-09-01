# PREREG P3R: IDENTICAL repeat of P3 (TimeMultiplier 5) - registered 2026-09-01, BEFORE running

Source: docs/experiments/REVIEW_P3_TIMEMULT5_2026-09-01.md sec 4 ("Recommended ONE next
probe: P3-REPEAT") and docs/HANDOFF_2026-09-01_R9_COMPLETE.md NEXT 5c. Coordinator brief
2026-09-01. ASCII only. The C++ repo (c2simVRFinterfacev2.36) is a frozen oracle.

## 1. The variable: NONE (this is a repeat)

Identical to P3 (docs/experiments/PREREG_P3_TIMEMULT5_2026-09-01.md, run
20260901T221227Z): `Vrf__TimeMultiplier=5`, init data/R9_Mojave_Lean_Initialization_
NoComments.xml, order data/R9_Mojave_UnitMove_Order_NoComments.xml, -RunSecs 420
(watch 980 s), untouched product at defaults, no template/env/mtl edits. Baseline for
endpoints and completion points remains P2c (20260901T211310Z, 1x, 3/3, 17/17 followers).
Purpose: split REVIEW_P3's h1 from h2 with a second sample at 5x (P3 is n=1).

## 2. Docs consulted (all cited in REVIEW_P3 sec 2; the four load-bearing ones re-read)

- C:/MAK/vrforces5.0.2/doc/help/Content/.../Introduction/Concepts/
  vrf_runFasterThanRealTime.htm: faster-than-real-time = a multiplier applied to the
  tick time (sim time PER TICK scales; no extra frames); default clock mode Variable-
  Frame Run-To-Complete "does not provide repeatable results"; high multiples "can cause
  performance of simulation object models to degrade". Install: clockMode 0 (variable
  frame), fastForwardSettings.mtl EMPTY, scenario time-multiplier 1 / frame-time 0.1.
- include/vrfobjparam/groundMoveToDescriptor.h:94-99: at-distance = "The distance from a
  destination point at which an entity considers itself to be at the point", default
  1 m (ground-tracked.sysdef:257-271 follow-in-formation near 25 / at 1.0 / approach
  4.0 m/s; :272-285 lead-formation at 1).
- include/vrfmodel/groundFollowInFormationController.h (the 5.0.2 class): doMoving
  "Moves forward along the route never backwards ... will wait for the target to catch
  up" (:78-84); checkTaskEndCondition "End the task ... when at end of route" (:91-93);
  NO passed-destination test declared (contrast groundMoveToDestinationController
  Component.h:143-171 passedDestination(); docs.mak.com 5.1.1/5.2 add
  isPastDestination() to the follower after 5.0.2).
- include/vrfmodel/disaggregatedMoveAlongController.h:46-47,186-191: the unit task is
  "complete when all subordinates have reached the end of the route and have issued
  task complete reports"; taskComplete() fires only when the echelon set empties - one
  follower miss blocks its unit, which blocks the company.
- Trace evidence (REVIEW_P3 2a): at 5x consecutive sim stamps show 0.25-0.6 s gaps; at
  approach speed 4 m/s the per-frame displacement is ~0.7-2.4 m against at-distance 1 m
  (1x: ~0.13 m).

## 3. Hypotheses and predictions (written before the run)

Completion checks repeated from P3 - 22 entity + 6 unit checks read from
bin64-vrfSim.log by long-substring grep ("<kind>'s task has Completed"):
- 17 followers (follow-in-formation, ID=5): 1222.MechPlt: M1A2 2, 3, 4 (leader M1A2 1);
  AR HQ Sec 1: AUV 1, M3 1, M1A2 6, HMMWV 1, HMMWV 2 (leader M1A2 5); AR Plt 1: M1A2 8,
  9, 10 (leader M1A2 7); AR Plt 2: M1A2 12, 13, 14 (leader M1A2 11); AR Plt 3: M1A2 16,
  17, 18 (leader M1A2 15). P2c 17/17; P3 16/17 (M1A2 18 missed).
- 5 leaders (lead-formation): M1A2 1, 5, 7, 11, 15. P2c 5/5; P3 5/5.
- 6 unit move-alongs (disaggregated-movement.move-along-controller): 1222.MechPlt,
  AR HQ Sec 1, AR Plt 1, AR Plt 2, AR Plt 3, 114.MechCoy. P2c 6/6; P3 4/6.
- Interface level: TSK lines in watchvrf-trace.csv and TASKCMPLT in vrfc2simapp.log
  (1.BdeHQ, 1222.MechPlt, 114.MechCoy). P2c 3/3; P3 2/3.

STALL GEOMETRY MEASURE (scratch geometry.py, calibrated on P3 vs P2c this session):
for each entity, final position in this run (deduped POSITION text reports in
bin64-vrfSim.log, name-tagged) minus its P2c completion point (= P2c final position,
since entities stop on completion), projected on the P2c direction of travel (unit
vector from the last P2c sample >= 20 m before its final point to that final point).
along > 0 = PAST the 1x completion point, < 0 = SHORT. Calibration on P3: M1A2 18
(the stalled entity) along = +1.45 m, cross 0.00. NOISE FLOOR, stated honestly: the 16
followers that DID complete in P3 scatter along-track -8.2 .. +13.8 m from their P2c
points (M1A2 2/3/4/14 within 0.3 m; M1A2 10 +1.9, 13 +4.0, HMMWV 1 +5.0, 17 -6.3,
AUV 1 +7.8, 12 -7.9, 6 -8.2, 16 +10.9, 8/9 +11.9, HMMWV 2 +13.8); leaders 7/11/15
within 0.11 m, M1A2 5 -16.6 (its HQ section re-forms at the end). So "within ~3 m past"
is a sharp discriminator only for a stalled entity in a low-scatter slot (the three
AR Plt columns: leaders stable to 0.11 m); for HQ-section slots the leader-relative
form is used as well: along_rel = along(entity) - along(its leader). Both numbers are
reported for every non-completing entity, plus its distance behind its own leader vs
the same slot in P2c (P3 M1A2 18: 92.2 m vs P2c 93.5 m).

h1 (5x per-tick overshoot of the 1 m at-distance window; follower never moves backward;
no passed-destination test in 5.0.2): predicts at least one follower or leader
completion miss again; EVERY stalled entity found within ~3 m PAST its 1x completion
point in the direction of travel (along in (0, +3], along_rel likewise, cross ~0), at
rest from the time its platoon-mates stop; its unit and 114.MechCoy never completing
(no unit/company TSK, 2/3 or fewer TASKCMPLT). HONEST NOTE: h1 is probabilistic per
entity (the miss depends on tick phase at arrival: 16/17 survived 5x in P3), so a
17/17 result LOWERS h1's odds without refuting the mechanism; the DISCRIMINATOR is the
stall geometry (past vs short), not the count alone. The slot need not repeat (h1 does
not predict M1A2 18 specifically); a repeat on the deepest slot (-100 m, leader's
column) is consistent with h1 and noted if it happens.
h2 (variable-frame nondeterminism unrelated to the at-distance window): predicts a
stalled entity SHORT of its completion point (along < -3 m with along_rel also < -3),
or misses of a different character (mid-route stalls, entities still moving, leaders or
whole units failing while followers complete, nav/route errors), or 17/17 with no
pattern. A miss SHORT plus zero nav/route errors would be the strongest h2 evidence.
h3 (REVIEW_P3: slot/order assignment): record every follower's slot line (rightOffset/
forwardOffset/leaderOffset) and each lead-formation subordinate order; if a miss lands
on a slot whose parameters differ from P3's M1A2 18 line ("-25,-75,-100"), h3 weakens.
Assignment of working-route index to subordinate is known to be arbitrary (P3 sec C)
and is not a variable.

INVARIANTS expected again (from P3; each MET/MISSED, none is a new variable):
I1 clock: vendor-clock ratio 5.0 +/- 0.3 (sim-stamp span / wall span in
   bin64-vrfSim.log) and onset->settle ratios P2c/P3R in [3.5, 7] per taskee.
I2 endpoints: 1222.MechPlt, 114.MechCoy, 1.BdeHQ settle within 2 m of P2c (P3: 0.2 /
   1.8 / 0.1 m) with bit-identical plateaus and POS==RPT.
I3 vendor log: 0 "Waiting for nav data", 0 "empty route", 0 "Can't find entity route".
I4 app log: 0 SocketException / "Only one usage" / "Connection error".
I5 platoon + entity TASKCMPLT/TSK present, trace t in [45, 70] (P3: 53.2 / 55.6 =
   ~+21 / +24 s after the order push); report counts per <ReportContent>.
I6 binding gate: `Sim Run() queued (start the VR-Forces clock; timeMult=5).`
A missed invariant is reported as such and makes the run a poorer h1/h2 split (I1 or I6
missed = the multiplier did not take = VOID for the split; I2/I3 missed = STOP and
report, do not interpret the completion count).

## 4. Command, appNos, environment, validity, stop rules

- Command (cwd repo root, pwsh): `$env:Vrf__TimeMultiplier='5'; pwsh -NoProfile -File
  scripts/RunC2SimScenario.ps1 -Init data/R9_Mojave_Lean_Initialization_NoComments.xml
  -Order data/R9_Mojave_UnitMove_Order_NoComments.xml -RunSecs 420`; console to the
  session scratchpad. Plumbing unchanged from P3 sec 3.
- appNos: runner-ledgered from the Appendix B marker, 3655 at registration -> 3655-3661,
  marker -> 3662. Never hand-edited.
- Environment at registration: VR-Forces DOWN; RTI RESIDENT (rtiAssistant 41336 /
  rtiexec 224608 / rtiForwarder 76620 - never touched); docker Up (pre-flight
  re-checks all three); repo main at cef469b.
- Validity gates as P3 sec 6 (6 units dispatched, RealTemplates, 6x Create-altitude
  Live, PushOrder EXIT=0 with 3 CreateRoute + 3 MoveAlongRoute, oracle gate, trace,
  RPT live) plus I6. Infra failure = VOID; two consecutive infra failures = stop.
- Reading: onset/settle on the trace clock as P2c/P3 (adjudicate_p3.py), completion
  chain and geometry from bin64-vrfSim.log (geometry.py). Raw mid-move POS distances
  are DR-poisoned and never gated.
- No further probe after this one regardless of outcome. The outcome is appended
  below, committed and pushed; the handoff OPERATIONAL STATE marker line is updated.

## Outcome

(to be filled AFTER the run, against section 3)
