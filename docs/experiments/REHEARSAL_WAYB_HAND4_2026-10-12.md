# REHEARSAL - WAY B HAND PATH, FOURTH RUN: DISPLAY OFF AT LAUNCH, WOKEN BY LAUNCHVRF52 (light registration)

STATUS: REGISTERED 2026-10-12, before any launch (the commit stamp is authoritative). Light registration under
RL-20261005-01; quiet machine requested from the busy local sessions before S8.
Predecessors: REHEARSAL_WAYB_HAND3_2026-10-11.md (-5, 5 of 5) and REHEARSAL_WAYB_STPPUSH_2026-10-11.md (-6: the map BLANK
with the display off at launch - correlation n = 4, no cause claimed).

## Registration

PREREG ID: REHEARSAL_WAYB-2026-10-12-7
TIER AND GATE: STANDARD / PREREG (light). RUN KIND: movement (a check, and a test of the blank-map correlation).
BINARY: RUNBOOK sec 9 "SDKLOG DEPLOY" (main f3dfb43 - VrfC2SimApp 1.0.0+git.f3dfb43, exe bc35c0c8...2ed3); scripts at
main 94eaac4 or later (LaunchVrf52's DISPLAY PRE-FLIGHT). Pin 9e8c96b5. Any other at S2 = no launch.
WHAT IS TESTED (two changes since -5, by design):
  T1 THE DISPLAY: S8 is started ONLY while Windows reports the screen OFF (last Kernel-Power 566 NextSessionType 1). LaunchVrf52
     sends SetThreadExecutionState(ES_DISPLAY_REQUIRED|ES_SYSTEM_REQUIRED) once before the GUI starts.
  T2 THE SDK SHUTDOWN LINE: the interface's last lines after the clean stop.
SEQUENCE: -5's (DEMO_RUNBOOK 1.3, 1.5, 2.1-2.4, 4 as written; nothing set by hand; PushInit/PushOrder on the private
server), plus 1.3 (the day holder: the 2026-10-11 holder ended 02:03Z). Shots as -5 (tools\display\Capture-VrfGui.ps1 -Screen,
after READY TO TASK and at T0+1, +2, +4 wall-min). Logs runs\launch52\wayb-hand-7\.

## Application numbers (marker `*** NEXT FREE: 5368 ***`)

5368-5371 = StartFederationHolder52 -AppNumbers (day holder, tried in order; unjoined = burned); 5372 / 5373 = LaunchVrf52
-FederationHoldAppNumber 5372 (+1 retry); 5374 = back end; 5375 = vrfGui; 5376 = VrfC2SimApp. CLAIMED before S4; marker
5368 -> 5377.

## Expectations (RECORDED, not gated)

| # | Expectation | Confidence |
|---|---|---|
| N1 | s8 log: "display: last Kernel-Power 566 transition ... -> screen OFF; ES_DISPLAY_REQUIRED sent (accepted); screen now ON (woken now)", and a new 566 screen-ON event within seconds of it | MEDIUM (n = 1 so far) |
| N2 | THE MAP DRAWS: the init and t01 shots show the terrain and the labelled units (not a white map area) - falsifies the display explanation if blank while N1 held | MEDIUM |
| N3 | No "fail:" line at the interface's clean stop; the SDK's "STOMP block reading cancelled" appears as an info line | HIGH |
| N4 | -5's L1-L5 (one clean label per unit; 0.25 from the overlay; terminal within 5 wall min; no -AppNumber / label WARN; READY TO TASK 36 of 36, 16 refused, 23 members; teardown 3 then forced 6; no crash) | MEDIUM |

STOP RULE: a crash, the interface leaving at PushInit, or an S2 mismatch = section 4 teardown, record, no retry under this ID.

## Result

(to be written)
