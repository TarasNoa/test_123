using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class TypeScriptToCSharpDomainMapperTests
{
    [Fact]
    public void MapUpstreamFiles_EmitsEnumRecordAndColumns()
    {
        const string ts = """
            export enum ColumnStatus { Backlog = 'backlog', Done = 'done' }
            export interface Card { id: string; title: string; done?: boolean; }
            const boardColumns = ['Бэклог', 'В работе', 'Готово'];
            """;

        var map = TypeScriptToCSharpDomainMapper.MapUpstreamFiles(
            new[] { ("upstream/src/board.ts", ts) });

        map.Enums.Should().ContainSingle(e => e.Name == "ColumnStatus");
        map.Records.Should().ContainSingle(r => r.Name == "Card");
        map.ColumnArrays.Should().Contain(c => c.Labels.Contains("Бэклог"));

        var csharp = TypeScriptToCSharpDomainMapper.GenerateCSharpFile("GeneratedApp.Api", map);
        csharp.Should().Contain("enum ColumnStatus");
        csharp.Should().Contain("record Card");
        csharp.Should().Contain("UpstreamColumnDefinitions");
    }
}
