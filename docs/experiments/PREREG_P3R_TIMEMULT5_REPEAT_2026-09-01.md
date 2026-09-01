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

## Outcome - P3R (run 20260901T230326Z_run) - VALID; 28/28 completions; no stall

Run dir runs/20260901T230326Z_run/. appNos 3655-3661 (marker 3655 -> 3662, runner-
ledgered). Runner EXIT=0, every stage exit 0, RTI preserved (41336/224608/76620),
nothing killed, VR-Forces down after StopVrf. Order pushed 23:05:50.466Z; observation
end 23:13:21.608Z. Same scripts as P3 (adjudicate_p3.py, geometry.py).

VALIDITY: all gates MET (6 units dispatched, RealTemplates, 6x Create-altitude Live,
PushOrder EXIT=0 with 3 CreateRoute + 3 MoveAlongRoute, oracle gate 132 real / 44
uuids, trace 224 samples per taskee, RPT 38 fixes per taskee). I6 binding gate MET:
`Sim Run() queued (start the VR-Forces clock; timeMult=5).` (vrfc2simapp.log:38).

COMPLETION COUNTS (bin64-vrfSim.log, long-substring grep):
- followers 17/17 (P3 16/17; P2c 17/17): M1A2 4 155.432, 3 157.754, 2 158.876;
  13 210.377, 10 210.838, 18 210.872, 12 212.040, 8 213.027, 9 213.027, 16 213.062,
  17 214.034, 14 214.376; HMMWV 1, HMMWV 2, M1A2 6, M3 1 215.387, AUV 1 215.387.
- leaders 5/5: M1A2 1 161.472, 7 214.515, 15 214.553, 11 214.902, 5 215.249.
- units 6/6: 1222.MechPlt 161.472, AR Plt 1 214.515, AR Plt 3 214.553, AR Plt 2
  214.902, AR HQ Sec 1 215.387, 114.MechCoy 215.424 (P3: 4/6; P2c 6/6 with company
  190.312).
- interface 3/3: TSK t=52.2 1.BdeHQ, 54.6 1222.MechPlt, 65.4 114.MechCoy; 3 TASKCMPLT.
- M1A2 18 (P3's stalled entity) completed at 210.872, FIRST of AR Plt 3's followers,
  as in P2c (185.807, also first).
STALL GEOMETRY: no non-completing entity, so nothing to classify past/short. For the
record, ALL 28 finals (17 followers, 5 leaders, 6 units) lie within 0.22 m along-track
and 0.00 m cross-track of their P2c completion points (largest: M1A2 12/13 +0.22,
M1A2 2 +0.18; 11 entities exactly 0.00). Contrast P3, where the 16 followers that DID
complete scattered -8.2 .. +13.8 m from P2c. So P3 differed from both 1x P2c and 5x
P3R company-wide, not only at M1A2 18 - an OBSERVATION, cause not established (the
scatter could be downstream of the non-completion - the company task stayed active -
or a symptom of the same upstream event).

INVARIANTS:
I1 clock MET: vendor stamps 32.697 @19:05:50 -> 215.424 @19:06:27 local = 182.7 sim s /
   37 +/- 1 wall s = 4.94x (4.81-5.08). Onset->settle ratios P2c/P3R: platoon 128.7/26.5
   = 4.86; company 181.8/36.5 = 4.98; entity 118.4/24.5 = 4.83 (all in [3.5, 7]).
I2 endpoints MET: 1222.MechPlt 34.612956,-116.587783 (0.1 m from P2c); 114.MechCoy
   34.653915,-116.693388 (0.0 m - identical); 1.BdeHQ 34.608416,-116.699992 (0.1 m).
   Plateaus bit-identical (420.6 / 408.5 / 424.6 s); POS==RPT 0.0 m for all three.
I3 MET: 0 "Waiting for nav data", 0 "empty route", 0 "Can't find entity route"; 1
   "invalid formation name" (as P2c/P3).
I4 MET: 0 SocketException / "Only one usage" / "Connection error".
I5 MET: TSK 52.2 and 54.6 in [45, 70] (P3 53.2 / 55.6); company TSK 65.4 (none in P3).
   Reports per <ReportContent>: 231 = 228 PositionReportContent + 3 TaskStatus (P3
   230 = 228 + 2; P2c 99 = 96 + 3).
I6 MET (above).

h3 RECORD: slot lines identical in kind to P3 where attributable (AR Plt 3 followers
leader=M1A2 15: "25,75,50", "25,-25,-50" [M1A2 16 in all three runs], "-25,-75,-100";
M1A2 18's own line garbled in P3R as in P2c - which of the two remaining slots it held
is UNVERIFIED in P2c and P3R, verified "-25,-75,-100" only in P3). Lead-formation
subordinate order for AR Plt 3: P2c "16, 17, 18"; P3 "18, 17, 16"; P3R "17, 18, 16" -
three runs, three orders: the order is nondeterministic and is not a variable. Working
route indices again permuted (AR Plt 3 on _R3, AR Plt 2 on _R1, AR Plt 1 on _R0).

ADJUDICATION AGAINST SECTION 3:
- h1 primary prediction ("at least one follower or leader completion miss again")
  MISSED. As registered, h1 is probabilistic per entity, so this LOWERS its odds
  without refuting the mechanism: the 5x record is now 33/34 follower completions over
  two runs (one miss), i.e. a per-entity miss rate on the order of 3% if h1 is the
  mechanism, vs 0/43 at 1x today. The discriminating observation (stall geometry) was
  not produced because nothing stalled.
- h2 prediction "17/17 with no pattern" MET. Also consistent with h2: the P3-only
  company-wide 8-14 m scatter of completed followers (P3R and P2c agree to 0.22 m
  across 1x/5x), i.e. P3 looks like a run-level deviation rather than a single-entity
  tick-phase event. This is suggestive, not established (n=2 at 5x).
- h3 uninformative (no miss to locate; slot for M1A2 18 unverifiable again).
- All six invariants MET: 5x reproduces the 1x endpoints and completions in this run.
VERDICT FOR THE RECORD: 5x is NOT established as safe (1 of 2 runs failed to complete
the company task) and NOT established as the cause of the P3 miss (the repeat was
clean). Probe runs stay at 1x per PREREG_P3's fired falsifier until a further ruling;
the user decides whether more 5x samples are worth their cost. No further probe was
started (brief).

VERIFIED: every number above from the run-dir artifacts. ASSUMED: identical inputs
(manifest paths identical to P3; product untouched between runs). UNEXPLAINED: (1) the
P3 M1A2 18 non-completion itself - a single event in 34 follower completions at 5x,
with +1.45 m overshoot geometry that fits h1 and a company-wide final-position scatter
that fits a run-level deviation; (2) why P3's completed followers ended 8-14 m from the
positions that both P2c (1x) and P3R (5x) reproduce to 0.22 m.
