# PREREG P3: TimeMultiplier 5 vs the P2c baseline (registered 2026-09-01, BEFORE running)

Source: docs/HANDOFF_2026-09-01_R9_COMPLETE.md NEXT item 5c - a higher TimeMultiplier
"must enter as its OWN registered variable with one A/B against a settled baseline first
(the 20x warp/DR history)". This is that A/B. Executor brief from the supervisor; user go
for live work stands (2026-09-01). ASCII only.

## 1. The ONE variable

Env `Vrf__TimeMultiplier=5` (app default 1). Baseline = P2c, run 20260901T211310Z
(docs/experiments/PREREG_P1_FIXED100_ENTITY_2026-09-01.md, Outcome RUN 6 / P2c): R9
Mojave order, 3/3 taskees, untouched product at defaults, TimeMultiplier=1.

Everything else identical to P2c, verified from P2c's run-manifest.json before writing this:
- Init  `data\R9_Mojave_Lean_Initialization_NoComments.xml`
- Order `data\R9_Mojave_UnitMove_Order_NoComments.xml`
- Scenario TropicTortoise; federation CWIX-2024; STOCK templates ("Tank Headquarters
  Section (USA).entity" byte-identical to .bak-20260901, checked); NavArea artifact still
  in navData\_disabled_20260901 (checked: "MAK Earth Space (online)" has no NavArea);
  vrfSim.mtl notifyLevel 3 / objectConsoleNotifyLevel 3 / enableLogFileTimestamps 1
  (unchanged since P1); NO other env override (`Get-ChildItem env:Vrf__*` empty before
  the run); binaries unchanged (no rebuild).
- NOT a sim variable: `-RunSecs 420` instead of P2c's 900. The observation window is an
  OBSERVATION PARAMETER (how long the runner watches after the order push before
  StopIface), adopted per handoff NEXT 5a. It does not change what the sim does; at 5x
  every P2c settle (t<220 at 1x) is predicted to land well inside it. WatchVrf duration
  derives as 560 + RunSecs = 980 s (P2c: 1460 s).

## 2. Documentation consulted (the standing rule)

LOCAL API HEADER - C:\MAK\vrforces5.0.2\include\vrfcontrol\vrfRemoteController.h:
- :827 `virtual void setTimeMultiplier(double multiplier) const;` in "IV. Scenario
  management - E / Controlling a Scenario" beside run/pause/step/rewind/exit. NO doc
  comment, no stated range, no stated constraint.
- :917-920 the other overload `setTimeMultiplier(DtScenario*)` "will set the time
  multiplier into the supplied scenario. It will warn if any of the backends do not
  match the first backend's time multiplier" (internal/save path, not our call).
- vrfmsgs\ifSetTimeMultiplier.h:36-42: DtIfSetTimeMultiplier "is sent from the
  front-end to the back-end with the time-multiplier value from the scenario file";
  payload one DtFloat64. So the remote call is the same message the GUI toolbar sends.
- The facade passes an int through a double parameter (VrfFacade.cpp:423-424) - 5.0
  exactly, no precision issue.

LOCAL HELP - C:\MAK\vrforces5.0.2\doc\help\Content\ (read verbatim this session):
- Introduction\Concepts\vrf_runFasterThanRealTime.htm: "VR-Forces supports simulation
  at rates faster than real-time by applying a multiplier to the tick time." Caveats:
  "Do not run your simulation at faster than real-time if you are interacting with
  other real-time simulations." and "Running a simulation faster than real-time can
  cause performance of simulation object models to degrade." Clock modes: the default
  Variable-Frame Run-To-Complete "advances simulation time by the amount of time passed
  since the last time the exercise clock was ticked ... does not provide repeatable
  results."
- Scenarios\CreateRun\vrf_automaticallyChangin.htm: "If you increase the speed at which
  a scenario runs, the frame rate is reduced and performance of models may degrade."
  Toolbar default range 1..15 with buttons "1;2;5;10;15" - FIVE IS A STOCK TOOLBAR
  BUTTON, i.e. a vendor-exposed operating point (20x was outside the exposed range).
  "When you increase the time scale in scenarios that have many entities, particularly
  if they are fast moving entities such as aircraft, simulation quality may degrade."
  The fast-forward auto-tuning file appData\settings\vrfSim\fastForwardSettings.mtl is
  EMPTY on this install (re-checked today: `(fast-forward-settings )`), so at 5x no
  dead-reckoning/frame-mode override is applied - default settings at all speeds.
- Scenarios\Files\vrf_scenarioParams.htm: time-multiplier "Specifies whether a scenario
  runs in real time or faster than real time ... You can set this value dynamically
  using the Time Multiplier toolbar. If you are running a scenario using HLA Time
  Management, it is strongly recommended that you set time-multiplier to 1."
  NOT APPLICABLE HERE: appData\settings\vrfSim\vrfSim.mtl:142
  `(setqb runInTimeManagementMode 0)` - time management is OFF on this back-end.
- Introduction\Concepts\vrf_advancingTimeUsingSimulationTimeOrWallClockT.htm: "When
  VR-Forces uses simulation time for time advancement, simulation objects dead reckon
  correctly in the GUI when you run faster or slower than real time ... However,
  simulation objects published by non-VR-Forces federates will not dead reckon
  correctly." Our units are VR-Forces-published; the affected party is the OBSERVER
  (WatchVrf is a plain VR-Link federate that dead-reckons reflected state on its own
  wall clock) - so raw POS samples during the moving phase may show a sawtooth
  (under-extrapolation between updates). This is the documented mechanism behind the
  July "20x warp" reading (docs/PRIOR_ART_SURVEY.md:283-290, docs/VRF_GROUND_TRUTH.md
  sec 8). It is an observation artifact on the POS channel only; a SETTLED unit has
  zero velocity and dead-reckons to itself, so settle points are unaffected.

PUBLIC CLASS REFERENCE (docs.mak.com, fetched this session):
- https://docs.mak.com/api/vrforces5.2/classref/class_dt_vrf_remote_controller.html -
  both setTimeMultiplier overloads listed, NO documentation text beyond the signatures;
  nothing on constraints or faster-than-real-time.
- https://docs.mak.com/api/vrforces4.10/classref/class_dt_vrf_remote_controller.html -
  same; the DtScenario* overload carries only "This routine will set the time
  multiplier into the supplied scenario."
- Web search (site:docs.mak.com + general) surfaced only the marketing capability page
  ("can be changed ... programmatically through the APIs, even while the simulation is
  running") and nothing on DR error growth, update-rate starvation or HLA update rates
  at high multipliers - consistent with docs/PRIOR_ART_SURVEY.md Q4.

CONCLUSION OF THE DOCS PASS: nothing documented constrains 5x on this configuration
(time management off; 5 is a stock toolbar value; no fast-forward profile active). The
documented risks are (a) model-quality degradation at high multiples with many/fast
entities - 6 slow ground units is the benign end; (b) observer-side DR artifacts on
reflected POS during motion - expected and recorded, not gated. The probe is warranted
because the July history is at 20x and the record has never A/B'd a multiplier against
a settled 1x baseline.

## 3. Plumbing (verified before the run)

`Vrf__TimeMultiplier` -> Host.CreateApplicationBuilder env provider (`__` -> `:`) ->
`config.GetSection("Vrf").Get<VrfSettings>()` (VrfC2SimService.cs:138) ->
VrfSettings.TimeMultiplier (int, default 1; VrfSettings.cs:116) -> on late-join, after
`_bridge.Run()` is enqueued, `if (TimeMultiplier > 1) Enqueue(SetTimeMultiplier)` and the
log line `Sim Run() queued (start the VR-Forces clock; timeMult={Mult}).`
(VrfC2SimService.cs:214-218) -> VrfBridge.cpp:208 -> VrfFacade.cpp:423
`controller->setTimeMultiplier(multiple)`.
Runner: scripts/RunC2SimScenario.ps1 launches VrfC2SimApp via Start-Process with no
environment override (Start-External, :625-645) - the child inherits the runner's
process environment, the same mechanism the runner itself uses for
Vrf__ApplicationNumber (:1658) and P1 used for Vrf__GroundWaypointAltitudeMode. The
runner only saves/restores Vrf__ApplicationNumber; it does not scrub other Vrf__* vars.

BINDING GATE (VOID if missed): the app log must contain exactly
`Sim Run() queued (start the VR-Forces clock; timeMult=5).` (P2c's reads timeMult=1).
HONEST LIMIT of that line: it is logged at ENQUEUE time, so it proves the setting BOUND
and the call was queued - not that the back-end applied it. The back-end-applied
evidence is the clock itself: prediction A below. If the line says 5 but the moving
phases run at 1x durations, the run is VALID and prediction A is MISSED (the remote
setTimeMultiplier did not take) - a finding, not a void.

## 4. Command, appNos, environment

- Command (cwd repo root, pwsh):
  `$env:Vrf__TimeMultiplier='5'; pwsh -NoProfile -File scripts\RunC2SimScenario.ps1
   -Init data\R9_Mojave_Lean_Initialization_NoComments.xml
   -Order data\R9_Mojave_UnitMove_Order_NoComments.xml -RunSecs 420`
  Console captured to the session scratchpad.
- appNos: the runner allocates 7 from the Appendix B marker (3648 at registration) =
  3648-3654, advances the marker to 3655, appends the CLAIMED block itself. createOneDiag
  burns unused on a healthy run.
- Environment at registration (inventoried 2026-09-01 ~18:00 local): VR-Forces DOWN (no
  vrfSim/vrfGui/vrfLauncher); RTI RESIDENT + ANSWERED (rtiAssistant 41336 / rtiexec
  224608 / rtiForwarder 76620 - identical to the handoff; not to be touched); docker
  stp-server + c2sim_server4.8.4.9 Up; repo on main at c24248f, working tree clean of
  tracked changes.

## 5. Predictions + falsifiers (written before the run)

Trace clock = WatchVrf t (2 s samples; t0 ~32 s before the order push, as in P2c).
P2c baseline moving phases (onset = first sample >25 m from birth; settle = first
sample of the terminal bit-identical plateau), computed from P2c's trace this session:
  114.MechCoy  onset 35.5 -> settle 217.3 : 181.8 s  end 34.653915,-116.693388
  1222.MechPlt onset 31.4 -> settle 160.1 : 128.7 s  end 34.612956,-116.587784
  1.BdeHQ      onset 29.4 -> settle 147.8 : 118.4 s  end 34.608416,-116.699993

A. CLOCK RATE (the point of the probe; confidence HIGH given the binding gate):
   each taskee's moving phase (onset -> settle) shortens by ~5x. Company ~36 s,
   platoon ~26 s, entity ~24 s wall. Accept: ratio P2c/P3 in [3.5, 7] per taskee (the
   2 s sampling and the unscaled first-tick overheads widen the band). Company settled
   by trace t <= ~110 (onset ~35 + 36 + slack). MISS: any ratio < 2 (clock not sped
   up - the multiplier did not take) or > 10 (something other than the clock changed).
B. ENDPOINTS (confidence HIGH): each settle point within 25 m of its P2c endpoint
   (the leading-edge completion geometry is speed-independent; one 0.5-s sim tick of
   overshoot at ground speeds is <10 m). MISS: any taskee >25 m off, or no plateau.
C. COMPLETIONS (confidence HIGH): 3/3 TASKCMPLT in the app log and 3 TSK lines in the
   trace; POS==RPT at settle (RPT last fix identical to the POS plateau) for all three.
   MISS: <3 completions with the window met, or POS/RPT disagreement at settle.
D. VENDOR-LOG CLEANLINESS (confidence HIGH): bin64-vrfSim.log has ZERO "Waiting for
   nav data", ZERO "empty route", ZERO "Can't find entity route"; the cosmetic
   "invalid formation name" fires ~1x as in P2c (not gated, recorded).
E. REPORT PATH (confidence HIGH): ZERO SocketException / "Only one usage of each
   socket address" / "Connection error" lines in vrfc2simapp.log (P2c: 0; the P4a
   20x history in docs/PLAN_DERISK_NOTES.md sec 1 came from thousands of pushes;
   this run pushes hundreds at most). Recorded, not gated: position-report count vs
   P2c's 192 - if report cadence is sim-time driven expect roughly 2-3x more per
   wall second during motion; if wall-driven, fewer in total (shorter window).
F. OBSERVER DR ARTIFACT (recorded, NOT gated; confidence MEDIUM): during the moving
   phase the raw POS series may be non-monotonic along-track (sawtooth) from the
   observer's own DR at wall-clock rate; RPT (back-end-generated) should not. Any
   such artifact vanishes at settle. If a unit's POS plateau is NOT bit-identical while
   RPT is, quote both.

FALSIFIERS - what makes 5x NOT a free variable for probe runs:
- A met but B or C missed (units arrive somewhere else, fail to complete, or POS/RPT
  disagree at settle) -> 5x changes OUTCOMES, not just wall time; probe runs stay at
  1x and COA-STP1 scale needs a different plan.
- D missed (nav/route errors appear only at 5x) -> the multiplier exposes a
  route-building/paging race; STOP and report (the terrain page-in family, VRF_GROUND_TRUTH
  sec 6/8).
- E missed -> report-path throughput is the limiter; P4a/P4b become prerequisites.
- Binding gate missed (timeMult=1 in the log) -> VOID; fix the env delivery, do not
  interpret.
- A missed with the gate met -> remote setTimeMultiplier is not honored on this path;
  a finding for the record, 5x cannot be used via this knob until understood.
A missed high-confidence prediction is a STOP: report it, no re-run with a different
setting.

## 6. Run-validity gates (must pass or the run is INVALID, not a verdict)

As P2c plus the binding gate: 6 units dispatched; RealTemplates type-mapping line;
SIX "Create-altitude mode=Live" lines (Live default; no altitude env override);
PushOrder EXIT=0 + three CreateRoute/MoveAlongRoute pairs; oracle gate (stage 7) on real
coordinates; trace written; ListenReports capturing (if RPT empty degrade to POS-only
and SAY SO); the timeMult=5 line (sec 3). Infra failures (RTI gate, launch, teardown)
are VOID, not verdicts; after two consecutive infra failures, stop and research.

## 7. Reading rules

Standard movement gate (static-while-paused -> moving -> settled; POS/RPT agreement;
MOVED = >=25 m net sustained >=3 samples with distance-to-final decreasing). Raw POS
distances during motion are DR-poisoned - never gate on a mid-move distance (and at 5x
expect more of it, prediction F). Durations are read onset->settle on the trace clock,
the same way the baseline numbers above were computed.

## 8. Budget + teardown

One run, ~15-20 min wall (420 s observation + launch/teardown). Standard runner finally
(StopIface -> app self-exit -> observers finish -> StopVrf); RTI left RESIDENT. Never
kill rtiAssistant/rtiexec/rtiForwarder. If StopVrf leaves vrfGui (known intermittent),
a second StopVrf pass as on P2b - nothing killed.

## Outcome

(to be filled AFTER the run, against the predictions above)
