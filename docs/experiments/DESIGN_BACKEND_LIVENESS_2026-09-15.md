# DESIGN - BACK-END LIVENESS ON A TIMER, AND THE NAV-AREA PRECONDITION (STP-822)

2026-09-15. Branch `feat/backend-liveness`. The defect: `docs/experiments/V6_LIVE_JOIN_GATE_2026-09-15.md` secs 9.5, 10.3 and the VERDICT.

## 1. THE DEFECT

On run `20260915T130627Z` (V6d) the VR-Forces back end STOPPED at the first ground `move-along`
on a fixture with no navigation area: no frames, no status messages, no motion, no terminal
reports. The interface logged `Backend discovered (BackendCount=1)` once at start-up and then
delivered **543 position reports, 0 failed, 0 warnings, 0 TASKABRT** off reflected attributes the
RTI still held. The same reading held on the healthy A4 run (8,169 reports): a product defect,
not a fixture property.

The mechanism is one line: `VrfC2SimService` re-read the back end ONLY inside the task clock's
stale branch (`taskSimStale = heldOnSim && obs.Stale`), which needs the sim clock
readable-confirmed AND flat. The R5 order carries no Duration, nothing was ever held, the branch
never ran, and **the reading was never taken again** - while `WatchVrf --report-backends`, a
different federate, watched `backends=` drop 1 -> 0 exactly 121 +/- 2 s after dispatch, twice.

## 2. WHAT WAS BUILT

### 2.1 `Vrf:BackendLivenessSeconds` (default 10; 0 = off) + `Vrf:BackendLossConfirmSeconds` (30)

A tick-loop phase, `MaybeCheckBackendLiveness`, registered BEFORE `MaybeSendPositionReports` so a
loss confirmed on a tick suppresses that tick's reports. It reads `ActiveBackendCount()`
(STP-809), `BackendCount()` and `BackendControlState()`, each guarded on its own.
`BackendLivenessPolicy.Classify` prefers the active count, falls back to `BackendCount` (the
signal measured to drop on this failure), and lets the control state decide only between "gone"
and "nothing legible": `BackendControlNoBackend` is a loss, a `Paused` reading never is (Q5 - a
paused back end is alive), and an all-unreadable sample is neither a miss nor a good reading.

`BackendLivenessMonitor` is the state machine. A LOSS needs **both** `ConfirmSamples` = 2
consecutive zero readings **and** `BackendLossConfirmSeconds` of them, and it is never declared
before a back end has been seen at least once (the start-up settle already says
`NO BACKEND DISCOVERED`). RECOVERY needs 2 consecutive good samples. Only STATE CHANGES are
logged or acted on.

**On LOSS** (one `BACK END LOST:` ERROR line, then):
1. one TASKABRT per in-flight task through the B1 emit point (`PushTaskStatus`), reason
   `VR-Forces back end lost (no status for N s)`, plus `_sequencer.NotifyAbandoned` so STREND
   successors fail fast - the Q1 rule that a gate must not contradict the report just sent;
2. ONE C2SIM ObservationReport (NameObservation, the `PreflightReports` shape) naming the loss,
   the last good stamp and how many tasks were running;
3. R1 position reports SUPPRESSED - counted, said once - until recovery; nothing is faked or
   zeroed, the fixes simply do not go out;
4. C16 stands down (`MaybeCheckStalls` returns, saying so once) and its sample rings are DROPPED,
   so nothing is ever measured across the outage - a back-end loss is not a unit stall;
5. the task clock's stale branch short-circuits to (count 0, NoBackend, active 0), which is
   `TaskClockAction` rule 1 -> WALL, so it can never print "the back end REPORTS PAUSED" about a
   back end this feature has declared lost.

**On RECOVERY**: one line, one ObservationReport, reports resume, C16 judges again. **Tasks are NOT restarted** - re-tasking would be the interface inventing an order.

No native change: `DtBackend::lastResponseTime()` (`vrfutil/backend.h:198-199`) is the exact
status age and the cleanest signal, but `VrfFacade` exposes no getter and adding one triggers the
G-A eleven-consumer redeploy (RUNBOOK sec 9). The counts already move, so it is not needed.

### 2.2 `Vrf:RequireNavAreaForGroundTasks` (default FALSE) + `Vrf:NavAreaEvidenceSeconds` (300)

At the dispatch of a ground move (after the route is final, beside the pre-flight hook) the gate
asks `NavAreaEvidence` whether a navigation area is loaded, and refuses the task with TASKABRT
`no navigation area evidence for <taskee>` + an ObservationReport when it COULD have seen the
evidence and none arrived.

**The evidence, found without the object console being a new channel:** the interface ALREADY
subscribes to the VR-Forces object console (`addObjectConsoleMessageCallback`,
`vrfRemoteController.h:1970`) and opens each created object's console at
`Vrf:ObjectConsoleNotifyLevel`. Two rows bear on navigation:
* POSITIVE - `New Primary nav area: | <area>`, printed at level >= 3. This is the same row the
  RUNNER's stage-7d READY gate greps out of `vrfc2simapp.log`
  (`scripts/RunnerLib.ps1 Get-NavAreaRows`), so the app is reading its own log's source in band.
* NEGATIVE - `Is current point in nav area?` / `fail in action ...`, level 4. It arrives DURING
  execution, so it can never gate the dispatch that produced it; it is recorded and named in the
  refusal text of the next one.

**What does NOT exist:** no nav-area query a remote controller can issue.
`vrfcontrol/vrfRemoteController.h` has no navigation accessor at all (its only "nav" matches are
IFF/ATC navaid parameters); `navigationAreasManager.h` lives in `vrfGuiCore`, the front-end GUI
library this process does not link; `vrfNavigation/*` is back-end/generator side; and the
terrain-profile reply carries elevations, not navigability.

## 3. THE LIMITATION OF (2), STATED PLAINLY

1. A `New Primary nav area` row proves an area was acquired BY THAT OBJECT, and it has never
   been established that every platform prints one (the runner's own gate accepts any object's
   row). So this gate refuses only when NO object has printed one at all - the V6d case -
   which is conservative against refusing on a fixture that does have nav data. **It does not
   prove the taskee's start point is inside an area**; the log line and the report say which.
2. Below object-console level 3 the gate can see nothing. There it CANNOT distinguish "no nav
   area" from "not watching", so it logs ONE warning naming what to set and DISPATCHES - refusing
   on ignorance would be worse than the risk it guards.
3. It is an evidence check, not a geometry check: "is this point on the walkable mesh?" belongs
   to scenario prep (STP-802/803/804), which can answer it offline.

## 4. THE DEFAULTS, AND WHOSE THEY ARE

| key | default | who decides |
|---|---|---|
| `Vrf:BackendLivenessSeconds` | **10** | shipped on; 0 restores the pre-STP-822 blindness |
| `Vrf:BackendLossConfirmSeconds` | **30** | shipped; a loss is reported ~30-40 s after the drop |
| `Vrf:RequireNavAreaForGroundTasks` | **FALSE** | **THE USER'S RULING** - the gate refuses tasks, so it ships off |
| `Vrf:NavAreaEvidenceSeconds` | **300** | covers the measured 236.9 s cold-cache acquisition |

All four are in `appsettings.json` at these values, overridable per process the usual way (`$env:Vrf__BackendLivenessSeconds = "0"`).

## 5. TEST

`VrfC2SimApp --liveness-selftest` drives a tick fixture through BackendCount 1 -> 0 -> 1 over
400 s and asserts the whole sequence; `--liveness-selftest --disabled` runs the SAME assertions
with the feature off - the V6d build - and **fails 11 of them**: that arm is the defect, in one
command. Offline - no bridge, no server, no network.

## 6. WHAT A LIVE CONFIRMATION MUST SHOW

The V6d recipe unchanged - plain `R9_Mojave_Empty_52` fixture, R9 lean init, the R5 order (no
Durations), object console 4 - with this build: **3 TASKABRT within ~40 s of the `backends=` drop,
exactly ONE loss ObservationReport, and 0 position reports after it** (V6d: 0 / 0 / 543). The
`--report-backends` column still drops at 121 s; what changes is that the interface says so. With
`Vrf:RequireNavAreaForGroundTasks=true` the same run should instead refuse all three tasks at
dispatch and never stop the back end at all.
