# MAK support case: VR-Forces 5.2d back end stops on a repeated navigate-to-location

FOR THE OPERATOR (not part of the case text): ruled D51 "send it" (2026-10-15 ~11:24Z, relayed by the STP-1097
supervisor). The seat does NOT contact MAK - the operator or the support-account holder submits this. Coordinates: the
operator OK'd sharing them. Attachments: the folder MAK_BACKEND_HANG_2026-10-15/ next to this file (list at the end).
Nothing in it comes from C:\MAK\logs, and it carries no customer data (re-checked by grep for unit, scenario, place
and project names; the two project-internal startup-banner lines of the interface log are redacted, as are local paths
and the licence file name). Record: docs/experiments/REPRO_BACKEND_STOP_2026-10-15.md (and D36 / D47 for the history).

---------------------------------------------------------------------------------------------------------------------
CASE TEXT (paste from here)
---------------------------------------------------------------------------------------------------------------------

Subject: VR-Forces 5.2d sim engine stops (no crash) ~0.5 s after a repeated navigate-to-location to an aggregate's
subordinates - reproducible at one location

Product / environment
- VR-Forces 5.2d, back end vrfSimHLA1516e.exe (HLA 1516e, 64-bit) and front end vrfGui.exe on one Windows 11 Pro host.
- MAK RTI 5.0.1, VR-Link 5.10.
- Terrain: MAK Earth Aggregate (online) (.mtf). Simulation model set: AggregateTacticalLevel.
- Run mode: fixed-frame, run to complete (the simulation ran at ~5-10x real time).
- Tasks are issued by a remote-controller application (DtVrfRemoteController) on the same federation.

Summary
The sim engine (back end) stops simulating within ~0.5 s of a second navigate-to-location being sent to the 17
subordinates of an aggregate, when the destination is the location below. It does NOT crash: no callstack, no dump, no
dialog; the process stays up, its log stops being written, one to three of its threads run at 100% CPU indefinitely,
and its working set stays flat. Remote controllers then extrapolate the simulation clock at 1x and age the back end out
of their back-end list about two minutes later. The back end does not respond to a graceful close and has to be
terminated. Reproduced 5 times out of 6 at this location (once with a fully synthetic unit), 0 times out of 2 at a
control location.

Steps to reproduce
1. Load a scenario on MAK Earth Aggregate (online), model set AggregateTacticalLevel, and run it.
2. Create a US infantry brigade AGGREGATE, entity type 11:1:225:8:3:1:1, at 54.0194 N, 23.3139 E.
3. Create 17 subordinate aggregates AGGREGATED and attach them to it (AddToOrganization), on a ring of ~490 m radius
   around its point: 4 x "Stryker HHT (USA)", 12 x "Mech CO (USA, M2)", 1 x "Stryker Cavalry SQDN (USA)".
4. Send to EACH of the 17 subordinates the scripted task navigate-to-location with:
   destination 54.0403 N, 23.3364 E; obstacleQuery "MAK_OBSTACLE"; pathQuery "MAK_ROAD"; buffer 10;
   displayRoute false; query "".
5. About 60-90 s (wall clock) later - while most subordinates are still moving (in one run, after all 17 had already
   completed) - send the SAME task with the SAME destination to all 17 subordinates again.
6. Observe: within 0.3-0.6 s the back end's log stops and the back end no longer advances; its threads spin.

Control
The same steps with destination 54.000 N, 23.290 E (a different, dry location, same start): no hang in 2 of 2 runs -
both moves completed and the back end kept running.

Observed signature (every hang)
- Back-end log: the last write is 0.27-0.58 s after the second navigate-to-location is sent; nothing after it.
- The last log lines are subordinate re-plans: "Graph search phase starting", "Obstacle avoidance phase starting", and
  (in 4 of 5 hangs) bursts of "Failed to navigate off road: Start point is within obstacle".
- Process alive; 1-3 threads at 100% CPU each; working set flat (~3-4 GB); no callstack file, no dump, no dialog.
- Remote controllers: simulation time advances at exactly 1x after the stop (extrapolated); the back end leaves the
  back-end list ~110-125 s later.
- At the hang location the first move's route crosses a lake (OSM natural=water) ~0.75 km from the start.

What we have ruled out
- Rules of engagement (the stop happens with hold fire and with fire at will).
- Subordinates still moving at the second order (it also stopped when all 17 had completed their first move).
- Memory exhaustion (flat working set; this differs from a runaway-allocation case we saw earlier on another terrain).

What we can provide
- The interface log of a hanging run (every task sent, its variables and timestamps), 1 Hz working-set / CPU samples of
  the back end, and synthetic C2SIM input files that drive the repro through our C2SIM interface (attached).
- A process dump of the hung back end on request (it needs one more run in which we dump instead of terminating).
- Our back-end log of a hanging run on request (it contains the process environment, so we would send a reviewed copy).

---------------------------------------------------------------------------------------------------------------------
END OF CASE TEXT
---------------------------------------------------------------------------------------------------------------------

Attachments (folder MAK_BACKEND_HANG_2026-10-15/):
- interface_log_run_Ra_redacted.log - our C2SIM interface's log of the hanging synthetic run (lines 75 and 207 redacted;
  local paths, the scenario file name and the licence file name replaced by placeholders).
- backend_memory_cpu_1Hz_run_Ra.csv - 1 Hz working set / private bytes / CPU cores / threads of the back end.
- repro_init.xml, repro_order_a.xml (hang location), repro_order_b.xml (control) - synthetic C2SIM 1.1 files, schema-valid.
