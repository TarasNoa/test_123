using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Extensions;

public static class ExtensionManifestLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static ExtensionManifestDocument LoadFromFile(string path)
    {
        var yaml = File.ReadAllText(path);
        var doc = Deserializer.Deserialize<ExtensionManifestDocument>(yaml)
                  ?? throw new InvalidOperationException($"empty extension manifest: {path}");
        if (string.IsNullOrWhiteSpace(doc.Name))
            doc.Name = Path.GetFileName(Path.GetDirectoryName(path) ?? path);
        return doc;
    }

    public static IReadOnlyList<LoadedExtension> ScanDirectory(string root, ExtensionSource source)
    {
        if (!Directory.Exists(root))
            return Array.Empty<LoadedExtension>();

        var loaded = new List<LoadedExtension>();
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var manifestPath = ResolveManifestPath(dir);
            if (manifestPath is null)
                continue;

            try
            {
                var manifest = LoadFromFile(manifestPath);
                var id = NormalizeId(manifest.Name);
                loaded.Add(new LoadedExtension(id, dir, source, manifest, manifestPath));
            }
            catch
            {
                // skip invalid manifests — host logs during refresh
            }
        }

        return loaded;
    }

    private static string? ResolveManifestPath(string extensionDir)
    {
        foreach (var fileName in new[] { "extension.yaml", "extension.yml", "manifest.yaml", "manifest.yml" })
        {
            var candidate = Path.Combine(extensionDir, fileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public static string NormalizeId(string name) =>
        name.Trim().ToLowerInvariant().Replace(' ', '-');

    public static string ResolveRelativePath(string extensionRoot, string relativePath)
    {
        var combined = Path.GetFullPath(Path.Combine(extensionRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var normalizedRoot = Path.GetFullPath(extensionRoot);
        if (!combined.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"extension_path_escape: {relativePath}");
        return combined;
    }
}
