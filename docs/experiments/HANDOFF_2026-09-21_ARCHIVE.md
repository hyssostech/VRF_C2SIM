# HANDOFF 2026-09-21 - archive

Verbatim material collapsed out of docs/HANDOFF_2026-09-14_PARALLEL_LANES.md on 2026-09-21 to hold
that file under its 200-line / 160-character caps. Nothing here is a new claim; every block below is
an exact copy of text that used to live in the entry doc, kept so that no settlement is lost in the
collapse. The live carry-over from each block stays in the entry doc, in section 1. This file has no
line cap of its own. ASCII + CRLF, per repo convention. The earlier collapse of 2026-09-14 pm is in
docs/experiments/HANDOFF_2026-09-14_ARCHIVE_pm.md and is unchanged.

## sec 1 - State at 2026-09-15 02:45Z, the full run-by-run paragraph (was HANDOFF section 1, "State")

Live carry-over kept in the entry doc: the E3F40524 bridge pin and the eleven-consumers rule; the
nav product rule (20 x 20 km, one tile, ratio >= 0.9) and MojaveCOA's retirement; the WS runaway
tripwire; and the open items STP-825 and "path LENGTH, not membership" (V6h).

**State at 2026-09-15 02:45Z:** main is at 03b955e (the V5/V8 appNo ledger). main carries: rulings
+ three review passes (0f4d09e), STP-809 (4cd84d7), the runner stop-rule fix (ff15a4e), the tools52
conversion (b4fcf58), V4b (16995b3), PauseSim (f13df1b) and route-shift (96fa396), all merged. GATE
G-A RERUN DONE (a9b8c2c): VrfBridge.dll re-pinned at main 5881b7d, hash E3F40524... (997,376 B,
2026-09-15T01:34:46Z) - ELEVEN consumers one hash, 19/19 offline suites, rulings-selftest 176/0,
routeshift-selftest 61/0; the 99B7B235/165e04c pin is SUPERSEDED. LIVE PROOFS off the merged build:
V2 (230706Z) ran the 42-task COA-STP1 chain, 41/42 dispatched, 40 timed TASKCMPLT, one ruled
TASKABRT cascade; V13 (001159Z) held all five gates (Q4 refusal, C16 fire/silent, stop rule +184s
of 900); B7 heading/speed PASSED off V2's capture. NAV THREAD CLOSED BY V7
(PREREG_V7_AO20_2026-09-15.md, 21c1430, C1 CONFIRMED): 6/6 formation-slot moves and the leader's
2.3 km V0 leg (30 points) planned on MojaveAO20, "not enough" 0 times in 846,600 trace lines -
product rule cap 20x20 km / tile / ratio >= 0.9 adopted, MojaveCOA retired for navigation
(mechanism in section 1's CLOSED list). V5 (PREREG_V5, 0e9884e): PAUSE HALF PASSES (2 TASK CLOCK
HELD lines naming REPORTS PAUSED, T1 at 1828/1800 armed sim s with the 181.5 s pause costing
nothing, PauseSim PAUSE_CONFIRMED/RESUME_CONFIRMED basis=state+clock both halves); KILL HALF NOT
RUN (the seat's Stop-Process was refused by the permission classifier, window closed with the back
end alive, STP-809's active-count path stays UNCONFIRMED LIVE - a re-run must anchor the kill on
the DISPATCH, t-29 s of PushOrder's own 30 s argument, not stage-8b t=0, and needs the user's hand
or a permission rule). V8 (023743Z, de32dac): shift went SOUTH - C2 SIDED the choice; FIXED c5f166d.
V8b (033226Z, 79d3fe4): ALL R8 MET - +250 m north, SIX OF SIX crossed, no crawl. V8z zero-offset control
(041036Z, 8d87391): BRANCH A - froze 19.7 m from P11; THE LATERAL OFFSET IS THE REMEDY, insertion inert. OPEN USER RULINGS: none. V6 RUN 03:10Z (scratch federation
20260915T030650Z): the tools JOIN but see NO back end + placeholder uuids only, ResetVrf a false green
(STP-820/823/822). CLOSED BY V6g (17:11Z run, V6_LIVE_JOIN_GATE sec 13, 79e1b12): the same three tasks with MOJAVE coordinates (PROBE_V6G_MOJAVE_Order.xml) COMPLETED - ws flat (13.4 MB/min vs 780-2,219), 51/51 off-road path jobs succeed in ~1 sim s, back end discoverable throughout, 3 TASKCMPLT with vendor pairing - so the 'memory runaway' was a DATA defect: the R5 order's waypoints are in SWEDEN and the interface dispatched an 8,769 km leg (STP-823 DONE; STP-833 refuse-out-of-terrain-routes MERGED and LIVE PASS 18:44Z in V6i - the Sweden order refused in 1 s, nothing reached the back end; STP-837 traversal-based arrival MERGED, first live exercise owed; RUNBOOK 11c/11d). STP-820 and STP-822 DONE (liveness live PASS). Open: the vendor's create rejection STP-825 (wire capture: sender byte-perfect; the runner's Stage 2h holder makes every launch a JOIN - first live run clean; MAK case + bundling-size experiment = user's decisions), STP-832 merged (new bridge pin 90272BC9), the surviving reading 'path LENGTH not membership' (one run V6h: a 40-60 km in-area leg), the WS tripwire abort MERGED f5c38d2 (exit 6 at 3 alerts since dispatch; warm-up reset built, unwired).
verdict, the Q5 kill-half re-run, and the nav second mechanism / offline 20x20 km control. Prior
01:00Z paragraph archived verbatim: docs/experiments/HANDOFF_2026-09-14_ARCHIVE_pm.md sec 8
(append).

## sec 2 - The D-series and Iron Storm cut-A narrative to 2026-09-21 (was HANDOFF section 5, item 4)

Live carry-over kept in the entry doc: next step 4 itself (DEMO-READY residue, then the STP task
vocabulary, then the aggregate-level profile); the 2026-09-21 user rulings line; the Iron Storm
cut-A status; and the STP-857 / re-clamp status, which now lives in section 1's CLOSED list and in
docs/RULINGS.md RL-20260921-03 / -06 / -07 / -09.

4. DEMO-READY residue (readiness rows 9, 13, 15, 18), then the STP TASK VOCABULARY (L8) - the user's
   stated goal beyond MOVE - and then the aggregate-level profile (Y-15). D1 STOPPED on teardown (vrfGui exit prompt, STP-844/845, fixed); D1b/D2 PASS (0.60x speed open); route shift default ON merged f26d4ad; D3 PASS; STP-825: RID levers refuted, mechanism less settled (79% SILENT refusals), demo posture = persistent holder + -n 3; A+C merged adca180; D6/D7 runner truth 7/7, C confirmed, A = 0.0 s on R9, N4 hex-ring centroid shift, N7 sim not real time; D8/1d0fb69: N4 FIXED (0.0 m, +28 s vs D7, no speed gain), P4 wording defect (sim/wall load-dependent 2.6-4.7x), join-detector fix live 6/6 vs 0/6; D5/D5b Way B: steps 1-3 HIT, then a crash (STP-854) + dead-sim-healthy (STP-853) + no order gate (STP-852); D9/4f1f149: P2 MISS - STP-855 (route-origin race, D8's 0.0 m was timing luck) REOPENS the build; D5c/4f1f149: Way B VERIFIED LIVE end to end, CLOSES rows 5/7 + half of 13; STP-852 confirmed (ordering not wall time); STP-854 open (3 confounds). USER RULINGS 2026-09-21: rtiAssistant closed; Iron Storm cut-A one diagnostic drive authorised; DEMO CLOCK = FAST; D5b callstack read (vendor use-after-destroy race, STP-854); MAK case OPEN, package in prep. D10+D5d/51d59c0: STP-855 CONFIRMED LIVE on BOTH provenance branches (Way A stressed the transient, Way B read the deleted shells) - CLOSED, rows 5/7/13 re-confirmed. Iron Storm cut-A diagnostic drive: fixture deployed and drivable, T10 crossed 0.82-0.90 connectivity for the first time (crawled at 0.28 m/s, cause open); T02/T14 (the control) measured 0 m displacement, both created at FALLBACK altitude on a cold-streamed terrain (STP-856, a create defect, NOT a shown freeze cause - VRF_ALTITUDE_FRAMES sec 5, causal claim withdrawn 2026-09-21) - BLOCKS full cut A AND its T14-only fallback either way; STP-857 (a stall TASKABRT suppressed by a timer completion) has its "USER RULING" label WITHDRAWN, but its SYMPTOM is real and the owner agrees ("a unit that ... gets stuck in the middle of the way certainly did not complete", owner 2026-09-21 session c3b364bd typed answer); what is NOT approved is the patch AS SCOPED (zero-displacement only), so branch fix/reclamp-verify-and-movement-only @ 3d58819 stays parked and UNMERGED, and how a task combining movement and a desired effect completes is UNDER THE OWNER'S REVIEW pending doctrinal + vendor research - write no settled rule either way (CORRECTIONS_LOG F-1); 0.9 bar not reopened.

## sec 3 - Navigation mesh: the N1 / N2b / N2c / N2d run narrative (was HANDOFF section 1, "DOCS PASS" bullet)

Live carry-over kept in the entry doc: propagation-box-extent is DEAD for ground vehicles and the
abstract-graph SMS stays the fix; abstract graphs are documented COST-BLIND and the documented lever
is slope-avoidance-factor; the ridge freeze is a LINE property and the remedy is the pre-flight route
shift; do not re-probe query flags.

- DOCS PASS 2026-09-14 20:00Z (NAVDOCS_ABSTRACT_GRAPHS_AND_SLOPE_2026-09-14.md): the flat A*
  query's documented propagation-box-extent is 200 m (Autodesk AStarQuery option,
  Ground_Vehicle.ope); the successful G7c-gate abstract routes deviated 231-292 m, outside it.
  Abstract graphs are documented COST-BLIND; the ridge face is legal to the mesh (slope-max 46
  deg, cost no-go unreachable at factor 1.0, soil invisible), so no planner flag routes around it
  - the documented lever is slope-avoidance-factor. 20:40Z HEADER READING (PREREG_N1_N2_CORRIDOR_SLOPE):
  propagation-box-extent is DEAD for ground vehicles (DtNavBot parameter; Ground_Vehicle.ope uses the
  dynamic-obstacle nav interface) -> N1 NOT RUN, AG SMS stays the fix. N2b RAN 21:05Z: P20a MISS -> STOP (AG query 0 points for EVERY goal of AtOrder members queried 5-10 s after the first area row; 5th freeze at the P11 point); N2c REFUSED identically (timing FALSIFIED); N2d (start +500 m): short goals plan, ALL long goals refused (regional AG), the unit drove the authored V0->V1->V2->V3 line 1.3 km north of the freeze - the freeze is a LINE property; remedy = pre-flight route shift - PREREG_N1_N2 sec 10.2/10.3.

## sec 4 - ORBAT / movement C1-C10 as the entry doc carried them (was HANDOFF section 1, "ORBAT / movement")

CANONICAL, and unchanged: docs/DESIGN_ORBAT_TO_VRF_2026-09-06.md CLOSED list C1-C10. The entry doc now
carries the live imperatives of C1-C10 in two lines and points here for the text it used to hold.

- C1 the company move failure had TWO superimposed causes: C1a structure (createSubordinates on a
  unit that ALSO has declared children -> template phantoms + orphaned platoons) and C1b movement (a
  template higher-unit created remotely scatters). C1b's MECHANISM IS CLOSED by the sim's own
  console: a move-into-formation gate whose template HQ-section M998 never reaches its slot, so the
  route move never starts. Not a race, not "unresolved formation", not a vendor bug.
- C2 THE FIX IS COMPOSE (vendor sample: empty shell + create members + addToOrganization + task the
  parent), 3/3 deterministic. C3 do not retry the template path at company+. C4 AggregateFormation
  auto stays OFF. C5 a coarse leaf EXPANDS to compose (recursive; pure higher-units only).
- C6 no post-attach reorganize and no client-side formation-validity wait (both refuted as "gaps").
  C7 leader identity does not gate movement; declared order is fidelity only. C8 address VRF objects
  by their REAL VRF_UUID from ObjectCreated, never a name-as-DtUUID (settled 2026-09-02). C9 there is
  no vendor MSDL/ORBAT importer sample. C10 sendVrfObjectCreateMsg + initialFormation: DROPPED.

## sec 5 - Navigation mesh: the G6 instrument-gate and area-dependence evidence (was HANDOFF section 1)

Live carry-over kept in the entry doc: the refusal is a property of the AREA, not of goal distance;
MojaveAO20 is the fixture and MojaveCOA is retired; UG52's 20 x 20 km is a GUI default, not a
generator limit, and the product rule caps an area at 20 x 20 km anyway.

- G6 STOPPED on its instrument gate: the 41 x 54 km area loaded, both nav-area gates passed, and vrf:findPathToLocation returned 0 points for every multi-km goal - a FOURTH reproduction of the stops, not a discriminator.
- The dependence is on the AREA, not goal distance: 10,131.6 m planned twice on the 1,600-sector MojaveAO20, 0 points on the 8,856-sector MojaveCOA for byte-identical destinations (22/22 pairs); refusals return FASTER (0.4-1.3 s) than successes (2.4-4.2 s) - a refusal, not an exhausted search; G1 ran on MojaveAO20, there never was a 3 x 3 km COA area.
- The generator accepted a 41 x 54 km area: UG52's 20 x 20 km is a GUI default, not a generator limit.

## sec 6 - Harness: the full "runner exit 127" paragraph (was HANDOFF section 1, "Harness")

Live carry-over kept in the entry doc: exit 127 here is a high-bit Windows code, not "command not
found"; a silent log with no WER event means the runner was killed from OUTSIDE; and NO Stop-Process
or taskkill sweeps while a run window is open.

Harness (docs/experiments/RUNNER_EXIT127_2026-09-14.md; RUNBOOK 0.5.14; full recap archived docs/experiments/HANDOFF_2026-09-14_ARCHIVE_pm.md sec 3):
- "runner exit: 127" does NOT mean "command not found" - on this MSYS bash it is a high-bit Windows code; the only SILENT one is 0xFFFFFFFF (TerminateProcess(-1) / Stop-Process / Process.Kill), so 127 + a silent log + no WER event = THE RUNNER WAS KILLED FROM OUTSIDE (killer unidentified, no process auditing here); the G6 capture was COMPLETE - "trace: (no samples)" was a separate tail-read defect, now fixed. NO Stop-Process / taskkill sweeps while a run window is open.

## sec 7 - The lane table in its table form (was HANDOFF section 2)

The entry doc now carries the same lane-to-Jira mapping as one compact paragraph, because the table
form cost thirteen lines of a 200-line cap and the live plan, docs/PLAN_PARALLEL_LANES_2026-09-14.md,
is canonical for it. Nothing was dropped: every lane and every Jira id below is in the entry doc.

| Lane | What it is | Jira |
|---|---|---|
| L1 | Mesh-query refusal | STP-788 epic; STP-790/791/792/793 |
| L2 | Reporting | STP-777 epic; STP-778..787 |
| L3 | Watchdog + sim clock | STP-783 |
| L4 | Runner robustness | STP-789 epic; STP-794 |
| L5/L10 | Records | - |
| L6 | User-owned | - |
| L8 | Task vocabulary beyond MOVE | epic created 2026-09-14 |
| L9 | STP-side strict parse check | STP-615 |
| L11 | 4-27 second read + offset-line scoring | - |
| L12 | A2 detached run watchdog | STP-794 |
