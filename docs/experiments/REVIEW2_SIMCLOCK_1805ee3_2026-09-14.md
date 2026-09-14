# Cold-start review PASS 2 of feat/sim-clock 1805ee3 (Opus reviewer, 2026-09-14 ~10:40Z) - MERGE WITH FIXES:
# F1 flat-reading skew -> false TASKABRT on a crawler; F2 NaN admitted as a clock; F3 rollback trips the stale
# hold; F4 silent dormancy (wall regression at StallCheckSeconds > window/3); F5-F9 minor. Fix pass 3 dispatched.

# COLD-START REVIEW, PASS 2 - commit 1805ee3 (parent 1616614), branch feat/sim-clock

Opus cold-start reviewer, 2026-09-14. READ-ONLY: no edits, no build, no launch. Tier: HEAVY
(the change carries cause claims about a live simulation clock, it ships a number a preregistered
run will act on, and it touches the path that emits TASKABRT to STP).

Method. `git diff 1616614 1805ee3`; full reads of StallPolicy.cs, VrfC2SimService.cs (0c
pre-flight :307-331, MarkDispatched :2204-2246, MaybeCheckStalls :2655-2873), VrfSettings.cs
:196-269; 51d78a5's MaybeCheckStalls read in full for the wall-path comparison; the RECAL doc read
on the MAIN checkout. Every helper (Admit, JudgeReady, NextCheckSeconds, NextClockMode,
SimClockStale, ResolveWindowSeconds, Decide) and the whole MaybeCheckStalls control flow were
re-implemented in python - `scratchpad/review2_sim.py` plus the drivers `review2_cases.py`,
`review2_cases2.py`, `review2_cases3.py`, `review2_cases4.py`, `review2_cases5.py`,
`review2_cases6.py`, `review2_failfirst.py` - and driven with the sequences the brief named.
Numbers below are from those runs unless marked otherwise.

VERDICT: **MERGE WITH FIXES.**

What holds. The wall path - the shipped default - reproduces 51d78a5's decision sequence exactly.
The findings-1/2 ring fixes, the findings-7/8/9 helpers and the per-clock defaults all do what the
report says, and the 240/360 numbers match RECAL_STALL_SIMSECONDS_2026-09-13.md to the digit.
Decide is byte-identical to 51d78a5, ASCII and CRLF are clean, the trailer is there.

What does not. The SIM path - the whole point of the branch - carries three defects a
preregistered StallClock=sim run would hit: a manufactured TASKABRT (F1), an unbounded ring and a
dissolved window on a NaN reading (F2), and a rollback that silently suspends the entire watchdog
for minutes behind a wrong diagnosis (F3). F4 is a silent-dormancy regression that bites the WALL
path too. None is hard to fix; F1 and F2 are a few lines each.

---

## MAJOR

### F1. MAJOR - Admit's back-replace manufactures a TASKABRT on a crawling unit
`src/VrfC2SimApp/StallPolicy.cs:209` (`if (ring.Count > 0 && clockNow <= ring[^1].Clock) ring[^1] = (clockNow, sample);`)

MECHANISM. The back-prune replaces the newest ring entry's PAYLOAD while keeping its old CLOCK
stamp. Harmless while that entry sits at the back - but a ring of exactly one entry has its only
entry at both ends: after a replace, `ring[0].Clock` says time C while `ring[0].P` holds positions
read up to one flat-clock interval LATER. The verdict at `VrfC2SimService.cs:2840-2843` measures
`positions` (fresh) against `oldest.P` and scores the result against a window of
`clockNow - ring[0].Clock`. When ring[0] is skewed the true elapsed interval is SHORTER than the
window it is judged against - the false-positive direction, and exactly what the 1.41x calibration
margin exists to buy off.

EVIDENCE (review2_cases5.py L2, review2_cases6.py L3). A unit crawling at 0.23 m/s of SIM time -
the RECAL doc's own tightest true negative, sec 3: "the tightest true negative's per-window minimum
displacement grows at 0.21-0.23 m/s of SIM time" - dispatched while the sim-clock reading is flat:

```
   ratio |  flat-read duration (wall s) that produces a FALSE TASKABRT
    1.50x | none up to the 60 s stale threshold
    3.00x | [59, 60]
    6.21x | [40, 50, 59, 60]
    9.00x | [50, 59, 60]
   control (same crawler, clock never flat, ratio 6x): fires = 0
```

At 6.21x - G5's measured ratio - a 40-wall-second flat reading reports the unit STALLED with a max
member displacement of 15.5 m over a nominal 360 sim s window. The true displacement over 360 sim s
is 83 m, well clear of the 50 m threshold.

WHY THE STALE HOLD DOES NOT COVER IT. `StaleClockWarnSeconds = 60` suspends judging only after
60 wall s of no advance; the false verdict lands at 40-59 wall s, inside that grace. The hold is
1 to 20 wall seconds too late, at every ratio tested.

REACHABILITY. Requires StallClock=sim (unreachable on the shipped wall default - DateTime.UtcNow
always advances, so line 209 is dead there) plus a sim reading that stays flat for tens of wall
seconds while the scenario really runs. That is precisely the second of the two LIVE UNKNOWNS the
commit itself records (`VrfSettings.cs:256-261`: whether `DtBackend::simTime()` extrapolates
between status messages, "which would make a paused reading a sawtooth rather than a flat line").
So the commit ships a defect whose trigger it already flags as unsettled.

THE DOCSTRING'S JUSTIFICATION IS WRONG ON ITS OWN CODE. `StallPolicy.cs:190-192` argues the replace
"keeps the NEWEST positions, which is what the verdict has to be computed from". It is not: the
verdict's NOW end is the local `positions` dictionary (`VrfC2SimService.cs:2841`), never `ring[^1]`.
Nothing needs the newest payload in the ring except when that entry later becomes ring[0] - the
harmful case.

FIX. On a non-advancing clock, DISCARD the incoming sample instead of overwriting; keep the
stamp/payload pair that is honest. The ring bound is unchanged (nothing is added). Keep the replace
only when the rollback branch fired this call, where the stored payload really is from an abandoned
timeline:

```csharp
bool rolledBack = false;
if (ring.Count > 0 && clockNow < ring[^1].Clock) { /* pop */ startClock = clockNow; rolledBack = true; }
if (double.IsNaN(startClock)) startClock = clockNow;
if (ring.Count > 0 && clockNow <= ring[^1].Clock) { if (rolledBack) ring[^1] = (clockNow, sample); }
else ring.Add((clockNow, sample));
```

VERIFIED (review2_cases6.py L4/L5): with that change the false-stall sweep is empty at every ratio
tested (1.5x, 3x, 6.21x, 9x, every hold from 5 to 60 wall s), and the pause-after-fill bound still
holds - a 3,000 wall-second pause after the ring filled leaves max ring 50, final 49, zero
verdicts. The fail-first check at StallPolicy.cs:230-232 still passes.

### F2. MAJOR - a NaN sim reading is admitted as a clock: the ring grows without bound and the window dissolves
`src/VrfC2SimApp/VrfC2SimService.cs:2720` (`if (usingSim && simSeconds < 0.0) return;`)

`NaN < 0.0` is false, so a NaN reading passes this guard and becomes `clockNow`. Note the asymmetry
with the same tick's other predicate: `StallPolicy.UsingSimClock` (:2688) uses `simSeconds >= 0.0`,
which correctly treats NaN as "no reading". The two disagree about the same value in the same method.

Downstream, a NaN stamp is appended (in `Admit`, `clockNow < ring[^1].Clock` and
`clockNow <= ring[^1].Clock` are both false against NaN, so neither the rollback nor the back-prune
branch fires), and then `ShouldDropOldest(now, ring[0], NaN, window)` is false for both disjuncts,
so the FRONT PRUNE STOPS PERMANENTLY at the first NaN that reaches index 1.

EVIDENCE (review2_cases5.py G3 - StallClock=sim, ratio 1.5x, a MOVING unit so no verdict ends
sampling, a NaN on every 7th read, never 3 consecutively so the mode hysteresis never fires and
never clears the rings):

```
  max ring: 1196   final ring: 1196   NaNs in ring: 171   mode: 1   mode lines: 1
  ring[0] stamp: 38.25   ring[1] stamp: nan   newest: 9000.75
  -> measured window is 8962.5 sim s against a configured 360
  (wall, ringlen): (1000, 196) (2000, 396) (3000, 596) (4000, 796) (6000, 1196)
```

Each entry is a full member-position dictionary. This is finding 1 of the pass-1 review -
unbounded ring growth - re-entering through a different door, and it additionally turns the 360 s
window into "since the first NaN", silently. With the guard corrected the same run gives max ring
43 and a window of exactly 360.0 (review2_cases5.py G4).

REACHABILITY is unproven: `VrfFacade::SimTimeSeconds` (VrfFacade.cpp:600-612) returns -1.0 on a
null controller, a zero back-end count and any exception, so NaN can only come out of
`DtVrfRemoteController::simTime()` itself. I could not establish offline whether it can. The fix
costs nothing either way.

FIX. `if (usingSim && !(simSeconds >= 0.0)) return;` - the same predicate `UsingSimClock` uses.
Belt and braces: make `Admit` reject a non-finite `clockNow` outright.

### F3. MAJOR - a snapshot rollback trips the STALE-CLOCK hold, with the wrong diagnosis, for minutes
`src/VrfC2SimApp/VrfC2SimService.cs:2781` (`if (clockNow > _stallSimClockLast)`), `:2791`

`_stallSimClockLast` is a HIGH-WATER MARK ("highest sim reading seen", :2643). After
`rollbackToSnapshot` the clock is genuinely advancing but below that mark, so the advance branch is
never taken, `_stallSimClockLastAdvanceWall` is never refreshed, and 60 wall s later `SimClockStale`
returns true. The whole watchdog then stops judging EVERY unit until the clock climbs back past the
pre-rollback value - and it says the opposite of what happened.

EVIDENCE (review2_cases2.py E2, StallClock=sim, ratio 1.0x, rollback at wall 1500):

```
  rollback of  100 sim s: STALE-WARN at wall 1555, resumed at 1600 -> judging suspended  45 wall s
  rollback of  475 sim s: STALE-WARN at wall 1555, resumed at 1975 -> judging suspended 420 wall s
  rollback of 1000 sim s: STALE-WARN at wall 1555, resumed at 2500 -> judging suspended 945 wall s
```

The warning text emitted is "the scenario is PAUSED, or the back end has stopped answering" -
neither is true, and the operator has no way to tell from the log that a rollback caused it. The
inconsistency is internal: `Admit` handles a backwards step correctly and explicitly (:196-202,
citing vrfRemoteController.h:605); the stale detector does not.

FIX. Treat a backwards step as a CHANGE, not a non-advance:
`if (clockNow != _stallSimClockLast) { /* advance-or-rollback: refresh both, clear the warn flag */ }`
plus, if the rollback is worth a line of its own, log it where `Admit` re-arms.

### F4. MAJOR - silent dormancy: the watchdog can stop judging forever with nothing in the log
`src/VrfC2SimApp/StallPolicy.cs:138` (`MinRingDepth = 4`), `:242` (`NextCheckSeconds`)

Two ways in, one of them a regression against 51d78a5 in the DEFAULT wall mode.

(a) WALL MODE. `NextCheckSeconds` only ever SHORTENS the cadence, and it is only called in sim mode
(`VrfC2SimService.cs:2756`). Nothing protects the depth floor when `StallCheckSeconds` is configured
coarse. Measured (review2_cases.py A2, wall, window 240, frozen unit):

```
  StallCheckSeconds= 80  old fires at 240   new fires at 240
  StallCheckSeconds=100  old fires at 300   new fires at 300
  StallCheckSeconds=120  old fires at 240   new fires: NONE   <-- dormant, nothing logged
  StallCheckSeconds=240  old fires at 240   new fires: NONE   <-- dormant, nothing logged
```

The boundary is `StallCheckSeconds > windowSeconds / 3`, i.e. > 80 s at the shipped 240 s wall
window. 51d78a5 fired in both cases. Config-only and outside the defaults, but it is silent.

(b) SIM MODE. Above the ratio at which the 1 s cadence floor binds, the front prune leaves fewer
than MinRingDepth entries and nothing is ever judged. Measured (review2_cases4.py K, window 360):
ratio 120x fires (ring 4), ratio 200x does not (ring 3), and the only log line in the 200x run is
the one-off mode line. The MinRingDepth docstring (StallPolicy.cs:131-137) states the ceiling as
`windowSeconds / (MinRingDepth x 1 s)` = 90x and calls it "Documented, not silent". Both halves are
wrong: the real ceiling is `windowSeconds / ((MinRingDepth - 1) x 1 s)` = 120x (measured: 90x, 100x,
110x, 119x, 120x, 150x all fire; 200x does not), and nothing is printed when it binds. A comment is
not documentation to an operator reading a run log.

FIX. (i) correct the docstring to `(MinRingDepth - 1)`; (ii) at the 0c pre-flight, refuse or warn
when `StallCheckSeconds > ResolveWindowSeconds(...) / (MinRingDepth - 1)`; (iii) in sim mode, log
ONCE (rate-limited like the mode line) when the cadence has been clamped at the 1 s floor and the
ring has stayed below MinRingDepth for a full window - "the watchdog cannot judge at this sim/wall
ratio".

---

## MINOR

### F5. MINOR - three of the 21 new checks are not fail-first, and the actual finding-3 change has no test at all
`src/VrfC2SimApp/StallPolicy.cs:233, :270, :291`

The commit message asserts "The new ring/gate checks were made to FAIL first against the pre-fix
logic". I re-ran each new block against the 1616614 Admit (append + front-only prune)
(review2_failfirst.py):

| check | line | pre-fix | discriminating? |
|---|---|---|---|
| PAUSE AFTER THE RING FILLED (ring size) | 230 | FAIL (max 249 vs 49) | YES |
| "...ring still holds the whole pre-pause window" | 233 | PASS | **NO** |
| ROLLBACK: no sample from the abandoned timeline | 253 | FAIL (ring0=1240, stamps to 1480 survive) | YES |
| "...rollback re-arms the watch" | 257 | FAIL (start=5.0, not 1005) | YES |
| "...first verdict from a sample stamped AFTER it" | 270 | PASS (oldest=1480 >= 1005) | **NO** |
| FRESH DISPATCH re-arms the watch | 291 | PASS | **NO** |
| ring-depth floor shuts the gate on a 2-entry ring | 316 | FAIL (WindowReady alone judges at sample 2) | YES |
| cadence drops to the 1 s floor at 60x | 321 | FAIL (pre-fix cadence fixed at 5.0) | YES |
| first judgeable check is the 60 wall-second floor | 334 | FAIL (would be sample 5, not 60) | YES |
| ParseClockPreference x3, NextClockMode x3, ResolveWindowSeconds x4, SimClockStale | 340-396 | n/a, new API | YES |

:270 is the check that most directly encodes the cross-discontinuity harm and it passes against the
buggy code: the pre-fix ring's oldest entry at the first post-rollback verdict is stamped 1480,
which satisfies `oldestAtFirstVerdict >= 1005.0` while being a PRE-rollback sample. :253 carries the
load; :270 adds nothing. (Not a defect in the product - a defect in the claim.)

:291 is worse in kind: the block clears the ring ITSELF (`ring.Clear(); start = double.NaN;`), so it
exercises no product code. The actual finding-3 fix -
`if (dest is not null) _stallSamples.TryRemove(unit.Name, out _);` at `VrfC2SimService.cs:2243` -
has NO test, because the self-test harness cannot reach the service. I modelled it instead
(review2_cases3.py H): a frozen unit re-tasked at wall 200 fires once, for the NEW task, at 440 s;
without the drop it would have fired at 240 s stamped with the new uuid from the old task's samples.
The change is CORRECT. It is simply untested, and the commit message should say so rather than list
it under the fail-first claim.

Also verified as clean while there: MarkDispatched runs on the tick thread
(`_tickActions.Enqueue(() => ExecuteTaskOnTick(task, unit))`, VrfC2SimService.cs:1748), so the new
`_stallSamples.TryRemove` cannot race MaybeCheckStalls' `GetOrAdd` + `ring` mutation.

### F6. MINOR - `_stallReported` is not cleared alongside the samples, so a re-tasked unit that already stalled once is never judged again
`src/VrfC2SimApp/VrfC2SimService.cs:2243` against `:2814` (`if (_stallReported.ContainsKey(name)) continue;`)

`_stallReported` is documented as "unit name -> task uuid reported TASKABRT" and the C16 header
promises "One report per unit-task, ever" - but the guard tests `ContainsKey(name)` and never
compares the uuid. Measured (review2_cases3.py H, second case): a unit reported stalled for T1 at
240 s and re-tasked at 800 s produces no further report, and `_stallReported` still holds `{U: T1}`
while T2 is in flight. The bottom-of-method prune only drops units that have left the live set, and
a re-tasked unit is live. So the new asymmetry - samples dropped at MarkDispatched, flag dropped
only at ClearStallState - means that in exactly the failure mode the commit's own comment names (the
route-created callback never arrives) the new task is unwatched.

Inherited from 51d78a5, not introduced here; 1805ee3 sharpens it. FIX (one line, matches the
documented semantics):
`if (_stallReported.TryGetValue(name, out var u) && u == (rec.TaskUuid ?? "")) continue;`

### F7. MINOR - two code comments still assert the sim window is un-calibrated, contradicting this same commit
`src/VrfC2SimApp/VrfSettings.cs:204` and `src/VrfC2SimApp/VrfC2SimService.cs:2670-2674`

- VrfSettings.cs:203-204: `"wall" is the DEFAULT and the only CALIBRATED mode - see RE-CALIBRATION
  OWED below`. The "RE-CALIBRATION OWED" block was DELETED by this commit (replaced by
  "CALIBRATION - THE WINDOW BELONGS TO THE CLOCK" at :214), so the cross-reference dangles, and
  "the only CALIBRATED mode" is refuted 40 lines lower by the 360 sim s derivation.
- VrfC2SimService.cs:2670-2674: `...resolves to WALL - the CALIBRATED mode - because the 240 s
  window was derived on wall-stamped traces and has not been re-derived in sim seconds
  (VrfSettings.cs "RE-CALIBRATION OWED")`. The same commit re-derives it.

The C16 doc paragraph was rewritten correctly ("supersedes the RE-CALIBRATION OWED note; the
re-calibration is DONE"); the two code comments were missed. Refuted content sitting beside live
guidance is the specific failure the standing doc rule names, and here it would tell the next
session to redo finished work. FIX: delete both clauses; say instead that wall is the default
because it is the mode measured LIVE so far (the commit message's own, correct, reason).

### F8. MINOR - in sim mode the wall floor, not the calibrated window, sets the detection time above ratio ~6x
`src/VrfC2SimApp/StallPolicy.cs:222-228` (JudgeReady's `wallSecondsSinceDispatch >= minSecondsSinceStart`)

`StallMinSecondsSinceDispatch = 60` is now ANDed on as a WALL floor. The 360 sim s window is 58
wall s at G5's 6.21x (RECAL sec 5's own table), so above ratio ~6x the floor dominates and the
watchdog measures 60 wall seconds, not 360 sim seconds. Measured (review2_cases2.py B, frozen unit,
window 360): at 60x the first verdict lands at wall 60.5 s / sim 3,660 s - ten times the calibrated
window. The RECAL doc flags the sibling issue explicitly ("StallMinSecondsSinceDispatch was
converted to sim seconds along with the window (60 sim s) ... the choice is untested"); the shipped
code applies it on BOTH clocks with the same number, which the calibration never modelled.

Conservative in direction (it delays, never advances, a verdict) so not a correctness bug, but it
silently negates the "fires EARLIER in wall time" property the 360 s default was chosen for, at
exactly the high ratios that motivated the sim clock. FIX: at minimum say so in the 0c startup line;
better, derive the wall floor or drop it in sim mode now that MinRingDepth is in place (the floor's
stated purpose - "no task is ever judged inside its first minute of real time" - is a policy choice
nothing measures).

### F9. MINOR - `StallWindowSeconds: 0` silently changes meaning for any existing deployment config
`src/VrfC2SimApp/VrfSettings.cs:264`

Before: `window = Math.Max(1, _vrf.StallWindowSeconds)`, so 0 (or a negative) meant a ONE-SECOND
window. Now 0 means 240/360 and a negative also means 240/360 (verified: `ResolveWindowSeconds(-5,
true) == 360`). Nobody would sensibly have shipped 0, and StallDetection is OFF, so the blast radius
is nil - but the reinterpretation is not called out in the commit message and an appsettings file
carrying an explicit `0` will behave completely differently. Worth one line in the settings comment.

---

## NOTE

- **N1.** `StallPolicy.SelectClock` is now DEAD in the product - the service inlines
  `usingSim ? simSeconds : wallNow` at :2721. It survives only in the self-test (:357-408), so four
  self-test checks now assert the behaviour of a helper the product does not use. That drift is what
  produced F2: `UsingSimClock` and the inline guard disagree about NaN. Either route the product
  back through `SelectClock` or delete it.
- **N2.** The mode-line rate limit reuses `StallPolicy.StaleClockWarnSeconds`
  (VrfC2SimService.cs:2735) for an unrelated purpose. Changing the stale threshold silently changes
  the log rate limit. Give it its own constant.
- **N3.** The early return at :2720 happens before the dead-unit prune at :2867-2871, so during a
  sim-clock blackout `_stallSamples` / `_stallReported` retain entries for units that have
  completed. Bounded by unit count, not a leak, but a behaviour the comment at :2866 ("the buffer
  must not grow across a whole run") does not cover.
- **N4.** `stallClockValid && stallPrefersSim` at :327-329 is redundant - `ParseClockPreference`
  returns true only for a valid "sim" - but harmless.
- **N5.** At a high sim/wall ratio the cadence drops to 1 s, so `TryReadMemberPositions` runs 5x
  more often per in-flight move unit. Bounded and cheap at the 11-taskee COA-STP1 scale (the "COA
  is not the ORBAT" ruling), but it is a new per-tick cost no measurement covers.
- **N6.** 53 `Check(` occurrences minus the local-function declaration at StallPolicy.cs:308 = 52
  assertions, consistent with the claimed "stall 52/52". That they PASS is not verifiable offline
  (no build permitted).

---

## The brief's questions, answered

**1a. Can a verdict fire on a cross-discontinuity comparison?** YES - see F1. Not across a ROLLBACK
(Admit:196-202 closes that; verified against the 1616614 code: pre-fix ring[0]=1240 with stamps to
1480 surviving, post-fix ring[0]=1005 with no stamp after the rollback). Not across a PAUSE
(positions do not change while the sim is stopped). The live discontinuity is a FLAT-BUT-NOT-PAUSED
reading with a one-entry ring, and it fires.

**1b. Can the ring grow unbounded?** YES, via NaN - 1,196 entries and still climbing after 6,000
wall s (F2). NOT via a pause: 1,000 wall s and 3,000 wall s of a frozen clock after the ring filled
both leave it at its filled size (49-50), confirming the finding-1 fix. NOT via a rollback. NOT via
coarse status granularity: at 5/10/30 sim s granularity, ratio 1.5x, max ring 49/37/13
(review2_cases3.py C).

**1c. Can the watchdog silently never judge?** YES, three ways, none of them logged as dormancy:
`StallCheckSeconds > window/3` on the WALL clock (a regression against 51d78a5); sim/wall ratio >
`window/((MinRingDepth - 1) x 1 s)` = 120x; and the F3 rollback hold (that one IS logged, but with a
wrong cause). See F4.

**1d. Does StallClock=wall behave EXACTLY as 51d78a5?** At shipped settings, YES - I compared the
decision sequence over seven synthetic wall feeds (frozen; 2 m/s; crawl 0.20 and 0.21 m/s, either
side of the 50 m/240 s line; move-600 s-then-freeze; a +/-2 m limit cycle, the 1-35 signature;
creep-then-stop at 1200 s) and every fire time matched to the second. Structurally: the rollback and
back-replace branches are unreachable on a monotone wall clock; rule (b) of ShouldDropOldest never
fires; the added `(clockNow - startClock) >= minSince` is subsumed by `(clockNow - oldest) >= window`
whenever window >= grace. The two DIFFERENCES are deliberate (MarkDispatched drops samples on a
re-task with a destination) and accidental (F4a).

**2. Fail-first claims.** Table in F5. 18 of 21 discriminate; :233, :270 and :291 do not, and the
finding-3 product change is untested.

**3. Defaults.** `ResolveWindowSeconds(0,false)=240`, `(0,true)=360`, `(240,true)=240`,
`(999,false)=999`, `(-5,*)` = the calibrated default - all as claimed, and 250 x (240/170) = 352.9
-> 360 matches RECAL sec 5 exactly. Every number the code cites (250 pooled / P11 250 / G3 200 / G5
none; the 1.41x margin from the 160/170 boundary; the 35-70 m wall band and 35-75 m sim band; 74/78 m
true-negative minima; all five true positives earlier in wall time, G5 135 vs 385) is in the RECAL
doc verbatim. The 0c startup line (:311-331) prints the clock, the window, the displacement
threshold, the provenance ("the configured Vrf:StallWindowSeconds" / "that clock's calibrated
default"), the doc reference and the fallback rule, plus a separate warning on an invalid
StallClock. `StallDetection` is still `= false` (VrfSettings.cs:262) and both the 0c block (:315)
and the tick call (:530) are behind it; `_bridge.SimTimeSeconds()` (:2685) is the only native call
on this path and is behind BOTH StallDetection and preferSim. Nothing native is reached with the
feature off.

**4. Cadence scaling.** The ratio comes from the SAME sim clock, differenced against the previous
check (:2757). During a pause or a stale hold the difference is 0 -> `NextCheckSeconds` returns the
configured cadence (verified: ratio 0, negative, NaN, +Inf and 1e-9 all return 5.0; 1e12 returns the
1.0 floor). No division by zero - `wallNow > _stallLastCheckWall` guards the denominator. First
tick: `_stallLastCheckClock` is NaN, the block is skipped, cadence stays configured. On a mode change
`_stallLastCheckClock` is reset to NaN (:2705). The one rough edge is the first tick AFTER a stale
hold releases, where the jump/5 s ratio is enormous and the cadence drops to the 1 s floor for one
tick - harmless. The cadence never exceeds the configured value, so it can only over-sample.

**5. MarkDispatched sample drop.** No path drops samples while a verdict for the PREVIOUS task is
still owed in a way that matters: `_inFlight.RecordDispatch` has ALREADY replaced the record by line
2243, so `rec.TaskUuid` is the new task and the old one could not have been reported correctly
anyway - the drop prevents a misattributed TASKABRT rather than losing a real one. The one-report
flag semantics are unchanged (`_stallReported` is untouched by MarkDispatched), which is the
conservative direction the comment claims - but see F6 for the cost of that asymmetry.

**6. Hygiene.** `rg -nP "[^\x09\x0a\x0d\x20-\x7E]"` over all four touched files: no hits (checker
validated first on a control carrying smart quotes, an en dash, NBSP, VT and BEL - it reported 3
dirty lines). All four files are 100 % CRLF (678/678, 3293/3293, 499/499, 613/613) with no BOM.
`Co-Authored-By: Claude Opus 5 (1M context)` present (plus a Fable co-author line).
`StallPolicy.Decide` is byte-identical to 51d78a5 (diff empty), and StallPolicy.cs is purely
additive from 1616614 - the diff contains no removed lines at all. No build outputs are tracked.

## Claims I could not verify offline

- That the self-tests pass (52/52) and the solution builds. No build permitted.
- Whether `DtVrfRemoteController::simTime()` can return NaN (F2's reachability), and whether it
  extrapolates between back-end status messages (F1's reachability). Both are the commit's own
  declared LIVE UNKNOWNS; both fixes are free regardless.
- Every vendor citation (vrfRemoteController.h:355-356/:605, vrfBackendListener.h:154-155/161-163,
  vrfutil/backend.h:273-274/410-419, ifStatus.h:85-87, commandLineRemoteController.cxx:1247-1256).
  Pass 1 reports having checked the clock-choice citations and they are unchanged here.
- That the deployed VrfBridge.dll carries `SimTimeSeconds` (added natively in 1616614). If a stale
  DLL is loaded the call throws and is caught at :2686 -> -1.0 -> wall fallback, so the failure is
  graceful, but a sim-clock run against an unrebuilt bridge would silently be a wall-clock run with
  the wall window. The startup line would not reveal it; only the later mode line would.

## Adversarial review

Competing hypothesis for F1, weighed and rejected: "the skew is bounded by one back-end status
period, which is 1-5 s, so it is immaterial." Tested directly - at 5/10/30 sim s of reader
granularity the ring and the verdicts are unaffected (review2_cases3.py C), which SUPPORTS that
hypothesis for ordinary quantisation. The false stall needs a flat reading of tens of wall seconds,
which is the deactivated-back-end case the commit builds F3's hold for - so the commit asserts the
condition is real while leaving a 40-59 wall-second window in front of the guard meant to cover it.
The falsifier would be evidence that the reader can never stay flat for more than a few wall seconds
while the sim runs; that is the unresolved live unknown at VrfSettings.cs:256-261, so the finding
stands as conditional-but-unrefuted.

Competing hypothesis for F4a, weighed and rejected: "the depth floor cannot regress the wall path
because the default cadence is 5 s against a 240 s window." True at defaults, which is why the
severity is MAJOR-not-BLOCKER; but `StallCheckSeconds` is a shipped knob with no documented upper
bound and the failure it produces is total and silent.

Symptom still unexplained: none. Every verdict, ring length and log line in the offline model is
accounted for by the code as read; the model reproduces the fix report's own claimed pre-fix numbers
(249 entries after the pause; the 1616614 ring keeping stamps to 1480 across the rollback) before it
was used to test anything new.

## Recommended order

1. F2 (one predicate) and F3 (one comparison) - both trivial, both sim-mode only.
2. F1 - the Admit change plus a self-test check that FAILS first (drive a one-entry ring through a
   flat clock with moving positions; assert no verdict). Do not run a StallClock=sim experiment
   before this lands: it can manufacture a TASKABRT on a crawler.
3. F4 - the docstring correction, the pre-flight guard on StallCheckSeconds, and the dormancy log.
4. F6, F7 - one line each.
5. F5, F8, F9 and the notes - record them; F7 and F8 belong in the C16 doc the same turn.
