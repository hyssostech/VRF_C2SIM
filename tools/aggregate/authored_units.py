#!/usr/bin/env python
"""AUTHORED US Army unit types for the aggregate-level profile (package C2, RL-20260927-04) -
the RECIPE GENERATOR. ASCII only. Offline; READS C:\\MAK, never writes under it.

WHAT IT DOES. tools/sms/aggregate_authored_design.json holds the DECISIONS (per authored type:
the donor .entity, the DIS object type, the FM 3-96 composition expressed as the vendor's own
assemblies, and every explicit parameter decision with its reason). This script turns them into
tools/sms/C2SIM_AggregateTacticalLevel.recipe.json: nothing but LITERAL, ASSERTED edits - each
single-line edit names the exact donor line it replaces, each block edit the block's first line,
line count and sha256 - which tools/sms/Deploy-C2SimAggregateSms.ps1 applies at deploy time to the
INSTALLED vendor files. No vendor file is copied into the repo (it is public); the recipe carries
only the asserted lines and our own values.

THE ROLL-UP IS THE VENDOR'S. A unit's combat variables are the vendor's roll-up of its assemblies
(UG52 72.3-72.5 p1420-1429): "When you roll up assemblies, the resources and modifiers in the
assemblies overwrite the current values for the unit ... Any parameters that are not part of an
assembly or roll up rules do not change" (72.5 p1428). The rules are the vendor's own data -
AggregateLevelBase\\gui\\assemblyData.xml rollUpRules: SUM (health, strengths, personnel, supplies,
usage), MAXIMUM (ranges, defense factors), AVERAGE weighted by Base-Health (vulnerability
modifiers), COLLECT (equipment, weapons, ammunition; systems too, see below). --selftest proves
this port reproduces the vendor's own rolled-up units (Engineering BN (POL): every rolled variable
and all three collected maps, exactly) before anything is authored with it.

SYSTEMS ARE NOT ROLLED UP. The vendor's shipped artillery battalions keep ONE howitzer system at
per-battery values whatever their tube count (FA BN (LTU, 105mm), SP Artillery BN (POL, K9):
strength 800, sheaf 75-80), so an authored unit keeps its DONOR's systems; the design deletes or
grafts (from another vendor .entity) whole system blocks where FM 3-96 requires it.

Run with the repo's pinned interpreter (%LOCALAPPDATA%\\Programs\\Python\\Python312\\python.exe):
    python tools/aggregate/authored_units.py --check      # recipe == regenerated (GATE)
    python tools/aggregate/authored_units.py --write      # regenerate the recipe
    python tools/aggregate/authored_units.py --report     # per type: rolled values, kept-from-donor list
    python tools/aggregate/authored_units.py --selftest   # roll-up port vs vendor units + DIRTY controls
"""
import collections
import copy
import hashlib
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)
import survey_magx as sm  # noqa: E402

DESIGN = os.path.join(REPO, "tools", "sms", "aggregate_authored_design.json")
RECIPE = os.path.join(REPO, "tools", "sms", "C2SIM_AggregateTacticalLevel.recipe.json")
SETS_DIR = os.path.join(sm.DEFAULT_VRF_HOME, "data", "simulationModelSets")
ASSEMBLY_FILES = [("AggregateLevelBase", os.path.join("AggregateLevelBase", "gui", "assemblyData.xml")),
                  ("AggregateTacticalLevel", os.path.join("AggregateTacticalLevel", "gui", "assemblyData.xml"))]


class RecipeError(Exception):
    pass


# ---------------------------------------------------------------------------------------------
# Vendor text files: byte-preserving lines (latin-1), one line terminator per file
# ---------------------------------------------------------------------------------------------

def sha256_bytes(b):
    return hashlib.sha256(b).hexdigest()


class VendorText(object):
    """A vendor text file as lines. Latin-1 decoding keeps every byte (a non-ASCII byte survives
    to the ASCII gate on the OUTPUT, where it fails the build unless an edit removed it)."""

    def __init__(self, sets_dir, rel):
        self.rel = rel.replace("\\", "/")
        self.path = os.path.join(sets_dir, rel.replace("/", os.sep))
        if not os.path.isfile(self.path):
            raise RecipeError("vendor file missing: %s" % self.path)
        raw = open(self.path, "rb").read()
        self.sha256 = sha256_bytes(raw)
        crlf, lf = raw.count(b"\r\n"), raw.count(b"\n")
        if lf == 0 or not raw.endswith(b"\n"):
            raise RecipeError("vendor file has no final line terminator: %s" % self.path)
        if crlf == lf:
            self.eol = "CRLF"
            body = raw[:-2].split(b"\r\n")
        elif crlf == 0:
            self.eol = "LF"
            body = raw[:-1].split(b"\n")
        else:
            raise RecipeError("vendor file mixes CRLF and LF: %s (CRLF %d, LF %d)" % (self.path, crlf, lf))
        self.lines = [x.decode("latin-1") for x in body]

    def join(self, lines):
        t = "\r\n" if self.eol == "CRLF" else "\n"
        return (t.join(lines) + t).encode("latin-1")


def block_sha(lines, eol):
    t = "\r\n" if eol == "CRLF" else "\n"
    return sha256_bytes((t.join(lines) + t).encode("latin-1"))


def find_one(lines, pred, what, where):
    hits = [i for i, x in enumerate(lines) if pred(x)]
    if len(hits) != 1:
        raise RecipeError("%s: expected exactly ONE line %s, found %d" % (where, what, len(hits)))
    return hits[0]


PARAM_RE = r'^(\s*)<(int|real|string|bool) paramName="%s"(?:>(.*)</\2>|/>)$'


def param_line(lines, name, where):
    rx = re.compile(PARAM_RE % re.escape(name))
    i = find_one(lines, lambda x: rx.match(x) is not None, '<int|real|string|bool paramName="%s">' % name, where)
    m = rx.match(lines[i])
    return i, m.group(1), m.group(2), (m.group(3) if m.group(3) is not None else "")


def fmt_param(indent, tag, name, value):
    if value == "":
        return '%s<%s paramName="%s"/>' % (indent, tag, name)
    return '%s<%s paramName="%s">%s</%s>' % (indent, tag, name, value, tag)


def block_span(lines, start_pred, what, where):
    """(start, count) of the element opened on the ONE line start_pred matches, up to the first
    later line that closes it at the same indentation; a self-closing start line is a 1-line block."""
    i = find_one(lines, start_pred, what, where)
    s = lines[i]
    if s.rstrip().endswith("/>"):
        return i, 1
    indent = s[:len(s) - len(s.lstrip())]
    tag = re.match(r"\s*<([A-Za-z_][\w.-]*)", s).group(1)
    close = indent + "</%s>" % tag
    for j in range(i + 1, len(lines)):
        if lines[j] == close:
            return i, j - i + 1
    raise RecipeError("%s: block %s never closes (%r)" % (where, what, close))


def map_start(name):
    return lambda x: x.strip() in ('<DtRwMap paramName="%s">' % name, '<DtRwMap paramName="%s"/>' % name)


def system_start(system_name):
    pre = '<componentSystem systemName="%s" ' % system_name
    return lambda x: x.strip().startswith(pre)


def struct_start(name):
    return lambda x: x.strip() == '<DtRwStructure paramName="%s">' % name


def xml_escape(s):
    """Element TEXT: & < > only - the vendor writes quotes raw in text ("Unit" "Infantry")."""
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def xml_attr(s):
    return xml_escape(s).replace('"', "&quot;")


# ---------------------------------------------------------------------------------------------
# The vendor assembly data and its roll-up rules
# ---------------------------------------------------------------------------------------------

def _s(n, name):
    e = n.find("string[@paramName='%s']" % name)
    return (e.text or "").strip() if e is not None and e.text else ""


def _attrs(n):
    a = n.find("strings[@paramName='attributes']")
    return [(s.get("paramName"), (s.text or "").strip()) for s in a] if a is not None else []


def _kids(n):
    c = n.find("DtAssembliesReaderWriterRwRootNodes[@paramName='children']")
    return list(c) if c is not None else []


class AssemblyData(object):
    """AggregateLevelBase then AggregateTacticalLevel assemblyData.xml: a keyword defined again in
    the higher-priority set replaces the included one (UG52 72.4.2 p1425)."""

    def __init__(self, sets_dir=SETS_DIR):
        self.assemblies = {}
        self.dup = []
        self.defs = {"equipment": {}, "weapons": {}, "ammunition": {}}
        self.rules = {}
        self.files = {}
        for sms, rel in ASSEMBLY_FILES:
            p = os.path.join(sets_dir, rel)
            raw = open(p, "rb").read()
            self.files[rel.replace("\\", "/")] = sha256_bytes(raw)
            rw = ET.fromstring(raw).find("DtRwAssembliesReaderWriter")
            seen = collections.Counter()
            sec = rw.find("DtAssembliesReaderWriterRwRootNodes[@paramName='assemblies']")
            for a in sec:
                kw = _s(a, "keyword")
                seen[kw] += 1
                self.assemblies[kw] = (sms, a)
            self.dup += ["%s/%s" % (sms, k) for k, v in seen.items() if v > 1]
            for kind in self.defs:
                s = rw.find("DtAssembliesReaderWriterRwRootNodes[@paramName='%s']" % kind)
                for n in (s if s is not None else []):
                    self.defs[kind][_s(n, "keyword")] = dict(_attrs(n))
            rr = rw.find("DtAssembliesReaderWriterRwRootNode[@paramName='rollUpRules']")
            if rr is not None:
                for rule in _kids(rr):
                    for c in _kids(rule):
                        self.rules[c.get("paramName")] = (rule.get("paramName"), dict(_attrs(c)))

    def contents(self, kw):
        if kw not in self.assemblies:
            raise RecipeError("assembly %r is not defined in the installed assemblyData.xml" % kw)
        if any(d.endswith("/" + kw) for d in self.dup):
            raise RecipeError("assembly %r is defined twice in one SMS - ambiguous, do not use it" % kw)
        out = []
        for c in _kids(self.assemblies[kw][1]):
            kind = c.get("paramName")
            key = _s(c, "keyword")
            if kind == "system":
                out.append((kind, key, None))
            else:
                out.append((kind, key, dict(_attrs(c)).get("value")))
        return out


def rollup(ad, assemblies):
    """The vendor's roll-up of [(keyword, count)]. Returns (values, supplied, collected, systems).
    values: every rule variable some assembly supplies; supplied: that set; collected: ordered
    {equipment|weapon|ammunition: {key: count}}; systems: the system definitions the assemblies
    carry (reported, never applied - see the module note)."""
    sums, maxs = collections.defaultdict(float), {}
    num, den = collections.defaultdict(float), collections.defaultdict(float)
    collected = collections.OrderedDict((k, collections.OrderedDict()) for k in ("equipment", "weapon", "ammunition"))
    systems, supplied, unknown = [], set(), set()
    for kw, n in assemblies:
        items = ad.contents(kw)
        health = sum(float(v) for k, key, v in items if k == "variable" and key == "Base-Health")
        for kind, key, v in items:
            if kind in collected:
                collected[kind][key] = collected[kind].get(key, 0) + int(round(float(v))) * n
            elif kind == "system":
                if key not in systems:
                    systems.append(key)
            elif kind == "variable":
                rule = ad.rules.get(key, ("?", {}))[0]
                x = float(v)
                supplied.add(key)
                if rule == "sum":
                    sums[key] += x * n
                elif rule == "maximum":
                    maxs[key] = max(maxs.get(key, x), x)
                elif rule == "average":
                    num[key] += x * health * n
                    den[key] += health * n
                elif rule == "ignore":
                    supplied.discard(key)
                else:
                    unknown.add(key)
    if unknown:
        raise RecipeError("assembly variable(s) with no roll-up rule: %s" % sorted(unknown))
    values = dict(sums)
    values.update(maxs)
    for k in num:
        values[k] = num[k] / den[k] if den[k] else 0.0
    return values, supplied, collected, systems


def fmt_number(tag, x):
    if tag == "int":
        return "%d" % int(round(x))
    s = "%.13g" % x
    return "0" if s in ("-0", "0") else s


# A supply's STOCK and its per-second USAGE are a pair: their ratio is how long the unit lasts
# without resupply (the vendor's units carry 1-4 days: Mech CO (USA, M2) food 400 at 0.00463/s = 1
# day, Engineering BN (POL) 1002 at 0.003229/s = 3.6 days). When the roll-up supplies only one half
# of a pair, the other half is rescaled so the DONOR's endurance is kept - otherwise a rolled stock
# meets the donor's consumption (e.g. an infantry battalion's rolled food against a mechanized
# battalion's eating rate: 14 hours of food).
ENDURANCE_PAIRS = [("Base-Food", "Food-Usage-Per-Second"), ("Base-Water", "Water-Usage-Per-Second"),
                   ("Base-Oil", "Oil-Usage-Per-Second"), ("Base-Lubricant", "Lubricant-Usage-Per-Second"),
                   ("Base-Diesel-Fuel", "Diesel-Fuel-Usage-Per-Second"),
                   ("Base-Motor-Gas", "Motor-Gas-Usage-Per-Second")]


# ---------------------------------------------------------------------------------------------
# One authored type -> its files (entity, leaf, magx) as asserted edits
# ---------------------------------------------------------------------------------------------

class Builder(object):
    def __init__(self, sets_dir=SETS_DIR):
        self.sets_dir = sets_dir
        self.ad = AssemblyData(sets_dir)
        self.inputs = collections.OrderedDict()
        self._cache = {}

    def vendor(self, rel):
        if rel not in self._cache:
            vt = VendorText(self.sets_dir, rel)
            self._cache[rel] = vt
            self.inputs[vt.rel] = vt.sha256
        return self._cache[rel]

    # -- edit helpers: each records the donor line / block it asserts, and applies it to `lines`
    def line_edit(self, lines, idx, new, edits, why):
        edits.append(collections.OrderedDict([("op", "line"), ("line", lines[idx]), ("with", new), ("why", why)]))
        lines[idx] = new

    def block_edit(self, lines, start, count, new_lines, edits, why, eol, summary):
        blk = lines[start:start + count]
        e = collections.OrderedDict([("op", "block"), ("start", blk[0]), ("count", count),
                                     ("sha256", block_sha(blk, eol)), ("summary", summary),
                                     ("with", list(new_lines)), ("why", why)])
        edits.append(e)
        lines[start:start + count] = list(new_lines)

    def build_entity(self, t, report):
        donor = self.vendor(t["donor"])
        lines = list(donor.lines)
        eol = donor.eol
        where = t["donor"]
        edits = []

        # 1. identity: the simObject line (objectType + an EXACT matchType, so the authored template
        #    only ever answers its own type and captures no query another template serves)
        i = find_one(lines, lambda x: x.strip().startswith("<simObject "), "<simObject ...>", where)
        ot = t["objectType"]
        new = re.sub(r'objectType="[^"]*"', 'objectType="%s"' % ot, lines[i])
        new = re.sub(r'matchType="[^"]*"', 'matchType="%s"' % ot, new)
        if new == lines[i] or 'matchType="%s"' % ot not in new:
            raise RecipeError("%s: the simObject line did not take the new type" % where)
        self.line_edit(lines, i, new, edits, "identity: DIS type %s, exact matchType (design sec 'identity')" % ot)
        ident = collections.OrderedDict([("gui-label", t["label"]),
                                         ("gui-unique-id", "ModelSet-%s-%s" % (t["guiUniqueNumber"], t["label"]))])
        ident.update(t["identity"])
        for name, value in ident.items():
            j, ind, tag, old = param_line(lines, name, where)
            if old != value:
                self.line_edit(lines, j, fmt_param(ind, tag, name, xml_escape(value)), edits, "identity: %s" % name)

        rolled, kept, decisions = {}, [], []
        donor_value = {}
        for x in donor.lines:
            m = re.match(r'\s*<(?:int|real) paramName="([^"]+)">([^<]*)</(?:int|real)>$', x)
            if m:
                donor_value[m.group(1)] = m.group(2)
        if t.get("assemblies"):
            asm = [(a[0], int(a[1])) for a in t["assemblies"]]
            values, supplied, collected, systems = rollup(self.ad, asm)
            rolled = values
            # assemblies map
            s, c = block_span(lines, map_start("Assemblies"), '<DtRwMap paramName="Assemblies">', where)
            ind = lines[s][:len(lines[s]) - len(lines[s].lstrip())]
            new_map = [ind + '<DtRwMap paramName="Assemblies">']
            new_map += ['%s   <int paramName="%s">%d</int>' % (ind, xml_attr(k), n) for k, n in asm]
            new_map += [ind + '   <KeyPrototype XMLType="string" paramName="key"/>', ind + '</DtRwMap>']
            self.block_edit(lines, s, c, new_map, edits, "roll-up input: the FM 3-96 composition as vendor assemblies",
                            eol, "donor Assemblies map -> %d authored assembly lines" % len(asm))
            # rolled variables, in donor line order
            for name in sorted(values, key=lambda n: param_line(lines, n, where)[0]):
                j, ind2, tag, old = param_line(lines, name, where)
                nv = fmt_number(tag, values[name])
                if nv != old:
                    self.line_edit(lines, j, fmt_param(ind2, tag, name, nv), edits,
                                   "roll-up (%s): %s" % (self.ad.rules[name][0], name))
            # the endurance rule (ENDURANCE_PAIRS): one half rolled, the other rescaled to the donor's ratio
            for stock, use in ENDURANCE_PAIRS:
                if (stock in supplied) == (use in supplied):
                    continue
                ds, du = float(donor_value.get(stock, "0")), float(donor_value.get(use, "0"))
                if ds <= 0.0 or du <= 0.0:
                    continue
                if stock in supplied:
                    name, nvf = use, values[stock] * du / ds
                else:
                    name, nvf = stock, values[use] * ds / du
                j, ind2, tag, old = param_line(lines, name, where)
                nv = fmt_number(tag, nvf)
                why = ("roll-up (endurance): %s rescaled to the rolled %s at the donor's ratio (%.3g days without "
                       "resupply)" % (name, stock if name == use else use, ds / du / 86400.0))
                decisions.append((name, nv, why))
                supplied.add(name)
                if nv != old:
                    self.line_edit(lines, j, fmt_param(ind2, tag, name, nv), edits, why)
            # collected maps (equipment gets the design's extras; reporting only, UG52 72.7.9)
            equip = collections.OrderedDict(collected["equipment"])
            for k, n in t.get("equipmentExtra", []):
                equip[k] = equip.get(k, 0) + int(n)
            for mapname, kind, items in (("Base-Equipment", "equipment", equip),
                                         ("Base-Weapons", "weapons", collected["weapon"]),
                                         ("Base-Ammunition", "ammunition", collected["ammunition"])):
                self._replace_map(lines, mapname, kind, items, t, edits, eol, where,
                                  "roll-up (collect): %s" % mapname)
            for name in sorted(self.ad.rules):
                rule = self.ad.rules[name][0]
                if rule in ("sum", "maximum", "average") and name not in supplied:
                    try:
                        _j, _i, _tag, old = param_line(lines, name, where)
                    except RecipeError:
                        continue
                    kept.append((name, old))
            report["systemsInAssemblies"] = systems
        elif t.get("equipment") is not None:
            self._replace_map(lines, "Base-Equipment", "equipment", collections.OrderedDict(
                (k, int(n)) for k, n in t["equipment"]), t, edits, eol, where,
                "equipment (reporting only, UG52 72.7.9): donor list -> US vehicles")

        # 2. explicit parameter decisions (after the roll-up; each carries its reason)
        for name, spec in t.get("set", {}).items():
            j, ind, tag, old = param_line(lines, name, where)
            nv = str(spec["value"])
            decisions.append((name, nv, "SET: " + spec["why"]))
            if nv != old:
                self.line_edit(lines, j, fmt_param(ind, tag, name, xml_escape(nv)), edits, spec["why"])
        decided = set(n for n, _v, _w in decisions)
        kept = [(n, v) for n, v in kept if n not in decided]

        # 3. systems: delete / replace from another vendor file / insert from another vendor file
        for spec in t.get("systems", {}).get("delete", []):
            s, c = block_span(lines, system_start(spec["systemName"]), "componentSystem %s" % spec["systemName"], where)
            blk = lines[s:s + c]
            edits.append(collections.OrderedDict([("op", "delete"), ("start", blk[0]), ("count", c),
                                                  ("sha256", block_sha(blk, eol)), ("why", spec["why"])]))
            del lines[s:s + c]
        for spec in t.get("systems", {}).get("replaceFrom", []):
            src = self.vendor(spec["source"])
            ss, sc = block_span(src.lines, system_start(spec["sourceSystemName"]),
                                "componentSystem %s" % spec["sourceSystemName"], spec["source"])
            s, c = block_span(lines, system_start(spec["systemName"]), "componentSystem %s" % spec["systemName"], where)
            blk, sblk = lines[s:s + c], src.lines[ss:ss + sc]
            edits.append(collections.OrderedDict([
                ("op", "replace-from"), ("start", blk[0]), ("count", c), ("sha256", block_sha(blk, eol)),
                ("from", src.rel), ("fromStart", sblk[0]), ("fromCount", sc), ("fromSha256", block_sha(sblk, src.eol)),
                ("why", spec["why"])]))
            lines[s:s + c] = sblk
        for spec in t.get("systems", {}).get("insertFrom", []):
            src = self.vendor(spec["source"])
            ss, sc = block_span(src.lines, system_start(spec["sourceSystemName"]),
                                "componentSystem %s" % spec["sourceSystemName"], spec["source"])
            a, _c = block_span(lines, system_start(spec["before"]), "componentSystem %s" % spec["before"], where)
            sblk = src.lines[ss:ss + sc]
            edits.append(collections.OrderedDict([
                ("op", "insert-from"), ("before", lines[a]), ("from", src.rel), ("fromStart", sblk[0]),
                ("fromCount", sc), ("fromSha256", block_sha(sblk, src.eol)), ("why", spec["why"])]))
            lines[a:a] = sblk
        for spec in t.get("resources", {}).get("delete", []):
            ms, mc = block_span(lines, map_start("Base-Resources"), "Base-Resources", where)
            sub = lines[ms:ms + mc]
            s, c = block_span(sub, struct_start(spec["resource"]), "resource %s" % spec["resource"], where)
            s += ms
            blk = lines[s:s + c]
            edits.append(collections.OrderedDict([("op", "delete"), ("start", blk[0]), ("count", c),
                                                  ("sha256", block_sha(blk, eol)), ("why", spec["why"])]))
            del lines[s:s + c]

        out = (("\r\n" if eol == "CRLF" else "\n").join(lines) + ("\r\n" if eol == "CRLF" else "\n")).encode("latin-1")
        bad = [(n + 1, x) for n, x in enumerate(lines) if any(ord(ch) > 126 or (ord(ch) < 32 and ch != "\t") for ch in x)]
        if bad:
            raise RecipeError("%s: output is not ASCII at line %d: %r - an edit must remove it" % (t["label"], bad[0][0], bad[0][1]))
        try:
            ET.fromstring(out)
        except ET.ParseError as e:
            raise RecipeError("%s: generated .entity does not parse: %s" % (t["label"], e))
        report["rolled"] = rolled
        report["keptFromDonor"] = kept
        report["decisions"] = decisions
        report["entitySha256"] = sha256_bytes(out)
        report["entityBytes"] = out
        return collections.OrderedDict([("to", "vrfSim/%s.entity" % t["label"]), ("from", donor.rel),
                                        ("fromSha256", donor.sha256), ("eol", eol), ("edits", edits),
                                        ("sha256", sha256_bytes(out))])

    def _replace_map(self, lines, mapname, kind, items, t, edits, eol, where, why):
        s, c = block_span(lines, map_start(mapname), '<DtRwMap paramName="%s">' % mapname, where)
        ind = lines[s][:len(lines[s]) - len(lines[s].lstrip())]
        pacing = set(t.get("pacing", []))
        new = [ind + '<DtRwMap paramName="%s">' % mapname]
        for key, n in items.items():
            if n <= 0:
                continue
            d = self.ad.defs[kind].get(key)
            if d is None:
                raise RecipeError("%s: %s key %r is not defined in the installed assemblyData.xml" % (t["label"], kind, key))
            if kind == "ammunition":
                third = ("Type", d.get("Type") or "")
            else:
                cat = d.get("Category") or ""
                if kind == "equipment" and (not cat or "Ground Vehicle" in cat):
                    cat = "Ground Vehicle Category"     # vendor typo 'Ground Ground ...' and unset Engr keys normalised
                third = ("Category", cat if kind == "equipment" else (cat or "Weapon Category"))
            new.append('%s   <DtRwStructure paramName="%s">' % (ind, xml_attr(key)))
            new.append('%s      <int paramName="Count">%d</int>' % (ind, n))
            pt = "Pacing" if key in pacing else ""
            new.append(fmt_param(ind + "      ", "string", "Pacing-Tracking", pt))
            new.append(fmt_param(ind + "      ", "string", third[0], xml_escape(third[1])))
            new.append('%s   </DtRwStructure>' % ind)
        new += [ind + '   <KeyPrototype XMLType="string" paramName="key"/>', ind + '</DtRwMap>']
        old_keys = [re.search(r'paramName="([^"]*)"', x).group(1) for x in lines[s + 1:s + c]
                    if x.strip().startswith("<DtRwStructure ")]
        self.block_edit(lines, s, c, new, edits, why, eol,
                        "%s: donor keys %s -> %s" % (mapname, ", ".join(old_keys) or "(none)",
                                                     ", ".join("%s x%d" % kv for kv in items.items() if kv[1] > 0)
                                                     or "(none)"))

    def build_leaf(self, t, report):
        src = self.vendor(t["leaf"]["source"])
        lines = list(src.lines)
        el = t["leaf"]["element"]
        i = find_one(lines, lambda x: x.strip() == "<myName>%s</myName>" % el and x.startswith("\t\t\t<myName>"),
                     "<myName>%s</myName> (the element name)" % el, src.rel)
        edits = []
        self.line_edit(lines, i, lines[i].replace(">%s<" % el, ">%s<" % t["label"]), edits,
                       "element definition renamed for the authored type (symbol glyph kept: %s)" % t["leaf"]["why"])
        out = src.join(lines)
        ET.fromstring(out)
        report["leafSha256"] = sha256_bytes(out)
        report["leafBytes"] = out
        return collections.OrderedDict([("to", "gui/visuals/Unit/%s.leaf" % t["label"]), ("from", src.rel),
                                        ("fromSha256", src.sha256), ("eol", src.eol), ("edits", edits),
                                        ("sha256", sha256_bytes(out))])

    def build_magx(self, t, design, report):
        m = design["magxTemplate"]
        src = self.vendor(m["source"])
        lines = list(src.lines)
        edits = []
        i = find_one(lines, lambda x: x.strip() == "<first>%s</first>" % m["first"], "<first>%s</first>" % m["first"], src.rel)
        self.line_edit(lines, i, lines[i].replace(m["first"], t["objectType"]), edits, "unit type map: the authored type")
        j = find_one(lines, lambda x: x.strip() == "<second>%s</second>" % m["second"], "<second>%s</second>" % m["second"], src.rel)
        self.line_edit(lines, j, lines[j].replace(m["second"], t["label"]), edits, "-> the authored element definition")
        out = src.join(lines)
        ET.fromstring(out)
        report["magxSha256"] = sha256_bytes(out)
        report["magxBytes"] = out
        return collections.OrderedDict([("to", "gui/visuals/Unit/%s.magx" % t["label"]), ("from", src.rel),
                                        ("fromSha256", src.sha256), ("eol", src.eol), ("edits", edits),
                                        ("sha256", sha256_bytes(out))])

    def build_sms(self, design):
        s = design["sms"]
        src = self.vendor(s["template"])
        lines = list(src.lines)
        for a in s["assert"]:
            find_one(lines, lambda x, a=a: x == a, repr(a), src.rel)
        edits = []
        for r in s["replace"]:
            i = find_one(lines, lambda x, r=r: x == r["line"], repr(r["line"]), src.rel)
            self.line_edit(lines, i, r["with"], edits, r["why"])
        if lines and lines[0].startswith(";;"):
            raise RecipeError("%s: the template now carries a header comment; re-derive the recipe" % src.rel)
        out = src.join(list(s["header"]) + lines)
        return collections.OrderedDict([("to", design["name"] + ".sms"), ("from", src.rel), ("fromSha256", src.sha256),
                                        ("eol", src.eol), ("header", list(s["header"])), ("assert", list(s["assert"])),
                                        ("edits", edits), ("sha256", sha256_bytes(out))]), out


def build_recipe(design, sets_dir=SETS_DIR):
    b = Builder(sets_dir)
    sms_entry, sms_bytes = b.build_sms(design)
    opd = b.vendor(design["opd"])
    files, reports, outputs = [], collections.OrderedDict(), collections.OrderedDict()
    outputs[sms_entry["to"]] = sms_bytes
    seen_types, seen_labels = set(), set()
    for t in design["types"]:
        if t["objectType"] in seen_types or t["label"] in seen_labels:
            raise RecipeError("duplicate authored type or label: %s / %s" % (t["objectType"], t["label"]))
        seen_types.add(t["objectType"])
        seen_labels.add(t["label"])
        rep = collections.OrderedDict()
        ent = b.build_entity(t, rep)
        outputs[design["name"] + "/" + ent["to"]] = rep.pop("entityBytes")
        files.append(ent)
        leaf = b.build_leaf(t, rep)
        outputs[design["name"] + "/" + leaf["to"]] = rep.pop("leafBytes")
        files.append(leaf)
        magx = b.build_magx(t, design, rep)
        outputs[design["name"] + "/" + magx["to"]] = rep.pop("magxBytes")
        files.append(magx)
        reports[t["id"]] = rep
    recipe = collections.OrderedDict()
    recipe["schemaVersion"] = 1
    recipe["generatedBy"] = ("tools/aggregate/authored_units.py --write, from tools/sms/aggregate_authored_design.json. "
                             "DO NOT HAND-EDIT: change the design and regenerate; --check gates drift.")
    recipe["name"] = design["name"]
    recipe["modelSetDirectory"] = design["name"]
    recipe["vendorRoot"] = "data/simulationModelSets (under -VrfRoot)"
    recipe["sms"] = sms_entry
    recipe["copy"] = [collections.OrderedDict([("from", opd.rel), ("to", "vrfSim.opd"), ("sha256", opd.sha256),
                                               ("why", "every SMS must have vrfSim.opd (UG52 68.3.5 p1314); "
                                                       "byte-identical to the SMS this one extends")])]
    recipe["files"] = files
    inputs = collections.OrderedDict(sorted(b.inputs.items()))
    inputs.update(sorted(b.ad.files.items()))
    recipe["vendorInputs"] = inputs
    outputs[design["name"] + "/vrfSim.opd"] = open(opd.path, "rb").read()
    b.outputs = outputs
    return recipe, reports, b


def dumps(obj):
    text = json.dumps(obj, indent=1, ensure_ascii=True)
    return text.replace("\r\n", "\n").replace("\n", "\r\n") + "\r\n"


# ---------------------------------------------------------------------------------------------
# Report, check, self-test
# ---------------------------------------------------------------------------------------------

def report_text(design, reports):
    out = []
    for t in design["types"]:
        r = reports[t["id"]]
        out.append("== %s  %s  (donor %s)" % (t["label"], t["objectType"], t["donor"].split("/")[-1]))
        decided = dict((n, (v, w)) for n, v, w in r.get("decisions", []))
        for k in sorted(r.get("rolled", {})):
            if k not in decided:
                out.append("   rolled   %-48s %s" % (k, "%.6g" % r["rolled"][k]))
        for k in sorted(decided):
            out.append("   DECIDED  %-48s %-14s %s" % (k, decided[k][0], decided[k][1]))
        for k, v in r.get("keptFromDonor", []):
            out.append("   KEPT     %-48s %s   (no authored assembly supplies it: the donor's value stands)" % (k, v))
        if r.get("systemsInAssemblies"):
            out.append("   systems the assemblies carry (NOT applied): %s" % ", ".join(
                s.split("\\")[-1] for s in r["systemsInAssemblies"]))
        out.append("   entity sha256 %s" % r["entitySha256"])
    return out


def selftest():
    bad = [0]

    def check(ok, what, detail=""):
        print("  [%s] %s%s" % ("PASS" if ok else "FAIL", what, (" -- " + str(detail)) if detail and not ok else ""))
        if not ok:
            bad[0] += 1

    print("--- 1. the roll-up port reproduces the vendor's own rolled-up units ---")
    ad = AssemblyData()
    ch = sm.Chain()

    def entity_values(name):
        t = next(x for x in ch.templates if x.name == name)
        root = ET.parse(t.path).getroot().find("simObject")
        vals = {}
        for e in root:
            if e.tag in ("int", "real") and e.get("paramName"):
                vals[e.get("paramName")] = float((e.text or "0").strip())
        asm = [(i.get("paramName"), int(i.text)) for i in root.find("DtRwMap[@paramName='Assemblies']")
               if i.tag == "int"]

        def mp(n):
            m = root.find("DtRwMap[@paramName='%s']" % n)
            return collections.OrderedDict((s.get("paramName"), int(s.find("int[@paramName='Count']").text))
                                           for s in m if s.tag == "DtRwStructure")
        return vals, asm, mp("Base-Equipment"), mp("Base-Weapons"), mp("Base-Ammunition")

    vals, asm, eq, wp, am = entity_values("Engineering BN (POL)")
    values, supplied, col, _sys = rollup(ad, asm)
    diffs = [k for k in values if k in vals and abs(vals[k] - values[k]) > max(1e-6, 1e-4 * abs(values[k]))]
    check(not diffs and len(values) >= 30,
          "Engineering BN (POL): all %d rolled variables equal the vendor's .entity" % len(values), diffs)
    check(dict(col["equipment"]) == dict(eq) and dict(col["weapon"]) == dict(wp) and dict(col["ammunition"]) == dict(am),
          "Engineering BN (POL): collected equipment / weapons / ammunition equal the vendor's maps")
    vals, asm, eq, wp, am = entity_values("Mech CO (USA, M2)")
    values, supplied, col, _sys = rollup(ad, asm)
    for k in ("Base-Health", "Base-Combat-Power-Anti-Tank-Strength", "Base-Combat-Power-Anti-Personnel-Strength",
              "Base-Combat-Power-High-Explosive-Strength", "Base-Combat-Power-Anti-Tank-Range", "Base-Diesel-Fuel"):
        check(abs(vals[k] - values[k]) < 1e-6, "Mech CO (USA, M2): %s %g == vendor %g" % (k, values[k], vals[k]))
    check(dict(col["weapon"]) == dict(wp) and dict(col["ammunition"]) == dict(am),
          "Mech CO (USA, M2): collected weapons and ammunition equal the vendor's maps")
    check(ad.rules.get("Base-Health", ("?",))[0] == "sum" and
          ad.rules.get("Base-Combat-Power-Anti-Tank-Range", ("?",))[0] == "maximum" and
          ad.rules.get("Base-Vulnerability-Anti-Tank-Modifier", ("?",))[0] == "average",
          "the vendor rules are read, not assumed: Base-Health sum, ranges maximum, vulnerability average")
    check(ad.rules["Base-Vulnerability-Anti-Tank-Modifier"][1].get("weight") == "Base-Health",
          "the vulnerability average is weighted by Base-Health (the vendor's rollUpRules)")
    # DISCRIMINATING: the average runs over the assemblies that DEFINE the modifier only. Tank BN HQ
    # (USA, M1A2) = 2 M1A2 (AT 0.7) + 2 M113A2 + 8 personnel assemblies (none define it): the vendor's
    # 0.7 is the supplier-only mean; counting the others as 0 would give 0.594.
    for name, key in (("Tank BN HQ (USA, M1A2)", "Base-Vulnerability-Anti-Tank-Modifier"),
                      ("Mortar PLT (USA, M1064)", "Base-Vulnerability-Anti-Personnel-Modifier")):
        vals, asm, _e, _w, _a = entity_values(name)
        values, _s, _c, _y = rollup(ad, asm)
        check(abs(values.get(key, -1) - vals[key]) < 1e-9 and values.get("Base-Health") == vals["Base-Health"],
              "%s: %s %g == vendor (supplier-only average) and health %g" % (name, key, values.get(key, -1),
                                                                            vals["Base-Health"]))
    design0 = json.load(open(DESIGN, encoding="utf-8"))
    ent = os.path.join(SETS_DIR, "EntityLevel", "vrfSim")
    missing = [f for f in design0["equipmentToEntityLevel"].values() if f and not os.path.isfile(os.path.join(ent, f))]
    check(not missing, "every entity-level vehicle named for an equipment key exists in EntityLevel\\vrfSim", missing)
    keys = set(design0["equipmentToEntityLevel"])
    used = set(k for t in design0["types"] for k, _n in (t.get("equipment") or []) + (t.get("equipmentExtra") or []))
    check(used <= keys, "every authored equipment key has an entity-level entry (or an explicit none)", sorted(used - keys))

    print("--- 2. the committed recipe is the regeneration of the committed design (CLEAN) ---")
    design = json.load(open(DESIGN, encoding="utf-8"))
    recipe, reports, _b = build_recipe(design)
    committed = open(RECIPE, "rb").read().decode("utf-8") if os.path.isfile(RECIPE) else ""
    check(dumps(recipe) == committed, "recipe == regenerated (run --write after changing the design)")
    check(all(ord(c) < 127 for c in committed), "recipe is ASCII")
    check(committed.count("\r\n") == committed.count("\n") and committed.endswith("\r\n"), "recipe is CRLF")

    print("--- 3. DIRTY controls: each defect must be REFUSED ---")

    def refused(name, mutate, expect):
        d = copy.deepcopy(design)
        mutate(d)
        try:
            build_recipe(d)
            ok, msg = False, "built without error"
        except (RecipeError, KeyError) as e:
            ok, msg = expect in str(e), str(e)
        check(ok, "DIRTY %s -> refused (%s)" % (name, expect), msg)

    def unknown_assembly(d):
        d["types"][0]["assemblies"].append(["assembly_No Such Thing", 1, "x"])

    def unknown_equipment(d):
        t = next(x for x in d["types"] if x.get("equipmentExtra"))
        t["equipmentExtra"].append(["No Such Vehicle", 1])

    def missing_system(d):
        t = next(x for x in d["types"] if x.get("systems", {}).get("delete"))
        t["systems"]["delete"][0]["systemName"] = "no-such-system"

    def dup_type(d):
        d["types"][1]["objectType"] = d["types"][0]["objectType"]

    def sms_assert(d):
        d["sms"]["assert"].append("   (no-such-line True)")

    def non_ascii_kept(d):
        t = next(x for x in d["types"] if x["donor"].endswith("Engineering BN (POL).entity"))
        t.pop("equipment", None)     # the donor's equipment map (with its non-ASCII key) would survive

    refused("an assembly the vendor does not define", unknown_assembly, "not defined")
    refused("an equipment key the vendor does not define", unknown_equipment, "not defined")
    refused("a system block the donor does not carry", missing_system, "exactly ONE line")
    refused("two authored types sharing one DIS type", dup_type, "duplicate")
    refused("a .sms template line that is not there", sms_assert, "exactly ONE line")
    refused("a donor non-ASCII line left in the output", non_ascii_kept, "not ASCII")
    print("AUTHORED UNITS SELFTEST %s (%d problem(s))" % ("PASS" if bad[0] == 0 else "FAIL", bad[0]))
    return 0 if bad[0] == 0 else 1


def main(argv=None):
    import argparse
    ap = argparse.ArgumentParser(description="Generate / check the C2SIM authored-unit recipe (package C2).")
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--write", action="store_true", help="regenerate tools/sms/C2SIM_AggregateTacticalLevel.recipe.json")
    g.add_argument("--check", action="store_true", help="GATE: the committed recipe equals the regeneration")
    g.add_argument("--report", action="store_true", help="print the rolled values and the kept-from-donor lists")
    g.add_argument("--selftest", action="store_true")
    g.add_argument("--emit", metavar="DIR",
                   help="write the files the recipe produces into DIR for review (a scratch directory; never under "
                        "C:\\MAK). The deploy script is the only thing that writes the deployed set.")
    a = ap.parse_args(argv)
    if a.selftest:
        return selftest()
    design = json.load(open(DESIGN, encoding="utf-8"))
    try:
        recipe, reports, b = build_recipe(design)
    except RecipeError as e:
        print("RECIPE REFUSED: %s" % e)
        return 3
    if a.emit:
        full = os.path.abspath(a.emit)
        low = full.lower().rstrip("\\") + "\\"
        if any(low.startswith(os.path.abspath(r).lower().rstrip("\\") + "\\") for r in ("C:\\MAK", sm.DEFAULT_VRF_HOME)):
            print("REFUSED: --emit under C:\\MAK or the vendor tree (%s)" % full)
            return 2
        for rel, data in b.outputs.items():
            p = os.path.join(full, rel.replace("/", os.sep))
            if not os.path.isdir(os.path.dirname(p)):
                os.makedirs(os.path.dirname(p))
            with open(p, "wb") as fh:
                fh.write(data)
            print("  %s  %7d  %s" % (sha256_bytes(data), len(data), p))
        return 0
    text = dumps(recipe)
    if a.write:
        with open(RECIPE, "wb") as fh:
            fh.write(text.encode("ascii"))
        print("wrote %s (%d files, %d vendor inputs)" % (RECIPE, len(recipe["files"]) + 1, len(recipe["vendorInputs"])))
        return 0
    if a.report:
        for line in report_text(design, reports):
            print(line)
        return 0
    committed = open(RECIPE, "rb").read().decode("utf-8") if os.path.isfile(RECIPE) else ""
    ok = committed == text
    print("RECIPE CHECK %s: %s %s the regeneration from the design and the installed vendor files"
          % ("PASS" if ok else "FAIL", RECIPE, "equals" if ok else "DIFFERS FROM"))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
