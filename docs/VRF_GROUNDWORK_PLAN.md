# VRF GROUNDWORK PLAN (2026-07-16) - learn VR-Forces first, then rebuild the mapping

User directive (2026-07-16, verbatim intent): "This is sounding very random. Draft a plan
to get this interface fixed at long last. This requires doing the ground work of learning
how to actually successfully use vrf... How can C2SIM entities and tasks be successfully
mapped to elements in vrf that will actually work, and remain as close to their real types
as possible?"

THIS SUPERSEDES the probe-queue ordering in SUPERVISED_RECOVERY_PLAN.md sec 3/3c for all
movement work. The standing evidence rules (WatchVrf displacement is the only movement
oracle; pre-registered single-variable probes; adversarial review before acceptance;
fresh appNos from the Appendix B ledger) REMAIN in force. ASCII-only. Keep current AS
work lands.

## 0. The honest diagnosis - why the last week looked random

1. The interface's CREATION layer was never validated against VR-Forces' own content
   model. Verified 2026-07-15: our echelon D/E/F DIS codes match NO specific installed
   unit template - units fall to the generic `Ground_Aggregate.entity` fallback with
   4 anonymous subordinates. "Not close to their real types" is the SHIPPED behavior,
   inherited from the C++ oracle at parity.
2. The authors' own docs bound what they built: "HLA version is believed to work
   properly ... task-follows-task"; "We do not plan to expand c2simVRF to the full
   capabilities of VR-Forces". COA-scale parallel tasking was never in its envelope.
3. What a week of probes DID establish (all telemetry-verified, none wasted):
   - Buried-birth altitude bug: FOUND and FIXED in both codebases (entity-freeze cured;
     the one fully-closed class).
   - Runaway/warp class: CODE-INDEPENDENT, altitude-exonerated, unexplained.
   - Pile mover/frozen split: CODE-INDEPENDENT, altitude-independent, every offline
     discriminator falsified; VRF-internal.
   - Completions: erratic in both directions (instant-vacuous AND absent); neither
     interface can be trusted to report or sequence on them.
   - Native GUI operation of VRF WORKS on this machine (drag-and-retask moved a unit
     our interface could not; palette units task fine).
   Item 3's pattern is the tell: everything still broken is on the OTHER side of the
   remote-control boundary - in how VRF interprets what we create and command. We have
   been debugging our half. The groundwork below is about finally learning THEIR half.

## 1. Goal and definition of done

GOAL: a C2SIM init + order (coa-gpt-shaped) executes in VR-Forces with units that
(a) are created as the CLOSEST REAL installed VRF types (no generic fallbacks),
(b) are structurally indistinguishable from units a human authors in the VRF GUI,
(c) execute route tasks with displacement-verified arrivals, zero runaways, zero
    false task-chain advancement.

DONE-GATE (final): a C2SIM init + order, run end-to-end through the port with ZERO human
interaction, produces telemetry meeting the documented arrival/runaway tolerances - then
COA-STP1 scored the same way. Any "native" control arm needed for comparison comes from a
GUI-authored .scnx loaded programmatically (groundwork 0.5 + loadScenario), NOT from a
human session.
*** CORRECTED 2026-07-18: this gate used to read "the Phase-1 native reference scenario
... matches its own NATIVE-GUI BASELINE telemetry". That made a human-at-the-GUI session
a prerequisite for shipping, and it sat FOUR LINES ABOVE sec 1a which forbids exactly
that. Read sec 1a next - it is the governing statement. ***

## 1a. THE HEADLESS MANDATE - READ THIS BEFORE ANY SESSION PLANNING

*** USER-STATED, 2026-07-18 EVENING, VERBATIM INTENT: "we are building an interface to
vrf, that takes C2SIM doc inputs and initializes, tasks and runs the implied
simulations. Zero humans using the UI to task anything, or to click on terrain, etc.
that is the whole point. I click a button, my C2SIM plan plays on its own." ***

THE PRODUCT IS HEADLESS. C2SIM document in -> units initialized, tasked and executed in
VR-Forces -> outcome verified from telemetry. ONE button. NO GUI in the loop - not a
human's hand on it, and NOT an agent driving it either.

WHY THIS SECTION EXISTS: on 2026-07-18 a supervisor read PHASE1_SESSION_SCRIPT.md's
"the user drives the VR-Forces GUI for creation/tasking steps", treated it as a
CONSTRAINT ON THE EFFORT rather than as scaffolding for one diagnostic, and spent a work
block building UI-Automation menu-driving for VR-Forces. That is off-mission work. The
product never touches the GUI, so automating the GUI is not a route to the goal - it is
a detour away from it. The user had to say so explicitly to stop it.

THE THREE RULES THAT FOLLOW FROM THIS:

1. NO STEP ON THE ACCEPTANCE PATH MAY REQUIRE A HUMAN OR A GUI. If a procedure says
   "the user does X in the GUI", that procedure is scaffolding, not product, and it must
   be labelled as such. A GUI step may NEVER be a prerequisite for a scored run.
2. VERIFICATION WAS NEVER HUMAN WORK AND NEVER WILL BE. Outcomes are decided by
   arithmetic on the WatchVrf trace (POS lines) plus the reports captured by
   ListenReports - expected waypoints vs observed displacement. A human is not required
   to judge whether a run matched its C2SIM input, and never was. Do not re-invent this
   as a manual review step.
3. GUI USE IS DIAGNOSTIC ONLY. It is legitimate to LOOK at the GUI to understand VRF
   behaviour. It is not legitimate for the GUI to be load-bearing in any run that scores
   the product.

WHAT IS *NOT* OFF-MISSION: unattended LAUNCH and TEARDOWN of VR-Forces
(scripts/LaunchVrf.ps1, scripts/StopVrf.ps1). The headless loop needs the simulator to
come up and go down without a human, so that work serves the mandate directly. The
detour was specifically UIA-driving of the Create/Task MENUS, which is abandoned.

## 2. Operating model for this plan

- The VR-Forces GUI is now an INSTRUMENT, not just a viewer. Phase 1 is a joint
  user+supervisor session at the GUI; short, scripted, captured.
  *** BOTH BULLETS IN THIS SECTION ARE SUPERSEDED - see sec 1a and the Status TOP entry.
  Phase 1's GUI method is RETIRED, and 0.4 PASSED so the human-launch dependency is
  gone. Kept for the record; do not act on them. ***
- Executors do the reading/cataloging/diffing offline; the supervisor gates each
  phase's exit criteria; the user adjudicates type-mapping choices (they know the
  military semantics). SUPERSEDED CLAUSE: this bullet used to end "and performs GUI
  steps until the vrfLauncher self-launch recipe (Phase 0.4) removes that dependency."
  0.4 passed on 2026-07-18; VR-Forces launches AND tears down unattended; NO standing
  arrangement assigns GUI work to the user. The user adjudicates MILITARY-SEMANTICS
  questions - that is the only standing user role.
- Every phase produces a durable doc; no findings live only in chat.

## Phase 0 - VRF competence base (offline + docs; no live time needed; start NOW)

Deliverable: docs/VRF_GROUND_TRUTH.md - the things we should have known on day one.

- 0.1 CONTENT CATALOG (executor): enumerate the INSTALLED simulation model set content
  on disk (C:\MAK\vrforces5.0.2\data\simulationModelSets\ - the C2simEx/EntityLevel
  chain we actually load): every aggregate-capable unit template (.entity/.opd), its
  DIS enumeration, echelon, real subordinate composition, formations, and the sysdef
  controllers it wires. Output: the definitive "real types available" table.
- 0.2 DOCS CURRICULUM (executor, systematic - not spot lookups): read and summarize,
  with file paths as citations, the on-disk User's Guide chapters
  (C:\MAK\vrforces5.0.2\doc\help\Content): scenario/organization authoring; aggregates
  and disaggregation; the full ground-movement task vocabulary and their parameters;
  plans and task chaining AS VRF MEANS THEM; ground clamp semantics; terrain paging and
  playbox bounds (feeds the 18.4 km stop-radius question); entity/aggregate status
  ICONS AND WARNING BADGES (identify the yellow triangle - it has been unidentified
  for two sessions); time management (real-time vs scaled and what breaks at 20x).
- 0.3 REMOTE-API SURFACE AUDIT (executor): the FULL creation/organization surface of
  DtVrfRemoteController (vrfRemoteController.h on disk) - not just createEntity/
  createAggregate: organization attachment (addObjectToSuperior etc.), structured/
  scenario-load creation paths (loadScenario, newScenario), task messages vs set-data
  requests, and which of these the GUI itself uses (cross-check with 0.5).
- 0.4 SELF-LAUNCH RECIPE (executor + one probe): script vrfLauncher.exe (NOT the raw
  vrfSim CLI - CONFIRMED UNSAFE) from RUNBOOK sec 0.5's seed; gate: launch, then
  ResetVrf --dry-run must join and read cleanly, twice. Removes the human-launch
  dependency for all later phases. (User-directed 2026-07-16.)
- 0.5 THE SCNX TRICK (executor): a GUI-authored scenario saved to .scnx is a READABLE
  XML SPEC of what a working unit IS. Parse the TropicTortoise .scnx + any scenario the
  user saves in Phase 1: extract exactly how a native unit is represented (org tree,
  embedded entities, aggregate state, controllers). This is the target our remote
  creations must match. (Programmatic .scnx work is already proven - a KEEPER. 0.3
  CONFIRMED the remote lever too: saveScenario(..., saveToZip) at
  vrfRemoteController.h:660 commands the backend to save the LIVE scenario - so
  remote-created scenarios can be saved and diffed against GUI-authored ones.)
- 0.6 CONSOLE CAPTURE TOOL (executor; NEW, escalated by the 0.2 finding): the yellow
  triangle badge = the Object Console warning icon - most of our units carry UNREAD
  VRF WARNING MESSAGES, and addObjectConsoleMessageCallback
  (vrfRemoteController.h:1970, verified; delivers uuid + notifyLevel + message text
  over the network) lets us capture them headlessly. Build: facade callback ->
  bridge event -> extend WatchVrf (or a sibling ConsoleWatch tool) to log
  CON,<t>,<uuid>,<level>,<message> lines alongside POS lines. Offline-buildable now;
  live-gated next session. From then on EVERY run captures VRF's own per-unit
  diagnostics - the pile split and runaways may already be explained in messages we
  never read.
  *** 0.6 IS NOT DONE. THE LIVE GATE RAN 2026-07-19 ON run 20260719T144109Z AND RETURNED
  ZERO CON LINES, over 180 s with 6 freshly created units, two of which were tasked and
  did not move. Do NOT re-record 0.6 as complete on the strength of the offline test.
  `git grep "^CON,"` across the tracked repo and all runs returns NOTHING - no live CON
  line has ever existed.
  WHAT WAS RULED OUT by a file:line audit (so it is not re-investigated): the path IS
  wired end to end and the callback IS registered. WatchVrf does instantiate a real
  DtVrlinkVrfRemoteController, and addObjectConsoleMessageCallback is registered inside
  VrfFacade::Start() (VrfFacade.cpp:346) - i.e. BEFORE BeginTrackingReflectedObjects and
  long before the first Tick(), so there is no ordering violation of the kind RUNBOOK
  sec 8 documents for reflected-object tracking. No config flag or opt-in gates it. The
  deployed binaries post-date commit 50a5c0c and provably contain the symbols. The trace
  is raw unfiltered stdout, so nothing filtered CON lines out.
  THE TRANSPORT IS THE SUSPECT. Object Console text travels by COMMENT PDU / INTERACTION
  (objectConsoleNetworkPrinter.h:19), not on the message-executive channel. Two things
  could not be settled statically, and either fully explains the silence: (a) whether the
  sim engine attaches the NETWORK printer by default at all (objectConsoleManager.h:38
  says init() "calls addDefaultPrinters()" without enumerating them) or only the VR-Link
  printer; (b) whether an HLA observer federate receives Comment interactions it never
  explicitly subscribed to.
  IMPORTANT TRAP, do not repeat it: THE GUI BADGE IS NOT EVIDENCE OF WIRE DELIVERY. The
  GUI receives console text via processObjectConsoleMessageCallback on a DIFFERENT channel
  (vrfGuiCore/objectConsoleMessageHandler.h:104). Seeing the yellow triangle in the GUI
  says nothing about whether a comment interaction reached a remote federate.
  THE DECISIVE EXPERIMENT, and the ONLY one that answers positively rather than by
  silence: logObjectConsoleToFile (vrfRemoteController.h:1983) makes the BACKEND ITSELF
  write an object's console to a file, bypassing the network question entirely. File has
  warnings + CON empty => DELIVERY GAP. File empty => no messages were raised, proven
  rather than inferred. Worth exposing setObjectNotifyLevel (:1977) and
  addBackendConsoleMessageCallback (:1992) in the same pass. This REQUIRES A NATIVE
  REBUILD of VrfFacade/VrfBridge - there is no C#-only route, because nothing on the
  managed surface can affect delivery. The toolchain was proven healthy 2026-07-19
  (full /t:Rebuild, 0 errors, 9 s). ***

Exit criteria: the five deliverables exist, cross-referenced, adversarially reviewed;
open unknowns are listed as QUESTIONS with where the answer lives (doc, probe, or MAK).

## Phase 1 - Native reference baseline - *** METHOD SUPERSEDED 2026-07-18, DO NOT RUN AS WRITTEN ***

(Heading used to read "user + supervisor at the GUI; ~1 hour, scripted". The
human-at-the-GUI method is RETIRED - see sec 1a. The QUESTIONS below survive and are to
be answered HEADLESSLY; docs/PHASE1_SESSION_SCRIPT.md carries the full banner.)

The ground truth run. At TropicTortoise (Mojave), USING ONLY THE VRF GUI:
- 1.1 Palette-create a small real force: one M1A2 (entity), one REAL armor platoon
  aggregate, one REAL armor company aggregate (types chosen from the 0.1 catalog -
  no generics), dispersed near the COA-STP1 AO.
- 1.2 Native-task each along a COA-shaped route (2-3 waypoints, ~2-20 km legs,
  including one leg crossing the 18.4 km radius where CPP-ALT-1's marchers stopped).
- 1.3 Capture: WatchVrf the entire run (fresh appNos); note every GUI status change,
  badge, and completion; SAVE THE SCENARIO to .scnx (feeds 0.5).
- 1.4 Same session, repeat one move at TimeMultiplier 20 - does NATIVE tasking survive
  the fast clock, or is 20x itself implicated in warp/runaway? (Cheap, high value:
  every port run used 20x; CPP-ALT-1 at real time showed no 100 km drives by its
  marchers.)

Exit criteria / what this settles:
- The native baseline telemetry = the acceptance target for everything after.
- If native units ALSO misbehave at Mojave (runaway, mid-route stop at the tile edge),
  the problem is environmental/VRF-internal and the MAK support question fires NOW with
  a perfect repro - BEFORE we rebuild anything.
- If native units behave, we have a proven-good target and the gap analysis (Phase 2)
  becomes a diff, not a guess.

## Phase 2 - Gap analysis: our creations vs the working ones (offline executors)

- 2.1 TYPE MAPPING TABLE: every COA-STP1 + golden-init unit -> nearest REAL installed
  template (from 0.1), by echelon + function + DIS proximity; flag every case where no
  close type exists (user adjudicates those). Deliverable: the mapping table the user
  reviews line-by-line - "as close to their real types as possible" made concrete.
- 2.2 STRUCTURE DIFF: field-by-field diff of a Phase-1 GUI unit (from the saved .scnx
  + its reflected attributes) vs the same type created by our interface (reflected
  attributes + any scnx save of a remote-created scenario). Every missing/differing
  attribute is a candidate cause of taskability differences - ranked.
- 2.3 TASK VOCABULARY MAP v2: C2SIM verbs -> the NATIVE task/params the GUI sends
  (0.2/0.3 tell us what those are), replacing collapse-to-moveAlongRoute where a
  closer native task exists.
- 2.4 Offline forensics debt (carried, now purposeful): runaway/warp census from the
  archived 3450/3453/3454 CSVs against the 0.2 terrain/paging chapter; yellow-badge
  identification; per-taskee census of CPP-ALT-1.

## Phase 3 - Rebuild the port's creation layer against the spec (executor; offline gates)

- New creation mode (Vrf:CreationMode=native; parity mode retained as escape hatch,
  UnitTranslator untouched as the parity oracle): real-template types from 2.1, real
  subordinate/org structure per 2.2, formation state set the R5 query-driven way.
- Acceptance (offline): builds 0 errors, selftests green unchanged (parity modes), new
  mapping selftest pins the 2.1 table.
- Acceptance (live, reference scenario first): create the force VIA C2SIM init;
  structure-diff the saved .scnx against a natively-authored reference ~clean; THEN
  task via C2SIM order - telemetry (WatchVrf displacement) must match the reference
  baseline.
  *** CORRECTED 2026-07-18: this gate used to include "native-task OUR units from the
  GUI (they must behave like GUI-created ones)" as a middle step. THAT IS A GUI STEP ON
  THE ACCEPTANCE PATH and sec 1a rule 1 forbids it - a GUI step may never be a
  prerequisite for a scored run. The comparison it was reaching for (do our units
  behave like natively-authored ones?) is obtained HEADLESSLY by structure-diffing the
  saved .scnx against a GUI-authored reference .scnx, which is what groundwork 0.5 and
  tools/ScnxDiff exist for. ***

## Phase 4 - Truthful execution layer (parallel with Phase 3; already specified)

- Truthful-arrival gate (recovery plan sec 4 item 1): no sequencer advance, no outward
  TASKCMPLT without center-displacement arrival. Both interfaces proved it mandatory.
- Runaway containment: halt + flag any mover exceeding route length x factor or exiting
  the AO radius. These two make every later run self-scoring.

## Phase 5 - Scale acceptance and the external asks

- COA-STP1 end-to-end on the rebuilt layer, scored by displacement only.
- coa-gpt data feedback updated (dispersed positions etc. - the memo exists).
- MAK support question: sent EARLIER (Phase 1 exit) if native units misbehave; else
  sent here for whatever residue survives the rebuild (pile split, warps) - now with
  native-baseline evidence attached, which is what makes it answerable.

## Sequencing and effort

- Phase 0 starts immediately, fully offline (executors); 0.4 needs one short live gate.
- [RETIRED 2026-07-18] "Phase 1 is the next LIVE session (user + supervisor, ~1 hour,
  scripted in advance)." Its METHOD is superseded (sec 1a). The next live action is the
  HEADLESS run - docs/HEADLESS_RUN_PLAN.md - with no user at the GUI.
- Phases 2-4 are mostly offline; live checks are short and single-purpose.
- No further scattergun live probes: every live minute from here serves a phase exit.

## Status

- 2026-07-19 LATE (LIVE): **THE ONE-BUTTON HEADLESS LOOP RAN END TO END, THREE TIMES,
  REPRODUCIBLY. THE UNITS DO NOT EXECUTE THEIR TASKS. AND THE MOVEMENT ORACLE ITSELF IS
  NOW IN QUESTION.** Full write-ups: docs/experiments/RUN_2026-07-19_MOJAVE_CHAIN.md and
  docs/experiments/PREREG_TSK_DELIVERY_2026-07-19.md.
  - THE LOOP WORKS: scripts/RunC2SimScenario.ps1 takes a C2SIM init + order through
    launch, join, creation, tasking, telemetry and clean resign with ZERO human
    interaction. That is the mandate, demonstrated.
  - THE RESULT, reproduced across runs 161438Z / 202349Z / 222134Z. *** NOT 144109Z - its teardown
  FAILED (StopVrf EXIT=3, back-end brought down BY HAND), so it is not a zero-human run.
  An earlier version of this line listed it and omitted 222134Z, the only run that
  exercised the current teardown code. ***: all three tasks are
    issued correctly (CreateRoute + MoveAlongRoute). 114.MechCoy and 1.BdeHQ do NOT move
    (0.0 m, bit-exact, confirmed by two independent channels). *** RETRACTED 2026-07-19 LATE: the "moves ~174 m and stops" reading below is FALSE. The
unit was STILL MOVING AND NOT SLOWING when observation ended (do not claim acceleration - two RPT legs cannot show it, and leg 1 contains spin-up from rest); ~174 m is a MEASURED displacement over RPT's coverage, not a stopping point.
*** PRECISION FIX 2026-07-20 - THE EARLIER PHRASING WAS WRONG. ~174 m is a REAL MEASURED
DISPLACEMENT: 84.0 m + 90.1 m across RPT's coverage span t=32.9 to t=157.1 = 124.2 s.
It is NOT "the length of the observation window" - that window is ~145 s and at 1.4 m/s
would predict ~203 m, which is not what was measured. The correct statement is: the unit
covered a MEASURED 174 m and WAS STILL MOVING when RPT coverage ended. NOTE - A CLAIM ABOUT AN "RPT GAP" WAS ADDED HERE ON 2026-07-20 AND WAS
FABRICATED. IT IS DELETED. There is no missing RPT fix: the reporting period is ~62 s
(fixes at t=32.9 / 95.2 / 157.1), the next was due at t~219 s, and the interface resigned
at ~182 s. The retracted claim compared ONE unit's last fix (157.1) against the GLOBAL POS
collapse (180.3) - different quantities. Do not chase this; it is not a defect. *** The route needs ~825 s at ~1.4 m/s and no run has ever observed more than
~145 s. RUN -RunSecs 900+. The two FROZEN units are unaffected - that result stands.
Superseded text follows: ***
1222.MechPlt moves ~174 m
    of a ~1155 m route and stops - 174.1 m and 174.4 m on two runs, so this is a
    REPRODUCIBLE defect, not noise. No TASKCMPLT is ever emitted: an HONEST failure.
  - RULED OUT with evidence, do NOT re-investigate: wrong/buried spawn (creation is EXACT
    to 6 dp, ground clamp works), missing route, missing task issue,
    *** "LYING COMPLETION" REMOVED FROM THIS LIST 2026-07-19 LATE - IT IS NOT RULED OUT.
    No TASKCMPLT was seen, but NOTHING is observable past the ~180 s collapse, so a
    completion at t=400 would be equally invisible. Absence here proves nothing. It is an
    UNTESTED question, and the long run (-RunSecs 900+) is what answers it. ***
    stale aggregate reads, scenario-injected behaviour (TropicTortoise .pln is a 36-byte
    EMPTY header - this also CLOSES the groundwork 0.5 ".pln unparsed" gap; Bogaland2 is
    the identical inert ancestor; taskRules/ and scriptedObjectMovement/ are empty ONLY IN THE C2simEx LAYER - *** NOT RULED OUT, RETRACTED 2026-07-19: C2simEx.sms includes EntityLevel.sms, whose taskRules/ holds default-task-rules.tsk + doctrines.dct + actionCategories.tsk and whose scriptedObjectMovement/ holds 19 files. NOBODY HAS OPENED THEM. ***),
    and AI-capable entity templates.
  - *** THE BIGGEST OPEN ITEM: THE ORACLE CONTRADICTS ITSELF. *** WatchVrf now also emits
    RPT lines (VR-Forces' OWN position reports, carrying MARKING TEXT - which incidentally
    solves the member-to-parent mapping the runaway census called unsolvable offline). On
    1222.MechPlt the two channels DISAGREE ABOUT DIRECTION: RPT shows steady EASTWARD
    progress toward the objective while the POS stream shows it 65 m WEST and frozen. THE
    CONTROL IS WHAT MAKES THIS SERIOUS: on the two units that do NOT move, the channels
    agree EXACTLY. They agree on stationary objects and disagree only on the moving one.
    POS displacement is the standing "only movement oracle" and every negative movement
    result in this project rests on it.
  - THE DECISIVE TEST IS BUILT BUT NOT SHIPPABLE YET: log lastSetLocation() (raw) beside
    location() (approximator-extrapolated). It NEEDS A NATIVE CHANGE. My first attempt
    BROKE THE PIPELINE TWICE and was REVERTED - see the four-run table in commit 5d14eda.
    Root cause of the regression: the change made Start() register extra controller
    callbacks UNCONDITIONALLY for every consumer including the production app. A
    DIAGNOSTIC MUST NOT CHANGE THE BEHAVIOUR OF THE THING IT OBSERVES. When redone:
    additive, OPT-IN, and it MUST NOT TOUCH THE DEFAULT Start() PATH.
  - FINDINGS THAT SURVIVE THE REVERT (implementation-independent, keep them):
    * RUNBOOK sec 7's "RTTI across the MAK DLL boundary" story is FALSE. MAK's own header
      (reflectedExtAggregate.h:15-19) shows that under DtHLA the class deliberately derives
      from DtReflectedObject, so a null dynamic_cast<DtReflectedAggregate*> is CORRECT.
    * The blind static_cast worked on aggregates only through ACCIDENTAL VTABLE SLOT
      ALIGNMENT, and is undefined behaviour on control objects - which is why location()
      returned garbage there while lastSetLocation() faulted.
    * "The TropicTortoise baseline objects are POSITIONLESS" (RUNBOOK 0.5.7) is *** THIRD ATTEMPT AT THIS CORRECTION; THE FIRST TWO WERE BOTH WRONG. Re-derived from the
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
readable objects, and a NaN row from GlobalEnv must NOT be read as correct-and-expected. *** SUPERSEDED WORDING FOLLOWS: it is an
      ARTEFACT of that bad cast, not a fact about VR-Forces. Their real positions have
      never been read.
  - TOOLING HARDENED THIS SESSION: argument guards on every tool (StopIface could act
    destructively with NO arguments); the runner's Start-Process -Wait DEADLOCK (it waits
    for DESCENDANTS, so it could never return from launching VR-Forces); the oracle gate,
    which PASSED ON GARBAGE (lat 1e-6, lon -90, altitude 1.02e15 m) because it never
    checked the altitude column it was already given; and a crashed oracle now FAILS the
    run instead of reporting "RUN COMPLETE".
  - KNOWN BLOCKER: a "Session Status - Close current terrain?" modal can hang StopVrf.
    *** CORRECTED: it does NOT fire on EVERY clean teardown - it is named in ZERO of the six stopvrf logs -
    INCLUDING 193252Z, the run where it demonstrably DID appear, because the top-level
    search could not see it. LOG ABSENCE IS THEREFORE INVALID EVIDENCE HERE. The VALID
    evidence that it is intermittent is that FOUR teardowns COMPLETED CLEANLY. IT IS INTERMITTENT. The descendant-scan fix has SHIPPED but is UNVERIFIED -
    it has never been exercised by a real occurrence. ***, because it is a NESTED child of the main
    window and StopVrf only searches TOP-LEVEL windows.
- 2026-07-19 (OFFLINE): **ARGUMENT GUARDS LANDED AND VERIFIED; PASS CRITERION WRITTEN BUT
  UNRATIFIED; VrfBridge TOOLCHAIN PROVEN HEALTHY. THE FIRST SCORED RUN HAS NOT RUN AND IS
  HELD ON A USER RULING.** Commits 0ca46f0, 54a27f0. No live time consumed; appNo marker
  still reads 3503 (nothing joined, nothing burned).
  - GUARDS DONE (the blocker named in the entry below). StopIface no longer acts on a bare
    invocation: the localhost defaults are GONE and an explicit --yes is required before any
    PushCommand. IMPORTANT FRAMING CORRECTION - HEADLESS_RUN_PLAN sec 4 called this an
    "argument guard" problem, and for StopIface it was not: args.Length==0 was the DEFAULT
    HAPPY PATH, so a usage message alone would NOT have prevented the 2026-07-18 incident.
    Also fixed: StopIface returned 0 unconditionally, so a silently failed teardown reported
    success. It now verifies GetStatus()==UNINITIALIZED and returns 1 otherwise.
    Contract across all tools: 0 success / 1 operational failure / 2 usage error, nothing done.
    Supervisor re-ran every acceptance check by hand: 8 guard paths confirmed exit=2 with no
    network contact (deliberately tested against a bogus port so a faulty guard would fail
    harmlessly), builds Release 0 errors, ASCII clean.
  - PASS CRITERION WRITTEN: HEADLESS_RUN_PLAN sec 4a now carries proposed arithmetic for
    arrival (split entity 50 m vs unit 250 m per the leading-edge rule), moved-at-all
    (>=25 m sustained over >=3 samples), runaway (>5x leg or >5 km from birth), DR-artifact
    rejection, and completion trust. *** IT IS MARKED AWAITING USER RULING AND THE RUN IS
    HELD ON IT. *** Do NOT run scored and then settle the numbers - that is this project's
    documented failure mode. Weakest number, flagged in place: the 250 m unit tolerance,
    because company formation depth may be comparable to the 556 m leg itself.
  - FIRST-TARGET PREMISE CORRECTED. HEADLESS_RUN_PLAN sec 3 claims the Mojave pair gives
    "a binary first result with no aggregation, no formation, and no controller-class
    confound". FALSE, verified in the file: R9_Mojave_UnitMove_Order.xml has THREE tasks
    against an ENTITY (1.BdeHQ), a COMPANY (114.MechCoy) and a PLATOON (1222.MechPlt), legs
    577.8 / 556.0 / 577.8 m. All three are MOVE, so there is no VERB confound - but the
    echelon/controller-class confound is exactly the axis the census could not de-confound
    offline. Run 1 yields THREE scored results, not one binary. Use
    R9_Mojave_Lean_Initialization.xml (6 units) not R9_Mojave_Initialization.xml (80 units;
    "158" is the ActorReference count, not units).
  - SCALE FACT worth carrying: the legs are ~556-578 m while every known failure phenomenon
    is 18-100 km. Run 1 therefore CANNOT test the 18.1-18.4 km stall band at all.
  - VrfBridge TOOLCHAIN IS HEALTHY - this retires the main risk on the raw-vs-DR oracle fix.
    MSBuild 18.6.3.22110 at the documented VS18 path; full /t:Rebuild = EXIT 0, 0 errors, 9
    seconds. CAUTION FOR WHOEVER REPEATS THIS: a plain build returns EXIT=0 in 2 seconds
    having compiled NOTHING (everything up to date). That is a false green of the same shape
    as the old reflected>0 criterion. Use /t:Rebuild or you have proven nothing.
  - RAW-vs-DR ORACLE FIX SCOPED, NOT BUILT. It REQUIRES a native change - VrfFacade.cpp:737
    `sr->location()` is the extrapolating read, and the raw DtVector never crosses the
    managed boundary, so it cannot be done in C#. The good news: `lastSetLocation()` (:118)
    and `lastSetVelocity()` (:133) are declared on DtBaseEntityStateRepository, which is
    exactly the type VrfFacade.cpp:723 already holds, so it needs ZERO new casts and the
    aggregate RTTI problem is already absorbed by the existing static_cast fallback.
  - *** NEW HAZARD FOUND, UNRESOLVED, DECISION NEEDED: THE SEVEN VrfBridge.dll COPIES ARE
    THREE DIFFERENT BUILDS. *** tools/ResetVrf runs a 2026-07-11 bridge (777,216 b) and
    src/SmokeTest a 2026-07-09 one (769,536 b), while VrfC2SimApp / CreateOne / SetSimRate /
    WatchVrf run 2026-07-18 (839,680 b). ResetVrf is the AUTOMATED STALE-FEDERATE RECOVERY
    TOOL (sec 8) and it is executing native code a week older than the oracle it exists to
    rescue. Nothing is committed (all DLLs are gitignored), so this drifted silently and
    there is no known-good binary to revert to - rollback means rebuilding from a commit.
    NOT "fixed" here on purpose: refreshing them changes the behavior of the recovery tool
    and the smoke test without either being tested.
- 2026-07-18 EVENING (RE-GROUNDING, user-directed): **THE EFFORT HAD DRIFTED OFF-MISSION.
  THE PRODUCT IS HEADLESS - C2SIM IN, SIMULATION RUNS, TELEMETRY VERIFIES, ZERO GUI.**
  New sec 1a (THE HEADLESS MANDATE) is now the governing statement; new
  docs/HEADLESS_RUN_PLAN.md is THE NEXT ACTION.
  - WHAT WENT WRONG: the supervisor read PHASE1_SESSION_SCRIPT.md's "the user drives the
    VR-Forces GUI" as a constraint on the whole effort, told the user live testing needed
    a human, and then built UI-Automation menu-driving to route around that invented
    human. Both halves were wrong. VERIFICATION never needed a human (it is arithmetic on
    the WatchVrf POS trace) and CREATION/TASKING never did either (tools/CreateOne creates
    headlessly; the full C2SIM pipeline ran end to end on 2026-07-10, RUNBOOK sec 7).
  - PHASE1_SESSION_SCRIPT.md is SUPERSEDED IN METHOD, banner-marked at the top. Its
    pre-registered QUESTIONS (P1-A..D) survive; its human-at-the-GUI method does not.
  - THE CHAIN IS COMPLETE AND BUILT: LaunchVrf -> VrfC2SimApp -> PushInit -> PushOrder ->
    WatchVrf -> ListenReports -> StopIface -> StopVrf. All headless. The four C2SIM tools
    were rebuilt this evening (Release, 0 errors); C2SIM server verified live (REST 8080
    HTTP 200, STOMP 61613 open); VrfC2SimApp builds 0 errors.
  - NEXT DELIVERABLE: scripts/RunC2SimScenario.ps1 - the button. First target
    R9_Mojave_Initialization.xml + R9_Mojave_UnitMove_Order.xml (Mojave matches the
    loaded terrain; single unit move = binary result, no aggregation confound).
  - BLOCKING FIRST: argument guards on PushInit / PushOrder / StopIface. StopIface ACTS
    with no arguments - it drove the live server RUNNING -> UNINITIALIZED during a
    usage probe. Unacceptable in an unattended runner. Copy the CreateOne/SetSimRate
    pattern (no defaults, hard exit 2, no action on bad args).
  - NOT NEXT, EXPLICITLY: GUI automation of any kind; the Phase 1 human session; the
    Phase 3 creation-layer rebuild (measure the current interface first).
- 2026-07-18 EVENING (LIVE): **UNATTENDED TEARDOWN BUILT; THE ORACLE PRE-CHECK CRITERION
  WAS WRONG IN BOTH DIRECTIONS AND IS CORRECTED. PHASE 1 STILL HAS NOT RUN.** This entry
  SUPERSEDES the 0.4 entry below wherever they disagree - in particular that entry's
  "NEW MANDATORY oracle pre-check (require reflected>0 ... ~13 s / ~15 s)" line is
  RETRACTED. Evidence: RUNBOOK sec 0.5.7 and 0.5.9.
  - TEARDOWN WAS NEVER UNATTENDED and the docs read as if it were. Closing vrfGui raises
    a modal confirm ("Are You Sure?", class makVrf::DtNeverAskAgainMessageBox) that
    blocks until answered. NEW scripts/StopVrf.ps1 answers it via UI AUTOMATION BY
    CONTROL NAME (that dialog exposes a full UIA tree; the RTI connection dialog does
    NOT - do not generalise between them). VERIFIED LIVE: teardown in 8 s, EXIT=0, zero
    human interaction, no force-kill, all three RTI processes preserved.
  - ORACLE PRE-CHECK CRITERION CORRECTED. reflected>0 PASSED on garbage (both readable
    objects degenerate: 90/-90 pole and NaN, 14/14 samples - the TropicTortoise baseline
    objects are POSITIONLESS). And "reflected=0 after 20 s -> STOP" would have ABORTED a
    healthy federation: same launch, visible ~40 s, blind ~20-50 s, visible ~104 s.
    LaunchVrf READY does not imply scenario loaded or federation joined. New rule:
    require a REAL-COORDINATE POS, retry up to ~3 min, CreateOne check now MANDATORY.
  - LAUNCH/TEARDOWN ROUND-TRIP PROVEN unattended (3493/3494 then 3497/3498), and a
    relaunch is CONFIRMED to clear a CreateOne throwaway from the live scenario.
  - VR-FORCES MAIN WINDOW EXPOSES A FULL UIA TREE (makVrf::DtVrfQtDeMainWindow, 114
    elements, menu bar incl. Create and Task). Whether native GUI creation/tasking can
    therefore run unattended is UNPROVEN - the Task menu returned 0 children (probably
    selection-dependent, NOT tested) and map/viewport interaction is very likely NOT
    UIA-addressable. Open question, not a result.
  - LEDGER: 3455 IS BURNED (consumed by the pre-check, not by Phase 1 telemetry). Only
    3456-3459 remain reserved for Phase 1. The line below claiming "3455-3459 remain
    reserved and untouched" is SUPERSEDED.
- 2026-07-18 (LIVE, 0.4): **0.4 COMPLETE - GATE PASSED. VR-FORCES NOW LAUNCHES
  UNATTENDED AND THE MOVEMENT ORACLE IS VERIFIED END TO END.** This retires the
  "a human must launch VR-Forces" dependency that has constrained every session.
  Full write-up: docs/experiments/SESSION_2026-07-18_SELFLAUNCH.md; gate result in
  docs/experiments/PREREG_0_4_SELFLAUNCH.md sec 12.
  - LAUNCH (zero human interaction, EXIT=0 READY, verified by TWO clean runs; a third
    earlier run failed and exposed two script defects):
      pwsh -File scripts\LaunchVrf.ps1 -Scenario TropicTortoise `
           -BackendAppNumber <fresh> -FrontendAppNumber <fresh>
  - GATE: ResetVrf --dry-run x2 (3489/3490) - joined cleanly, discovered the 2
    TropicTortoise baseline objects, no deletes, resigned cleanly, EXIT=0 both
    times, ZERO 0xC0000005. Prereg sec 4 prediction met exactly.
  - ORACLE (DISCOVERY + POSITION FIDELITY, *not* displacement): tools/CreateOne
    created one M1A2; WatchVrf reported POS ... 34.517156,-116.973525,1060.7 -
    exact requested lat/lon, altitude ground-clamped from 10000 MSL, stable across
    every sample. THE ENTITY NEVER MOVED, so DISPLACEMENT was NOT exercised;
    P1-A..D all rest on displacement.
  - ROOT CAUSE of every "VR-Forces hangs on launch" episode: on HLA the RTI
    Assistant PROMPTS for a connection and the federate does not start until it is
    answered. Vendor-documented, not a bug. ONE-TIME per machine: answer it with
    "Always try to use this connection" CHECKED. *** NEVER KILL rtiAssistant /
    rtiexec / rtiForwarder *** - an already-answered assistant is what makes
    unattended launch work; killing one as "cleanup" cost this entire session.
  - DO NOT USE RTI_ASSISTANT_DISABLE: processes start but federates never discover
    each other and WatchVrf goes silently blind (reflected=0).
  - PHASE 1 PRECONDITIONS UPDATED: manual launch retired; NEW MANDATORY oracle
    pre-check. *** THE CRITERION QUOTED HERE IS RETRACTED - SEE THE 2026-07-18 EVENING
    ENTRY ABOVE AND RUNBOOK 0.5.7. *** As written it said: "WatchVrf >=20 s, require
    reflected>0; discovery needs ~13 s to populate, do not judge before ~15 s". Both
    the reflected>0 pass rule and the 20 s abort rule failed live, in opposite
    directions. Kept verbatim so the error is not re-derived.
  - Six LaunchVrf.ps1 defects found and fixed (three previously recorded, plus the
    rtiexec readiness wait, the connection-dependent UDP-4000 health test, and a
    duplicated health expression where only one copy had been corrected).
  - THREE WRONG CORRECTIONS were written into RUNBOOK sec 0.5 during this session
    and then fixed; all three are recorded there so they are not re-derived.
  - PHASE 1 STILL HAS NOT RUN. (SUPERSEDED DETAIL: this entry said "3455-3459 remain
    reserved and untouched". 3455 was CONSUMED on 2026-07-18 evening by the oracle
    pre-check and is BURNED; only 3456-3459 remain reserved.)
- 2026-07-18 (superseded by the entry above; kept for the record): SCRIPTED
  BRING-UP PARTLY PROVEN; ROOT CAUSE UNRESOLVED; ONE SUPERVISOR CONCLUSION
  RETRACTED. Full write-up:
  docs/experiments/SESSION_2026-07-18_SELFLAUNCH.md. PHASE 1 DID NOT RUN - the live
  window went to the bring-up mechanism, the exact trade prereg 11.1 called the
  worse one. PHASE1_SESSION_SCRIPT.md is still READY and unstarted; 3455-3459
  remain reserved and untouched.
  - PROVEN: bare `vrfLauncher --usePredefinedConnection "<profile>"` (cwd bin64,
    license refreshed from Machine scope) brings up a HEALTHY combined-mode
    backend + front-end UNATTENDED, zero human clicks. First confirmed scripted
    VR-Forces bring-up in this effort.
  - NOT PROVEN: the same launch WITH fresh appNumber overrides and an auto-loaded
    scenario. That RUN STALLED (backend 2 threads, never joined).
  - RETRACTED: the in-session conclusion "the argument overrides break the
    backend". The comparison was NOT single-variable - a stale rtiAssistant
    holding port 6003 (alive and sitting on a modal dialog during the failed run,
    dead during the control) is an equally good explanation and was not
    controlled. Two candidates remain live; NEITHER is eliminated. A secondary
    retraction is recorded too: the supervisor had earlier dismissed the
    rtiAssistant hypothesis on invalid grounds (treating "the dialog existed" as
    "the dialog was blocking"). The user's error screenshots falsified that.
  - RUNBOOK sec 0.5 CORRECTED AT SOURCE: rtiexec NEVER runs on this machine
    (`RTI_useRtiExec 0`), so no readiness gate may wait for it. Real transport is
    UDP 4000. New backend-health oracle recorded (UDP 4000 bound + thread growth
    past 2 + vrfSim.log progression); PROCESS PRESENCE IS NOT HEALTH.
  - LaunchVrf.ps1: the three recorded defects are FIXED and tested (app-number
    hard gate verified by direct test; the MainWindowTitle check earned its keep
    live). A FOURTH defect was found and is NOT fixed - the poll still waits for
    rtiexec, so the script cannot report READY on this machine at all.
- 2026-07-18 (pre-session): PHASE 1 SCRIPT SELF-CONTRADICTION FIXED + binaries
  re-verified by hand. The script's WatchVrf precondition said to use the
  extended 0.6 build only "if [it] has passed its live gate", while the same
  script's closing note and this Status both say the 0.6 gate IS this session.
  That is CIRCULAR: followed literally it would have silently downgraded the
  session to GUI-only console capture and discarded the folded-in gate. The
  precondition now directs the extended build unconditionally, with an explicit
  CON-fault fallback that does NOT touch the movement oracle (the S0 gate
  established the POS emission block is absent from the 0.6 diff, so a CON
  failure cannot invalidate P1-A..D). Supervisor re-ran the acceptance checks
  rather than trusting the recorded ones: WatchVrf.exe present and NEWER than
  its last source commit 50a5c0c, --con-selftest 25/25 PASS exit 0;
  SetSimRate.exe present, arg validation hard-exits 2 on all five invalid forms
  (no args, non-numeric, zero, NEGATIVE multiplier, missing appNo). No
  federation join occurred and NO ledgered appNo was consumed (out-of-band 3999
  used deliberately for the arg probes). CAVEAT on the SetSimRate check: only
  the reject paths were exercised - the ACCEPT path cannot be tested without a
  live join, so the backend-settle wait, the 15 s cap, the 3 s flush, and
  whether the multiplier survives resign all remain UNRETIRED and are still
  Step 1b's job.
- 2026-07-18: S0 SUPERVISOR GATE of the two previously-claimed-green Phase 0
  builds: BOTH HOLD, independently re-verified this pass. Builds: VrfBridge
  Release 0 warnings / 0 errors; VrfC2SimApp 0 errors (2 pre-existing CS8632
  nullable warnings, unrelated to 0.5/0.6); WatchVrf 0/0. Selftests: CON
  25/25 PASS exit 0; translator 18/18 PASS exit 0; ScnxDiff all three
  acceptance checks exit 0 (dump, self-diff IDENTICAL, one-field mutation
  detected). CON escaping independently fuzzed 200000 cases - 0 round-trip
  failures, 0 raw-newline leaks.
  - WORDING CORRECTION (load-bearing) to the 2026-07-16 (late night) entry,
    which overstated the 0.6 result as "verified byte-identical vs HEAD".
    Corrected in place. What is true: the POS RECORDS are byte-identical,
    verified by normalized diff across the 0.6 refactor commit 50a5c0c - the
    POS emission block does not appear in the diff at all; format string,
    InvariantCulture wrapper, time base, sample cadence and skip filter are
    all unchanged; the native side is purely additive (VrfBridge.cpp 25/0,
    VrfFacade.cpp 25/0, VrfFacade.h 19/0 - zero deletions). What is NOT
    true: total stdout is not byte-identical. The "#" summary line became
    CultureInfo.InvariantCulture (a FIX - under a comma-decimal locale the
    old code emitted "# t=12,5s") and two banner/status lines were reworded.
  - D1 RESOLVED (user ruling 2026-07-18): Phase 1 Step 4 sets 20x via REMOTE
    setTimeMultiplier, not the GUI toolbar (which caps at 15). Mechanism: a
    new additive tool tools/SetSimRate cloned from ResetVrf (built
    separately); VrfFacade/VrfBridge already expose SetTimeMultiplier, so no
    existing source is edited and the WatchVrf POS path is not touched. API
    contract registered in ground truth 0.3 sec 6a. RESIDUAL RISK to retire
    before Step 4 depends on it: SetSimRate joins as an ADDITIONAL federate
    while WatchVrf is joined (ResetVrf uses the same pattern but normally
    with nothing else observing) - dry-run it alongside WatchVrf first, and
    budget two fresh ledgered appNos (each invocation is a full
    join/resign).
  - [SUPERSEDED 2026-07-18 evening - 0.4 PASSED and launch is now UNATTENDED via
    scripts/LaunchVrf.ps1; the manual-launch instruction in this bullet is RETIRED.
    See the Status TOP entry.] LIVE ORDER CHANGED BY USER (2026-07-18): the 0.4
    self-launch gate is
    DEMOTED behind Phase 1. The next live session is the Phase 1 baseline
    with the 0.6 CON live gate FOLDED IN (it costs nothing extra and Step
    0's console capture is exactly where its evidence lands); the user
    launches VR-Forces manually per RUNBOOK. 0.4 gets its own short session
    afterward, on a repaired script. Rationale: Phase 1 is the
    highest-value live hour in the effort, and 0.4 carries a HIGH-risk
    untried dialog mitigation (prereg RISK A) plus three confirmed script
    defects.
  - [SUPERSEDED - all three of these defects, plus three more found later, are
    FIXED. See the Status TOP entry.] 0.4 SCRIPT DEFECTS confirmed by supervisor
    read of scripts/LaunchVrf.ps1
    (detail recorded in docs/experiments/PREREG_0_4_SELFLAUNCH.md): readiness
    poll is process-presence only (line 291) while -DryRun advertises a
    MainWindowTitle modal-dialog check it never performs; app-number
    freshness is a WARNING only (lines 92-93, 122-123) against the
    never-reuse non-negotiable; MAKLMGRD_LICENSE_FILE is overwritten
    unconditionally (line 267) even when the Machine value is empty.
    CONFIRMED CLEAN: the script contains NO termination calls of any kind.
- 2026-07-17 (+3): E8 PRIOR-ART SURVEY LANDED and supervisor-ACCEPTED
  (docs/PRIOR_ART_SURVEY.md; provenance-tagged - [PRIMARY-FETCHED] vs
  [SEARCH-EXTRACT]; the MAK docs host was DNS-unreachable, so open-web nulls do not
  rule out MAK-KB answers). Gate verifications: MSDL-import claim re-fetched from
  MAK's own capabilities page (CONFIRMED verbatim); the VR-Link DR claim UPGRADED to
  header-verified locally - esr()->location() extrapolates to read time by design,
  and `lastSetLocation()` is the raw accessor, making the queued raw-vs-DR
  discriminator a two-accessor logging change (census sec 11 updated). Headlines:
  our five problem classes have NO public solutions (candidate MAK-ticket list);
  authoring model-set content is normal vendor practice (5.2 shipped more
  aggregates); MSDL IMPORT is the sanctioned OOB-load path others use - QUEUED:
  Phase-2 MSDL spike (import a small OOB, ScnxDiff it against remote-created and
  GUI-created units; live-gated, folds into the next live session after Phase 1).
- 2026-07-17 (+2): E8 PRIOR-ART WEB SURVEY launched (user-directed: "how do other
  people deal with these limitations?" - a pass the plan never scheduled; research
  debt owned). Questions: type-mapping/content-authoring practice in other C2SIM/BML
  integrations (NATO MSG-085/145/201, SISO SIW, GMU papers), public record of VRF
  remote-aggregate tasking problems, MSDL import as the established OOB-creation
  alternative (VRF docs show MSDL export; import support = E8 question), FTRT
  dead-reckoning practice, terrain-paging movement reports. Deliverable
  docs/PRIOR_ART_SURVEY.md. ALSO: authoring capabilities question answered from
  installed docs - Simulation Object Editor has full entity-level UNIT template
  authoring (create/edit units, subordinates+function+order, compositions,
  formations: SimulationModels\EntityLevelScenariosUnits\*), custom model-set
  layering (NewSMSCreate*), file import (EntityFilesImport), AddingContent.pdf;
  PRECEDENT: our loaded C2simEx set already contains 4 custom-authored templates
  (AR Scout, Mobile Irregular, Mobile Light Infantry, Skiff).
- 2026-07-17 (+1): Phase 2.1 and 2.4 DONE - both supervisor-accepted.
  E5 docs/TYPE_MAPPING_TABLE.md: 128 COA-STP1 partition EXACT 7 / NEAR 64 / PEND 54 /
  LONE 1 / AVN 2 (arithmetic re-summed at the gate; dispatch mechanism verified in
  UnitTranslator.cs - SIDC index 11, not EchelonCode); 71/128 get correct/near real
  types with NO user decision; golden init (80 units) shown separately; two soft gaps
  beyond the adjudication doc (friendly recon, anti-armor). PEND rows stay neutral.
  E7 docs/experiments/RUNAWAY_WARP_CENSUS_2026-07-17.md: controller-split SUPPORTED
  directionally (LF 3/5 moved, HU 0/4, entity 0/2, both codebases; echelon-confounded,
  live probe still required); 18.1-18.4 km band REAL (6 tight, terminal - nothing
  stops 19-50 km) at 1x, absent at 20x; arrived=0 both runs; pile not the
  discriminator (all taskees born in it). SUPERVISOR GATE ADDENDUM (census sec 11):
  transient warps are LOCKSTEP group events failing frame-stall arithmetic at 1x -
  leading candidate is an OBSERVER-SIDE dead-reckoning artifact; persistent
  underground end-states remain the real port-20x runaway class; member-entity warp
  telemetry observation-suspect pending a raw-vs-DR WatchVrf discriminator
  (enhancement candidate, registered as ground truth 0.0 item 6).
- 2026-07-17: Phase 2.3 DONE - docs/TASK_VOCABULARY_V2.md (E6) supervisor-accepted:
  verb counts + STREND 31/42 + all-42-self-target re-verified from raw XML at the
  gate; IsImplemented==false for HoldObjective/Clear confirmed in code. Headlines:
  the REAL collapse-to-move is the HoldObjective family (7 verbs, 14 tasks) +
  CLRLND, not the four wired Layer-2 families; the dominant fidelity blocker is
  AGGREGATED-vs-DISAGGREGATED (combat verbs need disaggregation - NEW USER POLICY
  QUESTION for Phase 3); Reconnoiter/Escort tasks NEVER self-complete (chain
  poison); sequencing recommendation = KEEP external sequencer, re-key CompleteTask
  to displacement arrival (native plans ride the same lying leading-edge signal and
  VRF has no position-based completion predicate). E5 (2.1 mapping table) and E7
  (2.4 forensics census) still in flight.
- 2026-07-16 (late night): ALL FOUR EXECUTORS LANDED AND SUPERVISOR-ACCEPTED.
  Phase 0 OFFLINE WORK IS COMPLETE; what remains of Phase 0 is two LIVE gates.
  - 0.6 console capture: BUILT (facade+bridge wrap of
    addObjectConsoleMessageCallback; WatchVrf now emits CON,<t>,<uuid>,<level>,
    <msg> beside POS on one UTC base; RFC-4180-style reversible escaping).
    Supervisor re-ran builds' selftests (CON selftest + translator selftest,
    both exit 0); POS RECORDS verified byte-identical vs HEAD (total stdout is
    NOT - see the 2026-07-18 wording correction). LIVE GATE PENDING:
    does VRF deliver console messages over the wire in our federation.
  - 0.5 scnx harness: BUILT (tools/ScnxDiff, Python stdlib). All three
    acceptance checks independently re-run by supervisor. LOAD-BEARING FORMAT
    FINDINGS registered as ground truth sec 0.5 (.oob is S-EXPRESSIONS not XML;
    echelon IDs NOT persisted; parent-name is the only org linkage).
  - 0.4 self-launch: DRAFTED (scripts/LaunchVrf.ps1 + docs/experiments/
    PREREG_0_4_SELFLAUNCH.md; combined-mode vrfLauncher, refuses backend-only
    and existing-process launches, -DryRun verified against real machine state).
    Supervisor fixed two defects found gating (slow-front-end misreported as
    PARTIAL; dry-run hard-fail exited 0) and re-verified. LIVE GATE PENDING
    (user approval + supervised session; top risk = session-startup dialog,
    prereg RISK A).
  - E4 adjudication table: docs/TYPE_GAP_ADJUDICATION.md (see night+1 entry).
- 2026-07-16 (night): Phase 0 remainder EXECUTING - four executors launched in
  parallel (user go 2026-07-16): E1 console-capture build (0.6), E2 scnx-diff
  harness (0.5), E3 self-launch recipe DRAFT (0.4, no execution), E4 type-gap
  adjudication table (feeds 2.1). All offline; supervisor gates before anything
  is committed. Phase 1 session script DRAFTED by supervisor at
  docs/PHASE1_SESSION_SCRIPT.md (pre-registered P1-A..D; both platoon and
  company cross the 18.1-18.4 km band to keep controller class and band
  un-conflated; open decision D1 = how to set 20x natively, since the GUI
  toolbar caps at 15). Awaiting user: D1, and whether the C++ repo's untracked
  tools/ dir should be preserved on the probe branch.
- 2026-07-16 (night, +1): E4 LANDED and supervisor-ACCEPTED -
  docs/TYPE_GAP_ADJUDICATION.md (gaps 1-3 + ArmorCoHQ decision item + 4 open
  policy questions; every cited template spot-checked on disk; the
  no-AggregateLevel-include claim independently re-verified by grep).
  Gating byproduct registered in ground truth 0.0 item 2: the LF/HU controller
  boundary is per-template wiring, NOT echelon (Stryker Rifle Platoon is PLT
  yet wires HU) - gives a same-echelon probe pair that de-confounds echelon
  from controller. AWAITING USER: the 4 decision items + 4 policy questions in
  TYPE_GAP_ADJUDICATION.md.
- 2026-07-16 (same evening): **Phase 0.1 / 0.2 / 0.3 DONE** - deliverable
  docs/VRF_GROUND_TRUTH.md (supervisor-gated; load-bearing claims spot-verified against
  headers/docs; read its sec 0.0 cross-findings first). Headlines: yellow badge = unread
  Object Console WARNINGS + a remote callback exists to capture them (0.6 tool is the
  first build); platoon vs company templates wire DIFFERENT movement controllers
  (matches the live per-class split - pre-registrable); type fidelity quantified
  (~64/128 COA-STP1 units mis-mapped, ~49 generic, ~15 lone tanks); remote
  saveScenario-to-scnx confirmed (0.5 unblocked); paged-region movement behavior
  UNDOCUMENTED (MAK question material). REMAINING IN PHASE 0 AS OF THAT DATE: 0.4
  (self-launch gate, needs one live probe), 0.5 (scnx harness build), 0.6
  (console-capture build). [ALL THREE ARE NOW DONE - 0.5 and 0.6 built and gated,
  0.4 gate PASSED 2026-07-18. This line is historical; see the Status TOP entry.]
  Next live session = Phase 1 (scripted; open the Object Console panel FIRST).

## Lessons learned (2026-07-16 fix session; register in behavior, not just here)

- L1 READ THE TOOL'S OWN DIAGNOSTICS FIRST. VR-Forces broadcast per-unit warning
  messages the entire time (the yellow badges); days of black-box probing happened
  while the vendor channel sat unread. For ANY third-party system: subscribe to its
  console/diagnostic surface before building custom instrumentation around it.
- L2 CHECK THE API CONTRACT BEFORE ACCEPTING A MECHANISM. The plan's leading suspect
  (SetAltitude re-burying units) died on one header read (arg = aboveGroundLevel).
  One hour of header reading killed a wrong fix before it was built.
- L3 SINGLE-CONSTANT PROBES ON THE ORACLE ARE CHEAP AND DECISIVE. CPP-ALT-1 (one
  constant on the pristine C++, probe branch, ~1 hr) settled code-independence of the
  root cause and exonerated altitude for runaway in the original too.
- L4 RECONCILE EYES AND TELEMETRY - BOTH WAYS. The user's GUI impressions were signal
  (circle -> on-terrain check; "outside routes" -> offset paths; "makes more sense" ->
  partly the 1x-vs-20x clock), and telemetry stayed the oracle (the "nothing moved"
  run had textbook arrivals missed in a 5-min window). Normalize CLOCK and VIEWPORT
  before comparing impressions across runs.
- L5 TIMESTAMP BASES DIFFER: app/tool logs stamp UTC, the machine runs local - check
  Get-Date before comparing (cost a real build-freshness scare).
- L6 FOUNDATION BEFORE PROBES. When a mapping layer was never validated against the
  target system's content model, the probe space explodes and results look random.
  The reset (this plan) is the fix for the process, not just the code.
- L7 CONCURRENT EXECUTOR WRITES to one shared doc worked only by append-only
  discipline; next time assign each executor its own file or pre-created section.

## Decisions log

- 2026-07-16 (late): user directed this reset ("learn how to actually successfully use
  vrf... map to elements that will actually work, as close to real types as possible").
  Supersedes the sec 3/3c probe ordering; evidence rules unchanged. Fix-session results
  (buried-birth fix accepted; runaway + split altitude-exonerated) are the inputs that
  make the reset well-posed: what remains broken is on the VRF side of the boundary.
- 2026-07-16 (later): Phase 0 core executed same evening (three parallel Opus
  executors + supervisor gates); lessons learned registered above; RESUME_PROMPT.md
  rewritten for the groundwork phase.
