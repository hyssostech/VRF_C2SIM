# PRE-REGISTERED PROBE: does VR-Forces deliver task reports to an observer federate?

Written BEFORE the run, per the standing rule (one variable; prediction and falsifier
recorded in advance). Nothing below may be edited after the run - append results instead.

## The question

Run 20260719T144109Z tasked three units with MoveAlongRoute. Two moved 0.0 m bit-exactly
across 76 samples; the third snapped 63 m AWAY from its objective and froze for 136 s. The
trace carried positions only, so it could not say whether VR-Forces ACCEPTED, REJECTED or
SILENTLY DROPPED the tasks. The Object Console stream that would normally explain this
returned ZERO lines and diagnosing that needs a native rebuild.

WatchVrf now also consumes TaskCompleted and TextReport - events that were already wired
in the facade and merely unsubscribed - emitting TSK and RPT lines. This probe asks ONE
question:

    DOES A TSK OR RPT LINE EVER APPEAR IN AN OBSERVER FEDERATE'S TRACE?

## Design: single variable

Identical inputs to run 20260719T144109Z - data/R9_Mojave_Lean_Initialization.xml +
data/R9_Mojave_UnitMove_Order.xml, -RunSecs 120. THE ONLY CHANGE IS THE INSTRUMENTATION.
Same scenario, same terrain, same three taskees, same duration, same runner.

DELIBERATE DEVIATION, recorded: the supervisor proposed "one unit, one task" for this
probe. Rejected, because authoring a new single-unit C2SIM order risks the RUNBOOK 0.6 XML
comment gotchas (a block comment anywhere in an order file crashes the receiving STOMP
client) and would produce data NOT directly comparable to run 1. Re-running the identical
case with only the instrumentation changed is the stricter single-variable test.

## Predictions, recorded in advance

OUTCOME A - TSK and/or RPT lines APPEAR.
  Then the report channel DOES reach an observer federate. The empty CON stream is then
  NOT a generic "reports are not broadcast" problem, and the console work needs its own
  explanation. We can classify accepted vs rejected vs dropped from the TSK content, and
  the native rebuild may be avoidable.

OUTCOME B - TSK and RPT are BOTH EMPTY.
  Then the most likely reading is that VR-Forces addresses these reports to the REQUESTING
  federate rather than broadcasting, so an observer joined under its own applicationNumber
  never sees them. This would share a root cause with the empty CON stream. THIS IS THE
  OUTCOME THAT AUTHORISES THE NATIVE WORK, because logObjectConsoleToFile has the BACKEND
  write to a file and is then the only route to a positive answer.

WHICH DO I EXPECT? Outcome B, with low confidence. Recorded so it cannot be claimed after
the fact either way.

## What would FALSIFY the reading, and what this probe CANNOT settle

- An empty TSK stream is NOT proof of task rejection. A task that is ACCEPTED and never
  COMPLETES also produces no TSK line. Absence is not an acceptance oracle in either
  direction. This probe tests DELIVERY, not acceptance.
- If RPT populates while TSK stays empty, the "reports are not delivered to observers"
  reading is FALSIFIED - both travel the same facade trampoline
  (VrfFacade.cpp:206-231, both raised from reportTrampoline, registered in Start()). That
  split would instead point at task completions specifically never being RAISED, i.e. the
  tasks never completed - which is consistent with units that never moved.
- A positive TSK line naming a unit that did NOT move would be a LYING COMPLETION and must
  be recorded as such (4a.5 FALSE COMPLETION), not read as success.

## Success criterion for the probe itself

This probe succeeds if it produces a DEFINITE answer to "did any TSK or RPT line appear",
regardless of which. It is not scored against 4a - movement is expected to reproduce run
1's result and that is not what is being tested.

## Pre-conditions verified before launch

- 0 VR-Forces processes; 3 RTI processes up and to be preserved
- C2SIM REST HTTP 200
- Appendix B marker reads 3516; appNos ledgered BEFORE any join
- Instrumented WatchVrf.dll built 2026-07-19 11:59; VrfBridge.dll sha A48ABE6C (unchanged,
  no native rebuild - this is a C#-only change)

## RESULT (appended after the run - nothing above was edited)

Run 20260719T161438Z. appNos 3516-3521, ledgered before the join. Runner EXIT=0 (the
StopVrf back-end fallback worked - teardown clean, RTI preserved).

    POS lines 4512    RPT lines 84    TSK lines 0    CON lines 0

## OUTCOME: NEITHER A NOR B. THE PRE-REGISTERED FALSIFIER FIRED.

The prediction (OUTCOME B, low confidence) was WRONG. RPT populated while TSK stayed
empty - which the pre-registration explicitly names as FALSIFYING the "reports are not
delivered to observer federates" reading, because both are raised from the same facade
trampoline (VrfFacade.cpp:206-231, registered in Start()).

WHAT THIS ESTABLISHES: the report channel DOES reach an observer federate. TSK is empty
NOT because we cannot hear completions, but because NO TASK EVER COMPLETED. That is
consistent with, and independent of, the position evidence.

It also means the empty CON stream needs its OWN explanation - it cannot be waved away as
"reports are not broadcast", because reports demonstrably are. CON travels a different
channel (Comment PDU/Interaction) from RPT/TSK.

## THE UNPLANNED FINDING, AND IT IS THE BIGGEST ONE: THE TWO ORACLES CONTRADICT

RPT lines carry MARKING TEXT, which incidentally solves the member-to-parent mapping the
runaway census called unsolvable offline. 28 named objects appear. 27 of them show 0.0 m.
Only 1222.MechPlt moves - and the two channels disagree about WHICH WAY.

1222.MechPlt longitude (objective lies EAST at -116.587860; start ~-116.6005):

| source | t~33 | t~95 | t~157 | reading |
|--------|------|------|-------|---------|
| RPT (VR-Forces' own position report) | -116.600485 | -116.599567 | -116.598583 | steady EAST, 174.1 m, ~1.4 m/s |
| POS (our DR-extrapolated oracle)     | -116.600476 | -116.601205 | -116.601202 | jumped 65 m WEST at t~43, then frozen |

The POS series is 68 of 76 samples parked around -116.6012, sawtoothing +/-1.5 m on a ~6 s
period about a FIXED mean. RPT is monotonic eastward at a plausible vehicle crawl.

*** THE CONTROL IS WHAT MAKES THIS CONCLUSIVE. *** For the two units that did NOT move,
the oracles agree EXACTLY - 114.MechCoy and 1.BdeHQ each report one identical coordinate
in RPT (3 fixes) and ONE distinct position across all 76 POS samples. The channels agree
perfectly on stationary objects and disagree ONLY on the moving one. That is the signature
of a TRACKING failure in POS, not a random discrepancy between two noisy sources.

## CONSEQUENCE - the run-1 headline is PARTIALLY OVERTURNED

NOT "the units did not move". The corrected reading:
- 114.MechCoy: GENUINELY FROZEN. Confirmed by two independent channels.
- 1.BdeHQ (entity): GENUINELY FROZEN. Confirmed by two independent channels.
- 1222.MechPlt: **IS MOVING TOWARD ITS OBJECTIVE** at ~1.4 m/s per VR-Forces' own report,
  and OUR PRIMARY MOVEMENT ORACLE FAILED TO TRACK IT - reporting it 65 m in the OPPOSITE
  direction and stationary.

At 1.4 m/s the ~1157 m route needs ~830 s. The 120 s observation window was never going to
show an arrival for it. The earlier claim that "duration is not the binding constraint" was
right for the two frozen units and WRONG for this one.

## WHY THIS MATTERS MORE THAN THE MOVEMENT RESULT

WatchVrf POS displacement is the standing evidence rule - "the ONLY movement oracle". This
run shows it misreporting a moving unit's DIRECTION. Every negative movement result this
project has recorded rests on that oracle. The runaway/warp census's leading hypothesis was
exactly this (observer-side dead-reckoning artifact, VrfFacade.cpp:737 sr->location() reads
THROUGH the approximator). This is no longer a hypothesis with a plausible mechanism; it is
an observed contradiction with a clean stationary control.

STATUS: ESTABLISHED that the two channels contradict, with a control. INFERRED, not proven,
that RPT is the truthful one - it is VR-Forces' own report of its own object and it is
kinematically plausible, while POS's sawtooth-about-a-fixed-mean is the classic stale-state
signature. NOT SETTLED offline.

## THE DECISIVE TEST IS NOW UNAVOIDABLE

The raw-vs-DR comparison scoped on 2026-07-19: log lastSetLocation() (the raw received
value, baseEntityStateRepository.h:118) alongside location() (the approximator-extrapolated
read, :113) on the same row. If raw tracks RPT eastward while extrapolated shows the frozen
western position, the oracle failure is proven and its mechanism identified.
This REQUIRES A NATIVE REBUILD (VrfFacade.cpp/.h + VrfBridge.cpp). It is ~35 lines against
a toolchain proven healthy the same day (full /t:Rebuild, 0 errors, 9 s), it needs ZERO new
casts because the accessors sit on DtBaseEntityStateRepository which VrfFacade.cpp:723
already holds, and the same rebuild should carry logObjectConsoleToFile
(vrfRemoteController.h:1983) to settle the CON question positively.
