# D47 (navigate gate, live) and D16 (paired Pro eyes-on, STP-1108) - 2026-10-15

STATUS: PREREG committed BEFORE any launch. Operator (relayed by the STP-1097 supervisor, ~01:06Z, verbatim): "And run
the live tests you need. I am stepping away for the next hours. Keep things flowing as you did last night." Rules as
D36: stop at once on RTI damage; day holder 26720, stp-lt (9500-9599) and 19575 never touched; clean teardown; each
arm recorded. App numbers: Appendix B 5448-5467 (5 per launch; unused = burned).

## D16 - paired Pro eyes-on with the Local seat (stp-20), VRF_C2SIM MAIN, today's extent

Topology (agreed with stp-20): disposable STP container stp-pro (STP main 6a90ee35) on 127.0.0.1:39500/39555/39575/
39599, bridge to c2sim-server-vrf 18080/61614. Pro (Local seat, add-in from run 38650876426) connects to OAA 39555
FIRST on an EMPTY server; then the seat posts the baseline + COA 1 (confirmed), launches VR-Forces + the interface
(main binary), pushes the Initialization; on "Reports On, go" pushes a small order - 1-112 IN EVACT -> HIDE, 28ID
HoldInPlace (no unit re-tasked while moving: HIDE is in place). VR-Forces up ~10 min after the go.
Expect (MEDIUM - first paired run): no BACK END LOST; SymbolReport events from stp-pro carry 1-112 IN's changing
position (StpListen, as 22:49Z); the Local seat reports what Pro shows (its verdict, not the seat's).

## D47 - the bounded navigate gate, LIVE (branch feat/navigate-gate, worktree build; main untouched)

Arm G (at most 2 runs): arm F's geometry and timing exactly (COA 1 init only; 48 IBCT CONSTR to (54.0403,23.3364), then
STREND ENGAGE to the same point), the interface started from the WORKTREE (its own exe and appsettings; gate ON,
Vrf:NavigateGateMaxWaitSeconds 480). PASS (all, per run): (1) no BACK END LOST in 6 min after the ENGAGE dispatch;
(2) a "NAVIGATE GATE: ... is HELD" line when the ENGAGE is released; (3) the ENGAGE dispatches after its members end
("RELEASED") or at the bound ("reached the bound ... DISPATCHING ANYWAY") and the bound is logged; (4) sampler flat,
no abort. A run that fails: STOP, the gate stays off main, report.
Prediction (MEDIUM): HELD at release with ~13 members out; CLEARED within ~3 min or BOUND at 480 s (arm (i): 10 members
never ended - then BOUND, and the late re-task ran on); no stop either way.
If 2 runs pass: merge to main after review, deploy (rebuild main), one re-pre-run of today's demo with the gate on:
37/37 exported, xsd PASS, same admitted tasks as the 22:38Z re-pre-run, no BACK END LOST, STP-1147/1149/1150 holding.

## D16 RESULT (2026-10-15 01:06-01:22Z; runs/launch52/d16-pro; app 5448-5452): PASS (the Local seat's verdict)

stp-pro up EMPTY 01:06:53Z; Pro (stp-20, add-in v33x from run 38650876426) connected and pushed its own plan; the seat
then posted baseline + COA 1 with replace=true (95 symbols / 37 tasks posted, confirmed; server 94 / 37), pushed the
Initialization through stp-pro (26 of 26 bound, late - READY TO TASK not reached in 20 s, then bound), and on stp-20's
"Reports On, go" (Reports On 01:14:33Z) pushed the order at 01:15:28Z: 28ID HoldInPlace (01:15:37Z) and 1-112 IN EVACT
(01:15:38.9Z, navigate-to-location) -> HIDE. Interface: 0 BACK END LOST, three TASKCMPLT, working set flat (max 3.3 GB).
STP: 0 "unknown uuid"; StpListen 28 SymbolReport, 18 poids, 2 changed. Pro (stp-20's report): 1-112 IN moved through
4 reported positions (01:16:03-01:17:03Z), 1,549 m from plan, ending 1 m from the EVACT target, then held; 28ID
re-drawn in place; 10 "report unit" updates; Reports Off 01:20:35Z -> back at its planned position within 10 s, all 30
Iron Storm features present. Teardown: StopVrf exit 3 -> forced exit 6; stp-pro removed; RTI and holder intact.

## D47 G1 RESULT (2026-10-15 01:28-01:44Z; runs/launch52/d47/G1; app 5453-5457): FAIL - THE GATE DOES NOT PREVENT THE STOP

Gate build (feat/navigate-gate 2cfac85, from the worktree; startup line "NAVIGATE GATE ON ... 480 s WALL"). CONSTR
dispatched 01:30:43.5Z; it ran OVERDUE and completed by arrival evidence, releasing the ENGAGE, which the gate HELD
("10 member(s) of its previous move still have their navigate-to-location out"). The last member ended ("the LAST of 17:
17 succeeded, 0 failed") and the gate RELEASED it after 136.3 WALL s ("the work in flight ended"); ENGAGE dispatched
01:38:29.524Z with NO member move outstanding. The vendor log's last write: 01:38:29.901Z (+0.38 s, read after the forced
exit); SIM/WALL 2.77 -> 1.000; BACK END LOST (last good 01:40:22Z); working set flat 3.79 GB, ~1.9 cores spinning; StopVrf
exit 3 -> forced exit 6; RTI and holder intact; no dump. The ENGAGE leg's pre-flight flagged NO water, and the vendor log
holds 0 "Start point is within obstacle" lines.
PER THE RULE ("if a run fails: stop, keep the gate off main, and report"): STOPPED after G1; G2 not run (5458-5462
burned); the gate stays on its branch; no re-pre-run with it.
WHAT G1 FALSIFIES: "members still moving" is NOT necessary (0 outstanding, all 17 succeeded - stopped); "a wet / in-obstacle
start" is NOT necessary either (no water on the leg, 0 in-obstacle lines - stopped). The D36 (a2) reading is withdrawn.
Stops so far: 19:45Z, F, H, G1 - all a SECOND navigate-to-location to 48 IBCT's 17 members, to the SAME point the first
move went to, (54.0403,23.3364), after a STREND release. No stop: (i) the same point by a separate order 8.25 min later
(10 members stuck); (ii) a different, dry point (54.000,23.290). The only common factor left in the stops is that place /
that re-task shape; nothing here separates them, and n is small. The cause is OPEN; this needs the vendor (MAK) - a
reproducible hang with a one-line recipe - rather than more interface-side guessing.
