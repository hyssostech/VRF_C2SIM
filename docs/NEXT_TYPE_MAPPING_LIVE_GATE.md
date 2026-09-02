# LIVE GATE - fidelity type mapping (hand this to the VR-Forces holder)

Written 2026-09-02 by the offline executor. This is
`docs/UNIT_TYPE_MAPPING_FIDELITY_2026-09-02.md` sec 7.5 **item 3**, the ONE step that pass could
not take. Items 1 and 2 are done and green (`--typemap-selftest`, 783 checks); sec 11.3 records
them. **Do not trust any row in `data/unit-type-map.json` until this gate passes.**

## 0. Why this gate exists (read this before the commands)

Every "resolves to" claim in the survey and in `data/unit-type-map.json` is STATIC best-match
arithmetic over the `.entity` files. There is exactly one live observation on record that
disagrees with the static picture: `docs/VRF_GROUND_TRUTH.md` sec 0.1.8 item 1 - on 2026-07-15
`114.MechCoy`'s formation list came back all-lowercase, the `Ground_Aggregate` signature, where
the static rule says a real template. That has never been reconciled. If the live matcher differs
from the static rule, **the whole `objectType` column changes** and this table is wrong in a way
no offline test can see.

Per memory `lessons-vendor-diagnostics-first`: read the vendor's own log and its verbosity knob
BEFORE probing anything. The vendor log stamps **LOCAL** time; our logs stamp UTC.

## 1. Configuration under test

```
Vrf:TypeMappingMode        = FidelityTable      (default is still RealTemplates - set it explicitly)
Vrf:FriendlyNation         = USA
Vrf:OpposingNation         = RUS
Vrf:TypeMapFile            = data/unit-type-map.json
Vrf:SurfaceProxySubstitutions = true
```

Environment-variable form for a one-off run, no rebuild and no appsettings edit:

```
$env:Vrf__TypeMappingMode = "FidelityTable"
$env:Vrf__OpposingNation  = "RUS"
```

## 2. Order of runs

Run them in this order. Stop at the first falsifier; each later run is worthless if an earlier
one failed.

| # | run | fixture | why this one |
|---|-----|---------|--------------|
| 0 | `--typemap-selftest` on the VR-Forces machine | none | proves the machine's OWN `C:\MAK` install matches the table (a different install = different catalog). Expect `SELF-TEST PASSED (783 checks)`, exit 0. **If it SKIPS parts B and C, `C:\MAK` was not found - fix that before anything else.** |
| 1 | normal scenario run, `TypeMappingMode=RealTemplates` | `data/R9_Mojave_Lean_Initialization_NoComments.xml` on `TropicTortoise_FFRTC` | the CONTROL. 6 units, the cheapest probe. Capture `bin64\vrfSim.log` and the run's own log. |
| 2 | same, `TypeMappingMode=FidelityTable` | same | the A/B. Config only - the binary is identical. |
| 3 | `TypeMappingMode=FidelityTable`, `OpposingNation=PRC` | any | must REFUSE TO START. ~10 seconds. |
| 4 | full scale, `TypeMappingMode=FidelityTable` | `data/COA-STP1_Initialization.xml` | 128 units, 44 groups. Only after 1-3 are clean. |

Use the normal runner (`scripts/RunC2SimScenario.ps1`) exactly as for any other run; nothing in
this gate needs a new script. Inventory `vrf`/`rti` processes first (memory
`vrf-instances-outlive-sessions`).

## 3. What to read, and where

1. **`C:\MAK\vrforces5.0.2\bin64\vrfSim.log`** - the creation lines. This is the primary
   evidence and the whole point of the gate: it is the BACK END saying which model it built,
   as opposed to our static arithmetic saying which model it should have built. Raise the
   notify level first if the creation lines are not there at the default level - a silent
   channel at a low notify level is not evidence.
2. **Our run log** - one `TYPE MAP <fidelity>: <name> -> <template> (<type>) [<note>]` line per
   unit, emitted before the create is enqueued. That line is what you compare against the
   vendor log.
3. **The report stream** - one `ObservationReport / NameObservation` per PROXY unit, whose
   `Marking` carries the substitution text.

## 4. Expected creation lines, run 2 (R9 lean, 6 units, FidelityTable, RUS)

All six units are friendly (`SF...`), so `OpposingNation` does not bite here.

| unit | SIDC | init SISOEntityType | table decision | expected template in `vrfSim.log` |
|---|---|---|---|---|
| `1141.MechPlt` | `SFGPUCIZ---D---` | `11:1:225:3:4:0:0` | key (a) - the init's own type, covered by row `F-UCIZ-D` | **Mechanized Platoon (USA) IFV (Deprecated)** - 4x M2A2 Bradley + 3 squads |
| `1142.MechPlt` | `SFGPUCIZ---D---` | `11:1:225:3:4:0:0` | key (a), row `F-UCIZ-D` | same |
| `1143.MechPlt` | `SFGPUCIZ---D---` | `11:1:225:3:4:0:0` | key (a), row `F-UCIZ-D` | same |
| `1222.MechPlt` | `SFGPUCIZ---D---` | `11:1:71:3:4:0:0` | key (a) BACKSTOP MISS (Country 71 is not in the table) -> key (b) row `F-UCIZ-D` | same, and a WARN naming the uncovered `3:11:1:71:3:4:0:0` |
| `114.MechCoy` | `SFGPUCIZ---E---` | `11:1:225:5:4:1:0` | key (a) BACKSTOP MISS (that is the row F3 wants AUTHORED) -> key (b) row `F-UCIZ-E`, PROXY | **Mechanized Platoon (USA) IFV (Deprecated)** at company echelon, with the substitution surfaced |
| `1.BdeHQ` | `SFGPUCIZ--EH---` | `11:1:153:5:4:0:0` | key (a) BACKSTOP MISS (Country 153) -> key (b) row `F-UCIZ-H`, PROXY | **M577A2_Command_Post**, a single ENTITY (not an aggregate) |

(The six init types above were re-read from `data/R9_Mojave_Lean_Initialization_NoComments.xml`
this pass. Note `1.BdeHQ` is `11:1:**153:5**:4:0:0`, a COMPANY-category type on a BDE unit -
the survey sec 4.3 records it as `11:1:153:3:4:0:0`. Either way Country 153 is not in the table,
so the backstop behaviour is the same.)

Contrast with run 1 (`RealTemplates`, the control): all four `D` units become
**Tank Platoon (USA)**, `114.MechCoy` becomes **Tank Company (USA)**, and `1.BdeHQ` becomes a
single **M1A2_Abrams_MBT**. If runs 1 and 2 produce the SAME creation lines, the config did not
take effect - check `Vrf:TypeMappingMode` actually reached the process (the run log's first
`Type-mapping mode = ...` line says which mode is live).

## 5. Expected creation lines, run 4 (COA-STP1, 128 units, FidelityTable, RUS)

The full per-unit expectation is the table itself; the offline dry-run says all 128 units match
on key (b) with **28 EXACT and 100 PROXY, and zero fallbacks to the echelon-only or catch-all
rows**. Spot-check these ten, which cover every distinct mechanism:

| census group | sample unit | expected template | row |
|---|---|---|---|
| SF UCA E COY | `A/1-35` | Tank Company (USA) | `F-UCA-E` EXACT |
| SF UCA F BN | `1-35_MAIN` | Tank Headquarters Section (USA) | `F-UCA-F` PROXY |
| SF UCF - NOS | `A/4-27` | Field Artillery Battery (USA) M109 | `F-UCF-N` EXACT |
| SF UCE E COY | `A/40` | Tank Breach Company (USA) | `F-UCE-E` PROXY |
| SF UCR - NOS | `HQ/1-1` | AR Scout | `F-UCR-N` PROXY |
| SF UULM E COY | `856/HHC` | aggregate-Company-HQ-Friendly | `F-UULM-E` PROXY |
| SH UCA E COY | `1/7154` | Tank Company (RUS) | `H-UCA-E` EXACT |
| SH UCIZ D PLT | `2/7151` | Mechanized Platoon (RUS) (Deprecated) | `H-UCIZ-D` EXACT |
| SH UCFHE E COY | `1/7158` | Field Artillery Battery (USA) M109 (WRONG NATION, surfaced) | `H-UCFHE-E` PROXY |
| SH UCD D PLT | `AD/7151` | Air Defense Artillery Platoon (RUS) | `H-UCD-D` EXACT |

**Zero units may create `Ground_Aggregate` (gui-label "Ground Unit").** Under `RealTemplates`
today, 26 battalion units do.

## 6. Expected result, run 3 (PRC refuse-to-start)

The process must log `LogCritical` and exit WITHOUT starting the bridge, with a message that
names the missing content:

```
Vrf:TypeMappingMode=FidelityTable - REFUSING TO START. Vrf:OpposingNation='PRC' has NO usable
UNIT template: 36 of 37 rows are AUTHORED_PENDING (e.g. P-UCA-E, P-UCA-D, ...). The installed
VR-Forces 5.0.2 model-set chain (C2simEx -> EntityLevel -> base) contains ZERO Country-45 unit
templates - only platform leaves - so every aggregate request would land a zero-subordinate
Country-0 abstract or Ground_Aggregate (empty units). REFUSING TO START. Fix: ...
```

If VR-Forces starts, or any unit is created, JC-2 is not implemented as ruled and this is a
STOP.

## 7. Falsifiers - any ONE of these fails the gate

1. **A `vrfSim.log` creation line names a different model than the table's `templateName`.**
   The static best-match rule is then wrong (the `VRF_GROUND_TRUTH` 0.1.8 item 1 hypothesis),
   and the whole `objectType` column has to be re-derived from live evidence. Record the exact
   line, the query type we sent, and the model the back end built.
2. **`Ground_Aggregate` / "Ground Unit" appears for ANY unit** under `FidelityTable`. The table
   never targets it; the loud-skip path exists precisely so it cannot be reached by accident.
3. **A created unit publishes ZERO subordinates** (the sec 3.5 empty-abstract trap) where the
   table says it is composed. Read the member count, not just the creation line.
4. **A PROXY unit's name comes back TRUNCATED** in `ObjectCreated`, or its tasking stops
   resolving, after the `~PXY` marking tag is appended. The back end resolves marking text
   through a 35-byte blob (`include\vrfutil\rwUUID.h:412`) - the same mechanism behind the
   2026-09-02 route-uuid failure - and the code only appends the tag when the result stays
   within 34 characters. If a tagged unit still breaks, set
   `Vrf:SurfaceProxySubstitutions=false` (the substitution is then reported and logged but not
   marked) and record it: the report-stream channel is unaffected either way.
5. **`114.MechCoy`'s formation list comes back all-lowercase again** while the table says
   `Mechanized Platoon (USA) IFV (Deprecated)`. That is the 2026-07-15 observation reproducing,
   and it is the single most important thing this gate can find. Capture the full
   `RequestAvailableFormations` reply.
6. **Runs 1 and 2 produce identical creation lines.** The config did not take effect; the gate
   proved nothing.
7. **A unit is silently not created** without a `TYPE MAP AuthoredPending/Failed` error line.
   The skip must always be loud.

## 8. What to send back

- `bin64\vrfSim.log` (the creation section) and the run log for runs 1, 2 and 4, plus the
  console output of run 3.
- The `--typemap-selftest` output from run 0 (the check count and exit code).
- For every row whose live landing differs from `data/unit-type-map.json`: the row id, the
  objectType we sent, and the model the back end actually built.

That last list is the deliverable. If it is empty, the static method is confirmed, the
2026-07-15 falsifier is finally closed, and `TypeMappingMode=FidelityTable` can become the
default. If it is not, nothing in the survey's sec 5 is safe.
