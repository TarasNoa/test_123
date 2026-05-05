# Libr4.IDE.AutonomousAppGeneration

Top-level **Orchestrator** that sits above every other IDE agent. It turns a
free-form user request (e.g. *"build me a banking REST API with JWT auth and
tests"* or *"FastAPI service with pytest"*) into a fully generated, built and
tested application by driving the existing IDE agents through a generate →
run-in-isolated-runtime → test → fix loop.

**Key features:**
- **True isolation**: generated code runs in Docker containers (or WSL/Hyper-V),
  never on the host.
- **Any tech stack**: generated apps can be Python, Node, Go, Rust, Java, C#,
  Ruby, PHP, … — whatever the planner picks.
- **Workspace pooling**: multiple shadow workspaces coexist in a single
  long-living VM.
- **Bidirectional sync**: IDE ↔ runtime file changes are instant via bind-mount
  + FileSystemWatcher.

## How it works

```
┌──────────────────────┐    1. Plan (tech stack, phases, agents)
│  User request (text) │───────────────────────────────────────────────────┐
└──────────┬───────────┘                                                   │
           │                                                               ▼
           │                                                    ┌────────────────────┐
           │                                                    │ LlmAppPlannerService│
           │                                                    │  (OpenRouter free) │
           │                                                    └─────────┬──────────┘
           ▼                                                              │
┌──────────────────────────────────────────────────────────────────────────┴─────┐
│                         AppGenerationOrchestrator                              │
│                                                                                │
│   2. Generate initial files  ──► LlmCodeGenerationService                      │
│   3. PrepareShadowWorkspace  ──► ProcessShadowExecutionService                 │
│                                                                                │
│   ┌──────────────────── iteration loop (until pass or budget) ────────────┐   │
│   │ 4. Run `dotnet build` + `dotnet test` in shadow workspace            │   │
│   │ 5. If failed:                                                         │   │
│   │      • LlmErrorAnalysisService → ErrorReports                         │   │
│   │      • LlmCodeGenerationService.ApplyFixes → new files                │   │
│   │      • Update workspace, retry                                        │   │
│   │ 6. If succeeded → MarkCompleted                                       │   │
│   └───────────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────────────────────┘
```

Key building blocks (all in `Libr4.IDE.Domain.AutonomousAppGeneration`):

| Class                                  | Role                                                            |
| -------------------------------------- | --------------------------------------------------------------- |
| `AppGenerationOrchestrator`            | Aggregate root; holds plan, iterations, files, status.          |
| `GenerationPlan` / `GenerationPhase`   | Plan produced by the planner LLM.                               |
| `TechStack`                            | Chosen languages/frameworks/databases.                          |
| `AgentAssignment`                      | Which existing IDE agent handles which phase.                   |
| `IterationCycle`                       | One generate/test/fix iteration.                                |
| `ExecutionResult`, `ConsoleLogEntry`   | Outcome of a shadow-workspace run (stdout/stderr captured).     |
| `ErrorReport`                          | Structured error extracted by `SemanticBlame`-style analysis.   |

Services (all in the `Libr4.IDE.AutonomousAppGeneration` project):

| Interface                  | Default implementation                  | Purpose                                                          |
| -------------------------- | --------------------------------------- | ---------------------------------------------------------------- |
| `IAppPlannerService`       | `LlmAppPlannerService`                  | Ask the LLM for the plan (tech stack, phases, required agents).  |
| `ICodeGenerationService`   | `LlmCodeGenerationService`              | Generate initial project files + apply fixes later.              |
| `IShadowExecutionService`  | `ProcessShadowExecutionService`         | Materialize files, run `dotnet build` + `dotnet test`.           |
| `IErrorAnalysisService`    | `LlmErrorAnalysisService`               | Turn stderr into structured `ErrorReport`s.                      |
| `IAppGenerationRepository` | `InMemoryAppGenerationRepository`       | Store orchestrator aggregates (swap for EF later).               |

## Agents the orchestrator can dispatch

The planner can reference any existing IDE agent. The following names are
recognised out-of-the-box:

- `TaskDecompositionAgent`
- `CodeGenerationAgent`
- `ArchitecturalGuardrailsAgent`
- `CodeReviewAgent`
- `SecurityTestingAgent`
- `SemanticBlameAgent`
- `WebSearchAgent`
- `HackerAgent`
- `AIWorkflowAutomationAgent`

## Running locally

1. Set the OpenRouter API key (free models recommended, e.g.
   `deepseek/deepseek-chat-v3.1:free`):

   ```powershell
   $env:AI__OpenRouter__ApiKey = "sk-or-v1-..."
   ```

2. Start the host:

   ```powershell
   dotnet run --project src/Services/IDE/Libr4.IDE.AutonomousAppGeneration.Host
   ```

3. Open Swagger at <http://localhost:5199/swagger> or call the API directly:

   ```http
   POST http://localhost:5199/api/ide/app-generation/start
   Content-Type: application/json

   {
     "userRequest": "Build a banking REST API with accounts, transfers and JWT auth",
     "maxIterations": 8
   }
   ```

4. Fetch the full report:

   ```http
   GET http://localhost:5199/api/ide/app-generation/{id}
   ```

## Extensibility

- Swap `ProcessShadowExecutionService` for a Docker-backed version without
  touching the orchestrator — just register a different
  `IShadowExecutionService` implementation.
- Replace `InMemoryAppGenerationRepository` with an EF Core implementation
  when you need persistence and querying across runs.
- The planner, fixer and error analyser are all LLM-driven but include
  deterministic fallbacks so the orchestrator never deadlocks when a free
  OpenRouter model misbehaves.
