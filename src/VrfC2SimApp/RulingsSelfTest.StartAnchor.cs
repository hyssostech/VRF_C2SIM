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
///       under both values;
///   (i) a gated DateTime StartTime is anchored too: start = max(predecessor completion, that instant).
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
            bool v1, v2, v3, v4, v5;
            bool parse =
                TaskDispatchPolicy.ParseStartTimeAnchor("Receipt", out v1) == TaskDispatchPolicy.StartTimeAnchor.Receipt && v1
                && TaskDispatchPolicy.ParseStartTimeAnchor(" predecessorcompletion ", out v2)
                   == TaskDispatchPolicy.StartTimeAnchor.PredecessorCompletion && v2
                && TaskDispatchPolicy.ParseStartTimeAnchor(null, out v3) == TaskDispatchPolicy.StartTimeAnchor.Receipt && v3
                && TaskDispatchPolicy.ParseStartTimeAnchor("ScenarioStart", out v4) == TaskDispatchPolicy.StartTimeAnchor.Receipt
                && !v4
                && TaskDispatchPolicy.ParseStartTimeAnchor("bogus", out v5) == TaskDispatchPolicy.StartTimeAnchor.Receipt && !v5;
            Check(ref failures, parse,
                  "(g) Vrf:StartTimeAnchor parses Receipt / PredecessorCompletion case-insensitively, blank = Receipt, and " +
                  "an unknown value (ScenarioStart is not offered) is INVALID and falls back to Receipt");
            Check(ref failures, TaskDispatchPolicy.StartAnchorClock(TaskDispatchPolicy.StartTimeAnchor.Receipt, 123.5) == 123.5
                                && double.IsNaN(TaskDispatchPolicy.StartAnchorClock(
                                       TaskDispatchPolicy.StartTimeAnchor.PredecessorCompletion, 123.5)),
                  "(g) Receipt hands the gate the receipt reading; PredecessorCompletion hands it NaN (no anchor: the old gate)");
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
                Check(ref failures, shipped.StartTimeAnchor == "Receipt"
                                    && rolledBack?.StartTimeAnchor == "PredecessorCompletion"
                                    && TaskDispatchPolicy.ParseStartTimeAnchor(rolledBack.StartTimeAnchor, out bool rv)
                                       == TaskDispatchPolicy.StartTimeAnchor.PredecessorCompletion && rv,
                      "(g) the shipped appsettings.json says Receipt, and Vrf:StartTimeAnchor=PredecessorCompletion " +
                      "round-trips through the configuration stack to the rollback value");

                string src = File.ReadAllText(service);
                string gateCall = Stp850Between(src, "var gate = await _sequencer.WaitForStartAsync(task.StartAfterTaskUuid,",
                                                "if (gate != GateResult.Proceed)");
                Check(ref failures, Stp850CountOf(src, "double orderReceiptClock = TaskClockSeconds;") == 1
                                    && src.Contains("_ = RunTaskAsync(t, u, orderReceiptClock);")
                                    && src.Contains("TaskDispatchPolicy.StartAnchorClock(_startTimeAnchor, orderReceiptClock)")
                                    && gateCall.StartsWith("var gate = await _sequencer.WaitForStartAsync(task.StartAfterTaskUuid, scaledStartMs,",
                                                           StringComparison.Ordinal)
                                    && gateCall.Contains("startAnchorClock: startAnchor"),
                      "(g) the service stamps receipt ONCE per order on the task clock, hands it to every task, and the " +
                      "gate gets the DurationScale-scaled offset (scaledStartMs) with that anchor");
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
                var nodesOld = new List<TaskDispatchPolicy.ChainNode>();
                var nodesNew = new List<TaskDispatchPolicy.ChainNode>();
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
                    nodesOld.Add(TaskDispatchPolicy.ChainNodeFor(t.TaskUuid, t.StartAfterTaskUuid, t.DurationMs,
                        t.SimulationStartMs, t.RelativeDelayMs, TaskDispatchPolicy.StartTimeAnchor.PredecessorCompletion));
                    nodesNew.Add(TaskDispatchPolicy.ChainNodeFor(t.TaskUuid, t.StartAfterTaskUuid, t.DurationMs,
                        t.SimulationStartMs, t.RelativeDelayMs, TaskDispatchPolicy.StartTimeAnchor.Receipt));
                }
                if (TaskDispatchPolicy.LongestChainLeadSeconds(nodesOld, 1.0)
                    != TaskDispatchPolicy.LongestChainLeadSeconds(nodesNew, 1.0)) leadDiffers++;
            }
            Check(ref failures, dataFiles > 0 && goldenFiles > 0 && unparsed.Count == 0,
                  $"(h) every order file parses to at least one task - {dataFiles} in data/, {goldenFiles} in " +
                  $"docs/golden-trace/orders/ ({(unparsed.Count == 0 ? "none failed" : "FAILED: " + string.Join(", ", unparsed))})");
            Check(ref failures, parsed == files.Count && gatedTasks > 0 && gatedWithOffset == 0 && leadDiffers == 0,
                  $"(h) PARITY: {parsed} orders parse; their {gatedTasks} gated tasks carry NO SimulationTime " +
                  $"or DateTime start ({gatedWithOffset} do), so the anchor never reaches them, and the logged chain " +
                  $"lead is identical under both values ({leadDiffers} orders differ)");
            Check(ref failures, ironstormOffsets.Count == 5 && ironstormOffsets.All(s => !s.Contains("(gated)") || s.Contains("=0s")),
                  $"(h) IRONSTORM_CUTA_Order.xml's offsets are on ROOTS only: {string.Join(", ", ironstormOffsets)}");

            foreach (var name in new[] { "COA-STP1_Order.xml", "IRONSTORM_CUTA_Order.xml" })
            {
                string f = data == null ? null : Path.Combine(data, name);
                if (f == null || !File.Exists(f)) { Check(ref failures, false, $"(h) {name} is on disk"); continue; }
                var order = OrderParser.Parse(File.ReadAllText(f));
                var graph = new List<ChainTask>();
                foreach (var t in order.Tasks)
                    graph.Add(new ChainTask(t.TaskUuid, t.StartAfterTaskUuid, t.DurationMs, t.SimulationStartMs));
                var before = WalkChain(graph, 600.0, 60.0, 1.0, Stp850Backstop, 60.0);
                var after = WalkChain(graph, 600.0, 60.0, 1.0, Stp850Backstop, 60.0, anchorAtReceipt: true);
                bool same = before.Dispatched == after.Dispatched && before.SkippedCount == after.SkippedCount
                            && before.DispatchedAt.All(kv => after.DispatchedAt.TryGetValue(kv.Key, out var at) && at == kv.Value);
                Check(ref failures, same && after.Dispatched == order.Tasks.Count,
                      $"(h) PARITY: {name} walks IDENTICALLY under both values - {after.Dispatched} of {order.Tasks.Count} " +
                      $"dispatched, every dispatch at the same task-clock second");
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
        }
    }

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
