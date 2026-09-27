# RULINGS - the ONLY place a ruling lives. Every other file points to an id and never restates the ruling.
# Format: RL-YYYYMMDD-NN | date the owner answered | status. Q = the question AS PUT. A = the OWNER'S OWN WORDS.
# status VERBATIM = Q and A both located, A inside a genuine owner-typed record. UNVERIFIED = searched, not found.
# Sources: L<n> = session a7f6a276-7ebc-4507-ac9d-c6bd361bd64e.jsonl; S<n> = session c3b364bd-a4ae-445a-b5c3-e585eaa5935c.jsonl (entry -09);
#   P<n> = session 5fc25950-1a10-4ade-9a7b-68cb5c1daf05.jsonl (entries RL-20260926-01, RL-20260927-01). Archived (cap): -08, 0914-02 on 09-26; 0925-01 on 09-27.
#   All are the 1-based PHYSICAL line `rg -n` prints; the record shape is named where it is not a plain type=user record.
# Owner text is EXACTLY as typed, misspellings included. Only transliteration: U+2019 -> ' and U+00EA -> [e^].
# "[...]" elides; the operative clause is never cut. A "supervisor reading:" line is scope only and binds nobody.
# A selected AskUserQuestion label is the SEAT's wording, recorded as a selection, never as the owner's words.
# Cap 120 lines / 160 chars. Overflow -> RULINGS_ARCHIVE.md, same format, no cap. Index of ids at the archive head.

RL-20260921-05 | 2026-09-21 | status VERBATIM
  Q (as put): none - unprompted correction. The seat's immediately preceding messages are L45432 and L45454.
  A (owner, L45457): "The rulling based on time is for tasks that do not include movement, such as defend in place and
    similar ones - units that are not expected to reach any other location. That's what you asked. "A unit that moved 60 m
    of a 5 km leg gets a complete" is a new interpretation. If I agreed with that, I was tricked. All along this project
    there was an intense focus on examining situations in which units failed to reach their destination, for example because
    they got stuck in unpassable terrain at some point. The notion that geting stuck midway is a complete is completelly
    illogical. There is also a ruled nuance - some tasks never report completed, and yet they make it to their
    destination - there is code (or should be as a result of the ruling) to adopt complete as the outcome even if the sim
    misses the notificaiton. Arriving late does _not_ imply an abortion - what happens is that follow on tasks are delayed
    by the slow progress on a leg. [...]" (the message ends mid-sentence, on the word "Also")
  supervisor reading: read WITH RL-20260921-08 (he calls the movement / non-movement split fuzzy) and, above all, with RL-20260921-09,
    the TEMPORARY position that now governs completion until the doctrine and vendor research is done. What this entry on its own
    settles is narrow: a unit stuck midway has not completed, and arriving late is not an abort.
  pointer: the HANDOFF CLOSED list TASK COMPLETION line; docs\CORRECTIONS_LOG.md F-1; RL-20260921-07 and -08; RL-20260921-04 (archive) for
    the one question he asked back inside the STP-857 exchange, which nobody has answered.
  Lane B's pointer, kept as written and not deleted: "NOT RULED here and not to be written as settled either way: movement-then-hold,
    patrol, follow/escort, successors of a stalled move, any bound on a late mover, watchdog default." SUPERSEDED IN PART 2026-09-21 by
    RL-20260921-09 (temporary position): patrol, follow/escort and every no-destination task end at their Duration; a never-arriving unit
    is a STUCK unit (RL-20260914-01).

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

RL-20260926-01 | 2026-09-26 | status VERBATIM - P3272: 2 SELECTIONS + 1 TYPED; P3645: 1 SELECTION; P3694, P3775: 1 TYPED each
  Q1 (as put, P3271 AskUserQuestion; echoed in the P3272 tool_result, type=user): "ATTACK: an attack is an operation ending on the objective, not a single
    fire task; STP always sends the performer as target plus the objective graphic ('the target is the objective' - your ruling). VR-Forces has no
    entity-level attack task, and a unit cannot take Fire At. What should a unit ATTACK do beyond advancing to the objective?" Options (the seat's labels):
    Advance + rules of engagement 'fire at will' (Recommended) / Advance only, as today / Suppressive Fire at a named target when one exists.
  A1 (owner SELECTION of the seat's label, 2026-09-26): "Advance + rules of engagement 'fire at will' (Recommended)"
  Q2 (as put, P3271): "Doctrine answer: BREACH is a combined-arms task against an OBSTACLE, normally at task-force level, needing engineers/plows [...] The
    failing 'BREACH on a unit' only exists in our own synthetic test order (enemy company as target); STP never sends a target other than the performer, and
    for that case the interface already just advances. [...] For STP's under-specified BREACH, what should the interface do?" Options (the seat's labels):
    Advance, report 'breach not simulated', complete by time (Recommended) / Refuse with a clear status.
  A2 (owner, P3272, TYPED in place of a selection): "As recommended, but open a Jira item on the STP project to fix the breach task according to doctrine. But
    look into your synthetic order as well. I don't think it is doctrinal to latch on a unit as you are doing. Tasks are usually against objectives, or
    reference other tgs instead of specific enemy units as far as I know. Why aren't you using the orders from iron Storm narrative 1, generated by stps coa
    renderer?"
  Q3 (as put, P3271; echoed in P3272): "The research also found an STP export defect: any task STP cannot map is exported as ATTACK, and 'follow and support'
    is probably one of them (a string mismatch: FOLLOW_AND_SUPPORT vs 'follow and support' in C2SimTask.cs). File it on the STP side (Jira)?" Options (the
    seat's labels): Draft the Jira issue for me to review (Recommended) / Not now.
  A3 (owner SELECTION of the seat's label, 2026-09-26): "Draft the Jira issue for me to review (Recommended)"
  Q4 (as put, P3637 AskUserQuestion; echoed in the P3645 tool_result, type=user): "STP sends 'hold fire' (ROEHold) on every task: all 42 COA-STP1 tasks, all
    23 Iron Storm tasks, all 5 in cut A. Your attack decision sets a unit's rules of engagement to fire at will, so for a unit ATTACK the interface now
    overrides the order's hold-fire (the log names the override). STP's hold-fire looks like a default rather than a planner's choice, but it is what the
    order says. Which wins for a unit ATTACK?" Options (the seat's labels): ATTACK means fire at will; override STP's blanket hold-fire (Recommended) / The
    order's ROE wins; ATTACK just advances / Tight (return fire) as a middle ground.
  A4 (owner SELECTION of the seat's label, 2026-09-26): "ATTACK means fire at will; override STP's blanket hold-fire (Recommended)"
  Q5 (as put, P3691 AskUserQuestion, its second question; echoed in the P3694 tool_result, type=user): "Cut A's T01 and T13 are passage-of-lines tasks. STP
    means them as CNFPSL (conduct forward passage of lines) but the same STP lower-case bug exports them as ExecutePlanPhase, which the interface runs as a
    hold. The interface has no CNFPSL verb yet, and T02 and T14 are chained after them as holds. For the first run:" Options (the seat's labels): Leave as
    ExecutePlanPhase for now; add to the STP ticket (Recommended) / Patch to CNFPSL and add a verb now.
  A5 (owner, P3694, TYPED in place of a selection): "Patch + Jira"
  Q6 (as put, P3773 AskUserQuestion; echoed in the P3775 tool_result, type=user): "How should the passage-of-lines verb (CNFPSL) behave? The order references
    four graphics per task: start point, passage point, passage lane, release point, listed out of order; driving them in listed order produces the 30-40 km
    zig-zag through water. Also T01 is '28 ID controls the division forward passage of lines' (a division HQ controlling, not passing), while T13 is '48 IBCT
    completes forward passage of lines through passage lanes 1-5 and occupies initial attack positions' (the brigade passing)." Options (the seat's labels):
    Route by graphic role; T01 stays a hold (Recommended) / Route by graphic role for both / Revert both to holds for the first run.
  A6 (owner, P3775, TYPED in place of a selection): "Hold for 1st run plus jira item for coa renderer"
  supervisor reading: a unit ATTACK advances with fire at will, never a Fire At; a BREACH advances, reports it is not simulated, completes by time
    (RL-20260921-09); unit-targeted test orders are retired; STP-865 opened. Q2's "enemy company" was the seat's (114.MechCoy is same-side). FOLSPT/FOLASS
    advance-and-hold is the coordinator's direction (no owner record, not in A1-A6). CNFPSL is held in place for now (A6 narrows A5); Jira STP-866.
  supervisor reading: extended to the ATTACK family (ATTMN/ATTSPT/DESTRY/FIX/DISRPT/PENTRT) because they share the ATTACK path; not asked
  pointer: feat/engage-doctrine + fix/engage-followup (TaskDispatchPolicy.ForEngage); SEMANTIC_MAPPING sec 3; RUNBOOK sec 11; STP-865, STP-866.

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
  pointer: docs/PLAN_MOVEMENT_2026-09-27.md; docs/experiments/FINDING_GROUND_MOVEMENT_PRACTICE_2026-09-27.md secs 5, 7, 8; branches
    feat/movement-moveto-per-vertex, feat/preflight-osm-features, feat/aggregate-profile.
