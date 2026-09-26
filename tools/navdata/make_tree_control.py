#!/usr/bin/env python
"""make_tree_control.py - prepare the N6 tree control: a small sub-box of MojaveAO20 generated on a
terrain whose biome definitions differ from the vendor's in ONE asset line.

Record: docs/experiments/PREREG_NAVTREES_YUCCA_2026-09-26.md. Python 3 stdlib only. Nothing is written
under C:\\MAK: the vendor tree is only READ (copied or linked to).

    python make_tree_control.py box     --runtime-config <AO20 .navRuntimeConfig> --i0 11 --j0 34 --n 5 --cfg <out .navGenConfig>
    python make_tree_control.py shadow  --shadow C:\\C2SIM\\vrf-nav\\shadow --swap DSS:YuccaPalm=HoneyMesquiteShortSpring
    python make_tree_control.py terrain --shadow C:\\C2SIM\\vrf-nav\\shadow --out "tools\\navdata\\out\\<name>.mtf"

box: the generator's AO20 sector grid (11 cells of 43 m per regular sector; x cells from -233, y cells from
  -232, log rows "Sector (i,j): xMin .. yMax" are inclusive cell indices) lives in an ENU frame about the
  area's runtime "offset". Sectors i0..i0+n-1 / j0..j0+n-1 span cells [-233+11*i0, -233+11*(i0+n)-1] x
  [-232+11*j0, ...]; their outer edges are converted ENU -> ECEF -> geodetic and written as the four corners
  at h = 700 m (the form of the AO20 config), inset by --inset m so the generator's "Aligning extent to
  CellSize 43" cannot round the box up to n*11+1 cells. tile-count = n keeps 11 cells per sector.
shadow: the MAK Earth (online) .earth pulls its includes by RELATIVE path ({% include x %}, xi:include
  href="./...", ../../ModelData ...), resolved against the including file. So instead of absolute includes,
  build a mirror: <shadow>\\TerrainData\\TerrainConfiguration is a real COPY of the vendor folder; every
  other entry of SharedData\\19\\latest and of its TerrainData is a directory JUNCTION to the vendor entry
  (read-only use). Then ONE edit inside the copy: in osgEarthCatalogs\\biome.definitions.CA-fveg.xml, in the
  <biome id="DSS"> block only, asset name="YuccaPalm" -> "HoneyMesquiteShortSpring" (the asset DSW already
  uses, :508). The edit must hit exactly one line.
terrain: a byte copy of the vendor MAK Earth (online).mtf whose ONE <myFilename> naming the .earth is
  pointed at the shadow copy, plus the vendor .surfChar.map beside it (as in N5).
"""
import argparse
import hashlib
import math
import os
import re
import shutil
import subprocess
import sys

A_WGS = 6378137.0
F_WGS = 1 / 298.257223563
E2 = F_WGS * (2 - F_WGS)
CELL = 43.0
LATEST = r"C:\MAK\SharedData\19\latest"
TC_REL = os.path.join("TerrainData", "TerrainConfiguration")
BIOME_DEF = os.path.join("osgEarthCatalogs", "biome.definitions.CA-fveg.xml")
EARTH = "MAK Earth (online).earth"
MTF = "MAK Earth (online).mtf"


def sha(p):
    return hashlib.sha256(open(p, "rb").read()).hexdigest()


def refuse_mak(p):
    if os.path.abspath(p).lower().startswith("c:\\mak"):
        raise SystemExit("refusing to write under C:\\MAK: %s" % p)


# ---------------- geometry ----------------

def ecef(lat, lon, h):
    la, lo = math.radians(lat), math.radians(lon)
    n = A_WGS / math.sqrt(1 - E2 * math.sin(la) ** 2)
    return ((n + h) * math.cos(la) * math.cos(lo), (n + h) * math.cos(la) * math.sin(lo), (n * (1 - E2) + h) * math.sin(la))


def geodetic(P):
    x, y, z = P
    p = math.hypot(x, y)
    lon = math.atan2(y, x)
    lat = math.atan2(z, p * (1 - E2))
    h = 0.0
    for _ in range(12):
        n = A_WGS / math.sqrt(1 - E2 * math.sin(lat) ** 2)
        h = p / math.cos(lat) - n
        lat = math.atan2(z, p * (1 - E2 * n / (n + h)))
    return math.degrees(lat), math.degrees(lon), h


def box(a):
    txt = open(a.runtime_config, encoding="utf-8").read()
    off = tuple(float(v) for v in re.search(r"\(offset\s+(-?[\d.]+)\s+(-?[\d.]+)\s+(-?[\d.]+)\)", txt).groups())
    lat0, lon0, _ = geodetic(off)
    sl, cl = math.sin(math.radians(lat0)), math.cos(math.radians(lat0))
    so, co = math.sin(math.radians(lon0)), math.cos(math.radians(lon0))
    E = (-so, co, 0.0)
    N = (-sl * co, -sl * so, cl)

    def to_ecef(e, n):
        return tuple(off[k] + e * E[k] + n * N[k] for k in range(3))
    x0 = (-233 + 11 * a.i0) * CELL
    x1 = (-233 + 11 * (a.i0 + a.n)) * CELL
    y0 = (-232 + 11 * a.j0) * CELL
    y1 = (-232 + 11 * (a.j0 + a.n)) * CELL
    ins = a.inset
    pts = {"nw": (x0 + ins, y1 - ins), "ne": (x1 - ins, y1 - ins), "sw": (x0 + ins, y0 + ins), "se": (x1 - ins, y0 + ins)}
    lines = []
    print("offset %s -> lat %.9f lon %.9f" % (off, lat0, lon0))
    print("sector block i %d..%d j %d..%d = cells x [%d, %d] y [%d, %d]; ENU edges x %.1f..%.1f y %.1f..%.1f (inset %.1f m)" % (
        a.i0, a.i0 + a.n - 1, a.j0, a.j0 + a.n - 1, -233 + 11 * a.i0, -233 + 11 * (a.i0 + a.n) - 1,
        -232 + 11 * a.j0, -232 + 11 * (a.j0 + a.n) - 1, x0, x1, y0, y1, ins))
    for k in ("nw", "ne", "sw", "se"):
        lat, lon, _ = geodetic(to_ecef(*pts[k]))
        c = ecef(lat, lon, 700.0)
        print("  %s  ENU (%.1f, %.1f)  lat %.9f lon %.9f  ECEF %.6f %.6f %.6f" % (k, pts[k][0], pts[k][1], lat, lon, *c))
        lines.append("   (extent-%s  %.6f %.6f %.6f)" % (k, c[0], c[1], c[2]))
    body = ("(nav-gen-config \n" + "\n".join(lines) + "\n"
            "   (tile-count-x %d)\n   (tile-count-y %d)\n"
            "   (allow-abstract-data True)\n   (generate-full-terrain False)\n   (generate-transition-points True)\n"
            "   (prune-no-go-areas True)\n   (raster-precision 0.200000)\n   (cell-size 43)\n"
            "   (profiles-to-generate \n      (ground-platform \"ground-platform\")\n   )\n)\n") % (a.n, a.n)
    data = body.replace("\n", "\r\n").encode("ascii")
    refuse_mak(a.cfg)
    os.makedirs(os.path.dirname(os.path.abspath(a.cfg)), exist_ok=True)
    open(a.cfg, "wb").write(data)
    print("wrote %s  %d B  sha256 %s" % (a.cfg, len(data), sha(a.cfg)))
    return 0


# ---------------- shadow tree ----------------

def junction(link, target):
    if os.path.exists(link):
        return "exists"
    r = subprocess.run(["cmd", "/c", "mklink", "/J", link, target], capture_output=True, text=True)
    if r.returncode != 0:
        raise SystemExit("mklink /J failed for %s: %s" % (link, r.stderr or r.stdout))
    return "junction"


def swap_asset(data, biome_id, old, new):
    """Replace asset name=old by new inside <biome id="biome_id"> ... </biome> only; exactly one hit."""
    m = re.search(rb'<biome id="' + biome_id.encode() + rb'"[^>]*>.*?</biome>', data, re.S)
    if not m:
        raise ValueError("biome %s not found" % biome_id)
    block = m.group(0)
    pat = re.compile(rb'(<asset name=")' + re.escape(old.encode()) + rb'(")')
    hits = pat.findall(block)
    if len(hits) != 1:
        raise ValueError("biome %s: expected one asset %s, found %d" % (biome_id, old, len(hits)))
    nb = pat.sub(rb"\g<1>" + new.encode() + rb"\g<2>", block)
    return data[:m.start()] + nb + data[m.end():]


def shadow(a):
    root = os.path.abspath(a.shadow)
    refuse_mak(root)
    os.makedirs(os.path.join(root, "TerrainData"), exist_ok=True)
    made = []
    for name in sorted(os.listdir(LATEST)):
        src = os.path.join(LATEST, name)
        if name == "TerrainData" or not os.path.isdir(src):
            continue
        made.append((name, junction(os.path.join(root, name), src)))
    for name in sorted(os.listdir(os.path.join(LATEST, "TerrainData"))):
        src = os.path.join(LATEST, "TerrainData", name)
        if name == "TerrainConfiguration" or not os.path.isdir(src):
            continue
        made.append(("TerrainData\\" + name, junction(os.path.join(root, "TerrainData", name), src)))
    dst_tc = os.path.join(root, TC_REL)
    if not os.path.exists(dst_tc):
        shutil.copytree(os.path.join(LATEST, TC_REL), dst_tc)
    n_files = sum(len(f) for _, _, f in os.walk(dst_tc))
    print("shadow %s: %s; TerrainConfiguration copied (%d files)" % (root, made, n_files))
    for spec in a.swap:
        biome_id, rest = spec.split(":", 1)
        old, new = rest.split("=", 1)
        src_def = os.path.join(LATEST, TC_REL, BIOME_DEF)
        dst_def = os.path.join(dst_tc, BIOME_DEF)
        data = swap_asset(open(src_def, "rb").read(), biome_id, old, new)
        open(dst_def, "wb").write(data)
        print("biome %s: %s -> %s in %s (vendor sha256 %s, copy sha256 %s)" % (biome_id, old, new, dst_def, sha(src_def), sha(dst_def)))
    return 0


# ---------------- terrain copy ----------------

def terrain(a):
    out = os.path.abspath(a.out)
    refuse_mak(out)
    root = os.path.abspath(a.shadow)
    src = open(os.path.join(LATEST, TC_REL, MTF), "rb").read()
    old = b"<myFilename>$(SHARED_DATA_DIR)/TerrainData/TerrainConfiguration/" + EARTH.encode() + b"</myFilename>"
    if src.count(old) != 1:
        raise SystemExit("expected one .earth <myFilename>, found %d" % src.count(old))
    new = b"<myFilename>" + os.path.join(root, TC_REL, EARTH).replace("\\", "/").encode() + b"</myFilename>"
    data = src.replace(old, new)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    open(out, "wb").write(data)
    tex = os.path.join(LATEST, TC_REL, "MAK Earth (online).surfChar.map")
    shutil.copyfile(tex, out[:-4] + ".surfChar.map")
    print("wrote %s (%d B, sha256 %s); .earth -> %s" % (out, len(data), sha(out), new.decode()))
    print("wrote %s" % (out[:-4] + ".surfChar.map"))
    return 0


def selftest():
    d = (b'<biome id="DSW" parent="x">\n<asset name="YuccaPalm"/>\n</biome>\n'
         b'<biome id="DSS" parent="x">\n   <asset name="YuccaPalm" />\n</biome>\n')
    o = swap_asset(d, "DSS", "YuccaPalm", "HoneyMesquiteShortSpring")
    ok = o.count(b"YuccaPalm") == 1 and b'<asset name="HoneyMesquiteShortSpring" />' in o and o.startswith(d[:60])
    try:
        swap_asset(d, "DSS", "Nope", "X")
        ok = False
    except ValueError:
        pass
    print("make_tree_control selftest: %s" % ("PASS" if ok else "FAIL"))
    return 0 if ok else 1


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    sub = ap.add_subparsers(dest="cmd")
    b = sub.add_parser("box")
    b.add_argument("--runtime-config", required=True)
    b.add_argument("--i0", type=int, required=True)
    b.add_argument("--j0", type=int, required=True)
    b.add_argument("--n", type=int, default=5)
    b.add_argument("--inset", type=float, default=1.0)
    b.add_argument("--cfg", required=True)
    s = sub.add_parser("shadow")
    s.add_argument("--shadow", required=True)
    s.add_argument("--swap", action="append", default=[], metavar="BIOME:OLD=NEW")
    t = sub.add_parser("terrain")
    t.add_argument("--shadow", required=True)
    t.add_argument("--out", required=True)
    sub.add_parser("selftest")
    a = ap.parse_args(argv)
    if a.cmd == "box":
        return box(a)
    if a.cmd == "shadow":
        return shadow(a)
    if a.cmd == "terrain":
        return terrain(a)
    if a.cmd == "selftest":
        return selftest()
    ap.print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main())
