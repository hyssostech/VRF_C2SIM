using System.Globalization;

namespace VrfC2SimApp.Preflight;

/// <summary>
/// Everything the chooser is allowed to vary, and the one number it borrows
/// (<see cref="Threshold"/>) from the pre-flight it serves. Defaults ARE the design note
/// (docs/experiments/DESIGN_ROUTE_SHIFT_2026-09-15.md sec 5); every one of them is a measured
/// quantity or a vendor parameter, not a tuned constant.
/// </summary>
public sealed record RouteShiftOptions
{
    /// <summary>Search band, +/- metres. PREREG_RIDGE_AG 3.3 measured +50..+550 clear; one step above.</summary>
    public double MaxMeters { get; init; } = 600.0;

    /// <summary>Search granularity = the vendor's own formation slot spacing (25 m).</summary>
    public double StepMeters { get; init; } = 25.0;

    /// <summary>How far BELOW the threshold a shifted line must land. The 0.097 calibration
    /// margin (PREFLIGHT_CALIBRATION: lowest frozen 0.966, best clean pass 0.870) rounded up.</summary>
    public double MarginRatio { get; init; } = 0.10;

    /// <summary>C2: every formation slot line must clear the THRESHOLD (not the margin).</summary>
    public bool ClearFormationBand { get; init; } = true;

    /// <summary>Pad each side of the flagged window. The measured freezes sit 31.5 m short of the
    /// window's near edge and 44.0 m short of its centre (PREREG_RIDGE_AG 3.2) - they stop at the
    /// TOE, so a detour that began at the window edge would begin 30-45 m too late.</summary>
    public double PadMeters { get; init; } = 50.0;

    /// <summary>Settled parallel run before the padded window: formationLength 60 (the unit's own
    /// maneuver-in-formation rows) + the widest slot 50. NOT the turning circle - the M1A2's
    /// turning-radius is 0.3 m and it pivots.</summary>
    public double LeadMeters { get; init; } = 110.0;

    /// <summary>The DESIGNED corner angle where the path leaves the authored line and where it
    /// returns: the transit is |offset| / tan(this). It is not a cap on something computed
    /// elsewhere - it is what sets the transit's length.</summary>
    public double MaxTurnDegrees { get; init; } = 30.0;

    /// <summary>The pre-flight's own flag threshold. Never a second threshold.</summary>
    public double Threshold { get; init; } = LegScorer.DefaultThreshold;

    /// <summary>The vendor's own follower rightOffsets, from this project's OWN app logs
    /// (READ_4-27_G3_AND_OFFSET_SCORING sec 2.2: {-50, -25, 0, +25, +50}).</summary>
    public IReadOnlyList<double> FormationSlots { get; init; } = new[] { -50.0, -25.0, 0.0, 25.0, 50.0 };

    /// <summary>C1's acceptance ceiling.</summary>
    public double AcceptRatio => Threshold - MarginRatio;
}

/// <summary>
/// What the terrain says about ONE candidate polyline: its worst leg ratio, and how many of its
/// sampled points had NO elevation tile.
///
/// The NaN count is carried OUT of the scorer because the chooser's rule and the flag's rule are
/// deliberately different. A FLAG is a warning and tolerates up to
/// <see cref="LegScorer.MaxNanFraction"/> missing samples so a tile gap cannot silence it. A SHIFT
/// is an ACTION - it changes where vehicles drive - and refuses any candidate with a single
/// unknown sample: unknown is never clear.
/// </summary>
public readonly record struct PolyScore(double WorstRatio, int NanSamples);

/// <summary>One offset the chooser tried, and what became of it. Kept so the log and the
/// ObservationReport can say WHY an offset was picked or why none was.</summary>
public sealed record ShiftCandidate(double OffsetMeters, double Ratio, double BandMax,
                                    int NanSamples, bool Accepted, string Refusal)
{
    public bool Refused => Refusal.Length > 0;
}

/// <summary>The outcome for ONE leg: shifted (with the two waypoints), or not (with the reason).</summary>
public sealed record LegShift
{
    public int LegIndex { get; init; }
    public bool Shifted { get; init; }
    public double OffsetMeters { get; init; }
    public double BaseRatio { get; init; }
    public double ShiftedRatio { get; init; }
    public double BandMax { get; init; }

    /// <summary>The two OFFSET waypoints - where the detour actually stands clear of the face.
    /// They are what a log or a report names; <see cref="Inserted"/> is what gets driven.</summary>
    public (double Lat, double Lon) In { get; init; }
    public (double Lat, double Lon) Out { get; init; }

    /// <summary>Every point inserted between the leg's two authored vertices, in order: the two
    /// transit points ON the authored line and the two offset waypoints between them.</summary>
    public List<(double Lat, double Lon)> Inserted { get; init; } = new();
    public double BestRatioTried { get; init; } = double.NaN;
    public double BestOffsetTried { get; init; } = double.NaN;
    public double BandSearchedMeters { get; init; }
    public string Note { get; init; } = "";
    public List<ShiftCandidate> Tried { get; init; } = new();

    /// <summary>"north"/"south"/"east"/"west" for the chosen side, from the leg's own bearing -
    /// the operator reads a compass word, not a sign convention.</summary>
    public string SideWord { get; init; } = "";

    /// <summary>TRUE when the shift was taken on the ROUTE-LINE rule alone because no offset on
    /// the side C1 chose could also clear the formation band. The shift still happened - refusing
    /// would leave the WHOLE unit on the face to spare one slot line - but some slot lines may sit
    /// on flagged ground and every channel says so.</summary>
    public bool BandNotCleared { get; init; }
}

/// <summary>
/// A claim that exactly ONE caller can ever win.
///
/// It exists for one defect: a deferred dispatch can be continued by the worker that finished it
/// OR by the timeout sweep that gave up on it, and if both continued it the task would be
/// DISPATCHED TWICE - two routes, two MoveAlongRoute, two MarkDispatched. It lives here, out of
/// the service, so the self-test can drive the race against the production type rather than
/// against a copy of it.
/// </summary>
public sealed class OneShotClaim
{
    private int _claimed;

    /// <summary>True for the first caller only, on any thread.</summary>
    public bool Claim() => Interlocked.CompareExchange(ref _claimed, 1, 0) == 0;

    /// <summary>Whether anybody has claimed it yet. Diagnostics only - never a gate.</summary>
    public bool Claimed => Volatile.Read(ref _claimed) != 0;
}

/// <summary>
/// THE LATERAL ROUTE SHIFT - pure geometry and a pure chooser. No tiles, no files, no network,
/// no clock, no logging: the terrain enters ONLY through the <c>worstRatio</c> delegate the
/// caller supplies, which is what lets the self-test drive this against the committed offline
/// tile cache and lets the service drive it against the live one.
///
/// WHAT IT DOES (docs/experiments/DESIGN_ROUTE_SHIFT_2026-09-15.md): when the pre-flight flags a
/// leg, insert TWO waypoints that carry the path laterally onto ground the same sampler scores
/// as clear, and leave STP's own vertices untouched and in order. The vendor warrant for
/// insertion being the lever at all: "The Move Along Route task takes a route object and
/// performs a sequence of movements directly to each vertex of the route. There is no path
/// planning done as it moves toward the next vertex."
/// (vrforces5.2d\doc\help\Content\ConceptsEntityLevel\GroundVehMove\
/// vrf_MoveAlongRouteTaskFunctionality.htm), and createRoute takes an arbitrary ordered vertex
/// list (vrfcontrol/vrfRemoteController.h:1023-1038).
///
/// IT NEVER REFUSES A TASK. Every path through here either returns a shifted route or returns
/// the route it was given. A leg it cannot help is REPORTED, never rewritten on a guess.
/// </summary>
public static class RouteShift
{
    /// <summary>Metres per degree of latitude - the constant leg_check.py's offset generator uses,
    /// kept identical so the two agree to the third decimal on the published tables.</summary>
    public const double MetresPerDegree = 111320.0;

    /// <summary>Initial bearing a->b, degrees clockwise from north.</summary>
    public static double BearingDegrees((double Lat, double Lon) a, (double Lat, double Lon) b)
    {
        double la1 = a.Lat * Math.PI / 180.0, la2 = b.Lat * Math.PI / 180.0;
        double dLon = (b.Lon - a.Lon) * Math.PI / 180.0;
        double y = Math.Sin(dLon) * Math.Cos(la2);
        double x = Math.Cos(la1) * Math.Sin(la2) - Math.Sin(la1) * Math.Cos(la2) * Math.Cos(dLon);
        return (Math.Atan2(y, x) * 180.0 / Math.PI + 360.0) % 360.0;
    }

    /// <summary>
    /// Move a point <paramref name="metres"/> along the RIGHT-HAND normal of
    /// <paramref name="bearingDeg"/> (negative = left). This is the same construction
    /// READ_4-27_G3_AND_OFFSET_SCORING sec 2.1 used for its offset lines, so the numbers here and
    /// the numbers in that record are on one scale.
    /// </summary>
    public static (double Lat, double Lon) OffsetLateral((double Lat, double Lon) p,
                                                         double bearingDeg, double metres)
    {
        double th = (bearingDeg + 90.0) * Math.PI / 180.0;
        double dLat = metres * Math.Cos(th) / MetresPerDegree;
        double dLon = metres * Math.Sin(th) / (MetresPerDegree * Math.Cos(p.Lat * Math.PI / 180.0));
        return (p.Lat + dLat, p.Lon + dLon);
    }

    /// <summary>The point <paramref name="s"/> metres along the straight line a-&gt;b.</summary>
    public static (double Lat, double Lon) PointAlong((double Lat, double Lon) a,
                                                      (double Lat, double Lon) b, double s)
    {
        double len = TileMath.DistanceMeters(a.Lat, a.Lon, b.Lat, b.Lon);
        if (len <= 0) return a;
        return TileMath.Interpolate(a.Lat, a.Lon, b.Lat, b.Lon, s / len);
    }

    /// <summary>The compass word for a signed lateral offset on a leg of this bearing.</summary>
    public static string SideWordFor(double bearingDeg, double offsetMeters)
    {
        double th = (bearingDeg + (offsetMeters >= 0 ? 90.0 : 270.0)) % 360.0;
        if (th < 45 || th >= 315) return "north";
        if (th < 135) return "east";
        if (th < 225) return "south";
        return "west";
    }

    /// <summary>
    /// The detour for one candidate offset: FOUR inserted points, so the path is
    /// A -&gt; Pin -&gt; D1 -&gt; D2 -&gt; Pout -&gt; B.
    ///
    ///   Pin, Pout  ON THE AUTHORED LINE, where the path leaves it and rejoins it;
    ///   D1, D2     on the line <paramref name="offsetMeters"/> metres laterally off it,
    ///              spanning the flagged window plus its pad plus a lead each side.
    ///
    /// WHY FOUR AND NOT TWO. A two-point form (A -&gt; D1 -&gt; D2 -&gt; B) makes the whole
    /// approach a slow diagonal, and that diagonal drifts across the ridge's shoulder: MEASURED
    /// on the reference leg, it scores 0.79-0.86 where the authored line over the same stretch
    /// scores 0.608, and the search then walks past the north side the record proved drivable and
    /// picks a SOUTH shift on the strength of ground the unit was never going to cross. Keeping
    /// the approach on the authored line and paying one corner instead is both closer to STP's
    /// intent and measurably better ground (design note sec 3.1a).
    ///
    /// Returns null with a REFUSAL when the leg has no room for the transit and lead - a unit
    /// cannot side-step a face it is already standing on, and the honest answer is a report.
    /// </summary>
    public static List<(double Lat, double Lon)> BuildDetour((double Lat, double Lon) a,
                                                             (double Lat, double Lon) b,
                                                             double windowStartM, double windowEndM,
                                                             double offsetMeters, RouteShiftOptions opt,
                                                             out string refusal)
    {
        refusal = "";
        double legLen = TileMath.DistanceMeters(a.Lat, a.Lon, b.Lat, b.Lon);
        double bearing = BearingDegrees(a, b);
        double eIn = Math.Max(0.0, windowStartM - opt.PadMeters);
        double eOut = Math.Min(legLen, windowEndM + opt.PadMeters);
        double d1S = eIn - opt.LeadMeters;
        double d2S = eOut + opt.LeadMeters;
        double transit = TransitMeters(offsetMeters, opt);
        double pInS = d1S - transit;
        double pOutS = d2S + transit;
        if (pInS <= 0.0 || pOutS >= legLen)
        {
            refusal = FormattableString.Invariant(
                $"no room for the transit+lead (window {eIn:F0}-{eOut:F0} m of a {legLen:F0} m leg; lead {opt.LeadMeters:F0} m, transit {transit:F0} m at {opt.MaxTurnDegrees:F0} deg)");
            return null;
        }
        return new List<(double Lat, double Lon)>
        {
            a,
            PointAlong(a, b, pInS),
            OffsetLateral(PointAlong(a, b, d1S), bearing, offsetMeters),
            OffsetLateral(PointAlong(a, b, d2S), bearing, offsetMeters),
            PointAlong(a, b, pOutS),
            b,
        };
    }

    /// <summary>The along-leg length of one transit: the run over which the path changes lane, so
    /// that the corner at each end is exactly <see cref="RouteShiftOptions.MaxTurnDegrees"/>.</summary>
    public static double TransitMeters(double offsetMeters, RouteShiftOptions opt)
    {
        double deg = Math.Min(89.0, Math.Max(1.0, opt.MaxTurnDegrees));
        return Math.Abs(offsetMeters) / Math.Tan(deg * Math.PI / 180.0);
    }

    /// <summary>
    /// The formation band of a driven polyline: every vertex moved <paramref name="slotMeters"/>
    /// along the LEG normal. That is what a follower drives - "Each subordinate computes an offset
    /// route and then traverses it by planning a path to each vertex in sequence"
    /// (Tasks/MovementTasks/ManeuverAlong.htm) - including at the start, where a member begins in
    /// its own slot rather than on the unit's point.
    /// </summary>
    public static List<(double Lat, double Lon)> SlotLine(IReadOnlyList<(double Lat, double Lon)> poly,
                                                          double bearingDeg, double slotMeters)
    {
        var outp = new List<(double Lat, double Lon)>(poly.Count);
        foreach (var p in poly) outp.Add(OffsetLateral(p, bearingDeg, slotMeters));
        return outp;
    }

    /// <summary>
    /// THE CHOOSER, in TWO PHASES.
    ///
    /// PHASE A picks the SIDE, from C1 ALONE: magnitudes in increasing order, both sides at each,
    /// and the FIRST magnitude at which anything satisfies C1 decides the side (lower ratio on a
    /// tie). PHASE B picks the MAGNITUDE on THAT SIDE ONLY: outward from there, the first
    /// magnitude that satisfies C1 and - when it is on - C2.
    ///
    /// WHY THE SPLIT, and it is a MEASUREMENT not a preference (run 20260915T023743Z, the feature's
    /// first live run; docs/experiments/PREREG_V8_ROUTE_SHIFT_2026-09-15.md RESULTS). The design note
    /// sec 4.1 says C2 "is used here only to SIZE a shift that the route-line verdict has already
    /// demanded". In that run it did not size the shift, it SIDED it: the unit's live anchor sat
    /// 26.7 m from the anchor the calibration and this suite use, +75 m NORTH cleared C1 at 0.735,
    /// its inner slot line scored 1.022, C2 refused it - and the old single-phase search walked past
    /// the north side the record has actually driven (N2d) and took -125 m SOUTH, the side six runs
    /// froze on and the side the pre-registration named as a STOP. C1 is the verdict; C2 may cost
    /// distance on the verdict's own side and nothing else.
    ///
    /// UNKNOWN IS NEVER CLEAR. A candidate polyline with ANY sample that had no elevation tile is
    /// UNSCORABLE and refused outright, rather than merely tolerated up to the flag's
    /// <see cref="LegScorer.MaxNanFraction"/>. (This was NOT the cause of the V8 south shift - that
    /// run had 0 NaN samples on every candidate at every magnitude - it is the hardening the
    /// investigation of it demanded.)
    ///
    /// ACCEPTANCE (design note sec 4.1):
    ///   C1  the whole shifted polyline's worst ratio &lt;= Threshold - MarginRatio;
    ///   C2  (optional, default on) every formation slot line of it is below the THRESHOLD.
    /// C2 does not touch the calibration: the band still flags nothing and decides no leg's
    /// verdict (the standing ruling, READ_4-27_G3_AND_OFFSET_SCORING sec 2.5).
    ///
    /// If NO magnitude on the chosen side also clears C2, the chooser falls back to the SMALLEST
    /// C1-clearing candidate on that side - exactly the brief's own rule - and says so in the note,
    /// the log and the report. Refusing instead would leave the whole unit on the face to spare one
    /// slot line.
    /// </summary>
    /// <param name="score">Scores a polyline: its worst leg ratio and its missing-tile count. The
    /// ONLY way terrain enters this class.</param>
    public static LegShift ChooseForLeg((double Lat, double Lon) a, (double Lat, double Lon) b,
                                        LegMetrics leg, RouteShiftOptions opt,
                                        Func<IReadOnlyList<(double Lat, double Lon)>, PolyScore> score)
    {
        var tried = new List<ShiftCandidate>();
        double legLen = TileMath.DistanceMeters(a.Lat, a.Lon, b.Lat, b.Lon);
        double bearing = BearingDegrees(a, b);
        // The flagged window's own ends, taken from the scorer's worst-window endpoints - which
        // ARE on the leg - rather than re-derived from the centre and a half width.
        double windowStart = TileMath.DistanceMeters(a.Lat, a.Lon, leg.WorstFrom.Lat, leg.WorstFrom.Lon);
        double windowEnd = TileMath.DistanceMeters(a.Lat, a.Lon, leg.WorstTo.Lat, leg.WorstTo.Lon);
        double bestRatio = double.NaN, bestOffset = double.NaN;

        // ------------------------------------------------ PHASE A: the SIDE, from C1 alone
        double sideSign = 0.0, sideMag = 0.0, sideRatio = double.NaN;
        List<(double Lat, double Lon)> sidePoly = null;
        for (double mag = opt.StepMeters; mag <= opt.MaxMeters + 1e-9; mag += opt.StepMeters)
        {
            var clearing = new List<(double Offset, double Ratio, List<(double Lat, double Lon)> Poly)>();
            foreach (double sign in new[] { 1.0, -1.0 })
            {
                double offset = sign * mag;
                var poly = BuildDetour(a, b, windowStart, windowEnd, offset, opt, out string refusal);
                if (poly == null)
                {
                    tried.Add(new ShiftCandidate(offset, double.NaN, double.NaN, 0, false, refusal));
                    continue;
                }
                var sc = score(poly);
                if (sc.NanSamples > 0)
                {
                    tried.Add(new ShiftCandidate(offset, double.NaN, double.NaN, sc.NanSamples, false,
                                                 UnknownReason(sc.NanSamples)));
                    continue;
                }
                if (double.IsNaN(bestRatio) || sc.WorstRatio < bestRatio)
                { bestRatio = sc.WorstRatio; bestOffset = offset; }
                if (sc.WorstRatio <= opt.AcceptRatio) clearing.Add((offset, sc.WorstRatio, poly));
                else tried.Add(new ShiftCandidate(offset, sc.WorstRatio, double.NaN, 0, false,
                                                  FormattableString.Invariant(
                                                      $"ratio {sc.WorstRatio:F3} > {opt.AcceptRatio:F3}")));
            }
            if (clearing.Count == 0) continue;
            clearing.Sort((x, y) => x.Ratio.CompareTo(y.Ratio));
            sideSign = Math.Sign(clearing[0].Offset);
            sideMag = mag;
            sideRatio = clearing[0].Ratio;
            sidePoly = clearing[0].Poly;
            string winSide = SideWordFor(bearing, clearing[0].Offset);
            string notSide = FormattableString.Invariant(
                                 $"C1 cleared, but {winSide} cleared at the same {mag:F0} m")
                           + FormattableString.Invariant(
                                 $" with a lower ratio ({clearing[0].Ratio:F3}) and so decides the side");
            for (int i = 1; i < clearing.Count; i++)
                tried.Add(new ShiftCandidate(clearing[i].Offset, clearing[i].Ratio, double.NaN, 0, false,
                                             notSide));
            break;
        }
        if (sideSign == 0.0)
            return NoLine(leg, opt, tried, bestRatio, bestOffset, legLen, windowStart, windowEnd);

        // ------------------------------------ PHASE B: the MAGNITUDE, on the chosen side only
        string side = SideWordFor(bearing, sideSign);
        double fbOffset = double.NaN, fbRatio = double.NaN;
        List<(double Lat, double Lon)> fbPoly = null;
        for (double mag = sideMag; mag <= opt.MaxMeters + 1e-9; mag += opt.StepMeters)
        {
            double offset = sideSign * mag;
            List<(double Lat, double Lon)> poly;
            double ratio;
            if (mag == sideMag) { poly = sidePoly; ratio = sideRatio; }
            else
            {
                poly = BuildDetour(a, b, windowStart, windowEnd, offset, opt, out string refusal);
                if (poly == null)
                {
                    tried.Add(new ShiftCandidate(offset, double.NaN, double.NaN, 0, false, refusal));
                    continue;
                }
                var sc = score(poly);
                if (sc.NanSamples > 0)
                {
                    tried.Add(new ShiftCandidate(offset, double.NaN, double.NaN, sc.NanSamples, false,
                                                 UnknownReason(sc.NanSamples)));
                    continue;
                }
                ratio = sc.WorstRatio;
                if (double.IsNaN(bestRatio) || ratio < bestRatio) { bestRatio = ratio; bestOffset = offset; }
                if (ratio > opt.AcceptRatio)
                {
                    tried.Add(new ShiftCandidate(offset, ratio, double.NaN, 0, false,
                                                 FormattableString.Invariant(
                                                     $"ratio {ratio:F3} > {opt.AcceptRatio:F3}")));
                    continue;
                }
            }
            double bandMax = double.NaN;
            bool ok = true;
            if (opt.ClearFormationBand)
            {
                bandMax = 0.0;
                foreach (double slot in opt.FormationSlots)
                {
                    double r;
                    if (slot == 0.0) r = ratio;
                    else
                    {
                        var ss = score(SlotLine(poly, bearing, slot));
                        // A slot line over unknown ground is not a clear slot line.
                        r = ss.NanSamples > 0 ? double.PositiveInfinity : ss.WorstRatio;
                    }
                    if (r > bandMax) bandMax = r;
                }
                ok = bandMax < opt.Threshold;
            }
            tried.Add(new ShiftCandidate(offset, ratio, bandMax, 0, ok,
                                         ok ? "" : DeclineReason(ratio, bandMax, opt)));
            if (double.IsNaN(fbOffset)) { fbOffset = offset; fbRatio = ratio; fbPoly = poly; }
            if (ok)
                return Shifted(leg, opt, bearing, offset, ratio, bandMax, poly, tried,
                               bestRatio, bestOffset, "");
        }

        // C2 could not be cleared ANYWHERE on the side C1 chose. Take the brief's own rule - the
        // route line alone, on that side - and say loudly that the band is not clear. Walking to
        // the other side instead is the defect this two-phase chooser exists to prevent.
        if (fbPoly != null)
        {
            string bandNote = FormattableString.Invariant(
                                  $" - NOTE: no offset on the {side} side cleared the formation band")
                            + FormattableString.Invariant(
                                  $" below {opt.Threshold:F2} within +/-{opt.MaxMeters:F0} m, so this is the ")
                            + "ROUTE-LINE rule alone and some formation slots may sit on flagged ground";
            return Shifted(leg, opt, bearing, fbOffset, fbRatio, double.NaN, fbPoly, tried,
                           bestRatio, bestOffset, bandNote);
        }
        return NoLine(leg, opt, tried, bestRatio, bestOffset, legLen, windowStart, windowEnd);
    }

    /// <summary>The chosen detour, packaged. <paramref name="extra"/> is empty for an ordinary
    /// accept and carries the band-not-cleared note for the fallback.</summary>
    private static LegShift Shifted(LegMetrics leg, RouteShiftOptions opt, double bearing,
                                    double offset, double ratio, double bandMax,
                                    List<(double Lat, double Lon)> poly, List<ShiftCandidate> tried,
                                    double bestRatio, double bestOffset, string extra)
    {
        string bandNote = opt.ClearFormationBand && !double.IsNaN(bandMax)
            ? FormattableString.Invariant($", formation band max {bandMax:F3}") : "";
        string side = SideWordFor(bearing, offset);
        return new LegShift
        {
            LegIndex = leg.Index,
            Shifted = true,
            OffsetMeters = offset,
            BaseRatio = leg.Ratio,
            ShiftedRatio = ratio,
            BandMax = bandMax,
            In = poly[2],
            Out = poly[3],
            Inserted = poly.GetRange(1, poly.Count - 2),
            BestRatioTried = bestRatio,
            BestOffsetTried = bestOffset,
            BandSearchedMeters = Math.Abs(offset),
            SideWord = side,
            Tried = tried,
            BandNotCleared = extra.Length > 0,
            Note = FormattableString.Invariant(
                $"shifted {Math.Abs(offset):F0} m {side}: ratio {leg.Ratio:F3} -> {ratio:F3}{bandNote}") + extra,
        };
    }

    /// <summary>The honest ending: a flagged leg nothing in the band could clear.</summary>
    private static LegShift NoLine(LegMetrics leg, RouteShiftOptions opt, List<ShiftCandidate> tried,
                                   double bestRatio, double bestOffset, double legLen,
                                   double windowStart, double windowEnd)
    {
        string bandClause = opt.ClearFormationBand
            ? FormattableString.Invariant($" with the formation band below {opt.Threshold:F2}") : "";
        int unknown = tried.Count(c => c.NanSamples > 0);
        string unknownClause = unknown > 0
            ? FormattableString.Invariant(
                $"; {unknown} candidate(s) were UNSCORABLE - part of the line had no elevation tile") : "";
        string head = FormattableString.Invariant($"NO CLEARED LINE within +/-{opt.MaxMeters:F0} m: ");
        string note = double.IsNaN(bestRatio)
            ? head + "every candidate was refused on geometry or had no terrain data "
                   + FormattableString.Invariant(
                         $"(leg {legLen:F0} m, window at {windowStart:F0}-{windowEnd:F0} m){unknownClause}")
            : head + FormattableString.Invariant(
                         $"the best candidate ({bestOffset:+0;-0} m) scored {bestRatio:F3}, ")
                   + FormattableString.Invariant(
                         $"and acceptance needs <= {opt.AcceptRatio:F3}{bandClause}{unknownClause}");
        return new LegShift
        {
            LegIndex = leg.Index,
            Shifted = false,
            BaseRatio = leg.Ratio,
            ShiftedRatio = double.NaN,
            BandMax = double.NaN,
            BestRatioTried = bestRatio,
            BestOffsetTried = bestOffset,
            BandSearchedMeters = opt.MaxMeters,
            Tried = tried,
            Note = note,
        };
    }

    private static string UnknownReason(int nan)
        => FormattableString.Invariant(
            $"UNSCORABLE: {nan} sample(s) had no elevation tile - unknown is never clear");

    private static string DeclineReason(double ratio, double bandMax, RouteShiftOptions opt)
        => ratio > opt.AcceptRatio
            ? FormattableString.Invariant($"ratio {ratio:F3} > {opt.AcceptRatio:F3}")
            : FormattableString.Invariant($"formation band max {bandMax:F3} >= {opt.Threshold:F2}");

    /// <summary>Every candidate the chooser tried, one line, for the log: offset, ratio, band,
    /// missing-tile count and the verdict. A feature that changes where units drive does not get
    /// to keep its reasoning to itself.</summary>
    public static string DescribeCandidates(LegShift s)
        => string.Join("; ", (s.Tried ?? new List<ShiftCandidate>()).Select(DescribeCandidate));

    private static string DescribeCandidate(ShiftCandidate c)
    {
        string ratio = double.IsNaN(c.Ratio) ? "-" : c.Ratio.ToString("F3", CultureInfo.InvariantCulture);
        string band = double.IsNaN(c.BandMax)
            ? "" : " band " + c.BandMax.ToString("F3", CultureInfo.InvariantCulture);
        return FormattableString.Invariant($"{c.OffsetMeters:+0;-0} m ratio {ratio}{band} ")
             + FormattableString.Invariant($"nan {c.NanSamples} {(c.Accepted ? "ACCEPTED" : c.Refusal)}");
    }

    /// <summary>
    /// Splice the chosen detours into a route. The authored vertices are COPIED THROUGH
    /// UNCHANGED, in order; only the inserted points of each shifted leg go between them.
    /// A route with no shifted leg comes back with the same points in the same order.
    /// </summary>
    public static List<(double Lat, double Lon)> Apply(IReadOnlyList<(double Lat, double Lon)> route,
                                                       IReadOnlyList<LegShift> shifts)
    {
        var byLeg = new Dictionary<int, LegShift>();
        if (shifts != null)
            foreach (var s in shifts)
                if (s.Shifted) byLeg[s.LegIndex] = s;
        var outp = new List<(double Lat, double Lon)>(route.Count + 4 * byLeg.Count);
        for (int i = 0; i < route.Count; i++)
        {
            outp.Add(route[i]);
            if (i + 1 >= route.Count) break;
            if (byLeg.TryGetValue(i + 1, out var s)) outp.AddRange(s.Inserted);
        }
        return outp;
    }

    /// <summary>The one-line summary a log or a report puts on a shifted leg.</summary>
    public static string Describe(string taskName, string unitName, LegShift s)
    {
        string inp = Pt(s.In), outp = Pt(s.Out);
        return FormattableString.Invariant(
            $"ROUTE SHIFT: task {taskName} ({unitName}) leg {s.LegIndex} - {s.Note}; inserted {inp} and {outp}. STP's own vertices are unchanged and in order.");
    }

    private static string Pt((double Lat, double Lon) p)
        => "(" + p.Lat.ToString("F6", CultureInfo.InvariantCulture) + ","
               + p.Lon.ToString("F6", CultureInfo.InvariantCulture) + ")";
}
