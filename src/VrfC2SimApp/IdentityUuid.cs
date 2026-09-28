using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace VrfC2SimApp;

/// <summary>
/// C1d (RL-20260928-02, owner: "This field is supposed to carry the uuid not the human name"): THE UUID EVERY ENTITY AND
/// AGGREGATE IS CREATED UNDER (and, below, the start-up check of the bridge and the log lines that say how an object
/// was bound).
///
/// THE RECORD. VR-Forces identifies a simulation object by its UUID - "Unique", "Persists from exercise to exercise" -
/// while its Name "Does not have to be unique" and has "Limits on the length of the character string" (entities 11,
/// units 31) (UG52 13.2 Table 21 p362-363). Every create takes a caller-chosen startingUUID
/// (vrfRemoteController.h 5.2 :1282-1293 createEntity, :1295-1306 createAggregate; "Use this UUID to create object (if
/// specified). If the UUID exists will be regenerated on creation", vrfmsgs/ifCreateVrfObject.h:105). The interface has
/// always passed the C2SIM uuid for its tactical graphics (EnqueueControlAreaCreate), as the BARE 8-4-4-4-12 string, and
/// VR-Forces honours it: run G1 (runs/20260928T102541Z_run/vrfc2simapp.log :203) shows BUFFALO under
/// VRF_UUID:6f8a4647-6201-3058-9b85-e598476a0781, its own init uuid (data/IRONSTORM_CUTA_Initialization.xml :1885).
/// ORBAT_LOADING_REQUIREMENTS_2026-09-06 G5 named this fix for units; its refutation is reversed by RL-20260928-02.
///
/// THE RULE (who gets which uuid):
///   - an init unit: its OWN C2SIM uuid (<see cref="ForC2SimUnit"/>) - a ~PXY proxy is that unit's one object (one
///     object per C2SIM uuid, UG52 22.1 p490; DESIGN_ORBAT_TO_VRF C10), so it carries the same uuid;
///   - an object the INTERFACE makes - a container member (PopulatePlanner), a synthesized sub-unit (MakeChildName's
///     children) and a unit RE-CREATED as its template (MaterializeUnit case 3) - a DERIVED uuid: RFC 4122 version 5
///     (SHA-1) of "&lt;parent uuid&gt;/&lt;suffix&gt;" in the fixed namespace <see cref="Namespace"/>
///     (<see cref="Derive"/>). Deterministic across runs (same parent, same suffix, same uuid), unique per suffix.
/// Every uuid is passed BARE and lower-case - the form every tactical graphic already passes through the SAME client-side
/// conversion (VrfFacade: DtUUID(uuid.c_str())), which VR-Forces honours (5 of 5 areas in G1). Note the header documents
/// only the "VRF_UUID:"-prefixed string as yielding a valid UUID (vrfutil/uuid.h:77-84); the bare form rests on that live
/// evidence, not on the header. Never a NAME: a string that is not a uuid goes to DtUUID's marking-text lookup
/// (PREREG_ROUTE_UUID_FIX_2026-09-02) - <see cref="Normalize"/> refuses one.
/// </summary>
public static class IdentityUuid
{
    /// <summary>The interface's ONE namespace for derived uuids: itself the RFC 4122 v5 uuid of
    /// <see cref="NamespaceSource"/> in the RFC's URL namespace (Python: uuid.uuid5(uuid.NAMESPACE_URL, NamespaceSource)),
    /// so anyone can re-derive it. NEVER CHANGE IT - every derived uuid of every run changes with it.</summary>
    public const string Namespace = "485b28e7-1cc7-534b-b6a6-9beddf07a1b1";

    /// <summary>What <see cref="Namespace"/> is the v5 uuid of.</summary>
    public const string NamespaceSource = "https://github.com/OpenC2SIM/OpenC2SIM.github.io/Software/Interfaces/VRF_C2SIM#identity";

    /// <summary>RFC 4122 Appendix C NameSpace_URL.</summary>
    public const string RfcUrlNamespace = "6ba7b811-9dad-11d1-80b4-00c04fd430c8";

    /// <summary>The prefix VR-Forces puts on a uuid's string form (DtUUID::uuidMarking(); the ObjectCreated callback's
    /// uuid is "VRF_UUID:&lt;uuid&gt;", VrfFacade.cpp objectCreatedTrampoline).</summary>
    public const string VrfPrefix = "VRF_UUID:";

    /// <summary>The suffix of the uuid a unit is RE-CREATED under as its template (MaterializeUnit case 3). Not the
    /// deleted shell's uuid: a reused uuid could read the deleted shell's lingering reflection as the new object's (the
    /// N13 readability release), and the vendor regenerates a uuid that still exists (ifCreateVrfObject.h:105).</summary>
    public const string RecreateSuffix = "recreate";

    /// <summary>The canonical form - lower-case 8-4-4-4-12 - of a uuid string, or null when it is not one. Accepts the
    /// "VRF_UUID:" prefix, braces and upper case (the C2SIM schema's UUIDBaseType allows A-F).</summary>
    public static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        string s = raw.Trim();
        if (s.StartsWith(VrfPrefix, StringComparison.OrdinalIgnoreCase)) s = s.Substring(VrfPrefix.Length).Trim();
        return Guid.TryParse(s, out var g) ? g.ToString("D", CultureInfo.InvariantCulture) : null;
    }

    /// <summary>The uuid an INIT UNIT is created under: its own C2SIM uuid, canonical. "" when the init's uuid is not
    /// a uuid at all (the C2SIM schema requires the 8-4-4-4-12 form, UUIDBaseType) - nothing is then passed and the unit
    /// can only be bound by name; the caller says so.</summary>
    public static string ForC2SimUnit(string c2simUuid) => Normalize(c2simUuid) ?? "";

    /// <summary>
    /// THE DERIVED UUID of an object the interface makes: RFC 4122 sec 4.3, version 5 - SHA-1 over the namespace's 16
    /// bytes in network order followed by the UTF-8 name "&lt;parent&gt;/&lt;suffix&gt;" (the parent canonical), the
    /// first 16 bytes of the hash, version nibble 5, variant 10xx. "" when the parent has no uuid.
    /// </summary>
    public static string Derive(string parentUuid, string suffix)
    {
        string parent = Normalize(parentUuid);
        if (parent == null || string.IsNullOrEmpty(suffix)) return "";
        return V5(Namespace, parent + "/" + suffix);
    }

    /// <summary>The name <see cref="Derive"/> hashes, for the log line that states it.</summary>
    public static string DerivationName(string parentUuid, string suffix)
        => (Normalize(parentUuid) ?? "(none)") + "/" + suffix;

    /// <summary>RFC 4122 version 5 of <paramref name="name"/> (UTF-8) in <paramref name="namespaceUuid"/>.</summary>
    public static string V5(string namespaceUuid, string name)
    {
        var ns = Guid.Parse(Normalize(namespaceUuid) ?? throw new ArgumentException("not a uuid", nameof(namespaceUuid)));
        byte[] nsBytes = ns.ToByteArray(bigEndian: true);
        byte[] nameBytes = Encoding.UTF8.GetBytes(name ?? "");
        byte[] input = new byte[nsBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(nsBytes, 0, input, 0, nsBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, input, nsBytes.Length, nameBytes.Length);
        byte[] hash = SHA1.HashData(input);
        byte[] u = new byte[16];
        Array.Copy(hash, u, 16);
        u[6] = (byte)((u[6] & 0x0F) | 0x50);   // version 5 (time_hi_and_version, high nibble)
        u[8] = (byte)((u[8] & 0x3F) | 0x80);   // variant RFC 4122 (clock_seq_hi_and_reserved, 10xx)
        return new Guid(u, bigEndian: true).ToString("D", CultureInfo.InvariantCulture);
    }

    /// <summary>The version nibble of a canonical uuid string (index 14), '?' when it is not one.</summary>
    public static char VersionOf(string uuid) => Normalize(uuid) is string c ? c[14] : '?';

    /// <summary>True when the variant bits are RFC 4122's 10xx (index 19 is 8, 9, a or b).</summary>
    public static bool IsRfc4122Variant(string uuid) => Normalize(uuid) is string c && "89ab".IndexOf(c[19]) >= 0;
}

/// <summary>
/// C1d: does a VrfBridge TYPE carry the two create overloads that take a uuid - CreateEntity(type, pos, force, heading,
/// name, uuid) and CreateAggregate(type, pos, force, heading, name, state, createSubordinates, uuid)? The service calls
/// them DIRECTLY, so a managed build against a bridge without them does not compile; this is the RUNTIME side of the same
/// guard - a new VrfC2SimApp.dll beside an OLD VrfBridge.dll (the partial deploy RUNBOOK sec 9 warns of) would otherwise
/// fail every create with MissingMethodException on the tick thread. The service REFUSES TO START on false, and
/// --populate-selftest (p15) reads the linked assembly. Reflection only touches the bridge's metadata.
/// </summary>
public static class IdentityBridge
{
    public static bool Present(Type bridgeType)
    {
        if (bridgeType == null) return false;
        var entity = bridgeType.GetMethod("CreateEntity", new[]
        {
            typeof(VrfC2Sim.EntityTypeSpec), typeof(VrfC2Sim.Geodetic), typeof(VrfC2Sim.Force), typeof(double),
            typeof(string), typeof(string),
        });
        var aggregate = bridgeType.GetMethod("CreateAggregate", new[]
        {
            typeof(VrfC2Sim.EntityTypeSpec), typeof(VrfC2Sim.Geodetic), typeof(VrfC2Sim.Force), typeof(double),
            typeof(string), typeof(VrfC2Sim.AggregateState), typeof(bool), typeof(string),
        });
        return entity != null && aggregate != null && entity.ReturnType == typeof(void) && aggregate.ReturnType == typeof(void);
    }
}

/// <summary>C1d: the log lines the identity binding writes - PURE, so --name-selftest pins their exact shapes (the G1-2
/// registration greps them; RUNBOOK sec 11i).</summary>
public static class IdentityLines
{
    /// <summary>One INFO line per object bound by the uuid it was created under.</summary>
    public static string CreatedAs(string requestedName, string uuid, string returnedName)
        => $"IDENTITY: '{requestedName}' created as uuid {uuid} (requested) marking '{returnedName}'";

    /// <summary>The WARN for an object that came back under a uuid we did not request - VR-Forces regenerated it (the
    /// uuid existed, ifCreateVrfObject.h:105) or ignored it - and was therefore bound by the name registry.</summary>
    public static string NotRequested(string uuid, string returnedName)
        => $"IDENTITY: VR-Forces returned uuid {uuid} for marking '{returnedName}', not one we requested - bound by name (NameRegistry)";

    /// <summary>The summary said at READY TO TASK (or its NOT REACHED variant) over the initialization's own objects.</summary>
    public static string Summary(int byUuid, int planned, int byName, int unbound, string scope)
        => Inv($"IDENTITY: {byUuid} of {planned} objects bound by uuid, {byName} by name") +
           (unbound > 0 ? Inv($", {unbound} not bound") : "") + $" ({scope}; RL-20260928-02)";

    /// <summary>A completion or POSITION marking carried by more than one object bound by uuid: the marking cannot tell
    /// them apart (a completion carries no uuid), so the name registry's answer is used - said once per marking.</summary>
    public static string AmbiguousMarking(string marking, IReadOnlyList<string> names, string fallback, string what)
        => $"IDENTITY: the {what} marking '{marking}' is carried by {names.Count} objects bound by uuid " +
           $"[{string.Join(", ", names)}] - a marking cannot tell them apart and the report carries no uuid; the name " +
           $"registry answered '{fallback}' (C1c's names unique within 30 characters are what keep markings apart)";

    private static string Inv(FormattableString f) => FormattableString.Invariant(f);
}
