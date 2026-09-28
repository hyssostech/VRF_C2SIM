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

        Console.WriteLine();
        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
