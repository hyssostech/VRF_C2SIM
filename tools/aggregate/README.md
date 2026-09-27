# tools/aggregate - the aggregate-level profile's offline tools

Plan: docs/PLAN_AGGREGATE_LEVEL_PROFILE_2026-09-06.md. Record: docs/experiments/AGGREGATE_PROFILE_OFFLINE_2026-09-27.md.
Populated containers (C1): docs/DESIGN_AGGREGATE_CONTAINERS_2026-09-27.md. The scripts only READ the installed VR-Forces
5.2d tree (VRF_HOME, default C:\MAK\vrforces5.2d); none writes under C:\MAK. Run them with the repo's pinned interpreter,
%LOCALAPPDATA%\Programs\Python\Python312\python.exe.

| script | what it does | self-test |
|---|---|---|
| survey_magx.py | walks AggregateTacticalLevel.sms -> AggregateLevelBase.sms -> base.sms, reads every kind-11 .entity and every .magx, resolves each type by the vendor best-match rule (ported from src/VrfC2SimApp/ObjectTypeResolver.cs) and writes the catalogue | --selftest |
| typemap_check.py | gates data/unit-type-map-52-aggregate.json: schema, uniqueness, resolution in the installed chain, the app's order-time EXPAND trap, and coverage of every unit of data/STP-IRON-STORM-SYNTHETIC_Initialization.xml | --selftest |
| composition_check.py | gates data/unit-composition-52-aggregate.json (DRAFT): every row resolves to catalogue-SIMULATED units with counts, sub-containers compose, echelons descend, nation, recursion, map keys, cut-A coverage; reports --tree (footprints, rings, flat vs nested), --init-census (the container each Iron Storm unit gets), --twins (what today's materialization would do), --vendor (the sample scenarios' containers, states and plan tasks) | --selftest |

```
python tools/aggregate/survey_magx.py --out docs/experiments/AGGREGATE_CATALOGUE_2026-09-27.md \
       --csv docs/experiments/AGGREGATE_CATALOGUE_2026-09-27.csv \
       --reference-scnx "C:\MAK\vrforces5.2d\userData\scenarios\Sample\VR-TheWorld_Online\AggregateTacticalLevel\RoadToKaunas\RoadToKaunas\RoadToKaunas.scnx"
python tools/aggregate/typemap_check.py [--census-md]     # TYPEMAP GATE PASS
python tools/aggregate/survey_magx.py --selftest          # SURVEY SELFTEST PASS
python tools/aggregate/typemap_check.py --selftest        # TYPEMAP SELFTEST PASS
python tools/aggregate/composition_check.py --tree --init-census --twins --vendor   # COMPOSITION GATE PASS
python tools/aggregate/composition_check.py --selftest     # COMPOSITION SELFTEST PASS
```

Why a validator instead of a `--parse-init` run: the app cannot select a type map offline (InitParseCheck.cs plans with
the legacy dispatch; `--typemap-selftest` picks its map from the chain root), so typemap_check.py ports the key logic
(UnitTypeMap.Lookup, UnitTranslator's pre-table dispatch, InitParser's hostility rule and coordinate cascade) and
proves the port against TypeMapSelfTest.cs's own answers on the entity map before trusting it.
