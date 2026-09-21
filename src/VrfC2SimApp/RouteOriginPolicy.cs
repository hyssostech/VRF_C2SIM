using System;
using System.Collections.Generic;
using System.Globalization;

namespace VrfC2SimApp;

/// <summary>
/// WHERE A COMPOSED PARENT'S ROUTE STARTS (N13, run D9 2026-09-21).
///
/// THE DEFECT. For a COMPOSED parent (a unit whose declared children are materialized at order
/// time - 114.MechCoy~PXY on R9 lean) the app deletes and re-creates the CHILDREN, waits for the
/// CHILDREN to reflect, and then immediately reads THE PARENT's published position as the route
/// origin (VrfC2SimService.ExecuteTaskOnTick, the TryGetEntityGeodetic call). The parent's own
/// VR-Forces object is never deleted, so it keeps publishing right through the window in which
/// its membership is a MIXTURE of deleted shells and new objects - and VR-Forces publishes a
/// composed aggregate at the CENTROID OF ITS DIRECT CHILDREN (D8, settled: 5.9-18.5 m agreement
/// against an all-leaf centroid that diverges to 260 m). A partial or double-counted membership
/// therefore puts the published position on a TRANSIENT sweep away from the truth.
///
/// D9 measured it exactly, against the app's own 202.1 m ring radius R:
///   order-0.20 s   100.9 m at bearing 300.0 deg  = R/2 at 300 (two of three children)
///   order+1.80 s    50.4 m at bearing 119.9 deg  = R/4 at 120 (the 120 slot counted twice)
///   order+3.80 s    42.5 m at 114.4 deg, order+5.90 s 34.2 m at 83.7 deg, ... then settled.
/// D9 dispatched at order+2.40 s, INSIDE that sweep, and built a 1,139 m route (D8: 1,112 m),
/// a 569 m traversal bar (556) and a 285 m arrival radius (278). D8's "0.0 m, N4 FIXED" and
/// D5c's 0.0 m were the SAME code winning the same race by 0.5 s and 0.39 s. Nothing in the
/// dispatch path arbitrates it.
///
/// THE RULE THIS CLASS ENCODES. For a composed parent with two or more declared children, the
/// route origin is the CENTROID OF THOSE CHILDREN'S OWN REFLECTED POSITIONS - which is what the
/// back end itself will publish once it settles, available immediately because the composition
/// gate has ALREADY waited for every child to be readable. At rest it is exact by construction:
/// the DeStacker spreads composed siblings on ONE ring at equal bearings 360/N apart, so the N
/// unit vectors sum to zero and the centroid IS the parent's authored coordinate
/// (DeStacker.CentroidPreservingRadius / EqualBearingOffset; RUNBOOK 11e). Once the unit has
/// MOVED the children move with it, so - unlike "just use the authored coordinate" - the origin
/// follows the fleet instead of marching it back to its assembly point.
///
/// ALL OR NOTHING, AND NOTHING IMPLAUSIBLE. If any declared child cannot be read, or if one of
/// them is an OUTLIER (<see cref="OutlierFactor"/>), the published position is used and SAID OUT
/// LOUD. A PARTIAL centroid is precisely what the transient IS (two of three children give R/2 at
/// 300 deg), and an averaged outlier would drag the route origin AND the STP-833 extent anchor
/// that is measured from it - so neither is ever computed.
///
/// WHAT THIS CLASS DOES NOT KNOW. It is pure: it cannot tell a re-compose transient from a moving
/// company. <see cref="Line"/> therefore always prints the MEASURED gap and claims the transient
/// only when the caller passes evidence that this parent's children were re-created recently
/// (<see cref="RecomposeRecentSeconds"/>). Cold-start review of a34c35b, SF-1: without that, any
/// gap over 10 m printed a diagnosis, and D8 measured 16-25 m on a MOVING company with no
/// re-compose in flight at all - a sentence a harvest would have greped as fact.
///
/// PURE. No bridge call, no logging, no state, no clock. The service reads the children through
/// the same NameRegistry + TryGetEntityGeodetic pair that released them for tasking, stamps the
/// re-create times, and hands the numbers here. Locked by --routeorigin-selftest, whose fixture
/// is D9's own trace.
/// </summary>
public static class RouteOriginPolicy
{
    /// <summary>The DeStacker's own flat-earth constant, so a centroid computed here and a ring
    /// laid out there cannot disagree by a metre of datum (DeStacker.MetersPerDegLat).</summary>
    public const double MetersPerDegLat = 111_320.0;

    /// <summary>Above this many metres the gap between the children's centroid and the parent's
    /// published position is worth a sentence of its own. It is a WORDING threshold only: the
    /// origin used is the same either way. 10 m is an order of magnitude below D9's smallest
    /// observed transient state (34.2 m) and just under D5c's 7-8 m settled asymptote.</summary>
    public const double TransientCallMeters = 10.0;

    /// <summary>
    /// HOW RECENTLY THIS PARENT'S CHILDREN MUST HAVE BEEN RE-CREATED before the gap may be CALLED
    /// a re-compose transient (SF-1). Derived from D5c's measured decay of the parent's own
    /// published position after its children were re-created: 48.2 m at +2 s, 15.4 m by +12 s,
    /// 8.8 m by +25 s, asymptote 7-8 m. By +25 s the transient is already BELOW
    /// <see cref="TransientCallMeters"/> and inside D8's 5.9-18.5 m moving-company residual, i.e.
    /// no longer separable from ordinary motion; 30 s is the next round number above the last
    /// sample at which it still was. Outside this window the line reports the gap and explicitly
    /// declines to diagnose it.
    /// </summary>
    public const double RecomposeRecentSeconds = 30.0;

    /// <summary>
    /// A child further from the children's own MEDIAN position than
    /// max(<see cref="OutlierFactor"/> x the median child radius, <see cref="OutlierFloorMeters"/>)
    /// is an OUTLIER and the centroid is not used (SF-4).
    ///
    /// WHY A MEDIAN AND NOT THE PUBLISHED POSITION: during the very transient this class exists
    /// for, the published position is the untrustworthy quantity. A component-wise median over the
    /// children is robust to one bad child by construction, and the median RADIUS about it is a
    /// robust estimate of the de-stack ring the children were placed on - derived from the data in
    /// hand rather than from a ring radius nothing carries to this call site.
    ///
    /// WHY 4 AND 1,000 m, from the evidence we have:
    ///   - AT REST every child sits on the ring to within 0.1 m (D9: 201.8 / 201.8 / 201.9 m), so
    ///     the factor is not about placement noise.
    ///   - UNDER WAY a formation stretches. D8 sec 2.5 measured this parent's sibling separations
    ///     at 35-486 m against 348-349 m at rest - a worst observed stretch of about 1.4x, i.e.
    ///     about 280 m from the centre on a 202 m ring. FOUR times the median radius leaves about
    ///     3x margin over the worst stretch ever measured, so this cannot fire on a legitimately
    ///     spread formation.
    ///   - THE FLOOR covers the degenerate case where the children are nearly co-located (de-stack
    ///     off, or a group the echelon table could not size), where 4 x ~0 would refuse everything.
    ///     1,000 m is comfortably above the longest shipped GROUND formation span, 660 m
    ///     (RUNBOOK 11e), so it cannot fire on a unit's own members either.
    ///   - WHAT IT DOES CATCH: a child at 0,0 (created but never positioned) - half a world away;
    ///     and the realistic one, a child that was TASKED INDEPENDENTLY and driven kilometres off,
    ///     which would otherwise drag both the route origin and the STP-833 extent anchor that is
    ///     measured from it, so STP-833 could not catch it either.
    /// </summary>
    public const double OutlierFactor = 4.0;

    /// <summary>See <see cref="OutlierFactor"/>. Applies only for THREE OR MORE children: an
    /// outlier test needs a majority to define "normal", and with two children there is no way to
    /// say which of them is the stray. <see cref="MaxChildFromPublishedMeters"/> is the net that
    /// covers N = 2.</summary>
    public const double OutlierFloorMeters = 1000.0;

    /// <summary>
    /// THE ABSOLUTE NET, for every N: no child may be further than this from the parent's own
    /// PUBLISHED position.
    ///
    /// The published position is the quantity this whole class distrusts - but it is distrusted by
    /// a BOUNDED amount. During a re-compose it is the centroid of a partial or double-counted
    /// membership, which cannot be further from the truth than the de-stack ring radius (202.1 m on
    /// R9 lean; 806.7 m worst case on any shipped fixture, R9 full at N=7). A legitimate child sits
    /// within a ring radius of the true centre, so on the worst shipped fixture a legitimate child
    /// is within about 1.6 km of the published position. 4,000 m leaves about 2.5x margin over
    /// that and still catches both cases this guard exists for: a child at 0,0 (created but never
    /// positioned) and a child TASKED INDEPENDENTLY and driven kilometres away.
    /// </summary>
    public const double MaxChildFromPublishedMeters = 4000.0;

    public enum Source
    {
        /// <summary>Not a composed parent (a platform, an independent aggregate, a parent with
        /// fewer than two declared children, or ComposeHierarchy off): the published read is used
        /// UNCHANGED and nothing is logged.</summary>
        NotComposed,
        /// <summary>Every declared child was readable and plausible; the origin is their centroid.</summary>
        ChildrenCentroid,
        /// <summary>At least one declared child could not be read; the published position stands
        /// in and the exposure is stated.</summary>
        ChildrenUnreadable,
        /// <summary>Every child was readable but one is implausibly far from the others; the
        /// published position stands in and the outlier is named.</summary>
        ChildOutlier,
    }

    /// <summary>One declared child of the composed parent, as the service found it.
    /// Readable=false means the name resolved to no uuid, or the uuid was not reflected.
    /// ReflectionProven=true means the position was read at the uuid whose READABILITY released this
    /// child for tasking, not at whatever the name registry currently resolves to - the distinction
    /// matters in the narrow window in which a name still resolves to its DELETED SHELL.</summary>
    public readonly record struct Child(string Name, bool Readable,
                                        double LatDeg, double LonDeg, double AltMeters,
                                        bool ReflectionProven = false);

    /// <param name="PublishedOffsetMeters">Distance from the parent's PUBLISHED position to the
    /// origin actually used. 0 by construction on every arm but <see cref="Source.ChildrenCentroid"/>.</param>
    /// <param name="OutlierName">On <see cref="Source.ChildOutlier"/>, the child that failed the
    /// plausibility bound; null otherwise.</param>
    public readonly record struct Origin(Source From, double LatDeg, double LonDeg, double AltMeters,
                                         int Readable, int Declared, double PublishedOffsetMeters,
                                         string OutlierName = null, double OutlierMeters = 0.0,
                                         double BoundMeters = 0.0);

    /// <summary>
    /// THE DECISION. <paramref name="children"/> is null or short for every taskee that is not a
    /// composed parent, and every arm but the centroid returns the published triple BIT-FOR-BIT -
    /// no arithmetic touches it, so an independent taskee's route is byte-for-byte what it was
    /// before N13.
    /// </summary>
    public static Origin Decide(double publishedLatDeg, double publishedLonDeg, double publishedAltMeters,
                                IReadOnlyList<Child> children)
    {
        int declared = children?.Count ?? 0;
        if (declared < 2)
            return new Origin(Source.NotComposed, publishedLatDeg, publishedLonDeg, publishedAltMeters,
                              0, declared, 0.0);

        int readable = 0;
        for (int i = 0; i < declared; i++)
            if (children[i].Readable && IsUsableCoordinate(children[i])) readable++;
        if (readable != declared)
            return new Origin(Source.ChildrenUnreadable, publishedLatDeg, publishedLonDeg, publishedAltMeters,
                              readable, declared, 0.0);

        // Tangent plane about the PUBLISHED position. The children sit within a ring radius of it
        // (202.1 m on R9 lean, 806.7 m worst case on any shipped fixture), so a flat mean is exact
        // to well under a metre, and anchoring on a point rather than averaging raw degrees is what
        // makes the antimeridian and the cos(lat) scaling safe.
        double metersPerDegLon = MetersPerDegLat
                                 * Math.Max(Math.Cos(publishedLatDeg * Math.PI / 180.0), 0.01);
        var north = new double[declared];
        var east = new double[declared];
        double sumN = 0.0, sumE = 0.0, alt = 0.0;
        for (int i = 0; i < declared; i++)
        {
            var c = children[i];
            north[i] = (c.LatDeg - publishedLatDeg) * MetersPerDegLat;
            east[i] = NormalizeLonDeltaDeg(c.LonDeg - publishedLonDeg) * metersPerDegLon;
            sumN += north[i];
            sumE += east[i];
            alt += c.AltMeters;
        }

        // SF-4: THE PLAUSIBILITY BOUNDS, before any averaging. TWO NETS.
        //
        // (1) THE ABSOLUTE NET, every N: distance from the PUBLISHED position. The published value
        //     is distrusted by a BOUNDED amount (at most the ring radius), so it is useless as a
        //     metre-scale reference and perfectly good as a kilometre-scale one.
        int absWorst = -1;
        double absWorstMeters = 0.0;
        for (int i = 0; i < declared; i++)
        {
            double d = Math.Sqrt(north[i] * north[i] + east[i] * east[i]);
            if (d > absWorstMeters) { absWorstMeters = d; absWorst = i; }
        }
        if (absWorst >= 0 && absWorstMeters > MaxChildFromPublishedMeters)
            return new Origin(Source.ChildOutlier, publishedLatDeg, publishedLonDeg, publishedAltMeters,
                              readable, declared, 0.0, children[absWorst].Name, absWorstMeters,
                              MaxChildFromPublishedMeters);

        // (2) THE RELATIVE NET, N >= 3 ONLY: component-wise median (robust to one bad child), the
        //     median radius about it as the scale, worst child against the bound. It needs a
        //     MAJORITY to define "normal" - with two children there is no way to say which is the
        //     stray, so for N = 2 net (1) is the whole guard. Nothing here touches the published
        //     position, which is the quantity under suspicion at metre scale.
        if (declared >= 3)
        {
            double medN = LowerMedian(north), medE = LowerMedian(east);
            var radii = new double[declared];
            for (int i = 0; i < declared; i++)
                radii[i] = Math.Sqrt((north[i] - medN) * (north[i] - medN) + (east[i] - medE) * (east[i] - medE));
            double bound = Math.Max(OutlierFactor * LowerMedian(radii), OutlierFloorMeters);
            int worst = 0;
            for (int i = 1; i < declared; i++) if (radii[i] > radii[worst]) worst = i;
            if (radii[worst] > bound)
                return new Origin(Source.ChildOutlier, publishedLatDeg, publishedLonDeg, publishedAltMeters,
                                  readable, declared, 0.0, children[worst].Name, radii[worst], bound);
        }

        double meanN = sumN / declared, meanE = sumE / declared;
        double lat = publishedLatDeg + meanN / MetersPerDegLat;
        double lon = NormalizeLonDeg(publishedLonDeg + meanE / metersPerDegLon);
        return new Origin(Source.ChildrenCentroid, lat, lon, alt / declared, readable, declared,
                          Math.Sqrt(meanN * meanN + meanE * meanE));
    }

    /// <summary>A coordinate we are willing to do arithmetic with: finite and on the globe. A
    /// created-but-unpositioned object and an arithmetic accident both land here, and both are
    /// treated as NOT READABLE rather than averaged.</summary>
    public static bool IsUsableCoordinate(Child c) =>
        double.IsFinite(c.LatDeg) && double.IsFinite(c.LonDeg) && double.IsFinite(c.AltMeters)
        && Math.Abs(c.LatDeg) <= 90.0 && Math.Abs(c.LonDeg) <= 180.0;

    /// <summary>The lower median of a copy of <paramref name="values"/> (index (n-1)/2 of the
    /// sorted copy). Lower rather than averaged so the result is always one of the observations -
    /// for an even count the pair is symmetric about the true centre anyway, and the floor governs
    /// the two-child case.</summary>
    public static double LowerMedian(IReadOnlyList<double> values)
    {
        if (values == null || values.Count == 0) return 0.0;
        var copy = new double[values.Count];
        for (int i = 0; i < values.Count; i++) copy[i] = values[i];
        Array.Sort(copy);
        return copy[(copy.Length - 1) / 2];
    }

    /// <summary>Longitude difference wrapped into [-180, 180]: a parent published just east of the
    /// antimeridian and a child just west of it are 0.01 deg apart, not 359.99.</summary>
    public static double NormalizeLonDeltaDeg(double deltaDeg)
    {
        double d = deltaDeg % 360.0;
        if (d > 180.0) d -= 360.0;
        else if (d < -180.0) d += 360.0;
        return d;
    }

    /// <summary>Longitude wrapped into [-180, 180).</summary>
    public static double NormalizeLonDeg(double lonDeg)
    {
        double d = lonDeg % 360.0;
        if (d >= 180.0) d -= 360.0;
        else if (d < -180.0) d += 360.0;
        return d;
    }

    /// <summary>
    /// THE ONE LOUD LINE. Returns null for <see cref="Source.NotComposed"/> - there is nothing to
    /// say about a taskee this policy did not touch - and a full self-sufficient sentence for every
    /// arm that DID decide something, including the two honest ones.
    ///
    /// EVERY ARM PRINTS THE CHILDREN it was handed, with their coordinates and where each was read
    /// from, so the centroid can be re-derived from vrfc2simapp.log alone and an outlier is visible
    /// without going back to the observer trace (SF-3).
    /// </summary>
    /// <param name="secondsSinceRecompose">How long ago this parent's declared children were last
    /// deleted/re-created, from the service's own stamps; NaN when there is no such stamp. It is
    /// the ONLY thing that licenses the words "re-compose transient" (SF-1).</param>
    public static string Line(string unitName, Origin o, IReadOnlyList<Child> children,
                              double secondsSinceRecompose = double.NaN)
    {
        if (o.From == Source.NotComposed) return null;
        var inv = CultureInfo.InvariantCulture;
        string lat = o.LatDeg.ToString("F6", inv), lon = o.LonDeg.ToString("F6", inv);
        string readable = o.Readable.ToString(inv), declared = o.Declared.ToString(inv);
        string roster = DescribeChildren(children);

        if (o.From == Source.ChildrenUnreadable)
        {
            var missing = new List<string>();
            if (children != null)
                for (int i = 0; i < children.Count; i++)
                    if (!children[i].Readable || !IsUsableCoordinate(children[i]))
                        missing.Add(children[i].Name);
            return "ROUTE ORIGIN for composed parent " + unitName + ": FALLING BACK to the parent's OWN "
                 + "PUBLISHED position (" + lat + "," + lon + ") - only " + readable + " of " + declared
                 + " declared child unit(s) can be read with a usable position right now (not usable: ["
                 + string.Join(", ", missing) + "]). " + roster
                 + " NO PARTIAL CENTROID IS COMPUTED: a partial membership is exactly what the "
                 + "re-compose transient IS (N13/D9 - two of three children published R/2 at 300 deg), "
                 + "so averaging what is readable would reproduce the defect while claiming to fix it. "
                 + "THE EXPOSURE: while the children are being re-created VR-Forces publishes this "
                 + "parent at the centroid of whatever is attached, so this route may start up to the "
                 + "de-stack ring radius from where the unit really is, and the route length, the "
                 + "traversal bar and the arrival radius inherit that error. THIS LINE IS THE REPORT: "
                 + "a child can be mid-delete at dispatch with no reflection warning emitted yet (the "
                 + "readiness classification gates on the PARENT, not on its children - D5c showed a "
                 + "dispatch that preceded the re-creates entirely), so do not look above for one.";
        }

        if (o.From == Source.ChildOutlier)
        {
            return "ROUTE ORIGIN for composed parent " + unitName + ": FALLING BACK to the parent's OWN "
                 + "PUBLISHED position (" + lat + "," + lon + ") - declared child " + o.OutlierName
                 + " is " + o.OutlierMeters.ToString("F0", inv) + " m out, past the "
                 + o.BoundMeters.ToString("F0", inv) + " m plausibility bound - either the absolute "
                 + "net (no child more than " + MaxChildFromPublishedMeters.ToString("F0", inv)
                 + " m from the parent's published position) or, for three or more children, the "
                 + "relative one (max(" + OutlierFactor.ToString("F0", inv) + " x the median child "
                 + "radius about their own median, " + OutlierFloorMeters.ToString("F0", inv)
                 + " m)). " + roster
                 + " NO CENTROID IS COMPUTED over an implausible membership: averaging that child would "
                 + "drag BOTH this route's origin and the STP-833 route-extent check that is measured "
                 + "from it, so the extent check could not catch it either. The usual causes are a "
                 + "child created but never positioned, and a child that has been TASKED "
                 + "INDEPENDENTLY and driven away from its parent - in which case this parent's own "
                 + "published position is the better of two imperfect answers, and it is what is used.";
        }

        // ChildrenCentroid. ALWAYS report the measured gap; diagnose it ONLY on evidence.
        string gap = o.PublishedOffsetMeters.ToString("F1", inv);
        bool recent = double.IsFinite(secondsSinceRecompose)
                      && secondsSinceRecompose >= 0.0
                      && secondsSinceRecompose <= RecomposeRecentSeconds;
        string agreement;
        if (o.PublishedOffsetMeters <= TransientCallMeters)
        {
            agreement = "The parent's own published position differs by " + gap + " m, which is inside "
                      + "the " + TransientCallMeters.ToString("F0", inv) + " m at which this line would "
                      + "say more; the centroid is used anyway because it is exact by construction "
                      + "(equal-bearing ring, RUNBOOK 11e) and does not depend on winning a race.";
        }
        else if (recent)
        {
            agreement = "The parent's own published position differs by " + gap + " m, and this parent's "
                      + "declared children were re-created "
                      + secondsSinceRecompose.ToString("F1", inv) + " s ago - inside the "
                      + RecomposeRecentSeconds.ToString("F0", inv) + " s window in which that difference "
                      + "is separable from ordinary motion - so this IS the RE-COMPOSE TRANSIENT: "
                      + "VR-Forces publishes a composed aggregate at the centroid of its DIRECT "
                      + "CHILDREN, and that centroid is still sweeping while the membership settles "
                      + "(N13, run D9 2026-09-21: 50.4 m of origin error lengthened a 1,112 m route to "
                      + "1,139 m). The children's own positions are the settled answer and they are "
                      + "readable NOW, so the route starts there instead of waiting the transient out.";
        }
        else
        {
            agreement = "The parent's own published position differs by " + gap + " m"
                      + (double.IsFinite(secondsSinceRecompose)
                            ? " and this parent's declared children were last re-created "
                              + secondsSinceRecompose.ToString("F1", inv) + " s ago"
                            : " and no re-creation of this parent's declared children is on record")
                      + ", i.e. OUTSIDE the " + RecomposeRecentSeconds.ToString("F0", inv)
                      + " s re-compose window: NO TRANSIENT IS CLAIMED. A difference of this size is "
                      + "consistent with a unit under way - D8 measured 16-25 m between a MOVING "
                      + "company's published position and its direct children's centroid with no "
                      + "re-compose in flight at all. The centroid is used either way; it is the "
                      + "fleet's own centre and it needs no diagnosis to be the right origin.";
        }
        return "ROUTE ORIGIN for composed parent " + unitName + ": the CENTROID OF ITS " + declared
             + " DECLARED CHILD UNIT(S) (" + readable + " of " + declared + " reflected), "
             + lat + "," + lon + ". " + agreement + " " + roster + " " + Provenance(o.Declared, children);
    }

    /// <summary>Which uuid each child was read at. All-proven is the ordinary case and says so in
    /// one clause; anything else is counted and explained, because a child read through the name
    /// registry alone was read at whatever that name currently resolves to.</summary>
    public static string Provenance(int declared, IReadOnlyList<Child> children)
    {
        int proven = 0;
        if (children != null)
            for (int i = 0; i < children.Count; i++) if (children[i].ReflectionProven) proven++;
        return proven == declared
            ? "Every child was read at the uuid whose reflection released it for tasking, so none of "
              + "them can be a deleted shell."
            : (declared - proven).ToString(CultureInfo.InvariantCulture) + " of them were read at the "
              + "uuid the name registry currently resolves, not at a reflection-proven one (a child "
              + "that was never re-created has no window to be wrong in; one released by the "
              + "reflection timeout has its own warning above). A shell and its replacement share one "
              + "de-stacked placement, so the centroid is the same point either way.";
    }

    /// <summary>The roster the centroid can be re-derived from, in the app's own log (SF-3).</summary>
    public static string DescribeChildren(IReadOnlyList<Child> children)
    {
        if (children == null || children.Count == 0) return "Children: [].";
        var parts = new List<string>(children.Count);
        for (int i = 0; i < children.Count; i++) parts.Add(DescribeChild(children[i]));
        return "Children: [" + string.Join("; ", parts) + "].";
    }

    /// <summary>Formats one child for the roster. Invariant culture, ASCII.</summary>
    public static string DescribeChild(Child c)
    {
        if (!c.Readable) return c.Name + " NOT READABLE";
        string where = c.ReflectionProven ? "reflection-proven uuid" : "name-registry uuid";
        if (!IsUsableCoordinate(c))
            return c.Name + " UNUSABLE COORDINATE ("
                 + c.LatDeg.ToString("R", CultureInfo.InvariantCulture) + ","
                 + c.LonDeg.ToString("R", CultureInfo.InvariantCulture) + ", " + where + ")";
        return c.Name + " " + c.LatDeg.ToString("F6", CultureInfo.InvariantCulture) + ","
             + c.LonDeg.ToString("F6", CultureInfo.InvariantCulture) + " ("
             + where + ")";
    }
}
