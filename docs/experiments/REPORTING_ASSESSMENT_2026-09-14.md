# Reporting lane assessment (Opus analysis executor, 2026-09-14 ~12:30Z). Supervisor notes: the user ruled
# TASKABRT for stalled units ('Ok on 2-3'), so G6/B6's code question is decided; B5/B3/B1/B2/B8 dispatched the
# same turn on feat/reporting (off the watchdog branch); B10 drafted as docs/DRAFT_STP_QUESTIONS_2026-09-14.md
# (not sent); B4 (pre-flight port) and B7 (heading/speed) queued behind them; B9 (bundle) = one run.
# UPDATE 2026-09-14 ~13:00Z: B5/B3/B1/B2/B8 are BUILT, cold-start reviewed and the review's findings
# applied on feat/reporting - see the STATUS section below for the shas, the four live gates and the
# two items still owed. Sec 6 marks each item; the rest of this record is UNCHANGED and still
# describes the shipped build as it was measured in G6.

# REPORTING ASSESSMENT - what STP and the demo audience see (2026-09-14)

## STATUS 2026-09-14 (branch `feat/reporting`) - B1/B2/B3/B5/B8 are BUILT, LIVE CONFIRMATION OWED

B5, B3, B1, B2 and B8 are all implemented and committed on `feat/reporting`, cold-start reviewed
(verdict MERGE WITH FIXES, 2 MAJOR latent + 11 minor/notes) and the review's findings applied. Build
commits: `9286398` (B5), `ebf8da9` + `f0d1c68` (B3), `81d108c` (B1), `f057fc2` (B2), `4e9a9ce` (B8).
Review-fix commits: `3242337` (NameRegistry - findings 1/2/3), `a83c79f` (the policies - findings
4/5/6/8/10), `319a098` (--report-selftest proofs), `b965dd8` (the service call sites - findings
1/4/6/7/8/12). Offline self-tests
(`--report-selftest`, `--name-selftest`, `--parse-selftest`, plus the untouched
`--stall/--arrival/--typemap` suites) are the only evidence so far.

NOTHING HERE IS CONFIRMED LIVE. The four gates, all owed to one COA-STP1 run:

1. the R1 line reads **128 sent, 0 skipped** (B3 closes G1);
2. **14 TASKSTRT** on the bus within seconds of the order (B1 closes G4);
3. **zero "Failed to deserialize" lines** in the run log - there are exactly 2 in every run today (B5);
4. **more than 102 NameObservations** (B8 closes G10).

ONE ITEM REMAINS OWED IRRESPECTIVE OF THAT RUN (the first of the two was landed on
`feat/integration`, 2026-09-14):

- ~~**Native `success()` forwarding.**~~ **DONE on `feat/integration`.** `feat/heading-speed`
  added the native half (`VrfFacade` reads `DtTaskCompleteReport::success()`,
  `taskCompleteReport.h:84-90`, into `TaskCompleted::success`; `VrfBridge` raises it as
  `TaskCompletedEventArgs.Success`) and the integration branch wired it: `OnVrfTaskCompleted`
  now reads `bool success = e.Success` instead of the literal, so a vendor-reported FAILURE
  becomes TASKABRT, releases no successor and cancels a parked engage. `--report-selftest`
  walks both flag values to the deserialized wire xml. What the offline test CANNOT see is
  that the call site reads `e.Success` (loading the mixed-mode bridge needs the MAK runtime),
  so G2 closes on the live gate: one run where a vendor `Failed` produces a TASKABRT.
- **STP's behaviour on a DUPLICATE ReportID.** B2's retry is at-least-once: it re-sends the same xml,
  same ReportID, and the SDK POSTs before it parses the answer, so a TaskStatus can reach the bus
  twice under one id. Added as **question 6** of B10 (`docs/DRAFT_STP_QUESTIONS_2026-09-14.md`). It
  also breaks the "0 duplicate ReportID" property sec 7 records for the G6 capture.

BEHAVIOUR CHANGE TO STATE (review finding 4): an **advance-then-engage** task (ATTACK/BREACH with a
resolved target) is one C2SIM task executed as two VR-Forces tasks. The MOVE half's completion now
reports **TASKINPRG**, and the task's single **TASKCMPLT** is kept for the engage's completion.
Before `feat/reporting` the move's completion sent TASKCMPLT and the engage's sent a second one with
the same `CurrentTask`; with B1's once-per-task rule and no fix, the engage's report would have been
suppressed entirely and STP would have been told the attack finished the moment the unit arrived at
its firing position. STP now sees TASKSTRT -> TASKINPRG -> TASKCMPLT for these tasks.

Lane L2 of `docs/PLAN_PARALLEL_LANES_2026-09-14.md`. READ-ONLY; every figure comes from the repo's code, its docs, or the run captures named. Tier: STANDARD. The two cause claims (sec 3, sec 5 G1) each carry a falsifier and were checked.

Sources: `src/VrfC2SimApp/{ReportBuilder,ReportSelfTest,VrfC2SimService,VrfSettings}.cs` and `appsettings.Demo.json`; `Software/Library/CS/C2SIMSDK/C2SIMSDK/{C2SIMSSDK.cs,C2SIM_SMX_LOX_CWIX2024.cs,C2SIMServerResponse.cs}`; `tools/ListenReports/Program.cs`; `tools/preflight/leg_check.py`; `docs/DEMO_READINESS_2026-09-06.md` rows 3/4/13/15/19/20; `docs/DESIGN_ORBAT_TO_VRF_2026-09-06.md` C15; `docs/HANDOFF_2026-09-01_R9_COMPLETE.md`; `docs/PORT.md` sec 6; `docs/DEMO_RUNBOOK.md`; `docs/experiments/PREREG_R1_POSITION_REPORTS_52_2026-09-06.md`; `docs/golden-trace/reports-captured_wire-xml.log`; runs `20260914T002716Z` (G6), `20260907T150643Z` (P11), `20260907T174654Z` (G2), `20260913T174516Z` (G3), `20260913T185936Z` (G5).

---

## 1. WHAT THE INTERFACE EMITS TODAY

The interface is a report PRODUCER only. Inbound reports are subscribed and discarded: `VrfC2SimService.cs:259` wires `_sdk.ReportReceived` to `OnReport`, and `OnReport` (`:2273-2277`) logs at Debug and returns ("the interface GENERATES reports; it does not consume them").

Every body is built by CONSTRUCTING the SDK's XSD-generated `C2SIM.Schema102` types and serializing with `C2SIMSDK.FromC2SIMObject` (`ReportBuilder.cs`). Deliberate departure from the C++ oracle's hand-assembled strings, which emitted malformed task-status XML and empty enum health fields (`ReportBuilder.cs:12-17`).

### 1.1 The shapes actually on the wire

| Report | Trigger | Cadence | Builder | Emit site |
|---|---|---|---|---|
| R-POS `ReportBody/PositionReportContent` (ONE content) | R1 periodic poll of every created unit with a reflected object | every `Vrf:PositionReportSeconds` - demo default 10 s, base default 0 = OFF | `ReportBuilder.cs:61-85` | `VrfC2SimService.cs:514-547` (`MaybeSendPositionReports`), called from the tick loop `:502` |
| R-POS-TEXT same shape | a VRF text report `POSITION "name" lat lon` from a Lua tracking script | per line | `ReportBuilder.cs:61-85` | `VrfC2SimService.cs:2633-2675` (`OnVrfTextReport`) |
| R-POS-BUNDLE `ReportBody` with N `PositionReportContent` | as above when `Vrf:BundlePositionReports=true` | count 10 / 10240 bytes / 2000 ms / stop | `ReportBuilder.cs:104-128` | `VrfC2SimService.cs:2655-2668`, `:2898-2902` |
| R-TASK `ReportBody/TaskStatus`, code TASKCMPLT | a dispatched task is judged complete | once per task | `ReportBuilder.cs:35-58` | `VrfC2SimService.cs:2587-2631` (`SynthesizeUnitCompletion`) |
| R-OBS `ReportBody/ObservationReportContent/NameObservation` | a unit whose type-map row is a PROXY, at creation | once per proxied unit, at init | `ReportBuilder.cs:140-178` | `VrfC2SimService.cs:740`, `:925-927` |

Notes:

- **R-POS is the ONLY position path that fires on 5.2.** R-POS-TEXT needs the 5.0.2 fixture's C2simEx Lua tracking script; 5.2 fixtures are on EntityLevel.sms and emit no POSITION text (PREREG_R1: "WatchVrf RPT rows: 20,784 in rung 2, 0 in every 5.2 run"). Dead code on the demo path, not a second channel.
- **R-POS-BUNDLE ships DEFAULT OFF** (`VrfSettings.cs:329`). The C++ oracle ALWAYS bundled (golden-trace report #1 carries several `PositionReportContent` in one body). Our 5.2 runs send one HTTP POST per unit per cycle - see sec 5 G3.
- **R-TASK has exactly TWO triggers**, both reaching the same emitter:
  (a) the vendor's completion callback `OnVrfTaskCompleted :2524`, attributed to the unit's IN-FLIGHT record (`InFlightTracker`) rather than "last dispatched" - the P0.1 fix;
  (b) ARRIVAL EVIDENCE (C15, user ruling 2026-09-07): `MaybeCheckArrivals :2471-2521` counts members within `Vrf:ArrivalRadiusMeters` (500) of the task's last vertex every `ArrivalCheckSeconds` (5) and, when MORE than `ArrivalMemberFraction` (0.5) are there, reports completion itself; the vendor's later completion is SWALLOWED ONCE (`:2530-2536`).
  Pairing rule: ONE TASKCMPLT per task. R10 fan-out member completions are aggregated by `FanOutTracker` and only the quorum emits (`:2551-2568`); a late straggler is swallowed rather than emitting a second, empty-uuid report.
- **R-OBS is R-SURFACE-PROXY** (user ruling 2026-07-17). Gated by `Vrf:SurfaceProxySubstitutions` (default TRUE, `VrfSettings.cs:106`). The substitution sentence rides in `NameObservation/Marking` because C2SIM 1.0.2 has no field for it.
- **ON A BRANCH, NOT IN MAIN:** TASKABRT from the progress watchdog (C16, row 19). `feat/sim-clock` commit `08146a2` adds `ReportBuilder.BuildTaskStatusReport(..., code, ...)` with `BuildTaskCompleteReport` as a TASKCMPLT wrapper, `VrfSettings` `StallDetection` (default FALSE), `StallClock` (wall), `StallWindowSeconds` (0 = calibrated 240 wall / 360 sim), `StallMoveMeters` 50, and `--stall-selftest` 62/62. Review pass 2 returned MERGE WITH FIXES; pass 3 in flight.

### 1.2 Content fields actually populated

`ReportBodyType`, every shape: `FromSender` = `ToReceiver` = the ZERO uuid (`ReportBuilder.cs:30-32` - the C++ hardcoded it with a "TODO: determine who is sender"; reproduced verbatim). `ReportID` = a fresh Guid per body (`:2723`). `ReportingEntity` = the subject (position), the taskee (status), the unit (observation); for a bundle, the FIRST fix's uuid (`ReportBuilder.cs:87-103`).

- **PositionReportContent sends:** `TimeOfObservation/DateTime/IsoDateTime`, `Location/GeodeticCoordinate/{Latitude, Longitude}`, `SubjectEntity`.
  **Does NOT send:** `EntityHealthStatus` (deliberately omitted, `ReportBuilder.cs:24-26`, because the golden's were EMPTY - the sec-6 aggregate-health bug), `HeadingAngle`, `Speed`, `Duration`, and any altitude in the coordinate. The schema HAS `HeadingAngle` and `Speed` on `PositionReportContentType` (`C2SIM_SMX_LOX_CWIX2024.cs:10900-11010`) and `AltitudeAGL`/`AltitudeMSL` on `GeodeticCoordinateType`.
- **TaskStatus sends:** `TimeOfObservation`, `CurrentTask` (the C2SIM task uuid), `TaskStatusCode`. The type has no `SubjectEntity` - the taskee is the body's `ReportingEntity`. The schema's full code set is TASKABRT / TASKCMPLT / TASKINPRG / TASKPEND / TASKSTRT (`:11077-11093`). **We only ever send TASKCMPLT.**
- **ObservationReportContent sends:** `TimeOfObservation` + one `Observation/NameObservation {ActorReference, Name, Marking}`. The other five Observation kinds - Activity, Health, Location, Resource, SubjectType (`:10280-10299`) - are unused.

### 1.3 Transport, and what happens to the server's answer

`PushReportAsync` (`VrfC2SimService.cs:2873-2878`) -> SDK `PushReportMessage` (`C2SIMSSDK.cs:454`) -> `WrappedMessage` adds `MessageBody/DomainMessageBody` (`:466-487`) -> `PushMessage` POSTs it over REST with performative REPORT (`:501-528`). The server rebroadcasts on STOMP; that is what ListenReports and STP subscribe to.

- **Fire and forget.** Every call site is a discarded Task. At 127 units / 10 s that is 127 concurrent HTTP POSTs per cycle, unordered.
- **The server's answer is DISCARDED.** `PushMessage` returns a `C2SIMServerResponse` carrying status OK or ERROR (`C2SIMServerResponse.cs:31`) and does NOT throw on ERROR (`C2SIMSSDK.cs:505-528` - only transport/parse failures throw). `PushReportAsync` awaits and drops the value. A report the server REJECTS is invisible. Only a transport failure is logged.

### 1.4 How reports are validated

| Layer | What it actually checks | Where |
|---|---|---|
| Construction | element names, nesting and ORDER come from the XSD-generated classes | `ReportBuilder.cs` |
| `--report-selftest` | builds TASKCMPLT + single position + 3-fix bundle + 1-fix bundle, ROUND-TRIPS each through `ToC2SIMObject<MessageBodyType>`, asserts code, task uuid, reporting entity, lat/lon, health absent, one ReportID per bundle | `ReportSelfTest.cs:15-113`; `Program.cs:28` |
| Live capture | `tools/ListenReports` writes every ReportBody verbatim (`--stop-file`, `--rest-url`/`--stomp-url`; capture written ONLY at exit) | `tools/ListenReports/Program.cs` |
| Server | accepts or rejects - we never look | sec 1.3 |

**NOT validated anywhere: XSD schema validation.** No `XmlSchema`/`Validate` call exists in `src/VrfC2SimApp` (the only `.xsd` mentions are comments citing line numbers). The round-trip proves well-formedness and that the SDK reads our own output back; it does NOT prove the body satisfies C2SIM 1.0.2 required-element/cardinality constraints, nor that STP's parser accepts it.

---

## 2. WHAT G6 ACTUALLY EMITTED (and P11/G2/G3/G5)

### 2.1 The G6 bus capture

14,698 report bodies, 13.6 MB, mean 973 chars, max 1,226. **ALL 14,698 parse as well-formed XML** (0 malformed). 14,698 distinct ReportIDs - no duplicates. Every body carries exactly ONE `ReportContent` block (bundling off, as configured).

| kind | count |
|---|---|
| ObservationReportContent / NameObservation | 102 |
| PositionReportContent | 14,596 |
| TaskStatus | **0** |

- The 102 observations land in 0.9 s (00:30:05.585-00:30:06.484), one per PROXY unit of the 128-unit init under the no-lifeform probe type map. Identical count in all five runs.
- Positions: 127 distinct `SubjectEntity` uuids, 115 cycles each (114 for 15 units that started one cycle late), 00:30:24.5-00:49:27.9 = 1,143 s.
- Cadence per unit: median gap 10.01-10.05 s against a 10 s target; typical min/max 7.6 / 12.2 s. 7 of 127 units show ONE paired outlier (~22 s gap immediately followed by a 0.02-0.47 s gap) - one cycle delivered late and bunched with the next, **not a loss**; each still has the full 115 fixes.
- Per-second buckets: a clean 127 in one bucket for most cycles; a few split across two adjacent seconds - STOMP fan-out jitter.

### 2.2 The TaskStatus timeline is NOT in the capture - it is in the app log

The capture holds ZERO TaskStatus because ListenReports stopped at its own 1200 s cap, while the run continued unattended for ~9 hours after the runner was killed (RUNNER_EXIT127).

From `vrfc2simapp.log` over the whole 9 h:

- **3,449** "R1 position reports" cycles, steady state "127 sent, 1 skipped" -> roughly **437,000 position reports pushed**, of which the capture covers 14,596 (3.3%).
- **8** `SENT TASK STATUS REPORT (TASKCMPLT)` - the complete task-status output of the run.
- **All eight came from ARRIVAL EVIDENCE (C15).** There were only 3 vendor completions in the whole run (1-1/2/1_AD~PXY, B/5-20~PXY, 5-20/2/1_A~PXY) and each arrived AFTER the evidence report for that unit, so all three were swallowed. **Without C15 this run would have reported nothing at all to STP in 9 hours.**

| # | unit | task | evidence | delay after dispatch |
|---|---|---|---|---|
| 1 | 1-1/2/1_AD~PXY | T23_AOA_SE_1-1_RECON | 3/4 within 500 m, nearest 350 m | 1,659 s |
| 2 | B/5-20~PXY | T35_AOA_SE_B/5-20_IN_(MECH)_P1 | 3/4, nearest 372 m | 1,840 s |
| 3 | 5-20/2/1_A~PXY | T31_AOA_SE_5-20_IN_(MECH) | 5/6, nearest 435 m | 3,708 s |
| 4 | B/5-20~PXY | T36_ClearEnemyThreatsFromPaa25E | 3/4, nearest 408 m | 2,356 s |
| 5 | 40/2/1_AD~PXY | T19_AOA_SE_40_EN | 4/6, nearest 303 m | 6,592 s |
| 6 | 5-20/2/1_A~PXY | T32_AOA_SE_5-20_IN_(MECH) | 4/6, nearest 442 m | 3,705 s |
| 7 | 5-20/2/1_A~PXY | T33_SecureObjMadison | 6/6, nearest 75 m | 35 s |
| 8 | 856/HHC~PXY | T27_SecureMovementCorridors | 3/4, nearest 43 m | 9,402 s |

The FIRST one lands 1,659 s after its dispatch; the observer's 1,200 s window ends before it. That, not a defect in the emit path, is why G6's capture shows no task status.

### 2.3 Anomalies found in G6

| # | Anomaly | Evidence | Assessment |
|---|---|---|---|
| A1 | **129 report pushes FAILED** with "The response ended prematurely. (ResponseEnded)" - server-side connection close during the REST POST (stack: `HttpConnection.SendAsync` -> `C2SIMClientRESTLib.SendTrans:389`) | 129 occurrences in 6 bursts at app-log lines 29.79M (51), 44.10M (13), 49.78M (28), 49.78M (22), 52.24M (14), 63.43M (1) | ~0.03% of pushes; **SILENT LOSS** - fire-and-forget, no retry, no counter. The largest burst drops 51 of one cycle's 127 fixes. No TASKCMPLT was in any burst, but nothing in the design PREVENTS it |
| A2 | **One init unit NEVER gets a position report, in every run**: `2/1_AD/25_` (uuid `47cb2dff-...`), the brigade. Logged forever as "127 sent, 1 skipped (no reflected object yet)" | 127 capture subjects vs 128 init Unit uuids; the uuid appears only twice in P11/G2/G3 captures, both times in its own ObservationReport | see sec 5 G1 - cause identified; the log line cannot say WHICH unit or WHY |
| A3 | **3 tasks REFUSED at dispatch** ("NO LOCATION GIVEN - CAN'T EXECUTE TASK 'T9...'/'T24...'/'T37...'") and **7 skipped** ("predecessor ... abandoned upstream; policy=skip ... NOT dispatched"). **Not one produced a C2SIM report** | `:1481, 7650103, 17287457` and `:1527-1531, 7650105-7650113, 17287459, 26479137` | STP is told nothing about a task the simulation declined. TASKABRT count in 9 h: **0** |
| A4 | 42 tasks in the order, 14 MoveAlongRoute dispatches, 8 TASKCMPLT | "ORDER: 42 task(s) for 11 taskee(s)" | **34 of 42 tasks produced no status of any kind in 9 hours** |
| A5 | 144 "PLACEMENT: UNIT" lines for a 128-unit init (order-time MATERIALIZE re-creates), but only 102 R-OBS, all at init | grep counts | a unit re-created at order time as a different template does NOT re-announce its substitution |

No malformed body, no duplicate ReportID, no duplicated content was found in G6.

### 2.4 Cross-run comparison (each run's own `reports-captured.log`)

| run | label | window (s) | bodies | Observation | Position | TaskStatus | distinct subjects |
|---|---|---|---|---|---|---|---|
| 20260907T150643Z | P11 | 4,292 | 54,189 | 102 | 54,084 | **3 (all TASKCMPLT)** | 127 |
| 20260907T174654Z | G2 | 4,338 | 54,537 | 102 | 54,435 | 0 | 127 |
| 20260913T174516Z | G3 | 1,554 | 19,660 | 102 | 19,558 | 0 | 127 |
| 20260913T185936Z | G5 | 642 | 8,103 | 102 | 8,001 | 0 | 127 |
| 20260914T002716Z | G6 | 1,162 | 14,698 | 102 | 14,596 | 0 | 127 |

Stable across all five: 102 observations, 127 of 128 subjects, ~12.6 position reports per second of capture. Only P11's observer outlived its first completion, and it captured 3 TASKCMPLT.

---

## 3. THE DESERIALIZATION ERROR AT 00:30:05 AND 00:30:23

Symptom (Windows Application log, .NET Runtime, category `C2SIM.C2SIMSDK`):

```
Failed to deserialize xml to type C2SIM.Schema102.MessageBodyType:
There is an error in XML document (1, 2).
```

**CAUSE (verified): it is OUR OWN root-robust parse fallback, logged by the SDK at Error level.**

- The SDK's STOMP pump does not hand clients a `MessageBody`. It parses the envelope with `XElement`, descends through `DomainMessageBody`, and raises the event with the BARE inner element (`C2SIMSSDK.cs:639-676`) - a bare `C2SIMInitializationBody`, or a bare `OrderBody`.
- `InitParser.Parse` tries the ENVELOPE first and falls back: `InitParser.cs:38` `try { init = ToC2SIMObject<S.MessageBodyType>(xml)... } catch { }` then `:42` the bare body. `OrderParser.cs:37/:41` does the same. Both comment the reason: a pushed FILE is MessageBody-rooted, the live event is not.
- `ToC2SIMObject` logs at Error and rethrows on any failure (`C2SIMSSDK.cs:823`). The interface swallows the exception; the LOG LINE survives. "(1, 2)" is line 1 column 2 - the root element name, exactly what a root mismatch produces.

**IMPACT: cosmetic only.** Both messages parsed on the fallback and were processed normally - app-log line 37 is followed immediately by the full type map and 144 PLACEMENT rows; line 1245 is followed immediately by "ORDER: 42 task(s) for 11 taskee(s)". No report is involved.

**Falsifiers checked:**
- *"A genuinely malformed message from STP or the server."* Refuted: both messages parsed and executed on the very next line.
- *"One of OUR reports being rejected."* Refuted: reports go out through `PushReportMessage`; a rejection would be an ERROR status or a transport exception, not a `MessageBodyType` deserialize. And the timestamps are exactly the init (00:30:05) and the order (00:30:23.054, the ORDER line at the head of `c2sim-bus.log`).
- **Recurrence:** exactly **2 per run** in every run checked - 20260913T174516Z, 20260913T185936Z, 20260907T174654Z, 20260907T150643Z and G6 - i.e. once per inbound C2SIM message (1 init + 1 order). The signature of a per-message code path, not an intermittent fault.

**Why it still matters:** an operator watching the interface at the demo (row 15) sees a red `fail:` line for every message STP sends, and the machine's Application event log collects the same. It is the loudest thing in the log and it means nothing. Fix is three lines (B5).

---

## 4. WHAT STP CONSUMES AND DISPLAYS - WHAT THE RECORD ACTUALLY SAYS

The record is nearly **silent** on STP's rendering. There is no STP documentation in this repo and no capture of STP consuming anything.

**VERIFIED from the record:**
1. STP is the SOURCE of the init and the orders, and `Vrf:ClientId` must equal the init's SystemName or nothing is created (`docs/RUNBOOK.md` sec 2, the CLIENTID TRAP).
2. **STP matches position reports to units BY NAME, and names are capped:** "Entity names truncate to exactly 10 chars (DIS/RPR marking-text limit; C2SIMxmlHandler.cpp:2365) ... On a truncation COLLISION, addUnit() silently rewrites the last char (modDigit 0-9), which then breaks position-report name matching. Set STP's max-name-length to 10 to make names deliberate." (`docs/PORT.md` sec 6). Every unit name in the COA-STP1 captures is indeed exactly 10 characters (`1-35/2/1_A`, `FSC/D/1-1/`, `7913/HQ_71`).
3. The DEMO exit criterion is written in terms of what STP shows: the operator "sees the three units created and moving in the GUI and their position + task-status reports in STP" (DEMO_READINESS Exit criterion; DEMO_RUNBOOK sec 1 step 5).
4. The planned pre-flight DELIVERY treats the STP MAP as a third-party renderer: "(2) STP map - an ObservationReport at the face's coordinates with the numbers, rendered by STP (**their end**; the channel to invest in)" (row 20). "Their end" is the record conceding we do not control or know that rendering.
5. Row 19 records the open question in the user's own terms: "whether TASKABRT is the code STP should see, or a distinct rendering on their side".

**NOT IN THE RECORD (do not assume):**
- Whether STP renders `ObservationReport/NameObservation` at all, or where.
- Whether STP plots a `LocationObservation` as a map graphic (leg_check's `emit_c2sim` writes one per flagged leg, with a TODO: "C2SIM 1.0.2 has NO observation type carrying a location AND free text ... If SISO/STP prefer one carrier, replace both with the agreed element").
- Whether STP reads `HeadingAngle` / `Speed` / `EntityHealthStatus` if we sent them.
- Whether STP distinguishes TASKABRT from TASKCMPLT visually, or shows TASKSTRT/TASKINPRG.
- Whether STP tolerates 12-13 reports per second, or wants bundles.
- What STP does with the zero-uuid `FromSender`/`ToReceiver` on every report.

**Consequence:** every build item that changes what STP *sees* (rather than what it is *told*) is a question for the STP side before it is a build. The cheapest way to close five of those unknowns is one message to the STP owner with a sample of each body we emit (B10).

---

## 5. GAPS FOR THE DEMO, RANKED

### G1 (HIGH) A unit represented by a single PLATFORM never reports its position at all
The brigade `2/1_AD/25_` is the one unit the fidelity table maps to a PLATFORM (`M577A2_Command_Post`) rather than an aggregate: `PLACEMENT: PLATFORM` appears **exactly once** in the whole G6 log and it is this unit. It has a VRF object (`VRF_UUID:2c8917fe-...`) and the observer federate records POS rows for it from wall 44 s, so it exists and is on the network - but it is skipped by R1 every cycle for 9 hours.

**Mechanism (supported by the log itself):** the interface requested marking `2/1_AD/25_~PXY` (PLACEMENT line) but VR-Forces returned the object under the TRUNCATED name `2/1_AD/25_` - every console line for that uuid prints the short form, and those lines are fed from `_nameByVrfUuid[e.Uuid] = e.Name`, i.e. the `ObjectCreated` callback's name (`VrfC2SimService.cs:2296-2297`). `ObjectCreated` therefore keys `_vrfUuidByName` under `2/1_AD/25_`, while `_unitByC2SimUuid` holds `Name = "2/1_AD/25_~PXY"`. R1 looks the unit up BY NAME (`:529`) and misses forever. Aggregates are unaffected because aggregate names are not DIS-marking-truncated - `1-1/2/1_AD~PXY` (14 chars) survives everywhere in the same log.

**Competing hypothesis weighed:** "our federate simply never reflects that entity". Not excluded by the network evidence alone (the observer is a different federate), but it does not explain why the two names differ in our own log, and the name mismatch alone is sufficient - the by-name lookup at `:529` cannot succeed. **Falsifier:** log the skipped unit's name and its `_vrfUuidByName` hit/miss; if the name resolves and `TryGetEntityGeodetic` is what fails, this diagnosis is wrong.

**Blast radius beyond reports:** EVERY by-name lookup for such a unit misses - arrival-evidence completion (`:2483`) and the R1 poll at minimum. A platform-mapped unit is invisible to STP and can never complete a task. Today the COA has one such unit and it is not tasked; a demo ORBAT with a tasked HQ platform would fail silently.

### G2 (HIGH) A task the simulation cannot or will not execute produces NO report
Three tasks refused at dispatch for having no location (the ADA coverage verbs) and seven skipped because a predecessor was abandoned. STP received nothing for any. Combined with G4, **34 of 42 tasks produced no status in 9 hours.** This is row 16's known DEFEND/HoldObjective problem seen from the reporting side: the audience sees a unit standing still and STP shows nothing at all.

### G3 (HIGH) Silent report loss, and no visibility into the server's verdict
129 pushes were dropped by the server mid-POST in G6 with no retry and no aggregate counter (A1), and a server that ACCEPTS the connection but REJECTS the content is not even logged (sec 1.3). Position fixes are self-healing (the next cycle corrects the map); **a lost TASKCMPLT is not** - it is the only notification for that task, ever. The design places the one message that cannot be lost on the same best-effort path as the 437,000 that can.

### G4 (HIGH) STP is told only about COMPLETION, never about start, progress or abort
The schema offers TASKPEND / TASKSTRT / TASKINPRG / TASKCMPLT / TASKABRT; we emit TASKCMPLT only. In G6 the first status report reached the bus **1,659 s (27 min)** after the order. For 27 minutes an STP operator had position reports and nothing else - no confirmation the order was even accepted. At demo pace that is the whole demo. TASKSTRT at dispatch is one line of code and no new decision, and it is the single largest improvement in what STP sees per unit of work.

### G5 (MEDIUM) The pre-flight warnings are not on the reporting channel at all
`tools/preflight/leg_check.py` is calibrated and committed (80831b9; 3/3 caught, 0/6 false alarms, margin 0.096) and can already write the exact bodies - `emit_c2sim` (`leg_check.py:1037-1121`) produces one ReportBody per flagged leg carrying a `LocationObservation` (where) plus a `NameObservation` (the numbers, wording kept predictive: "PREDICTED IMPASSABLE"). But it is a **standalone Python tool**, run by hand, writing a file. Nothing in the interface calls it, and the product "contains no Python" by ruling (row 6). Today the demo audience sees a unit drive into a ridge with no warning anywhere - precisely the user's 2026-09-13 point ("users don't read logs"). Row 20's ordering stands: warnings first, TASKABRT only after calibration shows zero false alarms.

### G6 (MEDIUM) The watchdog exists but is OFF and its code is undecided
C16 is built on `feat/sim-clock` with a 62/62 self-test and ships default OFF, so without a user decision and a settings change the demo behaves exactly as G6 did: a frozen unit reports its (unchanging) position every 10 s forever and never reports a status. The open question is row 19's: is TASKABRT the code STP should see for "stopped making progress"? TASKABRT means the task was ABANDONED - a claim about the simulation's intent, not an observation. An alternative needing no STP agreement is an ObservationReport (the unit stopped, where, for how long) plus TASKINPRG, leaving TASKABRT for when the interface actually gives up. **This is the user decision (RULE gate) the lane is blocked on.**

### G7 (MEDIUM) Position reports carry no heading, no speed, no health
The schema has `HeadingAngle` and `Speed` on `PositionReportContentType` and the sim has both: `VrfFacade::TryGetEntityGeodetic` reads `DtBaseEntityStateRepository::location()` (`VrfFacade.cpp:1016-1051`), and the same repository exposes velocity and orientation. Without speed, STP cannot distinguish "moving" from "stopped" from its own data - exactly the gap the watchdog is being built to fill from our side. Health is a second, larger question: the C++ oracle sent EMPTY health elements and we correctly send none - but "none" means STP can never show attrition.

### G8 (MEDIUM) Report cadence and bus load are unmeasured against STP
127 individual HTTP POSTs every 10 s (12.7/s, ~12.4 KB/s, ~22 MB per 30-minute demo) with bundling available and switched off. The C++ oracle bundled; our bundle path exists, has a self-test, and is default-off with no measurement behind the choice. The 6 connection-close bursts (A1) are the only load evidence we have and they are suggestive, not conclusive. Note also that ONE demo ORBAT unit = one report every 10 s regardless of whether it is in the COA - 117 of the 127 reported units in G6 are corps-level CONTEXT that never move ("COA is not the ORBAT").

### G9 (LOW-MEDIUM) Operator observability of the reporting channel (row 15)
Row 15's owed "READY" line is **DONE** (G6 line 29: "READY - joined the federation, 1 VR-Forces back-end(s), type mapping = FidelityTable, compose = on, position reports every 10s"), and the per-order summary is **DONE** too ("ORDER: 42 task(s) for 11 taskee(s); verbs [...]"). Still missing on the reporting side:
- the per-cycle line says "127 sent, 1 skipped (no reflected object yet)" and never names the skipped unit; the same counter covers TWO different failures (name-lookup miss at `:529`, geodetic-read miss at `:530`) - which is what hid G1 for five runs;
- no cumulative counter of reports sent or FAILED, so A1's 129 losses are invisible unless someone greps a 5.9 GB log;
- the loudest line in the log is the harmless deserialize error;
- no line for "task dispatched -> STP told" (there is none to log).

### G10 (LOW) A unit re-created at order time does not re-announce its substitution
144 PLACEMENT lines but 102 R-OBS, all in the first second. A unit MATERIALIZEd at order time as a different template (e.g. A/6-56/HHC re-created as Air Defense Artillery Platoon (USA)) sends no new NameObservation, so a consumer that took the init-time announcement as final has stale fidelity information.

### G11 (LOW) The capture instrument stops before the interesting part
ListenReports' `-WatchSecs` cap ended G6's capture at 1,200 s; the first TASKCMPLT was at 1,659 s. Four of five runs captured ZERO task status for this reason. A HARNESS gap, not a product gap - but it is why "TASKCMPLT at scale" keeps having to be read out of a multi-GB app log.

---

## 6. BUILD LIST, ordered by value / cost

### B1. TASKSTRT at dispatch (and the dispatch-side failure codes)
**What:** emit one `TaskStatus/TASKSTRT` the moment a task is dispatched, from the same `MarkDispatched` point that already records the in-flight task; and a TaskStatus for the two dispatch-side outcomes that today are silent - a refused task ("NO LOCATION GIVEN") and a skipped successor.
**Anchor:** `TaskStatusCodeType` TASKSTRT / TASKABRT (`C2SIM_SMX_LOX_CWIX2024.cs:11077-11093`); `ReportBuilder.BuildTaskStatusReport` already exists on `feat/sim-clock` (08146a2).
**Test:** extend `--report-selftest` with a TASKSTRT round-trip; one COA-STP1 run must show 14 TASKSTRT (= the MoveAlongRoute dispatch count) on the bus within seconds of the order, plus one status per refused/skipped task; TASKCMPLT count and pairing unchanged.
**User decision:** yes, for the FAILURE codes only - is a refused task TASKABRT, or does STP want TASKPEND/nothing? TASKSTRT itself needs none.
**Value:** closes G4 and half of G2; turns a 27-minute silence into immediate feedback. **Cost:** low.
**STATUS: BUILT** (`81d108c` + review fixes, `feat/reporting`), live confirmation owed - gate 2 above. `TaskStatusPolicy.cs` is the whole rule set (one TASKSTRT per execution, one TASKCMPLT per task, TASKABRT never suppresses a later TASKCMPLT, a TASKCMPLT does suppress a later TASKABRT); `PushTaskStatus` is the single emit point. Review fixes on top: TASKSTRT re-arms after a COMPLETION only, never after an abort (finding 10); the MOVE half of an advance-then-engage reports TASKINPRG (finding 4 - see the STATUS section at the top); the three silent dispatch dead ends with a taskee uuid (unit-not-created, taskee-not-in-initialization, orchestration threw) now report TASKABRT (finding 7). **G2**: the vendor's own `success()==false` is forwarded since the `feat/integration` wiring (2026-09-14) - see the STATUS section - so that branch is live and closes on its run gate.
CORRECTION 2026-09-25: this is a SUPERVISOR reading (2026-09-13), not an owner ruling - RL-20260914-01 covers only the TASKABRT code; see its scope note. The current TEMPORARY completion position is RL-20260921-09.

### B2. Never lose a task status: check the server's answer and retry
**What:** (a) make `PushReportAsync` inspect the returned `C2SIMServerResponse` and log an ERROR status with the server's message; (b) give TASK-STATUS pushes (not position pushes) a bounded retry with backoff and a loud line if they finally fail; (c) add a cumulative counter ("reports: N sent, M failed") to the existing R1 line.
**Anchor:** `C2SIMServerResponse.ResponseStatus` (`C2SIMServerResponse.cs:31`); `PushMessage` does not throw on ERROR (`C2SIMSSDK.cs:505-528`); measured loss in G6 = 129 pushes in 6 bursts.
**Test:** offline - a fake SDK returning ERROR once then OK proves one retry and one log line; live - point the interface at a stopped server, confirm the loud line, restart, confirm the status arrives. Existing runs are the regression control (TaskStatus loss must go to 0).
**User decision:** no. **Value:** closes G3. **Cost:** low.
**STATUS: BUILT** (`f057fc2` + review fixes, `feat/reporting`), live confirmation owed. `ReportPush.cs` is the pure retry/inspection policy (injected transport, testable with no SDK and no server); `PushReportAsync` counts every outcome and is LOUD on a final failure; the R1 line now labels its cumulative pair ("cumulative: N sent, M failed" - review finding 12, the unlabelled pair read as a second per-cycle count). Review fixes on top: an EMPTY server body is no longer counted as a delivery for a TaskStatus (finding 6 - `SendTrans` returns the body with NO status-code check, `C2SIMClientRestLib.cs:377-401`, so "" reaches `ToC2SIMObject` and becomes a null response); and the at-least-once semantics are stated in the `ReportPush` header (finding 5). DETERMINATION for the G6 symptom, measured from the run log: "The response ended prematurely" is an EXCEPTION, not an empty body - `HttpRequestException` "An error occurred while sending the request" with inner `HttpIOException` "The response ended prematurely. (ResponseEnded)", out of `_httpClient.SendAsync` at `C2SIMClientRestLib.cs:389`, rethrown by `SendTrans` as `C2SIMClientException`. B2 already retried that case; the empty-body hole was a separate one.

### B3. Fix the platform-unit name mismatch (G1)
**What:** key the R1 poll and the arrival check by the unit's VRF UUID rather than its name - `ObjectCreated` already gives both (`:2296`) - or record the callback's returned name against the `CreatedUnit` so the two maps cannot diverge. Separately, split the R1 "skipped" counter into "name unresolved" and "no reflected object", and name the unit the first time each occurs.
**Anchor:** `VrfC2SimService.cs:2294-2297`, `:529-530`, `:2483`; `docs/PORT.md` sec 6 on the 10-character DIS marking limit and its collision behaviour.
**Test:** offline - a self-test that feeds `ObjectCreated` a TRUNCATED name for a requested longer marking and asserts the unit is still found. Live - the COA-STP1 run must report 128 of 128 subjects and the R1 line must read "128 sent, 0 skipped".
**User decision:** no. **Value:** closes G1 (a silent, total reporting failure for a whole class of unit, which also makes such a unit uncompletable). **Cost:** low; care needed not to disturb the compose/materialize paths that legitimately key by name.
**STATUS: BUILT** (`ebf8da9` + `f0d1c68` + review fixes, `feat/reporting`), live confirmation owed - gate 1 above. `NameRegistry.cs` owns the name <-> uuid correlation: a returned name that is not itself requested resolves to the UNIQUE requested name that has it as a strict prefix, both spellings bind to the uuid, and the reverse map holds the resolved name. The R1 "skipped" counter is split into NAME UNRESOLVED and NOT REFLECTED and names each unit ONCE. The exact-match path is provably the two writes the pre-B3 code made, so the 127 units that already worked are untouched. Review fixes on top: an exact match that is ALSO a strict prefix of longer requested names keeps the exact binding but WARNS and names every candidate, and a create-time NAME PRE-FLIGHT lists such pairs once per batch (finding 1 - COA-STP1 really contains `510/40~PXY`, exactly 10 characters, plus its four EXPAND children); resolution targets only names still AWAITING their ObjectCreated, so a later object cannot take a live unit's identity (finding 2); a candidate registered after a resolution was cached invalidates that cache entry (finding 3). **Residual, stated in the class header:** correctness depends on every requested name fitting the marking width; `MaxVrfMarkingChars` here is 34, the sim truncates at 10-11.

### B4. Wire the route pre-flight into the interface at order receipt (row 20, deliveries 1 and 2)
**What:** port `leg_check.py`'s scoring into the interface (shelling out is not an option - the product contains no Python, row 6) so that at ORDER RECEIPT, before dispatch, each leg is scored and each flagged leg emits the ObservationReport pair the tool already writes. **Warnings only**; no TASKABRT from the pre-flight until row 20's calibration gate is met.
**Anchor:** `ObservationReportContentType` / `LocationObservationType` + `NameObservationType` (`:10233-10299`, `:10490-10590`); the emitter skeleton and field order are already written and cross-checked against a real capture (`leg_check.py:1037-1121`); `docs/experiments/PREFLIGHT_CALIBRATION_2026-09-13.md` (threshold 0.92 at a 40 m window, 3/3 caught, 0/6 false alarms, margin 0.096).
**Test:** offline - the C# scorer must reproduce leg_check.py's 14 flagged legs on COA-STP1 leg for leg (a fixture comparison, not a re-derivation); a report self-test round-trips the pair. Live - 14 ObservationReports on the bus within seconds of the order, before any unit moves.
**User decision:** whether the pre-flight runs at all in the demo, and whether STP wants two Observations or one agreed carrier (sec 4 unknown).
**Value:** closes G5 - the item the user called "very valuable". **Cost:** MEDIUM-HIGH (a port of a ~1,400-line Python tool incl. tile fetch, soil chain, `.entity` parse). Most expensive item here and the one most worth splitting - the scorer can land behind a flag with the tool's own outputs as the fixture.

### B5. Silence the false deserialize error (sec 3)
**What:** stop calling `ToC2SIMObject<MessageBodyType>` speculatively. Sniff the root element name first (`XElement.Load` or an ordinal test for `"MessageBody"`) and call the matching overload once. Three lines in `InitParser.cs:38`, three in `OrderParser.cs:37`.
**Anchor:** the SDK's own pump does exactly this - XElement, LocalName switch (`C2SIMSSDK.cs:639-676`); `ToC2SIMObject` logs at Error and rethrows (`:823`).
**Test:** `--parse-init` / `--parse-order` on both a MessageBody-rooted FILE and a bare-body string must both parse, and the SDK logger must emit NOTHING. Live: zero "Failed to deserialize" lines in the next run's log (today exactly 2, in every run).
**User decision:** no. **Value:** removes the loudest and most misleading line an operator sees at the demo (G9, row 15). **Cost:** trivial.
**STATUS: BUILT** (`9286398`, `feat/reporting`), live confirmation owed - gate 3 above. `C2SimXml.RootLocalName` sniffs the root with an `XmlReader` and `InitParser`/`OrderParser` call the matching overload ONCE; `--parse-selftest` drives both parsers over MessageBody-rooted, DomainMessageBody-rooted and bare-body documents with a capturing SDK logger and asserts ZERO SDK error lines. The sniff accepts a strict SUPERSET of what the old parsers accepted (a `DomainMessageBody`-rooted order could not be read at all before). No review finding against it; the cold-start review's one note is that a DOCTYPE now produces one misleading error naming the bare type instead of two, with the same net outcome.

### B6. Land the watchdog and decide its code (G6)
**What:** finish `feat/sim-clock`'s pass-3 review, merge, set the demo default. Then the code question: TASKABRT, or ObservationReport-plus-TASKINPRG.
**Anchor:** row 19 (APPROVED 2026-09-13, "2 as recommended"); the vendor pass that makes it necessary - `singleTaskControllerComponent.h:192-205` "always returns false", the `decideToGiveUpTask` sample (FINDING_EARLY_STOPS sec 6a); `TaskStatusCodeType`.
**Test:** already specified by row 19 - `--stall-selftest` (62/62 today) plus one run against the known 1-35 freeze (must fire by sim ~500) and the known-good units (must NOT fire).
**User decision:** YES - the lane's named RULE gate. **Value:** the frozen-unit case is the one the demo is most likely to hit. **Cost:** low (built); the decision is the blocker.

### B7. Heading and speed in the position report
**What:** extend the facade's `Geodetic` (or add a sibling read) with velocity and orientation from `DtBaseEntityStateRepository`, and populate `HeadingAngle` and `Speed`.
**Anchor:** `PositionReportContentType.HeadingAngle` / `.Speed` (`:10900-11010`); `VrfFacade::TryGetEntityGeodetic` reads the same state repository (`VrfFacade.cpp:1016-1051`); native changes pre-authorized (back up the DLLs, `/t:Rebuild`, redeploy all 7 copies).
**Test:** `--report-selftest` asserts both fields round-trip; one run shows non-zero Speed for a moving unit and 0 for the known-frozen 1-35 - which also gives the watchdog an independent cross-check.
**User decision:** worth ASKING STP whether they render it before building. **Value:** G7; gives STP the datum it needs to see a stall without interpretation from us. **Cost:** medium (C++ facade + bridge + marshalling).

### B8. Re-announce a substitution when a unit is re-created at order time
**What:** emit the NameObservation again from the MATERIALIZE path when the template actually changes.
**Anchor:** R-SURFACE-PROXY ruling 2026-07-17 ("never silently swallowed", `VrfSettings.cs:99-105`); emit site `:925-927`, proxy list built at `:740`.
**Test:** COA-STP1 must show more than 102 observations, one per MATERIALIZE that changed the template; the init-time count unchanged.
**User decision:** no. **Value:** G10, small. **Cost:** trivial.
**STATUS: BUILT** (`4e9a9ce` + review fixes, `feat/reporting`), live confirmation owed - gate 4 above. `SubstitutionAnnouncer.cs` turns "how is this unit represented" into ONE comparable string (template + shell/composed-from-N) and announces only when it is new or has CHANGED, so a re-creation as the same thing stays silent; `MaterializeUnit` calls it after the case-1 and platform early-returns. Review fix on top: the init loop's fallback when a proxied unit's final plan is not in the batch is now a SENTINEL, not the unit's own name (finding 8). NOTED, not a defect: `Substituted(sub, composedFrom)` returns true for a composition even with an empty substitution string, which widens R-SURFACE-PROXY beyond the 2026-07-17 ruling's wording - a composition standing in for the unit's own type is the intent, and the ruling's record should say so.

### B9. Measure the bundle, then choose a default
**What:** run one COA-STP1 with `Vrf:BundlePositionReports=true` against the existing default-off runs; compare server-side losses, end-to-end latency and capture size **at equal sim time**.
**Anchor:** the C++ oracle bundled (textIf.cxx:435-544; golden trace); our path and self-test exist (`ReportBuilder.cs:104-128`, `ReportSelfTest.cs:58-108`); lesson "compare at equal SIM time".
**Test:** bundle ON must not lose a TASKCMPLT, must carry the same fixes per unit per cycle, and must reduce the "response ended prematurely" count.
**User decision:** no (unless STP prefers one shape). **Value:** G8; also the cheapest mitigation for G3 if the bursts are load. **Cost:** low (one run, no code).

### B10. Ask the STP owner five questions (the cheapest item here)
**What:** one message with a sample of each body we emit, asking: (1) does STP render ObservationReport/NameObservation, and where; (2) does it plot LocationObservation on the map; (3) does it use HeadingAngle/Speed/EntityHealthStatus; (4) what does it do with TASKSTRT, TASKINPRG, TASKABRT; (5) does it prefer bundled or single position reports; **(6) what does STP do with a DUPLICATE ReportID** - B2's retry is at-least-once and re-sends the identical xml, so the same TaskStatus can arrive twice under one id (added 2026-09-14 from the cold-start review, finding 5; not yet in the draft message).
**Anchor:** sec 4's NOT-IN-THE-RECORD list; row 20's own words "their end; the channel to invest in".
**Test:** the answers are the test for B1, B4, B7 and B9.
**User decision:** yes - outward-facing message. **Value:** converts four of the eleven gaps from guesswork into specification. **Cost:** trivial.

### Suggested order
**B5, B3, B1** (no decisions, low cost, all visible at the demo) -> **B2** -> **B10** in parallel with everything -> **B6** once the user rules -> **B9** (one run) -> **B8** -> **B7** -> **B4** (split: scorer behind a flag first).

---

## 7. VERIFIED vs ASSUMED

**VERIFIED** (read from code, or counted in a capture, this session):
- The five report shapes, their triggers, code paths and exact populated fields (sec 1.1, 1.2).
- Reports are pushed over REST and the server's OK/ERROR status is discarded; only transport failures are logged (`C2SIMSSDK.cs:505-528`, `C2SIMServerResponse.cs:31`, `VrfC2SimService.cs:2873-2878`).
- No XSD validation exists in the interface; `--report-selftest` is a round-trip, not a validation.
- G6 capture: 14,698 bodies, 102 Observation + 14,596 Position + 0 TaskStatus, 127 distinct subjects, 0 malformed, 0 duplicate ReportID, 1 ReportContent per body, median cadence 10.01-10.05 s, mean body 973 chars.
- G6 full run: 3,449 R1 cycles, 8 TASKCMPLT (all from arrival evidence), 3 vendor completions (all swallowed), 129 failed pushes in 6 bursts, 0 TASKABRT, 3 refused tasks, 7 skipped successors, 14 MoveAlongRoute dispatches for 42 tasks.
- P11/G2/G3/G5 counts in sec 2.4, each from that run's own `reports-captured.log`.
- The deserialize error's cause, impact and per-run count of exactly 2 (sec 3), with both competing hypotheses checked.
- `2/1_AD/25_` is the only PLATFORM placement in G6 and the only init unit with no position report, in all five runs; its console lines print a name (`2/1_AD/25_`) different from the requested marking (`2/1_AD/25_~PXY`) in the same log.
- The C16 watchdog exists on `feat/sim-clock` with TASKABRT, default OFF, `--stall-selftest` 62/62, pass-2 verdict MERGE WITH FIXES.
- The pre-flight emitter is a standalone Python tool; nothing in the interface calls it.

**ASSUMED / NOT ESTABLISHED** (do not build on these without checking):
- Anything about STP's RENDERING (sec 4's second list). The record says what we send, never what STP shows.
- G1's mechanism is **SUPPORTED, not proved**: the name mismatch is visible in the log and is sufficient to explain the miss, but "our federate never reflects that entity" was not independently excluded. The falsifier is named in G1 and is one log line away.
- That the 129 dropped pushes are a LOAD effect. Six bursts over 9 hours is consistent with load, with a server-side GC pause, and with a proxy/keep-alive timeout. B9's run is the test.
- That no TASKCMPLT has ever been lost. None was lost in G6 (the bursts fall millions of lines after the last TASKCMPLT), but nothing in the design prevents it.
- The ~437,000 figure for position reports pushed in G6 is 127 x 3,449 cycles - arithmetic on the steady-state line, not a count of individual pushes.
- That C2SIM 1.0.2 has no single observation carrying both a location and free text. That is `leg_check.py`'s stated reading (its own TODO); the generated types are consistent with it, but SISO was not consulted.
- DEMO_READINESS row 15's remaining items: the READY line and the per-order summary are **DONE** in the shipped build (seen in G6's log); the row still reads O.

**Note on the record:** the brief referred to "C16 arrival-evidence / watchdog" in `DESIGN_ORBAT_TO_VRF_2026-09-06.md`. That document's CLOSED list **ends at C15** (arrival evidence); there is no C16 entry there. The watchdog lives as DEMO_READINESS row 19 and as the "C16" label in `feat/sim-clock`'s commits and `VrfSettings` comments. Worth reconciling when row 19 lands.
