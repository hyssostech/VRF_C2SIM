# START HERE - resuming the C2SIM VR-Forces -> .NET port

If you are picking this up in a fresh session with no prior context, read
this first. It gives you everything to continue with zero loss. ASCII-only.

## What this is

Porting the GMU `c2simVRFinterface` (C++, wraps MAK VR-Forces via
`DtVrfRemoteController`) to .NET on top of the HyssosTech C2SIM .NET SDK.
Two repos:
- THIS repo `c2simVRFinterfacev2.36` - the C++ interface being ported.
- The SDK `OpenC2SIM.github.io` at `Software/Library/CS/C2SIMSDK`, work on
  branch `dev/sdk-fixes` (NOT merged, NOT pushed).

## Read in this order

1. `docs/PORT.md` - the master reference. Every settled decision WITH its
   evidence (feasibility, architecture, toolchain, environment, golden-trace
   baseline, interface bugs, SDK changes, decisions log). This is the source
   of truth; trust it over any summarized recollection.
2. `docs/PHASE1_REWIRE.md` - the current actionable task: wiring the interface
   onto `VrfFacade`, with the full call-site catalogue and the verify procedure.
3. This file for repo state, build/run commands, and where artifacts live.

## Current status

Phase 1 rewire: DONE and verified - the interface runs 100% through VrfFacade and
reproduced golden-trace-02 (PORT.md sec 8). Phases 0 and 2-risk are done.

Session 2026-07-09 (post-rewire) outcomes - READ PORT.md sec 10 + docs/RUNBOOK.md:
- COA-STP1 adversarial test: rewire is CLEAN. The aggregate "freeze" is VR-Forces'
  disaggregated-set-maneuver behaviour (invalid default formation), not a port bug
  (PORT.md sec 5 + 10).
- Bare-support finding + two-layer target architecture: the C++ interface uses ~4 of
  263 `vrftasks` and ignores C2SIM `TaskActionCode` (every maneuver -> moveAlongRoute).
  The port should map C2SIM semantics -> real vrftasks. PORT.md sec 10.
- Aggregate-MOVEMENT FIX (validated vs MAK API/docs, prototyped as an uncommitted C++
  spike, clean build): `controller->setAggregateFormation(uuid,"Wedge")` before
  `moveAlongRoute`. Its real home is the .NET port, not the deprecated C++. PORT.md sec 10.
- VRF_C2SIM (.NET port home): created PUBLIC at github.com/hyssostech/VRF_C2SIM and wired
  as a submodule under the fork's `Software/Interfaces/VRF_C2SIM` (committed LOCALLY, not
  pushed - commit 6185848 on `dev/sdk-fixes`).
- docs/RUNBOOK.md: the operational runtime procedure (launch cmd, push-init-then-run,
  CLEAN STOP via UNINITIALIZED, block-buffered-stdout + fresh-appNumber gotchas). READ IT.

OPEN / not landed: the live movement proof. The Dockered C2SIM loopback proxy went slow
(self-inflicted by over-restarting Docker this session), which stalls the interface's STOMP
connect (0 units). The METHOD is sound - it ran many times this session. RUNBOOK sec 6 has the
fix: if a raw TCP connect to 127.0.0.1:61613 isn't near-instant, restart Docker Desktop, then
run RUNBOOK sec 3 (push init -> start interface -> push order).

## Repo state

THIS repo is git-tracked (it was not before - `git init` this effort).
`git log --oneline` should show, oldest to newest:
```
191933a Baseline: working c2simVRF interface (golden-trace instrumented)
2d0b1c1 Phase 1: add VrfFacade.h - the pure-native VRF boundary contract
01431ea Phase 1: implement VrfFacade.cpp (compiles clean, DIS + HLA, unwired)
7806ffd Phase 1: add VrfFacade.{h,cpp} to HLA vcxproj (compiles, unwired)
06e4278 Phase 1: capture decisions + rewire plan in docs/
b7df10e Phase 1: preserve session artifacts in-repo + add START_HERE
```
(There may be later commits from the rewire - `git log` is authoritative.)
Every commit builds. `bin64/` and `build64/` are gitignored (rebuild them).
The SDK repo is on branch `dev/sdk-fixes`, commits `f738edf` (static-state
fixes + tests) and `3b7cd33` (net10). Not merged/pushed - do that when ready.

## Where everything lives (all in THIS repo now - nothing depends on the volatile session scratchpad)

- `VrfFacade.h` / `VrfFacade.cpp` - the facade (contract + impl). In the HLA vcxproj.
- `docs/golden-trace/` - the PARITY ORACLE. The ported code must reproduce these:
  - `golden-trace-02_init-and-tasking.log` - late-join init (49 creates + 4 areas)
    and full tasking (order -> route -> move -> TASKCMPLT).
  - `golden-trace-report_generation.log` - reportGenerator poll path.
  - `reports-captured_wire-xml.log` - the actual PositionReport XML on the wire.
  - `c2sim-bus_order-echo.log` - order the server echoed on STOMP.
  - `STP-TC-small-6-12-24_Initialization.xml` - the init to push (80 units, STP).
  - `orders/1_VRF_Move_Order.xml` - the tasking order to push for the task trace.
  - `orders/{A..G}*.xml` - the experiment-matrix orders (the destroyed-unit probe).
- `bridge-spikes/` - the two proven C++/CLI spikes + the native isolation probe.
  `VrfBridgeSpike` (matrix), `VrfControlSpike` (boost-heavy vrfcontrol), `NativeProbe`.
  These are the PROOF that in-process C++/CLI works AND the working vcxproj templates
  for the real bridge. Build with VS2022+ MSBuild (net10, v143). See PORT.md sec 2-3.
- `tools/` - .NET helpers using the SDK: `PushInit` (reset+share an init),
  `PushOrder` (push an order, log STOMP echo), `ListenReports` (capture reports),
  `SdkVerify` (the offline SDK checks). They reference the SDK csproj by absolute
  path and target net6 - bump to net10 if rebuilding (the SDK is net10 now).

## Build

Interface (HLA1516e, the target protocol) with VS2019 BuildTools v142:
```
"C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" ^
    c2simVRFHLA1516e.sln /p:Configuration=Release /p:Platform=x64 /m
```
Standalone compile-check of just the facade (fast, no link) - see PORT.md sec 3
for the exact `cl /c` invocation with the MAK include/define set (both DIS and
HLA variants were verified this way).

## Run / verify (reproduce the golden trace)

READ `docs/RUNBOOK.md` FIRST for the operational procedure - launch command + arg map,
the push-init-then-run order, the CLEAN STOP (drive the C2SIM server to UNINITIALIZED so
the interface resigns - do NOT force-kill, which strands an RTI federate and forces a
VR-Forces reload), and the gotchas (block-buffered stdout, fresh appNumber per run). Those
cost a whole session to rediscover once; do not repeat it.

Full procedure in PHASE1_REWIRE.md "Verification". Prerequisites:
- MAK license valid (expires 15-sep-2026); `MAKLMGRD_LICENSE_FILE` points at it.
- Env for the exe: `QT_QPA_PLATFORM_PLUGIN_PATH=C:\MAK\vrforces5.0.2\bin64\platforms`,
  and `C:\MAK\vrforces5.0.2\bin64;C:\MAK\vrlink5.8\bin64;C:\MAK\makRti4.6.1\bin` on PATH.
- VR-Forces running HLA1516e, federation CWIX-2024, session 1.
- C2SIM container `c2sim-server` up (8080 REST, 61613 STOMP). Stop any process on
  IPv4 8080 first (a COA-GPT tileserver shadows it).
- Push the STP init with `tools/PushInit`, then run the interface:
  `bin64\c2simVRFHLA1516e.exe 127.0.0.1 8080 61613 STP 0 0 3 127.0.0.1 0 0 0 1 3201 1 3 0 0 CWIX-2024 0`
  (debug=0; debug=1 is broken - see PORT.md sec 6). Push an order with `tools/PushOrder`.
- Diff the new run against `docs/golden-trace/*`.

## The immediate next task

Do the rewire per `docs/PHASE1_REWIRE.md`: add the `StartAdopting` transition
scaffold, move the ~40 command call sites onto the facade in green-building
batches, relocate the three callbacks as the atomic final step, then verify
against the golden trace. Parity rule: reproduce behavior, do NOT fix the
interface bugs (those get fixed in the .NET port, Phase 4).

Keep `docs/PORT.md` (decisions/status) current AS you work, and after any
context compaction re-read it before deciding anything.
