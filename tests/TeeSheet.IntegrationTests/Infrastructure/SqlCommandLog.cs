using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TeeSheet.IntegrationTests.Infrastructure;

/// <summary>
/// Logger provider that records every "Executed DbCommand" EF Core log line,
/// so a test can assert how many SQL commands an endpoint ran (N+1 detection).
/// </summary>
public sealed class SqlCommandLog : ILoggerProvider
{
    private const string Category = "Microsoft.EntityFrameworkCore.Database.Command";

    private readonly ConcurrentQueue<string> _commands = new();

    public IReadOnlyList<string> Commands => _commands.ToArray();

    public void Clear() => _commands.Clear();

    public ILogger CreateLogger(string categoryName) =>
        categoryName == Category ? new CommandLogger(_commands) : NullLogger.Instance;

    public void Dispose()
    {
    }

    private sealed class CommandLogger(ConcurrentQueue<string> sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (eventId.Id == RelationalEventId.CommandExecuted.Id)
            {
                sink.Enqueue(formatter(state, exception));
            }
        }
    }
}
