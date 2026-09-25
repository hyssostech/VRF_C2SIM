# RULINGS ARCHIVE - overflow from RULINGS.md. Same format, same discipline, no line cap.
# These are rulings the live docs cite that did not fit the 120-line ledger. Priority order keeps the
# completion / tasking entries and the open 2026-09-21 questions in the ledger; everything else is here.
# Ids: RL-20260902-01, RL-20260903-01, RL-20260906-01, RL-20260906-02, RL-20260907-02, RL-20260913-01,
#      RL-20260913-02, RL-20260913-03, RL-20260914-01, RL-20260914-03, RL-20260914-04, RL-20260914-05,
#      RL-20260914-06, RL-20260915-01, RL-20260920-01, RL-20260920-02, RL-20260921-01, RL-20260921-02,
#      RL-20260921-03, RL-20260921-04, RL-UNVERIFIED-DIGUY01, RL-UNVERIFIED-MAK01, RL-UNVERIFIED-NAV09,
#      RL-20260907-01, RL-20260921-06, RL-20260921-07.
# Sources: L<n> = session a7f6a276-7ebc-4507-ac9d-c6bd361bd64e.jsonl, the 1-based physical line `rg -n` prints.
#   S<n> = session c3b364bd-a4ae-445a-b5c3-e585eaa5935c.jsonl, same line rule (RL-20260921-06 and -07; both moved here from RULINGS.md 2026-09-25).

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
  Q (as put, L21077; the message asks no question, its one ask is its last line): "Same interface, same 5.2 build, same model set,
    same terrain, same frame mode. What changes between the scenarios that work and the one that does not is the input. [...] The
    next step remains your ruling on applying Y-15 to COA-STP1."
  A (owner, L21085): "You are overblowing the coa-stp1 scenario. There are just 11 taskees. These are the only ones that
    need to be simulated. You are confusing the ORBAT for the whole Corps, likely, with the specific elements that are
    part of the COA proper."
  pointer: memory coa-is-not-the-orbat; DESIGN_ORBAT_TO_VRF.

RL-20260907-01 | 2026-09-07 | status VERBATIM
  Q (as put, L22578, the seat's last message before his answer, whose words he echoes): "If the new run repeats the pattern, the
    demo answer is interface-side: report a unit's completion from the unit's own arrival evidence, leader or majority at the route
    end, instead of waiting for the vendor's all-members report. That changes what "complete" means to STP, so it is a policy for you
    to rule on once the run confirms it." First put at L22515: "[...] the interface should report completion on its own evidence
    [...] rather than wait for the vendor's report."
  A (owner, L22586): "report a unit's completion from the unit's own arrival evidence is fine"
  pointer: DESIGN_ORBAT_TO_VRF C15 (:139-148); RUNBOOK 11d; src ArrivalPolicy.cs; HANDOFF CLOSED list C15. Re-affirmed inside
    RL-20260921-05, and it is the mechanism the temporary position RL-20260921-09 relies on for a unit that arrives late.

RL-20260907-02 | 2026-09-07 | status VERBATIM
  Q (as put): not located in the 60 assistant records before it; the preceding thread is the triangular-position evidence.
  A (owner, L22041): "I think I need to bite my to gu[e^] and accept trying to space out the entities at startup, given the
    evidence of the triangular position you now cite - never mentioned previously when asked."
  supervisor reading: "to gu[e^]" is exactly what he typed (U+00EA). The memory file renders it "bite my tongue"; that is a
    repair, not his text. The ruling itself - spread at startup - is unaffected by the typo.
  pointer: memory placement-spread-at-startup; PREREG_ASSEMBLY_LAYOUT_2026-09-07; HANDOFF CLOSED list C14; RUNBOOK 4.

RL-20260913-01 | 2026-09-13 | status VERBATIM
  Q (as put, L25450 item 3, the item he answers; his L25453 answer to items 1-3 and the seat's L25463 reply came between):
    "3. Which route lever to test first. [...] Interface-side leg validation from the same public elevation tiles the sim reads,
    which we validated to 3 cm. As a fix it means building a planner the vendor already ships; as a pre-flight warning to STP
    authors ("leg T1 crosses 55 m of 0.86 face") it is cheap and demo-relevant. Recommended as a warning only."
  A (owner, L25466): "The interface-side idea is very valuable. How would you deliver it though? Users dont read logs"
  supervisor reading: DEMO_READINESS row 20 renders the last clause "users don't read logs"; he typed "Users dont read logs".
  pointer: DEMO_READINESS row 20.

RL-20260913-02 | 2026-09-13 | status VERBATIM
  Q (as put, L25450 item 4): "4. C:\MAK writes, only if you choose the mesh option: editing the vendor's navigationProfiles.mtl,
    and giving the generated data a permanent home under the VR-Forces user data folder where the vendor puts it." Explained again
    at L25512: "Would fixing it mean hacking a C:\MAK file? No hack. [...] The reason it needs your word is our own project rule
    against writing under C:\MAK to protect the install, the same rule the fixture deploy went through, not because the change is
    improper." No later seat text before his answer mentions C:\MAK (L25549, cited here before, is the last seat message before it).
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
  Q (as put, L26818): "Decisions that are yours (L6): 1. Licence renewal download before tomorrow's lapse. 2. Whether TASKABRT is
    the code STP should see for a stalled unit. 3. If the custom SMS cannot live outside C:\MAK, sanction for its
    placement; and, if G8 is needed, relocating appData via --appDataDir."
  A (owner, L26918): "Ok on 2-3"
  supervisor reading: scope = the stalled-unit CODE and the C:\MAK placement. Suppression of a later TASKABRT by an earlier
    TASKCMPLT, and abort-then-complete, are a supervisor's reading (audit 1.7 D), not part of this answer. The record's
    "~12:40Z" stamp is wrong; the record stamps it 2026-09-14T11:15:53Z.
  pointer: DEMO_READINESS rows 19, 21 and 43; RUNBOOK sec 11h.

RL-20260914-03 | 2026-09-14 | status VERBATIM
  Q (as put, L28092 items 1, 3, 5, 6, explained again at L28321, the last seat message explaining them before his answer). L28092 item 5:
    "R5, which model set first. EntityLevel is reachable now but its scripted tasks reject our unit types. AggregateTacticalLevel
    has native attack, defend and air-defence tasks but needs the aggregate-profile work first." L28321 on R5: "My recommendation is
    EntityLevel first for the four company taskees and the demo, with the aggregate profile as the second column, as already
    planned." His R1, R3 and R6 parts are questions back; the L28321 sections they follow are headed "R1: real, and it is the export
    configuration you suspected", "R3: yes, that is exactly what VR-Forces wants" and "R6 explained: company typing for the vendor
    task filters". Item 6 as first put is quoted under RL-20260914-04.
  A (owner, L28400, sent mid-turn): "R1 the embedded location data is valid C2SIM. What is the gap to support it in addition
    to mapgraphicids? R3 are these aligned with stps task definitions for these tasks? R5 proceed as recommended. But we will
    need to offer users the option to use entity or aggregate, with the understanding of the limitations of each mode. R6 are
    you proposing a mixed aggregate-entity mode? Thats a bridge too far. If not that, explain it again"
  supervisor reading: this message is a MID-TURN record, not a type=user one. A type=user-only search loses it entirely.
  pointer: TASK_VOCABULARY_ASSESSMENT 7.1 (R5); memory goal-full-stp-task-vocabulary.

RL-20260914-04 | 2026-09-14 | status VERBATIM
  Q (as put, L28092 item 6): "R6, company types. The shipped seize script accepts only company-typed performers, and our
    proxies are platoons and HQ sections. Either the type map creates real company aggregates or we override the vendor
    filters." Re-explained at L28442 after the owner's "bridge too far" query, ending: "Recommendation: the test run first, fan-out
    as the product answer for battalions, widened filters only as the transition. No aggregate-level modelling anywhere in this."
  A (owner, L28516): "R6 as recommended. Echelons above battalion are ignored?"
  pointer: TASK_VOCABULARY_ASSESSMENT 7.1 (R6).

RL-20260914-05 | 2026-09-14 | status VERBATIM
  Q (as put, L30182): "Here are the rulings I need from you, in plain terms. [...] 1. Superseded task. A unit is doing task A when
    task B arrives for it and VR-Forces replaces A. What does STP see for A? My default: TASKABRT at the moment of replacement, and
    A's successors are abandoned immediately [...] 2. Which clock measures a Duration. My default: simulation time [...] 3.
    Zero-geometry tasks on the wire. [...] My default: accept this until STP renders observations (STP-800). 4. No Duration and no
    geometry. None in COA-STP1. My default: apply a configurable default hold of 60 s with a warning, so the chain proceeds. [...]
    5. A paused scenario. [...] My recommendation, not yet built: hold task time while the back end is still reporting, and fall to
    wall time only when it is not. 6. Interim demo setting. [...] My default: the fix lands tonight, so no stop-gap setting. 7.
    Rollback. [...] My default: keep it, since firing a completion on a rollback would be worse. [...] If you answer nothing, the
    defaults above stand and every one is a single setting to flip later."
  A (owner, L30205): "1 TASKABRT . 2 ok. 3 ok. 4 such a task is malformed, should be skipped with an error. 5 ok. 6 your
    default. 7 ok. [...]"
  supervisor reading: item 4 OVERRIDES the seat's 60 s default; 2, 3, 5 and 7 accept the defaults exactly as put at L30182.
  pointer: RUNBOOK sec 11 (the Q1-Q7 paragraphs); TASK_VOCABULARY_ASSESSMENT :1123-1166; DEMO_READINESS row 16.

RL-20260914-06 | 2026-09-14 | status VERBATIM
  Q (as put, L31897 and L31909, two seat messages he answers together): L31897: "STP-809 is the Jira ticket for a small native fix
    in the VR-Forces facade. [...] Why it is your call. Native changes are pre-authorized, but this one is a demo-risk trade. [...]
    The brief is written and ready; nothing runs until you say so." L31909 (after his L31900 "And E3"): "E3 is a finding from the
    pass-3 review about circular task chains. [...] at order receipt the interface now walks the predecessor graph once, and every
    task on a loop gets an error naming the loop, one TASKABRT, and its successors are released through the normal abandon path.
    [...] Why I am flagging it. It extends your ruling to a case you did not rule on. [...] If you disagree, the reversal is one
    commit and the loop goes back to waiting out the backstop."
  A (owner, L31912): "Good on these"
  pointer: memory goal-full-stp-task-vocabulary (E3, STP-809).

RL-20260915-01 | 2026-09-15 | status VERBATIM
  Q (as put, L32372): "The port has ten small command-line tools and programs that talk to VR-Forces through our native
    bridge DLL. Each one compiles against a copy of that [...] The decision. Either convert the four properly, about an hour of
    executor work plus a live join check, or declare them 5.0.2-only and stop counting them as deploy targets. My recommendation is
    to convert ResetVrf and SetSimRate, since the demo runbook's reset-between-runs step wants them on 5.2, and declare the other
    two 5.0.2-only."
  A (owner, L32412, sent mid-turn at 00:12:54Z): "Convert tools. Keep probing the western abstract graph - but make sure you
    have looked at documents, samples, community insights. And what else is in the queue? Ploughing ahead. Burning daylight"
  supervisor reading: RUNBOOK sec 0.5 dates this "00:15Z"; the record stamps it 00:12:54Z. The quoted words are his.
  pointer: RUNBOOK, the tools-52-conversion paragraph.

RL-20260920-01 | 2026-09-20 | status VERBATIM
  Q (as put, L39877): "1. Which scenario is "the demo"? [...] Recommend R9 lean as the deliverable and COA-STP1 as the stretch
    after it. 2. Way B: which C2SIM server does an STP-driven demo use? [...] Recommend pointing STP at 18080 for the demo. [...]
    3. Route shift default ON in the demo profile? [...] Recommend ON in appsettings.Demo.json only. [...] 4. Route-extent bounds
    [...] 5. STP-845 [...] 6. GUI quit-prompt fix [...] 7. The MAK support case for STP-825. [...] 8. The bundling-size experiment
    [...] 9. The Q5 kill-half ruling from the vocabulary work."
  A (owner, L39984): "1. I want the Iron Storm scenario generated by CoaRenderer. I can help you find it if you have
    trouble. 2. You can use the standard 8080/61613. 3. On. Use as default for any run. 4. Ok, but you will need terrain
    for Iron Storm. 5. Accept. 6. Fine. Go for 8-9 when convenient. Kill the leftover vr-forces window."
  supervisor reading: item 3 is BROADER than the question put - any run, not the demo profile. Item 7 was not answered;
    it was answered a day later, see RL-20260921-02 item 5.
  pointer: RUNBOOK sec 11 route-shift paragraph; DEMO_RUNBOOK sec 2; DEMO_READINESS row 24.

RL-20260920-02 | 2026-09-20 | status VERBATIM
  Q (as put, L41342, the seat's last message before his answer, restating what L41290 and L41330 tabled): "Your three decisions
    remain open: 1. Iron Storm representation: A, B or C. I recommend A, then B. 2. STP connector findings: do I file the tickets,
    or hand you the write-up? 3. rtiexec restart: a yes or no on one restart for the NotifyLevel-0 test. It is the one lead that
    could turn the create defect from "worked around" into "fixed"." L41290 tabled the options (A a demo-scale cut on today's
    representation; B the aggregate-level profile; C re-author in STP) with "My recommendation is A now, then B."
  A (owner, L41350): "1.  A and the B sounds right. 2. File Jira issues. 3. RTI restart as needed."
  supervisor reading: memory project-demo-rulings-2026-09-20 renders this "A and then B"; he typed "A and the B sounds right".
  pointer: memory project-demo-rulings-2026-09-20; DEMO_READINESS row 16; plan-aggregate-level-profile.

RL-20260921-01 | 2026-09-21 | status VERBATIM
  Q (as put, L41754 and L41880, both still open at 00:31Z): (a) T14 crosses a lake - "The decision I need from you on T14. I
    recommend the first option. [...] (1) I apply the 800 m-west destination nudge"; (b) de-stack A/C/D - "My recommendation is A
    plus C. [...] I need your word before building this." L41754 also says of the 0.9 nav bar: "Reopening that ruling is your call.
    I'm proceeding under it: FAIL." Restated at L42014, the seat's last message before his answer: "Still open with you: - de-stack
    (A plus C recommended); - T14's lake crossing (the 800 m nudge recommended); - the 0.9 nav bar for lake-district terrain."
  A (owner, L42017): "As recommended"
  supervisor reading: two words, answering the recommendations open at 00:31Z; they do NOT make the 0.9 nav bar an owner
    ruling - see RL-UNVERIFIED-NAV09. The recommendation he approved said "about 300 m for platoons" (L41880); the record
    at DEMO_READINESS row 23 and RUNBOOK sec 4 says 350 m. That gap is OPEN and is not resolved by this entry.
  pointer: RUNBOOK sec 4 (traversal bar, platoon sibling spacing); DEMO_READINESS row 23.

RL-20260921-02 | 2026-09-21 | status VERBATIM
  Q (as put, L43949): "1. The leftover rtiAssistant (pid 48392): may I close it? [...] 2. Iron Storm cut A: which path? [...]
    Option A is one pre-registered diagnostic drive across that band. [...] Option B is a thin T14-only demo cut. [...] 3. Demo
    clock: fast or real time? [...] I recommend real time for an audience. [...] 4. The crash call stack: do you want it read?
    [...] 5. MAK support case for STP-825: open it or not?"
  A (owner, L43952): "1. Close. 2. A. 3. Fast. 4. Read. 5. Open."
  supervisor reading: item 3 overrides the seat's own recommendation. Item 5 is five characters long: "5. Open." Any longer
    quotation of it in a repo file is a fabrication.
  pointer: RUNBOOK sec 0.5 (callstack), the MAK-case paragraph and the demo-clock paragraph; DEMO_RUNBOOK sec 4; HANDOFF
    CLOSED list, the 2026-09-21 rulings line.

RL-20260921-03 | 2026-09-21 | status VERBATIM, its "USER RULING" LABEL WITHDRAWN by RL-20260921-05 - do not delete it
  Q (as put, L44947): "The STP-857 ruling. What should the bus say when a move task's timer expires on a unit that never
    moved? The options are in the ticket. My recommendation is option 1: report an abort, "armed end reached with no
    movement", where the current build reports complete." Restated at L45083, the last seat message naming it before his
    answer: "The STP-857 ruling on how to report a unit that never moved when its timer completes."
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

RL-20260921-06 | 2026-09-21 | status VERBATIM - ANSWERED S555
  Q (as put, S291 AskUserQuestion; echoed in the S304 tool_result, type=user): "The re-clamp feature merged today (re-measure units
    after creation, put them on the ground, refuse to task units found off the terrain) is broken on not-yet-streamed terrain [...]
    What should its default be until it is rebuilt?" Options (the seat's labels): Off by default / Leave it on / Decide later.
  A (owner, S304): "Looks like this "re-clamp thing" is useless, or actually harmful. Does not improve the situation it is meant to
    help with, and messes up with legitimate tasks. Is that it? If so it should be removed, not switched off. But I may have misread
    what you said."
  Q (as put, S339 AskUserQuestion; echoed in the S344 tool_result, type=user): "Re-clamp, given the three parts above (placing works
    per one run; self-check never succeeds; the refuse-to-task gate harms legitimate tasks on cold terrain). [...] Your earlier answer:
    "If so it should be removed, not switched off." Which?" Options (the seat's labels): Remove it (revert) / Switch it off / Not now.
  A (owner, S344): "Confirm first that my interpretation of what you described is correct"
  A (owner, S441, typed, after the seat confirmed on half the evidence): "Reclamp: how can you say that harvest verified it
    works and in the next sentence that placement verification never succeeds? This smells of instrument error (not to
    mention the bizarre logic)"
  The seat checked and RETRACTED that confirmation. The three options then open were: remove it all / keep the placing and drop the
    gate and its tally / leave it until a re-cut.
  A (owner, S555, typed, 2026-09-21 19:08Z): "Not sure why you are still dancing around reclamping: it works, right? Then there is
    nothing to argue about it, or is there?"
  supervisor reading: his question was rhetorical; the seat's answer (S566) "it stays" drew no objection, and no later owner message
    in that session touches the re-clamp. The placement stays; the dispatch gate (it holds tasks on an unconfirmed read-back) and the
    false "32 NEVER MEASURED" tally are the defect, to be fixed in a PLAN-gated code unit; no revert of 8aeb127.
  MEASUREMENT, n = 1, seat-verified in runs\20260921T143243Z_run (our own files only): vrfc2simapp.log :626 "Terrain profile reply 43: 32
    sample(s)"; :628-:690 thirty-two "MEASURED OFF THE TERRAIN - live -0.0 m vs terrain <h> m ... Issuing setLocation"; :20461 "0
    RE-CLAMPED AND VERIFIED ... 32 NEVER MEASURED"; :772/:784 T1 and T13 HELD; :20307/:20345 ABANDONED at 60.0 s; :20311/:20347 follow-on
    tasks SKIPPED. watchvrf-trace.csv POS rows: the 32 corrected objects sit at exactly those terrain heights for every sample (min =
    max), while 4/278_ACR (6e3c0605..., created on the fallback, not enrolled) stays at -0.0 m for 1,196 samples.
  SEPARATELY, as implications and not as cause: the feature's tally disagrees with the feature's own log; the gate ended two tasks on
    units an independent trace shows on the terrain. NOT ESTABLISHED: there is no observer sample of the 32 from BEFORE the correction;
    the setLocation lines carry no timestamp; why the read-back never landed is undiagnosed.
  pointer: docs\CORRECTIONS_LOG.md F-2 (and its 2026-09-25 correction); commit 8aeb127; the HANDOFF CLOSED list re-clamp line.

RL-20260921-07 | 2026-09-21 | status VERBATIM
  Q (as put, S291 AskUserQuestion; echoed in the S304 tool_result, type=user): "Jira STP-857 (explained above) carries a comment
    'RULING (Paulo, 2026-09-21): option 1...' under your account. NO RULING FOUND on posting to Jira in your name - searched: handoff
    sec 2, memory feedback-push-when-tested. What should happen to the ticket?" Options (the seat's labels): Post a correction /
    Show me the text first / Leave Jira alone. "Premise was false" is not in the question: the seat's S290 message has the heading
    "Why the premise was false." and the description of "Post a correction" says "the ticket's premise was false".
  A (owner, S304): "As for 857, a unit that is meanto to move to an objective and perform some action, but instead gets
    stuck in the middle of the way certainly did not complete, so not sure why you say the "premisse was false".  My
    caveat was that you should not expect every task to require a movement, and abort in case the unit stays put."
  supervisor reading: the SYMPTOM is his too. The caveat is that not every task requires movement; the abort is what he
    wants FOR a movement task whose unit stays put. Do not compress the two halves into one rule.
  pointer: RL-20260921-03; Jira STP-857 (correction text drafted, NOT posted - awaiting him).

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
