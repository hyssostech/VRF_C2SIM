#!/usr/bin/env python
"""OFFLINE gate for data/unit-type-map-52-aggregate.json. ASCII only. Read-only.

WHY THIS EXISTS INSTEAD OF A --parse-init RUN: the app cannot select a type map offline -
`VrfC2SimApp --parse-init` plans every unit with the legacy RealTemplates dispatch
(src/VrfC2SimApp/InitParseCheck.cs: UnitTranslator.Plan(u) with no map), and
`--typemap-selftest` picks its map from the chain root (TypeMapSelfTest.cs: EntityLevel ->
unit-type-map-52.json). So this script PORTS the pieces that decide what a unit becomes and
applies them to the aggregate map:
  * UnitTypeMap.FunctionIdOf / EchelonCharOf / Lookup (keys b, c, d, e) and FindByObjectType
    (key a) - src/VrfC2SimApp/UnitTypeMap.cs;
  * UnitTranslator.Plan's pre-table dispatch (air / sea / DIS domain 3 / neutral never reach
    the table) - src/VrfC2SimApp/UnitTranslator.cs;
  * InitParser's hostility rule (first ForceSide = friendly, others take its relation code),
    its UUID sort and its single-pass superior-coordinate cascade - src/VrfC2SimApp/InitParser.cs;
  * UnitTypeMap.CheckNationSupported (JC-2).
The port is PROVED against the app's own known answers on the ENTITY map before it is trusted
(--selftest, section 1: the four TypeMapSelfTest.cs lookups).

Gates (every one exits non-zero on failure):
  schema       the entity map's fields, types and enums, plus modelSetKey
  uniqueness   ids; (functionId, echelon, nationRole, nation) keys; echelon-only and catch-all rows
  resolution   every objectType lands, by the vendor best-match rule over the INSTALLED
               AggregateTacticalLevel chain (tools/aggregate/survey_magx.py), the template the row
               names; never the base abstract; a warfare-model UNIT, or a CONTAINER whose
               subordinates all resolve to real templates (an EMPTY container cannot move)
  app trap     no objectType whose ENTITY-level twin is a pure higher unit: the app's resolver is
               rooted at EntityLevel.sms and CreationPolicy=AtOrder would EXPAND it
  coverage     every unit of data/STP-IRON-STORM-SYNTHETIC_Initialization.xml resolves to a
               usable row for the friendly nation and for EACH hostile nation the map carries
  authored     (package C2, RL-20260927-04) the map's "authoredRows": the AUTHORED US unit types of
               the derived set C2SIM_AggregateTacticalLevel (tools/sms). They are NOT lookup rows -
               the app looks up "rows" only (UnitTypeMap.Lookup), and parses fidelity AUTHORED there
               as Failed (ParseFidelity) while Lookup ignores fidelity, so an AUTHORED row in "rows"
               is refused. Since C1b the app ALSO reads "authoredRows" (UnitTypeMap.AuthoredRows,
               fidelity Authored) for the composition's authored variant and its derived-SMS guard
               only. Gated: schema; one row per design type (tools/sms/aggregate_authored_
               design.json) with the same type, label, .entity and .magx; on the DEPLOYED derived
               chain each lands its own warfare-model UNIT and its .magx names it; on the VENDOR chain
               it does NOT (the wrong-SMS hazard - what it would land is printed); the derived chain
               resolves every vendor template and every map row exactly as the vendor chain does
               (the set only ADDS); no EntityLevel twin the app would EXPAND.
Run with the repo's pinned interpreter:
    python tools/aggregate/typemap_check.py                 # gate the committed map
    python tools/aggregate/typemap_check.py --census-md     # + the per-unit census as markdown
    python tools/aggregate/typemap_check.py --selftest      # port proof + CLEAN/DIRTY controls
    python tools/aggregate/typemap_check.py --derived-sms <path>   # a derived set other than the default
The authored gate needs the derived set DEPLOYED (tools/sms/Deploy-C2SimAggregateSms.ps1); it FAILS,
naming the command, when it is not.
"""
import collections
import copy
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)
import survey_magx as sm  # noqa: E402

DEF_MAP = os.path.join(REPO, "data", "unit-type-map-52-aggregate.json")
ENTITY_MAP = os.path.join(REPO, "data", "unit-type-map-52.json")
IRON_STORM_INIT = os.path.join(REPO, "data", "STP-IRON-STORM-SYNTHETIC_Initialization.xml")
C2SIM_NS = {"c": "http://www.sisostds.org/schemas/C2SIM/1.1"}

ROW_FIELDS = {"id": str, "surveyRow": str, "functionId": str, "echelon": str, "echelonCode": str,
              "nationRole": str, "nation": str, "isAggregate": bool, "objectType": str,
              "templateName": str, "fidelity": str, "proxyNote": str}
OPTIONAL_ROW_FIELDS = {"role": str, "entityFile": str, "magx": str}
FIDELITIES = ("EXACT", "PROXY", "AUTHORED_PENDING")
# authoredRows (package C2): not lookup rows - see the module note.
AUTHORED_FIELDS = {"id": str, "templateName": str, "objectType": str, "entityFile": str, "magx": str, "role": str,
                   "isAggregate": bool, "fidelity": str, "nationRole": str, "nation": str, "echelonCode": str,
                   "modelSet": str, "recipeId": str, "proxyNote": str}
OPTIONAL_AUTHORED_FIELDS = {"servesComposition": list}
DESIGN = os.path.join(REPO, "tools", "sms", "aggregate_authored_design.json")
ECHELON_CODES = ("", "NOS", "TEAM", "SQUAD", "SECT", "PLT", "COY", "BN", "RGT", "BDE", "DIV", "CORPS",
                 "ARMY", "AG", "REGION")


# ---------------------------------------------------------------------------------------------
# Ports of the app's key logic (UnitTypeMap.cs)
# ---------------------------------------------------------------------------------------------

def function_id_of(sidc):
    """UnitTypeMap.FunctionIdOf: SIDC positions 5-10 (0-based 4..9), trailing '-' trimmed."""
    if not sidc or len(sidc) < 5:
        return "(none)"
    f = sidc[4:min(10, len(sidc))].rstrip("-")
    return f if f else "(none)"


def echelon_char_of(sidc):
    """UnitTypeMap.EchelonCharOf: SIDC position 12 (0-based 11), verbatim; '' when absent."""
    return sidc[11] if sidc and len(sidc) >= 12 else ""


def row_echelon(r):
    e = r.get("echelon", "")
    return e[0] if e else ""


def lookup(rows, function_id, echelon, echelon_code, nation_role, nation):
    """UnitTypeMap.Lookup, keys (b) -> (c) -> (d) -> (e). Returns (row or None, key)."""
    def nat(r):
        return (r["nationRole"].lower() == nation_role.lower()
                and r["nation"].lower() == nation.lower())
    for r in rows:
        if nat(r) and r["functionId"] and r["functionId"] == function_id and row_echelon(r) == echelon:
            return r, "b:functionId+sidcEchelon"
    if echelon_code:
        for r in rows:
            if (nat(r) and r["functionId"] and r["functionId"] == function_id
                    and r["echelonCode"].lower() == echelon_code.lower()):
                return r, "c:functionId+echelonCode"
    for r in rows:
        if nat(r) and not r["functionId"] and row_echelon(r) == echelon:
            return r, "d:sidcEchelon"
    for r in rows:
        if nat(r) and not r["functionId"] and row_echelon(r) == "*":
            return r, "e:catchAll"
    return None, "none"


def find_by_object_type(rows, t8):
    """UnitTypeMap.FindByObjectType (key a's backstop)."""
    for r in rows:
        if r["fidelity"] != "AUTHORED_PENDING" and r["objectType"] == t8:
            return r
    return None


def vrf_type_from_init_dis(dis7):
    """UnitTypeMap.VrfObjectTypeFromInitDis: 7 ints -> '3:...' / '1:...', None when all zero."""
    if dis7 is None or len(dis7) != 7 or all(v == 0 for v in dis7):
        return None
    return "%d:%s" % (3 if dis7[0] == 11 else 1, ":".join(str(v) for v in dis7))


def check_nation_supported(rows, nations, nation_role, nation):
    """UnitTypeMap.CheckNationSupported (JC-2). None = usable, else the error."""
    if not nation:
        return "empty nation"
    if nation not in nations:
        return "nation %s is not in the map's nations block" % nation
    mine = [r for r in rows if r["nationRole"] == nation_role and r["nation"] == nation]
    if not mine:
        return "no %s rows for %s" % (nation_role, nation)
    usable = [r for r in mine if r["fidelity"] in ("EXACT", "PROXY") and r["isAggregate"]]
    return None if usable else "no usable UNIT row for %s/%s" % (nation_role, nation)


# ---------------------------------------------------------------------------------------------
# The init, parsed the way InitParser.cs parses it
# ---------------------------------------------------------------------------------------------

def parse_init(path):
    root = ET.parse(path).getroot()
    sides = root.findall(".//c:ForceSide", C2SIM_NS)
    blue = (sides[0].findtext("c:UUID", "", C2SIM_NS) or "").strip() if sides else ""
    rel = {}
    if sides:
        for fr in sides[0].findall("c:ForceSideRelation", C2SIM_NS):
            rel[(fr.findtext("c:OtherSide", "", C2SIM_NS) or "").strip()] = \
                (fr.findtext("c:HostilityStatusCode", "", C2SIM_NS) or "").strip()

    def hostility(side):
        return "FR" if side == blue else rel.get(side, "")

    units = []
    for u in root.findall(".//c:Unit", C2SIM_NS):
        uuid = (u.findtext("c:UUID", "", C2SIM_NS) or "").strip()
        if not uuid:
            continue
        loc = u.find(".//c:Location//c:GeodeticCoordinate", C2SIM_NS)
        lat = (loc.findtext("c:Latitude", "", C2SIM_NS) or "").strip() if loc is not None else ""
        lon = (loc.findtext("c:Longitude", "", C2SIM_NS) or "").strip() if loc is not None else ""
        siso = u.find("c:SISOEntityType", C2SIM_NS)
        dis = None
        if siso is not None:
            try:
                dis = [int(siso.findtext("c:" + k, "0", C2SIM_NS)) for k in
                       ("DISKind", "DISDomain", "DISCountry", "DISCategory", "DISSubCategory",
                        "DISSpecific", "DISExtra")]
            except ValueError:
                dis = None
        units.append(dict(
            name=(u.findtext("c:Name", "", C2SIM_NS) or "").strip(), uuid=uuid,
            sidc=(u.findtext(".//c:APP6C-SIDC", "", C2SIM_NS) or "").strip(),
            echelon_code=(u.findtext("c:EchelonCode", "", C2SIM_NS) or "").strip(),
            hostility=hostility((u.findtext("c:EntityDescriptor/c:Side", "", C2SIM_NS) or "").strip()),
            superior=(u.findtext("c:EntityDescriptor/c:Superior", "", C2SIM_NS) or "").strip(),
            lat=lat, lon=lon, dis=dis, dis_domain=(dis[1] if dis else 0)))
    units.sort(key=lambda x: x["uuid"])            # InitParser: string.CompareOrdinal on the uuid
    idx = {x["uuid"]: i for i, x in enumerate(units)}
    for x in units:                                 # ONE forward pass, reading the superior's CURRENT coords
        if x["lat"] and x["lon"]:
            continue
        si = idx.get(x["superior"])
        if si is None:
            continue
        sup = units[si]
        if sup["lat"] and sup["lon"]:
            x["lat"], x["lon"], x["inherited"] = sup["lat"], sup["lon"], True
    return units


def plan_unit(rows, nations, unit, friendly, opposing):
    """UnitTranslator.Plan's dispatch + FromTable (FidelityTable mode)."""
    sidc = unit["sidc"]
    ho = unit["hostility"] == "HO"
    if sidc:
        if len(sidc) > 2 and sidc[2] == "A":
            return None, "not-table: air (SIDC position 3)"
        if len(sidc) > 2 and sidc[2] == "S":
            return None, "not-table: sea surface"
        if unit["dis_domain"] == 3:
            return None, "not-table: DIS domain 3"
        if len(sidc) > 1 and sidc[1] == "N":
            return None, "not-table: neutral"
    role = "hostile" if ho else "friendly"
    nation = opposing if ho else friendly
    declared = vrf_type_from_init_dis(unit["dis"])
    if declared:
        f = declared.split(":")
        named = next((n for n, c in nations.items() if str(c) == f[3] and f[3] != "0"), None)
        if named and named != nation:
            nation = named
        covered = find_by_object_type(rows, declared)
        if covered:
            return covered, "a:initSISOEntityType"
    return lookup(rows, function_id_of(sidc), echelon_char_of(sidc), unit["echelon_code"], role, nation)


# ---------------------------------------------------------------------------------------------
# The gates
# ---------------------------------------------------------------------------------------------

class Gate(object):
    """quiet=True collects the verdicts without printing them - the self-test's controls run
    the SAME gates on mutated copies and report one line per control instead."""
    def __init__(self, quiet=False):
        self.bad = 0
        self.quiet = quiet
        self.failures = []

    def check(self, ok, what, detail=""):
        if not ok:
            self.bad += 1
            self.failures.append(what + ((" -- " + detail) if detail else ""))
        if not self.quiet:
            print("  [%s] %s%s" % ("PASS" if ok else "FAIL", what, (" -- " + detail) if detail and not ok else ""))
        return ok


def gate_schema(g, m):
    g.check(m.get("schemaVersion") == 1, "schema: schemaVersion is 1", repr(m.get("schemaVersion")))
    g.check(m.get("modelSetKey") == "AggregateTacticalLevel", "schema: modelSetKey is AggregateTacticalLevel",
            repr(m.get("modelSetKey")))
    nations = m.get("nations")
    g.check(isinstance(nations, dict) and nations and all(isinstance(v, int) for v in nations.values()),
            "schema: nations is a name -> DIS country map", repr(nations))
    rows = m.get("rows")
    if not g.check(isinstance(rows, list) and rows, "schema: rows is a non-empty list"):
        return
    problems = []
    for r in rows:
        rid = r.get("id", "?")
        for k, typ in ROW_FIELDS.items():
            if k not in r or not isinstance(r[k], typ):
                problems.append("%s: field %s missing or not %s" % (rid, k, typ.__name__))
        for k, typ in OPTIONAL_ROW_FIELDS.items():
            if k in r and not isinstance(r[k], typ):
                problems.append("%s: field %s not %s" % (rid, k, typ.__name__))
        extra = set(r) - set(ROW_FIELDS) - set(OPTIONAL_ROW_FIELDS)
        if extra:
            problems.append("%s: unknown field(s) %s" % (rid, sorted(extra)))
        if r.get("fidelity") == "AUTHORED":
            problems.append("%s: fidelity AUTHORED belongs in authoredRows - the app parses it as Failed "
                            "(UnitTypeMap.ParseFidelity) and Lookup ignores fidelity" % rid)
        elif r.get("fidelity") not in FIDELITIES:
            problems.append("%s: fidelity %r" % (rid, r.get("fidelity")))
        if r.get("nationRole") not in ("friendly", "hostile"):
            problems.append("%s: nationRole %r" % (rid, r.get("nationRole")))
        if isinstance(nations, dict) and r.get("nation") not in nations:
            problems.append("%s: nation %r not in nations" % (rid, r.get("nation")))
        if len(r.get("echelon", "")) != 1:
            problems.append("%s: echelon must be ONE character" % rid)
        if r.get("echelonCode") not in ECHELON_CODES:
            problems.append("%s: echelonCode %r" % (rid, r.get("echelonCode")))
        fid = r.get("functionId", "")
        if fid and fid != "(none)" and not re.match(r"^[A-Z]{1,6}$", fid):
            problems.append("%s: functionId %r" % (rid, fid))
        if r.get("isAggregate") is not True:
            problems.append("%s: isAggregate must be true on the aggregate map" % rid)
        t8, nf = sm.parse_type(r.get("objectType", ""))
        if r.get("fidelity") != "AUTHORED_PENDING":
            if t8 is None or nf != 8 or t8[0] != 3 or t8[1] != sm.UNIT_KIND:
                problems.append("%s: objectType %r is not an 8-field kind-11 unit type" % (rid, r.get("objectType")))
        if r.get("fidelity") == "PROXY" and not r.get("proxyNote", "").strip():
            problems.append("%s: PROXY without a proxyNote" % rid)
        if t8 is not None and isinstance(nations, dict) and r.get("nation") in nations:
            if t8[3] != nations[r["nation"]] and "WRONG NATION" not in r.get("proxyNote", ""):
                problems.append("%s: DIS country %d is not %s (%d) and the note does not say WRONG NATION"
                                % (rid, t8[3], r["nation"], nations[r["nation"]]))
        if t8 is not None and t8[2] != 1:
            problems.append("%s: DIS domain %d is not land (1)" % (rid, t8[2]))
        for field in ("proxyNote", "templateName", "id"):
            if any(ord(c) > 126 for c in r.get(field, "")):
                problems.append("%s: non-ASCII in %s" % (rid, field))
    g.check(not problems, "schema: every row has the entity map's fields, types and enums (%d rows)" % len(rows),
            "; ".join(problems[:6]) + (" ..." if len(problems) > 6 else ""))


def gate_uniqueness(g, m):
    rows = m.get("rows") or []
    ids = collections.Counter(r.get("id") for r in rows)
    dup = [k for k, v in ids.items() if v > 1]
    g.check(not dup, "uniqueness: row ids", str(dup))
    keys = collections.Counter((r.get("functionId"), r.get("echelon"), r.get("nationRole"), r.get("nation"))
                               for r in rows)
    dupk = [k for k, v in keys.items() if v > 1]
    g.check(not dupk, "uniqueness: (functionId, echelon, nationRole, nation) keys", str(dupk))
    catch = collections.Counter((r["nationRole"], r["nation"]) for r in rows
                                if not r.get("functionId") and r.get("echelon") == "*")
    pairs = set((r.get("nationRole"), r.get("nation")) for r in rows)
    g.check(all(catch.get(p, 0) == 1 for p in pairs), "uniqueness: exactly ONE catch-all per (nationRole, nation)",
            str({p: catch.get(p, 0) for p in pairs}))
    # key (c) must not be ambiguous either: two rows of one function ID and nation sharing an
    # EchelonCode would make Lookup's answer depend on row order.
    kc = collections.Counter((r.get("functionId"), r.get("echelonCode"), r.get("nationRole"), r.get("nation"))
                             for r in rows if r.get("functionId"))
    dupc = [k for k, v in kc.items() if v > 1]
    g.check(not dupc, "uniqueness: (functionId, echelonCode, side) for key (c)", str(dupc))


def gate_resolution(g, m, chain, entity_chain):
    rows = m.get("rows") or []
    wrong, abstract, empty_container, bad_subs, trap, magx_bad = [], [], [], [], [], []
    for r in rows:
        if r.get("fidelity") == "AUTHORED_PENDING":
            continue
        t8, _ = sm.parse_type(r.get("objectType", ""))
        if t8 is None:
            continue
        t = chain.resolve(t8)
        if t is None or t.role == "BASE-ABSTRACT":
            abstract.append("%s %s -> %s" % (r["id"], r["objectType"], t.name if t else "nothing"))
            continue
        if r.get("templateName") not in (t.label, t.name):
            wrong.append("%s names %r, lands %r" % (r["id"], r.get("templateName"), t.label or t.name))
        if "role" in r and r["role"] != t.role:
            wrong.append("%s says role %s, the template is %s" % (r["id"], r["role"], t.role))
        if t.role == "CONTAINER":
            if not t.subs:
                empty_container.append("%s -> %s" % (r["id"], t.name))
            else:
                for st, _h in t.subs:
                    lt = chain.resolve(st)
                    if lt is None or lt.role in ("BASE-ABSTRACT", "OTHER(Generic_VR-Forces_Object.ope)"):
                        bad_subs.append("%s: sub %s lands %s" % (r["id"], sm.type_str(st), lt.name if lt else "nothing"))
        elif t.role not in ("UNIT",):
            wrong.append("%s lands a %s (%s), not a ground warfare-model unit" % (r["id"], t.role, t.name))
        et = entity_chain.resolve(t8)
        if et is not None and et.subs and all(s8[0] == 3 for s8, _h in et.subs):
            trap.append("%s %s: EntityLevel twin '%s' is a PURE higher unit (%d unit subs)"
                        % (r["id"], r["objectType"], et.name, len(et.subs)))
        if r.get("magx"):
            ms = [x for x in chain.magx if x["file"] == r["magx"]]
            if not ms or all(x["otype"] != t8 for x in ms):
                magx_bad.append("%s: magx %s does not name %s" % (r["id"], r["magx"], r["objectType"]))
    g.check(not abstract, "resolution: no row lands nothing or the base abstract (an EMPTY unit)", "; ".join(abstract))
    g.check(not wrong, "resolution: every row lands the template it names, as a ground warfare-model unit",
            "; ".join(wrong[:6]))
    g.check(not empty_container, "resolution: no row names an EMPTY container (its Move Along Route ends at once)",
            "; ".join(empty_container))
    g.check(not bad_subs, "resolution: every container row's subordinates resolve to real templates", "; ".join(bad_subs[:6]))
    g.check(not trap, "app trap: no row the app's EntityLevel-rooted resolver would EXPAND under AtOrder",
            "; ".join(trap[:6]))
    g.check(not magx_bad, "magx: every recorded .magx names the row's exact object type", "; ".join(magx_bad[:6]))


def gate_coverage(g, m, init_path, friendly="USA"):
    rows = m.get("rows") or []
    nations = m.get("nations") or {}
    units = parse_init(init_path)
    hostile_nations = sorted(set(r["nation"] for r in rows if r["nationRole"] == "hostile"))
    census = {}
    for opp in hostile_nations or [""]:
        g.check(check_nation_supported(rows, nations, "friendly", friendly) is None,
                "coverage: JC-2 friendly %s is a usable nation" % friendly)
        g.check(check_nation_supported(rows, nations, "hostile", opp) is None,
                "coverage: JC-2 hostile %s is a usable nation" % opp)
        failed, lines = [], []
        for u in units:
            row, key = plan_unit(rows, nations, u, friendly, opp)
            if row is None and not key.startswith("not-table"):
                failed.append(u["name"])
            elif row is not None and row["fidelity"] not in ("EXACT", "PROXY"):
                failed.append(u["name"] + " (" + row["fidelity"] + ")")
            lines.append((u, row, key))
        g.check(not failed, "coverage: all %d init units resolve to a usable row (friendly %s, hostile %s)"
                % (len(units), friendly, opp), "; ".join(failed[:6]))
        census[opp] = lines
    return units, census


def load_derived(path=None):
    """The deployed derived chain, or (None, reason)."""
    path = path or sm.DERIVED_AGGREGATE_SMS
    try:
        return sm.Chain(derived_sms=path), path
    except (IOError, OSError) as e:
        return None, "%s (%s)" % (path, e)


def gate_authored(g, m, chain, derived, entity_chain, design, derived_why=""):
    """The authoredRows gate (module note). Returns info lines (the wrong-SMS hazard per type)."""
    arows = m.get("authoredRows")
    info = []
    if arows is None:
        return info
    rows = m.get("rows") or []
    nations = m.get("nations") or {}
    problems = []
    if not isinstance(arows, list) or not arows:
        g.check(False, "authored: authoredRows is a non-empty list", repr(arows)[:80])
        return info
    for r in arows:
        rid = r.get("id", "?")
        for k, typ in AUTHORED_FIELDS.items():
            if k not in r or not isinstance(r[k], typ):
                problems.append("%s: field %s missing or not %s" % (rid, k, typ.__name__))
        for k, typ in OPTIONAL_AUTHORED_FIELDS.items():
            if k in r and not isinstance(r[k], typ):
                problems.append("%s: field %s not %s" % (rid, k, typ.__name__))
        extra = set(r) - set(AUTHORED_FIELDS) - set(OPTIONAL_AUTHORED_FIELDS)
        if extra:
            problems.append("%s: unknown field(s) %s" % (rid, sorted(extra)))
        if r.get("fidelity") != "AUTHORED":
            problems.append("%s: fidelity %r (authoredRows carry AUTHORED only)" % (rid, r.get("fidelity")))
        if r.get("role") != "UNIT" or r.get("isAggregate") is not True:
            problems.append("%s: an authored type is a warfare-model UNIT (role UNIT, isAggregate true)" % rid)
        if not r.get("proxyNote", "").strip():
            problems.append("%s: AUTHORED without a note" % rid)
        if r.get("echelonCode") not in ECHELON_CODES:
            problems.append("%s: echelonCode %r" % (rid, r.get("echelonCode")))
        t8, nf = sm.parse_type(r.get("objectType", ""))
        if t8 is None or nf != 8 or t8[0] != 3 or t8[1] != sm.UNIT_KIND or t8[2] != 1:
            problems.append("%s: objectType %r is not an 8-field kind-11 LAND unit type" % (rid, r.get("objectType")))
        elif r.get("nation") not in nations or t8[3] != nations[r["nation"]]:
            problems.append("%s: DIS country %s is not the nation %s" % (rid, t8[3], r.get("nation")))
        for field in ("proxyNote", "templateName", "id", "entityFile", "magx"):
            if any(ord(c) > 126 for c in r.get(field, "")):
                problems.append("%s: non-ASCII in %s" % (rid, field))
    g.check(not problems, "authored: every authoredRows row has its fields, types and enums (%d rows)" % len(arows),
            "; ".join(problems[:6]) + (" ..." if len(problems) > 6 else ""))
    # uniqueness, and no collision with a lookup row
    ids = collections.Counter([x.get("id") for x in rows] + [x.get("id") for x in arows])
    types = collections.Counter(x.get("objectType") for x in arows)
    row_types = set(x.get("objectType") for x in rows)
    bad = ([k for k, v in ids.items() if v > 1] + ["type %s twice" % k for k, v in types.items() if v > 1] +
           ["type %s is also a lookup row's" % x.get("objectType") for x in arows if x.get("objectType") in row_types])
    g.check(not bad, "authored: ids unique across rows and authoredRows; one row per authored type", str(bad[:6]))
    # one row per design type, same type / label / files
    link = []
    by_recipe = dict((x.get("recipeId"), x) for x in arows)
    dtypes = design.get("types", []) if design else []
    for t in dtypes:
        r = by_recipe.get(t["id"])
        if r is None:
            link.append("design type %s has no authoredRows row" % t["id"])
            continue
        want = dict(objectType="3:" + t["objectType"], templateName=t["label"], entityFile=t["label"] + ".entity",
                    magx=t["label"] + ".magx", modelSet=design["name"])
        for k, v in want.items():
            if r.get(k) != v:
                link.append("%s: %s %r, the design says %r" % (r.get("id"), k, r.get(k), v))
    extra = [x.get("id") for x in arows if x.get("recipeId") not in set(t["id"] for t in dtypes)]
    link += ["%s names no design type" % e for e in extra]
    g.check(not link, "authored: one row per design type (tools/sms/aggregate_authored_design.json), same type, "
                      "label, .entity and .magx", "; ".join(link[:6]))
    if derived is None:
        g.check(False, "authored: the derived set is DEPLOYED (the authored types exist only there)",
                "%s - run tools/sms/Deploy-C2SimAggregateSms.ps1" % derived_why)
        return info
    res, hazard, trap, magx_bad = [], [], [], []
    for r in arows:
        t8, _ = sm.parse_type(r.get("objectType", ""))
        if t8 is None:
            continue
        t = derived.resolve(t8)
        if t is None or t.role != "UNIT" or t.sms != derived.order[0][0] or r.get("templateName") not in (t.label, t.name) \
                or t.file != r.get("entityFile"):
            res.append("%s %s lands %s (%s, %s)" % (r.get("id"), r.get("objectType"), t.name if t else "nothing",
                                                    t.role if t else "-", t.sms if t else "-"))
        ms = [x for x in derived.magx if x["file"] == r.get("magx")]
        if not ms or any(x["otype"] != t8 or x["name"] != r.get("templateName") for x in ms):
            magx_bad.append("%s: %s" % (r.get("id"), r.get("magx")))
        v = chain.resolve(t8)
        if v is not None and r.get("templateName") in (v.label, v.name):
            hazard.append("%s: the VENDOR chain already lands %s - not an authored type" % (r.get("id"), v.name))
        info.append("  [info] wrong-SMS hazard: %-34s on the vendor SMS lands %s (%s)" % (
            r.get("templateName"), v.name if v else "nothing", v.role if v else "-"))
        et = entity_chain.resolve(t8)
        if et is not None and et.subs and all(s8[0] == 3 for s8, _h in et.subs):
            trap.append("%s: EntityLevel twin '%s' is a PURE higher unit" % (r.get("id"), et.name))
    g.check(not res, "authored: on the DEPLOYED derived chain every authored type lands its own warfare-model UNIT",
            "; ".join(res[:6]))
    g.check(not magx_bad, "authored: every authored .magx maps exactly its type to its element", "; ".join(magx_bad))
    g.check(not hazard, "authored: no authored type exists on the vendor chain (it needs the derived set)",
            "; ".join(hazard))
    g.check(not trap, "authored: no authored type the app's EntityLevel-rooted resolver would EXPAND", "; ".join(trap))
    moved = []
    for tm in chain.units():
        if -1 in tm.otype:
            continue
        a, b = chain.resolve(tm.otype), derived.resolve(tm.otype)
        if (a.name if a else None) != (b.name if b else None):
            moved.append("%s -> %s" % (tm.name, b.name if b else None))
    for x in rows:
        t8, _ = sm.parse_type(x.get("objectType", ""))
        if t8 is not None:
            a, b = chain.resolve(t8), derived.resolve(t8)
            if (a.name if a else None) != (b.name if b else None):
                moved.append("row %s -> %s" % (x.get("id"), b.name if b else None))
    # the US init-shell / container candidate space - every US ground type with a headquarters from
    # company to corps (11:1:225:5..10:0..34:1:0|1), what an init shell or a composition container asks for
    for t8 in CANDIDATE_TYPES:
        a, b = chain.resolve(t8), derived.resolve(t8)
        if (a.name if a else None) != (b.name if b else None):
            moved.append("candidate %s -> %s" % (":".join(str(v) for v in t8[1:]), b.name if b else None))
    g.check(not moved, "authored: the derived set only ADDS - every vendor template, every map row and every US "
                       "init-shell / container candidate type (11:1:225:5..10:0..34:1:0|1) resolves identically "
                       "on both chains", "; ".join(moved[:6]))
    return info


CANDIDATE_TYPES = [(3, 11, 1, 225, cat, sub, 1, extra)
                   for cat in range(5, 11) for sub in range(0, 35) for extra in (0, 1)]


def run_gates(m, chain, entity_chain, init_path=IRON_STORM_INIT, quiet=False, derived=None, design=None,
              derived_why=""):
    g = Gate(quiet=quiet)
    gate_schema(g, m)
    if g.bad:
        return g, None, None
    gate_uniqueness(g, m)
    gate_resolution(g, m, chain, entity_chain)
    units, census = gate_coverage(g, m, init_path)
    g.info = gate_authored(g, m, chain, derived, entity_chain, design, derived_why)
    return g, units, census


def census_markdown(units, census):
    out = []
    ech_counts = collections.Counter(u["echelon_code"] for u in units)
    out.append("Iron Storm init: %d units (%s); %d with coordinates after the superior cascade (the app "
               "creates only those)." % (len(units), ", ".join("%s %d" % kv for kv in ech_counts.most_common()),
                                           sum(1 for u in units if u["lat"] and u["lon"])))
    out.append("")
    for opp, lines in census.items():
        fid = collections.Counter(r["fidelity"] if r else "none" for _u, r, _k in lines)
        out.append("Hostile nation %s: %s." % (opp, ", ".join("%s %d" % kv for kv in sorted(fid.items()))))
    out.append("")
    first = list(census.values())[0]
    out.append("| unit | side | EchelonCode | SIDC | key | row | template (friendly USA / hostile %s) | fidelity | created |"
               % "/".join(census.keys()))
    out.append("|---|---|---|---|---|---|---|---|---|")
    for i, (u, row, key) in enumerate(first):
        if u["hostility"] == "HO" and len(census) > 1:
            alts = [census[o][i][1] for o in census]
            tmpl = " / ".join((a["templateName"] if a else "-") for a in alts)
            rid = " / ".join((a["id"] if a else "-") for a in alts)
        else:
            tmpl = row["templateName"] if row else "-"
            rid = row["id"] if row else "-"
        out.append("| %s | %s | %s | %s | %s | %s | %s | %s | %s |" % (
            u["name"][:48], "H" if u["hostility"] == "HO" else "F", u["echelon_code"], u["sidc"], key.split(":")[0],
            rid, tmpl, row["fidelity"] if row else "-",
            "yes" if (u["lat"] and u["lon"]) else "NO (no coordinates)"))
    return "\r\n".join(out) + "\r\n"


# ---------------------------------------------------------------------------------------------
# Self-test: the port against the app's known answers, then CLEAN / DIRTY controls
# ---------------------------------------------------------------------------------------------

def selftest(map_path=DEF_MAP, derived_path=None):
    g = Gate()
    print("--- 1. the key-logic port reproduces src/VrfC2SimApp/TypeMapSelfTest.cs on the ENTITY map ---")
    em = json.load(open(ENTITY_MAP, encoding="utf-8"))
    er = em["rows"]
    g.check(function_id_of("SFGPUCIZ---D---") == "UCIZ", "functionId of SFGPUCIZ---D--- is UCIZ")
    g.check(function_id_of("SFGP-------E---") == "(none)", "functionId of a blank field is (none)")
    g.check(echelon_char_of("SFGPUCIZ--EH---") == "H", "echelon char of SFGPUCIZ--EH--- is H")
    r, k = lookup(er, "UCA", "E", "COY", "friendly", "USA")
    g.check(k.startswith("b:") and r["templateName"] == "Tank Company (USA)", "(b) UCA/E/friendly-USA -> Tank Company (USA)")
    r, k = lookup(er, "UCA", "Z", "COY", "friendly", "USA")
    g.check(k.startswith("c:") and r["id"] == "F-UCA-E", "(c) UCA/echelon 'Z' falls to EchelonCode COY")
    r, k = lookup(er, "ZZZZ", "D", "PLT", "friendly", "USA")
    g.check(k.startswith("d:") and r["id"] == "F-GEN-D", "(d) unknown functionId at echelon D -> the echelon-only row")
    r, k = lookup(er, "ZZZZ", "Q", "NKN", "hostile", "RUS")
    g.check(k.startswith("e:") and r["id"] == "H-GEN-ANY", "(e) unknown functionId AND echelon -> the catch-all row")
    g.check(vrf_type_from_init_dis([11, 1, 225, 3, 4, 0, 0]) == "3:11:1:225:3:4:0:0", "(a) init DIS 11.1.225.3.4.0.0 -> 3:11:...")
    g.check(vrf_type_from_init_dis([0] * 7) is None, "(a) an all-zero SISOEntityType is no type at all")
    g.check(check_nation_supported(er, em["nations"], "hostile", "PRC") is not None
            and check_nation_supported(er, em["nations"], "hostile", "RUS") is None,
            "JC-2: PRC refused, RUS usable (TypeMapSelfTest's answers)")

    print("--- 2. the CLEAN control: the committed aggregate map passes every gate ---")
    chain = sm.Chain(top_sms="AggregateTacticalLevel")
    entity_chain = sm.Chain(top_sms="EntityLevel")
    derived, why = load_derived(derived_path)
    design = json.load(open(DESIGN, encoding="utf-8"))
    m = json.load(open(map_path, encoding="utf-8"))
    clean, _u, _c = run_gates(m, chain, entity_chain, quiet=True, derived=derived, design=design, derived_why=why)
    g.check(clean.bad == 0, "CLEAN: %s passes all gates" % os.path.basename(map_path), "; ".join(clean.failures))

    print("--- 3. DIRTY controls: each defect must FAIL its own gate ---")

    def dirty(name, mutate, expect, no_derived=False):
        d = copy.deepcopy(m)
        mutate(d)
        res, _u2, _c2 = run_gates(d, chain, entity_chain, quiet=True, derived=None if no_derived else derived,
                                  design=design, derived_why="(withheld by the control)")
        hit = any(expect in f for f in res.failures)
        g.check(hit, "DIRTY %s -> fails '%s' (%d gate(s) failed)" % (name, expect, res.bad),
                "failures: %s" % res.failures)

    def dup_key(d):
        r = copy.deepcopy(d["rows"][0])
        r["id"] = r["id"] + "-DUP"
        d["rows"].append(r)

    def magx_sourced(d):
        # The vendor's own .magx type for 'Rifle CO USMC PA' (11:1:225:5:3:1:75) lands the BASE abstract.
        d["rows"][0].update(objectType="3:11:1:225:5:3:1:75", magx="Rifle CO USMC PA.magx")

    def expand_trap(d):
        d["rows"][0].update(objectType="3:11:1:225:5:2:0:0", templateName="Tank CO (USA, M1A2)",
                            entityFile="Tank CO (USA, M1A2).entity", magx="")

    def empty_container(d):
        # What a map author would plausibly write for a USA mech-infantry BRIGADE: a concrete
        # Country-225 type. It lands the generic AggregateLevelBase container (country wildcard),
        # which has NO subordinates - Road to Kaunas populates such containers, this could not.
        d["rows"][0].update(objectType="3:11:1:225:8:4:1:0", templateName="BDE, Mech Infantry",
                            role="CONTAINER", entityFile="BDE, Mech Infantry.entity", magx="")

    def no_note(d):
        d["rows"][0]["proxyNote"] = ""

    def missing_field(d):
        del d["rows"][0]["nation"]

    def wrong_template(d):
        d["rows"][0]["templateName"] = "Stryker Cavalry SQDN (USA)"

    def drop_catch_all(d):
        d["rows"] = [r for r in d["rows"] if not (r["id"] == "H-GEN-ANY")]

    def uncovered(d):
        # remove every friendly fallback and the specific UCI rows: 48_IBCT etc. can no longer resolve
        d["rows"] = [r for r in d["rows"] if not (r["nationRole"] == "friendly"
                                                  and (r["functionId"] in ("", "UCI")))]

    dirty("duplicate key", dup_key, "uniqueness: (functionId, echelon")
    dirty("a .magx-sourced type that lands the base abstract", magx_sourced, "base abstract")
    dirty("Tank CO (USA, M1A2) - the AtOrder EXPAND trap", expand_trap, "app trap")
    dirty("an EMPTY generic container", empty_container, "EMPTY container")
    dirty("a PROXY with no note", no_note, "schema")
    dirty("a missing field", missing_field, "schema")
    dirty("a templateName the type does not land", wrong_template, "lands the template it names")
    dirty("no RUS catch-all", drop_catch_all, "catch-all")
    dirty("an Iron Storm unit with no row", uncovered, "coverage: all")

    # the authored rows (package C2)
    def authored_in_rows(d):
        r = copy.deepcopy(d["rows"][0])
        r.update(id="F-AUTHORED-LOOKUP", functionId="UCIX", fidelity="AUTHORED")
        d["rows"].append(r)

    def undeployed_type(d):
        d["authoredRows"][0]["objectType"] = "3:11:1:225:6:3:1:250"

    def authored_no_note(d):
        d["authoredRows"][0]["proxyNote"] = " "

    def vendor_posing(d):
        d["authoredRows"][0].update(objectType="3:11:1:225:5:4:0:0", templateName="Mech CO (USA, M2)")

    def design_type_dropped(d):
        d["authoredRows"].pop()

    def authored_twice(d):
        d["authoredRows"][1]["objectType"] = d["authoredRows"][0]["objectType"]

    if m.get("authoredRows"):
        dirty("an AUTHORED row among the lookup rows", authored_in_rows, "belongs in authoredRows")
        dirty("an authored type the deployed set does not publish", undeployed_type, "DEPLOYED derived chain")
        dirty("an authored row with no note", authored_no_note, "authored: every authoredRows row")
        dirty("a vendor catalogue type posing as authored", vendor_posing, "exists on the vendor chain")
        dirty("a design type with no authored row", design_type_dropped, "one row per design type")
        dirty("two authored rows with one type", authored_twice, "one row per authored type")
        dirty("the derived set not deployed", lambda d: None, "is DEPLOYED", no_derived=True)

        class Shadowing(object):
            # the derived chain with ONE candidate type answered by an authored template - what a loose
            # authored matchType (11:1:225:6:3:1:-1 instead of the exact type) would do to an init shell
            def __init__(self, inner, victim, shadow_name):
                self._inner, self._victim = inner, victim
                self._shadow = next(t for t in inner.units() if t.name == shadow_name)

            def __getattr__(self, k):
                return getattr(self._inner, k)

            def resolve(self, t8):
                return self._shadow if tuple(t8) == self._victim else self._inner.resolve(t8)

        victim = (3, 11, 1, 225, 6, 3, 1, 0)
        g.check(victim in CANDIDATE_TYPES and victim not in set(tuple(t.otype) for t in chain.units())
                and all(sm.parse_type(r.get("objectType", ""))[0] != victim for r in m["rows"]),
                "the shadowing control's victim 11:1:225:6:3:1:0 is a candidate only (no vendor template, no map row)")
        if derived is not None:
            res, _u2, _c2 = run_gates(copy.deepcopy(m), chain, entity_chain, quiet=True,
                                      derived=Shadowing(derived, victim, "Infantry BN (USA, IBCT)"), design=design,
                                      derived_why="(shadowed by the control)")
            hit = any("only ADDS" in f and "-- candidate 11:1:225:6:3:1:0 -> Infantry BN" in f for f in res.failures)
            g.check(hit, "DIRTY an authored template answering the candidate 11:1:225:6:3:1:0 -> fails 'only ADDS' "
                         "(%d gate(s) failed)" % res.bad, "failures: %s" % res.failures)
    else:
        g.check(False, "the committed map carries authoredRows (package C2)")
    print("TYPEMAP SELFTEST %s (%d problem(s))" % ("PASS" if g.bad == 0 else "FAIL", g.bad))
    return 0 if g.bad == 0 else 1


def main(argv=None):
    import argparse
    ap = argparse.ArgumentParser(description="Offline gate for the aggregate-level type map.")
    ap.add_argument("map", nargs="?", default=DEF_MAP)
    ap.add_argument("--init", default=IRON_STORM_INIT)
    ap.add_argument("--census-md", action="store_true", help="print the per-unit census as markdown")
    ap.add_argument("--derived-sms", default=None, metavar="SMS",
                    help="the deployed derived set the authoredRows live in (default %s)" % sm.DERIVED_AGGREGATE_SMS)
    ap.add_argument("--selftest", action="store_true")
    a = ap.parse_args(argv)
    if a.selftest:
        return selftest(a.map, a.derived_sms)
    print("=== aggregate type-map gate: %s ===" % a.map)
    m = json.load(open(a.map, encoding="utf-8"))
    chain = sm.Chain(top_sms="AggregateTacticalLevel")
    entity_chain = sm.Chain(top_sms="EntityLevel")
    derived, why = load_derived(a.derived_sms)
    design = json.load(open(DESIGN, encoding="utf-8"))
    g, units, census = run_gates(m, chain, entity_chain, a.init, derived=derived, design=design, derived_why=why)
    for line in getattr(g, "info", []) or []:
        print(line)
    if census and a.census_md:
        sys.stdout.write(census_markdown(units, census).replace("\r\n", "\n"))
    print("TYPEMAP GATE %s (%d problem(s))" % ("PASS" if g.bad == 0 else "FAIL", g.bad))
    return 0 if g.bad == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
