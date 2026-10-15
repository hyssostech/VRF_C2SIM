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
