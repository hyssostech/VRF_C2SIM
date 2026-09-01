# SESSION-JUMP HANDOFF (2026-09-01) - THE R9 ORDER EXECUTES 3/3; docs-first is law

THE CURRENT entry point (newest HANDOFF_*.md by git log). SUPERSEDES
HANDOFF_2026-07-23_TYPEFIX_CONFIRMED.md. ASCII only. RE-VERIFY load-bearing claims
against artifacts before trusting prose.

## THE STANDING RULE (user directive 2026-09-01, enforced)
DOCUMENTATION FIRST for any issue: local help -> the PUBLIC Developer's Guide at
docs.mak.com/api/vrforces{5.2,5.1.1,4.10}/classref/ -> internet research. A live probe
is registrable ONLY after its prereg cites the documentation consulted. Two months of
probing dissolved in one afternoon of reading (2026-09-01). See VRF_GROUNDWORK_PLAN
lessons L8-L10 for the relapse mechanisms this rule exists to break.

## ONE-LINE STATUS
The R9 Mojave order executes END-TO-END, HEADLESS, ALL THREE TASKEES (platoon, company,
single entity), telemetry-verified arrivals + TASKCMPLT, run 20260901T203702Z. Full
chain of evidence: docs/experiments/PREREG_P1_FIXED100_ENTITY_2026-09-01.md (5
pre-registered runs, outcomes inline) + docs/RESEARCH_MECHANISMS_2026-09-01.md.

## THE FOUR-LAYER BLOCKER STACK (all peeled 2026-07-22 .. 2026-09-01)
1. Type mapping (ArmorPlatoon -> real Tank Platoon (USA)); fixed 07-22, default
   RealTemplates.
2. The project's OWN 2026-07-14 generated NavArea (120k tiles) made units inside it
   wait forever for nav data (Info-level, invisible at default console verbosity).
   NOW DISABLED: moved to SharedData/16/latest/TerrainData/navData/_disabled_20260901/
   (restorable; KEEP DISABLED unless deliberately regenerating nav data).
3. Stock MAK content defect: Formation-*-Armor-Co(US).frm assign the HQ section
   formation names its template lacks. FIXED by alias entries in "Tank Headquarters
   Section (USA).entity" (backup .bak-20260901). Report to MAK with the verified fix.
4. Route-vertex altitude frame: 100-m-MSL vertices are the Users-Guide-warned
   authoring ERROR; above-terrain (Live mode, the default) is the documented frame and
   fills the company's working routes. Fixed100 remains only as a golden-parity relic.

## WORKING CONFIGURATION
TypeMappingMode=RealTemplates (default) + GroundWaypointAltitudeMode=Live (default) +
NavArea disabled + HQ-section aliases. No env overrides needed. Runner hardening now
permanent: Stage 2b boot-dialog watcher (scripts/AnswerRtiDialog.ps1; the RTI dialog is
ONCE PER REBOOT), Stage 2c RTI gate, per-run capture of bin64 vrfSim.log/vrfGui.log.
vrfSim.mtl: notifyLevel 3 / objectConsoleNotifyLevel 3 / enableLogFileTimestamps 1
(backup .bak-20260901) - KEEP: this is what made the freezes speak.

## NEXT (in order)
1. DOCUMENTED-FRAME HARDENING: author route vertices from the back-end's own terrain
   via DtIfRequestTerrainProfileInformation (vrfmsgs/ifRequestTerrainProfileInformation.h)
   instead of the live-altitude+50 approximation. Facade+bridge+app change; offline
   gates then one confirming run.
2. COA-STP1 SCALE RE-RUN on the clean state (the July scale results predate ALL FOUR
   fixes; every FALSIFIED stamp from July is layer-relative - see L9 - and the region/
   fan-out story needs re-adjudication).
3. MAK: (a) formation content defect report WITH the verified fix; (b) 5.x Developer's
   Guide dropped the aggregate/organization chapters (empty index hrefs) - ask for the
   5.x source or confirmation 4.10 is authoritative. License renewal in process per the
   user (2026-09-01); installed .lic ends 2026-09-15 - verify the new file landed
   before running after that date.
4. Backlog unchanged: remaining type adjudications (54 units), task vocabulary,
   completion re-keying, scoring (Phase 5).

## OPERATIONAL STATE (end of 2026-09-01 session)
VR-Forces DOWN (second StopVrf pass exit 0; the first left vrfGui - the known
intermittent GUI-quit failure; nothing was killed). RTI RESIDENT + ANSWERED
(rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620 at last inventory - inventory
fresh at start, do not trust PIDs). C2SIM docker UP. appNo marker NEXT FREE = 3641
(runs today consumed 3606-3640). All work committed + pushed through c211513.

## NON-NEGOTIABLES (unchanged plus the docs-first rule above)
One variable per probe; prediction + falsifier + DOC CITATIONS before running; movement
gate = static->moving->settled + POS/RPT agreement; never kill a joined federate; never
kill rtiAssistant/rtiexec/rtiForwarder without a fresh ruling; fresh ledgered appNo per
join; ASCII in tracked files (ripgrep, not grep -P); after two consecutive infra
failures, research before retry. FALSIFIED stamps are LAYER-RELATIVE (L9): when a new
blocker layer is found, re-adjudicate old falsifications before trusting their fences.
