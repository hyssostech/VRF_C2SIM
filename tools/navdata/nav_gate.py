#!/usr/bin/env python
"""nav_gate.py - offline abstract-graph connectivity gate for a vrfNavGenerator log.

Metric (docs/experiments/RESEARCH_ABSTRACT_GRAPH_CONNECTIVITY_2026-09-15.md sec 3; gate
threshold PREREG_V7_AO20_2026-09-15.md:109-111, DEMO_READINESS row 21):

    ratio = Average Neighbor Node Count / (Average Node Count - 1)

per sector, from that sector's "ABSTRACT GRAPH POST PROCESS REPORT" block in the generation
log. 1.00 iff every abstract node reaches every other. "fragmented" = ratio < 0.5 (the
research's threshold); the GATE fails if ANY measured sector is below 0.9.

    python nav_gate.py <gen.log> [--area-dir <area folder>] [--area-hash] [--json]
    python nav_gate.py --write-control {clean,dirty,west20} <out.log>

Exit codes: 0 = every measured sector >= 0.9; 1 = at least one sector < 0.9 (listed);
2 = instrument failure (no report block parsed at all, or the log is unreadable). A log
that yields nothing must never pass.

LOG FORMAT STATUS [A]: the record never quoted a whole report block verbatim (the old logs
were lost with the machine rebuild, gap G7). The parser keys on the fragments the record
DOES quote: "Sector (i,j): xMin ... yMax" rows (RESEARCH :246), the header "ABSTRACT GRAPH
POST PROCESS REPORT", the field names "Average Node Count" / "Average Neighbor Node Count"
(RESEARCH :30), "AbstractGraph Count : 4" (RESEARCH :176), "Generated N distinct nav tags."
(WEST20 :654), "^Generated: ground-platform" and "Generation time:" (WEST20 :204-205).
A report block is attributed to the most recent "Sector (i,j)" row. The synthetic controls
written by --write-control follow the same assumptions, so passing them is
self-consistency, not validation against a real log.

Companion tripwires (WEST20 :666-669): output byte size of the area folder (--area-dir;
166 MB on 2026-09-07 vs 268.7 MB on 2026-09-15 for the same box) and the per-sector
distinct-nav-tag count (when the log carries it).

Python 3 stdlib only. Self-contained; no MAK install needed.
"""
import argparse
import hashlib
import json
import os
import re
import statistics
import sys

GATE = 0.9
FRAGMENTED = 0.5

SECTOR_RE = re.compile(r"\bSector\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)")
REPORT_RE = re.compile(r"ABSTRACT\s+GRAPH\s+POST\s+PROCESS\s+REPORT", re.I)
NODES_RE = re.compile(r"Average\s+Node\s+Count\s*[:=]\s*([-+]?\d+(?:\.\d+)?)", re.I)
NBRS_RE = re.compile(r"Average\s+Neighbou?r\s+Node\s+Count\s*[:=]\s*([-+]?\d+(?:\.\d+)?)",
                     re.I)
TAGS_RE = re.compile(r"Generated\s+(\d+)\s+distinct\s+nav\s+tags?", re.I)
GENERATED_RE = re.compile(r"^\s*Generated:\s*ground-platform", re.I)
GENTIME_RE = re.compile(r"Generation\s+time:\s*([-+]?\d+(?:\.\d+)?)", re.I)


def parse_log(text):
    """Return a dict of per-sector records and log-level counters. Pure function."""
    sectors = {}          # (i, j) -> {"reports": [(nodes, nbrs)], "tags": int|None}
    order = []
    cur = None            # current sector key
    pending = None        # open report: {"nodes": x, "nbrs": y} awaiting both fields
    generated_rows = 0
    gen_time = None
    orphan_reports = 0
    for line in text.splitlines():
        m = SECTOR_RE.search(line)
        if m:
            key = (int(m.group(1)), int(m.group(2)))
            if key != cur:
                pending = None
            cur = key
            if key not in sectors:
                sectors[key] = {"reports": [], "tags": None}
                order.append(key)
        if REPORT_RE.search(line):
            if cur is None:
                orphan_reports += 1
                pending = None
            else:
                pending = {"nodes": None, "nbrs": None}
            continue
        if pending is not None:
            mn = NODES_RE.search(line)
            if mn:
                pending["nodes"] = float(mn.group(1))
            mb = NBRS_RE.search(line)
            if mb:
                pending["nbrs"] = float(mb.group(1))
            if pending["nodes"] is not None and pending["nbrs"] is not None:
                sectors[cur]["reports"].append((pending["nodes"], pending["nbrs"]))
                pending = None
        mt = TAGS_RE.search(line)
        if mt and cur is not None:
            sectors[cur]["tags"] = int(mt.group(1))
        if GENERATED_RE.search(line):
            generated_rows += 1
        mg = GENTIME_RE.search(line)
        if mg:
            gen_time = float(mg.group(1))
    return {"sectors": sectors, "order": order, "generated_rows": generated_rows,
            "generation_time": gen_time, "orphan_reports": orphan_reports}


def ratio_of(nodes, nbrs):
    if nodes <= 1.0:
        return None
    return nbrs / (nodes - 1.0)


def area_stats(area_dir, want_hash):
    total = 0
    files = 0
    manifest = []
    for root, dirs, names in os.walk(area_dir):
        dirs.sort()
        for n in sorted(names):
            p = os.path.join(root, n)
            sz = os.path.getsize(p)
            total += sz
            files += 1
            if want_hash:
                h = hashlib.sha256()
                with open(p, "rb") as f:
                    for chunk in iter(lambda: f.read(1 << 20), b""):
                        h.update(chunk)
                rel = os.path.relpath(p, area_dir).replace("\\", "/")
                manifest.append("%s\t%d\t%s\n" % (rel, sz, h.hexdigest()))
    out = {"area_dir": area_dir, "area_files": files, "area_bytes": total}
    if want_hash:
        out["area_manifest_sha256"] = hashlib.sha256(
            "".join(manifest).encode("utf-8")).hexdigest()
    return out


def evaluate(parsed):
    rows = []
    no_report = []
    degenerate = []
    multi = 0
    gt_one = 0
    tag_hist = {}
    for key in parsed["order"]:
        s = parsed["sectors"][key]
        if s["tags"] is not None:
            tag_hist[s["tags"]] = tag_hist.get(s["tags"], 0) + 1
        if not s["reports"]:
            no_report.append(key)
            continue
        if len(s["reports"]) > 1:
            multi += 1
        rs = [ratio_of(n, b) for (n, b) in s["reports"]]
        rs = [r for r in rs if r is not None]
        if not rs:
            degenerate.append(key)
            continue
        r = min(rs)  # conservative if a sector ever carries more than one report
        if r > 1.0 + 1e-9:
            gt_one += 1
        n, b = s["reports"][-1]
        rows.append({"i": key[0], "j": key[1], "ratio": r, "nodes": n, "nbrs": b,
                     "tags": s["tags"]})
    ratios = [r["ratio"] for r in rows]
    failing = sorted([r for r in rows if r["ratio"] < GATE], key=lambda r: (r["ratio"],
                                                                            r["i"], r["j"]))
    res = {
        "sectors_total": len(parsed["order"]),
        "sectors_measured": len(rows),
        "sectors_no_report": len(no_report),
        "sectors_degenerate_nodes_le_1": len(degenerate),
        "sectors_multi_report": multi,
        "sectors_ratio_gt_1": gt_one,
        "sectors_lt_0_5_fragmented": sum(1 for r in ratios if r < FRAGMENTED),
        "sectors_lt_0_9": len(failing),
        "sectors_eq_1": sum(1 for r in ratios if abs(r - 1.0) < 1e-9),
        "ratio_min": min(ratios) if ratios else None,
        "ratio_median": statistics.median(ratios) if ratios else None,
        "generated_rows": parsed["generated_rows"],
        "generation_time_s": parsed["generation_time"],
        "orphan_reports": parsed["orphan_reports"],
        "tag_count_histogram": {str(k): v for k, v in sorted(tag_hist.items())},
        "gate_threshold": GATE,
        "failing_sectors": [{"i": r["i"], "j": r["j"], "ratio": round(r["ratio"], 4),
                             "nodes": r["nodes"], "nbrs": r["nbrs"], "tags": r["tags"]}
                            for r in failing],
        "no_report_sectors": [{"i": k[0], "j": k[1]} for k in no_report],
    }
    if not rows:
        res["verdict"] = "INSTRUMENT FAILURE"
        res["exit_code"] = 2
    elif failing:
        res["verdict"] = "FAIL"
        res["exit_code"] = 1
    else:
        res["verdict"] = "PASS"
        res["exit_code"] = 0
    return res


def fmt(x):
    return "n/a" if x is None else ("%.4f" % x)


def print_text(res, max_list=50):
    print("nav_gate: sectors total %d, measured %d, no report %d, degenerate %d"
          % (res["sectors_total"], res["sectors_measured"], res["sectors_no_report"],
             res["sectors_degenerate_nodes_le_1"]))
    print("nav_gate: ratio < 0.5 (fragmented) %d; ratio < 0.9 %d; ratio == 1.00 %d"
          % (res["sectors_lt_0_5_fragmented"], res["sectors_lt_0_9"], res["sectors_eq_1"]))
    print("nav_gate: min ratio %s; median ratio %s; ratio > 1 (metric falsifier) %d;"
          " multi-report sectors %d" % (fmt(res["ratio_min"]), fmt(res["ratio_median"]),
                                        res["sectors_ratio_gt_1"],
                                        res["sectors_multi_report"]))
    print("nav_gate: 'Generated: ground-platform' rows %d; Generation time %s"
          % (res["generated_rows"], res["generation_time_s"]))
    if res["tag_count_histogram"]:
        print("nav_gate: distinct nav tags per sector: " + ", ".join(
            "%s tags x %d" % kv for kv in res["tag_count_histogram"].items()))
    else:
        print("nav_gate: distinct nav tags per sector: not in this log")
    if "area_bytes" in res:
        line = "nav_gate: area folder %d files, %d bytes (%.1f MB)" % (
            res["area_files"], res["area_bytes"], res["area_bytes"] / 1e6)
        if "area_manifest_sha256" in res:
            line += ", manifest sha256 " + res["area_manifest_sha256"]
        print(line)
    fs = res["failing_sectors"]
    for r in fs[:max_list]:
        print("nav_gate: FAILING sector (%d,%d) ratio %.4f (nodes %s, nbrs %s, tags %s)"
              % (r["i"], r["j"], r["ratio"], r["nodes"], r["nbrs"], r["tags"]))
    if len(fs) > max_list:
        print("nav_gate: ... %d more failing sectors (use --json for all)"
              % (len(fs) - max_list))
    print("nav_gate: GATE %s (threshold %.1f, any sector)" % (res["verdict"], GATE))


# ---- synthetic controls (format [A], see module docstring) ----

def _sector_block(i, j, nodes, nbrs, tags=1, report=True):
    lines = ["Sector (%d,%d): xMin %d yMin %d xMax %d yMax %d"
             % (i, j, i * 473, j * 473, (i + 1) * 473, (j + 1) * 473),
             "Generated %d distinct nav tags." % tags,
             "GENERATED NAVDATA: REGULAR"]
    if report:
        lines += ["ABSTRACT GRAPH POST PROCESS REPORT",
                  "AbstractGraph Count : 4",
                  "Average Node Count : %.2f" % nodes,
                  "Average Neighbor Node Count : %.2f" % nbrs]
    lines.append("Generated: ground-platform")
    return lines


def make_control(kind):
    """clean: 40x40 all ratio 1.00; dirty: one sector (7,3) at 0.40; west20: a log shaped
    to yield WEST20's published 234 of 1,598 graphed sectors < 0.5 and 409 < 0.9, 1,118
    exactly 1.00, 2 graph-less (WEST20 :300)."""
    nodes = 49.0
    lines = ["vrfNavGenerator SYNTHETIC CONTROL (%s) - not a real generation log" % kind]
    keys = [(i, j) for i in range(40) for j in range(40)]
    if kind in ("clean", "dirty"):
        for (i, j) in keys:
            nb = nodes - 1.0
            if kind == "dirty" and (i, j) == (7, 3):
                nb = 0.4 * (nodes - 1.0)
            lines += _sector_block(i, j, nodes, nb)
    elif kind == "west20":
        # 1,600 sectors: 2 without a report, then 234 < 0.5, 175 in [0.5, 0.9),
        # 71 in [0.9, 1.0), 1,118 at 1.00  (234 + 175 + 71 + 1,118 = 1,598)
        plan = ([None] * 2 + [0.25] * 234 + [0.70] * 175 + [0.95] * 71 + [1.0] * 1118)
        assert len(plan) == 1600
        for (i, j), r in zip(keys, plan):
            if r is None:
                lines += _sector_block(i, j, 0, 0, report=False)
            else:
                lines += _sector_block(i, j, nodes, r * (nodes - 1.0),
                                       tags=1 if r == 1.0 else 3)
    else:
        raise ValueError("unknown control kind: %s" % kind)
    lines.append("Generation time: 1429.81")
    return "\r\n".join(lines) + "\r\n"


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("log", nargs="?", help="vrfNavGenerator --logFileName output")
    ap.add_argument("--area-dir", help="the generated area folder (byte-size tripwire)")
    ap.add_argument("--area-hash", action="store_true",
                    help="also sha256 every file under --area-dir; prints a manifest hash")
    ap.add_argument("--json", action="store_true", help="print JSON instead of text")
    ap.add_argument("--write-control", nargs=2, metavar=("KIND", "OUT"),
                    help="write a synthetic control log: clean | dirty | west20")
    a = ap.parse_args(argv)
    if a.write_control:
        kind, out = a.write_control
        with open(out, "w", encoding="ascii", newline="") as f:
            f.write(make_control(kind))
        print("nav_gate: wrote %s control to %s" % (kind, out))
        return 0
    if not a.log:
        ap.error("a log path is required")
    try:
        with open(a.log, "r", encoding="utf-8", errors="replace") as f:
            text = f.read()
    except OSError as e:
        print("nav_gate: cannot read log: %s" % e, file=sys.stderr)
        return 2
    res = evaluate(parse_log(text))
    res["log"] = a.log
    if a.area_dir:
        res.update(area_stats(a.area_dir, a.area_hash))
    if a.json:
        print(json.dumps(res, indent=1, sort_keys=True))
    else:
        print_text(res)
    return res["exit_code"]


if __name__ == "__main__":
    sys.exit(main())
