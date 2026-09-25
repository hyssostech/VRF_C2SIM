# VR-Forces 5.2 vendor PDF index

The PDFs in this folder are git-ignored (`docs/vendor/**/*.pdf`); re-fetch them from the URLs
below (see ../README.txt). Text extracts for `rg` live in `txt/` (git-ignored, derived; one
`=== PDFPAGE n LABEL n ===` marker per page, made with pypdf 6.19.0).

Page numbers below are PDF page numbers (what a viewer's "go to page" uses). For every PDF in
this set the page label equals the physical page, and for the Users Guide the printed folio
equals it too (checked: UG p.368 prints "368", TOC entries match the heading pages).

Verified 2026-09-25: each file starts with `%PDF-`, is far above the 120,690-byte installer
placeholder, and opens in pypdf with the page count shown. Control: the public
VR-Forces_5.2_Release_Notes.pdf is byte-identical (same sha256) to the one real PDF the 5.2d
install ships, C:\MAK\vrforces5.2d\doc\VRF5.2ReleaseNotes.pdf.

## Files

Base URL: https://docs.mak.com/support/

| File | Revision (front matter) | Pages | Bytes | sha256 |
|---|---|---|---|---|
| VR-Forces_5.2_Users_Guide.pdf | VRF-5.2-02-251017 | 1806 | 109305050 | bc03dbc52c0469d4539e71051310225b21bfb407477c7e06cc98522db180d89f |
| VR-Forces_5.2_Release_Notes.pdf | VRF-5.2-01-251028 | 84 | 981264 | 81b402e48533ea1d895c485dd28946a8bab73a55fd59d71c5b6313a5b9572800 |
| VR-Forces_5.2_Migration_Guide.pdf | VRF-5.2-09-251017 | 90 | 1441025 | ca21219cb8c97c3cb8b5a64ffa6922ed36d770049fa97e8522a7f7305453db9c |
| VR-Forces_5.2_First_Experience_Guide.pdf | VRF-5.2-05-251017 | 90 | 26391575 | 4e905f6dd46b32f6e0fcf972c9b125760c54487e3b415699651b374ebc5e9226 |
| VR-Forces_5.2_Quick_Reference_Card.pdf | VRF-5.2-QRC-251017 | 26 | 286005 | 47558ec8b968eac2f7ce91d184114a1c33014cff8f07355153f2ed23cf5f3207 |
| VR-Forces_5.1.1_Entity_Catalog.pdf | VRF-5.1.1-22-240925 (5.1.1, not 5.2) | 1326 | 60336910 | 1eb7540f94ba5d8b904eee7bdb3e6334378f796550f85c37332ef99cc28373a3 |
| MAK_ONE_2025_Model_Catalog.pdf | MAK-25.0-11-250912 (MAK ONE 2025) | 680 | 135571044 | bed9bd31d7094195242dd78247a5d36bcaf27ce1f514e1d8206884d2b6eee0d5 |
| MAK_ONE_2025_Interoperability_Guide.pdf | MAK-25.0-03-251024 (MAK ONE 2025) | 96 | 1386931 | 4441453448e58d418faf249a4f44b98084de9b50fe9989cd777d2aa4557b2900 |

URL of each = base URL + file name. The two MAK ONE 2025 files are the public counterparts of
the installer's MAK-One-Model-Catalog.pdf and MAKInteroperabilityGuide.pdf; the installed
copies are placeholders, so there is no hash control that they are the exact revision 5.2d
ships.

## Where to find (UG = VR-Forces_5.2_Users_Guide.pdf)

- **vrfNavGenerator / navigation data generation (command line, config file, navDataDir)**
  - The strings `vrfNavGenerator` and `navDataDir` are not found in the PDF set (HTML Users
    Guide / classref / headers only). The PDF documents GUI-driven generation:
  - UG p.1271 "66. Generating Navigation Data" (chapter TOC); p.1272 "66.1. Introduction" /
    "66.1.1 How Navigation Data is Generated".
  - UG p.1281 "66.3. Generation of Navigation Data" (output to ./userData/navData) and
    "66.3.1 Navigation Profiles" (./appData/settings/vrfSim/navigationProfiles.mtl).
  - UG p.1282 "66.3.2 Generating Navigation Data for a Navigation Area"; p.1284 "66.3.3 Using
    Newly Generated Navigation Data", "66.3.4 Automatic Regeneration of Navigation Data".
  - UG p.1285 "66.4. Generating Navigation Data for Entities" (saved under the SMS's
    vrfSim/navData).
  - UG p.1287 "66.5. Importing Navigation Areas and Navigation Data" (.navGenConfig and
    .navRuntimeConfig files in ./userData/navData).
- **SMS include and override mechanism**
  - UG p.1309 "68.3. Simulation Model Sets".
  - UG p.1310 "68.3.1 Including Simulation Model Sets in Other Simulation Model Sets".
  - UG p.1312 "68.3.3 SMS Priority for Included and Multiple SMSs" (override examples continue
    on p.1313).
  - UG p.1313 "68.3.4 Lua Scripts in Included SMSs" (continues p.1314).
  - UG p.1314 "68.3.5 Application-Level Files in Included SMSs" (every SMS must have
    vrfSim.opd); same page "68.3.6 Search Paths for Referenced Files in an SMS".
  - **Table 15** in this PDF is "Table 15. Batch file parameters" (section "7.10.3 Editing a
    Batch File", UG p.270), table on pp.271-272; the absolute-path wording is on p.271
    (logger-files-path; scenario-filename "can be specified as an absolute path or a path
    relative to the directory in which the executable is located"; sms-filename overrides the
    scenario's SMS).
- **AutonomousActionsEnabled**
  - UG p.508 "23.6. Disabling Autonomous Actions" (Move To goes straight-line, no path
    planning, when disabled).
  - UG p.866 "40.3. Autonomous Actions Enabled" (the set data request).
  - Migration Guide p.21 "2.6. AI Enabled Renamed to Autonomous Actions Enabled" (state
    property AIEnabled -> AutonomousActionsEnabled).
  - Release Notes p.6 and p.76 (the rename).
- **setAltitude / AGL frame**
  - The string `setAltitude` is not found in the PDF set (classref / headers only).
  - UG p.386 "14.4. Specifying an Object's Altitude"; p.387 "14.4.1 Setting Altitude
    Dynamically" (set relative to the terrain) and "14.4.2 Setting Altitude in the Create
    Object or Edit Object Dialog Box" (Above Sea Level / Above Terrain).
  - UG p.869 "40.7. Altitude" (set data request; MSL or AGL base).
  - UG p.605 "30.29. Move to Altitude"; Release Notes p.64 (VRF-7622: AGL option in Move to
    Altitude task).
- **Move Along Route vs Move To**
  - UG p.500 "23.2. How the Move To Task Works" / "23.2.1 Path Planning".
  - UG p.505 "23.3. How the Move Along Route Task Works" ("There is no path planning done as
    it moves toward the next vertex").
  - UG p.599 "30.24. Move Along Route"; p.600 "30.25. Move Along Route with Actions";
    p.604 "30.28. Move To"; p.606 "30.32. Move To (Plan Path)".
  - Aggregate level: UG p.734 "35.5.7 Move Along Route", "35.5.9 Move To".
  - Migration Guide pp.19-20 "2.4.1 Task Updates" / "2.4.2 SMS Changes Related to Ground
    Vehicle Movement" (Table 1, old and new task IDs).
- **Entity level vs aggregate level**
  - UG p.368 "13.7. Entity-Level Modeling and Aggregate-Level Modeling".
  - UG p.386 "14.3.3 Default Placement of New Entities".
- **blockOnAsynchronousOperations**
  - UG p.1669, Appendix "C.1. The vrfSim.mtl Configuration File Parameters", "Table 76.
    vrfSim.mtl parameters" (fixed-frame clock modes only; default 0).
- **Plan trigger conditions (DtCeEntDestroyed, DtCeEntInArea)**
  - The class names `DtCeEntDestroyed` / `DtCeEntInArea` are not found in the PDF set
    (classref / headers only). The GUI-level conditions:
  - UG p.933 "42.2. Conditional Statements" (condition list p.934: Entity In Area, Entity
    Destroyed); p.935 "42.2.2 Triggers".
  - UG p.939 "42.2.6 Conditional Tests": "Entity Destroyed" p.939, "Entity In Area" p.940.
  - UG p.944 "42.5.1 Considerations for Using Triggers"; p.951 "43.1.5 Adding a Trigger to a
    Plan"; p.953 "43.1.6 Registering Triggers".
