using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Libr4.IDE.Application.Obscura;

namespace Libr4.IDE.Application.MultiAgentOrchestration;

/// <summary>
/// Agent Debates - Self-Reflecting Swarm with Weighted Consensus
/// For complex tasks (DB design, architecture), agents debate before action
/// </summary>
public interface IAgentDebateService
{
    /// <summary>
    /// Conduct a debate between agents with different roles
    /// </summary>
    Task<DebateResult> ConductDebateAsync(
        string topic,
        string context,
        AgentRole[] participants,
        DebateOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Multi-round debate until consensus or max rounds
    /// </summary>
    Task<ConsensusResult> ReachConsensusAsync(
        string task,
        string initialProposal,
        AgentRole[] reviewers,
        ConsensusOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Spawn subagents for parallel work
    /// </summary>
    Task<SpawnedAgentsResult> SpawnSpecializedAgentsAsync(
        string parentAgentId,
        string task,
        Specialization[] neededRoles,
        CancellationToken ct = default);

    /// <summary>
    /// Automatic tool discovery - agent finds missing capabilities
    /// </summary>
    Task<ToolDiscoveryResult> DiscoverAndCreateToolAsync(
        string agentId,
        string missingCapability,
        CancellationToken ct = default);
}

/// <summary>
/// Implementation of Agent Debates with Weighted Consensus
/// </summary>
public class AgentDebateService : IAgentDebateService
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IContextCompressionService _compression;
    private readonly IObscuraBrowserTool _browserTool;
    private readonly ILogger<AgentDebateService> _logger;
    private readonly Dictionary<string, AgentLearningProfile> _learningProfiles = new();

    public AgentDebateService(
        IAgentOrchestrator orchestrator,
        IContextCompressionService compression,
        IObscuraBrowserTool browserTool,
        ILogger<AgentDebateService> logger)
    {
        _orchestrator = orchestrator;
        _compression = compression;
        _browserTool = browserTool;
        _logger = logger;
    }

    public async Task<DebateResult> ConductDebateAsync(
        string topic,
        string context,
        AgentRole[] participants,
        DebateOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new DebateOptions();
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation(
            "Starting debate on '{Topic}' with {Count} participants",
            topic, participants.Length);

        var rounds = new List<DebateRound>();
        var currentContext = context;

        for (int round = 0; round < options.MaxRounds; round++)
        {
            _logger.LogDebug("Debate round {Round}/{Max}", round + 1, options.MaxRounds);
            
            var roundArguments = new List<AgentArgument>();
            
            // Each participant makes their case
            foreach (var participant in participants)
            {
                var weight = GetAgentWeight(participant.AgentId);
                
                var argument = await GenerateArgumentAsync(
                    participant,
                    topic,
                    currentContext,
                    roundArguments,  // See previous arguments in this round
                    weight,
                    ct);
                
                roundArguments.Add(argument);
                
                _logger.LogDebug(
                    "Agent {Agent} ({Role}): {Stance} (confidence: {Confidence})",
                    participant.AgentId,
                    participant.Role,
                    argument.Stance,
                    argument.Confidence);
            }

            rounds.Add(new DebateRound
            {
                RoundNumber = round + 1,
                Arguments = roundArguments
            });

            // Check for early consensus
            if (CheckConsensus(roundArguments, options.ConsensusThreshold))
            {
                _logger.LogInformation(
                    "Consensus reached after {Rounds} rounds",
                    round + 1);
                break;
            }

            // Compress context for next round
            currentContext = _compression.CompressAgentContext(
                FormatContextForNextRound(currentContext, roundArguments),
                targetTokens: 2000);
        }

        stopwatch.Stop();

        // Calculate final decision with weighted voting
        var allArguments = rounds.SelectMany(r => r.Arguments).ToList();
        var decision = CalculateWeightedDecision(allArguments);

        return new DebateResult
        {
            Topic = topic,
            Rounds = rounds,
            FinalDecision = decision,
            TotalRounds = rounds.Count,
            DurationMs = (int)stopwatch.ElapsedMilliseconds,
            Participants = participants.Select(p => p.Role).ToList(),
            ConsensusReached = rounds.Count < options.MaxRounds
        };
    }

    public async Task<ConsensusResult> ReachConsensusAsync(
        string task,
        string initialProposal,
        AgentRole[] reviewers,
        ConsensusOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new ConsensusOptions();
        
        _logger.LogInformation(
            "Reaching consensus for task: {Task} with {Reviewers} reviewers",
            task[..Math.Min(50, task.Length)],
            reviewers.Length);

        var proposal = initialProposal;
        var iterations = new List<ConsensusIteration>();

        for (int i = 0; i < options.MaxIterations; i++)
        {
            var reviews = new List<AgentReview>();
            
            // Each reviewer evaluates the proposal
            foreach (var reviewer in reviewers)
            {
                var review = await ReviewProposalAsync(
                    reviewer,
                    task,
                    proposal,
                    ct);
                
                reviews.Add(review);
                
                // Update learning profile
                UpdateLearningProfile(reviewer.AgentId, review.Score);
            }

            // Calculate weighted consensus score
            var consensusScore = CalculateConsensusScore(reviews);
            
            iterations.Add(new ConsensusIteration
            {
                Iteration = i + 1,
                Proposal = proposal,
                Reviews = reviews,
                ConsensusScore = consensusScore,
                Improvements = reviews.SelectMany(r => r.SuggestedImprovements).ToList()
            });

            // Check if consensus reached
            if (consensusScore >= options.ConsensusThreshold)
            {
                _logger.LogInformation(
                    "Consensus reached at iteration {Iteration} with score {Score:F2}",
                    i + 1, consensusScore);

                return new ConsensusResult
                {
                    Task = task,
                    FinalProposal = proposal,
                    ConsensusScore = consensusScore,
                    Iterations = iterations,
                    TotalIterations = i + 1,
                    Success = true,
                    KeyConcerns = reviews.SelectMany(r => r.Concerns).Distinct().ToList()
                };
            }

            // Generate improved proposal based on feedback
            proposal = await ImproveProposalAsync(
                proposal,
                reviews,
                ct);
        }

        // Max iterations reached without consensus
        _logger.LogWarning(
            "Failed to reach consensus after {Iterations} iterations. Best score: {Score:F2}",
            options.MaxIterations,
            iterations.Last().ConsensusScore);

        return new ConsensusResult
        {
            Task = task,
            FinalProposal = proposal,
            ConsensusScore = iterations.Last().ConsensusScore,
            Iterations = iterations,
            TotalIterations = options.MaxIterations,
            Success = false,
            KeyConcerns = iterations.Last().Reviews.SelectMany(r => r.Concerns).Distinct().ToList()
        };
    }

    public async Task<SpawnedAgentsResult> SpawnSpecializedAgentsAsync(
        string parentAgentId,
        string task,
        Specialization[] neededRoles,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Parent agent {Parent} spawning {Count} specialized agents",
            parentAgentId,
            neededRoles.Length);

        var spawnedAgents = new List<SpawnedAgent>();
        var stopwatch = Stopwatch.StartNew();

        // Create subagents in parallel
        var spawnTasks = neededRoles.Select(async role =>
        {
            var agentId = $"{parentAgentId}-{role.Name}-{Guid.NewGuid():N}";
            
            // Split context appropriately
            var subContext = await SplitContextForAgentAsync(
                parentAgentId,
                role,
                task,
                ct);

            // Spawn the agent
            var spawned = await _orchestrator.SpawnAgentAsync(new SpawnAgentRequest
            {
                ParentAgentId = parentAgentId,
                AgentId = agentId,
                Role = role.Name,
                Specialization = role.Description,
                Context = subContext,
                Tools = role.RequiredTools
            }, ct);

            return new SpawnedAgent
            {
                AgentId = agentId,
                ParentAgentId = parentAgentId,
                Role = role.Name,
                Specialization = role.Description,
                AssignedContext = subContext,
                SpawnedAt = DateTime.UtcNow,
                Status = spawned.Status
            };
        });

        spawnedAgents = (await Task.WhenAll(spawnTasks)).ToList();
        stopwatch.Stop();

        _logger.LogInformation(
            "Spawned {Count} agents in {DurationMs}ms",
            spawnedAgents.Count,
            stopwatch.ElapsedMilliseconds);

        return new SpawnedAgentsResult
        {
            ParentAgentId = parentAgentId,
            Task = task,
            SpawnedAgents = spawnedAgents,
            DurationMs = (int)stopwatch.ElapsedMilliseconds
        };
    }

    public async Task<ToolDiscoveryResult> DiscoverAndCreateToolAsync(
        string agentId,
        string missingCapability,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Agent {Agent} discovering tool for: {Capability}",
            agentId,
            missingCapability);

        // Step 1: Search for existing documentation
        var searchResult = await _browserTool.ResearchAsync(
            query: missingCapability,
            sources: new[]
            {
                "https://docs.microsoft.com",
                "https://stackoverflow.com",
                "https://github.com"
            },
            options: new WebResearchOptions { MaxSources = 3, StealthMode = true },
            ct);

        // Step 2: Generate tool implementation based on findings
        var toolCode = await GenerateToolImplementationAsync(
            missingCapability,
            searchResult,
            ct);

        // Step 3: Validate and compile
        var validationResult = await ValidateToolAsync(toolCode, ct);

        // Step 4: Register with orchestrator
        if (validationResult.IsValid)
        {
            await _orchestrator.RegisterToolAsync(new ToolRegistration
            {
                ToolId = $"discovered-{Guid.NewGuid():N}",
                Name = missingCapability,
                Code = toolCode,
                DiscoveredBy = agentId,
                ValidationStatus = "Valid"
            }, ct);
        }

        return new ToolDiscoveryResult
        {
            AgentId = agentId,
            MissingCapability = missingCapability,
            ResearchSources = searchResult.Sources.Select(s => s.Url).ToList(),
            GeneratedToolCode = toolCode,
            ValidationResult = validationResult,
            SuccessfullyCreated = validationResult.IsValid,
            DiscoveryTimeMs = (int)DateTime.UtcNow.Millisecond
        };
    }

    // Private helper methods
    private double GetAgentWeight(string agentId)
    {
        if (_learningProfiles.TryGetValue(agentId, out var profile))
        {
            // Weight based on success rate and expertise
            return profile.SuccessRate * profile.ExpertiseLevel;
        }
        return 1.0; // Default weight
    }

    private async Task<AgentArgument> GenerateArgumentAsync(
        AgentRole participant,
        string topic,
        string context,
        List<AgentArgument> previousArguments,
        double weight,
        CancellationToken ct)
    {
        // Simulate agent reasoning (in real implementation, call LLM)
        var stance = DetermineStance(participant.Role, topic);
        var confidence = CalculateConfidence(participant.Role, weight);
        
        var argument = new AgentArgument
        {
            AgentId = participant.AgentId,
            Role = participant.Role,
            Stance = stance,
            Reasoning = $"As {participant.Role}, I argue {stance} because...",
            Evidence = new List<string> { "Evidence based on role expertise" },
            Confidence = confidence,
            Weight = weight,
            CounterArguments = previousArguments
                .Where(a => a.Stance != stance)
                .Select(a => $"Counter to {a.Role}: {a.Reasoning}")
                .ToList()
        };

        await Task.Delay(100, ct); // Simulate processing
        return argument;
    }

    private string DetermineStance(string role, string topic)
    {
        // Role-based stance determination
        return role switch
        {
            "PerformanceOptimizer" => topic.Contains("speed") ? "Strong Support" : "Neutral",
            "CleanArchitecture" => topic.Contains("clean") || topic.Contains("pattern") ? "Strong Support" : "Caution",
            "SecurityExpert" => topic.Contains("security") ? "Strong Support" : "Caution",
            "CostAnalyst" => topic.Contains("cost") ? "Strong Support" : "Neutral",
            _ => "Neutral"
        };
    }

    private double CalculateConfidence(string role, double weight)
    {
        // Base confidence from role expertise + learned weight
        var baseConfidence = role switch
        {
            "PerformanceOptimizer" => 0.85,
            "CleanArchitecture" => 0.90,
            "SecurityExpert" => 0.95,
            "CostAnalyst" => 0.80,
            _ => 0.75
        };

        return Math.Min(0.99, baseConfidence * weight);
    }

    private bool CheckConsensus(List<AgentArgument> arguments, double threshold)
    {
        var totalWeight = arguments.Sum(a => a.Weight);
        var supportingWeight = arguments
            .Where(a => a.Stance.Contains("Support"))
            .Sum(a => a.Weight);

        return supportingWeight / totalWeight >= threshold;
    }

    private DebateDecision CalculateWeightedDecision(List<AgentArgument> allArguments)
    {
        var stanceGroups = allArguments
            .GroupBy(a => a.Stance)
            .Select(g => new
            {
                Stance = g.Key,
                TotalWeight = g.Sum(a => a.Weight),
                AvgConfidence = g.Average(a => a.Confidence),
                Arguments = g.ToList()
            })
            .OrderByDescending(g => g.TotalWeight)
            .First();

        return new DebateDecision
        {
            Stance = stanceGroups.Stance,
            Confidence = stanceGroups.AvgConfidence,
            SupportingWeight = stanceGroups.TotalWeight,
            TotalWeight = allArguments.Sum(a => a.Weight),
            KeyReasoning = stanceGroups.Arguments
                .OrderByDescending(a => a.Confidence)
                .First()
                .Reasoning,
            CounterPoints = allArguments
                .Where(a => a.Stance != stanceGroups.Stance)
                .Select(a => $"{a.Role}: {a.Reasoning}")
                .ToList()
        };
    }

    private async Task<AgentReview> ReviewProposalAsync(
        AgentRole reviewer,
        string task,
        string proposal,
        CancellationToken ct)
    {
        // Simulate review based on role
        var score = reviewer.Role switch
        {
            "PerformanceOptimizer" => EvaluatePerformance(proposal),
            "CleanArchitecture" => EvaluateArchitecture(proposal),
            "SecurityExpert" => EvaluateSecurity(proposal),
            _ => 0.75
        };

        var review = new AgentReview
        {
            AgentId = reviewer.AgentId,
            Role = reviewer.Role,
            Score = score,
            Concerns = GenerateConcerns(reviewer.Role, proposal),
            SuggestedImprovements = GenerateImprovements(reviewer.Role, proposal),
            ReviewWeight = GetAgentWeight(reviewer.AgentId)
        };

        await Task.Delay(50, ct);
        return review;
    }

    private double EvaluatePerformance(string proposal) => 
        proposal.Contains("async") || proposal.Contains("cache") ? 0.90 : 0.70;

    private double EvaluateArchitecture(string proposal) => 
        proposal.Contains("interface") || proposal.Contains("pattern") ? 0.92 : 0.75;

    private double EvaluateSecurity(string proposal) => 
        proposal.Contains("auth") || proposal.Contains("validate") ? 0.95 : 0.65;

    private List<string> GenerateConcerns(string role, string proposal)
    {
        var concerns = new List<string>();
        
        if (role == "SecurityExpert" && !proposal.Contains("auth"))
            concerns.Add("Missing authentication check");
        
        if (role == "PerformanceOptimizer" && proposal.Contains("sync"))
            concerns.Add("Synchronous operation may block");
        
        if (role == "CleanArchitecture" && proposal.Contains("new"))
            concerns.Add("Direct instantiation violates DI");

        return concerns;
    }

    private List<string> GenerateImprovements(string role, string proposal)
    {
        var improvements = new List<string>();
        
        if (role == "PerformanceOptimizer")
            improvements.Add("Consider using async/await for I/O operations");
        
        if (role == "CleanArchitecture")
            improvements.Add("Introduce interface abstraction");

        return improvements;
    }

    private double CalculateConsensusScore(List<AgentReview> reviews)
    {
        var totalWeight = reviews.Sum(r => r.ReviewWeight);
        var weightedScore = reviews.Sum(r => r.Score * r.ReviewWeight) / totalWeight;
        
        // Penalty for concerns
        var concernPenalty = reviews.Sum(r => r.Concerns.Count) * 0.02;
        
        return Math.Max(0, weightedScore - concernPenalty);
    }

    private async Task<string> ImproveProposalAsync(
        string proposal,
        List<AgentReview> reviews,
        CancellationToken ct)
    {
        // Combine all improvements
        var allImprovements = reviews.SelectMany(r => r.SuggestedImprovements).Distinct();
        
        // In real implementation, would call LLM to rewrite proposal
        var improved = proposal + "\n\n// Improvements based on feedback:\n" +
            string.Join("\n", allImprovements.Select(i => $"// - {i}"));

        await Task.Delay(100, ct);
        return improved;
    }

    private async Task<string> SplitContextForAgentAsync(
        string parentAgentId,
        Specialization role,
        string fullTask,
        CancellationToken ct)
    {
        // Split task context based on specialization
        var relevantPart = role.Name switch
        {
            "UIComponentDesigner" => ExtractUiContext(fullTask),
            "BusinessLogicDeveloper" => ExtractLogicContext(fullTask),
            "APIDesigner" => ExtractApiContext(fullTask),
            _ => fullTask
        };

        await Task.Delay(50, ct);
        return relevantPart;
    }

    private string ExtractUiContext(string task) => 
        $"UI Context: {task}\nFocus on: components, styling, user interactions";

    private string ExtractLogicContext(string task) => 
        $"Logic Context: {task}\nFocus on: business rules, validation, workflows";

    private string ExtractApiContext(string task) => 
        $"API Context: {task}\nFocus on: endpoints, data contracts, authentication";

    private async Task<string> GenerateToolImplementationAsync(
        string capability,
        WebResearchResult research,
        CancellationToken ct)
    {
        // Build prompt from research findings
        var researchSummary = string.Join("\n", research.Sources.Select(s => 
            $"- {s.Url}: {s.Summary}"));
        
        var prompt = $@"Generate a C# tool class for the capability: {capability}

Based on this research:
{researchSummary}

Generate a complete, compilable C# class that:
1. Has proper using statements
2. Implements the capability with realistic method signatures
3. Includes XML documentation comments
4. Follows clean architecture principles
5. Handles errors appropriately

Return only the C# code, no markdown formatting.";

        // In production, this would call an LLM service
        // For now, generate a structured template based on capability type
        var code = GenerateToolTemplate(capability, research);

        await Task.Delay(100, ct); // Simulate processing
        return code;
    }

    private string GenerateToolTemplate(string capability, WebResearchResult research)
    {
        var className = capability.Replace(" ", "").Replace("-", "") + "Tool";
        var researchUrls = string.Join("\n    /// ", research.Sources.Select(s => $"Based on: {s.Url}"));
        
        return
            "using System;\n" +
            "using System.Net.Http;\n" +
            "using System.Threading.Tasks;\n" +
            "using Microsoft.Extensions.Logging;\n\n" +
            "namespace Libr4.IDE.Tools.Discovered;\n\n" +
            $"/// <summary>\n/// Auto-discovered tool for: {capability}\n/// </summary>\n" +
            $"/// <remarks>\n/// Generated by AgentDebateService\n/// {researchUrls}\n/// </remarks>\n" +
            $"public class {className}\n" +
            "{\n" +
            "    private readonly HttpClient _httpClient;\n" +
            $"    private readonly ILogger<{className}> _logger;\n\n" +
            $"    public {className}(HttpClient httpClient, ILogger<{className}> logger)\n" +
            "    {\n" +
            "        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));\n" +
            "        _logger = logger ?? throw new ArgumentNullException(nameof(logger));\n" +
            "    }\n\n" +
            $"    public virtual async Task<object> ExecuteAsync(string input)\n" +
            "    {\n" +
            "        _logger.LogInformation(\"Executing capability with input length {Len}\", input.Length);\n" +
            "        await Task.CompletedTask;\n" +
            "        return new { Input = input, Processed = true, Timestamp = DateTime.UtcNow };\n" +
            "    }\n" +
            "}\n";
    }

    private async Task<ToolValidationResult> ValidateToolAsync(string code, CancellationToken ct)
    {
        // Basic validation
        var isValid = code.Contains("class") && code.Contains("public");
        // ... 
        
        var errors = new List<string>();
        if (!code.Contains("using"))
            errors.Add("Missing using statements");
        
        await Task.Delay(100, ct);

        return new ToolValidationResult
        {
            IsValid = isValid && !errors.Any(),
            Errors = errors,
            Warnings = new List<string> { "Generated code needs review" }
        };
    }

    private void UpdateLearningProfile(string agentId, double reviewScore)
    {
        if (!_learningProfiles.TryGetValue(agentId, out var profile))
        {
            profile = new AgentLearningProfile
            {
                AgentId = agentId,
                SuccessRate = 0.5,
                ExpertiseLevel = 1.0,
                ReviewCount = 0
            };
            _learningProfiles[agentId] = profile;
        }

        profile.ReviewCount++;
        // Exponential moving average for success rate
        profile.SuccessRate = profile.SuccessRate * 0.9 + reviewScore * 0.1;
    }

    private string FormatContextForNextRound(string currentContext, List<AgentArgument> roundArguments)
    {
        var summary = string.Join("\n", roundArguments.Select(a => 
            $"[{a.Role}] {a.Stance}: {a.Reasoning.Substring(0, Math.Min(100, a.Reasoning.Length))}"));
        
        return $"{currentContext}\n\n=== Round Summary ===\n{summary}";
    }
}

// Supporting types
public class AgentRole
{
    public string AgentId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
}

public class DebateOptions
{
    public int MaxRounds { get; set; } = 3;
    public double ConsensusThreshold { get; set; } = 0.67; // 2/3 majority
}

public class ConsensusOptions
{
    public int MaxIterations { get; set; } = 5;
    public double ConsensusThreshold { get; set; } = 0.80;
    public bool RequireUnanimity { get; set; } = false;
}

public class Specialization
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] RequiredTools { get; set; } = Array.Empty<string>();
}

public class DebateResult
{
    public string Topic { get; set; } = string.Empty;
    public List<DebateRound> Rounds { get; set; } = new();
    public DebateDecision FinalDecision { get; set; } = new();
    public int TotalRounds { get; set; }
    public int DurationMs { get; set; }
    public List<string> Participants { get; set; } = new();
    public bool ConsensusReached { get; set; }
}

public class DebateRound
{
    public int RoundNumber { get; set; }
    public List<AgentArgument> Arguments { get; set; } = new();
}

public class AgentArgument
{
    public string AgentId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Stance { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public List<string> Evidence { get; set; } = new();
    public double Confidence { get; set; }
    public double Weight { get; set; }
    public List<string> CounterArguments { get; set; } = new();
}

public class DebateDecision
{
    public string Stance { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public double SupportingWeight { get; set; }
    public double TotalWeight { get; set; }
    public string KeyReasoning { get; set; } = string.Empty;
    public List<string> CounterPoints { get; set; } = new();
}

public class ConsensusResult
{
    public string Task { get; set; } = string.Empty;
    public string FinalProposal { get; set; } = string.Empty;
    public double ConsensusScore { get; set; }
    public List<ConsensusIteration> Iterations { get; set; } = new();
    public int TotalIterations { get; set; }
    public bool Success { get; set; }
    public List<string> KeyConcerns { get; set; } = new();
}

public class ConsensusIteration
{
    public int Iteration { get; set; }
    public string Proposal { get; set; } = string.Empty;
    public List<AgentReview> Reviews { get; set; } = new();
    public double ConsensusScore { get; set; }
    public List<string> Improvements { get; set; } = new();
}

public class AgentReview
{
    public string AgentId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public double Score { get; set; }
    public List<string> Concerns { get; set; } = new();
    public List<string> SuggestedImprovements { get; set; } = new();
    public double ReviewWeight { get; set; }
}

public class SpawnedAgentsResult
{
    public string ParentAgentId { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public List<SpawnedAgent> SpawnedAgents { get; set; } = new();
    public int DurationMs { get; set; }
}

public class SpawnedAgent
{
    public string AgentId { get; set; } = string.Empty;
    public string ParentAgentId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string AssignedContext { get; set; } = string.Empty;
    public DateTime SpawnedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ToolDiscoveryResult
{
    public string AgentId { get; set; } = string.Empty;
    public string MissingCapability { get; set; } = string.Empty;
    public List<string> ResearchSources { get; set; } = new();
    public string GeneratedToolCode { get; set; } = string.Empty;
    public ToolValidationResult ValidationResult { get; set; } = new();
    public bool SuccessfullyCreated { get; set; }
    public int DiscoveryTimeMs { get; set; }
}

public class ToolValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class AgentLearningProfile
{
    public string AgentId { get; set; } = string.Empty;
    public double SuccessRate { get; set; }
    public double ExpertiseLevel { get; set; }
    public int ReviewCount { get; set; }
}

// Interfaces that need to be defined elsewhere
public interface IAgentOrchestrator
{
    Task<SpawnResult> SpawnAgentAsync(SpawnAgentRequest request, CancellationToken ct);
    Task RegisterToolAsync(ToolRegistration tool, CancellationToken ct);
}

public class SpawnAgentRequest
{
    public string ParentAgentId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string[] Tools { get; set; } = Array.Empty<string>();
}

public class SpawnResult
{
    public string Status { get; set; } = string.Empty;
}

public class ToolRegistration
{
    public string ToolId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DiscoveredBy { get; set; } = string.Empty;
    public string ValidationStatus { get; set; } = string.Empty;
}

public class ToolExecutionException : Exception
{
    public ToolExecutionException(string message, Exception? inner = null) : base(message, inner) { }
}
