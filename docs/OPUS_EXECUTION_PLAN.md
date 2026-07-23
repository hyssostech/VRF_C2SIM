# OPUS EXECUTION PLAN - C2SIM VR-Forces -> .NET port backlog

Purpose: a step-by-step plan an Opus-class executor can carry out UNDER SUPERVISION.
It closes the six backlog items that the 2026-07-13 de-risk pass (docs/PLAN_DERISK_NOTES.md)
made ready. Every step carries: exact files + code-level change spec, exact build/test
commands, acceptance criteria + verification gate, a rollback note, and STOP-AND-ESCALATE
conditions. ASCII-only per repo policy.

This plan is a DERIVED artifact. When it disagrees with a source-of-truth doc, the
source wins. On plan MECHANICS (fix specs, flush triggers, csproj paths) the winner is
docs/PLAN_DERISK_NOTES.md. Read these first and treat them over any recollection:
  1. docs/START_HERE.md              - status, repo state, build/run, tools
  2. docs/PORT.md                    - settled decisions WITH evidence (esp. sec 6/8/10)
  3. docs/UNIT_MOVEMENT_RESEARCH.md  - the R1-R11 aggregate-movement arc (sec 4/4b/4c)
  4. docs/PLAN_DERISK_NOTES.md       - the verified planning inputs (WINS on mechanics)
  5. docs/RUNBOOK.md                 - runtime procedure (sec 0/3/4/7/8)
  6. docs/SEMANTIC_MAPPING.md        - verb map status (context for the memo)

State at plan time (git log is authoritative - run `git log --oneline -1`, do NOT trust
hashes pinned in prose):
- PORT `VRF_C2SIM` branch main: tip `fe65db1`, clean, pushed to
  github.com/hyssostech/VRF_C2SIM. Eight offline selftests green.
- FORK `OpenC2SIM.github.io` branch dev/sdk-fixes: tip `2655c30`, pushed; the submodule
  pointer for Software/Interfaces/VRF_C2SIM tracks port main (`fe65db1`). (The fork
  working tree also shows unrelated IDE noise under the OLD c2simVRFinterface path and
  an untracked Standard/C2SIM ontology catalog - NOT ours; do not commit them.)
- SDK `C2SIMClientLib` (in the fork): version 4.8.3.2, multi-targets net10.0;netstandard2.0.

Execution order is deliberate and is the order below. P4a is first because it is the
smallest change with the biggest operational win (it removes the socket-exhaustion that
fires in EVERY live run), and because it must land BEFORE the live scale run so that run
can use "zero 10048 errors" as an acceptance discriminator.

CONFIDENCE + UNKNOWNS: before starting, read Appendix E - a ranked, honest inventory of
where the author's confidence is lower (verified vs assumed) and the per-item watch-point.
It exists so the executor inherits the calibration instead of re-deriving it. The two most
likely live surprises are P4b server acceptance (isolated OUT of the first scale run) and the
Step 5 completion COUNT (unpredictable by design); the most review-fragile code is the Step 2
FanOutTracker rewrite.

---

## 0. SUPERVISION PROTOCOL (read before touching anything)

The executor operates in a check-in loop with a human supervisor. Do NOT batch multiple
steps past a gate. The gates:

### 0.1 Gates (STOP and get supervisor sign-off at each)

- GATE-DIFF (before every commit): show the supervisor the full `git diff` (and the
  offline selftest output for that step). Do not commit until acknowledged.
- GATE-ENV (before every LIVE run): run the preflight checklist (Appendix A) and show its
  output. Do not launch the app until acknowledged. A live run consumes a fresh
  ApplicationNumber and touches the shared federation - it is not reversible.
- GATE-VERDICT (after every LIVE run): show the telemetry-backed verdict (WatchVrf
  displacement, completion counts, error grep) BEFORE writing it into the docs. A claim
  of movement with no telemetry is not a verdict (see the R11 trap rule).

### 0.2 The offline selftest gate (run BEFORE and AFTER any code change)

All EIGHT must pass, both before a change (to prove the baseline) and after (to prove no
regression). MAK bin dirs on PATH; RTI 4.6b is fine for offline (it only LOADs the DLLs).
```
# cwd = the PORT repo root (Software/Interfaces/VRF_C2SIM). NOTE the win-x64 RID subfolder -
# verified on disk 2026-07-13; some tools (e.g. PushInit) have no RID subfolder. If the path
# 404s, `ls` the bin tree rather than assuming.
$env:PATH = "C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6b\bin;$env:PATH"
$exe = "src\VrfC2SimApp\bin\Release\net10.0\win-x64\VrfC2SimApp.exe"
& $exe --translator-selftest      # 18/18
& $exe --parse-init docs\golden-trace\STP-TC-small-6-12-24_Initialization.xml STP   # 80 units, 49 creatable, 4 areas
& $exe --parse-order docs\golden-trace\orders\1_VRF_Move_Order.xml                  # 1 MOVE, taskee 670cfe3a..., 2 pts
& $exe --report-selftest          # 16 (P4b Step 3 LANDED: +7 position-bundle checks; was 9 pre-Step-3)
& $exe --sequencer-selftest       # 12, ALL CHECKS PASSED
& $exe --verb-selftest            # 28+, ALL CHECKS PASSED
& $exe --destack-selftest         # 20, ALL CHECKS PASSED
& $exe --fanout-selftest          # 36 (Step 2 LANDED: +19 quorum/timeout/swallow cases; was 17 pre-Step-2)
```
If any selftest that is unrelated to the current change regresses, STOP - do not "fix
forward"; revert and diagnose.

### 0.3 Hard rules (non-negotiable - copied verbatim from RUNBOOK + PLAN_DERISK_NOTES sec 6)

- NEVER force-kill a joined federate (`Stop-Process -Force`, `taskkill /F`). It leaves a
  STALE FEDERATE and the next join hangs. Clean-stop via `tools/StopIface` (drives the
  server STOP -> RESET -> UNINITIALIZED; the app catches UNINITIALIZED and resigns).
- FRESH `Vrf__ApplicationNumber` for EVERY RTI join - the app, ResetVrf, AND WatchVrf each
  join and each need their own. Take the number from the single line marked
  "*** NEXT FREE: <number> ***" in Appendix B. Increment and never reuse; record each.
  *** CORRECTED 2026-07-18: this line used to say "AppNos through 3385 are consumed; take
  the next-free number recorded at the Appendix B LEDGER TAIL (currently 3386)". BOTH
  halves are wrong and dangerous: the cached 3386 is stale by 117, and "take the number
  after the last ledger entry" is the FORBIDDEN rule - entries are NOT in numeric order
  and that rule pointed at ALREADY-CONSUMED numbers (see the warning at the head of
  Appendix B). Read the MARKER, never the tail, never a number quoted in prose. ***
- NEVER push an init to a RUNNING interface (PushInit's RESET step's UNINITIALIZED
  transient makes the running app resign). Push init only while NO app is running.
- Do NOT restart the c2sim-server container habitually - the restarts are what degraded
  the loopback proxy before. Loopback test FIRST: a raw TCP connect to 127.0.0.1:61613
  must be near-instant (<1 s). If it is slow, reset the Docker/WSL proxy (restart Docker
  Desktop or reboot), do NOT just restart the container.
- LIVE runs: RTI 4.6.1 on PATH (4.6b is build/offline ONLY - a live join must match the
  federation's RTI); `MAKLMGRD_LICENSE_FILE` read from Machine scope (a stale session
  value points at a deleted .lic and HANGS license checkout); cwd = C:\MAK\vrforces5.0.2\bin64;
  pass `--contentRoot=<exe dir>` so appsettings still loads. PushInit FIRST, then start
  the app (it late-joins). ResetVrf between heavy runs (entities accumulate and creates
  stop reflecting).
- vrfGui has been HUNG for days - the sim BACKEND is healthy; do NOT kill vrfSimHLA1516e.
  WatchVrf is the visual channel. If the BACKEND is also unresponsive -> STOP, coordinate
  with the user.
- Movement claims REQUIRE telemetry (WatchVrf displacement), NEVER completion events alone.
  R11 proved DtPlanAndMoveToTask fires TASKCMPLT while units sit at spawn - completions LIE
  at path-dead regions. WatchVrf per-object displacement is the only movement oracle.
- Bridge builds: VS18 MSBuild via PowerShell, NOT git-bash (git-bash mangles `/p:`). App
  builds: `DOTNET_CLI_USE_MSBUILD_SERVER=false dotnet build ... --disable-build-servers`
  (concurrent dotnet builds deadlock the shared build server).
- Keep docs/START_HERE.md, PORT.md, UNIT_MOVEMENT_RESEARCH.md, RESUME_PROMPT.md current AS
  work lands; after any context compaction re-read them before deciding anything.

### 0.4 Commit + push mechanics

- Commit granularity: ONE commit per step (or a tight sub-series), each after its GATE-DIFF.
- ORDER: push the PORT first, then bump + push the fork's submodule pointer (the fork's
  pointer references a port commit that must already exist on the remote).
  - Port (branch main), from the port working dir:
    `git add -A && git commit && git push origin main`
  - Fork submodule bump (branch dev/sdk-fixes), from the fork root:
    `git add Software/Interfaces/VRF_C2SIM && git commit -m "Bump VRF_C2SIM submodule -> <step>" && git push origin dev/sdk-fixes`
- SDK EDITS (Step 1 - P4a) are in the FORK repo, NOT the submodule. They commit ON THE
  FORK (dev/sdk-fixes) directly under Software/Library/CS/C2SIMSDK. So Step 1 produces a
  fork commit for the SDK change PLUS, if any port doc/config changes ride along, a port
  commit + submodule bump. Do the port push before the fork push as above.
- Commit message trailer (per user global policy): `Co-Authored-By: <the executing Claude
  model> <noreply@anthropic.com>` - use the SESSION'S ACTUAL model name (e.g. "Claude Fable 5"
  or "Claude Opus 4.8 (1M context)"), not a hardcoded one.
- WORKING DIRECTORIES (paths in this plan use two conventions): paths starting `Software\...`
  are FORK-root-relative (run from the OpenC2SIM.github.io checkout root); paths starting
  `src\`, `tools\`, `docs\`, `data\` are PORT-root-relative (run from
  Software/Interfaces/VRF_C2SIM). Do not cd into a project dir and reuse a root-relative path.
- Do NOT touch the unrelated fork working-tree noise (.vs/ under the old c2simVRFinterface,
  the ontology catalog). Stage only the paths you changed.

### 0.5 Global STOP-AND-ESCALATE (any step)

Stop and coordinate with the user if: an offline selftest unrelated to the change
regresses and does not revert cleanly; the sim BACKEND (vrfSimHLA1516e) is unresponsive;
the loopback proxy is slow and a Docker Desktop restart does not fix it; a live run leaves
a stale federate that ResetVrf cannot clear; the license has expired (checkout hangs; the
renewed .lic expires 2026-09-15); or any change would require force-killing a federate,
restarting the container, or reusing an appNo to proceed.

---

## Step 1 - P4a: SDK shared HttpClient (kill the port exhaustion)

GOAL: stop the C2SIM REST client from creating and disposing a new HttpClient per call, so
report pushes no longer strand sockets in TIME_WAIT and exhaust the ephemeral port range.

WHY / EVIDENCE (PLAN_DERISK_NOTES sec 1, verified): the SDK does
`using (HttpClient httpClient = new HttpClient())` per call at
Software/Library/CS/C2SIMSDK/C2SIMClientLib/C2SIMClientRestLib.cs:125 (ServerStatus) and
:369 (SendTrans - the POST path EVERY report push uses). Each disposal strands a socket in
TIME_WAIT (~4 min on Windows); thousands of position-report pushes at 20x exhaust the
ephemeral range -> SocketException 10048 ("Only one usage of each socket address...") on
127.0.0.1:8080, surfaced as SendTrans's catch text "Connection error:" at :381. Fired in
EVERY 2026-07-13 live run.

REPO: this is a FORK change (SDK lives in the fork, not the submodule). Branch dev/sdk-fixes.
Path: Software/Library/CS/C2SIMSDK/C2SIMClientLib/C2SIMClientRestLib.cs.

### 1.1 Code-level change spec

CRITICAL SUBTLETY (PLAN_DERISK_NOTES sec 1): a shared client must NOT mutate
`DefaultRequestHeaders.Accept` per call - that is a cross-thread race. Move Accept to
PER-REQUEST headers. And C2SIMClientLib multi-targets `net10.0;netstandard2.0`, so
`SocketsHttpHandler`/`PooledConnectionLifetime` (net5+ only) MUST be `#if`-guarded or the
netstandard2.0 leg fails to compile. The netstandard fallback is a plain shared static
HttpClient - by itself it fixes the exhaustion (connection pooling); PooledConnectionLifetime
only guards DNS staleness, irrelevant for a fixed loopback host.

(a) Add one shared static field (near the other statics around line 31):
```csharp
#if NET5_0_OR_GREATER
    // One shared client (was: new HttpClient() per call, which stranded a socket in
    // TIME_WAIT per report push -> ephemeral-port exhaustion / SocketException 10048 at
    // ~20x report volume). Accept headers are now PER-REQUEST (a shared client's
    // DefaultRequestHeaders are not thread-safe to mutate). PooledConnectionLifetime keeps
    // long-lived pooled connections from going stale.
    private static readonly HttpClient _httpClient =
        new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) });
#else
    // netstandard2.0: SocketsHttpHandler/PooledConnectionLifetime are unavailable; a plain
    // shared client still fixes the exhaustion (connections are pooled and reused).
    private static readonly HttpClient _httpClient = new HttpClient();
#endif
```

(b) ServerStatus (:117-147) - replace the `using (HttpClient ...) { ... GetStringAsync }`
block (:125-130). GetStringAsync throws HttpRequestException on non-2xx, so preserve that
via EnsureSuccessStatusCode() to keep the existing catch (:132) behavior:
```csharp
    url = new Uri(BuildC2SIMEndpoint("status"));
    using (var request = new HttpRequestMessage(HttpMethod.Get, url))
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        using (var resp = await _httpClient.SendAsync(request))
        {
            resp.EnsureSuccessStatusCode();
            result = await resp.Content.ReadAsStringAsync();
        }
    }
```

(c) SendTrans (:361-386) - replace the `using (HttpClient ...)` block (:369-377). It
already builds an HttpRequestMessage; just drop the per-call client and move Accept onto
the request:
```csharp
    Uri url = new Uri(u);
    using (var request = new HttpRequestMessage(HttpMethod.Post, url))
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        request.Content = new StringContent(xml, Encoding.UTF8, "application/xml"); // CONTENT-TYPE
        using (HttpResponseMessage resp = await _httpClient.SendAsync(request))
        {
            result = await resp.Content.ReadAsStringAsync();
        }
    }
```

Leave the catch blocks, the error-XML checks, and all other logic UNCHANGED. Do NOT change
the STOMP client (sockets, not HTTP - not the exhaustion source; grep confirmed only these
two HttpClient sites exist in the SDK).

Optional hygiene (recommended, matches SDK precedent commit f738edf): bump
C2SIMClientLib.csproj `<Version>` (4.8.3.2 -> 4.8.3.3) and add a ReleaseNotes.md line.

### 1.2 Build

```
# cwd = the FORK root. Build BOTH target frameworks to prove the #if guard compiles clean.
$env:DOTNET_CLI_USE_MSBUILD_SERVER = "false"
dotnet build "Software\Library\CS\C2SIMSDK\C2SIMClientLib\C2SIMClientLib.csproj" -c Release --disable-build-servers
# Then rebuild the app + the six SDK-consuming tools that reference it:
dotnet build "Software\Interfaces\VRF_C2SIM\src\VrfC2SimApp" -c Release --disable-build-servers
```
Expect 0 errors on BOTH TFMs.

### 1.3 Offline gate

- All eight selftests (0.2) pass - these load the SDK assembly, so a broken SDK build would
  surface.
- If the SDK test project builds on this machine, run RestLibTests:
  `dotnet test "Software\Library\CS\C2SIMSDK\C2SIMSDK.Tests" -c Release` (the tests use a
  fake REST server on real sockets; live-server tests are env-gated and may skip - that is
  fine). A green RestLibTests run is strong evidence the request path still works.

### 1.4 Live verification gate (the discriminator)

This is verified as a SIDE EFFECT of Step 5's scale run (do not schedule a separate live
run just for this). The decision rule: during a ~15-min live run at 20x with heavy position
reporting, grep the app log for "Only one usage of each socket address" AND "Connection
error:". PASS = ZERO such lines (every 2026-07-13 run had them; a clean run is the
discriminator). If they still appear, the fix did not take - STOP and re-examine (are report
pushes actually routing through the rebuilt SDK? is a stale SDK dll shadowing it?).

### 1.5 Acceptance criteria

- Both TFMs build 0 errors; eight offline selftests green; (if runnable) RestLibTests green.
- The change touches ONLY the two HttpClient sites + the one new static field (+ optional
  version/notes). No behavior change to headers, URLs, error handling.

### 1.6 Rollback

`git checkout Software/Library/CS/C2SIMSDK/C2SIMClientLib/C2SIMClientRestLib.cs` on the fork.
The change is isolated to one file; reverting restores the per-call client exactly.

### 1.7 STOP-AND-ESCALATE

- The netstandard2.0 leg will not compile even with the `#if` guard (unexpected - escalate
  with the exact error; do not disable the netstandard target).
- Any selftest that exercises a REST path starts failing after the change.

### 1.8 Commit

FORK commit (SDK): stage only
`Software/Library/CS/C2SIMSDK/C2SIMClientLib/C2SIMClientRestLib.cs` (+ version/notes if
bumped). Message e.g. "SDK P4a: shared static HttpClient (fix report-push port exhaustion);
per-request Accept".
PLUS a PORT docs commit + submodule bump: PORT.md sec 7 is the designated cross-reference for
SDK-side changes ("captured here for cross-reference") - add the P4a fix there and touch the
START_HERE SDK status line. So Step 1 ALWAYS yields both a fork SDK commit and a port docs
commit (port pushed first, then the fork bump, per 0.4).

---

## Step 2 - Fan-out robustness: completion quorum + straggler timeout

GOAL: one stuck member entity must not hold a unit's task open forever. Add a completion
QUORUM (synthesize the unit TASKCMPLT when a fraction of members finish) and a per-fan-out
straggler TIMEOUT (synthesize with a warning after N seconds), both opt-in and idempotent,
and swallow the late stragglers that arrive after synthesis.

WHY / EVIDENCE (UNIT_MOVEMENT_RESEARCH.md sec 4c; PLAN_DERISK_NOTES sec 3): the COA-STP1
unblock run scored 5/7 because each of the 2 CoHQs finished 3/4 members - ONE stuck GndV
per unit held the unit task open (46/52 members marched). FanOutTracker already stores Total
and returns `remaining` per completion, so a quorum is (Total - remaining)/Total evaluated
at completion time.

THE SUBTLETY THE PLAN MUST HANDLE (PLAN_DERISK_NOTES sec 3, found by design review): after a
quorum/timeout synthesis, LATE straggler completions must be SWALLOWED. Today
TryCompleteMember removes the fan-out on allDone; a member completing AFTER a quorum synthesis
would find no fan-out record, fall through to the unit-level path in OnVrfTaskCompleted, and -
because no in-flight task is recorded for that member name - emit a spurious TASKCMPLT with an
empty task uuid ("NO in-flight task recorded" warning). So the fan-out record must live on in a
SYNTHESIZED state that silently swallows remaining member completions (log at debug) until the
last member completes or a new task supersedes it.

REPO: PORT (submodule). Files:
- src/VrfC2SimApp/FanOutTracker.cs (the tracker + its state machine)
- src/VrfC2SimApp/VrfC2SimService.cs (wire the timer + honor the new synthesis signal)
- src/VrfC2SimApp/VrfSettings.cs (two new opt-in settings)
- src/VrfC2SimApp/FanOutSelfTest.cs (extend --fanout-selftest)

### 2.1 Settings (VrfSettings.cs, add near SubordinateFanOut, with the same comment density)

```csharp
    // R10 fan-out robustness (UNIT_MOVEMENT_RESEARCH.md sec 4c). Completion QUORUM: synthesize
    // the unit's TASKCMPLT once this FRACTION of fanned members complete (1.0 = today's
    // behavior: ALL must finish). Guards against one stuck member holding the unit task open
    // (the 3/4-CoHQ gap in the COA-STP1 unblock run). Late stragglers after synthesis are
    // swallowed (the tracker's Synthesized state), not re-reported. Range (0,1]; <=0 or >1
    // clamp to 1.0.
    public double FanOutCompletionFraction { get; set; } = 1.0;

    // R10 fan-out robustness: per-fan-out straggler TIMEOUT in seconds. If the quorum has not
    // been reached this long after the fan-out is registered, synthesize the unit completion
    // anyway WITH A WARNING (a member never completing - e.g. a stuck GndV - no longer hangs
    // the unit task). 0 = OFF (no timeout; rely on quorum/all-complete only). Either trigger
    // fires the synthesis at most once (idempotent).
    public int FanOutStragglerSeconds { get; set; } = 0;
```

### 2.2 FanOutTracker.cs - add a Synthesized state + a quorum/timeout synthesis path

Design (keep the class PURE and thread-safe - it is offline-tested and touched from the tick
thread and, once the timer lands, from a timer callback):

- Add a `bool Synthesized` flag to the private `FanOut` record and a `double Fraction`
  (captured at Register; default 1.0). Register gains a `double completionFraction` param
  (clamp to (0,1]); store `Total` and `Fraction`.
- `TryCompleteMember`: unchanged happy path, but compute allDone as a QUORUM:
  - remove the member from Pending and from _unitByMember as today;
  - `int completed = f.Total - f.Pending.Count;`
  - `bool quorumMet = completed >= (int)Math.Ceiling(f.Total * f.Fraction);`
  - if the fan-out is ALREADY `Synthesized` (a late straggler after a prior quorum/timeout
    synthesis): DO NOT report a unit completion. Return a distinct result so the caller
    SWALLOWS it (e.g. return true with a new `out bool alreadySynthesized = true`, allDone
    false). When Pending hits 0 while Synthesized, remove the record entirely.
  - else if `quorumMet` and not yet Synthesized: set `f.Synthesized = true`, set
    `allDone = true` (the caller synthesizes the unit TASKCMPLT now). If Pending is now empty,
    remove the record; otherwise KEEP it (Synthesized) so remaining members are swallowed.
  - else: allDone false, return the remaining count (the existing "member N remaining" log).
- Add `bool TrySynthesizeByTimeout(string unitName, string expectedTaskUuid, out int completed, out int total)`:
  under lock, if the unit has a NON-Synthesized fan-out AND its stored TaskUuid equals
  `expectedTaskUuid`, mark it Synthesized and return true with completed/total counts (for the
  warning). Return false if the fan-out is absent, already Synthesized, or its TaskUuid differs.
  The uuid guard is LOAD-BEARING, not belt-and-suspenders: supersession cancels the old record
  and Registers a NEW one under the same unit name - without the guard, the OLD task's timer
  firing later would find the NEW task's record and synthesize ITS completion prematurely.
  The Synthesized flag makes timer-vs-quorum idempotent; the uuid makes timer-vs-supersession safe.
- `Cancel`/`CancelLocked` (supersession) already drop the record and its member map entries -
  keep as is; a superseding task Registers anew (which CancelLocked-clears the old, including
  a Synthesized one).

Adversarial note to encode in the tests: fraction 1.0 MUST reproduce today's exact behavior
(synthesize only when the last member completes; no early synthesis; ceil(Total*1.0)==Total).

### 2.3 VrfC2SimService.cs - wire the fraction, the timer, and the swallow

TIMER SEMANTICS (decide and document): the straggler timer is a HARD CAP measured from
Register (fan-out start), NOT an idle timeout. Set it GENEROUSLY relative to healthy member
completion time so it fires ONLY for genuinely-stuck members: in the R10 unblock run healthy
members completed in ~4 min at 20x, so e.g. 600 s comfortably clears them and still fires well
within the ~35-min window for a stuck GndV. If all members complete first, the fan-out is
already removed and TrySynthesizeByTimeout no-ops (safe). (A future refinement - an IDLE timeout
that resets on each member completion, more robust to route length/multiplier - is noted but NOT
required for this step.)

- At Register (currently service:835): pass `_vrf.FanOutCompletionFraction`. Immediately after
  a successful Register with a positive `FanOutStragglerSeconds`, start a detached straggler
  timer for this unit (mirror EngageFallbackAsync at :894-904, gated on `_stoppingToken`):
  ```csharp
  if (_vrf.FanOutStragglerSeconds > 0)
      _ = FanOutStragglerAsync(unit.Name, task.TaskUuid);
  ```
  `FanOutStragglerAsync(unitName, capturedTaskUuid)` awaits
  `Task.Delay(FanOutStragglerSeconds, _stoppingToken)`, then calls
  `_fanOut.TrySynthesizeByTimeout(unitName, capturedTaskUuid, out completed, out total)`; if it
  returns true, log a WARNING ("fan-out straggler timeout for {Unit}: {completed}/{total}
  members done - synthesizing unit completion") and call the SAME unit-completion synthesis
  path OnVrfTaskCompleted uses. Factor the tail of OnVrfTaskCompleted (currently :1061-1102)
  into a private helper `SynthesizeUnitCompletion(string unitName, string vrfTaskTypeForLog)`
  and call it from BOTH OnVrfTaskCompleted (the allDone branch, after `name = fanUnit`) and the
  timer. NOTE the capturedTaskUuid is ONLY the supersession guard inside the tracker (see 2.2);
  the taskUuid for the report/sequencer comes from `_inFlight.TryComplete(unitName)` inside
  SynthesizeUnitCompletion (the in-flight record was written by MarkDispatched at Register,
  service:833). Because `_inFlight.TryComplete` REMOVES the record and the Synthesized flag
  blocks the second trigger, only ONE of {last-member quorum, timeout} can ever reach
  SynthesizeUnitCompletion for a given task - no double TASKCMPLT.
- At OnVrfTaskCompleted (:1049): honor the new `alreadySynthesized` swallow result - if the
  tracker says this member belongs to an ALREADY-synthesized fan-out, log at Debug ("late
  straggler {Member} of {Unit} after synthesis - swallowed") and RETURN. Do not fall through
  to the unit-level path (that is the spurious-empty-uuid bug this step exists to prevent).

Concurrency (VERIFIED against the current code - the timer path is SAFE, no tick-thread
marshalling redesign needed): today TryCompleteMember runs on the tick thread only; the timer
adds a second caller. FanOutTracker's `_lock` already serializes all fan-out state - keep every
access under it. The synthesis SIDE EFFECTS are all safe OFF the tick thread, exactly as the
existing EngageFallbackAsync timer proves: `_inFlight`/`_sequencer`/`_c2SimUuidByName` are
thread-safe (concurrent collections / a thread-safe sequencer); `PushReportAsync` is
fire-and-forget network; and the ONE bridge-touching action - a deferred engage - is issued via
`IssueEngage`, which does NOT call the bridge directly but ENQUEUES it on `_tickActions`
(service:915) for the tick loop to drain. SynthesizeUnitCompletion (the factored :1061-1102
tail) contains NO direct `_bridge.*` call - confirm this stays true when you factor it; if any
future edit adds a direct bridge call there, it MUST go through `_tickActions.Enqueue`.

### 2.4 Optional (cheap, same file): fan out the single-point MoveToLocation path

Today fan-out covers only the multi-point route path (service:814 is inside the CreateRoute
branch); the single-point branch (:781-793 MoveToLocation) does not fan out. If time permits,
mirror the fan-out there: read members, if any, issue `_bridge.MoveToLocation(m.Uuid, routeGeo[^1])`
per member and Register the fan-out. Keep it opt-in under the SAME SubordinateFanOut flag.
This is a NICE-TO-HAVE; if it adds risk, defer it and note the deferral.

### 2.5 Extend --fanout-selftest (FanOutSelfTest.cs)

Add cases (the count grows from 16; the new total becomes this step's gate number - record it
in START_HERE + the 0.2 list):
- quorum at 3/4 (fraction 0.75): synthesize on the 3rd of 4, allDone true at that point;
- late-straggler swallow: the 4th completion after a 3/4 quorum returns the swallow result and
  does NOT report a unit completion;
- supersession while Synthesized: Register a new task for the unit clears the Synthesized
  record; a stale member of the old fan-out then no-ops;
- fraction 1.0 == legacy: synthesize ONLY on the last member (regression guard);
- timeout synthesis: TrySynthesizeByTimeout returns true once, then false (idempotent);
  false when the expectedTaskUuid no longer matches (supersession replaced the fan-out);
  false after all members already completed (record removed - the timer no-ops).

### 2.6 Build / offline gate

```
$env:DOTNET_CLI_USE_MSBUILD_SERVER = "false"
dotnet build "Software\Interfaces\VRF_C2SIM\src\VrfC2SimApp" -c Release --disable-build-servers
```
Then all eight selftests (0.2), with --fanout-selftest now at the NEW count, ALL PASSED.
No bridge rebuild is needed (this is app-only; GetAggregateMembers already exists).

### 2.7 Live verification (folded into Step 5)

Decision rule at the scale run: with `Vrf:SubordinateFanOut=true` plus the STRAGGLER TIMEOUT as
the primary lever (`FanOutStragglerSeconds` set generously past healthy completion - see 2.3),
the 2 CoHQs that previously ended 3/4 (one stuck GndV each) now synthesize a unit TASKCMPLT WITH
a straggler warning, raising the 5/7 toward 7/7 WITHOUT any spurious empty-uuid TASKCMPLT
warnings. Prefer the timeout over a <1.0 quorum here because the quorum fraction is GLOBAL:
0.75 on the healthy 18-member companies would declare them complete at 14/18 and swallow 4
legitimate marchers. Keep `FanOutCompletionFraction=1.0` unless the supervisor deliberately
wants earlier unit-level declaration. Telemetry (WatchVrf) still governs the member MOVEMENT
claim; the timeout only governs when the UNIT-level completion is declared. Confirm the log
shows the straggler warning for the stuck member(s) and NO "NO in-flight task recorded" line.

### 2.8 Acceptance criteria

- App builds 0/0; eight selftests green with the new --fanout-selftest count.
- fraction 1.0 is byte-for-byte behavior-identical to today (the regression case proves it).
- No spurious unit TASKCMPLT on late stragglers (the swallow case proves it offline; the scale
  run proves it live).

### 2.9 Rollback

The change is confined to the four files. `git checkout` them to restore. Because the new
behavior is gated on FanOutCompletionFraction<1.0 OR FanOutStragglerSeconds>0 (both default to
the legacy 1.0 / 0), leaving the code in but the settings at default is ALSO a safe no-op
fallback if only the live tuning misbehaves.

### 2.10 STOP-AND-ESCALATE

- Factoring SynthesizeUnitCompletion out of OnVrfTaskCompleted forces a DIRECT `_bridge.*` call
  onto the timer thread (it should not - the :1061-1102 tail has none today; deferred engages go
  through IssueEngage -> _tickActions). If you cannot keep it bridge-call-free -> STOP; route the
  bridge work through `_tickActions.Enqueue` before any live run.
- The swallow logic cannot distinguish a legitimate NEW unit-level completion from a late
  straggler for a superseded task -> STOP; the empty-uuid TASKCMPLT is exactly the corruption
  we are removing.
- fraction 1.0 does NOT reproduce today's behavior byte-for-byte in the regression selftest
  (early synthesis, or the last-member case changed) -> STOP; the opt-in default must be a no-op.

### 2.11 Commit

PORT commit (four files + doc updates), then fork submodule bump. Message e.g. "R10 fan-out
robustness: completion quorum + straggler timeout + swallow late stragglers; --fanout-selftest
extended".

---

## Step 3 - P4b: position-report bundling (C++-parity shape, opt-in)

GOAL: bundle POSITION reports into one report envelope carrying N ReportContent blocks, with
the C++ flush triggers, to cut the report volume. TASKCMPLT stays unbundled.

WHY / EVIDENCE (PLAN_DERISK_NOTES sec 2): the C++ oracle (frozen repo, textIf.cxx:435-530
sendC2simReport) bundles POSITION reports only ("Observation/TaskStatus have other reasons
not to bundle"); a bundle is ONE report envelope whose body carries N ReportContent blocks;
the ReportID applies to the WHOLE bundle and is minted at send time. Flush triggers: report
count >= 10 (maxReportsPerBundleTextIf, "STOMP may balk at larger"), total size >= 10240
(maxBundleSizeTextIf), or a ~2-second reminder thread that force-flushes a partial bundle.
Bundling was opt-in (argv 17). .NET readiness (verified): ReportBuilder already fills
`ReportContent = new[] { ... }`; the generated ReportBodyType takes an ARRAY of
ReportContentType, so a multi-content body is a DATA change, not a schema fight.

ORDERING NOTE: do this AFTER P4a and its live check. P4a alone may fully clear the 10048
errors, making P4b a parity/politeness feature rather than a firefight. If the Step 5 run (with
P4a in) shows ZERO 10048s, P4b priority drops - still implement it for parity, but it is no
longer load-bearing. If P4a-with-a-single-client still shows pressure, P4b becomes the closer.

DECISION (2026-07-13, user-confirmed): P4b is DELIBERATELY NOT in the FIRST scale run (Step 5).
The first scale run carries Steps 1-2 only, so the P4a "zero 10048" verdict is not confounded by
a possible P4b multi-content server rejection (the one genuine live unknown here - see 3.9).
P4b gets its own SHORTER live pass AFTER the first scale run confirms P4a clean. Implement +
offline-gate + commit P4b whenever convenient; just do not enable Vrf:BundlePositionReports in
the Step 5 run.

REPO: PORT (submodule). Files:
- src/VrfC2SimApp/ReportBuilder.cs (a bundle builder)
- src/VrfC2SimApp/VrfC2SimService.cs (an accumulator in the OnVrfTextReport path + a flush
  timer + flush-on-stop)
- src/VrfC2SimApp/VrfSettings.cs (opt-in settings)
- src/VrfC2SimApp/ReportSelfTest.cs (extend --report-selftest)

### 3.1 ReportBuilder.cs - add a bundle builder

Add alongside BuildPositionReport (keep the single-content one for the unbundled path):
```csharp
    /// <summary>Position report bundle: ONE ReportBody carrying N PositionReportContent
    /// blocks (C++ parity, textIf.cxx:435-530 - position reports only; one ReportID for the
    /// whole bundle, minted at send). ReportingEntity/From/To mirror the single-content build.</summary>
    public static string BuildPositionReportBundle(
        IEnumerable<(string uuid, double latDeg, double lonDeg)> fixes,
        string isoDateTime, string reportId)
```
It builds `ReportContent = fixes.Select(f => new ReportContentType { Item = new
PositionReportContentType { TimeOfObservation = Time(isoDateTime), Location = Geo(f.latDeg,
f.lonDeg), SubjectEntity = f.uuid } }).ToArray()`, with `ReportID = reportId`. Decide
ReportingEntity: a bundle has N different subjects, so the single-report convention
(ReportingEntity = subjectUuid) cannot hold. Before guessing, CHECK the C++ oracle (frozen
repo, textIf.cxx:435-530) for what it put in the bundle envelope's reporting entity and mirror
that; if it is ambiguous, first choice = the FIRST fix's subject uuid (closest to single-report
semantics), fallback = ZeroUuid. If the server rejects the first choice live, try the other
before escalating under 3.9. Keep FromSender/ToReceiver = ZeroUuid as today. Serialize via
C2SIMSDK.FromC2SIMObject.

### 3.2 Settings (VrfSettings.cs)

```csharp
    // P4b position-report bundling (C++ parity, textIf.cxx:435-530). OFF = one PositionReport
    // per POSITION line (today's behavior). ON = accumulate POSITION reports into one envelope
    // (N ReportContent) and flush on count/size/timer. TASKCMPLT is never bundled. Opt-in.
    public bool BundlePositionReports { get; set; } = false;
    public int BundleMaxReports { get; set; } = 10;      // C++ maxReportsPerBundleTextIf
    public int BundleMaxBytes { get; set; } = 10240;     // C++ maxBundleSizeTextIf
    public int BundleFlushMs { get; set; } = 2000;       // C++ ~2 s reminder-thread flush
```

### 3.3 VrfC2SimService.cs - accumulator + flush

- Add a private accumulator: `List<(string uuid, double lat, double lon)> _posBundle` guarded
  by a `_posBundleLock`, plus a running serialized-size estimate (or re-serialize on flush and
  compare - simpler is to flush on count/timer and treat size as a secondary guard; document
  whichever you pick).
- OnVrfTextReport (:1105-1124): when `_vrf.BundlePositionReports` is true, instead of building +
  pushing a single report, take the lock, add the fix, and if `count >= BundleMaxReports` (or the
  size estimate >= BundleMaxBytes) call FlushPositionBundle(). When false, keep exactly
  today's single-report path (parity).
- FlushPositionBundle: under the lock, if the buffer is non-empty, snapshot + clear it, then
  BuildPositionReportBundle(snapshot, IsoNow(), NewReportId()) and PushReportAsync. Mint the
  ReportID at flush (matches "minted at send time").
- Timer: start a periodic flush every `BundleFlushMs` (a detached loop gated on `_stoppingToken`,
  mirroring the existing detached-timer pattern; or a System.Threading.Timer). It force-flushes a
  partial bundle so a trickle of reports is not held indefinitely.
- Flush on stop: in the clean-stop path, flush any pending bundle BEFORE resign so no reports are
  lost. Place it with the existing cleanup (near the Solution A delete sweep) but BEFORE the
  bridge resign.

Thread-safety: OnVrfTextReport and the flush timer both touch the buffer - the lock covers it.
Keep the buffer operations short (snapshot-under-lock, build+push outside the lock) to avoid
holding the lock across the serialize.

### 3.4 Extend --report-selftest (ReportSelfTest.cs)

Add checks: BuildPositionReportBundle with 3 fixes produces a body with 3 ReportContent blocks,
each a PositionReportContent with the right uuid/lat/lon; the bundle round-trips (parse back)
to 3 contents; a 1-fix bundle equals the single-content shape semantically; ReportID is present
once for the whole body. Keep the existing 9 checks; the new total is this step's gate number.

### 3.5 Build / offline gate

App-only build (3.3 command). Eight selftests, --report-selftest at the new count, ALL PASSED.
Because bundling is opt-in (default false), the golden/unbundled path is untouched - the
existing report round-trip checks still pass unchanged.

### 3.6 Live verification (folded into Step 5, or a short dedicated pass)

With `Vrf:BundlePositionReports=true` at 20x: the app log shows position reports going out in
bundles (one push per ~10 fixes or ~2 s), the C2SIM server accepts them (no schema rejects),
and a listener (tools/ListenReports) can still parse them. Combined with P4a, ZERO 10048s.
Telemetry (WatchVrf) is unaffected - it reads reflected positions, not the C2SIM report stream.

### 3.7 Acceptance criteria

- App builds 0/0; eight selftests green with the new --report-selftest count.
- Default (BundlePositionReports=false) is byte-for-byte the current single-report behavior.
- A bundle carries N contents, one ReportID, and round-trips; TASKCMPLT is never bundled.

### 3.8 Rollback

Confined to the four files; `git checkout` restores. Default-off means leaving the code in with
the setting false is a safe no-op if only the bundling behavior misbehaves live.

### 3.9 STOP-AND-ESCALATE

- The C2SIM server REJECTS a multi-content ReportBody (schema/impl mismatch vs the C++ that
  reportedly sent them) -> STOP; capture the server error; the .NET schema was array-ready but
  the SERVER's acceptance is the live unknown.
- Reports are LOST on stop (flush-on-stop not landing before resign) -> fix the ordering before
  claiming parity.

### 3.10 Commit

PORT commit (four files + docs), then fork submodule bump. Message e.g. "P4b: opt-in
position-report bundling (C++-parity: N ReportContent/envelope, count/size/timer flush)".

---

## Step 4 - coa-gpt data memo (4 evidence-backed items)

GOAL: a single memo that feeds the four evidence-backed data-quality findings back to the
coa-gpt COA generator. Pure writing task - NO code, NO live run.

WHY / EVIDENCE (PLAN_DERISK_NOTES sec 5; PORT.md sec 10; UNIT_MOVEMENT_RESEARCH.md sec 4-4c;
SEMANTIC_MAPPING.md sec 2b/3): each item is already proven in the docs; the memo just
assembles them for the COA-generation audience.

DELIVERABLE: docs/COA_GPT_FEEDBACK.md (new file, in the PORT repo). ASCII-only. Structure: a
one-paragraph framing (keep coa-gpt emitting RICH semantics - do NOT dumb it down to the bare
interface; these are DATA-quality fixes, not semantic reductions), then the four items, each
with: the finding, the EVIDENCE (cite the doc + the live run), the IMPACT on VR-Forces
execution, and the concrete ASK.

The four items:
1. DISTINCT AffectedEntity for engagement verbs. Evidence: ALL 42 COA-STP1 tasks self-target
   (AffectedEntity == PerformingEntity) - SEMANTIC_MAPPING.md sec 2b/5 (Unit 3), PORT.md sec 10.
   Impact: the fires/breach/escort paths have no target -> every engagement verb degrades to
   bare movement. Ask: emit a real distinct AffectedEntity (a valid OPFOR uuid for ATTACK-family,
   an obstacle for BREACH, the escorted unit for ESCRT).
2. Timing hygiene. Evidence: task T13 carries a 12,000,000 ms (3h20m) SimulationTime start delay;
   durations 1h20m-3h20m (PORT.md sec 5/10). Impact: unwatchable scenarios; huge idle gaps. Ask:
   sane start delays and set SimulationRealtimeMultiple so scenarios are watchable.
3. DISPERSED unit positions (nuanced by R8). Evidence: COA-STP1 packs 54 units at one identical
   coordinate (34.679985,-116.724799); the golden init is also stacked (max pile 13) but marched
   - the distinguishing pathology is pile SIZE (UNIT_MOVEMENT_RESEARCH.md sec 4/4b). NUANCE: R8
   create-time de-stacking mitigates it interface-side, and the R8 A/B FALSIFIED stacking as the
   SUFFICIENT blocker (geography dominates) - so present dispersion as good hygiene that removes
   gridlock, NOT as the movement fix. Ask: disperse unit positions; do not co-locate dozens of
   units at one coordinate.
4. REGION VALIDATION (the strongest, newest item). Evidence: R9 region swap CONFIRMED geography
   as the aggregate blocker - at the Mojave COA-STP1 region VR-Forces returns EMPTY unit
   leader-path plans (`moveAlong() - empty route`, ZERO member Offset Routes) while the golden
   Sweden site plans fine; R10 fan-out is the interface-side unlock but forfeits formation
   keeping (UNIT_MOVEMENT_RESEARCH.md sec 4c). Impact: disaggregated units cannot maneuver as
   units at a path-dead region regardless of the interface. Ask: validate a candidate region with
   a 1-unit move probe BEFORE generating COAs there, or pick regions with known-good ground
   content (the golden Sweden site works).

### 4.1 Acceptance / gate

- The memo exists, is ASCII-only (`rg -P "[^\x00-\x7F]" docs/COA_GPT_FEEDBACK.md` returns
  nothing), and every claim cites a source doc + (where live) the run that proved it.
- No code, no build, no selftest impact. GATE-DIFF only (supervisor reads the memo).

### 4.2 Rollback

Delete the file. No other artifact depends on it.

### 4.3 STOP-AND-ESCALATE

- Drafting surfaces a claim the cited source docs do NOT actually support -> STOP; this memo is
  OUTWARD-FACING (it goes to the coa-gpt team). Fix the source doc first or drop the claim; do
  not ship an overclaimed finding externally.

### 4.4 Commit

PORT commit (the new doc + a pointer from START_HERE/PORT sec 10), then fork submodule bump.

---

## Step 5 - COA-STP1 FULL 42-task order scale run (LIVE)

> EXECUTED 2026-07-13 (apps 3355-3359; see Appendix B + docs/experiments/COA-STP1_scale_2026-07-13.txt).
> DO NOT RE-RUN - it wastes an irreversible live run. Outcome: the pipeline HELD at scale (128 units /
> 42 tasks; de-stack + auto-formation + fan-out + Step-2 robustness fired; P4a zero 10048) BUT it
> surfaced F1 member RUNAWAYS (a member drove 53.8 km, ~18 km past its route end) and F2b VACUOUS
> completions (full member quorum, zero telemetry arrival). Movement quality is UNSOLVED (tied to the
> Mojave aggregate freeze). Everything below (incl. the 5.1 "START AT 3355" config) is the ORIGINAL
> plan / that run's config, retained for provenance - for any NEW run use the Appendix B ledger tail.

GOAL: exercise the full COA-STP1 order (42 tasks) end to end at scale with de-stack + auto
formation + fan-out + the Step-2 robustness, proving the pipeline holds at scale, the P4a fix
clears the 10048 errors, and unit completions far exceed the R5c-era count.

THIS IS A LIVE RUN. GATE-ENV before launch, GATE-VERDICT after. Steps 1-2 must be committed and
their offline gates green first. P4b (Step 3) is DELIBERATELY EXCLUDED from this first run
(user-confirmed decision, Step 3 ORDERING NOTE) so the P4a "zero 10048" verdict is clean; leave
Vrf:BundlePositionReports=false here. Read RUNBOOK sec 7 in full before starting.

WHY / EVIDENCE (PLAN_DERISK_NOTES sec 4): data/COA-STP1_Order.xml has 42 tasks, 11 distinct
performers, ALL self-target (verbs degrade to movement - expected/documented), 32 temporal deps
(the P0.2 skip policy governs), and 3 SCREEN tasks that are PATROL tasks - NOTE fan-out
deliberately EXCLUDES patrol (service:801/814 short-circuits it) and patrols never self-complete,
so those 3 will not fan out and will not report completion (expected).

### 5.1 Recommended run configuration (confirm with supervisor at GATE-ENV)

- Init: data/COA-STP1_Initialization.xml (128 units + 35 areas). ClientId=C2SIM (MUST equal the
  init SystemName or 0 units are created).
- Env knobs: `Vrf__DeStackCreates=true`, `Vrf__AggregateFormation=auto`,
  `Vrf__SubordinateFanOut=true`, `Vrf__TimeMultiplier=20`, plus the Step-2 robustness:
  `Vrf__FanOutStragglerSeconds=600` (the surgical lever: fires only for genuinely-stuck members;
  see Step 2.3/2.7) and `Vrf__FanOutCompletionFraction=1.0` (default; do NOT lower it globally -
  0.75 would truncate the healthy 18-member companies at 14/18). Also set
  `Vrf__TaskPredecessorTimeoutSeconds` EXPLICITLY (default 600; past experiments overrode it via
  env - do not inherit silently): with 32 temporal deps under the skip policy, each
  never-completing predecessor (the 3 patrols, no-location tasks) holds its successors up to
  this long from dispatch before skipping, which stretches the run - size the observation
  window and WatchVrf duration for it (45-60 min is safer than 35; a SECOND WatchVrf pass on a
  fresh appNo is fine if the first window expires mid-run). Make ALL experiment overrides
  EXPLICIT (do not rely on inherited env).
- Order: data/COA-STP1_Order.xml (the full 42-task order - this is the scale test, NOT the 7-task
  E1 probe).
- AppNos: START AT 3355 (this EXECUTED run's numbers, apps 3355-3359; for any NEW run take the
  number from the single "*** NEXT FREE: <number> ***" marker in Appendix B - NOT from the
  ledger tail, and NOT the "currently 3386" this line used to cache; both are wrong, see the
  correction near the top of this file). The app, ResetVrf, and WatchVrf each need their own
  fresh number. Record each in Appendix B as consumed.

### 5.2 Procedure (RUNBOOK sec 3 + sec 7; do NOT re-derive)

1. GATE-ENV: run Appendix A preflight; show output; get sign-off.
2. ResetVrf (fresh appNo) to clear any accumulated entities/orphans (`--dry-run` first to see
   what is present, then the real sweep).
3. PushInit data/COA-STP1_Initialization.xml -> expect "QUERYINIT: 128 Units". (Init FIRST,
   while NO app is running.)
4. Start WatchVrf (fresh appNo, duration covering the run - 45-60 min per the 5.1 window note,
   20 s samples) to CSV - this is the movement oracle. Starting it BEFORE the app is deliberate:
   it captures the spawn positions as the displacement baseline.
5. Start the app (fresh appNo, the 5.1 env, cwd=VRF bin64, --contentRoot=<exe dir>). Judge
   connect by THREAD COUNT (~9-10), not the block-buffered log.
6. PushOrder data/COA-STP1_Order.xml.
7. Observe to completion window (45-60 min at 20x - see the 5.1 predecessor-timeout note).
   Do NOT force-kill anything.
8. Clean stop via tools/StopIface (server STOP -> RESET -> UNINITIALIZED -> app resigns). Confirm
   no stale federate (rtiexec process count back to baseline).
9. ResetVrf again if the next run is heavy (accumulation).

### 5.3 Live decision rules (TELEMETRY-gated; the R11 trap governs)

- MOVEMENT is claimed ONLY from WatchVrf per-object displacement (fanned member entities marching
  their routes, ~1 km cohorts). A TASKCMPLT with no corresponding WatchVrf displacement is NOT
  movement - it is the R11 vacuous-completion trap; report it as such.
- UNIT COMPLETIONS: count synthesized unit TASKCMPLTs. Target: >> the R5c-era 0/6; the prior
  unblock run scored 5/7 on the 7-task probe. On the full order, the acceptance is that the
  aggregate MOVE-class tasks whose members can path (the platoons + companies proven at Mojave in
  R10) complete, and the Step-2 quorum/timeout closes the stuck-straggler CoHQ gap (no unit task
  left hanging on one stuck member).
- PORT EXHAUSTION (the P4a discriminator): grep the app log for "Only one usage of each socket
  address" and "Connection error:". PASS = ZERO. This is the primary P4a live gate (Step 1.4).
- FAN-OUT ROBUSTNESS (the Step-2 gate): a stuck member triggers the straggler WARNING and a
  synthesized unit completion; there are NO "NO in-flight task recorded" / empty-uuid TASKCMPLT
  lines.
- Expected non-completions (NOT failures): the 3 SCREEN patrols (excluded from fan-out, never
  self-complete); tasks with no location points (order data); tasks gated behind a skipped
  predecessor (P0.2 skip policy).

### 5.4 Acceptance criteria

- 128 units + 35 areas created; de-stack fires on the mega-pile (log: "54 units at ... spread");
  auto formation repair applies (113/113-class); fan-out dispatches on aggregate MOVE-class tasks
  (recursion surfaces the rosters).
- WatchVrf shows real member displacement for the fanned aggregates (telemetry-verified).
- Unit TASKCMPLTs far exceed the R5c-era count; the Step-2 quorum/timeout removes hung-on-straggler
  units.
- ZERO 10048/"Connection error:" lines (P4a proven live).
- Clean stop, no stale federate; Solution A + ResetVrf leave a clean slate.

### 5.5 Rollback / recovery

- A live run is not "rolled back" - but if it degrades (accumulated federation stops reflecting
  creates), ResetVrf and re-run per RUNBOOK sec 7's accumulation note. If a federate goes stale
  from an ACCIDENTAL force-kill (never do it deliberately), recover per RUNBOOK sec 5 (the human
  reloads the VR-Forces scenario) - escalate.

### 5.6 STOP-AND-ESCALATE

- The sim BACKEND (vrfSimHLA1516e) is unresponsive (not just vrfGui hung) -> STOP, coordinate.
- The loopback proxy is slow and a Docker Desktop restart does not fix it -> STOP.
- 10048 errors STILL appear after P4a is confirmed in the running build -> STOP; the fix is not
  in the live path (stale SDK dll? report pushes bypassing the shared client?).
- The run leaves a stale federate ResetVrf cannot clear -> STOP.

### 5.7 Docs + commit

Record the run in UNIT_MOVEMENT_RESEARCH.md (a new dated subsection under sec 4c), update
START_HERE "Current status", and archive the raw evidence (WatchVrf CSV extract + the de-stack/
dispatch/completion/error log lines) under docs/experiments/COA-STP1_scale_<date>.txt. GATE-VERDICT
before the docs land. PORT commit + fork submodule bump.

---

## Step 6 - Housekeeping: 6 tools csproj relative SDK paths

GOAL: make the six SDK-referencing tool csprojs use RELATIVE ProjectReference paths (they carry
ABSOLUTE machine paths today), so the repo builds on a fresh checkout / another machine.

WHY / EVIDENCE (PLAN_DERISK_NOTES sec 5; verified this session): six csprojs carry absolute SDK
paths. The app (src/VrfC2SimApp) sits at the same depth and uses the WORKING relative form
`..\..\..\..\Library\CS\C2SIMSDK\C2SIMSDK\C2SIMSDK.csproj` - a uniform substitution. (tools/ResetVrf
and tools/WatchVrf reference the BRIDGE, not the SDK - leave them alone.)

REPO: PORT (submodule). The six files + line numbers (current absolute -> relative):
- tools/ListenReports/ListenReports.csproj:11
- tools/PushInit/PushInit.csproj:11
- tools/PushOrder/PushOrder.csproj:11
- tools/SdkVerify/SdkVerify.csproj:12  (C2SIMSDK)  AND  :13 (C2SIMClientLib - TWO refs)
- tools/StompProbe/StompProbe.csproj:11
- tools/StopIface/StopIface.csproj:11

### 6.1 Change spec

Replace the absolute include with the relative form (tools/* are at the same depth as
src/VrfC2SimApp, so the SAME `..\..\..\..` prefix applies):
- `...\Library\CS\C2SIMSDK\C2SIMSDK\C2SIMSDK.csproj`
  -> `..\..\..\..\Library\CS\C2SIMSDK\C2SIMSDK\C2SIMSDK.csproj`
- SdkVerify's second ref:
  `...\Library\CS\C2SIMSDK\C2SIMClientLib\C2SIMClientLib.csproj`
  -> `..\..\..\..\Library\CS\C2SIMSDK\C2SIMClientLib\C2SIMClientLib.csproj`
Verify the exact depth by comparison with the app's working line (src/VrfC2SimApp csproj uses
exactly `..\..\..\..\Library\CS\C2SIMSDK\C2SIMSDK\C2SIMSDK.csproj`). Use backslashes (these are
Windows csprojs); MSBuild also accepts forward slashes if preferred - be consistent with the
app csproj.

### 6.2 Build / gate

Rebuild EACH of the six tools 0 errors:
```
$env:DOTNET_CLI_USE_MSBUILD_SERVER = "false"
foreach ($t in "ListenReports","PushInit","PushOrder","SdkVerify","StompProbe","StopIface") {
    dotnet build "Software\Interfaces\VRF_C2SIM\tools\$t" -c Release --disable-build-servers
}
```
Expect 0 errors each. This is purely mechanical (paths only) - no behavior change; the eight app
selftests are unaffected but run them anyway to prove nothing shifted.

### 6.3 Acceptance / rollback

- All six tools build from the relative paths; the app + tools still build on this machine.
- Rollback: `git checkout` the six csprojs.

### 6.4 STOP-AND-ESCALATE

- A tool fails to resolve the SDK via the relative path (depth mismatch) -> re-derive the depth
  from the actual directory tree; do NOT re-introduce an absolute path to "make it build".

### 6.5 Commit

PORT commit (the six csprojs), then fork submodule bump. Message e.g. "tools: relative SDK
ProjectReference paths (6 csprojs) for portable checkout".

(+ anything the user flags during plan review - e.g. the C++ repo private-remote decision, the
retained C++ originals deletion (migration step 1), or decoupling the SDK ProjectReference to a
published nuget. These are noted in START_HERE housekeeping #6 but are NOT in this plan's scope
unless the user adds them.)

---

## Appendix A - LIVE-RUN preflight checklist (run at every GATE-ENV)

```
# 1. Loopback proxy must be near-instant (do NOT restart the container as a habit).
#    Raw TcpClient, NOT Test-NetConnection - the latter adds ping/DNS overhead of its own and
#    can false-fail the <1 s threshold on a healthy proxy.
$t = [Net.Sockets.TcpClient]::new()
(Measure-Command { $t.Connect('127.0.0.1', 61613) }).TotalMilliseconds; $t.Dispose()  # expect ~ <100 ms; MUST be < 1000
# 2. C2SIM server reachable.
(Invoke-WebRequest "http://127.0.0.1:8080/C2SIMServer" -UseBasicParsing).StatusCode  # expect 200
# 3. RTI 4.6.1 (NOT 4.6b) + VRF + vrlink on PATH, in this order.
$env:PATH = "C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6.1\bin;$env:PATH"
# 4. License from MACHINE scope (a stale session value hangs checkout).
$env:MAKLMGRD_LICENSE_FILE = [Environment]::GetEnvironmentVariable('MAKLMGRD_LICENSE_FILE','Machine')
# 5. Sim backend healthy (do NOT kill vrfSimHLA1516e; vrfGui may be hung - that is fine).
Get-Process vrfSimHLA1516e,rtiexec -ErrorAction SilentlyContinue | Select Name,Id
# 6. Fresh appNo picked (from the "*** NEXT FREE: <number> ***" marker in Appendix B - NOT the ledger tail, NOT the stale "3386" once cached here; never reused). cwd will be C:\MAK\vrforces5.0.2\bin64;
#    the app gets --contentRoot=<exe dir>.
```
If loopback is slow: restart Docker Desktop (or reboot), re-measure, THEN proceed. Do not
proceed on a slow proxy - the STOMP client cannot ride it out.

## Appendix B - ApplicationNumber ledger

3200-3502: consumed, skipped, or RESERVED (see entries below). DO NOT infer the next free
number from this range header - it has gone stale before. Read the marker.

*** THE SINGLE AUTHORITATIVE NEXT FREE VALUE IS THE ONE VALUE-BEARING LINE OF THE FORM
    "*** NEXT FREE: <number> ***" BELOW. Search for that FORM, not the bare string
    "*** NEXT FREE:" - the bare string also matches this instruction and other pointers,
    so a literal search returns several lines and only ONE carries a number.
    Search for it. There is exactly ONE such line; if you ever find two, STOP and reconcile. ***
Do NOT infer it from the highest number you happen to see, and do NOT use the old rule
"take the number after the last ledger entry" - entries are NOT in numeric order and that
rule pointed at ALREADY-CONSUMED numbers on 2026-07-18. Record each join here as it is consumed (app /
ResetVrf / WatchVrf / SetSimRate / LaunchVrf back-end + front-end each take one). Never reuse.

CLAIMED 2026-07-18 for the SCRIPTED VR-Forces bring-up (scripts/LaunchVrf.ps1, combined
mode). Ledgered BEFORE the launch per the never-reuse non-negotiable. NOTE: these two
override the connection profile's baked-in 3001 (back-end) / 3101 (front-end); the profile
values are NOT used.
- 3460: USED - vrfLauncher back-end, scripted launch RUN 1, TropicTortoise. RESULT:
  backend STALLED at 2 threads, NEVER JOINED (no UDP 4000). Closed.
- 3461: USED - vrfLauncher front-end, RUN 1. Front-end fully healthy (47 threads, real
  window title) but never joined a federation. Closed.
- 3462: USED - ResetVrf --dry-run against RUN 1's stalled backend. HUNG at the RTI join
  (frozen at the rid.mtl banner, 0.5 CPU); killed under the failed-own-join exception,
  exit 255. No stale federate (never completed a join).
- 3463: BURNED UNUSED - ledgered for a second ResetVrf dry-run that never ran (the first
  never succeeded). Do NOT recycle.
- 3464: USED - vrfLauncher back-end, de-confounding run (overrides, clean assistant).
  STALLED. Cause later found to be the unanswered RTI Assistant prompt, not the overrides.
- 3465: USED - vrfLauncher front-end, same run.
- 3466/3467: USED - appNumber-override-only run (no scenario). STALLED, same cause.
- 3468/3469: DRY-RUN ONLY, never joined. Burned; do NOT recycle.
- 3470: USED - ResetVrf --dry-run #1, the 0.4 join gate against a launch made with
  RTI_ASSISTANT_DISABLE=1 (the documented fix, RTI RefMan 5.2.10).
- 3471: USED - ResetVrf --dry-run #2 (prereg requires TWO clean joins in a row).
  RESULT 3470+3471: BOTH JOINED CLEANLY, exit 0, resigned cleanly, NO 0xC0000005.
  Caveat: launch had NO scenario, so BackendCount=0 and 0 objects discovered - the
  prereg's "discovers 2 baseline objects" half was NOT exercised by these two.
- 3472: USED - vrfLauncher back-end, WITH TropicTortoise + appNumber overrides,
  RTI_ASSISTANT_DISABLE=1. Also the first fair test of the override args.
- 3473: USED - vrfLauncher front-end, same run.
- 3474: USED - ResetVrf --dry-run against that scenario-loaded launch. Joined cleanly,
  exit 0, no crash - but BackendCount=0 and 0 objects discovered.
- 3475: USED - WatchVrf discovery check under RTI_ASSISTANT_DISABLE=1. RESULT:
  reflected=0 readable=0 for 40 s - ORACLE BLIND. That configuration is abandoned.
- 3477/3478: USED - vrfLauncher back-end/front-end, TropicTortoise, assistant ENABLED
  and previously answered. NO DIALOG APPEARED. Backend healthy 67 threads, scenario
  loaded. First fully unattended zero-click launch in this effort.
- 3479: USED - WatchVrf discovery gate against that launch. RESULT: reflected=3
  readable=2, POS lines streaming, one uuid cross-confirmed against the backend log
  (Blocking Terrain Page-In Area). ORACLE SEES THE FEDERATION. Phase 1 unblocked.
- 3480: USED - tools/CreateOne, created M1A2 ORACLETEST at (34.517156,-116.973525) alt 10000 MSL. uuid=VRF_UUID:adfaadb3-da02-b04a-8f38-abd41c8049d1 entityId=1:3477:5. BackendCount=1.
  settle whether WatchVrf reports REAL coordinates for a REAL entity (all POS lines seen
  so far were NaN, on non-entity control objects only).
- 3481: USED - WatchVrf POS-fidelity gate. PASSED: POS,...,34.517156,-116.973525,1060.7 - exact requested lat/lon, ground-clamped alt (10000 -> 1060.7), stable across all samples. ORACLE FULLY VERIFIED.
  LINK before Phase 1.
- 3482/3483: USED - LaunchVrf.ps1 end-to-end run that exposed the rtiexec-refusal and duplicated-health-expression defects.
- 3484/3485: USED - LaunchVrf.ps1 end-to-end, ZERO human interaction, EXIT=0 READY (66 threads, UDP 4001).
- 3486/3487: USED - LaunchVrf.ps1 clean relaunch to reset scenario state (removes the
  CreateOne throwaway). EXIT=0 READY.
- 3488: USED - WatchVrf confirming the throwaway is gone; reflected=3 readable=2, no
  adfaadb3. NOTE: discovery reported 0 until t=13.3s - allow ~15 s before judging.
- 3489: USED - ResetVrf --dry-run #1 of the 0.4 GATE, against the WORKING configuration
  (assistant answered; NOT RTI_ASSISTANT_DISABLE). Joined cleanly, discovered 3 reflected
  (2 deletable = the TropicTortoise baseline objects), no deletes, resigned cleanly,
  EXIT=0, no 0xC0000005.
- 3490: USED - ResetVrf --dry-run #2, identical result. Two clean joins in a row =
  PREREG_0_4_SELFLAUNCH.md sec 4 prediction met; gate PASSED (that doc's sec 12).
- 3476: SKIPPED - never issued (numbering gap during the 2026-07-18 session). Do NOT
  recycle it; the never-reuse rule covers skipped numbers too.

- 3491/3492: USED - LaunchVrf.ps1 live regression after the 2026-07-18 adversarial
  sweep fixes. EXIT=0 READY, back-end 68 threads, RTI infrastructure correctly
  reported and preserved.

- 3493/3494: CLAIMED 2026-07-18 ~20:45 UTC (16:45 local) - LaunchVrf.ps1 bring-up for the
  PHASE 1 native-baseline session. Back-end 3493, front-end 3494. Ledgered BEFORE the
  launch per the precondition. Annotate with the actual result once the launch returns.

- 3455: USED 2026-07-18 22:21 UTC - oracle pre-check (WatchVrf 30 s, sample 2) after the
  3493/3494 launch. Joined and resigned cleanly, EXIT=0. reflected=3 readable=2, but BOTH
  readable objects were degenerate: one at 90.000000,-90.000000,0.0 (pole) and one with
  NaN lat/alt, stable across all 14 samples. SUPERVISOR NOTE / LEDGER DEVIATION: 3455 was
  RESERVED for Phase 1 whole-session telemetry and was consumed here on the pre-check
  instead - the pre-check is a SEPARATE join and needed its own number. The scored session
  telemetry now needs a NEW number; 3455 is burned and must NOT be recycled.
- 3495: CLAIMED - CreateOne, oracle position-fidelity discriminator (RUNBOOK 0.5.7
  "stronger check") to tell a blind oracle from the baseline objects' cast-corrupted readings (they are NOT positionless - see CORRECTIONS_LOG.md).
- 3496: CLAIMED - WatchVrf re-check to read the CreateOne entity's POS line.

- 3497/3498: CLAIMED - LaunchVrf.ps1 relaunch after the first unattended StopVrf.ps1
  teardown. Purpose: prove the launch/teardown round-trip AND that a relaunch clears the
  ORACLETEST throwaway entity from the live scenario.
- 3499: CLAIMED - WatchVrf, confirm uuid 95fcfa8a (ORACLETEST) is ABSENT after relaunch.

- 3499: USED - WatchVrf after the 3497/3498 relaunch. reflected=0 FOR THE FULL 20 s.
  At the time this was believed to be "the documented STOP condition"; IT IS NOT, and
  that phrasing is RETRACTED. Ran immediately after LaunchVrf reported READY, i.e. TOO
  EARLY - appNo 3500 saw the same federation fine at ~104 s. This run is the evidence
  that RETIRED the 20 s abort rule (RUNBOOK 0.5.7), not an instance of it.
- 3500: CLAIMED - WatchVrf re-check ~2 min after the same launch, to discriminate a
  SETTLING DELAY (READY precedes scenario load / federation join) from a genuinely
  blind federation. Single variable = elapsed time since launch; nothing else changed.

RESULTS BACK-FILLED 2026-07-18 evening for the CLAIMED entries above (the block's own
rule is "annotate with the actual result once the launch returns"; a sweep caught that
several were left CLAIMED with no outcome):
- 3493/3494: USED - LaunchVrf.ps1, EXIT=0 READY, back-end 64 threads. Clean.
- 3495: USED - CreateOne. Created uuid VRF_UUID:95fcfa8a-... name ORACLETEST,
  entityId 1:3493:5, EXIT=0.
- 3496: USED - WatchVrf. Read ORACLETEST back at 34.517156,-116.973525,1060.7 (requested
  lat/lon EXACT, 10000 m MSL ground-clamped). reflected=4 readable=3. This is the datum
  that killed the "blind oracle" hypothesis.
- 3497/3498: USED - LaunchVrf.ps1 after the first StopVrf teardown, EXIT=0 READY,
  back-end 70 threads.
- 3500: USED - WatchVrf ~104 s after that launch. reflected=3 readable=2, ORACLETEST
  ABSENT => the relaunch cleared the throwaway AND the oracle was not blind. Together
  with 3499 (reflected=0 at ~20-50 s) this is the settle-time evidence behind the
  corrected pre-check criterion in RUNBOOK 0.5.7.
- 3501/3502: USED - LaunchVrf.ps1 EXIT=0 READY, then StopVrf.ps1 EXIT=0.
  *** LEDGER VIOLATION, RECORDED DELIBERATELY: these two were NOT ledgered BEFORE the
  join. *** The supervisor folded the launch into a script-test command and advanced
  the marker only afterwards. No harm resulted (the numbers were genuinely free), but
  the rule exists because a stale/duplicate number causes a stale-federate join hang,
  and "it worked this time" is not a defence. Recorded so the lapse is visible rather
  than back-filled silently.

OBSERVED 2026-07-18: THE QUIT DIALOG'S COMPOSITION VARIES BETWEEN LAUNCHES. On the
3497/3498 teardown the dialog exposed a "Quit All Back-Ends" checkbox (ticked by
StopVrf, ToggleState=On). On the 3501/3502 teardown the SAME dialog title/class exposed
NO such checkbox, and StopVrf logged "checkbox not found". BOTH teardowns succeeded with
EXIT=0 and both processes down, because plain "Yes" in COMBINED mode closes the GUI and
the engine it started (vendor: Introduction\Starting\ExitingVR-Forces.htm). Cause of the
variation is NOT established - do not assume the checkbox will be present.

CLAIMED 2026-07-19 by the supervisor, BEFORE the join, for a single ResetVrf --dry-run
verifying the REFRESHED VrfBridge.dll (see VRF_GROUNDWORK_PLAN Status 2026-07-19: the
seven DLL copies were three different builds and tools/ResetVrf was running one that
PREDATED both the 2026-07-13 R10 fan-out and the 2026-07-17 0.6 console-capture work).
All seven copies are now byte-identical (sha256 A48ABE6C...). This dry-run is the gate
on that refresh: ResetVrf must join, discover, delete nothing, and resign cleanly.
- 3503: CONSUMED - ResetVrf --dry-run, refreshed-bridge verification. RESULT: PASS.
  EXIT=0 in 20 s. RTI 4.6.1 HLA 1516-2010 loaded, rid.mtl + vrfLegion.lua read,
  "Connected to RTI Assistant", joined (BackendCount=0), discovery ticked, resigned
  cleanly. NO 0xC0000005. Discovery returned 0 objects, which is CORRECT and not a
  fault: VR-Forces was deliberately NOT running for this check, so nothing reflects.
  WHAT THIS DOES AND DOES NOT PROVE - the refreshed native DLL LOADS, joins the RTI and
  resigns cleanly, which retires the crash risk that motivated verifying it. It does NOT
  re-verify DISCOVER-AND-DELETE against live objects; the 2026-07-11 live protocol
  (3271/3272/3273) did that on the older bridge. If ResetVrf is ever used in anger as
  the recovery lever, that discover-and-delete path is running on a bridge whose delete
  behaviour has not been re-exercised since the refresh.
  NOTE (unrelated to this gate, still outstanding): ResetVrf still prints "Verify the
  VR-Forces GUI now shows an empty scenario" - one of the GUI-referencing console
  strings HEADLESS_RUN_PLAN sec 4 lists for replacement. Not fixed here.


CLAIMED 2026-07-19 13:35 by scripts/RunC2SimScenario.ps1 (run 20260719T133529Z_run). Ledgered BEFORE any join,
per the never-reuse non-negotiable. Annotate with results from the run manifest.
*** OUTCOME: RUN FAILED AT STAGE 3. 3504/3505 CONSUMED, 3506-3509 BURNED UNUSED. ***
The runner DEADLOCKED and never reached stage 4. CAUSE (confirmed against Microsoft's
own Start-Process documentation, not inferred from the symptom): Invoke-External used
`Start-Process -Wait`, and -Wait is documented to wait for "the specified process AND
ITS DESCENDANTS" / "the PROCESS TREE ... and all its descendants". Stage 3 runs
LaunchVrf.ps1, whose PURPOSE is to leave VR-Forces RUNNING - so vrfGui and
vrfSimHLA1516e are descendants of that call and the wait could never return. LaunchVrf
itself SUCCEEDED (READY in 51 s, back-end 66 threads, front-end with a real window);
the runner then sat blocked for 47 minutes at 1.3 s CPU. Cf. Wait-Process, which is
documented to wait ONLY for the specified process - that asymmetry is the fix.
FALSIFIED HYPOTHESIS, recorded so it is not re-investigated: inherited stdout/stderr
handles were NOT the cause. The redirected log was verified openable with EXCLUSIVE
access while the runner was still blocked.
RECOVERY WAS CLEAN: the hung process was a SCRIPT that had joined no federation (no
trace file, no app log - both verified before acting), so stopping it did not violate
RUNBOOK sec 0. VR-Forces was then brought down by StopVrf.ps1 - EXIT=0 in 6 s,
graceful quit, nothing force-killed, all three RTI processes preserved.
- 3504: CONSUMED - LaunchVrf.ps1 back-end (vrfSimHLA1516e). Launched HEALTHY (66 thr).
- 3505: CONSUMED - LaunchVrf.ps1 front-end (vrfGui). Up with a real main window.
- 3506: BURNED UNUSED - never reached. WatchVrf advisory pre-check (RUNBOOK 0.5.7)
- 3507: BURNED UNUSED - never reached. WatchVrf MAIN trace / scoring input
- 3508: BURNED UNUSED - never reached. VrfC2SimApp Vrf__ApplicationNumber
- 3509: BURNED UNUSED - never reached. tools/CreateOne stage-7b failure-path diagnostic.
NOTE: numbers this runner allocates but does not consume (e.g. an abort before the
join) are BURNED, not recycled. The run manifest records which were actually used.


CLAIMED 2026-07-19 14:41 by scripts/RunC2SimScenario.ps1 (run 20260719T144109Z_run). Ledgered BEFORE any join,
per the never-reuse non-negotiable.
*** OUTCOME: THE CHAIN RAN END TO END, HEADLESS, ZERO HUMAN INTERACTION. Full write-up:
docs/experiments/RUN_2026-07-19_MOJAVE_CHAIN.md. Init accepted, 6 units created, all three
tasks issued (CreateRoute + MoveAlongRoute), oracle gate PASSED on 44 real-coordinate POS
lines, interface resigned cleanly (exit 0, no stale federate).
*** RETRACTED 2026-07-19 LATE - see docs/HANDOFF_2026-07-19.md sec 1. "No unit moved" is
FALSE for the third unit (VR-Forces' own reports show it moving east, still moving and not slowing - do not claim accelerating
when observation ended) and 577.8 m is the pre-correction leg figure - the FULL route is
~1155 m. Superseded text follows: ***
BUT NO UNIT MOVED: 114.MechCoy 0.0 m, 1.BdeHQ 0.0 m, 1222.MechPlt 63.4 m of a 577.8 m leg,
and that was oscillation in place (199.8 m of path for 63.4 m net), not progress. No
TASKCMPLT was emitted - an HONEST failure; the interface did not lie in either direction.
3515 (CreateOne stage-7b diagnostic) UNCONSUMED and BURNED - the oracle gate passed, so
the failure-path diagnostic correctly never fired.
TEARDOWN DEFECT: StopVrf exited 3 leaving vrfSimHLA1516e alive, still JOINED (established
connections to 6003/4001) and burning a full core. Resolved by hand with a graceful
CloseMainWindow - down in 5 s, no force-kill, all RTI preserved. ***
- 3510: CLAIMED - LaunchVrf.ps1 back-end (vrfSimHLA1516e), combined mode
- 3511: CLAIMED - LaunchVrf.ps1 front-end (vrfGui), combined mode
- 3512: CLAIMED - WatchVrf ADVISORY pre-init oracle pre-check (RUNBOOK 0.5.7)
- 3513: CLAIMED - WatchVrf MAIN run trace - the movement oracle / scoring input
- 3514: CLAIMED - VrfC2SimApp Vrf__ApplicationNumber (the interface federate)
- 3515: CLAIMED - tools/CreateOne - STAGE 7b FAILURE-PATH DIAGNOSTIC ONLY (RUNBOOK 0.5.7 STRONGER CHECK). CONSUMED ONLY IF THE ORACLE GATE FAILS; on a healthy run it is NEVER JOINED and this number goes UNCONSUMED. Unconsumed numbers are BURNED, never recycled - see the NOTE below. Allocated here rather than mid-run because every number must be ledgered BEFORE any join.
NOTE: numbers this runner allocates but does not consume (e.g. an abort before the
join) are BURNED, not recycled. The run manifest records which were actually used.


CLAIMED 2026-07-19 16:14 by scripts/RunC2SimScenario.ps1 (run 20260719T161438Z_run). Ledgered BEFORE any join,
per the never-reuse non-negotiable. Annotate with results from the run manifest.
- 3516: CLAIMED - LaunchVrf.ps1 back-end (vrfSimHLA1516e), combined mode
- 3517: CLAIMED - LaunchVrf.ps1 front-end (vrfGui), combined mode
- 3518: CLAIMED - WatchVrf ADVISORY pre-init oracle pre-check (RUNBOOK 0.5.7)
- 3519: CLAIMED - WatchVrf MAIN run trace - the movement oracle / scoring input
- 3520: CLAIMED - VrfC2SimApp Vrf__ApplicationNumber (the interface federate)
- 3521: CLAIMED - tools/CreateOne - STAGE 7b FAILURE-PATH DIAGNOSTIC ONLY (RUNBOOK 0.5.7 STRONGER CHECK). CONSUMED ONLY IF THE ORACLE GATE FAILS; on a healthy run it is NEVER JOINED and this number goes UNCONSUMED. Unconsumed numbers are BURNED, never recycled - see the NOTE below. Allocated here rather than mid-run because every number must be ledgered BEFORE any join.
NOTE: numbers this runner allocates but does not consume (e.g. an abort before the
join) are BURNED, not recycled. The run manifest records which were actually used.

CLAIMED 2026-07-19 by the supervisor, BEFORE the join, for ONE ResetVrf --dry-run
confirming the RTI Assistant still services joins after a "MAK RTI Error Notification"
modal (LRC #45, "Failed to open FDD file: RPR_FOM_v2.0_1516-2010.xml") was dismissed off
it. The assistant is long-lived (up since 2026-07-18 15:31) and killing one previously
cost an entire live window, so it was NOT killed - the dialog was dismissed via UIA and
this checks the assistant is still healthy BEFORE a 15-minute run depends on it.
- 3522: CONSUMED - ResetVrf --dry-run, post-dialog RTI Assistant health check. RESULT: PASS.
  EXIT=0 in 21 s (baseline 3503 was 20 s). "Connected to RTI Assistant", joined
  (BackendCount=0), discovery ticked, resigned cleanly. No 0xC0000005.
  TWO THINGS THIS SETTLES:
  (1) The assistant STILL SERVICES JOINS after the error dialog was dismissed off it. It
      did NOT need to be killed, and it was not. The FDD error did not damage it.
  (2) BONUS, and it retires a real risk: ResetVrf loads the NEWLY REBUILT VrfBridge.dll
      (sha 61FE865C, commit 7ec3b95). So the native rebuild joins the RTI and resigns
      cleanly. The "a rebuild might break the bridge" risk is retired for the join path;
      the raw-vs-DR READ path is still unexercised until a run with objects to read.


CLAIMED 2026-07-19 18:58 by scripts/RunC2SimScenario.ps1 (run 20260719T185814Z_run). Ledgered BEFORE any join,
*** OUTCOME: RUN FAILED. THE MOVEMENT ORACLE CRASHED. NO USABLE EVIDENCE. ***
WatchVrf died with 0xC0000005 (exit -1073741819) after emitting ONE POS line - BOTH the
pre-check and the main trace. Stack: vrf.VrfFacade.TryGetEntityMotion, the function added
in commit 7ec3b95. THIS WAS A SUPERVISOR-INTRODUCED REGRESSION, caught by the instrument
reproducibility check that was run BEFORE interpreting any result.
ROOT CAUSE (diagnosed, fix in progress): VrfFacade.cpp ~:734-735 ends its state-repository
resolution with a BLIND static_cast<DtReflectedEntity*>(obj). For a TropicTortoise BASELINE
CONTROL OBJECT - neither entity nor aggregate - that is undefined behaviour. Through the
bogus pointer, location() returns GARBAGE without faulting while lastSetLocation() faults.
The blind cast CANNOT simply be deleted: RUNBOOK sec 7 documents that
dynamic_cast<DtReflectedAggregate*> fails across the MAK DLL boundary, so disaggregated
aggregates resolve ONLY through it.
SECOND, INDEPENDENT DEFECT EXPOSED BY THE SAME RUN - and it is the more serious one:
THE ORACLE GATE DECLARED SUCCESS ON THE GARBAGE. It passed
"POS,3,VRF_UUID:cde66adc-...,0.000001,-90.000000,1020484223153767.2" - latitude 1e-6,
longitude -90, ALTITUDE 1.02e15 METRES - because it only ever checked lat/lon for NaN and
the pole and NEVER LOOKED AT THE ALTITUDE COLUMN IT WAS ALREADY BEING GIVEN. Same class of
failure as the retracted "reflected>0" criterion. The runner then reported "RUN COMPLETE -
evidence collected" and EXIT=0 on a run whose oracle had crashed 3 seconds in.
BOTH ARE FIXED: altitude sanity (>100 km = memory, not a position) plus an
equator/null-island placeholder check, and a crashed oracle now forces exit 3 and a FAIL
flag regardless of what every other stage returned.
- 3523/3524: CONSUMED - LaunchVrf back-end/front-end. Launch itself was fine (READY).
- 3525: CONSUMED - WatchVrf pre-check. CRASHED 0xC0000005.
- 3526: CONSUMED - WatchVrf main trace. CRASHED 0xC0000005. 1 POS line, 0 RAW lines.
- 3527: CONSUMED - VrfC2SimApp. Joined, created 6 units, resigned cleanly (exit 0).
- 3528: BURNED UNUSED - CreateOne stage-7b diagnostic; the gate falsely PASSED so it
  never fired. Had the gate been correct, this diagnostic would have run.
TEARDOWN WAS CLEAN: StopIface 0, app resigned 0, StopVrf 0, RTI preserved.
per the never-reuse non-negotiable. Annotate with results from the run manifest.
- 3523: CLAIMED - LaunchVrf.ps1 back-end (vrfSimHLA1516e), combined mode
- 3524: CLAIMED - LaunchVrf.ps1 front-end (vrfGui), combined mode
- 3525: CLAIMED - WatchVrf ADVISORY pre-init oracle pre-check (RUNBOOK 0.5.7)
- 3526: CLAIMED - WatchVrf MAIN run trace - the movement oracle / scoring input
- 3527: CLAIMED - VrfC2SimApp Vrf__ApplicationNumber (the interface federate)
- 3528: CLAIMED - tools/CreateOne - STAGE 7b FAILURE-PATH DIAGNOSTIC ONLY (RUNBOOK 0.5.7 STRONGER CHECK). CONSUMED ONLY IF THE ORACLE GATE FAILS; on a healthy run it is NEVER JOINED and this number goes UNCONSUMED. Unconsumed numbers are BURNED, never recycled - see the NOTE below. Allocated here rather than mid-run because every number must be ledgered BEFORE any join.
NOTE: numbers this runner allocates but does not consume (e.g. an abort before the
join) are BURNED, not recycled. The run manifest records which were actually used.


CLAIMED 2026-07-19 19:32 by scripts/RunC2SimScenario.ps1 (run 20260719T193252Z_run). Ledgered BEFORE any join,
per the never-reuse non-negotiable. Annotate with results from the run manifest.
- 3529: CLAIMED - LaunchVrf.ps1 back-end (vrfSimHLA1516e), combined mode
- 3530: CLAIMED - LaunchVrf.ps1 front-end (vrfGui), combined mode
- 3531: CLAIMED - WatchVrf ADVISORY pre-init oracle pre-check (RUNBOOK 0.5.7)
- 3532: CLAIMED - WatchVrf MAIN run trace - the movement oracle / scoring input
- 3533: CLAIMED - VrfC2SimApp Vrf__ApplicationNumber (the interface federate)
- 3534: CLAIMED - tools/CreateOne - STAGE 7b FAILURE-PATH DIAGNOSTIC ONLY (RUNBOOK 0.5.7 STRONGER CHECK). CONSUMED ONLY IF THE ORACLE GATE FAILS; on a healthy run it is NEVER JOINED and this number goes UNCONSUMED. Unconsumed numbers are BURNED, never recycled - see the NOTE below. Allocated here rather than mid-run because every number must be ledgered BEFORE any join.
NOTE: numbers this runner allocates but does not consume (e.g. an abort before the
join) are BURNED, not recycled. The run manifest records which were actually used.


CLAIMED 2026-07-19 20:23 by scripts/RunC2SimScenario.ps1 (run 20260719T202349Z_run). Ledgered BEFORE any join,
per the never-reuse non-negotiable. Annotate with results from the run manifest.
- 3535: CLAIMED - LaunchVrf.ps1 back-end (vrfSimHLA1516e), combined mode
- 3536: CLAIMED - LaunchVrf.ps1 front-end (vrfGui), combined mode
- 3537: CLAIMED - WatchVrf ADVISORY pre-init oracle pre-check (RUNBOOK 0.5.7)
- 3538: CLAIMED - WatchVrf MAIN run trace - the movement oracle / scoring input
- 3539: CLAIMED - VrfC2SimApp Vrf__ApplicationNumber (the interface federate)
- 3540: CLAIMED - tools/CreateOne - STAGE 7b FAILURE-PATH DIAGNOSTIC ONLY (RUNBOOK 0.5.7 STRONGER CHECK). CONSUMED ONLY IF THE ORACLE GATE FAILS; on a healthy run it is NEVER JOINED and this number goes UNCONSUMED. Unconsumed numbers are BURNED, never recycled - see the NOTE below. Allocated here rather than mid-run because every number must be ledgered BEFORE any join.
NOTE: numbers this runner allocates but does not consume (e.g. an abort before the
join) are BURNED, not recycled. The run manifest records which were actually used.


CLAIMED 2026-07-19 22:21 by scripts/RunC2SimScenario.ps1 (run 20260719T222134Z_run). Ledgered BEFORE any join,
per the never-reuse non-negotiable. Annotate with results from the run manifest.
- 3541: CLAIMED - LaunchVrf.ps1 back-end (vrfSimHLA1516e), combined mode
- 3542: CLAIMED - LaunchVrf.ps1 front-end (vrfGui), combined mode
- 3543: CLAIMED - WatchVrf ADVISORY pre-init oracle pre-check (RUNBOOK 0.5.7)
- 3544: CLAIMED - WatchVrf MAIN run trace - the movement oracle / scoring input
- 3545: CLAIMED - VrfC2SimApp Vrf__ApplicationNumber (the interface federate)
- 3546: CLAIMED - tools/CreateOne - STAGE 7b FAILURE-PATH DIAGNOSTIC ONLY (RUNBOOK 0.5.7 STRONGER CHECK). CONSUMED ONLY IF THE ORACLE GATE FAILS; on a healthy run it is NEVER JOINED and this number goes UNCONSUMED. Unconsumed numbers are BURNED, never recycled - see the NOTE below. Allocated here rather than mid-run because every number must be ledgered BEFORE any join.
NOTE: numbers this runner allocates but does not consume (e.g. an abort before the
join) are BURNED, not recycled. The run manifest records which were actually used.

CLAIMED 2026-07-21 (manual baseline run - stock MAK scenario HawaiiGround, supervisor).
Ledgered BEFORE any join, per the never-reuse non-negotiable. Purpose: run a MAK-authored
scenario headless (zero of our creation/tasking code) to establish a known-good movement
baseline and audit the accumulated hypotheses against reality.
- 3547: CLAIMED - LaunchVrf.ps1 back-end (vrfSimHLA1516e), combined mode, HawaiiGround
- 3548: CLAIMED - LaunchVrf.ps1 front-end (vrfGui), combined mode
- 3549: CLAIMED - WatchVrf trace (movement observer for the baseline)
- 3550: CLAIMED - tools/RunSim (start the sim clock on the loaded stock scenario)
NOTE: numbers allocated but not consumed are BURNED, not recycled.
- 3550: BURNED - RunSim first attempt failed to join (invoked with wrong cwd -> vrfLegion.lua
  / FDD not found -> CouldNotOpenFDD); it never drove the sim. Per never-reuse: burned.

CLAIMED 2026-07-21 (baseline run retry - RunSim with correct cwd=bin64, plus re-observe).
- 3551: CLAIMED - tools/RunSim retry (start the clock on the loaded HawaiiGround scenario)
- 3552: CLAIMED - WatchVrf trace (observe movement once the clock is running)

CLAIMED 2026-07-22 (supervisor - pre-registered fixture probe, SWEDEN GATE leg; prereg =
docs/experiments/PREREG_FIXTURE_REGION_VS_STRUCTURE_2026-07-22.md; user GO on record).
Ledgered BEFORE any join, per the never-reuse non-negotiable.
- 3553: CLAIMED - LaunchVrf.ps1 back-end (vrfSimHLA1516e), TankPltFixture_Sweden
- 3554: CLAIMED - LaunchVrf.ps1 front-end (vrfGui), combined mode
- 3555: CONSUMED - WatchVrf trace run 1 (360 s / 5 s). Captured a full 360 s PAUSED
  baseline only - RunSim had not yet started the clock (both attempts failed pre-join).
  All 5 real objects bit-static; RPT=0. Not a movement test.
- 3556: BURNED - RunSim usage error (federation string landed in the multiplier slot);
  exit 2 BEFORE any join. Burned per never-recycle discipline (not reused though no
  federate touched it).
- 3557: BURNED - RunSim invoked with wrong cwd (not bin64) -> CouldNotOpenFDD /
  vrfLegion.lua not found -> SEHException in bridge.Start(). Same failure as the 3550
  lesson. Never drove the sim; burned.
- 3558: CLAIMED - WatchVrf trace run 2 (the real static->run observation)
- 3559: CLAIMED - RunSim run 2 (cwd=bin64, multiplier 1 = real time)
NOTE: numbers allocated but not consumed are BURNED, not recycled. The Mojave leg
ledgers its own 4 only after the Sweden gate is analyzed.

CLAIMED 2026-07-22 (supervisor - fixture probe, MOJAVE leg; Sweden gate PASSED so Mojave
is interpretable; prereg = PREREG_FIXTURE_REGION_VS_STRUCTURE_2026-07-22.md). Ledgered
BEFORE any join. Same fixture as Sweden bar location (34.612956,-116.600487).
- 3560: CLAIMED - LaunchVrf.ps1 back-end (vrfSimHLA1516e), TankPltFixture_Mojave
- 3561: CLAIMED - LaunchVrf.ps1 front-end (vrfGui), combined mode
- 3562: CONSUMED - WatchVrf observer, Mojave attempt 1. BLIND (reflected=0 for 154 s+).
- 3563: CONSUMED - RunSim, Mojave attempt 1. Joined but discovered NO backend in 15 s
  (BackendCount=0); run() NOT sent. Federation-routing failure, not a fixture result.
- 3564: CONSUMED - WatchVrf 30 s discovery PROBE (fresh observer). ALSO BLIND
  (reflected=0). Confirms broken routing (not a stuck first observer). Backend WAS
  joined+publishing (vrfSim.log "Joined federation CWIX-2024"; established TCP to
  rtiexec:6003 + rtiForwarder:4001) but the PRESERVED rtiForwarder (1 thread, was 4
  during Sweden) did not relay objects to observer federates after the Sweden teardown.
- 3565: CLAIMED - LaunchVrf back-end, Mojave attempt 2 (clean relaunch recovery)
- 3566: CLAIMED - LaunchVrf front-end, Mojave attempt 2
- 3567: CONSUMED - WatchVrf oracle pre-check, Mojave attempt 2. BLIND (reflected=0,
  40 s). Clean relaunch did NOT clear the wedged forwarder.
CLAIMED 2026-07-22 - Mojave attempt 3, after a USER-APPROVED narrow RTI restart (kill
wedged rtiexec+rtiForwarder only, keep the answered rtiAssistant; non-negotiable
relaxed for THESE wedged processes by explicit user ruling this session).
- 3568: CLAIMED - LaunchVrf back-end, Mojave attempt 3 (post RTI restart)
- 3569: CLAIMED - LaunchVrf front-end, Mojave attempt 3
- 3570: CONSUMED - WatchVrf oracle pre-check, Mojave attempt 3. DISCOVERED reflected=9
  readable=8 - RTI restart FIXED the wedged forwarder. AR Plt 1 f0be86a8 + 4 M1A2 at
  Mojave 34.6128,-116.6005 alt 1041 m visible.
- 3571: CLAIMED - tools/RunSim (start clock, cwd=bin64, mult 1)
- 3572: CONSUMED - WatchVrf MAIN observation, Mojave attempt 3. MOVES (Branch B):
  static->moving, reflected 9->13, settled ~300 m E matching route. Region FALSIFIED.
CLAIMED 2026-07-22 - Branch-B confound control: below-terrain-waypoint Mojave variant
(TankPltFixture_Mojave_BelowTerrain, route at 100 m MSL ~941 m below terrain; single
variable vs Mojave = waypoint altitude). Prereg addendum sec 6a. May need the proven
narrow RTI restart after the teardown.
- 3573: CLAIMED - LaunchVrf back-end, below-terrain variant
- 3574: CLAIMED - LaunchVrf front-end, below-terrain variant
- 3575: CONSUMED - WatchVrf oracle pre-check, below-terrain. DISCOVERED reflected=9
  immediately (NO RTI restart needed - the wedge did NOT recur this teardown).
- 3576: CONSUMED - WatchVrf MAIN, below-terrain. MOVES: static->moving, reflected 9->13,
  settled ~300 m E (POS+RPT agree). Waypoint altitude FALSIFIED.
- 3577: CONSUMED - RunSim, below-terrain. EXIT=0, clock started.
CLAIMED 2026-07-22 - PLAN-ASSIGNMENT SPIKE, CELL C LIVE RUN (fresh-boot RTI ruling; the
3 idle RTI processes were stopped at run start per one-time user authorization). Pipeline
per docs/experiments/PREREG_PLAN_ASSIGNMENT_SPIKE.md. remote-create Tank Platoon (USA)
via CreateTaskAgg + bare CreateRoute/MoveAlongRoute = "R9's exact path with the CORRECT type".
- 3578: CONSUMED - LaunchVrf back-end, TropicTortoise base (Cell C). READY: healthy 66 threads.
- 3579: CONSUMED - LaunchVrf front-end, TropicTortoise base (Cell C). READY: vrfGui window up.
  (No RTI dialog appeared - persisted auto-connect survived the fresh assistant; no click needed.)
- 3580: CONSUMED - WatchVrf ORACLE PRE-CHECK (Cell C), TropicTortoise base. reflected=3
  (base globals), static, before create. Observers NOT blind - fresh RTI stack relays.
- 3581: CONSUMED - CreateTaskAgg CREATE (Cell C). EXIT=0. Aggregate CELLC_TANKPLT
  uuid=bc4187ab-6e80-ad41-bf1c-f965435c6994 entityId=1:3578:5 birth 34.612956,-116.600487 @10000 MSL.
- 3582: CONSUMED - WatchVrf MAIN observation (Cell C), one 300 s window. Create-verify reflected
  N=8 (3 base globals + aggregate bc4187ab + 4 M1A2 members) STATIC while paused (t=3..103);
  after task, static->moving at t~107, reflected 8->13 (4 offset-route transients + route),
  settled ~1165 m E at aggregate 34.612956,-116.587771 held stable t=118..299 (POS+RPT agree).
- 3583: CONSUMED - RunSim START CLOCK (Cell C), mult 1. EXIT=0, clock started 17:51:57.
  Platoon STATIC-but-clock-running confirmed (WatchVrf t=58-103 identical coords, RPT reports began).
- 3584: CONSUMED - CreateTaskAgg TASK (Cell C). EXIT=0. route CELLC_ROUTE
  uuid=f70908fa-6bdf-b14c-a061-b029b806ee88; MoveAlongRoute ISSUED (VOID) on bc4187ab.
NOTE: an operator applied ~15x sim-rate DURING the run (NOT via these tools; the interface ran
RunSim at mult 1). If that used a SetSimRate federate join, its appNo is UNLEDGERED here and the
supervisor must reconcile it; if it was a vrfGui/front-end rate change it consumed no appNo.
NOTE: numbers allocated but not consumed are BURNED, not recycled.


CLAIMED 2026-07-22 23:16 by scripts/RunC2SimScenario.ps1 (run 20260722T231614Z_run). Ledgered BEFORE any join,
per the never-reuse non-negotiable. Annotate with results from the run manifest.
- 3585: CONSUMED - LaunchVrf back-end (vrfSimHLA1516e), combined mode. Launched HEALTHY 19:16:24,
  then CRASHED mid-run (fatal-error dump 19:18:32; see TYPEFIX_CONFIRMING_2026-07-22).
- 3586: CONSUMED - LaunchVrf front-end (vrfGui pid 22512). Launched; SURVIVED the crash; StopVrf exit 3
  could not gracefully close it (real main window, not force-killed) - remnant at report time.
- 3587: CONSUMED - WatchVrf ADVISORY pre-init oracle pre-check. Ran (degenerate baseline, expected).
- 3588: CONSUMED - WatchVrf MAIN run trace. Ran from t=0; captured creation + crash; terminated
  when the runner process tree was stopped during the supervisor abort.
- 3589: CONSUMED - VrfC2SimApp (the interface federate). Joined, connected to C2SIM, dispatched 6 units,
  received order, created 3 routes. Terminated with the runner tree during abort WITHOUT a clean
  StopIface resign (back-end already dead) - possible stale federate in rtiexec until RTI timeout.
- 3590: BURNED - tools/CreateOne stage-7b FAILURE-PATH diagnostic. Oracle gate PASSED, so stage 7b
  never ran and this number was never joined. Allocated-not-consumed = BURNED, not recycled.
NOTE: numbers this runner allocates but does not consume (e.g. an abort before the
join) are BURNED, not recycled. The run manifest records which were actually used.


CLAIMED 2026-07-23 00:28 by scripts/RunC2SimScenario.ps1 (run 20260723T002834Z_run). Ledgered BEFORE any join,
per the never-reuse non-negotiable. Annotate with results from the run manifest.
- 3591: CLAIMED - LaunchVrf.ps1 back-end (vrfSimHLA1516e), combined mode
- 3592: CLAIMED - LaunchVrf.ps1 front-end (vrfGui), combined mode
- 3593: CLAIMED - WatchVrf ADVISORY pre-init oracle pre-check (RUNBOOK 0.5.7)
- 3594: CLAIMED - WatchVrf MAIN run trace - the movement oracle / scoring input
- 3595: CLAIMED - VrfC2SimApp Vrf__ApplicationNumber (the interface federate)
- 3596: CLAIMED - tools/CreateOne - STAGE 7b FAILURE-PATH DIAGNOSTIC ONLY (RUNBOOK 0.5.7 STRONGER CHECK). CONSUMED ONLY IF THE ORACLE GATE FAILS; on a healthy run it is NEVER JOINED and this number goes UNCONSUMED. Unconsumed numbers are BURNED, never recycled - see the NOTE below. Allocated here rather than mid-run because every number must be ledgered BEFORE any join.
NOTE: numbers this runner allocates but does not consume (e.g. an abort before the
join) are BURNED, not recycled. The run manifest records which were actually used.

CONSUMED 2026-07-23 (supervisor, manual single-tool ledger), BEFORE the join, per the
never-reuse non-negotiable. See docs/experiments/PREREG_RTIPROBE_WARM_2026-07-23.md.
- 3597: CONSUMED - tools/RtiProbe STANDALONE warm probe vs the resident RTI trio (rtiexec
  60672 / rtiForwarder 61696 / rtiAssistant 40956). First live exercise of the STEP 1 gate
  instrument + resident-stack serviceability check. ONE appNo covers all internal retries by
  design. RESULT: exit 0 (serviceable) on attempt 1, 13 s; created/joined CWIX-2024 and
  resigned cleanly; no stale federate. Outcome recorded in PREREG_RTIPROBE_WARM_2026-07-23.md
  sec 5.

*** NEXT FREE: 3598 *** (authoritative - the ONLY such marker in this file. Update this
line, and only this line, each time numbers are consumed.)
NOTE: the 2026-07-18 CONTROL launch ("Test A", bare vrfLauncher
--usePredefinedConnection with no --simArgs/--guiArgs) used the connection profile's OWN
3001 / 3101, not ledgered numbers - that is what a bare launch does and what every human
GUI launch has always done. Recorded so the trace is not mistaken for an unledgered join.
Session write-up: docs/experiments/SESSION_2026-07-18_SELFLAUNCH.md.

RESERVED 2026-07-18 for the Phase 1 native-baseline session (PHASE1_SESSION_SCRIPT.md).
Reserved ahead of the session per the script's precondition that the number is ledgered
BEFORE the join; annotate each with its actual result at session time. If the session does
not run, these stay burned - do NOT recycle them.
- 3455: *** BURNED - DO NOT USE. *** This entry formerly read "RESERVED - WatchVrf
  (POS+CON extended build, sampleSecs=2), Phase 1 whole-session telemetry". 3455 was
  CONSUMED 2026-07-18 evening by the oracle pre-check instead (see its USED entry
  earlier in this appendix). Phase 1 whole-session telemetry NEEDS A NEW NUMBER from
  the "*** NEXT FREE:" marker.
- 3456: RESERVED - SetSimRate 20x, Step 1b clock-persistence pre-check (throwaway mover).
- 3457: RESERVED - SetSimRate back to 1, Step 1b.
- 3458: RESERVED - SetSimRate 20x, Step 4 (the scored clock repeat).
- 3459: RESERVED - SetSimRate back to 1, Step 4 teardown.
NOTE: SetSimRate takes a SEPARATE appNo per invocation because each call is a full
join/resign cycle - four invocations, four numbers. If a ResetVrf pre-run sweep is added to
the session (prior sessions ran one; the Phase 1 preconditions do not currently call for
it), take the value from the "*** NEXT FREE:" marker line and advance it. (This line
previously said "take 3460" - 3460 IS ALREADY USED, see its entry above. Corrected
2026-07-18.)
- 3451: ResetVrf --dry-run, CPP-ALT-1 pre-run sweep on user-reloaded TT. Clean (2 baseline).
- 3452: PRISTINE C++ +altitude-probe app (branch b96688b), CPP-ALT-1 (COA-STP1, RUN C
  recipe, real-time). 6 marchers 18+ km on-terrain / 5 frozen at pile / runaway-warp class
  present - part 16 RESULT. Ended by kill (pristine cannot clean-stop) + rtiexec restart.
- 3453: WatchVrf 600s/15s CPP-ALT-1 window 1.
- 3454: WatchVrf 300s/15s CPP-ALT-1 end-state (delayed +20 min).
- 3448: ResetVrf --dry-run, COA-DEMO-1 pre-run sweep. Clean (2 baseline).
- 3449: port app, COA-DEMO-1 (COA-STP1 full, fix defaults, C2SIM, 20x, formation=auto,
  de-stack ON). 9 tasks dispatched, 1 completion, 38 movers, RUNAWAYS reproduced (541 km
  top) - part 15 RESULT. Clean-stopped.
- 3450: WatchVrf 600s/15s COA-DEMO-1 telemetry (the E4-falsifying capture).
- 3442: SKIPPED out of caution - part 13b's pre-registration text listed it for the P-C2
  watch, which actually ran as 3441; never confirmed unjoined, so retired unused.
- 3443: ResetVrf --dry-run, FIX-ACCEPT-1 pre-run sweep on the user-reloaded TT session
  (2026-07-16 evening). Clean: 2 baseline objects.
- 3444: port app, FIX-ACCEPT-1 (sec-3b fix at new defaults: Live + safe-MSL 10000 creates,
  parity SetAltitude skipped; STP, 20x, formation=auto, R9 lean init + R9 order). RESULT:
  prediction P1 - ALL THREE units moved with NO drag (investigation doc part 14).
- 3445: WatchVrf 90s/10s pre-order altitude check - entity clamped ON terrain (1131.4),
  members on terrain, aggregates degenerate 7-8 min post-create (resolved on tasking).
- 3446: WatchVrf 300s/10s FIX-ACCEPT-1 transit watch (the P1 evidence capture).
- 3447: WatchVrf 180s/15s end-state watch (company arrival / runaway check).
- 3435: ResetVrf --dry-run, fresh TT session (2026-07-16 ~13:00 launch). Clean, 2 baseline.
- 3436: port app, P-C1 run 1 (Live c=50, R9 lean, STP, 20x, formation=auto). 1222.MechPlt
  marched + completed (members telemetry-verified AT final waypoint); 114.MechCoy degenerate;
  1.BdeHQ frozen. Clean-stopped.
- 3437: WatchVrf 120s/15s for P-C1 run 1.
- 3438: ResetVrf REAL post run 1 - clean (Solution A held; 2 baseline deleted+respawned ok).
- 3439: port app, P-C1 run 2 (identical config). 1222.MechPlt FULL transit captured (arrived
  8 m from final wp); company degenerate; entity frozen. ALSO served P-C2 (supersession order)
  and the user's live P1a GUI probes. STILL RUNNING at ledger time (user experimenting).
- 3440: WatchVrf 180s/10s for P-C1 run 2 (the transit capture).
- 3441: WatchVrf 120s/10s for P-C2 (entity still frozen with first-leg-zero route).
- 3421: ResetVrf --dry-run, pre-run sweep on the user's fresh vrfLauncher TropicTortoise backend
  (2026-07-15 evening). Clean (2 baseline objects).
- 3422: port app, Fixed100 parity default + AggregateFormation=auto, R9 lean init, 20x - the
  MISSING CONTROL run. Result: universal freeze (investigation doc part 6/7).
- 3423: WatchVrf 150s/15s for the Fixed100 control - zero displacement, all 54 objects.
- 3424: SKIPPED - never joined (superseded by the live Freeze-Movement GUI check); do not reuse.
- 3425: WatchVrf 60s/10s after the user applied Freeze Movement=No to 1.BdeHQ live - no change.
- 3426: ResetVrf --dry-run post StopIface of 3422 - found 5 leftovers (3 orphans + 2 baseline).
- 3427: ResetVrf REAL - deleted the 5.
- 3428: port app, GroundWaypointAltitudeMode=Live clearance=50, same TT backend/data, 20x.
  Result: 1222.MechPlt FULL clean route completion (~1.16 km, stopped 8 m from final waypoint);
  other two units frozen with vacuous TASKCMPLTs (part 7).
- 3429: WatchVrf 120s/15s for the Live run (the completion telemetry).
- 3430: ResetVrf REAL post StopIface of 3428 - found 0 (already clean).
- 3431: ResetVrf --dry-run on the user's FRESH Bogaland2 backend (new vrfGui/vrfSim PIDs,
  2026-07-16). Clean (2 baseline objects).
- 3432: PRISTINE ORIGINAL C++ interface (master @ 191933a, rebuilt), FULL COA-STP1 128 units +
  42-task order, Bogaland2, 15x. 163/163 created; 3-of-9 first-wave movers (F1 overshoot
  reproduced in the ORIGINAL), 6 frozen at the mega-pile coordinate incl. a plain TANK entity
  (part 7). STILL JOINED at ledger time - clean-stop via StopIface when observation is done.
- 3433: WatchVrf 90s/15s for the pristine C++ run (first-wave telemetry).
- 3434: WatchVrf 600s/30s END-STATE capture of the pristine C++ run (launched 2026-07-16 late
  morning, background - final positions vs ordered destinations for the full task DAG).
- 3402: app join for the Tier-1 COA-STP1-Sweden-minimal reverse-transplant (ClientId=C2SIM,
  AggregateFormation=auto, TimeMultiplier=20, SubordinateFanOut off - genuine leader-path
  test), on the user-launched Bogaland2 backend. Init push itself required a real fix
  (RUNBOOK sec 0.6 - prolog-comment gotcha) before it would even reach the server. Both units
  created + formation-repaired cleanly, but the ORDER push then crashed the app's STOMP
  client (RUNBOOK sec 0.6 correction - block comments break order delivery too, a different
  mechanism than the init fix). App resigned cleanly on StopIface despite the dead STOMP
  thread; Solution A cleanup still ran (both units deleted, no orphans).
- 3403: consumed 2026-07-15, ResetVrf pre-retry dry-run sweep after the app crash/clean-stop.
  Confirmed clean (2 baseline env objects only, not our units - cleanup held).
- 3404: app join (retry, ClientId=C2SIM), same config as 3402. Both units created +
  formation-repaired; order pushed clean this time (comment stripped, RUNBOOK sec 0.6);
  CreateRoute + MoveAlongRoute dispatched for both AD/7152 and 3/7159.
- 3405: WatchVrf telemetry window (240s/15s samples) for the Tier-1 COA-STP1-Sweden-minimal
  probe - RESULT: neither unit marched. AD/7152 (platoon) reported TASKCMPLT but sat at a
  degenerate (0,0) position the ENTIRE window (vacuous completion, R11/F2-class); 3/7159
  (company) held a real position but drifted <1 m over 210 s (frozen, no TASKCMPLT). Full
  writeup: docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md.
- 3406: post-run ResetVrf dry-run sweep. Clean - Solution A cleanup removed both units, no
  orphans (2 baseline env objects only).
- 3407: pre-run ResetVrf dry-run sweep before the DIS-type variant probe (COA-STP1_Sweden_
  RealDIS_Initialization.xml - same 2 units/coords/order, real DIS type borrowed from the
  matching-echelon golden unit). Clean.
- 3408: app join for the DIS-type variant. Both units created + formation-repaired; order
  dispatched clean (CreateRoute + MoveAlongRoute for both).
- 3409: WatchVrf telemetry window for the DIS-type variant probe - RESULT: DIS-TYPE
  HYPOTHESIS FALSIFIED. Identical failure pattern to the zero-DIS run (AD/7152 vacuous
  TASKCMPLT at degenerate 0,0; 3/7159 frozen, sub-meter drift) even with real, valid DIS
  types borrowed from working golden units. See investigation doc for the writeup and the
  next candidate (force-side/hostility - the one remaining categorical difference).
- 3410: post-run ResetVrf dry-run sweep. Clean, no orphans.
- 3411: vrfSimHLA1516e launch (TropicTortoise, absolute FED path), for the altitude-clearance
  probe. Loaded clean, no popup, backend stable.
- 3412: ResetVrf pre-run dry-run sweep - CRASHED again (0xC0000005 in VrfFacade::Tick(),
  this time with a new clue: "Caught unknown exception in reflectAttributeValues" - crashing
  while processing an incoming attribute update, not during initial discovery). 2nd
  reproduction of this exact crash class at TropicTortoise specifically (0 reproductions at
  Sweden across many ResetVrf runs there). Backend itself SURVIVED this time (no new dump,
  stayed healthy) - contained to the ResetVrf client. Skipped the sweep (fresh scenario, no
  prior creates, no possible orphans) rather than retry into a 3rd crash. RUNBOOK sec 0.5
  updated with this new clue.
- 3413: app join attempt (GroundWaypointAltitudeMode=Live, clearance=0) against the same
  headless-launched TropicTortoise - CRASHED identically (0xC0000005 in Tick(), 3rd
  reproduction that session, this time killing the actual experiment app not just ResetVrf).
- 3414: ResetVrf dry-run against the USER's GUI-launched (combined front-end+back-end mode)
  TropicTortoise - CLEAN, 0 crashes, identical discovery to every Sweden run. ROOT CAUSE
  FOUND: the crash is specific to the sec-0.5 headless CLI launch recipe, NOT Mojave/
  TropicTortoise content (independently also ruled out: byte-identical .scnx to the repo
  snapshot and to Bogaland2's own page-in-area object, identical FOM/connection config for
  both scenarios). RUNBOOK sec 0.5 corrected - headless recipe marked unsafe, GUI launch
  required. Clears the way to actually run the altitude probe. User note: their launch uses
  `vrfLauncher.exe` (a combined front-end+back-end orchestrator via a predefined connection
  profile) - NOT a bare vrfSimHLA1516e.exe invocation; likely explains the gap, follow-up for
  a reliable self-launch recipe.
- 3415: app join (GroundWaypointAltitudeMode=Live, GroundWaypointLiveClearanceMeters=0)
  against the user's GUI-launched TropicTortoise backend. NO CRASH - all 6 units created +
  formation-repaired cleanly. First-ever clean Mojave app run this session.
- 3416: WatchVrf telemetry window for the Live/clearance=0 altitude probe (3 tasks dispatched:
  1222.MechPlt, 114.MechCoy, 1.BdeHQ - the exact R9 taskee set). RESULT: 1222.MechPlt/114.MechCoy
  degenerate (0,0) the whole window (unresolved position, same as every prior aggregate test);
  1.BdeHQ held a REAL position but ZERO net displacement the whole 153s window despite a
  confirmed CreateRoute+MoveAlongRoute dispatch and a confirmed-running sim clock (user verified
  the GUI clock advancing). This is new and unprecedented - 1.BdeHQ has moved successfully in
  EVERY prior Mojave test (R9, this doc's own history). See the investigation doc's final
  synthesis for the "movement execution may be globally suppressed under vrfLauncher" theory
  this motivated.
- 3417: post-run ResetVrf dry-run sweep + pre-run sweep for the clearance=50 retest. Clean, no
  orphans (Solution A held).
- 3418: app join (GroundWaypointAltitudeMode=Live, GroundWaypointLiveClearanceMeters=50), same
  backend, same units/order. No crash, clean creation + formation-repair.
- 3419: WatchVrf telemetry window for the Live/clearance=50 probe. RESULT: 1222.MechPlt now
  resolves to a REAL position (unlike clearance=0) and fired a genuine TASKCMPLT, but ZERO net
  displacement the whole 228s window (a vacuous completion with real coordinates instead of
  degenerate ones - a partial change, not a fix). 114.MechCoy also resolves to a real position,
  zero displacement, no completion. 1.BdeHQ AGAIN completely frozen, zero displacement, no
  completion - identical failure to the clearance=0 run. CONCLUSION: clearance value (0 vs 50)
  changed whether aggregate positions resolve to real coordinates, but did NOT produce actual
  movement for ANY of the 3 units in either test, and 1.BdeHQ's total failure in BOTH tests
  (never seen before, any region, any prior session) means this A/B is CONFOUNDED and
  INCONCLUSIVE - do not read either clearance value as validated or invalidated. The altitude
  hypothesis itself remains untested by a clean signal; something else broke movement EXECUTION
  in this environment. See investigation doc.
- 3420: post-run ResetVrf dry-run sweep. Clean, no orphans.

- 3388: consumed 2026-07-15, vrfSimHLA1516e itself (the sim backend, launched headless per
  RUNBOOK sec 0.5 - first time this project launched VR-Forces itself rather than a human via
  GUI). TropicTortoise (Mojave) scenario, execName CWIX-2024, RTI 4.6.1. Healthy: rtiexec up,
  terrain+scenario loaded clean. Backend join, not an interface/tool join - stays up across
  the following app/WatchVrf runs in this session (Tier-1 GroundWaypointAltitudeMode probe,
  docs/experiments/MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14.md).
- 3389: consumed 2026-07-15, ResetVrf pre-run dry-run sweep on the freshly-launched TropicTortoise
  backend. Clean: 0 deletable, 1 nil-uuid artifact skipped. Confirms no orphans before the
  Tier-1 GroundWaypointAltitudeMode=Live probe.
- 3390: consumed 2026-07-15, app attempt (GroundWaypointAltitudeMode=Live, lean golden-Mojave
  init) - "No backends found for object creation" (vrfSim's own federation join was stuck
  behind the LRC #8 FDD-path popup, see RUNBOOK sec 0.5 KNOWN ISSUE); clean-stopped via
  StopIface, no stale federate.
- 3391: consumed 2026-07-15, ResetVrf dry-run re-check after the app clean-stop; BackendCount=0
  confirmed (same underlying cause), 0 deletable, resigned cleanly.
- 3392-3397: consumed 2026-07-15, a bounded background poll (6x ResetVrf --dry-run, 30s apart)
  testing whether BackendCount just needed more time; all 6 came back empty/inconclusive
  (later understood: still the same stuck-federation-join state).
- 3398: NOT an interface join - vrfSimHLA1516e's own appNumber for the second launch attempt
  (absolute --fedFileName path fix). Loaded TropicTortoise cleanly this time (no popup), but
  the process itself crashed ~2 min later (RUNBOOK sec 0.5 KNOWN ISSUE; dump
  vrfSim5.0.2-MSVC++15.0_64-249613-36716.dmp).
- 3399: consumed 2026-07-15, ResetVrf dry-run against the freshly-loaded TropicTortoise (post-
  3398) - CRASHED (0xC0000005 access violation in VrfFacade::Tick(), during discovery, before
  any of our init/units were pushed). See RUNBOOK sec 0.5 KNOWN ISSUE.
- 3400: consumed 2026-07-15, ResetVrf dry-run retry immediately after the 3399 crash - succeeded
  cleanly (1 nil-uuid, 0 deletable, resigned clean). Not reproducible on demand.
- 3401: consumed 2026-07-15, ResetVrf pre-run dry-run sweep on the user-launched Bogaland2
  (Sweden) backend (vrfGui + vrfSimHLA1516e, human-launched after the TropicTortoise
  instability). Clean: 3 discovered (2 deletable baseline env objects - same UUIDs seen at
  Mojave, not orphans; 1 nil), 0 deletes issued (dry-run). No crash. Precedes the
  COA-STP1-Sweden-minimal reverse-transplant push.

- 3355-3359: consumed 2026-07-13 scale run (dry-run/sweep/watch/app/post-run).
- 3360-3362: consumed 2026-07-13 P4b live pass (dry-run/app/post-run dry-run). The 3363
  sweep was permission-denied and NOT consumed (2 Solution-A-race leftovers pending -
  the next pre-run sweep clears them).
- 3363-3367: consumed 2026-07-13 evening F3 PROBE (dry-run/sweep/WatchVrf/app/post-run).
  3363 dry-run + 3364 sweep cleared the 2 pending leftovers; 3367 post-run found+deleted 1
  race leftover (clean). F3 CONFIRMED (UNIT_MOVEMENT_RESEARCH sec 4c; evidence
  docs/experiments/F3_probe_2026-07-13.txt).
- 3368-3372: consumed 2026-07-14 SEMANTIC Units 2/5 Run 1 (dry-run/WatchVrf/app/sweep/confirm).
  3368 dry-run clean; 3371 sweep deleted 2 race leftovers; 3372 confirm dry-run clean. Units 2
  (Breach) + 5 (Reconnoiter, Escort) BEHAVIOR-VERIFIED at Sweden (SEMANTIC_MAPPING.md sec 7.3;
  evidence docs/experiments/semantic_units245_run1_2026-07-14.txt).
- 3373-3377: consumed 2026-07-14 SEMANTIC Unit 4 Run 2 (dry-run/sweep/WatchVrf/app/post-run).
  Unit 4 (MoveIntoFormation) BEHAVIOR-VERIFIED at Sweden - 14.MechBn moved 3990 m to dest (4 m),
  TASKCMPLT (premature ~40s). TASK (c) COMPLETE (Units 2/4/5). SEMANTIC_MAPPING.md sec 7.3;
  evidence docs/experiments/semantic_unit4_moveinformation_run2_2026-07-14.txt.
- 3378-3379: consumed 2026-07-14 TERRAIN page-in A/B (WatchVrf/app) on the TropicTortoise.scnx
  page-in scenario at the Mojave AO. Page-in area FALSIFIED as the aggregate fix (aggregates
  still froze, R9-identical). No ResetVrf (would delete the scenario's page-in area); clean stop.
  UNIT_MOVEMENT_RESEARCH.md sec 6; evidence docs/experiments/terrain_pagein_investigation_2026-07-14.txt.
- 3380-3385: consumed 2026-07-14 NAV-DATA test saga (3 runs): 3380/3381 no-op (Loaded-on-BE empty),
  3382/3383 lean-exe (broke tasking - "no current tasks"), 3384/3385 clean-exe (post-restart
  vacuous completions). Nav-data FIX still UNCONFIRMED - all 3 confounded. Setup solved (generated
  + loaded + active on BE via vrfSim restart). Evidence docs/experiments/navdata_test_saga_2026-07-14.txt;
  recipe SCENARIO_SETUP_GUIDE.md. Next free: 3386.

## Appendix C - build command reference

- Bridge (only if a step needs a facade/bridge change - none in this plan do): VS18 MSBuild via
  PowerShell, NOT git-bash:
  `& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" src\VrfBridge\VrfBridge.vcxproj /p:Configuration=Release /p:Platform=x64 /m`
- App / tools / SDK: `DOTNET_CLI_USE_MSBUILD_SERVER=false dotnet build <proj> -c Release --disable-build-servers`
- Offline selftest PATH (4.6b OK): `C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6b\bin`

## Appendix D - the R11 vacuous-completion rule (why telemetry is mandatory)

R11 (UNIT_MOVEMENT_RESEARCH.md sec 4c) proved DtPlanAndMoveToTask fires a TASKCMPLT while the
unit sits EXACTLY at its spawn point at a path-dead region. Completions LIE. Therefore: any
"the unit moved" claim in this plan's live steps MUST be backed by WatchVrf per-object
displacement, never a completion event alone. This is a hard gate at GATE-VERDICT, not advice.

## Appendix E - Confidence and unknowns (read before starting; watch hardest at the gates)

Honest calibration from the plan author. Ranked by how much each could bite. "Verified" =
checked first-hand this session (file:line or a cited live run); "Assumed" = not checked here.

TIER 1 - GENUINE UNKNOWNS (not resolvable on paper; only a live run or a run-time check settles them):
- P4b server acceptance. VERIFIED: the .NET ReportBody schema takes ReportContent[] (a bundle
  is a data change). ASSUMED: that THIS c2sim-server ingests N contents per envelope - the only
  evidence is that the C++ oracle sent bundles in a different era/client; the .NET port has never
  been seen to send one and get a 200. This is why P4b is held OUT of the first scale run
  (Step 3 ORDERING NOTE) and is a stop-and-escalate (3.9), not a claim. Secondary: an
  UNEXPLAINED ~2500-char truncation of some server BROADCASTS (UNIT_MOVEMENT_RESEARCH sec 4c
  op-finding #2) was on ORDER broadcasts, not report pushes - probably unrelated to bundling,
  but "unexplained" means not fully ruled out for a larger bundle payload.
- Step 5 completion COUNT is unpredictable. VERIFIED: the pipeline scales (128 units / 42 tasks /
  0 abandons previously) and fan-out marches platoons + companies at Mojave (R10, telemetry).
  NOT DONE: tracing the 42-task temporal-dependency graph. Under the skip policy, a skipped
  predecessor skips its successors; 11 performers are each tasked up to 4x with each retask
  cancelling the prior fan-out. So the 7-task probe's 5/7 is NOT a comparable baseline, and no
  hard completion number can be promised. Step 5 acceptance therefore leans on PROCESS
  correctness (de-stack fires, fan-out dispatches, telemetry shows displacement, zero 10048,
  clean stop). Treat the first full run as partly a CHARACTERIZATION run; a surprising count is
  data to explain, not automatically a failure.

TIER 2 - EXECUTION-FRAGILE (design is sound; the implementation is where a bug slips in):
- Step 2 FanOutTracker rewrite - the most review-fragile code here. TryCompleteMember's new
  `alreadySynthesized` out-param touches the caller AND all existing --fanout-selftest checks
  (regression surface). The "quorum met, Pending non-empty -> keep a Synthesized record to
  swallow stragglers" lifecycle must clean up _unitByMember on BOTH the late-completion path and
  supersession (leak/mis-route risk). Idempotency rests on TWO guards agreeing (the Synthesized
  flag AND _inFlight.TryComplete removing the record) - implement one only and you get a double
  TASKCMPLT or swallow-everything. Watch this hardest at GATE-DIFF; the specified offline cases
  only help if written faithfully.
- P4a is PROBABLY the only exhaustion source - "probably". VERIFIED: the fix and both HttpClient
  sites; a grep found only those two HTTP sites; the error text matches SendTrans's catch. NOT
  AUDITED: the STOMP raw-socket reconnect path for a second, smaller leak. Proof is "zero 10048"
  live; if it is not zero after P4a is in the running build, that is the signal of a second
  source (-> stop-and-escalate, Step 5.6), not something to assume away.

TIER 3 - ASSUMPTIONS TO CONFIRM AT STEP START (deferred to run-time by necessity):
- "Eight selftests are green right now" is INHERITED from the last session's record, not checked
  this session (planning session; the exe was not run). The 0.2 before/after gate converts this
  into a checked precondition - expect to CONFIRM the baseline, do not trust it.
- Environment drift: VR-Forces up, container reachable, loopback fast, license valid (expires
  2026-09-15), vrfGui hung-but-backend-healthy - all ASSUMED. Real elapsed time since 2026-07-13
  is unknown, so drift is likely. Appendix A preflight forces the re-check.

LEAN-TO-CUT: Step 2.4 (fan out the single-point MoveToLocation path). Optional and low-payoff -
real orders are overwhelmingly multi-point routes. Recommend SKIP in the first pass; add only if
a specific order surfaces the need.

HIGH CONFIDENCE (not hedged): the P4a core fix, Step 6 csproj paths (mechanical, anchored to the
app's known-working relative form), and the Step 4 coa-gpt memo (pure writing from verified
evidence). The R11 telemetry rule and the non-negotiables are copied verbatim from verified docs.
