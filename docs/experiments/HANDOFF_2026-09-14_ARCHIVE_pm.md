# HANDOFF 2026-09-14 pm - archive

Verbatim material collapsed out of docs/HANDOFF_2026-09-14_PARALLEL_LANES.md on 2026-09-14 pm to
hold that file under its 200-line cap. Nothing here is a new claim; every block is an exact copy of
text that used to live in the entry doc, kept so no settlement was lost in the collapse. This file
has no line cap of its own. ASCII + CRLF, per repo convention.

## sec 1 - Early stops sec 6-7d, full recap (was HANDOFF section 1, "Early stops")

Early stops (docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md secs 6-7d - point at it, not restate):
- sec 6 VENDOR PASS: a vehicle that stops while its task still runs is UNDETECTED BY DESIGN in 5.2
  (the base give-up test "always returns false"; the sample hands that test to the integrator).
- sec 7 CAUSE, SUPPORTED for the units that freeze ON a scored face (1-35, 4-27): seven consecutive
  8 m postings rising at 0.70-0.95 over 55 m on sand ahead of the freeze; the sampler reproduces the
  sim's own reported altitudes to ~3 cm.
- sec 7b SCOPE: this is a claim about those units, NOT about "the early stops". A SECOND mechanism is
  active (1-6 stopped on DESCENDING ground; 856/HHC on FLAT ground). Pre-flight calibration as
  corrected: 40 m window, threshold 0.92, margin 0.097, 3/3 caught, 0/6 false alarms - one order,
  one terrain, three positives.
- sec 7c it is the LEADER that freezes; followers crawl past it. The console excludes blockage,
  replan, give-up and goal change - it does NOT exclude formation speed control.
- sec 7d a THIRD stop kind: a move-along subtask FAILED ("Entity not embarked on same object as
  target"), VR-Forces re-formed the unit, and the task-less hull was PUSHED 1,994 m by the vehicle
  behind it. Two consequences that bind: (a) a vendor task FAILURE produces NO C2SIM report today
  (reporting item B1); (b) in a collapsed-ratio capture the observer's dead reckoning draws sawtooth
  motion - CHECK THE ALTITUDE RESIDUAL before reading any excursion as movement (G2's post-collapse
  tail is withdrawn as motion).

## sec 2 - Navigation mesh, full recap (was HANDOFF section 1, "Navigation mesh")

Navigation mesh (PREREG_NAVDATA_G6 sec 5 = STOP; MESH_QUERY_VS_DISTANCE_2026-09-14 sec 0):
- G6 STOPPED on its instrument gate: the 41 x 54 km area loaded, BOTH nav-area gates returned
  success, and vrf:findPathToLocation returned "not enough (0) points" for every multi-kilometre
  goal. G6 is therefore a FOURTH reproduction of the stops, not a discriminator.
- The dependence is on the AREA, not on goal distance: 10,131.6 m planned twice on the 1,600-sector
  MojaveAO20, 0 points on the 8,856-sector MojaveCOA for BYTE-IDENTICAL destinations; 22 pairs flip
  that way, none the other way. Refusals return in 0.4-1.3 s, FASTER than the 2.4-4.2 s long
  successes - a refusal, not an exhausted search. Frame correction: G1 ran on MojaveAO20; there
  never was a 3 x 3 km COA area.
- The generator accepted a 41 x 54 km area: UG52's 20 x 20 km is a GUI default, not a generator limit.
- G6/G7 CLOSED 2026-09-14 17:15Z: the refusal was the vendor script's hard-coded
  useAbstractGraphs = false (ground-vehicle-move-to.lua:488), not an area-size or memory/budget
  limit. The custom including SMS (C:\C2SIM\vrf-sms) planned the 10-seam, 4,989 m leg 8/8 with 0
  refusals (G7b); gamewareMemorySize and gamewareQueryTimeBudget are NOT the lever (G8/G8b still
  refused on the stock SMS). Record: docs/experiments/G7B_G8_RESULTS_2026-09-14.md (in progress);
  OPEN: whether abstract-graph paths avoid the ridge faces (FINDING_EARLY_STOPS).

## sec 3 - Harness / runner-exit-127, full recap (was HANDOFF section 1, "Harness")

Harness (docs/experiments/RUNNER_EXIT127_2026-09-14.md; RUNBOOK 0.5.14):
- "runner exit: 127" does NOT mean "command not found". On this MSYS bash it is a high-bit Windows
  exit code; the only SILENT one is 0xFFFFFFFF = TerminateProcess(-1) = Stop-Process / Process.Kill.
  127 + a silent log + no WER event = THE RUNNER WAS KILLED FROM OUTSIDE. The killer is unidentified
  (no process auditing on this machine). The G6 capture was COMPLETE; "trace: (no samples)" was a
  separate tail-read defect, fixed. NO Stop-Process / taskkill sweeps while a run window is open.

## sec 4 - Lane table, full per-lane state as of 2026-09-14 ~13:00Z

(was HANDOFF section 2's "State 2026-09-14 ~13:00Z" column; superseded by the live
docs/PLAN_PARALLEL_LANES_2026-09-14.md and, within the entry doc, by section 1 where later.)

| Lane | What it is | State 2026-09-14 ~13:00Z | Jira |
|---|---|---|---|
| L1 | Mesh-query refusal | attempts 1 and 2 VOID (tripwires above); ATTEMPT 3 (tank platoon 1222.MechPlt) launched 13:04Z as runs/20260914T130439Z_run - verdict PENDING, PREREG_MESHQUERY_G7 sec 5 | STP-788 epic; STP-790/791/792/793 |
| L2 | Reporting | assessment done (11 gaps, B1-B10); B5/B3/B1/B2/B8 built on feat/reporting | STP-777 epic; STP-778..787 |
| L3 | Watchdog + sim clock | feat/sim-clock f052ea7, reviewed to pass 4, supervisor-verified; MERGE AFTER G7 | STP-783 |
| L4 | Runner robustness | landed on main (f6e68d9); first live use = the G7 attempts | STP-789 epic; STP-794 |
| L5/L10 | Records | this file + docs/DEMO_RUNBOOK.md (readiness row 13) | - |
| L6 | User-owned | licence renewal; DI-Guy package; TASKABRT decided; C:\MAK sanctions given | - |
| L8 | Task vocabulary beyond MOVE | assessment running (docs-first) | epic created 2026-09-14 |
| L9 | STP-side strict parse check | running (offline console test) | STP-615 |
| L11 | 4-27 second read + offset-line scoring | running | - |
| L12 | A2 detached run watchdog | running (scripts only) | STP-794 |

## sec 5 - Licence, superseded 2026-09-15 lapse warning (was HANDOFF section 4)

(Superseded 2026-09-14 pm: the licence was renewed to 2026-10-31, RUNBOOK 0.5.15, c8730e7. The
text below is kept verbatim as the record of what this doc said before that.)

- LICENCE: the node-locked DEMO licence LAPSES 2026-09-15. It gates the whole toolchain, including
  vrfNavGenerator. The renewal is user-owned and the download was still owed at 13:00Z.
