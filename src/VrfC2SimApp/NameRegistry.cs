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
/// Resolution is CACHED per returned name, so the scan runs once per distinct truncation. The cache
/// entry is DROPPED when a later <see cref="Requested"/> registers a name that would have changed
/// the answer (cold-start review finding 3, 2026-09-14) - without that, a resolution computed while
/// only one candidate was known outlived the arrival of its sibling.
///
/// ONLY A NAME STILL AWAITING ITS ObjectCreated IS A RESOLUTION TARGET (review finding 2). An object
/// whose name is the prefix of a unit that is ALREADY BOUND binds under its OWN name, so a
/// sim-created object (a template member, an aggregate's own subordinate) can never take a live
/// unit's identity BY TRUNCATION. AMBIGUITY is still judged against the WHOLE requested set: two
/// candidates make the callback unattributable however many of them are already bound.
///
/// NO-HIJACK, STATED ACCURATELY (cold-start review 02b51de, m1). Finding 2's awaiting-only rule
/// covers the PREFIX-SCAN path ONLY; it says nothing about the EXACT-match short-circuit, which
/// runs first. A second ObjectCreated whose name is EXACTLY a requested name already bound to a
/// DIFFERENT uuid used to overwrite both maps silently - and "510/40~PXY" is exactly
/// <see cref="MarkingTruncationWidth"/> characters, so each of its four EXPAND children can come
/// back under precisely that string. <see cref="Bind"/> now REFUSES such a rebind: the prior
/// binding stands, neither map is touched, and <see cref="BindResult.RefusedRebind"/> tells the
/// caller to log an ERROR. The guarantee is therefore: ONCE A REQUESTED NAME IS BOUND, ONLY AN
/// ANNOUNCED re-create (<see cref="ExpectRebind"/>) CAN MOVE IT. It is deliberately narrow - it
/// does NOT cover the truncated spelling written alongside the resolved one, nor a name that was
/// never requested.
///
/// THE ONE LEGITIMATE REBIND is order-time materialization case 3 (VrfC2SimService.MaterializeUnit
/// / _recreatePending): the coarse shell is DELETED and the same requested name re-created as its
/// template, so a second ObjectCreated for that name is expected and its uuid is the real one. The
/// service calls <see cref="ExpectRebind"/> at the same statement that sets _recreatePending, and
/// the allowance is ONE-SHOT - consumed by the next bind of that name and by no later one.
///
/// *** CORRECTNESS DEPENDS ON EVERY REQUESTED NAME FITTING THE MARKING WIDTH *** (review finding 1).
/// When a returned name is simultaneously (a) EXACTLY one requested name and (b) a strict prefix of
/// longer requested names, the exact name wins. That is the only defensible choice - it is what the
/// 127 correctly-bound units of run G6 depend on - but it is a GUESS whenever a longer sibling could
/// have been truncated down to it, and the plain ambiguity guard below does NOT see that shape.
/// <see cref="BindResult.PrefixedCandidates"/> therefore carries those siblings so the caller can
/// WARN instead of binding silently, and <see cref="PrefixPairs"/> lets the service say so ONCE at
/// create time rather than never. The shape is real, not hypothetical: in COA-STP1 "510/40~PXY" is
/// exactly <see cref="MarkingTruncationWidth"/> characters long and its four EXPAND children
/// ("510/40~PXY.HQ1", ".TANK2", ".TANK3", ".PLOW4") all carry it as a strict prefix. docs/PORT.md
/// sec 6 tells STP to cap unit names at 10 (C2SIMxmlHandler.cpp:2365) for exactly this reason; this
/// interface's own MaxVrfMarkingChars is 34, i.e. three times the sim's.
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
    /// any real truncation is far above this floor.
    /// WHAT THIS ACTUALLY BUYS (review finding 2): it excludes the SHORT vendor names the sim gives
    /// its own template members ("M1A2 37", "M3 7", "SAM 1"), nothing more. It is not a defence
    /// against an unrequested object in general - "M577A2 7" and "HMMWV 13" are both exactly 8
    /// characters and clear it; they simply happen to prefix none of our names. The defence against
    /// that case is the awaiting-only rule in <see cref="Resolve"/>.</summary>
    public const int MinTruncatedNameChars = 8;

    /// <summary>The character count at which VR-Forces truncates the DIS marking a name is carried
    /// as. docs/PORT.md sec 6 says 10 (C2SIMxmlHandler.cpp:2365) and run G6 returned exactly 10
    /// ("2/1_AD/25_" for "2/1_AD/25_~PXY"). Used ONLY by <see cref="PrefixPairs"/>, the advisory
    /// start-up check - resolution itself never assumes a width.</summary>
    public const int MarkingTruncationWidth = 10;

    /// <summary>What <see cref="Bind"/> did with one ObjectCreated callback.</summary>
    /// <param name="Name">The name the rest of the interface should use (the requested one when
    /// the callback's name was a truncation of it, else the callback's name unchanged).</param>
    /// <param name="ReturnedName">Exactly what VR-Forces handed back.</param>
    /// <param name="Truncated">The callback's name was resolved to a longer requested name.</param>
    /// <param name="Ambiguous">The callback's name prefixes 2+ requested names - nothing was
    /// resolved, and the caller should say so once.</param>
    /// <param name="PrefixedCandidates">EMPTY in the ordinary case. Non-empty when the callback's
    /// name was EXACTLY a requested name AND is also a strict prefix of longer requested names that
    /// are still awaiting their own ObjectCreated (review finding 1): the exact binding stands, but
    /// any of those siblings could have produced this callback by truncation, so the caller must
    /// WARN and name them. Never a reason to refuse the binding - a wrong warning is cheap, an
    /// unbound unit is nine hours of silent nothing (the B3 bug).</param>
    /// <param name="PriorUuid">EMPTY in the ordinary case. The uuid this name was ALREADY bound to
    /// when a second, UNANNOUNCED ObjectCreated arrived under it (m1): the binding was REFUSED and
    /// this is the uuid that was kept. The caller must log an ERROR - the newcomer is an object we
    /// cannot attribute, and silently re-pointing the name at it would make R1 report the wrong
    /// object's position for a live unit and ExecuteTaskOnTick task it.</param>
    public readonly record struct BindResult(string Name, string ReturnedName, bool Truncated, bool Ambiguous,
                                             IReadOnlyList<string> PrefixedCandidates, string PriorUuid)
    {
        /// <summary>The bind was REFUSED to protect an existing binding; <see cref="PriorUuid"/>
        /// is the uuid that still owns <see cref="Name"/>. Nothing was written.</summary>
        public bool RefusedRebind => !string.IsNullOrEmpty(PriorUuid);
    }

    private readonly ConcurrentDictionary<string, byte> _requested = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _uuidByName = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _nameByUuid = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _requestedByReturned = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _ambiguous = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _rebindAllowed = new(StringComparer.Ordinal);

    /// <summary>Register a name we are ASKING VR-Forces to create (unit, route, waypoint). Call it
    /// before the create is enqueued - the callback can arrive as soon as the create is sent.</summary>
    public void Requested(string name)
    {
        if (string.IsNullOrEmpty(name) || !_requested.TryAdd(name, 0)) return;
        // Review finding 3: a cached resolution was computed against the candidate set AS IT STOOD.
        // A name registered later that shares a cached returned-name's prefix would have made that
        // scan ambiguous, so the cached answer is no longer entitled to stand. EnqueueCreates now
        // registers a whole batch before issuing any create (f0d1c68), which closes the within-batch
        // race; this closes the general case - CreateRoute / CreateWaypoint / control areas and the
        // order-time materialize path all call Requested long after init.
        foreach (var key in _requestedByReturned.Keys)
            if (name.Length > key.Length && name.StartsWith(key, StringComparison.Ordinal))
                _requestedByReturned.TryRemove(key, out _);
    }

    /// <summary>
    /// ANNOUNCE a deliberate re-create: the object currently bound to <paramref name="name"/> is
    /// being DELETED and the same name re-created (order-time materialization case 3 - the coarse
    /// shell becomes its template). Without this, <see cref="Bind"/> refuses the second
    /// ObjectCreated for that name and the maps would keep pointing at the deleted shell.
    /// ONE-SHOT: consumed by the next <see cref="Bind"/> of that name. Idempotent - announcing
    /// twice still permits exactly one rebind, which is what a re-issued create needs.
    /// </summary>
    public void ExpectRebind(string name)
    {
        if (!string.IsNullOrEmpty(name)) _rebindAllowed[name] = 0;
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
        // A returned name that IS ALREADY BOUND to an object is a SETTLED correlation, not a
        // prediction, so answer it from the maps. This is what keeps every later lookup for a
        // truncated unit working (the POSITION text report, the completion callback, the console
        // row) after the finding-3 invalidation has dropped the cache entry that first recorded it
        // - a fresh scan at that point would see candidates registered since and refuse.
        if (_uuidByName.TryGetValue(returned, out var boundUuid) && !string.IsNullOrEmpty(boundUuid)
            && _nameByUuid.TryGetValue(boundUuid, out var boundName) && !string.IsNullOrEmpty(boundName))
            return boundName;
        return ScanForRequested(returned);
    }

    /// <summary>
    /// The resolution a NEW ObjectCreated gets. Identical to <see cref="Resolve"/> except that it
    /// does NOT consult the settled-correlation maps: that correlation belongs to the object that
    /// was bound under this name, and a SECOND object arriving under the same returned name must be
    /// judged on its own - which, with two candidates now visible, is AMBIGUOUS.
    /// </summary>
    private string ResolveForBind(string returned)
    {
        if (string.IsNullOrEmpty(returned)) return returned;
        if (_requested.ContainsKey(returned)) return returned;
        if (_requestedByReturned.TryGetValue(returned, out var cached)) return cached;
        return ScanForRequested(returned);
    }

    // AMBIGUITY is judged against the WHOLE requested set (bound or not): two candidates mean the
    // callback cannot be attributed, and that verdict must not depend on which of them happened to
    // arrive first. RESOLUTION is then allowed only onto a candidate that is still AWAITING its
    // ObjectCreated (review finding 2) - a name already bound belongs to a live object, and handing
    // its identity to a second one would overwrite a working unit.
    private string ScanForRequested(string returned)
    {
        if (_ambiguous.ContainsKey(returned)) return returned;
        if (returned.Length < MinTruncatedNameChars) return returned;
        var longer = LongerRequested(returned);
        if (longer.Count > 1) { _ambiguous.TryAdd(returned, 0); return returned; }
        if (longer.Count == 0) return returned;
        string only = longer[0];
        if (_uuidByName.ContainsKey(only)) return returned;   // already bound: not a truncation of it
        _requestedByReturned[returned] = only;
        return only;
    }

    /// <summary>Every requested name that has <paramref name="returned"/> as a STRICT prefix, in no
    /// particular order. O(requested) and called at most twice per ObjectCreated.</summary>
    private List<string> LongerRequested(string returned)
    {
        var hits = new List<string>();
        foreach (var candidate in _requested.Keys)
            if (candidate.Length > returned.Length && candidate.StartsWith(returned, StringComparison.Ordinal))
                hits.Add(candidate);
        return hits;
    }

    /// <summary>
    /// ADVISORY START-UP CHECK (review finding 1). Pairs of requested names where the shorter is a
    /// strict PREFIX of the longer and is short enough to survive truncation at
    /// <paramref name="markingWidth"/> intact - i.e. the shapes in which a truncated callback for
    /// the LONGER unit is indistinguishable from an exact callback for the SHORTER one. Pairs whose
    /// shorter name is below <see cref="MinTruncatedNameChars"/> are excluded: the registry never
    /// treats such a name as a truncation, so they are not hazards ("C/1-35" and "C/1-35.HQ1").
    /// Nothing here changes behaviour; it exists so the hazard is stated ONCE, loudly, at the moment
    /// the names become known, instead of surfacing as a mis-bound unit nobody notices.
    /// </summary>
    public List<(string Shorter, string Longer)> PrefixPairs(int markingWidth)
    {
        var pairs = new List<(string Shorter, string Longer)>();
        foreach (var shorter in _requested.Keys)
        {
            if (shorter.Length < MinTruncatedNameChars || shorter.Length > markingWidth) continue;
            foreach (var longer in LongerRequested(shorter)) pairs.Add((shorter, longer));
        }
        return pairs;
    }

    /// <summary>
    /// Record an ObjectCreated: resolve the callback's name and bind the uuid under BOTH spellings
    /// (so every existing name-keyed lookup works whichever name it holds) and the reverse map
    /// under the resolved one.
    /// </summary>
    public BindResult Bind(string returnedName, string uuid)
    {
        string resolved = ResolveForBind(returnedName);
        bool truncated = !string.IsNullOrEmpty(returnedName)
                         && !string.Equals(resolved, returnedName, StringComparison.Ordinal);
        bool ambiguous = !string.IsNullOrEmpty(returnedName) && _ambiguous.ContainsKey(returnedName);

        // Review finding 1: the exact-match short-circuit in Resolve runs BEFORE the prefix scan, so
        // a name that is both an exact requested name and a strict prefix of others used to bind
        // silently - no Truncated, no Ambiguous, no log line. Keep the exact binding (it is the only
        // defensible choice and the 127 working units depend on it) and hand the caller the siblings
        // that could have produced the same callback. Computed BEFORE the writes below so the
        // still-awaiting test is not confused by this very binding.
        IReadOnlyList<string> collisions = Array.Empty<string>();
        if (!truncated && !string.IsNullOrEmpty(returnedName)
            && returnedName.Length >= MinTruncatedNameChars && _requested.ContainsKey(returnedName))
        {
            var awaiting = LongerRequested(returnedName).Where(c => !_uuidByName.ContainsKey(c)).ToList();
            if (awaiting.Count > 0) collisions = awaiting;
        }

        // m1 (cold-start review 02b51de): the write below is an OVERWRITE. When `resolved` is
        // already bound to a DIFFERENT object this hands a live unit's identity to a newcomer -
        // R1 would then report that object's position AS the unit and ExecuteTaskOnTick would task
        // it. Refuse, keep what we have, and tell the caller. The scope is deliberately the
        // NON-TRUNCATED case: a truncation resolution has already been vetted by ScanForRequested,
        // whose awaiting-only rule refuses a candidate that is bound (finding 2). The one
        // legitimate rebind - the shell deleted and re-created as its template - is ANNOUNCED
        // through ExpectRebind and consumed here, once.
        string priorUuid = "";
        if (!string.IsNullOrEmpty(uuid) && !truncated && !string.IsNullOrEmpty(resolved)
            && _uuidByName.TryGetValue(resolved, out var prior) && !string.IsNullOrEmpty(prior)
            && !string.Equals(prior, uuid, StringComparison.Ordinal)
            && !_rebindAllowed.TryRemove(resolved, out _))
        {
            priorUuid = prior;
        }

        if (!string.IsNullOrEmpty(uuid) && priorUuid.Length == 0)
        {
            if (!string.IsNullOrEmpty(resolved)) _uuidByName[resolved] = uuid;
            if (truncated) _uuidByName[returnedName] = uuid;
            if (!string.IsNullOrEmpty(resolved)) _nameByUuid[uuid] = resolved;
        }
        return new BindResult(resolved, returnedName, truncated, ambiguous, collisions, priorUuid);
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
