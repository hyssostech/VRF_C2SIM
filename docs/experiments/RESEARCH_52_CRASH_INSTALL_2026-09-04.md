# RESEARCH - is the 5.2d startup crash an INSTALL issue? (docs + community + machine audit)

Question (user, 2026-09-04): "a mature product would not simply crash on its own - could it be an
install issue?" Treats FORENSICS_52_STARTUP_CRASH_2026-09-04 sec 0 ("vendor heap defect") as the
hypothesis UNDER TEST. Read-only: nothing launched, nothing written under C:\MAK. Sources: the
5.2d/5.10/5.0.1 doc set under C:\MAK\...\doc, MAK's public KB (fetched 2026-09-04), registry / env /
filesystem reads, and a fresh byte-wise parse of all 5 minidumps in C:\MAK\logs. VERIFIED = an
artifact on this machine holds it. Doc short names, page = printed page: IG = MAK_ONE_Installation_
Guide.pdf (MAK-25.0-02-251010); UG52 = VRFUsersGuide.pdf; RN52 = VRF5.2ReleaseNotes.pdf
(VRF-5.2-01-251028); RTIUG = makRti5.0.1\doc\RTIUsersGuide.pdf.

## 1. What MAK actually requires of an install
(a) PATH. VERIFIED: no MAK document requires vrlink5.10\bin64, or ANY VR-Link directory, on the
    runtime PATH. The only PATH requirement in the set is the RTI - "The RTI dynamic libraries must
    be located somewhere on your dynamic library search path" (IG 4.2.1 p40; same text UG52 2.4.1
    p107). UG52 2.1.5 p99 names VR-Link 5.10 only as a BUILD prerequisite for Toolkit users, and
    vrforces5.2d\bin64 is self-sufficient (966 DLLs incl. its own vl/vlHLA1516e/vlutil/matrix/mtl).
    Our prefix is thus one entry wider than documented - undocumented, not a defect.
(b) Environment variables. VERIFIED, the complete documented set: MAK_INSTALL_DIR,
    MAK_SHARED_DATA_ROOT, MAK_SHARED_DATA_STEM, MAK_LOG_DIR, MAKLMGRD_LICENSE_FILE, RTI_CONFIG
    (IG index p43-44; text at 2.1.5 p13, 2.2.3 p18, 3.6.1 p28, 4.2.1 p40), plus RTI-side MAK_RTIDIR
    + RTI_RID_FILE + PATH ("The MAK RTI requires some environment variables: MAK_RTIDIR,
    RTI_RID_FILE, and LD_LIBRARY_PATH or PATH", RTIUG 2.1 p22; definitions 7.1.1 p68). MAK_VRFDIR,
    MAK_VRLDIR and VRF_HOME appear NOWHERE across UG52, RN52, IG, VRLinkGettingStartedGuide,
    VRL5.10ReleaseNotes, MAKInteroperabilityGuide, VRFMigrationGuide, RTIUsersGuide or
    RTIReferenceManual (grep, all texts), and nothing is documented as FORBIDDEN. This machine has
    MAK_VRFDIR=vrforces5.0.2 and MAK_VRLDIR=vrlink5.8 - previous generation, but undocumented vars
    that LaunchVrf52 overrides per process.
(c) VC++ redistributable. VERIFIED: MAK ships VC_redist.x64.exe + vcredist_x64_vc10/vc12.exe in the
    5.2d root but states no required version; RN52 p2 only says VC++ 14+ builds are binary
    compatible and to prefer one compiler across co-resident products. Installed: "Microsoft Visual
    C++ v14 Redistributable (x64) - 14.51.36247" (HKLM\...\VC\Runtimes\x64 Installed=1); the crash
    dumps load msvcp140/VCRUNTIME140 14.51.36247.0 from System32. Two non-causal oddities in sec 8.
(d) Side-by-side. VERIFIED: MAK SUPPORTS it - per-version directories under C:\MAK (IG 2.1.3 p11),
    data versioned by compatibility number so several sets coexist (IG 2.2.1 p15, 2.2.3 p17), RN52
    p2 only "strongly recommend[s]" a common compiler. The one hard rule is per-RUN: all federates
    in one federation execution use the same RTI (UG52 2.4 p106). No "do not mix" or ordering
    warning exists anywhere for 5.0.2 + 5.2d + two RTIs + a VR-Vantage.

## 2. Install integrity of THIS machine (all VERIFIED, read-only)
- vrforces5.2d is COMPLETE against UG52 Table 2 p103-104 (bin64, data, appData, plugins64, doc,
  appsrc, examples, factory, include, lib64, packages, translations, userData, Shortcuts; ./sfx
  absent). appData vs factory, 332 files hashed: 21 differ and 5 are missing, ALL under
  settings\vrfGui, vrfGuiKeyMappings or vrfLauncher - files the GUI/launcher rewrites. The SIM side
  is byte-identical to factory: vrfSim.mtl, channelSettings.mtl, vrEngage.mtl and
  plugins\scenarioPerformanceTest.xml all hash-match ./factory. The sim install is pristine.
- NO foreign MAK tree exists: C:\MAK\vrvantageTOT2018-01-17 DOES NOT EXIST and there is no
  vrvantage* directory at all. The log's "Ignoring feature source c:/MAK/vrvantageTOT2018-01-17/
  bin64/SmokeStackPlume" is a stale path in MAK-shipped effect data that the engine reports as
  IGNORED, in every healthy log (6 of 6) - not a discriminator.
- MACHINE PATH dirs holding MAK DLLs: makRti5.0.1\bin, vrforces5.0.2\bin64, vrlink5.8\bin64. USER
  PATH: makRti4.6.1\bin (TWICE). Plus one corrupt machine entry, "C:\Python312%". Every process
  inherits FOUR previous-generation MAK bin dirs; our prefix prepends, it does not remove. Not
  idle: dump 48944 (2026-09-03 21:12, the unrelated DI-Guy crash) shows a 5.2d vrfSimHLA1516e.exe
  with makRti4.6.1\bin\librti1516e64.dll and rtivlutil_64.dll LOADED.
- bin64 version audit, no stale drop-in: 60 DLLs at 5.2.282.607 (VR-Forces), 63 at 5.10.0.0 (VR-
  Link), 65 at 3.2.282.311 (VR-Vantage 3.2, same 282 train), 138 Qt 5.15, rest third-party.

## 3. The plugin angle - the one NEW mechanical finding
VERIFIED by parsing the ModuleListStream of every dump (not just stack-referenced frames): the three
crash dumps contain EXACTLY 16 modules each, in load order (bases are not sorted) - the exe,
ntdll/kernel32/KERNELBASE/ucrtbase, vrfcgf, vlutil, vlHLA1516e, vl, vrlinkNetworkInterfaceHLA1516e,
vrfutil, msvcp140, VCRUNTIME140, dbghelp, scenarioPerformanceTestPlugin.dll, dbgcore. Consequences:
1. H-wrong-DLL-on-PATH is falsified on the FULL module set, not a subset: at the fault NOTHING from
   vrforces5.0.2, vrlink5.8, makRti4.6.1 or makRti5.0.1 is mapped. Sec 2's PATH pollution cannot
   reach this crash.
2. scenarioPerformanceTestPlugin.dll is the LAST module loaded before the fault (dbgcore is written
   by the handler), so it is loaded INSIDE parseCmdLine - matching its own description in the
   healthy log, "Adds a string command line argument to the back-end command line processor and
   starts the DtScenarioPerformanceManager" (log 64364 line 5487). A plugin DLL thus mutates the
   option table in the very routine that then deletes and re-creates the log stream and dies.
Config context, VERIFIED: of 56 plugin descriptors in appData\plugins exactly ONE has myLoad=1 -
scenarioPerformanceTest.xml - and it is SHA-256-identical to factory\plugins, so it is MAK's shipped
default, not our misconfiguration. UG52 4.9 p156-158 documents plugin selection (Plug-ins Editor /
--loadPlugin) and warns only not to load the same plug-in twice; NO documented load order, and NO
plugin that must be disabled headless.

## 4. Community / vendor-public (searched 2026-09-04)
FOUND - MAK's public KB, "VR-Forces is Not Stable (The Ultimate Checklist)",
https://mak-tech.atlassian.net/wiki/spaces/KB/pages/90833014/ . Its causes, in substance: (i) run
the newest maintenance release; (ii) most GUI instability is graphics-driver / VR-Vantage; (iii) "If
you have any plugins, please try to run VR-Forces without the plugins."; (iv) "We have seen some
stability problems using a bad INTEL 13th and 14th generation CPUs ... unregulated voltage"; (v)
send .dmp/.callstack from C:\MAK\logs to the Support Portal. It says NOTHING about PATH, DLL
conflicts, VC++ redists, env variables or leftovers from other MAK versions. FOUND NOTHING, zero
relevant hits: "VR-Forces" + vrfSim + 0xC0000005 startup; "DtVrfSimOptions" parseCmdLine (only a
4.4.1 header listing on ftp.mak.com/out/classdocs); scenarioPerformanceTest Plugin /
DtScenarioPerformanceManager; any 5.2 known-issue or startup-crash page; any VR-Forces newer than
5.2 (no 5.2.1/5.3 notes exist publicly; mak.com/support/product-versions now only redirects); no
SISO/HLA thread. No public report of this signature, no vendor fix list for it.

## 5. Notify / log angle - no documented constraint is being violated
VERIFIED from UG52: --logFileName documented twice (vrfSim Table 11 p181, vrfGui Table 10 p170) with
NO rule on relative paths, pre-existing directories or length; --fileNotifyLevel p181; --notifyLevel
0..4 default 2 (p182, 5.4.3 p189); --enableChannel overrides channelSettings.mtl p181; 4.10 p162
says only that vrfSim.log/vrfGui.log go to C:/MAK/logs, relocatable via MAK_LOG_DIR (IG 2.1.5 p13).
No rotation, retention or reuse setting is documented anywhere - NO VR-Forces analogue of
RTI_reuseLogFile. C:\MAK\logs holds 30 files / 7.1 MB, each named <app><ver>-<date>-<time>-<host>-
<build>-<pid>.log so per-process collision is impossible; our path is far inside MAX_PATH.

## 6. Ranked hypotheses - INSTALL/CONFIG vs VENDOR DEFECT
H1 VENDOR DEFECT, HIGH. Indeterminate member read at vl.dll+0x6BA27 on the first install of the
   notify/log stream inside parseCmdLine. Cite: FORENSICS sec 3-4 + sec 3 above (16-module
   load-order list, zero log bytes written). FALSIFIER: an Rcx derivable from our inputs, a crash
   with a log already open, or a rate that moves with a config knob. TEST: 20 launches logging Rcx.
H2 INSTALL/CONFIG, MEDIUM - the best install-shaped candidate and the vendor's own first move.
   scenarioPerformanceTestPlugin, the single default-on plugin, is loaded inside parseCmdLine and
   registers a command-line argument there; disabling it removes a DLL and a mutation from the
   exact routine that faults. Cite: KB item (iii); log 64364 line 5487; myLoad=1; UG52 4.9.
   FALSIFIER: the crash recurs with the plugin off. TEST (CHEAPEST): set myLoad to 0 in
   appData\plugins\scenarioPerformanceTest.xml, hold everything else fixed, run the same 20.
H3 INSTALL/CONFIG, LOW-MED. Heap-layout perturbation - env-block size, PATH width, plugin set -
   shifting the odds of the H1 flip rather than causing it. Cite: FORENSICS sec 7.2. FALSIFIER: a
   parameter that moves the rate off ~1/3. TEST: the same 20 at --notifyLevel 0 vs 3, plus one run
   with the legacy MAK bin dirs stripped from the child PATH (which also closes H4).
H4 INSTALL/CONFIG, LOW. Legacy MAK generations on the inherited PATH (5.0.2, 5.8, 4.6.1 twice, plus
   "C:\Python312%"). Cite: sec 2; dump 48944 proves a 5.2d sim CAN bind makRti4.6.1. ALREADY
   FALSIFIED here by the full module list (sec 3.1) but a real hazard for later phases' RTI
   binding. TEST: none - just clean the PATH.
H5 HARDWARE, LOW - vendor-named, argued down. This machine IS a 13th Gen Intel Core i9-13900HX
   (Lenovo 82WR, 24C/32T, 64 GB), the family the KB blames. BUT Intel's root-cause statement says
   13th/14th Gen MOBILE parts are unaffected by Vmin Shift Instability (community.intel.com, "Intel
   Core 13th and 14th Gen Desktop Instability Root Cause Update"); microcode here is 0x0000012E,
   past the final 0x12B mitigation; BIOS KWCN54WW 2025-10-21; zero WHEA-Logger events in 30 days;
   and Vmin shift would scatter the crash site, whereas ours is one RVA with a byte-identical
   register set every time. FALSIFIER: a second crash signature under load, or a WHEA entry.
NOT worth testing (falsified): rid variants, --deviceAddress, launcher method, restart spacing, VC++
redist state, missing/foreign MAK trees, log-path shape.

## 7. Plain answer to the user
The evidence does NOT support "install issue", and for most of the install surface it is not close.
The 5.2d sim-side install is byte-identical to MAK's factory copy, the tree is complete, no foreign
or stale MAK DLL exists on disk, the VC++ runtime is current, side-by-side installs are explicitly
supported by MAK, and - decisively - the FULL 16-module load-order list of all three crashed
processes holds nothing but 5.2d bin64 binaries, the Windows CRT and the one default MAK plugin.
Every "wrong DLL / wrong version / leftover install" story is falsified AT the fault, not argued
away. The install is nevertheless UNTIDY in ways worth fixing on their own merits (four legacy MAK
bin dirs plus a corrupt entry on the inherited PATH; MAK_VRFDIR/MAK_VRLDIR naming 5.0.2/5.8), and
dump 48944 shows that untidiness CAN bind a 5.2d sim to a 4.6.1 RTI. The one genuinely open lead is
CONFIGURATION, not installation: MAK's checklist says to try without plugins and this build ships
exactly one, loaded inside the faulting routine - run H2 before filing with MAK.

## 8. Adversarial review
Competing hypotheses weighed. (1) The crash is OURS via the plugin: the descriptor hashes identical
to factory so we did not enable it, but "shipped default" is not "innocent" - H2 stays open and
untested. (2) FORENSICS sec 3 listed only stack-referenced modules, so a foreign DLL could have
hidden behind them: a real gap, now closed by the full ModuleListStream parse (16 = complete); the
answer did not change. (3) The CPU is the exact family MAK names, which would have been a
hardware/install answer - argued down on Intel's mobile carve-out, microcode 0x12E, no WHEA events
and the single-RVA signature, so recorded as H5 rather than dismissed. Unexplained residue: (i) the
plugin is present in BOTH outcomes, so it is a candidate mechanism, never a discriminator; (ii) 5
factory files absent from appData under vrfGui*/vrfLauncher, presumed rewritten by the new 5.2
launcher (RN52 p3) but not proven; (iii) System32 lacks vcruntime140_1.dll though the 14.51 x64
redist registers as installed, and bin64 ships concrt140.dll 14.26 beside a
14.51 System32 set - both shown non-causal (neither is loaded in any crash) rather than understood.
