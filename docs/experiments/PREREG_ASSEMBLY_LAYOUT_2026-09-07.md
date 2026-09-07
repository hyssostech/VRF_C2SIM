# PREREG: spacing co-located units at startup (assembly-area layout) - 2026-09-07

Tier: HEAVY (a cause claim rides on it: "the residual stall is the start-point crowding").
Gate: PREREG (this file, before the run). User ruling 2026-09-07 ~00:20Z: "I need to bite my
tongue and accept trying to space out the entities at startup, given the evidence of the
triangular position you now cite." This SUPERSEDES the R8 ruling of 2026-07-13 for 5.2: that
ruling was made on 5.0.2 (no vehicle-vehicle avoidance) and against a different symptom.

## 0. Vendor anchors (sample > docs > our runs)
- SAMPLE: the shipped remote-control sample never stacks objects - it offsets even one
  platoon's four tanks by 10 m from each other, "to create an initial triangle formation"
  (examples/remoteControl/commandLineRemoteController.cxx:756-770).
- DOCS: UG52 23.2.3 - avoidance "is not planning a path through the obstacles it sees ...
  the vehicle could become trapped ... by moving entities that close off its path"; UG52
  25.2.1 - a unit created from the panel gets its members laid out in its default formation
  (one unit per point, never two). Shipped formation files
  (data/simulationModelSets/EntityLevel/vrfSim/formation): Formation-Column-Armor-Co(US)
  spans x -430..+200 m (630 m long, platoon columns +/-50 m wide); Wedge/Line/Vee-Armor-Co
  span 0..300 x, +/-200 y; Ar_Plt_US_* +/-50 m; Ar_Co_HQ_* -100..0 x, +/-50 y.
- SCRIPT: ground-vehicle-move-to.lua - a vehicle stopped by another for 10 s is a blockage
  (back up 2 lengths, skirt 5), MAX_REPLANS = 3, one global replan, then "Loop to stall for
  replanning" (doWhile = true) - a stalled vehicle never untangles by itself.
- OUR RUNS (PREREG_ORDER_TIME_MATERIALIZATION 3.3/3.4): 4,828 "BlockedByVehicle" messages in
  the first 300 s after tasking; 5/9, 8/9, 8/9 tasked units beyond 1 km; the stuck composed
  company waited on move-into-formation from one sub-unit that had stalled in the pile.

## 1. The lever
The existing DeStacker (hex rings, first unit kept in place, adjacent slots `spacing` apart;
`--destack-selftest` PASS on the deployed build 2026-09-07) applied at init to every group of
units sharing a coordinate, with Vrf:DeStackSpacingMeters = 700: larger than the longest
shipped company formation (630 m) so no two units' default formations can overlap whatever
their heading. 54 units at STP's point -> rings 1..4 (60 slots), outer radius 2.8 km - the
size of a brigade assembly area. The stored order-time plans carry the spread positions
(review fix H, 13ac3c3), so members materialize where their shell is. Context shells (no
vehicles) take slots too; harmless, and the GUI shows a laid-out ORBAT.
Env for the run: Vrf__DeStackCreates=true, Vrf__DeStackSpacingMeters=700 (no runner switch;
the app logs "DeStack (R8): N units at (lat,lon) spread onto 700 m rings").

## 2. The run
Same as the completion run (234525Z): AtOrder, FidelityTable + scratch no-lifeform map,
R9_Mojave_Empty_52, -NoGui, RunSecs 2700, -StopWhenComplete, unit consoles at 4 AND member
consoles at 3 (needed for the blocking count; the 1 M-row cost was 1.84x in 3.3). ONE variable
against 234525Z's configuration: the spacing (plus member consoles, which 3.3 showed do not
change movement).

## 3. Predictions (written before launch)
- P1 "BlockedByVehicle" messages over the whole run < 500 (co-located: 4,828 in 300 s).
- P2 9 of 9 tasked units beyond 1 km (co-located: 5/9, 8/9, 8/9).
- P3 the composed tank company C/1-35 logs "task complete msg rcvd from" ALL FOUR sub-units
  and advances to maneuver-along (in 231401Z it never received TANK2's).
- P4 at least one TASKCMPLT within 2700 s (first legs 24-45 km; at >= 1.8x a 28 km leg is
  ~1,500 s wall). Medium confidence: the exact leg speed is not measured.
- P5 the app log shows the DeStack line with 54 units spread onto 700 m rings, and the
  materialized members are created at the spread positions (PLACEMENT lines).
FALSIFIERS: P1 miss with P5 met -> the crowding is not (only) at the start point, or 700 m is
not enough - read the members' consoles, no theory first. P3 miss with P1 met -> the
composed unit's stall has another cause; that unit's console decides. P2/P4 misses with P1
and P3 met -> movement is limited elsewhere (route, terrain, speed) - measure before
claiming.

## 3b. FOLLOWER-TRACK RUN (user: "Go", 2026-09-07 ~10:40Z) - registered before launch
Question: what does the LAST member of a unit that never reports completion do after the
others arrive. Same configuration as the spaced run with: Vrf:PositionReportSeconds=10 (the
R1 reports, the demo default; ListenReports captures them), -WatchSecs 3000 (the observer
covers the whole 2700 s window), unit consoles at 4, MEMBER consoles at 0 (fatal only: the
member -> unit map is still logged, the app log stays small - last night's 1.2 GB came from
level 3), runner launched from a 64-bit pwsh (the 32-bit tool host died at 176 MB).
Predictions: P6 >= 2 TASKCMPLT again (the report path is proven). P7 every unit without a
TASKCMPLT at the end shows "Subs still moving: 1" and exactly one member > 1 km from the
unit's other members. P8 (the decision) that member's last-15-minute track is ONE of:
STUCK (net movement < 100 m), CIRCLING (returns within 200 m of where it was), CLOSING
(monotonic approach to the others). Falsifier of "vendor quirk": the straggler is heading
for a vertex OUR route authoring placed (compare its heading/goal with the route's last
vertices, which the app log carries) - then the defect is ours.

PRE-READ ON LAST NIGHT'S DATA (tools/analysis/straggler_track.py on 003457Z, trace to t =
3,258 s, last-15-min window) - written before the new run reports, as a prior, not a result:
- 4-27/2/1_A~PXY: straggler M3 2 at 22.7 km, 7.0 km behind the others; window net 1 m,
  path 603 m -> STUCK (oscillating in place).
- 40/2/1_AD~PXY: straggler M577A2 5 (the command-post vehicle) 1.26 km behind; net 0 m,
  path 222 m -> STUCK (oscillating).
- 1-6/2/1_AD~PXY: the opposite split - HMMWV 7 drove the whole 33.5 km leg ALONE while the
  other five members stayed at 0.8-1.6 km from birth; the leader finished, the followers
  never followed; net 0 at the end -> STUCK.
So the prior for P8 is STUCK/oscillating, in two shapes: a follower left behind mid-route
(vehicle-specific: an M3, an M577), and followers that never left with their leader. Neither
is a route-authoring shape (the routes are the units' 24-33 km legs; the stragglers are
kilometres from any vertex WE placed, not at one). If the new run repeats this, the demo
answer is interface-side: report a unit's completion from the unit's OWN arrival evidence
(leader / majority at the route end) instead of waiting for the vendor's all-members report
- a POLICY the user rules on, since it changes what "complete" means to STP.

RULING RECEIVED WHILE THE RUN WAS UP (user, ~11:00Z): "report a unit's completion from the
unit's own arrival evidence is fine". BUILT: ArrivalPolicy.cs (majority within 500 m of the
last vertex; --arrival-selftest 9/9), MaybeCheckArrivals on the tick thread, the vendor's
later completion swallowed once; DESIGN_ORBAT C15. NOT in this run (deployed after it); the
run after this one verifies it: prediction P9 = every unit whose majority reaches the leg
end reports TASKCMPLT within ~35 s of arriving, and the lone-leader case does not.
INCIDENT (11:10Z): a self-test invocation on a STALE scratch build fell through to the host
(the flag was unknown to that build) and joined the federation beside this run for five
minutes until the tool timeout killed it; the run's object count never moved (342) and its
log shows one init and one order - no contamination. Guard added: an unknown "--" switch
never starts the host (exit 2), verified.

### 3b RESULTS - run 20260907T103246Z (runner exit 0 with a proper teardown this time; trace
35 MB to t = 2,770 s covering the whole window; 69,314 position reports captured; ratio 1.88x)
- P6 HOLDS: TASKCMPLT x2 (856/HHC~PXY, B/5-20~PXY) - the vendor's own completions, again for
  two units.
- P7 HOLDS: every unit without a completion that has a straggler shows exactly one member far
  from the others: 1-6/2/1_AD HMMWV 7 (28.9 km from its five followers - the SAME vehicle as
  last night: the leader drives the leg alone, the followers never leave), 40/2/1_AD M577A2 5
  (1.1 km back - the SAME command-post vehicle as last night), 1-35/2/1_A M3 1 (1.0 km back),
  C/1-35 M577A2 7 (3.7 km back, the company HQ's M577; 17 of 18 members together), 5-20/2/1_A
  M1A2 32 (2.9 km back and CLOSING at ~100 m/min). 4-27/2/1_A and 1-1/2/1_AD: all members
  within 1 km of each other, no completion at the cap (their legs are 33.5 / 28.7 km).
- P8: STUCK in four of five (net 0-4 m over the last 15 min with 14-799 m of path = oscillating
  in place), CLOSING in one. Two of the stuck vehicles are the same vehicles in the same units
  as last night -> reproducible, vehicle-type-specific vendor behaviour (an M577A2 command post
  twice, an M3 twice across the two runs, a lone HMMWV leader twice), not chance. None is near
  a vertex we authored (the route-authoring branch of the decision is out).
- A/6-56/HHC (the ADA battery, four SAM launchers): never moved - its task carries no route
  points (an air-defence coverage verb), so no move was issued; not a straggler case.
DECISION (already ruled and built while the run was up): completion from the unit's own
arrival evidence, majority within 500 m of the last vertex. Applied to THIS run's end state
the rule would have reported 40/2/1_AD (5 of 6 within), 1-35/2/1_A (5 of 6), C/1-35 (17 of
18) and, once the M1A2 closes, 5-20/2/1_A; it would NOT report 1-6/2/1_AD (leader alone) -
the correct answer in every case. P9 (next run, new build) tests exactly that.
CORRECTION FROM THE DESTINATION CHECK (straggler_track.py against each task's last point):
at the 2,700 s cap the non-completing units were NOT yet at their destinations - centroid
1.4 km (1-35), 1.8 km (40/2/1_AD), 7.6 km (5-20), 9.2 km (4-27), 9.8 km (1-1) and 29 km
(C/1-35, 1-6) from the last vertex, 0 members within 500 m - while the two vendor-completed
units sat 5-6 m from theirs with 4 of 4 members within 500 m. So in a 45-minute window the
majorities had simply not arrived (legs of 24-33 km at ~20-29 km covered), and the stuck
stragglers were behind units still en route; last night's 8-hour run is the evidence that
the majorities DO arrive later and the straggler does not. P9 therefore needs a window in
which majorities can arrive: RunSecs 4200 (70 min) for the verification run. The "arrival
rule COMPLETE" column of the tool matched the two vendor completions exactly (4/4 within,
5-6 m) - a first agreement check of the rule against the vendor's own verdicts.
Adversarial review: the alternative reading "the stragglers are still coming and would
complete given time" is refuted for the four STUCK ones by last night's 8-hour run (the same
vehicle types never arrived) and by net movement of metres over 15 minutes; it holds for the
one CLOSING case, which the majority rule handles without waiting for it. Verified: all
numbers from the trace and the app log. Assumed: none.

REVIEW OF THE ARRIVAL-EVIDENCE CODE (workflow wf_62e5bdf7, 37 agents, 3 lenses x 2
verifiers; 14 findings, 9 distinct, all fixed before the P9 run): BLOCKER - the new
unknown-switch guard refused the runner's own --contentRoot=<dir> host argument (host
switches "--Key=Value" now pass; a bare unknown switch is still refused); the swallow flag
was cleared when a successor was MARKED (CreateRoute) instead of when its VR-Forces command
is ISSUED (now cleared at the synchronous bridge calls, in the route-created callback, and in
IssueEngage - a deferred engage's own completion was being swallowed and left the unit BUSY);
the R10 fan-out (opt-in) would have emitted a second empty-uuid TASKCMPLT (the fan-out is
marked synthesized under the task uuid before the evidence completion); ArrivalMemberFraction
1.0 could never fire (>= all); "arrival-evidence" as a task type tripped the attribution-
anomaly warning (empty type now); the scorers (movement_check.py, straggler_track.py) key on
"VRF task complete" and now count ARRIVAL EVIDENCE lines and un-count swallowed vendor
completions. ACCEPTED AS THE RULING'S TOLERANCE (split vote): a move whose destination is
within 500 m of the unit at dispatch reports complete ~30 s later without moving - the
COA-STP1 order chains seven successors at 0 m from their predecessor's destination
(T40->T41->T42 ...), which is exactly what lets a unit with a stuck straggler progress; an
"must have left the radius first" gate would stall those chains for good. Non-findings: inert
with ArrivalCompletion=false; the company's members ARE vehicles (GetAggregateMembers
recurses, depth 3); tick cost is local lookups; the vendor callback and the check share the
tick thread (no race); -StopWhenComplete's closing rule is unchanged.

### 3c. P9 VERIFICATION RUN - launched 11:41Z on build 392cc81 (arrival-evidence completion
ON by default), same configuration as 3b with RunSecs 4200 / WatchSecs 4500 (70 min, so the
majorities can reach their 24-33 km legs' ends).
Predictions: P9a every unit whose majority reaches its last vertex logs "ARRIVAL EVIDENCE"
and a TASKCMPLT within ~35 s of the majority entering 500 m (the check runs every 5 s);
P9b the two units the vendor completed in 3b complete again, by whichever path is first,
and their vendor completion (if later) is logged as swallowed - ONE TASKCMPLT per task;
P9c 1-6/2/1_AD (leader alone, followers at the start) does NOT report; P9d successors
released by evidence completions are dispatched (MoveAlongRoute lines beyond the first 9);
P9e no "attribution anomaly" warning and no empty-uuid TASKCMPLT.
Falsifiers: a TASKCMPLT for a unit whose centroid is > 500 m from its last vertex at the
time of the report (false completion); two TASKCMPLT for one task uuid; a unit at its
destination with a majority within 500 m and no report for > 60 s.

HARNESS NOTE (11:45Z): the runner's status line now reads the trace TAIL (Read-LiveTail,
256 KB; commit 66f96a7) instead of the whole file - the mechanism of the 00:47Z runner
death. tests/RunnerTurnaround.Tests.ps1 run DURING the P9 run reports 218/223: the five
failures are the dry-run tests, whose pre-flight refuses to start beside a joined WatchVrf
(the live run's observer) - an artefact of running the suite while a run is up, to be
re-confirmed at 222+/223 after the teardown.

### 3d. WHAT IS SPECIAL ABOUT THE STUCK VEHICLES (user question, 11:50Z) - from both spaced runs
Per member, designator, type, displacement and deficit behind the unit's lead member, night
(003457Z) vs day (103246Z):
- The stuck vehicle stops at the SAME PLACE in both runs: 1-35's M3 1 (designator 3) at
  23.9 km both times, 0.0 km apart; 40/2/1_AD's M577A2 5 (designator 5) at 26.2 km both
  times, 0.0 km apart; 4-27's M3 2 (designator 3) at 22.7 km both times, 0.0 km apart.
  Deterministic, not chance.
- Two DIFFERENT units' stragglers stopped ~500 m from each other: 1-35's M3 at
  34.58197,-116.98337 and 40/2/1_AD's M577 at 34.58285,-116.97806 (their routes share the
  same axis) - the same patch of terrain catches one lane of each unit's formation.
- 1-6/2/1_AD is a different shape, also deterministic: its LEADER (M1A2 19, designator 1)
  and four followers never left their ring slot (0.7-1.6 km, both runs, 0.1 km apart) while
  designator 4 (HMMWV 7) drove the whole 33.5 km leg alone, twice. C/1-35 (night) likewise:
  17 members within 2.3 km of their slot and M1A2 42 (designator 10) 37 km away alone.
- Not a vehicle-type property: M3s and M577s are the mid-route stragglers, but in 1-6 and
  C/1-35 the M1A2s and HMMWVs are the stuck ones; the same types drive 25-31 km in the other
  units.
- Navigation preference: the unit consoles log "Setting navigation preference to
  ignore-roads" at task start. OUR CODE DOES NOT SET IT (grep of src: nothing); it is the
  vendor default - UG52 40.54: "Military vehicles typically default to ignoring roads ...
  they plan a path that is as direct as possible while avoiding obstacles"; "Civilian
  vehicles typically default to preferring roads".
READING (candidate, not verified): a terrain feature on that vehicle's OFFSET LANE (each
member of a maneuver-along "computes an offset route and then traverses it ... at its own
pace", UG52 30.22) that the direct-path planner cannot cross or skirt (UG52 23.2.3 "trapped
by a very large alley"; the script parks after MAX_REPLANS=3 + one global replan), while
the neighbouring lanes 50-100 m away pass. The whole-unit cases are the same thing at the
unit's ring slot (the slot is deterministic, so the terrain under it is too), with one
member whose lane escaped. Competing reading: a vehicle-type limit (the M577 is a box on an
M113 chassis) - refuted by the M1A2s stuck in 1-6/C/1-35 and the M577s that drove 25-31 km
elsewhere. FALSIFIER / TEST, one run each, vendor-backed: (a) Navigation Preferences =
Prefer Roads for the tasked units (UG52 40.54 set data; MG 2.4) - if the same vehicles then
pass the same spots, the trap is cross-country terrain; (b) shift the ring (a different
spacing, e.g. 750 m) - if 1-6's stall moves or vanishes, its slot's terrain was the cause.
(c) A look at 34.582,-116.98 and at 1-6's slot 34.6625,-116.7326 in the VR-Forces GUI (the
audience's window, a human look) would settle the terrain question directly.

### 3c RESULTS - run 20260907T114026Z (P9), 4200 s cap, runner exit 0
- P9a HOLDS: 4 ARRIVAL EVIDENCE completions -> 4 TASKCMPLT: 856/HHC, 1-1/2/1_AD, B/5-20 (the
  vendor's own completion arrived ~90 log lines later each time and was SWALLOWED - 3 swallow
  lines) and 1-35/2/1_A, the C15 target case: reported with 4 of 6 members within 500 m
  (centroid 529 m from the last vertex; its M3 straggler 1 km back) while the vendor never
  completed it in 70 minutes.
- P9b HOLDS: one TASKCMPLT per task uuid (no duplicates); 4 reports for 4 tasks.
- P9c HOLDS: 1-6/2/1_AD not reported (1 of 6 within: the lone HMMWV); 5-20 (1 of 6, centroid
  1.0 km short), 40/2/1_AD (0 of 6, 1.17 km short), 4-27 (9.3 km short), C/1-35 (35.6 km
  short - the company again went almost nowhere) not reported - all correct.
- P9e HOLDS: no attribution anomaly, no empty-uuid report.
- P9d NOT TESTABLE HERE: 9 MoveAlongRoute = the first 9 only; the successors had been
  abandoned at 600 s by the HARNESS default TaskPredecessorTimeoutSeconds (the demo overlay's
  7200 is not applied under the runner). The rotation run passes 7200 through the environment
  so successor dispatch after an evidence completion is exercised there.
VERDICT: the arrival-evidence completion works as ruled; STP now receives a completion for a
unit that arrives, whatever one straggler does. Adversarial review: the falsifier "a
TASKCMPLT for a unit whose centroid is > 500 m from its last vertex" did not occur (the
farthest reported centroid was 529 m with 4 of 6 members inside the radius - within the
ruling's tolerance, majority rule); "two TASKCMPLT for one task" did not occur; "a unit at
its destination with a majority inside and no report" did not occur.
HARNESS: tests/RunnerTurnaround.Tests.ps1 rerun with no run active at 12:55Z: 222 passed,
1 failed (the pre-existing LaunchVrf52 harvest check) - the five extra failures seen during
the live run were the observer artefact, as recorded.

### 3e. ROTATION TEST (user, 12:40Z: "Rotate the unit placements to verify your terrain theory")
- registered before launch
Lever: Vrf:DeStackRotationDeg (new; DeStacker.RingOffset rotates the hex pattern about the
anchor). Run = the P9 configuration (spaced 700 m, AtOrder, arrival evidence on, position
reports, unit consoles 4, members 0, 70 min, 64-bit host) with DeStackRotationDeg = 90:
every displaced unit gets different ground, the same neighbours and spacing, and IDENTICAL
routes (a route starts at the unit's live position and then follows the order's vertices).
Predictions (the theory splits in two, and so do they):
- P10a SLOT STALLS MOVE: 1-6/2/1_AD's leader and followers, stuck at their slot in both
  previous runs (0.7-1.6 km, 0.1 km apart), leave and drive their leg; the runaway-follower
  pattern for that unit does not recur. (C/1-35's night-run slot stall likewise does not recur
  on its new ground.) High confidence if the theory is right.
- P10b MID-ROUTE TRAPS STAY: 1-35's M3 (designator 3) and 40/2/1_AD's M577 (designator 5)
  stop again within ~500 m of 34.582,-116.98, and 4-27's M3 within ~500 m of
  34.479,-116.820 - the route lanes cross the same ground whatever the start slot. High
  confidence; a miss here means the trap depends on the approach, not the patch.
- P10c NEW SLOT STALLS MAY APPEAR for units whose rotated slot is bad ground (unpredicted
  which; recorded if any).
- P10d the arrival-evidence completions and the vendor completions behave as in P9.
FALSIFIERS: 1-6 stuck again at its NEW slot -> the stall is in the unit, not the ground
(then the unit's template/leader is the suspect); the mid-route stragglers stop at NEW places
or not at all -> the trap is start-dependent (approach path), not a fixed patch.
Competing hypothesis kept alive for the record: the whole-unit stall is a template/leader
property of the CP-proxy for infantry battalions (1-6 is an IN battalion mapped to the
scratch no-lifeform proxy) - P10a's outcome decides between them in one run.

### 3e RESULTS - run 20260907T125540Z (rings rotated 90 deg; 4200 s cap; runner exit 0)
- P10b HOLDS for the clearest case: 1-35's M3 (designator 3) stopped at the SAME coordinates
  as in both previous runs (0.00 km apart) from a start 4 km away - a fixed trap on that lane
  of that route, THREE runs in a row. 4-27's M3 and 40/2/1_AD's M577 could not be tested:
  their units never got that far this time.
- P10a FAILS: 1-6/2/1_AD's leader (#1) and followers #2, #3, #5 stalled again 1.3-5 km out
  from a slot 3 km from the old one, while #4 (HMMWV 7) drove the 34.8 km leg alone, and #6
  this time too. The stall is not the ground under 1-6's slot.
- P10c, worse than "may appear": the rotation spread the pattern. 40/2/1_AD (26 km last run)
  stalled at 1.1-2.0 km with #4 (HMMWV 9) alone at 26.4 km; 1-1 stalled #1-#3 at 1.9-2.8 km
  with #4 (M1A2 26) alone at 27.8 km; 5-20 all six at 2.6 km (#6 at 0); 4-27 all at 3-4.6 km;
  C/1-35's HQ + three platoons at 24.7 km with the fourth platoon (#11-#18) at 8-12 km. The
  runaway is DESIGNATOR 4 in three units (1-6 twice, 40/2/1_AD, 1-1).
- P10d HOLDS: 12 MoveAlongRoute = 9 first legs + 3 successors (T28, T36, ...) released by
  completions with the 7200 s predecessor timeout; 4 TASKCMPLT for 4 task uuids, no
  duplicates: T27 + T28 (856/HHC; T28's destination is T27's end, so the evidence completed
  it 30 s after dispatch - the ruling's accepted tolerance, and STP's plan advanced), T35
  (B/5-20, 3 of 4 within 500 m at 1,588 s) + T36.
READING (revised): the stalls are NOT the ground under the slot. They happen 1-5 km after
leaving it, on the cross-country APPROACH from the slot to the route's first vertex, and
rotating the slots changed the approaches and therefore which units got caught. One member
per unit (designator 4, twice a HMMWV) escapes and drives the leg. With the vendor's own
rules: military vehicles ignore roads by default (UG52 40.54), the feature-obstacle planner
"does not take ground slope into account" (23.5), and "vehicles can just barely move up the
max-slope ... may slide down slopes" - a unit sent cross-country through the Mojave hills
without navigation data stalls wherever a lane meets a grade it cannot climb, silently (no
block message: verified in 3d's member consoles). The fixed trap of 1-35's M3 is the same
mechanism on the route itself.
Adversarial review: verified - positions, designators, the same-spot repeat, the vendor
text. Refuted by this run - "bad ground under the slot" (1-6 stalled on two different
slots). Not yet verified - that the stall points are grades above max-slope (needs a
height profile at the stall points, or the vendor's navigation data). The competing
"template/leader property" for 1-6 is weakened by 40/2/1_AD and 1-1 showing the same
shape only after rotation - the property is the approach path, not the template.

## 4. Results
Run 20260907T003457Z (launched 00:35Z on the deployed build 7ac70c9). RUNNER INCIDENT: the
runner process exited with code 9 at ~t = 590 s of the window with no Stop-Runner message;
the sim, the interface and WatchVrf kept running (the runner's teardown never ran - manual
teardown after the window). Scored from the live trace. MECHANISM (code + instrument
facts, not a run): the window loop's status line calls Read-LiveText, which reads the WHOLE
trace with StreamReader.ReadToEnd (RunC2SimScenario.ps1:976-990) every 30 s; with unit
consoles at 4 AND member consoles at 3 the trace grew ~17 MB/min (176 MB at t = 590 s), and
the launching PowerShell host is 32-bit ([Environment]::Is64BitProcess False, found
2026-09-06) - a 350 MB UTF-16 string plus copies inside a 32-bit process is an out-of-memory
death. The diagnostic run 224326Z (member consoles only, ~150 MB by its END) survived the
same host; this one crossed the line at 10 minutes. Competing explanation - the tool's
10-minute background timeout - is refuted by the 45-minute run bxe7sxsdb from the same host.
FIX OWED (runner, not product): Read-LiveText for the status line reads a tail, not the file;
launch long runs from a 64-bit host. Falsifier of the mechanism: the same configuration
dying under a 64-bit host with a tail read.
MID-RUN, t = 646 s (order at ~t = 40 s):
- P5 HOLDS: "DeStack (R8): 54 units ... spread onto 700 m rings" (+ a second group of 2);
  341 reflected objects on 308 distinct ~10 m cells, largest co-located group 5 (co-located
  runs: one cell holding every taskee's vehicles).
- P1 HOLDS: "BlockedByVehicle" 63 messages in 10 minutes, all runs' worst minute 30 (the
  co-located diagnostic: 4,828 in the first 5 minutes).
- P3 HOLDS: C/1-35's console logs "task complete msg rcvd from" TANK2 (sim 89 s), HQ1 (111 s),
  TANK3 (141 s), TANK4 (163 s) - all four - then issues maneuver-along on its four offset
  routes C/1-35_R0..R3 at wall t = 103 s. The gate that stayed shut in 231401Z opened in under
  three sim minutes.
- P2 in progress: 8 of 9 beyond 1 km at t = 646 s (2.8 / 5.0 / 5.2 / 8.8 / 9.0 / 9.6 / 10.4 /
  10.6 km); the ninth is C/1-35 at 415 m, which began maneuvering at t = 103 s. At the same
  wall time the co-located diagnostic (224326Z) had 0.14-5.0 km.
- P4 pending (first legs 24-45 km).
FINAL (trace capped at t = 3,258 s by WatchVrf's own limit; the app and the sim ran on
unattended until the manual teardown at 10:12Z - StopIface resign in 10 s, StopVrf52 graceful,
RTI untouched):
- P4 HOLDS: TASKCMPLT x2 inside the window - B/5-20 (28.7 km leg) and 1-1/2/1_AD (28.7 km) -
  the FIRST task completions ever reported at scale on this interface (5.0.2 never reached a
  route end; PREREG_COASTP1_QPAIR / HANDOFF item 5). One successor (T13, 510/40 breach company)
  was released and its move issued.
- P2: at t = 3,258 s eight units at 18.7-29.7 km, i.e. at or near the end of their 24-33 km
  first legs (1-35 25.0/28.5, 40/2/1_AD 27.4/28.5, 4-27 29.7/33.5, 5-20 29.0/28.7, 856/HHC
  18.7/24.1); 1-6/2/1_AD at 5.2 km; C/1-35 maneuvering (its console: formation speed-up /
  slow-down commands to its platoons). 9 of 10 tasked units beyond 1 km.
- P1 over the trace window: 63 blocking messages in the first 10 min; over the whole night
  3,018 in the app log (vs 4,828 in five minutes co-located) - the residual blocking is
  units meeting on the roads over 9 hours, not the start point.
- ratio 1.64x with unit consoles at 4 + member consoles at 3 (the heaviest instrument load
  of the series; 745 MB trace, 1.2 GB app log - the console stream is ALSO written to the app
  log at INFO, which a demo must not do: set ObjectConsole levels to -1, the demo overlay
  already does).
VERDICT: the spacing ruling (C14) is CONFIRMED on every prediction. Spread on 700 m rings,
the 11-unit COA moves as a unit set, the composed company's formation gate opens, and units
complete 28 km legs and report it. NEW OPEN OBSERVATION (outside this prereg): only 2 of the
9 first legs reported completion in 9 unattended hours although the others stood at the ends
of their legs by 54 minutes; the sim kept ticking (console lines still streaming at 10:11Z).
The predecessor-timeout policy (TaskPredecessorTimeoutSeconds 600, policy skip) had
abandoned 31 successor tasks long before any 28 km leg could complete - a HARNESS default
that the demo must override (legs run 30-60 min of sim time). The completion-at-the-leg-end
question is the next thing to read from this run's unit consoles - not a theory.
FIRST READ (10:15Z): the sim reached sim time 29,847 s (8.3 h) overnight; the two completions
came at sim 3,121 s and 3,295 s. The unfinished CP proxies' LAST unit-console lines are all
". Subs still moving: N" (4-27: 2 then 1; 40/2/1_AD: 3 then 2; 1-6: 3 then 2) - the unit's
move-along holds its completion until EVERY member reports arrival, and one to three members
per unit never did. Which members and why is in the member consoles (level 3) of the 1.2 GB
app log - the next analysis. This is the C1b family at the END of the leg (the unit waits on
a member that never reaches its slot), no longer the pile at the start.
MEMBER-LEVEL READ (last 900 MB of the app log, sim 7,000-28,800 s): the members of the
unfinished units were NOT parked - they kept planning and driving all night: "Job Plan path
success" x10-18 per tank (C/1-35's M1A2 44 planned its 18th path at sim 28,778 s), "Task Move
along route success" x9-17, "Turn to route success", "Move On Arc success", "Backup success",
one "FailWhen condition is true; exiting" (1-35's M1A2 1 at sim 9,425 s). So the picture at
the leg end is a unit whose maneuver-along keeps completing per member and being re-issued
while the unit-level task holds "Subs still moving: N" - repeated per-member success without
unit completion over 8 sim-hours. NOT explained here; the unit-level console around the leg
end (level 4, in the same log) and UG52 30.24's unit paragraph ("the pseudo-aggregate
considers the task complete when its lead subordinate completes", moveAlongTasks.h:35-38)
are the next reading. The demo needs this closed: a unit must report completion when it
arrives.
THE UNIT'S OWN COUNT (4-27/2/1_A~PXY, six members, whole night): ". Subs still moving: 5"
(app-log line 6.56 M), 4, 3 (6.56 M), 2 (9.26 M), 1 (9.35 M) - and never 0 in the remaining
6.9 M lines. Five of six members arrived one at a time over the night; the sixth kept
planning and driving to the end. UG52 30.22: in Maneuver Along "each subordinate computes an
offset route and then traverses it by planning a path to each vertex in sequence at its own
pace. There are a leader and followers, which use speed control to remain within some
distance of each other". The next reading is THAT member's console at the leg end (which
vertex it is on, what it re-plans toward) - the instrument exists (member level 3 is in the
log; the member -> unit map is in the request line since fa123b0).
THAT MEMBER (4-27/2/1_A~PXY, six members - M1A2 x2, M3, HMMWV x2, M577A2): over the whole
night NONE of the six ever logged "Loop to stall", "Global Replan", "not embarked" or
"BlockedByVehicle" (the spacing removed every one of those); each planned two paths. In the
trace window (to t = 3,258 s) five members stood at 30.9-31.2 km and the M3 (Bradley) at
22.7 km - 8 km behind - and it alone kept ticking its move behaviour tree (29,330 "Maybe Skirt
Blockage" checks vs 9-15k for the others, i.e. active to the last line at sim 29,847 s) with
no failure event at all. Where it went after t = 3,258 s is UNOBSERVED: WatchVrf capped at
its own duration and the R1 position reports were off in this harness run. So: the unit
waited all night for a follower that was 8 km behind at 54 minutes and never reported
arriving. Open, with two instrument notes for the next run: WatchVrf's cap must cover the
whole window (-WatchSecs), and position reports on (the demo default) would have shown the
follower's track.
Adversarial review: the competing explanation for the movement gain, "a lucky run", is
refuted by the size of the effect against four co-located runs (blocking 63 vs 4,828; the
formation gate opened in 3 sim-min where it never opened; 9 of 10 past 1 km) and by the
mechanism the vendor documents. Verified: everything above from the trace and the app log.
Assumed: none on the placement claim. Unexplained and recorded: 7 legs without a
completion report after 9 hours.
