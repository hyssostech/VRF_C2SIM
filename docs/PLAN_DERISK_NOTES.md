# De-risk notes for the Opus execution plan (2026-07-13)

Inputs for the NEXT session's deliverable: docs/OPUS_EXECUTION_PLAN.md - a plan
detailed enough for an Opus-class model to execute under supervision. Every fact
below was VERIFIED first-hand on 2026-07-13 (file:line or live evidence cited) so
the plan can specify mechanisms instead of guessing. ASCII-only per repo policy.

## 1. P4a - the port-exhaustion ROOT CAUSE (verified; the highest-value quick fix)

- MECHANISM: the C2SIM SDK REST client creates and disposes a NEW HttpClient PER
  CALL - `using (HttpClient httpClient = new HttpClient())` at
  Software/Library/CS/C2SIMSDK/C2SIMClientLib/C2SIMClientRestLib.cs:125
  (ServerStatus) and :369 (SendTrans - the POST path every report push uses).
  Each disposal strands sockets in TIME_WAIT (~4 min on Windows); thousands of
  position-report pushes at 20x exhaust the ephemeral range -> SocketException
  10048 on 127.0.0.1:8080. The live error text ("Connection error: ...") is
  SendTrans's own catch at :381. This fired in EVERY live run on 2026-07-13.
- FIX SPEC (SDK repo, branch dev/sdk-fixes - our fork, we own it): one shared
  `static readonly HttpClient` (construct over `new SocketsHttpHandler {
  PooledConnectionLifetime = TimeSpan.FromMinutes(2) }`). CRITICAL SUBTLETY: the
  current code mutates `DefaultRequestHeaders.Accept` per call - on a SHARED
  client that is a cross-thread race; the fix must move Accept to PER-REQUEST
  headers (HttpRequestMessage.Headers.Accept), including converting
  ServerStatus's GetStringAsync to an explicit HttpRequestMessage GET.
- VERIFICATION: SDK + app + tools rebuild 0 errors; the six offline selftests;
  PushInit/StopIface smoke against the live server; then any ~15-min live run at
  20x with ZERO "Only one usage of each socket address" lines (every 2026-07-13
  run had them - a clean run is the discriminator).
- Note: the SDK has a test project (C2SIMSDK.Tests) and precedent for exactly
  this class of fix (commit f738edf "static-state fixes + tests").

## 2. P4b - report BUNDLING (parity feature; the C++ shape is verified)

- C++ oracle (frozen repo, textIf.cxx:435-530 sendC2simReport): POSITION reports
  ONLY are bundled (comment says Observation/TaskStatus have "other reasons not
  to bundle"); a bundle is ONE report envelope whose body carries N ReportContent
  blocks; the ReportID applies to the WHOLE bundle and is minted at send time.
  Flush triggers: report count >= maxReportsPerBundleTextIf (10, "STOMP may balk
  at larger"), total size >= maxBundleSizeTextIf (10240), or a ~2-second reminder
  thread that force-flushes a partial bundle. Bundling was OPT-IN (argv 17).
- .NET readiness (verified): ReportBuilder.cs already fills
  `ReportContent = new[] { ... }` - the generated schema type
  (C2SIM.Schema102 ReportBodyType) takes an ARRAY of ReportContentType, so a
  multi-content body is a data change, not a schema fight. Spec: a
  `BuildPositionReportBundle(IEnumerable<(uuid,lat,lon)>, time, reportId)` +
  an accumulator in the service's OnVrfTextReport path with the three C++ flush
  triggers; opt-in `Vrf:BundlePositionReports` (suggest count 10 / 2000 ms /
  10240 bytes as defaults mirroring the C++). TASKCMPLT stays unbundled.
- ORDERING: do P4a FIRST and re-run live; P4a alone may fully clear the 10048
  errors, making P4b a politeness/parity feature rather than a firefight.

## 3. Fan-out robustness (quorum + straggler timeout) - design notes

- Motivation (live 2026-07-13): the COA-STP1 unblock run ended 5/7 because ONE
  stuck GndV per CoHQ held each unit task open forever (46/52 members marched).
- FanOutTracker (src/VrfC2SimApp/FanOutTracker.cs) already stores Total per
  fan-out and returns `remaining` per completion - a quorum needs only
  (Total - remaining)/Total >= fraction evaluated at completion time.
- SUBTLETY THE PLAN MUST HANDLE (found by design review): after a quorum-based
  synthesis, LATE straggler completions must be SWALLOWED. Today a completion
  for an unknown member falls through to the unit-level path and would emit a
  spurious TASKCMPLT with an empty task uuid ("NO in-flight task recorded"
  warning). Keep the fan-out record alive in a Synthesized state (swallow member
  completions silently, log at debug) until the last member completes or a new
  task supersedes.
- Straggler TIMEOUT precedent: the service already runs detached fallback timers
  (EngageFallbackAsync + _stoppingToken); a per-fan-out timer that synthesizes
  completion with a WARNING after Vrf:FanOutStragglerSeconds fits the same
  pattern. Suggest: quorum fraction (Vrf:FanOutCompletionFraction, default 1.0 =
  today's behavior) AND timer (default 0 = off) both opt-in; either trigger
  synthesizes once (idempotent - the tracker's removal/Synthesized state guards
  double-fire).
- Extend --fanout-selftest: quorum at 3/4, late-straggler swallow, supersession
  while Synthesized, fraction 1.0 == legacy behavior.
- Optional (cheap, same file): fan out the SINGLE-POINT MoveToLocation path too
  (fan-out currently covers only the multi-point route path).

## 4. COA-STP1 full-order scale run (live; procedure known)

- data/COA-STP1_Order.xml: 42 tasks, 11 distinct performers, ALL self-target
  (verbs degrade to movement - expected, documented), 32 temporal deps (P0.2
  skip policy governs), 3 SCREEN are PATROL tasks - NOTE fan-out deliberately
  EXCLUDES patrol (PendingRouteTask.Patrol short-circuits it) and patrols never
  self-complete. Success criteria for the plan: creates 128+35, de-stack fires,
  fan-out dispatches on aggregate MOVE-class tasks, unit TASKCMPLTs >= the
  R5c-era count by a wide margin, no 10048 errors (post-P4a), clean stop.
- Environment ledger: appNos 3200-3350 consumed; START AT 3355. One join per
  ResetVrf/WatchVrf/app instance, always fresh.

## 5. Housekeeping facts (verified)

- SIX csproj files carry ABSOLUTE SDK ProjectReference paths: tools/ListenReports,
  PushInit, PushOrder, SdkVerify (TWO refs - also C2SIMClientLib), StompProbe,
  StopIface. tools/* sit at the SAME depth as src/VrfC2SimApp, whose working
  relative form is `..\..\..\..\Library\CS\C2SIMSDK\C2SIMSDK\C2SIMSDK.csproj` -
  a uniform substitution. Verify: rebuild each tool 0 errors.
- coa-gpt memo: FOUR evidence-backed items (distinct AffectedEntity; timing
  hygiene; DISPERSED positions - nuanced by R8 de-stack; REGION VALIDATION -
  probe a region with 1 unit before generating COAs, R9/R10 evidence). Pure
  writing task; sources: PORT.md sec 10, UNIT_MOVEMENT_RESEARCH.md secs 4-4c.

## 6. Supervision protocol facts (for the plan's guardrails section)

- The Opus executor MUST NOT: force-kill a joined federate; push an init to a
  running interface; restart the c2sim-server container; kill vrfSimHLA1516e
  (vrfGui has been hung for days - backend healthy); reuse an appNo; skip the
  offline selftest gate (all EIGHT: translator 18, parse-init 80/49/4,
  parse-order, report 9, sequencer 12, verb 28, destack 20, fanout 16+);
  claim movement from a completion event (R11 proved completions LIE at
  path-dead regions - WatchVrf telemetry is the only movement oracle).
- Natural supervision checkpoints: after each offline-verified step (review diff
  + selftest output before commit); before EVERY live run (env checklist:
  loopback <1s, RTI 4.6.1 PATH, Machine-scope license, fresh appNo, cwd bin64,
  --contentRoot); after each live run (verdict review before docs land).
- Build commands are exact and non-negotiable: VS18 MSBuild for the bridge
  (PowerShell, NOT git-bash - POSIX /p: mangling), DOTNET_CLI_USE_MSBUILD_SERVER
  =false + --disable-build-servers for the app.
