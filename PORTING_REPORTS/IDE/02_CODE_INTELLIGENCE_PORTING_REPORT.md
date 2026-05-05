# Отчёт о портировании: code_intelligence.py

## 📊 Общая информация

| Параметр | Значение |
|----------|----------|
| **Исходный файл** | `D:\Desktop\freelance_libr4-main\backend\app\api\endpoints\code_intelligence.py` |
| **Размер** | 207.4 KB (5,599 строк) |
| **Язык оригинала** | Python 3.11 + FastAPI |
| **Целевой язык** | C# 12 + ASP.NET Core |
| **Сложность** | 🔴 **КРИТИЧЕСКАЯ** |
| **Оценка времени** | 2-3 недели |

---

## 📋 Что содержит оригинал

### 1. AST Parsing & Symbol Analysis
```python
# Python AST module
import ast
tree = ast.parse(code)
for node in ast.walk(tree):
    if isinstance(node, ast.FunctionDef):
        # Extract function info
```
**Статус:** ❌ Не портировано  
**Замена:** Microsoft.CodeAnalysis.CSharp (Roslyn)

### 2. LSP Integration
```python
from app.services.lsp_service import lsp_service
# Real-time diagnostics
# Type checking
# Go-to-definition
```
**Статус:** ❌ Не портировано  
**Замена:** OmniSharp LSP или custom LSP client

### 3. Semantic Understanding (25+ сервисов!)

| Сервис Python | Файл | C# статус |
|---------------|------|-----------|
| `lsp_service` | `lsp_service.py` | ❌ Нет |
| `architectural_guardrails` | `architectural_guardrails_service.py` | ❌ Нет |
| `ai_commit_service` | `ai_commit_service.py` | ❌ Нет |
| `context_sharing_service` | `context_sharing_service.py` | ❌ Нет |
| `ai_presence_service` | `ai_presence_service.py` | ❌ Нет |
| `agent_context_awareness_service` | `agent_context_awareness_service.py` | ❌ Нет |
| `global_arbitr_service` | `global_arbitr_service.py` | ❌ Нет |
| `collaborative_shadow_workspace_service` | `collaborative_shadow_workspace_service.py` | ❌ Нет |
| `multi_agent_orchestration_service` | `multi_agent_orchestration_service.py` | ❌ Нет |
| `contextual_war_room_service` | `contextual_war_room_service.py` | ❌ Нет |
| `hybrid_llm_service` | `hybrid_llm_service.py` | ❌ Нет |
| `deep_context_awareness_service` | `deep_context_awareness_service.py` | ❌ Нет |
| `semantic_blame_service` | `semantic_blame_service.py` | ❌ Нет |
| `attribution_tagging_service` | `attribution_tagging_service.py` | ❌ Нет |
| `ai_native_canvas_service` | `ai_native_canvas_service.py` | ❌ Нет |
| `artifact_context_store_service` | `artifact_context_store_service.py` | ❌ Нет |
| `context_composer_service` | `context_composer_service.py` | ❌ Нет |
| `agent_orchestration_run_service` | `agent_orchestration_run_service.py` | ❌ Нет |
| `ai_qa_automation_service` | `ai_qa_automation_service.py` | ❌ Нет |
| `ide_code_review_service` | `ide_code_review_service.py` | ❌ Нет |
| `internal_skill_catalog_service` | `internal_skill_catalog_service.py` | ❌ Нет |
| `mcp_adapter_service` | `mcp_adapter_service.py` | ❌ Нет |
| `wasm_sandboxing_service` | `wasm_sandboxing_service.py` | ❌ Нет |
| `workbench_context_service` | `workbench_context_service.py` | ❌ Нет |
| `project_embedding_graph_service` | `project_embedding_graph_service.py` | ❌ Нет |

---

## 📁 Структура доменной модели (C# - ТОЛЬКО СКЕЛЕТ)

### Есть в C# (CodeIntelligence - минимально):
```csharp
// Libr4.AI.Domain.CodeIntelligence/CodeIntelligence.cs
// (предполагаемое наличие, если нет - создать!)
```

**Проверка:** Нужно проверить существование домена CodeIntelligence!

---

## 🔧 Что нужно создать

### 1. Domain Layer (Libr4.AI.Domain.CodeIntelligence) - НОВЫЙ!

```csharp
// Сущности:
CodeAnalysis              // AST analysis result
SymbolDefinition          // Symbol info
CodeReference             // Reference tracking
SemanticGraph             // Project graph
CodeQualityMetrics        // Quality scores
SecurityVulnerability       // Security issues
RefactoringSuggestion     // Refactor proposals
```

### 2. Application Layer (НОВЫЙ!)

```csharp
// Commands:
AnalyzeCodeCommand                    // POST /api/v1/ai/code/analyze
GetSymbolDefinitionQuery              // GET /api/v1/ai/code/symbols/{id}
FindReferencesQuery                   // GET /api/v1/ai/code/references
GetCodeQualityReportQuery             // GET /api/v1/ai/code/quality
GetSecurityScanReportQuery            // GET /api/v1/ai/code/security
GenerateRefactoringSuggestionsCommand // POST /api/v1/ai/code/refactor-suggestions
SuggestBugPredictionsQuery            // GET /api/v1/ai/code/bug-prediction
GenerateTestsCommand                  // POST /api/v1/ai/code/generate-tests
GenerateDocsCommand                   // POST /api/v1/ai/code/generate-docs
```

### 3. Infrastructure Layer (25+ сервисов!)

```csharp
// Core Analysis:
ICodeAnalysisService              // Roslyn-based
ISymbolAnalysisService           // Symbol extraction
ICallGraphService                // Call graph
IDependencyGraphService          // Dependencies

// LSP:
ILSPClientService                // LSP communication
IDiagnosticsService              // Real-time errors
ITypeCheckingService             // Type check
IGoToDefinitionService           // Navigation

// AI Services:
IHybridLLMService                // LLM routing
IDeepContextAwarenessService     // Context understanding
ISemanticBlameService            // Git blame + semantics
IAttributionTaggingService       // Attribution
IContextComposerService          // Context composition
IArtifactContextStoreService     // Artifact storage
IAINativeCanvasService           // Canvas UI

// Collaboration:
IContextSharingService           // Context sharing
IAIPresenceService               // AI presence
ICollaborativeShadowWorkspace    // Shared workspaces
IMultiAgentOrchestrationService  // Multi-agent
IContextualWarRoomService        // War room

// Automation:
IAICommitService                 // AI commits
IAIQaAutomationService           // QA automation
IIDECodeReviewService              // Code review
IAgentOrchestrationRunService      // Orchestration

// Other:
IArchitecturalGuardrailsService  // Architecture check
IGlobalArbitrService             // Arbitration
IInternalSkillCatalogService     // Skills
IMCPAdapterService               // MCP protocol
IWasmSandboxingService           // WASM sandbox
IWorkbenchContextService         // Workbench
IProjectEmbeddingGraphService    // Embeddings
```

### 4. API Layer

```csharp
// Endpoints (15+ новых!):
POST   /api/v1/ai/code/analyze              // AST analysis
GET    /api/v1/ai/code/symbols/{id}        // Symbol info
GET    /api/v1/ai/code/references          // Find references
GET    /api/v1/ai/code/quality             // Quality report
GET    /api/v1/ai/code/security            // Security scan
POST   /api/v1/ai/code/refactor-suggestions // Refactor ideas
GET    /api/v1/ai/code/bug-prediction      // Bug prediction
POST   /api/v1/ai/code/generate-tests      // Test generation
POST   /api/v1/ai/code/generate-docs       // Doc generation
POST   /api/v1/ai/code/commit-suggestion  // AI commit
POST   /api/v1/ai/code/review              // Code review
```

---

## 🛠️ Технологии

### Python → C# маппинг

| Python | C# |
|--------|-----|
| `ast` module | Microsoft.CodeAnalysis.CSharp (Roslyn) |
| `pylsp` | OmniSharp or custom LSP |
| `jedi` | Roslyn Symbol API |
| `tree-sitter` | Tree-sitter .NET bindings |
| `radon` | Custom cyclomatic complexity |
| `bandit` | Security analyzers |
| `mypy` | Roslyn analyzers |

---

## 📊 Зависимости

| Модуль | Зависимость | Критичность |
|--------|-------------|-------------|
| IDEAIAgent | Multi-agent orchestration | 🔴 Высокая |
| IDEGit | Semantic blame | 🔴 Высокая |
| LocalAI | LLM for analysis | 🔴 Высокая |
| IDELSP | Real-time diagnostics | 🔴 Высокая |
| Memory | Context storage | 🟡 Средняя |
| RAGSearch | Knowledge retrieval | 🟡 Средняя |

---

## 📝 План портирования

### Этап 1: Roslyn Setup (Неделя 1)
- [ ] Настроить Microsoft.CodeAnalysis.CSharp
- [ ] AST parsing service
- [ ] Symbol extraction service
- [ ] Call graph builder

### Этап 2: LSP Integration (Неделя 1-2)
- [ ] LSP client service
- [ ] Diagnostics service
- [ ] Type checking service

### Этап 3: AI Services (Неделя 2)
- [ ] HybridLLMService
- [ ] DeepContextAwarenessService
- [ ] ContextComposerService

### Этап 4: Collaboration Services (Неделя 3)
- [ ] ContextSharingService
- [ ] MultiAgentOrchestrationService
- [ ] CollaborativeShadowWorkspace

### Этап 5: API + Testing (Неделя 3)
- [ ] All endpoints
- [ ] Integration tests

---

## 🎯 Acceptance Criteria

- [ ] AST parsing работает (C#/F# поддержка)
- [ ] Symbol definition lookup
- [ ] Find all references
- [ ] Real-time diagnostics via LSP
- [ ] Code quality metrics
- [ ] Security vulnerability detection
- [ ] Refactoring suggestions
- [ ] Bug prediction
- [ ] Test generation
- [ ] Documentation generation
- [ ] AI commit suggestions
- [ ] Code review automation

---

## 📁 Создаваемые файлы

**~40 новых файлов!**

- Domain: 7 файлов
- Application: 10 файлов
- Infrastructure: 25 файлов
- API: 3 файла

---

**Статус:** 🟡 ГОТОВ К ПОРТИРОВАНИЮ (требует Roslyn экспертизы)
