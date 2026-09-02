# SESSION-JUMP HANDOFF (opened 2026-09-01, current 2026-09-02) - THE AGGREGATE FREEZE IS FIXED

THE CURRENT entry point (newest HANDOFF_*.md by git log). SUPERSEDES
HANDOFF_2026-07-23_TYPEFIX_CONFIRMED.md. ASCII only. HARD CAP 200 LINES - when a phase closes,
collapse it to a few lines plus a pointer; never drop live guidance to make room. RE-VERIFY
load-bearing claims against artifacts before trusting prose.

## CLOSED - DO NOT REOPEN (tripwires; each line names its record)
- ROUTE FREEZE = OUR ROUTE-BY-NAME ADDRESSING (a name over 35 chars cut in DtUUID's 36-byte
  blob), not region / template / type / waypoint altitude / vertex count / creation order /
  pile density / point count. **FIXED 2026-09-02 (726f762), LIVE-VERIFIED ON R9 AND AT SCALE** -
  pass the route's real uuid: PREREG_ROUTE_UUID_FIX sec 6 (run 20260902T153837Z) and
  PREREG_COASTP1_RUNG2 sec 7 (run 20260902T165144Z, ALL NINE COA-STP1 performers march, names to
  99 chars). DO NOT shorten or cap route names - WITHDRAWN and unnecessary (255 allowed).
  Route-name length predicted rung 1's 9-of-9 mover/freezer split PRE-REGISTERED, which CLOSES
  rung 1's "why these four?". Reopening evidence = a cut name, or a nonzero
  `Can't find entity route`, in a run on this binary.
- THE TIME MULTIPLIER IS NOT THE LEVER - the multiplier and at-distance ladders are WITHDRAWN;
  the SCENARIO CLOCK MODE is the only clock lever, and its direction depends on load (next
  line). PREREG_R9_FIXED_FRAME_RTC_2026-09-02.md sec 8 (2030ebd, c0e90b7).
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
- FFRTC IS NOT A SPEED LEVER AT SCALE - a 3.77x SLOWDOWN on COA-STP1 (0.2652 sim-s per wall-s vs
  0.9995 variable-frame and 7.4-13.1 on the 3-unit R9 order). See the FFRTC block; do NOT budget
  a scale run as if FFRTC compresses.
- Birth altitude, "nav data ruled out", the 10-char marking collision: docs/CORRECTIONS_LOG.md.
  Every July FALSIFIED stamp is LAYER-RELATIVE (L9) - re-adjudicate before trusting it.

## THE STANDING RULE (user directive 2026-09-01, enforced)
DOCUMENTATION FIRST for any issue: local help -> the PUBLIC Developer's Guide at
docs.mak.com/api/vrforces{5.2,5.1.1,4.10}/classref/ -> internet research. A live probe is
registrable ONLY after its prereg cites the documentation consulted. Two months of probing
dissolved in one afternoon of reading (2026-09-01). VRF_GROUNDWORK_PLAN lessons L8-L10.

## ONE-LINE STATUS
THE AGGREGATE FREEZE IS FIXED AND THE FIX HOLDS AT SCALE. Routes are addressed by their real
VRF_UUID, not by name (726f762, C# only). R9 runs end-to-end headless 3/3 TASKCMPLT; the FULL
COA-STP1 ORDER marches ALL NINE dispatching performers, `Can't find entity route` 14,904 -> 0,
names intact to 99 chars (20260902T165144Z). The MERGED build (type-mapping table) is GATED and
reproduces all of it (20260902T181203Z). FFRTC is a 3.77x SLOWDOWN at scale.

## WORKING CONFIGURATION
TypeMappingMode=RealTemplates + GroundWaypointAltitudeMode=TerrainProfile (both compiled
DEFAULTS since 2026-09-02, bridge A7504441) + NavArea disabled + STOCK templates, no env
overrides - an untouched product at default settings. Vendor defects found across the whole
saga: ZERO (the one candidate, the DtUUID route-name cut, was our own contract violation).
Runner hardening permanent - Stage 2b boot-dialog watcher (AnswerRtiDialog.ps1; the RTI dialog
is ONCE PER REBOOT), Stage 2c RTI gate, per-run bin64 log capture, stop-file trace close, and
off-by-default -StopWhenComplete with -SettleHoldSecs 60 as a FLOOR plus rule 4 (every taskee
needs an RPT LATER than its TSK, within 2 m of its latest POS): RUNNER_TURNAROUND_2026-09-01.md,
RUNBOOK 0.5.11. vrfSim.mtl: notifyLevel 3 / objectConsoleNotifyLevel 3 /
enableLogFileTimestamps 1 (backup .bak-20260901) - KEEP: it is what made the freezes speak.

## FIXED-FRAME RUN-TO-COMPLETE - VALIDATED, BUT **NOT** A SPEED LEVER AT SCALE
Records: PREREG_R9_FIXED_FRAME_RTC_2026-09-02.md sec 8 (2030ebd / c0e90b7, run 20260902T140808Z);
PREREG_COASTP1_RUNG2_2026-09-02.md sec 7 (the scale measurement).
THE RULE (revised 2026-09-02 by rung 2): FFRTC stays the default for REPEATABILITY and for
time-managed HLA; TIMEMULTIPLIER STAYS 1x (multiplier and at-distance ladders remain WITHDRAWN;
the scenario clock mode is the lever - Users Guide 3.4.3 / 7.6.1 / 12.2.1 + Table 17). But
**FFRTC DOES NOT COMPRESS AT SCALE** - measured, frame_gaps.py LS slope, sim-s per wall-s:
  R9 3 units FFRTC 7.43-13.11 | COA-STP1 FFRTC 0.2652 (...165144Z) | COA-STP1 VARIABLE 0.9995
FFRTC advances a FIXED 0.0333 sim-s per frame "even if a frame takes longer than the fixed amount
to compute" (3.4.3), so wall = sim_s / frame_time x frame_cost and the ratio lands on WHICHEVER
SIDE OF 1.0 the load puts it; variable-frame is pinned at 1.0x. BUDGET A SCALE RUN AS
wall = sim / 0.27, or say why variable-frame is right for it. CORRECTION: "compression only ever
gives a wall budget MORE margin" IS FALSE at scale - our task timers are WALL (600 s predecessor
timeout, T13's 12,000 s delay), so at 0.2652 a 600 s wall timeout is 159 SIM seconds, 4x tighter.
NEW, MEASURED (rung 2): THE C2SIM POSITION-REPORT STREAM IS SIM-PACED, NOT WALL-PACED - 1,536
reports vs rung 1's 5,717 over the same wall window and same 128 units, ratio 3.72 against the
clock ratio 3.77. TIMERS are wall; DATA is sim-paced, so "N reports" or "a report within X
seconds" inherits the sim clock silently. THRESHOLD RULE (adopted 2026-09-02): EVERY speed or
timeout threshold MUST NAME ITS CLOCK - rung 2's P4(c) miss was exactly this error.
FIXTURE: tools/FixtureGen/frame_variants/TropicTortoise_FFRTC.scnx - stock TropicTortoise with TWO
lines moved, (frame-mode "fixed-frame-run-to-complete") + (frame-time 0.033333); DEPLOYED at
C:\MAK\vrforces5.0.2\userData\scenarios\ (SHA-256 D27E540F8BCC...B0B9, 7112 bytes); load with
-Scenario TropicTortoise_FFRTC. Stock TropicTortoise.scnx untouched. Mode check
`python tools/analysis/frame_gaps.py . <run>`: PASS = Test A >= 95% in {0.033,0.034} AND R >= 0.99.
It DISCRIMINATES - rung 1 (variable-frame) scores R = 0.0276.
OPEN DESIGN ITEM, raised to MEDIUM by rung 2: OUR APP HAS NO NOTION OF SIM TIME
(VrfFacade.cpp:478-482 pins the federate clock to elapsedRealTime; TickLoop is 20 Hz WALL). At
0.27x the mismatch changes which task chains dispatch. LOW: an observed-run
fastForwardSettings.mtl entry (7.6.1) NEEDS USER OK first.

## COA-STP1 SCALE - CLOSED (rungs 0/1/2). Full records in the preregs; this is the residue.
Rung 0 fc93a1e. Rung 1 run 20260902T125423Z (prereg d1f2e10, outcome 7963aed): 4 of 8 dispatching
aggregates plus the lone entity built ZERO offset routes and never moved, SILENTLY, with the July
"empty route" grep oracle DEAD. Rung 2 run 20260902T165144Z (prereg b3792d1, outcome
PREREG_COASTP1_RUNG2_2026-09-02.md sec 7): ALL NINE performers march; nine route names arrive at
FULL length 29-99 chars with ZERO 35-char cuts; 8 of 8 aggregates build offset routes; `Can't
find entity route` 14,904 -> 0; lateral 1-418 m; 0 clean objects outside the AO box; cleanup 172.
ROOT CAUSE (SETTLED, FIXED 726f762, VERIFIED TWICE): OURS, not the vendor's - a contract
violation at the DtUUID string ctor (`rwUUID.h:246-253`; the 36-byte blob at `rwUUID.h:412`, one
byte the type tag, so a marking-text name survives to 35 chars). All FOUR route/waypoint call
sites now pass `e.Uuid`, not `e.Name`; the pending queue stays keyed by route NAME. C# ONLY -
bridge stays A7504441. RUNG 2 STOPPED under its own miss rule on two thresholds, both mine,
nothing retuned or re-run - see its sec 7; hence the THRESHOLD RULE in the FFRTC block.
STILL OPEN: the NATIVE completion-status item (NEXT row 3). Rung 1 finding A (a VACUOUS entity
TASKCMPLT that falsely released T24) DID NOT REPRODUCE - T23 marched 5.94 km, rung 2 had ZERO
TASKCMPLT - so the OCCASION is gone but THE NATIVE GAP IS UNTESTED AND STANDS.
OTHER RUNG-1 FINDINGS, unfixed: (B) 26 echelon-'F' units land the GENERIC Ground_Aggregate
fallback (UnitTranslator.cs:70/:134, TYPE_GAP item 4, USER call) - it MARCHES, not a freeze cause;
(C) TerrainProfile re-entry double-logs the verb classification; (D) ResetVrf after StopVrf is
blind (BackendCount=0) - run it between StopIface and StopVrf.
NOT EXERCISED by either order, so NOT verified live: PatrolRoute and PlanAndMoveTo.
UNEXPLAINED from rung 2: (1) 55 objects report the (90,-90,0) pole for their whole life, a class
ABSENT from rung 1 - plausibly the extra route objects, unproven; (2) cast-corrupted reflections
28 -> 58 objects; (3) T27/T35 lateral ~400 m against 1-72 m for every other performer.

## OPERATIONAL STATE (2026-09-02, after the MERGED-BUILD CONTROL run)
VR-FORCES DOWN (StopVrf exit 0, graceful, RTI preserved); post-run ResetVrf sweep on 3766 joined
clean, 0 reflected, exit 0.
appNo marker NEXT FREE = 3767 (docs/OPUS_EXECUTION_PLAN.md Appendix B, runner-managed, CRLF).
Merged-build control consumed 3759-3765 (3765 unconsumed) + 3766. Rung 2 consumed 3750-3756 +
3758; **3757 IS BURNED** - ResetVrf invoked without the documented launch environment (RUNBOOK
:1208-1215 requires cwd = C:\MAK\vrforces5.0.2\bin64 plus the VR-Forces/VR-Link/makRti bin PATH
prefix AND Machine-scope MAKLMGRD_LICENSE_FILE) and failed before joining. READ :1208-1215
BEFORE RUNNING ResetVrf - the 2026-09-02 18:18 sweep used it and exited 0.
DEPLOYED APP: src\VrfC2SimApp\bin\Release\net10.0\win-x64\VrfC2SimApp.dll SHA-256
**570619630015...ACEB52A6** (2026-09-02 14:02:48) - the MERGED build (3c5af9a), GATED by run
20260902T181203Z (PREREG_MERGED_BUILD_CONTROL sec 7). The runner starts the app from that path
(RunC2SimScenario.ps1:382) - building IS deploying for the APP; only the BRIDGE has a 10-copy
deploy step. Deployed bridge = A7504441 (10/10, Ijwhost 38255036; backups
bak-20260902-a48abe6c/ and bak-20260902-28e993fe/).
CLIENTID TRAP: the DEPLOYED (gitignored) bin\...\appsettings.json Vrf:ClientId must MATCH the
init's SystemName or the runner ABORTS at validation, exit 2 (RunC2SimScenario.ps1:1154-1165).
R9 inits declare STP; COA-STP1 declares C2SIM. It is at "STP". Edit the DEPLOYED copy only.
RTI RESIDENT + ANSWERED: rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620 - UNCHANGED
across every 2026-09-02 run; still inventory fresh at session start, do not trust PIDs. C2SIM
docker UP. Dump 70668 sits in bin64, no newer one (RUNBOOK 0.5.12:
scripts/AnswerCrashDumpDialog.ps1 answers the prompt; never halt on it). Firewall: do NOT set
NotifyOnListen False (user ruling); Cancel the testhost prompt. MAK license expires 2026-09-15,
renewal in process - verify the new .lic landed before running after that.
CORRECTION 2026-09-02: THE VENDOR LOG'S WALL STAMPS ARE LOCAL TIME (-04:00), NOT UTC (ours UTC).

## NEXT (in order)
1. MERGED-BUILD CONTROL on R9 - **DONE 2026-09-02, GATE PASSED** (run 20260902T181203Z, appNos
   3759-3765; prereg 0f75f29, outcome PREREG_MERGED_BUILD_CONTROL_2026-09-02.md sec 7). The
   merged binary is behaviourally identical to 3b7b8d2e at default config: app logs diff to ZERO
   HUNKS after normalisation, endpoints match to six decimals (0.00 m), 3/3 TASKCMPLT, `Can't
   find entity route` 0, ZERO FidelityTable log forms, slope 9.77 in the R9 band. Every
   2026-09-02 conclusion measured on the old binary stands on this one. -StopWhenComplete FIRED
   live for the first time ($rxB fix confirmed), 4 min 36 s wall vs the control's 33 min 38 s.
   ALSO the type-mapping live gate's RUN 1 (RealTemplates control); its RUN 0 passed 783 checks.
2. `-q` (doNotUseConsole) PROBE AT SCALE - registered next. DOCS ANSWER, so the probe's stated
   risk is CLOSED BEFORE LAUNCH: **-q does NOT suppress the log file**, only the console. Users
   Guide Table 8 p.177 ("all vrfSim output go to the log file rather than the console"; a high
   notify level + console "can degrade performance", quiet mode "prevents this degradation"),
   sec 4.9 p.161 (on Windows vrfSim.log is created unconditionally), App. C.1 Table 71 p.1663.
   We run notifyLevel 3 = verbose (5.4.3 p.185) and vrfSimHLA1516e.exe is CONSOLE-subsystem with
   its own Start-Process window, so THE DOCS PREDICT A SPEED-UP.
3. NATIVE COMPLETION STATUS - forward DtTaskCompleteReport success()/taskId()/
   taskTrackingNumber() through VrfFacade::TaskCompleted (rung block above). The only known
   remaining cause of a FALSE TASKCMPLT. Standing authorization: back up the DLLs, /t:Rebuild
   always, REDEPLOY ALL 10 COPIES, verify ONE hash. Rung 2 had ZERO TASKCMPLT, so this now stands
   on the source reading (VrfFacade.cpp:217-242) alone, which is enough.
4. A COMPLETION-CAPABLE SCALE RUN. No COA-STP1 run has reached a route end: the shortest head
   route is 24.11 km, the best progress ever 26.84 km in 2700 sim s. FFRTC makes this WORSE
   (0.2652). Pick the mode from the FFRTC block and budget wall = sim / ratio.
5. TYPE_GAP ITEM 4 - the echelon-'F' -> generic Ground_Aggregate fallback needs a USER RULING
   (docs/TYPE_GAP_ADJUDICATION.md, Decision item 4). Pending; not a movement cause.
6. MAK MESSAGE - docs/MAK_MESSAGE_2026-09-02.md is send-ready and THE USER SENDS IT.
   *** ITS APPENDED DtUUID ROUTE-NAME-LENGTH DRAFT IS STALE AND MUST NOT BE SENT AS A DEFECT
   REPORT: the cause was OUR contract violation, not a vendor bug (rwUUID.h:246-253 documents it;
   fix 726f762). REWRITE OR DROP THAT SECTION. *** Vendor defects across the saga remains ZERO.
7. BACKLOG unchanged: type adjudications (54 units - see the 5.2b checklist first), task
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
SHORT-ROUTE probe variants are allowed (platoon/entity legs ~200 m) but COMPANY probe routes stay
>= ~1 km (formation depth ~430 m). Keep ONE canonical-length run per milestone. THE 5x MULTIPLIER
RECORD is superseded and NOT to be re-run (ANALYSIS_P3_STEP_PROFILE_2026-09-01.md). watchvrf POS
mid-move is DEAD-RECKONED - only plateaus are truth.

## NON-NEGOTIABLES (unchanged, plus the docs-first rule above)
One variable per probe; prediction + falsifier + DOC CITATIONS before running; movement gate =
static -> moving -> settled with POS/RPT agreement; never kill a joined federate; never kill
rtiAssistant / rtiexec / rtiForwarder without a fresh ruling; fresh ledgered appNo per join;
ASCII in tracked files; after two consecutive infra failures, research before retry. FALSIFIED
stamps are LAYER-RELATIVE (L9). EVERY threshold NAMES ITS CLOCK (FFRTC block).
Prove any instrument reproduces the KNOWN result before trusting a new one (lessons: false
greens 2026-07-19; this session's regex fix was checked against a reproduced failure).
