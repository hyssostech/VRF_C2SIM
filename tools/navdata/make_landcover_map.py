#!/usr/bin/env python
"""make_landcover_map.py - build a terrain COPY with its own terrain-specific land-cover map.

VR-Forces 5.2 Release Notes VRF-7074 (p64; also p8): "You can now define a terrain-specific land cover
mapping that overrides the global one, for example, myTerrain.landCoverDataSurfChar.map." The vendor's
local example sits beside its terrain file: TerrainConfiguration\\Example_Ala Moana.landCoverDataSurfChar.map
next to Ala Moana.mtf (prefixed "Example_" so it is inactive). This tool writes, for a terrain copy
<dir>\\<name>.mtf OUTSIDE C:\\MAK:

    <dir>\\<name>.mtf                        byte copy of the vendor .mtf (every path in it is a $(SHARED_DATA_DIR)
                                            macro, so the copy works from any folder - make_nav_terrain.py)
    <dir>\\<name>.surfChar.map               byte copy of the vendor's <terrain>.surfChar.map, so the texture map
                                            the vendor terrain carries is held constant
    <dir>\\<name>.landCoverDataSurfChar.map  the installed GLOBAL map (appData\\settings\\vrfSim\\
                                            landCoverDataSurfChar.map) with the --set changes and nothing else

    python make_landcover_map.py --out "tools\\navdata\\out\\MAK Earth (online) + X.mtf" --set BM_LAND=sand

Each --set MATERIAL=soil must hit exactly one "Match <MATERIAL> <soil>" line; the separator, line ending
and every other byte are preserved. The derived map is NOT committed (it is the vendor's file with one line
changed); this script regenerates it from the installed map. Refuses to write under C:\\MAK.
Python 3 stdlib only.
"""
import argparse
import hashlib
import os
import re
import shutil
import sys

VENDOR_TC = r"C:\MAK\SharedData\19\latest\TerrainData\TerrainConfiguration"
GLOBAL_MAP = r"C:\MAK\vrforces5.2d\appData\settings\vrfSim\landCoverDataSurfChar.map"


def sha(path):
    return hashlib.sha256(open(path, "rb").read()).hexdigest()


def apply_sets(data, sets):
    """Return (new bytes, [(old line, new line)]). Pure; raises unless each set hits exactly one line."""
    changes = []
    for material, soil in sets:
        pat = re.compile(rb"^(Match[ \t]+" + re.escape(material.encode("ascii")) + rb"[ \t]+)(\S+)([ \t]*)(\r?)$", re.M)
        hits = list(pat.finditer(data))
        if len(hits) != 1:
            raise ValueError("%s: expected exactly one Match line, found %d" % (material, len(hits)))
        m = hits[0]
        new_line = m.group(1) + soil.encode("ascii") + m.group(3) + m.group(4)
        changes.append((m.group(0).decode("ascii").rstrip("\r"), new_line.decode("ascii").rstrip("\r")))
        data = data[:m.start()] + new_line + data[m.end():]
    return data, changes


def selftest():
    src = b"SurfaceCharacteristicMap\t\t\n{\t\t\nMatch\tBM_LAND\tdryground\nMatch\tBM_LAND-GRASS\tgrass\n}\t\t\n"
    out, ch = apply_sets(src, [("BM_LAND", "sand")])
    ok = (out == src.replace(b"BM_LAND\tdryground", b"BM_LAND\tsand")
          and ch == [("Match\tBM_LAND\tdryground", "Match\tBM_LAND\tsand")])
    try:
        apply_sets(src, [("BM_NOPE", "sand")])
        ok = False
    except ValueError:
        pass
    print("make_landcover_map selftest: %s" % ("PASS" if ok else "FAIL"))
    return 0 if ok else 1


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--out", help="the terrain COPY to create, <dir>\\<name>.mtf (outside C:\\MAK)")
    ap.add_argument("--terrain", default="MAK Earth (online)", help="vendor terrain base name in TerrainConfiguration")
    ap.add_argument("--set", action="append", default=[], metavar="MATERIAL=soil")
    ap.add_argument("--selftest", action="store_true")
    a = ap.parse_args(argv)
    if a.selftest:
        return selftest()
    if not a.out or not a.out.lower().endswith(".mtf") or not a.set:
        ap.error("--out <copy .mtf> and at least one --set are required")
    out = os.path.abspath(a.out)
    if out.lower().startswith("c:\\mak"):
        raise SystemExit("refusing to write under C:\\MAK")
    base = out[:-4]
    sets = [tuple(s.split("=", 1)) for s in a.set]
    src_mtf = os.path.join(VENDOR_TC, a.terrain + ".mtf")
    src_tex = os.path.join(VENDOR_TC, a.terrain + ".surfChar.map")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    shutil.copyfile(src_mtf, out)
    if os.path.exists(src_tex):
        shutil.copyfile(src_tex, base + ".surfChar.map")
    data, changes = apply_sets(open(GLOBAL_MAP, "rb").read(), sets)
    open(base + ".landCoverDataSurfChar.map", "wb").write(data)
    for p in (src_mtf, src_tex, GLOBAL_MAP):
        print("source  %s  %d B  sha256 %s" % (p, os.path.getsize(p), sha(p)))
    for p in (out, base + ".surfChar.map", base + ".landCoverDataSurfChar.map"):
        print("wrote   %s  %d B  sha256 %s" % (p, os.path.getsize(p), sha(p)))
    for old, new in changes:
        print("changed %r -> %r" % (old, new))
    return 0


if __name__ == "__main__":
    sys.exit(main())
