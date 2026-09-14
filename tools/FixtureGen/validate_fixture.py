#!/usr/bin/env python
"""Adversarial offline validation of the authored fixtures. ASCII only.

Two gates:
  check()          - the 5.0.2 authored Tank Platoon fixtures (unchanged since
                     2026-07-21; the default when the script is run with no args).
  check_empty_52() - the EMPTY 5.2-native fixture built by
                     `build_fixture.py --profile 5.2 --empty`
                     (docs/experiments/FIXTURE_52_EMPTY_2026-09-04.md).
Both .oob object-type syntaxes are accepted by the shared parser in build_fixture
(5.0.2 nested `(object-type  1 (17 0 0 2 0 0 0))`, 5.2 flat `(object-type 17 ...)`).
"""
import math
import os
import re
import sys
import zipfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_fixture as bf  # noqa: E402

SEXPR = (".scn", ".oob", ".xtr", ".orb", ".pln", ".omp")


def paren_balance(text):
    """Quote-aware paren balance. Returns (net, min_depth)."""
    depth, mind, instr = 0, 0, False
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if instr:
            if c == "\\":
                i += 2
                continue
            if c == '"':
                instr = False
        elif c == '"':
            instr = True
        elif c == "(":
            depth += 1
        elif c == ")":
            depth -= 1
            mind = min(mind, depth)
        i += 1
    return depth, mind, instr


def nonascii(text):
    return [(i, ord(ch)) for i, ch in enumerate(text) if ord(ch) > 126 or (ord(ch) < 9)]


def check(path):
    print("=" * 76)
    print("VALIDATE", path)
    print("=" * 76)
    ok = True
    members = {}
    with zipfile.ZipFile(path) as z:
        for name in z.namelist():
            raw = z.read(name)
            members[name] = raw
            if name.endswith(SEXPR):
                txt = raw.decode("utf-8", "replace")
                net, mind, instr = paren_balance(txt)
                na = nonascii(txt)
                status = "OK" if (net == 0 and mind == 0 and not instr and not na) else "FAIL"
                if status == "FAIL":
                    ok = False
                print("  %-34s paren net=%d min=%d openstr=%s nonascii=%d  [%s]"
                      % (name, net, mind, instr, len(na), status))
                if na:
                    print("      first nonascii:", na[:5])

    oob = next(v.decode("utf-8", "replace") for k, v in members.items() if k.endswith(".oob"))
    pln = next(v.decode("utf-8", "replace") for k, v in members.items() if k.endswith(".pln"))
    omp = next(v.decode("utf-8", "replace") for k, v in members.items() if k.endswith(".omp"))

    # aggregate own uuid = the class-3 block's uuid
    agg_uuid = None
    for m in re.finditer(r"\(local-vrf-object", oob):
        pass
    m = re.search(r'\(object-type\s+3\s+\(11 1 225 3 2 0 0\)\)', oob)
    # find the uuid nearest before the header object-type-3 marking "AR Plt 1"
    hm = re.search(r'marking-text "AR Plt 1".*?\(uuid\s+"(VRF_UUID:[0-9a-f-]+)"\)', oob, re.S)
    agg_uuid = hm.group(1) if hm else None
    print("  aggregate uuid           :", agg_uuid)

    # plan checks
    pn = re.search(r'\(plan-name\s+"(VRF_UUID:[0-9a-f-]+)"\)', pln)
    rt = re.search(r'\(route\s+"(VRF_UUID:[0-9a-f-]+)"\)', pln)
    tt = re.search(r'\(task-type "([^"]+)"\)', pln)
    trg = re.search(r'\(triggers\s*\)', pln)
    print("  plan-name                :", pn.group(1) if pn else None,
          "== agg?", (pn and pn.group(1) == agg_uuid))
    print("  plan task-type           :", tt.group(1) if tt else None)
    print("  plan route ref           :", rt.group(1) if rt else None)
    print("  plan (triggers ) empty   :", bool(trg))
    # route uuid present in oob?
    if rt:
        print("  route uuid in .oob        :", ('(uuid  "%s")' % rt.group(1)) in oob)

    # members parent = aggregate?
    par = re.findall(r'\(parent-name\s+"(VRF_UUID:[0-9a-f-]+)"\)', oob)
    n_par_agg = sum(1 for p in par if p == agg_uuid)
    print("  members parented to agg  :", n_par_agg, "(expect 4)")

    # demo scripted task must be gone
    print("  demo task stripped       :", "test-vehicle-platoon-position-query" not in oob)
    # aggregate task-status-list empty? (no task-status under the aggregate)
    aggblk = re.search(r'marking-text "AR Plt 1".*?(?=\(local-vrf-object|\Z)', oob, re.S)
    ab = aggblk.group(0) if aggblk else ""
    print("  agg has NO task-status   :", "(task-status " not in ab)
    print("  agg aggregate-state      :", (re.search(r'\(aggregate-state\s+(\w+)\)', ab) or ['', '?'])[1]
          if re.search(r'\(aggregate-state\s+(\w+)\)', ab) else "?")
    print("  agg move-along PSR present:",
          "vrf-aggregate-move-along-process-state-repository-default" in ab
          or "aggregated-move-along-process-state-repository-default" in ab)

    # omp entry count
    n_omp = len(re.findall(r"\(map-entry", omp))
    print("  .omp map-entries         :", n_omp, "(expect 9)")
    # every oob object uuid has an omp entry?
    oob_uuids = set(re.findall(r'\(uuid\s+"(VRF_UUID:[0-9a-f-]+)"\)', oob))
    # keep only header uuids (approx: those that also are object identities) - compare omp set subset
    omp_uuids = set(re.findall(r'\(uuid\s+"(VRF_UUID:[0-9a-f-]+)"\)', omp))
    print("  omp uuids not in oob     :", sorted(omp_uuids - oob_uuids), "(expect [])")

    print("  RESULT                   :", "OK" if ok else "FAIL")
    return ok


# ---------------------------------------------------------------------------
# The EMPTY 5.2-native fixture gate.

E2 = bf.E2
A = bf.A


def ecef_to_geodetic(x, y, z):
    lon = math.degrees(math.atan2(y, x))
    p = math.hypot(x, y)
    lat = math.atan2(z, p * (1 - E2))
    h = 0.0
    for _ in range(8):
        n = A / math.sqrt(1 - E2 * math.sin(lat) ** 2)
        h = p / math.cos(lat) - n
        lat = math.atan2(z, p * (1 - E2 * n / (n + h)))
    n = A / math.sqrt(1 - E2 * math.sin(lat) ** 2)
    h = p / math.cos(lat) - n
    return math.degrees(lat), lon, h


def _say(ok_flag, label, detail, good):
    print("  %-40s %-44s [%s]" % (label, detail, "OK" if good else "FAIL"))
    return ok_flag and good


def _info(label, detail):
    """A reported fact that is NOT a pass/fail condition."""
    print("  %-40s %-44s [%s]" % (label, detail, "info"))


def _read(path):
    with open(path, "r", encoding="utf-8", errors="replace") as fh:
        return fh.read()


# Vendor EntityLevel model set shipped with VR-Forces 5.2d. Every override
# under a derived SMS's model-set-directory is diffed against the file at the
# same relative path here (UG52 68.3.1 p1310: "any parameters or files in the
# higher priority SMSs that are also in the lower priority SMSs override
# those in the lower priority SMSs").
VENDOR_ENTITY_LEVEL = r"C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel"


def _iter_files(root):
    """Yield (relpath, abspath) for every regular file under root, relpath
    using forward slashes. Yields nothing if root does not exist."""
    if not os.path.isdir(root):
        return
    for dirpath, _dirs, files in os.walk(root):
        for f in files:
            ap = os.path.join(dirpath, f)
            rp = os.path.relpath(ap, root).replace(os.sep, "/")
            yield rp, ap


def _override_kind(relpath):
    ext = os.path.splitext(relpath)[1].lower()
    if ext == ".ope":
        return "object-parameter file (.ope)"
    if ext == ".sysdef":
        return "system definition (.sysdef)"
    return "vendor artefact"


def _parse_name_value_lines(text):
    """Parse single-line S-expression assignments of the shape `(name value)`
    or `(DtRwType name value)` - the flat form used throughout .ope/.sysdef
    parameter blocks (e.g. `(DtRwReal propagation-box-extent 200.000000)`,
    `(slope-avoidance-factor 1.000000)`). A line that does not close its own
    paren (a nested block opener) is skipped. Returns {name: raw value
    string}; a name repeated on more than one line keeps its last value."""
    out = {}
    for line in text.splitlines():
        s = line.strip()
        if not (s.startswith("(") and s.endswith(")") and len(s) > 2):
            continue
        inner = s[1:-1].strip()
        m = re.match(r"^(?:Dt[A-Za-z]+\s+)?([A-Za-z][\w-]*)\s+(.+)$", inner)
        if m:
            out[m.group(1)] = m.group(2).strip()
    return out


def _diff_overlay_file(derived_abs, vendor_abs):
    """One-line-per-difference summary of a derived overlay file against its
    vendor same-relative-path counterpart, as flat (name value) assignments:
    "name old -> new" for a changed value, "name (absent) -> new" for a name
    the vendor file does not set at all. Falls back to a whole-file note when
    neither side parses into any name/value pairs (e.g. a binary asset)."""
    dtxt = _read(derived_abs)
    if not os.path.isfile(vendor_abs):
        return ["(new file - no vendor counterpart at %s)" % vendor_abs]
    vtxt = _read(vendor_abs)
    dvals = _parse_name_value_lines(dtxt)
    vvals = _parse_name_value_lines(vtxt)
    if not dvals and not vvals:
        return (["(identical to vendor)"] if dtxt == vtxt
                else ["(differs from vendor - not parsed as name/value lines)"])
    diffs = ["%s %s -> %s" % (name, vvals.get(name, "(absent)"), dval)
             for name, dval in dvals.items() if vvals.get(name) != dval]
    return diffs or ["(no differing name/value lines found)"]


def describe_sms(path):
    """Report what a DERIVED simulation model set carries, and gate the parts a
    fixture depends on: the file exists, it INCLUDES a shipped SMS (UG52 68.3.1
    p1310), it names a model-set-directory, and that directory overrides at
    least one vendor artefact of ANY kind - a script under <dir>\\scripts, or
    any file under <dir>\\vrfSim\\** (an object-parameter file (.ope), a
    system definition (.sysdef), or another vendor artefact type). UG52
    68.3.1 p1310 makes the whole model-set-directory tree override-capable,
    not scripts alone; scripts/ and vrfSim/ are the two subtrees actually used
    across the AbstractGraphs/Corridor2000 SMS family. A pointless derived SMS
    that overrides nothing at all is the only FAIL here.

    For every overriding .lua, print the script id taken from its .xml sidecar
    (<myScriptId>, the id the higher-priority SMS supersedes - UG52 68.3.4
    p1313), the useAbstractGraphs setting found in the .lua, and the run-time
    proof line the .lua prints (INFO, not gates - another derived SMS may
    legitimately override a different script). For every overriding file under
    vrfSim/ (.ope, .sysdef, or otherwise), print its kind and a one-line diff
    summary against the vendor file at the same relative path under
    VENDOR_ENTITY_LEVEL (also INFO: the diff is a report, not a gate - the
    only thing gated here is that SOME override exists).

    Returns the ok flag.
    """
    ok = _say(True, "sms file exists on disk", path, os.path.isfile(path))
    if not os.path.isfile(path):
        return ok
    sms_txt = _read(path)
    includes = re.findall(r'\(include\s+"([^"]*)"\s*\)', sms_txt)
    ok = _say(ok, "sms includes another SMS (UG52 68.3.1)",
              ", ".join(includes) or "(none)",
              any(i.lower().endswith(".sms") for i in includes))
    m = re.search(r'\(model-set-directory\s+"([^"]*)"\s*\)', sms_txt)
    mset = m.group(1) if m else None
    ok = _say(ok, "sms model-set-directory", mset or "(absent)", bool(mset))
    if not mset:
        return ok

    mset_dir = os.path.join(os.path.dirname(os.path.abspath(path)), mset)
    scripts_dir = os.path.join(mset_dir, "scripts")
    vrfsim_dir = os.path.join(mset_dir, "vrfSim")
    luas = (sorted(f for f in os.listdir(scripts_dir) if f.lower().endswith(".lua"))
            if os.path.isdir(scripts_dir) else [])
    vrf_overrides = sorted(_iter_files(vrfsim_dir))
    ok = _say(ok, "sms overrides at least one vendor artefact",
              "%d script(s) under %s\\scripts, %d file(s) under %s\\vrfSim"
              % (len(luas), mset, len(vrf_overrides), mset),
              bool(luas) or bool(vrf_overrides))

    for lua in luas:
        stem = os.path.splitext(lua)[0]
        body = _read(os.path.join(scripts_dir, lua))
        xml = os.path.join(scripts_dir, stem + ".xml")
        sid = None
        if os.path.isfile(xml):
            mm = re.search(r"<myScriptId>([^<]*)</myScriptId>", _read(xml))
            sid = mm.group(1) if mm else None
        ok = _say(ok, "  overrides script id",
                  "%s   (from %s)" % (sid or "(no myScriptId in the .xml sidecar)", lua),
                  sid == stem)
        ag = re.findall(r"useAbstractGraphs\s*=\s*(\w+)", body)
        _info("    useAbstractGraphs", ", ".join(ag) or "(not set in this script)")
        proof = re.search(r'printInfo\(\s*"([^"]*override[^"]*)"', body)
        _info("    run-time proof line",
              proof.group(1) if proof
              else "(none - a run cannot prove which copy executed)")

    for relpath, abspath in vrf_overrides:
        kind = _override_kind(relpath)
        vendor_abs = os.path.join(VENDOR_ENTITY_LEVEL, "vrfSim",
                                  relpath.replace("/", os.sep))
        _info("  overrides %s" % kind, "vrfSim/%s" % relpath)
        for d in _diff_overlay_file(abspath, vendor_abs):
            _info("    diff vs vendor", d)

    return ok


def check_empty_52(path, donor=None, frame_mode="fixed-frame-run-to-complete",
                   frame_time=0.033333, aoi=None, terrain=None, sms=None):
    """Validate an EMPTY 5.2 fixture. Returns True/False; prints every check.

    terrain: the Terrain-Database / Gui-Terrain-Database string the fixture is
    EXPECTED to carry. Default = the shipped MAK Earth (online).mtf; a fixture
    built with build_fixture.py --terrain <copy> is validated with the same path.

    sms: the Simulation-Model-Set-Files string the fixture is EXPECTED to carry.
    Default = whatever build_fixture.resolve_sms_52() would write today, i.e. the
    derived abstract-graph SMS when it is on this machine (the 5.2 default since
    2026-09-14 / G7b) and the shipped EntityLevel.sms otherwise. Pass "vendor" to
    validate a fixture deliberately built on the shipped SMS, or the path of
    another derived SMS. A derived SMS is also OPENED and described: its include
    chain and the script ids it overrides.
    """
    aoi = aoi or bf.R9_AOI
    donor = donor or bf.DONORS_52["GroundMovement"]
    print("=" * 76)
    print("VALIDATE (empty 5.2)", path)
    print("=" * 76)
    ok = True

    # parser self-test: BOTH .oob object-type syntaxes must parse identically
    nested = bf.parse_object_type("(object-type  1 (17 0 0 2 0 0 0))")
    flat = bf.parse_object_type("(object-type 17 0 0 2 0 0 0)")
    ok = _say(ok, "object-type parser (nested/flat)",
              "%s / %s" % (nested, flat),
              nested == (1, (17, 0, 0, 2, 0, 0, 0)) and flat == (None, (17, 0, 0, 2, 0, 0, 0)))

    base = os.path.splitext(os.path.basename(path))[0]
    dbase = os.path.splitext(os.path.basename(donor))[0]
    with zipfile.ZipFile(path) as z:
        members = {n: z.read(n) for n in z.namelist()}
    with zipfile.ZipFile(donor) as z:
        dmembers = {n: z.read(n) for n in z.namelist()}

    expect = set(n.replace(dbase, base) for n in dmembers)
    ok = _say(ok, "zip member set == donor's",
              "%d members" % len(members),
              set(members) == expect)
    if set(members) != expect:
        print("      missing:", sorted(expect - set(members)))
        print("      extra  :", sorted(set(members) - expect))

    for name, raw in sorted(members.items()):
        if not name.endswith(SEXPR):
            continue
        txt = raw.decode("utf-8", "replace")
        net, mind, instr = paren_balance(txt)
        na = nonascii(txt)
        ok = _say(ok, "sexpr %s" % name.split(".")[-1],
                  "paren net=%d min=%d openstr=%s nonascii=%d" % (net, mind, instr, len(na)),
                  net == 0 and mind == 0 and not instr and not na)

    oob = members[base + ".oob"].decode("utf-8")
    scn = members[base + ".scn"].decode("utf-8")
    omp = members[base + ".omp"].decode("utf-8")

    # ---- zero simulation objects --------------------------------------------
    blocks = bf.iter_blocks(oob)
    globals_, sims = [], []
    for b in blocks:
        (globals_ if bf.global_object_kind(b) else sims).append(b)
    marks = [re.search(r'marking-text\s+"([^"]*)"', b) for b in globals_]
    ok = _say(ok, "simulation objects in .oob",
              "%d (expect 0); globals %d: %s"
              % (len(sims), len(globals_),
                 ", ".join(m.group(1) for m in marks if m)),
              len(sims) == 0 and len(globals_) >= 1)
    if sims:
        for b in sims[:3]:
            m = re.search(r'marking-text\s+"([^"]*)"', b)
            print("      stray object:", m.group(1) if m else "?",
                  bf.parse_object_type(b)[1])
    kinds = set(bf.global_object_kind(b) for b in globals_)
    ok = _say(ok, "both global singletons present",
              ", ".join(sorted(kinds)),
              kinds == set(bf.GLOBAL_TYPE_PREFIXES.values()))

    # ---- terrain / SMS -------------------------------------------------------
    want_terrain = terrain or bf.TERRAIN_52
    want_sms = bf.resolve_sms_52(sms, verbose=False)
    if terrain and terrain != bf.TERRAIN_52:
        ok = _say(ok, "terrain override exists on disk", terrain, os.path.isfile(terrain))
    if want_sms == bf.SMS_52:
        _info("sms", "the shipped EntityLevel.sms ($(DATA_DIR) macro; no script override)")
    else:
        ok = describe_sms(want_sms) and ok
    for key, want in (("Terrain-Database", want_terrain),
                      ("Gui-Terrain-Database", want_terrain),
                      ("Simulation-Model-Set-Files", want_sms)):
        m = re.search(r"\(" + key + r'\s+"([^"]*)"\)', scn)
        ok = _say(ok, key, m.group(1) if m else "(absent)", bool(m) and m.group(1) == want)

    # ---- frame keys ----------------------------------------------------------
    fm = re.search(r'\(frame-mode\s+"([^"]*)"\)', scn)
    ft = re.search(r"\(frame-time\s+([-+0-9.eE]+)\s*\)", scn)
    ok = _say(ok, "frame-mode", fm.group(1) if fm else "(absent)",
              bool(fm) and fm.group(1) == frame_mode)
    ftv = float(ft.group(1)) if ft else None
    ok = _say(ok, "frame-time", ("%.6f" % ftv) if ft else "(absent)",
              ft is not None and abs(ftv - float(frame_time)) < 1e-9)
    ok = _say(ok, "frame-time non-zero in a fixed-frame mode",
              "mode=%s" % (fm.group(1) if fm else "?"),
              not str(fm and fm.group(1)).startswith("fixed-frame") or (ftv or 0.0) != 0.0)

    # ---- .scn part references resolve ---------------------------------------
    refs = re.findall(r'\((?:Order-Of-Battle|Scenario-Scripts|Orbat|Plan|Overlay|'
                      r'SelectionGroups|Object-Map|Scenario-extras|GuiObserverViews|'
                      r'GuiScenarioSettings)\s+"([^"]+)"\)', scn)
    bad_refs = [r for r in refs if r not in members]
    ok = _say(ok, ".scn part references resolve",
              "%d refs, %d unresolved" % (len(refs), len(bad_refs)),
              len(refs) >= 8 and not bad_refs)
    if bad_refs:
        print("      unresolved:", bad_refs)

    # ---- .omp mirrors the .oob ----------------------------------------------
    oob_uuids = set(bf.own_uuid(b) for b in blocks)
    omp_uuids = set(re.findall(r'\(uuid\s+"(VRF_UUID:[0-9a-f-]+)"\)', omp))
    ok = _say(ok, ".omp uuid set == .oob object set",
              "%d / %d" % (len(omp_uuids), len(oob_uuids)),
              omp_uuids == oob_uuids)

    # ---- no plan, no dangling references to stripped objects ----------------
    pln = members[base + ".pln"].decode("utf-8")
    ok = _say(ok, ".pln has no Plan block", "%d bytes" % len(pln), "(Plan " not in pln)

    doob = dmembers[dbase + ".oob"].decode("utf-8")
    stripped = set(bf.own_uuid(b) for b in bf.iter_blocks(doob)
                   if not bf.global_object_kind(b)) - {None}
    dangling = {}
    for name, raw in members.items():
        if name.endswith(".oob"):
            continue
        hit = set(re.findall(r"VRF_UUID:[0-9a-fA-F-]+",
                             raw.decode("utf-8", "replace"))) & stripped
        if hit:
            dangling[name] = sorted(hit)[:3]
    ok = _say(ok, "no member references a stripped object",
              str(dangling) if dangling else "clean", not dangling)

    # ---- playbox / AOI -------------------------------------------------------
    ex = re.search(r'\(ScenarioExtentInformation\s+"([^"]*)"\)', scn)
    if not ex:
        ok = _say(ok, "ScenarioExtentInformation", "(absent)", False)
    else:
        x, y, zc, r = [float(v) for v in ex.group(1).split(",")]
        lat, lon, h = ecef_to_geodetic(x, y, zc)
        inside = (aoi["lat_min"] <= lat <= aoi["lat_max"]
                  and aoi["lon_min"] <= lon <= aoi["lon_max"])
        _cx, _cy, _cz, want_r = bf.aoi_extent_ecef(aoi)
        ok = _say(ok, "extent centre inside the R9 AOI",
                  "%.4f, %.4f, %.0f m" % (lat, lon, h), inside)
        ok = _say(ok, "extent radius covers the AOI box",
                  "%.1f m (need >= %.1f)" % (r, want_r), r >= want_r - 1.0)

    print("  %-40s %-44s [%s]" % ("RESULT", "", "OK" if ok else "FAIL"))
    return ok


LEGACY_DEFAULTS = [
    r"C:\MAK\vrforces5.0.2\userData\scenarios\TankPltFixture_Sweden.scnx",
    r"C:\MAK\vrforces5.0.2\userData\scenarios\TankPltFixture_Mojave.scnx",
]


if __name__ == "__main__":
    import argparse

    ap = argparse.ArgumentParser(
        description="Offline fixture gates. With no arguments, validates the two "
                    "5.0.2 authored Tank Platoon fixtures exactly as before.")
    ap.add_argument("--legacy", nargs="*", default=None, metavar="SCNX",
                    help="5.0.2 authored fixtures to check (default: %s)"
                         % ", ".join(os.path.basename(p) for p in LEGACY_DEFAULTS))
    ap.add_argument("--empty-52", nargs="*", default=None, metavar="SCNX",
                    help="EMPTY 5.2-native fixtures to check.")
    ap.add_argument("--donor", default=None,
                    help="donor .scnx the --empty-52 fixtures were built from "
                         "(default: the GroundMovement donor in build_fixture).")
    ap.add_argument("--frame-mode", default="fixed-frame-run-to-complete")
    ap.add_argument("--frame-time", default=0.033333, type=float)
    ap.add_argument("--expect-fail", action="store_true",
                    help="negative control: exit 0 only if EVERY fixture FAILS.")
    ap.add_argument("--terrain", default=None, metavar="MTF",
                    help="--empty-52: the terrain the fixtures are EXPECTED to name "
                         "(default: the shipped MAK Earth (online).mtf). Pass the "
                         "navigation-area copy for fixtures built with --terrain.")
    ap.add_argument("--sms", default=None, metavar="SMS",
                    help="--empty-52: the simulation model set the fixtures are "
                         "EXPECTED to name. Default = the build default, i.e. the "
                         "derived abstract-graph SMS when it exists on this machine "
                         "(%s) and the shipped EntityLevel.sms otherwise. Pass "
                         "'vendor' for a fixture deliberately built on the shipped "
                         "SMS, or the path of another derived SMS."
                         % bf.SMS_52_CUSTOM)
    args = ap.parse_args()

    results = []
    if args.empty_52 is None and args.legacy is None:
        for p in LEGACY_DEFAULTS:
            results.append(check(p))
    else:
        for p in (args.legacy if args.legacy is not None else []):
            results.append(check(p))
        for p in (args.empty_52 if args.empty_52 is not None else []):
            results.append(check_empty_52(p, donor=args.donor,
                                          frame_mode=args.frame_mode,
                                          frame_time=args.frame_time,
                                          terrain=args.terrain, sms=args.sms))

    if args.expect_fail:
        good = bool(results) and not any(results)
        print("\nNEGATIVE CONTROL (every fixture must FAIL):",
              "OK" if good else "BROKEN GATE")
        sys.exit(0 if good else 1)
    allok = bool(results) and all(results)
    print("\nALL FIXTURES:", "OK" if allok else "FAIL")
    sys.exit(0 if allok else 1)
