# PREREG ADDENDUM - L1 / L2 / L5 aligned with the installed MAK docs (2026-10-15)

STATUS: written 2026-10-15, BEFORE any L1, L2 or L5 run. NO PREDICTION IS CHANGED. This file corrects
citations and reading notes in PREREG_L1_L2_DOCUMENTED_MOVE_2026-10-15.md (ec3583d),
PREREG_L5_FRAME_MODE_ADDENDUM_2026-10-15.md (8cf8f7c, c402e90) and HANDOFF_2026-10-15.md (9fc85a6), and adds one
observation-only logging step to L2 and one precondition to L5. Order LD-2026-10-15-09, ticket STP-976; ruling
D68 (the docs install) and the docs-first rule.

Sources (C:\MAK\vrforces5.2d\doc, installed per D68; every page below was checked in a page-tagged text
extraction of the PDF; printed page = PDF page):
- UG = VRFUsersGuide.pdf; AC = AddingContent.pdf; RN = VRF5.2ReleaseNotes.pdf;
- OPE = VRFSysdef_OPEParameterReference.html; LUA = the luadoc Task Catalog;
- the research report LD-2026-10-15-08 (STP-976 comment 14265), from which every item below is taken.

## (1) WITHDRAWN - "the docs are not installed stubs"

Withdrawn: the sentence in L1/L2 "Why live" ("The API reference, luadoc and help are 'Documentation Has Not
Been Installed' stubs on this host ...") and the last sentence of HANDOFF docs-first finding 5 ("The installed
classdoc / luadoc / help are 'not installed' stubs ..."). Both were true when written and are false now.
What is installed (D68): doc\VRFUsersGuide.pdf (1,805 pp), the other product PDFs (AC, RN, Migration Guide,
Interoperability Guide, First Experience, Quick Reference), classdoc (the Doxygen Developer's Guide), help (the
MadCap web help), luadoc (50 modules, incl. the Task Catalog), the OPE parameter reference and
ExtendedAggregateProperties.xlsx. No placeholder page is left.
The L1/L2 rationale stands on its other grounds: the docs say nothing about the back-end stop (STP-1163) or
about re-tasking the members of a moving aggregate; LD-08 lists both as NOT IN DOCS.

## (2) L2 Arm B is a documented user task - and how to read it

- Arm B (Vrf:AggregateMovePlanner=PlanPath) sends move_to_location_plan_path, which is the documented user
  task "Move to Location (Plan Along Roads)" (UG 35.5.11 p735: the unit moves to the nearest road, along the
  road network to the point nearest the destination, then off-road to the destination).
- Reading caveat 1: Arm B is PER-MEMBER planning (each member plans its own road path to its own offset). It is
  NOT the documented group road move of UG 28.3.2 p551 (one route generated for the group; subordinates queue
  along it and unpack at the destination). A clean B therefore implicates the per-member documented task, not
  the group move.
- Reading caveat 2: on roads the speed factor is 1, the top priority, against 0.65 / 0.25 on restricted L1 / L2
  terrain (forest and urban are L2 for mech, motorized and tank) (AC 7.8 p234-236). Arm B will reach its
  destinations at different times from Arm A; the second-dispatch time and per-step completion times are not
  comparable between arms as speeds. Compare the stop signature, not the arrival times.

## (3) NEW L2 logging step (observation only; no new prediction)

In every arm, for the second dispatch (STREND ENGAGE to the same point), record per member whether its task
completes within about one tick of issue ("already there", LD-08 Q3b). The docs do not say what a move task
does when issued to an entity already at its goal (NOT IN DOCS). The closest documented value is the OPE
aggregated-move-to-descriptor at-distance, default 0.5 m ("considered to be 'at' desired location"). Record the
count per arm; it is read after the run and does not enter any registered reading.

## (4) frame-time 0.1 is not a variable in variable-frame

UG Table 14 p233: Frame Time is "not enabled if Frame Mode is variable-frame". The L5 variant's
(frame-time 0.1) is a no-op, kept only to match RoadToKaunas.scn. L5 has ONE frame-mode variable (variable-frame
vs fixed-frame-run-to-complete) plus the declared coupled Vrf:TimeMultiplier; frame-time is not a second
variable, and no reading may attribute an effect to it.

## (5) L5 PRECONDITION - fastForwardSettings.mtl must be empty

UG 7.6.1 p261: entries in appData\settings\vrfSim\fastForwardSettings.mtl switch dead-reckoning, frame mode and
frame time automatically when the time scale passes an entry's play-speed (the highest play-speed below the
current speed applies). With Vrf:TimeMultiplier 6, a non-empty file could switch the variant's frame mode back
during the run and void the one variable.
- At L5 start, read settings\vrfSim\fastForwardSettings.mtl in the appData the run uses
  (C:\C2SIM\vrf-appdata-unattended\appData, as D36(b)). It must hold no fast-forward-entry. If it holds any:
  STOP, do not run, report.
- Observed 2026-10-15 (read-only, this order): the file is "(fast-forward-settings )" with no entry. That is a
  pre-check, not the start-of-run check; it is repeated at the start.

## (6) "UG 39244-39273" means UG p1247

The references "UG 39244-39273" (L1 "Why live") and "UG52 39248-39251" (HANDOFF finding 3) are LINE numbers of
an older text extraction, not pages. The content cited (the sim pages terrain by object need and prediction; it
pages out least-recently-used data when the polygon count exceeds the configured maximum) is UG 63.6.1 p1247.
Read every such reference as UG p1247.

## (7) NOTE - group mode (no change to L1/L2/L5)

L1/L2 "Not in L1/L2" says group mode is unusable on our containers as built. That stands for these runs. For a
later M3 the documented remedy is in the release notes: RN p69 (VRF-8801) - aggregate-level SMSs have a tab to
add additional systems to pseudo-aggregates; RN p75 (VRF-9312) - for the Aggregate Tactical Level SMS,
ground-echelon-group-tactics-enabler.sysdef is propagated to pseudo-aggregate units. Whether our brigade
container templates can take it is an M3 question with its own PREREG.

## Registration lines (C-4 prereg lint, check 13d) - added 2026-10-15, before any L1, L2 or L5 run

Lint lines only; predictions unchanged. Order LD-2026-10-15-04 (supervisor ruling, relayed by the dispatcher).
Every citation below is already in this file, its sibling PREREG files or c402e90; nothing new was read.

VENDOR CITATION: UG 35.5.11 p735; UG 28.3.2 p551; AC 7.8 p234-236; UG Table 14 p233; UG 7.6.1 p261;
UG 63.6.1 p1247; RN p69 (VRF-8801), p75 (VRF-9312); OPE aggregated-move-to-descriptor at-distance (default
0.5 m) - items (2) to (7) above.

OWN-RECORD CITATION: PREREG_L1_L2_DOCUMENTED_MOVE_2026-10-15.md (ec3583d); PREREG_L5_FRAME_MODE_ADDENDUM_2026-10-15.md
(8cf8f7c, c402e90); HANDOFF_2026-10-15.md (9fc85a6); research report LD-2026-10-15-08 (STP-976 comment
14265); ruling D68.

RUN KIND: movement

DEVIATION FROM RECORD: no "--pre-order-gate" and no "CONSOLE LEVEL: 4" here: this file registers no run of
its own; the runs it amends (L1, L2, L5) carry their own lines.
