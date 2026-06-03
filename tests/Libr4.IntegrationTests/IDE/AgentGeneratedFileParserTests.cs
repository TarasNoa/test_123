using Libr4.IDE.AutonomousAppGeneration.Agents;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AgentGeneratedFileParserTests
{
    [Fact]
    public void TryParse_ParsesJsonFilesArray()
    {
        const string content = """
            Here is the output:
            {"files":[{"relativePath":"backend/pom.xml","content":"<project/>"},{"relativePath":"frontend/src/App.tsx","content":"export default function App(){}"}]}
            """;

        var files = AgentGeneratedFileParser.TryParse(content);

        Assert.Equal(2, files.Count);
        Assert.Contains(files, f => f.RelativePath == "backend/pom.xml");
        Assert.Contains(files, f => f.RelativePath == "frontend/src/App.tsx");
    }

    [Fact]
    public void HasParseableFiles_ReturnsFalse_ForEmptyContent()
    {
        Assert.False(AgentGeneratedFileParser.HasParseableFiles(null));
        Assert.False(AgentGeneratedFileParser.HasParseableFiles("no files here"));
    }

    [Fact]
    public void TryParse_MergesMultipleJsonBlocks()
    {
        const string content = """
            {"files":[{"relativePath":"backend/pom.xml","content":"a"}]}
            later
            {"files":[{"relativePath":"frontend/package.json","content":"{}"}]}
            """;

        var files = AgentGeneratedFileParser.TryParse(content);
        Assert.Equal(2, files.Count);
    }

    [Fact]
    public void TryParse_ParsesJsonInsideMarkdownFence()
    {
        const string content = """
            ```json
            {"files":[{"relativePath":"backend/App.java","content":"class App{}"}]}
            ```
            """;

        var files = AgentGeneratedFileParser.TryParse(content);
        Assert.Single(files);
        Assert.Equal("backend/App.java", files[0].RelativePath);
    }
}
