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
