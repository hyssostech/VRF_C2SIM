#!/usr/bin/env python3
"""movement_check.py - score a WatchVrf POS trace for MOVEMENT + COMPLETION.

OFFLINE. Reads an existing WatchVrf trace and (optionally) the app log and the
vendor vrfSim log. Never joins a federation, never touches C:\\MAK, never launches
anything. It is the scorer for PREREG_R9_52 (the R9 ORDER run on 5.2): the sister
of placement_check.py, which answers create-time PLACEMENT. This one answers "did
each taskee MOVE, and did its task COMPLETE - without a vacuous (false-TASKCMPLT)
pass".

WHAT IT ANSWERS, per real object (members included), and per named taskee:
  disp      great-circle distance from the FIRST sample position to the LAST.
            "MOVED" = disp > --move-threshold (default 5 m). This is the robust
            net-movement measure: in the golden 5.0.2 trace it reads ~0 m for the
            untasked platoons and 700-1200 m for the tasked units.
  maxdisp   the largest great-circle distance of ANY sample from the first. It
            catches transient excursions, but on a disaggregated unit it ALSO
            picks up the create-time member snap (a single mid-run sample can sit
            tens of km off) - so maxdisp is diagnostic, NOT the MOVED test.
  completed whether the app log carried a completion for the taskee's name (the
            "VRF task complete: <name> / <type>" line, backed by a
            "SENT TASK STATUS REPORT (TASKCMPLT)"). Reported per name.
  VACUOUS   a name that COMPLETED but whose object did NOT move (disp <= threshold)
            - the false-TASKCMPLT shape. Always a FAIL.
  arrival   with --expect-end NAME=LAT,LON, the closest approach (min great-circle
            distance of any sample to that point). REPORTED, not judged - see below.

WHY MEMBERS, NOT ONLY THE AGGREGATE (RESEARCH_52_MOVEMENT_ORDER G2/G3): a
disaggregated unit's own point is DERIVED from its member platforms by the
disaggregatedActuator; the unit may publish a static or absent point while its
members move. So every real object is scored and printed with its vendor-log
label, and member movement is visible on its own row. NOTE/LIMITATION: the
vendor log's "Locally Simulated:" lines do NOT record a member's parent unit, and
member markings ("M1A2 7") do not contain the unit name, so this tool cannot
group members under their unit from the artifacts alone. --expect-move on an
aggregate name therefore tests the AGGREGATE's own published point; if a run ever
shows a static aggregate point with moving members, read the member rows directly
or pass --expect-move on the member labels.

COMPLETION + HEALTH come from the app log (--app-log). Line formats, verbatim from
src/VrfC2SimApp/VrfC2SimService.cs (read, not guessed):
    "VRF task complete: {Unit} / {Task}"                                  (:1665)
    "SENT TASK STATUS REPORT (TASKCMPLT) taskee={Uuid} task={Task}."      (:1758)
    "DROPPING TASK '{Task}' BECAUSE UNIT {Uuid} ({Name}) WAS NOT CREATED."(:1072)
    "ABANDONING TASK '{Task}': could not read live location for {Name}..." (:1176)
The .NET console logger prints the category on one line and the MESSAGE on the
next, indented; this tool matches the message substrings anywhere on a line, so
the two-line layout does not matter. The create-before-task HEALTH GATE (G4/P1):
any nonzero DROPPING TASK or ABANDONING TASK count is a FAIL.

VENDOR-LOG COMPLETION (supplementary, if --vendor-log is given): VR-Forces logs
per-controller completion as "... 's task has Completed" / "subtask has Completed"
(seen in bin64-vrfSim.log). Those lines are COUNTED and reported, but the vendor
sim log interleaves concurrent entity writes ("M1A2 10M1A2 9::"), so the entity
names in them are unreliable; the count is evidence, never a pass/fail input.

ARRIVAL TOLERANCE IS NOT HARD-CODED. The shipped 5.2 ground move-along arrival is
at-distance 1.0 m (2.0 m maneuver-in-formation), near-distance 15 m
(EntityLevel/.../ground-tracked.sysdef, RESEARCH_52_MOVEMENT_ORDER G3) - NOT the
old 250 m rule. But a WatchVrf trace sampled at ~1-2 s cannot resolve a 1 m
approach for a vehicle moving several m/s, so --expect-end REPORTS the closest
approach and leaves the tolerance judgement to the caller.

NOT-REAL OBJECTS ARE NOT SCORED. Same filter as placement_check.py / run_census.py:
the 5.2 control form is (NaN, +90, NaN) or (0, +90, huge-alt) (GlobalEnv), the
5.0.2 forms are the (+/-90, -90) pole placeholder and |alt| > 1e8. They are listed
under SKIPPED and never counted as a pass or a fail.

UUIDS. A POS line carries the VR-Forces uuid, not the C2SIM uuid. Labels come from
--vendor-log's "Locally Simulated: <name> (VRF_UUID:...)" lines; the 5.2 vendor log
QUOTES the name ("GlobalEnv 1"), the 5.0.2 log does not - both are handled (the
surrounding quotes are stripped). --expect-* keys match a label (or uuid) exactly
first, then as a unique substring; an ambiguous substring matches nothing.

EXIT CODES (the tools/Shared/ToolArgs.cs standard): 0 all checks passed, 1 at
least one FAILED (or nothing was scorable - an empty trace is never a pass), 2
usage error.
"""
import math
import os
import sys

USAGE = [
    "usage: python tools/analysis/movement_check.py <watch.trace> [options]",
    "",
    "  --move-threshold M     disp > M counts as MOVED (default 5.0)",
    "  --vendor-log PATH      bin64 vrfSim log; labels uuids from its",
    "                         'Locally Simulated: <name> (VRF_UUID:...)' lines and,",
    "                         supplementary, counts '... has Completed' controller lines",
    "  --app-log PATH         VrfC2SimApp log; source of completion + the DROPPING/",
    "                         ABANDONING health gate (nonzero => FAIL)",
    "  --expect-move NAME     NAME (label/uuid substring) MUST show MOVED; repeatable",
    "  --expect-complete NAME NAME MUST have an app-log completion; repeatable",
    "  --expect-end NAME=LAT,LON  report NAME's closest approach to LAT,LON (m).",
    "                         REPORTED, not pass/failed: a 1-2 s trace cannot resolve",
    "                         the shipped 1.0/2.0 m arrival tol - the caller judges.",
    "  --zero-tol M           |alt| <= M is a forbidden ZERO altitude (default 1.0)",
    "  --high M               forbidden high altitude (default 10000.0)",
    "  --high-tol M           |alt - high| <= M is a forbidden HIGH altitude (default 50.0)",
    "  --show-skipped         list the not-real objects that were filtered out",
    "",
    "FAIL (exit 1) if: nothing scorable; any scored object hits a forbidden altitude;",
    "  DROPPING TASK or ABANDONING TASK count > 0; a COMPLETED taskee did not move",
    "  (VACUOUS); or any --expect-move / --expect-complete is unmet.",
    "",
    "example (PREREG_R9_52 order run):",
    "  python tools/analysis/movement_check.py runs/launch52/watch_<appNo>.trace \\",
    "      --vendor-log runs/launch52/vrfSim_<appNo>_<stamp>.log \\",
    "      --app-log    runs/launch52/app_<appNo>_<stamp>.log \\",
    "      --expect-move 1222.MechPlt --expect-move 114.MechCoy --expect-move 1.BdeHQ \\",
    "      --expect-complete 1222.MechPlt --expect-complete 114.MechCoy \\",
    "      --expect-complete 1.BdeHQ",
]


def usage(problem):
    sys.stderr.write("movement_check: " + problem + "\n\n")
    for line in USAGE:
        sys.stderr.write(line + "\n")
    return 2


def parse_float(text):
    """Return a float, or None when the field is not a finite number.

    VR-Forces emits NaN through the observer for control objects, and the platform
    spelling of that is not portable, so an unparseable field is a non-position.
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
        return True                      # 5.0.2 / 5.2 huge-alt placeholder (GlobalEnv)
    if abs(abs(lat) - 90.0) < 1e-9 and abs(lon + 90.0) < 1e-9:
        return True                      # 5.0.2 pole placeholder, either sign
    if abs(lat) < 1e-9 and abs(abs(lon) - 90.0) < 1e-9:
        return True                      # the (0, +90) form seen in the 5.2 traces
    return False


def great_circle(lat1, lon1, lat2, lon2):
    """Haversine distance in metres (R = 6371000 m, spherical earth).

    Distances here are hundreds to thousands of metres over a small AOI, where the
    spherical approximation is well under the trace's own sampling noise.
    """
    radius = 6371000.0
    p1 = math.radians(lat1)
    p2 = math.radians(lat2)
    dphi = math.radians(lat2 - lat1)
    dlmb = math.radians(lon2 - lon1)
    h = (math.sin(dphi / 2.0) ** 2
         + math.cos(p1) * math.cos(p2) * math.sin(dlmb / 2.0) ** 2)
    return 2.0 * radius * math.asin(min(1.0, math.sqrt(h)))


def read_trace(path):
    """-> (samples, skipped) where samples[uuid] = [(t, lat, lon, alt), ...].

    INPUT FORMAT (tools/WatchVrf/WatchRunner.cs): POS,<t>,<uuid>,<lat>,<lon>,<alt>.
    Every other line shape (CON/TSK/RPT, '#' summaries, banner) is ignored.
    """
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

    5.2 QUOTES the name ('GlobalEnv 1'), 5.0.2 does not - the surrounding double
    quotes are stripped so the label matches the app-log's unquoted names. MEMBER
    platforms of a disaggregated unit appear here too.
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


def count_vendor_completions(path):
    """Supplementary count of VR-Forces controller-completion lines.

    'Controller ... 's task has Completed' / 'subtask has Completed' in the vendor
    sim log. Count only - the log interleaves concurrent entity writes, so the
    entity names on these lines are unreliable.
    """
    count = 0
    with open(path, "r", encoding="utf-8", errors="replace") as handle:
        for raw in handle:
            if "has Completed" in raw:
                count += 1
    return count


def read_app_log(path):
    """Parse the app log for completions + the health gate.

    -> dict:
       completed   { name: [task-type, ...] }   from 'VRF task complete: N / T'
       report_count  count of 'SENT TASK STATUS REPORT (TASKCMPLT)'
       dropping    count of 'DROPPING TASK'
       abandoning  count of 'ABANDONING TASK'
       order_seen  True if 'C2SIM Order received' appeared (context, not scored)
    """
    completed = {}
    report_count = 0
    dropping = 0
    abandoning = 0
    order_seen = False
    tag = "VRF task complete: "
    with open(path, "r", encoding="utf-8", errors="replace") as handle:
        for raw in handle:
            line = raw.rstrip("\r\n")
            at = line.find(tag)
            if at >= 0:
                rest = line[at + len(tag):].strip()
                if " / " in rest:
                    name, task_type = rest.split(" / ", 1)
                else:
                    name, task_type = rest, ""
                completed.setdefault(name.strip(), []).append(task_type.strip())
            if "SENT TASK STATUS REPORT (TASKCMPLT)" in line:
                report_count += 1
            if "DROPPING TASK" in line:
                dropping += 1
            if "ABANDONING TASK" in line:
                abandoning += 1
            if "C2SIM Order received" in line:
                order_seen = True
    return {
        "completed": completed,
        "report_count": report_count,
        "dropping": dropping,
        "abandoning": abandoning,
        "order_seen": order_seen,
    }


def match_key(key_text, candidates):
    """Return the single candidate that key_text names, or None.

    Exact match first, then key_text as a unique substring of exactly one
    candidate; an ambiguous or absent key resolves to None rather than guessing.
    """
    if key_text in candidates:
        return key_text
    hits = [c for c in candidates if c and key_text in c]
    if len(hits) == 1:
        return hits[0]
    return None


def main(argv):
    trace_path = None
    move_threshold = 5.0
    vendor_log = None
    app_log = None
    expect_move = []
    expect_complete = []
    expect_end = {}                       # name -> (lat, lon)
    zero_tol = 1.0
    high = 10000.0
    high_tol = 50.0
    show_skipped = False

    index = 0
    while index < len(argv):
        arg = argv[index]
        if arg in ("--move-threshold", "--zero-tol", "--high", "--high-tol"):
            index += 1
            if index >= len(argv):
                return usage(arg + " needs a number")
            value = parse_float(argv[index])
            if value is None:
                return usage(arg + " '" + argv[index] + "' is not a number")
            if arg == "--move-threshold":
                move_threshold = value
            elif arg == "--zero-tol":
                zero_tol = value
            elif arg == "--high":
                high = value
            else:
                high_tol = value
        elif arg == "--vendor-log":
            index += 1
            if index >= len(argv):
                return usage("--vendor-log needs a path")
            vendor_log = argv[index]
        elif arg == "--app-log":
            index += 1
            if index >= len(argv):
                return usage("--app-log needs a path")
            app_log = argv[index]
        elif arg in ("--expect-move", "--expect-complete"):
            index += 1
            if index >= len(argv):
                return usage(arg + " needs a NAME")
            name = argv[index].strip()
            if not name:
                return usage(arg + " needs a non-empty NAME")
            if arg == "--expect-move":
                expect_move.append(name)
            else:
                expect_complete.append(name)
        elif arg == "--expect-end":
            index += 1
            if index >= len(argv):
                return usage("--expect-end needs NAME=LAT,LON")
            text = argv[index]
            if "=" not in text:
                return usage("--expect-end needs NAME=LAT,LON, got '" + text + "'")
            name, coords = text.split("=", 1)
            if "," not in coords:
                return usage("--expect-end coords need LAT,LON, got '" + coords + "'")
            lat_text, lon_text = coords.split(",", 1)
            lat = parse_float(lat_text)
            lon = parse_float(lon_text)
            if not name.strip() or lat is None or lon is None:
                return usage("--expect-end '" + text + "' is not a usable NAME=LAT,LON")
            expect_end[name.strip()] = (lat, lon)
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
    if app_log is not None and not os.path.isfile(app_log):
        return usage("--app-log '" + app_log + "' does not exist")

    samples, skipped = read_trace(trace_path)
    names = read_names(vendor_log) if vendor_log else {}
    vendor_completions = count_vendor_completions(vendor_log) if vendor_log else None
    app = read_app_log(app_log) if app_log else None

    print("movement_check: " + trace_path)
    print("  move-threshold=%.1f m  forbidden alt: |alt|<=%.1f and %.1f+/-%.1f"
          % (move_threshold, zero_tol, high, high_tol))
    if vendor_log:
        print("  vendor-log=%s (labels=%d, '...has Completed' lines=%d)"
              % (vendor_log, len(names), vendor_completions))
    if app_log:
        print("  app-log=%s (order-received=%s, TASKCMPLT-reports=%d, "
              "DROPPING=%d, ABANDONING=%d)"
              % (app_log, app["order_seen"], app["report_count"],
                 app["dropping"], app["abandoning"]))
    for name in expect_move:
        print("  expect-move: %s" % name)
    for name in expect_complete:
        print("  expect-complete: %s" % name)
    for name in sorted(expect_end):
        print("  expect-end: %s -> %.5f,%.5f" % (name, expect_end[name][0], expect_end[name][1]))
    print("")

    # ---- score every real object -----------------------------------------
    failures = 0
    # per-uuid record we reuse for expectation resolution
    scored = {}      # uuid -> dict(label, moved, disp, ...)
    label_index = {} # label -> [uuid, ...]  (labels can repeat, e.g. members)

    for uuid in sorted(samples):
        series = samples[uuid]
        first_t, first_lat, first_lon, first_alt = series[0]
        last_t, last_lat, last_lon, last_alt = series[-1]
        alts = [row[3] for row in series]
        disp = great_circle(first_lat, first_lon, last_lat, last_lon)
        maxdisp = max(great_circle(first_lat, first_lon, r[1], r[2]) for r in series)
        moved = disp > move_threshold
        label = names.get(uuid, "")

        reasons = []
        if any(abs(alt) <= zero_tol for alt in alts):
            reasons.append("ZERO(|alt|<=%.1f seen)" % zero_tol)
        if any(abs(alt - high) <= high_tol for alt in alts):
            reasons.append("HIGH(%.0f+/-%.0f seen)" % (high, high_tol))

        # arrival (report only) for any --expect-end name that matches this label
        arrival_text = ""
        for name in expect_end:
            if label and name in label:
                elat, elon = expect_end[name]
                closest = min(great_circle(r[1], r[2], elat, elon) for r in series)
                arrival_text = "  arrival(%s): closest=%.1f m" % (name, closest)
                break

        if reasons:
            failures += 1

        scored[uuid] = {
            "label": label, "moved": moved, "disp": disp, "n": len(series),
            "alt_fail": bool(reasons),
        }
        if label:
            label_index.setdefault(label, []).append(uuid)

        verdict = "FAIL" if reasons else ("MOVED" if moved else "still")
        print("%-5s %s%s n=%d disp=%.1f maxdisp=%.1f first=(%.5f,%.5f,%.1f) "
              "last=(%.5f,%.5f,%.1f)%s%s"
              % (verdict, uuid, (" [" + label + "]") if label else "",
                 len(series), disp, maxdisp,
                 first_lat, first_lon, first_alt, last_lat, last_lon, last_alt,
                 arrival_text, ("  " + "; ".join(reasons)) if reasons else ""))

    # ---- completion + vacuous check --------------------------------------
    print("")
    completed = app["completed"] if app else {}
    if app:
        if completed:
            for name in sorted(completed):
                types = ", ".join(t for t in completed[name] if t) or "(no type)"
                # find scored objects for this completed name and test vacuity
                moved_here = None
                for uuid, rec in scored.items():
                    if rec["label"] and name in rec["label"]:
                        moved_here = moved_here or rec["moved"]
                if moved_here is None:
                    vac = "  (no trace object labelled '%s' - movement unknown)" % name
                elif not moved_here:
                    vac = "  VACUOUS(completed but disp<=%.1f m)" % move_threshold
                    failures += 1
                else:
                    vac = ""
                print("COMPLETE %s / %s%s" % (name, types, vac))
        else:
            print("COMPLETE (none: no 'VRF task complete' line in the app log)")
        if app["dropping"] or app["abandoning"]:
            failures += 1
            print("HEALTH FAIL: DROPPING=%d ABANDONING=%d (G4 create-before-task gate; "
                  "must be 0)" % (app["dropping"], app["abandoning"]))
        else:
            print("HEALTH ok: DROPPING=0 ABANDONING=0")
    else:
        print("COMPLETE (not checked: no --app-log given)")

    # ---- expectations ----------------------------------------------------
    if expect_move or expect_complete:
        print("")
    for name in expect_move:
        target = match_key(name, label_index)
        moved_any = False
        if target is not None:
            moved_any = any(scored[u]["moved"] for u in label_index[target])
        else:
            # fall back to uuid substring match across scored objects
            uhits = [u for u in scored if name in u]
            moved_any = any(scored[u]["moved"] for u in uhits)
            target = uhits[0] if len(uhits) == 1 else None
        if moved_any:
            print("EXPECT-MOVE ok: %s moved" % name)
        else:
            failures += 1
            where = "no object matched" if target is None else "object present but disp<=threshold"
            print("EXPECT-MOVE FAIL: %s did not move (%s)" % (name, where))
    for name in expect_complete:
        got = match_key(name, completed) is not None or name in completed
        if got:
            print("EXPECT-COMPLETE ok: %s completed" % name)
        else:
            failures += 1
            print("EXPECT-COMPLETE FAIL: %s has no completion in the app log" % name)

    # ---- skipped ---------------------------------------------------------
    if show_skipped and skipped:
        print("")
        for uuid in sorted(skipped):
            lab = names.get(uuid, "")
            print("SKIP %s%s %d sample(s) - not a real position (control/placeholder)"
                  % (uuid, (" [" + lab + "]") if lab else "", skipped[uuid]))

    # ---- verdict ---------------------------------------------------------
    moved_count = sum(1 for r in scored.values() if r["moved"])
    print("")
    print("scored=%d moved=%d still=%d alt-fail=%d not-real-skipped=%d"
          % (len(scored), moved_count, len(scored) - moved_count,
             sum(1 for r in scored.values() if r["alt_fail"]), len(skipped)))
    if len(scored) == 0:
        print("RESULT: FAIL - no scorable POS object in the trace. An empty trace is "
              "not a pass (the false-green rule).")
        return 1
    print("RESULT: " + ("PASS" if failures == 0 else "FAIL"))
    return 0 if failures == 0 else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
