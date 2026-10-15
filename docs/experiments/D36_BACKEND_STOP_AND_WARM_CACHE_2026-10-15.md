# D36: BACK-END-STOP DISCRIMINATOR + WARM TERRAIN-CACHE TEST (and D33's adoption gate)

STATUS: PREREG committed 2026-10-14 23:24Z (c7971f3), BEFORE any launch. Operator ruling relayed by the STP-1097 supervisor
(Paulo, 2026-10-14 ~23:15Z, verbatim "Go for 1-4"): D36 GO and D33 GO as recommended - D33 = adopt OPTION 3's extent
(SCOPE_DEMO_EXTENT_2026-10-14) IF part (b) PASSES, else keep today's (RL-20261005-02).
Inputs: STP main 6a90ee35 (adds STP-1148 - objectives not drawn over their unit - so (b) is also its live check);
local image, disposable container on 39500/39575/39599; the COA 1 render (baseline copy, assemble --laydown r1,
confirmed re-post) as every run tonight - the 13/12 prediction was made on it. Day holder 5404 (RtiProbe 26720, to
~03:34Z) is joined, never touched. Sampler: 1 Hz working set / CPU on the back end with an ABORT at > 700 MB/min over
300 s or > 16 GB (V6's stop ran 780-2,200 MB/min); an ABORT means immediate teardown (StopIface, StopVrf52, forced
own back end on exit 3). App numbers: Appendix B claim 5418-5437 (5 per launch, unused = burned).

## (a) Back-end-stop discriminator (PRERUN_STP976_2026-10-15, BACK END LOST analysis)

Each arm: fresh launch (NotifyLevel 3, as on 19:18Z), the COA 1 Initialization only (no COA order - no unit carries
order tasks), then a 2-task probe on 48 IBCT: CONSTR to (54.0403,23.3364), then STREND the second task to the same
point - the 19:18Z geometry, so the second task re-tasks the 17 members from the in-water start while most of them
still have the CONSTR move outstanding.
- Arm F (fire at will): second task ENGAGE - the exact 19:18Z sequence (ATTACK path: fire at will).
- Arm H (order's ROE): second task OCCUPY (HoldObjective: same navigate-to-location, ROEHold -> hold fire). ENGAGE
  cannot carry the order's ROE (the unit ATTACK path always sets fire at will), so OCCUPY is the ROE-hold twin.
Order: F first. If F stops, run H. If F does not stop, the event is NOT REPRODUCED (n=1) - H is then uninformative
about ROE and is skipped; (b) follows.
STOP = the vendor log's last write freezes within seconds of the second dispatch AND simTime() advances at exactly 1x
AND the interface reports BACK END LOST ~2 min later. Window: 6 min wall after the second dispatch.
Readings: F stops and H stops -> the re-task (planning from that start), not the ROE; F stops, H does not -> the ROE /
fire-at-will set is implicated; neither -> not reproducible at n=1 (a stochastic or state-dependent cause is not
excluded). Working-set slope during a stop is recorded either way (V6 runaway or not).

## (b) Warm terrain-cache test on OPTION 3's box (S 53.813 W 23.050 N 54.450 E 23.764, + 2 km margin)

Two launches with NotifyLevel 4 (the 10-05 measure, "queued for <s> seconds", is absent at level 3 - checked on
tonight's two vendor logs, 0 lines), both with Vrf__DemoExtent=53.813,23.050,54.450,23.764 for the interface process
only, the run-owned appData C:\C2SIM\vrf-appdata-unattended\appData (its cache persists - SCOPE doc) reused unchanged:
- FILL (throw-away): init + the full COA 1 order; run until the last executed task ends or 20 min wall after receipt.
- SCORED: the same, on the cache the fill left.
Cache size (appData\cache\vrfsim, files / MB) is read before FILL, after FILL and after SCORED.
SCORED PASSES only if ALL hold (any miss = FAIL -> keep today's extent and report why):
- P1 no BACK END LOST;
- P2 every SIM/WALL RATIO line from order receipt to the end of the window reads >= 1.000 (never slower than real time);
- P3 working-set slope <= 200 MB/min over every 5-min window after population completes;
- P4 56 SBCT T2 and 116 ABCT T15 (if admitted) are driven to their objectives: TASKCMPLT on arrival within the window,
  and no STALL WATCHDOG verdict on either.
Recorded, not gating: the vendor log's feature_source / MAK_OBSTACLE queue waits (median, p95; counts only - never
quoted) in FILL vs SCORED - the fetch-vs-processing reading; STP-1148 (no objective drawn over its own unit, by eye
on the GUI capture).
Predictions: admitted 13 / executed 12 on the 37-task export (MEDIUM - the 299d4d46-export emulation plus STP-1150; the
re-pre-run admitted two 48 IBCT tasks the emulation had refused in the chain); exact list written below from the
6a90ee35 export BEFORE the FILL launch.

## (b) prediction on the 6a90ee35 export (committed 23:24Z with the PREREG, before any launch)

Export: 95 symbols / 37 tasks posted (STP main 6a90ee35, container stp-d36). Emulation of the interface's rule on
option 3's box: the DEMO EXTENT line refuses 17 and admits 20 per task - T1 T2 T5 T6 T7 (56 SBCT), T11 (278 ACR),
T12-T14 (1-112 IN), T15 T19 T21 (116 ABCT), T22 T23 T28 (48 IBCT), T33 (169 FAB), T34-T36 (113 SB), T37 (11 CAB).
Chain-runnable 13 (T1 T2 T12 T13 T14 T15 T22 T23 T33 T34 T35 T36 T37); EXECUTED 12 (T37 refused at populate, by
ruling); SKIPPED at dispatch 7 (T5 T6 T7 T11 T19 T21 T28 - a STREND predecessor out of the box). The supervisor's
"13/12" is the chain-runnable / executed pair. MEDIUM.
Arm probes (xsd PASS): F = PROBE_48IBCT_CONSTR -> PROBE_48IBCT_ENGAGE; H = PROBE_48IBCT_CONSTR -> PROBE_48IBCT_OCCUPY;
ROEHold on all, as STP writes it.

## (a) RESULT (2026-10-14 23:24-23:43Z; runs/launch52/d36/armF, armH): REPRODUCED IN BOTH ARMS - ROE IS NOT THE CAUSE

| | Arm F (ENGAGE, fire at will) | Arm H (OCCUPY, ROEHold -> hold fire) |
|---|---|---|
| CONSTR dispatched | 23:26:11.7Z (sim 114.7 s) | 23:35:13.9Z (sim 80.0 s) |
| second task dispatched | 23:27:45.099Z (sim 742.7 s) | 23:37:04.025Z (sim 718.3 s) |
| vendor log last write (read after the forced exit) | 23:27:45.369Z (+0.27 s) | 23:37:04.321Z (+0.30 s) |
| sim clock after | ratio 5.9 -> 1.000 (extrapolated) | ratio 6.1 -> 1.431 -> 1.000 |
| BACK END LOST (last good) | yes (23:29:34Z, ~109 s after) | yes (23:38:58Z, ~114 s after) |
| working set after the stop | 3,840 MB, FLAT (no runaway) | 3,835 MB, FLAT |
| back-end CPU after the stop | ~2.0 cores: TWO threads at 100% | ~1.0 core: ONE thread at 100% |
| teardown | StopVrf exit 3 -> forced exit 6 (as in every arm, stopped or not - the harness, not a stop signature) | same |
Both vendor logs end inside the re-plans, as on 19:45Z (shape only: "Start point is within obstacle" bursts). The
interface sent the ROE at BOTH dispatches of both arms (VrfC2SimService.cs:6223 - every dispatch sets the order's ROE;
fire at will only for arm F's ENGAGE), so arm H never set fire at will.
READING (PREREG table): F stops AND H stops -> the RE-TASK, not the ROE. With 19:45Z that is 3 of 3: re-tasking 48 IBCT's
17 members with a second navigate-to-location from that start, while most of the CONSTR moves are still outstanding,
stops the 5.2d back end within 0.3 s - no crash, no dump, no memory runaway (unlike V6), one or two threads spinning.
NOT separated yet (next discriminator, needs a go): the start position in water vs the outstanding first moves - a
re-task after ALL 17 members have ended, or the same re-task from a dry start. Demo consequence, flagged before (b):
every STREND navigate successor dispatched on arrival evidence while members still move is the same shape - option 3
has 56 SBCT T1 -> T2 and 48 IBCT T22 -> T23, and 113 SB T34 -> T35 -> T36.
PREREG ADDENDUM for (b), written before the FILL launch: P1 (no BACK END LOST) is now EXPECTED TO FAIL (MEDIUM) on
those chains; the criteria are unchanged.

## (b) FILL RESULT (2026-10-14 23:43-23:51Z; runs/launch52/d36/fill; app 5428-5432)

DEMO EXTENT 37 tasks, 17 refused, 20 admitted (as predicted). Dispatched: T1 and T22 (CNFPSL, hold-in-place), then
navigates T33 (169 FAB, 23:45:57Z), T15 (116 ABCT, 23:46:55Z), T2 (56 SBCT, 23:47:25Z), T23 (48 IBCT, 23:47:33Z). SIM/WALL
fell 2.04 -> 1.000; BACK END LOST (last good 23:47:47Z). A DIFFERENT FAILURE FROM (a): the vendor log kept writing until
the teardown (last write 23:50:50Z, 629,546 lines, 0 "Start point is within obstacle"), with 45,243 "queued for <s>
seconds" lines - median 114 s, p95 161 s (10-05: 71 / 192 s) - over 131,878 feature_source lines: the 10-05 terrain
feature-paging BACKLOG, not the (a) hang. Working set 3.9 -> 9.4 GB; the sampler ABORTED at 23:50:00Z (1,409 MB/min
over 300 s) and the run was torn down (StopVrf exit 3 -> forced exit 6; RTI and holders intact). Cache vrfsim
28,070 files / 509.5 MB -> 31,537 / 516.7 MB. The fill was cut short by the abort, so the cache is only as warm as
~5 min of paging made it.

## (b) SCORED RESULT (2026-10-14 23:51-23:59Z; runs/launch52/d36/scored; app 5433-5437): FAIL -> D33 KEEPS TODAY'S EXTENT

Same order, same extent, on the cache the fill left. DEMO EXTENT 37 / 17 refused / 20 admitted. Dispatched T1, T22
(hold-in-place), T33 (23:53:23Z), T15 (23:54:43Z), T2 (23:54:57Z), T23 (23:55:42Z, after the last good reading); SIM/WALL 1.32 -> 1.000; BACK END LOST (last good
23:55:16Z, ~2 min after the order). The vendor log kept writing to the teardown (853,450 lines; 157,394 feature_source;
64,362 "queued for" - median 73 s, p95 135 s; 0 in-obstacle); working set 2.6 -> 14.3 GB, sampler ABORT 23:57:49Z
(2,367 MB/min over 300 s), torn down 23:58Z (StopVrf exit 3 -> forced exit 6; RTI and holders intact; no dump).
Cache vrfsim 31,537 / 516.7 MB -> 32,283 / 518.9 MB.
| Criterion | Verdict |
|---|---|
| P1 no BACK END LOST | FAIL (2 min after the order) |
| P2 SIM/WALL >= 1.000 | NOT FAILED on its own terms (1.323, then 1.000 - the extrapolation after the loss); uninformative after the loss |
| P3 slope <= 200 MB/min | FAIL - ~1,860 MB/min over the 3.3-min post-population window before the abort (no full 5-min window existed) |
| P4 T2 / T15 to their objectives | FAIL (abandoned at the loss) |
READING: the warm cache did not make option 3 safe (P1, P3, P4). Queue waits fell (median 114 -> 73 s, n=1) but stayed at
minutes, and the memory climb started at order receipt with one navigate out (2.6 -> 6.4 GB by 23:53:50Z). The
fetch-vs-processing question is NOT settled: cache\vrfsim was already warm before FILL (28,070 files, written by the
19:18Z run), so FILL/SCORED is warm-vs-warmer, and that feature paging lands in cache\vrfsim is assumed, not shown. D33: per the
operator's rule ("if (b) FAILS, keep today's extent") the demo stays on RL-20261005-02's box; no config change.
STP-1148 not judged (no capture before the loss showed the objectives).

## (a2) PREREG - two more arms (supervisor GO, within the D36 approval; committed 2026-10-14 23:59Z (6d9c4a5), before launch)

Same rules (fresh launch, COA 1 init only, 48 IBCT only, 1 Hz sampler + abort, NotifyLevel 3).
- Arm (i) AFTER ALL ENDED: CONSTR alone to (54.0403,23.3364); wait until the interface logs 17 of 17 members ended
  (cap 8 min wall; record N if not all end); then a SEPARATE order, ENGAGE to the same point (no STREND, start 0).
- Arm (ii) DRY START, moves outstanding: the arm-F sequence (CONSTR -> STREND ENGAGE, same point for both) at
  (54.000,23.290), south-west of the start, away from the lake. VALIDITY: the ENGAGE leg's pre-flight must show no
  deep / OSM water at 0.00 km along, and the vendor log's "Start point is within obstacle" count is recorded; if the
  start is wet the arm is INVALID (not a reading).
Readings: (i) stops -> outstanding moves are not needed (the start position / the re-task itself suffices); (i) does
not -> outstanding moves are needed (a gate that waits for every member to end would avoid it). (ii) stops -> a wet
start is not needed; (ii) does not -> the wet / in-obstacle start is involved. STOP = same definition as (a).

## (a2) RESULT (2026-10-15 00:00-00:33Z; runs/launch52/d36/armI, armII; app 5438-5447)

| Arm | start | second task | members ended at re-task (outstanding) | in-obstacle lines | STOP |
|---|---|---|---|---|---|
| F (23:27Z) | pre-flight DRY at 0.00 km; wet only by the vendor's per-member in-obstacle count | ENGAGE, STREND, ~1.5 min after CONSTR | 4 of 17 (13) | 42 | YES (+0.27 s) |
| H (23:37Z) | wet (water at 0.00 km on the leg) | OCCUPY, STREND, ~1.8 min after | 4 of 17 (13) | 35 | YES (+0.30 s) |
| (i) (00:10Z) | wet (water at 0.00 km on the leg) | ENGAGE, separate order, 8 min after | 7 of 17 (10, never ended - capped) | 57 | NO - ran on (SIM/WALL ~7), vendor log growing; the unit then STALLED (TASKABRT by C16) |
| (ii) (00:25Z) | DRY (no water on either leg; VALID) | ENGAGE, STREND, ~2 min after | 10 of 17 (7) | 0 | NO - ran on (SIM/WALL ~6.6), ENGAGE moves completing |
(i) is not the clean "after ALL ended" arm the PREREG asked for: 10 members never ended in 8 min (stuck), so it reads
"a LATE re-task from the wet start". READING (n=1 per new arm - NOT a closed cause; Fable cold review applied): the
results are CONSISTENT WITH the stop needing a re-task early in the move, while many members are still planning from an
obstacle start (F, H, 19:45Z: 3 of 3; each vendor log ends inside those re-plans). Two alternatives are NOT separated:
(1) the NUMBER OF MEMBERS STILL OUTSTANDING at the re-task - 13, 13, 10, 7 - is monotone with the stop and alone explains
all four arms; (2) the wet start: F's own leg pre-flighted DRY at 0.00 km (wet only by the vendor's member count), and
(ii) changed destination, route (1,331 m) and road layer as well, so it does not isolate "dry". The data bound the
unsafe window only as "~1.8 min wall / ~740 sim s after the first move: stopped; 8 min: safe once".

## Mitigation view (offline, for the operator's options)

*** REFUTED 2026-10-15 01:38Z by D47 G1 (D47_NAVGATE_LIVE_AND_D16_PRO_2026-10-15.md): the gate held the re-task until all 17
members had ended - and the back end STOPPED anyway, from a start with no water and 0 in-obstacle lines. The (a2) reading
and the bounded-hold candidate below are WITHDRAWN; kept only as the record of what was believed. ***

- VRF_C2SIM side, the candidate that covers every observed case: HOLD ANY NAVIGATE to a container (a chain successor
  OR a new order) while its members' previous navigates are outstanding, until every member has ended, BOUNDED (the 10 stuck members of (i) never end - a bound is required; a late
  re-task was safe in (i)). Untested as a fix - an untested CANDIDATE, not a proven remedy; it would need its own arm (F's geometry with the hold on).
- NOT recommended without a test: stopping the outstanding member tasks first - a stop is itself a re-task of members
  mid-plan, the very shape that stops the back end.
- No STP change is needed for this (the order is valid; the timing is the interface's).
- Today's demo (RL-20261005-02 box) does not contain the shape: 1-112 IN's tasks are in place, 48 IBCT's chain is
  skipped. The risk is a NEW order re-tasking a unit that is moving from wet ground (the probe did exactly that).
- STP-884 (successor released while an ATTACK's engage phase is in flight) is the SAME FAMILY - a successor dispatched
  onto a unit whose predecessor is still executing - so one gate rule ("no successor dispatch while the unit has vendor
  work in flight; bounded wait") covers both by design; they are two code paths (platform engage completion vs container
  member moves), so one change, two tests.
- STP-854 is probably NOT the same mechanism: a CRASH (callstack + dump + error dialog) 1.3 s after an order during init
  churn; tonight's stops leave no dump and no dialog, the process lives with threads spinning, and they follow a
  mid-move re-task, not an order on the heels of an init. Keep it separate.
- Option 3's failure (b) is a different class (feature-paging backlog) that the interface cannot fix except by the
  extent bound already in force; "fewer simultaneous movers" is NOT supported (the climb began with one navigate out).

## Item 3 (Pro eyes-on)

ArcGIS Pro 3.6.0.59527 IS installed on this host. Pressing Pro's UI (confirm tasks, export, "C2SIM Reports On") is not
something the seat can do reliably unattended; the Pro half goes to the Local seat or the owner (supervisor's routing).
