using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;

public sealed class FileUserProfileStore : IUserProfileStore
{
    private readonly UserProfileOptions _options;

    public FileUserProfileStore(IOptions<UserProfileOptions> options) => _options = options.Value;

    public async Task<UserProfileDocument?> LoadAsync(string userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var path = GetProfilePath(userId);
        if (!File.Exists(path))
            return null;

        var content = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        return UserProfileDocument.Parse(userId, content);
    }

    public async Task SaveAsync(UserProfileDocument document, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ct.ThrowIfCancellationRequested();

        var path = GetProfilePath(document.UserId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, document.ToMarkdown(), ct).ConfigureAwait(false);
    }

    public string GetProfilePath(string userId)
    {
        var safeUserId = UserProfileIdentityResolver.SanitizeUserId(userId);
        var root = Path.IsPathRooted(_options.UsersRoot)
            ? _options.UsersRoot
            : Path.GetFullPath(_options.UsersRoot);
        return Path.Combine(root, safeUserId, "USER.profile.md");
    }
}
