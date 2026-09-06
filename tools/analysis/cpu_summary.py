"""Summarise a scripts/SampleCpu.ps1 CSV: machine and per-process CPU (cores' worth), by phase.

Usage: python tools/analysis/cpu_summary.py <cpu-samples.csv> [--order-tsec N] [--logical 32]

Phases: 'pre' = before --order-tsec (creation), 'post' = after it (tasked run). Without
--order-tsec the whole file is one phase. Reads with encoding='ascii' (the sampler writes ASCII).
Vendor context: UG52 6.1.1 - the sim engine's default configuration limits CPU use to its
configured thread counts (numCallbackThreads 4, nav 2, network 1 unless thread-safe RTI) - so a
sim pinned at ~5-6 cores' worth on a 32-logical-CPU machine is an ENGINE thread-budget ceiling,
not a saturated machine. The machine is 'overwhelmed' only if machineCpuPct is high.
"""
import argparse
import csv
import io
import statistics
import sys


def fnum(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return None


def summarise(rows, col):
    vals = [fnum(r.get(col)) for r in rows]
    vals = [v for v in vals if v is not None]
    if not vals:
        return None
    return dict(n=len(vals), mean=statistics.mean(vals), max=max(vals),
                p90=sorted(vals)[int(0.9 * (len(vals) - 1))])


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('csv')
    ap.add_argument('--order-tsec', type=float, default=None)
    ap.add_argument('--logical', type=int, default=32)
    a = ap.parse_args()
    with io.open(a.csv, encoding='ascii', errors='replace', newline='') as f:
        rows = list(csv.DictReader(f))
    if not rows:
        print('no samples')
        return 1
    cols = [c for c in rows[0] if c.endswith('_cores')]
    phases = {'all': rows}
    if a.order_tsec is not None:
        phases = {'pre-order': [r for r in rows if fnum(r['tSec']) is not None and fnum(r['tSec']) < a.order_tsec],
                  'post-order': [r for r in rows if fnum(r['tSec']) is not None and fnum(r['tSec']) >= a.order_tsec]}
    print(f'{a.csv}: {len(rows)} samples, tSec {rows[0]["tSec"]}..{rows[-1]["tSec"]}, logical CPUs {a.logical}')
    for name, rs in phases.items():
        if not rs:
            continue
        m = summarise(rs, 'machineCpuPct')
        print(f'\n[{name}] {len(rs)} samples')
        if m:
            print(f'  machine CPU %      mean {m["mean"]:5.1f}  p90 {m["p90"]:5.1f}  max {m["max"]:5.1f}   '
                  f'(100 = all {a.logical} logical CPUs busy)')
        for c in cols:
            s = summarise(rs, c)
            if not s or s['max'] == 0:
                continue
            thr = summarise(rs, c.replace('_cores', '_threads'))
            ws = summarise(rs, c.replace('_cores', '_wsMB'))
            print(f'  {c[:-6]:22} cores mean {s["mean"]:5.2f}  p90 {s["p90"]:5.2f}  max {s["max"]:5.2f}'
                  f'   threads max {int(thr["max"]) if thr else "-":>4}   WS max {int(ws["max"]) if ws else "-":>6} MB')
    return 0


if __name__ == '__main__':
    sys.exit(main())
