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

## Decisions log

- 2026-07-16 (late): user directed this reset ("learn how to actually successfully use
  vrf... map to elements that will actually work, as close to real types as possible").
  Supersedes the sec 3/3c probe ordering; evidence rules unchanged. Fix-session results
  (buried-birth fix accepted; runaway + split altitude-exonerated) are the inputs that
  make the reset well-posed: what remains broken is on the VRF side of the boundary.
