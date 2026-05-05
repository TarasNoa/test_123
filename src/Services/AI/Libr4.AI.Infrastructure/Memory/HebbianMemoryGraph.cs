namespace Libr4.AI.Infrastructure.Memory;

/// <summary>
/// Hebbian Associative Graph - links concepts that are activated together
/// Based on NGT Memory pattern: "neurons that fire together, wire together"
/// </summary>
public class HebbianMemoryGraph
{
    private readonly Dictionary<string, MemoryNode> _nodes = new();
    private readonly Dictionary<string, float> _edges = new();
    private readonly float _learningRate = 0.1f;
    private readonly float _decayRate = 0.01f;

    /// <summary>
    /// Add a memory fact and strengthen associations with related concepts
    /// </summary>
    public void AddMemory(string fact, List<string> concepts)
    {
        // Create or update nodes
        foreach (var concept in concepts)
        {
            if (!_nodes.ContainsKey(concept))
            {
                _nodes[concept] = new MemoryNode
                {
                    Concept = concept,
                    ActivationCount = 0,
                    LastAccessed = DateTimeOffset.UtcNow
                };
            }
            _nodes[concept].ActivationCount++;
            _nodes[concept].LastAccessed = DateTimeOffset.UtcNow;
        }

        // Strengthen edges between concepts that appear together
        for (int i = 0; i < concepts.Count; i++)
        {
            for (int j = i + 1; j < concepts.Count; j++)
            {
                var edgeKey = GetEdgeKey(concepts[i], concepts[j]);
                if (!_edges.ContainsKey(edgeKey))
                {
                    _edges[edgeKey] = 0f;
                }
                _edges[edgeKey] = Math.Min(1f, _edges[edgeKey] + _learningRate);
            }
        }

        // Decay old edges
        DecayEdges();
    }

    /// <summary>
    /// Get related concepts based on Hebbian associations
    /// </summary>
    public List<AssociatedConcept> GetAssociatedConcepts(string concept, int maxCount = 5)
    {
        var associations = new List<AssociatedConcept>();

        foreach (var kvp in _edges)
        {
            var parts = kvp.Key.Split('|');
            if (parts.Length == 2 && (parts[0] == concept || parts[1] == concept))
            {
                var relatedConcept = parts[0] == concept ? parts[1] : parts[0];
                associations.Add(new AssociatedConcept
                {
                    Concept = relatedConcept,
                    Strength = kvp.Value
                });
            }
        }

        return associations
            .OrderByDescending(a => a.Strength)
            .ThenByDescending(a => _nodes.TryGetValue(a.Concept, out var node) ? node.ActivationCount : 0)
            .Take(maxCount)
            .ToList();
    }

    /// <summary>
    /// Boost retrieval using Hebbian graph
    /// Returns original results + associated concepts
    /// </summary>
    public List<string> BoostRetrieval(List<string> originalResults, int boostCount = 3)
    {
        var allConcepts = originalResults.SelectMany(r => ExtractConcepts(r)).ToList();
        var boosted = new HashSet<string>(originalResults);

        foreach (var concept in allConcepts)
        {
            var associations = GetAssociatedConcepts(concept, boostCount);
            foreach (var assoc in associations.Where(a => a.Strength > 0.3f))
            {
                boosted.Add(assoc.Concept);
            }
        }

        return boosted.ToList();
    }

    /// <summary>
    /// Get hierarchical consolidation status
    /// Frequently accessed facts move to long-term memory
    /// </summary>
    public ConsolidationStatus GetConsolidationStatus(string concept)
    {
        if (!_nodes.TryGetValue(concept, out var node))
            return ConsolidationStatus.Unknown;

        // High activation count + recent access = consolidated
        if (node.ActivationCount > 10 && 
            (DateTimeOffset.UtcNow - node.LastAccessed).TotalDays < 30)
            return ConsolidationStatus.LongTerm;

        // Moderate activation = working memory
        if (node.ActivationCount > 3)
            return ConsolidationStatus.WorkingMemory;

        // Low activation = short-term
        return ConsolidationStatus.ShortTerm;
    }

    private void DecayEdges()
    {
        var keysToDecay = _edges.Keys.ToList();
        foreach (var key in keysToDecay)
        {
            _edges[key] = Math.Max(0f, _edges[key] - _decayRate);
            
            // Remove very weak edges
            if (_edges[key] < 0.05f)
            {
                _edges.Remove(key);
            }
        }
    }

    private string GetEdgeKey(string concept1, string concept2)
    {
        return string.Compare(concept1, concept2) < 0 
            ? $"{concept1}|{concept2}" 
            : $"{concept2}|{concept1}";
    }

    private List<string> ExtractConcepts(string text)
    {
        // Simple concept extraction - in production would use NLP
        var words = text.Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '(', ')' }, 
            StringSplitOptions.RemoveEmptyEntries);
        return words.Where(w => w.Length > 3).Distinct().ToList();
    }

    public void PruneOldNodes(TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        var nodesToRemove = _nodes
            .Where(kvp => kvp.Value.LastAccessed < cutoff && kvp.Value.ActivationCount < 2)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var node in nodesToRemove)
        {
            _nodes.Remove(node);
            // Remove associated edges
            var edgesToRemove = _edges.Keys.Where(k => k.StartsWith(node + "|") || k.EndsWith("|" + node)).ToList();
            foreach (var edge in edgesToRemove)
            {
                _edges.Remove(edge);
            }
        }
    }
}

public class MemoryNode
{
    public string Concept { get; set; } = string.Empty;
    public int ActivationCount { get; set; }
    public DateTimeOffset LastAccessed { get; set; }
}

public class AssociatedConcept
{
    public string Concept { get; set; } = string.Empty;
    public float Strength { get; set; }
}

public enum ConsolidationStatus
{
    Unknown,
    ShortTerm,
    WorkingMemory,
    LongTerm
}
