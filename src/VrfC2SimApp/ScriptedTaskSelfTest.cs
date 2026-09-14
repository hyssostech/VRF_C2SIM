using System.Globalization;
using VrfC2Sim;

namespace VrfC2SimApp;

/// <summary>
/// Offline round-trip of the SCRIPTED-TASK VARIABLE channel (<c>--scripted-task-selftest</c>).
/// Build item V2 of docs/experiments/TASK_VOCABULARY_ASSESSMENT_2026-09-14.md.
///
/// No VR-Forces, no federation, nothing sent: VrfBridge.DescribeScriptVars is static and builds
/// a real DtScriptedTaskTask through the SAME marshalling helper RunScriptedTask and
/// SendScriptedSet use (VrfFacade.cpp addScriptVar), then reads every variable BACK out of the
/// vendor's own DtRwVariableBindings. It still needs the MAK DLLs on PATH, like
/// --typemap-selftest, because the bridge assembly loads them.
///
/// WHAT IT LOCKS, and why each line can fail:
///  - THE VENDOR'S ACCEPTED TYPES. C:\MAK\vrforces5.2d\include\vrftasks\scriptedTaskTask.h:90-106
///    declares DtScriptedTask::setValue for bool (:90), int (:91), double (:92),
///    std::string (:93), DtString (:94), const char* (:95), DtUUID (:96), DtVector (:97),
///    DtEntityType (:98), DtTaitBryan (:99) and the vector/map forms (:100-106). Before V2 the
///    facade modelled only DtUUID and double, so reconnoiter-route (patrol:Bool,
///    duration:Integer), company_seize (twoAssault:Bool) and unit_movement_simplified
///    (destination:Location3D, useRoads:Bool) could not be parameterised at all. Each case below
///    asserts the variable came back as the vendor's DOCUMENTED scripted-task data type - the
///    DtScriptedTask*Variable constants of vrfutil/vrfScriptedTasksConstants.h:
///      bool -> "checkbox", int -> "integer", double -> "double", string -> "string",
///      DtUUID -> "simulationobject", DtVector -> "location".
///    A wrong overload (e.g. a bool silently taken as int) changes that string and FAILS here.
///  - THE VALUE. Each case asserts the value decoded from the CONCRETE reader/writer the vendor
///    chose equals what went in. That is what proves the cast, not just the type label: if a
///    location were stored as something other than a DtRwVector the value field comes back "?".
///  - THE GEODETIC FRAME. A location is handed to the vendor GEOCENTRIC (vrfRemoteController.h
///    :991-1011 "the position needs to be in geocentric coordinates"); the read-back converts it
///    back to degrees/metres, so a missing or doubled deg/rad conversion fails on lat/lon and a
///    dropped altitude fails on alt.
///  - THE DATA-TYPE BINDING ITSELF. setValue writes variables() AND variableDataTypes()
///    (scriptedTaskTask.h:140-146); the addVariable-only form V2 replaced wrote only the first,
///    so every dataType assertion below would report "?" under the old marshalling.
/// </summary>
public static class ScriptedTaskSelfTest
{
    private static int _fail;

    // The vendor's scripted-task type constants (vrfutil/vrfScriptedTasksConstants.h).
    private const string TypeCheckBox    = "checkbox";
    private const string TypeInteger     = "integer";
    private const string TypeDouble      = "double";
    private const string TypeString      = "string";
    private const string TypeSimObject   = "simulationobject";
    private const string TypeLocation    = "location";

    // A VR-Forces uuid carrying the special marking: "VRF_UUID:<uuid>" (uuid.h:76-82, :124-129
    // uuidMarking()/makeStringUUID). This is the form the ObjectCreated callback hands the
    // service, so it is the runtime form for anything VR-Forces created and reported back.
    private const string AreaUuid = "VRF_UUID:f615fc10-4402-869a-2efc-4658e4d5c0fe";
    // The same area as C2SIM writes it (COA-STP1_Initialization.xml, area BANDIT_II) - no
    // "VRF_UUID:" prefix. The interface creates its tactical graphics WITH this bare string as
    // the starting uuid (VrfFacade.cpp CreateControlArea), so this form is the live one for
    // every init graphic; the case below records what the binding does with it.
    private const string BareC2SimUuid = "f615fc10-4402-869a-2efc-4658e4d5c0fe";
    // A string that is NOT uuid-shaped: the marking-text path, for contrast.
    private const string MarkingText = "1-35_AR_HQ";

    public static int Run()
    {
        Console.WriteLine("=== scripted-task variable self-test (V2) ===");
        Console.WriteLine("  vendor contract: scriptedTaskTask.h:90-106 (setValue overloads); type");
        Console.WriteLine("  constants: vrfutil/vrfScriptedTasksConstants.h. Nothing is sent to VR-Forces.");

        // ---- one variable of each modelled kind, each in its own task ----
        One("ObjectUuid -> simulationobject (a tactical graphic / unit reference)",
            ScriptVar.Object("objective", AreaUuid), TypeSimObject, AreaUuid);

        One("Real -> double (the only numeric kind that existed before V2)",
            ScriptVar.Number("trailingDistance", 250.5), TypeDouble, "250.500000");

        One("Bool true -> checkbox (reconnoiter-route 'patrol', company_seize 'twoAssault')",
            ScriptVar.Flag("patrol", true), TypeCheckBox, "true");

        One("Bool false -> checkbox (a false must NOT be dropped or become 0.0)",
            ScriptVar.Flag("useRoads", false), TypeCheckBox, "false");

        One("Integer -> integer (reconnoiter-route 'duration' in seconds)",
            ScriptVar.Count("duration", 300), TypeInteger, "300");

        One("Integer 0 -> integer (a zero must survive as an integer, not vanish)",
            ScriptVar.Count("rounds", 0), TypeInteger, "0");

        One("Integer negative -> integer",
            ScriptVar.Count("offsetMeters", -75), TypeInteger, "-75");

        One("String -> string (call signs, formation names, sheaf/fuse selectors)",
            ScriptVar.Text("callSign", "1-35_AR"), TypeString, "1-35_AR");

        One("String empty -> string (an empty value must still bind, with its type)",
            ScriptVar.Text("label", ""), TypeString, "");

        // ---- locations: the kind with a coordinate-frame conversion in the path ----
        LocationCase("Location -> location, ground point (COA-STP1 area BANDIT_II vertex)",
                     "destination", 34.6334050352462, -116.734454302635, 0.0);
        LocationCase("Location -> location, with altitude (alt must not be dropped)",
                     "holdPoint", 34.43378269637718, -116.97201656170652, 1234.5);
        LocationCase("Location -> location, southern/eastern hemisphere sign handling",
                     "farPoint", -33.8688, 151.2093, 15.0);

        // ---- the non-prefixed uuid strings the interface actually passes ----
        // uuid.h:76-82: a string without the "VRF_UUID:" marking "will have an invalid UUID" and
        // may be kept as object MARKING TEXT. The interface hands VR-Forces bare C2SIM uuids for
        // every tactical graphic it creates (VrfFacade.cpp CreateControlArea startingUUID), so
        // both rows below are live shapes. They are a REGRESSION LOCK on the binding (nothing is
        // dropped or mangled), not evidence about how the sim RESOLVES either form - see the
        // limit printed under them.
        One("bare C2SIM uuid (uuid-SHAPED, no prefix) binds and comes back with the VRF marking",
            ScriptVar.Object("objectiveByC2SimUuid", BareC2SimUuid),
            TypeSimObject, "VRF_UUID:" + BareC2SimUuid);
        One("non-uuid-shaped string binds too, and comes back with the VRF marking as well",
            ScriptVar.Object("byMarking", MarkingText), TypeSimObject, "VRF_UUID:" + MarkingText);
        Console.WriteLine("       LIMIT OF THIS INSTRUMENT, stated so nobody over-reads the two lines above:");
        Console.WriteLine("       both forms bind, and DtRwUUID::uuidString() (rwUUID.h:82) prefixes BOTH with");
        Console.WriteLine("       \"VRF_UUID:\", so this round-trip does NOT tell a parsed uuid from marking text.");
        Console.WriteLine("       That distinction is DtUUID::uuidType()/isBoostUUID() (uuid.h:113-116) and only a");
        Console.WriteLine("       live run shows which one the SIM resolves. What is settled: neither string is");
        Console.WriteLine("       dropped, and the C2SIM-uuid form is the one the init graphics already use.");

        // ---- a MIXED list in ONE task: order preserved, kinds do not bleed into each other ----
        {
            var vars = new List<ScriptVar>
            {
                ScriptVar.Object("route", AreaUuid),
                ScriptVar.Count("duration", 600),
                ScriptVar.Flag("patrol", true),
                ScriptVar.Flag("deployDrones", false),
                ScriptVar.Number("speedMps", 8.25),
                ScriptVar.Text("posture", "Hasty-Attack"),
                ScriptVar.Place("objectivePoint", new Geodetic { LatDeg = 34.5, LonDeg = -116.8, AltMeters = 42.0 }),
            };
            var lines = VrfBridge.DescribeScriptVars(vars);
            Report("mixed 7-variable task: one line per variable, in order",
                   lines.Count == vars.Count, $"lines={lines.Count} vars={vars.Count}");
            if (lines.Count == vars.Count)
            {
                string[] expectedTypes = { TypeSimObject, TypeInteger, TypeCheckBox, TypeCheckBox,
                                           TypeDouble, TypeString, TypeLocation };
                for (int i = 0; i < lines.Count; i++)
                {
                    var (rw, type, val) = Split(lines[i]);
                    string name = lines[i].Split('|')[0];
                    Report($"  [{i}] {name} -> {expectedTypes[i]}",
                           name == vars[i].Name && type == expectedTypes[i] && val != "?",
                           $"rwType={rw} dataType={type} value={val}");
                }
                // The six modelled kinds must map to six DISTINCT vendor data types - a collapse
                // (everything "double", the pre-V2 behaviour for all non-uuid variables) fails here.
                var distinct = lines.Select(l => Split(l).Type).Distinct().Count();
                Report("the mixed task uses 6 DISTINCT vendor data types (no collapse to 'double')",
                       distinct == 6, $"distinct={distinct}");
            }
        }

        // ---- empty input is not an error ----
        Report("empty variable list -> empty description (no throw)",
               VrfBridge.DescribeScriptVars(new List<ScriptVar>()).Count == 0, "");

        Console.WriteLine(_fail == 0
            ? "scripted-task-selftest: ALL CHECKS PASSED"
            : $"scripted-task-selftest: FAILED ({_fail})");
        return _fail == 0 ? 0 : 1;
    }

    // ---- helpers ----------------------------------------------------------

    private static string Describe(ScriptVar v)
    {
        var lines = VrfBridge.DescribeScriptVars(new List<ScriptVar> { v });
        return lines.Count == 1 ? lines[0] : $"{v.Name}|?|?|?";
    }

    private static (string Rw, string Type, string Value) Split(string line)
    {
        // "<name>|<readerWriterType>|<scriptedTaskDataType>|<value>"; a String value may itself
        // be empty, so split with a count and keep the tail intact.
        var parts = line.Split('|', 4);
        return parts.Length == 4 ? (parts[1], parts[2], parts[3]) : ("?", "?", "?");
    }

    private static void One(string label, ScriptVar v, string expectedType, string expectedValue)
    {
        var (rw, type, val) = Split(Describe(v));
        Report(label, type == expectedType && val == expectedValue,
               $"rwType={rw} dataType={type} value=\"{val}\" (expected type={expectedType} value=\"{expectedValue}\")");
    }

    private static void LocationCase(string label, string name, double lat, double lon, double alt)
    {
        var v = ScriptVar.Place(name, new Geodetic { LatDeg = lat, LonDeg = lon, AltMeters = alt });
        var (rw, type, val) = Split(Describe(v));
        bool ok = type == TypeLocation;
        string detail = $"rwType={rw} dataType={type} value={val}";
        if (ok)
        {
            var f = val.Split(',');
            // Tolerances: the value is printed to 6 decimal places of a degree (~0.11 m) and
            // 3 of a metre, so the assertion is the print precision, not the maths precision.
            // A dropped or doubled deg/rad conversion is off by a factor of 57.3 and fails wide.
            ok = f.Length == 3
                 && double.TryParse(f[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double rlat)
                 && double.TryParse(f[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double rlon)
                 && double.TryParse(f[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double ralt)
                 && Math.Abs(rlat - lat) < 1e-5
                 && Math.Abs(rlon - lon) < 1e-5
                 && Math.Abs(ralt - alt) < 0.05;
            detail += $" (expected {lat.ToString("F6", CultureInfo.InvariantCulture)}," +
                      $"{lon.ToString("F6", CultureInfo.InvariantCulture)}," +
                      $"{alt.ToString("F3", CultureInfo.InvariantCulture)})";
        }
        Report(label, ok, detail);
    }

    private static void Report(string label, bool ok, string detail)
    {
        if (!ok) _fail++;
        Console.WriteLine($"  [{(ok ? "OK" : "FAIL")}] {label}{(detail.Length > 0 ? "  (" + detail + ")" : "")}");
    }
}
