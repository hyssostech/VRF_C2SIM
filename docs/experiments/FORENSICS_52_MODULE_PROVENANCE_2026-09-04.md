# FORENSICS - module provenance of the 5.2d sim startup crash
Read-only, 2026-09-04: nothing launched, nothing written under C:\MAK. Is the 1-in-3 startup crash
(FORENSICS_52_STARTUP_CRASH_2026-09-04) a vendor defect or an ABI mismatch from a mis-install? Sources: the four
C:\MAK\logs\*.dmp parsed field-by-field in Python (no debugger EXE on this box - Windows Kits\10\Debuggers holds only
dbghelp/dbgcore/srcsrv/symsrv), dumpbin (VS18 14.51.36231), SHA-256 + PE headers on disk, runs/launch52 logs,
Machine/User PATH, MS binary-compat doc. VERIFIED = an artifact holds it; the register-level analysis is not
revisited.
## 0. VERDICT
The recorded module set is a CLEAN SINGLE-VERSION 5.2d load - no 5.0.2, 5.8, 4.6b, TOT2018, user or temp module, every
file byte-identical to disk. But those lists are FILTERED (sec 1), so they never COULD have shown a stray module: the
prior doc's "H-wrong-DLL-on-PATH: FALSIFIED here by the dump module list" was not carried by its evidence. Sec 6
re-establishes it independently: of 392 import names in the sim's whole transitive closure only 8 (all MAK RTI) are
PATH-reachable, and all 8 land on makRti5.0.1 under Machine PATH and the launcher prefix alike. NO MIXING AND NO ABI
HAZARD IS DEMONSTRABLE - verdict (i), vendor defect, stands. One real mixed-toolset fact exists (VR-Link v141 and
VR-Forces v143 objects in one process, sec 5) but it is MAK's own shipped combination, expressly supported by
Microsoft. NEW: all three crashed processes had a plugin loaded and stack-referenced whose job is to add a
command-line argument to the very option processor that faults (sec 4).
## 1. Method, and the caveat that changes the prior finding (VERIFIED)
These are MiniDumpNormal-weight dumps: dbghelp prunes the module list to modules REFERENCED BY the captured thread
stacks. Proof by contradiction, not assumption: vrfSimHLA1516e.exe statically imports 31 DLLs, of which matrix, mtl,
readerWriter, vrfobjcore, vrfSimCore, vrfNavigation, vrfMsgTransport, vrfExtObjectsHLA1516e, TracyClient,
librti1516e64, VCRUNTIME140_1, WINMM and USER32 appear in NO crashed dump - yet a static import is mapped before the
entry point runs and the fault is inside main(). So >=13 loaded modules are missing. Second proof: dump 48944 lists
osgEarth.dll but none of the osg*.dll it imports. ThreadListStream sizes confirm the weight: 244 bytes = 5 threads
(crashed), 3364 = 70 (48944). So "all-5.2d DLLs" is true OF WHAT WAS RECORDED only.
## 2. Complete ModuleListStream, per crashed pid (VERIFIED)
All three dumps record 16 modules and the SAME 16 paths - the set is identical across 38180 / 39028 / 59936 (bases and
order differ, sec 4). ts = PE TimeDateStamp UTC; fv from VS_FIXEDFILEINFO.

| module (full path)                                                     | size   | ts         | fv          |
|------------------------------------------------------------------------|--------|------------|-------------|
| C:\MAK\vrforces5.2d\bin64\vrfSimHLA1516e.exe                           | F1000  | 2026-01-06 | (none)      |
| C:\MAK\vrforces5.2d\bin64\vl.dll                                       | 279000 | 2025-10-16 | 5.10.0.0    |
| C:\MAK\vrforces5.2d\bin64\vlutil.dll                                   | 341000 | 2025-10-16 | 5.10.0.0    |
| C:\MAK\vrforces5.2d\bin64\vlHLA1516e.dll                               | 819000 | 2025-10-16 | 5.10.0.0    |
| C:\MAK\vrforces5.2d\bin64\vrlinkNetworkInterfaceHLA1516e.dll           | 400000 | 2026-01-06 | 5.2.282.607 |
| C:\MAK\vrforces5.2d\bin64\vrfutil.dll                                  | 915000 | 2026-01-06 | 5.2.282.607 |
| C:\MAK\vrforces5.2d\bin64\vrfcgf.dll                                   | 30E000 | 2026-01-06 | 5.2.282.607 |
| C:\MAK\vrforces5.2d\plugins64\release\scenarioPerformanceTestPlugin.dll | 9A000  | 2026-01-06 | 5.2.282.607 |

The remaining 8 are C:\Windows\System32: ntdll, kernel32, KERNELBASE, ucrtbase, dbghelp, dbgcore (6.2.26100.x, OS)
plus msvcp140.dll (9D000) and VCRUNTIME140.dll (2C000), both 14.51.36247.0 redist (sec 5). OUTSIDE the allowed dirs:
exactly ONE entry, benign - scenarioPerformanceTestPlugin.dll, a sibling of bin64 in the same 5.2d tree, same
5.2.282.607 stamp. NOTHING from vrforces5.0.2, vrlink5.8, makRti4.6b, vrvantageTOT2018-01-17 (absent from this
machine), or any user/temp path. CONTRAST dump 48944 (DI-Guy defect, 52 modules) DOES cross versions: it loads
makRti4.6.1\bin\librti1516e64.dll and rtivlutil_64.dll into a 5.2d process. That run (21:12, app 3826) predates the
21:48 switch to RTI 5.0.1 so it was intended - but it proves the mixing this lane hunted is possible here, just absent
from the three crashes.
## 3. Cross-check against the files now on disk (VERIFIED)
All 52 distinct module paths across the four dumps were re-read and their PE TimeDateStamp and SizeOfImage compared
with the dump fields: 52 of 52 MATCH - nothing was replaced, patched or rebuilt since, and the dumps loaded the files
we assume. SHA-256 of the MAK modules in the crashed set:
  98b85bf02ba3c3950ea553c6bbd9db04c8459121733a9144c332309d3758f54a  vrfSimHLA1516e.exe
  4d8a6a2b485dfcb8178c1c52fbbeaf675f7ac98172a6b9dfcb9cf06a58e0152b  vl.dll
  50600848523ee8f606260d52139176b626149fd004bdfa4e325ef9cde3cbf59b  vlutil.dll
  ff8d80809ab9cb38c1d8344a3507c3f02abdbc2388beff75abaf48a15e7088cc  vlHLA1516e.dll
  2e11096c04dbfcb4c64290833ef03bdce758ecdbcb8a142320b58decaa1f72f7  vrlinkNetworkInterfaceHLA1516e
  8a223d19fe6b33fe0cd05f864fb35ca0dcd55454e7be5c2fc7ad2108804ab23c  vrfutil.dll
  f28cfd87203ae1a75842082e03b5fe6c2a22283dedbb443f9432fcdd7da7f082  vrfcgf.dll
  8451abd7b9b365630404c144dd36489e96136aefd5d045bedc8adc31aba38a0c  scenarioPerformanceTestPlugin
INSTALL INTEGRITY: vrlink5.10\bin64 and vrforces5.2d\bin64 share 234 DLL names and 233 are BYTE-IDENTICAL (only
iconv_64.dll differs, 955392 vs 901632 bytes, not in the sim's closure), so the bundled VR-Link 5.10 set is the
genuine 5.10 build - not a half-overwritten install.
## 4. Load order, plugin timing, and the healthy runs
VERIFIED: the module SET is identical across the three crashes; the ORDER dbghelp wrote is not - 38180 = vrfcgf,
vlutil, vlHLA1516e, vl, vrlinkNI, vrfutil; 39028 = vlHLA1516e, vl, vlutil, vrlinkNI, vrfcgf, vrfutil; 59936 a third
permutation, bases differing too (ASLR). INFERRED: Windows 10+ parallel loader worker threads map a static dependency
graph in non-deterministic order - a per-process layout perturbation that varies exactly as the crash does. HEALTHY
RUNS name no DLL paths in their logs except plugins; the comparable evidence is the plugin lines, identical in all
five (3851, 3852, 3854, 3858, 3864): "Loaded plugin ..\plugins64\release\scenarioPerformanceTestPlugin.dll" (~5485),
"Loaded plugin in POST INIT" (~5510), vantageTerrainImplementationPlugin.dll (~5534). Those lines are late in the log,
but the DLL is loaded EARLY: it is present and stack-referenced in all three crashed dumps, which produced no log at
all. Coherent and load-bearing - VR-Forces LoadLibrary's back-end plugins BEFORE DtVrfSimOptions::parseCmdLine,
because this plugin's own banner says it "adds a string command line argument to the back-end command line processor".
Its static initialisers have run and registered into the option processor before the faulting routine executes.
Enabled by appData\plugins\scenarioPerformanceTest.xml; the only back-end plugin either path loads, so there is no
plugin difference crashed vs healthy.
## 5. The C++ runtime and the ABI question (VERIFIED, then adjudicated)
Exactly ONE msvcp140.dll and ONE VCRUNTIME140.dll are in the process, both C:\Windows\System32, both 14.51.36247.0
(dated 2026-05-27 - installed by the VS18 redist, not by MAK), over ucrtbase 10.0.26100.8875. No second CRT copy, no
msvcp120/141 variant, no static CRT: every module checked imports MSVCP140.dll + VCRUNTIME140.dll dynamically
(v143-built ones also VCRUNTIME140_1.dll). vrforces5.0.2\bin64 ships its own DIFFERENT msvcp140/vcruntime140; neither
was loaded. Linker versions (dumpbin /HEADERS): vl, vlutil, vlHLA1516e = 14.16 (v141, VS2017 15.9) = VR-Link 5.10;
vrfSimHLA1516e.exe, vrfcgf, vrfutil, vrlinkNI, plugin = 14.44 (v143, VS2022 17.14) = VRF 5.2d; makRti5.0.1
librti1516e64.dll = 14.16 (the makRti4.6.1 file: 14.00, v140). So the process DOES mix v141 and v143 objects - the ABI
hypothesis has a real foothold. It does not survive. Microsoft states binaries built with v140, v141, v142, v143 and
v145 build tools can be combined provided the installed Redistributable is >= the build tools used; here 14.51 >=
14.44 >= 14.16, and STL type layouts are frozen across that family, so sizeof(std::ofstream) is the same for the 14.16
and 14.44 headers. Further, the fault is a virtual DELETING destructor: allocation and deallocation of that object
both occur inside vl.dll through vl.dll's own CRT imports, so no cross-module new/delete pairing is in play at the
fault. The composition is MAK's shipped product - their own installers put the 14.16 vl*.dll and the 14.44 vrf*.dll in
one bin64.
## 6. PATH collisions and reachability (this replaces the prior doc's sec-5 warrant)
Basename collisions, SHA-256-compared, across the stack dirs:
  vrforces5.2d\bin64 vs vrforces5.0.2\bin64 : 588 shared names, 303 DIFFERENT bytes
  vrforces5.2d\bin64 vs vrlink5.8\bin64     : 197 shared,        60 DIFFERENT
  vrforces5.2d\bin64 vs vrlink5.10\bin64    : 234 shared,         1 DIFFERENT (iconv_64.dll)
  makRti5.0.1\bin    vs makRti4.6.1\bin     :  39 shared,        39 DIFFERENT (4.6.1 == 4.6b exactly)
Colliding names that were loaded include vl, vlutil, vlHLA1516e, vrfutil, vrfcgf, vrlinkNI, readerWriter, msvcp140,
vcruntime140 and the plugin - each with a version-inconsistent 5.0.2/5.8 twin on the Machine PATH. WOULD A PLAIN PATH
SEARCH HAVE FOUND A DIFFERENT FILE FIRST? No. Windows searches the EXE's own directory before any PATH entry, and the
sim's exe and cwd are both vrforces5.2d\bin64. The transitive static+delay import closure of vrfSimHLA1516e.exe was
computed (388 modules, 392 distinct names) and resolved in true search order: 128 resolve from bin64, 251 from
System32/Windows, 5 are Windows-side optional DLLs present nowhere, and exactly 8 resolve via PATH - all MAK RTI
(librti1516e64, libfedtime1516e64, rtiutil_64, rtimtl_64, rtivlutil_64, rtimatrix_64, rtiomtReader_64, assistant_64).
Those 8 are the ONLY PATH exposure and they land on makRti5.0.1 either way: the Machine PATH's FIRST entry is
C:\MAK\makRti5.0.1\bin and LaunchVrf52 prefixes it again (lines 466/537). Runtime plugin loads are PATH-immune too -
the log shows explicit relative paths. FRAGILITY WORTH FIXING (not a cause here): the healthy log's env block shows
the process PATH ending at ...\OpenSSH with vrforces5.0.2\bin64, vrlink5.8\bin64 and makRti4.6.1\bin ABSENT - not by
design but because LaunchVrf52 does `$env:PATH = $prefix + $env:PATH` and the parent shell's PATH was already
truncated. From a normal shell those three return: harmless for the sim (exe dir wins), NOT harmless for our own C#
interface exe, which does not live in bin64.
## 7. Verdict, falsifier, next test
VERDICT (i): clean single-version load - the vendor-defect finding stands. One consistent 5.2d / VR-Link-5.10 /
RTI-5.0.1 stack, every file matching its installer bytes, the only version mixing being MAK's own v141+v143
composition that Microsoft explicitly supports, and no 5.0.2/5.8/4.6.x file reachable by the loader. Mis-install NOT
SUPPORTED; the prior doc's error was the WARRANT. SINGLE FALSIFYING OBSERVATION: a complete loaded-module snapshot of
a 5.2d sim - Process Explorer, Process Monitor Load Image events, or a full-memory dump - showing ANY module loaded
from vrforces5.0.2\bin64, vrlink5.8\bin64, makRti4.6.1\bin or makRti4.6b\bin. The four minidumps here CANNOT produce
that observation (sec 1), so it is untaken, not negative. NEXT (cheapest first, none run here): (1) take that snapshot
on the next healthy launch (Process Monitor, Operation=Load Image, ~2 min) - closes sec 1's gap and would catch a
4.6.1 librti sneaking in. (2) Single-variable plugin test: move appData\plugins\scenarioPerformanceTest.xml aside, run
20 launches; this plugin registers a command-line argument into the processor that faults and was stack-referenced in
all three crashes - the only in-process actor this lane newly exposed. ~1/3 clears it; 0 reopens the diagnosis. (3)
Ask MAK whether 5.2d is qualified against a 14.51 (VS2026) redistributable - supported per the compat rule but newer
than anything MAK tested; one sentence in the support case.
