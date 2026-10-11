# DEMO RUNBOOK - Iron Storm on VR-Forces 5.2d (aggregate profile), for the demo operator

Rewritten 2026-10-05 for the demo as it now stands. Written for the person RUNNING the demo, not for the
engineers: every step says what to type, what you should SEE, and what to do if you do not. Anything not
confirmed live is marked UNVERIFIED - read that as "rehearse it before the audience is in the room".
The entity-level Mojave / COA-STP1 runbook this replaces is history (Appendix A).

THE DEMO: STP's Iron Storm order (its raw export, unchanged) runs on VR-Forces' AGGREGATE model set, bounded
to the Iron Storm centre area (Vrf:DemoExtent, RL-20261005-02). It is shown as WORK IN PROGRESS
(RL-20261004-04): section 5 is what the presenter says about the parts that do not run yet.

Two ways to run it:
  WAY B (PRIMARY, RL-20261004-05) - STP DRIVES. You start VR-Forces and the interface; the STP operator
        pushes the initialization and the order. Section 2.
  WAY A (FALLBACK) - ONE COMMAND. The runner starts everything and pushes the order itself, standing in
        for STP. Use it if STP or Way B fails in the room. Section 3.

WHAT HAS ACTUALLY RUN (read this before promising anything):
| Path | Status |
|---|---|
| Way A, raw export, bounded, GUI on | PASSED 2026-10-05 (docs/experiments/REHEARSAL_WAYB_BOUNDED_2026-10-05.md) |
| Way A, derived cut-A order | PASSED headless 2026-10-04 (G1-6); with the GUI: UNVERIFIED |
| Way B hand path (2.1-2.4, 4) on Iron Storm, PushInit/PushOrder standing in for STP | PASSED twice: 2026-10-05 at DurationScale 0.25 (docs/experiments/REHEARSAL_WAYB_HAND_2026-10-05.md) and 2026-10-11 on the profile as shipped, 1.0 (REHEARSAL_WAYB_HAND2_2026-10-11.md); and once more 2026-10-11 as now written, 0.25 from the profile (REHEARSAL_WAYB_HAND3_2026-10-11.md) |
| The Demo profile as the Iron Storm aggregate profile (2.2) | RAN LIVE 2026-10-05 and 2026-10-11 (the second with nothing set by hand) |
| STP itself pushing (not a stand-in) | NEVER RUN - UNVERIFIED |
| Clean labels ("48 IBCT/28ID", RL-20261005-03), the name line hidden (RL-20261011-01) | SEEN ON SCREEN 2026-10-11: one clean label per unit (REHEARSAL_WAYB_HAND3_2026-10-11) |
| STP-850 start anchor (Receipt, RL-20260928-05) | deployed and run live 2026-10-11; STP's export has minOffset 0, so nothing is rebased |

All commands run from the repository root of the MAIN checkout, in PowerShell 7 unless Git Bash is named.

---

## 0. Once per machine

0.1 THE MAK STACK. VR-Forces 5.2d, VR-Link 5.10 and MAK RTI 5.0.1 under C:\MAK; .NET 10 runtime.
- LICENCE: the node-locked licence was RENEWED on 2026-09-14 and now LAPSES 2026-10-31. Every entry script
  prints the file and its expiry as it starts ("licence file: ... (expires 31-oct-2026)") - read that line;
  after the date nothing MAK starts until it is renewed (MAK Sales).
- The fixture `IronStorm_Centre_52_Aggregate.scnx` must be in C:\MAK\vrforces5.2d\userData\scenarios
  (present 2026-10-05).

0.2 THE DEPLOYED INTERFACE. Check the build, then smoke-test it (it starts nothing and joins nothing):
      (Get-Item src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\VrfC2SimApp.exe).VersionInfo.ProductVersion
  SEE: `1.0.0+git.473f20a.Release-5.2` (2026-10-11), or the version of the latest deploy line in RUNBOOK
  sec 9 - never one ending in DIRTY.
      $env:DOTNET_ENVIRONMENT = 'Demo'
      & src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\VrfC2SimApp.exe --runtime-check
      Remove-Item Env:DOTNET_ENVIRONMENT
  SEE: "VrfBridge loaded; native stack = 5.2 | C:\MAK\vrforces5.2d\bin64\vrfcontrol.dll", "runtime-check: OK",
  exit 0 (last confirmed by RUNBOOK sec 9 "RL-20261011-01 DEPLOY"). Anything else: stop, call an engineer.

0.3 THE UNATTENDED appData - *** ONE TREE, AND IT IS `C:\C2SIM\vrf-appdata-unattended\appData`. ***
  It is the only tree whose TWO GUI TEARDOWN PROMPTS ARE PRE-DISABLED (STP-844), so the GUI closes without
  a human; every GUI command below names it. A machine rebuild deletes it (RUNBOOK sec 0.5, STP-844 block,
  AMENDMENT 2026-10-05). If `C:\C2SIM\vrf-appdata-unattended\appData` is missing, re-create it - from
  PowerShell, path in single quotes (a bash run with an unescaped backslash once seeded a stray tree):
      pwsh -NoProfile -File scripts\NewVrfAppData52.ps1 -Dest 'C:\C2SIM\vrf-appdata-unattended' -DryRun
      pwsh -NoProfile -File scripts\NewVrfAppData52.ps1 -Dest 'C:\C2SIM\vrf-appdata-unattended'
  SEE: exit 0 (0 = tree present, both prompts off). Then do 0.4 for it. Never use the older
  C:\C2SIM\vrf-appdata tree for a demo: it lacks the teardown fix.

0.4 LABELS ON THE MAP (RL-20261004-05, RL-20261011-01). The map shows each unit's clean LABEL and NOT its
  VR-Forces NAME (the 30-character cut C2SIM name): the Label decoration ON, the Name decoration OFF. That is
  a GUI setting that lives in EACH appData, so it is deployed into the vendor install AND the unattended
  tree. With NO vrfGui running:
      pwsh -NoProfile -File tools\display\Enable-LabelDecoration.ps1 -Verify
      pwsh -NoProfile -File tools\display\Enable-LabelDecoration.ps1 -AppDataDir 'C:\C2SIM\vrf-appdata-unattended\appData' -Verify
  SEE: exit 0, "ON for 5/Ground, 5/Default, Name hidden". Exit 1 (OFF - "Label off" and/or "Name shown")
  or 2 (file missing): run the same line with
  -WhatIf, then with neither switch, then -Verify again (RUNBOOK sec 9 "LABEL DECORATION DEPLOY"). Exit 4 =
  a vrfGui is running - close it first. Without it the map shows the 30-character names (with the "__"
  text) and/or no clean labels, and LaunchVrf52 prints "[WARN] LABEL DECORATION NOT ON" at every launch.

0.5 FIREWALL (the stable demo paths only). Windows prompts the first time each program path listens.
  The owner runs this once, in an ELEVATED PowerShell (safe to re-run; one group, easy to remove). The
  group `C2SIM-VRF` was observed present on 2026-10-04:
      $repo = 'F:\Repos\C2SIM\OpenC2SIM.github.io\Software\Interfaces\VRF_C2SIM'
      $progs = @('C:\MAK\makRti5.0.1\bin\rtiexec.exe', 'C:\MAK\makRti5.0.1\bin\rtiForwarder.exe',
        'C:\MAK\makRti5.0.1\bin\gui\rtiAssistant.exe', 'C:\MAK\vrforces5.2d\bin64\vrfSimHLA1516e.exe',
        'C:\MAK\vrforces5.2d\bin64\vrfGui.exe', 'C:\MAK\vrforces5.2d\bin64\vrfLauncher.exe',
        "$repo\src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\VrfC2SimApp.exe",
        "$repo\tools\RtiProbe\bin\Release-5.2\net10.0\win-x64\RtiProbe.exe",
        "$repo\tools\WatchVrf\bin\Release-5.2\net10.0\win-x64\WatchVrf.exe")
      foreach ($p in $progs) {
        $name = 'C2SIM-VRF ' + [IO.Path]::GetFileNameWithoutExtension($p)
        if (-not (Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue)) {
          New-NetFirewallRule -DisplayName $name -Group 'C2SIM-VRF' -Direction Inbound -Action Allow `
            -Program $p -Profile Domain,Private,Public -RemoteAddress LocalSubnet | Out-Null } }
      Get-NetFirewallRule -Group 'C2SIM-VRF' | Select-Object DisplayName, Enabled, Profile
      # undo: Remove-NetFirewallRule -Group 'C2SIM-VRF'
  A prompt for any OTHER path (a worktree, a testhost): Cancel it - loopback is not filtered, nothing breaks.
  Do NOT run `Set-NetFirewallProfile -NotifyOnListen False` - machine-wide, refused 2026-09-02 (RUNBOOK
  sec 0.5, "THE testhost FIREWALL PROMPT").

0.6 THE C2SIM SERVERS. Two, NOT interchangeable:
    STANDARD  REST http://127.0.0.1:8080/C2SIMServer   STOMP http://127.0.0.1:61613/topic/C2SIM
              the operator's own server; Way B may use it (RL-20260920-01). Never reset it: not ours.
              On 2026-10-05 ~13:00Z it did NOT answer on this machine - check it before using it.
    PRIVATE   REST http://127.0.0.1:18080/C2SIMServer  STOMP http://127.0.0.1:61614/topic/C2SIM
              docker container c2sim-server-vrf; Way A always uses it; Way B may.
  Start the private one if needed and check it:
      docker start c2sim-server-vrf
      curl.exe -s -o NUL -w "%{http_code}" http://127.0.0.1:18080/C2SIMServer
  SEE: 200. (`docker start`, not `docker run`: the container exists and only needs starting.)

0.7 TERRAIN. The fixture's terrain is MAK Earth (online) over the Suwalki area. VR-Forces keeps its own tile
  cache (C:\MAK\vrforces5.2d\appData\cache, shared by the unattended tree through a junction); G1-5's Iron
  Storm run of 2026-10-04 left it unchanged (21,807 files) and the route pre-flight's own deployed cache
  covers the demo box (0 downloads in that run). So the demo AREA is cached - but every recorded run had the
  internet up: a run WITHOUT internet is UNVERIFIED. Keep the internet; never move the demo to an area this
  machine has not run (a cold area loads slowly and has created objects at a fallback altitude - the
  pre-warm procedure is in the history, Appendix A).

---

## 1. Before each demo

1.1 QUIET MACHINE. No builds, no test suites, no runner, no other session launching anything until the demo
  is over - agree it with the other sessions first (RL-20261005-01). Then:
      Get-Process vrfSim*, vrfGui*, VrfC2SimApp, WatchVrf -ErrorAction SilentlyContinue
  SEE: nothing. A leftover blocks the next launch: close it (section 4) - never force it through with an
  allow-existing switch. A live `RtiProbe` is a federation holder: EXPECTED, never kill it. rtiexec /
  rtiForwarder / rtiAssistant are shared infrastructure: never kill or restart them.

1.2 rtiexec (survives between demos; this starts it only if it is not up):
      pwsh -NoProfile -File scripts\StartRtiExec52.ps1
  SEE: "RTIEXEC READY ..." or "ALREADY UP ... NOTHING was started", exit 0. Exit 3: stop, call an engineer.

1.3 THE FEDERATION HOLDER, once per demo day (STP-825: rtiexec refuses some federation CREATES; with a
  holder, every later launch only JOINS). Take four application numbers from the ledger - the ONE marker
  `*** NEXT FREE: <n> ***` in docs\OPUS_EXECUTION_PLAN.md Appendix B; advance it and record the claim; never
  reuse a number. Then, with the four claimed numbers in place of a,b,c,d:
      pwsh -NoProfile -File scripts\StartFederationHolder52.ps1 -AppNumbers a,b,c,d -SettleSecs 28800 -WhatIf
      pwsh -NoProfile -File scripts\StartFederationHolder52.ps1 -AppNumbers a,b,c,d -SettleSecs 28800
  SEE: "HOLDER JOINED: pid ... appNo ...", exit 0. Numbers tried and not joined are burned. Exit 1 (none
  joined): stop - do not keep launching; ask an engineer. Exit 2: a precondition (rtiexec down, a sim or an
  RtiProbe already running). LEAVE THIS WINDOW OPEN all day: the holder shares its console, so the script does
  not return. A number or two failing first ("create FAILED ... STP-825, this number is now burned") is normal:
  on 2026-10-11 the third of four joined.

1.4 LABELS: the two `-Verify` lines of 0.4, exit 0 both.
1.5 SERVER: REST 200 on whichever server the demo uses (0.6), and the server UNINITIALIZED before 2.2 starts
  the interface. Read its state (nothing is sent), with the pair the demo uses - private shown:
      tools\StopIface\bin\Release\net10.0\StopIface.exe http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM --dry-run
  SEE: "before      : UNINITIALIZED". Anything else (after `docker start` the private server came back RUNNING
  on 2026-10-11): on the PRIVATE server run the same line with --yes instead of --dry-run - SEE "after RESET :
  UNINITIALIZED", exit 0. On the STANDARD server that is STP's session: ask the STP operator to reset it.
  Why: the interface stops itself when the server goes UNINITIALIZED, and pushing the initialization onto a
  RUNNING server passes through that state while the interface listens - untested (REHEARSAL_WAYB_HAND2 K1).
1.6 After a reboot, do one throw-away launch first: the MAK RTI connection dialog can appear on the first
  start after a reboot (pick the rtiexec connection). A same-day rehearsal is the simplest way.

---

## 2. WAY B - STP drives (primary)

Rehearsed twice on Iron Storm with PushInit/PushOrder standing in for STP (table at the top); STP itself
pushing is UNVERIFIED. Rehearse it once, end to end, before the audience.

APPLICATION NUMBERS (RL-20261011-01): every federate takes a FRESH number from the ledger - the ONE marker
`*** NEXT FREE: <n> ***` in docs\OPUS_EXECUTION_PLAN.md Appendix B; advance it and record the claim BEFORE
launching; never reuse a number (a reused number can hang on a stale federate after a forced teardown,
RUNBOOK sec 0). Each launch of 2.1 + 2.2 needs FIVE: h and h+1 (LaunchVrf52's own short-lived holder and its
retry), b (back end), f (front end), i (the interface). Numbers not used are burned.

2.1 VR-FORCES WITH THE GUI - the audience's window:
      pwsh -NoProfile -File scripts\LaunchVrf52.ps1 -Scenario IronStorm_Centre_52_Aggregate -FederationHoldAppNumber h -BackendAppNumber b -FrontendAppNumber f -AppDataDir 'C:\C2SIM\vrf-appdata-unattended\appData'
  SEE, in order: its own short-lived holder (a "Federation HOLDER (STP-825)" section; with 1.3's holder up
  it simply JOINS); "vrfGui teardown prompts are OFF in ..."; NO "[WARN] LABEL DECORATION NOT ON" line;
  "back-end started (pid N)" - WRITE N DOWN (section 4); "scenario LOAD CONFIRMED"; "READY: 5.2d back-end
  HEALTHY", exit 0. The GUI shows the Iron Storm area, no units yet.
  If not: exit 4 BLOCKED = a dialog is waiting on screen - answer it, never kill; exit 3 = not ready or
  crashed at start-up - read its lines, run section 4, call an engineer; exit 2 = a precondition, most
  often a leftover process (1.1).
  (h, b, f: the claimed numbers - APPLICATION NUMBERS above. The fixed 9101-9199 demo block is retired.)

2.2 THE INTERFACE, ON THE DEMO PROFILE - SET NOTHING BY HAND. appsettings.Demo.json IS THE IRON STORM AGGREGATE
  DEMO PROFILE (since 2026-10-05, branch feat/demo-overlay-aggregate): StartInterface52 loads it, and it carries
  the aggregate model set (above BN is aggregate-only, RL-20260927-06 - on EntityLevel the D2b guard REFUSES this
  order at receipt), the aggregate type map, the Iron Storm fixture (Vrf:Scenario), the catalogue composition,
  stall detection on the SIMULATION clock (RL-20260925-01 Q3), the 600 s successor floor and the demo area
  (RL-20261005-02) - the values the passing runs used (G1-6, the bounded rehearsal). Use a FRESH PowerShell
  window: a window that ran the older version of this step still carries `Vrf__...` variables, and a variable
  beats the profile. Check it:
      Get-ChildItem Env:Vrf__* -ErrorAction SilentlyContinue
  SEE: nothing. Anything else: close the window, open a new one.
  THE DEPLOYED PROFILE must be this one:
      Select-String -Path src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\appsettings.Demo.json -Pattern '"ModelSet": "AggregateTacticalLevel"'
  SEE: exactly one line. Nothing = an older deploy: stop, call an engineer (a redeploy, RUNBOOK sec 9).
      pwsh -NoProfile -File scripts\StartInterface52.ps1 -ClientId "Not Set" -Server standard -AppNumber i -WhatIf
  (i: the claimed number.) Read the plan, then run the same line without -WhatIf. Use this window for nothing
  else. Without -AppNumber the script warns and falls back to 9101 - do not.
  - `-ClientId` MUST equal the SystemName in the initialization STP pushes. STP's Iron Storm export says
    `Not Set`; whether STP sends the same when it pushes live is UNVERIFIED - ask the STP operator. A
    mismatch creates NOTHING and looks healthy.
  - `-Server standard` = 8080/61613; `-Server private` = 18080/61614. Use the one STP is pointed at (ask).
  - PACE (RL-20261011-01): the profile runs DurationScale 0.25 - every authored duration and start delay is a
    quarter; the accepted tasks are terminal about 3 wall min after the order (2026-10-05). At 1.0 (STP's own
    durations) it took about 13 min (2026-10-11). Nothing to type for it.
  - RAN LIVE on this hand-started path 2026-10-05 (0.25 by hand) and 2026-10-11 (the profile, then at 1.0).
  WHAT THE RUNNER SET IN THE REHEARSED RUNS THAT THE PROFILE DOES NOT CARRY, AND WHY:
  - application number (a fresh ledger number per run): StartInterface52 -AppNumber (RL-20261011-01).
  - Vrf__ClientId and the C2SIM server pair: StartInterface52's -ClientId and -Server.
  - object-console levels 4 / 4 (RunScenario.sh's defaults, for the scorers): the profile keeps -1, the vendor
    default - a diagnostic channel nothing in the demo reads. A difference from the rehearsed runs (UNVERIFIED).
  - Vrf__ConnectionConfigFile in the unattended appData tree: the profile names the vendor tree's copy; the two
    files were byte-identical on 2026-10-05. If the unattended copy is ever edited, set it to that file.
  - Vrf__Scenario as a rooted path: the profile names the fixture, which resolves under VrfHome to the same file.
  - RunScenario.sh's Vrf__TaskPredecessorTimeoutSeconds=7200: every rehearsed run overrode it with 600 (--env).
  - the MAK PATH / MAK_*DIR / RTI variables: StartInterface52 and the app's own MakRuntime bootstrap set them.
  - RunScenario.sh's DeStackRotationDeg 0 and DropOriginVertexMeters 100 are the code defaults, inherited as is.
  SEE, in the interface console:
      *** C2SIM SERVER THE INTERFACE WILL LISTEN TO: rest=...  stomp=... ***   (must be STP's server)
      MODEL SET RULE (D2b; ...): Vrf:ModelSet='AggregateTacticalLevel' -> AggregateTacticalLevel ...
      DEMO EXTENT ON (Vrf:DemoExtent, RL-20261005-02): the demo extent S 53.93972 ... + 2 km margin ...
      READY - joined the federation, 1 VR-Forces back-end(s), type mapping = FidelityTable, ...
  If READY never appears, the interface did not join: stop and fix that before STP pushes anything.
  "DEMO EXTENT NOT ARMED" or "-> EntityLevel" in those lines: stop (Ctrl+C) - a stale `Vrf__` variable is
  overriding the profile; open a fresh window and start again.

2.3 STP PUSHES: the initialization first, then - after the interface prints
  `READY TO TASK - N of N init unit(s) bound` - the order. Never push a second initialization into a running
  interface (it is ignored by design); a re-run is the full cycle of section 4.
  WHAT THE AUDIENCE SEES (the counts are the bounded rehearsal's, raw export, pushed by the runner):
  - After the initialization: every unit appears as an EMPTY aggregate container (36), labelled.
  - At the order, within about a second: 16 of the 23 tasks are refused OUT OF DEMO EXTENT (their unit
    starts, or their route runs, outside the area); T17 is skipped (its predecessor T16 was refused). The
    7 accepted tasks belong to 28ID, 1-112 IN and 48 IBCT, which are populated in place: 1 + 5 + 17 = 23
    member units appear. Nothing else is populated.
  - 48 IBCT (T14) drives about 2.8 km along STP's own line (the aggregate Auto planner); T1, T2, T10, T11
    and T13 are holds. All 23 tasks were terminal about 2 min 47 s after the order at DurationScale 0.25, about
    13 min at the profile's 1.0.
  - Labels: the clean designator, e.g. "48 IBCT/28ID" on the container and "48 IBCT/28ID.INF3RIF1" on a member
    (RL-20261005-03; SEEN 2026-10-11), each under a line with the unit's 30-character name (section 5).
  EXPECTED INTERFACE LINES: one "DEMO EXTENT (Vrf:DemoExtent, RL-20261005-02): 23 task(s) in this order, 16
  refused out of ..." summary naming the units it will not populate; one TASKABRT per refused task, "OUT OF DEMO EXTENT:
  <what> at <lat,lon> is <km> km outside ..."; one "POPULATE IN PLACE ... N of N created" per populated
  unit; TASKCMPLT per finished task; position reports every 10 s.
  IF THE UNITS NEVER APPEAR: (1) the server pair (2.2's loud line vs where STP pushed); (2) the ClientId;
  (3) an ERROR "MODEL SET RULE ... REFUSED" line = the interface did not run the Demo profile's aggregate model
  set - a stale `Vrf__ModelSet` in the window, or the appsettings.Demo.json deployed beside the exe predates the
  2026-10-05 profile (0.2: the deployed build must include it).

2.4 STANDING IN FOR STP (rehearsal, or STP not in the room) - the endpoints MUST MATCH the pair the
  interface printed. Use BOTH lines of ONE block, never one from each:

  PRIVATE server - use with `StartInterface52.ps1 -Server private` (rehearsal):
    tools\PushInit\bin\Release\net10.0\PushInit.exe data\STP-IRON-STORM-SYNTHETIC_Initialization.xml http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM
    tools\PushOrder\bin\Release\net10.0\PushOrder.exe data\STP-IRON-STORM-SYNTHETIC_Order.xml 60 http://127.0.0.1:18080/C2SIMServer http://127.0.0.1:61614/topic/C2SIM

  STANDARD server - use with `StartInterface52.ps1 -Server standard` (only when the demo is meant to run
  on the operator's own server):
    tools\PushInit\bin\Release\net10.0\PushInit.exe data\STP-IRON-STORM-SYNTHETIC_Initialization.xml http://127.0.0.1:8080/C2SIMServer http://127.0.0.1:61613/topic/C2SIM
    tools\PushOrder\bin\Release\net10.0\PushOrder.exe data\STP-IRON-STORM-SYNTHETIC_Order.xml 60 http://127.0.0.1:8080/C2SIMServer http://127.0.0.1:61613/topic/C2SIM

  A MISMATCHED PAIR IS SILENT: the interface waits on one server, the push succeeds on the other, nothing
  is created, every console looks healthy. Never hand-edit an initialization or order in the room.
  Each command is ONE line: PowerShell 7 has no `^` continuation (cmd's `^` would become a stray argument).

---

## 3. WAY A - the runner plays STP (fallback)

From the main checkout, in a FRESH Git Bash window (not 2.2's PowerShell window), after section 1. The
runner claims its own 11 application numbers from the ledger and moves the marker itself, starts its own
short-lived holder (it joins the federation 1.3 holds), uses the PRIVATE server, and tears everything down.
Watch it from a second window with `tail -f <the runner log it names>`; never pipe it, never run a process-killing sweep while it runs.

3.1 THE RAW EXPORT, BOUNDED - exactly the command that passed (REHEARSAL_WAYB_BOUNDED_2026-10-05):
    scripts/RunScenario.sh --scenario IronStorm_Centre_52_Aggregate \
      --init data/STP-IRON-STORM-SYNTHETIC_Initialization.xml --order data/STP-IRON-STORM-SYNTHETIC_Order.xml \
      --client-id "Not Set" --model-set auto --duration-scale 0.25 --object-console 4 --member-console 4 \
      --stop-when-complete --run-secs 2700 --env Vrf__StallDetection=true --env Vrf__StallClock=sim \
      --env Vrf__TaskPredecessorTimeoutSeconds=600 \
      --env Vrf__DemoExtent=53.939723,23.108483,54.119385,23.414360 --env Vrf__DemoExtentMarginKm=2 \
      --sample-threads --gui --vrf-appdata-dir 'C:\C2SIM\vrf-appdata-unattended\appData' \
      --log runs/launch52/RunScenario-demo-$(date -u +%Y%m%dT%H%M%SZ).log
  (The runner reads appsettings.json, not the Demo profile, hence the extent by --env. Keep the single
  quotes: an unquoted backslash path is mangled by bash.) SEE: the GUI as in 2.3, then teardown: StopVrf52
  exit 0 or 6 (6 in the rehearsal), "no VR-Forces processes remain", runner exit 0. The run ends by itself
  once every task is terminal - in the rehearsal about 6.5 min after the order, teardown included.

3.2 THE DERIVED CUT-A ORDER - more motion: T02, T10 and T14 all drive and arrive, 5 of 5 tasks done about
  3 min after the order (G1-6, 2026-10-04, headless). It is STP's order with hand fixes
  (data/IRONSTORM_CUTA_CHANGES.md), so say so if you show it. Same command as 3.1 with:
      --init data/IRONSTORM_CUTA_Initialization.xml --order data/IRONSTORM_CUTA_Order.xml
  and the two `--env Vrf__DemoExtent...` options dropped (cut A lies inside the area by construction).
  With the GUI: UNVERIFIED.

---

## 4. Teardown and reset

WAY A: the runner tears down. StopVrf52's verdict is in its log: 0 = down cleanly; 6 = the back end refused
the graceful close and the runner force-stopped ITS OWN back end, nothing else (what both 2026-10-05 runs
did - normal today); 7 = forced, but another VR-Forces process is still up (usually a vrfGui: see below);
8 = the back end had CRASHED before the close - an engineer wants the .callstack.log / .dmp from C:\MAK\logs,
never the vendor .log (it prints the whole environment, secrets included).

WAY B, in this order - THE INTERFACE FIRST, so it resigns from the federation cleanly:
  1. Ctrl+C in the interface's own window does the same thing as `tools\StopIface` (RUNBOOK sec 4) and is
     the simple choice at the keyboard. The scripted stop is
         tools\StopIface\bin\Release\net10.0\StopIface.exe http://127.0.0.1:8080/C2SIMServer http://127.0.0.1:61613/topic/C2SIM --yes
     with the SAME pair the interface listens to (18080 / 61614 for -Server private). It drives that
     server to UNINITIALIZED - on the standard server that is STP's session too, so agree it with STP.
  2. VR-Forces:
         pwsh -NoProfile -File scripts\StopVrf52.ps1
     0 = down. 3 = still running, NOTHING was killed - the back end ignored the graceful close (the same
     refusal the runner answers with exit 6). EXPECT 3: every run from 2026-10-05 to 2026-10-11 (4 of 4) got it.
     Then force ONLY the back end 2.1 started (pid N), identified by pid AND start time (done by hand on
     2026-10-05 and 2026-10-11, exit 6 both; the runner does exactly this):
         $p = Get-Process -Id N
         pwsh -NoProfile -File scripts\StopVrf52.ps1 -ForceOwnBackendPid $p.Id -ForceOwnBackendStartUtc $p.StartTime.ToUniversalTime().ToString('o')
     6 = done; 7 and 8 as for Way A. 5 = unexpected error - VR-Forces may still be up; call an engineer.
  3. Get-Process vrfSim*, vrfGui*, VrfC2SimApp -ErrorAction SilentlyContinue  -> nothing. Leave rtiexec,
     rtiForwarder and every RtiProbe holder running.
  4. Labels: the two -Verify lines of 0.4 once after the first GUI exit (the GUI could rewrite its own
     settings on exit; on 2026-10-05 it did not).

IF A vrfGui IS LEFT ON "Session Status" OR "Are You Sure?" (it should not be, on the unattended tree): click
Yes on "Session Status", then File > Exit, then Yes on "Are You Sure?" (RUNBOOK sec 0.5, AMENDMENT
2026-10-05). While it is up, the next launch refuses to start.

RESET BETWEEN RUNS: the full cycle only - stop the interface, stop VR-Forces, launch again, start the
interface again, push the initialization again (Way A: run the command again). The interface builds its
unit map at initialization, so it must be restarted. Leave a few minutes between runs.

---

## 5. Known limits - what the presenter says (work in progress, RL-20261004-04)

- ONLY THE DEMO AREA RUNS (RL-20261005-02). Tasks whose unit starts, or whose route runs, outside the
  Iron Storm centre area are REPORTED out of area with the reason, not executed: 16 of 23 on STP's order.
  Why: the full order made the simulation page terrain far to the east and fall minutes behind
  (REHEARSAL_WAYB_2026-10-05). The refused units stay on the map as empty, labelled containers.
- SEVERAL TASKS ARE HOLDS because STP's export gives them no geometry (T2 and T10 on the raw export).
  The interface invents no positions (RL-20260914-02, kept by RL-20261005-04); the ask is filed with STP
  as STP-962. Inside the area that leaves one mover (48 IBCT) and five holds - say it is a thin slice.
- PACE. The simulation runs faster than real time while the GUI clock reads 1x: 3.0-7.4x in the Iron
  Storm rehearsal. The ratio is LOAD-DEPENDENT and moves within a run (Mojave: 2.6x under load to 4.7x
  idle, RUNBOOK sec 11f; 4.1-9.9x on 2026-10-11), so never quote one number; the interface's per-minute `SIM/WALL RATIO` line is
  the instrument. The real-time fixture is not in the demo path (RL-20260921-02); a time-multiplier setting
  was never decided (HANDOFF_SEAT_2026-09-28 sec 4). The demo runs DurationScale 0.25 (RL-20261011-01): the
  show lasts about 3 min, every task a quarter of STP's authored time - say so. At 1.0 it ran about 13 min.
- LABELS OVERPRINT where units stack (48 IBCT over 1-112 IN, the 28ID headquarters). Zoom in. The long
  C2SIM description after "__" is dropped for now; an extended label comes after the demo (RL-20261005-04).
  The VR-Forces NAME line (the C2SIM name cut to 30 characters, e.g. "169_FAB/28ID__FRIENDLY_FIELD_A") is
  hidden by the 0.4 deploy (RL-20261011-01); only the clean label shows. Identity is the UUID, never the name.
- SOME 48 IBCT MEMBERS END OFF THE OBJECTIVE: 6 of 17 stopped 262-790 m from it in the rehearsal; not yet
  explained. Do not promise that every vehicle reaches its point.
- WHAT STP SHOWS (as last checked 2026-09-14, STP pinned to SDK 1.3.1; UNVERIFIED since): STP displays the
  position reports only. Task starts, completions and the out-of-area refusals are SENT (the interface log
  and a bus capture show them) but STP does not display them yet. Say the interface REPORTS them.

---

## 6. When something goes wrong

- LAUNCH REFUSED, A VR-FORCES PROCESS IS UP: section 4, then launch again. Never "force it through".
- "the federation HOLDER could not join ... after 4 attempt(s)" (STP-825): 1.3 was skipped or its holder
  has ended (8 h). Start it, then launch again. If the holder itself exhausts its numbers: stop, ask.
- UNITS NEVER APPEAR: 2.3's three checks - server pair, ClientId, model set.
- `runner exit: 127` does NOT mean "command not found": something outside killed the run.
- Never kill rtiexec / rtiForwarder / rtiAssistant / RtiProbe. Never send a VR-Forces .log to anyone.

---

## Appendix A - history (the entity-level Mojave runbook)

Until 2026-10-05 this file described the ENTITY-LEVEL demo on the Mojave fixtures (R9_Mojave_Empty_52 and
its _AG siblings; R9 lean and COA-STP1 orders; Way A primary, Way B with clientId STP). Its full text is in
git: `git show b503c9b:docs/DEMO_RUNBOOK.md` (sections 0.4 terrain pre-warm, 0.5 navigation set-up, 6 reset
options and 9 smaller traps are there). What carried over is above. Records kept here because later
readers cite them:
- RUN D1 (2026-09-20): its "real time, ratio 1.00" reading is WITHDRAWN: it compared wall clock against wall
  clock (RUNBOOK sec 11f).
- VERIFIED LIVE AS A SINGLE SEQUENCE (D5c, 2026-09-21): rtiexec, LaunchVrf52 with its own holder,
  StartInterface52, then PushInit/PushOrder standing in for STP, on R9 Mojave and the private server:
  3/3 TASKCMPLT, clean teardown. Still owed then and now: STP ITSELF PUSHING, a human reading the lines,
  and Iron Storm.
- The Way B example once read 9201/9202, which sit outside the
  block entirely and were therefore exempt from nothing - corrected 2026-09-21 to 9102/9103.
