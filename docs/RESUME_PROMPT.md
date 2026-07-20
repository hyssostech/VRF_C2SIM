# RESUME PROMPT (2026-07-19 LATE - THE LOOP RUNS; TWO OF THREE UNITS ARE FROZEN, THE THIRD MOVES)

Paste the block below into a fresh session. It supersedes all earlier resume prompts.
Keep this file current AS work lands - the 2026-07-18 version went stale within a day and
still told its reader to build something that already existed. ASCII only.

---

Resume the C2SIM -> VR-Forces interface effort in SUPERVISOR MODE (user-directed standing
model): YOU supervise - design and gate work, adjudicate evidence adversarially, keep docs
current AS work lands - while executor agents do the reading, analysis and code. Re-run
executors' acceptance checks with your own hands before accepting any load-bearing claim.

*** FIRST: READ docs/HANDOFF_2026-07-19.md. *** It is the single entry point - current
state, the ruled-out list, the traps that produce false greens on this machine, and the
open decision. Ten minutes there saves re-deriving a day of work.

*** WHAT WE ARE BUILDING. *** A HEADLESS interface: a C2SIM document goes in; units are
initialized, tasked and run in VR-Forces; the outcome is verified from telemetry. ONE
button. The user's words (2026-07-18): "Zero humans using the UI to task anything, or to
click on terrain, etc. that is the whole point. I click a button, my C2SIM plan plays on
its own."

*** THE BUTTON EXISTS AND WORKS. *** scripts/RunC2SimScenario.ps1 has taken a C2SIM init +
order through launch, join, creation, tasking, telemetry and clean resign with ZERO human
interaction. Do NOT plan to build it; it is built.
COUNT, CORRECTED TWICE - the first correction was ALSO wrong, which is itself the lesson:
161438Z, 202349Z and 222134Z are fully unattended end-to-end successes (THREE). 144109Z
reached a full trace but its teardown FAILED and the back-end was brought down BY HAND, so
it is NOT a zero-human run. Never count a run still in flight - the original "four" was
written while 222134Z had not reached teardown.
AND: my first correction said 222134Z was distinguished by "needing the back-end graceful
fallback". FALSE - RE-DERIVED, AND MY OWN
FIRST CORRECTION OF THIS WAS WRONG TOO (third correction; recorded because the pattern is
the point): 144109Z did not have the fallback feature AT ALL. Of the FIVE runs that DID
have it, it fired FIVE TIMES - including 193252Z, which I initially miscounted by grepping
one phrasing when that log uses another. THE GUI QUIT HAS NEVER ONCE
CARRIED THE BACK-END where it could be observed - the fallback is not a fallback, it is the
path, not a per-run distinction: the GUI quit routinely
fails to carry the back-end. A correction written in a hurry is just another unverified
claim. Do NOT plan GUI
automation - GUI use is DIAGNOSTIC ONLY. The one exception is simulator lifecycle
(StopVrf.ps1 answers VR-Forces' modals via UIA so the sim tears down unattended); that is
not tasking. If you find yourself telling the user something needs them to click, STOP - a
previous supervisor invented a human requirement and then built UI automation to route
around the human it had invented.

*** THE RESULT - THIS IS THE PROBLEM NOW. *** All three tasks are issued correctly
(CreateRoute + MoveAlongRoute in the app log, every run). Then:
  114.MechCoy   0.0 m, lat/lon BIT-EXACT every run - FROZEN, and this STANDS. (Precision: 114.MechCoy's altitude column flickers 0.1 m in 2 of 4 runs; sample counts are 76/76/75/77, not a flat 76. Neither weakens the result.)
  1.BdeHQ       0.0 m, lat/lon BIT-EXACT every run - FROZEN, and this STANDS. (Precision: 114.MechCoy's altitude column flickers 0.1 m in 2 of 4 runs; sample counts are 76/76/75/77, not a flat 76. Neither weakens the result.)
  1222.MechPlt  ~174 m - *** IT DID NOT STOP. WE STOPPED WATCHING. ***

*** THE "AND THEN STOPS" READING WAS FALSE AND IS RETRACTED (found by a cold-reader audit,
2026-07-19 late, verified by hand). *** The observation window is only ~145 s: the trace's
usable span runs from t~35 to t~180, where the interface RESIGNS at teardown and its
created objects are removed (the two uuids still readable AFTER the collapse are EXACTLY the two present at t=3,
BEFORE the interface created anything - cde66adc and f864e51f. That is the proof, and it
needs no clock alignment at all. DO NOT cite "StopIface fires at trace t=182.1s": that is
on a different clock from trace t and at face value puts the cause AFTER the effect.)
1222.MechPlt's speed in its FINAL observed leg, all three runs:
    161438Z  1.35 -> 1.46 m/s      202349Z  1.39 -> 1.49 m/s      222134Z  1.28 -> 1.48 m/s
IT WAS NOT SLOWING (do NOT claim acceleration: there are only THREE RPT fixes per unit per run = TWO legs, leg 1 starts at task issue and so contains spin-up from rest, and two legs cannot distinguish "still accelerating" from "reached cruise ~1.48 m/s". The conclusion is unaffected and arguably stronger at 1.48 m/s.) There is no deceleration signature and
no stop in evidence. The ~1155 m route needs ~825 s at ~1.4 m/s; the ~174 m spans the RPT
coverage window t=32.9->157.1 = 124.2 s (x1.403 m/s = 174.3 m), NOT the ~145 s observation
window - RPT stops reporting ~25 s before the collapse. 145 s x 1.4 would be 203 m. Get
this arithmetic right: the whole retraction rests on it. THE "REPRODUCIBLE 174 m" IS NOT A DEFECT.
*** PRECISION FIX 2026-07-20 - THE EARLIER PHRASING WAS WRONG. ~174 m is a REAL MEASURED
DISPLACEMENT: 84.0 m + 90.1 m across RPT's coverage span t=32.9 to t=157.1 = 124.2 s.
It is NOT "the length of the observation window" - that window is ~145 s and at 1.4 m/s
would predict ~203 m, which is not what was measured. The correct statement is: the unit
covered a MEASURED 174 m and WAS STILL MOVING when RPT coverage ended. NEW, and not
previously recorded anywhere: RPT STOPS REPORTING ~23 s BEFORE THE POS TRACE DOES
(t=157.1 vs t=180.3). That gap is its own open question. ***
CORRECTION TO A CLAIM THIS FILE PREVIOUSLY MADE: "duration is not the binding constraint"
was WRONG for this unit - PREREG_TSK_DELIVERY_2026-07-19.md:130 had already said so and the
newer docs contradicted it. FOR 1222.MechPlt, RUN LONGER: -RunSecs 900 or more. It remains
RIGHT for the two frozen units, which moved 0.0 m bit-exactly across 76 samples INSIDE the
observed window - that is a real result and needs no more time.
*** THE STRONGEST OFFLINE EVIDENCE, and RESUME previously omitted it entirely: in run
161438Z the taskee''s POS stream is parked at ~-116.6012 for 68 OF 76 samples, and at
t=159.9 EXACTLY ONE SAMPLE reads -116.598533 before snapping back - landing 4.58 m from
where RPT says the unit was at t=157.1 (~1.64 m/s). That is the POS channel briefly
emitting the true position and corroborating RPT AGAINST ITSELF. QUALIFIER, do not drop it:
n=1, in one run of three. 144109Z and 202349Z show no eastward outlier; 222134Z has two
small ones but BEFORE the westward snap. Still strong - a spurious sample landing that
close to the RPT-predicted position is an unlikely coincidence - but it is not settled. ***

*** PROVENANCE, and an audit caught this being glossed: THE ~174 m FIGURE IS RPT-DERIVED,
i.e. it comes from the very channel these docs elsewhere mark as "INFERRED, NOT PROVEN" to
be truthful. Under the RATIFIED scoring instrument - the POS trace - the SAME unit scores
-63.4 m (AWAY from its objective) and is NOT MOVED under AMENDMENT 1. The two channels
disagree; quoting only the friendlier number is exactly the trap. Quote BOTH or neither.
Also note the RPT "control" is 3 fixes per unit per run, not 76 - real evidence, but far
coarser than POS and unable to resolve the oscillation POS reports. ***
No TASKCMPLT is ever emitted WITHIN THE OBSERVED WINDOW - and note that absence is now
weak evidence, not strong: a completion at t=400 would be as invisible as an RPT at t=400,
because nothing is observed past ~180 s. Do not cite "no TASKCMPLT" as proof of anything
until a run observes long enough for a completion to be possible.

THE OPEN ENGINEERING PROBLEM, RESTATED CORRECTLY AFTER THE AUDIT:
  1. TWO of three taskees are genuinely FROZEN - 0.0 m bit-exact across 76 samples inside
     a window where the third unit demonstrably moved. That is a real defect and the
     primary one.
  2. The third unit MOVES, at ~1.4 m/s, and was never observed to stop. Whether it would
     ARRIVE is simply UNTESTED - nobody has ever run long enough.
The previous framing ("VR-Forces does not execute a well-formed MoveAlongRoute") was
overstated: it is true for two units and unsupported for the third.

*** THE BIGGEST OPEN ITEM OUTRANKS THE MOVEMENT RESULT: THE TWO ORACLES CONTRADICT. ***
WatchVrf now also emits RPT lines - VR-Forces' OWN position reports, carrying MARKING TEXT
(which incidentally solves the member-to-parent mapping the runaway census called
unsolvable offline). On the one unit that moves, the channels disagree about DIRECTION:
RPT shows steady EASTWARD progress toward the objective; the POS stream shows it 65 m WEST
and frozen. THE CONTROL: on the two units that do NOT move, the channels agree EXACTLY.
They agree on stationary objects and disagree only on the moving one - a TRACKING failure,
not two noisy sources. POS displacement is the standing "the ONLY movement oracle" rule, so
every negative movement result this project has recorded rests on it, including the
runaway/warp census. ESTABLISHED: they contradict, with a control. INFERRED, NOT PROVEN:
that RPT is truthful. Not settleable offline.

*** READ THE VENDOR DOCUMENTATION BEFORE EXPERIMENTING. *** An earlier session burned a
live window probing DOCUMENTED VENDOR BEHAVIOUR. Docs: C:\MAK\vrforces5.0.2\doc\help\Content
and C:\MAK\makRti4.6.1\doc\*.pdf. Reading MAK's own headers is what proved RUNBOOK sec 7's
long-standing RTTI explanation FALSE. Pre-register probes (one variable; prediction AND
falsifier written BEFORE running - on 2026-07-19 a pre-registered falsifier fired against
the supervisor's own prediction and was right). ASCII-only in tracked files.

ASSUME YOUR OWN CONCLUSIONS ARE DEFECTIVE. On 2026-07-19 alone: the oracle gate declared
success on lat 1e-6 / lon -90 / ALTITUDE 1.02e15 m because it never checked the altitude
column it was already handed; the runner printed "RUN COMPLETE - evidence collected" after
the oracle had crashed 3 seconds in; and a native "diagnostic" broke the pipeline TWICE
before being reverted. DO NOT "CLEAN UP" PROCESSES YOU DID NOT START.

*** ADVERSARIAL SWEEPS ARE MANDATORY - SELF-REVIEW DOES NOT WORK HERE. *** Before any
handoff and after any sustained work block, spin CLEAN-CONTEXT agents to audit what you
produced. Give them NO summary of your reasoning and tell them not to trust it. Run at
least: (a) DOC-CONSISTENCY; (b) CODE; (c) FACT-CHECK (every vendor quote re-verified on
disk); (d) COLD-READER given ONLY this prompt. The 2026-07-18 sweeps found 8 CRITICAL +
13 MAJOR in already-pushed work, then 7 CRITICAL + 7 MAJOR in the work that fixed those.

WHERE THE WORK LIVES: port repo VRF_C2SIM (submodule at OpenC2SIM.github.io/Software/
Interfaces/VRF_C2SIM, branch main, origin = hyssostech/VRF_C2SIM). PUSH POLICY (user,
2026-07-18): commit and push once offline gates are green and nothing live-unverified is
recorded AS verified. NATIVE FIXES ARE AUTHORISED (user, 2026-07-19) when genuinely
required - see the rules below. Writing new product code still warrants a go-ahead.

READ IN ORDER:
(1) docs/HANDOFF_2026-07-19.md - FIRST, see above.
(2) docs/VRF_GROUNDWORK_PLAN.md - sec 1a THE HEADLESS MANDATE, then the Status TOP entry.
(3) docs/HEADLESS_RUN_PLAN.md - sec 4a is the RATIFIED pass criterion INCLUDING AMENDMENT 1
    (MOVED now requires the distance to the objective to DECREASE - a unit oscillating in
    place satisfied the original rule).
(4) docs/experiments/RUN_2026-07-19_MOJAVE_CHAIN.md and
    docs/experiments/PREREG_TSK_DELIVERY_2026-07-19.md - the two result write-ups.
(5) docs/RUNBOOK.md secs 0 / 0.5 / 0.5.7 / 0.5.9 / 7 / 8 before any live work.
(6) docs/VRF_GROUND_TRUTH.md sec 0.0 findings 1-7, then 0.1 / 0.2 / 0.3 / 0.5.
(7) Phase-2 deliverables: TYPE_GAP_ADJUDICATION.md (incl. USER RULINGS), TYPE_MAPPING_
    TABLE.md, TASK_VOCABULARY_V2.md, experiments/RUNAWAY_WARP_CENSUS_2026-07-17.md.

THE NEXT DECISION - REORDERED BY THE 2026-07-19 AUDIT. The cold reader argued that both
options below assume the question is POS-vs-RPT accuracy, when a cheaper question comes
first. It was right, though not for the reason it gave (it read the t=180 collapse as an
oracle fault; it is teardown). The cheap question is simply: DOES THE MOVING UNIT ARRIVE?
  (0) RUN LONG ENOUGH - DO THIS FIRST, IT IS FREE AND IT IS UNTESTED.
      pwsh -File scripts\RunC2SimScenario.ps1 -RunSecs 900
      ~825 s of motion is needed; every run so far observed ~145 s. This settles whether
      1222.MechPlt ARRIVES, whether a TASKCMPLT ever fires, and whether the two frozen
      units stay frozen over a long window - three open questions for one run. NOTE the
      observation window is bounded by teardown, so RunSecs must exceed the travel time.
  (a) RPT-FIRST, NO NATIVE WORK. VR-Forces' own named position reports produced the oracle
      contradiction. Cheap, keeps a working pipeline. CAVEAT the audit raised: RPT is only
      ~3 fixes per unit per run, far coarser than POS, and cannot resolve the oscillation
      POS reports - so it is a cross-check, not a replacement oracle.
  (b) RE-ATTEMPT THE NATIVE raw-vs-DR DIAGNOSTIC - log lastSetLocation() (raw,
      baseEntityStateRepository.h:118) beside location() (approximator-extrapolated, :113).
      Highest value if it lands - it would settle whether every past negative movement
      result is trustworthy - but it broke the pipeline twice on 2026-07-19. Read the
      native rules above before touching it, and note the UB is STILL IN THE BRIDGE.

*** RULES IF YOU DO NATIVE WORK - THESE ARE NOT OPTIONAL. ***
A DIAGNOSTIC MUST NOT CHANGE THE BEHAVIOUR OF THE THING IT OBSERVES. The 2026-07-19
regression happened because the change made Start() register extra controller callbacks
UNCONDITIONALLY for every consumer including the production app. An executor FLAGGED that
in review and the supervisor accepted it anyway.
1. Additive only - new function; do NOT widen vrf::Geodetic (five consumers share it).
2. OPT-IN only - nothing new on the default Start() path.
3. Validate against this four-run table BEFORE trusting any build:
       bridge      run        "No backends"  MoveAlongRoute  POS lines
       A48ABE6C    144109Z          0              3           4512
       A48ABE6C    161438Z          0              3           4512
       61FE865C    185814Z          0              0              1   (WatchVrf crashed)
       4FF31875    193252Z          6              0              0   (creation FAILED)
       A48ABE6C    202349Z          0              3           4464   (rollback confirmed)
4. Back up all 7 VrfBridge.dll copies first - NONE are committed, so rollback means
   rebuilding from a commit, not git checkout of a binary.
5. Redeploy to all 7 consumer dirs and confirm ONE hash. They were THREE different builds
   before being unified on 2026-07-19.
6. Build from PowerShell (git-bash mangles /p:) with VS18 Community MSBuild, ALWAYS
   /t:Rebuild.

TRAPS THAT PRODUCE FALSE GREENS ON THIS MACHINE - do not rediscover them:
- Start-Process -Wait waits for the process AND ALL DESCENDANTS, so it can NEVER return
  from a stage that launches VR-Forces. Cost a 47-minute deadlock. Use -PassThru +
  $p.WaitForExit(ms). Wait-Process waits only for the named process.
- A plain MSBuild of VrfBridge returns EXIT=0 in ~2 s having compiled NOTHING.
- grep -P SILENTLY DEGRADES on this locale; `grep -P ... || echo CLEAN` fires on the ERROR.
  Do ASCII checks with a PowerShell byte scan.
- Parser::ParseFile(path, [ref]tokens, [ref]errors) - swapping the last two reports
  thousands of "parse errors" that are tokens.
- [IO.File] uses the PROCESS cwd, not Set-Location. It once aimed a write at the FROZEN
  C++ ORACLE repo. Pass absolute paths.
- $PID is read-only. A Where-Object CommandLine -like '*needle*' query matches ITS OWN
  process; build the needle at runtime.

OPERATIONAL STATE:
- ONE BUTTON:  pwsh -File scripts\RunC2SimScenario.ps1 -RunSecs 900   <- 900, NOT 120. 120 is what produced the ~145 s window and the retracted reading.
                   [-Init <xml>] [-Order <xml>] [-ConsoleLogDir console] [-DryRun]
  Defaults to data/R9_Mojave_Lean_Initialization.xml + data/R9_Mojave_UnitMove_Order.xml.
  USE THE LEAN INIT (6 units) not R9_Mojave_Initialization.xml (158). -DryRun prints the
  whole plan, creates nothing, contacts nothing and does NOT advance the ledger marker.
  Exit codes: 0 ok / 2 validation / 3 failed after VR-Forces was up / 4 teardown incomplete
  / 5 unexpected. A CRASHED ORACLE NOW FORCES 3 - it used to report success.
- VR-FORCES LAUNCH is unattended and verified. TEARDOWN IS PARTLY VERIFIED - read this
  precisely, an audit caught the first version overstating it:
    VERIFIED: the back-end graceful-close fallback (-BackEndCloseTimeoutSec). On run
      222134Z the GUI quit did NOT carry the back-end, the fallback fired, the back-end
      exited inside 30 s, nothing was force-killed, RTI preserved, StopVrf EXIT=0.
    NOT VERIFIED: the NESTED-DIALOG handling. A "Session Status - Close current terrain?"
      modal is a DESCENDANT of the main window, not a top-level one, so StopVrf's original
      top-level search could not see it and it hung a teardown once (run 193252Z). The fix
      scans descendants - but on the only run since, the modal DID NOT APPEAR, and there
      were NO nested-window log lines at all, so there is not even evidence the scan RAN.
      IT IS INTERMITTENT. Expect it to hang again until you see `answered No to "Session
      Status"` in a real teardown log.
    ALSO CORRECTED: it does NOT fire on "every" clean teardown - FOUR runs tore down cleanly without it -
      161438Z, 185814Z, 202349Z and 222134Z. (An earlier version listed three and omitted
      222134Z, the only run that exercised the current teardown code.) It appeared on a run that FAILED.
- *** NEVER KILL rtiAssistant / rtiexec / rtiForwarder. *** A modal ON the assistant was
  DISMISSED via UIA, not killed, and the assistant was then confirmed healthy by a
  ResetVrf --dry-run. UIA-drivability is NOT predictable per process - the same process
  owns one dialog with a full UIA tree and one without. ENUMERATE, never predict.
- PRE-FLIGHT: inventory processes first (RUNBOOK 0.5.0). A leftover VR-Forces HARD-BLOCKS
  LaunchVrf; -AllowExistingVrf is the documented false-READY trap. Tear it down with
  StopVrf.
- BACKEND HEALTH = THREAD COUNT (blocked 2-4; healthy 23-70 observed). Process presence is
  NOT health.
- ORACLE PRE-CHECK (RUNBOOK 0.5.7): PASS = a POS line with REAL lat/lon, retry up to
  ~3 min. NOTE THE 2026-07-19 CORRECTION: the claim that "the TropicTortoise baseline
  objects are POSITIONLESS" is *** THIRD ATTEMPT AT THIS CORRECTION; THE FIRST TWO WERE BOTH WRONG. Re-derived from the
.oob AND from every trace, 2026-07-20. AUTHORED positions: GlblTerrDmg (d39a55ad) and
GlobalEnv (f864e51f) at ECEF (6378137,1,1) = null island; Page-In Area (cde66adc) at
34.615N/-116.55W, a REAL position. REFLECTED values, which is what matters:
  d39a55ad  NEVER APPEARS IN ANY TRACE AT ALL - 0 samples. That is why readable=2 of
            reflected=3. Its position has never been read because it never reflects.
  f864e51f  1388 samples, TWO distinct values: "NaN,-90.000000,NaN" and
            "0.000000,-90.000000,6.4e72". NEVER 9e-6. Its authored null-island position
            has NEVER been read either - the readings are garbage, not a faithful null.
  cde66adc  1390 samples, FOUR distinct values for a STATIC object: 90/-90/0.0,
            NaN/-90/NaN, 0.000001/-90/1.02e15, 0.000001/-90/6.4e72.
SO: the bad cast corrupts BOTH readable objects, not one. An earlier version of this
correction said "2 of 3 are genuinely positionless as claimed, only the Page-In Area is
cast-corrupted" - THAT IS FALSE. Neither readable object's true position has ever been
seen, and "reflects as 90/-90" holds in only 2 of the 5 observed value-forms.
CONSEQUENCE FOR THE NATIVE RE-ATTEMPT: a control-object-aware accessor is needed for BOTH
readable objects, and a NaN row from GlobalEnv must NOT be read as correct-and-expected. *** The earlier wording here called it simply an ARTEFACT and that overstated it -
  their real positions have never been read. The gate now also checks ALTITUDE SANITY and
  rejects an equator/null-island placeholder.
- C2SIM server docker: REST http://127.0.0.1:8080/C2SIMServer -> HTTP 200, STOMP 61613.
  MAK license expires 2026-09-15.
- TIMESTAMP GOTCHA: logs stamp UTC, machine runs local (-04:00). Record both.
- Non-negotiables: never force-kill a JOINED federate; every appNo from the SINGLE
  "*** NEXT FREE:" marker in OPUS_EXECUTION_PLAN.md Appendix B, LEDGERED BEFORE the join,
  read from the MARKER and never from prose. Unconsumed numbers are BURNED. This includes
  the app's own Vrf__ApplicationNumber (the runner handles it via env override).

WHAT IS RULED OUT - DO NOT RE-INVESTIGATE (each closed with evidence 2026-07-19):
wrong or buried spawn (creation is EXACT to 6 dp; ground clamp works); missing route;
missing task issue; lying completions (none are emitted - BUT this is WEAK evidence, not a closed question: nothing could have been observed past ~180 s anyway); stale aggregate reads hiding
real movement (1.BdeHQ is an ENTITY with no members and moved zero bits); scenario-injected
behaviour or AI-capable templates (TropicTortoise .pln is a 36-byte EMPTY header - this
also CLOSES the groundwork 0.5 ".pln unparsed" gap; .spt scripts count=0; taskRules/ and
scriptedObjectMovement/ are empty ONLY IN THE C2simEx LAYER; Bogaland2 is the inert
ancestor).
*** MODEL-SET DEFAULT BEHAVIOUR IS **NOT** RULED OUT - RETRACTED 2026-07-19. C2simEx.sms
includes EntityLevel.sms, whose taskRules/ holds default-task-rules.tsk, doctrines.dct and
actionCategories.tsk, and whose scriptedObjectMovement/ holds 19 files. NOBODY HAS OPENED
ANY OF THEM. The SCENARIO half above is solid; the MODEL-SET half was checked in the wrong
directory layer. ***

FINDINGS THAT OVERTURNED DOCUMENTED "FACTS" - keep them, they are implementation-independent:
- RUNBOOK sec 7's "RTTI across the MAK DLL boundary" story is FALSE. MAK's own header
  reflectedExtAggregate.h:15-19: under DtHLA the class deliberately derives from
  DtReflectedObject, so a null dynamic_cast<DtReflectedAggregate*> is CORRECT behaviour.
- The blind static_cast worked on aggregates only through ACCIDENTAL VTABLE SLOT ALIGNMENT;
  on a CONTROL object the same slot is a disjoint branch, which is why location() returned
  garbage there while lastSetLocation() faulted.
- "Baseline objects are positionless": *** THIRD ATTEMPT AT THIS CORRECTION; THE FIRST TWO WERE BOTH WRONG. Re-derived from the
.oob AND from every trace, 2026-07-20. AUTHORED positions: GlblTerrDmg (d39a55ad) and
GlobalEnv (f864e51f) at ECEF (6378137,1,1) = null island; Page-In Area (cde66adc) at
34.615N/-116.55W, a REAL position. REFLECTED values, which is what matters:
  d39a55ad  NEVER APPEARS IN ANY TRACE AT ALL - 0 samples. That is why readable=2 of
            reflected=3. Its position has never been read because it never reflects.
  f864e51f  1388 samples, TWO distinct values: "NaN,-90.000000,NaN" and
            "0.000000,-90.000000,6.4e72". NEVER 9e-6. Its authored null-island position
            has NEVER been read either - the readings are garbage, not a faithful null.
  cde66adc  1390 samples, FOUR distinct values for a STATIC object: 90/-90/0.0,
            NaN/-90/NaN, 0.000001/-90/1.02e15, 0.000001/-90/6.4e72.
SO: the bad cast corrupts BOTH readable objects, not one. An earlier version of this
correction said "2 of 3 are genuinely positionless as claimed, only the Page-In Area is
cast-corrupted" - THAT IS FALSE. Neither readable object's true position has ever been
seen, and "reflects as 90/-90" holds in only 2 of the 5 observed value-forms.
CONSEQUENCE FOR THE NATIVE RE-ATTEMPT: a control-object-aware accessor is needed for BOTH
readable objects, and a NaN row from GlobalEnv must NOT be read as correct-and-expected. ***

TOOL INVOCATIONS - VERIFY AGAINST --help/SOURCE; THIS LIST GOES STALE (it did, in a day).
All joiners run from cwd C:\MAK\vrforces5.0.2\bin64 with the RTI env of RUNBOOK sec 7, and
every one that JOINS needs its own FRESH LEDGERED appNo:
    WatchVrf.exe <appNo> <durationSecs> <sampleSecs> [federation]
    *** THERE IS NO --console-log-dir FLAG. It went out with the native revert (5d14eda).
    WatchVrf rejects ANY unknown flag on the live path with EXIT 2, so passing it burns a
    ledgered appNo and kills the stage. Verified: EXIT=2 against the deployed binary. ***
    WatchVrf.exe --con-selftest                          # offline; loads no native DLL
    CreateOne.exe <appNo>
    SetSimRate.exe <multiplier> <appNo>
    ResetVrf.exe <appNo> [--dry-run]
    PushInit.exe  <init.xml>  [restUrl] [stompUrl] [--verbose]
    PushOrder.exe <order.xml> [seconds] [restUrl] [stompUrl]
    StopIface.exe <restUrl> <stompUrl> (--yes | --dry-run)
*** StopIface CHANGED 2026-07-19: it used to ACT WITH NO ARGUMENTS and drove a live server
RUNNING -> UNINITIALIZED during a usage probe. It now REQUIRES both endpoints AND --yes,
has NO defaults, and verifies the server actually reached UNINITIALIZED. ***
Contract across all tools: 0 success / 1 operational failure / 2 usage error, NOTHING done.

TRACE FORMAT (WatchVrf, one line type per record; POS is byte-stable, the rest are additive):
    POS,<t>,<uuid>,<lat>,<lon>,<alt>              the movement oracle - SEE THE CAVEAT ABOVE
    RPT,<t>,"<text>"                              VR-Forces' own reports; carry MARKING TEXT
    TSK,<t>,"<marking>","<taskType>"              task completions; NEVER YET SEEN LIVE
    CON,<t>,<uuid>,<level>,<msg>                  object console; NEVER YET SEEN LIVE
    *** BCON and CONARM DO NOT EXIST - removed from this list 2026-07-19 late. They went
    out with the native revert (5d14eda). tools/WatchVrf emits POS, CON, TSK and RPT and
    nothing else. CONARM lines DO appear in runs 185814Z and 193252Z - artifacts of the
    reverted build - which makes the phantom look corroborated. Do not budget a diagnostic
    that waits for a BCON line; its absence would prove nothing. ***
0.6 CONSOLE CAPTURE IS NOT DONE: its live gate ran and returned ZERO CON lines. The path is
wired and registered; the suspect is the TRANSPORT (Comment PDU/Interaction). TRAP: the
GUI's yellow badge is NOT evidence of wire delivery - the GUI reads a different channel.

PENDING USER DECISIONS (do NOT decide these yourself; none block the next action):
- Q1 hostile-side country: (a) USA-225 as today, (b) RUS-222 mirrors, (c) author Country-45.
  VERIFIED: 11 Chinese platforms on disk, air/naval/AD ONLY - ZERO ground-combat platforms,
  ZERO Chinese aggregates. If exploratory, (b) is clearly right.
- ArmorCoHQ Decision 4: A (one-field match fix) vs B (Tank Headquarters Section,
  militarily correct). Supervisor recommends B.
- Golden-order file identity for Phase-5 scoring.
- AGGREGATION-STATE POLICY - a supervisor inferred entity-level units should run
  DISAGGREGATED where the verb needs combat. The user never said it. Flagged as over-read.

WHICH REPO YOU ARE IN - CHECK FIRST, TWO REPOS HAVE SIMILAR NAMES AND OPPOSITE RULES.
Work happens in the PORT repo VRF_C2SIM (.../Software/Interfaces/VRF_C2SIM, branch main).
The C++ repo c2simVRFinterfacev2.36 is a FROZEN ORACLE - do not develop there. Your shell
may open showing the C++ repo's branch; that is not where you work. Confirm with
`git remote -v` (origin must be hyssostech/VRF_C2SIM).

BUILDING (nothing here needs VR-Forces running):
    dotnet build tools\<Name>\<Name>.csproj -c Release
Built exes land under bin\Release\<tfm>\... and the TFM VARIES (most net10.0/win-x64, some
plain net10.0), so locate with Get-ChildItem -Recurse rather than hard-coding a path.

START by reporting: git log --oneline -5 + git status -sb of the PORT repo (expect a clean
tree; an untracked *.code-workspace is known and ignorable); confirmation you read
HANDOFF_2026-07-19.md and the HEADLESS MANDATE; the current "*** NEXT FREE:" value READ
FROM THE MARKER; and a process inventory (RUNBOOK 0.5.0). Then state which of the two
paths above you propose and ask for the go-ahead in the same message. Get explicit user
go-ahead before ANY live work. Committing GATED, GREEN work does not need one.
