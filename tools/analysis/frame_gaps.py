#!/usr/bin/env python
"""
frame_gaps.py - OFFLINE frame-quantum test on a runner run's vendor log.

Answers ONE question: is the back end's exercise clock advancing on a FIXED
grid of q sim-seconds per frame, or on a variable (jittered) frame?

Reads only runs/<run_id>/bin64-vrfSim.log. Never touches VR-Forces, the RTI,
or C:\\MAK. Reuses step_profile.vendor_log() so both instruments parse the
vendor stamps with exactly one piece of code.

WHY A PLAIN "MEDIAN GAP" TEST DOES NOT WORK. The vendor prints sim time to
3 decimals. A true fixed grid of q = 0.033333 s does NOT print as a constant
0.033: consecutive grid points round to 0.033, 0.033, 0.034, 0.033, 0.033,
0.034 ... (three frames = exactly 0.100 s). So a fixed frame and a variable
frame have the SAME median. What separates them is the SPREAD:

  TEST A (gap census, n = the sub-0.06 s gaps between consecutive distinct
  stamps). On a fixed grid of q, a one-frame gap can print ONLY as
  floor(q,3) or ceil(q,3) - for q = 0.033333 that is exactly {0.033, 0.034},
  nothing else, ever. Two frames (0.067) is already above the 0.06 filter.
  Variable frame scatters outside that pair.

  TEST B (grid residual, n = the distinct stamps themselves, ~2.5x more
  samples than Test A and independent of which frames happened to be logged).
  On a fixed grid every stamp satisfies t = k*q + phase for integer k, so the
  residual of t about the grid is bounded by the 0.0005 print rounding. The
  phase is FITTED (circular mean of t mod q) rather than assumed zero, so an
  exercise clock that did not start at 0 still passes. Uniformly scattered
  stamps give residuals uniform over [-q/2, q/2) and a resultant length R
  near 0; a perfect grid gives R near 1.

Usage:
  python tools/analysis/frame_gaps.py <repo_root> <run_id> [--q 0.033333]
"""
import argparse
import importlib.util
import math
import os
import statistics
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location(
    "step_profile", os.path.join(_HERE, "step_profile.py"))
_sp = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_sp)
vendor_log = _sp.vendor_log
STAMP_RE = _sp.STAMP_RE

PRINT_HALF = 0.0005          # half of the vendor's 0.001 s print quantum


def distinct_stamps(path):
    """Sorted distinct sim stamp values (the same parse step_profile uses)."""
    sims = set()
    with open(path, encoding='utf-8', errors='replace') as fh:
        for line in fh:
            for _h, _m, _s, t in STAMP_RE.findall(line):
                t = float(t)
                if t < 5000:
                    sims.add(t)
    return sorted(sims)


def grid_residuals(sims, q):
    """Fitted-phase residuals of every stamp about a grid of step q.

    Returns (phase, residuals, resultant_length R). R = 1 is a perfect grid,
    R = 0 is uniform scatter.
    """
    ang = [2.0 * math.pi * (t % q) / q for t in sims]
    cs = sum(math.cos(a) for a in ang) / len(ang)
    sn = sum(math.sin(a) for a in ang) / len(ang)
    R = math.hypot(cs, sn)
    phase = (math.atan2(sn, cs) % (2.0 * math.pi)) / (2.0 * math.pi) * q
    res = []
    for t in sims:
        d = (t - phase) % q
        if d > q / 2.0:
            d -= q
        res.append(d)
    return phase, res, R


def main():
    ap = argparse.ArgumentParser(description="frame-quantum test on a run's vendor log")
    ap.add_argument("root")
    ap.add_argument("run_id")
    ap.add_argument("--q", type=float, default=0.033333,
                    help="candidate fixed frame time in sim-s (default 0.033333)")
    ap.add_argument("--gap-max", type=float, default=0.06,
                    help="upper bound of a one-frame gap (default 0.06)")
    a = ap.parse_args()

    log = os.path.join(a.root, 'runs', a.run_id, 'bin64-vrfSim.log')
    if not os.path.exists(log):
        raise SystemExit("no such log: %s" % log)

    v = vendor_log(log)
    sims = distinct_stamps(log)
    q = a.q
    lo = math.floor(q * 1000.0) / 1000.0
    hi = math.ceil(q * 1000.0) / 1000.0
    band = sorted({round(lo, 3), round(hi, 3)})

    print("RUN %s   q = %.6f sim-s" % (a.run_id, q))
    print("  parse: lines %d, stamped %d, discarded %d, distinct sim stamps %d"
          % (v['lines'], v['stamped'], v['discarded'], v['distinct']))
    print("  LS clock slope %.4f sim-s per wall-s (resid sd %.2f, max %.2f)"
          % (v['ls_slope'], v['ls_resid_sd'], v['ls_resid_max']))

    gaps = v['tick_proxy']
    gaps = [g for g in gaps if g < a.gap_max]
    print("  TEST A - gap census, n=%d one-frame gaps (< %.2f s)" % (len(gaps), a.gap_max))
    if gaps:
        print("    min %.3f  median %.3f  max %.3f  sd %.4f"
              % (min(gaps), statistics.median(gaps), max(gaps),
                 statistics.pstdev(gaps) if len(gaps) > 1 else 0.0))
        cens = {}
        for g in gaps:
            k = round(g, 3)
            cens[k] = cens.get(k, 0) + 1
        print("    census: " + "  ".join("%.3f x%d" % (k, cens[k]) for k in sorted(cens)))
        inband = sum(n for k, n in cens.items() if round(k, 3) in band)
        print("    in the only two values a %.6f grid can print %s: %d/%d = %.1f%%"
              % (q, band, inband, len(gaps), 100.0 * inband / len(gaps)))
        tight = sum(1 for g in gaps if abs(g - q) <= PRINT_HALF)
        print("    (for reference only, NOT the test - within +/-%.4f of q exactly: "
              "%d/%d = %.1f%%; a true grid cannot exceed ~67%% here because of print "
              "rounding)" % (PRINT_HALF, tight, len(gaps), 100.0 * tight / len(gaps)))

    phase, res, R = grid_residuals(sims, q)
    on = sum(1 for d in res if abs(d) <= PRINT_HALF)
    print("  TEST B - grid residual, n=%d distinct stamps, fitted phase %.6f s" % (len(sims), phase))
    print("    resultant length R = %.4f   (1 = perfect grid, 0 = uniform scatter)" % R)
    print("    |residual| <= %.4f s: %d/%d = %.1f%%"
          % (PRINT_HALF, on, len(sims), 100.0 * on / len(sims)))
    print("    residual sd %.5f s (uniform scatter on this grid would be %.5f)"
          % (statistics.pstdev(res), q / math.sqrt(12.0)))
    return 0


if __name__ == '__main__':
    sys.exit(main())
