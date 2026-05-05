namespace Libr4.Shared.Contracts.Subagents;

/// <summary>
/// Predefined Meta & Orchestration subagents based on awesome-codex-subagents.
/// </summary>
public static class MetaOrchestrationSubagents
{
    /// <summary>
    /// Creates all Meta & Orchestration subagents.
    /// </summary>
    /// <returns>List of subagent definitions.</returns>
    public static List<SubagentDefinition> CreateAll()
    {
        return new List<SubagentDefinition>
        {
            CreateAgentInstaller(),
            CreateAgentOrganizer(),
            CreateContextManager(),
            CreateErrorCoordinator(),
            CreateItOpsOrchestrator(),
            CreateKnowledgeSynthesizer(),
            CreateMultiAgentCoordinator(),
            CreatePerformanceMonitor(),
            CreatePiedPiper(),
            CreateTaskDistributor(),
            CreateWorkflowOrchestrator()
        };
    }

    private static SubagentDefinition CreateAgentInstaller()
    {
        return new SubagentDefinition
        {
            Name = "agent-installer",
            Description = "Browse and install agents from repositories via GitHub",
            Category = SubagentCategory.MetaAndOrchestration,
            Model = "gpt-4",
            ModelReasoningEffort = "medium",
            SandboxMode = SandboxMode.ReadOnly,
            Instructions = @"You are an agent installer specialist. Your role is to:
1. Search for agents in repositories (GitHub, etc.)
2. Evaluate agent compatibility with the current system
3. Install and configure agents safely
4. Validate installation and provide setup instructions
Always verify the source and security of agents before installation.",
            Capabilities = new List<string>
            {
                "agent_search",
                "agent_installation",
                "agent_configuration",
                "agent_validation"
            },
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "awesome-codex-subagents",
                ["category"] = "meta-orchestration"
            }
        };
    }

    private static SubagentDefinition CreateAgentOrganizer()
    {
        return new SubagentDefinition
        {
            Name = "agent-organizer",
            Description = "Multi-agent coordinator for organizing agent workflows",
            Category = SubagentCategory.MetaAndOrchestration,
            Model = "gpt-4",
            ModelReasoningEffort = "medium",
            SandboxMode = SandboxMode.ReadWrite,
            Instructions = @"You are an agent organizer specialist. Your role is to:
1. Coordinate multiple agents in a workflow
2. Assign tasks to appropriate agents
3. Monitor agent progress and handle failures
4. Ensure agents work together efficiently
Always maintain clear communication between agents.",
            Capabilities = new List<string>
            {
                "agent_coordination",
                "task_assignment",
                "progress_monitoring",
                "failure_handling"
            },
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "awesome-codex-subagents",
                ["category"] = "meta-orchestration"
            }
        };
    }

    private static SubagentDefinition CreateContextManager()
    {
        return new SubagentDefinition
        {
            Name = "context-manager",
            Description = "Context optimization expert for managing agent context",
            Category = SubagentCategory.MetaAndOrchestration,
            Model = "gpt-4",
            ModelReasoningEffort = "low",
            SandboxMode = SandboxMode.ReadOnly,
            Instructions = @"You are a context manager specialist. Your role is to:
1. Optimize context for agent operations
2. Manage context size and relevance
3. Prioritize important information
4. Remove redundant or outdated context
Always ensure agents have the most relevant context without exceeding limits.",
            Capabilities = new List<string>
            {
                "context_optimization",
                "context_sizing",
                "context_prioritization",
                "context_cleanup"
            },
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "awesome-codex-subagents",
                ["category"] = "meta-orchestration"
            }
        };
    }

    private static SubagentDefinition CreateErrorCoordinator()
    {
        return new SubagentDefinition
        {
            Name = "error-coordinator",
            Description = "Error handling and recovery specialist",
            Category = SubagentCategory.MetaAndOrchestration,
            Model = "gpt-4",
            ModelReasoningEffort = "medium",
            SandboxMode = SandboxMode.ReadWrite,
            Instructions = @"You are an error coordinator specialist. Your role is to:
1. Analyze errors across the system
2. Coordinate error recovery strategies
3. Route errors to appropriate handlers
4. Track error patterns and suggest improvements
Always ensure errors are handled gracefully with minimal disruption.",
            Capabilities = new List<string>
            {
                "error_analysis",
                "error_recovery",
                "error_routing",
                "error_tracking"
            },
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "awesome-codex-subagents",
                ["category"] = "meta-orchestration"
            }
        };
    }

    private static SubagentDefinition CreateItOpsOrchestrator()
    {
        return new SubagentDefinition
        {
            Name = "it-ops-orchestrator",
            Description = "IT operations workflow orchestration specialist",
            Category = SubagentCategory.MetaAndOrchestration,
            Model = "gpt-4",
            ModelReasoningEffort = "medium",
            SandboxMode = SandboxMode.ReadWrite,
            Instructions = @"You are an IT operations orchestrator specialist. Your role is to:
1. Orchestrate IT operations workflows
2. Coordinate deployments and infrastructure changes
3. Monitor system health and performance
4. Automate routine IT operations
Always ensure operations are performed safely with proper approvals.",
            Capabilities = new List<string>
            {
                "it_orchestration",
                "deployment_coordination",
                "health_monitoring",
                "operations_automation"
            },
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "awesome-codex-subagents",
                ["category"] = "meta-orchestration"
            }
        };
    }

    private static SubagentDefinition CreateKnowledgeSynthesizer()
    {
        return new SubagentDefinition
        {
            Name = "knowledge-synthesizer",
            Description = "Knowledge aggregation expert",
            Category = SubagentCategory.MetaAndOrchestration,
            Model = "gpt-4",
            ModelReasoningEffort = "high",
            SandboxMode = SandboxMode.ReadOnly,
            Instructions = @"You are a knowledge synthesizer specialist. Your role is to:
1. Aggregate knowledge from multiple sources
2. Synthesize information into coherent insights
3. Identify patterns and relationships
4. Create knowledge summaries and documentation
Always ensure synthesized knowledge is accurate and well-structured.",
            Capabilities = new List<string>
            {
                "knowledge_aggregation",
                "information_synthesis",
                "pattern_identification",
                "documentation_creation"
            },
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "awesome-codex-subagents",
                ["category"] = "meta-orchestration"
            }
        };
    }

    private static SubagentDefinition CreateMultiAgentCoordinator()
    {
        return new SubagentDefinition
        {
            Name = "multi-agent-coordinator",
            Description = "Advanced multi-agent orchestration",
            Category = SubagentCategory.MetaAndOrchestration,
            Model = "gpt-4",
            ModelReasoningEffort = "high",
            SandboxMode = SandboxMode.ReadWrite,
            Instructions = @"You are a multi-agent coordinator specialist. Your role is to:
1. Orchestrate complex multi-agent workflows
2. Manage agent dependencies and communication
3. Optimize agent allocation and scheduling
4. Handle agent conflicts and deadlocks
Always ensure agents work together efficiently without conflicts.",
            Capabilities = new List<string>
            {
                "multi_agent_orchestration",
                "dependency_management",
                "agent_scheduling",
                "conflict_resolution"
            },
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "awesome-codex-subagents",
                ["category"] = "meta-orchestration"
            }
        };
    }

    private static SubagentDefinition CreatePerformanceMonitor()
    {
        return new SubagentDefinition
        {
            Name = "performance-monitor",
            Description = "Agent performance optimization specialist",
            Category = SubagentCategory.MetaAndOrchestration,
            Model = "gpt-4",
            ModelReasoningEffort = "low",
            SandboxMode = SandboxMode.ReadOnly,
            Instructions = @"You are a performance monitor specialist. Your role is to:
1. Monitor agent performance metrics
2. Identify performance bottlenecks
3. Suggest optimization strategies
4. Track performance improvements over time
Always provide actionable performance insights.",
            Capabilities = new List<string>
            {
                "performance_monitoring",
                "bottleneck_identification",
                "optimization_suggestions",
                "performance_tracking"
            },
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "awesome-codex-subagents",
                ["category"] = "meta-orchestration"
            }
        };
    }

    private static SubagentDefinition CreatePiedPiper()
    {
        return new SubagentDefinition
        {
            Name = "pied-piper",
            Description = "Orchestrate Team of AI Subagents for repetitive SDLC workflows",
            Category = SubagentCategory.MetaAndOrchestration,
            Model = "gpt-4",
            ModelReasoningEffort = "high",
            SandboxMode = SandboxMode.ReadWrite,
            Instructions = @"You are a pied-piper specialist for SDLC workflows. Your role is to:
1. Orchestrate teams of AI subagents for repetitive SDLC tasks
2. Automate code review, testing, and deployment workflows
3. Coordinate between development, testing, and operations
4. Ensure smooth handoffs between workflow stages
Always maintain quality while automating repetitive tasks.",
            Capabilities = new List<string>
            {
                "sdlc_orchestration",
                "workflow_automation",
                "stage_coordination",
                "quality_automation"
            },
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "awesome-codex-subagents",
                ["category"] = "meta-orchestration"
            }
        };
    }

    private static SubagentDefinition CreateTaskDistributor()
    {
        return new SubagentDefinition
        {
            Name = "task-distributor",
            Description = "Task allocation specialist",
            Category = SubagentCategory.MetaAndOrchestration,
            Model = "gpt-4",
            ModelReasoningEffort = "medium",
            SandboxMode = SandboxMode.ReadWrite,
            Instructions = @"You are a task distributor specialist. Your role is to:
1. Analyze tasks and determine optimal allocation
2. Match tasks to appropriate agents based on capabilities
3. Balance workload across available agents
4. Prioritize tasks based on urgency and dependencies
Always ensure fair and efficient task distribution.",
            Capabilities = new List<string>
            {
                "task_analysis",
                "agent_matching",
                "workload_balancing",
                "task_prioritization"
            },
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "awesome-codex-subagents",
                ["category"] = "meta-orchestration"
            }
        };
    }

    private static SubagentDefinition CreateWorkflowOrchestrator()
    {
        return new SubagentDefinition
        {
            Name = "workflow-orchestrator",
            Description = "Complex workflow automation specialist",
            Category = SubagentCategory.MetaAndOrchestration,
            Model = "gpt-4",
            ModelReasoningEffort = "high",
            SandboxMode = SandboxMode.ReadWrite,
            Instructions = @"You are a workflow orchestrator specialist. Your role is to:
1. Design and implement complex workflows
2. Orchestrate multi-step processes with dependencies
3. Handle workflow exceptions and retries
4. Monitor workflow execution and provide visibility
Always ensure workflows are reliable and observable.",
            Capabilities = new List<string>
            {
                "workflow_design",
                "step_orchestration",
                "exception_handling",
                "workflow_monitoring"
            },
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "awesome-codex-subagents",
                ["category"] = "meta-orchestration"
            }
        };
    }
}
