# RULINGS - the ONLY place a ruling lives. Every other file points to an id and never restates the ruling.
# Format: RL-YYYYMMDD-NN | date the owner answered | status. Q = the question AS PUT. A = the OWNER'S OWN WORDS.
# status VERBATIM = Q and A both located, A inside a genuine owner-typed record. UNVERIFIED = searched, not found.
# Sources: L<n> = session a7f6a276-7ebc-4507-ac9d-c6bd361bd64e.jsonl; S<n> = session c3b364bd-a4ae-445a-b5c3-e585eaa5935c.jsonl (entry -09);
#   P<n> = session 5fc25950-1a10-4ade-9a7b-68cb5c1daf05.jsonl (entries RL-20260926-01, RL-20260927-01, -02). Archived (cap): -08, 0914-02 on 09-26;
#   and 0926-01 on 09-27.
#   All are the 1-based PHYSICAL line `rg -n` prints; the record shape is named where it is not a plain type=user record.
# Owner text is EXACTLY as typed, misspellings included. Only transliteration: U+2019 -> ' and U+00EA -> [e^].
# "[...]" elides; the operative clause is never cut. A "supervisor reading:" line is scope only and binds nobody.
# A selected AskUserQuestion label is the SEAT's wording, recorded as a selection, never as the owner's words.
# Cap 120 lines / 160 chars. Overflow -> RULINGS_ARCHIVE.md, same format, no cap. Index of ids at the archive head.

RL-20260921-09 | 2026-09-21 | status VERBATIM - TEMPORARY, BY HIS OWN WORD
  Q (as put): none - volunteered after the seat said completion rules were under his review pending doctrine and vendor
    research. Record shape: session c3b364bd line 536, a mid-turn owner message (type=attachment, queued_command,
    kind=human) - there is no type=user copy of it, so a type=user-only search would miss it.
  A (owner, S536): "And the doctrinal and vendor doc research is on you buddy - not me. For now, pending this research,
    let's take the temporary position that completion is based on  start time + duration. Tasks involving units may still
    arrive late. If they arrive after the expected start time + duration, they complete immediatelly. Follow-on tasks still
    are permitted to take their whole specified duration, even if they were forced to start late. Mark this is as
    temporary, as a measure to let us move forward on this until we have better data."
  A (owner, S569, typed, correcting an earlier supervisor reading of this same entry): "On task completion [...]: "a unit that never
    arrives" - isn't there ruling already for units that get stuck? That's the only way a unit can "never arrive". "patrol and
    follow" - yes, there's plenty of tasks that involve no movement, as discussed repeatedly. And there is ruling for thoise as
    well -  they are completed when their duration elapses. "an effect that has not been achieved by end time" - it is uncertain
    whether there is a way to ascertain the effect - that is the thrust of the research. For this temporary ruling, we ignore that
    part and complete based on time. Again it is bizarre that you bring this up given the discussion. [...]"
  supervisor reading: every task ends at start time + Duration; a unit still travelling at that moment is reported complete when it
    arrives; a unit that gets stuck is already ruled (RL-20260914-01, archive: abort is the code STP sees; that a later arrival still
    reports complete is a supervisor position, see that entry's scope note); tasks with no destination end when their Duration elapses
    (RL-20260914-02); whether an effect was achieved is IGNORED under the temporary position - that is the research question, not a gap;
    each follow-on task gets its full Duration from its actual start. The research is the seat's job.
  IMPLEMENTATION FACT (supervisor note, updated 2026-09-25): until the completion unit (branch feat/completion-temporary-position,
    docs\CORRECTIONS_LOG.md F-4; live confirmation owed) main ended a task on its Duration timer even when the unit had not arrived
    (since 746c091) and stall detection shipped OFF. That unit builds this position; stall detection is ON in the demo profile and OFF
    elsewhere (RL-20260925-01), so outside the demo profile the stuck-unit ruling acts only once it is switched on.
  pointer: docs\experiments\TASK_COMPLETION_RESEARCH_2026-09-21.md (the per-verb completion table; research only, no recommendation).

RL-20260927-01 | 2026-09-27 | status VERBATIM - P5338, one TYPED word
  Q (as put, P5321 - the seat's decision brief, requested by the owner at P5314 (last-prompt record) "Restate the proposed approach given the facts above";
    its closing block): "Decisions: (a) This sequence: entity-level fix and run first, aggregate profile behind it. (b) Move To per vertex for lone
    platforms, reversing the 2026-09-07 withdrawal. (c) The pre-flight port with per-profile rules. On your go, this becomes the plan document and the
    first registered run is drafted." The brief's body (P5321) is the approach: one pre-flight for both profiles (STP vertices and legs checked against the
    OSM water and buildings the sim reads; bad vertices nudged and reported; wet legs detoured or reported; rivers reported as STP authoring defects);
    lone platforms driven by Move To per STP vertex, units unchanged; the aggregate-level profile for Iron Storm per PLAN_AGGREGATE_LEVEL_PROFILE_2026-09-06;
    hand waypoints (i)(j)(k) and the corridor launch bar retired once live-proven. Between Q and A (P5327-P5337) the seat answered "What's the thinking
    about when aggregate level and entity level are used" - explanation only, no new question.
  A (owner, P5338): "Go"
  supervisor reading: "Go" approves (a), (b), (c) and the sequence as put. It leaves RL-20260921-09, RL-20260913-03 and RL-20260914-01 as they are (the brief
    kept them) and does NOT decide the hostile-side aggregate mapping (RU 4 vs BLR 31 vendor types), which the aggregate lane puts back to him.
    [CORRECTED 2026-09-27, seat's own premise: the aggregate catalogue has RUS 107 / BLR 24 simulated units; "RU 4" counted a label spelling
    (AGGREGATE_CATALOGUE_2026-09-27; CORRECTIONS_LOG). The decision itself is unchanged and still his.]
  pointer: docs/PLAN_MOVEMENT_2026-09-27.md; docs/experiments/FINDING_GROUND_MOVEMENT_PRACTICE_2026-09-27.md secs 5, 7, 8; branches
    feat/movement-moveto-per-vertex, feat/preflight-osm-features, feat/aggregate-profile.

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
