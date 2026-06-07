# Libr4 Terminal Agent Platform — Master Implementation Plan

> **Версия:** 2026-06-06  
> **Статус:** living document — единый источник правды по всем нереализованным работам  
> **Охват:** Claude Code, Qwen Code, Kimi CLI, OpenAI Codex, Hermes Agent, Gemini CLI, Grok CLI, DeepSeek-Coder, Obscura Browser Plane  
> **Принцип:** только production-grade реализации. Упрощённые обёртки и «сделаем потом» не допускаются.

---

## Легенда статусов

| Маркер | Значение |
|--------|----------|
| `[x]` | Реализовано и подключено к пайплайну |
| `[~]` | Частично: код есть, но не полный контракт / не wired / stub / Null-реализация |
| `[ ]` | Не реализовано |

### Сводка прогресса (2026-06-06)

| Фаза | Статус | Готово | Частично | Не начато |
|------|--------|--------|----------|-----------|
| **0** Runtime Foundation | `[x]` | 0.0–0.11 | — | — |
| **1** Orchestration | `[x]` | 1.1–1.8 | — | — |
| **2** Context Engine | `[x]` | **2.1–2.8** ✅ | — | — |
| **3** Memory Hermes | `[x]` | 3.1–3.12 | — | — |
| **4** Verify Plane | `[x]` | 4.1–4.5 | — | — |
| **O** Obscura | `[x]` | O1.1–O1.6, O2.1–O2.6, O3.1–O3.5, O4 | — | — |
| **5–6** Platform | `[x]` | 5.1–5.13, 6.1–6.4 | — | — |
| **7** Command Surface | `[x]` | 7.1–7.11 | — | — |

**Последнее закрыто:** Backend Golden Stack migration (Waves 1–7) — см. [`BACKEND_LANGUAGE_MIGRATION_CHECKLIST.md`](./BACKEND_LANGUAGE_MIGRATION_CHECKLIST.md).  
**Сейчас:** Agent Platform Golden Stack migration complete. Следующий scope — Wave 6 C++ muscle (opt-in, по критериям «Rust vs C++»), perf gates, или product items вне плана.

---

## Целевая архитектура

```text
User Request
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ Manifest → Plan → Generate → Test → Verify → Repair → Ship       │
├─────────────────────────────────────────────────────────────────┤
│ Agent Runtime          │ Tools, Hooks, Subagents, Compaction      │
│ Context Engine         │ RepoGraph, FIM, LIBR4.md, Skills       │
│ Memory / Learning      │ Hermes Loop L0–L4, Playbook, Crystallize │
│ Execution Plane        │ Shadow Git, Sandbox, ExecPolicy          │
│ Obscura Browser Plane  │ Sessions, Verify, Computer, Evidence     │
│ Observability          │ Rollout, NDJSON, FTS, Dashboard          │
│ Provider Mesh          │ OpenRouter, DMR, Batch API, Routing      │
└─────────────────────────────────────────────────────────────────┘
```

---

## Backend Golden Stack (C# · F# · Rust · C++)

> **Чеклист:** [`BACKEND_LANGUAGE_MIGRATION_CHECKLIST.md`](./BACKEND_LANGUAGE_MIGRATION_CHECKLIST.md) — **Backend Golden Stack migration complete (Waves 1–6, 2026-06-06).**

| Слой | Роль | Agent Platform | Статус |
|------|------|----------------|--------|
| **C#** | Skeleton — API, DI, Host, persistence, LLM HTTP | `Libr4.IDE.AutonomousAppGeneration`, `*.Api`, `*.Host` | `[—]` by design |
| **F#** | Brain — алгоритмы, scoring, parsing, state machines | `Libr4.IDE.AutonomousAppGeneration.Algorithms.FSharp`, `Libr4.IDE.Domain.Algorithms` | `[x]` Waves 1–2 |
| **Rust** | **Muscle (default)** — sandbox, delegation, fast-context, rollout, gateway, embeddings gRPC | `rust/libr4-*`, `obscura/` | `[x]` Wave 3 |
| **C++** | **Muscle (opt-in)** — tree-sitter, ONNX ORT EP, libclang repo analysis | `native/cpp/` (`libr4_tree_sitter`, `libr4_ort_ep`, `libr4_libclang`) | `[x]` Wave 6 complete |

**Rust vs C++:** новый native код → **Rust**, если нет веской причины. C++ — когда нужен низкоуровневый доступ или зрелая C/C++ экосистема; всё равно через C# bridge + smoke tests (как Wave 3).

**Тесты:** `Algorithms.FSharp.Tests` 35/35 · `IDE.Domain.Algorithms.Tests` 8/8 · Rust/C++ bridges via `Libr4.IntegrationTests` (skip-if-unavailable smoke).

**CI:** `ci.yml` — `cargo build` rust workspace + sandbox + embeddings; C++ cmake (tree-sitter + ORT + libclang); job `rust-embeddings`; MSBuild `Libr4.RustNative.targets` + `Libr4.CppNative.targets`.

**Docker stack (agent profile):** `autonomous-app-generation-host` → `Memory:Embeddings:Provider=grpc` → `rust-embeddings:50061` (alt: `ort-cpp` with local ONNX).

---

## Текущий инвентарь (что уже есть)

### Agent Runtime — базовый слой

| Компонент | Статус | Путь / примечание |
|-----------|--------|-------------------|
| `AgentSession` turn loop | `[x]` | `AgentRuntime/Core/AgentSession.cs` — SQLite persistence, resume, rollout, NDJSON |
| Tool registry (19 tools) | `[x]` | +apply_patch, list_directory; schema validation, path gating |
| `ToolCallRecovery` | `[x]` | raw coercion → nudge → strict schema → BoilerplateFallback |
| `BoilerplateRegistry` | `[x]` | manage.py, wsgi, docker-compose templates |
| `SemanticContextCompactor` | `[x]` | 2.7: heuristic/LLM summary + truncate fallback |
| `ToolResultBudgetCompactor` | `[x]` | fallback внутри semantic compaction |
| `RepairPlaybookService` | `[x]` | 3.5: SQLite `SqliteRepairPlaybookStore`, signature prefetch |
| `FeatureDependencyGrouper` | `[x]` | Django/FastAPI/Spring buckets |
| `IncrementalFileBatchGrouper` | `[x]` | feature batches size 6 |
| `RunTestsTool` | `[x]` | gated by `AllowBashDuringGeneration` |
| `BuildErrorCategoryClassifier` | `[x]` | Runtime category |
| `SubagentTool` | `[~]` | foreground only, generic `agent {role, task}` |
| `DefaultPermissionGate` | `[x]` | Plan/AcceptEdits/Bypass/Dangerous + per-run store |
| `AgentToolAuditHook` + Rollout | `[x]` | rollout.jsonl + exec-audit.jsonl + permission audit |
| `ShadowAgentRepairService` | `[x]` | resume by runId, wired в iteration loop |
| `AgentRuntimeIncrementalGenerator` | `[~]` | opt-in via `UseAgentRuntimeGeneration` |
| AgentRuntime integration tests | `[x]` | 33/33 passed (Phase 0 coverage) |

### Autonomous Generation — оркестрация

| Компонент | Статус | Путь / примечание |
|-----------|--------|-------------------|
| `StartAppGenerationCommandHandler` | `[x]` | partial class split (`Handler.cs` + `Handler.Run.cs`); pipeline milestones default on |
| Pipeline stages (plan prefix + post-plan) | `[x]` | Idempotency → Ship; `UsePipelineRunnerForFullHandle: true` |
| `LlmSecurityReviewGateService` | `[x]` | LLM security вместо regex |
| `BenchmarkExecutionPathPolicy` | `[x]` | Required/Optional/Disabled stages |
| `ClaudeCodeStyleRepairService` | `[x]` | primary repair в iteration loop (production DI) |
| `SurgicalPatchEngine` | `[x]` | 3-way merge + integration test |
| `SqliteHermesMemoryStore` | `[x]` | 3.1: SQLite L0–L4, retention 90d episodic |
| `HermesMemoryManager` | `[x]` | 3.2: prefetch, tool ingest, pre-compact hook |
| `MemoryWriteTool` / `MemoryReadTool` | `[x]` | 3.3: scope user/project/run, rollout audit |
| `IPostRunExtractor` | `[x]` | 3.6: rollout + errors → Hermes lessons, hosted queue |
| `InMemoryMemoryStore` | `[x]` | obsolete, tests only; production → `HermesBackedMemoryStore` |
| `AutonomousMemoryConsolidationService` | `[x]` | заменён на `IDreamConsolidationService` в queue/background |
| `CognitiveMemorySystem` L0–L4 | `[x]` | `HermesCognitiveMemoryBridge` + decorator + prefetch |
| `QdrantVectorMemoryStore` | `[x]` | wired via `AddQdrantSync` (optional L2 index) |
| `ContextPackBuilder` | `[x]` | RepoGraph-ordered related_files section |
| `ActivateSkillTool` + skill manifest | `[x]` | 2.5 progressive disclosure, −80% default prompt tokens |
| `ContextFragmentRepairAssembler` | `[x]` | 2.6 bounded repair fragments + provenance markers |
| MCP `browser.smoke` / `browser.auth` | `[~]` | Node `browser-mcp-server`, не Obscura |
| `IAgentEventEmitter` browser events | `[x]` | `BrowserToolEventHook` → emitter + rollout + NDJSON |
| `GetBuildDiagnosticsDashboard` | `[x]` | build KPI + verify/obscura artifacts + stack filters |
| 42 stack skills | `[x]` | manifest в prompt + `activate_skill` on demand (2.5) |
| `UseFeatureScopedGeneration` | `[x]` | appsettings flag |

### Obscura — browser plane

| Компонент | Статус | Путь / примечание |
|-----------|--------|-------------------|
| `ObscuraEndpoints` (IDE.Api) | `[x]` | 9 endpoints → MediatR handlers |
| `ObscuraBrowserServiceAdapter` | `[x]` | стабильный sessionId, трекинг сессий |
| `browser_automation.proto` + gRPC client | `[x]` | `Application/Obscura/` |
| docker-compose `libr4-obscura` | `[x]` | :9222 CDP |
| `IAgentObscuraTool` / `ISubagentObscuraIntegration` | `[x]` | полные реализации + `AddObscuraBrowserPlane()` |
| Native `browser_*` Agent Runtime tools | `[x]` | 13 tools + `ObscuraBrowserToolFacade` |
| Obscura в Autonomous Host DI | `[x]` | `AddObscuraBrowserPlane` in Host Program.cs |
| `docs/ObscuraIntegration.md` | `[x]` | design doc (целевой контракт) |

---

## ФАЗА 0 — Runtime Foundation

> **Цель:** production Agent Runtime как самостоятельный bounded context  
> **Источники:** Claude Code, Codex, Qwen  
> **Оценка:** 8–12 недель

### 0.1 AgentSession Persistence & Resume

- [x] SQLite schema `agent_sessions` (sessionId, runId, subagentId, model, status, createdAt, lastStepAt, tokenBudget, costUsd)
- [x] SQLite schema `agent_messages` (id, sessionId, role, content, toolCallsJson, stepNumber, timestamp)
- [x] SQLite schema `agent_tool_calls` (id, sessionId, toolName, inputJson, outputJson, success, durationMs, startedAt)
- [x] `IAgentSessionStore` + `SqliteAgentSessionStore`
- [x] `AgentSession.ResumeAsync(sessionId)` — replay messages + restore tool state + file cache
- [x] `AgentSession.CheckpointAsync()` — snapshot messages + workspace file hashes
- [x] `AgentSession.RewindAsync(checkpointId)` — Codex thread rollback
- [x] Migration runner (EF Core или raw SQL) для session DB — `AgentSessionSchemaMigrator` hosted service
- [x] Integration test: run 5 turns → crash → resume → continue
- [x] Wire resume в `ShadowAgentRepairService` для `resumeFixOnly` / iteration repair path

### 0.2 Permission Modes (Claude Code)

- [x] Enum `AgentPermissionMode`: Plan, AcceptEdits, BypassPermissions, Dangerous
- [x] Per-run permission в `AgentSessionState` + `IAgentRunPermissionStore`
- [x] `IPermissionGate` расширить: check mode + tool + path
- [x] Plan mode: block write_file, edit_file, bash; allow read/grep/glob
- [x] AcceptEdits: prompt user / auto-accept policy per tool category + `POST .../permission-mode/resolve`
- [x] API endpoint: `PATCH /api/ide/app-generation/{runId}/permission-mode` + `GET .../permission-mode`
- [x] Dashboard UI hook для permission prompts (`PermissionPromptModal` + `SessionDetail` polling/WS)
- [x] Audit log permission decisions в rollout

### 0.3 Tool JSON Schema Strict Validation

- [x] JSON Schema per tool в `ToolSchemaRegistry` (не только Description string)
- [x] `ToolInputValidator` — validate before execute, reject with structured error
- [x] Расширить `ToolCallRecovery` stage 4: schema-coerce с field-level hints
- [x] Regression test: `manage.py` invalid JSON → recovery → valid write
- [x] Regression test: raw Python code block → coercion → write_file

### 0.4 apply_patch (Codex-grade)

- [x] `ApplyPatchTool` — unified diff format input
- [x] `UnifiedDiffParser` — parse @@ hunks, context lines
- [x] `PatchApplicator` — exact match apply
- [x] `FuzzyPatchApplicator` — Levenshtein fallback с confidence threshold
- [x] `ThreeWayMergePatchApplicator` — base/ours/theirs при conflict
- [x] Reject on ambiguity — return structured conflict report to agent
- [x] `PatchAttemptRecorder` — every attempt → `patches/` dir per runId
- [x] Integration test: unified diff apply success
- [x] Wire `ApplyPatchTool` в Repair subagent toolset (via `AgentToolRegistry` DI)
- [x] Wire `ApplyPatchTool` в `ClaudeCodeStyleRepairService` as primary (`UseApplyPatchRepair`, unified diff before JSON fallback)

### 0.5 Surgical Repair — полная интеграция

- [x] Убрать `NullClaudeCodeStyleRepairService` из `StartAppGenerationCommandHandler` constructor path *(test ctor принимает optional override)*
- [x] `ClaudeCodeStyleRepairService` — primary repair для iteration loop
- [x] `SurgicalFixerOutputParser` — handle markdown fences, multiple edits, malformed blocks
- [x] `SurgicalPatchEngine` — 3-way merge при partial apply
- [x] Repair prompt: build log fragments + affected files + FIM context *(2.3 FIM + 2.6 ContextFragments)*
- [x] KPI: `patchesApplied` counter в `RecoveryEfficiencyReportDto`
- [x] Integration test: Java compile error → surgical patch → rebuild pass

### 0.6 ExecPolicy Engine (Codex)

- [x] `execpolicy.yaml` schema: rules[] with action, pattern, decision (allow/prompt/forbid)
- [x] `IExecPolicyEngine` + `YamlExecPolicyEngine`
- [x] Bash tool: evaluate command against policy before spawn (`ExecPolicyToolHook`)
- [x] Browser tools: evaluate URL/action against policy (`ObscuraExecPolicyToolHook`, O3)
- [x] `exec-audit.jsonl` per runId
- [x] PreToolUse hook integration
- [x] Deny `rm -rf`, `curl | bash`, outbound exfil patterns by default
- [x] Stack-specific allowlists: `mvn`, `npm`, `pip`, `python manage.py`
- [x] Integration test: forbidden command → policy_denied

### 0.7 Hooks Pipeline (Claude + Gemini + Grok)

- [x] `AgentToolHookPipeline` — ordered hook execution (Pre/Post tool)
- [x] Hook kinds: SessionStart, SessionEnd, PreCompact, PostCompact + `IAgentLifecycleHookRunner`
- [x] `ConfigurableScriptHook` — run external script with JSON stdin/stdout
- [x] `HookContext` — session, tool, run, workspace paths
- [x] Hook timeout + failure policy (block / warn / ignore)
- [x] Register built-in hooks: ExecPolicy, Audit, MemoryPrefetch, EvidenceCapture (`MemoryPrefetchToolHook`, `EvidenceCaptureToolHook`, `BrowserToolEventHook`)
- [x] `appsettings.json` → `AgentHooks[]` configuration
- [x] Integration test: path gating blocks `/etc/passwd`

### 0.8 Rollout Recorder (Codex)

- [x] `IRolloutRecorder` interface
- [x] `rollout.jsonl` per runId — every step, tool, token, cost, timing
- [x] `rollout.db` SQLite — indexed by runId, toolName, stepNumber, timestamp
- [x] FTS5 index on tool output text
- [x] `RolloutReplayService` — replay session from jsonl
- [x] `RolloutSearchService` — FTS query across all runs
- [x] Wire в `AgentSession` — record every turn automatically
- [x] API: `GET /api/ide/app-generation/{runId}/rollout`
- [x] API: `GET /api/ide/app-generation/rollout/search?q=manage.py`
- [x] Dashboard timeline component consuming rollout *(IDE BottomPanel Timeline + SSE `runEvents.ts`)*

### 0.9 NDJSON Event Stream (Grok + Gemini)

- [x] NDJSON events: `step_start`, `reasoning`, `tool_use`, `step_finish`, `error`
- [x] `NdjsonEventWriter` → `.logs/runs/{runId}/events.jsonl`
- [x] Tool timing: startedAt, finishedAt, durationMs per tool_use event
- [x] `media[]` attachment на tool_use (patch paths)
- [x] SSE endpoint: `GET /api/ide/app-generation/{runId}/events/stream`
- [x] Wire `AgentEventWebSocketHandler` — Browser* + runtime NDJSON via `AgentRuntimeWebSocketBridge`
- [x] PowerShell runner scripts: `.logs/parse-run-events.ps1`
- [x] Integration test: event sequence integrity after 3-turn session

### 0.10 list_directory + Path Gating (Qwen)

- [x] `ListDirectoryTool` — tree output, depth param, symlink policy
- [x] `IWorkspacePathValidator` — canonical resolution, workspace root binding
- [x] Deny `..` escape, absolute paths outside workspace, symlink escape
- [x] Deny glob patterns from `DeniedPathPatterns` config
- [x] Audit every denied access
- [x] Wire во все file tools: read, write, edit, grep, glob, list_directory, apply_patch
- [x] Integration test: path traversal attempts blocked

### 0.11 Reasoning Channel Separation (Qwen)

- [x] Parse `<think>` / `<thinking>` blocks из LLM response
- [x] Store reasoning separately from tool-use content
- [x] NDJSON event type `reasoning` (не смешивать с `text`)
- [x] Option: strip reasoning before tool parser
- [x] Option: include reasoning in rollout for debug, exclude from next turn context
- [x] Config: `AgentRuntime:IncludeReasoningInContext`

---

## ФАЗА 1 — Orchestration & Subagents

> **Источники:** Kimi CLI, Grok CLI, Claude Code  
> **Оценка:** 6–10 недель

### 1.1 Builtin Prompt Variables (Kimi)

- [x] `IBuiltinPromptVarResolver` — resolve at prompt build time
- [x] Variables: `LIBR4_STACK`, `LIBR4_MANIFEST_FILES`, `LIBR4_WORKSPACE_LS`, `LIBR4_BUILD_LOG`, `LIBR4_ERRORS`, `LIBR4_RUN_ID`, `LIBR4_STAGE`, `LIBR4_REPAIR_ATTEMPT`
- [x] `{{LIBR4_*}}` substitution в `AgentPromptBuilder` system prompt
- [x] Stage-specific injection: Planning vs Generating vs Repairing vs Verify
- [x] Workspace LS: actual `list_directory` snapshot bound to var
- [x] Build log: last N lines from shadow execution
- [x] Errors: structured last_errors from orchestrator
- [x] Test: var present in prompt snapshot per stage

### 1.2 YAML Agent Specs (Kimi + Codex)

- [x] Schema `*.agent.yaml`: name, extend, model, maxTurns, maxTokens, toolset[], instruction, permissions
- [x] `AgentSpecLoader` — load from `Agents/Subagents/`
- [x] `extend:` inheritance resolver (child overrides parent)
- [x] Reserved names: explore, implementer, verify, repair, computer
- [x] Custom agents from `appsettings.json` → `subAgents[]`
- [x] `AgentSpecRegistry` — runtime lookup by name
- [x] Wire в `SubagentTool` → spawn by spec name, не freeform role string
- [x] Wire в `MultiAgentIncrementalManifest` → `@subagent verify` syntax (Gemini)
- [x] Example specs: `explore.agent.yaml`, `implementer.agent.yaml`, `verify.agent.yaml`, `repair.agent.yaml`, `computer.agent.yaml`
- [x] Integration test: spawn verify agent → restricted toolset enforced

### 1.3 Flow Engine (Kimi)

- [x] Flow definition format: Mermaid DAG или YAML с nodes/edges
- [x] `/flow:calorie-vision` → `Flows/calorie-django-solidjs.flow.yaml`
- [x] Node types: stage, gate, parallel, retry, escalate
- [x] Precondition evaluator: files exist, tests pass, verify passed
- [x] Failure routing: retry/skip/escalate/abort per edge
- [x] `IFlowEngine` + `YamlFlowEngine`
- [x] Wire flows в `StartAppGenerationCommandHandler` as optional orchestration mode
- [x] Dashboard: render flow progress с current node highlighted
- [x] Flows: `calorie-django-solidjs`, `banking-java-react`, `nextjs-shop`

### 1.4 SubagentStore per Run (Kimi)

- [x] Directory: `.logs/runs/{runId}/subagents/{subagentId}/`
- [x] Files: `spec.yaml`, `messages.jsonl`, `output.md`, `status.json`
- [x] `ISubagentStore` — create, update, complete, fail
- [x] Wire в `SubagentTool` — persist every subagent invocation
- [x] API: `GET /api/ide/app-generation/{runId}/subagents`
- [x] Dashboard: subagent list с status + output preview (`SubagentsPanel`, `BackgroundAgentsWidget`)

### 1.5 task / delegate Split (Grok)

- [x] Rename/refactor `SubagentTool` → `TaskTool` (foreground, blocking, read+write)
- [x] New `DelegateTool` (background, detached, read-only explore only)
- [x] `IDelegationManager` — spawn background process/session
- [x] Storage: `.logs/runs/{runId}/delegations/{id}.json` + `{id}.md`
- [x] `DelegationNotification` — inject into parent AgentSession on completion
- [x] `delegation_list`, `delegation_read` meta-tools
- [x] Deny nested delegation (`DELEGATE_BACKGROUND_CHILD` env equivalent)
- [x] Human-readable delegation IDs: `brisk-blue-fox` style
- [x] Integration test: delegate explore while implementer writes files

### 1.6 DMail Handoff (Kimi)

- [x] `IDMailBus` — async message between subagents
- [x] `DMailMessage`: from, to, runId, payload, ackRequired, timestamp
- [x] Storage: `.logs/runs/{runId}/dmail/` или SQLite `dmail_messages`
- [x] `dmail_send`, `dmail_read`, `dmail_ack` tools
- [x] Wire в feature batch handoff: backend subagent → frontend subagent
- [x] Integration test: two feature subagents exchange context via DMail

### 1.7 Pipeline Stage Migration (strangler-fig)

- [x] `GenerationStage` — file generation (multi-agent + agent runtime)
- [x] `SecurityReviewStage`
- [x] `ReviewGate2Stage`
- [x] `ConsistencyCheckStage`
- [x] `StartupBuildStage`
- [x] `RepairLoopStage` — iteration loop as stage
- [~] `VerifyStage` — см. Фаза 4 + Obscura
- [x] `ShipStage` — export, report, memory consolidation
- [x] `UsePipelineRunnerForFullHandle: true` — migrate off monolithic Handle
- [x] Каждый stage: unit test isolated
- [x] Integration test: full pipeline via stages end-to-end (`FullPipelineStagesE2ETests`)

### 1.8 Slash Commands

- [x] `ISlashCommandRegistry`
- [x] Commands: `/verify`, `/compact`, `/rewind`, `/flow:{name}`, `/delegate`, `/memory-search`
- [x] Parse в user request prefix → route to handler
- [x] API: slash command execution on active run

---

## ФАЗА 2 — Context Engine

> **Источники:** DeepSeek-Coder, Gemini CLI, Kimi, Codex  
> **Оценка:** 6–8 недель

### 2.1 RepoGraphBuilder (DeepSeek)

- [x] `IRepoGraphBuilder` — parse dependencies per language
- [x] Python: `ast` module import analysis
- [x] TypeScript/JavaScript: import/export analysis
- [x] C#: using + csproj ProjectReference
- [x] Java: package import analysis
- [x] Go: import path analysis
- [x] `TopologicalSort` → generation order (dependencies first)
- [x] Reverse topo → repair order (dependents first)
- [x] Integration в `FeatureDependencyGrouper` — refine buckets with actual deps
- [x] Integration в `IncrementalFileBatchGrouper` — respect dep order within batch
- [x] Unit test: Django app deps → models before views before urls

### 2.2 Multi-File Context Format (DeepSeek)

- [x] `RepoContextFormatter` — `#path/to/file.py\n{content}\n\n` format
- [x] Wire в `ContextPackBuilder` — include related files in dep order
- [x] Budget: priority eviction — keep highest-dep-ranked files
- [x] Stage budgets: Planning 9K, Generating 16K, Repairing 16K, Verify 8K
- [x] Config: `ContextPack:UseRepoGraphOrdering: true`

### 2.3 FIM Infilling Mode (DeepSeek)

- [x] `IFimPromptBuilder` — prefix + `<|fim_hole|>` + suffix format
- [x] Detect repair scenario: existing file + error location → FIM instead of full rewrite
- [x] `FimGenerationMode` в `AgentSession` — activate for repair subagent
- [x] `ClaudeCodeStyleRepairService` — use FIM when file > 200 lines
- [x] `SurgicalPatchEngine` — fallback when FIM output invalid
- [x] Integration test: large views.py repair → FIM → compile pass

### 2.4 JIT LIBR4.md (Gemini)

- [x] `LIBR4.md` в repo root + per-directory `LIBR4.md`
- [x] `LIBR4.override.md` per directory (Grok AGENTS.override semantics)
- [x] `IContextInjector` — on tool access to path, inject nearest LIBR4.md
- [x] Merge rule: root → ... → cwd, override wins per directory
- [x] Wire в `AgentPromptBuilder` per-turn dynamic section
- [x] CalorieVision: `backend/LIBR4.override.md`, `frontend/LIBR4.override.md`
- [x] Token budget for JIT injection: max 2K chars per injection

### 2.5 activate_skill Progressive Disclosure (Gemini)

- [x] `ActivateSkillTool` — agent requests skill by name
- [x] `ISkillConsentGate` — first activation per skill per run requires consent (configurable auto-approve)
- [x] Load skill content from `Skills/{name}/SKILL.md` on activation only
- [x] Track activated skills per session — don't re-inject
- [x] Remove 42 skills from default system prompt — only manifest list (name + one-liner)
- [x] Expected: −80% skill tokens in default prompt
- [x] `SkillActivationAudit` в rollout
- [x] Integration test: agent activates `django-rest-framework` mid-run (alias → `python-django`)

### 2.6 ContextualUserFragment Manager (Codex)

- [x] `IContextFragmentManager` — bounded fragments with hard cap
- [x] Fragment types: build_log, error_report, file_excerpt, design_artifact, verify_evidence
- [x] Priority + eviction when total chars exceed cap
- [x] Provenance markers: `[fragment:build_log:attempt=3]`
- [x] Wire в repair prompt assembly
- [x] Config: `ContextFragments:MaxTotalChars`, per-type caps

### 2.7 Semantic Compaction (Kimi + Claude)

- [x] `ISemanticCompactor` — LLM summarizer (не truncate)
- [x] Output schema: `decisions[]`, `files_touched[]`, `open_issues[]`, `next_actions[]`, `errors_resolved[]`
- [x] Trigger: token budget > 80% или PreCompact hook
- [x] Preserve: last 3 tool results verbatim, all errors, manifest paths
- [x] Replace older turns with compact summary
- [x] `CompactionAudit` в rollout — before/after token counts
- [x] Integration test: 50-turn session → compact → continue without quality loss

### 2.8 Instruction Template Standardization (DeepSeek)

- [x] Standardize all agent prompts: `### Instruction:\n...\n### Response:\n`
- [x] Per-role system prompts: implementer, explore, verify, repair, computer
- [x] `IPromptTemplateRegistry` — versioned templates
- [x] A/B test harness for prompt variants (`PromptVariantSelector` + `AbVariants` config)

---

## ФАЗА 3 — Memory & Learning Loop (Hermes)

> **Источники:** Hermes Agent  
> **Оценка:** 8–12 недель

### 3.1 SqliteHermesMemoryStore

- [x] SQLite schema: `memories` (id, runId, userId, kind, stage, key, summary, payloadJson, tokens, score, createdAt)
- [x] Memory kinds: L0_episodic, L1_procedural, L2_semantic, L3_strategic, L4_meta
- [x] `IHermesMemoryStore` + `SqliteHermesMemoryStore`
- [x] Replace `InMemoryMemoryStore` as default в DI (`AddHermesMemory`)
- [x] Migration from InMemory on first run (none needed, fresh start)
- [x] Retention policy: episodic 90 days, semantic/procedural/strategic/meta permanent

### 3.2 HermesMemoryManager

- [x] `IHermesMemoryManager`
- [x] `PrefetchBeforeTurnAsync` — top-K relevant memories inject into prompt
- [x] `SyncAfterToolAsync` — ingest tool result if matches ingest rules
- [x] `OnPreCompactAsync` — consolidate before semantic compaction (`HermesMemoryLifecycleHook`)
- [x] Relevance scoring: fingerprint match, keyword, recency, success_rate (via `HermesMemoryScoring`)
- [x] Wire в `AgentSession` turn loop

### 3.3 memory_write / memory_read Tools

- [x] `MemoryWriteTool` — agent explicitly saves lesson/pattern
- [x] `MemoryReadTool` — agent queries memory by keyword/kind
- [x] Scope: user / project / run (`HermesMemoryScopeResolver`)
- [x] Size limits + validation (`MemoryToolOptions`)
- [x] Audit в rollout (`RecordMemoryOperationAsync` → `memory_operation`)

### 3.4 Memory Nudges в AgentSession

- [x] After memory prefetch: inject `## relevant_memory` section
- [x] Format: `[L2_semantic] key: summary (score: 0.87, reason: L2_semantic;keyword_match)`
- [x] Cap: max 5 nudges per turn (`MaxNudgesPerTurn`)

### 3.5 RepairPlaybook Persistence

- [x] SQLite `repair_playbook` (errorSignature, stackPattern, fixPattern, successCount, failCount, lastUsedAt)
- [x] `IRepairPlaybookStore` — replace in-memory `RepairPlaybookService` backing
- [x] Error signature: hash of (errorType + filePattern + message keywords)
- [x] On repair success: increment successCount
- [x] On repair fail: increment failCount, decay score
- [x] Prefetch matching playbook entry before repair turn
- [x] Integration test: same error twice → second repair uses playbook

### 3.6 Post-Run Extractor

- [x] `IPostRunExtractor` — LLM extracts lessons after run completes
- [x] Input: rollout.jsonl + final status + errors
- [x] Output: structured lessons → memory ingest
- [x] Run on BOTH success and failure
- [x] `AutonomousMemoryConsolidationService` — extend for failure analysis (`PostRunExtractionBackgroundService` E2E test)
- [x] Hosted trigger: on run Complete/Failed event

### 3.7 Skill Crystallization

- [x] `ISkillCrystallizer` — after N successful repairs of same pattern
- [x] Generate `.libr4/skills/crystallized/{error-hash}.md` *(SHA256-16 hex filename)*
- [x] Include: trigger conditions, fix steps, example diff
- [x] Register in skill manifest automatically
- [x] Threshold config: `CrystallizeAfterSuccessCount: 3`
- [x] Human review gate (optional): queue for approval before activation

### 3.8 FTS5 Session Search

- [x] FTS5 index on rollout tool outputs + memory summaries
- [x] `ISessionSearchService` — query: "manage.py invalid json"
- [x] API: `GET /api/ide/memory/search?q=...`
- [x] CLI/script: search past fixes from PowerShell runner (`scripts/Search-SessionMemory.ps1`)

### 3.9 USER.profile.md (Hermes / Honcho-lite)

- [x] Per `userId`: `.libr4/users/{userId}/USER.profile.md`
- [x] Auto-update: preferred stacks, recurring failures, successful patterns
- [x] Inject into planning prompt (`PlanGenerationStage` + legacy handler path)
- [x] Privacy: user data isolation (sanitized per-user directory, `TenantId`/`TriggerActor` scope)

### 3.10 Dream Consolidation

- [x] Nightly hosted job: merge episodic → semantic (`DreamConsolidationNightlyHostedService`)
- [x] Prune stale memories (score < threshold, age > retention)
- [x] Deduplicate via minhash on summary text (+ token Jaccard fallback)
- [x] Report: consolidation stats в admin dashboard (`GET /api/ide/memory/consolidation/stats`, manual `POST .../run`)

### 3.11 Qdrant Sync (optional vector layer)

- [x] Wire `QdrantVectorMemoryStore` as L2 semantic index (`Memory/Qdrant/`, decorator `QdrantSyncHermesMemoryStore`, startup backfill)
- [x] Embed memory summaries via Ollama/local embeddings (`LocalEmbeddingService`, hash fallback)
- [x] Hybrid retrieval: FTS + vector RRF (`HybridSessionSearchService`, `ReciprocalRankFusion`)
- [x] Config: `Memory:UseQdrantSync: true` — optional; prod defaults `false` (FTS-only until Qdrant enabled)

### 3.12 CognitiveMemorySystem Wiring

- [x] Connect existing `CognitiveMemorySystem` L0–L4 к `HermesMemoryManager` (`HermesCognitiveMemoryBridge`, `CognitiveSyncHermesMemoryStore`, prefetch L1 hints)
- [x] Remove parallel unused memory paths — single source of truth (`HermesBackedMemoryStore`, dream consolidation queue, `InMemoryMemoryStore` obsolete)

---

## ФАЗА 4 — Verify Plane (Grok + Obscura)

> **Источники:** Grok CLI, Obscura  
> **Оценка:** 6–8 недель (пересекается с Obscura O2)

### 4.1 VerifySubagent

- [x] `verify.agent.yaml` spec — toolset: bash, run_tests, browser_*, read_file
- [x] `IVerifySubagentService` — orchestrate verify stage
- [x] Separate stage в pipeline после Testing — **`RunVerifyMilestoneAsync` wired; banking bypass paths run verify before `MarkCompleted` (`BankingBypassCompletionFlow`)**
- [x] Cannot be skipped in production mode (benchmark mode: Optional)

### 4.2 VerifyRecipeRegistry

- [x] `IVerifyRecipeRegistry` — detect stack from workspace files
- [x] Recipes: django, fastapi, vite, solidjs, nextjs, spring-boot, dotnet, express, generic-fallback
- [x] Recipe fields: installCommands[], buildCommands[], testCommands[], startCommands[], smokeTargets[], smokeKind
- [x] CalorieVision recipe: backend django :8000 + frontend solidjs :5173
- [x] Banking recipe: java backend :8080 + react frontend :3000
- [x] `verify-detect` LLM fallback когда deterministic detection fails
- [x] Persist detected recipe: `.logs/runs/{runId}/verify/manifest.json`

### 4.3 VerifyOrchestrator

- [x] `prepareVerifyRun()` — recipe + checkpoint + sandbox settings
- [x] `runVerifyOrchestration()` — spawn verify subagent
- [x] Readiness probe: HTTP curl loop с timeout
- [x] Pass/fail gate: `IVerifyGateService`
- [x] On fail: structured evidence → Repair stage

### 4.4 Verify Evidence Store

- [x] Directory: `.logs/runs/{runId}/verify/`
- [x] Artifacts: `app.log`, `readiness.json`, `screenshot-final.png`, `smoke.webm`, `dom-snapshot.md`, `console-errors.json`, `verify-report.json`
- [x] `IVerifyEvidenceStore` — persist + retrieve + list
- [x] Wire в `GetBuildDiagnosticsDashboard` — artifact links + thumbnail *(test: `BuildDiagnosticsDashboardServiceTests`, `ObscuraEvidenceStoreTests`)*

### 4.5 Verify Integration Tests

- [x] Test: django app boot → readiness → screenshot captured
- [x] Test: verify fail → repair receives evidence in context
- [x] CalorieVision E2E: full verify stage

---

## ФАЗА O — Obscura Browser Plane (полная)

> **Оценка:** 10–13 недель (O1–O4)  
> **Принцип:** Obscura = единственный browser engine. Deprecate browser-mcp-server.

### O1 — Foundation (3–4 недели)

#### O1.1 Восстановить Application/Obscura layer

- [x] `Libr4.IDE.Application/Obscura/Protos/browser_automation.proto` — restore/generate
- [x] `BrowserAutomationGrpcClient` — full gRPC client
- [x] `IObscuraBrowserService` interface + models (`ObscuraLaunchOptions`, `ObscuraSessionInfo`, `AgentBrowserTask`, `AgentBrowserResult`)
- [x] `IAgentObscuraTool` — ResearchAsync, ScrapeAsync, PerformActionsAsync, ExtractAsync
- [x] `ISubagentObscuraIntegration` — RegisterConfig, ScrapeWithConfig, ExecuteBrowserTask
- [x] `ObscuraBrowserTool` implements `IObscuraBrowserTool`
- [x] `AgentObscuraTool` implements `IAgentObscuraTool`
- [x] `SubagentObscuraIntegration` implements `ISubagentObscuraIntegration`
- [x] MediatR commands: Launch, Navigate, Screenshot, ExecuteJs, Content, Close, Wait, Click, Type
- [x] MediatR handlers для всех commands
- [x] `DomToMarkdownConverter` — full implementation
- [x] Fix `ObscuraBrowserServiceAdapter` — stable sessionId + session manager integration

#### O1.2 ObscuraSessionManager

- [x] `IObscuraSessionManager` + `ObscuraSessionManager`
- [x] `ObscuraSessionLease` — sessionId, runId, purpose, acquiredAt, expiresAt
- [x] SQLite `obscura_sessions` table (`SqliteObscuraSessionRepository`)
- [x] Pool: max concurrent sessions, port range 9222–9250
- [x] `ObscuraSessionJanitor` hosted service — close stale sessions
- [x] Lease per runId — reuse session across browser tool calls in same run

#### O1.3 Native browser_* Agent Runtime Tools

- [x] `BrowserLaunchTool` (`browser_launch`)
- [x] `BrowserNavigateTool` (`browser_navigate`)
- [x] `BrowserSnapshotTool` — accessibility tree + refs (`browser_snapshot`)
- [x] `BrowserClickTool` (`browser_click`)
- [x] `BrowserTypeTool` (`browser_type`)
- [x] `BrowserScrollTool` (`browser_scroll`)
- [x] `BrowserWaitTool` (`browser_wait`)
- [x] `BrowserScreenshotTool` (`browser_screenshot`)
- [x] `BrowserExecuteJsTool` (`browser_execute_js`)
- [x] `BrowserConsoleTool` (`browser_console`)
- [x] `BrowserGetContentTool` (`browser_get_content`)
- [x] `BrowserExtractTool` (`browser_extract`)
- [x] `BrowserCloseTool` (`browser_close`)
- [x] `ObscuraBrowserToolFacade` — shared logic + runId lease reuse
- [x] Register all в `AgentRuntimeServiceCollectionExtensions`
- [x] Toolset restriction per subagent spec (`verify.agent.yaml` + `FilteredAgentToolRegistry`)

#### O1.4 Event Wiring

- [x] Wire every browser tool → `IAgentEventEmitter` EmitBrowser* methods
- [x] Wire → NDJSON `tool_use` events with `media[]`
- [x] Wire → `rollout.jsonl`
- [x] `AgentEventWebSocketHandler` — add browser event type mappings

#### O1.5 Autonomous Host DI

- [x] `AddObscuraBrowserPlane()` extension method *(IDE.Api + Autonomous Host)*
- [x] Register в `Libr4.IDE.AutonomousAppGeneration.Host/Program.cs`
- [x] `appsettings.json` → `Obscura` section (GrpcEndpoint, CdpFallback, MaxSessions, EvidenceRoot, AllowedHostPatterns)
- [x] docker-compose: `autonomous-app-generation-host` profile `autonomous` depends_on `obscura`; `ide-api` depends_on `obscura`

#### O1.6 O1 Tests

- [x] Integration: launch → navigate https://example.com → screenshot → close
- [x] Integration: session reuse within same runId (MediatR + stable sessionId)
- [x] Integration: session janitor closes expired

### O2 — Verify Plane (4–5 недель)

#### O2.1 ObscuraNetworkRouter

- [x] `IObscuraNetworkRouter` — resolve(runId, serviceName) → URL
- [x] Register port map on `PrepareShadowWorkspace`
- [x] Support: backend, frontend, api, custom service names
- [x] Docker host mapping: `host.docker.internal:PORT`
- [x] `ObscuraReadinessProbeService` — HTTP poll loop until 200 or timeout

#### O2.2 Browser Recording

- [x] `BrowserRecordStartTool` — WebM recording (frame capture + ffmpeg/minimal WebM)
- [x] `BrowserRecordStopTool` — persists `obscura/recording-stepN.webm` + `verify/smoke.webm`
- [x] Wire в VerifyOrchestrator smoke flow (Grok browser guidance pattern)

#### O2.3 ObscuraEvidenceStore

- [x] `IObscuraEvidenceStore` — persist screenshots, video, DOM snapshots, console
- [x] Path: `.logs/runs/{runId}/obscura/` + mirror to `.logs/runs/{runId}/verify/`
- [x] Content-addressed filenames (SHA-256) + `manifest.json`
- [x] Dashboard artifact viewer (`ObscuraEvidence` on build dashboard + `/obscura/artifacts/{file}`)

#### O2.4 Verify Stage на Obscura

- [x] Replace Grok `agent-browser` references с Obscura tools *(verify plane: `browser_*` tools + `ObscuraVerifySmokeRunner`; MCP `browser.smoke` → O2.5)*
- [x] Full smoke flow: record → navigate → wait → snapshot → click → console → screenshot → record stop
- [x] CalorieVision verify recipe execution end-to-end
- [x] Banking verify recipe execution end-to-end

#### O2.5 MCP Consolidation

- [x] `ObscuraMcpBridge` — `browser.smoke` → Obscura native (`ScrapeAsync`)
- [x] `ObscuraMcpBridge` — `browser.auth` → Obscura PerformActions
- [x] Config flag: `Mcp:BrowserLane:Provider: Obscura|Node` (default Obscura)
- [x] Deprecation notice на browser-mcp-server (`docs/DEPRECATED_BROWSER_MCP.md` + appsettings notice)
- [x] Remove Node browser-lane config after validation *(removed from Autonomous Host appsettings; Node fallback via `Provider: Node` + `browser-lane` profile)*

#### O2.6 O2 Tests

- [x] E2E: shadow app boot → Obscura navigate → screenshot in evidence store (`ObscuraO2E2ETests`)
- [x] E2E: verify fail → repair prompt contains screenshot path + console errors (`VerifyRepairEvidenceFormatter` + repair fragments)

### O3 — Subagents, Policy, Computer (3–4 недели)

#### O3.1 SubagentObscura YAML Integration

- [x] `SubagentBrowserConfig` loader from `*.agent.yaml` browser section
- [x] `DataSelector` execution engine
- [x] `BrowserTask` template engine (`{{url}}`, `{{param}}` substitution)
- [x] Example: `Agents/Subagents/verify-calorie.agent.yaml`
- [x] Example: `Agents/Subagents/price-monitor.agent.yaml` (из docs)

#### O3.2 ObscuraExecPolicyEngine

- [x] `obscura-exec-policy.yaml` — URL patterns, action rules
- [x] Deny file://, prompt https://external, allow localhost/*
- [x] `browser_execute_js` exfiltration patterns → prompt/forbid
- [x] `obscura-exec-audit.jsonl` per runId
- [x] PreToolUse hook для всех browser_* tools

#### O3.3 computer Subagent

- [x] `computer.agent.yaml` — toolset: browser_* + bash + read_file (evidence only)
- [x] `ComputerSubagentService` — UI automation for complex flows
- [x] Use cases: login flow, form fill, visual design check
- [x] Wire как typed subagent (Grok `computer` equivalent)

#### O3.4 browser_research Tool

- [x] `BrowserResearchTool` — multi-URL research (full `IAgentObscuraTool.ResearchAsync`)
- [x] Replace `CascadePlanner:PrefetchToolName: browser.smoke` → `browser_research` or native prefetch service (`CascadeWebPrefetchService`)
- [x] Stealth mode для external URLs (`BrowserUrlClassifier` + auto stealth in tool/prefetch)

#### O3.5 O3 Tests

- [x] Policy: external URL blocked without consent
- [x] computer subagent: login flow on test app
- [x] Subagent YAML task execution

### O4 — Fleet & Hardening (ongoing)

- [x] Session pool autoscaling across ports 9222–9250 (`ObscuraSessionPoolScaler` hosted service)
- [x] Stealth mode + user-agent rotation для external research (`ObscuraUserAgentRotator`)
- [x] Proxy profile support per session (`Obscura:ProxyProfiles` + `ProxyProfileName`)
- [x] `security-scanner :7070` post-verify scan integration (`VerifyOrchestrator` → `IObscuraSecurityScannerClient`)
- [x] Multi-tenant session isolation per userId (SQLite `user_id` + `TenantUserId` в generation и repair path)
- [x] Video playback в dashboard (`SessionDetail` evidence tab)
- [x] CDP fallback когда gRPC unavailable (`FallbackBrowserAutomationService`)
- [x] Obscura health check в Host startup gate (`ObscuraStartupHealthGate` + `/health/obscura`)
- [x] Metrics: sessions active, actions/min, evidence bytes, policy denials (`ObscuraTelemetry` wired)
- [x] Complete removal of browser-mcp-server dependency (Node browser lane hard-blocked in `McpToolInvocationService`)

---

## ФАЗА 5 — Platform & Ecosystem

> **Оценка:** ongoing, 6+ месяцев

### 5.1 MCP Host Full (Claude Code)

- [x] MCP server host: stdio + SSE transports (`McpStdioSession`, `McpSseTransport`, `McpRunHostManager`)
- [x] Tool/resource/prompt registry (`IMcpHostCatalog` / `McpHostCatalog`)
- [x] MCP server lifecycle per run (`McpRunHostManager` + `ReleaseRun` on run completion + janitor)
- [x] External MCP server discovery + preflight (`McpExternalServerDiscovery`, `/host/discovery`)
- [x] Replace ad-hoc MCP lanes with unified host where possible (`McpToolInvocationService` → unified host when enabled)

### 5.2 LSP Bridge (Claude Code)

- [x] `ILspBridge` — connect to language servers per stack (`LspBridge` + `ProcessLspClient` + stack resolver)
- [x] Inject definitions, references, diagnostics into context pack (`ContextPackBuilder` LSP section)
- [x] Wire на repair: show compiler diagnostics from LSP (`ShadowAgentRepairService` + `RepairFragmentInput.LspDiagnostics`)

### 5.3 Shadow Git Checkpoint (Gemini)

- [x] `git init` в shadow workspace on create (`IsolatedShadowExecutionService` + `IShadowGitCheckpointService`)
- [x] Tag before each repair attempt: `repair-attempt-{n}` (`ShadowAgentRepairService`)
- [x] `git diff` evidence в repair context (`RepairFragmentInput.GitDiffEvidence` + `ContextFragmentType.GitDiff`)
- [x] `rewind_to_tag` tool (`RewindToTagTool`)

### 5.4 Workspace Trust Store (Grok)

- [x] SQLite `workspace_trust` (workspaceHash, sandboxPolicy, hostMode, decidedAt) (`SqliteWorkspaceTrustStore`)
- [x] First-run prompt: remember choice per project (`WorkspaceTrustRunGate` + `/workspace-trust/resolve`)
- [x] Config override: explicit flags bypass trust store (`BypassTrustStore`, `ForceSandboxPolicy`, `ForceHostMode`)

### 5.5 Batch API / CI Mode (Grok + Codex)

- [x] `AutonomousAppGeneration:BatchLlmProfile:UseBatchLlmProfile` — cheaper model, no streaming (`LlmCallPreferenceContext` + `AutonomousBatchLlmProfileScope`)
- [x] Nightly CI: CalorieVision + Banking + NextJS regression runs (`.github/workflows/nightly-autogen-regression.yml` + `Run-NightlyAutogenRegression.ps1`)
- [x] Benchmark harness с KPI gates: `PipelineStageReached >= RepairLoop`, `patchesApplied > 0` (`BenchmarkRegressionHarness` + API evaluate endpoint)

### 5.6 Scheduling Daemon (Grok)

- [x] `IScheduledAgentRunService` — recurring headless prompts (`ScheduledAgentRunService` + SQLite store + hosted daemon)
- [x] MassTransit scheduler integration (`ExecuteScheduledAgentRunMessage` + `ScheduledAgentRunConsumer` + optional `UseMassTransit`)
- [x] Cron expressions per flow (`AgentScheduling:Flows` + `FlowCronParser` for calorie/banking/nextjs flows)

### 5.7 Extension Ecosystem (Gemini)

- [x] `.libr4/extensions/` plugin host (`ExtensionHost` + startup refresh + `/extensions` API)
- [x] Extension manifest: name, version, hooks, tools, skills (`extension.yaml` + `ExtensionManifestLoader`)
- [x] Sandboxed extension execution (`SandboxedExtensionRunner` — path jail, timeout, isolated env)

### 5.8 GitHub Actions Dispatch (Gemini + Grok)

- [x] Trigger GitHub workflow after verify pass (`GitHubShipService` + `ShipStage` + `libr4-autogen-ship.yml`)
- [x] PR creation with generated app (`GitHubApiClient` Git Data API + branch `libr4/autogen-{runId}`)

### 5.9 Honcho Multi-User Memory (Hermes)

- [x] Full Honcho SDK integration (beyond USER.profile.md) (`HonchoHttpClient` + dialectic chat API + local fallback)
- [x] Persona tracking per user per project (`.libr4/users/{userId}/projects/{projectKey}/PERSONA.md` + `HonchoMemoryService`)

### 5.10 KLIP Meta-Agent Evolution (Kimi)

- [x] Meta-agent analyzes failed runs → proposes agent spec diffs
- [x] Human approval queue for spec changes
- [x] Versioned agent specs with changelog

### 5.11 Fine-Tuning Data Pipeline (DeepSeek)

- [x] Extract `{instruction, output}` JSONL from successful Libr4 runs
- [x] Quality filter: syntax check, minhash dedup, readability score
- [x] Per-stack datasets: django, react, dotnet
- [x] DeepSpeed fine-tune pipeline (optional self-hosted model improvement)

### 5.12 Internal Eval Harness (DeepSeek)

- [x] `Evaluation/` suite: stack-specific benchmarks
- [x] HumanEval-style для django-views, react-components, dotnet-controllers
- [x] MBPP-style для algorithm files
- [x] Regression gate: eval score must not decrease on PR

### 5.13 Live Search Tools (Grok + Hermes)

- [x] `search_web` tool с rate limit + cache
- [x] `search_x` tool (optional, API key gated)
- [x] SSRF protection, content size cap

> **Out of scope (removed):** Telegram bot, VS Code extension, Agent Client Protocol — не входят в продуктовый roadmap Libr4.

---

## ФАЗА 6 — Provider Mesh & Infrastructure

### 6.1 Model Routing per Role

- [x] `IAgentModelRouter` — model per subagent type (fast for explore, strong for implementer)
- [x] Config: `AgentModels:explore`, `:implementer`, `:verify`, `:repair`, `:computer`
- [x] OpenRouter profile / DMR profile / Batch profile
- [x] Fallback chain с circuit breaker (уже частично есть — расширить per-role)

### 6.2 Provider Capability Matrix — расширение

- [x] Per-stage provider override в appsettings
- [x] Cost tracking per provider per run
- [x] Budget enforcement per stage (расширить `IBudgetService`)

### 6.3 Docker Compose Full Stack

- [x] `libr4-obscura` — verified healthy before Host accept runs
- [x] `shadow-sync`, `sandbox-controller`, `security-scanner` — wired to verify stage
- [x] `qdrant` — wired to memory sync
- [x] Single `docker compose --profile agent up` для полного стека
- [x] Health gate script: all services healthy before smoke

### 6.4 Autonomous Host Profiles

- [x] `OpenRouter` profile (cloud)
- [x] `DockerModelRunner` profile (local)
- [x] `BatchCI` profile (unattended)
- [x] `Benchmark` profile (KPI mode)
- [x] Documented switch procedure (как в `.logs/run-*.ps1`)

---

## ФАЗА 7 — Agent Command Surface (Devin Desktop / Cursor / Windsurf parity)

> **Цель:** закрыть продуктовый разрыв с [Devin Desktop](https://devin.ai/desktop) (бывш. Windsurf), Cursor, Codex CLI — не копируя чужой runtime, а строя **Command Surface** поверх уже существующего Libr4 backend (Cascade, Agent Runtime, Hermes, Obscura, Flow).  
> **Принцип:** каждый пункт — production-grade: API + persistence + UI + observability + тесты. Stub/Null не допускаются.  
> **Источники:** Devin Sessions/Board/Spaces/ACP/Fast Context, Cursor Composer/Background Agents/Bugbot, Windsurf Cascade/Supercomplete/Flow handoff.  
> **Оценка:** 12–16 недель (можно параллелить 7.1 UI + 7.3 Fast Context + 7.2 Worktrees).

### Конкурентный контекст — что уже есть у Libr4 (не дублировать)

| Возможность конкурентов | Статус у Libr4 | Не повторять |
|-------------------------|----------------|--------------|
| Cascade orchestrator pass | `[x]` `AutonomousCascadePlanner` | ещё один planner |
| Subagents + YAML specs | `[x]` 1.2, O3.1–O3.3 | generic chat agent |
| Background explore | `[~]` `DelegateTool` 1.5 | отдельный explore-сервис |
| Memory между run'ами | `[x]` Hermes L0–L4 | простой chat history |
| Browser verify + evidence | `[x]` Obscura O1–O3 | MCP browser.smoke |
| Flow recipes | `[x]` 1.3 calorie/banking | ad-hoc scripts |
| Permission / exec policy | `[x]` 0.2, O3.2 | regex-only guard |

**Moat Libr4 (сохранять и усиливать в UI):** полный Plan→Generate→Verify→Repair→Ship pipeline, Obscura evidence, Flow recipes, Hermes playbook, exec policy consent.

---

### 7.1 Agent Command Center — Sessions Board (Devin Desktop «home for every agent»)

> Devin Desktop = не чат, а **флот агентов**: List/Board, статусы Working / PR ready / Waiting for CI, переключение между сессиями без потери контекста.  
> У Libr4 backend готов (runs, subagents, delegations, flow, rollout); не хватает **операционной поверхности**.

#### 7.1.1 Domain & API

- [x] `IAgentFleetRegistry` — агрегат всех активных и недавних run'ов пользователя (не только in-memory orchestrator)
- [x] `AgentFleetEntry` DTO: `runId`, `title`, `spaceId`, `status`, `stage`, `agentCount`, `lastActivityAt`, `costUsd`, `modelProfile`, `verifyStatus`, `prUrl`, `ciStatus`
- [x] Статусы: `Queued`, `Planning`, `Generating`, `Verifying`, `Repairing`, `WaitingForApproval`, `WaitingForCi`, `PrReady`, `Completed`, `Failed`, `Cancelled`
- [x] `GET /api/ide/agent-fleet` — list с фильтрами: `status`, `spaceId`, `stack`, `dateRange`, `search`
- [x] `GET /api/ide/agent-fleet/{runId}/summary` — rollup: phases, subagents, delegations, flow node, last error, evidence count
- [x] `PATCH /api/ide/agent-fleet/{runId}` — rename title, pin, archive
- [x] `POST /api/ide/agent-fleet/{runId}/cancel` — cooperative cancel через `IAutonomousRunControlService`
- [x] WebSocket/SSE канал `agent-fleet-events` — push status transitions (не polling dashboard)
- [x] Persistence: SQLite `agent_fleet_index` (денормализованный индекс поверх run state) + rebuild из `.logs/runs/{runId}/`

#### 7.1.2 Board UI (Frontend SolidJS)

- [x] Route `/ide/agent-board` — Kanban по статусам (колонки = 7.1.1 statuses)
- [x] Card: title, stack badges, elapsed time, agent avatars (subagent roles), verify badge (pass/fail/pending)
- [x] List view toggle — таблица с сортировкой по `lastActivityAt`, `costUsd`, `stage`
- [x] Quick filters: Running / Waiting for review / Done (как Devin Board)
- [x] Card click → split view: left = session timeline, right = live NDJSON stream
- [x] Bulk actions: cancel selected, archive completed > 7d
- [x] Empty state + onboarding CTA «Start new generation»
- [x] Mobile-responsive collapse (list-only на узких экранах)

#### 7.1.3 Session Detail Surface

- [x] Unified timeline: plan phases + tool calls + subagent spawns + delegation complete + verify attempts (`GET /agent-fleet/{runId}/timeline`, `UnifiedTimeline.tsx`)
- [x] Вкладки: Overview | Agents | Diff | Evidence | Memory | Rollout | Settings *(Diff = generated-files preview; full diff aggregator → 7.4)*
- [x] Live step counter + token/cost meter per run (`GET /usage` + `RunUsageMeter`, poll 5s)
- [x] Permission prompt modal (wire 0.2 `POST .../permission-mode/resolve`) — blocking UI когда `obscura_execpolicy_prompt`
- [x] Keyboard shortcuts: `j/k` navigate cards, `Enter` open run, `c` cancel

#### 7.1.4 Observability & SLO

- [x] Метрики Prometheus: `libr4_fleet_runs_active`, `libr4_fleet_status_transition_total`, `libr4_fleet_time_to_verify_seconds`
- [x] Alert: run stuck in `Repairing` > 30 min без tool activity — `AgentFleetStuckRunMonitor` + `libr4_fleet_stuck_repairing_total` + UI banner
- [x] Audit: кто cancel/archive/pin (userId + timestamp)

#### 7.1.5 Tests

- [x] Integration: create run → status transitions appear in fleet API within 2s
- [x] Integration: WebSocket receives `Verifying` after VerifyStage starts *(AgentFleetEventHub unit test; E2E — Phase 7.1.5 Playwright)*
- [x] E2E Playwright: board renders 3 runs in different columns (`e2e/agent-board.spec.ts`)

---

### 7.2 Spaces — shared context + Git worktree isolation (Devin Spaces)

> Devin **Spaces** = один продуктовый контекст (задача, репо, memory scope) + **несколько агентов на изолированных ветках/worktree**.  
> У Libr4: shadow workspace `[x]`, delegate `[~]`, DMail `[x]` — нет production worktree и Space entity.

#### 7.2.1 Space Domain Model

- [x] `AgentSpace` aggregate: `spaceId`, `name`, `repositoryUrl`, `baseBranch`, `ownerId`, `sharedMemoryScope`, `mcpProfile`, `createdAt`
- [x] `SpaceMember`: `agentRole` (implementer | explorer | verifier | computer), `runId`, `worktreePath`, `branchName`, `status`
- [x] `ISpaceStore` — SQLite `agent_spaces`, `space_members`
- [x] API: `POST /api/ide/spaces` — create space from user request + optional upstream repo
- [x] API: `GET /api/ide/spaces/{spaceId}` — members, active runs, shared artifacts
- [x] API: `POST /api/ide/spaces/{spaceId}/agents` — spawn typed agent into new worktree
- [x] API: `POST /api/ide/spaces/{spaceId}/merge/{memberId}` — merge member branch → integration branch (with conflict report)

#### 7.2.2 Git Worktree Manager

- [x] `IGitWorktreeService` — обёртка над `git worktree add` / merge
- [x] On space create: `git clone` or bind existing shadow repo → `main` worktree
- [x] On agent spawn: `worktree add ../wt-{memberId} -b agent/{role}/{shortId}`
- [x] Worktree path: `.logs/spaces/{spaceId}/worktrees/{memberId}/`
- [x] Janitor: remove worktree on agent complete + optional retain 24h for diff review — `SpaceWorktreeJanitorHostedService`
- [x] Guard: max worktrees per space (config default 4, hard cap 8)
- [x] ExecPolicy: deny `git push --force`, deny worktree escape outside space root

#### 7.2.3 Shared Context Bus

- [x] `ISpaceContextBus` — pub/sub внутри space: plan摘要, API contracts, design artifacts, verify results (`context-events.jsonl` + `shared/`)
- [x] Wire Hermes memory scope = `project:{spaceId}` для cross-agent recall (`HermesMemoryScopeResolver`, `AgentSessionRunRequest.SpaceId`)
- [x] Wire DMail default addressing: `@space/{role}` + handoff on `space_context_ready` (`SpaceContextNdjsonFanout`)
- [x] Shared files dir: `.logs/spaces/{spaceId}/shared/` (LIBR4.md, design.md, api-openapi.yaml) — all agents read-only except architect role *(write via bus snapshots)*
- [x] Event: `SpaceContextUpdated` → push to all active member sessions via NDJSON (`SpaceContextNdjsonFanout` → `events.jsonl`)

#### 7.2.4 Parallel Agent Orchestration

- [x] `ISpaceOrchestrator` — DAG внутри space: explorer → implementer → verifier (может пересекаться по времени)
- [x] Explorer (delegate) стартует first; implementer waits on `SpaceContextReady` или timeout
- [x] Verifier agent mounts integration worktree after implementer checkpoint
- [x] Conflict policy: **human prompt only** on merge conflict — no auto-resolution (`SpaceMergeConflictPolicy`, merge UI report)
- [x] Fleet limiter: `ISpaceConcurrencyGate` — max parallel LLM calls per space

#### 7.2.5 UI

- [x] Space picker в Agent Board (filter by space)
- [x] Space detail: member cards с branch name, context timeline, merge + orchestrate actions (`/ide/spaces/:spaceId`)
- [x] «Open in worktree» — file tree scoped to member worktree (`WorktreeExplorer`, `GET .../members/{id}/files`)
- [x] Merge preview UI before integration — `GET .../merge/{memberId}/preview`, diff stat + unified diff in Space Detail

#### 7.2.6 Tests

- [x] Integration: 2 agents same space, different worktrees, no file collision
- [x] Integration: delegate explorer completes → DMail → implementer receives context
- [x] Integration: merge conflict surfaces human-readable report
- [x] Integration: orchestrator pipeline spawns explorer/implementer/verifier
- [x] E2E: space with implementer + verifier → verify runs on integration branch (`e2e/agent-space.spec.ts`)

---

### 7.3 Fast Context Engine (Windsurf «Fast Context» / Cursor `@codebase`)

> Миллисекундный поиск релевантных файлов и строк для агента — не статический ContextPack, а **on-demand tool** с fusion ranker.  
> У Libr4: RepoGraph `[x]`, Qdrant optional `[x]` — нет unified `search_codebase` и prefetch hook в repair.

#### 7.3.1 Index Layer

- [x] `ICodebaseIndex` interface: `IndexAsync`, `SearchAsync`, `GetSymbolAsync`, `InvalidateAsync`
- [x] `RipgrepCodeIndex` — fast literal/regex search, ripgrep JSON output, per-run cache + scan fallback
- [x] `EmbeddingCodeIndex` — chunk files 40–80 lines, embed via `IEmbeddingService`, store in `IVectorMemoryStore` collection `libr4_codebase_{workspaceHash}`
- [x] `RepoGraphRanker` — boost files connected to hit via import/dependency edges
- [x] `FastContextFusionRanker` — RRF merge: `score = w1*rg + w2*graph + w3*path_heuristic`
- [x] Incremental index on file change — `FastContextWorkspaceSyncBridge` invalidates cache on `FileSystemWorkspaceSyncService` events
- [x] Index manifest: `.logs/runs/{runId}/context-index/manifest.json` (chunk hashes, indexedAt)

#### 7.3.2 Agent Tool

- [x] `SearchCodebaseTool` (`search_codebase`) — input: `{ "query": "...", "limit": 12, "include_tests": false, "languages": ["py","ts"] }`
- [x] Output: `[{ path, startLine, endLine, score, snippet, matchKind }]`
- [x] `GetSymbolContextTool` (`get_symbol_context`) — dedicated tool via `ICodebaseIndex.GetSymbolAsync`
- [x] Register in `AgentRuntimeServiceCollectionExtensions`; include in default implementer toolset
- [x] Budget: max 8 search calls per repair turn; result truncation 4K chars per hit

#### 7.3.3 Prefetch Hooks

- [x] `IFastContextPrefetcher` — before repair/generate turn: auto-run top-3 queries from error message + file path
- [x] Wire в `ShadowAgentRepairService` и `ClaudeCodeStyleRepairService`
- [x] Wire в `ContextPackBuilder` as `## fast_context` section when index hit confidence > 0.7
- [x] Cascade prefetch synergy: repo URLs from user request + `search_codebase` on cloned upstream (`CascadeCodebasePrefetchService`, `GitUpstreamCloneProvider`)

#### 7.3.4 Performance SLO

- [x] P95 `search_codebase` < 800ms warm index, < 3s cold index (workspace < 500 files) — CI gate `FastContextSearchSloTests`
- [x] Background index build on `PrepareShadowWorkspace` — не блокировать plan phase (`IsolatedShadowExecutionService` fire-and-forget warm)
- [x] Metrics: `libr4_fast_context_query_duration_ms`, `libr4_fast_context_cache_hit_ratio` (`FastContextTelemetry`)

#### 7.3.5 Tests

- [x] Unit: fusion ranker orders graph-neighbor above unrelated file
- [x] Integration: Django project search «User model» returns `models.py` in top-3
- [x] Integration: repair turn auto-prefetch injects failing file neighbors
- [x] Benchmark: index 300 files < 10s on CI runner (`FastContextBenchmarkTests`)

---

### 7.4 Diff Review & Evidence Panel (Devin «review every agent diff» + Obscura moat)

> Devin: human review всех diff'ов агента до push. Libr4: `DiffPanel` + `EvidenceFilmstrip` + review API — unified review surface в SessionDetail.

#### 7.4.1 Diff Aggregation Backend

- [x] `IRunDiffAggregator` — собрать все file changes по run: from rollout tool results + patch attempts
- [x] `RunFileDiff` DTO: `path`, `language`, `changeKind` (add/modify/delete), `hunks[]`, `stepNumber`, `agentRole`, `toolName`
- [x] `GET /api/ide/app-generation/{runId}/diffs` — paginated, filter by path/step
- [x] `GET /api/ide/app-generation/{runId}/diffs/detail?path=` — unified diff + provenance JSON
- [x] Link each diff hunk → rollout.jsonl tool_call id (provenance `rollout:{line}` / `patch:{file}`)
- [x] Snapshot diff at verify checkpoint: `verify-pass-{n}` tag
- [x] `GET /api/ide/app-generation/{runId}/diffs/checkpoints` + `?checkpoint=verify-pass-{n}` filter

#### 7.4.2 Evidence Correlation

- [x] `IEvidenceDiffCorrelator` — привязать Obscura screenshot/console/DOM к diff step (same stepNumber)
- [x] `GET /api/ide/app-generation/{runId}/diffs/evidence?path=` — screenshots, console errors, smoke.webm clips (+ overlays без path)
- [x] Verify fail overlay: highlight files mentioned in console stack trace
- [x] Security review overlay: highlight files flagged by `LlmSecurityReviewGateService`

#### 7.4.3 Review Workflow API

- [x] `ReviewDecision` enum: `Approve`, `Reject`, `RequestRepair`, `ApproveWithNotes`
- [x] `POST /api/ide/app-generation/{runId}/review` — per-file or batch decision + optional notes
- [x] `GET /api/ide/app-generation/{runId}/review` — aggregate review status
- [x] `IReviewGate` — ShipStage blocked until `reviewStatus == Approved` when `RequireHumanReview: true` (production default)
- [x] Rejected files → auto-spawn repair subagent with scoped prompt (only rejected paths)
- [x] Audit trail: `.logs/runs/{runId}/review/decisions.jsonl`

#### 7.4.4 Review UI

- [x] Route `/ide/runs/{runId}/review` — file tree left, diff center, evidence right
- [x] Side-by-side diff viewer (monaco-diff)
- [x] Evidence filmstrip: screenshots chronological per verify attempt
- [x] Console error panel synced to selected file (stack trace click → jump)
- [x] Batch approve bar + per-file checkboxes
- [x] «Request repair» opens scoped repair dialog with pre-filled file list
- [x] Keyboard: `a` approve file, `r` reject, `n/p` next-prev file

#### 7.4.5 Tests

- [x] Integration: verify fail → review UI shows console + screenshot for same step
- [x] Integration: reject file → repair subagent receives only that path in AllowedTools scope
- [x] E2E: batch approve → ShipStage proceeds
- [x] Contract: diff API stable for frontend (pact test)

---

### 7.5 Cloud ↔ Local Run Handoff (Devin «close laptop → continue in cloud»)

> Devin Desktop позволяет handoff local session → cloud worker. Libr4: resume/checkpoint `[x]` — нет promote/export между средами.

#### 7.5.1 Run Export Package

- [x] `IRunExportService` — создать portable bundle: `run-manifest.json`, `agent_session.sqlite` snapshot, workspace tarball, `.logs/runs/{runId}/` artifacts
- [x] Content-addressed tarball SHA-256; max size guard (config 2GB); exclude `node_modules`, `.venv`
- [x] API: `POST /api/ide/app-generation/{runId}/export` → download URL + `GET .../export/{exportId}/download`
- [x] Export includes: permission store state, playbook hints used, flow position, space membership

#### 7.5.2 Run Import / Promote

- [x] `IRunImportService` — validate manifest, rehydrate workspace, resume `AgentSession` at `lastStepNumber`
- [x] API: `POST /api/ide/app-generation/import` — upload bundle → new `runId` linked to `sourceRunId` lineage
- [x] `POST /api/ide/app-generation/{runId}/promote-to-cloud` — enqueue on Autonomous Host queue (MassTransit)
- [x] Environment remap: localhost URLs → `IObscuraNetworkRouter` shadow ports on import
- [x] Idempotency key per export SHA — duplicate import returns same runId

#### 7.5.3 Sync While Local

- [x] Optional live sync: local IDE changes → `WorkspaceSyncDelta` stream to cloud run (when both active)
- [x] Conflict resolution: last-write-wins per file with merge marker on collision
- [x] WebSocket channel `run-sync-{runId}` *(route: `/ws/run-sync/{runId}?role=local|cloud&workspaceRoot=...`)*

#### 7.5.4 UI & Ops

- [x] Button «Continue in cloud» on Session Detail (when local host profile)
- [x] Status badge `HandoffPending` / `HandoffComplete` on fleet board
- [x] Run sync indicator: poll `GET .../sync/conflicts`, badge «Sync active» / «N sync conflicts» (`RunSyncIndicator`)
- [x] Runbook: export/import CLI `dotnet libr4-run.dll export|import`
- [x] Retention policy: export bundles expire 7d

#### 7.5.5 Tests

- [x] Integration: local run 5 steps → export → import on clean host → resume step 6
- [x] Integration: verify URLs remapped after import into shadow environment
- [x] Chaos: import corrupted tarball → structured error, no partial workspace

---

### 7.6 Multi-Agent Backend Adapters (ACP-inspired, без жёсткой зависимости от ACP)

> Devin унифицирует Codex, Claude Agent, OpenCode через [Agent Client Protocol](https://devin.ai/desktop). Libr4: provider mesh `[~]` — нет adapter layer для **внешних** agent backends в одном fleet.

#### 7.6.1 Adapter Contract

- [x] `IAgentBackend` interface: `SpawnAsync`, `SendMessageAsync`, `StreamEventsAsync`, `CancelAsync`, `GetStatusAsync`
- [x] `AgentBackendKind` enum: `Libr4Native`, `CursorSdk`, `CodexCli`, `OpenCodeCli`, `ExternalAcp` (optional)
- [x] `AgentBackendDescriptor` в YAML spec: `backend: libr4-native | cursor-sdk | codex-cli` + `backendConfig`
- [x] Unified event mapping → Libr4 NDJSON schema (tool_use, message, status, cost)

#### 7.6.2 Native Backend (default)

- [x] `Libr4NativeAgentBackend` — thin wrapper over existing `AgentSession` (no behavior change)
- [x] Feature parity gate: all 7.x UI works only through this backend initially *(AgentSpecSubagentRunner → IAgentBackendCoordinator)*

#### 7.6.3 External Adapters (incremental)

- [x] `CursorSdkAgentBackend` — Node runner `scripts/cursor-sdk-agent.mjs` + `@cursor/sdk` Agent.prompt
- [x] `CodexCliAgentBackend` — subprocess `codex exec` with JSONL stdout parse
- [x] `OpenCodeCliAgentBackend` — subprocess adapter with timeout + stderr capture
- [x] `ExternalAcpAgentBackend` — JSON-RPC 2.0 over stdio; configurable methods + `scripts/acp-mock-agent.mjs`
- [x] Sandboxing: external backends run in `IIsolatedRuntime` container, no host FS except workspace mount *(IsolateExternalBackends + backendConfig isolate=true)*

#### 7.6.4 Fleet Integration

- [x] Fleet card shows `backend` badge (Native / Cursor / Codex)
- [x] Cost accounting per backend in `IBudgetService` (`GetBackendUsage`, `/usage` backendUsage)
- [x] Policy: which backends allowed per org (`AllowedBackends` in AgentBackends config)
- [x] Fallback: external backend fail → retry with Libr4Native + `fallbackFrom` badge in fleet

#### 7.6.5 Tests

- [x] Integration: Libr4Native backend spawn + complete simple task *(StubAgentSession E2E)*
- [x] Contract test: each backend emits valid NDJSON event sequence
- [x] Mock external backend timeout → graceful cancel + fleet status `Failed` *(CliBackend_Cancel_MarksCancelled)*

---

### 7.7 Background Agent Fleet (Cursor Background Agents + Devin parallel sessions)

> Cursor background agents + Devin «team of agents». Libr4: `DelegateTool` `[~]` без production e2e и fleet coordination.

#### 7.7.1 Delegate Runtime Hardening

- [x] `IDelegationWorkerHost` — `ManagedDelegationWorkerHost` (in-process + timeout/retry) + `ProcessDelegationWorkerHost` (out-of-process via `libr4-run delegation-run`)
- [x] Delegation lifecycle: `Queued` → `Running` → `Completed` | `Failed` | `TimedOut`
- [x] Config: `DelegationTimeoutMinutes` (default 15), `MaxConcurrentDelegationsPerRun` (default 3)
- [x] Resource limits: CPU/memory cap per delegation worker (`WorkerMemoryLimitMb`, Windows `MaxWorkingSet`)
- [x] Auto-restart once on worker crash; then fail with diagnostics
- [x] `DelegationWorkerCliBootstrap` + CLI `delegation-run --request <worker.json>`
- [x] `DelegationBackgroundContext` (AsyncLocal) + `DELEGATE_BACKGROUND_CHILD` for out-of-process
- [x] Integration tests: start/list, concurrency limit, retry-then-fail

#### 7.7.2 Parent Session Integration

- [x] `DelegationNotification` inject в parent turn boundary (не mid-tool) — `InjectDelegationNotificationsAsync` в начале каждого turn
- [x] Parent prompt section `## delegation_results` с markdown summary + file pointers — `DelegationPromptFormatter`
- [x] Deny nested delegation — `DelegationBackgroundContext` (AsyncLocal) + `DELEGATE_BACKGROUND_CHILD` out-of-process
- [x] Wire `delegate` / `delegation_list` / `delegation_read` в default implementer + repair toolset (yaml)
- [x] Integration tests: prompt formatter, spec toolset, notification dequeue, nested deny

#### 7.7.3 Fleet Coordination

- [x] `IBackgroundFleetScheduler` — priority queue: user-initiated > scheduled > retry
- [x] Fair scheduling across users (tenant quota via `MaxConcurrentDelegationsPerTenant`)
- [x] Preempt low-priority delegations when implementer needs GPU/LLM budget (`RaiseImplementerBudgetPressure`)
- [x] Dashboard widget: «N background agents running» с expand list (`BackgroundAgentsWidget` + SubagentsPanel)
- [x] API: `GET /api/v1/ide/agent-fleet/background-delegations`, `GET .../app-generation/{id}/delegations`
- [x] Integration tests: priority, tenant fairness, preempt

#### 7.7.4 Observability

- [x] Separate rollout file per delegation: `.logs/runs/{runId}/delegations/{id}/rollout.jsonl`
- [x] Metrics: `libr4_delegation_duration_seconds`, `libr4_delegation_timeout_total` (+ completed/failed counters)
- [x] Alert: delegation timeout rate > 10% per hour — `DelegationTimeoutAlertMonitor` + `libr4_delegation_timeout_rate_alert_total`
- [x] API: `GET /api/v1/ide/agent-fleet/delegation-metrics`

#### 7.7.5 Tests (закрывает 1.5 `[~]`)

- [x] **E2E: delegate explore while implementer writes files** — explore spec read-only + `DelegateTool.IsReadOnly` enforced
- [x] Integration: delegation timeout → parent receives partial summary (notification)
- [x] Integration: 3 parallel delegations → all complete, parent ingests notifications
- [x] Load: 10 delegations queued → fair drain within global quota (scheduler)

---

### 7.8 IDE Inline Intelligence — Supercomplete / Tab (Windsurf «Tab, Tab, Ship»)

> Windsurf Supercomplete предсказывает **следующую мысль**, не только следующий токен.

#### 7.8.1 Completion Service

- [x] `IInlineCompletionService` — request/response с latency budget (`MaxLatencyMs`, default 2000ms)
- [x] Context window: current file at cursor + RepoGraph related imports hint + optional session intent
- [x] Model routing: fast role (`explore` via `AgentModels`) — не блокирует main agent LLM budget
- [x] Trigger: Monaco inline provider (typing pause), suppress while agent running / read-only tab

#### 7.8.2 Frontend Editor Integration

- [x] Monaco inline ghost text provider (`registerInlineCompletionsProvider`)
- [x] Accept via Tab (Monaco default), dismiss Esc
- [x] Disable in agent-editing / diff read-only states

#### 7.8.3 Agent Synergy

- [x] Suppress while `isAIStreaming`, `activeGenerationRunId`, or `activeAgents.running`
- [x] API: `POST /api/v1/ide/app-generation/inline-complete`

#### 7.8.4 Privacy & Cost

- [x] No send when `WorkspaceTrust.DenyCloudInference` on active run
- [x] Separate completion path (not charged against agent run budget)
- [x] Opt-in via `AutonomousAppGeneration:InlineCompletion:Enabled`

#### 7.8.5 Tests

- [~] Latency benchmark gate in CI *(future perf gate)*
- [x] Integration: disabled + ghost text response (`InlineCompletionTests`)

**Статус фазы:** `[x]`

---

### 7.9 Session History & Cross-Run Search (Devin session list + Hermes FTS)

> Devin показывает историю сессий «42m ago», search across work. Libr4: FTS session search `[x]` 3.8 — не объединено с fleet UI и space scope.

#### 7.9.1 Unified Session Index

- [x] Extend FTS5 index: fleet title, user request, error signatures, files touched, space name, stack tags (`fleet_session_fts`)
- [x] `GET /api/v1/ide/agent-fleet/search?q=...` — facets stack/outcome/space/dateBucket
- [x] Facets: stack, outcome (pass/fail/running), date bucket, space
- [x] «Similar runs» — embedding nearest neighbor on user request + error signature (Qdrant / in-process fallback)

#### 7.9.2 History UI

- [x] `/ide/history` — searchable list with preview snippet
- [x] Click → readonly timeline via `/ide/runs/{runId}`
- [x] «Fork from run» — `POST /agent-fleet/{runId}/fork`
- [x] Pin runs (existing fleet patch)

#### 7.9.3 Retention & GDPR

- [x] Configurable retention: episodic 90d, fleet index 365d, artifacts per storage policy
- [x] `DELETE /api/ide/agent-fleet/{runId}/gdpr-erase` — fleet index + FTS + run directory
- [x] Export user data bundle on request (`GET .../gdpr-export`, `POST .../retention/sweep`)

#### 7.9.4 Tests

- [x] Integration: search by unique error string finds correct run
- [x] Integration: fork run preserves plan YAML, new runId
- [x] Integration: similar runs embedding excludes self (`FleetSimilarRunsTests`)
- [x] Performance: search 500 index < 200ms P95 (`FleetSessionSearchTests`)

---

### 7.10 PR / CI Integration Surface (Devin «PR is ready» / «Waiting for CI»)

> Devin Board статусы PR ready, Waiting for CI. Libr4: ShipStage `[x]`, GitHub trigger `[x]` 5.8 + fleet CI loop 7.10.

#### 7.10.1 Git Provider Integration

- [x] `IGitHubShipService` / `GitHubApiClient` — branch, commit, push, open PR *(5.8)*
- [x] `IPullRequestService` — `POST /agent-fleet/{runId}/pull-request` after review + verify
- [x] PR body auto-generated with verify summary
- [x] Attach Obscura screenshot manifest as PR comment (`ObscuraPrCommentFormatter`, `CreatePullRequestCommentAsync`)

#### 7.10.2 CI Webhook Loop

- [x] `POST /api/v1/ide/webhooks/github/ci` — workflow_run / check_run
- [x] Map CI state → fleet status `WaitingForCi` → `Completed` | `Failed`
- [x] Store CI logs link on fleet card (`ciLogsUrl`)
- [x] Auto-spawn repair run on CI fail with log prefetch (`CiRepairDispatcher`, `GitHubCiLogPrefetcher`, `FleetCiRepairTests`)

#### 7.10.3 Fleet Status Sync

- [x] `PrReady` / `WaitingForCi` / `Completed` via `RunShipState` + `FleetShipSyncService`
- [x] Manual override API: `PATCH /agent-fleet/{runId}` with `statusOverride`
- [x] ShipStage records PR state into fleet index

#### 7.10.4 UI

- [x] PR link + CI badge on board cards
- [x] «Open PR» CTA on review approve bar
- [x] CI log preview drawer

#### 7.10.5 Tests

- [x] Integration: ship state → fleet `WaitingForCi` (`FleetShipSyncServiceTests`, `AgentFleetRegistryTests`)
- [x] Integration: CI webhook transitions ship state
- [x] E2E: full loop verify → review → PR → CI → Completed (`FleetPrCiLoopTests`, `e2e/pr-ci-loop.spec.ts`)

---

### 7.11 Libr4 Competitive Moat — усилить, не копировать

> Эти пункты **отличают** Libr4 от Devin/Cursor; усиливать параллельно с 7.1–7.10.

- [x] **Verify-first Ship gate:** ShipStage hard-block без Obscura evidence manifest (production config)
- [x] **Flow recipe library:** versioned `Flows/*.flow.yaml` с CI regression per recipe (calorie, banking, nextjs-shop) — `FlowRecipeRegressionTests`, `flow-recipe-gate.yml`
- [x] **Playbook-driven repair:** surface playbook hit rate on fleet card; auto-activate matching playbook on error signature *(hint via TryGetHintAsync)*
- [x] **Exec policy consent UX:** dedicated modal stream for `obscura_execpolicy_prompt` (`ObscuraExecPolicyPromptModal`, NDJSON event, `execPolicyPromptStream`)
- [x] **Cascade + Fast Context:** orchestrator prefetch = `browser_research` + `search_codebase` on upstream clone
- [x] **Multi-stack quality gates:** stack-specific verify recipes в dashboard filter
- [x] **Run quality score:** composite KPI (verify pass, patches, playbook hits, human review time) — sort fleet by quality

---

## Тестирование — обязательный минимум per фаза

| Фаза | Обязательные тесты |
|------|-------------------|
| 0 | Session resume; apply_patch; execpolicy deny; NDJSON sequence; path gating |
| 1 | YAML agent spawn; delegate background; flow execution; stage migration |
| 2 | RepoGraph ordering; FIM repair; activate_skill; semantic compaction |
| 3 | Memory persist restart; playbook hit; crystallize; FTS search |
| 4 | Verify pass/fail; evidence in dashboard; repair with evidence |
| O | Obscura E2E; session pool; policy; MCP bridge; CalorieVision verify |
| 5 | Extension load; shadow git rewind; batch CI regression |
| 6 | Per-role routing; full docker stack health |
| 7 | Fleet board E2E; worktree isolation; search_codebase; diff review; delegate e2e; PR/CI loop |

---

## Приоритет исполнения (рекомендуемый)

```text
Wave 1 (стабилизация текущего):
  0.5 Surgical Repair full wire
  0.3 Tool JSON Schema
  O1 Obscura Foundation
  1.7 Pipeline stages (Generation + Repair)

Wave 2 (quality loop):
  4.1–4.4 Verify Plane
  O2 Obscura Verify
  0.8 Rollout Recorder
  0.9 NDJSON Events

Wave 3 (intelligence):
  3.1–3.5 Hermes Memory + Playbook
  2.1 RepoGraph
  2.5 activate_skill
  1.5 task/delegate

Wave 4 (platform):
  1.2 YAML Agent Specs
  1.3 Flow Engine
  0.4 apply_patch full
  3.7 Skill Crystallization ✅
  3.8 FTS5 Session Search ✅

Wave 5 (platform):
  5.x Platform items (5.1–5.13)
  5.11 Fine-tuning pipeline
  5.12 Eval harness

Wave 6 (command surface — Devin/Cursor/Windsurf parity): ✅ shipped
  7.1–7.11 including Supercomplete + Spaces human-merge policy
```

---

## Файлы для создания (сводка верхнего уровня)

```text
src/Services/IDE/Libr4.IDE.Application/
  Obscura/                          # O1 — restore full layer
  AgentRuntime/                     # расширение existing
  Memory/Hermes/                    # 3.x
  Context/RepoGraph/                # 2.1
  Context/Fim/                      # 2.3
  Context/Fragments/                # 2.6
  Context/Compaction/               # 2.7
  AgentRuntime/Prompting/Templates/ # 2.8
  Verify/                           # 4.x
  Flow/                             # 1.3
  Rollout/                          # 0.8
  ExecPolicy/                       # 0.6
  Hooks/                            # 0.7

src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/
  AgentRuntime/Tools/Browser*.cs    # O1.3
  AgentRuntime/Tools/ApplyPatchTool.cs  # 0.4
  AgentRuntime/Tools/ListDirectoryTool.cs # 0.10
  AgentRuntime/Tools/Memory*.cs       # 3.3
  AgentRuntime/Tools/ActivateSkillTool.cs # 2.5
  AgentRuntime/Tools/DelegateTool.cs  # 1.5
  Agents/Subagents/*.agent.yaml       # 1.2
  Flows/*.flow.yaml                   # 1.3

.logs/runs/{runId}/
  rollout.jsonl
  events.jsonl
  obscura/
  verify/
  subagents/
  delegations/
  patches/
  exec-audit.jsonl
  review/decisions.jsonl
  context-index/manifest.json

.logs/spaces/{spaceId}/
  worktrees/{memberId}/
  shared/

src/Services/IDE/Libr4.IDE.Application/
  Fleet/                            # 7.1 AgentFleetRegistry
  Spaces/                           # 7.2 Space store, worktree, context bus
  FastContext/                      # 7.3 Index, fusion ranker, prefetcher
  Review/                           # 7.4 Diff aggregator, evidence correlator
  Handoff/                          # 7.5 Export/import run packages
  AgentBackends/                    # 7.6 IAgentBackend adapters

src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/
  AgentRuntime/Tools/SearchCodebaseTool.cs   # 7.3
  AgentRuntime/Tools/GetSymbolContextTool.cs # 7.3

src/Frontend/src/features/IDE/
  AgentBoard/                       # 7.1 Kanban + list
  RunReview/                        # 7.4 diff + evidence
  Spaces/                           # 7.2 space detail
  History/                          # 7.9 session search

docs/
  LIBR4_TERMINAL_AGENT_PLATFORM_MASTER_PLAN.md  # этот файл
```

---

## KPI успеха платформы

| Метрика | Цель |
|---------|------|
| `PipelineStageReached` | >= `Verify` для production runs |
| `patchesApplied` | > 0 при repair |
| Verify evidence | screenshot + console-errors.json при каждом verify |
| Memory persist | playbook hit rate > 30% на повторных ошибках |
| Obscura | 0 зависимость от browser-mcp-server |
| Rollout | 100% tool calls в rollout.jsonl |
| Token efficiency | −80% skill tokens via activate_skill |
| CalorieVision | Completed + verify pass |
| Banking | Completed + verify pass |
| Fleet board latency | status transition visible < 2s P95 |
| Fast Context | `search_codebase` P95 < 800ms warm |
| Diff review | 100% ShipStage runs with `RequireHumanReview` have audit trail |
| Delegate e2e | explore + implementer parallel with 0 write collision |
| PR/CI loop | fleet `PrReady` → `Completed` without manual status refresh |

---

## Связанные документы

- [ObscuraIntegration.md](./ObscuraIntegration.md) — целевой контракт Obscura API
- [REPOSITORY_STUDY_ANALYSIS.md](./REPOSITORY_STUDY_ANALYSIS.md) — анализ внешних репозиториев
- [IDE/CascadeServiceArchitecture.md](./IDE/CascadeServiceArchitecture.md) — Cascade (Windsurf-inspired, уже реализован)
- [Devin Desktop](https://devin.ai/desktop) — референс Command Center, Spaces, ACP, Fast Context
- Agent Runtime README: `src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/README.md`

---

*Последнее обновление: 2026-06-06 (Backend Golden Stack migration complete; см. `BACKEND_LANGUAGE_MIGRATION_CHECKLIST.md`). При реализации пункта — менять `[ ]` на `[x]` и добавлять ссылку на PR/commit в примечание.*
