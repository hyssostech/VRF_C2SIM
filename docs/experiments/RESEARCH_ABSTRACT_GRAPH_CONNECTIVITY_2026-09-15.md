# Abstract-graph connectivity research (Opus, 2026-09-15 00:15-00:55Z; docs > samples > community; read-only). Supervisor notes:
# TWO FINDINGS - (1) UG52 66.2 p1276: 'The maximum size for a navigation area using the default values is 20 km by 20 km'; MojaveCOA
# (54 x 41 km) is 5.5x over it, MojaveAO20 (20.08 x 19.95 km) sits exactly on it; (2) the generation log's per-sector ABSTRACT GRAPH
# POST PROCESS REPORT gives a connectivity ratio nbrs/(nodes-1): 254 of 8,856 COA sectors are fragmented (< 0.5), concentrated in the
# north-west = 1-35's lane (destack start 0.11 -> 0/6 short plans; N2d start 0.29 -> 4/6; east 1.00 -> all); the SAME ground on AO20 has 0
# fragmented sectors. Product consequence (STP-802/803): cap every generated area at 20 x 20 km and tile the AO. Probe V7 = N2d on the
# AO20 terrain. Carried: a second mechanism (same-second success/refusal for co-located vehicles in G7b run A) is NOT explained by this - ticketed STP-819 (2026-09-15 03:28Z): offline test first (query-time positions vs the sector table), one staggered-dispatch run only if inconclusive.

# FINDINGS: why the WEST refuses and the EAST plans on NavArea-ground-platform MojaveCOA
Opus research executor, 2026-09-14. READ-ONLY: nothing launched, no build, no vendor sim log
opened, no file written outside this scratchpad. Tier HEAVY (the output decides the next probe).

## 0. HEADLINE - five things, in order of load

H1. **We are 5.5x over a documented vendor limit, and the area that works is exactly at it.**
    VR-Forces 5.2 Users Guide, 66.2 Navigation Areas, p1276, verbatim:
      "The maximum size of the area on which you can generate navigation data depends on the
       raster precision value used to generate it. If you reduce the precision of navigation
       data, creating a sparser graph, the size of the maximum area increases. THE MAXIMUM SIZE
       FOR A NAVIGATION AREA USING THE DEFAULT VALUES IS 20 KM BY 20 KM. (Raster precision is
       configured in ./appData/settings/vrfSim/navigationProfiles.mtl.)"
    MojaveCOA = 54.4 x 41.2 km at the DEFAULT raster precision 0.2 -> 5.5x the maximum by area,
    2.7x on the long axis. MojaveAO20 = 20.08 x 19.95 km = the documented maximum, to the metre.
    Corroboration: navigationProfiles.mtl line 25, (max-cells 1000) - "Maximum number of
    cells along each axis, used to determine max area length/width in the gui". MojaveCOA is
    1,267 cells on x.

H2. **There is a per-sector abstract-graph connectivity number in our own generation log, it is
    regional, and it lines up with every run we have.** Each sector's ABSTRACT GRAPH POST
    PROCESS REPORT prints `Average Node Count` and `Average Neighbor Node Count`. In a fully
    connected sector graph every node reaches every other, so
    ratio = nbrs / (nodes - 1) == 1.00. Over scratchpad\navtest\log\gen-COA.log
    (8,856 sector blocks parsed by scratchpad\agresearch\parse_gen.py):
      - 254 sectors (2.87 %) have ratio < 0.5. They form SPATIAL blobs, concentrated in the
        NW quadrant - which is 1-35's lane.
      - 1-35 destack start (34.658442/-116.740092) -> sector (54,79): 46 nodes, mean 5
        neighbours, **ratio 0.11**, NavData 475 kB.
      - N2d start (34.658134/-116.745512) -> sector (53,79): **ratio 0.29**, 386 kB.
      - The V1 goal (34.651212/-116.811637) -> sector (40,78): **ratio 0.07**, 461 kB.
      - 9 of the 16 sectors on the destack-start -> V1 leg are below 0.5.
      - EAST lane (G7b/G7c, 1222.MechPlt 34.612956/-116.600487 westward): start sector (81,69)
        **ratio 1.00**, 56 kB; 16 of the 17 sectors crossed are >= 0.65; one is 0.39.
    DOSE RESPONSE against the runs: ratio 0.11 -> 0/6 short (30 m) goals planned (N2b, N2c);
    ratio 0.29 -> 4/6 planned (N2d); ratio 1.00 -> everything planned (east; AO20).

H3. **The same ground generated clean under the 20 km area and shattered under the 54 km area.
    So it is not the terrain.** 1,505 MojaveCOA sectors have their centre inside MojaveAO20's
    footprint. 119 of them are fragmented on COA; **ZERO of 1,600 AO20 sectors are below 0.5**
    (median 1.00, only 0.1 % below 0.95). Same profile, same cell-size 43, same raster 0.2,
    same 11-cells-per-sector geometry, same 4 abstract graphs per sector, same terrain.
    Example, identical ground: COA (41,75) ratio 0.02, 525 kB, 113,854 input triangles
    vs AO20 (0,28) ratio 1.00, 57 kB, 98,931 triangles. 15 % more input triangles, 9x the
    NavData, and a shattered abstract graph. Area-wide medians are the SAME (COA 62 kB /
    98,852 tris vs AO20 60 kB / 98,787 tris), so the defect is local to ~3 % of sectors, not a
    uniform degradation.

H4. **The "regional" claim in the record is real but was measured against a confounded pair, and
    the "flat query ceiling of 23 m on this area" is an artefact.** G6's 23 m figure comes from a
    goal distribution with nothing between 120 m and 500 m and a next-shortest goal of 9.3 km
    (MESH_QUERY_VS_DISTANCE sec 4b). G7b run A, the FLAT query on the same MojaveCOA area,
    planned 1,889.6 m with 226 points (4 of 4 members) and 5,013.7 m with 718 points (1 of 4).
    So the flat query is alive on COA in clean sectors. Separately, the west runs (N2b/c/d) used
    COA-STP1_Initialization.xml while every east run used R9_Mojave_Lean_Initialization.xml;
    region and scenario size are confounded in that pair. The fragmentation map separates them
    without a run.

H5. **The abstract-graph flag can never be the cause of a refusal, and the failure reason is not
    reachable from Lua.** Gameware, in the shipped vrfNavigation.dll string table:
    "Start and/or destination are not covered by an AbstractGraph. m_abstractGraphTraversalMode
    is forcibly set to PATHFINDER_DO_NOT_TRAVERSE_ABSTRACT_GRAPHS" and "Propagation over
    AbstractGraphs is currently supported only when starting and ending on a NavMesh.
    abstractGraphTraversalMode is forced set to PATHFINDER_DO_NOT_TRAVERSE_ABSTRACT_GRAPHS".
    The library degrades to the flat query rather than failing. Every such message goes to the
    VENDOR SIM LOG, which this executor may not open - see sec 9.

---

## 1. SOURCES CONSULTED (every one read in this session unless marked)

### 1.1 Autodesk Navigation SDK (Gameware Navigation), live help
- Creating AbstractGraphs -
  https://help.autodesk.com/cloudhelp/ENU/Navigation-SDK-Help/nav-help/navdata/creating_abstractgraphs.html
  and the 2016 mirror .../files/GUID-11DD2A34-AD33-4973-B5FB-190F16C1A3C9.htm
- Sectorizing the Terrain -
  https://help.autodesk.com/cloudhelp/ENU/Navigation-SDK-Help/nav-help/navdata/sectorizing_the_terrain.html
- gwnavruntime/abstractgraph/abstractgraph.h SOURCE -
  https://help.autodesk.com/cloudhelp/2016/ENU/Navigation-SDK-Help/api/abstractgraph_8h_source.html
- abstractgraphcellgrid.h SOURCE - .../api/abstractgraphcellgrid_8h_source.html
- abstractgraphdatagenerator.h SOURCE - .../api/abstractgraphdatagenerator_8h_source.html
- abstractgraphgenerator.h SOURCE - .../api/abstractgraphgenerator_8h_source.html
- pathfinderabstractgraphtraversalmode.h SOURCE - .../api/pathfinderabstractgraphtraversalmode_8h_source.html
- database.h SOURCE - .../api/database_8h_source.html
- File list - https://help.autodesk.com/cloudhelp/2016/ENU/Navigation-SDK-Help/api/files.html
- (Carried from the earlier pass, not re-fetched: AStarQuery Options / propagation box;
  Kaim namespace AStarQueryResult; Forbidding, Avoiding and Preferring NavTags - see
  docs/experiments/NAVDOCS_ABSTRACT_GRAPHS_AND_SLOPE_2026-09-14.md sec 8.)

### 1.2 MAK 5.2 documentation
- C:\MAK\vrforces5.2d\doc\VRFUsersGuide.pdf 66.2 p1276, 66.2.1 p1277, 66.2.3 p1280, 66.3 p1281
  (read via the scratchpad extracts ug52_ch66.txt and ug52_nav_1277_1284.txt from the earlier
  pypdf page-range extraction - no 109 MB whole-file load).
- docs.mak.com/api/vrforces5.2/classref/class_dt_nav_area.html (fetched this session).

### 1.3 Installed 5.2d tree (exact paths, read-only)
- C:\MAK\vrforces5.2d\appData\settings\vrfSim\navigationProfiles.mtl (max-cells, area-depth /
  area-height, the whole ground-platform profile incl. feature-tag-volumes).
- C:\MAK\vrforces5.2d\appData\settings\vrfSim\vrfSim.mtl:378-445 (gamewareMemorySize,
  gamewareQueryTimeBudget, regenerateNavigationDataAtRuntime, numberOfNavQueueProcessingThreads,
  loadAllNavigationDataOnTerrainLoad, blockOnAsynchronousOperations).
- C:\MAK\vrforces5.2d\doc\luadoc\modules\vrf.html (findPathToLocation, generateRandomPoints).
- C:\MAK\SharedData\19\latest\TerrainData\navData\*\*.navGenConfig and *.navRuntimeConfig -
  all 33 shipped nav areas (the vendor reference table, sec 4.2).
- scratchpad\vrfNavigation_strings.txt (an earlier session's string dump of the shipped
  vrfNavigation.dll) and scratchpad\strings_vrfNavGenerator.exe.txt.

### 1.4 Our own records and data
- scratchpad\navtest\log\gen-COA.log (29.9 MB), gen-AO20.log, gen4.log.
- C:\Users\PAULOB~1\Temp\nav\cfg\NavArea-ground-platform Mojave{COA,AO20,Assembly}.navGenConfig
- C:\C2SIM\vrf-nav\navData\MAK Earth (online)\NavArea-ground-platform MojaveCOA.navRuntimeConfig
- docs/experiments: NAVDOCS_ABSTRACT_GRAPHS_AND_SLOPE, PREREG_N1_N2_CORRIDOR_SLOPE (secs 1, 3.3,
  4.1, 5.1-5.6, 10.1-10.3, 11), G6_RESULTS, G7B_G8_RESULTS, MESH_QUERY_VS_DISTANCE.
- data/R9_Mojave_Lean_Initialization.xml, data/PROBE_G7b_CrossSector_Order.xml,
  data/PROBE_RIDGE_1-35_Order.xml.
- My scripts, all in scratchpad\agresearch\: parse_gen.py, parse_ao20.py, cmp.py, cover.py,
  cover2.py, lanes.py, h2t.py; outputs sectors.json, sectors_ao20.json, map.txt.

---

## 2. Q1 - GAMEWARE ABSTRACT GRAPHS: HOW THEY ARE BUILT, LINKED, AND HOW A QUERY FAILS

### 2.1 Structure (DOCUMENTED, primary source, quoted)
abstractgraph.h:

    class AbstractGraphNodeLink {
      enum NavMeshLinkStatus {
        NavMeshLink_NoNavMesh = 0, // No Navmesh is loaded underneath, i.e. no ActiveCell at the CellPos of the node
        NavMeshLink_Inside    = 1, // A NavMesh is loaded, and the Node is on the NavMesh
        NavMeshLink_Undefined = KyUInt32MAXVAL };
      bool CanTraverse() const { return m_pairedNodeIdx.IsValid(); }
      LoadedAbstractGraphNodeIdx m_pairedNodeIdx; //< gives the node in a neighbor graph, this node is paired to
      NavMeshLinkStatus m_navMeshLinkStatus; };
    class AbstractGraph {
      KyFloat32 GetNeighborCost(AbstractGraphNodeIdx from, AbstractGraphNodeIdx to) const;
      bool IsNodePaired(...) const;
      enum DebugDisplay { DebugDisplay_AbstractEdges, DebugDisplay_CellBoxCoverage, DebugDisplay_All };
      AbstractGraphNodeLink* m_links; // for each graph nodes gives link information about the node in the neighbor AbstractGraph, and the Navmesh
    };

Two distinct links per node: (i) a PAIRING to a node in the NEIGHBOUR graph - CanTraverse() is
exactly m_pairedNodeIdx.IsValid() - and (ii) a NavMesh link status that is NavMeshLink_NoNavMesh
when no ActiveCell is loaded under the node.

abstractgraphcellgrid.h: AbstractGraphCellGrid(Database*) : m_altitudeTolerance(0.5f);
void EnlargeGrid(const CellBox&); AbstractGraph* InsertAbstractGraph(AbstractGraphBlob*);
void UpdateNavMeshLinkStatus(CellBox); KyResult CheckGenerationParameters(const NavMeshGenParameters&).
So pairing and NavMesh link status are established at RUNTIME on insert, with a 0.5 m altitude
tolerance.

abstractgraphdatagenerator.h: AbstractGraphDataGenerator() : m_boxExtentInCellSize(10);
SetExtentInNumberOfCells(KyInt32); GenerateOneAbstractGraphFromCellBox(const CellBox&) with the
comment "Fulfill m_tmpAbstractGraphBorderAsCellPos with CellPos defining the border of the
AbstractGraph (different from the boundary of the cellBox)".
abstractgraphgenerator.h: CreateNodes(cellPosBorder, wantedBoundariesPerCells);
m_graphNodeTriangles "used to store the triangles on which an abstract graph node lies on";
m_graphNodeIdxToGraphCellFloorIndices "associates an abstract graph node to the indices of its
abstract graph cell and floor"; ComputeAllCosts().
Autodesk: m_workingMemorySizeLimit "gives the size limit (in Bytes) of the WorkingMemory, which is
used to run MultiDestinationPathFinderQuery when computing the edge costs in an AbstractGraph".

THEREFORE: an abstract graph's EDGES are NavMesh paths computed at generation time, inside one cell
box, under a working-memory budget. If two border nodes are not connected through the mesh inside
that box - or the budget runs out - there is no edge. That is exactly what
"Average Neighbor Node Count" measures, and why it collapses in 254 COA sectors (sec 3).

Arithmetic that ties the default to our data: default extent 10 cells; our sectors are 11 cells of
43 m; 11 / 10 -> a 2x2 arrangement -> "AbstractGraph Count : 4" in every regular COA and AO20
sector. The anomalous last row/column sectors confirm it: (i,81) is 11 x 68 cells -> 14 graphs;
(107,j) is 90 x 11 -> 18; (107,81) is 90 x 68 -> 63.
The matching vendor message exists in the DLL and never fired for us because 11 >= 10:
"Edge size in cells of sector " ... " is less than the abstract graph extent size of " ...
". Changing abstract graph extent to ".

### 2.2 How a query fails, and the result codes (DOCUMENTED)
From the shipped vrfNavigation.dll string table (scratchpad\vrfNavigation_strings.txt):

| string (verbatim) | what it means |
|---|---|
| Start and/or destination are not covered by an AbstractGraph.m_abstractGraphTraversalMode is forcibly set to PATHFINDER_DO_NOT_TRAVERSE_ABSTRACT_GRAPHS | no abstract node at either end -> SILENT FALLBACK to the flat query |
| Propagation over AbstractGraphs is currently supported only when starting and ending on a NavMesh. abstractGraphTraversalMode is forced set to PATHFINDER_DO_NOT_TRAVERSE_ABSTRACT_GRAPHS | off-mesh start/end -> also falls back to flat |
| This traversal has reached its maximum working memory size for storing astarNodes and corresponding AbstractGraphNodeRawPtr | abstract traversal ran out of working memory |
| Not enough working memory for storing the AbstractGraphNodeRawPtr associated to an AstarNode | same, per node |
| Failed to allocate %i bytes for the AbstractGraph. it won't be inserted / Failed to insert AbstractGraph | a graph can be DROPPED at load |
| Difference in generation parameters prevents to load the AbstractGraphBlob with the data currently in the Database | mismatched NavMeshGenParameters -> graph dropped |
| NavData should be made of one ond only one NavMesh, overlap are not supported by AbstractGraphs | why our log says "Generation with cellBox: 0 bytes of overlap data" on all 8,856 sectors |
| No AbstractGraph to be saved for sector [%s] | the 35 warnings in gen-COA.log (+11 silent) = 46 sectors with no graph, all in the SE, none on either lane |
| Invalid AltitudeTolerance value in NavMeshGenParameters | the 0.5 m tolerance is a generation parameter, checked at insert |

PathFinderAbstractGraphTraversalMode has exactly two values:
PATHFINDER_DO_NOT_TRAVERSE_ABSTRACT_GRAPHS = 0, PATHFINDER_TRAVERSE_ABSTRACT_GRAPHS.

Result codes present in the shipped 5.2 DLL: ASTAR_NOT_INITIALIZED, ASTAR_NOT_PROCESSED,
ASTAR_PROCESSING_TRAVERSAL, _TRAVERSAL_DONE, _ABSTRACT_PATH, _REFINING_INIT, _REFINING_RESETCOST,
_REFINING, _PATHCLAMPING_INIT, _PATHCLAMPING, _PATHBUILDING, _CHANNEL_INIT, _CHANNEL_COMPUTE,
ASTAR_DONE_PATH_FOUND, ASTAR_DONE_PATH_NOT_FOUND, ASTAR_DONE_ERROR_LACK_OF_WORKING_MEMORY,
ASTAR_DONE_START_OUTSIDE, ASTAR_DONE_END_OUTSIDE, ASTAR_DONE_START_NAVTAG_FORBIDDEN,
ASTAR_DONE_END_NAVTAG_FORBIDDEN, ASTAR_DONE_DEST_IS_START_NO_PATH, ASTAR_DONE_NAVDATA_CHANGED,
ASTAR_DONE_COMPUTATION_ERROR, ASTAR_DONE_COMPUTATION_CANCELED, ASTAR_DONE_CHANNELCONFIG_ERROR,
ASTAR_DONE_CHANNELCOMPUTATION_ERROR.

So "no NavData at start", "no NavData at destination", "no path" and "out of working memory" ARE
distinguished (START_OUTSIDE / END_OUTSIDE / PATH_NOT_FOUND / ERROR_LACK_OF_WORKING_MEMORY).
"Abstract graph disconnected" has NO code - the library forces the flat mode and logs a warning.
None of these is reachable from Lua (vrf:findPathToLocation returns an AsyncJob whose only
accessors are cancel and result), which is why our console can only ever say "(0) points".

---

## 3. THE MEASUREMENT - a headless connectivity diagnostic that already exists in our own data

Metric: per sector, ratio = Average Neighbor Node Count / (Average Node Count - 1).
1.00 iff every abstract node reaches every other; a mean out-degree of 5 among 46 nodes means the
sector's abstract graph is shattered into components of about six nodes.
Scripts: scratchpad\agresearch\parse_gen.py, parse_ao20.py, lanes.py; map at map.txt.

| population | n | ratio < 0.5 | median ratio | median navkb |
|---|---|---|---|---|
| MojaveCOA, all sectors | 8,856 | 254 (2.87 %) | 1.00 | 60.5 kB |
| MojaveAO20, all sectors | 1,600 | 0 (0.00 %) | 1.00 | 59.9 kB |
| COA sectors whose centre is inside the AO20 footprint | 1,505 | 119 (7.9 %) | 1.00 | 61.9 kB |
| the AO20 sectors over that same ground | 1,505 | 0 | 1.00 | 60.3 kB |

Predictors within COA (regular sectors): navkb > 400 kB -> 98 % fragmented (123/125);
navkb > 300 -> 73 %; input tris > 105k -> 27 %; tris <= 102k -> 0.5 %.
Clustering is SPATIAL, not generation-order: bad-bad adjacency is 155 in the generation-consecutive
direction (same i, j+1) and 146 across an 82-sector generation gap (same j, i+1). I tested and
FALSIFIED the "terrain-cache streak during a 3.3 h generation" reading with that number.

Lane profiles (lanes.py):

| lane | sectors crossed | ratio < 0.5 | start sector | goal sector | run outcome |
|---|---|---|---|---|---|
| EAST 1222.MechPlt 34.612956/-116.600487 westward | 17 | 1 (6 %) | (81,69) r=1.00, 56 kB | (65,69) r=0.65 | AG 8/8 at 5 km; flat 4/4 at 2 km |
| WEST destack start -> V1 | 16 | 9 (56 %) | (54,79) r=0.11, 475 kB | (40,78) r=0.07, 461 kB | AG 0/6 even at 30 m |
| WEST full route assembly -> V1 -> V2 -> V3 | 66 | 17 (26 %) | (57,81) r=0.69 | - | all long goals refused |

Sector indexing above is the GENERATOR'S OWN (from the log's "Sector (i,j): xMin ... yMax" rows),
not the 108x82-equal-division formula used in the earlier records. They differ: the generator's
sectors are 11 cells = 473 m wide, not 504 m, so the two index systems drift by up to four sectors
and one earlier "west" index maps to a sector that does not exist. The earlier claim "every sector
of the leg carries an abstract graph" is still TRUE in the generator's index (the 46 graph-less
sectors are all in the SE), but the CONNECTIVITY of those graphs was never measured until now.

---

## 4. Q2 / Q3 - THE MAK DOCUMENTS AND THE VENDOR'S OWN DATA

### 4.1 Users Guide chapter 66 (verbatim; whitespace restored from the text extract)
- 66.2 p1276: the maximum-size sentence quoted in H1.
- 66.2.1 p1277: "The disadvantages of using sectors are: - Increasing the number of sectors slows
  down generation of navigation data. - THE NAVIGATION DATA GENERATION PROCESS CAN FAIL IF ANY
  SINGLE SECTOR IS TOO LARGE, TOO COMPLEX, OR BOTH. However, this is also true if you generate
  navigation data for a very large terrain without using sectors. - PATH PLANNING OVER LONG
  DISTANCES MAY BE SLOWER. To generate a maximum size navigation area, you must use multiple
  sectors."
- 66.2.3 p1280: "If you are creating a large navigation area, we recommend that you always
  sectorize it. Generation of large unsectorized navigation areas sometimes fails." Dialog fields:
  Generate Full Terrain ("If the terrain is too large for this option, it is disabled"),
  Sectorize Navigation Area, Automatic Sector Size.
- 66.3 p1281: "MAK has recorded this taking one week to finish on a maximum size navigation area."
- "abstract graph" occurs NOWHERE in the 1,806 pages (established by the earlier pass).
- NOT FOUND anywhere in the UG, release notes or migration guide: any statement about
  abstract-data generation parameters, inter-sector abstract connectivity, a long-path limit, or
  how to diagnose a nav area.

### 4.2 The vendor's own shipped nav areas - what a COMPLETE generation looks like
Every .navGenConfig under C:\MAK\SharedData\19\latest\TerrainData\navData\ (33 areas). Largest
ground-platform ones:

| terrain | area | size m | tiles | cell | raster | cells/sector | sectors |
|---|---|---|---|---|---|---|---|
| MAK Earth (online) | ground-platform Thun | 10,080 x 10,080 | 49 x 49 | 41 | 0.200 | 5 x 5 | 2,401 |
| MAK Earth (online) | ground-platform Ala Moana | 2,610 x 2,610 | 30 x 30 | 43 | 0.200 | 2 x 2 | 900 |
| MAK Earth (online) | ground-platform Range220 | 2,064 x 2,064 | 24 x 24 | 43 | 0.200 | 2 x 2 | 576 |
| MAK Earth (online) | ground-platform Kilo2 | 1,440 x 2,070 | 11 x 16 | 43 | 0.200 | 3 x 3 | 176 |
| Ground_DB II | ground-platform | 2,250 x 2,250 | 7 x 7 | 43 | 0.200 | 7 x 7 | 49 |
| Brooklyn | ground-platform | 1,000 x 1,000 | 3 x 3 | 43 | 0.200 | 7 x 7 | 9 |

The LARGEST ground-platform nav area MAK ships is 10 x 10 km with 2,401 sectors of 5 cells. Every
shipped area uses 1-7 cells per sector; none uses 11. Ours is 54 x 41 km with 8,856 sectors of 11
cells. allow-abstract-data True and raster-precision 0.200000 in every one of them, so our profile
settings are vendor-standard; only the SIZE is not.
Every shipped area has its .navGenConfig beside its navData, and every one has
(enable-random-points True) in its runtime config - so ours is not anomalous there either.

### 4.3 vrfNavGenerator options (from its own string table; --help is documented as
"Displays usage information and exits", so running it would have been safe - I did not need to)
--terrain --config --runtimeConfigPath --outputPath --dataDir --sharedDataDir --sharedDataRoot
--sharedDataStem --appDataDir --userDataDir --verbose --navDataDir --regenAll --model
--logFileName --tracy.  --regenAll: "Specifies that all nav data should be regenerated in the
specified directory. Looks for all .navRuntimeConfig files and uses those to regenerate the nav
data."  THERE IS NO ABSTRACT-GRAPH OPTION AND NO WORKING-MEMORY OPTION.

DtRwNavGenConfig's complete setter list (demangled from the DLL): localExtentNW/NE/SW/SE,
tileCountX/Y, allowAbstractData, generateFullTerrain, generateTransitionPoints, pruneNoGoAreas,
rasterPrecision, cellSize, profilesToGenerate. The Gameware keys DoGenerateAbstractGraphs,
AbstractGraphExtentsInNumberOfCells and AbstractGraphWorkingMemorySizeLimit exist in the DLL under
a GlobalConfig / PostProcessParams blob, but VR-Forces exposes NONE of them. The only
abstract-graph levers we have are indirect: extent, tile-count, cell-size, raster-precision.

### 4.4 The shipped Lua, and the one connectivity accessor that IS reachable
ground-vehicle-move-to.lua:488 {useAbstractGraphs = false, useChannels = true, channelRadius = 4.0}
(unchanged from the earlier pass). The Lua doc for findPathToLocation adds one sentence that
matters: "The start and end locations must be in navigation areas."
NEW AND USEFUL: vrf:generateRandomPoints(parameters) accepts access_point - "If specified, the
random points generated will be tested for CONNECTIVITY to the access_point" - plus boundary,
min/max_distance_from_access_point, number_of_points, ground_clamp. That is a Lua-level, headless,
NavigationLab-free connectivity probe, and our area's .navRuntimeConfig already has
(enable-random-points True).
C++ equivalents on DtNavArea: validPosition(profile, localPos, navTag*, outPos*) - "Tests to see if
the position is on the nav mesh part of the nav area, i.e. on a traversable part ... outPos will be
filled in with the projected position of the input point onto the triangle found" - and
sectorAtLocation(profile, localPos). The class reference also shows the sector API is about
REGENERATION (sectorsQueuedForRegeneration, sectorsBeingRegenerated, requestNavDataGeneration),
not about streaming or loading.

---

## 5. Q4 - COMMUNITY: NOTHING, AGAIN
Searches run this session: Gameware Navigation OR Kynapse + AbstractGraph + pathfinding sectors
disconnected; Autodesk Navigation SDK + AbstractGraph + AbstractGraphCell class reference Kaim;
VR-Forces navigation area maximum size 20 km raster precision sectorize MAK.
Result: NOT FOUND. No forum, issue tracker, blog or paper discusses AbstractGraph connectivity
failures, sector-boundary connectivity, or VR-Forces nav-area limits. The only technical hits are
the Autodesk help itself - which does serve the runtime HEADER SOURCES, and that is where sec 2.1
comes from and is the single most valuable thing found this session - and MAK marketing pages.
This matches the earlier pass's six searches. Gameware Navigation was withdrawn in 2017; there is
no living community. Treat absence of evidence here as close to evidence of absence.

---

## 6. ANSWERS (a) - (e)

**(a) Is a REGIONAL abstract-graph disconnection a known/documented failure mode, and what
produces it?**
- Known/documented by the vendor: **NOT FOUND**. Neither MAK nor Autodesk documents regional
  abstract-graph disconnection, and no community source exists.
- Real in our data: **MEASURED**. Not as inter-sector disconnection but as INTRA-SECTOR
  fragmentation: 254 of 8,856 COA sectors have a mean abstract out-degree far below their node
  count, in spatial blobs, and the 1-35 lane sits in the worst of them (sec 3).
- What produces it: **INFERRED**, two mechanisms consistent with the primary sources.
  (i) Abstract edges are MultiDestinationPathFinderQuery results inside one cell box under
  AbstractGraphWorkingMemorySizeLimit; the fragmented sectors are exactly the ones carrying 6-9x
  the NavData (navkb > 400 -> 98 % fragmented), which is where that budget would bite.
  (ii) The same sectors' meshes are genuinely more shredded (more triangles, more tags), so some
  node pairs may be unreachable through the mesh. Not exclusive; our data cannot separate them.
  What our data DOES settle is that it is NOT the terrain shape: the identical ground under
  MojaveAO20 produced 57 kB and a perfect graph (H3).

**(b) Is the missing .navGenConfig consequential for the RUNTIME query, or only for regeneration?**
**DOCUMENTED - only for regeneration.** vrfNavGenerator --regenAll and DtNavArea's regeneration API
are its consumers; DtRwNavGenConfig is a generation-side type; the runtime path (nav-data-path,
extents, offset, profile entries) comes entirely from the .navRuntimeConfig, which we do have.
Two caveats: (1) regenerateNavigationDataAtRuntime is 1 by default and the ground-platform profile
lists "entity" among regenerate-for-dynamic-terrain-types, so runtime regeneration IS armed and
will be unable to run; (2) every vendor area ships the file, so writing it is free correctness -
copy C:\Users\PAULOB~1\Temp\nav\cfg\NavArea-ground-platform MojaveCOA.navGenConfig next to the
navData. G7c-gate planned 8/8 without it, so it is NOT the blocker (already filed as STP-803).

**(c) Is there a documented way to DIAGNOSE connectivity headless?**
**YES - three, and one of them is already done.**
1. The generator's own report, offline, no run (sec 3): Average Node Count / Average Neighbor Node
   Count per sector. Cheap, already computed, and it should become a gate in tools/navdata (fail
   the generation if any sector on a planned lane has ratio < 0.9).
2. vrf:generateRandomPoints{ access_point = <start>, number_of_points = N, boundary = ... } from an
   SMS script override - the Lua doc says the points are "tested for connectivity to the
   access_point". Headless, no C++, no NavigationLab, and it needs only the override mechanism we
   already have at C:\C2SIM\vrf-sms.
3. The vendor sim log. Every Gameware warning in sec 2.2 is printed there. A targeted grep -
   counts and the matched Gameware lines only, never attaching or paging the file - would say
   directly whether graphs failed to insert, whether working memory ran out, or whether the query
   fell back to flat. THIS EXECUTOR IS NOT PERMITTED TO OPEN THOSE LOGS; it is the single cheapest
   decisive read available and it needs no run.
NOT available: a query result code from Lua (sec 2.2); a connectivity dump from the generator;
anything in NavigationLab that is not a GUI.

**(d) What single change to GENERATION or to the QUERY do the documents predict would make the
western legs plan?**
**GENERATION, and the documents name it: bring the nav area inside the documented maximum.**
UG52 p1276 says the maximum is 20 x 20 km at the default raster precision and that reducing the
precision raises the maximum. MojaveCOA is 5.5x over; MojaveAO20 is exactly at it and has zero
fragmented sectors over the same ground. So the predicted fix is to TILE THE AO INTO <= 20 x 20 km
GROUND-PLATFORM NAV AREAS (or, if one area is required, raise raster-precision until 54 x 41 km is
within the maximum - but the UG gives no formula and coarser nav data is a fidelity cost, so tiling
is the supported move).
NO QUERY CHANGE is predicted to work: the abstract flag already degrades safely to flat (H5), and
the propagation box is not settable for a vehicle (the earlier pass's amendment, still standing).
Second-order, zero-regeneration, worth doing anyway: set (enable-random-points False) in the
MojaveCOA .navRuntimeConfig - the profile comment says "This uses considerably more memory at
runtime" and that the runtime config takes precedence, and the shipped ground-platform profile's
own default is False. This does not target connectivity; it targets the runtime memory that
"Failed to allocate %i bytes for the AbstractGraph. it won't be inserted" needs.

**(e) What does the flat query's "~23 m ceiling on this area but 2 km on the 20x20 km area" imply
under the documented propagation-box model? What differs - sector count, abstract extent, cell
size?**
**The premise needs correcting first (H4).** There is no 23 m ceiling on MojaveCOA: G7b run A
planned 1,889.6 m (226 points) 4/4 and 5,013.7 m (718 points) 1/4 with useAbstractGraphs=false on
this very area. G6's 23 m figure is a goal-distribution artefact - its next shortest goal after
120 m was 9.3 km.
WHAT DOES NOT DIFFER between the two areas: cell size (43 m both), raster precision (0.2 both),
sector size (11 cells = 473 m both), abstract graph extent (the Gameware default 10 cells, hence
"AbstractGraph Count : 4" per regular sector in both), profile, terrain, and the overlap regime
(0 bytes in both, because "overlap are not supported by AbstractGraphs").
WHAT DOES DIFFER: total extent (54.4 x 41.2 km vs 20.1 x 20.0 km; 5.5x the documented maximum vs
exactly the maximum), sector count (8,856 vs 1,600), total NavData, and - the consequence - 254
fragmented sectors vs zero.
UNDER THE PROPAGATION-BOX MODEL the observation is only half-explained: a 200 m corridor around the
chord cannot be satisfied where the mesh inside it is shredded, so the flat query fails in the
fragmented west; but the box is identical on both areas and cannot by itself produce a same-chord
difference between them. The box is a NECESSARY constraint, not the DIFFERENTIATOR. The
differentiator in our data is the generated NavData quality, and the timing supports it:
MESH_QUERY_VS_DISTANCE sec 7 measured the COA refusals returning in 0.4-1.3 s - the same time as a
23 m success and FASTER than AO20's 10 km successes (2.4-4.2 s). A REFUSAL FASTER THAN A SUCCESS IS
NOT AN EXHAUSTED SEARCH; IT IS AN EARLY NO.

---

## 7. THREE CANDIDATE CAUSES, RANKED, EACH WITH ITS ONE FALSIFIER

**C1 (most probable). The 41 x 54 km area exceeds the documented 20 x 20 km maximum, and its
generation produced fragmented abstract graphs in ~3 % of sectors, concentrated in the west; the
1-35 start sector (ratio 0.11) and its goal sector (ratio 0.07) are among the worst, the eastern
lane is clean (1.00), and the same ground under the compliant 20 km area is clean (0 of 1,600).**
- Single falsifying observation: run the western lane, unchanged, against MojaveAO20, whose navData
  and terrain copy both still exist on disk. If the 30 m slot moves still refuse 6/6 and a 2 km
  goal inside AO20 still refuses, C1 is dead. (This is the probe, sec 8.)
- Second, offline falsifier: if a fresh 20 x 20 km generation over the same western ground also
  produces sectors with ratio < 0.5, size is not the operative variable.

**C2. Runtime failure to insert or traverse the abstract graphs on an oversized area** - the
"Failed to allocate %i bytes for the AbstractGraph. it won't be inserted" / "This traversal has
reached its maximum working memory size" path. The western sectors carry the largest graphs on the
area (400-530 kB in the lane, 1-3 MB in row 81) and the area holds ~2.3 GB.
- Single falsifying observation: grep the run's vendor sim log for AbstractGraph, working memory,
  not covered by an AbstractGraph. ZERO HITS FALSIFIES C2 OUTRIGHT. No run needed. Counts and the
  matched Gameware lines only; never attach the file.
- Note against C2: G8 raised gamewareMemorySize 16 -> 128 and changed nothing - but the mtl says
  that value is PER BLOCK, so it is not a monotone knob and the null is weak evidence either way.

**C3. The 1-35 start positions sit on an isolated mesh island** (the record's surviving (L)
reading). 0/6 short goals at the destack start vs 4/6 500 m away is real. Under C1 this is not a
rival but a CONSEQUENCE - fragmentation is what makes islands - so C3 becomes the primary cause
only if C1's probe refuses.
- Single falsifying observation: from the N2d spot, where 4/6 short goals DID plan, issue a 1-2 km
  goal along the lane. If it plans, the start is on a connected island and the failure is a
  corridor property, not a start property; if it refuses, the island is under 1 km across. This
  costs nothing extra - the probe in sec 8 measures it as a by-product.

**Explicitly refuted this session; do not re-open:**
- "The northern/eastern margin of the declared extent has no NavData." I derived it from
  108 sectors x 11 cells < 1,267 cells and then FALSIFIED IT MYSELF: the last row and column
  sectors absorb the remainder ((i,81) covers cells 412..479; (107,j) covers 545..634), so coverage
  is complete and the assembly origin IS on the mesh.
- "MAK_VEGETATION no-go volumes carve holes in the west." gen-COA.log:
  "Added 0 tag volumes from feature query: MAK_VEGETATION". Zero, area-wide. (MAK_WATERWAY 48 and
  MAK_ROAD 5,829 volumes were added; sectors with only ONE distinct nav tag are NEVER fragmented -
  5,362 of 5,362 clean - so a tag volume is a necessary condition, but sectors with 4-6 tags are
  never fragmented either, so tags are not sufficient.)
- "The bad sectors are a terrain-cache streak during the 3.3 h generation." Bad-bad adjacency is
  symmetric between the generation-consecutive and generation-distant directions (155 vs 146).
- "Load timing." Already falsified by N2c (+304 s); nothing here reopens it.

---

## 8. THE ONE PROBE - single variable, no regeneration, no new nav data

**Run the N2d configuration with exactly ONE change: the nav area.**
Everything else byte-identical to run 20260914T224505Z - same C2SIM_EntityLevel_AbstractGraphs_Slope2
SMS, same COA-STP1_Initialization_N2d.xml, same PROBE_RIDGE_1-35_Order.xml, same AtOrder policy,
same --pre-order-gate nav-area, same window, same appData. The one change is the fixture's
Terrain-Database:

    ...\tools\navdata\out\MAK Earth (online) + MojaveCOA.mtf
      ->  ...\tools\navdata\out\MAK Earth (online) + MojaveAO20.mtf

Feasibility, checked on disk this session: the AO20 terrain copy exists and its navData record
points at C:\Users\PAULOB~1\Temp\nav\navData\MAK Earth (online)\NavArea-ground-platform
MojaveAO20.navRuntimeConfig, which exists, and whose nav-data-path directory exists. NO GENERATION,
NO LICENCE SPEND, one new .scnx from build_fixture.py.
Risk to check before launching: that navData lives under the user TEMP tree; confirm it is intact
and consider copying it to C:\C2SIM\vrf-nav first (that is a second change to the .mtf, so either
copy the tree and re-point, or accept the temp path for one run and say so in the prereg).

Geometry, computed this session (cover.py, lanes.py), so the predictions are pre-registered.
AO20 covers 34.5183-34.6980 N, -116.8091 to -116.5909 W. INSIDE: the N2d start
(34.658134/-116.745512, AO20 sector about (12,32), ratio 1.00), the destack start, the P11 freeze
point, and V0 the assembly origin (34.67998/-116.72480). OUTSIDE by 206 m: V1
(34.651212/-116.811637), and therefore V2 and V3 as well.

Predictions, written before the run:
- **P-A (HIGH, instrument).** The trace names NavArea-ground-platform MojaveAO20 and never names
  MojaveCOA. MISS -> the fixture did not swap; stop and read nothing else.
- **P-B (HIGH, the test).** The ~30 m formation-slot moves print "Planned path has N points." with
  N > 1 for 6 of 6 members. Baseline: 0/6 at the destack start and 4/6 at this same N2d start on
  MojaveCOA.
- **P-C (THE DISCRIMINATOR, MEDIUM-HIGH).** The V0 goal (2.3 km NE) PLANS. On MojaveCOA it was
  refused (N2d sec 10.3). V0 is inside AO20 and its sectors are all ratio 1.00.
- **P-D (self-validating control, HIGH).** V1 / V2 / V3 fail the "Is destination in nav area?" gate
  (they are outside AO20) rather than producing "not enough (0) points". That is a DIFFERENT
  console signature from the COA symptom and proves the gate and the area bounds are working.
- Reading: P-B and P-C both PLAN -> C1 CONFIRMED: the refusal is a property of the oversized area's
  generated data, not of the ground, not of useAbstractGraphs, not of timing, not of the start
  position. The product fix is then to cap every generated nav area at the documented 20 x 20 km
  and tile the AO - which is exactly what STP-802 (scenario prep from an AO) has to build anyway,
  so the finding pays for itself.
  P-B PLANS but P-C REFUSES -> the island reading (C3) is bounded and the corridor is the problem.
  BOTH REFUSE -> C1 is dead; go to C2 via the vendor-log grep, and the next arm is a fresh
  20 km generation rather than another query flag.

DO THIS FIRST, BEFORE THE RUN, BECAUSE IT IS FREE: the vendor-log grep for C2 (sec 6c item 3). If
it shows "Failed to insert AbstractGraph" or the working-memory messages, the probe's reading
changes and the fix moves from generation size to runtime memory.

OFFLINE GATE TO ADD REGARDLESS: fold the sec 3 ratio metric into tools/navdata as a
post-generation check - parse the generation log and fail any area where a sector on a planned
route has Average Neighbor Node Count / (Average Node Count - 1) < 0.9. It costs one script and it
would have caught this before six runs were spent on the ridge.

---

## 9. WHAT I COULD NOT REACH
1. **The vendor sim logs** (C:\MAK\logs, runs\**\vrfSim*.log) - forbidden to this executor (they
   dump the environment in cleartext). Every Gameware warning in sec 2.2 is printed there and
   nowhere else. This is the largest single gap and the cheapest to close.
2. **Why the same ground generated 57 kB / connected under AO20 and 475 kB / shattered under COA.**
   I established THAT it did (H3) with a matched 1,505-sector control and that the input triangle
   counts differ by only ~15 %. I could not establish the mechanism: no MAK or Autodesk document
   describes how the area extent influences per-sector generation, and the Gameware GENERATION code
   is not published (only the runtime headers are).
3. **AbstractGraphWorkingMemorySizeLimit and AbstractGraphExtentsInNumberOfCells** - present in the
   DLL under a GlobalConfig blob, absent from DtRwNavGenConfig and from the generator command line.
   I could not find any file VR-Forces reads them from, so I cannot say they are settable at all.
4. **A direct read of the abstract-graph blobs on disk.** AbstractData files sit next to the
   NavData files; parsing them would give true node-level connectivity, but the blob format is
   undocumented and I did not attempt it.
5. **NavigationLab** - GUI only, and a run is in progress; not attempted.
6. **--help on vrfNavGenerator** - not run. The binary documents the flag as "Displays usage
   information and exits", so it would have been safe; I took the option list from its string table
   instead, which is equivalent and required no execution.
7. **Whether the east/west contrast is confounded with scenario size.** It is (H4), and I did not
   design the probe to separate it, because the fragmentation map separates it without a run and
   the sec 8 probe holds the init constant.

---

## 10. METHOD / REPRODUCIBILITY
All scripts and outputs are in scratchpad\agresearch\:
- parse_gen.py / parse_ao20.py - parse the generation logs into sectors.json / sectors_ao20.json
  (i, j, cell box, input triangles, distinct nav tags, NavData kB, AbstractGraph Count,
  Average Node Count, Average Neighbor Node Count).
- cover2.py - proves my ENU frame reproduces the runtime config corners to 0.5 m, so every lat/lon
  to sector mapping below is exact, not approximate.
- cover.py / lanes.py - point and lane lookups, both areas.
- cmp.py - west/east/whole-area statistics.
- map.txt - an 108 x 82 ASCII map of the connectivity ratio over MojaveCOA.
- h2t.py + the downloaded Autodesk header pages (abstractgraph_h.html/.txt etc.).
Nothing was launched, no build was run, no vendor sim log was opened, and no file outside this
scratchpad was written.

---

## 11. ADVERSARIAL REVIEW OF THIS RECORD (HEAVY tier; written against my own findings)

1. **"Average Neighbor Node Count does not mean mean out-degree."** The strongest objection to the
   whole of sec 3. Answer: in 5,362 COA sectors and in essentially all 1,600 AO20 sectors the
   printed value equals Node Count minus one EXACTLY - the signature of a complete graph's mean
   out-degree and an implausible coincidence under any other reading; the value NEVER exceeds
   nodes-1 in 8,845 samples; and abstractgraphgenerator.h carries m_graphNodeNeighbors (a per-node
   neighbour list) and m_graphNodeNeighborCosts, with the report printed as "Average Abstract Graph
   Generation" over that sector's graphs. Falsifier if anyone wants one: a sector with
   nbrs > nodes-1. None exists in our data.
2. **"The dose response is three points."** Correct, and it is the weakest support in the record:
   ratio 0.11 -> 0/6, 0.29 -> 4/6, 1.00 -> all. It is suggestive, not proof. The sec 8 probe is
   designed so that it does not depend on the dose response being right.
3. **"You have replaced a region story with a size story and both fit."** Partly right. Size is
   better supported because it has a MATCHED CONTROL (the same 1,505 sectors of ground, clean on
   one area and fragmented on the other) and a VENDOR LIMIT that the two areas fall on opposite
   sides of, whereas the region story rests on a pair of runs that also differ in initialization.
   But size is the CORRELATE, not the demonstrated mechanism, and sec 9.2 says so.
4. **UNEXPLAINED AND CARRIED, do not bury it (ticket STP-819).** G7b run A: on a lane whose sectors are all clean,
   the 5,013.7 m leg planned with 718 points for M1A2 1 and was REFUSED for M1A2 2/3/4 with goals
   within 130 m of it, in the same wall second. Fragmentation cannot explain that. A per-query
   working-memory or concurrency limit can, and G8's null on gamewareMemorySize does not settle it
   because the value is per block. So there are probably TWO mechanisms in this record - a
   data-quality one (the west) and a concurrency/budget one (long flat queries anywhere) - and this
   pass only addresses the first.
5. **"You claimed a coverage gap and then withdrew it."** Yes - sec 7 records it as refuted, with
   the arithmetic that refuted it. It is left in deliberately so no later session re-derives it.
6. Verified vs assumed: VERIFIED = every quoted string, every number in secs 3 and 4.2, the ENU
   frame (0.5 m against the runtime config), the COA/AO20 control. ASSUMED = that the fragmentation
   measured at generation is what the runtime query sees (the runtime could in principle repair or
   worsen it), and that sec 8's probe holds everything but the area constant.
