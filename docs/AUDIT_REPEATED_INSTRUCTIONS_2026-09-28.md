# AUDIT: instructions the owner had to repeat (2026-09-01 .. 2026-09-28)

Ordered by the owner 2026-09-28 (P8612: "Do an audit of the things I repeated multiple times. Show me the list").
Source: every owner-typed message in the nine session transcripts of this project on this machine - 319 messages,
extracted by scratch tool extract_owner_msgs.py (type=user records, tool results and injections excluded). Refs are
session:line as the ledger uses them (L = a7f6a276 09-01..09-21; S = c3b364bd 09-21; P = 5fc25950 09-25..09-28;
B = be8c4cc4 09-03). The July-August sessions are NOT on this rebuilt machine: repeats before 09-01 are visible only
through the record's own citations (MOJAVE_ROOTCAUSE_INVESTIGATION_2026-07-14, the R8 ruling of 07-13). "IN CODE" was
verified on main 16c7e4b today, not taken from docs.

Counting rule: a message counts when it restates, redirects to, or rebukes the absence of a decision already given.
~130 of the 319 messages are such repeats or redirections (about 40 percent).

## The list

1. READ THE VENDOR DOCS, SAMPLES AND COMMUNITY FIRST; NO RANDOM WALKS OR PROBES WITHOUT A CITATION. About 25 times:
   L177, L1444, L1536 (became CLAUDE.md "Research bias"), L3484, L3705, L3773, L4062, L4806, L5321 ("What's wrong with
   the links I gave you (twice)"), L5731, L10711, L10756, L11619, L12845 ("docs, docs, docs"), L13459, L13525 ("HIT THE
   BOOKS! No random probes until you can cite..."), L15571, L16675, L17447, L17656, L17699, L17884, L19122, L20763,
   L25221, P4769. RECORDED: CLAUDE.md Research bias (09-01); memory. IN CODE: n/a (behaviour). WHY IT RECURRED: the
   rule is in the constitution but nothing gates "cite the doc before the probe"; after a compaction or a model
   fallback a session restarts from instinct. Still violated 09-27 (movement) and 09-28 (names).
2. OBJECT IDENTITY IS THE UUID, NOT THE NAME (the name-in-a-uuid-blob confusion). 3 times plus the record: L17884
   ("settled as a confusion that resulted from sticking name into a uuid blob, needed the actual uuid to be used
   instead. Looks like the sessions own notes are being ignored"), L17968, P8243, P8612. RECORDED: MOJAVE_ROOTCAUSE
   07-14 item 1; ORBAT_LOADING_REQUIREMENTS_2026-09-06 G5 (filed as "optional" - wrong, reversed 09-28);
   DESIGN_ORBAT C8 (addressing only); RL-20260928-02. IN CODE: graphics yes since 09-02/09-14 (startingUUID = C2SIM
   uuid); UNITS NO - never, in this port nor in the C++ oracle (createEntity with no startingUUID, C2SIMinterface.cpp
   :706-711); package C1d in progress. WHY: recorded with the wrong status; no tripwire; the seat approved C1c (name
   uniqueness) on a lane's measurement without the record grep.
3. ALTITUDE AND CLAMPING: no 10000 m birth; place per the vendor's model (platforms clamp, units organize). About 12
   times: L1444, L12845, L13005 ("Scour the record and identify and fix the source of rot"), L13145, L13217, L13267
   ("never raises its ugly head ever again"), L14007, L14140, L14192, L14795, L45180 ("Can't believe we are back to
   relitigating altitude"), S441, S555. RECORDED: VRF_ALTITUDE_FRAMES, PlacementPolicy, RL-20260921-06. IN CODE: yes -
   PlacementPolicy; PlacementSelfTest "no 10000 m birth anywhere"; re-clamp merged 09-21. WHY: re-derived three times
   (09-04 after an Opus fallback, 09-05, 09-21) before the self-test pinned it.
4. SUPERVISOR MODE: no pauses; delegate to the right model; cold-start review of the seat's own work. About 18
   times: L1741, L9996, L11300, L15750, L16032, L17865, L24157, L24272, L24543, L25674, L26504, L27596, L29329,
   L31444, L31834, L39668, L44362, S9, P2744, P6010. RECORDED: memory feedback-supervised-delegation-method; CLAUDE.md
   operating posture. IN CODE: n/a. WHY: behavioural; every compaction or fallback resets it.
5. TASK COMPLETION SEMANTICS: time-based completion is for tasks WITHOUT movement; a stuck unit is not complete; a
   late arrival is not an abort; arrival evidence closes a task the sim never reports. 6 times, escalating: L22586,
   L30205, L45457 ("All of this seems all like rediscoveries to me, and for the worse, undoing settled matters"),
   L45496 ("STOP!"), L45516 ("unable to correct your behavior 3 times"), S569. RECORDED: RL-20260914-01,
   RL-20260921-03/-04/-05/-09. IN CODE: yes - TimedCompletionPolicy, D-6 withhold, arrival evidence (E1, E2-2, G1
   records). WHY: the 09-21 seat re-interpreted "60 m of 5 km = complete"; the fresh-start handoff corrected it.
6. ENTITY VERSUS AGGREGATE LEVEL: what each is, when each is used, whether hybrid. About 9 times: L14192, L14795,
   L15528, L15546, L20972 ("Are you trying to apply entity level techniques to an aggregate level scenario?"), L21003,
   L21447 ("what would it take to have an option for aggregate level simulation"), P5324, P(09-28, echelon of the
   tasked units). RECORDED: RL-20260927-06, RL-20260928-01, FINDING_GROUND_MOVEMENT sec 5. IN CODE: yes now -
   Vrf:ModelSet (A1, 09-27), the D2 selector (09-28). WHY: asked for on 09-06, built 09-27.
7. UNIT REPRESENTATION: the vendor's recipe and catalogue; compose members from the TO; populate containers; do not
   reinvent. About 9 times: L4038, L4062, L16675 ("Are you following that recipe or inventing some new stuff?"),
   L17054, L17429 ("just bind to the vendors catalog directly ... instead of reinventing the wheel"), L17447, L17581,
   P5906 ("Again a discussion that's been had ... populating the containers is what needs to be done while we still
   remember it"), P6260, P6398, P6431. RECORDED: DESIGN_ORBAT (09-06), RL-20260927-02/-04. IN CODE: yes now - C1
   containers, C2 authored types (09-27/28); the proxy arm was the drift. WHY: the 09-06 design was PARKED (C10)
   instead of closed; the 09-27 A1 lane proposed proxies again.
8. ONLY THE TASKED UNITS ARE SIMULATED; the whole ORBAT is displayed, not hydrated. 4 times: L21085 ("just 11
   taskees ... confusing the ORBAT for the whole Corps"), L21150, P6030 ("settled already"), P(09-28). RECORDED:
   RL-20260927-03, RL-20260927-06. IN CODE: yes since 09-27/28 (empty containers at init, populate at order, D2 by the
   tasked echelon). WHY: three weeks between the instruction (09-06) and the code.
9. HOSTILE SIDE: "For hostile, I prefer chinese. Russian will do on a pinch" - L4080 (09-02); RE-ASKED by the seat and
   re-answered P5906 (09-27, "1. RUS"). RECORDED: only RL-20260927-02 - the 09-02 answer was never ledgered. IN CODE:
   RUS (nation 260) in the aggregate type map. WHY: a pure re-ask; the answer had no ledger entry.
10. MOVEMENT: leverage the sim's own routing and obstacle avoidance (the 5.2 autonomy shift was researched on 09-04);
    the vendor's expected practice. About 6 times: L13577 ("Don't forget the whole shift towards having units have
    autonomy to navigate, avoid obstacles"), L25453, P4705, P4723, P4769, P5258. RECORDED:
    FINDING_GROUND_MOVEMENT_PRACTICE, RL-20260927-01. IN CODE: yes since 09-27 (M1 Move To per vertex; E1/E2-2 proven).
    WHY: Y-10 ("keep Move Along Route", a seat recommendation recorded as if ruled) held for three weeks against L13577.
11. VR-FORCES IS NOT BROKEN; a suspected bug means we misuse it. 4 times: L1600, L11619, L20763, L20839. RECORDED:
    nowhere as a rule until today's CLOSED list. IN CODE: n/a.
12. THE DEMO IS IRON STORM (Suwalki); Mojave preserved, no new effort. 4 times: L39984, P3345, P3363 ("a full day
    spent on the wrong terrain"), P3388. RECORDED: RL-20260920-01, memory feedback-demo-is-iron-storm. IN CODE:
    PARTLY - the runner wrapper's usage still names an R9 (Mojave) default scenario (scripts/RunScenario.sh usage).
    WHY: the 09-26 terrain work defaulted to the Mojave fixture.
13. FASTER RUNS: run at a multiple; use the faster probes. About 8 times: L1662, L1678, L1835, L2233, L3558-L3810
    ("When are we going to take advantage of the faster probes?"), L21955, L21968. RECORDED: the 09-02 fixed-frame
    finding (the 1 m completion trigger at a multiple; "2a ok on the multipliers for now"). IN CODE: Vrf:TimeMultiplier
    exists, default 1; the runner never sets it; no registered run since has raised it. STATUS: parked, not ruled -
    an owner decision whether to revisit on 5.2d.
14. DE-STACKING AT START-UP: ruled out (L20584 "the de stacking BS you ruled out ages ago"), then ACCEPTED by the owner
    on 09-07 (L22041, "bite my tongue") on the triangular-position evidence. RECORDED: appsettings C14 text,
    PREREG_ASSEMBLY_LAYOUT. IN CODE: yes (Vrf:DeStackCreates). A reversal, listed for completeness.
15. C2SIM RICH TASKING beyond "move" (STP's task factory). 4 times: L7203, L7265, L19586, L28191. RECORDED:
    TASK_VOCABULARY_ASSESSMENT 09-14, RL-20260926-01. IN CODE: partly (scripted tasks, hold-in-place, ATTACK).
    Sequenced behind movement.
16. STP EXPORT DEFECTS GO TO JIRA; hack the XML meanwhile. 3 times: L28191, L41350, P3601. RECORDED: Jira drafts
    (owner posts). IN CODE: the corrected data copies. OK.
17. FILE-BASED INPUT WITHOUT THE SERVER: L4916, L5213 ("low pri"). IN CODE: no. Not a repeat - asked twice, low
    priority by the owner's own words.
18. MODEL FALLBACK TO OPUS MID-SESSION caused regressions: B323, L17865, L17884, L20676 ("went straight into crazy
    mode"), S569 ("one of the cheap models pretending to be Fable"). Root cause behind items 3, 5 and 7's
    re-derivations on 09-04, 09-06 and 09-21. Option A (disable auto-switching, ask instead) was implemented 09-06
    (L20738); whether it survived the 09-25 machine rebuild is unverified.

## Structural causes, from the pattern

- Answers given in chat and never ledgered (9, 11, the 09-04 movement instruction 10): the ledger started 09-02 and
  was applied to explicit AskUserQuestion exchanges, not to redirections typed in passing.
- Settlements recorded with the wrong status (2: G5 "optional"; 10: Y-10 as if ruled; 7: C10 "parked").
- No CLOSED tripwire list until 2026-09-28; the handoff docs carried status, not settlements.
- Model fallback and compaction restart a session from instinct (18); the seat then approves a lane's confident
  measurement without the record grep (2, 09-27 and 09-28).

## Applied today (2026-09-28)

CLOSED tripwire list at the top of PLAN_MOVEMENT_2026-09-27 (items 2, 5, 6, 7, 8, 9, 10, 11 covered); REVERSED
markers at the line in ORBAT_LOADING_REQUIREMENTS G5 and DESIGN_ORBAT C8; RL-20260928-02; package C1d with a source
guard (every create passes a uuid); memory rule extended to lane approvals. Open for the owner: item 13 (a ruling),
item 12 (make Iron Storm the runner default), item 18 (verify Option A survived the rebuild).
