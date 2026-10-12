using Microsoft.Extensions.Logging;

namespace VrfC2SimApp;

/// <summary>
/// THE C2SIM SDK'S EXPECTED-CANCELLATION LINE, RE-LEVELLED (REHEARSAL_WAYB_HAND_2026-10-05 F5; the 2026-10-11 runs again).
///
/// On every clean stop the SDK cancels its STOMP reader, and C2SIMClientSTOMPLib.GetNext_Block logs that EXPECTED
/// cancellation with LogError("STOMP block reading cancelled", ...). The console then ends a perfectly clean run with
/// "fail: C2SIM.C2SIMSDK[0] STOMP block reading cancelled" - the last line an audience sees. The SDK is a shared library
/// (Software/Library/CS/C2SIMSDK, also STP's), so the interface does not edit it: it hands the SDK a logger factory that
/// logs exactly that message, at exactly Error, at Information instead. Every other SDK message - and that text at any
/// other level - passes through unchanged, so a real STOMP failure still reads "fail:".
/// Checked by --liveness-selftest ("SDK shutdown noise", fail-first against the raw factory).
/// </summary>
public static class SdkLogging
{
    /// <summary>The SDK's message text, verbatim (C2SIMClientSTOMPLib.cs GetNext_Block).</summary>
    public const string ExpectedCancel = "STOMP block reading cancelled";

    /// <summary>The level a message is written at: the expected cancellation at Error becomes Information.</summary>
    public static LogLevel EffectiveLevel(LogLevel level, string message)
        => level == LogLevel.Error && string.Equals(message, ExpectedCancel, StringComparison.Ordinal)
            ? LogLevel.Information
            : level;

    /// <summary>A factory for the SDK that delegates to <paramref name="inner"/> (the host's), re-levelling only the
    /// expected cancellation. Disposing it does NOT dispose the host's factory.</summary>
    public static ILoggerFactory Wrap(ILoggerFactory inner) => new Factory(inner);

    private sealed class Factory : ILoggerFactory
    {
        private readonly ILoggerFactory _inner;
        public Factory(ILoggerFactory inner) { _inner = inner; }
        public ILogger CreateLogger(string categoryName) => new Relevelling(_inner.CreateLogger(categoryName));
        public void AddProvider(ILoggerProvider provider) => _inner.AddProvider(provider);
        public void Dispose() { }   // the host owns the inner factory
    }

    private sealed class Relevelling : ILogger
    {
        private readonly ILogger _inner;
        public Relevelling(ILogger inner) { _inner = inner; }
        public IDisposable BeginScope<TState>(TState state) => _inner.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                                Func<TState, Exception, string> formatter)
        {
            LogLevel level = logLevel == LogLevel.Error ? EffectiveLevel(logLevel, formatter(state, exception)) : logLevel;
            if (_inner.IsEnabled(level)) _inner.Log(level, eventId, state, exception, formatter);
        }
    }
}
