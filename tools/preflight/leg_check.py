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
# AN AO-SPECIFIC DEFAULT, and marked as one. starts_P11.csv holds ten MOJAVE positions from the
# P11 run; paired with another AO's order it silently scores legs thousands of km long that
# nobody drives. check_starts_distance() refuses a start further than DEF_STARTS_MAX_KM from
# every vertex of its own unit's tasks, and an all-refused DEFAULT file stops the tool.
DEF_STARTS = os.path.join(HERE, "starts_P11.csv")
DEF_STARTS_MAX_KM = 100.0     # the interface's own Vrf:MaxVertexFromTaskeeKm (appsettings.json)

C2SIM_NS = "http://www.sisostds.org/schemas/C2SIM/1.1"
NS = {"c": C2SIM_NS}
TMS_BASE = "http://vr-theworld.com/vr-theworld/tiles/1.0.0"

ELEV_DS = 149
# THE LEVEL IS AN AO PROPERTY, NOT A CONSTANT (STP-802). 13 is the deepest DataExtent the
# server serves over the MOJAVE AO (FINDING sec 7) and stays the default, so every published
# Mojave number reproduces exactly. It is NOT universal: measured 2026-09-20, dataset 149
# returns NO DATA at L13 anywhere over the Suwalki Gap (0 of 25 grid samples over the
# SuwalkiN20 box) and serves that ground at L12. With the level pinned every Suwalki leg came
# back "NO VERDICT - tiles missing" and nothing was ever flagged - a false green. Tiles now
# falls back one level at a time, down to ELEV_MIN_LEVEL, and REMEMBERS what worked per area.
# Override with --elev-level / --elev-min-level; the C# port reads the same pair as
# Vrf:PreflightElevationLevel / Vrf:PreflightElevationMinLevel.
ELEV_LEVEL = 13
ELEV_MIN_LEVEL = 11
TILEPX = 257
SEG = TILEPX - 1

# F2 (cold-start review of f26d4ad, 2026-09-20). *** ABSENT IS NOT FAILED. ***
# This reader and the C# port used to memoise "no tile" for any fetch that produced no usable
# bytes - a 404, a DNS failure, a 5xx and a 60 s timeout alike - and nothing ever cleared it. One
# dropped packet at L13 therefore sent the level cascade to L12 and REMEMBERED L12 for that area,
# so every later verdict over that ground was scored on a DEM the 0.92 threshold was never
# calibrated on, and the only line that fired ("scored at ... COARSER than L13") could not tell the
# server's answer from our own lost packet.
#   ABSENT - the server answered and has no tile (404/410/204, or a 200 with a body too short to be
#            a tile, which is how this TMS says it). A property of the ground: memoised, and the
#            only outcome allowed to drive the cascade.
#   FAILED - we never got the server's answer (curl transport error, timeout, 5xx). A property of
#            the network: counted, retried up to MAX_FETCH_ATTEMPTS per tile, and if it still fails
#            the leg gets NO VERDICT and a loud line - never a quieter score one level down.
# The C# TileSource makes exactly the same distinction with the same attempt bound, so the two
# readers stay equivalent (TileSource.cs, TileOutcome / MaxFetchAttempts).
MAX_FETCH_ATTEMPTS = 3
FETCH_OK, FETCH_ABSENT, FETCH_FAILED = "ok", "absent", "failed"

# (tileset, level, label) in descending ground resolution; the first with data wins. The
# levels are the DEEPEST the server actually serves (probed 2026-09-13: 154 and 165 return
# "no tile" at L13, 188 at L11) - one oversample level above each catalogue's data level.
#
# CLCplus (59, L14) is the EUROPEAN layer the vendor's own terrain composes
# (biomes.landcover.coverage.online.xml:50) and is FIRST because it is the finest: 10 m against
# Copernicus's 100 m. Europe only - probed 2026-09-20 it answers over the Suwalki AO (21
# woodland, 51 grassland) and returns NO TILE over North America, so at Mojave the cascade
# falls through to exactly the three sources it used before and no Mojave verdict changes.
# It also closes the WATER blind spot: CLCplus class 100 is live (preset="Water" -> deep-water,
# acceleration-factor 0.000) while Copernicus's class 80 is COMMENTED OUT of the vendor
# catalogue and resolves to no soil at all.
LC_SOURCES = [(59, 14, "CLCplus 10m"), (154, 12, "CA FVEG 15m"), (165, 12, "NLCD 30m"),
              (188, 10, "Copernicus 100m")]

# The soils of ground-tracked.sysdef's soil-list that are WATER. deep-water is
# acceleration-factor 0.000000 / stopping-factor 0.000000 - a dead stop the vendor reports as
# TaskRunning for ever. A NAME test, not a factor test: a zero factor can also come from a
# catalogue that would not load, and those two must never be confused.
WATER_SOILS = ("deep-water", "shallow-water")

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

# MapGraphicID resolution, mirroring TaskGeometryResolver's own constants (:297, :305).
ORIGIN_COINCIDENCE_M = 100.0   # a vertex this close to the taskee IS the taskee's position
CHAIN_GAP_M = 5000.0           # a graphic further than this from the route end is not chained

# VERBS THAT ISSUE NO VENDOR TASK AND MOVE NOTHING (VerbMapping: TaskIntent.HoldInPlace).
# ExecutePlanPhase is a phase MARKER - "no vendor task; the task is executed at the unit's own
# position and ends at its C2SIM Duration" (VerbMapping.cs:140-142). Chaining a unit's next task
# onto the "end" of one of these is wrong twice over: the unit never went anywhere, and the hold
# carries the phase graphic's coordinates, so the successor was scored from a line the unit was
# never on (ironstorm_cuta_prep_report.md STEP 2, gap 2: T01/T13 are holds and T02/T14 start
# where their taskee stands).
NON_MOVING_ACTIONS = frozenset(["EXECUTEPLANPHASE"])

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


def dist_m_flat(lat1, lon1, lat2, lon2):
    """SF-F: THE PRODUCT'S OWN METRIC, byte for byte -
    TaskGeometryResolver.DistMeters (src/VrfC2SimApp/TaskGeometryResolver.cs:193-200).

    Equirectangular with a fixed 111,320 m per degree of latitude and cos(a.Lat) - NOT the
    haversine dist_m above. The two disagree by ~0.1-0.3% at Iron Storm's latitude, and the
    assembly rules compare against two HARD edges (ORIGIN_COINCIDENCE_M, CHAIN_GAP_M), so a
    vertex at 99.9 m or a graphic at 4,995 m could in principle classify differently in the
    two tools. It is exported so the resolver pin (--dump-resolved) can run the WHOLE assembly
    under both metrics and show they agree, instead of arguing from the percentage.

    Note the asymmetry the product has: cos is taken at a's latitude only, so
    dist(a,b) != dist(b,a) in general. Reproduced deliberately - a pin that "fixed" it here
    would stop pinning the thing it is pinning.
    """
    meters_per_deg_lat = 111320.0
    d_lat = (lat1 - lat2) * meters_per_deg_lat
    d_lon = (lon1 - lon2) * meters_per_deg_lat * math.cos(math.radians(lat1))
    return math.sqrt(d_lat * d_lat + d_lon * d_lon)


def interp(lat1, lon1, lat2, lon2, f):
    """Linear in lat/lon - exact enough over a few km, and it is the straight line the
    vendor's ground-vehicle-move-to actually drives when no nav mesh covers the leg."""
    return lat1 + (lat2 - lat1) * f, lon1 + (lon2 - lon1) * f


# --------------------------------------------------------------------------- tiles


class Tiles(object):
    """VR-TheWorld TMS reader with an on-disk cache. curl does the fetch: python urllib
    gets 403 from vr-theworld.com."""

    def __init__(self, cache, offline=False, nearest=False,
                 elev_level=ELEV_LEVEL, elev_min_level=ELEV_MIN_LEVEL):
        self.cache = cache
        self.offline = offline
        self.nearest = nearest       # sample the nearest posting instead of bilinear
        # A level below the floor, or a floor above the level, would silently disable the
        # cascade; clamp both and keep the pair ordered.
        self.elev_level = max(1, min(20, int(elev_level)))
        self.elev_min_level = max(1, min(self.elev_level, int(elev_min_level)))
        # AREA -> the level that returned data, or 0 for "no data at any level". An area is one
        # tile AT THE START LEVEL - the finest cell whose coverage the probe actually establishes
        # (see resolve_level; the older comment here said "coarsest" and was stale). Memoised so a
        # blind AO costs ONE probe, not one per sample, and so the loud report is emitted per leg
        # rather than per sample. NOT written when the cascade ended in a FETCH FAILURE (F2): a
        # level we could not establish must not be remembered as one we did.
        self.level_by_area = {}
        self.mem = {}
        self.fetched = 0
        # F2: two different facts, kept apart. `absent` is the server's answer and is permanent;
        # `failures` counts our own unanswered attempts per tile and bounds the retrying.
        self.absent = set()
        self.failures = {}
        # SF3: success bodies refused because they are not a tile in the expected format. None of
        # them reached the cache and none was counted as absence.
        self.undecodable = 0
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

    def _fetch(self, ds, level, x, y, ext, minbytes, validate=None):
        """-> (path_or_None, outcome). See the F2 note at MAX_FETCH_ATTEMPTS.

        The HTTP STATUS is read rather than inferred from the body, because `curl -s` prints a
        404's error page to stdout with exit 0 - which the old `len(data) < minbytes` test read as
        "no tile", correctly, but it read a TIMEOUT (exit 28, empty stdout) the same way, which is
        the defect. `-w %{http_code} -o <file>` puts the code on stdout and the body in the file,
        so the transport result (returncode) and the server's answer (code) are separate.

        SF3 (cold-start review of 9d67f97), TWO FIXES, both to agree with the C# reader:
        1. STATUS CLASSIFICATION. This used to call only returncode!=0, code==0, 5xx and 429
           FAILED, so a 403 - the documented behaviour of this server on the wrong headers, the
           reason curl's User-Agent is used at all - fell through and was CACHED AS A TILE and
           counted as fetched, while TileSource.cs called the same 403 FAILED. Now: anything that
           is not 2xx and not 404/410/204 is FAILED, exactly as the C# does.
        2. DECODE BEFORE THE CACHE. `validate(bytes) -> bool` decides before the file is written.
           A success body that is not a tile in this format is FAILED: never written to disk,
           never memoised as absence. Write-then-decode let one proxy error page poison the shared
           cache directory permanently, for this reader AND for the C# one."""
        key = (ds, level, x, y)
        fn = self._file(ds, level, x, y, ext)
        if os.path.exists(fn) and os.path.getsize(fn) >= minbytes:
            return fn, FETCH_OK
        # OFFLINE IS ABSENCE, DELIBERATELY: --offline means "score only what the cache holds", so a
        # tile that is not cached is not coming and the cascade may fall through to a level that
        # IS. Calling it FAILED would turn every offline run into a route of no-verdict legs.
        if self.offline or key in self.absent:
            return None, FETCH_ABSENT
        if self.failures.get(key, 0) >= MAX_FETCH_ATTEMPTS:
            return None, FETCH_FAILED
        url = "%s/%d/%d/%d/%d.%s" % (TMS_BASE, ds, level, x, y, ext)
        part = fn + ".part"
        try:
            r = subprocess.run(["curl", "-s", "-m", "60", "-w", "%{http_code}", "-o", part, url],
                               capture_output=True)
        except OSError:
            # curl itself is missing or unrunnable. Every call will fail the same way, so counting
            # it is what stops the retry loop being infinite.
            self.failures[key] = self.failures.get(key, 0) + 1
            return None, FETCH_FAILED
        code = 0
        try:
            code = int((r.stdout or b"").decode("ascii", "ignore").strip() or 0)
        except ValueError:
            code = 0
        data = b""
        if os.path.exists(part):
            try:
                with open(part, "rb") as fh:
                    data = fh.read()
            except OSError:
                data = b""
            try:
                os.remove(part)
            except OSError:
                pass
        if r.returncode != 0 or code == 0:
            # Transport error (timeout 28, connect 7, DNS 6, ...): we never got an answer at all.
            # Transient by assumption: counted and retried, never remembered as absence.
            self.failures[key] = self.failures.get(key, 0) + 1
            return None, FETCH_FAILED
        if code in (404, 410, 204):
            # The server's own answer: no tile here. Definitive and memoised - this is the one
            # outcome the level cascade is entitled to act on. Checked BEFORE the 2xx test, as
            # TileSource.Bytes does.
            self.absent.add(key)
            return None, FETCH_ABSENT
        if not (200 <= code < 300):
            # 403, 5xx, 429, a redirect we did not follow: the server did not answer the QUESTION.
            # Same treatment as a transport error - and the same as the C# reader's
            # `!resp.IsSuccessStatusCode`.
            self.failures[key] = self.failures.get(key, 0) + 1
            return None, FETCH_FAILED
        if len(data) < minbytes:
            # A 2xx with a body too small to be a tile IS this TMS's "no tile".
            self.absent.add(key)
            return None, FETCH_ABSENT
        if validate is not None and not validate(data):
            # SF3: a success body that is not a tile. Not absence, and never cached.
            self.undecodable += 1
            self.failures[key] = self.failures.get(key, 0) + 1
            return None, FETCH_FAILED
        with open(fn, "wb") as fh:
            fh.write(data)
        self.fetched += 1
        return fn, FETCH_OK

    def _decodes(self, data):
        """SF3: do these bytes open as an image this reader can use? The same question the C#
        DecodesAsElevation / DecodesAsCover ask, through the same decoder this reader will use."""
        try:
            import io
            im = self.Image.open(io.BytesIO(data))
            im.load()
            return True
        except Exception:
            return False

    # ---- elevation ----
    def _elev_tile2(self, level, x, y):
        """-> (decoded_tile_or_None, outcome).

        F2, second half: the DECODE is memoised too, and a dict that has stored None for a FAILED
        fetch would freeze that failure exactly as the old `failed` set did - so a FAILED outcome
        is not stored at all, which is what makes the retry in _fetch reachable on the next
        sample.

        SF3: a body that arrived but WILL NOT DECODE is FAILED, not absent. This line used to say
        FETCH_ABSENT, which is how a poisoned cache file - written by an older build, by the older
        C# reader, or by any tool sharing this directory - was read back as "the server has no tile
        here" on every cache hit, permanently and silently. It is not an answer about the ground:
        the cascade stops, nothing is memoised, and the leg gets NO VERDICT."""
        key = (ELEV_DS, level, x, y)
        if key in self.mem:
            data = self.mem[key]
            return data, (FETCH_OK if data is not None else FETCH_ABSENT)
        fn, outcome = self._fetch(ELEV_DS, level, x, y, "tif", 1000, validate=self._decodes)
        data = None
        if fn:
            try:
                im = self.Image.open(fn)
                data = list(im.get_flattened_data() if hasattr(im, "get_flattened_data")
                            else im.getdata())
            except Exception:
                data = None
            if data is None:
                self.undecodable += 1
                outcome = FETCH_FAILED
        if outcome != FETCH_FAILED:
            self.mem[key] = data
        return data, outcome

    def _elev_tile(self, level, x, y):
        return self._elev_tile2(level, x, y)[0]

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

    @staticmethod
    def _tile_index(level, lat, lon):
        """The (x, y) of the tile that OWNS a point at a level."""
        p = (180.0 / (2 ** level)) / SEG
        return (int(math.floor((lon + 180.0) / p)) // SEG,
                int(math.floor((lat + 90.0) / p)) // SEG)

    def resolve_level(self, lat, lon):
        """-> (level, fetch_failed). The level this AREA is served at, or 0 when no level of the
        cascade has a tile (fetch_failed False) or when a fetch never got an answer at all
        (fetch_failed True).

        The AREA is one tile AT THE START LEVEL - the finest cell whose coverage the probe
        actually establishes. A coarser cell would let one tile's presence decide for its
        neighbours, so an AO where the start level is PARTIAL would keep scoring the holes as
        NaN instead of falling back - the very failure this cascade exists to remove.

        The probe asks only whether the tile that OWNS the point decodes; the neighbour-tile
        fallback in _elev_sample is a sampling detail and deliberately not part of the
        decision, so the level a leg is scored at is a property of the ground and not of
        which tile edge it happened to clip.

        F2: a level that FAILED to fetch STOPS the cascade and is NOT memoised. Falling through to
        the next level down on a failed fetch is the silent downgrade F2 names - it would score the
        ground one level coarser for a reason that has nothing to do with the ground - and
        remembering that conclusion would make one dropped packet permanent for the process. So
        ABSENT at L means "try L-1", while FAILED at L means we do not know whether the server has
        a tile at L and therefore cannot conclude anything about L-1 either: return 0 with
        fetch_failed set, remember nothing, and let the caller shout."""
        key = self._tile_index(self.elev_level, lat, lon)
        if key in self.level_by_area:
            return self.level_by_area[key], False
        found = 0
        for level in range(self.elev_level, self.elev_min_level - 1, -1):
            x, y = self._tile_index(level, lat, lon)
            tile, outcome = self._elev_tile2(level, x, y)
            if tile is not None:
                found = level
                break
            if outcome == FETCH_FAILED:
                return 0, True
        self.level_by_area[key] = found
        return found, False

    def elev_at(self, lat, lon, level):
        """Terrain height at ONE GIVEN level - no cascade. NaN where the tile is missing."""
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

    def elev3(self, lat, lon):
        """-> (height, level used, fetch_failed). level 0 = no usable data, height NaN with it;
        fetch_failed True means the NaN is OUR failure and not the server's answer (F2), and the
        leg it belongs to gets NO VERDICT rather than a quieter score one level coarser."""
        level, failed = self.resolve_level(lat, lon)
        if level == 0:
            return float("nan"), 0, failed
        return self.elev_at(lat, lon, level), level, False

    def elev2(self, lat, lon):
        """-> (height, level used). The F2-unaware form, kept for the probe/sensitivity paths
        that only want a number."""
        z, level, _ = self.elev3(lat, lon)
        return z, level

    def elev(self, lat, lon, level=None):
        """The cascade form. Pass an explicit level to pin one (the --level-sensitivity grid)."""
        if level is not None:
            return self.elev_at(lat, lon, level)
        return self.elev2(lat, lon)[0]

    @staticmethod
    def posting_m(lat, level=ELEV_LEVEL):
        p = (180.0 / (2 ** level)) / SEG
        return p * 111320.0 * math.cos(math.radians(lat)), p * 111320.0

    @staticmethod
    def window_postings(lat, level, window_m):
        """How many native postings the sustained window spans - the one number that says what
        a coarser DEM does to a verdict. The window is a sliding mean UPHILL grade, so relief
        shorter than a posting is AVERAGED AWAY before the scorer sees it."""
        ew, ns = Tiles.posting_m(lat, level)
        return (window_m / ew if ew > 0 else 0.0, window_m / ns if ns > 0 else 0.0)

    @staticmethod
    def calibration_note(lat, level, window_m, threshold):
        ew, ns = Tiles.posting_m(lat, level)
        pew, pns = Tiles.window_postings(lat, level, window_m)
        if level >= ELEV_LEVEL:
            caveat = "the level the %.2f threshold was calibrated on" % threshold
        else:
            caveat = ("COARSER than the L%d the %.2f threshold was calibrated on - the sustained "
                      "window is averaged over fewer postings, so a real face reads LOWER and a "
                      "flag can be MISSED (never invented)" % (ELEV_LEVEL, threshold))
        return ("elevation L%d at %.2f N: posting %.1f m E-W x %.1f m N-S, so the %.0f m "
                "sustained window spans %.1f x %.1f postings - %s"
                % (level, lat, ew, ns, window_m, pew, pns, caveat))

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
            fn, outcome = self._fetch(ds, level, x, y, "png", 100, validate=self._decodes)
            im = None
            if fn:
                try:
                    im = self.Image.open(fn)
                    im.load()
                except Exception:
                    im = None
                if im is None:
                    # SF3: a cached body that will not decode is FAILED, not absent (as in
                    # _elev_tile2) - so it is not memoised and the failure stays visible.
                    self.undecodable += 1
                    outcome = FETCH_FAILED
            # F2: do not memoise a FAILED fetch - an unanswered request is not evidence that this
            # tileset has no class here, and freezing it would silently move the land-cover cascade
            # on to a coarser source for the rest of the process.
            if outcome != FETCH_FAILED:
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
    """-> [dict(name, uuid, performer, affected, action, points, graphic_ids)] in file order.

    graphic_ids is EVERY MapGraphicID the task carries, in order (schema :4080 allows a list).
    The interface PREFERS them over the embedded Location (TaskGeometryResolver, user ruling
    of 2026-09-14 "precedence MapGraphicID > embedded"); before this the tool had no
    MapGraphicID handling at all - zero occurrences in the file - so on the Iron Storm export
    it reported "no route points in the order" for tasks that do have geometry and scored the
    WRONG line for the rest (ironstorm_cuta_prep_report.md STEP 2)."""
    root = ET.parse(path).getroot()
    tasks = []
    for t in root.iter("{%s}ManeuverWarfareTask" % C2SIM_NS):
        pts = []
        for loc in t.findall("c:Location", NS):
            la = loc.findtext(".//c:Latitude", namespaces=NS)
            lo = loc.findtext(".//c:Longitude", namespaces=NS)
            if la and lo:
                pts.append((float(la), float(lo)))
        gids = [(e.text or "").strip() for e in t.findall("c:MapGraphicID", NS)]
        tasks.append(dict(
            name=t.findtext("c:Name", default="", namespaces=NS),
            uuid=t.findtext("c:UUID", default="", namespaces=NS),
            performer=t.findtext("c:PerformingEntity", default="", namespaces=NS),
            affected=t.findtext("c:AffectedEntity", default="", namespaces=NS),
            action=t.findtext("c:TaskActionCode", default="", namespaces=NS),
            graphic_ids=[g for g in gids if g],
            points=pts))
    return tasks


# The five C2SIM elements that carry a referenceable tactical graphic, and the KIND each one
# contributes (OrderParser.CollectGraphics). TacticalArea is an AREA - a place to go to, reduced
# to its centroid; everything else is a LINE - a path - unless it resolves to a single vertex, in
# which case it is a POINT. Graphics live in the ORDER as well as the initialization: the schema
# gives OrderBodyType its own Entity list before its Task list (xsd:2960-2977), and the real STP
# export uses that exclusively (34 of Iron Storm's 35 references name a graphic carried in the
# order; ZERO name an init graphic).
GRAPHIC_ELEMENTS = ("Route", "Boundary", "Point", "TacticalArea", "TaskGraphic")


def parse_graphics(*paths):
    """-> {uuid: dict(uuid, name, element, kind, points)} over every file given.

    FIRST PUBLISHER WINS, like the interface's own registry, except that an INITIALIZATION
    overwrites an order graphic under the same uuid ("the init is the shared world every order
    is written against"). Pass the order first and the init second to reproduce that.
    """
    graphics = {}
    for path in paths:
        if not path or not os.path.exists(path):
            continue
        root = ET.parse(path).getroot()
        for element in GRAPHIC_ELEMENTS:
            for g in root.iter("{%s}%s" % (C2SIM_NS, element)):
                uuid = (g.findtext("c:UUID", default="", namespaces=NS) or "").strip()
                if not uuid:
                    continue
                pts = []
                for geo in g.iter("{%s}GeodeticCoordinate" % C2SIM_NS):
                    la = geo.findtext("c:Latitude", namespaces=NS)
                    lo = geo.findtext("c:Longitude", namespaces=NS)
                    if la and lo:
                        pts.append((float(la), float(lo)))
                if not pts:
                    continue          # nothing to resolve to; the task falls back to its Location
                kind = ("area" if element == "TacticalArea"
                        else "point" if len(pts) == 1 else "line")
                graphics[uuid] = dict(uuid=uuid, name=g.findtext("c:Name", default="",
                                                                 namespaces=NS),
                                      element=element, kind=kind, points=pts)
    return graphics


def centroid(pts):
    return (sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts))


def probe(probes, threshold, d, what):
    """SF-F instrumentation: record one comparison the assembly made against a HARD edge.

    Doing nothing when `probes` is None is deliberate - run_preflight and every other caller
    pay nothing for this; only --dump-resolved passes a list. What it collects is the quantity
    the SF-F question is actually about: not "how far apart are the two metrics in percent",
    but "how close did any real decision on this fixture come to flipping".
    """
    if probes is not None:
        probes.append((what, float(threshold), float(d), abs(float(d) - float(threshold))))


def assemble_route_from_graphics(resolved, taskee_pos, distfn=dist_m, probes=None):
    """Mirror TaskGeometryResolver.AssembleRoute (src/VrfC2SimApp/TaskGeometryResolver.cs:377).

    distfn: SF-F. The product measures with TaskGeometryResolver.DistMeters (flat earth,
    111,320 m/deg, cos at the FIRST argument's latitude); this tool's own dist_m is haversine.
    Passing dist_m_flat re-runs the WHOLE assembly under the product's metric, which is how
    --dump-resolved shows that the ~0.1-0.3% difference changes no classification on the one
    fixture that exercises these rules. Every call site below therefore goes through distfn,
    and the ARGUMENT ORDER of each one matches the C# call it mirrors (the product's metric is
    not symmetric - cos is taken at the first argument's latitude - so the order is load-bearing
    for the pin, not a style choice).

    THE ROLES, which is the whole point of resolving by reference rather than by document order:
      * an AREA is ONE destination - its centroid. An area is a place to go to, not a path.
      * a POINT is ONE destination - its own vertex.
      * a LINE is a PATH and contributes its vertices in order.
    Then, in order:
      1. every vertex of a line that IS the taskee's own position is DROPPED, leading or
         interior (SF9: Iron Storm's GroundAttackAxi_116_ABCT_SLOT2 has the unit's own
         coordinate as its THIRD of four vertices, so the graphic alone sends it 45 km out,
         back, and out again);
      2. the lines are chained from the taskee outwards by nearest end, reversed when the tail
         is nearer than the head, and a line whose nearer end is more than CHAIN_GAP_M from the
         route is NOT chained (the interface does not invent a leg the order did not describe);
      3. at most ONE destination is appended, LAST, never as a waypoint;
      4. a route that returns within ORIGIN_COINCIDENCE_M of the start after leaving it is
         TRUNCATED there.
    -> (points, notes).
    """
    route, notes = [], []
    if not resolved:
        return route, notes
    lines, destinations = [], []
    for g in resolved:
        if g["kind"] == "area":
            destinations.append((g, centroid(g["points"]),
                                 "area, %d vertices" % len(g["points"])))
        elif g["kind"] == "point" or len(g["points"]) == 1:
            destinations.append((g, g["points"][0], "%s, 1 vertex" % g["kind"]))
        else:
            pts = list(g["points"])
            dropped = 0
            if taskee_pos:
                # SF-G: REMOVED FROM THE TAIL DOWN, exactly as TaskGeometryResolver.cs:420-421
                # does it ("for i = pts.Count-1; i >= 0 && pts.Count > 1; i--"). The head-first
                # loop that stood here agreed with the product on every mixed case and NOT on
                # the degenerate one: when EVERY vertex of a line is the taskee's own position
                # the product keeps pts[0] and this kept pts[-1]. Latent on today's fixtures and
                # still drift in the exact rule this function exists to mirror.
                for i in range(len(pts) - 1, -1, -1):
                    if len(pts) <= 1:
                        break
                    dv = distfn(pts[i][0], pts[i][1], taskee_pos[0], taskee_pos[1])
                    probe(probes, ORIGIN_COINCIDENCE_M, dv, "vertex-is-the-taskee")
                    if dv <= ORIGIN_COINCIDENCE_M:
                        del pts[i]
                        dropped += 1
            if dropped:
                notes.append("%s '%s': %d vertex(es) dropped - they ARE the taskee's own position"
                             % (g["element"], g["name"], dropped))
            lines.append((g, pts))

    unused = list(lines)
    unchained = []
    while unused:
        frm = route[-1] if route else taskee_pos
        best, best_rev, best_d = 0, False, float("inf")
        for i, (_, pts) in enumerate(unused):
            if frm is None:
                best, best_rev, best_d = 0, False, 0.0
                break
            d_head = distfn(frm[0], frm[1], pts[0][0], pts[0][1])
            d_tail = distfn(frm[0], frm[1], pts[-1][0], pts[-1][1])
            d = min(d_head, d_tail)
            if d < best_d:
                best, best_rev, best_d = i, d_tail < d_head, d
        g, pts = unused.pop(best)
        take = list(reversed(pts)) if best_rev else pts
        if route:
            probe(probes, CHAIN_GAP_M, best_d, "chain-gap")
        if route and best_d > CHAIN_GAP_M:
            unchained.append(g)
            continue
        added = 0
        for p in take:
            if route:
                probe(probes, ORIGIN_COINCIDENCE_M,
                      distfn(route[-1][0], route[-1][1], p[0], p[1]), "vertex-already-covered")
            if route and distfn(route[-1][0], route[-1][1], p[0], p[1]) <= ORIGIN_COINCIDENCE_M:
                continue
            route.append(p)
            added += 1
        notes.append("path from %s '%s' (%d vertices%s): %d vertex(es) joined"
                     % (g["element"], g["name"], len(pts),
                        ", REVERSED for continuity" if best_rev else "", added))
    for g in unchained:
        notes.append("%s '%s' is NOT continuous with the rest of the route (more than %.0f m away) "
                     "and was NOT chained in" % (g["element"], g["name"], CHAIN_GAP_M))

    dest_taken = False
    for g, pt, what in destinations:
        if route:
            probe(probes, ORIGIN_COINCIDENCE_M,
                  distfn(route[-1][0], route[-1][1], pt[0], pt[1]), "route-already-ends-there")
        if route and distfn(route[-1][0], route[-1][1], pt[0], pt[1]) <= ORIGIN_COINCIDENCE_M:
            dest_taken = True
            notes.append("destination '%s' (%s): the route already ends there, not repeated" % (g["name"], what))
            continue
        if dest_taken:
            notes.append("destination '%s' (%s) IGNORED - a task is in ONE place" % (g["name"], what))
            continue
        route.append(pt)
        dest_taken = True
        notes.append("destination '%s' (%s): appended LAST, never as a waypoint" % (g["name"], what))

    if taskee_pos and route:
        left = False
        for i, p in enumerate(route):
            d = distfn(p[0], p[1], taskee_pos[0], taskee_pos[1])
            probe(probes, ORIGIN_COINCIDENCE_M, d, "doubles-back")
            if not left:
                if d > ORIGIN_COINCIDENCE_M:
                    left = True
                continue
            if d <= ORIGIN_COINCIDENCE_M:
                notes.append("the assembled route RETURNS to the taskee's own start at vertex %d of %d "
                             "and is TRUNCATED there" % (i + 1, len(route)))
                del route[i:]
                break
    return route, notes


def resolve_task_geometry(task, units, graphics, distfn=dist_m, probes=None):
    """The PRECEDENCE this tool shares with TaskGeometryResolver.Resolve, isolated so the pin
    and run_preflight cannot drift apart: MapGraphicID(s) that resolve are the geometry, else
    the task's embedded Location, else nothing.

    -> (points, source, resolved_ids, unmatched_ids). source is one of
    'map_graphic' / 'embedded_location' / 'none' - the three GeometrySource values.
    """
    unit = units.get(task["performer"])
    taskee_pos = (unit["lat"], unit["lon"]) if unit and unit["lat"] is not None else None
    gids = task.get("graphic_ids") or []
    resolved_ids = [g for g in gids if graphics and g in graphics and graphics[g]["points"]]
    unmatched = [g for g in gids if g not in resolved_ids]
    if resolved_ids:
        pts, _notes = assemble_route_from_graphics([graphics[g] for g in resolved_ids],
                                                   taskee_pos, distfn=distfn, probes=probes)
        if pts:
            return pts, "map_graphic", resolved_ids, unmatched
    pts = list(task["points"])
    if not pts:
        return [], "none", resolved_ids, unmatched
    return pts, "embedded_location", resolved_ids, unmatched


def dump_resolved(path, order_path, init_path, units, tasks, graphics, out=sys.stdout):
    """SF-G: WRITE THE RESOLVED GEOMETRY OF EVERY TASK, for the C# port to be pinned against.

    THE GAP THIS CLOSES. tools/preflight/leg_check.py is a RE-IMPLEMENTATION of
    TaskGeometryResolver and can drift from it. The existing --preflight-selftest comparison
    runs on COA-STP1 and R9, which carry ZERO MapGraphicIDs - the code asserts that itself - so
    it exercises none of the assembly rules. The one shipped fixture that does exercise them is
    the Iron Storm export, and nothing compared the two implementations on it. This writes the
    Python side's answer; `VrfC2SimApp --preflight-selftest` reads it back and compares the C#
    resolver's answer task by task, field by field.

    NO TERRAIN, NO TILES, NO NETWORK: resolution is pure geometry over the order and the init.

    SF-F IS MEASURED HERE, not argued. The whole assembly is run TWICE - once with this tool's
    haversine dist_m, once with dist_m_flat, which is the product's own equirectangular metric -
    and the two results are compared vertex for vertex. Disagreement is FATAL (exit 3): it would
    mean a task classifies differently in the two tools at one of the assembly's two hard edges
    (ORIGIN_COINCIDENCE_M = 100 m, CHAIN_GAP_M = 5,000 m), and the pin would be pinning the
    wrong thing. The margin - the smallest distance from either threshold over every comparison
    the two metrics make - is written into the reference so the next reader can see how much
    room there actually is.
    """
    rows, fatal = [], []
    probes = []
    for i, task in enumerate(tasks):
        unit = units.get(task["performer"])
        pts_h, src_h, res_h, unm_h = resolve_task_geometry(task, units, graphics, dist_m, probes)
        pts_f, src_f, _res_f, _unm_f = resolve_task_geometry(task, units, graphics, dist_m_flat,
                                                             probes)
        if src_h != src_f or len(pts_h) != len(pts_f) or any(
                a[0] != b[0] or a[1] != b[1] for a, b in zip(pts_h, pts_f)):
            fatal.append("task[%d] '%s': haversine and flat-earth resolution DISAGREE "
                         "(%s/%d vertices vs %s/%d)"
                         % (i, task["name"], src_h, len(pts_h), src_f, len(pts_f)))
        rows.append(dict(
            index=i,
            task=task["name"],
            task_uuid=task["uuid"],
            unit=(unit["name"] if unit else ""),
            unit_uuid=task["performer"],
            source=src_h,
            graphic_ids=list(task.get("graphic_ids") or []),
            resolved_ids=res_h,
            unmatched_ids=unm_h,
            embedded_points=len(task["points"]),
            points=[[p[0], p[1]] for p in pts_h]))
    if fatal:
        for f in fatal:
            sys.stderr.write("FATAL: %s\n" % f)
        sys.stderr.write("FATAL: the resolver reference was NOT written. SF-F is no longer a "
                         "margin question on this fixture - align leg_check.py's dist_m to "
                         "TaskGeometryResolver.DistMeters (the C# side is the product).\n")
        return 3
    # THE MARGIN, at the two hard edges, over every comparison both metrics made.
    margin = {}
    for what, threshold, _d, slack in probes:
        key = "%.0f" % threshold
        if key not in margin or slack < margin[key][0]:
            margin[key] = (slack, what)
    doc = dict(
        generated=iso_now(),
        tool="tools/preflight/leg_check.py --dump-resolved",
        order=os.path.basename(order_path),
        init=os.path.basename(init_path),
        origin_coincidence_m=ORIGIN_COINCIDENCE_M,
        chain_gap_m=CHAIN_GAP_M,
        metric_agreement=dict(
            haversine_vs_flat_earth="identical resolution on every task",
            comparisons_probed=len(probes),
            closest_to_a_threshold_m=dict(
                (k, dict(margin_m=round(v[0], 3), rule=v[1])) for k, v in sorted(margin.items()))),
        tasks=rows)
    text = json.dumps(doc, indent=2, sort_keys=True, ensure_ascii=True)
    with open(path, "w", encoding="ascii", newline="\r\n") as fh:
        fh.write(text)
        fh.write("\n")
    out.write("wrote %d resolved task(s) to %s\n" % (len(rows), path))
    out.write("  haversine and flat-earth (TaskGeometryResolver.DistMeters) agree on EVERY task\n")
    out.write("  %d threshold comparison(s) probed under both metrics; closest approach to an edge:\n"
              % len(probes))
    for k in sorted(margin, key=float):
        out.write("    %s m edge: %.1f m of margin (%s)\n" % (k, margin[k][0], margin[k][1]))
    return 0


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
    coarsest, served, fetch_failures = 0, 0, 0
    for i in range(n):
        f = 0.0 if length == 0 else s[i] / length
        la, lo = interp(a[0], a[1], b[0], b[1], f)
        z, level, failed = tiles.elev3(la, lo)
        if failed:
            fetch_failures += 1
        if level:
            served += 1
            if coarsest == 0 or level < coarsest:
                coarsest = level
        samples.append(dict(s=s[i], lat=la, lon=lo, z=z,
                            soil=soil.classify(tiles, la, lo)))
    # F2: a leg that lost even ONE sample to a FETCH FAILURE has NO VERDICT, whatever the
    # nan-fraction tolerance says and whatever level its other samples resolved at, and its
    # recorded level is 0 - "the coarsest level some samples used" is not a description of a leg
    # nothing could be established for. Same rule and same field in the C# port
    # (PreflightService.ScoreLeg / LegMetrics.ElevationFetchFailures).
    elev_level = 0 if fetch_failures else (coarsest if served else 0)
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
    # WATER, counted over the WHOLE leg and not just the worst window. Water inside the window
    # already derates the limit to zero and flags the leg through the ratio; water anywhere else
    # on the line was invisible, and it is the same dead stop wherever it sits.
    wet = [p for p in samples if p["soil"]["soil"] in WATER_SOILS]
    w0 = wet[0] if wet else None
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
        no_verdict=bool(fetch_failures) or bool(n and float(nan_n) / n > DEF_MAX_NAN),
        elev_level=elev_level, elev_fetch_failures=fetch_failures,
        water_samples=len(wet),
        water_fraction=(float(len(wet)) / n if n else 0.0),
        water_soil=(w0["soil"]["soil"] if w0 else ""),
        water_source=(w0["soil"]["source"] if w0 else ""),
        water_desc=(w0["soil"]["desc"] if w0 else ""),
        water_first=([w0["lat"], w0["lon"]] if w0 else None),
        water_first_s_m=(w0["s"] if w0 else 0.0),
        start=[a[0], a[1]], end=[b[0], b[1]])


# --------------------------------------------------------------------------- orchestration


def run_preflight(args, tiles, soil, sms, tmap, units, sides, tasks, starts,
                  only_first=False, unit_filter=None, progress=True, chain=True,
                  graphics=None):
    """chain: a unit's SECOND and later tasks start at the end of its previous task's route
    (the interface sequences a unit's tasks in declared order, dispatching the next one only
    after the previous has finished), not at its initial position.

    *** A NON-MOVING TASK DOES NOT ADVANCE THE CHAIN *** (NON_MOVING_ACTIONS). A HoldInPlace
    verb issues no vendor task, so the unit is exactly where it was when the next task is
    dispatched; chaining through it scored the successor from a line nobody drove.

    graphics: {uuid: graphic} from parse_graphics. When a task names MapGraphicID(s) that
    resolve, THEY are the geometry and the embedded Location is ignored - the interface's own
    precedence (TaskGeometryResolver, user ruling 2026-09-14)."""
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
        # THE TASKEE'S OWN AUTHORED POSITION - what the MapGraphicID assembly measures
        # "is this vertex the unit itself?" against, exactly as the interface does.
        taskee_pos = (unit["lat"], unit["lon"]) if unit and unit["lat"] is not None else None
        # PRECEDENCE: MapGraphicID(s) that resolve, else the embedded Location.
        geom_pts, geom_src, geom_notes = list(task["points"]), "embedded Location", []
        gids = task.get("graphic_ids") or []
        if gids and graphics:
            resolved = [graphics[g] for g in gids if g in graphics]
            unmatched = [g for g in gids if g not in graphics]
            if resolved:
                geom_pts, geom_notes = assemble_route_from_graphics(resolved, taskee_pos)
                geom_src = ("%d MapGraphicID(s) resolved by ROLE (lines = path, areas/points = "
                            "ONE destination)" % len(resolved))
                if unmatched:
                    geom_notes.append("%d MapGraphicID(s) matched NO registered graphic and were "
                                      "ignored: %s" % (len(unmatched), ", ".join(unmatched)))
                if task["points"]:
                    geom_notes.append("%d embedded Location point(s) IGNORED - MapGraphicID wins "
                                      "(user ruling 2026-09-14)" % len(task["points"]))
            elif gids:
                geom_notes.append("%d MapGraphicID(s) resolved to NOTHING - falling back to the "
                                  "embedded Location: %s" % (len(gids), ", ".join(gids)))
        task = dict(task, points=geom_pts)
        if not geom_pts:
            results.append(dict(task=task["name"], task_uuid=task["uuid"], unit=uname,
                                unit_uuid=task["performer"], legs=[],
                                geometry_source=geom_src, geometry_notes=geom_notes,
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
        # A NON-MOVING TASK LEAVES THE UNIT WHERE IT WAS. Chaining the next task onto this
        # route's end would score it from a line the unit never drove.
        moving = (task.get("action") or "").strip().upper() not in NON_MOVING_ACTIONS
        if chain:
            if moving:
                chain_pos[uname] = route[-1]
            else:
                chain_pos[uname] = start
                geom_notes.append("this task's verb '%s' issues NO vendor task and moves nothing, "
                                  "so the unit's NEXT task starts where this one did, not at this "
                                  "route's end" % task.get("action", ""))
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
            geometry_source=geom_src, geometry_notes=geom_notes, moves_the_unit=moving,
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
    blind = 0
    wet = 0
    for r in results:
        if not r["legs"]:
            out.write("%-34s %-14s  %s\n"
                      % (r["task"][:34], r["unit"], r.get("note", "no legs")))
            continue
        for lg in r["legs"]:
            # LOUD, and BEFORE the verdict line: a leg the cascade found no elevation for at
            # ANY level was never checked, and must never read as clear ground.
            if lg.get("elev_level", ELEV_LEVEL) == 0:
                blind += 1
                out.write("%s leg %d (%s): *** NO ELEVATION DATA AT ANY LEVEL *** dataset %d "
                          "returned no tile from L%d down to L%d anywhere on this leg "
                          "(%.5f,%.5f) -> (%.5f,%.5f). NOTHING about it was checked; it is not "
                          "clear ground.\n"
                          % (r["task"], lg["index"], r["unit"], ELEV_DS, args.elev_level,
                             args.elev_min_level, lg["start"][0], lg["start"][1],
                             lg["end"][0], lg["end"][1]))
            if lg.get("water_samples"):
                wet += 1
                out.write("%s leg %d (%s): WATER ON THE LINE - %d of %d sample(s) classify as "
                          "%s (%s%s), first at %.2f km along (%.5f,%.5f). deep-water is "
                          "acceleration-factor 0.000, so a ground vehicle driven onto it STOPS "
                          "and the task stays TaskRunning. Nothing is refused or altered.\n"
                          % (r["task"], lg["index"], r["unit"], lg["water_samples"],
                             lg["n_samples"], lg["water_soil"], lg["water_source"],
                             (": " + lg["water_desc"]) if lg["water_desc"] else "",
                             lg["water_first_s_m"] / 1000.0,
                             lg["water_first"][0], lg["water_first"][1]))
            if lg.get("elev_fetch_failures"):
                # F2: distinct from "tiles missing" on purpose. The server never answered, so
                # nothing here is a statement about the ground, and the run's ratios for this leg
                # are not quotable. It is NOT rescored one level coarser.
                noverdict += 1
                out.write("%s leg %d (%s): NO VERDICT - THE ELEVATION SERVICE FAILED, it did not "
                          "say 'no data' (%d of %d samples unresolved after %d attempt(s) per "
                          "tile: timeout, connection or 5xx). Not clear ground, not rescored "
                          "coarser, and no shift may be taken on it. Fix the network or pre-warm "
                          "the cache; DISREGARD this leg's ratio.\n"
                          % (r["task"], lg["index"], r["unit"], lg["elev_fetch_failures"],
                             lg["n_samples"], MAX_FETCH_ATTEMPTS))
            elif lg.get("no_verdict"):
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
              "missing tiles; %d leg(s) had NO ELEVATION AT ANY LEVEL; %d leg(s) cross water.\n"
              % (flagged, sum(len(r["legs"]) for r in results), len(results),
                 args.threshold, args.window, degen, DEF_MIN_LEG, noverdict, blind, wet))
    # Which DEM produced these numbers. A ratio quoted without its level is un-anchored, and
    # the posting arithmetic is latitude-dependent - so take the AO's OWN latitude from the
    # first task that actually has a route, not from whichever task happens to be first.
    levels = sorted({lg.get("elev_level", 0) for r in results for lg in r["legs"]} - {0})
    routed = [r for r in results if r.get("route")]
    lat = routed[0]["route"][0][0] if routed else 34.66
    out.write("elevation: dataset %d, cascade L%d -> L%d; level(s) actually used: %s\n"
              % (ELEV_DS, args.elev_level, args.elev_min_level,
                 ", ".join("L%d" % v for v in levels) if levels else "NONE"))
    for v in levels:
        out.write("  %s\n" % Tiles.calibration_note(lat, v, args.window, args.threshold))
    return flagged


def emit_json(results, args, path):
    doc = dict(
        tool="tools/preflight/leg_check.py", generated=iso_now(),
        parameters=dict(step_m=args.step, sustained_window_m=args.window,
                        short_window_m=args.short_window, threshold=args.threshold,
                        drop_origin_meters=args.drop_origin_meters,
                        min_leg_m=DEF_MIN_LEG, max_nan_fraction=DEF_MAX_NAN,
                        typemap=os.path.basename(args.typemap),
                        elevation="TMS %d L%d..L%d %s" % (ELEV_DS, args.elev_level,
                                                          args.elev_min_level,
                                                          "nearest" if args.elev_nearest
                                                          else "bilinear"),
                        elev_level=args.elev_level,
                        elev_min_level=args.elev_min_level,
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
                # AT MOST ONE BODY PER LEG, and WATER OUTRANKS THE GRADE FLAG (the same policy
                # as PreflightReports.BuildForTask). A water sample inside the worst window
                # derates the limit to zero, so the flag sentence would read "sustained 0.03 vs
                # limit 0.000" - true arithmetic, useless English - and water OUTSIDE that
                # window does not flag the leg at all while being the same dead stop.
                if lg.get("water_samples"):
                    marking = ("ROUTE PRE-FLIGHT - WATER ON THE LINE: task %s (%s) leg %d - %d "
                               "of %d sample(s) along this leg classify as %s (%s%s), first at "
                               "%.2f km along. The vendor's own soil table gives deep water "
                               "acceleration-factor 0.000, so a ground vehicle driven onto it "
                               "STOPS and the task stays TaskRunning. This is an ESTIMATE off "
                               "land-cover tiles, not a vendor verdict, and NOTHING was refused "
                               "or altered by it - the leg is reported and dispatched."
                               % (r["task"], r["unit"], lg["index"], lg["water_samples"],
                                  lg["n_samples"], lg["water_soil"], lg["water_source"],
                                  (": " + lg["water_desc"]) if lg["water_desc"] else "",
                                  lg["water_first_s_m"] / 1000.0))
                    lat, lon, alt = lg["water_first"][0], lg["water_first"][1], None
                elif lg["flagged"]:
                    marking = ("ROUTE PRE-FLIGHT: task %s leg %d - %.0f m of sustained %.3f "
                               "rise-over-run on %s, %.2f km along the leg; %s limit %.3f "
                               "(max-slope %.2f x soil %.2f). PREDICTED IMPASSABLE (pre-flight "
                               "estimate, ratio %.2f vs threshold %.2f)."
                               % (r["task"], lg["index"], lg["sustained_window_m"],
                                  lg["sustained"], lg["soil"], lg["worst_s_m"] / 1000.0,
                                  r["template"] or "unit", lg["limit"], lg["limit_raw"],
                                  lg["factor"], lg["ratio"], args.threshold))
                    lat, lon, alt = lg["worst_lat"], lg["worst_lon"], lg["worst_z_m"]
                else:
                    continue
                n += 1
                geo = ""
                if alt is not None:
                    geo += '                <AltitudeMSL>%.1f</AltitudeMSL>\n' % alt
                geo += ('                <Latitude>%.6f</Latitude>\n'
                        '                <Longitude>%.6f</Longitude>\n' % (lat, lon))
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
                    '%s'
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
                    % (C2SIM_NS, zero, zero, now, r["unit_uuid"], geo,
                       r["unit_uuid"], xesc(marking),
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
#
# CLCplus (59) is EUROPE ONLY: None at all three North-American controls, which is exactly the
# property that keeps Mojave verdicts unchanged now that it heads the cascade.
SELFTEST_LC = [
    ("Pacific Ocean off SF", 37.0, -125.0, {59: None, 154: 0, 165: 0, 188: 200}),
    ("Downtown Los Angeles", 34.0522, -118.2437, {59: None, 154: 12, 165: 24, 188: 50}),
    ("Lake Tahoe", 39.0968, -120.0324, {59: None, 154: 18, 165: 11, 188: 80}),
]
SELFTEST_ELEV = (34.65607, -116.76144, 1585.6, 0.5)

# THE EUROPEAN CONTROLS (STP-802, measured 2026-09-20 against the same public tiles).
# They are the positive half of the CLCplus row above - "returns None over America" proves
# nothing on its own, since a source that was never queried also returns None - and the proof
# that the elevation cascade does what it says: dataset 149 has NO L13 tile anywhere over this
# AO and answers at L12, which is the whole reason the level stopped being a constant.
# (lat, lon, CLCplus class, Copernicus class, elevation level, height, tolerance)
SELFTEST_EU = [
    ("Suwalki 56th SBCT", 54.21702, 23.76443, 21, 126, 12, 149.07, 0.5),
    ("Suwalki 278th ACR", 54.19044, 23.58498, 51, 30, 12, 160.46, 0.5),
    ("Suwalki 1-112 IN", 54.04269, 23.30823, 51, 40, 12, 130.11, 0.5),
]

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
    z, lvl = tiles.elev2(la, lo)
    ok = abs(z - want) <= tol and lvl == ELEV_LEVEL
    bad += 0 if ok else 1
    print("--- tile math: elevation ---")
    print("  %.5f/%.5f  L%d bilinear = %.2f m (expected %.1f +/- %.1f at L%d)  %s"
          % (la, lo, lvl, z, want, tol, ELEV_LEVEL, "OK" if ok else "MISMATCH"))
    ew, ns = tiles.posting_m(la)
    print("  posting at this latitude: %.2f m E-W x %.2f m N-S (FINDING sec 7: 7.86 x 9.56)"
          % (ew, ns))

    # ---- the AO-independence controls (STP-802) --------------------------------------------
    print("--- CLCplus + the elevation cascade over EUROPE (STP-802; needs network or a warm "
          "cache) ---")
    if tiles.offline:
        print("  SKIPPED - --offline, and the committed Mojave cache carries no European tile.")
    else:
        for name, la, lo, clc, cop, want_lvl, want_z, tol in SELFTEST_EU:
            v59 = tiles.landcover_value(59, 14, la, lo)
            v188 = tiles.landcover_value(188, 10, la, lo)
            z2, lvl2 = tiles.elev2(la, lo)
            s = soil.classify(tiles, la, lo)
            good = (v59 == clc and v188 == cop and lvl2 == want_lvl
                    and z2 == z2 and abs(z2 - want_z) <= tol
                    and s["source"] == "CLCplus 10m")
            bad += 0 if good else 1
            print("  %-19s CLCplus=%-5s (want %s)  Copernicus=%-5s (want %s)  elev L%-3s "
                  "(want L%d) = %-8s (want %.2f)  soil source %-13s %s"
                  % (name, v59, clc, v188, cop, lvl2, want_lvl,
                     ("%.2f" % z2) if z2 == z2 else "NaN", want_z, s["source"],
                     "OK" if good else "MISMATCH"))
        # THE POINT OF THE WHOLE CHANGE, stated as a check: with the level pinned at 13 the
        # same ground is NaN, so every leg over it used to read "NO VERDICT - tiles missing"
        # and nothing was ever flagged or shifted. Pinned here so the defect cannot come back.
        la, lo = SELFTEST_EU[0][1], SELFTEST_EU[0][2]
        z13 = tiles.elev_at(la, lo, ELEV_LEVEL)
        ok13 = z13 != z13        # NaN
        bad += 0 if ok13 else 1
        print("  %-19s L%d (the OLD pinned level) = %-8s %s"
              % ("same point", ELEV_LEVEL, ("%.2f" % z13) if z13 == z13 else "NO DATA (NaN)",
                 "OK - the fallback is what makes this AO scorable" if ok13
                 else "MISMATCH: L%d answered here, so this control proves nothing" % ELEV_LEVEL))
        for lvl3 in (ELEV_LEVEL, 12):
            print("  %s" % Tiles.calibration_note(SELFTEST_EU[0][1], lvl3, DEF_WINDOW,
                                                  DEF_THRESHOLD))

    # ---- water (the finding CLCplus made possible) -------------------------------------------
    print("--- water detection (deep-water acceleration-factor 0.000 = a dead stop) ---")
    for name, la, lo, _w in SELFTEST_LC:
        s = soil.classify(tiles, la, lo)
        print("  %-22s -> %-13s factor %.2f  water=%s"
              % (name, s["soil"], s["factor"], s["soil"] in WATER_SOILS))
    ok = all(soil.classify(tiles, la, lo)["soil"] in WATER_SOILS
             for name, la, lo, _w in SELFTEST_LC if name.startswith("Pacific"))
    bad += 0 if ok else 1
    print("  the open Pacific classifies as water: %s" % ("OK" if ok else "MISMATCH"))
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
    # ---- F2: ABSENT vs FAILED (pure: a stubbed curl, no tile, no network) -------------------
    # The defect this locks: a transient HTTP failure used to be memoised exactly like a real
    # "no tile", which permanently downgraded that area's elevation level for the process and
    # silently changed verdicts on the terrain the 0.92 threshold was calibrated on. Every check
    # below fails on the pre-F2 code.
    print("--- F2: a failed fetch is NOT an absent tile (stubbed curl, no network) ---")
    import io
    import tempfile

    class _Curl(object):
        """A stand-in for subprocess.run: hands back a queued (returncode, http_code, body)."""
        def __init__(self, script):
            self.script = list(script)
            self.calls = 0

        def __call__(self, argv, **kw):
            self.calls += 1
            rc, code, body = self.script[min(self.calls - 1, len(self.script) - 1)]
            out = argv[argv.index("-o") + 1] if "-o" in argv else None
            if out and body:
                with open(out, "wb") as fh:
                    fh.write(body)
            return subprocess.CompletedProcess(argv, rc, stdout=str(code).encode(), stderr=b"")

    def _f2(label, script, want_outcomes, check=None, calls=None):
        tmp = tempfile.mkdtemp(prefix="f2_")
        t = Tiles(tmp, offline=False)
        real, curl = subprocess.run, _Curl(script)
        try:
            subprocess.run = curl
            got = [t._fetch(ELEV_DS, 13, 1, 1, "tif", 1000)[1] for _ in want_outcomes]
        finally:
            subprocess.run = real
        ok = (got == list(want_outcomes)
              and (calls is None or curl.calls == calls)
              and (check is None or check(t)))
        print("  %-62s %s" % (label, "OK" if ok else
                              "MISMATCH (outcomes %s, curl calls %d)" % (got, curl.calls)))
        return 0 if ok else 1

    _tile = (ELEV_DS, 13, 1, 1)
    bad += _f2("a 404 is ABSENT, memoised, and asked for exactly once",
               [(0, 404, b"")], [FETCH_ABSENT, FETCH_ABSENT],
               check=lambda t: _tile in t.absent and not t.failures, calls=1)
    bad += _f2("a 200 with a body too short to be a tile is ABSENT (this TMS's 'no tile')",
               [(0, 200, b"tiny")], [FETCH_ABSENT],
               check=lambda t: _tile in t.absent)
    bad += _f2("a curl TIMEOUT is FAILED, never memoised as absent, and IS retried",
               [(28, 0, b"")], [FETCH_FAILED, FETCH_FAILED],
               check=lambda t: _tile not in t.absent and t.failures.get(_tile) == 2, calls=2)
    bad += _f2("a 503 is FAILED too - the server did not answer the question",
               [(0, 503, b"")], [FETCH_FAILED],
               check=lambda t: _tile not in t.absent)
    bad += _f2("retrying is BOUNDED: after MAX_FETCH_ATTEMPTS curl is not called again",
               [(28, 0, b"")], [FETCH_FAILED] * (MAX_FETCH_ATTEMPTS + 2),
               check=lambda t: _tile not in t.absent, calls=MAX_FETCH_ATTEMPTS)

    # ---- SF3: a SUCCESS body that is not a tile is FAILED, never cached, never absent ----
    print("--- SF3: a non-tile body is FAILED, never cached (stubbed curl, no network) ---")

    def _png_bytes(w=4, h=4):
        buf = io.BytesIO()
        Tiles.__dict__  # keep the import of Image local to the instance below
        from PIL import Image as _I
        _I.new("L", (w, h), 7).save(buf, format="PNG")
        return buf.getvalue()

    def _sf3(label, script, ext, minbytes, want_outcomes, check=None):
        tmp = tempfile.mkdtemp(prefix="sf3_")
        t = Tiles(tmp, offline=False)
        real, curl = subprocess.run, _Curl(script)
        try:
            subprocess.run = curl
            got = [t._fetch(ELEV_DS, 13, 1, 1, ext, minbytes, validate=t._decodes)[1]
                   for _ in want_outcomes]
        finally:
            subprocess.run = real
        cached = os.listdir(tmp)
        ok = (got == list(want_outcomes) and not cached
              and (check is None or check(t)))
        print("  %-62s %s" % (label, "OK" if ok else
                              "MISMATCH (outcomes %s, cache %s)" % (got, cached)))
        return 0 if ok else 1

    # A captive-portal / proxy error page: a 200, comfortably above minBytes, pure HTML.
    _html = b"<html><head><title>403 Forbidden</title></head><body>" + b"x" * 2000 + b"</body></html>"
    bad += _sf3("an HTML error page served with 200 is FAILED, not ABSENT, and is NOT cached",
                [(0, 200, _html)], "tif", 1000, [FETCH_FAILED],
                check=lambda t: _tile not in t.absent and t.undecodable == 1
                                and t.failures.get(_tile) == 1)
    # A TRUNCATED image: a valid PNG signature, a body that will not load.
    _trunc = _png_bytes(64, 64)[:40] + b"\x00" * 2000
    bad += _sf3("a TRUNCATED image body is FAILED too (the signature is not the test)",
                [(0, 200, _trunc)], "png", 100, [FETCH_FAILED],
                check=lambda t: _tile not in t.absent and t.undecodable == 1)
    # And the status alignment the two readers now share.
    bad += _f2("a 403 is FAILED, exactly as TileSource.Bytes calls it (was: cached as a tile)",
               [(0, 403, b"x" * 4000)], [FETCH_FAILED],
               check=lambda t: _tile not in t.absent and t.failures.get(_tile) == 1)
    bad += _f2("a 429 is FAILED", [(0, 429, b"x" * 4000)], [FETCH_FAILED],
               check=lambda t: _tile not in t.absent)
    bad += _f2("a 410 is ABSENT", [(0, 410, b"")], [FETCH_ABSENT],
               check=lambda t: _tile in t.absent)
    # A REAL tile still lands in the cache - the validator must not refuse everything.
    def _good():
        tmp = tempfile.mkdtemp(prefix="sf3ok_")
        t = Tiles(tmp, offline=False)
        real, curl = subprocess.run, _Curl([(0, 200, _png_bytes(64, 64))])
        try:
            subprocess.run = curl
            fn, outcome = t._fetch(ELEV_DS, 13, 1, 1, "png", 100, validate=t._decodes)
        finally:
            subprocess.run = real
        ok = outcome == FETCH_OK and fn and os.path.exists(fn) and t.undecodable == 0
        print("  %-62s %s" % ("a REAL png IS accepted and IS cached (the validator is not a wall)",
                              "OK" if ok else "MISMATCH (%s, %s)" % (outcome, fn)))
        return 0 if ok else 1
    bad += _good()

    # The cascade itself: ABSENT falls through, FAILED stops and is not remembered.
    def _cascade(label, script, want_level, want_failed, want_memo):
        tmp = tempfile.mkdtemp(prefix="f2c_")
        t = Tiles(tmp, offline=False)
        real, curl = subprocess.run, _Curl(script)
        try:
            subprocess.run = curl
            lvl, failed = t.resolve_level(34.66, -116.6)
        finally:
            subprocess.run = real
        memo = bool(t.level_by_area)
        ok = (lvl == want_level and failed == want_failed and memo == want_memo)
        print("  %-62s %s" % (label, "OK" if ok else
                              "MISMATCH (level %s, failed %s, memoised %s)" % (lvl, failed, memo)))
        return 0 if ok else 1

    bad += _cascade("ABSENT at every level -> level 0, not a failure, and REMEMBERED",
                    [(0, 404, b"")], 0, False, True)
    bad += _cascade("a FAILED fetch STOPS the cascade: level 0, failed, and NOT remembered",
                    [(28, 0, b"")], 0, True, False)
    # The whole point: a failure at the START level must not silently hand back a coarser level.
    bad += _cascade("a FAILED start level is never rescored at the next level down",
                    [(28, 0, b""), (0, 200, b"x" * 4000)], 0, True, False)
    print("  the C# port makes the same distinction with the same bound "
          "(TileSource.TileOutcome, MaxFetchAttempts=%d)" % MAX_FETCH_ATTEMPTS)

    # ---- the AO guard on the DEFAULT starts file (pure: no tile, no network) ----------------
    print("--- the starts file must belong to the same AO as the vertices ---")
    mojave = {"U1": (34.658442, -116.740092)}
    units = {"p1": dict(name="U1")}
    suwalki_task = [dict(performer="p1", points=[(54.19044, 23.58498), (54.17694, 23.54506)])]
    mojave_task = [dict(performer="p1", points=[(34.651212, -116.811637)])]
    buf = io.StringIO()
    kept, dropped = check_starts_distance(dict(mojave), mojave_task, units,
                                          DEF_STARTS_MAX_KM, "starts_P11.csv", True, buf)
    ok = (kept == mojave and dropped == 0 and buf.getvalue() == "")
    bad += 0 if ok else 1
    print("  a Mojave start with Mojave vertices is KEPT, silently: %s"
          % ("OK" if ok else "MISMATCH"))
    buf = io.StringIO()
    kept, dropped = check_starts_distance(dict(mojave), suwalki_task, units,
                                          DEF_STARTS_MAX_KM, "starts_P11.csv", True, buf)
    ok = (kept == {} and dropped == 1 and "REFUSED" in buf.getvalue())
    bad += 0 if ok else 1
    print("  the same start with SUWALKI vertices is REFUSED and said out loud: %s"
          % ("OK" if ok else "MISMATCH"))
    try:
        check_starts_distance(dict(mojave), suwalki_task, units, DEF_STARTS_MAX_KM,
                              "starts_P11.csv", False, io.StringIO())
        ok = False
    except SystemExit as e:
        ok = (e.code == 2)
    bad += 0 if ok else 1
    print("  and an all-refused DEFAULT starts file is FATAL (exit 2): %s"
          % ("OK" if ok else "MISMATCH"))
    buf = io.StringIO()
    kept, dropped = check_starts_distance({"other": (0.0, 0.0)}, suwalki_task, units,
                                          DEF_STARTS_MAX_KM, "x.csv", True, buf)
    ok = (kept == {"other": (0.0, 0.0)} and dropped == 0)
    bad += 0 if ok else 1
    print("  a start for a unit this order never tasks is left alone: %s"
          % ("OK" if ok else "MISMATCH"))

    # ---- MapGraphicID RESOLUTION BY ROLE (pure: no tile, no network) ------------------------
    # The first of the two gaps ironstorm_cuta_prep_report.md STEP 2 found: this file had ZERO
    # occurrences of MapGraphicID, so on the real STP export - where 34 of 35 references name a
    # graphic carried in the ORDER - it reported "no route points in the order" for the tasks
    # whose geometry is a reference, and scored the embedded (lossy, linearised) line for the
    # rest. Every check below fails on the pre-fix code.
    print("--- MapGraphicID resolution by ROLE (lines = path, areas/points = ONE destination) ---")

    def _g(uuid_, kind, pts, name="g", element="Route"):
        return dict(uuid=uuid_, name=name, element=element, kind=kind, points=pts)

    def _chk(label, cond):
        print("  %-74s %s" % (label[:74], "OK" if cond else "MISMATCH"))
        return 0 if cond else 1

    taskee = (54.28000, 23.32000)
    # (a) a LINE is a path: its vertices come through in order.
    line = _g("l1", "line", [(54.30, 23.34), (54.32, 23.36), (54.34, 23.38)])
    pts, _ = assemble_route_from_graphics([line], taskee)
    bad += _chk("a LINE contributes its 3 vertices IN ORDER", pts == line["points"])
    # (b) an AREA is ONE destination - its centroid - and never a path.
    area = _g("a1", "area", [(54.40, 23.40), (54.42, 23.40), (54.42, 23.44), (54.40, 23.44)],
              name="OBJ LANCASTER", element="TacticalArea")
    pts, _ = assemble_route_from_graphics([area], taskee)
    bad += _chk("an AREA reduces to ONE vertex, its centroid",
                len(pts) == 1 and abs(pts[0][0] - 54.41) < 1e-9 and abs(pts[0][1] - 23.42) < 1e-9)
    # (c) a POINT is ONE destination.
    point = _g("p1", "point", [(54.5, 23.5)], element="Point")
    pts, _ = assemble_route_from_graphics([point], taskee)
    bad += _chk("a POINT contributes its own single vertex", pts == [(54.5, 23.5)])
    # (d) line + area: the path first, the destination LAST - never a waypoint in the middle.
    pts, _ = assemble_route_from_graphics([area, line], taskee)
    bad += _chk("line + area: the 3 line vertices come first and the area's centroid is LAST",
                len(pts) == 4 and pts[:3] == line["points"] and abs(pts[3][0] - 54.41) < 1e-9)
    # (e) TWO destinations: a task is in ONE place; the second is ignored, not driven to.
    area2 = _g("a2", "area", [(54.60, 23.60), (54.62, 23.62)], name="OBJ OTHER",
               element="TacticalArea")
    pts, notes = assemble_route_from_graphics([area, area2], taskee)
    bad += _chk("TWO destination graphics -> ONE destination, the other IGNORED and said so",
                len(pts) == 1 and any("IGNORED" in n for n in notes))
    # (f) SF9: a vertex that IS the taskee's own position is dropped, INTERIOR ones included.
    # This is Iron Storm's GroundAttackAxi_116_ABCT_SLOT2, verbatim.
    axis = _g("ax", "line", [(54.40219, 24.04281), (54.38968, 23.96870),
                             (54.28000, 23.32000), (54.37061, 23.99426)],
              name="GroundAttackAxi_116_ABCT_SLOT2")
    pts, notes = assemble_route_from_graphics([axis], taskee)
    bad += _chk("the axis vertex that IS the taskee's own position (3rd of 4) is DROPPED",
                len(pts) == 3 and (54.28000, 23.32000) not in pts
                and any("taskee's own position" in n for n in notes))
    # (f2) SF-G: THE DEGENERATE CASE, where this tool used to disagree with the product.
    # When EVERY vertex of a line is the taskee's own position the rule can only keep one, and
    # which one it keeps is not a matter of taste: TaskGeometryResolver.cs:420-421 removes from
    # the TAIL DOWN while pts.Count > 1, so pts[0] survives. This tool dropped from the head and
    # kept pts[-1]. No shipped fixture reaches it - which is exactly why it went unnoticed, and
    # why it is pinned here rather than left to the next export to discover.
    allmine = _g("am", "line", [(54.28000, 23.32000), (54.28010, 23.32010),
                                (54.28020, 23.32020)], name="every vertex is the unit")
    pts, notes = assemble_route_from_graphics([allmine], taskee)
    bad += _chk("all-coincident line: ONE vertex survives and it is the FIRST (the C# survivor)",
                len(pts) == 1 and pts[0] == (54.28000, 23.32000))
    # (g) a graphic more than CHAIN_GAP_M from the route is not spliced onto it.
    far = _g("f1", "line", [(50.0, 20.0), (50.1, 20.1)], name="somewhere else")
    pts, notes = assemble_route_from_graphics([line, far], taskee)
    bad += _chk("a graphic %.0f km away is NOT chained in, and the tool says so"
                % (CHAIN_GAP_M / 1000.0),
                pts == line["points"] and any("NOT chained" in n for n in notes))
    # (h) a line whose TAIL is nearer the taskee is reversed for continuity.
    rev = _g("r1", "line", [(54.34, 23.38), (54.32, 23.36), (54.30, 23.34)])
    pts, notes = assemble_route_from_graphics([rev], taskee)
    bad += _chk("a line whose TAIL is nearer the taskee is REVERSED for continuity",
                pts == list(reversed(rev["points"])) and any("REVERSED" in n for n in notes))
    # (i) the backstop: a route that comes back through the start is truncated there.
    loop = _g("lp", "line", [(54.30, 23.34), (54.32, 23.36), (54.28000, 23.32000)])
    pts, notes = assemble_route_from_graphics([loop], taskee)
    bad += _chk("a route that RETURNS to the taskee's own start is truncated there",
                all(dist_m(p[0], p[1], taskee[0], taskee[1]) > ORIGIN_COINCIDENCE_M for p in pts))
    # (j) the parser finds a graphic carried by the ORDER, with the right kind.
    gs = parse_graphics(os.path.join(REPO, "data", "STP-IRON-STORM-SYNTHETIC_Order.xml"),
                        os.path.join(REPO, "data", "STP-IRON-STORM-SYNTHETIC_Initialization.xml"))
    kinds = {}
    for g in gs.values():
        kinds[g["kind"]] = kinds.get(g["kind"], 0) + 1
    ts = parse_order(os.path.join(REPO, "data", "STP-IRON-STORM-SYNTHETIC_Order.xml"))
    refs = [gid for t in ts for gid in t["graphic_ids"]]
    bad += _chk("the Iron Storm export: %d graphic(s) parsed %s, %d MapGraphicID reference(s), "
                "%d of them resolve"
                % (len(gs), sorted(kinds.items()), len(refs),
                   sum(1 for r in refs if r in gs)),
                len(gs) > 0 and len(refs) > 0 and sum(1 for r in refs if r in gs) > 0)
    bad += _chk("COA-STP1 carries NO MapGraphicID, so the reference fixture is untouched by all "
                "of the above",
                not any(t["graphic_ids"] for t in parse_order(DEF_ORDER)))

    # ---- THE CHAIN MUST NOT RUN THROUGH A NON-MOVING PREDECESSOR ---------------------------
    print("--- a HoldInPlace predecessor does not move the unit (the chain rule) ---")
    bad += _chk("ExecutePlanPhase is classified as a verb that moves NOTHING",
                "EXECUTEPLANPHASE" in NON_MOVING_ACTIONS)
    bad += _chk("MOVE, ATTACK and SECURE all DO move the unit",
                not any(v in NON_MOVING_ACTIONS for v in ("MOVE", "ATTACK", "SECURE")))
    holds = [t for t in ts if (t["action"] or "").upper() in NON_MOVING_ACTIONS]
    bad += _chk("the Iron Storm export carries %d such task(s), which is why this matters there"
                % len(holds), len(holds) > 0)
    bad += _chk("COA-STP1 carries NONE, so the reference fixture is untouched by the chain rule "
                "too",
                not any((t["action"] or "").upper() in NON_MOVING_ACTIONS
                        for t in parse_order(DEF_ORDER)))

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


def level_sensitivity(args, soil, sms, tmap, units, sides, tasks, starts, out=sys.stdout):
    """THE PER-LEVEL TABLE: the same legs scored at each elevation level, side by side.

    This is the honest answer to "what does a one-level-coarser DEM do to the verdict", and it
    is a MEASUREMENT rather than an argument: the 0.92 threshold was calibrated at L13 over the
    Mojave AO, the Suwalki AO is only served at L12, and the two are not the same instrument.

    Needs no run directory and no ground truth - it reports how the METRIC moves with the DEM,
    which is the part a threshold has to survive. Pair it with --calibrate --verify-run
    --sensitivity (the window/step/interpolation grid) when labels exist for the AO.
    """
    levels = ([int(x) for x in args.levels.split(",")] if args.levels
              else list(range(args.elev_level, args.elev_min_level - 1, -1)))
    per = {}
    for lvl in levels:
        a2 = argparse.Namespace(**vars(args))
        a2.elev_level = a2.elev_min_level = lvl
        t2 = Tiles(args.cache, offline=args.offline, nearest=args.elev_nearest,
                   elev_level=lvl, elev_min_level=lvl)
        per[lvl] = run_preflight(a2, t2, soil, sms, tmap, units, sides, tasks, starts,
                                 only_first=args.first_leg_only,
                                 unit_filter=(set(args.units.split(",")) if args.units else None),
                                 progress=True)
    base = levels[0]
    routed = [r for r in per[base] if r.get("route")]
    lat = routed[0]["route"][0][0] if routed else 34.66
    out.write("\nELEVATION-LEVEL SENSITIVITY - the SAME legs scored at each level of dataset %d.\n"
              % ELEV_DS)
    out.write("The %.2f threshold on a %.0f m window was calibrated at L%d; a coarser DEM can only\n"
              "AVERAGE relief away, never invent it, so the bias is ONE-SIDED - a real face reads\n"
              "LOWER and a flag can be MISSED, never manufactured.\n\n"
              % (args.threshold, args.window, ELEV_LEVEL))
    for lvl in levels:
        out.write("  %s\n" % Tiles.calibration_note(lat, lvl, args.window, args.threshold))
    out.write("\n%-30s %-13s %8s" % ("task", "unit", "len m"))
    for lvl in levels:
        out.write(" %17s" % ("L%d sust/ratio" % lvl))
    out.write("\n" + "-" * (53 + 18 * len(levels)) + "\n")
    worst_drop = None
    for ti, r in enumerate(per[base]):
        for li, _lg in enumerate(r["legs"]):
            out.write("%-30s %-13s %8.0f" % (r["task"][:30], r["unit"][:13], _lg["length_m"]))
            ref = None
            for lvl in levels:
                rr = per[lvl][ti]
                lg = rr["legs"][li] if li < len(rr["legs"]) else None
                if lg is None or lg.get("elev_level", 0) == 0:
                    out.write(" %17s" % "NO DATA")
                    continue
                mark = "F" if lg["flagged"] else ("?" if lg["no_verdict"] else " ")
                out.write(" %16s%s" % ("%.3f/%.3f" % (lg["sustained"], lg["ratio"]), mark))
                if ref is None:
                    ref = lg["ratio"]
                elif ref == ref and lg["ratio"] == lg["ratio"]:
                    d = lg["ratio"] - ref
                    if worst_drop is None or d < worst_drop[0]:
                        worst_drop = (d, r["task"], lg["index"], lvl)
            out.write("\n")
    out.write("\n")
    for lvl in levels:
        legs = [lg for r in per[lvl] for lg in r["legs"]]
        out.write("L%-3d %3d leg(s): %2d flagged, %2d no-verdict, %2d with NO elevation at this "
                  "level, %2d crossing water\n"
                  % (lvl, len(legs), sum(1 for lg in legs if lg["flagged"]),
                     sum(1 for lg in legs if lg["no_verdict"]),
                     sum(1 for lg in legs if lg.get("elev_level", 0) == 0),
                     sum(1 for lg in legs if lg.get("water_samples"))))
    if worst_drop:
        out.write("\nlargest ratio move against L%d: %+0.3f (%s leg %d at L%d).\n"
                  % (base, worst_drop[0], worst_drop[1], worst_drop[2], worst_drop[3]))
    out.write("READ IT AS: the threshold is CONSERVATIVE at the coarser level, not transferred.\n")
    return per


def check_starts_distance(starts, tasks, units, max_km, label, explicit, out=sys.stderr):
    """REFUSE TO PAIR ONE AO'S START POSITIONS WITH ANOTHER AO'S VERTICES.

    tools/preflight/starts_P11.csv is the DEFAULT --starts file and it holds ten MOJAVE
    positions. Run the pre-flight on a Suwalki order without saying otherwise and every unit
    was silently scored from a start 8,000 km from its own route - a leg length no bound in
    this tool ever sees, because the bound lives in the interface and not here.

    -> ({kept starts}, n_dropped). A start further than max_km from EVERY vertex of its unit's
    tasks is DROPPED (the authored initialization position is then used, which is correct) and
    said out loud. The caller decides whether an all-dropped DEFAULT file is fatal.
    """
    if not starts:
        return starts, 0
    by_unit = {}
    for t in tasks:
        u = units.get(t["performer"])
        if not u:
            continue
        by_unit.setdefault(u["name"], []).extend(t.get("points") or [])
    kept, dropped = {}, 0
    for name, (la, lo) in starts.items():
        pts = by_unit.get(name)
        if not pts:
            kept[name] = (la, lo)          # not in this order: nothing to contradict
            continue
        near = min(dist_m(la, lo, p[0], p[1]) for p in pts)
        if near <= max_km * 1000.0:
            kept[name] = (la, lo)
            continue
        dropped += 1
        out.write("START POSITION REFUSED: '%s' in %s is %.0f km from the nearest vertex of its "
                  "own task(s) - more than the %.0f km bound. It belongs to a DIFFERENT AREA OF "
                  "OPERATIONS and pairing it with these vertices would score legs nobody drives. "
                  "The authored initialization position is used instead.\n"
                  % (name, label, near / 1000.0, max_km))
    if dropped and kept == {} and not explicit:
        out.write("\nFATAL: EVERY start position in the DEFAULT file %s was refused. That file is "
                  "the Mojave P11 trace and this order is somewhere else. Pass --no-starts (use "
                  "the authored positions), or --starts <your AO's csv>, or "
                  "--starts-from-run <RUNDIR>. Refusing to guess.\n" % label)
        raise SystemExit(2)
    return kept, dropped


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
    ap.add_argument("--starts", default=None,
                    help="CSV unit,lat,lon of ACTUAL start positions (DeStack spreads units). "
                         "DEFAULT: %s - which holds MOJAVE positions from the P11 run. A start "
                         "further than --starts-max-km from every vertex of its own task(s) is "
                         "REFUSED, and an all-refused default file is fatal: pass --no-starts, "
                         "your own --starts, or --starts-from-run." % DEF_STARTS)
    ap.add_argument("--starts-max-km", type=float, default=DEF_STARTS_MAX_KM,
                    help="how far a paired start may sit from its unit's own vertices before it "
                         "is refused as another AO's (default %.0f, the interface's own "
                         "Vrf:MaxVertexFromTaskeeKm)" % DEF_STARTS_MAX_KM)
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
    ap.add_argument("--no-chain", action="store_true",
                    help="score every task from the unit's INITIAL position instead of chaining "
                         "a unit's later tasks onto the end of its previous task's route. "
                         "(Chaining is still the default and is what the interface does; a task "
                         "whose verb moves nothing never advances the chain either way.)")
    ap.add_argument("--text", action="store_true")
    ap.add_argument("--verbose", action="store_true", help="--text prints passing legs too")
    ap.add_argument("--json", default=None)
    ap.add_argument("--c2sim-observations", default=None)
    ap.add_argument("--vrf-overlay", default=None)
    ap.add_argument("--elev-nearest", action="store_true",
                    help="sample the nearest elevation posting instead of bilinear")
    ap.add_argument("--elev-level", type=int, default=ELEV_LEVEL,
                    help="deepest elevation level to TRY (default %d - the deepest the server "
                         "serves over the Mojave AO; Suwalki is served at 12)" % ELEV_LEVEL)
    ap.add_argument("--elev-min-level", type=int, default=ELEV_MIN_LEVEL,
                    help="floor of the 'no data at L -> try L-1' fallback (default %d). A leg "
                         "that resolves at NO level is reported LOUDLY, never as clear."
                         % ELEV_MIN_LEVEL)
    ap.add_argument("--level-sensitivity", action="store_true",
                    help="score the SAME legs at each elevation level and print the table - the "
                         "measurement behind 'the 0.92 threshold was calibrated at L13'")
    ap.add_argument("--levels", default=None,
                    help="with --level-sensitivity: comma-separated levels (default: the whole "
                         "cascade, --elev-level down to --elev-min-level)")
    ap.add_argument("--dump-resolved", default=None,
                    help="SF-G: write the RESOLVED geometry of every task (MapGraphicID "
                         "assembly, else the embedded Location) to this json and exit. No "
                         "terrain, no tiles, no network. It is the reference "
                         "`VrfC2SimApp --preflight-selftest` pins the C# TaskGeometryResolver "
                         "against on the ONE fixture with MapGraphicIDs.")
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
    # The default starts file is an AO-SPECIFIC default and is now marked as one: an explicit
    # --starts is the user's own choice and is only warned about, the default can be fatal.
    starts_explicit = args.starts is not None
    if args.starts is None:
        args.starts = DEF_STARTS

    tiles = Tiles(args.cache, offline=args.offline, nearest=args.elev_nearest,
                  elev_level=args.elev_level, elev_min_level=args.elev_min_level)
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
    # The ORDER's own graphics first, then the INITIALIZATION's - the init is the shared world
    # every order is written against, so an init graphic wins a uuid collision. The real STP
    # export carries 34 of its 35 referenced graphics in the ORDER and none in the init.
    graphics = parse_graphics(args.order, args.init)

    # SF-G: pure geometry, so it runs BEFORE the start-position check (which is AO-specific and
    # would warn about a fixture it has no CSV for) and touches neither terrain nor the network.
    if args.dump_resolved:
        return dump_resolved(args.dump_resolved, args.order, args.init, units, tasks, graphics)

    starts, _dropped = check_starts_distance(starts, tasks, units, args.starts_max_km,
                                             os.path.basename(args.starts), starts_explicit)

    if args.level_sensitivity:
        level_sensitivity(args, soil, sms, tmap, units, sides, tasks, starts)
        return 0

    if args.calibrate:
        results = calibrate(args, tiles, soil, sms, tmap, units, sides, tasks, starts)
    else:
        results = run_preflight(
            args, tiles, soil, sms, tmap, units, sides, tasks, starts,
            only_first=args.first_leg_only, chain=not args.no_chain, graphics=graphics,
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
