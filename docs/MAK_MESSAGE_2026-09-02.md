# Message to MAK support - SEND-READY DRAFT (2026-09-02), user sends

Consolidates Notes 1 and 3 of docs/MAK_NOTES_DRAFT_2026-09-01.md (Note 2 resolved, no
defect report) into one message. Rulings of 2026-09-02: multipliers stay at 1x for probes,
the question below is asked as a question; the crash-dump report (vrfSim 70668) is handled
separately by the user (done). Row 2c (run 20260902T104832Z) CONFIRMED the terrain-profile
reply packing (3 samples per request once every entry of every set is read) - no terrain
question for MAK; the message is complete as written.

---

To: MAK support
Subject: VR-Forces 5.0.2 Remote Control API - three questions (docs, unit-level tasking, time multiplier)

Hello,

We are integrating an external C2 system (C2SIM) with VR-Forces 5.0.2 (vrfSim
5.0.2-MSVC++15.0_64-249613, HLA 1516e federate, MAK RTI, Windows 11) purely through
the Remote Control API from our own headless application. Everything we need works;
we would like to confirm we are on the intended path in three places.

1. Documentation. We work from the public Developer's Guide and class reference at
   docs.mak.com (5.2 and 5.1.1). The 4.10 guide's chapters "The Aggregate Entity
   Behavior Model", "The Organization Manager", "Echelon IDs", "Object Console Messages"
   and "Ground Disaggregated Movement System" do not appear in the 5.x guides, although
   their titles remain in the 5.2 keyword index with empty links. Is there a 5.x home for
   that material, or should we treat the 4.10 text as authoritative for 5.0.2?

2. Unit-level tasking through the API. We create entity-level units of company echelon
   and above with createAggregate (createSubordinates = true) and task them with
   move-along routes, all from the Remote Control API with no GUI interaction. Is there
   recommended reading or training material for this use, so we can confirm it is the
   intended path rather than an exotic one?

   One minor observation, no action needed: creating the stock "Tank Company (USA)"
   (EntityLevel) logs `Aggregate state has invalid formation name "column-left"` for its
   HQ section (the company .frm files reference sub-formation names the HQ-section
   template does not define). The documented working-formation fallback covers it and
   the company operates normally; we mention it only because at raised console verbosity
   it appears on every company creation.

3. Follow-in-formation completion under a time multiplier. With
   DtVrfRemoteController::setTimeMultiplier(5) in the default variable-frame mode, one of
   two otherwise identical company move-along runs had a single follower stop about
   1.4 m beyond its offset-route endpoint; its follow-in-formation task never reported
   complete (vrfSim.log at notify level 3 shows the task cleared only at entity removal),
   so the platoon's and company's move-along never completed. The identical repeat
   completed normally, and at 1x we have never seen it. We see in the 5.0.2 headers that
   DtGroundFollowInFormationControllerComponent completes on an at-distance test
   (ground-tracked.sysdef: at 1.0 m, near 25 m, approach 4 m/s), and in the 5.1.1 class
   reference that the class gained isPastDestination() and distanceBehind().
   a. Is running unit-level tasking at a time multiplier in variable-frame mode an
      intended configuration, or should we switch to fixed-frame (fastForwardSettings)
      whenever a multiplier is applied?
   b. Does isPastDestination() in 5.1.1+ change the follower's completion test, so that a
      follower stopping slightly past its endpoint now completes?
   c. Is there a recommended way to make unit-level move-along completion robust to one
      subordinate stopping just outside the at-distance tolerance?

   We can supply both vrfSim.log files (notify level 3), the scenario, and the order if
   useful.

Thank you,
Paulo Barthelmess
Hyssos Tech

---

Evidence pointers (internal, not part of the message): item 3 = runs 20260901T221227Z (P3,
the miss) and 20260901T230326Z (P3R, 28/28), 1x baselines 20260901T211310Z and
20260901T235823Z, docs/experiments/ANALYSIS_P3_STEP_PROFILE_2026-09-01.md; item 2
observation = run 20260901T211310Z (P2c).
