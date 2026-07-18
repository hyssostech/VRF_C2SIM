# RESUME PROMPT (2026-07-18 evening - RE-GROUNDED ON THE HEADLESS GOAL)

Paste the block below into a fresh session. It supersedes all earlier resume prompts.
Keep this file current AS work lands. ASCII only.

---

Resume the C2SIM -> VR-Forces interface effort in SUPERVISOR MODE (user-directed standing
model): YOU supervise - design and gate work, adjudicate evidence adversarially, keep docs
current AS work lands - while executor agents do the reading, analysis and code. Re-run
executors' acceptance checks with your own hands before accepting any load-bearing claim.

*** WHAT WE ARE BUILDING - READ THIS FIRST, IT IS THE THING MOST RECENTLY GOT WRONG. ***
A HEADLESS interface: a C2SIM document goes in; units are initialized, tasked and run in
VR-Forces; the outcome is verified from telemetry. ONE button. The user's words
(2026-07-18): "Zero humans using the UI to task anything, or to click on terrain, etc.
that is the whole point. I click a button, my C2SIM plan plays on its own."

TWO COROLLARIES YOU MUST NOT RE-DERIVE WRONGLY:
1. VERIFICATION NEVER REQUIRES A HUMAN. Whether a run matched its C2SIM input is decided
   by arithmetic on the WatchVrf POS trace (expected waypoints vs observed displacement)
   plus the reports ListenReports captures. It is not a manual review step. It never was.
2. CREATION AND TASKING NEVER REQUIRE A HUMAN EITHER. tools/CreateOne creates entities
   headlessly through the remote API, and the FULL pipeline (C2SIM init + order -> create
   -> task -> unit MOVES -> TASKCMPLT pushed back -> clean stop) was verified end to end
   on 2026-07-10 - RUNBOOK sec 7. The loop RUNS. What is wrong with it is QUALITY: wrong
   unit types (generic fallbacks), runaways, and completions that lie in both directions.

*** THE MISTAKE THIS PROMPT EXISTS TO PREVENT. *** On 2026-07-18 the supervisor read
PHASE1_SESSION_SCRIPT.md's line "the user drives the VR-Forces GUI for creation/tasking",
treated it as a constraint on the WHOLE EFFORT, told the user that live testing therefore
needed a human - and then spent a work block building UI-Automation menu-driving for
VR-Forces to route around the human it had just invented. It had ALREADY, in that same
session, created an entity headlessly and verified its position with zero human input,
and did not notice the contradiction. IF YOU FIND YOURSELF PLANNING GUI AUTOMATION, OR
TELLING THE USER SOMETHING NEEDS THEM TO CLICK, STOP - you are repeating this.
GUI use is DIAGNOSTIC ONLY and may never be load-bearing in a scored run. The ONE
legitimate exception is simulator lifecycle: StopVrf.ps1 answers VR-Forces' quit dialog
via UI Automation so the sim can be torn down unattended. That is not tasking.

*** READ THE VENDOR DOCUMENTATION BEFORE EXPERIMENTING. *** An earlier session burned a
whole live window probing what was DOCUMENTED VENDOR BEHAVIOUR on disk. Docs:
C:\MAK\vrforces5.0.2\doc\help\Content (HTML) and C:\MAK\makRti4.6.1\doc\*.pdf. When
something "hangs": read the vendor PROCEDURE, then its DIAGNOSTICS, and only then probe.
Pre-register probes (one variable; prediction + falsifier written BEFORE running).
Movement claims REQUIRE WatchVrf displacement - completions LIE in both directions.
ASCII-only in tracked files.

ASSUME YOUR OWN CONCLUSIONS ARE DEFECTIVE. This project's record includes a cause
published, retracted, republished as its opposite, and wrong BOTH times; three successive
wrong "corrections" written into RUNBOOK sec 0.5; and a supervisor that "fixed" a rule in
the reference docs while leaving it stated as current in the two entry-point documents.
All are recorded in place deliberately. Do not re-derive them. DO NOT "CLEAN UP" PROCESSES
YOU DID NOT START.

*** ADVERSARIAL SWEEPS ARE MANDATORY - SELF-REVIEW DOES NOT WORK HERE. *** Before any
handoff and after any sustained work block, spin CLEAN-CONTEXT agents to audit what you
produced. Give them NO summary of your reasoning and tell them explicitly not to trust it.
Run at least: (a) DOC-CONSISTENCY (contradictions, surviving stale instructions,
superseded text appearing BEFORE its correction, ledger integrity); (b) CODE (half-applied
fixes, output text that misdescribes what the code does); (c) FACT-CHECK (every vendor
quote and line cite re-verified against the files on disk); (d) COLD-READER (an agent
given ONLY this prompt, which must answer "what are we building", "how do I run it",
"what must I not kill" without guessing). Rank CRITICAL/MAJOR/MINOR, fix in that order,
and RE-VERIFY the fixes rather than assuming they landed. The 2026-07-18 sweeps found 8
CRITICAL + 13 MAJOR in already-pushed work, then 7 CRITICAL + 7 MAJOR in the work that
fixed those.

WHERE THE WORK LIVES: port repo VRF_C2SIM (submodule at OpenC2SIM.github.io/Software/
Interfaces/VRF_C2SIM, branch main, remote origin = hyssostech/VRF_C2SIM). Everything is
committed and pushed - confirm with git log --oneline -5 and git status -sb. PUSH POLICY
(user, 2026-07-18): commit and push once offline gates are green (build clean, selftests
pass, ASCII check clean) and nothing live-unverified is recorded AS verified. Do not sit
on completed work. Writing product code still needs a go-ahead; committing gated green
work does not.

READ IN ORDER:
(1) docs/VRF_GROUNDWORK_PLAN.md - sec 1a THE HEADLESS MANDATE first, then the Status TOP
    entry (which IS the current state), then Phases 3-5.
(2) docs/HEADLESS_RUN_PLAN.md - THE NEXT ACTION. The complete headless chain, the runner
    to build, the first run target, and the tooling defects that block it.
(3) docs/RUNBOOK.md secs 0 / 0.5 / 0.6 / 7 before any live work. 0.5.0 pre-flight process
    inventory; 0.5.1 launch; 0.5.2 never-kill; 0.5.7 the CORRECTED oracle pre-check;
    0.5.9 unattended teardown; sec 7 the port's live run environment (four things that
    must be right or it crashes or hangs).
(4) docs/VRF_GROUND_TRUTH.md sec 0.0 cross-findings 1-7 (note item-2 REFINEMENT, item-6
    warp decomposition, item-7 oracle bug), then 0.1 catalog / 0.2 curriculum / 0.3 API
    audit / 0.5 scnx.
(5) Phase-2 deliverables: TYPE_GAP_ADJUDICATION.md (incl. USER RULINGS), TYPE_MAPPING_
    TABLE.md, TASK_VOCABULARY_V2.md, PRIOR_ART_SURVEY.md, experiments/RUNAWAY_WARP_
    CENSUS_2026-07-17.md (sec 11 = warp reinterpretation).
(6) docs/PHASE1_SESSION_SCRIPT.md - SUPERSEDED IN METHOD. Read its top banner ONLY, to
    understand what survives. Do NOT run it; it is the human-at-the-GUI diagnostic.
(7) docs/experiments/SESSION_2026-07-18_SELFLAUNCH.md - how the launch problem was solved
    and mis-diagnosed five times first. "SUPERSEDED" sections are labelled AT the heading.
    NOTE experiments/PREREG_0_4_SELFLAUNCH.md secs 1-10 contain PROCEDURES NOW DANGEROUS
    (they order clearing RTI processes) - its banner says so; heed it.

THE NEXT ACTION (docs/HEADLESS_RUN_PLAN.md):
- BUILD scripts/RunC2SimScenario.ps1 - the button. Sequences: LaunchVrf -> VrfC2SimApp ->
  PushInit -> PushOrder -> WatchVrf -> ListenReports -> StopIface -> StopVrf, unattended,
  leaving a timestamped run directory (WatchVrf trace, captured reports, app log, and a
  manifest of every appNo, both clocks, and each stage's exit code).
- FIRST, FIX THE ARGUMENT GUARDS - they are hazards in an unattended runner:
  tools/StopIface ACTS WHEN RUN WITH NO ARGS (it drove the live C2SIM server
  RUNNING -> UNINITIALIZED during a usage probe on 2026-07-18); PushInit throws a bare
  ArgumentException; PushOrder throws IndexOutOfRangeException. Copy the
  CreateOne/SetSimRate pattern: no defaults, usage message, hard exit 2, NO ACTION.
- FIRST RUN TARGET: data/R9_Mojave_Initialization.xml + data/R9_Mojave_UnitMove_Order.xml
  (Mojave matches the loaded TropicTortoise terrain; one unit move = binary result, no
  aggregation or controller-class confound).
- WatchVrf must START BEFORE the init is pushed so unit births are in the trace.
- Then measure what the CURRENT interface produces. Do NOT start the Phase 3 creation-
  layer rebuild before there is a measured baseline to score it against.

OPERATIONAL STATE:
- VR-FORCES LAUNCHES UNATTENDED:
    $env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')
    pwsh -File scripts\LaunchVrf.ps1 -Scenario TropicTortoise `
         -BackendAppNumber <fresh> -FrontendAppNumber <fresh>
  Expect EXIT=0 and "[OK] READY", ~20-35 s. Do NOT pass -AllowExistingRtiAssistant: it
  still parses but is a NO-OP the script retains only for compatibility.
- VR-FORCES TEARS DOWN UNATTENDED:  pwsh -File scripts\StopVrf.ps1   (EXIT=0; -DryRun to
  preview). Closing vrfGui raises a modal "Are You Sure?" confirm that blocks until
  answered; StopVrf answers it via UI Automation BY CONTROL NAME. That dialog exposes a
  full UIA tree; the RTI connection dialog does NOT - do not generalise between them.
  The dialog's composition VARIES between launches (a "Quit All Back-Ends" checkbox was
  present on one teardown, absent on the next); both succeeded, cause unknown.
- PRE-FLIGHT: INVENTORY PROCESSES FIRST (RUNBOOK 0.5.0). A VR-Forces instance from a
  previous session SURVIVES a context clear, and LaunchVrf HARD-REFUSES to launch on top
  of one. -AllowExistingVrf is NOT the way past that - it is the documented false-READY
  trap. Tear the leftover down with StopVrf instead. Identify it by command line:
  Get-CimInstance Win32_Process -Filter "Name='vrfSimHLA1516e.exe' OR Name='vrfGui.exe'"
  carries --appNumber and --scenarioFileName.
- *** NEVER KILL rtiAssistant / rtiexec / rtiForwarder. *** RTI infrastructure, persists
  across launches. An ALREADY-ANSWERED rtiAssistant is what makes unattended launch work.
  A new assistant failing to bind port 6003 each launch is EXPECTED AND BENIGN.
- IF the "Choose RTI Connection" dialog appears (e.g. after a reboot - UNTESTED): answer
  ONCE with "Always try to use this connection" CHECKED, then Connect. Qt window, NO UIA
  tree; automate by screenshot + coordinate click - Connect centre is window-relative
  (383,553) on a 573x583 dialog; allow >3 s to dismiss.
- DO NOT set RTI_ASSISTANT_DISABLE. Processes start in 8 s but federates never discover
  each other and WatchVrf goes SILENTLY BLIND (reflected=0).
- BACKEND HEALTH = THREAD COUNT (blocked 2-4 indefinitely; healthy 23-70 observed - that
  is the range seen, not a ceiling). PROCESS PRESENCE IS NOT HEALTH. "UDP 4000 bound" is
  NOT health - connection-dependent and FALSE on a healthy back-end here.
- ORACLE PRE-CHECK - THE CRITERION WAS WRONG IN BOTH DIRECTIONS AND IS CORRECTED
  (RUNBOOK 0.5.7). The retired rule was "require reflected>0; if 0 after 20 s, STOP".
  reflected>0 PASSED live on nothing but a 90/-90 pole placeholder and a NaN - the
  TropicTortoise baseline objects are POSITIONLESS. And reflected=0 at 20 s is usually
  MEASURED TOO EARLY: the same healthy launch read visible at ~40 s, BLIND at ~20-50 s,
  visible again at ~104 s. LaunchVrf's READY means thread-count + main window, NOT
  scenario loaded and NOT federation joined. USE: PASS = at least one POS line with REAL
  lat/lon (not NaN, not the 90/-90 pole); RETRY with a FRESH ledgered appNo for up to
  ~3 MINUTES; STOP only if nothing real appears by then AND a CreateOne entity also
  fails to read back real coordinates. The CreateOne check is MANDATORY - on a stock
  TropicTortoise load it is the only check that CAN pass.
- C2SIM server docker RUNNING and verified 2026-07-18: REST http://127.0.0.1:8080/
  C2SIMServer -> HTTP 200, STOMP 61613 open. License expires 2026-09-15.
- TIMESTAMP GOTCHA: app/tool logs stamp UTC, machine runs local (-04:00) - Get-Date and
  record both before comparing anything.
- Non-negotiables: never force-kill a JOINED federate; fresh appNo per join, LEDGERED
  BEFORE the join, taken from the SINGLE line marked "*** NEXT FREE:" in
  OPUS_EXECUTION_PLAN.md Appendix B - READ THE MARKER, never trust a number quoted in
  prose (a cached one went stale and is a recorded defect). This includes the app's own
  Vrf__ApplicationNumber. RTI 4.6.1 + Machine-scope license + cwd bin64 for live runs;
  raw vrfSimHLA1516e CLI is CONFIRMED UNSAFE (vrfLauncher only); XML gotchas per
  RUNBOOK 0.6.

STATE:
- The headless chain is COMPLETE AND BUILT. VrfC2SimApp builds 0 errors; PushInit,
  PushOrder, StopIface, ListenReports rebuilt Release 0 errors; WatchVrf, CreateOne,
  SetSimRate, ResetVrf, ScnxDiff already built. scripts/RunC2SimScenario.ps1 is the ONE
  MISSING PIECE.
- Launch and teardown are both unattended and verified; the round-trip
  launch -> teardown -> relaunch was proven, and a relaunch is CONFIRMED to clear a
  CreateOne throwaway entity from the live scenario.
- ORACLE VERIFIED for DISCOVERY + POSITION FIDELITY (not displacement): a created M1A2
  read back 34.517156,-116.973525,1060.7 - requested lat/lon exact, 10000 m MSL
  ground-clamped. THE ENTITY WAS STATIONARY; DISPLACEMENT IS STILL UNEXERCISED THIS
  SESSION. Archived runs show displacement capture working; do not cite CreateOne for it.
- Phase 0 and Phase 2 offline tracks are DONE (content catalog, docs curriculum, API
  audit, scnx, console capture, type mapping 128 = EXACT 7 / NEAR 64 / PEND 54 / LONE 1
  / AVN 2, task vocabulary v2, runaway/warp census).
- PHASE1_SESSION_SCRIPT.md is SUPERSEDED IN METHOD. 3455 is BURNED (consumed by a
  pre-check); only 3456-3459 remain reserved from that block.

PENDING USER DECISIONS (do NOT decide these yourself; none block the next action):
- ArmorCoHQ Decision 4: A (one-field match fix -> 4 generic dismounts) vs B (retarget ->
  Tank Headquarters Section; militarily correct). Supervisor recommends B. An authored
  armor BN HQ is a real option C the adjudication doc dismissed.
- Q1 hostile-side country: (a) USA-225 as today, (b) RUS-222 mirrors, (c) author
  Country-45 (China). VERIFIED: 11 Chinese platforms on disk, air/naval/AD ONLY - ZERO
  ground-combat platforms, ZERO Chinese aggregates. Ask what drives the question first;
  if exploratory, (b) is clearly right.
- Golden-order file identity for Phase-5 scoring: the golden-trace MOVE pair
  (authoritative but MOVE-only) vs data/VRF-Approved-5June24_Order.xml (38 MOVE + 29
  SCOUT, but SCOUT is dead).
- AGGREGATION-STATE POLICY - CONFIRM OR CORRECT: a supervisor inferred that entity-level
  units should run DISAGGREGATED where the verb needs combat. Recorded as policy and it
  will drive Phase 3, but the user never said it. Flagged as a possible over-read.

KNOWN RESIDUAL DEFECTS (recorded, deliberately not all fixed):
- scripts/LaunchVrf.ps1 picks the back-end with Select-Object -First 1 and never
  correlates to the process it launched - with -AllowExistingVrf beside an older healthy
  back-end it can measure the OLD one and report a FALSE READY.
- LaunchVrf: no validation on -PollIntervalSec / -ReadyTimeoutSec / -BackendMinThreads;
  exit codes documented only inside a dry-run string; -Mode FrontendOnly waits the full
  timeout for a back-end it never launched then reports a wrong diagnosis; its
  "VR-Forces/RTI processes ALREADY running" warning inspects only the three VR-Forces
  names, never any rti* process.
- tools/ListenReports targets net6.0 while the rest target net10.0, and writes its
  capture beside its own binary rather than to a caller-specified run directory.
- tools/CreateOne hard-codes the M1A2 DIS type with no override.
- UNTRACED: whether vrfLauncher needs "--" to terminate --simArgs/--guiArgs; PowerShell
  re-quoting of the embedded-quote -ArgumentList when a name contains spaces.

DO-NOT-RELITIGATE (evidence-settled): altitude as the FREEZE cause (FIXED both
codebases); altitude as the RUNAWAY cause (EXONERATED both); nav data; DIS type;
formation names; pile-size-as-sufficient; name-length collisions; template quality;
echelon-as-such; aggregate-level modeling (user RULED it out); "the original works
better" (falsified both directions); the VR-Forces launch/hang class (it was the
documented RTI Assistant prompt); the vrfLauncher --simArgs/--guiArgs overrides (FULLY
EXONERATED after being wrongly accused twice).

OPEN BY DESIGN: paged-tile boundary as the stop context; warp backend-vs-observer; and
the E7 "18.1-18.4 km band at 1x" verdict carries a qualifier because the C++ oracle
applies a time multiplier even when its own disable flag says it is ignoring it (ground
truth 0.0 item 7). Not contaminated; not clean.

C++ repo (c2simVRFinterfacev2.36): probe branch probe/create-altitude-above-ground at
b96688b; master pristine at 191933a - DO NOT develop there. Its untracked tools/ dir is
BUILD OUTPUT ONLY - do not commit it.

START by reporting: git log --oneline -5 + git status -sb of the port repo; confirmation
you read the HEADLESS MANDATE (plan sec 1a), the plan Status TOP entry, and
HEADLESS_RUN_PLAN.md; the current "*** NEXT FREE:" value READ FROM THE MARKER; and a
process inventory (RUNBOOK 0.5.0). Then propose the next step. Get explicit user
go-ahead before ANY live work and before writing product code. Committing and pushing
GATED, GREEN work does not need a separate go-ahead.
