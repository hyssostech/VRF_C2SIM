# DRAFT notes to MAK support (2026-09-01) - FOR USER REVIEW, NOTHING SENT

Posture (user ruling 2026-09-01): VR-Forces is a mature, widely-deployed product; where
our results look like "vendor bugs" the stronger prior is that WE are off the beaten
path. Both notes below are written from that posture. Note 2 has two variants; run P2c
(in flight) selects one. The user sends these, not the tooling.

---

## NOTE 1 - documentation access question (send any time; independent of P2c)

Subject: VR-Forces 5.x Developer's Guide - aggregate/organization chapters

We are integrating an external C2 system with VR-Forces 5.0.2 via the Remote Control
API and are working from the public Developer's Guide at
docs.mak.com/api/vrforces5.2/classref/ (also 5.1.1). Two questions:

1. The 4.10 guide's chapters "The Aggregate Entity Behavior Model", "The Organization
   Manager", "Echelon IDs", "Object Console Messages", "Ground Disaggregated Movement
   System" do not appear in the 5.x guides; their titles remain in the 5.2 keyword
   index with empty links. Is there a 5.x home for this material, or should we treat
   the 4.10 text as authoritative for 5.0.2?
2. Is there recommended reading (or training material) for driving ENTITY-LEVEL units
   of company echelon and above purely through the Remote Control API (createAggregate
   with createSubordinates + move-along tasking)? We want to confirm we are on the
   intended path rather than an exotic one.

---

## NOTE 2 - RESOLVED BY P2c (2026-09-01 evening): NO DEFECT REPORT.
Run 20260901T211310Z: the STOCK "Tank Headquarters Section (USA)" template works
end-to-end under correctly-authored (above-terrain) route vertices; the invalid-
formation warning fired once and the documented working-formation fallback covered it.
Fold ONE optional observation into Note 1:

3. Minor observation, no action needed: creating "Tank Company (USA)" (EntityLevel)
   logs `Aggregate state has invalid formation name "column-left"` for its HQ section
   (the company .frm files reference sub-formation names the HQ-section template does
   not define); the working-formation fallback covers it and the company operates
   normally. Mentioned only because at raised console verbosity it appears on every
   company creation.

---

## NOTE 3 - QUESTION (not a defect report): follow-in-formation completion at a
## time multiplier, 5.0.2. Drafted 2026-09-01 late; user decides whether to send.

Evidence base (all in repo): runs 20260901T221227Z (P3) and 20260901T230326Z (P3R),
identical company move-along orders at DtVrfRemoteController::setTimeMultiplier(5);
1x baselines 20260901T211310Z and 20260901T235823Z. In P3 ONE follower (M1A2 18) of a
Tank Company (USA) stopped 1.45 m past its offset-route end and its follow-in-formation
task never completed (vrfSim.log at notify level 3: task 5 is cleared only at entity
removal), so the platoon and company move-along never completed. The identical repeat
P3R completed 28/28. Step analysis found nothing distinguishing the two runs (clock
5.003x vs 4.978x, same tick quantum, same report cadence) -
docs/experiments/ANALYSIS_P3_STEP_PROFILE_2026-09-01.md. Cause unknown; n=2.

Subject: Follow-in-formation completion tolerance under setTimeMultiplier (VR-Forces 5.0.2)

We drive Tank Company (USA) units (EntityLevel, createAggregate + move-along) through
the Remote Control API with setTimeMultiplier(5) in the default variable-frame mode. In
one of two otherwise identical runs a single follower stopped about 1.4 m beyond its
offset-route endpoint and its follow-in-formation task did not report complete, so the
unit's move-along never completed; the repeat run completed normally. We see in the
5.0.2 headers that DtGroundFollowInFormationControllerComponent completes on an
at-distance test (ground-tracked.sysdef: at 1.0 m, near 25 m, approach 4 m/s), and in
the 5.1.1 class reference that the class gained isPastDestination(). Questions:

1. Is running units at a time multiplier in variable-frame mode an intended
   configuration for unit-level tasking, or should we use the fixed-frame setting
   (fastForwardSettings) whenever a multiplier is applied?
2. Does the 5.1.1+ isPastDestination() change the follower's completion test (i.e.
   would a follower that stops slightly past its endpoint now complete)?
3. Is there a recommended way to make unit-level move-along completion robust to a
   single subordinate stopping just outside the at-distance tolerance?

We can supply both vrfSim.log files (notify level 3) and the scenario/order if useful.

Send order: Note 1 now if desired; Note 2 variant per P2c; Note 3 only if the user
wants the question asked (it is a question, not a report); license renewal is handled
separately (in process).
