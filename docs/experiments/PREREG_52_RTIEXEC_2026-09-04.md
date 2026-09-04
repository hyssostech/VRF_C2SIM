# PREREG - the DOCUMENTED 5.2d HLA posture: MAK RTI 5.0.1 in rtiexec mode, interface 127.0.0.1

Date 2026-09-04. Tier HEAVY (a cause claim rides on it). Docs consulted (cited, read
before writing): UG52 5.5.1 p190 "You cannot use the MAK RTI in lightweight mode with
VR-Forces" (verified in the PDF by the seat); UG52 Tables 10/11 (--deviceAddress /
--hostAddressString on vrfGui and vrfSim); IOG 5.2.1 p81 (default interface = first
device); RTI Users Guide 5.0.1 sec 7.3 p73 (what an assistant rtiexec connection sets);
RTI Ref Manual 6.3.1 / 14.3 (lightweight mode does not merge FOM modules; modular FOMs
need rtiexec); RESEARCH_52_HLA_CONNECTION_CONFIG_2026-09-04.md (the 5.0.2 -> 5.2d
parameter table from vrfLauncher.pdf and the C2SIM interface README/.bat files).

## 1. Frame
Every 5.2d run so far violated UG52 5.5.1 (lightweight MAK RTI) and never supplied the
interface address the 5.0.2 configuration fixed at 127.0.0.1. This run applies the
documented posture with NO other change: RTI 5.0.1, rid-configured EXACTLY like an
assistant rtiexec connection (config/rid-501-rtiexec-min.mtl: the twelve parameters
RTI UG 7.3 lists, nothing else), the headless rtiexec already up (pid 15720, forwarder
43728, 127.0.0.1:4001/5000), sim and observers pinned to 127.0.0.1 (--deviceAddress +
--hostAddressString on the sim via LaunchVrf52 -DeviceAddress; the facade now pushes
--deviceAddress 127.0.0.1 on its 5.2 HLA argv; RTI_networkInterfaceAddr 127.0.0.1 in
the rid). Scenario Sample\Traffic (vehicle-only; the DIGuy scenario crashes the sim).

## 2. Predictions (before launch; a missed HIGH = STOP)
P1 HIGH: the sim does NOT crash in DtVrfSimOptions::parseCmdLine and joins MAK-ONE-2025
   (log "Joined federation"). Falsifier: the 38180-style callstack again -> the crash is
   NOT the three extra rid keys of rid-501-rtiexec.mtl; the rid-configured rtiexec route
   is then dead and the assistant connections.xml route (research sec 5c) is next.
P2 MEDIUM: RunSim-5.2 discovers the back-end (BackendCount=1) and starts the clock.
P3 MEDIUM (the one that matters): WatchVrf-5.2 --diag --report-backends reflects >= 1
   entity (ent >= 1 in the direct list counts) within 60 s of a running Traffic scenario.
   Falsifier: all counts 0 with P1 and P2 held -> rtiexec mode + interface pinning are NOT
   sufficient; remaining candidates M3 (requiredFomClasses / module set) and H4 (no entity
   exists) - next discriminator = the sim's own RTI log-file trace (rid RTI_logFileName).
Success = P1 AND P3. P3 alone decides whether the observation channel is repaired.

## 3. Procedure (numbers ledgered BEFORE each join; every run tee'd to runs/launch52/)
1. LaunchVrf52 -RtiDir 5.0.1 -RidFile rid-501-rtiexec-min.mtl -NoGui -DeviceAddress
   127.0.0.1 -Scenario Sample\Traffic (3854). Check the log for join / crash.
2. RunSim-5.2 (3855) under the same env. 3. WatchVrf-5.2 --diag --report-backends 60 s
(3856). 4. Fill sec 4; relabel the observation channel if P3 holds.

## 4. Result (2026-09-04 06:25 local; captures runs/launch52/*3854-3856*)
P1 HELD: sim 3854 joined MAK-ONE-2025 on the rtiexec connection, no crash (log 6212 lines,
loaded, 71 threads). CORRECTED by COLDSTART_REVIEW_RTIEXEC_2026-09-04: the earlier
parseCmdLine crash (3848) is NOT explained by rid keys - run 3853 (LIGHTWEIGHT rid, direct
launch) crashed with a byte-identical callstack (C:\MAK\logs\...39028.callstack.log; the
ledger row 3853 was mislabelled "stalled"). The crash hit 2 of 5 sim launches today on both
rid types; TRIGGER UNKNOWN - the launch stage must detect it (callstack file / Error window)
and fail loudly. rid-501-rtiexec-min differs from stock 5.0.1 in SIX keys (the other six of
the "twelve" are stock values) and from rid-501-rtiexec in ONE (tcpNetworkInterfaceAddr;
distributeFedFile/fomModuleMerging are 1 in stock). rid-min is the profile rid.
P2 HELD: RunSim 3855 BackendCount=1 in 0.1 s, clock started.
P3 HELD - THE OBSERVATION CHANNEL IS REPAIRED: WatchVrf 3856 direct list counts ent=44 at
t=3 s rising to ent=62 / env=19 / ctl=19 at t=53 s (vendor printReflectedObjectCounts:
Reflected Entities 62, Total 81); UUID-callback reflected=67 -> 106; real POS lines
(21.29xx, -157.86xx, alt 1.0) plus the known placeholder encoding (NaN, 90, NaN) for
objects without a resolved location. discovered=1, backends=1, licence rti=1 vrlink=1.
CAUSE (HEAVY - falsification gate, corrected after COLDSTART_REVIEW_RTIEXEC_2026-09-04):
(a) MAK RTI in rtiexec mode instead of the prohibited lightweight mode (UG52 5.5.1 p190,
verbatim, verified in both PDFs; 5.0.2 only forbade it for multiple federations) is
SUPPORTED BY ELIMINATION - no mechanism is named for WHY lightweight passed one object
class (MAK_TimeAndDate) and every interaction yet dropped entity classes; that residue
stays OPEN. (b) the VR-Forces-level --deviceAddress/--hostAddressString 127.0.0.1 is NOT
established as part of the cause: the RTI-layer interface (RTI_networkInterfaceAddr) is
inherent to the loopback-broadcast rtiexec connection, and every lightweight run already
had sim and observer on one interface with traffic crossing both ways. It is carried as
the 5.0.2 configuration's value, TUNABLE and UNTESTED; discriminator = WatchVrf with
--deviceAddress suppressed against the live rtiexec sim (observer side), then a sim
relaunch on rid-min without -DeviceAddress. VALID negative controls: 3839 (4.6.1
lightweight, Traffic RUNNING, 60 s complete), 3845 (5.0.1 lightweight, paused), and the
vendor listen_3838 (Traffic running, joined, 30 s, 0 entities). NOT valid: 3847 (cut at
3 s), listen_3829/3834 (sim 3826 had crashed). "The scenario is the difference" is
REFUTED by 3839 and 3838. Evidence notes: the 19 (NaN,90,NaN) POS lines are the 19
CONTROL objects (readable = ent + ctl every sample), not unresolved entities; env=19 ==
ctl=19 at every sample is a ReflectedCounts() instrument anomaly (env accessor) to fix;
the rtiexec log shows at most two federates joined, so the unlicensed-for-two cap is
UNTESTED; "received size does not match header size" appears in every 5.0.1 lightweight
federate, unexplained; the launch_3854 stdout capture was not written (Tee path) - the
sim log is the launch evidence.
P4 (added 2026-09-04 before run 3857, MEDIUM): WatchVrf-5.2 --device-address none (the
facade pushes NO --deviceAddress; VR-Forces picks its first device) against the LIVE
rtiexec sim 3854, 60 s, STILL reflects >= 1 entity -> the VR-Forces-level device address
is NOT required on 5.2 and drops from the posture (kept only as a tunable). Falsifier:
ent=0 for 60 s with backends=1 -> it IS required observer-side; then a sim relaunch
without -DeviceAddress decides the sim side.
P4 HELD (watchvrf_3857_rtiexec_nodevaddr.txt): device-address=none, ent=56 -> 54 over
60 s, vendor count Reflected Entities 55 / Total 74, 323 real POS lines, backends=1,
licence rti=1. The VR-Forces-level --deviceAddress is NOT required observer-side; it
drops from the posture (tunable, default = not passed). Sim side: still launched WITH
-DeviceAddress 127.0.0.1 (3854); the runner profile's first run launches WITHOUT it and
closes that half. Posture now = MAK RTI 5.0.1 in rtiexec mode (rid-501-rtiexec-min +
headless rtiexec), nothing else.
CONSEQUENCES: the assistant-free LIGHTWEIGHT rid (config/rid-461-ridconfigured.mtl,
2026-09-03) was a WRONG fix - it bypassed the version-locked assistant but put the
federation in a mode VR-Forces does not support; assistant-free stays (RTI_ASSISTANT_
DISABLE) but the rid must be the rtiexec posture, and an rtiexec must be running
(headless start: rtiexec.exe -M -R <rid> -P 4001 -T 4001 -A 127.255.255.255 -N 127.0.0.1
-i 127.0.0.1 -D 5000 -r, started 2026-09-03 21:4x, pid 15720 + forwarder 43728 - it starts
its own forwarder). The runner's 5.2 profile must carry: RtiDir 5.0.1, rid-501-rtiexec-min,
-DeviceAddress 127.0.0.1, a start/verify-rtiexec stage (never kill), and tools on the same
rid. 5.0.2 golden-path runs (4.6.1, assistant rtiexec loopback) were ALWAYS rtiexec mode.
