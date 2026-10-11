# REHEARSAL - WAY B HAND PATH, SECOND RUN: THE MAP BY EYE, THE PROFILE'S OWN PACE, STP-850 LIVE (light registration)

STATUS: REGISTERED 2026-10-11, before any launch (the commit stamp is authoritative). Light registration under
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

(to be written)
