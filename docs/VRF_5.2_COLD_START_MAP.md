# Cold-start roadmap vs work accomplished (2026-09-03)

A clean-room Opus agent was given the requirements (C2SIM in, VR-Forces 5.2d
runs headless, telemetry verifies, one button), the 5.2d install, the vendor
docs and the C2SIM SDK - and NOTHING from this repo. Its deliverable is
archived verbatim in VRF_5.2_COLD_START_ROADMAP.md (776 lines). This file maps
each of its phases, decisions, predictions and traps to what already exists,
adjudicates the divergences, and lists what is genuinely new. Every "verified"
below was checked on disk this session, not taken from the agent.

Legend: DONE = exists and was exercised on 5.0.2 (nothing is yet exercised on
5.2d); PARTIAL; OPEN = not done; DIVERGES = the agent chose differently;
WRONG = the agent's claim fails against evidence.

## 1. Vendor grain (cold-start sec 1) - agrees with VRF_5.2_DECISION_EVIDENCE sec 0

All five grain statements (entity intelligence, one exercise-connection file,
streaming terrain + block knob, remote controller as federate, tasks-as-data)
match the evidence doc independently. Two agents reading the same docs blind
to each other reached the same direction: treat sec 0 as settled.

## 2. Architecture (cold-start sec 2) - same skeleton, three real divergences

| Cold-start choice | Ours | Status |
|---|---|---|
| A: C++ remote-control shim as control plane | VrfBridge.dll over VrfFacade (DtVrlinkVrfRemoteController + DtExerciseConn) | DONE - same choice, reached independently |
| B: C# telemetry plane on vrLinkSharp, separate federate | tools/WatchVrf joins as its own federate but through VrfBridge, not vrLinkSharp | PARTIAL - process/federate independence yes, code independence no. Acceptable: the report path and the reflected-entity path are different MAK subsystems even inside one DLL |
| D: batch mode as regression harness | rejected (UG52 7.10: read-only; cannot create or task) | DIVERGES - the agent proposes it only for pre-placed scenarios with embedded plans; that is a different product (see 2.3 row 7) |
| Direct vrfSimHLA1516e launch, no launcher | scripts/LaunchVrf.ps1 launches bin64 directly | DONE |
| Own copy of the exercise-connection XML, per-run execName | shared C:\MAK config, appNo ledger (NEXT FREE) for federate identity | DIVERGES - see 5.c |
| Custom offline .earth from N34W117 | Y-7 candidates all online (MAK Earth (online) ruled) | NEW - see 5.a |
| EntityLevel.sms include-and-extend | data/unit-type-map.json names shipped 5.2d templates; no custom SMS yet | DONE for shipped types; the extend half is the PRC-authoring item (5.g) |
| Task the unit; company Move To -> Maneuver To; move-along -> maneuver-along | Y-10: keep moveAlongRoute (adapter repackages it as maneuver-along, disaggregatedMoveAlongAdapterController.h :9) | DONE / same conclusion |
| requestTasksAndSetsFor startup assertion | not implemented | NEW - see 5.d |
| Plan callbacks for task completion (addPlanCompleteCallback, vrfRemoteController.h :1808) | report-category callback -> DtTaskCompleteReport (VrfFacade.cpp :404-409); 3/3 TASKCMPLT on R9 | WRONG as stated ("Tasks section has no completion callback") - the report path works today. Plan callbacks are a valid CROSS-CHECK, not a replacement (Phase 4 note) |
| FFRTC + fixed frame time + seed + multiplier 1 | fixed-frame-run-to-complete adopted in FixtureGen; TimeMultiplier 1x is law; seed pinning NOT done | PARTIAL - seed pinning is a one-line fixture item, add to Phase 2 |
| Own rtiexec per run on a per-run UDP port; teardown = process baseline | rtiAssistant-managed shared RTI; NEVER kill rtiexec/rtiForwarder; teardown NOT solved (2 of 6 failed; relaunch wedges rtiForwarder) | DIVERGES - see 5.c |

## 3. Phases (cold-start sec 3) -> what we have

| Cold-start phase | Ours | Status / evidence |
|---|---|---|
| Prototype zero: prebuilt examples\remoteControl\build64\RelWithDebInfo\remoteControlHLA1516e.exe drives a shipped scenario, zero code | never done; we always ran our own facade | OPEN, ADOPT - a vendor binary that reproduces a symptom proves it is not our bug; cheapest instrument in the kit. Insert before Phase 2 PREREG_R9_52 |
| 0 Environment truth (licence, paths, processes) | licence expires 2026-09-15 (memory); process inventory at session start (memory); 5.2d install verified complete with data (a4d4ec9) | DONE |
| 1 Headless boot + join, teardown to baseline | LaunchVrf.ps1 + AnswerRtiDialog.ps1 + runner Stage 2b watcher; join gated on serviceable readiness | PARTIAL - boot/join DONE on 5.0.2; TEARDOWN OPEN (memories vrf-launch-procedure, vrf-teardown-relaunch-wedges-rti, rti-fresh-boot-join-race). 5.2d rtiAssistant re-check pending (plan Phase 2) |
| 2 External control moves a vehicle | R9: 6/6 creation lines, routes by uuid, 3/3 TASKCMPLT (HANDOFF_2026-09-01_R9_COMPLETE) | DONE on 5.0.2; on 5.2d it is plan Phase 2 with the 7-field objectType fix first |
| 3 Telemetry plane in C# | WatchVrf (positions, placeholder encodings), tools/analysis/run_census.py, frame_gaps.py, phase_timing.py | DONE on 5.0.2; re-baseline on 5.2d before any claim (plan Phase 2) |
| 4 Order compiler (C2SIM -> tasks) | src/VrfC2SimApp (parse, type map, verbs, routes, sequencing, reports, STOMP); private server c2sim-server-vrf 18080/61614 | DONE - the agent's Phase 4 is our whole C# layer |
| 5 Units and fidelity | 30/31 types resolve on 5.2d once the resolver accepts 7 fields; AR Scout / Mobile Irregular / Mobile Light Infantry gaps; PRC units absent (5.g) | PARTIAL |
| 6 Repeatability (FFRTC, seed, A/B) | FFRTC adopted; ratio 9x on R9, 0.27x on COA-STP1; Tank-Company non-determinism SUPPORTED, -q FALSIFIED (5c1a76e); VRF-8968 is the 5.2 hypothesis | PARTIAL - seed not pinned; 5.2d re-adjudication is plan Phase 3 |
| 7 One button | scripts/RunC2SimScenario.ps1 + RunnerLib.ps1 (launch, fixture deploy, push init/order, watch, stop) | PARTIAL - blocked by the teardown gap only |

## 4. Predictions and traps worth carrying

Predictions: P1 (join + control on a shipped scenario with zero code) is the
prototype-zero gate; P3 (moveToLocation gone) already settled by Y-10; P5
(Maneuver To keeps the company tighter than Move To) is measurable with
run_census's sub-route/spread table and belongs in the Phase 3 prereg as a
secondary metric; the rest restate sec G rows.

Traps that are new to our record (the others are already memories):
- T5 Data Logger refuses a second batch recording (UG52 7.10.5) - moot unless
  batch is ever used; recorded so nobody rediscovers it.
- T10 "sessionId must match the sim engine's" - already in the DIFF doc; the
  agent adds that a mismatch is SILENT (controller joins, sees nothing).
  Prototype zero checks this for free.
- T14 vrfSim.log now defaults to C:\MAK\logs (known) - the agent notes the
  file is truncated per launch, so copy it into the run folder BEFORE relaunch
  (runner Stage change, cheap).
- T18 requestTasksAndSetsFor returns empty until the back end has published
  its capabilities - so the startup assertion must poll with a bound, not
  fire once.

## 5. Genuinely new items - candidates for sec G (user decides; nothing acted on)

- 5.a Y-7 option (c): offline .earth built from the shipped USGS tiles
  SharedData\19\latest\TerrainData\Terrain\California\Elevation\N34W117.dem
  (27,264,000 bytes) + Imagery\N34W117.tif (verified on disk; no shipped .earth
  references them). Tile covers 34-35N / 116-117W: the whole R9 box and most of
  the COA order box (west edge -117.01 marginal), NOT the COA init positions
  (33.70-36.18N, -117.80..-115.17W). No roads either - irrelevant for tracked
  vehicles on 5.2d (Ignore Roads default) but not for wheeled ones. Value:
  removes the vr-theworld tile cache from the determinism budget. Under
  blockOnAsynchronousOperations (Y-9) online terrain is content-deterministic
  too, so the gain is reproducibility across MONTHS (server-side tile updates),
  not across runs. Recommendation: stay with MAK Earth (online) for Phases 2-3;
  keep (c) as the R9-class fallback if a Phase 2 terrain-profile gate fails.
- 5.b Prototype zero (sec 3 row 1). Recommendation: ADOPT as Phase 2 step 0.
- 5.c Per-run rtiexec on a per-run port with a per-run federation name. The
  agent cites the RTI Users Guide; our record says the assistant-managed RTI
  wedges on relaunch and races on fresh boot. A self-run rtiexec per run is a
  docs-backed alternative to an UNSOLVED problem, not a proven fix. Needs the
  MAK RTI Users Guide fetch (already queued) and its own prereg. Do not touch
  the running assistant model until then.
- 5.d requestTasksAndSetsFor startup assertion (bounded poll, T18): every
  task/set name the type map emits must be in the back end's advertised list
  before the first order is pushed. Cheap, catches renamed tasks at t=0
  instead of mid-run. Recommendation: ADOPT in Phase 1 (C# + one facade call).
- 5.e Plan-complete callback as a cross-check of the report path (Phase 4).
  Recommendation: record only; the report path is working.
- 5.f Scenario-embedded laydown: FixtureGen already writes the .scn; the agent
  would also embed the initial units (from the C2SIM Initialization) and their
  plans, so the sim starts with everything in place and no create-then-task
  race (VRF-8968). This changes the product shape (orders become scenario
  edits, not runtime commands) and conflicts with THE GOAL's "C2SIM in" being
  live. Recommendation: reject for the interface; permissible as a Phase 3
  A/B instrument only if VRF-8968 survives on 5.2d.
- 5.g PRC units: 0 unit templates in EntityLevel (both versions), 7 in 5.2d
  AggregateTacticalLevel (MECH CO PA / Mech HQ SEC / Mech PLT (CHN, ZBL),
  Rocket PLT (CHN, DF-21 / YJ-62)), 19 PRC platforms; RUS has 34 EntityLevel
  units. UNIT_TYPE_MAPPING_FIDELITY already recorded zero PRC units on 5.0.2;
  what is new is that 5.2d changes nothing at EntityLevel (our level) and its
  "no PRC aggregate content" note is refuted by the 7 CHN AggregateTacticalLevel
  units. The "both full" ruling therefore still means: PRC entity-level UNITS
  are authored by us (include-and-extend SMS) - an authoring queue item, not a
  catalog pick. User call on when.
- 5.h DIS as a diagnostic instrument (DIS exercise in parallel is cheap and
  RTI-free). Record only; HLA is the product.

## 6. What the clean perspective confirms

The agent, blind to two months of history, arrived at: the same native
skeleton, the same task family (maneuver-*), the same terrain product, the
same determinism knobs, the same "task the unit not the vehicles" rule, and
the same 5.2 pitfalls (7-field types were not in its scope; it found the
sessionId, log path and launcher traps). Nothing in the accumulated work is
against the grain. Its additions are instruments (prototype zero, the startup
assertion, per-run rtiexec) rather than redesigns. Its one error is an
"absence" claim (no completion callback); its PRC census refuted one of OURS
("5.2 adds no PRC aggregate content"). Same failure class as the earlier
"no MAK Earth Space successor" and "myHlaConnection compiles" claims: a
presence or absence must be checked on disk before it is written down.
