# REHEARSAL - WAY B WITH STP ITSELF PUSHING (STP in a container, path A) (light registration)

STATUS: REGISTERED 2026-10-11, before the interface or VR-Forces launch (the commit stamp is authoritative). Light
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

(to be written)
