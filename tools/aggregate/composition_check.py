#!/usr/bin/env python
"""OFFLINE gate for data/unit-composition-52-aggregate.json (C1, populated containers). ASCII only. Read-only.

WHAT IT PROVES (docs/DESIGN_AGGREGATE_CONTAINERS_2026-09-27.md): every composition row resolves, in the INSTALLED
AggregateTacticalLevel chain (tools/aggregate/survey_magx.py - the vendor best-match rule), to catalogue-SIMULATED
units with counts - a leaf is a warfare-model UNIT (AggregateLevelAggregate.ope), a sub-container is a CONTAINER
(PseudoAggregate.ope) that is itself composed (by another row, or by its template's own configured subordinates),
and nothing lands the base abstract or an empty container. UG52 72.2.1 p1419 is why: warfare-model units "do not
have the configuration options for specifying subordinates", containers do; an EMPTY container's move ends at
once (AggregateLevelBase/scripts/PA_Move_Along_Route.lua tick()).

Gates (each exits non-zero on failure):
  schema      fields, types, enums, counts >= 1, unique ids, ASCII
  container   each row's 'container' lands, by the vendor rule, the CONTAINER it names (never a UNIT/abstract)
  resolution  every UNIT entry lands the UNIT it names; every CONTAINER entry lands the CONTAINER it names and
              composes (a row ref, or CATALOGUE with non-empty configured subordinates, recursively)
  echelon     every subordinate is of a LOWER echelon than its container (UG52 40.80 p902: "For aggregate-level
              scenarios, the superior must be a higher echelon unit")
  nation      every simulated leaf carries the row's DIS country (unless its note says WRONG NATION)
  recursion   no compose cycle; depth <= MAX_DEPTH; every compose ref names a row
  mapKeys     every mapRowId exists in the aggregate type map; the row key equals its first map row's key
  coverage    every PERFORMER of the cut-A order resolves (the UnitTypeMap port in typemap_check.py) to a map row
              a composition row covers
Reports:
  --tree         each row expanded to its simulated leaves, with the Travel-posture footprints read off the
                 leaves' .entity files and the derived centroid-preserving ring (DeStacker.CentroidPreservingRadius)
  --init-census  the EMPTY container every Iron Storm unit is created as at init, by the design's rule
  --vendor       the vendor's aggregate sample scenarios: aggregate-state by role, containers vs the centroid of
                 their immediate subordinates, nesting, battalion-container member distances, units inside
                 containers, and which tasks the plans give containers
  --twins        what TODAY's order-time materialization (EntityLevel-rooted resolver) would do with each container
Run with the repo's pinned interpreter (%LOCALAPPDATA%\\Programs\\Python\\Python312\\python.exe):
    python tools/aggregate/composition_check.py                  # gate the committed draft
    python tools/aggregate/composition_check.py --tree --init-census --vendor
    python tools/aggregate/composition_check.py --selftest       # CLEAN control + DIRTY controls
"""
import collections
import copy
import glob
import json
import math
import os
import re
import sys
import xml.etree.ElementTree as ET
import zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)
import survey_magx as sm  # noqa: E402
import typemap_check as tc  # noqa: E402

DEF_COMPOSITION = os.path.join(REPO, "data", "unit-composition-52-aggregate.json")
DEF_MAP = os.path.join(REPO, "data", "unit-type-map-52-aggregate.json")
CUTA_ORDER = os.path.join(REPO, "data", "IRONSTORM_CUTA_Order.xml")
IRON_STORM_INIT = tc.IRON_STORM_INIT
SCENARIO_ROOT = os.path.join(sm.DEFAULT_VRF_HOME, "userData", "scenarios")
MAX_DEPTH = 4
ROLES = ("UNIT", "CONTAINER")
FIDELITIES = ("EXACT", "PROXY")
ROW_FIELDS = {"id": str, "key": dict, "mapRowIds": list, "servesUnits": str, "container": dict, "depth": str,
              "provenance": str, "subordinates": list, "omitted": list}
SUB_FIELDS = {"function": str, "count": int, "role": str, "objectType": str, "templateName": str,
              "fidelity": str, "note": str, "provenance": str}
C2SIM_NS = {"c": "http://www.sisostds.org/schemas/C2SIM/1.1"}

# Echelon RANK from the Unit Category field (UG52 D.3.4 p1689-1690). Team, Squad and Section are VR-Forces
# extensions numbered ABOVE the army echelons, so the category number is not an order - this is.
ECHELON_RANK = {1: 0, 2: 1, 12: 2, 13: 2, 14: 2, 3: 3, 4: 4, 5: 4, 6: 5, 7: 6, 8: 7, 9: 8, 10: 9, 11: 10}


def rank(t8):
    return ECHELON_RANK.get(t8[4]) if t8 else None


def centroid_preserving_radius(count, spacing):
    """PORT of DeStacker.CentroidPreservingRadius (src/VrfC2SimApp/DeStacker.cs:480-483): N children at equal
    bearings on ONE ring whose nearest-neighbour chord is `spacing`; their centroid is the anchor exactly."""
    if count <= 1 or not spacing > 0.0:
        return 0.0
    return spacing / (2.0 * math.sin(math.pi / count))


# ---------------------------------------------------------------------------------------------
# Leaf footprints (read off the .entity; UG52 27.1.2 p529-530: the footprint is a radius, and a unit is
# created in Travel posture, 27.1.3 p530)
# ---------------------------------------------------------------------------------------------

def entity_reals(path):
    out = {}
    try:
        root = ET.parse(path).getroot()
    except (ET.ParseError, OSError):
        return out
    for e in root.iter("real"):
        n = e.get("paramName")
        if n:
            try:
                out[n] = float((e.text or "").strip())
            except ValueError:
                pass
    return out


def travel_footprint(tmpl):
    """(base radius m, Travel modifier, Travel radius m, overlap speed modifier) or None."""
    r = entity_reals(tmpl.path)
    base = r.get("Base-Physical-Footprint")
    if base is None:
        return None
    mod = r.get("Footprint-Posture-Travel-Modifier", 1.0)
    return base, mod, base * mod, r.get("MaximumSpeed-Footprint-Overlap-Modifier")


# ---------------------------------------------------------------------------------------------
# Expansion: a row -> its tree of sub-units (the same walk the app's populate planner will make)
# ---------------------------------------------------------------------------------------------

class Node(object):
    __slots__ = ("path", "role", "t8", "tmpl", "count_path", "children", "source", "function")

    def __init__(self, **kw):
        for k in self.__slots__:
            setattr(self, k, kw.get(k))


def expand_row(comp, row_id, chain, problems, depth=0, stack=()):
    """Expand one row into a list of Nodes (children of the row's container). Problems are appended as
    (gate, text). CATALOGUE entries expand the template's configured subordinates, recursively."""
    rows = {r.get("id"): r for r in comp.get("rows", [])}
    row = rows.get(row_id)
    if row is None:
        problems.append(("recursion", "compose ref %s names no row" % row_id))
        return []
    if row_id in stack:
        problems.append(("recursion", "compose cycle %s" % " -> ".join(stack + (row_id,))))
        return []
    if depth > MAX_DEPTH:
        problems.append(("recursion", "row %s is deeper than MAX_DEPTH %d" % (row_id, MAX_DEPTH)))
        return []
    out = []
    for s in row.get("subordinates", []):
        t8, _ = sm.parse_type(s.get("objectType", ""))
        tmpl = chain.resolve(t8) if t8 else None
        for i in range(max(0, s.get("count", 0) if isinstance(s.get("count"), int) else 0)):
            n = Node(path="%s/%s%d" % (row_id, s.get("function", "?"), i + 1), role=s.get("role"), t8=t8,
                     tmpl=tmpl, children=[], source=row_id, function=s.get("function"))
            if s.get("role") == "CONTAINER":
                ref = s.get("compose", "")
                if ref == "CATALOGUE":
                    n.children = expand_catalogue(tmpl, chain, problems, depth + 1, n.path)
                elif ref:
                    n.children = expand_row(comp, ref, chain, problems, depth + 1, stack + (row_id,))
                else:
                    problems.append(("resolution", "%s: CONTAINER entry has no 'compose'" % n.path))
            out.append(n)
    return out


def expand_catalogue(tmpl, chain, problems, depth, where):
    if tmpl is None:
        return []
    if depth > MAX_DEPTH:
        problems.append(("recursion", "%s: CATALOGUE expansion deeper than MAX_DEPTH %d" % (where, MAX_DEPTH)))
        return []
    if not tmpl.subs:
        problems.append(("resolution", "%s: CATALOGUE compose on %s, which configures NO subordinates (an EMPTY "
                                       "container - its move would end at once)" % (where, tmpl.name)))
        return []
    out = []
    for k, (st, handle) in enumerate(tmpl.subs):
        lt = chain.resolve(st)
        n = Node(path="%s/%s%d" % (where, handle or "SUB", k + 1), role=lt.role if lt else "?", t8=st, tmpl=lt,
                 children=[], source="CATALOGUE:" + tmpl.name, function=handle or "SUB")
        if lt is not None and lt.role == "CONTAINER":
            n.children = expand_catalogue(lt, chain, problems, depth + 1, n.path)
        out.append(n)
    return out


def walk(nodes):
    for n in nodes:
        yield n
        for m in walk(n.children):
            yield m


def leaves_and_containers(nodes):
    lv = [n for n in walk(nodes) if n.role == "UNIT"]
    ct = [n for n in walk(nodes) if n.role == "CONTAINER"]
    return lv, ct


SIZING_RANK_MAX = ECHELON_RANK[5]   # a leaf ABOVE company echelon does not size a ring (design sec 5)


def reach(nodes, exact=False):
    """The ring these nodes form round their container (design sec 5): spacing = 2 x the largest REACH among
    the members that size it - a leaf's Travel-posture footprint radius (UG52 27.1.2-27.1.3: a unit is created
    in Travel), a sub-container's own ring radius + its largest member reach. A leaf above company echelon
    (a BN-level unit whose footprint is a battalion's area) takes a slot but does not size the ring unless
    exact=True: at birth it overlaps its neighbours, which only lowers their MAXIMUM speed by the
    MaximumSpeed-Footprint-Overlap-Modifier while they overlap (27.1.4), and the move converges every member
    onto one route anyway. Returns (reach of the whole ring, spacing, radius)."""
    sizing, alls = [], []
    for n in nodes:
        if n.role == "CONTAINER":
            r, _s, _R = reach(n.children, exact)
            sizing.append(r)
            alls.append(r)
        else:
            fp = travel_footprint(n.tmpl) if n.tmpl else None
            r = fp[2] if fp else 0.0
            alls.append(r)
            nr = rank(n.tmpl.otype) if n.tmpl else None
            if exact or nr is None or nr <= SIZING_RANK_MAX:
                sizing.append(r)
    if not alls:
        return 0.0, 0.0, 0.0
    spacing = 2.0 * max(sizing or alls)
    radius = centroid_preserving_radius(len(nodes), spacing)
    return radius + max(alls), spacing, radius


def flat_ring(nodes):
    """The FLAT alternative (design sec 4.4, decision D-8): every simulated leaf attached directly to the tasked
    container on ONE ring. Returns (leaves, spacing, radius, reach)."""
    lv, _ct = leaves_and_containers(nodes)
    return (len(lv),) + reach(lv)[1:] + (reach(lv)[0],)


# ---------------------------------------------------------------------------------------------
# The gates
# ---------------------------------------------------------------------------------------------

def gate_schema(g, comp):
    g.check(comp.get("schemaVersion") == 1, "schema: schemaVersion is 1", repr(comp.get("schemaVersion")))
    g.check(comp.get("modelSetKey") == "AggregateTacticalLevel", "schema: modelSetKey is AggregateTacticalLevel",
            repr(comp.get("modelSetKey")))
    rows = comp.get("rows")
    if not g.check(isinstance(rows, list) and rows, "schema: rows is a non-empty list"):
        return
    problems = []
    for r in rows:
        rid = r.get("id", "?")
        for k, typ in ROW_FIELDS.items():
            if k not in r or not isinstance(r[k], typ):
                problems.append("%s: field %s missing or not %s" % (rid, k, typ.__name__))
        key = r.get("key") if isinstance(r.get("key"), dict) else {}
        for k in ("functionId", "echelon", "nationRole", "nation"):
            if not isinstance(key.get(k), str) or not key.get(k):
                problems.append("%s: key.%s missing" % (rid, k))
        cont = r.get("container") if isinstance(r.get("container"), dict) else {}
        for k in ("objectType", "templateName", "fidelity"):
            if not isinstance(cont.get(k), str) or not cont.get(k):
                problems.append("%s: container.%s missing" % (rid, k))
        if cont.get("fidelity") not in FIDELITIES:
            problems.append("%s: container.fidelity %r" % (rid, cont.get("fidelity")))
        if isinstance(r.get("subordinates"), list) and not r["subordinates"]:
            problems.append("%s: no subordinates (an EMPTY container cannot move)" % rid)
        for s in r.get("subordinates", []) if isinstance(r.get("subordinates"), list) else []:
            where = "%s/%s" % (rid, s.get("function", "?"))
            for k, typ in SUB_FIELDS.items():
                if k not in s or not isinstance(s[k], typ) or isinstance(s[k], bool) != (typ is bool):
                    problems.append("%s: field %s missing or not %s" % (where, k, typ.__name__))
            if isinstance(s.get("count"), int) and not isinstance(s.get("count"), bool) and s["count"] < 1:
                problems.append("%s: count %d < 1" % (where, s["count"]))
            if s.get("role") not in ROLES:
                problems.append("%s: role %r" % (where, s.get("role")))
            if s.get("fidelity") not in FIDELITIES:
                problems.append("%s: fidelity %r" % (where, s.get("fidelity")))
            if s.get("fidelity") == "PROXY" and not s.get("note", "").strip():
                problems.append("%s: PROXY without a note" % where)
            if s.get("role") == "CONTAINER" and not s.get("compose"):
                problems.append("%s: CONTAINER without 'compose'" % where)
            if s.get("role") == "UNIT" and "compose" in s:
                problems.append("%s: a UNIT cannot be composed (can-have-subordinates False)" % where)
            t8, nf = sm.parse_type(s.get("objectType", ""))
            if t8 is None or nf != 8 or t8[0] != 3 or t8[1] != sm.UNIT_KIND or t8[2] != 1:
                problems.append("%s: objectType %r is not an 8-field kind-11 LAND unit type" % (where, s.get("objectType")))
    ids = collections.Counter(r.get("id") for r in rows)
    problems += ["duplicate row id %s" % k for k, v in ids.items() if v > 1]
    text = json.dumps(comp, ensure_ascii=False)
    bad = [c for c in text if ord(c) > 126]
    if bad:
        problems.append("non-ASCII character(s) %r" % sorted(set(bad))[:5])
    g.check(not problems, "schema: every row and entry has its fields, types and enums (%d rows)" % len(rows),
            "; ".join(problems[:6]) + (" ..." if len(problems) > 6 else ""))


def gate_container(g, comp, chain):
    bad = []
    for r in comp.get("rows", []):
        c = r.get("container", {})
        t8, _ = sm.parse_type(c.get("objectType", ""))
        t = chain.resolve(t8) if t8 else None
        if t is None or t.role != "CONTAINER":
            bad.append("%s: %s lands %s (%s)" % (r.get("id"), c.get("objectType"), t.name if t else "nothing",
                                                   t.role if t else "-"))
        elif c.get("templateName") not in (t.label, t.name):
            bad.append("%s: names %r, lands %r" % (r.get("id"), c.get("templateName"), t.label or t.name))
    g.check(not bad, "container: every row's container type lands the CONTAINER it names", "; ".join(bad[:6]))


def gate_resolution_and_tree(g, comp, chain):
    """resolution + echelon + nation + recursion over every row's expansion."""
    problems = []
    trees = {}
    for r in comp.get("rows", []):
        nodes = expand_row(comp, r.get("id"), chain, problems)
        trees[r.get("id")] = nodes
        # entries as authored: templateName and role
        for s in r.get("subordinates", []):
            t8, _ = sm.parse_type(s.get("objectType", ""))
            t = chain.resolve(t8) if t8 else None
            where = "%s/%s" % (r.get("id"), s.get("function"))
            if t is None or t.role == "BASE-ABSTRACT":
                problems.append(("resolution", "%s: %s lands %s" % (where, s.get("objectType"),
                                                                    t.name if t else "nothing")))
                continue
            if s.get("templateName") not in (t.label, t.name):
                problems.append(("resolution", "%s: names %r, lands %r" % (where, s.get("templateName"),
                                                                           t.label or t.name)))
            if t.role != s.get("role"):
                problems.append(("resolution", "%s: says %s, the template is a %s (%s)" % (where, s.get("role"),
                                                                                         t.role, t.name)))
        # the whole expansion: every leaf a UNIT, every container composed, echelons descending, nation
        c8, _ = sm.parse_type(r.get("container", {}).get("objectType", ""))
        nation_code = c8[3] if c8 else None

        def check(nodes_, parent_rank, parent_where):
            for n in nodes_:
                if n.tmpl is None or n.tmpl.role in ("BASE-ABSTRACT",) or n.role not in ROLES:
                    problems.append(("resolution", "%s lands %s" % (n.path, n.tmpl.name if n.tmpl else "nothing")))
                    continue
                nr = rank(n.tmpl.otype)
                if parent_rank is not None and (nr is None or nr >= parent_rank):
                    problems.append(("echelon", "%s (%s, %s) is not below its container %s" % (
                        n.path, n.tmpl.name, sm.echelon_of(n.tmpl), parent_where)))
                if n.role == "UNIT" and nation_code is not None and n.tmpl.otype[3] != nation_code:
                    problems.append(("nation", "%s (%s) is DIS country %d, the row's container is %d" % (
                        n.path, n.tmpl.name, n.tmpl.otype[3], nation_code)))
                if n.role == "CONTAINER":
                    if not n.children:
                        problems.append(("resolution", "%s (%s) composes to NOTHING" % (n.path, n.tmpl.name)))
                    check(n.children, nr, n.path)
        check(nodes, rank(c8) if c8 else None, r.get("id"))
    for gate in ("resolution", "echelon", "nation", "recursion"):
        mine = [t for k, t in problems if k == gate]
        label = {"resolution": "resolution: every leaf lands a simulated UNIT and every sub-container a composed CONTAINER",
                 "echelon": "echelon: every subordinate is below its container (UG52 40.80 p902)",
                 "nation": "nation: every simulated leaf carries its row's DIS country",
                 "recursion": "recursion: every compose ref resolves, no cycle, depth <= %d" % MAX_DEPTH}[gate]
        g.check(not mine, label, "; ".join(mine[:6]))
    return trees


def gate_map_keys(g, comp, typemap):
    rows_by_id = {r["id"]: r for r in typemap.get("rows", [])}
    bad = []
    for r in comp.get("rows", []):
        ids = r.get("mapRowIds") or []
        for mid in ids:
            if mid not in rows_by_id:
                bad.append("%s: map row %s does not exist" % (r.get("id"), mid))
        if ids and ids[0] in rows_by_id:
            m = rows_by_id[ids[0]]
            k = r.get("key", {})
            if (k.get("functionId"), k.get("echelon"), k.get("nationRole"), k.get("nation")) != \
                    (m["functionId"], m["echelon"], m["nationRole"], m["nation"]):
                bad.append("%s: key %s is not map row %s's key" % (r.get("id"), k, ids[0]))
        for mid in ids[1:]:
            m = rows_by_id.get(mid)
            k = r.get("key", {})
            if m and (m["echelon"], m["nationRole"], m["nation"]) != (k.get("echelon"), k.get("nationRole"),
                                                                     k.get("nation")):
                bad.append("%s: map row %s is another echelon or nation" % (r.get("id"), mid))
    g.check(not bad, "mapKeys: every mapRowId exists and matches the row key", "; ".join(bad[:6]))


def order_units(order_path):
    """(performer uuids, affected-entity uuids that are not their task's performer) of an order."""
    root = ET.parse(order_path).getroot()
    perf, aff = [], []
    for t in root.iter("{%s}ManeuverWarfareTask" % C2SIM_NS["c"]):
        pe = (t.findtext("c:PerformingEntity", "", C2SIM_NS) or "").strip()
        if pe and pe not in perf:
            perf.append(pe)
        for a in t.findall("c:AffectedEntity", C2SIM_NS):
            v = (a.text or "").strip()
            if v and v != pe and v not in aff:
                aff.append(v)
    return perf, aff


def gate_coverage(g, comp, typemap, init_path=IRON_STORM_INIT, order_path=CUTA_ORDER, friendly="USA",
                  opposing="RUS"):
    units = {u["uuid"]: u for u in tc.parse_init(init_path)}
    covered = set()
    for r in comp.get("rows", []):
        covered.update(r.get("mapRowIds") or [])
    perf, aff = order_units(order_path)
    lines, bad = [], []
    for uuid, kind in [(p, "performer") for p in perf] + [(a, "affected") for a in aff]:
        u = units.get(uuid)
        if u is None:
            bad.append("%s %s is not in the init" % (kind, uuid))
            continue
        row, key = tc.plan_unit(typemap["rows"], typemap["nations"], u, friendly, opposing)
        ok = row is not None and row["id"] in covered
        lines.append((u["name"], kind, row["id"] if row else "-", ok))
        if kind == "performer" and not ok:
            bad.append("%s -> %s has no composition row" % (u["name"], row["id"] if row else "no map row"))
    g.check(not bad, "coverage: every performer of the cut-A order has a composition (%d performers)" % len(perf),
            "; ".join(bad[:6]))
    return lines


class Gate(tc.Gate):
    pass


def run_gates(comp, typemap, chain, quiet=False):
    g = Gate(quiet=quiet)
    gate_schema(g, comp)
    if g.bad:
        return g, None, None
    gate_container(g, comp, chain)
    trees = gate_resolution_and_tree(g, comp, chain)
    gate_map_keys(g, comp, typemap)
    cov = gate_coverage(g, comp, typemap)
    return g, trees, cov


# ---------------------------------------------------------------------------------------------
# Reports
# ---------------------------------------------------------------------------------------------

def tree_report(comp, trees):
    out = []
    for r in comp.get("rows", []):
        nodes = trees.get(r["id"], [])
        lv, ct = leaves_and_containers(nodes)
        rch, spacing, radius = reach(nodes)
        xr, xs, xrad = reach(nodes, exact=True)
        out.append("%s (%s) -> container %s; %d simulated leaf unit(s), %d sub-container(s); immediate ring: %d "
                   "slot(s), spacing %.0f m, radius %.0f m, reach %.0f m (no overlap even for BN-level leaves: "
                   "spacing %.0f m, radius %.0f m, reach %.0f m)" % (
                       r["id"], r.get("depth"), r["container"]["templateName"], len(lv), len(ct), len(nodes),
                       spacing, radius, rch, xs, xrad, xr))
        fn, fs, frad, freach = flat_ring(nodes)
        out.append("  FLAT (D-8): %d leaves on one ring, spacing %.0f m, radius %.0f m, reach %.0f m" % (
            fn, fs, frad, freach))

        def show(ns, ind):
            for n in ns:
                fp = travel_footprint(n.tmpl) if (n.tmpl and n.role == "UNIT") else None
                extra = ""
                if fp:
                    extra = " | footprint %.0f m x Travel %.2f = %.0f m; overlap speed x%s" % (
                        fp[0], fp[1], fp[2], "%.2f" % fp[3] if fp[3] is not None else "?")
                elif n.role == "CONTAINER":
                    _r, s2, rad2 = reach(n.children)
                    extra = " | ring of %d, spacing %.0f m, radius %.0f m" % (len(n.children), s2, rad2)
                out.append("%s%s %s %s (%s, %s)%s" % ("  " * ind, n.path.split("/")[-1], n.role,
                                                       n.tmpl.name if n.tmpl else "?",
                                                       sm.echelon_of(n.tmpl) if n.tmpl else "?",
                                                       sm.type_str(n.t8), extra))
                show(n.children, ind + 1)
        show(nodes, 1)
    return out


# The design's init rule (sec 3): nation code + echelon category + a branch subcategory, completed from the
# landing container template's own matchType; the first candidate branch that lands a CONTAINER wins.
BRANCH_CANDIDATES = {
    "UCI": [3, 4], "UCIZ": [4], "UCIM": [18, 4], "UCA": [2], "UCATA": [2], "UCAW": [4, 2], "UCAAA": [12, 2],
    "UCRVA": [30, 5, 2], "UCF": [7, 8], "UCE": [10], "US": [31], "UCVR": [14, 3], "UCVRA": [14, 3],
    "GS": [30], "UCSGM": [18, 4],
}
ECHELON_CATEGORY = {"D": 3, "E": 5, "F": 6, "G": 7, "H": 8, "I": 9, "J": 10}
ECHELON_CODE_CATEGORY = {"PLT": 3, "COY": 5, "BN": 6, "RGT": 7, "BDE": 8, "DIV": 9, "CORPS": 10}
FALLBACK_BRANCHES = [3, 4, 2]


def container_for(chain, nation_code, category, branches):
    """(t8, template, exactBranch) of the first candidate that lands a CONTAINER, else (None, None, False)."""
    tried = list(branches) + [b for b in FALLBACK_BRANCHES if b not in branches]
    for i, sub in enumerate(tried):
        for spec, extra in ((1, 0), (1, 1)):
            t8 = (3, sm.UNIT_KIND, 1, nation_code, category, sub, spec, extra)
            t = chain.resolve(t8)
            if t is not None and t.role == "CONTAINER":
                return t8, t, i < len(branches) and i == 0
    return None, None, False


def init_census(chain, typemap, init_path=IRON_STORM_INIT, friendly="USA", opposing="RUS"):
    nations = typemap["nations"]
    units = tc.parse_init(init_path)
    rows = []
    for u in units:
        fid = tc.function_id_of(u["sidc"])
        e = tc.echelon_char_of(u["sidc"])
        cat = ECHELON_CATEGORY.get(e)
        flags = []
        if cat is None:
            cat = ECHELON_CODE_CATEGORY.get(u["echelon_code"], 6)
            flags.append("no SIDC echelon (%s) -> %s" % (u["echelon_code"] or "-", sm.ECHELON_ABBR.get(cat)))
        code = nations[opposing if u["hostility"] == "HO" else friendly]
        t8, t, exact = container_for(chain, code, cat, BRANCH_CANDIDATES.get(fid, []))
        if t is not None and not exact:
            flags.append("NEAREST branch (no %s container for %s)" % (sm.ECHELON_ABBR.get(cat), fid))
        created = bool(u["lat"] and u["lon"])
        rows.append((u, fid, t8, t, flags, created))
    return rows


def entity_twins(types, entity_chain):
    """What TODAY's order-time materialization would do with each container type: its resolver is rooted at
    EntityLevel (VrfC2SimService.GetResolver -> ObjectTypeResolver.LoadChain(home)), ExpandCoarseLeaves expands
    only a PURE higher unit, and anything else goes to case 3 - delete the shell and re-create it."""
    out = []
    for t8 in sorted(set(types)):
        e = entity_chain.resolve(t8)
        pure = bool(e is not None and e.subs and all(s[0][0] == 3 for s in e.subs))
        out.append((t8, e.name if e else "nothing", len(e.subs) if e else 0,
                    "EXPAND (case 2)" if pure else "DELETE + re-create (case 3)"))
    return out


# ---- the vendor's aggregate sample scenarios ------------------------------------------------

def _tokens(s):
    i, n = 0, len(s)
    while i < n:
        c = s[i]
        if c in "()":
            yield c
            i += 1
        elif c.isspace():
            i += 1
        elif c == '"':
            j = s.find('"', i + 1)
            j = n if j < 0 else j
            yield ("S", s[i + 1:j])
            i = j + 1
        elif c == ";":
            j = s.find("\n", i)
            i = n if j < 0 else j
        else:
            j = i
            while j < n and not s[j].isspace() and s[j] not in '()"':
                j += 1
            yield ("A", s[i:j])
            i = j


def sexpr(s):
    stack = [[]]
    for t in _tokens(s):
        if t == "(":
            stack.append([])
        elif t == ")":
            if len(stack) > 1:
                x = stack.pop()
                stack[-1].append(x)
        else:
            stack[-1].append(t)
    return stack[0]


def _head(node):
    return node[0][1] if isinstance(node, list) and node and isinstance(node[0], tuple) else None


def _val(node, name):
    for x in node[1:]:
        if isinstance(x, list) and _head(x) == name and len(x) > 1 and isinstance(x[1], tuple):
            return x[1][1]
    return None


def plan_tasks(pln_text):
    """[(owner uuid, task-type, script-id)] - a Task belongs to the plan it sits in; an issue-plan command
    switches the owner to the object it issues the plan to."""
    out = []

    def walk_(node, owner):
        if not isinstance(node, list):
            return
        h = _head(node)
        if h == "issue-plan":
            owner = _val(node, "plan-name") or owner
        elif h == "Task":
            out.append((owner, _val(node, "task-type"), _val(node, "script-id")))
        for x in (node[1:] if h else node):
            walk_(x, owner)
    tree = sexpr(pln_text)
    root = tree[0] if tree and isinstance(tree[0], list) and _head(tree[0]) is None else tree
    for p in root:
        if isinstance(p, list) and _head(p) == "Plan":
            walk_(p, _val(p, "plan-name"))
    return out


def scenario_objects(oob_text):
    objs = []
    for s, e in sm._blocks(oob_text):
        b = oob_text[s:e]
        ot = re.search(r"\(object-type\s+(-?\d+(?:\s+-?\d+){6})\s*\)", b)
        if not ot:
            continue
        t7 = tuple(int(x) for x in ot.group(1).split())
        if t7[0] != sm.UNIT_KIND:
            continue
        uu = re.search(r'\(uuid\s+"(VRF_UUID:[^"]+)"\)', b)
        mk = re.search(r'\(marking-text\s+"([^"]*)"\)', b)
        pn = re.findall(r'\(parent-name\s+"([^"]*)"\)', b)
        st = re.findall(r"\(aggregate-state\s+(\w+)\)", b)
        pos = re.search(r"\(position\s+(-?[\d.]+)\s+(-?[\d.]+)\s+(-?[\d.]+)\)", b)
        objs.append(dict(uuid=uu.group(1) if uu else "", marking=mk.group(1) if mk else "?", t8=(3,) + t7,
                         parent=pn[-1] if pn else "", state=st[-1] if st else "",
                         xyz=tuple(float(v) for v in pos.groups()) if pos else None))
    return objs


def orbat_roles(orb_text, chain):
    """uuid -> role for the ORBAT-panel objects of a scenario (.orb), which a plan may task before (or
    without) the object being in the saved .oob."""
    roles = {}
    for m in re.finditer(r"\(orbat-data(.*?)\n   \)", orb_text, re.S):
        b = m.group(1)
        uu = re.search(r'\(uuid\s+"([^"]+)"\)', b)
        ty = re.search(r"\(type\s+(-?\d+(?:\s+-?\d+){6})\)", b)
        if uu and ty:
            t7 = tuple(int(x) for x in ty.group(1).split())
            if t7[0] == sm.UNIT_KIND:
                t = chain.resolve((3,) + t7)
                roles[uu.group(1)] = t.role if t else "?"
    return roles


def vendor_report(chain, root=SCENARIO_ROOT):
    files = sorted(f for f in glob.glob(os.path.join(root, "**", "*.scnx"), recursive=True)
                   if "ggregate" in f or "first_experience_aggregate" in f)
    out, totals, all_tasks = [], collections.Counter(), collections.Counter()
    for f in files:
        try:
            z = zipfile.ZipFile(f)
        except (zipfile.BadZipFile, OSError):
            continue
        names = z.namelist()

        def read(ext):
            ns = [n for n in names if n.lower().endswith(ext)]
            return z.read(ns[0]).decode("utf-8", "replace") if ns else ""
        objs = scenario_objects(read(".oob"))
        for o in objs:
            t = chain.resolve(o["t8"])
            o["role"] = t.role if t else "?"
        by = {o["uuid"]: o for o in objs}
        kids = collections.defaultdict(list)
        for o in objs:
            kids[o["parent"]].append(o)
        rs = collections.Counter((o["role"], o["state"]) for o in objs)
        for k, v in rs.items():
            totals[k] += v
        conts = [o for o in objs if o["role"] == "CONTAINER"]
        resid, unplaced = [], 0
        for o in conts:
            ks = kids.get(o["uuid"], [])
            placed = [k for k in ks if k["xyz"] and math.sqrt(sum(v * v for v in k["xyz"])) > 1.0e6]
            unplaced += len(ks) - len(placed)
            if placed and len(placed) == len(ks) and o["xyz"]:
                c = [sum(k["xyz"][i] for k in placed) / len(placed) for i in range(3)]
                resid.append(math.sqrt(sum((o["xyz"][i] - c[i]) ** 2 for i in range(3))))
        nested = sum(1 for o in conts if by.get(o["parent"], {}).get("role") == "CONTAINER")
        bn_near, bn_far = [], 0
        for o in conts:
            if o["t8"][4] != 6 or not o["xyz"]:
                continue
            ds = [math.sqrt(sum((k["xyz"][i] - o["xyz"][i]) ** 2 for i in range(3)))
                  for k in kids.get(o["uuid"], []) if k["xyz"]]
            if len(ds) > 1 and max(ds) <= 2000.0:
                bn_near.extend(ds)
            elif ds and max(ds) > 2000.0:
                bn_far += 1
        inside = sum(1 for o in objs if o["role"] == "UNIT" and by.get(o["parent"], {}).get("role") == "CONTAINER")
        units = sum(1 for o in objs if o["role"] == "UNIT")
        unit_parents = [o["marking"] for o in objs if o["role"] == "UNIT" and kids.get(o["uuid"])]
        empty = [o["marking"] for o in conts if not kids.get(o["uuid"])]
        roles = orbat_roles(read(".orb"), chain)
        roles.update({o["uuid"]: o["role"] for o in objs})
        tasks = collections.Counter()
        pln = read(".pln")
        if pln:
            for owner, tt, sid in plan_tasks(pln):
                if roles.get(owner) == "CONTAINER":
                    tasks[tt] += 1
                    all_tasks[tt] += 1
        bn_note = ""
        if bn_near or bn_far:
            s = sorted(bn_near)
            bn_note = (" | battalion containers: %d deployed beyond 2 km; the others' member distances %s" % (
                bn_far, ("%.0f-%.0f m, median %.0f m over %d" % (s[0], s[-1], s[(len(s) - 1) // 2], len(s)))
                if s else "n/a"))
        out.append("%s: %s | containers %d (empty %d, nested in a container %d), units %d of which inside a container "
                   "%d, units with children %d | container - centroid of its immediate subordinates: %s%s%s | plan "
                   "tasks on containers (.oob or .orb): %s"
                   % (os.path.basename(f), ", ".join("%s/%s %d" % (k[0], k[1] or "-", v) for k, v in sorted(rs.items())),
                      len(conts), len(empty), nested, units, inside, len(unit_parents),
                      ("max %.1f m over %d fully placed container(s)" % (max(resid), len(resid))) if resid else "n/a",
                      (" (%d subordinate(s) unplaced/embarked, their containers skipped)" % unplaced) if unplaced else "",
                      bn_note, dict(tasks) if tasks else "none"))
    out.append("TOTAL role/state over %d scenario(s): %s" % (len(files), ", ".join(
        "%s/%s %d" % (k[0], k[1] or "-", v) for k, v in sorted(totals.items()))))
    out.append("TOTAL plan tasks on containers: %s" % (dict(all_tasks) if all_tasks else "none"))
    return out


# ---------------------------------------------------------------------------------------------
# Self-test: CLEAN control, the draft's pinned numbers, then DIRTY controls
# ---------------------------------------------------------------------------------------------

def selftest(path=DEF_COMPOSITION):
    g = Gate()
    chain = sm.Chain()
    typemap = json.load(open(DEF_MAP, encoding="utf-8"))
    comp = json.load(open(path, encoding="utf-8"))
    print("--- 1. ports ---")
    g.check(abs(centroid_preserving_radius(4, 350.0) - 247.487) < 0.01 and
            abs(centroid_preserving_radius(6, 350.0) - 350.0) < 1e-6 and centroid_preserving_radius(1, 350.0) == 0.0,
            "ring radius port reproduces DeStacker.cs:438-447 (N=4 247.5 m, N=6 350.0 m, N=1 not spread)")
    for n in (2, 3, 5, 7):
        rad = centroid_preserving_radius(n, 180.0)
        pts = [(rad * math.cos(2 * math.pi * k / n), rad * math.sin(2 * math.pi * k / n)) for k in range(n)]
        cx, cy = sum(p[0] for p in pts) / n, sum(p[1] for p in pts) / n
        chord = math.hypot(pts[1][0] - pts[0][0], pts[1][1] - pts[0][1])
        g.check(math.hypot(cx, cy) < 1e-6 and abs(chord - 180.0) < 1e-6,
                "a ring of %d keeps its centroid on the anchor and its chord at the spacing" % n)
    g.check(rank(sm.parse_type("11:1:225:14:2:1:2")[0]) < rank(sm.parse_type("11:1:225:3:2:0:1")[0]) <
            rank(sm.parse_type("11:1:225:5:4:0:0")[0]) < rank(sm.parse_type("11:1:225:6:3:1:0")[0]) <
            rank(sm.parse_type("11:1:225:8:3:1:1")[0]), "echelon rank orders SEC < PLT < CO < BN < BDE")
    t = chain.resolve(sm.parse_type("11:1:225:8:3:1:1")[0])
    g.check(t is not None and t.role == "CONTAINER" and not t.subs,
            "the US infantry BDE type lands an EMPTY generic container (the init shell)", t.name if t else "nothing")
    t = chain.resolve(sm.parse_type("11:1:225:6:2:1:0")[0])
    g.check(t is not None and t.role == "CONTAINER" and len(t.subs) == 4,
            "Armor CAB Group (USA) carries 4 configured subordinates (source 2)", t.name if t else "nothing")
    fp = travel_footprint(chain.resolve(sm.parse_type("11:1:225:5:4:0:0")[0]))
    g.check(fp is not None and fp[0] == 300.0 and abs(fp[2] - 90.0) < 1e-6 and fp[3] == 0.9,
            "Mech CO (USA, M2) footprint 300 m x Travel 0.3 = 90 m, overlap speed x0.9 (its .entity)", repr(fp))
    tw = entity_twins([sm.parse_type("11:1:225:8:3:1:1")[0]], sm.Chain(top_sms="EntityLevel"))
    g.check(tw and tw[0][3].startswith("DELETE"),
            "TODAY's EntityLevel-rooted materialization would DELETE the 48 IBCT container (the in-place rule's reason)",
            repr(tw))

    print("--- 2. CLEAN control: the committed draft passes every gate, with its pinned counts ---")
    clean, trees, _cov = run_gates(comp, typemap, chain, quiet=True)
    g.check(clean.bad == 0, "CLEAN: %s passes all gates" % os.path.basename(path), "; ".join(clean.failures))
    pinned = {"C-USA-DIV-UCI": (1, 0), "C-USA-BDE-UCI": (17, 3), "C-USA-BN-UCI": (5, 0),
              "C-USA-BDE-UCA": (26, 6), "C-USA-BN-UCIZ": (8, 1)}
    if trees:
        for rid, (nl, nc) in pinned.items():
            lv, ct = leaves_and_containers(trees.get(rid, []))
            g.check((len(lv), len(ct)) == (nl, nc), "%s expands to %d leaf unit(s) and %d sub-container(s)" % (
                rid, nl, nc), "got %d and %d" % (len(lv), len(ct)))

    print("--- 3. DIRTY controls: each defect must FAIL its own gate ---")

    def dirty(name, mutate, expect):
        d = copy.deepcopy(comp)
        mutate(d)
        res, _t, _c = run_gates(d, typemap, chain, quiet=True)
        hit = any(expect in f for f in res.failures)
        g.check(hit, "DIRTY %s -> fails '%s' (%d gate(s) failed)" % (name, expect, res.bad),
                "failures: %s" % res.failures)

    def row(d, rid):
        return next(r for r in d["rows"] if r["id"] == rid)

    def container_as_unit(d):
        s = row(d, "C-USA-BDE-UCA")["subordinates"][1]
        s["role"] = "UNIT"
        del s["compose"]

    def abstract_leaf(d):
        row(d, "C-USA-BN-UCI")["subordinates"][1].update(objectType="3:11:1:225:5:3:1:75",
                                                          templateName="Rifle CO USMC PA")

    def empty_catalogue(d):
        row(d, "C-USA-BDE-UCI")["subordinates"][1]["compose"] = "CATALOGUE"

    def cycle(d):
        row(d, "C-USA-BN-UCIZ")["subordinates"][1].update(role="CONTAINER", objectType="3:11:1:225:8:2:1:0",
                                                          templateName="BDE, Armor", compose="C-USA-BDE-UCA")

    def echelon_up(d):
        row(d, "C-USA-BN-UCI")["subordinates"][1].update(role="CONTAINER", objectType="3:11:1:225:8:3:1:1",
                                                         templateName="BDE", compose="C-USA-BN-UCIZ")

    def wrong_nation(d):
        row(d, "C-USA-BN-UCI")["subordinates"][1].update(objectType="3:11:1:260:5:2:0:64",
                                                         templateName="Tank CO (RUS, T-90)")

    def zero_count(d):
        row(d, "C-USA-BN-UCI")["subordinates"][1]["count"] = 0

    def bad_map_row(d):
        row(d, "C-USA-BDE-UCI")["mapRowIds"] = ["F-NO-SUCH-ROW"]

    def uncovered(d):
        row(d, "C-USA-BDE-UCI")["mapRowIds"] = []

    def unit_container(d):
        row(d, "C-USA-BN-UCI")["container"].update(objectType="3:11:1:225:5:4:0:0", templateName="Mech CO (USA, M2)")

    def non_ascii(d):
        row(d, "C-USA-BN-UCI")["subordinates"][1]["note"] += " " + chr(0x2014) + " dash"   # an em dash, built so this file stays ASCII

    def proxy_no_note(d):
        row(d, "C-USA-BN-UCI")["subordinates"][1]["note"] = ""

    def empty_row(d):
        row(d, "C-USA-DIV-UCI")["subordinates"] = []

    dirty("a sub-container marked UNIT", container_as_unit, "resolution")
    dirty("a leaf that lands the base abstract", abstract_leaf, "resolution")
    dirty("CATALOGUE on a container with no configured subordinates", empty_catalogue, "resolution")
    dirty("a compose cycle", cycle, "recursion")
    dirty("a BDE inside a BN", echelon_up, "echelon")
    dirty("a RUS leaf in a USA row", wrong_nation, "nation")
    dirty("a count of 0", zero_count, "schema")
    dirty("a map row that does not exist", bad_map_row, "mapKeys")
    dirty("48 IBCT with no composition", uncovered, "coverage")
    dirty("a row whose container is a UNIT", unit_container, "container")
    dirty("a non-ASCII note", non_ascii, "schema")
    dirty("a PROXY with no note", proxy_no_note, "schema")
    dirty("a row with no subordinates", empty_row, "schema")
    print("COMPOSITION SELFTEST %s (%d problem(s))" % ("PASS" if g.bad == 0 else "FAIL", g.bad))
    return 0 if g.bad == 0 else 1


def main(argv=None):
    import argparse
    ap = argparse.ArgumentParser(description="Offline gate for the aggregate-level composition table (C1).")
    ap.add_argument("composition", nargs="?", default=DEF_COMPOSITION)
    ap.add_argument("--map", default=DEF_MAP)
    ap.add_argument("--tree", action="store_true", help="print each row expanded, with footprints and rings")
    ap.add_argument("--init-census", action="store_true", help="print the container every Iron Storm unit gets")
    ap.add_argument("--vendor", action="store_true", help="print the vendor aggregate-scenario evidence")
    ap.add_argument("--twins", action="store_true",
                    help="print what TODAY's EntityLevel-rooted materialization would do with each container type")
    ap.add_argument("--selftest", action="store_true")
    a = ap.parse_args(argv)
    if a.selftest:
        return selftest(a.composition)
    print("=== aggregate composition gate: %s ===" % a.composition)
    comp = json.load(open(a.composition, encoding="utf-8"))
    typemap = json.load(open(a.map, encoding="utf-8"))
    chain = sm.Chain()
    g, trees, cov = run_gates(comp, typemap, chain)
    if cov:
        for name, kind, rid, ok in cov:
            print("  [info] cut-A %-9s %-52s -> map row %-10s %s" % (kind, name[:52], rid,
                                                                   "composed" if ok else "NOT composed"))
    if trees and a.tree:
        print("--- composition tree ---")
        for line in tree_report(comp, trees):
            print(line)
    if a.init_census:
        print("--- init census: the EMPTY container each Iron Storm unit is created as (hostile RUS) ---")
        rows = init_census(chain, typemap)
        bad = 0
        for u, fid, t8, t, flags, created in rows:
            if created and (t is None or t.role != "CONTAINER"):
                bad += 1
            print("  %-48s %s %-6s %-6s -> %-22s %-26s %s%s" % (
                u["name"][:48], "H" if u["hostility"] == "HO" else "F", u["echelon_code"], fid,
                sm.type_str(t8) if t8 else "-", (t.label or t.name) if t else "NO CONTAINER",
                "; ".join(flags), "" if created else " (not created: no coordinates)"))
        n_created = sum(1 for r in rows if r[5])
        g.check(bad == 0, "init census: all %d created units land an EMPTY-able CONTAINER by the rule" % n_created)
    if a.twins:
        print("--- today's MaterializeUnit on a container: the EntityLevel twin of each container type ---")
        types = [sm.parse_type(r["container"]["objectType"])[0] for r in comp.get("rows", [])]
        types += [t8 for _u, _f, t8, _t, _fl, created in init_census(chain, typemap) if t8 and created]
        for t8, name, nsubs, verdict in entity_twins(types, sm.Chain(top_sms="EntityLevel")):
            print("  %-22s -> EntityLevel '%s' (%d subordinate(s)) -> %s" % (sm.type_str(t8), name, nsubs, verdict))
    if a.vendor:
        print("--- vendor aggregate scenarios (%s) ---" % SCENARIO_ROOT)
        for line in vendor_report(chain):
            print("  " + line)
    print("COMPOSITION GATE %s (%d problem(s))" % ("PASS" if g.bad == 0 else "FAIL", g.bad))
    return 0 if g.bad == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
