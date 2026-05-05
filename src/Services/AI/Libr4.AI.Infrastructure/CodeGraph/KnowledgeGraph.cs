namespace Libr4.AI.Infrastructure.CodeGraph;

public class KnowledgeGraph
{
    private readonly Dictionary<string, CodeNode> _nodes;
    private readonly Dictionary<string, List<CodeEdge>> _edges;

    public KnowledgeGraph()
    {
        _nodes = new Dictionary<string, CodeNode>();
        _edges = new Dictionary<string, List<CodeEdge>>();
    }

    public void AddNode(CodeNode node)
    {
        _nodes[node.Id] = node;
    }

    public void AddEdge(CodeEdge edge)
    {
        if (!_edges.ContainsKey(edge.Source))
        {
            _edges[edge.Source] = new List<CodeEdge>();
        }
        _edges[edge.Source].Add(edge);
    }

    public void Merge((List<CodeNode> Nodes, List<CodeEdge> Edges) extraction)
    {
        foreach (var node in extraction.Nodes)
        {
            _nodes[node.Id] = node;
        }

        foreach (var edge in extraction.Edges)
        {
            if (!_edges.ContainsKey(edge.Source))
            {
                _edges[edge.Source] = new List<CodeEdge>();
            }
            
            // Avoid duplicate edges
            if (!_edges[edge.Source].Any(e => 
                e.Target == edge.Target && e.Relation == edge.Relation))
            {
                _edges[edge.Source].Add(edge);
            }
        }
    }

    public List<CodeNode> GetNodes()
    {
        return _nodes.Values.ToList();
    }

    public List<CodeEdge> GetEdges()
    {
        var allEdges = new List<CodeEdge>();
        foreach (var edgeList in _edges.Values)
        {
            allEdges.AddRange(edgeList);
        }
        return allEdges;
    }

    public List<CodeNode> GetRelatedNodes(string nodeId)
    {
        var related = new List<CodeNode>();
        
        if (_edges.ContainsKey(nodeId))
        {
            foreach (var edge in _edges[nodeId])
            {
                if (_nodes.ContainsKey(edge.Target))
                {
                    related.Add(_nodes[edge.Target]);
                }
            }
        }
        
        // Also find nodes that have edges pointing to this node
        foreach (var kvp in _edges)
        {
            foreach (var edge in kvp.Value)
            {
                if (edge.Target == nodeId && _nodes.ContainsKey(kvp.Key))
                {
                    related.Add(_nodes[kvp.Key]);
                }
            }
        }
        
        return related;
    }

    public List<CodeNode> FindNodesByType(string nodeType)
    {
        return _nodes.Values.Where(n => n.NodeType == nodeType).ToList();
    }

    public List<CodeNode> FindNodesByLabel(string label)
    {
        return _nodes.Values.Where(n => n.Label.Equals(label, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public void Clear()
    {
        _nodes.Clear();
        _edges.Clear();
    }

    public int NodeCount => _nodes.Count;
    public int EdgeCount => _edges.Values.Sum(list => list.Count);
}
