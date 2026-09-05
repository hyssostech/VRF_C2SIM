#!/usr/bin/env python3
"""placement_check.py - score a WatchVrf POS trace for CREATE-TIME PLACEMENT.

OFFLINE. Reads an existing WatchVrf trace (and, optionally, a vendor vrfSim log
for uuid -> name labels). Never joins a federation, never touches C:\\MAK, never
launches anything.

WHAT IT ANSWERS, per uuid, which is exactly what PREREG_PLACEMENT_R9_52 needs and
no existing script in tools/analysis/ reports:
  first     the altitude of the FIRST sample the observer ever read for that uuid
            - the create-time reading, before any post-create SetAltitude can have
            been observed. This is the P1/P3 clamp-vs-set discriminator.
  rise      the first sample at or above --rise metres (default 1000), with its
            time - "when did it get onto the terrain", if it was not born there.
  steady    the median of the last --steady samples - where it ended up.
  min/max   the extremes, which is how the P4 forbidden values are caught even
            when they occur only mid-run.

INPUT FORMAT (tools/WatchVrf/WatchRunner.cs:346-347):
    POS,<elapsed-seconds>,<uuid>,<latDeg>,<lonDeg>,<altM>
Every other line shape in the trace (CON / TSK / RPT, '#' summaries, '# DIAG',
the vendor's bracketed count block, banner text) is IGNORED by construction.

NOT-REAL OBJECTS ARE NOT SCORED. VR-Forces publishes control objects and, on the
5.0.2 stack, pole placeholders that are readable but carry no position
(tools/analysis/run_census.py, REBASELINE_52_INSTRUMENTS): the 5.2 form is
(NaN, +90, NaN) and the 5.0.2 forms are (+/-90, -90) and |alt| > 1e8. They are
listed separately under SKIPPED and never counted as a pass or a fail - a filter
that scored them would turn 19 control objects into 19 failures.

BANDS. A single global band is wrong for the R9 AOI: the terrain under the three
R9 lean-init create points differs by ~90 m (1131.4 / 1116.7 / 1040.6 m
ellipsoid, read off the terrain-profile replies in PREREG_TERRAIN_ROW3_DEFAULT_
2026-09-02 sec 6). So pass --expect UUID=TERRAIN (repeatable) for a per-object
band of TERRAIN +/- --tol, and let --band cover anything not named.

UUIDS. A POS line carries the VR-FORCES uuid the ObjectCreated callback returned,
NOT the C2SIM uuid from the init - so the --expect keys are only knowable after
the run. Get them from --vendor-log (its 'Locally Simulated:' lines name every
platform, members included) or from the app log's 'VRF created <name> -> <uuid>'
line, then use --expect-name NAME=TERRAIN, which needs no uuid at all.

EXIT CODES (the tools/Shared/ToolArgs.cs standard): 0 every scored uuid PASSED,
1 at least one FAILED (or nothing was scorable), 2 usage error.
"""
import math
import os
import sys

USAGE = [
    "usage: python tools/analysis/placement_check.py <watch.trace> [options]",
    "",
    "  --band MIN:MAX        band for uuids with no --expect (default 1000:1200)",
    "  --expect UUID=ALT     per-uuid expected terrain height, m; repeatable.",
    "                        UUID may be a unique substring of the full VRF_UUID.",
    "  --expect-name NAME=ALT  same, keyed on the --vendor-log marking; NAME may be",
    "                        a substring, so 'M1A2'=1131.4 covers every M1A2 member.",
    "                        --expect wins over --expect-name for the same object.",
    "  --tol M               half-width of an --expect band (default 5.0)",
    "  --rise M              'first sample at or above' threshold (default 1000.0)",
    "  --steady N            samples in the steady window, from the end (default 3)",
    "  --zero-tol M          |alt| <= M is a ZERO violation (default 1.0)",
    "  --high M              forbidden high altitude (default 10000.0)",
    "  --high-tol M          |alt - high| <= M is a HIGH violation (default 50.0)",
    "  --vendor-log PATH     bin64 vrfSim log; labels uuids from its",
    "                        'Locally Simulated: <name> (VRF_UUID:...)' lines",
    "  --show-skipped        list the not-real objects that were filtered out",
    "",
    "example:",
    "  python tools/analysis/placement_check.py runs/launch52/watch_<appNo>.trace \\",
    "      --vendor-log runs/launch52/vrfSim_<appNo>_<stamp>.log \\",
    "      --expect-name 1.BdeHQ=1131.4 --expect-name 114.MechCoy=1116.7 \\",
    "      --expect-name 1222.MechPlt=1040.6 --band 1000:1200",
]


def usage(problem):
    sys.stderr.write("placement_check: " + problem + "\n\n")
    for line in USAGE:
        sys.stderr.write(line + "\n")
    return 2


def parse_float(text):
    """Return a float, or None when the field is not a finite number.

    VR-Forces emits NaN through the observer for control objects, and the
    platform spelling of that ('nan', '-nan(ind)') is not portable, so an
    unparseable field is treated the same as a NaN: not a position.
    """
    try:
        value = float(text)
    except ValueError:
        return None
    if math.isnan(value) or math.isinf(value):
        return None
    return value


def is_not_real(lat, lon, alt):
    """True for the readable-but-positionless objects documented in run_census.py."""
    if lat is None or lon is None or alt is None:
        return True                      # 5.2 control object: (NaN, +90, NaN)
    if abs(alt) > 1e8:
        return True                      # 5.0.2 second placeholder encoding
    if abs(abs(lat) - 90.0) < 1e-9 and abs(lon + 90.0) < 1e-9:
        return True                      # 5.0.2 pole placeholder, either sign
    if abs(lat) < 1e-9 and abs(abs(lon) - 90.0) < 1e-9:
        return True                      # the (0, +90) form seen in the 5.2 traces
    return False


def read_trace(path):
    """-> (samples, skipped) where samples[uuid] = [(t, lat, lon, alt), ...]."""
    samples = {}
    skipped = {}
    with open(path, "r", encoding="utf-8", errors="replace") as handle:
        for raw in handle:
            line = raw.strip()
            if not line.startswith("POS,"):
                continue
            parts = line.split(",")
            if len(parts) < 6:
                continue
            t = parse_float(parts[1])
            uuid = parts[2]
            lat = parse_float(parts[3])
            lon = parse_float(parts[4])
            alt = parse_float(parts[5])
            if t is None or not uuid:
                continue
            if is_not_real(lat, lon, alt):
                skipped[uuid] = skipped.get(uuid, 0) + 1
                continue
            samples.setdefault(uuid, []).append((t, lat, lon, alt))
    for series in samples.values():
        series.sort(key=lambda row: row[0])
    return samples, skipped


def read_names(path):
    """uuid -> marking, from the vendor log's 'Locally Simulated:' lines.

    Same regex family as tools/analysis/run_census.py. MEMBER platforms of a
    disaggregated unit appear here too, which is what makes P2 scorable by name.
    The 5.2 vendor log QUOTES the marking ('Locally Simulated: "1.BdeHQ" (...)'),
    while the app log and --expect-name keys are unquoted; strip the surrounding
    quotes so an EXACT-match key works and the displayed label carries no quotes
    (matches tools/analysis/movement_check.py read_labels). Substring matching hid
    this before - '1.BdeHQ' is a substring of '"1.BdeHQ"' - so scores were right
    but exact keys and labels were not.
    """
    names = {}
    marker = "Locally Simulated: "
    with open(path, "r", encoding="utf-8", errors="replace") as handle:
        for raw in handle:
            at = raw.find(marker)
            if at < 0:
                continue
            rest = raw[at + len(marker):]
            open_paren = rest.find("(VRF_UUID:")
            if open_paren < 0:
                continue
            close_paren = rest.find(")", open_paren)
            if close_paren < 0:
                continue
            name = rest[:open_paren].strip().strip('"').strip()
            uuid = rest[open_paren + 1:close_paren].strip()
            if uuid and name:
                names.setdefault(uuid, name)
    return names


def median(values):
    ordered = sorted(values)
    count = len(ordered)
    if count == 0:
        return float("nan")
    middle = count // 2
    if count % 2 == 1:
        return ordered[middle]
    return (ordered[middle - 1] + ordered[middle]) / 2.0


def resolve_expect(expect, expect_name, uuid, label):
    """Expected terrain for one object, or None.

    --expect (uuid-keyed) is consulted first and wins; --expect-name is the
    fallback and needs a --vendor-log label. Both match exactly first, then as a
    unique substring - an AMBIGUOUS substring resolves to nothing rather than to
    a guess, so a mistyped key falls back to the global band visibly instead of
    scoring an object against the wrong terrain.
    """
    for table, key_text in ((expect, uuid), (expect_name, label)):
        if not table or not key_text:
            continue
        if key_text in table:
            return table[key_text]
        hits = [k for k in table if k in key_text]
        if len(hits) == 1:
            return table[hits[0]]
    return None


def main(argv):
    trace_path = None
    band_lo, band_hi = 1000.0, 1200.0
    expect = {}
    expect_name = {}
    tol = 5.0
    rise = 1000.0
    steady_n = 3
    zero_tol = 1.0
    high = 10000.0
    high_tol = 50.0
    vendor_log = None
    show_skipped = False

    index = 0
    while index < len(argv):
        arg = argv[index]
        if arg == "--band":
            index += 1
            if index >= len(argv):
                return usage("--band needs MIN:MAX")
            text = argv[index]
            if ":" not in text:
                return usage("--band needs MIN:MAX, got '" + text + "'")
            lo_text, hi_text = text.split(":", 1)
            lo, hi = parse_float(lo_text), parse_float(hi_text)
            if lo is None or hi is None or hi <= lo:
                return usage("--band '" + text + "' is not a usable MIN:MAX")
            band_lo, band_hi = lo, hi
        elif arg in ("--expect", "--expect-name"):
            index += 1
            if index >= len(argv):
                return usage(arg + " needs KEY=ALT")
            text = argv[index]
            if "=" not in text:
                return usage(arg + " needs KEY=ALT, got '" + text + "'")
            key, value_text = text.split("=", 1)
            value = parse_float(value_text)
            if not key or value is None:
                return usage(arg + " '" + text + "' is not a usable KEY=ALT")
            if arg == "--expect":
                expect[key] = value
            else:
                expect_name[key] = value
        elif arg in ("--tol", "--rise", "--zero-tol", "--high", "--high-tol"):
            index += 1
            if index >= len(argv):
                return usage(arg + " needs a number")
            value = parse_float(argv[index])
            if value is None:
                return usage(arg + " '" + argv[index] + "' is not a number")
            if arg == "--tol":
                tol = value
            elif arg == "--rise":
                rise = value
            elif arg == "--zero-tol":
                zero_tol = value
            elif arg == "--high":
                high = value
            else:
                high_tol = value
        elif arg == "--steady":
            index += 1
            if index >= len(argv):
                return usage("--steady needs a positive integer")
            try:
                steady_n = int(argv[index])
            except ValueError:
                return usage("--steady '" + argv[index] + "' is not an integer")
            if steady_n < 1:
                return usage("--steady must be >= 1")
        elif arg == "--vendor-log":
            index += 1
            if index >= len(argv):
                return usage("--vendor-log needs a path")
            vendor_log = argv[index]
        elif arg == "--show-skipped":
            show_skipped = True
        elif arg.startswith("--"):
            return usage("unknown option '" + arg + "'")
        elif trace_path is None:
            trace_path = arg
        else:
            return usage("only one trace path is accepted; got a second: '" + arg + "'")
        index += 1

    if trace_path is None:
        return usage("missing <watch.trace>")
    if not os.path.isfile(trace_path):
        return usage("trace '" + trace_path + "' does not exist "
                     "(resolved from '" + os.getcwd() + "')")
    if vendor_log is not None and not os.path.isfile(vendor_log):
        return usage("--vendor-log '" + vendor_log + "' does not exist")

    samples, skipped = read_trace(trace_path)
    names = read_names(vendor_log) if vendor_log else {}

    print("placement_check: " + trace_path)
    print("  band(default)=%.1f..%.1f  tol=+/-%.1f  rise>=%.1f  steady=last %d  "
          "forbidden: |alt|<=%.1f and %.1f+/-%.1f"
          % (band_lo, band_hi, tol, rise, steady_n, zero_tol, high, high_tol))
    for table, what in ((expect, "expect uuid~"), (expect_name, "expect name~")):
        for key in sorted(table):
            print("  %s%s -> terrain %.1f m (band %.1f..%.1f)"
                  % (what, key, table[key], table[key] - tol, table[key] + tol))
    if expect_name and not names:
        print("  WARNING: --expect-name given but no labels are available "
              "(--vendor-log missing or matched nothing); those bands cannot apply.")
    print("")

    failures = 0
    scored = 0
    for uuid in sorted(samples):
        series = samples[uuid]
        alts = [row[3] for row in series]
        first_t, _, _, first_alt = series[0]
        steady = median(alts[-steady_n:])
        lowest, highest = min(alts), max(alts)

        label = names.get(uuid, "")
        target = resolve_expect(expect, expect_name, uuid, label)
        if target is None:
            lo, hi = band_lo, band_hi
        else:
            lo, hi = target - tol, target + tol

        rise_text = "none"
        for t, _, _, alt in series:
            if alt >= rise:
                rise_text = "%.1fs:%.1f" % (t, alt)
                break

        reasons = []
        if any(abs(alt) <= zero_tol for alt in alts):
            reasons.append("ZERO(|alt|<=%.1f seen)" % zero_tol)
        if any(abs(alt - high) <= high_tol for alt in alts):
            reasons.append("HIGH(%.0f+/-%.0f seen)" % (high, high_tol))
        first_in = lo <= first_alt <= hi
        steady_in = lo <= steady <= hi
        if not first_in:
            reasons.append("first %.1f outside %.1f..%.1f" % (first_alt, lo, hi))
        if not steady_in:
            reasons.append("steady %.1f outside %.1f..%.1f" % (steady, lo, hi))

        if first_in:
            mechanism = "CREATE"          # already on terrain at the first reading
        elif steady_in:
            mechanism = "SET"             # arrived on terrain only after the first reading
        else:
            mechanism = "NEITHER"

        verdict = "PASS" if not reasons else "FAIL"
        scored += 1
        if reasons:
            failures += 1

        print("%-4s %s%s n=%d first=%.1fs:%.1f rise=%s steady=%.1f min=%.1f max=%.1f "
              "band=%.1f..%.1f mech=%s%s"
              % (verdict, uuid, (" [" + label + "]") if label else "",
                 len(series), first_t, first_alt, rise_text, steady,
                 lowest, highest, lo, hi, mechanism,
                 ("  " + "; ".join(reasons)) if reasons else ""))

    if show_skipped and skipped:
        print("")
        for uuid in sorted(skipped):
            print("SKIP %s%s %d sample(s) - not a real position "
                  "(control object / placeholder)"
                  % (uuid, (" [" + names.get(uuid, "") + "]") if names.get(uuid) else "",
                     skipped[uuid]))

    print("")
    print("scored=%d passed=%d failed=%d not-real-skipped=%d"
          % (scored, scored - failures, failures, len(skipped)))
    if scored == 0:
        print("RESULT: FAIL - no scorable POS object in the trace. An empty trace is "
              "not a pass (the false-green rule).")
        return 1
    print("RESULT: " + ("PASS" if failures == 0 else "FAIL"))
    return 0 if failures == 0 else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
