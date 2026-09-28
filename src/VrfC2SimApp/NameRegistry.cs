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
/// C1c (2026-09-28, run G1): AN AGGREGATE IS CUT AT 30, NOT 34. VR-Forces returns an aggregate name that overflows its
/// 31-character marking field (DtMaxAggregateMarkingLength) as its first <see cref="VrfNames.AggregateMarkingChars"/>
/// characters - 107 of 107 cut names; one that fits comes back whole - see <see cref="VrfNames"/> - and G1's container
/// members, built within 34, lost 22 of 23 to exactly the ambiguity below. What closes it BY CONSTRUCTION rather than
/// by a warning:
///   - <see cref="KeyConflict"/>: every name the interface asks for is checked against every name already asked for
///     (and the rest of its own batch) at the 30-character key, and a collision is resolved BEFORE the create - a made
///     name takes a ~k tag (VrfNames.UniqueChildName), a C2SIM unit name its unique 30-character form
///     (<see cref="UniqueTruncatable"/>). The ambiguity guard in <see cref="ScanForRequested"/> stays, and becomes
///     unreachable for anything the interface requested: no two truncatable names share a key.
///   - <see cref="RequestedWhole"/>: a route, waypoint, control area or line/point graphic comes back WHOLE (237 of 237
///     route names of 31-206 characters), so it can never be the source of a truncated callback. It is still an
///     exact-match target but never a prefix-scan CANDIDATE - so a long graphic name that shares a unit's first 30
///     characters can no longer make that unit's callback ambiguous.
/// Platforms are still returned at <see cref="MarkingTruncationWidth"/> (10): unique-at-30 is necessary for them, not
/// sufficient, and the 10-character hazard stays the advisory PrefixPairs / NAME PRE-FLIGHT line it always was.
///
/// Both spellings end up bound to the uuid: the requested name (what the rest of the interface
/// asks for) and the returned one (what the sim's own callbacks - completions, POSITION text
/// reports, console rows - carry). The reverse map holds the RESOLVED name, because that is the
/// name a log line or a lookup in another map needs.
///
/// C1d (2026-09-28, RL-20260928-02 - owner: "This field is supposed to carry the uuid not the human name"): IDENTITY IS
/// THE UUID, AND EVERYTHING ABOVE IS NOW THE FALLBACK. VR-Forces documents the UUID as the identifier ("Unique", "Persists
/// from exercise to exercise") and the Name as length-limited and NOT unique (UG52 13.2 Table 21 p362-363), and every
/// entity and aggregate is now created under a uuid the interface chose (<see cref="IdentityUuid"/>; the create's
/// startingUUID). <see cref="RequestedUuid"/> registers that uuid with the requested name before the create is issued;
/// <see cref="BindCreated"/> - what OnVrfObjectCreated calls - binds an ObjectCreated whose uuid is one of them to THAT
/// name EXACTLY (<see cref="TryBindByUuid"/>: no prefix scan, whatever marking came back), and only a uuid we did not
/// request falls through to the name rule (<see cref="Bind"/>; VR-Forces regenerates a uuid that already exists,
/// ifCreateVrfObject.h:105). A uuid binding is CERTAIN and displaces a name-rule guess; a name-rule binding never
/// displaces a uuid binding (the rebind refusal above). The marking VR-Forces returned is recorded per uuid, and a
/// completion or POSITION report - which carries ONLY a marking - is resolved marking -> uuid -> requested name FIRST
/// (<see cref="ResolveMarking"/>), then by the name rule. C1c's unique-within-30 planning stays as the SECONDARY key: it
/// is what keeps two objects' markings apart, and a marking two uuid-bound objects share cannot be attributed by uuid.
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
    /// <param name="ByUuid">C1d: bound through the uuid it was created under (<see cref="TryBindByUuid"/>) - exact,
    /// whatever marking came back. False = the name rule decided.</param>
    /// <param name="DisplacedUuid">C1d: EMPTY in the ordinary case. The uuid a NAME-rule guess had bound to this
    /// requested name before its own uuid arrived; the uuid binding replaced it, and the caller says so.</param>
    /// <param name="FallbackWarn">C1d: the uuid was not one we requested, and the object is one we DID request a uuid
    /// for (VR-Forces regenerated or ignored it) or one the name rule could not attribute - the caller WARNs
    /// (<see cref="IdentityLines.NotRequested"/>). False for an object never given a uuid (a task route or waypoint,
    /// a graphic), which the name rule binds by design.</param>
    public readonly record struct BindResult(string Name, string ReturnedName, bool Truncated, bool Ambiguous,
                                             IReadOnlyList<string> PrefixedCandidates, string PriorUuid,
                                             bool ByUuid = false, string DisplacedUuid = "", bool FallbackWarn = false)
    {
        /// <summary>The bind was REFUSED to protect an existing binding; <see cref="PriorUuid"/>
        /// is the uuid that still owns <see cref="Name"/>. Nothing was written.</summary>
        public bool RefusedRebind => !string.IsNullOrEmpty(PriorUuid);
    }

    /// <summary>C1d: how <see cref="ResolveMarking"/> resolved a marking.</summary>
    public enum MarkingVia
    {
        /// <summary>The marking was recorded for objects bound by uuid, all of them ONE requested name: exact.</summary>
        Uuid,
        /// <summary>No uuid-bound object carries it: the name rule answered (<see cref="Resolve"/>'s pre-C1d rule).</summary>
        Name,
        /// <summary>Two or more DIFFERENT uuid-bound objects carry it: a marking cannot tell them apart, and the name
        /// rule answered. The residual C1c's unique-within-30 names prevent for every name the interface requests.</summary>
        AmbiguousUuid,
    }

    /// <summary>C1d: a resolved marking - the name, how it was found, and (ambiguous only) the objects that carry it.</summary>
    public readonly record struct MarkingResolution(string Name, MarkingVia Via, IReadOnlyList<string> Candidates);

    private readonly ConcurrentDictionary<string, byte> _requested = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _uuidByName = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _nameByUuid = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _requestedByReturned = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _ambiguous = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _rebindAllowed = new(StringComparer.Ordinal);
    // C1c: requested names VR-Forces returns WHOLE (routes, waypoints, control areas, line/point graphics).
    private readonly ConcurrentDictionary<string, byte> _whole = new(StringComparer.Ordinal);
    // C1d: the uuid each entity/aggregate was REQUESTED under (canonical, bare) <-> its requested name.
    private readonly ConcurrentDictionary<string, string> _nameByRequestedUuid = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _requestedUuidByName = new(StringComparer.Ordinal);
    // C1d: requested names whose CURRENT binding came through their uuid (certain), not the name rule (a reading).
    private readonly ConcurrentDictionary<string, byte> _boundByUuid = new(StringComparer.Ordinal);
    // C1d: the marking VR-Forces returned at ObjectCreated -> the VRF uuids (as the callback carried them) of every object
    // of ours that carries it, however it was bound - so a marking two objects share is SEEN to be shared; and the VRF
    // uuids whose binding came through the uuid they were created under.
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _uuidsByMarking = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _uuidBoundUuids = new(StringComparer.Ordinal);
    // C1d: objects of ours a uuid binding DISPLACED from a name-rule reading - unattributed, but still deleted on stop.
    private readonly ConcurrentDictionary<string, byte> _displacedUuids = new(StringComparer.Ordinal);

    /// <summary>Register a name we are ASKING VR-Forces to create (unit, route, waypoint). Call it
    /// before the create is enqueued - the callback can arrive as soon as the create is sent.</summary>
    public void Requested(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        bool added = _requested.TryAdd(name, 0);
        // C1c: a TRUNCATABLE request wins over an earlier whole one (the conservative reading), and then it can change
        // a cached answer like any new name.
        bool wasWhole = _whole.TryRemove(name, out _);
        if (!added && !wasWhole) return;
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
    /// C1c: register a name VR-Forces returns WHOLE - a route, waypoint, control area or line/point graphic (no DIS
    /// marking; 237 route names of 31-206 characters came back intact over 132 runs, <see cref="VrfNames"/>). Its own
    /// ObjectCreated is an EXACT match like any requested name's, but it can never be the source of a TRUNCATED
    /// callback, so it is never a prefix-scan candidate and never invalidates a cached truncation. A name already
    /// registered through <see cref="Requested"/> stays truncatable (the conservative reading).
    /// </summary>
    public void RequestedWhole(string name)
    {
        if (string.IsNullOrEmpty(name) || !_requested.TryAdd(name, 0)) return;
        _whole.TryAdd(name, 0);
    }

    /// <summary>True when <paramref name="name"/> was registered through <see cref="RequestedWhole"/>.</summary>
    public bool IsWhole(string name) => !string.IsNullOrEmpty(name) && _whole.ContainsKey(name);

    /// <summary>
    /// C1c - THE REQUEST-TIME CHECK that keeps every callback attributable. Would asking VR-Forces for
    /// <paramref name="name"/> make any ObjectCreated - its own or another's - unattributable, given that a TRUNCATABLE
    /// name (a unit, a container member, a synthesized sub-unit) longer than 30 may come back as its first
    /// <see cref="VrfNames.AggregateMarkingChars"/> (it does whenever it overflows the 31-character aggregate field; a
    /// name of exactly 31 fits and comes back whole, and is treated the same way here - conservatively), while a WHOLE
    /// name (<see cref="RequestedWhole"/>) comes back intact? Checked against every requested name,
    /// <paramref name="alsoPlanned"/> (names of the same batch not registered yet, truncatable) and
    /// <paramref name="alsoWhole"/> (the same batch's graphics, whole). Returns the name it collides with, or null. The
    /// cases, one per way a callback goes wrong:
    ///   - the same name twice (except two WHOLE names: a duplicate route name is the FIFO case of _pendingRouteTasks);
    ///   - two truncatable names with the same first 30 characters when either is longer than 30 - the cut callback
    ///     is AMBIGUOUS (two candidates) or HIJACKED (an exact name equal to it);
    ///   - a whole name EQUAL to a truncatable name's 30-character form - the cut callback exact-matches the graphic.
    /// Platforms are cut at 10, not 30: this check is necessary for them, not sufficient (the PrefixPairs advisory).
    /// </summary>
    public string KeyConflict(string name, bool truncatable, IEnumerable<string> alsoPlanned = null,
                              IEnumerable<string> alsoWhole = null)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string key = VrfNames.Key(name);
        bool nameCut = truncatable && name.Length > VrfNames.AggregateMarkingChars;
        foreach (var x in _requested.Keys)
            if (Collides(x, !_whole.ContainsKey(x))) return x;
        if (alsoPlanned != null)
            foreach (var x in alsoPlanned)
                if (Collides(x, true)) return x;
        if (alsoWhole != null)
            foreach (var x in alsoWhole)
                if (Collides(x, false)) return x;
        return null;

        bool Collides(string x, bool xTruncatable)
        {
            if (string.IsNullOrEmpty(x)) return false;
            if (string.Equals(x, name, StringComparison.Ordinal)) return truncatable || xTruncatable;
            bool xCut = xTruncatable && x.Length > VrfNames.AggregateMarkingChars;
            if (truncatable && xTruncatable)
                return (nameCut || xCut) && string.Equals(key, VrfNames.Key(x), StringComparison.Ordinal);
            if (truncatable) return nameCut && string.Equals(x, key, StringComparison.Ordinal);
            if (xTruncatable) return xCut && string.Equals(name, VrfNames.Key(x), StringComparison.Ordinal);
            return false;
        }
    }

    /// <summary>
    /// C1c: a C2SIM UNIT name made unique within 30 characters BEFORE it is requested - <paramref name="desired"/>
    /// itself when <see cref="KeyConflict"/> clears it (every shipped init: 13 of 13 files have no two unit names alike
    /// in their first 30), else the first <see cref="VrfNames.Disambiguated"/>(desired, k), k = 2 .. 99, that clears.
    /// <paramref name="alsoWhole"/> are the same init's graphic names, which are registered AFTER its units on some
    /// paths and so are not in the registry yet. <paramref name="collidesWith"/> names what the desired name met (null
    /// when it met nothing). Null when all 98 tags are taken.
    /// </summary>
    public string UniqueTruncatable(string desired, IEnumerable<string> alsoPlanned, out string collidesWith,
                                    IEnumerable<string> alsoWhole = null)
    {
        var planned = alsoPlanned?.ToList();
        var whole = alsoWhole?.ToList();
        collidesWith = KeyConflict(desired, true, planned, whole);
        if (collidesWith == null) return desired;
        for (int k = 2; k <= VrfNames.MaxDisambiguator; k++)
        {
            string candidate = VrfNames.Disambiguated(desired, k);
            if (KeyConflict(candidate, true, planned, whole) == null) return candidate;
        }
        return null;
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
    /// C1d: the marking is looked up among the objects bound BY UUID first (<see cref="ResolveMarking"/>), so every
    /// caller - including one that never heard of C1d - gets the uuid-exact answer when there is one.
    /// </summary>
    public string Resolve(string returned) => ResolveMarking(returned).Name;

    /// <summary>
    /// C1d - A MARKING, RESOLVED THROUGH THE UUID FIRST. A completion or a POSITION text report carries only the marking
    /// VR-Forces holds for the object (VrfFacade.cpp reportTrampoline: transmitter().markingText()). When that marking was
    /// recorded at ObjectCreated for objects bound by uuid and they are all ONE requested name, that name is the answer,
    /// exactly - however the marking was cut. Otherwise the pre-C1d name rule answers (<see cref="Resolve"/>'s exact match,
    /// settled correlation and prefix scan); when two DIFFERENT uuid-bound objects carry the marking the result says
    /// <see cref="MarkingVia.AmbiguousUuid"/> and names them, so the caller can say it - a marking alone cannot tell them
    /// apart, which is exactly the collision C1c's unique-within-30 names prevent at request time.
    /// </summary>
    public MarkingResolution ResolveMarking(string marking)
    {
        if (string.IsNullOrEmpty(marking)) return new MarkingResolution(marking, MarkingVia.Name, Array.Empty<string>());
        // Only a marking at least one UUID-BOUND object carries is answered here; with none, the pre-C1d rule answers
        // exactly as it did before (a run, or a test, with no uuid binding is unchanged).
        if (_uuidsByMarking.TryGetValue(marking, out var carriers) && carriers.Keys.Any(u => _uuidBoundUuids.ContainsKey(u)))
        {
            var names = carriers.Keys
                .Select(u => _nameByUuid.TryGetValue(u, out var n) && !string.IsNullOrEmpty(n) ? n : null)
                .Where(n => n != null).Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToList();
            if (names.Count == 1) return new MarkingResolution(names[0], MarkingVia.Uuid, Array.Empty<string>());
            if (names.Count > 1) return new MarkingResolution(ResolveByName(marking), MarkingVia.AmbiguousUuid, names);
        }
        return new MarkingResolution(ResolveByName(marking), MarkingVia.Name, Array.Empty<string>());
    }

    /// <summary>C1d: record that the object <paramref name="vrfUuid"/> came back carrying <paramref name="marking"/>.</summary>
    private void RecordMarking(string marking, string vrfUuid)
    {
        if (string.IsNullOrEmpty(marking) || string.IsNullOrEmpty(vrfUuid)) return;
        _uuidsByMarking.GetOrAdd(marking, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))
                       .TryAdd(vrfUuid, 0);
    }

    /// <summary>The pre-C1d rule: exact requested name, then a settled correlation, then the prefix scan.</summary>
    private string ResolveByName(string returned)
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
    /// particular order. O(requested) and called at most twice per ObjectCreated. C1c: a WHOLE name
    /// (<see cref="RequestedWhole"/>) is never a candidate - VR-Forces does not cut it, so it cannot be what a
    /// truncated callback came from.</summary>
    private List<string> LongerRequested(string returned)
    {
        var hits = new List<string>();
        foreach (var candidate in _requested.Keys)
            if (candidate.Length > returned.Length && candidate.StartsWith(returned, StringComparison.Ordinal)
                && !_whole.ContainsKey(candidate))
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
            // C1d: this binding is the name rule's - a reading, not the certainty a uuid binding is - and the marking it
            // came back under is recorded like a uuid-bound object's, so a marking two objects share is seen as shared.
            if (!string.IsNullOrEmpty(resolved)) _boundByUuid.TryRemove(resolved, out _);
            RecordMarking(returnedName, uuid);
        }
        return new BindResult(resolved, returnedName, truncated, ambiguous, collisions, priorUuid);
    }

    // ================================ C1d - IDENTITY BY UUID (RL-20260928-02) ================================

    /// <summary>The canonical bare uuid of a callback's "VRF_UUID:&lt;uuid&gt;" (or of a bare one); "" when it is not a
    /// uuid (a marking-text DtUUID).</summary>
    public static string BareUuid(string vrfUuid) => IdentityUuid.Normalize(vrfUuid) ?? "";

    /// <summary>
    /// C1d: register the uuid <paramref name="name"/> is being CREATED under (the create's startingUUID), before the create
    /// is issued - its ObjectCreated can arrive as soon as the create is sent. Returns null, or the OTHER requested name
    /// that uuid is already registered for: two objects asked under one uuid is a defect (VR-Forces regenerates the
    /// second, ifCreateVrfObject.h:105, which then binds by name) - the first registration stands and the caller says so.
    /// The same name registered again under a NEW uuid (a unit re-created as its template) moves the name to it.
    /// </summary>
    public string RequestedUuid(string name, string uuid)
    {
        string bare = IdentityUuid.Normalize(uuid);
        if (string.IsNullOrEmpty(name) || bare == null) return null;
        string owner = _nameByRequestedUuid.GetOrAdd(bare, name);
        if (!string.Equals(owner, name, StringComparison.Ordinal)) return owner;
        _requestedUuidByName[name] = bare;
        return null;
    }

    /// <summary>C1d: the uuid <paramref name="name"/> was last requested under (canonical), or false.</summary>
    public bool TryGetRequestedUuid(string name, out string uuid)
    {
        uuid = "";
        return !string.IsNullOrEmpty(name) && _requestedUuidByName.TryGetValue(name, out uuid) && !string.IsNullOrEmpty(uuid);
    }

    /// <summary>C1d: was <paramref name="name"/> requested under a uuid?</summary>
    public bool HasRequestedUuid(string name) => TryGetRequestedUuid(name, out _);

    /// <summary>C1d: is <paramref name="name"/>'s CURRENT binding the one its own uuid made (not the name rule's)?</summary>
    public bool IsBoundByUuid(string name)
        => !string.IsNullOrEmpty(name) && _boundByUuid.ContainsKey(name) && _uuidByName.ContainsKey(name);

    /// <summary>
    /// C1d - THE UUID BINDING. When the callback's uuid is one we requested, bind THAT requested name to it EXACTLY - no
    /// prefix scan, whatever marking came back - and record the marking against the uuid for the report paths. False (and
    /// nothing written) when the uuid is not one we requested. A name the NAME RULE bound earlier (a reading: an
    /// unrequested object's cut marking exactly equal to this name) is DISPLACED - the uuid is certain. A name already
    /// bound BY UUID to another object moves only when a re-create was announced (<see cref="ExpectRebind"/>), as a name
    /// binding does; otherwise the bind is refused like the name rule's (m1).
    /// </summary>
    public bool TryBindByUuid(string returnedName, string vrfUuid, out BindResult result)
    {
        result = default;
        string bare = BareUuid(vrfUuid);
        if (bare.Length == 0 || !_nameByRequestedUuid.TryGetValue(bare, out var name) || string.IsNullOrEmpty(name))
            return false;
        bool truncated = !string.IsNullOrEmpty(returnedName) && !string.Equals(returnedName, name, StringComparison.Ordinal);
        string priorUuid = "", displaced = "";
        if (_uuidByName.TryGetValue(name, out var prior) && !string.IsNullOrEmpty(prior)
            && !string.Equals(prior, vrfUuid, StringComparison.Ordinal))
        {
            // An ANNOUNCED re-create (the shell deleted, the unit re-created as its template) moves the name however it
            // was bound, and consumes the one-shot allowance; otherwise a name-rule reading is replaced (the uuid is
            // certain) and a uuid binding is refused.
            bool announced = _rebindAllowed.TryRemove(name, out _);
            if (!announced && !_boundByUuid.ContainsKey(name)) displaced = prior;
            else if (!announced) priorUuid = prior;
        }
        if (priorUuid.Length == 0)
        {
            if (displaced.Length > 0)
            {
                _nameByUuid.TryRemove(new KeyValuePair<string, string>(displaced, name));
                _displacedUuids.TryAdd(displaced, 0);
            }
            _uuidByName[name] = vrfUuid;
            _nameByUuid[vrfUuid] = name;
            _boundByUuid[name] = 0;
            _uuidBoundUuids.TryAdd(vrfUuid, 0);
            RecordMarking(returnedName, vrfUuid);
        }
        result = new BindResult(name, returnedName, truncated, false, Array.Empty<string>(), priorUuid,
                                ByUuid: true, DisplacedUuid: displaced);
        return true;
    }

    /// <summary>
    /// C1d - THE ObjectCreated BINDING, what OnVrfObjectCreated calls: the uuid first (<see cref="TryBindByUuid"/>), the
    /// name rule (<see cref="Bind"/>) only for a uuid we did not request - with <see cref="BindResult.FallbackWarn"/> set
    /// when that object is one we DID request a uuid for (VR-Forces regenerated or ignored it) or one the name rule
    /// cannot attribute; an object never given a uuid (a task route or waypoint, a graphic) binds by name silently, as
    /// it always has.
    /// </summary>
    public BindResult BindCreated(string returnedName, string vrfUuid)
    {
        if (TryBindByUuid(returnedName, vrfUuid, out var byUuid)) return byUuid;
        var b = Bind(returnedName, vrfUuid);
        bool notable = b.Ambiguous || b.RefusedRebind || !IsRequested(b.Name) || HasRequestedUuid(b.Name);
        return b with { FallbackWarn = notable };
    }

    /// <summary>C1d: how many of <paramref name="names"/> are bound by their uuid, by the name rule, and not at all.</summary>
    public (int ByUuid, int ByName, int Unbound) IdentityCensus(IEnumerable<string> names)
    {
        int byUuid = 0, byName = 0, unbound = 0;
        foreach (var n in names ?? Array.Empty<string>())
        {
            if (!TryGetUuid(n, out _)) unbound++;
            else if (IsBoundByUuid(n)) byUuid++;
            else byName++;
        }
        return (byUuid, byName, unbound);
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

    /// <summary>Every distinct uuid this run bound - what the stop path deletes. C1d: with the objects a uuid binding
    /// displaced from a name-rule reading, which are ours and unattributed.</summary>
    public List<string> CreatedUuids()
        => _uuidByName.Values.Concat(_displacedUuids.Keys).Where(v => !string.IsNullOrEmpty(v))
                      .Distinct(StringComparer.Ordinal).ToList();
}
