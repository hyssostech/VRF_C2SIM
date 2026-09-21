# PREREG (DRAFT for the seat to register) - STP-825 NOTIFY-2: instance-level replication of -NotifyLevel 3 vs 0

REGISTERED 2026-09-20 by the seat, BEFORE any create of this experiment; design by the Opus research lane after the independent adjudication of the pilot (PREREG_STP825_NOTIFY_2026-09-20.md RESULT); ANALYSED ALONE - no pooling with any earlier run; the user authorised rtiexec restarts "as needed" on 2026-09-20 (this design uses seventeen: sixteen blocks + the final return to -NotifyLevel 3).

Written 2026-09-20 by an Opus RESEARCH executor. NOTHING WAS RUN that joins a federation; no
rtiexec / rtiForwarder / RtiProbe was started, stopped or queried in any way that could disturb it
(rtiexec 51560, rtiForwarder 71500, holder RtiProbe 12916 are alive on purpose and were only listed).
Docs first (sec 2). Tier HEAVY. Register BEFORE the first restart. ASCII only.

**THIS EXPERIMENT IS SELF-CONTAINED AND IS ANALYSED ALONE.** The 60 creates of
PREREG_STP825_NOTIFY_2026-09-20 are a PILOT for it and contribute NOTHING to its test. No pooling
with that run, with the A/B/C run, or with the census, in the primary or the secondary analysis. Any
pooled figure that appears anywhere in the write-up is a description, never a test.

Entry records read in full: `scratchpad\validation\stp825_notify_adjudication.md`,
`scratchpad\validation\stp825_abc_adjudication.md`. Harness: `stp825_notify2_orchestrator.ps1`
(written, parse-checked, self-tested, NOT run).

## AMENDMENT A1 (2026-09-21 00:30Z, registered BEFORE any create of this experiment)

The first launch (2026-09-21 00:13:12Z, orchestrator pid 90556) ABORTED at the quiescence gate before block B01: 600 s timeout, CPU 11%, "interfering" = seven Docker Desktop / docker-agent processes. NO create was issued, no block ran, no CSV row exists, no appNumber was consumed (4802-4993 stay claimed for this experiment).
Defect: sec 7.5's interfering-process pattern includes "docker", but Docker Desktop is a CONSTANT background on this host (it hosts c2sim-server-vrf and c2sim_server4.8.4.9; it ran during every earlier STP-825 run and every demo rehearsal), so it cannot differ between conditions and can never be quiet - the gate was unsatisfiable.
AMENDMENT: "docker" is REMOVED from the interfering-process pattern, which drives the quiescence gate, the per-create "interfering" column and the dirty-instance count alike (one function, verified in the orchestrator). Keeping it would mark all sixteen instances dirty in both arms - it could not fire R-L1 (8 vs 8) but would leave R-L2's clean-instance sensitivity analysis with zero instances. As a constant it is instead recorded ONCE, in the session conditions file the seat captures at launch (docker ps + the Docker process list: scratch validation\stp825_notify2_conditions.txt), and total CPU % is still sampled before and after every create, so any Docker-driven load still reaches the analysis through the CPU columns and R-L1's CPU-gap test. Nothing else in the design changes: same frozen order (seed 825), same N, same tests, same alpha, same verdict function.
Side effect of the abort, recorded: the orchestrator had already stopped the seed rtiexec 51560 / rtiForwarder 71500 before the gate timed out, so the host had NO rtiexec between 00:13Z and the amended launch; the seat's persistent holder 12916 had been stopped by the seat beforehand as the procedure requires.

## AMENDMENT A2 (2026-09-21, registered BEFORE any create of this experiment)

Second launch (2026-09-21 00:27:14Z, orchestrator pid 75068) ABORTED seconds into block B01 with "The property 'Count' cannot be found on this object": under Set-StrictMode -Version Latest a function returning an EMPTY array is unrolled to $null; with Docker out of the pattern (A1) the interfering-process list was empty for the first time and `$bad.Count` threw. NO create was issued, no CSV row exists, no appNumber was consumed (4802-4993 stay claimed).
The orchestrator's -SelfTest had been green because it covered only pure functions; the live control path had never executed.
INSTRUMENT FIXES (no change to what is measured or how it is scored): one array convention throughout (producers return plainly; every call site wraps the CALL in @( )), nine sites changed after a full audit of the class, seven regression checks added to -SelfTest (51/51 green). A NEW -DryLive mode runs the ENTIRE session control flow (16 blocks in the frozen order, gate, stop/verify/start bookkeeping, per-create loop, load sampling, log-delta reads, MC1/MC2, block-end assertions, CSV, scoring) with only process stop/start and the probe launch faked, on separate appNumbers (20001-20192) and separate output files. Eight scripted scenarios each ended in the verdict they were built to produce: hit -> HIT; all-clean and sparse -> INDETERMINATE-BY-DESIGN; both-arms -> NULL-NO-EFFECT; underpowered -> INCONCLUSIVE-UNDERPOWERED; mc1-fail -> VOID-MANIPULATION-UNVERIFIED (aborts in B02); already-up -> VOID-INCOMPLETE; oob-duration -> VOID-INSTRUMENT. The dry-live run itself caught TWO further defects before any live use: the author's first fix combined two mutually exclusive array conventions (an EMPTY result then reported Count 1 and the gate could never pass), and the fake failure generator silently produced zero failures.
PROCEDURE CHANGE (prereg 7.1 order stop -> verify -> gate -> start becomes GATE -> stop -> verify -> start, for EVERY block): the quiescence gate now runs BEFORE the previous rtiexec is stopped, so a gate timeout aborts having touched nothing and the host keeps its rtiexec (A1 recorded the opposite side effect). This changes nothing about what is measured, the order of conditions, N, the tests, alpha or the verdict function.
OPERATIONAL NOTE (not an amendment to the pattern): dotnet / MSBuild / VBCSCompiler / vrfNavGenerator / vrfSim / vrfGui / VrfC2SimApp / WatchVrf / node remain in the interfering pattern because, unlike Docker, they are variable load that could differ between arms; the seat runs `dotnet build-server shutdown`, confirms zero matching processes and launches only when no build or harvest lane is live.

---------------------------------------------------------------------------------------------------
## 1. WHAT THE PILOT ESTABLISHED, AND THE FIVE DEFECTS THIS DESIGN FIXES

Pilot result (registered verdict **INCONCLUSIVE-UNDERPOWERED**): control 5/30, treatment 0/30,
Fisher one-sided p = 0.0261 against a registered alpha of 0.005. The manipulation demonstrably
happened (0 arrival lines and 0-byte logs in 30 treatment creates against 32 arrivals and
~110 KB/create in 30 control creates). The direction is the predicted one; the bar was not met.

The five defects, each with its fix here:

| # | defect (adjudication sec) | fix in this prereg |
|---|---|---|
| D1 | The verdict branches were NOT an exhaustive partition: `fN0<=1 AND fN3>=5 AND p>=alpha` was unallocated, and the harness fell through to MISS prose that did not apply (1, 1.1) | sec 8: an ORDERED TOTAL FUNCTION over the whole outcome space, with a written partition proof and a self-test that enumerates the grid (sec 8.3) |
| D2 | The replicate is the rtiexec INSTANCE, not the create. Level-3 rates across six instances are 0/45, 0/27, 3/18, 33/97, 3/15, 2/15; homogeneity chi-square 31.63 on 5 df. A 2-v-2 design has a minimum attainable exact p of 1/C(4,2) = 0.167 and could never have reached 0.005 (4.1) | sec 7: 16 instances, 8 per arm; PRIMARY test is an exact permutation test over the 16 instance-level counts, minimum attainable p = 1/C(16,8) = 7.77e-05; plus an assignment-free detectability check (sec 8.1 P1) that catches this class of defect before it can recur |
| D3 | 3 of 5 control refusals had NO libxml2 diagnostic at all, after 36/36 prior refusals had one (3.3) | sec 9: refusals are classified TAIL-ONLY / CASCADE / SILENT and the content reading is tested ONLY on the first two; the SILENT fraction is a reported headline because it bounds the earlier census |
| D4 | Harness bug: `foreach ($n in @($AppNos))` overwrote `$N` (PowerShell names are case-insensitive); the banner read "4733 creates" and every block died on an index error, so block-end MC2, late MC1 and teardown never ran (2.5) | sec 10: distinct loop variable names, `Set-StrictMode -Version Latest`, and an explicit end-of-block assertion that the row count equals the block size AND that every registered check executed, with a non-zero exit otherwise |
| D5 | B2's restart output was never captured and level-0 logs are 0 bytes, so the level is not verifiable from any artefact; load was never measured; the two treatment blocks were adjacent in time (2.3, 4.2, 4.3) | sec 7.4 (the running process's own `Win32_Process.CommandLine` captured per block - a stronger artefact than the start script's echo), sec 7.5 (per-create load, a pre-block quiescence gate, and a pre-registered balance gate), sec 7.2 (pair-blocked randomised order, max run length 2) |

---------------------------------------------------------------------------------------------------
## 2. DOCS - what is settled, and the one silence that still governs the reading

Sources: the installed MAK RTI 5.0.1 PDFs (`C:\MAK\makRti5.0.1\doc`, extracted to
`scratchpad\validation\rtidocs\`). docs.mak.com/support was walked earlier: 5.0.1 is the newest
public RTI doc set, so there is no later release note to consult and no vendor fix to cite.

1. **Scale and direction (unchanged, re-quoted so this prereg stands alone).** RM Table 13 p.221,
   `RTI_notifyLevel`: "Specifies the category of notification messages to print. 0 - Fatal.
   1 - Warn. 2 - Notify. 3 - Verbose. 4 - Debug. Default: 1." UG Table 2 p.46, `(--notifyLevel | -n)`:
   "The least verbose message level is 0, the highest is 4. The default is 2." So `-n 0` is the quiet
   end.
2. **The log file is a separate switch.** UG Table 2 `(--setLogFileName | -l)`; RM Table 13
   `RTI_rtiExecLogFileName`: "Enables rtiexec diagnostic logging to a file with the specified name.
   Default: Value is unset, logging is disabled." `StartRtiExec52.ps1` always passes `-l`, which is
   why a level-0 log **exists and is 0 bytes** rather than being absent - the pilot verified exactly
   that on both treatment instances. Keeping `-l` identical in both arms is what makes sec 7.4's
   file-existence check meaningful.
3. **THE SILENCE THAT STILL GOVERNS.** Every description of the level concerns what is **printed**.
   Nothing in the Reference Manual or the Users Guide says whether a message above the level is
   formatted into a buffer and then discarded, or never formatted at all. Searched again under
   RM 4.3 (p.52), Table 13 (p.221-223), UG 4.2 Table 2 and RM 7.3. **Consequence: `-n 0` removes
   formatted CONTENT and log I/O together, so even a clean HIT does not by itself say which of the
   two matters** - that is what sec 9's separate probe is for, and sec 8.4 says so in advance.
4. **No runtime level change; the instance must be restarted.** RM 5.6 Table 7 is the complete
   runtime command set of the `rti` tool - `delete`, `list`, `kill`, `shutdownall` - and has no
   set-notify command. VERIFIED OPERATIONALLY by the seat: `rti list` answers **"RTI commands are not
   available when the RTI Assistant is disabled"**, so the whole `rti` tool, including its documented
   graceful `kill`, is unavailable under our `RTI_ASSISTANT_DISABLE=1` posture.
5. **Therefore the documented shutdown path we CAN use is the vendor's own third bullet.** UG 4.2.3
   "Shutting Down the rtiexec" lists, verbatim: Federations View (Assistant - unavailable), the
   toolbar button (unavailable), "**Kill the rtiexec process.**", the `rti` tool's `kill`
   (unavailable per 2.4), and Shut Down All ("not recommended"). Stopping the process by pid IS a
   vendor-documented shutdown procedure; that is the sentence to cite if a permission layer balks.
6. **Orphaned federates and federation lifetime.** RM 6.8.1: "When the rtiexec detects that this TCP
   connection has been lost, the federate is forcibly removed from its federation, if it is joined."
   RM 6.8.2: reconnection requires `RTI_reconnectEnabled`, which our rid sets to 0. Federation state
   lives in the rtiexec process, so every restart destroys any federation it held. **This is why the
   seat must stop its own holder before the session and re-arm one after it** (sec 10 steps 1 and 9).
7. **`RTI_detachNotifyLevelFromStdOut`** (RM Table 13): "Detaches all notification messages at the
   given level and above from the standard output. This setting has no affect on logging
   notifications to a file. Default: 5." Held in reserve; per adjudication 5.5 it is NOT run in this
   session and its trigger has not fired.
8. **No documented maximum federation-name length** was found in either guide (searched "federation
   execution name", "maximum ... characters", "legal characters"). The sec 9 probe therefore pilots
   its longest name before spending the block.

---------------------------------------------------------------------------------------------------
## 3. HYPOTHESES

- **H-LOGSILENCE (the one this tests).** Silencing rtiexec's own diagnostic output removes the FDD
  create refusals. *Predicts:* treatment instances refuse at a rate near 0 while control instances
  refuse at the machine's usual level-3 rate. *Falsified by:* control and treatment instance counts
  being exchangeable (permutation p >= alpha) with the power guard and detectability check passed.
- **H-INSTANCE-HETEROGENEITY (the rival the pilot could not exclude).** Level-3 refusal rates vary
  enormously between rtiexec instances for reasons unrelated to the notify level (0/45 and 0/27 are
  in the record); the pilot's 0/30 was an ordinary clean instance. *Predicts:* control instances in
  THIS run will themselves be heterogeneous (some 0/12, some 3/12) and the arms will overlap.
  *Separated from H-LOGSILENCE by:* the permutation test over 8+8 instances, which is exactly the
  test that treats instance-level variation as the noise it is.
- **H-CONTENT vs H-TIMING (not separated here, by design).** `-n 0` removes formatted content AND
  log I/O. Sec 9's probe attacks this separately and at no restart cost.
- **Already falsified / not re-tested:** H-BUNDLE-BIG, H-COALESCE, sender-side corruption,
  long-lived-rtiexec state, clustering as a property of the defect, sender identity, "fresh instances
  are safer for their first 15 creates" (adjudication 4.5, dismissed on evidence), and "the leak is
  log buffers" (adjudication 3.4: 9.36 MB/create in BOTH arms, Welch t = 0.000).

---------------------------------------------------------------------------------------------------
## 4. INSTRUMENT

Scoring is from RtiProbe stdout, unchanged and twice validated: 62/62 against the A/B/C run's
rtiexec-log verdicts, and 60/60 in the pilot with **zero** stdout-vs-exit-code disagreements and
zero CSV-vs-recount mismatches. Rule: `Failed to process FOM file (\S+) when creating federation`
= FAIL; `\[OK\] RTI serviceable on attempt` = OK; anything else, or a disagreement with the exit
code (0 <-> OK, non-zero <-> FAIL), = OTHER. OTHER rows are excluded from the 2x2 and listed
individually; an instance with any OTHER row is flagged (sec 8.1 G3).

**What the instrument cannot do at `-n 0`, restated:** there is no create verdict, no
`Creating federation ... with handle N`, no module list and no RID dump. The only positive evidence
that a treatment create actually distributed 58 modules and exercised the FOM Reader is its wall
duration. The pilot measured level-0 creates at 12.87 s mean (11-14 s), indistinguishable from
level-3 successes (12.60 s) and far above level-3 failures (6.20 s) or an already-exists
short-circuit. **Pre-registered here as a per-create check:** any create whose wall duration falls
outside **9.0-18.0 s** is flagged `DURATION-OUT-OF-BAND` and counted at sec 8.1 G3.

---------------------------------------------------------------------------------------------------
## 5. DESIGN SUMMARY

    conditions : N3 = StartRtiExec52.ps1 -NotifyLevel 3 (control)
                 N0 = StartRtiExec52.ps1 -NotifyLevel 0 (treatment)
    replicate  : the rtiexec INSTANCE. 16 fresh instances, 8 per condition.
    creates    : 12 per instance, 5 s spacing, federation STP825AB, RtiProbe <appNo> STP825AB 1 2 3,
                 maxAttempts=1, the SHARED UNMODIFIED config\rid-501-rtiexec-min.mtl in every block.
    total      : 192 creates, 192 appNumbers, 16 restarts.
    primary    : exact permutation test over the 16 instance-level failure counts, alpha 0.005,
                 one-sided (control worse). All C(16,8) = 12,870 assignments enumerated.
    secondary  : create-level Fisher exact 2x2, same alpha, REPORTED not decisive.
    no interim looks. The p value is computed once, after all 16 blocks.

### 5.1 Why 12 x 16 and not 15 x 4 or 30 x 2
Minimum attainable exact p with k instances per arm is 1/C(2k,k): k=2 -> 0.167 (the pilot, which
could never have reached alpha), k=4 -> 0.0143, k=5 -> 0.0040, k=8 -> **7.77e-05**. Eight per arm
clears alpha 0.005 with two orders of magnitude of margin *at the instance level*, which is the level
the adjudication proved is the real unit. At the create level, 96 per arm reaches Fisher p = 1.5e-04
at a control rate of 0.167 and 3.2e-04 even if the control lands one failure below expectation - the
exact mode that sank the pilot. Restarts cost ~10 s (measured from the pilot's B3/B4 restart records
inside the ~35 s inter-block gap), so 16 restarts cost ~3 minutes of the session; there is no longer
any reason to use long blocks.

### 5.2 Wall time and appNumbers
Per block: ~10 s stop + ~15 s start + 12 creates x ~13 s + 11 x 5 s gaps ~ 3 min 45 s, plus the
quiescence gate. **16 blocks ~ 60-70 minutes.** Ledger marker verified 2026-09-20 at
`*** NEXT FREE: 4791 ***` (docs\OPUS_EXECUTION_PLAN.md Appendix B). Claim **4791-4982** for the
replication and advance the marker to **4983** BEFORE the first create - as claimed by the seat
immediately before the run (recorded in OPUS_EXECUTION_PLAN.md) - if the marker has moved, the
blocks take the next 192 numbers in order, 12 each. The sec 9 probe claims a
further 60 (**4983-5042**, marker -> 5043) when it runs - the same "claimed immediately before the
run, if the marker has moved take the next block in order" rule applies to it too. Never touch the
9101-9199 demo block.

---------------------------------------------------------------------------------------------------
## 6. THE FROZEN BLOCK ORDER (generated now, before any data exist)

**Pair-blocked randomisation**: the 16 blocks form 8 consecutive pairs; each pair contains exactly
one N3 and one N0, and the order WITHIN each pair is randomised. Generator, recorded so it is
reproducible: `python random.Random(825)`, `for pair in range(8): a=['N3','N0']; r.shuffle(a);
order += a`. **Seed 825** (the ticket number, chosen before any data existed and recorded here).

    ORDER (frozen): N3,N0,N3,N0,N0,N3,N0,N3,N0,N3,N0,N3,N0,N3,N3,N0
    sha256(order string) = 644ed1fea71ac85e...   8 x N3, 8 x N0, 14 runs, MAX RUN LENGTH 2

| block | cond | appNos | | block | cond | appNos |
|---|---|---|---|---|---|---|
| B01 | N3 | 4791-4802 | | B09 | N0 | 4887-4898 |
| B02 | N0 | 4803-4814 | | B10 | N3 | 4899-4910 |
| B03 | N3 | 4815-4826 | | B11 | N0 | 4911-4922 |
| B04 | N0 | 4827-4838 | | B12 | N3 | 4923-4934 |
| B05 | N0 | 4839-4850 | | B13 | N0 | 4935-4946 |
| B06 | N3 | 4851-4862 | | B14 | N3 | 4947-4958 |
| B07 | N0 | 4863-4874 | | B15 | N3 | 4959-4970 |
| B08 | N3 | 4875-4886 | | B16 | N0 | 4971-4982 |

**Why pair-blocked randomisation and not the adjudicator's strict alternation, and not free
randomisation.** Strict alternation is perfectly balanced in time but makes condition a deterministic
function of block parity, so any period-2 artefact of the harness itself (for example, a resource the
previous block leaves behind) would be perfectly confounded with the treatment, and the order is
guessable in advance. Free randomisation removes both problems but can produce runs of four or five
of the same condition, reintroducing exactly the adjacency confound the adjudication raised (pilot
4.2: the two treatment blocks were adjacent for ~10 minutes). Pair-blocking gives the strengths of
both: **exact balance in every consecutive pair** (so any transient shorter than two blocks hits both
arms), **maximum run length 2** (verified above), and a non-systematic order. The permutation test is
valid under any fixed assignment, so this choice buys robustness, not validity.
The order is hard-coded in the orchestrator's default and the script asserts it matches this literal;
`-Order` exists only so a registered amendment could change it deliberately.

---------------------------------------------------------------------------------------------------
## 7. PROCEDURE PER BLOCK, AND THE FOUR THINGS THE PILOT DID NOT RECORD

### 7.1 Block sequence (the orchestrator does all of it unattended)
Stop the current experiment rtiexec and rtiForwarder BY PID with a name assertion -> verify none of
either name remains and TCP 4001 and 5002 are free -> quiescence gate (7.5) -> start
`scripts\StartRtiExec52.ps1 -NotifyLevel <n>`, capturing its FULL stdout+stderr to
`stp825_notify2_runs\<stamp>_<block>_start.out` -> assert the output contains `started=yes` and does
NOT contain `ALREADY UP` -> read the new rtiexec and rtiForwarder pids -> capture the manipulation
artefact (7.4) -> 12 creates, 5 s apart, load sampled per create -> block-end assertions (7.6).

### 7.2 Order
Frozen in sec 6. Adjacent same-condition blocks are limited to 2 by construction.

### 7.3 Held constant
The shared rid, unmodified, for the rtiexec and for every federate; ports 4001/4001; forwarder port
5002; federation STP825AB; RtiProbe binary and arguments; 5 s spacing; 12 creates; the same
orchestrator code path for every block. **The ONLY thing that differs between conditions is the
integer passed to `-NotifyLevel`.**

### 7.4 The manipulation, made verifiable from an artefact (fixes D5)
Three independent records per block, all archived:
1. **The running process's own command line**, read with
   `(Get-CimInstance Win32_Process -Filter "ProcessId=<pid>").CommandLine` immediately after the
   start. VERIFIED available on this host (readable for a same-user process). The orchestrator
   asserts the captured string contains `-n 0` for an N0 block and `-n 3` for an N3 block, and stores
   the whole line in the CSV and in the per-block start file. **This is strictly stronger than the
   start script's echo: it is what the OS says the process was actually launched with.**
2. **The full `StartRtiExec52.ps1` output** to a per-block file (the pilot's B2 had none).
3. **The log-file check**: the log named for the new pid must EXIST, and must be 0 bytes at block end
   for N0 (MC2) and > 500 KB for N3 (positive control); `DtFedExec: Fed File arrived at FedEx` must
   occur 0 times across an N0 block (MC1) and at least once per create across an N3 block.
MC1 is also checked after the FIRST create of each N0 block, so a failed manipulation costs one
appNumber, not twelve.

### 7.5 Load: measured, gated, and given a rule BEFORE the analysis (fixes D5)
**Measured per create** and written to the CSV: total machine CPU % (`Win32_PerfFormattedData_
PerfOS_Processor` `_Total`, VERIFIED readable), the rtiexec's own CPU-time delta, and the names+pids
of any process matching `dotnet|MSBuild|VBCSCompiler|vrfNavGenerator|vrfSim|vrfGui|VrfC2SimApp|
WatchVrf|docker|node`. Sampled immediately BEFORE each create and again immediately after.

**Prospective gate (preferred to any retrospective correction).** Before each block the orchestrator
waits until no interfering process is present and total CPU < 25 % for **20 consecutive seconds**,
polling in-process (one script, no repeated `pwsh` command lines - memory: Defender flags polling
loops). Timeout **10 minutes**: on timeout the orchestrator ABORTS THE WHOLE SESSION rather than run
an unbalanced block, leaving the current rtiexec up. Equalising load prospectively is sound;
dropping creates afterwards on a covariate is a garden of forking paths and is not permitted.

**How load enters the analysis - fixed now, three rules and no others.**
- **R-L1 (balance gate, part of the partition at sec 8.1 G4).** After the run, compare the arms on
  (a) mean per-create total CPU % and (b) the number of instances containing at least one interfering
  process. If the arm means differ by **>= 15 percentage points**, or if one arm has interfering
  processes in **>= 3 instances while the other has 0**, the verdict is **CONFOUNDED-BY-LOAD** and
  nothing else is read from the run.
- **R-L2 (sensitivity, secondary, reported not decisive).** Repeat the permutation test using only
  instances with zero interfering processes. Report both p values. A disagreement between them is
  reported as a caveat; it does not change the registered verdict.
- **R-L3 (no exclusions).** No create and no instance is dropped from the primary analysis for any
  load reason. R-L1 can void the run; it can never trim it.

### 7.6 Block-end assertions (fixes D4)
`rows.Count -eq 12`; every per-create row has a non-empty result; MC1/MC2 or the positive control
evaluated and recorded; the command-line assertion recorded; the load samples present for all 12
rows; and an explicit `ChecksRan` flag written to the CSV for each block. A block that fails any of
these exits non-zero, the session stops, and the block is marked INVALID (sec 8.1 G3).

---------------------------------------------------------------------------------------------------
## 8. THE VERDICT - AN EXHAUSTIVE, MUTUALLY EXCLUSIVE PARTITION (fixes D1)

### 8.1 The decision function, as an ORDERED TOTAL FUNCTION
Inputs, all computed once after all 16 blocks: the 16 instance failure counts `c[1..16]` (each
0..12); the arm labels from sec 6; `fC` = sum of control counts (0..96); `fT` = sum of treatment
counts (0..96); `p_lo` = one-sided permutation p for "control worse"; `p_hi` = one-sided permutation
p for "treatment worse"; `p_min` = the smallest `p_lo` attainable from this multiset of 16 counts
under ANY assignment; the gate booleans G1..G4. Alpha = 0.005.

    VERDICT(inputs):
      G1  if any N0 block's captured command line lacks '-n 0', or any N3 block's lacks '-n 3',
          or any N0 log is missing / non-zero-length, or any N0 block has an arrival line,
          or any N3 block has zero arrival lines            -> VOID-MANIPULATION-UNVERIFIED
      G2  elif fewer than 16 blocks completed, or any block's ChecksRan flag is false,
          or any block has rows.Count != 12                 -> VOID-INCOMPLETE
      G3  elif any row is OTHER or DURATION-OUT-OF-BAND     -> VOID-INSTRUMENT
      G4  elif R-L1 fires                                   -> CONFOUNDED-BY-LOAD
      P1  elif p_min >= alpha                               -> INDETERMINATE-BY-DESIGN
      P2  elif fC < 10                                      -> INCONCLUSIVE-UNDERPOWERED
      P3  elif p_lo <  alpha and fT <= 4                    -> HIT
      P4  elif p_lo <  alpha and fT >= 5                    -> PARTIAL
      P5  elif p_hi <  alpha                                -> WORSE
      P6  else                                              -> NULL-NO-EFFECT

### 8.2 Partition proof
The function is an ordered if/elif chain ending in an unconditional `else`, over inputs that are all
total (every gate is a boolean over recorded fields; `fC`, `fT`, `p_lo`, `p_hi`, `p_min` are defined
for every possible dataset because the permutation distribution is always computable from 16 finite
counts). Therefore: **(a) at least one branch always fires** - the final `else` has no condition;
**(b) at most one branch fires** - `elif` semantics; hence exactly one. The outcome space is
partitioned with no gap and no overlap. The specific gap that broke the pilot - `fT <= 1` and
`fC >= 5` and `p >= alpha` - now lands in P6 (or P1/P2 if the data are degenerate), never in a
default it was not written for. The orchestrator's `-SelfTest` enumerates a dense grid of count
vectors and asserts one and only one label per cell, that the label is drawn from the ten names
above, and that no input yields `$null`.

### 8.3 Why P1 exists, and why it is not peeking
`p_min` depends only on the MULTISET of the 16 counts, not on which arm each belongs to, so computing
it reveals nothing about the treatment effect. It answers "could these data have reached the bar
under any assignment at all?" - the generalisation of the adjudication's finding that a 2-v-2 design
has a floor of 0.167. If, say, 15 instances score 0 and one scores 1, no assignment reaches 0.005 and
the honest label is INDETERMINATE-BY-DESIGN, not NULL. Checking it first prevents the pilot's failure
mode from recurring in a new dress.

**A consequence to expect, not to mistake for a bug** (found while smoke-testing the scorer): if the
16 instance counts come out **very homogeneous** - the extreme case being all 16 identical - then
`p_min` = 1 and P1 fires, so a "null-looking" result is labelled INDETERMINATE-BY-DESIGN rather than
NULL-NO-EFFECT. That is the correct epistemic call: one cannot conclude "no effect" from data that
could not have shown an effect under any assignment. With realistically heterogeneous counts the
floor is far below alpha and P6 is reached normally - verified on synthetic data: counts
`3,2,0,3,1,2,4,0,2,1,3,4,0,2,1,3` give `p_min` = 0.00047, `p_lo` = 0.865 -> NULL-NO-EFFECT. Both
behaviours are exercised in the orchestrator's `-SelfTest` and its scorer smoke tests.

### 8.4 What each verdict is allowed to be written as
- **HIT**: "silencing rtiexec's diagnostic output removes the refusals at the instance level,
  p = <p_lo>". It must be accompanied, in the same paragraph, by sec 2.3: `-n 0` removes formatted
  CONTENT and log I/O together, so a HIT does not yet say which matters - sec 9 is the separator.
  The holder posture STAYS regardless until a real `LaunchVrf52` run with the SIM creating passes at
  level 0; that is the user's call, not this experiment's.
- **PARTIAL**: a real but incomplete reduction. Do not adopt level 0 as a remedy; report the numbers.
- **WORSE**: record it; a quieter rtiexec refusing MORE is itself evidence about the buffer.
- **NULL-NO-EFFECT**: "the design had the power and the realised data could have reached the bar; they
  did not." This is the branch that supports H-INSTANCE-HETEROGENEITY. It is NOT licence to repeat
  the pilot's category error: do not quote the old sec 8.2 "no change is equally consistent with the
  strings still being formatted" unless the treatment arm actually showed no change.
- **INCONCLUSIVE-UNDERPOWERED** (P2): **ONE** permitted extension, fixed now - two further blocks per
  arm (2 x N3 and 2 x N0, 12 creates each, 48 creates, 48 appNumbers), appended in a pair-blocked
  order generated from seed 825 continuing the same stream, analysed pooled with the original 16 at
  the same alpha, ONCE. No second extension under any circumstance.
- **VOID-\*, CONFOUNDED-BY-LOAD, INDETERMINATE-BY-DESIGN**: nothing is read from the run about
  H-LOGSILENCE. Fix the cause and re-register.

### 8.5 Hard stop conditions during the session (none of them kills anything unowned)
An rtiexec the orchestrator started exits mid-block; `StartRtiExec52.ps1` reports `ALREADY UP`; a
refusal whose stdout names no module; three consecutive probe launch failures; the quiescence gate
times out; a name assertion fails when stopping a pid. On any of these the orchestrator stops, leaves
the CURRENT rtiexec running (except an abort before block 1, which leaves NONE - observed
2026-09-21), prints full state, and exits non-zero. The session is then VOID-INCOMPLETE.

---------------------------------------------------------------------------------------------------
## 9. SEPARATE PROBE (own hypothesis, own criteria, no restart): FEDERATION-NAME LENGTH AT -n 3

This is **not** part of the sec 5 experiment and is analysed separately. It attacks adjudication 4.6:
`-n 0` removes content and I/O together, and this probe holds I/O essentially constant while changing
CONTENT length.

**Hypotheses.**
- **H-CONTENT**: the corrupting bytes are the *text* rtiexec formatted, lying in or beside the FDD
  buffer. Precedent: A/B/C create #61 contained `<attrSTP825ABibute` - the federation name spliced
  into the XML - alongside `CurBlockSize: :` and `blockSequenceNumber:` fragments.
- **H-TIMING**: log I/O widens a race; the identity of the text is irrelevant.

**Manipulation.** Three federation names differing only in length, all legal HLA identifiers
(A-Z, 0-9, hyphen; no documented maximum exists - sec 2.8 - so the longest is piloted first):

    SHORT  "ZQ7"                                                (3 chars)
    MID    "STP825AB"                                           (8 chars, the anchor used to date)
    LONG   "ZQ7-STP825-NAMELENGTHPROBE-AAAAAAAAAAAAAAAAAAAAA"   (48 chars)

**Design.** ONE rtiexec at `-n 3` (no restart; it may be the instance left standing after sec 5),
60 creates in a fixed rotation SHORT, MID, LONG, SHORT, ... = 20 per length, 5 s spacing, 60
appNumbers (4983-5042). PILOT: the first LONG create must produce a normal 58-block create in the
rtiexec log; if the name is rejected or truncated, drop LONG to 24 chars and restart the probe.

**The discriminating observable is the CONTENT of the refusals, not the rate.** For every refusal the
probe records the shape (sec 9.1), the echoed offending line, the caret column, and - the key binary -
**whether the echoed text contains the federation name literal**.

| observation | H-CONTENT predicts | H-TIMING predicts |
|---|---|---|
| echoed text containing the federation name | occurs, and MORE OFTEN for LONG than SHORT (a 48-char token occupies 16x more of the log stream than a 3-char one, so it is far likelier to be the bytes adjacent to the buffer) | never, or at a rate independent of length |
| shape mix (tail-only / cascade / silent) | shifts with name length: longer log records change which record abuts the buffer and therefore where the corruption lands | flat across lengths |
| where in the XML the foreign text lands (tail vs interior) | longer records push more corruption into the interior, so the CASCADE fraction rises with length | flat |
| refusal RATE | little or no change (the name adds ~2.4 KB to a ~110 KB per-create log, about 2 %) | little or no change (same 2 %) |

Note the rate is deliberately NOT the discriminator - both readings predict it is flat, which is
precisely why the pilot-style rate comparison cannot settle this and the content record can.

**9.1 Refusal shape classification (also used in sec 5's N3 blocks; fixes D3).** For each refusal,
from the rtiexec log window for that create:
- **TAIL-ONLY**: exactly one `Extra content at the end of the document` and the reported line equals
  the module's own line count + 1 (or the no-trailing-newline last-line shape), plus the two
  `FED file format is unrecognized` lines.
- **CASCADE**: two or more `parser error` lines, or an `Extra content` line whose number is not the
  module's end (the create-#61 shape).
- **SILENT**: `Could not find Document Root` with **zero** `parser error` lines anywhere in the
  window - the shape that appeared in 3 of 5 pilot control refusals after 36/36 prior refusals had a
  diagnostic.
**Pre-registered: the content reading is tested ONLY on TAIL-ONLY and CASCADE refusals.** SILENT
refusals count in the rate and are reported as a fraction, and that fraction is a headline result in
its own right because it bounds how much of the "32/32 log text in the FDD buffer" census is a census
of the subset that happened to print.

**9.2 Criteria (alpha 0.05 throughout - this is a descriptive probe, not a remedy test).**
- **CONTENT-SUPPORTED**: at least one TAIL-ONLY or CASCADE refusal at LONG whose echoed text contains
  the 48-character name AND zero such occurrences at SHORT; or a shape-mix difference across the
  three lengths at chi-square p < 0.05.
- **CONTENT-DISFAVOURED**: >= 6 classifiable refusals, no name-bearing echo at any length, shape mix
  flat (p >= 0.05), and rate flat (p >= 0.05). Write it as "content-aliasing is disfavoured", NEVER
  as "the timing reading is confirmed" - this probe cannot confirm H-TIMING, only fail to support
  H-CONTENT.
- **INCONCLUSIVE**: fewer than 6 TAIL-ONLY+CASCADE refusals in the 60 creates.
**Order (adjudication 5.5, adopted): run this probe FIRST if the seat wants the cheapest information
- it needs no restart and no repo change - or immediately after sec 5. Do not run it inside the sec 5
session: it would add conditions to a design whose replicate is the instance.**

---------------------------------------------------------------------------------------------------
## 10. SEAT PROCEDURE

Sequencing (seat): this session takes the machine for ~70 minutes with NO federation holder and no
demo launch possible; it runs AFTER the demo-path runs D6 and the first Iron Storm cut-A run, not
before. The seat stops its own persistent holder first (the orchestrator refuses to start while any
RtiProbe is alive) and re-arms one on the final rtiexec afterwards with freshly ledgered numbers.

1. **Register** this prereg (fill in sec 12), claim 4791-4982, advance the ledger marker to 4983 -
   as claimed by the seat immediately before the run (recorded in OPUS_EXECUTION_PLAN.md); if the
   marker has moved, the blocks take the next 192 numbers in order, 12 each.
2. **Stop the seat's own holder** (RtiProbe 12916). The orchestrator REFUSES to start while any
   RtiProbe is alive, by design: a joined federate would keep MAK-ONE-2025 alive across a restart
   attempt, and RM 6.8.1 means the first restart destroys it anyway. This is the seat's own process
   and the seat's own call.
3. **Confirm no demo is imminent.** From step 4 until step 9 the machine has no holder and
   MAK-ONE-2025 does not exist. The session is ~60-70 minutes plus the quiescence gates.
4. **Quiesce the machine**: stop or pause other worktree lanes (build lanes, scenario prep /
   `vrfNavGenerator`). The orchestrator will wait for quiet and will abort the session rather than
   run an unbalanced block.
5. **Launch detached** (the run exceeds a tool call's limit), modelled on `stp825_abc_launch.ps1`:
   `& '<sp>\validation\stp825_notify2_launch.ps1'` - which starts the orchestrator hidden with
   stdout/stderr to `stp825_notify2_run.out` / `.err` and returns the child pid. Or run it in the
   foreground with `-WhatIfBlocks` first to see the plan without starting anything.
6. **Do not touch the machine while it runs.** Every interfering process is recorded and can void the
   run through R-L1.
7. **Score**: `& '<sp>\validation\stp825_notify2_orchestrator.ps1' -Score` - reads the CSV only,
   starts nothing, prints the 16 instance counts, the permutation distribution summary, `p_lo`,
   `p_hi`, `p_min`, the create-level Fisher secondary, the R-L2 sensitivity, and the single sec 8.1
   verdict.
8. **The orchestrator leaves ONE rtiexec running at `-NotifyLevel 3`** (block 16 is N0 in the frozen
   order, so the script performs a final restart to N3 after the last block - the non-HIT branch of
   the previous prereg's sec 10 binds until this one is scored, and level 3 is the diagnostic-capable
   default).
9. **Re-arm the holder yourself** - the script prints the exact command and does NOT run it, because
   the seat owns the ledger:
   `RtiProbe.exe <fresh appNo> MAK-ONE-2025 1 900 3` in `C:\MAK\vrforces5.2d\bin64` with the 5.2
   profile env, or simply `LaunchVrf52.ps1` with its default `-FederationHoldSecs 900`. Verify
   "has joined federation" in the new rtiexec's log before declaring the machine demo-ready.
10. **Record** the verdict in RUNBOOK 9c and STP-825 the same turn, whichever way it falls.

---------------------------------------------------------------------------------------------------
## 11. WHAT THE MACHINE IS LEFT IN

Unchanged from the previous prereg's non-HIT branch, and it binds until this experiment is scored:
**`-NotifyLevel 3`, the shared rid at vendor defaults, nothing changed in the repo, the holder
armed.** The adjudication's sec 5.6 reasoning is adopted: at level 0 there is no create verdict, no
handle, no module list and no RID dump, so RUNBOOK 9c's "count-grep the rtiexec log" step stops
working - changing the posture on an under-powered result, hours before a demo, is the wrong
direction of risk. On a HIT here, the change becomes discussable and would carry: the
`StartRtiExec52.ps1` default, a blunt RUNBOOK 9c warning that the log is now empty and that the
replacement instrument is RtiProbe/back-end stdout (validated 62/62 and 60/60), and the holder
staying anyway until a sim-creating run passes at level 0.

---------------------------------------------------------------------------------------------------
## 12. PRIOR, AND VERIFIED vs ASSUMED

**P(HIT) = 0.30.** Down from the 0.35 I gave the pilot, and the reason is the adjudication's sec 4.1,
not the pilot's direction: two level-3 instances on this machine produced 0/45 and 0/27 with nothing
manipulated, and the six-instance homogeneity chi-square is 31.63 on 5 df. A 30-create zero streak at
level 3 is already in the record twice, so the pilot's 0/30 is not the surprise it looks like. Against
that: the direction was right, the manipulation was verified, and the 2026-09-13 log's
character-interleaved RID dump (`notifyLevelnotifyLevel: : 33`) is direct evidence that rtiexec's log
sink is written by more than one thread without serialisation, which is the family of mechanisms this
tests. P(NULL-NO-EFFECT) ~ 0.40; P(PARTIAL) ~ 0.10; P(a VOID / CONFOUNDED / INDETERMINATE outcome) ~
0.15; P(WORSE) ~ 0.05. Worth running either way: it is the first design on this problem whose
replicate matches the unit the variation actually lives at, and NULL-NO-EFFECT would be a real result
that redirects the whole investigation to instance heterogeneity.

**VERIFIED (this session, primary sources):** every doc quote in sec 2, including the absence of a
set-notify command in RM 5.6 Table 7 and the absence of any documented federation-name length limit;
`Win32_Process.CommandLine` readable for a same-user process on this host; the `_Total`
`PercentProcessorTime` counter readable; the ledger marker `*** NEXT FREE: 4791 ***`; the frozen
order, its composition (8/8), its 14 runs and maximum run length 2, and its sha256 prefix; the
minimum attainable exact p values 1/C(2k,k); the create-level power table; that rtiexec 51560,
rtiForwarder 71500 and RtiProbe 12916 are alive (listed only, never queried in a disturbing way).
All pilot and A/B/C figures quoted here are VERIFIED **in the adjudications**, recomputed there from
raw artefacts; this prereg cites them, it does not re-derive them.

**REASONED (inference, stated as such):** that pair-blocked randomisation dominates both strict
alternation and free randomisation for this threat model; that `p_min` is a legitimate
assignment-free pre-check; that a 48-character federation name occupies enough of the log stream to
make a name-bearing echo detectably more likely than a 3-character one; that the 9.0-18.0 s duration
band is a sufficient proxy for "the treatment create really parsed the FDD" (from the pilot's
11-14 s level-0 band against 5-7 s for failures).

**ASSUMED / NOT PROVEN:** that 16 restarts in ~70 minutes do not themselves destabilise the RTI
(memory: teardown-relaunch wedges rtiForwarder - the orchestrator's port and name assertions are the
detector, and a wedge shows up as a start failure, which aborts the session rather than corrupting
it); that a 48-character federation name is accepted (piloted in sec 9); that the quiescence gate can
actually be satisfied on this machine within 10 minutes per block; that the instance-level counts are
exchangeable under the null, which is what the permutation test assumes and what sec 6's design is
built to make true.

**UNEXPLAINED, and they are falsifiers not footnotes:** (i) three of five pilot control refusals
produced NO libxml2 diagnostic at all after 36/36 prior refusals did - until that is explained, the
"the corrupting content is rtiexec's own log text" reading describes a subset of refusals of unknown
size, and sec 9.1 is the first attempt to size it; (ii) which create is hit and which module, still
unexplained, with a byte-identical sender stream every time; (iii) the ~9.4 MB retained per create,
identical at both levels, still unaccounted for.
