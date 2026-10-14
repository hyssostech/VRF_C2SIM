# D36: BACK-END-STOP DISCRIMINATOR + WARM TERRAIN-CACHE TEST (and D33's adoption gate)

STATUS: PREREG written 2026-10-14 ~23:35Z, BEFORE any launch. Operator ruling relayed by the STP-1097 supervisor
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

## (b) prediction on the 6a90ee35 export (written ~23:50Z, before the FILL launch)

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
| graceful close | refused (exit 3) -> forced exit 6 | refused (exit 3) -> forced exit 6 |
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

## Item 3 (Pro eyes-on)

ArcGIS Pro 3.6.0.59527 IS installed on this host. Pressing Pro's UI (confirm tasks, export, "C2SIM Reports On") is not
something the seat can do reliably unattended; the Pro half goes to the Local seat or the owner (supervisor's routing).
