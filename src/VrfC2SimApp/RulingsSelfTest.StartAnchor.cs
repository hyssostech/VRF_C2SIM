using Microsoft.Extensions.Configuration;

namespace VrfC2SimApp;

/// <summary>
/// STP-850 - A StartTime/SimulationTime OFFSET IS MEASURED FROM ORDER RECEIPT (Vrf:StartTimeAnchor).
///
/// STP exports every task with SimulationTime = start slot x phase duration (an ABSOLUTE offset, the same
/// for same-slot tasks on different units) and every non-first task of a unit with ONE same-unit STREND
/// relation to that unit's previous task, as a lower bound. The gate used to serve the offset as a delay
/// AFTER the predecessor completed, so along one unit's chain the start grew quadratically and the tail
/// fell past Vrf:TaskChainBackstopSeconds. Under Receipt (the default) a task starts at
/// max(predecessor completion, receipt + offset); PredecessorCompletion keeps the old rule for rollback
/// and is the FAIL-FIRST control of every case below.
///
///   (a) same-slot tasks on two units start together;
///   (b) an on-time chained task starts at its offset - no stretch;
///   (c) a late predecessor delays its successor until it completes, not before;
///   (d) back to back [0,1) -> [1,2): the successor opens on the predecessor's Completed signal, not on
///       its offset (unit idleness at that moment is NOT covered offline - see the case's comment);
///   (e) a 10-task chain at d = 60 min completes inside the backstop (no quadratic growth);
///   (f) Vrf:DurationScale 0.25 scales the offset exactly as it scales the Duration;
///   (g) the setting round-trips (parse, default, shipped json, override) and the service wiring;
///   (h) PARITY: every order in data/ and docs/golden-trace/orders/ parses and dispatches identically
///       under all three values, except the orders whose minimum offset is not 0 (named);
///   (i) a gated DateTime StartTime is anchored too: start = max(predecessor completion, that instant),
///       and it is never rebased;
///   (j) SPLIT ORDERS: a phase-wave order with absolute slots starts on its own receipt under Receipt
///       (rebased on the order's minimum offset), and idles under ReceiptAbsolute (the fail-first);
///   (k) a full-plan order (minimum 0) dispatches identically under Receipt and ReceiptAbsolute;
///   (l) a task with no StartTime does not pin the minimum; the parser marks a present SimulationTime.
/// Cases (a)-(f) hand the gate the offset directly (their orders have a slot-0 task, minimum 0), so the
/// split-order rebase leaves them unchanged. Receipt below means the rebased Receipt unless it says
/// ReceiptAbsolute.
/// Everything runs on the signalled StepClock - task-clock seconds, no wall time.
/// </summary>
public static partial class RulingsSelfTest
{
    private const double SlotSeconds = 3600.0;          // d = 60 min, STP's PhaseDuration in the brief
    private const double Stp850Backstop = TaskDispatchPolicy.DefaultChainBackstopSeconds;

    private static void StartTimeAnchorChecks(ref int failures)
    {
        const double D = SlotSeconds;

        // ------------------------------------------------------------------ (b) ----
        foreach (bool receipt in new[] { false, true })
        {
            var r = new AnchorRig(receipt);
            r.Dispatch("A1", 0.0);
            var a2 = r.Gate("A1", D, window: D + 60.0);
            r.StepTo(D - 1.0);
            bool earlyShut = !a2.IsCompleted;
            r.StepTo(D); r.Complete("A1");
            double opened = r.RunUntilOpen(a2, 3.0 * D);
            if (receipt)
                Check(ref failures, earlyShut && a2.Result == GateResult.Proceed && opened == D,
                      $"(b) STP-850 Receipt: an ON-TIME chained task (slot 1, offset {D:F0} s, predecessor ends at " +
                      $"{D:F0} s) dispatches AT ITS OFFSET - opened at {opened:F0} s, no stretch");
            else
                Check(ref failures, opened == 2.0 * D,
                      $"FAIL-FIRST (b) PredecessorCompletion: the same task opens at {opened:F0} s = predecessor end " +
                      $"+ offset ({2.0 * D:F0} s) - the offset served AGAIN after the predecessor");
        }

        // ------------------------------------------------------------------ (a) ----
        foreach (bool receipt in new[] { false, true })
        {
            var r = new AnchorRig(receipt);
            r.Dispatch("A1", 0.0); r.Dispatch("B1", 0.0);
            var a2 = r.Gate("A1", D, window: D + 60.0);             // unit A: A1 ends at D
            var b2 = r.Gate("B1", D, window: D / 2.0 + 60.0);       // unit B: B1 (half a slot) ends at D/2
            r.StepTo(D / 2.0); r.Complete("B1");
            r.StepTo(D); r.Complete("A1");
            double aAt = r.RunUntilOpen(a2, 3.0 * D);
            double bAt = r.RunUntilOpen(b2, 3.0 * D);
            if (receipt)
                Check(ref failures, aAt == D && bAt == D,
                      $"(a) STP-850 Receipt: the two slot-1 tasks of units A and B start TOGETHER at their shared " +
                      $"offset (A2 at {aAt:F0} s, B2 at {bAt:F0} s; B's predecessor ended early at {D / 2.0:F0} s)");
            else
                Check(ref failures, aAt == 2.0 * D && bAt == 1.5 * D,
                      $"FAIL-FIRST (a) PredecessorCompletion: the same-slot tasks split - A2 at {aAt:F0} s, B2 at " +
                      $"{bAt:F0} s - because each served the offset after its own predecessor");
        }

        // ------------------------------------------------------------------ (c) ----
        foreach (bool receipt in new[] { false, true })
        {
            var r = new AnchorRig(receipt);
            r.Dispatch("A1", 0.0);
            // RL-20260927-05 / RL-20260921-09: a mover still travelling at its end time is OVERDUE and its
            // follow-on waits (to the backstop) for it - the gate's own extension, unchanged by STP-850.
            var a2 = r.Gate("A1", D, window: D + 60.0, overdueBackstop: Stp850Backstop);
            r.StepTo(D); r.Seq.NotifyOverdue("A1");
            r.StepTo(1.5 * D - 60.0);
            bool shutWhileLate = !a2.IsCompleted;
            r.StepTo(1.5 * D); r.Complete("A1");
            double opened = r.RunUntilOpen(a2, 4.0 * D);
            if (receipt)
                Check(ref failures, shutWhileLate && a2.Result == GateResult.Proceed && opened == 1.5 * D,
                      $"(c) STP-850 Receipt: a LATE predecessor (arrives at {1.5 * D:F0} s, past the {D:F0} s offset) " +
                      $"holds its successor until it completes - shut at {1.5 * D - 60.0:F0} s, opened at {opened:F0} s");
            else
                Check(ref failures, opened == 2.5 * D,
                      $"FAIL-FIRST (c) PredecessorCompletion: the successor waits a further full offset after the " +
                      $"late completion - opened at {opened:F0} s");
        }

        // ------------------------------------------------------------------ (d) ----
        {
            // Back to back on ONE unit: A1 in [0, 1), A2 in [1, 2). The offset is reached at D, but A1's
            // Completed signal is OBSERVED two seconds later (the clock staircase + the timed walk). What this
            // checks is the GATE's invariant only: under Receipt the successor opens on the predecessor's
            // Completed signal and not on its offset. NOT COVERED OFFLINE (the funnels are private service
            // methods that need the bridge): whether the unit is idle at that moment. It is for the hold-in-place
            // pop and SynthesizeUnitCompletion's TryComplete, which clear the in-flight record before
            // CompleteTask; it is NOT for a platform ATTACK, whose engage is re-recorded in flight by
            // IssueEngage AFTER CompleteTask, nor for a timed completion of a non-hold task, which releases
            // successors without popping - RUNBOOK sec 11 names that exposure (pre-existing, more frequent
            // under Receipt).
            var r = new AnchorRig(receipt: true);
            r.Dispatch("A1", 0.0);
            var a2 = r.Gate("A1", D, window: D + 60.0);
            r.StepTo(D + 1.0);
            bool shutPastOffset = !a2.IsCompleted;
            r.StepTo(D + 2.0);
            r.Complete("A1");
            double opened = r.RunUntilOpen(a2, 3.0 * D);
            Check(ref failures, shutPastOffset && a2.Result == GateResult.Proceed && opened == D + 2.0,
                  $"(d) STP-850 Receipt, back to back: with the offset reached at {D:F0} s and A1 NOT YET COMPLETED the " +
                  $"gate stays shut; it opens on A1's Completed signal ({opened:F0} s), not on the offset (unit " +
                  "idleness at that moment is not covered offline - see the comment)");
        }

        // ------------------------------------------------------------------ (e) ----
        {
            var chain = SlotChain(10, D, 1.0);
            double leadReceipt = LongestLeadSeconds(chain, 1.0, anchorAtReceipt: true);
            double leadOld = LongestLeadSeconds(chain, 1.0);
            Check(ref failures, leadReceipt == 9.0 * D && leadOld == 54.0 * D,
                  $"(e) the chain-lead arithmetic the service logs at receipt (E4): the last of 10 back-to-back " +
                  $"{D:F0} s tasks dispatches at {leadReceipt:F0} s under Receipt, {leadOld:F0} s (54 h) under " +
                  $"PredecessorCompletion - only the old rule trips the 'deeper than the backstop' WARNING");

            var old = WalkChain(chain, 600.0, 60.0, 1.0, Stp850Backstop, 60.0);
            Check(ref failures, old.Dispatched == 7 && old.SkippedCount == 3
                                && old.Result("T8") == GateResult.PredecessorNeverDispatched,
                  $"FAIL-FIRST (e) PredecessorCompletion: {old.Dispatched} of 10 dispatch and {old.SkippedCount} are " +
                  $"TASKABRT'd ({old.Result("T8")}) at the {Stp850Backstop:F0} s backstop - dispatch times " +
                  $"{old.Times("T1", "T2", "T3", "T4", "T5", "T6", "T7")} s grow quadratically");

            var now = WalkChain(chain, 600.0, 60.0, 1.0, Stp850Backstop, 60.0, anchorAtReceipt: true);
            bool onSlot = now.Dispatched == 10;
            for (int k = 0; k < 10 && onSlot; k++) onSlot = now.At("T" + (k + 1)) == k * D;
            double lastEnd = now.At("T10") + D;
            Check(ref failures, onSlot && now.SkippedCount == 0 && lastEnd < Stp850Backstop,
                  $"(e) STP-850 Receipt: all 10 dispatch ON THEIR SLOTS ({now.Times("T1", "T2", "T3", "T10")} s ...) and " +
                  $"the chain ends at {lastEnd:F0} s, inside the {Stp850Backstop:F0} s backstop");
        }

        // ------------------------------------------------------------------ (f) ----
        {
            const double scale = 0.25;
            var chain = SlotChain(10, D, 1.0);
            var r = WalkChain(chain, 600.0, 60.0, scale, Stp850Backstop, 15.0, anchorAtReceipt: true);
            bool onSlot = r.Dispatched == 10;
            for (int k = 0; k < 10 && onSlot; k++) onSlot = r.At("T" + (k + 1)) == k * D * scale;
            Check(ref failures, onSlot && r.SkippedCount == 0,
                  $"(f) Vrf:DurationScale={scale}: the offset is scaled exactly as the Duration - slot k dispatches at " +
                  $"k x {D * scale:F0} s ({r.Times("T1", "T2", "T3", "T10")} s ...), no stretch, none skipped");
        }

        // ------------------------------------------------------------------ (g) ----
        {
            bool v1, v2, v3, v4, v5, v6;
            bool parse =
                TaskDispatchPolicy.ParseStartTimeAnchor("Receipt", out v1) == TaskDispatchPolicy.StartTimeAnchor.Receipt && v1
                && TaskDispatchPolicy.ParseStartTimeAnchor(" predecessorcompletion ", out v2)
                   == TaskDispatchPolicy.StartTimeAnchor.PredecessorCompletion && v2
                && TaskDispatchPolicy.ParseStartTimeAnchor(null, out v3) == TaskDispatchPolicy.StartTimeAnchor.Receipt && v3
                && TaskDispatchPolicy.ParseStartTimeAnchor("ScenarioStart", out v4) == TaskDispatchPolicy.StartTimeAnchor.Receipt
                && !v4
                && TaskDispatchPolicy.ParseStartTimeAnchor("bogus", out v5) == TaskDispatchPolicy.StartTimeAnchor.Receipt && !v5
                && TaskDispatchPolicy.ParseStartTimeAnchor("RECEIPTABSOLUTE", out v6)
                   == TaskDispatchPolicy.StartTimeAnchor.ReceiptAbsolute && v6;
            Check(ref failures, parse,
                  "(g) Vrf:StartTimeAnchor parses Receipt / ReceiptAbsolute / PredecessorCompletion case-insensitively, " +
                  "blank = Receipt, and an unknown value (ScenarioStart is not offered) is INVALID and falls back to Receipt");
            Check(ref failures, TaskDispatchPolicy.StartAnchorClock(TaskDispatchPolicy.StartTimeAnchor.Receipt, 123.5) == 123.5
                                && TaskDispatchPolicy.StartAnchorClock(TaskDispatchPolicy.StartTimeAnchor.ReceiptAbsolute, 123.5) == 123.5
                                && double.IsNaN(TaskDispatchPolicy.StartAnchorClock(
                                       TaskDispatchPolicy.StartTimeAnchor.PredecessorCompletion, 123.5)),
                  "(g) Receipt and ReceiptAbsolute hand the gate the receipt reading; PredecessorCompletion hands it NaN " +
                  "(no anchor: the old gate)");
            Check(ref failures, new VrfSettings().StartTimeAnchor == "Receipt",
                  "(g) the C# default of Vrf:StartTimeAnchor is Receipt");

            string repo = FindRulingsRepoRoot();
            string appSettings = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "appsettings.json");
            string service = repo == null ? null : Path.Combine(repo, "src", "VrfC2SimApp", "VrfC2SimService.cs");
            bool onDisk = appSettings != null && File.Exists(appSettings) && File.Exists(service);
            Check(ref failures, onDisk, $"(g) appsettings.json and the service source are on disk ({appSettings})");
            if (onDisk)
            {
                var shipped = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                                  .AddJsonFile(appSettings, optional: false)
                                  .Build().GetSection("Vrf").Get<VrfSettings>() ?? new VrfSettings();
                var rolledBack = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                                     .AddJsonFile(appSettings, optional: false)
                                     .AddInMemoryCollection(new Dictionary<string, string>
                                     {
                                         ["Vrf:StartTimeAnchor"] = "PredecessorCompletion",
                                     }).Build().GetSection("Vrf").Get<VrfSettings>();
                var absolute = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                                   .AddJsonFile(appSettings, optional: false)
                                   .AddInMemoryCollection(new Dictionary<string, string>
                                   {
                                       ["Vrf:StartTimeAnchor"] = "ReceiptAbsolute",
                                   }).Build().GetSection("Vrf").Get<VrfSettings>();
                Check(ref failures, shipped.StartTimeAnchor == "Receipt"
                                    && rolledBack?.StartTimeAnchor == "PredecessorCompletion"
                                    && TaskDispatchPolicy.ParseStartTimeAnchor(rolledBack.StartTimeAnchor, out bool rv)
                                       == TaskDispatchPolicy.StartTimeAnchor.PredecessorCompletion && rv
                                    && TaskDispatchPolicy.ParseStartTimeAnchor(absolute?.StartTimeAnchor, out bool av)
                                       == TaskDispatchPolicy.StartTimeAnchor.ReceiptAbsolute && av,
                      "(g) the shipped appsettings.json says Receipt, and Vrf:StartTimeAnchor=PredecessorCompletion and " +
                      "=ReceiptAbsolute round-trip through the configuration stack");

                string src = File.ReadAllText(service);
                string gateCall = Stp850Between(src, "var gate = await _sequencer.WaitForStartAsync(task.StartAfterTaskUuid,",
                                                "if (gate != GateResult.Proceed)");
                Check(ref failures, Stp850CountOf(src, "double orderReceiptClock = TaskClockSeconds;") == 1
                                    && src.Contains("_ = RunTaskAsync(t, u, orderReceiptClock, orderMinOffsetMs);")
                                    && Stp850CountOf(src, "long orderMinOffsetMs = TaskDispatchPolicy.MinSimulationOffsetMs(") == 1
                                    && Stp850CountOf(src, "TaskDispatchPolicy.RebasedSimulationOffsetMs(") == 2
                                    && src.Contains("TaskDispatchPolicy.StartAnchorClock(_startTimeAnchor, orderReceiptClock)")
                                    && gateCall.StartsWith("var gate = await _sequencer.WaitForStartAsync(task.StartAfterTaskUuid, scaledStartMs,",
                                                           StringComparison.Ordinal)
                                    && gateCall.Contains("startAnchorClock: startAnchor"),
                      "(g) the service stamps receipt and the order's minimum offset ONCE per order, rebases each offset " +
                      "(dispatch AND the chain-lead line) through the same policy call, and the gate gets the " +
                      "DurationScale-scaled rebased offset (scaledStartMs) with the receipt anchor");
            }
        }

        // ------------------------------------------------------------------ (h) ----
        {
            string repo = FindRulingsRepoRoot();
            string data = repo == null ? null : Path.Combine(repo, "data");
            string golden = repo == null ? null : Path.Combine(repo, "docs", "golden-trace", "orders");
            var files = new List<string>();
            if (data != null && Directory.Exists(data)) files.AddRange(Directory.GetFiles(data, "*Order*.xml"));
            if (golden != null && Directory.Exists(golden)) files.AddRange(Directory.GetFiles(golden, "*.xml"));
            files.Sort(StringComparer.Ordinal);
            int dataFiles = files.Count(f => Path.GetDirectoryName(f) == data);
            int goldenFiles = files.Count - dataFiles;
            int parsed = 0, gatedTasks = 0, gatedWithOffset = 0, leadDiffers = 0;
            var unparsed = new List<string>();
            var rebasedOrders = new List<string>();
            var ironstormOffsets = new List<string>();
            foreach (var f in files)
            {
                // EVERY file must parse to at least one task: OrderParser swallows a deserialize failure and
                // returns an EMPTY order, so zero tasks is a parse failure here, and it is named, not skipped.
                OrderData order = null;
                try { order = OrderParser.Parse(File.ReadAllText(f)); }
                catch (Exception ex) { unparsed.Add($"{Path.GetFileName(f)} ({ex.GetType().Name})"); continue; }
                if (order == null || order.Tasks.Count == 0) { unparsed.Add($"{Path.GetFileName(f)} (no tasks)"); continue; }
                parsed++;
                foreach (var t in order.Tasks)
                {
                    if (!string.IsNullOrEmpty(t.StartAfterTaskUuid))
                    {
                        gatedTasks++;
                        if (t.SimulationStartMs > 0 || t.AbsoluteStartUtc != null) gatedWithOffset++;
                    }
                    if (Path.GetFileName(f) == "IRONSTORM_CUTA_Order.xml")
                        ironstormOffsets.Add($"{(t.TaskName ?? "").Split('_')[0]}={t.SimulationStartMs / 1000}s" +
                                             (string.IsNullOrEmpty(t.StartAfterTaskUuid) ? "" : "(gated)"));
                }
                // All three values: the logged lead must agree whenever the order's minimum offset is 0. An order
                // whose minimum is NOT 0 is named - Receipt rebases it, the other two do not (a legitimate change).
                long min = TaskDispatchPolicy.MinSimulationOffsetMs(order.Tasks.Select(t => (t.HasSimulationStart, t.SimulationStartMs)));
                double lPc = LeadUnder(order, TaskDispatchPolicy.StartTimeAnchor.PredecessorCompletion);
                double lAbs = LeadUnder(order, TaskDispatchPolicy.StartTimeAnchor.ReceiptAbsolute);
                double lRec = LeadUnder(order, TaskDispatchPolicy.StartTimeAnchor.Receipt);
                if (min > 0) rebasedOrders.Add($"{Path.GetFileName(f)} (min {min / 1000} s: lead {lAbs:F0} -> {lRec:F0} s)");
                else if (lPc != lAbs || lAbs != lRec) leadDiffers++;
            }
            Check(ref failures, dataFiles > 0 && goldenFiles > 0 && unparsed.Count == 0,
                  $"(h) every order file parses to at least one task - {dataFiles} in data/, {goldenFiles} in " +
                  $"docs/golden-trace/orders/ ({(unparsed.Count == 0 ? "none failed" : "FAILED: " + string.Join(", ", unparsed))})");
            Check(ref failures, parsed == files.Count && gatedTasks > 0 && gatedWithOffset == 0 && leadDiffers == 0,
                  $"(h) PARITY: {parsed} orders parse; their {gatedTasks} gated tasks carry NO SimulationTime " +
                  $"or DateTime start ({gatedWithOffset} do), so the anchor never reaches them, and the logged chain " +
                  $"lead is identical under all THREE values for every order whose minimum offset is 0 " +
                  $"({leadDiffers} such orders differ)");
            Check(ref failures, rebasedOrders.Count == 1 && rebasedOrders[0].StartsWith("PROBE_RIDGE_1-35_DELAYED_Order.xml ",
                                                                                      StringComparison.Ordinal),
                  $"(h) the ONLY on-disk order Receipt rebases is the one-task N2c probe (its 300 s delay is pulled to " +
                  $"receipt; ReceiptAbsolute keeps it): {string.Join(", ", rebasedOrders)}");
            Check(ref failures, ironstormOffsets.Count == 5 && ironstormOffsets.All(s => !s.Contains("(gated)") || s.Contains("=0s")),
                  $"(h) IRONSTORM_CUTA_Order.xml's offsets are on ROOTS only: {string.Join(", ", ironstormOffsets)}");

            foreach (var name in new[] { "COA-STP1_Order.xml", "IRONSTORM_CUTA_Order.xml" })
            {
                string f = data == null ? null : Path.Combine(data, name);
                if (f == null || !File.Exists(f)) { Check(ref failures, false, $"(h) {name} is on disk"); continue; }
                var order = OrderParser.Parse(File.ReadAllText(f));
                var before = WalkChain(GraphUnder(order, TaskDispatchPolicy.StartTimeAnchor.PredecessorCompletion),
                                       600.0, 60.0, 1.0, Stp850Backstop, 60.0);
                var abs = WalkChain(GraphUnder(order, TaskDispatchPolicy.StartTimeAnchor.ReceiptAbsolute),
                                    600.0, 60.0, 1.0, Stp850Backstop, 60.0, anchorAtReceipt: true);
                var after = WalkChain(GraphUnder(order, TaskDispatchPolicy.StartTimeAnchor.Receipt),
                                      600.0, 60.0, 1.0, Stp850Backstop, 60.0, anchorAtReceipt: true);
                bool same = SameWalk(before, abs) && SameWalk(abs, after);
                Check(ref failures, same && after.Dispatched == order.Tasks.Count,
                      $"(h) PARITY: {name} walks IDENTICALLY under all three values - {after.Dispatched} of " +
                      $"{order.Tasks.Count} dispatched, every dispatch at the same task-clock second");
            }
        }

        // ------------------------------------------------------------------ (i) ----
        {
            // A GATED DateTime StartTime, through the service's own conversion (TaskDispatchPolicy.StartOffsetMs:
            // the instant minus the wall clock at receipt) and then the gate, exactly as RunTaskAsync hands it.
            var receiptUtc = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
            long offsetMs = TaskDispatchPolicy.StartOffsetMs(0L, receiptUtc.AddSeconds(D), receiptUtc);
            bool conversion = offsetMs == (long)(D * 1000.0)
                              && TaskDispatchPolicy.StartOffsetMs(0L, receiptUtc.AddSeconds(-60), receiptUtc) == 0L
                              && TaskDispatchPolicy.StartOffsetMs(5000L, receiptUtc.AddSeconds(D), receiptUtc) == 5000L;
            Check(ref failures, conversion,
                  $"(i) a DateTime StartTime becomes an offset from receipt ({offsetMs / 1000} s for an instant {D:F0} s " +
                  "after it), a past instant becomes 0, and a SimulationTime offset takes precedence");

            foreach (bool receipt in new[] { false, true })
            {
                var opened = new List<double>();
                foreach (double predEnd in new[] { D / 2.0, 1.5 * D })
                {
                    var r = new AnchorRig(receipt);
                    r.Dispatch("P", 0.0);
                    var g = r.Gate("P", offsetMs / 1000.0, window: predEnd + 60.0);
                    r.StepTo(predEnd); r.Complete("P");
                    opened.Add(r.RunUntilOpen(g, 4.0 * D));
                }
                if (receipt)
                    Check(ref failures, opened[0] == D && opened[1] == 1.5 * D,
                          $"(i) STP-850 Receipt, a gated DateTime start {D:F0} s after receipt: predecessor ends early " +
                          $"({D / 2.0:F0} s) -> opens at THE INSTANT ({opened[0]:F0} s); ends late ({1.5 * D:F0} s) -> " +
                          $"opens at its completion ({opened[1]:F0} s)");
                else
                    Check(ref failures, opened[0] == 1.5 * D && opened[1] == 2.5 * D,
                          $"FAIL-FIRST (i) PredecessorCompletion: the same DateTime start is served AFTER the predecessor " +
                          $"- opens at {opened[0]:F0} s and {opened[1]:F0} s, never at the instant");
            }
            Check(ref failures,
                  TaskDispatchPolicy.RebasedSimulationOffsetMs(0L, false, (long)(3.0 * D * 1000.0),
                                                               TaskDispatchPolicy.StartTimeAnchor.Receipt) == 0L
                  && TaskDispatchPolicy.StartOffsetMs(
                         TaskDispatchPolicy.RebasedSimulationOffsetMs(0L, false, (long)(3.0 * D * 1000.0),
                                                                      TaskDispatchPolicy.StartTimeAnchor.Receipt),
                         receiptUtc.AddSeconds(D), receiptUtc) == (long)(D * 1000.0),
                  "(i) SPLIT ORDERS: a DateTime start is an INSTANT - the Receipt rebase never touches it (still " +
                  $"{D:F0} s after receipt with the order's minimum at {3.0 * D:F0} s)");
        }

        // ------------------------------------------------------------------ (j) ----
        {
            // SPLIT ORDERS. STP sends one order per phase wave and keeps ABSOLUTE slots: wave 2 of a plan with
            // d = 60 min carries U1's slots 3 and 4 (180 and 240 min, STREND between them) and U2's slot 4
            // (240 min). STP emits no STREND to a wave-1 task (STP-886), so each unit's first wave-2 task is a
            // root here. Receipt must start the wave on its own receipt: U1 at +0 and +60 min, U2 at +60.
            var wave2 = new List<(ChainTask Task, bool HasSim)>
            {
                (new ChainTask("U1-S3", "", (long)(D * 1000.0), (long)(3.0 * D * 1000.0)), true),
                (new ChainTask("U1-S4", "U1-S3", (long)(D * 1000.0), (long)(4.0 * D * 1000.0)), true),
                (new ChainTask("U2-S4", "", (long)(D * 1000.0), (long)(4.0 * D * 1000.0)), true),
            };
            var rec = WalkChain(Rebased(wave2, TaskDispatchPolicy.StartTimeAnchor.Receipt), 600.0, 60.0, 1.0,
                                Stp850Backstop, 60.0, anchorAtReceipt: true);
            var abs = WalkChain(Rebased(wave2, TaskDispatchPolicy.StartTimeAnchor.ReceiptAbsolute), 600.0, 60.0, 1.0,
                                Stp850Backstop, 60.0, anchorAtReceipt: true);
            Check(ref failures, abs.At("U1-S3") == 3.0 * D && abs.At("U1-S4") == 4.0 * D && abs.At("U2-S4") == 4.0 * D,
                  $"FAIL-FIRST (j) ReceiptAbsolute: wave 2 IDLES three phases after its receipt - U1 at " +
                  $"{abs.Times("U1-S3", "U1-S4")} s, U2 at {abs.Times("U2-S4")} s");
            Check(ref failures, rec.At("U1-S3") == 0.0 && rec.At("U1-S4") == D && rec.At("U2-S4") == D && rec.SkippedCount == 0,
                  $"(j) STP-850 Receipt, SPLIT ORDER wave 2 (offsets 180/240 + 240 min, minimum 180): U1 starts at " +
                  $"receipt +{rec.Times("U1-S3", "U1-S4")} s and U2 at +{rec.Times("U2-S4")} s - the wave starts on its receipt");
            var scaled = WalkChain(Rebased(wave2, TaskDispatchPolicy.StartTimeAnchor.Receipt), 600.0, 60.0, 0.25,
                                   Stp850Backstop, 15.0, anchorAtReceipt: true);
            Check(ref failures, scaled.At("U1-S3") == 0.0 && scaled.At("U1-S4") == 0.25 * D && scaled.At("U2-S4") == 0.25 * D,
                  $"(j) Vrf:DurationScale=0.25 scales the REBASED offset: wave 2 at +{scaled.Times("U1-S3", "U1-S4", "U2-S4")} s");
        }

        // ------------------------------------------------------------------ (k) ----
        {
            // A FULL-PLAN order (a slot-0 task present, minimum 0) is not touched by the rebase.
            var plan = new List<(ChainTask Task, bool HasSim)>();
            foreach (var t in SlotChain(5, D, 1.0)) plan.Add((t with { Uuid = "U1-" + t.Uuid, Pred = t.Pred == "" ? "" : "U1-" + t.Pred }, true));
            foreach (var t in SlotChain(5, D, 1.0)) plan.Add((t with { Uuid = "U2-" + t.Uuid, Pred = t.Pred == "" ? "" : "U2-" + t.Pred }, true));
            var rec = WalkChain(Rebased(plan, TaskDispatchPolicy.StartTimeAnchor.Receipt), 600.0, 60.0, 1.0,
                                Stp850Backstop, 60.0, anchorAtReceipt: true);
            var abs = WalkChain(Rebased(plan, TaskDispatchPolicy.StartTimeAnchor.ReceiptAbsolute), 600.0, 60.0, 1.0,
                                Stp850Backstop, 60.0, anchorAtReceipt: true);
            Check(ref failures, SameWalk(rec, abs) && rec.Dispatched == 10 && rec.At("U2-T5") == 4.0 * D,
                  $"(k) a FULL-PLAN order (minimum offset 0) dispatches IDENTICALLY under Receipt and ReceiptAbsolute " +
                  $"({rec.Dispatched} dispatched; U2's last at {rec.At("U2-T5"):F0} s)");
        }

        // ------------------------------------------------------------------ (l) ----
        {
            long H(double slots) => (long)(slots * D * 1000.0);
            long pinned = TaskDispatchPolicy.MinSimulationOffsetMs(new[] { (false, 0L), (true, H(3)), (true, H(4)) });
            long real0 = TaskDispatchPolicy.MinSimulationOffsetMs(new[] { (true, 0L), (true, H(3)) });
            long none = TaskDispatchPolicy.MinSimulationOffsetMs(new[] { (false, 0L), (false, 0L) });
            Check(ref failures, pinned == H(3) && real0 == 0L && none == 0L,
                  $"(l) a task with NO StartTime (or a DateTime one) does not pin the order's minimum ({pinned / 1000} s, " +
                  $"not 0); an EXPLICIT SimulationTime of 0 does ({real0}); no carried offset at all -> 0 ({none})");

            string repo = FindRulingsRepoRoot();
            string coa = repo == null ? null : Path.Combine(repo, "data", "COA-STP1_Order.xml");
            if (coa != null && File.Exists(coa))
            {
                var order = OrderParser.Parse(File.ReadAllText(coa));
                int carried = order.Tasks.Count(t => t.HasSimulationStart);
                int gatedCarried = order.Tasks.Count(t => t.HasSimulationStart && !string.IsNullOrEmpty(t.StartAfterTaskUuid));
                Check(ref failures, carried == 11 && gatedCarried == 0,
                      $"(l) the parser marks a PRESENT SimulationTime: COA-STP1 has {carried} (10 x P0 + T13's 3h20m), none " +
                      $"of them on its 31 RelativeTime-started gated tasks ({gatedCarried})");
            }
            else Check(ref failures, false, "(l) data/COA-STP1_Order.xml is on disk");
        }
    }

    /// <summary>STP-850 split orders: the suite graph with each offset rebased as the service does it.</summary>
    private static List<ChainTask> Rebased(List<(ChainTask Task, bool HasSim)> tasks, TaskDispatchPolicy.StartTimeAnchor anchor)
    {
        long min = TaskDispatchPolicy.MinSimulationOffsetMs(tasks.Select(t => (t.HasSim, t.Task.StartDelayMs)));
        return tasks.Select(t => t.Task with
        {
            StartDelayMs = TaskDispatchPolicy.RebasedSimulationOffsetMs(t.Task.StartDelayMs, t.HasSim, min, anchor),
        }).ToList();
    }

    /// <summary>An on-disk order as the gate graph under this anchor (rebased under Receipt).</summary>
    private static List<ChainTask> GraphUnder(OrderData order, TaskDispatchPolicy.StartTimeAnchor anchor)
        => Rebased(order.Tasks.Select(t => (new ChainTask(t.TaskUuid, t.StartAfterTaskUuid, t.DurationMs,
                                                          t.SimulationStartMs), t.HasSimulationStart)).ToList(), anchor);

    /// <summary>The E4 lead the service logs for this order under this anchor.</summary>
    private static double LeadUnder(OrderData order, TaskDispatchPolicy.StartTimeAnchor anchor)
    {
        long min = TaskDispatchPolicy.MinSimulationOffsetMs(order.Tasks.Select(t => (t.HasSimulationStart, t.SimulationStartMs)));
        var nodes = order.Tasks.Select(t => TaskDispatchPolicy.ChainNodeFor(t.TaskUuid, t.StartAfterTaskUuid, t.DurationMs,
            TaskDispatchPolicy.RebasedSimulationOffsetMs(t.SimulationStartMs, t.HasSimulationStart, min, anchor),
            t.RelativeDelayMs, anchor)).ToList();
        return TaskDispatchPolicy.LongestChainLeadSeconds(nodes, 1.0);
    }

    private static bool SameWalk(ChainOutcome a, ChainOutcome b)
        => a.Dispatched == b.Dispatched && a.SkippedCount == b.SkippedCount
           && a.DispatchedAt.All(kv => b.DispatchedAt.TryGetValue(kv.Key, out var at) && at == kv.Value);

    /// <summary>A serial one-unit chain as STP exports it: task k (1-based) in slot k-1, Duration one slot,
    /// SimulationTime (k-1) x slot, STREND on task k-1.</summary>
    private static List<ChainTask> SlotChain(int n, double slotSeconds, double durationSlots)
    {
        var chain = new List<ChainTask>();
        for (int k = 0; k < n; k++)
            chain.Add(new ChainTask("T" + (k + 1), k == 0 ? "" : "T" + k,
                                    (long)(durationSlots * slotSeconds * 1000.0), (long)(k * slotSeconds * 1000.0)));
        return chain;
    }

    /// <summary>One gate at a time on the StepClock, with the service's call shape: the backstop as phase 1
    /// (a predecessor in this order), the offset as SimulationTime, and the anchor at receipt = clock 0.</summary>
    private sealed class AnchorRig
    {
        public readonly StepClock Clock = new();
        public readonly TaskSequencer Seq = new();
        private readonly bool _receipt;
        public AnchorRig(bool receipt) { _receipt = receipt; }

        public void Dispatch(string uuid, double at) => Seq.NotifyDispatched(uuid, at);
        public void Complete(string uuid) => Seq.CompleteTask(uuid);

        // The clock reading at which each gate opened, taken by a synchronous continuation: the clock never
        // advances while a step is settling, so the reading is the step the gate opened at.
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Task<GateResult>, double> _openedAt = new();

        public Task<GateResult> Gate(string pred, double offsetSeconds, double window,
                                     double overdueBackstop = double.NaN)
        {
            var gate = Seq.WaitForStartAsync(pred, (long)(offsetSeconds * 1000.0), 0L, window, Clock.AsTaskClock(),
                                             CancellationToken.None, Stp850Backstop, overdueBackstop,
                                             startAnchorClock: _receipt ? 0.0 : double.NaN);
            gate.ContinueWith(g => _openedAt[g] = Clock.Now, TaskContinuationOptions.ExecuteSynchronously);
            return gate;
        }

        /// <summary>Advance in one-second steps to this reading, letting every gate settle at each step that
        /// released a clock waiter (a step that released none changes nothing a gate can see).</summary>
        public void StepTo(double seconds)
        {
            Quiet();
            while (Clock.Now < seconds)
                if (Clock.AdvanceTo(Math.Min(seconds, Math.Floor(Clock.Now) + 1.0)) > 0) Quiet();
        }

        /// <summary>The clock reading at which the gate opened (NaN if it did not by the horizon).</summary>
        public double RunUntilOpen(Task<GateResult> gate, double horizon)
        {
            Settle(gate);
            while (!gate.IsCompleted && Clock.Now < horizon)
                if (Clock.AdvanceTo(Math.Floor(Clock.Now) + 1.0) > 0) Settle(gate);
            Settle(gate);
            return _openedAt.TryGetValue(gate, out double at) ? at : double.NaN;
        }

        private void Settle(Task<GateResult> gate)
        {
            Quiet();
            try { gate.Wait(TimeSpan.FromMilliseconds(20)); } catch (AggregateException) { }
            for (int i = 0; i < 200 && gate.IsCompleted && !_openedAt.ContainsKey(gate); i++) Thread.Sleep(1);
        }

        // Nothing moves until the pool has run what the last step released and the gates have registered
        // their next wait: settle on an unchanged registration count for a few quiet rounds.
        private void Quiet()
        {
            long last = -1;
            for (int quiet = 0; quiet < 40; quiet++)
            {
                long now = Volatile.Read(ref Clock.Registrations);
                if (now != last) { last = now; quiet = 0; }
                if (quiet < 20) Thread.Yield(); else Thread.Sleep(1);
            }
        }
    }

    private static string Stp850Between(string s, string from, string to)
    {
        int a = s.IndexOf(from, StringComparison.Ordinal);
        if (a < 0) return "";
        int b = s.IndexOf(to, a + from.Length, StringComparison.Ordinal);
        return b < 0 ? s.Substring(a) : s.Substring(a, b - a);
    }

    private static int Stp850CountOf(string s, string needle)
    {
        int n = 0;
        for (int i = s.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = s.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
            n++;
        return n;
    }
}
