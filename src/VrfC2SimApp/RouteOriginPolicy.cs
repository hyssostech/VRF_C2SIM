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
/// a 569 m traversal bar (556) and a 285 m arrival radius (278). D8's "0.0 m, N4 FIXED" was the
/// SAME code winning the same race by about 0.5 s. Nothing in the dispatch path arbitrates it.
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
/// ALL OR NOTHING. If any declared child cannot be read, the published position is used and SAID
/// OUT LOUD. A PARTIAL centroid is precisely what the transient IS (two of three children give
/// R/2 at 300 deg), so computing one would reproduce the defect while claiming to fix it.
///
/// PURE. No bridge call, no logging, no state, no clock. The service reads the children through
/// the same NameRegistry + TryGetEntityGeodetic pair that released them for tasking, and hands
/// the numbers here. Locked by --routeorigin-selftest, whose fixture is D9's own trace.
/// </summary>
public static class RouteOriginPolicy
{
    /// <summary>The DeStacker's own flat-earth constant, so a centroid computed here and a ring
    /// laid out there cannot disagree by a metre of datum (DeStacker.MetersPerDegLat).</summary>
    public const double MetersPerDegLat = 111_320.0;

    /// <summary>Above this many metres the gap between the children's centroid and the parent's
    /// published position is called by its name - the re-compose transient - rather than reported
    /// as agreement. It is a WORDING threshold only: the origin used is the same either way.
    /// 10 m is an order of magnitude below D9's smallest observed transient state (34.2 m) and an
    /// order of magnitude above D8's settled agreement at rest (0.0 m to printed precision).</summary>
    public const double TransientCallMeters = 10.0;

    public enum Source
    {
        /// <summary>Not a composed parent (a platform, an independent aggregate, a parent with
        /// fewer than two declared children, or ComposeHierarchy off): the published read is used
        /// UNCHANGED and nothing is logged.</summary>
        NotComposed,
        /// <summary>Every declared child was readable; the origin is their centroid.</summary>
        ChildrenCentroid,
        /// <summary>At least one declared child could not be read; the published position stands
        /// in and the exposure is stated.</summary>
        ChildrenUnreadable,
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
    public readonly record struct Origin(Source From, double LatDeg, double LonDeg, double AltMeters,
                                         int Readable, int Declared, double PublishedOffsetMeters);

    /// <summary>
    /// THE DECISION. <paramref name="children"/> is null or short for every taskee that is not a
    /// composed parent, and those arms return the published triple BIT-FOR-BIT - no arithmetic
    /// touches it, so an independent taskee's route is byte-for-byte what it was before N13.
    /// </summary>
    public static Origin Decide(double publishedLatDeg, double publishedLonDeg, double publishedAltMeters,
                                IReadOnlyList<Child> children)
    {
        int declared = children?.Count ?? 0;
        if (declared < 2)
            return new Origin(Source.NotComposed, publishedLatDeg, publishedLonDeg, publishedAltMeters,
                              0, declared, 0.0);

        int readable = 0;
        for (int i = 0; i < declared; i++) if (children[i].Readable) readable++;
        if (readable != declared)
            return new Origin(Source.ChildrenUnreadable, publishedLatDeg, publishedLonDeg, publishedAltMeters,
                              readable, declared, 0.0);

        // Tangent plane about the PUBLISHED position. The children sit within a ring radius of it
        // (202.1 m on R9 lean, 806.7 m worst case on any shipped fixture), so a flat mean is exact
        // to well under a metre, and anchoring on a point rather than averaging raw degrees is what
        // makes the antimeridian and the cos(lat) scaling safe.
        double metersPerDegLon = MetersPerDegLat
                                 * Math.Max(Math.Cos(publishedLatDeg * Math.PI / 180.0), 0.01);
        double north = 0.0, east = 0.0, alt = 0.0;
        for (int i = 0; i < declared; i++)
        {
            var c = children[i];
            north += (c.LatDeg - publishedLatDeg) * MetersPerDegLat;
            east += NormalizeLonDeltaDeg(c.LonDeg - publishedLonDeg) * metersPerDegLon;
            alt += c.AltMeters;
        }
        north /= declared;
        east /= declared;
        alt /= declared;
        double lat = publishedLatDeg + north / MetersPerDegLat;
        double lon = NormalizeLonDeg(publishedLonDeg + east / metersPerDegLon);
        return new Origin(Source.ChildrenCentroid, lat, lon, alt, readable, declared,
                          Math.Sqrt(north * north + east * east));
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
    /// say about a taskee this policy did not touch - and a full sentence for both of the arms
    /// that DID decide something, including the honest one.
    /// </summary>
    public static string Line(string unitName, Origin o, IReadOnlyList<Child> children)
    {
        if (o.From == Source.NotComposed) return null;
        var inv = CultureInfo.InvariantCulture;
        string lat = o.LatDeg.ToString("F6", inv), lon = o.LonDeg.ToString("F6", inv);
        string readable = o.Readable.ToString(inv), declared = o.Declared.ToString(inv);
        if (o.From == Source.ChildrenUnreadable)
        {
            var missing = new List<string>();
            if (children != null)
                for (int i = 0; i < children.Count; i++)
                    if (!children[i].Readable) missing.Add(children[i].Name);
            return "ROUTE ORIGIN for composed parent " + unitName + ": FALLING BACK to the parent's OWN "
                 + "PUBLISHED position (" + lat + "," + lon + ") - only " + readable + " of " + declared
                 + " declared child unit(s) can be read right now (not readable: ["
                 + string.Join(", ", missing) + "]). NO PARTIAL CENTROID IS COMPUTED: a partial membership "
                 + "is exactly what the re-compose transient IS (N13/D9 - two of three children published "
                 + "R/2 at 300 deg), so averaging what is readable would reproduce the defect while "
                 + "claiming to fix it. THE EXPOSURE: while the children are being re-created VR-Forces "
                 + "publishes this parent at the centroid of whatever is attached, so this route may start "
                 + "up to the de-stack ring radius from where the unit really is, and the route length, the "
                 + "traversal bar and the arrival radius inherit that error. A child that never reflected "
                 + "has already been reported above.";
        }
        string gap = o.PublishedOffsetMeters.ToString("F1", inv);
        string agreement = o.PublishedOffsetMeters > TransientCallMeters
            ? "The parent's OWN published position is " + gap + " m away from it: VR-Forces publishes a "
            + "composed aggregate at the centroid of its DIRECT CHILDREN, and that centroid is still "
            + "sweeping through the RE-COMPOSE TRANSIENT left by the order-time delete/re-create (N13, run "
            + "D9 2026-09-21: 50.4 m of origin error lengthened a 1,112 m route to 1,139 m). The children's "
            + "own positions are the settled answer and they are readable NOW, so the route starts there "
            + "instead of waiting the transient out."
            : "The parent's own published position agrees to " + gap + " m, so no re-compose transient is "
            + "in progress; the centroid is used anyway because it is exact by construction (equal-bearing "
            + "ring, RUNBOOK 11e) and does not depend on winning a race.";
        // Which uuid each child was read at. All-proven is the ordinary case and says so in one
        // clause; anything else is named, because a child read through the name registry alone was
        // read at whatever that name currently resolves to.
        int proven = 0;
        if (children != null)
            for (int i = 0; i < children.Count; i++) if (children[i].ReflectionProven) proven++;
        string provenance = proven == o.Declared
            ? " Every child was read at the uuid whose reflection released it for tasking, so none of " +
              "them can be a deleted shell."
            : " " + (o.Declared - proven).ToString(inv) + " of them were read at the uuid the name "
              + "registry currently resolves, not at a reflection-proven one (a child that was never "
              + "re-created has no window to be wrong in; one released by the reflection timeout has "
              + "its own warning above). A shell and its replacement share one de-stacked placement, "
              + "so the centroid is the same point either way.";
        return "ROUTE ORIGIN for composed parent " + unitName + ": the CENTROID OF ITS " + declared
             + " DECLARED CHILD UNIT(S) (" + readable + " of " + declared + " reflected), "
             + lat + "," + lon + ". " + agreement + provenance;
    }

    /// <summary>Formats one child for a debug line. Invariant culture, ASCII.</summary>
    public static string DescribeChild(Child c) =>
        c.Readable
            ? c.Name + " " + c.LatDeg.ToString("F6", CultureInfo.InvariantCulture) + ","
                     + c.LonDeg.ToString("F6", CultureInfo.InvariantCulture)
            : c.Name + " (not readable)";
}
