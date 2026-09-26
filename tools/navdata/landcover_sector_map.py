#!/usr/bin/env python
"""landcover_sector_map.py - which land-cover classes / soils lie in each sector of a generated nav area,
set against the generator's per-sector "Generated N distinct nav tags." count.

Written for docs/experiments/FINDING_NAV_TAGS_OSM_REFUTED_2026-09-26.md sec 7-8; CLCplus and OSM water added for
docs/experiments/FINDING_IRONSTORM_CORRIDOR_2026-09-26.md. Python 3 stdlib only; reads the vendor mapping files
under C:\\MAK READ-ONLY; --fetch downloads tiles with curl.

    python landcover_sector_map.py --log <gen.log> --tiles <dir> [--fetch] [--bbox S N W E]
                                   [--levels 59=14 154=12 165=12 188=10] [--water-tiles <dir>] [--json out.json]
    python landcover_sector_map.py --selftest

Chain, as the vendor files define it:
  pixel value -> <mapping> in coverage/layer.<X>.online.xml (soiltype= or preset= -> coverage/presets.xml)
  -> BM_* material -> appData/settings/vrfSim/landCoverDataSurfChar.map "Match <BM_*> <soil>" -> soil.
Layers, as composed by osgEarthCatalogs/biomes.landcover.coverage.online.xml (:32 "Add layers in order of resolution
from lowest to highest"): Copernicus 100 m (TMS 188, :35), NLCD 30 m (165, :38), CA-FVEG 15 m (154, :41),
CLCplus 10 m (59, :50 "CLCplus, Europe, 10m; data level 14"), OSM roads (:55), OSM inland water (:58, the top
online layer over Europe; the Range220 / 29Palms / Kilo2 layers are local US insets and Maxar 199 answered 404 over
Suwalki on 2026-09-26). A pixel's EFFECTIVE class is the highest layer whose value has a mapping line; unmapped
values (the commented-out water values, nodata) fall through [A: osgEarth compositing]. Same order as
tools/preflight/leg_check.py LC_SOURCES (:118-126: CLCplus first, Europe only, 404 over North America, so a Mojave
run falls through to exactly the three layers it used before).
OSM roads are NOT composed here: osm_sector_map.py maps them per sector (its --json), so the effective class below
is "OSM water, else raster" [A: a road stripe would win over the raster under it].
Raster tiles: global-geodetic PNG TMS at http://vr-theworld.com/vr-theworld/tiles/1.0.0/<id>/<L>/<x>/<y>.png,
x from -180, y from -90 (south), 180/2^L degrees per tile, value in the R channel (R = G = B, alpha 255).
The server refuses Python's default user agent (403) and serves curl, so --fetch shells out to curl.
OSM water: the z14 spherical-mercator MVT tiles of mbtiles/osm-water/ (osm.features.water.xml:5-9, TMS rows,
<set>/14_<x>_<tmsy>.pbf as osm_sector_map.py --fetch --fetch-sets osm-water writes them). Polygons are filled
(even-odd over all rings of a feature) on a 256 x 256 grid per tile with the value of selectStyle() in
coverage/layer.OSM.water.LOD14.online.xml:14-45 (81 standing, 82 flowing, 200 ocean, 90 marsh, 80 other), after the
islet/sand/scrub filter of osm.features.water.xml:15-19. LINE features are not coverage (the style has no stroke
width [A]); they are counted per sector as "osmw-line" so a stream through a sector is still visible.
Sectors are sampled on a --step (default 0.0001 deg) lattice through osm_sector_map.SectorFrame (same frame and
caveat; a name-only log gets synthesised cells).
"""
import argparse
import collections
import json
import os
import re
import struct
import subprocess
import sys
import zlib

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from osm_sector_map import SectorFrame, AO20_BBOX, decode_mvt  # noqa: E402

CAT = r"C:\MAK\SharedData\19\latest\TerrainData\TerrainConfiguration\osgEarthCatalogs\coverage"
LCMAP = r"C:\MAK\vrforces5.2d\appData\settings\vrfSim\landCoverDataSurfChar.map"
OSMW = 0   # pseudo layer id for the OSM water layer
LAYERS = {188: "layer.Copernicus.100m.online.xml", 165: "layer.NLCD.30m.online.xml",
          154: "layer.CA-FVEG.15m.online.xml", 59: "layer.CLCplus.10m.online.xml",
          OSMW: "layer.OSM.water.LOD14.online.xml"}
ORDER = (OSMW, 59, 154, 165, 188)   # top of the composite first
WATER_Z = 14


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


def parse_mappings(txt, presets):
    txt = re.sub(r"<!--.*?-->", "", txt, flags=re.S)
    m = {}
    for mm in re.finditer(r"<mapping ([^>]*)/>", txt):
        a = dict(re.findall(r'(\w+)="([^"]*)"', mm.group(1)))
        m[int(a["value"])] = (a.get("desc", ""), a.get("soiltype") or presets.get(a.get("preset")))
    return m


def load_chain(lcmap=LCMAP, cat=CAT):
    presets = dict(re.findall(r'<preset name="([^"]+)"[^>]*soiltype="([^"]+)"',
                              open(os.path.join(cat, "presets.xml"), encoding="utf-8-sig").read()))
    maps = {lid: parse_mappings(open(os.path.join(cat, fn), encoding="utf-8-sig").read(), presets)
            for lid, fn in LAYERS.items()}
    soil = {}
    for line in open(lcmap, encoding="utf-8-sig"):
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
                if os.path.exists(out) and os.path.getsize(out) > 0:
                    got[(lid, L, "cached")] += 1
                    continue
                url = "http://vr-theworld.com/vr-theworld/tiles/1.0.0/%d/%d/%d/%d.png" % (lid, L, x, y)
                code = subprocess.run(["curl", "-s", "-o", out, "-w", "%{http_code}", url],
                                      capture_output=True, text=True).stdout
                got[(lid, L, code)] += 1
    return dict(("%s" % (k,), v) for k, v in got.items())


# ---------------- OSM water (vendor selectStyle + filter, transcribed) ----------------

def water_value(props):
    """layer.OSM.water.LOD14.online.xml:14-45 selectStyle(); None when osm.features.water.xml:15-19 drops it."""
    if props.get("place") in ("islet", "island") or props.get("natural") in ("sand", "scrub"):
        return None
    w = props.get("water")
    if w in ("lake", "pond", "reservoir", "lock", "basin", "stream_pool", "oxbow", "cenote", "reflecting_pool"):
        return 81
    if w in ("river", "stream", "canal", "rapids", "motorway_link", "trunk_link", "primary_link",
             "secondary_link"):
        return 82
    if w in ("harbour", "lagoon"):
        return 200
    if w == "swamp":
        return 90
    return 80


def fill_polygon(grid, rings, value, ext, n=256):
    """Even-odd scanline fill of all rings (tile coords 0..ext) into an n x n grid (row 0 = tile top)."""
    edges = []
    for ring in rings:
        for (x0, y0), (x1, y1) in zip(ring, ring[1:] + ring[:1]):
            if y0 != y1:
                edges.append((x0, y0, x1, y1))
    s = ext / float(n)
    for r in range(n):
        yc = (r + 0.5) * s
        xs = []
        for x0, y0, x1, y1 in edges:
            if (y0 <= yc < y1) or (y1 <= yc < y0):
                xs.append(x0 + (yc - y0) * (x1 - x0) / (y1 - y0))
        xs.sort()
        for k in range(0, len(xs) - 1, 2):
            c0 = max(0, int(xs[k] / s + 0.5))
            c1 = min(n - 1, int(xs[k + 1] / s - 0.5))
            for c in range(c0, c1 + 1):
                grid[r][c] = value


def water_grids(tiles_dir, n=256):
    """{(x, tms_y): (area_grid, line_grid)}; grids are n x n lists (row 0 = north), None = no water."""
    out = {}
    if not tiles_dir or not os.path.isdir(tiles_dir):
        return out
    for fn in sorted(os.listdir(tiles_dir)):
        if not fn.endswith(".pbf"):
            continue
        z, x, y = (int(v) for v in fn[:-4].split("_"))
        if z != WATER_Z:
            continue
        b = open(os.path.join(tiles_dir, fn), "rb").read()
        if not b or b[:1] != b"\x1a":   # empty / 404 body
            continue
        area = [[None] * n for _ in range(n)]
        line = [[None] * n for _ in range(n)]
        for _lname, ext, feats in decode_mvt(b):
            for gtype, props, parts in feats:
                v = water_value(props)
                if v is None:
                    continue
                if gtype == 3:
                    fill_polygon(area, parts, v, ext, n)
                elif gtype == 2:
                    for part in parts:
                        for (x0, y0), (x1, y1) in zip(part, part[1:]):
                            steps = max(1, int(max(abs(x1 - x0), abs(y1 - y0)) * n / ext) * 2)
                            for k in range(steps + 1):
                                t = k / float(steps)
                                c = int((x0 + (x1 - x0) * t) * n / ext)
                                r = int((y0 + (y1 - y0) * t) * n / ext)
                                if 0 <= r < n and 0 <= c < n:
                                    line[r][c] = v
        out[(x, y)] = (area, line)
    return out


def water_lookup(wg, lat, lon, n=256):
    import math
    N = 2 ** WATER_Z
    X = (lon + 180.0) / 360.0 * N
    la = math.radians(lat)
    Yxyz = (1.0 - math.log(math.tan(la) + 1.0 / math.cos(la)) / math.pi) / 2.0 * N
    x, yx = int(X), int(Yxyz)
    g = wg.get((x, N - 1 - yx))
    if g is None:
        return None, None
    c = min(n - 1, int((X - x) * n))
    r = min(n - 1, int((Yxyz - yx) * n))
    return g[0][r][c], g[1][r][c]


# ---------------- raster grids ----------------

def raster_grids(tiles, levels):
    """{lid: (L, {(x, y): rows})} - tiles kept whole (a flat pixel dict of an L14 box does not fit in memory)."""
    grids = {}
    for lid, L in levels.items():
        g = {}
        for fn in os.listdir(tiles):
            parts = fn[:-4].split("_") if fn.endswith(".png") else None
            if not parts or len(parts) != 4 or int(parts[0]) != lid or int(parts[1]) != L:
                continue
            rows = png_values(os.path.join(tiles, fn))
            if rows is None:
                continue
            g[(int(parts[2]), int(parts[3]))] = rows
        grids[lid] = (L, g)
    return grids


def raster_lookup(grid, lat, lon):
    L, g = grid
    t = 180.0 / 2 ** L
    fx, fy = (lon + 180.0) / t, (lat + 90.0) / t
    x, y = int(fx), int(fy)
    rows = g.get((x, y))
    if rows is None:
        return None
    px = min(255, int((fx - x) * 256))
    py = 255 - min(255, int((fy - y) * 256))
    return rows[py][px]


def effective(maps, grids, wg, lat, lon):
    """(class, water_line): class = (lid, value) of the top layer with a mapped value, or None."""
    wline = None
    for lid in ORDER:
        if lid == OSMW:
            if wg:
                v, wline = water_lookup(wg, lat, lon)
                if v is not None and v in maps[OSMW]:
                    return (OSMW, v), wline
            continue
        if lid not in grids:
            continue
        v = raster_lookup(grids[lid], lat, lon)
        if v is not None and v in maps[lid]:
            return (lid, v), wline
    return None, wline


def soil_of(maps, soilmap, c):
    if c is None:
        return "none"
    bm = maps[c[0]][c[1]][1]
    return soilmap.get(bm, "UNMATCHED(%s)" % bm)


def survey(frame, maps, grids, wg, bbox, step):
    s, n, w, e = bbox
    s, n, w, e = s - 0.003, n + 0.003, w - 0.003, e + 0.003
    eff = collections.defaultdict(collections.Counter)
    lines = collections.defaultdict(collections.Counter)
    ilat = 0
    while True:
        lat = s + ilat * step
        if lat > n:
            break
        ilon = 0
        while True:
            lon = w + ilon * step
            if lon > e:
                break
            sec = frame.sector_of(lat, lon)
            if sec:
                cls, wl = effective(maps, grids, wg, lat, lon)
                eff[sec][cls] += 1
                if wl is not None:
                    lines[sec][wl] += 1
            ilon += 1
        ilat += 1
    return eff, lines


# ---------------- selftest (offline, synthetic; no C:\MAK needed) ----------------

def selftest():
    fails = []

    def check(name, ok, detail=""):
        print("%s  %s %s" % ("PASS" if ok else "FAIL", name, detail))
        if not ok:
            fails.append(name)
    # 1. layer order: OSM water > CLCplus > Copernicus; an unmapped top value falls through.
    maps = {OSMW: {81: ("lake", "BM_WATER-FRESH-STANDING")}, 59: {21: ("needle", "BM_FOREST")},
            188: {111: ("forest", "BM_FOREST")}, 154: {}, 165: {}}
    one = lambda v: (14, {(18501, 13109): [[v] * 256 for _ in range(256)]})  # noqa: E731
    lat, lon = 54.03, 23.26          # inside CLCplus L14 tile (18501, 13109)
    grids = {59: one(21), 188: (10, {})}
    check("1a CLCplus wins with no water", effective(maps, grids, {}, lat, lon)[0] == (59, 21))
    grids_un = {59: one(250), 188: (10, {})}
    check("1b unmapped CLCplus value falls through to nothing", effective(maps, grids_un, {}, lat, lon)[0] is None)
    wg = {(9250, 11125): ([[81] * 256 for _ in range(256)], [[None] * 256 for _ in range(256)])}
    check("1c OSM water is the top layer", effective(maps, grids, wg, lat, lon)[0] == (OSMW, 81))
    # 2. even-odd fill keeps a hole open.
    g = [[None] * 16 for _ in range(16)]
    outer = [(0, 0), (4096, 0), (4096, 4096), (0, 4096), (0, 0)]
    hole = [(1024, 1024), (3072, 1024), (3072, 3072), (1024, 3072), (1024, 1024)]
    fill_polygon(g, [outer, hole], 81, 4096, 16)
    check("2a fill covers the ring", g[0][0] == 81 and g[15][15] == 81)
    check("2b fill leaves the hole empty", g[8][8] is None)
    # 3. vendor selectStyle transcription + the islet filter.
    check("3a stream -> 82, lake -> 81, swamp -> 90, other -> 80",
          [water_value({"water": k}) for k in ("stream", "lake", "swamp", "x")] == [82, 81, 90, 80])
    check("3b islet dropped", water_value({"place": "islet", "water": "lake"}) is None)
    # 4. PNG decoder on a synthetic RGBA tile (filter 0 and 2 rows).
    raw = b""
    for r in range(4):
        raw += (b"\x00" if r % 2 == 0 else b"\x02") + (bytes([r * 10 + 1] * 3 + [255]) if r % 2 == 0 else b"\x00" * 4) * 4
    ihdr = struct.pack(">IIBBBBB", 4, 4, 8, 6, 0, 0, 0)

    def chunk(t, d):
        return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xffffffff)
    import tempfile
    fd, pth = tempfile.mkstemp(suffix=".png")
    os.write(fd, b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b""))
    os.close(fd)
    rows = png_values(pth)
    os.remove(pth)
    check("4 PNG R channel incl. an Up-filtered row", [r[0] for r in rows] == [1, 1, 21, 21], str([r[0] for r in rows]))
    # 5. vendor files, when installed: the lines this tool relies on.
    if os.path.isdir(CAT):
        clc = open(os.path.join(CAT, LAYERS[59]), encoding="utf-8-sig").read()
        osw = open(os.path.join(CAT, LAYERS[OSMW]), encoding="utf-8-sig").read()
        check("5a vendor CLCplus layer is TMS 59", "tiles/1.0.0/59/" in clc)
        check("5b vendor OSM water maps 81/82/90", all('value="%d"' % v in osw for v in (81, 82, 90)))
    else:
        print("SKIP  5 vendor files (no %s)" % CAT)
    print("selftest: %d failed" % len(fails))
    return 1 if fails else 0


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--selftest", action="store_true")
    ap.add_argument("--log")
    ap.add_argument("--tiles")
    ap.add_argument("--fetch", action="store_true")
    ap.add_argument("--bbox", nargs=4, type=float, default=AO20_BBOX, metavar=("S", "N", "W", "E"))
    ap.add_argument("--levels", nargs="*", default=["59=14", "154=12", "165=12", "188=10"])
    ap.add_argument("--water-tiles", help="folder of z14 osm-water .pbf tiles (14_<x>_<tmsy>.pbf)")
    ap.add_argument("--step", type=float, default=0.0001, help="lattice step, degrees")
    ap.add_argument("--json", help="write per-sector class counts, soils, water lines and tags here")
    ap.add_argument("--lcmap", default=LCMAP, help="land-cover -> soil map (default: the installed global one)")
    a = ap.parse_args(argv)
    if a.selftest:
        return selftest()
    if not a.log or not a.tiles:
        ap.error("--log and --tiles are required")
    levels = {int(k): int(v) for k, v in (x.split("=") for x in a.levels)}
    if a.fetch:
        print("fetch:", fetch(a.tiles, levels, a.bbox))
    maps, soilmap = load_chain(a.lcmap)
    frame = SectorFrame(a.log)
    grids = raster_grids(a.tiles, levels)
    print("raster tiles loaded: %s" % {lid: len(g[1]) for lid, g in grids.items()})
    wg = water_grids(a.water_tiles)
    print("osm-water tiles with features: %d" % len(wg))
    eff, wlines = survey(frame, maps, grids, wg, a.bbox, a.step)

    classes = collections.Counter()
    for sec in eff:
        for c in eff[sec]:
            classes[c] += 1
    print("effective classes (sectors containing them), with the chain:")
    for c, k in sorted(classes.items(), key=lambda kv: -kv[1]):
        desc = maps[c[0]][c[1]] if c else ("(none)", None)
        t = collections.Counter(frame.tags.get(sec) for sec in eff if eff[sec].get(c))
        print("  %-10s %-26s %-28s soil %-16s sectors %4d  tags %s" % (
            c, desc[0][:26].encode("ascii", "replace").decode("ascii"), desc[1], soil_of(maps, soilmap, c), k,
            dict(sorted((kk, vv) for kk, vv in t.items() if kk is not None))))
    tab = collections.defaultdict(collections.Counter)
    for sec in frame.sectors:
        tab[tuple(sorted({soil_of(maps, soilmap, c) for c in eff[sec]}))][frame.tags.get(sec)] += 1
    print("soil set present in the sector -> tag-count histogram:")
    for k, c in sorted(tab.items(), key=lambda kv: -sum(kv[1].values())):
        print("  %-48s %s" % ("+".join(k), dict(sorted((kk, vv) for kk, vv in c.items() if kk is not None))))
    b3 = b2 = 0
    for sec in frame.sectors:
        dg = any(soil_of(maps, soilmap, c) == "dryground" for c in eff[sec])
        b3 += dg == (frame.tags.get(sec, 1) >= 3)
        b2 += dg == (frame.tags.get(sec, 1) >= 2)
    N = len(frame.sectors)
    print("'a dryground-soil class is present' vs tags>=3: %.1f %%, vs tags>=2: %.1f %% (base rates %.1f / %.1f)" % (
        100.0 * b3 / N, 100.0 * b2 / N, 100.0 * sum(1 for s_ in frame.sectors if frame.tags.get(s_, 1) < 3) / N,
        100.0 * sum(1 for s_ in frame.sectors if frame.tags.get(s_, 1) < 2) / N))
    if a.json:
        out = {}
        for sec in sorted(frame.sectors):
            cnt = eff.get(sec, {})
            soils = collections.Counter()
            for c, v in cnt.items():
                soils[soil_of(maps, soilmap, c)] += v
            out["%d,%d" % sec] = {
                "tags": frame.tags.get(sec), "samples": sum(cnt.values()),
                "classes": {("none" if c is None else "%d:%d" % c): v for c, v in cnt.items()},
                "soils": dict(soils), "water_lines": {str(k): v for k, v in wlines.get(sec, {}).items()}}
        with open(a.json, "w", encoding="utf-8") as f:
            json.dump(out, f)
        print("wrote", a.json)
    return 0


if __name__ == "__main__":
    sys.exit(main())
