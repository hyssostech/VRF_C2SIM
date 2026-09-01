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
3. [DEMOTED BY P2c, run 20260901T211310Z] The HQ-section formation-name mismatch is a
   COSMETIC warning: the STOCK template works end-to-end under correctly-authored
   vertices (the documented working-formation fallback covers it). The P2 aliases were
   UNNECESSARY and are reverted (stock file in place; .aliased-20260901 kept as
   history). NO defect report to MAK - at most the variant-A observation in
   docs/MAK_NOTES_DRAFT_2026-09-01.md.
4. Route-vertex altitude frame: 100-m-MSL vertices are the Users-Guide-warned
   authoring ERROR; above-terrain (Live mode, the default) is the documented frame and
   fills the company's working routes. Fixed100 remains only as a golden-parity relic.

## WORKING CONFIGURATION
TypeMappingMode=RealTemplates (default) + GroundWaypointAltitudeMode=Live (default) +
NavArea disabled. STOCK templates, no env overrides - an untouched product at default
settings (P2c-final). Vendor defects found across the whole saga: ZERO. Runner hardening now
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
3. MAK: (a) NO defect report (P2c: warning is cosmetic, stock template works) - at most
   the optional observation in docs/MAK_NOTES_DRAFT_2026-09-01.md; (b) 5.x Developer's
   Guide dropped the aggregate/organization chapters (empty index hrefs) - ask for the
   5.x source or confirmation 4.10 is authoritative. License renewal in process per the
   user (2026-09-01); installed .lic ends 2026-09-15 - verify the new file landed
   before running after that date.
4. VR-FORCES 5.2b IS EXPECTED SOON (user, 2026-09-01). Upgrade checklist when it lands:
   (a) diff its EXPANDED AGGREGATE MODEL SET against the 54 pending type adjudications
   BEFORE authoring anything (PRIOR_ART Q1: 5.2 shipped new NATO/Russian formations);
   (b) 5.2's "ground path planning enhanced with vector-based terrain data" touches
   exactly today's route/clamp machinery - re-run R9 on 5.2b and RE-ADJUDICATE before
   trusting any 5.0.2-era behavioral conclusion; (c) migrate local state DELIBERATELY:
   vrfSim.mtl notify levels (re-apply), HQ-section aliases (per P2c verdict), the
   DISABLED NavArea artifact (do NOT carry it into 5.2's SharedData), runner/env paths
   pinned to vrforces5.0.2, and a full VrfBridge /t:Rebuild + 7-copy redeploy against
   the new libs; (d) read the API migration guides first
   (docs.mak.com/api/vrforces5.2/classref/vrf_migration50.html + vrf_migration51.html).
5. PROBE TURNAROUND (user-approved 2026-09-01, adopt at the next natural break; do
   NOT change mid-protocol): (a) probe windows -RunSecs 420 (today's settles were all
   t<220; the 900 s figure came from a misread since retracted); (b) author a
   SHORT-ROUTE probe order variant of R9 - platoon/entity legs can be ~200 m, but
   COMPANY probe routes stay >= ~1 km (formation depth ~430 m + leading-edge
   completion - a route shorter than the formation confounds the read); (c) higher
   TimeMultiplier: P3 A/B DONE (docs/experiments/PREREG_P3_TIMEMULT5_2026-09-01.md,
   run 20260901T221227Z): the clock ran 5.0x and all endpoints matched P2c, BUT the
   company never completed (one follower, M1A2 18, never fired follow-in-formation
   completion while sitting 1.4 m from its 1x endpoint) -> falsifier fired, STOP.
   PROBE RUNS STAY AT 1x. 5x remains NECESSARY for COA-STP1 scale (13-40 km routes)
   so the miss must be understood first: docs-first (4.10 disaggregated-movement /
   follow-in-formation pages), then ONE registered repeat at 5x to split
   "5x-induced" from "run-to-run nondeterminism" (n=1). Keep ONE canonical-length
   1x run per milestone for comparability with the record.
   (d) RUNNER TURNAROUND - IMPLEMENTED OFFLINE, PENDING CONFIRMING RUN (branch
   runner-turnaround, not merged; docs/RUNNER_TURNAROUND_2026-09-01.md, RUNBOOK
   0.5.11): the WatchVrf/ListenReports trace now ends with the window via a
   stop-file the runner touches at StopIface + Trail (removes the measured 8 min 21 s
   dead time per run; tools take their normal resign/disconnect path, nothing killed;
   capability-probed so an old deployed binary falls back to the record's
   behaviour), and an OFF-by-default `-StopWhenComplete` closes the window once every
   taskee has TASKCMPLT + `-SettleHoldSecs` 60 (RunSecs stays the cap). To confirm:
   merge, rebuild + redeploy WatchVrf and ListenReports, run R9 at 1x with the
   defaults, check the design note sec 4 list; a missing `# STOP requested via
   stop-file` line or a stale federate on the next launch is a STOP.
6. Backlog unchanged: remaining type adjudications (54 units - but see item 4a first),
   task vocabulary, completion re-keying, scoring (Phase 5).

## OPERATIONAL STATE (end of 2026-09-01 session)
VR-Forces DOWN (second StopVrf pass exit 0; the first left vrfGui - the known
intermittent GUI-quit failure; nothing was killed). RTI RESIDENT + ANSWERED
(rtiAssistant 41336 / rtiexec 224608 / rtiForwarder 76620 at last inventory - inventory
fresh at start, do not trust PIDs). C2SIM docker UP. appNo marker NEXT FREE = 3655
(runs today consumed 3606-3654; P3 = 3648-3654). All work committed + pushed (see git log).

## NON-NEGOTIABLES (unchanged plus the docs-first rule above)
One variable per probe; prediction + falsifier + DOC CITATIONS before running; movement
gate = static->moving->settled + POS/RPT agreement; never kill a joined federate; never
kill rtiAssistant/rtiexec/rtiForwarder without a fresh ruling; fresh ledgered appNo per
join; ASCII in tracked files (ripgrep, not grep -P); after two consecutive infra
failures, research before retry. FALSIFIED stamps are LAYER-RELATIVE (L9): when a new
blocker layer is found, re-adjudicate old falsifications before trusting their fences.
