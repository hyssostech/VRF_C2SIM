using System.Xml.Linq;

namespace VrfC2SimApp;

/// <summary>
/// X9 (REHEARSAL_WAYB_2026-10-05, demo item D6): THE StartTime/RelativeTime DELAY IS READ.
///
/// C2SIM's TimeInstantType is a choice of DateTime, RelativeTime or SimulationTime
/// (C2SIM_SMX_LOX_CWIX2024.xsd:4332-4341). RelativeTimeType "Specifies an absolute time relative to
/// the start or end time of an event" and carries DelayTimeAmount (1..1), EventReference (1..1) and
/// TimeReferenceCode (1..1) (xsd:3288-3300); TimeReferenceCode is IntervalEndTime | IntervalStartTime
/// (xsd:4343-4352); DelayTimeAmount is "a time delay from some other referenced value" (xsd:968-973).
///
/// STP's raw Iron Storm export writes 14 of them, and EVERY ONE names the task's own STREND
/// predecessor (EventReference == ActionTemporalRelationship/TemporalAssociationWithAction) at
/// IntervalEndTime: "start at the predecessor's end + delay". The STREND gate already serves
/// RelativeDelayMs after the predecessor completes (TaskSequencer.WaitForStartAsync), so the
/// parser maps exactly that case onto RelativeDelayMs. T2's PT20M was the one non-zero value and
/// was dispatched 44 ms after T1 completed in the Way B rehearsal - unread.
///
/// ANY OTHER REFERENCE (another task, a start time, no predecessor at all) is NOT HANDLED: one
/// warning per order names it and the task keeps today's behaviour (the delay is not applied).
/// SimulationTime handling, the gate rules and completion are untouched.
/// </summary>
public static class RelativeTimeSelfTest
{
    private const string T2Uuid = "696fbb33";   // the raw export's T2 (cut A's T02), PT20M after T1's end

    public static int Run()
    {
        int failures = 0;
        string repo = FindRepoRoot();
        if (repo == null)
        {
            Console.WriteLine("  [FAIL] data/ not found - the order files cannot be read");
            return 1;
        }

        // (x1) THE RAW EXPORT: 14 RelativeTimes, each its own predecessor's end, each now a RelativeDelayMs.
        failures += CheckOrderFile(repo, "STP-IRON-STORM-SYNTHETIC_Order.xml", expectedRelative: 14);
        // (x2) THE DERIVED ORDERS carry the same RelativeTimes in the pattern form (cut A: 2, FULL: 14).
        failures += CheckOrderFile(repo, "IRONSTORM_CUTA_Order.xml", expectedRelative: 2);
        failures += CheckOrderFile(repo, "IRONSTORM_FULL_Order.xml", expectedRelative: 14);
        // ... and the reference order (31 RelativeTimes, every one P0) gains no delay at all.
        failures += CheckOrderFile(repo, "COA-STP1_Order.xml", expectedRelative: 31);

        // (x3) THE SYNTHETIC CASES: one handled reference in each duration form, three NOT handled.
        {
            var o = OrderParser.Parse(SyntheticOrderXml());
            var byName = o.Tasks.ToDictionary(t => t.TaskName, StringComparer.Ordinal);
            int notHandled = o.Warnings.Count(w => w.Contains(OrderParser.RelativeTimeNotHandledMarker, StringComparison.Ordinal));
            string warn = o.Warnings.FirstOrDefault(w => w.Contains(OrderParser.RelativeTimeNotHandledMarker, StringComparison.Ordinal)) ?? "";
            Check(ref failures, o.Tasks.Count == 7, $"(x3) the synthetic order parses to 7 tasks (got {o.Tasks.Count})");
            Check(ref failures, byName.TryGetValue("R_SHORT", out var rs) && rs.RelativeDelayMs == 300000L
                                && rs.SimulationStartMs == 0L,
                  "(x3) a RelativeTime PT5M on the predecessor's IntervalEndTime gives RelativeDelayMs 300,000 (short form)");
            Check(ref failures, byName.TryGetValue("R_PATTERN", out var rp) && rp.RelativeDelayMs == 420000L,
                  "(x3) ... and P00Y00M00DT00H07M00S gives 420,000 (the C2SIM pattern form)");
            Check(ref failures, byName.TryGetValue("R_BOTH", out var rb) && rb.RelativeDelayMs == 240000L,
                  "(x3) with an ActionTemporalRelationship Duration PT2M as well, the LATER lower bound wins (240,000)");
            Check(ref failures, byName.TryGetValue("R_OTHER", out var ro) && ro.RelativeDelayMs == 0L
                                && ro.StartAfterTaskUuid == Uuid(2),
                  "(x3) a RelativeTime naming ANOTHER task's end (not its predecessor) is NOT applied: RelativeDelayMs 0, gate unchanged");
            Check(ref failures, byName.TryGetValue("R_START", out var rst) && rst.RelativeDelayMs == 0L,
                  "(x3) a RelativeTime on the predecessor's IntervalStartTime is NOT applied (RelativeDelayMs 0)");
            Check(ref failures, byName.TryGetValue("R_NOPRED", out var rn) && rn.RelativeDelayMs == 0L
                                && rn.StartAfterTaskUuid.Length == 0,
                  "(x3) a RelativeTime on a task with NO predecessor is NOT applied (RelativeDelayMs 0)");
            Check(ref failures, notHandled == 1,
                  $"(x3) the three unhandled RelativeTimes give ONE warning for the order (got {notHandled})");
            Check(ref failures, warn.StartsWith("3 ", StringComparison.Ordinal) && warn.Contains("R_OTHER")
                                && warn.Contains("R_START") && warn.Contains("R_NOPRED") && !warn.Contains("R_SHORT"),
                  "(x3) ... which counts 3 and names exactly R_OTHER, R_START and R_NOPRED");
            Check(ref failures, o.ShortFormDurations.Any(s => s.Contains("R_SHORT") && s.Contains("RelativeTime"))
                                && !o.ShortFormDurations.Any(s => s.Contains("R_OTHER")),
                  "(x3) a READ short-form RelativeTime joins the order's one short-form warning; an unread one does not");
        }

        // (x4) THE GATE serves it: predecessor completion + the relative delay, on the task clock.
        {
            var clock = new ManualClock();
            var seq = new TaskSequencer();
            seq.NotifyDispatched("PRED", 0.0);
            var gate = seq.WaitForStartAsync("PRED", 0L, 1200000L, 3600.0, clock.AsTaskClock(), CancellationToken.None);
            clock.AdvanceTo(100.0);
            seq.CompleteTask("PRED");
            SpinUntil(() => clock.HasWaiterDueAt(100.0 + 1200.0) || gate.IsCompleted);
            clock.AdvanceTo(100.0 + 1199.0);
            bool early = gate.IsCompleted;
            clock.AdvanceTo(100.0 + 1200.0);
            SpinUntil(() => gate.IsCompleted);
            Check(ref failures, !early && gate.IsCompleted && gate.Result == GateResult.Proceed,
                  "(x4) the STREND gate holds a RelativeDelayMs of 1,200,000 for 1,200 s AFTER the predecessor completes, then proceeds");
        }
        return failures;
    }

    /// <summary>Parse the file twice - as it is, and with every StartTime/RelativeTime removed (today's
    /// reading of it) - and require: each RelativeTime names its own predecessor's IntervalEndTime;
    /// its task's RelativeDelayMs is its decoded DelayTimeAmount; NOTHING else differs.</summary>
    private static int CheckOrderFile(string repo, string name, int expectedRelative)
    {
        int failures = 0;
        string xml = File.ReadAllText(Path.Combine(repo, "data", name));
        var doc = XDocument.Parse(xml);
        var rel = doc.Descendants().Where(e => e.Name.LocalName == "RelativeTime").ToList();
        var expected = new Dictionary<string, (long Ms, string Ref)>(StringComparer.Ordinal);
        foreach (var r in rel)
        {
            var task = r.Ancestors().First(a => a.Name.LocalName == "ManeuverWarfareTask");
            string uuid = task.Elements().First(e => e.Name.LocalName == "UUID").Value.Trim();
            string iso = r.Descendants().First(e => e.Name.LocalName == "IsoTimeDuration").Value;
            string evRef = r.Elements().First(e => e.Name.LocalName == "EventReference").Value.Trim();
            expected[uuid] = (OrderParser.DecodeIsoDuration(iso, out _), evRef);
        }
        Check(ref failures, rel.Count == expectedRelative,
              $"(x1/x2) {name}: {rel.Count} StartTime/RelativeTime element(s) (expected {expectedRelative})");

        var parsed = OrderParser.Parse(xml);
        var stripped = XDocument.Parse(xml);
        foreach (var st in stripped.Descendants().Where(e => e.Name.LocalName == "StartTime"
                                                           && e.Elements().Any(c => c.Name.LocalName == "RelativeTime")).ToList())
            st.Remove();
        var today = OrderParser.Parse(stripped.ToString());

        int selfPred = 0, mapped = 0, others = 0;
        foreach (var t in parsed.Tasks)
        {
            var u = today.Tasks.FirstOrDefault(x => x.TaskUuid == t.TaskUuid);
            bool same = u != null && u.SimulationStartMs == t.SimulationStartMs && u.StartAfterTaskUuid == t.StartAfterTaskUuid
                        && u.DurationMs == t.DurationMs && u.AbsoluteStartUtc == t.AbsoluteStartUtc
                        && u.ActionCode == t.ActionCode && u.TaskeeUuid == t.TaskeeUuid;
            if (expected.TryGetValue(t.TaskUuid, out var e))
            {
                if (e.Ref == t.StartAfterTaskUuid) selfPred++;
                if (same && u.RelativeDelayMs == 0 && t.RelativeDelayMs == e.Ms) mapped++;
            }
            else if (same && u.RelativeDelayMs == t.RelativeDelayMs) others++;
        }
        Check(ref failures, selfPred == rel.Count,
              $"(x1/x2) {name}: every RelativeTime names its task's own STREND predecessor ({selfPred} of {rel.Count})");
        Check(ref failures, mapped == rel.Count,
              $"(x1/x2) {name}: RelativeDelayMs == the decoded DelayTimeAmount on {mapped} of {rel.Count}, every other field as today");
        Check(ref failures, others == parsed.Tasks.Count - rel.Count && parsed.Tasks.Count == today.Tasks.Count,
              $"(x1/x2) {name}: the {parsed.Tasks.Count - rel.Count} task(s) without one are unchanged ({others})");
        Check(ref failures, !parsed.Warnings.Any(w => w.Contains(OrderParser.RelativeTimeNotHandledMarker, StringComparison.Ordinal)),
              $"(x1/x2) {name}: no RelativeTime is reported NOT HANDLED");
        var t2 = parsed.Tasks.FirstOrDefault(t => t.TaskUuid.StartsWith(T2Uuid, StringComparison.Ordinal));
        if (expected.Keys.Any(k => k.StartsWith(T2Uuid, StringComparison.Ordinal)))
            Check(ref failures, t2 != null && t2.RelativeDelayMs == 1200000L,
                  $"(x1/x2) {name}: T2 (28ID, {T2Uuid}) starts 20 min after T1's end - RelativeDelayMs 1,200,000 (got {t2?.RelativeDelayMs})");
        return failures;
    }

    private static string Uuid(int n) => $"{n}{n}{n}{n}{n}{n}{n}{n}-9999-9999-9999-999999999999";

    /// <summary>Seven tasks: T1 a root; R_SHORT / R_PATTERN / R_BOTH name their predecessor's end
    /// (handled); R_OTHER names a task that is not its predecessor, R_START its predecessor's START,
    /// R_NOPRED has no predecessor (all three NOT handled).</summary>
    private static string SyntheticOrderXml()
    {
        static string Atr(string pred, string dur) =>
            "<ActionTemporalRelationship><ActionTemporalAssociationCode>STREND</ActionTemporalAssociationCode>"
            + (dur == null ? "" : "<Duration><IsoTimeDuration>" + dur + "</IsoTimeDuration></Duration>")
            + "<TemporalAssociationWithAction>" + pred + "</TemporalAssociationWithAction></ActionTemporalRelationship>";
        static string Rel(string delay, string evRef, string code) =>
            "<StartTime><RelativeTime><DelayTimeAmount><IsoTimeDuration>" + delay + "</IsoTimeDuration></DelayTimeAmount>"
            + "<EventReference>" + evRef + "</EventReference><TimeReferenceCode>" + code + "</TimeReferenceCode>"
            + "</RelativeTime></StartTime>";
        static string Task(string name, int n, string atr, string start) =>
            "<Task><ManeuverWarfareTask>"
            + (atr ?? "")
            + "<Duration><IsoTimeDuration>P00Y00M00DT00H20M00S</IsoTimeDuration></Duration>"
            + "<Name>" + name + "</Name>"
            + "<PerformingEntity>88888888-8888-8888-8888-888888888888</PerformingEntity>"
            + (start ?? "")
            + "<TaskActionCode>SECURE</TaskActionCode>"
            + "<UUID>" + Uuid(n) + "</UUID>"
            + "</ManeuverWarfareTask></Task>";
        return "<OrderBody xmlns=\"http://www.sisostds.org/schemas/C2SIM/1.1\">"
             + "<OrderID>relativetime-selftest</OrderID>"
             + Task("T1", 1, null, null)
             + Task("R_SHORT", 2, Atr(Uuid(1), null), Rel("PT5M", Uuid(1), "IntervalEndTime"))
             + Task("R_PATTERN", 3, Atr(Uuid(2), null), Rel("P00Y00M00DT00H07M00S", Uuid(2), "IntervalEndTime"))
             + Task("R_BOTH", 4, Atr(Uuid(1), "PT2M"), Rel("PT4M", Uuid(1), "IntervalEndTime"))
             + Task("R_OTHER", 5, Atr(Uuid(2), null), Rel("PT6M", Uuid(1), "IntervalEndTime"))
             + Task("R_START", 6, Atr(Uuid(2), null), Rel("PT8M", Uuid(2), "IntervalStartTime"))
             + Task("R_NOPRED", 7, null, Rel("PT9M", Uuid(1), "IntervalEndTime"))
             + "</OrderBody>";
    }

    /// <summary>A task clock moved by hand: DelayAsync completes when AdvanceTo reaches its due time.</summary>
    private sealed class ManualClock
    {
        private readonly object _lock = new();
        private readonly List<(double Due, TaskCompletionSource Tcs)> _waiters = new();
        private double _now;
        public bool HasWaiterDueAt(double due) { lock (_lock) return _waiters.Any(w => Math.Abs(w.Due - due) < 1e-6); }
        public TaskClock AsTaskClock() => new(() => { lock (_lock) return _now; }, DelayAsync);

        private Task DelayAsync(double seconds, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!(seconds > 0.0)) { tcs.TrySetResult(); return tcs.Task; }
            lock (_lock) _waiters.Add((_now + seconds, tcs));
            return tcs.Task;
        }

        public void AdvanceTo(double seconds)
        {
            var due = new List<TaskCompletionSource>();
            lock (_lock)
            {
                _now = Math.Max(_now, seconds);
                for (int i = _waiters.Count - 1; i >= 0; i--)
                    if (_waiters[i].Due <= _now) { due.Add(_waiters[i].Tcs); _waiters.RemoveAt(i); }
            }
            foreach (var t in due) t.TrySetResult();
        }
    }

    private static void SpinUntil(Func<bool> done)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!done() && sw.ElapsedMilliseconds < 5000) Thread.Sleep(5);
    }

    private static string FindRepoRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            for (var d = new DirectoryInfo(start); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "data", "STP-IRON-STORM-SYNTHETIC_Order.xml"))) return d.FullName;
        return null;
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
