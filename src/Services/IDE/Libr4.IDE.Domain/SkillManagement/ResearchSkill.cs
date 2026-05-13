namespace Libr4.IDE.Domain.SkillManagement;

/// <summary>
/// Skill category (from AI-Research-SKILLs and claude-skills)
/// </summary>
public enum SkillCategory
{
    Autoresearch,
    Ideation,
    ModelArchitecture,
    Tokenization,
    FineTuning,
    MechanisticInterpretability,
    DataProcessing,
    PostTraining,
    SafetyAlignment,
    DistributedTraining,
    Infrastructure,
    Optimization,
    Evaluation,
    InferenceServing,
    MLOps,
    Agents,
    RAG,
    PromptEngineering,
    Observability,
    Multimodal,
    EmergingTechniques,
    MLPaperWriting,
    AgentNativeResearchArtifact,
    Engineering,
    Product,
    Marketing,
    ProjectManagement,
    RegulatoryQM,
    CLevelAdvisory,
    BusinessGrowth,
    Finance
}

/// <summary>
/// Skill quality level (from AI-Research-SKILLs and claude-skills)
/// </summary>
public enum SkillQualityLevel
{
    Basic,
    Standard,
    GoldStandard,
    Powerful
}

/// <summary>
/// Skill tier (from claude-skills)
/// </summary>
public enum SkillTier
{
    Core,
    Advanced,
    Powerful
}

/// <summary>
/// Skill security audit result (from claude-skills)
/// </summary>
public enum SecurityAuditResult
{
    Pass,
    Warn,
    Fail
}

/// <summary>
/// Research skill with documentation and references (from AI-Research-SKILLs)
/// Enhanced with claude-skills structure and security auditing
/// </summary>
public class ResearchSkill
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Version { get; private set; }
    public SkillCategory Category { get; private set; }
    public SkillQualityLevel QualityLevel { get; private set; }
    public SkillTier Tier { get; private set; }
    
    /// <summary>
    /// Quick reference content (SKILL.md)
    /// </summary>
    public string QuickReference { get; private set; }
    
    /// <summary>
    /// When to use this skill
    /// </summary>
    public string UsageGuidance { get; private set; }
    
    /// <summary>
    /// Quick patterns and examples
    /// </summary>
    public List<string> Patterns { get; private set; }
    
    /// <summary>
    /// Reference documentation paths
    /// </summary>
    public List<string> ReferencePaths { get; private set; }
    
    /// <summary>
    /// Helper scripts (from claude-skills)
    /// </summary>
    public List<string> Scripts { get; private set; }
    
    /// <summary>
    /// Templates and assets
    /// </summary>
    public List<string> Assets { get; private set; }
    
    /// <summary>
    /// Security audit result (from claude-skills)
    /// </summary>
    public SecurityAuditResult SecurityAuditResult { get; private set; }
    
    /// <summary>
    /// Security audit timestamp
    /// </summary>
    public DateTime? SecurityAuditAt { get; private set; }
    
    /// <summary>
    /// Multi-tool compatibility (from claude-skills)
    /// </summary>
    public List<string> CompatibleTools { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public int UsageCount { get; private set; }
    
    public ResearchSkill(
        string name,
        string description,
        SkillCategory category,
        string quickReference,
        SkillQualityLevel qualityLevel = SkillQualityLevel.Standard,
        SkillTier tier = SkillTier.Core)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Category = category;
        QuickReference = quickReference;
        QualityLevel = qualityLevel;
        Tier = tier;
        Version = "1.0.0";
        UsageGuidance = string.Empty;
        Patterns = new List<string>();
        ReferencePaths = new List<string>();
        Scripts = new List<string>();
        Assets = new List<string>();
        SecurityAuditResult = SecurityAuditResult.Warn;
        CompatibleTools = new List<string>();
        CreatedAt = DateTime.UtcNow;
        UsageCount = 0;
    }
    
    public void AddPattern(string pattern)
    {
        Patterns.Add(pattern);
    }
    
    public void AddReferencePath(string path)
    {
        ReferencePaths.Add(path);
    }
    
    public void AddScript(string script)
    {
        Scripts.Add(script);
    }
    
    public void AddAsset(string asset)
    {
        Assets.Add(asset);
    }
    
    public void AddCompatibleTool(string tool)
    {
        CompatibleTools.Add(tool);
    }
    
    public void UpdateVersion(string newVersion)
    {
        Version = newVersion;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void RecordUsage()
    {
        UsageCount++;
    }
    
    /// <summary>
    /// Set security audit result (from claude-skills)
    /// </summary>
    public void SetSecurityAuditResult(SecurityAuditResult result)
    {
        SecurityAuditResult = result;
        SecurityAuditAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Orchestration pattern (from claude-skills)
/// </summary>
public enum OrchestrationPattern
{
    /// <summary>
    /// Switch personas across project phases
    /// </summary>
    SoloSprint,
    
    /// <summary>
    /// One persona + multiple stacked skills
    /// </summary>
    DomainDeepDive,
    
    /// <summary>
    /// Personas review each other's output
    /// </summary>
    MultiAgentHandoff,
    
    /// <summary>
    /// Sequential skills, no persona needed
    /// </summary>
    SkillChain
}

/// <summary>
/// Agent persona with curated skill loadout (from claude-skills)
/// </summary>
public class AgentPersona
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Domain { get; private set; }
    public List<string> SkillLoadout { get; private set; }
    public string CommunicationStyle { get; private set; }
    public List<string> BestFor { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public AgentPersona(string name, string description, string domain, string communicationStyle)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Domain = domain;
        CommunicationStyle = communicationStyle;
        SkillLoadout = new List<string>();
        BestFor = new List<string>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddSkill(string skillName)
    {
        SkillLoadout.Add(skillName);
    }
    
    public void AddBestFor(string useCase)
    {
        BestFor.Add(useCase);
    }
}

/// <summary>
/// Skill source (from skillkit)
/// </summary>
public class SkillSource
{
    public string Repository { get; private set; }
    public string Type { get; private set; } // github, gitlab, local, gist
    public DateTime LastSyncedAt { get; private set; }
    public int SkillCount { get; private set; }
    
    public SkillSource(string repository, string type)
    {
        Repository = repository;
        Type = type;
        LastSyncedAt = DateTime.UtcNow;
        SkillCount = 0;
    }
    
    public void UpdateSyncTime()
    {
        LastSyncedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Agent format (from skillkit)
/// </summary>
public class AgentFormat
{
    public string AgentName { get; private set; }
    public string Format { get; private set; } // SKILL.md, .mdc, Markdown, etc.
    public string Directory { get; private set; }
    
    public AgentFormat(string agentName, string format, string directory)
    {
        AgentName = agentName;
        Format = format;
        Directory = directory;
    }
}

/// <summary>
/// Stack-aware recommendation (from skillkit)
/// </summary>
public class SkillRecommendation
{
    public string SkillName { get; private set; }
    public double RelevanceScore { get; private set; }
    public string Reason { get; private set; }
    public List<string> DetectedStack { get; private set; }
    
    public SkillRecommendation(string skillName, double relevanceScore, string reason, List<string> detectedStack)
    {
        SkillName = skillName;
        RelevanceScore = relevanceScore;
        Reason = reason;
        DetectedStack = detectedStack;
    }
}

/// <summary>
/// Session memory entry (from skillkit)
/// </summary>
public class SessionMemoryEntry
{
    public Guid Id { get; private set; }
    public string Content { get; private set; }
    public DateTime CapturedAt { get; private set; }
    public List<string> Tags { get; private set; }
    public double RelevanceScore { get; private set; }
    
    public SessionMemoryEntry(string content, List<string>? tags = null)
    {
        Id = Guid.NewGuid();
        Content = content;
        CapturedAt = DateTime.UtcNow;
        Tags = tags ?? new List<string>();
        RelevanceScore = 0.5;
    }
    
    public void AddTag(string tag)
    {
        Tags.Add(tag);
    }
}

/// <summary>
/// Skill manifest for team collaboration (from skillkit)
/// </summary>
public class SkillManifest
{
    public string Version { get; private set; }
    public List<string> SkillSources { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    public SkillManifest()
    {
        Version = "1.0.0";
        SkillSources = new List<string>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddSkillSource(string source)
    {
        SkillSources.Add(source);
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Skill library with orchestration (from AI-Research-SKILLs, claude-skills, and skillkit)
/// Enhanced with package manager, format translation, and stack-aware recommendations
/// </summary>
public class SkillLibrary
{
    public List<ResearchSkill> Skills { get; private set; }
    public Dictionary<SkillCategory, List<ResearchSkill>> CategoryIndex { get; private set; }
    
    /// <summary>
    /// Available personas (from claude-skills)
    /// </summary>
    public List<AgentPersona> Personas { get; private set; }
    
    /// <summary>
    /// Skill sources (from skillkit)
    /// </summary>
    public List<SkillSource> Sources { get; private set; }
    
    /// <summary>
    /// Supported agent formats (from skillkit)
    /// </summary>
    public List<AgentFormat> AgentFormats { get; private set; }
    
    /// <summary>
    /// Session memory (from skillkit)
    /// </summary>
    public List<SessionMemoryEntry> SessionMemory { get; private set; }
    
    /// <summary>
    /// Skill manifest (from skillkit)
    /// </summary>
    public SkillManifest? Manifest { get; private set; }
    
    public SkillLibrary()
    {
        Skills = new List<ResearchSkill>();
        CategoryIndex = new Dictionary<SkillCategory, List<ResearchSkill>>();
        Personas = new List<AgentPersona>();
        Sources = new List<SkillSource>();
        AgentFormats = new List<AgentFormat>();
        SessionMemory = new List<SessionMemoryEntry>();
    }
    
    public void RegisterSkill(ResearchSkill skill)
    {
        Skills.Add(skill);
        
        if (!CategoryIndex.ContainsKey(skill.Category))
            CategoryIndex[skill.Category] = new List<ResearchSkill>();
        
        CategoryIndex[skill.Category].Add(skill);
    }
    
    /// <summary>
    /// Register persona (from claude-skills)
    /// </summary>
    public void RegisterPersona(AgentPersona persona)
    {
        Personas.Add(persona);
    }
    
    /// <summary>
    /// Add skill source (from skillkit)
    /// </summary>
    public void AddSkillSource(SkillSource source)
    {
        Sources.Add(source);
    }
    
    /// <summary>
    /// Add agent format (from skillkit)
    /// </summary>
    public void AddAgentFormat(AgentFormat format)
    {
        AgentFormats.Add(format);
    }
    
    /// <summary>
    /// Capture session memory (from skillkit)
    /// </summary>
    public void CaptureSessionMemory(string content, List<string>? tags = null)
    {
        var entry = new SessionMemoryEntry(content, tags);
        SessionMemory.Add(entry);
    }
    
    /// <summary>
    /// Search session memory (from skillkit)
    /// </summary>
    public List<SessionMemoryEntry> SearchSessionMemory(string query)
    {
        return SessionMemory
            .Where(m => m.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       m.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(m => m.RelevanceScore)
            .ToList();
    }
    
    /// <summary>
    /// Compress session memory (from skillkit)
    /// </summary>
    public void CompressSessionMemory()
    {
        // Simple compression: remove duplicates and low-relevance entries
        var uniqueEntries = SessionMemory
            .GroupBy(m => m.Content)
            .Select(g => g.First())
            .Where(m => m.RelevanceScore > 0.3)
            .ToList();
        
        SessionMemory = uniqueEntries;
    }
    
    /// <summary>
    /// Generate stack-aware recommendations (from skillkit)
    /// </summary>
    public List<SkillRecommendation> GenerateRecommendations(List<string> detectedStack)
    {
        var recommendations = new List<SkillRecommendation>();
        
        // Simple recommendation logic based on detected stack
        foreach (var stackItem in detectedStack)
        {
            var matchingSkills = Skills
                .Where(s => s.Description.Contains(stackItem, StringComparison.OrdinalIgnoreCase) ||
                           s.QuickReference.Contains(stackItem, StringComparison.OrdinalIgnoreCase))
                .Take(3);
            
            foreach (var skill in matchingSkills)
            {
                var score = CalculateRelevanceScore(skill, detectedStack);
                recommendations.Add(new SkillRecommendation(
                    skill.Name,
                    score,
                    $"Matches detected stack: {stackItem}",
                    detectedStack
                ));
            }
        }
        
        return recommendations.OrderByDescending(r => r.RelevanceScore).ToList();
    }
    
    private double CalculateRelevanceScore(ResearchSkill skill, List<string> detectedStack)
    {
        double score = 0.0;
        foreach (var stackItem in detectedStack)
        {
            if (skill.Description.Contains(stackItem, StringComparison.OrdinalIgnoreCase))
                score += 0.3;
            if (skill.QuickReference.Contains(stackItem, StringComparison.OrdinalIgnoreCase))
                score += 0.2;
            if (skill.Patterns.Any(p => p.Contains(stackItem, StringComparison.OrdinalIgnoreCase)))
                score += 0.1;
        }
        return Math.Min(1.0, score);
    }
    
    /// <summary>
    /// Translate skill to agent format (from skillkit)
    /// </summary>
    public string TranslateSkill(ResearchSkill skill, string targetAgent)
    {
        var targetFormat = AgentFormats.FirstOrDefault(f => f.AgentName == targetAgent);
        if (targetFormat == null)
            return skill.QuickReference;
        
        // Simple translation logic - in real implementation, this would convert formats
        return $"[{targetAgent} Format]\n{skill.QuickReference}";
    }
    
    /// <summary>
    /// Initialize skill manifest (from skillkit)
    /// </summary>
    public void InitializeManifest()
    {
        Manifest = new SkillManifest();
    }
    
    /// <summary>
    /// Add skill to manifest (from skillkit)
    /// </summary>
    public void AddToManifest(string source)
    {
        if (Manifest == null)
            Manifest = new SkillManifest();
        
        Manifest.AddSkillSource(source);
    }
    
    public List<ResearchSkill> GetSkillsByCategory(SkillCategory category)
    {
        return CategoryIndex.ContainsKey(category) 
            ? CategoryIndex[category] 
            : new List<ResearchSkill>();
    }
    
    public ResearchSkill? GetSkillByName(string name)
    {
        return Skills.FirstOrDefault(s => s.Name == name);
    }
    
    public List<ResearchSkill> GetGoldStandardSkills()
    {
        return Skills.Where(s => s.QualityLevel == SkillQualityLevel.GoldStandard).ToList();
    }
    
    /// <summary>
    /// Get powerful tier skills (from claude-skills)
    /// </summary>
    public List<ResearchSkill> GetPowerfulSkills()
    {
        return Skills.Where(s => s.Tier == SkillTier.Powerful).ToList();
    }
    
    /// <summary>
    /// Get persona by name (from claude-skills)
    /// </summary>
    public AgentPersona? GetPersonaByName(string name)
    {
        return Personas.FirstOrDefault(p => p.Name == name);
    }
    
    /// <summary>
    /// Get personas by domain (from claude-skills)
    /// </summary>
    public List<AgentPersona> GetPersonasByDomain(string domain)
    {
        return Personas.Where(p => p.Domain == domain).ToList();
    }
    
    /// <summary>
    /// Get skills for a persona (from claude-skills)
    /// </summary>
    public List<ResearchSkill> GetSkillsForPersona(AgentPersona persona)
    {
        return persona.SkillLoadout
            .Select(skillName => GetSkillByName(skillName))
            .Where(skill => skill != null)
            .Cast<ResearchSkill>()
            .ToList();
    }
    
    /// <summary>
    /// Get skills by security audit result (from claude-skills)
    /// </summary>
    public List<ResearchSkill> GetSkillsBySecurityResult(SecurityAuditResult result)
    {
        return Skills.Where(s => s.SecurityAuditResult == result).ToList();
    }
    
    public List<ResearchSkill> SearchSkills(string query)
    {
        return Skills
            .Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       s.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

/// <summary>
/// Agent-Native Research Artifact (ARA) - from AI-Research-SKILLs
/// </summary>
public class AgentNativeResearchArtifact
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// Cognitive layer: claims, concepts, heuristics
    /// </summary>
    public List<ResearchClaim> Claims { get; private set; }
    
    /// <summary>
    /// Physical layer: configs, code snippets
    /// </summary>
    public List<ResearchArtifact> Artifacts { get; private set; }
    
    /// <summary>
    /// Exploration graph: research DAG
    /// </summary>
    public ResearchExplorationGraph ExplorationGraph { get; private set; }
    
    /// <summary>
    /// Grounded evidence
    /// </summary>
    public List<Evidence> Evidence { get; private set; }
    
    public AgentNativeResearchArtifact(string title)
    {
        Id = Guid.NewGuid();
        Title = title;
        CreatedAt = DateTime.UtcNow;
        Claims = new List<ResearchClaim>();
        Artifacts = new List<ResearchArtifact>();
        ExplorationGraph = new ResearchExplorationGraph();
        Evidence = new List<Evidence>();
    }
    
    public void AddClaim(ResearchClaim claim)
    {
        Claims.Add(claim);
    }
    
    public void AddArtifact(ResearchArtifact artifact)
    {
        Artifacts.Add(artifact);
    }
    
    public void AddEvidence(Evidence evidence)
    {
        Evidence.Add(evidence);
    }
}

/// <summary>
/// Research claim with provenance tags
/// </summary>
public class ResearchClaim
{
    public Guid Id { get; private set; }
    public string Statement { get; private set; }
    public string ProvenanceTag { get; private set; } // user, ai-suggested, ai-executed, user-revised
    public DateTime CreatedAt { get; private set; }
    
    public ResearchClaim(string statement, string provenanceTag)
    {
        Id = Guid.NewGuid();
        Statement = statement;
        ProvenanceTag = provenanceTag;
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Research artifact (config, code snippet, etc.)
/// </summary>
public class ResearchArtifact
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } // config, code, data, etc.
    public string Content { get; private set; }
    public string Path { get; private set; }
    
    public ResearchArtifact(string type, string content, string path)
    {
        Id = Guid.NewGuid();
        Type = type;
        Content = content;
        Path = path;
    }
}

/// <summary>
/// Research exploration graph (DAG)
/// </summary>
public class ResearchExplorationGraph
{
    public List<ExplorationNode> Nodes { get; private set; }
    public List<ExplorationEdge> Edges { get; private set; }
    
    public ResearchExplorationGraph()
    {
        Nodes = new List<ExplorationNode>();
        Edges = new List<ExplorationEdge>();
    }
    
    public void AddNode(ExplorationNode node)
    {
        Nodes.Add(node);
    }
    
    public void AddEdge(ExplorationEdge edge)
    {
        Edges.Add(edge);
    }
}

/// <summary>
/// Exploration node in research graph
/// </summary>
public class ExplorationNode
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } // experiment, decision, pivot, etc.
    public string Description { get; private set; }
    public DateTime Timestamp { get; private set; }
    
    public ExplorationNode(string type, string description)
    {
        Id = Guid.NewGuid();
        Type = type;
        Description = description;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Exploration edge in research graph
/// </summary>
public class ExplorationEdge
{
    public Guid FromNodeId { get; private set; }
    public Guid ToNodeId { get; private set; }
    public string Relationship { get; private set; }
    
    public ExplorationEdge(Guid fromNodeId, Guid toNodeId, string relationship)
    {
        FromNodeId = fromNodeId;
        ToNodeId = toNodeId;
        Relationship = relationship;
    }
}

/// <summary>
/// Grounded evidence
/// </summary>
public class Evidence
{
    public Guid Id { get; private set; }
    public string Source { get; private set; }
    public string Content { get; private set; }
    public double RelevanceScore { get; private set; }
    
    public Evidence(string source, string content, double relevanceScore)
    {
        Id = Guid.NewGuid();
        Source = source;
        Content = content;
        RelevanceScore = relevanceScore;
    }
}
