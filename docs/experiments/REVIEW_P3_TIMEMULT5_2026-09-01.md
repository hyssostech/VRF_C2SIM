# REVIEW - Probe P3 (Vrf__TimeMultiplier=5), cold re-adjudication (2026-09-01)

Tier: HEAVY (the executor's outcome carries a cause claim: "5x-specific per-frame
arrival miss"). Reviewer had READ access only: run artifacts, C:\MAK docs/headers/
logs, docs.mak.com. No sim launched, no federation joined, nothing under C:\MAK
touched, oracle C++ repo untouched.

Subject:   docs/experiments/PREREG_P3_TIMEMULT5_2026-09-01.md
P3 run:    runs/20260901T221227Z_run  (TimeMultiplier=5)
Baseline:  runs/20260901T211310Z_run  (P2c, 1x, stock template, 3/3 complete)

## 0. Verdict in one paragraph

AGREE with the executor's per-prediction verdicts (A met, B met, C MISSED 2/3,
D met, E met, F met) and with "falsifier fired, STOP". Every A/B/D/F number
reproduced exactly from the raw traces. Two recorded-not-gated facts in the
prereg are wrong or unsupported: (1) "8 TaskStatus" - the captured stream holds
2 TaskStatus reports (P2c: 3), not 8; (2) "M1A2 18 slot parameters identical in
kind to P2c" - M1A2 18's P2c slot line is unrecoverable from the garbled vendor
log, so this is an assumption, and the lead-formation subordinate ORDER is
reversed between runs. Neither changes a gated verdict. The executor's h1
("5x-specific per-frame arrival miss") is the best-supported hypothesis but is
NOT established: n=1 at 5x, and the P3 outcome is also consistent with
run-to-run nondeterminism (h2) or a slot/order difference (h3). One registered
5x repeat splits h1 from h2; that is the recommended next probe and it matches
HANDOFF NEXT row 5(c).

## 1. Independent re-derivation of A-F

Method: WatchVrf trace (POS,t,uuid,lat,lon,alt / RPT / TSK), vrfc2simapp.log,
reports-captured.log, bin64-vrfSim.log; scratch scripts adj.py, frames.py,
finals.py, misses.py (scratchpad, not tracked). Onset = first POS > 25 m from
birth; settle = first sample of the terminal bit-identical plateau.

| Pred | Executor claim | Reviewer result | Agree |
|------|----------------|-----------------|-------|
| A clock | onset->settle ratios P2c/P3 4.52 / 4.25 / 5.29 | 4.52 / 4.25 / 5.29 (MechPlt / MechCoy / BdeHQ); vendor clock vs wall 5.0036x from sim stamps | yes |
| B endpoints | ~0.2 / 1.8 / 0.1 m from P2c | 0.18 / 1.80 / 0.09 m; RPT==POS 0.0 m; plateaus 420.4 / 406.1 / 426.5 sim-s | yes |
| C completion | MISSED 2/3 | TSK at 53.2 (BdeHQ) and 55.6 (MechPlt) wall-s; 2 TASKCMPLT in vrfc2simapp.log; MechCoy never | yes |
| D | met | 3 taskees resolved (a81f2304 / 5eb35810 / d4126b05), no clamp/warning delta vs P2c | yes |
| E | met; "456 PositionReportContent (P2c 192) + 8 TaskStatus (P2c 9)" | 456 / 192 PositionReportContent CONFIRMED. TaskStatus: 2 in P3, 3 in P2c (grep -c counted lines, not reports) | numbers: NO; verdict: yes |
| F | met | binding line "Sim Run() queued (start the VR-Forces clock; timeMult=5)." present | yes |

Vendor-log completion chain, P3 (bin64-vrfSim.log; counted with long unambiguous
substrings because concurrent thread writes merge line prefixes):
- follow-in-formation Completed: 16 of 17 followers. The one miss is M1A2 18.
- lead-formation Completed: 4/4 (M1A2 5, 7, 11, 15).
- unit move-along Completed: 4/6 (1222.MechPlt 155.279, AR Plt 1 218.139,
  AR HQ Sec 1 219.582, AR Plt 2 223.134). AR Plt 3 and 114.MechCoy never
  (their subordinate sets never emptied - see disaggregatedMoveAlongController.h
  below). Clearing lines at 2283.150 (M1A2 18 follow, ID=5), AR Plt 3 (ID=1),
  114.MechCoy (ID=0) are the end-of-run teardown, not completions.
- P2c: M1A2 18 completed at 185.807 (first of AR Plt 3), company at 190.312.

Geometry of the miss (finals.py): M1A2 18's P3 final RPT is +1.45 m north of
its P2c completion point (north = direction of travel, i.e. TOWARD the leader),
same longitude. Its leader M1A2 15 sits 0.11 m from its P2c position. M1A2 18
is in the leader's own column (rightOffset -25), 92.2 m behind (P2c 93.5 m).
For scale: followers that DID complete differ between runs by up to 31 m
(M1A2 5 31.35, HMMWV 2 24.85, AUV 1 15.69, M1A2 8/9 11.90, M1A2 16 11.38), so
a 1.45 m offset is not itself anomalous - what is anomalous is the missing
completion report.

Task-parameter lines (vrfSim.log): P3 line 10003 M1A2 18 "rightOffset=-25;
forwardOffset=-75; leaderOffset=-100"; P3 10065 M1A2 16 "25,-25,-50"; P3 9998
(garbled prefix) "25,75,50". P2c 9355 M1A2 16 "25,-25,-50"; P2c 9360 (garbled
"M1A2 8" prefix) "25,75,50"; the third P2c slot line is not attributable.
Lead-formation subordinate list: P2c "M1A2 16, M1A2 17, M1A2 18"; P3
"M1A2 18, M1A2 17, M1A2 16". The executor's "identical in kind" is therefore
unverified for M1A2 18.

## 2. Docs-first: what the vendor documents (with citations)

Sources read, in the mandated order: local Users Guide HTML and headers under
C:\MAK\vrforces5.0.2, then docs.mak.com 5.2 / 5.1.1 / 4.10 classref, then one
internet search.

### 2a. How the time multiple works

Documented: the multiplier scales SIM TIME PER FRAME; it does not add frames.
- C:\MAK\vrforces5.0.2\doc\...\Introduction\Concepts\vrf_runFasterThanRealTime.htm:
  faster-than-real-time is achieved "by applying a multiplier to the tick time";
  three clock modes (Variable-Frame Run-To-Complete = default, sim time advances
  by wall elapsed since last tick and "does not provide repeatable results";
  Fixed-Frame Best-Effort; Fixed-Frame Run-To-Complete); high multiples "can
  cause performance of simulation object models to degrade".
- ...\Scenarios\CreateRun\vrf_automaticallyChangin.htm: at higher speeds "the
  frame rate is reduced and performance of models may degrade"; documents
  fastForwardSettings.mtl entries that switch dead-reckoning algorithm,
  frame-mode (1/2/3) and frame-time at play-speed thresholds.
- ...\Scenarios\Files\vrf_scenarioParams.htm: frame-mode / frame-time /
  time-multiplier scenario keys.
- ...\Introduction\Performance\FrameRateTuning.htm: targetFrameRate applies in
  variable frame mode only.
Install state: C:\MAK\vrforces5.0.2\appData\settings\vrfSim\vrfSim.mtl:283
";; (setqb targetFrameRate 30)" (commented out), :290 clockMode 0;
fastForwardSettings.mtl is the empty "(fast-forward-settings )"; scenario
userData\scenarios\TropicTortoise.scn: (frame-mode "variable-frame")
(frame-time 0.100000) (time-multiplier 1.000000) (random-number-seed 0).
Trace evidence agrees: consecutive sim stamps 0.031-0.039 s apart in both runs,
with 0.25-0.6 sim-s gaps appearing only at 5x. At approach-speed 4 m/s the
per-frame displacement is ~0.13 m at 1x and ~0.7-2.4 m at 5x, against an
at-distance of 1 m.

### 2b. Completion criterion of follow-in-formation and unit move-along

Documented (headers; the Users Guide has zero hits for "follow-in-formation" /
"lead-formation" and ComponentDescriptorElements.htm only shows the parameters
in an example without defining them):
- C:\MAK\vrforces5.0.2\include\vrfobjparam\groundMoveToDescriptor.h:94-99:
  at-distance = "The distance from a destination point at which an entity
  considers itself to be at the point." Default 1 m. :88-92 near-distance =
  point at which the entity begins to slow down, default 25 m. :101-107
  approach-speed default 2 m/s.
- include\vrfmodel\groundFollowInFormationController.h (copyright 2021, the
  5.0.2 class): :29 derives DtGroundAutoControllerComponent; :49-51 tick
  "Checks the task end condition"; :78-84 doMoving "Moves forward along the
  route never backwards. If the target position is back along the route, entity
  will wait for the target to catch up ... steer towards a point 20 meters
  ahead"; :91-93 checkTaskEndCondition "End the task and releases the throttle
  when at end of route"; :123-125 findTargetLocation "based on the myLeader's
  position and myBackwardsOffset. There is an extra case for the end of route
  when myBackwardsOffset is negative"; :176-183 myNearDistance "used to keep on
  route", myAtDistance "Descriptor parameter, used to end task", myApproachSpeed.
  NO overshoot / passed-destination test is declared.
- include\vrfmodel\groundLeadFormationController.h: derives from the follow
  controller, same end condition text.
- Contrast: include\vrfmodel\groundMoveToDestinationControllerComponent.h:143-171
  declares nearDestination(), atDestination() (distance^2 <= at-distance^2) AND
  passedDestination() ("dead-reckons its position ... dot product > 0 ... will
  pass the destination this tick") - the move-to family has an overshoot guard.
- include\vrfmodel\disaggregatedLeadFollowInFormationController.h:42-43 and
  disaggregatedMoveAlongController.h:46-47,186-191: unit task is "complete when
  all subordinates have reached the end of the route and have issued task
  complete reports"; processTaskComplete removes the echelon id; taskComplete()
  fires when the set is empty. This is why one follower miss (M1A2 18) blocks
  AR Plt 3, which blocks 114.MechCoy.
- Sysdef values in force: data\simulationModelSets\EntityLevel\vrfSim\systems\
  movement\ground-tracked.sysdef:257-271 follow-in-formation near 25 / at 1.0 /
  approach 4.0, min-tick-period -1, tick-period-uses-real-time False; :272-285
  lead-formation near 25 / at 1; :240-256 move-along at 1 / approach 2.
- docs.mak.com/api/vrforces5.1.1/classref/class_dt_ground_follow_in_formation_
  controller_component.html and the 5.2 page: the follower class GAINED
  isPastDestination(const int currentIndex), adjustStartPoint, distanceBehind,
  isLeaderCapable after 5.0.2 (grep of C:\MAK\vrforces5.0.2\include finds none
  of these names); 5.2 deprecates the class in favour of
  DtGroundManeuverInFormationControllerComponent, whose page says the task ends
  "when at end of route" and isPastDestination reports "whether an entity has
  progressed beyond waypoints". The 4.10 classref page for the follower class
  is 404; 4.10 entity_behaviors_ground_disaggregated_movement.html lists the
  controllers only, no completion tolerance text.

### 2c. MAK caveat about completion at time multiples > 1

NOT DOCUMENTED. Searched: the two Users Guide pages in 2a (only the generic
"performance of models may degrade"), FrameRateTuning.htm, all headers named
above, VRF5.0.2ReleaseNotes.pdf, FixedDefectsVRF.csv, Known Problems section
(VRF-5665 monitor scaling, VRF-4370 saved paths only), docs.mak.com 5.2 / 5.1.1
follower and maneuver class pages, and an internet search ("VR-Forces" "time
multiplier" / "faster than real time" task complete follow formation) which
returned only marketing pages and unrelated VR time-perception papers.

### 2d. Release / migration notes on path-planning or arrival changes

- VRF5.0.2ReleaseNotes.pdf (pdftotext): fixed "SIM crashes when executing a
  Lead / Follow in Formation task"; "Human move along fail" (reset current
  vertex); "Task complete report messages are not received in the front-end
  callbacks" (VRF-5738/5740/5745/5747/5753). Nothing on time multiplier.
- FixedDefectsVRF.csv VRF-5677 (fixed in 5.0.1): "Vehicle convoys sometimes get
  stuck at the end of the road ... unit ... executing a convoy task to
  recognize that the task was complete" - a prior end-of-route completion
  defect in the same controller family, fixed before our version.
- docs.mak.com 5.1.1 -> 5.2 API delta (2b): isPastDestination added to the
  follower, class deprecated in 5.2. Not described as a fix in any note found;
  it is circumstantial evidence that overshoot handling in the follower was
  revisited after 5.0.2. Migration guides vrf_migration50/51.html were not
  read for this review (HANDOFF NEXT 4(d) covers them for the 5.2b upgrade).

## 3. 1x baseline follower miss rate (today)

follow-in-formation task starts vs "task has Completed" per run (misses.py):
191004Z 3/3, 194029Z 3/3, 200935Z 3/3, 203702Z 17/17, 211310Z 17/17.
Total at 1x: 43/43, ZERO misses. P3 at 5x: 16/17, one miss. (Company-level
non-completions in 191004Z/194029Z/200935Z were NavArea / empty-route
failures per PREREG_P1 and are not follower misses.)

## 4. Hypotheses, falsifiers, recommended probe

h1 (executor's, 5x-specific): at 5x the follower's per-frame step (0.7-2.4 m)
exceeds the 1 m at-distance window; the 5.0.2 follower moves "never backwards"
and declares no passed-destination test, so once past the end-of-route target
it waits forever. Supports: +1.45 m overshoot in the direction of travel;
43/43 at 1x vs 16/17 at 5x; move-to family has passedDestination() and later
versions added isPastDestination() to the follower. Falsifier: a 5x repeat with
identical inputs completing 17/17 (single clean repeat weakens h1 sharply, since
16 of 17 followers already survived 5x once); or a stalled entity found
SHORT of its 1x completion point.

h2 (strongest competitor): variable-frame nondeterminism unrelated to the
multiple ("does not provide repeatable results", vrf_runFasterThanRealTime.htm).
Weakened by 43/43 at 1x today but not excluded at n=1. Falsifier: a second 5x
miss, especially at the same slot/geometry, or any 1x miss over the next runs.

h3: slot/order assignment difference (subordinate list reversed; P2c slot for
M1A2 18 unverifiable). Falsifier: the 5x repeat logs M1A2 18 with the same
"-25,-75,-100" slot and completes; or the miss lands on a different slot.

Recommended ONE next probe (do not run under this review): P3-REPEAT - the
identical 5x run (same order, same scenario, same TimeMultiplier=5, RunSecs per
HANDOFF 5(a)), no other change. Predictions: h1 -> at least one follower miss,
the stalled entity within ~3 m PAST its 1x completion point along the route and
its unit/company never completing; h2 -> most likely 17/17 (p(miss) at 1x
today is 0/43); h3 -> miss only if the slot assignment repeats. Gated read: the
follow-in-formation Completed count (long-substring grep) and the stalled
entity's final RPT relative to its P2c completion point. Levers NOT chosen for
the first split, in order of documented support: (i) fastForwardSettings.mtl
fixed-frame entry at speed >= 5 (vendor-documented knob, but changes frame mode
AND dead-reckoning, two variables); (ii) 2x run (scaling curve, weaker split);
(iii) at-distance override in a C2simEx sysdef copy (content change under
C:\MAK, needs a ruling).

## 5. Verified vs assumed

Verified: all table numbers in section 1; completion chains; 43/43 at 1x;
header and Users Guide citations; docs.mak.com method lists; install settings.
Assumed: that the 5.0.2 follower's end-of-route branch has no overshoot guard
in its implementation (only the HEADER is visible - a guard could exist
unnamed inside checkTaskEndCondition); that no vertex-level difference exists
between P2c and P3 routes (route payloads were byte-identical per the prereg's
D gate, not re-diffed here).

Adversarial review: the strongest competing hypothesis to h1 is h2
(nondeterminism); today's 43/43 at 1x is the only evidence against it and it
is a same-day sample from one scenario. Unexplained symptom: why M1A2 18 and
not any of the other 16 followers - it is the deepest slot (-100 m) in the
leader's own column, which is where an end-of-route target computed from a
negative myBackwardsOffset ("extra case", header :123-125) applies, but this
is a reading of a header comment, not an observation. Second unexplained
item: the executor's "8 TaskStatus" figure has no source in the artifacts.

## 6. Recommended prereg edits (docs only)

- PREREG_P3 outcome E: replace "8 TaskStatus (P2c 9)" with "2 TaskStatus
  reports (P2c 3)".
- PREREG_P3: mark "M1A2 18 slot identical in kind to P2c" as UNVERIFIED
  (garbled P2c line) and record the reversed subordinate order.
- HANDOFF NEXT 5(c): unchanged - already names the 5x repeat; add the
  overshoot geometry as the gated read.
