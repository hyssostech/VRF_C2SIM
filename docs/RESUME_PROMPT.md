# RESUME PROMPT (2026-07-16 evening handoff - the create-underground breakthrough)

Paste the block below into a fresh session. It supersedes all earlier resume prompts.

---

Resume the C2SIM VR-Forces -> .NET port in SUPERVISOR MODE (user-directed standing model):
YOU supervise - design and gate probes, adjudicate evidence adversarially, keep docs current
AS work lands, coordinate the user's in-the-loop steps - while Opus (or lower) EXECUTOR
agents do the actual work (code, analysis, reading, run execution). Run an adversarial
refuter pass on any load-bearing claim before accepting it. Pre-register every probe (one
variable; prediction + falsifier written BEFORE running). Movement claims REQUIRE WatchVrf
displacement - completions LIE (VRF-sourced vacuous completions exist in BOTH interfaces).

WHERE THE WORK LIVES: port repo VRF_C2SIM (submodule at OpenC2SIM.github.io/Software/
Interfaces/VRF_C2SIM, branch main). READ IN ORDER before touching anything:
(1) docs/SUPERVISED_RECOVERY_PLAN.md - THE plan of record; sec 3b is the immediate work item.
(2) docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md parts 13 + 13b (the
    2026-07-16 breakthrough - read IN FULL), then parts 9-12 (audit verdicts + killed
    hypotheses - do NOT re-litigate anything on that list).
(3) docs/START_HERE.md status banner; docs/RUNBOOK.md secs 0 / 0.5 / 0.6 / 7 before any live
    run; docs/OPUS_EXECUTION_PLAN.md Appendix B (appNo ledger - NEXT FREE: 3442).

THE STATE: the entity-freeze ROOT CAUSE is FOUND, LIVE-VERIFIED, CODE-VERIFIED, and
AUTHOR-CORROBORATED - ground units are created UNDERGROUND at high-elevation terrain. The
interface (original C++ and port parity mode alike) pushes fixed-MSL altitudes: create pos =
ElevationAgl (default 1000, no init file ever carries the field) MSL; deferred post-create
SetAltitude = 1001 MSL; Fixed100 route vertices = 100 MSL. Above ground at sea-level
Bogaland (VRF clamp fixes it silently - why it always "worked" there); ~130-1030 m BELOW
terrain at Mojave's ~1131 m. VR-Forces accepts tasks on a buried object (Active, speed 0)
but never executes movement - a rule the ORIGINAL AUTHORS DOCUMENTED
(VRFadditionalFiles/README.txt: elevation AGL <= 0 "will not execute a route"). Live proof:
native GUI tasking failed identically on the frozen entity; the user DRAGGED it onto the
surface and it then moved - and even executed the interface's OWN previously-"failed" route
cleanly. Separately proven reproducible: GroundWaypointAltitudeMode=Live (live-ground route
vertices) produced textbook ~1.16 km arrivals (8 m from final waypoint) twice back-to-back
(P-C1). The RUNAWAY class (movers driving 49-135 km past routes, terminating underground/
offshore) is the same disease's other face.

THE IMMEDIATE TASK - implement the fix (SPECIFIED in plan sec 3b, NOT yet built), via an
Opus executor with adversarial review before merge:
- UnitTranslator.cs stays BYTE-PARITY UNTOUCHED (ported oracle; selftests pin it).
- Service-layer change gated on the existing Vrf:GroundWaypointAltitudeMode setting:
  Fixed100 = byte-parity today (escape hatch); Live = THE NEW DEFAULT, and under Live the
  create path must never emit a below-terrain altitude. Leading suspect to settle with
  evidence: the deferred SetAltitude(1001, TRUE) forcing the already-ground-clamped object
  back underground (reflected create positions DO clamp - telemetry showed terrain height) -
  likely skip or live-clamp that call by reading the reflected geodetic first
  (TryGetEntityGeodetic), guarding against the KNOWN ~30 s degenerate-position transient on
  aggregates ((0,0,-6378137) - never clamp to a degenerate reading; retry/skip + warn). Check
  what setAltitude's third arg (TRUE) means in the MAK headers before deciding.
- OFFLINE acceptance: build 0 errors; all 8 selftests green UNCHANGED (paths/commands in
  START_HERE "Run / verify"; exe path has the win-x64 RID subfolder).
- LIVE acceptance (supervisor-gated; user must launch VR-Forces - agent launch is CONFIRMED
  UNSAFE): re-run data/R9_Mojave_Lean_Initialization.xml + data/R9_Mojave_UnitMove_Order.xml
  on TropicTortoise with the fix and NO manual drag - ALL units (1.BdeHQ entity + both
  aggregates) must move, telemetry-verified (WatchVrf), fresh appNos from the ledger.

STEP 0 BEFORE ANYTHING LIVE: a port app (appNo 3439) MAY still be running from the handoff
session with live units the user was experimenting on - check for a VrfC2SimApp process; if
present, confirm with the user, then clean-stop via tools/StopIface (note Solution A deletes
its created units on stop). C2SIM server was left RUNNING.

OPEN ITEMS AFTER THE FIX (plan secs 3/4, in rough order): (a) 114.MechCoy "position NEVER
publishes" - a DISTINCT signature from the entity freeze (degenerate (0,0) forever, vs the
platoon's transient that resolves in ~30 s); the altitude fix may or may not cure it - treat
as its own tracked question with its own probe. (b) TRUTHFUL-ARRIVAL GATE (staged): VRF
completions must never advance the TaskSequencer or produce outward TASKCMPLT without
center-displacement verification (documented tolerances: entity at-distance ~1 m, unit
in-position 0.2 m; unit completion = formation LEADING EDGE reaching last vertex - premature
by design). One pristine-C++ completion fired 325 ms after dispatch on a never-moved unit and
falsely advanced the DAG. (c) Runaway containment (route-end stop / AO-exit halt). (d) P-B
force-side flip at Sweden (COA-STP1's own units freezing at Sweden remains UNEXPLAINED -
force-side is the last untested categorical difference; flip a proven golden marcher to
WASA/hostile, single variable). (e) P1b port-vs-C++ same-backend A/B (controls:
AggregateFormation="", PredecessorTimeoutPolicy=force - 31 of COA-STP1's 42 tasks are
predecessor-gated and the port's default skip policy drops subtrees the C++ dispatches).
(f) MAK support question - drafted in investigation doc part 12; only send if the pile
mover/frozen split still matters after the fix.

DO-NOT-RELITIGATE LIST (all falsified/settled with evidence - investigation doc parts 9-12):
overlap-footprint slowdown (parameter not even loaded in the C2simEx/EntityLevel SMS chain),
nav data, terrain page-in, DIS type, formation names, pile-size-as-sufficient, template
quality (RUN B inverted it), echelon, member/creation structure, name-length collisions
(zero exist in any dataset; the "10-char VRF limit" is the C++'s OWN parse-time truncation -
the port handles full names fine, do NOT "fix" it). The pristine C++ baseline is MEASURED:
zero correct arrivals on COA-STP1; its movers were lone icons (members left behind); it also
CANNOT clean-stop against the current server (hardcoded protocol 1.0.1 discards the stop
broadcast) - end its runs with VR-Forces close + user-approved process kill. "The original
works better" is falsified in both directions.

NON-NEGOTIABLES (unchanged): never force-kill a joined federate without user approval
(StopIface is the clean stop); never push init to a running app; fresh appNo per join,
ledgered in Appendix B; VR-Forces launches are USER-only via GUI/vrfLauncher; RTI 4.6.1 +
Machine-scope license + cwd bin64 + --contentRoot for live runs; XML gotchas per RUNBOOK 0.6
(prolog comments break init push; ANY block comment breaks order STOMP delivery); keep
START_HERE / the investigation doc / this RESUME_PROMPT current AS work lands; after any
context compaction re-read the plan doc before deciding anything. C++ repo: master = pristine
191933a, Phase-1 work on branch phase1-vrffacade-extraction in a sibling worktree - do not
develop on the C++ repo.

START by reporting: git state of port + fork (git log --oneline -3, git status -sb), the
eight-selftest baseline, whether app 3439 / VR-Forces / the C2SIM server are still up (do
not assume), and your executor tasking plan for the sec-3b fix - then get the user's
go-ahead before the executor touches code.

---
