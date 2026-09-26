#!/usr/bin/env python
"""corridor_gate.py - the CORRIDOR gate: abstract-graph connectivity of the sectors a planned
route crosses, from a vrfNavGenerator log.

The area gate (nav_gate.py) fails whenever ANY sector of the area reads < 0.9; on a lake
district (IRONSTORM-CENTRE, Suwalki) that is certain and says nothing about the routes. This
gate asks the load-bearing question: does every sector a leg actually drives through read
>= 0.9, with a graph? Promoted from the 2026-09-20/21 Iron Storm session's scratch
(ironstorm\\cuta\\sector_map.py, paired.py; record part3b.md / part4.md) by lane I1, 2026-09-26.

    python corridor_gate.py <gen.log> [--runtime-config <area>.navRuntimeConfig]
                            [--preset ironstorm-cuta] [--leg LABEL LAT,LON LAT,LON ...]
                            [--step 2] [--gate 0.9] [--json]
    python corridor_gate.py --selftest-gen1 <gen-1 console log of 2026-09-20>

Exit: 0 = every corridor sector >= gate with a graph; 1 = at least one corridor sector below
the gate, graph-less, or degenerate (<= 1 node); 2 = instrument failure (no sectors parsed, no
extent line, a grid the formula cannot reproduce, a leg outside the grid).

METRIC: per sector, ratio = Average Neighbor Node Count / (Average Node Count - 1), parsed by
nav_gate.parse_log (either the "Sector (i,j): xMin.." row or the name row "Sector
ground-platform_<i>_<j>_<tag> has N triangles." keys a sector). Unlike the area gate, a corridor
sector with no report or with <= 1 node FAILS here: a unit cannot plan through it.

GRID: cells are 43 m, indexed floor(metres / 43) in the area's local frame; the log's
"CalculateTransitionPointLocations extent = (xmin,ymin,z,xmax,ymax,z)" gives the cell range.
Sectors are a FIXED stride of cells (floor(cells / sectors-per-axis)), the last one keeping the
remainder - fitted and validated on AO20R (1,600 of 1,600 coordinate rows reproduced, scratch
sector_map.py). When the log carries coordinate rows the formula is re-checked against every one
of them and a mismatch is exit 2.

FRAME: ENU about the area origin. With --runtime-config the origin is its "offset" (what the
2026-09-20 record used). Without it the origin is FITTED: the centroid of the log's "Adjusted -"
corners, shifted east so the NW corner lands on the extent's xmin (the osm_sector_map.py
method; +21.5 m = half a cell on both AO20 and IRONSTORM-CENTRE). The fitted frame is [A]: it
reproduces the 2026-09-20 corridor table (--selftest-gen1) but is not the vendor's offset.

LEGS are straight lines sampled every --step metres at segment midpoints; each sample's length is
booked to its sector ("route m"). The preset ironstorm-cuta is the three driven legs of
data/IRONSTORM_CUTA_Order.xml (T02 28ID, T10 1-112 IN, T14 48 IBCT with its destination nudged
799 m west out of a lake, IRONSTORM_CUTA_CHANGES.md:136-137).

Python 3 stdlib only. Offline. Writes nothing.
"""
import argparse
import json
import math
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import nav_gate  # noqa: E402

A_WGS = 6378137.0
F_WGS = 1 / 298.257223563
E2 = F_WGS * (2 - F_WGS)
CELL = 43.0
GATE = 0.9

PRESETS = {
    "ironstorm-cuta": [
        ("T02 28ID", (53.99238486824088, 23.211255470526073),
         (54.028873887648274, 23.264400661185014)),
        ("T10 1-112 IN", (54.04268819191243, 23.30823457011959),
         (54.019388734463774, 23.313901568645093)),
        ("T14 48 IBCT (nudged)", (54.019388734463774, 23.313901568645093),
         (54.040348, 23.324206)),
    ],
}

# The 2026-09-20 generation-1 corridor table (old-session scratch part4.md, sampled every 2 m,
# frame = that area's runtime offset): the seven sectors below 0.9 and their route metres.
GEN1_EXPECTED = {
    "T02 28ID": {(14, 13): (0.8947, 100), (16, 14): (0.8868, 422), (17, 15): (0.8800, 318)},
    "T10 1-112 IN": {(27, 22): (0.8475, 60), (27, 23): (0.8868, 478), (28, 21): (0.8182, 476),
                     (28, 22): (0.8571, 418)},
    "T14 48 IBCT (nudged)": {},
}
GEN1_SECTORS_PER_LEG = {"T02 28ID": 16, "T10 1-112 IN": 8, "T14 48 IBCT (nudged)": 8}
GEN1_DISTINCT = 29


def llh2ecef(lat, lon, h=0.0):
    la, lo = math.radians(lat), math.radians(lon)
    n = A_WGS / math.sqrt(1 - E2 * math.sin(la) ** 2)
    return ((n + h) * math.cos(la) * math.cos(lo),
            (n + h) * math.cos(la) * math.sin(lo),
            (n * (1 - E2) + h) * math.sin(la))


def ecef2llh(x, y, z):
    lon = math.atan2(y, x)
    p = math.hypot(x, y)
    lat = math.atan2(z, p * (1 - E2))
    for _ in range(60):
        n = A_WGS / math.sqrt(1 - E2 * math.sin(lat) ** 2)
        h = p / math.cos(lat) - n
        lat = math.atan2(z, p * (1 - E2 * n / (n + h)))
    n = A_WGS / math.sqrt(1 - E2 * math.sin(lat) ** 2)
    return math.degrees(lat), math.degrees(lon), p / math.cos(lat) - n


class Frame(object):
    """ENU about an ECEF origin, plus an optional east shift."""

    def __init__(self, origin_ecef, shift_east=0.0, source=""):
        self.origin = tuple(origin_ecef)
        self.llh = ecef2llh(*self.origin)
        self.shift_east = shift_east
        self.source = source

    def enu_of_ecef(self, p):
        la, lo = math.radians(self.llh[0]), math.radians(self.llh[1])
        dx, dy, dz = (p[k] - self.origin[k] for k in range(3))
        e = -math.sin(lo) * dx + math.cos(lo) * dy
        n = (-math.sin(la) * math.cos(lo) * dx - math.sin(la) * math.sin(lo) * dy
             + math.cos(la) * dz)
        return e + self.shift_east, n

    def enu(self, lat, lon):
        return self.enu_of_ecef(llh2ecef(lat, lon, self.llh[2]))


def read_log_geometry(text):
    m = re.search(r"Adjusted -\s*(.*?)\^", text, re.S)
    corners = {}
    if m:
        for k, x, y, z in re.findall(r"(ne|nw|se|sw): \{(-?[\d.]+), (-?[\d.]+), (-?[\d.]+)\}",
                                     m.group(1)):
            corners[k] = (float(x), float(y), float(z))
    ext = re.search(r"CalculateTransitionPointLocations extent = \((-?[\d.]+),(-?[\d.]+),[^,]+,"
                    r"(-?[\d.]+),(-?[\d.]+),", text)
    extent = tuple(float(v) for v in ext.groups()) if ext else None
    coords = {}
    for mm in re.finditer(r"(?m)^Sector \((\d+),(\d+)\): xMin: (-?\d+) yMin: (-?\d+) "
                          r"xMax: (-?\d+) yMax: (-?\d+)", text):
        coords[(int(mm.group(1)), int(mm.group(2)))] = tuple(int(g) for g in mm.groups()[2:])
    return corners, extent, coords


def fitted_frame(corners, extent):
    if len(corners) != 4 or extent is None:
        raise ValueError("fitted frame needs the four 'Adjusted -' corners and the extent line")
    c = [sum(v[k] for v in corners.values()) / 4.0 for k in range(3)]
    f = Frame(c, 0.0, "fitted: Adjusted-corner centroid + east shift to the extent")
    f.shift_east = extent[0] - f.enu_of_ecef(corners["nw"])[0]
    return f


def rtc_frame(path):
    txt = open(path, "r", encoding="utf-8", errors="replace").read()
    m = re.search(r"\(offset\s+(-?[\d.]+)\s+(-?[\d.]+)\s+(-?[\d.]+)\)", txt)
    if not m:
        raise ValueError("no (offset x y z) in %s" % path)
    return Frame(tuple(float(g) for g in m.groups()), 0.0, "runtime config offset: " + path)


class Grid(object):
    def __init__(self, extent, nx, ny):
        self.cx0 = int(math.floor(extent[0] / CELL + 1e-9))
        self.cy0 = int(math.floor(extent[1] / CELL + 1e-9))
        # last cell INCLUSIVE: the coordinate rows run to floor(xmax / 43) (AO20 2026-09-25:
        # extent xmax 10062 = 234 x 43, row (39,0) xMax 234)
        self.cx1 = int(math.floor(extent[2] / CELL + 1e-9))
        self.cy1 = int(math.floor(extent[3] / CELL + 1e-9))
        self.nx, self.ny = nx, ny
        self.sx = (self.cx1 - self.cx0 + 1) // nx
        self.sy = (self.cy1 - self.cy0 + 1) // ny

    def sector_of_cell(self, cx, cy):
        if not (self.cx0 <= cx <= self.cx1 and self.cy0 <= cy <= self.cy1):
            return None
        return (min(self.nx - 1, (cx - self.cx0) // self.sx),
                min(self.ny - 1, (cy - self.cy0) // self.sy))

    def sector_of_enu(self, e, n):
        return self.sector_of_cell(int(math.floor(e / CELL)), int(math.floor(n / CELL)))

    def check_rows(self, coords):
        """Every coordinate row's min cell must map to its own (i,j); returns mismatches."""
        bad = []
        for (i, j), (xmn, ymn, xmx, ymx) in coords.items():
            if self.sector_of_cell(xmn, ymn) != (i, j) or self.sector_of_cell(xmx, ymx) != (i, j):
                bad.append((i, j))
        return bad


def sector_table(text):
    parsed = nav_gate.parse_log(text)
    table = {}
    for key in parsed["order"]:
        s = parsed["sectors"][key]
        if not s["reports"]:
            table[key] = {"ratio": None, "state": "NO GRAPH", "tags": s["tags"]}
            continue
        rs = [nav_gate.ratio_of(n, b) for (n, b) in s["reports"]]
        rs = [r for r in rs if r is not None]
        n, b = s["reports"][-1]
        if not rs:
            table[key] = {"ratio": None, "state": "DEGENERATE", "nodes": n, "nbrs": b,
                          "tags": s["tags"]}
            continue
        table[key] = {"ratio": min(rs), "state": "OK", "nodes": n, "nbrs": b, "tags": s["tags"]}
    return table


def evaluate(text, legs, rtc=None, step=2.0, gate=GATE):
    corners, extent, coords = read_log_geometry(text)
    table = sector_table(text)
    res = {"gate": gate, "step_m": step, "legs": [], "verdict": None, "exit_code": None,
           "problems": []}
    if not table or extent is None:
        res["problems"].append("no sectors parsed" if not table else "no extent line")
        res["verdict"], res["exit_code"] = "INSTRUMENT FAILURE", 2
        return res
    nx = max(k[0] for k in table) + 1
    ny = max(k[1] for k in table) + 1
    grid = Grid(extent, nx, ny)
    res["grid"] = {"extent": extent, "cells_x": [grid.cx0, grid.cx1], "cells_y": [grid.cy0, grid.cy1],
                   "sectors": [nx, ny], "stride_cells": [grid.sx, grid.sy],
                   "coordinate_rows": len(coords)}
    if coords:
        bad = grid.check_rows(coords)
        res["grid"]["coordinate_rows_mismatched"] = len(bad)
        if bad:
            res["problems"].append("grid formula does not reproduce %d coordinate rows, e.g. %s"
                                   % (len(bad), bad[:3]))
            res["verdict"], res["exit_code"] = "INSTRUMENT FAILURE", 2
            return res
    try:
        frame = rtc_frame(rtc) if rtc else fitted_frame(corners, extent)
    except (OSError, ValueError) as e:
        res["problems"].append("no frame: %s" % e)
        res["verdict"], res["exit_code"] = "INSTRUMENT FAILURE", 2
        return res
    res["frame"] = {"source": frame.source, "origin_llh": frame.llh, "shift_east_m": frame.shift_east}
    if corners:
        want = {"nw": (extent[0], extent[3]), "ne": (extent[2], extent[3]),
                "sw": (extent[0], extent[1]), "se": (extent[2], extent[1])}
        res["frame"]["corner_residuals_m"] = {
            k: [round(frame.enu_of_ecef(c)[0] - want[k][0], 2),
                round(frame.enu_of_ecef(c)[1] - want[k][1], 2)] for k, c in sorted(corners.items())}
    distinct = {}
    for label, a, b in legs:
        ea, na = frame.enu(*a)
        eb, nb = frame.enu(*b)
        d = math.hypot(eb - ea, nb - na)
        steps = max(200, int(d / step))
        metres = {}
        outside = 0
        for s in range(steps):
            f = (s + 0.5) / steps
            sec = grid.sector_of_enu(ea + (eb - ea) * f, na + (nb - na) * f)
            if sec is None:
                outside += 1
                continue
            metres[sec] = metres.get(sec, 0.0) + d / steps
        rows = []
        for sec in sorted(metres, key=lambda k: -metres[k]):
            t = table.get(sec, {"ratio": None, "state": "NOT IN LOG", "tags": None})
            fail = t["ratio"] is None or t["ratio"] < gate
            rows.append({"i": sec[0], "j": sec[1], "ratio": t["ratio"], "state": t["state"],
                         "nodes": t.get("nodes"), "nbrs": t.get("nbrs"), "tags": t.get("tags"),
                         "route_m": round(metres[sec], 1), "fail": fail})
            distinct[sec] = rows[-1]
        failing = [r for r in rows if r["fail"]]
        vals = [r["ratio"] for r in rows if r["ratio"] is not None]
        res["legs"].append({"label": label, "start": a, "end": b, "length_m": round(d, 1),
                            "samples_outside_grid": outside, "sectors": rows,
                            "sectors_failing": len(failing),
                            "route_m_failing": round(sum(r["route_m"] for r in failing), 1),
                            "worst_ratio": min(vals) if vals else None})
        if outside:
            res["problems"].append("%s: %d samples outside the grid" % (label, outside))
    fails = sorted([r for r in distinct.values() if r["fail"]],
                   key=lambda r: (r["ratio"] if r["ratio"] is not None else -1, r["i"], r["j"]))
    vals = [r["ratio"] for r in distinct.values() if r["ratio"] is not None]
    hist = {}
    for r in distinct.values():
        hist[str(r["tags"])] = hist.get(str(r["tags"]), 0) + 1
    res.update({"sectors_distinct": len(distinct), "sectors_failing": len(fails),
                "failing": [{"i": r["i"], "j": r["j"], "ratio": r["ratio"], "state": r["state"]}
                            for r in fails],
                "sectors_lt_0_5": sum(1 for v in vals if v < 0.5),
                "sectors_no_graph_or_degenerate": sum(1 for r in distinct.values()
                                                      if r["ratio"] is None),
                "ratio_min": min(vals) if vals else None,
                "corridor_tag_histogram": dict(sorted(hist.items()))})
    if res["problems"]:
        res["verdict"], res["exit_code"] = "INSTRUMENT FAILURE", 2
    elif fails:
        res["verdict"], res["exit_code"] = "FAIL", 1
    else:
        res["verdict"], res["exit_code"] = "PASS", 0
    return res


def print_text(res):
    g = res.get("grid")
    if g:
        print("corridor_gate: grid %d x %d sectors, stride %s cells, cells x %s y %s, "
              "coordinate rows %d" % (g["sectors"][0], g["sectors"][1], g["stride_cells"],
                                      g["cells_x"], g["cells_y"], g["coordinate_rows"]))
    fr = res.get("frame")
    if fr:
        print("corridor_gate: frame %s; origin %.6f %.6f %.1f; east shift %.2f m"
              % (fr["source"], fr["origin_llh"][0], fr["origin_llh"][1], fr["origin_llh"][2],
                 fr["shift_east_m"]))
        if "corner_residuals_m" in fr:
            print("corridor_gate: corner residuals vs extent (e, n) m: %s"
                  % fr["corner_residuals_m"])
    for leg in res["legs"]:
        print("")
        print("%s  %.0f m, %d sector(s), %d below %.2f (%.0f route m), worst %s"
              % (leg["label"], leg["length_m"], len(leg["sectors"]), leg["sectors_failing"],
                 res["gate"], leg["route_m_failing"],
                 "%.4f" % leg["worst_ratio"] if leg["worst_ratio"] is not None else "n/a"))
        for r in leg["sectors"]:
            print("   (%2d,%2d)  %-8s nodes %-5s nbrs %-5s tags %-4s route %6.0f m%s"
                  % (r["i"], r["j"], "%.4f" % r["ratio"] if r["ratio"] is not None else r["state"],
                     "%g" % r["nodes"] if r["nodes"] is not None else "-",
                     "%g" % r["nbrs"] if r["nbrs"] is not None else "-", r["tags"], r["route_m"],
                     "  <-- FAIL" if r["fail"] else ""))
    print("")
    for p in res["problems"]:
        print("corridor_gate: PROBLEM %s" % p)
    if "sectors_distinct" in res:
        print("corridor_gate: %d distinct corridor sectors; %d failing; %d < 0.5; %d without a "
              "graph or degenerate; min %s" % (res["sectors_distinct"], res["sectors_failing"],
                                               res["sectors_lt_0_5"],
                                               res["sectors_no_graph_or_degenerate"],
                                               "%.4f" % res["ratio_min"] if res["ratio_min"]
                                               is not None else "n/a"))
        print("corridor_gate: corridor tag histogram %s" % res["corridor_tag_histogram"])
    print("corridor_gate: CORRIDOR GATE %s (threshold %.2f, any corridor sector)"
          % (res["verdict"], res["gate"]))


def selftest_gen1(log):
    """Reproduce the 2026-09-20 generation-1 corridor table from its console log alone."""
    text = open(log, "r", encoding="utf-8", errors="replace").read()
    res = evaluate(text, PRESETS["ironstorm-cuta"], rtc=None, step=2.0)
    ok = [0, 0]

    def check(name, cond, detail=""):
        ok[0 if cond else 1] += 1
        print("%s  %s  %s" % ("PASS" if cond else "FAIL", name, detail))

    check("gen1 verdict FAIL, exit 1", res["verdict"] == "FAIL" and res["exit_code"] == 1,
          res["verdict"])
    check("gen1 grid 40 x 40, stride 11", res.get("grid", {}).get("sectors") == [40, 40]
          and res["grid"]["stride_cells"] == [11, 11], str(res.get("grid")))
    check("gen1 %d distinct corridor sectors" % GEN1_DISTINCT,
          res.get("sectors_distinct") == GEN1_DISTINCT, str(res.get("sectors_distinct")))
    check("gen1 7 failing, 0 < 0.5, 0 graph-less", res.get("sectors_failing") == 7
          and res.get("sectors_lt_0_5") == 0 and res.get("sectors_no_graph_or_degenerate") == 0,
          "%s/%s/%s" % (res.get("sectors_failing"), res.get("sectors_lt_0_5"),
                        res.get("sectors_no_graph_or_degenerate")))
    for leg in res["legs"]:
        exp = GEN1_EXPECTED[leg["label"]]
        got = {(r["i"], r["j"]): r for r in leg["sectors"] if r["fail"]}
        check("gen1 %s sector count %d" % (leg["label"], GEN1_SECTORS_PER_LEG[leg["label"]]),
              len(leg["sectors"]) == GEN1_SECTORS_PER_LEG[leg["label"]], str(len(leg["sectors"])))
        check("gen1 %s failing set %s" % (leg["label"], sorted(exp)), set(got) == set(exp),
              str(sorted(got)))
        for sec, (ratio, metres) in sorted(exp.items()):
            r = got.get(sec)
            check("gen1 %s (%d,%d) ratio %.4f, ~%d route m" % (leg["label"], sec[0], sec[1],
                                                                ratio, metres),
                  r is not None and abs(r["ratio"] - ratio) < 5e-5
                  and abs(r["route_m"] - metres) <= 10.0,
                  "" if r is None else "%.4f / %.1f m" % (r["ratio"], r["route_m"]))
    loose = evaluate(text, PRESETS["ironstorm-cuta"], rtc=None, step=2.0, gate=0.8)
    check("gen1 at gate 0.80 the same corridor PASSES (the verdict is the threshold's, not "
          "hard-wired)", loose["verdict"] == "PASS", loose["verdict"])
    print("corridor_gate selftest: %d passed, %d failed" % tuple(ok))
    return 0 if ok[1] == 0 else 1


def parse_pt(s):
    lat, lon = s.split(",")
    return (float(lat), float(lon))


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("log", nargs="?")
    ap.add_argument("--runtime-config", help="the area's .navRuntimeConfig (frame = its offset)")
    ap.add_argument("--preset", choices=sorted(PRESETS))
    ap.add_argument("--leg", nargs=3, action="append", default=[],
                    metavar=("LABEL", "LAT,LON", "LAT,LON"))
    ap.add_argument("--step", type=float, default=2.0)
    ap.add_argument("--gate", type=float, default=GATE)
    ap.add_argument("--json", action="store_true")
    ap.add_argument("--selftest-gen1", metavar="LOG")
    a = ap.parse_args(argv)
    if a.selftest_gen1:
        return selftest_gen1(a.selftest_gen1)
    if not a.log:
        ap.error("a log path is required")
    legs = list(PRESETS.get(a.preset, []))
    legs += [(lab, parse_pt(p), parse_pt(q)) for lab, p, q in a.leg]
    if not legs:
        ap.error("no legs: pass --preset and/or --leg")
    try:
        text = open(a.log, "r", encoding="utf-8", errors="replace").read()
    except OSError as e:
        print("corridor_gate: cannot read log: %s" % e, file=sys.stderr)
        return 2
    res = evaluate(text, legs, rtc=a.runtime_config, step=a.step, gate=a.gate)
    res["log"] = a.log
    if a.json:
        print(json.dumps(res, indent=1, sort_keys=True, default=str))
    else:
        print_text(res)
    return res["exit_code"]


if __name__ == "__main__":
    sys.exit(main())
