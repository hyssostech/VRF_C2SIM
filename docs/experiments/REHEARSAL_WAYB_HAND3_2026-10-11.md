# REHEARSAL - WAY B HAND PATH, THIRD RUN: RL-20261011-01 AS DEPLOYED (light registration)

STATUS: REGISTERED 2026-10-11, before any launch (the commit stamp is authoritative). Light registration under
RL-20261005-01; quiet machine requested from STP-1055 and STP-1021 L13 before S8.
Predecessor: REHEARSAL_WAYB_HAND2_2026-10-11.md (REHEARSAL_WAYB-2026-10-11-4, 6 of 6 HELD).

## Registration

PREREG ID: REHEARSAL_WAYB-2026-10-11-5
TIER AND GATE: STANDARD / PREREG (light). RUN KIND: movement (a check of the deployed ruling, not an experiment).
BINARY: RUNBOOK sec 9 "RL-20261011-01 DEPLOY" (main 473f20a): VrfC2SimApp 1.0.0+git.473f20a, exe b116337a...b1da, dll
041aaf44...86df6, appsettings.Demo.json 644f7968... (DurationScale 0.25); pin 9e8c96b5; Name decoration hidden in
C:\MAK\vrforces5.2d and C:\C2SIM\vrf-appdata-unattended (-Verify 0 both). Any other at S2 = no launch.
WHAT DIFFERS FROM -4: the RL-20261011-01 deploy only (the overlay's 0.25, the Name decoration OFF, -AppNumber passed).
The RTI trio and the day holder 29456 (5351) are -4's, still up; 1.3 is not re-run.
SEQUENCE: -4's (DEMO_RUNBOOK 1.5, 2.1-2.4, 4 as now written), logs to runs\launch52\wayb-hand-5\, nothing set by hand;
shots after READY TO TASK and at T0+1, +2, +4 wall-min (CopyFromScreen and PrintWindow, scratchpad lane\shot2.ps1).

## Application numbers - from the ledger (marker `*** NEXT FREE: 5358 ***`)

5358 / 5359 = LaunchVrf52 -FederationHoldAppNumber 5358 (attempt 1 / its +1 retry); 5360 = back end; 5361 = vrfGui;
5362 = VrfC2SimApp (StartInterface52 -AppNumber 5362). CLAIMED before S8; marker 5358 -> 5363.

## Expectations (RECORDED, not gated)

| # | Expectation | Confidence |
|---|---|---|
| L1 | On screen, every unit carries ONE text line, its clean label ("169 FAB/28ID"; members "48 IBCT/28ID.INF3RIF1"-style); no 30-character name line, no "__" text (init and t01 shots) | HIGH |
| L2 | Pace from the overlay with nothing set by hand: "Vrf:DurationScale=0.25" in the start-delay lines; T2 "start delay 300 s (order says 1200 s ...)"; holds end at 300 SIM s (T10 450) | HIGH |
| L3 | The last accepted task is terminal 2-5 wall min after the order (-3: about 3 min at 0.25) | MEDIUM |
| L4 | StartInterface52 prints NO "-AppNumber not given" WARN (5362 passed); LaunchVrf52 prints no "LABEL DECORATION NOT ON" | HIGH |
| L5 | -4's J2 and J5 (36 of 36 bound, 16 refused, 23 members; unattended teardown, forced back end expected, no crash) | MEDIUM |

STOP RULE: a crash, the interface leaving at PushInit, or an S2 mismatch = section 4 teardown, record, no retry under this ID.

## Result

(to be written)
