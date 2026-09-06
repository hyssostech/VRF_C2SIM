"""sim_ratio.py - sim-time / wall-time ratio of a 5.2 run from the object-console rows.

frame_gaps.py reads the 5.0.2 vendor log's wall stamps; the 5.2d vendor log carries none
(REBASELINE_52_INSTRUMENTS sec 2, sec 6). On 5.2 the sim narrates its own clock on each object's
console: level-3/4 lines start with the SIM time in seconds ("100.199 Task 0 starting subtask
...", "180.732 Disagg mv into form: ..."), and WatchVrf stamps every CON row with WALL seconds
since the observer joined. Pairing them gives (wall, sim) samples; the least-squares slope is
the ratio (FFRTC: > 1 = faster than real time).

usage: python sim_ratio.py <run_dir> [--min-samples N]
Needs Vrf:ObjectConsoleNotifyLevel >= 3 on at least one of our objects during the run.
"""
import csv
import html
import re
import sys
import os

XML_STRING = re.compile(r'<string[^>]*>(.*?)</string>', re.S)
NESTED_TAG = re.compile(r'^<string[^>]*>')
SIM_PREFIX = re.compile(r'^\s*(\d{1,7}\.\d{1,3})\s+\S')


def decode(msg):
    s = msg.replace('\\n', '\n')
    parts = [NESTED_TAG.sub('', html.unescape(p).strip()).strip() for p in XML_STRING.findall(s) if p.strip()]
    parts = [p for p in parts if p]
    return ' | '.join(parts) if parts else s.strip()


def samples(run_dir):
    out = []
    with open(os.path.join(run_dir, 'watchvrf-trace.csv'), encoding='utf-8', errors='replace', newline='') as f:
        for r in csv.reader(f):
            if not r or r[0] != 'CON' or len(r) < 5:
                continue
            try:
                wall = float(r[1])
            except ValueError:
                continue
            m = SIM_PREFIX.match(decode(','.join(r[4:])))
            if m:
                out.append((wall, float(m.group(1))))
    return out


def slope(pts):
    n = len(pts)
    mx = sum(p[0] for p in pts) / n
    my = sum(p[1] for p in pts) / n
    sxx = sum((p[0] - mx) ** 2 for p in pts)
    sxy = sum((p[0] - mx) * (p[1] - my) for p in pts)
    b = sxy / sxx if sxx > 0 else float('nan')
    a = my - b * mx
    resid = [p[1] - (a + b * p[0]) for p in pts]
    sd = (sum(e * e for e in resid) / max(1, n - 2)) ** 0.5
    return b, a, sd


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    run_dir = sys.argv[1]
    min_n = int(sys.argv[sys.argv.index('--min-samples') + 1]) if '--min-samples' in sys.argv else 10
    pts = samples(run_dir)
    # de-duplicate identical wall stamps (many rows share a tick) by keeping the first sim value
    seen = {}
    for w, s in pts:
        seen.setdefault(w, s)
    pts = sorted(seen.items())
    if len(pts) < min_n:
        print(f'{run_dir}: only {len(pts)} (wall, sim) samples - need >= {min_n}; is the console level >= 3 on any object?')
        return 1
    b, a, sd = slope(pts)
    w0, s0 = pts[0]
    w1, s1 = pts[-1]
    print(f'{run_dir}: {len(pts)} samples, wall {w0:.1f}..{w1:.1f} s, sim {s0:.1f}..{s1:.1f} s')
    print(f'  ratio (LS slope sim/wall) = {b:.3f}x   endpoints = {((s1 - s0) / (w1 - w0)) if w1 > w0 else float("nan"):.3f}x   resid sd = {sd:.2f} s')
    return 0


if __name__ == '__main__':
    sys.exit(main())
