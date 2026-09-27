#!/usr/bin/env python
"""Survey the VR-Forces 5.2d AGGREGATE-LEVEL unit catalogue. ASCII only. Offline, read-only.

What it reads (never writes under C:\\MAK):
  * the simulation-model-set CHAIN rooted at AggregateTacticalLevel.sms
    (AggregateTacticalLevel -> AggregateLevelBase -> base, followed through the (include ...)
    lines exactly as src/VrfC2SimApp/ObjectTypeResolver.cs follows them), every vrfSim\\*.entity;
  * every unit type-mapping file (*.magx) under each model set's gui\\visuals\\Unit;
  * optionally a reference scenario (.scnx, a zip) - e.g. the vendor's Road to Kaunas - to show
    how the vendor itself populates a brigade scenario (containers vs warfare-model units).

What it produces:
  * one row per kind-11 (military hierarchy) template: name, object type, role (warfare-model
    UNIT vs CONTAINER), nation, echelon, function, subordinates, source .magx;
  * docs/experiments/AGGREGATE_CATALOGUE_2026-09-27.md (--out) and the full table as CSV (--csv).

THE .magx IS NOT THE SIM'S TEMPLATE SELECTOR. A .magx is a boost-serialised UnitTypeMap
(<first> object type -> <second> unit ELEMENT DEFINITION name) that lives beside the .leaf
element definitions (MIL-STD icon visualizers) under gui\\visuals\\Unit and is merged by the
vrfGui option --mergeUnitTypeMap (UG52 Table 10 p172). The SIMULATOR picks a template for a
requested object type by the documented best-match rule over the .entity files' matchType
(ObjectTypeResolver.cs, same rule, ported below). So every object type taken from a .magx is
also RESOLVED here, and a .magx entry whose type does not land the template it names is a
finding, not a row to trust.

Enumerations: Unit Category (field 4 of the 7-field type) and Unit Subcategory (field 5), UG52
appendix D.3.4 p1689-1690. 5.2d .entity files publish 7-field types; the 8-field form used by
the app prepends superType 3 for kind 11 (ObjectTypeResolver.ParseType).

Run with the repo's pinned interpreter (tests/NavGate.Tests.ps1: %LOCALAPPDATA%\\Programs\\
Python\\Python312\\python.exe):
    python tools/aggregate/survey_magx.py                       # summary to stdout
    python tools/aggregate/survey_magx.py --out docs/experiments/AGGREGATE_CATALOGUE_2026-09-27.md \\
        --csv docs/experiments/AGGREGATE_CATALOGUE_2026-09-27.csv \\
        --reference-scnx "C:\\MAK\\vrforces5.2d\\userData\\scenarios\\Sample\\VR-TheWorld_Online\\
AggregateTacticalLevel\\RoadToKaunas\\RoadToKaunas\\RoadToKaunas.scnx"
    python tools/aggregate/survey_magx.py --selftest            # resolver + parser controls
"""
import collections
import csv
import io
import os
import re
import sys
import xml.etree.ElementTree as ET
import zipfile

DEFAULT_VRF_HOME = os.environ.get("VRF_HOME") or r"C:\MAK\vrforces5.2d"
TOP_SMS = "AggregateTacticalLevel"
UNIT_KIND = 11
# The C2SIM derived aggregate model set (tools/sms/Deploy-C2SimAggregateSms.ps1): AggregateTacticalLevel
# plus the AUTHORED US unit types. Deployed at run time under C:\C2SIM\vrf-sms, never in the repo.
DERIVED_AGGREGATE_SMS = (os.environ.get("C2SIM_AGGREGATE_SMS")
                         or r"C:\C2SIM\vrf-sms\C2SIM_AggregateTacticalLevel.sms")

# UG52 appendix D.3.4 p1689-1690 ("Unit Category"; Force, Team, Squad, Section are VR-Forces
# extensions). -1 is a matchType wildcard, not an enumeration.
UNIT_CATEGORY = {
    0: "Other", 1: "IndividualVehicle", 2: "Element", 3: "Platoon", 4: "Battery",
    5: "Company", 6: "Battalion", 7: "Regiment", 8: "Brigade", 9: "Division", 10: "Corps",
    11: "Force", 12: "Team", 13: "Squad", 14: "Section",
}
ECHELON_ABBR = {
    0: "OTHER", 1: "VEH", 2: "ELEM", 3: "PLT", 4: "BTY", 5: "CO", 6: "BN", 7: "RGT", 8: "BDE",
    9: "DIV", 10: "CORPS", 11: "FORCE", 12: "TEAM", 13: "SQD", 14: "SEC", -1: "(any)",
}
# UG52 D.3.4 p1690 "Unit Subcategory" (the Echelon Type / branch). Values above 24 are MAK
# extensions read off the .entity files (docs/UNIT_TYPE_MAPPING_FIDELITY_2026-09-02.md sec 2.4);
# they are labelled "(ext N)" rather than guessed.
UNIT_SUBCATEGORY = {
    0: "Other", 1: "CavalryTroop", 2: "Armor", 3: "Infantry", 4: "MechanizedInfantry",
    5: "Cavalry", 6: "ArmoredCavalry", 7: "Artillery", 8: "SelfPropelledArtillery",
    9: "CloseAirSupport", 10: "Engineer", 11: "AirDefenseArtillery", 12: "AntiTank",
    13: "ArmyAviationFixedWing", 14: "ArmyAviationRotaryWing", 15: "ArmyAttackHelicopter",
    16: "AirCavalry", 17: "ArmorHeavyTaskForce", 18: "MotorizedRifle",
    19: "MechanizedHeavyTaskForce", 20: "CommandPost", 21: "CEWI", 22: "TankOnly",
    23: "ForceFriendly", 24: "ForceOpposing", -1: "(any)",
}
# Platform -> role. UG52 72.2.1 p1419: warfare-model units "do not have the configuration
# options for specifying subordinates. Therefore, AggregateLevelBase.sms has a platform,
# Aggregate Container, for use when aggregating simulation objects" - PseudoAggregate.ope
# (Table 80 p1688 "Unit Container"). A CONTAINER runs no warfare model of its own; its move
# tasks are scripts that task its subordinates (AggregateLevelBase\scripts\PA_Move_Along_Route.lua).
PLATFORM_ROLE = {
    "AggregateLevelAggregate.ope": "UNIT",
    "PseudoAggregate.ope": "CONTAINER",
    "AircraftAggregate.ope": "AIR-UNIT",
    "AirPseudoAggregate.ope": "AIR-CONTAINER",
    "Aggregate.ope": "BASE-ABSTRACT",
    "Top_Level_Entity.ope": "TOP-LEVEL",
}
# DIS country -> the nation tag used in the vendor's own template names. VERIFIED ON DISK by
# the survey itself (country_evidence() prints the gui-deployable-countries seen per code), not
# taken from SISO-REF-010: 260 is Russia in this catalogue (137 templates deployable "RU"), NOT
# the 222 the ENTITY-level map uses (only 2 aggregate templates carry 222).
NATION_TAG = {
    225: "USA", 260: "RUS", 222: "RUS222", 246: "BLR", 175: "POL", 255: "LTU", 78: "DEU",
    224: "GBR", 45: "CHN", 180: "ROU", 71: "FRA", 153: "NLD", 249: "HRV", 0: "GENERIC0",
    -1: "ANY",
}


# ---------------------------------------------------------------------------------------------
# Object types (the ObjectTypeResolver.cs rules, ported)
# ---------------------------------------------------------------------------------------------

def parse_type(s):
    """'11:1:225:5:2:0:0' or '3:11:1:225:5:2:0:0' -> (8-tuple, nfields) or (None, 0)."""
    if not s:
        return None, 0
    parts = [p.strip() for p in s.split(":")]
    if len(parts) not in (7, 8):
        return None, 0
    try:
        v = [int(p) for p in parts]
    except ValueError:
        return None, 0
    if len(v) == 7:
        v = [3 if v[0] == UNIT_KIND else 1] + v
    return tuple(v), len(parts)


def parse_match(s, expected_fields):
    """matchType -> 8 (lo, hi) pairs; (-1, -1) = wildcard. None when absent/malformed."""
    if not s:
        return None
    parts = [p.strip() for p in s.split(":")]
    if len(parts) != expected_fields:
        return None
    out = []
    for p in parts:
        dash = p.find("-", 1)
        try:
            if dash > 0:
                out.append((int(p[:dash]), int(p[dash + 1:])))
            else:
                x = int(p)
                out.append((-1, -1) if x == -1 else (x, x))
        except ValueError:
            return None
    if len(out) == 7:
        kind = out[0]
        if kind == (-1, -1):
            sup = (-1, -1)
        elif kind == (UNIT_KIND, UNIT_KIND):
            sup = (3, 3)
        elif kind[0] <= UNIT_KIND <= kind[1]:
            sup = (-1, -1)
        else:
            sup = (1, 1)
        out = [sup] + out
    return out


def type_str(t8, seven=True):
    if t8 is None:
        return ""
    return ":".join(str(x) for x in (t8[1:] if seven else t8))


def _accepts(f, v):
    return f == (-1, -1) or f[0] <= v <= f[1]


def _score(m):
    lead = 0
    while lead < 8 and m[lead] != (-1, -1):
        lead += 1
    return lead, sum(1 for f in m if f != (-1, -1))


class Template(object):
    __slots__ = ("path", "file", "sms", "otype", "match", "platform", "role", "label",
                 "echelon_level", "categories", "countries", "short_name", "can_create",
                 "subs", "order")

    def __init__(self, **kw):
        for k in self.__slots__:
            setattr(self, k, kw.get(k))

    @property
    def name(self):
        return os.path.splitext(self.file)[0]


class Chain(object):
    """The installed model-set chain + the vendor's best-match rule (ObjectTypeResolver.cs).

    derived_sms: the ABSOLUTE path of a derived .sms OUTSIDE the vendor tree (the C2SIM derived
    sets under C:\\C2SIM\\vrf-sms, tools/sms). It heads the chain - an including SMS has the higher
    priority (UG52 68.3.3 p1312), so its templates win a best-match tie - its model-set-directory
    is resolved against the .sms file's own directory (the layout the deployed derived sets use:
    NAME.sms beside NAME\\), and its (include ...) lines are followed into the vendor tree by file
    name exactly as for a vendor SMS. top_sms is then ignored."""

    def __init__(self, vrf_home=DEFAULT_VRF_HOME, top_sms=TOP_SMS, derived_sms=None):
        self.vrf_home = vrf_home
        self.sets_dir = os.path.join(vrf_home, "data", "simulationModelSets")
        self.order = []          # [(sms name, model-set-directory)] - an absolute directory for a derived SMS
        self.derived_sms = derived_sms
        if derived_sms:
            self._follow_derived(derived_sms)
        else:
            self._follow(top_sms, set())
        self.templates = []
        self.unreadable = []
        for sms, mset in self.order:
            d = os.path.join(self.sets_dir, mset, "vrfSim")
            if not os.path.isdir(d):
                continue
            for f in sorted(os.listdir(d)):
                if f.lower().endswith(".entity"):
                    self._read_entity(os.path.join(d, f), sms)
        self.magx = self._read_all_magx()

    def _follow_derived(self, path):
        if not os.path.isfile(path):
            raise IOError("derived SMS not found: %s (deploy it: tools/sms/Deploy-C2SimAggregateSms.ps1)" % path)
        text = open(path, "r", encoding="utf-8", errors="replace").read()
        name = os.path.splitext(os.path.basename(path))[0]
        m = re.search(r'\(model-set-directory\s+"([^"]*)"\)', text)
        mset = m.group(1) if m and m.group(1) else name
        self.order.append((name, os.path.join(os.path.dirname(os.path.abspath(path)), mset)))
        seen = set([name.lower()])
        for inc in re.findall(r'\(include\s+"([^"]*)"\)', text):
            base = os.path.splitext(os.path.basename(inc.replace("\\", "/")))[0]
            if base:
                self._follow(base, seen)

    def _follow(self, sms, seen):
        path = os.path.join(self.sets_dir, sms + ".sms")
        if not os.path.isfile(path) or sms.lower() in seen:
            return
        seen.add(sms.lower())
        text = open(path, "r", encoding="utf-8", errors="replace").read()
        m = re.search(r'\(model-set-directory\s+"([^"]*)"\)', text)
        self.order.append((sms, m.group(1) if m and m.group(1) else sms))
        for inc in re.findall(r'\(include\s+"([^"]*)"\)', text):
            name = os.path.splitext(os.path.basename(inc.replace("\\", "/")))[0]
            if name:
                self._follow(name, seen)

    def _read_entity(self, path, sms):
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError:
            self.unreadable.append(path)
            return
        for so in root.iter("simObject"):
            otype, nf = parse_type(so.get("objectType"))
            if otype is None:
                continue
            match = parse_match(so.get("matchType"), nf) or [(v, v) for v in otype]

            def sval(name):
                e = so.find("string[@paramName='%s']" % name)
                return (e.text or "").strip() if e is not None else ""

            def bval(name):
                e = so.find("bool[@paramName='%s']" % name)
                return (e.text or "").strip() if e is not None else ""

            subs = []
            for s in so.iter("subordinate"):
                st, _ = parse_type(s.get("objectType"))
                if st is not None:
                    subs.append((st, (s.get("functionHandle") or "").strip()))
            platform = os.path.basename((so.get("platform") or "").replace("\\", "/"))
            self.templates.append(Template(
                path=path, file=os.path.basename(path), sms=sms, otype=otype, match=match,
                platform=platform, role=PLATFORM_ROLE.get(platform, "OTHER(%s)" % platform),
                label=sval("gui-label"), echelon_level=sval("echelon-level"),
                categories=sval("gui-categories"), countries=sval("gui-deployable-countries"),
                short_name=sval("short-name"), can_create=bval("gui-can-create"), subs=subs,
                order=len(self.templates)))

    def _read_all_magx(self):
        out = []
        for sms, mset in self.order:
            d = os.path.join(self.sets_dir, mset, "gui", "visuals", "Unit")
            if not os.path.isdir(d):
                continue
            for f in sorted(os.listdir(d)):
                if not f.lower().endswith(".magx"):
                    continue
                p = os.path.join(d, f)
                try:
                    root = ET.parse(p).getroot()
                except ET.ParseError:
                    out.append(dict(file=f, sms=sms, otype=None, name="(unparseable)"))
                    continue
                for item in root.iter("item"):
                    first, second = item.find("first"), item.find("second")
                    if first is None or second is None:
                        continue
                    t, _ = parse_type((first.text or "").strip())
                    out.append(dict(file=f, sms=sms, otype=t, name=(second.text or "").strip()))
        return out

    # ---- the best-match rule -------------------------------------------------------------
    def resolve_all(self, q8):
        cands = [t for t in self.templates if all(_accepts(t.match[i], q8[i]) for i in range(8))]
        if not cands:
            return []
        best = max(_score(t.match) for t in cands)
        return [t for t in cands if _score(t.match) == best]

    def resolve(self, q8):
        """The winning template (ties break on chain order), or None."""
        c = self.resolve_all(q8)
        return c[0] if c else None

    def units(self):
        return [t for t in self.templates if t.otype[1] == UNIT_KIND]


# ---------------------------------------------------------------------------------------------
# Row derivation
# ---------------------------------------------------------------------------------------------

def nation_of(t):
    c = t.otype[3]
    return NATION_TAG.get(c, "C%d" % c)


def echelon_of(t):
    cat = t.otype[4]
    return ECHELON_ABBR.get(cat, "cat%d" % cat)


def branch_of(t):
    sub = t.otype[5]
    return UNIT_SUBCATEGORY.get(sub, "(ext %d)" % sub)


def is_group_name(t):
    return "group" in (t.label or t.name).lower()


def describe_subs(chain, t, limit=None):
    """'HQ: <name>; SAM x3: <name>' - each subordinate RESOLVED by the same best-match rule."""
    if not t.subs:
        return ""
    counts = collections.OrderedDict()
    for st, handle in t.subs:
        landed = chain.resolve(st)
        key = (handle or "-", landed.name if landed else "UNRESOLVED " + type_str(st))
        counts[key] = counts.get(key, 0) + 1
    parts = []
    for (handle, name), n in counts.items():
        parts.append("%s%s: %s" % (handle, (" x%d" % n) if n > 1 else "", name))
    if limit and len(parts) > limit:
        parts = parts[:limit] + ["... (%d more)" % (len(parts) - limit)]
    return "; ".join(parts)


def magx_for(chain, t):
    """The .magx entries naming EXACTLY this template's object type."""
    return [m for m in chain.magx if m["otype"] == t.otype]


def build_rows(chain):
    rows = []
    for t in chain.units():
        mx = magx_for(chain, t)
        landed = chain.resolve(t.otype)
        rows.append(collections.OrderedDict([
            ("name", t.label or t.name),
            ("entityFile", t.file),
            ("sms", t.sms),
            ("objectType", type_str(t.otype)),
            ("objectType8", type_str(t.otype, seven=False)),
            ("role", t.role),
            ("nation", nation_of(t)),
            ("disCountry", t.otype[3]),
            ("deployable", t.countries.replace('"', "")),
            ("echelon", echelon_of(t)),
            ("echelonLevelParam", t.echelon_level),
            ("branch", branch_of(t)),
            ("specific", t.otype[6]),
            ("extra", t.otype[7]),
            ("guiCategories", t.categories.replace('"', "")),
            ("shortName", t.short_name),
            ("guiCanCreate", t.can_create),
            ("nSubordinates", len(t.subs)),
            ("subordinates", describe_subs(chain, t)),
            ("magx", "; ".join("%s -> %s" % (m["file"], m["name"]) for m in mx)),
            ("selfResolves", "yes" if landed is t else ("NO -> %s" % (landed.name if landed else "nothing"))),
        ]))
    return rows


def country_evidence(chain):
    ev = collections.defaultdict(collections.Counter)
    for t in chain.units():
        ev[t.otype[3]][t.countries.replace('"', "") or "(none)"] += 1
    return ev


def magx_crosscheck(chain):
    """Every .magx entry against the .entity chain."""
    exact, renamed, landed_other, unresolved = [], [], [], []
    for m in chain.magx:
        if m["otype"] is None:
            unresolved.append((m, None))
            continue
        same = [t for t in chain.templates if t.otype == m["otype"]]
        if same:
            if any((t.label or t.name) == m["name"] or t.name == m["name"] for t in same):
                exact.append((m, same[0]))
            else:
                renamed.append((m, same[0]))
        else:
            landed = chain.resolve(m["otype"])
            (landed_other if landed else unresolved).append((m, landed))
    no_magx = [t for t in chain.units() if not magx_for(chain, t)]
    return exact, renamed, landed_other, unresolved, no_magx


# ---------------------------------------------------------------------------------------------
# Reference scenario (.scnx): how the vendor populates a brigade scenario
# ---------------------------------------------------------------------------------------------

def _blocks(text):
    """(start, end) of every top-level (local-vrf-object ...) block, quote-aware."""
    for m in re.finditer(r"\(local-vrf-object", text):
        depth, i, n, instr = 0, m.start(), len(text), False
        while i < n:
            c = text[i]
            if instr:
                if c == '"':
                    instr = False
            elif c == '"':
                instr = True
            elif c == "(":
                depth += 1
            elif c == ")":
                depth -= 1
                if depth == 0:
                    yield m.start(), i + 1
                    break
            i += 1


def read_reference_scnx(path):
    with zipfile.ZipFile(path) as z:
        names = z.namelist()
        scn = next(n for n in names if n.lower().endswith(".scn"))
        oob = next(n for n in names if n.lower().endswith(".oob"))
        scn_text = z.read(scn).decode("utf-8", "replace")
        oob_text = z.read(oob).decode("utf-8", "replace")
    objs = []
    for s, e in _blocks(oob_text):
        b = oob_text[s:e]
        ot = re.search(r"\(object-type\s+(-?\d+(?:\s+-?\d+){6})\s*\)", b)
        if not ot:
            continue
        t7 = tuple(int(x) for x in ot.group(1).split())
        if t7[0] != UNIT_KIND:
            continue
        mk = re.search(r'\(marking-text\s+"([^"]*)"\)', b)
        uu = re.search(r'\(uuid\s+"(VRF_UUID:[^"]+)"\)', b)
        pn = re.findall(r'\(parent-name\s+"([^"]*)"\)', b)
        st = re.findall(r"\(aggregate-state\s+(\w+)\)", b)
        objs.append(dict(marking=mk.group(1) if mk else "?", t8=(3,) + t7,
                         uuid=uu.group(1) if uu else "", parent=pn[-1] if pn else "",
                         state=st[-1] if st else ""))
    settings = {}
    for key in ("Terrain-Database", "Simulation-Model-Set-Files", "frame-mode", "frame-time",
                "time-multiplier", "exercise-start-time", "ScenarioExtentInformation",
                "Set_Scenario_Start_Time_At_Local_On_First_Object_Creation", "UseDayNightModel",
                "auto-reorganize", "run-duration-time"):
        m = re.search(r"\(" + re.escape(key) + r'\s+"?([^")]*)"?\)', scn_text)
        settings[key] = m.group(1) if m else "(absent)"
    return objs, settings


def reference_summary(chain, path):
    objs, settings = read_reference_scnx(path)
    by_uuid = {o["uuid"]: o for o in objs}
    kids = collections.defaultdict(list)
    for o in objs:
        kids[o["parent"]].append(o)
        landed = chain.resolve(o["t8"])
        o["landed"] = landed
        o["role"] = landed.role if landed else "UNRESOLVED"
    role_state = collections.Counter((o["role"], o["state"]) for o in objs)
    roots = [o for o in objs if o["parent"] not in by_uuid]

    def depth(o, seen=()):
        p = by_uuid.get(o["parent"])
        return 0 if p is None or o["uuid"] in seen else 1 + depth(p, seen + (o["uuid"],))

    containers = [o for o in objs if o["role"] == "CONTAINER"]
    return dict(objs=objs, settings=settings, role_state=role_state, roots=roots,
                kids=kids, depth=depth, containers=containers)


# ---------------------------------------------------------------------------------------------
# Output
# ---------------------------------------------------------------------------------------------

def _md_table(headers, rows):
    out = ["| " + " | ".join(headers) + " |", "|" + "|".join("---" for _ in headers) + "|"]
    for r in rows:
        out.append("| " + " | ".join(str(c).replace("|", "/") for c in r) + " |")
    return out


GROUND_ROLES = ("UNIT", "CONTAINER")
ECH_ORDER = ["CORPS", "DIV", "BDE", "RGT", "BN", "BTY", "CO", "PLT", "SEC", "TEAM", "SQD",
             "ELEM", "VEH", "OTHER", "(any)"]


def render_markdown(chain, rows, ref=None, vrf_home=DEFAULT_VRF_HOME):
    L = []
    units = chain.units()
    L.append("# AGGREGATE CATALOGUE - VR-Forces 5.2d AggregateTacticalLevel (2026-09-27)")
    L.append("")
    L.append("GENERATED by `tools/aggregate/survey_magx.py` (do not hand-edit; re-run it). Offline,")
    L.append("read-only over `%s`. Full table: the `.csv` beside this file." % vrf_home)
    L.append("Chain followed (include lines, top first): %s." % " -> ".join(
        "%s.sms" % s for s, _ in chain.order))
    L.append("")
    L.append("## 1. What a .magx is, and why the object types are RESOLVED here")
    L.append("")
    L.append("- A `.magx` is a boost-serialised `UnitTypeMap`: `<first>` object type -> `<second>` unit ELEMENT")
    L.append("  definition. It sits beside the `.leaf` element definitions (MIL-STD icon visualizers) under")
    L.append("  `gui\\visuals\\Unit` and is merged by the vrfGui option `--mergeUnitTypeMap` (UG52 Table 10 p172).")
    L.append("- The SIMULATOR picks the template for a requested type by the best-match rule over the `.entity`")
    L.append("  matchType fields (ported from `src/VrfC2SimApp/ObjectTypeResolver.cs`). Every row's `selfResolves`")
    L.append("  column is that rule applied to the row's own type; the map validator applies it to every map row.")
    L.append("- ROLE comes from the platform file. UNIT = `AggregateLevelAggregate.ope`, one simulated object with")
    L.append("  combat power, health and a footprint (UG52 27.1 p528). CONTAINER = `PseudoAggregate.ope`, the")
    L.append("  \"Aggregate Container\" (UG52 72.2.1 p1419; Table 80 p1688 \"Unit Container\"): no warfare model")
    L.append("  and no movement system of its own; its Move Along Route is `PA_Move_Along_Route.lua`, which tasks")
    L.append("  each subordinate and ends when all have finished (`tick()`: `allSubordinatesComplete()` ->")
    L.append("  `endTask(true)`) - so an EMPTY container's move completes at once and nothing moves.")
    L.append("")
    L.append("## 2. Census")
    L.append("")
    rc = collections.Counter(t.role for t in units)
    L.append("Kind-11 templates in the chain: %d (%s). `.magx` entries: %d. Unreadable .entity files: %d."
             % (len(units), ", ".join("%s %d" % kv for kv in sorted(rc.items())), len(chain.magx),
                len(chain.unreadable)))
    L.append("")
    L.append("### 2.1 Unit and container templates (air excluded) by nation (DIS country) and echelon (Unit Category)")
    L.append("")
    ground = [t for t in units if t.role in GROUND_ROLES]
    nat = collections.Counter(nation_of(t) for t in ground)
    ech_present = [e for e in ECH_ORDER if any(echelon_of(t) == e for t in ground)]
    ech_present += sorted(set(echelon_of(t) for t in ground) - set(ech_present))
    hdr = ["nation (code)", "total", "units", "containers"] + ech_present
    trows = []
    for n, _cnt in sorted(nat.items(), key=lambda kv: (-kv[1], kv[0])):
        ts = [t for t in ground if nation_of(t) == n]
        code = ts[0].otype[3]
        trows.append(["%s (%d)" % (n, code), len(ts), sum(1 for t in ts if t.role == "UNIT"),
                      sum(1 for t in ts if t.role == "CONTAINER")] +
                     [sum(1 for t in ts if echelon_of(t) == e) or "" for e in ech_present])
    L.extend(_md_table(hdr, trows))
    L.append("")
    L.append("Air templates (AIR-UNIT / AIR-CONTAINER) are counted in the total above and left out of this")
    L.append("table: %d." % sum(1 for t in units if t.role.startswith("AIR")))
    L.append("")
    L.append("### 2.2 DIS country code evidence (gui-deployable-countries seen per code)")
    L.append("")
    ev = country_evidence(chain)
    L.extend(_md_table(["code", "tag used here", "deployable-countries (count)"],
                       [[c, NATION_TAG.get(c, "C%d" % c),
                         ", ".join("%s x%d" % kv for kv in ev[c].most_common())]
                        for c in sorted(ev)]))
    L.append("")
    L.append("### 2.3 The hostile side")
    L.append("")
    for tag in ("RUS", "BLR", "RUS222"):
        ts = [t for t in ground if nation_of(t) == tag]
        e = collections.Counter(echelon_of(t) for t in ts)
        L.append("- %s: %d unit/container templates (%d warfare-model units, %d containers); by echelon %s."
                 % (tag, len(ts), sum(1 for t in ts if t.role == "UNIT"),
                    sum(1 for t in ts if t.role == "CONTAINER"),
                    ", ".join("%s %d" % (k, e[k]) for k in ECH_ORDER if e.get(k))))
    L.append("")
    L.append("### 2.4 Land warfare-model UNITS only (the rows a type map can name for a mover), by nation and echelon")
    L.append("")
    land_units = [t for t in units if t.role == "UNIT" and t.otype[2] == 1]
    lu_ech = [e for e in ECH_ORDER if any(echelon_of(t) == e for t in land_units)]
    lu_ech += sorted(set(echelon_of(t) for t in land_units) - set(lu_ech))
    lu_rows = []
    for n, cnt in sorted(collections.Counter(nation_of(t) for t in land_units).items(), key=lambda kv: (-kv[1], kv[0])):
        ts = [t for t in land_units if nation_of(t) == n]
        lu_rows.append([n, len(ts)] + [sum(1 for t in ts if echelon_of(t) == e) or "" for e in lu_ech])
    L.extend(_md_table(["nation", "units"] + lu_ech, lu_rows))
    L.append("")
    L.append("## 3. The BDE / DIV / CORPS rows and the \"Group\" rows, in full")
    L.append("")
    pairs = [(r, t) for r, t in zip(rows, units)
             if echelon_of(t) in ("BDE", "DIV", "CORPS") or is_group_name(t)]
    big = [r for r, t in pairs if t.otype[2] == 1]
    other = [r for r, t in pairs if t.otype[2] != 1]
    hi = [r for r in big if r["echelon"] in ("BDE", "DIV", "CORPS")]
    hi_units = [r for r in hi if r["role"] == "UNIT"]
    hi_empty = [r for r in hi if r["role"] == "CONTAINER" and r["nSubordinates"] == 0]
    hi_empty_wild = [r for r in hi_empty if r["disCountry"] == -1]
    L.append("Land-domain templates (DIS domain 1). At BDE/DIV/CORPS: %d template(s), of which %d warfare-model "
             "UNIT(s) and %d CONTAINER(s) with NO subordinates (%d of them generic nodes with country wildcard -1); "
             "the rest are containers with their subordinates preconfigured. The \"Group\" rows are "
             "battalion-level." % (len(hi), len(hi_units), len(hi_empty), len(hi_empty_wild)))
    L.append("")
    big.sort(key=lambda r: (ECH_ORDER.index(r["echelon"]) if r["echelon"] in ECH_ORDER else 99,
                            r["nation"], r["name"]))
    L.extend(_md_table(["name", "objectType", "role", "nation", "echelon", "branch", "subs", "subordinates (resolved)",
                        "source .magx", "canCreate"],
                       [[r["name"], r["objectType"], r["role"], r["nation"], r["echelon"], r["branch"],
                         r["nSubordinates"], r["subordinates"] or "-", r["magx"] or "(none)",
                         r["guiCanCreate"]] for r in big]))
    L.append("")
    L.append("Non-land templates carrying the same Unit Category codes (air aggregates reuse category 9, naval task")
    L.append("forces 8/9) - listed for completeness, not candidates for a ground unit: " +
             "; ".join("%s (%s, %s, %s)" % (r["name"], r["objectType"], r["role"], r["nation"])
                       for r in sorted(other, key=lambda r: r["name"])) + ".")
    L.append("")
    L.append("## 4. .magx against the .entity chain")
    L.append("")
    exact, renamed, landed_other, unresolved, no_magx = magx_crosscheck(chain)
    L.append("- %d `.magx` entries name a template whose own object type is the entry's and whose label/file"
             " name is the entry's name." % len(exact))
    L.append("- %d entries carry an object type a template publishes, under a DIFFERENT name (the .leaf"
             " element name differs from the .entity label - cosmetic)." % len(renamed))
    L.append("- %d entries carry an object type NO template publishes; the best-match rule lands another"
             " template for them:" % len(landed_other))
    for m, t in landed_other[:40]:
        L.append("  - `%s` %s -> lands `%s` (%s)" % (m["file"], type_str(m["otype"]),
                                                      t.name if t else "nothing", t.role if t else "-"))
    if len(landed_other) > 40:
        L.append("  - ... %d more (see --csv)" % (len(landed_other) - 40))
    L.append("- %d entries resolve to nothing at all." % len(unresolved))
    L.append("- %d kind-11 templates have NO `.magx` naming their exact type (mostly the generic"
             " AggregateLevelBase containers and air units)." % len(no_magx))
    if ref:
        L.append("")
        L.append("## 5. How the vendor populates a brigade scenario (reference: %s)" % os.path.basename(ref["path"]))
        L.append("")
        objs = ref["objs"]
        L.append("Kind-11 objects in the .oob: %d. Role (by the best-match rule) x saved aggregate-state:"
                 % len(objs))
        L.append("")
        L.extend(_md_table(["role", "aggregate-state", "count"],
                           [[k[0], k[1] or "(none)", v] for k, v in sorted(ref["role_state"].items())]))
        L.append("")
        L.append("Containers and what they hold (children by role):")
        L.append("")
        crow = []
        for o in sorted(ref["containers"], key=lambda o: (ref["depth"](o), o["marking"])):
            kids = ref["kids"].get(o["uuid"], [])
            kr = collections.Counter(k["role"] for k in kids)
            crow.append([o["marking"], type_str(o["t8"]), o["landed"].name if o["landed"] else "-",
                         ref["depth"](o), len(kids), ", ".join("%s %d" % kv for kv in sorted(kr.items())) or "EMPTY"])
        L.extend(_md_table(["container", "objectType", "lands template", "depth", "children", "children by role"], crow))
        L.append("")
        L.append("Scenario settings (.scn): " + "; ".join("%s = %s" % kv for kv in ref["settings"].items()))
    L.append("")
    return "\r\n".join(L) + "\r\n"


def write_csv(rows, path):
    buf = io.StringIO()
    w = csv.writer(buf, lineterminator="\r\n")
    w.writerow(list(rows[0].keys()))
    for r in rows:
        w.writerow([r[k] for k in r])
    data = buf.getvalue()
    _assert_ascii(data, path)
    with open(path, "w", encoding="utf-8", newline="") as fh:
        fh.write(data)


def _assert_ascii(text, what):
    bad = [(i, ord(c)) for i, c in enumerate(text) if ord(c) > 126 or (ord(c) < 32 and c not in "\r\n\t")]
    if bad:
        raise SystemExit("non-ASCII output for %s at %s - fix the source, never ship it" % (what, bad[:5]))


# ---------------------------------------------------------------------------------------------
# Self-test (offline; needs the vendor tree)
# ---------------------------------------------------------------------------------------------

def selftest(vrf_home=DEFAULT_VRF_HOME):
    bad = 0

    def check(ok, what, detail=""):
        nonlocal bad
        print("  [%s] %s%s" % ("PASS" if ok else "FAIL", what, (" -- " + detail) if detail and not ok else ""))
        if not ok:
            bad += 1

    print("--- parser controls ---")
    check(parse_type("11:1:225:5:2:0:0") == ((3, 11, 1, 225, 5, 2, 0, 0), 7), "7-field unit type gets superType 3")
    check(parse_type("1:1:1:225:1:1:3:0") == ((1, 1, 1, 225, 1, 1, 3, 0), 8), "8-field type kept as is")
    check(parse_type("1:2:3") == (None, 0), "a 3-field type is refused")
    m = parse_match("11:1:-1:9:4:1:-1", 7)
    check(m is not None and m[0] == (3, 3) and m[3] == (-1, -1), "7-field matchType: superType 3, wildcard kept")
    check(parse_match("11:1:12-22:1:1:1:1", 7)[3] == (12, 22), "a range field parses")
    print("--- chain + resolver against the installed tree ---")
    ch = Chain(vrf_home)
    check([s for s, _ in ch.order][:2] == ["AggregateTacticalLevel", "AggregateLevelBase"],
          "chain order AggregateTacticalLevel -> AggregateLevelBase", str(ch.order))
    check(len(ch.units()) > 300, "more than 300 kind-11 templates read", str(len(ch.units())))
    check(len(ch.magx) > 300, "more than 300 .magx entries read", str(len(ch.magx)))
    # A known exact template must resolve to itself (DIRTY control below must not).
    t = ch.resolve(parse_type("11:1:225:4:11:0:0")[0])
    check(t is not None and t.label == "ADA BTY (USA, MIM-104 Patriot)", "Patriot battery type lands its own template",
          t.label if t else "nothing")
    # Wildcard containers: a USA mech-infantry division has no specific template, so the
    # generic AggregateLevelBase container must win (country -1).
    t = ch.resolve(parse_type("11:1:225:9:4:1:0")[0])
    check(t is not None and t.name == "DIV, Mech Infantry" and t.role == "CONTAINER",
          "USA mech-inf DIVISION lands the generic 'DIV, Mech Infantry' CONTAINER", t.name if t else "nothing")
    t = ch.resolve(parse_type("11:1:225:10:0:0:0")[0])
    check(t is not None and t.name == "Corps, Army", "a CORPS type lands 'Corps, Army'", t.name if t else "nothing")
    # DIRTY control: an unknown kind-11 type falls through to the BASE abstract, which the
    # validator must treat as a failure - prove the rule really reaches it.
    t = ch.resolve(parse_type("11:5:999:1:99:0:0")[0])
    check(t is not None and t.role == "BASE-ABSTRACT", "an unmatched kind-11 type lands the base abstract (DIRTY control)",
          t.role if t else "nothing")
    rows = build_rows(ch)
    check(len(rows) == len(ch.units()), "one catalogue row per kind-11 template", "%d vs %d" % (len(rows), len(ch.units())))
    # A CONCRETE type (no wildcard) must land a template that publishes that same type - itself,
    # or a same-typed twin that wins the tie on chain order (the rows' 'selfResolves' says which).
    # A VENDOR TYPO is told apart from a resolver bug: a template whose OWN matchType rejects its
    # own objectType (5.2d: 'Missile, 9K720 Iskander SS-26' publishes domain 11 but matches domain
    # 1; 'Missile, P-1000' publishes country 222 but matches 225) can never land by its type, and
    # that is the catalogue's defect - reported, not failed. Any OTHER drift is the resolver's.
    drift, vendor_typos = [], []
    for t in ch.units():
        if -1 in t.otype:
            continue
        landed = ch.resolve(t.otype)
        if landed is None or landed.otype != t.otype:
            if not all(_accepts(t.match[i], t.otype[i]) for i in range(8)):
                vendor_typos.append("%s (objectType %s, matchType rejects it)" % (t.name, type_str(t.otype)))
            else:
                drift.append("%s -> %s" % (t.name, landed.name if landed else "nothing"))
    check(not drift, "every concrete type lands a template publishing that same type", "; ".join(drift[:5]))
    print("  [info] %d vendor template(s) whose own matchType rejects their objectType (never creatable by "
          "type): %s" % (len(vendor_typos), "; ".join(vendor_typos)))
    twins = []
    for t in ch.units():
        landed = ch.resolve(t.otype)
        if landed is not None and landed is not t and landed.otype == t.otype:
            twins.append("%s in %s (-> %s in %s)" % (t.name, t.sms, landed.name, landed.sms))
    print("  [info] %d template(s) share their type with an earlier twin and never land by type: %s"
          % (len(twins), "; ".join(twins)))
    print("SURVEY SELFTEST %s (%d problem(s))" % ("PASS" if bad == 0 else "FAIL", bad))
    return 0 if bad == 0 else 1


def main(argv=None):
    import argparse
    ap = argparse.ArgumentParser(description="Survey the 5.2d aggregate-level unit catalogue (read-only).")
    ap.add_argument("--vrf-home", default=DEFAULT_VRF_HOME)
    ap.add_argument("--top-sms", default=TOP_SMS)
    ap.add_argument("--derived-sms", default=None, metavar="SMS",
                    help="survey a DERIVED chain headed by this .sms (absolute path; e.g. %s). "
                         "Overrides --top-sms." % DERIVED_AGGREGATE_SMS)
    ap.add_argument("--out", default=None, help="write the markdown catalogue here")
    ap.add_argument("--csv", default=None, help="write the full per-template table here")
    ap.add_argument("--reference-scnx", default=None,
                    help="a vendor scenario (.scnx) to analyse as the brigade-population reference")
    ap.add_argument("--selftest", action="store_true")
    a = ap.parse_args(argv)
    if a.selftest:
        return selftest(a.vrf_home)
    chain = Chain(a.vrf_home, a.top_sms, derived_sms=a.derived_sms)
    rows = build_rows(chain)
    ref = None
    if a.reference_scnx:
        ref = reference_summary(chain, a.reference_scnx)
        ref["path"] = a.reference_scnx
    md = render_markdown(chain, rows, ref, a.vrf_home)
    _assert_ascii(md, "markdown")
    if a.out:
        with open(a.out, "w", encoding="utf-8", newline="") as fh:
            fh.write(md)
        print("wrote %s" % a.out)
    if a.csv:
        write_csv(rows, a.csv)
        print("wrote %s (%d rows)" % (a.csv, len(rows)))
    if not a.out:
        sys.stdout.write(md.replace("\r\n", "\n"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
