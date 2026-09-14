# Docs read on the nav-mesh query refusal (Sonnet reader, 2026-09-14 ~11:20Z; supervisor-verified quotes:
# vrfSim.mtl:383/389, ground-vehicle-move-to.lua:488 useAbstractGraphs = false). Governs PLAN lane L1 / PREREG G7.
# Supervisor additions: the shipped Lua calls the query with useAbstractGraphs = FALSE while the API default is true
# and the doc says true 'can speed up long path planning queries'; the Lua job API exposes no failure predicate
# (class_dt_lua_nav_find_path_job: results/complete only), so outOfWorkingMemory is not visible from Lua.

NAVMESH QUERY DOCS
==================
Scope: documentation-only research (read-only) into why vrf:findPathToLocation
("ground-vehicle-move-to.lua") prints "Planned nav path has not enough (0)
points." for 9-20 km goals on a 41 x 54 km, 8,856-sector ground-platform
NavArea, while 6-23 m goals succeed. No logs opened, nothing launched, no
edits made anywhere. Sources: installed VR-Forces 5.2d docs under
C:\MAK\vrforces5.2d\doc (identical byte size to the repo mirror under
docs\vendor\mak-5.2, confirmed for the Users Guide: 109,305,050 bytes both),
plus docs.mak.com and general web search.

Two of the task's own section citations (Users Guide 23.5, Migration Guide
1.2.2) do not match the shipped documents; both mismatches are called out
below rather than silently substituted.


1. vrfSim.mtl comment blocks, lines ~370-409
---------------------------------------------
File read: C:\MAK\vrforces5.2d\appData\settings\vrfSim\vrfSim.mtl

Line 371-376 (reuseEntityIdentifiersOnScenarioLoad) - not nav-related, skipped.

Lines 378-383, gamewareMemorySize:
  ";; Set the working memory size limit used internally by GameWare when
  calculating paths. Size is in MB."
  ";; Increasing this limit can allow for path plans on larger nav areas by
  more entities simultaneously."
  ";; Note:  Multiple working memory blocks are allocated with this limit,
  so this is not an overall limit for GameWare."
  ";; Default: 16"
  "(setqb gamewareMemorySize 16)"

Lines 385-389, gamewareQueryTimeBudget:
  ";; Set the time budget for GameWare navigation queries per frame. This is
  the amount of time GameWare is given to perform tasks such as path
  planning each sim frame. The higher the value, the more responsiveness
  path planning will be, but the slower the VR-Forces sim frame rate will
  be."
  ";; Default: 5.0 ms"
  "(setqb gamewareQueryTimeBudget 5.0)"

  NOTE - this shipped "Default: 5.0 ms" comment DISAGREES with both the
  Users Guide Appendix C table and the Developer's Guide struct default
  (both say the true engine default is 1.0 ms / 1). See item 2 and item 4.

Lines 391-395, regenerateNavigationDataAtRuntime; 397-402,
maxNavigationRegenerationThreads; 404-409, navigationRegenerationDelay: all
concern runtime regeneration in response to dynamic terrain, not query-time
path planning on static terrain. Not relevant to a static 41x54 km area with
no dynamic-terrain events; noted for completeness only.

No line in this file, or anywhere else in vrfSim.mtl (grepped the whole
file), states a maximum search radius, a maximum number of sectors touched
per query, or any other explicit distance bound. The two gameware*
parameters shown above are the only mtl knobs that govern a nav-mesh path
query's cost budget.


2. VR-Forces 5.2 Users Guide
-----------------------------
File read: C:\MAK\vrforces5.2d\doc\VRFUsersGuide.pdf (1,806 pages; identical
to repo docs\vendor\mak-5.2\VR-Forces_5.2_Users_Guide.pdf). Page numbers
below are the PRINTED page numbers (PDF footer text), verified in both
directions with per-page pdftotext extraction against the outline/bookmark
page indices.

Sectorization and area-size limits - Chapter 66, "66.2. Navigation Areas",
p.1276:
  "Navigation areas define areas on the terrain for which you can generate
  navigation data. Navigation areas are rectangular areas that are aligned
  to the North/South axis. The maximum size of the area on which you can
  generate navigation data depends on the raster precision value used to
  generate it. If you reduce the precision of navigation data, creating a
  sparser graph, the size of the maximum area increases. The maximum size
  for a navigation area using the default values is 20 km by 20 km. (Raster
  precision is configured in ./appData/settings/vrfSim/navigationProfiles.mtl.)"

  Our NavArea is 41 x 54 km - roughly 5.5x the documented default-precision
  max area by footprint. The doc frames this as a raster-precision tradeoff,
  not a hard wall, but it also never states what changes about query
  behavior once an area is generated at reduced precision far past the
  documented default max. See "what the docs do not say" below.

"66.2.1 Sectorizing Navigation Areas", p.1277 (this is the section that
contains the exact phrase the task asked for):
  "You can generate navigation data for a navigation area as a whole or you
  can divide the navigation area into sectors and generate navigation data
  for each sector. VR-Forces can regenerate navigation data at runtime in
  response to changes in the terrain (dynamic terrain damage and placement
  of buildings). If you sectorize the navigation area, regeneration is much
  quicker because VR-Forces can restrict regeneration to the sector that is
  affected rather than the entire navigation area. The disadvantages of
  using sectors are:
   - Increasing the number of sectors slows down generation of navigation
     data.
   - The navigation data generation process can fail if any single sector
     is too large, too complex, or both. However, this is also true if you
     generate navigation data for a very large terrain without using
     sectors.
   - Path planning over long distances may be slower.
  To generate a maximum size navigation area, you must use multiple
  sectors."

  This is the ONLY sentence in the entire Users Guide that connects
  sectorization to long-distance path planning, and it says "may be
  slower," never "may return zero points" or "may fail outright." The doc
  does not give a mechanism (memory exhaustion vs. time-budget starvation
  vs. sector-loading gap) for why it would be slower, and does not say at
  what distance or sector-count "slower" turns into "fails."

Lazy loading - Appendix C (below) and p.1280-1281:
  "For performance reasons, navigation data does not load until you place a
  simulation object that will use it. When you enable Navigation Lab
  debugging and look at the terrain's navigation areas before a simulation
  object uses it, the information dialog box shows that the data has not
  been loaded." (p.1280-1281, "66.2.7 Displaying Information about a
  Navigation Area")

  This describes lazy loading at the granularity of the WHOLE navigation
  area ("place a simulation object... within it"), not per-sector loading
  on approach to a distant goal. Nothing in the chapter describes sectors
  being paged in/out individually as an entity's path or query crosses
  sector boundaries; regeneration (66.3.4) is sector-scoped, but ordinary
  read-only path queries are never described as sector-scoped.

Memory / query time budget - Appendix C, "C.1. The vrfSim.mtl Configuration
File Parameters", Table 76:

  p.1670, gamewareMemorySize:
    "Set the working memory size limit used internally by GameWare when
    calculating paths. Size is in MB. Increasing this limit can allow for
    path plans on larger navigation areas by more entities simultaneously.
    Note: Multiple working memory blocks are allocated with this limit, so
    this is not an overall limit for Gameware."
    (No stated default printed adjacent to this entry in the table; the
    shipped mtl comment says 16.)

  p.1671, gamewareQueryTimeBudget:
    "Set the time budget for GameWare navigation queries per frame. This is
    the amount of time GameWare is given to perform tasks such as path
    planning each simulation frame. The higher the value, the more
    responsiveness path planning will be, but the slower the VR-Forces
    simulation frame rate will be. Default: 1.0 ms."

    CONFLICT: the shipped vrfSim.mtl comment says "Default: 5.0 ms" (item
    1) and the file itself sets 5.0; the Users Guide table says the true
    default is "1.0 ms"; the Developer's Guide struct doc (item 4) agrees
    with the Users Guide ("Default value is 1."). Two independent doc
    sources agree with each other and disagree with the shipped file's own
    comment. This is a genuine, citable documentation/shipped-config
    mismatch, not a research artifact.

  p.1671, loadAllNavigationDataOnTerrainLoad:
    "Loads all of the navigation data when the scenario loads, rather than
    waiting until an entity needs to use that data (either when it is
    placed in a navigation area or moves into one). If set to 1, navigation
    loads the data at the same time you load or create a scenario. Please
    be aware this may negatively impact performance. If it is set to 0, the
    navigation data loads only when an entity exists in the navigation
    area. Default: 0."

    This is the closest the manual comes to describing "lazy loading of
    sectors" - but it is phrased as a whole-navigation-area on/off switch
    (all vs. on-demand-by-entity-presence), not a per-sector, per-query
    streaming mechanism. There is no parameter here that lazily loads only
    the sectors near a distant goal point while leaving the rest unloaded.

Path planning, nav mesh vs. feature-planner fallback - the task named
"chapter 23.5"; the real content lives in "23.2.1 Path Planning" (p.500-501)
under Chapter 23, "Ground Vehicle Movement". Chapter 23.5 is actually "How
Terrain Affects Movement" (soil/slope costs, p.504-507) - a real section,
but not the nav-mesh-vs-feature-planner-fallback content the task
described; that content is one section earlier, in 23.2.1. Cited correctly
below:

  p.500, "23.2.1 Path Planning":
    "Path planning involves several distinct types of logic, depending on
    the terrain. When roads are available and road driving is desired, the
    planner automatically looks for a road network. When road movement is
    not desired, then the planner either uses a nav mesh or creates a clear
    route between feature obstacles."

  p.501, "Path Planning on the Nav Mesh" (the exact fallback rule):
    "If a navigation mesh is available, each off-road segment is planned
    using this nav mesh. The planner takes the costs of different types of
    soils and the slope of the terrain into account as it plans the path.
    If navigation mesh planning is not successful--for example, if there is
    no navigation mesh in the area--then the segment will be planned around
    obstacles using feature path planning."

    This sentence gives exactly one documented reason nav-mesh planning can
    fail over to feature planning: "no navigation mesh in the area." It
    does NOT say a working-memory or time-budget exhaustion on an otherwise
    present nav mesh triggers this fallback, and it does NOT describe the
    "0 points" message at all - that string is not in the Users Guide
    (confirmed by full-text search of the extracted 73,146-line dump of the
    entire manual; zero hits for "not enough" combined with "path" or
    "points" in a nav-planning context, and zero hits for the message
    verbatim).

Directory structure / settings survival - covered together with item 3
below since both come from the same manual.


3. Where vrfSim.mtl is read from; per-user/per-project overrides
-------------------------------------------------------------------
Users Guide 3.7 ("Managing VR-Forces Settings", p.123-126) turns out to be
about GUI display/dependent settings (model definitions, tactical-graphic
toggles, etc.) saved under ./appData/settings/vrfGui - it does not discuss
vrfSim.mtl or sim-engine settings survival at all. The actually relevant
material is Chapter 2 ("Installing VR-Forces") and Chapter 5
("Command-Line Options").

Where vrfSim.mtl lives (Appendix C intro, p.1667):
  "The file ./appData/settings/vrfSim/vrfSim.mtl configures sim engines.
  Table 76 describes the parameters in vrfSim.mtl."

  This matches the installed layout exactly:
  C:\MAK\vrforces5.2d\appData\settings\vrfSim\vrfSim.mtl

Directory structure, Table 2 (Chapter 2.2, p.102-104):
  "./appData ... Schema, model definitions, settings, configuration files,
  and other data." (p.102)
  "./factory ... Backups of the application data and settings provided by
  MAK." (p.104)
  "./userData ... Scenarios, terrain files, and other files that are
  created as you use VR-Forces." (p.104)

  So vrfSim.mtl lives in ./appData, which is INSIDE the versioned product
  install tree (C:\MAK\vrforces5.2d\...), not in ./userData.

Reinstall/uninstall behavior (Chapter 2.1.7/2.1.8, p.100-101):
  "When you uninstall VR-Forces, files such as scenarios or SMSs that
  reside in the product directory are also deleted. Before uninstalling,
  copy any of your custom files from the product directory to another
  folder. The uninstaller does not remove the data package. That is in a
  separate directory (for example, ./SharedData/19/latest)." (repeated
  near-verbatim for both Windows p.100-101 and Linux p.101)

  This is explicit: a direct edit to
  C:\MAK\vrforces5.2d\appData\settings\vrfSim\vrfSim.mtl is a "file that
  resides in the product directory" and is NOT protected from an uninstall
  or a reinstall-over-the-same-version. Only ./SharedData (the makData
  package) is called out as surviving. The manual never states that a
  reinstall preserves ./appData; it only ever discusses that guarantee for
  ./SharedData "Backup / Delete / Overwrite" options (p.99-100), which is a
  DIFFERENT mechanism (the MAK ONE Software Manager's data-package
  installer) than the VR-Forces product installer that owns
  C:\MAK\vrforces5.2d itself.

The sanctioned survival mechanism - Chapter 5, "5.2. Command-Line Options
for vrfSim", Table 11 (verified by direct per-page extraction to avoid the
column-reflow artifacts that pdftotext -layout produces on this two-column
table; each quote below was re-extracted from its own single page in plain
reading order and cross-checked against the -layout dump):

  p.178, --appDataDir directory:
    "Specifies the location of application data. If not specified, VR-
    Forces uses the default: ./appData."

  p.183, --sharedSettingsDirectory directory:
    "Specifies the directory where shared settings are stored. The default
    directory is $(APP_DIR)/settings. If the --useUserSettingsDirectory
    flag is set, then the user settings directory is used to store the
    settings instead. If not, the application's configuration location
    (system-dependent) is used."

  p.183, (--settingsFile | -d) file_name:
    "Specifies a configuration file to load instead of vrfSimSettings.xml."
    (Note: this option is about vrfSimSettings.xml, a different settings
    layer than vrfSim.mtl; the manual never ties --settingsFile to
    vrfSim.mtl specifically.)

  p.184, --userDataDir directory:
    "Specifies the location of the user data directory for the sim
    engine."

  p.185, --useUserSettingsDirectory:
    "Indicates that the system should store settings in the user login
    settings directory, rather than the application settings directory.
    Use in conjunction with --sharedSettingsDirectory."

  Release Notes corroboration that --appDataDir is a live, maintained
  mechanism in 5.2 (see item 5): VRF-9255 ("VRF Launcher does not respect
  updated appData paths for plug-in selection") was fixed by adding
  --appDataDir support to the Launcher, and VRF-9265 ("--appDataDir is not
  processed until after attempting to load config files...") was fixed to
  load data files correctly under this option - both filed and fixed
  specifically against VRF-5.2, i.e., this is the current, supported
  relocation mechanism, not a legacy one.

SANCTIONED WAY TO SURVIVE A REINSTALL: launch vrfSim (and vrfGui, if used)
with --appDataDir pointing at a directory OUTSIDE C:\MAK\vrforces5.2d (for
example, a persistent path under C:\MAK\config\ or similar), copy the
existing appData\settings\vrfSim\vrfSim.mtl tree there once, and make all
future edits (gamewareMemorySize, gamewareQueryTimeBudget) in that external
copy. Optionally combine with --sharedSettingsDirectory /
--useUserSettingsDirectory to push settings into the OS per-user profile
location instead. Editing vrfSim.mtl in place under C:\MAK\vrforces5.2d has
no documented reinstall protection.

vrfSimHLA1516e.exe --help: NOT executed, per the task's own conditional
instruction (I could not first confirm from the Users Guide that it is
documented as a help-only, self-terminating invocation, and running an
unverified sim-engine binary is outside a read-only documentation task
regardless). The Users Guide's own command-line appendix (Chapter 5, Table
11, and the HLA-only options on p.185-186) was used instead and is
sufficient - it documents (--help | -h) as: "Displays command-line usage
information in a console window," and separately confirms that
vrfSimHLA1516e and vrfSimUserHLA4 share Table 11 plus an "HLA Only" block of
additional options (p.185: "These are the options specific to
vrfSimHLA1516e and vrfSimUserHLA4.").

Migration Guide 1.2.2 - the task asked for "1.2.2 on custom settings"; the
actual section 1.2.2 in VRFMigrationGuide.pdf (p.12) is "Copying versus
Including VR-Forces SMS" and is about Simulation Model Set upgrade
strategy, not about settings-file survival:
  "A custom SMS is typically either a modified copy of a shipped VR-Forces
  SMS or it includes and extends a VR-Forces SMS. Custom SMSs that include
  and extend a VR-Forces SMS are much easier to upgrade..."
  This section has nothing to do with vrfSim.mtl, appData, or settings
  persistence. The Migration Guide's table of contents was checked in full
  (90 pages) and contains no section on custom settings-file survival
  across versions; the nearest topically-related heading in the whole guide
  is "6.2. File Locations" under "Migration to VR-Forces 4.9" (p.33), which
  is about where SMS-related files moved between old VR-Forces 4.x
  releases, not about appData/vrfSim.mtl overrides, and is 8 major versions
  stale relative to 5.2.


4. Developer's Guide - nav-mesh path planner API
---------------------------------------------------
No file named vrf_groundVehiclePathPlanningNavMesh.htm exists in the
installed classdoc/classref tree or at the docs.mak.com mirror (checked:
docs.mak.com/api/vrforces5.2/classref/class_dt_nav_area.html is live and
matches the installed copy verbatim for the quoted API, confirmed by
WebFetch). The Lua-facing findPathToLocation is implemented against
DtNavArea::findPathToLocation and returns a DtNavAreaQuery; those are the
right pages.

C:\MAK\vrforces5.2d\doc\classdoc\classref\vrf_the_navigation_a_p_i.html
("Navigation API" conceptual page - also at
docs.mak.com/api/vrforces5.2/classref/vrf_the_navigation_a_p_i.html):
  "myQuery = navArea->findPathToLocation(
    "lifeform",          // some Nav Areas support multiple profiles...
    localStartPosition,  // starting position
    localEndPosition,    // ending position
    useAbstractGraphs,   // setting to true can speed up long path planning
                          // queries
    localPointToAvoid,   // optional position to avoid
    avoidanceRadius);    // radius (m) to go around point to avoid"

  This is the ONLY sentence in the entire Developer's Guide that
  acknowledges "long path planning queries" as a distinct case, and its
  only documented lever is useAbstractGraphs (default true - already on in
  the Lua default parameters, see below). No document explains what an
  "abstract graph" is beyond this one inline comment, and none states what
  happens differently when useAbstractGraphs is false vs. true beyond
  "speed."

C:\MAK\vrforces5.2d\doc\classdoc\classref\struct_dt_lua_nav_find_path_parameters.html
(the Lua findPathToLocation "parameters" table argument):
  Public Attributes:
    "bool useAbstractGraphs = true"
    "bool useChannels = false"
    "double channelRadius = 4.0"
    "DtLocation3D avoidLocation"
    "double avoidanceRadius = 0.0"
    "DtString navigationProfile"
    "DtActiveBotNavInterface * activeNavIf = nullptr"
    "DtNavPathEvalFunction evalFcn = DtNavPathEvalFunction(0, 0)"

  No member here bounds search distance, sector count, or ties to
  gamewareMemorySize/gamewareQueryTimeBudget. There is no "maxDistance" or
  "timeout" field on this struct.

C:\MAK\vrforces5.2d\doc\classdoc\classref\class_dt_nav_area.html
(DtNavArea::findPathToLocation itself):
  "virtual DtNavAreaQuery::Ptr DtNavArea::findPathToLocation(const DtString
  &profile, const DtVector &start, const DtVector &destination, bool
  useAbstractGraphs = true, bool useChannels = false, double channelRadius
  = 4.0, const DtVector &locationToAvoid = DtVector(), double
  avoidanceRadius = 0, DtNavBot::NavBotPathLocationEvalFcn costEvalFcn = 0,
  void *costEvalFcnUsrData = 0)"
  "Returns a query that finds a path to the specified location from the
  specified start point."

  That one-line doc comment is the entire documented contract - no
  enumeration of failure modes, no distance limit, no mention of sector
  loading.

C:\MAK\vrforces5.2d\doc\classdoc\classref\class_dt_nav_area_query.html
(DtNavAreaQuery - the async query object findPathToLocation returns; THIS
is where the documented failure vocabulary actually lives):
  "virtual bool complete()" - "Returns true when the query has completed."
  "virtual bool success()" - "Returns true if the query completed
  successfully."
  "virtual bool navDataChanged()" - "Returns true if the nav data has
  changed during this query."
  "virtual bool outOfWorkingMemory()" - "Returns true if the query failed
  due to a lack of working memory."
  "virtual bool hadComputationError()" - "Returns true if the query failed
  due to a computation error."
  "virtual std::vector<DtVector> results()" - "A list of locations that may
  be the result of this query."

  This is the closest thing to a documented answer to "when does it return
  an empty path": the query can fail two documented ways -
  outOfWorkingMemory() (directly tied to gamewareMemorySize - a query on a
  huge sectorized area over a 9-20 km goal is exactly the scenario the
  Users Guide's gamewareMemorySize description calls out: "Increasing this
  limit can allow for path plans on larger navigation areas") and
  hadComputationError(). Neither the Lua wrapper (findPathToLocation) nor
  the "Planned nav path has not enough (0) points." message documented
  anywhere expose which of these two flags (or plain success()==true with
  an empty results() list, meaning "no route exists") produced the 0-point
  outcome - that distinction is not surfaced to Lua in any documented API,
  and is a genuine documentation gap (see closing section).

C:\MAK\vrforces5.2d\doc\classdoc\classref\struct_dt_nav_area_config.html
(DtNavAreaConfig - the runtime config struct that gamewareMemorySize/
gamewareQueryTimeBudget feed into):
  "int workingMemorySize" - "Size (in MB) used when initializing working
  memory blocks within GameWare. Note that multiple blocks of this size are
  allowed, so this is not an overall limit."
  "double queryTimeBudget" - "The time budget per frame for path queries in
  ms. Default value is 1."

  This is the SECOND independent doc source (after the Users Guide Table
  76) confirming the true default query time budget is 1 ms, not the 5.0 ms
  the shipped vrfSim.mtl comment claims. Two-for-two against the shipped
  file's comment.


5. Release Notes and First Experience Guide
-----------------------------------------------
C:\MAK\vrforces5.2d\doc\VRF5.2ReleaseNotes.pdf (3,672-line text dump,
searched for "gameware"/"nav mesh"/"navigation area"/"sector"/"query
budget"/"memory"):

  VRF-9225 / VRF-9238, "Major VRF sim performance penalty for remote
  entities inside navigation areas":
    "The application no longer loads navigation areas when remote entities
    are in them, only when local entities are present in the area. This
    improves sim engine performance."
    This is a 5.2 behavior CHANGE to the lazy-loading rule described in
    Users Guide Appendix C (loadAllNavigationDataOnTerrainLoad /
    p.1280-1281): as of 5.2, a remote (reflected) entity sitting inside a
    navigation area no longer forces that area's nav data to load; only a
    LOCAL entity does. Not directly about long-distance queries, but
    directly about when nav data for an area is resident at all - relevant
    background for "New Primary nav area: ... loaded and recognised."

  VRF-9255, "VRF Launcher does not respect updated appData paths for
  plug-in selection":
    "The VR-Forces Launcher now supports the --appDataDir command-line
    option."

  VRF-9265 / VRF-9270, "--appDataDir is not processed until after
  attempting to load config files and many instances of hard-coded relative
  paths":
    "Fixed to ensure that the data files are loaded correctly when using
    this command-line option."
    (Both corroborate --appDataDir as the current, actively-fixed-in-5.2
    relocation mechanism cited in item 3.)

  VRF-8813, "Entities tasked to wander can get stuck on uneven terrain":
    "Fixed in third-party product, Gameware Navigation."
    Confirms Gameware Navigation is treated by MAK as a third-party,
    externally-fixed dependency for at least some navigation defects - MAK
    does not claim to control or fully document its internals, consistent
    with the shallow API-level documentation found in item 4.

  No entry anywhere in the Release Notes mentions gamewareMemorySize,
  gamewareQueryTimeBudget, or a distance/sector limit on path queries by
  name.

C:\MAK\vrforces5.2d\doc\VR-ForcesFirstExperience.pdf (2,361-line text dump):
  Zero matches for "gameware", "nav mesh"/"navmesh", "navigation area",
  "query" + "budget", or "sector" in any case. The First Experience Guide
  is a getting-started walkthrough and does not touch navigation
  internals at all.


6. Internet search
----------------------
All queries run via web search on 2026-09-14; results below are exhaustive
for what each query returned, not excerpts.

  "gamewareMemorySize" VR-Forces - no public hit. Results were generic
  MAK/VR-Forces marketing and specs pages
  (https://www.mak.com/mak-one/apps/vr-forces,
  https://www.mak.com/mak-one/support/help/specs/vr-forces); none mention
  this parameter.

  "gamewareQueryTimeBudget" VR-Forces - no public hit. Same generic result
  set as above plus unrelated VR-gaming pages.

  "Planned nav path has not enough" VR-Forces - no public hit for the
  literal error string. The only VR-Forces-specific hit was
  https://www.mak.com/learn/blog?view=article&id=178 ("Tech tip:
  Configuring VR-Forces path planning to support cover points"), which is
  about a DIFFERENT symptom (not enough pre-generated cover points for
  soldiers taking cover along a wall) and is unrelated to long-distance
  goal path failures; noted so it is not mistaken for a match.

  "VR-Forces nav mesh large area path planning fails" (site:docs.mak.com) -
  no public hit; docs.mak.com does not appear to expose a separate
  knowledge-base/FAQ search surface distinct from the classref/PDF mirror
  already covered in items 2 and 4.

  General search for a MAK community/support forum thread on nav-mesh
  path-planning failures over long distances - no public hit; no
  community.mak.com or mak.com forum thread was found discussing this
  symptom or these two parameters.

  Spot-check: docs.mak.com/api/vrforces5.2/classref/class_dt_nav_area.html
  fetched directly and confirmed to carry the identical
  findPathToLocation() one-line doc comment quoted in item 4, verifying the
  online mirror matches the installed copy for this API (it does NOT
  additionally document DtNavAreaQuery's outOfWorkingMemory/
  hadComputationError/success on that same page - that content is on the
  separate class_dt_nav_area_query.html page, consistent with the local
  copy).


Synthesis
=========
Across all four documentation sources, the model that governs a distant
findPathToLocation query on a large sectorized area is thin and mostly
inferential, not spelled out end to end anywhere. What IS documented,
assembled into one picture: GameWare Navigation (a third-party, MAK-
described-as-external AI middleware) runs an A* search per query inside a
per-query "working memory" block whose size is capped by gamewareMemorySize
(16 MB shipped) and whose per-frame CPU allowance is capped by
gamewareQueryTimeBudget (1 ms true default per two independent doc sources,
though the shipped vrfSim.mtl comment and value both claim 5.0 ms - a
genuine mismatch); the DtNavAreaQuery object that findPathToLocation
returns exposes exactly two documented failure predicates,
outOfWorkingMemory() and hadComputationError(), neither of which is wired
into any documented Lua-visible diagnostic, so a Lua-side "0 points" result
is consistent with (but not provably caused by) working-memory exhaustion
on a search that has to traverse many of the area's 8,856 sectors to reach
a 9-20 km goal. The only API lever explicitly tied to "long path planning
queries" is useAbstractGraphs (on by default), described only as something
that "can speed up" such queries, with no explanation of the mechanism or
of what happens when it is insufficient. Separately, the Users Guide
documents the area itself as roughly 5.5x the size of the documented
default-precision maximum (20x20 km vs. our 41x54 km), states sectorizing
is mandatory to reach "maximum size" areas at all, and states in one
sentence that "path planning over long distances may be slower" when
sectorized - but never connects that sentence to memory or time-budget
exhaustion, never states a distance or sector-count threshold, and never
describes per-sector (as opposed to per-area) lazy loading. Net: the
documentation supports "raise gamewareMemorySize and/or
gamewareQueryTimeBudget and re-test" as the first documented, sanctioned
experiment, but does not document that this WILL fix a 0-point result at
9-20 km, because the docs never state a working-memory or time-budget
number that maps to a 9-20 km query on an 8,856-sector area.

Sanctioned way to change the two settings so edits survive a reinstall:
launch the sim engine with --appDataDir pointing at a directory outside
C:\MAK\vrforces5.2d (Users Guide Table 11, p.178: default is ./appData
relative to the install tree, which the Uninstalling sections, p.100-101,
confirm is deleted/overwritten by the product installer/uninstaller), and
put the edited settings\vrfSim\vrfSim.mtl under that external directory;
optionally add --sharedSettingsDirectory / --useUserSettingsDirectory
(p.183/185) to push into the OS per-user profile location instead. Editing
C:\MAK\vrforces5.2d\appData\settings\vrfSim\vrfSim.mtl in place has no
documented protection across a reinstall.

What the docs do NOT say (gaps, not inferred, called out explicitly per the
task's request):
  - No document states a maximum search distance, sector-hop count, or
    area-size ceiling for findPathToLocation / DtNavArea::findPathToLocation
    - only the unrelated 20x20 km "maximum navigation AREA to generate"
    figure exists, which is about generation, not querying.
  - No document states what numeric gamewareMemorySize or
    gamewareQueryTimeBudget value is sufficient for a given area size,
    sector count, or query distance. Both parameters are described only in
    relative terms ("increasing this limit can allow...", "the higher the
    value, the more...").
  - No document ties outOfWorkingMemory() or hadComputationError() to any
    Lua-visible signal, message, or the specific "Planned nav path has not
    enough (0) points." string - that string does not appear in any
    installed or online document searched.
  - No document explains what useAbstractGraphs actually does algorithmically
    (hierarchical/coarse graph? precomputed portals? nothing is defined) -
    it is described only by its effect ("can speed up long path planning
    queries").
  - No document describes per-sector, on-demand loading of navigation data
    during a live query; the only documented lazy-loading mechanism
    (loadAllNavigationDataOnTerrainLoad, and its 5.2 refinement in VRF-9225)
    operates at the whole-navigation-area granularity, gated on local (not
    remote) entity presence, not at goal-approach or per-sector granularity.
  - No document states whether "Planned nav path has not enough (0)
    points." originates from success()==true with empty results() (a
    real, computed "no route exists" answer) or from a failure predicate
    (memory/computation) being swallowed before reaching Lua. This
    distinction is load-bearing for what to try next and neither the
    Developer's Guide nor the Lua reference resolves it.
