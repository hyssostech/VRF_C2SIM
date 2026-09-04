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
AUDIT 2026-09-04 (cold-start, adversarial - recomputed from the CSVs and the vendor logs):
ARITHMETIC EXACT (11/30, 0/12; p = 0.012762 -> 0.0128; round-2 p = 0.03126 -> 0.031). ARMS
CONFIRMED IDENTICAL AND INTERLEAVED from the vendor's OWN echoed command lines, not our CSVs.
P3 STRENGTHENED BEYOND WHAT WE CLAIMED: all ELEVEN .callstack.log files share one caller chain
(vl.dll <- vlHLA1516e.dll <- DtVrfSimOptions::parseCmdLine(768) <- DtVrfApp::init(632) <-
main(107)); rounds 2-3 had no frame column, so our "every crash the same frame" was unevidenced
for 8 of 11 until the audit read them. Also confirmed: no C-arm pid has a callstack log and all
12 C-arm launches wrote full-size vendor logs - the 0/12 is 12 real launches, not 12 no-ops.
TWO OVERSTATEMENTS CORRECTED: (1) "NEVER PASS" is the right OPERATING rule but 0/12 does not
show the option is NECESSARY - the exact 95% upper bound on the no-option crash rate is
1 - 0.05^(1/12) ~ 22%. (2) OPTION COUNT IS CONFOUNDED WITH THE OPTION: 5 options 0/12, 6 options
6/18, 7 options 5/12. Round 3 added --appDataDir to BOTH arms and so never ran the one cell that
separates them - `--appDataDir` WITHOUT `--logFileName`, a 6-launch arm. Until that runs, the
supported claim is "bound to --logFileName OR to one more option in that position".
ACTION: stop passing --logFileName. The vendor always writes its own log to C:\MAK\logs;
the runner/launcher must HARVEST that file into the run directory instead (and it is the
file that carries the cleartext environment - see the secrets memory - so it is copied,
never attached anywhere). Retry-on-crash stays as a belt-and-braces guard.
## 6. ROUND 3 (registered 2026-09-04 BEFORE running, after FORENSICS_52_MODULE_PROVENANCE)
NEW EVIDENCE: scenarioPerformanceTestPlugin.dll is loaded AND stack-referenced in all
three crashed dumps; its own banner says it "adds a string command line argument to the
back-end command line processor" - the very processor that faults
(DtVrfSimOptions::parseCmdLine). It is enabled by one config record,
appData\plugins\scenarioPerformanceTest.xml. That is a CONFIG/INSTALL artifact: a
performance-TEST plugin has no business in a headless production run.
CONSISTENCY CHECK (done first, so this is not a fresh guess): the plugin loads on healthy
launches too (the vendor log prints "Unloading plugin ...scenarioPerformanceTestPlugin"),
so the plugin ALONE cannot be sufficient - rounds 1-2 showed 0/12 crashes without
--logFileName while the plugin was loaded every time. The live hypothesis is therefore an
INTERACTION: the plugin registers into the option processor, and the --logFileName path
then replaces/deletes a stream in that processor.
ARMS (12 launches, --logFileName ALWAYS passed, interleaved, one appData copy in the
scratchpad so nothing under C:\MAK is written; --appDataDir is the documented redirect,
UG52 5.4.4 p190):
  A2 = --appDataDir <copy WITH scenarioPerformanceTest.xml>   x6  (controls for the
       redirect itself: it must still crash, or the redirect is a confound)
  E  = --appDataDir <copy WITHOUT that one record>            x6
P4: if E crashes 0/6 while A2 crashes >= 2/6, the plugin is the necessary co-factor:
    the fix is to stop loading a test plugin (config hygiene, OURS), and --logFileName
    becomes usable again. Falsifier: E crashes at A2's rate -> the plugin is exonerated
    and the round-2 conclusion (do not pass --logFileName) stands unchanged.
P5: A2 must crash at roughly the round-1/2 rate. If A2 is 0/6, --appDataDir itself
    changed the outcome and the round is VOID - report that, do not read E.
RESULT (round 3, 2026-09-04; runs/launch52/bisect3_results.csv, appNos 3896-3907):
A2 (plugin record PRESENT) 2 CRASH / 6; E (record REMOVED) 3 CRASH / 6.
P5 HELD: A2 crashed at the round-1/2 rate, so --appDataDir is not a confound and the
round is valid. P4 FALSIFIED: the plugin is NOT the co-factor - removing it did not
reduce the crash rate (it was, if anything, higher). THE PLUGIN IS EXONERATED; the
"performance-test plugin has no business in a headless run" observation stands only as
config hygiene, not as a cause.
The round nevertheless CONFIRMS round 2 on a third independent batch, because both arms
passed --logFileName and both crashed at ~40%.
POOLED OVER ALL THREE ROUNDS (42 launches):
  --logFileName PASSED  : 11 crashes / 30  (37%)  [A 5/12, D 1/6, A2 2/6, E 3/6]
  --logFileName OMITTED :  0 crashes / 12
  Fisher's exact one-sided p = C(30,11)/C(42,11) = 54,627,300 / 4,280,561,376 = 0.0128.
INDEPENDENT CORROBORATION of the mechanism, found by the harvest executor while reading
C:\MAK\logs: of the 10 pids that have a .callstack.log, NINE have a .dmp + .callstack.log
but NO vendor .log at all - exactly what a fault INSIDE the log-stream installer
predicts, and evidence nobody produced to support the claim (it was found while building
the harvest).

Adversarial review: strongest competitor = "the crash is random and the split is luck".
Against it: 0/12 in the no-option arm against a 33% base rate, p = 0.031, arms
interleaved so machine drift hits both equally, identical env/cwd/scenario/detection
window, and every crash carrying the identical faulting frame. Not explained and NOT
swept: why the option's delete path fails only ~1 time in 3 - consistent with the
uninitialized-member reading of FORENSICS_52_STARTUP_CRASH, which this result does not
overturn, only re-scopes from "any launch" to "the --logFileName path".
