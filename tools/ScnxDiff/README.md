# ScnxDiff - VR-Forces .scnx dump / diff harness

Groundwork plan item 0.5 (`docs/VRF_GROUNDWORK_PLAN.md`). This is the instrument
that lets us read exactly how VR-Forces represents a *working* unit, so Phase 2.2
can diff a GUI-authored (working) unit against a port-created one field by field.

OFFLINE ONLY. The tool never touches a live VR-Forces / RTI process. It only
reads `.scnx` files, which are ZIP containers of scenario members. Pure Python 3
standard library - no build, no dependencies.

## What a .scnx actually is (format finding - read this first)

The plan brief called the container "XML". That is only half right. A `.scnx` is
a ZIP holding a **mix** of two formats:

| Member          | Format                | Holds                                             |
|-----------------|-----------------------|---------------------------------------------------|
| `.scn`          | MAK S-expression      | scenario master: terrain, model set, time params  |
| `.oob`          | MAK S-expression      | **the simulation objects (units + entities)**     |
| `.xtr`          | MAK S-expression      | force / hostility table                            |
| `.orb .pln .omp`| MAK S-expression      | orbat / plan / object-id -> uuid address map       |
| `.osrx .spt .sgr .ovl .gui_settings` | boost XML | observer views, scripts, selection groups, GUI state |

The object model we care about lives in the **`.oob`**, and it is **MAK Lisp-style
S-expressions, not XML** - e.g. `(local-vrf-object (marking-text "M1A2 1")
(object-type 1 (1 1 225 1 1 3 0)) ...)`. This tool parses that S-expression
format directly. XML members are listed in the container table but not parsed
(they carry no unit structure).

## The organization model (important, and not what the brief assumed)

The brief asked for "echelon IDs and subordinate order". Empirical finding across
all 68 installed scenarios:

* **Echelon IDs are NOT persisted in a `.scnx`.** There is no `echelon` key in any
  installed scenario. VR-Forces assigns echelon IDs at runtime from the org tree;
  the saved file does not carry them.
* **Composition is stored only as a parent reference.** Each object carries
  `(parent-name "...")` inside its `state-repository`. Its value is:
  * `"VRF_UUID:<guid>"` -> this object is a **subordinate of the object with that uuid**;
  * `"VRF_UUID:<n> Force"` -> a **top-level subordinate of force `<n>`**;
  * `""` (empty) -> force-level / unattached (ForceOther etc.).
  The static per-controller `(subordinates )` lists in the file are **empty at rest**
  (VR-Forces repopulates them at runtime), so they are not a composition source.
* **Subordinate ORDER = document order** of objects sharing a parent in the `.oob`.
  Per the User's Guide the first subordinate is the unit leader, and order cannot be
  changed after creation. This tool marks the first subordinate with `*`.
  CAVEAT: that VR-Forces' runtime leader assignment equals `.oob` document order is
  an assumption, not offline-verifiable; confirm against a reflected echelon ID in
  Phase 1/2.2.

The tool reconstructs the tree by resolving `parent-name` -> `uuid`. Objects whose
parent chain never reaches a root (malformed cycles / dangling refs) are still
shown, under an explicit `(orphaned / cyclic references)` or `(unresolved: ...)`
root, so nothing silently disappears.

## Usage

```
python scnx_diff.py dump  <scenario.scnx>            # structure dump
python scnx_diff.py dump  <scenario.scnx> --brief    # container + org tree only
python scnx_diff.py dump  <scenario.scnx> --full     # do not truncate controller lists

python scnx_diff.py diff  <a.scnx> <b.scnx>                  # semantic unit diff
python scnx_diff.py diff  <a.scnx> <b.scnx> --match-by uuid  # match on uuid instead of name
python scnx_diff.py diff  <a.scnx> <b.scnx> --ignore <tag>   # ignore an extra S-expr tag (repeatable)
```

Output goes to stdout (redirect to a file to capture). Output is emitted as UTF-8
so non-ASCII scenario content (e.g. accented unit names) does not crash on a
Windows cp1252 console.

### DUMP surfaces, per object

Container member list with per-member format and size; scenario parameters (`.scn`);
forces (`.xtr`); the reconstructed **organization tree** (superior/subordinate,
ordered, leader marked); and per-object detail: `marking-text` (name), `uuid`,
`object-identifier`, **object class + DIS enumeration** (`class=1(entity)` /
`class=3(aggregate)` + the 7-number DIS-style type tuple), force, aggregate state
(Aggregated/Disaggregated), superior, ordered subordinates, wired subsystems and
controller PSR blocks, active tasks (from `task-status-list`: controller-name +
task-type), embedded entities, and notable state flags (destroyed,
independently-tasked, request-task-needed, suspended-task count, appearance bits).

## DIFF: canonicalization and ignore rules

Two `.scnx` are compared **semantically**, not byte-wise. Each object is parsed to
its S-expression tree, matched to its counterpart, and flattened to a set of
leaf `path -> value` entries which are compared.

**Unit match key.** Default is `marking-text` (the human-assigned unit name).
Rationale: the name is the one identity that survives across creation methods, so
it is the right key for the Phase 2.2 goal (GUI unit vs port-created unit, whose
uuids will differ). `--match-by uuid` is offered for same-lineage diffs
(a file vs an edited copy), where uuids are identical. Duplicate keys are paired
positionally (`name#0`, `name#1`) with document order preserved.

**Canonicalization (applied before comparing).**
* S-expression is parsed to a tree, so **attribute order, whitespace, and
  indentation are irrelevant** - only structure and values matter.
* **Floats are normalized** to 4 decimal places and `-0.0` collapses to `0.0`
  (so `0.000000` == `0.0`, and the `-0.000000` vs `0.000000` orientation noise
  that VR-Forces emits does not register as a difference).
* Sibling nodes with the same tag are indexed positionally (`tag[0]`, `tag[1]`)
  so repeated structures (multiple tasks, vertices) diff element-by-element.

**Ignored by default (identity + volatile fields).** These are dropped from the
field-level diff because they are regenerated on every save / creation and carry
no semantic meaning across two files:

| Tag | Why ignored |
|-----|-------------|
| `uuid` | object identity - regenerated every save/creation |
| `object-identifier` | simulation address - regenerated |
| `parent-name` | raw superior uuid - replaced by the derived `#superior` (by name) |
| `next-suspend-id` | runtime counter |
| `time-of-day`, `next-supply-check-time`, `set-scenario-start-time-at-local-on-first-object-creation-time` | scenario clock snapshots |

Add more with `--ignore <tag>`.

**Identity vs volatile - what is compared instead.** So that ignoring the raw
uuid-based `parent-name` does not hide real org changes, each object gains two
**derived, name-based** fields that ARE diffed:
* `#superior` - the superior resolved to its `marking-text` (or `Force N`);
* `#subordinates` - the ordered child marking names.
These make a change of "who is subordinate to whom" show up by name even though
the underlying uuids differ.

**Not auto-ignored (deliberately).** uuid-*valued* cross-references that are
semantic (e.g. a task's target entity, `entity-to-follow`) are left in, so a
different follow-target shows as a difference. In a cross-creation diff (Phase 2.2)
these will appear as differences purely because the referenced uuid differs; use
`--ignore <tag>` to suppress a specific one once judged non-semantic.

## Acceptance results

* **DUMP** runs on `C:\MAK\vrforces5.0.2\userData\scenarios\TropicTortoise.scnx`
  (the scenario referenced throughout the repo docs) - 3 objects, tree + detail.
  Also demonstrated on `HawaiiGround.scnx` (a 4-level BTY -> PLT -> SEC -> gun tree).
* **DIFF of a file against itself** -> `RESULT: IDENTICAL (zero differences)`.
* **DIFF against a copy with one hand-edited attribute** (`sea-state 3` -> `7`) ->
  exactly one field diff: `UNIT 'GlobalEnv 1' (1 field diff): ~ /state-repository/sea-state : A=3 B=7`.

## Known limitations / Phase 2.2 gaps

* Leader = `.oob` document order is assumed, not confirmed against a live reflected
  echelon ID (see the org-model caveat above).
* `.pln` (individual/global plans) and the boost-XML members (`.osrx`, `.spt`, ...)
  are not yet structurally parsed; if Phase 2.2 needs plan-statement-level diffing,
  the `.pln` (S-expression) parser is a small extension.
* Template *names* (e.g. "Tank Platoon (USA)") are not in the `.oob`; only the DIS
  enumeration is. Name resolution needs the 0.1 content catalog (`.sms`/`.opd`),
  which is a separate deliverable - the DIS tuple here is the join key to it.
* uuid-valued semantic cross-references (task targets) are not auto-normalized for
  cross-creation diffs (see "Not auto-ignored" above).
