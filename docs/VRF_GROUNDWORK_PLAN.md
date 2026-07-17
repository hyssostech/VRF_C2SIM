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

DONE-GATE (final): the Phase-1 native reference scenario, driven end-to-end through
C2SIM by the port, matches its own native-GUI baseline telemetry within documented
tolerances - then COA-STP1 scored the same way.

## 2. Operating model for this plan

- The VR-Forces GUI is now an INSTRUMENT, not just a viewer. Phase 1 is a joint
  user+supervisor session at the GUI; short, scripted, captured.
- Executors do the reading/cataloging/diffing offline; the supervisor gates each
  phase's exit criteria; the user adjudicates type-mapping choices (they know the
  military semantics) and performs GUI steps until the vrfLauncher self-launch recipe
  (Phase 0.4) removes that dependency.
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

Exit criteria: the five deliverables exist, cross-referenced, adversarially reviewed;
open unknowns are listed as QUESTIONS with where the answer lives (doc, probe, or MAK).

## Phase 1 - Native reference baseline (user + supervisor at the GUI; ~1 hour, scripted)

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
- Acceptance (live, reference scenario first): create the Phase-1 force VIA C2SIM init;
  structure-diff vs the GUI baseline ~clean; native-task OUR units from the GUI (they
  must behave like GUI-created ones); THEN task via C2SIM order - telemetry must match
  the Phase-1 baseline.

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
- Phase 1 is the next LIVE session (user + supervisor, ~1 hour, scripted in advance).
- Phases 2-4 are mostly offline; live checks are short and single-purpose.
- No further scattergun live probes: every live minute from here serves a phase exit.

## Status

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
    both exit 0); POS path verified byte-identical vs HEAD. LIVE GATE PENDING:
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
  UNDOCUMENTED (MAK question material). REMAINING IN PHASE 0: 0.4 (self-launch gate,
  needs one live probe), 0.5 (scnx harness build), 0.6 (console-capture build).
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
