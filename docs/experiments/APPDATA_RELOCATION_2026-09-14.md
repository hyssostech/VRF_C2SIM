# appData relocation (Opus executor, 2026-09-14 ~13:45Z; lane L1/G8, Jira STP-792). Supervisor notes: sanctioned by the
# user ("Ok on 2-3"); nothing under C:\MAK modified; C:\C2SIM\vrf-appdata\appData is a byte copy of the vendor tree
# with cache/data/userData junctioned back (the 1.43 GB terrain cache FOLLOWS --appDataDir, so a plain copy would have
# started cold) and ONE change, loadAllNavigationDataOnTerrainLoad 1. Launcher -AppDataDir committed with this record;
# the runner passthrough (-VrfAppDataDir) is applied separately. Caveat carried: --appDataDir shares the option-parsing
# path with --logFileName (the ~1-in-3 startup crash) - if the validation launch dies at startup, drop it once first.

# appData relocation for VR-Forces 5.2 - eager navigation-data load

Date: 2026-09-14. Tier: STANDARD (reversible; default path unchanged; no live launch).
Scope: one relocated appData tree, one vendor setting, one optional launcher parameter.

## 0. Problem

Run `20260914T130439Z`: members were created at wall second 25; the sim logged
`New Primary nav area` at wall second 201 - about 175 s later. Units tasked at
creation therefore planned their routes with no navigation mesh loaded, which is
the plausible mechanism behind route plans that ignore the sectorised nav area
built by `vrfNavGenerator`.

This is the sim's designed behaviour, not a misconfiguration: nav data is loaded
lazily when an entity is placed in (or moves into) a navigation area. The vendor
exposes one switch that turns that off.

## 1. Citations (all read, all local)

### 1.1 The setting

`C:\MAK\vrforces5.2d\appData\settings\vrfSim\vrfSim.mtl:433-436` (vendor text):

```
;; If set to true (1), navigation data will be loaded with the scenario.
;; If false (0), default behavior will be used,  and navigation data will be loaded when an entity is placed.
;; Default: false (0)
(setqb loadAllNavigationDataOnTerrainLoad 0)
```

**UG52 Appendix C, Table 76 `vrfSim.mtl` parameters, p1671**, entry
`loadAllNavigationDataOnTerrainLoad`:

> "Loads all of the navigation data when the scenario loads, rather than waiting
> until an entity needs to use that data (either when it is placed in a
> navigation area or moves into one). If set to 1, navigation loads the data at
> the same time you load or create a scenario. Please be aware this may
> negatively impact performance."

The performance warning is the known cost and is accepted deliberately: the run
we care about already pays 175 s of lazy loading, just at the wrong moment.

### 1.2 Why lazy loading is the 5.2 norm

**VR-Forces 5.2 Release Notes, Table 1 Fixed Bugs, VRF-9225**
("Major VRF sim performance penalty for remote entities inside navigation areas"):

> "The application no longer loads navigation areas when remote entities are in
> them, only when local entities are present in the area. This improves sim
> engine performance."

So 5.2 deliberately narrowed - not removed - lazy nav-area loading. The eager
setting is the documented opt-out, and it is the only one.

### 1.3 `--appDataDir`

**UG52 Table 11, "vrfSim command-line options", p178**:

> `--appDataDir directory` - "Specifies the location of application data. If not
> specified, VR-Forces uses the default: ./appData."

**UG52 Table 10, "vrfGui command-line options", p164**: identical option and
identical wording, plus a separate `--appCacheDir directory` for the GUI cache.

**UG52 Table 11 p180** also documents `--dataDir directory` ("Specifies the
location of the application's data directory. If not specified, VR-Forces uses
the default, ./data") and p184 `--userDataDir directory` ("Specifies the location
of the user data directory for the sim engine"). Neither is used here - see 3.3.

**UG52 5.4.4 "Loading Plug-ins", p187** (the UG's own index entry reads
`--appDataDir, 164, 178, 187, 1306`):

> "If your plug-ins are not in the ./appData directory of the VR-Forces
> installation, you can use the --appDataDir command-line option to point to the
> directory where your plug-ins are located"

- i.e. `appData\plugins` follows `--appDataDir`, so a settings-only copy would
silently drop the plug-in set. Full copy required.

**VR-Forces 5.2 Release Notes, VRF-9265**:

> Summary: "--appDataDir is not processed until after attempting to load config
> files and many instances of hard-coded relative paths"
> Release note: "Fixed to ensure that the data files are loaded correctly when
> using this command-line option." Fix version VRF-5.2.

**VR-Forces 5.2 Release Notes, VRF-9255**:

> "VRF Launcher does not respect updated appData paths for plug-in selection" ->
> "The VR-Forces Launcher now supports the --appDataDir command-line option."
> Fix version VRF-5.2.

(The same two fixes appear on the VR-Vantage side as VRV-6320 and VRV-6326, the
latter specifically about `resolveDots` in `DtVrfFilesystem.cxx` mangling a valid
Windows path under `--appDataDir`. Relevant only as a reminder to pass a plain
drive-letter path with no `..` segments, which we do.)

Net: 5.2 is the first release in which `--appDataDir` is trustworthy; on 5.0.2 it
was not, which is why the existing scripts never used it.

### 1.4 How relative paths inside those files resolve

**UG52 12.2.1 "Scenario Parameters", p352**:

> "Paths in the scenario files are relative to the application directory, for
> example, ./bin64."

**UG52 12.2.2 p355**: "When VR-Forces saves a new scenario, it creates pathnames
that are relative to the application directory... change the paths to be relative
to the ./bin64 directory."

`vrfSim.mtl:202-204` says the same about its own setting, in the vendor's words:
"Default simulation model set. The path is relative from the application start
directory" - then `(setqb simulationModelSet "../data/simulationModelSets/EntityLevel.sms")`.

There are exactly four LIVE upward-relative references in the settings tree that
matter (everything else with `../` is inside comments - `featureconfig.txt` and
the commented defaults in `terrainInterfaceConfig.mtl`):

| file:line | reference | sibling needed |
|---|---|---|
| `settings\vrfSim\vrfSim.mtl:204` | `../data/simulationModelSets/EntityLevel.sms` | `data` |
| `settings\vrfSim\vrfSim.mtl:210` | `../appData/settings/vrfSim/character_animations_table.mtl` | `appData` |
| `settings\vrfSim\vrfSim.mtl:213` | `../appData/settings/vrfSim/msdl.hierarchy.csv` | `appData` |
| `settings\vrfSim\vrfSim.mtl:448` | `(load "../appData/settings/vrfSim/vrEngage.mtl")` | `appData` |

`settings\vrfGui\default_SimulationSettings.sisx:16,25` adds `../data/...`, and
`settings\vrfGui\backups\Application.backup:6` adds `../userData/...`.

The vendor ALSO uses macros in the same tree - `$(APP_DIR)`, `$(SHARED_DATA_DIR)`,
`$(USER_DIR)`, `$(USER_DATA)` - resolved by `makVrf::DtAppPathResolver`
(`include\vrfutil\appPathResolver.h`, methods `setDataPath` / `setAppDataPath` /
`setUserDataPath` / `resolvePath`). `include\vrfGuiCore\vrfGuiCommandLineParser.inl:1103-1105`
shows `--appDataDir` feeding exactly that singleton:

```cpp
if (vrfArgs.appDataDir->isSet())
   DtAppPathResolver::Instance().setAppDataPath(vrfArgs.appDataDir->getValue().c_str());
```

`settings\vrfSim\terrainInterfaceConfig.mtl:244` documents the sim cache default
as `$(APP_DIR)/cache/vrfsim`, and `:247` the feature config as
`$(APP_DIR)/settings/featureconfig.txt` - both children of appData, which pins
`$(APP_DIR)` to the appData directory, not to `bin64`.

## 2. Layout decision

**Decided: FULL copy of appData (cache excluded), nested one level down, with
three directory junctions.**

```
C:\C2SIM\vrf-appdata\
    README-C2SIM.txt      provenance, diff, reinstall procedure
    appData\              <-- the --appDataDir value; 699 files, 33.28 MB
        cache\            JUNCTION -> C:\MAK\vrforces5.2d\appData\cache
        settings\ cdb\ drivers\ drivers-legacy\ gdal_data\ importConfig\
        logs\ osgEarth\ plugins\ proj_lib\ qt\ schema\ videoStreams\
    data      JUNCTION -> C:\MAK\vrforces5.2d\data
    userData  JUNCTION -> C:\MAK\vrforces5.2d\userData
```

### 2.1 Full copy, not settings-only

`--appDataDir` replaces the whole appData root, not just its `settings` child.
UG52 5.4.4 p187 says plug-ins are found there; `plugins\` holds 57 files. `schema\`,
`gdal_data\`, `proj_lib\`, `osgEarth\`, `cdb\`, `importConfig\` are all resolved
from the same root. A settings-only copy would be a silent partial install.
Cost of the full copy is trivial once `cache` is excluded: 33.28 MB.

### 2.2 `cache` is a junction, not a copy

`appData\cache` is 1,433.6 MB across 1,154,756 files, and it is generated, not
authored. `terrainInterfaceConfig.mtl:244` shows the sim cache follows
`$(APP_DIR)`, so a relocated appData with an empty `cache` would make the sim
re-fetch and re-tile the whole streamed terrain on the first run. That is both
slow and a measurement confound for any comparison against earlier timed runs -
exactly the trap the "compare at equal sim time" lesson warns about.

The junction keeps the warm cache and keeps cache writes landing where they land
today (`C:\MAK\vrforces5.2d\appData\cache`). This is not a modification of the
vendor tree: it is the sim continuing to write its own cache to its own default
location.

Residual: a VR-Forces reinstall that deletes the vendor cache leaves the junction
dangling. README step 2 covers it.

### 2.3 Nested `appData\` plus `data` / `userData` junctions - and whether it is needed

Needed as insurance, cheap enough not to argue about.

Under the documented reading (UG52 12.2.1 p352) those `../data` and `../appData`
strings resolve against the application directory, which `LaunchVrf52.ps1` keeps
at `C:\MAK\vrforces5.2d\bin64` (`-WorkingDirectory $bin64`, line 656/660 in the
pre-change file). Under that reading a flat `C:\C2SIM\vrf-appdata` used directly
as the appData root would work fine and no junction would be needed.

But VRF-9265 says in so many words that `--appDataDir` was previously "not
processed until after attempting to load config files and many instances of
hard-coded relative paths", and that this was FIXED in 5.2. MAK does not document
which base each of those now-fixed resolutions uses. Rather than guess, the nested
layout makes both readings resolve:

| reference | base = `bin64` (documented) | base = appData dir (post-VRF-9265 possibility) |
|---|---|---|
| `../data/...` | `C:\MAK\vrforces5.2d\data` | `C:\C2SIM\vrf-appdata\data` -> same tree (junction) |
| `../appData/...` | `C:\MAK\vrforces5.2d\appData` (vendor original) | `C:\C2SIM\vrf-appdata\appData` (this copy) |
| `../userData/...` | `C:\MAK\vrforces5.2d\userData` | `C:\C2SIM\vrf-appdata\userData` -> same tree (junction) |

Every cell resolves to a real, correct tree. The question never has to be answered.

Directory junctions need no elevation on Windows and are created with
`mklink /J`; all three were created and verified (`LinkType = Junction`, targets
reachable: `appData\cache\vrfsim`, `data\simulationModelSets\EntityLevel.sms`,
`userData\scenarios`).

### 2.4 Not done: `--dataDir`, `--userDataDir`

Both exist (UG52 Table 11 p180 and p184) and both are deliberately NOT passed.
`data\` and `userData\` are unmodified vendor content; relocating them would buy
nothing and would move the scenario tree that `-Scenario` resolves against
(`../userData/scenarios/<name>.scnx`, launcher line 414). The junctions in 2.3
cover the only reason they might have been needed.

## 3. What was built and changed

### 3.1 The tree

- Source `C:\MAK\vrforces5.2d\appData` (vendor files dated 2026-01-05).
- `robocopy /E /XD <src>\cache /R:1 /W:1` -> 699 files, 33.28 MB, 0 failures.
  (699 = 1,155,455 total files minus 1,154,756 cache files - the copy is complete.)
- Three junctions as above.
- `C:\C2SIM\vrf-appdata\README-C2SIM.txt` written: source path, date, the diff,
  the reinstall procedure (re-copy, re-junction, re-apply, re-verify), and the
  runtime confirmation check. 9,090 bytes, CRLF, ASCII-clean.

### 3.2 The one setting change

`C:\C2SIM\vrf-appdata\appData\settings\vrfSim\vrfSim.mtl`, line 436:

```diff
--- C:\MAK\vrforces5.2d\appData\settings\vrfSim\vrfSim.mtl
+++ C:\C2SIM\vrf-appdata\appData\settings\vrfSim\vrfSim.mtl
@@ lines 433-436 @@
     ;; If set to true (1), navigation data will be loaded with the scenario.
     ;; If false (0), default behavior will be used,  and navigation data will be loaded when an entity is placed.
     ;; Default: false (0)
-    (setqb loadAllNavigationDataOnTerrainLoad 0)
+    (setqb loadAllNavigationDataOnTerrainLoad 1)
```

Verified byte-exact: 448 lines both sides, `Compare-Object -SyncWindow 0` returns
exactly two entries (one `=>`, one `<=`), 21,000 bytes / 447 CR / 447 LF in both
files, 0 non-ASCII or stray control bytes.

**Trap found the hard way (worth a memory line):** MSYS/Git-Bash `sed -i` and
`perl -i` run in TEXT mode on this box. `sed -i` on the CRLF `vrfSim.mtl` silently
rewrote all 447 line endings to LF - a 447-line diff instead of a 1-line diff.
The file was restored from the vendor copy and the edit redone with
`[IO.File]::ReadAllText` / `WriteAllText` and an explicit `\r\n` in the match
string, with an assertion that the old text occurs exactly once. Note that
`sed -n ... | cat -A` does NOT reveal this: the same text-mode translation strips
the CR on read, so the control looked clean. Only a byte count exposed it.

### 3.3 The launcher change

`scripts\LaunchVrf52.ps1`, +50 / -1 lines, five sites:

| file:line (post-change) | what |
|---|---|
| `167-186` | new `[string] $AppDataDir = ''` parameter with the citation block (Table 11 p178 / Table 10 p164, VRF-9225/9255/9265, the p1671 setting, and why `cache\` is junctioned) |
| `429-433` | `$connDir` now follows `-AppDataDir` when given (`<appData>\settings\connections`), so the exercise-connection precondition checks the file the sim will actually open, not the vendor copy. Unchanged when the parameter is absent |
| `480-497` | precondition: hard-fails (`$hardFail` -> exit 2) if `-AppDataDir` is not an existing directory; on success echoes the path AND the `loadAllNavigationDataOnTerrainLoad` line from the tree, so the run log records which way it was set; warns if `settings\vrfSim\vrfSim.mtl` is missing (wrong nesting level); when absent, reports the vendor default explicitly |
| `606-608` and `612` | appends `--appDataDir "<path>"` to `$simArgs` (608) and to `$guiArgs` (612) - both executables document the option |
| `625-626` | Plan block prints an `appData :` line either way |

Default behaviour is unchanged: with `-AppDataDir` absent no option is added and
the emitted command line is byte-identical to before (verified, case B below).

Gates: `[Parser]::ParseFile` -> 0 errors. 64,427 bytes, 852 CR / 852 LF (CRLF
convention preserved, +49 lines), 0 non-ASCII or stray control bytes. The ASCII
checker was validated FIRST on a known-dirty control (NBSP + en dash + ZWSP + BOM
+ VT + BEL -> 13 offending bytes) before being trusted on the real files.

### 3.4 Dry-run evidence (nothing launched; `-DryRun` exits 0 before `Start-Process`)

| case | command | result |
|---|---|---|
| A | `-DryRun -NoGui -BackendAppNumber 3999 -AppDataDir 'C:\C2SIM\vrf-appdata\appData'` | exit 0; back-end line ends `--appDataDir "C:\C2SIM\vrf-appdata\appData"`; precondition echoed `(setqb loadAllNavigationDataOnTerrainLoad 1)`; connection config resolved to the relocated tree and exists |
| B | same, no `-AppDataDir` | exit 0; back-end line `--siteId 1 --appNumber 3999 --sessionId 1 --notifyLevel 3` - identical to pre-change; connection config back at `C:\MAK\vrforces5.2d\appData\settings\connections\...` |
| C | with GUI + `-AppDataDir` | exit 0; front-end line ends `--hla1516e --appDataDir "C:\C2SIM\vrf-appdata\appData"` |
| D | `-AppDataDir` pointing at a non-existent directory | exit 2, `[FAIL] -AppDataDir is MISSING or is not a directory` |
| E | `-AppDataDir C:\C2SIM\vrf-appdata` (parent, the easy mistake) | exit 2 via the connection-config check, plus the targeted `[WARN] ... has no settings\vrfSim\vrfSim.mtl - pass the directory that CONTAINS settings\` |

## 4. Runner passthrough still needed (NOT applied - another executor owns the file)

`scripts\RunC2SimScenario.ps1` is being edited by another executor, so this is a
specification, not a patch. Three edits, all additive, all 5.2-only:

**(a) New parameter.** In the param block, next to `-DeviceAddress` (which ends at
line 280 in the pre-existing file) and before the
`# VR-Forces bring-up (passed straight through to the profile's launch script).`
comment at line 282:

```powershell
    # 5.2 ONLY: relocated appData for the sim and the gui (LaunchVrf52.ps1 -AppDataDir ->
    # --appDataDir on both, UG52 Table 11 p178 / Table 10 p164). EMPTY (default) = nothing is
    # passed and VR-Forces uses $VrfRoot\appData, so every existing command line is unchanged.
    # 'C:\C2SIM\vrf-appdata\appData' is the reinstall-safe copy whose only delta from the
    # vendor tree is (setqb loadAllNavigationDataOnTerrainLoad 1) - navigation data loaded
    # WITH the scenario instead of lazily at first entity placement (UG52 App. C p1671);
    # see that tree's README-C2SIM.txt. Refused on 5.0.2 (LaunchVrf.ps1 has no such option).
    [string] $VrfAppDataDir = '',
```

**(b) Profile guard.** In the 5.0.2-refusal block alongside the existing `-NoGui`
and `-DeviceAddress` messages (lines 1586 and 1589):

```powershell
    if ($VrfAppDataDir -and -not $Is52) {
        $bad += '-VrfAppDataDir is a 5.2 profile switch (LaunchVrf52.ps1 -AppDataDir). The 5.0.2 combined-mode launcher takes appData from the installation; use -VrfProfile 5.2 or drop -VrfAppDataDir.'
    }
```

**(c) Passthrough.** One line inside the existing `if ($Is52) { ... }` block at
lines 2492-2497, next to the `-DeviceAddress` line, using the same
pass-only-when-non-empty convention so the launch line in the evidence says
plainly when nothing was relocated:

```powershell
        if ($VrfAppDataDir) { $launchArgs += @('-AppDataDir', $VrfAppDataDir) }
```

**(d) Manifest.** Beside `$Manifest.inputs.quietBackend` / `.backendNotifyLevel`
(lines 1675-1676):

```powershell
$Manifest.inputs.vrfAppDataDir = $(if ($Is52 -and $VrfAppDataDir) { $VrfAppDataDir } elseif ($Is52) { '(not passed - vendor appData)' } else { $null })
```

Nothing else changes: `LaunchVrf52.ps1` already validates the directory and
already prints it into the launch output the runner captures.

## 5. One-launch validation plan

Run the ordinary 5.2 runner path once, with `-VrfAppDataDir 'C:\C2SIM\vrf-appdata\appData'`
and nothing else changed from the `20260914T130439Z` configuration, so the pair is
a single-variable comparison. Do NOT run other agents during it (the G2 lesson).

**Predictions, written before the run (a missed high-confidence one is a stop):**

1. **High.** The sim's own log at `C:\MAK\logs\vrfSimHLA1516e5.2d-*-<pid>.log`
   (harvested by the launcher into `runs\launch52\`) names
   `C:\C2SIM\vrf-appdata\appData` at startup - in the appData/settings path it
   reports, in the `vrfSim.mtl` it says it read, or in the `DtPrintEnvironmentVariables`
   / options dump. If NO line in that log mentions the relocated path, the option
   did not take and everything below is void.
   *(Deliberately hedged: I have not verified WHICH line carries it, only that
   `--appDataDir` sets `DtAppPathResolver`. If the banner is silent, the fallback
   check is 1b.)*
1b. **Fallback if 1 is silent.** Touch-time on `C:\C2SIM\vrf-appdata\appData\settings\vrfGui\*`
   or a new file under `C:\C2SIM\vrf-appdata\appData\logs\` after the run proves
   the tree was opened for writing. Absence of any write is weak evidence; absence
   of BOTH 1 and 1b is a fail.
2. **High.** `New Primary nav area` rows appear during SCENARIO LOAD - before the
   first entity/member creation line in the same log - instead of ~175 s after it.
   This is the whole point of the change; a miss is a stop, not a tuning knob.
3. **Medium.** Scenario load takes measurably LONGER than the baseline (UG52
   p1671 warns the eager load "may negatively impact performance"). A load time
   indistinguishable from baseline is a reason to doubt prediction 2 actually
   fired rather than a reason to celebrate.
4. **Not predicted, must be measured separately.** Whether route plans improve.
   Compare at EQUAL SIM TIME only (the 2026-09-13 lesson); a wall-clock or
   total-displacement verdict here would be the third false headline.

**Watch item, not a prediction:** `--appDataDir` is a NEW option on a command line
whose known 5.2 startup crash (0xC0000005) lives in
`makVrf::DtVrfSimOptions::parseCmdLine`, the same parse path that `--logFileName`
crashes in ~1 launch in 3. There is no evidence `--appDataDir` shares that defect
and the argument is short and `..`-free (VRV-6326's `resolveDots` trap avoided),
but if the first launch dies at startup, the FIRST hypothesis is this option, not
the environment - re-run once without `-VrfAppDataDir` to discriminate before
touching anything else. The launcher's existing crash detector will catch it,
print the frames, close its own corpse and exit 3.

**Rollback:** drop `-VrfAppDataDir`. Nothing under `C:\MAK` was modified, so the
vendor default is one absent argument away.

## 6. Adversarial review

- **Competing hypothesis for the 175 s gap:** that it is nav-area loading
  triggered by entity placement (the vendor's documented lazy path) rather than
  anything we control. That is not a competing hypothesis - it IS the vendor's
  stated mechanism, and the setting is its documented opt-out. What remains
  genuinely open is whether removing the gap changes route planning at all;
  prediction 4 is explicitly NOT claimed.
- **Falsifier held open:** if `New Primary nav area` still appears after member
  creation with the setting at 1, then either the option did not reach the sim
  (check 1/1b) or nav areas are loaded on a path this setting does not govern -
  and the relocation, not just the value, would need re-examining.
- **Unexplained symptom, named:** nothing in this work explains why the
  `vrfNavGenerator` mesh gave no speed effect at equal sim time in G3, or why
  1-35 froze at 1.97 km in 3/3 runs. Eager loading is a candidate contributor to
  the first, not an explanation of the second. Do not let this change be reported
  as closing `FINDING_EARLY_STOPS_2026-09-13`.
- **Frame risk accepted:** the nested-directory layout is insurance against an
  undocumented resolution base (2.3). If a live run proves the documented
  `bin64` base is the only one in play, the layout is one level deeper than it
  needed to be - harmless, and cheaper than the run it would take to find out.
- **Verified vs assumed.** Verified: the copy is complete (699 = total - cache);
  exactly one line differs; CRLF and ASCII preserved on both edited files; the
  script parses; all five dry-run cases behave as specified; the junctions
  resolve. Assumed until the validation launch: that the sim honours
  `--appDataDir` for `vrfSim.mtl` itself (VRF-9265 says it does in 5.2; not yet
  observed on this box), and that `$(APP_DIR)`-based cache paths follow it (read
  from a COMMENTED vendor default, never observed live).

## 7. RESULTS - the validation launch (2026-09-14), and the narrowing it forces

Tier HEAVY (this block adjudicates a cause claim). Full record:
docs/experiments/G7B_G8_RESULTS_2026-09-14.md. Three of the four runs of 2026-09-14 16:49-17:30Z
exercised the relocated tree:

| run | id | --appDataDir | delta from the vendor vrfSim.mtl |
|---|---|---|---|
| A | 20260914T164906Z_run | C:\C2SIM\vrf-appdata\appData | :436 loadAllNavigationDataOnTerrainLoad 0 -> 1 |
| B | 20260914T165919Z_run | C:\C2SIM\vrf-appdata-g8\appData | + :383 gamewareMemorySize 16 -> 128 |
| D | 20260914T172134Z_run | C:\C2SIM\vrf-appdata-g8b\appData | + :389 gamewareQueryTimeBudget 5.0 -> 50.0 |

Control in the same session: run C (20260914T170824Z_run) took the VENDOR appData, i.e.
loadAllNavigationDataOnTerrainLoad = 0, on the same terrain and the same navigation area.
`diff` confirms exactly one changed line in A's tree and exactly two in each of B's and D's (A's line
plus one), and the rest of each tree byte-identical to the vendor copy.

### 7.1 VERDICT: PASS as an operation, MISS on the effect it was built for

**PASS (verified).** `--appDataDir "C:\C2SIM\vrf-appdata*\appData"` is on the back-end command line
in all three runs (launchvrf.stdout.log:41), and all three launched, joined, ran and tore down
cleanly: runnerExitCode 0, no startup crash in 3 of 3 (the watch item in sec 5 - that `--appDataDir`
shares the option-parsing path with the ~1-in-3 `--logFileName` crash - did not fire), the launcher's
precondition echoed the setting it read from the tree, the relocated
`settings\connections\MAK-ONE-2025-Config.xml` resolved, the watchdog stood down without touching
anything in each run, and rtiexec 69856 / rtiForwarder 50520 survived all four runs unchanged.
The rollback path was never needed. Default behaviour was also exercised in the same session and is
unchanged: run C passed no `-VrfAppDataDir` and its manifest records
`"vrfAppDataDir": "(not passed - vendor appData)"`.

**Prediction 1 (HIGH: the sim's own log names the relocated path): still UNCHECKED as written** - the
harvesting executor is barred from opening any vendor sim log (they dump the process environment in
cleartext). **But fallback 1b FIRED, positively and decisively.** The sim rewrites 19 files under
`<appData>\settings\vrfSim\` at back-end startup - `vrfSimSettings.xml`, six `default_*` files
(.mlsx/.lrsx/.edsx/.clcx/.ppsx/.mrsx) and twelve `backups\*.backup`. Each of the four runs of this
session produced exactly one such burst, and every burst landed in exactly the tree that run's
command line named:

| run | --appDataDir passed | LaunchVrf stage | write burst | tree written |
|---|---|---|---|---|
| A | C:\C2SIM\vrf-appdata\appData | 16:49:22.2 - 16:49:43.3Z | 16:49:28.4 - 16:49:30.3Z | vrf-appdata |
| B | C:\C2SIM\vrf-appdata-g8\appData | 16:59:33.8 - 16:59:52.9Z | 16:59:39.7 - 16:59:41.5Z | vrf-appdata-g8 |
| C | **(none)** | 17:08:41.0 - 17:09:01.1Z | 17:08:46.7 - 17:08:48.3Z | **C:\MAK\vrforces5.2d\appData** |
| D | C:\C2SIM\vrf-appdata-g8b\appData | 17:21:50.9 - 17:22:10.0Z | 17:21:56.9 - 17:21:58.6Z | vrf-appdata-g8b |

One-to-one, four for four, with run C as a clean negative control: today the vendor tree was written
only inside C's launch window, and each relocated tree only inside its own run's. **`--appDataDir` is
honoured, and the directory it redirects is `settings\vrfSim` - the directory that holds the edited
`vrfSim.mtl`.** (Earlier draft of this block said 1b was negative; that was based on `appData\logs\`,
which is not where the sim writes - it writes to C:\MAK\logs by default. The settings directory is.)
Residual, stated rather than hidden: this proves the settings ROOT was adopted, not that a particular
`setqb` line was parsed and obeyed. Prediction 1 remains the way to close that last gap and is still
cheap; it is no longer blocking.

**Operational consequence for this tree, and it belongs in README-C2SIM.txt: the relocated appData is
NOT read-only at runtime.** The sim rewrites those 19 files on every launch, so "a byte copy of the
vendor tree with one change" is true at creation and drifts from then on. Any future byte-comparison
against the vendor tree must exclude `settings\vrfSim\vrfSimSettings.xml`,
`settings\vrfSim\default_*` and `settings\vrfSim\backups\*`.

**Prediction 2 (HIGH: `New Primary nav area` rows during SCENARIO LOAD, before the first entity):
MISSED.** In every run the row lands AFTER entity placement, with the setting on and off alike:

| run | loadAll | first entity placed | first `New Primary nav area` | delta |
|---|---|---|---|---|
| A | 1 | 16:51:31.9Z | 16:51:44.0Z | +12.1 s |
| B | 1 | 17:01:41.3Z | 17:01:51.9Z | +10.6 s |
| C | **0** | 17:10:49.7Z | 17:10:58.9Z | **+9.3 s** |
| D | 1 | 17:23:58.5Z | 17:24:07.6Z | +9.1 s |

A fair caveat the prediction did not anticipate: `New Primary nav area` is an OBJECT console row, so
it can never precede the object. The prediction as written was not falsifiable in the intended
direction, and the honest instrument is the back-end working set, which says the same thing more
strongly.

**Prediction 3 (MEDIUM: scenario load measurably LONGER): MISSED.** The 0 -> ~2,920 MB scenario-load
ramp is 15-20 s in every run, with the setting on and off. At the samplers' own t = 20.1 s mark:
A 2,918 MB, B 2,923 MB, D 2,925 MB - and C, with the setting OFF, 2,925 MB (already 2,962 MB at
t = 15.1 s). Sec 5 said a load time indistinguishable from baseline "is a reason to doubt prediction
2 actually fired". It is.

**The measurement that decides it (back-end working set, 5 s sampling):**

| run | loadAll | pre-placement plateau | growth window | total |
|---|---|---|---|---|
| B | 1 | 2,924 MB, flat for 100 s | 17:01:41.1 -> 17:02:01.2 (placement at 17:01:41.3) | +2,349 MB |
| C | **0** | 2,926 MB, flat for 100 s | 17:10:49.1 -> 17:11:04.2 (placement at 17:10:49.7) | **+2,324 MB** |
| D | 1 | 2,926 MB, flat for 100 s | 17:23:58.9 -> 17:24:19.0 (placement at 17:23:58.5) | +2,352 MB |

Same size, same shape, same trigger, with the setting ON and OFF. **On this evidence
`loadAllNavigationDataOnTerrainLoad = 1` produced no observable effect: the ~2.33 GB navigation-area
stream is placement-triggered either way.** The falsifier sec 6 held open has fired, and 1b's write
bursts pick which branch: not "the option did not reach the sim" but **"nav areas are loaded on a
path this setting does not govern"** - at least for a sectorised area on a streamed (MAK Earth
online + MojaveCOA.mtf) terrain. The relocation is therefore sound infrastructure with, so far, an
inert payload.

Run A has no working-set curve of its own: its sampler was capped at `max=100s`
(runs\launch52\RunScenario-20260914T164905Z.sampler.log) and stopped 31 s before PushInit. That is an
instrument defect, not a result; the ruling above rests on B, C and D.

### 7.2 What the runs DID establish about the load, and what the demo must do instead

- The nav-area stream costs the same 2,324-2,352 MB every time and is triggered by the FIRST entity
  placement (init shells and platforms, not the tasked members).
- **Its duration is cache-bound, not setting-bound:** 236.9 s on G7 attempt 4 (15:45Z, first load of
  the day on that area, minutes after a C:\MAK filesystem scan) against 20-25 s on four back-to-back
  runs an hour later. The "area ready" delay follows: 9.1-12.1 s warm, 236.9 s cold.
- **The window is real and it is silent.** Under CreationPolicy=AtOrder with no settle the order
  reaches the bus 4.7-7.7 s BEFORE the area is usable, and every member's slot move and first leg
  (476-673 m) failed `Is current point in nav area?` and fell to the FEATURE planner - 8 such
  failures in A, 8 in B, 7 in D - with the only trace a level-3 console line. Run D resolves the
  transition to 0.3 s: a goal at w=32.40 fails the gate, the area row is at w=32.60, a goal at
  w=32.70 returns 69 mesh points. Run C (AtInit + 240 s settle) had 32 of 32 gate successes.
- **Demo rule:** (1) warm the cache in the prepare step - load the scenario once before the
  demo; (2) keep `loadAllNavigationDataOnTerrainLoad = 1` (free, documented, reversible) but do not
  let it stand in for the wait; (3) gate PushOrder on the first `New Primary nav area` row from any
  created PLATFORM at object-console level 3 - 1.BdeHQ printed it in all four runs, 235 s before the
  order in C - rather than on a fixed settle; (4) if a fixed settle is used anyway, >= 30 s warm and
  240 s+ cold; (5) prefer CreationPolicy=AtInit, which puts the members in place before the wait.

### 7.3 Prediction 4 (route quality) - measured separately, and the answer is NOT about this tree

Sec 5 said route quality "must be measured separately". It was, in the same session, and the result
belongs to the SMS thread rather than to appData: with `useAbstractGraphs = true` in a derived
simulation model set (run C, vendor appData) the 5 km legs planned 8 of 8, against 2 of 24 for the
shipped flat query across attempt 4 and runs A, B and D. Neither `gamewareMemorySize 128` (B) nor
`gamewareQueryTimeBudget 50 ms` (D) changed any long-query outcome. Full adjudication:
docs/experiments/G7B_G8_RESULTS_2026-09-14.md sec 2; prereg record: PREREG_MESHQUERY_G7_2026-09-14
sec 5. **This does not close FINDING_EARLY_STOPS_2026-09-13** - the caution in sec 6 stands, and run
B in fact added a fresh unexplained crawl (four members at 0.29-0.37 m/s for ~2,400 sim s under a
logged 10 mps order) to that thread.

### 7.4 Adversarial review of this block

- **Competing hypothesis for "the setting does nothing": the option never reached the sim.**
  REFUTED by 7.1's write bursts - four runs, four bursts, each in the tree its own command line named,
  with the vendor tree as C's negative control. The surviving form of the objection is narrower: the
  sim adopted the relocated `settings\vrfSim` root but might take `vrfSim.mtl` itself from elsewhere.
  Nothing supports that and VRF-9265 is against it; prediction 1 closes it.
- **Falsifier held open, restated:** a run on a COLD cache with the setting at 1 that shows the
  ~2.33 GB arriving during scenario load, before any entity exists. That would mean the setting does
  work and today's four runs were all too warm to see it - which would be surprising (C, with the
  setting OFF, was equally warm and behaved identically) but has not been excluded.
- **Unexplained symptom, named:** why the same 2.33 GB streams at placement whether the setting is 0
  or 1, when the vendor text (p1671) says it should load with the scenario.
- **Second unexplained symptom, named because it appeared in one of these runs:** run B's four
  members crawled its entire return leg at 0.29-0.37 m/s for ~2,400 sim s under a logged 10 mps
  order, on ground they had climbed at 9.5 m/s minutes earlier. It is recorded against
  FINDING_EARLY_STOPS_2026-09-13, NOT attributed to `gamewareMemorySize 128`; n = 1.
- **Verified vs assumed.** VERIFIED: the three trees are single-variable at byte level; the option is
  on the command line; three clean launches with no startup crash and clean teardown; the four
  startup write bursts and their one-to-one mapping to the four `--appDataDir` values; the
  working-set curves and their placement trigger in B, C and D; the gate-failure counts and their
  partition by the area row; the 9.1-12.1 s warm / 236.9 s cold delay. ASSUMED: that adopting the
  relocated settings root means the relocated `vrfSim.mtl` is the file parsed (prediction 1 still
  unchecked); that the working-set growth IS the nav-area load (inherited from G7 attempt 4 CR4 and
  still not isolated); that the warm/cold ratio generalises off this machine.
