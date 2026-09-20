using System.Globalization;
using Microsoft.Extensions.Configuration;
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
    // NOTE WHOSE POSITION THIS IS. tools/preflight/leg_check.py's starts_from_run() takes the
    // FORMATION LEADER's first fix, so the calibration, the published lateral tables and this suite
    // are all anchored on M1A2 1. The LIVE interface anchors the route on the UNIT object's own
    // position, which in run 20260915T023743Z was 26.7 m away - see RidgeALive.
    private static readonly (double Lat, double Lon) RidgeA = (34.658442, -116.740092);
    private static readonly (double Lat, double Lon) RidgeB = (34.651212159120796, -116.81163703922806);

    // THE ANCHOR THE FIRST LIVE RUN ACTUALLY SCORED (run 20260915T023743Z): 1-35/2/1_A~PXY's own
    // first PositionReport, 02:40:16.802Z, 26.7 m south of RidgeA. On this line the authored leg
    // scores 1.248 rather than 1.098 and the good northern corridor sits one step further out - which
    // is what let the OLD single-phase chooser walk past it onto a SOUTHWARD shift. It is a fixture
    // here for exactly that reason: the regression this suite must never let back in.
    private static readonly (double Lat, double Lon) RidgeALive = (34.65820208652259, -116.74009186651882);

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

        // ------------------------------------------------- 0. THE DEFAULT (no tiles, no network)
        // Deliberately FIRST and before the tile-cache gate: the shipped default is now a user
        // ruling, and a checkout with no tile cache must still fail loudly if someone flips it.
        Defaults(ref failures, repo);

        using var svc = new PreflightService(new PreflightOptions { CacheDir = cache, Offline = true });
        if (!Directory.Exists(cache) || Directory.GetFiles(cache).Length == 0)
        {
            Console.Error.WriteLine($"ROUTESHIFT SELFTEST: the tile cache {cache} is empty - nothing can be scored. " +
                                    "It is gitignored; copy it from a checkout that has it.");
            // A real failure in section 0 outranks "could not run": 2 must never hide a FAIL.
            return failures > 0 ? failures : 2;
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

        // ------------------------------------------------- 3b. THE V8 LIVE ANCHOR (the regression)
        // Run 20260915T023743Z, the feature's first live run and the reason for the two-phase
        // chooser: 26.7 m of anchor moved the whole lateral profile, C2 refused +75 m NORTH on its
        // inner slot line, and the old search walked on to -125 m SOUTH - the side six runs froze on
        // and the side the pre-registration named as a STOP. These numbers were computed on
        // leg_check.py's own sampler against the run's own log BEFORE the fix was written.
        Console.WriteLine("-- 3b. the V8 live anchor: C1 picks the side, C2 may only size the shift");
        {
            var liveBase = svc.ScoreLeg(RidgeALive, RidgeB, TankLimitRaw);
            Check(ref failures, Math.Abs(liveBase.Ratio - 1.248) < 0.005,
                  $"the line the live run scored is 1.248, not the calibration's 1.098 (got {liveBase.Ratio:F3}) " +
                  "- the pre-flight anchors on the UNIT, the calibration on its formation LEADER");
            Check(ref failures, liveBase.Flagged, "the live line is FLAGGED");

            var live = RouteShift.ChooseForLeg(RidgeALive, RidgeB, liveBase, opt,
                                               svc.WorstRatioScorer(TankLimitRaw));
            Console.WriteLine($"     chosen: {live.Note}");
            Check(ref failures, live.Shifted, "a shift IS chosen from the live anchor");
            Check(ref failures, live.OffsetMeters > 0 && live.SideWord == "north",
                  $"THE REGRESSION: the side is NORTH, never the south side the run took " +
                  $"(got {live.OffsetMeters:+0;-0} m {live.SideWord})");
            Check(ref failures, Math.Abs(live.OffsetMeters + 125.0) > 1e-9,
                  $"the -125 m SOUTH shift of run 20260915T023743Z is NOT chosen (got {live.OffsetMeters:+0;-0} m)");
            Check(ref failures, Math.Abs(live.OffsetMeters) >= 50.0 && Math.Abs(live.OffsetMeters) <= 550.0,
                  $"it stays inside the measured clear band +50..+550 m (got {Math.Abs(live.OffsetMeters):F0})");
            Check(ref failures, live.ShiftedRatio <= opt.AcceptRatio,
                  $"C1 still holds by the full margin (got {live.ShiftedRatio:F3} vs {opt.AcceptRatio:F3})");
            Check(ref failures, !live.BandNotCleared && live.BandMax < opt.Threshold,
                  $"C2 still holds on the chosen side (band max {live.BandMax:F3})");
            Check(ref failures, Math.Abs(live.OffsetMeters - 250.0) < 1e-9
                             && Math.Abs(live.ShiftedRatio - 0.761) < 0.005,
                  $"the shipped defaults pick +250 m at 0.761 (pre-registered on leg_check.py; got " +
                  $"{live.OffsetMeters:F0} m at {live.ShiftedRatio:F3})");

            // THE DECISIVE CHECK. C1 alone puts the shift at +75 m NORTH from this same anchor, so
            // the southward choice was C2's doing and nothing else's.
            var liveLineOnly = RouteShift.ChooseForLeg(RidgeALive, RidgeB, liveBase, lineOnly,
                                                       svc.WorstRatioScorer(TankLimitRaw));
            Console.WriteLine($"     line-only: {liveLineOnly.Note}");
            Check(ref failures, liveLineOnly.Shifted && liveLineOnly.OffsetMeters > 0
                             && Math.Abs(liveLineOnly.OffsetMeters - 75.0) < 1e-9,
                  $"C1 ALONE chooses +75 m north from the live anchor (got {liveLineOnly.OffsetMeters:+0;-0} m) " +
                  "- so the run's southward shift was C2 siding the search, not C1 scoring the ground");
            Check(ref failures, Math.Sign(live.OffsetMeters) == Math.Sign(liveLineOnly.OffsetMeters),
                  "C2 never changes the SIDE C1 chose - it may only push the magnitude outward on it");
            Check(ref failures, Math.Abs(live.OffsetMeters) >= Math.Abs(liveLineOnly.OffsetMeters),
                  "...and outward is the only direction it may push");
            var southTried = live.Tried.Where(c => c.OffsetMeters < 0).ToList();
            Check(ref failures, southTried.All(c => !c.Accepted),
                  $"no southward candidate is ever ACCEPTED on this leg ({southTried.Count} tried)");
            Check(ref failures, live.Tried.All(c => Math.Abs(c.OffsetMeters) <= 250.0 + 1e-9),
                  "the search stops at the winning magnitude - it does not keep scoring past it");
        }

        // ------------------------------------------------- 3c. UNKNOWN GROUND IS NEVER CLEAR
        Console.WriteLine("-- 3c. a candidate over missing tiles is UNSCORABLE, not clear");
        {
            // The pure rule, driven against the production chooser with a synthetic scorer: a
            // beautiful ratio plus ONE missing sample must still be refused. (The V8 run itself had
            // 0 NaN samples on every candidate at every magnitude, so this is hardening, not the
            // cause - and a rule that cannot be exercised by a real cache is exactly the rule that
            // needs a fixture.)
            var ridgeBase2 = svc.ScoreLeg(RidgeA, RidgeB, TankLimitRaw);
            var unknown = RouteShift.ChooseForLeg(RidgeA, RidgeB, ridgeBase2, opt,
                                                  _ => new PolyScore(0.100, 1));
            Check(ref failures, !unknown.Shifted,
                  $"a 0.100 candidate with ONE missing tile is refused, not taken ({unknown.Note})");
            Check(ref failures, unknown.Note.Contains("UNSCORABLE"),
                  $"...and the reason says so ({unknown.Note})");
            Check(ref failures, unknown.Tried.Count > 0 && unknown.Tried.All(c => c.NanSamples > 0 || c.Refused),
                  "every candidate carries its own missing-tile count");

            // The control that makes the check falsifiable: the SAME ratio with no missing sample
            // is taken at once.
            var known = RouteShift.ChooseForLeg(RidgeA, RidgeB, ridgeBase2, opt,
                                                _ => new PolyScore(0.100, 0));
            Check(ref failures, known.Shifted && Math.Abs(known.OffsetMeters) <= opt.StepMeters + 1e-9,
                  $"the same ratio with NO missing sample is accepted at the first step ({known.Note})");

            // End to end: no tiles at all -> NO VERDICT -> not flagged -> the authored line, untouched.
            string empty = Path.Combine(Path.GetTempPath(),
                                        "routeshift-selftest-nocache-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var bare = new PreflightService(new PreflightOptions { CacheDir = empty, Offline = true });
                var route = new List<(double Lat, double Lon)> { RidgeA, RidgeB };
                var outcome = bare.ShiftRoute(route, TankLimitRaw, opt);
                Check(ref failures, outcome.Legs.Count == 1 && outcome.Legs[0].NoVerdict,
                      "with no tiles at all the leg gets NO VERDICT");
                Check(ref failures, !outcome.Legs[0].Flagged && outcome.Shifts.Count == 0,
                      "it is neither flagged nor shifted - the pre-flight reports, it does not guess");
                Check(ref failures, !outcome.Changed && outcome.Route.Count == 2
                      && outcome.Route[0] == RidgeA && outcome.Route[1] == RidgeB,
                      "the route comes back as authored, point for point");
                Check(ref failures, bare.Tiles.Fetched == 0, "and nothing was fetched trying");
            }
            finally
            {
                try { if (Directory.Exists(empty)) Directory.Delete(empty, true); } catch { }
            }
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

    /// <summary>
    /// SECTION 0 - THE SHIPPED DEFAULT IS ON (user ruling 2026-09-20: "Route shift: ON. Use as
    /// default for any run."), AND AN EXPLICIT false STILL TURNS IT OFF.
    ///
    /// Three places can state the default and all three are checked, because a default that
    /// changes WHERE UNITS DRIVE must not be true in one of them and false in another:
    ///   the C# property initialiser (what a run with no configuration file gets),
    ///   src/VrfC2SimApp/appsettings.json (the shipped file),
    ///   src/VrfC2SimApp/appsettings.Demo.json (the demo overlay).
    /// The off-switch is exercised through the REAL configuration stack - the json files as the
    /// Host layers them, then the environment - rather than by setting the property directly, so
    /// this asserts the documented escape hatch and not a C# assignment.
    ///
    /// No tile, no network, no bridge: it runs in a checkout with no preflight cache at all.
    /// </summary>
    private static void Defaults(ref int failures, string repo)
    {
        Console.WriteLine("-- 0. the shipped default (user ruling 2026-09-20: route shift ON)");
        string appSettings = Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.json");
        string demoSettings = Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.Demo.json");

        Check(ref failures, new VrfSettings().PreflightRouteShift,
              "VrfSettings.PreflightRouteShift initialises to TRUE - a run with no configuration " +
              "file at all still shifts");
        Check(ref failures, File.Exists(appSettings) && File.Exists(demoSettings),
              $"both shipped settings files are on disk ({appSettings}, {demoSettings})");
        if (!File.Exists(appSettings) || !File.Exists(demoSettings)) return;

        // The json files as the Host reads them, in the Host's own order.
        var shipped = new ConfigurationBuilder().AddJsonFile(appSettings, optional: false).Build()
                          .GetSection("Vrf").Get<VrfSettings>() ?? new VrfSettings();
        Check(ref failures, shipped.PreflightRouteShift,
              "appsettings.json SAYS true - the default is written down, not only compiled in");
        var demo = new ConfigurationBuilder()
                       .AddJsonFile(appSettings, optional: false)
                       .AddJsonFile(demoSettings, optional: false).Build()
                       .GetSection("Vrf").Get<VrfSettings>() ?? new VrfSettings();
        Check(ref failures, demo.PreflightRouteShift,
              "the DEMO overlay keeps it true (appsettings.json + appsettings.Demo.json)");

        // THE OFF SWITCH, twice: the config key and the environment override the RUNBOOK names.
        var offByKey = new ConfigurationBuilder()
                           .AddJsonFile(appSettings, optional: false)
                           .AddInMemoryCollection(new Dictionary<string, string>
                               { ["Vrf:PreflightRouteShift"] = "false" }).Build()
                           .GetSection("Vrf").Get<VrfSettings>();
        Check(ref failures, offByKey != null && !offByKey.PreflightRouteShift,
              "an explicit \"PreflightRouteShift\": false in a later settings file TURNS IT OFF");

        const string EnvKey = "Vrf__PreflightRouteShift";
        string savedEnv = Environment.GetEnvironmentVariable(EnvKey);
        try
        {
            Environment.SetEnvironmentVariable(EnvKey, "false");
            var offByEnv = new ConfigurationBuilder()
                               .AddJsonFile(appSettings, optional: false)
                               .AddJsonFile(demoSettings, optional: false)
                               .AddEnvironmentVariables().Build()
                               .GetSection("Vrf").Get<VrfSettings>();
            Check(ref failures, offByEnv != null && !offByEnv.PreflightRouteShift,
                  $"{EnvKey}=false TURNS IT OFF over BOTH json files - the escape hatch the RUNBOOK " +
                  "and StartInterface52.ps1 -RouteShift off name");
            Environment.SetEnvironmentVariable(EnvKey, "true");
            var onByEnv = new ConfigurationBuilder()
                              .AddJsonFile(appSettings, optional: false)
                              .AddEnvironmentVariables().Build()
                              .GetSection("Vrf").Get<VrfSettings>();
            Check(ref failures, onByEnv != null && onByEnv.PreflightRouteShift,
                  $"{EnvKey}=true leaves it on (the override is read at all - a check that cannot " +
                  "pass by the key being ignored)");
        }
        finally { Environment.SetEnvironmentVariable(EnvKey, savedEnv); }
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
