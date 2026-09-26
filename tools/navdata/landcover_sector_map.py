#!/usr/bin/env python
"""landcover_sector_map.py - which land-cover classes / soils lie in each sector of a generated nav area,
set against the generator's per-sector "Generated N distinct nav tags." count.

Written for docs/experiments/FINDING_NAV_TAGS_OSM_REFUTED_2026-09-26.md sec 7-8. Python 3 stdlib only;
reads the vendor mapping files under C:\\MAK READ-ONLY; --fetch downloads PNG tiles with curl.

    python landcover_sector_map.py --log <gen.log> --tiles <dir> [--fetch] [--levels 154=12 165=12 188=10]

Chain, as the vendor files define it:
  pixel value -> <mapping> in coverage/layer.<X>.online.xml (soiltype= or preset= -> coverage/presets.xml)
  -> BM_* material -> appData/settings/vrfSim/landCoverDataSurfChar.map "Match <BM_*> <soil>" -> soil.
Layers (biomes.landcover.coverage.online.xml, "lowest to highest" resolution): Copernicus 100 m (TMS 188),
NLCD 30 m (165), CA-FVEG 15 m (154). A pixel's EFFECTIVE class is the highest layer whose value has a mapping
line; unmapped values (the commented-out water values, nodata) fall through [A: osgEarth compositing].
Tiles: global-geodetic PNG TMS at http://vr-theworld.com/vr-theworld/tiles/1.0.0/<id>/<L>/<x>/<y>.png,
x from -180, y from -90 (south), 180/2^L degrees per tile, value in the R channel (R = G = B, alpha 255).
The server refuses Python's default user agent (403) and serves curl, so --fetch shells out to curl.
Sectors are sampled on a 0.0001 deg lattice through osm_sector_map.SectorFrame (same frame and caveat).
"""
import argparse
import ast
import collections
import os
import re
import struct
import subprocess
import sys
import zlib

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from osm_sector_map import SectorFrame, AO20_BBOX  # noqa: E402

CAT = r"C:\MAK\SharedData\19\latest\TerrainData\TerrainConfiguration\osgEarthCatalogs\coverage"
LCMAP = r"C:\MAK\vrforces5.2d\appData\settings\vrfSim\landCoverDataSurfChar.map"
LAYERS = {188: "layer.Copernicus.100m.online.xml", 165: "layer.NLCD.30m.online.xml", 154: "layer.CA-FVEG.15m.online.xml"}
ORDER = (154, 165, 188)   # highest resolution first


def png_values(path):
    """R channel of an 8-bit RGBA, non-interlaced PNG as a list of rows; None if not a PNG."""
    b = open(path, "rb").read()
    if b[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    i, idat, w, h = 8, b"", 0, 0
    while i < len(b):
        n, = struct.unpack(">I", b[i:i + 4])
        typ, data = b[i + 4:i + 8], b[i + 8:i + 8 + n]
        i += 12 + n
        if typ == b"IHDR":
            w, h, bd, ct, _, _, il = struct.unpack(">IIBBBBB", data)
            if (bd, ct, il) != (8, 6, 0):
                raise ValueError("%s: unsupported PNG (%d, %d, %d)" % (path, bd, ct, il))
        elif typ == b"IDAT":
            idat += data
    raw, bpp = zlib.decompress(idat), 4
    stride, rows, prev, p = w * bpp, [], bytearray(w * bpp), 0
    for _ in range(h):
        f, line = raw[p], bytearray(raw[p + 1:p + 1 + stride])
        p += 1 + stride
        for x in range(stride):
            a = line[x - bpp] if x >= bpp else 0
            up = prev[x]
            c = prev[x - bpp] if x >= bpp else 0
            if f == 1:
                line[x] = (line[x] + a) & 255
            elif f == 2:
                line[x] = (line[x] + up) & 255
            elif f == 3:
                line[x] = (line[x] + ((a + up) >> 1)) & 255
            elif f == 4:
                pp = a + up - c
                pa, pb, pc = abs(pp - a), abs(pp - up), abs(pp - c)
                line[x] = (line[x] + (a if pa <= pb and pa <= pc else (up if pb <= pc else c))) & 255
        rows.append([line[4 * k] if line[4 * k + 3] else None for k in range(w)])
        prev = line
    return rows


def load_chain():
    presets = dict(re.findall(r'<preset name="([^"]+)"[^>]*soiltype="([^"]+)"',
                              open(os.path.join(CAT, "presets.xml"), encoding="utf-8-sig").read()))
    maps = {}
    for lid, fn in LAYERS.items():
        txt = re.sub(r"<!--.*?-->", "", open(os.path.join(CAT, fn), encoding="utf-8-sig").read(), flags=re.S)
        m = {}
        for mm in re.finditer(r"<mapping ([^>]*)/>", txt):
            a = dict(re.findall(r'(\w+)="([^"]*)"', mm.group(1)))
            m[int(a["value"])] = (a.get("desc", ""), a.get("soiltype") or presets.get(a.get("preset")))
        maps[lid] = m
    soil = {}
    for line in open(LCMAP, encoding="utf-8-sig"):
        mm = re.match(r"\s*Match\s+(\S+)\s+(\S+)", line)
        if mm:
            soil[mm.group(1)] = mm.group(2).lower()
    return maps, soil


def fetch(tiles, levels, bbox):
    s, n, w, e = bbox
    os.makedirs(tiles, exist_ok=True)
    got = collections.Counter()
    for lid, L in levels.items():
        t = 180.0 / 2 ** L
        for x in range(int((w + 180) // t), int((e + 180) // t) + 1):
            for y in range(int((s + 90) // t), int((n + 90) // t) + 1):
                out = os.path.join(tiles, "%d_%d_%d_%d.png" % (lid, L, x, y))
                url = "http://vr-theworld.com/vr-theworld/tiles/1.0.0/%d/%d/%d/%d.png" % (lid, L, x, y)
                code = subprocess.run(["curl", "-s", "-o", out, "-w", "%{http_code}", url],
                                      capture_output=True, text=True).stdout
                got[(lid, L, code)] += 1
    return dict(got)


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--log", required=True)
    ap.add_argument("--tiles", required=True)
    ap.add_argument("--fetch", action="store_true")
    ap.add_argument("--levels", nargs="*", default=["154=12", "165=12", "188=10"])
    a = ap.parse_args(argv)
    levels = {int(k): int(v) for k, v in (x.split("=") for x in a.levels)}
    if a.fetch:
        print("fetch:", fetch(a.tiles, levels, AO20_BBOX))
    maps, soilmap = load_chain()
    frame = SectorFrame(a.log)
    grids = {}
    for lid, L in levels.items():
        res, g = 180.0 / 2 ** L / 256, {}
        for fn in os.listdir(a.tiles):
            parts = fn[:-4].split("_") if fn.endswith(".png") else None
            if not parts or int(parts[0]) != lid or int(parts[1]) != L:
                continue
            rows = png_values(os.path.join(a.tiles, fn))
            if rows is None:
                continue
            x, y = int(parts[2]), int(parts[3])
            for py, row in enumerate(rows):
                for px, v in enumerate(row):
                    g[(x * 256 + px, y * 256 + 255 - py)] = v
        grids[lid] = (res, g)
    s, n, w, e = AO20_BBOX
    s, n, w, e = s - 0.003, n + 0.003, w - 0.003, e + 0.003
    eff = collections.defaultdict(collections.Counter)
    step, lat = 0.0001, s
    while lat <= n:
        lon = w
        while lon <= e:
            sec = frame.sector_of(lat, lon)
            if sec:
                cls = None
                for lid in ORDER:
                    if lid not in grids:
                        continue
                    res, g = grids[lid]
                    v = g.get((int((lon + 180) / res), int((lat + 90) / res)))
                    if v is not None and v in maps[lid]:
                        cls = (lid, v)
                        break
                eff[sec][cls] += 1
            lon += step
        lat += step

    def soil_of(c):
        if c is None:
            return "none"
        bm = maps[c[0]][c[1]][1]
        return soilmap.get(bm, "UNMATCHED(%s)" % bm)
    classes = collections.Counter()
    for sec in eff:
        for c in eff[sec]:
            classes[c] += 1
    print("effective classes (sectors containing them), with the chain:")
    for c, k in sorted(classes.items(), key=lambda kv: -kv[1]):
        desc = maps[c[0]][c[1]] if c else ("(none)", None)
        t = collections.Counter(frame.tags[sec] for sec in eff if eff[sec].get(c))
        print("  %-10s %-26s %-16s soil %-12s sectors %4d  tags %s" % (c, desc[0][:26], desc[1], soil_of(c), k,
                                                                       dict(sorted(t.items()))))
    tab = collections.defaultdict(collections.Counter)
    for sec in frame.sectors:
        tab[tuple(sorted({soil_of(c) for c in eff[sec]}))][frame.tags[sec]] += 1
    print("soil set present in the sector -> tag-count histogram:")
    for k, c in sorted(tab.items(), key=lambda kv: -sum(kv[1].values())):
        print("  %-36s %s" % ("+".join(k), dict(sorted(c.items()))))
    b3 = b2 = 0
    for sec in frame.sectors:
        dg = any(soil_of(c) == "dryground" for c in eff[sec])
        b3 += dg == (frame.tags[sec] >= 3)
        b2 += dg == (frame.tags[sec] >= 2)
    N = len(frame.sectors)
    print("'a dryground-soil class is present' vs tags>=3: %.1f %%, vs tags>=2: %.1f %% (base rates %.1f / %.1f)" % (
        100.0 * b3 / N, 100.0 * b2 / N, 100.0 * sum(1 for s_ in frame.sectors if frame.tags[s_] < 3) / N,
        100.0 * sum(1 for s_ in frame.sectors if frame.tags[s_] < 2) / N))
    return 0


if __name__ == "__main__":
    sys.exit(main())
