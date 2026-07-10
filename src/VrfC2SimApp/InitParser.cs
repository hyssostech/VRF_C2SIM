using System.Collections;
using System.Globalization;
using System.Reflection;
using C2SIM;
using S = C2SIM.Schema102; // SISO-STD-C2SIM 1.0.2 (CWIX2024) generated types

namespace VrfC2SimApp;

/// <summary>
/// Parses a C2SIM Initialization message into <see cref="InitData"/> by DESERIALIZING
/// into the SDK's XSD-generated schema types (C2SIM.Schema102 via ToC2SIMObject) rather
/// than hand-navigating element names - so the parse follows the C2SIM schema, not the
/// shape of any one sample. Units/ForceSides/TacticalAreas are collected from the typed
/// object graph (robust to how the schema nests them), then read via typed properties.
///
/// The DOMAIN behavior mirrors the C++ interface (C2SIMinterface.cpp / C2SIMxmlHandler):
///   - Hostility: the FIRST ForceSide is "blue"; its units are "FR", others take blue's
///     ForceSideRelation HostilityStatusCode for that side.
///   - SystemName: SystemEntityList maps ActorReference UUIDs -> SystemName (the interface
///     only creates units whose SystemName matches its clientId).
///   - Missing lat/lon inherits the SUPERIOR unit's coordinates (order-dependent cascade).
///
/// Validated offline against the golden-trace STP init (`--parse-init`): 80 units,
/// 49 creatable, 4 areas - matching the golden trace.
/// </summary>
public static class InitParser
{
    public static InitData Parse(string xml)
    {
        var data = new InitData();
        if (string.IsNullOrWhiteSpace(xml)) return data;

        // Root-robust: a pushed init FILE is <MessageBody>-rooted, but the SDK's live
        // InitializationReceived event (and JoinSession/QUERYINIT) deliver the BARE
        // <C2SIMInitializationBody>. Try the envelope first, then the bare body directly
        // (both C2SIMInitializationBodyType carries [XmlRoot], so it deserializes alone).
        S.C2SIMInitializationBodyType init = null;
        try { init = C2SIMSDK.ToC2SIMObject<S.MessageBodyType>(xml)?.Item as S.C2SIMInitializationBodyType; }
        catch { /* not MessageBody-rooted */ }
        if (init == null)
        {
            try { init = C2SIMSDK.ToC2SIMObject<S.C2SIMInitializationBodyType>(xml); }
            catch { return data; }
        }
        if (init == null) return data;

        // SystemName: SystemEntityList (ActorReference UUIDs -> SystemName).
        var systemNameByUuid = new Dictionary<string, string>();
        foreach (var sel in init.SystemEntityList ?? Array.Empty<S.SystemEntityListType>())
        {
            string sys = (sel.SystemName ?? "").Trim();
            if (sys.Length > 0) data.SystemName = sys;
            foreach (var ar in sel.ActorReference ?? Array.Empty<string>())
                if (!string.IsNullOrWhiteSpace(ar)) systemNameByUuid[ar.Trim()] = sys;
        }

        // Collect typed Units / ForceSides / TacticalAreas from the graph.
        var units = new List<S.UnitType>();
        var forceSides = new List<S.ForceSideType>();
        var areas = new List<S.TacticalAreaType>();
        Walk(init, new HashSet<object>(ReferenceEqualityComparer.Instance), node =>
        {
            switch (node)
            {
                case S.UnitType u: units.Add(u); break;
                case S.ForceSideType f: forceSides.Add(f); break;
                case S.TacticalAreaType a: areas.Add(a); break;
            }
        });

        // Blue = first ForceSide. hostility(side) = FR if blue, else blue's relation code.
        string blueUuid = "";
        var blueRelations = new Dictionary<string, string>();
        if (forceSides.Count > 0)
        {
            blueUuid = (forceSides[0].UUID ?? "").Trim();
            foreach (var rel in forceSides[0].ForceSideRelation ?? Array.Empty<S.ForceSideRelationType>())
            {
                string other = (rel.OtherSide ?? "").Trim();
                if (other.Length > 0) blueRelations[other] = rel.HostilityStatusCode.ToString();
            }
        }
        string HostilityOf(string side) =>
            side == blueUuid ? "FR" : (blueRelations.TryGetValue(side, out var h) ? h : "");

        foreach (var u in units)
        {
            string uuid = (u.UUID ?? "").Trim();
            if (uuid.Length == 0) continue;
            var siso = u.SISOEntityType;
            var (lat, lon, elev) = LocationOf(u.CurrentState);
            data.Units.Add(new InitUnit
            {
                Name = (u.Name ?? "").Trim(),
                Uuid = uuid,
                SystemName = systemNameByUuid.TryGetValue(uuid, out var sn) ? sn : "",
                HostilityCode = HostilityOf((u.EntityDescriptor?.Side ?? "").Trim()),
                Latitude = lat,
                Longitude = lon,
                ElevationAgl = elev,
                SymbolId = (u.APP6CSymbol?.APP6CSIDC ?? "").Trim(),
                DisEntityType = DisTypeString(siso),
                DisDomain = siso?.DISDomain ?? 0,
                DirectionPhi = "",
                SuperiorUuid = (u.EntityDescriptor?.Superior ?? "").Trim()
            });
        }

        // Stable order matching the C++ std::map<uuid, Unit*> iteration.
        data.Units.Sort((a, b) => string.CompareOrdinal(a.Uuid, b.Uuid));

        // Missing lat/lon -> inherit the SUPERIOR unit's coordinates (order-dependent
        // multi-level cascade; C2SIMinterface.cpp:1421-1441). Single forward pass over
        // the UUID-sorted list, reading the superior's CURRENT (possibly-inherited) coords.
        var idxByUuid = new Dictionary<string, int>();
        for (int i = 0; i < data.Units.Count; i++) idxByUuid[data.Units[i].Uuid] = i;
        for (int i = 0; i < data.Units.Count; i++)
        {
            var u = data.Units[i];
            if (u.Latitude.Length != 0 && u.Longitude.Length != 0) continue;
            if (u.SuperiorUuid.Length == 0 || !idxByUuid.TryGetValue(u.SuperiorUuid, out int si)) continue;
            var sup = data.Units[si];
            if (sup.Latitude.Length != 0 && sup.Longitude.Length != 0)
                data.Units[i] = u with { Latitude = sup.Latitude, Longitude = sup.Longitude, ElevationAgl = sup.ElevationAgl };
        }

        foreach (var a in areas)
        {
            var area = new InitArea { Name = (a.Name ?? "").Trim(), Uuid = (a.UUID ?? "").Trim() };
            foreach (var g in AllGeodetics(a.CurrentState))
                area.Points.Add((g.Latitude, g.Longitude, ElevD(g)));
            data.Areas.Add(area);
        }

        return data;
    }

    // ---- typed navigation helpers -----------------------------------------

    private static (string lat, string lon, string elev) LocationOf(S.EntityStateType state)
    {
        var g = AllGeodetics(state).FirstOrDefault();
        if (g == null) return ("", "", "");
        return (Str(g.Latitude), Str(g.Longitude), Elevation(g));
    }

    private static IEnumerable<S.GeodeticCoordinateType> AllGeodetics(S.EntityStateType state)
    {
        var ps = state?.Item as S.PhysicalStateType;
        foreach (var loc in ps?.Location ?? Array.Empty<S.LocationType>())
            if (loc?.Item is S.GeodeticCoordinateType g)
                yield return g;
    }

    private static string Elevation(S.GeodeticCoordinateType g)
        => g.AltitudeAGLSpecified ? Str(g.AltitudeAGL)
         : g.AltitudeMSLSpecified ? Str(g.AltitudeMSL) : "";

    private static double ElevD(S.GeodeticCoordinateType g)
        => g.AltitudeAGLSpecified ? g.AltitudeAGL : (g.AltitudeMSLSpecified ? g.AltitudeMSL : 0.0);

    // DIS fields are sbyte in the schema (country is a string that exceeds sbyte range).
    private static string DisTypeString(S.SISOEntityTypeType t)
        => t == null ? ""
         : $"{(int)t.DISKind}.{(int)t.DISDomain}.{(t.DISCountry ?? "0").Trim()}.{(int)t.DISCategory}." +
           $"{(int)t.DISSubCategory}.{(int)t.DISSpecific}.{(int)t.DISExtra}";

    private static string Str(double d) => d.ToString("R", CultureInfo.InvariantCulture);

    // Reflective depth-first walk of the deserialized C2SIM graph, visiting every node.
    // Robust to how the schema nests Units/ForceSides/TacticalAreas across versions.
    private static void Walk(object node, HashSet<object> seen, Action<object> visit)
    {
        if (node == null) return;
        var t = node.GetType();
        if (t.IsPrimitive || t.IsEnum || node is string || node is decimal || node is DateTime) return;
        if (!seen.Add(node)) return;
        visit(node);
        if (node is IEnumerable en)
        {
            foreach (var item in en) Walk(item, seen, visit);
            return;
        }
        if (t.Namespace == null || !t.Namespace.StartsWith("C2SIM")) return;
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead || p.GetIndexParameters().Length > 0) continue;
            object val;
            try { val = p.GetValue(node); } catch { continue; }
            Walk(val, seen, visit);
        }
    }
}
