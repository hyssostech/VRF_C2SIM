namespace VrfC2SimApp;

/// <summary>
/// Parses a C2SIM Initialization message into <see cref="InitData"/>. This is the
/// .NET port of the relevant parts of the C++ C2SIMxmlHandler init parse.
///
/// STATUS: STUB (returns empty). Implementing this is the next parity slice.
///
/// Element map for the C2SIM init (from the STP golden-trace init structure):
///   - Top: C2SIMInitializationBody / ObjectDefinitions ; SystemName (scenario-wide).
///   - Unit: MilitaryOrganization/Unit -> Name, UUID; an ActorReference links it to
///     an Entity (ActorEntity/CollectiveEntity). EchelonCode on the organization.
///   - Entity: SISOEntityType -> DISKind/DISDomain/DISCountry/DISCategory/
///     DISSubCategory/DISSpecific/DISExtra (compose the "k.d.c.cat.sub.spec.extra"
///     disEntityType string; DisDomain is the int). APP6C/APP6CSymbol -> SymbolId (SIDC).
///     Location/GeodeticCoordinate -> Latitude/Longitude (+ elevation if present).
///   - Hostility: ForceSide / ForceSideRelation / HostilityStatusCode -> HostilityCode
///     ("HO" hostile). Side links a unit to its ForceSide.
///   - Areas: TacticalArea (+ MapGraphic/Line/Location points) -> InitArea.
///   - Routes arrive here too (Route/Line) OR via ObjectInitialization (SDK) - the C++
///     handled both; port routes in the OnOrder/ObjectInitialization slice.
///
/// Parity notes to preserve when implementing:
///   - Iterate units in a STABLE order (C++ used a std::map keyed by UUID) so the
///     create stream matches the golden trace.
///   - Only units whose SystemName == clientId are created (handled in the service).
///   - Missing lat/lon falls back to the SUPERIOR unit's coordinates (needs the
///     superior/subordinate relation - CommandRelation/Superior/Subordinate).
///   - Prefer System.Xml.Linq (XDocument) or the SDK's ToC2SIMObject&lt;T&gt; schema
///     types; strip namespaces as needed.
/// </summary>
public static class InitParser
{
    public static InitData Parse(string xml)
    {
        // TODO(parity, Phase 4): implement per the element map above.
        return new InitData();
    }
}
