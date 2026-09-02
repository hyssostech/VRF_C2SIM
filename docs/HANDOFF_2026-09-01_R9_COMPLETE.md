# SESSION-JUMP HANDOFF (opened 2026-09-01, current 2026-09-02) - THE AGGREGATE FREEZE IS FIXED

THE CURRENT entry point (newest HANDOFF_*.md by git log). SUPERSEDES
HANDOFF_2026-07-23_TYPEFIX_CONFIRMED.md. ASCII only. HARD CAP 200 LINES - when a phase closes,
collapse it to a few lines plus a pointer; never drop live guidance to make room. RE-VERIFY
load-bearing claims against artifacts before trusting prose.

## CLOSED - DO NOT REOPEN (tripwires; each line names its record)
- ROUTE FREEZE = OUR ROUTE-BY-NAME ADDRESSING (a >34-char name cut to 35 in DtUUID's blob),
  not region / template / type / waypoint altitude / vertex count / creation order / pile
  density. **FIXED 2026-09-02 (726f762) and LIVE-VERIFIED** - pass the route's real uuid:
  docs/experiments/PREREG_ROUTE_UUID_FIX_2026-09-02.md sec 6, run 20260902T153837Z_run.
  DO NOT shorten or cap route names - that fix is WITHDRAWN and unnecessary (names may be 255).
  Reopening evidence = the falsifier in sec 8 of ANALYSIS_COASTP1_RUNG1_FREEZE_2026-09-02.md.
- SPEED-UP = FRAME MODE, NOT TIME MULTIPLIER; the multiplier and at-distance ladders are
  WITHDRAWN. docs/experiments/PREREG_R9_FIXED_FRAME_RTC_2026-09-02.md sec 8 (2030ebd, c0e90b7).
- The REGION / Mojave-terrain cause is FALSIFIED (docs/CORRECTIONS_LOG.md "The region
  hypothesis"; tagged 2026-09-02 in the six live docs that still stated it).
- TYPE MAPPING is fixed: ArmorPlatoon -> real Tank Platoon (USA); RealTemplates is the compiled
  DEFAULT (2026-07-22).
- The 2026-07-14 project-generated NavArea (120k tiles) WAS the 2026-07-15..2026-09-01 freeze;
  it is now in SharedData/16/latest/TerrainData/navData/_disabled_20260901/ (restorable). KEEP
  DISABLED unless deliberately regenerating nav data.
- The HQ-section formation-name warning is COSMETIC; the P2 aliases were reverted (P2c).
- ROUTE-VERTEX ALTITUDE FRAME: TerrainProfile (terrain height + 10 m) is the compiled DEFAULT,
  Live (+50 m) the fallback, Fixed100 a relic (DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01 sec 7).
- Birth altitude, "nav data ruled out", the 10-char marking collision: docs/CORRECTIONS_LOG.md.
  Every July FALSIFIED stamp is LAYER-RELATIVE (L9) - re-adjudicate before trusting it.

## THE STANDING RULE (user directive 2026-09-01, enforced)
DOCUMENTATION FIRST for any issue: local help -> the PUBLIC Developer's Guide at
docs.mak.com/api/vrforces{5.2,5.1.1,4.10}/classref/ -> internet research. A live probe is
registrable ONLY after its prereg cites the documentation consulted. Two months of probing
dissolved in one afternoon of reading (2026-09-01). VRF_GROUNDWORK_PLAN lessons L8-L10.

## ONE-LINE STATUS
The R9 Mojave order executes END-TO-END, HEADLESS, ALL THREE TASKEES (platoon, company, entity),
telemetry-verified arrivals + TASKCMPLT, at 1x and under FFRTC. **THE AGGREGATE FREEZE THAT COST
THE WHOLE SAGA IS FIXED** (726f762, C# only): routes are addressed by their real VRF_UUID, not
by name. The 44-char route name that froze 114.MechCoy 0.0 m for 30 min now marches it 698.97 m
to the control endpoint, 0.00 m off, 3/3 TASKCMPLT (run 20260902T153837Z). COA-STP1 AT SCALE is
UNTESTED SINCE THE FIX - rung 2 is the next run.

## WORKING CONFIGURATION
TypeMappingMode=RealTemplates + GroundWaypointAltitudeMode=TerrainProfile (both compiled
DEFAULTS since 2026-09-02, bridge A7504441) + NavArea disabled + STOCK templates, no env
overrides - an untouched product at default settings. Vendor defects found across the whole
saga: ZERO (the one candidate, the DtUUID route-name cut, was our own contract violation).
Runner hardening permanent - Stage 2b boot-dialog watcher (AnswerRtiDialog.ps1; the RTI dialog
is ONCE PER REBOOT), Stage 2c RTI gate, per-run bin64 log capture, stop-file trace close,
off-by-default -StopWhenComplete with -SettleHoldSecs 60 as a FLOOR and rule 4 (every taskee
needs an RPT LATER than its TSK and within 2 m of its latest POS): docs/
RUNNER_TURNAROUND_2026-09-01.md, RUNBOOK 0.5.11. vrfSim.mtl: notifyLevel 3 /
objectConsoleNotifyLevel 3 / enableLogFileTimestamps 1 (backup .bak-20260901) - KEEP: it is
what made the freezes speak.

## FIXED-FRAME RUN-TO-COMPLETE - THE SPEED-UP LEVER (CLOSED, validated 2026-09-02)
Full record: docs/experiments/PREREG_R9_FIXED_FRAME_RTC_2026-09-02.md sec 8 (prereg 2030ebd,
outcome c0e90b7, run 20260902T140808Z, appNos 3726-3732). VERDICT PASS, no falsifier.
THE RULE: ALL PROBES RUN UNDER FFRTC MODE unless the prereg states why not; TIMEMULTIPLIER STAYS
1x; the multiplier and at-distance LADDERS ARE WITHDRAWN. The SCENARIO'S EXERCISE CLOCK MODE,
not TimeMultiplier, is the speed lever (Users Guide 3.4.3 / 7.6.1 / 12.2.1 + Table 17).
FIXTURE: tools/FixtureGen/frame_variants/TropicTortoise_FFRTC.scnx - stock TropicTortoise with
TWO lines moved, (frame-mode "fixed-frame-run-to-complete") + (frame-time 0.033333); DEPLOYED at
C:\MAK\vrforces5.0.2\userData\scenarios\ (SHA-256 D27E540F8BCC...B0B9, 7112 bytes); load with
-Scenario TropicTortoise_FFRTC. Stock TropicTortoise.scnx untouched. Answer unchanged across 4
runs of the R9 order; ~9x wall, LOAD-DEPENDENT (7.4-13.1 sim-s per wall-s). Mode check:
`python tools/analysis/frame_gaps.py . <run>`, PASS = Test A >= 95% in {0.033,0.034} AND R >= 0.99.
OPEN DESIGN ITEM (not a blocker): OUR APP HAS NO NOTION OF SIM TIME - VrfFacade.cpp:478-482 pins
the federate clock to elapsedRealTime, the TickLoop is 20 Hz WALL and every app timeout is wall;
compression only ever gives a wall budget MORE margin. LOW PRIORITY: an observed-run
fastForwardSettings.mtl entry (7.6.1) NEEDS USER OK first.

## COA-STP1 SCALE RE-RUN - RUNG 1 RESULT AND ROOT CAUSE
RUNG 0 DONE (fc93a1e): July region hypothesis RETRACTED in six live docs; 31 (not 32) temporal
deps; DEFECTS A/B verified ALREADY FIXED in source (InFlightTracker, TaskSequencer).
RUNG 1 RESULT (run 20260902T125423Z, appNos 3718-3724; prereg d1f2e10, outcome sec 6 of
PREREG_COASTP1_RUNG1_BOUNDED_2026-09-02.md, 7963aed): the July mechanism is GONE (zero
"moveAlong() - empty route" in 140 MB - that grep oracle is DEAD) but 4 of 8 dispatching
aggregates built ZERO offset routes and never moved, SILENTLY; the other 4 marched 13.2-26.7 km
at 8.0-8.2 m/s, offset routes and movement correlating 1:1 across all 8. THAT is the freeze the
route-uuid fix below repairs; rung 2 re-tests it.
OTHER FINDINGS (still unfixed): (A) the lone ENTITY taskee reported TASKCMPLT from the BACK
END's own callback while never leaving its spawn ring - a VACUOUS COMPLETION that falsely
released T24 and cascade-skipped T25/T26 (needs the NATIVE success()/taskId() item below);
(B) all 26 echelon-'F' units land the GENERIC Ground_Aggregate fallback (UnitTranslator.cs:70/
:134, TYPE_GAP_ADJUDICATION item 4, a USER call) - but that fallback MARCHES, so not a freeze
cause; (C) TerrainProfile re-entry double-logs the verb classification; (D) ResetVrf after
StopVrf is blind (BackendCount=0) - run it between StopIface and StopVrf.

RUNG 1 ROOT CAUSE - **FOUND, SETTLED BY MANIPULATION, FIXED (726f762), AND LIVE-VERIFIED.**
Full record: docs/experiments/PREREG_ROUTE_UUID_FIX_2026-09-02.md sec 6. Chain: discriminator
9/9 (ANALYSIS_COASTP1_RUNG1_FREEZE_2026-09-02, H1-H5 refuted sec 4) -> manipulated cause
(PREREG_ROUTE_NAME_LENGTH_2026-09-02 sec 6, 854841a) -> fix -> re-verified.
THE DEFECT WAS OURS, not the vendor's: a contract violation at the DtUUID string ctor
(`rwUUID.h:246-253` - a VALID uuid ONLY from a "VRF_UUID:..." string; anything else becomes an
object-marking-text lookup in the 36-byte blob at `rwUUID.h:412`). We handed the route's NAME to
`moveAlongRoute(DtUUID, DtUUID)`, so a name over 34 chars arrived CUT TO 35, `myRoute` never
resolved, no offset routes were built, the aggregate froze SILENTLY. CreateRoute's DtString is
unbounded, so the route OBJECT existed at full length all along - which is exactly why July's
"names pass at 99 chars" test (it tested CREATION, never the task) came out clean.
MOJAVE_ROOTCAUSE part 12's "name length falsified" is OVERTURNED.
THE FIX (C# ONLY - no native source, no bridge rebuild; bridge stays A7504441):
`OnVrfObjectCreated` already had the route's REAL uuid (`e.Uuid` at :1110, the `uuidString()`
the callback carries - vrfRemoteController.h:102-103, VrfFacade.cpp:211). All FOUR
route/waypoint call sites now pass it instead of `e.Name` (MoveAlongRoute, R10 fan-out,
PatrolRoute, PlanAndMoveTo); the pending queue stays keyed by route NAME, which works.
VERIFIED LIVE (run 20260902T153837Z_run, appNos 3742-3748, FFRTC, 1x): SAME padded order
byte-identical, SAME 44-char name, ONE variable moved - the app binary. 114.MechCoy marched
698.97 m to 34.653915,-116.693388, **0.00 m from the short-name control's endpoint**, parked
905 samples / 1863 s; the other two taskees also 0.00 m; 3/3 TASKCMPLT (frozen run: 2). The
back end printed the FULL 44 chars at bin64-vrfSim.log:5821 where the frozen run cut it at 35.
Sub-routes 114.MechCoy_R0..R3 built and tasked, 30 mentions / 4 distinct (frozen: 0). Freeze
diagnostic `buildEntityRouteFollowingMap() : Can't find entity route` **67,590 -> 0**.
**ROUTE NAMES STAY FULL LENGTH** - "cap at 34 chars / short synthetic route ids" are WITHDRAWN,
not deferred: workarounds for a bug that no longer exists. Users Guide 41.1 p.989 allows 255.
NOT EXERCISED by this order, so NOT verified live: PatrolRoute and PlanAndMoveTo (no
SCREEN/SCOUT verb, AggregatePlanAndMove off) - changed-by-argument, not proven.
STILL QUEUED, NATIVE, UNCHANGED BY THIS WORK: forward `DtTaskCompleteReport::success()` /
`taskId()` / `taskTrackingNumber()` through VrfFacade::TaskCompleted. All three are DROPPED at
VrfFacade.cpp:217-242 (struct TaskCompleted, VrfFacade.h:119-123 has no such field), so a
success=false FAILURE is indistinguishable from a real success - that is what let rung-1
finding A through as a TASKCMPLT. Under STANDING AUTHORIZATION: back up the DLLs, /t:Rebuild
always, REDEPLOY ALL 10 COPIES, verify ONE hash across them.
RUNNER DEFECTS - both found live, both FIXED, both test-covered (gate 96 -> 105 checks):
(1) RunC2SimScenario.ps1:2169 wrapped `@()` around the CALL, so `.Missing` member-enumerated and
a one-element result unwrapped to a bare string; `$missing.Count` threw under Set-StrictMode
(run 20260902T143638Z, EXIT=5). Now `@( (Test-EarlyExit ...).Missing )` - EXERCISED LIVE, runner
EXIT=0. (2) RunnerLib.ps1 `Get-VrfUuidByName` `$rxB` did not tolerate the route uuid the app now
logs, so the report-evidence gate mapped no taskee and `-StopWhenComplete` never fired on run
20260902T153837Z - the window ran its 1800 s cap against a healthy 3/3 app log; the
parenthetical is now OPTIONAL so BOTH forms parse (verified against a reproduced failure).
LESSON: A LOG-FORMAT CHANGE IS AN API CHANGE WHEN ANOTHER TOOL PARSES THAT LOG - grep the repo
for the old format before changing it.

## OPERATIONAL STATE (2026-09-02, after the ROUTE-UUID FIX verification run)
VR-FORCES DOWN between runs (StopVrf exit 0, "graceful quit; no process was force-killed").
appNo marker NEXT FREE = 3750, authoritative marker in docs/OPUS_EXECUTION_PLAN.md Appendix B
(runner-managed, ledger CRLF). 2026-09-02 consumed 3676-3749 - see the ledger.
DEPLOYED APP (the fix): src\VrfC2SimApp\bin\Release\net10.0\win-x64\VrfC2SimApp.dll SHA-256
3b7b8d2e...c60cea0, built 2026-09-02 11:30. The runner starts the app straight from that path
(RunC2SimScenario.ps1:382) - building IS deploying for the APP; only the BRIDGE has a 10-copy
deploy step. RTI RESIDENT + ANSWERED: rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620 -
UNCHANGED across every 2026-09-02 run; still inventory fresh at session start, do not trust
PIDs. C2SIM docker UP. Deployed bridge = A7504441 (10/10 copies, Ijwhost 38255036; backups
bak-20260902-a48abe6c/ and bak-20260902-28e993fe/). Dump 70668 sits in bin64, no newer one
(RUNBOOK 0.5.12: scripts/AnswerCrashDumpDialog.ps1 answers the prompt; never halt on it).
Firewall: do NOT set NotifyOnListen False (user ruling); Cancel the testhost prompt. MAK license
expires 2026-09-15, renewal in process - verify the new .lic landed before running after that.
CORRECTION 2026-09-02: THE VENDOR LOG'S WALL STAMPS ARE LOCAL TIME (-04:00), NOT UTC - convert
before cross-referencing a bin64-vrfSim.log stamp against a UTC artifact (ours stamp UTC).

## NEXT (in order)
1. COA-STP1 RUNG 2 - THE NEXT RUN. FULL order under FFRTC with the route-uuid fix; R9 has
   re-verified (20260902T153837Z). ~9x compression makes the whole order affordable at 1x, so
   rung 1's bounded window is no longer the constraint. Expect T13 NOT to dispatch (12,000 s
   start delay, not a miss). PREREGISTER: rung 1's four frozen aggregates must now build offset
   routes and march, and `buildEntityRouteFollowingMap() : Can't find entity route` must be 0
   (rung 1: 14,913). Rung-1 finding A (the vacuous ENTITY completion) WILL STILL BE THERE - it
   needs item 2, which is NOT in this fix.
2. NATIVE COMPLETION STATUS - forward DtTaskCompleteReport success()/taskId()/
   taskTrackingNumber() through VrfFacade::TaskCompleted (details in the rung-1 block above).
   The only known remaining cause of a FALSE TASKCMPLT. Standing authorization applies.
3. TYPE_GAP ITEM 4 - the echelon-'F' -> generic Ground_Aggregate fallback needs a USER RULING
   (docs/TYPE_GAP_ADJUDICATION.md, Decision item 4). Still pending.
4. SIM-TIME READ-BACK - the design item in the FFRTC block. Nothing is blocked on it today.
5. MAK MESSAGE - docs/MAK_MESSAGE_2026-09-02.md is send-ready and THE USER SENDS IT.
   *** ITS APPENDED DtUUID ROUTE-NAME-LENGTH DRAFT IS NOW STALE AND MUST NOT BE SENT AS A
   DEFECT REPORT: the cause was OUR contract violation, not a vendor bug (rwUUID.h:246-253
   documents exactly this behaviour, and the fix is 726f762). REWRITE OR DROP THAT SECTION
   BEFORE SENDING. *** Vendor defects found across the whole saga remains ZERO.
6. BACKLOG unchanged: type adjudications (54 units - see the 5.2b checklist first), task
   vocabulary, completion re-keying, scoring (Phase 5).

## VR-FORCES 5.2b UPGRADE CHECKLIST (expected soon - user, 2026-09-01)
(a) diff its EXPANDED AGGREGATE MODEL SET against the 54 pending type adjudications BEFORE
authoring anything (PRIOR_ART Q1); (b) 5.2's "ground path planning enhanced with vector-based
terrain data" touches exactly today's route/clamp machinery - re-run R9 on 5.2b and RE-ADJUDICATE
before trusting any 5.0.2-era behavioural conclusion; (c) migrate local state DELIBERATELY:
vrfSim.mtl notify levels, the DISABLED NavArea artifact (do NOT carry it into 5.2's SharedData),
the FFRTC fixture, runner/env paths pinned to vrforces5.0.2, and a full VrfBridge /t:Rebuild +
10-copy redeploy; (d) read the API migration guides FIRST (docs.mak.com/api/vrforces5.2/
classref/vrf_migration50.html + vrf_migration51.html).

## PROBE PROTOCOL (adopted 2026-09-01/02; do NOT change mid-protocol)
FFRTC mode per the block above; TimeMultiplier 1x; -RunSecs is a CAP under -StopWhenComplete.
SHORT-ROUTE probe variants are allowed - platoon/entity legs may be ~200 m - but COMPANY probe
routes stay >= ~1 km (formation depth ~430 m plus leading-edge completion). Keep ONE
canonical-length run per milestone for comparability. THE 5x MULTIPLIER RECORD is superseded and
NOT to be re-run (its one unexplained miss: ANALYSIS_P3_STEP_PROFILE_2026-09-01.md). watchvrf
POS mid-move is DEAD-RECKONED - only plateaus are truth.

## NON-NEGOTIABLES (unchanged, plus the docs-first rule above)
One variable per probe; prediction + falsifier + DOC CITATIONS before running; movement gate =
static -> moving -> settled with POS/RPT agreement; never kill a joined federate; never kill
rtiAssistant / rtiexec / rtiForwarder without a fresh ruling; fresh ledgered appNo per join;
ASCII in tracked files (ripgrep, not grep -P); after two consecutive infra failures, research
before retry. FALSIFIED stamps are LAYER-RELATIVE (L9) - re-adjudicate on the clean state.
Prove any instrument reproduces the KNOWN result before trusting a new one (lessons: false
greens 2026-07-19; this session's regex fix was checked against a reproduced failure).
