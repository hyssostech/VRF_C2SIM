# CORRECTIONS LOG

Provenance of claims that were once stated and later found wrong. Only the two ENTRY docs -
RESUME_PROMPT.md and HANDOFF - were rewritten clean (2026-07-21) and state current truth
with no retraction history. RUNBOOK and VRF_GROUNDWORK_PLAN are large accreted files that
STILL carry in-line retraction / READ-FIRST blocks; treat any sentence in them as current
only if it is not inside a superseded fence. This file is where the history lives; consult
it only to answer "was X ever believed, and why is it not believed now". ASCII only.

Each entry: the claim, why it was wrong, and the evidence that settled it.

## Movement of 1222.MechPlt

- CLAIMED (through 2026-07-19 early): "moves ~174 m of a ~1155 m route, then STOPS - a
  reproducible defect." WRONG. The unit was still moving, not decelerating, when telemetry
  ended. ~174 m is a REAL MEASURED displacement over RPT's ~124 s coverage, not a stopping
  point and not the observation-window length (that would predict ~203 m at 1.4 m/s).
  Evidence: RPT final-leg speeds 1.45/1.49/1.48 m/s across three runs, no deceleration; a
  POS sample at t=159.9 (run 161438Z) 4.6 m from the concurrent RPT fix.
- CLAIMED (2026-07-20): "still ACCELERATING when observation ended." OVER-READ. Three RPT
  fixes = two legs, and leg 1 begins at task issue so it contains spin-up from rest; two
  legs cannot distinguish acceleration from reaching cruise ~1.48 m/s. Current wording:
  "still moving, not slowing."
- CLAIMED (2026-07-20, round 6): "RPT stops reporting ~23 s before the POS trace does
  (t=157.1 vs 180.3) - a new open gap." FABRICATED. No gap: RPT's period is ~62 s, the next
  fix was due at t~219 s, and the interface resigned at ~182 s. The figure also compared one
  unit's last fix (157.1) against the global POS collapse (180.3) - different quantities.
  Deleted round 7.

## The t=180 readable collapse

- CLAIMED (briefly, round 4 cold-read): the 53->2 readable collapse at t~180 is an oracle
  fault. WRONG. It is the interface RESIGNING at teardown; the two survivors are the two
  baseline uuids present before creation. Evidence: collapse tracks VrfC2SimApp process exit;
  trace keeps sampling to t~679.
- CLAIMED: "tracks the app exit to within 0.3 s." Own numbers give deltas up to 0.5 s, and
  the app-exit column was not reproducible without a fitted per-run offset. Current evidence
  is the survivor-uuid identity, which needs no clock alignment.

## Baseline objects "positionless"

- CLAIMED (RUNBOOK 0.5.7, for days): "the TropicTortoise baseline objects are POSITIONLESS -
  that is simply how they reflect." WRONG as a statement about reflected values, and
  corrected wrongly FOUR times before the counted census stuck. Verified census:
  d39a55ad (GlblTerrDmg) 0 samples, never reflects; f864e51f (GlobalEnv) 1388 samples, forms
  NaN,-90,NaN and 0.0,-90,6.4e72, never its authored 9e-6; cde66adc (Page-In Area) 1390
  samples, four forms incl. altitudes 1.02e15 and 6.4e72. BOTH readable objects are
  cast-corrupted; neither's true position has ever been read.

## VrfFacade RTTI / the aggregate cast

- CLAIMED (RUNBOOK sec 7, since 2026-07-10): "dynamic_cast<DtReflectedAggregate*> fails due
  to RTTI across the MAK DLL boundary." FALSE. Under DtHLA the class deliberately derives from
  DtReflectedObject (reflectedExtAggregate.h:15-19), so a null cast is correct. The blind
  static_cast worked on aggregates only by accidental vtable-slot alignment, and is UB on
  control objects - the cause of the 0xC0000005 crash.
- CLAIMED (2026-07-19, briefly): "the static_cast is removed; aggregates resolve via the
  typed list; resolveStateRep." That native change was REVERTED (commit 5d14eda) because it
  broke object creation. resolveStateRep has zero hits in tracked source; the blind cast is
  STILL at VrfFacade.cpp:735.

## Tooling

- CLAIMED: StopIface acts with no arguments (it drove a live server RUNNING->UNINITIALIZED
  during a usage probe). FIXED 2026-07-19: requires <restUrl> <stompUrl> --yes, no defaults.
- CLAIMED: the runner/RESUME support -ConsoleLogDir / --console-log-dir. The flag went out
  with revert 5d14eda; WatchVrf rejects it with exit 2, killing the run. Disarmed in the
  runner, removed from docs.
- CLAIMED: RAW / BCON / CONARM trace record types and LogObjectConsoleToFile /
  SetObjectNotifyLevel exist. All went out with revert 5d14eda; WatchVrf emits POS/CON/TSK/RPT.
- CLAIMED (RUNBOOK sec 7): recover a stale federate by "reloading the scenario in the GUI."
  FALSE - recovery is automated (tools/ResetVrf, sec 8). No GUI step on any scored path.
- CLAIMED: the Session Status modal "fires on EVERY clean teardown." INTERMITTENT - named in
  zero of six stopvrf logs (the search cannot see it); four teardowns completed cleanly. The
  nested-dialog fix is UNVERIFIED (never exercised by a real occurrence).

## Run accounting

- CLAIMED variously: "four separate runs", "5 of 6 fallback", "4 of 4 fallback", "one
  teardown failed", "the four-run table" (five rows). Verified: THREE fully unattended runs
  (161438Z, 202349Z, 222134Z); TWO teardowns failed (144109Z, 193252Z); the back-end
  graceful fallback fired on ALL FIVE runs that had the feature; the bridge validation table
  has FIVE rows.

## Model-set default behaviour

- CLAIMED (2026-07-19): "RULED OUT - taskRules/ and scriptedObjectMovement/ are empty."
  WRONG layer. Empty only in C2simEx; C2simEx.sms includes EntityLevel.sms, whose taskRules/
  holds default-task-rules.tsk + doctrines.dct and whose scriptedObjectMovement/ holds 19
  files. None opened. NOT ruled out.

## Birth altitude / the "underground birth" freeze hypothesis

- CONTEXT: the probe branch probe/create-altitude-above-ground (oracle commit b96688b) and the
  port's "Create-altitude mode=Live" raise unit birth from 1000 to 10000 MSL so VRF's
  create-time ground clamp drops each unit onto the terrain surface, curing the historical
  buried-birth.
- ESTABLISHED (2026-07-21, re-derived from run artifacts by the supervisor): this fix was
  ALREADY ACTIVE in the three Jul-19 scored runs, and the frozen units froze anyway. So
  "underground birth" is FALSIFIED as the CURRENT freeze cause.
  Evidence:
  * runs/20260719T161438Z_run/vrfc2simapp.log lines 22-32: all six units incl. 114.MechCoy and
    1.BdeHQ "created at safe MSL 10000 m (original create alt 1000 m); parity post-create
    SetAltitude SKIPPED (born-above-terrain + VRF ground clamp places it on the surface)".
  * watchvrf-trace.csv (161438Z): units clamp to three distinct terrain-following surface
    altitudes - 1222.MechPlt 1040.6 m, 114.MechCoy 1116.7 m, 1.BdeHQ 1131.4 m; zero samples at
    10000, zero negative; altitude tracks sub-meter lon offsets = a real ground clamp. Units are
    on the SIM surface, not buried and not airborne.
  * DISCRIMINATOR TEST: all three taskees got identical treatment (vrfc2simapp.log 48-58:
    CreateRoute 3 pts -> Route created -> MoveAlongRoute issued) at the same birth altitude, yet
    1222.MechPlt moved while 114.MechCoy and 1.BdeHQ froze bit-exact. Same altitude, divergent
    outcome => birth altitude is not the discriminator.
  * Independent corroboration: the 2026-07-16 alt1 experiment (COA-STP1, C++ oracle, apps
    3452/3453/3454) at 10000 MSL birth clamped all units to terrain (~1137 m) and 124/128 still
    froze; only units with executing routes moved; tank 1-1/2/1_AD got MoveAlongRoute + a
    TaskComplete yet stayed bit-exact frozen (its route logged a garbled ~100 MSL start).
- STILL OPEN (the real primary defect): what makes a tasked, surface-clamped ground unit
  execute vs ignore its MoveAlongRoute. Leading un-examined surfaces: ROUTE/WAYPOINT altitude
  (not birth altitude) and the never-opened model-set defaults (see "Model-set default
  behaviour"). The movement model is documented to re-clamp MOVING ground vehicles to the
  surface, which competes with the route-altitude reading and is not yet reconciled.
- RESIDUALS: (a) sim terrain sits ~75 m below real USGS 3DEP terrain at these coords
  (terrain-DB fidelity; does not affect in-sim freeze); (b) the port's primary XML deserializer
  fails on both init and order (Schema102 "error in XML document (1,2)"), a fallback rescues it
  - a separate latent defect; (c) the exact config knob file was not confirmed (a reader cited
  VrfSettings.cs CreateAltitudeSafeMslMeters=10000.0; grep did not find it at that path) - the
  runtime log confirms the behaviour regardless.
- RE-ENTERED A THIRD TIME (2026-09-21, the Iron Storm cut-A diagnostic drive, run
  20260921T114910Z, STP-856): CLAIMED, by the seat and repeated independently in two harvest
  reports (v6harvest/ironstorm_drive_harvest_report.md and the seat's records pass 23): "CAUSE
  SETTLED - the two platforms (28ID, 48 IBCT) never moved BECAUSE they were created ~150 m
  under the terrain." WRONG for the same reason established above: this is exactly the "born
  buried, therefore never moves" form this section falsified in 2026-07-21, re-stated as VRF_
  ALTITUDE_FRAMES.md sec 5's own falsification and sec 7's first tripwire. WHERE IT WAS
  WRITTEN, and withdrawn the same day at every site: docs/experiments/PREREG_IRONSTORM_
  DRIVE_2026-09-21.md ("## RESULT" cause chain and "## RE-RUN (R2)" WHY clause, both now
  carrying dated CORRECTION blocks, the registered R2 text itself left unstruck as history);
  docs/DEMO_READINESS_2026-09-06.md row 16 (reworded to the measurement + the scoped defect);
  docs/HANDOFF_2026-09-14_PARALLEL_LANES.md's extended line (reworded, plus a new CLOSED-list
  tripwire); docs/DEMO_RUNBOOK.md sec 0.4 (reworded); docs/RUNBOOK.md sec 11h (a scope pointer
  added, the section's own mechanism text was already careful and needed no change).
  WHAT STANDS, as a MEASUREMENT: the init terrain query timed out, 36 of 36 creates took the
  FALLBACK altitude, and the two platforms reflected -0.0 m against 145.4 m and 155.8 m of
  terrain on the independent WatchVrf trace for the whole 40-minute run, displacing 0.0 m.
  WHAT IS KEPT, RE-SCOPED: the fallback-altitude create defect itself is real and is what
  STP-856 now names precisely (a hole in the 2026-09-05 birth-altitude cure), separated from
  any claim about why the platforms did not move.
  GENERATOR (Q2/Q3 of VRF_ALTITUDE_FRAMES.md sec 6, the actual regression each time): the seat
  briefed and merged code-lane work in a domain this file and VRF_ALTITUDE_FRAMES.md sec 5 had
  already settled, without reading the canonical record first - the same failure mode Q2
  describes (a refutation written in one place, a stale claim repeated at the point of use) and
  Q3 names (a true narrow finding fused to a causal/design conclusion at the moment it is
  written up). The standing fix, restated: keep the measurement and any causal implication in
  separate sentences, and check docs/VRF_ALTITUDE_FRAMES.md sec 5 and 7 before writing anything
  that puts "buried"/"underground" near "freeze"/"never moves".

## F-1: "STP-857 is a user ruling", and its premise "a MOVE timer-completes by R4" (2026-09-21)
  (Ledger: RL-20260921-03 is the STP-857 exchange, RL-20260921-05 the owner's correction of it,
  RL-20260921-07 the symptom he agrees with, and RL-20260921-09 the temporary position now in force.)

*** THIS ENTRY WAS ITSELF OVER-READ ON ITS FIRST WRITING AND WAS REVISED THE SAME DAY. The
first draft said the ticket had been turned down by the owner outright, and that its premise
was false, without qualification. Both were over-reads, and the owner corrected them: the
SYMPTOM the ticket reports is real and he AGREES with it. What is withdrawn is only the "USER
RULING" label, and what is not approved is the patch AS SCOPED. See "WHAT THE OWNER SAID WHEN HE CORRECTED THIS ENTRY" below before quoting anything
from it. ***

- CLAIMED (2026-09-21, records pass 24, commit 2f52622; RL-20260921-03): that the user RULED on 2026-09-21,
  as STP-857, that "a MOVE task that reaches its armed end with NO displacement reports
  TASKABRT, not TASKCMPLT", NARROWING a rule whose wider form - every dispatched task with a
  Duration is timer-completed, moves included - "WAS the ruled behaviour ... meeting R4".
  THE "USER RULING" LABEL (RL-20260921-03) IS WITHDRAWN, and the 2026-09-14 record (RL-20260914-02) does not support the wider
  form as a ruling. Neither point makes the ticket's underlying complaint wrong.
- WHAT THE PRIMARY SOURCE SAYS. Citations are physical line numbers in the session a7f6a276
  transcript (`~\.claude\projects\...c2simVRFinterfacev2-36\a7f6a276-...jsonl`), each read
  with `rg -n` and the record's `type` checked; the user's own words are `type=user` records
  and are quoted with his spelling.
  * THE QUESTION AS PUT, 2026-09-14 (line 28092, the seat's message, item 4 of six):
    "R4, when a hold ends. SECURE, OCCUPY and DEFEND have no natural completion in
    VR-Forces. Decide whether they complete on arrival, after a duration, or only when a
    later order supersedes them."
  * HIS ANSWER, 2026-09-14 (line 28191, type=user, item 4 in full): "4 given by the end time".
  * THE STP-857 RECOMMENDATION, 2026-09-21 (line 44947, the seat's message): "What should the
    bus say when a move task's timer expires on a unit that never moved? The options are in
    the ticket. My recommendation is option 1: report an abort, 'armed end reached with no
    movement', where the current build reports complete." The clause "where the current build
    reports complete" presents timer-completion of a MOVE as already settled; the 2026-09-14
    record does not establish it.
  * HIS ANSWER, 2026-09-21 (line 45180, type=user): "857 as recommended - just make sure the
    task does involve movement. Not all do."
  * HIS UNPROMPTED MESSAGE THE SAME DAY (line 45457, type=user): "The rulling based on time is
    for tasks that do not include movement, such as defend in place and similar ones - units
    that are not expected to reach any other location. That's what you asked. 'A unit that
    moved 60 m of a 5 km leg gets a complete' is a new interpretation. If I agreed with that,
    I was tricked. ... The notion that geting stuck midway is a complete is completelly
    illogical. ... Arriving late does _not_ imply an abortion - what happens is that follow on
    tasks are delayed by the slow progress on a leg."
  * The 2026-09-21 AskUserQuestion answer "Abandon them" is at line 45418 (a `type=user`
    record carrying the tool result; the question itself is the assistant's tool_use at line
    45417). It was given INSIDE the STP-857 frame and is not a free-standing ruling. His
    second answer in the same exchange was not an endorsement but a question back: "This
    sounds odd. Tasks should include the expected final location if they are indeed moves,
    shouldn't they?"
- WHAT THE OWNER SAID WHEN HE CORRECTED THIS ENTRY. Source: the owner, 2026-09-21, typed
  answers in the fresh-start supervisor session c3b364bd (NOT in transcript a7f6a276); quoted
  with his spelling. Cite it as "owner, 2026-09-21, session c3b364bd, typed answer".
  * ON THE SYMPTOM: "As for 857, a unit that is meanto to move to an objective and perform
    some action, but instead gets stuck in the middle of the way certainly did not complete,
    so not sure why you say the 'premisse was false'. My caveat was that you should not expect
    every task to require a movement, and abort in case the unit stays put."
  * ON THE MOVEMENT / NON-MOVEMENT SPLIT: "The notion of a hold-off or movement tasks seems to
    be rather fuzzy as described. Some verbs imply that the unit stays in place (e.g. to
    DEFEND). There are plenty of tasks where there is movement preceding the desired effect,
    like in 'ATTACK TO SECURE'. How are these distinguished? Seems to me that the time applies
    to the full task, including the movement and the achievement of the desired effect, at
    which point the unit might be tasked to perform a 'follow on' task (not a 'follower')."
  * ON THE LEDGER'S "EARLIER" CLAUSE: "And I hear you about the 'eralier' - this assumed that
    all a unit had to do was complete the movement, but your point is valid, that theres
    action required after that - requires a doctrinal research, and as well as of vendor
    documentation - is the sim able to determine when some of the desired effect has been
    achieved, for example DESTROY (no enemy unit operational in the target area or something
    like that?)."
- SO, SEPARATELY AND IN ORDER. MEASUREMENT: on run 20260921T114910Z two platforms displaced
  0.0 m and both C16 stall TASKABRTs were suppressed by an already-fired timed TASKCMPLT.
  WHAT THE OWNER AGREES WITH: a unit that does not reach its objective has not completed, and
  not every task requires a movement, so a unit must not be aborted merely for staying put.
  WHAT IS WITHDRAWN: the "USER RULING" label on STP-857 (RL-20260921-03), and any reading of R4 as authority
  for what a MOVE task's expiring timer should report - R4 is row 4 of the 2026-09-14 question
  table ("when is a SECURE / OCCUPY / DEFEND complete: on arrival, after its Duration, or only
  when a later order supersedes it?"), and a MOVE was not the question asked. WHAT IS NOT
  APPROVED: the patch AS SCOPED - zero-displacement only, completion semantics for units that
  DID move left unchanged - because it would still report complete for a unit that moved part
  of the way and stuck; his own unanswered question at line 45418 is exactly that point.
  WHAT IS UNDER THE OWNER'S REVIEW: how completion is determined for a task that combines
  movement with a desired effect, pending doctrinal and vendor-documentation research. Until
  he rules, write NEITHER "a MOVE completes on arrival" NOR "time-based completion is for
  non-movement tasks" as a settled rule anywhere. DESIGN IMPLICATION, in its own sentence:
  arming the Duration timer on every task with a Duration (746c091, 2026-09-14) is a code
  defect; its fix is gated on that review (U1) and a PLAN gate. RUN CONDITION, not a finding:
  that run set `Vrf:DurationScale` 0.25, putting the 300 s armed end inside the 360 sim-s
  stall window. STATUS: branch `fix/reclamp-verify-and-movement-only` @ 3d58819 stays PARKED
  and unmerged.
- STILL NOT RULED, either way - do not write any of these as settled: movement-then-hold
  (SECURE/OCCUPY/DEFEND naming a location), patrol, follow/escort, the successors of a stalled
  move, any bound on waiting for a late mover, whether the stall watchdog ships ON, and how a
  task that combines movement with a desired effect is judged complete at all.
- WHERE IT WAS WRITTEN, and corrected the same day at every site (dated CORRECTION blocks; no
  body rewritten away):
  * `docs/DEMO_READINESS_2026-09-06.md` row 19 (RL-20260921-03) - the "2026-09-21 STP-857 USER RULING"
    paragraph struck, CORRECTION block with the verbatim quotes appended.
  * `docs/RUNBOOK.md` sec 11h - both the "RULED 2026-09-21 (STP-857)" sentence and the
    "meeting R4" premise struck; CORRECTION block appended after the paragraph.
  * `docs/HANDOFF_2026-09-14_PARALLEL_LANES.md` :200 - status corrected in the line ("needs a
    ruling" -> label withdrawn, symptom agreed, patch-as-scoped not approved, branch parked,
    semantics under the owner's review).
  * `docs/experiments/TASK_VOCABULARY_ASSESSMENT_2026-09-14.md` - SCOPE CORRECTION block under
    "R4 RULED", and a scope pointer at "R4 BUILT".
  * `docs/experiments/PREREG_IRONSTORM_DRIVE_2026-09-21.md` - CORRECTION POINTER at the P10
    "NEW FINDING, filed as STP-857" paragraph, covering the R2 text's "STP-857 recurrences"
    limb as well; the registered text itself left unstruck as history.
  * `src/VrfC2SimApp/appsettings.Demo.json` `_TimedCompletion` - the comment opened "R4 (user
    ruling 2026-09-14)" over every dispatched task (RL-20260914-02); a SCOPE CORRECTION now leads it. The
    setting VALUE is unchanged.
  Not this lane's, listed so the gap is visible: the memory file
  `project-demo-rulings-2026-09-21` item 6, the Jira STP-857 text, and `docs/RULINGS.md`.
- GENERATOR (the same shape this file keeps recording): a supervisor's paraphrase was stored as
  the ruling, a later question was built on the paraphrase, and the answer to that question was
  then written up as a ruling of its own. The standing fix: a ruling is recorded as the
  QUESTION AS PUT plus the user's VERBATIM words with a locatable citation, and a supervisor's
  reading is labelled "supervisor reading:" and kept in its own sentence.
  (Ledger: RL-20260921-03, and RL-20260921-07 for the symptom he does agree with.)
  (Ledger: RL-20260914-02 is the 2026-09-14 record this paragraph says does not support the wider reading.)
  (Ledger: RL-20260921-03 for the withdrawn label; RL-20260914-02 for what R4 actually was.)
  (Ledger: RL-20260921-03. That row now carries the id too.)
  (Ledger: RL-20260914-02 for the 2026-09-14 answer this comment cited.)

## F-2: shipped comments naming setAltitude as the placement correction, and fusing burial with a stationary vehicle (2026-09-21)

*** CORRECTION 2026-09-25 (U2 lane F). The banner below says the keep/remove decision is OPEN.
That is stale: the owner answered on 2026-09-21 at 19:08Z (session c3b364bd line 555, typed),
after the banner was written, and the seat's reply that the placement stays drew no objection.
Supervisor reading, binding nobody: the placement stays; the dispatch gate and the false tally
are the defect, to be fixed in a PLAN-gated code unit; no revert of 8aeb127. His words and the
reading: RL-20260921-06, now in docs/RULINGS_ARCHIVE.md. The banner below is kept as history. ***

*** CORRECTION 2026-09-21 (U2 lane D, the same day). An earlier version of this banner said the
placement re-clamp "is being REMOVED, not switched off" as an OWNER DECISION. That was an
over-read of a half-answer. THE KEEP/REMOVE DECISION IS OPEN AND IS WITH THE OWNER. He wrote "If
so it should be removed, not switched off" and, in the same answer, "But I may have misread what
you said"; asked again he replied "Confirm first that my interpretation of what you described is
correct"; the seat confirmed on half the evidence and he then wrote "This smells of instrument
error". The seat retracted the confirmation. The three live options are: remove it all; keep the
placing and drop the refuse-to-task gate and its tally; leave it until a re-cut. Nobody may
record a keep-or-remove decision, or revert 8aeb127, until he gives one. His words in full, the
measurement behind them and what is NOT established: docs/RULINGS.md RL-20260921-06.
The comment corrections below stand on their own. The two record defects they document - naming
setAltitude for a setLocation, and fusing burial with a stationary vehicle - are the lasting
content of this entry whatever happens to the feature. ***

- CLAIMED (2026-09-21, commit 8aeb127, in files that SHIP): that a fallback-created object is
  "corrected with the documented setAltitude(0 m AGL)". WRONG about the code. The re-clamp
  calls `_bridge.SetLocation` - `src/VrfC2SimApp/VrfC2SimService.cs:3433` (the sweep) and
  `:3559` (the route-terrain path), both verified by reading the lines. The vendor headers are
  why: `setAltitudeRequest.h:23-25` "It is ignored if the vehicle is not an air-going vehicle";
  `setLocationRequest.h:26-32` "Z is ignored for non-air vehicles ... Ground vehicles will be
  clamped to the terrain surface". `src/VrfC2SimApp/VrfSettings.cs:875` and
  `PlacementReclampPolicy.cs:163` already said "THE CORRECTION IS A setLocation, NOT A
  setAltitude" in the same commit - the two settings comments simply did not get the message.
  CORRECTED IN PLACE: `src/VrfC2SimApp/appsettings.json` `_PlacementReclamp` and
  `src/VrfC2SimApp/appsettings.Demo.json` `_PlacementReclamp`. Not corrected by this lane,
  reported instead: `src/VrfC2SimApp/DispatchReadiness.cs:202` emits the same wording in an
  OPERATOR-FACING log string ("The documented correction (setAltitude 0 m above ground level)
  has been issued"); it is a code string, not a comment, so changing it is a behaviour change
  and belongs to the lane that re-cuts the re-clamp. (2026-09-25: GONE - the state and its sentence were
  retired by the completion unit; see F-4.)
- CLAIMED (same commit, `appsettings.Demo.json` `_PlacementReclamp`): the setting "IS THE
  DIFFERENCE BETWEEN A DEMO AND A STATIONARY VEHICLE", and without it a unit is "shown to an
  audience sitting still under the ground". This is the falsified burial-to-no-movement fusion
  in a shipped file - the third re-entry of the form this log's "Birth altitude" section and
  `docs/VRF_ALTITUDE_FRAMES.md` sec 5 falsified, and exactly what sec 7's first tripwire
  forbids. The same commit's own `appsettings.json` text says "THIS IS NOT A FREEZE FIX", so
  the two shipped profiles contradicted each other. Records pass 24 named the Demo line as a
  candidate and left `src/` untouched. CORRECTED IN PLACE 2026-09-21 (U2 lane A): the fusion is
  gone, the measurement (0.0 m displacement, -0.0 m against 145.4 m and 155.8 m of terrain) is
  kept, and the justification is stated separately as the placement contract (UG52 14.3.3) plus
  the duty not to task a unit the interface has itself measured off the ground.
- ALSO CORRECTED: `src/VrfC2SimApp/VrfC2SimService.cs:3328` read "The two buried platforms
  happened not to move; that is the only reason it did not bite." Now states the measurement
  (0.0 m displacement) and says explicitly that WHY they did not move is not established and
  that the guard rests on the 0.0 m, not on any reason.
- LEFT ALONE DELIBERATELY, with the reason, so the next sweep does not re-litigate them:
  `docs/START_HERE.md`:145 ("3 movers ran 49-135 km away and terminated underground/offshore,
  rest never moved") describes two disjoint groups and makes no causal claim;
  `docs/VRF_GROUND_TRUTH.md`:881 is an OPEN QUESTION to MAK about a stationary entity created
  below terrain, not an assertion; every other hit of the word pair is inside a falsification
  or CORRECTION site (this file, `VRF_ALTITUDE_FRAMES.md`, `CLAUDE.md`, `DEMO_RUNBOOK.md`:100,
  `HANDOFF`, `DEMO_READINESS` row 16, `PREREG_IRONSTORM_DRIVE`:505, `VrfSettings.cs`).

## F-3: abort-then-complete recorded as a ruling at the 2026-09-14 experiment sites (2026-09-25)
  (Ledger: RL-20260914-01 and its scope note; RL-20260921-09 the temporary completion position now in force.)

- CLAIMED (2026-09-14 experiment records): abort-then-complete / "a later TASKCMPLT is never
  suppressed" recorded as a ruling ("the C16 ruling") - it is a SUPERVISOR reading (RL-20260914-01
  scope note: that entry covers only the TASKABRT code). Dated corrections added in place at:
  `docs/experiments/FIXPASS3_RULINGS_1ffb4ee_2026-09-14.md`:202;
  `docs/experiments/PREREG_V13_REPORTING_WATCHDOG_2026-09-14.md`:10 and :41;
  `docs/experiments/REPORTING_ASSESSMENT_2026-09-14.md`:288;
  `docs/experiments/REVIEW2_RULINGS_0c96f50_2026-09-14.md`:270 (head of sec 2.3, table row :281);
  `docs/experiments/REVIEW_INTEGRATION_02b51de_2026-09-14.md`:91;
  `docs/experiments/REVIEW_RULINGS_8db033e_2026-09-14.md`:268 (head of sec 2.5, table row :281).
  Also `docs/DESIGN_ORBAT_TO_VRF_2026-09-06.md`:405 "supervisor ruling 2026-09-13" now reads
  "supervisor position 2026-09-13", with the same dated correction at :412 (RL-20260914-01, its scope note).
- OWED to the next code unit (src comments still say "C16 ruling" - RL-20260914-01 does not cover
  it): `src/VrfC2SimApp/TaskStatusPolicy.cs`:28, `src/VrfC2SimApp/VrfC2SimService.cs`:164 and
  :7491, `src/VrfC2SimApp/ReportSelfTest.cs`:97.

## F-4: "a task completes at its Duration even if its unit has not arrived" and "a taskee measured off the terrain is HELD" - true of main until 2026-09-25, no longer (2026-09-25)
  (Ledger: RL-20260921-09 is the temporary position built here; RL-20260921-06 the re-clamp answer; RL-20260925-01 the
  owner's approval of this unit's scope and his four decisions, being added to the ledger by another lane.)

This is not a refuted MEASUREMENT: both statements were accurate descriptions of main. They became false when
the completion unit landed (branch `feat/completion-temporary-position`, commits 60e45a6, 2338e16, 02e604d;
docs in the commit that adds this entry). LIVE CONFIRMATION IS OWED (a pre-registered run, the seat's step).
- WHAT CHANGED, completion (the owner's TEMPORARY position, RL-20260921-09): an early finish is HELD to start time
  + Duration; a task WITH a destination whose unit is still travelling at that time is logged OVERDUE, reports
  nothing, releases nothing, and is reported TASKCMPLT the moment the unit arrives (its follow-ons wait, up to
  `Vrf:TaskChainBackstopSeconds` from its dispatch); a task with NO destination ends at its Duration as before.
  The stall watchdog's TASKABRT is REPORT-ONLY for the task (it no longer cancels the end time) and now ABANDONS
  the stuck unit's follow-ons (the owner's decision D2); a late ATTACK/BREACH move still gets its parked engage
  and reports TASKCMPLT on arrival (D4); stall detection is ON in `appsettings.Demo.json` and OFF by default (D3).
  Back-end loss also aborts tasks held after an early finish.
- WHAT CHANGED, re-clamp (RL-20260921-06; D1, including dropping the dispatch-time setLocation): the dispatch gate
  measures and LOGS only - no hold, no refusal, no setLocation at dispatch; `TaskeeReadiness.NotOnTheGround`
  (BOUND-BUT-NOT-ON-THE-GROUND) is retired, and with it F-2's open item, the "setAltitude 0 m above ground
  level" sentence at `DispatchReadiness.cs:202`; the summary has five counts, and a correction whose read-back
  never landed is "CORRECTED, READ-BACK NOT RECEIVED", not NEVER MEASURED (the "32 NEVER MEASURED" of run
  20260921T143243Z). Why the read-back never landed stays UNDIAGNOSED.
- WHERE THE OLD DESCRIPTIONS WERE, corrected at every site in the same commit series: HANDOFF sec 1 (TASK
  COMPLETION + re-clamp lines) and sec 1/6 status lines; DEMO_READINESS row 19 (UPDATE block) and new row 26;
  RUNBOOK sec 11 (a TimedCompletion bullet) and sec 11h (UPDATE block + the gate, tolerance, lines-to-look-for,
  sequences and offline-proof sites); TASK_COMPLETION_RESEARCH_2026-09-21 (banner, summary-table note, and an
  UPDATE line in each "code today" cell whose statement changed); in src: the TimedCompletionPolicy header, the
  VrfSettings TimedCompletion / StallDetection / re-clamp comments, the appsettings.json and appsettings.Demo.json
  `_TimedCompletion` / `_PlacementReclamp` / `_PlacementReclampToleranceMeters` texts, PlacementReclampPolicy,
  DispatchReadiness, Program.cs, and the four "C16 ruling" comment sites, relabelled "supervisor position
  (RL-20260914-01 covers the code only)".
- NOT CHANGED, and stated so nobody assumes it: the runner scores a task by its FIRST terminal code
  (`scripts/RunnerLib.ps1` terminalByTask), so a stall-then-arrive task is scored TASKABRT; with stall detection
  OFF a stuck unit never goes terminal and a `-StopWhenComplete` window runs to its cap; tasks with no Duration
  keep evidence-only completion; whether a desired effect was achieved is ignored (the research question).

## Process

- The single-auditor repair loop (rounds 1-7) did not converge: like-for-like orchestrated
  audits found 26 then 29 defects, because each repair pass added correction layers that were
  themselves defect-prone (mis-scoped fences, corrections after the text they retract,
  headlines outliving bodies, one fabricated finding). The entry points were rewritten clean
  2026-07-21 to break that loop. LESSON: state the current truth in the live doc; keep
  provenance HERE; do not stack retractions in a document a fresh reader must act on.

## The entity freeze / "nav data ruled out" (resolved 2026-09-01)

- CLAIMED (2026-07-14, nav-data falsification; repeated in UNIT_MOVEMENT_RESEARCH sec 6
  and MOJAVE_ROOTCAUSE): "nav data is NOT the Mojave cause - Sweden marches with none."
  TRUE for MISSING nav data, but the same 2026-07-14 session GENERATED a 120,002-tile
  NavArea over the Mojave AO and left it in SharedData/16/latest/TerrainData/navData/.
  From 2026-07-15 every run loaded it, and ground units whose movement consulted it
  waited forever ("Waiting for nav data to load", Info-level, invisible at
  objectConsoleNotifyLevel 1). The freeze the falsification left "unexplained" was
  CREATED BY the falsification session's own instrument. Evidence: P1 RUN 2
  (20260901T191004Z: 12,100 waiting lines from 1.BdeHQ, bit-static) and P1c
  (20260901T194029Z: artifact moved aside -> the entity DROVE ITS ROUTE AND COMPLETED).
  The artifact now sits in navData/_disabled_20260901/ (restorable).
- CLAIMED (HANDOFF_2026-07-23): "no entity move has ever been proven through the
  interface." FALSE - 1.BdeHQ reached its Mojave route end on 2026-07-13 (pre-artifact)
  and at Sweden repeatedly; corrected in the handoff's read-first note 2026-09-01.
- CLAIMED (2026-09-01 morning, RESEARCH_MECHANISMS sec 4b): "the entity freeze
  correlates with GroundWaypointAltitudeMode=Live" (H-ENT-1). FALSIFIED the same day by
  P1 RUN 2 (frozen under Fixed100, all gates met). The Live default and the NavArea
  artifact landed on the same day (2026-07-14/15) - a textbook confound.

## The region hypothesis / "the leader path plan is EMPTY at Mojave" (retracted 2026-09-02)

- CLAIMED (2026-07-13, R9 region swap; evidence docs/experiments/R9_region_swap_2026-07-13.txt:32-35;
  written up in UNIT_MOVEMENT_RESEARCH sec 4c): "at the COA-STP1 Mojave region VR-Forces cannot plan
  unit movement paths - the back end logs `moveAlong() - empty route -- not sending move along to
  subordinate` three times per aggregate and creates ZERO member Offset Route objects, against 45 in
  the same-day Sweden control; so the REGION / streamed terrain content is the aggregate blocker, and
  it is NOT an interface defect." RETRACTED. The region is not the cause.
- FALSIFIED 2026-07-22 by docs/experiments/PREREG_FIXTURE_REGION_VS_STRUCTURE_2026-07-22.md. An
  AUTHORED, structurally complete Tank Platoon loaded from a .scnx at the SAME Mojave AO drove its
  route: ":203-205 Disaggregated-move MECHANISM engaged: reflected 9 -> 13 at onset = 4 new
  offset-route/control transients - the SAME buildOffsetRoute path R9 reported EMPTY (0 offset routes)
  for our REMOTE-CREATED units at this same AO"; ":209-211 INTERPRETATION - the region hypothesis
  (Branch A) is FALSIFIED. Mojave terrain does NOT fundamentally break disaggregated movement for an
  authored, structurally-complete Tank Platoon. R9's '0 offset routes at Mojave' is therefore NOT a
  property of the terrain; it is a property of what our interface CREATES/TASKS there." The
  below-terrain-waypoint control ran in the same experiment and killed the other environmental
  candidate: ":287-289 CONSEQUENCE: WAYPOINT ALTITUDE (below-terrain clamp-up) is FALSIFIED as a cause
  of the R9 freeze. Both environmental hypotheses for the empty-offset-route freeze are now DEAD:
  REGION (Mojave terrain) and WAYPOINT ALTITUDE."
- WHY IT WAS WRONG (mechanism, not just the counter-example): R9 was observed with the TYPE-MAPPING
  layer still in place. Remote-created aggregates emitted 11.1.225.1.1.3.0, which has no Kind-11 leaf,
  fell back to Ground_Aggregate, and therefore had NO MEMBER SET for buildOffsetRoute to build routes
  for. The empty offset route was a property of the object this interface created, not of the ground
  under it. Type mapping was fixed 2026-07-22 (UnitTranslator RealTemplates, now the default); three
  further layers were peeled 2026-09-01/02 (this project's own generated NavArea artifact, the
  cosmetic HQ formation-name warning, the route-vertex altitude frame). See
  HANDOFF_2026-09-01_R9_COMPLETE.md "THE FOUR-LAYER BLOCKER STACK". Every July-era falsification is
  LAYER-RELATIVE (lesson L9) and must be re-adjudicated on the clean state before its fence is trusted.
- THE RETRACTION DID NOT PROPAGATE for six weeks. As of 2026-09-02 the region story was still stated
  as CURRENT in six live docs; each now carries an inline
  "[RETRACTED 2026-07-22 - the REGION cause is FALSIFIED; see docs/CORRECTIONS_LOG.md.]" pointer at
  the cited passage:
    docs/COA_GPT_FEEDBACK.md Item 4 (a full RETRACTION NOTICE at the head of the item; INTERNAL - the
      user decides whether coa-gpt is ever told)
    docs/SCENARIO_SETUP_GUIDE.md :35, :90, :130
    docs/PORT.md :10, :625, :757-765
    docs/UNIT_MOVEMENT_RESEARCH.md :297, :562, :608
    docs/START_HERE.md :191, :346-370, :638
    docs/NEXT_SESSION_GUIDANCE.md :60-61
  (line numbers as they stood before the tags were inserted). The bodies are NOT rewritten - they are
  history and must read as history.
- WHAT IS **NOT** SETTLED. Retracting the region CAUSE does not assert the SYMPTOM is gone. Whether
  COA-STP1's own remote-created aggregates now build member offset routes at Mojave under the clean
  state is an OPEN question, pre-registered as prediction P3 of
  docs/experiments/PREREG_COASTP1_RUNG1_BOUNDED_2026-09-02.md. Nothing here licenses a claim that
  aggregates march at Mojave until that run is scored.

## "Route-name length is falsified" (superseded 2026-09-02, pending the manipulation probe)

- CLAIMED (docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md part 12, :1192-1215,
  2026-07-16): name length is among the exhausted/falsified candidates for the mover/frozen
  split - "Route names pass through VR-Forces at up to 99 chars UNTRUNCATED", and the
  10-char truncation is the C++ interface's OWN, applied at XML parse time. SUPERSEDED, not
  simply wrong: what part 12 tested was the CREATION path and the 10-char marking
  collisions. It never inspected the route name inside the move-along TASK, which is a
  DIFFERENT transport.
- WHY THE DISTINCTION MATTERS (mechanism, docs/experiments/ANALYSIS_COASTP1_RUNG1_FREEZE_2026-09-02.md,
  commit 04bcc0f): VrfFacade::CreateRoute passes DtString(name.c_str()), unbounded
  (src/VrfFacade/VrfFacade.cpp:529-534) - so the 99-character route OBJECT really does exist
  intact in the back end, exactly as part 12 says. VrfFacade::MoveAlongRoute passes
  DtUUID(routeUuid) (:569-571), and DtUUID is a FIXED 36-byte blob, 1 type byte + 35 payload
  (C:\MAK\vrforces5.0.2\include\vrfutil\rwUUID.h, `char myData[36]`). A route name over
  34 characters therefore reaches the aggregate cut to 35 with no terminator, resolves to no
  object, and no member offset route is ever built - silently. Observed on run
  20260902T125423Z in 9 of 9 performers with no exception: <= 34 chars marched, >= 36 chars
  froze; T27's in-task name is visibly unterminated with trailing junk
  (bin64-vrfSim.log:52979).
- The 10-char marking-collision finding of part 12 is NOT reopened; it stands.
- Part 12's own heading now carries an inline [SUPERSEDED 2026-09-02 for the TASK path ...]
  pointer, so the retraction does not sit only here (the six-week non-propagation lesson).
- STATUS: SUPERSEDED PENDING THE MANIPULATION PROBE. The truncation is INFERRED from the
  header plus the observed 35-character cut; no instrumentation proves the buffer. The
  single-variable A/B probe (same geometry, 30-char vs 40-char route name) is registered as
  docs/experiments/PREREG_ROUTE_NAME_LENGTH_2026-09-02.md. Until it scores, this entry
  supersedes part 12 for the TASK path only and leaves the CREATION path claim standing.
  Falsifier that would reinstate part 12 wholesale: a performer with a >= 36-character route
  name that builds offset routes and marches, or a <= 34-character one that builds zero while
  its route object is present.

## "Logs stamp UTC" applied to the VENDOR log (retracted 2026-09-02)

- CLAIMED (docs/RESUME_PROMPT.md:98, and the same blanket phrasing echoed wherever a time
  base is discussed): "Logs stamp UTC; the machine runs local (-04:00)." TRUE of OUR OWN app
  and tool logs - WatchVrf and ListenReports stamp DateTime.UtcNow, and the runner's manifest
  clocks are UTC - but FALSE of the VENDOR log. bin64-vrfSim.log's wall stamps
  (enableLogFileTimestamps 1) are LOCAL TIME, i.e. UTC-4 on this box.
- EVIDENCE (docs/experiments/PREREG_R9_FIXED_FRAME_RTC_2026-09-02.md sec 8, item C1): Row 3's
  first vendor stamp reads 07:38:38 against an orderPushedUtc of 11:38:37Z, and the FFRTC
  run's reads 10:08:24 against a startUtc of 14:08:08Z. Offset -4 in both, from two
  independent runs.
- CONSEQUENCE: any cross-reference of a vendor-log stamp to a UTC artifact must convert.
  Measurements built from stamp DIFFERENCES (every ratio in that prereg) are unaffected.
  docs/RESUME_PROMPT.md and docs/VRF_GROUNDWORK_PLAN.md L5 now carry an inline
  [RETRACTED 2026-09-02 ...] pointer at the claim. docs/RUNBOOK.md does NOT state it - checked
  and clear. Memory files were not edited (user owns them).

## COA-STP1 order/init arithmetic (corrected 2026-09-02, all re-verified from the XML)

- CLAIMED (OPUS_EXECUTION_PLAN.md:724 and :739, PLAN_DERISK_NOTES.md:81,
  UNIT_MOVEMENT_RESEARCH.md:437): the order has "32 temporal deps". WRONG: 31. data/COA-STP1_Order.xml
  contains exactly 42 ManeuverWarfareTask and 31 ActionTemporalRelationship elements, all with
  ActionTemporalAssociationCode STREND. 42 tasks - 11 chain heads = 31 dependent tasks, each with
  exactly one predecessor: 10 performers carry chains of 4, one (510/40) carries a chain of 2.
  Heads: T1, T5, T9, T13, T15, T19, T23, T27, T31, T35, T39. Those four sites are corrected; the two
  remaining "32" mentions (PORT.md:602, START_HERE.md:500) describe what a PAST RUN did and are left
  as written history - the order they describe still had 31.
- CLAIMED (HANDOFF_2026-09-01_R9_COMPLETE.md:126): COA-STP1 scale means "13-40 km routes". Understated
  at both ends. Measured by haversine over each task's inline Location list: longest SINGLE route
  T17 = 42.37 km; longest CHAINED total for one performer 1-6/2/1_AD = 77.92 km (T15 35.55 + T17
  42.37). Full census of tasks with a route: T17 42.37, T39 40.20, T15 35.55, T23 28.71, T31 28.71,
  T35 28.71, T1 28.53, T19 28.53, T32 23.60, T13 0.63, T36 0.63. The remaining 31 tasks carry 0 or 1
  Location and have no route length; the 9 with ZERO Locations are T8, T9, T10, T16, T21, T24, T34,
  T37, T38. Corrected in the handoff.
- INCOMPLETE (NEXT_SESSION_GUIDANCE.md:158): "T13/T19 are not even temporally gated". True - both are
  chain heads - but T13 carries the order's ONLY start delay,
  StartTime/SimulationTime/DelayTimeAmount/IsoTimeDuration = P00Y00M00DT03H20M00S (12,000 s), at
  data/COA-STP1_Order.xml:504. All ten other heads carry P00Y00M00DT00H00M00S and no task carries a
  nonzero RelativeTime. So T13 cannot dispatch inside any run window shorter than 3 h 20 m at
  TimeMultiplier 1; a T13 that does not dispatch in a 45-minute window is EXPECTED, not a miss. The
  guidance line now says so.
- RE-VERIFIED and CORRECT as written (no change needed): data/COA-STP1_Initialization.xml has 128
  Unit elements and 35 TacticalArea elements; 67 units are hostile (SIDC char 1 = 'H') and 61 friendly
  ('F'); all 128 are ground (SIDC char 2 = 'G') and all SISOEntityType leaves are zero. SIDC echelon
  char (index 11) census: 'E' 64 -> ArmorCompany (aggregate), 'F' 26 -> ArmorCoHQ (aggregate), 'D' 23
  -> ArmorPlatoon (aggregate, the only branch the 2026-07-22 type fix touched), 15 others ('-' 12,
  'C' 2, 'H' 1) -> the lone-Tank default. So 113 aggregates + 15 entities. 54 units - including all 11
  order performers - share the single spawn coordinate 34.67998497, -116.72479854.
