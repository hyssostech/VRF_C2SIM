# REHEARSAL - WAY B WITH STP ITSELF PUSHING (STP in a container, path A) (light registration)

STATUS: REGISTERED 2026-10-11 (8e98b5f), RUN 2026-10-11 (S8 21:48Z, init 21:50:02Z, order 21:50:22Z), Result below; registered before the interface or VR-Forces launch (the commit stamp is authoritative). Light
registration under RL-20261005-01; the owner: "Also launch stp on docket at will. Watch out for taken ports. Dont use
defaults" (2026-10-11). Plan and citations: the seat's read-only lane laneSTPPUSH (scratchpad
d0bba772...\scratchpad\laneSTPPUSH\PLAN.md; push client StpPush.ps1 beside it).
Predecessor: REHEARSAL_WAYB_HAND3_2026-10-11.md (-5, 5 of 5 HELD, PushInit/PushOrder as the stand-in).

## Registration

PREREG ID: REHEARSAL_WAYB-2026-10-11-6
TIER AND GATE: STANDARD / PREREG (light). RUN KIND: movement.
THE ONE CHANGE FROM -5: the init and the order are pushed BY STP - container stp-vrf-push (image stp-server:coa-l13,
STP main revision 8134d296; host ports 127.0.0.1:39500 / 39599, free when checked; env STP_C2SimBridge__App__RestUrl
http://host.docker.internal:18080/C2SIMServer, StompUrl .../61614/topic/C2SIM, RestPassword the reference default,
ResetBeforeInitialize True, RunAfterInitialize True, SystemName "Not Set") over its WebSocket API (Register, then
PushC2SIMContent {content, dataType initialization|order}), with the SAME bytes -5 pushed
(data/STP-IRON-STORM-SYNTHETIC_Initialization.xml, _Order.xml; PATH A - STP pushes what it is given, builds nothing).
The lane read STP's push to send the same server commands as PushInit (STOP, RESET, INITIALIZE unless INITIALIZING;
push; SHARE, START) and PushOrder; the container's STOMP listener is a second subscriber of our server.
S1 DONE before this registration (container only, no federate): V1 engineState Running; V2 the bridge config above;
V3 one STOMP link to 61614 (F0AE, state 01); V4 REST 200 from inside the container.
BINARY: as -5 (main 473f20a deploy). Sequence: -5's, with the pushes by StpPush.ps1. Logs runs\launch52\wayb-stp-6\.

## Application numbers (marker `*** NEXT FREE: 5363 ***`)

5363 / 5364 = LaunchVrf52 -FederationHoldAppNumber 5363 (+1 retry); 5365 = back end; 5366 = vrfGui; 5367 = VrfC2SimApp.
CLAIMED before S8; marker 5363 -> 5368. The STP container joins no federation.

## Expectations (RECORDED, not gated)

| # | Expectation | Confidence |
|---|---|---|
| M1 | StpPush initialization exit 0 "PUSH OK" within about 10 s; the interface does NOT log "initiating clean stop"; READY TO TASK 36 of 36; server RUNNING after | MEDIUM (first STP container push) |
| M2 | StpPush order exit 0 "PUSH OK"; DEMO EXTENT "23 task(s) in this order, 16 refused"; 23 members populated; the 6 accepted tasks TASKCMPLT, all within 5 wall min (as -5, same bytes) | MEDIUM |
| M3 | One clean label per unit on screen (as -5); teardown as -5 (StopVrf52 3, forced back end 6) | HIGH |
| M4 | No STP push error in `docker logs stp-vrf-push`; the bridge's per-report "unknown uuid" lines are expected noise | MEDIUM |

STOP RULE: a push that fails or the interface leaving = section 4 teardown, record, no retry under this ID.

## Result

Scored by the seat from runs\launch52\wayb-stp-6\ (git-ignored): s10-stppush-init.log, s11-stppush-order.log,
s9-interface.log, stp-vrf-push.docker.log (saved before the container was removed), the shots, the teardown logs.
VERDICT: M1, M2, M4 HELD - STP ITSELF PUSHED THE DEMO; M3 HELD for teardown, NOT OBSERVABLE for the labels (blank map).

| # | Verdict | Measured |
|---|---|---|
| M1 | HELD | StpPush Register success 21:50:01.950Z (session E637BA961A2F); PushC2SIMContent initialization (169,286 chars) success=true in 1.02 s; the interface: server -> INITIALIZING then RUNNING, no "initiating clean stop", READY TO TASK 36 of 36 after 11.5 s, IDENTITY 36 of 36 by uuid |
| M2 | HELD | PushC2SIMContent order (121,530 chars) success=true in 1.23 s (21:50:22.714Z); DEMO EXTENT "23 task(s) in this order, 16 refused"; T17 SKIPPED; populated 28ID 1 of 1, 1-112 IN 5 of 5, 48 IBCT 17 of 17; 6 TASKCMPLT by 21:54:23Z = within 4.0 wall min of the order (SIM/WALL 7.1, 5.1, 5.0, 4.5) - the counts of -5 (same bytes) |
| M3 | HELD (teardown) / NOT OBSERVABLE (labels) | StopIface 0, interface exit 0, StopVrf52 3 then forced back end 36272 -> 6 (6 of 6 runs), no VR-Forces process left, no crash record, labels still deployed (-Verify 0). THE MAP WAS BLANK WHITE in every shot (init, t01, t02, t04, and a re-shot at 21:57Z), BOTH capture methods (CopyFromScreen and PrintWindow: 442 of 442 sampled map pixels white); toolbars, clock and panels drew; vrfGui Responding. The vrfGui log is line-for-line the same as -5's (where the map drew) |
| M4 | HELD | STP log in the push window: only "Unit matching uuid ... was not found" per unit per report (the bridge as a second STOMP listener, expected noise) and the push_c2sim_content query trace; the "SS_UNKNOWN graphic path" errors are at 21:46:07, container start-up, before any push |

FINDING (correlation, NO cause claimed): the blank map matches the DISPLAY being OFF when the GUI started. Windows
Kernel-Power event 566 (NextSessionType 1 = screen off, 0 = on; reason 12 = idle timeout): screen OFF 2026-10-05
17:13:38Z -> ON 19:22:20Z (-3 launched 18:45Z: blank map); OFF 2026-10-11 20:23:18Z, no ON before -6 (launched 21:48Z,
user idle 8,984 s at 21:57Z: blank map); ON at the launches of -4 (18:10Z) and -5 (19:30Z): map drawn - n = 4, 4 of 4
consistent. Competing reading: a GL start-up failure unrelated to the display, by chance on the same two runs.
Falsifier: a launch with the display awake that draws blank, or one with the display off that draws the map; and,
untested, whether a blank view recovers when the display wakes. For the demo the display is awake; for unattended
rehearsals keep the display awake before launching the GUI.

APPENDIX B ANNOTATION (marker stays 5368):
    RESULT REHEARSAL_WAYB-2026-10-11-6: 5363 CONSUMED (LaunchVrf52 holder 7708, attempt 1); 5364 BURNED unused; 5365
    CONSUMED (back end 36272, forced exit 6 after StopVrf52 3); 5366 CONSUMED (vrfGui 53952, closed, no modal); 5367
    CONSUMED (VrfC2SimApp, clean stop via StopIface). The STP container joined no federation; stopped and removed.
