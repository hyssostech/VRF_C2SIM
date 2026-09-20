#!/usr/bin/env python
"""Build the Mojave/Sweden authored Tank Platoon fixture .scnx.

Base   = TropicTortoise.scnx (geocentric MAK Earth Space online + C2simEx.sms).
Graft  = 1 Tank Platoon (USA) aggregate + 4 M1A2 members (cloned from
         testFindTankPlatoonPositions) + 1 route (cloned from MaklandCoordinatedAttack),
         all relocated to the target site; + an auto-run move-along plan.

Every world position (3 kinematics triples/object) is overwritten with the target
ECEF; every orientation-tait-bryan (3/object) with the East-level DIS-Euler at the
site. Asserts exactly 3 of each per object so a bad edit fails loudly. ASCII only.
"""
import math
import os
import re
import shutil
import uuid
import zipfile

# ---------------------------------------------------------------------------
SCEN_DIR = r"C:\MAK\vrforces5.0.2\userData\scenarios"
WORK = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_work")

# Source scenarios (extracted on demand from the MAK install):
#   TropicTortoise                 - base (geocentric globe + C2simEx + globals)
#   testFindTankPlatoonPositions   - Tank Platoon (USA) aggregate + 4 M1A2 clone src
#   MaklandCoordinatedAttack       - authored move-along route (control-measure) src
SRC_SCNX = {
    "TropicTortoise": os.path.join(SCEN_DIR, "TropicTortoise.scnx"),
    "testFindTankPlatoonPositions": os.path.join(
        SCEN_DIR, "developer_toolkit_examples", "luaTerrainReasoningQuery",
        "testFindTankPlatoonPositions.scnx"),
    "MaklandCoordinatedAttack": os.path.join(SCEN_DIR, "MaklandCoordinatedAttack.scnx"),
}


def ensure_sources():
    import zipfile as _zip
    for name, scnx in SRC_SCNX.items():
        dst = os.path.join(WORK, name)
        if os.path.isdir(dst):
            continue
        os.makedirs(dst, exist_ok=True)
        with _zip.ZipFile(scnx) as z:
            z.extractall(dst)


TROPIC = os.path.join(WORK, "TropicTortoise")
TANKPLT = os.path.join(WORK, "testFindTankPlatoonPositions", "testFindTankPlatoonPositions.oob")
MAKLAND = os.path.join(WORK, "MaklandCoordinatedAttack", "MaklandCoordinatedAttack.oob")
OUTDIR = os.path.join(WORK, "fixtures")

AGG_SRC_UUID = "VRF_UUID:6af0c793-0b80-0548-86ac-0f2ffb225828"
ROUTE_SRC_UUID = "VRF_UUID:1244a407-6b1b-4119-953b-46e5e91d0b3d"
MEMBER_MARKINGS = ["M1A2 1", "M1A2 2", "M1A2 3", "M1A2 4"]
# member ENU offsets from leader (East, North) metres; a small ~40 m cluster
MEMBER_ENU = [(0.0, 0.0), (35.0, -10.0), (-35.0, -10.0), (0.0, -40.0)]

SITES = {
    "Sweden": dict(lat=58.702956, lon=16.499229, h=51.0, base="TankPltFixture_Sweden"),
    "Mojave": dict(lat=34.612956, lon=-116.600487, h=1041.0, base="TankPltFixture_Mojave"),
    # Branch-B confound control (2026-07-22): IDENTICAL to Mojave except the route
    # anchor is BELOW terrain instead of terrain+150 m. route_alt_msl=100.0 puts the
    # 300 m eastward route at 100 m MSL, ~941 m BELOW the 1041 m Mojave surface -
    # mirroring the C++ original's "route vertices 100 MSL" convention so the per-vertex
    # ground clamp is a clamp-UP (the documented R9 empty-offset-route failure case).
    # Single variable vs Mojave: route waypoint altitude. Structure held IDENTICAL.
    "Mojave_BelowTerrain": dict(lat=34.612956, lon=-116.600487, h=1041.0,
                                route_alt_msl=100.0,
                                base="TankPltFixture_Mojave_BelowTerrain"),
}

# ---------------------------------------------------------------------------
# EXERCISE-CLOCK FRAME SETTINGS (added 2026-09-02 for the fixed-frame probe).
#
# Primary sources (C:\MAK\vrforces5.0.2\doc\VRFUsersGuide.pdf, 5.0.2):
#   sec 12.2.1 p.351-352 - the .scn carries (frame-mode "variable-frame") and
#                          (frame-time 0.100000) as plain top-level parameters.
#   Table 17 p.354       - frame-mode is one of variable-frame / fixed-frame /
#                          fixed-frame-run-to-complete; frame-time is "the length
#                          of a frame, in seconds", and with either fixed-frame
#                          mode "you must set the frame time to a non-zero value.
#                          A value of zero for frame time prevents simulation time
#                          from advancing in either of these modes."
#   sec 3.4.3 p.122-123  - what the three modes mean.
#   include\vrfcgf\cgf.h:1192-1203 - the same three mode strings in the API.
#
# THE DEFAULT IS None FOR BOTH, meaning "leave the base scenario's two lines
# exactly as they are". That identity default is what keeps every fixture
# generated before 2026-09-02 byte-for-byte reproducible.
FRAME_MODES = ("variable-frame", "fixed-frame", "fixed-frame-run-to-complete")

FRAME_MODE_RE = re.compile(r'(\(frame-mode\s+")[^"]*(")')
FRAME_TIME_RE = re.compile(r'(\(frame-time\s+)[-+0-9.eE]+(\s*\))')


def set_frame_settings(scn, frame_mode=None, frame_time=None):
    """Rewrite (frame-mode ...) / (frame-time ...) in a .scn body.

    Both None -> the text is returned unchanged (identity). Raises rather than
    emitting a scenario whose sim clock cannot advance (Table 17 p.354).
    """
    if frame_mode is not None:
        if frame_mode not in FRAME_MODES:
            raise ValueError("unknown frame-mode %r; legal: %s"
                             % (frame_mode, ", ".join(FRAME_MODES)))
        scn, n = FRAME_MODE_RE.subn(r"\g<1>" + frame_mode + r"\g<2>", scn, count=1)
        assert n == 1, 'no (frame-mode "...") line in the .scn'
    if frame_time is not None:
        ft = float(frame_time)
        if ft <= 0.0 and str(frame_mode or "").startswith("fixed-frame"):
            raise ValueError("frame-time must be non-zero in a fixed-frame mode "
                             "(VRFUsersGuide Table 17 p.354)")
        scn, n = FRAME_TIME_RE.subn(r"\g<1>%.6f\g<2>" % ft, scn, count=1)
        assert n == 1, "no (frame-time ...) line in the .scn"
    return scn


def extract_scnx(name, scnx_path=None):
    """Extract <name>.scnx into _work/<name> once; return that directory."""
    dst = os.path.join(WORK, name)
    if not os.path.isdir(dst):
        src = scnx_path or SRC_SCNX.get(name) or os.path.join(SCEN_DIR, name + ".scnx")
        os.makedirs(dst, exist_ok=True)
        with zipfile.ZipFile(src) as z:
            z.extractall(dst)
    return dst


def build_frame_variant(src_name, out_name, frame_mode=None, frame_time=None,
                        out_dir=None, scenario_name=None):
    """Emit <out_name>.scnx = <src_name>.scnx with ONLY the frame settings moved.

    Every part except the .scn is copied BYTE-FOR-BYTE. The .scn is the base .scn
    with (a) its part-name references retargeted from <src_name> to <out_name> -
    the same rename convention build_site() uses, which is the only .scnx naming
    convention this repo has ever loaded live - and (b) the two frame lines
    rewritten. Nothing else is touched: no uuid, no position, no oob/omp/pln.

    Returns (scnx_path, stage_dir).
    """
    src_dir = extract_scnx(src_name)
    stage = os.path.join(OUTDIR, out_name + "_parts")
    if os.path.exists(stage):
        shutil.rmtree(stage)
    os.makedirs(stage)

    scn = read(os.path.join(src_dir, src_name + ".scn"))
    scn = scn.replace(src_name, out_name)
    if scenario_name is not None:
        scn = re.sub(r'(\(scenario-name\s+")[^"]*(")',
                     r"\g<1>" + scenario_name + r"\g<2>", scn, count=1)
    scn = set_frame_settings(scn, frame_mode, frame_time)

    member_order = []
    for name in sorted(os.listdir(src_dir)):
        outname = name.replace(src_name, out_name)
        member_order.append(outname)
        if outname == out_name + ".scn":
            with open(os.path.join(stage, outname), "w",
                      encoding="utf-8", newline="") as f:
                f.write(scn)
        else:
            shutil.copyfile(os.path.join(src_dir, name),
                            os.path.join(stage, outname))

    target_dir = out_dir or SCEN_DIR
    if not os.path.isdir(target_dir):
        os.makedirs(target_dir)
    scnx_path = os.path.join(target_dir, out_name + ".scnx")
    with zipfile.ZipFile(scnx_path, "w", zipfile.ZIP_DEFLATED) as z:
        for outname in member_order:
            z.write(os.path.join(stage, outname), outname)
    print("BUILT %s" % scnx_path)
    print("  base        = %s" % os.path.join(SCEN_DIR, src_name + ".scnx"))
    print("  frame-mode  = %s" % (frame_mode if frame_mode is not None else "(unchanged)"))
    print("  frame-time  = %s" % (("%.6f" % float(frame_time))
                                  if frame_time is not None else "(unchanged)"))
    print("  parts       = %d (%d copied byte-for-byte)"
          % (len(member_order), len(member_order) - 1))
    return scnx_path, stage


# ---------------------------------------------------------------------------
# WGS84 + orientation (validated in ecef.py / orient.py)
A = 6378137.0
F = 1.0 / 298.257223563
E2 = F * (2 - F)


def geodetic_to_ecef(lat_deg, lon_deg, h):
    lat, lon = math.radians(lat_deg), math.radians(lon_deg)
    sl = math.sin(lat)
    N = A / math.sqrt(1 - E2 * sl * sl)
    return ((N + h) * math.cos(lat) * math.cos(lon),
            (N + h) * math.cos(lat) * math.sin(lon),
            (N * (1 - E2) + h) * sl)


def enu_basis(lat_deg, lon_deg):
    lat, lon = math.radians(lat_deg), math.radians(lon_deg)
    sl, cl, so, co = math.sin(lat), math.cos(lat), math.sin(lon), math.cos(lon)
    E = (-so, co, 0.0)
    N = (-sl * co, -sl * so, cl)
    U = (cl * co, cl * so, sl)
    return E, N, U


def Rz(a): c, s = math.cos(a), math.sin(a); return [[c, -s, 0], [s, c, 0], [0, 0, 1]]
def Ry(a): c, s = math.cos(a), math.sin(a); return [[c, 0, s], [0, 1, 0], [-s, 0, c]]
def Rx(a): c, s = math.cos(a), math.sin(a); return [[1, 0, 0], [0, c, -s], [0, s, c]]
def matmul(P, Q): return [[sum(P[i][k] * Q[k][j] for k in range(3)) for j in range(3)] for i in range(3)]
def transpose(M): return [[M[j][i] for j in range(3)] for i in range(3)]


def ecef_to_ned_dcm(lat, lon):
    sl, cl, so, co = math.sin(lat), math.cos(lat), math.sin(lon), math.cos(lon)
    return [[-sl * co, -sl * so, cl], [-so, co, 0.0], [-cl * co, -cl * so, -sl]]


def dcm_to_tb(R):
    theta = math.asin(max(-1.0, min(1.0, -R[2][0])))
    if abs(math.cos(theta)) > 1e-9:
        psi = math.atan2(R[1][0], R[0][0]); phi = math.atan2(R[2][1], R[2][2])
    else:
        psi = math.atan2(-R[0][1], R[1][1]); phi = 0.0
    return psi, theta, phi


def dis_euler(lat_deg, lon_deg, heading_deg):
    lat, lon = math.radians(lat_deg), math.radians(lon_deg)
    Rb2ned = Rz(math.radians(heading_deg))  # pitch=roll=0
    Rb2ecef = matmul(transpose(ecef_to_ned_dcm(lat, lon)), Rb2ned)
    return dcm_to_tb(Rb2ecef)


# ---------------------------------------------------------------------------
# S-expr block extraction by paren matching (quote-aware)

def iter_blocks(text):
    """Yield each full top-level (local-vrf-object ...) block (quote-aware paren match)."""
    blocks = []
    for m in re.finditer(r"\(local-vrf-object", text):
        start = m.start()
        depth, i, n, instr = 0, start, len(text), False
        while i < n:
            c = text[i]
            if instr:
                if c == '"':
                    instr = False
            elif c == '"':
                instr = True
            elif c == '(':
                depth += 1
            elif c == ')':
                depth -= 1
                if depth == 0:
                    blocks.append(text[start:i + 1])
                    break
            i += 1
    return blocks


def own_uuid(block):
    """The object's OWN uuid = the first (uuid "...") in the block (header, before
    any cross-referencing PSR/parent-name)."""
    m = re.search(r'\(uuid\s+"(VRF_UUID:[0-9a-fA-F-]+)"\)', block)
    return m.group(1) if m else None


def own_class(block):
    m = re.search(r"\(object-type\s+(\d+)\s+\(", block)
    return m.group(1) if m else None


def get_block_by_own_uuid(text, target):
    for b in iter_blocks(text):
        if own_uuid(b) == target:
            return b
    raise RuntimeError("no block whose OWN uuid is %s" % target)


def replace_balanced(block, opener_literal, replacement):
    """Replace the balanced S-expr that starts at opener_literal with replacement."""
    idx = block.find(opener_literal)
    if idx < 0:
        raise RuntimeError("opener %r not found" % opener_literal)
    depth, i, n, instr = 0, idx, len(block), False
    while i < n:
        c = block[i]
        if instr:
            if c == '"':
                instr = False
        elif c == '"':
            instr = True
        elif c == '(':
            depth += 1
        elif c == ')':
            depth -= 1
            if depth == 0:
                return block[:idx] + replacement + block[i + 1:]
        i += 1
    raise RuntimeError("unbalanced for %r" % opener_literal)


NUM = r"(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)"
POS_RE = re.compile(
    r"(\((?:parent-kinematics-state|kinematics-state|local-kinematics-state)\s*\n\s*\(position\s+)"
    + NUM + r"\s+" + NUM + r"\s+" + NUM + r"(\s*\))")
TB_RE = re.compile(r"(\(orientation-tait-bryan\s+)" + NUM + r"\s+" + NUM + r"\s+" + NUM + r"(\s*\))")


def set_positions(block, ecef, expect=3):
    tx, ty, tz = ecef
    cnt = [0]

    def repl(m):
        cnt[0] += 1
        return "%s %.6f %.6f %.6f%s" % (m.group(1), tx, ty, tz, m.group(5))
    out = POS_RE.sub(repl, block)
    if expect is None:
        assert cnt[0] >= 1, "no kinematics positions found"
    else:
        assert cnt[0] == expect, "positions replaced=%d expect=%d" % (cnt[0], expect)
    return out


def set_orientation(block, tb, expect=3):
    a, b, c = tb
    cnt = [0]

    def repl(m):
        cnt[0] += 1
        return "%s %.6f %.6f %.6f%s" % (m.group(1), a, b, c, m.group(5))
    out = TB_RE.sub(repl, block)
    if expect is None:
        assert cnt[0] >= 1, "no orientations found"
    else:
        assert cnt[0] == expect, "orientations replaced=%d expect=%d" % (cnt[0], expect)
    return out


def set_first(block, tag, value):
    """Replace the value of the first (tag  VALUE) scalar occurrence."""
    pat = re.compile(r"(\(" + re.escape(tag) + r"\s+)([^\n)]*?)(\s*\))")
    new, n = pat.subn(lambda m: m.group(1) + value + m.group(3), block, count=1)
    assert n == 1, "tag %s not replaced" % tag
    return new


def read(path):
    with open(path, "r", encoding="utf-8") as f:
        return f.read()


def det_uuid(site, role):
    return "VRF_UUID:" + str(uuid.uuid5(uuid.NAMESPACE_DNS, "vrf-fixture-%s-%s" % (site, role)))


# ---------------------------------------------------------------------------
def build_site(site, cfg, frame_mode=None, frame_time=None, out_dir=None):
    lat, lon, h = cfg["lat"], cfg["lon"], cfg["h"]
    base = cfg["base"]
    E, N, U = enu_basis(lat, lon)
    leader_ecef = geodetic_to_ecef(lat, lon, h)
    tb_east = dis_euler(lat, lon, 90.0)          # units face East, level
    tb_north = dis_euler(lat, lon, 0.0)          # route local frame = NED
    # route anchor altitude: default terrain+150 m (above -> clamp-DOWN = success);
    # a cfg route_alt_msl override sets an ABSOLUTE MSL altitude for the below-terrain
    # confound variant (below -> clamp-UP = the R9 failure case).
    route_alt = cfg.get("route_alt_msl")
    route_alt = (h + 150.0) if route_alt is None else route_alt
    anchor_ecef = geodetic_to_ecef(lat, lon, route_alt)

    agg_uuid = det_uuid(site, "agg")
    route_uuid = det_uuid(site, "route")
    member_uuids = [det_uuid(site, "m%d" % i) for i in range(4)]

    tank_oob = read(TANKPLT)
    makland_oob = read(MAKLAND)

    tank_blocks = iter_blocks(tank_oob)

    # ---- discover members FIRST (needed to remap aggregate-internal refs) ----
    parent_re = re.compile(r'\(parent-name\s+"' + re.escape(AGG_SRC_UUID) + r'"\)')
    src_members = [b for b in tank_blocks
                   if own_class(b) == "1"
                   and "(object-type  1 (1 1 225 1 1 3 0))" in b
                   and parent_re.search(b)]
    assert len(src_members) == 4, "found %d members" % len(src_members)
    old_member_uuids = [own_uuid(b) for b in src_members]
    umap = dict(zip(old_member_uuids, member_uuids))     # old -> new member uuid
    src_all_uuids = [AGG_SRC_UUID] + old_member_uuids

    # ---- aggregate (select by OWN header uuid, not substring) ----
    agg = get_block_by_own_uuid(tank_oob, AGG_SRC_UUID)
    assert own_class(agg) == "3", "aggregate is not class 3"
    # strip demo scripted task AND the baked script-controller run-state that
    # references the luaTerrainReasoningQuery script (absent from C2simEx.sms).
    # Clean form taken from BehaviorGroundAttackByFire's aggregate.
    agg = replace_balanced(agg, "(task-status-list ", "(task-status-list )")
    agg = replace_balanced(agg, "(script-state ", "(script-state )")
    agg = replace_balanced(agg, "(script-information ", "(script-information )")
    assert "test-vehicle-platoon-position-query" not in agg, "demo script still present"
    agg = agg.replace(AGG_SRC_UUID, agg_uuid)            # self-uuid
    for old, new in umap.items():                         # aggregate's member-handle map
        agg = agg.replace(old, new)
    agg = set_first(agg, "object-identifier", '"1:3001:4"')
    agg = set_first(agg, "marking-text", '"AR Plt 1"')
    agg = set_positions(agg, leader_ecef)
    agg = set_orientation(agg, tb_east)

    # ---- members ----
    members = []
    for i, blk in enumerate(src_members):
        su = old_member_uuids[i]
        de, dn = MEMBER_ENU[i]
        mecef = tuple(leader_ecef[k] + de * E[k] + dn * N[k] for k in range(3))
        blk = blk.replace(su, member_uuids[i])          # self uuid
        blk = blk.replace(AGG_SRC_UUID, agg_uuid)        # parent-name -> new aggregate
        blk = set_first(blk, "object-identifier", '"1:3001:%d"' % (5 + i))
        blk = set_first(blk, "marking-text", '"%s"' % MEMBER_MARKINGS[i])
        blk = set_positions(blk, mecef)
        blk = set_orientation(blk, tb_east)
        members.append(blk)

    # ---- route (from Makland; select by OWN uuid) ----
    route = get_block_by_own_uuid(makland_oob, ROUTE_SRC_UUID)
    route = route.replace(ROUTE_SRC_UUID, route_uuid)
    route = set_first(route, "object-identifier", '"1:3001:9"')
    route = set_first(route, "marking-text", '"FixtureRoute"')
    new_verts = ("(body-vertices \n"
                 "               (vertex  0.000000 0.000000 0.000000)\n"
                 "               (vertex  0.000000 150.000000 0.000000)\n"
                 "               (vertex  0.000000 300.000000 0.000000)\n"
                 "            )")
    route = replace_balanced(route, "(body-vertices ", new_verts)
    route = set_positions(route, anchor_ecef, expect=None)
    route = set_orientation(route, tb_north, expect=None)

    # ---- assemble .oob (inject into TropicTortoise order-of-battle) ----
    tropic_oob = read(os.path.join(TROPIC, "TropicTortoise.oob"))
    graft = "\n".join([agg] + members + [route]) + "\n"
    # guard: no source-scenario uuid may survive anywhere in the graft
    for stale in src_all_uuids + [ROUTE_SRC_UUID]:
        assert stale not in graft, "STALE source uuid survived: %s" % stale
    # insert before the final closing paren of (order-of-battle ...)
    rstrip = tropic_oob.rstrip()
    assert rstrip.endswith(")"), "unexpected .oob tail"
    new_oob = rstrip[:-1] + "   " + graft + ")\n"

    # ---- .omp (append 6 map-entries) ----
    omp = read(os.path.join(TROPIC, "TropicTortoise.omp"))
    entries = ""
    for u in [agg_uuid] + member_uuids + [route_uuid]:
        entries += ('      (map-entry \n'
                    '         (address  1 3001)\n'
                    '         (uuid  "%s")\n'
                    '      )\n' % u)
    # insert entries before the object-map closing paren (last two ')')
    i_close = omp.rstrip().rfind(")")               # closes address-map
    i_obj = omp.rstrip()[:i_close].rfind(")")       # closes object-map
    new_omp = omp[:i_obj] + entries + omp[i_obj:]

    # ---- .pln (auto-run move-along, plan-name = aggregate uuid) ----
    new_pln = (
        "(\n"
        '   (Plan-File (version "2.0"))\n'
        "(Plan \n"
        "      (pending-triggers )\n"
        "      (triggers )\n"
        '      (plan-name  "%s")\n'
        "      (ordinal 1)\n"
        "      (plan-variables \n"
        "         (DtRwPlanSimulationObject\n"
        '            (SimulationObject_12345678910  "VRF_UUID:SimulationObject_12345678910"\n'
        "               (title \n"
        "                  (string-queue \n"
        '                     (translate=DtRwTranslatableStringObject "Simulation Object")\n'
        "                  )\n"
        "               )\n"
        '               (simulation-object  "")\n'
        "            )\n"
        "         )\n"
        "         (DtRwPlanSimulationObject\n"
        '            (CreatedObject_12345678910  "VRF_UUID:CreatedObject_12345678910"\n'
        "               (title \n"
        "                  (string-queue \n"
        '                     (translate=DtRwTranslatableStringObject "Created Object")\n'
        "                  )\n"
        "               )\n"
        '               (simulation-object  "")\n'
        "            )\n"
        "         )\n"
        "      )\n"
        "      (Block \n"
        "         (Task \n"
        '            (task-type "move-along")\n'
        "            (subtask False)\n"
        "            (allow-task-visualizations True)\n"
        '            (route  "%s")\n'
        "            (traversal-direction 0)\n"
        "            (start-at-closest-point True)\n"
        "         )\n"
        "      )\n"
        "      (plan-execution-stack \n"
        "      )\n"
        "   )\n"
        ")\n" % (agg_uuid, route_uuid))

    # ---- .scn (keep terrain/model set; retarget member filenames + name) ----
    scn = read(os.path.join(TROPIC, "TropicTortoise.scn"))
    scn = scn.replace("TropicTortoise", base)
    scn = re.sub(r'(\(scenario-name\s+")[^"]*(")',
                 r'\1Tank Platoon fixture %s\2' % site, scn, count=1)
    # Exercise-clock frame settings: identity unless the caller asked otherwise,
    # so every pre-2026-09-02 fixture regenerates byte-identical.
    scn = set_frame_settings(scn, frame_mode, frame_time)

    # ---- write parts + zip ----
    stage = os.path.join(OUTDIR, base + "_parts")
    if os.path.exists(stage):
        shutil.rmtree(stage)
    os.makedirs(stage)
    parts = {}
    for name in os.listdir(TROPIC):
        parts[name.replace("TropicTortoise", base)] = os.path.join(TROPIC, name)
    # overwrite the parts we authored
    authored = {base + ".oob": new_oob, base + ".omp": new_omp,
                base + ".pln": new_pln, base + ".scn": scn}
    member_order = []
    for outname, srcpath in parts.items():
        member_order.append(outname)
        if outname in authored:
            with open(os.path.join(stage, outname), "w", encoding="utf-8", newline="") as f:
                f.write(authored[outname])
        else:
            shutil.copyfile(srcpath, os.path.join(stage, outname))

    target_dir = out_dir or SCEN_DIR
    if not os.path.isdir(target_dir):
        os.makedirs(target_dir)
    scnx_path = os.path.join(target_dir, base + ".scnx")
    with zipfile.ZipFile(scnx_path, "w", zipfile.ZIP_DEFLATED) as z:
        for outname in member_order:
            z.write(os.path.join(stage, outname), outname)

    print("BUILT %s" % scnx_path)
    print("  leader ECEF   = %.3f %.3f %.3f" % leader_ecef)
    print("  east tb       = %.6f %.6f %.6f" % tb_east)
    print("  agg uuid      = %s (oid 1:3001:4)" % agg_uuid)
    print("  member uuids  = %s" % ", ".join(u.split(":")[1][:8] for u in member_uuids))
    print("  route uuid    = %s (oid 1:3001:9)" % route_uuid)
    print("  src members   = %s" % ", ".join(own_uuid(b).split(":")[1][:8] for b in src_members))
    return scnx_path


# ---------------------------------------------------------------------------
# PROFILE 5.2 - the EMPTY 5.2-native fixture (R1 of
# docs/experiments/RESEARCH_52_FIXTURE_FORMAT_2026-09-04.md sec 5).
#
# Everything below is a SEPARATE, EXPLICIT code path selected by --profile 5.2
# --empty. Nothing in the 5.0.2 path above is touched, and there is no implicit
# version detection anywhere: the profile is always named on the command line.
#
# Sources (all verified on disk 2026-09-04; see the research note for the walk):
#   donors   C:\MAK\vrforces5.2d\userData\scenarios\Sample\VR-TheWorld_Online\
#            {GroundMovement,Weather,BehaviorGroundAttackByFire}.scnx - the three
#            5.2-NATIVE-saved samples that already carry MAK Earth (online) +
#            EntityLevel.sms and the flat (object-type k k k k k k k) syntax.
#            GroundMovement is the default: its .pln (36 B), .osrx, .sgr, .ovl and
#            .spt are already the EMPTY stubs, so only .scn/.oob/.omp/.gui_settings
#            need authoring. The donor is opened READ-ONLY and never modified.
#   terrain  "MAK Earth (online).mtf", listed 2026-09-04 in
#            C:\MAK\SharedData\19\latest\TerrainData\TerrainConfiguration\ .
#            It is the Y-7 ruling and the only shipped family covering the R9 AOI
#            (RESEARCH sec 2; "MAK Earth Space (online).mtf" is ABSENT from 5.2d).
#   SMS      the DERIVED SMS SMS_52_CUSTOM by default since 2026-09-14 (G7b); the
#            shipped $(DATA_DIR)\simulationModelSets\EntityLevel.sms (UG52 Table 20
#            p.354; there is no C2simEx.sms under 5.2d - DIFF C2 / Y-8) with
#            --sms vendor / --no-custom-sms. See SMS_52_CUSTOM below.
#   frame    frame-mode / frame-time, UG52 Table 20 p.354 + sec 3.4.3 p.122 (Y-9);
#            the keys are UNCHANGED from 5.0.2, so set_frame_settings() is reused.
#
# SANCTIONED DEPLOY (documented, NOT executed by the offline builder):
#   python build_fixture.py --profile 5.2 --empty \
#          --out-dir "C:\MAK\vrforces5.2d\userData\scenarios"
# The default --out-dir for --empty is tools/FixtureGen/frame_variants/, i.e. the
# builder writes nothing under C:\MAK unless that path is passed explicitly.

SCEN_DIR_52 = r"C:\MAK\vrforces5.2d\userData\scenarios"
FRAME_VARIANTS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                  "frame_variants")

DONORS_52 = {
    "GroundMovement": os.path.join(SCEN_DIR_52, "Sample", "VR-TheWorld_Online",
                                   "GroundMovement.scnx"),
    "Weather": os.path.join(SCEN_DIR_52, "Sample", "VR-TheWorld_Online",
                            "Weather.scnx"),
    "BehaviorGroundAttackByFire": os.path.join(SCEN_DIR_52, "Sample",
                                               "VR-TheWorld_Online",
                                               "BehaviorGroundAttackByFire.scnx"),
}

TERRAIN_52 = (r"$(SHARED_DATA_DIR)\TerrainData\TerrainConfiguration"
              r"\MAK Earth (online).mtf")
SMS_52 = r"$(DATA_DIR)\simulationModelSets\EntityLevel.sms"

# The DEFAULT Simulation-Model-Set-Files for generated 5.2 fixtures since
# 2026-09-14 (G7b): a derived SMS that INCLUDES the shipped EntityLevel.sms
# unchanged (UG52 68.3.1 p1310) and overrides exactly ONE system script by script
# id - scripts/ground-vehicle-move-to.lua - whose only functional change is
# useAbstractGraphs = true, plus one printInfo line that proves at run time which
# copy executed (UG52 68.3.3 p1312 priority; 68.3.4 p1313 "scripts in the highest
# priority SMS supersede those in the lower priority SMSs").
#
# WHY IT IS THE DEFAULT AND NOT AN OPT-IN: the vendor's ground-vehicle-move-to.lua
# hard-codes useAbstractGraphs = false, and with that setting the nav-mesh query
# REFUSES a leg beyond roughly 2 km on a large sectorised navigation area (G7
# attempt 4, run 20260914T154243Z: legs up to 1,994 m planned 4/4, a 4,914 m leg
# refused 4/4). With the override the same legs plan 8/8 (G7b). Every fixture this
# path builds exists to be driven by multi-kilometre C2SIM order legs, so the
# override is the fixture's normal state and the vendor script is the opt-out.
#
# It is named by ABSOLUTE path - allowed for MTL filename parameters, which are
# "an absolute path or a path relative to the directory in which the executable is
# located" (UG52 Table 15 p271) - so the SMS lives OUTSIDE C:\MAK and survives a
# vendor reinstall. If the file is absent (a machine that has not got it) the
# builder falls back to the shipped SMS and says so on stdout; it never fails.
SMS_52_CUSTOM = r"C:\C2SIM\vrf-sms\C2SIM_EntityLevel_AbstractGraphs.sms"

# --sms values that mean "the shipped EntityLevel.sms, no script override" - the
# same as --no-custom-sms. Anything else is taken as a path to a derived SMS.
SMS_VENDOR_ALIASES = ("vendor", "shipped", "entitylevel", "none")


def resolve_sms_52(sms, verbose=True):
    """Return the Simulation-Model-Set-Files string a 5.2 fixture should carry.

    sms None           -> SMS_52_CUSTOM when it exists on disk, else SMS_52.
    sms a vendor alias -> SMS_52, the shipped EntityLevel.sms.
    anything else      -> returned UNCHANGED, so a fixture built with an explicit
                          --sms <path> is byte-identical to one built before this
                          default existed. The caller still checks that it exists.
    """
    if sms is None:
        if os.path.isfile(SMS_52_CUSTOM):
            return SMS_52_CUSTOM
        if verbose:
            print("NOTE: %s is ABSENT - falling back to the shipped SMS. The fixture "
                  "will NOT carry the abstract-graph override, so nav-mesh legs beyond "
                  "~2 km will be refused on a sectorised area (G7b)." % SMS_52_CUSTOM)
        return SMS_52
    if sms.strip().lower() in SMS_VENDOR_ALIASES:
        return SMS_52
    return sms


# R9 Mojave AOI - data\R9_Mojave_Initialization.xml (58 pts) + _UnitMove_Order.xml (6).
# AN AO-SPECIFIC DEFAULT, and marked as one since STP-802. It is the .scn's
# ScenarioExtentInformation - the playbox the front end frames and the scenario claims as its
# own ground - so a fixture built for another AO with this left in place names CALIFORNIA.
# Override with --aoi lat_min,lat_max,lon_min,lon_max[,h]; the default is unchanged, which is
# what keeps every existing fixture byte-identical (the README's reproducibility property).
R9_AOI = dict(lat_min=34.5605, lat_max=34.6696,
              lon_min=-116.7127, lon_max=-116.3867, h=1041.0)

# Named boxes, so a fixture can be built without retyping six decimals. The Suwalki values
# cover BOTH nav tiles of scratchpad/validation/suwalki_nav_plan.md sec 1.3 (SuwalkiN20 +
# SuwalkiS20), and h is that plan's measured 120-165 m terrain rounded to its own corner
# height. Adding a name here changes NOTHING unless it is asked for by --aoi.
NAMED_AOI = {
    "r9-mojave": R9_AOI,
    "suwalki": dict(lat_min=54.0000, lat_max=54.2353,
                    lon_min=23.1808, lon_max=23.7954, h=150.0),
}


def parse_aoi(spec):
    """'lat_min,lat_max,lon_min,lon_max[,h]' or a NAMED_AOI key -> the aoi dict.

    Validated rather than trusted: a transposed pair or a swapped lat/lon is the whole failure
    mode this option exists to prevent, and it would otherwise show up as a plausible-looking
    extent on the other side of the planet.
    """
    if spec is None:
        return None
    key = spec.strip().lower()
    if key in NAMED_AOI:
        return dict(NAMED_AOI[key])
    parts = [p.strip() for p in spec.split(",")]
    if len(parts) not in (4, 5):
        raise SystemExit("--aoi wants lat_min,lat_max,lon_min,lon_max[,h] or one of %s; got %r"
                         % (", ".join(sorted(NAMED_AOI)), spec))
    try:
        v = [float(p) for p in parts]
    except ValueError:
        raise SystemExit("--aoi values must be numbers; got %r" % spec)
    aoi = dict(lat_min=v[0], lat_max=v[1], lon_min=v[2], lon_max=v[3],
               h=(v[4] if len(v) == 5 else 0.0))
    if not (-90.0 <= aoi["lat_min"] < aoi["lat_max"] <= 90.0):
        raise SystemExit("--aoi latitudes must satisfy -90 <= lat_min < lat_max <= 90; got "
                         "%r..%r" % (aoi["lat_min"], aoi["lat_max"]))
    if not (-180.0 <= aoi["lon_min"] < aoi["lon_max"] <= 180.0):
        raise SystemExit("--aoi longitudes must satisfy -180 <= lon_min < lon_max <= 180; got "
                         "%r..%r" % (aoi["lon_min"], aoi["lon_max"]))
    if not (-500.0 <= aoi["h"] <= 9000.0):
        raise SystemExit("--aoi h must be a WGS84 height in metres (-500..9000); got %r"
                         % aoi["h"])
    return aoi


def aoi_selftest():
    """OFFLINE gate for --aoi: writes nothing, reads nothing, launches nothing."""
    bad = 0

    def check(ok, what):
        nonlocal bad
        print("  [%s] %s" % ("PASS" if ok else "FAIL", what))
        if not ok:
            bad += 1

    print("--- --aoi parsing and the extent arithmetic (STP-802) ---")
    check(parse_aoi(None) is None, "no --aoi means the default is used, untouched")
    check(parse_aoi("r9-mojave") == R9_AOI, "the named default round-trips to R9_AOI")
    ref = aoi_extent_ecef(R9_AOI)
    check(aoi_extent_ecef(parse_aoi("r9-mojave")) == ref,
          "and produces the SAME ScenarioExtentInformation - existing fixtures are unchanged")
    check(aoi_extent_ecef(parse_aoi(
              "34.5605,34.6696,-116.7127,-116.3867,1041.0")) == ref,
          "so does spelling the Mojave box out by hand")
    suw = parse_aoi("suwalki")
    check(suw["lat_min"] == 54.0 and suw["lat_max"] == 54.2353
          and suw["lon_min"] == 23.1808 and suw["lon_max"] == 23.7954 and suw["h"] == 150.0,
          "the suwalki box covers both nav tiles (54.0000..54.2353 N, 23.1808..23.7954 E, h 150)")
    sx, sy, sz, sr = aoi_extent_ecef(suw)
    check(sx > 0 and sy > 0 and sz > 0 and 20000.0 < sr < 40000.0,
          "its extent is a positive-octant ECEF centre with a %.0f m radius (Poland, not "
          "California)" % sr)
    check(ref[3] > 0 and (sx - ref[0]) ** 2 > 1e12,
          "and it is nowhere near the Mojave centre")
    for bogus in ("34.6696,34.5605,-116.7127,-116.3867",     # latitudes transposed
                  "34.5605,34.6696,-116.3867,-116.7127",     # longitudes transposed
                  "91,92,0,1", "0,1,-181,-179", "0,1,2,3,99999",
                  "1,2,3", "a,b,c,d", "nope"):
        try:
            parse_aoi(bogus)
            check(False, "REFUSED %r" % bogus)
        except SystemExit:
            check(True, "REFUSED %r" % bogus)
    print("AOI SELFTEST %s (%d problem(s))" % ("PASS" if bad == 0 else "FAIL", bad))
    return 0 if bad == 0 else 1

# The two GLOBAL singletons a scenario keeps when every simulation object is
# stripped. Matched on the FIRST SIX object-type fields, because the 7th differs
# across versions (5.0.2 GlobalEnv = 21 0 0 1 0 0 0, 5.2 = 21 0 0 1 0 0 1).
# Matching on the KIND alone would be WRONG: Weather.scnx carries ordinary weather
# REGION objects of type (21 0 0 2 0 0 1) that are simulation objects, not globals.
GLOBAL_TYPE_PREFIXES = {
    (105, 105, 105, 105, 105, 105): "global dynamic terrain damage",
    (21, 0, 0, 1, 0, 0): "global environment",
}

# Both .oob object-type syntaxes (RESEARCH sec 1):
#   5.0.2 nested  (object-type  1 (17 0 0 2 0 0 0))   <- class prefix + 7-tuple
#   5.2   flat    (object-type 17 0 0 2 0 0 0)        <- 7-tuple only
OBJTYPE_NESTED_RE = re.compile(
    r"\(object-type\s+(\d+)\s+\(\s*(\d+(?:\s+\d+){6})\s*\)\s*\)")
OBJTYPE_FLAT_RE = re.compile(r"\(object-type\s+(\d+(?:\s+\d+){6})\s*\)")


def parse_object_type(block):
    """Return (class_or_None, 7-tuple_or_None) for either .oob syntax."""
    m = OBJTYPE_NESTED_RE.search(block)
    if m:
        return int(m.group(1)), tuple(int(x) for x in m.group(2).split())
    m = OBJTYPE_FLAT_RE.search(block)
    if m:
        return None, tuple(int(x) for x in m.group(1).split())
    return None, None


def global_object_kind(block):
    """Name of the global singleton this block is, or None if it is a sim object."""
    _cls, t = parse_object_type(block)
    if t is None:
        return None
    return GLOBAL_TYPE_PREFIXES.get(t[:6])


def block_spans(text):
    """Like iter_blocks() but returns (start, end) spans of each top-level block."""
    spans = []
    for m in re.finditer(r"\(local-vrf-object", text):
        start = m.start()
        depth, i, n, instr = 0, start, len(text), False
        while i < n:
            c = text[i]
            if instr:
                if c == '"':
                    instr = False
            elif c == '"':
                instr = True
            elif c == '(':
                depth += 1
            elif c == ')':
                depth -= 1
                if depth == 0:
                    spans.append((start, i + 1))
                    break
            i += 1
    return spans


def strip_oob_to_globals(oob):
    """Return (new_oob, kept_uuids, dropped_uuids, kept_names).

    Keeps the .oob prefix/separator/suffix glue byte-for-byte so the result is
    the donor file with non-global (local-vrf-object ...) blocks removed.
    """
    spans = block_spans(oob)
    assert spans, "no (local-vrf-object ...) blocks in the .oob"
    prefix = oob[:spans[0][0]]
    suffix = oob[spans[-1][1]:]
    sep = oob[spans[0][1]:spans[1][0]] if len(spans) > 1 else "\n   "
    kept, kept_names, dropped = [], [], []
    for s, e in spans:
        blk = oob[s:e]
        u = own_uuid(blk)
        name = global_object_kind(blk)
        if name:
            kept.append(blk)
            kept_names.append((name, u))
        else:
            dropped.append(u)
    assert kept, "donor .oob has no global singletons to keep"
    return prefix + sep.join(kept) + suffix, [u for _n, u in kept_names], dropped, kept_names


# NOTE the leading [ \t]* rather than \n: entries are back-to-back, so a pattern
# that ate BOTH the leading and the trailing newline could only match every other
# entry (23 of GroundMovement's 45). The independent (map-entry count below is the
# tripwire for that class of bug.
OMP_ENTRY_RE = re.compile(
    r"[ \t]*\(map-entry[ \t]*\n"
    r"[ \t]*\(address[^)]*\)[ \t]*\n"
    r'[ \t]*\(uuid[ \t]+"(VRF_UUID:[0-9a-fA-F-]+)"\)[ \t]*\n'
    r"[ \t]*\)[ \t]*\n")


def strip_omp_to(omp, keep_uuids):
    """Drop every (map-entry ...) whose uuid is not in keep_uuids."""
    keep = set(keep_uuids)
    found = [m.group(1) for m in OMP_ENTRY_RE.finditer(omp)]
    assert found, "no (map-entry ...) parsed out of the .omp"
    assert len(found) == omp.count("(map-entry"), \
        "regex saw %d of %d (map-entry blocks" % (len(found), omp.count("(map-entry"))
    out = OMP_ENTRY_RE.sub(lambda m: m.group(0) if m.group(1) in keep else "", omp)
    left = [m.group(1) for m in OMP_ENTRY_RE.finditer(out)]
    assert len(left) == out.count("(map-entry"), "post-strip .omp scan is incomplete"
    assert set(left) == keep and len(left) == len(keep), \
        "omp keep-set mismatch: %s vs %s" % (sorted(left), sorted(keep))
    return out, found


# The canonical EMPTY .gui_settings. Structure copied VERBATIM from a shipped 5.2d
# scenario that already has zero object settings (Sample\DroneAttack.gui_settings),
# so the boost class_id numbering is the one VR-Forces itself writes: with
# DtObjectSettings empty, SystemScriptsAvailable is class_id 2 and Overlays 3 - NOT
# 4 and 5 as in a donor that carries object settings. Renumbering by hand is the
# trap here; the donor's Overlays block is spliced in with its ids rewritten.
GUI_SETTINGS_52_HEAD = (
    '<?xml version="1.0" encoding="UTF-8" standalone="yes" ?>\n'
    "<!DOCTYPE boost_serialization>\n"
    '<boost_serialization signature="serialization::archive" version="20">\n'
    '<DtGuiScenarioSettingsManager class_id="0" tracking_level="0" version="0">\n'
    '\t<DtObjectSettings class_id="1" tracking_level="0" version="0">\n'
    "\t\t<count>0</count>\n"
    "\t\t<item_version>0</item_version>\n"
    "\t</DtObjectSettings>\n"
    '\t<SystemScriptsAvailable class_id="2" tracking_level="0" version="0">\n'
    "\t\t<count>0</count>\n"
    "\t\t<item_version>0</item_version>\n"
    "\t</SystemScriptsAvailable>\n")
GUI_SETTINGS_52_TAIL = ("</DtGuiScenarioSettingsManager>\n"
                        "</boost_serialization>\n")


def strip_gui_settings(gui):
    """Empty DtObjectSettings; keep the donor's Overlays block, class ids renumbered."""
    m = re.search(r"\t<Overlays\b.*?</Overlays>\n", gui, re.S)
    assert m, "no <Overlays> block in the donor .gui_settings"
    ov = m.group(0)
    ids = []
    for cm in re.finditer(r'class_id="(\d+)"', ov):
        if cm.group(1) not in ids:
            ids.append(cm.group(1))
    assert len(ids) == 3, "unexpected Overlays class ids %s" % ids
    remap = dict(zip(ids, ["3", "4", "5"]))
    ov = re.sub(r'class_id="(\d+)"',
                lambda cm: 'class_id="%s"' % remap[cm.group(1)], ov)
    out = GUI_SETTINGS_52_HEAD + ov + GUI_SETTINGS_52_TAIL
    assert "VRF_UUID" not in out, "uuid survived the .gui_settings strip"
    return out


def aoi_extent_ecef(aoi):
    """(x, y, z, radius) covering the AOI box - the .scn ScenarioExtentInformation.

    Verified against the donor: GroundMovement's "4.33827e+06,576541,4.62543e+06,
    13563.5" back-converts to 46.78N 7.57E, its own Swiss play area.
    """
    lat = 0.5 * (aoi["lat_min"] + aoi["lat_max"])
    lon = 0.5 * (aoi["lon_min"] + aoi["lon_max"])
    h = aoi["h"]
    c = geodetic_to_ecef(lat, lon, h)
    r = 0.0
    for la in (aoi["lat_min"], aoi["lat_max"]):
        for lo in (aoi["lon_min"], aoi["lon_max"]):
            p = geodetic_to_ecef(la, lo, h)
            r = max(r, math.sqrt(sum((p[k] - c[k]) ** 2 for k in range(3))))
    return c[0], c[1], c[2], r


def set_scn_string(scn, key, value):
    """Rewrite (<key> "...") in a .scn. Raises if the key is absent."""
    pat = re.compile(r"(\(" + re.escape(key) + r'\s+")[^"]*(")')
    new, n = pat.subn(lambda m: m.group(1) + value + m.group(2), scn, count=1)
    assert n == 1, "no (%s \"...\") line in the .scn" % key
    return new


DETERMINISTIC_ZIP_DATE = (1980, 1, 1, 0, 0, 0)


def write_zip(path, members, deterministic=False):
    """members = list of (arcname, text-or-bytes), written in the order given."""
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as z:
        for arcname, payload in members:
            data = payload.encode("utf-8") if isinstance(payload, str) else payload
            if deterministic:
                zi = zipfile.ZipInfo(arcname, date_time=DETERMINISTIC_ZIP_DATE)
                zi.compress_type = zipfile.ZIP_DEFLATED
                zi.external_attr = 0o600 << 16
                z.writestr(zi, data)
            else:
                z.writestr(arcname, data)


def build_empty_52(out_name, donor="GroundMovement", frame_mode=None,
                   frame_time=None, out_dir=None, scenario_name=None,
                   aoi=None, verbose=True, terrain=None, sms=None):
    """Emit <out_name>.scnx: the donor 5.2-native scenario with EVERY simulation
    object stripped, leaving a globals-only .oob, on MAK Earth (online) +
    EntityLevel.sms with the frame lever set and the extent on the R9 AOI.

    terrain: the Terrain-Database / Gui-Terrain-Database string. Default
    TERRAIN_52 (the shipped online terrain). A COPY of that .mtf carrying
    navigation-area records (tools/navdata/make_nav_terrain.py) is passed
    here by absolute path; the copy is verified to exist on this machine.

    sms: the Simulation-Model-Set-Files string, resolved by resolve_sms_52().
    None (the default) = SMS_52_CUSTOM, the derived abstract-graph SMS, when it
    exists on this machine, else the shipped SMS_52. "vendor" = the shipped
    EntityLevel.sms named through the $(DATA_DIR) macro. Any other value is used
    verbatim: a derived SMS that INCLUDES EntityLevel.sms (UG52 68.3.1 p1310)
    passed by absolute path - allowed for MTL filename parameters, which are "an
    absolute path or a path relative to the directory in which the executable is
    located" (UG52 Table 15 p271) - and verified to exist on this machine.

    Returns (scnx_path, report_dict). The donor .scnx is only READ.
    """
    # THE DEFAULT IS AO-SPECIFIC. Left unset it is the Mojave playbox, which is correct for
    # every fixture built before STP-802 and wrong for every fixture built for another AO -
    # so the chosen box is PRINTED below rather than left to be discovered in the .scn.
    aoi_default = aoi is None
    aoi = aoi or R9_AOI
    terrain = terrain or TERRAIN_52
    if terrain != TERRAIN_52 and not os.path.isfile(terrain):
        raise SystemExit("terrain not found: %s" % terrain)
    sms = resolve_sms_52(sms, verbose=verbose)
    if sms != SMS_52 and not os.path.isfile(sms):
        raise SystemExit("sms not found: %s" % sms)
    donor_path = DONORS_52.get(donor, donor)
    if not os.path.isfile(donor_path):
        raise SystemExit("donor .scnx not found: %s" % donor_path)

    with zipfile.ZipFile(donor_path) as z:
        order = list(z.namelist())
        raw = {n: z.read(n) for n in order}
    dname = os.path.splitext(os.path.basename(donor_path))[0]

    def txt(ext):
        return raw[dname + ext].decode("utf-8")

    # ---- .oob : globals only -------------------------------------------------
    new_oob, kept_uuids, dropped_uuids, kept_names = strip_oob_to_globals(txt(".oob"))
    assert not [b for b in iter_blocks(new_oob) if global_object_kind(b) is None], \
        "a simulation object survived the .oob strip"

    # ---- .omp : one map-entry per surviving global ---------------------------
    new_omp, omp_before = strip_omp_to(txt(".omp"), kept_uuids)

    # ---- .gui_settings : drop the stale per-object settings ------------------
    new_gui = strip_gui_settings(txt(".gui_settings"))

    # ---- members that carry NO reference to a stripped object ---------------
    # .pln/.xtr/.sgr/.ovl/.spt/.osrx/.orb are copied byte-for-byte; assert first
    # that none of them names an object we removed (verified for GroundMovement:
    # its .pln is the empty 36-byte stub and the .xtr's only uuids are the two
    # spawn-TEMPLATE self-ids, whose sink-nodes/spawn-points lists are empty).
    dropped_set = set(u for u in dropped_uuids if u)
    copied = []
    for n in order:
        ext = os.path.splitext(n)[1]
        if ext in (".oob", ".omp", ".gui_settings", ".scn"):
            continue
        body = raw[n].decode("utf-8", "replace")
        stale = sorted(set(re.findall(r"VRF_UUID:[0-9a-fA-F-]+", body)) & dropped_set)
        assert not stale, "%s still references stripped objects: %s" % (n, stale[:3])
        copied.append(n)

    # ---- .scn ----------------------------------------------------------------
    scn = txt(".scn")
    scn = scn.replace(dname, out_name)             # part-name references
    scn = set_scn_string(scn, "Terrain-Database", terrain)
    scn = set_scn_string(scn, "Gui-Terrain-Database", terrain)
    scn = set_scn_string(scn, "Simulation-Model-Set-Files", sms)
    scn = set_frame_settings(scn, frame_mode, frame_time)
    if scenario_name is not None:
        scn = set_scn_string(scn, "scenario-name", scenario_name)
    ex, ey, ez, er = aoi_extent_ecef(aoi)
    extent = "%g,%g,%g,%g" % (ex, ey, ez, er)
    scn = set_scn_string(scn, "ScenarioExtentInformation", extent)
    new_keys = [k for k in ("gui-runtime-scheme", "gui-runtime-scheme-data",
                            "remote-attachment-scheme", "remote-attachment-scheme-data")
                if ("(%s " % k) in scn]

    # ---- assemble ------------------------------------------------------------
    members = []
    for n in order:
        outname = n.replace(dname, out_name)
        ext = os.path.splitext(n)[1]
        if ext == ".scn":
            members.append((outname, scn))
        elif ext == ".oob":
            members.append((outname, new_oob))
        elif ext == ".omp":
            members.append((outname, new_omp))
        elif ext == ".gui_settings":
            members.append((outname, new_gui))
        else:
            members.append((outname, raw[n]))

    target_dir = out_dir or FRAME_VARIANTS_DIR
    if not os.path.isdir(target_dir):
        os.makedirs(target_dir)
    scnx_path = os.path.join(target_dir, out_name + ".scnx")
    write_zip(scnx_path, members, deterministic=True)

    rep = dict(scnx=scnx_path, donor=donor_path, donor_name=dname,
               members=[m for m, _ in members], donor_members=order,
               kept=kept_names, n_dropped=len(dropped_uuids),
               omp_before=len(omp_before), omp_after=len(kept_uuids),
               terrain=terrain, sms=sms, extent=extent, aoi=dict(aoi),
               aoi_is_default=aoi_default,
               frame_mode=frame_mode, frame_time=frame_time, new_52_keys=new_keys)
    if verbose:
        print("BUILT %s" % scnx_path)
        print("  donor         = %s (READ-ONLY)" % donor_path)
        print("  members       = %d (donor %d, %d copied byte-for-byte)"
              % (len(members), len(order), len(copied)))
        print("  .oob globals  = %s" % ", ".join("%s %s" % (n, u.split(":")[1][:8])
                                                 for n, u in kept_names))
        print("  .oob dropped  = %d simulation objects" % len(dropped_uuids))
        print("  .omp entries  = %d -> %d" % (len(omp_before), len(kept_uuids)))
        print("  terrain       = %s%s" % (terrain, "" if terrain == TERRAIN_52
                                          else "  (OVERRIDE - not the shipped terrain)"))
        if sms == SMS_52:
            sms_note = "  (the shipped SMS - no script override)"
        elif sms == SMS_52_CUSTOM:
            sms_note = "  (DEFAULT since G7b - includes EntityLevel.sms, abstract graphs on)"
        else:
            sms_note = "  (OVERRIDE - not the shipped SMS)"
        print("  sms           = %s%s" % (sms, sms_note))
        print("  frame-mode    = %s" % (frame_mode if frame_mode else "(unchanged)"))
        print("  frame-time    = %s" % (("%.6f" % float(frame_time))
                                        if frame_time is not None else "(unchanged)"))
        print("  aoi           = %.4f..%.4f N, %.4f..%.4f E, h %.1f m%s"
              % (aoi["lat_min"], aoi["lat_max"], aoi["lon_min"], aoi["lon_max"], aoi["h"],
                 "  (DEFAULT - the R9 MOJAVE playbox; pass --aoi for another AO)"
                 if aoi_default else "  (--aoi)"))
        print("  extent        = %s" % extent)
        print("  5.2 .scn keys = %s" % (", ".join(new_keys) or "(none)"))
    return scnx_path, rep


def build_empty_52_negative_controls(out_name, out_dir, donor="GroundMovement",
                                     frame_mode=None, frame_time=None,
                                     scenario_name=None, sms=None):
    """Two DELIBERATELY BROKEN copies of the empty fixture, for the validator gate.

    (a) _NEG_noframetime : the (frame-time ...) line deleted from the .scn.
    (b) _NEG_strayobject : one real simulation object spliced back into the .oob.
    Both must FAIL validate_fixture.py --empty-52.
    """
    good, _rep = build_empty_52(out_name, donor=donor, frame_mode=frame_mode,
                                frame_time=frame_time, out_dir=out_dir,
                                scenario_name=scenario_name, verbose=False,
                                sms=sms)
    donor_path = DONORS_52.get(donor, donor)
    dname = os.path.splitext(os.path.basename(donor_path))[0]
    with zipfile.ZipFile(donor_path) as z:
        donor_oob = z.read(dname + ".oob").decode("utf-8")
    stray = next(b for b in iter_blocks(donor_oob) if global_object_kind(b) is None)

    with zipfile.ZipFile(good) as z:
        order = list(z.namelist())
        raw = {n: z.read(n) for n in order}

    out = []
    for suffix, mutate in (("_NEG_noframetime", "frametime"),
                           ("_NEG_strayobject", "stray")):
        name = out_name + suffix
        members = []
        for n in order:
            outname = n.replace(out_name, name)
            body = raw[n]
            if n.endswith(".scn"):
                # retarget the part-name references too, so the control carries
                # EXACTLY ONE fault and not a second (dangling .scn refs).
                body = body.decode("utf-8").replace(out_name, name).encode("utf-8")
            if mutate == "frametime" and n.endswith(".scn"):
                t = body.decode("utf-8")
                t, k = re.subn(r"[ \t]*\(frame-time[^\n]*\n", "", t, count=1)
                assert k == 1, "no (frame-time ...) line to delete"
                body = t.encode("utf-8")
            elif mutate == "stray" and n.endswith(".oob"):
                t = body.decode("utf-8")
                cut = t.rstrip().rfind(")")
                body = (t[:cut] + "\n   " + stray + "\n" + t[cut:]).encode("utf-8")
            members.append((outname, body))
        p = os.path.join(out_dir, name + ".scnx")
        write_zip(p, members, deterministic=True)
        print("BUILT NEGATIVE CONTROL %s (%s)" % (p, mutate))
        out.append(p)
    return out


if __name__ == "__main__":
    import argparse
    import sys

    ap = argparse.ArgumentParser(
        formatter_class=argparse.RawDescriptionHelpFormatter,
        description="Build the authored Tank Platoon fixtures, a frame-mode "
                    "variant of an existing .scnx, or (--profile 5.2 --empty) the "
                    "EMPTY 5.2-native fixture.",
        epilog="EMPTY 5.2 FIXTURE (R1)\n"
               "  Build into the repo (default, writes nothing under C:\\MAK):\n"
               "    python build_fixture.py --profile 5.2 --empty \\\n"
               "           --frame-mode fixed-frame-run-to-complete --frame-time 0.033333\n"
               "  SANCTIONED DEPLOY (a live executor runs this, not the offline builder):\n"
               "    python build_fixture.py --profile 5.2 --empty \\\n"
               "           --frame-mode fixed-frame-run-to-complete --frame-time 0.033333 \\\n"
               '           --out-dir "C:\\MAK\\vrforces5.2d\\userData\\scenarios"\n'
               "  Then: LaunchVrf52.ps1 -Scenario R9_Mojave_Empty_52\n"
               "  The SMS defaults to the abstract-graph derived SMS when present:\n"
               "    " + SMS_52_CUSTOM + "\n"
               "  --no-custom-sms (= --sms vendor) writes the shipped EntityLevel.sms.\n")
    ap.add_argument("sites", nargs="*",
                    help="site(s) to build; default = all of %s" % ", ".join(SITES))
    ap.add_argument("--frame-mode", default=None, choices=list(FRAME_MODES),
                    help="(frame-mode \"...\") to write into the .scn. Omit to leave "
                         "the base scenario's line untouched (the default; keeps "
                         "existing fixtures byte-identical).")
    ap.add_argument("--frame-time", default=None, type=float,
                    help="(frame-time <s>) to write into the .scn. Omit to leave the "
                         "base scenario's line untouched. Must be non-zero with either "
                         "fixed-frame mode (VRFUsersGuide Table 17 p.354).")
    ap.add_argument("--out-dir", default=None,
                    help="directory to write the .scnx into. Default: %s . Point this "
                         "at a scratch directory to build WITHOUT writing under C:\\MAK."
                         % SCEN_DIR)
    ap.add_argument("--frame-variant", default=None, metavar="SRC:OUT",
                    help="instead of building a site, emit OUT.scnx = SRC.scnx with "
                         "ONLY --frame-mode / --frame-time changed (every other part "
                         "copied byte-for-byte). Example: TropicTortoise:TropicTortoise_FFRTC")
    ap.add_argument("--scenario-name", default=None,
                    help="optional (scenario-name \"...\") for --frame-variant "
                         "and for --empty.")
    ap.add_argument("--profile", default="5.0.2", choices=["5.0.2", "5.2"],
                    help="which VR-Forces generation to build for. EXPLICIT ONLY - "
                         "nothing in this script sniffs the version. Default 5.0.2 "
                         "(every pre-existing behaviour).")
    ap.add_argument("--empty", action="store_true",
                    help="build the EMPTY fixture: a 5.2-native donor with every "
                         "simulation object stripped (globals-only .oob). Requires "
                         "--profile 5.2.")
    ap.add_argument("--donor", default="GroundMovement",
                    help="--empty donor: %s, or a path to a .scnx. Default "
                         "GroundMovement." % ", ".join(sorted(DONORS_52)))
    ap.add_argument("--out-name", default="R9_Mojave_Empty_52",
                    help="--empty output base NAME STEM (default R9_Mojave_Empty_52 - an "
                         "AO-specific default). Pair it with --aoi for another AO, e.g. "
                         "--aoi suwalki --out-name Suwalki_Empty_52_Nav_AG.")
    ap.add_argument("--aoi", default=None, metavar="BOX",
                    help="--empty only: the .scn ScenarioExtentInformation playbox, as "
                         "lat_min,lat_max,lon_min,lon_max[,h] or a named box (%s). DEFAULT: the "
                         "R9 MOJAVE box - so a fixture for another AO built without this option "
                         "claims California as its ground. Omitting it keeps every existing "
                         "fixture byte-identical." % ", ".join(sorted(NAMED_AOI)))
    ap.add_argument("--aoi-selftest", action="store_true",
                    help="offline gate for --aoi: parse + extent arithmetic, writes nothing.")
    ap.add_argument("--negative-controls", default=None, metavar="DIR",
                    help="--empty only: also write two DELIBERATELY BROKEN copies "
                         "(missing frame-time; a stray simulation object) into DIR, "
                         "for the validator's negative gate. Never point this at a "
                         "tracked directory.")
    ap.add_argument("--terrain", default=None, metavar="MTF",
                    help="--empty only: Terrain-Database / Gui-Terrain-Database to "
                         "write instead of the shipped MAK Earth (online).mtf - e.g. "
                         "the navigation-area copy made by tools/navdata/"
                         "make_nav_terrain.py (absolute path; must exist).")
    ap.add_argument("--sms", default=None, metavar="SMS",
                    help="--empty only: Simulation-Model-Set-Files to write. DEFAULT "
                         "since 2026-09-14 (G7b) = %s when that file exists - the "
                         "derived SMS that INCLUDES EntityLevel.sms and overrides "
                         "ground-vehicle-move-to.lua with useAbstractGraphs=true. Pass "
                         "'vendor' (same as --no-custom-sms) for the shipped "
                         "EntityLevel.sms, or another derived SMS by absolute path "
                         "(must exist)." % SMS_52_CUSTOM)
    ap.add_argument("--no-custom-sms", action="store_true",
                    help="--empty only: shorthand for --sms vendor. Writes the shipped "
                         "EntityLevel.sms, i.e. the pre-2026-09-14 default - nav-mesh "
                         "legs beyond ~2 km are then refused on a sectorised area.")
    args = ap.parse_args()

    if args.aoi_selftest:
        sys.exit(aoi_selftest())

    if args.aoi and not args.empty:
        raise SystemExit("--aoi is a --profile 5.2 --empty option (the 5.0.2 path takes its "
                         "extent from the base scenario)")

    if (args.sms or args.no_custom_sms) and not args.empty:
        raise SystemExit("--sms / --no-custom-sms are --profile 5.2 --empty options "
                         "(the 5.0.2 path takes its SMS from the base scenario)")

    if not os.path.exists(OUTDIR):
        os.makedirs(OUTDIR)

    if args.empty:
        if args.profile != "5.2":
            raise SystemExit("--empty is only defined for --profile 5.2")
        if args.sites or args.frame_variant:
            raise SystemExit("--empty takes neither sites nor --frame-variant")
        if args.no_custom_sms and args.sms:
            raise SystemExit("--no-custom-sms and --sms are mutually exclusive "
                             "(--no-custom-sms IS --sms vendor)")
        sms_arg = "vendor" if args.no_custom_sms else args.sms
        print("=" * 70)
        build_empty_52(args.out_name, donor=args.donor,
                       frame_mode=args.frame_mode, frame_time=args.frame_time,
                       out_dir=args.out_dir, scenario_name=args.scenario_name,
                       terrain=args.terrain, sms=sms_arg, aoi=parse_aoi(args.aoi))
        if args.negative_controls:
            print("=" * 70)
            build_empty_52_negative_controls(
                args.out_name, args.negative_controls, donor=args.donor,
                frame_mode=args.frame_mode, frame_time=args.frame_time,
                scenario_name=args.scenario_name, sms=sms_arg)
        sys.exit(0)

    if args.profile != "5.0.2":
        raise SystemExit("--profile 5.2 currently builds only --empty")

    if args.frame_variant:
        if ":" not in args.frame_variant:
            raise SystemExit("--frame-variant wants SRC:OUT, got %r" % args.frame_variant)
        src_name, out_name = args.frame_variant.split(":", 1)
        if args.sites:
            raise SystemExit("--frame-variant does not take site arguments")
        print("=" * 70)
        build_frame_variant(src_name, out_name,
                            frame_mode=args.frame_mode, frame_time=args.frame_time,
                            out_dir=args.out_dir, scenario_name=args.scenario_name)
        sys.exit(0)

    ensure_sources()
    # optional argv: build only the named site(s); default = all.
    wanted = args.sites if args.sites else list(SITES.keys())
    for site in wanted:
        if site not in SITES:
            raise SystemExit("unknown site %r; known: %s" % (site, ", ".join(SITES)))
        print("=" * 70)
        build_site(site, SITES[site],
                   frame_mode=args.frame_mode, frame_time=args.frame_time,
                   out_dir=args.out_dir)
