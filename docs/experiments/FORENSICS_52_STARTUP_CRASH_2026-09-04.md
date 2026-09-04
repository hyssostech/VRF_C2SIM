# FORENSICS - the 5.2d sim startup crash (0xC0000005 in DtVrfSimOptions::parseCmdLine)

Read-only, 2026-09-04: nothing launched, no process touched, nothing written under C:\MAK. Sources:
C:\MAK\logs *.callstack.log + *.dmp (parsed byte-wise), every launch's vendor log, runs/launch52
captures, PREREG_52_RTIEXEC / _APP_SMOKE / COLDSTART_REVIEW_RTIEXEC, UG52 + the VRF5.2 and VRL5.10
release notes (fitz), dumpbin (VS18) on vl.dll. VERIFIED = an artifact holds it.

## 0. VERDICT
VERIFIED: all three startup crashes are the SAME instruction - `mov rax,[rcx]` at vl.dll+0x6BA27 -
loading the vtable of an object vl.dll is about to `delete` through a virtual deleting destructor.
The pointer passes a non-null test and is then garbage: a DIFFERENT value each time. INFERRED
(strong): an indeterminate (never-assigned) member, benign whenever the recycled heap block holds 0,
since the null guard then skips the delete. A vendor defect in VR-Link/VR-Forces code, NOT a property
of our rid, argv, launcher, cwd or timing (all falsified, sec 5). Undocumented by MAK.

## 1. Launch table - 9 sim launches on the 5.2d stack, 3 crashes (1 in 3)
Survivors' pid/rid/argv VERIFIED from each log's argv echo + DtPrintEnvironmentVariables; crashed rows
have no log (sec 2), rid INFERRED from PREREG_52_RTIEXEC sec 4 / COLDSTART_REVIEW F3. "gap" = s from
the previous sim's last log write to this process start.

| app  | pid   | local start | gap    | launcher              | rid                          | out |
|------|-------|-------------|--------|-----------------------|------------------------------|-----|
| 3844 | 41352 | 09-03 21:37 | n/a    | LaunchVrf52           | makRti5.0.1\rid.mtl (assist) | ok  |
| 3848 | 38180 | 09-03 21:49 | 2.3 s  | LaunchVrf52           | rid-501-rtiexec.mtl          |CRASH|
| 3851 | 11648 | 09-03 21:54 | 292 s  | LaunchVrf52           | rid-501-ridconfigured-notify4| ok  |
| 3852 | 40548 | 09-03 21:55 | 63 s   | LaunchVrf52, -q       | rid-501-ridconfigured-notify4| ok  |
| 3853 | 39028 | 09-03 21:57 | 103 s  | ProcessStartInfo,pipes| rid-501-ridconfigured-notify4|CRASH|
| 3854 | 59296 | 09-04 06:25 | 8.5 h  | LaunchVrf52,-DeviceAdr| rid-501-rtiexec-min.mtl      | ok  |
| 3858 | 44664 | 09-04 07:15 | 1279 s | LaunchVrf52           | rid-501-rtiexec-min.mtl      | ok  |
| 3860 | 59936 | 09-04 07:28 | 761 s  | LaunchVrf52           | rid-501-rtiexec-min.mtl      |CRASH|
| 3864 | 64364 | 09-04 07:30 | ~90 s  | LaunchVrf52 (retry)   | rid-501-rtiexec-min.mtl      | ok  |

Constant on every row: cwd C:\MAK\vrforces5.2d\bin64, --siteId 1 --sessionId 1 --notifyLevel 3
--logFileName <repo>\runs\launch52\..., -NoGui, no vrfGui alive (last vrfGui log ends 21:29),
rtiexec 15720 up from 21:48. NO DISCRIMINATOR SURVIVES: three rids crash and the same three also
succeed; both launcher methods do both; gaps interleave (crashes 2.3/103/761 s, successes
63/292/1279 s/8.5 h). Hygiene: launch_3848_rtiexec.txt and launch_3860_appsmoke.txt named in the
brief DO NOT EXIST - only launch_3816.txt survives (same Tee gap as review F4).

## 2. What the logs say (VERIFIED)
- The three crashed pids produced NO vendor log anywhere: death precedes any log stream opening.
- vrfSim_3853_console_rtitrace.txt is the only console capture of a crash, in full: RDTSCP probe,
  "Loading Config File: ..\appData\settings\vrfSim\vrfSim.mtl", ".../vrEngage.mtl", then the callstack.
  A healthy log's next lines are "Using relative timestamps" / "Current thread priority is 15" /
  "Placing log file output to C:\MAK\logs\...". No RTI banner precedes any crash.
- A FOURTH callstack is a DIFFERENT defect, do not conflate: ...-48944 = app 3826, DtDiGuyController
  ::determineInitialHandItem(504) <- preFirstTickInit <- tick, first-tick DI-Guy on a live sim.

## 3. Minidump analysis (no cdb/windbg on this machine - dumps parsed directly)
`where cdb`, Windows Kits\10\Debuggers (no cdb.exe), Program Files\Windows Kits and VS2022 are all
absent; only dumpbin (VS18) exists, so the dumps were parsed field-by-field in Python. All VERIFIED:

| pid   | thread | RIP            | reported fault addr | Rcx (the bad pointer) |
|-------|--------|----------------|---------------------|-----------------------|
| 38180 | 42856  | 0x7FF8BD00BA27 | 0xFFFFFFFFFFFFFFFF  | 0x0FFF0F67AC4FBCCD    |
| 39028 | 41792  | 0x7FF8BD00BA27 | 0xFFFFFFFFFFFFFFFF  | 0x14007A47E5835837    |
| 59936 | 12964  | 0x7FF8BD00BA27 | 0x0000000100010000  | 0x0000000100010000    |

RIP = C:\MAK\vrforces5.2d\bin64\vl.dll + 0x6BA27 in all three (bases differ, the RVA does not). Code
0xC0000005, ExceptionInformation[0]=0 (READ). Rax=0, Rsi=0, R8=0x7FFFFFFFFFFFFFFC, R12=0x68, R13=0,
R15=0xD are BYTE-IDENTICAL across the three: same call path every time, only the data differs. In
38180/39028 Rcx is non-canonical (bits 63:48 = 0x0FFF / 0x1400) so the CPU raises #GP and Windows
reports the address as -1; in 59936 Rcx is canonical but unmapped so the reported address IS Rcx -
the two different "access violation" addresses are one fault on one operand. CPU time at death = 0 s.
Modules at the fault (16, stack-referenced): vrfSimHLA1516e.exe, vrlinkNetworkInterfaceHLA1516e.dll,
vl / vlHLA1516e / vlutil / vrfutil / vrfcgf.dll + CRT/ntdll - every MAK module from vrforces5.2d\bin64,
none from vrlink5.10\bin64 or the 5.0.2/5.8 stack, no RTI or Qt yet: the 5.2 PATH trap is NOT present.

## 4. The faulting code (dumpbin /DISASM of vl.dll; .pdata gives the bounds)
Function = vl.dll RVA 0x6B950..0x6BB83 (563 bytes, not exported; nearest export 0x656FD below, so no
name is recoverable). Shape, from the disassembly:

    0x6B987  cmp byte [rcx+95Ch],0 / je end          ; "if (!enabled) return"
    0x6B9B2  mov rdx,[rbx+2250h] / test rdx,rdx / je  ; the member under test
    0x6B9BE  5x: mov rcx,<vlutil import slot i>/call  ; de-register(slot_i, member)
    0x6BA1B  mov rcx,[rbx+2250h] / test rcx,rcx / je
    0x6BA27  mov rax,qword ptr [rcx]   <== FAULT      ; load vtable
    0x6BA2A  mov edx,1 / call qword ptr [rax]         ; slot 0, flags=1 = deleting dtor
    0x6BA31  mov [rbx+2250h],rsi(=0) / mov ecx,1B8h / call <operator new>  ; ctor from [rbx+950h]
    0x6BAA6  4x: <int at [rbx+818h]> >= 1,2,3,4 ? re-register with slot_i

VERIFIED reading: the routine REPLACES a polymorphic member at this+0x2250 - delete the old one,
allocate a 0x1B8-byte successor, re-attach it to five vlutil singletons behind a 0..4 threshold.
INFERRED (strong): the notify/log-stream installer - 0..4 is exactly UG52's --notifyLevel range (5
levels, 5 singletons), 0x1B8 matches MSVC sizeof(std::ofstream), and the crash lands where a healthy
run prints "Placing log file output to ...". As the crashed processes produced no log file at all,
this is the FIRST invocation in the process: this+0x2250 was never assigned by it - indeterminate
memory, not a pointer this routine left dangling.

## 5. Falsification
- H-rid / H-argv / H-cwd / H-logpath / H-redirected-stdio: FALSIFIED. Three rids crash and the same
  three succeed; cwd and --logFileName are identical on all nine launches; the fault operand is not
  derivable from any argument; 38180 and 59936 were plain Start-Process, not redirected pipes.
- H-restart-race / previous federate's RTI timeout / leftover lock: FALSIFIED by the gap column
  (2.3 s crashes, 63 s succeeds; 761 s crashes, 1279 s succeeds) and by sec 3 (no RTI module).
- H-wrong-DLL-on-PATH: FALSIFIED here by the dump module list (all 5.2d bin64).
- H-null-deref-in-option-parsing: FALSIFIED - the code null-checks twice and the pointer is non-null
  garbage: a memory-initialisation defect, not a missing-option defect.
- H-use-after-free: NOT SUPPORTED - first invocation, and the member is nulled right after delete.
- SURVIVOR: indeterminate member read - it alone explains three operands at one instruction, ~1 in 3
  incidence, the identical callee-saved set, and why nothing predicts which launch loses the flip.

## 6. Vendor documentation (searched, nothing found)
VRF5.2ReleaseNotes.pdf (84 pp): 40+ crash fixes, none in parseCmdLine, option parsing, logging or
startup; VRF-9173 (crash on cancelling the RTI connection window) is the only startup item and does
not apply (assistant-free, no dialog). VRL5.10ReleaseNotes.pdf (8 pp): no hit at all. UG52 idx
169/180 document --logFileName with no caveat; idx 97-98 say symbolisation needs "the correct PDB
installer" - and vrlink5.10\bin64 has ZERO .pdb while bin64's 449 PDBs include no vl.pdb /
vlHLA1516e.pdb / vlutil.pdb, which is why the innermost frames print "Unknown".

## 7. Ranked triggers, falsifier and cheapest test each (none run here)
1. INDETERMINATE MEMBER at this+0x2250 read before assignment (HIGH; sec 4/5). Falsifier: an Rcx
   derivable from our inputs, or a crash with a log already open. Test: 20 launches, log Rcx (20 min).
2. A HEAP-LAYOUT-PERTURBING FACTOR shifting the odds - plugin set, env block size (LOW-MED; refines
   1). Falsifier: a parameter moving the rate off ~1/3. Test: the same 20 at --notifyLevel 0 vs 3.
3. VENDOR BUILD MISMATCH inside the 5.2d tree (LOW). Falsifier: mismatched build stamps among bin64
   vl*.dll. Cheapest test: dumpbin /HEADERS across bin64 vl*.dll, 2 min, read-only.
NOT worth testing: rid variants, --deviceAddress, launcher method, restart spacing - all falsified.

## 8. Is a one-retry policy in the runner justified?
YES, bounded and loud. It masks no configuration defect: sec 5 removes every configuration variable
from the causal chain and sec 4 puts the fault inside vendor code that runs identically on every
launch, so a retry is the right response to a per-process coin flip. Conditions LaunchVrf52 lacks:
(a) CLOSE THE CRASHED PID FIRST. MAK's handler leaves the process alive behind its Error box; that
    lingering pid made launch 3862 fail the pre-existing-process precondition with exit 2 and burned
    3860-3863 (PREREG_52_APP_SMOKE sec 4). This is the concrete defect the retry must fix.
(b) A FRESH ledgered app number for the retry; never reuse the crashed one.
(c) RETRY ONLY THIS SIGNATURE - parseCmdLine in the callstack. The 48944 DI-Guy tick crash and any
    other callstack must still fail loudly at exit 3; a blanket retry WOULD mask real defects.
(d) EXACTLY ONE retry, and append every crash to a counter file so the rate stays visible.
(e) It is a mitigation, not a fix: it must not close the MAK support case.

## 9. Actions
1. Report to MAK support (UG52 idx 98): the three .callstack.log + .dmp for 38180 / 39028 / 59936
   plus sec 3-4 (RVA 0x6BA27, the three Rcx values, the #GP explanation). WARNING: do NOT attach a
   healthy vendor .log - DtPrintEnvironmentVariables at notifyLevel 3 dumps the whole environment and
   ours holds AZURE_CLIENT_SECRET and App__AzureKey in cleartext (the crash dumps carry no log).
2. Install the MAK PDB set for VR-Link 5.10 / the vl* modules so future callstacks name these frames.
3. Implement 8(a)-(e) in LaunchVrf52.ps1 and the runner profile.
4. Fix the runs/launch52 Tee gap: launch captures for 3848, 3854 and 3860 were never written.
