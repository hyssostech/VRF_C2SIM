"""Task-phase timing and RPT truth-position approach per run.

Companion to step_profile.py (same offline-only scope: reads run artifacts,
never touches VR-Forces).  Two views:

  1. Sim-time offsets of every task event in bin64-vrfSim.log relative to the
     order (first 'move-along-controller beginning' stamp), so runs at 1x and
     5x can be compared in SIM time.  Prefix attribution is best effort: the
     vendor log interleaves threads and entity-name prefixes are frequently
     garbled; a garbled prefix is shown as '?...'.
  2. RPT "POSITION" text-report truth points (6-decimal, ~61 sim-s cadence)
     for AR Plt 3 (M1A2 15-18): distance to own final plateau and implied
     speed between reports.  POS records are dead-reckoned and unusable
     during motion (see RUNBOOK ~378).

Usage: python tools/analysis/phase_timing.py <repo_root> <run_id>[:<mult>[:<order_trace_t>]] ...
       e.g. ... 20260901T211310Z_run:1:31.8 20260901T221227Z_run:5:32.5
ASCII only; all file I/O is utf-8 with errors='replace'.
"""
import math
import re
import sys

STAMP_RE = re.compile(r'^(.*?)\[Tue Sep  1 (\d\d:\d\d:\d\d) 2026\] (\d+\.\d{3}):? (.*)$')
RPT_RE = re.compile(r'^RPT,([\d.]+),"POSITION ""([^"]+)"" ([-\d.]+) ([-\d.]+)"')
NAMES = ['M1A2 %d' % i for i in range(1, 29)] + [
    'AUV 1', 'M3 1', 'HMMWV 1', 'HMMWV 2', '1222.MechPlt', '114.MechCoy',
    '1.BdeHQ', 'AR Plt 1', 'AR Plt 2', 'AR Plt 3', 'AR HQ Sec 1', 'M577A2 1']
KEYS = [
    ('move-into-formation-controller beginning', 'MIF-begin'),
    ("move-into-formation-controller's task has Co", 'MIF-done'),
    ('move-into-formation-controller clearing', 'MIF-clear'),
    ('move-to-location beginning', 'MTL-begin'),
    ("move-to-location's subtask has Completed", 'MTL-done'),
    ("turn-to-heading's subtask has Completed", 'TTH-done'),
    ("stop-moving's task has Completed", 'STOP-done'),
    ('move-along-controller beginning', 'MAL-begin'),
    ('follow-in-formation beginning', 'FOL-begin'),
    ('lead-formation beginning', 'LEAD-begin'),
    ("follow-in-formation's task has Completed", 'FOL-done'),
    ("lead-formation's task has Completed", 'LEAD-done'),
    ("move-along-controller's task has Completed", 'MAL-done'),
]


def phase_events(path, horizon=400.0):
    order = None
    ev = []
    n_stamped = n_discard = 0
    with open(path, encoding='utf-8', errors='replace') as f:
        for line in f:
            m = STAMP_RE.match(line.rstrip('\n'))
            if not m:
                continue
            n_stamped += 1
            prefix, wall, sim, rest = m.groups()
            sim = float(sim)
            if sim > 5000:
                n_discard += 1
                continue
            p = prefix.strip().rstrip(':').strip()
            who = p if p in NAMES else ('?' + p[-14:] if p else '?')
            if order is None and 'move-along-controller beginning' in rest:
                order = sim
            if order is None or sim - order > horizon:
                continue
            for needle, key in KEYS:
                if needle in rest:
                    ev.append((sim, sim - order, key, who, wall))
                    break
    return order, sorted(ev), n_stamped, n_discard


def enu(lat, lon, lat0, lon0):
    r = 6378137.0
    return (math.radians(lon - lon0) * r * math.cos(math.radians(lat0)),
            math.radians(lat - lat0) * r)


def rpt_approach(path, mult, order_t, names=('M1A2 15', 'M1A2 16', 'M1A2 17', 'M1A2 18')):
    by = {}
    with open(path, encoding='utf-8', errors='replace') as f:
        for line in f:
            m = RPT_RE.match(line)
            if not m:
                continue
            t, n, lat, lon = float(m.group(1)), m.group(2), float(m.group(3)), float(m.group(4))
            by.setdefault(n, []).append((t, lat, lon))
    out = []
    for n in names:
        pts = by.get(n, [])
        if not pts:
            out.append('  %-8s no RPT records' % n)
            continue
        fl = pts[-1]
        i = len(pts) - 1
        while i > 0 and abs(pts[i - 1][1] - fl[1]) < 1e-7 and abs(pts[i - 1][2] - fl[2]) < 1e-7:
            i -= 1
        seg = pts[max(0, i - 3):i + 1]
        cells = []
        for (t, lat, lon) in seg:
            e, nn = enu(lat, lon, fl[1], fl[2])
            cells.append('t=%.1f sim%+.0f d=%.1f (E%+.1f N%+.1f)' % (t, (t - order_t) * mult, math.hypot(e, nn), e, nn))
        sp = []
        for a, b in zip(seg, seg[1:]):
            ea, na = enu(a[1], a[2], fl[1], fl[2])
            eb, nb = enu(b[1], b[2], fl[1], fl[2])
            sp.append('%.2f' % (math.hypot(eb - ea, nb - na) / ((b[0] - a[0]) * mult)))
        out.append('  %-8s final %.6f %.6f | %s | m/sim-s: %s' % (n, fl[1], fl[2], ' ; '.join(cells), ','.join(sp)))
    return out


def main():
    root = sys.argv[1]
    for spec in sys.argv[2:]:
        parts = spec.split(':')
        run = parts[0]
        mult = float(parts[1]) if len(parts) > 1 else 1.0
        order_t = float(parts[2]) if len(parts) > 2 else 32.5
        base = '%s/runs/%s/' % (root, run)
        print('== %s (x%g, order at trace t=%.1f)' % (run, mult, order_t))
        order, ev, ns, nd = phase_events(base + 'bin64-vrfSim.log')
        print('  stamped lines %d, discarded (sim>=5000) %d, order sim stamp %s' % (ns, nd, order))
        for sim, off, key, who, wall in ev:
            print('   %8.3f %+8.3f %-10s %-16s %s' % (sim, off, key, who, wall))
        print('  RPT truth approach (AR Plt 3):')
        for line in rpt_approach(base + 'watchvrf-trace.csv', mult, order_t):
            print(line)


if __name__ == '__main__':
    main()
