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
loaded, 71 threads). So the earlier parseCmdLine crash (3848) came from the three extra keys
in rid-501-rtiexec.mtl (distributeFedFile / fomModuleMerging / tcpNetworkInterfaceAddr) -
that rid is superseded by rid-501-rtiexec-min.mtl.
P2 HELD: RunSim 3855 BackendCount=1 in 0.1 s, clock started.
P3 HELD - THE OBSERVATION CHANNEL IS REPAIRED: WatchVrf 3856 direct list counts ent=44 at
t=3 s rising to ent=62 / env=19 / ctl=19 at t=53 s (vendor printReflectedObjectCounts:
Reflected Entities 62, Total 81); UUID-callback reflected=67 -> 106; real POS lines
(21.29xx, -157.86xx, alt 1.0) plus the known placeholder encoding (NaN, 90, NaN) for
objects without a resolved location. discovered=1, backends=1, licence rti=1 vrlink=1.
CAUSE (HEAVY - falsification gate): the run changed TWO documented settings at once
against every earlier run - (a) MAK RTI in rtiexec mode instead of the prohibited
lightweight mode (UG52 5.5.1 p190), (b) interface pinned to 127.0.0.1 on sim, observer
and RTI (the 5.0.2 configuration's fixed value). VERIFIED: with both, entities reflect;
with neither (every run 2026-09-03) nothing did. NOT separated: which of (a)/(b) is
necessary - the single-variable discriminator is one observer run with the interface
unpinned on the same rtiexec sim (needs a tool flag to blank DeviceAddress; the facade
now pushes 127.0.0.1 by default). Operationally moot: BOTH are documented requirements
(5.5.1 prohibits lightweight outright; the 5.0.2 setup pinned the interface), so the 5.2
profile carries both. Strongest competitor to "posture" as cause = "the Traffic scenario
simply has entities while the earlier scenarios did not": REFUTED - Traffic itself ran
on 4.6.1 lightweight (3835/3839, running clock) and on 5.0.1 lightweight (3844-3847) with
0 reflected. Unexplained residue: none material; the placeholder-encoded objects are the
5.0.2-known census pattern (run_census.py filters them).
CONSEQUENCES: the assistant-free LIGHTWEIGHT rid (config/rid-461-ridconfigured.mtl,
2026-09-03) was a WRONG fix - it bypassed the version-locked assistant but put the
federation in a mode VR-Forces does not support; assistant-free stays (RTI_ASSISTANT_
DISABLE) but the rid must be the rtiexec posture, and an rtiexec must be running
(headless start: rtiexec.exe -M -R <rid> -P 4001 -T 4001 -A 127.255.255.255 -N 127.0.0.1
-i 127.0.0.1 -D 5000 -r, started 2026-09-03 21:4x, pid 15720 + forwarder 43728 - it starts
its own forwarder). The runner's 5.2 profile must carry: RtiDir 5.0.1, rid-501-rtiexec-min,
-DeviceAddress 127.0.0.1, a start/verify-rtiexec stage (never kill), and tools on the same
rid. 5.0.2 golden-path runs (4.6.1, assistant rtiexec loopback) were ALWAYS rtiexec mode.
