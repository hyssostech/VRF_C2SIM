# RULINGS - the ONLY place a ruling lives. Every other file points to an id and never restates the ruling.
# Format: RL-YYYYMMDD-NN | date the owner answered | status. Q = the question AS PUT. A = the OWNER'S OWN WORDS.
# status VERBATIM = Q and A both located, A inside a genuine owner-typed record. UNVERIFIED = searched, not found.
# Sources: L<n> = session a7f6a276-7ebc-4507-ac9d-c6bd361bd64e.jsonl; S<n> = session c3b364bd-a4ae-445a-b5c3-e585eaa5935c.jsonl (entry -09);
#   P<n> = session 5fc25950-1a10-4ade-9a7b-68cb5c1daf05.jsonl (entries RL-20260927-02 to -06, RL-20260928-01 to -04). Archived (cap): -08, 0914-02 on 09-26;
#   Q<n> = session 7207877b-f104-4515-91f7-c662e3fce85c.jsonl (1004-01..-06, 1005-01..-03). Archived 10-04/05: 0927-02..-06, 0928-01, -03.
#   0925-01, 0921-05, 0926-01 on 09-27; 0927-01 on 09-28; 0921-09 on 09-28 (for -03).
#   All are the 1-based PHYSICAL line `rg -n` prints; the record shape is named where it is not a plain type=user record.
# Owner text is EXACTLY as typed, misspellings included. Only transliteration: U+2019 -> ' and U+00EA -> [e^].
# "[...]" elides; the operative clause is never cut. A "supervisor reading:" line is scope only and binds nobody.
# A selected AskUserQuestion label is the SEAT's wording, recorded as a selection, never as the owner's words.
# Cap 120 lines / 160 chars. Overflow -> RULINGS_ARCHIVE.md, same format, no cap. Index of ids at the archive head.

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
RL-20260928-04 | 2026-09-28 | VERBATIM
  Q (owner, P10345): "DOes it make sense to have opus 5.5 run the seat, and invoke Fable for the real work requiring special skills, rather
    than burn Fable tokens as supervisor?" - the seat (P10347) answered yes with one inversion: Opus 5.5 runs the seat (orchestration, the
    record discipline, go-lives, routine adversarial reading); Fable is invoked as the cold-start REVIEWER of every cause claim and design
    brief before it reaches the owner, and as the executor of the few judgment-heavy lanes; switch at a natural cut (after M3b / G1-4).
  A (owner, P10359): "Ok. Slot this switch for the soonest appropriate opportunity then"
  supervisor reading: the soonest appropriate cut is the M3b merge (the lane in flight at 19:56Z): the Fable seat merges M3b, writes the seat
    handoff (docs/HANDOFF_SEAT_2026-09-28.md) and stops; the next session starts on Opus 5.5 and runs the deploy, the G1-4 registration and
    the go-live on holder 5255 (RtiProbe 56380, up to ~03:00Z 09-29), invoking Fable through the Agent tool (model fable) for reviews and
    judgment lanes. Seat quality is measured by the owner's corrections per day (2026-09-28: six).
  pointer: docs/HANDOFF_SEAT_2026-09-28.md; CLAUDE.md sec 5 (the seat rule); memory feedback-seat-model-policy.
RL-20261004-01 | 2026-10-04 | VERBATIM - Q171, TYPED
  Q (seat, Q159, after Fable's cold review of the G1-4 Result; markdown emphasis dropped): "My recommendation on the RULE is that the W FAIL
    binds as scored, whatever you decide about N3." [...] "(a) Keep H5 on all members and first give nudged slots a water clearance of at
    least the 10 m buffer. That is a build change, so G1-5 would test two variables at once." [...] "I recommend (a)." [...] "the bridge
    rule should judge fixes against the road and bridge ways, with a fail-first test on G1-4's pre-warm data."
  A (owner, Q171): "As recommended"
  supervisor reading: (1) G1-4 is STOPPED at its W gate, FAIL (H5, P-FALS(e)) as scored; nothing re-scored (H5 governs as written; P-FALS(e)
    fails alone, 16.24 m > 15). (2) G1-5 gates H5 on every tasked member, on a build whose nudged member slots clear OSM water by >= the
    planner's 10 m buffer; its scorer's bridge rule reads the fixes against the road/bridge ways, fail-first on G1-4's pre-warm.
  pointer: PREREG_IRONSTORM_AGG_G1-4_2026-09-28 Result (N1-N3, COLD REVIEW AND RULING); HANDOFF_SEAT_2026-09-28 sec 3 item 3-4.
RL-20261004-02 | 2026-10-04 | VERBATIM - Q478, TYPED
  Q (seat, Q475, after Fable's GO WITH FIXES on lane W1): "Should slots that lie within 10 m of water, not only in it, be moved, with no bridge
    exemption for slots? I recommend keeping both, since they change nothing on cut A. Reply "keep both" or "inside-only"."
  A (owner, Q478): "Keep both"
  supervisor reading: RL-20261004-01's slot clearance applies to a member slot IN OSM water or WITHIN the clearance of it (a slot at
    exactly the clearance is in the band), and a road bridge exempts nothing for a slot; STP route vertices are untouched (M2's rule).
  pointer: fix/slot-water-clearance (e4badc8, 03a350d, 377ce98); Vrf:PreflightSlotWaterClearanceMeters; --osm-selftest 8b, (d), (g)-(j).
RL-20261004-03 | 2026-10-04 | VERBATIM - Q1005, TYPED
  Q (seat, Q987, after Fable's ACCEPT WITH FIXES on the G1-5 Result; markdown dropped): "1. Does G1-5's W FAIL bind as scored? I recommend
    yes. [...] G1-6 would register the culvert clause before its run and test it first on G1-5's pre-warm. 2. Will you install the VR-Forces
    5.2 documentation package before G1-6 is registered? [...] I recommend yes, first."
  A (owner, Q1005): "1 as suggested. 2 will look for you. Make progress on issues that do not depend on it. [...]"
  supervisor reading: (1) G1-5 is STOPPED at its W gate, FAIL (P-FALS(e)) as scored; nothing re-scored; a culvert clause belongs to G1-6,
    registered before its run, fail-first on G1-5's pre-warm. (2) The owner looks for the vendor documentation (Adding Content) to install;
    the culvert clause waits on it; work that does not depend on it proceeds.
  pointer: PREREG_IRONSTORM_AGG_G1-5_2026-10-04 Result (N1, COLD REVIEW); HANDOFF_SEAT_2026-09-28 sec 3 item 3-4.
RL-20261004-04 | 2026-10-04 | VERBATIM - Q1275, TYPED, unprompted (after the seat's status and demo-gap list)
  Q (as put): none - the seat's preceding message listed what is missing to reach the demo and recommended G1-6 first.
  A (owner, Q1275): "Find a balance between the investigations and getting a demoable version in place. We can tackle the finer points
    afterwards, provided we can show some level of simulation to make things credible. The demo is presented as work in progress, so we
    have some space to postpone harder problems that only affect smaller parts of the simulation"
  A (owner, Q1294, queued mid-turn): "This culvert thing is one such example"
  supervisor reading: demo readiness first; what does not block a credible WIP demo (the culvert, N1's cause, the speed dip, the G1-3
    crash stack, verbs/units in a small part of the order) is deferred, not dropped; registered runs go on where cheap or as rehearsals.
  pointer: HANDOFF_SEAT_2026-09-28 sec 3 item 3-4 (the demo track); PLAN_MOVEMENT row G2.
RL-20261004-05 | 2026-10-04 | VERBATIM - Q1452, TYPED
  Q (seat, Q1433): "Still waiting on you: 1. G1-6 go-live [...] 2. Tier 2: compositions for the five missing brigades. 3. Demo path:
    Way A or Way B. 4. Label display: the operator toggles it in the GUI, or the script edits a file under C:\MAK." [...]
  A (owner, Q1452): "(1) go. (2) is the composition available as public unclass information? (3) B. (4) is this robust? I need this to
    work on every installed instance of this component. Ok to change C:\MAK, but needs to be automated/reproducible"
  supervisor reading: (1) G1-6 go-live authorised. (2) a question, not a ruling: compositions only from public unclassified sources.
    (3) the demo runs WAY B (STP pushes its own order). (4) the Label decoration may be written under C:\MAK ONLY by an automated,
    reproducible, verifiable deploy step that works on every installed instance; no hand edits.
  pointer: PREREG_IRONSTORM_AGG_G1-6_2026-10-04; tools/display/Enable-LabelDecoration.ps1 (feat/full-designation-label).
RL-20261004-06 | 2026-10-04 | VERBATIM - Q1858, TYPED
  Q (seat, Q1855): "1. Tier 2a compositions: 278 ACR and 169 FAB now; 55 MEB and 11 CAB left refused [...] 2. 56 SBCT: the Polish stand-in
    battalion, or an authored Stryker infantry battalion. 3. Way B's duration fix: STP fixes STP-848 on its side, and/or the interface also
    accepts the short form PT20M. I recommend both. [...] 4. Firewall rules: pre-create them as an automated deploy step"
  A (owner, Q1858): "1 ok. 2 standin. 3 both - is there a jira item for stp already? 4 commands to put this in place?"
  supervisor reading: (1) compose 278 ACR (ABCT pattern) and 169 FAB from public doctrine; 55 MEB, 11 CAB stay refused with reasons.
    (2) 56 SBCT's Stryker infantry = the Polish motorized BN as a labelled STAND-IN. (3) STP-848 stays for STP AND the interface accepts
    the canonical ISO-8601 short form too (reverses the 2026-09-20 strict-decoder choice in OrderParser.cs, a seat choice, not a ruling).
    (4) a question: the commands are shown; any change to the host firewall is the owner's to run (elevated).
  pointer: STP-848 (Jira, To Do); scratch laneCOMP (sources); HANDOFF_SEAT_2026-09-28 sec 3 item 3-4.
RL-20261005-01 | 2026-10-05 | VERBATIM - Q2088, TYPED
  Q (seat, Q2085): "for the Way B rehearsal, STP's raw export run once with the GUI, do you want a full registration or a lighter
    rehearsal registration? Either way it records its expectations before launch, and it needs your go-live."
  A (owner, Q2088): "Lighter rehearsal go now. You are free to go live, pending agreement with other sessions for shared resources"
  supervisor reading: (1) the Way B rehearsal is registered LIGHT (expectations written before launch, no Fable pre-review) and goes
    now. (2) STANDING go-live authority for the seat, conditional on agreeing shared resources (machine quiet, desktop, ports) with
    the other sessions first; a PREREG before every launch still applies (CLAUDE.md sec 5, the house rite).
  pointer: docs/experiments/REHEARSAL_WAYB_2026-10-05.md; HANDOFF_SEAT_2026-09-28 sec 3 item 3-4.
RL-20261005-02 | 2026-10-05 | VERBATIM - the user record after Q2570, TYPED
  Q (seat, Q2570, after the vendor-log reading): "(a) Bounded demo order, recommended. [...] tasks whose routes leave a set demo
    area reported as out of area rather than executed [...] (b) Pre-load the terrain. [...] (c) Run the --no-gui leg first"
  A (owner): "As recommended"
  supervisor reading: option (a): the interface bounds the demo by a terrain extent setting - a task whose route leaves it is
    REPORTED out of area (TASKABRT with the reason), not executed; STP's export stays unchanged (Way B); then a light GUI rehearsal
    of the bounded order on the run-owned unattended appData. (b) terrain pre-load is a later improvement; (c) dropped.
  pointer: REHEARSAL_WAYB_2026-10-05 Result ADDENDUM 2026-10-05 (the vendor-log backlog); HANDOFF_SEAT_2026-09-28 sec 3 item 3-4.
RL-20261005-03 | 2026-10-05 | VERBATIM - Q2752 (queue-operation, typed mid-turn), unprompted
  Q (as put): none - sent while the bounded Way B rehearsal ran (labels drawn as name + full C2SIM name).
  A (owner, Q2752): "Add a note regarding g the Labels: C2SIM names: use just the prefix separated with "__", and remove
    underscores to recover the clean designators. The suffix is a verbose description. Use it if there is a field that can
    accommodate that, discard otherwise." (the owner's quotes around __ were typographic; transliterated)
  supervisor reading: the LABEL shows the CLEAN DESIGNATOR: the C2SIM name's part before the first "__" with underscores removed
    (e.g. 48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE -> 48 IBCT/28ID - whether "_" becomes a space or nothing is
    the lane's to show from the data); the verbose suffix goes into a VR-Forces field that can hold it, if one exists (the
    vendor docs decide), else it is dropped. Identity stays the UUID (RL-20260928-02).
  pointer: HANDOFF_SEAT_2026-09-28 sec 3 item 3-4; feat/full-designation-label (the Label plumbing, merged 6a978a5).
