# RULINGS ARCHIVE - overflow from RULINGS.md. Same format, same discipline, no line cap.
# These are rulings the live docs cite that did not fit the 120-line ledger. Priority order keeps the
# completion / tasking entries and the open 2026-09-21 questions in the ledger; everything else is here.
# Ids: RL-20260902-01, RL-20260903-01, RL-20260906-01, RL-20260906-02, RL-20260907-02, RL-20260913-01,
#      RL-20260913-02, RL-20260913-03, RL-20260914-01, RL-20260914-03, RL-20260914-04, RL-20260914-05,
#      RL-20260914-06, RL-20260915-01, RL-20260920-01, RL-20260920-02, RL-20260921-01, RL-20260921-02,
#      RL-20260921-03, RL-20260921-04, RL-UNVERIFIED-DIGUY01, RL-UNVERIFIED-MAK01, RL-UNVERIFIED-NAV09.
# Sources: L<n> = session a7f6a276-7ebc-4507-ac9d-c6bd361bd64e.jsonl, the 1-based physical line `rg -n` prints.

RL-20260902-01 | 2026-09-02 | status VERBATIM
  Q (as put, L4035): "Item 5 is a type-mapping question: what VR-Forces unit an echelon-F (battalion) C2SIM unit becomes
    when it is created in the sim."
  A (owner, L4038): "SImple answer for 5 - the ported c++ interface took many shortcuts to bypass the difficulties you are
    finding in properly configuring for VRF. The rulling is that I want the best possible fidelity using available VRF
    resources. This is a capable SIM that can represent forces with high fidelity. So map units to their actual correct
    representation in VRF. Go find the DIS if you have to."
  pointer: memory feedback-fidelity-over-oracle-shortcuts; docs\SEMANTIC_MAPPING.md; the FidelityTable type map.

RL-20260903-01 | 2026-09-03 | status VERBATIM
  Q (as put): none - unprompted. The seat's preceding message (L7200) described the three 5.2d task layers.
  A (owner, L7203): "C2SIM itself supports very rich jc3iedm tasking. The move you see supported is an artifact of the
    poverty of the c++ bridge implementation. We want to support as much as STP itself is able to represent, not just move."
  pointer: TASK_VOCABULARY_ASSESSMENT 7.1; memory goal-full-stp-task-vocabulary.

RL-20260906-01 | 2026-09-06 | status VERBATIM
  Q (as put): none located - the seat's preceding messages (L19577, L19583) report run P's counts, not a question.
  A (owner, L19586): "When the time comes for tackling the real tasks, not just move along, let's first get the app ready
    for deployment as a demo, working without the harness for this bit. But there's plenty you need to do to get to that
    point, it appears. So add this to the plan."
  pointer: DEMO_READINESS_2026-09-06.md line 3 (which already quotes him); memory project-demo-ready-milestone.

RL-20260906-02 | 2026-09-06 | status VERBATIM
  Q (as put, L21077): "Same interface, same 5.2 build, same model set, same terrain, same frame mode. What changes between
    the scenarios that work and the one that does not [...]"
  A (owner, L21085): "You are overblowing the coa-stp1 scenario. There are just 11 taskees. These are the only ones that
    need to be simulated. You are confusing the ORBAT for the whole Corps, likely, with the specific elements that are
    part of the COA proper."
  pointer: memory coa-is-not-the-orbat; DESIGN_ORBAT_TO_VRF.

RL-20260907-02 | 2026-09-07 | status VERBATIM
  Q (as put): not located in the 60 assistant records before it; the preceding thread is the triangular-position evidence.
  A (owner, L22041): "I think I need to bite my to gu[e^] and accept trying to space out the entities at startup, given the
    evidence of the triangular position you now cite - never mentioned previously when asked."
  supervisor reading: "to gu[e^]" is exactly what he typed (U+00EA). The memory file renders it "bite my tongue"; that is a
    repair, not his text. The ruling itself - spread at startup - is unaffected by the typo.
  pointer: memory placement-spread-at-startup; PREREG_ASSEMBLY_LAYOUT_2026-09-07; HANDOFF CLOSED list C14; RUNBOOK 4.

RL-20260913-01 | 2026-09-13 | status VERBATIM
  Q (as put, L25450): "Four decisions and one action, in order of urgency. [...] 1. Licence renewal, before Tuesday. [...]"
    (the pre-flight scorer is the item he answers).
  A (owner, L25466): "The interface-side idea is very valuable. How would you deliver it though? Users dont read logs"
  supervisor reading: DEMO_READINESS row 20 renders the last clause "users don't read logs"; he typed "Users dont read logs".
  pointer: DEMO_READINESS row 20.

RL-20260913-02 | 2026-09-13 | status VERBATIM
  Q (as put, L25549): "The homework changes the design, and you were right to smell it. The gate is now read in the vendor's
    code, not inferred [...]" (the C:\MAK customisation question is the item he answers).
  A (owner, L25601): "About c:\mak - we need something that survives reinstalls. And what type of customization would you do
    here? Your explanation made it more confusing. [...]"
  pointer: DEMO_READINESS row 21.

RL-20260913-03 | 2026-09-13 | status VERBATIM
  Q (as put, L25450 item 2): "A ruling on stall reporting. [...] a unit whose members show no net movement for N simulated
    seconds while its task still reports running gets a task-status report to STP, and nothing else automatic. [...] My
    recommendation: build it, report-only, N of 120 simulated seconds, no automatic re-tasking yet. I need your yes, and
    whether STP should see it as failed, blocked, or a distinct status."
  A (owner, L25453): "1 already got, need to download only. 2 as recommended. 3 are you saying the vendor does not offer a
    router that takes the terrain and vehicle type into consideration, other than 'prefer roads'? That is hard to believe."
  supervisor reading: he answered the yes. He did NOT answer the second half (failed / blocked / distinct status), and the
    question never mentioned a default-ON or default-OFF ship setting - so the watchdog's default is UNRULED (audit Q-E).
  pointer: DEMO_READINESS rows 19 and 32; DESIGN_ORBAT_TO_VRF C16; VrfSettings.cs Vrf:StallDetection.

RL-20260914-01 | 2026-09-14 | status VERBATIM
  Q (as put, L26818): "Decisions that are yours (L6): 1. Licence renewal before tomorrow's lapse. 2. Whether TASKABRT is
    the code STP should see for a stalled unit. 3. If the custom SMS cannot live outside C:\MAK, sanction for its
    placement; and, if G8 is needed, relocating appData via --appDataDir."
  A (owner, L26918): "Ok on 2-3"
  supervisor reading: scope = the stalled-unit CODE and the C:\MAK placement. Suppression of a later TASKABRT by an earlier
    TASKCMPLT, and abort-then-complete, are a supervisor's reading (audit 1.7 D), not part of this answer. The record's
    "~12:40Z" stamp is wrong; the record stamps it 2026-09-14T11:15:53Z.
  pointer: DEMO_READINESS rows 19, 21 and 43; RUNBOOK sec 11h.

RL-20260914-03 | 2026-09-14 | status VERBATIM
  Q (as put, L28092 items 1, 3, 5, 6; R5 as put: "Which model set first. EntityLevel is reachable now but its scripted tasks
    reject our unit types. AggregateTacticalLevel has native attack, defend and air-defence tasks but needs the
    aggregate-profile work first.")
  A (owner, L28400, sent mid-turn): "R1 the embedded location data is valid C2SIM. What is the gap to support it in addition
    to mapgraphicids? R3 are these aligned with stps task definitions for these tasks? R5 proceed as recommended. But we will
    need to offer users the option to use entity or aggregate, with the understanding of the limitations of each mode. R6 are
    you proposing a mixed aggregate-entity mode? Thats a bridge too far. If not that, explain it again"
  supervisor reading: this message is a MID-TURN record, not a type=user one. A type=user-only search loses it entirely.
  pointer: TASK_VOCABULARY_ASSESSMENT 7.1 (R5); memory goal-full-stp-task-vocabulary.

RL-20260914-04 | 2026-09-14 | status VERBATIM
  Q (as put, L28092 item 6): "R6, company types. The shipped seize script accepts only company-typed performers, and our
    proxies are platoons and HQ sections. Either the type map creates real company aggregates or we override the vendor
    filters." Re-explained at L28442 after the owner's "bridge too far" query.
  A (owner, L28516): "R6 as recommended. Echelons above battalion are ignored?"
  pointer: TASK_VOCABULARY_ASSESSMENT 7.1 (R6).

RL-20260914-05 | 2026-09-14 | status VERBATIM
  Q (as put, L30182): seven numbered questions, each with the seat's stated default. Q1 superseded task (default "TASKABRT
    at the moment of replacement, and A's successors are abandoned immediately"); Q2 which clock (default simulation time);
    Q3 zero-geometry tasks on the wire; Q4 no Duration and no geometry (default "apply a configurable default hold of 60 s
    with a warning"); Q5 a paused scenario; Q6 interim demo setting; Q7 rollback.
  A (owner, L30205): "1 TASKABRT . 2 ok. 3 ok. 4 such a task is malformed, should be skipped with an error. 5 ok. 6 your
    default. 7 ok. [...]"
  supervisor reading: item 4 OVERRIDES the seat's 60 s default; 2, 3, 5 and 7 accept the defaults exactly as put at L30182.
  pointer: RUNBOOK sec 11 (the Q1-Q7 paragraphs); TASK_VOCABULARY_ASSESSMENT :1123-1166; DEMO_READINESS row 16.

RL-20260914-06 | 2026-09-14 | status VERBATIM
  Q (as put, L31897 STP-809, the facade back-end control-state reader; L31909 E3: "at order receipt the interface now walks
    the predecessor graph once, and every task on a loop gets an error naming the loop, one TASKABRT, and its successors are
    released through the normal abandon path").
  A (owner, L31912): "Good on these"
  pointer: memory goal-full-stp-task-vocabulary (E3, STP-809).

RL-20260915-01 | 2026-09-15 | status VERBATIM
  Q (as put, L32372): "The port has ten small command-line tools and programs that talk to VR-Forces through our native
    bridge DLL. Each one compiles against a copy of that [...]"
  A (owner, L32412, sent mid-turn at 00:12:54Z): "Convert tools. Keep probing the western abstract graph - but make sure you
    have looked at documents, samples, community insights. And what else is in the queue? Ploughing ahead. Burning daylight"
  supervisor reading: RUNBOOK sec 0.5 dates this "00:15Z"; the record stamps it 00:12:54Z. The quoted words are his.
  pointer: RUNBOOK, the tools-52-conversion paragraph.

RL-20260920-01 | 2026-09-20 | status VERBATIM
  Q (as put, L39877): "1. Which scenario is 'the demo'? [...] Recommend R9 lean as the deliverable. 2. Way B: which C2SIM
    server does an STP-driven demo use? [...] Recommend pointing STP at 18080. 3. Route shift default ON in the demo
    profile? [...] Recommend ON in appsettings.Demo.json only. 4. Route-extent bounds [...] 5. STP-845 [...] 6. GUI
    quit-prompt fix [...] 7. The MAK support case for STP-825. 8. The bundling-size experiment. 9. The Q5 kill-half."
  A (owner, L39984): "1. I want the Iron Storm scenario generated by CoaRenderer. I can help you find it if you have
    trouble. 2. You can use the standard 8080/61613. 3. On. Use as default for any run. 4. Ok, but you will need terrain
    for Iron Storm. 5. Accept. 6. Fine. Go for 8-9 when convenient. Kill the leftover vr-forces window."
  supervisor reading: item 3 is BROADER than the question put - any run, not the demo profile. Item 7 was not answered;
    it was answered a day later, see RL-20260921-02 item 5.
  pointer: RUNBOOK sec 11 route-shift paragraph; DEMO_RUNBOOK sec 2; DEMO_READINESS row 24.

RL-20260920-02 | 2026-09-20 | status VERBATIM
  Q (as put, L41290): Iron Storm representation, three options tabled (A a demo-scale cut on today's representation; B the
    aggregate-level profile; C re-author in STP), "My recommendation is A now, then B"; plus the STP-side connector findings
    and RTI restarts for the STP-825 work.
  A (owner, L41350): "1.  A and the B sounds right. 2. File Jira issues. 3. RTI restart as needed."
  supervisor reading: memory project-demo-rulings-2026-09-20 renders this "A and then B"; he typed "A and the B sounds right".
  pointer: memory project-demo-rulings-2026-09-20; DEMO_READINESS row 16; plan-aggregate-level-profile.

RL-20260921-01 | 2026-09-21 | status VERBATIM
  Q (as put, L41754 and L41880, both still open at 00:31Z): (a) T14 crosses a lake - "(1) I apply the 800 m-west
    destination nudge [...] I recommend the first option"; (b) de-stack A/C/D - "My recommendation is A plus C." L41754
    also says of the 0.9 nav bar: "Reopening that ruling is your call. I'm proceeding under it: FAIL."
  A (owner, L42017): "As recommended"
  supervisor reading: two words, answering the recommendations open at 00:31Z; they do NOT make the 0.9 nav bar an owner
    ruling - see RL-UNVERIFIED-NAV09. The recommendation he approved said "about 300 m for platoons" (L41880); the record
    at DEMO_READINESS row 23 and RUNBOOK sec 4 says 350 m. That gap is OPEN and is not resolved by this entry.
  pointer: RUNBOOK sec 4 (traversal bar, platoon sibling spacing); DEMO_READINESS row 23.

RL-20260921-02 | 2026-09-21 | status VERBATIM
  Q (as put, L43949): "1. The leftover rtiAssistant (pid 48392): may I close it? 2. Iron Storm cut A: which path? [A = one
    pre-registered diagnostic drive across that band; B = a thin T14-only demo cut.] 3. Demo clock: fast or real time? [...]
    I recommend real time for an audience. 4. The crash call stack: do you want it read? 5. MAK case: open it or not?"
  A (owner, L43952): "1. Close. 2. A. 3. Fast. 4. Read. 5. Open."
  supervisor reading: item 3 overrides the seat's own recommendation. Item 5 is five characters long: "5. Open." Any longer
    quotation of it in a repo file is a fabrication.
  pointer: RUNBOOK sec 0.5 (callstack), the MAK-case paragraph and the demo-clock paragraph; DEMO_RUNBOOK sec 4; HANDOFF
    CLOSED list, the 2026-09-21 rulings line.

RL-20260921-03 | 2026-09-21 | status VERBATIM, its "USER RULING" LABEL WITHDRAWN by RL-20260921-05 - do not delete it
  Q (as put, L44947): "The STP-857 ruling. What should the bus say when a move task's timer expires on a unit that never
    moved? The options are in the ticket. My recommendation is option 1: report an abort, 'armed end reached with no
    movement', where the current build reports complete."
  A (owner, L45180): "857 as recommended - just make sure the task does involve movement. Not all do. Can't believe we are
    back to relitigating altitude. This was exhaustively examined for weeks. Did you bother reading the record?"
  supervisor reading: the question presupposes that timer-completion of MOVE tasks was ruled. It was not - RL-20260914-02. Withdrawn
    are the "USER RULING" label and the patch as scoped; the SYMPTOM is real and he agrees with it (RL-20260921-07). Never write "rejected".
  pointer: DEMO_READINESS row 19 last para; RUNBOOK sec 11h; Jira STP-857; docs\CORRECTIONS_LOG.md F-1.

RL-20260921-04 | 2026-09-21 | status VERBATIM (one SELECTION + one typed note), inside the frame RL-20260921-05 repudiates
  Q (as put, L45417, AskUserQuestion): "STP-857: when a MOVE task reaches its armed end with no movement and reports
    TASKABRT, what happens to the tasks that come after it in the chain?" and "how much displacement counts as 'the unit moved'?"
  A (owner SELECTED option, seat's wording, L45418): "Abandon them"
  A (owner TYPED, L45418): "This sounds odd. Tasks should include the expected final location if they are indeed moves,
    shouldn't they? Is this creating a special class of errors for no movement from the get go as opposed to movement that
    falls short of the objective?"
  supervisor reading: the typed half is a QUESTION BACK from the owner and is STILL UNANSWERED; no movement bar was ever
    chosen. It belongs on the list of things to put to him, beside the audit's Q-A..Q-F.
  pointer: RL-20260921-03; RL-20260921-05; Jira STP-857.

RL-UNVERIFIED-DIGUY01 | (claimed, undated) | status UNVERIFIED - no owner words found
  Q (as put): none located. A (owner): NOT FOUND. Searched all 276 genuine owner messages for /fidelity/i (1 hit, which is
    RL-20260902-01 and is about type mapping, not DI-Guy), /DI-?Guy/i and /lifeform/i (1 hit, L20763: "DIGuy installed. Did
    you do the research of vendor docs, samples, community posts, vendor code when you found the new 'issue'."). The claim
    at DEMO_READINESS row 17 that "dropping the DI-Guy package (or lifeform rows) is a fidelity regression = user ruling"
    has no owner words behind it. RL-20260902-01 is the nearest real ruling and it is about unit type mapping.
  pointer: DEMO_READINESS row 17.

RL-UNVERIFIED-MAK01 | (claimed, undated) | status UNVERIFIED - no owner words found
  Q (as put, L25450 item 5): "Optional: one question to MAK support, which I will not send without you [...]"
  A (owner, L25453): NOT ANSWERED - his reply covers items 1, 2 and 3 only. Searched all 276 owner messages for /MAK/i,
    /support case/i, /open the case/i: none asks for or forbids questions to MAK. The HANDOFF standing rule "NO questions
    to MAK (user ruling)" reads an UNANSWERED item as a prohibition; RL-20260921-02 item 5 ("5. Open.") points the other way.
  pointer: HANDOFF section 3, the documentation-first rule; RUNBOOK, the MAK support case paragraph.

RL-UNVERIFIED-NAV09 | (claimed 2026-09-20/21) | status UNVERIFIED - no owner words exist
  Q (as put): none located. A (owner): NOT FOUND. Searched every genuine owner message (276 records, turn and mid-turn) for
    /connectivit/i and /0\.9/: 0 hits. "connectivity ratio >= 0.9 is the gate and STAYS (owner 2026-09-20/21)" is a
    SUPERVISOR's statement; he neither affirmed nor declined it. The bar may stand on its own merits, but not on his authority.
  pointer: coldstart_fresh_start_handoff.md sec 1 NAV DATA; memory project-demo-rulings-2026-09-20 third round; HANDOFF
    CLOSED list, the nav-area product rule.
