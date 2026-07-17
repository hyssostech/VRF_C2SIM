# PRIOR ART SURVEY - how other teams handle the C2SIM / VR-Forces problems we hit

Executor E8 web-research deliverable (2026-07-17). Doc-only; no code touched.
Purpose: survey the PUBLIC record for how other teams deal with the five problem
classes this port has hit integrating C2SIM (SISO standard) with MAK VR-Forces 5.0.2
driven remotely over HLA/RPR-FOM. Our C++ oracle is the George Mason University (GMU)
OpenC2SIM "c2simVRF" reference interface (Pullen et al.).

ASCII only. This is external evidence, gathered to inform Phase 2/3/5 of
docs/VRF_GROUNDWORK_PLAN.md and the eventual MAK-support question set.

---

## 0. How to read this doc (provenance and its hard limits)

Every load-bearing claim is tagged so the reader can weight it:

- [PRIMARY-FETCHED]  = I fetched the actual page and read its text this session.
- [SEARCH-EXTRACT]   = the claim comes from a search-engine result summary/snippet;
                       the primary URL is given but I could NOT parse it in-session.
                       Treat as a lead to verify, not settled fact.
- Relevance tags per finding: FOUND-AND-RELEVANT / FOUND-BUT-TANGENTIAL / NOT FOUND.

HARD ENVIRONMENT LIMITS THIS SESSION (these shape every "NOT FOUND" below - read them):
1. MAK's own technical docs (class reference, VR-Link/VR-Forces API HTML, KB) live on
   `ftp.mak.com`, which did NOT resolve in this environment (DNS ENOTFOUND) on every
   attempt. MAK's customer support portal is behind authentication and was not
   reachable at all. So "no public record" for MAK-internal behavior means "no
   OPEN-web record I could reach" - it does NOT rule out a MAK KB article or support
   thread behind their login. This is the single biggest caveat in the doc.
2. Most NATO STO, SISO, and GMU primary sources are PDFs that failed to parse
   in-session (compressed-stream binaries the fetch converter could not decode:
   netlab.gmu.edu also had a TLS cert failure; apps.dtic.mil rate-limited 429;
   sisostandards CDN and sto.nato.int returned binary/403). Their existence, titles,
   authors, and years are confirmed via multiple search results; their DETAILED
   contents below are [SEARCH-EXTRACT] unless noted. The URLs are given so a human
   (or a session with working PDF extraction) can open them directly.
3. HTML pages on `www.mak.com` and `github.com` fetched cleanly - those are the
   [PRIMARY-FETCHED] anchors.

Date discipline: the GMU MSDL/C-BML VR-Forces paper is 2012 - it PREDATES both the
C2SIM standard (SISO-STD-019, 2020) and VR-Forces 5.x. It is lineage/ancestor
evidence, not evidence about 5.0.2 behavior. C2SIM became a NATO standard under
STANAG 4856. VR-Forces 5.2 material is 2025 (we run 5.0.2). Dates are stamped per item.

Not treated as independent evidence: anything on `openc2sim.github.io` or the C++
`c2simVRF` user guide - that is OUR oracle's own artifact family (the repo we live in).
It is cited only where it states a fact about the C2SIM SERVER/STANDARD that a third
party would also rely on, and is flagged [OUR-FAMILY] when used.

---

## Q1. Type mapping to the target sim's content; is authoring custom content normal?

VERDICT: FOUND-AND-RELEVANT (partial). The standard side is well documented; the
"map the standard's hierarchical enumeration onto a specific installed VRF model set,
without generic fallbacks" step - our actual pain - is essentially undocumented in the
open literature. Authoring/curating VRF content IS normal practice (MAK ships and
keeps extending model sets), but no public account describes an integrator authoring
custom .sms content specifically to raise C2SIM type fidelity.

Findings:

- The C2SIM standard carries entity type as a HIERARCHICAL ENUMERATION, DIS-style:
  "Entity Type is a set of category enumerations defining the type of an Entity for
  representation in simulations ... a string can have several separated fields that
  can each hold an enumeration, thus identifying a category at each level of a
  hierarchy." [SEARCH-EXTRACT] SISO-STD-019-2020 (C2SIM), 2020.
  URL: https://cdn.ymaws.com/www.sisostandards.org/resource/resmgr/standards_products/siso-std-019-2020_c2sim.pdf
  Read: the standard defines the SOURCE type space (hierarchical enumeration + an
  ontology "Standard Military Extension / SMX"), but it does NOT prescribe how you map
  that onto a target simulator's installed templates. That mapping is the integrator's
  job - exactly the seam where our generic-fallback problem lives.

- MAK's normal answer to "the content I need is not there" is AUTHORED/CURATED MODEL
  SETS, and MAK actively extends them: VR-Forces 5.2 (2025) shipped an "Expanded
  Aggregate Simulation Model Set: New units for NATO and Russian formations extend
  VR-Forces' constructive simulation capabilities." [PRIMARY-FETCHED]
  URL: https://www.mak.com/learn/blog?view=article&id=491 (VR-Forces 5.2, 2025).
  Read: our missing engineer / mech-inf / mortar aggregates are precisely the class of
  content MAK adds via new model-set releases - i.e. authoring/curating aggregate
  templates is the sanctioned path, not a hack. (We are on 5.0.2; 5.2 may already
  contain some of what our loaded chain lacks - worth checking at upgrade time.)

- VR-Forces natively imports externally-authored order-of-battle content: "VR-Forces
  lets you import externally defined data into scenarios, such as MSDL, airspace
  control orders, and linear, areal, and point objects defined in CSV files."
  [PRIMARY-FETCHED] MAK VR-Forces Capabilities (current, MAK ONE era).
  URL: https://www.mak.com/mak-one/apps/vr-forces/capabilities

- The GMU ancestor interface established the "map MSDL/C-BML unit descriptors into
  VR-Forces objects" pattern: "An Open Source MSDL/C-BML Interface to VR-Forces",
  Pullen & Ababneh, 2012 Fall SIW (paper 12F-SIW-036). [SEARCH-EXTRACT - full text
  not parseable in-session; title/authors/year/venue confirmed across 4 result sets].
  URLs (none fetchable this session): https://netlab.gmu.edu/pubs/12F-SIW-036-slides.pdf
  ; https://www.semanticscholar.org/paper/953ef6b7221a83a675747f278c22ab88523701b0
  Caveat: 2012, pre-C2SIM, pre-VRF-5.x. Lineage only.

WHAT TRANSFERS (actionable):
- Our Phase 2.1 mapping table is the right shape of work; the open literature offers no
  ready-made SISO-enum -> VRF-template crosswalk to borrow. This is ours to build.
- Treat "author/curate a small custom .sms model set for the aggregates our chain
  lacks" as NORMAL VR-Forces practice, not a workaround - MAK does exactly this per
  release. Check whether VRF 5.2's expanded aggregate set closes our engineer/mech-inf/
  mortar gaps before hand-authoring them.
- Because the standard's entity type is a DIS-style hierarchical enumeration, our
  nearest-match logic should key on that hierarchy (category levels), which is the same
  representation VRF templates carry - not on SIDC/echelon alone.

---

## Q2. Does anyone publicly document VR-Forces remote-tasking problems?

VERDICT: mostly NOT FOUND in the open web for OUR specific symptoms (premature/missing
task completion, aggregate stalls, remote-vs-GUI taskability). What IS public:
(a) MAK does not ship its own C2SIM interface - the GMU c2simVRF is THE reference,
so "the interface" and "our oracle" are the same lineage everyone uses;
(b) integration issues surface in a PUBLIC standard tracker, but they are about the
STANDARD's schemas/messages, not VRF execution quirks; (c) NATO CWIX test campaigns
report pass/limited/fail counts but at coarse grain.

Findings:

- MAK itself relies on the GMU interface for C2SIM and does not publish a competing one:
  "VR-Forces demonstrated simulation-C2 interoperability through C2SIM, using a
  C2SIM-CGF interface developed at George Mason University C4I Center." Also: "Doug
  Reece, a Principal Engineer on MAK's VR-Forces team, is the Vice Chair of the C2SIM
  PDG, and Chair of the associated Product Support Group (PSG)" and "the PSG
  established a Github site for tracking C2SIM Standard change requests and questions."
  [PRIMARY-FETCHED] MAK "Simulation Standards Activity Update" (SIW pre-read, ~2022-23).
  URL: https://www.mak.com/test?view=article&id=240&catid=19
  Read: there is a MAK insider (Reece) steering the C2SIM standard, and a PUBLIC GitHub
  channel for questions/change-requests. That GitHub is our best open-web venue to
  raise or find our issues - see next item.

- The public C2SIM change-request / problem-report tracker exists and is browsable, but
  its issues are STANDARD/SCHEMA level, not VRF-execution level. Representative open
  issues: #62 "SystemEntity is missing uuid property (useful for mapping to NETN)",
  #66 "Write SISOEntityType as a single string", #63 "Side and Superior mandatory in
  EntityDescriptor", #71/#72 server responses "not following the standard". [PRIMARY-
  FETCHED] URL: https://github.com/OpenC2SIM/C2SIMArtifacts/issues
  Read: NONE of the visible issues concern VR-Forces task completion, aggregate
  movement stalls, or remote-created-vs-authored taskability. The community's public
  friction is with the message standard, not with VRF's interpretation of orders.
  That is a meaningful NULL: our behavioral problems are not (openly) other teams'
  reported problems.

- MAK's C2SIM PSG did work on representation problems adjacent to ours - "specifying
  ordering for related objects in the ontology, defining different task organization
  structures, and handling entity additions to the C2 structure after simulation
  starts" (planned for CWIX 2023). [PRIMARY-FETCHED] same MAK URL as above.
  Read: "task organization structures" and "entity additions after start" are cousins
  of our org-tree / remote-creation concerns, but the writeup does not describe
  premature completion or movement stalls.

- NATO CWIX campaigns report outcomes at test-count grain, and DO surface real defects:
  CWIX 2018 - "Out of seventeen tests, fourteen were completely successful, three could
  not be held due to network problems, and two were classed as 'limited success' due to
  software issues that were resolved" (systems incl. VR-Forces, JSAF(UK), KORA,
  NORCCIS/SWAP). CWIX 2023 - "23 successful tests, 9 cases with limited success, and 4
  cases where essential interoperability issues could be identified", and a concrete
  standard gap: "The PositionReport in the C2SIM standard currently has no provision for
  'speed' and 'heading', which is required in several C2 systems." [SEARCH-EXTRACT]
  Sources: "Validating M&S Standards Interoperation in CWIX 2022", Pullen et al., 2023
  SIW (Presentation 07):
  https://cdn.ymaws.com/www.sisostandards.org/resource/collection/5BEEB53B-7B13-4237-9CE9-A9AE5356C5B9/2023-SIW-Presentation-07.pdf
  ; NATO STO-EN-MSG-211 (Pullen, C2SIM educational notes, multiple 2023-2024 versions):
  https://publications.sto.nato.int/publications/STO%20Educational%20Notes/STO-EN-MSG-211/EN-MSG-211-2.7P.pdf
  Read: these confirm other teams hit "limited success / essential issues" but report
  them as counts, not as the fine-grained tasking pathologies we chase. No public CWIX
  writeup mentions leading-edge completion, ~18 km stalls, or co-located piles.

- The c2simVRF authors' OWN limitation statements ("HLA version believed to work
  properly ... task-follows-task"; "we do not plan to expand c2simVRF to the full
  capabilities of VR-Forces") are real and load-bearing for us, but they live in the
  C++ interface's user guide / GMU SIW slides - i.e. our oracle's own artifact family
  [OUR-FAMILY], not independent third-party evidence. Cited here only to record that
  the scope limit is the authors' stated intent, not a bug.

WHAT TRANSFERS (actionable):
- The public MAK-facing channel for our questions is the OpenC2SIM/C2SIMArtifacts
  GitHub issues and the C2SIM PSG - Doug Reece (MAK) chairs it. Our aggregate-stall /
  premature-completion findings, if we frame them as standard-vs-implementation
  questions, have a real venue there BEFORE (or alongside) a private MAK ticket.
- Nobody else has publicly solved our exact tasking pathologies. That raises the value
  of our own instrumentation (WatchVrf displacement oracle, the 0.6 console capture) -
  we are likely the first to characterize these at this resolution.
- The CWIX PositionReport "no speed/heading" gap is orthogonal to us but confirms the
  pattern that real integration defects are standard-level and get fixed upstream -
  supports routing findings to the PSG.

---

## Q3. MSDL import as the established alternative to per-unit remote creation

VERDICT: FOUND-AND-RELEVANT. MSDL import into VR-Forces is a documented, supported,
first-class path, and C2SIM initialization is explicitly the successor to MSDL v1 with
a server-side MSDL<->C2SIM translation. Loading an order of battle as an
initialization file/scenario is the established alternative to creating each unit via
the remote API.

Findings:

- VR-Forces imports MSDL directly (already quoted in Q1): "VR-Forces lets you import
  externally defined data into scenarios, such as MSDL, airspace control orders ..."
  [PRIMARY-FETCHED] https://www.mak.com/mak-one/apps/vr-forces/capabilities
  Read: MSDL import is a shipped feature, not a research prototype. It is the
  batch/order-of-battle equivalent of our per-unit createEntity/createAggregate loop.

- C2SIM initialization is the standardized superset of MSDL, and the GMU C2SIM server
  translates between them: "C2SIM Initialization supersedes the MSDL version 1 standard
  ... The C2SIM Reference Implementation Server supports initialization between MSDL and
  C2SIM initialization." Also the standard's intent: "C2SIM is intended to combine the
  functions of CBML and MSDL. MSDL provides for consistent initialization/start-up data
  for both C2 and Simulation systems." [SEARCH-EXTRACT / partly OUR-FAMILY - the server
  doc is on openc2sim.github.io].
  Sources: C2SIM Server Reference Implementation Documentation v4.8.2.3
  (https://openc2sim.github.io/C2SIMServerReferenceImplementationDocumentation4.8.2.3.pdf)
  [OUR-FAMILY]; corroborated by SISO-GUIDE-010-2020 and SISO-STD-019-2020 (both
  sisostandards CDN, not parseable in-session).

- MSDL/order-of-battle-as-initialization is an active current topic, not legacy: "C2SIM
  as a Mission Planning Tool Standard", E. Michael Bearss, 2024 SIW Paper 15.
  [SEARCH-EXTRACT] URL:
  https://cdn.ymaws.com/www.sisostandards.org/resource/collection/8FE7944A-71E0-42D9-9552-DEAB9FA71BA1/2024_SIW_Paper_15.pdf

- MSDL EXPORT from VR-Forces is documented in our on-disk 0.2 curriculum; the OPEN-WEB
  question was IMPORT support in 5.x - answered above (import is a listed capability).
  I did not find a public statement of MSDL-import LIMITATIONS in 5.x (e.g. which
  attributes survive the round trip). That specific gap is NOT FOUND publicly and is a
  candidate to verify empirically or ask MAK.

WHAT TRANSFERS (actionable):
- There is a sanctioned non-remote-API creation path: build the order of battle as an
  MSDL (or C2SIM init) document and IMPORT/LOAD it, rather than issuing N remote
  createAggregate calls. This directly addresses hypothesis (e) - remote-created units
  differing from authored ones - because an imported MSDL OOB is processed by the same
  scenario-load machinery the GUI uses. Worth a Phase-2/3 spike: import a small MSDL
  OOB and diff the resulting units against both our remote-created and GUI-created ones
  (feeds the 2.2 structure diff and the .scnx harness).
- The GMU server already does MSDL<->C2SIM translation, so a C2SIM init we already
  produce can, in principle, be rendered to MSDL for import - reuse rather than build.

---

## Q4. Faster-than-real-time: dead reckoning and reflected-state artifacts

VERDICT: FOUND-AND-RELEVANT for the MECHANISM (VR-Link's reflected state is
dead-reckoned/extrapolated - which is exactly our observer-side warp hypothesis);
NOT FOUND for any public warning about accuracy degradation at high time multipliers,
and NOT FOUND for any public account of who runs FTRT with external federates and how.

Findings:

- VR-Link reflected (remote) entity state is DEAD-RECKONED by default, not the last
  received value: "By default, the position, velocity, and orientation for a
  DtReflectedEntity ... are dead-reckoned values - not necessarily the values last
  received via state updates ... but extrapolated forward to the current value of
  VR-Link simulation time from acceleration, velocity, and angular velocity based on
  the entity's current dead-reckoning algorithm." VR-Link also applies optional
  SMOOTHING to mask DR-to-actual snaps. [SEARCH-EXTRACT] Source: MAK VR-Link class docs,
  "Other Simulation Concepts" / "Working with Remote Entities"
  (ftp.mak.com/out/classdocs/vrlink*/... - host did not resolve in-session; text is
  from search extract of that page).
  Read: this is direct vendor confirmation of the mechanism behind our ground-truth
  0.0 item 6(a) hypothesis - transient lockstep "warps" are consistent with an
  OBSERVER-SIDE DR artifact: a corrupt/thrashing member velocity extrapolated forward
  to sim-time produces an absurd position that snaps back on the next real update.
  It also means: what WatchVrf reads is DR-extrapolated by default; to get raw received
  state we must read the last update, not the DR value (the raw-vs-DR discriminator
  already registered as a WatchVrf enhancement candidate).

- Time multiplier / FTRT is a supported, documented control with NO published fidelity
  caveat: "Simulation time can be mapped one-to-one with wall clock time or it can run
  slower or faster than real time"; changeable "through the Time Multiplier toolbar ...
  or programmatically through the APIs, even while the simulation is running."
  [PRIMARY-FETCHED] https://www.mak.com/mak-one/apps/vr-forces/capabilities
  Read: MAK documents THAT you can run fast, not what breaks when you do. No public MAK
  statement was found on DR error growth, update-rate starvation, or reflected-state
  artifacts specifically at high multipliers.

WHAT TRANSFERS (actionable):
- Our "20x causes warps" observation aligns with a KNOWN, VENDOR-DOCUMENTED mechanism
  (DR extrapolation of reflected state), not a mystery: at 20x, sim-time advances 20x
  faster between network updates, so DR extrapolates 20x further per received packet -
  any velocity glitch is magnified. This strengthens the case that the warp is an
  observation artifact on our side of the boundary, not units actually teleporting.
- Concrete: implement the raw-received-vs-DR WatchVrf discriminator (read last-received
  state alongside the DR value). If the raw track is smooth while the DR track warps,
  the artifact is confirmed and the "runaway" reframes as a reporting artifact for the
  transient class (persistent underground end-states remain the real runaway class).
- No prior team has published FTRT + external-federate guidance we can lift; running
  the reference scenario at 1x vs 20x natively (Phase 1.4) is the right way to get our
  own ground truth, since the literature will not supply it.

---

## Q5. Terrain paging vs large ground movements

VERDICT: FOUND-BUT-TANGENTIAL. Terrain paging is documented as a feature and is enough
of a real-world pain point that MAK added dedicated DIAGNOSTICS in 5.2 (2025) - but I
found NO public report of ground movement STOPPING at a paged-terrain boundary, and NO
public "Terrain Page-In Area" remedy document. This remains largely MAK-support / novel
territory in the open web.

Findings:

- Terrain paging is a documented capability: "Terrain paging allows VR-Forces to load
  only the necessary parts of the terrain used for the simulation." [PRIMARY-FETCHED]
  https://www.mak.com/mak-one/apps/vr-forces/capabilities

- MAK considers paging enough of a problem area to instrument it in the current release:
  VR-Forces 5.2 (2025) added "Terrain Paging Diagnostics: New UI metrics expose paging
  performance and help users pinpoint terrain-loading bottlenecks", plus "CDB Terrain
  Performance: Major internal optimizations" and "Ground path planning is now enhanced
  with vector-based terrain data, improving movement accuracy for all vehicle types."
  [PRIMARY-FETCHED] https://www.mak.com/learn/blog?view=article&id=491
  Read: the fact that 5.2 ships paging DIAGNOSTICS and ground-path-planning improvements
  is indirect corroboration that paging/movement interactions are a genuine, known-hard
  area - but MAK frames it as performance/accuracy, and says nothing publicly about
  movers HALTING at a page boundary. Our ~18 km stall band is not described anywhere I
  could reach.

- The "Terrain Page-In Area" remedy referenced in our internal notes did not appear in
  any open-web MAK page or third-party writeup. NOT FOUND publicly (may exist in the
  on-disk User's Guide or the gated MAK KB - check locally / with MAK).

WHAT TRANSFERS (actionable):
- Upgrading past 5.0.2 could matter for movement: 5.2's terrain-paging diagnostics +
  vector-based ground path planning target exactly the "movers behave oddly over large
  terrain" symptom. If the stall survives Phase 1 native repro on 5.0.2, "does it
  reproduce on 5.2?" becomes a cheap, high-value question and a natural MAK-ticket
  framing.
- Because the specific stall-at-boundary symptom is undocumented publicly, Phase 1.2's
  leg crossing the 18.4 km band with NATIVE (GUI-tasked) units is the decisive test: if
  native units also stop there, we have a clean, novel repro for MAK; if not, the fault
  is on our side of the remote boundary.

---

## What nobody seems to have solved publicly (novel-territory / MAK-support list)

These are the items for which the open web yielded no solution or even a description.
Each is a candidate for a MAK support ticket and/or a C2SIM PSG GitHub question, and
each is where our own instrumentation is likely first-of-kind. (Caveat from sec 0: the
gated MAK KB was unreachable - some of these MAY be answered behind MAK's login.)

1. VR-Forces UNIT/aggregate route completion firing at the formation LEADING EDGE
   (premature by design) - and sometimes NEVER firing. No public remedy; no public
   position-based completion predicate. (Our fix: external displacement-gated arrival.)
2. Remote-tasked aggregates FREEZING in co-located "piles" / movers STALLING at a common
   ~18 km radius. Zero public reports; not in the C2SIM issue tracker; MAK docs silent.
3. Difference in TASKABILITY between remote-API-created units and GUI/scenario-authored
   units. No public characterization. (MSDL import - Q3 - is the sanctioned path most
   likely to close it, but nobody has published the comparison.)
4. Faster-than-real-time (20x) reflected-state WARPS with external federates. The DR
   MECHANISM is documented (Q4) but no team has published FTRT-with-external-federate
   guidance, artifact characterization, or a raw-vs-DR reading recipe.
5. Ground movers STOPPING at paged-terrain boundaries, and the operational use of a
   "Terrain Page-In Area" to prevent it. Feature exists conceptually; the symptom and
   remedy are undocumented in the open web.
6. A reusable SISO-C2SIM-enumeration -> installed-VRF-template crosswalk that avoids
   generic fallbacks and content gaps (engineer / mech-inf / mortar aggregates). The
   standard defines the source enumeration; nobody publishes the target-side mapping.

---

## Adversarial self-review (what this pass caught)

- SOURCE PROVENANCE (the big one): a first draft would have stated CWIX results, the
  VR-Link DR text, and the MSDL-supersedes claim as flat facts. They are [SEARCH-
  EXTRACT] - I could not parse the primary PDFs/class-docs in-session. Fixed by tagging
  every claim PRIMARY-FETCHED vs SEARCH-EXTRACT and giving the primary URL so the
  supervisor/user can verify. The only claims I fully stand behind as read-with-my-own-
  eyes are the www.mak.com HTML pages and the github.com issues list.
- FALSIFIER I could not close: "nobody has solved these publicly" has a live competing
  hypothesis - MAK's authenticated support KB (unreachable this session, DNS-blocked)
  may contain exactly these answers. The single observation that would falsify the
  novel-territory framing is a MAK KB article on aggregate-move stalls or leading-edge
  completion. I could not check it. So the "nobody solved" list is scoped to the OPEN
  web and explicitly says so; it must not be read as "MAK has no answer."
- DATE TRAP avoided: the strongest-looking VR-Forces interface paper (GMU MSDL/C-BML,
  2012) predates C2SIM (2020) and VRF 5.x. I demoted it to lineage evidence rather than
  letting a 2012 result speak for 5.0.2 behavior. VR-Forces 5.2 items are 2025 and are
  labeled as such (we run 5.0.2 - they inform upgrade decisions, not current behavior).
- OUR-REPO CONTAMINATION avoided: openc2sim.github.io and the c2simVRF user guide are
  our oracle's own family; I flagged them [OUR-FAMILY] and leaned on them only for
  server/standard facts a third party would equally rely on, never as independent
  corroboration of our own findings.
- Residual risk: a couple of MAK capability quotes are undated (MAK ONE marketing
  pages are living documents). They describe current shipping behavior, which is
  adequate for "is this normal practice" but should not be cited as version-specific.

---

## Searches / fetches that came up EMPTY or blocked (explicit null record)

- "VR-Forces units freeze / aggregate stuck / not moving" -> results were entirely
  CONSUMER-VR (SteamVR, VRChat, flight-sim) noise. No MAK CGF discussion exists at that
  phrasing; the name collision itself shows MAK VR-Forces troubleshooting is NOT in
  public consumer forums.
- No public MAK statement on FTRT / time-multiplier ACCURACY degradation or
  reflected-state artifacts at high multipliers (only "you can run faster than real
  time" - no caveat).
- No public account of WHO runs FTRT with external federates and HOW (no worked example).
- No open-web "Terrain Page-In Area" remedy doc; no public report of movers halting at
  paged-terrain boundaries.
- No public MSDL-import LIMITATION statement for VR-Forces 5.x (which attributes survive).
- C2SIMArtifacts GitHub issues: NO issue about VR-Forces task completion, aggregate
  movement, or remote-vs-authored taskability (searched the issue list directly).
- Could NOT fetch/parse in-session (existence confirmed, contents unread by me):
  netlab.gmu.edu 12F-SIW-036 (TLS cert fail); c4i.gmu.edu LS-141 papers (PDF binary);
  apps.dtic.mil MSG-145 AD1148021 / AD1183694 (HTTP 429); sisostandards CDN
  SISO-STD-019 / SISO-GUIDE-010 (PDF binary); publications.sto.nato.int STO-EN-MSG-211
  (HTTP 403); academia.edu / researchgate mirrors (HTTP 403); web.archive.org (blocked
  by fetch policy); ftp.mak.com class docs / VR-Link API HTML (DNS ENOTFOUND - the whole
  MAK technical-docs host was unreachable).

---

## Primary source ledger (title - year - URL - could I read it this session?)

- SISO-STD-019-2020, C2SIM standard - 2020 -
  https://cdn.ymaws.com/www.sisostandards.org/resource/resmgr/standards_products/siso-std-019-2020_c2sim.pdf
  - NO (PDF binary; contents via search extract).
- SISO-GUIDE-010-2020, C2SIM guidance - 2020 -
  https://cdn.ymaws.com/www.sisostandards.org/resource/resmgr/guidance_products_/siso-guide-010-2020_c2sim.pdf
  - NO (PDF binary).
- "An Open Source MSDL/C-BML Interface to VR-Forces", Pullen & Ababneh - 2012 Fall SIW
  (12F-SIW-036) - https://netlab.gmu.edu/pubs/12F-SIW-036-slides.pdf - NO (TLS fail).
  Lineage/ancestor evidence only (pre-C2SIM, pre-VRF-5.x).
- "Validating M&S Standards Interoperation in CWIX 2022", Pullen et al. - 2023 SIW
  Presentation 07 -
  https://cdn.ymaws.com/www.sisostandards.org/resource/collection/5BEEB53B-7B13-4237-9CE9-A9AE5356C5B9/2023-SIW-Presentation-07.pdf
  - NO (contents via search extract). Cross-CGF CWIX results (SWORD, VR-Forces, KORA...).
- NATO STO-EN-MSG-211, "Technical Description of C2SIM" / FMN M&S, Pullen - 2023-2024
  (versions 1.3P..3.0) - https://publications.sto.nato.int/publications/STO%20Educational%20Notes/STO-EN-MSG-211/EN-MSG-211-2.7P.pdf
  - NO (HTTP 403).
- MAK VR-Forces Capabilities - current (MAK ONE) -
  https://www.mak.com/mak-one/apps/vr-forces/capabilities - YES [PRIMARY-FETCHED]
  (MSDL import; terrain paging; time multiplier; aggregate/entity; remote control).
- MAK VR-Forces 5.2 announcement - 2025 - https://www.mak.com/learn/blog?view=article&id=491
  - YES [PRIMARY-FETCHED] (terrain paging diagnostics; expanded aggregate model set;
  vector-based ground path planning).
- MAK "Simulation Standards Activity Update" (C2SIM/PSG, Reece) - ~2022-23 -
  https://www.mak.com/test?view=article&id=240&catid=19 - YES [PRIMARY-FETCHED]
  (MAK uses GMU interface; PSG GitHub for change requests; task-org/entity-addition work).
- OpenC2SIM/C2SIMArtifacts issue tracker (C2SIM change requests/problem reports) -
  current - https://github.com/OpenC2SIM/C2SIMArtifacts/issues - YES [PRIMARY-FETCHED]
  (schema/type/message issues; NONE on VRF tasking behavior).
- MAK VR-Link class docs ("reflected entities are dead-reckoned/extrapolated") -
  ftp.mak.com/out/classdocs/vrlink*/... - host DNS-unreachable; text via search extract.
