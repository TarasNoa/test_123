using System.Text;
using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic TypeScript → C# domain projection for upstream snapshot files (enums, interfaces, const columns).
/// </summary>
public static class TypeScriptToCSharpDomainMapper
{
    private static readonly Regex ExportEnum = new(
        @"export\s+enum\s+(\w+)\s*\{([^}]{1,2000})\}",
        RegexOptions.Singleline | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(500));

    private static readonly Regex ExportInterface = new(
        @"export\s+interface\s+(\w+)\s*\{([^}]{1,4000})\}",
        RegexOptions.Singleline | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(500));

    private static readonly Regex InterfaceProperty = new(
        @"(\w+)\s*(\??)\s*:\s*([^;,\n]+)",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    private static readonly Regex ExportTypeAlias = new(
        @"export\s+type\s+(\w+)\s*=\s*([^;\n]+)",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    private static readonly Regex ConstStringArray = new(
        @"const\s+(\w+)\s*=\s*\[([^\]]{1,800})\]",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(300));

    public static TypeScriptDomainMapResult MapUpstreamFiles(IEnumerable<(string Path, string Content)> upstreamFiles)
    {
        var enums = new List<MappedEnum>();
        var records = new List<MappedRecord>();
        var aliases = new List<MappedAlias>();
        var columnArrays = new List<MappedColumnArray>();

        foreach (var (path, content) in upstreamFiles)
        {
            if (!IsTypeScriptLike(path) || string.IsNullOrWhiteSpace(content))
                continue;

            foreach (Match m in ExportEnum.Matches(content))
            {
                var name = m.Groups[1].Value;
                var members = ParseEnumMembers(m.Groups[2].Value);
                if (members.Count > 0)
                    enums.Add(new MappedEnum(name, path, members));
            }

            foreach (Match m in ExportInterface.Matches(content))
            {
                var name = m.Groups[1].Value;
                var props = ParseInterfaceProperties(m.Groups[2].Value);
                if (props.Count > 0)
                    records.Add(new MappedRecord(name, path, props));
            }

            foreach (Match m in ExportTypeAlias.Matches(content))
            {
                var name = m.Groups[1].Value.Trim();
                var target = m.Groups[2].Value.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                    aliases.Add(new MappedAlias(name, MapTypeExpression(target), path));
            }

            foreach (Match m in ConstStringArray.Matches(content))
            {
                var labels = ExtractQuotedStrings(m.Groups[2].Value);
                if (labels.Count >= 2)
                    columnArrays.Add(new MappedColumnArray(m.Groups[1].Value, path, labels));
            }
        }

        return new TypeScriptDomainMapResult(
            enums.GroupBy(e => e.Name, StringComparer.Ordinal).Select(g => g.First()).ToList(),
            records.GroupBy(r => r.Name, StringComparer.Ordinal).Select(g => g.First()).ToList(),
            aliases.GroupBy(a => a.Name, StringComparer.Ordinal).Select(g => g.First()).ToList(),
            columnArrays);
    }

    public static string GenerateCSharpFile(string ns, TypeScriptDomainMapResult map)
    {
        if (map.Enums.Count == 0 && map.Records.Count == 0 && map.Aliases.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("// Auto-mapped from upstream TypeScript snapshot (deterministic).");
        sb.AppendLine($"namespace {ns}.Domain.Adapted;");
        sb.AppendLine();

        foreach (var en in map.Enums)
        {
            sb.AppendLine($"/// <summary>From {en.SourcePath}</summary>");
            sb.Append("public enum ").Append(en.Name).AppendLine();
            sb.AppendLine("{");
            for (var i = 0; i < en.Members.Count; i++)
            {
                var member = en.Members[i];
                sb.Append("    ").Append(member.Name);
                if (member.NumericValue.HasValue)
                    sb.Append(" = ").Append(member.NumericValue.Value);
                if (i < en.Members.Count - 1)
                    sb.Append(',');
                sb.AppendLine();
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        foreach (var rec in map.Records)
        {
            sb.AppendLine($"/// <summary>From {rec.SourcePath}</summary>");
            sb.Append("public sealed record ").Append(rec.Name).Append('(');
            var props = rec.Properties
                .Select(p => $"{MapTypeExpression(p.TypeName)} {ToPascal(p.Name)}")
                .ToList();
            sb.Append(string.Join(", ", props));
            sb.AppendLine(");");
            sb.AppendLine();
        }

        if (map.ColumnArrays.Count > 0)
        {
            sb.AppendLine("public static class UpstreamColumnDefinitions");
            sb.AppendLine("{");
            foreach (var col in map.ColumnArrays)
            {
                var field = ToPascal(col.VariableName);
                var values = string.Join(", ", col.Labels.Select(l => $"\"{Escape(l)}\""));
                sb.AppendLine($"    public static readonly string[] {field} = new[] {{ {values} }};");
            }
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    public static string BuildMappingSummary(TypeScriptDomainMapResult map, int maxChars = 8_000)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TYPESCRIPT → C# DOMAIN MAP");
        foreach (var en in map.Enums)
            sb.AppendLine($"enum {en.Name} ({en.SourcePath}): {string.Join(", ", en.Members.Select(m => m.Name))}");
        foreach (var rec in map.Records)
            sb.AppendLine($"record {rec.Name} ({rec.SourcePath}): {string.Join(", ", rec.Properties.Select(p => $"{p.Name}:{p.TypeName}"))}");
        foreach (var col in map.ColumnArrays)
            sb.AppendLine($"columns {col.VariableName}: {string.Join(", ", col.Labels)}");

        var text = sb.ToString();
        return text.Length <= maxChars ? text : text[..maxChars] + "\n…(truncated)";
    }

    private static List<EnumMember> ParseEnumMembers(string body)
    {
        var members = new List<EnumMember>();
        foreach (var part in body.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            var name = part;
            int? numeric = null;
            if (part.Contains('='))
            {
                var eq = part.Split('=', 2, StringSplitOptions.TrimEntries);
                name = eq[0].Trim();
                var rhs = eq[1].Trim().Trim('\'', '"');
                if (int.TryParse(rhs, out var n))
                    numeric = n;
                else if (!string.IsNullOrWhiteSpace(rhs))
                    name = SanitizeIdentifier(name);
            }

            name = SanitizeIdentifier(name.Trim());
            if (char.IsLetter(name[0]) || name[0] == '_')
                members.Add(new EnumMember(SanitizeIdentifier(name), numeric));
        }

        return members;
    }

    private static List<InterfaceProperty> ParseInterfaceProperties(string body)
    {
        var props = new List<InterfaceProperty>();
        foreach (Match m in InterfaceProperty.Matches(body))
        {
            var name = m.Groups[1].Value;
            var optional = m.Groups[2].Value == "?";
            var typeName = m.Groups[3].Value.Trim();
            if (!string.IsNullOrWhiteSpace(name))
                props.Add(new InterfaceProperty(name, typeName, optional));
        }

        return props;
    }

    private static List<string> ExtractQuotedStrings(string fragment)
    {
        var list = new List<string>();
        foreach (Match m in Regex.Matches(fragment, @"['""]([^'""]{1,80})['""]", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200)))
            list.Add(m.Groups[1].Value);
        return list;
    }

    private static string MapTypeExpression(string ts)
    {
        var t = ts.Trim().TrimEnd(';');
        if (t.EndsWith("[]", StringComparison.Ordinal))
        {
            var inner = t[..^2].Trim();
            return $"IReadOnlyList<{MapTypeExpression(inner)}>";
        }

        return t.ToLowerInvariant() switch
        {
            "string" => "string",
            "number" => "double",
            "boolean" => "bool",
            "void" => "void",
            "any" or "unknown" => "object",
            "null" => "object?",
            _ when t.Contains('|') => MapUnion(t),
            _ => SanitizeIdentifier(t.Replace('.', '_'))
        };
    }

    private static string MapUnion(string union)
    {
        var parts = union.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.All(p => p is "string" or "number" or "boolean"))
            return "object";
        return parts.Length == 1 ? MapTypeExpression(parts[0]) : "object";
    }

    private static string ToPascal(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Value";
        if (name.Length == 1)
            return char.ToUpperInvariant(name[0]).ToString();
        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    private static string SanitizeIdentifier(string raw)
    {
        var sb = new StringBuilder();
        foreach (var c in raw)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
                sb.Append(c);
        }

        var s = sb.ToString();
        return string.IsNullOrWhiteSpace(s) ? "Member" : s;
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static bool IsTypeScriptLike(string path)
    {
        var lower = path.ToLowerInvariant();
        return lower.EndsWith(".ts") || lower.EndsWith(".tsx");
    }
}

public sealed record TypeScriptDomainMapResult(
    IReadOnlyList<MappedEnum> Enums,
    IReadOnlyList<MappedRecord> Records,
    IReadOnlyList<MappedAlias> Aliases,
    IReadOnlyList<MappedColumnArray> ColumnArrays);

public sealed record MappedEnum(string Name, string SourcePath, IReadOnlyList<EnumMember> Members);

public sealed record EnumMember(string Name, int? NumericValue);

public sealed record MappedRecord(string Name, string SourcePath, IReadOnlyList<InterfaceProperty> Properties);

public sealed record InterfaceProperty(string Name, string TypeName, bool Optional);

public sealed record MappedAlias(string Name, string CSharpType, string SourcePath);

public sealed record MappedColumnArray(string VariableName, string SourcePath, IReadOnlyList<string> Labels);
