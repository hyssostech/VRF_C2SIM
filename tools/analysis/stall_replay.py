"""stall_replay.py - replay a run's position trace through the C16 progress-watchdog rule.

The watchdog (src/VrfC2SimApp/StallPolicy.cs, VrfSettings.Stall*) reports ONE C2SIM TaskStatus
with TASKABRT when NO member of a moving unit covers StallMoveMeters of NET displacement over
StallWindowSeconds of WALL time. VR-Forces 5.2 never reports such a unit itself
(docs/experiments/FINDING_EARLY_STOPS_2026-09-13.md sec 6a), so the thresholds cannot be
calibrated against the vendor - they are calibrated against traces we already hold.

This tool applies the SAME rule offline, on a run's watchvrf-trace.csv POS rows, so a change to
the defaults can be scored without re-running the simulation:

  window          net displacement per member between the oldest sample in the window and now
  fire            every member with data under the threshold (a moving leader = no fire)
  suppression     scanning stops the moment the ARRIVAL rule (ArrivalPolicy/C15, > 50 % of
                  members within 500 m of the task's last vertex) would have fired, because that
                  is when the live interface pops the in-flight record and the watchdog stops
                  looking at the unit. A fire after that point is not reachable in the product.

Joins (all from files the runner already writes):
  vrfc2simapp.log  "VRF console level N requested for <unit> (VRF_UUID:<agg>)."     unit <-> aggregate
                   "... for M members of <unit>: <name> [VRF_UUID:<u>], ..."        unit -> members
                   "Route '<task> ROUTE' (...) created; MoveAlongRoute issued for VRF_UUID:<agg>"
                   "ARRIVAL EVIDENCE: <unit> task '<task>'" / "VRF task complete: <unit> / ..."
  <order>.xml      task name -> its last route vertex (the destination)
  watchvrf-trace.csv
                   POS,<wall s>,VRF_UUID:<u>,<lat>,<lon>,<alt>    member positions
                   CON,<wall s>,VRF_UUID:<u>,<level>,<msg>        the unit's own console

DISPATCH ANCHOR. The app log carries NO timestamps, so the trace's own clock is the only one
available. The anchor is the first console row on the TASKED AGGREGATE announcing its move task
("beginning to process ... task" / "Move-Along Route"); this needs Vrf:ObjectConsoleNotifyLevel
>= 2 on our objects during the run. Without it the tool falls back to --dispatch-t and says so;
it never silently anchors at t=0 (every unit stands still before the order is pushed, and a t=0
anchor would manufacture fires).

usage:
  python tools/analysis/stall_replay.py <run-dir> [--order data/COA-STP1_Order.xml]
      [--window 240] [--move-m 50] [--min-since-dispatch 60] [--check 5] [--min-members 1]
      [--arrival-radius 500] [--arrival-fraction 0.5]
      [--expect-fire 1-35,1-6] [--dispatch-t 200] [--quiet]

All files are read with encoding='utf-8' (errors replaced), per the repo's Windows rules.
"""
import argparse
import csv
import glob
import html
import io
import math
import os
import re
import sys

RE_AGG = re.compile(r"VRF console level \d+ requested for (?!\d+ members of )(.+?) \(VRF_UUID:([0-9a-f-]+)\)\.")
RE_MEMBERS = re.compile(r"VRF console level \d+ requested for \d+ members of (.+?): (.+)$")
RE_MEMBER = re.compile(r"([^,]+?) \[VRF_UUID:([0-9a-f-]+)\]")
RE_ISSUED = re.compile(r"Route '(.+?)' \(VRF_UUID:[0-9a-f-]+\) created; (?:R10 fan-out )?MoveAlongRoute "
                       r"issued (?:for|to \d+ members of) VRF_UUID:([0-9a-f-]+)")
RE_MIF = re.compile(r"Task '(.+?)': MoveIntoFormation for AGGREGATE (.+?) \(VRF_UUID:([0-9a-f-]+)\)")
RE_ARRIVAL = re.compile(r"ARRIVAL EVIDENCE: (.+?) task '(.+?)'")
RE_VRFDONE = re.compile(r"VRF task complete: (.+?) / ")
RE_XMLSTR = re.compile(r'<string[^>]*>(.*?)</string>', re.S)
RE_NESTED = re.compile(r'^<string[^>]*>')
RE_SIMPREFIX = re.compile(r'^\s*(\d{1,7}\.\d{1,3})\s+\S')
RE_MOVETASK = re.compile(r'beginning to process|Move-Along Route|move-along', re.I)


def hav(lat1, lon1, lat2, lon2):
    r = 6371000.0
    p1, p2 = math.radians(lat1), math.radians(lat2)
    dp = math.radians(lat2 - lat1)
    dl = math.radians(lon2 - lon1)
    h = math.sin(dp / 2) ** 2 + math.cos(p1) * math.cos(p2) * math.sin(dl / 2) ** 2
    return 2 * r * math.asin(math.sqrt(h))


def decode(msg):
    """WatchVrf stores the console payload as the sim's own translatable-string xml."""
    s = msg.replace('\\n', '\n')
    parts = [RE_NESTED.sub('', html.unescape(p).strip()).strip() for p in RE_XMLSTR.findall(s) if p.strip()]
    parts = [p for p in parts if p]
    return ' | '.join(parts) if parts else s.strip()


def read_app_log(run_dir):
    logs = [f for f in glob.glob(os.path.join(run_dir, '*.log')) if 'app' in os.path.basename(f).lower()]
    if not logs:
        raise SystemExit('no app log (*app*.log) in ' + run_dir)
    text = io.open(logs[0], encoding='utf-8', errors='replace').read()
    agg_of_unit, unit_of_agg, members, tasked, completed = {}, {}, {}, [], set()
    for line in text.splitlines():
        line = line.strip()
        m = RE_AGG.search(line)
        if m:
            agg_of_unit[m.group(1)] = m.group(2)      # last wins: the MATERIALIZED object
            unit_of_agg[m.group(2)] = m.group(1)
        m = RE_MEMBERS.search(line)
        if m:
            members[m.group(1)] = [u for _, u in RE_MEMBER.findall(m.group(2))]
        m = RE_ISSUED.search(line)
        if m:
            route, agg = m.group(1), m.group(2)
            task = route[:-6].strip() if route.endswith(' ROUTE') else route
            tasked.append((task, agg))
        m = RE_MIF.search(line)
        if m:
            tasked.append((m.group(1), m.group(3)))
        m = RE_ARRIVAL.search(line)
        if m:
            completed.add(m.group(1))
        m = RE_VRFDONE.search(line)
        if m:
            completed.add(m.group(1))
    return agg_of_unit, unit_of_agg, members, tasked, completed


def read_order(path):
    """task name -> (lat, lon) of its LAST route vertex (the destination)."""
    text = io.open(path, encoding='utf-8', errors='replace').read()
    dest = {}
    for blk in re.findall(r'<Task>(.*?)</Task>', text, flags=re.S):
        nm = re.search(r'<Name>([^<]+)<', blk)
        pts = re.findall(r'<Latitude>([-\d.eE+]+)</Latitude>\s*<Longitude>([-\d.eE+]+)</Longitude>', blk)
        if nm and pts:
            dest[nm.group(1).strip()] = (float(pts[-1][0]), float(pts[-1][1]))
    return dest


def read_trace(run_dir, want_pos, want_con, con_stride):
    """One pass: POS series for the uuids we care about, CON rows for the tasked aggregates,
    and (wall, sim) pairs for the run's sim/wall ratio."""
    pos = {u: [] for u in want_pos}
    con = {u: [] for u in want_con}
    ratio_pts = {}
    path = os.path.join(run_dir, 'watchvrf-trace.csv')
    n_con = 0
    with io.open(path, encoding='utf-8', errors='replace', newline='') as f:
        for raw in f:
            if raw.startswith('POS,'):
                p = raw.rstrip('\r\n').split(',')
                if len(p) < 5:
                    continue
                u = p[2].rsplit(':', 1)[-1]     # trace column is "VRF_UUID:<hex>"; the log capture is bare
                if u not in pos:
                    continue
                try:
                    t, lat, lon = float(p[1]), float(p[3]), float(p[4])
                except ValueError:
                    continue
                if math.isnan(lat) or math.isnan(lon):
                    continue
                pos[u].append((t, lat, lon))
            elif raw.startswith('CON,'):
                n_con += 1
                p = next(csv.reader([raw.rstrip('\r\n')]))
                if len(p) < 5:
                    continue
                u = p[2].rsplit(':', 1)[-1]
                mine = u in con
                if not mine and (n_con % con_stride):
                    continue
                try:
                    t = float(p[1])
                except ValueError:
                    continue
                msg = decode(','.join(p[4:]))
                sm = RE_SIMPREFIX.match(msg)
                if sm:
                    ratio_pts.setdefault(t, float(sm.group(1)))
                if mine:
                    con[u].append((t, msg))
    return pos, con, sorted(ratio_pts.items())


def ls_slope(pts):
    n = len(pts)
    if n < 10:
        return float('nan')
    mx = sum(p[0] for p in pts) / n
    my = sum(p[1] for p in pts) / n
    sxx = sum((p[0] - mx) ** 2 for p in pts)
    sxy = sum((p[0] - mx) * (p[1] - my) for p in pts)
    return sxy / sxx if sxx > 0 else float('nan')


def at(series, t):
    """last sample at or before t (the live sampler reads the CURRENT reflected position)."""
    lo, hi, found = 0, len(series) - 1, None
    while lo <= hi:
        mid = (lo + hi) // 2
        if series[mid][0] <= t:
            found = series[mid]
            lo = mid + 1
        else:
            hi = mid - 1
    return found


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('run')
    ap.add_argument('--order', default='data/COA-STP1_Order.xml')
    ap.add_argument('--window', type=float, default=240.0)   # = VrfSettings.StallWindowSeconds
    ap.add_argument('--move-m', type=float, default=50.0)
    ap.add_argument('--min-since-dispatch', type=float, default=60.0)
    ap.add_argument('--check', type=float, default=5.0)
    ap.add_argument('--min-members', type=int, default=1)
    ap.add_argument('--arrival-radius', type=float, default=500.0)
    ap.add_argument('--arrival-fraction', type=float, default=0.5)
    ap.add_argument('--expect-fire', default='', help='comma-separated unit-name prefixes that MUST fire')
    ap.add_argument('--dispatch-t', type=float, default=None, help='fallback anchor (wall s) when no console rows')
    ap.add_argument('--con-stride', type=int, default=37, help='decode 1 in N foreign CON rows for the ratio')
    ap.add_argument('--quiet', action='store_true')
    a = ap.parse_args()

    agg_of_unit, unit_of_agg, members, tasked, completed = read_app_log(a.run)
    dest = read_order(a.order)
    expect = [s.strip() for s in a.expect_fire.split(',') if s.strip()]

    # one row per tasked MOVE: the last dispatch wins (a re-task replaces the in-flight task)
    jobs, dispatch_count = {}, {}
    for task, agg in tasked:
        unit = unit_of_agg.get(agg)
        if unit is None:
            continue
        jobs[unit] = (task, agg)
        dispatch_count[unit] = dispatch_count.get(unit, 0) + 1
    # LIMITATION, stated rather than hidden: a unit tasked MORE THAN ONCE is scanned as one
    # stretch - the anchor is its FIRST move-along console row and the destination is its LAST
    # task's. The live watchdog instead resets its window and its reported flag at each new
    # dispatch, so such a row is a lower bound on fidelity; read it with that in mind.
    retasked = sorted(u for u, n in dispatch_count.items() if n > 1)

    want_pos = set()
    for unit, (task, agg) in jobs.items():
        want_pos.update(members.get(unit) or [])
        want_pos.add(agg)
    pos, con, ratio_pts = read_trace(a.run, want_pos, {agg for _, agg in jobs.values()}, max(1, a.con_stride))
    ratio = ls_slope(ratio_pts)

    rows = []
    for unit in sorted(jobs):
        task, agg = jobs[unit]
        mem = [u for u in (members.get(unit) or [agg]) if pos.get(u)]
        total = len(members.get(unit) or [agg])
        # dispatch anchor: the unit's own console announcing the move task
        t0, anchor = None, 'console'
        for t, msg in con.get(agg, []):
            if RE_MOVETASK.search(msg):
                t0 = t
                break
        if t0 is None:
            t0, anchor = a.dispatch_t, 'given'
        if t0 is None:
            rows.append((unit, task, None, None, float('nan'), float('nan'),
                         'NO ANCHOR (no console rows; pass --dispatch-t)', anchor))
            continue
        if not mem:
            rows.append((unit, task, None, None, float('nan'), float('nan'), 'NO POSITION DATA', anchor))
            continue
        tend = max(s[-1][0] for s in (pos[u] for u in mem))
        d = dest.get(task)

        fire, arrive, maxfire, closest = None, None, None, float('inf')
        t = t0 + max(a.min_since_dispatch, a.window)
        while t <= tend:
            # C15 first: an arrival pops the in-flight record, and the watchdog then stops looking
            if d is not None:
                within = 0
                for u in mem:
                    p = at(pos[u], t)
                    if p and hav(p[1], p[2], d[0], d[1]) <= a.arrival_radius:
                        within += 1
                arrived = (within >= total) if a.arrival_fraction >= 1.0 else (within > a.arrival_fraction * total)
                if arrived:
                    arrive = t
                    break
            disp = []
            for u in mem:
                now_p, old_p = at(pos[u], t), at(pos[u], t - a.window)
                if now_p and old_p and old_p[0] >= t0:
                    disp.append(hav(old_p[1], old_p[2], now_p[1], now_p[2]))
            if len(disp) >= max(1, a.min_members):
                closest = min(closest, max(disp))
                if max(disp) < a.move_m:
                    fire, maxfire = t, max(disp)
                    break
            t += a.check

        want = any(unit.startswith(e) for e in expect)
        done = unit in completed
        if fire is not None:
            verdict = 'true positive' if want else 'FALSE ALARM'
        elif arrive is not None:
            verdict = 'MISSED (arrived first)' if want else 'true negative (arrived %.0f s)' % arrive
        else:
            verdict = 'MISSED' if want else 'true negative'
        if done and fire is None:
            verdict += '; reported complete in the log'
        # how far the NEAREST member still is from the destination at the end of the scan - the
        # difference between "stopped short" (a real early stop) and "stopped because it is there"
        last = [at(pos[u], fire or arrive or tend) for u in mem]
        short = min([hav(p[1], p[2], d[0], d[1]) for p in last if p], default=float('nan')) if d else float('nan')
        rows.append((unit, task, fire, maxfire, closest, short, verdict, anchor + ' t0=%.0f' % t0))

    print('== %s ==' % a.run)
    print('rule: window %.0f s wall, move %.0f m net, min %.0f s since dispatch, check %.0f s, '
          'min members with data %d' % (a.window, a.move_m, a.min_since_dispatch, a.check, a.min_members))
    print('sim/wall ratio (LS slope over %d console sim-time samples) = %.2fx' % (len(ratio_pts), ratio))
    if retasked:
        print('NOTE: re-tasked in this run (scanned as one stretch: first anchor, last destination): %s'
              % ', '.join(retasked))
    print('%-24s %-11s %-11s %-11s %-11s %s'
          % ('unit', 'first fire', 'max moved', 'closest', 'short by', 'verdict'))
    for unit, task, fire, maxfire, closest, short, verdict, anchor in rows:
        print('%-24s %-11s %-11s %-11s %-11s %s   [%s]'
              % (unit[:24],
                 '-' if fire is None else '%.0f s' % fire,
                 '-' if maxfire is None else '%.1f m' % maxfire,
                 '-' if closest in (None, float('inf')) or closest != closest else '%.0f m' % closest,
                 '-' if short != short else '%.0f m' % short,
                 verdict, anchor))
        if not a.quiet:
            print('%-24s   task %s' % ('', task))
    print('columns: closest = the SMALLEST per-window max displacement seen while the task ran '
          '(the margin to firing); short by = the nearest member\'s distance to the destination there.')
    bad = [r for r in rows if 'FALSE ALARM' in r[6] or r[6].startswith('MISSED')]
    print('verdict: %d row(s) need attention' % len(bad) if bad else 'verdict: every row as expected')
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main())
