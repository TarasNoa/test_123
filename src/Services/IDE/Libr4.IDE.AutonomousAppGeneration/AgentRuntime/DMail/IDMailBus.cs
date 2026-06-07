namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.DMail;

public interface IDMailBus
{
    Task<DMailMessage> SendAsync(
        Guid runId,
        string from,
        string to,
        string payload,
        bool ackRequired = false,
        CancellationToken ct = default);

    Task<IReadOnlyList<DMailMessage>> ReadAsync(
        Guid runId,
        string? to = null,
        string? from = null,
        bool unackedOnly = false,
        CancellationToken ct = default);

    Task<bool> AckAsync(Guid runId, string messageId, CancellationToken ct = default);
}

public sealed class FileDMailBus : IDMailBus
{
    private readonly DMailOptions _options;

    public FileDMailBus(Microsoft.Extensions.Options.IOptions<DMailOptions> options) =>
        _options = options.Value;

    public async Task<DMailMessage> SendAsync(
        Guid runId,
        string from,
        string to,
        string payload,
        bool ackRequired = false,
        CancellationToken ct = default)
    {
        var message = new DMailMessage(
            Guid.NewGuid().ToString("N")[..12],
            runId,
            from,
            to,
            payload,
            ackRequired,
            DateTime.UtcNow);
        await WriteAsync(message, ct).ConfigureAwait(false);
        return message;
    }

    public async Task<IReadOnlyList<DMailMessage>> ReadAsync(
        Guid runId,
        string? to = null,
        string? from = null,
        bool unackedOnly = false,
        CancellationToken ct = default)
    {
        var dir = DMailDir(runId);
        if (!Directory.Exists(dir))
            return Array.Empty<DMailMessage>();

        var messages = new List<DMailMessage>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            var json = await File.ReadAllTextAsync(file, ct).ConfigureAwait(false);
            var message = System.Text.Json.JsonSerializer.Deserialize<DMailMessage>(json);
            if (message is null)
                continue;
            if (!string.IsNullOrWhiteSpace(to)
                && !message.To.Equals(to, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrWhiteSpace(from)
                && !message.From.Equals(from, StringComparison.OrdinalIgnoreCase))
                continue;
            if (unackedOnly && message.AckedAtUtc is not null)
                continue;
            messages.Add(message);
        }

        messages.Sort((a, b) => a.TimestampUtc.CompareTo(b.TimestampUtc));
        return messages;
    }

    public async Task<bool> AckAsync(Guid runId, string messageId, CancellationToken ct = default)
    {
        var path = MessagePath(runId, messageId);
        if (!File.Exists(path))
            return false;

        var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        var message = System.Text.Json.JsonSerializer.Deserialize<DMailMessage>(json);
        if (message is null)
            return false;

        var acked = message with { AckedAtUtc = DateTime.UtcNow };
        await WriteAsync(acked, ct).ConfigureAwait(false);
        return true;
    }

    private async Task WriteAsync(DMailMessage message, CancellationToken ct)
    {
        var dir = DMailDir(message.RunId);
        Directory.CreateDirectory(dir);
        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await File.WriteAllTextAsync(MessagePath(message.RunId, message.Id), json, ct).ConfigureAwait(false);
    }

    private string DMailDir(Guid runId) =>
        Path.Combine(_options.RunsRoot, runId.ToString("D"), "dmail");

    private string MessagePath(Guid runId, string messageId) =>
        Path.Combine(DMailDir(runId), $"{messageId}.json");
}
