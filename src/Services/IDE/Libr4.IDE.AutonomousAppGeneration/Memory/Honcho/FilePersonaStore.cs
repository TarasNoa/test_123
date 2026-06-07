namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Honcho;

public interface IPersonaStore
{
    Task<PersonaDocument?> LoadAsync(string userId, string projectKey, CancellationToken ct = default);

    Task SaveAsync(PersonaDocument document, CancellationToken ct = default);

    string GetPersonaPath(string userId, string projectKey);
}

public sealed class FilePersonaStore : IPersonaStore
{
    private readonly HonchoMemoryOptions _options;

    public FilePersonaStore(Microsoft.Extensions.Options.IOptions<HonchoMemoryOptions> options) =>
        _options = options.Value;

    public async Task<PersonaDocument?> LoadAsync(string userId, string projectKey, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var path = GetPersonaPath(userId, projectKey);
        if (!File.Exists(path))
            return null;

        var content = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        return PersonaDocument.Parse(userId, projectKey, content);
    }

    public async Task SaveAsync(PersonaDocument document, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ct.ThrowIfCancellationRequested();

        var path = GetPersonaPath(document.UserId, document.ProjectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, document.ToMarkdown(), ct).ConfigureAwait(false);
    }

    public string GetPersonaPath(string userId, string projectKey)
    {
        var safeUser = Profile.UserProfileIdentityResolver.SanitizeUserId(userId);
        var safeProject = Profile.UserProfileIdentityResolver.SanitizeUserId(projectKey);
        var root = Path.IsPathRooted(_options.PersonaRoot)
            ? _options.PersonaRoot
            : Path.GetFullPath(_options.PersonaRoot);
        return Path.Combine(root, safeUser, "projects", safeProject, "PERSONA.md");
    }
}
