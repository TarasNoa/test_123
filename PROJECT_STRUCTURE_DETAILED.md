# Libr4 Framework - Detailed Technical Blueprint

**Version:** 1.0  
**Last Updated:** May 2, 2026  
**Status:** Production-Ready (Advanced Prototype Phase)

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [High-Level Architecture](#2-high-level-architecture)
3. [Service Map](#3-service-map)
4. [Language Boundaries](#4-language-boundaries)
5. [Autonomous AI & Agents](#5-autonomous-ai--agents)
6. [Shadow Workspace](#6-shadow-workspace)
7. [Infrastructure & Memory](#7-infrastructure--memory)
8. [Cross-Cutting Concerns](#8-cross-cutting-concerns)
9. [Deployment & Operations](#9-deployment--operations)

---

## 1. Executive Summary

Libr4 is a **polyglot, microservices-based AI-powered development platform** designed for autonomous application generation, freelancer marketplace operations, and intelligent code assistance. It combines the strengths of multiple programming paradigms:

- **C# 12 (.NET 8)**: Business logic, API controllers, domain services
- **F# 8**: Financial calculations, time tracking, tax management (Units of Measure for compile-time safety)
- **Rust**: Obscura headless browser (30MB, 85ms load), performance-critical media processing
- **Next.js 15 (TypeScript)**: Modern React frontend with App Router

**Core Innovation**: Shadow Workspace Escrow System - Docker-isolated build environments with AI self-healing and payment-triggered code release.

---

## 2. High-Level Architecture

### 2.1 System Topology

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENT LAYER                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────┐      ┌─────────────────────────────────────┐  │
│  │   Next.js 15 Frontend   │      │   Mobile Apps (Future: React Native)│  │
│  │   Port: 3000            │      │                                     │  │
│  │   • TailwindCSS         │      │                                     │  │
│  │   • TanStack Query      │      │                                     │  │
│  │   • Yjs CRDT Sync       │      │                                     │  │
│  └───────────┬─────────────┘      └─────────────────────────────────────┘  │
│              │ HTTPS + JWT (Bearer)                                        │
└──────────────┼──────────────────────────────────────────────────────────────┘
               │
┌──────────────▼──────────────────────────────────────────────────────────────┐
│                           GATEWAY LAYER (Port 5000)                         │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                    YARP Reverse Proxy (Libr4.Gateway)                 │   │
│  │  • JWT Validation                                                    │   │
│  │  • Dynamic Preview Routing (/preview/{hash}/{orderId})               │   │
│  │  • Rate Limiting (30 req/min per preview)                            │   │
│  │  • WebSocket Upgrade Handling                                        │   │
│  └────────┬────────┬────────┬────────┬────────┬────────┬────────────────┘   │
│           │        │        │        │        │        │                      │
└───────────┼────────┼────────┼────────┼────────┼────────┼──────────────────┘
            │        │        │        │        │        │
   ┌────────▼──┐  ┌──▼───┐  ┌▼──────┐ ┌▼─────┐ ┌▼──────┐ ┌▼────────┐
   │   Auth    │  │ Tasks│  │Payments│ │ Chat │ │Trading│ │   AI    │
   │ Port 5001 │  │ 5002 │  │  5003  │ │ 5004 │ │ 5005  │ │  5006   │
   └───────────┘  └──────┘  └────────┘ └──────┘ └───────┘ └─────────┘
            │        │        │        │        │        │
   ┌────────▼────────────────────────────────────────────────────────┐
   │                    IDE SERVICE (Port - Internal)                  │
   │         625+ items - Largest microservice                     │
   │  • Shadow Workspace                                             │
   │  • Autonomous App Generation                                    │
   │  • Code Intelligence                                            │
   │  • Obscura Browser Integration                                  │
   └─────────────────────────────────────────────────────────────────┘
```

### 2.2 Data Flow Patterns

**Synchronous (HTTP via Gateway)**:
```
Frontend → Gateway → Service.Api → Service.Application → Service.Domain
                                              ↓
                                    Service.Infrastructure (EF Core, Redis)
```

**Asynchronous (RabbitMQ + MassTransit - COMMENTED OUT)**:
```
Service.Application → Integration Event → RabbitMQ → Other Service Handler
```

**Real-time (WebSocket/Socket.IO)**:
```
Client ←→ SignalR Hub ←→ Service ←→ Redis Backplane (for scale)
```

---

## 3. Service Map

### 3.1 Gateway (Libr4.Gateway)

**Location**: `src/Gateway/Libr4.Gateway/`  
**Port**: 5000  
**Purpose**: API Gateway, reverse proxy, authentication checkpoint

**Structure**:
```
Libr4.Gateway/
├── Program.cs                          # Entry point, YARP configuration
├── RateLimitingExtensions.cs         # Preview-specific rate limiting (30 req/min)
├── DynamicPreviewRouter.cs             # Runtime route registration for Shadow Workspace
├── PreviewManagementEndpoints.cs       # API for managing preview routes
└── PreviewCleanupBackgroundService.cs  # Auto-cleanup expired routes (5-min interval)
```

**Key Technologies**:
- **YARP** (Yet Another Reverse Proxy): Dynamic routing to microservices and Shadow containers
- **JWT Bearer Authentication**: Validates tokens from Auth service
- **Rate Limiting**: Fixed window for previews (30/min), token bucket for WebSockets

**How DynamicPreviewRouter Works**:
1. When escrow workspace created, IDE calls `POST /api/gateway/previews/register`
2. Router creates YARP route: `/preview/{customerHash}/{orderId}/**` → `http://shadow-container-{orderId}:3000`
3. Route stored in `ConcurrentDictionary` with 2-hour expiry
4. Background service cleans expired routes every 5 minutes

---

### 3.2 Auth Service (Libr4.Auth)

**Location**: `src/Services/Auth/`  
**Port**: 5001  
**Purpose**: Identity, authentication, authorization, RBAC

**Structure**:
```
Libr4.Auth/
├── Libr4.Auth.Api/                     # Controllers, Program.cs
│   ├── Program.cs                      # DI configuration, JWT setup
│   ├── Endpoints/                      # Minimal API endpoints
│   └── appsettings.json                # JWT signing keys, connection strings
├── Libr4.Auth.Application/             # Use cases, handlers
│   ├── Commands/                       # Register, Login, RefreshToken
│   ├── Queries/                        # GetUser, ValidateToken
│   └── Abstractions/                   # ITokenService, IPasswordHasher
├── Libr4.Auth.Domain/                  # Entities, value objects
│   ├── User.cs                         # Aggregate root with roles
│   ├── RefreshToken.cs                 # Value object
│   └── Events/                         # UserRegistered, PasswordChanged
├── Libr4.Auth.Infrastructure/          # Persistence, external APIs
│   ├── Persistence/                    # AuthDbContext, migrations
│   ├── Services/                       # JwtTokenService, BcryptPasswordHasher
│   └── DependencyInjection.cs          # EF Core, Identity registration
└── Libr4.Auth.Domain.Algorithms/       # F# algorithms (stub - not implemented)
```

**Key Technologies**:
- **ASP.NET Core Identity**: User management with claims-based RBAC
- **JWT HS256**: Token signing with symmetric key (configurable)
- **2FA with TOTP**: Time-based one-time passwords
- **EF Core 8**: PostgreSQL persistence with migrations (COMMENTED OUT in Program.cs)

**Token Flow**:
1. User POST `/api/auth/login` → receives JWT + refresh token
2. Gateway validates JWT on every request via `AddLibr4JwtAuth`
3. Roles stored in claims: `[Authorize(Roles = "admin")]`

---

### 3.3 Tasks Service (Libr4.Tasks)

**Location**: `src/Services/Tasks/`  
**Port**: 5002  
**Purpose**: Task management, project organization, time tracking

**Structure**:
```
Libr4.Tasks/
├── Libr4.Tasks.Api/                    # API endpoints
├── Libr4.Tasks.Application/            # MediatR handlers
├── Libr4.Tasks.Domain/                 # Entities (Task, Project, TimeSession)
├── Libr4.Tasks.Infrastructure/         # EF Core, repositories
└── Libr4.Tasks.Domain.TimeTracking.FSharp/  # ⭐ F# UNITS OF MEASURE
    ├── FinancialTypes.fs               # Currencies (RUB/USD/EUR), time (hour/day/week)
    ├── Types.fs                        # TimeSession, Screenshot, ActivityLog records
    ├── TimeSessionOps.fs               # Business logic operations
    └── TimeTrackingErrors.fs           # Domain errors
```

**Key Technologies**:
- **F# Units of Measure**: Compile-time safety for financial/time calculations
- **MediatR**: CQRS with commands/queries
- **Redis**: Session caching

**F# Financial Types Example**:
```fsharp
[<Measure>] type RUB
[<Measure>] type USD
[<Measure>] type hour
[<Measure>] type percent

type HourlyRate<'currency> = float<'currency/hour>

// CANNOT accidentally mix units!
let calculateEarnings (hours: float<hour>) (rate: HourlyRate<RUB>) : float<RUB> =
    hours * rate  // Returns RUB
```

---

### 3.4 Payments Service (Libr4.Payments)

**Location**: `src/Services/Payments/`  
**Port**: 5003  
**Purpose**: Stripe integration, escrow, fraud detection, AML/KYC

**Structure**:
```
Libr4.Payments/
├── Libr4.Payments.Api/                 # Controllers, endpoints
├── Libr4.Payments.Application/         # Handlers, DTOs
│   ├── Escrow/                         # Escrow release logic
│   └── Tax/                            # Tax calculation service
├── Libr4.Payments.Domain/              # Payment, Escrow aggregates
├── Libr4.Payments.Domain.TaxManagement.FSharp/  # ⭐ F# TAX CALCULATIONS
│   ├── TaxTypesWithMeasures.fs         # TaxRate, TaxAmount<'currency>
│   └── TaxCalculator.cs                # C# wrapper for F# functions
├── Libr4.Payments.Infrastructure/      # Stripe SDK, repositories
└── Rust/                               # ⭐ 16 Rust files for crypto/tax
    └── src/                            # Financial calculations in Rust
```

**Key Technologies**:
- **Stripe SDK**: Payment processing, subscriptions
- **F# Tax Calculation**: Compile-time safe VAT/income tax
- **Escrow System**: Payment hold until code delivery confirmed

**Tax Calculation with F#**:
```csharp
// C# uses F# Units of Measure internally
var result = TaxCalculator.CalculateVAT(
    netPrice: 1000.0, 
    vatRate: 20.0  // 20%
);
// Returns: { NetPrice: 1000, VATAmount: 200, GrossPrice: 1200 }
```

---

### 3.5 Chat Service (Libr4.Chat)

**Location**: `src/Services/Chat/`  
**Port**: 5004  
**Purpose**: Real-time messaging, team collaboration, notifications

**Structure**:
```
Libr4.Chat/
├── Libr4.Chat.Api/                     # REST + WebSocket endpoints
├── Libr4.Chat.Application/             # Message handlers
├── Libr4.Chat.Domain/                  # Message, Channel, Thread aggregates
├── Libr4.Chat.Infrastructure/          # SignalR, persistence
└── Libr4.Chat.Domain.Algorithms/       # F# message processing
```

**Key Technologies**:
- **SignalR**: Real-time messaging with Redis backplane
- **WebSocket Streaming**: Live typing indicators, presence
- **EF Core**: Message persistence with pagination

---

### 3.6 Trading Service (Libr4.Trading)

**Location**: `src/Services/Trading/`  
**Port**: 5005  
**Purpose**: Cryptocurrency trading, Binance integration, market data

**Structure**:
```
Libr4.Trading/
├── Libr4.Trading.Api/                  # Trading endpoints
├── Libr4.Trading.Application/          # Order execution, strategies
├── Libr4.Trading.Domain/               # Order, Position, Trade aggregates
└── Libr4.Trading.Infrastructure/       # Binance API client, WebSocket feeds
```

**Key Technologies**:
- **Binance API**: Spot and futures trading
- **WebSocket Streams**: Real-time market data (NOT IMPLEMENTED - throws NotImplementedException)
- **F# Algorithms**: Trading logic (time series analysis)

---

### 3.7 AI Service (Libr4.AI)

**Location**: `src/Services/AI/`  
**Port**: 5006  
**Purpose**: LLM orchestration, multi-provider AI, hooks system

**Structure**:
```
Libr4.AI/
├── Libr4.AI.Api/                       # OpenAI-compatible endpoints
│   ├── Program.cs                      # Hook system initialization
│   └── Endpoints/                      # Chat, agents, voice endpoints
├── Libr4.AI.Application/               # LLM routing, context management
│   ├── Abstractions/                   # ILLMProvider, IChatContext
│   ├── MultiProvider/                  # OpenAI, Anthropic, Groq, Ollama
│   └── Agents/                         # Agent orchestration
├── Libr4.AI.Domain/                    # Conversation, Message entities
│   ├── Conversations/                  # Chat session aggregates
│   ├── Memory.FSharp/                  # ⭐ F# enhanced memory algorithms
│   └── LocalAI.FSharp/                 # ⭐ F# local model inference
├── Libr4.AI.Infrastructure/              # LLM clients, hooks, persistence
│   ├── LLM/                            # Provider implementations
│   ├── Hooks/                          # SessionLogging, ContextCompression
│   └── AI.DbContext/                   # EF Core (MCP memory storage)
└── Libr4.AI.Domain.Agents.Algorithms/  # F# agent algorithms
```

**Key Technologies**:
- **Multi-Provider LLM Routing**: OpenAI, Anthropic, Groq, OpenRouter, Ollama
- **Hook System**: Modular processing pipeline
  - `SessionLoggingHook`: Logs all AI interactions
  - `ContextCompressionHook`: Compresses long contexts for token limits
  - `HumanizerHook`: Makes AI responses more natural
  - `ToolUsageLoggingHook`: Tracks tool/agent usage
- **MCP Server**: Model Context Protocol for memory (remember/recall tools)

**Hook Registration** (in Program.cs):
```csharp
var hookManager = app.Services.GetRequiredService<HookManager>();
hookManager.RegisterHook(sessionLoggingHook);
hookManager.RegisterHook(contextCompressionHook);
hookManager.RegisterHook(humanizerHook);
hookManager.RegisterHook(toolUsageLoggingHook);
```

---

### 3.8 IDE Service (Libr4.IDE) - MONOLITHIC MICROSERVICE

**Location**: `src/Services/IDE/`  
**Port**: Internal (via Gateway)  
**Purpose**: Code assistance, autonomous generation, Shadow Workspace, agent orchestration

**Size**: 625+ items - **Largest service in the solution**

**Structure**:
```
Libr4.IDE/
├── Libr4.IDE.Api/                      # 32 items - REST + SignalR endpoints
│   ├── Program.cs                      # Massive DI registration (80+ lines)
│   ├── ShadowWorkspaceHub.cs           # ⭐ SignalR for real-time collaboration
│   ├── ObscuraEndpoints.cs             # Browser automation endpoints
│   ├── CodeReviewEndpoints.cs          # AI code review
│   ├── SecurityTestingEndpoints.cs     # Security scan endpoints
│   └── ... (18+ endpoint files)
├── Libr4.IDE.Application/              # 179 items - Use cases
│   ├── ShadowWorkspace/                # ⭐ Docker container management
│   │   ├── SelfHealingBuildPipeline.cs # ⭐ AI fixes build errors
│   │   └── Handlers/                   # CreateShadowWorkspaceCommand
│   ├── FreelancerMarketplace/          # ⭐ Escrow business logic
│   │   ├── EscrowCodeService.cs        # ⭐ Payment-triggered release
│   │   └── GatewayPreviewIntegration.cs # ⭐ YARP preview registration
│   ├── Obscura/                        # ⭐ Browser automation
│   │   ├── AgentObscuraTool.cs         # High-level agent browser API
│   │   ├── SubagentObscuraIntegration.cs # Subagent browser config
│   │   └── Handlers/                   # Navigate, Screenshot, etc.
│   ├── CodeReview/                     # AI pattern analysis
│   ├── SecurityTesting/                # SQL Injection, XSS detection
│   ├── HackerAgent/                    # Security script execution
│   ├── SemanticBlame/                  # Git + AI analysis
│   ├── SemanticCodeGraph/              # Code navigation graph
│   ├── CodeIntelligence/               # Smart completions
│   ├── GitHubBootstrap/                # Clone + analyze
│   ├── ArchitecturalGuardrails/        # SOLID validation
│   ├── WebSearch/                      # Multi-provider search
│   └── MultiAgentOrchestration/        # ⭐ Swarm intelligence
│       └── ContextCompressionService.cs # ⭐ HiveMind compression
├── Libr4.IDE.Domain/                   # 177 items - Entities
│   ├── MultiAgentOrchestration/        # AgentOrchestration aggregate
│   │   └── AgentOrchestration.cs       # 10,000+ lines - Swarm logic
│   ├── ShadowWorkspace/                # Domain entities
│   └── HackerAgent/                    # Security agent aggregate
├── Libr4.IDE.Infrastructure/           # 11 items
│   ├── Containers/                     # ⭐ Docker management
│   │   ├── ContainerManager.cs         # ⭐ Container lifecycle
│   │   └── ContainerConnectionTracker.cs # ⭐ WebSocket connection tracking
│   ├── Collaboration/                  # ⭐ CRDT document sync
│   │   └── CrdtDocumentService.cs      # Yjs-compatible CRDT
│   └── Obscura/                      # ⭐ Browser service implementation
│       └── ObscuraBrowserService.cs      # CDP WebSocket, process management
├── Libr4.IDE.AutonomousAppGeneration/  # 224 items - ⭐ MAJOR SUBSYSTEM
│   ├── Agents/                         # Specialized agents
│   │   ├── CICDPipelineAgent.cs        # CI/CD generation
│   │   ├── DatabaseDesignAgent.cs        # ERD, schema design
│   │   ├── PerformanceProfilingAgent.cs  # Performance analysis
│   │   ├── TechDebtTrackingAgent.cs      # Technical debt
│   │   ├── ObservabilityAgent.cs         # Logging, metrics, traces
│   │   └── SubagentOrchestrator.cs     # Subagent coordination
│   ├── Tooling/                        # Agent tooling
│   │   ├── Subagents/                  # Subagent configuration
│   │   │   └── SubagentProfileServices.cs # 16 references
│   │   └── PlanAgent/                  # Planning algorithms
│   └── AutonomousAppGeneration/        # Main orchestration
└── Libr4.IDE.AutonomousAppGeneration.Rules.FSharp/  # F# generation rules
```

---

## 4. Language Boundaries

### 4.1 C# 12 (.NET 8) - Primary Language

**Usage**: 80% of codebase  
**Responsibility**: Business logic, API controllers, DI, domain services

**Key Features Used**:
- **Minimal APIs**: `app.MapGet()`, `app.MapPost()` for endpoints
- **Records**: DTOs with value semantics
- **Pattern Matching**: `switch` expressions for domain logic
- **Async/Await**: Throughout for I/O operations
- **Dependency Injection**: Scoped, Singleton, Transient lifetimes

**Entry Points**:
- All `Program.cs` files (10+ services)
- All MediatR handlers (100+)
- All controller classes

---

### 4.2 F# 8 - Mathematical/Financial Domain

**Usage**: 15% of codebase (205 .fs files)  
**Responsibility**: Financial calculations, time tracking, tax management, algorithms

**Key Files**:
```
Libr4.Tasks.Domain.TimeTracking.FSharp/
├── FinancialTypes.fs                   # ⭐ UNITS OF MEASURE
│   # [<Measure>] type RUB, USD, EUR, hour, day, percent
│   # HourlyRate<'currency>, Money<'currency>
│   # Billing.calculateInvoice (compile-time safe)

Libr4.Payments.Domain.TaxManagement.FSharp/
├── TaxTypesWithMeasures.fs             # ⭐ TAX CALCULATIONS
│   # TaxRate, TaxAmount<'currency>
│   # calculateTax, calculateVAT, calculateProgressiveTax

Libr4.AI.Domain.Memory.FSharp/
├── MemoryGraph.fs                      # Graph memory algorithms

Libr4.IDE.Domain.Algorithms/
└── CodeAssistant.fs                      # Code generation in F#
```

**Why F#?**
- **Units of Measure**: Compile-time currency/time safety
  ```fsharp
  // This will NOT compile:
  let wrong = 100.0<RUB> + 50.0<USD>  // ERROR!
  ```
- **Discriminated Unions**: Perfect for domain modeling
- **Pipe Operator**: Clean data transformation

**C# Interop**:
```csharp
// C# calls F# function
var result = FinancialCalculator.CalculateInvoice(
    hoursWorked: 40,
    hourlyRate: 1500,
    discountPercent: 10,
    taxPercent: 20
);
// Uses F# Units of Measure internally
```

---

### 4.3 Rust - Performance & Browser

**Usage**: 5% of codebase (16 .rs files + Obscura binary)  
**Responsibility**: Headless browser, performance-critical operations, FFI

**Locations**:
```
obscura/                                # ⭐ HEADLESS BROWSER (External)
├── crates/                             # 47 Rust crates
├── Cargo.toml                        # V8 engine, CDP protocol
└── README.md                         # 30MB RAM, 85ms load

src/Services/Payments/Rust/           # Financial crypto
src/Services/Media/Rust/              # Media processing
```

**Obscura Integration**:
```csharp
// C# starts Rust process, communicates via CDP WebSocket
var process = new Process {
    StartInfo = new ProcessStartInfo {
        FileName = "obscura",  // Rust binary
        Arguments = "serve --port 9222 --stealth"
    }
};
process.Start();

// Connect via WebSocket to Chrome DevTools Protocol
using var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri("ws://localhost:9222/devtools/browser"));
```

**Why Rust?**
- **Memory Safety**: No garbage collection pauses
- **Performance**: 30MB vs 200MB for Chrome
- **FFI**: Can be called from C# via P/Invoke or process spawn

---

### 4.4 IronPython - Legacy Interop (Planned)

**Status**: NOT IMPLEMENTED  
**Purpose**: Legacy Python script integration  
**Location**: Would be in `src/Services/IDE/Libr4.IDE.Infrastructure.Python/`

**Design Pattern** (from global_rules.md):
```csharp
// Wrap in C# decorator to isolate
public class PythonScriptExecutor : IScriptExecutor
{
    private readonly ScriptEngine _pythonEngine;  // IronPython
    
    public object Execute(string script, Dictionary<string, object> args)
    {
        // Sandboxed execution
        var scope = _pythonEngine.CreateScope();
        foreach (var arg in args) scope.SetVariable(arg.Key, arg.Value);
        return _pythonEngine.Execute(script, scope);
    }
}
```

---

## 5. Autonomous AI & Agents

### 5.1 IAgent Hierarchy

**Location**: `src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/Agents/`

```csharp
namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Base interface for all autonomous agents
/// </summary>
public interface IAgent
{
    Task<AgentResult> ExecuteAsync(AgentContext context);
}

/// <summary>
/// Agent execution context
/// </summary>
public class AgentContext
{
    public string ApplicationName { get; set; }
    public string Description { get; set; }
    public string TechStack { get; set; }
    public GeneratedFile[]? GeneratedFiles { get; set; }
    public string? Feedback { get; set; }
    public AgentTask? Task { get; set; }
}

/// <summary>
/// Agent execution result
/// </summary>
public class AgentResult
{
    public bool IsSuccess { get; set; }
    public string Content { get; set; }
    public List<AgentTask>? SuggestedSubtasks { get; set; }
    public object? DatabaseDesign { get; set; }
    public object? CICDPipeline { get; set; }
    public object? PerformanceProfile { get; set; }
}
```

### 5.2 Specialized Agents

| Agent | Responsibility | Key Method |
|-------|---------------|------------|
| **CICDPipelineAgent** | Generate CI/CD configs | `GeneratePipelineAsync()` |
| **DatabaseDesignAgent** | Design ERD, schemas | `GenerateDatabaseDesignAsync()` |
| **PerformanceProfilingAgent** | Analyze performance | `ProfileAsync()` |
| **TechDebtTrackingAgent** | Track technical debt | `AnalyzeCodebaseAsync()` |
| **ObservabilityAgent** | Logging, metrics, traces | `GenerateObservabilityConfig()` |
| **SubagentOrchestrator** | Coordinate subagents | `OrchestrateAsync()` |

### 5.3 Subagent System

**Configuration Location**: `src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/Tooling/Subagents/`

**Key Components**:
1. **SubagentProfileServices.cs** - 16 references to subagent management
2. **SubagentProfileContracts.cs** - Interface definitions
3. **SubagentRoutingService.cs** - Routes tasks to appropriate subagents

**5-Tier Priority System**:
```yaml
# Subagent configuration example
subagent_id: database-designer
name: Database Designer Agent
priority: 1  # 1=Critical, 5=Background
permissions:
  - read_schema
  - suggest_migrations
  - generate_erd
model_pointers:
  main: gpt-4
  task: gpt-3.5-turbo
  compact: gpt-3.5-turbo-16k
fork_context: true  # Isolated context per task
```

### 5.4 Multi-Agent Orchestration

**Location**: `src/Services/IDE/Libr4.IDE.Domain/MultiAgentOrchestration/`

**10,000+ line file**: `AgentOrchestration.cs`

**Swarm Topologies**:
```csharp
public enum SwarmTopology
{
    Hierarchical,    // Queen-led hierarchy with central coordinator
    Mesh,           // Peer-to-peer network
    Adaptive,       // Changes based on task complexity
    HiveMind        // Shared consciousness
}
```

**Consensus Mechanisms**:
```csharp
public enum ConsensusMechanism
{
    Majority,       // Simple voting
    Weighted,       // Based on agent performance
    Unanimous       // Full consensus required
}
```

**Learning Pattern** (from Ruflo SONA):
```csharp
public class LearningPattern
{
    public string PatternName { get; }
    public double SuccessRate { get; private set; }  // EMA: (rate * 0.9) + (success ? 0.1 : 0)
    public int UsageCount { get; }
    
    public void RecordSuccess(bool success)
    {
        SuccessRate = (SuccessRate * 0.9) + (success ? 0.1 : 0.0);
    }
}
```

---

## 6. Shadow Workspace

### 6.1 Overview

**Purpose**: Docker-isolated escrow environment for freelancer marketplace

**Key Innovation**: Customer sees preview but cannot access source code until payment

### 6.2 Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     SHADOW WORKSPACE SYSTEM                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │              Customer Browser (Preview Mode)             │  │
│  │  • Sees running app via YARP proxy                        │  │
│  │  • No access to container shell                          │  │
│  │  • Read-only VNC/WebRTC stream (optional)               │  │
│  └──────────────┬────────────────────────────────────────────┘  │
│                 │ HTTPS (YARP Gateway)                          │
│  ┌──────────────▼────────────────────────────────────────────┐  │
│  │              YARP Gateway (Port 5000)                      │  │
│  │  • /preview/{customerHash}/{orderId}/*                   │  │
│  │  • Dynamic route to container                            │  │
│  │  • Rate limiting: 30 req/min                             │  │
│  └──────────────┬────────────────────────────────────────────┘  │
│                 │ HTTP (internal network)                       │
│  ┌──────────────▼────────────────────────────────────────────┐  │
│  │         Docker Container (shadow-{orderId})              │  │
│  │  • Node.js/React/Next.js running                        │  │
│  │  • Port 3000 exposed                                     │  │
│  │  • Read-only filesystem (except /tmp)                    │  │
│  │  • Memory: 2GB, CPU: 1 core                           │  │
│  └──────────────┬────────────────────────────────────────────┘  │
│                 │ Docker API (volume mount)                     │
│  ┌──────────────▼────────────────────────────────────────────┐  │
│  │         Host IDE Service                                  │  │
│  │  • CRDT sync (collaborative editing)                     │  │
│  │  • Build pipeline (SelfHealingBuildPipeline)           │  │
│  │  • Escrow business logic (EscrowCodeService)           │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  Payment Trigger:                                                │
│  Customer pays → Stripe webhook → EscrowCodeService            │
│  → Git merge to main → Container destroyed                     │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 6.3 Key Components

| Component | Location | Responsibility |
|-----------|----------|---------------|
| **ContainerManager** | `Infrastructure/Containers/` | Docker lifecycle (create, start, stop, remove) |
| **CrdtDocumentService** | `Infrastructure/Collaboration/` | Yjs-compatible CRDT sync |
| **SelfHealingBuildPipeline** | `Application/ShadowWorkspace/` | Build → Error → AI Fix → Retry loop |
| **EscrowCodeService** | `Application/FreelancerMarketplace/` | Payment logic, merge criteria |
| **ShadowWorkspaceHub** | `Api/` | SignalR for real-time collaboration |
| **ContainerConnectionTracker** | `Infrastructure/Containers/` | WebSocket connection tracking |

### 6.4 Self-Healing Build Pipeline

**Logic Flow**:
```
Developer pushes code → Trigger Build
    ↓
Build fails → AI analyzes error
    ↓
AI generates fix → Apply fix
    ↓
Retry build (max 3 attempts)
    ↓
Success → Continue to tests
Fail → Notify developer with AI suggestions
```

**Implementation**: `SelfHealingBuildPipeline.cs`
```csharp
public async Task<BuildResult> ExecuteWithHealingAsync(
    string workspaceId, 
    int maxIterations = 3)
{
    for (int i = 0; i < maxIterations; i++)
    {
        var result = await ExecuteSingleBuildAsync(workspaceId);
        if (result.Success) return result;
        
        // AI analyzes error and generates fix
        var fix = await _aiService.GenerateFixAsync(result.ErrorOutput);
        await ApplyFixAsync(workspaceId, fix);
    }
    return result;
}
```

### 6.5 Escrow Business Logic

**Release Criteria** (from `EscrowCodeService.cs`):
1. Build passes
2. Tests pass (coverage threshold)
3. Security scan clean
4. Code review approved (AI + human)
5. **Customer payment received**

**API**:
```csharp
public interface IEscrowCodeService
{
    Task SetupEscrowWorkspaceAsync(string orderId, string freelancerId);
    Task EvaluateReleaseCriteriaAsync(string orderId);
    Task MergeToMainAsync(string orderId);  // Triggered by payment
    Task<string> GetPreviewUrlAsync(string orderId, string customerId);
}
```

---

## 7. Infrastructure & Memory

### 7.1 Hybrid Memory System (Qdrant + Neo4j)

**Purpose**: Agent memory with vector search + graph relationships

**Architecture**:
```
┌─────────────────────────────────────────────────────────────┐
│                    MEMORY LAYERS                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Level 1: User Memory (Long-term)                          │
│  ├── Qdrant: Vector embeddings for semantic search         │
│  └── Neo4j: Entity relationships (User → Project → Task)    │
│                                                              │
│  Level 2: Session Memory (Short-term)                        │
│  └── Redis: Conversation context, recent actions            │
│                                                              │
│  Level 3: Agent Memory (Specialized)                       │
│  └── F# algorithms in Memory.Enhanced.FSharp            │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

**MCP Server Tools**:
- `remember`: Store information in memory
- `recall`: Retrieve with semantic similarity
- `list_agents`: Get available agents
- `compress_context`: Summarize long contexts

### 7.2 Docker Compose Infrastructure

**Location**: `docker-compose.yml` + `docker-compose.infra.yml`

**Services**:
```yaml
# Infrastructure (docker-compose.infra.yml)
postgres:       # Per-service databases (libr4_auth, libr4_tasks, etc.)
redis:          # Caching, session storage, Pub/Sub
rabbitmq:       # Message bus (COMMENTED OUT in code)
ollama:         # Local LLM inference
prometheus:     # Metrics collection
grafana:        # Metrics visualization (planned)

# Application Services (docker-compose.yml)
gateway:        # YARP reverse proxy (Port 5000)
auth-api:       # JWT authentication (Port 5001)
tasks-api:      # Task management (Port 5002)
payments-api:   # Stripe integration (Port 5003)
chat-api:       # Real-time messaging (Port 5004)
trading-api:    # Binance trading (Port 5005)
ai-api:         # LLM orchestration (Port 5006)
frontend:       # Next.js 15 (Port 3000)
```

### 7.3 Observability Stack

**Metrics (Prometheus)**:
- `/metrics` endpoint on all services
- Custom metrics: build duration, AI token usage, container count

**Logging (Serilog)**:
- Structured JSON logging
- Correlation IDs across services
- OpenTelemetry integration

**Tracing (OpenTelemetry)**:
- Distributed tracing across microservices
- Jaeger/Zipkin export (configurable)

---

## 8. Cross-Cutting Concerns

### 8.1 Shared Libraries (src/Shared/)

**Libr4.Shared.Kernel**:
- `Entity<T>`: Base class for all domain entities
- `AggregateRoot<T>`: Base for aggregate roots
- `Result<T>`: Railway-oriented programming
- `Error`: Domain error types
- `IClock`: Testable time abstraction

**Libr4.Shared.Contracts**:
- Integration event DTOs
- **Subagent system** (from Kode-Agent)
- **Template system** (from Fragments)
- Rate limiting contracts
- Analytics contracts
- Streaming contracts

**Libr4.Shared.Infrastructure**:
- `DbContextBase`: EF Core base with outbox pattern
- Redis DI extensions
- MassTransit setup (COMMENTED OUT)
- OpenTelemetry configuration

**Libr4.Shared.Web**:
- `AddLibr4JwtAuth()`: JWT Bearer authentication
- `ExceptionHandlingMiddleware`: Global error handling
- `CorrelationIdMiddleware`: Request tracing
- Health checks (/health/live, /health/ready)
- Swagger configuration

**Libr4.Shared.Observability**:
- Serilog configuration
- OpenTelemetry traces/metrics
- Prometheus integration

### 8.2 Security Patterns

**Authentication Flow**:
```
1. User POST /api/auth/login (Auth service)
2. Validate credentials → Generate JWT + Refresh token
3. Client stores JWT (HttpOnly cookie or localStorage)
4. Gateway validates JWT on every request
5. Services extract claims for authorization
```

**Rate Limiting** (from `RateLimitingExtensions.cs`):
```csharp
// Preview routes: 30 requests per minute
options.AddFixedWindowLimiter("PreviewStrict", opt => {
    opt.PermitLimit = 30;
    opt.Window = TimeSpan.FromMinutes(1);
});

// WebSockets: Token bucket for burst
options.AddTokenBucketLimiter("WebSocketBurst", opt => {
    opt.TokenLimit = 10;
    opt.ReplenishmentPeriod = TimeSpan.FromSeconds(30);
});
```

---

## 9. Deployment & Operations

### 9.1 Development Setup

```bash
# 1. Start infrastructure
docker-compose up -d postgres redis rabbitmq

# 2. Run migrations (need to uncomment in Program.cs first)
dotnet ef database update --project src/Services/Auth/Libr4.Auth.Infrastructure

# 3. Start services
dotnet run --project src/Services/Auth/Libr4.Auth.Api
dotnet run --project src/Services/AI/Libr4.AI.Api
dotnet run --project src/Services/IDE/Libr4.IDE.Api

# 4. Start gateway
dotnet run --project src/Gateway/Libr4.Gateway

# 5. Start frontend
cd frontend && npm run dev
```

### 9.2 Production Checklist

**High Priority (2-3 weeks)**:
- [ ] Uncomment database migrations in all Program.cs
- [ ] Replace InMemory repositories with EF Core
- [ ] Enable RabbitMQ/MassTransit (uncomment)
- [ ] Implement TOML parsing for subagents
- [ ] Fix empty catch blocks
- [ ] Remove hardcoded secrets
- [ ] Implement admin checks in Payments
- [ ] Fix async/await warnings (25+ warnings)

**Medium Priority (1-2 weeks)**:
- [ ] Integrate E2B SDK for sandbox
- [ ] Implement WebSocket streaming in Trading
- [ ] Add exception handling
- [ ] Production configuration
- [ ] Implement linting in HarnessEnvironment

### 9.3 Key Configuration Values

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=postgres;Port=5432;Database=libr4_{service};Username=libr4;Password=***",
    "Redis": "redis:6379",
    "RabbitMq": "rabbitmq:5672"
  },
  "Jwt": {
    "Issuer": "libr4",
    "Audience": "libr4-clients",
    "SigningKey": "***64-bytes-minimum***"
  },
  "Obscura": {
    "BinaryPath": "/usr/local/bin/obscura"
  },
  "ShadowWorkspace": {
    "DefaultMemory": "2g",
    "DefaultCpu": "1.0",
    "Network": "libr4-shadow-network"
  }
}
```

---

## Summary Statistics

| Metric | Value |
|--------|-------|
| **Total Projects** | 80+ |
| **C# Files** | 1,587 |
| **F# Files** | 205 |
| **Rust Files** | 16 + Obscura binary |
| **Services** | 17 microservices |
| **Largest Service** | IDE (625 items) |
| **Lines in Solution** | 1,225 (libr4.sln) |
| **Frontend** | Next.js 15 (131 items) |
| **Docker Services** | 10+ |

---

## Document Information

- **Author**: Libr4 Technical Team
- **Generated By**: Kimi AI (Cascade)
- **Format**: Markdown with code blocks
- **Last Updated**: May 2, 2026
- **Version**: 1.0

**Next Steps**: Review implementation status, prioritize production readiness tasks, deploy to staging environment.
