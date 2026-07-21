# RESUME PROMPT (rewritten clean 2026-07-21)

Paste the block below into a fresh session. Every statement here is present-tense and
current; it was rewritten from a re-verified fact base to remove seven rounds of
correction-on-correction. The provenance of every superseded claim lives in
docs/CORRECTIONS_LOG.md - read that ONLY if you need to know what was previously wrong.
Keep THIS file free of retraction history: when something changes, state the new truth and
move the old claim to the log. ASCII only.

---

Resume the C2SIM -> VR-Forces interface effort in SUPERVISOR MODE: you supervise, gate and
adjudicate; executor agents do the reading, analysis and code. Re-run every executor's
acceptance check with your own hands before accepting any load-bearing claim - and re-derive
any number from the run artifacts rather than trusting a document, including this one.

## WHAT THIS IS

A HEADLESS interface. A C2SIM init + order document goes in; units are created, tasked and
run in VR-Forces; the outcome is scored from telemetry. ONE command, zero humans in the UI.
User's words (2026-07-18): "I click a button, my C2SIM plan plays on its own."

GUI use is DIAGNOSTIC ONLY. The sole GUI automation on the product path is simulator
lifecycle - StopVrf.ps1 answers VR-Forces' quit modal via UIA so teardown is unattended.
If you find yourself planning to drive the Create/Task GUI, or telling the user something
needs them to click, STOP.

## STATE - WHAT IS TRUE RIGHT NOW

THE LOOP WORKS. scripts/RunC2SimScenario.ps1 takes a C2SIM init + order through launch ->
interface join -> unit creation -> tasking -> telemetry -> clean resign, unattended. Three
fully unattended end-to-end runs: 20260719T161438Z, 202349Z, 222134Z.

THE UNITS DO NOT COMPLETE THEIR ROUTES, and the shape of that is now precise:
- 114.MechCoy and 1.BdeHQ: FROZEN. lat/lon bit-exact across every sample of every run
  (one distinct value across 76/77 samples). This is the primary defect. It needs no more
  observation time.
- 1222.MechPlt: MOVES. A measured ~174 m at ~1.4-1.5 m/s (net 174.1 / 174.4 / 168.4 m across
  the three runs), still moving when telemetry coverage ended. Whether it ARRIVES is
  UNTESTED - never observed long enough. The route is ~1157 m, needing ~825 s at that speed;
  no run has observed more than ~145 s.
- No TASKCMPLT has been observed. This is WEAK evidence: nothing is observable after the
  interface resigns (~t=180 s), so a completion at t=400 would be invisible too.

THE MOVEMENT ORACLE IS IN QUESTION - this outranks the movement result. Two telemetry
channels disagree about 1222.MechPlt's DIRECTION:
- RPT (VR-Forces' own position reports, 3 fixes/unit/run): moves EAST toward the objective.
- POS (WatchVrf's dead-reckoned read): 63 m WEST of start, then a stationary sawtooth.
The control that makes this serious: on the two FROZEN units the channels agree EXACTLY.
They agree on stationary objects and disagree only on the moving one - a tracking failure,
not two noisy sources. ESTABLISHED: they contradict, with a control. NOT PROVEN: which is
truthful (one POS sample at t=159.9 in run 161438Z briefly matches RPT to 4.6 m, suggesting
RPT is right, but that is n=1). POS displacement is the standing "only movement oracle", so
this shadow falls on every past negative movement result including the runaway/warp census.

## NEXT ACTION

    pwsh -File scripts\RunC2SimScenario.ps1 -RunSecs 900

Free, never done, and it settles three questions in one run: does 1222.MechPlt ARRIVE, does
a TASKCMPLT ever fire, and do the frozen units stay frozen over a long window. Use 900+ (the
route needs ~825 s of motion; the observation window is ~RunSecs + 62 s). -RunSecs 120 is
what produced the ~145 s window - do not use it for a scored run.

CAVEAT: 4a ARRIVAL is scored on the POS trace, the channel that misreports this unit. If POS
is still broken, the long run answers "does RPT show arrival" and "did TASKCMPLT fire" but
cannot produce a 4a-scored POS arrival. Capture RPT and reason from both channels.

## HOW TO RUN / OPERATIONAL FACTS

- ONE BUTTON: pwsh -File scripts\RunC2SimScenario.ps1 -RunSecs 900 [-Init <xml>] [-Order <xml>] [-DryRun]
  Defaults to data/R9_Mojave_Lean_Initialization.xml (6 units, 3 taskees) + data/R9_Mojave_UnitMove_Order.xml
  (3 MOVE tasks). Do NOT use data/R9_Mojave_Initialization.xml (158 units) - not comparable
  to the scored runs. -DryRun prints the full plan, creates nothing, contacts nothing, does
  NOT advance the ledger. Runner exit codes: 0 ok / 2 validation / 3 failed after launch /
  4 teardown incomplete / 5 unexpected.
- LAUNCH is unattended and verified: scripts/LaunchVrf.ps1. TEARDOWN (StopVrf.ps1) is unattended
  but NOT fully reliable - 2 of 6 teardowns failed (exited 3, nothing force-killed), and the
  GUI quit has never once carried the back-end, so the graceful back-end-close fallback is the
  normal path, not the exception. Budget for a teardown that may need a hand.
- BACKEND HEALTH = THREAD COUNT (blocked 2-4; healthy ~23-70). Process presence is not health.
- ORACLE PRE-CHECK (RUNBOOK 0.5.7): PASS = a POS line with real lat/lon (not NaN, not the
  90/-90 pole), retry up to ~3 min. The gate also checks altitude sanity and rejects the
  equator/null-island placeholder.
- C2SIM server (docker): REST http://127.0.0.1:8080/C2SIMServer HTTP 200, STOMP 61613.
  MAK license expires 2026-09-15.
- Logs stamp UTC; the machine runs local (-04:00). Record both before comparing times.

## NON-NEGOTIABLES

- NEVER force-kill a JOINED federate. NEVER kill rtiAssistant / rtiexec / rtiForwarder.
- Every appNo comes from the SINGLE "*** NEXT FREE: <n> ***" marker in
  OPUS_EXECUTION_PLAN.md Appendix B, searched BY THAT FORM (decoys exist), LEDGERED BEFORE
  the join. Unconsumed numbers are BURNED, never recycled. Includes the app's own
  Vrf__ApplicationNumber. CURRENT VALUE: 3547.
- ASCII only in tracked files (verify with a PowerShell byte scan; grep -P silently degrades
  on this locale and reports a false clean).
- The C++ repo c2simVRFinterfacev2.36 is a FROZEN ORACLE - never develop there. Its checked-out
  branch is a probe, not master. You work in the PORT repo VRF_C2SIM (origin
  hyssostech/VRF_C2SIM, branch main); confirm with `git remote -v`.

## RULED OUT - do not re-investigate (each closed with evidence)

Wrong or buried spawn (creation is exact to 6 dp); missing route; missing task issue; stale
aggregate reads hiding motion (1.BdeHQ is an entity with no members, moved zero bits);
scenario-injected behaviour (TropicTortoise .pln is a 36-byte empty header; .oob defines 3
control objects with empty task lists; Bogaland2 is the inert ancestor).

STILL OPEN, do NOT treat as ruled out: MODEL-SET DEFAULT BEHAVIOUR. C2simEx.sms includes
EntityLevel.sms, whose taskRules/ holds default-task-rules.tsk + doctrines.dct and whose
scriptedObjectMovement/ holds 19 files. None has been opened. Also open: whether a TASKCMPLT
ever fires (see NEXT ACTION); the two-channel oracle contradiction.

## IF YOU DO NATIVE WORK (src/VrfFacade, src/VrfBridge)

Authorized by the user (2026-07-19) when genuinely required. Rules, learned from a pipeline
break: A DIAGNOSTIC MUST NOT CHANGE THE BEHAVIOUR OF THE THING IT OBSERVES. Additive only;
OPT-IN only, nothing new on the default Start() path; validate a new build against the
five-run bridge table in HANDOFF before trusting it; back up all 7 VrfBridge.dll copies
first (none are committed) and redeploy to all 7, confirming one hash. Build from PowerShell
with VS18 Community MSBuild, ALWAYS /t:Rebuild (a plain build exits 0 having compiled nothing).
The blind static_cast at VrfFacade.cpp:735 that crashes on control objects is STILL PRESENT
(the fix went out with revert 5d14eda) - the raw-vs-DR oracle test is un-built, not shipping.

## TRAPS (each cost real time; all verified)

- Start-Process -Wait waits for the process AND ALL DESCENDANTS - it never returns from a
  stage that launches VR-Forces. Use -PassThru + $p.WaitForExit(ms).
- A plain MSBuild of VrfBridge exits 0 in ~2 s having compiled NOTHING. Use /t:Rebuild.
- grep -P silently degrades on this locale (false clean). Byte-scan in PowerShell.
- Parser::ParseFile(path, [ref]tokens, [ref]errors) - swapping the last two reports tokens as errors.
- [IO.File] uses the process cwd, not Set-Location; pass absolute paths.
- WatchVrf has NO --console-log-dir flag (went out with revert 5d14eda) and rejects any
  unknown flag on the live path with exit 2. Do not pass it.

## PENDING USER DECISIONS (do not decide yourself; none block the next action)

- Q1 hostile-side country: keep USA-225 / RUS-222 mirrors / author Country-45. 11 Chinese
  platforms on disk are air/naval/AD only, zero ground-combat.
- ArmorCoHQ mapping A vs B (supervisor recommends B).
- Golden-order file identity for Phase-5 scoring.
- Aggregation-state policy (a supervisor inferred disaggregate-for-combat; user never confirmed).

## START BY REPORTING

git log --oneline -5 and git status -sb of the PORT repo (clean tree expected bar an
untracked .code-workspace); the "*** NEXT FREE:" value read from the marker; a process
inventory (RUNBOOK 0.5.0). Then propose the next step and get an explicit go-ahead before
any live work. Committing gated, green work does not need one.
