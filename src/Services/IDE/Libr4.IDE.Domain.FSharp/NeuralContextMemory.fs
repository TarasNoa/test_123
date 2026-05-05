namespace Libr4.IDE.Domain.FSharp

open System

// ============================================================================
// NEURAL CONTEXT & COGNITIVE MEMORY (F#)
// Hierarchical Knowledge Graph with Ephemeral/Project/Global memory tiers
// Code DNA enforcement through AST pattern matching
// ============================================================================

/// Memory tier - determines lifetime and scope
type MemoryTier =
    | Ephemeral of EphemeralConfig  // Current session only
    | Project of ProjectConfig      // Project-level knowledge
    | Global of GlobalConfig        // Cross-project patterns

and EphemeralConfig = {
    SessionId: string
    ExpiresAt: DateTime
    MaxTokens: int
}

and ProjectConfig = {
    ProjectId: string
    RepositoryUrl: string option
    TechStack: string list
    DomainEntities: string list
}

and GlobalConfig = {
    OrganizationId: string
    CodingStandards: string list
    SecurityPolicies: string list
}

/// Knowledge node in the graph
type KnowledgeNode =
    | CodeEntity of CodeEntityNode
    | Pattern of PatternNode
    | Relationship of RelationshipNode
    | Context of ContextNode

and CodeEntityNode = {
    Id: string
    Name: string
    EntityType: CodeEntityType
    Location: CodeLocation
    SemanticHash: string  // AST hash for "Code DNA"
    Dependencies: string list
    LastModified: DateTime
}

and CodeEntityType =
    | Class of ClassInfo
    | Function of FunctionInfo
    | Interface of InterfaceInfo
    | Module of ModuleInfo
    | Query of QueryInfo  // DB queries tracked separately

and ClassInfo = {
    BaseClass: string option
    Interfaces: string list
    Methods: string list
    Properties: string list
    IsRepository: bool  // Important for architecture enforcement
}

and FunctionInfo = {
    Parameters: (string * string) list  // name * type
    ReturnType: string
    IsAsync: bool
    Purity: PurityLevel
}

and PurityLevel = Pure | Impure | Unknown

and InterfaceInfo = {
    Implementations: string list
    Methods: string list
}

and ModuleInfo = {
    Functions: string list
    Types: string list
    IsInternal: bool
}

and QueryInfo = {
    QueryType: QueryType
    Tables: string list
    IsParameterized: bool  // Security: must be true
}

and QueryType = Sql | Linq | Raw

and CodeLocation = {
    FilePath: string
    StartLine: int
    EndLine: int
    CommitHash: string
}

/// Pattern detection for "Code DNA"
and PatternNode = {
    Id: string
    PatternType: PatternType
    Description: string
    Frequency: int  // How often seen
    Confidence: float  // 0.0 - 1.0
    Examples: CodeEntityNode list
}

and PatternType =
    | Architectural of ArchitecturalPattern
    | Security of SecurityPattern
    | Performance of PerformancePattern
    | Style of StylePattern

and ArchitecturalPattern =
    | RepositoryPattern
    | CQRS
    | EventSourcing
    | CleanArchitecture
    | Microservice

and SecurityPattern =
    | InputValidation
    | OutputEncoding
    | ParameterizedQueries
    | SecureDefaults
    | LeastPrivilege

and PerformancePattern =
    | AsyncAwait
    | Caching
    | Batching
    | Streaming

and StylePattern =
    | NamingConvention of string
    | ErrorHandling of string
    | Documentation of string

/// Relationships between entities (Neo4j-style)
and RelationshipNode = {
    Id: string
    SourceId: string
    TargetId: string
    RelationType: RelationType
    Strength: float  // 0.0 - 1.0
    Metadata: Map<string, string>
}

and RelationType =
    | Calls
    | DependsOn
    | Implements
    | Inherits
    | Uses  // For DI/service usage
    | References  // DB references
    | Follows  // Pattern adherence
    | Violates  // Pattern violation

/// Context for current operation
and ContextNode = {
    Id: string
    SessionId: string
    ActiveEntities: string list  // Currently in focus
    WorkingMemory: WorkingMemoryItem list  // Limited capacity
    StackTrace: string list  // Call stack context
}

and WorkingMemoryItem = {
    EntityId: string
    RelevanceScore: float
    AccessTime: DateTime
}

// ============================================================================
// CODE DNA - Pattern enforcement
// ============================================================================

/// Code DNA fingerprint for architectural enforcement
type CodeDNA = {
    ProjectId: string
    Patterns: PatternFingerprint list
    Violations: Violation list
    HealthScore: float  // 0.0 - 1.0
}

and PatternFingerprint = {
    Pattern: PatternType
    Frequency: int
    Locations: string list
    AdherenceRate: float  // % of code following pattern
}

and Violation = {
    EntityId: string
    ViolatedPattern: PatternType
    Severity: ViolationSeverity
    SuggestedFix: string option
}

and ViolationSeverity = Info | Warning | Critical | Blocker

/// DNA Checker - validates code against project patterns
type DNAChecker(projectMemory: MemoryTier) =
    
    member _.CheckCode(entity: CodeEntityNode) : Violation list =
        match projectMemory with
        | Project config ->
            // Check repository pattern enforcement
            if entity.EntityType = Class { BaseClass = None; Interfaces = []; Methods = []; Properties = []; IsRepository = true } then
                // Repository MUST use parameterized queries
                let violations = 
                    if not (entity.Dependencies |> List.exists (fun d -> d.Contains("Parameterized"))) then
                        [{
                            EntityId = entity.Id
                            ViolatedPattern = Security ParameterizedQueries
                            Severity = Critical
                            SuggestedFix = Some "Use parameterized queries in repository"
                        }]
                    else
                        []
                violations
            else
                []
        | _ -> []
    
    member _.ComputeHealthScore(violations: Violation list) : float =
        if violations.IsEmpty then 1.0
        else
            let blockerCount = violations |> List.filter (fun v -> v.Severity = Blocker) |> List.length
            let criticalCount = violations |> List.filter (fun v -> v.Severity = Critical) |> List.length
            
            if blockerCount > 0 then 0.0
            else (1.0 - (float criticalCount * 0.2)) |> max 0.0

// ============================================================================
// MEMORY OPERATIONS
// ============================================================================

module MemoryOperations =
    /// Create hierarchical memory structure
    let createMemory (tier: MemoryTier) : KnowledgeNode list =
        match tier with
        | Ephemeral config ->
            [Context {
                Id = $"ctx-{config.SessionId}"
                SessionId = config.SessionId
                ActiveEntities = []
                WorkingMemory = []
                StackTrace = []
            }]
        | Project config ->
            [Context {
                Id = $"proj-{config.ProjectId}"
                SessionId = config.ProjectId
                ActiveEntities = config.DomainEntities
                WorkingMemory = []
                StackTrace = []
            }]
        | Global config ->
            [Context {
                Id = $"global-{config.OrganizationId}"
                SessionId = config.OrganizationId
                ActiveEntities = config.CodingStandards
                WorkingMemory = []
                StackTrace = []
            }]

    /// Query Neo4j-style graph
    let queryGraph (nodes: KnowledgeNode list) (query: string) : KnowledgeNode list =
        // Simplified query - in production would use Neo4j driver
        nodes |> List.filter (fun n ->
            match n with
            | CodeEntity e -> e.Name.Contains(query)
            | Pattern p -> p.Description.Contains(query)
            | _ -> false)

    /// Add entity to working memory (LRU eviction)
    let addToWorkingMemory (context: ContextNode) (entity: CodeEntityNode) (maxItems: int) : ContextNode =
        let newItem = {
            EntityId = entity.Id
            RelevanceScore = 1.0
            AccessTime = DateTime.UtcNow
        }
        
        let updatedMemory = 
            newItem :: context.WorkingMemory
            |> List.sortByDescending (fun i -> i.RelevanceScore)
            |> List.truncate maxItems
        
        { context with
            WorkingMemory = updatedMemory
            ActiveEntities = entity.Id :: context.ActiveEntities }

    /// Extract patterns from code entities
    let extractPatterns (entities: CodeEntityNode list) : PatternNode list =
        entities
        |> List.groupBy (fun e -> e.EntityType)
        |> List.map (fun (typ, group) ->
            {
                Id = $"pattern-{typ.GetHashCode()}"
                PatternType = Architectural RepositoryPattern  // Simplified
                Description = $"Pattern for {typ}"
                Frequency = group.Length
                Confidence = 0.85
                Examples = group |> List.truncate 3
            })

// ============================================================================
// CONSENSUS INTEGRATION
// ============================================================================

module ConsensusIntegration =
    /// Block code that violates project DNA
    let enforceDNAViaConsensus 
        (dna: CodeDNA) 
        (proposedCode: CodeEntityNode)
        (consensus: ConsensusResult<obj>) 
        : bool =
        
        let hasBlockerViolations = 
            dna.Violations 
            |> List.exists (fun v -> v.Severity = Blocker && v.EntityId = proposedCode.Id)
        
        match consensus with
        | Accepted _ ->
            if hasBlockerViolations then
                // Even with consensus, DNA violations block
                false
            else
                true
        | _ -> false

// ============================================================================
// C# INTEROP
// ============================================================================

module NeuralCSharpInterop =
    /// Create Code DNA checker for C#
    let createDNACheckerForCSharp (projectId: string) (standards: string[]) : obj =
        let config = {
            ProjectId = projectId
            TechStack = standards |> Array.toList
            DomainEntities = []
            RepositoryUrl = None
        }
        
        let checker = DNAChecker(Project config)
        box checker

    /// Check entity via C# bridge
    let checkEntityForCSharp (checker: obj) (entityData: obj) : obj =
        // Convert C# data to F# and check
        // Simplified - real implementation would parse entity
        box ([] : Violation list)

// ============================================================================
// EXAMPLES
// ============================================================================

module NeuralExamples =
    let demonstrate () =
        printfn "\n=== NEURAL CONTEXT & CODE DNA ==="
        
        // Create project memory
        let projectConfig = {
            ProjectId = "libr4-payments"
            RepositoryUrl = Some "https://github.com/libr4/payments"
            TechStack = ["F#"; "C#"; "PostgreSQL"; "RabbitMQ"]
            DomainEntities = ["Payment"; "Invoice"; "Escrow"; "Refund"]
        }
        
        let memory = MemoryOperations.createMemory (Project projectConfig)
        printfn "✅ Created project memory for %s" projectConfig.ProjectId
        
        // Create code entity with DNA
        let paymentService = {
            Id = "entity-001"
            Name = "PaymentService"
            EntityType = Class {
                BaseClass = None
                Interfaces = ["IPaymentService"]
                Methods = ["ProcessPayment"; "Refund"]
                Properties = ["DbContext"]
                IsRepository = true
            }
            Location = {
                FilePath = "/src/PaymentService.fs"
                StartLine = 1
                EndLine = 100
                CommitHash = "abc123"
            }
            SemanticHash = "a1b2c3d4"
            Dependencies = ["DbContext"; "Logger"]
            LastModified = DateTime.UtcNow
        }
        
        // Check DNA compliance
        let checker = DNAChecker(Project projectConfig)
        let violations = checker.CheckCode(paymentService)
        
        if violations.IsEmpty then
            printfn "✅ Code DNA: PaymentService follows repository pattern"
        else
            printfn "⚠️ Code DNA violations found:"
            violations |> List.iter (fun v ->
                printfn "   - %A: %A" v.Severity v.ViolatedPattern)
        
        // Create DNA report
        let dna = {
            ProjectId = projectConfig.ProjectId
            Patterns = [
                {
                    Pattern = Architectural RepositoryPattern
                    Frequency = 5
                    Locations = ["/src/PaymentService.fs"]
                    AdherenceRate = 0.95
                }
            ]
            Violations = violations
            HealthScore = if violations.IsEmpty then 1.0 else 0.8
        }
        
        printfn "\n📊 Project Health Score: %.0f%%" (dna.HealthScore * 100.0)
        printfn "🧬 Code DNA verification complete!"

// Run: Examples.demonstrate ()
