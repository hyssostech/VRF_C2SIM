# PREREG (DRAFT for the seat to register) - STP-825: does silencing rtiexec's own logging stop the FDD corruption?

REGISTERED 2026-09-20 by the seat, BEFORE any create of the experiment; design by the Opus research lane; the user authorised rtiexec restarts "as needed" on 2026-09-20 (this design uses four).

Written 2026-09-20 by an Opus RESEARCH executor. NOTHING WAS RUN: no federate, no create, no
rtiexec/rtiForwarder/RtiProbe touch, no repo edit. Docs read and cited first (sec 2). Tier HEAVY
(cause claim). Register BEFORE the first restart. ASCII only.

Entry record: `scratchpad\validation\stp825_abc_adjudication.md` (read in full),
`docs\experiments\PREREG_STP825_BUNDLING_2026-09-20.md`, `docs\RUNBOOK.md` sec 9c,
`scratchpad\wirecap\wirecap_report.md`.
Evidence produced for THIS prereg (all read-only, all in scratchpad validation/):
`stp825_notify_instrument.py` (the instrument agreement table and the ordinal-trend test - sec 5, 6),
`rtidocs\RTIReferenceManual.txt` / `rtidocs\RTIUsersGuide.txt` (pdftotext of the installed 5.0.1 PDFs).
Harness: `scratchpad\validation\stp825_notify_harness.ps1` (written, parse-checked, NOT run).

---------------------------------------------------------------------------------------------------
## 1. WHERE WE ARE, AND THE ONE QUESTION THIS ASKS

Settled by the A/B/C adjudication and NOT re-opened here: both RID bundling remedies are REFUTED
(A 4/20, B 9/20, C 4/20); the bytes past the end of the rejected module are **rtiexec's own log text
in printable ASCII** in 32/32 tail-only rejections (`DtFedExec: Fed File arrived at FedEx.` and
`[=================================================`); create #61 shows those same log strings
written *inside* the XML with libxml2's semantic errors naming the corrupted tokens; the earlier
NUL/length-prefix inference is FALSIFIED; the census clustering does NOT reproduce under fixed
spacing (z = -0.118, p = 0.91), so outcomes are plain Bernoulli trials; rtiexec retains ~7.5-9.9 MB
per create and is at ~957 MB private after 97 creates.

Surviving reading: **H-FDD-UNTERMINATED** - rtiexec assembles each module into a buffer it does not
terminate at the accumulated `CurBlockSize`, and that buffer aliases (or abuts) the memory rtiexec
formats its log lines into.

**THE QUESTION.** If the corrupting content is rtiexec's log output, then removing rtiexec's log
output should remove the corruption. One manipulation: **the rtiexec's own notification level.**

---------------------------------------------------------------------------------------------------
## 2. DOCS - what the notify level actually controls, and what the docs do NOT say

Installed 5.0.1 set, `C:\MAK\makRti5.0.1\doc\*.pdf`, extracted to `scratchpad\validation\rtidocs\`.
docs.mak.com/support was walked previously: 5.0.1 is the newest public RTI doc set, so these are the
primary and only sources.

1. **Direction of the scale - settled. 0 is the QUIETEST.**
   RM Table 13 (A.4 LRC-Specific Parameters, p.221), `RTI_notifyLevel`, verbatim:
   "Specifies the category of notification messages to print. **0 - Fatal. 1 - Warn. 2 - Notify.
   3 - Verbose. 4 - Debug.** Default: 1."
   UG Table 2 (rtiexec command-line options, p.46), `(--notifyLevel | -n) notification_level`:
   "Specifies the notification level for application messages, where notification_level is a number
   from 0 through 4. **The least verbose message level is 0, the highest is 4.** The default is 2."
   So `-n 0` = Fatal only. The adjudicator's direction is right and `-NotifyLevel 0` is the quiet end.
2. **A documented one-flag equivalent exists.** UG Table 2: `(--setNotifyQuiet | -q)` -
   "Suppresses output of diagnostic messages (**sets the notification level to 0**)." `-q` and
   `-n 0` are documented as the same thing for rtiexec. `StartRtiExec52.ps1` passes `-n`, so use
   `-NotifyLevel 0`; do not add `-q` as well (one flag, one variable).
3. **The log FILE is a separate switch from the level.** RM Table 13: `RTI_rtiExecLogFileName` -
   "Enables rtiexec diagnostic logging to a file with the specified name. **Default: Value is unset,
   logging is disabled.**" UG Table 2: `(--setLogFileName | -l) filename` - "Specifies the log file
   name for the rtiexec." So there are TWO sinks and two independent switches. We keep `-l` in both
   conditions (identical), and vary only `-n`. Keeping `-l` is what makes the manipulation check in
   sec 7.4 possible at all.
4. **A third, rid-only knob, held in reserve.** RM Table 13, `RTI_detachNotifyLevelFromStdOut`:
   "Detaches all notification messages at the given level and above from the standard output. **This
   setting has no affect on logging notifications to a file.** Default: 5 (no notification levels
   detached)." Our rid sets 5, i.e. nothing detached: at level 3 every message goes to BOTH the
   hidden console and the file. Setting it to 0 would silence the console sink while keeping the
   file - the designated MISS-branch follow-up (sec 8.4).
5. **THE SILENCE THAT MATTERS, AND IT IS THE CENTRAL RISK.** Every description above is about what
   is **printed**. **Nothing in the Reference Manual or the Users Guide says whether a message above
   the notification level is formatted into a buffer and then discarded, or never formatted at all.**
   I looked for it under Notify Level (RM 4.3 p.52), Table 13 (p.221-223), UG 4.2 Table 2, and RM 7.3
   "Enabling LRC Diagnostic Data Logging"; it is not stated anywhere. **Consequence, pre-registered:
   a null result does not refute H-FDD-UNTERMINATED** - see sec 8.2. VERIFIED as a documentation gap,
   not inferred.
6. **Changing the level WITHOUT a restart: no documented path.** RM 5.6 Table 7 lists the entire
   runtime command set of the `rti` command-line tool - `delete`, `list`, `kill`, `shutdownall`.
   There is no set-notify-level command. The RTI Assistant could change settings, but our posture
   sets `RTI_ASSISTANT_DISABLE=1` and assistants are version-locked (memory: RTI assistant version
   gate). **Therefore an ABAB inside one rtiexec instance is NOT available, and each condition needs
   its own process.** That is why sec 7 spends four restarts instead of one.
7. **Documented shutdown procedures for the rtiexec** - UG 4.2.3 "Shutting Down the rtiexec",
   verbatim list: (a) Federations View > rtiexec > Shut Down rtiexec ("This is the preferred
   method"); (b) the Shut Down rtiexec button; (c) "**Kill the rtiexec process.**"; (d) "Use the RTI
   Command Line Tool to send a `kill` command"; (e) Shut Down All on the RTI Assistant menu ("This
   may have unintended consequences. It is not recommended").
   (a), (b) and (e) need the RTI Assistant - unavailable to us. (d) is the documented headless
   graceful path: RM 5.6 Table 7, `kill <componentHandle>` - "Resigns local federates and shuts down
   local rtiexecs and RTI Forwarders", with the handle from `rti list`. NOTE the caveat in the same
   table: "The componentHandle is the handle **the RTI Assistant uses** to designate the different
   local components connected to it" - so `rti list`/`rti kill` may return nothing with the assistant
   disabled. **(c) - killing the process - is a vendor-documented shutdown procedure in its own
   right**, which is the sentence to quote if a permission layer balks. Procedure in sec 9 tries (d)
   first and falls back to (c).
8. **What happens to a joined federate when the rtiexec goes away.** RM 6.8.1: "To detect broken TCP
   connections, an LRC must establish a TCP connection to the rtiexec's RTI Forwarder
   (RTI_tcpForwarderAddr). **When the rtiexec detects that this TCP connection has been lost, the
   federate is forcibly removed from its federation, if it is joined.**" RM 6.8.2: reconnection
   happens only if `RTI_reconnectEnabled` is 1 - **our rid sets it to 0** (line 366), so an orphaned
   federate does not come back. Federation state lives in the rtiexec process: a new rtiexec knows
   nothing of MAK-ONE-2025 (VERIFIED: rtiexec 75168's federation handles start at 1).
   **So the holder does NOT have to be stopped first for correctness** - the restart destroys the
   federation it was holding either way, and the experiment's creates go to a different federation on
   a different process. What it does mean: after the first restart the demo federation is GONE, the
   holder process is holding nothing, and a fresh holder must be armed at the end (sec 9 step 8).
9. **Forwarder logging.** RM Table 13 `RTI_rtiForwarderLogFileName` enables forwarder logging and is
   **commented out in our rid** (line 228), so rtiForwarder writes no log at all and has no notify
   level of its own in play. The forwarder is started BY the rtiexec "using the same connection
   configuration that the rtiexec uses" (RM 5.3), so it inherits the rid but not a `-n` we never
   pass to it. **The corrupting strings (`DtFedExec: ...`) are rtiexec's, not the forwarder's**
   (adjudication 1.5), so the forwarder is not the target of this manipulation.

---------------------------------------------------------------------------------------------------
## 3. THE CURRENT CONFIGURATION (read from OUR log's own RID dump - the process was NOT queried)

`scripts\StartRtiExec52.ps1` (read in full): exposes `-NotifyLevel` validated `0..4`, **default 3**,
and passes it as `-n <level>`. Full command line it builds:
`-M -R "<rid>" -P 4001 -T 4001 -A 127.255.255.255 -N 127.0.0.1 -i 127.0.0.1 -D 5002 -r -l "<log>" -n <level>`.
`-K` is deliberately NOT passed. The script is **ENSURE-UP**: if an rtiexec from `-RtiDir\bin` is
running AND TCP 4001 is listening it prints "ALREADY UP", starts nothing and exits 0.
**TRAP, flagged loudly: if the old rtiexec is not actually gone, the script will report READY and the
notify level will NOT have changed.** Sec 9 gates on this.

rtiexec **75168** (started 2026-09-15 11:50:29 local), from the head of
`runs\launch52\rtiexec_20260915T155028Z5.0.1-20260915-115029-Legatus-281993-75168.log`:
`notifyLevel: 3` (the rid says `RTI_notifyLevel 2`, so the command-line `-n 3` overrode it - which
also proves the command line wins), `detachNotifyLevelFromStdOut: 5`, `tcpPort: 4001`,
`udpPort: 4001`, `destAddrString: 127.255.255.255`, `tcpNetworkInterfaceAddr: 127.0.0.1` (rid says
`0.0.0.0`, so `-i` overrode it too), `distributedForwarderPort: 5002`,
`rtiExecLogFileName: ...\runs\launch52\rtiexec_20260915T155028Z.log`.
All four of our archived rtiexec logs show `notifyLevel: 3` - **there is no natural level-0 or
level-1 comparison anywhere in the record**, which is why this has to be run.

Log volume at level 3 (adjudication sec 4): **103 KB of log per create** (success ~121.6 KB, failure
~57-58 KB). `Fed File arrived at FedEx` is emitted **once per create** - and twice when it is the
text libxml2 echoes. These are the numbers the manipulation check in sec 7.4 tests against.

---------------------------------------------------------------------------------------------------
## 4. HYPOTHESES AND WHAT EACH PREDICTS

- **H-FDD-UNTERMINATED (the surviving reading).** The FDD buffer is not terminated at its accumulated
  length and the adjacent memory carries rtiexec's log formatting.
  *Predicts:* at `-n 0`, with the log emissions gone, the refusal rate collapses toward 0.
  *Falsified by:* the rate at `-n 0` being indistinguishable from the rate at `-n 3` **provided the
  manipulation check in sec 7.4 shows the emissions really stopped AND one is willing to assume the
  suppressed messages are not still being formatted** - see sec 8.2 for why that second clause makes
  a null result weak.
- **H-FDD-UNTERMINATED-FORMAT-ONLY (the sub-case that makes a null ambiguous).** The level gates
  *printing*, not *formatting*; the strings are still rendered into the same memory and discarded.
  *Predicts:* no change at `-n 0`, with log volume nonetheless collapsing.
  *Separated from the above only by* sec 8.4's follow-up, or by MAK.
- **H-ADJACENT-OTHER.** The FDD buffer is unterminated but what follows it is some other rtiexec
  allocation; the log text is what we happen to see because logging is the busiest allocator at
  level 3. *Predicts:* at `-n 0` the rate persists and, if a refusal still occurs, the extra content
  is something other than the two known strings - but we cannot see it at `-n 0` (sec 8.3).
- **H-RACE (general).** A concurrency defect independent of the log content. *Predicts:* no change.
- **Already falsified, not re-tested:** H-BUNDLE-BIG, H-COALESCE (adjudication 1.1/3.2); sender-side
  corruption (wire capture); long-lived rtiexec state (PREREG_V6F 4.5); clustering as a property of
  the defect (adjudication 3.1); sender identity.

---------------------------------------------------------------------------------------------------
## 5. INSTRUMENT PRE-VALIDATION - DONE, at zero cost, BEFORE relying on it

At `-n 0` the rtiexec log carries no create verdict, so scoring must come from RtiProbe's own
stdout. That instrument was validated here against all 62 rows of the A/B/C run, whose reference
verdicts came from the rtiexec log (`stp825_notify_instrument.py`).

Candidate rule: `FAIL` if stdout matches `Failed to process FOM file (\S+) when creating federation`;
`OK` if stdout matches `\[OK\] RTI serviceable on attempt`; anything else `AMBIGUOUS`.

    === AGREEMENT: rtiexec-log verdict (ref) vs RtiProbe-stdout verdict (candidate) ===
      ref \ cand   FAIL   OK    total
      FAIL         18     0     18
      OK            0    44     44
      agreement: 62/62 = 1.0000        AMBIGUOUS: 0        mismatches: 0
      rejected-module name agreement:  18/18
      crashes (exit -1073741819): 12; stdout still names the module in 12/12
      exit codes: 0 x44, 1 x6, -1073741819 x12

**The instrument is exact on 62/62**, it recovers the rejected module name on 18/18, and - the point
that matters most - **it survives the 0xC0000005 crash shape: all 12 crashed probes had already
written the module name to stdout before dying.** The exit code is a perfect secondary check on this
sample (0 = OK, anything else = FAIL, 62/62), and the harness records both and flags any
disagreement between them as `OTHER`.

**What the instrument CANNOT do, stated now:** zero of the 124 stdout/stderr files contain a libxml2
`Entity: line N` echo. The corrupting text is only ever visible in the rtiexec log. **At `-n 0` we
lose the ability to see WHAT the extra content is** - we keep only whether a refusal happened. That
is the price of the manipulation and it is why sec 8.3 pre-registers what a refusal at `-n 0` may
and may not be read as.

---------------------------------------------------------------------------------------------------
## 6. IS MEMORY A THIRD VARIABLE? Tested here - NO EVIDENCE FOR IT

A fresh rtiexec resets ~957 MB of retained memory as well as the notify level, so "fresh instance"
and "less memory" travel together. If memory pressure drove the defect, the failure rate should rise
with the number of creates an instance has served. rtiexec 75168 served federation handles 1..97 in
one continuous life (35 census creates + 62 A/B/C creates), so the trend is directly measurable:

| handles | failures | rate |
|---|---|---|
| 1-24 | 9/24 | 0.375 |
| 25-48 | 10/24 | 0.417 |
| 49-72 | 7/24 | 0.292 |
| 73-97 | 7/25 | 0.280 |

point-biserial r(ordinal, failure) = **-0.132**, t(95) = -1.30 - **not significant, and the sign is
NEGATIVE**: the rate drifted slightly DOWN as retained memory grew from ~125 MB to ~957 MB. So
memory growth is not a plausible driver, and resetting it is unlikely to be the operative change.
This does not eliminate "fresh instance" as a variable - it *bounds* it, and sec 7's design removes
it entirely by giving **both** conditions fresh instances.

---------------------------------------------------------------------------------------------------
## 7. THE DESIGN

### 7.1 One variable, and the confound removed
The adjudicator's proposal (one restart at `-n 0`, 20 creates, compare against history) leaves
"fresh rtiexec" as a second variable. With restarts now authorised as needed, that confound is
removable outright: **give every block a fresh rtiexec, and vary only `-n`.** Everything else is held
identical - the same `StartRtiExec52.ps1` invocation, the same shared rid
`config\rid-501-rtiexec-min.mtl` unmodified for the rtiexec and for every federate, ports 4001/4001,
forwarder port 5002, the same scratch federation, the same probe binary, the same 5 s spacing.

### 7.2 ABBA, four blocks, four fresh rtiexecs

| block | rtiexec started with | creates | why here |
|---|---|---|---|
| **B1** | `-NotifyLevel 3` (control) | 15 | opens with the status quo |
| **B2** | `-NotifyLevel 0` (treatment) | 15 | |
| **B3** | `-NotifyLevel 0` (treatment) | 15 | |
| **B4** | `-NotifyLevel 3` (control) | 15 | closes with the status quo |

**Why ABBA and not ABAB or AB.** (i) It balances order: neither condition is systematically first or
last, so a monotone drift over the ~50 minutes cannot masquerade as a treatment effect. (ii) The two
control blocks BRACKET the treatment, so B1 vs B4 is a built-in drift check that costs nothing: if
B1 and B4 differ materially from each other the whole block is suspect and the run is INCONCLUSIVE
regardless of B2/B3 (sec 8.5). (iii) Splitting each condition across two separate rtiexec processes
means a result cannot be an accident of one unlucky instance. ABAB would balance order equally well
but would not bracket; a plain AB does neither. Cost is the same four restarts.

### 7.3 Trial unit, spacing, scoring
Identical to the validated A/B/C harness: one `RtiProbe.exe <appNo> STP825AB 1 2 3` per ledgered
appNumber, `maxAttempts=1`, fixed 5 s spacing, `Start`/`WaitForExit` with stdout and stderr captured
per number, no nested shell, no polling child processes. `RTI_RID_FILE` points at the **shared,
unmodified** rid in every block (no rid copies in this experiment). Scoring by the sec 5 rule from
stdout, with the exit code as a cross-check.

### 7.4 MANIPULATION CHECK - mandatory, and it gates the treatment blocks
A treatment block is only admissible if the manipulation actually happened. Both checks are read from
the block's own rtiexec log (`-l` is passed in every block, so the file exists at level 0 too):
- **MC1, after the FIRST create of each `-n 0` block:** the count of `DtFedExec: Fed File arrived at
  FedEx` in that create's log window must be **0** (baseline at level 3: exactly 1 per create, 2 on a
  tail-only failure).
- **MC2, at block end:** log bytes per create must be **< 10 KB** (baseline 103 KB/create).
If MC1 fails, the harness aborts that block immediately having burned ONE appNumber and exits 3:
`-n 0` does not suppress the emission, the manipulation never happened, and the whole experiment is
void - report that and stop. If MC2 fails while MC1 passed, record the block and flag it.
For the `-n 3` control blocks the same two numbers are recorded as a positive control: the count must
be >= 1 per create and the volume ~100 KB per create. If the control blocks do NOT show that, the
instrument is broken, not the hypothesis.

### 7.5 N, power, and appNumbers
Clustering is refuted, so these are plain Bernoulli trials and the A/B/C prereg's doubling of N is
not needed. Base rate: 17/60 = 0.283 pooled in the A/B/C run, 5/22 = 0.227 for its control arm;
the design is sized on the pessimistic 0.227.

| per condition | control fails (expected) | treatment 0 | Fisher one-sided p |
|---|---|---|---|
| 20 (2 x 10) | ~5 at 0.227 / ~6 at 0.283 | 0 | 0.0236 / 0.0101 |
| **30 (2 x 15)** | **~7 at 0.227 / ~8 at 0.283** | **0** | **0.0053 / 0.0023** |

Single-arm, ignoring the control: 0 failures in 30 against p0 = 0.227 gives p = 0.00044; against
0.283, p = 0.00005.
**Chosen: 15 creates per block, 60 creates total, 30 per condition**, pre-registered alpha **0.005**,
Fisher exact one-sided (control worse than treatment) on the pooled 2x2. 20 per condition was
rejected as under-powered at the pessimistic base rate.

**appNumber cost: 60.** Ledger marker `*** NEXT FREE: 4719 ***` (docs\OPUS_EXECUTION_PLAN.md
Appendix B, verified 2026-09-20) -> claim **4719-4778**, advance the marker to **4779 BEFORE the
first create** - as claimed by the seat immediately before the run (recorded in
OPUS_EXECUTION_PLAN.md) - if the marker has moved, the four blocks take the next 60 numbers in
order, 15 each. Allocation: B1 4719-4733, B2 4734-4748, B3 4749-4763, B4 4764-4778. Do not touch the
9101-9199 demo block. If MC1 aborts B2 after one create, the unused numbers stay burned in the ledger
(they are integers, not a scarce resource) - do not recycle them.

Wall clock: ~13 s per create x 15 + 5 s gaps ~ 4.5 min per block, plus ~1-2 min per restart. Roughly
**30-40 minutes end to end.** rtiexec memory: each block's instance ends at ~150 MB private
(15 creates x ~10 MB) and is then discarded - the leak is not a constraint here.

---------------------------------------------------------------------------------------------------
## 8. HIT / MISS / INCONCLUSIVE - written before the run

Let fN3 = failures in B1+B4 (n=30) and fN0 = failures in B2+B3 (n=30).

- **HIT.** fN0 = 0 (or 1) with fN3 >= 5 and Fisher one-sided p < 0.005, both MC checks passed in both
  treatment blocks, and B1 vs B4 not materially different (sec 8.5). Reading: **the corrupting
  content is rtiexec's own log output; silencing it removes the defect.** This is a cause claim and
  it goes to MAK as the answer to the sec 5.2(8) question, plus it is an operational remedy we
  control (sec 10).
- **PARTIAL.** 2 <= fN0 <= 4 with fN3 >= 8 and p < 0.005. Reading: log volume is *a* contributor, not
  the whole mechanism. Keep the holder, send the numbers to MAK, do not change the default.
- **MISS.** fN0 >= 5 and the Fisher test not significant. Reading: **see 8.2 - this does NOT refute
  H-FDD-UNTERMINATED**, it refutes "reducing the *printed* log removes it".
- **WORSE.** fN0 >= 15/30 with fN3 <= 8/30 and p < 0.05 in the opposite direction. Record it; a
  quieter log making it worse would itself be evidence about buffer reuse. Do not adopt level 0.
- **INCONCLUSIVE.** Any of: MC1 fails (void, see 7.4); fN3 < 5 (the control did not reproduce the
  defect at all - nothing to remove); B1 and B4 differ by >= 5 failures out of 15 (drift, sec 8.5);
  a block aborted on a stop condition.
- **STOP CONDITIONS (any one, immediately; nothing is killed):** the block's rtiexec exits;
  `StartRtiExec52.ps1` reports "ALREADY UP" (the old instance was not gone - the level did not
  change; sec 9 step 4); a refusal whose stdout names no module; three consecutive probe launch
  failures.

### 8.2 The asymmetry, declared in advance
A HIT is strong. **A MISS is weak, and deliberately so.** Because the docs do not say whether a
suppressed message is formatted before being discarded (sec 2.5), "no change at `-n 0`" is equally
consistent with (a) H-FDD-UNTERMINATED being wrong and (b) the strings still being rendered into the
same memory and thrown away. **Do not write "the log-aliasing reading is refuted" on a MISS.** Write
"reducing the printed log does not change the rate; whether the text is still being formatted is not
determinable from the vendor documentation", and go to 8.4.

### 8.3 What a refusal at `-n 0` can and cannot tell us
Sec 5: no probe stdout has ever contained the libxml2 echo. So a refusal in B2/B3 gives the module
name and nothing about the corrupting bytes. **Pre-registered: do not infer the content of the extra
bytes from a level-0 refusal.** If B2/B3 produce refusals and the seat wants the content, that needs
a further block at a level where the echo survives - which is a different experiment.

### 8.4 The designated next step on a MISS (pre-registered now so it is not invented afterwards)
Run one further pair of blocks varying `RTI_detachNotifyLevelFromStdOut` (RM Table 13: detaches
messages from **stdout only**, "no affect on logging notifications to a file"; our rid has 5 = nothing
detached) in a rid COPY handed to the rtiexec via `StartRtiExec52.ps1 -RidFile`, at `-NotifyLevel 3`
throughout. `detach 0` vs `detach 5`, 15 creates each, same harness. It separates the two sinks while
KEEPING the rtiexec log instrument intact, which is exactly what `-n 0` cannot do. If `detach 0`
fixes creates, the console-formatting path is implicated; if neither knob moves the rate, the
adjacent memory is not the logging path and H-ADJACENT-OTHER is promoted.
A second, zero-restart option worth recording: create #61 showed the **federation name** spliced into
the document (`<attrSTP825ABibute`). A block run against a federation name of a very different length
tests whether the corrupting offset tracks the log line length, at the cost of appNumbers only.

### 8.5 The built-in drift check
B1 and B4 are the same condition on two different fresh rtiexecs ~40 minutes apart. Report them
separately as well as pooled. A difference of >= 5 failures out of 15 between them means the machine
was not stable across the run and the treatment comparison cannot be trusted - INCONCLUSIVE,
regardless of how good B2/B3 look.

---------------------------------------------------------------------------------------------------
## 9. THE SEAT PROCEDURE, EXACT AND IN ORDER

Restarts are now authorised "as needed" for STP-825; this spends **four**. No executor performs any
of this. Read sec 2.7 before step 3 - the vendor lists killing the process as a documented shutdown
procedure, which is the sentence to cite if a permission layer balks.

0. **Register this prereg** (fill in sec 11), claim appNos 4719-4778, advance the ledger marker to
   4779 - as claimed by the seat immediately before the run (recorded in OPUS_EXECUTION_PLAN.md); if
   the marker has moved, the four blocks take the next 60 numbers in order, 15 each. Confirm no demo
   is imminent: **from step 3 until step 8 the machine has no demo holder and MAK-ONE-2025 does not
   exist.**
1. **Inventory, read-only.** `Get-Process rtiexec, rtiForwarder, RtiProbe, vrfSim*, vrfGui*,
   VrfC2SimApp*, WatchVrf*`; record pids, start times and rtiexec private bytes. Confirm no
   `runs\runner.lock`. Expect rtiexec 75168, rtiForwarder 70700, holder RtiProbe 68304.
2. **Account for the holder.** RtiProbe 68304 (appNo 4635) holds MAK-ONE-2025 until ~04:25Z
   2026-09-21. Per sec 2.8 it does NOT have to go first: the restart destroys the federation anyway
   and the experiment uses a different federation on a different process. **Decision for the seat,
   recorded in sec 11:** either wait for it (hours) or accept that step 3 orphans it. If orphaned, it
   is a process whose RTI has been taken away underneath it - with `RTI_reconnectEnabled 0` it will
   not recover; expect it to abort or to spin uselessly. Stopping THAT process afterwards is within
   the narrow standing permission (a process whose own RTI connection has failed), but if there is
   any doubt, ask the user - it is one question, not an assumption.
   Holder decision (seat): pid 68304 is NOT waited for - the restart destroys MAK-ONE-2025 regardless
   (RM 6.8.1) and the holder cannot reconnect (RTI_reconnectEnabled 0); it is left to be cleaned up
   after the final block and a fresh persistent holder is armed on the final rtiexec.
3. **Stop the pair, gracefully first.** In `C:\MAK\makRti5.0.1\bin`: `rti list` (read-only, harmless)
   and then `rti kill <componentHandle>` for the rtiexec (RM 5.6 Table 7: "Resigns local federates and
   shuts down local rtiexecs and RTI Forwarders"). If `rti list` enumerates nothing because the
   assistant is disabled, fall back to the vendor's own documented alternative (UG 4.2.3: "Kill the
   rtiexec process") - rtiexec first, then rtiForwarder if it survives. Do NOT use `rti shutdownall`
   (assistant-mediated, "not recommended for normal shutdown").
4. **Verify it is really gone before starting the next one.** No `rtiexec.exe` and no
   `rtiForwarder.exe` from `C:\MAK\makRti5.0.1\bin`; **nothing listening on TCP 4001 and nothing on
   5002**. This is the trap in sec 3: `StartRtiExec52.ps1` is ENSURE-UP and will happily report
   "ALREADY UP" against the old instance, leaving the notify level unchanged and the whole block
   void. If it prints ALREADY UP, STOP.
5. **Start block N's rtiexec** (only the level changes between blocks):
   `scripts\StartRtiExec52.ps1 -NotifyLevel 3` for B1/B4, `-NotifyLevel 0` for B2/B3. Everything else
   default: RtiDir `C:\MAK\makRti5.0.1`, rid = the repo's shared
   `config\rid-501-rtiexec-min.mtl` **unmodified**, 4001/4001, dest 127.255.255.255, interface
   127.0.0.1, **ForwarderPort 5002**, log to `runs\launch52\rtiexec_<UTCstamp>.log`. Exit 0 = READY,
   and the output must say `started=yes`. Record the new rtiexec and rtiForwarder pids.
   Expect the fresh-boot join race (memory: RTI fresh-boot join race) - the harness's first create
   doubles as the readiness gate; if it fails to launch, wait and re-run the block, do not restart.
6. **Run the block:**
   `& '<scratchpad>\validation\stp825_notify_harness.ps1' -AppNos (4719..4733) -RtiexecPid <pid> -NotifyLevel 3 -BlockLabel B1-N3`
   then B2 `-AppNos (4734..4748) -NotifyLevel 0 -BlockLabel B2-N0`, B3 `(4749..4763) -NotifyLevel 0
   -BlockLabel B3-N0`, B4 `(4764..4778) -NotifyLevel 3 -BlockLabel B4-N3`. `-RtiexecPid` is mandatory
   and asserted against the single running instance, so a block can never be scored against the wrong
   process. Repeat steps 3-6 for each block.
7. **Score:** `& '<scratchpad>\validation\stp825_notify_harness.ps1' -Score` - reads the CSV only,
   runs no creates, prints per-block and pooled tables, the manipulation-check columns, the B1-vs-B4
   drift check, the Fisher p and the sec 8 verdict.
8. **Re-arm the demo holder** on the LAST rtiexec (whatever level it ends at, per sec 10), on a fresh
   ledgered appNo: `RtiProbe.exe <appNo> MAK-ONE-2025 1 900 3`, or simply let `LaunchVrf52.ps1` start
   its own (RUNBOOK 9c, `-FederationHoldSecs` default 900, appNo 9190/9191). Verify "has joined
   federation" in the new rtiexec's log before declaring the machine demo-ready. Then clean up the
   orphaned RtiProbe 68304 if it is still present.
9. **Record** the outcome in RUNBOOK 9c and STP-825 the same turn, whichever way it falls, and update
   the MAK package per sec 10.

---------------------------------------------------------------------------------------------------
## 10. WHAT THE MACHINE IS LEFT IN

- **On a HIT** (level 0 removes the refusals): leave the machine on a **`-NotifyLevel 0` rtiexec** and
  change `scripts\StartRtiExec52.ps1`'s `-NotifyLevel` **default from 3 to 0**, with the parameter
  help renaming 3 as "diagnostic mode - required to investigate STP-825 or any create failure".
  RUNBOOK 9c must gain a blunt warning: **the demo posture's rtiexec log is now nearly empty, so the
  "count-grep the rtiexec log for the create block" instruction in 9c no longer works** - the
  replacement instrument is RtiProbe/back-end stdout, validated 62/62 in sec 5. **The holder posture
  STAYS** until a real `LaunchVrf52` run with the SIM creating (not joining) passes on a level-0
  rtiexec; only then is dropping the holder even discussable, and that is the user's call. Belt and
  braces cost nothing here.
- **On a PARTIAL, MISS, WORSE or INCONCLUSIVE**: leave the machine on a **`-NotifyLevel 3`** rtiexec
  (finish with B4's instance or start one more), change **nothing** in the repo, keep the shared rid
  at the vendor defaults, and keep the holder. The diagnostic log is worth more than a partial rate
  improvement.
- **In every outcome**: record the rtiexec pid, notify level, start time and private bytes in the
  handoff doc, and treat "how many creates has this instance served" as a health number
  (adjudication sec 4). Add to the MAK package: the level-0 result either way, and the sec 2.5
  documentation gap - *the manual does not state whether messages above the notification level are
  formatted before being discarded* - which is a fair question to ask them directly.

---------------------------------------------------------------------------------------------------
## 11. HONEST PRIOR, and VERIFIED vs ASSUMED

**P(HIT) = 0.35.** Higher than the 0.25 I gave the bundling arms, because this time the manipulation
targets the thing that was actually *observed* in the buffer rather than a configuration parameter
inferred to matter. Held down by three things: (i) the sec 2.5 gap - if the level filters after
formatting, the manipulation is a no-op and we get a MISS that means nothing; (ii) H-FDD-UNTERMINATED
still does not explain **which** module is chosen or why the rate is ~20-45 % rather than 0 or 100 %,
so a mechanism that fits the evidence is not yet a mechanism that predicts it; (iii) even at level 0
rtiexec still formats *something* (fatal-level paths, internal bookkeeping), so "log output" is
reduced, not eliminated. P(MISS that is genuinely informative) ~ 0.25; P(MISS that is ambiguous per
8.2) ~ 0.40.
Worth running regardless: 40 minutes, 60 ledger integers, no repo change, no vendor dependency, and
on a HIT it is both a diagnosis and a remedy we own.

**VERIFIED (primary sources, this session):** the notify-level scale and its direction, `-q`, `-l`,
`RTI_rtiExecLogFileName`, `RTI_detachNotifyLevelFromStdOut`, the absence of any runtime
set-notify-level command in RM 5.6 Table 7, the UG 4.2.3 shutdown list including "Kill the rtiexec
process", RM 6.8.1 forcible removal on a lost TCP connection and RM 6.8.2 reconnect requiring
`RTI_reconnectEnabled` (ours is 0), the forwarder log being disabled in our rid; `StartRtiExec52.ps1`
exposing `-NotifyLevel 0..4` default 3 and being ENSURE-UP; rtiexec 75168 running at `notifyLevel: 3`
with forwarder port 5002 (read from our own log's RID dump, the process was not queried); all four
archived rtiexec logs at level 3; the 62/62 stdout-instrument agreement, 18/18 module-name agreement
and 12/12 module names surviving the 0xC0000005 crash; zero libxml2 echoes in any probe stdout; the
ordinal-trend quartiles and r = -0.132; the ledger marker 4719; the power table.

**REASONED (inference, stated as such):** that a fresh rtiexec is the only other variable a restart
changes that matters, bounded by sec 6; that ABBA's bracketing gives a usable drift check; that an
orphaned holder will not recover (from `RTI_reconnectEnabled 0` plus RM 6.8.2, not from an observed
orphaning).

**ASSUMED / NOT PROVEN:** that `-n 0` actually suppresses the `DtFedExec: Fed File arrived at FedEx.`
emission - **this is what MC1 tests, and the experiment is void if it does not**; that the suppressed
messages are not still formatted (sec 2.5 - undocumented, and untestable from outside rtiexec); that
`rti list`/`rti kill` work with the assistant disabled (step 3 has a documented fallback); that the
base rate in a fresh instance is the 0.227-0.283 observed on 75168.

**UNEXPLAINED, and it is a falsifier not a footnote:** which module is chosen on a given create, and
why the rate sits at ~20-45 %. The sender's stream is byte-identical every time. A HIT here would
identify the corrupting *content* without explaining the *selection*, and the prereg says so in
advance so that nobody later reads a HIT as a complete diagnosis.

---------------------------------------------------------------------------------------------------
## RESULT (run 2026-09-20 22:27Z-22:48Z, executed by the seat; adjudicated by an independent Opus reader)

Execution facts: `rti list` answered "RTI commands are not available when the RTI Assistant is
disabled", so the UG 4.2.3 "Kill the rtiexec process" fallback was used, by pid, under the
user's 2026-09-20 "restart as needed" authorisation; old rtiexec 75168 held 957 MB private.
Four fresh instances left running: B1 -n 3 pid 56088, B2 -n 0 pid 11704, B3 -n 0 pid 88624, B4
-n 3 pid 51560; ledger 4719-4778; persistent holder re-armed 22:50Z pid 12916 appNo 4779 on
the first create.
Block table: B1 3/15 (0.200), B2 0/15, B3 0/15, B4 2/15 (0.133). Pooled control (-n 3) 5/30
(0.167) vs treatment (-n 0) 0/30; Fisher one-sided p=0.0260928 against the pre-registered
alpha 0.005; drift |B1-B4|=1 (threshold 5, passes). Manipulation check: 32 "Fed File arrived"
lines over 30 control creates, 0 over 30 treatment creates; both level-0 logs exist at exactly
0 bytes.
REGISTERED VERDICT: INCONCLUSIVE-UNDERPOWERED, by exclusion. The harness printed "MISS", but
that is WRONG against the prereg's own sec 8 wording: MISS requires fN0>=5 (fN0=0, not met);
HIT fails on p alone (0.0261 >= 0.005, every other HIT condition met); PARTIAL does not apply
(fN0 not in [2,4]); INCONCLUSIVE's enumerated triggers (MC1 fail, fN3<5, drift>=5, a stop
condition) do NOT fire either (fN3=5, one over the line) - the five branches were not an
exhaustive partition of the outcome space, a prereg defect, recorded as such rather than
papered over. Alpha stays 0.005 (not loosened after the fact); the remedy claim is NOT
established; the sec 10 machine-left-in rule for a non-HIT binds: stay at -NotifyLevel 3,
change nothing in the repo, keep the holder.
What the data SUPPORT, separately from the registered label: 95% Clopper-Pearson upper bound
on the level-0 rate from 0/30 is 0.1157; single-arm P(0/30) = 4.21e-03 at today's own control
rate, 1.87e-03 pooled across all level-3 creates on the machine (41/217) - BUT two level-3
instances have run 0/45 and 0/27 before with NO manipulation (per-instance rates 0/45, 0/27,
3/18, 33/97, 3/15, 2/15; homogeneity chi-sq=31.63, 5 df): THE TRUE REPLICATE IS THE INSTANCE,
not the create, and a 2-vs-2 instance design has a minimum attainable exact p of 0.167 - this
design could never have reached alpha 0.005 under a clustering-aware analysis.
New falsifier, not a footnote: 3 of today's 5 control refusals produced NO libxml2 diagnostic
at all, after 36/36 prior refusals showed the tail-only "Extra content" shape - the "log text
in the FDD" reading now covers a subset of refusals of unknown size.
Memory growth is 9.36 MB/create at BOTH notify levels (Welch t=0.000) - the leak is not log
buffers.
Defects of the run itself: the harness has a `$N`/`$n` case-insensitivity collision (data
intact - 60/60 recount agreement - but every block-end manipulation check never executed);
B2's restart output was never captured to a file (it exists only in the seat's own session
transcript: "NEW rtiexec pid 11704 rtiForwarder pid 80016 notifyLevel 0"); background load was
never measured; the two treatment blocks ran adjacent in time (22:33-22:43), confounded with
any mid-run transient.
Next: a NEW prereg (16 alternating fresh instances, 192 creates, an instance-level permutation
test) is being drafted, to be analysed ALONE - today's 60 creates are a pilot for it and
contribute nothing to its test.
