# Guidance for the next fixing session (written for an Opus-class model)

Produced 2026-07-12 by a deep multi-agent review of the whole port effort
(five parallel reviewers over the .NET app, the native facade + bridge, the
MAK 5.0.2 docs/data/logs, cross-repo state, and the master docs), followed by
first-hand verification of every load-bearing claim. ASCII-only per repo policy.

HOW THIS DOC RELATES TO THE OTHERS: it does NOT replace RESUME_PROMPT.md /
START_HERE.md / SEMANTIC_MAPPING.md - read those first as usual. But where this
doc CORRECTS them (sec 2 below), this doc wins: it carries newer, verified
evidence. Fold each correction into the master docs as you act on it.

Everything below is tagged:
- [VERIFIED] = re-checked first-hand during this review (file/line/command cited).
- [AGENT] = found by a reviewer with citations, not independently re-run.
- [HYPOTHESIS] = the leading explanation; its falsification test is given.

---

## 1. Orientation (do this before anything else)

1. Read, in order: `docs/START_HERE.md`, `docs/PORT.md` (sec 6, 8, 10),
   `docs/SEMANTIC_MAPPING.md` (esp sec 5), `docs/RUNBOOK.md` (sec 0, 3, 4, 6, 7, 8),
   then this doc. Run `git log --oneline -10` here and in the fork.
2. The port repo VRF_C2SIM is the single source of truth. The C++ repo
   `c2simVRFinterfacev2.36` is a FROZEN parity oracle - do not develop there;
   its docs are stale (sec 2.6 below).
3. CLOUD vs LOCAL: a cloud checkout can do docs/design/source-editing only.
   Builds, offline selftests, and live runs need the local machine (MAK + VS18 +
   VR-Forces). Every experiment in sec 4 is LIVE-GATED unless marked offline.
4. Non-negotiables (full text in RUNBOOK - violating these destroys state):
   - NEVER force-kill a joined federate; clean-stop via `tools/StopIface`.
   - NEVER push an init to a running interface (it resigns).
   - Do NOT restart the c2sim-server container habitually (degrades the Docker
     loopback proxy). Test: raw TCP connect to 127.0.0.1:61613 must be near-instant.
   - Fresh `Vrf__ApplicationNumber` every run. RTI 4.6.1 on PATH for live runs
     (4.6b is build/offline only). License env var from Machine scope.
     cwd = C:\MAK\vrforces5.0.2\bin64 + `--contentRoot=<exe dir>`.
   - Build the bridge with VS18 MSBuild, the app with
     `DOTNET_CLI_USE_MSBUILD_SERVER=false dotnet build ... --disable-build-servers`.
   - Run the offline selftests before and after any change:
     `--translator-selftest` 18/18, `--parse-init` 80/49/4, `--parse-order`,
     `--report-selftest` 9/9, `--sequencer-selftest` 12 checks (was 5 pre-P0),
     `--verb-selftest` 28/28.
   - `tools/ResetVrf <freshAppNo>` (or a GUI scenario reload) between heavy runs;
     entity accumulation across runs makes creates stop reflecting.

---

## 2. What this review changes (corrections to the recorded state)

### 2.1 ROOT CAUSE FOUND for the stuck disaggregated aggregates

The docs' open question #1 ("what actually moves disaggregated COA-STP1
aggregates?") is now answered with direct evidence. Mechanism:

- VR-Forces moves a disaggregated unit by computing the leader's path and
  parallel formation paths for the members; a resolvable formation is a hard
  prerequisite (MAK help: vrf_closeFormationVsReorganization.htm "How Units
  Move"; moveAlongTasks.h: lead subordinate + others follow in formation). [AGENT]
- Every aggregate the interface creates arrives with formation name
  "column-left", which is invalid for every unit type we create. [VERIFIED:
  `grep -c 'invalid formation name' C:\MAK\vrforces5.0.2\bin64\vrfSim.log`
  = 128 lines of `<unit>: Aggregate state has invalid formation name "column-left"`.]
- Formation names are defined PER UNIT TYPE in the .entity files, and the
  spellings are inconsistent across types. [VERIFIED by reading the files:]
  - `EntityLevel/vrfSim/Ground_Aggregate.entity`: catch-all
    matchType="3:11:1:-1:-1:-1:-1:-1", formations LOWERCASE
    "line"/"column"/"wedge"/"vee".
  - `Tank Company (USA).entity`: matchType="3:11:1:225:5:2:-1:-1", formations
    Title-Case "Column"/"Line"/"Wedge"/"Vee".
  - `Infantry Battalion.entity` and `Artillery Battalion.entity`: formation
    list EMPTY (`<formations paramName="formation-list" autoLayout="0"/>`) -
    no formation can EVER resolve for types that match these.
- The port creates exactly 5 aggregate DIS types (UnitTranslator.cs:110-122)
  [VERIFIED]: Scout 11.1.225.2.1.1.0, ArmorPlatoon 11.1.225.1.1.3.0,
  ArmorCompany 11.1.225.5.2.0.0, ArmorCoHQ 11.1.225.5.20.0.0,
  MobileIrregular 11.1.0.13.34.0.1.
- NO aggregate .entity matches the Scout or ArmorPlatoon types [VERIFIED:
  grep over EntityLevel + C2simEx matchTypes returns nothing for
  3:11:1:225:2:* or 3:11:1:225:1:*] -> they fall back to Ground_Aggregate ->
  their valid formations are LOWERCASE. ArmorCompany matches Tank Company
  (USA) -> Title-Case. So a global `SetAggregateFormation(uuid, "Wedge")`
  (Title-Case) resolves ONLY for the company-matched types.
- That exactly predicts the observed "Wedge moved ~3/32": the movers were the
  Title-Case-matched types; the scout/platoon (Ground_Aggregate-matched)
  types stayed on an invalid formation and froze. [HYPOTHESIS - the per-type
  case sensitivity of the lookup is strongly indicated ("invalid formation
  name" shows exact-string validation) but not yet proven; sec 4 E1 is the
  discriminating test.]

Unexplained residue (record what you observe, do not paper over it): in the
GOLDEN run, 11.MechBn moved with NO formation ever set while its twin
14.MechBn froze (PORT.md sec 5). So an invalid formation does not freeze 100%
of units 100% of the time - vrfmodel.dll also carries a fallback string
"formation is uninitialized. Setting formation based on current subordinate
positions" [AGENT], and 14.MechBn was disaggregated to "2 of 4" subordinates.
Subordinate completeness likely interacts with the formation fallback. The
fix ladder in sec 4 works regardless, but log the per-unit outcomes.

### 2.2 The MoveIntoFormation "settled negative" is UNSAFE as recorded - reopen it

RESUME_PROMPT.md records "MoveIntoFormation does NOT move the disaggregated
COA-STP1 aggregates ... strong hypothesis: it needs AGGREGATED sets. RULED
OUT." Three problems:

1. MAK's own help says the OPPOSITE of the hypothesis: "You can order a
   disaggregated ground unit to move to a formation at a specified location...
   Moving to a formation causes the members of the unit to travel through the
   terrain into the new formation." [VERIFIED:
   doc/help/Content/Tasks/MovementTasks/FormationMoveInto.htm. For AGGREGATED
   units it is merely equivalent to a Set-Formation request.] The
   aggregate-move-into-formation-controller is present in the disaggregated
   movement sysdefs. [AGENT]
2. The experiment never repaired the invalid state formation: the
   MoveIntoFormation path REPLACES SetAggregateFormation + moveAlongRoute
   (VrfC2SimService.cs:614-638 early-returns before the Wedge enrichment)
   [VERIFIED], so every target aggregate still held invalid "column-left"
   when tasked. If the formation controller cannot resolve the CURRENT
   formation, the transition plausibly aborts (vrfmodel.dll: "Setting new
   form. Old %s; new %s") [AGENT]. And "Wedge" itself was the wrong case for
   most of the 11 units (sec 2.1).
3. The experiment was confounded by orchestration (sec 2.4): "35 aggregates"
   is ~35 dispatches to only 11 distinct performers [VERIFIED: COA-STP1 has
   11 distinct PerformingEntity uuids across 42 tasks], each retasked up to 4
   times during the run.

ACTION: keep `Vrf:MoveIntoFormation` default-off, but strike "RULED OUT" from
the docs; re-test it per sec 4 E2 only after E1 lands. Also note the app
sends FireAtTarget/Breach in the same tick right after MoveIntoFormation for
units with resolved targets (:625-636), which would immediately replace the
formation move - not a factor in the recorded run (all COA-STP1 targets are
self-targets and were guarded off) but it must be fixed before synthetic
multi-verb experiments (sec 3, P0.3).

### 2.3 COA-STP1 is more degenerate than recorded: ALL 42 tasks self-target

[VERIFIED by parsing data/COA-STP1_Order.xml]: every one of the 42 tasks has
AffectedEntity == PerformingEntity - not just the 19 ATTACK-family tasks the
docs record. Consequences:

- BREACH (3 tasks) and ESCRT (1 task) can NEVER dispatch their Layer-2 tasks
  from this order, at ANY run length: the distinct-target guards
  (VrfC2SimService.cs:512-518, :527-528) [VERIFIED] correctly reject the
  self-reference and degrade to movement. The docs' explanation "gated tasks
  did not dispatch in the run window; needs a longer run" is WRONG for
  Breach/Escort (T13/T19 are not even temporally gated [AGENT]); it is right
  only for SCREEN/PatrolRoute (all 3 SCREEN tasks are gated; T24 also has no
  Location and can never patrol [AGENT]).
- RESUME_PROMPT's "BREACH obstacles are MAP GRAPHICS ... TryResolveVrfUuid
  misses them" is factually wrong: the order has zero MapGraphicID elements;
  resolution SUCCEEDS and the distinct-check rejects it. [AGENT - mechanism;
  the 42/42 self-target base fact is VERIFIED] Fix the doc; the coa-gpt
  feedback (sec 5) changes accordingly.
- Exercising Breach/Escort/Screen therefore REQUIRES synthetic orders
  (sec 4 E5). No amount of re-running COA-STP1 will do it.

### 2.4 Two real orchestration defects (fix before ANY new experiment)

These corrupt both the outbound reports and the experimental evidence:

DEFECT A - task-completion misattribution. `_currentTaskUuidByName[unit.Name]`
is overwritten on EVERY dispatch (VrfC2SimService.cs:460-461) and read back at
completion by unit name only (:787-797) [VERIFIED]. The VRF completion
callback carries only unitMarking + task-type string (VrfFacade.cpp:203-211)
[AGENT]. With 10 of 11 COA-STP1 units carrying 4 tasks each, a completion
almost always gets attributed to a LATER task than the one that completed:
the TASKCMPLT report names the wrong task uuid AND `_sequencer.CompleteTask`
releases the wrong successor's gate.

DEFECT B - predecessor-timeout dispatch burst. Every task's
`WaitForStartAsync` starts at ORDER ARRIVAL (fire-and-forget loop :415-417),
and on PredecessorTimeout the successor "dispatches anyway" (:427-431)
[VERIFIED]. All gated tasks time out together, so each unit gets a burst of
retasks; VRF executes ONE task at a time and a retask replaces the in-flight
task, so predecessors get cancelled mid-route and which task finally "sticks"
is arbitrary. The live run.log shows the burst ("did not complete within 30s;
dispatching anyway" x12+) [AGENT]. This is the main confound of every
COA-STP1 aggregate experiment: with 11 units executing one task at a time,
at most ~11 route completions were ever possible, not 32.

### 2.5 "Advance then engage" is message-ordering only

FireAtTarget/Breach are issued in the SAME callback immediately after
MoveAlongRoute (VrfC2SimService.cs:737-749) and immediately after
MoveToLocation / MoveIntoFormation [VERIFIED]. Since a new task replaces the
current one, the engage step likely CANCELS the advance the moment both are
real. Unobservable so far (all real orders self-target), but it invalidates
any synthetic advance+engage experiment until fixed (P0.3).

### 2.6 Cross-repo / hygiene facts

- Push state [AGENT, from git]: port main == origin/main == 4699a27; fork
  dev/sdk-fixes == origin == 7200698. RESUME_PROMPT's "all pushed" is TRUE
  (its main=d3a8309 figure is just 2 doc commits stale). Port START_HERE.md
  is the stale one (claims 29-ahead/unpushed, HEAD 340d608, and still lists
  MoveIntoFormation as a NEXT candidate in one spot).
- The C++ repo has NO git remote - the golden-trace regeneration rig exists
  on one disk only [AGENT]. Its docs are systematically stale (live proof
  "not landed", formation fix "unverified", branches "unpushed" - all
  falsified) and its RESUME_PROMPT routes a fresh session to the wrong repo.
- The C++ working tree holds the uncommitted 16-line Wedge spike (superseded
  by the port's live-verified `Vrf:AggregateFormation`), modified docs, and
  untracked docs/RUNBOOK.md + docs/TASK_EXPANSION_PLAN.md + the occupy assets.
- occupy_position (Lua script + data/occupy-test package) was authored and
  statically validated, never wired, never run [AGENT]. SEMANTIC_MAPPING maps
  OCCUPY to native DtHoldUntilTask; the script is a parked fallback.
- 6 tools csproj files carry absolute SDK ProjectReference paths
  (PushInit/PushOrder/ListenReports/SdkVerify/StopIface/StompProbe) [AGENT];
  ResetVrf and the main app are already relative.

---

## 3. P0 - fixes that must land BEFORE the next live experiment

STATUS 2026-07-12: P0.1-P0.4 LANDED (same-day follow-up session). P0.1-P0.3 +
the P0-adjacent cleanups are implemented in the app (InFlightTracker.cs,
TaskSequencer two-phase wait + NotifyDispatched/NotifyAbandoned,
Vrf:PredecessorTimeoutPolicy default skip, Vrf:EngageFallbackSeconds, FIFO
route matching, dup-init guard, loud ClientId-mismatch error, order-parse
warnings); all six selftests green (--sequencer-selftest now 12 checks,
covering attribution + the dispatch-relative window). P0.4 doc corrections
folded into SEMANTIC_MAPPING / RESUME_PROMPT / START_HERE / PORT.md. The
golden MOVE-order live re-verification (end of this section) is still
PENDING - do it in the next live session, before/with E1.

All are offline-verifiable (build + selftests); none changes golden parity
for single-task-per-unit orders (the golden orders are 13 x MOVE, one task
per unit, so attribution and timeout paths never fired there).

P0.1 Completion attribution (DEFECT A). In VrfC2SimService.cs:
  - Replace the last-write `_currentTaskUuidByName` with a per-unit in-flight
    record written at dispatch: unit name -> (taskUuid, expected VRF task
    kind, dispatched-at). On dispatching a NEW task to a unit that has an
    in-flight record, mark the old record SUPERSEDED (log it; do NOT let a
    later completion release its gate as if it completed).
  - On OnVrfTaskCompleted, attribute to the unit's current in-flight record;
    if e.TaskType clearly mismatches the expected kind (e.g. a "patrol" task
    type completing when a move-along was dispatched), log the anomaly.
  - Keep the TASKCMPLT report shape identical (ReportBuilder unchanged).
  - Add a `--sequencer-selftest` case: dispatch A then B to the same unit,
    complete once -> B's uuid is reported, A's gate is NOT released.

P0.2 Timeout policy (DEFECT B). Two changes:
  - Start a successor's predecessor-timeout clock at predecessor DISPATCH
    (or first activity), not at order arrival - i.e. WaitForStartAsync should
    time out relative to when the predecessor actually started running.
  - On PredecessorTimeout, do NOT blindly retask a unit with an in-flight
    task. Add `Vrf:PredecessorTimeoutPolicy` = `skip` (default: log + do not
    dispatch) | `force` (today's behavior, kept for compatibility) |
    `whenIdle` (dispatch only when the unit has no in-flight task).
    NOTE: check VrfSettings.cs for the actual TaskPredecessorTimeoutSeconds
    default - the docs say 600 s but the live run logged 30 s, so a run
    override existed; make the experiment configs explicit.

P0.3 Make advance-then-engage completion-gated. Move the deferred
  FireAtTarget/Breach dispatch from "same tick as MoveAlongRoute"
  (:737-749, :625-636, :569-582 in-place paths stay as-is) to "on completion
  of that unit's move task" using the P0.1 in-flight record. Keep a
  configurable fallback delay for moves that never complete.

P0.4 Doc corrections (cheap, prevents the next session acting on wrong
  records): update SEMANTIC_MAPPING.md sec 5 Unit 4 (strike RULED OUT,
  record the reopening rationale + MAK help citation), RESUME_PROMPT.md
  dead-ends (MoveIntoFormation wording, breach map-graphics claim, "35
  aggregates" -> ~35 dispatches to 11 units, ALL 42 tasks self-target),
  and port START_HERE.md (push state, HEAD, remove stale Unit-4 NEXT).

Smaller P0-adjacent cleanups (do with P0.1-P0.3 since you are in the file):
key `_pendingRoute*` maps by task uuid instead of TaskName+" ROUTE" (dup
names collide); guard duplicate init delivery; warn on non-STREND
ActionTemporalAssociationCode (OrderParser reads only the first relationship
and assumes start-after-end [AGENT]); fail loudly when 0 units match ClientId
(appsettings ships ClientId=STP; the tracked COA-STP1 init needs C2SIM).

Verification for all of P0: rebuild app (no bridge rebuild needed), run all
six offline selftests, re-run the golden MOVE order live once (RUNBOOK sec 7)
and diff behavior vs golden-trace-02 (49 creates + 4 areas, 1.BdeHQ +
11.MechBn move + TASKCMPLT).

---

## 4. P1 - the aggregate-movement fix (experiment ladder)

Run these IN ORDER; each has a decision rule. Shared setup for every
experiment:
- De-confounded synthetic order: ONE task per aggregate, NO temporal
  relationships, distinct route per unit, distinct TaskNames. Derive it from
  COA-STP1 (same 11 performers) or write a 5-unit variant. Without P0 fixes
  this is still interpretable (no gates, one task per unit = no bursts, no
  misattribution).
- Fresh federation (ResetVrf or GUI reload), fresh appNumber, Vrf:ClientId
  matched to the init's SystemName.
- INSTRUMENT: watch the backend log during the run -
  `Get-Content C:\MAK\vrforces5.0.2\bin64\vrfSim.log -Tail 0 -Wait |
   Select-String -Pattern 'formation'`
  This is the direct oracle: "Aggregate state has invalid formation name"
  lines tell you formation resolution failed; their absence after your
  set-formation tells you it succeeded. [VERIFIED the log carries these.]

LADDER SUPERSEDED 2026-07-12 (post-E1): a three-agent sweep of the vendor's own
docs/headers/content produced docs/UNIT_MOVEMENT_RESEARCH.md - read THAT for the
documented unit-movement model, the grounded E1 diagnosis (uninitialized birth
formation + unestablished leader + snap semantics + stacked-spawn geometry), and
the REVISED plan R1-R7 (set-formation at CREATE time, ReorganizeAggregate, member
telemetry, available-formations query, golden-scenario-first micro-experiment).
E2/E3/E4 below remain reference material; execute R1-R7 instead.

E1 STATUS 2026-07-12: RAN LIVE (same-day session; full record PORT.md sec 10 "E1 RUN").
Implemented as `Vrf:AggregateFormation=auto` + data/E1_Formation_Order.xml. All per-type
names ACCEPTED (the invalid-formation oracle stayed quiet) and the entity control
completed, but NO aggregate route-marched: companies ran away 150+ km, platoons shuffled
in place, CoHQs were subordinate-scattered at creation ("AR HQ Sec ... Column-Left is an
invalid or malformed formation"). Decision rule outcome: formation-name resolution alone
FALSIFIED as the sufficient fix; the prior "Wedge moved ~3/32" is reclassified as a
suspect runaway artifact. E2 stays parked. NEXT: E1b - repeat on the golden STP init
(dispersed unit positions; its 14.MechBn genuinely marched with Wedge) to discriminate
the scenario-data (stacked/identical coordinates) hypothesis; then E3/E4.

E1 (highest confidence, cheapest) - per-matched-type formation names:
  - Change: in the aggregate move path (the current Wedge enrichment block,
    VrfC2SimService.cs:640+), set the formation per the CREATED type instead
    of one global name. Two options; do (a), fall back to (b):
    (a) static map keyed by the unit's CreationPlan/DIS type:
        Scout 11.1.225.2.1.1.0     -> "column" (lowercase; Ground_Aggregate)
        ArmorPlatoon 11.1.225.1.1.3.0 -> "column" (lowercase; Ground_Aggregate)
        ArmorCompany 11.1.225.5.2.0.0 -> "Column" (Title-Case; Tank Company (USA))
        ArmorCoHQ 11.1.225.5.20.0.0   -> try "Wedge"; if the invalid-formation
          line appears for CoHQ units, they fell back to Ground_Aggregate ->
          use lowercase (their exact match is ambiguous: aggregate-Company-HQ-
          Friendly matchType is 3:11:1:225:5:20:1:0, our type ends :20:0:0).
        MobileIrregular 11.1.0.13.34.0.1 -> "Wedge" (C2simEx def, Title-Case)
        ("column" over "wedge" for route march; either works for the test.)
    (b) dual-set: issue SetAggregateFormation twice per aggregate - lowercase
        then Title-Case. An invalid name in a Set command is a logged no-op
        [AGENT: vrfmodel.dll "Formation %s in Set command is an invalid
        formation."], so whichever spelling is valid wins. Crude but requires
        zero type-matching knowledge.
  - Expected: ALL ground aggregates track their routes; the vrfSim.log stops
    showing invalid-formation lines for tasked units after the set.
  - Decision rule: if scout/platoon units now move where Title-Case "Wedge"
    failed -> case/type resolution CONFIRMED as the root cause; adopt (a) as
    the durable fix and record it in PORT.md sec 10. If units still freeze
    WITH the backend log showing their formation resolved -> the formation
    hypothesis is falsified for those types; go to E3/E4 and record the
    negative with the log excerpt.

E2 - reopen MoveIntoFormation (only after E1 moves units):
  - Change: prepend the E1 formation set (+ the existing ~0.5 s settle)
    before issuing DtMoveIntoFormationTask; run one unit first (a Tank-
    Company-matched aggregate), then the synthetic order.
  - Expected if the sec-2.2 reading is right: the set moves into formation at
    the destination (backend log shows the DtAggregateMoveIntoFormationController
    "Setting new form" line [AGENT]).
  - Decision rule: works -> MoveIntoFormation becomes the Layer-2
    MoveInFormation composition (SEMANTIC_MAPPING table row). Fails while
    E1+moveAlongRoute works -> re-record the settled negative, now clean.

E3 - runtime formation discovery (the durable, type-agnostic version):
  - Add facade support for DtRequestAvailableFormationsAdmin ->
    DtAvailableFormationsAdmin (include/vrftasks/availableFormationsAdmin.h,
    requestAvailableFormationsAdmin.h; radio type "request-available-
    formations") [AGENT]. On aggregate creation, query; cache uuid ->
    formation list; before any aggregate move, set the first valid formation
    (prefer column/Column). Needs a bridge rebuild (VS18) - budget for it.
  - This also covers FUTURE types (e.g. anything matching Infantry/Artillery
    Battalion, whose formation lists are EMPTY [VERIFIED] - for those the
    query returns nothing and the correct behavior is: log clearly and fall
    back to E4 subordinate tasking; no formation name can ever work).

E4 - fallbacks, only if E1-E3 leave types stuck:
  - E4a: task SUBORDINATE entities directly (documented+supported: unit
    members can be tasked independently and revert to superior tasking on
    completion [AGENT: UnitMembersTaskIndependently.htm]). Fan out the move
    to subordinate uuids (they stream through the object-created callbacks).
    Forfeits unit-level task semantics; keep unit-level reports.
  - E4b: create aggregates AGGREGATED / createSubordinates=false. LOW
    confidence: docs claim aggregated-state movement, but NO EntityLevel type
    has an aggregated-movement component and C2simEx's referenced
    ground-aggregated-movement.sysdef does not exist on disk [AGENT]; also
    aggregated units do not fight. 10-minute experiment; expect a negative.
  - E4c: re-key created DIS types to the C2simEx custom types (AR Scout
    11.1.225.14.30.0.x etc. - the C++ even contains a dead
    DtObjectType(11,1,225,14,30,0,0) in createScoutUnit suggesting original
    intent [AGENT]). Opt-in only: changes published DIS types = parity break.

Record every E-run's outcome (movers/total by type, backend-log excerpts) in
PORT.md sec 10 as you go. The golden 11.MechBn-vs-14.MechBn anomaly (sec 2.1
residue) should get explained or explicitly re-flagged by these runs.

---

## 5. P2 - exercise the remaining Layer-2 verbs + coa-gpt feedback

1. Synthetic orders (COA-STP1 cannot exercise these - sec 2.3):
   - BREACH: distinct AffectedEntity pointing at an init-created obstacle-like
     entity; observe whether DtBreachTask with only setBreachTarget (no
     StPt/EndPt) behaves sanely [AGENT flag: unverified whether VRF derives
     the breach points]. Pattern: scratchpad/synthetic_attack_order.xml from
     the Unit-3 test.
   - ESCRT: distinct escorted entity. FIRST fix FollowEntity's zero offset
     (P3) - today the follower stations itself ON the leader.
   - SCREEN: ungated variant of a COA-STP1 SCREEN task (T24 also needs a
     Location added).
   - ATTACK behavior observation: does the synthetic-attack unit close and
     destroy the target, ROE permitting (recorded open question).
2. coa-gpt data-quality memo (update the earlier feedback): (a) ALL 42
   COA-STP1 tasks emit AffectedEntity == PerformingEntity [VERIFIED] - verbs
   that act on an object (ATTACK/BREACH/ESCRT/...) need a real, distinct
   AffectedEntity or the executor can only degrade to movement; (b) timing
   hygiene stands (the 3.3-hr T13 delay; SimulationRealtimeMultiple).

---

## 6. P3 - bridge/facade robustness batch (one bridge rebuild)

From the adversarial facade/bridge review [all AGENT with file:line, spot-
checked plausible]. Bundle into one VS18 rebuild + SmokeTest + one live run:

1. Wrap the four Raise* calls in VrfBridge.cpp's native thunks (~:400-431)
   in try/catch: a throwing .NET handler currently unwinds through
   /EHs-compiled MAK frames mid-tick (state corruption risk).
2. Break the gcroot cycle: either convert thunk gcroots to weak handles or
   clear the facade's std::functions in Dispose; document that an undisposed
   bridge is a permanent leak.
3. TryGetEntityGeodetic (VrfFacade.cpp:574-581): the static_cast fallback
   works by vtable-slot coincidence (entityStateRep() is virtual; entity and
   aggregate happen to declare their accessor at the same slot). Fix the
   misleading comment now; harden by consulting the UUID manager's typed
   lists (open question: why dynamic_cast fails - cross-DLL RTTI vs actual
   concrete type) before ever passing non-unit uuids.
4. StartupConfig.Protocol is dead (compile-time #if DtHLA); throw
   NotSupportedException for VrfProtocol.Dis in the HLA-only bridge.
5. FollowEntity: set a trailing offset, e.g. setOffsetVector(-25, 5, 0),
   per the MAK header example (required before the ESCRT test).
6. Small: delete the leaked DtScenario in SetExerciseStartTime (and verify
   its real semantics - header reads as scenario-save helper, not a push);
   guard VrfFacade::Start() re-entry; null-check _facade in bridge methods
   (throw ObjectDisposedException); bound TaskSequencer._completions.

---

## 7. P4 - parity/report polish (after P0-P2)

- Report enrichment: EntityHealthStatus (bridge must surface health),
  aggregate-component dedup, multi-content bundling (4140 reports in one run
  is too chatty).
- Deferred sec-6 bug fixes: distinct C2SimUuid/VrfUuid types (makes the
  SetTarget parity no-op fixable for real); aggregate heading;
  OnObjectInitialization stub (orders that task via named map-graphic routes).
- Formal golden-trace message diff (never performed).

---

## 8. P5 - repo hygiene (can interleave; ask the user where flagged)

1. C++ repo: commit the modified docs + untracked RUNBOOK.md /
   TASK_EXPANSION_PLAN.md to master WITH a stale-banner at the top of
   START_HERE.md and RESUME_PROMPT.md ("FROZEN parity oracle; source of truth
   is VRF_C2SIM docs/; do not act on status/next-task claims here").
   Commit the 16-line Wedge spike to a side branch
   (spike/aggregate-formation-wedge, message: superseded by the port's
   Vrf:AggregateFormation), then restore a clean master tree.
2. occupy assets: copy data/occupy-test/ + the two script files into the
   port repo (parked Layer-2 fallback; add a one-line pointer in
   SEMANTIC_MAPPING's HoldObjective row), and commit the originals in the
   C++ repo for provenance.
3. Convert the 6 absolute SDK ProjectReference paths in tools/*/*.csproj to
   the relative form the main app uses.
4. ASK THE USER: add a private remote for the C++ repo (only golden-trace
   rig, zero off-disk copies); whether to clean the fork's tracked .vs/
   noise under c2simVRFinterfacev2.33-patched.
5. Keep docs/START_HERE.md, PORT.md, SEMANTIC_MAPPING.md, RESUME_PROMPT.md
   current AS you work (the standing rule).

---

## 9. Verified-vs-assumed ledger (for the falsification-minded)

VERIFIED first-hand in this review:
- vrfSim.log: 128x 'Aggregate state has invalid formation name "column-left"'.
- Ground_Aggregate/Tank Company/Infantry Bn/Artillery Bn .entity formation
  lists and matchTypes as stated in sec 2.1; C2simEx Mobile Irregular.entity
  (matchType 3:11:1:-1:13:34:0:1 wildcard-matches our 11.1.0.13.34.0.1,
  formations Title-Case Wedge/Column/Line/Vee).
- No aggregate .entity matches the created Scout/ArmorPlatoon DIS types.
- UnitTranslator.cs:110-122 creates exactly the 5 DIS types listed.
- COA-STP1_Order.xml: 42 tasks, 11 distinct performers, 42/42 self-target.
- FormationMoveInto.htm: Move Into Formation is for DISAGGREGATED ground units.
- VrfC2SimService.cs: current-task overwrite (:460-461), completion lookup
  (:787-797), timeout dispatch-anyway (:427-431), breach/escort distinct
  guards (:512-518, :527-528), same-tick fire/breach after move (:737-749)
  and after MoveIntoFormation (:625-636), MoveIntoFormation early return
  (:614-638).

Load-bearing but NOT independently re-verified [AGENT]: vrfmodel.dll embedded
diagnostic strings; run.log burst lines; sysdef controller wiring; gcroot /
EH specifics in VrfBridge.cpp; push-state git output; csproj absolute paths.
Spot-check any of these before building a fix directly on top of it.

Open HYPOTHESES with their tests:
- Formation lookup is case-sensitive per type -> E1 decision rule.
- MoveIntoFormation fails only due to unrepaired/wrong-case formation ->
  E2 decision rule.
- ArmorCoHQ type falls back to Ground_Aggregate (inexact match) -> E1(a)
  CoHQ sub-rule.
- The 11.MechBn golden anomaly (moved with no formation set) - watch E-runs.
