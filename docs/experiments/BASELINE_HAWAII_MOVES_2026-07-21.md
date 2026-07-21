# WORKING BASELINE - stock MAK HawaiiGround moves headless (2026-07-21)

First "something that actually works" after many days of tail-chasing. A stock, MAK-authored
scenario was run HEADLESS with ZERO of our C2SIM creation/tasking code, and its units moved.
User directive that set this up: build a minimal working scenario from the MAK docs, learn
from it, THEN audit the accumulated hypotheses against reality (not guesswork).

## What was run
- LaunchVrf.ps1 -Scenario HawaiiGround (stock MAK scenario, userData/scenarios/HawaiiGround.scnx;
  EntityLevel.sms model set; authored .pln with move-along / convoy / move-to tasks).
- tools/RunSim (NEW, this session) started the sim clock at 10x. RunSim is a ~120-line copy of
  tools/SetSimRate that calls VrfBridge.Run() (controller->run()) instead of SetTimeMultiplier.
  VrfBridge.Run() was ALREADY a public managed method (VrfBridge.cpp:206) - NO native rebuild,
  no 7-DLL redeploy; pure `dotnet build`.
- tools/WatchVrf observed positions before and after the clock started.

## Result - UNITS MOVE
- Clock advanced (WatchVrf simTime 3.0 -> 58.3 s).
- 22 of 148 observed objects moved > 50 m; top net displacements 1862.8 / 820.2 / 750.0 /
  747.2 / 319.4 m. Real directed movement along authored routes.
- Teardown CLEAN: StopVrf exit 0, GUI quit modal answered via UIA, "Quit All Back-Ends"
  carried the back-end, RTI infra (rtiAssistant/rtiexec/rtiForwarder) preserved. No force-kill.

## What this PROVES (retires guesswork)
1. The load-scenario -> RunSim(start clock) -> WatchVrf(observe) pipeline WORKS headless, with
   zero C2SIM code. This is a reusable known-good harness.
2. VR-Forces moves authored units correctly. A freeze is NOT a fundamental "VRF cannot move
   units" problem, nor a broken sim/observer. RunSim and WatchVrf are validated instruments.
3. On Hawaii terrain, movement is fine - so the movement failure we chase is not universal.

## appNos / ledger
Consumed 3547-3552 (marker now 3553). 3550 BURNED (RunSim first attempt was invoked with the
wrong cwd -> vrfLegion.lua/FDD not found -> CouldNotOpenFDD; never drove the sim; per never-reuse
it was burned and a fresh 3551 used). LESSON: RunSim/WatchVrf/SetSimRate MUST run with
cwd = C:\MAK\vrforces5.0.2\bin64 (they load vrfLegion.lua + the FDD relative to cwd).

## Caveats (do not overread)
- WatchVrf crashed near t=58 s sim (a .NET SEH exit) on an undecodable artillery signal
  interaction once combat began ("Error decoding VRF Object Message type 1"). It still captured
  1635 POS rows - enough to establish movement unambiguously. A longer/robust observation would
  want WatchVrf hardened against that message, but it is not needed for this result.
- A few uuids show NaN coordinates (the known cast-corruption on certain object types). The 22
  movers are clean-coordinate objects.

## AUDIT NEXT (the user's directive: test hypotheses against THIS reality)
The baseline moves on HAWAII terrain; our C2SIM units freeze at MOJAVE. The decisive audit:
1. Confirm whether the Hawaii movers are AGGREGATES (units) or individual entities - read
   HawaiiGround.oob / the CON channel. If aggregates move here, "aggregates can't move" dies.
2. THE test that adjudicates region-vs-code: run a properly-authored aggregate WITH a move plan
   on MOJAVE terrain (author/transplant a minimal scnx onto the TropicTortoise terrain, or find
   a Mojave scenario with an authored moving aggregate). Outcome decides:
   - Authored aggregate FREEZES at Mojave -> the Mojave leader-path-empty is region/terrain, not
     our unit structure (H1 stands; our-structure theories weaken).
   - Authored aggregate MOVES at Mojave -> the failure is that OUR remote-created units are
     structurally different from authored ones (H2 stands); diff ours vs the working authored one.
3. Only after (2) do we touch the port again, aimed at whatever reality selected.
