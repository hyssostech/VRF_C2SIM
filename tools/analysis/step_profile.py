#!/usr/bin/env python
"""
step_profile.py - OFFLINE sim-time step / clock-jitter profile of a runner run.

Reads only existing run artifacts (bin64-vrfSim.log, watchvrf-trace.csv,
run-manifest.json). Never touches VR-Forces, the RTI, or C:\\MAK.

Evidence sources and what each can and cannot show
  (a) bin64-vrfSim.log  - task-event lines carry "[wall 1 s] <sim>.mmm:". Sparse
      (a few hundred per run, clustered at task start/complete). Consecutive
      DISTINCT sim stamps are on tick boundaries, so the SMALLEST differences
      bound the sim step per tick; wall stamps are 1 s resolution, so the clock
      rate is only recoverable over clusters spanning >= 3 wall s.
      The file is written by several threads and lines interleave; a stamp is
      accepted only if the float is followed by ':' or ' ' and lies in
      [0, 5000). Non-monotonic stamps inside a wall second are kept (different
      entities), the garbled rest of the line is not parsed.
  (b) watchvrf-trace.csv RPT records - "POSITION <name> lat lon" text reports
      that every entity emits on a fixed SIM-time period (~61.2 sim s: 61.2 wall
      s at 1x, 12.25 wall s at 5x). Their receipt wall time (0.1 s resolution)
      is the densest sim-clock proxy available: 37 intervals x 44 entities per
      5x run. A sim stall or jump of d wall-s shows as +/- d on every interval
      that spans it.
  (c) watchvrf-trace.csv POS records - dead-reckoned observer positions at
      ~2 s wall (RUNBOOK: no RAW record type exists). Per-sample steps are
      DR-poisoned at 5x (velocity is in sim m/s but extrapolated over wall
      time) and cannot resolve a single tick. Used only run-vs-run (P3 vs P3R,
      both 5x, same observer) and for the M1A2 18 approach reconstruction.

Usage: python tools/analysis/step_profile.py <repo_root> <run_id> [<run_id> ...]
"""
import json
import math
import os
import re
import statistics
import sys
from collections import defaultdict

# Vendor stamp: "[Www Mmm D HH:MM:SS YYYY] <sim>.mmm". The weekday/month/day/year
# are NOT pinned (a hard-coded "Tue Sep  1" silently parsed ZERO stamps from any
# other day's log - a false green). Only HH:MM:SS and the sim float are captured,
# so the wall axis remains seconds-of-day: a run that crosses midnight would wrap.
STAMP_RE = re.compile(
    r'\[[A-Za-z]{3} [A-Za-z]{3} [ 0-9]\d (\d\d):(\d\d):(\d\d) \d{4}\] (\d+\.\d{3})(?=[: ])')
RPT_RE = re.compile(r'^RPT,([\d.]+),"POSITION ""([^"]+)"" ([-\d.]+) ([-\d.]+)"')
POS_RE = re.compile(r'^POS,([\d.]+),(VRF_UUID:[0-9a-f-]+),([-\d.]+),([-\d.]+),([-\d.]+)')
NAME_RE = re.compile(r'Locally Simulated: (.+?) \((VRF_UUID:[0-9a-f-]+)\) using parameters')
REMOVE_RE = re.compile(r'removing sim object (VRF_UUID:[0-9a-f-]+) (.+?)\s*$')

FOLLOWERS = ['M1A2 2', 'M1A2 3', 'M1A2 4', 'AUV 1', 'M3 1', 'M1A2 6', 'HMMWV 1', 'HMMWV 2',
             'M1A2 8', 'M1A2 9', 'M1A2 10', 'M1A2 12', 'M1A2 13', 'M1A2 14',
             'M1A2 16', 'M1A2 17', 'M1A2 18']
LEADERS = ['M1A2 1', 'M1A2 5', 'M1A2 7', 'M1A2 11', 'M1A2 15']


def pct(xs, p):
    if not xs:
        return float('nan')
    xs = sorted(xs)
    k = (len(xs) - 1) * p
    f = math.floor(k)
    c = min(f + 1, len(xs) - 1)
    return xs[f] + (xs[c] - xs[f]) * (k - f)


def enu(lat0, lon0, lat, lon):
    """Local east/north metres of (lat, lon) relative to (lat0, lon0)."""
    r = 6378137.0
    dn = math.radians(lat - lat0) * r
    de = math.radians(lon - lon0) * r * math.cos(math.radians(lat0))
    return de, dn


def dist(a, b):
    de, dn = enu(a[0], a[1], b[0], b[1])
    return math.hypot(de, dn)


# ---------------------------------------------------------------- (a) vendor log
def vendor_log(path):
    total = 0
    stamped = 0
    discarded = 0
    pairs = set()
    with open(path, encoding='utf-8', errors='replace') as fh:
        for line in fh:
            total += 1
            hits = STAMP_RE.findall(line)
            if not hits:
                continue
            for h, m, s, t in hits:
                stamped += 1
                t = float(t)
                if t >= 5000:
                    discarded += 1
                    continue
                pairs.add((int(h) * 3600 + int(m) * 60 + int(s), t))
    pairs = sorted(pairs, key=lambda p: (p[1], p[0]))
    sims = sorted(set(t for _, t in pairs))
    # clusters of stamps separated by < 5 sim s
    clusters = []
    cur = [sims[0]]
    for a, b in zip(sims, sims[1:]):
        if b - a < 5:
            cur.append(b)
        else:
            clusters.append(cur)
            cur = [b]
    clusters.append(cur)
    out = {'lines': total, 'stamped': stamped, 'discarded': discarded,
           'distinct': len(sims), 'clusters': []}
    small = []
    for c in clusters:
        diffs = [b - a for a, b in zip(c, c[1:])]
        walls = sorted(set(w for w, t in pairs if c[0] <= t <= c[-1]))
        rate = None
        if walls[-1] - walls[0] >= 3:
            rate = (c[-1] - c[0]) / (walls[-1] - walls[0])
        small += [d for d in diffs if d < 0.06]
        out['clusters'].append({'sim0': c[0], 'sim1': c[-1], 'n': len(c),
                                'wall0': walls[0], 'wall1': walls[-1], 'rate': rate,
                                'diffs': diffs})
    out['tick_proxy'] = sorted(small)
    # least-squares clock over the whole run (wall 1 s quantised)
    xs = [w for w, t in pairs]
    ys = [t for w, t in pairs]
    if len(xs) > 2:
        mx, my = statistics.mean(xs), statistics.mean(ys)
        sxx = sum((x - mx) ** 2 for x in xs)
        sxy = sum((x - mx) * (y - my) for x, y in zip(xs, ys))
        slope = sxy / sxx
        resid = [y - (my + slope * (x - mx)) for x, y in zip(xs, ys)]
        out['ls_slope'] = slope
        out['ls_resid_sd'] = statistics.pstdev(resid)
        out['ls_resid_max'] = max(abs(r) for r in resid)
    return out


def name_map(path):
    m = {}
    with open(path, encoding='utf-8', errors='replace') as fh:
        for line in fh:
            for name, uuid in NAME_RE.findall(line):
                m.setdefault(uuid, name)
            for uuid, name in REMOVE_RE.findall(line):
                m.setdefault(uuid, name)
    return m


# ---------------------------------------------------------------- (b)+(c) trace
def trace(path):
    rpt = defaultdict(list)
    pos = defaultdict(list)
    with open(path, encoding='utf-8', errors='replace') as fh:
        for line in fh:
            m = RPT_RE.match(line)
            if m:
                rpt[m.group(2)].append((float(m.group(1)), float(m.group(3)), float(m.group(4))))
                continue
            m = POS_RE.match(line)
            if m:
                lat, lon = float(m.group(3)), float(m.group(4))
                if abs(lat) > 89 or lat == 0.0:
                    continue  # pre-placement pole / NaN samples
                pos[m.group(2)].append((float(m.group(1)), lat, lon, float(m.group(5))))
    return rpt, pos


def rpt_intervals(rpt, names=None):
    """(t_mid, dt, name) for consecutive report pairs of each entity."""
    out = []
    for name, rows in rpt.items():
        if names and name not in names:
            continue
        ts = [t for t, _, _ in rows]
        for a, b in zip(ts, ts[1:]):
            out.append(((a + b) / 2, b - a, name))
    return out


def move_phase(rows, start_r=5.0):
    """Indices [i0, i1] of the moving phase: first sample > start_r from the
    first sample, through the first sample of the terminal identical plateau."""
    if len(rows) < 3:
        return None
    p0 = (rows[0][1], rows[0][2])
    i0 = None
    for i, r in enumerate(rows):
        if dist(p0, (r[1], r[2])) > start_r:
            i0 = i
            break
    if i0 is None:
        return None
    last = (rows[-1][1], rows[-1][2])
    i1 = len(rows) - 1
    while i1 > 0 and (rows[i1 - 1][1], rows[i1 - 1][2]) == last:
        i1 -= 1
    return i0, i1


def pos_steps(rows, i0, i1):
    steps = []
    for i in range(max(i0, 1), i1 + 1):
        a, b = rows[i - 1], rows[i]
        steps.append((b[0], dist((a[1], a[2]), (b[1], b[2])), b[0] - a[0]))
    return steps


def approach(rows, i1, window=60.0):
    """Distance-to-final vs t over the last `window` wall-s before settle."""
    final = (rows[i1][1], rows[i1][2])
    t1 = rows[i1][0]
    out = []
    for r in rows:
        if t1 - window <= r[0] <= t1 + 6:
            out.append((r[0], dist(final, (r[1], r[2])), r[1], r[2]))
    return out


def bearing(a, b):
    de, dn = enu(a[0], a[1], b[0], b[1])
    return (math.degrees(math.atan2(de, dn)) + 360) % 360


def main():
    root = sys.argv[1]
    runs = sys.argv[2:]
    for run in runs:
        rd = os.path.join(root, 'runs', run)
        print('=' * 78)
        print('RUN', run)
        man = json.load(open(os.path.join(rd, 'run-manifest.json'), encoding='utf-8'))
        print('  orderPushedUtc', man['clocks']['orderPushedUtc'],
              'trace start', [s for s in man['stages'] if s['name'] == 'WatchVrf-trace'][0]['startedUtc'])
        # (a)
        v = vendor_log(os.path.join(rd, 'bin64-vrfSim.log'))
        print('  (a) vrfSim.log lines %d, stamped %d, discarded(garbled float) %d, distinct stamps %d'
              % (v['lines'], v['stamped'], v['discarded'], v['distinct']))
        print('      LS clock slope %.3f sim-s/wall-s, resid sd %.2f max %.2f (wall quantised to 1 s)'
              % (v['ls_slope'], v['ls_resid_sd'], v['ls_resid_max']))
        tp = v['tick_proxy']
        print('      tick proxy (consecutive distinct stamp diffs < 0.06 s): n=%d min %.3f median %.3f max %.3f'
              % (len(tp), min(tp), statistics.median(tp), max(tp)))
        print('      ', ' '.join('%.3f' % d for d in tp))
        for c in v['clusters']:
            r = '%.2fx' % c['rate'] if c['rate'] else '   -  '
            print('      cluster sim %8.3f-%8.3f n=%2d wall %02d:%02d:%02d-%02d:%02d:%02d rate %s'
                  % (c['sim0'], c['sim1'], c['n'],
                     c['wall0'] // 3600, c['wall0'] % 3600 // 60, c['wall0'] % 60,
                     c['wall1'] // 3600, c['wall1'] % 3600 // 60, c['wall1'] % 60, r))
        # (b)
        names = name_map(os.path.join(rd, 'bin64-vrfSim.log'))
        rpt, pos = trace(os.path.join(rd, 'watchvrf-trace.csv'))
        ents = FOLLOWERS + LEADERS + ['M1A2 19', 'M1A2 20', 'M1A2 21', 'M1A2 22', 'M1A2 23', 'M1A2 24', 'M1A2 25', 'M1A2 26', 'M1A2 27', 'M1A2 28']
        iv = rpt_intervals(rpt)
        dts = [d for _, d, _ in iv]
        print('  (b) RPT intervals all entities: n=%d mean %.3f sd %.3f min %.2f p5 %.2f p95 %.2f max %.2f'
              % (len(dts), statistics.mean(dts), statistics.pstdev(dts), min(dts), pct(dts, .05), pct(dts, .95), max(dts)))
        mean = statistics.mean(dts)
        # time series in 10 s wall bins
        bins = defaultdict(list)
        for tm, d, _ in iv:
            bins[int(tm // 10) * 10].append(d)
        print('      per-10s-bin mean interval (deviation from run mean, s), first 12 bins after order:')
        for k in sorted(bins)[:14]:
            b = bins[k]
            print('        t=%4d-%4d n=%3d mean %.3f dev %+.3f max %.2f min %.2f'
                  % (k, k + 10, len(b), statistics.mean(b), statistics.mean(b) - mean, max(b), min(b)))
        outl = [(tm, d, n) for tm, d, n in iv if abs(d - mean) > 0.5]
        print('      intervals deviating > 0.5 s from mean: %d' % len(outl), outl[:10])
        # (c)
        byname = {}
        for uuid, rows in pos.items():
            n = names.get(uuid)
            if n:
                byname[n] = rows
        missing = [e for e in FOLLOWERS + LEADERS if e not in byname]
        print('  (c) POS entities mapped %d; unmapped taskees %s' % (len(byname), missing))
        allsteps = []
        cruise_cv = []
        for group, members in (('followers', FOLLOWERS), ('leaders', LEADERS)):
            gsteps = []
            for e in members:
                rows = byname.get(e)
                if not rows:
                    continue
                mp = move_phase(rows)
                if not mp:
                    continue
                st = pos_steps(rows, mp[0], mp[1])
                gsteps += st
                # cruise = middle 50% of the move by index
                mid = st[len(st) // 4: 3 * len(st) // 4]
                if len(mid) >= 4:
                    ds = [s[1] for s in mid]
                    cruise_cv.append((e, statistics.mean(ds), statistics.pstdev(ds) / statistics.mean(ds)))
            ds = [s[1] for s in gsteps]
            print('      %s: moving samples %d, step/2s-sample: mean %.1f sd %.1f p50 %.1f p95 %.1f p99 %.1f max %.1f m'
                  % (group, len(ds), statistics.mean(ds), statistics.pstdev(ds), pct(ds, .5), pct(ds, .95), pct(ds, .99), max(ds)))
            allsteps += gsteps
        print('      cruise-phase step CV per entity (mean m, cv):',
              ' '.join('%s %.1f/%.2f' % (e, m, cv) for e, m, cv in cruise_cv))
        cvs = [cv for _, _, cv in cruise_cv]
        print('      cruise CV median %.3f max %.3f' % (statistics.median(cvs), max(cvs)))
        # sampling cadence of the trace itself
        ts = sorted(set(r[0] for rows in byname.values() for r in rows))
        sd = [b - a for a, b in zip(ts, ts[1:])]
        print('      trace sample cadence: n=%d mean %.3f min %.2f max %.2f' % (len(sd), statistics.mean(sd), min(sd), max(sd)))
        # M1A2 18 and siblings approach
        print('  M1A2 18 / AR Plt 3 approach (distance to own final, m, vs trace t):')
        for e in ['M1A2 15', 'M1A2 16', 'M1A2 17', 'M1A2 18']:
            rows = byname.get(e)
            if not rows:
                continue
            mp = move_phase(rows)
            i1 = mp[1]
            ap = approach(rows, i1, 40.0)
            final = (rows[i1][1], rows[i1][2])
            # bearing of approach from the last sample > 20 m out
            far = [a for a in ap if a[1] > 20]
            brg = bearing((far[-1][2], far[-1][3]), final) if far else float('nan')
            laststep = ap[-1][1] if False else None
            steps = [(round(a[0], 1), round(a[1], 1)) for a in ap]
            # last non-zero step size
            st = pos_steps(rows, mp[0], i1)
            print('    %-8s settle t=%.1f final %.6f %.6f approach brg %.0f deg; last 3 steps %s m; (t,dist) %s'
                  % (e, rows[i1][0], final[0], final[1], brg,
                     ['%.2f' % s[1] for s in st[-3:]], steps))
            # post-settle motion
            post = [dist(final, (r[1], r[2])) for r in rows[i1:]]
            print('             post-settle samples %d, max drift from final %.3f m' % (len(post), max(post)))
        # RPT truth positions for M1A2 18 around arrival
        for e in ['M1A2 18', 'M1A2 15']:
            rows = rpt.get(e, [])
            fin = rows[-1]
            print('    RPT %-8s n=%d last 6 (t, dist-to-last-RPT m): %s' % (
                e, len(rows), [(round(t, 1), round(dist((fin[1], fin[2]), (la, lo)), 1)) for t, la, lo in rows[-6:]]))
            if e in byname:
                pf = byname[e][-1]
                print('            RPT final vs POS final: %.2f m' % dist((fin[1], fin[2]), (pf[1], pf[2])))


if __name__ == '__main__':
    main()
