# RULINGS - the ONLY place a ruling lives. Every other file points to an id and never restates the ruling.
# Format: RL-YYYYMMDD-NN | date the owner answered | status. Q = the question AS PUT. A = the OWNER'S OWN WORDS.
# status VERBATIM = Q and A both located, A inside a genuine owner-typed record. UNVERIFIED = searched, not found.
# Sources: L<n> = session a7f6a276-7ebc-4507-ac9d-c6bd361bd64e.jsonl; S<n> = session c3b364bd-a4ae-445a-b5c3-e585eaa5935c.jsonl (entry -09);
#   P<n> = session 5fc25950-1a10-4ade-9a7b-68cb5c1daf05.jsonl (entries RL-20260927-02 to -06, RL-20260928-01 to -04). Archived (cap): -08, 0914-02 on 09-26;
#   Q<n> = session 7207877b-f104-4515-91f7-c662e3fce85c.jsonl (1004-01..-06, 1005-01..-06). Archived 10-04/05: 0927-02..-06, 0928-01..-04.
#   0925-01, 0921-05, 0926-01 on 09-27; 0927-01 on 09-28; 0921-09 on 09-28 (for -03).
#   All are the 1-based PHYSICAL line `rg -n` prints; the record shape is named where it is not a plain type=user record.
# Owner text is EXACTLY as typed, misspellings included. Only transliteration: U+2019 -> ' and U+00EA -> [e^].
# "[...]" elides; the operative clause is never cut. A "supervisor reading:" line is scope only and binds nobody.
# A selected AskUserQuestion label is the SEAT's wording, recorded as a selection, never as the owner's words.
# Cap 120 lines / 160 chars. Overflow -> RULINGS_ARCHIVE.md, same format, no cap. Index of ids at the archive head.

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
RL-20261005-04 | 2026-10-05 | VERBATIM - Q3018, TYPED
  Q (seat, Q3015, explaining the open items): 1 extended labels for the verbose suffix now or after the demo; 2 keep RL-20260914-02
    (a task with no geometry runs where its unit is; missing geometry is STP's); 3 file the STP geometry ask; 4 the stray folder.
  A (owner, Q3018): "1 after. 2 keep. 3 file. 4 - are you able to do it safelly?"
  supervisor reading: (1) extended labels (the suffix field) AFTER the demo. (2) RL-20260914-02 KEPT: the interface derives no
    positions. (3) filed as Jira STP-962 (bug, To Do), with a comment on STP-848 (the interface now accepts the short form).
    (4) the seat removed the stray C:/C2SIMvrf-appdata-unattended: 3 junctions removed as links only (targets unchanged 8/9/24
    entries), then the junction-free folder (643 copied files); the real C:/C2SIM/vrf-appdata-unattended untouched.
  pointer: STP-962; STP-848 comment 13682; scratch laneIMPL (the doctrine research); HANDOFF_SEAT_2026-09-28 sec 3 item 3-4.
RL-20260928-05 | 2026-09-28 (recorded 2026-10-05) | VERBATIM - GitHub PR #1 comment by pbarthelmess, 2026-09-28T21:06:19Z
  Q (as put): the STP-850 PR's default for Vrf:StartTimeAnchor (HANDOFF_SEAT_2026-09-28 sec 2: "the DEFAULT anchor (Receipt) is
    the owner's to confirm").
  A (owner, PR #1 comment): "Owner decision (Paulo Barthelmess, 2026-09-28): `Vrf:StartTimeAnchor` default = `Receipt` (start =
    max(predecessor completion, receipt + (offset - order minOffset))). No code change needed - it is already the shipped
    default in appsettings."
  A (owner, Q3367, 2026-10-05, when the seat re-asked): "We already had this discussion. What does the record say?"
  supervisor reading: the default IS Receipt, decided 2026-09-28; the seat re-asked because the ruling sat in the PR comment, not
    in this ledger - recorded here now. PredecessorCompletion is the rollback, ReceiptAbsolute the no-rebase variant.
  pointer: PR #1 (fix/stp-850-start-at-max); the review scratch lanePR1; CORRECTIONS_LOG 2026-10-05.
RL-20261005-06 | 2026-10-05 | VERBATIM - Q3456, TYPED (queued mid-turn)
  Q (seat): the open items "2. Pace ... 3. Refused units on the map: keep, mark, or hide them. 4. T14: a 120 s settle hold."
  A (owner, Q3456): "In any case? 3 is keep. Tell memore about 4 if it is really new"
  supervisor reading: (3) units whose tasks the demo extent refuses STAY on the map as empty labelled containers (RL-20260927-03
    unchanged). (4) not new: the early TASKCMPLT is RL-20260921-09's arrival rule; the 60 s hold is the runner's window only; on
    the Way B demo path nothing stops the sim, so stragglers keep driving - dropped. (2) the time multiplier stays parked at 1x
    (2026-09-02 "ok on the multipliers for now"); the demo DurationScale (0.25 rehearsed, 1.0 in the profile) is not ruled.
  pointer: REHEARSAL_WAYB_HAND_2026-10-05 Result; DEMO_RUNBOOK 2.2 PACE.
RL-20261011-01 | 2026-10-11 | VERBATIM - R843 = session d0bba772-d00e-4b25-a434-607f22da850a.jsonl, TYPED
  Q (seat, markdown dropped): "1. Demo pace: 0.25 [...] or 1.0 [...]. I recommend 0.25. 2. The name line on the map: I recommend hiding it, so
    the audience sees only the clean label. 3. Application numbers for the demo (OP1): fixed numbers in the 9101-9199 block, or fresh ledger
    numbers per launch, as both rehearsals did. I recommend the ledger, because it keeps the house rule of one marker."
  A (owner, R843): "As recommended. On 2:Isn't the vrf name a GUID? We went back and forth on this multiple times. In any case, there is no
    reason to display this name, GUID or not on the map. The ask was to find a place to store the description (suffix after __) somewhere, if a
    viable slot were available in vrf, something a user could see when digging into details, for example. Ok not to include that if no obvious
    fit for a description exists."
  supervisor reading: (1) the Demo profile runs DurationScale 0.25. (2) the vendor NAME decoration is OFF on the map (the Label stays ON); the
    name is display only, identity is the UUID (RL-20260928-02). The "__" suffix goes in a VR-Forces field a user sees on inspection, else is
    dropped - after the demo (RL-20261005-04). (3) every demo federate takes a fresh number from the Appendix B marker; the 9101 block retires.
RL-20261011-02 | 2026-10-11 | VERBATIM - R1673 = session d0bba772-d00e-4b25-a434-607f22da850a.jsonl, TYPED
  Q (seat, markdown dropped, abridged per item): "1. Stall window [...] Recommendation: ratify 360 simulated seconds. 2. Platoon spacing
    [...] Recommendation: ratify 350 m. 3. Battalion fan-out (RL-20260914-04) [...] Recommendation: retire it [...] 4. Type-mapping
    default (RL-20260902-01) [...] Recommendation: (b) now, (a) after the demo. 5. Cleanup [...] Recommendation: yes, after you've seen
    that list."
  A (owner, R1673): "Go as recommended"
  supervisor reading: (1) RL-20260913-03's stall window is 360 SIM s (the 2026-09-13 calibration), not 120. (2) RL-20260921-01's
    platoon ring is 350 m. (3) RL-20260914-04 (battalion fan-out) is RETIRED; Vrf:SubordinateFanOut stays off. (4) base TypeMappingMode
    stays RealTemplates until after the demo; every Iron Storm profile is pinned to FidelityTable (ShippedProfileSelfTest s1); the
    base flip follows the demo. (5) stale worktrees/branches are deleted only after the owner has seen the itemised list.
RL-20261012-01 | 2026-10-12 | VERBATIM - R2436 = session d0bba772-d00e-4b25-a434-607f22da850a.jsonl, TYPED
  Q (seat, markdown dropped): "1. The yellow triangle: leave it on during the demo, or turn it off? My recommendation is to leave it on. [...]
    2. Ghosted icons: worth doing before the demo? [...] My recommendation is to defer it." (and later: "3. lower the 16 out-of-extent
    refusal lines from fail: to warnings")
  A (owner, R2436): "Ok to defer 1-2"
  supervisor reading: (1) the object-console warning icon stays as shipped (visible) for the demo; (2) the ghosted-icon declutter
    (Unit Display Settings / adsx) waits until after the demo. (3) asked again (R2477 "It can wait"): the 16 out-of-extent refusal
    lines stay fail: for now; lowering them to warnings is deferred.
RL-20261012-02 | 2026-10-12 | VERBATIM - R2788 = session d0bba772-d00e-4b25-a434-607f22da850a.jsonl, TYPED
  Q (seat): "which STP build will run at the demo? If it includes #180, the runbook as written is right. If it's older, the interface
    needs -ClientId "Not Set""
  A (owner, R2788): "Demo will be with current or better release"
  supervisor reading: the demo STP includes STP PRs #180 (SystemName STP) and #184 (CNFPSL/CNRPSL): -ClientId STP (DEMO_RUNBOOK 2.2). The
    rehearsed 09-20 export is OLDER than the demo STP - a rehearsal on a current-main export is owed (STP-883). Owner, next message: "Shooting
    to wrap 962 before the demo" - STP-962 (task geometry) is targeted for the demo build.
RL-20261013-01 | 2026-10-13 | VERBATIM - R4261 = session d0bba772-d00e-4b25-a434-607f22da850a.jsonl, TYPED
  Q (seat, R4258): "does the demo show STP's own map updating from what VR-Forces reports?"
  A (owner, R4261): "Yes, I'd like that. Is the feature fixed in stp?"
  supervisor reading: the demo shows STP's map driven by the interface's reports, so STP-1108 (positions logged as unknown uuid)
    and STP-800 (TaskStatus/Observation discarded) are demo-required; both To Do with no PR on 2026-10-13. A live round-trip check is owed.
RL-20261014-01 | 2026-10-14 | VERBATIM - R5681 = session d0bba772-d00e-4b25-a434-607f22da850a.jsonl, AskUserQuestion SELECTION
  Q (seat, R5680): "STP's phase slots go from 10 to 60 minutes (D1). [...] asks me to change the Demo profile's DurationScale from 0.25
    (your ruling RL-20261011-01) to about 0.04. Which do you mean?"
  A (owner, R5681, a selected label - the seat's wording): "DurationScale 0.04, at #413 (Recommended)"
  supervisor reading: amends RL-20261011-01 (1): VR-Forces stays at 1x; the Demo profile's DurationScale becomes ~0.04 (0.25 x 10/60)
    WHEN STP #413 (D1, PhaseDuration 60) merges and the demo STP build carries it - not before. 0.25 until then.
RL-20261014-02 | 2026-10-14 | VERBATIM - R5730 = session d0bba772-d00e-4b25-a434-607f22da850a.jsonl, AskUserQuestion SELECTION
  Q (seat, R5729): "#413 (60-minute slots) is merged to STP main as 7e475c9d. [...] Will the demo run an STP built from 7e475c9d or later?"
  A (owner, R5730, a selected label - the seat's wording): "Yes - switch to 0.0417 now (Recommended)"; also selected: "Get the list,
    propose a mapping (Recommended)" for STP's new task codes (#417, #429).
  supervisor reading: RL-20261014-01's trigger is met - the Demo profile runs DurationScale 0.0417; the demo STP is built from 7e475c9d+.
RL-20261014-03 | 2026-10-14 | VERBATIM - R5827 = session d0bba772-d00e-4b25-a434-607f22da850a.jsonl, AskUserQuestion SELECTION
  Q (seat, R5819): "Map STP's five new task codes (EVACT, HoldInPlace, CONSTR, HIDE, UNCONW) as in the table above?"
  A (owner, R5827, a selected label - the seat's wording): "As proposed (Recommended)"
  supervisor reading: EVACT -> move to the point and hold; CONSTR -> move to the obstacle, hold, one 'construction not simulated'
    observation; HoldInPlace, HIDE, UNCONW -> held where the unit is. ROE as ordered for all (HIDE's 'hold fire' is NOT added).
