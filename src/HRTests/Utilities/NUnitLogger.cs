using Microsoft.Extensions.Logging;
using System;

namespace HRTests.Utilities;

internal class NUnitLogger<T> : ILogger<T>, IDisposable
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        TestContext.Out.WriteLine($"{logLevel}: {state}");
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) => this;

    public void Dispose()
    { }
}
