# DEMO RUNBOOK - C2SIM to VR-Forces 5.2d, for the demo operator

Revised 2026-09-14 (DEMO_READINESS row 13). Written for the person RUNNING the demo, not for the
engineers. Every step says what to type and what you should SEE. Anything this file could not
confirm from the record is marked UNVERIFIED - treat those as "rehearse it before the audience is
in the room", never as "it will be fine".

Two ways to run the demo:
  A. ONE COMMAND - the harness wrapper brings up everything AND plays STP itself (it pushes the
     initialization and the order). Use this when there is no live STP in the room.
  B. STP DRIVES - you start three things by hand and STP pushes the initialization and the order.
     Use this when the STP operator is part of the demo.

---

## 0. Before the demo

ONCE PER MACHINE
- VR-Forces 5.2d, VR-Link 5.10 and MAK RTI 5.0.1 installed under C:\MAK; .NET 10 runtime present.
- The interface built and deployed once. Smoke-test the deployed folder before you trust it:
      set DOTNET_ENVIRONMENT=Demo
      VrfC2SimApp.exe --runtime-check
  EXPECT: "MakRuntime: PATH prefixed with ...", "VrfBridge loaded; native stack = 5.2 | C:\MAK\
  vrforces5.2d\bin64\vrfcontrol.dll", "runtime-check: OK", exit code 0. It starts nothing and joins
  nothing. A wrong install path fails loudly here instead of silently at the first move order.
  (Verified 2026-09-07, including the negative control.)
- INTERNET is required: the terrain is "MAK Earth (online)". Without it VR-Forces stalls on terrain
  load. There is no offline fallback terrain configured today (UNVERIFIED - never tested).

ONCE PER DAY / BEFORE EACH DEMO
- STEP ZERO - START THE PERSISTENT FEDERATION HOLDER. rtiexec's federation CREATE can be
  refused by its FOM Reader (STP-825) - measured on the live rtiexec at roughly 42% of
  creates, and refusals CLUSTER (several in a row), so Stage 2h's four automatic attempts per
  launch are not demo-grade on their own. Once per demo day, before the first launch, run
  `pwsh -File scratchpad\validation\p7_holder_retry.ps1 -SettleSecs 28800` - it retries a
  block of ledgered application numbers until one JOINS and then holds the federation open
  for the rest of the day. Every launch after that only JOINS, never creates. A live
  `RtiProbe.exe` afterwards is EXPECTED - never kill it.
- Nothing of ours may still be running: `Get-Process vrfSim*,vrfGui*,VrfC2SimApp` must come back
  empty. rtiexec / rtiForwarder / rtiAssistant MAY stay up between sessions and must NEVER be
  killed - they are shared infrastructure, not part of your run.
- The C2SIM server the demo uses is the PRIVATE one: REST http://127.0.0.1:18080/C2SIMServer,
  STOMP http://127.0.0.1:61614/topic/C2SIM (docker container c2sim-server-vrf). The operator's own
  server on 8080 / 61613 is a DIFFERENT server - never push to it, reset it or restart it.
- LICENCE: the node-locked DEMO licence LAPSES 2026-09-15. After that date nothing in the toolchain
  starts until it is renewed (MAK Sales). Check before you promise a demo.

WHICH SCENARIO AND WHICH DATA
- Scenario (the VR-Forces fixture): `R9_Mojave_Empty_52`, already deployed under
  C:\MAK\vrforces5.2d\userData\scenarios.
- SHORT DEMO (recommended - a few minutes): init `data\R9_Mojave_Lean_Initialization.xml`, order
  `data\R9_Mojave_UnitMove_Order.xml`, clientId `STP`. Six units are created, three are tasked,
  legs are ~0.6 km and all three complete.
- LONG DEMO (tens of minutes): init `data\COA-STP1_Initialization.xml`, order
  `data\COA-STP1_Order.xml`, clientId `C2SIM`. Eleven units are tasked on 24-33 km legs. BE WARNED
  (section 5): several of them stop part-way and never arrive - this is a known open defect, not
  something you can fix in the room.
- The fixture runs FASTER than real time (roughly 1.5-2.8x at the eleven-unit size). A real-time
  fixture was built (`R9_Mojave_Empty_52_RT`) but is NOT deployed - UNVERIFIED, do not plan on it.
- Fixtures whose name ends in `_AG` are the same scenarios carrying the navigation fix of
  section 0.5, and are deployed alongside the ones above. WHICH fixture the demo names is an
  engineering decision - confirm it before the demo rather than substituting one yourself.

---

## 0.5 Navigation - what lets the units plan long routes

Three things outside the interface decide whether a tasked unit can plan a multi-kilometre
route. All three are already set up on this machine; this section says what they are so that
a failure is recognisable and so a rebuilt machine can be put back.

THE CUSTOM SIMULATION MODEL SET (the `_AG` fixtures)
- VR-Forces plans off-road movement on a navigation mesh. The vendor's own movement script
  asks the mesh NOT to use abstract graphs, and with that setting the mesh REFUSES any leg
  beyond roughly 2 km on a large sectorised navigation area - measured 2026-09-14: legs up
  to 1,994 m planned 4/4, a 4,914 m leg refused 4/4. The long COA demo legs are 24-33 km.
- The fix is a CUSTOM simulation model set at `C:\C2SIM\vrf-sms\C2SIM_EntityLevel_AbstractGraphs.sms`.
  It includes the shipped `EntityLevel.sms` unchanged and replaces exactly one script
  (`ground-vehicle-move-to.lua`) with a copy that turns abstract graphs ON. With it the same
  legs planned 8/8. It lives OUTSIDE `C:\MAK` so a VR-Forces reinstall does not delete it -
  but the fixture names it by absolute path, so that folder MUST exist on the demo machine.
- WHICH FIXTURES CARRY IT: the ones whose name ends in `_AG` -
  `R9_Mojave_Empty_52_AG` (sibling of the section-0 scenario, shipped terrain) and
  `R9_Mojave_Empty_52_NavAO_AG` (the COA navigation-area terrain). Both are deployed under
  `C:\MAK\vrforces5.2d\userData\scenarios`. The non-`_AG` fixtures of the same names are
  the old ones on the vendor script and are left in place unchanged.
- UNVERIFIED as a DEMO: the `_AG` fixtures are proven for path planning (8/8), not yet
  rehearsed as a full demo run. Ask an engineer which fixture to name before the demo.

THE RELOCATED appData (navigation data loaded WITH the terrain)
- `C:\C2SIM\vrf-appdata\appData` is a copy of the vendor `appData` tree whose ONE change is
  `loadAllNavigationDataOnTerrainLoad 1`: navigation data is loaded when the scenario loads
  instead of lazily when the first entity is placed.
- VALIDATED 2026-09-14 (G7B_G8_RESULTS sec 3, 3.1) as a NULL RESULT, not a fix. The relocated
  tree IS read by the sim (it rewrites 19 settings files into it at every launch, proving the
  settings root was adopted), but the setting itself changes NOTHING OBSERVABLE: runs with it
  ON and OFF show the identical ~2.3 GB placement-triggered navigation-data stream, and runs
  with it ON still fail the first 7-8 goals' current-point gate exactly like runs without it.
  Keep it - free, documented, reversible - but do not rely on it to close the early-goal-
  failure window below; that needs the ready signal instead.
- How to pass it: `scripts/RunScenario.sh --vrf-appdata-dir C:\C2SIM\vrf-appdata\appData`,
  or `RunC2SimScenario.ps1 -VrfAppDataDir ...`, or `LaunchVrf52.ps1 -AppDataDir ...`. Leave
  it out and VR-Forces uses its own `appData` exactly as before.

THE CACHE WARM-UP AND THE READY SIGNAL (the trap on a first run after a reboot)
- On the lazy path the navigation area starts streaming when the FIRST ENTITY IS PLACED (init
  shells and platforms, not the tasked members) and every ground-vehicle-move-to falls to the
  feature planner, silently, until the stream finishes. Measured 2026-09-14: the unusable
  window from first placement to first usable area is 9.1-12.1 seconds WARM and 236.9 seconds
  COLD (a freshly evicted filesystem cache) - the same ~2.3 GB stream either way; only the
  read time differs.
- The cheap, real READY SIGNAL is the first `New Primary nav area` row printed by any placed
  platform's object console (level 3). The runner gates the order push on it
  (`--pre-order-gate nav-area`, first live use 2026-09-14 19:07Z, fired at +118 s cold / +18 s warm).
  CAVEAT (N2b, 2026-09-14 21:05Z): the row is NECESSARY, NOT SUFFICIENT - members materialised at the
  order and queried 5-10 s after that row passed both nav-area gates and still got "not enough (0)
  points" for every goal (straight fallback, the ridge freeze). Whether a settle after the row fixes it
  is N2c (PREREG_N1_N2_CORRIDOR_SLOPE sec 5.5); until it decides, add a settle after the gate or
  delay the first task (StartTime) by a few minutes on a first run.
- Until that gate exists, a first run after a boot needs EITHER a fixed pre-order settle
  (`scripts/RunScenario.sh --pre-order-settle 240`, which holds after the units exist and
  before the order is pushed) sized to the WORST case - at least 30 seconds warm, 240+
  seconds COLD - OR one throw-away warm-up run beforehand (loading the scenario once drops
  the read to the warm figure). A rehearsal run the same day is the simplest warm-up and is
  worth doing anyway.
- `CreationPolicy=AtInit` also helps: it places the tasked members before the wait instead of
  at order time, so a wait taken before the order actually covers them.

---

## 1. Way A - the one command

ONE-TIME SETUP (STP-844, before the first GUI-on demo run): `pwsh -NoProfile -File
scripts\NewVrfAppData52.ps1 -Dest C:\C2SIM\vrf-appdata-unattended` (about 700 files / 33 MB;
the terrain cache is junctioned, not copied; safe to re-run).

From the repository root, in Git Bash:

    scripts/RunScenario.sh --gui \
      --scenario R9_Mojave_Empty_52 \
      --init data/R9_Mojave_Lean_Initialization.xml \
      --order data/R9_Mojave_UnitMove_Order.xml \
      --client-id STP \
      --object-console -1 --member-console -1 \
      --vrf-appdata-dir C:\C2SIM\vrf-appdata-unattended\appData

`scripts/RunScenario.sh --help` lists every option. What this one command does, in order: starts
rtiexec if it is not already up, launches VR-Forces (with the GUI, because of `--gui`), starts the
interface, pushes the initialization, then pushes the order, then watches the run and finally tears
everything down except rtiexec.

The two console options above turn the per-unit diagnostic chatter OFF. Leave them out only if an
engineer asks for them: they can write a gigabyte of log in a long run.

`--vrf-appdata-dir` (STP-844) points the GUI and the sim at the run-owned appData seeded above,
whose two teardown prompts are pre-disabled. REQUIRED for a GUI run - proven live in D1b and D2
(teardown clean in under 10 s both times, nothing left running; see below).

WATCH IT from a SECOND window with `tail -f <the runner log path the command prints>`. Do NOT pipe
the command, do not add `| tee`, and do not run any process-killing sweep while it is running.

RUN 2026-09-20 (D1, GUI ON, no `--vrf-appdata-dir`): the one-command wrapper reached READY
unattended, pushed the init and the order, and all three tasks completed in real time (ratio
1.00, 4 min 44 s order-to-last-completion) - start, init, order and completion all worked.
TEARDOWN DID NOT: the GUI was left open on its own documented exit prompt (UG52 sec 4.6/4.6.1)
and StopVrf52.ps1 exited 3 (still running - nothing was killed).
2026-09-21 CORRECTION: the "real time (ratio 1.00 ...)" reading above is WITHDRAWN - D7's
harvest measured this run-to-complete frame mode at ~3.00x the wall clock, not 1.00x; see
docs/experiments/PREREG_DEMO_REHEARSAL_2026-09-20.md D7 RESULT N7.

FIXED, CONFIRMED LIVE (D1b, D2 - STP-844): with `--vrf-appdata-dir` given, teardown is clean in
under 10 s both times (9.87 s, 9.77 s), CloseMainWindow returns TRUE, and nothing is left
running. On a run WITHOUT `--vrf-appdata-dir`, the fallback still applies: click the GUI's quit
prompt by hand at the end, and expect the leftover vrfGui to block the next launch until you
close it (`pwsh -File scripts\StopVrf52.ps1` or the GUI itself).

---

## 2. Way B - STP drives (three windows)

1. rtiexec (survives between demos; start it only if it is not already up):
       pwsh -File scripts\StartRtiExec52.ps1
   EXPECT: "RTIEXEC READY ... tcp=...:4001", or "ALREADY UP ... NOTHING was started". Exit code 0.

1b. THE FEDERATION HOLDER (STP-825, automatic - nothing to type). rtiexec 5.0.1 rejects a
    federation CREATOR's FOM-module distribution intermittently, so without a holder
    VR-Forces itself would be the creator and could die at startup on a rejected create.
    Step 2 below starts a small holder (`RtiProbe.exe`) first and waits for it to join, so
    VR-Forces only ever JOINS. EXPECT a live `RtiProbe.exe` process after the demo - it is
    the holder, not a leftover, and resigns on its own after 900 seconds; do not stop it.

2. VR-Forces WITH the GUI - this is the audience's window:
       pwsh -File scripts\LaunchVrf52.ps1 -Scenario R9_Mojave_Empty_52 `
            -BackendAppNumber 9201 -FrontendAppNumber 9202 `
            -AppDataDir C:\C2SIM\vrf-appdata-unattended\appData
   EXPECT: a "Federation HOLDER (STP-825)" section (step 1b) first, then "READY" from the
   script, then the GUI showing an empty Mojave map with the simulation clock running.
   Terrain load takes a while the first time (it is streaming from the internet).
   `-AppDataDir` (STP-844) is the same run-owned, pre-seeded tree as Way A's
   `--vrf-appdata-dir`. REQUIRED for a GUI run - proven live in D1b and D2 (section 1).

3. The interface:
       pwsh -File scripts\StartInterface52.ps1 -ClientId STP
   `-ClientId` MUST equal the SystemName inside the initialization STP is going to push. If they
   differ the interface creates NOTHING and looks healthy while doing it.

Then STP pushes the initialization, then the order (section 4).

UNVERIFIED as a single sequence: each script is in daily use, but the full hand-started, STP-driven
run has not been done end to end (DEMO_READINESS row 7).

---

## 3. What READY looks like

In the interface's own console, this single line is the "we are live" signal:

    READY - joined the federation, N VR-Forces back-end(s), type mapping = FidelityTable,
    compose = on, position reports every 10s. Waiting for the C2SIM initialization (clientId=STP)

Read it as: N should be 1 (the sim you just launched); the clientId must match the initialization
you are about to push. If the line never appears, the interface did not join - stop and fix that
before pushing anything.

In Way A the wrapper prints its own stage lines and a periodic status line instead; the same READY
line is in the interface log it names.

---

## 4. Pushing the initialization and the order

IN WAY A you push nothing - the command does it.

IN WAY B the STP operator pushes, in this order: the Initialization first, then the Order(s). Never
push a second initialization into a running interface: duplicates are ignored by design, so it looks
like nothing happened - that is a guard, not a reset (section 6).

If you need to stand in for STP by hand (rehearsal, or STP is not in the room), the endpoints are
NOT optional - the tools default to the OTHER server:

    tools\PushInit\bin\Release\net10.0\PushInit.exe  data\R9_Mojave_Lean_Initialization.xml ^
        http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
    tools\PushOrder\bin\Release\net10.0\PushOrder.exe data\R9_Mojave_UnitMove_Order.xml 60 ^
        http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM

Never hand-edit an initialization or order in the room. A BLANK LINE inside an XML comment silently
kills the message channel (it happened on 2026-09-14); if a file must change, an engineer changes it
and it is pushed once in rehearsal first.

---

## 5. What the audience sees

IN THE VR-FORCES GUI
- Seconds after the initialization: unit icons appear across an assembly area, deliberately SPREAD
  onto 700 m rings (units that share one coordinate in the C2SIM data would otherwise be stacked on
  top of each other and block each other). A brigade's worth of units spans a few kilometres.
- Units the order does not mention stay as empty markers; a unit the order TASKS gets its vehicles
  at that moment - a company visibly fills in with its platoons.
- Then the tasked units drive their routes in formation. On the short R9 demo this takes a couple of
  minutes. On the long COA order, legs are 24-33 km and take tens of minutes even at the sped-up
  clock.

IN STP
- POSITION REPORTS every 10 seconds for every unit the interface tracks (verified at scale:
  69,314 reports in 45 minutes for 127 units).
- TASK STATUS: today the only code STP ever receives is TASKCMPLT, when a unit arrives. TASKSTRT (at
  dispatch) and TASKABRT (for a task that is refused or a unit that stalls) are BUILT but NOT yet
  merged or deployed - do not promise them for a demo run today.

*** WHAT STP RENDERS TODAY - DO NOT NARRATE MORE THAN THIS *** (added 2026-09-14; verified from
STP's own source, docs/experiments/STP_PARSE_CHECK_2026-09-14.md secs 1 and 3, and MAJOR-2 of
docs/experiments/REVIEW_INTEGRATION_02b51de_2026-09-14.md). STP's C2SimBridge has exactly ONE report
parse site, and `ParseReportContent` handles ONLY `PositionReportContentType`: an
`ObservationReportContentType` or a `TaskStatusType` is deserialized correctly and then DISCARDED,
with no log line. So POSITION is the only channel STP consumes today - task starts, task aborts,
route pre-flight warnings and type-substitution announcements are all no-ops AT STP as it is pinned
(HyssosTech.Sdk.C2SIM 1.3.1), whatever the interface sends, until STP-800. HeadingAngle and Speed
are a third case: they now ride on every position report and STP's parser accepts them without
complaint (gate G-B: PARSED on 1.3.0 / 1.3.1 / 1.4.0, both values delivered), but STP does not yet
READ them - `// TODO: Add HeadingAngle, Speed` sits in its own builder. The evidence for all of
these is therefore the BUS CAPTURE, not STP's display: do NOT tell an audience "STP now sees task
starts, aborts, heading and speed" - say that the interface now REPORTS them, and show the capture
if asked.

SAY THIS OUT LOUD IF YOU RUN THE LONG ORDER: some units stop part-way and never arrive. The cause is
under investigation (steep sustained ridge faces on the straight-line legs, plus at least one second
mechanism); the simulator itself does not notice a vehicle that has stopped, which is exactly why a
progress watchdog is being added. It is a known open item, not a failure of the demo.

---

## 6. Reset between runs

What the record actually supports on 5.2, in order of preference:

1. FULL CYCLE (this is the verified one). Stop the interface, stop VR-Forces gracefully, launch
   again, start the interface again, push the initialization again. rtiexec stays up and untouched.
   Two such cycles ran back to back on 2026-09-14 with no trouble. In Way A this is simply: run the
   one command again. REHEARSED WITH THE GUI 2026-09-20 (D1b then D2, back to back): functional
   reset VERIFIED - one command, no wedge, no blind observers, a fresh holder appNo each time.
   Operator timing (n=1 each): previous runner exit -> next READY 80.1 s; READY -> order on the
   bus ~114 s; order -> all three tasks terminal 290 s on a rested machine, 387 s on the
   back-to-back second cycle; teardown ~10 s both times. HONEST REASON for the spread: the second
   cycle's entities moved at a constant ~0.60x ground speed (sim clock still held real time) for a
   cause that is NOT YET DIAGNOSED (candidate: no settle time between cycles, confounded here by a
   concurrent I/O-heavy harvest process; discriminating run D3 is registered but not yet run).
   Guidance until D3 decides it: leave a few minutes between demo runs, or rehearse the demo on the
   SECOND cycle's timings rather than the first's.
2. Reloading the scenario in the GUI and restarting only the interface: plausible but UNVERIFIED on
   5.2 (DEMO_READINESS row 9). Rehearse it before relying on it in front of an audience.
3. `tools\ResetVrf`: BUILDS for 5.2 since 2026-09-15 (it was 5.0.2-only before; RUNBOOK sec 9), and
   it now joins with the 5.2 connection-config identity instead of the old CWIX-2024 constants. It
   is still UNVERIFIED against a LIVE 5.2 federation - the live join gate is owed - so do not use it
   during a demo. When it is verified, the invocation is, from `C:\MAK\vrforces5.2d\bin64` with the
   5.2 PATH / RTI_RID_FILE / RTI_ASSISTANT_DISABLE environment and a FRESH ledgered appNo:
   `tools\ResetVrf\bin\Release-5.2\net10.0\win-x64\ResetVrf.exe <freshAppNo> --dry-run` to see what
   is present, then the same command without `--dry-run` to delete it. `--help` prints the plan and
   the bound stack without joining anything. `tools\SetSimRate` (same tree,
   `SetSimRate.exe <multiplier> <freshAppNo>`) is 5.2-capable on the same terms - also unverified
   live - and is NOT part of a reset; it only changes the clock rate.

In all cases the interface must be restarted between runs: its map of units is built at
initialization time, and pushing a second initialization into a live interface does nothing.

---

## 7. Clean stop

- Way A: the command tears its own run down and prints what, if anything, is still up.
- Way B: Ctrl+C the interface first (it resigns from the federation cleanly), then close VR-Forces
  (File > Exit, or `pwsh -File scripts\StopVrf52.ps1`). LEAVE rtiexec / rtiForwarder RUNNING.
- Then confirm: `Get-Process vrfSim*,vrfGui*,VrfC2SimApp` comes back empty. If it does not, see
  section 8, item 3.

---

## 8. The three things that commonly go wrong

1. A LEFTOVER VR-FORCES BACK END FROM AN EARLIER RUN BLOCKS THE LAUNCH.
   Remedy: close it (`pwsh -File scripts\StopVrf52.ps1`) and launch again - never "force it through"
   with an allow-existing switch, which reports READY against somebody else's simulation.
   NOTE: a live `RtiProbe.exe` is NOT this problem - it is the STP-825 federation holder
   (step 1b), expected to still be running after a demo, and is never VR-Forces itself.

2. THE MAK RTI CONNECTION DIALOG APPEARS ON THE FIRST START AFTER A REBOOT.
   Remedy: it is once per reboot - pick the rtiexec connection and continue. Reboot the machine well
   before the demo and do one throw-away launch so the dialog is behind you.

3. THE SIMULATION IS STILL RUNNING AFTER THE RUN "ENDED" (Way A).
   Remedy: the wrapper has a backstop and prints "THE RUNNER DID NOT RECORD A COMPLETED TEARDOWN"
   plus a list of what is still up; it stops the interface and VR-Forces for you. If a process is
   still listed after that, run `pwsh -File scripts\StopVrf52.ps1` yourself. Also: a line reading
   `runner exit: 127` does NOT mean "command not found" - it means something outside killed the run.
   Never run process-killing sweeps while a run is open; that is the shape that caused it.

4. STP-825: "the federation HOLDER could not join ... after 4 attempt(s)".
   Remedy: this is the rtiexec FOM-module-distribution refusal, and refusals cluster (several
   in a row) - four ledgered attempts is not always enough. Start a PERSISTENT holder by hand
   (`pwsh -File scratchpad\validation\p7_holder_retry.ps1 -SettleSecs 28800`, section 0's step
   zero) and re-launch once it reports JOINED; every launch after that only joins the held
   federation. Never restart rtiexec / rtiForwarder yourself - that is the user's call, not
   the operator's.

---

## 9. Smaller traps worth knowing

- clientId vs SystemName must match exactly, or the interface creates nothing and says nothing.
- Two interfaces on one network need different application numbers (demo block 9101-9199).
- Turning the per-unit consoles on is a diagnostic setting, never a demo one (gigabytes of log).
- Do not send a VR-Forces simulation log to anyone: those logs print the machine's entire
  environment, secrets included. Engineers want the .callstack.log / .dmp instead.
- A stale build given a switch it does not know exits with code 2 instead of starting.

## 10. Still unverified at the time of writing (rehearse these)

- The hand-started, STP-driven sequence end to end (section 2).
- Reset without restarting VR-Forces (section 6, items 2 and 3).
- Dismounts were created and moved in D1 on 2026-09-20 with the character-data package installed
  (no crash); known issue STP-845 (some dismounts abandon an obstructed destination).
- Any demo after 2026-10-31 without a renewed licence (renewed 2026-09-14; lapses 2026-10-31).
