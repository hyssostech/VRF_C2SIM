# VR-Forces 5.2d UPGRADE ASSESSMENT (2026-09-02)

Offline, documents-and-files-only survey. No VR-Forces / vrfSim / vrfGui / MAK process was launched (a 5.0.2 run was live, RTI shared); nothing was written under `C:\MAK`.

Under assessment: `C:\MAK\vrforces5.2d` (rev 282607, `^/vrforces/branches/vrf5-2-branch`, built 01-05-2026, "Plugin Compatible Version: 5.2d" - `VR-Forces_Build_Information.txt`) and `C:\MAK\vrlink5.10` (rev 280763, `^/vrlink/trunk`, built 10-16-2025 - `VR-Link_Build_Information.txt`). Incumbent: `vrforces5.0.2` (rev 249613, `vrf5-0-branch`, 12-05-2022), `vrlink5.8`, `makRti4.6.1`.

Evidence convention: **VERIFIED** = read this pass from the named file/page. **INFERRED** = derived from VERIFIED facts, derivation stated. **UNTESTED** = settleable only by running 5.2.

## 0. What could NOT be read, and why

VERIFIED: the 5.2d **documentation was not installed**. Every PDF in `C:\MAK\vrforces5.2d\doc` except `VRF5.2ReleaseNotes.pdf` is a 120,690-byte placeholder whose only page reads "Documentation Has Not Been Installed ... You must run the separate documentation installer" (`VRFMigrationGuide.pdf` p1; byte-identical for `MAK_ONE_Installation_Guide.pdf`, `MAK-One-Model-Catalog.pdf`, `VRFUsersGuide.pdf`, `AddingContent.pdf`, `MAKInteroperabilityGuide.pdf`, `VR-ForcesQuickReference.pdf`, `VR-ForcesFirstExperience.pdf`). `doc/classdoc/` and `doc/help/` hold one stub each. CORRECTION 2026-09-02: the missing PDFs are PUBLIC at `https://docs.mak.com/support/` (VR-Forces_5.2_Users_Guide.pdf 1,806 pp, First_Experience_Guide, Migration_Guide, Quick_Reference_Card, Release_Notes; latest Entity Catalog online is 5.1.1) - fetched into `docs/vendor/mak-5.2/` (git-ignored, README lists the URLs). `vrForces5.2.docu.exe` was an INVENTED filename, never a vendor artifact.

Substitutes used: the real 84-page release notes; the online class reference at docs.mak.com (`vrf_migration50/51/52.html`); the 5.0.2 PDFs for baseline text; and the installed 5.2 headers and data files (primary sources, stronger than prose for API and catalog questions). `https://mak.com/mak-one-installation-guide` and `https://www.mak.com/support/product-versions` returned no usable body (the latter is now a "Support Pages Have Moved" notice), so section 1 rests on release-note text plus on-disk package metadata.

---

## 1. Data generation: SharedData/19 vs /16 - can 16 be reused?

**Vendor statement (VERIFIED), `VRF5.2ReleaseNotes.pdf` p2 "The makData Package":** "VR-Forces 5.2 is compatible with the makData 19 package. MAK applications can share the same data package. For instructions on installing the data, see 'The makData Package' in the VR-Forces Users Guide." Same page, "Disk Space Requirements": "A full installation of VR-Forces requires approximately 20.2 GB ... and an additional 82 GB for the complete makData 19 package, including the supplemental data package."

**5.0.2 counterpart (VERIFIED), `vrforces5.0.2\doc\VRF5.0.2ReleaseNotes.pdf` p5:** "VR-Forces 5.0.2 is compatible with the makData 16 package ... A complete installation of the makData 16 package requires 50 GB."

**Ruling on "can 16 be reused": NO, not as a supported configuration.** The vendor states one-to-one product/package compatibility (5.2 <-> 19, 5.0.2 <-> 16) and nowhere says an older package satisfies a newer product. INFERRED from those two quotes; no vendor text explicitly permits or forbids the downgrade, so this is the conservative reading - and the on-disk evidence supports it (VERIFIED, `C:\MAK\SharedData\{16,19}\latest\!packageInfo\CoreDataVersionInfo.txt`):

| | SharedData\16 | SharedData\19 |
|---|---|---|
| Version / Date | 16 / 2022-11-13 | 19 / 2026-01-08 |
| Revision / URL | 249132 / `^/makData/branches/vrv3_0-vrf5_0-vre2_0/data` | 282738 / `^/makData/trunk` |
| Core file count | 12,179 | 16,798 |
| Core size on disk | 3,082,998 KB | 5,653,680 KB |

16 is a branch cut for the vrf5.0 line; 19 is trunk, 4,619 more core files.

**Required vs optional (VERIFIED, same `!packageInfo`):** `CoreDataVersionInfo.txt` - "Description: Core data package containing essential models and definition files"; Core is required. `TerrainDataVersionInfo_1.txt` reports "Number Of Parts: 4 / Part Number: 1", i.e. Terrain is a four-part terrain-dependent set; only part 1 plus TestTerrain are present. The 82 GB figure is for the *complete* package "including the supplemental data package" (p2), which we do not need. INFERRED: headless needs Core plus whichever terrain part carries the AO.

Both trees coexist, and nothing under `vrforces5.2d\appData\settings` names a SharedData version (VERIFIED: no match in that tree), so version selection is by install-time data path, not a settings file we must edit. **UNTESTED:** which of parts 2-4 the R9 fixture terrain needs.

---

## 2. Release-notes items that touch this project

All VERIFIED from `C:\MAK\vrforces5.2d\doc\VRF5.2ReleaseNotes.pdf`, page cited.

**Ground movement / path planning (p3) - the biggest behavioural risk.** "The ground vehicle movement system has changed significantly. One result is the changes to movement tasks ... There are also changes to the way ground vehicles decide how to move, and vehicles might take different paths or perform different collision avoidance than they did in previous releases." p7: "Ground vehicle units now default to using the new ManeuverTo task when they are assigned the MoveTo task. They use MoveTo only if they are not configured with ManeuverTo." p7 adds a `CollisionAvoidanceEnabled` set-data request - "You might want to disable this behavior when you need more direct control over the entity during movement planning tasks." INFERRED: every corridor-discipline / offset-route / route-adherence result measured on 5.0.2 is invalidated and must be re-measured.

**Tasks consolidated (p7-p8).** MoveTo (entity + aggregate) and MoveToRetrograde (aggregate) replace the location/waypoint pairs; FireForEffect, LaseTarget, ProvideSuppressiveFire, ReleaseBomb, CrowdAround, PatternHold merge likewise. p8: "Note that the old tasks, while not in the UI, have been preserved for backward compatibility with scenarios created before VR-Forces 5.2."

**Remote-control API (p9 + FeatureList / FixedBugs tables).** VRF-9816: "Add GlobalId option to RC API create entity/aggregate arguments - For remote control create calls you can now specify the specific HLA global ID (or, if DIS, the entity ID)." VRF-10115: "DtVrfRemoteController::createControlArea does not fill out DtAppearance correctly ... Add appearance as a parameter to create control area and create a default appearance that is unfilled." VRF-9090 "SetHeading sometimes uses TaskHeading instead when using VRFRemoteController" - fixed. VRF-9252 "Time of Day incorrect from the Remote Control API" - fixed. VRF-8556: `remoteControlTextInterface` rewritten for C++20. VRF-9639: the vendor `remoteController` example was fixed for missing dependencies (we carry a copy - see sec 3.2).

**HLA / DIS (p3, p~78).** "VR-Forces supports the new HLA 4 (IEEE HLA 1516-2025) interface specification"; VRF-9183 "The VR-Forces GUI and VR-Forces sim engine support the HLA 4 protocol". VERIFIED on disk: `vrlink5.10\lib64` adds `*HLA4.lib` beside the `HLA1516e` variants; `vrforces5.2d\bin64` adds `HLAstandardMIM-2025.xml` and `envProcListenHLA4.exe`. Our HLA-1516e path is unaffected.

**Connection configuration (p3) - this breaks our launcher.** "the application uses the new exercise connection configuration (exConnConfig) file, MAK-ONE-YYYY-Config.xml, which specifies the basic set of connection settings." VERIFIED on disk: `vrforces5.2d\appData\settings\connections\MAK-ONE-2025-Config.xml` carries `execName MAK-ONE-2025`, `fedFileName RPR_FOM_v2.0_1516-2010.xml`, `rprFomVersion 2.0` and a NETN 3.0 module list; and the DIS / HLA / RPR-FOM keys 5.0.2 kept in `appData\settings\vrfSim\vrfSim.mtl` (`disPort`, `exerciseId`, `execName "VR-Link"`, `fedFileName`, `rprFomVersion 1.0`, `suppressSelfReflect`) are **absent from the 5.2 vrfSim.mtl** (file diff). Load-bearing: `scripts\LaunchVrf.ps1:100` defaults `-ConnectionProfile 'HLA 1516 Evolved RPR 2.0 with MAK extensions'`; that profile exists under `vrforces5.0.2\appData\settings\vrfLauncher\` but **not** under `vrforces5.2d\appData\settings\vrfLauncher\`, which ships only `DIS (7).xml`, `DIS (7) localhost.xml`, `HLA 1516 Evolved.xml`, `HLA 4.xml`.

**Launcher (p3).** "VR-Forces now uses a single menu option to start the Launcher. You can choose which applications start, whether it's the GUI, the simulation engine, or both." INFERRED: `LaunchVrf.ps1` and the RTI-dialog choreography around it need end-to-end re-verification.

**Frame modes - unchanged.** VERIFIED: `include\vrfmsgs\messageTypes.h:470` `const char DtFixedFrameRunToCompleteModeTypeString[] = "fixed-frame-run-to-complete";` with `:464 DtFixedFrameRunToCompleteModeType`; `vrfcgf\cgf.h:1228-1231` keeps the same deltaTime semantics; `vrfmsgs\ifSetFrameMode.h` differs from 5.0.2 only by added `override` keywords. FFRTC survives as a mechanism. Whether the measured 3.77x-slower-than-real-time at 128 units improves is **UNTESTED**, and confounded by the new movement system.

**Logging / console - same knobs.** VERIFIED: `appData\settings\vrfSim\vrfSim.mtl:168 (setqb doNotUseConsole 0)`, `:176 (setqb notifyLevel 2)`; `channelSettings.mtl` still carries per-channel `(notify-level N)`. New: `appData\settings\databaseConfig.mtl` (p9: SQLite built in, MySQL as a plug-in example; "SQL database logging also now supports aggregate-level simulations") - a candidate replacement for parts of our golden-trace scraping.

**Licensing.** p2: "VR-Forces 5.2 uses FLEXlm 11.17.1. You must install the license management software"; "The MAK License Manager files are not part of the VR-Forces installer." VERIFIED, `C:\MAK\MAKLicenseManager\SALES-TEMP-9-15-26-MAK-node-locked-DEMO_1-dec-2025.lic`: every INCREMENT line reads `maklmgrd 2026.258 15-sep-2026 uncounted`, and the packages we need are present - `vrf_remote_controller` (COMPONENTS=vrf9), `vrfremote`, `vrfengine`, `vrforces_eng_dev`, `vrfgui`. New and licence-relevant, `vrfRemoteController.h:198-202`: "This class will check out a VR-Forces Remote Control API license when instantiated, and will cancel the VR-Link license. Make sure to instantiate this class before instantiating the DtExerciseConn to avoid requiring an extra VR-Link license." **Two risks: (a) licence version 2026.258 against a 2026-01 build - INFERRED covered, UNTESTED; (b) expiry 15-sep-2026, thirteen days from today.**

**Terrain and other.** p8-p9: terrain-specific land-cover override maps, `biome.vegetation.config.xml` polygon reduction, "Terrain Page-In Area (no vegetation)" variants, simTreesTool default change; C++20 support; internal state properties; new `DtTerrainInterface::trianglesForGeometry()` and instanced-geometry callbacks (adjacent to our `DtIfRequestTerrainProfileInformation` usage, not a replacement). p6: the `AIEnabled` state property is renamed `AutonomousActionsEnabled`.

---

## 3. Migration: API, toolchain, VR-Link

Sources: `docs.mak.com/api/vrforces5.2/classref/vrf_migration52.html` and `vrf_migration51.html` (VERIFIED, fetched this pass), plus header diffs of the two installed trees (VERIFIED, primary).

### 3.1 The one breaking change that hits us: DtObjectType is gone

VERIFIED, `C:\MAK\vrforces5.2d\include\vrfExtProtocol\objectType.h` (the whole file):

```
#ifdef ALLOW_VRF4API
#include <vlpi/entityType.h>
using DtObjectType = DtEntityType;
#define DtObjectTypeIndividual 1
#define DtObjectType(SuperType, EntityType) DtEntityType(EntityType)
#pragma message("WARNING: objectType.h has been deprecated. ...")
#else
#error ERROR objectType.h has been deprecated.  The DtObjectType class has been made
equivalent to DtEntityType. ... Any supertype methods or references can be removed.
#endif
```

Corroborated by the migration guide ("DtObjectType - entirely removed; replace with DtEntityType") and by `vrfRemoteController.h`, where a `createSimObject` overload changes `const DtObjectType& type` -> `const DtEntityType& type` and the line-14 `#include <vrfExtProtocol/objectType.h>` is dropped.

**Consequence: the 8-field object type is now 7-field.** VERIFIED in the catalog: `EntityLevel\vrfSim\2A18 D-30 Howitzer.entity` is `objectType="1:1:1:222:5:4:0:0"` in 5.0.2 and `objectType="1:1:222:5:4:0:0"` in 5.2. Aggregates likewise: 5.0.2 `Tank CO US` `3:11:1:225:5:2:1:1`; 5.2 `Tank BN HQ (USA, M1A2)` `11:1:225:3:2:1:0`. Aggregate vs entity is now carried by kind=11 vs kind=1, not by superType.

Our C++ surface is already clean: `src/VrfFacade/VrfFacade.cpp:89` `toDtType()` builds a 7-field `DtEntityType`, and `createEntity`/`createAggregate` already take `DtEntityType` (5.2 `vrfRemoteController.h:1267,1282`). **The 8-field assumption lives in C#:** `src/VrfC2SimApp/ObjectTypeResolver.cs:16,17,20,136,150,174,184` hard-code `Length == 8` and `ObjectType[0] == 3` for `IsUnit`, and all 123 `objectType` strings in `data/unit-type-map.json` are 8-field. Those plus `TypeMapSelfTest.cs` are the work item.

Caveat (VERIFIED): 5.2's catalog is **mixed**. 184 of 2,510 5.2 templates still carry 8-field types - the whole legacy `AggregateLevel` set (96), most of `AggregateLevelBase` (78), and 10 stragglers (e.g. `EntityLevel\Infantry Headquarters Section (RUS Army)` `3:11:1:222:14:3:1:127`; `AggregateTacticalLevel\Battalion HQ (RUS, BMP-3)` `3:11:1:260:3:4:1:126`). A 5.2 resolver must accept both widths, normalising by stripping a leading superType when eight fields are present. **UNTESTED:** what the 5.2 back-end does with these - see question 1.

### 3.2 Every Dt* symbol we use, and its 5.2 status

From `src/VrfFacade/*.{cpp,h,cxx}` and `src/VrfBridge/VrfBridge.cpp`.

| symbol / call | 5.2 status | evidence |
|---|---|---|
| `DtVrfRemoteController`, `DtVrlinkVrfRemoteController` | present, same class | `vrfcontrol/vrfRemoteController.h` |
| `createRoute` (both overloads, incl. object-created callback) | **byte-identical signature** | 5.0.2 L1010-1026 vs 5.2 L1023-1039 |
| `moveAlongRoute(DtUUID entity, DtUUID route, addr)` | **unchanged** | 5.0.2 L1653 vs 5.2 L1638 |
| `moveToLocation`, `moveToWaypoint`, `modifyRoute` | unchanged | same diff |
| `createEntity` / `createAggregate` | source-compatible; **new trailing** `const DtString& globalId` | 5.2 L1276-1277, L1292-1293 |
| `createControlArea` | **new trailing** `const DtAppearance& appearance` | 5.2 L1096-1097, L1108-1109 |
| `createDisaggregationArea`, `modifyDisaggregationArea` | **REMOVED** | 5.0.2 L1137-1171; absent in 5.2 |
| `DtUUID`, `nullUUID()`, `DtUUIDOwner`, `DtObjectIdentificationResolver` | present; **moved** `vrfutil/rwUUID.h` -> `vrfutil/uuid.h` (rwUUID.h includes it, so our include still resolves) | both headers |
| `DtUUID` API | additive: `DtVrlUuid` ctor/assign/compare, `vrlUuid()`; **`operator==(const DtRwUUID&)` commented out** | `vrfutil/uuid.h:75,91,120,145,151-157` |
| `DtRwUUID`, `DtRwObjectName` | present; `NULL`->`nullptr`, `override` added, new `DtVrlUuid` overloads | `vrfutil/rwUUID.h` diff |
| `DtTaskCompleteReport` (task-complete reports) | **contract unchanged**; diff is `override` keywords and reflow only | `vrftasks/taskCompleteReport.h` diff |
| `DtMoveToTask`, `DtPlanAndMoveToTask` | already the 5.2 names (5.2 renames `DtMoveToLocationTask` -> `DtMoveToTask`, `DtPlanAndMoveToLocationTask` -> `DtPlanAndMoveToTask`); we are on the survivors | migration52 |
| `DtFireAtTargetTask`, `DtFollowEntityTask`, `DtBreachTask`, `DtPatrolRouteTask`, `DtMoveIntoFormationTask`, `DtScriptedTaskTask/Set` | in no removal list | migration52 |
| `DtIfRequestTerrainProfileInformation`, `DtIfIntersectionInformationResponse` | present | `vrfmsgs/` |
| `DtRequestAvailableFormationsAdmin`, `DtAvailableFormationsAdmin` | present | `vrftasks/` |
| `DtRemoteControlInitializer` (`remoteControlInit.cxx`) | our copy of the vendor example; VRF-9639 fixed that example - re-sync | release notes p39 |
| `setResourceValue(const char* resourceType, ...)` | **changed** to `(const DtEntityType&, double, int bin = 0, ...)`; `requestResourceNames()` removed; `requestResources()` re-signed to `std::list<DtEntityType>` + event-manager callback | 5.2 L1433, L1923-1934 |
| factory accessors (`taskFactory()` etc.) | return types de-`Default`-ed (`DtDefaultSimTaskFactory*` -> `DtSimTaskFactory*`) | 5.2 L2031-2038 |
| `AttachFirst` / `AttachEven` enum | replaced by `CanAttachAny` | 5.2 L212 |
| many senders gain trailing `bool sendImmediately = false` | additive | 5.2 L1562, L1873, L1988-2007 |

We use none of `DtObjectType`, the removed controllers, the removed task dialogs, the rewind callbacks, `DtStateView`, the 5.1 `DtSimComponent` tick-period methods, or `setVisibility()`, so the rest of the 5.1/5.2 removal lists does not reach us.

### 3.3 Toolchain and link surface

VERIFIED: `src/VrfBridge/VrfBridge.vcxproj:27` `<PlatformToolset>v143</PlatformToolset>` = **VS2022**, with `<VrfDir>C:/MAK/vrforces5.0.2</VrfDir>`, `<VrlDir>C:/MAK/vrlink5.8</VrlDir>`, `<RtiDir>C:/MAK/makRti4.6b</RtiDir>` - note the inconsistency: this vcxproj says `makRti4.6b` while `bridge-spikes/*.vcxproj` and the run manifests say `makRti4.6.1`. Artifacts confirm v143 (`src/VrfBridge/build/Release/obj/vc143.pdb`).

Release notes p2 "Compiler Compatibility on Windows": "MAK ONE products built with VC++14 and later are binary compatible and will work together ... The MAK RTI HLA 4 libraries are built using the Video Studio 2017 compiler and use the vc141 compiler marker." INFERRED: v143 stays valid; no toolset change is forced. p9's "VR-Forces now supports C++20" is an option, not a requirement.

Every library we link exists in 5.2 / VR-Link 5.10 (VERIFIED by listing): `vrfcontrol`, `vrlinkNetworkInterfaceHLA1516e`, `vrfMsgTransport`, `vrfExtProtocol`, `vrfExtObjectsHLA1516e`, `readerWriter`, `vrfmsgs`, `vrfplan`, `vrftasks`, `vrfutil`; and `vlHLA1516e`, `vl`, `vlutil`, `matrix`, `mtl` under `vrlink5.10\lib64`. Two `vrfcontrol` headers are gone in 5.2 (`vrfFileParser.h`, `automaticBackendSelector.h`); new are `vrfProcess.h` and `vrfutil/backend.h` (the latter now pulled in by `vrfRemoteController.h`).

---

## 4. Catalog diff for the type map

Method (INFERRED, mechanical): every `*.entity` under `<version>\data\simulationModelSets\<set>\vrfSim\` was parsed for the first `<simObject ...>` tag's `objectType`/`matchType` - 4,184 templates across both installs - and 8-field types normalised by dropping the leading superType. Set sizes (VERIFIED):

| set | 5.0.2 | 5.2d |
|---|---|---|
| `base` | 104 | 210 (+108, nearly all new tactical graphics: boundary lines by echelon, engagement areas, FSCL, FEBA, assault/attack positions, dynamic-terrain berms and ditches) |
| `EntityLevel` | 1,387 | 1,702 |
| `EntityLevel` unit (kind 11) templates | 125 | 174 (+49, none removed) |
| `AggregateLevel` | 175 | 96 (legacy, still 8-field) |
| `AggregateLevelBase` | - | 84 (**new**) |
| `AggregateTacticalLevel` | - | 411 (**new**) |
| `C2simEx` | 4 | **absent** |

5.2 include chains (VERIFIED, `(include ...)` lines): `EntityLevel.sms -> base.sms`; `AggregateLevelBase.sms -> base.sms`; `AggregateLevel.sms -> AggregateLevelBase.sms`; `AggregateTacticalLevel.sms -> AggregateLevelBase.sms`. `AggregateTacticalLevel` does **not** include `EntityLevel`, so under R-ENTITY-LEVEL its content is out of chain unless the successor to `C2simEx.sms` is authored to include both.

### 4.1 `C2simEx.sms` does not exist in 5.2 and is not in git

VERIFIED: it exists only at `C:\MAK\vrforces5.0.2\data\simulationModelSets\C2simEx.sms` plus the sibling `C2simEx\` directory; `git ls-files` returns no `.sms` or `.entity` file, so **the only copy is on this machine's 5.0.2 install and it is unversioned**. It is not vendor content: unlike `base`, `EntityLevel`, `AggregateLevel` and `MAKTest`, the `C2simEx` directory contains **no `smsChecksum*.cksm` files**, and its files are dated 2024 against a 2022-12-05 install.

What must be ported: the SMS itself (its `(include "..\data\simulationModelSets\EntityLevel.sms")` line, `vrfSim.opd`, `physicalWorldParams.mtl`, `detonationParams.mtl`, `ammunitionParams.mtl`, `indirectArtilleryTypes.mtl`, `commModelParams.mtl`, `forceHostilty.mtl`, `extra/componentAttachmentTable.mtl`, `favorites.mst`, `gui/`, `objectGroups/`, `scripts/`) and its four templates - `AR Scout` `3:11:1:225:14:30:0:1`, `Mobile Irregular` `3:11:1:-1:13:34:0:1`, `Mobile Light Infantry` `3:11:1:225:13:3:0:200`, `Skiff` `1:1:3:0:84:1:0:0` - each needing its leading superType dropped for 5.2. **Action before anything else: put `C2simEx` under version control in this repo.**

### 4.2 Gaps that 5.2 CLOSES in-chain (`EntityLevel`, so R-ENTITY-LEVEL-safe)

All VERIFIED; 5.2 types as they appear on disk (7-field).

| our gap | 5.2 template (set `EntityLevel`) | 5.2 objectType |
|---|---|---|
| **"no battalion echelon for 225"** (GEN-F, UCA-F, etc.) | `Mechanized Battalion (US Army M2)` | `11:1:225:6:4:0:126` |
| USA mech Co / Plt / HQ fidelity | `Mechanized Company (US Army M2)`; `Mechanized Platoon (USA Army M2)`; `Mechanized Headquarters Company (USA)`; `Mechanized Headquarters Section (USA)` | `11:1:225:5:4:0:126`; `11:1:225:3:4:0:126`; `11:1:225:5:4:1:0`; `11:1:225:14:4:1:0` |
| US mortar (was an FA proxy) | `Mortar Platoon (US Army M1064)`; `Mortar Section (US Army M1064)` | `11:1:225:3:8:0:110`; `11:1:225:14:8:0:110` |
| **H-UCFHE-E** hostile SP-howitzer battery (was `Field Artillery Battery (USA) M109`, wrong nation) | `Field Artillery Battery (RUS 2S1)`; `Field Artillery Platoon (RUS 2S1)`; `Field Artillery Section (RUS 2S1)`; `Field Artillery Headquarters Section (RUS)` | `11:1:222:4:8:0:0`; `11:1:222:3:8:0:0`; `11:1:222:14:8:0:0`; `11:1:222:14:7:1:0` |
| **H-UCFM-D/E** hostile mortar (was US M109) | `Mortar Platoon (RUS 2S31)`; `Mortar Section (RUS 2S31)` | `11:1:222:3:8:0:110`; `11:1:222:14:8:0:110` |
| **H-UCAA-D** hostile anti-armor (was `Antitank Team (USA Army) Javelin`) | `ATGM Section (RUS Kornet-D Tigr)` | `11:1:222:14:12:0:120` |
| **H-UCIZ-E** hostile mech-inf company (was a platoon) | `Mechanized Company (RUS)`; `Mechanized Platoon (RUS BMP-2M)`; `Mechanized Rifle Squad (RUS)`; `Mechanized Headquarters Section (RUS)` | `11:1:222:5:4:0:126`; `11:1:222:3:4:0:126`; `11:1:222:13:4:0:126`; `11:1:222:14:4:1:0` |
| **H-UCI-E** hostile rifle company / HQ | `Rifle Headquarters Section (RUS)`; `Rifle Fire Team (RUS / RUS SA / RUS AT)` | `11:1:222:14:3:1:127`; `11:1:222:12:3:0:{122,124,125}` |
| **H-UCRVA-E** hostile recon | second variant: `Recon Vehicle Platoon (RUS BRDM2)` | `11:1:222:3:6:0:99` |

Also new in-chain and possibly useful: `Vehicle Crew (RUS) Notional`, `Vehicle 2 Man Crew`, `Mortar Section (Irregular Technical)`, `Motorized Section (Irregular Technical)`, a full Canadian set (Mechanized Combat Team, Rifle Platoon HQ Section, Antitank and Machinegun Teams), and national tank platoons for Austria, Germany, Israel, Netherlands, Poland and Switzerland.

### 4.3 Gaps 5.2 does NOT close in-chain

- **All 36 `AUTHORED_PENDING` rows (every one PRC).** VERIFIED: 5.2 `EntityLevel` contains **no kind-11 unit template with country 45** - its 79 PRC items are individual platforms (kind 1) or sensors (kind 9). PRC *units* exist only in the out-of-chain `AggregateTacticalLevel`: `MECH CO PA (CHN, ZBL)` `11:1:45:5:4:0:76`, `Mech PLT (CHN, ZBL)` `11:1:45:3:4:0:0`, `Mech HQ SEC (CHN, ZBL)` `11:1:45:14:4:1:76`, `Weapons PLT (CHN, ZBL)` `11:1:45:3:4:0:78`, `Rocket PLT (CHN, DF-21)` `11:1:45:3:7:0:99`, `Rocket PLT (CHN, YJ-62)` `11:1:45:3:8:0:45`, `SAM PLT (CHN, HQ-16)` `11:1:45:3:11:0:0`. Six ground units, platoon/company/section echelon only - nothing at battalion or brigade.
- **PRC dismount: absent everywhere.** VERIFIED: no kind-3 (life form) template with country 45 in either install, any set.
- **Engineer (subcategory 10) for the hostile force: still absent in-chain** (H-UCE-E/F, H-UCEC-E/F stay armor proxies). Out of chain: `Engineer Company (RUS, Mech)` `11:1:260:5:10:0:0`, `Engineer BN (RUS, Mech)` `11:1:260:6:10:1:0`.
- **Hostile MLRS / rocket (H-UCFR-E): still absent in-chain.** Out of chain there are eight or more (`MRL Battery (RUS, BM-21 / BM-27 / BM-30 / 9A53-S)`, `MRL Battalion (...)`, `Rocket BTY (RUS, 9K79 SS-21)`, `Missile BTY (RUS, 9K720 Iskander SS-26)`).
- **Hostile target-acquisition radar (H-UCFTR-C/E): no candidate anywhere.** Country 260 + battery echelon + subcategory 30 returns nothing; nearest are `Target Acquisition BN/BTY (RUS)` `11:1:260:{6,4}:8:0:{31,30}` (subcategory 8, out of chain).
- **New PRC platforms worth re-checking the entity-level proxies against (in-chain, VERIFIED):** `ZBL-08 (Type 08) IFV` `1:1:45:2:20:0:0`, `Type 96 (ZTZ96) MBT` `1:1:45:1:10:0:0`, `Type 95 PGZ95 SPAAG` `1:1:45:28:4:8:0`, `Type 59 (M1954) Howitzer` `1:1:45:5:2:2:0`, the `HQ-16A` TEL / command post / radars, `Type 052C` and `052D` destroyers, `CASC CH-5 Rainbow`.

### 4.4 Does 5.2 still code Russia as 260?

**Yes, and it spreads the practice.** VERIFIED: the legacy `AggregateLevel` set is unchanged (14 country-260 templates with byte-identical types, e.g. `TANK BN RU solo` `3:11:1:260:6:2:1:20`), **and the new `AggregateTacticalLevel` set uses 260 for its entire Russian order of battle** - 162 templates at country 260 against 2 at 222. `EntityLevel` by contrast uses 222 for RUS units (37 country-222 unit templates). So in 5.2: entity-level RUS = 222, aggregate-level RUS = 260. Any move to consume `AggregateTacticalLevel` must map RUS to 260. (260 is the SISO code for the USSR, 222 for Russia - INFERRED from usage, not read from a 5.2 enumerations file this pass.)

---

## 5. Recommended upgrade sequence

Each row names what must be *re-verified on 5.2*, not merely ported.

| # | step | gate | why |
|---|---|---|---|
| 1 | DONE 2026-09-02: 5.2 Users Guide / Migration Guide / First Experience / Quick Ref / Release Notes fetched from docs.mak.com/support into `docs/vendor/mak-5.2/` | - | sec 0: re-verify this document's prose claims against the real Migration Guide + Users Guide before step 4 |
| 2 | Commit `C2simEx` (SMS + directory) into this repo from the 5.0.2 install | - | sec 4.1: a single unversioned copy; losing it loses the fixture |
| 3 | Renew / confirm the licence | **SPEND** | expires 15-sep-2026; confirm `vrf_remote_controller` covers a 2026-01 build |
| 4 | Port `C2simEx` to 5.2: drop superType from the 4 templates, re-point the include, re-check the eight `.mtl` payloads against 5.2 defaults | **PLAN** | sec 3.1, 4.1 |
| 5 | Widen `ObjectTypeResolver.cs` and `TypeMapSelfTest.cs` to 7-or-8 field (normalise by stripping a leading superType); regenerate `data/unit-type-map.json` against the 5.2 chain | **PREREG** | sec 3.1; predict which rows change fidelity class BEFORE regenerating, or the diff cannot be audited |
| 6 | Rebuild `VrfBridge`/`VrfFacade` against `vrforces5.2d` + `vrlink5.10` (back up DLLs, `/t:Rebuild`, redeploy all copies) | - | sec 3.2-3.3; expect only additive-default and `setResourceValue` breakage |
| 7 | Re-point the runner: `LaunchVrf.ps1`, `RunC2SimScenario.ps1`, `AnswerCrashDumpDialog.ps1` and the three `bridge-spikes/*.vcxproj` hard-code `C:/MAK/vrforces5.0.2`, `vrlink5.8`, `makRti4.6{b,.1}` | **RULE** | sec 3.3; and the `-ConnectionProfile` default no longer exists in 5.2 (sec 2) - ask which 5.2 profile plus exConnConfig to use |
| 8 | Re-run the type-map live gate (`docs/experiments/PREREG_TYPEMAP_LIVE_GATE_2026-09-02.md`) on 5.2 | **PREREG** | the static best-match rule has one outstanding live falsifier (`VRF_GROUND_TRUTH.md` 0.1.8) and the type format changed underneath it |
| 9 | Re-run the R9 fixture end to end (discovery, static position, route-uuid fix, task-complete) | **PREREG** | route / `moveAlongRoute` / `DtTaskCompleteReport` contracts are unchanged (sec 3.2), so a failure here is a movement-system or connection-config effect, not an API effect |
| 10 | Re-measure FFRTC at 128 units | **PREREG** | mechanism survives (sec 2); the 3.77x number does not transfer across a rewritten ground-movement system |
| 11 | Only then consider `AggregateTacticalLevel` | **PLAN** | it reopens R-ENTITY-LEVEL and requires RUS -> 260 |

**State that migrates deliberately, and what must NOT:**

- `vrfSim.mtl` notify levels: port `notifyLevel`, `doNotUseConsole` and the `channelSettings.mtl` per-channel levels **by hand**. Do not copy the 5.0.2 file - 5.2's version has had the DIS and HLA/RPR-FOM blocks removed to the exConnConfig (sec 2), so a wholesale copy reintroduces dead keys and drops the new ones.
- **The DISABLED NavArea artifact must NOT be carried into SharedData\19.** It is the 2026-07-14 project-generated 120k-tile NavArea in `SharedData/16/latest/TerrainData/navData/` that caused the 2026-07-15..2026-09-01 freeze (`docs/HANDOFF_2026-09-01_R9_COMPLETE.md:25`, `docs/CORRECTIONS_LOG.md:143`). 19 starts clean; if nav data is needed, regenerate it on 5.2 and record the generation as its own change.
- FFRTC fixture (`TropicTortoise_FFRTC.scnx`) and `tools/FixtureGen`: carry forward, but the fixture references `C2simEx.sms` and the 5.0.2 terrain, so it is downstream of steps 2 and 4.
- Runner paths pinned to `vrforces5.0.2`: step 7. `VrfBridge` rebuild + full redeploy: step 6.

---

## 6. Open questions for the user / for MAK

1. **Mixed-width object types.** 5.2 ships 184 templates in the old 8-field form beside 2,326 in the new 7-field form (sec 3.1). Is the 8-field form still accepted by the 5.2 back-end matcher, or are those files stale? This decides whether our resolver normalises or rejects.
2. **RUS = 260 vs 222 (sec 4.4).** Entity-level RUS content is country 222; the entire new aggregate-level Russian OOB is 260. Which does the C2SIM hostile-nation config emit, and do we surface the discrepancy downstream under R-SURFACE-PROXY?
3. **PRC (sec 4.3).** 5.2 still ships no PRC unit and no PRC dismount at entity level. Options: (a) keep the 36 rows AUTHORED_PENDING and author them from the new PRC platforms (ZBL-08, Type 96, Type 95, HQ-16A) under R-AUTHORING-IN-SCOPE; (b) relax R-ENTITY-LEVEL far enough to reach `AggregateTacticalLevel`'s six PRC ground units; (c) keep PRC on RUS proxies. A ruling, not a derivation.
4. **Licence.** Expiry 15-sep-2026 and version 2026.258 against a 2026-01 build - is a renewed or 5.2-specific `.lic` coming?
5. **RTI.** Stay on `makRti4.6.1` + HLA 1516e, or move to the HLA 4 stack 5.2 now ships? All our libraries are HLA1516e; both variants exist in VR-Link 5.10.
6. **Which makData 19 terrain parts (2-4) does the target AO need?** Only part 1 and TestTerrain are loaded (sec 1).
7. **Is 5.2 the target at all, or is this a survey?** Steps 1-11 are roughly a week of work with several PREREG gates, and the 5.0.2 stack is currently green.

## 7. Adversarial review

Competing hypothesis weighed for the central claim of sec 3.1 ("5.2 dropped the superType"): that VR-Forces instead dropped the *domain* field, which would explain `1:1:1:222:...` -> `1:1:222:...` equally well. Falsified by an air platform: `Chengdu J-10C` goes `1:1:2:45:1:5:6:0` (5.0.2) -> `1:2:45:1:5:6:0` (5.2). Dropping the domain would yield `1:1:45:...`; dropping the superType yields the observed `1:2:45:...`. Independently corroborated by `objectType.h`'s `#error` text ("Any supertype methods or references can be removed") and by the `DtObjectType` -> `DtEntityType` parameter change in `vrfRemoteController.h`.

Symptom still unexplained: the 184 8-field survivors inside 5.2 (sec 3.1, question 1). That is a genuine falsifier of the strong form "5.2 is uniformly 7-field", so the claim is stated in the weak form (canonical form is 7-field; the shipped catalog is mixed) and the residue is carried into the open questions rather than into the migration plan.

Deliberately not asserted: anything from the 5.2 Users Guide, Migration Guide PDF, Installation Guide or Model Catalog (sec 0), and any runtime behaviour of 5.2 - nothing was executed.
