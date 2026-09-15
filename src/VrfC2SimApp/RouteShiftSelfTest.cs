using System.Globalization;
using VrfC2Sim;
using VrfC2SimApp.Preflight;

namespace VrfC2SimApp;

/// <summary>
/// OFFLINE check of the LATERAL ROUTE SHIFT (STP-804/806;
/// docs/experiments/DESIGN_ROUTE_SHIFT_2026-09-15.md sec 7):
/// `VrfC2SimApp --routeshift-selftest`.
///
/// It runs on the committed tile cache with Offline = true - a tile FETCH is a failure, asserted -
/// so every number below is the same terrain the calibration and the published lateral tables were
/// computed on, and no check can pass by reaching the network.
///
/// The legs are the record's own, and the expected answers were computed BEFORE this code existed,
/// with tools/preflight/leg_check.py's own sampler (the design note's sec 6 table). This suite is
/// the port agreeing with that pre-registration, not a fit to whatever the port happens to say.
/// </summary>
public static class RouteShiftSelfTest
{
    // The 1-35 ridge leg: the DeStack start every run places it at, to route vertex V1.
    // PREREG_RIDGE_AG_2026-09-14 sec 3.2; the leg that froze six runs.
    private static readonly (double Lat, double Lon) RidgeA = (34.658442, -116.740092);
    private static readonly (double Lat, double Lon) RidgeB = (34.651212159120796, -116.81163703922806);

    // 1-1/2/1_AD's T23 leg 1 - a clean mover that arrived, scored 0.870 (PREFLIGHT_CALIBRATION).
    private static readonly (double Lat, double Lon) ReconA = (34.662425, -116.746154);
    private static readonly (double Lat, double Lon) ReconB = (34.6508861485138, -116.81211493934713);

    // 1-35's AUTHORED V0 -> V1 line: the line N2d drove to the first completed leg in any run
    // (PREREG_N1_N2 sec 10.3). Scored here as an independent offline confirmation of that run.
    private static readonly (double Lat, double Lon) AuthoredV0 = (34.67998, -116.72480);

    // T4 / T26's PL BLUE leg: 42.9 km, flagged 0.988, and NOTHING in the band clears it.
    private static readonly (double Lat, double Lon) BlueA = (34.32941147391077, -116.50651992054665);
    private static readonly (double Lat, double Lon) BlueB = (34.3270745574951, -116.973606254397);

    private const double TankLimitRaw = 0.94;         // M1A2 max-slope, the vendor's own .entity

    public static int Run()
    {
        int failures = 0;
        string repo = FindRepoRoot();
        if (repo == null)
        {
            Console.Error.WriteLine("ROUTESHIFT SELFTEST: cannot locate the repo root (data/COA-STP1_Order.xml).");
            return 2;
        }
        string cache = Path.Combine(repo, "tools", "preflight", "preflight_cache");
        Console.WriteLine("=== ROUTE SHIFT (STP-804/806) - offline, on the committed tile cache ===");
        Console.WriteLine($"tiles     : {cache} (offline)");

        using var svc = new PreflightService(new PreflightOptions { CacheDir = cache, Offline = true });
        if (!Directory.Exists(cache) || Directory.GetFiles(cache).Length == 0)
        {
            Console.Error.WriteLine($"ROUTESHIFT SELFTEST: the tile cache {cache} is empty - nothing can be scored. " +
                                    "It is gitignored; copy it from a checkout that has it.");
            return 2;
        }
        var opt = new RouteShiftOptions();
        Console.WriteLine($"defaults  : band +/-{opt.MaxMeters:F0} m step {opt.StepMeters:F0} m, accept <= " +
                          $"{opt.AcceptRatio:F3} (threshold {opt.Threshold:F2} - margin {opt.MarginRatio:F2}), " +
                          $"formation band {(opt.ClearFormationBand ? "ON" : "off")}, pad {opt.PadMeters:F0} m, " +
                          $"lead {opt.LeadMeters:F0} m, corner {opt.MaxTurnDegrees:F0} deg");

        // ---------------------------------------------------------------- 1. GEOMETRY, pure
        Console.WriteLine("-- 1. geometry (no tiles)");
        {
            double brg = RouteShift.BearingDegrees(RidgeA, RidgeB);
            Check(ref failures, Math.Abs(brg - 263.02) < 0.05,
                  $"the ridge leg's bearing is 263.02 (got {brg:F2})");

            var right = RouteShift.OffsetLateral(RidgeA, brg, 100.0);
            var left = RouteShift.OffsetLateral(RidgeA, brg, -100.0);
            Check(ref failures, right.Lat > RidgeA.Lat && left.Lat < RidgeA.Lat,
                  $"+offset on a 263 deg leg goes NORTH, -offset SOUTH (got {right.Lat - RidgeA.Lat:+0.000000;-0.000000} / " +
                  $"{left.Lat - RidgeA.Lat:+0.000000;-0.000000} deg)");
            Check(ref failures, RouteShift.SideWordFor(brg, 75) == "north" && RouteShift.SideWordFor(brg, -75) == "south",
                  "the side word on this leg is north for +, south for -");
            double d = TileMath.DistanceMeters(RidgeA.Lat, RidgeA.Lon, right.Lat, right.Lon);
            Check(ref failures, Math.Abs(d - 100.0) < 0.5, $"a 100 m lateral offset measures 100 m (got {d:F2})");

            double legLen = TileMath.DistanceMeters(RidgeA.Lat, RidgeA.Lon, RidgeB.Lat, RidgeB.Lon);
            Check(ref failures, Math.Abs(legLen - 6593.3) < 1.0,
                  $"the ridge leg is 6,593.3 m (PREREG_RIDGE_AG 3.2; got {legLen:F1})");
            var mid = RouteShift.PointAlong(RidgeA, RidgeB, 2006.0);
            double dm = TileMath.DistanceMeters(RidgeA.Lat, RidgeA.Lon, mid.Lat, mid.Lon);
            Check(ref failures, Math.Abs(dm - 2006.0) < 1.0, $"PointAlong(2006) is 2,006 m along (got {dm:F1})");

            // The detour's shape: A -> Pin -> D1 -> D2 -> Pout -> B, Pin/Pout ON the authored line.
            var poly = RouteShift.BuildDetour(RidgeA, RidgeB, 1990.0, 2030.0, 75.0, opt, out string refusal);
            Check(ref failures, poly != null && refusal.Length == 0, $"a mid-leg window yields a detour ({refusal})");
            if (poly != null)
            {
                Check(ref failures, poly.Count == 6 && poly[0] == RidgeA && poly[5] == RidgeB,
                      "the detour is A -> Pin -> D1 -> D2 -> Pout -> B with BOTH authored endpoints unchanged");
                double transit = RouteShift.TransitMeters(75.0, opt);
                Check(ref failures, Math.Abs(transit - 75.0 / Math.Tan(30.0 * Math.PI / 180.0)) < 0.01,
                      $"the transit is |offset| / tan(turn) = {transit:F1} m for 75 m at 30 deg");

                // Pin and Pout are ON the authored line: their cross-track is zero.
                double xt = CrossTrackMeters(RidgeA, RidgeB, poly[1]);
                Check(ref failures, Math.Abs(xt) < 0.5,
                      $"Pin lies ON the authored line, so the approach is unchanged ground (cross-track {xt:F2} m)");
                Check(ref failures, Math.Abs(CrossTrackMeters(RidgeA, RidgeB, poly[4])) < 0.5,
                      "Pout lies ON the authored line too");
                Check(ref failures, Math.Abs(CrossTrackMeters(RidgeA, RidgeB, poly[2]) - 75.0) < 0.5
                                 && Math.Abs(CrossTrackMeters(RidgeA, RidgeB, poly[3]) - 75.0) < 0.5,
                      "D1 and D2 are BOTH at the full requested offset - the stretch across the window is parallel");

                double d1 = TileMath.DistanceMeters(RidgeA.Lat, RidgeA.Lon, poly[2].Lat, poly[2].Lon);
                double expected = Math.Sqrt(Math.Pow(1990.0 - opt.PadMeters - opt.LeadMeters, 2) + 75.0 * 75.0);
                Check(ref failures, Math.Abs(d1 - expected) < 2.0,
                      $"D1 sits at (window - pad - lead) laterally offset (got {d1:F1}, expected {expected:F1})");
                double pin = TileMath.DistanceMeters(RidgeA.Lat, RidgeA.Lon, poly[1].Lat, poly[1].Lon);
                Check(ref failures, Math.Abs(pin - (1990.0 - opt.PadMeters - opt.LeadMeters - transit)) < 2.0,
                      $"Pin sits one transit before D1 (got {pin:F1})");
            }

            // No room for the transit+lead -> REFUSED, never clamped onto the unit's own position.
            var none = RouteShift.BuildDetour(RidgeA, RidgeB, 40.0, 80.0, 75.0, opt, out string why1);
            Check(ref failures, none == null && why1.Contains("no room"),
                  $"a window in the first 160 m is REFUSED, not clamped ({why1})");
            // A big offset needs a long transit, and a window near the start has nowhere to put it.
            var steep = RouteShift.BuildDetour(RidgeA, RidgeB, 300.0, 340.0, 600.0, opt, out string why2);
            Check(ref failures, steep == null && why2.Contains("no room"),
                  $"a 600 m offset needs a {RouteShift.TransitMeters(600.0, opt):F0} m transit and is REFUSED where " +
                  $"there is no room for it ({why2})");
        }

        // ---------------------------------------------------------------- 2. THE REFERENCE LEG
        Console.WriteLine("-- 2. the 1-35 ridge leg (the leg that froze P11, G2, G3, G5)");
        var limit = svc.LimitFor("Tank Headquarters Section (USA)", hostile: false);
        Check(ref failures, Math.Abs(limit.LimitRaw - TankLimitRaw) < 1e-9,
              $"the unit's own limit resolves to max-slope {TankLimitRaw:F2} (got {limit.LimitRaw:F2}; {limit.Note})");

        var ridgeBase = svc.ScoreLeg(RidgeA, RidgeB, TankLimitRaw);
        Check(ref failures, Math.Abs(ridgeBase.Ratio - 1.098) < 0.003,
              $"the authored line scores 1.098 (PREFLIGHT_CALIBRATION; got {ridgeBase.Ratio:F3})");
        Check(ref failures, ridgeBase.Flagged, "the authored line is FLAGGED");
        Check(ref failures, Math.Abs(ridgeBase.WorstSM - 2006.0) < 10.0,
              $"its worst window centres at s = 2,006 m (PREREG_RIDGE_AG 3.2; got {ridgeBase.WorstSM:F1})");
        Check(ref failures, Math.Abs(ridgeBase.Limit - 0.752) < 0.002 && ridgeBase.Soil == "sand",
              $"the window is on sand, limit 0.752 (got {ridgeBase.Soil} {ridgeBase.Limit:F3})");

        var ridgeShift = RouteShift.ChooseForLeg(RidgeA, RidgeB, ridgeBase, opt, svc.WorstRatioScorer(TankLimitRaw));
        Console.WriteLine($"     chosen: {ridgeShift.Note}");
        Check(ref failures, ridgeShift.Shifted, "a shift IS chosen for the ridge leg");
        Check(ref failures, ridgeShift.OffsetMeters > 0 && ridgeShift.SideWord == "north",
              $"the chosen side is NORTH - the cleared side (PREREG_RIDGE_AG 3.3; got {ridgeShift.OffsetMeters:+0;-0} m {ridgeShift.SideWord})");
        Check(ref failures, Math.Abs(ridgeShift.OffsetMeters) >= 50.0 && Math.Abs(ridgeShift.OffsetMeters) <= 550.0,
              $"the chosen offset is inside the measured clear band +50..+550 m (got {Math.Abs(ridgeShift.OffsetMeters):F0})");
        Check(ref failures, ridgeShift.ShiftedRatio < opt.Threshold,
              $"the shifted polyline is below the threshold (got {ridgeShift.ShiftedRatio:F3} vs {opt.Threshold:F2})");
        Check(ref failures, ridgeShift.ShiftedRatio <= opt.AcceptRatio,
              $"C1: it clears by the full calibration margin (got {ridgeShift.ShiftedRatio:F3} vs {opt.AcceptRatio:F3})");
        Check(ref failures, ridgeShift.BandMax < opt.Threshold,
              $"C2: every formation slot line clears the threshold (band max {ridgeShift.BandMax:F3})");
        Check(ref failures, Math.Abs(ridgeShift.OffsetMeters - 75.0) < 1e-9,
              $"the shipped defaults pick +75 m (the design note's pre-registered answer; got {ridgeShift.OffsetMeters:F0})");

        // The brief's own rule - the route line alone - is what ClearFormationBand=false reproduces.
        var lineOnly = opt with { ClearFormationBand = false };
        var ridgeLineOnly = RouteShift.ChooseForLeg(RidgeA, RidgeB, ridgeBase, lineOnly, svc.WorstRatioScorer(TankLimitRaw));
        Console.WriteLine($"     line-only: {ridgeLineOnly.Note}");
        Check(ref failures, ridgeLineOnly.Shifted && Math.Abs(ridgeLineOnly.OffsetMeters - 50.0) < 1e-9,
              $"with the formation band OFF the rule picks +50 m (got {ridgeLineOnly.OffsetMeters:F0})");
        Check(ref failures, Math.Abs(ridgeLineOnly.ShiftedRatio - 0.728) < 0.005,
              $"...scoring 0.728 (the pre-registered number; got {ridgeLineOnly.ShiftedRatio:F3})");
        Check(ref failures, Math.Abs(ridgeShift.OffsetMeters) > Math.Abs(ridgeLineOnly.OffsetMeters),
              "C2 costs distance rather than saving it - it can over-shift, never under-shift");

        // ---------------------------------------------------------------- 3. THE SEARCH ORDER
        Console.WriteLine("-- 3. the search: smallest magnitude, on the side that clears");
        {
            var tried = ridgeShift.Tried;
            foreach (var c in tried)
            {
                string verdict = c.Accepted ? "ACCEPTED" : c.Refusal;
                Console.WriteLine(FormattableString.Invariant(
                    $"     tried {c.OffsetMeters,5:0} m: ratio {c.Ratio,6:F3} band {c.BandMax,6:F3} {verdict}"));
            }
            Check(ref failures, tried.Count >= 4, $"both sides were tried at each magnitude ({tried.Count} candidates)");
            double win = Math.Abs(ridgeShift.OffsetMeters);
            Check(ref failures, tried.Where(c => Math.Abs(c.OffsetMeters) < win).All(c => !c.Accepted),
                  "no SMALLER magnitude was acceptable - the chosen shift is the smallest that clears");
            var south = tried.FirstOrDefault(c => Math.Abs(c.OffsetMeters + 50.0) < 1e-9);
            var north = tried.FirstOrDefault(c => Math.Abs(c.OffsetMeters - 50.0) < 1e-9);
            Check(ref failures, south != null && north != null && south.Ratio > north.Ratio,
                  $"the face's asymmetry is FOUND, not assumed: -50 m scores {south?.Ratio ?? double.NaN:F3} vs " +
                  $"+50 m {north?.Ratio ?? double.NaN:F3}");
            Check(ref failures, south != null && south.Ratio > opt.Threshold,
                  "a southward shift would be WORSE than the authored line and is rejected");
        }

        // ---------------------------------------------------------------- 4. UNTOUCHED CONTROLS
        Console.WriteLine("-- 4. legs the shift must not touch");
        foreach (var (label, a, b, expected) in new[]
        {
            ("1-1/2/1_AD T23 leg 1 (a clean mover that arrived)", ReconA, ReconB, 0.870),
            ("1-35's AUTHORED V0->V1 (the line N2d drove)", AuthoredV0, RidgeB, 0.524),
        })
        {
            var route = new List<(double Lat, double Lon)> { a, b };
            var outcome = svc.ShiftRoute(route, TankLimitRaw, opt);
            var leg = outcome.Legs[0];
            Check(ref failures, Math.Abs(leg.Ratio - expected) < 0.005,
                  $"{label} scores {expected:F3} (got {leg.Ratio:F3})");
            Check(ref failures, !leg.Flagged, $"{label} is NOT flagged");
            Check(ref failures, outcome.Shifts.Count == 0 && !outcome.Changed,
                  $"{label}: nothing is even considered");
            Check(ref failures, outcome.Route.Count == 2 && outcome.Route[0] == a && outcome.Route[1] == b,
                  $"{label}: the vertex list comes back IDENTICAL, point for point");
        }

        // ---------------------------------------------------------------- 5. REPORT-ONLY CASE
        Console.WriteLine("-- 5. a flagged leg no offset can clear (T4/T26, PL BLUE, 42.9 km)");
        var blueRoute = new List<(double Lat, double Lon)> { BlueA, BlueB };
        var blue = svc.ShiftRoute(blueRoute, TankLimitRaw, opt);
        {
            var leg = blue.Legs[0];
            Check(ref failures, leg.Flagged && Math.Abs(leg.Ratio - 0.988) < 0.005,
                  $"the PL BLUE leg is flagged at 0.988 (got {leg.Ratio:F3})");
            Check(ref failures, blue.Shifts.Count == 1 && !blue.Shifts[0].Shifted,
                  "it is looked at and DECLINED");
            Check(ref failures, blue.Shifts[0].Note.StartsWith("NO CLEARED LINE"),
                  $"the reason names the band searched ({blue.Shifts[0].Note})");
            Check(ref failures, !blue.Changed && blue.Route.Count == 2
                  && blue.Route[0] == BlueA && blue.Route[1] == BlueB,
                  "the task keeps the line exactly as authored - nothing is invented");
        }

        // ---------------------------------------------------------------- 6. SPLICING
        Console.WriteLine("-- 6. splicing the waypoints into the route");
        {
            var route = new List<(double Lat, double Lon)> { RidgeA, RidgeB, (34.596350, -116.952329) };
            var outcome = svc.ShiftRoute(route, TankLimitRaw, opt);
            Check(ref failures, outcome.Legs.Count == 2 && outcome.Legs[0].Flagged && !outcome.Legs[1].Flagged,
                  $"leg 1 flags, leg 2 does not ({string.Join(", ", outcome.Legs.Select(l => l.Ratio.ToString("F3", CultureInfo.InvariantCulture)))})");
            Check(ref failures, outcome.Route.Count == 7, $"one shifted leg adds exactly 4 vertices (got {outcome.Route.Count})");
            Check(ref failures, outcome.Route[0] == route[0] && outcome.Route[5] == route[1] && outcome.Route[6] == route[2],
                  "every authored vertex survives, in order, unmoved");
            var geo = VrfC2SimService.SpliceShift(
                route.Select(p => new Geodetic { LatDeg = p.Lat, LonDeg = p.Lon, AltMeters = 1234.5 }).ToList(),
                outcome.Shifts);
            Check(ref failures, geo.Count == 7 && geo.All(g => Math.Abs(g.AltMeters - 1234.5) < 1e-9),
                  "the Geodetic splice inherits the preceding vertex's altitude - no invented height");
            Check(ref failures, Math.Abs(geo[1].LatDeg - outcome.Route[1].Lat) < 1e-12
                             && Math.Abs(geo[2].LonDeg - outcome.Route[2].Lon) < 1e-12,
                  "the service splice and the pure splice agree point for point");
        }

        // ---------------------------------------------------------------- 7. THE REPORTS
        Console.WriteLine("-- 7. what the C2 side is told");
        {
            var route = new List<(double Lat, double Lon)> { RidgeA, RidgeB };
            var outcome = svc.ShiftRoute(route, TankLimitRaw, opt);
            int n = 0;
            var reports = PreflightReports.BuildForShift("uuid-1", "1-35/2/1_A", "T1_AOA_SE", outcome.Shifts,
                                                        outcome.Legs, "2026-09-15T00:00:00Z",
                                                        () => "rpt-" + (++n).ToString(CultureInfo.InvariantCulture));
            Check(ref failures, reports.Count == 1, $"one report per flagged leg (got {reports.Count})");
            string marking = PreflightReports.ShiftMarking("T1_AOA_SE", "1-35/2/1_A", outcome.Shifts[0]);
            Check(ref failures, marking.Contains("ROUTE SHIFT") && marking.Contains("1.098")
                             && marking.Contains("north") && marking.Contains("vertices are unchanged"),
                  $"the Marking carries the leg, both ratios, the side and the promise: {marking}");
            Check(ref failures, reports[0].Contains("<") && reports[0].Contains("1-35/2/1_A"),
                  "the report serializes through the SDK's schema types");

            string noneMark = PreflightReports.NoShiftMarking("T4_PL_BLUE", "1-35/2/1_A", blue.Shifts[0]);
            Check(ref failures, noneMark.Contains("NOT APPLIED") && noneMark.Contains("as authored"),
                  $"a declined leg says so, and says the task still goes: {noneMark}");

            var clean = svc.ShiftRoute(new List<(double Lat, double Lon)> { ReconA, ReconB }, TankLimitRaw, opt);
            var noReports = PreflightReports.BuildForShift("uuid-1", "1-1/2/1_AD", "T23", clean.Shifts, clean.Legs,
                                                          "2026-09-15T00:00:00Z", () => "rpt");
            Check(ref failures, noReports.Count == 0, "an untouched leg says NOTHING at all");
        }

        // ---------------------------------------------------------------- 8. THE DOUBLE-DISPATCH LATCH
        Console.WriteLine("-- 8. the one-shot claim (the worker vs the timeout sweep)");
        {
            var claim = new OneShotClaim();
            Check(ref failures, !claim.Claimed, "a fresh claim is unclaimed");
            Check(ref failures, claim.Claim() && !claim.Claim() && !claim.Claim(),
                  "the first caller wins and every later one loses");

            // The race itself, not a reading of it: many threads, one winner, every time.
            bool everWrong = false;
            for (int trial = 0; trial < 200 && !everWrong; trial++)
            {
                var c = new OneShotClaim();
                int wins = 0;
                var gate = new ManualResetEventSlim(false);
                var threads = new List<Thread>();
                for (int t = 0; t < 8; t++)
                {
                    var th = new Thread(() => { gate.Wait(); if (c.Claim()) Interlocked.Increment(ref wins); });
                    th.Start();
                    threads.Add(th);
                }
                gate.Set();
                foreach (var th in threads) th.Join();
                if (wins != 1) everWrong = true;
            }
            Check(ref failures, !everWrong, "8 threads x 200 races: EXACTLY ONE winner every time");
        }

        Check(ref failures, svc.Tiles.Fetched == 0,
              $"NOTHING was fetched over the network ({svc.Tiles.Fetched} fetches, {svc.Tiles.CacheHits} cache hits)");

        Console.WriteLine(failures == 0 ? "routeshift-selftest: PASS" : $"routeshift-selftest: {failures} FAILURE(S)");
        return failures;
    }

    /// <summary>
    /// Signed metres RIGHT of the straight line a-&gt;b, measured INDEPENDENTLY of the code under
    /// test: the direction comes from the two endpoints in a local north-east plane, not from
    /// RouteShift's bearing, so a wrong bearing cannot cancel itself out of the answer.
    /// </summary>
    private static double CrossTrackMeters((double Lat, double Lon) a, (double Lat, double Lon) b,
                                           (double Lat, double Lon) p)
    {
        double k = RouteShift.MetresPerDegree * Math.Cos(a.Lat * Math.PI / 180.0);
        double hN = (b.Lat - a.Lat) * RouteShift.MetresPerDegree, hE = (b.Lon - a.Lon) * k;
        double h = Math.Sqrt(hN * hN + hE * hE);
        if (h <= 0) return 0.0;
        hN /= h; hE /= h;
        double dN = (p.Lat - a.Lat) * RouteShift.MetresPerDegree, dE = (p.Lon - a.Lon) * k;
        return dE * hN - dN * hE;          // the right-hand normal of (hN, hE) is (-hE, hN)
    }

    private static void Check(ref int failures, bool ok, string what)
    {
        Console.WriteLine($"  [{(ok ? "ok" : "FAIL")}] {what}");
        if (!ok) failures++;
    }

    /// <summary>Walk up from the executable until data/COA-STP1_Order.xml appears.</summary>
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "data", "COA-STP1_Order.xml"))) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
