using System.Xml.Linq;
using C2SIM;
using Microsoft.Extensions.Logging;

namespace VrfC2SimApp;

/// <summary>
/// Offline check of the INBOUND parse path (no bridge, no MAK, no VR-Forces, no server):
/// `VrfC2SimApp --parse-selftest`. B5, 2026-09-14.
///
/// What it proves that --parse-init / --parse-order cannot: the parse does not make the SDK log
/// a FALSE ERROR. ToC2SIMObject logs "Failed to deserialize xml to type ..." through the SDK's own
/// static logger and THEN throws (C2SIMSSDK.cs:823), so a caller's catch hides the exception but
/// not the log line - two of them landed in every run log (one per inbound init, one per inbound
/// order) because both parsers used to try the MessageBody overload speculatively.
///
/// Method: install a CAPTURING ILoggerFactory as the SDK's logger (constructing a C2SIMSDK is what
/// sets the static field, C2SIMSSDK.cs:70-93 - the constructor only builds objects; it neither
/// connects nor sends), then parse each message in BOTH shapes it arrives in:
///   - the pushed FILE shape, &lt;MessageBody&gt;-rooted (data/*.xml);
///   - the LIVE event shape, the bare body the SDK's STOMP pump hands us (bodyElement.ToString()
///     of C2SIMInitializationBody / OrderBody - reproduced here by lifting that element out of
///     the same file, so the two cases are the same message);
///   - an order rooted at DomainMessageBody (the middle shape the schema allows).
/// Every case must parse to the SAME content AND leave the SDK's Error count at zero.
/// </summary>
public static class ParseSelfTest
{
    private const string InitFile = "data/R9_Mojave_Lean_Initialization.xml";
    private const string OrderFile = "data/R9_Mojave_UnitMove_Order.xml";

    public static int Run()
    {
        int failures = 0;

        // ---- root sniff (pure) ----
        Check(ref failures, C2SimXml.RootLocalName("<MessageBody xmlns=\"x\"><A/></MessageBody>") == "MessageBody",
              "RootLocalName reads a bare root element");
        Check(ref failures, C2SimXml.RootLocalName("<?xml version=\"1.0\"?><!-- c --><OrderBody/>") == "OrderBody",
              "RootLocalName skips the xml declaration AND a leading comment");
        Check(ref failures, C2SimXml.RootLocalName("") == "", "RootLocalName of an empty string is empty");
        Check(ref failures, C2SimXml.RootLocalName("not xml at all") == "",
              "RootLocalName of a non-xml string is empty (no throw)");

        // ---- install the capturing logger into the SDK ----
        var log = new CapturingLoggerFactory();
        // The settings are never used: nothing in this test connects or pushes. Real URLs only
        // because the constructor parses them (C2SIMSSDK.cs:76-80).
        var sdk = new C2SIMSDK(log, new C2SIMSDKSettings(
            "VrfC2SimApp-parse-selftest",
            "http://127.0.0.1:18080/C2SIMServer", "",
            "http://127.0.0.1:61613/topic/C2SIM",
            "SISO-STD-C2SIM", "1.0.2"));
        Check(ref failures, sdk != null, "SDK constructed (its static logger is now the capture)");

        string initPath = UnitTypeMap.ResolvePath(InitFile);   // walks up from the exe dir (shared resolver)
        string orderPath = UnitTypeMap.ResolvePath(OrderFile);
        if (initPath == null || orderPath == null)
        {
            Console.WriteLine($"  FAIL: sample not found (init '{InitFile}' -> {initPath ?? "(null)"}, " +
                              $"order '{OrderFile}' -> {orderPath ?? "(null)"})");
            return 1;
        }
        string initFileXml = File.ReadAllText(initPath);
        string orderFileXml = File.ReadAllText(orderPath);

        // ---- init: MessageBody-rooted FILE ----
        int errorsBefore = log.Errors.Count;
        var fromFile = InitParser.Parse(initFileXml);
        Console.WriteLine($"  init FILE  ({C2SimXml.RootLocalName(initFileXml)}-rooted): " +
                          $"{fromFile.Units.Count} unit(s), {fromFile.Areas.Count} area(s), " +
                          $"{log.Errors.Count - errorsBefore} SDK error(s).");
        Check(ref failures, fromFile.Units.Count > 0, "init FILE parsed to at least one unit");
        Check(ref failures, log.Errors.Count == errorsBefore, "init FILE parse logged NO SDK error");

        // ---- init: BARE body, as the live InitializationReceived event delivers it ----
        string bareInit = Inner(initFileXml, 1);
        Check(ref failures, C2SimXml.RootLocalName(bareInit) == "C2SIMInitializationBody",
              "lifted body is C2SIMInitializationBody-rooted (the live event shape)");
        errorsBefore = log.Errors.Count;
        var fromBare = InitParser.Parse(bareInit);
        Console.WriteLine($"  init BARE  (C2SIMInitializationBody-rooted): {fromBare.Units.Count} unit(s), " +
                          $"{log.Errors.Count - errorsBefore} SDK error(s).");
        Check(ref failures, fromBare.Units.Count == fromFile.Units.Count,
              "bare-body init parses to the SAME unit count as the file");
        Check(ref failures, log.Errors.Count == errorsBefore, "init BARE parse logged NO SDK error");

        // ---- order: MessageBody-rooted FILE ----
        errorsBefore = log.Errors.Count;
        var orderFromFile = OrderParser.Parse(orderFileXml);
        Console.WriteLine($"  order FILE ({C2SimXml.RootLocalName(orderFileXml)}-rooted): " +
                          $"{orderFromFile.Tasks.Count} task(s), {log.Errors.Count - errorsBefore} SDK error(s).");
        Check(ref failures, orderFromFile.Tasks.Count > 0, "order FILE parsed to at least one task");
        Check(ref failures, log.Errors.Count == errorsBefore, "order FILE parse logged NO SDK error");

        // ---- order: BARE OrderBody, as the live OrderReceived event delivers it ----
        string bareOrder = Inner(orderFileXml, 2);   // MessageBody -> DomainMessageBody -> OrderBody
        Check(ref failures, C2SimXml.RootLocalName(bareOrder) == "OrderBody",
              "lifted body is OrderBody-rooted (the live event shape)");
        errorsBefore = log.Errors.Count;
        var orderFromBare = OrderParser.Parse(bareOrder);
        Console.WriteLine($"  order BARE (OrderBody-rooted): {orderFromBare.Tasks.Count} task(s), " +
                          $"{log.Errors.Count - errorsBefore} SDK error(s).");
        Check(ref failures, orderFromBare.Tasks.Count == orderFromFile.Tasks.Count,
              "bare-body order parses to the SAME task count as the file");
        Check(ref failures, log.Errors.Count == errorsBefore, "order BARE parse logged NO SDK error");

        // ---- order: DomainMessageBody-rooted (the middle shape) ----
        string domainOrder = Inner(orderFileXml, 1);
        Check(ref failures, C2SimXml.RootLocalName(domainOrder) == C2SimXml.DomainMessageBody,
              "lifted body is DomainMessageBody-rooted");
        errorsBefore = log.Errors.Count;
        var orderFromDomain = OrderParser.Parse(domainOrder);
        Console.WriteLine($"  order DMB  (DomainMessageBody-rooted): {orderFromDomain.Tasks.Count} task(s), " +
                          $"{log.Errors.Count - errorsBefore} SDK error(s).");
        Check(ref failures, orderFromDomain.Tasks.Count == orderFromFile.Tasks.Count,
              "DomainMessageBody-rooted order parses to the SAME task count as the file");
        Check(ref failures, log.Errors.Count == errorsBefore, "order DMB parse logged NO SDK error");

        // ---- the headline: FOUR inbound messages, ZERO false SDK errors ----
        Check(ref failures, log.Errors.Count == 0,
              $"the whole inbound parse path logged NO SDK Error (saw {log.Errors.Count})");
        foreach (var e in log.Errors) Console.WriteLine("    SDK ERROR: " + e);

        Console.WriteLine();
        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>Descend <paramref name="depth"/> first-child elements and return that element's
    /// xml - the same lift the SDK's STOMP pump does before raising its events
    /// (C2SIMSSDK.cs:654-671, bodyElement.ToString()).</summary>
    private static string Inner(string xml, int depth)
    {
        var el = XDocument.Parse(xml).Root;
        for (int i = 0; i < depth && el != null; i++) el = el.Elements().FirstOrDefault();
        return el?.ToString() ?? "";
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }

    /// <summary>Records what the SDK logs, so a test can assert on the ERROR lines a run log
    /// would have shown. The SDK takes an ILoggerFactory and keeps the ILogger it creates in a
    /// static field (C2SIMSSDK.cs:50, :72) - that field is what ToC2SIMObject writes through.</summary>
    private sealed class CapturingLoggerFactory : ILoggerFactory
    {
        public readonly List<string> Errors = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);
        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }

        private sealed class CapturingLogger : ILogger
        {
            private readonly CapturingLoggerFactory _owner;
            public CapturingLogger(CapturingLoggerFactory owner) { _owner = owner; }
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                                    Func<TState, Exception, string> formatter)
            {
                if (logLevel < LogLevel.Error) return;
                _owner.Errors.Add(formatter != null ? formatter(state, exception) : state?.ToString() ?? "");
            }
        }
    }
}
