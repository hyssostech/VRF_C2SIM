using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using C2SIM;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SdkVerify;

internal static class Program
{
    const string NS = "http://www.sisostds.org/schemas/C2SIM/1.1";
    static int _pass, _fail;

    static void Check(string name, bool ok, string detail = "")
    {
        if (ok) { _pass++; Console.WriteLine($"  PASS  {name}"); }
        else { _fail++; Console.WriteLine($"  FAIL  {name} {detail}"); }
    }

    static string Header() =>
        $"<C2SIMHeader xmlns=\"{NS}\">"
        + "<CommunicativeActTypeCode>Inform</CommunicativeActTypeCode>"
        + "<ConversationID>11111111-1111-1111-1111-111111111111</ConversationID>"
        + "<FromSendingSystem>VERIFY</FromSendingSystem>"
        + "<MessageID>22222222-2222-2222-2222-222222222222</MessageID>"
        + "<Protocol>SISO-STD-C2SIM</Protocol>"
        + "<ProtocolVersion>1.0.2</ProtocolVersion>"
        + "<ToReceivingSystem>ALL</ToReceivingSystem>"
        + "</C2SIMHeader>";

    static string Msg(string bodyInner) =>
        $"<Message xmlns=\"{NS}\">{Header()}<MessageBody>{bodyInner}</MessageBody></Message>";

    static readonly string OrderMsg = Msg("<DomainMessageBody><OrderBody><OrderID>ORD-1</OrderID></OrderBody></DomainMessageBody>");
    static readonly string ObjInitMsg = Msg("<ObjectInitializationBody><ObjectDefinitions><Route><Name>R1</Name></Route></ObjectDefinitions></ObjectInitializationBody>");
    static readonly string ReportMsg = Msg("<DomainMessageBody><ReportBody><ReportID>REP-1</ReportID></ReportBody></DomainMessageBody>");
    static readonly string InitMsg = Msg("<C2SIMInitializationBody><Name>INIT-1</Name></C2SIMInitializationBody>");

    static async Task<int> Main()
    {
        Console.WriteLine("=== 1. Dispose without Connect (was NullReferenceException) ===");
        TestDisposeWithoutConnect();

        Console.WriteLine("\n=== 2. C2SIMClientRESTLib static mutable state ===");
        TestRestFieldsAreInstance();

        Console.WriteLine("\n=== 3. STOMP pump dispatch (fake broker) ===");
        await TestPumpDispatch();

        Console.WriteLine($"\n==== {_pass} passed, {_fail} failed ====");
        return _fail == 0 ? 0 : 1;
    }

    static void TestDisposeWithoutConnect()
    {
        var settings = new C2SIMSDKSettings
        {
            SubmitterId = "VERIFY",
            RestUrl = "http://127.0.0.1:8080/C2SIMServer",
            RestPassword = "pw",
            StompUrl = "http://127.0.0.1:61699/topic/C2SIM",
        };
        try
        {
            var sdk = new C2SIMSDK(NullLoggerFactory.Instance, settings);
            sdk.Dispose();       // Connect() never called -> _cancellationSource is null
            Check("Dispose() before Connect() does not throw", true);
        }
        catch (Exception e)
        {
            Check("Dispose() before Connect() does not throw", false, $"-> {e.GetType().Name}: {e.Message}");
        }
    }

    static void TestRestFieldsAreInstance()
    {
        Type t = Type.GetType("C2SimClientLib.C2SIMClientRESTLib, C2SIMClientLib");
        if (t is null) { Check("locate C2SIMClientRESTLib", false); return; }

        foreach (string f in new[] { "_protocol", "_protocolVersion" })
        {
            FieldInfo fi = t.GetField(f, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            Check($"{f} is an instance field", fi != null && !fi.IsStatic,
                  fi == null ? "-> field not found" : "-> still static");
        }
        foreach (string f in new[] { "_cachedXDoc", "_cachedXDocHash" })
        {
            FieldInfo fi = t.GetField(f, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            Check($"{f} removed (hash-keyed cache)", fi == null, "-> still present");
        }
        MethodInfo dp = t.GetMethod("DetermineProtocol", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Check("DetermineProtocol is an instance method", dp != null && !dp.IsStatic,
              dp == null ? "-> not found" : "-> still static");
    }

    static async Task TestPumpDispatch()
    {
        int port = 61699;
        using var broker = new FakeStompBroker(port);
        broker.Start();

        var settings = new C2SIMSDKSettings
        {
            SubmitterId = "VERIFY",
            RestUrl = "http://127.0.0.1:8080/C2SIMServer",
            RestPassword = "pw",
            StompUrl = $"http://127.0.0.1:{port}/topic/C2SIM",
        };

        using var sdk = new C2SIMSDK(NullLoggerFactory.Instance, settings);

        int raw = 0, order = 0, oder = 0, objInit = 0, init = 0, report = 0, errors = 0;
        Exception lastError = null;

        sdk.C2SIMMessageReceived += (s, e) => Interlocked.Increment(ref raw);
        sdk.OrderReceived += (s, e) => Interlocked.Increment(ref order);
#pragma warning disable CS0618
        sdk.OderReceived += (s, e) => Interlocked.Increment(ref oder);
#pragma warning restore CS0618
        sdk.ObjectInitializationReceived += (s, e) => Interlocked.Increment(ref objInit);
        sdk.InitializationReceived += (s, e) => Interlocked.Increment(ref init);
        sdk.Error += (s, e) => { Interlocked.Increment(ref errors); lastError = e; };
        // A throwing subscriber is the realistic source of pump exceptions
        sdk.ReportReceived += (s, e) =>
        {
            Interlocked.Increment(ref report);
            throw new InvalidOperationException("subscriber blew up");
        };

        await sdk.Connect();
        await broker.ClientConnected;

        broker.SendMessage(OrderMsg);
        broker.SendMessage(ObjInitMsg);
        broker.SendMessage(InitMsg);
        broker.SendMessage(ReportMsg);

        // Poll rather than sleep blindly
        for (int i = 0; i < 100 && raw < 4; i++) await Task.Delay(50);

        Check("all 4 messages reached C2SIMMessageReceived", raw == 4, $"-> raw={raw}");
        Check("OrderBody -> OrderReceived", order == 1, $"-> {order}");
        Check("OrderBody -> OderReceived (deprecated, still raised)", oder == 1, $"-> {oder}");
        Check("ObjectInitializationBody -> ObjectInitializationReceived", objInit == 1, $"-> {objInit} (was silently dropped)");
        Check("C2SIMInitializationBody -> InitializationReceived", init == 1, $"-> {init}");
        Check("ReportBody -> ReportReceived", report == 1, $"-> {report}");
        Check("throwing subscriber surfaces via Error event", errors == 1, $"-> errors={errors} (OnError previously had no call site)");
        Check("Error carries the root exception", lastError is InvalidOperationException,
              $"-> {lastError?.GetType().Name ?? "null"}");

        // Pump must survive a throwing subscriber
        broker.SendMessage(OrderMsg);
        for (int i = 0; i < 100 && order < 2; i++) await Task.Delay(50);
        Check("pump still alive after subscriber threw", order == 2, $"-> order={order}");

        // Clean shutdown must not raise Error
        int errorsBefore = errors;
        await sdk.Disconnect();
        await Task.Delay(300);
        Check("Disconnect() raises no spurious Error", errors == errorsBefore, $"-> {errors - errorsBefore} extra");
    }
}

/// <summary>Minimal STOMP 1.2 broker: CONNECTED handshake, then MESSAGE frames on demand.</summary>
internal sealed class FakeStompBroker : IDisposable
{
    readonly TcpListener _listener;
    readonly TaskCompletionSource<bool> _connected = new();
    NetworkStream _stream;

    public Task ClientConnected => _connected.Task;

    public FakeStompBroker(int port) => _listener = new TcpListener(IPAddress.Loopback, port);

    public void Start()
    {
        _listener.Start();
        _ = Task.Run(async () =>
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            _stream = client.GetStream();
            // Drain the CONNECT + SUBSCRIBE frames the client sends
            var buf = new byte[4096];
            await _stream.ReadAsync(buf, 0, buf.Length);
            Send("CONNECTED\nversion:1.2\n\n\0\n");
            _connected.TrySetResult(true);
        });
    }

    public void SendMessage(string xml)
    {
        // Client reads content line-by-line and concatenates, so keep the XML on one line
        string frame = "MESSAGE\n"
                     + "destination:/topic/C2SIM\n"
                     + $"content-length:{Encoding.UTF8.GetByteCount(xml)}\n"
                     + "\n"
                     + xml + "\n"
                     + "\0\n";
        Send(frame);
    }

    void Send(string s)
    {
        byte[] b = Encoding.UTF8.GetBytes(s);
        _stream.Write(b, 0, b.Length);
        _stream.Flush();
    }

    public void Dispose()
    {
        try { _stream?.Dispose(); } catch { }
        try { _listener.Stop(); } catch { }
    }
}
