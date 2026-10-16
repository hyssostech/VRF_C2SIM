# HANDOFF - VR-FORCES EFFORT PARKED (2026-10-16)

STATUS: PARKED by the operator on 2026-10-16 (relayed by the STP-1192 supervisor). This week's capacity goes to the
STP COA renderer laydown and to a new effort. Nothing here is running. Nothing is owed until the effort is resumed.
Written by Worker R (STP-1192). Measurements and design implications are kept in separate sentences (CLAUDE.md sec 4).
Roles and counts only. The C2SIM order and init derive from a customer scenario, so no names or coordinates appear here.
Code refs are at feat/aggregate-documented-move 7467e53a (the #2 merge, the build LV-1 and LV-1b ran) unless stated.

## 1. Where things stand

- Branch: feat/aggregate-documented-move. The STP-1210 fix (#2) is merged into it, and its main port (#3) is merged into main.
- Ledger: `*** NEXT FREE: 5557 ***` (docs/OPUS_EXECUTION_PLAN.md Appendix B). LV-1 used 5544-5552; LV-1b used 5553-5556.
- The day holder that LV-1/LV-1b reused (RtiProbe, joined 12:05:14Z, settle 28800 s) expires on its own. Never kill it.
- Jira: STP-1192 carries the live-run records:
  - c14384 is the LV-1 PREREG;
  - c14386 is the LV-1 RESULT;
  - c14387 is the LV-1b amendment;
  - c14392 is the LV-1b RESULT;
  - c14393 closes the AV-s3 route-count correction.
- STP-976 carries L2, LD-21 and LD-22 (c14318-c14333).

### LV-1 (VR-Forces attempt 3, GUI on) - STP-1192 c14386

- Measurement: the MEMORY STOP (F6) fired at 12:27:57Z, before T0. Free memory went from 6.23 to 5.40 GiB within about 2 WALL min of the interface start. No order was pushed.
- Passed before the stop: PRE, k1 (exe git.7467e53; routeextent 80/0, rulings 778/0), k2, k5 and G1 (27 of 27 bound in 23 WALL s; 0 report refusals).
- Working sets at the stop (GiB): VR-Forces GUI 4.10, back end 3.18, WSL VM 2.82, Pro 2.40.
- Records: stp-data c5a6d22; ledger 8701a822 + 7eae5d9.

### LV-1b (attempt 3b, -NoGui) - STP-1192 c14387 (amendment), c14392 (result)

- Measurement: G2 FAILED (no "C2SIM Order received" by T0 + 60 WALL s; T0 13:04:41.655Z), and F5 fired. From about T0 the server refused every position report push ("Error processing message"): 8,370 failures by the reset. The STP engine received 0 reports after T0.
- Passed: PRE, k1, k2, k5, the init discriminator, and G1 (27 of 27 bound in about 12.7 WALL s, all 27 placed from the terrain query, 0 refusals). k3 is IDENTICAL to LC-1 except OrderBody/IssuedTime.
- Memory (free GiB): 13.55 at VR-Forces READY, 10.56 after Pro, 8.94 after G1. The MEMORY STOP did not fire.
- Teardown: the interface did not resign (it was deaf after the server reset). It was left running and later ended by the operator's update session.
- Records: stp-data d59e0f61 (server logs) and aa3b008 (run records); ledger 9aa2767.
- c14393: AV-s3's graphic is an init Point with 3 Locations. The interface registers Points[0] only (InitModels.cs:78-79; VrfC2SimService.cs:2175-2182), so routeGeo = 2 points, as c14384 stated.

## 2. The order-push failure (LA-1)

LA-1's CLAIM (a box analysis; UNREVIEWED at the time of writing, so a hypothesis):
- The C2SIM server's STOMP client declares content-length = Java chars + 1, but writes UTF-8 bytes.
- The 10-16 order carried 4 non-ASCII characters in one task Name, plus STP's echo of it in an XML comment.
- The broker rejected the frame and closed the server's publishing connection, so nothing was published after it.
- Since STP #466 (2ac981bc, 10-15) the agent wire is UTF-8, so non-ASCII text now reaches the export.

LA-1R VERDICT (cold review): [SLOT - filled when the supervisor relays LA-1R]

Questions LA-1R was asked:
- (1) Does the server parse and RE-SERIALIZE an order before publishing it? If it does, an STP-side ASCII escape does nothing.
- (2) Did any non-ASCII init ever publish fine?
- (3) Is the report path exposed too? vrf_c2sim's reports carry init unit names (ReportBuilder.cs uses XmlSerializer).

STP-side fix (STP 5.11.1; design APPROVED, build NOT started):
- What: one AsciiXml.Escape at the two push choke points in NallSuite/Agents/C2SimBridge/C2SimXmlBuilder.cs, PushInitialization :192 and PushOrder :215 (STP main b03db4e1). It covers both built and raw content.
- Text and attribute values get &#xH; per Rune. Comments get the same text, which is not equivalent and is documented. The "utf-8" declaration is kept.
- Measured (local .NET 10): XmlWriterSettings{Encoding=ASCII} over a TextWriter is IGNORED. Over a Stream it escapes, but it throws on non-ASCII in a comment.
- Build gate: LA-1R confirms AND the supervisor says go. If LA-1R finds the server re-serializes, stop and rethink.

## 3. OPEN decisions (owner or operator)

- D114(a): GROUP-TASK REDESIGN of container movement.
  - Measurement: 5 of 6 re-tasks at the stop place stopped the back end (REPRO_BACKEND_STOP:36-42, as VD-1 cites it). In L2, Auto stopped (A1) and PlanPath ran clean in 2 arms (L2_RESULT_2026-10-16.md:28, :42-48).
  - Vendor: UG p551 (group planned movement computes member destinations itself) and p561 (a new movement task replaces the current one), as read in docs/experiments/FINDING_VENDOR_DOCS_VD1_2026-10-16.md:251-255.
  - Implication (VD-1): the vendor's group-task path is the documented operator path, and it has not been tried on the stop recipe. It needs an owner RULE and a PREREG before any run (VD-1:269).
- D112 / LV-2: a LARGER DEMO EXTENT.
  - The prep (draft v2, held, never posted) covers a box-size method, the extent override (Vrf__DemoExtent env for the interface only, not a branch commit), the cache warm-up, a PREREG skeleton and a memory budget.
  - Measurement: D36(b) at 3,300 km2 FAILED (D36_BACKEND_STOP_AND_WARM_CACHE_2026-10-15.md:107-113). D36 re-tasked POPULATED members on the same planner family (Auto); no run exists at 1,000-2,000 km2.
  - Memory gate, LV-1b numbers: 8.9 GiB free at T0, against the 6.0 GiB MEMORY STOP, leaves 2.9 GiB of headroom. The prep's gate needs at least 3.5 GB so that the rate abort (700 MB/min over 300 s) fires before the memory stop.
  - Implication: by that gate LV-2 is NOT RUN, unless about 0.6 GiB more is freed or the abort window is shortened. That choice is the operator's.
  - VD-1 adds: "Feature data is not cached" (AC p239; VD-1:136, :177, :256-258). Its ranked item 2 is to hold the extent and raise the feature-pager knobs with MAK.
- AIR-CONTAINER NOTE (a hold item, not a claim):
  - Code fact: StartContainerPlannedMove is reached for any RouteTask on a container with a planned planner, with no isGround test (VrfC2SimService.cs:6440-6443; VertexChain.cs:91-93). So an air container takes the ground per-vertex planned move.
  - Owed: a vendor-doc check of how an air aggregate is meant to be moved, before this becomes anything.

## 4. PARKED items

- LV-2 (D112), above. Its draft is held by Worker R and was never posted.
- LD-22a, the offset-goal water check (STP-976 c14323, amendments c14330 and c14333):
  - IssuePlannedVertex passes each per-member offset goal through a cache-only CheckSlot overload;
  - the self-test is B1's 17 points (PASS = exactly the 4 water goals move, plus building-clearance moves that are listed);
  - LD-22b is the confirming live run.
  - It was blocked by the run-and-record profile; the vrf-dev profile is now applied, so building is unblocked. Not started.
  - The cold-cache gap (c14330) is open: the tiles under offset goals 400-500 m out are not guaranteed to be cached before dispatch.
- D114(c), questions for MAK (VD-1:271-289):
  - is navigate-to-location supported when sent straight to an aggregate's subordinates, including a re-send mid-plan?
  - what does the PA suffix on the Echelon Group containers mean?
  - does the online aggregate terrain give the sim engine building geometry?
  - what bounds the feature pager's memory?
  - can the GUI run on a second host?
  - why do init elevation queries time out?
- Also open, from L2: per-member instantaneous speed (terrain cap vs path inflation) is not yet measured (STP-976 c14278, c14318).

## 5. RESUME steps (in order)

1. C2SIM server: D106, OPERATOR ONLY. The operator copies the server logs, restarts c2sim-server-vrf and ends any stale interface.
   - Measurement: the c2sim-server-vrf publishing leg has been dead since LV-1b, and it stays dead until a restart. The container holds about 1.5 GiB.
   - After LV-1b the server was reset to UNINITIALIZED. LA-1 claims its publishing connection was closed (sec 2).
   - No session restarts or edits c2sim-server-vrf. Never touch stp-lt (9500-9599) or 19575.
2. STP-side fix: check whether the ASCII escape has merged (sec 2) and whether the STP build in the engine image carries it.
   - Until it has, an order carrying non-ASCII text can reproduce the LV-1b failure, if LA-1 holds.
   - Check k3 for non-ASCII bytes before T0.
3. Memory gate:
   - VR-Forces runs -NoGui by default (standing operator ruling, 10-16); 4 launch numbers per launch (launch holder, its +1 retry, back end, interface);
   - free memory at least 7.0 GiB before StartInterface52 (hold up to 15 WALL min, then the supervisor decides);
   - MEMORY STOP below 6.0 GiB; stop sonar-local and close finished windows first (D86).
4. Approvals profiles that exist on the box:
   - vrf-demo: run and record, now with the -NoGui LaunchVrf52 line (D113); guard-config refuses -NoGui mixed with -FrontendAppNumber;
   - vrf-dev.json: the D100 source-edit profile (applied 10-16; 79 allow / 39 deny), needed for LD-22a.
   - Every vrf-demo command goes through the Bash tool, in its exact allow-line form (c14387 item 6).
   - An extent override (LV-2) needs a NEW narrow allow line for Vrf__DemoExtent; none exists.
5. Re-run LV-1 as LV-1c: c14384 with the c14387 amendment, a fresh ledger number, and Reports On only after G2 passes (c14392 departure). Then LV-2 only if its memory gate passes.
6. Before any 5.2 behaviour work: read CLAUDE.md sec 1-3, docs/VRF_5.2_MIGRATION_DIFF.md, docs/VRF_5.2_DECISION_EVIDENCE.md and VD-1.
