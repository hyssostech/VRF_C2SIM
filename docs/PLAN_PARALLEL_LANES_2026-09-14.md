# PLAN - parallel lanes to the demo (2026-09-14, supervisor: Fable 5.1)

User, 2026-09-14 ~11:00Z: "Anything you can do in parallel? These nightlong unattended waits are not
helping us deliver this" / "Start looking into reporting in parallel" / "Make a plan".
Context: G6 STOPPED on its instrument gate (PREREG_NAVDATA_G6 sec 5); the runner was killed from outside
and left the sim up for nine hours (RUNNER_EXIT127); the DEMO licence lapses 2026-09-15 (user holds the
renewal, download owed). Gate for this document: PLAN (a written scope; the user corrects it here).

## 0. Rules that shape the plan
- The ONLY serial resource is a TIMED RUN (~25 min of an idle machine). Everything else runs in parallel:
  executors in worktrees, analyses on captures, docs reads, builds on branches.
- A timed run is polled from the FOREGROUND with bounded waits; the supervisor never ends its turn while a
  run is open (lesson 2026-09-14). The runner's teardown is out-of-process (lane L4) before the next run.
- One variable per preregistered run; every claim of cause carries its falsifier; the CLOSED lists in
  DESIGN_ORBAT_TO_VRF and the FINDING remain canonical.
- Model tiering: Sonnet extracts (docs), Opus executes/verifies/reviews, the supervisor decides.

## 1. Lanes (all live now unless marked)
| # | Lane | Objective / deliverable | State 2026-09-14 11:30Z | Next step | Gate | User? |
|---|---|---|---|---|---|---|
| L1 | Mesh query refusal | Why vrf:findPathToLocation returns 0 points for every multi-km goal inside a loaded 41x54 km area (G6 sec 5). Deliver PREREG G7 with ONE variable. | Docs read DONE (NAVMESH_QUERY_DOCS): the query's documented failure predicates are outOfWorkingMemory / hadComputationError (class_dt_nav_area_query), gamewareMemorySize 16 MB is the working-memory cap "for larger nav areas" (vrfSim.mtl:383, UG52 p1670), the shipped ground-vehicle-move-to.lua calls the query with useAbstractGraphs = FALSE (:488) although the API says true "can speed up long path planning queries" and the area carries 8,810 abstract-graph files. Goal-distance pairing across G1/G2/G3/G6 RUNNING. | PAIRING DONE 11:50Z: AREA-dependent (10.1 km planned on the 1,600-sector area, 0 points on the 8,856-sector
area for the same bytes; fast refusals) -> G7 = the CROSS-SECTOR PROBE (one unit, 0.6 / 2 / 5 km legs on MojaveCOA,
consoles 4; order being authored) decides refusal-vs-disconnected-data; G7b = the abstract-graph SMS only if the
data is connected and 5 km fails; (b) design a CUSTOM INCLUDING SMS (UG52 68.3.1 / Migration 1.2.2) that overrides only ground-vehicle-move-to.lua with useAbstractGraphs = true, at a repo-owned root if the SMS reference accepts an absolute path (survives reinstalls; else user sanction for the location); (c) PREREG G7: variable = useAbstractGraphs; falsifier = still 0 points -> G8 variable = gamewareMemorySize via --appDataDir (UG52 Table 11 p178, the reinstall-safe mechanism). | PREREG | SMS location if under C:\MAK; G8's appData relocation |
| L2 | Reporting | What STP and the audience SEE: position reports, TaskStatus (TASKCMPLT / TASKABRT), ObservationReports (pre-flight warnings, watchdog). Assessment -> ranked build list -> builds. | Assessment RUNNING (Opus): what is emitted today and how validated; what G6 emitted (14,698 reports); the SDK deserialization error at 00:30Z; what the record says STP displays; gaps. | Build the top items on a branch with self-tests; wire pre-flight ObservationReports at order receipt; decide TASKABRT semantics. | STANDARD; RULE for TASKABRT | TASKABRT code decision |
| L3 | Watchdog + sim clock | C16 progress watchdog (report-only, default OFF) with the window on the back end's scenario clock; calibrated 240 wall / 360 sim (RECAL_STALL_SIMSECONDS). | Pass-3 fix RUNNING on feat/sim-clock (pass-2 review: 4 MAJOR on the sim path). | Pass-3 cold review -> merge watchdog + sim-clock into main (StallClock default wall) -> rebuild main -> the next run pins that build (watchdog OFF, so no behaviour change). | review-until-clean (max 4) | none |
| L4 | Runner robustness | A run can never leave the sim + interface running; status line truthful on a 1 GB trace; 64-bit host enforced; no inherited stdout pipe. | DONE 12:30Z (f6e68d9): 64-bit gate, launched/teardown markers + wrapper backstop, block-scan status line, incremental log read, RUNBOOK 0.5.14. A2 detached watchdog deferred; killer unidentified. | First live use = G7 (scripts/RunScenario.sh). | STANDARD | none |
| L5 | Records | This plan; the entry HANDOFF doc; DEMO_READINESS rows 19-22; memory. | Current. | Update the same turn a lane settles. | - | none |
| L6 | User-owned | Licence renewal (row 12, lapses 2026-09-15); DI-Guy data package (row 17, any lifeform run); TASKABRT code; C:\MAK sanctions above. | 12:40Z user "Ok on 2-3": TASKABRT DECIDED as the stalled-unit code; SMS placement under C:\MAK and --appDataDir relocation SANCTIONED if needed. Licence + DI-Guy still open. | - | RULE | yes |
| L7 | DEMO-READY residue | Rows 13 (runbook), 15 (operator observability), 18 (slot validator). | Open. | After L2's list - 13 and 15 overlap with reporting delivery. | STANDARD | none |

## 2. Serial spine (the only ordering that matters)
1. L4 lands (hardened runner) and L3 merges + main rebuilds (optional for G7; pinned either way).
2. G7 runs on an idle machine, foreground-polled, ~25 min: ONE variable (L1), instrumentation only
   otherwise. Harvest by an Opus executor against the prereg within the hour.
3. G7 HOLDS -> the router is finally exercised: the FINDING's confirming test (does the mesh route around the
   face?) reads directly off the same run. G7 MISSES -> G8 (gamewareMemorySize via --appDataDir), same day.
4. L2 builds land on a branch in parallel and ride the NEXT rebuild after their review.

## 3. Today's expected timeline (UTC)
11:30 all lanes running | ~13:00 L4 reviewed + committed, L3 merged, G7 prereg written (user OK on the SMS
location if needed) | ~13:30 G7 | ~14:15 harvest + verdict | afternoon: L2 builds, G8 if owed.

## 4. Dissent / risks
- L1 rests on the docs' thin contract for the query (no distance or sector bound is documented anywhere);
  the falsifier for the abstract-graph hypothesis is a G7 that still prints 0 points - then memory (G8).
- A custom SMS is the vendor's upgrade-safe mechanism, but whether a .scnx accepts an ABSOLUTE SMS path is
  unverified (build_fixture.py:605 uses $(DATA_DIR)); if not, the SMS must sit under C:\MAK\...\data and the
  user sanctions it as the fixture deploy was sanctioned.
- The runner's killer is unidentified; L4's backstop makes the consequence bounded, not the cause.
