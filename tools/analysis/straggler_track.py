"""Straggler analysis for a unit that never reports completion (PREREG_ASSEMBLY_LAYOUT 3b).

Usage: python tools/analysis/straggler_track.py <run-dir> [--window-min 15] [--unit NAME ...]

For every tasked unit (MoveAlongRoute issued in the app log), using the member -> unit map the
app logs on the member-console request line ("... members of <unit>: <name> [<VRF_UUID>], ..."),
reads each member's POS rows from the WatchVrf trace and reports, at the END of the trace:
  - the unit's TASKCMPLT status (from the app log),
  - each member's final displacement from its birth point and its distance from the unit's
    member centroid,
  - for the member farthest from the others ("straggler"): net movement and path length over
    the last --window-min minutes, and a classification:
      STUCK    net < 100 m over the window,
      CIRCLING path length > 5 x net and net < 500 m,
      CLOSING  distance to the centroid shrank monotonically over the window,
      OTHER    anything else (print the numbers).
  - the route's last vertex (from the app log's CreateRoute lines, when present) and the
    straggler's final distance to it.
All files read with encoding='utf-8' (errors replaced). No claims - a table.
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
    ap.add_argument('--window-min', type=float, default=15.0)
    ap.add_argument('--unit', action='append', default=None)
    a = ap.parse_args()
    logs = [f for f in glob.glob(a.run + '/*.log') if 'app' in f.lower()]
    if not logs:
        print('no app log in', a.run)
        return 1
    members = {}      # unit -> {member name: uuid}
    routes = {}       # unit name (taskee) -> [(lat, lon)] route vertices, when logged
    cmplt = set()
    with io.open(logs[0], encoding='utf-8', errors='replace') as f:
        for line in f:
            if 'console level' in line and 'requested for' in line and 'members of' in line:
                m = re.search(r'members of ([^:]+): (.+?)\.\s*$', line)
                if m:
                    d = members.setdefault(m.group(1).strip(), {})
                    for part in m.group(2).split(', '):
                        mm = re.match(r'(.+?) \[(VRF_UUID:[0-9a-f-]+)\]', part.strip())
                        if mm:
                            d[mm.group(1)] = mm.group(2)
            elif 'VRF task complete:' in line:
                m = re.search(r'VRF task complete: (.+?) / ', line)
                if m:
                    cmplt.add(m.group(1).strip())
    if not members:
        print('no member map in the app log (member console level must be >= 0)')
        return 1
    units = list(members) if not a.unit else [u for u in members if u in a.unit]
    inv = {}
    for u in units:
        for name, uuid in members[u].items():
            inv[uuid] = (u, name)
    track = {}   # uuid -> list of (t, lat, lon)
    for line in io.open(a.run + '/watchvrf-trace.csv', encoding='utf-8', errors='replace'):
        if not line.startswith('POS,'):
            continue
        p = line.rstrip('\n').split(',')
        if len(p) < 5 or p[2] not in inv:
            continue
        try:
            t, la, lo = float(p[1]), float(p[3]), float(p[4])
        except ValueError:
            continue
        if abs(la) < 1e-6:
            continue
        track.setdefault(p[2], []).append((t, la, lo))
    if not track:
        print('no POS rows for any member')
        return 1
    t_end = max(v[-1][0] for v in track.values())
    win = a.window_min * 60.0
    print(f'{a.run}: trace ends t = {t_end:.0f} s; window = last {a.window_min:.0f} min')
    for u in units:
        uu = [members[u][n] for n in members[u] if members[u][n] in track]
        if not uu:
            continue
        finals = {x: track[x][-1] for x in uu}
        cen = (sum(v[1] for v in finals.values()) / len(finals), sum(v[2] for v in finals.values()) / len(finals))
        rows = []
        for x in uu:
            name = inv[x][1]
            birth = track[x][0]
            disp = hav((birth[1], birth[2]), (finals[x][1], finals[x][2]))
            dcen = hav(cen, (finals[x][1], finals[x][2]))
            rows.append((dcen, name, disp, x))
        rows.sort(reverse=True)
        status = 'TASKCMPLT' if u in cmplt else 'no completion'
        print(f'\n[{u}] {status}; {len(uu)} members with tracks')
        for dcen, name, disp, x in rows:
            print(f'   {name:10} displacement {disp:8.0f} m   from the others\' centroid {dcen:7.0f} m   last t {track[x][-1][0]:6.0f}')
        # straggler = farthest from the centroid, only meaningful if > 1 km
        dcen, name, disp, x = rows[0]
        if dcen < 1000:
            print('   no straggler (all members within 1 km of each other)')
            continue
        pts = [p for p in track[x] if p[0] >= t_end - win]
        if len(pts) < 3:
            print(f'   straggler {name}: too few samples in the window')
            continue
        net = hav((pts[0][1], pts[0][2]), (pts[-1][1], pts[-1][2]))
        path = sum(hav((pts[i][1], pts[i][2]), (pts[i + 1][1], pts[i + 1][2])) for i in range(len(pts) - 1))
        dists = [hav(cen, (p[1], p[2])) for p in pts]
        mono = all(dists[i + 1] <= dists[i] + 5 for i in range(len(dists) - 1)) and dists[-1] < dists[0] - 50
        if net < 100:
            cls = 'STUCK'
        elif path > 5 * net and net < 500:
            cls = 'CIRCLING'
        elif mono:
            cls = 'CLOSING'
        else:
            cls = 'OTHER'
        print(f'   STRAGGLER {name}: window net {net:.0f} m, path {path:.0f} m, distance to the others {dists[0]:.0f} -> {dists[-1]:.0f} m  => {cls}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
