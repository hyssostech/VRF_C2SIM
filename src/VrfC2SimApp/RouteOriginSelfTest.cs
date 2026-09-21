using System;
using System.Collections.Generic;
using System.Globalization;
using VrfC2Sim;
using P = VrfC2SimApp.RouteOriginPolicy;

namespace VrfC2SimApp;

/// <summary>
/// N13 + N15 offline suite (--routeorigin-selftest). No bridge, no network, no clock, no file.
///
/// THE FIXTURE IS RUN D9 ITSELF (runs/20260921T094051Z_run, harvest v6harvest/d9_harvest_report.md
/// sec 3.3 and 3.4), not invented geometry:
///   - the authored parent coordinate of 114.MechCoy~PXY, verbatim from the app's own DeStack line
///     and from the D8 terrain-profile reply that read it back;
///   - the THREE re-created platoon aggregates as the trace measured them at materialization -
///     201.8 m at 120.0 deg, 201.8 m at 240.0 deg, 201.9 m at 360.0 deg;
///   - the parent's PUBLISHED position at the instant T_R5_CO1 dispatched, 34.647403,-116.692910
///     (trace t=31.7, order+1.80 s), which is `#0` of the app's own `Terrain profile reply 18` to
///     six decimal places - i.e. the number the route was actually built from;
///   - the EARLIER transient state, 100.9 m at bearing 300.0 deg (trace t=29.7);
///   - T_R5_CO1's own two authored waypoints, so the route LENGTH consequence is re-derived here
///     with the same haversine MarkDispatched uses, and lands on D8's 1,112 m and D9's 1,139 m.
///
/// The FAIL-FIRST arm is explicit: sec 2 asserts that the PUBLISHED position - what the code used
/// before this change - is 50.4 m out and yields D9's wrong 1,139 m route, its 569 m bar and its
/// 285 m radius. That arm IS the defect, and it is what these checks would have caught.
/// </summary>
public static class RouteOriginSelfTest
{
    // --- D9 / D8 numbers -----------------------------------------------------------------------
    private const double AuthoredLat = 34.647628996814;
    private const double AuthoredLon = -116.693387536163;
    private const double RingRadiusM = 202.1;              // the app's own DeStack line
    // The parent's PUBLISHED position at the D9 dispatch (trace t=31.7 = order+1.80 s).
    private const double TransientLat = 34.647403;
    private const double TransientLon = -116.692910;
    // T_R5_CO1's authored waypoints (data/R9_Mojave_Lean_Order.xml; the same pair
    // RouteExtentSelfTest pins for the V6g positive control).
    private const double V1Lat = 34.652628996814, V1Lon = -116.693387536163;
    private const double V2Lat = 34.657628996814, V2Lon = -116.693387536163;

    private static (double Lat, double Lon) AtBearing(double fromLat, double fromLon,
                                                      double meters, double bearingDeg)
    {
        double a = bearingDeg * Math.PI / 180.0;
        double north = meters * Math.Cos(a), east = meters * Math.Sin(a);
        double mLon = P.MetersPerDegLat * Math.Max(Math.Cos(fromLat * Math.PI / 180.0), 0.01);
        return (fromLat + north / P.MetersPerDegLat, fromLon + east / mLon);
    }

    /// <summary>The three re-created platoons as D9's trace measured them at materialization.</summary>
    private static List<P.Child> D9Children()
    {
        var c1 = AtBearing(AuthoredLat, AuthoredLon, 201.8, 120.0);
        var c2 = AtBearing(AuthoredLat, AuthoredLon, 201.8, 240.0);
        var c3 = AtBearing(AuthoredLat, AuthoredLon, 201.9, 360.0);
        return new List<P.Child>
        {
            new("1141.MechPlt", true, c1.Lat, c1.Lon, 1260.0, true),
            new("1142.MechPlt", true, c2.Lat, c2.Lon, 1262.0, true),
            new("1143.MechPlt", true, c3.Lat, c3.Lon, 1258.0, true),
        };
    }

    private static double RouteLength(double originLat, double originLon) =>
        RouteExtentPolicy.PathLengthMeters(new[]
        {
            (originLat, originLon), (V1Lat, V1Lon), (V2Lat, V2Lon),
        });

    public static int Run()
    {
        int fails = 0;
        void Check(string what, bool cond)
        {
            Console.WriteLine((cond ? "  [PASS] " : "  [FAIL] ") + what);
            if (!cond) fails++;
        }
        static string F(double v, int d) => v.ToString("F" + d, CultureInfo.InvariantCulture);
        static double Gc(double a, double b, double c, double d) => RouteExtentPolicy.GreatCircleMeters(a, b, c, d);

        var children = D9Children();

        Console.WriteLine("--- 1. THE FIXTURE IS D9's OWN GEOMETRY");
        Check($"the parent's PUBLISHED position at the D9 dispatch is {F(Gc(AuthoredLat, AuthoredLon, TransientLat, TransientLon), 1)} m " +
              "from its authored coordinate (the harvest measured 50.4 m at bearing 119.9 deg)",
              Math.Abs(Gc(AuthoredLat, AuthoredLon, TransientLat, TransientLon) - 50.4) < 0.5);
        for (int i = 0; i < children.Count; i++)
            Check($"child {children[i].Name} sits {F(Gc(AuthoredLat, AuthoredLon, children[i].LatDeg, children[i].LonDeg), 1)} m " +
                  "from the authored coordinate (the ring is 202.1 m; the trace measured 201.8/201.8/201.9)",
                  Math.Abs(Gc(AuthoredLat, AuthoredLon, children[i].LatDeg, children[i].LonDeg) - RingRadiusM) < 1.0);

        Console.WriteLine("--- 2. FAIL-FIRST: THE PUBLISHED POSITION - WHAT THE CODE USED BEFORE THIS CHANGE");
        double badLen = RouteLength(TransientLat, TransientLon);
        double badRadius = ArrivalPolicy.RadiusFor(500.0, badLen);
        double badBar = Math.Max(0.5 * badLen, 100.0);
        Check($"a route built from the published position is {F(badLen, 0)} m - D9's own figure was 1,139 m " +
              "(D8, which won the same race by 0.5 s, got 1,112)", badLen > 1135.0 && badLen < 1143.0);
        Check($"... its arrival radius is {F(badRadius, 0)} m (D9 printed 285; D8 printed 278)",
              badRadius > 283.0 && badRadius < 287.0);
        Check($"... its traversal bar is {F(badBar, 0)} m (D9 printed 569; D8 printed 556)",
              badBar > 567.0 && badBar < 572.0);
        Check("THIS ARM IS THE DEFECT: the origin is more than 1 m from the authored coordinate",
              Gc(AuthoredLat, AuthoredLon, TransientLat, TransientLon) > 1.0);

        Console.WriteLine("--- 3. THE FIX: the children's centroid, replayed on D9's transient samples");
        var o = P.Decide(TransientLat, TransientLon, 1275.0, children);
        Check("source = ChildrenCentroid, 3 of 3 reflected",
              o.From == P.Source.ChildrenCentroid && o.Readable == 3 && o.Declared == 3);
        double err = Gc(AuthoredLat, AuthoredLon, o.LatDeg, o.LonDeg);
        Check($"the origin is {F(err, 3)} m from the AUTHORED coordinate - REGISTERED BOUND: within 1 m",
              err < 1.0);
        Check($"the line reports the published position {F(o.PublishedOffsetMeters, 1)} m away " +
              "(the harvest's 50.4 m)", Math.Abs(o.PublishedOffsetMeters - 50.4) < 0.5);
        double goodLen = RouteLength(o.LatDeg, o.LonDeg);
        Check($"the route is now {F(goodLen, 0)} m - back inside D3/D6/D8's 1,110-1,116 m band",
              goodLen >= 1110.0 && goodLen <= 1116.0);
        Check($"... its arrival radius is {F(ArrivalPolicy.RadiusFor(500.0, goodLen), 0)} m (D8's 278) and its " +
              $"traversal bar {F(Math.Max(0.5 * goodLen, 100.0), 0)} m (D8's 556)",
              Math.Abs(ArrivalPolicy.RadiusFor(500.0, goodLen) - 278.0) < 1.5
              && Math.Abs(Math.Max(0.5 * goodLen, 100.0) - 556.0) < 2.0);
        string line = P.Line("114.MechCoy~PXY", o, children);
        Console.WriteLine("        line: " + line);
        Check("the line names the unit, says CENTROID, gives the count and calls the gap by its name",
              line != null
              && line.Contains("ROUTE ORIGIN for composed parent 114.MechCoy~PXY", StringComparison.Ordinal)
              && line.Contains("CENTROID OF ITS 3 DECLARED CHILD UNIT(S) (3 of 3 reflected)", StringComparison.Ordinal)
              && line.Contains("RE-COMPOSE TRANSIENT", StringComparison.Ordinal)
              && line.Contains("50.4 m", StringComparison.Ordinal));

        Console.WriteLine("--- 3a. WHICH UUID 'THE CHILD' MEANS IS SAID, NOT ASSUMED (the rebind window)");
        Check("all three read at a reflection-proven uuid -> the line says none can be a deleted shell",
              line.Contains("none of them can be a deleted shell", StringComparison.Ordinal));
        var viaRegistry = new List<P.Child>(children);
        viaRegistry[2] = viaRegistry[2] with { ReflectionProven = false };
        var oReg = P.Decide(TransientLat, TransientLon, 1275.0, viaRegistry);
        string regLine = P.Line("114.MechCoy~PXY", oReg, viaRegistry);
        Check("one read through the NAME REGISTRY instead -> the line counts it and says why it is still " +
              "the same point (one de-stacked placement for a shell and its replacement)",
              regLine.Contains("1 of them were read at the uuid the name registry currently resolves",
                               StringComparison.Ordinal)
              && regLine.Contains("share one de-stacked placement", StringComparison.Ordinal));
        Check("... and the ORIGIN is unaffected - provenance is reported, it does not change the answer",
              oReg.LatDeg == o.LatDeg && oReg.LonDeg == o.LonDeg);

        Console.WriteLine("--- 3b. THE EARLIER TRANSIENT STATE (R/2 at 300 deg, trace t=29.7) IS ALSO CORRECTED");
        var half = AtBearing(AuthoredLat, AuthoredLon, 100.9, 300.0);
        var oHalf = P.Decide(half.Lat, half.Lon, 1275.0, children);
        Check($"published {F(Gc(AuthoredLat, AuthoredLon, half.Lat, half.Lon), 1)} m out (the harvest's 100.9 m), " +
              $"origin {F(Gc(AuthoredLat, AuthoredLon, oHalf.LatDeg, oHalf.LonDeg), 3)} m out",
              Gc(AuthoredLat, AuthoredLon, oHalf.LatDeg, oHalf.LonDeg) < 1.0);
        Check("the worst state D9 observed would have cost 100.9 m of origin error - the line says so",
              Math.Abs(oHalf.PublishedOffsetMeters - 100.9) < 1.0);

        Console.WriteLine("--- 3c. AT REST the centroid and the published position AGREE, and the wording changes");
        var oRest = P.Decide(AuthoredLat, AuthoredLon, 1275.0, children);
        Check($"published offset {F(oRest.PublishedOffsetMeters, 3)} m - under the {F(P.TransientCallMeters, 0)} m " +
              "threshold, so no transient is claimed", oRest.PublishedOffsetMeters < P.TransientCallMeters);
        string restLine = P.Line("114.MechCoy~PXY", oRest, children);
        Check("the at-rest line says AGREES and does NOT claim a transient",
              restLine.Contains("agrees to", StringComparison.Ordinal)
              && !restLine.Contains("RE-COMPOSE TRANSIENT", StringComparison.Ordinal));

        Console.WriteLine("--- 4. A MOVED UNIT MUST NOT SNAP BACK TO ITS AUTHORED COORDINATE");
        // This is the check that REFUSES option A ("for a unit at rest, just use the authored
        // coordinate"): the whole company has driven 800 m north and the origin must follow it.
        var moved = new List<P.Child>();
        foreach (var c in children)
        {
            var m = AtBearing(c.LatDeg, c.LonDeg, 800.0, 0.0);
            moved.Add(c with { LatDeg = m.Lat, LonDeg = m.Lon });
        }
        var movedPub = AtBearing(AuthoredLat, AuthoredLon, 800.0, 0.0);
        var oMoved = P.Decide(movedPub.Lat, movedPub.Lon, 1275.0, moved);
        double movedErr = Gc(AuthoredLat, AuthoredLon, oMoved.LatDeg, oMoved.LonDeg);
        Check($"the origin is {F(movedErr, 0)} m from the authored coordinate - it followed the fleet, it did " +
              "NOT snap back", movedErr > 700.0);
        Check("... and it lands on the moved fleet's own centroid, within 1 m",
              Gc(movedPub.Lat, movedPub.Lon, oMoved.LatDeg, oMoved.LonDeg) < 1.0);
        Check("... and it still reports agreement with the (also moved) published position",
              oMoved.From == P.Source.ChildrenCentroid && oMoved.PublishedOffsetMeters < 1.0);

        Console.WriteLine("--- 5. NOT ALL CHILDREN REFLECTED -> THE DOCUMENTED FALLBACK, NEVER A PARTIAL CENTROID");
        var partial = new List<P.Child>(children);
        partial[0] = new P.Child("1141.MechPlt", false, 0.0, 0.0, 0.0, false);
        var oPart = P.Decide(TransientLat, TransientLon, 1275.0, partial);
        Check("source = ChildrenUnreadable, 2 of 3",
              oPart.From == P.Source.ChildrenUnreadable && oPart.Readable == 2 && oPart.Declared == 3);
        Check("the origin is the published position BIT-FOR-BIT (no arithmetic touched it)",
              oPart.LatDeg == TransientLat && oPart.LonDeg == TransientLon && oPart.AltMeters == 1275.0);
        string partLine = P.Line("114.MechCoy~PXY", oPart, partial);
        Console.WriteLine("        line: " + partLine);
        Check("the fallback line names the unreadable child, says NO PARTIAL CENTROID, and states the exposure",
              partLine.Contains("FALLING BACK", StringComparison.Ordinal)
              && partLine.Contains("1141.MechPlt", StringComparison.Ordinal)
              && partLine.Contains("NO PARTIAL CENTROID IS COMPUTED", StringComparison.Ordinal)
              && partLine.Contains("ring radius", StringComparison.Ordinal));
        // The point of refusing a partial centroid, in numbers: averaging the two readable children
        // would land on the R/2 transient state itself.
        var twoOnly = new List<P.Child> { children[1], children[2] };
        var oTwo = P.Decide(TransientLat, TransientLon, 1275.0, twoOnly);
        Check($"WHY ALL-OR-NOTHING: a centroid of only 2 of the 3 children is " +
              $"{F(Gc(AuthoredLat, AuthoredLon, oTwo.LatDeg, oTwo.LonDeg), 1)} m out - i.e. R/2, which is " +
              "EXACTLY the transient state D9 saw at order-0.20 s",
              Math.Abs(Gc(AuthoredLat, AuthoredLon, oTwo.LatDeg, oTwo.LonDeg) - RingRadiusM / 2.0) < 2.0);

        Console.WriteLine("--- 6. INDEPENDENT TASKEES AND PLATFORMS ARE BYTE-FOR-BYTE UNCHANGED");
        foreach (var (what, kids) in new (string, IReadOnlyList<P.Child>)[]
                 {
                     ("a platform / an independent aggregate (no declared children: null)", null),
                     ("a taskee with an EMPTY declared-children list", new List<P.Child>()),
                     ("a composed parent with ONE declared child (a lone child is not spread)",
                      new List<P.Child> { children[0] }),
                 })
        {
            var oNone = P.Decide(TransientLat, TransientLon, 1275.0, kids);
            Check($"{what}: NotComposed, and lat/lon/alt are the published doubles EXACTLY",
                  oNone.From == P.Source.NotComposed
                  && oNone.LatDeg == TransientLat && oNone.LonDeg == TransientLon
                  && oNone.AltMeters == 1275.0 && oNone.PublishedOffsetMeters == 0.0);
            Check($"{what}: NOTHING is logged", P.Line("1.BdeHQ~PXY", oNone, kids) == null);
        }
        Check("an independent taskee's route is therefore identical to the pre-fix route, to the last bit",
              RouteLength(P.Decide(TransientLat, TransientLon, 1275.0, null).LatDeg,
                          P.Decide(TransientLat, TransientLon, 1275.0, null).LonDeg) == badLen);

        Console.WriteLine("--- 7. THE CENTROID ARITHMETIC ITSELF");
        Check("the mean ALTITUDE of the children is used (1260 + 1262 + 1258) / 3 = 1260",
              Math.Abs(o.AltMeters - 1260.0) < 1e-9);
        Check("an N-child equal-bearing ring has its centroid exactly on the anchor, for N = 2..12",
              CheckRingCentroids());
        Check("a longitude delta across the antimeridian is 0.02 deg, not 359.98",
              Math.Abs(P.NormalizeLonDeltaDeg(-179.99 - 179.99) - 0.02) < 1e-9);
        var wrap = new List<P.Child>
        {
            new("w1", true, 10.0, 179.99, 0.0),
            new("w2", true, 10.0, -179.99, 0.0),
        };
        var oWrap = P.Decide(10.0, 179.995, 0.0, wrap);
        Check($"... and a parent whose two children straddle it gets an origin at 180.000 deg, not 0 deg " +
              $"(got {F(Math.Abs(oWrap.LonDeg), 4)})", Math.Abs(Math.Abs(oWrap.LonDeg) - 180.0) < 1e-6);

        Console.WriteLine("--- 8. N15: ONE CENSUS FOR THE C13 LINE AND THE INIT CREATION BARRIER");
        // R9 lean's own final plan list: 1.BdeHQ~PXY is the single PLATFORM; 1222.MechPlt,
        // 1141/1142/1143.MechPlt are aggregates flipped to shells by the AtOrder policy; and
        // 114.MechCoy~PXY is a COMPOSED PARENT that ApplyHierarchyComposition had ALREADY set to
        // CreateSubordinates=false. That last one is what the old line called a "platform".
        var plans = new List<CreationPlan>
        {
            Plan("1.BdeHQ~PXY", aggregate: false, createSubs: true),
            Plan("114.MechCoy~PXY", aggregate: true, createSubs: false),   // composed parent, already a shell
            Plan("1141.MechPlt", aggregate: true, createSubs: false),
            Plan("1142.MechPlt", aggregate: true, createSubs: false),
            Plan("1143.MechPlt", aggregate: true, createSubs: false),
            Plan("1222.MechPlt", aggregate: true, createSubs: false),
        };
        var cen = CreationCensus.Of(plans);
        Check($"R9 lean: {cen.Total} planned, {cen.Aggregates} aggregates, {cen.EmptyShells} EMPTY SHELLS, " +
              $"{cen.Platforms} platform - the barrier line's own 5 and 1",
              cen.Total == 6 && cen.Aggregates == 5 && cen.EmptyShells == 5 && cen.Platforms == 1);
        Check("THE OLD ARITHMETIC IS THE DEFECT: `toCreate.Count - flipped` with flipped = 4 gives 2, " +
              "and this init has ONE platform", plans.Count - 4 == 2 && cen.Platforms == 1);
        Check("AggregatesWithMembers is 0 in AtOrder mode - every aggregate is a shell",
              cen.AggregatesWithMembers == 0);
        var mixed = new List<CreationPlan>
        {
            Plan("p1", aggregate: false, createSubs: true),
            Plan("p2", aggregate: false, createSubs: true),
            Plan("a1", aggregate: true, createSubs: true),
            Plan("a2", aggregate: true, createSubs: false),
        };
        var cenMixed = CreationCensus.Of(mixed);
        Check("a non-AtOrder init counts an aggregate that KEEPS its members as an aggregate, not a shell " +
              "and not a platform",
              cenMixed.Platforms == 2 && cenMixed.Aggregates == 2 && cenMixed.EmptyShells == 1
              && cenMixed.AggregatesWithMembers == 1);
        Check("an empty or null plan list is all zeroes",
              CreationCensus.Of(new List<CreationPlan>()).Total == 0 && CreationCensus.Of(null).Total == 0);

        Console.WriteLine(fails == 0 ? "routeorigin-selftest: ALL CHECKS PASSED"
                                     : $"routeorigin-selftest: {fails} FAILED");
        return fails == 0 ? 0 : 1;
    }

    private static CreationPlan Plan(string name, bool aggregate, bool createSubs) =>
        new(aggregate, default, Force.Friendly, 0.0, name,
            new Geodetic { LatDeg = AuthoredLat, LonDeg = AuthoredLon, AltMeters = 0.0 },
            null, TypeFidelity.Unspecified, "", "", "", "", createSubs);

    /// <summary>The property the whole fix rests on: N points at equal bearings 360/N apart and
    /// equal radius average to the anchor. Asserted directly, for N = 2..12 and two rotations.</summary>
    private static bool CheckRingCentroids()
    {
        for (int n = 2; n <= 12; n++)
            foreach (double rot in new[] { 0.0, 37.0 })
            {
                var kids = new List<P.Child>(n);
                for (int i = 0; i < n; i++)
                {
                    var (north, east) = DeStacker.EqualBearingOffset(i, n, RingRadiusM, rot);
                    double mLon = P.MetersPerDegLat * Math.Max(Math.Cos(AuthoredLat * Math.PI / 180.0), 0.01);
                    kids.Add(new P.Child("c" + i.ToString(CultureInfo.InvariantCulture), true,
                                         AuthoredLat + north / P.MetersPerDegLat,
                                         AuthoredLon + east / mLon, 0.0));
                }
                // Decide about a DELIBERATELY WRONG published position - the transient - so the
                // centroid cannot be inheriting the right answer from its own anchor.
                var d = P.Decide(TransientLat, TransientLon, 0.0, kids);
                if (RouteExtentPolicy.GreatCircleMeters(AuthoredLat, AuthoredLon, d.LatDeg, d.LonDeg) > 0.05)
                    return false;
            }
        return true;
    }
}
