namespace VrfC2SimApp;

/// <summary>
/// Offline check of <see cref="NameRegistry"/> (no bridge, no MAK, no VR-Forces):
/// `VrfC2SimApp --name-selftest`. B3, 2026-09-14.
///
/// The case is taken verbatim from runs/20260914T002716Z_run/vrfc2simapp.log: we requested the
/// marking "2/1_AD/25_~PXY" and VR-Forces returned the object as "2/1_AD/25_" (the DIS marking,
/// truncated). Every lookup below is the EXACT lookup a service path makes:
///   - MaybeSendPositionReports (R1) does TryGetUuid(CreatedUnit.Name) - the REQUESTED marking;
///   - TryReadMemberPositions (the arrival-evidence check C15, and the stall watchdog C16) does
///     the same TryGetUuid(unit name);
///   - ExecuteTaskOnTick does it again before it will task a unit;
///   - OnVrfTaskCompleted / OnVrfTextReport carry the name the SIM uses, so they Resolve() it;
///   - the console and formation replies carry only a uuid, so they TryGetName(uuid).
/// The service-level feed (raising ObjectCreated on a live service) needs the bridge and is NOT
/// covered here - see the B3 report.
/// </summary>
public static class NameSelfTest
{
    public static int Run()
    {
        int failures = 0;

        // ---- the B3 case: a platform whose marking came back truncated ----
        const string requested = "2/1_AD/25_~PXY";                              // what the type map produced
        const string returned = "2/1_AD/25_";                                   // what VR-Forces handed back
        const string uuid = "VRF_UUID:2c8917fe-2bb7-b147-b7f8-7588af71db75";    // from the same run log
        var reg = new NameRegistry();
        reg.Requested(requested);
        var bind = reg.Bind(returned, uuid);

        Console.WriteLine($"=== truncated marking: requested '{requested}' ({requested.Length} chars), " +
                          $"returned '{returned}' ({returned.Length} chars) ===");
        Check(ref failures, bind.Truncated, "Bind reports the callback name as a TRUNCATION");
        Check(ref failures, !bind.Ambiguous, "Bind reports no ambiguity");
        Check(ref failures, bind.Name == requested, "Bind resolves to the requested marking");
        Check(ref failures, reg.TryGetUuid(requested, out var byRequested) && byRequested == uuid,
              "R1 POSITION POLL finds the unit by its requested name (TryGetUuid(unit.Name))");
        Check(ref failures, reg.TryGetUuid(returned, out var byReturned) && byReturned == uuid,
              "the sim's own spelling still resolves (TryGetUuid(returned name))");
        Check(ref failures, reg.TryGetName(uuid, out var back) && back == requested,
              "the console/formation reverse map gives the REQUESTED name for the uuid");
        Check(ref failures, reg.Resolve(returned) == requested,
              "a completion/POSITION callback carrying the truncated name resolves to the unit");

        // ---- the ordinary case: a name that fits, returned unchanged ----
        const string plain = "114.MechCoy";
        reg.Requested(plain);
        var exact = reg.Bind(plain, "VRF_UUID:aaaa");
        Check(ref failures, !exact.Truncated && exact.Name == plain, "an untruncated name binds unchanged");
        Check(ref failures, reg.TryGetUuid(plain, out var pu) && pu == "VRF_UUID:aaaa",
              "the untruncated unit is found by name");

        // ---- ambiguity: siblings sharing the truncated prefix are NOT guessed ----
        var amb = new NameRegistry();
        amb.Requested("AAAAAAAAAA.tk1");
        amb.Requested("AAAAAAAAAA.tk2");
        var ambBind = amb.Bind("AAAAAAAAAA", "VRF_UUID:bbbb");
        Check(ref failures, ambBind.Ambiguous, "two requested names sharing the prefix = AMBIGUOUS");
        Check(ref failures, ambBind.Name == "AAAAAAAAAA", "an ambiguous callback name is NOT resolved (no guess)");
        Check(ref failures, !amb.TryGetUuid("AAAAAAAAAA.tk1", out _) && !amb.TryGetUuid("AAAAAAAAAA.tk2", out _),
              "neither sibling is given the ambiguous object's uuid");

        // ---- a SHORT prefix is a different object, not a truncation ----
        var shortReg = new NameRegistry();
        shortReg.Requested("M1A2 Platoon");
        var shortBind = shortReg.Bind("M1A2", "VRF_UUID:cccc");
        Check(ref failures, !shortBind.Truncated && shortBind.Name == "M1A2",
              $"a returned name under {NameRegistry.MinTruncatedNameChars} chars is never a truncation");
        Check(ref failures, !shortReg.TryGetUuid("M1A2 Platoon", out _),
              "the short-named object does not steal the requested unit's identity");

        // ---- an object we never asked for (a template member the sim created) ----
        var member = new NameRegistry();
        member.Requested("1141.MechPlt");
        var memberBind = member.Bind("M2A3 IFV 1", "VRF_UUID:dddd");
        Check(ref failures, !memberBind.Truncated && memberBind.Name == "M2A3 IFV 1",
              "an unrequested name passes through unchanged");
        Check(ref failures, !member.TryGetUuid("1141.MechPlt", out _),
              "an unrequested object does not bind itself to a requested name");
        Check(ref failures, member.TryAddName("VRF_UUID:eeee", "member-2")
                            && member.TryGetName("VRF_UUID:eeee", out var m2) && m2 == "member-2",
              "TryAddName names a member the bridge reported");

        // ---- REVIEW FINDING 1: a returned name that is BOTH an exact requested name AND the
        // prefix of longer ones. This is the REAL COA-STP1 shape, not a hypothetical: the proxied
        // brigade "510/40~PXY" is exactly MarkingTruncationWidth characters long and every EXPAND
        // child (MakeChildName) carries it as a strict prefix. The exact match must still win - the
        // 127 units that bind correctly today depend on it - but it must not bind SILENTLY.
        Console.WriteLine();
        Console.WriteLine("=== finding 1: exact match that is also a prefix (the real COA-STP1 shape) ===");
        var coa = new NameRegistry();
        const string parent = "510/40~PXY";
        string[] kids = { parent + ".HQ1", parent + ".TANK2", parent + ".TANK3", parent + ".PLOW4" };
        coa.Requested(parent);
        foreach (var k in kids) coa.Requested(k);
        const string coaUuid = "VRF_UUID:coa-parent";
        var coaBind = coa.Bind(parent, coaUuid);
        Check(ref failures, coaBind.Name == parent && !coaBind.Truncated && !coaBind.Ambiguous,
              "the EXACT requested name still wins (the working units are untouched)");
        Check(ref failures, coa.TryGetUuid(parent, out var cu) && cu == coaUuid
                            && coa.TryGetName(coaUuid, out var cn) && cn == parent,
              "... and the SAME two writes happen: name -> uuid and uuid -> name");
        Check(ref failures, coaBind.PrefixedCandidates.Count == 4,
              $"the 4 EXPAND children sharing the prefix are REPORTED, not swallowed " +
              $"(saw {coaBind.PrefixedCandidates.Count})");
        Check(ref failures, kids.All(k => coaBind.PrefixedCandidates.Contains(k, StringComparer.Ordinal)),
              "every colliding candidate is named, so the warning can list them");
        Check(ref failures, coa.PrefixPairs(NameRegistry.MarkingTruncationWidth).Count == 4,
              "the start-up pre-flight finds the same 4 pairs BEFORE any object comes back");
        var shortPair = new NameRegistry();
        shortPair.Requested("C/1-35");                 // 6 chars - below the truncation floor
        shortPair.Requested("C/1-35.HQ1");
        Check(ref failures, shortPair.PrefixPairs(NameRegistry.MarkingTruncationWidth).Count == 0,
              "a prefix shorter than the truncation floor is NOT a hazard and is not reported");

        // ---- REVIEW FINDING 2: only a name still AWAITING its ObjectCreated is a resolution
        // target, so a later object cannot take a live unit's identity ----
        Console.WriteLine();
        Console.WriteLine("=== finding 2: an already-bound unit cannot be hijacked by a later object ===");
        var bound = new NameRegistry();
        const string live = "ABCDEFGHIJ~PXY";
        bound.Requested(live);
        bound.Bind(live, "VRF_UUID:live");
        var hijack = bound.Bind("ABCDEFGHIJ", "VRF_UUID:intruder");
        Check(ref failures, !hijack.Truncated && hijack.Name == "ABCDEFGHIJ",
              "a returned name that prefixes an ALREADY BOUND unit binds under its OWN name");
        Check(ref failures, bound.TryGetUuid(live, out var lu) && lu == "VRF_UUID:live",
              "the bound unit keeps its uuid - no identity hijack");
        Check(ref failures, bound.TryGetName("VRF_UUID:live", out var ln) && ln == live,
              "and the reverse map still names it");

        // ---- REVIEW FINDING 3: a candidate registered LATER invalidates a cached resolution ----
        Console.WriteLine();
        Console.WriteLine("=== finding 3: the resolution cache does not outlive its candidate set ===");
        var cache = new NameRegistry();
        cache.Requested("ABCDEFGHIJ.tk1");
        var firstBind = cache.Bind("ABCDEFGHIJ", "VRF_UUID:cache-1");
        Check(ref failures, firstBind.Truncated && firstBind.Name == "ABCDEFGHIJ.tk1",
              "the only candidate visible at that instant resolves (and is cached)");
        cache.Requested("ABCDEFGHIJ.tk2");             // routes/waypoints/materialize register later
        var secondBind = cache.Bind("ABCDEFGHIJ", "VRF_UUID:cache-2");
        Check(ref failures, secondBind.Ambiguous && secondBind.Name == "ABCDEFGHIJ",
              "a second candidate registered later makes the SAME returned name AMBIGUOUS, not cached");
        // ... and dropping the cache entry must NOT un-resolve a unit that is already BOUND: a
        // POSITION text report or a completion callback arrives with the sim's truncated spelling
        // long after order-time names (routes, waypoints, materialize) have been registered.
        var settled = new NameRegistry();
        settled.Requested(requested);
        settled.Bind(returned, "VRF_UUID:settled");
        settled.Requested(requested + " ROUTE");        // registered at order time, invalidates the cache
        Check(ref failures, settled.Resolve(returned) == requested,
              "a SETTLED correlation survives a later Requested() that drops the cache entry");
        Check(ref failures, settled.TryGetUuid(requested, out var su) && su == "VRF_UUID:settled",
              "... and the unit is still found by its requested name");

        // ---- m1 (cold-start review 02b51de): a SECOND ObjectCreated under a name that is
        // EXACTLY a requested name ALREADY BOUND to a different object. Finding 2's awaiting-only
        // rule guards the PREFIX path; the exact-match short-circuit bypasses it, so the write
        // `_uuidByName[resolved] = uuid` used to hand the live unit's identity to the newcomer.
        // The real shape: "510/40~PXY" is exactly MarkingTruncationWidth chars, so any of its four
        // EXPAND children can come back under it. From then on R1 reported the WRONG object's
        // position for that unit and ExecuteTaskOnTick would have tasked it.
        Console.WriteLine();
        Console.WriteLine("=== m1: an already-bound EXACT name is never re-pointed at another object ===");
        var rebind = new NameRegistry();
        const string liveUnit = "510/40~PXY";
        rebind.Requested(liveUnit);
        rebind.Bind(liveUnit, "VRF_UUID:first");
        var second = rebind.Bind(liveUnit, "VRF_UUID:second");
        Check(ref failures, rebind.TryGetUuid(liveUnit, out var ru) && ru == "VRF_UUID:first",
              "a second ObjectCreated under a BOUND requested name does NOT take the unit's uuid");
        Check(ref failures, rebind.TryGetName("VRF_UUID:first", out var rn) && rn == liveUnit,
              "... the reverse map still names the FIRST object");
        Check(ref failures, !rebind.TryGetName("VRF_UUID:second", out _),
              "... and the newcomer is given no name (it is not this unit)");
        Check(ref failures, second.RefusedRebind && second.PriorUuid == "VRF_UUID:first",
              "... and Bind SAYS SO (RefusedRebind + the uuid it kept), so the caller can log ERROR");

        // The DELIBERATE re-create (MaterializeUnit case 3, VrfC2SimService.cs: _recreatePending):
        // the shell is DELETED and the same name re-created as its template, so that one rebind is
        // legitimate and must still work. ExpectRebind is the one-shot allowance for exactly it.
        var recreate = new NameRegistry();
        recreate.Requested(liveUnit);
        recreate.Bind(liveUnit, "VRF_UUID:shell");
        recreate.ExpectRebind(liveUnit);
        var templated = recreate.Bind(liveUnit, "VRF_UUID:template");
        Check(ref failures, !templated.RefusedRebind
                            && recreate.TryGetUuid(liveUnit, out var tu) && tu == "VRF_UUID:template"
                            && recreate.TryGetName("VRF_UUID:template", out var tn) && tn == liveUnit,
              "the MATERIALIZE re-create (shell deleted) DOES rebind - both maps follow the new object");
        var afterwards = recreate.Bind(liveUnit, "VRF_UUID:intruder");
        Check(ref failures, afterwards.RefusedRebind
                            && recreate.TryGetUuid(liveUnit, out var tu2) && tu2 == "VRF_UUID:template",
              "the allowance is ONE-SHOT: the next unexpected rebind is refused again");

        // ---- C1c (2026-09-28, run G1): an AGGREGATE comes back at 30 characters; the request-time key check ----
        // The G1 shape, verbatim: the division container (32 chars) comes back as its first 30, and its HQ member,
        // built within 34, came back as THE SAME 30 characters.
        const string div = "28ID__FRIENDLY_INFANTRY_DIVISION";
        const string divKey = "28ID__FRIENDLY_INFANTRY_DIVISI";
        Check(ref failures, VrfNames.Key(div) == divKey && VrfNames.AggregateMarkingChars == 30
                            && VrfNames.AggregateMarkingField == 31
                            && VrfNames.EntityMarkingChars == NameRegistry.MarkingTruncationWidth,
              "C1c: VrfNames.Key is the first 30 characters - what an aggregate name that overflows its 31-character field " +
              "comes back as; a platform stays at the 10 of MarkingTruncationWidth");
        Check(ref failures, VrfNames.ReturnedAggregateName(div) == divKey
                            && VrfNames.ReturnedAggregateName("4ID__FRIENDLY_INFANTRY_DIVISION") == "4ID__FRIENDLY_INFANTRY_DIVISION"
                            && VrfNames.ReturnedAggregateName("ABC") == "ABC",
              "C1c: the MEASURED rule - 32 characters came back as the first 30, a 31-character name that FITS the field came " +
              "back whole (runs 20260928T101531Z / T102541Z), a short one whole");
        var g1 = new NameRegistry();
        g1.Requested(div);
        g1.Bind(divKey, "VRF_UUID:div");
        Check(ref failures, g1.KeyConflict("28ID__FRIENDLY_INFANTRY_DIVISI.HQ1", truncatable: true) == div,
              "C1c FAIL-FIRST: the 34-character member name '28ID__FRIENDLY_INFANTRY_DIVISI.HQ1' collides with its own " +
              "container within 30 - the check names the container");
        Check(ref failures, g1.KeyConflict("28ID__FRIENDLY_INFANTRY_DI.HQ1", truncatable: true) == null,
              "C1c: the 30-character member name '28ID__FRIENDLY_INFANTRY_DI.HQ1' collides with nothing");
        Check(ref failures, g1.KeyConflict(divKey, truncatable: true) == div,
              "C1c: a name EQUAL to a cut name's 30 characters is a collision (its exact callback would hijack the cut one)");
        var twins = new NameRegistry();
        twins.Requested("4ID/III_Corps__FOUR_TH_US_INFANTRY_DIVISION");
        string twin = twins.UniqueTruncatable("4ID/III_Corps__FOUR_TH_US_INFANTRY_DIVISION_REAR", null, out var met);
        Check(ref failures, met == "4ID/III_Corps__FOUR_TH_US_INFANTRY_DIVISION" && twin == "4ID/III_Corps__FOUR_TH_US_IN~2"
                            && twins.KeyConflict(twin, truncatable: true) == null,
              $"C1c: two unit names alike in their first 30 - the second is requested as its unique 30-character form " +
              $"('{twin}')");
        Check(ref failures, twins.UniqueTruncatable("ABCDEFGHIJ", null, out var none) == "ABCDEFGHIJ" && none == null,
              "C1c: a name that collides with nothing is requested UNCHANGED");
        // A WHOLE name (route, waypoint, control area, line/point) comes back intact, so it is never a candidate for a
        // unit's cut callback. FAIL-FIRST with the old registration: the same route makes the unit AMBIGUOUS.
        const string unitLong = "48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE";
        const string routeLong = "48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE T14 ROUTE";
        var oldReg = new NameRegistry();
        oldReg.Requested(unitLong);
        oldReg.Requested(routeLong);
        Check(ref failures, oldReg.Bind(VrfNames.Key(unitLong), "VRF_UUID:unit").Ambiguous,
              "C1c FAIL-FIRST: a long route registered like a unit makes the unit's 30-character callback AMBIGUOUS");
        var wholeReg = new NameRegistry();
        wholeReg.Requested(unitLong);
        wholeReg.RequestedWhole(routeLong);
        var unitBind = wholeReg.Bind(VrfNames.Key(unitLong), "VRF_UUID:unit");
        Check(ref failures, !unitBind.Ambiguous && unitBind.Name == unitLong && wholeReg.IsWhole(routeLong)
                            && wholeReg.Bind(routeLong, "VRF_UUID:route").Name == routeLong,
              "C1c: registered WHOLE, the route is no candidate - the unit's cut callback resolves to the unit, the route's " +
              "own callback is an exact match");
        Check(ref failures, wholeReg.KeyConflict(VrfNames.Key(unitLong), truncatable: false) == unitLong
                            && wholeReg.KeyConflict(routeLong, truncatable: false) == null,
              "C1c: a whole name EXACTLY equal to a unit's 30-character name is reported (the one way it could take the " +
              "unit's callback); a route sharing only the unit's prefix, or itself (the FIFO duplicate case), is not");
        var upgrade = new NameRegistry();
        upgrade.RequestedWhole("ABCDEFGHIJKLMNOP");
        upgrade.Requested("ABCDEFGHIJKLMNOP");
        Check(ref failures, !upgrade.IsWhole("ABCDEFGHIJKLMNOP"),
              "C1c: a truncatable request wins over an earlier whole one (the conservative reading)");
        // The init rename also sees the SAME init's graphics (not registered yet on the terrain-query path): a unit whose
        // 30 characters equal an area's name would hand its cut callback to the area.
        var graphics = new NameRegistry();
        string beforeArea = graphics.UniqueTruncatable(unitLong, null, out var metArea, new[] { VrfNames.Key(unitLong) });
        Check(ref failures, metArea == VrfNames.Key(unitLong) && beforeArea == "48_IBCT/28ID__FRIENDLY_INFAN~2"
                            && graphics.UniqueTruncatable(unitLong, null, out _, new[] { routeLong }) == unitLong,
              "C1c: a unit whose 30-character form EQUALS a planned graphic's name is renamed ('48_IBCT/28ID__FRIENDLY_INFAN~2'); " +
              "a graphic that only shares its prefix changes nothing");

        // ---- cleanup list: one uuid however many names point at it ----
        Check(ref failures, reg.CreatedUuids().Count == 2,
              "CreatedUuids is DISTINCT (the truncated unit's two names are one object)");

        failures += IdentityChecks();

        Console.WriteLine();
        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>
    /// C1d (2026-09-28, RL-20260928-02): IDENTITY BY UUID. (b) the derived uuid - an independent oracle (Python's
    /// uuid.uuid5 values, pinned), determinism, format; (c) the fallback when VR-Forces returns a uuid we did not request
    /// - WARN + the name rule - and the three log-line shapes the G1-2 registration greps; (d) a report's marking resolved
    /// marking -> uuid -> name, for a cut marking and for two objects whose markings coincide at 30 (the residual that is
    /// C1c's job); plus the binding rules around it (a name-rule reading displaced, a uuid binding never re-pointed by a
    /// name, the announced template re-create, the census).
    /// </summary>
    private static int IdentityChecks()
    {
        int failures = 0;
        Console.WriteLine();
        Console.WriteLine("=== C1d: identity by uuid (RL-20260928-02) ===");

        // (b) THE DERIVED UUID. An INDEPENDENT oracle first: Python 3.12's uuid.uuid5, run 2026-09-28, gives these.
        Check(ref failures, IdentityUuid.V5("6ba7b810-9dad-11d1-80b4-00c04fd430c8", "python.org")
                            == "886313e1-3b8a-5372-9b90-0c9aee199e5d",
              "(b) RFC 4122 v5 matches the reference: uuid5(NAMESPACE_DNS, 'python.org') = 886313e1-3b8a-5372-9b90-0c9aee199e5d");
        Check(ref failures, IdentityUuid.V5(IdentityUuid.RfcUrlNamespace, IdentityUuid.NamespaceSource) == IdentityUuid.Namespace
                            && IdentityUuid.Namespace == "485b28e7-1cc7-534b-b6a6-9beddf07a1b1",
              "(b) the interface namespace 485b28e7-1cc7-534b-b6a6-9beddf07a1b1 IS uuid5(NAMESPACE_URL, its source URL) - " +
              "anyone can re-derive it, and it is pinned (changing it changes every derived uuid)");
        const string ibct = "dd3d21b2-c5e0-d45a-9fba-b4b8bb879e6a";   // 48 IBCT's C2SIM uuid (IRONSTORM_CUTA init)
        const string div28 = "200d3a3f-8f36-4951-9459-c748527896ba";  // 28ID's
        const string in112 = "8d5b2ba6-73c1-6c55-812c-7c8078ea8c97";  // 1-112 IN's
        Check(ref failures, IdentityUuid.Derive(ibct, "HQ1") == "d003da2c-d813-5e00-ae78-3fbfbfe7dd71"
                            && IdentityUuid.Derive(ibct, "CAV1") == "46c65670-fcf0-5fe2-b0da-3e90b753db71"
                            && IdentityUuid.Derive(div28, "HQ1") == "c8d5c7b5-89b5-5584-9ae9-ec2e5a958ff4"
                            && IdentityUuid.Derive(in112, IdentityUuid.RecreateSuffix) == "aef29587-8a62-52c6-a09a-bd30457bd384",
              "(b) derived uuids equal Python's uuid5(namespace, '<parent>/<suffix>') for G1's containers (48 IBCT HQ1 and " +
              "CAV1, 28ID HQ1, 1-112 IN recreate)");
        string d1 = IdentityUuid.Derive(ibct, "INF1RIF1");
        Check(ref failures, d1 == IdentityUuid.Derive(ibct, "INF1RIF1")
                            && d1 == IdentityUuid.Derive("VRF_UUID:" + ibct.ToUpperInvariant(), "INF1RIF1")
                            && d1 == IdentityUuid.Derive("{" + ibct + "}", "INF1RIF1"),
              "(b) DETERMINISTIC: the same parent and suffix give the same uuid every time, whatever the parent's spelling " +
              "(case, VRF_UUID: prefix, braces)");
        Check(ref failures, d1 != IdentityUuid.Derive(ibct, "INF1RIF2") && d1 != IdentityUuid.Derive(div28, "INF1RIF1")
                            && IdentityUuid.Derive(ibct, "INF1RIF1") != IdentityUuid.Derive(ibct, "inf1rif1"),
              "(b) a different suffix or a different parent gives a different uuid (the suffix is case-sensitive)");
        Check(ref failures, System.Text.RegularExpressions.Regex.IsMatch(d1,
                                "^[0-9a-f]{8}-[0-9a-f]{4}-5[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$")
                            && IdentityUuid.VersionOf(d1) == '5' && IdentityUuid.IsRfc4122Variant(d1),
              $"(b) FORMAT: lower-case 8-4-4-4-12, version nibble 5, variant 10xx ('{d1}')");
        Check(ref failures, IdentityUuid.Derive("", "HQ1") == "" && IdentityUuid.Derive("not-a-uuid", "HQ1") == ""
                            && IdentityUuid.Derive(ibct, "") == "",
              "(b) no parent uuid (or no suffix) -> no derived uuid, never a guess");
        Check(ref failures, IdentityUuid.ForC2SimUnit(ibct.ToUpperInvariant()) == ibct && IdentityUuid.ForC2SimUnit("  " + ibct) == ibct
                            && IdentityUuid.ForC2SimUnit("1-112_IN") == "" && IdentityUuid.ForC2SimUnit(null) == "",
              "(b) an init unit is created under its OWN C2SIM uuid, canonical lower-case; a C2SIM 'uuid' that is not one " +
              "(the schema's UUIDBaseType) is not passed at all");

        // (c) THE FALLBACK: a uuid we did NOT request.
        var fb = new NameRegistry();
        const string unitA = "2/1_AD/25_~PXY";
        const string askedA = "11111111-2222-4333-8444-555555555555", gotA = "99999999-8888-4777-8666-555555555555";
        fb.Requested(unitA);
        Check(ref failures, fb.RequestedUuid(unitA, askedA) == null && fb.HasRequestedUuid(unitA),
              "(c) the uuid a unit is created under is registered with its requested name");
        var regenerated = fb.BindCreated("2/1_AD/25_", "VRF_UUID:" + gotA);
        Check(ref failures, !regenerated.ByUuid && regenerated.Name == unitA && regenerated.Truncated && regenerated.FallbackWarn
                            && fb.TryGetUuid(unitA, out var fbu) && fbu == "VRF_UUID:" + gotA && !fb.IsBoundByUuid(unitA),
              "(c) VR-Forces REGENERATED the uuid: the callback falls through to the NAME rule (its cut marking resolves to " +
              "the unit) and the caller is told to WARN; the binding is the name rule's, not the uuid's");
        Check(ref failures, IdentityLines.NotRequested(gotA, "2/1_AD/25_")
                            == "IDENTITY: VR-Forces returned uuid 99999999-8888-4777-8666-555555555555 for marking '2/1_AD/25_', " +
                               "not one we requested - bound by name (NameRegistry)",
              "(c) the WARN line, exactly: IDENTITY: VR-Forces returned uuid <x> for marking '<m>', not one we requested - " +
              "bound by name (NameRegistry)");
        const string route = "T14 ROUTE";
        fb.RequestedWhole(route);
        var routeBind = fb.BindCreated(route, "VRF_UUID:0badc0de-0000-4000-8000-000000000001");
        Check(ref failures, !routeBind.ByUuid && routeBind.Name == route && !routeBind.FallbackWarn,
              "(c) an object NEVER given a uuid (a task route) binds by name SILENTLY, as it always has - no WARN");
        var stranger = fb.BindCreated("M1A2 37", "VRF_UUID:0badc0de-0000-4000-8000-000000000002");
        Check(ref failures, !stranger.ByUuid && stranger.FallbackWarn,
              "(c) a callback neither the uuid nor the name rule can attribute is said at WARN too");
        Check(ref failures, fb.RequestedUuid("ANOTHER_UNIT", askedA) == unitA,
              "(c) one uuid requested for two names is REFUSED for the second (the first stands; the caller logs ERROR - " +
              "VR-Forces would regenerate it)");

        // THE BINDING BY UUID, and the INFO line.
        var ok = new NameRegistry();
        const string contLong = "48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE";   // 50 chars: comes back cut at 30
        ok.Requested(contLong);
        ok.RequestedUuid(contLong, ibct);
        var okBind = ok.BindCreated(VrfNames.Key(contLong), "VRF_UUID:" + ibct);
        Check(ref failures, okBind.ByUuid && !okBind.FallbackWarn && okBind.Name == contLong && okBind.Truncated
                            && !okBind.Ambiguous && ok.IsBoundByUuid(contLong)
                            && ok.TryGetUuid(contLong, out var ou) && ou == "VRF_UUID:" + ibct
                            && ok.TryGetName("VRF_UUID:" + ibct, out var on) && on == contLong,
              "a callback carrying the REQUESTED uuid binds THAT name exactly, whatever marking came back (here cut to 30) - " +
              "no prefix scan, both maps written");
        Check(ref failures, IdentityLines.CreatedAs(contLong, ibct, VrfNames.Key(contLong))
                            == "IDENTITY: '48_IBCT/28ID__FRIENDLY_INFANTRY_BRIGADE_TASK_FORCE' created as uuid " +
                               "dd3d21b2-c5e0-d45a-9fba-b4b8bb879e6a (requested) marking '48_IBCT/28ID__FRIENDLY_INFANTR'",
              "the INFO line, exactly: IDENTITY: '<requested name>' created as uuid <uuid> (requested) marking '<returned>'");
        Check(ref failures, IdentityLines.Summary(36, 36, 0, 0, "x") == "IDENTITY: 36 of 36 objects bound by uuid, 0 by name (x; RL-20260928-02)"
                            && IdentityLines.Summary(35, 36, 1, 0, "x") == "IDENTITY: 35 of 36 objects bound by uuid, 1 by name (x; RL-20260928-02)"
                            && IdentityLines.Summary(34, 36, 1, 1, "x") == "IDENTITY: 34 of 36 objects bound by uuid, 1 by name, 1 not bound (x; RL-20260928-02)",
              "the SUMMARY line, exactly: IDENTITY: N of M objects bound by uuid, K by name[, U not bound] (<scope>; RL-20260928-02)");

        // (d) A REPORT'S MARKING: marking -> uuid -> requested name, first.
        var cut = ok.ResolveMarking(VrfNames.Key(contLong));
        Check(ref failures, cut.Name == contLong && cut.Via == NameRegistry.MarkingVia.Uuid && ok.Resolve(VrfNames.Key(contLong)) == contLong,
              "(d) a completion carrying the CUT 30-character marking resolves to the uuid-bound unit, exactly (Via Uuid)");
        // Two objects whose markings coincide at 30 - G1's own shape (the 34-character member names, C1c's fail-first).
        var twin = new NameRegistry();
        const string m1 = "48_IBCT/28ID__FRIENDLY_IN.INF1RIF1", m2 = "48_IBCT/28ID__FRIENDLY_IN.INF1RIF2";
        string u1 = IdentityUuid.Derive(ibct, "INF1RIF1"), u2 = IdentityUuid.Derive(ibct, "INF1RIF2");
        foreach (var (n, u) in new[] { (m1, u1), (m2, u2) }) { twin.Requested(n); twin.RequestedUuid(n, u); }
        var t1 = twin.BindCreated(VrfNames.ReturnedAggregateName(m1), "VRF_UUID:" + u1);
        var t2 = twin.BindCreated(VrfNames.ReturnedAggregateName(m2), "VRF_UUID:" + u2);
        Check(ref failures, VrfNames.ReturnedAggregateName(m1) == VrfNames.ReturnedAggregateName(m2)
                            && t1.ByUuid && t2.ByUuid && t1.Name == m1 && t2.Name == m2 && !t1.Ambiguous && !t2.Ambiguous
                            && twin.TryGetUuid(m1, out var tu1) && tu1 == "VRF_UUID:" + u1
                            && twin.TryGetUuid(m2, out var tu2) && tu2 == "VRF_UUID:" + u2,
              "(d) two members whose markings COINCIDE at 30 ('" + VrfNames.ReturnedAggregateName(m1) + "') are BOTH bound by " +
              "uuid, each to its own name - the create path no longer needs the names apart");
        var shared = twin.ResolveMarking(VrfNames.ReturnedAggregateName(m1));
        Check(ref failures, shared.Via == NameRegistry.MarkingVia.AmbiguousUuid && shared.Candidates.Count == 2
                            && shared.Candidates.Contains(m1) && shared.Candidates.Contains(m2)
                            && shared.Name == VrfNames.ReturnedAggregateName(m1),
              "(d) THE RESIDUAL, stated: a COMPLETION carrying that shared marking cannot be told apart - it carries no uuid - " +
              "so it is reported AmbiguousUuid naming both, and the name rule refuses to guess (the raw marking comes back, " +
              "and the service sends no report for it). Keeping markings apart at request time is C1c's job");
        var single = new NameRegistry();
        single.Requested(m1); single.RequestedUuid(m1, u1);
        single.BindCreated(VrfNames.ReturnedAggregateName(m1), "VRF_UUID:" + u1);
        Check(ref failures, single.ResolveMarking(VrfNames.ReturnedAggregateName(m1)) is { Via: NameRegistry.MarkingVia.Uuid } r1
                            && r1.Name == m1,
              "(d) the same cut marking carried by ONE uuid-bound object resolves to it exactly");
        // A marking a uuid-bound and a NAME-bound object share is SEEN as shared (every ObjectCreated's marking is recorded).
        var mixed = new NameRegistry();
        mixed.Requested(m1); mixed.RequestedUuid(m1, u1);
        mixed.Requested(VrfNames.ReturnedAggregateName(m1));   // a unit named exactly the cut marking, bound by name
        mixed.BindCreated(VrfNames.ReturnedAggregateName(m1), "VRF_UUID:0badc0de-0000-4000-8000-000000000003");
        mixed.BindCreated(VrfNames.ReturnedAggregateName(m1), "VRF_UUID:" + u1);
        var mix = mixed.ResolveMarking(VrfNames.ReturnedAggregateName(m1));
        Check(ref failures, mix.Via == NameRegistry.MarkingVia.AmbiguousUuid && mix.Name == VrfNames.ReturnedAggregateName(m1),
              "(d) a marking a uuid-bound object shares with a name-bound one is ambiguous too - and the name rule's exact " +
              "match answers, as before C1d");
        var plain = new NameRegistry();
        plain.Requested(unitA);
        plain.Bind("2/1_AD/25_", "VRF_UUID:plain");
        Check(ref failures, plain.ResolveMarking("2/1_AD/25_") is { Via: NameRegistry.MarkingVia.Name } pr && pr.Name == unitA,
              "(d) with no uuid binding at all the pre-C1d rule answers, unchanged");

        // THE BINDING RULES AROUND IT.
        var disp = new NameRegistry();
        const string unitN = "1-112_IN/28ID__FRIENDLY_INFANT", askedN = "22222222-3333-4444-8555-666666666666";
        disp.Requested(unitN); disp.RequestedUuid(unitN, askedN);
        disp.BindCreated(unitN, "VRF_UUID:0badc0de-0000-4000-8000-000000000004");   // an unrequested object, exact name
        var own = disp.BindCreated(unitN, "VRF_UUID:" + askedN);
        Check(ref failures, own.ByUuid && own.DisplacedUuid == "VRF_UUID:0badc0de-0000-4000-8000-000000000004"
                            && disp.TryGetUuid(unitN, out var du) && du == "VRF_UUID:" + askedN
                            && !disp.TryGetName("VRF_UUID:0badc0de-0000-4000-8000-000000000004", out _)
                            && disp.CreatedUuids().Contains("VRF_UUID:0badc0de-0000-4000-8000-000000000004"),
              "a NAME-rule reading of a requested name is DISPLACED when its own uuid arrives (the uuid is certain); the " +
              "displaced object is unattributed but still deleted on stop");
        var intruder = disp.BindCreated(unitN, "VRF_UUID:0badc0de-0000-4000-8000-000000000005");
        Check(ref failures, !intruder.ByUuid && intruder.RefusedRebind && intruder.FallbackWarn
                            && disp.TryGetUuid(unitN, out var du2) && du2 == "VRF_UUID:" + askedN,
              "a uuid binding is NEVER re-pointed by a name-rule callback (refused, WARN + the existing ERROR)");
        // MaterializeUnit case 3: the shell deleted, the unit re-created as its template under a DERIVED uuid.
        var rc = new NameRegistry();
        string recreated = IdentityUuid.Derive(askedN, IdentityUuid.RecreateSuffix);
        rc.Requested(unitN); rc.RequestedUuid(unitN, askedN);
        rc.BindCreated(unitN, "VRF_UUID:" + askedN);
        var unannounced = rc.BindCreated(unitN, "VRF_UUID:" + askedN.Replace('2', '7'));
        rc.ExpectRebind(unitN);
        rc.RequestedUuid(unitN, recreated);
        var remade = rc.BindCreated(unitN, "VRF_UUID:" + recreated);
        Check(ref failures, unannounced.RefusedRebind && remade.ByUuid && !remade.RefusedRebind
                            && rc.TryGetUuid(unitN, out var ru2) && ru2 == "VRF_UUID:" + recreated
                            && rc.ResolveMarking(unitN) is { Via: NameRegistry.MarkingVia.Uuid } rr && rr.Name == unitN,
              "the ANNOUNCED template re-create (MaterializeUnit case 3) moves the name to its derived uuid; the shell and " +
              "the template carry one marking and it still resolves to the one unit");
        var rc2 = new NameRegistry();
        rc2.Requested(unitN); rc2.RequestedUuid(unitN, askedN);
        rc2.BindCreated(unitN, "VRF_UUID:0badc0de-0000-4000-8000-000000000007");   // the shell: its uuid regenerated, by name
        rc2.ExpectRebind(unitN);
        rc2.RequestedUuid(unitN, recreated);
        var remade2 = rc2.BindCreated(unitN, "VRF_UUID:" + recreated);
        var after2 = rc2.Bind(unitN, "VRF_UUID:0badc0de-0000-4000-8000-000000000008");
        Check(ref failures, remade2.ByUuid && remade2.DisplacedUuid.Length == 0 && !remade2.RefusedRebind
                            && after2.RefusedRebind && rc2.TryGetUuid(unitN, out var ru3) && ru3 == "VRF_UUID:" + recreated,
              "... and when the SHELL had been bound by the name rule, the announced re-create still just moves the name (no " +
              "'displaced' reading) and CONSUMES the one-shot allowance - the next unannounced rebind is refused");
        var census = new NameRegistry();
        foreach (var (n, u) in new[] { ("A_UNIT", askedA), ("B_UNIT", askedN) }) { census.Requested(n); census.RequestedUuid(n, u); }
        census.Requested("C_UNIT");
        census.BindCreated("A_UNIT", "VRF_UUID:" + askedA);
        census.BindCreated("B_UNIT", "VRF_UUID:0badc0de-0000-4000-8000-000000000006");   // regenerated: by name
        var c = census.IdentityCensus(new[] { "A_UNIT", "B_UNIT", "C_UNIT" });
        Check(ref failures, c.ByUuid == 1 && c.ByName == 1 && c.Unbound == 1,
              $"the census counts by uuid / by name / not bound (1 / 1 / 1; saw {c.ByUuid} / {c.ByName} / {c.Unbound})");
        return failures;
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
