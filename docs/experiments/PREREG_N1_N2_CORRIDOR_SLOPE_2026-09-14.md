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

### 5.5 N2c - the same run with the V1 query issued 300 s after placement (SUPERVISOR ADDITION, 2026-09-14 21:40Z, written AFTER N2b and BEFORE N2c)

N2b (sec 10) missed P20a: with the abstract-graph override provably active, EVERY goal of every 1-35
member - the ~30 m formation-slot move at creation and the 6,593 m V1 leg - returned "not enough (0)
points", and the unit drove the straight fallback into the fifth ridge freeze. G7c-gate planned 8/8 with
the same override on the eastern lane. Two hypotheses survive the record, and one run separates them:

- **(T) load timing.** UG52 66.2 (p1281, read): "For performance reasons, navigation data does not load
  until you place a simulation object that will use it." vrfNavigation.dll carries an asynchronous NavData
  queue (`DtNavAreaImpl::loadNavData`, `DtNavAreaImpl::processQueues`, states `NavData: ToBeAdded /
  BeingAdded / ToBeRemoved / BeingRemoved` - Gameware's streaming states; strings read from the shipped
  DLL). N2b's members were materialised AtOrder at the destack start (sector i=50 j=75) and queried 0.3 s
  (slot move) and 5 s (V1) after placement. CORRECTION 22:00Z (placement_cells.py on the N2b trace): that
  cell was NOT fresh - the init had placed the 1-35 shell proxy `1-35/2/1_A~PXY` IN cell (50,75) at wall
  49.8, 22 s before the members (and 1-1's and HQ/1-6's proxies in the adjacent cells); in G7c-gate the
  resident proxy `1222.MechPlt~PXY` had been in cell (76,65) for 119 s (cold cache) before its members
  planned. So the surviving (T) variable is the time between the cell's first placement and the query
  (22 s warm vs 119 s cold), not whether the cell was occupied; their OWN "New Primary nav area" rows came AFTER the first query (72.3 vs 71.8 wall).
  G7c-gate's tasked unit also queried 0.4 s after creation, but in a sector its small init had populated
  140 s earlier (i=76 j=65). The docs do not say whether loading is per sector or whole-area, nor what a
  query returns during loading; the DLL's queue says it is asynchronous.
- **(L) lane.** The abstract-coverage variant is REFUTED OFFLINE from the generation log
  (scratchpad navtest/log/gen-COA.log, 8,856 sector blocks parsed): every sector of the leg, i 36..51
  x j 73..75, carries an abstract graph (count 4; 98,576-114,139 triangles each), identical to the
  G7c-gate lane; the 46 sectors without one lie at i 63-106, j 20-54, far to the east and south. What is
  left of (L) is a mesh hole exactly under the destack start, which the triangle counts make unlikely
  but do not exclude.

**N2c = N2b with ONE change:** the task carries a 300 s `StartTime/SimulationTime/DelayTimeAmount`
(the COA order's own form; the deployed 2026-09-07 build parses it - `--parse-order` prints
`timing: simStartMs=300000 relDelayMs=0`; `TaskSequencer.WaitForStartAsync` sleeps it on the wall clock;
AtOrder materialises the members at order RECEIPT, `VrfC2SimService.cs` HandleOrder, before the task
orchestration). Order `data/PROBE_RIDGE_1-35_DELAYED_Order.xml`; launch line scratchpad n1n2/n2c_launch.sh
(`n2b_launch.sh` with the order swapped and `--run-secs 1200` to cover the delay); dry run clean. The
creation-time slot query is the BUILT-IN CONTROL - same 0.3 s timing as N2b.

**P21a (control, HIGH):** the slot-move query at creation (+0.3 s) returns 0 points again, as in N2b.
If it PLANS, N2b's refusal was not deterministic at that timing - recorded, does not decide.
**P21b (THE DISCRIMINATOR, prediction PLANNED at MEDIUM):** the V1 query at ~+300 s prints
`Planned path has N points.` with N > 1 and no "not enough (0)". PLANNED -> (T) confirmed: a mesh query
issued within seconds of placing an object in a fresh sector fails while the sector's NavData streams,
and the runner's ready gate (first area row from ANY object) is INSUFFICIENT on its own - a taskee
queried within ~5-10 s of that row can still be refused - a demo-flow finding (STP-806: per-taskee readiness or a settle after
materialisation). P20b-P20d then become READABLE in this run and are scored as written in sec 5.3 (the
slope factor 2.0 is present). REFUSED -> (T) falsified for a 300 s delay; (L)'s mesh-hole variant or an
undocumented query limit is live; STOP, no further arm without a docs/vendor answer.
**P21c:** with P21b PLANNED, P20b is read: DETOUR (>= +150 m north at s = 2,006, > 150 m from the window
centre) or STRAIGHT (< 50 m from the P11 point, < 20 m advance over the final 600 sim s) or MIDDLE, per
sec 5.3, at equal SIM time since dispatch.

**Docs consulted for (T), and the one number that does NOT separate the runs (recorded before N2c).**
The 5.2 class reference (docs.mak.com/api/vrforces5.2/classref/class_dt_nav_area.html, fetched 21:50Z):
`DtNavArea::loadNavData` "Loads Nav Data for this area" (one call, the whole area; returns bool); the
sector API at runtime is about REGENERATION after terrain change (`sectorsQueuedForRegeneration`,
`sectorsBeingRegenerated`, `requestNavDataGeneration`, regeneration callbacks), not about streaming;
queries are asynchronous (`hasPendingQueries`, `numberOfPendingQueries`, `cancelQuery` - the console's
"Job Calc off road nav path part" is such a queued query). Nothing documents what a query returns while
the area's NavData is still being added to the Gameware world (the DLL's `NavData: ToBeAdded /
BeingAdded` states). The number: G7c-gate's tasked unit planned 6 points **2.2 s after the run's first
area row** (row 140.2, plan 142.6); N2b's members were refused **4.7-9.7 s after theirs** (row 67.1,
queries 71.8-76.8). Time since the first area row therefore does NOT separate success from refusal - if
(T) is right, the operative variable is whether the cell the object was placed in had been resident
(populated by an earlier placement) rather than the clock since registration. N2c's 300 s covers either
form of (T); a PLANNED result confirms "time after placement in the cell" without deciding between them,
and the follow-up is then a placement-history read, not another arm.

Confounds carried: the 300 s wall delay is served by the interface, not the sim (fine: the sim clock ran
~1.0x in N2b, 4,755 sim s over the window); the members idle in formation for 300 s (no movement, so no
sector change); the same warmed cache as N2b (the launch line warms it).

### 5.6 N2d - the same run with the start moved 500 m along the leg (SUPERVISOR ADDITION, 2026-09-14 22:45Z, written AFTER N2c and BEFORE N2d)

N2c (sec 10.2) falsified load timing: the six 1-35 positions at the destack start refuse every abstract-graph query
at +0.3 s and at +304 s alike. Docs and data consulted for a SPATIAL cause, all read before this design:
- `appData\settings\vrfSim\navigationProfiles.mtl`, profile `ground-platform`: entity-radius 3.0, entity-height 3.0,
  step-max 0.56, slope-max 46 deg, raster-precision 0.2, generate-abstract-data True, min-navigable-surface 10.0,
  soil no-go = "mud" (underwater), feature-tag-volumes: MAK_WATERWAY and MAK_VEGETATION are `no-go-exclusive`,
  MAK_ROAD tagged road, MAK_BUILDING commented out. So a vehicle standing inside an OSM waterway/vegetation feature
  volume would be off the walkable mesh whatever the goal.
- MAK Earth (online).mtf: features are OpenStreetMap ("Buildings, point features, and other data from OpenStreetMaps").
  Overpass (overpass.kumi.systems, 22:40Z): NO OSM way with natural/landuse/waterway/highway within 200 m of the
  destack start - no feature volume there. (overpass-api.de timed out; the mirror answered.)
- Offline elevation tiles (tools/preflight cache, 7.9 m posting): a 200 m box around the destack start spans 14.5 m of
  relief, max neighbour slope 5 deg, no edge near slope-max; soil "Desert Succulent Shrub" / dryground at all six
  positions (CA FVEG 15 m). Nothing the profile names would carve a hole at that spot at this posting.
- The vendor Lua returns the point list only (`vrf:findPathToLocation` -> `node.result()`; `AsyncJob` has `cancel` and
  `result`, no status) - the failure REASON cannot be printed from a script override. So no cheaper instrument exists.
- Positive control one cell away: in G6 (flat query) 1-1's M1A2 23/26 planned their slot moves (2 points at 7-9 m)
  from cell (49,76), ~700 m north-west of the destack start.

**N2d = N2b with ONE change:** the INIT places 1-35/2/1_A at 34.657894/-116.745512 = the old destack start moved 500 m
along the leg bearing (263.00 deg): old axis s = 500.0 m, cross-track 0.0 m; cell i=49 j=75; elev 1265.6 m, dryground,
max local slope 21 deg. (CORRECTED 22:55Z before launch: the first design moved the ORDER's V0, but `DeStacker.Apply`
anchors on the init creation plans - 64 units share the stack location 34.67998497486787/-116.72479854165415 and 1-35 is
slot 4 of that stack - so moving V0 would have moved nothing; the wrong order file was removed.) As a singleton 1-35 is
not destacked and its members form up around the given point. The leg axis, the face and every P20 threshold stay as
scored: the worst window sits at OLD s = 2,006 m = 1,506 m from the new start. Init `data/COA-STP1_Initialization_N2d.xml`
(verified with `--parse-init`), order unchanged (`PROBE_RIDGE_1-35_Order.xml`); launch line scratchpad n1n2/n2d_launch.sh
(n2b's with the init swapped; no start delay - timing is closed). Harvest with the same scripts, axis origin unchanged
(the old destack start) so s is comparable across N2b/N2c/N2d.

**P22a (HIGH):** the members are created within 60 m of 34.657894/-116.745512 (the init location; a singleton is not
destacked). MISS -> the init copy was not the one loaded, or the singleton was still destacked; STOP and read the log.
**P22b (THE TEST, prediction PLANNED at MEDIUM):** the slot move and the V1 goal print `Planned path has N points.`,
N > 1, no "not enough (0)". PLANNED -> the refusal is LOCAL to the original destack start (a spot the profile's
documented rules do not explain; filed for STP-804/806 as "validate every placement point against the mesh / snap to
the nearest walkable point"), and P20b becomes readable: **P22c** = P20b scored as in sec 5.3 on the old axis (DETOUR
>= +150 m north at old-s 2,006 and > 150 m from the window centre; STRAIGHT within 50 m of the P11 point with < 20 m
advance over the final 600 sim s; MIDDLE), at equal SIM time since dispatch. REFUSED -> the defect spans at least
500 m of this lane; next = the FLAT-query control from the original spot (vendor SMS fixture, same order) to separate
"off-mesh spot" (flat fails too) from "abstract connectivity" (flat plans the 30 m slot move, as 1-1's did).
Confounds: the new start's local relief (21 deg) is steeper than the old (5 deg) - irrelevant to a hole hypothesis
under slope-max 46 but recorded; the first 500 m of the old leg are no longer driven (nothing was measured there).

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

### 10.1 N2b - run 20260914T205046Z (launched 20:47Z, order pushed at the ready gate, window 900 s to its cap)

**P20a MISSED -> STOP; the run is VOID for the slope question** (sec 5.3's own rule). Harvest
scratchpad n1n2/harvest/n2b_harvest.py, plan_counts.py, area_timing.py on watchvrf-trace.csv
(764,906 lines; no vendor log opened):
- Every 1-35 member (M1A2 1, M1A2 2, M3 1, M577A2 1, HMMWV 1, HMMWV 2): 2 goals, 4 planning attempts,
  **0 planned, 4 x "Planned nav path has not enough (0) points"**, the override proof line printed at
  every attempt, both nav-area gates `success` every time, then `Planned path has 1 parts.` (straight
  feature path). The slot move at creation (+0.3 s, ~30 m) failed exactly like the 6,593 m V1 leg.
- The members' own `New Primary nav area` rows arrived AFTER their first query (M1A2 1: query 71.8,
  area row 72.3; HMMWV 2: query 71.7, area row 79.2); the ready gate had fired at 67.1 on an init object
  elsewhere, 18 s after placement on the warmed cache (113 s cold in G7c-gate).
- The fifth ridge freeze, to the metre: leader M1A2 1 last fix 34.65608/-116.76142 = **0.7 m** from the
  P11/G3/G5/G6 point, leg s 1,971, cross-track -23.3 m (south), 0.0 m advance over the final 600 sim s;
  O-axis at sim +300/+600/+900 since dispatch 4,106/4,106/4,106 (P11 4,117/4,108/4,105). M1A2 2 at
  50.6 m, M577A2 1 at 41.2 m, HMMWV 1 at 11.0 m, M3 1 at 61.7 m from that point; HMMWV 2 crawled past
  the window (+18.9 m north at s = 2,006, closest 18.9 m to its centre) to s = 2,975 at 0.1-0.4 m/s -
  **P20d PRESENT**. Altitude steps up to 47 m between samples on the face (sec 6.5 caveat: the
  positions at the freeze are stable, the crawl speeds are indicative).
- Runner: `-StopWhenComplete` did not fire (no TASKCMPLT), window 901.7 s, teardown clean, RTI preserved
  (12th clean teardown). Side finding: the sim log says
  `NavArea-ground-platform MojaveCOA.navGenConfig does not exist` beside the runtime config - the
  headless generator wrote none; G7c-gate planned without it, so not the blocker (STP-803 note).
- Adversarial note: the strongest reading of this run is NOT "the slope factor did nothing" - the factor
  was never reached; it is "the abstract-graph override is not sufficient on this lane in this
  configuration". The two explanations and the discriminating run are sec 5.5.


### 10.2 N2c - run 20260914T215944Z (launched 21:55Z; order pushed 22:05Z at the ready gate; task dispatched +300 s; window 1200 s to its cap)

**P21b REFUSED -> hypothesis (T), load timing, is FALSIFIED for a 300 s delay.** Harvest with the same three scripts
(1,003,954 trace lines; no vendor log opened):
- The members were materialised at order receipt (first fix wall 62.5; the same six positions as N2b to the metre) and
  stood still for 304 s. The task dispatched at wall 366.6: EVERY member's EVERY goal - the ~30 m formation-slot move
  AND the 6,593 m V1 leg - returned "Planned nav path has not enough (0) points" (0 planned / 4 refusals per member,
  the abstract-graph proof line printed at every attempt, both nav-area gates `success`), then the straight
  feature path. Same outcome as N2b at +0.3 s / +5 s, now at +304 s / +306 s after placement and 307 s after the
  run's first area row (59.9, on `2/1_AD/25_` again).
- P21a's "control at +0.3 s" DID NOT EXIST as designed: the slot move is issued at task DISPATCH, not at creation, so
  under the delay both queries moved to +304 s. Recorded; it does not weaken P21b (the point was a late query).
- The members' OWN `New Primary nav area` rows again came AFTER their first query (367.7-367.9 vs 366.9) and ~305 s
  after placement: an object registers its primary area at its first navigation call, not at placement. That is a
  reading of what the row MEANS (a per-object registration, not a load-complete signal) - it removes the row as a
  readiness instrument for anything but "the area is loaded for SOMEONE".
- The SIXTH ridge freeze, to the metre: leader last fix 34.65607/-116.76143 = **1.4 m** from the P11 point, leg s
  1,972, cross-track -23.3 m, 0.9 m advance over the final 600 sim s; O-axis at sim +300/+600/+900 since dispatch
  4,091/4,106/4,104 (P11 4,117/4,108/4,105). HMMWV 2 again crawled past the window (+17.1 m north at s = 2,006) to
  s = 2,029. HMMWV 1 10.4 m, M577A2 1 41.3 m, M1A2 2 56.6 m, M3 1 61.5 m from the P11 point.
- Runner: gate fired after 10 s (warm), StopWhenComplete did not fire, window 1,201.7 s, teardown clean (13th), RTI
  preserved. A stale design executor woke on its own background job during Stages 3-6 (before the order), ran a few
  read-only checks and stood down; before the measurement window, low load - recorded as a confound note.

**WHAT SURVIVES.** The refusal at the 1-35 destack start is not about WHEN the query runs (0.3 s, 5 s, 304 s) and not
about abstract coverage (sec 5.5, all leg sectors carry abstract graphs). It is about WHERE: the same six positions
refuse a 30 m goal that units elsewhere plan in 6-23 m (G6, flat query: 1-1's members one cell away at (49,76), 40/2/1_AD,
C/1-35) and that the eastern lane plans over 5 km with this very override (G7c-gate). Live readings of (L): (i) the start
positions are OFF the walkable NavMesh (a hole or a disconnected island under 34.658442/-116.740092 and its five slots
~50 m around it - the Kaim `InsidePosFromOutsidePosQuery` in vrfNavigation.dll is how VR-Forces snaps an off-mesh
position, and a snap that finds nothing fails the whole query regardless of goal distance); (ii) an abstract-graph
connectivity defect local to that cell (the 4 abstract nodes exist but the start's triangle is not connected to
them). Both predict the same console output; they differ in what a FLAT query would do from the same spot - unknown
(G6's 1-35 slot queries failed the GATE, not the mesh).

**Adversarial review of this reading.** Strongest competing hypothesis: "the abstract-graph query is broken everywhere
and G7c-gate was a fluke" - refuted by two independent runs on the eastern lane (G7b run C 8/8, G7c-gate 32/32) and by
the flat-query control on that lane (2/24), i.e. the override moved the outcome there in the predicted direction. Second:
"the 1-35 members' start is outside the nav AREA" - refuted by `Is current point in nav area?: success` at every attempt.
Third: "the 300 s delay was not served" - refuted by the dispatch timestamps (order 62.5, first goal 366.6; the
interface's own log carries the wait). Unexplained and carried: why the same query fails for a 30 m goal (a snap or
island failure explains it; a distance or budget limit does not). VERIFIED: everything above from the trace. ASSUMED:
that the six start positions are the operative variable (the next step tests exactly that).

**NEXT (docs first, then one run):** (1) read the Lua API for `vrf:findPathToLocation` and the object it returns - if
the path/job carries a status or failure reason, add ONE `printInfo` of it to OUR override script (the custom SMS is
already the vehicle for that) so the console says WHY (start off-mesh vs no path) instead of "(0) points"; (2) the
navigation profile `ground-platform` in `appData\settings\vrfSim\navigationProfiles.mtl` (entity radius / slope / soil
entries) for what makes ground un-walkable at generation; (3) the offline tile cache under the destack start (land
cover / slope) for a hole candidate; then (4) ONE spatial run: the same AG_S2 fixture and order with the members
created ~800 m closer to V0 (`Vrf__DeStackSpacingMeters` 700 -> 500 moves the 1-35 slot from 2,800 m to 2,000 m
south-west of V0) - if the slot move and the leg PLAN from there, the hole/island at the original start is confirmed
and P20b (slope factor 2.0 vs the face) becomes readable in the same run; if they refuse again, the defect is wider
than one cell and the flat-query control from the same spot is the next arm.

### 10.3 N2d - run 20260914T224505Z (launched 22:40Z; order pushed 22:50Z at the ready gate; window 900 s to its cap; first run on the MERGED, G-A-pinned build)

**P22a HOLD, P22b REFUSED for every long goal (and for the leader's short one), P22c UNREADABLE - and the unit DROVE THE
WHOLE COA ROUTE.** Harvest with the three scripts (923,496 trace lines):
- The six members were created at the shifted point (leader 34.658134/-116.745512, 34 m from the predicted 34.657894/
  -116.745512; all six within 60 m) - the init copy was the placement anchor, as designed.
- Short goals at the NEW spot: the ~30 m formation-slot move PLANNED for 4 of 6 members (HMMWV 1 "Planned path has 5
  points", M3 1, M577A2 1, HMMWV 2 one success each) and was REFUSED for both M1A2s. At the OLD spot (N2b, N2c) 0 of 6.
  So the new spot is on the mesh for short queries; the old one is not (for these six positions).
- Long goals: EVERY one refused with "not enough (0) points" for every member - the route's V0 (2.3 km NE; see next
  bullet), V1 (8.5 km), V2 (14 km), V3 (20 km) - 8-10 refusals per member, the abstract-graph proof line at each. On the
  eastern lane the same override planned 4.9-5.1 km legs 8/8 (G7b C, G7c-gate). Distance does not separate (2.3 km refused
  here, 5 km planned there); REGION does.
- CONFOUND I INTRODUCED, recorded: because 1-35 no longer sat within Vrf:DropOriginVertexMeters (100 m) of V0, the 3f
  origin-vertex drop did NOT fire and the route kept V0 - the unit first drove 2.3 km back north-east to the assembly point,
  then V0 -> V1 -> V2 -> V3. The V0->V1 line is the AUTHORED line, ~1.28 km NORTH of the destack-start->V1 line every
  earlier run drove (at the freeze longitude: 34.6677 vs 34.6561). So P22c (slope factor 2.0 vs the face) is unreadable
  twice over: no mesh path, and a different line.
- THE FIRST COMPLETED 1-35 ROUTE IN ANY RUN: on straight feature paths the unit reached V1 ("Task completed successfully"
  at wall 243.5, sim ~1,500 since dispatch for 8.5 km -> ~5.7 m/s), then V2 (479.6), and was 0.85 km short of V3 when the
  window closed (leader last fix 34.57419/-116.99830; along-track 24.6 km on the old leg axis; sim ran ~7.5x wall, 5,820 sim
  s in the window). Its closest approach to the P11 worst-window centre was 1,188 m (north). The ridge that froze six runs is
  passable 1.3 km north of the line those runs drove - exactly what PREREG_RIDGE_AG sec 3.3's lateral table predicted (+550 m
  already clear at 0.720; the authored V0->V1 line was never scored, and is now known to pass).
- Runner: gate fired after 10 s (warm), StopWhenComplete did not fire (the leg outlasted the window), teardown clean (14th),
  RTI preserved. The merged build joined, dispatched the init and the order, and pushed reports with no MissingMethod /
  Tick-phase failure line - the first live confirmation of gate G-A. (V2 is the deliberate proof run.)

**Adversarial review.** (1) "The unit completed because the slope factor 2.0 steered it" - NO: no mesh path was ever
planned, the factor acts inside mesh planning only; the line itself avoided the face. (2) "The old spot is off-mesh" -
supported (0/6 short there vs 4/6 here) but the two M1A2s' short refusals here show it is not clean; a 30 m query can also
fail for a 3 m-radius vehicle whose slot lies across a micro-feature at the 0.2 m raster - not excluded. (3) "Long queries
fail because the abstract graph is DISCONNECTED between the 1-35 region and V0/V1/V2/V3" - the surviving reading: the
eastern lane's 5 km legs plan, the western 2.3-20 km legs do not, whatever the start; the generation log shows abstract
graphs in every leg sector but says nothing about their inter-sector links (63,752 transition points area-wide, not per
sector). Falsifier available offline: none found (no connectivity dump). Falsifier live: a FLAT-query run from the new spot
- if the flat query plans a 1-2 km goal there, the mesh is connected locally and the abstract layer is what fails.
(4) Unexplained and carried: why the flat query in G6 planned nothing beyond 23 m anywhere while the abstract query plans
5 km in the east; and the two M1A2 short refusals here.

**Consequences.** (a) DEMO/ROUTING: the freeze is a LINE property; the pre-flight tool's lateral sensitivity plus a
north-shift rule would have routed 1-35 through - FINDING sec 7 stands and gains a remedy the interface can apply
(STP-804/806: pre-flight the leg, shift the line to the cleared side, log it). (b) NAV: the abstract-graph override is
regional on this area; the western half needs either a connectivity diagnosis (NavigationLab is the vendor's only viewer
- GUI) or a regeneration with a smaller sector / different abstract settings, or the question to MAK (user's call).
(c) The origin-vertex drop must key on the TASKEE's position relative to V0 as it does, but a unit placed away from the
assembly point will legitimately drive to V0 first - correct behaviour, worth a log line naming it.

---

## 11. ORDERING

| # | step | gate | state |
|---|---|---|---|
| 1 | Supervisor ruling on sec 1.5 (a) / (b) / (c) | RULE | RULED 2026-09-14 per the project record: (c) - N1 is NOT run, the abstract-graph override stays the product fix, N2b is the lever |
| 2 | N2b - `n2b_launch.sh`, ridge lane, AG + saf 2.0, single variable vs RIDGE-AG | SPEND | RUN 205046Z: P20a MISS -> STOP (sec 10.1) |
| 2b | N2c - `n2c_launch.sh`, N2b + 300 s task start delay (sec 5.5), the (T)/(L) discriminator | SPEND | RUN 215944Z: P21b REFUSED -> (T) FALSIFIED; spatial (L) live (sec 10.2) |
| 2c | N2d - `n2d_launch.sh`, 1-35 placed 500 m along the leg by an init copy (sec 5.6), the spatial discriminator that keeps the face | SPEND | RUN 224505Z: short goals plan 4/6, ALL long goals refused (regional), the unit drove V0->V1->V2->V3 on straight paths 1.3 km north of the freeze line (sec 10.3) |
| 2d | Flat-query control from the new spot (vendor SMS fixture, same init copy) - local mesh connected? | PREREG | owed if the nav thread continues; the demo remedy (route shift) does not need it |
| 3 | N1 - `n1_launch.sh`, only if sec 1.1 is to be falsified live | SPEND | ready, dry-run clean |
| 4 | N2 - `n2_launch.sh`, only if N1 hits | SPEND | ready, dry-run clean; gate predicted to miss |
| 5 | `validate_fixture.py` script-gate extension (sec 8 item 2) | - | owed, one line, outside this task's write set |
| 6 | Option (b): `local-objects (nav-interface "active-bot")` in a derived `Ground_Vehicle.ope` | RULE | not built; needs a ruling |
