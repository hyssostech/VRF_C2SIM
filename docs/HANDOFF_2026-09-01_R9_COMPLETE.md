# SESSION-JUMP HANDOFF (opened 2026-09-01, current 2026-09-02) - THE AGGREGATE FREEZE IS FIXED

THE CURRENT entry point (newest HANDOFF_*.md by git log). SUPERSEDES
HANDOFF_2026-07-23_TYPEFIX_CONFIRMED.md. ASCII only. HARD CAP 200 LINES - when a phase closes,
collapse it to a few lines plus a pointer; never drop live guidance to make room. RE-VERIFY
load-bearing claims against artifacts, not prose.

## CLOSED - DO NOT REOPEN (tripwires; each line names its record)
- ROUTE FREEZE = OUR ROUTE-BY-NAME ADDRESSING (a name over 35 chars cut in DtUUID's 36-byte
  blob), not region / template / type / waypoint altitude / vertex count / creation order /
  pile density / point count. **FIXED 2026-09-02 (726f762), LIVE-VERIFIED ON R9 AND AT SCALE** -
  pass the route's real uuid: PREREG_ROUTE_UUID_FIX sec 6 (20260902T153837Z), PREREG_COASTP1_RUNG2
  sec 7 (20260902T165144Z, ALL NINE march, names to 99 chars). DO NOT shorten or cap route names.
  Reopening evidence = a cut name, or a nonzero `Can't find entity route`, on this binary.
- THE TIME MULTIPLIER IS NOT THE LEVER - the multiplier and at-distance ladders are WITHDRAWN; the
  SCENARIO CLOCK MODE is the only clock lever, and its direction depends on load (next line).
  PREREG_R9_FIXED_FRAME_RTC_2026-09-02.md sec 8 (2030ebd, c0e90b7).
- The REGION / Mojave-terrain cause is FALSIFIED (docs/CORRECTIONS_LOG.md "The region hypothesis").
- TYPE MAPPING is fixed: ArmorPlatoon -> real Tank Platoon (USA); RealTemplates is the compiled
  DEFAULT (2026-07-22). **THE STATIC BEST-MATCH METHOD IS LIVE-CONFIRMED** on R9 in BOTH modes,
  6/6 units, from the back end's own `Locally Simulated: <name> ... using parameters:
  ...\vrfSim\<Template>.entity` lines (PREREG_TYPEMAP_LIVE_GATE sec 7). Reopening evidence = a
  creation line naming a template the table does not.
- The 2026-07-14 project-generated NavArea (120k tiles) WAS the 2026-07-15..2026-09-01 freeze; it
  is now in SharedData/16/latest/TerrainData/navData/_disabled_20260901/ (restorable). KEEP
  DISABLED unless deliberately regenerating nav data.
- The HQ-section formation-name warning is COSMETIC; the P2 aliases were reverted (P2c).
- ROUTE-VERTEX ALTITUDE FRAME: TerrainProfile (terrain + 10 m) is the compiled DEFAULT, Live
  (+50 m) the fallback, Fixed100 a relic (DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01 sec 7).
- FFRTC IS NOT A SPEED LEVER AT SCALE - a 3.2-3.8x SLOWDOWN on COA-STP1 (0.2652, or 0.3140 with
  `-q`, vs 0.9995 variable-frame and 7.4-13.1 on the 3-unit R9 order). Do NOT budget a scale run
  as if FFRTC compresses.
- Birth altitude, "nav data ruled out", the 10-char marking collision: docs/CORRECTIONS_LOG.md; every
  July FALSIFIED stamp is LAYER-RELATIVE (L9) - re-adjudicate before trusting it.

## THE STANDING RULE (user directive 2026-09-01, enforced)
DOCUMENTATION FIRST for any issue: local help + C:\MAK\vrforces5.0.2\doc\VRFUsersGuide.pdf -> the
PUBLIC Developer's Guide at docs.mak.com/api/vrforces{5.2,5.1.1,4.10}/classref/ -> internet
research. A live probe is registrable ONLY after its prereg cites the documentation consulted; two
months of probing dissolved in one afternoon of reading. VRF_GROUNDWORK_PLAN lessons L8-L10.

## ONE-LINE STATUS
THE AGGREGATE FREEZE IS FIXED AND THE FIX HOLDS AT SCALE. Routes are addressed by their real
VRF_UUID, not by name (726f762, C# only). R9 runs end-to-end headless 3/3 TASKCMPLT; the FULL
COA-STP1 ORDER marches ALL NINE dispatching performers, `Can't find entity route` 14,904 -> 0,
names intact to 99 chars (20260902T165144Z). The MERGED build (type-mapping table) is GATED and
reproduces all of it (20260902T181203Z), and the type-map table is LIVE-CONFIRMED 6/6 on R9
(20260902T193508Z). FFRTC is a 3.2-3.8x SLOWDOWN at scale. OPEN: the three Tank Companies
distribute unreliably run to run (NEXT row 1).

## WORKING CONFIGURATION
TypeMappingMode=RealTemplates + GroundWaypointAltitudeMode=TerrainProfile (both compiled DEFAULTS
since 2026-09-02, bridge A7504441) + NavArea disabled + STOCK templates, no env overrides - an
untouched product at default settings. Vendor defects found across the whole saga: ZERO (the one
candidate, the DtUUID route-name cut, was our own contract violation).
Runner hardening permanent - Stage 2b boot-dialog watcher (AnswerRtiDialog.ps1; the RTI dialog is
ONCE PER REBOOT), Stage 2c RTI gate, per-run bin64 log capture, stop-file trace close, off-by-
default -StopWhenComplete with -SettleHoldSecs 60 as a FLOOR plus rule 4 (every taskee needs an
RPT LATER than its TSK, within 2 m of its latest POS - CONFIRMED LIVE 20260902T181203Z; but see
NEXT row 2), and off-by-default -QuietBackend: RUNNER_TURNAROUND_2026-09-01.md, RUNBOOK 0.5.11.
vrfSim.mtl: notifyLevel 3 / objectConsoleNotifyLevel 3 / enableLogFileTimestamps 1 (backup
.bak-20260901) - KEEP: it is what made the freezes speak.

## FIXED-FRAME RUN-TO-COMPLETE - VALIDATED, BUT **NOT** A SPEED LEVER AT SCALE (records:
PREREG_R9_FIXED_FRAME_RTC_2026-09-02.md sec 8; PREREG_COASTP1_RUNG2 sec 7 for the scale numbers)
THE RULE: FFRTC stays the default for REPEATABILITY and time-managed HLA; TIMEMULTIPLIER STAYS 1x
(ladders WITHDRAWN; the scenario clock mode is the lever - Users Guide 3.4.3 / 7.6.1 / 12.2.1). But
**FFRTC DOES NOT COMPRESS AT SCALE** - measured, frame_gaps.py LS slope, sim-s per wall-s:
  R9 3 units FFRTC 7.43-13.11 | COA-STP1 FFRTC 0.2652 | same +`-q` 0.3140 | COA-STP1 VARIABLE 0.9995
FFRTC advances a FIXED 0.0333 sim-s per frame "even if a frame takes longer than the fixed amount
to compute" (3.4.3), so the ratio lands on WHICHEVER SIDE OF 1.0 the load puts it; variable-frame is
pinned at 1.0x. BUDGET A SCALE RUN AS wall = sim / measured ratio. CORRECTION: "compression only
ever gives MORE margin" IS FALSE at scale - task timers are WALL (600 s predecessor timeout, T13's
12,000 s delay), so at 0.2652 a 600 s wall timeout is 159 SIM s.
THE C2SIM POSITION-REPORT STREAM IS SIM-PACED, NOT WALL-PACED (rung 2, re-confirmed by the -q run).
THRESHOLD RULE: EVERY speed or timeout threshold MUST NAME ITS CLOCK - rung 2's P4(c) miss and the
-q run's P1(b) miss were both this error.
FIXTURE: tools/FixtureGen/frame_variants/TropicTortoise_FFRTC.scnx - stock TropicTortoise with TWO
lines moved, (frame-mode "fixed-frame-run-to-complete") + (frame-time 0.033333); DEPLOYED at
C:\MAK\vrforces5.0.2\userData\scenarios\ (SHA-256 D27E540F8BCC...B0B9). Mode check `python
tools/analysis/frame_gaps.py . <run>`: PASS = Test A >= 95% in {0.033,0.034} AND R >= 0.99; it
DISCRIMINATES - rung 1 (variable-frame) scores R = 0.0276.
OPEN DESIGN ITEM, MEDIUM: OUR APP HAS NO NOTION OF SIM TIME (VrfFacade.cpp:478-482 pins the federate
clock to elapsedRealTime; TickLoop is 20 Hz WALL); at 0.27-0.31x that changes which chains dispatch.

## COA-STP1 SCALE - CLOSED (rungs 0/1/2). Full records in the preregs; this is the residue.
Rung 0 fc93a1e; rung 1 20260902T125423Z (d1f2e10 / 7963aed) - 5 performers built ZERO offset routes
and never moved, SILENTLY; rung 2 20260902T165144Z (b3792d1) - ALL NINE march. ROOT CAUSE (SETTLED,
FIXED 726f762, VERIFIED THREE TIMES): OURS - a contract violation at the DtUUID string ctor
(`rwUUID.h:246-253`; the 36-byte blob at `rwUUID.h:412`, one byte the type tag, so a marking-text
name survives to 35 chars). All FOUR route/waypoint sites pass `e.Uuid`, not `e.Name`. C# ONLY.
STILL OPEN: the NATIVE completion-status item (NEXT row 4) - rung 1 finding A did NOT reproduce,
so the OCCASION is gone but THE NATIVE GAP IS UNTESTED AND STANDS.
OTHER RUNG-1 FINDINGS, unfixed: (B) 26 echelon-'F' units land the GENERIC Ground_Aggregate
fallback under RealTemplates (UnitTranslator.cs:70/:134, TYPE_GAP item 4, USER call) - it MARCHES,
not a freeze cause, and FidelityTable would not do it; (C) TerrainProfile re-entry double-logs the
verb classification; (D) ResetVrf after StopVrf is blind (BackendCount=0).
NOT EXERCISED by any order, so NOT verified live: PatrolRoute and PlanAndMoveTo.
UNEXPLAINED, item 3 being the LIVE one: (1) pole-only objects, 132 (rung 2) / 110 (-q run) of
~1850, not growing - the ever-real population is 1,732 in BOTH; (2) cast-corrupted reflections
28 -> 58; (3) **THE THREE TANK COMPANIES ARE THE UNSTABLE CLASS** - rung 2 T27 1.80 / T35 2.85 km
against seven performers at 6.07-6.64 km; the -q run CLEARED T27 to 6.55 km and STALLED T35 at
0.41 km, ZERO sub-routes. Which company misbehaves is not stable. NEXT row 1.

## OPERATIONAL STATE (2026-09-02, after the TYPE-MAPPING LIVE GATE run)
VR-FORCES DOWN (StopVrf exit 0, graceful, RTI preserved); post-run ResetVrf sweep on 3782 joined
clean, 0 reflected, exit 0. appNo marker **NEXT FREE = 3783** (docs/OPUS_EXECUTION_PLAN.md
Appendix B, runner-managed, CRLF). Live gate 3775-3781 + 3782; -q run 3767-3773 + 3774;
merged-build control 3759-3765 + 3766; rung 2 3750-3756 + 3758.
**3757 IS BURNED** - ResetVrf invoked without the documented launch environment (RUNBOOK
:1208-1215 requires cwd = C:\MAK\vrforces5.0.2\bin64 plus the VR-Forces/VR-Link/makRti bin PATH
prefix AND Machine-scope MAKLMGRD_LICENSE_FILE) and failed before joining. READ :1208-1215
BEFORE RUNNING ResetVrf - all three 2026-09-02 sweeps used it and exited 0.
DEPLOYED APP: src\VrfC2SimApp\bin\Release\net10.0\win-x64\VrfC2SimApp.dll SHA-256
**570619630015...ACEB52A6** (2026-09-02 14:02:48) - the MERGED build (3c5af9a), GATED by run
20260902T181203Z. The runner starts the app from that path (RunC2SimScenario.ps1:382) - building
IS deploying for the APP; only the BRIDGE has a 10-copy deploy step. Deployed bridge = A7504441
(10/10, Ijwhost 38255036; backups bak-20260902-a48abe6c/ and bak-20260902-28e993fe/).
CLIENTID TRAP: the DEPLOYED (gitignored) bin\...\appsettings.json Vrf:ClientId must MATCH the
init's SystemName or the runner ABORTS at validation, exit 2 (RunC2SimScenario.ps1:1154-1165).
R9 inits declare STP; COA-STP1 declares C2SIM. It is at "STP". Edit the DEPLOYED copy only.
RTI RESIDENT + ANSWERED: rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620 - UNCHANGED
across every 2026-09-02 run; still inventory fresh at session start, do not trust PIDs. C2SIM
docker UP (stp-server, c2sim_server4.8.4.9, stp-lt511) **AND SHARED - see NEXT row 3**. Dump
70668 sits in bin64, no newer (RUNBOOK 0.5.12: scripts/AnswerCrashDumpDialog.ps1 answers the
prompt; never halt on it). Firewall: do NOT set NotifyOnListen False (user ruling); Cancel the
testhost prompt. MAK license expires 2026-09-15 - verify the new .lic landed before running after
that. THE VENDOR LOG'S WALL STAMPS ARE LOCAL TIME (-04:00), NOT UTC (ours UTC).

## NEXT (in order)
DONE 2026-09-02 pm, three probes; read the prereg sec 7s, not this summary:
  * MERGED-BUILD CONTROL on R9, GATE PASSED (20260902T181203Z, 3759-3765; 0f75f29 /
    PREREG_MERGED_BUILD_CONTROL). Merged binary behaviourally identical to 3b7b8d2e at default
    config - app logs diff to ZERO HUNKS, endpoints to six decimals. ALSO the live gate's RUN 1.
  * `-q` AT SCALE, STOPPED on its own miss rule (20260902T183135Z, 3767-3773; 4d2f4c3 /
    PREREG_QUIET_BACKEND_SCALE). -q does NOT suppress vrfSim.log (961.9 lines per SIM s vs
    966.2); the console costs ~18% (0.2652 -> 0.3140). `-QuietBackend` DEFAULT OFF; do not adopt.
  * TYPE-MAPPING LIVE GATE, GATE PASSES / run formally INVALID (20260902T193508Z, 3775-3781;
    1976ec1 / PREREG_TYPEMAP_LIVE_GATE). All six R9 units landed EXACTLY the template
    data/unit-type-map.json names, both lookup keys, aggregate and individual; ZERO
    Ground_Aggregate; composition exact; 2 NameObservation proxy reports; 3/3 TASKCMPLT, 0
    dropped tasks. The gate's deliverable list is EMPTY. Rows 2-3 are its follow-ups.

1. **TANK-COMPANY NON-DISTRIBUTION - the live open question.** In the -q run B/5-20 (T35) built
   ZERO sub-routes and STALLED at 0.41 km (rung 2: 4 sub-routes, 2.85 km), missing that run's P4
   and P1(b). One run cannot separate -q-via-timing from NON-DETERMINISM in the two-level
   distribution; evidence favours the latter, because the anomaly MOVED - the same run CLEARED
   T27 (1.80 -> 6.55 km) while degrading T35. SETTLE IT with a COA-STP1 PAIR on this binary, one
   WITHOUT -q and one WITH, prereged together. -q stays unadopted until then.
2. DONE 2026-09-02 (supervisor, a5cdc95): RULE 4 `~PXY` MATCH - RunnerLib Resolve-MarkingKey maps the
   init <Name> to the unique `<name>~<tag>` marking; replayed on the live-gate run (tagged) 3/3 at
   0.00 m and on the control run (untagged) 3/3 unchanged. The app's tasking was never affected.
3. DONE 2026-09-02 (supervisor, a5cdc95): THE SHARED C2SIM SERVER. The foreign init WAS THE USER,
   working the C2SIM GUI against their own server (`c2sim_server4.8.4.9`, 8080/61613). FIX: a
   PRIVATE docker instance `c2sim-server-vrf` (18080/61614, own mount c2simFiles-vrf) is now the
   runner DEFAULT and reaches every stage (ListenReports gained --rest-url/--stomp-url +
   capability 'endpoints'; the app takes C2SIM__RestUrl/StompUrl and logs its endpoints).
   Isolation proven: R9 init pushed to 18080 landed in c2simFiles-vrf only. RUNBOOK sec 1.
   NEVER push to, reset, or restart the operator's 8080/61613 server. STILL OWED by the gate:
   run 3 (PRC must REFUSE TO START) and run 4 (COA-STP1, 128 units). FidelityTable is NOT the
   default; that stays a USER decision.
4. NATIVE COMPLETION STATUS - forward DtTaskCompleteReport success()/taskId()/
   taskTrackingNumber() through VrfFacade::TaskCompleted. The only known remaining cause of a
   FALSE TASKCMPLT. Standing authorization: back up the DLLs, /t:Rebuild always, REDEPLOY ALL 10
   COPIES, verify ONE hash. Stands on the source reading (VrfFacade.cpp:217-242).
5. A COMPLETION-CAPABLE SCALE RUN. No COA-STP1 run has reached a route end: shortest head route
   24.11 km, best ever 26.84 km. Pick the mode from the FFRTC block, budget wall = sim / ratio.
6. TYPE_GAP ITEM 4 - the echelon-'F' -> generic Ground_Aggregate fallback needs a USER RULING
   (docs/TYPE_GAP_ADJUDICATION.md, Decision item 4). Pending; not a movement cause.
7. MAK MESSAGE - docs/MAK_MESSAGE_2026-09-02.md is send-ready and THE USER SENDS IT.
   *** ITS APPENDED DtUUID ROUTE-NAME-LENGTH DRAFT IS STALE AND MUST NOT BE SENT AS A DEFECT
   REPORT: the cause was OUR contract violation, not a vendor bug (rwUUID.h:246-253 documents it;
   fix 726f762). REWRITE OR DROP THAT SECTION. *** Vendor defects across the saga remains ZERO.
8. BACKLOG: type adjudications (54 units - 5.2b checklist first), task vocabulary, completion
   re-keying, scoring (Phase 5).

## VR-FORCES 5.2b UPGRADE CHECKLIST (expected soon - user, 2026-09-01)
(a) diff its EXPANDED AGGREGATE MODEL SET against the 54 pending type adjudications BEFORE
authoring anything (PRIOR_ART Q1), and RE-RUN --typemap-selftest against the 5.2 catalog;
(b) 5.2's "ground path planning enhanced with vector-based terrain data" touches today's
route/clamp machinery AND the NEXT-row-1 distribution question - re-run R9 on 5.2b and
RE-ADJUDICATE before trusting any 5.0.2-era behavioural conclusion; (c) migrate local state
DELIBERATELY: vrfSim.mtl notify levels, the DISABLED NavArea artifact (do NOT carry it into 5.2's
SharedData), the FFRTC fixture, env paths pinned to vrforces5.0.2, a VrfBridge /t:Rebuild +
10-copy redeploy; (d) read docs.mak.com/api/vrforces5.2/classref/vrf_migration5{0,1}.html FIRST.

## PROBE PROTOCOL (adopted 2026-09-01/02; do NOT change mid-protocol)
FFRTC mode per the block above; TimeMultiplier 1x; -RunSecs is a CAP under -StopWhenComplete.
SHORT-ROUTE probe variants are allowed (platoon/entity legs ~200 m) but COMPANY probe routes stay
>= ~1 km. THE 5x MULTIPLIER RECORD is superseded and NOT to be re-run
(ANALYSIS_P3_STEP_PROFILE_2026-09-01.md). watchvrf POS mid-move is DEAD-RECKONED - only plateaus
are truth; for per-performer distance use the C2SIM report stream, not the trace.

## NON-NEGOTIABLES (unchanged, plus the docs-first rule above)
One variable per probe; prediction + falsifier + DOC CITATIONS before running; movement gate =
static -> moving -> settled with POS/RPT agreement; never kill a joined federate; never kill
rtiAssistant / rtiexec / rtiForwarder without a fresh ruling; fresh ledgered appNo per join;
ASCII in tracked files; after two consecutive infra failures, research before retry. EVERY threshold
NAMES ITS CLOCK, and a threshold that measures BEHAVIOUR must not be filed under an instrument check
(the -q run's P1(b)). Prove any instrument reproduces the KNOWN result first (false greens).
