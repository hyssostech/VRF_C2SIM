#!/usr/bin/env python3
"""Per-run census for a COA-STP1 scale run: per-performer net displacement from the
C2SIM report stream, the aggregate sub-route census from the vendor log, and the
watchvrf object census.

RECONSTRUCTION, NOT A NEW INSTRUMENT. The rung-2 and `-q` sec 7 outcomes recorded the
NUMBERS these three measurements produced but did not commit the code that produced
them (only tools/analysis/frame_gaps.py is in the tree). This file re-implements the
three measurements exactly as those sections describe them and is GATED, before use,
on reproducing BOTH published tables:

    python tools/analysis/run_census.py . 20260902T165144Z_run --gate rung2
    python tools/analysis/run_census.py . 20260902T183135Z_run --gate quiet

A gate failure means this is NOT the same instrument and the numbers it produces are
not comparable to the record (the false-greens rule). ASCII only.

Definitions, taken from the two sec 7s:
  net_km        great-circle distance between a performer's FIRST and LAST C2SIM
                position report, keyed by the manifest's inputs.orderTaskees.
  sub-routes    distinct `Locally Simulated: <name>_R<n>` objects in bin64-vrfSim.log.
  ever-real     distinct uuids in watchvrf-trace.csv POS lines with at least one
                readable coordinate that is not the (90,-90,0) pole placeholder.
  pole-only     distinct POS uuids that are never ever-real. The pole placeholder is
                (+/-90, -90): the SIGN of the latitude varies, and a filter that tests
                only +90 miscounts by one object - this is what pinned the reconstruction
                to the published 1,732 / 132 / 110. A SECOND encoding of the same
                placeholder, (0, -90, ~1.9e34 m), appears from run A-1 on; it is filtered
                by |alt| > 1e8 m (see object_census).

VR-FORCES 5.2d RE-BASELINE, 2026-09-04 (REBASELINE_52_INSTRUMENTS). object_census PARSES
5.2 traces unchanged - the new `# t=` fields (ent/agg/env/ctl/extattr/waitext/discovered/
backends), the `# DIAG` lines and the vendor `Printing Reflected Object List counts` block
are all non-POS and already skipped - but READ "poleOnly" WITH CARE ON 5.2:

  - Neither 5.0.2 placeholder encoding occurs on 5.2. Measured over the three 5.2d
    captures: (90,-90) lines 0, |alt| > 1e8 lines 0.
  - The ONLY never-real form on 5.2 is (NaN, +90.000000, NaN) - note lon +90, where
    5.0.2's NaN form was lon -90 - and it is NOT a placeholder. Those uuids are the
    19 CONTROL OBJECTS (COLDSTART_REVIEW_RTIEXEC_2026-09-04): readable = ent + ctl,
    and the count is exactly 19 in every sample of every 5.2 capture.
  - So on a 5.2 trace `poleOnly` reads "objects with no position", which on this
    build means the control objects, not un-placed entities. `everReal` is still the
    entity population - but it is a UNION over the run, and the 5.2 Traffic fixture
    churns entities, so everReal EXCEEDS the final ent= count and is not a headcount.
"""

import argparse
import json
import math
import os
import re
import sys

R_EARTH_M = 6371008.8  # IUGG mean radius

REPORT_HDR = re.compile(r"^\[(\d\d):(\d\d):(\d\d)\.(\d+)\] REPORT #(\d+)")
RE_LAT = re.compile(r"<Latitude>([-0-9.eE+]+)</Latitude>")
RE_LON = re.compile(r"<Longitude>([-0-9.eE+]+)</Longitude>")
RE_SUBJ = re.compile(r"<SubjectEntity>([0-9a-fA-F-]+)</SubjectEntity>")
RE_ISO = re.compile(r"<IsoDateTime>([^<]+)</IsoDateTime>")
RE_SUBROUTE = re.compile(r"Locally Simulated: ([A-Za-z0-9/_.-]+_R\d+)")
# 5.2 (2026-09-06): the vendor log no longer prints per-object "Locally Simulated" lines (2 lines,
# both global objects, even at --notifyLevel 4 - PREREG_CONSOLE_CHANNEL). The offset sub-routes a
# unit generates for its subordinates are visible instead on the SUBORDINATE's object console at
# level 3 ("Task 1 name and parameters: Move-Along Route: "<parent>_R<n>""), captured by WatchVrf
# as CON rows when Vrf:ObjectConsoleNotifyLevel >= 3 is set for the subordinates we created.
# In the trace CSV the quotes around the route name are CSV-doubled ("") or XML-escaped (&quot;).
RE_SUBROUTE_CON = re.compile(r'Move-Along Route: (?:&quot;|"{1,2})([A-Za-z0-9/_.-]+_R\d+)(?:&quot;|"{1,2})')


def great_circle_km(a, b):
    lat1, lon1 = math.radians(a[0]), math.radians(a[1])
    lat2, lon2 = math.radians(b[0]), math.radians(b[1])
    dlat, dlon = lat2 - lat1, lon2 - lon1
    h = math.sin(dlat / 2) ** 2 + math.cos(lat1) * math.cos(lat2) * math.sin(dlon / 2) ** 2
    return 2 * R_EARTH_M * math.asin(min(1.0, math.sqrt(h))) / 1000.0


def read_reports(path):
    """Return {uuid: [(iso, lat, lon), ...]} in file order."""
    tracks = {}
    block = []
    out_blocks = []
    with open(path, "r", encoding="utf-8", errors="replace") as fh:
        for line in fh:
            if REPORT_HDR.match(line):
                if block:
                    out_blocks.append("".join(block))
                block = [line]
            else:
                block.append(line)
    if block:
        out_blocks.append("".join(block))
    for blk in out_blocks:
        m_s = RE_SUBJ.search(blk)
        m_la = RE_LAT.search(blk)
        m_lo = RE_LON.search(blk)
        if not (m_s and m_la and m_lo):
            continue  # not a position report
        m_t = RE_ISO.search(blk)
        tracks.setdefault(m_s.group(1).lower(), []).append(
            (m_t.group(1) if m_t else "", float(m_la.group(1)), float(m_lo.group(1)))
        )
    return tracks


def subroute_census(path, trace_path=None):
    """Distinct <parent>_R<n> sub-route names: from the vendor log (5.0.2 form) and, when a trace
    is given, from the object-console CON rows (5.2 form). Union of both; a name counts once."""
    names = set()
    if path and os.path.exists(path):
        with open(path, "r", encoding="utf-8", errors="replace") as fh:
            for line in fh:
                for m in RE_SUBROUTE.finditer(line):
                    names.add(m.group(1))
    if trace_path and os.path.exists(trace_path):
        with open(trace_path, "r", encoding="utf-8", errors="replace") as fh:
            for line in fh:
                if not line.startswith("CON,"):
                    continue
                for m in RE_SUBROUTE_CON.finditer(line):
                    names.add(m.group(1) or m.group(2))
    by_parent = {}
    for n in sorted(names):
        parent = n.rsplit("_R", 1)[0]
        by_parent.setdefault(parent, []).append(n)
    return by_parent


def object_census(path):
    total, real, pole = set(), set(), set()
    with open(path, "r", encoding="utf-8", errors="replace") as fh:
        for line in fh:
            if not line.startswith("POS,"):
                continue
            parts = line.rstrip("\r\n").split(",")
            if len(parts) < 6:
                continue
            uuid, lat, lon, alt = parts[2], parts[3], parts[4], parts[5]
            total.add(uuid)
            if lat == "NaN" or lon == "NaN":
                continue
            try:
                flat, flon = float(lat), float(lon)
            except ValueError:
                continue
            if abs(abs(flat) - 90.0) < 1e-6 and abs(flon + 90.0) < 1e-6:
                pole.add(uuid)
                continue
            # SECOND placeholder encoding (supervisor, 2026-09-02, pair A-1/A-2): the same
            # not-yet-positioned object that reads NaN,-90,NaN in the -q run reads
            # 0.000000,-90.000000,1.9e34 in A-1 - an ECEF garbage point, not a fix. The -q
            # comparator already carried 156 such lines; A-1/A-2 carry ~30k and NO NaN form.
            # Filtering it restores everReal = 1,732 EXACT on all four scale runs. An
            # altitude of 1e5 km is unreachable by anything we task; the lat/lon test is
            # NOT used because (0,-90) is a real point in the Pacific.
            try:
                falt = float(alt)
            except ValueError:
                continue
            if abs(falt) > 1.0e8:
                pole.add(uuid)
            else:
                real.add(uuid)
    return {
        "posUuids": len(total),
        "everReal": len(real),
        "poleOnly": len(total - real),
    }


def census(repo, run):
    rd = os.path.join(repo, "runs", run)
    man = json.load(open(os.path.join(rd, "run-manifest.json"), encoding="utf-8"))
    taskees = [t.lower() for t in man["inputs"]["orderTaskees"]]
    tracks = read_reports(os.path.join(rd, "reports-captured.log"))

    perf = []
    for t in taskees:
        fixes = tracks.get(t, [])
        if len(fixes) >= 2:
            net = great_circle_km((fixes[0][1], fixes[0][2]), (fixes[-1][1], fixes[-1][2]))
        else:
            net = 0.0
        perf.append({"uuid": t, "fixes": len(fixes), "net_km": round(net, 2)})

    # Vendor log: 5.0.2 runs keep bin64-vrfSim.log in the run dir; the 5.2 profile HARVESTS the
    # vendor's own log to runs/launch52/vrfSim_<appNo>_<stamp>.log and records the path in the
    # manifest (inputs.vrfProfile.vendorLog.harvestedTo). Prefer the run-dir file; fall back.
    vendor_log = os.path.join(rd, "bin64-vrfSim.log")
    if not os.path.exists(vendor_log):
        harvested = (((man.get("inputs") or {}).get("vrfProfile") or {}).get("vendorLog") or {}).get("harvestedTo")
        if harvested and os.path.exists(harvested):
            vendor_log = harvested
    trace = os.path.join(rd, "watchvrf-trace.csv")

    return {
        "run": run,
        "reports": sum(len(v) for v in tracks.values()),
        "reportUuids": len(tracks),
        "performers": perf,
        "subRoutes": subroute_census(vendor_log, trace),
        "vendorLog": vendor_log,
        "objects": object_census(trace),
        "quietBackend": man["inputs"].get("quietBackend"),
        "restUrl": man["inputs"].get("restUrl"),
        "stompUrl": man["inputs"].get("stompUrl"),
    }


# Published sec-7 values, transcribed from the two prereg outcomes. The gate is exact
# to the 0.01 km the records print.
GATES = {
    "rung2": {
        "reports": 1536,
        "reportUuids": 128,
        "everReal": 1732,
        "subRouteParents": {"856/HHC": 4, "B/5-20": 4, "C/1-35": 4},
        "net_km": {
            "3ac081eb-6adc-7e58-b0b5-9b506b4eae0f": 6.64,
            "50828a9b-0357-e75e-8873-5404000a90e6": 6.59,
            "6977b035-d84c-da5f-9cc0-497b9f334eb7": 6.57,
            "74bdb03b-c85e-5e54-8f04-180c44ddc9c3": 6.64,
            "d6df3c3d-f31b-701a-bfc6-2fb9bc86092a": 6.07,
            "de16a337-b2a6-c029-07b5-869191631621": 5.95,
            "6a266f06-12d7-3159-8cd6-f1e5bc9c6e72": 1.80,
            "b5b42765-36d6-1d90-6ce6-0139250949c4": 4.27,
            "1375ca0a-d212-d86a-e275-5555aef42fd8": 2.85,
            "5cd92a83-c6bd-875e-96ae-5650b790a1b6": 0.00,
            "e151451b-4fa5-eec8-4196-82bb73e3c355": 0.00,
        },
    },
    "quiet": {
        "reports": 1793,
        "reportUuids": 128,
        "everReal": 1732,
        "subRouteParents": {"856/HHC": 4, "C/1-35": 4},
        "net_km": {
            "3ac081eb-6adc-7e58-b0b5-9b506b4eae0f": 7.87,
            "50828a9b-0357-e75e-8873-5404000a90e6": 7.72,
            "6977b035-d84c-da5f-9cc0-497b9f334eb7": 7.84,
            "74bdb03b-c85e-5e54-8f04-180c44ddc9c3": 7.86,
            "d6df3c3d-f31b-701a-bfc6-2fb9bc86092a": 7.32,
            "de16a337-b2a6-c029-07b5-869191631621": 7.20,
            "6a266f06-12d7-3159-8cd6-f1e5bc9c6e72": 6.55,
            "b5b42765-36d6-1d90-6ce6-0139250949c4": 5.50,
            "1375ca0a-d212-d86a-e275-5555aef42fd8": 0.41,
            "5cd92a83-c6bd-875e-96ae-5650b790a1b6": 0.00,
            "e151451b-4fa5-eec8-4196-82bb73e3c355": 0.00,
        },
    },
}

TOL_KM = 0.01


def run_gate(result, name):
    exp = GATES[name]
    fails = []
    if result["reports"] != exp["reports"]:
        fails.append("reports %d != %d" % (result["reports"], exp["reports"]))
    if result["reportUuids"] != exp["reportUuids"]:
        fails.append("reportUuids %d != %d" % (result["reportUuids"], exp["reportUuids"]))
    if result["objects"]["everReal"] != exp["everReal"]:
        fails.append("everReal %d != %d" % (result["objects"]["everReal"], exp["everReal"]))
    got_sr = {k: len(v) for k, v in result["subRoutes"].items()}
    if got_sr != exp["subRouteParents"]:
        fails.append("subRoutes %s != %s" % (got_sr, exp["subRouteParents"]))
    for p in result["performers"]:
        want = exp["net_km"].get(p["uuid"])
        if want is None:
            fails.append("unexpected performer %s" % p["uuid"])
        elif abs(p["net_km"] - want) > TOL_KM + 1e-9:
            fails.append("net_km %s = %.2f, published %.2f" % (p["uuid"][:8], p["net_km"], want))
    return fails


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("repo")
    ap.add_argument("run")
    ap.add_argument("--gate", choices=sorted(GATES), default=None)
    args = ap.parse_args()

    result = census(args.repo, args.run)
    print(json.dumps(result, indent=1, sort_keys=True))

    if args.gate:
        fails = run_gate(result, args.gate)
        if fails:
            print("\nGATE %s: FAIL" % args.gate)
            for f in fails:
                print("  - " + f)
            sys.exit(1)
        print("\nGATE %s: PASS - reproduces the published sec 7 table." % args.gate)
    return 0


if __name__ == "__main__":
    sys.exit(main())
