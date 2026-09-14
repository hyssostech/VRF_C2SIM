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
