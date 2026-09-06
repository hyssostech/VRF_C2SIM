"""Per-tasked-unit displacement from a run's WatchVrf trace, joined through the app log.

Usage: python tools/analysis/taskee_displacement.py <run-dir> [--init data/X.xml] [--order data/Y.xml]

Join path (works with object consoles OFF, i.e. no CON rows and no 'console level requested'
lines): the app log's "Route '<task>' (<route uuid>) created; MoveAlongRoute issued for
<VRF_UUID>" lines give task name -> taskee VRF uuid; the order gives task name -> PerformingEntity
(C2SIM uuid); the init gives C2SIM uuid -> unit name. Displacement = haversine from the unit's
first valid POS row. Prints max and final displacement per tasked unit and the count of units
beyond 1 km (the movement gate used by the COA-STP1 prereg series).
All files are read with encoding='utf-8' (errors replaced).
"""
import argparse
import glob
import io
import math
import re
import sys


def hav(a, b):
    r = 6371000.0
    p1, p2 = math.radians(a[0]), math.radians(b[0])
    d = math.radians(b[0] - a[0])
    dl = math.radians(b[1] - a[1])
    h = math.sin(d / 2) ** 2 + math.cos(p1) * math.cos(p2) * math.sin(dl / 2) ** 2
    return 2 * r * math.asin(math.sqrt(h))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('run')
    ap.add_argument('--init', default='data/COA-STP1_Initialization.xml')
    ap.add_argument('--order', default='data/COA-STP1_Order.xml')
    ap.add_argument('--gate-m', type=float, default=1000.0)
    a = ap.parse_args()
    logs = [f for f in glob.glob(a.run + '/*.log') if 'app' in f.lower()]
    if not logs:
        print('no app log in', a.run)
        return 1
    log = io.open(logs[0], encoding='utf-8', errors='replace').read()
    issued = re.findall(r"Route '([^']+)' \([^)]+\) created; (?:MoveAlongRoute|R10 fan-out MoveAlongRoute) issued "
                        r"(?:for|to \d+ members of) (VRF_UUID:[0-9a-f-]+)", log)
    order = io.open(a.order, encoding='utf-8', errors='replace').read()
    init = io.open(a.init, encoding='utf-8', errors='replace').read()
    name = {}
    for u in re.findall(r'<Unit>(.*?)</Unit>', init, flags=re.S):
        n = re.search(r'<Name>([^<]+)<', u)
        uu = re.search(r'<UUID>([^<]+)<', u)
        if n and uu:
            name[uu.group(1).strip()] = n.group(1).strip()
    tname = {}
    for t in re.findall(r'<Task>(.*?)</Task>', order, flags=re.S):
        p = re.search(r'<PerformingEntity>([^<]+)<', t)
        n = re.search(r'<(?:TaskName|Name)>([^<]+)<', t)
        if p and n:
            tname[n.group(1).strip()] = p.group(1).strip()
    want = {u for _, u in issued}
    first, far, last, cnt = {}, {}, {}, {}
    for line in io.open(a.run + '/watchvrf-trace.csv', encoding='utf-8', errors='replace'):
        if not line.startswith('POS,'):
            continue
        p = line.rstrip('\n').split(',')
        if len(p) < 5 or p[2] not in want:
            continue
        try:
            t, la, lo = float(p[1]), float(p[3]), float(p[4])
        except ValueError:
            continue
        if abs(la) < 1e-6:
            continue
        u = p[2]
        cnt[u] = cnt.get(u, 0) + 1
        if u not in first:
            first[u] = (la, lo, t)
            far[u] = 0.0
        d = hav(first[u][:2], (la, lo))
        far[u] = max(far[u], d)
        last[u] = (d, t)
    print(f'{a.run}: {len(issued)} MoveAlongRoute issued; gate {a.gate_m:.0f} m')
    print(f'{"task":22} {"taskee":20} {"POS":>5} {"t0":>6} {"tLast":>6} {"maxDisp":>9} {"lastDisp":>9}')
    beyond = 0
    seen = set()
    for rn, u in issued:
        # the app names a route "<task name> ROUTE" (VrfC2SimService.ExecuteTaskOnTick)
        task_name = rn[:-6] if rn.endswith(' ROUTE') else rn
        tk = tname.get(task_name, '?')
        un = name.get(tk, tk)[:20]
        if u in first:
            print(f'{rn[:22]:22} {un:20} {cnt[u]:5} {first[u][2]:6.0f} {last[u][1]:6.0f} {far[u]:8.0f}m {last[u][0]:8.0f}m')
            if u not in seen and far[u] >= a.gate_m:
                beyond += 1
            seen.add(u)
        else:
            print(f'{rn[:22]:22} {un:20}   no POS rows for {u}')
    print(f'units beyond the gate: {beyond} of {len(seen)} tasked units with POS rows')
    return 0


if __name__ == '__main__':
    sys.exit(main())
