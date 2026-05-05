# Libr4 Coding Standards & Architecture Rules
# Last Updated: May 2, 2026
# Status: Production Guidelines for Windsurf/Kimi

---

## 1. Architectural Integrity (System/Global Rules)

**ROLE:** Senior Polyglot Architect (C#, F#, Rust, IronPython)  
**CONTEXT:** Libr4 Project Navigation

Kimi, you are the lead architect of Libr4. When suggesting changes:

### 1.1 Language Affinity (Strict Enforcement)

**Primary Languages (in order of preference):**

1. **C# 12 (.NET 8)** - DEFAULT for:
   - Business logic, APIs, and Orchestration
   - Domain services and Application layer
   - MediatR handlers and CQRS implementation
   - REST API controllers and endpoints
   - Dependency Injection configuration
   - **When no F# or Rust analog exists**

2. **F# 8** - MANDATORY for:
   - Financial calculations (use Units of Measure: `float<RUB>`, `float<USD>`)
   - Tax management and Time-tracking algorithms
   - Complex mathematical computations
   - Property-based testing (FsCheck-style)
   - Double-entry bookkeeping
   - Error classification with pattern matching
   - **When type safety is critical**

3. **Rust** - REQUIRED for:
   - Performance-critical components (< 100ms response)
   - Browser automation (Obscura headless browser)
   - Media processing and encryption
   - Memory-safe systems programming
   - WebAssembly modules
   - **When memory safety or performance is paramount**

4. **IronPython** - FALLBACK for:
   - Legacy Python script integration (when no C# analog exists)
   - Data science workflows (NumPy/Pandas interop)
   - Machine learning model wrappers (when ML.NET insufficient)
   - Quick prototyping (migrating to C# after validation)
   - **ONLY when C#/F#/Rust solution is impractical**

### 1.2 Architecture Patterns (Non-Negotiable)

- **Clean Architecture**: Domain → Application → Infrastructure → API
- **Domain Models**: Keep pure, no dependencies
- **CQRS**: Use MediatR for all command/query dispatch
- **Event Sourcing**: F# for immutable audit trails
- **Outbox Pattern**: For reliable message publishing

### 1.3 Code Quality Rules

- **NO OMISSIONS**: NEVER use comments like "// ... rest of the code"
- **FULL IMPLEMENTATION**: Write complete file content unless explicitly asked for diff
- **NO STUBS**: Do not leave `NotImplementedException` or `TODO` placeholders
- **NO HARDCODED SECRETS**: Use configuration or key vaults

### 1.4 Interoperability Requirements

When changes involve cross-language calls:

1. **C# → F#**: Update C# wrapper in `*Calculator.cs` files
2. **C# → Rust**: Update CDP/WebSocket bridge in `ObscuraBrowserService`
3. **C# → IronPython**: Use `ScriptEngine` with isolated scope (security)
4. **F# → C#**: Use `[<CompiledName("...")>]` attributes
5. **Rust → C#**: Use P/Invoke or gRPC for complex scenarios

---

## 2. Self-Healing & Debugging (Error Response Protocol)

**TASK:** Root Cause Analysis & Self-Healing  
**TRIGGER:** Build/runtime error in Shadow Workspace

When I encounter an error, follow this protocol:

### 2.1 Analysis Phase

1. **Semantic Code Graph Analysis**:
   - Check if error is breaking change in `Libr4.Shared.*`
   - Analyze impact on dependent services via dependency graph
   - Review `AgentOrchestration.cs` if multi-agent involved

2. **Distributed Tracing**:
   - Check trace between C# Gateway (Port 5000) and target Service
   - Review OpenTelemetry spans in Jaeger/Grafana
   - Verify correlation IDs across service boundaries

3. **Language-Specific Validation**:
   - **F# Errors**: Verify against domain rules in `FinancialTypes.fs` or `TaxTypesWithMeasures.fs`
   - **C# Errors**: Check for null reference, async/await misuse
   - **Rust Errors**: Verify memory safety and CDP protocol compliance
   - **IronPython Errors**: Check sandbox isolation and script timeout

### 2.2 Fix Implementation

1. **Minimal Complete Fix**:
   - Generate smallest change that resolves error
   - Maintain backward compatibility
   - Update all affected language wrappers

2. **Security Scan**:
   - Check for XSS: Validate all HTML output encoding
   - Check for SQLi: Verify parameterization in all queries
   - Check for auth bypass: Validate JWT claims
   - Check for path traversal: Validate file system access

3. **Explanation**:
   - Document root cause (why original failed)
   - Document fix rationale
   - Document prevention measures

### 2.3 Validation

- Run F# property tests if financial logic affected
- Verify Obscura CDP communication if browser automation involved
- Check Shadow Workspace container isolation
- Validate Double-Entry bookkeeping balance (0.00001 tolerance)

---

## 3. Swarm Agent Development (AgentOrchestration.cs Compliance)

**TASK:** Swarm Agent Expansion  
**SCOPE:** `Libr4.IDE.AutonomousAppGeneration.Agents` namespace

When adding new Agent to 10,000-line `AgentOrchestration.cs` ecosystem:

### 3.1 Interface Compliance

```csharp
public class NewAgent : IAgent
{
    public async Task<AgentResult> ExecuteAsync(AgentContext context)
    {
        // MUST return structured AgentResult
        return new AgentResult
        {
            IsSuccess = true,
            IsApproved = true,  // Based on consensus
            Content = "Detailed execution report",
            PerformanceProfile = new PerformanceProfile { ... },
            TechDebt = new TechDebtItem { ... },
            Observability = new ObservabilityData { ... }
        };
    }
}
```

**Requirements:**
- Implement `IAgent` interface explicitly
- Return `AgentResult` with all fields populated
- Include performance metrics (duration, token usage)
- Track tech debt items
- Emit OpenTelemetry traces

### 3.2 HiveMind Integration

Detail agent participation in `ConsensusMechanism`:

- **SwarmTopology**: Hierarchical | Mesh | Adaptive | HiveMind
- **ConsensusMechanism**: Majority | Weighted | Unanimous
- **DelegationMode**: Sequential | Parallel | Hierarchical | Adaptive

Example:
```csharp
var consensus = await _debateService.ReachConsensusAsync(
    task: "Critical Security Decision",
    reviewers: new[] {
        new AgentRole { Role = "SecurityGuard", Weight = 0.95 },
        new AgentRole { Role = "RiskAnalyst", Weight = 0.85 }
    },
    options: new ConsensusOptions {
        MaxIterations = 3,
        ConsensusThreshold = 0.67  // 2/3 weighted majority
    }
);
```

### 3.3 Context Management

**MANDATORY:** Use `ContextCompressionService` to avoid token overflow

```csharp
// Compress before processing
var compressed = await _compression.CompressAgentContextAsync(
    context: fullContext,
    targetTokens: 1500  // Hard limit
);

// Use compressed context for LLM calls
// Decompress for final output if needed
```

### 3.4 Learning Pattern (EMA)

**REQUIRED:** Implement `LearningPattern` with Exponential Moving Average

```csharp
private readonly LearningPattern _learningPattern = new(
    patternName: "AgentName",
    description: "What this agent learns"
);

// Update after each execution
_learningPattern.RecordSuccess(success: result.IsSuccess);
// EMA Formula: SuccessRate = (SuccessRate * 0.9) + (success ? 0.1 : 0.0)

// Use for weighted consensus
var weight = _learningPattern.SuccessRate * expertiseLevel;
```

---

## 4. Obscura Integration (Rust + C# Interop)

**TASK:** Obscura Tool Integration  
**SCOPE:** Browser automation capabilities

When exposing new capability from Obscura to Agents:

### 4.1 Rust Layer (Obscura Crate)

1. Modify `obscura/crates/*/src/` to handle new CDP command
2. Add Chrome DevTools Protocol command handler
3. Implement memory-safe Rust logic
4. Add unit tests with `cargo test`

### 4.2 IPC/Process Layer (C#)

Update `ObscuraBrowserService`:

```csharp
public async Task<NewCapabilityResult> ExecuteNewCapabilityAsync(
    string sessionId, 
    NewCapabilityRequest request,
    CancellationToken ct)
{
    // 1. Validate session
    var session = GetSession(sessionId);
    
    // 2. Send CDP command via WebSocket
    var result = await SendCdpCommandAsync(
        sessionId,
        "NewDomain.newCommand",
        request,
        ct);
    
    // 3. Parse and return
    return ParseResult(result);
}
```

### 4.3 Agent Tooling

Create `AgentObscuraTool` wrapper:

```csharp
public async Task<SimpleResult> UseNewCapabilityForAgentAsync(
    string url,
    CapabilityOptions options)
{
    // High-level API for agents
    // Hide CDP complexity
    // Handle errors gracefully
    // Return structured result
}
```

### 4.4 IronPython Fallback (Optional)

If complex scripting needed:

```csharp
public class ObscuraPythonBridge
{
    private readonly ScriptEngine _python;
    
    public async Task ExecutePythonScriptAsync(
        string script,
        Dictionary<string, object> args)
    {
        // Sandboxed execution
        var scope = _python.CreateScope();
        foreach (var arg in args) scope.SetVariable(arg.Key, arg.Value);
        
        // Timeout protection
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        
        // Execute with security restrictions
        return _python.Execute(script, scope);
    }
}
```

---

## 5. IronPython Integration Guidelines

### 5.1 When to Use IronPython

- **Data Science**: NumPy/Pandas interop when ML.NET insufficient
- **Legacy Scripts**: Existing Python automation (migrating to C# later)
- **ML Models**: Loading Python ML models (TensorFlow, PyTorch)
- **Prototyping**: Quick validation before C# implementation

### 5.2 Security Requirements

```csharp
public class SecurePythonExecutor
{
    public object ExecuteInSandbox(string script, Dictionary<string, object> args)
    {
        var engine = Python.CreateEngine();
        var scope = engine.CreateScope();
        
        // Security: Limit imports
        var bannedModules = new[] { "os", "sys", "subprocess", "socket" };
        
        // Security: Timeout
        var source = engine.CreateScriptSourceFromString(script);
        
        // Security: Isolated AppDomain (for untrusted scripts)
        
        return source.Execute(scope);
    }
}
```

### 5.3 C# Interop Pattern

```csharp
// C# calls Python
var result = _python.Execute(@"
def calculate(data):
    return sum(data) / len(data)

calculate(args['data'])
", new Dictionary<string, object> { ["data"] = new[] { 1, 2, 3, 4, 5 } });

// Python calls C#
// Use [DllImport] or C# object exposed to Python scope
```

---

## 🔥 Thought Chain Enforcing (MANDATORY)

**For ALL complex requests, before writing code:**

> **"Before writing any code, produce a 'Plan & Impact' analysis: list which services will be affected, how the F# units of measure will change, and if there are any risks for the Shadow Workspace isolation."**

### Plan & Impact Template:

```markdown
## Plan & Impact Analysis

### Affected Services
- [ ] Gateway (YARP routing)
- [ ] IDE (Shadow Workspace)
- [ ] Payments (Escrow/F# logic)
- [ ] AI (Agent orchestration)
- [ ] Other: ___________

### Language Changes
- C#: ___________
- F#: ___________
- Rust: ___________
- IronPython: ___________

### F# Units of Measure Impact
- New measures: ___________
- Modified calculations: ___________
- C# wrapper updates needed: ___________

### Shadow Workspace Risks
- [ ] Container isolation affected
- [ ] New Docker permissions needed
- [ ] YARP routing changes
- [ ] Security boundary crossed

### Testing Requirements
- [ ] F# property tests
- [ ] Integration tests
- [ ] Security scan
- [ ] Performance benchmark
```

---

## 📋 Working with These Standards

### Using `@` Mentions

Always tag specific files:
- `@src/Shared/Libr4.Shared.Contracts/Events/IntegrationEvents.cs`
- `@src/Services/Payments/Libr4.Payments.Domain.TaxManagement.FSharp/FinancialTypes.fs`
- `@src/Services/IDE/Libr4.IDE.Infrastructure/Obscura/ObscuraBrowserService.cs`

### Terminal Integration

If build fails:
```
terminal: read last 50 lines
```

Feed output to AI with **Prompt #2 (Self-Healing)** for analysis.

---

## ✅ Compliance Checklist

Before submitting any code:

- [ ] Language affinity rules followed (C#/F#/Rust/IronPython)
- [ ] Clean Architecture patterns applied
- [ ] No "// ..." omissions in code
- [ ] Interop layers updated if cross-language
- [ ] F# Units of Measure used for financial/time
- [ ] Context compression for agent contexts
- [ ] LearningPattern EMA implemented for agents
- [ ] Security scan completed
- [ ] Plan & Impact analysis documented

---

**Version:** 1.0  
**Author:** Libr4 Architecture Team  
**Applies To:** All Windsurf/Kimi interactions with Libr4 codebase
