# RESUME PROMPT (2026-07-16 late-evening handoff - GROUNDWORK PHASE)

Paste the block below into a fresh session. It supersedes all earlier resume prompts.

---

Resume the C2SIM VR-Forces -> .NET effort in SUPERVISOR MODE (user-directed standing
model): YOU supervise - design and gate probes, adjudicate evidence adversarially, keep
docs current AS work lands - while Opus (or lower) EXECUTOR agents do the work (code,
analysis, reading, runs). Adversarial refuter pass on load-bearing claims before
acceptance. Pre-register every probe (one variable; prediction + falsifier written
BEFORE running). Movement claims REQUIRE WatchVrf displacement - completions LIE in both
directions (instant-vacuous AND absent).

WHERE THE WORK LIVES: port repo VRF_C2SIM (submodule at OpenC2SIM.github.io/Software/
Interfaces/VRF_C2SIM, branch main - run git log --oneline -3 for the tip; unpushed
commits are expected). READ IN ORDER:
(1) docs/VRF_GROUNDWORK_PLAN.md - THE plan of record (2026-07-16 user-directed reset:
    learn native VRF first, then rebuild the creation/mapping layer onto REAL installed
    types, as close to real types as possible).
(2) docs/VRF_GROUND_TRUTH.md - the Phase 0 deliverable (read 0.0 supervisor
    cross-findings FIRST, then 0.1 content catalog / 0.2 semantics curriculum /
    0.3 remote-API audit as needed).
(3) docs/SUPERVISED_RECOVERY_PLAN.md - evidence record + standing rules (telemetry
    oracle, probe discipline, appNo ledger); its sec 3/3c probe ordering is RETIRED.
(4) docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md parts 13-16 (the
    2026-07-16 fix-session arc: 13c header evidence, 14 FIX-ACCEPT-1, 15 COA-DEMO-1,
    16 CPP-ALT-1) as needed; RUNBOOK secs 0/0.5/0.6/7 before any live work.

STATE (2026-07-16 late evening): the buried-birth altitude root cause is FIXED and
LIVE-ACCEPTED in the port (FIX-ACCEPT-1 prediction P1: all lean-set units arrived with
NO drag; GroundWaypointAltitudeMode=Live is the DEFAULT, ground units born at safe
10000 MSL + parity SetAltitude skipped; Fixed100 = byte-parity escape hatch) and
confirmed CODE-INDEPENDENT via a single-constant pristine-C++ probe (C++ repo branch
probe/create-altitude-above-ground b96688b; master stays pristine 191933a - do not
develop there). Proven ALTITUDE-EXONERATED and still open: the runaway/warp class and
the pile mover/frozen split (both reproduce in BOTH codebases with above-ground
births). The user's yellow unit badges = VRF Object Console WARNINGS nobody ever read.
COA-STP1 type fidelity is quantified: ~64/128 units mis-map to Tank Company (USA),
~49 hit the generic Ground_Aggregate fallback, ~15 become lone M1A2 entities;
ArmorCoHQ misses a real template by ONE matchType field.

IMMEDIATE NEXT WORK (groundwork plan phases; (a)-(c) fully offline):
(a) BUILD 0.6 CONSOLE-CAPTURE FIRST: facade/bridge wrap of
    addObjectConsoleMessageCallback (vrfRemoteController.h:1970, supervisor-verified;
    delivers uuid + notifyLevel + message text) -> CON,<t>,<uuid>,<level>,<msg> lines
    alongside WatchVrf POS lines. VRF may already explain the pile split and runaways
    in messages we never subscribed to.
(b) Build 0.5 scnx-diff harness (remote saveScenario(...saveToZip) at header:660 is
    confirmed - the backend can save the LIVE scenario for GUI-vs-remote unit diffing)
    and 0.4 vrfLauncher self-launch recipe (probe-gated: launch via vrfLauncher.exe,
    then ResetVrf --dry-run must NOT 0xC0000005, twice; the raw vrfSimHLA1516e CLI
    remains CONFIRMED UNSAFE). The user explicitly directed the agent to learn VRF
    self-launch - stop depending on the user for bring-up.
(c) USER ADJUDICATION pending: nearest-real-type choices for the loaded-chain content
    gaps (NO engineer aggregate, NO composed USA mech-infantry company, NO mortar/
    rocket aggregate) - present options from ground truth 0.1.7 as a review table.
(d) PHASE 1 scripted GUI session (user + supervisor, ~1 hour, next live session):
    native reference baseline at TropicTortoise - OPEN THE OBJECT CONSOLE SUMMARY
    PANEL FIRST and capture every unit warning; palette-create a REAL tank + platoon +
    company (types from the 0.1 catalog, no generics); native-task along COA-shaped
    routes including one leg crossing the 18.4 km radius where CPP-ALT-1's marchers
    stopped; repeat one move at 1x vs 20x (time-multiplier warp hypothesis); SAVE the
    scenario to .scnx. If native units ALSO misbehave -> the MAK support question
    fires immediately with that repro, before anything is rebuilt.
(e) Then Phase 2 gap analysis (real-type mapping table for user line-by-line review;
    GUI-vs-remote scnx structure diff; task vocabulary v2), Phase 3 creation-layer
    rebuild (new Vrf:CreationMode=native; parity mode retained; UnitTranslator
    untouched as the parity oracle), Phase 4 truthful-arrival gate (unit route
    completion = formation LEADING EDGE at last vertex, premature BY DESIGN -
    doc-verbatim; entity moves accept setAtDistance arrival radius) + runaway
    containment, Phase 5 COA-STP1 acceptance scored by displacement only.

PRE-REGISTRABLE HYPOTHESIS ON RECORD (ground truth 0.0 item 2): platoon-echelon
templates wire aggregate-lead-follow-in-formation-controller while company/battalion
templates wire aggregate-move-along-controller - and the live per-class behavior split
(platoon marches+publishes / company mute or halts-short) tracks that controller
boundary exactly. Single-variable probe: same force created as platoon-class vs
company-class real templates, tasked identically.

OPERATIONAL STATE: VR-Forces CLOSED; rtiexec STOPPED; the pristine interface was
killed per the documented teardown (it cannot clean-stop - hardcoded protocol 1.0.1);
C2SIM server docker RUNNING (REST 8080 / STOMP 61613). appNo ledger
(OPUS_EXECUTION_PLAN.md Appendix B): NEXT FREE 3455. License expires 2026-09-15.
TIMESTAMP GOTCHA (cost real confusion 2026-07-16): the app/tool logs stamp UTC while
the machine runs local time - always check Get-Date before comparing timestamps.
Non-negotiables unchanged: never push init to a running app; fresh appNo per join,
ledgered; never force-kill a joined federate without user approval (StopIface = the
port's clean stop; the pristine C++ ends with VRF close + approved kill + rtiexec
restart); RTI 4.6.1 + Machine-scope license + cwd bin64 + --contentRoot for live runs;
XML gotchas per RUNBOOK 0.6; keep the groundwork plan / ground truth / this prompt
current AS work lands; after any context compaction re-read the plan doc before
deciding anything.

DO-NOT-RELITIGATE (recovery plan sec 1 + investigation parts 9-16, all
evidence-settled): altitude as the FREEZE cause (FIXED, both codebases), altitude as
the RUNAWAY cause (EXONERATED, both codebases), nav data, terrain page-in AS THE
FREEZE CAUSE (the paged-tile boundary as the STOP/TERMINATION context is OPEN - user
hypothesis, queued), DIS type, formation names, pile-size-as-sufficient, name-length
collisions (do NOT "fix" the 10-char non-issue), template quality, echelon, member
structure. "The original works better" is falsified in both directions - the pristine
baseline measured ZERO correct arrivals on COA-STP1.

START by reporting: git state of port + C++ repos (git log --oneline -3, git status
-sb), confirmation you read the groundwork plan + ground truth 0.0, and your proposed
execution order for (a)-(d) with executor tasking briefs - then get the user's
go-ahead before any executor touches code.
