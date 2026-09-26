# RULINGS - the ONLY place a ruling lives. Every other file points to an id and never restates the ruling.
# Format: RL-YYYYMMDD-NN | date the owner answered | status. Q = the question AS PUT. A = the OWNER'S OWN WORDS.
# status VERBATIM = Q and A both located, A inside a genuine owner-typed record. UNVERIFIED = searched, not found.
# Sources: L<n> = session a7f6a276-7ebc-4507-ac9d-c6bd361bd64e.jsonl; S<n> = session c3b364bd-a4ae-445a-b5c3-e585eaa5935c.jsonl (entries -08, -09);
#   P<n> = session 5fc25950-1a10-4ade-9a7b-68cb5c1daf05.jsonl (entry RL-20260925-01).
#   All are the 1-based PHYSICAL line `rg -n` prints; the record shape is named where it is not a plain type=user record.
# Owner text is EXACTLY as typed, misspellings included. Only transliteration: U+2019 -> ' and U+00EA -> [e^].
# "[...]" elides; the operative clause is never cut. A "supervisor reading:" line is scope only and binds nobody.
# A selected AskUserQuestion label is the SEAT's wording, recorded as a selection, never as the owner's words.
# Cap 120 lines / 160 chars. Overflow -> RULINGS_ARCHIVE.md, same format, no cap. Index of ids at the archive head.

RL-20260914-02 | 2026-09-14 | status VERBATIM
  Q (as put, L28092): "4. R4, when a hold ends. SECURE, OCCUPY and DEFEND have no natural completion in VR-Forces. Decide
    whether they complete on arrival, after a duration, or only when a later order supersedes them." Items 1-3 of the same
    message put R1 (finding the objective), R2 (tasks with no geometry) and R3 (who the enemy is).
  A (owner, L28191): "1 if that is real, that is a defect of the stp export. Map graphics should be included. Perhaps the
    wrong configuration was used and an embedded location was placed inside the task. Check, let me know. 2 another stp
    issue if real. Some tasks will only have the geometry of the who unit. 3 the target is the objective in doctrinal
    terms. Enemies may happen to be in there. Not what vrf wants? 4 given by the end time"
  supervisor reading: item 4 answers a question about HOLD-type tasks. The message contains no word about MOVE tasks.
  pointer: TASK_VOCABULARY_ASSESSMENT 7.1 (R1-R4); RUNBOOK 11. Read with RL-20260921-05 and -08. R5/R6: see the archive.

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

RL-20260921-08 | 2026-09-21 | status VERBATIM - COMPLETION SEMANTICS ARE UNDER HIS REVIEW, NOTHING HERE IS A RULE
  Q (as put, S291 AskUserQuestion; echoed in the S304 tool_result, type=user): "Completion tripwires for the entry doc, rewritten in
    plain words (no 'R4'): (a) a task ends at start time + Duration only if it involves no movement; (b) a move ends on arrival - the
    sim's completion or the unit's own arrival evidence; (c) stuck is not complete: stalled = abort, a later arrival still reports
    complete; (d) late is not an abort: follow-on tasks wait; (e) the code on main ends moves on the Duration timer - that is a defect
    to fix, never a ruling; plus an explicit NOT SETTLED list (move-then-hold, patrol, follow, how long followers wait, watchdog
    on/off). RULING ON FILE: "report a unit's completion from the unit's own arrival evidence is fine" (2026-09-07) and your
    2026-09-21 message quoted earlier. OK to write these?" Options (the seat's labels): Yes, write them / Edit first / Not yet.
  A (owner, S304): "The notion of a hold-off or movement tasks seems to be rather fuzzy as described. Some verbs imply that
    the unit stays in place (e.g. to DEFEND). There are plenty of tasks where there is movement preceding the desired
    effect, like in "ATTACK TO SECURE". How are these distinguished? Seems to me that the time applies to the full task,
    including the movement and the achievement of the desired effect, at which point the unit might be tasked to perform a
    "follow on" task (not a "follower"). SPeaking of which what do you mean by "late delays followers" - I meant to say
    "follow ons" - the tasks for the same unti that may be set to start after it has completed a preceding task. And I hear
    you about the "eralier" - this assumed that all a unit had to do was complete the movement, but your point is valid,
    that theres action required after that - requires a doctrinal research, and as well as of vendor documentation - is the
    sim able to determine when some of the desired effect has been achieved, for example DESTROY (no enemy unit operational
    in the target area or something like that?)."
  Q (as put, S257 AskUserQuestion; echoed in the S260 tool_result, type=user; the list it points to is in S256): "Completion
    tripwires (a)-(e) as listed in my message above, for the HANDOFF CLOSED list. RULING ON FILE: "The rulling based on time is for
    tasks that do not include movement, such as defend in place and similar ones - units that are not expected to reach any other
    location." (your message, transcript line 45457). Do (a)-(e) state your semantics correctly?" Options (the seat's labels): Yes,
    as written / Needs edits / Hold - not yet.
  A (owner, S260, the same hour, on an earlier version of the same list): "SOunds ok, except for the cryptic "R$"
    reference. DOn't know what your little codes mean. But more importantly, what does the record say? I mentioned what I
    could recall. There may be way more related to this are than what I said. Are you even reading the materials?"
  supervisor reading: "Seems to me" is tentative; nothing in this entry is a rule. Two standing instructions do follow from
    the S260 answer: use PLAIN WORDS in anything put to him (codes only in parentheses), and read the record before asking.
  pointer: RL-20260921-09; docs\experiments\TASK_COMPLETION_RESEARCH_2026-09-21.md, the per-verb table carrying the research he asked for.

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

RL-20260925-01 | 2026-09-25 | status VERBATIM - four SELECTIONS of the seat's labels in one P981 record (23:18:21Z), no typed note
  Q1 (as put, P968 AskUserQuestion; echoed in the P981 tool_result, type=user): "PLAN gate for the next code unit [...] In short: every task with a Duration
    ends at dispatch + Duration; an early arrival is held and reported at that end time; a unit still travelling at end time is reported complete when it
    arrives, and that arrival is no longer suppressed; a stuck unit gets the abort already ruled; follow-ons wait for a late predecessor instead of being
    skipped 60 s after its end time, and get their full Duration from their own start; tasks with no destination end at Duration; effect ignored. Re-clamp:
    placement stays, the dispatch gate stops holding/abandoning, the tally is fixed, and the dispatch-time setLocation is dropped. Do you approve this scope?"
    Options (the seat's labels): Approve as written (Recommended) / Approve, but keep the dispatch-time setLocation / Changes needed.
  A (owner SELECTION of the seat's label, 2026-09-25): "Approve as written (Recommended)"
  Q2 (as put, P968): "Follow-on tasks of a unit reported STUCK (TASKABRT): today the stall abort is report-only and the Duration timer released the follow-ons
    anyway; after this unit nothing would. What should happen to them?" Options (the seat's labels): Abandon them with their own aborts (Recommended) / Keep
    waiting for a late arrival / Release them to start.
  A (owner SELECTION of the seat's label, 2026-09-25): "Abandon them with their own aborts (Recommended)"
  Q3 (as put, P968): "Stall detection ships OFF. With the timer no longer ending an unarrived mover, a stuck unit with detection off never gets a terminal
    report and its follow-ons sit until the backstop. Calibration on file: 3/3 freezes caught, 0/6 false alarms; the C16 watchdog passed live. Default?"
    Options (the seat's labels): ON in the demo profile (Recommended) / ON everywhere / Leave OFF.
  A (owner SELECTION of the seat's label, 2026-09-25): "ON in the demo profile (Recommended)"
  Q4 (as put, P968): "An attack or breach whose movement arrives AFTER its Duration: the task is reported complete on arrival. Should the parked engage (the
    action at the objective) still be issued at that point?" Options (the seat's labels): Issue it (Recommended) / Drop it.
  A (owner SELECTION of the seat's label, 2026-09-25): "Issue it (Recommended)"
  supervisor reading: these settle the four open implementation questions of the next code unit under RL-20260921-09; nothing here alters -09.
  pointer: scratchpad u3\laneP_next_unit_scope.md (session scratch); the code unit: branch feat/completion-temporary-position (F-4).
