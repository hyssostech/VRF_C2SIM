# SESSION-JUMP HANDOFF (opened 2026-09-01, current 2026-09-02) - THE AGGREGATE FREEZE IS FIXED

THE CURRENT entry point (newest HANDOFF_*.md by git log). SUPERSEDES
HANDOFF_2026-07-23_TYPEFIX_CONFIRMED.md. ASCII only. HARD CAP 200 LINES - when a phase closes,
collapse it to a few lines plus a pointer; never drop live guidance to make room. RE-VERIFY
load-bearing claims against artifacts, not prose.

## CLOSED - DO NOT REOPEN (tripwires; each line names its record)
- ROUTE FREEZE = OUR ROUTE-BY-NAME ADDRESSING (a name over 35 chars cut in DtUUID's 36-byte
  blob), not region / template / type / waypoint altitude / vertex count / creation order /
  pile density / point count. **FIXED 2026-09-02 (726f762), LIVE-VERIFIED ON R9 AND AT SCALE** -
  pass the route's real uuid: PREREG_ROUTE_UUID_FIX sec 6, PREREG_COASTP1_RUNG2 sec 7 (ALL NINE
  march, names to 99 chars). DO NOT shorten or cap route names. Reopening evidence = a cut name,
  or a nonzero `Can't find entity route`, on this binary.
- THE TIME MULTIPLIER IS NOT THE LEVER - the multiplier and at-distance ladders are WITHDRAWN; the
  SCENARIO CLOCK MODE is the only clock lever, and its direction depends on load (next line).
  PREREG_R9_FIXED_FRAME_RTC sec 8 (2030ebd, c0e90b7).
- The REGION / Mojave-terrain cause is FALSIFIED (docs/CORRECTIONS_LOG.md "The region hypothesis").
- TYPE MAPPING is fixed: ArmorPlatoon -> real Tank Platoon (USA); RealTemplates is the compiled
  DEFAULT (2026-07-22). **THE STATIC BEST-MATCH METHOD IS LIVE-CONFIRMED** on R9 in BOTH modes,
  6/6 units, from the back end's own `Locally Simulated: <name> ... using parameters:
  ...\vrfSim\<Template>.entity` lines (PREREG_TYPEMAP_LIVE_GATE sec 7). Reopening evidence = a
  creation line naming a template the table does not.
- The 2026-07-14 project-generated NavArea (120k tiles) WAS the 2026-07-15..2026-09-01 freeze; it
  is in SharedData/16/latest/TerrainData/navData/_disabled_20260901/ (RESTORABLE). KEEP DISABLED
  unless deliberately regenerating nav data.
- The HQ-section formation-name warning is COSMETIC; the P2 aliases were reverted (P2c).
- ROUTE-VERTEX ALTITUDE FRAME: TerrainProfile (terrain + 10 m) is the compiled DEFAULT, Live
  (+50 m) the fallback, Fixed100 a relic (DESIGN_TERRAIN_PROFILE_VERTICES_2026-09-01 sec 7).
- FFRTC IS NOT A SPEED LEVER AT SCALE - a 3.2-3.8x SLOWDOWN on COA-STP1 (0.265-0.314 vs 0.9995
  variable-frame and 7.4-13.1 on the 3-unit R9 order). Do NOT budget as if FFRTC compresses.
- Birth altitude, "nav data ruled out", the 10-char marking collision: docs/CORRECTIONS_LOG.md;
  every July FALSIFIED stamp is LAYER-RELATIVE (L9) - re-adjudicate before trusting it.

## THE STANDING RULE (user directive 2026-09-01, enforced)
DOCUMENTATION FIRST for any issue: local help + C:\MAK\vrforces5.0.2\doc\VRFUsersGuide.pdf -> the
PUBLIC Developer's Guide at docs.mak.com/api/vrforces{5.2,5.1.1,4.10}/classref/ -> internet
research. A probe is registrable ONLY after its prereg cites the documentation consulted; two
months of probing dissolved in one afternoon of reading. VRF_GROUNDWORK_PLAN L8-L10.

## ONE-LINE STATUS
THE AGGREGATE FREEZE IS FIXED AND THE FIX HOLDS AT SCALE. Routes are addressed by their real
VRF_UUID, not by name (726f762, C# only). R9 runs headless 3/3 TASKCMPLT; the FULL COA-STP1 ORDER
marches, `Can't find entity route` 14,904 -> 0 in five consecutive scale runs, names intact to 99
chars. The MERGED build is GATED and the type-map table LIVE-CONFIRMED 6/6 on R9. FFRTC is a
3.2-3.8x SLOWDOWN at scale. OPEN: the three Tank Companies distribute unreliably run to run, and
TWO INSTRUMENT CHECKS NEED A RULING (NEXT row 1).
## WORKING CONFIGURATION
TypeMappingMode=RealTemplates + GroundWaypointAltitudeMode=TerrainProfile (both compiled DEFAULTS
since 2026-09-02, bridge A7504441) + NavArea disabled + STOCK templates, no env overrides - an
untouched product at default settings. Vendor defects across the whole saga: ZERO (the one
candidate, the DtUUID route-name cut, was our own contract violation).
Runner hardening permanent - Stage 2b boot-dialog watcher (AnswerRtiDialog.ps1; the RTI dialog is
ONCE PER REBOOT), Stage 2c RTI gate, per-run bin64 log capture, stop-file trace close, off-by-
default -StopWhenComplete (-SettleHoldSecs 60 is a FLOOR; rule 4 = every taskee needs an RPT LATER
than its TSK within 2 m of its latest POS) and off-by-default -QuietBackend: RUNBOOK 0.5.11.
vrfSim.mtl: notifyLevel 3 / objectConsoleNotifyLevel 3 / enableLogFileTimestamps 1 - KEEP: it is
what made the freezes speak.
## FIXED-FRAME RUN-TO-COMPLETE - VALIDATED, BUT **NOT** A SPEED LEVER AT SCALE
(records: PREREG_R9_FIXED_FRAME_RTC sec 8; PREREG_COASTP1_RUNG2 sec 7 for the scale numbers)
THE RULE: FFRTC stays the default for REPEATABILITY and time-managed HLA; TIMEMULTIPLIER STAYS 1x
(ladders WITHDRAWN; the scenario clock mode is the lever - Users Guide 3.4.3 / 7.6.1 / 12.2.1). But
**FFRTC DOES NOT COMPRESS AT SCALE** - measured, frame_gaps.py LS slope, sim-s per wall-s:
  R9 3 units FFRTC 7.43-13.11 | COA-STP1 FFRTC 0.2652 / 0.2863 / 0.2751 | +`-q` 0.3140 | VARIABLE
  0.9995. FFRTC advances a FIXED 0.0333 sim-s per frame "even if a frame takes longer than the
fixed amount to compute" (3.4.3), so the ratio lands on WHICHEVER SIDE OF 1.0 the load puts it.
BUDGET A SCALE RUN AS wall = sim / measured ratio. CORRECTION: "compression only ever gives MORE
margin" IS FALSE at scale - task timers are WALL, so at 0.2652 a 600 s wall timeout is 159 SIM s.
THE C2SIM POSITION-REPORT STREAM IS SIM-PACED (rung 2, re-confirmed by the -q run and by QPAIR
A-1/A-2: 1,664 reports each). **THE LS SLOPE IS THE NOISIER CLOCK - residual sd ranges 1.66 to
58.66 across runs; where a sim window matters, CROSS-CHECK WITH THE FIT-FREE REPORT COUNT.**
THRESHOLD RULE: EVERY speed or timeout threshold MUST NAME ITS CLOCK **AND BOUND ANY DENOMINATOR
IT DIVIDES BY** - rung 2 P4(c), the -q run's P1(b) and QPAIR I4/I6 were all this error class.
FIXTURE: tools/FixtureGen/frame_variants/TropicTortoise_FFRTC.scnx - stock TropicTortoise with TWO
lines moved, (frame-mode "fixed-frame-run-to-complete") + (frame-time 0.033333); DEPLOYED at
C:\MAK\vrforces5.0.2\userData\scenarios\ (SHA-256 D27E540F8BCC...B0B9). Mode check `python
tools/analysis/frame_gaps.py . <run>`: PASS = Test A >= 95% in {0.033,0.034} AND R >= 0.99; rung 1
(variable-frame) scores R = 0.0276, so it DISCRIMINATES. OPEN DESIGN ITEM, MEDIUM: OUR APP HAS NO
NOTION OF SIM TIME (VrfFacade.cpp:478-482 pins the federate clock to elapsedRealTime; TickLoop is
20 Hz WALL); at 0.27-0.31x that changes which chains dispatch.
## COA-STP1 SCALE - CLOSED (rungs 0/1/2). Full records in the preregs; this is the residue.
Rung 0 fc93a1e; rung 1 20260902T125423Z (d1f2e10 / 7963aed) - 5 performers built ZERO offset routes
and never moved, SILENTLY; rung 2 20260902T165144Z (b3792d1) - ALL NINE march. ROOT CAUSE (SETTLED,
FIXED 726f762, VERIFIED FIVE TIMES incl. QPAIR A-1/A-2): OURS - a contract violation at the DtUUID
string ctor (`rwUUID.h:246-253`; the 36-byte blob at `rwUUID.h:412`, one byte the type tag, so a
marking-text name survives to 35 chars). All FOUR route/waypoint sites pass `e.Uuid`, not `e.Name`.
STILL OPEN: the NATIVE completion-status item (NEXT row 4) - rung 1 finding A did NOT reproduce,
so the OCCASION is gone but THE NATIVE GAP IS UNTESTED AND STANDS. OTHER RUNG-1 FINDINGS, unfixed:
(B) 26 echelon-'F' units land the GENERIC Ground_Aggregate fallback under RealTemplates
(UnitTranslator.cs:70/:134, TYPE_GAP item 4, USER call) - it MARCHES; (C) TerrainProfile re-entry
double-logs the verb classification; (D) ResetVrf after StopVrf is blind (BackendCount=0). NOT
EXERCISED by any order: PatrolRoute and PlanAndMoveTo.
UNEXPLAINED, item 3 being the LIVE one: (1) pole-only objects were 132 (rung 2) / 110 (-q run),
ever-real 1,732 in BOTH - **BUT BOTH QPAIR RUNS HAVE ZERO POLE AND ZERO NaN POS LINES, ever-real
1,847 / 1,864, while `Created radio` stays 1,733 in ALL FOUR.** The observer now resolves
everything; 1,732 was old-configuration residue - DO NOT REUSE IT AS A CHECK (NEXT row 1);
(2) cast-corrupted reflections 28 -> 58; (3) **THE THREE TANK COMPANIES ARE THE UNSTABLE CLASS** -
rung 2 T27 1.80 / T35 2.85 km against six performers at 6.07-6.64 km; the -q run CLEARED T27 to
6.55 and STALLED T35 at 0.41 km with ZERO sub-routes; QPAIR A-1 stalled T35 again (0, 0.37 km) and
A-2, an IDENTICAL invocation, did not (4, 3.69 km). NEXT row 1.
## OPERATIONAL STATE (2026-09-02, after the TWO no-`-q` COA-STP1 runs of the QPAIR probe)
VR-FORCES DOWN (StopVrf exit 0, graceful, RTI preserved); BOTH post-run ResetVrf sweeps (3790,
3798) joined clean, 0 reflected, exit 0, with the :1206-1215 environment. appNo marker
appNo marker: docs/OPUS_EXECUTION_PLAN.md Appendix B (runner-managed; NEXT FREE = 3803 as of
2026-09-03 pm - 3799-3802 went to the two LaunchVrf52 attempts). Per-run number history lives
in that ledger, not here. 3757 IS BURNED (ResetVrf without the RUNBOOK :1208-1215 launch
environment - cwd bin64 + PATH prefix + Machine MAKLMGRD_LICENSE_FILE; read it before ResetVrf).
DEPLOYED APP: src\VrfC2SimApp\bin\Release\net10.0\win-x64\VrfC2SimApp.dll SHA-256
**53130C93BD76...A7EF27A9** (2026-09-02 16:28:08) - a5cdc95, i.e. the GATED merged build plus ONE
added log line (`C2SIM endpoints:`). The runner starts the app from that path
(RunC2SimScenario.ps1:382) - building IS deploying for the APP; only the BRIDGE has a 10-copy
deploy step. Deployed bridge = A7504441 (10/10, Ijwhost 38255036).
CLIENTID TRAP: the DEPLOYED (gitignored) bin\...\appsettings.json Vrf:ClientId must MATCH the
init's SystemName or the runner ABORTS at validation, exit 2 (RunC2SimScenario.ps1:1154-1165).
R9 inits declare STP; COA-STP1 declares C2SIM. It is at "STP" (restored after A-2, per the
convention every COA-STP1 run followed) - SET IT TO C2SIM for the owed run B. DEPLOYED copy only.
RTI RESIDENT + ANSWERED: rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620 - UNCHANGED
across every 2026-09-02 run; still inventory fresh at session start, do not trust PIDs. Docker UP:
`c2sim-server-vrf` (OURS, 18080/61614) plus the operator's c2sim_server4.8.4.9, stp-server,
stp-lt511. Dump 70668 sits in bin64, no newer (RUNBOOK 0.5.12 answers the prompt; never halt on
it). Firewall: do NOT set NotifyOnListen False (user ruling); Cancel the testhost prompt. MAK
license expires 2026-09-15. THE VENDOR LOG'S WALL STAMPS ARE LOCAL (-04:00), NOT UTC (ours UTC).

## NEXT (in order)
DONE 2026-09-02 pm, three probes (READ THE PREREG SEC 7s, NOT THIS SUMMARY): merged-build
control GATE PASSED (3759-3765, zero-hunk log diff); `-q` at scale STOPPED on its own miss
rule (3767-3773; -q does NOT suppress vrfSim.log, console ~18%); type-mapping live gate
PASSES / run formally invalid (3775-3781; 6/6 map names, 3/3 TASKCMPLT).
1. **TANK-COMPANY NON-DISTRIBUTION - PAIR ADJUDICATED (supervisor, PREREG_COASTP1_QPAIR sec 9):**
   `-q` FALSIFIED as the cause (clause (a): A-1, no `-q`, B/5-20 at 0 sub-routes / 0.37 km);
   NON-DETERMINISM SUPPORTED (A-1 vs A-2, identical invocations, B/5-20 0 vs 4 sub-routes);
   run B NOT owed; `-q` stays default-OFF; both instrument misses falsified as misses (I4
   second placeholder encoding now filtered, I6 raw distances match within 4% - prereg sec 9).
   NEXT OBJECT: the SERIAL COMPANY BUILD (one at a time, B/5-20 last or absent in 4/4, two
   companies exactly 9 s apart in 3/4). DOCS FIRST: 5.0.2 Users Guide aggregate disaggregation /
   task processing + 4.10 entitymodels_aggregates.html, cited in the prereg, before any probe.
   Known-unexplained (bounded): why the placeholder encoding flipped between 18:31Z and 20:47Z.
2. DONE 2026-09-02 (supervisor, a5cdc95): RULE 4 `~PXY` MATCH - RunnerLib Resolve-MarkingKey maps
   the init <Name> to the unique `<name>~<tag>` marking; replayed 3/3 at 0.00 m, tagged and not.
3. DONE 2026-09-02 (supervisor, a5cdc95): THE PRIVATE C2SIM SERVER `c2sim-server-vrf`
   (18080/61614) is the runner DEFAULT and reaches every stage; the app logs the endpoints it
   heard (VERIFIED LIVE, QPAIR A-1/A-2). RUNBOOK sec 1. **NEVER push to, reset, or restart the
   operator's 8080/61613 server.** STILL OWED by the type-map gate: run 3 (PRC must REFUSE TO
   START) and run 4 (COA-STP1, 128 units). FidelityTable is NOT the default - a USER decision.
4. NATIVE COMPLETION STATUS - forward DtTaskCompleteReport success()/taskId()/
   taskTrackingNumber() through VrfFacade::TaskCompleted. The only known remaining cause of a
   FALSE TASKCMPLT. Standing authorization: back up the DLLs, /t:Rebuild always, REDEPLOY ALL 10
   COPIES, verify ONE hash (VrfFacade.cpp:217-242).
5. A COMPLETION-CAPABLE SCALE RUN. No COA-STP1 run has reached a route end: shortest head route
   24.11 km, best ever 26.84 km. Pick the mode from the FFRTC block, budget wall = sim / ratio.
6. TYPE_GAP ITEM 4 - the echelon-'F' -> Ground_Aggregate fallback needs a USER RULING
   (docs/TYPE_GAP_ADJUDICATION.md item 4); not a movement cause.
7. MAK MESSAGE - docs/MAK_MESSAGE_2026-09-02.md is send-ready and THE USER SENDS IT. *** ITS
   APPENDED DtUUID ROUTE-NAME-LENGTH DRAFT IS STALE AND MUST NOT BE SENT AS A DEFECT REPORT: the
   cause was OUR contract violation (rwUUID.h:246-253; fix 726f762). REWRITE OR DROP IT. ***
8. BACKLOG: type adjudications (54 units - 5.2b checklist first), task vocabulary, completion
   re-keying, scoring (Phase 5). LOW (user, 2026-09-02): a DIRECT FILE-LOAD PATH for the app -
   init + order read from files, the C2SIM server BYPASSED (not dismantled: the SDK path stays
   the default) so it runs where there is NO server; reports need a file sink. A deployment mode,
   not a test shortcut; the runner keeps the server path.
## VR-FORCES 5.2 MIGRATION - IN PROGRESS (user ruling 2026-09-02: move to 5.2d, docs first)
Phase 0 DONE 2026-09-02 + evidence pass 2026-09-03: docs/VRF_5.2_MIGRATION_DIFF.md (rows A-E
cited, C# re-verify list F, PHASE 1 RECORD sec H, sec G = the CANONICAL decision ledger, ALL
RULED Y-1..Y-16; full ruling texts in docs/VRF_5.2_DECISION_EVIDENCE.md). Cold-start check:
docs/VRF_5.2_COLD_START_MAP.md; STP vocabulary docs/STP_TASK_VOCABULARY_2026-09-03.md. PHASE 1 COMPILE DONE 2026-09-03 (sec H): VrfBridge configs Release (5.0.2) | Release-5.2 |
Release-5.2-HLA4, 0 errors, toolset v145 (v143 GONE from the machine); app
`dotnet build src\VrfC2SimApp -c Release -p:BridgeConfig=Release-5.2` -> bin\Release-5.2\;
8/8 offline self-tests on both stacks; 7-field type fix CONFIRMED as predicted; 5.2 type table =
data/unit-type-map-52.json (AR Scout -> Mechanized Platoon/Company (USA Army M2), PROXY, Y-8).
TRAP: a 5.2 process needs PATH prefixed with vrforces5.2d\bin64;vrlink5.10\bin64;makRti4.6.1\bin
or the bridge dies loading (name-bound MAK DLLs); VrfBridge.NativeStackInfo() logs which stack
bound. FIRST 5.2 LAUNCH+JOIN PROVEN 2026-09-03 pm (PREREG_52_LAUNCH_2026-09-03.md):
scripts/LaunchVrf52.ps1 (independent mode, UG52 4.1.2; LaunchVrf.ps1 is INVALID on 5.2) ->
"Joined federation MAK-ONE-2025", back-end healthy at 36 threads. Blocker found+fixed: the
RTI 5.0.1 installer's ELEVATED 5.0.1 rtiAssistant on 6003 version-rejects EVERY 4.6.1 LRC
(both stacks - 5.0.2 runs are ALSO blocked until reboot or assistant exit); fix = per-process
RTI_ASSISTANT_DISABLE + config/rid-461-ridconfigured.mtl (assistant-free, dialog-free; DIFF
A12/H). Prototype zero NOT automatable (DtGetInputLine reads keyboard only - piped stdin
ignored); 5.b is DEMOTED to symptom discrimination. NOT PROVEN: OUR bridge/tools joining 5.2.
NEXT = tools BridgeConfig (RtiProbe/CreateOne/WatchVrf Release-5.2 + stack-aware identity via
NativeStackInfo: 5.2 = config-file join, Federation/FedFileName/FomModules EMPTY) as the join
gate, then the runner 5.2 profile (TypeMapFile -52, manifest records NativeStackInfo + rid),
then PREREG_R9_52 (Phase 2).
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
