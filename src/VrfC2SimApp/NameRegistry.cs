using System.Collections.Concurrent;

namespace VrfC2SimApp;

/// <summary>
/// The interface's NAME &lt;-&gt; VRF-UUID correlation, and the one place that knows the VR-Forces
/// object we asked for may come back under a DIFFERENT (shorter) name than we requested.
///
/// THE BUG THIS EXISTS FOR (B3, 2026-09-14). A unit created as a single PLATFORM carries its name
/// as a DIS entity MARKING, which is 11 bytes; VR-Forces returns the marking it kept, not the name
/// we sent. From runs/20260914T002716Z_run/vrfc2simapp.log:
///     "TYPE MAP Proxy: 2/1_AD/25_~PXY -&gt; M577A2_Command_Post ..."
///     "PLACEMENT: PLATFORM 2/1_AD/25_~PXY domain=1 created at authored lat/lon ..."
///     "VRF console level 4 requested for 2/1_AD/25_ (VRF_UUID:2c8917fe-2bb7-b147-b7f8-7588af71db75)."
/// We requested "2/1_AD/25_~PXY" (14 chars); the ObjectCreated callback carried "2/1_AD/25_" (10).
/// Every map in the service is keyed by the REQUESTED name (it comes from the C2SIM init), so the
/// created object was filed under a name nothing ever looks up: the R1 position poll reported
/// "127 sent, 1 skipped (no reflected object yet)" every 10 s for nine hours, the arrival-evidence
/// check never judged that unit, and a vendor completion for it could not be attributed.
///
/// THE RULE. A returned name that is not itself a requested name resolves to the UNIQUE requested
/// name that has it as a strict PREFIX - that is what truncation does, and nothing else about the
/// returned string is usable. Guards, because a wrong identity is worse than an unresolved one:
///   - a returned name shorter than <see cref="MinTruncatedNameChars"/> is never treated as a
///     truncation (marking truncation lands at 10-11 characters; a short name that happens to
///     prefix one of ours is a DIFFERENT object - a template member the sim created itself);
///   - two or more requested names sharing that prefix is AMBIGUOUS: no guess, the raw name stands
///     and the caller says so out loud. (MakeChildName can produce such siblings - "X.tk1"/"X.tk2"
///     - so this is a real case, not a hypothetical.)
/// Resolution is CACHED per returned name, so the scan runs once per distinct truncation.
///
/// Both spellings end up bound to the uuid: the requested name (what the rest of the interface
/// asks for) and the returned one (what the sim's own callbacks - completions, POSITION text
/// reports, console rows - carry). The reverse map holds the RESOLVED name, because that is the
/// name a log line or a lookup in another map needs.
///
/// Thread-safety: concurrent maps throughout. <see cref="Requested"/> runs on the init/order
/// thread strictly before the create is enqueued; <see cref="Bind"/> and the readers run on the
/// tick thread and on SDK event threads.
/// </summary>
public sealed class NameRegistry
{
    /// <summary>A returned name shorter than this is never treated as a truncated marking.
    /// DIS marking text is 11 bytes and VR-Forces returned 10 characters in the B3 evidence, so
    /// any real truncation is far above this floor.</summary>
    public const int MinTruncatedNameChars = 8;

    /// <summary>What <see cref="Bind"/> did with one ObjectCreated callback.</summary>
    /// <param name="Name">The name the rest of the interface should use (the requested one when
    /// the callback's name was a truncation of it, else the callback's name unchanged).</param>
    /// <param name="ReturnedName">Exactly what VR-Forces handed back.</param>
    /// <param name="Truncated">The callback's name was resolved to a longer requested name.</param>
    /// <param name="Ambiguous">The callback's name prefixes 2+ requested names - nothing was
    /// resolved, and the caller should say so once.</param>
    public readonly record struct BindResult(string Name, string ReturnedName, bool Truncated, bool Ambiguous);

    private readonly ConcurrentDictionary<string, byte> _requested = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _uuidByName = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _nameByUuid = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _requestedByReturned = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _ambiguous = new(StringComparer.Ordinal);

    /// <summary>Register a name we are ASKING VR-Forces to create (unit, route, waypoint). Call it
    /// before the create is enqueued - the callback can arrive as soon as the create is sent.</summary>
    public void Requested(string name)
    {
        if (!string.IsNullOrEmpty(name)) _requested.TryAdd(name, 0);
    }

    /// <summary>True if <paramref name="name"/> is exactly a name we asked for.</summary>
    public bool IsRequested(string name) => !string.IsNullOrEmpty(name) && _requested.ContainsKey(name);

    /// <summary>How many distinct names we have asked VR-Forces to create.</summary>
    public int RequestedCount => _requested.Count;

    /// <summary>
    /// The name the interface knows a VR-Forces-supplied name by: the requested name when the
    /// supplied one is its unique truncation, else the supplied name unchanged. Pure lookup - it
    /// binds nothing. Safe to call from any callback that carries a marking (completions, POSITION
    /// text reports, console rows).
    /// </summary>
    public string Resolve(string returned)
    {
        if (string.IsNullOrEmpty(returned)) return returned;
        if (_requested.ContainsKey(returned)) return returned;                  // the ordinary case
        if (_requestedByReturned.TryGetValue(returned, out var cached)) return cached;
        if (_ambiguous.ContainsKey(returned)) return returned;
        if (returned.Length < MinTruncatedNameChars) return returned;

        string only = null;
        int matches = 0;
        foreach (var candidate in _requested.Keys)
        {
            if (candidate.Length <= returned.Length || !candidate.StartsWith(returned, StringComparison.Ordinal))
                continue;
            only = candidate;
            if (++matches > 1) break;
        }
        if (matches == 1)
        {
            _requestedByReturned[returned] = only;
            return only;
        }
        if (matches > 1) _ambiguous.TryAdd(returned, 0);
        return returned;
    }

    /// <summary>
    /// Record an ObjectCreated: resolve the callback's name and bind the uuid under BOTH spellings
    /// (so every existing name-keyed lookup works whichever name it holds) and the reverse map
    /// under the resolved one.
    /// </summary>
    public BindResult Bind(string returnedName, string uuid)
    {
        string resolved = Resolve(returnedName);
        bool truncated = !string.IsNullOrEmpty(returnedName)
                         && !string.Equals(resolved, returnedName, StringComparison.Ordinal);
        bool ambiguous = !string.IsNullOrEmpty(returnedName) && _ambiguous.ContainsKey(returnedName);
        if (!string.IsNullOrEmpty(uuid))
        {
            if (!string.IsNullOrEmpty(resolved)) _uuidByName[resolved] = uuid;
            if (truncated) _uuidByName[returnedName] = uuid;
            if (!string.IsNullOrEmpty(resolved)) _nameByUuid[uuid] = resolved;
        }
        return new BindResult(resolved, returnedName, truncated, ambiguous);
    }

    /// <summary>The VRF uuid bound to a name (requested or as-returned).</summary>
    public bool TryGetUuid(string name, out string uuid)
    {
        uuid = "";
        return !string.IsNullOrEmpty(name) && _uuidByName.TryGetValue(name, out uuid)
               && !string.IsNullOrEmpty(uuid);
    }

    /// <summary>The name bound to a VRF uuid (the RESOLVED name for objects we created).</summary>
    public bool TryGetName(string uuid, out string name)
    {
        name = null;
        return !string.IsNullOrEmpty(uuid) && _nameByUuid.TryGetValue(uuid, out name);
    }

    /// <summary>Name an object we did NOT create (an aggregate's member, whose uuid+name the
    /// bridge reports), without disturbing a binding that is already there.</summary>
    public bool TryAddName(string uuid, string name)
        => !string.IsNullOrEmpty(uuid) && _nameByUuid.TryAdd(uuid, name ?? "");

    /// <summary>Every distinct uuid this run bound - what the stop path deletes.</summary>
    public List<string> CreatedUuids()
        => _uuidByName.Values.Where(v => !string.IsNullOrEmpty(v)).Distinct(StringComparer.Ordinal).ToList();
}
