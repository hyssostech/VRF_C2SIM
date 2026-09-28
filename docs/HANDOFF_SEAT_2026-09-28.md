# SEAT HANDOFF - Fable seat (session 5fc25950) to the Opus 5.5 seat, 2026-09-28 (RL-20260928-04)

Read in this order, then act: this file; the CLOSED block at the top of docs/PLAN_MOVEMENT_2026-09-27.md; docs/RULINGS.md
(RL-20260928-01..-04 are today's); docs/experiments/PREREG_IRONSTORM_AGG_G1-3_2026-09-28.md Result; CLAUDE.md sec 3 and 5.
The rite (global CLAUDE.md): PREREG before any launch; falsification gate on every cause claim; the ledger and the
corrections log are the record; ASCII + CRLF (edit with perl/Edit/python with newline="", never sed -i); a suite run
(`"C:\Program Files\PowerShell\7\pwsh.exe" -NonInteractive -NoProfile -File tests/RunnerTurnaround.Tests.ps1 < /dev/null`,
770 passed / 0 failed / 1 skipped on main today) before every push; push only when green.

## 1. The seat model rule (RL-20260928-04)

The seat runs on Opus 5.5. Before any cause claim or design brief reaches the owner, call Agent(model: "fable") as a
COLD-START REVIEWER on the evidence only (the run's app log lines, the vendor citations, the brief - not this session).
Use Fable for the judgment-heavy lanes (vendor-mechanism research, a crash reading if the owner authorises it). Measure the
seat by the owner's corrections per day (2026-09-28: six - all seat mechanism claims made without the record or the docs).
THE OPS TIER (under RL-20260928-04; the follow-up question of 2026-09-28 ~19:58Z, "Is even Opus overkill for the seat?"): the seat keeps the decisions - briefs, the adversarial
reading of reports, merge order and conflicts, the owner's rulings - and hands the MECHANICAL sequences to a cheap ops agent
(Agent, model haiku or sonnet) driven by the scripted procedures that exist: merge -> suite -> push (push only on 0 failed);
the go-live A/C3/D/W/E/F command lines; the ASCII/CRLF/13c checks; the sim-cache listings; RUNBOOK sec 9 rebuild lines. One-line
task in, one-line result out, so the seat's own context stays small - the seat's cost is its context re-read, not its price.
A Sonnet seat is untested here; if the owner wants that data, take it on a docs-only day, never on a go-live day.

## 2. State at the handoff (2026-09-28 ~20:05Z)

- main: see `git log -1`; every commit of today is pushed; the tree is clean except the runner-written ledger (below).
- Deployed build: 1.0.0+git.fde3ff2.Release-5.2 in the eleven output trees (RUNBOOK sec 9, lane R4 17:30Z); bridge pin
  03226dd0 (NEW PIN 13:34Z, C1d); the road layer staged: 41 osm-highways tiles in the deployed preflight-cache (manifest
  213bae20...). The M3b lane (below) changes managed code only - a managed rebuild of the ten consumers redeploys it
  (RUNBOOK sec 9 R2/R4 procedure); tools/RtiProbe's tree is held open by the live holder and its code is unchanged.
- Live holder: RtiProbe pid 56380, appNo 5255, joined 19:00:02Z, resigns on its own ~03:00Z 2026-09-29. NEVER kill it or
  rtiexec/rtiForwarder/rtiAssistant. While it is up every launch only JOINS.
- Ledger marker (docs/OPUS_EXECUTION_PLAN.md Appendix B): `*** NEXT FREE: 5270 ***`. 5255 = the holder; 5256-5258
  burned; the G1-3 pre-warm took 5259-5269. G1-4 while 56380 is up: pre-warm 5270-5280, scored 5281-5291, marker 5292.
  After it resigns: a holder claim first (5270-5273), pre-warm 5274-5284, scored 5285-5295, marker 5296.
- M3b MERGED as 739cce2 (branch fix/planned-move-destination 5ae2193; managed only, pin 03226dd0 stands): (1) member DISPLAY names
  at most 16 characters (VrfNames.cs, ContainerComposition.cs) so the vendor script's route reference "<member> Path part N_<counter>"
  fits the 35-byte DtUUID payload (uuid.h :247-249; the 2026-09-02 cut, PREREG_ROUTE_NAME_LENGTH; the vendor's own saves name a
  script route's uuid "<name>_<counter>"); (2) a vacuous planned intermediate vertex = EXECUTOR REFUSED -> TASKABRT (VertexChain,
  AggregateMovePlanner; the last vertex still goes to D-6; M1 lone platforms unchanged); (3) a crashed back end VOIDS the window
  (RunnerLib/RunC2SimScenario: crash record by name+mtime, the modal, the process gone, WatchVrf backends 1->0; StopVrf52 exit 8;
  the watchdog reads 8). Tests: planned-move 81, populate 199, rulings 615, RunnerTurnaround 777/0/2 in the lane's worktree.
  NOT YET DEPLOYED: the deployed trees are still fde3ff2 - step 1 of sec 3 is the managed rebuild of the eleven from main 739cce2
  (RtiProbe's code unchanged; its tree is held by the holder - leave it) and the redeploy of appsettings.json.
  Unexplained, carried: the back-end crash (callstack unread; the owner's call); vertex 2's "Cound not create route" for two
  members. Next hypothesis if short names still fail in G1-4: buffer 10 (the vendor's save used 0).
- The STP session (stp-live-picture-d8, STP-850) holds a PR #1 (fix/stp-850-start-at-max) on this repo: the task-start
  anchor at receipt. Review it here with the record rules; the DEFAULT anchor (Receipt) is the owner's to confirm.

## 3. Next steps, in order

1. (DONE by the Fable seat) M3b merged as 739cce2, suite green, pushed.
2. Deploy: a managed rebuild of the ten consumers from main 739cce2 (RUNBOOK sec 9, the R4 line is the model; an ops
   agent can run it); verify by the output trees; record the sec 9 line; RtiProbe stays as is while the holder runs.
3. G1-4 registration (a lane, from PREREG_IRONSTORM_AGG_G1-3_2026-09-28.md: same order/fixture/init; the one
   variable = M3b; HIGH: 0 "route does not exist", every member moves, T14 and T10 arrive; the crash-void line as a
   falsifier; numbers per sec 2). Fable reviews the registration's cause claims cold before the go-live.
4. Go-live (the seat, under RL-20260928-01): A -> C3 -> W (dry run, pre-warm, W gate) -> E -> F, the scripts of
   u3\laneG1-3 adapted (scratchpad H:\claude\F--Repos-C2SIM-c2simVRFinterfacev2-36\5fc25950-1a10-4ade-9a7b-68cb5c1daf05\
   scratchpad\u3\laneG1-3\); ask the STP session to hold builds first (idle MSBuild workers fail the pre-launch check;
   let them time out, never kill).
5. LABEL follow-up (owner 2026-09-28 ~20:55Z, from UG52 13.2.5 / 21.2 / 21.3): our creates send label = nullString
   (VrfFacade.cpp :943, :964, :980), so the map shows only the NAME - the 30-character cut for containers, the 16-character
   display name for members. The vendor's LABEL is uncapped, not unique and never a key: put the full C2SIM designation in the
   Label on every create (a small native change - the facade hard-codes the null label; the bridge overloads take a label
   argument; NEW PIN procedure) and enable the Label symbol decoration in the fixture's display settings (21.2), so the demo
   audience sees the full designation. After G1-4's Result, not before; one lane; the fixture edit is the sanctioned deploy.
6. After that: the STP-850 PR; audit follow-ups (AUDIT_RULINGS_IN_CODE_2026-09-28 sec 3: the owner's decisions;
   the time multiplier; the D2b/M3 partials); the 8i-2 and observer test-isolation weaknesses are fixed / noted.

## 4. Decisions that are the owner's (do not decide them)

- Whether the G1-3 crash callstack (C:\MAK\logs, pid 3344, 19:05:16Z) is read (RL-20260921-02 item 4 precedent).
- The STP-850 default anchor (Receipt recommended) and the PR merge.
- The audit's open items: the stall window (240 wall / 360 sim s vs the approved 120 sim s), the platoon ring 350 m vs
  "about 300 m", RL-20260914-04 (fan-out) retire or keep, the time multiplier, RL-20260921-04's open question.
- Whether highway=track counts as a road for the AUTO planner and whether 500 m is the right proximity (observed:
  every cut-A leg is NEAR on tracks; the NONE branch is untested).

## 5. What the Fable seat got wrong today (so the next seat does not repeat it)

CORRECTIONS_LOG 2026-09-28 entries: the name limit re-derived instead of the uuid (RL-20260928-02); "roads are not a
routing option" written into the CLOSED list without the docs; "plan-then-follow along roads" asserted from a script skim;
the G1-3 binding diagnosis given to the owner before the harvest and refuted by the Result. Common cause: a mechanism
claim built on a lane's stated assumption or a file skim, not on the record, the vendor docs and the run's own evidence.
