using Microsoft.Extensions.Configuration;

namespace VrfC2SimApp;

/// <summary>
/// D1 (RL-20260927-01; docs/PLAN_MOVEMENT_2026-09-27.md row D1): a MEMBERLESS aggregate on the aggregate model
/// set counts as ONE position for the task judges; on the entity model set it is still skipped. Its own section of
/// `--rulings-selftest` - no bridge, no MAK, no clock:
///
///   (d1) THE SOURCE - UnitPositionPolicy.SourceFor, the whole table, both model sets.
///   (d2) THE KEY - Vrf:ModelSet: the accepted spellings, the safe default, and the configuration path the
///        service reads it by (json and the environment), which works before and after the key lands on main.
///   (d3) THE JUDGES ON ONE POSITION - the REAL ArrivalPolicy and StallPolicy on the sample the reader now
///        yields for a leaf: arrival scored 1 of 1, a stop at a lake reported; with the pre-D1 reader (no
///        sample) as the fail-first arm - the silence D1 exists to end.
///   (d4) THE ENTITY-LEVEL TRANSIENT - members not reflected yet are still NOT judged (no false stall, no
///        false arrival), and a unit that stays memberless is said once, never judged.
///   (d5) SOURCE TRIPWIRES - the service reads positions through ONE reader that asks SourceFor, and both
///        judges use it.
/// </summary>
public static class AggregateLeafSelfTest
{
    public static int Run()
    {
        int failures = 0;

        // ------------------------------------------------------------------------------ (d1) ----
        foreach (bool agg in new[] { false, true })
        {
            string set = agg ? "AggregateTacticalLevel" : "EntityLevel";
            Check(ref failures, UnitPositionPolicy.SourceFor(isAggregate: false, memberCount: 0, aggregateModelSet: agg)
                                == UnitPositionSource.Platform,
                  $"(d1) {set}: a PLATFORM is its own position, as before");
            Check(ref failures, UnitPositionPolicy.SourceFor(true, 6, agg) == UnitPositionSource.Members,
                  $"(d1) {set}: an aggregate WITH members is judged on its members, as before");
        }
        Check(ref failures, UnitPositionPolicy.SourceFor(true, 0, aggregateModelSet: true) == UnitPositionSource.AggregateLeaf,
              "(d1) AggregateTacticalLevel: a MEMBERLESS aggregate is an aggregate-level leaf - ONE position (D1)");
        Check(ref failures, UnitPositionPolicy.SourceFor(true, 0, aggregateModelSet: false) == UnitPositionSource.NotYetJudgeable,
              "(d1) EntityLevel: a MEMBERLESS aggregate is NOT judged - its members have not reflected yet, or it is a shell " +
              "(the pre-D1 behaviour, unchanged)");

        // ------------------------------------------------------------------------------ (d2) ----
        foreach (var v in new[] { "AggregateTacticalLevel", "aggregatetacticallevel", " AggregateTacticalLevel ",
                                  "AggregateTacticalLevel.sms", "AGGREGATETACTICALLEVEL.SMS" })
            Check(ref failures, UnitPositionPolicy.TryParseModelSet(v, out bool a) && a,
                  $"(d2) Vrf:ModelSet '{v}' is the aggregate model set");
        foreach (var v in new[] { null, "", "   ", "EntityLevel", "entitylevel", "EntityLevel.sms" })
            Check(ref failures, UnitPositionPolicy.TryParseModelSet(v, out bool a) && !a,
                  $"(d2) Vrf:ModelSet {(v == null ? "absent" : "'" + v + "'")} is the ENTITY model set - the safe default");
        foreach (var v in new[] { "Aggregate", "AggregateLevel", "EntityLevl", "Aggregate Tactical Level" })
            Check(ref failures, !UnitPositionPolicy.TryParseModelSet(v, out bool a) && !a,
                  $"(d2) an unrecognised Vrf:ModelSet '{v}' is NOT taken as aggregate - it falls back to EntityLevel, " +
                  "and the start-up line WARNS");
        {
            // The service reads config.GetSection("Vrf")["ModelSet"] - the RAW key, through json and environment
            // alike. Before the key lands on main: absent -> entity. After: the runner's Vrf__ModelSet wins.
            var none = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
                           { ["Vrf:ClientId"] = "STP" }).Build();
            Check(ref failures, none.GetSection("Vrf")["ModelSet"] == null
                                && UnitPositionPolicy.TryParseModelSet(none.GetSection("Vrf")["ModelSet"], out bool a0) && !a0,
                  "(d2) the key ABSENT (main today): the read is null and resolves to EntityLevel");
            var json = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
                           { ["Vrf:ModelSet"] = "EntityLevel" }).Build();
            const string EnvKey = "Vrf__ModelSet";
            string saved = Environment.GetEnvironmentVariable(EnvKey);
            try
            {
                Environment.SetEnvironmentVariable(EnvKey, "AggregateTacticalLevel");
                var withEnv = new ConfigurationBuilder()
                                  .AddInMemoryCollection(new Dictionary<string, string> { ["Vrf:ModelSet"] = "EntityLevel" })
                                  .AddEnvironmentVariables().Build();
                Check(ref failures, UnitPositionPolicy.TryParseModelSet(json.GetSection("Vrf")["ModelSet"], out bool aj) && !aj
                                    && UnitPositionPolicy.TryParseModelSet(withEnv.GetSection("Vrf")["ModelSet"], out bool ae) && ae,
                      $"(d2) json 'EntityLevel' is entity; {EnvKey}=AggregateTacticalLevel (what the runner's -ModelSet " +
                      "exports) overrides it - the same read the service makes");
            }
            finally { Environment.SetEnvironmentVariable(EnvKey, saved); }
        }
        {
            string repo = FindRepoRoot();
            string app = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.json");
            string demo = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.Demo.json");
            if (app != null && File.Exists(app) && File.Exists(demo))
            {
                var shipped = new ConfigurationBuilder().AddJsonFile(app).AddJsonFile(demo).Build();
                Check(ref failures, UnitPositionPolicy.TryParseModelSet(shipped.GetSection("Vrf")["ModelSet"], out bool s) && !s,
                      "(d2) the SHIPPED settings resolve to the ENTITY model set (absent, or 'EntityLevel' once the " +
                      "aggregate lane lands) - D1 changes nothing on an entity-level run");
            }
            else Check(ref failures, false, $"(d2) the shipped settings files are on disk ({app})");
        }

        // ------------------------------------------------------------------------------ (d3) ----
        // A 5 km route; the leaf's centre point is sampled like a platform's. Arrival: the REAL
        // ArrivalPolicy with the shipped defaults (radius 500, fraction 0.5, min travel 100, approach 0.5).
        const double RouteM = 5000.0, FromStartM = 5000.0;
        var arrived = ArrivalPolicy.DecideWithTraversal(
            new[] { new ArrivalPolicy.MemberSample(40.0, 4960.0, 5000.0) }, totalMembers: 1,
            500.0, 0.5, RouteM, 100.0, 0.5, FromStartM);
        Check(ref failures, arrived.Arrived && arrived.Within == 1 && arrived.Total == 1,
              $"(d3) a leaf whose centre point is 40 m from the last vertex after 4,960 m of travel ARRIVES - scored " +
              $"{arrived.Within} of {arrived.Total} (D1)");
        var atLake = ArrivalPolicy.DecideWithTraversal(
            new[] { new ArrivalPolicy.MemberSample(2000.0, 3000.0, 5000.0) }, 1, 500.0, 0.5, RouteM, 100.0, 0.5, FromStartM);
        Check(ref failures, !atLake.Arrived && atLake.Within == 0,
              "(d3) a leaf stopped at a lake 2,000 m short does NOT arrive");
        var noSample = ArrivalPolicy.DecideWithTraversal(new ArrivalPolicy.MemberSample[0], 1, 500.0, 0.5, RouteM, 100.0,
                                                         0.5, FromStartM);
        Check(ref failures, !noSample.Arrived,
              "FAIL-FIRST (d3): with no sample - the pre-D1 reader skips a memberless aggregate - the same arrival is " +
              "NEVER scored");
        // Stall: the REAL StallPolicy with the shipped defaults (50 m, 1 member with data).
        var stuck = StallPolicy.Decide(new[] { 3.0 }, totalMembers: 1, moveMeters: 50.0, minMembersWithData: 1);
        var moving = StallPolicy.Decide(new[] { 400.0 }, 1, 50.0, 1);
        Check(ref failures, stuck.Stalled && stuck.Total == 1 && !moving.Stalled,
              "(d3) the watchdog on ONE position: a centre point that moved 3 m in the window is STALLED (the lake stop " +
              "is reported), one that moved 400 m is not");
        var silent = StallPolicy.Decide(new double[0], 1, 50.0, 1);
        Check(ref failures, !silent.Stalled,
              "FAIL-FIRST (d3): with no sample the same stop is never called - the silence D1 ends");

        // The reader, replayed on the real SourceFor: what TryReadUnitPositions yields per case.
        var own = (Lat: 54.00, Lon: 23.00);
        var leaf = Read(isAggregate: true, members: 0, aggregateModelSet: true, own);
        Check(ref failures, leaf.Ok && leaf.Total == 1 && leaf.Positions.Count == 1 && leaf.Positions.ContainsKey("VRF_UUID:unit")
                            && leaf.Source == UnitPositionSource.AggregateLeaf,
              "(d3) the reader: an aggregate-level leaf yields ONE sample (its own centre point, keyed by its own uuid, as " +
              "the dispatch baseline and the stall ring key it), total 1");
        var leafUnread = Read(true, 0, true, null);
        Check(ref failures, leafUnread.Ok && leafUnread.Total == 1 && leafUnread.Positions.Count == 0,
              "(d3) ... a centre point that cannot be read this tick still counts 1 against arrival (like an unreadable member)");

        // ------------------------------------------------------------------------------ (d4) ----
        var transient = Read(true, 0, aggregateModelSet: false, own);
        Check(ref failures, !transient.Ok && transient.Source == UnitPositionSource.NotYetJudgeable,
              "(d4) EntityLevel: an aggregate whose members have not reflected yet yields NO sample - no false stall, no " +
              "false arrival while they arrive");
        var reflected = Read(true, 4, false, own);
        Check(ref failures, reflected.Ok && reflected.Source == UnitPositionSource.Members && reflected.Total == 4,
              "(d4) ... and once they reflect it is judged on its members, as before");
        Check(ref failures,
              !UnitPositionPolicy.ShouldWarnMemberless(UnitPositionSource.NotYetJudgeable, 30.0)
              && UnitPositionPolicy.ShouldWarnMemberless(UnitPositionSource.NotYetJudgeable, 60.0)
              && !UnitPositionPolicy.ShouldWarnMemberless(UnitPositionSource.AggregateLeaf, 600.0)
              && !UnitPositionPolicy.ShouldWarnMemberless(UnitPositionSource.Members, 600.0),
              "(d4) an entity-level aggregate still memberless 60 s after dispatch is SAID (once), never judged; a leaf " +
              "and a unit with members are never warned about");

        // Start-up line.
        string lineAgg = UnitPositionPolicy.StartupLine("AggregateTacticalLevel", true, true);
        string lineEnt = UnitPositionPolicy.StartupLine(null, true, false);
        string lineBad = UnitPositionPolicy.StartupLine("Aggregate", false, false);
        Check(ref failures, lineAgg.Contains("RL-20260927-01") && lineAgg.Contains("ONE position")
                            && lineEnt.Contains("(not set)") && lineEnt.Contains("NOT judged")
                            && lineBad.Contains("WARNING") && lineBad.Contains("EntityLevel is used")
                            && (lineAgg + lineEnt + lineBad).All(ch => ch < 128),
              "(d4) the start-up line names RL-20260927-01, says both states, WARNS on an unrecognised value, and is ASCII");

        // ------------------------------------------------------------------------------ (d5) ----
        string repoRoot = FindRepoRoot();
        string service = repoRoot == null ? null : Path.Combine(repoRoot, "src", "VrfC2SimApp", "VrfC2SimService.cs");
        Check(ref failures, service != null && File.Exists(service), $"(d5) the service source is on disk ({service})");
        if (service == null || !File.Exists(service)) return failures;
        string src = File.ReadAllText(service);
        string reader = Between(src, "private bool TryReadUnitPositions(", "private void NoteMemberlessSkip(");
        Check(ref failures, src.Contains("=> TryReadUnitPositions(name, out positions, out total, out _);"),
              "(d5) TryReadMemberPositions (the dispatch baseline and the engage fallback's reader) delegates to the ONE reader");
        Check(ref failures, reader.Contains("UnitPositionPolicy.SourceFor(isAggregate: true, members?.Count ?? 0, _aggregateModelSet)")
                            && reader.Contains("source == UnitPositionSource.AggregateLeaf")
                            && reader.Contains("total = 1;")
                            && reader.Contains("_bridge.TryGetEntityGeodetic(vrfUuid, out var own)"),
              "(d5) the reader asks SourceFor and, for a leaf, returns ONE sample from the unit's own reflected position");
        Check(ref failures, CountOf(src, "TryReadUnitPositions(name, out var positions, out int total, out var positionSource)") == 2
                            && CountOf(src, "NoteMemberlessSkip(name, rec, positionSource, now);") == 2,
              "(d5) BOTH judges (arrival evidence, progress watchdog) read through it and say an entity-level memberless skip");
        Check(ref failures, CountOf(src, "if (!TryReadMemberPositions(name, out var positions, out int total)) continue;") == 0,
              "(d5) no judge reads positions any other way");
        Check(ref failures, src.Contains("config.GetSection(\"Vrf\")[\"ModelSet\"]")
                            && src.Contains("UnitPositionPolicy.TryParseModelSet(_modelSetRaw, out _aggregateModelSet)")
                            && src.Contains("UnitPositionPolicy.StartupLine(_modelSetRaw, _modelSetRecognised, _aggregateModelSet)"),
              "(d5) the service reads the RAW Vrf:ModelSet key, parses it with the policy and says it at start-up");
        return failures;
    }

    private readonly record struct ReadResult(bool Ok, int Total, Dictionary<string, (double Lat, double Lon)> Positions,
                                              UnitPositionSource Source);

    /// <summary>TryReadUnitPositions' aggregate branch, on the REAL SourceFor (the service is pinned to the same
    /// shape by the (d5) tripwires): the unit's own uuid is "VRF_UUID:unit" and its members "VRF_UUID:m1".. .</summary>
    private static ReadResult Read(bool isAggregate, int members, bool aggregateModelSet, (double Lat, double Lon)? own)
    {
        var positions = new Dictionary<string, (double Lat, double Lon)>(StringComparer.Ordinal);
        var source = UnitPositionPolicy.SourceFor(isAggregate, members, aggregateModelSet);
        switch (source)
        {
            case UnitPositionSource.Platform:
            case UnitPositionSource.AggregateLeaf:
                if (own is (double lat, double lon)) positions["VRF_UUID:unit"] = (lat, lon);
                return new ReadResult(true, 1, positions, source);
            case UnitPositionSource.Members:
                for (int i = 1; i <= members; i++) positions["VRF_UUID:m" + i] = (54.0 + i * 1e-4, 23.0);
                return new ReadResult(true, members, positions, source);
            default:
                return new ReadResult(false, 0, positions, source);
        }
    }

    private static string Between(string s, string from, string to)
    {
        int a = s.IndexOf(from, StringComparison.Ordinal);
        if (a < 0) return "";
        int b = s.IndexOf(to, a + from.Length, StringComparison.Ordinal);
        return b < 0 ? s.Substring(a) : s.Substring(a, b - a);
    }

    private static int CountOf(string s, string needle)
    {
        int n = 0;
        for (int i = s.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = s.IndexOf(needle, i + 1, StringComparison.Ordinal)) n++;
        return n;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "data", "COA-STP1_Order.xml"))) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
