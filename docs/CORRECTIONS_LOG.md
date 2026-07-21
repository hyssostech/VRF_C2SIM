# CORRECTIONS LOG

Provenance of claims that were once stated and later found wrong. The live docs
(RESUME_PROMPT.md, HANDOFF, RUNBOOK, VRF_GROUNDWORK_PLAN) state only the CURRENT truth with
no retraction history; this file is where the history lives. Consult it only to answer "was
X ever believed, and why is it not believed now". ASCII only.

Each entry: the claim, why it was wrong, and the evidence that settled it.

## Movement of 1222.MechPlt

- CLAIMED (through 2026-07-19 early): "moves ~174 m of a ~1155 m route, then STOPS - a
  reproducible defect." WRONG. The unit was still moving, not decelerating, when telemetry
  ended. ~174 m is a REAL MEASURED displacement over RPT's ~124 s coverage, not a stopping
  point and not the observation-window length (that would predict ~203 m at 1.4 m/s).
  Evidence: RPT final-leg speeds 1.45/1.49/1.48 m/s across three runs, no deceleration; a
  POS sample at t=159.9 (run 161438Z) 4.6 m from the concurrent RPT fix.
- CLAIMED (2026-07-20): "still ACCELERATING when observation ended." OVER-READ. Three RPT
  fixes = two legs, and leg 1 begins at task issue so it contains spin-up from rest; two
  legs cannot distinguish acceleration from reaching cruise ~1.48 m/s. Current wording:
  "still moving, not slowing."
- CLAIMED (2026-07-20, round 6): "RPT stops reporting ~23 s before the POS trace does
  (t=157.1 vs 180.3) - a new open gap." FABRICATED. No gap: RPT's period is ~62 s, the next
  fix was due at t~219 s, and the interface resigned at ~182 s. The figure also compared one
  unit's last fix (157.1) against the global POS collapse (180.3) - different quantities.
  Deleted round 7.

## The t=180 readable collapse

- CLAIMED (briefly, round 4 cold-read): the 53->2 readable collapse at t~180 is an oracle
  fault. WRONG. It is the interface RESIGNING at teardown; the two survivors are the two
  baseline uuids present before creation. Evidence: collapse tracks VrfC2SimApp process exit;
  trace keeps sampling to t~679.
- CLAIMED: "tracks the app exit to within 0.3 s." Own numbers give deltas up to 0.5 s, and
  the app-exit column was not reproducible without a fitted per-run offset. Current evidence
  is the survivor-uuid identity, which needs no clock alignment.

## Baseline objects "positionless"

- CLAIMED (RUNBOOK 0.5.7, for days): "the TropicTortoise baseline objects are POSITIONLESS -
  that is simply how they reflect." WRONG as a statement about reflected values, and
  corrected wrongly FOUR times before the counted census stuck. Verified census:
  d39a55ad (GlblTerrDmg) 0 samples, never reflects; f864e51f (GlobalEnv) 1388 samples, forms
  NaN,-90,NaN and 0.0,-90,6.4e72, never its authored 9e-6; cde66adc (Page-In Area) 1390
  samples, four forms incl. altitudes 1.02e15 and 6.4e72. BOTH readable objects are
  cast-corrupted; neither's true position has ever been read.

## VrfFacade RTTI / the aggregate cast

- CLAIMED (RUNBOOK sec 7, since 2026-07-10): "dynamic_cast<DtReflectedAggregate*> fails due
  to RTTI across the MAK DLL boundary." FALSE. Under DtHLA the class deliberately derives from
  DtReflectedObject (reflectedExtAggregate.h:15-19), so a null cast is correct. The blind
  static_cast worked on aggregates only by accidental vtable-slot alignment, and is UB on
  control objects - the cause of the 0xC0000005 crash.
- CLAIMED (2026-07-19, briefly): "the static_cast is removed; aggregates resolve via the
  typed list; resolveStateRep." That native change was REVERTED (commit 5d14eda) because it
  broke object creation. resolveStateRep has zero hits in tracked source; the blind cast is
  STILL at VrfFacade.cpp:735.

## Tooling

- CLAIMED: StopIface acts with no arguments (it drove a live server RUNNING->UNINITIALIZED
  during a usage probe). FIXED 2026-07-19: requires <restUrl> <stompUrl> --yes, no defaults.
- CLAIMED: the runner/RESUME support -ConsoleLogDir / --console-log-dir. The flag went out
  with revert 5d14eda; WatchVrf rejects it with exit 2, killing the run. Disarmed in the
  runner, removed from docs.
- CLAIMED: RAW / BCON / CONARM trace record types and LogObjectConsoleToFile /
  SetObjectNotifyLevel exist. All went out with revert 5d14eda; WatchVrf emits POS/CON/TSK/RPT.
- CLAIMED (RUNBOOK sec 7): recover a stale federate by "reloading the scenario in the GUI."
  FALSE - recovery is automated (tools/ResetVrf, sec 8). No GUI step on any scored path.
- CLAIMED: the Session Status modal "fires on EVERY clean teardown." INTERMITTENT - named in
  zero of six stopvrf logs (the search cannot see it); four teardowns completed cleanly. The
  nested-dialog fix is UNVERIFIED (never exercised by a real occurrence).

## Run accounting

- CLAIMED variously: "four separate runs", "5 of 6 fallback", "4 of 4 fallback", "one
  teardown failed", "the four-run table" (five rows). Verified: THREE fully unattended runs
  (161438Z, 202349Z, 222134Z); TWO teardowns failed (144109Z, 193252Z); the back-end
  graceful fallback fired on ALL FIVE runs that had the feature; the bridge validation table
  has FIVE rows.

## Model-set default behaviour

- CLAIMED (2026-07-19): "RULED OUT - taskRules/ and scriptedObjectMovement/ are empty."
  WRONG layer. Empty only in C2simEx; C2simEx.sms includes EntityLevel.sms, whose taskRules/
  holds default-task-rules.tsk + doctrines.dct and whose scriptedObjectMovement/ holds 19
  files. None opened. NOT ruled out.

## Process

- The single-auditor repair loop (rounds 1-7) did not converge: like-for-like orchestrated
  audits found 26 then 29 defects, because each repair pass added correction layers that were
  themselves defect-prone (mis-scoped fences, corrections after the text they retract,
  headlines outliving bodies, one fabricated finding). The entry points were rewritten clean
  2026-07-21 to break that loop. LESSON: state the current truth in the live doc; keep
  provenance HERE; do not stack retractions in a document a fresh reader must act on.
