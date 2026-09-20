# PREREG (DRAFT for the seat to register) - STP-825: does RTI packet bundling cause the FDD create rejections?

REGISTERED 2026-09-20 by the seat, BEFORE any create of the experiment; design by the Opus research lane; source scratchpad PREREG_STP825_BUNDLING_DRAFT.md.

Written 2026-09-20 by an Opus RESEARCH executor. NOTHING WAS RUN: no federate, no RtiProbe, no
rtiexec/rtiForwarder touch, no repo edit. Docs read and cited first (sec 2). Tier HEAVY (cause claim).
Register BEFORE any create is issued. ASCII only.

Entry record: docs/experiments/PREREG_V6F_FDD_CREATE_2026-09-15.md (P1-P7, sec 4.6 wire capture),
docs/RUNBOOK.md sec 9c, scratchpad wirecap/wirecap_report.md, scratchpad validation/v6harvest/
d3_harvest_report.md (STP-825 section).
Evidence produced for this prereg (scratchpad validation/): parse_creates.py + create_table.txt
(the full create census), create_stats.py (clustering), design_power.py (N), rtidocs/*.txt
(pdftotext of the installed RTI 5.0.1 PDFs).

---------------------------------------------------------------------------------------------------
## 1. THE CREATE CENSUS - what the log actually says (VERIFIED, recomputed here)

Source: runs/launch52/rtiexec_20260915T155028Z5.0.1-20260915-115029-Legatus-281993-75168.log
(OUR log, 6.5 MB; grepped, never opened whole). rtiexec pid 75168 up since 2026-09-15 11:50:29
local; rtiForwarder pid 70700 since 11:50:31.

- 82 CreateMsgKind messages reached rtiexec. **47 of them never touched the FOM Reader**: they got
  "Could not create federation MAK-ONE-2025, because it already exists." and a 37-byte
  CreateResponse. Only **35 creates distributed modules and were parsed**: 20 Success, 15 Error =
  **42.9 %**. (The brief's 19/14 plus today's 4634 refusal and 4635 success = these 35.)
- Every one of the 35 shipped the SAME payload: 58 FomModuleDistExec messages, 18 modules,
  3,117,884 payload bytes, identical order and identical per-block sizes in failures and successes.
  Largest message 65,093 B (payload cap 65,000); smallest 4,813 B.
- **All 35 creates on this rtiexec were issued by a `remoteControl` federate (RtiProbe / the
  holder)** - every success is followed by "Federate remoteControl <pid> (\"remoteControl\" 2) has
  joined". The sim joined as handle 4/5/11/18, never as the creator. This **falsifies the
  PREREG_V6F sec 4.1 reading that "the sender identity matters"** (that was rtiexec 36840: sim 3/3
  fail, RtiProbe 6/6 success). On 75168 RtiProbe fails 15/35.

### 1.1 The create table (35 rows) - moved to the census sibling

The full 35-row create table is unchanged in substance but moved out of this file for length,
together with the appNo attribution note for rows 30-35 (the coordinator's D3 harvest):
docs/experiments/PREREG_STP825_BUNDLING_2026-09-20_CENSUS.md. Nothing was deleted.

### 1.2 What the census DOES and DOES NOT support

**(a) SIZE THRESHOLD: ABSENT. The "message larger than RTI_tcpPacketBundlingSize" reading, in its
simple form, is FALSIFIED.** Rejected payloads: 5,692 / 9,114 / 9,699 / 30,178 / 34,197 / 52,829 /
65,000 B. **8 of the 15 rejections are modules whose whole message is well under the 50,000-byte
bundle**, the smallest being MAK-VRLExt-3 at 5,692 B (5,772 B on the wire) - rejected twice. There
is no threshold, no TCP-segment multiple (5,772 is not a multiple of 1460/1500/8192/65535), and the
rejected module's position in the 58-block stream varies over the whole range (ordinals 1, 20, 22,
24, 25, 42, 46, 54, 56, 58). Whatever selects the victim, message size is not it.

**(b) TAIL-ONLY CORRUPTION: 15/15, DETERMINISTIC, NEW AND STRONG.** For every rejection the libxml2
error is at exactly (that module's own line count + 1), verified against the files on disk
(C:\MAK\vrforces5.2d\bin64, specific files listed, no recursive scan):

    MAK-VRLExt-3 130 -> 131 (x2)      MAK-Aerodrome-1 686 -> 687 (x3)
    RPR-Enumerations 28413 -> 28414 (x3)  MAK-VRFExt-12 1180 -> 1181
    MAK-DynamicTerrain-2 247 -> 248   MAK-DER-1 190 -> 191   MAK-METOC-3 505 -> 506
    MAK-VRFAggregate-7 2506 -> 2507   RPR-MAK_IFF-4 3152 -> 3153   RPR_FOM_v2.0 25668 -> 25669

The parser NEVER fails inside a document. It always parses the complete, correct module and then
finds more bytes. Combined with the wire capture (sender byte-perfect, sec 4.6 of PREREG_V6F), the
defect is: **the receive path hands the FOM Reader a buffer that is the module PLUS at least one
byte that is not the module's.**

**(c) THE FIRST EXTRA BYTE PRINTS AS NOTHING - REASONED, and it discriminates.** libxml2 echoes the
offending line and puts a caret under the offending column. In the log:
  - MAK-DER-1 and MAK-VRLExt-3 (the two rejected modules whose file has NO trailing newline):
    the echo is `</objectModel>` and the caret is at **column 15** - the first byte past the
    document, on the same line.
  - MAK-METOC-3, RPR-Enumerations, MAK-Aerodrome-1, MAK-VRFExt-12 (files that DO end with a
    newline): the error is at line N+1, and the caret is at **column 1** with NOTHING echoed before
    it.
  In both shapes libxml2 printed ZERO characters of the extra content. Its context printer copies
  bytes up to a newline and prints them as a C string, so a leading NUL prints as nothing.
  A big-endian u32 length prefix of any message in this stream (4,813..65,093) begins 0x00 0x00.
  **This fits "the buffer contains the module followed by the start of the NEXT wire message" and
  fits poorly with "stale XML left in a reused buffer" (that tail would be printable text).**
  FALSIFIER: any rejection whose echoed line shows printable junk after the root close tag.
  (The `DtFedExec: Fed File arrived at FedEx.` / `[====` text seen between the error and the caret
  is rtiexec's own log output spliced in by another thread, not echoed buffer content. Treat it as
  evidence that module arrivals run CONCURRENTLY with the parse, nothing more.)

**(d) CLUSTERING IS REAL AND STATISTICALLY SIGNIFICANT.** Sequence
`FFFSSSFFSSSSSSSSSSSFFFFSFSSSSFFFFFS`: 10 runs where 18.1 are expected,
Wald-Wolfowitz z = -2.85, two-sided p = 0.0043. Lag-1: P(fail | previous failed) = 10/15 = 0.67
against P(fail | previous succeeded) = 4/19 = 0.21. **Create outcomes are NOT independent Bernoulli
trials**; any design that counts consecutive successes must pay for this (sec 5.2).

**(e) NOT A FUNCTION OF IDLE TIME.** Median gap before a failure 15.9 s, before a success 28.5 s;
failures occur after gaps of 7 s and after 3 hours. The apparent short-gap bias is the clustering of
(d) re-expressed - retries follow a failure fast. No support for a time-decaying receiver state.

**(f) STP-832 IS NOT CLOSED (side finding, hand to the seat).** The deployed
tools\RtiProbe\bin\Release-5.2\net10.0\win-x64\RtiProbe.exe has mtime 2026-09-15 16:53:06Z, i.e. it
IS the post-merge 13e8c73 rebuild (RUNBOOK: consumers rebuilt 16:52:50-16:53:13Z), yet appNo 4632
and 4634 today still died `Fatal error. 0xC0000005 at <Module>.vrf.VrfFacade.Start` AFTER printing
the vendor refusal. Other refusals on the same build exited cleanly. So the crash is in the probe's
post-refusal cleanup and is intermittent. Consequence for this experiment: the harness MUST treat
exit -1073741819 as "create REFUSED", not as a harness fault, and must not retry the appNumber.

---------------------------------------------------------------------------------------------------
## 2. DOCS READ (vendor first), with the quotes the design leans on

Installed copies, extracted with pdftotext into scratchpad validation/rtidocs/:
C:\MAK\makRti5.0.1\doc\{RTIReferenceManual,RTIUsersGuide,RTI5.0.1ReleaseNotes,MAKInteroperabilityGuide}.pdf
(directory listed, not scanned). The repo's docs/vendor/mak-5.2 holds VR-Forces 5.2 PDFs only - no
RTI guide - so the installed set is the primary.

1. **RM Table 13 (A.4 LRC-Specific Parameters), p.227**, verbatim:
   - `RTI_tcpPacketBundlingSize` - "When packet bundling is enabled, specifies the size of the
     message bundles for TCP packets. **Default: 50000.**"
   - `RTI_udpPacketBundlingSize` - "... for UDP datagrams. The value should be <= MTU of the
     network. Default: 1400."
   - `RTI_enablePacketBundling` - "Bundles messages together into a single TCP packet or UDP
     datagram, up to the size limit specified by RTI_packetBundlingSize. Default: 1400."
   **No maximum and no legal range is documented, and nothing is said about a single message larger
   than the bundle size.** That silence is itself a finding for the MAK case.
   `RTI_tcpPacketBundlingSize` appears ONLY in Table 13 (LRC-specific), never in Table 12
   (exercise-wide) - so it is a per-federate (LRC) setting, which is what makes sec 4's
   federate-only arms legitimate.
2. **RM 9.3 "Packet Bundling for Reliable and Best Effort Messages", p.123**: "If bundling is
   enabled ... the RTI bundles messages into a single TCP packet or UDP datagram, up to the size
   limit configured by the Packet Bundling Size RTI setting. ... The recommended maximum bundle size
   is 1492 bytes for WAN traffic, or the maximum transmission unit (MTU) of the local network, if
   you are just running on a LAN. **Bundles are flushed at the end of every tick(), even if the
   bundle is not full.**"
   Note the tension in the vendor's own text: the TCP default is 50,000 while the guidance is
   "<= MTU". Our 65,093-byte messages are 43x the recommended bundle and 1.3x the configured one.
3. **RM 9.8 "Configuring the Network Buffer Sizes", p.129**: "The TCP Buffer Size RTI setting sets
   the size of the internal buffer into which TCP packets get read. **This buffer size should be
   large enough to hold the largest message to be received. Default: 500000.**" Ours is 500,000
   (>= 65,093, so this is NOT the defect) - and it caps how large arm C may legally go.
4. **RM 4.9 "The RTI Settings File Consistency Checker"**: the listed settings "are evaluated
   before the FOM consistency check is performed. Therefore, they cannot have their consistency
   enforced by the consistency checker. It is your responsibility to ensure that, if required, they
   are the same in all federates." The list contains `RTI_enablePacketBundling` but **NOT**
   `RTI_tcpPacketBundlingSize`. Our rid sets `RTI_ridConsistencyChecking 0` (= NONE), so rtiexec
   overrides nothing. Per-federate variation is therefore legal and unpoliced - with the caveat in
   sec 4.4 for arm B.
5. **RM 13.3 (FOM modules)**: "If a federate tries to create a federation that already exists, any
   FOM modules it specifies have no effect on the current FOM. Additionally, the create and join
   operations, including the merging of the FOM modules, are an atomic action. A failure to merge a
   single FOM module results in the failure of the entire call." -> **while ANY federate holds
   MAK-ONE-2025, a create measures nothing** (confirmed empirically: 47 of 82 creates). This governs
   sec 4.5.
6. **RM 6.2.2 / 13.1**: FED-file distribution is required when FOM modules are used; the creating
   federate's LRC ships the content to the rtiexec. So the creator is the sender and its rid governs
   the send side.
7. **Release Notes 5.0.1** (Fixed Bugs, Feature List, Known Problems): nothing on FOM module
   distribution, create, bundling, forwarder framing or large messages. New in 5.0.1 and useful for
   the MAK case: "Added Wireshark dissector files for MAK RTI messages for advanced debugging".
8. **Vendor default rid, C:\MAK\makRti5.0.1\rid.mtl vs our config\rid-501-rtiexec-min.mtl** - every
   transport parameter is IDENTICAL; the only difference in this whole family is the forwarder port:

   | parameter | shipped rid.mtl | ours | line |
   |---|---|---|---|
   | RTI_enablePacketBundling | 1 | 1 | 402 |
   | RTI_tcpPacketBundlingSize | 50000 | 50000 | 403 |
   | RTI_udpPacketBundlingSize | 1400 | 1400 | 404 |
   | RTI_waitIOPeriodForBundling | 0 | 0 | 411 |
   | RTI_tcpNoDelay | 1 | 1 | 372 |
   | RTI_tcpBufferSize | 500000 | 500000 | 373 |
   | RTI_socketReceiveBufferSize | 2000000 | 2000000 | 374 |
   | RTI_socketSendBufferSize | 20000000 | 20000000 | 375 |
   | RTI_maxUdpPacketSize | 15000 | 15000 | 376 |
   | RTI_enableTcpCompression / Udp | 0 / 0 | 0 / 0 | 394/396 |
   | RTI_distributeFedFile | 1 | 1 | 290 |
   | RTI_fullFedFileDistribution | 0 | 0 | 295 |
   | RTI_fomModuleMerging / Sorting | 1 / 1 | 1 / 1 | 298/303 |
   | RTI_crcCheckFedFile | 0 | 0 | 321 |
   | RTI_distributedForwarderPort | 5000 | **5002** | 86 |

   **We are running the vendor's own defaults.** That is an adversarial point against any bundling
   hypothesis (every MAK RTI 5.0.1 site shipping a large modular FOM would hit this) and it is the
   strongest single reason to keep the prior low - see sec 7.
9. **Internet (2026-09-20)**: the exact strings "Bad FDD File. Could not find Document Root in FDD
   File" and "Extra content at the end of the document" + FOM module return nothing MAK-specific.
   docs.mak.com/support was WALKED: the newest RTI doc set listed is **5.0.1** (Reference Manual,
   Users Guide, Release Notes); there is no public 5.1 doc set, so **no vendor fix after 5.0.1 can
   be cited**, and "upgrade the RTI" is not an available remedy today. (The 5.0.1 Fixed Bugs table
   does show a parallel "RTI 5.1" fix-version column, i.e. 5.1 exists internally at MAK - a
   question for the support case, not a lever we hold.)

---------------------------------------------------------------------------------------------------
## 3. HYPOTHESES, stated falsifiably

**H-BUNDLE-BIG (R1).** A FomModuleDistExec message (up to 65,093 B) is larger than
RTI_tcpPacketBundlingSize (50,000), so the sender takes an oversize/flush path whose length
bookkeeping is wrong, and the receiver's de-bundler mis-terminates a module buffer.
*Predicts:* raising the bundle above the largest message removes the failures. Also predicts that
turning bundling OFF removes them (the oversize path is never entered either way).
*Falsified by:* failures that persist at a bundle size > 65,093.
*Already weakened by:* sec 1.2(a) - modules of 5,692 B are rejected as readily as 65,000 B ones.

**H-COALESCE (R2) - the reading the evidence favours.** The receive path (rtiForwarder or rtiexec)
de-bundles a buffer holding SEVERAL messages and computes one module's end wrong by a small amount,
so the FOM Reader sees `<module bytes><start of the next message>`. Bundling is what puts a next
message in the same buffer.
*Predicts:* anything that reduces coalescing - bundling OFF, or a bundle small enough to hold one
message - reduces or removes the failures; **a LARGER bundle is neutral or WORSE** (more messages
per bundle).
*Supported by:* 15/15 tail-only errors at exactly end+1 (1.2b); the extra content printing as
nothing, consistent with a NUL-leading u32 length prefix (1.2c); the wire capture showing the
sender's frames tile exactly.
*Falsified by:* failures unchanged with bundling OFF, or a rejection whose extra content is
printable XML.

**H-RACE.** A concurrency defect in the FOM Reader independent of framing: modules keep arriving on
the I/O thread while the parse runs (the spliced `Fed File arrived at FedEx.` lines prove the
overlap), and a shared length/terminator is written under a race.
*Predicts:* NO RID setting helps; the rate tracks how much overlap there is, so it should respond to
arrival pacing, not to bundling.
*Separated from H-BUNDLE/H-COALESCE by:* arms B and C both failing to change the rate (sec 6, MISS).
*Note:* this hypothesis also predicts the clustering of 1.2(d) more naturally than the others do.

**H-BUFFER-REUSE.** rtiexec reuses one FDD buffer across modules/creates and does not re-terminate
it, so a shorter module shows the previous occupant's tail.
*Predicts:* the victim is always a module SHORTER than a recently parsed one, and the extra content
is printable XML.
*DISFAVOURED here:* RPR-Enumerations (1,084,962 B, the LONGEST module in the set) was rejected three
times - nothing longer precedes it; and 1.2(c) says the extra bytes print as nothing.

**H-FORWARDER-REFRAME.** rtiForwarder re-frames on relay and injects the error.
*Predicts:* a change made only in the FEDERATE's rid has no effect, because the forwarder re-bundles
with its own (rtiexec-inherited) settings.
*Separated by:* arm B/C showing nothing while the sec 8 shared-rid + restart arm (which changes
rtiexec and the forwarder too) does. **This is the main reason sec 8 exists.**

**Already falsified, do not re-test:** long-lived rtiexec state (a fresh rtiexec failed too,
PREREG_V6F 4.5); sender-side corruption (wire capture); text-mode reads (LF-only modules fail alike);
Defender; machine load; sender identity (sec 1: RtiProbe fails 15/35 on this rtiexec).

---------------------------------------------------------------------------------------------------
## 4. THE EXPERIMENT - ONE variable (the bundling configuration), three levels, interleaved

### 4.1 Why not "before on the old rtiexec, after on a restarted one"
That design changes TWO things at once (the rid AND the rtiexec instance) and spends the user's one
restart authorisation on the weakest arm. `RTI_tcpPacketBundlingSize` is an LRC parameter (sec 2.1)
read by each federate process at start, and each RtiProbe run is a fresh process, so the sender-side
setting can be varied **per invocation, with no restart at all**, by pointing `RTI_RID_FILE` at a
copy - exactly the technique the wire-capture executor used with `rid-501-relay4002.mtl`.
Interleaving the arms on ONE rtiexec also makes the clustering of 1.2(d) hit all arms equally
instead of confounding a before/after comparison.

### 4.2 Arms (each rid is a COPY in the scratchpad differing from the control in ONE line)
Control file: `config\rid-501-rtiexec-min.mtl` (605 CR / 605 LF, 0 non-ASCII - preserve CRLF; edit
with a Python script file, never a heredoc, and never sed -i/perl -i: MSYS rewrites CRLF).

| arm | file (scratchpad validation/) | the single change | tests |
|-----|-------------------------------|-------------------|-------|
| A   | (the control itself, unmodified) | none - `RTI_enablePacketBundling 1`, `RTI_tcpPacketBundlingSize 50000` | baseline |
| B   | `rid-stp825-nobundle.mtl` | line 402 `RTI_enablePacketBundling 1` -> `0` | H-COALESCE |
| C   | `rid-stp825-bundle100k.mtl` | line 403 `RTI_tcpPacketBundlingSize 50000` -> `100000` | H-BUNDLE-BIG |

`100000` is chosen because it is > the largest message on the wire (65,093 B, so no message can span
a bundle) and 5x below `RTI_tcpBufferSize` 500000, which RM 9.8 says "should be large enough to hold
the largest message to be received". No vendor maximum is documented; do not exceed 400,000.
B and C predict OPPOSITE outcomes under R1 vs R2, which is exactly why both run, and why they run
INTERLEAVED rather than in a fixed order (a fixed order would confound with the clustering).

### 4.3 The trial unit
One `RtiProbe.exe <appNo> <federation> 1 2 3` invocation - **maxAttempts=1** so that one appNumber
buys exactly one create and the outcome is unambiguous (the default 5 hides up to 5 creates behind
one appNumber; that is how the 4-in-a-row cluster was absorbed). Posture copied verbatim from
`validation\p1_rtiprobe_x5.ps1`: MAK_VRFDIR/MAK_VRLDIR/MAK_RTIDIR, `RTI_ASSISTANT_DISABLE=1`,
`MAKLMGRD_LICENSE_FILE` from the User scope, PATH prefixed
`C:\MAK\vrforces5.2d\bin64;C:\MAK\vrlink5.10\bin64;C:\MAK\makRti5.0.1\bin`, cwd
`C:\MAK\vrforces5.2d\bin64`, stdout/stderr to files. The ONLY difference between arms is
`RTI_RID_FILE`. Spacing: wait for `Get-Process RtiProbe` to be empty for 3 consecutive seconds, then
3 s, then the next trial (the p1/wirecap concurrency guard). Order: A,B,C,A,B,C,... fixed rotation.
Outcome is read from the rtiexec log window for that trial (`Sending Create Response =
Success|Error`, `Failed to process FOM file <M>`), NOT from the probe's exit code; exit codes 0 / 1 /
-1073741819 are all recorded, and -1073741819 counts as REFUSED (sec 1.2f).

### 4.4 Risk on arm B, and its own falsifier
`RTI_enablePacketBundling` is one of the settings RM 4.9 says the consistency checker cannot police
and that "it is your responsibility to ensure ... are the same in all federates". Arm B deliberately
differs from rtiexec/rtiForwarder. Our own wire capture shows the stream is a flat run of
`[u32 BE len][0xff kind][body]` frames with no bundle envelope, so a non-bundling sender should
produce a stream the receiver parses identically - REASONED from our capture, not vendor-documented.
**If arm B trials fail to CONNECT or are refused with a transport/version error rather than the FDD
parser error, stop arm B at once**: that means the parameter is effectively federation-wide, and
arm B moves to sec 8 (shared rid + the one authorised restart).

### 4.5 Where to create - and the holder problem (RULE gate, decide before running)
**A create only exercises the FOM Reader if the federation does not already exist** (RM 13.3; 47/82
creates in the log took the "already exists" path and parsed nothing). Right now `RtiProbe` pid
68304 holds MAK-ONE-2025 with an 8-hour settle started 2026-09-20 20:25:21Z - it resigns about
**04:25Z on 2026-09-21**. The 900 s holder (pid 33232, appNo 4653) has already expired; only 68304
remains alive.

- **PRIMARY (recommended): create a SEPARATE federation.** `RtiProbe`'s second positional argument
  overrides the federation name while the FOM identity still comes from the connection config -
  verified in source, `tools/Shared/StackIdentity.cs:50` sets `cfg.Federation = federationArg` and
  the 5.2 banner says "explicit override; FOM identity still from the connection config"; only
  `ConnectionConfigFile` is applied afterwards. So `RtiProbe.exe <appNo> STP825AB 1 2 3` ships the
  same 18 modules through the same FOM Reader while **MAK-ONE-2025 and its holder are untouched**
  and the demo path keeps working during the experiment. Each trial creates, joins, settles 2 s,
  resigns and (as sole federate) destroys STP825AB, so nothing accumulates.
  **PILOT-0, mandatory, 2 appNumbers, run first:** two arm-A creates on STP825AB. Confirm in the
  rtiexec log: `Creating federation STP825AB with handle N. Using FDD files:` listing 18 modules and
  **58 FomModuleDistExec blocks** for each. If the block count is not 58, or rtiexec short-circuits
  because it already has the modules, ABANDON the separate-federation route and use the fallback.
  Residual risk: two concurrent federations on one rtiexec is a posture this project has never run.
  If PILOT-0 produces ANY disturbance of MAK-ONE-2025 (a holder resign, a forwarder error), stop.
- **FALLBACK: MAK-ONE-2025 with no holder.** Requires the 8 h holder to be gone. Either wait for it
  to resign (~04:25Z 2026-09-21) or ask the USER to stop it - **the seat must NOT stop it on its own**
  (the standing permission covers only a process that failed its own join; 68304 is healthy and is
  the demo holder). This is a RULE gate: ask, do not assume.

CONDITIONS FOR THIS RUN: No rtiexec restart is used by this design; the user's single authorised
restart (2026-09-20) is HELD for the confirmation step (sec 8). The 8 h persistent holder (pid
68304, appNo 4635) stays joined to MAK-ONE-2025 throughout; the experiment's creates go to the
separate federation STP825AB. Machine-load conditions at run time are recorded in the RESULT block.

### 4.6 What each trial records (one CSV row)
`utc, arm, appNo, federation, probe_pid, exit_code, rtiexec_result, rejected_module, module_bytes,
libxml_line, gap_since_prev_create_s, rtiexec_log_line_window`.

---------------------------------------------------------------------------------------------------
## 5. N - and what the clustering does to it

### 5.1 Why "k consecutive successes" is the wrong statistic here
Baseline p0 = 15/35 = 0.429.
- If creates were i.i.d.: k=6 successes reject "rate unchanged" at 0.05, **k=9 at 0.01**.
- With the measured lag-1 clustering (P(S) = 0.571, P(S|S) = 0.79), one clean BURST of k creates has
  probability 0.571 x 0.79^(k-1) under the null, so you need **k=12 at 0.05 and k=19 at 0.01** -
  clustering roughly DOUBLES the required run. A run of 9 clean creates is exactly what rows 10-19
  of the census already produced with no change at all.
- Blocks of 4 rapid creates separated widely: P(a block is clean | null) = 0.282; 5 clean blocks give
  p = 0.0018. That is the single-arm design if a control arm is impossible.

### 5.2 The chosen design and its N
Interleaved 3-arm, **n = 20 trials per arm = 60 creates = 60 ledgered appNumbers** (plus 2 for
PILOT-0 = **62 total**), Fisher exact one-sided, control vs each treatment, alpha 0.005 each
(Bonferroni over the two comparisons, family-wise 0.01):

| A failures | B (or C) failures | Fisher one-sided p |
|---|---|---|
| 9 (the expected 0.429 x 20) | 0 | 0.00061 |
| 8 | 0 | 0.00164 |
| 7 | 0 | 0.00416 |
| 6 | 0 | 0.01010 |
| 5 | 0 | 0.02356 |
| 9 | 1 | 0.00418 |
| 9 | 2 | 0.01548 |

Interleaving turns the clustering into a CONSERVATIVE nuisance: a cluster spans consecutive trials,
which by construction belong to different arms, so it pushes the arms together, never apart.

**Pre-registered extension rule (fixes the "A got lucky" hole).** If the A arm yields **fewer than 7
failures** in its 20 trials and the treatment arm is clean, the result is INCONCLUSIVE-UNDERPOWERED:
run ONE further block of 10 trials per arm (30 creates, 30 appNumbers) and test the pooled 30-per-arm
table at the same alpha. **No other extension is permitted**, and the rule is fixed now.

### 5.3 Ledger cost
`docs/OPUS_EXECUTION_PLAN.md` Appendix B marker was `*** NEXT FREE: 4657 ***` at draft time. The
62 numbers this experiment needs are claimed by the seat immediately before the run - 62 numbers
from the Appendix B marker (recorded in OPUS_EXECUTION_PLAN.md), marker advanced BEFORE the first
trial; the extension, if triggered, claims a further 30 numbers the same way, advanced before that
block's first trial. Do not use the 9101-9199 demo block.

---------------------------------------------------------------------------------------------------
## 6. HIT / MISS / INCONCLUSIVE - written before the run

Let fA, fB, fC be the failure counts out of 20 per arm.

- **HIT (H-COALESCE confirmed).** fB = 0 (or fB <= 1) with fA >= 7 and Fisher p < 0.005, while fC is
  NOT significantly better than A. Reading: the defect needs messages coalesced into one bundle;
  bundling OFF is the remedy. Action: propose `RTI_enablePacketBundling 0` in the shared rid as the
  product fix, then CONFIRM per sec 8 before shipping it.
- **HIT (H-BUNDLE-BIG confirmed).** fC = 0 (or <= 1) with fA >= 7 and p < 0.005, while B is not
  better. Reading: the oversize-message path is the defect. Action: `RTI_tcpPacketBundlingSize
  100000` in the shared rid, then sec 8.
- **BOTH HIT.** Both arms clean. Reading: bundling is implicated but the census (1.2a) says size is
  not the selector, so prefer B (removes the mechanism rather than moving a threshold) and record
  both in the MAK case.
- **MISS (bundling exonerated).** fB and fC are each within the Fisher-non-significant band of fA
  (p >= 0.05 both). Reading: **H-RACE or H-FORWARDER-REFRAME**. The RID is not the lever. Action: do
  NOT change the shared rid; go to sec 8 only to separate the forwarder (a shared-rid change reaches
  rtiexec and the forwarder as well), and otherwise escalate to MAK with the sec 9 package.
- **WORSE.** Any arm with fB or fC >= 15/20 (clearly above the 0.429 baseline; P(>=15 | 0.429) <
  0.01). Stop that arm immediately, record it - "a larger bundle makes it worse" is itself strong
  support for H-COALESCE and belongs in the MAK case.
- **INCONCLUSIVE.** fA < 7 (underpowered - apply the sec 5.2 extension exactly once); or PILOT-0
  fails (the separate federation does not distribute 58 blocks) and the fallback window is not
  available; or arm B cannot connect (sec 4.4 - move arm B to sec 8).
- **STOP CONDITIONS (any one, immediately).** rtiForwarder or rtiexec exits; MAK-ONE-2025's holder
  resigns or the demo federation is disturbed; a failure mode appears that is NOT the FDD parser
  error; three consecutive trials fail to launch the probe at all.

### 6.1 What each outcome means for the demo posture
**The holder stays in EVERY outcome, at least until a confirmation run on a fresh rtiexec (sec 8)
passes.** Reasons: (i) joins have never failed, so the join path is the only posture with a clean
record; (ii) LaunchVrf52 / runner Stage 2h already start their own holder and removing that is a
separate, tested change; (iii) a HIT proved on ONE rtiexec instance with a federate-only rid change
does not yet prove the shipped posture (shared rid, sim as creator) is safe. Only after sec 8 passes
on a fresh rtiexec, with the sim actually creating, should dropping the holder even be discussed -
and that is the user's call, not this experiment's.

### 6.2 What each outcome means for the MAK support case
The package is scratchpad\wirecap (c2s_2/c2s_8, s2c_2/s2c_8, index.jsonl, analyze.py, frame.py,
s2c.py, the rtiexec log window 104,197-105,809). Add from this prereg, whatever the outcome:
- the 35-create census with the 42.9 % rate and the clustering statistic;
- **the 15/15 "error line = module line count + 1" table** (sec 1.2b) - the single most diagnostic
  fact, and new since the wire capture;
- the observation that libxml2 prints ZERO bytes of the extra content, i.e. it begins with a byte
  that terminates a C string (sec 1.2c);
- that rejected module size spans 5,692 to 1,084,962 bytes with no threshold at the 50,000-byte
  bundle;
- the A/B/C result table, whichever way it falls. A MISS is as valuable to MAK as a HIT: it tells
  them the defect is not in the bundling configuration.
- the explicit question: **does the FOM Reader size and terminate its per-module FDD buffer from the
  accumulated CurBlockSize, or can the next message's bytes remain visible past the module's end?**
NEVER attach a vrfSim/vrfGui log or anything from C:\MAK\logs (they dump the environment in
cleartext). FOM XML and our own captures only.

---------------------------------------------------------------------------------------------------
## 7. HONEST PRIOR (record it now; a prereg that hides its prior is not a prereg)

**P(a bundling RID change removes the failures) = 0.25.** Breakdown:
- P(arm B / bundling OFF helps) ~ 0.22 - it is the arm the tail-only + NUL-leading evidence points
  at, but the mechanism still has to survive the fact that TCP coalesces at the socket layer whether
  the RTI bundles or not (`RTI_tcpBufferSize` 500,000 read buffer), so removing RTI-level bundling
  may not remove the "next message in the same read" condition at all.
- P(arm C / bigger bundle helps) ~ 0.08 - the census already falsified the size threshold this arm
  assumes.
- The strongest argument AGAINST both: **we run the vendor's shipped defaults, unmodified** (sec
  2.8). A defect triggered by `tcpPacketBundlingSize 50000` plus a large modular FOM would be hit by
  every MAK RTI 5.0.1 site using RPR FOM + NETN, and no trace of it exists in the 5.0.1 release notes
  or anywhere public.
- P(the experiment is still worth running) = 1.0 regardless: it is ~15 minutes, no restart, no
  process kill, no repo change, it costs only ledger integers, and a MISS is a publishable input to
  the MAK case that removes the entire RID surface from the search.
- **My leading hypothesis is H-RACE / receive-path framing rather than either bundling arm** - the
  spliced-log evidence that arrivals run concurrently with the parse, plus the clustering, fits it
  better. Nothing in this design tests H-RACE directly; that is a known gap and belongs in the MAK
  case, not in a RID knob.

---------------------------------------------------------------------------------------------------
## 8. THE ONE AUTHORISED rtiexec RESTART - hold it back for the CONFIRMATION, not the A/B

Sec 4 needs NO restart. Spend the authorisation only (a) to confirm a HIT in the shipped posture, or
(b) to separate H-FORWARDER-REFRAME after a MISS. Procedure for the SEAT (the seat alone; never an
executor; never kill rtiexec/rtiForwarder/rtiAssistant outside this authorisation):

1. **Account for the holder FIRST.** `RtiProbe` pid 68304 holds MAK-ONE-2025 until ~04:25Z
   2026-09-21. It must have resigned, or the USER must stop it, before anything is torn down. Any
   other RtiProbe/vrfSim/WatchVrf/VrfC2SimApp must be gone; `runs\runner.lock` must be absent;
   Docker and c2sim-server-vrf state per RUNBOOK. Inventory with Get-Process and record pids.
2. **Edit the SHARED rid** `config\rid-501-rtiexec-min.mtl` - ONE line (whichever arm won), CRLF
   preserved, ASCII checked with `rg -P "[^\x09\x0a\x0d\x20-\x7E]"` against a known-dirty control
   first. Commit or stash it so the rollback is one `git checkout`.
3. **Stop the old pair.** rtiexec 75168 first, then rtiForwarder 70700 if it survives (rtiexec
   normally takes it down; RTI UG 4.2.1 p4-11: the rtiexec starts its own forwarder and exits if it
   cannot). Confirm TCP 4001 and 5002 are free - `RTI_distributedForwarderPort` is **5002** in both
   the rid (line 86) and `scripts\StartRtiExec52.ps1 -ForwarderPort` (default 5002), moved off 5000
   on 2026-09-15 because an unrelated user process took 0.0.0.0:5000 (RUNBOOK 9c). Do not revert one
   without the other.
4. **Start the new one** with the scripted path only:
   `scripts\StartRtiExec52.ps1` (defaults: RtiDir C:\MAK\makRti5.0.1, rid = the repo's shared rid,
   4001/4001, dest 127.255.255.255, interface 127.0.0.1, ForwarderPort 5002, notify 3, log to
   `runs\launch52\rtiexec_<UTCstamp>.log`). It is ENSURE-UP: it refuses to start a second rtiexec and
   exits 3 if one is present but not listening - so step 3 must actually have completed. Exit 0 =
   READY. `-K` is deliberately not passed; the new rtiexec must outlive the run.
5. **Re-arm the demo holder** before anything else uses the federation, on a fresh ledgered appNo
   (`RtiProbe.exe <appNo> MAK-ONE-2025 1 900 3`, or let LaunchVrf52 do it) - and expect the
   fresh-boot join race (gate on RtiProbe serviceability, not on port-open).
6. **Confirmation measurement:** 20 creates on the fresh rtiexec with the changed shared rid, holder
   DOWN for that window, plus - because the shipped posture has the SIM creating - at least one real
   LaunchVrf52 run with `-FederationHoldSecs 0`. Pre-registered reading: 0 failures in 20 = the fix
   holds in the shipped posture (p=0.00061 against the 0.429 baseline); >= 5 failures = it does not.
7. **ROLLBACK, if creates get WORSE (any arm >= 15/20, or the confirmation run fails >= 5/20).**
   `git checkout -- config/rid-501-rtiexec-min.mtl`, repeat steps 3-5 with the unmodified rid, and
   record the whole sequence. The demo posture is unchanged by a rollback because the holder never
   left. **Do NOT spend a second restart without asking the user.**
8. Teardown-relaunch is known to wedge rtiForwarder (memory: teardown-relaunch wedges RTI). If the
   new pair does not come up clean, the next step is a REBOOT, which is the user's call - not a
   second kill attempt.

---------------------------------------------------------------------------------------------------
## 9. ORDERING TABLE

| # | step | gate | cost |
|---|------|------|------|
| 0 | Register this prereg; claim appNos from the Appendix B marker (62 numbers, claimed immediately before the run) | PREREG | ledger edit |
| 1 | Decide PRIMARY (STP825AB) vs FALLBACK (holder down) | RULE (ask the user about pid 68304) | - |
| 2 | Write the two rid copies; ASCII+CRLF check | - | scratchpad only |
| 3 | PILOT-0: 2 arm-A creates on STP825AB, confirm 58 blocks | - | 2 appNos |
| 4 | Run the interleaved A/B/C block, n=20 each | - | 60 appNos, ~15 min |
| 5 | Score against sec 6; extend once only if sec 5.2 triggers | PREREG | 30 appNos |
| 6 | Update RUNBOOK 9c + STP-825 with the result either way | - | doc edit |
| 7 | Confirmation on a fresh rtiexec with the shared rid | SPEND (the one restart) + RULE | user |
| 8 | MAK support package update | SPEND (outward-facing) | user |

---------------------------------------------------------------------------------------------------
## 10. VERIFIED vs ASSUMED

**VERIFIED (primary sources, re-derived here):** the 35-create census and 42.9 % rate; 58 blocks /
3,117,884 B identical in every create; all 35 creates issued by `remoteControl`; 47 creates
short-circuited by "already exists"; the 15/15 error-line = module-line-count + 1 table (line counts
read from the files on disk); rejected payloads 5,692..65,000 B with no threshold; the runs-test
clustering (z = -2.85, p = 0.0043); every transport parameter in our rid equals the vendor default
except the forwarder port; RM quotes in sec 2; docs.mak.com/support lists nothing newer than RTI
5.0.1; `StackIdentity.cs:50` lets the federation name be overridden while the FOM comes from the
connection config; RtiProbe.exe mtime 2026-09-15 16:53:06Z (post-STP-832) and today's 0xC0000005.

**REASONED (not vendor-documented; stated as inference):** that the extra content begins with a NUL
because libxml2 printed none of it, and that a u32 BE length prefix supplies that NUL; that a
non-bundling federate produces a wire stream the receiver parses identically (from our own capture's
flat framing); that `RTI_tcpPacketBundlingSize` governs the sender only (from its Table 13
LRC-specific placement).

**ASSUMED (must be checked by PILOT-0 or accepted as risk):** that creating a second federation
(STP825AB) on the same rtiexec distributes the same 58 blocks and does not disturb MAK-ONE-2025;
that arm B's per-federate `RTI_enablePacketBundling 0` is accepted by the forwarder; that 100,000 is
a legal `RTI_tcpPacketBundlingSize` (no maximum is documented; it is below the 500,000 read buffer).

**UNEXPLAINED, and it is a falsifier not a footnote:** nothing in this design explains WHY a given
create fails or which module is chosen. The sender's stream is identical every time. Until that is
explained, a clean A/B arm is evidence about a lever, not a diagnosis.

---------------------------------------------------------------------------------------------------
## RESULT (run 2026-09-20 21:22Z-21:37Z; adjudicated by an Opus reader, read-only)

Conditions as recorded: no build/sim process, only an idle compiler server; baseline CPU 15%;
containers up (c2sim-server-vrf, c2sim_server4.8.4.9, unrelated c2sim-stp615); no subagent
live; ledger 4657-4718 (62 creates); holder 68304 undisturbed; NO rtiexec restart used - the
user's authorisation remains unspent.
PILOT-0 PASSED (2 appNos): 1 refused+crashed (0xC0000005), 1 success, 58 blocks each,
MAK-ONE-2025 untouched.
Result (3x2): A control 4/20 (0.200); B bundling-OFF 9/20 (0.450); C bundle-100000 4/20
(0.200). Pre-registered Fisher one-sided (A worse): B vs A p=0.979629, C vs A p=0.652618 -
neither significant. Reader's own treatment-worse check: one-sided 0.0880, two-sided 0.1760 -
also not significant.
VERDICTS: REMEDY REFUTED for BOTH arms (neither fB nor fC is 0/<=1 - no HIT criterion met);
H-BUNDLE-BIG FALSIFIED (its falsifier "failures persist at bundle >65,093" met at fC=4);
H-COALESCE FALSIFIED (its falsifier "failures unchanged with bundling OFF, or extra content
printable" met twice - fB=9 and the content IS printable, finding A). RATE INCONCLUSIVE
(fA=4<7, the prereg's own power bar governs over MISS). Extension does NOT fire (no clean
treatment arm) and is NOT recommended (no arm to rescue; remedy question already settled).
Instrument SOUND: harness console, CSV and an independent rtiexec-log scan agree row for row
- 62 creates/62 destroys, handles 36-97, every child loaded its own rid. ONE limitation:
nothing proves a child APPLIED the modified parameter (RTI_ridConsistencyChecking=0 warns of
nothing). Arm and rotation position are perfectly confounded.
THREE findings: (A) the extra content past every tail-only rejection is rtiexec's OWN LOG
TEXT, 32/32 across this run and the re-read 09-15 census - the "NUL/u32 length-prefix"
reading above is RETRACTED. (B) create #61: rtiexec log strings written INSIDE the XML via
length-preserving substitutions, libxml2's semantic errors naming the corrupted tokens ->
surviving reading H-FDD-UNTERMINATED (FDD buffer not terminated at its accumulated length,
aliasing rtiexec's log-formatting memory); H-RACE survives; H-FORWARDER-REFRAME disfavoured.
(C) census clustering does NOT reproduce under fixed 5 s spacing (runs z=-0.118, p=0.91;
lag-1 0.294 vs 0.286) - today's earlier z=-2.85 is CORRECTED, not confirmed.
Also: no size relation (4,729-1,058,783 B); victim module ~uniform (14 of 18 distinct);
clean-vs-crash is a TIME-BAND, not an arm effect; all 12 crashes die in VrfFacade.Start before
the destroy call (STP-832 stays OPEN); rtiexec memory grows ~7.5-9.9 MB per cycle (957 MB
private after 97 lifetime creates). Still unexplained, as a falsifier: WHY a given create is
hit, and WHICH module.
Proposed next manipulation (NOT registered; needs the user's word for the restart):
`-NotifyLevel 0`, 20 arm-A creates; 0-1 failures = strong support, >=5 = refuted, 2-4 =
inconclusive; fresh-rtiexec confound declared.
