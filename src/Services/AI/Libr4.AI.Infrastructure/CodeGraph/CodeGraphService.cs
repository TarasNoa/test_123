using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.CodeGraph;

public class CodeGraphService
{
    private readonly KnowledgeGraph _graph;
    private readonly CodeExtractor _extractor;
    private readonly ILogger<CodeGraphService> _logger;

    public CodeGraphService(ILogger<CodeGraphService> logger)
    {
        _graph = new KnowledgeGraph();
        _extractor = new CodeExtractor();
        _logger = logger;
    }

    public async Task BuildGraphFromDirectory(string directoryPath, string[]? fileExtensions = null)
    {
        var extensions = fileExtensions ?? new[] { ".cs" };
        
        if (!Directory.Exists(directoryPath))
        {
            _logger.LogError("Directory does not exist: {Directory}", directoryPath);
            return;
        }

        var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

        _logger.LogInformation("Found {Count} files to process", files.Count());

        foreach (var file in files)
        {
            try
            {
                var extraction = await _extractor.ExtractFromCSharpFile(file);
                _graph.Merge(extraction);
                _logger.LogDebug("Processed file: {File} - {Nodes} nodes, {Edges} edges", 
                    file, extraction.Nodes.Count, extraction.Edges.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process file: {File}", file);
            }
        }

        _logger.LogInformation("Graph built: {NodeCount} nodes, {EdgeCount} edges", 
            _graph.NodeCount, _graph.EdgeCount);
    }

    public async Task BuildGraphFromFile(string filePath)
    {
        try
        {
            var extraction = await _extractor.ExtractFromCSharpFile(filePath);
            _graph.Merge(extraction);
            _logger.LogInformation("Added file: {File} - {Nodes} nodes, {Edges} edges", 
                filePath, extraction.Nodes.Count, extraction.Edges.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process file: {File}", filePath);
        }
    }

    public List<CodeNode> GetNodes()
    {
        return _graph.GetNodes();
    }

    public List<CodeEdge> GetEdges()
    {
        return _graph.GetEdges();
    }

    public List<CodeNode> GetRelatedNodes(string nodeId)
    {
        return _graph.GetRelatedNodes(nodeId);
    }

    public List<CodeNode> FindNodesByType(string nodeType)
    {
        return _graph.FindNodesByType(nodeType);
    }

    public List<CodeNode> FindNodesByLabel(string label)
    {
        return _graph.FindNodesByLabel(label);
    }

    public void Clear()
    {
        _graph.Clear();
    }

    public (int NodeCount, int EdgeCount) GetStats()
    {
        return (_graph.NodeCount, _graph.EdgeCount);
    }
}
