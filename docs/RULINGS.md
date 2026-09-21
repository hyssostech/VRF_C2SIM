# RULINGS - the ONLY place a ruling lives. Every other file points to an id and never restates the ruling.
# Format: RL-YYYYMMDD-NN | date the owner answered | status. Q = the question AS PUT. A = the OWNER'S OWN WORDS.
# status VERBATIM = Q and A both located, A inside a genuine owner-typed record. UNVERIFIED = searched, not found.
# Sources: L<n> = session a7f6a276-7ebc-4507-ac9d-c6bd361bd64e.jsonl; S<n> = session c3b364bd-a4ae-445a-b5c3-e585eaa5935c.jsonl (entries -06..-09).
#   Both are the 1-based PHYSICAL line `rg -n` prints; the record shape is named where it is not a plain type=user record.
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

RL-20260921-06 | 2026-09-21 | status VERBATIM - OPEN, THE KEEP/REMOVE DECISION IS WITH THE OWNER
  Q (as put, S304): "The re-clamp feature merged today (re-measure units after creation, put them on the ground, refuse to task units
    found off the terrain) is broken on not-yet-streamed terrain [...] What should its default be until it is rebuilt?"
  A (owner, S304): "Looks like this "re-clamp thing" is useless, or actually harmful. Does not improve the situation it is meant to
    help with, and messes up with legitimate tasks. Is that it? If so it should be removed, not switched off. But I may have misread
    what you said."
  Q (as put, S344, the same question re-put with the three parts spelled out - the placing works per one run, the self-check never
    succeeds, the refuse-to-task gate harms legitimate tasks on cold terrain).
  A (owner, S344): "Confirm first that my interpretation of what you described is correct"
  A (owner, S441, typed, after the seat confirmed on half the evidence): "Reclamp: how can you say that harvest verified it
    works and in the next sentence that placement verification never succeeds? This smells of instrument error (not to
    mention the bizarre logic)"
  The seat checked and RETRACTED that confirmation. STATUS: OPEN - remove it all / keep the placing and drop the gate and
    its tally / leave it until a re-cut. No file may record a keep-or-remove decision until he gives one.
  MEASUREMENT, n = 1, seat-verified in runs\20260921T143243Z_run (our own files only): vrfc2simapp.log :626 "Terrain profile reply 43: 32
    sample(s)"; :628-:690 thirty-two "MEASURED OFF THE TERRAIN - live -0.0 m vs terrain <h> m ... Issuing setLocation"; :20461 "0
    RE-CLAMPED AND VERIFIED ... 32 NEVER MEASURED"; :772/:784 T1 and T13 HELD; :20307/:20345 ABANDONED at 60.0 s; :20311/:20347 follow-on
    tasks SKIPPED. watchvrf-trace.csv POS rows: the 32 corrected objects sit at exactly those terrain heights for every sample (min =
    max), while 4/278_ACR (6e3c0605..., created on the fallback, not enrolled) stays at -0.0 m for 1,196 samples.
  SEPARATELY, as implications and not as cause: the feature's tally disagrees with the feature's own log; the gate ended two tasks on
    units an independent trace shows on the terrain. NOT ESTABLISHED: there is no observer sample of the 32 from BEFORE the correction;
    the setLocation lines carry no timestamp; why the read-back never landed is undiagnosed.
  pointer: docs\CORRECTIONS_LOG.md F-2; commit 8aeb127; the HANDOFF CLOSED list re-clamp line.

RL-20260921-07 | 2026-09-21 | status VERBATIM
  Q (as put, S304, AskUserQuestion): what should happen to Jira STP-857, the seat having said its premise was false.
  A (owner, S304): "As for 857, a unit that is meanto to move to an objective and perform some action, but instead gets
    stuck in the middle of the way certainly did not complete, so not sure why you say the "premisse was false".  My
    caveat was that you should not expect every task to require a movement, and abort in case the unit stays put."
  supervisor reading: the SYMPTOM is his too. The caveat is that not every task requires movement; the abort is what he
    wants FOR a movement task whose unit stays put. Do not compress the two halves into one rule.
  pointer: RL-20260921-03; Jira STP-857 (correction text drafted, NOT posted - awaiting him).

RL-20260921-08 | 2026-09-21 | status VERBATIM - COMPLETION SEMANTICS ARE UNDER HIS REVIEW, NOTHING HERE IS A RULE
  Q (as put, S304, AskUserQuestion): the seat proposed five completion tripwires (a time-based end only for tasks without
    movement; a move ends on arrival; stuck is not complete; late is not an abort; timer-on-moves is a defect).
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
    arrives; a unit that gets stuck is already ruled (RL-20260914-01, archive: abort is the code STP sees; a later arrival still
    reports complete); tasks with no destination end when their Duration elapses (RL-20260914-02); whether an effect was achieved is
    IGNORED under the temporary position - that is the research question, not a gap; each follow-on task gets its full Duration from
    its actual start. The research is the seat's job.
  IMPLEMENTATION FACT: stall detection ships OFF by default on main (DEMO_READINESS row 19), so the stuck-unit ruling only takes
    effect once it is switched on.
  pointer: docs\experiments\TASK_COMPLETION_RESEARCH_2026-09-21.md (the per-verb completion table; research only, no recommendation); the
    code defect noted in the HANDOFF CLOSED list - main ends a task on the Duration timer even when the unit has not arrived, and then
    suppresses the real arrival (since 746c091). Fixing that is the next code unit and is PLAN-gated with the owner.
