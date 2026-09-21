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
        // 2.0 s since the re-create: D9's own dispatch was order+2.40 s, inside the window.
        string line = P.Line("114.MechCoy~PXY", o, children, 2.0);
        Console.WriteLine("        line: " + line);
        Check("the line names the unit, says CENTROID, gives the count and calls the gap by its name",
              line != null
              && line.Contains("ROUTE ORIGIN for composed parent 114.MechCoy~PXY", StringComparison.Ordinal)
              && line.Contains("CENTROID OF ITS 3 DECLARED CHILD UNIT(S) (3 of 3 reflected)", StringComparison.Ordinal)
              && line.Contains("RE-COMPOSE TRANSIENT", StringComparison.Ordinal)
              && line.Contains("50.4 m", StringComparison.Ordinal));

        Console.WriteLine("--- 3-SF1. THE TRANSIENT IS A DIAGNOSIS AND IT NEEDS EVIDENCE");
        // D8 sec 2.4 measured 16-25 m between a MOVING company's published position and its direct
        // children's centroid with NO re-compose in flight. The old line called any gap over 10 m a
        // re-compose transient, so a second task would have printed a diagnosis of something that
        // did not happen - and a harvest greps that string as fact.
        Check("re-create 2.0 s ago (inside the 30 s window) -> the transient IS claimed, with the age",
              line.Contains("re-created 2.0 s ago", StringComparison.Ordinal)
              && line.Contains("RE-COMPOSE TRANSIENT", StringComparison.Ordinal));
        string moving = P.Line("114.MechCoy~PXY", o, children, 120.0);
        Console.WriteLine("        line (no recent re-create): " + moving);
        Check("THE SAME 50.4 m GAP, re-created 120 s ago -> NO TRANSIENT IS CLAIMED, and the line says " +
              "what such a gap IS consistent with (D8's 16-25 m on a moving company)",
              !moving.Contains("RE-COMPOSE TRANSIENT", StringComparison.Ordinal)
              && moving.Contains("NO TRANSIENT IS CLAIMED", StringComparison.Ordinal)
              && moving.Contains("consistent with a unit under way", StringComparison.Ordinal)
              && moving.Contains("16-25 m", StringComparison.Ordinal));
        // NOTE the trap this check was written to avoid: the transient arm's prose CITES D9's
        // "50.4 m of origin error", so asserting the literal "50.4 m" would pass on the citation
        // and say nothing about the MEASURED value. Assert the measured number, formatted as the
        // line formats it (50.5 m here - the reconstructed ring is 0.07 m off D9's printed 50.4).
        Check($"... and it still prints the MEASURED gap ({F(o.PublishedOffsetMeters, 1)} m), which is the " +
              "fact, not the diagnosis",
              moving.Contains("differs by " + F(o.PublishedOffsetMeters, 1) + " m", StringComparison.Ordinal)
              && line.Contains("differs by " + F(o.PublishedOffsetMeters, 1) + " m", StringComparison.Ordinal));
        string noStamp = P.Line("114.MechCoy~PXY", o, children, double.NaN);
        Check("no re-create on record at all -> also no transient, and it says there is no record",
              !noStamp.Contains("RE-COMPOSE TRANSIENT", StringComparison.Ordinal)
              && noStamp.Contains("no re-creation of this parent's declared children is on record",
                                  StringComparison.Ordinal));
        Check($"the window is {F(P.RecomposeRecentSeconds, 0)} s, and it is a boundary not a cliff: 29.9 s " +
              "claims the transient, 30.1 s does not",
              P.Line("x", o, children, 29.9).Contains("RE-COMPOSE TRANSIENT", StringComparison.Ordinal)
              && !P.Line("x", o, children, 30.1).Contains("RE-COMPOSE TRANSIENT", StringComparison.Ordinal));
        Check("the ORIGIN is identical on all three wordings - evidence changes the sentence, never the " +
              "answer", o.LatDeg == P.Decide(TransientLat, TransientLon, 1275.0, children).LatDeg);

        Console.WriteLine("--- 3-SF3. THE CHILD ROSTER IS IN THE LINE, SO THE CENTROID IS RE-DERIVABLE FROM IT");
        foreach (var c in children)
            Check($"{c.Name} appears with its coordinate to 6 dp and its uuid provenance",
                  line.Contains(c.Name + " " + c.LatDeg.ToString("F6", CultureInfo.InvariantCulture) + ","
                                + c.LonDeg.ToString("F6", CultureInfo.InvariantCulture)
                                + " (reflection-proven uuid)", StringComparison.Ordinal));
        Check("the roster is labelled and bracketed so a parser can find it",
              line.Contains("Children: [", StringComparison.Ordinal));

        Console.WriteLine("--- 3a. WHICH UUID 'THE CHILD' MEANS IS SAID, NOT ASSUMED (the rebind window)");
        Check("all three read at a reflection-proven uuid -> the line says none can be a deleted shell",
              line.Contains("none of them can be a deleted shell", StringComparison.Ordinal));
        var viaRegistry = new List<P.Child>(children);
        viaRegistry[2] = viaRegistry[2] with { ReflectionProven = false };
        var oReg = P.Decide(TransientLat, TransientLon, 1275.0, viaRegistry);
        string regLine = P.Line("114.MechCoy~PXY", oReg, viaRegistry, 2.0);
        Check("... and the roster marks THAT child 'name-registry uuid' while the others stay " +
              "'reflection-proven uuid'",
              regLine.Contains("1143.MechPlt 34.649443,-116.693388 (name-registry uuid)", StringComparison.Ordinal)
              && regLine.Contains("1141.MechPlt 34.646723,-116.691479 (reflection-proven uuid)",
                                  StringComparison.Ordinal));
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
        string restLine = P.Line("114.MechCoy~PXY", oRest, children, 2.0);
        Check("the at-rest line reports the measured gap and does NOT claim a transient - even with a " +
              "re-create 2.0 s ago, because there is nothing to diagnose",
              restLine.Contains("differs by 0.0 m", StringComparison.Ordinal)
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
        string partLine = P.Line("114.MechCoy~PXY", oPart, partial, 2.0);
        Console.WriteLine("        line: " + partLine);
        Check("the fallback line names the unreadable child, says NO PARTIAL CENTROID, and states the exposure",
              partLine.Contains("FALLING BACK", StringComparison.Ordinal)
              && partLine.Contains("not usable: [1141.MechPlt]", StringComparison.Ordinal)
              && partLine.Contains("NO PARTIAL CENTROID IS COMPUTED", StringComparison.Ordinal)
              && partLine.Contains("ring radius", StringComparison.Ordinal));
        // SF-2: the old line said "a child that never reflected has already been reported above".
        // D5c proved a dispatch can precede the re-creates entirely, and the readiness classification
        // gates on the PARENT, so that warning may not exist. The line must stand on its own.
        Check("SF-2: the line does NOT point at a warning that may not exist, and says why",
              !partLine.Contains("already been reported above", StringComparison.Ordinal)
              && partLine.Contains("THIS LINE IS THE REPORT", StringComparison.Ordinal)
              && partLine.Contains("gates on the PARENT", StringComparison.Ordinal));
        Check("SF-2/SF-3: it carries the full per-child roster, so the state of every child is in the " +
              "line itself",
              partLine.Contains("1141.MechPlt NOT READABLE", StringComparison.Ordinal)
              && partLine.Contains("1142.MechPlt 34.646723,-116.695296", StringComparison.Ordinal)
              && partLine.Contains("1143.MechPlt 34.649443,-116.693388", StringComparison.Ordinal));
        // The point of refusing a partial centroid, in numbers: averaging the two readable children
        // would land on the R/2 transient state itself.
        var twoOnly = new List<P.Child> { children[1], children[2] };
        var oTwo = P.Decide(TransientLat, TransientLon, 1275.0, twoOnly);
        Check($"WHY ALL-OR-NOTHING: a centroid of only 2 of the 3 children is " +
              $"{F(Gc(AuthoredLat, AuthoredLon, oTwo.LatDeg, oTwo.LonDeg), 1)} m out - i.e. R/2, which is " +
              "EXACTLY the transient state D9 saw at order-0.20 s",
              Math.Abs(Gc(AuthoredLat, AuthoredLon, oTwo.LatDeg, oTwo.LonDeg) - RingRadiusM / 2.0) < 2.0);

        Console.WriteLine("--- 5b. SF-4: AN IMPLAUSIBLE CHILD IS REFUSED, NOT AVERAGED");
        // FAIL-FIRST, stated as the measurement the guard exists to prevent: without it, ONE child
        // driven away drags the origin by (its displacement / N) - and the STP-833 extent check is
        // anchored on that same poisoned point, so it cannot catch it either.
        var strayPos = AtBearing(AuthoredLat, AuthoredLon, 9000.0, 90.0);
        var stray = new List<P.Child>(children);
        stray[1] = stray[1] with { LatDeg = strayPos.Lat, LonDeg = strayPos.Lon };
        double unguarded;
        {
            // what an UNGUARDED mean would have produced, computed here so the cost is a number
            double n = 0.0, e = 0.0;
            double mLon = P.MetersPerDegLat * Math.Cos(AuthoredLat * Math.PI / 180.0);
            foreach (var c in stray)
            {
                n += (c.LatDeg - AuthoredLat) * P.MetersPerDegLat;
                e += (c.LonDeg - AuthoredLon) * mLon;
            }
            unguarded = Math.Sqrt(n * n + e * e) / 3.0;
        }
        Check($"THE DEFECT THIS PREVENTS: one child 9,000 m away would move the origin {F(unguarded, 0)} m " +
              "(displacement / N) and poison the STP-833 anchor with it", unguarded > 2500.0);
        var oStray = P.Decide(TransientLat, TransientLon, 1275.0, stray);
        Check("source = ChildOutlier; the origin is the published position BIT-FOR-BIT",
              oStray.From == P.Source.ChildOutlier
              && oStray.LatDeg == TransientLat && oStray.LonDeg == TransientLon
              && oStray.AltMeters == 1275.0);
        Check($"the outlier is NAMED ({oStray.OutlierName}) with its distance {F(oStray.OutlierMeters, 0)} m " +
              $"against the {F(oStray.BoundMeters, 0)} m bound - the ABSOLUTE net fires first here",
              oStray.OutlierName == "1142.MechPlt" && oStray.OutlierMeters > 8000.0
              && Math.Abs(oStray.BoundMeters - P.MaxChildFromPublishedMeters) < 1e-9);
        string strayLine = P.Line("114.MechCoy~PXY", oStray, stray, 2.0);
        Console.WriteLine("        line: " + strayLine);
        Check("the line falls back LOUDLY, names the outlier and its distance, and names the realistic cause",
              strayLine.Contains("FALLING BACK", StringComparison.Ordinal)
              && strayLine.Contains("declared child 1142.MechPlt is", StringComparison.Ordinal)
              && strayLine.Contains("plausibility bound", StringComparison.Ordinal)
              && strayLine.Contains("TASKED", StringComparison.Ordinal)
              && strayLine.Contains("Children: [", StringComparison.Ordinal));
        // A child at 0,0 - created but never positioned.
        var atNull = new List<P.Child>(children);
        atNull[0] = atNull[0] with { LatDeg = 0.0, LonDeg = 0.0 };
        var oNull = P.Decide(TransientLat, TransientLon, 1275.0, atNull);
        Check("a child at 0,0 (created but never positioned) is refused too",
              oNull.From == P.Source.ChildOutlier && oNull.OutlierName == "1141.MechPlt");
        // A non-finite coordinate is not even arithmetic - it is treated as unreadable.
        var nan = new List<P.Child>(children);
        nan[2] = nan[2] with { LatDeg = double.NaN };
        Check("a NON-FINITE coordinate is classified UNREADABLE, never averaged and never NaN-propagated",
              P.Decide(TransientLat, TransientLon, 1275.0, nan).From == P.Source.ChildrenUnreadable
              && !P.IsUsableCoordinate(nan[2]));
        // THE GUARD MUST NOT FIRE ON ANYTHING LEGITIMATE.
        Check("the healthy D9 ring PASSES the bound (it must not fire on the fixture it was built for)",
              o.From == P.Source.ChildrenCentroid);
        Check("the MOVED fleet passes", oMoved.From == P.Source.ChildrenCentroid);
        // D8 sec 2.5's worst observed mid-move stretch: separations 35-486 m against 348-349 m at
        // rest, i.e. about 280 m from the centre on a 202 m ring. Reproduce that spread and require
        // the guard to stay silent.
        var stretched = new List<P.Child>
        {
            children[0] with { LatDeg = AtBearing(AuthoredLat, AuthoredLon, 280.0, 120.0).Lat,
                               LonDeg = AtBearing(AuthoredLat, AuthoredLon, 280.0, 120.0).Lon },
            children[1] with { LatDeg = AtBearing(AuthoredLat, AuthoredLon, 280.0, 240.0).Lat,
                               LonDeg = AtBearing(AuthoredLat, AuthoredLon, 280.0, 240.0).Lon },
            children[2] with { LatDeg = AtBearing(AuthoredLat, AuthoredLon, 280.0, 0.0).Lat,
                               LonDeg = AtBearing(AuthoredLat, AuthoredLon, 280.0, 0.0).Lon },
        };
        Check("D8's WORST observed mid-move stretch (486 m separations, ~280 m radius) passes - the " +
              "guard cannot fire on a legitimately spread formation",
              P.Decide(TransientLat, TransientLon, 1275.0, stretched).From == P.Source.ChildrenCentroid);
        Check($"the floor is {F(P.OutlierFloorMeters, 0)} m, above the longest shipped GROUND formation " +
              "span (660 m, RUNBOOK 11e), so a unit's own members can never trip it",
              P.OutlierFloorMeters > 660.0);
        Check($"K = {F(P.OutlierFactor, 0)}: on the 202.1 m ring the bound is the floor, and on a ring big " +
              "enough for K to bind (806.7 m, R9 full at N=7) it is " +
              $"{F(P.OutlierFactor * 806.7, 0)} m - about 3x D8's worst stretch",
              Math.Max(P.OutlierFactor * RingRadiusM, P.OutlierFloorMeters) == P.OutlierFloorMeters
              && P.OutlierFactor * 806.7 > 3.0 * 280.0);
        // TWO CHILDREN: the RELATIVE net cannot run (no majority to say which of two is the stray),
        // so the absolute net is the whole guard. This is a declared limit, asserted so it cannot
        // change silently.
        var twoFar = new List<P.Child>
        {
            children[0] with { LatDeg = AtBearing(AuthoredLat, AuthoredLon, 1500.0, 0.0).Lat,
                               LonDeg = AtBearing(AuthoredLat, AuthoredLon, 1500.0, 0.0).Lon },
            children[1] with { LatDeg = AtBearing(AuthoredLat, AuthoredLon, 1500.0, 180.0).Lat,
                               LonDeg = AtBearing(AuthoredLat, AuthoredLon, 1500.0, 180.0).Lon },
        };
        Check("N=2: a 3,000 m-separated pair is ACCEPTED - the relative net needs a majority and does " +
              "not run, and both children are inside the absolute net",
              P.Decide(AuthoredLat, AuthoredLon, 1275.0, twoFar).From == P.Source.ChildrenCentroid);
        var twoStray = new List<P.Child>
        {
            children[0],
            children[1] with { LatDeg = strayPos.Lat, LonDeg = strayPos.Lon },
        };
        Check("N=2: but a child 9,000 m out is still caught, by the absolute net",
              P.Decide(TransientLat, TransientLon, 1275.0, twoStray).From == P.Source.ChildOutlier);

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
