# Cold-start review of feat/integration 02b51de (Opus, 2026-09-14 ~14:40Z): FIX FIRST - G-A native rebuild + deploy
# to all TEN bridge consumers (deferred until after G7 attempt 4, supervisor), G-B STP parse check on the B7 shape
# (result in STP_PARSE_CHECK sec 3.6 - PASS on 1.3.0/1.3.1/1.4.0), m1/m3/m6/m2/m7/m8 + tick-loop hardening applied in b223afd.

# COLD-START REVIEW - feat/integration @ 02b51de (base main 3f88b9b)

Reviewer: Opus cold-start, read-only. Tier HEAVY (this build is deployed to the demo interface
and drives the first live run of five features at once). Worktree reviewed:
`<repo>\.claude\worktrees\integration`. Nothing was modified; the worktree was BUILT
(`dotnet build src\VrfC2SimApp -c Release -p:BridgeConfig=Release-5.2`) and the offline
self-tests were run. No native rebuild, no launch, no commit.

## 0. VERDICT

**FIX FIRST** - two gates, both cheap, neither a code defect in the merge:

  G-A  Rebuild the NATIVE VrfBridge in the MAIN checkout (`/t:Rebuild`, Release-5.2) and refresh
       EVERY consumer bin, then re-pin the deployed build. Evidence and failure mode: MAJOR-1.
       The "all 7 copies" figure in the records is STALE - there are TEN csproj consumers now.
  G-B  Re-run the STP parse harness (docs/experiments/STP_PARSE_CHECK_2026-09-14.md sec 3) on a
       PositionReport built by THIS build WITH HeadingAngle and Speed populated, against the
       HyssosTech.Sdk.C2SIM versions STP pins (1.3.0 / 1.3.1 / 1.4.0). B7 adds two elements to
       every position report - the ONE report shape STP actually consumes - and STP drops the
       WHOLE report on any deserialize failure. The existing check predates B7. Evidence: MAJOR-3.

Recommended in the same turn (one-line each, not blocking): MINOR-3 and MINOR-6.

Everything else I could break is either correct, already guarded, or pre-existing on main.
The merge itself is clean: all four branch tips are ancestors of 02b51de and no branch's
behaviour was dropped (NOTE-8). The build compiles and all seven offline suites pass.

## 1. FINDINGS TABLE

| # | Sev | Where | Failure scenario (inputs -> wrong outcome) | Fix |
|---|---|---|---|---|
| M1 | MAJOR | `src/VrfC2SimApp/VrfC2SimService.cs:659` (call), `:587-606` (TickLoop), deploy | A managed-only refresh of the deployed folder (new `VrfC2SimApp.dll/exe`, old `VrfBridge.dll`) -> at t+10 s the R1 poll JITs `MaybeSendPositionReports`, `_bridge.TryGetEntityKinematics` is missing -> `MissingMethodException` on the vrf-tick thread. TickLoop wraps only `action()` and `_bridge.Tick()` in try/catch; `Program.cs` installs no `AppDomain.UnhandledException` handler (stated at `StallPolicy.cs:1085`) -> THE INTERFACE PROCESS DIES mid-demo. `appsettings.Demo.json` sets `PositionReportSeconds: 10`, so R1 is ON by default and the call has NO feature flag. | `/t:Rebuild` VrfBridge Release-5.2 in the MAIN checkout (its DLL is dated 2026-09-06 12:02 and has none of the new members; pinned demo exe is 2026-09-07 11:06), back up the old DLL first, then rebuild all consumers so every bin copy is one hash. Optionally wrap the three unguarded tick-loop calls in the same try/catch the `action()` drain already has. |
| M2 | MAJOR (frame, not code) | STP `C2SimBridge/C2SimXmlBuilder.cs:~690-722` + `:720` | STP's `ParseReportContent` handles ONLY `PositionReportContentType`; `ObservationReportContentType` and `TaskStatusType` are parsed and then DISCARDED (STP_PARSE_CHECK secs 3.2/3.3), and `// TODO: Add HeadingAngle, Speed` sits at `:720`. So B1 (TASKSTRT/TASKABRT), C16 (stall TASKABRT), B4 (pre-flight warnings), B8 (substitutions) AND B7 (heading/speed) are ALL no-ops for STP as pinned today. Risk: the demo is narrated as "STP now sees task starts, aborts, heading and speed". | No code change. The four live gates in REPORTING_ASSESSMENT are correctly written against the BUS capture, which is where the evidence is. State the limit in DEMO_RUNBOOK and keep it in B10 / DRAFT_STP_QUESTIONS. |
| M3 | MAJOR | `src/VrfC2SimApp/ReportBuilder.cs` `Position()`; STP `C2SimXmlBuilder.cs:729` | B7 adds `HeadingAngle` + `Speed` to EVERY position report. STP wraps its whole parse+extract in one `catch` that returns null and drops the WHOLE report. If our fork's generated `PositionReportContentType` disagrees with STP's pinned 1.3.1 on these two elements, 100% of position reports vanish - and position is the only channel STP consumes (M2). The sec-3 harness tested the PRE-B7 shape only (B7 did not exist at 13:10Z). | Re-run the harness on a body from this build with both values non-null, on 1.3.0/1.3.1/1.4.0. Offline, minutes. Partial verification already done here: our generated type carries `headingAngleField`/`speedField` with `*Specified` companions (`C2SIM_SMX_LOX_CWIX2024.cs:10908-10916`, properties `:10952/:10963/:10983/:10994`), ReportBuilder sets value AND flag, and `--report-selftest` round-trips through our own SDK. |
| m1 | MINOR | `src/VrfC2SimApp/NameRegistry.cs:240-242` vs class doc `:35-40` | `_uuidByName[resolved] = uuid` is unconditional. When `resolved` is EXACTLY a requested name ALREADY bound to a different uuid - the documented COA-STP1 shape `510/40~PXY` (exactly 10 chars) plus its four EXPAND children (`.HQ1/.TANK2/.TANK3/.PLOW4`) - a second callback under that string REPLACES the live unit's uuid and its reverse-map entry. From then on R1 reports the wrong object's position for that unit and ExecuteTaskOnTick would task it. The "an object can never take a live unit's identity" guard is in `ScanForRequested:175`; the exact-match short-circuit at `:157` bypasses it. A `NAME COLLISION RISK` warning DOES fire (`VrfC2SimService.cs:2634`), so it is loud, but the maps change anyway. NOT a merge regression - main's `_vrfUuidByName[e.Name] = e.Uuid` overwrote the same way. Reachability is limited on the demo path: EXPAND children are created as AGGREGATES (`VrfC2SimService.cs:1494` `new CreationPlan(true, ...)`), and the truncation evidence is for PLATFORM markings. | In `Bind`, when `_uuidByName.TryGetValue(resolved, out prior) && prior != uuid && !truncated`, log ERROR and KEEP the prior binding (the deliberate re-create path already routes through `_recreatePending`). At minimum, correct the class doc: the no-hijack guarantee covers the prefix-scan path only. |
| m2 | MINOR | `VrfC2SimService.cs:684` | Bundling path: `sent = snapshot.Count` counts fixes carried over from the POSITION-text path, and reports 0 when the drain returns null. Log only; identical to main; `BundlePositionReports` ships OFF. | Count what this cycle added, or drop the assignment. |
| m3 | MINOR | `VrfC2SimService.cs:2916` | The "NO VERDICT - tiles missing" line is `LogInformation`. With the shipped `PreflightCacheDir` default (`""` -> `<exe dir>\preflight-cache`, EMPTY on a fresh deploy) EVERY leg is NoVerdict (`LegScorer.MaxNanFraction = 0.01`) and the operator gets one Info line per task saying nothing is being checked. | `LogWarning` when `noVerdict == legs.Count`, naming the cache directory. |
| m4 | MINOR | `VrfC2SimService.cs:2834`, `Preflight/TileSource.cs` `TmsBase` | `PreflightOffline` defaults false. Turning `PreflightWarnings` on without a populated cache makes the first order issue hundreds of HTTP GETs to `http://vr-theworld.com` from worker threads - a live internet dependency in a demo. (It is correctly OFF the tick thread: `QueuePreflight` -> `Task.Run`, `:2887`.) | Before any ON run set `Vrf:PreflightCacheDir` to a populated cache and/or `Vrf:PreflightOffline=true`. |
| m5 | MINOR | `tools/preflight/.gitignore` | `--preflight-selftest` passed here off 332 cached tiles in `tools/preflight/preflight_cache`, which is GITIGNORED (only 6 tracked files under `tools/preflight`). The suite is not reproducible from a clean clone; a green run proves nothing about a fresh machine. | Say so in the self-test output / README, or commit a minimal tile subset. |
| m6 | MINOR | `VrfC2SimService.cs:2921` | Pre-flight ObservationReports are pushed with the default `ReportKind.Position`. No behavioural difference (neither kind retries) but a failure logs `PUSH FAILED (Position)` for an observation. | Pass `ReportKind.Observation`. |
| m7 | MINOR | `Preflight/TileSource.cs:104-105` | `Fetched` / `CacheHits` are plain `int` written from several concurrent pre-flight workers - they can under-count. Log only. | `Interlocked`, or document as approximate. |
| m8 | MINOR | `VrfC2SimService.cs:3506-3527`, `:3570` | R10 fan-out passes the vendor `success` flag only on the QUORUM branch, so members 1..n-1 reporting `success=false` are counted as completions and the LAST member's flag alone decides the unit's code; the straggler timer synthesizes `success=true` by construction. `Vrf:SubordinateFanOut` defaults FALSE (`VrfSettings.cs:352`), so this is off the demo path. | When fan-out is used: carry "any member failed" in `FanOutTracker` and pass it to `SynthesizeUnitCompletion`. |
| n* | NOTE | see section 3 | verified-and-correct items, recorded so the next session does not re-derive them | - |

## 2. WHAT I TRIED TO BREAK, AND WHAT HELD

### 2.1 Merge seams (brief item 1)

- **Lost or doubled behaviour.** All four tips are ancestors of 02b51de (`f052ea7`, `eb80864`,
  `3ec0173`, `9fe51f8`; 0 commits outstanding on any). Every line a branch "removes" relative to
  integration is a SUPERSEDED earlier version - `feat/reporting` branched off the sim-clock series
  at `08146a2` and carries its pass-2 text, while integration keeps the pass-4 text from `f052ea7`
  (dormancy axis, rollback rate-limit, `applyWallFloor`), plus the B7 tuple widening of the bundle
  types. Nothing was dropped.
- **Two emits of one TaskStatus.** Impossible by construction: EVERY TaskStatus in the file goes
  through `PushTaskStatus` (`:3921`), which consults `TaskStatusPolicy` before building. Emit sites:
  TASKSTRT `:2515` (MarkDispatched, the single dispatch funnel); TASKABRT `:1894` (taskee not in
  init), `:1948` (successor skipped), `:1989` (orchestration threw), `:2015` (no VRF object bound),
  `:2197` (no location), `:3381` (stall watchdog); completion `:3608`.
- **A stall TASKABRT after a TASKCMPLT.** Blocked twice: `MaybeCheckStalls:3323` skips a unit in
  `_arrivalReported`, and `TaskStatusPolicy.ShouldEmitAbort` refuses once `Completed` is set.
- **ClearStallState with an unresolved/empty marking.** Guarded at `:3487`
  (`if (!string.IsNullOrEmpty(marking))`). `SynthesizeUnitCompletion:3559` can be reached with
  `name == ""`, but the removals are keyed by unit name and hit nothing, and the method then exits
  on the missing C2SIM uuid.
- **R1 dropping a unit twice.** `unresolved` / `unreflected` / `noKinematics` are per-cycle locals
  on disjoint branches (`continue` on the first two); `_r1Unresolved` / `_r1Unreflected` are
  one-shot name sets so the line is said once per unit, not once per cycle.
- **Counters.** `_reportsSent` / `_reportsFailed` are `Interlocked` and incremented exactly once per
  `PushReportAsync`; a bundle counts as one. The R1 line labels the cumulative pair explicitly
  (review finding 12).

### 2.2 The success flag (brief item 2)

Traced every path into `CodeForCompletion`:

- **Vendor `success=false`, ordinary move.** `OnVrfTaskCompleted:3478` `bool success = e.Success;`
  (the line 02b51de exists for - read and confirmed) -> `SynthesizeUnitCompletion(name, type, false)`
  -> `_inFlight.TryComplete` REMOVES the record -> `_sequencer.NotifyAbandoned` (successors fail
  fast, each getting its own TASKABRT at `:1948`) -> no parked engage (`if (success && ...)`)
  -> `CodeForCompletion(false, false)` = TASKABRT.
- **Can a vendor-failure TASKABRT be followed by an arrival-evidence TASKCMPLT for the same task?**
  NO. `MaybeCheckArrivals` iterates `_inFlight.Snapshot()`, and the record was removed by
  `TryComplete` before the abort was pushed. The reverse order is swallowed at `:3488`
  (`_arrivalReported.TryRemove`). And `TaskStatusPolicy` is the backstop: a TASKCMPLT suppresses a
  later TASKABRT, a TASKABRT never suppresses a later TASKCMPLT (row 19, abort-then-complete).
  **STP never sees two terminal codes for one task.**
- **`taskContinues` (advance-then-engage).** `CodeForCompletion(true, true)` = TASKINPRG, which
  `ShouldEmit` always allows and which consumes no slot; the engage's later completion still gets
  the single TASKCMPLT. `CodeForCompletion(false, true)` = TASKABRT (nothing continues after a
  failure) - asserted in `--report-selftest`.
- **The arrival-evidence + engage seam.** `MaybeCheckArrivals` sets `_arrivalReported[name]` BEFORE
  synthesizing, so I expected the engage's own completion to be swallowed and the task to end at
  TASKINPRG forever. It does not: `IssueEngage:2585-2587` clears `_arrivalReported` (and
  `ClearStallState`) inside its enqueued tick action, which runs ~50 ms later, long before any
  engage completion. Held.
- **TASKSTRT is not doubled by the engage** - `IssueEngage` calls `_inFlight.RecordDispatch`
  directly, not `MarkDispatched`; and even if it did, `ShouldEmitStart` refuses while
  `Started && !Completed`.
- **The G2 case** ("Entity not embarked on same object as target", move-along Failed at sim 320.4):
  this build emits TASKSTRT at dispatch, then TASKABRT at the vendor completion, abandons the
  successors, and cancels the parked engage. Before the merge it emitted TASKCMPLT (the flag was
  dead code) or, on main, nothing at all for nine hours.

### 2.3 Threading (brief item 3)

- Kinematics is read on the TICK thread (`:659`), inside the poll, which is where the
  single-threaded facade must be read.
- Pre-flight is the only worker path: `QueuePreflight:2879` copies route, template and hostility
  BEFORE `Task.Run` (`:2881-2886`), scores off-thread, and pushes through the same
  `PushReportAsync`. It never touches `_bridge`.
- No `await` occurs inside any `lock` in the branch (checked mechanically). `_posBundleLock` is
  never held across a serialize or a push (snapshot-under-lock, build+push outside).
- Shared mutable state is concurrent or immutable-at-handoff: `NameRegistry` (ConcurrentDictionary
  throughout, per-key `TryAdd`), `TaskStatusPolicy` (`ConcurrentDictionary` + per-entry `lock`, no
  awaits), `SubstitutionAnnouncer`, `_templateByName`, `_hostilityByC2SimUuid`, `_arrivalReported`,
  `_stallSamples`, `_stallReported` - all `ConcurrentDictionary`; counters `Interlocked`.
  The stall ring's non-concurrent fields (`_stallClockMode`, `_stallSimClockLast`, the dormancy
  axis) are written by `MaybeCheckStalls` only, which is tick-thread-only.
- One acknowledged, pre-existing shape: `_ = PushReportAsync(...)` runs its synchronous prologue -
  including the SDK's message wrapping - on the CALLING thread, which for R1 and for every
  TaskStatus is the tick thread. Unchanged from main; retries and backoff happen on the pool.
- `GetPreflight()` holds `_preflightLock` across the `PreflightService` construction (vendor SMS +
  soil catalogue file reads). Worker threads only; the first order's tasks serialize briefly behind
  it. Acceptable.

### 2.4 Kinematics (brief item 4)

Checked against the installed vendor headers, not from memory:

- `DtGetHeadingFromGeocentric(DtVector, DtTaitBryan)` returns RADIANS and does the topographic
  transformation itself (`C:\MAK\vrlink5.10\include\matrix\topoCoord.h:46-49`); the facade
  multiplies by `kDegRadFactor = 57.2957795131` (= 180/pi), i.e. rad -> deg. Correct sense of the
  conversion (the same constant is DIVIDED by everywhere the code goes deg -> rad).
- `velocity()` returns `const DtVector32&` in geocentric m/s
  (`vl/baseEntityStateRepository.h:126-128`); `DtLatLon_to_GeocToTopo(const DtGeodeticCoord&,
  DtDcmRef)` and the `DtVector32` overload of `DtDcmVecMul` (`matrix/vlDcm.h:135`) are the exact
  calls `topoCoord.h:33-37` documents; `DtDcmRef` is `DtDcm&` (`vlDcm.h:221`), so the local
  `DtDcm geocToTopo` binds. Frame is X=north, Y=east, Z=down (`topoCoord.h:20-23`), and the code
  takes `hypot(x, y)` - ground speed, vertical rate dropped. Units are m/s and degrees true, which
  is what the C2SIM `SpeedType` / `HeadingAngleType` mean.
- **Heading from ORIENTATION, not velocity** - deliberate and correct for `HeadingAngle`
  ("heading direction in degrees where north is zero"). A STOPPED unit therefore reports Speed 0
  with a valid hull heading. That is real data, not a fabricated default, and the watchdog
  validation run needs it.
- **NaN / Infinity cannot reach the XML.** `VrfFacade::TryGetEntityKinematics` returns false on
  `!std::isfinite(speed) || !std::isfinite(heading)`; `VrfBridge::TryGetEntityKinematics` zeroes
  both outs and returns false; the service leaves both `double?` NULL; `ReportBuilder.Position()`
  sets neither the value nor the `*Specified` flag, so `XmlSerializer` emits no element. There is
  no path by which a `NaN` is serialized as "NaN".
- **Omission rule vs "read zero is sent".** A FAILED read omits both elements and increments
  `noKinematics` (the fix still goes out); a SUCCESSFUL read of a stationary unit sends 0. Both are
  the documented intent and both are right.
- Heading is folded to `[0, 360)` range-agnostically and `-0.0` is folded to `+0.0`.

### 2.5 NameRegistry (brief item 5)

- **Ambiguity** is judged against the WHOLE requested set and cached in `_ambiguous`, so the verdict
  does not depend on arrival order; two candidates leave the object under its returned name and the
  service logs an ERROR (`:2626`).
- **Prefix / identity.** Resolution is allowed ONLY onto a candidate still awaiting its
  ObjectCreated (`ScanForRequested:175`), and a cached resolution is invalidated when a later
  `Requested()` would have changed it (`:115-117`). `EnqueueCreates:1169` registers the WHOLE batch
  before issuing any create, closing the within-batch race.
- **The residual hole is the exact-match path** - see MINOR-1. `PrefixPairs` + `WarnOnPrefixedNames`
  (`:1199`) announce the hazard once at registration, and `BindResult.PrefixedCandidates` warns per
  object, but the write itself is an overwrite.
- **EXPAND children and control objects.** Route names (`task.TaskName + " ROUTE"`, `:2401`),
  waypoint names (`:2335`), area names (`:1113`) and EXPAND child names (`MakeChildName:1675`) are
  all registered in the SAME `_requested` set. That is what lets a route whose name was cut at the
  34-char rwUUID limit still resolve. The inverse risk - a control object's returned name binding a
  UNIT's identity - needs the control name to be a strict prefix of exactly one awaiting unit name
  and at least 8 chars; no such pair exists in COA-STP1, and `WarnOnPrefixedNames` would print it if
  one did.
- **14-char marking truncated to 10 with two units sharing the first 10 chars**: `LongerRequested`
  returns 2 candidates -> `_ambiguous` -> NOT resolved, raw name stands, ERROR logged, the two units
  stay unbound and the R1 line names them once each ("NO VR-Forces uuid"). That is the designed
  outcome - no guess - and it is exercised by `--name-selftest`.
- Aggregate members are named through `TryAddName` (reverse map only, `TryAdd`), so a member name
  can never become a forward lookup key.

### 2.6 Stall watchdog default-OFF (brief item 6)

- **Proof that no TASKABRT can be emitted.** The gate is at the CALL SITE:
  `TickLoop:605` `if (_vrf.StallDetection) MaybeCheckStalls();`, and `StallDetection` defaults
  `false` (`VrfSettings.cs`), is absent from both `appsettings.json` and `appsettings.Demo.json`,
  and has no script parameter. No line inside `MaybeCheckStalls` executes; `_stallReported` and
  `_stallSamples` are written nowhere else; `PushTaskStatus(... TASKABRT ...)` from `:3381` is
  unreachable. This is stronger than a "Decide byte-identical" argument and does not depend on it.
- **`SimTimeSeconds()` failing.** It is called at exactly one place, `:3120`, inside
  `MaybeCheckStalls`, and only when `preferSim` - i.e. only with BOTH `StallDetection=true` AND
  `StallClock="sim"`. The default `StallClock` is `"wall"`, so a stale native DLL cannot even reach
  it. When it IS reached: the call is wrapped in try/catch -> `-1.0`; `-1.0`, `NaN`,
  `+/-Infinity` are all "no reading" through the single predicate `StallPolicy.UsingSimClock`;
  three consecutive readings are required to switch mode; on wall mode the window falls back to the
  wall calibration (`ResolveWindowSeconds`), the wall dispatch floor is re-applied, and
  `if (usingSim && !simReadable) return;` skips the check entirely rather than judging on a bad
  clock. **Degradation is to WALL or to "no check", never to a false abort.** `--stall-selftest`
  passes, including the `+Infinity` crash-point and mode-thrash dormancy cases.
- An invalid `Vrf:StallClock` value logs once and runs on WALL.

### 2.7 Pre-flight (brief item 7)

- **OFF = nothing.** `PreflightWarnings` defaults false; `GetPreflight()` is reached only from
  `QueuePreflight`, which is called only from the guarded site `:2282`. No file is opened, no
  directory created (`Directory.CreateDirectory` lives in the `TileSource` constructor), no socket,
  no thread.
- **ON: a cold tile fetch cannot block the tick.** `QueuePreflight` copies its inputs and runs the
  whole scorer inside `Task.Run`; the tick-thread code path after `:2283` continues straight into
  the dispatch. `TileSource` is documented as "nothing here may run on the tick thread" and the one
  caller honours it.
- **Missing cache -> no verdict, not a false flag.** `LegScorer` sets
  `NoVerdict = nanFraction > MaxNanFraction` (0.01) and `Flagged = ratio >= threshold &&
  lengthM > windowM && !noVerdict`; `PreflightReports.BuildForTask` emits nothing for a
  no-verdict leg. Asserted offline ("a leg with NO VERDICT emits nothing, however bad its ratio
  looks"). The only complaint is loudness - MINOR-3.
- **Ground-only guard** present: `:2282` requires `isGround`, and `isGround = unit.Domain == 1`
  (`:2138`) - the DIS domain of the type we actually created, not a SIDC guess.
- Failure is contained: the whole worker body is in a try/catch that logs and leaves the task alone.
- `--preflight-selftest` reproduces the python tool exactly: 14/14 flagged legs (which matches
  PREFLIGHT_CALIBRATION sec 2's "14 legs flagged, on 8 units" - it is NOT a contradiction of the
  "3 positives" line, which is about the three units that froze), worst ratio deviation 1.0e-14,
  0 tiles fetched, 14/14 observation bodies byte-identical after round trip.

### 2.8 Settings and config (brief item 8)

- New keys and their SHIPPED defaults: `StallDetection=false`, `StallClock="wall"`,
  `StallWindowSeconds=0` (= the clock's calibrated window, 240 wall / 360 sim),
  `StallMoveMeters=50`, `StallMinSecondsSinceDispatch=60`, `StallCheckSeconds=5`,
  `StallMinMembersWithData=1`, `TaskStatusPushTries=3`, `TaskStatusPushBackoffMs=1000`,
  `PreflightWarnings=false`, `PreflightThreshold=0.92`, `PreflightWindowMeters=40`,
  `PreflightStepMeters=8`, `PreflightShortWindowMeters=20`, `PreflightCacheDir=""`,
  `PreflightOffline=false`, `PreflightSharedDataDir=C:\MAK\SharedData\19\latest` (present on this
  machine). `CreateInitLines` is correctly NOT in this branch.
- `appsettings.json` and `appsettings.Demo.json` are UNCHANGED by the branch (verified by diff).
  **No deployed appsettings and no demo profile needs a new entry** - every new key runs on its C#
  default, which is the intended off/report-only posture.
- Consequence for handoff next-step 3 (the C16 validation run): there is NO appsettings key and NO
  `StartInterface52.ps1` parameter for the watchdog. It must be enabled by environment override
  (`Vrf__StallDetection=true`, plus `Vrf__StallClock=sim` if the sim window is wanted). Worth a
  runbook line or a script switch before that run.
- `StallWindowSeconds` REINTERPRETATION (0 and negative now mean "calibrated", where main meant a
  1 s window) has nil blast radius because nothing ships a value - but a hand-written config
  carrying an explicit 0 behaves completely differently. Documented in `VrfSettings.cs`; worth
  repeating to whoever writes the validation-run config.

### 2.9 Self-tests (brief item 9)

All seven suites were RUN on this build (worktree, Release-5.2, `C:\MAK\vrforces5.2d\bin64` first
on PATH). All pass: `--report-selftest`, `--name-selftest`, `--parse-selftest`, `--stall-selftest`,
`--arrival-selftest`, `--typemap-selftest` (783 checks), `--preflight-selftest`.

I looked specifically for vacuous checks and did not find one:

- `--report-selftest` builds the body and ROUND-TRIPS it through `ToC2SIMObject`, then asserts the
  deserialized code AND asserts the OTHER code's string is absent from the xml - so it cannot pass
  by both sides sharing a bug in one direction. The `success` walk covers both flag values.
- `--arrival-selftest` compares THREE independently written samplers (main / broken / fixed) over
  32 arrangements and additionally asserts that the BROKEN one diverges - an explicit anti-vacuity
  guard ("so the guard above has teeth").
- `--stall-selftest` asserts against literal expected clocks and sample indices, and each check
  names the sha whose behaviour it discriminates.
- `--preflight-selftest` compares against a COMMITTED reference produced by the python tool, not
  against itself, and asserts `0 tiles fetched`.
- No assertion I read can be satisfied by `0 == 0`; the count assertions all carry a non-zero
  literal (`14 == 14`, `2 attempts`, `3 attempts`, `2 retry lines`, `1 retry line`).
- The one thing NO offline test can see - that `OnVrfTaskCompleted` reads `e.Success` rather than a
  literal - is stated in the test's own comment and covered by the live gate. I read the line:
  `VrfC2SimService.cs:3478` `bool success = e.Success;`. Correct.

### 2.10 Native (brief item 10)

- `TryGetEntityKinematics` resolves through the SAME `stateRepOf` helper as `TryGetEntityGeodetic`,
  including the oracle's blind `static_cast<DtReflectedEntity*>` fallback for a control object -
  extracted, not duplicated, so position and heading can never disagree about which repository
  they read. Null-safe at every step (`!p_->uuidMgr`, `!obj`, `!sr`). Aggregates go through
  `aggregateStateRep()` first, so the fallback is only reached for objects that are neither.
- `SimTimeSeconds` gates on `backends().count() > 0` before calling `simTime()`, returns `-1.0`
  otherwise, and swallows every exception - "no exception crosses the facade boundary". Correct:
  without the gate, `simTime()` with no back end is indistinguishable from a scenario at t=0.
- **Exported symbol set.** There is no `VrfFacade.dll` and no `.def` - `VrfFacade` is compiled INTO
  `VrfBridge.dll` (build dir contains only `VrfBridge.{dll,lib,exp,pdb}` + `Ijwhost.dll`), and the
  managed side binds by C++/CLI reference, not P/Invoke. The C# build SUCCEEDING against
  `src/VrfBridge/build/Release-5.2/VrfBridge.dll` is therefore proof that `SimTimeSeconds`,
  `TryGetEntityKinematics` and `TaskCompletedEventArgs.Success` are all present on the referenced
  assembly. That DLL is dated 2026-09-14 09:58 in the integration worktree, is UNTRACKED
  (`.gitignore:11 build/`), and is the ONLY native artefact carrying this branch's C++.
- **Deploy copies.** The `all 7 copies` figure (docs/HANDOFF_2026-07-19.md sec 5,
  docs/RESUME_PROMPT.md:131-132, memory) is STALE. Ten csproj files now reference the bridge:
  `src/SmokeTest`, `src/VrfC2SimApp`, `tools/CreateOne`, `tools/CreateTaskAgg`, `tools/ResetVrf`,
  `tools/RtiProbe`, `tools/RunSim`, `tools/SetAlt`, `tools/SetSimRate`, `tools/WatchVrf`
  (plus `bridge-spikes/.../SpikeRunner`, which references `VrfBridge.Spike.dll`, a different
  artefact). All ten resolve the SAME `src/VrfBridge/build/<config>/VrfBridge.dll` by HintPath and
  each keeps its own copy in its `bin`. The deploy step must refresh the source DLL and then
  rebuild every consumer so all copies are one hash. Update the count in the records.

## 3. VERIFIED vs ASSUMED

**VERIFIED (by running, reading the artefact, or reading the vendor header on this machine)**

1. The branch compiles: `dotnet build src\VrfC2SimApp -c Release -p:BridgeConfig=Release-5.2` ->
   Build succeeded, 0 errors, 6 pre-existing warnings.
2. All seven offline suites pass on that build (section 2.9).
3. All four feature-branch tips are ancestors of 02b51de; 0 commits outstanding; every "removed"
   line is a superseded earlier revision (section 2.1). `f0d1c68` and `7672957` (the shas named in
   the handoff prose) are both ancestors too.
4. `appsettings.json` / `appsettings.Demo.json` unchanged by the branch.
5. Vendor API contracts for B7 read from `C:\MAK\vrlink5.10\include\matrix\topoCoord.h`,
   `matrix/vlDcm.h`, `vl/baseEntityStateRepository.h` (section 2.4). The generated schema's
   `HeadingAngle`/`Speed` + `*Specified` properties read from the SDK source.
6. STP's parser: `ParseReportContent` handles only `PositionReportContentType`; `// TODO: Add
   HeadingAngle, Speed` at `C2SimXmlBuilder.cs:720`; the catch-all at `:729`. Read from
   `C:\Users\PauloBarthelmess\Source\Repos\STP\STP-release\...\C2SimBridge\`.
7. The main checkout's `src/VrfBridge/build/Release-5.2/VrfBridge.dll` is dated 2026-09-06 12:02;
   the pinned demo exe is 2026-09-07 11:06; `StartInterface52.ps1:47` runs the exe out of
   `<repo>\src\VrfC2SimApp\bin\Release-5.2\net10.0\win-x64\`.
8. The pre-flight tile cache is gitignored (6 tracked files under `tools/preflight`).
9. The `sent`/skip/abort/complete control flow of sections 2.1-2.2 was traced line by line in
   `VrfC2SimService.cs` at the line numbers cited.

**ASSUMED (not checked, and what would settle each)**

A. That the 2026-09-14 09:58 `VrfBridge.dll` in the integration worktree was produced by
   `/t:Rebuild` from this branch's C++ and against the 5.2d stack. Settled by the rebuild G-A does
   in the main checkout anyway, plus `NativeStackInfo` at start-up.
B. That `DtGetHeadingFromGeocentric`'s SENSE is north -> east increasing. The header does not say;
   the facade comment flags it as "the one thing here a live run should confirm". Settled by one
   run: a unit driving a known bearing.
C. That `DtVrfRemoteController::simTime()` reports the clock the object console prints, and whether
   `DtBackend::simTime()` extrapolates between status messages. Both already registered as the two
   live unknowns in `VrfSettings.cs`; only reachable with `StallClock=sim`.
D. That STP's pinned 1.3.1 `PositionReportContentType` agrees with our fork's on the two new
   elements - that is exactly what G-B settles.
E. That the demo machine can reach `vr-theworld.com` if pre-flight is ever turned on without a
   cache (MINOR-4). Not needed while the feature is OFF.
F. Live behaviour of everything: NOTHING in this branch has run against VR-Forces. The four
   REPORTING_ASSESSMENT gates, the C16 validation run and the B7 heading/speed sanity check are all
   still owed.

## 4. ADVERSARIAL REVIEW (required at HEAVY)

**Competing hypothesis weighed.** My first reading of the arrival-evidence path said the merge had
broken review finding 4: `MaybeCheckArrivals` sets `_arrivalReported[name]` and then synthesizes,
so I predicted the parked engage would be issued, the task would report TASKINPRG, and the ENGAGE's
own vendor completion would then be swallowed by `_arrivalReported` at `:3488` - leaving STP with
TASKSTRT -> TASKINPRG -> silence, the exact failure B1 exists to remove. The falsifier I named was
"some path clears `_arrivalReported` when the engage is issued". It exists: `IssueEngage:2585-2587`
clears it inside the enqueued tick action, which runs on the next loop pass, ~50 ms later and long
before any engage completion. The hypothesis is REFUTED and the code is correct. I record it
because the clear is easy to miss - it is inside a lambda, in a different method from the one that
sets the flag.

A second competing hypothesis on MAJOR-1: "the stale-DLL crash is unreachable because the managed
build would fail to compile first". Partly right, and it is why M1 is a deploy-discipline finding
rather than a latent bug - I checked and the main checkout's bridge genuinely does not have the new
members, so `dotnet build` there fails LOUDLY today. The residual risk is a hand-copied binary, and
the reason it still rates MAJOR is the consequence: process death on the vrf-tick thread, in front
of an audience, ten seconds into the run, with no handler and no restart.

**Symptoms still unexplained.** None found in the code. Two facts are unexplained but belong to
other threads and are not merge defects: the early-stop mechanism (FINDING_EARLY_STOPS sec 7b's
second mechanism) and the G6 mesh-query refusal - the watchdog and the pre-flight are instruments
pointed at them, not fixes for them.

**Where I stopped.** Four passes over the seams; I did not re-derive the C16 calibration (four
prior review passes plus `RECAL_STALL_SIMSECONDS` own that), did not re-verify the pre-flight
numbers beyond confirming the port reproduces the python tool to 1e-14, and did not attempt a
native rebuild (out of scope by instruction).

## 5. WHAT TO DO, IN ORDER

1. **G-A** - back up `src/VrfBridge/build/Release-5.2/VrfBridge.dll` (2026-09-06), `/t:Rebuild` the
   bridge in the MAIN checkout for Release-5.2, rebuild all TEN consumers, confirm one hash, and
   correct the "7 copies" count in docs/HANDOFF_2026-07-19.md sec 5, docs/RESUME_PROMPT.md and the
   memory entry.
2. **G-B** - re-run the STP parse harness on a B7-shaped PositionReport against 1.3.0/1.3.1/1.4.0
   and append the result to docs/experiments/STP_PARSE_CHECK_2026-09-14.md.
3. Apply MINOR-3 and MINOR-6 (one line each); consider MINOR-1's ERROR-and-keep guard.
4. Merge to main, rebuild, re-pin the deployed build, and record the pin.
5. Then the handoff's own next steps 3 and 4: the C16 validation run (enable with
   `Vrf__StallDetection=true` - there is no config key), and the reporting live gates read from the
   BUS capture, not from STP behaviour (MAJOR-2).
