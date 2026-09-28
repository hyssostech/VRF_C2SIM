# tools/aggregate - the aggregate-level profile's offline tools

Plan: docs/PLAN_AGGREGATE_LEVEL_PROFILE_2026-09-06.md. Record: docs/experiments/AGGREGATE_PROFILE_OFFLINE_2026-09-27.md.
Populated containers (C1): docs/DESIGN_AGGREGATE_CONTAINERS_2026-09-27.md. The scripts only READ the installed VR-Forces
5.2d tree (VRF_HOME, default C:\MAK\vrforces5.2d); none writes under C:\MAK. Run them with the repo's pinned interpreter,
%LOCALAPPDATA%\Programs\Python\Python312\python.exe.

| script | what it does | self-test |
|---|---|---|
| survey_magx.py | walks AggregateTacticalLevel.sms -> AggregateLevelBase.sms -> base.sms, reads every kind-11 .entity and every .magx, resolves each type by the vendor best-match rule (ported from src/VrfC2SimApp/ObjectTypeResolver.cs) and writes the catalogue | --selftest |
| typemap_check.py | gates data/unit-type-map-52-aggregate.json: schema, uniqueness, resolution in the installed chain, the app's order-time EXPAND trap, and coverage of every unit of data/STP-IRON-STORM-SYNTHETIC_Initialization.xml | --selftest |
| composition_check.py | gates data/unit-composition-52-aggregate.json (DRAFT): every row resolves to catalogue-SIMULATED units with counts, sub-containers compose, echelons descend, nation, recursion, map keys, cut-A coverage - per VARIANT ("catalogue" on the shipped chain, "authored" on the derived chain); reports --tree (footprints, rings, flat vs nested), --init-census (the container each Iron Storm unit gets), --twins (what today's materialization would do), --vendor (the sample scenarios' containers, states and plan tasks) | --selftest |
| authored_units.py | package C2: turns tools/sms/aggregate_authored_design.json into tools/sms/C2SIM_AggregateTacticalLevel.recipe.json (--write), gates it (--check: recipe == regeneration), prints rolled and KEPT-from-donor values (--report), builds the Python-side outputs into a scratch dir (--emit DIR); the deploy is tools/sms/Deploy-C2SimAggregateSms.ps1 | --selftest |

The AUTHORED types (package C2) live only in the DERIVED set C:\C2SIM\vrf-sms\C2SIM_AggregateTacticalLevel.sms
(tools/sms). typemap_check.py and composition_check.py read it as a second chain (derived -> AggregateTacticalLevel
-> AggregateLevelBase -> base), by default at that path; `--derived-sms PATH` (or env C2SIM_AGGREGATE_SMS) names
another deployment. survey_magx.py surveys the shipped chain unless given `--derived-sms`. typemap_check.py gates the map's "authoredRows" on it - each type lands its own UNIT there, none exists on
the shipped chain, and the set only ADDS (every vendor template, map row and US init-shell / container candidate type
resolves as on the shipped chain); both validators FAIL when the derived set is not deployed.

THE APP SIDE (C1b, RL-20260927-04; RUNBOOK sec 11i). The app narrows the composition to ONE variant
(`Vrf:CompositionVariant`, default `catalogue`; an undeclared name refuses the start) and reads the type map's
"authoredRows" with fidelity `Authored` - still never as lookup rows ("AUTHORED" in "rows" still parses as Failed,
which is why typemap_check.py refuses it there). Rows with authored content start ONLY when the fixture the back end
loads is on the derived set and `Vrf:CatalogueSms` is that same .sms: the app reads the fixture's
Simulation-Model-Set-Files exactly as the runner's Stage 0 does (scripts/RunnerLib.ps1 Get-ScenarioModelSet), from
the scenario `Vrf:Scenario` names, and refuses with `COMPOSITION VARIANT authored needs the derived SMS ... loads <sms>`
otherwise. `VrfC2SimApp --populate-selftest [--variant authored]` pins the same pairing these validators gate offline:
the catalogue variant on the vendor set (authored rows SKIPPED), the authored variant on the derived set, the guard
both ways.

```
python tools/aggregate/survey_magx.py --out docs/experiments/AGGREGATE_CATALOGUE_2026-09-27.md \
       --csv docs/experiments/AGGREGATE_CATALOGUE_2026-09-27.csv \
       --reference-scnx "C:\MAK\vrforces5.2d\userData\scenarios\Sample\VR-TheWorld_Online\AggregateTacticalLevel\RoadToKaunas\RoadToKaunas\RoadToKaunas.scnx"
python tools/aggregate/typemap_check.py [--census-md]     # TYPEMAP GATE PASS
python tools/aggregate/survey_magx.py --selftest          # SURVEY SELFTEST PASS
python tools/aggregate/typemap_check.py --selftest        # TYPEMAP SELFTEST PASS
python tools/aggregate/composition_check.py --tree --init-census --twins --vendor   # COMPOSITION GATE PASS
python tools/aggregate/composition_check.py --selftest     # COMPOSITION SELFTEST PASS
python tools/aggregate/authored_units.py --check           # RECIPE CHECK PASS
python tools/aggregate/authored_units.py --selftest        # AUTHORED UNITS SELFTEST PASS
```

Why a validator instead of a `--parse-init` run: the app cannot select a type map offline (InitParseCheck.cs plans with
the legacy dispatch; `--typemap-selftest` picks its map from the chain root), so typemap_check.py ports the key logic
(UnitTypeMap.Lookup, UnitTranslator's pre-table dispatch, InitParser's hostility rule and coordinate cascade) and
proves the port against TypeMapSelfTest.cs's own answers on the entity map before trusting it.
