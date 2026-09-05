# SESSION-JUMP HANDOFF (opened 2026-09-01, current 2026-09-02) - THE AGGREGATE FREEZE IS FIXED

THE CURRENT entry point (newest HANDOFF_*.md by git log). Read CLAUDE.md first. SUPERSEDES
HANDOFF_2026-07-23_TYPEFIX_CONFIRMED.md. ASCII only. HARD CAP 200 LINES - when a phase closes,
collapse it to a few lines plus a pointer; never drop live guidance to make room. RE-VERIFY
load-bearing claims against artifacts, not prose.

## CLOSED - DO NOT REOPEN (tripwires; each line names its record)
- ROUTE FREEZE = OUR ROUTE-BY-NAME ADDRESSING (a name over 35 chars cut in DtUUID's 36-byte blob),
  not region / template / type / waypoint altitude / vertex count / creation order / pile density.
  **FIXED 2026-09-02 (726f762), LIVE-VERIFIED ON R9 AND AT SCALE** - pass the route's real uuid:
  PREREG_ROUTE_UUID_FIX sec 6, PREREG_COASTP1_RUNG2 sec 7 (ALL NINE march, names to 99 chars). DO
  NOT shorten route names. Reopening = a cut name or a nonzero `Can't find entity route`.
- THE TIME MULTIPLIER IS NOT THE LEVER - multiplier and at-distance ladders WITHDRAWN; the SCENARIO
  CLOCK MODE is the only clock lever, direction depends on load. PREREG_R9_FIXED_FRAME_RTC sec 8.
- The REGION / Mojave-terrain cause is FALSIFIED (docs/CORRECTIONS_LOG.md "The region hypothesis").
- TYPE MAPPING is fixed: ArmorPlatoon -> real Tank Platoon (USA); RealTemplates is the compiled
  DEFAULT (2026-07-22). **THE STATIC BEST-MATCH METHOD IS LIVE-CONFIRMED** on R9 in BOTH modes,
  6/6 units, from the back end's own `Locally Simulated: <name> ... using parameters:
  ...\vrfSim\<Template>.entity` lines (PREREG_TYPEMAP_LIVE_GATE sec 7). Reopening evidence = a
  creation line naming a template the table does not.
- The 2026-07-14 project-generated NavArea (120k tiles) WAS the 2026-07-15..2026-09-01 freeze; it is
  in SharedData/16/latest/TerrainData/navData/_disabled_20260901/ (RESTORABLE). KEEP DISABLED.
- The HQ-section formation-name warning is COSMETIC; the P2 aliases were reverted (P2c).
- ROUTE-VERTEX ALTITUDE FRAME: TerrainProfile (terrain + 10 m) is the compiled DEFAULT, Live
  (+50 m) the fallback, Fixed100 a relic (DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01 sec 7).
- FFRTC IS NOT A SPEED LEVER AT SCALE - a 3.2-3.8x SLOWDOWN on COA-STP1 (0.265-0.314 vs 0.9995
  variable-frame and 7.4-13.1 on the 3-unit R9 order). Do NOT budget as if FFRTC compresses.
- **ALTITUDE: READ docs/VRF_ALTITUDE_FRAMES.md sec 0, DO NOT RE-DERIVE.** Root = SOURCE frame error:
  C2SIM has AltitudeAGL AND AltitudeMSL, both optional (xsd :2716-2717), our inits carry neither;
  the oracle read either as ABSOLUTE, invented 1000, sent +1 as AGL. FIXED 4b4d0f9 (PlacementPolicy):
  authored lat/lon + setAltitude(agl,TRUE) (vrfRemoteController.h:1372); land + nothing = AGL 0;
  "ground" = DIS domain, not SIDC. 10000/+1/SIDC GONE; LIVE CONFIRMATION OWED (6b). Route VERTICES
  have NO AGL frame (TerrainProfile stays). Birth/waypoint alt NOT freeze causes. "MSL" = ellipsoid.
- **5.0.2 IS ARCHIVE** (user direction 2026-09-04: "Are you still pursuing 5.0.2?" / "why keep
  5.0.2 stuff in there" - supersedes the plan's "must stay runnable side by side"): do NOT repair,
  re-run or spend effort on it. Oracle = the 39 RECORDED run dirs; nothing on the 5.2 path
  launches it (the runner 5.0.2 control is DryRun-only). REOPENING EVIDENCE = a 5.2 result
  adjudicable ONLY by a fresh run; recipe = RESEARCH_502_SIDE_BY_SIDE_2026-09-04.
  MACHINE PATH APPLIED 2026-09-04 (FixMachinePath.ps1 -RemoveLegacyMak): 44 -> 34 entries,
  vrforces5.0.2\bin64 + vrlink5.8\bin64 OFF the PATH (installs untouched on disk).
  %MAK_RTIDIR%\bin stays, REG_EXPAND_SZ - drift lives in MAK_RTIDIR (=makRti5.0.1), not PATH.
  Backup runs/env-backup/MachinePATH_before_20260904T142048Z.txt (restore line in the script).
  RUNNER UNAFFECTED (builds $PathPrefix, RunC2SimScenario.ps1:1933); a bare VrfC2SimApp.exe now
  FAILS to bind (vl/vlutil not beside it) instead of silently loading 2022 DLLs.

## THE STANDING RULE (user directive 2026-09-01, enforced)
DOCUMENTATION FIRST for any issue: C:\MAK\vrforces5.2d\doc (Users Guide, IOG, RTI manuals) -> docs.mak.com
classref -> internet. A probe is registrable ONLY after its prereg cites the docs consulted (2026-09-03: a
night of RTI probing dissolved in one sentence, UG52 5.5.1 p190). NO questions to MAK (user 2026-09-04).

## ONE-LINE STATUS
5.0.2: THE AGGREGATE FREEZE IS FIXED AND HOLDS AT SCALE (routes by real VRF_UUID, 726f762); R9 3/3
TASKCMPLT; the FULL COA-STP1 ORDER marches (`Can't find entity route` 14,904 -> 0, names to 99 chars);
merged build GATED; type map LIVE-CONFIRMED 6/6; FFRTC = 3.2-3.8x SLOWDOWN at scale; OPEN: Tank Company
distribution non-determinism (PARKED). 5.2: launch/join/control/OBSERVATION proven; 2026-09-05 the
FIRST C2SIM INIT RAN END-TO-END ON 5.2 (PREREG_PLACEMENT_R9_52): PushInit -> app late-join -> 6
units created AT queried terrain height -> 44/44 on terrain in WatchVrf. Order/movement not yet
(PREREG_R9_52 next). No frame-mode claim until REBASELINE_52.
## WORKING CONFIGURATION
TypeMappingMode=RealTemplates + GroundWaypointAltitudeMode=TerrainProfile (both compiled DEFAULTS
since 2026-09-02, bridge A7504441) + NavArea disabled + STOCK templates, no env overrides - an
untouched product at default settings. Vendor defects across the whole saga: ZERO (the one
candidate, the DtUUID route-name cut, was our own contract violation).
Runner hardening permanent (RUNBOOK 0.5.11) - Stage 2b boot-dialog watcher (AnswerRtiDialog.ps1;
the RTI dialog is ONCE PER REBOOT), Stage 2c RTI gate, per-run bin64 log capture, stop-file trace
close, off-by-default -StopWhenComplete (-SettleHoldSecs 60 is a FLOOR; rule 4 = every taskee needs
an RPT LATER than its TSK within 2 m of its latest POS) and off-by-default -QuietBackend.
vrfSim.mtl: notifyLevel 3 / objectConsoleNotifyLevel 3 / enableLogFileTimestamps 1 - KEEP.
## FIXED-FRAME RUN-TO-COMPLETE - VALIDATED, BUT **NOT** A SPEED LEVER AT SCALE
(records: PREREG_R9_FIXED_FRAME_RTC sec 8; PREREG_COASTP1_RUNG2 sec 7 for the scale numbers)
THE RULE: FFRTC stays the default for REPEATABILITY and time-managed HLA; TIMEMULTIPLIER STAYS 1x
(ladders WITHDRAWN; the scenario clock mode is the lever - Users Guide 3.4.3 / 7.6.1 / 12.2.1). But
**FFRTC DOES NOT COMPRESS AT SCALE** (5.0.2-measured, LS slope sim-s/wall-s: R9 3 units 7.4-13.1;
COA-STP1 0.265-0.314; variable 0.9995). It advances a FIXED 0.0333 sim-s per frame regardless of
compute cost (UG 3.4.3), so the ratio lands on whichever side of 1.0 the load puts it. BUDGET
wall = sim / measured ratio; task timers are WALL (at 0.2652 a 600 s timeout is 159 SIM s). The
report stream is SIM-PACED. The LS slope is the NOISIER clock (residual sd 1.66-58.66) - cross-check
with the fit-free report count. THRESHOLD RULE: every speed/timeout threshold MUST NAME ITS CLOCK
AND BOUND ITS DENOMINATOR. NO 5.2 FRAME BASELINE EXISTS YET (REBASELINE_52) - PREREG_R9_52 may not
cite frame mode until one does. 5.0.2 fixture TropicTortoise_FFRTC.scnx; mode check `python
tools/analysis/frame_gaps.py . <run>` PASS = Test A >= 95% in {0.033,0.034} AND R >= 0.99 (rung 1
variable-frame scores 0.0276, so it DISCRIMINATES). OPEN DESIGN ITEM, MEDIUM: OUR APP HAS NO NOTION
OF SIM TIME (VrfFacade.cpp:478-482 pins the federate clock to elapsedRealTime; TickLoop is 20 Hz
WALL); at 0.27-0.31x that changes which chains dispatch.
## COA-STP1 SCALE - CLOSED (rungs 0/1/2). Full records in the preregs; this is the residue.
Rungs 0-2 (fc93a1e / d1f2e10 / b3792d1): ROOT CAUSE SETTLED, FIXED 726f762, verified 5x - OURS, a
DtUUID string-ctor contract violation (rwUUID.h:246-253/:412, names survive to 35 chars).
STILL OPEN (5.0.2-era, carried to 5.2): the NATIVE completion-status item; (B) 26 echelon-F units
land the GENERIC Ground_Aggregate fallback (UnitTranslator.cs:70/:134, TYPE_GAP 4, USER call) - it
MARCHES; (C) TerrainProfile re-entry double-logs; (D) ResetVrf after StopVrf is blind.
UNEXPLAINED (5.0.2, PARKED - numbers in the QPAIR/rung preregs): 1,732 ever-real was old-config
residue (**DO NOT REUSE AS A CHECK**); reflections 28 -> 58 cast-corrupted; **THE THREE TANK
COMPANIES ARE THE UNSTABLE CLASS** - identical invocations stall or march (QPAIR A-1 vs A-2).
## OPERATIONAL STATE (2026-09-02, after the TWO no-`-q` COA-STP1 runs of the QPAIR probe)
appNo marker: OPUS_EXECUTION_PLAN.md Appendix B (READ THE MARKER; per-run history there). 3757 IS
BURNED (ResetVrf without the RUNBOOK :1208-1215 launch env). 3908-3910 = the 5.2 fixture run.
Building IS deploying for the APP (bin, :382); only the BRIDGE has a 10-copy deploy step.
CLIENTID TRAP (LIVE): the DEPLOYED (gitignored) bin\...\appsettings.json Vrf:ClientId must MATCH
the init's SystemName or the runner ABORTS at validation, exit 2 (RunC2SimScenario.ps1:1154-1165).
R9 inits declare STP, COA-STP1 declares C2SIM; it currently reads "STP". DEPLOYED copy only.
RTI: inventory fresh at session start - never trust recorded PIDs. Docker: `c2sim-server-vrf` is
OURS (18080/61614); the operator's servers are NOT. Firewall: do NOT set NotifyOnListen False (user
ruling); Cancel the testhost prompt. MAK licence expires 2026-09-15. VENDOR LOG STAMPS LOCAL (-04:00), ours UTC.

## NEXT (in order)
DONE 2026-09-02 pm (READ THE PREREG SEC 7s): merged-build control GATE PASSED (3759-3765); `-q` at
scale STOPPED on its miss rule (3767-3773); type-map live gate PASSES (3775-3781; 6/6, 3/3).
1. **TANK-COMPANY NON-DISTRIBUTION - ADJUDICATED (PREREG_COASTP1_QPAIR sec 9):** `-q` FALSIFIED
   as the cause; NON-DETERMINISM SUPPORTED (A-1 vs A-2 identical, 0 vs 4 sub-routes); run B NOT
   owed. PARKED behind the 5.2 migration (5.2 replaced this mechanism - Y-11/RN VRF-8968).
2/3. DONE 2026-09-02 (a5cdc95): rule-4 `~PXY` marking match (replayed 3/3 at 0.00 m); the private
   `c2sim-server-vrf` (18080/61614) is the runner DEFAULT and reaches every stage (RUNBOOK sec 1;
   **NEVER touch the operator's 8080/61613**). STILL OWED by the type-map gate: run 3 (PRC must
   REFUSE TO START) and run 4 (COA-STP1, 128 units). FidelityTable is NOT default - USER decision.
4. NATIVE COMPLETION STATUS - forward DtTaskCompleteReport success()/taskId()/taskTrackingNumber()
   through VrfFacade::TaskCompleted (VrfFacade.cpp:217-242). Only known FALSE-TASKCMPLT cause. Std
   auth: back up DLLs, /t:Rebuild, REDEPLOY 10 COPIES, verify ONE hash.
5. A COMPLETION-CAPABLE SCALE RUN. No COA-STP1 run reached a route end (shortest head 24.11 km,
   best 26.84 km). Mode from the FFRTC block; budget wall = sim / ratio.
6. TYPE_GAP ITEM 4 (echelon-'F' -> Ground_Aggregate) needs a USER RULING (TYPE_GAP_ADJUDICATION.md).
6a. **RECONCILIATION (docs/VRF_5.2_PLAN_RECONCILIATION_2026-09-04.md, AUDITED)**: FIVE ruled items
   unqueued (A1 A2 A3 A5 A6); A4 WITHDRAWN; B1 CLOSED (Y-7: no nav mesh needed - MoveAlongRoute does
   no planning; do NOT regenerate nav data). Before the R9 run only B2 (does the route touch roads).
   Then A1 Y-9 knobs NOT WIRED (FIXED-FRAME only; seed has no delivery path), A2, A3, A5, A6.
   **STANCE 2026-09-05: THE CODE IS ENTITY-LEVEL ONLY.** Every scenario ever loaded declares
   EntityLevel.sms; nothing branches on the SMS; the Y-15 hybrid = two scenario PROFILES per run
   (13.7 forbids mixing), NOT IMPLEMENTED. Placement acts on member PLATFORMS (VRF_ALTITUDE_FRAMES).
6b. **PLACEMENT - DONE AND LIVE-CONFIRMED 2026-09-05 (def8a5c + the run, PREREG_PLACEMENT_R9_52).**
   The create asks the back end for terrain height at each point (one DtIfRequestTerrainProfileInfo,
   the route path's plumbing) and creates AT terrain+1 m. Run with the set ISOLATED: **44/44 on
   terrain, mech=CREATE, 0 fail**; 6/6 TERRAIN QUERY (real heights even for points sent ~1150 m
   below); no -0.0/10000/NaN. 10000 m birth, +1, SIDC test GONE; AGL set proven not needed, kept
   default-ON. NEXT = the ORDER run (PREREG_R9_52), 5.2 basis in RESEARCH_52_MOVEMENT_ORDER_
   2026-09-05: completion = ALL-subordinates (not 5.0.2 leading-edge); arrival at the SHIPPED
   at-distance 1.0 m / near 15 m (NOT 250 m); route CONSUMED (route-by-uuid holds); expect the
   VRF-8968 formation wait; GATE the order on all-taskees-created (DROPPING/ABANDONING == 0).
7. MAK MESSAGE - docs/MAK_MESSAGE_2026-09-02.md is send-ready and THE USER SENDS IT. *** ITS
   APPENDED DtUUID ROUTE-NAME-LENGTH DRAFT IS STALE - the cause was OUR contract violation
   (rwUUID.h:246-253; fix 726f762). REWRITE OR DROP IT; do not send it as a defect report. ***
8. BACKLOG: type adjudications (54 units), task vocabulary, completion re-keying, scoring
   (Phase 5). LOW (user, 2026-09-02): a DIRECT FILE-LOAD PATH - init + order from files, C2SIM
   server BYPASSED (SDK path stays default) so it runs where there is NO server; reports need a
   file sink. A deployment mode, not a shortcut.
## VR-FORCES 5.2 MIGRATION - IN PROGRESS (user ruling 2026-09-02: move to 5.2d, docs first)
Phase 0 DONE 2026-09-02 + evidence pass 2026-09-03: docs/VRF_5.2_MIGRATION_DIFF.md (rows A-E
cited, C# re-verify list F, PHASE 1 RECORD sec H, sec G = the CANONICAL decision ledger, ALL
RULED Y-1..Y-16; full ruling texts in docs/VRF_5.2_DECISION_EVIDENCE.md). Cold-start check:
docs/VRF_5.2_COLD_START_MAP.md; STP vocabulary docs/STP_TASK_VOCABULARY_2026-09-03.md. PHASE 1 COMPILE DONE 2026-09-03 (sec H): VrfBridge configs Release (5.0.2) | Release-5.2 |
Release-5.2-HLA4, 0 errors, v145 (v143 GONE); app `dotnet build src\VrfC2SimApp -c Release
-p:BridgeConfig=Release-5.2` -> bin\Release-5.2\; 8/8 offline self-tests on both stacks; 7-field
type fix CONFIRMED; 5.2 type table data/unit-type-map-52.json (AR Scout PROXY, Y-8).
TRAP: a 5.2 process needs the 5.2 PATH prefix (name-bound MAK DLLs; NativeStackInfo() logs
which stack bound). FIRST 5.2 LAUNCH+JOIN PROVEN 2026-09-03 pm (PREREG_52_LAUNCH_2026-09-03.md):
scripts/LaunchVrf52.ps1 (independent mode; LaunchVrf.ps1 is INVALID on 5.2) -> "Joined
federation MAK-ONE-2025", healthy back-end 36 threads. Assistants are VERSION-LOCKED (a 5.0.1 one
rejects every 4.6.1 LRC); we run assistant-free via per-process RTI_ASSISTANT_DISABLE (DIFF A12/H).
The 2026-09-03 TOOL GATE (observation channel FAILED, reflected=0 x4) and both its hypotheses are
SUPERSEDED by the lightweight finding below - PREREG_52_TOOLJOIN sec 6. A13 DtHaveRtiLicense stays
a GATE (licence VERIFIED live). USER RULING 2026-09-03: NO OLD BITS - 5.2 runs on MAK RTI 5.0.1
(PREREG_52_REFLECTION sec 5). rtiexec 15720 + forwarder 43728 + four -K assistants LEFT RUNNING
(never kill). NO questions to MAK (user
2026-09-04). RESOLVED 2026-09-04 (PREREG_52_RTIEXEC_2026-09-04): UG52 5.5.1 p190 prohibits the MAK
RTI in LIGHTWEIGHT mode with VR-Forces - every 2026-09-03 run was lightweight. Documented posture =
RTI 5.0.1 in rtiexec mode (config/rid-501-rtiexec-min.mtl, headless rtiexec): WatchVrf reflects 62
entities, real POS (cause by ELIMINATION; --deviceAddress NOT required either side, 3857/3859). The
parseCmdLine startup crash is DETECTED by LaunchVrf52 and its trigger is now BISECTED - see below.
RUNNER 5.2 PROFILE ON THAT POSTURE DONE (5cea2ed; StartRtiExec52 Stage 2r; smoke PREREG_52_PROFILE_SMOKE
green for the INFRASTRUCTURE STAGES ONLY - no C2SIM stage has ever run on 5.2 (COLDSTART_AUDIT R3):
paused Traffic reflects 44; app 3865 joins the config-file way (PREREG_52_APP_SMOKE). STARTUP CRASH
BISECTED 2026-09-04 over 42 launches (PREREG_52_CRASH_BISECT): it is OURS and bound to `--logFileName`
- 11 crashes/30 passing it, 0/12 omitting it, Fisher p=0.013; path length and the perf plugin both
FALSIFIED. **NEVER PASS `--logFileName`** (runner/launcher no longer do); harvest C:\MAK\logs instead,
and keep close-the-corpse + retry ONCE as a guard. This SUPERSEDES the earlier "vendor heap defect on
any rid/method" reading, which was scoped to the whole product and is now scoped to that one option.
SECURITY: vendor logs dump the whole environment in cleartext - never attach one (send .callstack/.dmp). NEXT = a first 5.2 C2SIM run; per COLDSTART_AUDIT R7 a FIXTURE IS NOT REQUIRED
(the app creates units from the init; RESEARCH_52_FIXTURE_FORMAT R2 = -T terrain, no -L, but that loses
the frame lever) - the empty fixture R9_Mojave_Empty_52.scnx is built and validated as the R1 route.
Then PREREG_R9_52: no frame-mode claim until a stamped 5.2 log exists (REBASELINE_52), and NO
prediction from 5.0.2 PATH/TIMING goldens - Y-13 flipped armour Prefer Roads -> IGNORE ROADS,
near-distance 25 -> 15 m (arrival/TASKCMPLT shift, per-soil speed caps gone); Y-12 has planning +
avoidance ON and a blocked vehicle replans ONCE. Assert STRUCTURE only; CLAUDE.md sec 2.
## PROBE PROTOCOL (adopted 2026-09-01/02; do NOT change mid-protocol)
FFRTC mode per the block above; TimeMultiplier 1x; -RunSecs is a CAP under -StopWhenComplete.
SHORT-ROUTE probe variants are allowed (platoon/entity legs ~200 m) but COMPANY probe routes stay
>= ~1 km. THE 5x MULTIPLIER RECORD is superseded and NOT to be re-run
(ANALYSIS_P3_STEP_PROFILE_2026-09-01.md). watchvrf POS mid-move is DEAD-RECKONED - only plateaus
are truth; for per-performer distance use the C2SIM report stream, not the trace.
SCALE-RUN MEASUREMENT: `python tools/analysis/run_census.py . <run>` (per-performer net_km,
per-company sub-route census, object census) carries `--gate rung2|quiet`, which REPRODUCES those
runs' published sec-7 tables EXACTLY; run a gate before trusting it (false-greens rule).

## NON-NEGOTIABLES (unchanged, plus the docs-first rule above)
One variable per probe; prediction + falsifier + DOC CITATIONS before running; movement gate =
static -> moving -> settled with POS/RPT agreement; never kill a joined federate; never kill
rtiAssistant / rtiexec / rtiForwarder without a fresh ruling; fresh ledgered appNo per join;
ASCII in tracked files; after two consecutive infra failures, research before retry. EVERY
threshold NAMES ITS CLOCK **AND BOUNDS ITS DENOMINATOR**, a threshold that measures BEHAVIOUR must
not be filed as an instrument check, an instrument check must be RE-BASELINED when the
configuration changes, and any instrument must reproduce the KNOWN result before it is believed.
