namespace Libr4.IDE.Domain.AgentMemorySystem;

/// <summary>
/// Cross-reference between memory fragments
/// </summary>
public class MemoryCrossReference
{
    public string ReferenceId { get; set; } = string.Empty;
    public string FromFragmentId { get; set; } = string.Empty;
    public string ToFragmentId { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty; // "related_to", "depends_on", "contradicts", etc.
    public float Weight { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public MemoryCrossReference()
    {
    }
    
    public MemoryCrossReference(string referenceId, string fromFragmentId, string toFragmentId, string relationshipType, float weight = 1.0f)
    {
        ReferenceId = referenceId;
        FromFragmentId = fromFragmentId;
        ToFragmentId = toFragmentId;
        RelationshipType = relationshipType;
        Weight = weight;
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Memory layer (from GenericAgent L0-L4)
/// </summary>
public enum MemoryLayer
{
    /// <summary>
    /// L0 - Meta Rules: Core behavioral rules and system constraints
    /// </summary>
    MetaRules,
    
    /// <summary>
    /// L1 - Insight Index: Minimal memory index for fast routing and recall
    /// </summary>
    InsightIndex,
    
    /// <summary>
    /// L2 - Global Facts: Stable knowledge accumulated over long-term operation
    /// </summary>
    GlobalFacts,
    
    /// <summary>
    /// L3 - Task Skills/SOPs: Reusable workflows for completing specific task types
    /// </summary>
    TaskSkills,
    
    /// <summary>
    /// L4 - Session Archive: Archived task records distilled from finished sessions
    /// </summary>
    SessionArchive
}

/// <summary>
/// Memory fragment with layer (from GenericAgent layered memory)
/// </summary>
public class LayeredMemoryFragment
{
    public Guid Id { get; private set; }
    public MemoryLayer Layer { get; private set; }
    public string Content { get; private set; }
    public Dictionary<string, string> Metadata { get; private set; }
    public double RelevanceScore { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastAccessedAt { get; private set; }
    public int AccessCount { get; private set; }
    
    public LayeredMemoryFragment(MemoryLayer layer, string content, Dictionary<string, string>? metadata = null)
    {
        Id = Guid.NewGuid();
        Layer = layer;
        Content = content;
        Metadata = metadata ?? new Dictionary<string, string>();
        RelevanceScore = 0.5;
        CreatedAt = DateTime.UtcNow;
        AccessCount = 0;
    }
    
    public void UpdateRelevanceScore(double score)
    {
        RelevanceScore = score;
    }
    
    public void RecordAccess()
    {
        LastAccessedAt = DateTime.UtcNow;
        AccessCount++;
    }
}

/// <summary>
/// Self-evolving skill (from GenericAgent skill crystallization)
/// </summary>
public class SelfEvolvingSkill
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string ExecutionPath { get; private set; }
    public int UsageCount { get; private set; }
    public double SuccessRate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public bool IsCrystallized { get; private set; }
    
    public SelfEvolvingSkill(string name, string description, string executionPath)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        ExecutionPath = executionPath;
        UsageCount = 0;
        SuccessRate = 0.5;
        CreatedAt = DateTime.UtcNow;
        IsCrystallized = false;
    }
    
    public void RecordUsage(bool success)
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
        
        // Update success rate with exponential moving average
        SuccessRate = (SuccessRate * 0.9) + (success ? 0.1 : 0.0);
    }
    
    public void Crystallize()
    {
        IsCrystallized = true;
    }
}

/// <summary>
/// Cognitive memory system with multi-sector memory
/// Enhanced with GenericAgent layered memory and self-evolution
/// </summary>
public class CognitiveMemorySystem
{
    public string SystemId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public Dictionary<MemorySector, SectorMemory> Sectors { get; set; } = new();
    public Dictionary<string, MemoryCrossReference> CrossReferences { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    
    /// <summary>
    /// Layered memory fragments (from GenericAgent L0-L4)
    /// </summary>
    public List<LayeredMemoryFragment> LayeredFragments { get; set; } = new();
    
    /// <summary>
    /// Self-evolving skills (from GenericAgent skill crystallization)
    /// </summary>
    public List<SelfEvolvingSkill> Skills { get; set; } = new();
    
    /// <summary>
    /// Insight index for fast routing (from GenericAgent L1)
    /// </summary>
    public Dictionary<string, List<Guid>> InsightIndex { get; set; } = new();
    
    public CognitiveMemorySystem()
    {
    }
    
    public CognitiveMemorySystem(string systemId, string agentId)
    {
        SystemId = systemId;
        AgentId = agentId;
        CreatedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
        
        // Initialize all sectors
        InitializeSectors();
        
        // Initialize hindsight banks
        HindsightBanks = new Dictionary<string, HindsightMemoryBank>();
    }
    
    /// <summary>
    /// Initialize all memory sectors
    /// </summary>
    private void InitializeSectors()
    {
        foreach (MemorySector sector in Enum.GetValues(typeof(MemorySector)))
        {
            var sectorId = $"{SystemId}_{sector}";
            Sectors[sector] = new SectorMemory(sectorId, sector);
        }
    }
    
    /// <summary>
    /// Store a fragment in a specific sector
    /// </summary>
    public void StoreInSector(MemorySector sector, MemoryFragment fragment)
    {
        if (!Sectors.ContainsKey(sector))
        {
            var sectorId = $"{SystemId}_{sector}";
            Sectors[sector] = new SectorMemory(sectorId, sector);
        }
        
        Sectors[sector].AddFragment(fragment);
        LastUpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Retrieve fragments from a sector
    /// </summary>
    public List<MemoryFragment> RetrieveFromSector(MemorySector sector, string query)
    {
        if (!Sectors.ContainsKey(sector))
        {
            return new List<MemoryFragment>();
        }
        
        Sectors[sector].Access();
        return Sectors[sector].GetFragments(query);
    }
    
    /// <summary>
    /// Retrieve all active fragments from a sector
    /// </summary>
    public List<MemoryFragment> RetrieveAllFromSector(MemorySector sector)
    {
        if (!Sectors.ContainsKey(sector))
        {
            return new List<MemoryFragment>();
        }
        
        Sectors[sector].Access();
        return Sectors[sector].GetActiveFragments();
    }
    
    /// <summary>
    /// Add a cross-reference between fragments
    /// </summary>
    public void AddCrossReference(string fromFragmentId, string toFragmentId, string relationshipType, float weight = 1.0f)
    {
        var referenceId = Guid.NewGuid().ToString();
        var reference = new MemoryCrossReference(referenceId, fromFragmentId, toFragmentId, relationshipType, weight);
        CrossReferences[referenceId] = reference;
        LastUpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Get related fragments
    /// </summary>
    public List<MemoryFragment> GetRelatedFragments(string fragmentId)
    {
        var relatedIds = CrossReferences
            .Where(r => r.Value.FromFragmentId == fragmentId)
            .Select(r => r.Value.ToFragmentId)
            .ToList();
        
        var allFragments = GetAllFragments();
        return allFragments.Where(f => relatedIds.Contains(f.Id.ToString())).ToList();
    }
    
    /// <summary>
    /// Get all fragments across all sectors
    /// </summary>
    public List<MemoryFragment> GetAllFragments()
    {
        return Sectors.Values.SelectMany(s => s.GetActiveFragments()).ToList();
    }
    
    /// <summary>
    /// Search across all sectors
    /// </summary>
    public List<MemoryFragment> SearchAll(string query)
    {
        return GetAllFragments()
            .Where(f => f.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
    
    /// <summary>
    /// Compress a sector
    /// </summary>
    public void CompressSector(MemorySector sector, MemoryCompressionLevel level)
    {
        if (Sectors.ContainsKey(sector))
        {
            Sectors[sector].CompressionLevel = level;
            LastUpdatedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Get memory system statistics
    /// </summary>
    public MemorySystemStatistics GetStatistics()
    {
        return new MemorySystemStatistics
        {
            SystemId = SystemId,
            AgentId = AgentId,
            TotalFragments = GetAllFragments().Count,
            SectorStatistics = Sectors.ToDictionary(
                kvp => kvp.Key,
                kvp => new SectorStatistics
                {
                    Sector = kvp.Key,
                    Capacity = kvp.Value.Capacity,
                    CurrentUsage = kvp.Value.CurrentUsage,
                    UsagePercentage = kvp.Value.UsagePercentage
                }),
            TotalCrossReferences = CrossReferences.Count,
            LastUpdatedAt = LastUpdatedAt,
            TotalLayeredFragments = LayeredFragments.Count,
            TotalSkills = Skills.Count
        };
    }
    
    /// <summary>
    /// Add layered memory fragment (from GenericAgent)
    /// </summary>
    public void AddLayeredFragment(LayeredMemoryFragment fragment)
    {
        LayeredFragments.Add(fragment);
        
        // Update insight index
        var contentKey = fragment.Content.ToLower();
        if (!InsightIndex.ContainsKey(contentKey))
            InsightIndex[contentKey] = new List<Guid>();
        
        InsightIndex[contentKey].Add(fragment.Id);
        LastUpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Crystallize execution path into skill (from GenericAgent)
    /// </summary>
    public SelfEvolvingSkill CrystallizeSkill(string name, string description, string executionPath)
    {
        var skill = new SelfEvolvingSkill(name, description, executionPath);
        skill.Crystallize();
        Skills.Add(skill);
        LastUpdatedAt = DateTime.UtcNow;
        return skill;
    }
    
    /// <summary>
    /// Recall skill by name (from GenericAgent)
    /// </summary>
    public SelfEvolvingSkill? RecallSkill(string name)
    {
        var skill = Skills.FirstOrDefault(s => s.Name == name);
        if (skill != null)
        {
            skill.RecordUsage(true);
            LastUpdatedAt = DateTime.UtcNow;
        }
        return skill;
    }
    
    /// <summary>
    /// Search layered memory by layer (from GenericAgent)
    /// </summary>
    public List<LayeredMemoryFragment> SearchLayeredFragments(MemoryLayer layer, string query, int topN = 10)
    {
        return LayeredFragments
            .Where(f => f.Layer == layer && f.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.RelevanceScore)
            .Take(topN)
            .ToList();
    }
    
    /// <summary>
    /// Get most used skills (from GenericAgent)
    /// </summary>
    public List<SelfEvolvingSkill> GetMostUsedSkills(int topN = 5)
    {
        return Skills.OrderByDescending(s => s.UsageCount).Take(topN).ToList();
    }
    
    /// <summary>
    /// Task complexity level (from cursor-memory-bank)
    /// </summary>
    public enum TaskComplexityLevel
    {
        /// <summary>
        /// Level 1: Quick Bug Fix - Single file changes, targeted fixes
        /// </summary>
        Level1,
        
        /// <summary>
        /// Level 2: Simple Enhancement - Multiple files, clear requirements
        /// </summary>
        Level2,
        
        /// <summary>
        /// Level 3: Intermediate Feature - New components, design decisions needed
        /// </summary>
        Level3,
        
        /// <summary>
        /// Level 4: Complex System - Multiple subsystems, architectural decisions
        /// </summary>
        Level4
    }
    
    /// <summary>
    /// Memory Bank file (from cursor-memory-bank)
    /// </summary>
    public class MemoryBankFile
    {
        public string FileName { get; private set; }
        public string Content { get; private set; }
        public DateTime LastUpdated { get; private set; }
        public int Version { get; private set; }
        
        public MemoryBankFile(string fileName, string content)
        {
            FileName = fileName;
            Content = content;
            LastUpdated = DateTime.UtcNow;
            Version = 1;
        }
        
        public void UpdateContent(string newContent)
        {
            Content = newContent;
            LastUpdated = DateTime.UtcNow;
            Version++;
        }
    }
    
    /// <summary>
    /// Memory Bank directory (from cursor-memory-bank)
    /// </summary>
    public class MemoryBankDirectory
    {
        public string Path { get; private set; }
        public Dictionary<string, MemoryBankFile> Files { get; private set; }
        public DateTime CreatedAt { get; private set; }
        
        public MemoryBankDirectory(string path)
        {
            Path = path;
            Files = new Dictionary<string, MemoryBankFile>();
            CreatedAt = DateTime.UtcNow;
        }
        
        public void AddFile(MemoryBankFile file)
        {
            Files[file.FileName] = file;
        }
        
        public MemoryBankFile? GetFile(string fileName)
        {
            return Files.ContainsKey(fileName) ? Files[fileName] : null;
        }
    }
    
    /// <summary>
    /// Hierarchical rule loading (from cursor-memory-bank)
    /// </summary>
    public class HierarchicalRuleLoader
    {
        public List<string> CoreRules { get; private set; }
        public Dictionary<string, List<string>> CommandSpecificRules { get; private set; }
        public Dictionary<TaskComplexityLevel, List<string>> ComplexitySpecificRules { get; private set; }
        public Dictionary<string, List<string>> SpecializedRules { get; private set; }
        
        public HierarchicalRuleLoader()
        {
            CoreRules = new List<string>();
            CommandSpecificRules = new Dictionary<string, List<string>>();
            ComplexitySpecificRules = new Dictionary<TaskComplexityLevel, List<string>>();
            SpecializedRules = new Dictionary<string, List<string>>();
        }
        
        public void AddCoreRule(string rule)
        {
            CoreRules.Add(rule);
        }
        
        public void AddCommandRule(string command, string rule)
        {
            if (!CommandSpecificRules.ContainsKey(command))
                CommandSpecificRules[command] = new List<string>();
            
            CommandSpecificRules[command].Add(rule);
        }
        
        public void AddComplexityRule(TaskComplexityLevel level, string rule)
        {
            if (!ComplexitySpecificRules.ContainsKey(level))
                ComplexitySpecificRules[level] = new List<string>();
            
            ComplexitySpecificRules[level].Add(rule);
        }
        
        public void AddSpecializedRule(string category, string rule)
        {
            if (!SpecializedRules.ContainsKey(category))
                SpecializedRules[category] = new List<string>();
            
            SpecializedRules[category].Add(rule);
        }
        
        /// <summary>
        /// Get rules for specific command and complexity
        /// </summary>
        public List<string> GetRules(string? command = null, TaskComplexityLevel? complexity = null)
        {
            var rules = new List<string>(CoreRules);
            
            if (command != null && CommandSpecificRules.ContainsKey(command))
                rules.AddRange(CommandSpecificRules[command]);
            
            if (complexity != null && ComplexitySpecificRules.ContainsKey(complexity.Value))
                rules.AddRange(ComplexitySpecificRules[complexity.Value]);
            
            return rules;
        }
        
        /// <summary>
        /// Calculate token savings from hierarchical loading
        /// </summary>
        public double CalculateTokenSavings()
        {
            int totalRules = CoreRules.Count +
                           CommandSpecificRules.Values.Sum(v => v.Count) +
                           ComplexitySpecificRules.Values.Sum(v => v.Count) +
                           SpecializedRules.Values.Sum(v => v.Count);
            
            int coreRulesOnly = CoreRules.Count;
            return ((double)(totalRules - coreRulesOnly) / totalRules) * 100;
        }
    }
    
    /// <summary>
    /// Add Memory Bank support (from cursor-memory-bank)
    /// </summary>
    public MemoryBankDirectory? MemoryBank { get; private set; }
    
    /// <summary>
    /// Hierarchical rule loader (from cursor-memory-bank)
    /// </summary>
    public HierarchicalRuleLoader? RuleLoader { get; private set; }
    
    /// <summary>
    /// Initialize Memory Bank (from cursor-memory-bank)
    /// </summary>
    public void InitializeMemoryBank(string path)
    {
        MemoryBank = new MemoryBankDirectory(path);
        RuleLoader = new HierarchicalRuleLoader();
        
        // Add core files
        MemoryBank.AddFile(new MemoryBankFile("tasks.md", "# Tasks\nSource of truth for task tracking"));
        MemoryBank.AddFile(new MemoryBankFile("activeContext.md", "# Active Context\nCurrent development focus"));
        MemoryBank.AddFile(new MemoryBankFile("progress.md", "# Progress\nImplementation status"));
    }
    
    /// <summary>
    /// Get Memory Bank file (from cursor-memory-bank)
    /// </summary>
    public MemoryBankFile? GetMemoryBankFile(string fileName)
    {
        return MemoryBank?.GetFile(fileName);
    }
    
    /// <summary>
    /// Update Memory Bank file (from cursor-memory-bank)
    /// </summary>
    public void UpdateMemoryBankFile(string fileName, string content)
    {
        var file = MemoryBank?.GetFile(fileName);
        if (file != null)
        {
            file.UpdateContent(content);
        }
        else if (MemoryBank != null)
        {
            MemoryBank.AddFile(new MemoryBankFile(fileName, content));
        }
    }
    
    /// <summary>
    /// Memory type (from hindsight biomimetic structure)
    /// </summary>
    public enum MemoryType
    {
        /// <summary>
        /// World: Facts about the world
        /// </summary>
        World,
        
        /// <summary>
        /// Experiences: Agent's own experiences
        /// </summary>
        Experiences,
        
        /// <summary>
        /// Mental Models: Learned understanding formed by reflecting
        /// </summary>
        MentalModels
    }
    
    /// <summary>
    /// Retrieval strategy (from hindsight)
    /// </summary>
    public enum RetrievalStrategy
    {
        /// <summary>
        /// Semantic: Vector similarity
        /// </summary>
        Semantic,
        
        /// <summary>
        /// Keyword: BM25 exact matching
        /// </summary>
        Keyword,
        
        /// <summary>
        /// Graph: Entity/temporal/causal links
        /// </summary>
        Graph,
        
        /// <summary>
        /// Temporal: Time range filtering
        /// </summary>
        Temporal
    }
    
    /// <summary>
    /// Biomimetic memory entry (from hindsight)
    /// </summary>
    public class BiomimeticMemoryEntry
    {
        public Guid Id { get; private set; }
        public MemoryType Type { get; private set; }
        public string Content { get; private set; }
        public DateTime Timestamp { get; private set; }
        public List<string> Entities { get; private set; }
        public List<string> Relationships { get; private set; }
        public Dictionary<string, object> Metadata { get; private set; }
        public float[]? VectorEmbedding { get; private set; }
        public double RelevanceScore { get; private set; }
        
        public BiomimeticMemoryEntry(MemoryType type, string content, DateTime? timestamp = null)
        {
            Id = Guid.NewGuid();
            Type = type;
            Content = content;
            Timestamp = timestamp ?? DateTime.UtcNow;
            Entities = new List<string>();
            Relationships = new List<string>();
            Metadata = new Dictionary<string, object>();
            RelevanceScore = 0.5;
        }
        
        public void AddEntity(string entity)
        {
            Entities.Add(entity);
        }
        
        public void AddRelationship(string relationship)
        {
            Relationships.Add(relationship);
        }
        
        public void SetVectorEmbedding(float[] embedding)
        {
            VectorEmbedding = embedding;
        }
        
        public void UpdateRelevanceScore(double score)
        {
            RelevanceScore = score;
        }
    }
    
    /// <summary>
    /// Memory bank (from hindsight)
    /// </summary>
    public class HindsightMemoryBank
    {
        public string BankId { get; private set; }
        public List<BiomimeticMemoryEntry> Entries { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime LastAccessedAt { get; private set; }
        
        public HindsightMemoryBank(string bankId)
        {
            BankId = bankId;
            Entries = new List<BiomimeticMemoryEntry>();
            CreatedAt = DateTime.UtcNow;
            LastAccessedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Retain: Store information (from hindsight)
        /// </summary>
        public void Retain(MemoryType type, string content, DateTime? timestamp = null)
        {
            var entry = new BiomimeticMemoryEntry(type, content, timestamp);
            Entries.Add(entry);
            LastAccessedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Recall: Retrieve memories (from hindsight)
        /// </summary>
        public List<BiomimeticMemoryEntry> Recall(string query, List<RetrievalStrategy>? strategies = null)
        {
            strategies = strategies ?? new List<RetrievalStrategy> 
            { 
                RetrievalStrategy.Semantic, 
                RetrievalStrategy.Keyword,
                RetrievalStrategy.Graph,
                RetrievalStrategy.Temporal
            };
            
            var results = new List<BiomimeticMemoryEntry>();
            
            foreach (var strategy in strategies)
            {
                results.AddRange(RecallWithStrategy(query, strategy));
            }
            
            // Merge and rank by relevance (reciprocal rank fusion simulation)
            return results
                .GroupBy(e => e.Id)
                .Select(g => g.First())
                .OrderByDescending(e => e.RelevanceScore)
                .ToList();
        }
        
        private List<BiomimeticMemoryEntry> RecallWithStrategy(string query, RetrievalStrategy strategy)
        {
            switch (strategy)
            {
                case RetrievalStrategy.Semantic:
                    return SemanticSearch(query);
                case RetrievalStrategy.Keyword:
                    return KeywordSearch(query);
                case RetrievalStrategy.Graph:
                    return GraphSearch(query);
                case RetrievalStrategy.Temporal:
                    return TemporalSearch(query);
                default:
                    return new List<BiomimeticMemoryEntry>();
            }
        }
        
        private List<BiomimeticMemoryEntry> SemanticSearch(string query)
        {
            // Simple semantic search simulation
            return Entries
                .Where(e => e.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(e => { e.UpdateRelevanceScore(0.8); return e; })
                .ToList();
        }
        
        private List<BiomimeticMemoryEntry> KeywordSearch(string query)
        {
            // BM25-like exact matching simulation
            var keywords = query.Split(' ');
            return Entries
                .Where(e => keywords.Any(k => e.Content.Contains(k, StringComparison.OrdinalIgnoreCase)))
                .Select(e => { e.UpdateRelevanceScore(0.9); return e; })
                .ToList();
        }
        
        private List<BiomimeticMemoryEntry> GraphSearch(string query)
        {
            // Entity/relationship graph search simulation
            return Entries
                .Where(e => e.Entities.Any(en => en.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                           e.Relationships.Any(r => r.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .Select(e => { e.UpdateRelevanceScore(0.7); return e; })
                .ToList();
        }
        
        private List<BiomimeticMemoryEntry> TemporalSearch(string query)
        {
            // Time-based filtering simulation
            return Entries
                .Where(e => e.Timestamp >= DateTime.UtcNow.AddDays(-7))
                .Select(e => { e.UpdateRelevanceScore(0.6); return e; })
                .ToList();
        }
        
        /// <summary>
        /// Reflect: Generate new insights from existing memories (from hindsight)
        /// </summary>
        public string Reflect(string query)
        {
            var relevantMemories = Recall(query);
            
            // Simple reflection simulation
            var insights = new List<string>();
            insights.Add($"Based on {relevantMemories.Count} relevant memories:");
            
            foreach (var memory in relevantMemories.Take(3))
            {
                insights.Add($"- {memory.Type}: {memory.Content.Substring(0, Math.Min(100, memory.Content.Length))}...");
            }
            
            return string.Join("\n", insights);
        }
    }
    
    /// <summary>
    /// Hindsight memory banks (from hindsight)
    /// </summary>
    public Dictionary<string, HindsightMemoryBank> HindsightBanks { get; private set; }
    
    /// <summary>
    /// Create or get hindsight memory bank (from hindsight)
    /// </summary>
    public HindsightMemoryBank GetOrCreateHindsightBank(string bankId)
    {
        if (!HindsightBanks.ContainsKey(bankId))
        {
            HindsightBanks[bankId] = new HindsightMemoryBank(bankId);
        }
        return HindsightBanks[bankId];
    }
    
    /// <summary>
    /// Retain information in hindsight bank (from hindsight)
    /// </summary>
    public void HindsightRetain(string bankId, MemoryType type, string content, DateTime? timestamp = null)
    {
        var bank = GetOrCreateHindsightBank(bankId);
        bank.Retain(type, content, timestamp);
    }
    
    /// <summary>
    /// Recall information from hindsight bank (from hindsight)
    /// </summary>
    public List<BiomimeticMemoryEntry> HindsightRecall(string bankId, string query, List<RetrievalStrategy>? strategies = null)
    {
        if (HindsightBanks.ContainsKey(bankId))
        {
            return HindsightBanks[bankId].Recall(query, strategies);
        }
        return new List<BiomimeticMemoryEntry>();
    }
    
    /// <summary>
    /// Reflect on memories in hindsight bank (from hindsight)
    /// </summary>
    public string HindsightReflect(string bankId, string query)
    {
        if (HindsightBanks.ContainsKey(bankId))
        {
            return HindsightBanks[bankId].Reflect(query);
        }
        return "No memories found for reflection.";
    }
}

/// <summary>
/// Statistics for a memory sector
/// </summary>
public class SectorStatistics
{
    public MemorySector Sector { get; set; }
    public int Capacity { get; set; }
    public int CurrentUsage { get; set; }
    public double UsagePercentage { get; set; }
}

/// <summary>
/// Statistics for the memory system
/// </summary>
public class MemorySystemStatistics
{
    public string SystemId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public int TotalFragments { get; set; }
    public int TotalLayeredFragments { get; set; }
    public int TotalSkills { get; set; }
    public Dictionary<MemorySector, SectorStatistics> SectorStatistics { get; set; } = new();
    public int TotalCrossReferences { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

/// <summary>
/// Memory Bank file (from aimemory)
/// </summary>
public class MemoryBankFile
{
    public string FileName { get; private set; }
    public string Content { get; private set; }
    public DateTime LastUpdated { get; private set; }
    public int Size { get; private set; }
    
    public MemoryBankFile(string fileName, string content)
    {
        FileName = fileName;
        Content = content;
        LastUpdated = DateTime.UtcNow;
        Size = content.Length;
    }
    
    public void UpdateContent(string newContent)
    {
        Content = newContent;
        LastUpdated = DateTime.UtcNow;
        Size = newContent.Length;
    }
}

/// <summary>
/// Memory Bank (from aimemory)
/// </summary>
public class MemoryBank
{
    public Guid Id { get; private set; }
    public string ProjectId { get; private set; }
    public Dictionary<string, MemoryBankFile> Files { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }
    
    public MemoryBank(string projectId)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        Files = new Dictionary<string, MemoryBankFile>();
        CreatedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
        
        // Initialize standard memory bank files (from aimemory)
        InitializeStandardFiles();
    }
    
    private void InitializeStandardFiles()
    {
        Files["projectbrief.md"] = new MemoryBankFile("projectbrief.md", "# Project Brief\n\nFoundation document that shapes all other files.");
        Files["productContext.md"] = new MemoryBankFile("productContext.md", "# Product Context\n\nWhy this project exists, problems it solves, user experience goals.");
        Files["activeContext.md"] = new MemoryBankFile("activeContext.md", "# Active Context\n\nCurrent work focus, recent changes, next steps.");
        Files["systemPatterns.md"] = new MemoryBankFile("systemPatterns.md", "# System Patterns\n\nSystem architecture, key technical decisions, design patterns.");
        Files["techContext.md"] = new MemoryBankFile("techContext.md", "# Tech Context\n\nTechnologies used, development setup, technical constraints.");
        Files["progress.md"] = new MemoryBankFile("progress.md", "# Progress\n\nWhat works, what's left to build, current status, known issues.");
    }
    
    public void UpdateFile(string fileName, string content)
    {
        if (Files.ContainsKey(fileName))
        {
            Files[fileName].UpdateContent(content);
        }
        else
        {
            Files[fileName] = new MemoryBankFile(fileName, content);
        }
        LastUpdatedAt = DateTime.UtcNow;
    }
    
    public string GetFileContent(string fileName)
    {
        return Files.ContainsKey(fileName) ? Files[fileName].Content : string.Empty;
    }
    
    public List<string> GetFileNames()
    {
        return Files.Keys.ToList();
    }
}

/// <summary>
/// Palace wing (from mempalace - structured index)
/// </summary>
public class PalaceWing
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public List<PalaceRoom> Rooms { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public PalaceWing(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Rooms = new List<PalaceRoom>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddRoom(PalaceRoom room)
    {
        Rooms.Add(room);
    }
}

/// <summary>
/// Palace room (from mempalace - structured index)
/// </summary>
public class PalaceRoom
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Topic { get; private set; }
    public List<PalaceDrawer> Drawers { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public PalaceRoom(string name, string topic)
    {
        Id = Guid.NewGuid();
        Name = name;
        Topic = topic;
        Drawers = new List<PalaceDrawer>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddDrawer(PalaceDrawer drawer)
    {
        Drawers.Add(drawer);
    }
}

/// <summary>
/// Palace drawer (from mempalace - structured index, stores original content)
/// </summary>
public class PalaceDrawer
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Content { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastAccessed { get; private set; }
    
    public PalaceDrawer(string name, string content)
    {
        Id = Guid.NewGuid();
        Name = name;
        Content = content;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void Access()
    {
        LastAccessed = DateTime.UtcNow;
    }
}

/// <summary>
/// Memory Palace (from mempalace)
/// </summary>
public class MemoryPalace
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public List<PalaceWing> Wings { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }
    
    public MemoryPalace(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        Wings = new List<PalaceWing>();
        CreatedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
    }
    
    public PalaceWing CreateWing(string name, string description)
    {
        var wing = new PalaceWing(name, description);
        Wings.Add(wing);
        LastUpdatedAt = DateTime.UtcNow;
        return wing;
    }
    
    public PalaceRoom CreateRoom(Guid wingId, string name, string topic)
    {
        var wing = Wings.FirstOrDefault(w => w.Id == wingId);
        if (wing != null)
        {
            var room = new PalaceRoom(name, topic);
            wing.AddRoom(room);
            LastUpdatedAt = DateTime.UtcNow;
            return room;
        }
        throw new ArgumentException($"Wing with ID {wingId} not found");
    }
    
    public PalaceDrawer CreateDrawer(Guid roomId, string name, string content)
    {
        foreach (var wing in Wings)
        {
            var room = wing.Rooms.FirstOrDefault(r => r.Id == roomId);
            if (room != null)
            {
                var drawer = new PalaceDrawer(name, content);
                room.AddDrawer(drawer);
                LastUpdatedAt = DateTime.UtcNow;
                return drawer;
            }
        }
        throw new ArgumentException($"Room with ID {roomId} not found");
    }
    
    /// <summary>
    /// Search across all drawers (from mempalace semantic search)
    /// </summary>
    public List<PalaceDrawer> Search(string query)
    {
        var results = new List<PalaceDrawer>();
        foreach (var wing in Wings)
        {
            foreach (var room in wing.Rooms)
            {
                foreach (var drawer in room.Drawers)
                {
                    if (drawer.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        drawer.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(drawer);
                    }
                }
            }
        }
        return results;
    }
}

/// <summary>
/// Knowledge graph entity (from mempalace)
/// </summary>
public class KnowledgeGraphEntity
{
    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string Name { get; private set; }
    public Dictionary<string, object> Properties { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    
    public KnowledgeGraphEntity(string type, string name)
    {
        Id = Guid.NewGuid();
        Type = type;
        Name = name;
        Properties = new Dictionary<string, object>();
        ValidFrom = DateTime.UtcNow;
    }
    
    public void SetProperty(string key, object value)
    {
        Properties[key] = value;
    }
    
    public void Invalidate()
    {
        ValidTo = DateTime.UtcNow;
    }
}

/// <summary>
/// Knowledge graph relationship (from mempalace)
/// </summary>
public class KnowledgeGraphRelationship
{
    public Guid Id { get; private set; }
    public Guid FromEntityId { get; private set; }
    public Guid ToEntityId { get; private set; }
    public string RelationshipType { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    
    public KnowledgeGraphRelationship(Guid fromEntityId, Guid toEntityId, string relationshipType)
    {
        Id = Guid.NewGuid();
        FromEntityId = fromEntityId;
        ToEntityId = toEntityId;
        RelationshipType = relationshipType;
        ValidFrom = DateTime.UtcNow;
    }
    
    public void Invalidate()
    {
        ValidTo = DateTime.UtcNow;
    }
}

/// <summary>
/// Knowledge graph (from mempalace)
/// </summary>
public class KnowledgeGraph
{
    public Guid Id { get; private set; }
    public Dictionary<Guid, KnowledgeGraphEntity> Entities { get; private set; }
    public Dictionary<Guid, KnowledgeGraphRelationship> Relationships { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public KnowledgeGraph()
    {
        Id = Guid.NewGuid();
        Entities = new Dictionary<Guid, KnowledgeGraphEntity>();
        Relationships = new Dictionary<Guid, KnowledgeGraphRelationship>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public KnowledgeGraphEntity AddEntity(string type, string name)
    {
        var entity = new KnowledgeGraphEntity(type, name);
        Entities[entity.Id] = entity;
        return entity;
    }
    
    public KnowledgeGraphRelationship AddRelationship(Guid fromEntityId, Guid toEntityId, string relationshipType)
    {
        var relationship = new KnowledgeGraphRelationship(fromEntityId, toEntityId, relationshipType);
        Relationships[relationship.Id] = relationship;
        return relationship;
    }
    
    public List<KnowledgeGraphEntity> GetRelatedEntities(Guid entityId, string relationshipType)
    {
        var related = new List<KnowledgeGraphEntity>();
        var relationships = Relationships.Values.Where(r => 
            r.FromEntityId == entityId && r.RelationshipType == relationshipType && r.ValidTo == null);
        
        foreach (var rel in relationships)
        {
            if (Entities.ContainsKey(rel.ToEntityId))
            {
                related.Add(Entities[rel.ToEntityId]);
            }
        }
        return related;
    }
    
    public void InvalidateEntity(Guid entityId)
    {
        if (Entities.ContainsKey(entityId))
        {
            Entities[entityId].Invalidate();
        }
    }
    
    public void InvalidateRelationship(Guid relationshipId)
    {
        if (Relationships.ContainsKey(relationshipId))
        {
            Relationships[relationshipId].Invalidate();
        }
    }
}
