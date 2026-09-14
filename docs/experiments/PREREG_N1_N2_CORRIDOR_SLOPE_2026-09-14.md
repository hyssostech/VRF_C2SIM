# PREREG N1 / N2: propagation-box-extent 2000 and slope-avoidance-factor 2.0

Registered 2026-09-14 by an Opus design/build executor BEFORE any launch. DESIGN AND BUILD ONLY -
nothing was launched, no C# was built, no order was pushed; the only writes are three new derived
SMS trees under `C:\C2SIM\vrf-sms\`, three NEW `.scnx` under `tools/FixtureGen/frame_variants/` and
the sanctioned deploy folder, three launch lines in the scratchpad, and two files in
`docs/experiments/`. Tier: HEAVY (a prereg, and it carries a mechanism claim).

It realises the N1 / N2 / N3 ordering of
`docs/experiments/NAVDOCS_ABSTRACT_GRAPHS_AND_SLOPE_2026-09-14.md` sec 6-7 - and it opens with a
finding that changes that ordering, so read sec 1 before spending a run.

---

## 1. THE FINDING THAT REORDERS THIS PREREG: a local ground vehicle has no DtNavBot

**CLAIM: `propagation-box-extent` in `Ground_Vehicle.ope` is a DEAD PARAMETER for a local ground
vehicle in the shipped 5.2 EntityLevel SMS, so N1's lever as briefed is expected to be inert.**
Every link below is a line in a shipped header or a shipped data file, read this session.

### 1.1 The chain, verbatim

1. **The parameter lives only in the bot's configuration struct.**
   `include/vrfNavigation/navBot.h:41-67`, `DtNavBot::NavigationConfiguration`, defaults
   `myUseAbstractGraph(true)`, `myMinAbstractGraphReplanDistance(100.)`,
   `myPropagationBoxExtent(200.)`, `myRadiusFactor(1.0)`, `myEnablePathPlanTiming(false)`, with the
   comment "Determines the size of the box used for path planning. Path planning is limited to the
   2d box created by inflating the segment going from the start pos to the destination pos by the
   propagation box extent."

2. **Only one function builds that struct from an .ope, and it belongs to the active-bot interface.**
   `include/vrfobjcore/activeBotNavInterface.h:249-254`: "Sets the navigation configuration from the
   parameters specified in the platform OPE file. The following parameters are read from the
   navigation-params sections: navigation-method (string ...), use-abstract-graph (bool),
   min-abstract-graph-replan-distance (float)" -> `virtual void
   setNavigationConfigParams(DtNavBot::NavigationConfiguration& navConfig);`
   `grep -rn "setNavigationConfigParams" C:\MAK\vrforces5.2d\include` returns exactly ONE line, that
   one. The base class `DtVrfObjectNavInterface` has no such member and states
   "Basic nav interfaces are not able to navigate" (`vrfObjectNavInterface.h:52`).

3. **Which interface an object gets is set in its .ope, and the vendor says so.**
   `vrfObjectNavInterface.h:36-38`: "The type of nav interface used for a particular type of object
   is defined in the .ope file for the object, in the local-objects and remote-objects sections, in
   the nav-interface parameter." The two types, by their own class comments:
   - `activeBotNavInterface.h:24-26` - "objects which are actively querying the navigation system.
     They will have a DtNavBot instantiated if they are within a navigation area."
   - `dynamicObstacleNavInterface.h:18-21` - "objects which are treated as frequently moving
     obstacles by the navigation system, **but are not using the navigation system to plan paths**.
     For example, this is used for remote entities."
   The string constants are `vrfObjectNavInterfaceTypes.h:13-14`
   (`DtActiveBotNavInterfaceType[] = "active-bot"`, `DtDynamicObstacleNavInterfaceType[] =
   "dynamic-obstacle"`).

4. **The shipped ground vehicle is bound to the obstacle interface in BOTH sections.**
   `EntityLevel/vrfSim/platforms/Ground_Vehicle.ope:520` (local-objects) and `:532`
   (remote-objects), both `(nav-interface "dynamic-obstacle")`. By contrast
   `Human.ope:574` and `Animal.ope:431` bind local-objects to `(nav-interface "active-bot")` and
   only their remote-objects to `"dynamic-obstacle"`. So in the shipped SMS **lifeforms get a
   DtNavBot and ground vehicles do not.**

5. **Consequence.** For a local M1A2 / M3 / M577A2 / HMMWV no `NavigationConfiguration` is ever
   constructed from the .ope, so the whole group
   `use-abstract-graph`, `min-abstract-graph-replan-distance`, `propagation-box-extent`,
   `radius-factor`, `enable-path-plan-timing`
   is read by nothing. The vehicle reaches the mesh only through the Lua one-shot
   `vrf:findPathToLocation(...)`, whose C++ entry
   `DtNavArea::findPathToLocation(profile, start, destination, useAbstractGraphs, useChannels,
   channelRadius, locationToAvoid, avoidanceRadius, costEvalFcn, costEvalFcnUsrData)`
   (`navArea.h:398-402`) takes **no bot and no propagation-box argument** - unlike its sibling
   `findBestPathTo(..., DtNavBot* searcher, ...)` (`navArea.h:392-396`). With no bot in the chain the
   query can only take the library default, and Autodesk's default is the 200.0f box.

### 1.2 What this makes dead, and what it does NOT touch

| lever | where it lives | reaches a ground vehicle? |
|---|---|---|
| `propagation-box-extent` (N1) | `DtNavBot::NavigationConfiguration`, built only by `DtActiveBotNavInterface::setNavigationConfigParams` | **NO** as shipped - no active bot, no config |
| `use-abstract-graph` in the .ope | same struct | **NO** - which is why only the Lua flag mattered in G7b |
| `useAbstractGraphs` in the Lua (the AG SMS) | a parameter of the query itself | YES - measured, 8/8 vs 2/24 |
| `slope-avoidance-factor` (N2) | the path COST eval function, held by `DtNavInterfaceManager` / `DtNavigationLocalObjectDecorator`, **not** by the bot | **YES** - see 1.3 |

### 1.3 Why N2's lever survives the same test

The cost function is object-level state, not bot state. `navInterfaceManager.h:20-26` defines
`struct DtNavPathEvalFunction { DtNavBot::NavBotPathLocationEvalFcn fcn; void* userData; }` and
`:66-67` gives `DtNavInterfaceManager::navigationPathEvalFunction()` /
`setNavigationPathEvalFunction(...)`. `navigationDecorator.h:69-73` says it explicitly:
"Accessor/mutator for the navigation path eval function. The actual state is stored in the
internalState(). This will also be changed in the navigation interface (if created). **If not
created will be set into the interface when it is.**" The Lua job carries the same type -
`luaNavJob.h:319-328`, `DtLuaNavFindPathParameters { ... DtNavPathEvalFunction evalFcn; }` - and
`DtNavArea::findPathToLocation` accepts a `costEvalFcn` directly. The eval function's signature is
`bool (*)(const DtNavTag&, float slope, float elevationChange, float* costMultiplier, void*)`
(`navBot.h:130`), i.e. exactly the inputs the `slope-avoidance-factor` formula needs.
The Users Guide points the user at this file for this purpose: UG52 23.5.2 p508 - "For planning
paths in nav meshes, higher slopes cost more (whether the slope is up or down). In the movement
.sysdef files ... the slope-avoidance-factor in the navigation-preference-controller descriptor can
vary the effect." And the descriptor's own text says "for a vehicle" when it derates max-slope
(`navigationPreferenceDescriptor.h:125-127`), which would be odd if the model never applied to
vehicles.
CONFIDENCE: HIGH that the box is bot-only and vehicles have no bot (four quoted lines and a
one-hit grep). MEDIUM-HIGH that the cost path does not need a bot (the state is held one level out
and the Lua parameter struct carries it; no source proves the controller installs it).

### 1.4 What this corrects in the record

`NAVDOCS_ABSTRACT_GRAPHS_AND_SLOPE` sec 2.1 reads `(use-abstract-graph True)` out of
`Ground_Vehicle.ope` and concludes "MAK ships abstract graphs ON for the NavBot's own
destination-following, and OFF only for the Lua one-shot query". **That does not hold for ground
vehicles**: the .ope line is inert and the Lua `false` is the only live setting, which is exactly why
flipping the Lua flag moved everything in G7b (8/8) and G7c-gate while every appData knob moved
nothing. The correction is stamped on the copied record's header line 3. Nothing in that record's
secs 3-4 (slope, soil, cost arithmetic, stall detection) is affected.

### 1.5 The decision this hands back

It is the supervisor's, not this executor's. The three options, with what each costs:

- **(a) Spend N1 anyway, as a one-run falsifier of sec 1.1.** Cheap (the fixture is built and
  deployed, the launch line dry-runs clean) and it is the only way to be sure. Predicted outcome:
  unchanged from run A / G7c's stock-SMS behaviour. The `enable-path-plan-timing` line in the
  overridden .ope is the discriminator (P19e).
- **(b) Skip N1 and make the parameter live**, by adding ONE line to the derived
  `Ground_Vehicle.ope`: `local-objects (nav-interface "active-bot")`, as `Human.ope:574`,
  `Animal.ope:431` and the vendor's own
  `developer_toolkit_examples/extendStateRepository/vrfSim/platforms/Ground_Vehicle.ope:530` all
  do. CENSUS, added after the fact and it is the strongest single line of evidence in sec 1:
  `active-bot` occurs **six times in the entire shipped 5.2d data tree** - EntityLevel's
  `Human.ope:574` and `Animal.ope:431`, three developer-toolkit copies of those same lifeform
  files, and exactly ONE ground vehicle, the `extendStateRepository` example above, which is a
  DERIVED SMS doing precisely what option (b) proposes. No shipped ground vehicle runs on an
  active bot anywhere in the product. `Navigation-profile-for-query` occurs nowhere under
  `appData/settings/`, so there is no application-level fallback that could supply a
  propagation box either.
  NOT BUILT HERE, deliberately: giving every ground vehicle a DtNavBot changes how it follows a
  destination, avoids other entities and replans, so it is a large second variable and needs a
  ruling, not an executor's initiative.
- **(c) Go straight to the slope measurement** in the configuration where it can actually happen -
  sec 5.4, N2b, which IS built.

---

## 2. DOCS AND RECORD CONSULTED (docs-first rule; read this session unless marked)

Vendor documentation
- UG52 68.3.1 p1310 (including SMSs; "Any parameters or files in the higher priority SMSs that are
  also in the lower priority SMSs override those in the lower priority SMSs"), 68.3.3 p1312
  (priority), 68.3.4 p1313 (scripts), 68.3.5 p1314 ("every SMS must have vrfSim.opd"), 68.3.6 p1314
  (search paths), 68.3.8-68.3.9 p1314-1316 (promoting a model / a system to the derived SMS).
- UG52 23.5 p505-507 (nav preferences, soil roughness table), 23.5.2 p508 (slope effects; the
  slope-avoidance-factor sentence; "Ground slope is not taken into account by the path planner that
  plans around feature obstacles"), Table 15 p271 (absolute paths for MTL filename parameters).
- `doc/luadoc/modules/vrf.html`, `findPathToLocation` - the parameter table, including
  "navigationProfile (string) ... If not specified, and the entity has configured navigation
  parameters, then the entity's navigation profile is used."
- Autodesk Navigation SDK help, AStarQuery Options (the 200.0f propagation box), via the copied
  record's sec 8 URL list.

Installed 5.2d headers and data
- `include/vrfNavigation/navBot.h:41-67, :130`; `include/vrfNavigation/navArea.h:392-396, :398-402`;
  `include/vrfLua/luaNavJob.h:319-328`; `include/vrfobjcore/activeBotNavInterface.h:24-26, :249-254`;
  `include/vrfobjcore/dynamicObstacleNavInterface.h:18-21`;
  `include/vrfobjcore/vrfObjectNavInterface.h:36-38, :52`;
  `include/vrfobjcore/vrfObjectNavInterfaceTypes.h:13-14`;
  `include/vrfobjcore/navInterfaceManager.h:20-26, :66-67`;
  `include/vrfobjcore/navigationDecorator.h:69-73`;
  `include/vrfobjparam/navigationPreferenceDescriptor.h:100-140`;
  `include/vrfobjparam/movingObjectParameters.h:294-310`.
- `EntityLevel/vrfSim/platforms/Ground_Vehicle.ope:16-34, :22, :520, :532`; `Human.ope:574`;
  `Animal.ope:431`; `EntityLevel/vrfSim/systems/movement/ground-tracked.sysdef:75-134, :787-830`;
  `EntityLevel/scripts/ground-vehicle-move-to.lua:485-500`;
  `M1A2 SEP V2 Abrams.entity:300` (max-slope 0.94), and the `platform=` attribute of
  `M1A2 SEP V2 Abrams`, `M1A2_Abrams_MBT`, `M3A2_Bradley_CFV`, `M577A2_Command_Post`,
  `M1025 HMMWV with M2` - all `@(platforms-dir)/Ground_Vehicle.ope`.
- Vendor derived-SMS examples: `developer_toolkit_examples/addSignatureModifier` (vrfSim.opd +
  `vrfSim/platforms/Human.ope` ONLY, no promoted .entity), `addTask` (vrfSim.opd +
  `vrfSim/systems/movement/ground-tracked.sysdef` ONLY), `extendStateRepository` (vrfSim.opd +
  `vrfSim/platforms/Ground_Vehicle.ope` + one promoted .entity, and it flips local-objects to
  `active-bot`).
- `bin64/vrfobjcore.dll` parameter-name string table, read at the byte offset of
  `propagation-box-extent`: `... enable-navigation, navigation-method,
  min-abstract-graph-replan-distance, propagation-box-extent, radius-factor,
  enable-path-plan-timing` - one contiguous block. Supporting evidence only.

Our own record
- `docs/experiments/NAVDOCS_ABSTRACT_GRAPHS_AND_SLOPE_2026-09-14.md` (all of it).
- `docs/experiments/G7B_G8_RESULTS_2026-09-14.md` secs 1.5, 2, 8.
- `docs/experiments/PREREG_RIDGE_AG_2026-09-14.md` (secs 2, 3, 4, 6, 7).
- `docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md` secs 6, 7, 7b, 7c.

---

## 3. THE ARTEFACTS, AND THE OVERRIDE CHAIN

### 3.1 Three derived SMSs, all under `C:\C2SIM\vrf-sms\`

| SMS | overrides | purpose |
|---|---|---|
| `C2SIM_EntityLevel_Corridor2000` | `vrfSim/platforms/Ground_Vehicle.ope` | N1 - the flat query's corridor |
| `C2SIM_EntityLevel_Corridor2000_Slope2` | the same .ope + `vrfSim/systems/movement/ground-tracked.sysdef` | N2 - N1 plus the cost gate |
| `C2SIM_EntityLevel_AbstractGraphs_Slope2` | `scripts/ground-vehicle-move-to.lua` (+ .xml) + the same .sysdef | N2b - sec 5.4, EXECUTOR ADDITION |

Each carries `vrfSim.opd` byte-identical to `EntityLevel/vrfSim.opd` (UG52 68.3.5 "every SMS must
have vrfSim.opd"); each `.sms` is byte-identical to `C2SIM_EntityLevel_AbstractGraphs.sms` below its
header comment except for one line, `model-set-directory`; each includes
`..\data\simulationModelSets\EntityLevel.sms`. Diffs against the vendor originals, from
`diff`, are exactly:

    Ground_Vehicle.ope, line 22
    <       (DtRwReal propagation-box-extent 200.000000)
    ---
    >       (DtRwReal propagation-box-extent 2000.000000)
    >       (DtRwBoolean enable-path-plan-timing True)

    ground-tracked.sysdef, line 133
    <          (slope-avoidance-factor 1.000000)
    ---
    >          (slope-avoidance-factor 2.000000)

`enable-path-plan-timing` is the run-time PROOF line, the role the `printInfo` line plays in the AG
SMS's script copy: `navBot.h:66-67` "For debugging and performance testing. Prints the time it takes
to perform path planning." It is also the P19d instrument. It is safe to add because the
`navigation-parameters` block is a bag of `DtRw*` parameters consumed by several readers - the
shipped block already contains `navigation-profile` and `collidable-when-destroyed`, which are NOT
in vrfobjcore.dll's table, and `use-abstract-graph`, which is in vrfutil.dll instead - so a key no
consumer claims is simply unread. If an .ope ever fails to load, remove this one line first.

### 3.2 The override chain - which file the sim loads for the M1A2's parameters

Scripts are matched by SCRIPT ID (UG52 68.3.4; the `.xml` sidecar's `<myScriptId>`). Object
parameter files are not: they are matched by their **path under the SMS's own
`model-set-directory`**, because that is how an entity names them. The chain, each link read this
session:

1. `EntityLevel/vrfSim/M1A2 SEP V2 Abrams.entity:3` -
   `platform="@(platforms-dir)/Ground_Vehicle.ope"`. Same attribute on `M1A2_Abrams_MBT`,
   `M3A2_Bradley_CFV`, `M577A2_Command_Post` and `M1025 HMMWV with M2` - one file covers every
   ground vehicle our type map creates.
2. `platforms-dir` is an SMS variable: `EntityLevel.sms` and each derived `.sms` bind
   `(platforms-dir "$(opd-dir)/vrfSim/platforms")` with `(opd-dir "$(model-set-directory)")`. So the
   identity of the override is the relative path `vrfSim/platforms/Ground_Vehicle.ope`, which is
   where our copy sits.
3. UG52 68.3.6 p1314 says an object configured in a lower-priority SMS may search the higher: "An
   SMS can load files from any SMS it includes, **and any currently loaded SMS it is included in**
   ... base.sms. Can search in base.sms, EntityLevel.sms, mySMS.sms, and yourSMS.sms." UG52 68.3.1
   p1310 then resolves the collision: files in the higher-priority SMS override the lower's.
4. VENDOR PRECEDENT, and it is the decisive evidence that no `.entity` promotion is required:
   `developer_toolkit_examples/addSignatureModifier.sms` includes
   `../data/simulationModelSets/EntityLevel.sms` and its whole tree is
   `vrfSim.opd` + `vrfSim/platforms/Human.ope`. There is no promoted entity anywhere in it, so the
   .ope override must reach EntityLevel's own lifeforms or the example would do nothing.
   `addTask.sms` is the same shape for `vrfSim/systems/movement/ground-tracked.sysdef` - the exact
   file N2 and N2b override.

RESIDUAL RISK, stated: this is the documented and shipped shape, but no run of ours has yet proved
an .ope override taking effect. P19e is the proof line for it.

### 3.3 Three fixtures

Built by `tools/FixtureGen/build_fixture.py --profile 5.2 --empty`, frame
`fixed-frame-run-to-complete` / `0.033333`, terrain
`tools\navdata\out\MAK Earth (online) + MojaveCOA.mtf`, R9 AOI extent - i.e. the `_NavAO` fixture in
every respect but the SMS. `.scn` diff against `R9_Mojave_Empty_52_NavAO.scnx` with part names
normalised is ONE line in each case, `Simulation-Model-Set-Files`.

| fixture | SMS named in the .scn | sha256 (repo == deployed) |
|---|---|---|
| `R9_Mojave_Empty_52_NavAO_C2K` | `C2SIM_EntityLevel_Corridor2000.sms` | `b013961...7833f2a` |
| `R9_Mojave_Empty_52_NavAO_C2K_S2` | `C2SIM_EntityLevel_Corridor2000_Slope2.sms` | `b7fc818...858a9d9d` |
| `R9_Mojave_Empty_52_NavAO_AG_S2` | `C2SIM_EntityLevel_AbstractGraphs_Slope2.sms` | `b7bb517...96a2cd0d` |

Each is committed at `tools/FixtureGen/frame_variants/` and deployed byte-identical to
`C:\MAK\vrforces5.2d\userData\scenarios\`. Nothing else under `C:\MAK` changed (sec 8 gate 6).

---

## 4. RUN N1 - the corridor

### 4.1 Configuration - one variable

`scratchpad\n1n2\n1_launch.sh`, which is `scratchpad\g7c_gate_launch.sh` with `--scenario`
changed and nothing else:

    scenario   R9_Mojave_Empty_52_NavAO_C2K          (was ..._NavAO_AG)
    init       data/R9_Mojave_Lean_Initialization.xml    client-id STP
    order      data/PROBE_G7b_CrossSector_Order.xml      (2 tasks, out and back)
    type map   <scratchpad>\unit-type-map-52-nolifeform.json  (byte-identical to the repo copy)
    env        Vrf__TypeMappingMode=FidelityTable  Vrf__CreationPolicy=AtOrder
               Vrf__DeStackCreates=true  Vrf__DeStackSpacingMeters=700  Vrf__DeStackRotationDeg=0
               Vrf__DropOriginVertexMeters=100  Vrf__TaskPredecessorTimeoutSeconds=7200
    gate       --pre-order-gate nav-area  --pre-order-gate-timeout 600   (no settle)
    appData    C:\C2SIM\vrf-appdata\appData   (relocated, as runs A / B / D / G7c-gate)
    consoles   object 4  member 4   position reports 10 s   backend notify 3
    window     --run-secs 600  --stop-when-complete

Brackets: against **G7c-gate** (20260914T190751Z) the only difference is the SMS - AG script
override vs .ope corridor override. Against **run A** (20260914T164906Z) the differences are the SMS
and the ready gate. Performer in all of them: `1222.MechPlt~PXY` -> Tank Platoon (USA) = 4 x M1A2;
legs ~599 m / 1 seam, ~1,996 m / 4 seams, ~4,989 m / 10 seams.

### 4.2 Predictions (written before launch; a missed HIGH prediction is a stop)

**P19a (HIGH - THE GATE).** Every goal passes both nav-area gates: 0 rows of
`fail in action Is current point in nav area?`, and the ready gate holds the order until the
simulator's own `New Primary nav area: | NavArea-ground-platform MojaveCOA` row, exactly as in
G7c-gate (0 failures / 32 successes). The members' slot moves and first legs are mesh-planned.
MISS -> the run measures nothing about the query; STOP, re-run after warming the cache, read nothing
else.

**P19b (THE MEASUREMENT, as briefed).** The long leg (4.9-5.1 km, 9-10 seams) is planned by the FLAT
query **8 of 8**, with FINE spacing of about 7-9 m per point, i.e. roughly **550-720 points**, and
there is no `Planned nav path has not enough (0) points.` row for any of them.

  **REGISTERED EXPECTATION: P19b MISSES. Confidence MEDIUM-HIGH, on sec 1.**
  The stated mechanism is that the overridden parameter is never read for a `dynamic-obstacle`
  entity, so the query runs at the library's 200 m box and behaves exactly as run A / run D did:
  the long legs refuse (22 of 24 across A4/A/B/D) and the feature planner drives them.
  Quantitatively, if the override IS read the box grows from `(5,013.7+400) x 400 = 2.17 km^2` to
  `(5,013.7+4,000) x 4,000 = 36.05 km^2`, a **16.6x** area increase (not 100x - the leg length does
  not scale), and the G7b abstract route's measured 231-292 m cross-track would then sit well inside
  the corridor.
  READINGS:
  - HIT (8/8 planned, fine spacing) -> sec 1's claim is FALSIFIED, the .ope override does reach a
    ground vehicle, and the 200 m corridor was the G6/G7 gate. That is the better outcome and it
    would make N1 a strictly better fix than the abstract-graph flag (a fine path keeps the NavTag
    and slope costs that the abstract layer is documented to discard).
  - MISS with P19e present (the timing line prints) -> the override WAS read and the corridor is not
    the gate; go to N3 / N2b.
  - MISS with P19e absent -> consistent with sec 1; the corridor is untested, not refuted. The
    cheapest next step is option (b) of sec 1.5, one line: `local-objects (nav-interface
    "active-bot")`.
  - PARTIAL (some long legs plan, some refuse) -> registered now so it cannot be read post hoc: that
    is run D's signature (1 of 8) and means the run reproduced the stock behaviour, i.e. a MISS.

**P19c.** The driven track's cross-track about the designed V2->V3 chord, measured as in
G7B_G8_RESULTS sec 2.3 (window anchored geographically, first sample within 150 m of V2 to first
later sample within 150 m of V3). If P19b HITS and the fine path routes around something, expect
**> 60 m**; the 60 m is the formation offset, not a routing decision. If P19b MISSES expect
**23-33 m max, 14-31 m rms**, the A4/A/B/D band. A reading between 60 m and 200 m with P19b a HIT is
the interesting one: it is a real fine-path detour inside the enlarged box.

**P19d (timing).** Gate-row to outcome-row wall delta per long query, against the 0.0-0.5 s baseline
that covers every A/B/C/D outcome, success and refusal alike. Plus, if P19e prints, the planner's
own reported path-planning time per query. A 16.6x larger search box is the reason to look; no
prediction is registered, the number is RECORDED. The frame-rate cost, if any, is read from the
sim-prefixed console rows (A 10.47x, B 9.93x, C 9.69x, D 10.68x).

**P19e (THE DISCRIMINATOR; an addition by this executor, not in the brief).** At least one console
or app-log row reporting a path-planning time appears during the run, from
`enable-path-plan-timing True`. PRESENT -> the derived `Ground_Vehicle.ope` was loaded AND its
`navigation-parameters` block was read into a `NavigationConfiguration`, which refutes sec 1.1 step 5
and makes P19b's MISS attributable to the corridor rather than to the override. ABSENT -> consistent
with sec 1 but NOT proof, because we do not know which sink the line writes to and the vendor sim log
is not opened (it carries the process environment in cleartext). State it that way in the results;
do not upgrade absence into evidence.

---

## 5. RUN N2 - the slope cost gate

### 5.1 Configuration

`scratchpad\n1n2\n2_launch.sh`, which is `scratchpad\ridge\ridge_launch.sh` with `--scenario`
changed and nothing else:

    scenario   R9_Mojave_Empty_52_NavAO_C2K_S2       (was ..._NavAO_AG)
    init       data/COA-STP1_Initialization.xml          client-id C2SIM
    order      data/PROBE_RIDGE_1-35_Order.xml           (T1 only, taskee 1-35/2/1_A)
    type map   data/unit-type-map-52-nolifeform.json  -> Tank Headquarters Section (USA), 6 vehicles
    env        as N1 (AtOrder, DeStack 700 m, drop-origin 100 m)
    gate       --pre-order-gate nav-area  with --pre-order-settle 240 as the timeout fallback
    appData    VENDOR (no --vrf-appdata-dir), as PREREG_RIDGE_AG sec 6.2
    window     --run-secs 900  --stop-when-complete

The ground and the scoring axes are PREREG_RIDGE_AG sec 3, unchanged and not restated here: leg axis
`s` from the DeStack start 34.658442/-116.740092 toward V1, length 6,593.3 m; the pre-flight's worst
40 m window centred at s = 2,006 m; the P11/G3 leader freeze at 34.65608/-116.76142 = s 1,970.4 m
(O-axis 4,104.0 m); the face itself 55 m of 0.70-0.95 rise/run, mean 0.858, on sand.

### 5.2 The cost arithmetic the factor produces, stated exactly

`cost = inherent cost * slope-avoidance-factor * (slope / max-slope)^2 * MAX_SLOPE_MULTIPLIER`, no-go
at `cost >= 100` (`navigationPreferenceDescriptor.h:111-133`). For the M1A2 over the face:
inherent = `default-path-cost 5.0` (sand is NOT tagged for the `ground-platform` profile, which lists
only road and pavedroad, so no soil cost-entry applies); effective max-slope = 0.94 x 0.80 = 0.752;
MAX_SLOPE_MULTIPLIER = 10.0. Hence

    saf 1.0 (shipped):  cost(s) =  88.4 * s^2       no-go at s >= 1.063  (unreachable: mesh max 1.036)
    saf 2.0 (N2):       cost(s) = 100 * (s/0.752)^2 no-go at s >= 0.752  exactly

| slope s | what it is | cost at saf 1.0 | cost at saf 2.0 | no-go at 2.0? |
|---|---|---|---|---|
| 0.200 | the control line over the same ridge | 3.5 | 7.1 | no |
| 0.661 | 1-1's worst 55 m window, and it drove 25 km | 38.6 | 77.3 | no |
| 0.700 | the face's toe posting | 43.3 | 86.7 | no |
| 0.732 | 1-1's steepest 20 m pitch | 47.4 | 94.8 | no |
| **0.752** | **max-slope x acceleration-factor** | 50.0 | **100.0** | **boundary** |
| 0.764 | 1-1's steepest 5 m pitch | 51.6 | 103.2 | YES |
| 0.858 | the 55 m face's MEAN | 65.1 | 130.2 | YES |
| 0.950 | the face's worst posting | 79.8 | 159.6 | YES |
| 1.036 | slope-max: the steepest triangle that can exist | 94.9 | 189.8 | YES |

So six of the seven postings across the face become no-go and the toe posting does not; the control
line and 1-1's sustained crossing stay legal, which is what a useful threshold looks like. Factor 2.0
is not tuned: it puts the boundary where MAK's own text says the vehicle stops being able to
accelerate.

### 5.3 Predictions

**P20a (HIGH - THE GATE).** For 1-35's formation leader M1A2 1, the second `ground-vehicle-move-to`
goal of the run - the V1 goal - is MESH-planned: `Node Is current point in nav area?: success` ->
`Node Is destination in nav area?: success` -> `Job Calc off road nav path part success` ->
`Planned path has N points.` with **N > 1**, and NO `Planned nav path has not enough (0) points.`
and NO `Planned path has N parts.` for that goal.

  **REGISTERED EXPECTATION: P20a MISSES, confidence HIGH.** G6 (20260914T002716Z) is the same unit,
  the same leg, the same MojaveCOA area and the same stock flat query, and PREREG_RIDGE_AG sec 2.1
  records its outcome: "with MojaveCOA loaded and both nav-area gates returning success, the FLAT
  query returned zero points and the leader froze at the same metre." The leg is 6,593 m, longer than
  any leg the flat query has ever planned here (its two successes were 5,013.7 m). Sec 1 says the
  corridor override will not change that. **A cost model cannot bend a route inside a query that is
  never answered**, so a P20a miss makes P20b-P20d unreadable and the run VOID for the slope
  question. That is why sec 5.4 exists.
  MISS -> STOP. Read nothing else. Switch to N2b.

**P20b (THE MEASUREMENT, readable only if P20a holds).** Scored on the leader M1A2 1's own POS track
from `watchvrf-trace.csv`, projected on the sec 3.2 leg axis of PREREG_RIDGE_AG, after its sec 6.5
altitude-residual check.
- **DETOUR** - the cost gate bent the route: the track's cross-track at leg abscissa **s = 2,006 m is
  >= 150 m to the NORTH**, AND its closest approach to the worst-window centre
  34.656242/-116.761859 is **> 150 m**. X = 150 m is PREREG_RIDGE_AG sec 3.3's derived threshold:
  2.5x the 60 m formation corridor, at a lateral shift whose worst 40 m sustained ratio is 0.796,
  and the face is asymmetric - it worsens to the south (-50 m scores 1.239) and clears to the north
  (+50 to +550 m all below threshold).
- **STRAIGHT** - the track's last fix is within **50 m of 34.65608/-116.76142** and its net advance
  over the final 600 sim s is under 20 m. That is what P11, G3, G5 and G6 all did.
- **MIDDLE CASE**, registered now: it passes s = 2,006 m with cross-track under 150 m and keeps
  going. Scored by re-running the pre-flight on the leader's own driven polyline; if that line's
  worst 40 m ratio is below 0.92 the release is explained by the lane, not by an avoidance.
  PREDICTED, with its reason: **DETOUR, confidence MEDIUM.** Unlike the abstract-graph case (where
  P18b predicted STRAIGHT because the coarse layer is documented cost-blind and samples at 137-177 m),
  here the planner both sees the slope - the eval callback is handed `slope` and `elevationChange`
  per candidate point - and is forbidden to cross it: six of seven postings price above 100. The
  required detour, 150 m, fits inside the shipped 200 m propagation box, so the corridor does not
  block it. What could still produce STRAIGHT: the mesh triangle's slope, not the 7.86 m posting
  gradient, is what the formula sees (sec 7.3).

**P20c (arrival, at equal SIM time).** The leader's straight-line distance to V1 falls below 200 m by
sim 1,000, and its O-axis along-track is recorded at sim 300 / 600 / 900 beside P11 (4,117 / 4,108 /
4,105), G3 (3,907 / 4,103 / 4,107) and G6 (3,823 / 4,110 / 4,103). Every comparison is at equal SIM
time, never equal wall time (lessons-compare-at-equal-sim-time). HOLDS with P20b = DETOUR -> the
confirming test of FINDING sec 7 fires and the slope cause claim is CONFIRMED.

**P20d (the crawl; RECORDED, not predicted).** Per member: mean speed over the final 120 sim s, over
sim 1,000-1,200, and the per-200-sim-s bins; whether any member overruns the leader. PRESENT = at
least one member sits in a constant 0.2-0.8 m/s band for >= 300 sim s while its task still reports
TaskRunning. Recorded either way; it decides nothing here.

### 5.4 N2b - the same measurement in a configuration that can be read (EXECUTOR ADDITION)

Not in the brief. Built because P20a is predicted to miss, and because the fix is one file.

`C2SIM_EntityLevel_AbstractGraphs_Slope2` = the AG SMS's script override byte-for-byte (so the long
leg PLANS: 8/8 in G7b and again in G7c-gate) **plus** the same one-line `ground-tracked.sysdef`
change. Fixture `R9_Mojave_Empty_52_NavAO_AG_S2`; launch line `scratchpad\n1n2\n2b_launch.sh`, which
is `ridge_launch.sh` with only `--scenario` changed.

**It is SINGLE-VARIABLE against the paused RIDGE-AG run** (`PREREG_RIDGE_AG_2026-09-14.md`): same
fixture family, same order, same init, same policy, same gate, same window - only
`slope-avoidance-factor` differs, 1.0 vs 2.0. It therefore reads P20a-P20d exactly as written above,
with two changes to expectations:
- P20a is expected to HOLD (the AG query answered 8/8 at 4.9-5.1 km; 6,593 m is longer than anything
  it has planned, which is the one new risk).
- P20b's coarseness caveat applies: the abstract layer is documented cost-blind ("AbstractGraphs ...
  do not consider holes punched in the NavMesh during runtime, or traversibility of a NavTag and its
  cost"), so a DETOUR here must come from the REFINEMENT stage
  (`ASTAR_PROCESSING_REFINING`) and from the 300 m re-plans as the entity advances, not from the
  coarse route. If N2b reads DETOUR, that is also the first evidence that refinement is cost-aware.
- Point spacing is RECORDED: G7b's abstract plans ran 137-177 m per point at 5 km, and the sec 5.2
  cost gate acts per triangle, not per point, so a coarse point list does not by itself prevent
  avoidance.

**Recommended order given sec 1:** N2b first (it is the only one of the three whose gate is expected
to pass), then N1 only if the supervisor wants sec 1.1 falsified, then N2 only if N1 hits.

---

## 6. N3

`N3` in the copied record is the abstract-graph ridge run, i.e. the already-written and already-gated
`PREREG_RIDGE_AG_2026-09-14.md`, to be spent "only if N1 refuses". **Saying so, as instructed: N1 is
expected to refuse** (sec 4.2), so N3 is expected to be owed. But N3 and N2b are the same run apart
from `slope-avoidance-factor`, and N2b answers N3's question as well as its own: if N2b's leg plans,
"is the flag the only way to get a long route at all?" is answered yes for this lane; if N2b's leader
still drives the chord and freezes, G7B sec 2.4's falsifier fires exactly as N3 would have made it
fire, with the cost model additionally excluded. Running N3 and N2b both would spend two runs to
learn what one learns, so the recommendation is N2b, and to treat RIDGE-AG's P18a-P18d as the
`saf 1.0` arm already recorded in P11 / G3 / G5 / G6 if a control is wanted.

---

## 7. CONFOUNDS, NAMED

**7.1 The dead-parameter risk itself** is sec 1 and is the headline, not a footnote. It makes N1's
result attributable only through P19e.

**7.2 N2 changes tracked vehicles only.** M1A2, M2/M3 Bradley and M577A2 take
`@(system-dir)\movement\ground-tracked.sysdef`; the two HMMWVs of the Tank HQ Section take
`ground-wheels-off-road.sysdef` and keep `slope-avoidance-factor 1.0`. Deliberate: the scored object
is the tracked leader, and the HMMWV's own max-slope is 1.0, so factor 2.0 there would set a
different threshold (0.80 on sand) and add a second variable. CONSEQUENCE TO WATCH: a HMMWV can still
plan across the face while the M1A2 detours, and in G5 HMMWV 2 did creep 61 m further west and 39 m
higher than the leader. If the members diverge, that is why. The one-file fix, if the supervisor
wants it: add `vrfSim/systems/movement/ground-wheels-off-road.sysdef` with the same edit.

**7.3 The slope the formula sees is a TRIANGLE slope, not a posting gradient.** The nav mesh was
generated with the `ground-platform` profile (`entity-radius 3.0`, `step-max 0.56`, `slope-max 46`,
`raster-precision 0.2`, `min-navigable-surface 10.0`); a triangle spanning the face can average the
7.86 m postings down. `min-elevation-change-to-consider-slope 0.3` also gates whether slope counts at
all (a 55 m face rising ~48 m clears that by two orders of magnitude). If P20b reads STRAIGHT with a
mesh-planned leg, this is the first thing to suspect, and the diagnostic is whether the plan's points
cross the face at all.

**7.4 The `inherent cost` term has two readings and only one makes 2.0 the right number.**
`navigationPreferenceDescriptor.h` says inherent cost "may come from the soil type or a semantic tag.
Soil type costs may be directly specified by the soil-name cost-entry elsewhere in this descriptor,
**or may be calculated from 1.0/acceleration-factor in the soil-factors system meta-data**." Our
reading is `default-path-cost 5.0`, because the `ground-platform` profile tags only road and
pavedroad, so the face carries no soil tag and no cost-entry matches (NAVDOCS H5 / 3.4). If instead
the planner computes `1/0.8 = 1.25`, then `cost(s) = 25 * (s/0.752)^2` and no-go starts at
s = 1.504 - unreachable, and **N2 / N2b are no-ops**. A P20b STRAIGHT with a mesh-planned leg is
consistent with that reading; the follow-up in that case is `slope-avoidance-factor 8.0`, which puts
the boundary back at 0.752 under the 1.25 reading. Registered now so it is not invented afterwards.

**7.5 Factor 2.0 is global.** It scales cost everywhere the slope is non-zero, so it changes route
choice across the whole area, not only at the face. The sec 5.2 table shows the control line (0.20)
and 1-1's sustained 0.661 crossing stay far below 100, so the ridge is not wholesale excised - but
short pitches at or above 0.752 anywhere become no-go triangles, and a leg that must cross one will
now detour or refuse. If a DIFFERENT leg refuses in N2/N2b that did not refuse before, this is why.

**7.6 The gate timing** is PREREG_RIDGE_AG sec 6.1 unchanged: AtOrder with the ready gate, 240 s
settle as the timeout fallback, and the launcher warms the navData cache read-only first (G7c ran
VOID on a cold cache within an hour of build load, G7B sec 8.1).

**7.7 The appData trees differ between N1 and N2/N2b** - N1 uses the relocated
`C:\C2SIM\vrf-appdata\appData` because its bracket runs do; N2/N2b use the vendor tree because
RIDGE-AG does. Both knobs in those trees are measured to do nothing (G7B secs 2.1, 2.2, 3), and
neither run is compared across the boundary.

**7.8 What no instrument here can carry.** The console prints commands, never the vehicle's response:
no reflected velocity, no commanded speed, no `pathStatusString()`. So "cannot move" versus
"commanded to stop" stays unseparated, and FINDING sec 7's formation mutual-speed-control hypothesis
stays open whatever these runs show. Likewise the query's own failure reason
(`ASTAR_DONE_PATH_NOT_FOUND` vs `ASTAR_DONE_ERROR_LACK_OF_WORKING_MEMORY`) is not reachable from Lua,
so a refusal can never be attributed from the script layer.

---

## 8. OFFLINE GATES - RUN BEFORE COMMIT, WITH RESULTS

1. **SMS override gate** (`scratchpad\n1n2\check_sms_override.py`, written for this task because
   `validate_fixture.py` cannot check a non-script override): for both new SMSs - include chain,
   `model-set-directory` matching the directory name, `vrfSim.opd` present and byte-identical to
   EntityLevel's, the override set exactly the declared one, each override at the SAME relative path
   as the vendor original, CRLF-only and printable-ASCII, and the line diff against the vendor file
   exactly the expected one or two lines. **RESULT: OK (36 of 36 checks).**

2. **`validate_fixture.py --empty-52` on all three fixtures.**
   - `R9_Mojave_Empty_52_NavAO_AG_S2`: **RESULT OK**, 25/25, and it reports the override chain -
     `overrides script id ground-vehicle-move-to (from ground-vehicle-move-to.lua)`,
     `useAbstractGraphs true`, `run-time proof line C2SIM override ground-vehicle-move-to.lua:
     useAbstractGraphs=true`.
   - `R9_Mojave_Empty_52_NavAO_C2K` and `..._C2K_S2`: **24 of 25 OK, one FAIL**, and it is a
     validator limitation, not a fixture defect:

         sms overriding scripts    0 in C:\C2SIM\vrf-sms\C2SIM_EntityLevel_Corridor2000\scripts  [FAIL]

     `describe_sms()` gates on a derived SMS having at least one overriding `.lua`; these two
     legitimately have none (its own docstring already allows that "another derived SMS may
     legitimately override a different script"). Every other check passes, including
     `Simulation-Model-Set-Files`, the include chain and `model-set-directory`. CONTROL: the same
     command on `R9_Mojave_Empty_52_NavAO_AG` returns `RESULT OK`, so the instrument is working and
     the FAIL is specific to the script gate. OWED, one line, NOT taken here because
     `tools/FixtureGen/validate_fixture.py` is outside this task's sanctioned write set: make that
     gate accept `vrfSim/platforms/*.ope` and `vrfSim/systems/**/*.sysdef` overrides as well, and
     print their one-line diffs the way it prints the script's proof line.

3. **Fixture single-variable check.** Unzipped both new `.scnx` (11 members each, the donor member
   set) and diffed the `.scn` against `R9_Mojave_Empty_52_NavAO.scn` with part names normalised:
   ONE changed line each, `(Simulation-Model-Set-Files ...)`. Terrain, frame lever, extent, `.oob`,
   `.omp` unchanged.

4. **ASCII**, `rg -nP "[^\x09\x0a\x0d\x20-\x7E]"` over this file, the copied NAVDOCS record, the
   three `.sms` files, the two overridden `.ope`/`.sysdef` copies and the three launch lines: no
   matches. INSTRUMENT VALIDATED on a deliberately dirty control
   (`scratchpad\n1n2\dirty_control.txt`: em dash, NBSP, zero-width space, BEL + VT, smart quotes) -
   it flags 5 of its 6 lines and leaves the clean line alone.

5. **Line endings.** The two `docs/experiments/*.md` files are CRLF (matching every other file in
   that directory); every `.sms`, `.ope`, `.sysdef` and `.opd` is CRLF-only, matching the vendor
   originals byte for byte. No `sed -i` / `perl -i` was used anywhere; every edit is a Python
   byte-level `replace` asserted to hit exactly one occurrence.

6. **Nothing else under `C:\MAK` changed.** The scenarios directory before/after listing differs by
   exactly the three added `.scnx` and nothing removed or modified; a recursive last-write scan of
   `C:\MAK\vrforces5.2d\{data\simulationModelSets, appData, userData}` since the session start
   returns those three files and nothing else. The vendor `EntityLevel` tree has no file modified
   since 2026-09-14 00:00.

7. **Deploy identity.** Each deployed `.scnx` has the same SHA-256 as its `frame_variants` twin
   (sec 3.3), and each was written only after a `test ! -e` guard - no existing scenario was
   overwritten.

8. **Launcher dry runs, all three PASSED**, wrapper exit 0, nothing launched, no run directory
   created, backstop correctly not armed. Banners quoted in the executor's report; the load-bearing
   lines are `scenario: R9_Mojave_Empty_52_NavAO_C2K` / `..._C2K_S2` / `..._AG_S2`, licence
   `SALES-TEMP-10-31-26` valid to 31-oct-2026, and the derived observer windows
   1760 s / 2000 s / 2000 s.

---

## 9. ADVERSARIAL REVIEW OF THIS DESIGN (HEAVY; before the commit, not after a run)

1. **"You have talked yourself out of the run the brief asked for."** The strongest objection. Answer:
   the artefacts the brief asked for are BUILT, DEPLOYED and DRY-RUN, and N1 is still runnable in one
   command; what changed is the registered expectation and the reason, which is what a prereg is for.
   A prereg that predicted a hit here would have been predicting against four quoted vendor lines and
   a one-hit grep. NOT EXCLUDED: sec 1 rests on headers and shipped data, not on source - the .ope
   block could be read by some other consumer that also builds a query. That is precisely why P19e
   exists and why option (a) of sec 1.5 is listed first.
2. **"`enable-path-plan-timing` is a second variable in a single-variable run."** Partly right. It is
   a debug print, it is in the same string-table block as the parameter being changed, and it mirrors
   the `printInfo` proof line the record already accepted in the AG SMS. If it is judged too much,
   deleting that one line from both `.ope` copies restores a strict one-line override and costs only
   P19e. Registered, not smoothed over.
3. **"N2b is scope creep."** True that it was not asked for; the justification is that N2's own gate
   is predicted to fail on evidence already in the record (G6), so delivering N2 alone would be
   delivering a predicted-void experiment on the last day before the licence check. It is additive -
   no existing file was changed, N1 and N2 are exactly as briefed and still runnable - and it is a
   better experiment than either, being single-variable against a run already designed and gated.
   The supervisor can ignore it at zero cost.
4. **"A DETOUR reading in N2/N2b could come from the formation offset, not the router."** Guarded by
   the threshold: every flat-mesh and feature track in G7b stayed inside a 60 m corridor that IS the
   formation offset, X is 150 m, and the middle case is scored in advance.
5. **"The .ope override chain is asserted, never observed."** Correct, and stated in sec 3.2. The
   warrant is UG52 68.3.1 / 68.3.6 plus two shipped vendor SMSs of exactly this shape with no
   promoted entity. The observation is owed and P19e is the cheapest way to get it.

UNRESOLVED AND RECORDED: whether `DtNavigationPreferenceController` actually installs the eval
function for a `dynamic-obstacle` entity is not provable from the headers (the controller has no
public header; only its PSR does), so 1.3 is MEDIUM-HIGH, not HIGH. If N2b reads STRAIGHT with a
mesh-planned leg AND sec 7.4's arithmetic is ruled out, "the cost model never runs for a ground
vehicle either" becomes the live hypothesis, and the next probe is a `saf 8.0` arm rather than
another mechanism hunt.

---

## 10. RESULTS

(to be filled after the runs; nothing in this section was written before them)

---

## 11. ORDERING

| # | step | gate | state |
|---|---|---|---|
| 1 | Supervisor ruling on sec 1.5 (a) / (b) / (c) | RULE | RULED 2026-09-14 per the project record: (c) - N1 is NOT run, the abstract-graph override stays the product fix, N2b is the lever |
| 2 | N2b - `n2b_launch.sh`, ridge lane, AG + saf 2.0, single variable vs RIDGE-AG | SPEND | ready, dry-run clean |
| 3 | N1 - `n1_launch.sh`, only if sec 1.1 is to be falsified live | SPEND | ready, dry-run clean |
| 4 | N2 - `n2_launch.sh`, only if N1 hits | SPEND | ready, dry-run clean; gate predicted to miss |
| 5 | `validate_fixture.py` script-gate extension (sec 8 item 2) | - | owed, one line, outside this task's write set |
| 6 | Option (b): `local-objects (nav-interface "active-bot")` in a derived `Ground_Vehicle.ope` | RULE | not built; needs a ruling |
