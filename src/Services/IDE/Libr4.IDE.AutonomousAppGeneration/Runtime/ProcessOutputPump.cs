using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

/// <summary>
/// Drains redirected process streams without deadlocking on tools that write
/// carriage-return progress (Maven/npm) without newline terminators.
/// </summary>
internal static class ProcessOutputPump
{
    public static Task PumpAsync(
        TextReader reader,
        string stream,
        List<ConsoleLogEntry> logs,
        CancellationToken ct)
    {
        return Task.Run(async () =>
        {
            var buffer = new char[4096];
            var pending = new System.Text.StringBuilder();

            while (!ct.IsCancellationRequested)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
                if (read == 0)
                    break;

                for (var i = 0; i < read; i++)
                {
                    var ch = buffer[i];
                    if (ch is '\n' or '\r')
                    {
                        FlushPending(stream, logs, pending);
                        continue;
                    }

                    pending.Append(ch);
                }
            }

            FlushPending(stream, logs, pending);
        }, ct);
    }

    private static void FlushPending(string stream, List<ConsoleLogEntry> logs, System.Text.StringBuilder pending)
    {
        if (pending.Length == 0)
            return;

        var line = pending.ToString();
        pending.Clear();
        lock (logs)
        {
            logs.Add(new ConsoleLogEntry(DateTime.UtcNow, stream, line));
        }
    }
}
