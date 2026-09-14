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

---

## 1. Way A - the one command

From the repository root, in Git Bash:

    scripts/RunScenario.sh --gui \
      --scenario R9_Mojave_Empty_52 \
      --init data/R9_Mojave_Lean_Initialization.xml \
      --order data/R9_Mojave_UnitMove_Order.xml \
      --client-id STP \
      --object-console -1 --member-console -1

`scripts/RunScenario.sh --help` lists every option. What this one command does, in order: starts
rtiexec if it is not already up, launches VR-Forces (with the GUI, because of `--gui`), starts the
interface, pushes the initialization, then pushes the order, then watches the run and finally tears
everything down except rtiexec.

The two console options above turn the per-unit diagnostic chatter OFF. Leave them out only if an
engineer asks for them: they can write a gigabyte of log in a long run.

WATCH IT from a SECOND window with `tail -f <the runner log path the command prints>`. Do NOT pipe
the command, do not add `| tee`, and do not run any process-killing sweep while it is running.

UNVERIFIED: this wrapper has been used live three times (2026-09-14), always WITHOUT the GUI. The
`--gui` path through the wrapper has not been run. Rehearse it once before the demo.

---

## 2. Way B - STP drives (three windows)

1. rtiexec (survives between demos; start it only if it is not already up):
       pwsh -File scripts\StartRtiExec52.ps1
   EXPECT: "RTIEXEC READY ... tcp=...:4001", or "ALREADY UP ... NOTHING was started". Exit code 0.

2. VR-Forces WITH the GUI - this is the audience's window:
       pwsh -File scripts\LaunchVrf52.ps1 -Scenario R9_Mojave_Empty_52 `
            -BackendAppNumber 9201 -FrontendAppNumber 9202
   EXPECT: "READY" from the script, then the GUI showing an empty Mojave map with the simulation
   clock running. Terrain load takes a while the first time (it is streaming from the internet).

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
   one command again.
2. Reloading the scenario in the GUI and restarting only the interface: plausible but UNVERIFIED on
   5.2 (DEMO_READINESS row 9). Rehearse it before relying on it in front of an audience.
3. `tools\ResetVrf`: live-verified in 2026-07 on the OLD 5.0.2 stack only, and it needs that stack's
   environment. UNVERIFIED on 5.2 - do not use it during a demo.

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

2. THE MAK RTI CONNECTION DIALOG APPEARS ON THE FIRST START AFTER A REBOOT.
   Remedy: it is once per reboot - pick the rtiexec connection and continue. Reboot the machine well
   before the demo and do one throw-away launch so the dialog is behind you.

3. THE SIMULATION IS STILL RUNNING AFTER THE RUN "ENDED" (Way A).
   Remedy: the wrapper has a backstop and prints "THE RUNNER DID NOT RECORD A COMPLETED TEARDOWN"
   plus a list of what is still up; it stops the interface and VR-Forces for you. If a process is
   still listed after that, run `pwsh -File scripts\StopVrf52.ps1` yourself. Also: a line reading
   `runner exit: 127` does NOT mean "command not found" - it means something outside killed the run.
   Never run process-killing sweeps while a run is open; that is the shape that caused it.

---

## 9. Smaller traps worth knowing

- clientId vs SystemName must match exactly, or the interface creates nothing and says nothing.
- Two interfaces on one network need different application numbers (demo block 9101-9199).
- Turning the per-unit consoles on is a diagnostic setting, never a demo one (gigabytes of log).
- Do not send a VR-Forces simulation log to anyone: those logs print the machine's entire
  environment, secrets included. Engineers want the .callstack.log / .dmp instead.
- A stale build given a switch it does not know exits with code 2 instead of starting.

## 10. Still unverified at the time of writing (rehearse these)

- The `--gui` run through the one-command wrapper (section 1).
- The hand-started, STP-driven sequence end to end (section 2).
- Reset without restarting VR-Forces (section 6, items 2 and 3).
- Any demo whose ORBAT contains infantry or other dismounts: the simulator crashes on the first one
  until a 6.33 GB vendor character-data package is installed (still owed).
- Any demo after 2026-09-15 without a renewed licence.
