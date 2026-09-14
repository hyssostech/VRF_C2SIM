using C2SIM;
using S = C2SIM.Schema102;

namespace VrfC2SimApp;

/// <summary>
/// Offline check of ReportBuilder (no bridge, no MAK, no VR-Forces):
/// `VrfC2SimApp --report-selftest`. Builds one task-status (TASKCMPLT) report and one
/// position report, prints them, and ROUND-TRIPS each back through ToC2SIMObject to prove
/// the serialized xml is well-formed and schema-valid with the expected field values
/// (compare against docs/golden-trace/reports-captured_wire-xml.log).
/// </summary>
public static class ReportSelfTest
{
    public static int Run()
    {
        int failures = 0;
        const string iso = "2026-07-10T12:00:00Z";
        const string reportId = "11111111-2222-3333-4444-555555555555";
        const string taskee = "670cfe3a-6c43-f267-ad7f-bd6e739def24";
        const string taskUuid = "a0e0eeb4-59b4-4d4f-88d2-5101538fa371";

        // ---- task-status (TASKCMPLT) ----
        string statusXml = ReportBuilder.BuildTaskCompleteReport(taskee, taskUuid, iso, reportId);
        Console.WriteLine("=== TaskStatus (TASKCMPLT) report ===");
        Console.WriteLine(statusXml);
        var rs = ReportBodyOf(Roundtrip(statusXml));
        if (rs != null && rs.ReportContent is { Length: 1 }
            && rs.ReportContent[0].Item is S.TaskStatusType ts)
        {
            Check(ref failures, ts.TaskStatusCode == S.TaskStatusCodeType.TASKCMPLT, "TaskStatusCode == TASKCMPLT");
            Check(ref failures, ts.CurrentTask == taskUuid, "CurrentTask == taskUuid");
            Check(ref failures, rs.ReportingEntity == taskee, "ReportingEntity == taskee");
            Check(ref failures, IsoOf(ts.TimeOfObservation) == iso, "TimeOfObservation == iso");
        }
        else { failures++; Console.WriteLine("  FAIL: task-status did not round-trip to a TaskStatus content"); }

        // ---- task-status (TASKABRT) - the progress watchdog's report (C16, StallPolicy.cs) ----
        // Same builder, same path to the server, only the code differs; the round-trip proves the
        // TASKABRT enum serializes and deserializes as schema-valid xml exactly as TASKCMPLT does.
        string abrtXml = ReportBuilder.BuildTaskStatusReport(taskee, taskUuid,
                                                            S.TaskStatusCodeType.TASKABRT, iso, reportId);
        Console.WriteLine();
        Console.WriteLine("=== TaskStatus (TASKABRT) report ===");
        Console.WriteLine(abrtXml);
        var ra = ReportBodyOf(Roundtrip(abrtXml));
        if (ra != null && ra.ReportContent is { Length: 1 }
            && ra.ReportContent[0].Item is S.TaskStatusType ta)
        {
            Check(ref failures, ta.TaskStatusCode == S.TaskStatusCodeType.TASKABRT, "TaskStatusCode == TASKABRT");
            Check(ref failures, ta.CurrentTask == taskUuid, "TASKABRT CurrentTask == taskUuid");
            Check(ref failures, ra.ReportingEntity == taskee, "TASKABRT ReportingEntity == taskee");
            Check(ref failures, IsoOf(ta.TimeOfObservation) == iso, "TASKABRT TimeOfObservation == iso");
            Check(ref failures, abrtXml.Contains("TASKABRT", StringComparison.Ordinal)
                                && !abrtXml.Contains("TASKCMPLT", StringComparison.Ordinal),
                  "the TASKABRT wire xml carries TASKABRT and no TASKCMPLT");
        }
        else { failures++; Console.WriteLine("  FAIL: TASKABRT did not round-trip to a TaskStatus content"); }

        // ---- task-status (TASKSTRT) - emitted at DISPATCH (B1) ----
        string strtXml = ReportBuilder.BuildTaskStatusReport(taskee, taskUuid,
                                                            S.TaskStatusCodeType.TASKSTRT, iso, reportId);
        Console.WriteLine();
        Console.WriteLine("=== TaskStatus (TASKSTRT) report ===");
        Console.WriteLine(strtXml);
        var rt2 = ReportBodyOf(Roundtrip(strtXml));
        if (rt2 != null && rt2.ReportContent is { Length: 1 }
            && rt2.ReportContent[0].Item is S.TaskStatusType tstrt)
        {
            Check(ref failures, tstrt.TaskStatusCode == S.TaskStatusCodeType.TASKSTRT, "TaskStatusCode == TASKSTRT");
            Check(ref failures, tstrt.CurrentTask == taskUuid, "TASKSTRT CurrentTask == taskUuid");
            Check(ref failures, rt2.ReportingEntity == taskee, "TASKSTRT ReportingEntity == taskee");
            Check(ref failures, strtXml.Contains("TASKSTRT", StringComparison.Ordinal)
                                && !strtXml.Contains("TASKCMPLT", StringComparison.Ordinal),
                  "the TASKSTRT wire xml carries TASKSTRT and no TASKCMPLT");
        }
        else { failures++; Console.WriteLine("  FAIL: TASKSTRT did not round-trip to a TaskStatus content"); }

        // ---- the EMISSION RULES (B1, TaskStatusPolicy) ----
        // Policy level: which code a task may still emit and how often. No bridge, no server.
        Console.WriteLine();
        Console.WriteLine("=== TaskStatusPolicy (emission rules) ===");
        var pol = new TaskStatusPolicy();
        const string tA = "task-A";
        Check(ref failures, pol.ShouldEmitStart(tA), "a dispatch emits ONE TASKSTRT");
        Check(ref failures, !pol.ShouldEmitStart(tA),
              "the same dispatch RE-ENTERED (TerrainProfile second pass) emits no second TASKSTRT");
        Check(ref failures, pol.ShouldEmitComplete(tA), "its completion emits ONE TASKCMPLT");
        Check(ref failures, !pol.ShouldEmitComplete(tA), "a second completion of the same task emits nothing");
        Check(ref failures, !pol.ShouldEmitAbort(tA), "a TASKCMPLT SUPPRESSES a later TASKABRT");
        Check(ref failures, pol.ShouldEmitStart(tA), "a RE-TASK after completion re-arms and emits TASKSTRT");

        const string tB = "task-B";                       // the stalled / failed task (C16 rule)
        Check(ref failures, pol.ShouldEmitStart(tB), "dispatch of B emits TASKSTRT");
        Check(ref failures, pol.ShouldEmitAbort(tB), "the watchdog emits ONE TASKABRT");
        Check(ref failures, !pol.ShouldEmitAbort(tB), "a second stall of the same task emits nothing");
        Check(ref failures, pol.ShouldEmitComplete(tB), "a TASKABRT does NOT suppress a later TASKCMPLT (C16)");

        const string tC = "task-C";                       // refused at dispatch / skipped successor
        Check(ref failures, pol.ShouldEmitAbort(tC), "a REFUSED (never dispatched) task emits ONE TASKABRT");
        Check(ref failures, !pol.ShouldEmitAbort(tC), "and only one");
        Check(ref failures, TaskStatusPolicy.CodeForCompletion(true) == S.TaskStatusCodeType.TASKCMPLT,
              "a vendor completion with success=true maps to TASKCMPLT");
        Check(ref failures, TaskStatusPolicy.CodeForCompletion(false) == S.TaskStatusCodeType.TASKABRT,
              "a vendor completion with success=false maps to TASKABRT (taskCompleteReport.h:84-90)");
        // A FAILED completion must produce exactly one TASKABRT and no TASKCMPLT (supervisor 2026-09-14).
        const string tD = "task-D";
        var failCode = TaskStatusPolicy.CodeForCompletion(false);
        Check(ref failures, failCode == S.TaskStatusCodeType.TASKABRT && pol.ShouldEmitAbort(tD)
                            && !pol.ShouldEmitAbort(tD),
              "a failed vendor completion yields exactly ONE TASKABRT");
        Check(ref failures, pol.ShouldEmitComplete(tD),
              "... and no TASKCMPLT was consumed by it (the completion slot is untouched)");

        const string unattributed = "";                   // an unattributed completion carries no uuid
        Check(ref failures, pol.ShouldEmitComplete(unattributed) && pol.ShouldEmitComplete(unattributed),
              "an EMPTY task uuid is never suppressed (two unattributed completions = two reports)");

        // ---- DELIVERY (B2, ReportPush): the push knows whether the report arrived ----
        // Fake transports, no SDK and no server; sleep is a no-op so the test runs instantly.
        Console.WriteLine();
        Console.WriteLine("=== ReportPush (delivery + bounded retry) ===");
        Func<TimeSpan, Task> noSleep = _ => Task.CompletedTask;

        // (a) the server answers ERROR once, then OK: one retry, one warn line, delivered.
        int calls = 0;
        var warns = new List<string>();
        var r = ReportPush.SendAsync(
            _ => Task.FromResult(++calls == 1
                     ? new ReportPush.PushResult(false, "duplicate ReportID")
                     : new ReportPush.PushResult(true, "OK")),
            statusXml, 3, a => ReportPush.BackoffFor(a), noSleep, w => warns.Add(w)).Result;
        Check(ref failures, r.Ok, "ERROR-then-OK: the report IS delivered");
        Check(ref failures, r.Attempts == 2, $"ERROR-then-OK: exactly 2 attempts (saw {r.Attempts})");
        Check(ref failures, warns.Count == 1, $"ERROR-then-OK: exactly ONE retry line (saw {warns.Count})");
        Check(ref failures, warns.Count == 1 && warns[0].Contains("duplicate ReportID", StringComparison.Ordinal),
              "the retry line carries the SERVER's message");

        // (b) the transport throws every time: 3 attempts, 2 retry lines, a final failure to be loud about.
        calls = 0; warns.Clear();
        var r2 = ReportPush.SendAsync(
            _ => { calls++; throw new InvalidOperationException("The response ended prematurely"); },
            statusXml, 3, a => ReportPush.BackoffFor(a), noSleep, w => warns.Add(w)).Result;
        Check(ref failures, !r2.Ok, "3 throws: the push FAILS (it is not silently counted as sent)");
        Check(ref failures, calls == 3 && r2.Attempts == 3, $"3 throws: exactly 3 attempts (saw {calls})");
        Check(ref failures, warns.Count == 2, $"3 throws: 2 retry lines before the loud failure (saw {warns.Count})");
        Check(ref failures, r2.LastMessage.Contains("The response ended prematurely", StringComparison.Ordinal),
              "the failure carries the transport's own message (the G6 symptom)");

        // (c) a POSITION push is NOT retried: one attempt, no retry line, failure reported.
        calls = 0; warns.Clear();
        var r3 = ReportPush.SendAsync(
            _ => { calls++; return Task.FromResult(new ReportPush.PushResult(false, "server busy")); },
            // tries=1 IS what "a position push" means to ReportPush (the service passes
            // Vrf:TaskStatusPushTries only for ReportKind.TaskStatus).
            statusXml, 1, a => ReportPush.BackoffFor(a), noSleep, w => warns.Add(w)).Result;
        Check(ref failures, !r3.Ok && calls == 1 && warns.Count == 0,
              $"a position push is tried ONCE and not retried (attempts {calls}, retry lines {warns.Count})");

        // (d) the backoff is the documented 1 / 2 / 4 s.
        Check(ref failures, (int)ReportPush.BackoffFor(1).TotalMilliseconds == 1000
                            && (int)ReportPush.BackoffFor(2).TotalMilliseconds == 2000
                            && (int)ReportPush.BackoffFor(3).TotalMilliseconds == 4000,
              "backoff is 1 s, 2 s, 4 s");
        Check(ref failures, ReportPush.BackoffFor(20).TotalSeconds <= 30, "backoff is capped at 30 s");

        // ---- R-SURFACE-PROXY re-announcement (B8, SubstitutionAnnouncer) ----
        // A unit created as an EMPTY SHELL at init and re-created at order time as something else
        // must announce again; a re-creation as the same thing must not.
        Console.WriteLine();
        Console.WriteLine("=== SubstitutionAnnouncer (re-announce a changed representation) ===");
        var ann = new SubstitutionAnnouncer();
        const string unit = "2/1_AD/25_~PXY";
        string shell = SubstitutionAnnouncer.Representation("M577A2_Command_Post", false, 0);
        string full = SubstitutionAnnouncer.Representation("M577A2_Command_Post", true, 0);
        string composed = SubstitutionAnnouncer.Representation("Tank Company (USA)", true, 4);
        Check(ref failures, shell != full && full != composed && shell != composed,
              $"shell / template / composed are distinct representations ('{shell}', '{full}', '{composed}')");
        Check(ref failures, ann.ShouldAnnounce(unit, shell), "init: the shell is announced once");
        Check(ref failures, !ann.ShouldAnnounce(unit, shell), "an unchanged representation emits NOTHING");
        Check(ref failures, ann.ShouldAnnounce(unit, full),
              "a re-creation as a DIFFERENT representation emits one more NameObservation");
        Check(ref failures, !ann.ShouldAnnounce(unit, full), "... and only one");
        Check(ref failures, ann.ShouldAnnounce(unit, composed), "composing it from sub-units announces again");
        Check(ref failures, ann.Current(unit) == composed, "the announcer remembers the LAST representation");

        var exact = new SubstitutionAnnouncer();
        exact.Record("1141.MechPlt", SubstitutionAnnouncer.Representation("Mech Platoon (USA)", false, 0));
        Check(ref failures, exact.ShouldAnnounce("1141.MechPlt",
                                SubstitutionAnnouncer.Representation("Mech Platoon (USA)", true, 0)),
              "a RECORDED (never announced) unit still announces when its representation changes");
        Check(ref failures, !SubstitutionAnnouncer.Substituted("", 0),
              "an exact-type unit created as its own template has NO substitution to report");
        Check(ref failures, SubstitutionAnnouncer.Substituted("", 4),
              "a unit composed from sub-units IS a substitution (of representation)");
        Check(ref failures, SubstitutionAnnouncer.Substituted("Proxy: M577A2_Command_Post - ...", 0),
              "a proxy template IS a substitution");

        // ---- position ----
        const string subject = "001aa71b-4c26-a1ea-28b2-f7dfe8e76342";
        const double lat = 58.703, lon = 16.4992;
        string posXml = ReportBuilder.BuildPositionReport(subject, lat, lon, iso, reportId);
        Console.WriteLine();
        Console.WriteLine("=== Position report ===");
        Console.WriteLine(posXml);
        var rp = ReportBodyOf(Roundtrip(posXml));
        if (rp != null && rp.ReportContent is { Length: 1 }
            && rp.ReportContent[0].Item is S.PositionReportContentType pc)
        {
            Check(ref failures, pc.SubjectEntity == subject, "SubjectEntity == subject");
            Check(ref failures, rp.ReportingEntity == subject, "ReportingEntity == subject");
            var g = pc.Location?.Item as S.GeodeticCoordinateType;
            Check(ref failures, g != null && g.Latitude == lat && g.Longitude == lon, "Location lat/lon match");
            Check(ref failures, (pc.EntityHealthStatus?.Length ?? 0) == 0, "EntityHealthStatus omitted (no health data)");
            Check(ref failures, IsoOf(pc.TimeOfObservation) == iso, "TimeOfObservation == iso");
        }
        else { failures++; Console.WriteLine("  FAIL: position did not round-trip to a PositionReportContent"); }

        // ---- position BUNDLE (P4b: ONE ReportBody carrying N PositionReportContent blocks) ----
        var fixes = new (string uuid, double latDeg, double lonDeg)[]
        {
            ("aaaaaaaa-0000-0000-0000-000000000001", 58.10, 16.10),
            ("bbbbbbbb-0000-0000-0000-000000000002", 58.20, 16.20),
            ("cccccccc-0000-0000-0000-000000000003", 58.30, 16.30),
        };
        string bundleXml = ReportBuilder.BuildPositionReportBundle(fixes, iso, reportId);
        Console.WriteLine();
        Console.WriteLine("=== Position report BUNDLE (3 fixes) ===");
        Console.WriteLine(bundleXml);
        var rb = ReportBodyOf(Roundtrip(bundleXml));
        if (rb != null && rb.ReportContent is { Length: 3 })
        {
            Check(ref failures, true, "bundle round-trips to 3 ReportContent blocks");
            bool allMatch = true, timeOk = true;
            for (int i = 0; i < 3; i++)
            {
                if (rb.ReportContent[i].Item is S.PositionReportContentType bc)
                {
                    var bg = bc.Location?.Item as S.GeodeticCoordinateType;
                    if (bc.SubjectEntity != fixes[i].uuid || bg == null
                        || bg.Latitude != fixes[i].latDeg || bg.Longitude != fixes[i].lonDeg)
                        allMatch = false;
                    if (IsoOf(bc.TimeOfObservation) != iso) timeOk = false;
                }
                else { allMatch = false; timeOk = false; }
            }
            Check(ref failures, allMatch, "each bundle content has its own uuid/lat/lon (in order)");
            Check(ref failures, timeOk, "each bundle content TimeOfObservation == iso");
            Check(ref failures, rb.ReportID == reportId, "one ReportID for the whole bundle body");
            Check(ref failures, rb.ReportingEntity == fixes[0].uuid,
                  "bundle ReportingEntity == first fix uuid (C++-parity envelope choice)");
        }
        else { failures++; Console.WriteLine("  FAIL: 3-fix bundle did not round-trip to 3 contents"); }

        // A 1-fix bundle is semantically the single-content shape (one content; ReportingEntity=subject).
        var oneFix = new (string uuid, double latDeg, double lonDeg)[] { (subject, lat, lon) };
        string oneBundleXml = ReportBuilder.BuildPositionReportBundle(oneFix, iso, reportId);
        var r1 = ReportBodyOf(Roundtrip(oneBundleXml));
        if (r1 != null && r1.ReportContent is { Length: 1 }
            && r1.ReportContent[0].Item is S.PositionReportContentType b1)
        {
            var g1 = b1.Location?.Item as S.GeodeticCoordinateType;
            Check(ref failures, b1.SubjectEntity == subject && g1 != null
                  && g1.Latitude == lat && g1.Longitude == lon,
                  "1-fix bundle == single-content shape (uuid/lat/lon)");
            Check(ref failures, r1.ReportingEntity == subject,
                  "1-fix bundle ReportingEntity == subject (single-report semantics)");
        }
        else { failures++; Console.WriteLine("  FAIL: 1-fix bundle did not round-trip to a single content"); }

        Console.WriteLine();
        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    // PushReportMessage wraps a bare ReportBody in MessageBody/DomainMessageBody; do the
    // same before deserializing so ToC2SIMObject<MessageBodyType> can read it back.
    private static S.MessageBodyType Roundtrip(string reportBodyXml)
    {
        string wrapped = "<MessageBody xmlns=\"http://www.sisostds.org/schemas/C2SIM/1.1\">" +
                         "<DomainMessageBody>" + StripDecl(reportBodyXml) +
                         "</DomainMessageBody></MessageBody>";
        try { return C2SIMSDK.ToC2SIMObject<S.MessageBodyType>(wrapped); }
        catch (Exception e) { Console.WriteLine($"  FAIL: round-trip deserialize threw: {e.Message}"); return null; }
    }

    private static string StripDecl(string xml)
    {
        int i = xml.IndexOf("?>", StringComparison.Ordinal);
        return i >= 0 ? xml.Substring(i + 2).TrimStart() : xml;
    }

    // MessageBody -> DomainMessageBody -> ReportBody (same nesting OrderParser walks).
    private static S.ReportBodyType ReportBodyOf(S.MessageBodyType body)
        => (body?.Item as S.DomainMessageBodyType)?.Item as S.ReportBodyType;

    private static string IsoOf(S.TimeInstantType t) => (t?.Item as S.DateTimeType)?.IsoDateTime;

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
