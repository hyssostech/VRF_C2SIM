# REHEARSAL - WAY B HAND PATH, SECOND RUN: THE MAP BY EYE, THE PROFILE'S OWN PACE, STP-850 LIVE (light registration)

STATUS: REGISTERED 2026-10-11 (bca93c5), RUN 2026-10-11 (S8 18:10Z, order 18:23:00Z), Result below; registered before any launch (the commit stamp is authoritative). Light registration under
RL-20261005-01 (standing go-live; quiet machine agreed 2026-10-11 with the two busy local sessions, STP-1055 and STP-1021
L13 - both replied "no objection", no builds until released). The seat runs it.
Predecessor: REHEARSAL_WAYB_HAND_2026-10-05.md (REHEARSAL_WAYB-2026-10-05-3): H1-H6, H8-H10 HELD, H7 NOT OBSERVABLE.

## Registration

PREREG ID: REHEARSAL_WAYB-2026-10-11-4
TIER AND GATE: STANDARD / PREREG (light). RUN KIND: movement.
BINARY: RUNBOOK sec 9 "STP-850 DEPLOY 2026-10-11 17:36Z" (main b50b9f1): VrfC2SimApp.exe sha256 e0e95af5...3ace7, .dll
38aa7c81...97aa1, ProductVersion 1.0.0+git.b50b9f1.Release-5.2; appsettings.Demo.json sha256 0cfea21b...840d2 (unchanged
since -3); VrfBridge pin 9e8c96b5. Any other at S2 = no launch.
WHAT DIFFERS FROM -3 (several on purpose - a rehearsal, not a one-variable experiment):
  V1 PACE: NOTHING set by hand - the Demo profile's own DurationScale 1.0 (-3 ran 0.25). The demo pace is the owner's
     open decision (DEMO_RUNBOOK 2.2 PACE; RL-20261005-06 supervisor reading): this run gives it its first 1.0 data point.
  V2 BUILD: STP-850 (Vrf:StartTimeAnchor=Receipt, RL-20260928-05). STP's export has minOffset 0 (--parse-order, RUNBOOK
     sec 9), so no task is rebased; T2's X9 relative delay is unchanged.
  V3 CAPTURE (H7): besides PrintWindow (blank map in -3), a CopyFromScreen of the vrfGui rectangle after ShowWindow
     (SW_RESTORE) + SetForegroundWindow - no keyboard or mouse input (scratchpad lane\shot2.ps1). The seat looks at the
     PNGs itself.
  V4 FRESH RTI after the 2026-10-06 reboot: S3 starts rtiexec, S4 runs DEMO_RUNBOOK 1.3 (the day holder). 1.6: the first
     launch after a reboot may raise the MAK RTI connection dialog (LaunchVrf52 exit 4) - answered, never killed.
  V5 SERVER: c2sim-server-vrf was Exited (255); `docker start` (host ports 18080/61614 checked free first; the
     operator's 8080/61613 and STP's 9500-9599 untouched). OP3: if S6 reads any state but UNINITIALIZED, StopIface on the
     private pair BEFORE S9, so PushInit meets the state -3 tested.
VENDOR / OWN-RECORD CITATION: as REHEARSAL_WAYB_HAND_2026-10-05 Registration; plus RL-20260928-05, PR #1, RUNBOOK sec 9.

## Application numbers - from the ledger (marker read 2026-10-11: `*** NEXT FREE: 5349 ***`)

| Number | Federate | Parameter |
|---|---|---|
| 5349-5352 | the day holder (StartFederationHolder52, tries in order; untried/unjoined = burned) | -AppNumbers 5349,5350,5351,5352 |
| 5353 / 5354 | LaunchVrf52's own STP-825 holder, attempt 1 / its automatic +1 retry | -FederationHoldAppNumber 5353 |
| 5355 | back end vrfSimHLA1516e | -BackendAppNumber 5355 |
| 5356 | front end vrfGui | -FrontendAppNumber 5356 |
| 5357 | VrfC2SimApp | StartInterface52 -AppNumber 5357 |
BLOCK CLAIMED BEFORE S4: 5349-5357, marker 5349 -> 5358. Not federates: StartRtiExec52, PushInit, PushOrder, StopIface,
StopVrf52, Enable-LabelDecoration.

## Sequence

As REHEARSAL_WAYB_HAND_2026-10-05 S1-S17 (PowerShell 7, main checkout root, logs to runs\launch52\wayb-hand-4\), with:
- S1: no RTI trio expected (rebooted); the trio check is replaced by S3/S4.
- S3: StartRtiExec52 - SEE "RTIEXEC READY", 0 (a fresh start).
- S4 (1.3): StartFederationHolder52 -AppNumbers 5349,5350,5351,5352 -SettleSecs 28800 (-WhatIf first) in its own
  background window; SEE "HOLDER JOINED: pid ... appNo ...", 0.
- S5a: docker start c2sim-server-vrf; REST 200 on 18080.
- S6: StopIface --dry-run reads the state; not UNINITIALIZED -> StopIface --yes on the private pair (V5).
- S8: LaunchVrf52 -BackendAppNumber 5355 -FrontendAppNumber 5356 -FederationHoldAppNumber 5353 -AppDataDir
  'C:\C2SIM\vrf-appdata-unattended\appData'.
- S9: StartInterface52 -ClientId "Not Set" -Server private -AppNumber 5357, NO Vrf__ variable at all (V1).
- Shots (V3): after READY TO TASK (init shells), T0+1, T0+5, T0+10 wall-min, and at the last TASKCMPLT.
- HOLD until every accepted task is terminal (or the cap T0 + 30 wall-min), then S12-S16 as -3.

## Expectations (RECORDED, not gated)

| # | Expectation | Confidence |
|---|---|---|
| J1 | -3's H1 lines, the TASK CLOCK line with "Vrf:DurationScale=1"; start-up "START TIME ANCHOR (STP-850): ... from Receipt"; at the order "START TIME BASE (STP-850): this order's smallest SimulationTime offset is 0 s over 9 task(s)" | HIGH |
| J2 | -3's H2-H4 (READY TO TASK 36 of 36; 16 refused out of the extent, T17 skipped; 28ID 1 + 1-112 IN 5 + 48 IBCT 17 populated) | HIGH |
| J3 | PACE at 1.0: holds T1, T11, T13 end at about 1200 SIM s, T10 about 1800 SIM s (4x -3's); T2 dispatches about 1200 SIM s after T1's TASKCMPLT ("start delay 1200 s (order says 1200 s; Vrf:DurationScale=1)") and ends about 1200 SIM s later; T14 dispatches after T13 and arrives (TASKCMPLT) before its overdue line. The last accepted task is terminal 8-16 WALL min after the order at -3's 5.5-7x; RECORD the SIM/WALL lines. A miss = beyond the 30 wall-min cap | MEDIUM |
| J4 | H7 by eye: the -scr shots show the terrain (not blank), 36 container symbols after init, labels as the clean designator ("48 IBCT/28ID", members "48 IBCT/28ID.HQ1"-style), no "__" text; 48 IBCT's members visibly displaced between T0+1 and the last shot. The -pw shots stay blank (as -3) | MEDIUM |
| J5 | -3's H8-H10 (unattended GUI exit; StopVrf52 3 then forced 6 expected - 3 of 3 runs on 2026-10-05; labels ON after; no crash) | MEDIUM |
| J6 | 1.6: no RTI connection dialog at S8 (exit 0), else exit 4 answered by hand and recorded | LOW |

STOP RULE: a crash, the interface leaving at PushInit, or S2 hash mismatch = section 4 teardown, record, no retry under this ID.

## Result

RUN 2026-10-11, scored by the seat from runs\launch52\wayb-hand-4\ (git-ignored): s9-interface.log (1,344 lines), the
s3-s16 logs, shot_init / t01 / t05 / t10 / t15 (-pw and -scr). The vendor log copy was not read. VERDICT: 6 of 6 HELD
(J4 with caveats). The hand path ran end to end on the Demo profile with NO variable set by hand.

| # | Verdict | Measured |
|---|---|---|
| J1 | HELD | SERVER line 18080/61614; MODEL SET RULE -> AggregateTacticalLevel; DEMO EXTENT ON + 2 km; TASK CLOCK sim; "START TIME ANCHOR (STP-850): ... from Receipt"; READY - joined, 1 back end, FidelityTable. At the order: "START TIME BASE (STP-850): this order's smallest SimulationTime offset is 0 s over 9 task(s)"; T10 "start delay 1200 s (order says 1200 s; Vrf:DurationScale=1; authored SimulationTime offset 1200 s -> offset used 1200 s, rebased on the order's smallest offset 0 s), counted from ORDER RECEIPT"; T2 "start delay 1200 s (... relative delay 1200 s), counted from its predecessor's completion" (X9) |
| J2 | HELD | READY TO TASK - 36 of 36 after 11.7 s, IDENTITY 36 of 36 by uuid; D2b allowed; 23 tasks, 16 refused out of the extent, T17 SKIPPED; populated 28ID 1 of 1, 1-112 IN 5 of 5, 48 IBCT 17 of 17 |
| J3 | HELD | Profile pace 1.0. Dispatch / end on the SIM clock: T1, T13 at 592 s, TASKCMPLT at their 1200 s Duration; T14 dispatched 1810.6 s after T13, ARRIVAL EVIDENCE at 2545.4 s (734.8 SIM s / 174.6 WALL s; farthest travel 2351 m of 2768 m, nearest 484 m), TASKCMPLT at its Duration end (early arrival held to start + Duration, RL-20260921-09); T10 at receipt + 1200 s, TASKCMPLT at its 1800 s Duration; T2 dispatched 3022.3 s (T1's end + 1200 s relative delay); T11 dispatched 3493.5 s; last TASKCMPLT (T11) about 18:36:05Z = 13.1 WALL min after the order (predicted 8-16). SIM/WALL 9.9, 6.3, 6.5, 6.2 before T14 moved, 4.1-5.0 while it moved and after. GUI toolbar showed 1x throughout (-3's F3) |
| J4 | HELD, CAVEATS | THE MAP IS VISIBLE (the first live look): both capture methods show terrain, symbols and labels - the -pw (PrintWindow) shots too, once the window had been restored (the init -pw was 3862x2110, the restored window 2582x1330); -3's blank map stays UNEXPLAINED (no cause claimed). Init: the containers on the Suwalki imagery at 1:200,000, each with TWO text lines - the 30-character NAME (e.g. "169_FAB/28ID__FRIENDLY_FIELD_A") above the clean LABEL ("169 FAB/28ID") - so "__" text IS on screen, from the name decoration, not the label. Members: "48 IBCT/28ID.INF3RIF1"-style labels. 48 IBCT's members visibly displaced about 1.5 km NE between t01 and t10. Labels overprint where units stack (48 IBCT and 1-112 IN; the 278 ACR / 4ID stacks). Green bars over the populated units: not identified. A light-blue polygon fills the map's east edge: not identified |
| J5 | HELD | StopIface 0 (RUNNING -> UNINITIALIZED); interface clean stop, exit 0, last line "fail: C2SIM.C2SIMSDK ... STOMP block reading cancelled" (-3's F5); StopVrf52 3 (graceful close refused; "NVOGLDC invisible" / "Default IME" only, no modal); S14 forced back end 44220 by pid + start -> 6 (4 of 4 runs since 2026-10-05); no VR-Forces process left; labels ON in both trees after; no .callstack.log / .dmp since 18:00Z; rtiexec 32260, rtiForwarder 50916, day holder 29456 alive |
| J6 | HELD | no RTI connection dialog after the reboot; S8 exit 0 |

FINDINGS (no cause claims)
- K1 OP3 ANSWERED FOR THE RUNBOOK: after `docker start` the private server came back RUNNING (S6). The interface stops on
  UNINITIALIZED (VrfC2SimService.cs:1536-1538) and PushInit's ResetToInitializing sends STOP, RESET, INITIALIZE unless the
  server is INITIALIZING (C2SIMSDK C2SIMSSDK.cs:298-313), so starting the interface on a RUNNING server and then pushing
  the init is the untested race. S6b (StopIface --yes BEFORE the interface) put it at UNINITIALIZED and PushInit went
  UNINITIALIZED -> INITIALIZING -> RUNNING with the interface staying up. DEMO_RUNBOOK 1.5 gains that step.
- K2 1.3 burned two numbers: 5349 and 5350 create FAILED (exit -1073741819 on NETN-BASE.xml / MAK-VRLExt-3_evolved.xml,
  STP-825), 5351 JOINED. StartFederationHolder52 does not return while its holder lives (its console is the holder's).
- K3 The "START TIME BASE" line says "every offset is rebased on it, so the order starts on its receipt" when the
  minimum is 0 (a rebase by nothing) - wording only.
- K4 T14's ARRIVAL EVIDENCE again judged the populated container as "ONE POSITION: an aggregate-level unit with no
  members" (-3's F1, reproduced).
- K5 PACE DATA for the owner: at 1.0 the accepted slice runs about 13 wall min (6 holds of 1200-1800 SIM s and one
  2.8 km move); at 0.25 (-3) about 3 min. The mover's own drive does not scale (734.8 SIM s here, 637.3 in -3).

APPENDIX B ANNOTATION (appended under the CLAIMED entry; marker stays 5358):
    RESULT REHEARSAL_WAYB-2026-10-11-4: 5349, 5350 BURNED (day-holder creates failed, STP-825); 5351 CONSUMED (day holder
    RtiProbe 29456, 28800 s from 18:03:36Z, never killed); 5352 BURNED unused; 5353 CONSUMED (LaunchVrf52 holder 43000,
    attempt 1, its 900 s end); 5354 BURNED unused; 5355 CONSUMED (back end 44220, forced exit 6 after StopVrf52 3);
    5356 CONSUMED (vrfGui 53336, closed, no modal); 5357 CONSUMED (VrfC2SimApp, clean stop via StopIface).
