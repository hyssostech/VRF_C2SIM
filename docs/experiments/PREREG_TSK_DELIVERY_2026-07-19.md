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
