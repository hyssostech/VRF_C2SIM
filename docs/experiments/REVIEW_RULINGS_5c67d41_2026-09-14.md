# Cold-start review of feat/tasking-rulings 5c67d41 (Opus, 2026-09-14 ~16:20Z): FIX FIRST - M1 the timed completion loses the race against TaskPredecessorTimeoutSeconds by construction (the chain still dies), M2 two clocks, M3 no hysteresis, M4 stale sim clock, M5 R1 covers areas only;
# Q1-Q4 user rulings (supervisor defaults: superseded -> TASKABRT, Duration on the SIM clock under Vrf:TaskClock, wire ambiguity accepted, DefaultHoldSeconds 60);
# fix pass running.

# COLD-START REVIEW - feat/tasking-rulings @ 5c67d41 (base 02b51de)

Reviewer: Opus cold-start, read-only. Tier HEAVY. Date 2026-09-14.
Worktree reviewed: <repo>/.claude/worktrees/rulings (not modified).
Scope: R4 746c091, R2 1f65f55, R3 0cd8905, R1 0193379, docs 5c67d41.

VERDICT: **FIX FIRST** - five MAJOR items, one of which (M1) means the branch does not
achieve its own stated purpose on the order it was built for, plus one question that needs
a USER ruling (superseded-task completion) before merge.

---

## 0. WHAT I RAN

- `dotnet build src/VrfC2SimApp -c Release -p:BridgeConfig=Release-5.2` - green, 0 warnings.
- `--rulings-selftest` - ALL CHECKS PASSED (41: R4 19, R2 7, R3 6, R1 9).
- `--parse-order data/COA-STP1_Order.xml` - 42 tasks, durations 42/42 (32 x 4800000 ms,
  10 x 7200000 ms), start delays 41 x 0 + 1 x 12000000 ms, geometry 0 MapGraphicID /
  33 embedded / 9 none. Matches the commit message exactly.
- 12 other offline suites green: report, sequencer, verb, fanout, placement, compose,
  arrival, stall, parse, name, preflight, typemap (783 checks).
- NOT exercised (need the native VrfBridge.dll, absent from this worktree's bin, and
  --parse-init likewise): translator, destack, terrain. None of them touches changed code.
- ASCII check (bytes >126 or control other than TAB/LF/CR) over the 9 changed sources and
  the 2 docs: 0 offenders. Control file with a UTF-8 e-acute returns 2 - the instrument works.
- Independent census of data/COA-STP1_Order.xml and _Initialization.xml (task graph,
  taskee distribution, area geometry) - numbers below.

---

## 1. FINDINGS TABLE

Severity: MAJOR = wrong behaviour or a silent stop in the demo path. MINOR = wrong in a
reachable but narrower case, or an accuracy/logging defect. NOTE = record only.

### MAJOR

| # | file:line | input -> wrong outcome | fix |
|---|-----------|------------------------|-----|
| **M1** | VrfC2SimService.cs:1955, :1978; TaskSequencer.cs:112-122; TimedCompletionPolicy.cs:109-119 | **The timed completion loses the race against `TaskPredecessorTimeoutSeconds` by construction, so R4's chain still dies.** The successor's phase-2 gate is `Task.Delay(predecessorTimeout - (now - pred.DispatchedAtUtc))`, i.e. it expires at `dispatch + timeout`. The timed completion fires at `dispatch + Duration x DurationScale + anchor + cadence`: `MarkDispatched` calls `NotifyDispatched` (:2621) BEFORE `_timed.Register` (:2648), and `Advance`'s first walk after registration ANCHORS ONLY (`LastClock` NaN -> `continue`, TimedCompletionPolicy.cs:109-114), so Elapsed starts accruing at the first 1 s walk after dispatch. The completion is therefore **strictly later** than `dispatch + Duration`. Concrete: (a) harness default `TaskPredecessorTimeoutSeconds=600`, `DurationScale=1.0` -> all **31 gated COA-STP1 tasks** (21 behind a 4800 s predecessor, 10 behind a 7200 s one) time out and are SKIPPED with TASKABRT -> 11 dispatches, not 42; (b) `appsettings.Demo.json:31` sets 7200 -> the **10 tasks gated on a PT2H predecessor** lose by 1-2 s, deterministically. The R4 commit's own goal ("42 dispatches, not 9", assessment gate 2) is unreachable at scale 1.0. | The predecessor timeout must be derived from the predecessor's own end time, not from a fixed constant: e.g. `timeout = max(configured, ScaleOrderMs(pred.DurationMs)/1000 + margin)`, or gate the successor on the predecessor's armed end time directly. At minimum apply `DurationScale` to `TaskPredecessorTimeoutSeconds` and document the constraint `timeout > max(Duration) x DurationScale`. |
| **M2** | VrfC2SimService.cs:3140-3152 vs TaskSequencer.cs:131-132, :112; VrfSettings.cs:277 | **Two clocks.** The Duration is served on the clock `Vrf:StallClock` picks (SIM when asked for and readable); the start delay and the predecessor gate are pure WALL (`Task.Delay`). Live gate 5 asks for `StallClock=sim`. At the measured COA-STP1 sim ratios (0.27x-0.73x, memory `vrf-frame-mode-not-multiplier`), 4800 SIM s is 6.6k-17.8k WALL s, so even the 7200 s demo timeout skips every successor - gate 5 and gate 2 cannot both pass as configured. Second consequence: with the DEFAULT `StallClock=wall`, a C2SIM Duration (a statement about SIMULATED time) is served in wall seconds, which is a semantics choice nothing in the branch states. Third: R4's completion semantics are now controlled by a knob named for the stall watchdog - an operator turning on `StallClock=sim` for C16 silently changes when every task completes. | Measure the start delay and the predecessor timeout on the SAME clock as the Duration; give R4 its own `Vrf:TaskClock` (defaulting to StallClock) rather than borrowing the watchdog's knob; state the default explicitly in VrfSettings and the runbook. |
| **M3** | TimedCompletionPolicy.cs:109-115; VrfC2SimService.cs:3145-3148; StallPolicy.cs `NextClockMode` | **Clock-mode hysteresis is applied to the watchdog and NOT to the timed walk, so an alternating sim reader starves every task forever.** `MaybeCompleteTimedTasks` computes `usingSim` from a single raw `SimTimeSeconds()` read; `MaybeCheckStalls` runs the same observation through `StallPolicy.NextClockMode` with `ModeSwitchConfirmations` precisely because a reader that alternates -1 / >=0 at the check cadence is a documented case (StallPolicy pass-2 review finding 8, "the back end briefly out of the list"). In the timed walk a mode flip re-anchors and serves ZERO (:109-114), so an alternation at the 1 s cadence means `Elapsed` never grows: **no task ever completes, no warning, forever**, and every successor is then skipped at the predecessor timeout. The R4 header's claim that using StallPolicy avoids "a second clock abstraction free to disagree with the watchdog" is exactly what is violated - it reads the RAW mode, the watchdog the CONFIRMED one. | Resolve the mode once per tick through `NextClockMode` and hand the held mode to both consumers (or have `MaybeCompleteTimedTasks` read the watchdog's `_stallClockMode`). |
| **M4** | VrfC2SimService.cs:3128-3152; StallPolicy.cs `SimClockStale` | **A STALE sim clock freezes every end time silently.** `VrfFacade::SimTimeSeconds` gates on `backends().count() > 0`, and a back end that stops answering is DEACTIVATED, not removed - the reader keeps returning the last cached value (StallPolicy documents this as review finding 9 and added `SimClockStale` for it). `MaybeCompleteTimedTasks` has no stale detection and no fallback: `usingSim` stays true, `clockNow` is constant, `step <= 0`, so every pending task stops ageing and NO TASKCMPLT is ever emitted again. This is exactly the silent-stop mode R4 exists to remove, reintroduced one layer up. The `StallDetection` default is `false`, so nothing else warns either. | Reuse `StallPolicy.SimClockStale` in the timed walk: warn ONCE and either continue serving on the wall clock or hold with a loud, repeating line. |
| **M5** | VrfC2SimService.cs:1132-1137, :122-125; InitParser.cs:64-76, :147-153; InitModels.cs:43-48 | **R1's graphic map covers 35 of the init's 409 graphics.** `_graphicsByC2SimUuid` is populated only from `init.Areas`, and `InitParser` collects `S.TacticalAreaType` and nothing else. Measured on `data/COA-STP1_Initialization.xml`: 35 TacticalArea, **41 Line, 317 Point, 16 TaskGraphic**. So on the STP re-export R1 is built for (live gate 7, `IncludeMapGraphicIdInTasks=True`), a `MapGraphicID` naming a phase line, an axis of advance (the graphic the doctrine table says MOVE and PENTRT are defined against), a boundary, a decision point or an attack-by-fire position matches nothing, falls through to the embedded Location, and - when the re-export drops the embedded Location in favour of the id - to **R2 in-place**: the unit does not move and the task reports TASKCMPLT at its end time. The unmatched-id line is logged at **Information** (:2082-2084). The claim in the field comment that lines and points "join it unchanged when Vrf:CreateInitLines/Points lands" is wrong twice: nothing writes those rows, and the resolver needs only AUTHORED POINTS, so it does not depend on the create flags at all. | Parse Line and Point graphics into `InitData` (feat/tasking-foundation `0995fab` already has `InitLine`/`InitPoint`) and register them in `_graphicsByC2SimUuid` unconditionally, independent of any create flag. Raise an unmatched `MapGraphicID` to **Warning**. Until then, say in the docs that R1 resolves AREAS ONLY. |

### MINOR

| # | file:line | input -> wrong outcome | fix |
|---|-----------|------------------------|-----|
| m1 | VrfC2SimService.cs:2588-2601 | **A superseded task still reports TASKCMPLT at its authored end time.** `MarkDispatched` logs "the old task will not complete", cancels the pending engage and the R10 fan-out - and does NOT cancel `_timed` for the old uuid. The interface contradicts itself in the report stream. NOT reachable on COA-STP1 under the default policy (measured: 11 taskees, 10 ungated root tasks, **0 taskees with more than one ungated task** - every taskee is a serial chain), but reachable with `PredecessorTimeoutPolicy=force` or `whenidle`, and on any order with concurrent tasks per taskee. | Needs a USER ruling (sec 4, Q1). Engineering default: cancel the timer and push TASKABRT for the superseded task at the supersede point. |
| m2 | VrfC2SimService.cs:2240-2258, :2601-2608 | **R2's in-place dispatch supersedes an in-flight move without issuing any replacing VR-Forces command.** It is the first dispatch kind for which "the old vendor task keeps running" is permanent rather than transient. The old task's eventual completion reaches `SynthesizeUnitCompletion` -> `_inFlight.TryComplete` returns the NEW record -> TASKCMPLT is emitted for the IN-PLACE task, its R4 end time is cancelled, and its successors dispatch early. The in-place branch also clears `_arrivalReported` and `ClearStallState` "because the new VRF task is issued" - no task is issued. Same reachability caveat as m1. | Do not clear the arrival-swallow / stall state on a dispatch that issues nothing; or explicitly stop the unit (a vendor hold) so the in-place claim is true. |
| m3 | VrfC2SimService.cs:2564-2574, stale comment :2131-2137 | **`Unresolved` is relabelled as the R3 ruling and demoted from Warning to Information.** An ATTACK naming an OPFOR entity this interface did not create is a scope/data gap, not "the target IS the objective"; the message now asserts the doctrinal ruling for it. The comment above `attackTargetVrf` still promises "an out-of-scope OPFOR target degrades to advance-only + a warn". | Keep `SelfIsObjective` at Information (correct - R3), return `Unresolved` and `NoTarget` to Warning with their own wording, and fix the stale comment. |
| m4 | OrderParser.cs:174-179, :194 | **The month term is `108000L` = 30 x 60 x 60 s (30 HOURS).** The recorded rationale - "behavior-neutral for the golden trace (all durations are zero); do not correct here (parity-first)" - no longer holds now that `Duration` decides completion: `P00Y01M00DT00H00M00S` would end a task after 30 hours instead of 30 days. COA-STP1 is unaffected (Y/M/D all zero). | Either fix the month term for the Duration path only (the C++ never read Duration, so there is no parity to preserve there) or record the quirk as a KNOWN LIMIT in OrderModels' Duration comment. |
| m5 | VrfC2SimService.cs:4113-4131 | `_timed.Cancel` sits AFTER the empty-taskee early return, so a genuine TASKCMPLT/TASKABRT that cannot be sent leaves the timer armed. Low impact (the later timed report is equally unsendable), but it inverts the comment's own argument ("a completion that is suppressed has still happened"). | Move the Cancel above the taskee guard. |
| m6 | VrfC2SimService.cs:2214-2217, :2261-2268 vs :2186-2192 | **The refusal branch is dead code** - `ForZeroGeometry(performerResolved: true, ...)` is a literal - so after this change the dispatch site never emits its own refusal. Meanwhile the one real "no performing unit" path immediately above it, `TryGetEntityGeodetic` failure, abandons with `NotifyAbandoned` and **no TASKABRT**: STP is left waiting, the exact silence B1 was built to end. | Push TASKABRT at :2186-2192 with the new "no performing unit" wording, and either pass the real predicate into `ForZeroGeometry` or delete the unreachable branch. |
| m7 | TaskGeometryResolver.cs:91-97 | When MapGraphicIDs resolve, an embedded `Location` present on the same task is **discarded silently** - no log line, no comparison. The user's R1 note asked for "precedence MapGraphicID > embedded, **consistency check when both**". | Log "n embedded Location point(s) ignored (MapGraphicID wins)" and, when both are present, report the separation between them. |
| m8 | VrfC2SimService.cs:1948-1949, :2634-2645 | `Vrf:DurationScale` of 0, negative or NaN collapses the Duration to "NO end time armed" but collapses the start delay to "dispatch now" - one scale, two opposite readings. A run at scale 0 dispatches everything at once and completes nothing. | Reject a non-finite or non-positive `DurationScale` at startup (fail loud, clamp to 1.0), rather than per-task warnings on one half only. |
| m9 | VrfC2SimService.cs:2579-2652; SynthesizeUnitCompletion | The timed completion does not pop the in-flight record, so a unit that ran an in-place task is `_inFlight.IsBusy` forever and `PredecessorTimeoutPolicy=whenidle` will never dispatch on it again. (The duplicate-report risk is contained: `TaskStatusPolicy.ShouldEmitComplete`/`ShouldEmitAbort` suppress the later arrival and stall reports - verified.) | Pop the in-flight record for kind `hold-in-place` at the timed completion, or exclude it from `IsBusy`. |

### NOTE

| # | where | note |
|---|-------|------|
| n1 | TaskGeometryResolver.cs:113-123 | The area "centroid" is the **vertex arithmetic mean**, not the polygon area centroid. Measured on the 12 multi-vertex COA-STP1 areas: the mean lies INSIDE the polygon in 12/12 (good), but is 149-1130 m from the true area centroid (HAMILTON 1130 m, SAXONS 1118 m, OILERS 1112 m) against `ArrivalRadiusMeters` 500. 23 of the 35 areas carry a single vertex and are unaffected. A concave or self-intersecting area could put the mean outside; nothing checks. The code comment's "no spherical correction: the difference is centimetres" is about a different quantity and reads as a general accuracy claim. |
| n2 | same | The longitude mean is not antimeridian-safe. Not reachable on Mojave. |
| n3 | :2253-2257; ReportBuilder.BuildTypeSubstitutionReport / BuildTaskStatusReport | R2's derivation rides in a `NameObservation.Marking` - the same field `SubstitutionAnnouncer` uses for TYPE substitutions - and STP discards ObservationReports (STP-800). `TaskStatus` carries only the code and the task uuid; the `why` string is log-only. So **STP cannot distinguish a zero-geometry in-place task from a performed one**: it sees TASKSTRT then TASKCMPLT. That is a consequence of the ruling, not a coding error, but the assessment's "report the derivation" is satisfied only on a channel the consumer drops. |
| n4 | :3110 `TimedCheckSeconds = 1.0` | The cadence is 1 WALL second while the served clock may be SIM. Under fixed-frame-run-to-complete (9x measured on R9) the completion overshoot is up to 9 sim s - immaterial against 4800 s, but it is the term that loses M1's race. |
| n5 | :1132-1137 | An order arriving BEFORE the init leaves `_graphicsByC2SimUuid` empty. The comment covers the create-drain race, not the order-before-init one. |
| n6 | OrderParser.cs:136-143 | `DateTime.TryParse(..., AssumeUniversal)`: an `IsoDateTime` with no offset is read as UTC. Fine for a UTC producer; wrong by the offset for a local-time one. |
| n7 | :3075, :3509 | VERIFIED GOOD: both the arrival check and the stall watchdog skip in-flight records with no destination, so R2's `dest: null` really does keep both off a correctly-stationary unit. |
| n8 | RulingsSelfTest.cs | Test quality. R4's 19 checks are substantive (fake clock, pause, rollback, mode change, chain release through a real `TaskSequencer`) and the commit's fail-first evidence (11 of 19 failing pre-change) is credible for them. R2/R3's 13 checks mostly restate 3-line pure functions: `(d2)` asserts `RefusesForTarget` is false for every enum value, and the method body is `return false` - the check **cannot fail**; `(c4)` compares a const to its own literal. Nothing in the suite exercises `ScaleOrderMs`, `DurationScale`, `AbsoluteStartUtc`, the Duration format variants, or ANY service wiring - so none of M1-M5, m1-m9 could have been caught by it. |
| n9 | - | ASCII clean across all changed files (0 offenders; control returns 2). |
| n10 | - | Build green; 13 suites green. translator/destack/terrain/parse-init not exercised (native `VrfBridge.dll` absent from this worktree). |

---

## 2. MERGE-BACK HAZARDS vs feat/integration (9a18ab0)

| # | hazard |
|---|--------|
| x1 | **Textual conflict, TickLoop.** `b223afd` replaced the six `if (...) MaybeX();` lines with `TickPhase(name, enabled, body)` calls (M1 hardening); this branch adds a seventh line in the same block. Resolution: `TickPhase("MaybeCompleteTimedTasks", _vrf.TimedCompletion, MaybeCompleteTimedTasks);`. If the conflict is resolved by keeping the raw `if`, the new phase becomes the ONLY unguarded one - and it is the one that calls `ReportBuilder` XML serialization on the tick thread. |
| x2 | `MaybeCheckArrivals` gains a third `out` on `_fanOut.TrySynthesizeByTimeout` (integration) in the method this branch's `MaybeCompleteTimedTasks` is appended after. Adjacent hunks; should auto-merge - verify. |
| x3 | `SynthesizeUnitCompletion` gains a `success` parameter on integration. The rulings branch adds no caller. No conflict. |
| x4 | **Semantic, not textual:** merging feat/tasking-foundation `0995fab` (InitLine / InitPoint / `Vrf:CreateInitLines`/`Points`) does NOT by itself make R1 resolve lines and points - registration loops for them must be ADDED at VrfC2SimService.cs:1132. The comment at :122-125 asserting they "join it unchanged" will otherwise stand as a false claim next to code that never writes those rows. See M5. |
| x5 | m1 (NameRegistry keep-prior guard) and m6 (ReportKind.Observation for pre-flight) do not overlap the rulings hunks. |

---

## 3. VERIFIED vs ASSUMED

**Verified by reading the code in this worktree, by running it, or by parsing the data:**
- The `--rulings-selftest` 41 checks pass; 12 other suites pass; build is clean.
- COA-STP1: 42 tasks, 11 taskees, 10 ungated roots, 31 gated, 0 taskees with more than one
  ungated task; 42/42 durations (32 x 4800 s, 10 x 7200 s); 1 start delay of 12000 s;
  9 zero-geometry tasks, 8 of them gated; AffectedEntity == taskee on 42/42.
- Predecessor durations behind the 31 gated tasks: 21 x 4800 s, 10 x 7200 s.
- The init carries 35 TacticalArea (all with UUIDs), 41 Line, 317 Point, 16 TaskGraphic;
  `InitParser` collects only `S.TacticalAreaType`.
- Vertex-mean vs area centroid on the 12 multi-vertex areas: inside 12/12, offset 149-1130 m.
- `NotifyDispatched` precedes `_timed.Register` inside `MarkDispatched`; `Advance`'s first
  walk after `Register` anchors only. (This is what makes M1 an ordering proof, not a race.)
- All 8 `MarkDispatched` call sites are on the tick thread; `PushTaskStatus` (and therefore
  `_timed.Cancel`) is called from the tick thread, the SDK callback thread and the thread
  pool. The `Advance` remove-then-report uses the atomic pair `TryRemove`, so a concurrent
  `Cancel` and a due entry cannot both act; and if both TASKCMPLTs do reach `PushTaskStatus`,
  `TaskStatusPolicy.ShouldEmitComplete` emits exactly one. **No double terminal code from
  the timed/arrival pair.**
- Arrival evidence and the stall watchdog both skip destination-less in-flight records.
- Clock robustness: `UsingSimClock` rejects -1, NaN, -Inf and +Inf; `Advance` rejects a
  non-finite reading. An unreadable sim clock falls to wall and re-anchors - correct.
- All three former self-target guards (ATTACK, BREACH, ESCRT) are folded into
  `ResolveAffectedTarget`; no other `== vrfUuid` guard remains.
- Timer leak: bounded - one entry per task uuid, removed when it fires or is cancelled.
  (Except under M3/M4, where nothing fires.)
- ASCII: clean, with a dirty control file proving the check.

**Assumed / not checked here:**
- No live VR-Forces run was made; every finding is static or data-derived.
- The 1-2 s margin in M1 is derived from the code ordering, not measured. The DIRECTION of
  the inequality is proven; the magnitude is not.
- Whether `DtVrfRemoteController::simTime()` actually alternates or goes stale in 5.2 is
  unproven (M3, M4 are hazards the neighbouring watchdog code already treats as real and
  hardened against; the timed walk simply did not inherit that hardening).
- STP's handling of ObservationReports (STP-800) is taken from the task brief, not verified.
- `--typemap-selftest` was run with `C:\MAK\vrforces5.2d\bin64` prepended to PATH; the three
  bridge-dependent suites were not run at all.

---

## 4. QUESTIONS THAT NEED A USER RULING

**Q1 (blocking merge). A SUPERSEDED task: does it still complete at its authored end time?**
R4 says "completion is given by the end time - the order's statement, not the simulator's".
Read literally, a task that was replaced in VR-Forces still ENDS when the order says it ends,
and STP should see TASKCMPLT. Read as the interface's own log says ("the old task will not
complete"), a replaced task never finished and should be TASKABRT. Today the code does the
first while logging the second. What STP sees under the current code: TASKSTRT(old),
TASKSTRT(new), TASKCMPLT(old) at the old task's end time, and the old task's successors then
dispatch onto a unit that is doing something else. Recommendation: TASKABRT at the supersede
point (the taskee is demonstrably not performing it), but this is a semantics call, not an
engineering one. Not reachable on COA-STP1 under `PredecessorTimeoutPolicy=skip`.

**Q2. Which clock is a C2SIM Duration measured on?** The branch answers "whichever
`Vrf:StallClock` says", default WALL. A Duration in an order is naturally a statement about
SIMULATED time. If the answer is "sim", R4 needs its own setting and M2's wall-clock gate and
start delay must move with it. If the answer is "wall", say so, and live gate 5 is wrong.

**Q3. Is a zero-geometry task allowed to look identical to a performed one on the wire?**
STP receives TASKSTRT + TASKCMPLT and nothing else (n3); the derivation goes out only as an
ObservationReport, which STP discards. Acceptable, or should the in-place case be marked in a
channel STP reads?

**Q4. A task with NO Duration and no geometry** (none in COA-STP1; all 42 carry one). Today
it dispatches in place, arms no timer, and its successors wait out `TaskPredecessorTimeoutSeconds`
before being skipped. Fail fast, complete at once, or leave as is?

---

## 5. WHAT TO FIX BEFORE MERGE

1. **M1** - the predecessor timeout must be derived from the predecessor's end time (or be
   scaled with `DurationScale`). Without this the branch does not change the COA-STP1 outcome
   at `DurationScale=1.0`, and assessment gate 2 cannot pass.
2. **M2** - put the start delay and the predecessor gate on the same clock as the Duration,
   and separate R4's clock setting from the stall watchdog's.
3. **M3** - share the watchdog's hysteresis-confirmed clock mode with the timed walk.
4. **M4** - stale-sim-clock detection in the timed walk.
5. **M5** - register Line and Point graphics (InitParser + the registration loop), and raise
   an unmatched `MapGraphicID` to Warning. Until then, correct the doc claim to "areas only".
6. **m6** - push TASKABRT on the `TryGetEntityGeodetic` failure path; it is the only real
   "no performing unit" case and it is silent.
7. **Q1** - user ruling, then implement it.
8. Add self-tests for `ScaleOrderMs`, `DurationScale` boundary values, `AbsoluteStartUtc`,
   the Duration format variants (missing T, fractional seconds, negative, `PT1H20M` short
   form -> currently -1 + a warning), and a service-level check of the
   Duration-vs-predecessor-timeout relation. Drop or rewrite the tautological `(d2)`.

Everything else (m1-m5, m7-m9, n1-n10) is mergeable as recorded debt.

---

## 6. ADVERSARIAL REVIEW

**Competing hypothesis for M1:** "the gate and the completion both run from the predecessor's
dispatch, so they are simultaneous and the completion may win." **Falsifier checked:**
`MarkDispatched` calls `_sequencer.NotifyDispatched` (:2621) BEFORE `_timed.Register` (:2648),
and `TimedCompletionPolicy.Advance` contributes ZERO on its first walk for an entry
(:109-114), so `Elapsed` begins accruing strictly after registration and advances in <= 1 s
steps. The completion is therefore strictly later than `dispatch + Duration`, and the gate
expires at exactly `dispatch + timeout`. The hypothesis is falsified by ordering within one
method; it is not a race whose outcome could go either way. What remains unmeasured is the
magnitude of the margin, not its sign.

**Competing hypothesis for M3/M4:** "the 5.2 sim reader does not alternate and does not go
stale, so neither can bite." **Weighed:** I cannot falsify this from here - no live run. But
`StallPolicy` carries both cases as its OWN pass-2 review findings 8 and 9, with the vendor
citations (`vrfBackendListener.h:154-163`, `vrfRemoteController.h:605`), and added hysteresis
and `SimClockStale` for exactly them. The timed walk consumes the same reader through the same
helpers and inherits neither guard. So the honest statement is: these are hazards the adjacent
code already judged real and hardened against, and the new consumer did not inherit the
hardening - not observed failures.

**Competing hypothesis for M5:** "the create flags are the blocker, so this is deferred work,
not a defect." **Falsified:** `_graphicsByC2SimUuid` is written from `init.Areas` BEFORE the
create is queued and stores only AUTHORED POINTS (:1132-1137) - it never touches a VR-Forces
object. The blocker is `InitParser` collecting only `S.TacticalAreaType`, which is independent
of `Vrf:CreateInitLines`/`Points`. The field comment's claim to the contrary is the defect.

**Competing hypothesis for m1/m2:** "supersede cannot happen on COA-STP1, so these are
theoretical." **Partly upheld:** measured, the order has 0 taskees with more than one ungated
task, so under `PredecessorTimeoutPolicy=skip` no supersede occurs. They are downgraded to
MINOR on that basis, not dismissed - `force` and `whenidle` reach them, and the demo profile
does not pin the policy.

**Symptom still unexplained (not this branch's):** the doctrine record's verb-vs-symbol
mismatch - `GFTPAS` (follow and support) x3, `GFTPA` (follow and assume), `GFTPN` (neutralize)
drawn in the init while FOLSPT / FOLASS / NTRCOM appear in none of the 42 `TaskActionCode`
values. Untouched here; still an open STP-side question.

**What no test can cover here:** every one of M1-M5 is an interaction between two subsystems
on the live tick thread, and the 41-check suite is entirely composed of pure-function checks.
That gap is itself a finding (n8), and the fix list item 8 is the remedy.
