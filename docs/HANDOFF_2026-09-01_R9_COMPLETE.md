# SESSION-JUMP HANDOFF (opened 2026-09-01, current 2026-09-02) - R9 EXECUTES 3/3; FFRTC VALIDATED

THE CURRENT entry point (newest HANDOFF_*.md by git log). SUPERSEDES
HANDOFF_2026-07-23_TYPEFIX_CONFIRMED.md. ASCII only. HARD CAP 200 LINES - when a phase closes,
collapse it to a few lines plus a pointer; never drop live guidance to make room. RE-VERIFY
load-bearing claims against artifacts before trusting prose.

## CLOSED - DO NOT REOPEN (tripwires; each line names its record)
- ROUTE FREEZE = ROUTE-NAME LENGTH, not region / template / type / waypoint altitude / vertex
  count / creation order / pile density. 9 of 9, no exceptions; reopening evidence = the
  falsifier in sec 8 of docs/experiments/ANALYSIS_COASTP1_RUNG1_FREEZE_2026-09-02.md (04bcc0f).
- SPEED-UP = FRAME MODE, NOT TIME MULTIPLIER; the multiplier and at-distance ladders are
  WITHDRAWN. docs/experiments/PREREG_R9_FIXED_FRAME_RTC_2026-09-02.md sec 8 (2030ebd, c0e90b7).
- The REGION / Mojave-terrain cause is FALSIFIED (docs/CORRECTIONS_LOG.md "The region
  hypothesis"; tagged 2026-09-02 in the six live docs that still stated it).
- TYPE MAPPING is fixed: ArmorPlatoon -> real Tank Platoon (USA); RealTemplates is the compiled
  DEFAULT (2026-07-22).
- The 2026-07-14 project-generated NavArea (120k tiles) WAS the 2026-07-15..2026-09-01 freeze;
  it is now in SharedData/16/latest/TerrainData/navData/_disabled_20260901/ (restorable). KEEP
  DISABLED unless deliberately regenerating nav data.
- The HQ-section formation-name warning is COSMETIC; the P2 aliases were reverted (P2c). No
  defect report to MAK.
- ROUTE-VERTEX ALTITUDE FRAME: TerrainProfile (terrain height + 10 m) is the compiled DEFAULT,
  Live (+50 m) the fallback, Fixed100 a relic (DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01 sec 7).
- Birth altitude, "nav data ruled out", the 10-char marking collision: docs/CORRECTIONS_LOG.md.
  Every July FALSIFIED stamp is LAYER-RELATIVE (L9) - re-adjudicate before trusting its fence.

## THE STANDING RULE (user directive 2026-09-01, enforced)
DOCUMENTATION FIRST for any issue: local help -> the PUBLIC Developer's Guide at
docs.mak.com/api/vrforces{5.2,5.1.1,4.10}/classref/ -> internet research. A live probe is
registrable ONLY after its prereg cites the documentation consulted. Two months of probing
dissolved in one afternoon of reading (2026-09-01). VRF_GROUNDWORK_PLAN lessons L8-L10.

## ONE-LINE STATUS
The R9 Mojave order executes END-TO-END, HEADLESS, ALL THREE TASKEES (platoon, company, single
entity), telemetry-verified arrivals + TASKCMPLT - run 20260901T203702Z at 1x and again under
fixed-frame run-to-complete (20260902T140808Z) in 20 s of wall where the 1x comparator spent
182 s. COA-STP1 AT SCALE still freezes 4 of 8 dispatching aggregates: the CAUSE IS NOW KNOWN
(route-name length), THE FIX IS NOT YET APPLIED. Evidence chain:
PREREG_P1_FIXED100_ENTITY_2026-09-01.md + RESEARCH_MECHANISMS_2026-09-01.md.

## WORKING CONFIGURATION
TypeMappingMode=RealTemplates + GroundWaypointAltitudeMode=TerrainProfile (both compiled
DEFAULTS since 2026-09-02, bridge A7504441) + NavArea disabled + STOCK templates, no env
overrides - an untouched product at default settings. Vendor defects found across the whole
saga: ZERO. Runner hardening permanent - Stage 2b boot-dialog watcher (AnswerRtiDialog.ps1; the
RTI dialog is ONCE PER REBOOT), Stage 2c RTI gate, per-run bin64 log capture, stop-file trace
close, off-by-default -StopWhenComplete with -SettleHoldSecs 60 as a FLOOR and rule 4 (every
taskee needs an RPT LATER than its TSK and within 2 m of its latest POS): see
docs/RUNNER_TURNAROUND_2026-09-01.md, RUNBOOK 0.5.11. vrfSim.mtl: notifyLevel 3 /
objectConsoleNotifyLevel 3 / enableLogFileTimestamps 1 (backup .bak-20260901) - KEEP: this is
what made the freezes speak.

## FIXED-FRAME RUN-TO-COMPLETE - THE SPEED-UP LEVER (validated 2026-09-02)
FINDING. The SCENARIO'S EXERCISE CLOCK MODE, not TimeMultiplier, is how this federation runs
faster than the wall clock (Users Guide sec 3.4.3 p.122-123, sec 7.6.1 p.254-255, sec 12.2.1
p.351-355 + Table 17). Fixed-Frame Run-To-Complete is the vendor's named mode for "run a
simulation overnight and view the results the following day"; variable-frame is the mode the
vendor says "does not provide repeatable results". FIXTURE:
tools/FixtureGen/frame_variants/TropicTortoise_FFRTC.scnx - stock TropicTortoise with TWO lines
moved, (frame-mode "fixed-frame-run-to-complete") and (frame-time 0.033333, the frame length the
box was already running) - DEPLOYED as C:\MAK\vrforces5.0.2\userData\scenarios\ with the same
name; load with -Scenario TropicTortoise_FFRTC. Stock TropicTortoise.scnx untouched.
RESULT (run 20260902T140808Z, appNos 3726-3732; prereg 2030ebd, outcome c0e90b7, sec 8):
VERDICT PASS, NO FALSIFIER FIRED. Mode in effect - 100% of small gaps on grid (32/32), 85/85
distinct sim stamps within the 0.0005 s print rounding, R = 0.9986, fitted phase 1 microsecond
(a deterministic zero-phased grid). Answer unchanged - 3/3 TASKCMPLT, COMPLETION ORDER IDENTICAL
taskee for taskee across 3 runs of this order, endpoints within 0.09 m, terrain lines
character-for-character identical, app log 103 lines in both with six non-semantic differences.
9.04x WALL from order push to last TASKCMPLT (20.18 s vs 182.34 s); LS slope 10.18; the ratio is
LOAD-DEPENDENT (frames are cheaper when nothing moves). NO TIMEOUT FIRED. Hygiene clean: every
stage exit 0, RTI trio untouched, no new dump, ResetVrf found zero leftovers.
THE RULE: ALL PROBES FROM NOW ON RUN UNDER FFRTC MODE unless the prereg states why not;
TIMEMULTIPLIER STAYS 1x; the multiplier and at-distance LADDERS ARE WITHDRAWN.
OPEN DESIGN ITEM (not a blocker, not a repair): OUR APP HAS NO NOTION OF SIM TIME.
VrfFacade.cpp:478-482 pins the federate clock to elapsedRealTime
(`clock()->setSimTime(clock()->elapsedRealTime())`), VrfC2SimService's TickLoop is 20 Hz WALL and
every timeout in the app is wall (inventory with budgets: prereg sec 5 P4). Compression
only ever gives a wall budget MORE margin - the one budget it does not shorten,
TerrainProfileTimeoutSeconds 10, was armed three times and did NOT fire (3/3 replies). A sim-time
read-back is a DESIGN item to decide, not a defect to fix. Separately and LOW PRIORITY: an
observed-run fastForwardSettings.mtl entry (sec 7.6.1) NEEDS USER OK first.

## COA-STP1 SCALE RE-RUN - RUNG 1 RESULT AND ROOT CAUSE
RUNG 0 DONE (fc93a1e): July region hypothesis RETRACTED and tagged in six live docs; 31 (not 32)
temporal deps; DEFECTS A and B verified ALREADY FIXED in source (InFlightTracker, TaskSequencer).
RUNG 1 RESULT (run 20260902T125423Z, appNos 3718-3724; prereg d1f2e10, outcome sec 6 of
docs/experiments/PREREG_COASTP1_RUNG1_BOUNDED_2026-09-02.md, 7963aed): THE JULY MECHANISM IS
GONE; THE FREEZE IS NOT, AND IT IS NOW SILENT. Zero "moveAlong() - empty route" lines in 140 MB
of back-end log - that grep oracle is DEAD - yet 4 of the 8 dispatching aggregates built ZERO
member offset routes and never moved, with no diagnostic of any kind. The other 4 built them
and MARCHED 13.2-26.7 km at 8.0-8.2 m/s, still moving at window close: the first COA-STP1
aggregates ever observed driving their own order at 1x. Offset routes and movement correlate
1:1 across all 8. No runaway, nothing underground or offshore, no CPP-ALT-1 stop radius,
terrain authoring clean for movers and freezers alike, cleanup 172 = 128+35+9 exactly.
OTHER FINDINGS (recorded, unfixed): (A) the lone ENTITY taskee reported TASKCMPLT from the BACK
END's own callback while never leaving its spawn ring - a VACUOUS COMPLETION that falsely
released T24 and cascade-skipped T25/T26; (B) all 26 echelon-'F' units land the GENERIC
Ground_Aggregate fallback (UnitTranslator.cs:70/:134, TYPE_GAP_ADJUDICATION item 4, still a
USER call) - but that fallback MARCHES, so it is not the freeze cause; (C) TerrainProfile
re-entry double-logs the verb classification; (D) ResetVrf after StopVrf is blind
(BackendCount=0) - it must run between StopIface and StopVrf.

RUNG 1 ROOT CAUSE (offline forensics, no live run; ANALYSIS_COASTP1_RUNG1_FREEZE_2026-09-02.md)
DISCRIMINATOR, exception-free in 9 of 9: ROUTE-NAME LENGTH. Every performer whose route name is
<= 34 characters marched; every one whose route name is >= 36 characters froze. H1-H5 (dispatch
race, leader selection, boxing-in, aggregation state, vertex count) are ALL REFUTED in sec 4.
MECHANISM. DtUUID stores a fixed 36-byte blob - 1 type byte + 35 payload
(C:\MAK\vrforces5.0.2\include\vrfutil\rwUUID.h, `char myData[36]`). The move-along task
addresses the route BY NAME through that blob: VrfFacade::MoveAlongRoute passes
DtUUID(routeUuid) at src/VrfFacade/VrfFacade.cpp:571 (call site :569-571), so a name longer than
34 characters arrives at the aggregate CUT TO 35 WITH NO TERMINATOR - T27's in-task name is
visibly unterminated with trailing junk at bin64-vrfSim.log:52979. DtSimObjectReference myRoute
then never resolves, generateFormationRoutes / beginFollowInFormation are never reached, ZERO
offset routes are built and NOTHING IS LOGGED (the old oracle line lives in a later stage). The
CREATION path is unaffected - CreateRoute passes an unbounded DtString at VrfFacade.cpp:529-534,
so the full-length route objects exist intact, which is exactly why the July "names pass at 99
chars" test came out clean.
FIX CANDIDATES (neither applied; NOT before the probe scores):
 1. ROUTE NAMING (C#, cheap, reversible): stop naming routes with the C2SIM task name
    (src/VrfC2SimApp/VrfC2SimService.cs:929 builds it; :1142 and :1172 address by it). Use a
    SHORT SYNTHETIC ID <= 34 chars - e.g. derived from the task UUID. VR-Forces names its own
    sub-routes C/1-35_R0..R3 (9 chars) and they resolve. The C2SIM task name belongs in the log
    line, not in the object name.
 2. FACADE COMPLETION STATUS (native C++): forward DtTaskCompleteReport success() / taskId() /
    taskTrackingNumber() through VrfFacade::TaskCompleted. All three are DROPPED today at
    src/VrfFacade/VrfFacade.cpp:217-242 (struct TaskCompleted, VrfFacade.h:119-123, has no such
    field), so a success=false FAILURE report is indistinguishable from a real success - which
    is what let finding A through as a TASKCMPLT. Native change under STANDING AUTHORIZATION:
    back up the DLLs, /t:Rebuild always, REDEPLOY ALL 10 COPIES, verify ONE hash across them.
STATUS: PROBE IN FLIGHT - docs/experiments/PREREG_ROUTE_NAME_LENGTH_2026-09-02.md (A/B on route
name length ONLY, data-only, under FFRTC mode). A live executor owns that file, runs/ and
tools/FixtureGen. DISSENT ON RECORD against MOJAVE_ROOTCAUSE part 12's "name length falsified":
it tested the CREATION path (unbounded DtString) and the 10-char marking collisions, never the
route name inside the move-along TASK. Logged in docs/CORRECTIONS_LOG.md.
UNEXPLAINED, a falsifier candidate rather than a footnote:
"buildEntityRouteFollowingMap() : Can't find entity route" appears 14,880 times, from the
dispatch second on, at a flat ~335/min for 45 minutes. No object prefix at notify level 3, so
unattributable. It does not contradict the name-length finding; the finding does not explain
it either.

## OPERATIONAL STATE (2026-09-02, after the FFRTC run, before the name-length probe)
VR-FORCES DOWN between runs (StopVrf exit 0, "graceful quit; no process was force-killed").
appNo marker NEXT FREE = 3734, authoritative marker in docs/OPUS_EXECUTION_PLAN.md Appendix B
(runner-managed, ledger CRLF). 2026-09-02 blocks consumed: terrain Rows 1/2/2R/2c/2cR/3
3676-3717, COA-STP1 rung 1 3718-3724, FFRTC 3726-3732, post-run ResetVrf sweep 3733.
RTI RESIDENT + ANSWERED: rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620 - UNCHANGED
across every 2026-09-02 run; still inventory fresh at session start, do not trust PIDs. C2SIM
docker UP. Deployed bridge = A7504441 (10/10 copies, Ijwhost 38255036; backups
bak-20260902-a48abe6c/ and bak-20260902-28e993fe/). Dump 70668 sits in bin64 with no newer one
since (RUNBOOK 0.5.12: scripts/AnswerCrashDumpDialog.ps1 answers the prompt; never halt on it).
Firewall: do NOT set NotifyOnListen False (user ruling); Cancel the testhost prompt. MAK license
expires 2026-09-15, renewal in process - verify the new .lic landed before running after that.
CORRECTION 2026-09-02: THE VENDOR LOG'S WALL STAMPS ARE LOCAL TIME (-04:00), NOT UTC - convert
before cross-referencing any bin64-vrfSim.log stamp against a UTC artifact. Our own app and
tool logs do stamp UTC. See docs/CORRECTIONS_LOG.md.

## NEXT (in order)
1. ROUTE-NAME LENGTH: let the A/B probe score, then APPLY fix candidate 1 (short synthetic
   route ids) and RE-VERIFY on R9 under FFRTC before anything at scale.
2. COA-STP1 RUNG 2 - the FULL order under FFRTC, after the fix lands and R9 re-verifies. Rung 1's
   bounded 45-minute window is no longer the constraint: ~9x compression makes the whole order
   affordable at TimeMultiplier 1. Expect T13 NOT to dispatch (12,000 s start delay, not a miss).
3. TYPE_GAP ITEM 4 - the echelon-'F' -> generic Ground_Aggregate fallback needs a USER RULING
   (docs/TYPE_GAP_ADJUDICATION.md, Decision item 4). Still pending.
4. SIM-TIME / MULTIPLIER READ-BACK - the design item in the FFRTC block: the app cannot read the
   back end's exercise clock and every budget it holds is wall. Nothing is blocked on it today.
5. MAK MESSAGE - docs/MAK_MESSAGE_2026-09-02.md is send-ready and THE USER SENDS IT. A DRAFT
   ADDITION covering the DtUUID route-name-length question is appended to that file, clearly
   marked; the user decides whether it goes into this message or a later one.
6. BACKLOG unchanged: type adjudications (54 units - see the 5.2b checklist first), task
   vocabulary, completion re-keying, scoring (Phase 5).

## VR-FORCES 5.2b UPGRADE CHECKLIST (expected soon - user, 2026-09-01)
(a) diff its EXPANDED AGGREGATE MODEL SET against the 54 pending type adjudications BEFORE
authoring anything (PRIOR_ART Q1); (b) 5.2's "ground path planning enhanced with vector-based
terrain data" touches exactly today's route/clamp machinery - re-run R9 on 5.2b and
RE-ADJUDICATE before trusting any 5.0.2-era behavioural conclusion; (c) migrate local state
DELIBERATELY: vrfSim.mtl notify levels, the DISABLED NavArea artifact (do NOT carry it into
5.2's SharedData), the FFRTC fixture, runner/env paths pinned to vrforces5.0.2, and a full
VrfBridge /t:Rebuild + 10-copy redeploy; (d) read the API migration guides FIRST
(docs.mak.com/api/vrforces5.2/classref/vrf_migration50.html + vrf_migration51.html).

## PROBE PROTOCOL (adopted 2026-09-01/02; do NOT change mid-protocol)
FFRTC mode per the block above; TimeMultiplier 1x; -RunSecs is a CAP under -StopWhenComplete
(420 s was the 1x figure; under FFRTC the same work costs far less wall). SHORT-ROUTE probe
variants are allowed - platoon/entity legs may be ~200 m - but COMPANY probe routes stay >=
~1 km (formation depth ~430 m plus leading-edge completion). Keep ONE canonical-length run per
milestone for comparability with the record. THE 5x MULTIPLIER RECORD is superseded by FFRTC
and NOT to be re-run (P3 lost a follower's completion, P3R repeated clean 28/28, the miss is
unexplained: docs/experiments/ANALYSIS_P3_STEP_PROFILE_2026-09-01.md). watchvrf POS mid-move is
DEAD-RECKONED - only plateaus are truth.

## NON-NEGOTIABLES (unchanged, plus the docs-first rule above)
One variable per probe; prediction + falsifier + DOC CITATIONS before running; movement gate =
static -> moving -> settled with POS/RPT agreement; never kill a joined federate; never kill
rtiAssistant / rtiexec / rtiForwarder without a fresh ruling; fresh ledgered appNo per join;
ASCII in tracked files (ripgrep, not grep -P); after two consecutive infra failures, research
before retry. FALSIFIED stamps are LAYER-RELATIVE (L9) - re-adjudicate on the clean state.
