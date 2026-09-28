# RULINGS - the ONLY place a ruling lives. Every other file points to an id and never restates the ruling.
# Format: RL-YYYYMMDD-NN | date the owner answered | status. Q = the question AS PUT. A = the OWNER'S OWN WORDS.
# status VERBATIM = Q and A both located, A inside a genuine owner-typed record. UNVERIFIED = searched, not found.
# Sources: L<n> = session a7f6a276-7ebc-4507-ac9d-c6bd361bd64e.jsonl; S<n> = session c3b364bd-a4ae-445a-b5c3-e585eaa5935c.jsonl (entry -09);
#   P<n> = session 5fc25950-1a10-4ade-9a7b-68cb5c1daf05.jsonl (entries RL-20260927-02 to -06, RL-20260928-01 to -03). Archived (cap): -08, 0914-02 on 09-26;
#   0925-01, 0921-05, 0926-01 on 09-27; 0927-01 on 09-28; 0921-09 on 09-28 (for -03).
#   All are the 1-based PHYSICAL line `rg -n` prints; the record shape is named where it is not a plain type=user record.
# Owner text is EXACTLY as typed, misspellings included. Only transliteration: U+2019 -> ' and U+00EA -> [e^].
# "[...]" elides; the operative clause is never cut. A "supervisor reading:" line is scope only and binds nobody.
# A selected AskUserQuestion label is the SEAT's wording, recorded as a selection, never as the owner's words.
# Cap 120 lines / 160 chars. Overflow -> RULINGS_ARCHIVE.md, same format, no cap. Index of ids at the archive head.

RL-20260927-02 | 2026-09-27 | status VERBATIM - P5906, TYPED
  Q (as put, P5695, the seat's A1 report; its "Two decisions for you before G1"): "1. Hostile side: RUS (107 units, branch-correct stand-ins for every
    hostile function, the vendor's own Baltic choice, and the map's default) or BLR (24, proxies). I recommend RUS. 2. Representation for the first
    aggregate runs: proxies (company/battalion-sized aggregates; they move, report and carry footprints) now, with populated containers as the later
    fidelity step; or build the container population first. I recommend proxies now."
  A (owner, P5906): "1. RUS. 2. Again a discussion Thats been has. Many sessions ago you said that "authoring" was the vendors standard way of dealing
    with units. I understood you were talking about the composition you just rediscovered. But your surprise makes me think the container thing might
    be a bit shaky. In any case, if this is real, then going back to populating the containers is what needs to be gone while we still remember it. You
    have the TO providing the composition, so it seems simpler than arbitrary proxies that will need to be changed later" (line break after "if this")
  supervisor reading: (1) hostile side RUS. (2) populate the containers NOW, before G1, instead of proxies - conditional on the container model being
    real, which UG52 72.2.1 p1419 confirms ("aggregate-level simulation objects that are configured to use the aggregate warfare model do not have the
    configuration options for specifying subordinates. Therefore, AggregateLevelBase.sms has a platform, Aggregate Container"). The TO (STP init) gives
    corps -> division -> brigade (15 Superior relations, 4 battalions); brigade -> battalion/company composition is NOT in it and must come from the
    catalogue's configured sub-units or an authored composition table (DESIGN_ORBAT_TO_VRF sec 5 (b)) - the source of that table is not decided here.
  pointer: PLAN_MOVEMENT_2026-09-27 rows C1, G1; AGGREGATE_CATALOGUE_2026-09-27; DESIGN_ORBAT_TO_VRF_2026-09-06 secs 1, 2, 5.

RL-20260927-03 | 2026-09-27 | status VERBATIM - P6030, TYPED, unprompted (restates C13 for the container work)
  Q (as put): none - the seat's preceding message (the RL-20260927-02 exchange) had said C1 populates containers.
  A (owner, P6030): "Another thing that is settled already is that the only units that need to be hydrated are the ones that are actually task. No
    reason to carry the whole tree if just a few units are actually meaningful for the simulation. I still want all units to show on the map on
    initialization though."
  supervisor reading: C13 restated for C1 (RL-20260906-02 "just 11 taskees ... the only ones that need to be simulated"; PREREG_ORDER_TIME_MATERIALIZATION:
    display at init, configure at order time): every unit is an empty container at init, visible at its authored position; only a tasked unit is populated.
  pointer: PLAN_MOVEMENT_2026-09-27 row C1; CreationPolicy=AtOrder (C13) in VrfC2SimService; PREREG_ORDER_TIME_MATERIALIZATION_2026-09-06 sec 2.

RL-20260927-04 | 2026-09-27 | status VERBATIM - P6260 TYPED, P6304 TYPED (mid-turn follow-up)
  Q (as put, P6251, the seat's C1 decision brief, its "Decisions owed" D-1..D-8 with a recommendation each; D-2 read: "Brigade depth: manoeuvre elements
    only, 17 units (recommended). The catalogue has no US engineer or support battalions; a full TO&E adds nothing that moves or fights." and the close:
    "If you take the recommendations as a block, 'as recommended' is enough and the code lane starts when E2 has finished.")
  A (owner, P6260): "As recommended. But clarify D-2 - "The catalogue has no US engineer or support battalions". But there are engineering tasks. Can't the
    units be authored even if not in the catalog?"
  A (owner, P6304): "Same for support and other missing from catalog"
  supervisor reading: D-1, D-3..D-8 as recommended (28ID as HQ only; company-level HQ; Mech CO for a US rifle company; performer only populated; withhold
    a short/vacuous container completion; TO twins display-only; flat containers). D-2 REVISED by him: compose to doctrine (FM 3-96), and AUTHOR the US unit
    types the aggregate catalogue lacks - engineer, support and any other - as new unit types (UG52 72.2 parameters incl. Engineering systems; 27.2.3;
    the aggregate breach/obstacle tasks exist) in the derived model set, from the catalogue's own engineer/support units of other nations (Engineering BN
    (POL), Engineer BN (RUS, Mech), Logistics BN (LTU), CSS CO (USA)). Sequencing (seat's, not his): G1 proves the container mechanism with catalogue units
    first; the authored battalions join the composition as package C2 lands.
  pointer: DESIGN_AGGREGATE_CONTAINERS_2026-09-27 secs 6-9; PLAN_MOVEMENT_2026-09-27 rows C1, C2, G1; tools/sms/Deploy-C2SimSms.ps1 (the recipe pattern).

RL-20260927-05 | 2026-09-27 | status VERBATIM - P6612, TYPED (mid-turn)
  Q (as put, P6535, restated P6584 - the RL-20260925-01 Q1 race the timer-anchor lane found: a task-clock step > 60 s between timed walks can skip a
    late predecessor's follow-ons): "Two ways to close it: (a) When the window expires, ask the timer directly: a predecessor with a destination that
    has not finished is treated as overdue, and the gate extends to the backstop. Small, and my recommendation. (b) Gate on the predecessor's own
    end-time signal instead of a separate margin. Cleaner, a larger change."
  A (owner, P6612): "Q1 a"
  supervisor reading: option (a) as put. The no-Duration mover (never OVERDUE, skipped at the floor) was named as outside Q1 and is not decided here.
  pointer: fix/gate-late-predecessor; RUNBOOK sec 11; TimerAnchorSelfTest t4; RL-20260925-01 (archive) Q1.

RL-20260927-06 | 2026-09-27 | status VERBATIM - P6635, TYPED (mid-turn, unprompted)
  Q (as put): none - the seat's rationale at P5335 stood as Y-15: "Entity level for company-and-below orders; aggregate level for battalion-and-above,
    or whenever the vehicle count makes the fixed-frame clock crawl. The profile is a fixture plus a type map plus a runner setting, chosen per order".
  A (owner, P6635): "On aggregate vs entity : consider the echelon threshold just for the units actually tasked, not the overall set placed at
    initialization but never acted upon. And I trust that the automated default can be overruled by a setting so that battalion and bow can be
    simulated at an aggregate level rather than entity. Higher echelons can only be run at aggregate level because of vrf limitations"
  supervisor reading: refines Y-15: (1) the model set is chosen by the highest echelon among the TASKED units (the order's performers), not the init;
    (2) a setting (Vrf:ModelSet / runner -ModelSet) overrides the automatic choice, so battalion-and-below may run at aggregate level too; (3) above
    battalion is aggregate-only (the entity catalogue stops at BN). "bow" read as "below". Package D2 builds the automatic selector.
  pointer: PLAN_MOVEMENT_2026-09-27 row D2; VRF_5.2_DECISION_EVIDENCE Y-15 (dated note); scripts/RunC2SimScenario.ps1 -ModelSet (A1).

RL-20260928-01 | 2026-09-28 | status VERBATIM - P7459, TYPED
  Q (as put, P7308 - the D2 lane's two side effects: "1. The runner's default Mojave order becomes aggregate-only ... 1.BdeHQ carries EchelonCode BDE
    ... 2. The cut-A entity-level command lines (E1, E2) are refused ... My recommendation on both: keep the rule as ruled, with no escape hatch. For (1),
    fix the test data, not the rule: 1.BdeHQ is a company-sized element and its code should say so (COY) ... For (2), accept it"; and P7452 - the G1
    push blocked by the permission classifier: "This is yours to authorize, not mine to work around. Options: 1. Allow it here ... 2. A fresh executor
    ... 3. Run step C3 yourself ... I recommend option 1: the server is the sanctioned private validation target")
  A (owner, P7459): "D2 as recommended. Go for the server"
  supervisor reading: (1) RL-20260927-06 stands with no downward override; the Mojave test order's HQ unit is re-coded below battalion in the TEST DATA in
    a way that keeps its current single-vehicle representation (the executor verifies with --parse-init), and cut A keeps no entity-level regression
    run - E1/E2 stay as records. (2) "Go for the server" authorises the private validation server push (127.0.0.1:18080 / 61614) for G1 and its
    successors; the operator's 8080/61613 server stays out of bounds as before.
  pointer: PLAN_MOVEMENT_2026-09-27 rows D2, G1; PREREG_IRONSTORM_AGG_G1_2026-09-28 sec 3 (C3); feat/modelset-selector.
RL-20260928-02 | 2026-09-28 | VERBATIM
  Q (seat, P8236): the seat reported C1c merged - "member names are built within the 30 characters VR-Forces keeps, every requested name is
    checked for uniqueness at 30 before the create" - and G1-2 being registered on it.
  A (owner, P8241): "Looks like you fell again on a very old trap and are rediscovering the name limit. Look at the record!!!! This field is
    supposed to carry the uuid not the human name"
  supervisor reading: object identity is the UUID field (UG52 13.2 Table 21 p362-363: UUID unique; Name length-limited, NOT unique; Label
    unlimited). The interface passes a startingUUID on every create (the unit's C2SIM uuid - already done for every tactical graphic; G5 of
    ORBAT_LOADING_REQUIREMENTS_2026-09-06 named this fix for units and its refutation called it optional - reversed) and correlates
    ObjectCreated by uuid; members get a derived uuid; names are display only. C1c (84c4f62) is demoted to a secondary key; C1d does it.
  pointer: PLAN_MOVEMENT_2026-09-27 rows C1c, C1d, G1-2; CORRECTIONS_LOG "Name-keyed identity" (2026-09-28); UG52 13.2 p362-363.
RL-20260928-03 | 2026-09-28 | VERBATIM
  Q (seat, P9695, after the owner's P9693 "What does the documentation say for regions with no roads. Are you basing the default to use
    roads just on the Kaunas sample?"): "no global default. An AUTO mode decides per leg the way the manual tells an operator to: every
    member plans with obstacleQuery MAK_OBSTACLE and a 10 m buffer regardless [...]; pathQuery MAK_ROAD when the OSM road network lies
    within a proximity setting of the leg's line (default 500 m, from the manual's 'fairly wide area'), otherwise NONE [...]. Your
    ruling then confirms AUTO as the default or picks an explicit mode"
  A (owner, P9739): "AUTO it is"
  supervisor reading: a tasked container moves by the vendor's PLANNING tasks per STP vertex (FINDING_AGGREGATE_MOVEMENT_OBSTACLES
    _2026-09-28; UG52 35.5.11 p735, 30.5 p587): per member navigate-to-location, obstacleQuery MAK_OBSTACLE, buffer 10 m, pathQuery
    MAK_ROAD where roads lie within Vrf:RoadProximityMeters (500) of the leg, else NONE; Vrf:AggregateMovePlanner=Auto is the default;
    Group, PerMemberOffRoad and Literal stay selectable for registered comparisons; the pre-flight is report + fallback; the first
    registered run is G1-3 on T14's line. Y-11/Y-13 (entity-level Ignore Roads) are untouched.
  pointer: PLAN_MOVEMENT_2026-09-27 rows M3, G1-3; feat/aggregate-planned-move; appsettings.json _AggregateMovePlanner.
