# PREREG - is the 5.2 startup crash OURS (invocation/install) or the vendor's?

Date 2026-09-04. Tier HEAVY (it adjudicates a cause claim already in the record).
User challenge: "Unlikely that a mature product like VR-Forces would simply crash on its
own. Could it be an install issue?" The current record (FORENSICS_52_STARTUP_CRASH_
2026-09-04) calls it a vendor heap defect on the strength of register evidence plus the
falsification of every CONFIG discriminator tried so far (rid, launch method, gap,
cwd). That claim is now UNDER TEST, not assumed.

## 1. The lead the forensics itself supplies
The faulting routine is the NOTIFY/LOG STREAM INSTALLER: it deletes the current stream,
allocates a replacement std::ofstream from a filename member, and re-registers it with
the vlutil notify singletons gated on a 0-4 level. The two command-line options that
drive exactly that routine are `--logFileName <path>` and `--notifyLevel <0-4>`, and we
pass BOTH on every launch - `--logFileName` being an absolute ~150-character path into
the repo, which is NOT the vendor's own default (C:\MAK\logs, UG52 Table 11). If the
crash rate depends on those arguments, the cause is OUR invocation - ours to fix - and
the "vendor defect" label is wrong.

## 2. Arms (one variable each; same binary, env, cwd, scenario, machine state)
A (baseline, 6 launches): exactly today's profile invocation - `--siteId 1 --appNumber N
  --sessionId 1 --notifyLevel 3 --logFileName <repo runs\launch52 path> --scenarioFileName
  ../userData/scenarios/Sample/Traffic.scnx`.
C (6 launches): identical to A but WITHOUT `--logFileName` (the vendor then writes only
  its own C:\MAK\logs file - its default path).
Both -NoGui, RTI 5.0.1 rtiexec posture, rid-501-rtiexec-min, rtiexec 15720 already up.
Each launch is taken to READY-or-CRASH and then closed; a crash is identified by a new
C:\MAK\logs\vrfSimHLA1516e*-<pid>.callstack.log AND its faulting frame read.

## 3. Predictions (before running; the priors are the observed 3-in-9 = 33%)
P1: arm A reproduces the crash at roughly the historical rate (expect 1-3 of 6).
    Falsifier: 0 of 6 in A -> the crash is not reliably reproducible by invocation at
    all and this whole bisect is underpowered; STOP and say so rather than read noise.
P2 (THE ONE THAT MATTERS): if C crashes 0 of 6 while A crashes >= 2 of 6, the log-file
    option is implicated and the cause is OURS (invocation), NOT a vendor defect - the
    record must be relabelled and the runner must stop passing --logFileName (or pass a
    short/vendor-default path). If BOTH arms crash at similar rates, --logFileName is
    exonerated and the vendor-defect claim survives this test.
P3: every crash in either arm shows the SAME faulting frame (DtVrfSimOptions::
    parseCmdLine <- vl.dll). A DIFFERENT frame is a different defect - record separately.
NOTE ON POWER, stated before seeing the data: with 6 per arm and a ~33% rate, "0 of 6 vs
3 of 6" is suggestive (p ~ 0.09 by Fisher's exact, one-sided), NOT proof. A 0-vs->=2
split justifies a FOLLOW-UP arm (12 more), never a final cause claim on its own.

## 4. Procedure
App numbers ledgered as a block BEFORE any launch; every launch tee'd to
runs/launch52/bisect_<arm><n>_<appNo>.txt; crashed pids closed by LaunchVrf52 (it now
does this itself); rtiexec/forwarder never touched. Arms interleaved A,C,A,C,... so any
drift in machine state hits both arms equally.

## 5. Result (2026-09-04; runs/launch52/bisect_results.csv, bisect2_results.csv, appNos 3866-3895)
Round 1 (A vs C, 6 each, interleaved): A 3 CRASH / 6, C 0 CRASH / 6. Same faulting
instruction each time (vl.dll+0x...BA27, the ASLR-shifted +0x6BA27). Per sec 3 that
earned a follow-up, NOT a verdict.
Round 2 (A vs C vs D, 6 each, interleaved) with D = --logFileName pointing at the
VENDOR'S OWN short directory (C:\MAK\logs\bD<app>.log, 22 chars) to separate PATH from
OPTION: A 2/6, C 0/6, D 1/6.
POOLED: --logFileName PASSED = 6 crashes / 18 launches (33%, matching the historical
3-in-9); --logFileName OMITTED = 0 crashes / 12. Fisher's exact, one-sided, p = 0.031
(P(all 6 crashes fall in the 18-arm) = C(18,6)/C(30,6) = 18564/593775).
P1 HELD (A reproduced at 42%). P2 DECIDED: the crash is bound to the OPTION, not the
product's normal startup. P3 HELD (every crash the same frame).
PATH LENGTH / LOCATION FALSIFIED: arm D crashed with a 22-character path inside the
vendor's own log directory, so "our long absolute repo path" is NOT the trigger.
MECHANISM (consistent, not separately proven): the disassembly says the routine DELETES
the current notify/log stream and installs a replacement. Without --logFileName there is
no replacement and no delete; with it, the delete runs against an object whose member is
indeterminate ~1 time in 3. That is a genuine vendor defect IN THE --logFileName PATH -
but we trigger it, and we do not have to.
THE USER'S HYPOTHESIS (install issue) IS FALSIFIED as the cause, and their instinct was
right that the product does not crash "on its own": it crashes on an argument WE pass.
Install evidence gathered alongside (no bearing on this crash, recorded for hygiene):
vl/vlHLA1516e/vlutil/matrix/mtl are byte-identical between vrforces5.2d\bin64 and
vrlink5.10\bin64 (so both on PATH cannot mix them); the MAK-family DLLs unique to the
2022 stack on the Machine PATH are all HLA13/HLA1516 (non-Evolved) + VR-Vantage variants
that a 1516e process never loads; C:\MAK\vrvantageTOT2018-01-17 is referenced by a 5.2d
feature-source config but DOES NOT EXIST on this machine (the sim logs "Ignoring feature
source" and continues) - stale config, not a crash cause.
ACTION: stop passing --logFileName. The vendor always writes its own log to C:\MAK\logs;
the runner/launcher must HARVEST that file into the run directory instead (and it is the
file that carries the cleartext environment - see the secrets memory - so it is copied,
never attached anywhere). Retry-on-crash stays as a belt-and-braces guard.
Adversarial review: strongest competitor = "the crash is random and the split is luck".
Against it: 0/12 in the no-option arm against a 33% base rate, p = 0.031, arms
interleaved so machine drift hits both equally, identical env/cwd/scenario/detection
window, and every crash carrying the identical faulting frame. Not explained and NOT
swept: why the option's delete path fails only ~1 time in 3 - consistent with the
uninitialized-member reading of FORENSICS_52_STARTUP_CRASH, which this result does not
overturn, only re-scopes from "any launch" to "the --logFileName path".
