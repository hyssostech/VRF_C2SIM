# TYPEFIX CONFIRMING RUN - 2026-07-22 (ABORTED: back-end crash mid-run)

RAW OBSERVATIONS ONLY. No verdicts. The supervisor adjudicates per
PREREG_TYPEFIX_CONFIRMING_RUN.md. ASCII only.

## 0. Identity

- Run id / dir      : runs/20260722T231614Z_run (artifacts copied into this directory)
- Command           : pwsh -File scripts/RunC2SimScenario.ps1
                      -Init data/R9_Mojave_Lean_Initialization_NoComments.xml
                      -Order data/R9_Mojave_UnitMove_Order_NoComments.xml -RunSecs 900
- Repo HEAD         : 0b4529f (branch main) - R9 type-mapping fix
- App exe           : src/VrfC2SimApp/bin/Release/net10.0/win-x64/VrfC2SimApp.exe
                      built 2026-07-22 19:07:09 local; --translator-selftest re-run
                      by the executor BEFORE the run = PASS (all 5 R9 assertions green).
- appNos (Appendix B, from marker 3585): 3585 backend, 3586 frontend, 3587 oracle-pre,
  3588 trace, 3589 app, 3590 createOne-diag. Marker advanced 3585 -> 3591 by the runner.

## 1. HEADLINE (raw)

The VR-Forces BACK-END (vrfSimHLA1516e) CRASHED with a fatal error mid-run. Crash dump
written 2026-07-22 19:18:32 local. The crash occurred AFTER the interface created the 3
routes and BEFORE any MoveAlongRoute task was issued. NO movement window ever opened for
any taskee. The observation window never produced a single movement sample.

## 2. TIMELINE (this machine stamps logs UTC; local = UTC-4)

| local     | UTC        | event (source) |
|-----------|------------|----------------|
| 19:16:14  | 23:16:14   | runner launched; marker 3585->3591; run dir created |
| 19:16:24  | 23:16:24   | LaunchVrf READY: back-end HEALTHY (21 threads), front-end main window up (launchvrf.stdout.log) |
| 19:17:45  | 23:17:45   | observers started: WatchVrf-trace (3588), ListenReports |
| 19:18:05-07 | 23:18:05-07 | PushInit: server RUNNING; QUERYINIT 6 Units SystemName=STP (pushinit.stdout.log) |
| 19:18:11  | 23:18:11.105 | app connected to RTI Assistant (vrfc2simapp.log RTI INFO) |
| ~19:18:1x | ~23:18:1x  | app: "Type-mapping mode = RealTemplates (ArmorPlatoon -> Tank Platoon (USA) (11.1.225.3.2.0.0))" |
| ~19:18:1x | ~23:18:1x  | app: 6 GROUND units created (Create-altitude mode=Live, 10000 m safe MSL, clamp); "Init dispatched: 6 units"; "Sim Run() queued (timeMult=1)" |
| 19:18:19.886 | 23:18:19.886 | PushOrder: ORDER (3302 chars) pushed, result OK (pushorder.stdout.log, c2sim-bus.log) |
| ~19:18:20 | ~23:18:20  | app: 3x "CreateRoute (3 pts) for <taskee>; move deferred to route-created". vrfc2simapp.log LAST WRITE 19:18:20. NO MoveAlongRoute line follows. |
| 19:18:32  | 23:18:32   | **vrfSim FATAL-ERROR DUMP WRITTEN** (crash) |
| ~19:19:09 | ~23:19:09  | trace readable count collapses 46 -> 0 at trace t=80.8s (RTI declares crashed federate lost; reflected stays frozen at 47) |
| 19:29-19:33 | 23:29-23:33 | executor abort: runner stopped, StopVrf run, evidence captured |

Note the ~5.89 s trace-t0 offset convention (prereg sec 3.4) is not load-bearing here:
no task-onset alignment is possible because no MoveAlongRoute was ever issued.

## 3. VALIDITY GATES (prereg sec 2) - status BEFORE the crash

- Type-mapping active (RealTemplates, not GoldenParity): **PASSED** - vrfc2simapp.log line:
  "Type-mapping mode = RealTemplates (ArmorPlatoon -> Tank Platoon (USA) (11.1.225.3.2.0.0))".
  The run is NOT a GoldenParity void.
- 6 units created: **PASSED** - "Init dispatched: 6 units + 0 areas"; PushInit QUERYINIT
  "6 Units"; trace reflected 3->47 readable 2->46 at t=25.5s.
- Creation on REAL coordinates (post-init oracle gate, stage 7 FATAL): **PASSED** - the run
  reached PushOrder (stage 8), which only executes after stage 7 passes. Trace shows real
  clamped coordinates (see sec 4).
- Order delivery / PushOrder EXIT=0: **PASSED** - "push result: OK"; c2sim-bus.log holds the
  3-task order; app logged "C2SIM Order received (3302 bytes)".
- Three MoveAlongRoute task lines in the app log: **NOT MET** - the app logged the 3
  CreateRoute lines with "move deferred to route-created" and then STOPPED (back-end crashed
  before the route-created callbacks fired). No MoveAlongRoute was issued for any taskee.
- ListenReports (RPT channel) capturing: **DEGRADED** - listenreports.stdout.log shows only
  "listening for reports, 1460s ..."; NO reports-captured.log file was produced (0 reports).
  This is expected given no move ever executed, but the RPT channel is empty regardless.

## 4. PER-TASKEE RAW OBSERVATIONS

The order (c2sim-bus.log) carries 3 MOVE tasks; PerformingEntity UUIDs match the 3 taskees:
- T_R5_PL1 -> 001aa71b-...6342 = 1222.MechPlt
- T_R5_CO1 -> 139aa71b-...6242 = 114.MechCoy
- T_R5_TK1 -> 670cfdb2-...ef24 = 1.BdeHQ

For ALL three: created at correct coords (below), route created ("CreateRoute (3 pts) ...;
move deferred to route-created"), and then NO MoveAlongRoute issued (back-end crash). No
onset, no movement sample, on EITHER channel, for any taskee. Trace positions are STATIC
across the readable window (frames 46 and 48 bit-identical per object).

Creation coordinates (watchvrf-trace.csv, frame 46, trace t~44s; match init to 6 dp):

| taskee | trace uuid (aggregate/entity) | lat | lon | alt (m, ground clamp) |
|--------|-------------------------------|-----|-----|-----------------------|
| 1222.MechPlt | VRF_UUID:b5827613-69e9-... | 34.612956 | -116.600487 | 1040.6 |
| 114.MechCoy  | VRF_UUID:2c8aa465-4596-... (+661fcd73, 9266929b, c67d723a co-located) | 34.647629 | -116.693388 | 1116.7 |
| 1.BdeHQ      | VRF_UUID:db9efb1e-2b2d-... | 34.608416 | -116.712685 | 1131.4 |

(Aggregate<->taskee linkage is geometric/inferred from co-location + exact init-coord match,
as in the 2026-07-19 run; the interface does not log child uuids. 114.MechCoy is the ~38-object
company cluster around 34.6476,-116.6934; 1222.MechPlt is a 5-object cluster; 1.BdeHQ a lone
entity - same cluster sizes as 2026-07-19.)

POS channel: static creation coords above, then frozen-stale after the crash (readable->0 at
t=80.8s; reflected frozen at 47).
RPT channel: empty (no PositionReports; no move executed).
No POS/RPT disagreement to quote - both channels are silent on movement because no move ran.

## 5. CRASH EVIDENCE

- Dump file : C:\MAK\vrforces5.0.2\bin64\vrfSim5.0.2-MSVC++15.0_64-249613-36676.dmp.dmp
- Size      : 829687 bytes
- LastWrite : 2026-07-22 19:18:32 local (the fatal-error moment)
- Preserved IN PLACE (not moved, not deleted), per supervisor order.
- vrfc2simapp.stderr.log is EMPTY: the crash was in the back-end process, not the .NET app.
- Last app action before the crash (discriminating evidence): the three
  "CreateRoute (3 pts) for <taskee>; move deferred to route-created" calls (~19:18:20),
  12 s before the dump. The route-created callbacks never fired.

Two benign deserialize warnings appear in vrfc2simapp.log ("Failed to deserialize xml to
type C2SIM.Schema102.MessageBodyType: There is an error in XML document (1,2)") at QUERYINIT
and at Order-received. These are the KNOWN RUNBOOK 0.6 server-re-broadcast/prolog artifacts;
the interface still processed init (6 units) and order (3 tasks) from the raw text. Not a new
error and not the crash cause on the evidence here.

## 6. COMPETING HYPOTHESES FOR THE CRASH (no verdict - for supervisor)

- H1 (route-create / deferred-move on the fixed correct-type templates): the crash is 12 s
  after 3x CreateRoute-with-deferred-move on the NEW RealTemplates aggregates (Tank Platoon
  (USA) etc.). "move deferred to route-created" is a code path NOT present in the 2026-07-19
  app log (which logged "CreateRoute -> MoveAlongRoute" directly). Novel combination.
- H2 (environmental / incidental back-end instability): a fresh back-end (launched HEALTHY at
  19:16:24) on the PRESERVED RTI stack; VR-Forces back-end crashes have been seen before.
- H3 (Sim Run() clock start): the clock was started (timeMult=1) shortly before the order.
Falsifier for H1 that is NOT yet checked: whether a run with GoldenParity type (old enum) or
with the direct MoveAlongRoute path survives CreateRoute on this same preserved stack. Cell C
(fresh boot, bespoke tool, direct CreateRoute+MoveAlongRoute) did NOT crash - but it used a
FRESH RTI boot and a different tool, so it does not isolate H1 vs H2.

## 7. END-STATE INVENTORY (at report time, ~19:33 local)

- vrfSimHLA1516e : DEAD (crashed).
- vrfGui pid 22512 : UP (StopVrf exit 3 could not gracefully close its real main window; NOT
  force-killed). REMNANT.
- VrfC2SimApp, WatchVrf, ListenReports : GONE (terminated when the runner process tree was
  stopped during the abort).
- rtiAssistant 8888 / rtiexec 68020 / rtiForwarder 68464 : UP, UNTOUCHED.
- C2SIM server (docker) : was RUNNING at crash; not reset (next PushInit self-resets).

## 8. DEVIATIONS / FLAGS

1. RUNNER-SCRIPT FIX (pre-run, mechanical, NOT committed): scripts/RunC2SimScenario.ps1
   line 1447 had a backtick line-continuation followed by an inline comment, which orphaned
   the following "-Note '...'" argument and threw a terminating parse error at Stage 3
   (LaunchVrf) in BOTH DryRun and live. Introduced 2026-07-21 by commit 8c36abe ("Round-11
   fixes"), AFTER the last successful 2026-07-19 run, so the script had never run live in
   this state. Fixed by extracting the timeout to a variable (comment moved to its own line)
   and restoring the -Note continuation. Behavior change: -Note (a manifest annotation string)
   is now passed as intended; timeout still computes to 720 s. DryRun then ran clean end to end.
   No gate/threshold/read-rule/type-mapping variable touched.
2. TASKSTOP TERMINATED THE JOINED APP FEDERATE (3589) WITHOUT A CLEAN RESIGN. To comply with
   the supervisor's "stop advancing immediately", the runner process tree was stopped, which
   also killed VrfC2SimApp (a background child) before a StopIface resign could run. The
   back-end was already dead at that point. Possible stale federate 3589 lingering in rtiexec
   until RTI timeout. appNos are never reused, so no direct collision, but flagged for the
   next launch. (The supervisor's later "StopIface first" order arrived after the tree was
   already stopped.)
3. Brief time estimate discrepancy (non-blocking): the brief expected the app-exe timestamp
   "AFTER ~19:4x"; actual build was 19:07:09 and the run launched 19:16 - it is only ~19:33
   now, so "~19:4x" was an over-estimate. The selftest re-run confirmed the fix is in the
   on-disk exe regardless.
4. vrfGui remnant: StopVrf answered the "Session Status" dialog (No) but the GUI kept a real
   "VR-Forces" main window and was NOT force-closed (RUNBOOK sec 0). Left running; supervisor
   to decide disposition.

## 9. appNo LEDGER (Appendix B annotated)

- 3585 backend  : CONSUMED (launched HEALTHY, then crashed)
- 3586 frontend : CONSUMED (vrfGui, remnant)
- 3587 oracle-pre: CONSUMED (pre-check ran)
- 3588 trace    : CONSUMED (trace ran; stopped with the tree during abort)
- 3589 app      : CONSUMED (joined; killed with the tree, no clean resign)
- 3590 createOne: BURNED (stage 7b never ran - oracle gate passed - never joined)
Total: 5 consumed, 1 burned. Marker at 3591. (Budget was ~6; <= 2 burned OK.)
