#!/usr/bin/env python
"""osm_sector_map.py - map OSM highway ways onto a generated nav area's sectors and compare them
with the generator's per-sector "Generated N distinct nav tags." counts.

Written for docs/experiments/FINDING_NAV_TAGS_OSM_REFUTED_2026-09-26.md (the Part A check of
lane N3). Offline except for --fetch. Python 3 stdlib only. Nothing is written under C:\\MAK.

    python osm_sector_map.py --log <gen.log> --tiles <dir> [--fetch] [--json out.json] [--map]

INPUTS
- The vrfNavGenerator log (--log): the "Adjusted -" corner block (ECEF, after "Aligning extent
  to CellSize"), the line "CalculateTransitionPointLocations extent = (xmin,ymin,z,xmax,ymax,z)",
  every "Sector (i,j): xMin: a yMin: b xMax: c yMax: d" row (inclusive CELL indices), and the
  "Generated N distinct nav tags." line that follows each sector row.
- OSM vector tiles (--tiles DIR with subfolders osm/ and osm-highways/, files 14_<x>_<tmsy>.pbf):
  the two sources MAK Earth (online) reads - mbtiles/osm/ (land-cover roads,
  osm.features.xml TFSFeatures "data:osm-all-features") and mbtiles/osm-highways/ (the MAK_ROAD
  feature volumes, osm.roads.model.xml "data:vehicle-roads-linear"). --fetch downloads them
  from http://vr-theworld.com/vr-theworld/mbtiles/<set>/14/<x>/<y>.pbf.

TMS-Y CONVENTION: the server's TFS tiles use TMS rows (y counted from the south):
tms_y = 2^z - 1 - xyz_y. Verified by HTTP status on 2026-09-26: XYZ row 6511 -> 404, TMS row
9872 -> 200 at x 2880. 404s are normal (empty tiles) and are skipped.

THE FRAME AND THE FITTED SHIFT: the generator's sector grid lives in a local ENU frame whose
origin is the area's runtime "offset". That offset is written only to the .navRuntimeConfig,
which the 2026-09-25 AO20 run failed to write. The tool therefore builds ENU about the
centroid of the log's "Adjusted" corners (the vendor's Ala Moana .navRuntimeConfig has its
offset exactly at the corner centroid) and then shifts it east by the amount that makes the
corners land on the log's own CalculateTransitionPointLocations extent. For AO20 that shift is
+21.5 m (= half a 43 m cell): the centroid frame puts the corners at x = +/-10040.5, the log
says -10019 .. 10062. The shift is FITTED to that one log line, so the placement is verified
against the extent (0.0 m residual at all four corners) but the offset itself is assumed.
Half a cell is far below the 473 m sector size, so it cannot move a sector-scale result.

The per-way land-cover class is a transcription of selectStyle() in
TerrainConfiguration/osgEarthCatalogs/coverage/layer.OSM.roads.LOD14.online.xml (191 paved,
119 dirt, 66 sand, 50 grass). Ways are sampled every ~3 m with a +/-6 m stroke buffer.
"""
import argparse
import collections
import gzip
import json
import math
import os
import re
import sys
import urllib.request

A_WGS = 6378137.0
F_WGS = 1 / 298.257223563
E2 = F_WGS * (2 - F_WGS)
CELL = 43.0


# ---------------- log / frame ----------------

class SectorFrame(object):
    """Sector grid and tag counts from a generation log, plus lat/lon -> sector lookup."""

    def __init__(self, log_path, shift_east=None):
        text = open(log_path, encoding="utf-8", errors="replace").read()
        m = re.search(r"Adjusted -\s*(.*?)\^", text, re.S)
        if not m:
            raise ValueError("no 'Adjusted -' corner block in the log")
        corners = {}
        for k, x, y, z in re.findall(r"(ne|nw|se|sw): \{(-?[\d.]+), (-?[\d.]+), (-?[\d.]+)\}", m.group(1)):
            corners[k] = (float(x), float(y), float(z))
        if len(corners) != 4:
            raise ValueError("expected four adjusted corners, got %d" % len(corners))
        ext = re.search(r"CalculateTransitionPointLocations extent = \((-?[\d.]+),(-?[\d.]+),[^,]+,"
                        r"(-?[\d.]+),(-?[\d.]+),", text)
        self.extent = tuple(float(v) for v in ext.groups()) if ext else None
        self.origin = [sum(c[k] for c in corners.values()) / 4 for k in range(3)]
        p = math.hypot(self.origin[0], self.origin[1])
        lon0 = math.atan2(self.origin[1], self.origin[0])
        lat0 = math.atan2(self.origin[2], p * (1 - E2))
        for _ in range(10):
            n = A_WGS / math.sqrt(1 - E2 * math.sin(lat0) ** 2)
            h = p / math.cos(lat0) - n
            lat0 = math.atan2(self.origin[2], p * (1 - E2 * n / (n + h)))
        self._trig = (math.sin(lat0), math.cos(lat0), math.sin(lon0), math.cos(lon0))
        self.shift_east = 0.0
        if shift_east is None and self.extent:
            shift_east = self.extent[0] - self.enu(corners["nw"])[0]
        self.shift_east = shift_east or 0.0
        self.corner_residuals = {}
        if self.extent:
            want = {"nw": (self.extent[0], self.extent[3]), "ne": (self.extent[2], self.extent[3]),
                    "sw": (self.extent[0], self.extent[1]), "se": (self.extent[2], self.extent[1])}
            for k, c in corners.items():
                e, n = self.enu(c)
                self.corner_residuals[k] = (e - want[k][0], n - want[k][1])
        self.sectors = {}
        self.tags = {}
        cur = None
        for line in text.splitlines():
            ms = re.match(r"Sector \((\d+),(\d+)\): xMin: (-?\d+) yMin: (-?\d+) xMax: (-?\d+) yMax: (-?\d+)", line)
            if ms:
                cur = (int(ms.group(1)), int(ms.group(2)))
                self.sectors[cur] = tuple(int(g) for g in ms.groups()[2:])
                continue
            mt = re.search(r"Generated (\d+) distinct nav tags", line)
            if mt and cur is not None:
                self.tags[cur] = int(mt.group(1))
        self._ix = sorted({(k[0], v[0], v[2]) for k, v in self.sectors.items()})
        self._iy = sorted({(k[1], v[1], v[3]) for k, v in self.sectors.items()})

    def enu(self, P):
        sl, cl, so, co = self._trig
        d = [P[k] - self.origin[k] for k in range(3)]
        return (-so * d[0] + co * d[1] + self.shift_east, -sl * co * d[0] - sl * so * d[1] + cl * d[2])

    @staticmethod
    def ecef(lat, lon, h=700.0):
        la, lo = math.radians(lat), math.radians(lon)
        n = A_WGS / math.sqrt(1 - E2 * math.sin(la) ** 2)
        return ((n + h) * math.cos(la) * math.cos(lo), (n + h) * math.cos(la) * math.sin(lo),
                (n * (1 - E2) + h) * math.sin(la))

    def sector_of_enu(self, e, n):
        cx = math.floor(e / CELL)
        cy = math.floor(n / CELL)
        i = next((k for k, a, b in self._ix if a <= cx <= b), None)
        j = next((k for k, a, b in self._iy if a <= cy <= b), None)
        return None if i is None or j is None else (i, j)

    def sector_of(self, lat, lon):
        return self.sector_of_enu(*self.enu(self.ecef(lat, lon)))


# ---------------- MVT decoding ----------------

def _varint(b, i):
    r = s = 0
    while True:
        c = b[i]
        i += 1
        r |= (c & 0x7F) << s
        s += 7
        if c < 0x80:
            return r, i


def _fields(b):
    i = 0
    while i < len(b):
        k, i = _varint(b, i)
        f, w = k >> 3, k & 7
        if w == 0:
            v, i = _varint(b, i)
        elif w == 2:
            n, i = _varint(b, i)
            v = b[i:i + n]
            i += n
        elif w == 1:
            v = b[i:i + 8]
            i += 8
        elif w == 5:
            v = b[i:i + 4]
            i += 4
        else:
            raise ValueError("unsupported wire type %d" % w)
        yield f, w, v


def _packed(b):
    out = []
    i = 0
    while i < len(b):
        v, i = _varint(b, i)
        out.append(v)
    return out


def _zz(n):
    return (n >> 1) ^ -(n & 1)


def _value(b):
    for f, w, v in _fields(b):
        if f == 1:
            return v.decode("utf-8", "replace")
        if f == 6:
            return _zz(v)
        if f == 7:
            return bool(v)
        return v


def decode_mvt(b):
    """[(layer, extent, [(geom_type, props, parts)])]; parts are lists of (x, y) tile coords."""
    if b[:2] == b"\x1f\x8b":
        b = gzip.decompress(b)
    layers = []
    for f, w, v in _fields(b):
        if f != 3:
            continue
        name, feats, keys, vals, extent = None, [], [], [], 4096
        for f2, w2, v2 in _fields(v):
            if f2 == 1:
                name = v2.decode()
            elif f2 == 2:
                feats.append(v2)
            elif f2 == 3:
                keys.append(v2.decode())
            elif f2 == 4:
                vals.append(_value(v2))
            elif f2 == 5:
                extent = v2
        out = []
        for fb in feats:
            tags, gtype, geom = [], 0, []
            for f3, w3, v3 in _fields(fb):
                if f3 == 2:
                    tags = _packed(v3)
                elif f3 == 3:
                    gtype = v3
                elif f3 == 4:
                    geom = _packed(v3)
            props = {keys[tags[k]]: vals[tags[k + 1]] for k in range(0, len(tags), 2)}
            parts, x, y, i, cur = [], 0, 0, 0, None
            while i < len(geom):
                cmd, cnt = geom[i] & 7, geom[i] >> 3
                i += 1
                if cmd == 7:
                    if cur:
                        cur.append(cur[0])
                    continue
                for _ in range(cnt):
                    x += _zz(geom[i])
                    y += _zz(geom[i + 1])
                    i += 2
                    if cmd == 1:
                        cur = [(x, y)]
                        parts.append(cur)
                    else:
                        cur.append((x, y))
            out.append((gtype, props, parts))
        layers.append((name, extent, out))
    return layers


def tile_to_latlon(z, x, tms_y, px, py, extent):
    n = 2 ** z
    X = (x + px / extent) / n
    Y = (n - 1 - tms_y + py / extent) / n
    return math.degrees(math.atan(math.sinh(math.pi * (1 - 2 * Y)))), X * 360 - 180


# ---------------- classification (vendor filters, transcribed) ----------------

DIRT = ("unpaved", "fine_gravel", "gravel", "dirt", "earth", "mud", "ground", "sand", "compacted")
NODRIVE = ("path", "footway", "bridleway", "steps", "busway", "via_ferrata", "pedestrian", "cycleway", "raceway")


def landcover_value(props, gtype):
    """selectStyle() of layer.OSM.roads.LOD14.online.xml -> coverage value."""
    s = props.get("surface")
    if gtype == 3:
        if s == "sand":
            return 66
        if s == "grass":
            return 50
        if s in DIRT[:7]:
            return 119
        return 191
    h = props.get("highway")
    if h in ("path", "footway", "bridleway", "steps", "cycleway", "track"):
        return 119 if (s is None or s in DIRT) else 191
    if h in ("service", "residential", "tertiary", "unclassified"):
        return 119 if s in DIRT else 191
    if h == "construction":
        return 119
    return 191


def classify_landcover(gtype, props):
    if "highway" not in props or gtype == 1 or "tunnel" in props:
        return None
    return "lc%d" % landcover_value(props, gtype)


def classify_roadvol(gtype, props):
    if "highway" not in props or gtype != 2 or props.get("highway") in NODRIVE:
        return None
    return "roadvol"


# ---------------- tiles ----------------

def fetch(tiles_dir, bbox, z=14):
    lat_s, lat_n, lon_w, lon_e = bbox
    n = 2 ** z

    def xy(lat, lon):
        la = math.radians(lat)
        return (int((lon + 180) / 360 * n),
                int((1 - math.log(math.tan(la) + 1 / math.cos(la)) / math.pi) / 2 * n))
    x0, y_top = xy(lat_n, lon_w)
    x1, y_bot = xy(lat_s, lon_e)
    stats = collections.Counter()
    for tset in ("osm", "osm-highways"):
        os.makedirs(os.path.join(tiles_dir, tset), exist_ok=True)
        for x in range(x0, x1 + 1):
            for yx in range(y_top, y_bot + 1):
                tms = n - 1 - yx
                url = "http://vr-theworld.com/vr-theworld/mbtiles/%s/%d/%d/%d.pbf" % (tset, z, x, tms)
                out = os.path.join(tiles_dir, tset, "%d_%d_%d.pbf" % (z, x, tms))
                try:
                    with urllib.request.urlopen(url, timeout=30) as r:
                        data = r.read()
                    stats[(tset, 200)] += 1
                except Exception as e:
                    data = b""
                    stats[(tset, getattr(e, "code", "err"))] += 1
                open(out, "wb").write(data)
    return dict(("%s %s" % k, v) for k, v in stats.items())


def rasterize(frame, tiles_dir, tset, classify, hits, step_m=3.0, buffer_m=6.0, counter=None):
    d = os.path.join(tiles_dir, tset)
    for fn in sorted(os.listdir(d)):
        if not fn.endswith(".pbf"):
            continue
        z, x, y = (int(v) for v in fn[:-4].split("_"))
        b = open(os.path.join(d, fn), "rb").read()
        if not b or b[:1] != b"\x1a":   # empty / 404 body
            continue
        tile_m = 40075016.686 * math.cos(math.radians(34.6)) / 2 ** z
        for lname, ext, feats in decode_mvt(b):
            for gtype, props, parts in feats:
                r = classify(gtype, props)
                if r is None:
                    continue
                if counter is not None:
                    counter[(props.get("highway"), props.get("surface"))] += 1
                for part in parts:
                    segs = list(zip(part, part[1:])) or [(part[0], part[0])]
                    for a, c in segs:
                        L = max(1, int(math.dist(a, c) / ext * tile_m / step_m))
                        for s in range(L + 1):
                            t = s / L
                            lat, lon = tile_to_latlon(z, x, y, a[0] + (c[0] - a[0]) * t, a[1] + (c[1] - a[1]) * t, ext)
                            e, n = frame.enu(frame.ecef(lat, lon))
                            for de, dn in ((0, 0), (buffer_m, 0), (-buffer_m, 0), (0, buffer_m), (0, -buffer_m)):
                                k = frame.sector_of_enu(e + de, n + dn)
                                if k:
                                    hits[k][r] += 1


AO20_BBOX = (34.518288963, 34.697951198, -116.809143400, -116.590856600)


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--log", required=True)
    ap.add_argument("--tiles", required=True, help="folder with osm/ and osm-highways/ tile subfolders")
    ap.add_argument("--fetch", action="store_true", help="download the z14 tiles over --bbox first")
    ap.add_argument("--bbox", nargs=4, type=float, default=AO20_BBOX, metavar=("S", "N", "W", "E"))
    ap.add_argument("--json", help="write per-sector tags and hits to this file")
    ap.add_argument("--map", action="store_true", help="print the ASCII tag map (upper case = no way)")
    a = ap.parse_args(argv)
    frame = SectorFrame(a.log)
    print("frame: shift_east %.2f m; corner residuals (m) %s" % (
        frame.shift_east, {k: tuple(round(v, 2) for v in r) for k, r in sorted(frame.corner_residuals.items())}))
    print("sectors %d, with tag counts %d; tag histogram %s" % (
        len(frame.sectors), len(frame.tags), dict(sorted(collections.Counter(frame.tags.values()).items()))))
    if a.fetch:
        print("fetch:", fetch(a.tiles, a.bbox))
    hits = collections.defaultdict(collections.Counter)
    ways = collections.Counter()
    rasterize(frame, a.tiles, "osm", classify_landcover, hits, counter=ways)
    rasterize(frame, a.tiles, "osm-highways", classify_roadvol, hits)
    print("land-cover ways by (highway, surface):", ways.most_common(12))
    tab = collections.Counter()
    for k in frame.sectors:
        tab[(frame.tags.get(k), bool(hits.get(k)))] += 1
    print("tags | no highway way | some highway way")
    for t in sorted({t for t, _ in tab}):
        print("%4s | %13d | %d" % (t, tab[(t, False)], tab[(t, True)]))
    lc191 = sum(1 for k in frame.sectors if hits.get(k, {}).get("lc191"))
    print("sectors touched by a 191 (paved) stroke: %d" % lc191)
    agree = sum(1 for k in frame.sectors if bool(hits.get(k)) == (frame.tags.get(k, 1) >= 2))
    print("agreement 'has a way' vs 'tags >= 2': %.1f %%" % (100.0 * agree / len(frame.sectors)))
    if a.map:
        for j in range(max(k[1] for k in frame.sectors), -1, -1):
            row = ""
            for i in range(max(k[0] for k in frame.sectors) + 1):
                t = frame.tags.get((i, j), 0)
                ch = str(t) if t else "."
                if hits.get((i, j)):
                    ch = "abcdefghi"[t - 1] if t else "?"
                row += ch
            print("%2d %s" % (j, row))
    if a.json:
        with open(a.json, "w", encoding="utf-8") as f:
            json.dump({"%d,%d" % k: {"tags": frame.tags.get(k), "hits": dict(hits.get(k, {}))}
                       for k in sorted(frame.sectors)}, f)
    return 0


if __name__ == "__main__":
    sys.exit(main())
