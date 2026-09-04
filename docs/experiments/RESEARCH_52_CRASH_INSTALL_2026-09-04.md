# RESEARCH - is the 5.2d startup crash an INSTALL issue? (docs, community, machine audit)

Question (user, 2026-09-04): "Unlikely that a mature product like VR-Forces would simply crash on
its own. Could it be an install issue?" This is the DOCUMENTARY + MACHINE-AUDIT lane; it ran in
parallel with the empirical lane, which has since CLOSED the question - read sec 0 first, then use
secs 1-5 as reference material. Two claims in this file's first revision were wrong: corrected in
sec 3, logged in sec 8. Read-only: nothing launched, no process touched, nothing written under
C:\MAK. Sources: the 5.2d / 5.10 / 5.0.1 doc set under C:\MAK\...\doc; MAK's public KB and web
search (2026-09-04); registry / environment / filesystem reads; byte-wise parses of the 5 minidumps
in C:\MAK\logs and of PE import tables in bin64. VERIFIED = an artifact on this machine holds it.
Doc short names (page = printed page): UG52 = VRFUsersGuide.pdf, RN52 = VRF5.2ReleaseNotes.pdf,
RTIUG = makRti5.0.1\doc\RTIUsersGuide.pdf, IG = MAK_ONE_Installation_Guide.pdf.

## 0. SETTLED - this lane did not decide it; the bisect did
PREREG_52_CRASH_BISECT_2026-09-04 sec 5-6, 42 interleaved launches: --logFileName PASSED = 11
crashes / 30 (37%); OMITTED = 0 / 12; Fisher exact one-sided p = 0.0128. Arm D (a 22-char path in
the vendor's own C:\MAK\logs) still crashed, so path length and location are falsified too - it is
the OPTION. The scenarioPerformanceTest plugin lead this lane raised was tested in round 3 and
EXONERATED (2/6 with the record, 3/6 without). FORENSICS_52_MODULE_PROVENANCE_2026-09-04 sec 6
re-established no-mixing on a sound warrant: of 392 import names in the sim's transitive closure
only 8 are PATH-reachable, all MAK RTI resolving to 5.0.1. So the install is EXONERATED as the
cause; the fix (stop passing --logFileName, harvest the vendor's own log) is already implemented.
Secs 1-5 below are the documentary backing for why the install was never a plausible cause.

## 1. What MAK actually requires of an install (documentation)
(a) PATH. VERIFIED: no MAK document requires vrlink5.10\bin64, or ANY VR-Link directory, on the
    runtime PATH. The only PATH requirement in the whole set is the RTI - "The RTI dynamic libraries
    must be located somewhere on your dynamic library search path" (IG 4.2.1 p40 = UG52 2.4.1 p107).
    UG52 2.1.5 p99 names VR-Link 5.10 only as a BUILD prerequisite for Toolkit users, and
    vrforces5.2d\bin64 is self-sufficient (966 DLLs, its own vl / vlHLA1516e / vlutil / matrix).
(b) Environment variables. VERIFIED, the complete documented set is MAK_INSTALL_DIR,
    MAK_SHARED_DATA_ROOT, MAK_SHARED_DATA_STEM, MAK_LOG_DIR, MAKLMGRD_LICENSE_FILE, RTI_CONFIG (IG
    index p43-44; text at 2.1.5 p13, 2.2.3 p18, 3.6.1 p28, 4.2.1 p40) plus RTI-side MAK_RTIDIR +
    RTI_RID_FILE + PATH (RTIUG 2.1 p22, definitions 7.1.1 p68). MAK_VRFDIR, MAK_VRLDIR and VRF_HOME
    appear NOWHERE across the ten UG52 / RN52 / IG / VR-Link / RTI texts (grep), and nothing is
    FORBIDDEN. This machine sets MAK_VRFDIR=vrforces5.0.2, MAK_VRLDIR=vrlink5.8 - previous
    generation, but undocumented vars that LaunchVrf52 overrides per process.
(c) VC++ redistributable. VERIFIED: MAK ships VC_redist.x64.exe + vcredist_x64_vc10/vc12.exe in the
    5.2d root but states no required version; RN52 p2 says only that VC++ 14+ builds are binary
    compatible and co-resident products should prefer one compiler. Installed: "v14 Redistributable
    (x64) - 14.51.36247" (HKLM\...\VC\Runtimes\x64 Installed=1), and every dump loads msvcp140 /
    VCRUNTIME140 14.51.36247.0 from System32. Provenance sec 5 adjudicates the v141 + v143 mix as
    MAK's own composition, expressly supported by Microsoft.
(d) Side-by-side. VERIFIED: MAK SUPPORTS it - per-version directories under C:\MAK (IG 2.1.3 p11),
    data versioned so several sets coexist (IG 2.2.1 p15), RN52 p2 only "strongly recommend[s]" a
    common compiler. The one hard rule is per-RUN: all federates in one federation execution must
    use the same RTI (UG52 2.4 p106). No "do not mix" or ordering warning exists for this box.

## 2. Install integrity of THIS machine (all VERIFIED, read-only)
- vrforces5.2d is COMPLETE against UG52 Table 2 p103-104 (bin64, data, appData, plugins64, doc,
  appsrc, examples, factory, include, lib64, packages, translations, userData, Shortcuts; ./sfx
  absent). NEW HERE: appData hashed against ./factory, 332 files - 21 differ and 5 are missing, ALL
  under settings\vrfGui, vrfGuiKeyMappings or vrfLauncher, i.e. files the GUI and launcher rewrite.
  The SIM side is byte-identical to factory (vrfSim.mtl, channelSettings.mtl, vrEngage.mtl,
  plugins\scenarioPerformanceTest.xml): nobody edited the sim's shipped configuration.
- Plugin configuration: of 56 descriptors in appData\plugins exactly ONE has myLoad=1 -
  scenarioPerformanceTest.xml - SHA-256-identical to factory\plugins: MAK's shipped default, not us.
- NO foreign MAK tree exists: C:\MAK\vrvantageTOT2018-01-17 DOES NOT EXIST, nor any vrvantage* dir.
  The log's "Ignoring feature source .../vrvantageTOT2018-01-17/bin64/SmokeStackPlume" is a stale
  path in MAK-shipped effect data that the engine reports as IGNORED, in every healthy log checked
  (6 of 6) - not a discriminator.
- MACHINE PATH dirs holding MAK DLLs: makRti5.0.1\bin, vrforces5.0.2\bin64, vrlink5.8\bin64; USER
  PATH: makRti4.6.1\bin, listed TWICE; plus one corrupt Machine entry, "C:\Python312%". Our prefix
  prepends, it does not remove - harmless for the sim (its own exe directory wins), see provenance
  sec 6 for where it is NOT (our own C# exe, which does not live in bin64).
- bin64 version audit, no stale drop-in: 60 DLLs at 5.2.282.607 (VR-Forces), 63 at 5.10.0.0
  (VR-Link), 65 at 3.2.282.311 (VR-Vantage 3.2, same 282 train), 138 Qt 5.15, rest third-party.

## 3. CORRECTION - the minidump module lists are FILTERED
This file's first revision claimed the crash dumps' 16-module lists were COMPLETE, and built on that
a claim that scenarioPerformanceTestPlugin.dll was "the last module loaded before the fault". Both
are WRONG: these are MiniDumpNormal-weight dumps and dbghelp prunes the module list to modules
referenced by the captured thread stacks (FORENSICS_52_MODULE_PROVENANCE sec 1). INDEPENDENTLY
RE-VERIFIED here by parsing PE import tables: vrfSimHLA1516e.exe statically imports readerWriter,
vrfobjcore, vrfSimCore, vrfNavigation, vrfMsgTransport and vrfExtObjectsHLA1516e, and vrfcgf.dll 22
further MAK DLLs - none of which appear in any crashed dump. A static import is mapped before the
entry point runs and the fault is inside main(), so >=13 loaded modules are missing from the record.
The error was the WARRANT, not the conclusion: no-mixing is re-established in provenance sec 6, and
the plugin was then tested and exonerated (sec 0).

## 4. Community / vendor-public (searched 2026-09-04)
FOUND - MAK's public knowledge base, "VR-Forces is Not Stable (The Ultimate Checklist)" at
https://mak-tech.atlassian.net/wiki/spaces/KB/pages/90833014/ . Its causes, in substance: (i) run
the newest maintenance release; (ii) most GUI instability is graphics-driver / VR-Vantage; (iii) "If
you have any plugins, please try to run VR-Forces without the plugins."; (iv) "We have seen some
stability problems using a bad INTEL 13th and 14th generation CPUs ... unregulated voltage"; (v)
send .dmp / .callstack to the Support Portal. It says NOTHING about PATH, DLL conflicts, VC++
redists, env variables or leftovers from other MAK versions - the whole install surface the user
suspected is absent from the vendor's own checklist. Item (iii) became this lane's plugin
hypothesis; round 3 falsified it. Item (iv) is R5.
FOUND NOTHING, zero relevant hits: "VR-Forces" + vrfSim + 0xC0000005 startup; "DtVrfSimOptions"
parseCmdLine (only a 4.4.1 header listing on ftp.mak.com/out/classdocs); scenarioPerformanceTest
Plugin / DtScenarioPerformanceManager; any 5.2 known-issue or startup-crash page; any VR-Forces
newer than 5.2 (no 5.2.1 / 5.3 notes public); no SISO or HLA thread. No public report of this
signature exists and no vendor fix list covers it.

## 5. The notify / log documentation - we violate no documented constraint
This matters now that --logFileName is the known trigger: the option is documented, ordinary, and
carries no caveat we ignored. VERIFIED from UG52: --logFileName appears twice (vrfSim Table 11 p181,
vrfGui Table 10 p170) with NO rule about relative paths, pre-existing directories or length;
--fileNotifyLevel p181; --notifyLevel 0..4 default 2 (p182, 5.4.3 p189); --enableChannel overrides
channelSettings.mtl p181; 4.10 p162 says only that vrfSim.log / vrfGui.log go to C:/MAK/logs,
relocatable via MAK_LOG_DIR (IG 2.1.5 p13). No rotation, retention or reuse setting is documented -
there is NO VR-Forces analogue of RTI_reuseLogFile. C:\MAK\logs holds 30 files / 7.1 MB, each named
<app><ver>-<date>-<time>-<host>-<build>-<pid>.log, so per-process collision is impossible, and our
path is far inside MAX_PATH. CONCLUSION: --logFileName is documented usage, so the fault it triggers
is a VENDOR DEFECT IN A DOCUMENTED OPTION, not misuse - file the case on that footing.

## 6. What is left open, and the cheapest way to close each
R1 The defect (vl.dll+0x6BA27, indeterminate member in the notify/log-stream installer) is unfixed
   and undocumented by MAK. CLOSE BY: file the case with the 3 .callstack.log + .dmp, the bisect
   table and sec 5. WARNING, unchanged: do NOT attach a healthy vendor .log - at notifyLevel 3 it
   dumps the environment, AZURE_CLIENT_SECRET and App__AzureKey in cleartext. Dumps carry no log.
R2 Why the delete path fails only ~1 in 3 is unexplained (PREREG sec 6). CLOSE BY: MAK's to explain.
R3 PATH hygiene: four legacy MAK bin dirs plus "C:\Python312%" inherited, MAK_VRFDIR / MAK_VRLDIR
   naming 5.0.2 / 5.8. Not a cause (sec 0) but provenance sec 6 makes it a live hazard for our own
   C# exe. CLOSE BY: clean the PATH.
R4 No complete loaded-module snapshot has ever been taken (sec 3: minidumps cannot supply one).
   CLOSE BY: Process Monitor, Operation = Load Image, one healthy launch, ~2 min.
R5 HARDWARE, argued down but recorded. This machine IS a 13th Gen Intel Core i9-13900HX (Lenovo
   82WR, 24C/32T, 64 GB) - the family MAK's KB item (iv) blames. Against it: Intel states 13th/14th
   Gen MOBILE parts are unaffected by Vmin Shift Instability (community.intel.com, "Intel Core 13th
   and 14th Gen Desktop Instability Root Cause Update"); microcode 0x0000012E, past the final 0x12B
   mitigation; BIOS KWCN54WW 2025-10-21; zero WHEA-Logger events in 30 days; and decisively, Vmin
   shift would scatter the crash site whereas this crash vanishes when one option is dropped.
   REINSTATE ONLY IF a second crash signature appears, or a WHEA entry is logged.

## 7. Plain answer to the user
No - it is not an install issue, and the machine audit says so on every axis independently of the
bisect: the sim-side install is byte-identical to MAK's factory copy, the tree is complete, no
foreign or stale MAK tree exists on disk, the VC++ runtime is current and the toolset mix is MAK's
own, side-by-side installs are supported, and only 8 of 392 imports are even PATH-reachable (all MAK
RTI, all landing on 5.0.1). But the other half of the instinct was right: a mature product does not
crash on its own, and this one does not - it crashes on --logFileName, an option WE pass and the
vendor documents without caveat (11 crashes / 30 launches with it, 0 / 12 without). The install is
untidy in ways worth fixing on their own merits (R3); none causes this.

## 8. Adversarial review and corrections log
CORRECTED THIS REVISION: (1) "the 16-module dump lists are complete" - FALSIFIED by my own PE import
parse (sec 3); the lists are MiniDumpNormal-filtered. (2) "the plugin is the last module loaded
before the fault, and the best remaining install-shaped lead" - the ordering inference died with (1)
and the lead was then tested and exonerated at 2/6 vs 3/6 (sec 0). (3) "the one genuinely open lead
is configuration" - superseded; the trigger is identified.
LESSON: the fatal step was inferring completeness from a dump stream documented to be filtered, then
reading load ORDER out of it. A minidump's module list is evidence about what was RECORDED, never
about what was loaded; the disproof cost one PE import parse and belonged before the claim.
STILL UNEXPLAINED, not swept: the ~1-in-3 rate within the --logFileName arm (R2); 5 factory files
absent from appData under vrfGui* / vrfLauncher, presumed rewritten by the new 5.2 launcher (RN52
p3) but not proven; System32 lacks vcruntime140_1.dll although the 14.51 x64 redist registers as
installed, and bin64 ships concrt140.dll 14.26 beside a 14.51 System32 set - shown non-causal.
