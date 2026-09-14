#!/usr/bin/env python3
"""ROUTE PRE-FLIGHT for the C2SIM -> VR-Forces interface (DEMO_READINESS row 20).

Walks every leg of a C2SIM order against the SAME terrain the sim streams and the performing
unit's own vehicle limits, and flags legs a tracked vehicle probably cannot traverse. Every
flag is a PREDICTION off terrain tiles, never a vendor verdict, and says so in every output.

WHY, in one line: the interface authors routes from the heights at the ROUTE VERTICES only
(4-5 km apart) and never sees what is between them; in P11/G2/G3 two battalions drove into a
55 m face of 0.86 rise-over-run on sand and stopped there for the rest of the run while the
vendor reported the task "TaskRunning" forever
(docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md sec 7).

DATA SOURCES (all read-only; nothing here launches or touches VR-Forces):
  elevation  VR-TheWorld TMS dataset 149, level 13 (the MAK Earth elevation layer the sim
             streams: elevation.worldwide.online.xml:24-35), 257x257 float32 GeoTIFF,
             EPSG:4326 global-geodetic, bilinear (osgEarth default under
             options.terrain-rex-default.xml). Validated in FINDING sec 7 against the sim's
             own reported altitudes: median residual +0.03 m over 129 samples.
  land cover VR-TheWorld TMS 154 (CA FVEG 15 m), 165 (NLCD 30 m), 188 (Copernicus 100 m);
             highest-resolution-with-data wins. Class -> soiltype from
             <SharedData>/TerrainData/TerrainConfiguration/osgEarthCatalogs/coverage/
             layer.*.online.xml.
  soil       soiltype (BM_*) -> surface characteristic from
             <VRF>/appData/settings/vrfSim/landCoverDataSurfChar.map; surface characteristic
             -> soil -> acceleration-factor from
             <VRF>/data/simulationModelSets/EntityLevel/vrfSim/systems/movement/
             ground-tracked.sysdef (soil-factors/soil-list).
  vehicles   data/unit-type-map-52.json (unit -> VRF template) then the vendor .entity files
             (<subordinates> recursively, <real paramName="max-slope">, parentFile
             inheritance).

USAGE (from the repo root)
  python tools/preflight/leg_check.py --selftest
  python tools/preflight/leg_check.py --calibrate --verify-run runs/20260907T150643Z_run
  python tools/preflight/leg_check.py --calibrate --verify-run RUNDIR --sensitivity
  python tools/preflight/leg_check.py --text
  python tools/preflight/leg_check.py --text --json out.json \
      --c2sim-observations obs.xml --vrf-overlay overlay.json
"""

import argparse
import csv
import glob
import json
import math
import os
import re
import subprocess
import sys
import uuid
import xml.etree.ElementTree as ET

# --------------------------------------------------------------------------- paths/defaults

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))

DEF_ORDER = os.path.join(REPO, "data", "COA-STP1_Order.xml")
DEF_INIT = os.path.join(REPO, "data", "COA-STP1_Initialization.xml")
DEF_TYPEMAP = os.path.join(REPO, "data", "unit-type-map-52.json")
# The PROBE map every COA-STP1 run since 2026-09-06 actually used (the DI-Guy data package is
# absent, so the fidelity table's lifeform rows were replaced by vehicles before the run, not
# inside this tool). --calibrate defaults to it so the calibration checks the templates the sim
# really created; --typemap overrides.
DEF_TYPEMAP_NOLIFEFORM = os.path.join(REPO, "data", "unit-type-map-52-nolifeform.json")
DEF_VRF = os.environ.get("VRF_HOME", r"C:\MAK\vrforces5.2d")
DEF_SHARED = os.environ.get("MAK_SHARED_DATA", r"C:\MAK\SharedData\19\latest")
DEF_CACHE = os.environ.get("PREFLIGHT_CACHE", os.path.join(HERE, "preflight_cache"))

C2SIM_NS = "http://www.sisostds.org/schemas/C2SIM/1.1"
NS = {"c": C2SIM_NS}
TMS_BASE = "http://vr-theworld.com/vr-theworld/tiles/1.0.0"

ELEV_DS = 149
ELEV_LEVEL = 13          # the best DataExtent over the Mojave AO (FINDING sec 7)
TILEPX = 257
SEG = TILEPX - 1

# (tileset, level, label) in descending ground resolution; the first with data wins. The
# levels are the DEEPEST the server actually serves (probed 2026-09-13: 154 and 165 return
# "no tile" at L13, 188 at L11) - one oversample level above each catalogue's data level.
LC_SOURCES = [(154, 12, "CA FVEG 15m"), (165, 12, "NLCD 30m"), (188, 10, "Copernicus 100m")]

# DEFAULT WINDOW AND THRESHOLD - CHOSEN FROM THE CALIBRATION TABLE (--calibrate --verify-run;
# README "Calibration"). A leg is flagged when its worst sustained (40 m) climb reaches
# THRESHOLD x the derated vehicle limit on that ground. Against the ground truth DERIVED from
# the P11 trace (--verify-run: three units stopped on leg 1, not two), the frozen legs sit at
# ratio 0.966..1.195 and the clean movers at 0.415..0.870, so 0.92 is the midpoint of that
# 0.096-wide gap. NEITHER NUMBER IS A CONSTANT: at an 80 m window the separation dies
# (margin -0.003). See --sensitivity.
DEF_THRESHOLD = 0.92
DEF_WINDOW = 40.0        # m, the sustained window (5 native postings; see --sensitivity)
DEF_SHORT = 20.0         # m, the short window (reported, never the verdict)
DEF_STEP = 8.0           # m, sample spacing along the leg
DEF_DROP_ORIGIN = 100.0  # m, Vrf:DropOriginVertexMeters (VrfSettings.cs:250)
DEF_MIN_LEG = 1.0        # m, legs shorter than this are not legs (a chained start that
                         # coincides with the task's first vertex) and are skipped
DEF_MAX_NAN = 0.01       # fraction of elevation samples that may be NaN before the leg gets
                         # NO VERDICT instead of a pass/flag

R_EARTH = 6371000.0

# DtSoilType (geometry/surface.h:19-44; the right-hand column of landCoverDataSurfChar.map)
# -> DtRoughnessSoilType (surface.h:75-88; the names in ground-tracked.sysdef's soil-list).
# ASSUMED except the "sand" row, which FINDING sec 7 confirms end to end for this AO: the
# implementation (DtMapSurfaceToRoughness::operator()) lives in the geometry DLL, not in a
# header. Every assumed row is listed by --selftest and marked per sample in --json.
SOIL_BRIDGE = {
    "pavedroad": ("paved-road", False), "asphaltorotherhardsurface": ("paved-road", True),
    "usrailroad": ("hard-packed", True), "eurorailroad": ("hard-packed", True),
    "gravelroad": ("gravel", True), "dirtroad": ("hard-packed", True),
    "dryground": ("hard-packed", True), "cultivatedfields": ("hard-packed", True),
    "grass": ("hard-packed", True), "orchards": ("hard-packed", True),
    "forest": ("hard-packed", True), "softsoil": ("sand", True),
    "flimsy": ("hard-packed", True), "rock": ("rocks", True), "boulder": ("rocks", True),
    "sand": ("sand", False),
    "mud": ("muck", True), "muddyroad": ("muck", True), "swamp": ("muck", True),
    "bodyofwater": ("deep-water", True), "ocean": ("deep-water", True),
    "deeplake": ("deep-water", True), "deepriver": ("deep-water", True),
    "shallowlake": ("shallow-water", True), "shallowriver": ("shallow-water", True),
    "tree": ("hard-packed", True), "building": ("hard-packed", True),
    "undefinedsoiltype": ("hard-packed", True),
}
# Used only when ground-tracked.sysdef cannot be read (its lines 787-820).
SOIL_FACTORS_FALLBACK = {
    "paved-road": 1.00, "hard-packed": 0.98, "gravel": 0.95, "rocks": 0.80, "sand": 0.80,
    "shallow-water": 0.70, "deep-water": 0.00, "muck": 0.40, "snow": 0.60, "ice": 1.00,
}
# Used only when the vendor SMS is unreachable (movingObjectParameters.h: rise-over-run).
MAXSLOPE_FALLBACK_MIN = 0.94

# LIFEFORM PROXY - FALLBACK ONLY. The 5.2 fidelity table points several COA-STP1 rows at
# DI-Guy lifeform templates, and the headless sim CRASHES at the first DI-Guy human while the
# DI-Guy data package is absent, so every run to date (P11 included) ran the probe map
# data/unit-type-map-52-nolifeform.json, which has the substitution baked in. That map is what
# --calibrate uses. This proxy only catches a lifeform row that a DIFFERENT --typemap still
# carries, so that a foot unit is not checked against a soldier's 1.5 max-slope. Detection is
# unambiguous: no vendor GROUND VEHICLE exceeds max-slope 1.0, and every lifeform is 1.5 or
# 1.57 (probed across all 1,646 .entity files).
LIFEFORM_SLOPE = 1.2
LIFEFORM_PROXY = {"friendly": "Tank Platoon (USA)", "hostile": "Tank Platoon (RUS)"}

# SUPERSEDED P11 labels, kept only as the --calibrate fallback when --verify-run is not given.
# These label a unit by its NET DISPLACEMENT over the whole trace, which misses a unit that
# drove far and then stopped: 4-27 is "MOVED" here and FROZE on its leg-1 face at 22.7 km.
# Use --verify-run RUNDIR, which derives the label from the leader's own track.
P11_NET_LABELS = {
    "1-1/2/1_AD": ("MOVED", 26111), "1-35/2/1_A": ("FROZE", 1968),
    "1-6/2/1_AD": ("FROZE", 2838), "4-27/2/1_A": ("MOVED", 22701),
    "40/2/1_AD": ("MOVED", 27963), "5-20/2/1_A": ("MOVED", 15083),
    "856/HHC": ("MOVED", 23871), "B/5-20": ("MOVED", 13976),
    "C/1-35": ("MOVED", 24527), "A/6-56/HHC": ("NO-ROUTE", 0),
}

# --verify-run rule (review fix 1). All four numbers are stated, none fitted.
VERIFY_TAIL_S = 600.0     # s, the tail of the trace the stall test looks at
VERIFY_STALL_M = 20.0     # m, along-leg advance over that tail below which a leader is stopped
VERIFY_ARRIVE_M = 200.0   # m, distance to the next route vertex that counts as an arrival
VERIFY_CRAWL_M = 300.0    # m, tail advance below which a still-moving leader is "crawling"

# --------------------------------------------------------------------------- geodesy


def dist_m(lat1, lon1, lat2, lon2):
    dla = math.radians(lat2 - lat1)
    dlo = math.radians(lon2 - lon1)
    a = (math.sin(dla / 2.0) ** 2
         + math.cos(math.radians(lat1)) * math.cos(math.radians(lat2))
         * math.sin(dlo / 2.0) ** 2)
    return 2.0 * R_EARTH * math.asin(math.sqrt(min(1.0, a)))


def interp(lat1, lon1, lat2, lon2, f):
    """Linear in lat/lon - exact enough over a few km, and it is the straight line the
    vendor's ground-vehicle-move-to actually drives when no nav mesh covers the leg."""
    return lat1 + (lat2 - lat1) * f, lon1 + (lon2 - lon1) * f


# --------------------------------------------------------------------------- tiles


class Tiles(object):
    """VR-TheWorld TMS reader with an on-disk cache. curl does the fetch: python urllib
    gets 403 from vr-theworld.com."""

    def __init__(self, cache, offline=False, nearest=False):
        self.cache = cache
        self.offline = offline
        self.nearest = nearest       # sample the nearest posting instead of bilinear
        self.mem = {}
        self.fetched = 0
        self.failed = set()
        if not os.path.isdir(cache):
            os.makedirs(cache)
        try:
            from PIL import Image
        except ImportError:
            sys.stderr.write("FATAL: Pillow (PIL) is required: python -m pip install pillow\n")
            raise
        self.Image = Image

    def _file(self, ds, level, x, y, ext):
        return os.path.join(self.cache, "%d_%d_%d_%d.%s" % (ds, level, x, y, ext))

    def _fetch(self, ds, level, x, y, ext, minbytes):
        fn = self._file(ds, level, x, y, ext)
        if os.path.exists(fn) and os.path.getsize(fn) >= minbytes:
            return fn
        if self.offline or (ds, level, x, y) in self.failed:
            return None
        url = "%s/%d/%d/%d/%d.%s" % (TMS_BASE, ds, level, x, y, ext)
        try:
            data = subprocess.run(["curl", "-s", "-m", "60", url],
                                  capture_output=True).stdout
        except OSError:
            return None
        if len(data) < minbytes:
            self.failed.add((ds, level, x, y))
            return None
        with open(fn, "wb") as fh:
            fh.write(data)
        self.fetched += 1
        return fn

    # ---- elevation ----
    def _elev_tile(self, level, x, y):
        key = (ELEV_DS, level, x, y)
        if key in self.mem:
            return self.mem[key]
        fn = self._fetch(ELEV_DS, level, x, y, "tif", 1000)
        data = None
        if fn:
            try:
                im = self.Image.open(fn)
                data = list(im.get_flattened_data() if hasattr(im, "get_flattened_data")
                            else im.getdata())
            except Exception:
                data = None
        self.mem[key] = data
        return data

    def _elev_sample(self, level, gi, gj):
        """gi = global sample column (lon index); gj = global sample row from the SOUTH."""
        x, i = divmod(gi, SEG)
        y, j = divmod(gj, SEG)
        d = self._elev_tile(level, x, y)
        if d is None:
            for (xx, ii, yy, jj) in ((x - 1, i + SEG, y, j), (x, i, y - 1, j + SEG),
                                     (x - 1, i + SEG, y - 1, j + SEG)):
                if 0 <= ii <= SEG and 0 <= jj <= SEG:
                    d2 = self._elev_tile(level, xx, yy)
                    if d2 is not None:
                        return d2[(SEG - jj) * TILEPX + ii]
            return float("nan")
        return d[(SEG - j) * TILEPX + i]          # PIL row 0 = the tile's north edge

    def elev(self, lat, lon, level=ELEV_LEVEL):
        p = (180.0 / (2 ** level)) / SEG          # posting, degrees
        gi = (lon + 180.0) / p
        gj = (lat + 90.0) / p
        i0, j0 = int(math.floor(gi)), int(math.floor(gj))
        fi, fj = gi - i0, gj - j0
        if self.nearest:
            return self._elev_sample(level, i0 + (1 if fi >= 0.5 else 0),
                                     j0 + (1 if fj >= 0.5 else 0))
        z00 = self._elev_sample(level, i0, j0)
        z10 = self._elev_sample(level, i0 + 1, j0)
        z01 = self._elev_sample(level, i0, j0 + 1)
        z11 = self._elev_sample(level, i0 + 1, j0 + 1)
        return (z00 * (1 - fi) * (1 - fj) + z10 * fi * (1 - fj)
                + z01 * (1 - fi) * fj + z11 * fi * fj)

    @staticmethod
    def posting_m(lat, level=ELEV_LEVEL):
        p = (180.0 / (2 ** level)) / SEG
        return p * 111320.0 * math.cos(math.radians(lat)), p * 111320.0

    # ---- land cover ----
    def landcover_value(self, ds, level, lat, lon):
        size = 180.0 / (2 ** level)
        x = int((lon + 180.0) / size)
        y = int((lat + 90.0) / size)
        fx = ((lon + 180.0) - x * size) / size
        fy = ((lat + 90.0) - y * size) / size
        px = min(255, int(fx * 256))
        py = min(255, 255 - int(fy * 256))
        key = (ds, level, x, y)
        if key in self.mem:
            im = self.mem[key]
        else:
            fn = self._fetch(ds, level, x, y, "png", 100)
            im = None
            if fn:
                try:
                    im = self.Image.open(fn)
                    im.load()
                except Exception:
                    im = None
            self.mem[key] = im
        if im is None:
            return None
        try:
            v = im.getpixel((px, py))
        except Exception:
            return None
        return v[0] if isinstance(v, tuple) else v


# --------------------------------------------------------------------------- soil chain


class SoilChain(object):
    """class value -> soiltype -> surface characteristic -> soil -> acceleration-factor."""

    def __init__(self, shared=DEF_SHARED, vrf=DEF_VRF):
        self.maps = {}          # tileset -> {class value: (soiltype, desc)}
        self.surfchar = {}      # BM_* (upper) -> surface characteristic
        self.factors = dict(SOIL_FACTORS_FALLBACK)
        self.sources = []
        self._load_layers(shared)
        self._load_surfchar(vrf)
        self._load_factors(vrf)
        self.cache = {}

    def _load_layers(self, shared):
        base = os.path.join(shared, "TerrainData", "TerrainConfiguration",
                            "osgEarthCatalogs", "coverage")
        presets = {}
        pfile = os.path.join(base, "presets.xml")
        if os.path.exists(pfile):
            txt = open(pfile, encoding="utf-8-sig", errors="replace").read()
            for m in re.finditer(r'<preset\s+name="([^"]+)"[^>]*?soiltype="([^"]+)"', txt):
                presets[m.group(1)] = m.group(2)
        for ds, _lvl, _lbl in LC_SOURCES:
            fn = None
            for cand in sorted(glob.glob(os.path.join(base, "layer.*.online.xml"))):
                body = open(cand, encoding="utf-8-sig", errors="replace").read()
                if ("/%d/" % ds) in body.replace("\\", "/"):
                    fn = cand
                    break
            if fn is None:
                continue
            txt = open(fn, encoding="utf-8-sig", errors="replace").read()
            txt = re.sub(r"<!--.*?-->", "", txt, flags=re.S)     # drop commented-out rows
            table = {}
            for m in re.finditer(r"<mapping\s+([^>]*?)/>", txt):
                attrs = dict(re.findall(r'(\w+)="([^"]*)"', m.group(1)))
                if "value" not in attrs:
                    continue
                soiltype = attrs.get("soiltype")
                if not soiltype and attrs.get("preset"):
                    soiltype = presets.get(attrs["preset"])
                if not soiltype:
                    continue
                try:
                    table[int(attrs["value"])] = (soiltype, attrs.get("desc", ""))
                except ValueError:
                    continue
            if table:
                self.maps[ds] = table
                self.sources.append("%s -> %d class rows" % (os.path.basename(fn), len(table)))

    def _load_surfchar(self, vrf):
        fn = os.path.join(vrf, "appData", "settings", "vrfSim", "landCoverDataSurfChar.map")
        if not os.path.exists(fn):
            return
        for line in open(fn, encoding="utf-8", errors="replace"):
            parts = line.split()
            if len(parts) >= 3 and parts[0] == "Match":
                self.surfchar[parts[1].upper()] = parts[2]
            elif len(parts) == 2 and parts[0].upper().startswith("BM_"):
                self.surfchar[parts[0].upper()] = parts[1]   # the file's one unindented row
        self.sources.append("%s -> %d soiltype rows"
                            % (os.path.basename(fn), len(self.surfchar)))

    def _load_factors(self, vrf):
        fn = os.path.join(vrf, "data", "simulationModelSets", "EntityLevel", "vrfSim",
                          "systems", "movement", "ground-tracked.sysdef")
        if not os.path.exists(fn):
            return
        txt = open(fn, encoding="utf-8", errors="replace").read()
        m = re.search(r"\(soil-list(.*?)\n\s*\)\s*\n\s*\)\s*\n\s*\)", txt, re.S)
        if not m:
            return
        got = {}
        for mm in re.finditer(r"\(([a-z\-]+)\s*\n\s*\(acceleration-factor\s+([0-9.]+)\)",
                              m.group(1)):
            got[mm.group(1)] = float(mm.group(2))
        if got:
            self.factors.update(got)
            self.sources.append("ground-tracked.sysdef -> %d soil acceleration factors"
                                % len(got))

    def classify(self, tiles, lat, lon):
        """-> dict(source, value, desc, soiltype, surfchar, soil, factor, assumed)."""
        key = (round(lat, 5), round(lon, 5))
        if key in self.cache:
            return self.cache[key]
        out = dict(source="none", value=None, desc="", soiltype="", surfchar="",
                   soil="hard-packed", factor=self.factors.get("hard-packed", 0.98),
                   assumed=True)
        for ds, level, label in LC_SOURCES:
            v = tiles.landcover_value(ds, level, lat, lon)
            if v is None or v == 0:               # 0 = no data in this tileset here
                continue
            table = self.maps.get(ds, {})
            if v not in table:
                continue
            soiltype, desc = table[v]
            sc = self.surfchar.get(soiltype.upper(), "undefinedsoiltype")
            sl, assumed = SOIL_BRIDGE.get(sc.lower(), ("hard-packed", True))
            out = dict(source=label, value=v, desc=desc, soiltype=soiltype, surfchar=sc,
                       soil=sl, factor=self.factors.get(sl, 0.98), assumed=assumed)
            break
        self.cache[key] = out
        return out


# --------------------------------------------------------------------------- vendor SMS


class VendorSms(object):
    """Index of the vendor .entity files: objectType -> label / max-slope / subordinates,
    with parentFile inheritance (M3A2_Bradley_CFV, for one, inherits M2A2_Bradley_IFV)."""

    RX_OBJ = re.compile(r'<simObject\s+objectType="([^"]+)"')
    RX_MATCH = re.compile(r'matchType="([^"]+)"')
    RX_PARENT = re.compile(r'parentFile="([^"]+)"')
    RX_LABEL = re.compile(r'<string paramName="gui-label">([^<]*)</string>')
    RX_SLOPE = re.compile(r'<real paramName="max-slope">([^<]+)</real>')
    RX_SUB = re.compile(r'<subordinate\s+objectType="([^"]+)"')

    def __init__(self, sms_dir):
        self.dir = sms_dir
        self.by_type = {}
        self.by_label = {}
        self.by_file = {}
        self.ok = os.path.isdir(sms_dir)
        if not self.ok:
            return
        for fn in glob.glob(os.path.join(sms_dir, "*.entity")):
            try:
                txt = open(fn, encoding="utf-8", errors="replace").read()
            except Exception:
                continue
            mo = self.RX_OBJ.search(txt)
            if not mo:
                continue
            head = txt[mo.start():txt.find(">", mo.start())]
            lbl = self.RX_LABEL.search(txt)
            slope = self.RX_SLOPE.search(txt)
            mt = self.RX_MATCH.search(head)
            pf = self.RX_PARENT.search(head)
            stem = os.path.basename(fn)[:-len(".entity")]
            rec = dict(objectType=mo.group(1), matchType=mt.group(1) if mt else None,
                       parent=pf.group(1) if pf else None, file=stem,
                       label=(lbl.group(1) if lbl else stem),
                       maxSlope=float(slope.group(1)) if slope else None,
                       subs=self.RX_SUB.findall(txt))
            self.by_type.setdefault(rec["objectType"], rec)
            self.by_file.setdefault(stem, rec)
            self.by_label.setdefault(rec["label"], rec)
            self.by_label.setdefault(stem, rec)

    def find(self, name_or_type):
        if name_or_type in self.by_label:
            return self.by_label[name_or_type]
        if name_or_type in self.by_file:
            return self.by_file[name_or_type]
        t = name_or_type
        if t.count(":") == 7:                 # 8-field VRF type: strip the superType
            t = t.split(":", 1)[1]
        if t in self.by_type:
            return self.by_type[t]
        for rec in self.by_type.values():     # matchType wildcards (-1 fields)
            mt = rec.get("matchType")
            if not mt:
                continue
            a, b = mt.split(":"), t.split(":")
            if len(a) == len(b) and all(x == "-1" or x == y for x, y in zip(a, b)):
                return rec
        return None

    def _parent_of(self, rec):
        p = rec.get("parent")
        if not p:
            return None
        stem = p[:-len(".entity")] if p.lower().endswith(".entity") else p
        return self.by_file.get(stem)

    def _inherited(self, rec, field, depth=0):
        while rec is not None and depth <= 8:
            v = rec.get(field)
            if v not in (None, [], ""):
                return v
            rec = self._parent_of(rec)
            depth += 1
        return None

    def vehicles(self, template, depth=0, seen=None):
        """-> [(label, max-slope or None)] for the leaves of the subordinate tree."""
        seen = set() if seen is None else seen
        rec = self.find(template)
        if rec is None:
            return [("UNRESOLVED:%s" % template, None)]
        if rec["objectType"] in seen or depth > 6:
            return []
        seen = seen | {rec["objectType"]}
        subs = self._inherited(rec, "subs") or []
        if not subs:
            return [(rec["label"], self._inherited(rec, "maxSlope"))]
        out = []
        for s in subs:
            out += self.vehicles(s, depth + 1, seen)
        return out

    def min_max_slope(self, template):
        veh = self.vehicles(template)
        slopes = [s for _, s in veh if s is not None]
        if not slopes:
            return None, veh
        return min(slopes), veh


# --------------------------------------------------------------------------- type map


class TypeMap(object):
    """Replicates UnitTypeMap.Lookup (src/VrfC2SimApp/UnitTypeMap.cs:217-262)."""

    def __init__(self, path, friendly="USA", opposing="RUS"):
        d = json.load(open(path, encoding="utf-8"))
        self.rows = d["rows"]
        self.nations = d.get("nations", {})
        self.friendly = friendly
        self.opposing = opposing

    @staticmethod
    def function_id(sidc):
        if not sidc or len(sidc) < 5:
            return "(none)"
        f = sidc[4:min(10, len(sidc))].rstrip("-")
        return f if f else "(none)"

    @staticmethod
    def echelon_char(sidc):
        return sidc[11] if sidc and len(sidc) >= 12 else "\0"

    def lookup(self, sidc, echelon_code, hostile):
        role = "hostile" if hostile else "friendly"
        nation = self.opposing if hostile else self.friendly
        fid = self.function_id(sidc)
        ech = self.echelon_char(sidc)

        def nat(r):
            return (r["nationRole"].lower() == role
                    and (not r.get("nation") or r["nation"] == nation))
        for r in self.rows:                                   # (b) functionId + SIDC echelon
            if nat(r) and r["functionId"] and r["functionId"] == fid and r["echelon"] == ech:
                return r, "b:functionId+sidcEchelon"
        if echelon_code:                                      # (c) functionId + EchelonCode
            for r in self.rows:
                if (nat(r) and r["functionId"] and r["functionId"] == fid
                        and r.get("echelonCode", "").upper() == echelon_code.upper()):
                    return r, "c:functionId+echelonCode"
        for r in self.rows:                                   # (d) echelon-only
            if nat(r) and not r["functionId"] and r["echelon"] == ech:
                return r, "d:sidcEchelon"
        for r in self.rows:                                   # (e) catch-all
            if nat(r) and not r["functionId"] and r["echelon"] == "*":
                return r, "e:catchAll"
        return None, "none"


# --------------------------------------------------------------------------- C2SIM parsing


def parse_init(path):
    """-> (units by uuid, force sides by uuid)."""
    root = ET.parse(path).getroot()
    sides = {}
    for fs in root.iter("{%s}ForceSide" % C2SIM_NS):
        u = fs.findtext("c:UUID", default="", namespaces=NS)
        sides[u] = fs.findtext("c:Name", default="", namespaces=NS)
    units = {}
    for un in root.iter("{%s}Unit" % C2SIM_NS):
        u = un.findtext("c:UUID", default="", namespaces=NS)
        if not u:
            continue
        lat = un.findtext(".//c:GeodeticCoordinate/c:Latitude", namespaces=NS)
        lon = un.findtext(".//c:GeodeticCoordinate/c:Longitude", namespaces=NS)
        units[u] = dict(
            uuid=u,
            name=un.findtext("c:Name", default="", namespaces=NS),
            lat=float(lat) if lat else None,
            lon=float(lon) if lon else None,
            sidc=un.findtext(".//c:APP6C-SIDC", default="", namespaces=NS),
            echelon=un.findtext("c:EchelonCode", default="", namespaces=NS),
            side=un.findtext(".//c:EntityDescriptor/c:Side", default="", namespaces=NS))
    return units, sides


def parse_order(path):
    """-> [dict(name, uuid, performer, affected, action, points)] in file order."""
    root = ET.parse(path).getroot()
    tasks = []
    for t in root.iter("{%s}ManeuverWarfareTask" % C2SIM_NS):
        pts = []
        for loc in t.findall("c:Location", NS):
            la = loc.findtext(".//c:Latitude", namespaces=NS)
            lo = loc.findtext(".//c:Longitude", namespaces=NS)
            if la and lo:
                pts.append((float(la), float(lo)))
        tasks.append(dict(
            name=t.findtext("c:Name", default="", namespaces=NS),
            uuid=t.findtext("c:UUID", default="", namespaces=NS),
            performer=t.findtext("c:PerformingEntity", default="", namespaces=NS),
            affected=t.findtext("c:AffectedEntity", default="", namespaces=NS),
            action=t.findtext("c:TaskActionCode", default="", namespaces=NS),
            points=pts))
    return tasks


# --------------------------------------------------------------------------- start positions


def load_starts_csv(path):
    starts = {}
    with open(path, encoding="ascii", newline="") as fh:
        for row in csv.DictReader(fh):
            if not row.get("unit"):
                continue
            starts[row["unit"].strip()] = (float(row["lat"]), float(row["lon"]))
    return starts


def leaders_from_log(run_dir):
    """-> {unit: (member name, VRF uuid)} for the FIRST member of each unit, which is the
    formation leader, from vrfc2simapp.log's "members of <unit>: <name> [VRF_UUID:...]"."""
    log = os.path.join(run_dir, "vrfc2simapp.log")
    rx = re.compile(r"members of ([^:]+):\s*(.*)")
    rxm = re.compile(r"([^,\[]+)\s*\[VRF_UUID:([0-9a-f\-]+)\]")
    leader = {}
    for line in open(log, encoding="utf-8", errors="replace"):
        m = rx.search(line)
        if not m:
            continue
        unit = m.group(1).strip()
        if unit.endswith("~PXY"):
            unit = unit[:-4]
        members = rxm.findall(m.group(2))
        if members and unit not in leader:
            leader[unit] = (members[0][0].strip(), members[0][1])
    return leader


def starts_from_run(run_dir):
    """The interface SPREADS co-located units onto 700 m rings at init (DeStack), so the
    authored position is NOT where a unit starts. Recover the real start from a run: the
    member map in vrfc2simapp.log ("members of <unit>: <name> [VRF_UUID:...]") plus the FIRST
    POS row of the FORMATION LEADER (the first member listed) in watchvrf-trace.csv."""
    trace = os.path.join(run_dir, "watchvrf-trace.csv")
    leader = leaders_from_log(run_dir)
    want = {uu: unit for unit, (_nm, uu) in leader.items()}
    first = {}
    with open(trace, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            if not line.startswith("POS,"):
                continue
            p = line.rstrip("\r\n").split(",")
            if len(p) < 6:
                continue
            uu = p[2][9:] if p[2].startswith("VRF_UUID:") else p[2]
            if uu not in want or uu in first:
                continue
            try:
                la, lo = float(p[3]), float(p[4])
            except ValueError:
                continue
            if la != la or lo != lo:
                continue
            first[uu] = (la, lo)
    return {unit: first[uu] for unit, (_nm, uu) in leader.items() if uu in first}, leader


# ------------------------------------------------------------------- derived run ground truth


def leader_tracks(run_dir, leaders):
    """-> {unit: [(wall_t, lat, lon), ...]} for each unit's FORMATION LEADER, trace order.

    watchvrf-trace.csv POS rows are type,wall_t,uuid,lat,lon,alt with the uuid carrying the
    "VRF_UUID:" prefix; rows before the object has a position carry NaN/90.0 and are dropped."""
    want = {uu: unit for unit, (_nm, uu) in leaders.items()}
    out = {unit: [] for unit in leaders}
    trace = os.path.join(run_dir, "watchvrf-trace.csv")
    with open(trace, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            if not line.startswith("POS,"):
                continue
            p = line.rstrip("\r\n").split(",")
            if len(p) < 6:
                continue
            uu = p[2][9:] if p[2].startswith("VRF_UUID:") else p[2]
            unit = want.get(uu)
            if unit is None:
                continue
            try:
                t, la, lo = float(p[1]), float(p[3]), float(p[4])
            except ValueError:
                continue
            if la != la or lo != lo or t != t or abs(la) > 89.0:
                continue
            out[unit].append((t, la, lo))
    return out


def project_along(a, b, p):
    """Metres of p along the a->b direction, in an equirectangular tangent plane at a on the
    same sphere dist_m uses (R_EARTH), so an along-leg advance and a distance are comparable."""
    k = R_EARTH * math.pi / 180.0
    kx = k * math.cos(math.radians(a[0]))
    ux, uy = (b[1] - a[1]) * kx, (b[0] - a[0]) * k
    n = math.hypot(ux, uy)
    if n <= 0.0:
        return 0.0
    px, py = (p[1] - a[1]) * kx, (p[0] - a[0]) * k
    return (px * ux + py * uy) / n


def verify_leg(track, a, b):
    """DERIVE the ground truth for leg a->b from the leader's own track (review fix 1).

    Net displacement over a whole trace cannot tell a unit that drove far and then froze from
    one that kept going, so the label comes from the TAIL of the trace and from whether the
    leader ever reached the vertex leg 1 was aiming at:

      FROZE  - along-leg advance < VERIFY_STALL_M over the final VERIFY_TAIL_S seconds AND the
               leader never came within VERIFY_ARRIVE_M of the next route vertex;
      MOVED  - anything else, sub-labelled "arrived" when it did reach that vertex and
               "crawling" when it did not but is still advancing by less than VERIFY_CRAWL_M
               over the tail.

    NOTE on "never came within" - the review's wording was "ends > 200 m from the next route
    vertex". The whole route is handed to VR-Forces at once, so five of the nine P11 units
    drove THROUGH vertex 1 and were 15-20 km past it at the end of the trace; an end-distance
    test labels those FROZE. Closest approach over the track is the test that reproduces the
    review's own expected labels (1-1, 40, C/1-35, B/5-20, 5-20 clean).
    """
    out = dict(label="NO-TRACK", detail="", along_m=0.0, advance_m=0.0, vertex_m=0.0,
               end_vertex_m=0.0, fixes=len(track), t0=0.0, t1=0.0)
    if len(track) < 2:
        return out
    t0, t1 = track[0][0], track[-1][0]
    last = (track[-1][1], track[-1][2])
    along_end = project_along(a, b, last)
    tail0 = t1 - VERIFY_TAIL_S
    ref = track[0]
    for fix in track:
        if fix[0] <= tail0:
            ref = fix
        else:
            break
    advance = along_end - project_along(a, b, (ref[1], ref[2]))
    closest = min(dist_m(f[1], f[2], b[0], b[1]) for f in track)
    end_vertex = dist_m(last[0], last[1], b[0], b[1])
    if advance < VERIFY_STALL_M and closest > VERIFY_ARRIVE_M:
        label, detail = "FROZE", "stopped at %.0f m along leg 1" % along_end
    elif closest <= VERIFY_ARRIVE_M:
        label, detail = "MOVED", "arrived"
    elif advance < VERIFY_CRAWL_M:
        label, detail = "MOVED", "crawling"
    else:
        label, detail = "MOVED", "moving"
    out.update(label=label, detail=detail, along_m=along_end, advance_m=advance,
               vertex_m=closest, end_vertex_m=end_vertex, t0=t0, t1=t1)
    return out


# --------------------------------------------------------------------------- leg analysis


def build_route(task, unit, start, drop_origin):
    """Replicate VrfC2SimService.ExecuteTaskOnTick (src/VrfC2SimApp/VrfC2SimService.cs:1888-
    1945): the route STARTS at the unit's live position, and leading vertices within
    drop_origin of the unit's AUTHORED position are dropped when the unit has been spread
    further than drop_origin from that authored position - never all of them."""
    pts = list(task["points"])
    authored = (unit["lat"], unit["lon"]) if unit and unit["lat"] is not None else None
    skip = 0
    note = ""
    if (drop_origin > 0 and len(pts) > 1 and authored
            and dist_m(start[0], start[1], authored[0], authored[1]) > drop_origin):
        while (skip < len(pts) - 1
               and dist_m(pts[skip][0], pts[skip][1], authored[0], authored[1]) <= drop_origin):
            skip += 1
        if skip:
            note = ("dropped %d leading vertex(es) on the authored origin (%.5f,%.5f); "
                    "the unit was spread %.0f m from it"
                    % (skip, authored[0], authored[1],
                       dist_m(start[0], start[1], authored[0], authored[1])))
    return [start] + pts[skip:], skip, note


def analyse_leg(tiles, soil, a, b, limit_raw, step, window, short):
    """Sample the straight line a->b every `step` metres and return the leg metrics."""
    length = dist_m(a[0], a[1], b[0], b[1])
    n = max(2, int(math.ceil(length / step)) + 1)
    s = [length * i / (n - 1) for i in range(n)]
    samples = []
    for i in range(n):
        f = 0.0 if length == 0 else s[i] / length
        la, lo = interp(a[0], a[1], b[0], b[1], f)
        samples.append(dict(s=s[i], lat=la, lon=lo, z=tiles.elev(la, lo),
                            soil=soil.classify(tiles, la, lo)))
    zs = [p["z"] for p in samples]
    ok = [z == z for z in zs]                      # False where the tile was missing (NaN)
    nan_n = ok.count(False)
    climb = sum(max(0.0, zs[i + 1] - zs[i]) for i in range(n - 1) if ok[i] and ok[i + 1])
    descend = sum(max(0.0, zs[i] - zs[i + 1]) for i in range(n - 1) if ok[i] and ok[i + 1])

    def worst(win):
        """max MEAN UPHILL grade over a sliding ~win metre window -> (grade, i0, i1).
        Windows with a NaN endpoint are skipped, and counted in nan_samples."""
        if length <= 0:
            return 0.0, 0, 0
        k = max(1, int(round(win / step)))
        if k >= n:
            k = n - 1
        best = (-9.9, 0, min(k, n - 1))
        for i in range(0, n - k):
            run = s[i + k] - s[i]
            if run <= 0 or not ok[i] or not ok[i + k]:
                continue
            g = (zs[i + k] - zs[i]) / run
            if g > best[0]:
                best = (g, i, i + k)
        if best[0] < -9.0:
            return 0.0, 0, min(k, n - 1)
        return best

    g55, i0, i1 = worst(window)
    g20, j0, j1 = worst(short)
    mid = (i0 + i1) // 2
    win_samples = samples[i0:i1 + 1] or [samples[0]]
    # The governing limit over the window is the WORST (lowest) derated limit inside it.
    gov = min(win_samples, key=lambda p: limit_raw * p["soil"]["factor"])
    limit = limit_raw * gov["soil"]["factor"]
    return dict(
        length_m=length, n_samples=n,
        sustained=g55, sustained_window_m=(s[i1] - s[i0]) if n > 1 else 0.0,
        short=g20, short_window_m=(s[j1] - s[j0]) if n > 1 else 0.0,
        worst_lat=samples[mid]["lat"], worst_lon=samples[mid]["lon"],
        worst_s_m=samples[mid]["s"], worst_z_m=samples[mid]["z"],
        worst_seg=[[samples[i0]["lat"], samples[i0]["lon"]],
                   [samples[i1]["lat"], samples[i1]["lon"]]],
        soil=gov["soil"]["soil"], soil_desc=gov["soil"]["desc"],
        soil_source=gov["soil"]["source"], soil_assumed=gov["soil"]["assumed"],
        soil_surfchar=gov["soil"]["surfchar"], soil_soiltype=gov["soil"]["soiltype"],
        factor=gov["soil"]["factor"], limit_raw=limit_raw, limit=limit,
        ratio=(g55 / limit) if limit > 0 else float("inf"),
        climb_m=climb, descend_m=descend,
        nan_samples=nan_n, nan_fraction=(float(nan_n) / n if n else 0.0),
        no_verdict=bool(n and float(nan_n) / n > DEF_MAX_NAN),
        start=[a[0], a[1]], end=[b[0], b[1]])


# --------------------------------------------------------------------------- orchestration


def run_preflight(args, tiles, soil, sms, tmap, units, sides, tasks, starts,
                  only_first=False, unit_filter=None, progress=True, chain=True):
    """chain: a unit's SECOND and later tasks start at the end of its previous task's route
    (the interface sequences a unit's tasks in declared order, dispatching the next one only
    after the previous has finished), not at its initial position."""
    chain_pos = {}
    friendly_side = None
    for su, nm in sides.items():
        low = nm.lower()
        if "coalition" in low or "nato" in low:
            friendly_side = su
    results = []
    for ti, task in enumerate(tasks):
        unit = units.get(task["performer"])
        uname = unit["name"] if unit else ("uuid:" + task["performer"][:8])
        if unit_filter and uname not in unit_filter:
            continue
        if not task["points"]:
            results.append(dict(task=task["name"], task_uuid=task["uuid"], unit=uname,
                                unit_uuid=task["performer"], legs=[],
                                note="no route points in the order"))
            continue
        hostile = bool(unit and friendly_side and unit["side"] != friendly_side)
        row, how = (tmap.lookup(unit["sidc"], unit["echelon"], hostile)
                    if unit else (None, "no unit in the initialization"))
        template = row["templateName"] if row else ""
        limit_raw, veh = (sms.min_max_slope(template) if (sms.ok and template) else (None, []))
        veh_note = ""
        if (limit_raw is not None and limit_raw >= LIFEFORM_SLOPE and not args.allow_lifeforms
                and sms.ok):
            proxy = LIFEFORM_PROXY["hostile" if hostile else "friendly"]
            lim2, veh2 = sms.min_max_slope(proxy)
            if lim2 is not None:
                veh_note = ("lifeform template '%s' (max-slope %.2f) replaced by the "
                            "vehicle-only proxy '%s' - the DI-Guy data package is absent and "
                            "the sim crashes on the first human; P11 ran the same "
                            "substitution" % (template, limit_raw, proxy))
                template, limit_raw, veh = proxy, lim2, veh2
        if limit_raw is None:
            limit_raw = MAXSLOPE_FALLBACK_MIN
            veh_note = ("FALLBACK max-slope %.2f - template '%s' did not resolve to vehicles"
                        % (limit_raw, template))
        start, start_src = None, ""
        if chain and uname in chain_pos:
            start, start_src = chain_pos[uname], "end of this unit's previous task's route"
        if start is None:
            start = starts.get(uname)
            start_src = "run trace (DeStack-spread position)"
        if start is None:
            if unit and unit["lat"] is not None:
                start = (unit["lat"], unit["lon"])
                start_src = "authored position (initialization)"
            else:
                start = task["points"][0]
                start_src = "first route vertex"
        route, skipped, drop_note = build_route(task, unit, start, args.drop_origin_meters)
        if chain:
            chain_pos[uname] = route[-1]
        legs = []
        degenerate = 0
        for li in range(len(route) - 1):
            if only_first and li > 0:
                break
            # A chained start that coincides with the task's first vertex produces a leg of a
            # few centimetres. It is not a leg; it is the same point twice (review fix 8).
            if dist_m(route[li][0], route[li][1],
                      route[li + 1][0], route[li + 1][1]) < DEF_MIN_LEG:
                degenerate += 1
                continue
            if progress:
                sys.stderr.write("\r  task %2d/%d %-30s leg %d/%d   "
                                 % (ti + 1, len(tasks), task["name"][:30], li + 1,
                                    len(route) - 1))
                sys.stderr.flush()
            m = analyse_leg(tiles, soil, route[li], route[li + 1], limit_raw,
                            args.step, args.window, args.short_window)
            m["index"] = li + 1
            m["flagged"] = bool(m["ratio"] >= args.threshold and m["length_m"] > args.window
                                and not m["no_verdict"])
            legs.append(m)
        results.append(dict(
            task=task["name"], task_uuid=task["uuid"], unit=uname,
            unit_uuid=task["performer"], action=task["action"], template=template,
            typemap_rule=how, vehicles=sorted({v[0] for v in veh}), limit_raw=limit_raw,
            vehicle_note=veh_note, start=[start[0], start[1]], start_source=start_src,
            dropped_vertices=skipped, drop_note=drop_note, degenerate_legs=degenerate,
            route=[[p[0], p[1]] for p in route], legs=legs))
    if progress:
        sys.stderr.write("\r" + " " * 78 + "\r")
        sys.stderr.flush()
    return results


# --------------------------------------------------------------------------- outputs


def iso_now():
    import datetime
    return datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def xesc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def emit_text(results, args, out=sys.stdout):
    flagged = 0
    noverdict = 0
    for r in results:
        if not r["legs"]:
            out.write("%-34s %-14s  %s\n"
                      % (r["task"][:34], r["unit"], r.get("note", "no legs")))
            continue
        for lg in r["legs"]:
            if lg.get("no_verdict"):
                noverdict += 1
                out.write("%s leg %d (%s): NO VERDICT - tiles missing (%d of %d elevation "
                          "samples NaN, %.1f%%)\n"
                          % (r["task"], lg["index"], r["unit"], lg["nan_samples"],
                             lg["n_samples"], 100.0 * lg["nan_fraction"]))
            elif lg["flagged"]:
                flagged += 1
                out.write("%s leg %d (%s): %.0f m of %.3f on %s at %.4f/%.4f, %.1f km from "
                          "the leg start; %s limit %.3f (max-slope %.2f x %s %.2f); "
                          "PREDICTED IMPASSABLE (pre-flight estimate, ratio %.2f vs "
                          "threshold %.2f)\n"
                          % (r["task"], lg["index"], r["unit"], lg["sustained_window_m"],
                             lg["sustained"], lg["soil"], lg["worst_lat"], lg["worst_lon"],
                             lg["worst_s_m"] / 1000.0, r["template"] or "unit", lg["limit"],
                             lg["limit_raw"], lg["soil"], lg["factor"], lg["ratio"],
                             args.threshold))
            elif args.verbose:
                out.write("%s leg %d (%s): %.0f m leg, worst %.0f m window %.3f on %s, "
                          "limit %.3f, ratio %.2f; ok\n"
                          % (r["task"], lg["index"], r["unit"], lg["length_m"],
                             lg["sustained_window_m"], lg["sustained"], lg["soil"],
                             lg["limit"], lg["ratio"]))
    degen = sum(r.get("degenerate_legs", 0) for r in results)
    out.write("\n%d leg(s) flagged of %d checked across %d task(s); threshold %.2f x the "
              "derated limit on the %.0f m sustained window. %d leg(s) under %.0f m skipped "
              "(a chained start on the task's own first vertex); %d leg(s) got NO VERDICT for "
              "missing tiles.\n"
              % (flagged, sum(len(r["legs"]) for r in results), len(results),
                 args.threshold, args.window, degen, DEF_MIN_LEG, noverdict))
    return flagged


def emit_json(results, args, path):
    doc = dict(
        tool="tools/preflight/leg_check.py", generated=iso_now(),
        parameters=dict(step_m=args.step, sustained_window_m=args.window,
                        short_window_m=args.short_window, threshold=args.threshold,
                        drop_origin_meters=args.drop_origin_meters,
                        min_leg_m=DEF_MIN_LEG, max_nan_fraction=DEF_MAX_NAN,
                        typemap=os.path.basename(args.typemap),
                        elevation="TMS %d L%d %s" % (ELEV_DS, ELEV_LEVEL,
                                                     "nearest" if args.elev_nearest
                                                     else "bilinear"),
                        landcover=["TMS %d L%d %s" % s for s in LC_SOURCES]),
        tasks=results)
    with open(path, "w", encoding="ascii", newline="\r\n") as fh:
        json.dump(doc, fh, indent=1)
        fh.write("\n")


def emit_c2sim(results, args, path):
    """EMITTER SKELETON (not wired into the interface yet).

    One <ReportBody> per flagged leg. Element names and ORDER come from the SDK's generated
    classes (Software/Library/CS/C2SIMSDK/C2SIMSDK/C2SIM_SMX_LOX_V1.0.1.cs: ReportBodyType =
    FromSender/ToReceiver/ReportContent/ReportID/ReportingEntity; ObservationReportContentType
    = Duration/TimeOfObservation/Observation; LocationObservationType = ActorReference/
    ConfidenceLevel/UncertaintyInterval/DirectionOfMovement/Location/Speed;
    NameObservationType = ActorReference/ConfidenceLevel/UncertaintyInterval/
    HostilityStatusCode/Marking/Name/Side; GeodeticCoordinateType = AltitudeAGL/AltitudeMSL/
    Latitude/Longitude), cross-checked against a real captured report
    (runs/20260902T193508Z_run/reports-captured.log) and against
    src/VrfC2SimApp/ReportBuilder.cs.

    TODO (schema): C2SIM 1.0.2 has NO observation type carrying a location AND free text, so
    each flagged leg emits TWO Observations - a LocationObservation for WHERE, and a
    NameObservation whose Marking carries the numbers. If SISO/STP prefer one carrier,
    replace both with the agreed element; no field here is invented beyond that Marking text.
    TODO (addressing): FromSender/ToReceiver are the zero UUID, as everywhere in
    ReportBuilder.cs; the real values belong to the interface's session, not to this tool.
    TODO (wrapper): the interface's PushReportMessage wraps a bare ReportBody in
    MessageBody/DomainMessageBody. This file is a CONCATENATION of bare ReportBody elements
    under a non-schema <PreflightObservationReports> container so it can hold several; send
    them one at a time."""
    zero = "00000000-0000-0000-0000-000000000000"
    now = iso_now()
    n = 0
    with open(path, "w", encoding="ascii", newline="\r\n") as fh:
        fh.write("<!-- Route pre-flight observations, %s, from "
                 "tools/preflight/leg_check.py. Container element is NOT schema; each child "
                 "ReportBody is. -->\n" % now)
        fh.write("<PreflightObservationReports>\n")
        for r in results:
            for lg in r["legs"]:
                if not lg["flagged"]:
                    continue
                n += 1
                marking = ("ROUTE PRE-FLIGHT: task %s leg %d - %.0f m of sustained %.3f "
                           "rise-over-run on %s, %.2f km along the leg; %s limit %.3f "
                           "(max-slope %.2f x soil %.2f). PREDICTED IMPASSABLE (pre-flight "
                           "estimate, ratio %.2f vs threshold %.2f)."
                           % (r["task"], lg["index"], lg["sustained_window_m"],
                              lg["sustained"], lg["soil"], lg["worst_s_m"] / 1000.0,
                              r["template"] or "unit", lg["limit"], lg["limit_raw"],
                              lg["factor"], lg["ratio"], args.threshold))
                fh.write(
                    '  <ReportBody xmlns="%s">\n'
                    '    <FromSender>%s</FromSender>\n'
                    '    <ToReceiver>%s</ToReceiver>\n'
                    '    <ReportContent>\n'
                    '      <ObservationReportContent>\n'
                    '        <TimeOfObservation>\n'
                    '          <DateTime>\n'
                    '            <IsoDateTime>%s</IsoDateTime>\n'
                    '          </DateTime>\n'
                    '        </TimeOfObservation>\n'
                    '        <Observation>\n'
                    '          <LocationObservation>\n'
                    '            <ActorReference>%s</ActorReference>\n'
                    '            <Location>\n'
                    '              <GeodeticCoordinate>\n'
                    '                <AltitudeMSL>%.1f</AltitudeMSL>\n'
                    '                <Latitude>%.6f</Latitude>\n'
                    '                <Longitude>%.6f</Longitude>\n'
                    '              </GeodeticCoordinate>\n'
                    '            </Location>\n'
                    '          </LocationObservation>\n'
                    '        </Observation>\n'
                    '        <Observation>\n'
                    '          <NameObservation>\n'
                    '            <ActorReference>%s</ActorReference>\n'
                    '            <Marking>%s</Marking>\n'
                    '            <Name>%s</Name>\n'
                    '          </NameObservation>\n'
                    '        </Observation>\n'
                    '      </ObservationReportContent>\n'
                    '    </ReportContent>\n'
                    '    <ReportID>%s</ReportID>\n'
                    '    <ReportingEntity>%s</ReportingEntity>\n'
                    '  </ReportBody>\n'
                    % (C2SIM_NS, zero, zero, now, r["unit_uuid"], lg["worst_z_m"],
                       lg["worst_lat"], lg["worst_lon"], r["unit_uuid"], xesc(marking),
                       xesc(r["unit"]), str(uuid.uuid4()), r["unit_uuid"]))
        fh.write("</PreflightObservationReports>\n")
    return n


def emit_overlay(results, args, path):
    """EMITTER SKELETON: a JSON list a later C# consumer feeds to the remote controller's
    overlay-object API (DEMO_READINESS row 20, delivery 3). Per flagged leg: the worst
    window in red, and the whole leg in amber for context."""
    items = []
    for r in results:
        for lg in r["legs"]:
            if not lg["flagged"]:
                continue
            items.append(dict(
                label="%s leg %d: %.0f m at %.2f on %s (limit %.2f)"
                      % (r["task"], lg["index"], lg["sustained_window_m"], lg["sustained"],
                         lg["soil"], lg["limit"]),
                color="red",
                points=[[round(p[0], 6), round(p[1], 6)] for p in lg["worst_seg"]]))
            items.append(dict(
                label="%s leg %d (whole leg, %s)" % (r["task"], lg["index"], r["unit"]),
                color="amber",
                points=[[round(lg["start"][0], 6), round(lg["start"][1], 6)],
                        [round(lg["end"][0], 6), round(lg["end"][1], 6)]]))
    with open(path, "w", encoding="ascii", newline="\r\n") as fh:
        json.dump(items, fh, indent=1)
        fh.write("\n")
    return len(items)


# --------------------------------------------------------------------------- selftest

# The land-cover controls lcctl.py used, with the class values it got from each tileset
# (0 = the tileset has no data there). These check the TILE MATH - tile index, pixel index,
# TMS y-origin - independently of the soil chain. Note the vendor catalogues map no water
# class at all (every water row in layer.*.online.xml is commented out), so Lake Tahoe and
# the open Pacific legitimately resolve to no soil: water is carried by the water layers.
SELFTEST_LC = [
    ("Pacific Ocean off SF", 37.0, -125.0, {154: 0, 165: 0, 188: 200}),
    ("Downtown Los Angeles", 34.0522, -118.2437, {154: 12, 165: 24, 188: 50}),
    ("Lake Tahoe", 39.0968, -120.0324, {154: 18, 165: 11, 188: 80}),
]
SELFTEST_ELEV = (34.65607, -116.76144, 1585.6, 0.5)
SELFTEST_SLOPES = {"Tank Headquarters Section (USA)": 0.94, "Tank Company (USA)": 0.94,
                   "Tank Platoon (USA)": 0.94, "M1A2_Abrams_MBT": 0.94,
                   "M577A2_Command_Post": 1.0}


def selftest(tiles, soil, sms):
    bad = 0
    print("--- tile math: land cover (the lcctl.py controls, raw class values) ---")
    for name, la, lo, want in SELFTEST_LC:
        cells = []
        for ds, level, label in LC_SOURCES:
            v = tiles.landcover_value(ds, level, la, lo)
            good = (v == want[ds])
            bad += 0 if good else 1
            cells.append("%s L%d=%s%s" % (label, level, v, "" if good else
                                          "(EXPECTED %s)" % want[ds]))
        print("  %-22s %s" % (name, "  ".join(cells)))
    got = soil.classify(tiles, SELFTEST_ELEV[0], SELFTEST_ELEV[1])
    ok = (got["soil"] == "sand" and got["source"] == "CA FVEG 15m" and got["value"] == 30)
    bad += 0 if ok else 1
    print("  %-22s %-17s value=%-4s %-20s -> %-12s factor %.2f  %s"
          % ("1-35 freeze point", got["source"], got["value"], got["desc"][:20], got["soil"],
             got["factor"], "OK" if ok else "MISMATCH (expected CA FVEG 15m 30 Sagebrush)"))
    la, lo, want, tol = SELFTEST_ELEV
    z = tiles.elev(la, lo)
    ok = abs(z - want) <= tol
    bad += 0 if ok else 1
    print("--- tile math: elevation ---")
    print("  %.5f/%.5f  L%d bilinear = %.2f m (expected %.1f +/- %.1f)  %s"
          % (la, lo, ELEV_LEVEL, z, want, tol, "OK" if ok else "MISMATCH"))
    ew, ns = tiles.posting_m(la)
    print("  posting at this latitude: %.2f m E-W x %.2f m N-S (FINDING sec 7: 7.86 x 9.56)"
          % (ew, ns))
    print("--- soil chain sources ---")
    for s in soil.sources:
        print("  " + s)
    if not soil.sources:
        print("  NONE - vendor/shared data not readable")
        bad += 1
    print("--- vehicle limits (vendor .entity resolution) ---")
    for tpl, want_slope in sorted(SELFTEST_SLOPES.items()):
        lim, veh = sms.min_max_slope(tpl)
        ok = (lim is not None and abs(lim - want_slope) < 1e-9)
        bad += 0 if ok else 1
        print("  %-32s min max-slope %-6s over %2d vehicle(s): %-46s %s"
              % (tpl, lim, len(veh), ", ".join(sorted({v[0] for v in veh}))[:46],
                 "OK" if ok else "MISMATCH (expected %s)" % want_slope))
    assumed = sorted(k for k, (_s, a) in SOIL_BRIDGE.items() if a)
    print("--- ASSUMED rows of the DtSoilType -> DtRoughnessSoilType bridge (%d of %d) ---"
          % (len(assumed), len(SOIL_BRIDGE)))
    print("  " + ", ".join(assumed))
    print("\nSELFTEST %s (%d problem(s)); tiles fetched this run: %d"
          % ("PASS" if bad == 0 else "FAIL", bad, tiles.fetched))
    return 0 if bad == 0 else 1


# --------------------------------------------------------------------------- calibration


def first_tasks(tasks, units, wanted):
    out, seen = [], set()
    for t in tasks:
        u = units.get(t["performer"])
        nm = u["name"] if u else None
        if nm in wanted and nm not in seen:
            seen.add(nm)
            out.append(t)
    return out


def truth_for(res, verify_run):
    """-> {unit: verify_leg dict} for the leg-1 of each result, derived from the run trace."""
    leaders = leaders_from_log(verify_run)
    tracks = leader_tracks(verify_run, leaders)
    out = {}
    for r in res:
        if not r["legs"] or len(r["route"]) < 2:
            continue
        out[r["unit"]] = verify_leg(tracks.get(r["unit"], []),
                                    tuple(r["route"][0]), tuple(r["route"][1]))
    return out


def margin_of(rows, with_crawlers=False):
    """rows: [(result, leg, truth)] -> (margin, lowest frozen, highest mover) or None.
    A CRAWLING unit is neither a clean pass nor a stop, so by default it is excluded from both
    ends and printed separately; with_crawlers counts it as a mover, which is the stricter
    reading and the one that kills the separation at an 80 m window."""
    froze = [x[1]["ratio"] for x in rows if x[2]["label"] == "FROZE"]
    moved = [x[1]["ratio"] for x in rows if x[2]["label"] == "MOVED"
             and (with_crawlers or x[2]["detail"] != "crawling")]
    if not froze or not moved:
        return None
    return min(froze) - max(moved), min(froze), max(moved)


def sensitivity(args, soil, sms, tmap, units, sides, tasks, starts, wanted):
    """The window/step/interpolation grid behind DEF_WINDOW: 40 m is an OPERATING POINT."""
    ft = first_tasks(tasks, units, wanted)
    print("\nSENSITIVITY - margin = lowest frozen ratio - highest mover ratio, on the labels")
    print("derived from %s. Each cell is EXCLUDING the crawling unit /"
          % os.path.basename(args.verify_run.rstrip("\\/")))
    print("COUNTING it as a mover. Negative = the metric no longer separates.\n")
    print("%-9s %-5s %14s %14s %14s" % ("interp", "step", "w=40", "w=55", "w=80"))
    print("-" * 62)
    truth = None
    worst = None
    for nearest in (False, True):
        for step in (4.0, 8.0, 16.0):
            t2 = Tiles(args.cache, offline=args.offline, nearest=nearest)
            cells = []
            for window in (40.0, 55.0, 80.0):
                a2 = argparse.Namespace(**vars(args))
                a2.step, a2.window = step, window
                res = run_preflight(a2, t2, soil, sms, tmap, units, sides, ft, starts,
                                    only_first=True, unit_filter=set(wanted), progress=True,
                                    chain=False)
                if truth is None:
                    truth = truth_for(res, args.verify_run)
                rows = [(r, r["legs"][0], truth[r["unit"]]) for r in res
                        if r["legs"] and r["unit"] in truth]
                m, ma = margin_of(rows), margin_of(rows, True)
                cells.append("%+.3f/%+.3f" % (m[0], ma[0]) if (m and ma) else "n/a")
                if ma and (worst is None or ma[0] < worst[0]):
                    worst = (ma[0], "nearest" if nearest else "bilinear", step, window)
            print("%-9s %-5.0f %14s %14s %14s"
                  % ("nearest" if nearest else "bilinear", step, cells[0], cells[1], cells[2]))
    if worst:
        print("\nworst cell: %+.3f (%s, step %.0f m, window %.0f m) - the window is an "
              "OPERATING POINT,\nnot a constant." % (worst[0], worst[1], worst[2], worst[3]))


def calibrate(args, tiles, soil, sms, tmap, units, sides, tasks, starts):
    """Compare the FIRST leg of each unit's FIRST task (the only one dispatched at order
    time - later tasks are sequenced behind an arrival that never happened) against what
    that unit did in P11.

    With --verify-run RUNDIR the label is DERIVED from that run's own trace (review fix 1):
    the leader's track is projected onto leg 1, and a unit that drove 22 km and then stopped
    on the leg is FROZE, which a net-displacement label cannot see. Without it the superseded
    net labels are used and the table says so."""
    wanted = set(P11_NET_LABELS)
    ft = first_tasks(tasks, units, wanted)
    res = run_preflight(args, tiles, soil, sms, tmap, units, sides, ft, starts,
                        only_first=True, unit_filter=wanted, progress=True, chain=False)
    truth = truth_for(res, args.verify_run) if args.verify_run else {}
    rows = []
    for r in res:
        if not r["legs"]:
            continue
        if truth:
            t = truth.get(r["unit"], dict(label="NO-TRACK", detail="", along_m=0.0,
                                          advance_m=0.0, vertex_m=0.0, end_vertex_m=0.0))
        else:
            lbl, net = P11_NET_LABELS.get(r["unit"], ("?", 0))
            t = dict(label=lbl, detail="net displacement only", along_m=float(net),
                     advance_m=float("nan"), vertex_m=float("nan"),
                     end_vertex_m=float("nan"))
        rows.append((r, r["legs"][0], t))
    rows.sort(key=lambda x: -x[1]["ratio"])
    print("CALIBRATION - COA-STP1 first legs against the P11 run (%s)"
          % (os.path.basename(args.verify_run.rstrip("\\/")) if args.verify_run
             else "runs/20260907T150643Z_run, SUPERSEDED net labels"))
    print("start positions: each unit's formation leader's first POS fix in that trace")
    print("typemap: %s;  window %.0f m;  step %.0f m;  %s elevation"
          % (os.path.basename(args.typemap), args.window, args.step,
             "nearest" if args.elev_nearest else "bilinear"))
    if args.verify_run:
        print("labels DERIVED from the trace: FROZE = along-leg advance < %.0f m over the "
              "final %.0f s AND the\n  leader never came within %.0f m of the next route "
              "vertex (minvtx); 'arrived' = it did; 'crawling' =\n  it did not but is still "
              "advancing, by less than %.0f m over that tail.\n"
              % (VERIFY_STALL_M, VERIFY_TAIL_S, VERIFY_ARRIVE_M, VERIFY_CRAWL_M))
    else:
        print("NO --verify-run: labels are the SUPERSEDED net-displacement ones "
              "(they miss a unit that drove far and then froze).\n")
    print("%-12s %-26s %7s %7s %6s %-10s %6s %6s %-14s %8s %7s %8s %8s"
          % ("unit", "task", "leg m", "sust%.0f" % args.window, "w20", "soil", "limit",
             "ratio", "P11 (derived)" if args.verify_run else "P11 (net label)",
             "along m", "adv600", "minvtx", "endvtx"))
    print("-" * 144)
    for r, lg, t in rows:
        lbl = t["label"] + (":" + t["detail"].split()[0] if t["detail"]
                            and t["label"] == "MOVED" else "")
        print("%-12s %-26s %7.0f %7.3f %6.3f %-10s %6.3f %6.2f %-14s %8.0f %7.0f %8.0f %8.0f"
              % (r["unit"][:12], r["task"][:26], lg["length_m"], lg["sustained"], lg["short"],
                 lg["soil"], lg["limit"], lg["ratio"], lbl[:14], t["along_m"],
                 t["advance_m"], t["vertex_m"], t["end_vertex_m"]))
    froze = [x for x in rows if x[2]["label"] == "FROZE"]
    clean = [x for x in rows if x[2]["label"] == "MOVED" and x[2]["detail"] != "crawling"]
    crawl = [x for x in rows if x[2]["detail"] == "crawling"]
    m = margin_of(rows)
    if froze and clean:
        print("\nfrozen legs: ratio %.3f .. %.3f    clean movers: ratio %.3f .. %.3f"
              % (min(x[1]["ratio"] for x in froze), max(x[1]["ratio"] for x in froze),
                 min(x[1]["ratio"] for x in clean), max(x[1]["ratio"] for x in clean)))
        if m and m[0] > 0:
            print("SEPARATION: margin %.3f (lowest frozen %.3f - highest clean mover %.3f); "
                  "midpoint threshold %.2f" % (m[0], m[1], m[2], (m[1] + m[2]) / 2.0))
        else:
            print("NO SEPARATION on this metric: lowest frozen %.3f <= highest clean mover "
                  "%.3f" % (m[1], m[2]))
    if crawl:
        print("excluded from both ends (crawling): %s"
              % ", ".join("%s ratio %.3f, +%.0f m over the last %.0f s"
                          % (x[0]["unit"], x[1]["ratio"], x[2]["advance_m"], VERIFY_TAIL_S)
                          for x in crawl))
    print("\ncurrent --threshold %.2f flags: %s"
          % (args.threshold,
             ", ".join("%s(%s%s)" % (r["unit"], t["label"],
                                     "/" + t["detail"].split()[0] if t["detail"] else "")
                       for r, lg, t in rows if lg["ratio"] >= args.threshold) or "nothing"))
    misses = [r["unit"] for r, lg, t in rows
              if t["label"] == "FROZE" and lg["ratio"] < args.threshold]
    false_alarms = [r["unit"] for r, lg, t in rows
                    if t["label"] == "MOVED" and t["detail"] != "crawling"
                    and lg["ratio"] >= args.threshold]
    print("misses (froze, not flagged): %s; false alarms (clean mover, flagged): %s"
          % (", ".join(misses) or "none", ", ".join(false_alarms) or "none"))
    if args.sensitivity and args.verify_run:
        sensitivity(args, soil, sms, tmap, units, sides, tasks, starts, wanted)
    return res


# --------------------------------------------------------------------------- main


def main(argv=None):
    ap = argparse.ArgumentParser(
        description="C2SIM route pre-flight against the terrain the sim streams")
    ap.add_argument("--order", default=DEF_ORDER)
    ap.add_argument("--init", default=DEF_INIT)
    ap.add_argument("--typemap", default=None,
                    help="default: data/unit-type-map-52.json, or the -nolifeform probe map "
                         "under --calibrate (it is what every run since 2026-09-06 used)")
    ap.add_argument("--vrf-home", default=DEF_VRF)
    ap.add_argument("--shared-data", default=DEF_SHARED)
    ap.add_argument("--cache", default=DEF_CACHE)
    ap.add_argument("--offline", action="store_true", help="cache only; never fetch a tile")
    ap.add_argument("--starts", default=os.path.join(HERE, "starts_P11.csv"),
                    help="CSV unit,lat,lon of ACTUAL start positions (DeStack spreads units)")
    ap.add_argument("--starts-from-run", default=None,
                    help="derive the start positions from a run directory instead")
    ap.add_argument("--no-starts", action="store_true",
                    help="use the authored initialization positions")
    ap.add_argument("--allow-lifeforms", action="store_true",
                    help="do NOT substitute a vehicle platoon for a DI-Guy lifeform template")
    ap.add_argument("--friendly-nation", default="USA")
    ap.add_argument("--opposing-nation", default="RUS")
    ap.add_argument("--step", type=float, default=DEF_STEP)
    ap.add_argument("--window", type=float, default=DEF_WINDOW)
    ap.add_argument("--short-window", type=float, default=DEF_SHORT)
    ap.add_argument("--threshold", type=float, default=DEF_THRESHOLD)
    ap.add_argument("--drop-origin-meters", type=float, default=DEF_DROP_ORIGIN)
    ap.add_argument("--units", default=None, help="comma-separated unit names to check")
    ap.add_argument("--first-leg-only", action="store_true")
    ap.add_argument("--text", action="store_true")
    ap.add_argument("--verbose", action="store_true", help="--text prints passing legs too")
    ap.add_argument("--json", default=None)
    ap.add_argument("--c2sim-observations", default=None)
    ap.add_argument("--vrf-overlay", default=None)
    ap.add_argument("--elev-nearest", action="store_true",
                    help="sample the nearest elevation posting instead of bilinear")
    ap.add_argument("--selftest", action="store_true")
    ap.add_argument("--calibrate", action="store_true")
    ap.add_argument("--verify-run", default=None,
                    help="with --calibrate: DERIVE each unit's label from this run's "
                         "watchvrf-trace.csv instead of the stored net-displacement labels")
    ap.add_argument("--sensitivity", action="store_true",
                    help="with --calibrate --verify-run: print the window/step/interpolation "
                         "grid the default window rests on")
    ap.add_argument("--write-starts", default=None,
                    help="with --starts-from-run: write the derived CSV here and exit")
    args = ap.parse_args(argv)
    if args.typemap is None:
        args.typemap = (DEF_TYPEMAP_NOLIFEFORM
                        if (args.calibrate and os.path.exists(DEF_TYPEMAP_NOLIFEFORM))
                        else DEF_TYPEMAP)

    tiles = Tiles(args.cache, offline=args.offline, nearest=args.elev_nearest)
    soil = SoilChain(args.shared_data, args.vrf_home)
    sms = VendorSms(os.path.join(args.vrf_home, "data", "simulationModelSets", "EntityLevel",
                                 "vrfSim"))
    if not sms.ok:
        sys.stderr.write("WARNING: vendor SMS not found at %s - falling back to max-slope "
                         "%.2f for every unit\n" % (sms.dir, MAXSLOPE_FALLBACK_MIN))

    if args.selftest:
        return selftest(tiles, soil, sms)

    if args.starts_from_run:
        starts, leaders = starts_from_run(args.starts_from_run)
        if args.write_starts:
            with open(args.write_starts, "w", encoding="ascii", newline="\r\n") as fh:
                fh.write("unit,lat,lon,leader,run\n")
                for u in sorted(starts):
                    fh.write("%s,%.6f,%.6f,%s,%s\n"
                             % (u, starts[u][0], starts[u][1], leaders[u][0],
                                os.path.basename(args.starts_from_run.rstrip("\\/"))))
            print("wrote %d start position(s) to %s" % (len(starts), args.write_starts))
            return 0
    elif args.no_starts or not os.path.exists(args.starts):
        starts = {}
    else:
        starts = load_starts_csv(args.starts)

    tmap = TypeMap(args.typemap, args.friendly_nation, args.opposing_nation)
    units, sides = parse_init(args.init)
    tasks = parse_order(args.order)

    if args.calibrate:
        results = calibrate(args, tiles, soil, sms, tmap, units, sides, tasks, starts)
    else:
        results = run_preflight(
            args, tiles, soil, sms, tmap, units, sides, tasks, starts,
            only_first=args.first_leg_only,
            unit_filter=(set(args.units.split(",")) if args.units else None))
        if args.text or not (args.json or args.c2sim_observations or args.vrf_overlay):
            emit_text(results, args)
    if args.json:
        emit_json(results, args, args.json)
        print("wrote %s" % args.json)
    if args.c2sim_observations:
        n = emit_c2sim(results, args, args.c2sim_observations)
        print("wrote %d ObservationReport(s) to %s" % (n, args.c2sim_observations))
    if args.vrf_overlay:
        n = emit_overlay(results, args, args.vrf_overlay)
        print("wrote %d overlay item(s) to %s" % (n, args.vrf_overlay))
    return 0


if __name__ == "__main__":
    sys.exit(main())
