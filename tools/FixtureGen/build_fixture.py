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
}

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
def build_site(site, cfg):
    lat, lon, h = cfg["lat"], cfg["lon"], cfg["h"]
    base = cfg["base"]
    E, N, U = enu_basis(lat, lon)
    leader_ecef = geodetic_to_ecef(lat, lon, h)
    tb_east = dis_euler(lat, lon, 90.0)          # units face East, level
    tb_north = dis_euler(lat, lon, 0.0)          # route local frame = NED
    anchor_ecef = geodetic_to_ecef(lat, lon, h + 150.0)

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

    scnx_path = os.path.join(SCEN_DIR, base + ".scnx")
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


if __name__ == "__main__":
    ensure_sources()
    if not os.path.exists(OUTDIR):
        os.makedirs(OUTDIR)
    for site, cfg in SITES.items():
        print("=" * 70)
        build_site(site, cfg)
