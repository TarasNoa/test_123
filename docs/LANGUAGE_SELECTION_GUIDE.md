# Руководство по выбору языка реализации

**Дата:** 30.04.2026
**Статус:** Рекомендации по выбору C#, F#, Rust для компонентов интеграции

---

## Общие принципы выбора языка

### C# - основной язык проекта
**Подходит для:**
- Domain модели (AggregateRoot, Entity, Value Objects)
- Application сервисы и бизнес-логика
- API контроллеры и интеграция с HTTP
- Интеграция с существующими C# сервисами
- Complex state transitions и workflows
- Database operations (EF Core)
- Dependency injection и IoC

**Преимущества:**
- Единая кодовая база с существующим проектом
- Полная интеграция с .NET ecosystem
- Отличная поддержка IDE и tooling
- Легкая интеграция с существующими сервисами

### F# - функциональный язык
**Подходит для:**
- Алгоритмическая логика и вычисления
- Data processing и transformation pipelines
- Graph алгоритмы и traversals
- ML/статистические вычисления
- Type-safe validation
- Immutable data structures
- Domain-specific languages (DSL)
- State machines и workflow engines
- Mathematical operations

**Преимущества:**
- Type safety и immutability по умолчанию
- Сопоставление с образцом (pattern matching)
- Expression-based syntax
- Отличная поддержка для алгоритмов
- Легкая интеграция с C# через .NET

### Rust - системный язык
**Подходит для:**
- Высокопроизводительные операции
- Browser automation (Obscura уже на Rust)
- Memory management и garbage collection-free операции
- Low-level операции и system programming
- Конкурентность и параллелизм без race conditions
- Взаимодействие с native библиотеками
- CPU-intensive задачи
- Network operations

**Преимущества:**
- Zero-cost abstractions
- Memory safety без garbage collector
- Отличная производительность
- Безопасность concurrency (borrow checker)
- Совместимость с C через FFI

---

## Рекомендации по компонентам

### 1. agentsys - AgentOrchestrationPipelineService

#### 1.1 Gated Phases
**Рекомендация:** C#

**Компоненты:**
- `GatedPhase.cs` - Domain модель → **C#**
- `QualityGate.cs` - Domain модель → **C#**
- `QualityGateService.cs` - Application сервис → **C#**
- `RunQualityGateCommand.cs` - CQRS Command → **C#**
- `RunQualityGateHandler.cs` - CQRS Handler → **C#**

**Обоснование:**
- Domain модели должны быть на C# для интеграции с существующей Domain layer
- Application сервисы интегрируются с существующими C# сервисами
- State transitions и workflow логика хорошо ложится на C#
- Интеграция с AgentOrchestration (уже на C#)

#### 1.2 Certainty Levels
**Рекомендация:** C# (Domain), F# (алгоритмы расчета)

**Компоненты:**
- `CertaintyLevel.cs` - Domain модель → **C#**
- `AgentDecision.cs` - Domain модель → **C#**
- `DecisionTrackingService.cs` - Application сервис → **C#**
- `CertaintyCalculator.fs` - Алгоритм расчета уверенности → **F#**

**Обоснование:**
- Domain модели на C# для интеграции
- Алгоритм расчета уверенности (confidence scoring, probability calculations) → F#
- F# отлично подходит для математических вычислений
- Легкая интеграция через .NET

#### 1.3 Pipeline Execution Engine
**Рекомендация:** C# (Core), F# (Workflow engine)

**Компоненты:**
- `IPipelineExecutionEngine.cs` - Interface → **C#**
- `PipelineExecutionEngine.cs` - Core implementation → **C#**
- `PipelineExecutionResult.cs` - Domain модель → **C#**
- `WorkflowEngine.fs` - Workflow execution logic → **F#**

**Обоснование:**
- Core implementation на C# для интеграции с существующими сервисами
- Workflow engine с state machine → F# (pattern matching, expression-based)
- F# отлично подходит для workflow engines и state machines

---

### 2. autoresearch - AutoresearchService

#### 2.1 Mechanical Verification
**Рекомендация:** C# (Domain/Service), Rust (Code Execution)

**Компоненты:**
- `VerificationStep.cs` - Domain модель → **C#**
- `MechanicalVerificationPlan.cs` - Domain модель → **C#**
- `MechanicalVerificationService.cs` - Application сервис → **C#**
- `CodeExecutionModule.rs` - Выполнение кода для верификации → **Rust**

**Обоснование:**
- Domain модели и сервисы на C# для интеграции
- Code execution в изолированной среде → Rust (безопасность, производительность)
- Rust отлично подходит для sandboxed code execution
- Можно интегрировать с Obscura (уже на Rust)

#### 2.2 Automatic Rollback
**Рекомендация:** C#

**Компоненты:**
- `RollbackCheckpoint.cs` - Domain модель → **C#**
- `RollbackOperation.cs` - Domain модель → **C#**
- `RollbackService.cs` - Application сервис → **C#**

**Обоснование:**
- Domain модели на C# для интеграции
- Rollback логика интегрируется с ShadowWorkspace (C#)
- File operations через C# API

#### 2.3 Research Orchestration
**Рекомендация:** C# (Domain/Service), F# (Task decomposition algorithms)

**Компоненты:**
- `ResearchTask.cs` - Domain модель → **C#**
- `ResearchSubtask.cs` - Domain модель → **C#**
- `AutoresearchService.cs` - Application сервис → **C#**
- `TaskDecompositionAlgorithm.fs` - Алгоритм декомпозиции → **F#**

**Обоснование:**
- Domain модели и сервисы на C# для интеграции
- Алгоритм декомпозиции исследовательских задач → F# (graph algorithms, recursion)
- F# отлично подходит для tree/graph traversal

---

### 3. OpenMemory - CognitiveMemoryService

#### 3.1 Multi-Sector Memory
**Рекомендация:** C# (Domain), F# (Memory management algorithms)

**Компоненты:**
- `MemorySector.cs` - Domain модель → **C#**
- `SectorMemory.cs` - Domain модель → **C#**
- `CognitiveMemorySystem.cs` - Domain модель → **C#**
- `CognitiveMemoryService.cs` - Application сервис → **C#**
- `MemoryEvictionAlgorithm.fs` - Алгоритм eviction → **F#**
- `MemoryCompressionAlgorithm.fs` - Алгоритм компрессии → **F#**

**Обоснование:**
- Domain модели на C# для интеграции
- Memory eviction algorithms (LRU, LFU) → F# (functional, immutable)
- Memory compression algorithms → F# (data processing)
- F# отлично подходит для data structures and algorithms

#### 3.2 Temporal Knowledge Graph
**Рекомендация:** C# (Domain), F# (Graph algorithms), Rust (Storage)

**Компоненты:**
- `KnowledgeNode.cs` - Domain модель → **C#**
- `KnowledgeEdge.cs` - Domain модель → **C#**
- `TemporalKnowledgeGraph.cs` - Domain модель → **C#**
- `TemporalKnowledgeGraphService.cs` - Application сервис → **C#**
- `GraphTraversalAlgorithm.fs` - Graph traversal → **F#**
- `KnowledgeGraphStorage.rs` - High-performance storage → **Rust**

**Обоснование:**
- Domain модели на C# для интеграции
- Graph traversal algorithms (DFS, BFS, shortest path) → F# (recursive, functional)
- High-performance graph storage → Rust (memory efficiency, performance)
- F# отлично подходит для graph algorithms
- Rust для storage layer (performance, memory safety)

---

### 4. claude-context - SemanticCodeSearchService

#### 4.1 Semantic Code Indexing
**Рекомендация:** C# (Domain/Service), F# (Embedding calculations), Rust (Vector storage)

**Компоненты:**
- `CodeIndex.cs` - Domain модель → **C#**
- `IndexedFile.cs` - Domain модель → **C#**
- `SemanticCodeIndexingService.cs` - Application сервис → **C#**
- `EmbeddingCalculator.fs` - Embedding calculations → **F#**
- `VectorIndexStorage.rs` - Vector index storage → **Rust**

**Обоснование:**
- Domain модели и сервисы на C# для интеграции
- Embedding calculations и similarity search → F# (mathematical operations)
- Vector index storage (HNSW, ANN) → Rust (performance, memory efficiency)
- F# отлично подходит для mathematical operations
- Rust для high-performance vector operations

#### 4.2 Semantic Search Query
**Рекомендация:** C# (Domain/Service), F# (Similarity algorithms)

**Компоненты:**
- `SemanticSearchQuery.cs` - Domain модель → **C#**
- `SearchResult.cs` - Domain модель → **C#**
- `SemanticCodeSearchService.cs` - Application сервис → **C#**
- `SimilarityAlgorithm.fs` - Similarity calculation → **F#**

**Обоснование:**
- Domain модели и сервисы на C# для интеграции
- Similarity algorithms (cosine similarity, dot product) → F# (mathematical)
- F# отлично подходит для vector operations

#### 4.3 Context Retrieval
**Рекомендация:** C#

**Компоненты:**
- `ContextRetrievalService.cs` - Application сервис → **C#**
- `RetrievedContext.cs` - Domain модель → **C#**

**Обоснование:**
- Domain модель на C# для интеграции
- Service интегрируется с SemanticCodeSearchService (C#)
- Логика retrieval хорошо ложится на C#

---

### 5. gnhf - GnhfOrchestratorService

#### 5.1 Automatic Commits
**Рекомендация:** C#

**Компоненты:**
- `AutomaticCommitPolicy.cs` - Domain модель → **C#**
- `CommitCandidate.cs` - Domain модель → **C#**
- `GnhfOrchestratorService.cs` - Application сервис → **C#**

**Обоснование:**
- Domain модели на C# для интеграции
- Service интегрируется с GitAutomation (C#)
- Git operations через LibGit2Sharp (C# library)

#### 5.2 Automatic Rollback
**Рекомендация:** C#

**Компоненты:**
- `RollbackPolicy.cs` - Domain модель → **C#**
- `RollbackOperation.cs` - Domain модель → **C#**

**Обоснование:**
- Domain модели на C# для интеграции
- Rollback логика интегрируется с GitAutomation (C#)
- Git operations через LibGit2Sharp (C# library)

---

### 6. superpowers - TDD Cycle Integration

#### 6.1 TDD Cycle
**Рекомендация:** C#

**Компоненты:**
- `TDDCycle.cs` - Domain модель → **C#**
- `TDDCycleService.cs` - Application сервис → **C#**

**Обоснование:**
- Domain модель на C# для интеграции
- Service интегрируется с AppGenerationOrchestrator (C#)
- Test execution через Terminal (C#)

#### 6.2 Brainstorming Phase
**Рекомендация:** C# (Domain), F# (Idea generation algorithms)

**Компоненты:**
- `BrainstormingSession.cs` - Domain модель → **C#**
- `BrainstormingService.cs` - Application сервис → **C#**
- `IdeaGenerationAlgorithm.fs` - Алгоритм генерации идей → **F#**

**Обоснование:**
- Domain модель на C# для интеграции
- Idea generation algorithms (divergent thinking, clustering) → F# (data processing)
- F# отлично подходит для generative algorithms

---

### 7. context-engineering-kit - Reflexion, SDD, SADD

#### 7.1 Reflexion Plugin
**Рекомендация:** C# (Domain/Service), F# (Feedback analysis)

**Компоненты:**
- `ReflexionCycle.cs` - Domain модель → **C#**
- `ReflexionService.cs` - Application сервис → **C#**
- `FeedbackAnalyzer.fs` - Анализ feedback → **F#**

**Обоснование:**
- Domain модель на C# для интеграции
- Service интегрируется с AgentMemorySystem (C#)
- Feedback analysis (pattern recognition) → F# (pattern matching)

#### 7.2 Spec-Driven Development (SDD)
**Рекомендация:** C#

**Компоненты:**
- `TaskSpecification.cs` - Domain модель → **C#**
- `SDDService.cs` - Application сервис → **C#**

**Обоснование:**
- Domain модель на C# для интеграции
- Service интегрируется с TaskDecomposition и AutonomousAppGeneration (C#)
- Specification parsing и validation → C#

#### 7.3 Subagent-Driven Development (SADD)
**Рекомендация:** C# (Domain/Service), F# (Evaluation algorithms)

**Компоненты:**
- `SADDExecution.cs` - Domain модель → **C#**
- `SADDService.cs` - Application сервис → **C#**
- `SubagentEvaluationAlgorithm.fs` - Алгоритм оценки → **F#**

**Обоснование:**
- Domain модель на C# для интеграции
- Service интегрируется с MultiAgentOrchestration (C#)
- Evaluation algorithms (comparison, ranking) → F# (data processing)

---

### 8. claude-skills - Adaptation of Key Skills

#### 8.1 Skill System
**Рекомендация:** C#

**Компоненты:**
- `AgentSkill.cs` - Domain модель → **C#**
- `SkillLibraryService.cs` - Application сервис → **C#**
- `Skills/` - Адаптированные навыки → **C#**

**Обоснование:**
- Domain модель на C# для интеграции
- Service интегрируется с существующими Agent (C#)
- Skills - это промпты и metadata, хорошо ложатся на C#

---

### 9. andrej-karpathy-skills - Four Principles

#### 9.1 Four Principles Integration
**Рекомендация:** C#

**Компоненты:**
- `KarpathyGuidelines.cs` - Domain модель → **C#**
- `KarpathyGuidelineEnforcer.cs` - Application сервис → **C#**

**Обоснование:**
- Domain модель на C# для интеграции
- Service интегрируется с SeniorRolePrompts (C#)
- Guidelines - это промпты и metadata, хорошо ложатся на C#

---

### 10. antigravity-awesome-skills - Key Skills and Bundle System

#### 10.1 Bundle System
**Рекомендация:** C#

**Компоненты:**
- `SkillBundle.cs` - Domain модель → **C#**
- `BundleManager.cs` - Application сервис → **C#**

**Обоснование:**
- Domain модель на C# для интеграции
- Service интегрируется с SkillLibraryService (C#)
- Bundle management - это dependency resolution, хорошо ложится на C#

---

## Итоговая таблица выбора языка

| Компонент | Domain | Application Service | Алгоритмы | Storage/Performance |
|-----------|--------|-------------------|-----------|---------------------|
| agentsys - Gated Phases | C# | C# | - | - |
| agentsys - Certainty Levels | C# | C# | F# | - |
| agentsys - Pipeline Engine | C# | C# | F# | - |
| autoresearch - Verification | C# | C# | - | Rust |
| autoresearch - Rollback | C# | C# | - | - |
| autoresearch - Orchestration | C# | C# | F# | - |
| OpenMemory - Multi-Sector | C# | C# | F# | - |
| OpenMemory - Knowledge Graph | C# | C# | F# | Rust |
| claude-context - Indexing | C# | C# | F# | Rust |
| claude-context - Search | C# | C# | F# | - |
| claude-context - Retrieval | C# | C# | - | - |
| gnhf - Commits | C# | C# | - | - |
| gnhf - Rollback | C# | C# | - | - |
| superpowers - TDD | C# | C# | - | - |
| superpowers - Brainstorming | C# | C# | F# | - |
| context-engineering-kit - Reflexion | C# | C# | F# | - |
| context-engineering-kit - SDD | C# | C# | - | - |
| context-engineering-kit - SADD | C# | C# | F# | - |
| claude-skills | C# | C# | - | - |
| andrej-karpathy-skills | C# | C# | - | - |
| antigravity-awesome-skills | C# | C# | - | - |

---

## Приоритеты реализации

### Фаза 1: C# Domain Models и Services (основа)
Начать с реализации всех Domain моделей и Application сервисов на C#, так как:
- Это основа для интеграции
- Быстрая разработка
- Полная интеграция с существующим кодом

### Фаза 2: F# Алгоритмы (оптимизация)
Добавить F# алгоритмы для:
- Certainty calculations
- Task decomposition
- Memory eviction/compression
- Graph traversal
- Embedding calculations
- Similarity algorithms
- Idea generation
- Feedback analysis
- Subagent evaluation

### Фаза 3: Rust High-Performance Components (оптимизация)
Добавить Rust компоненты для:
- Code execution sandbox
- Vector index storage
- Knowledge graph storage

---

## Рекомендации по интеграции F# в проект

### Структура проекта F#
```
src/Services/IDE/Libr4.IDE.Domain.Algorithms/
  ├── CertaintyCalculator.fs
  ├── WorkflowEngine.fs
  ├── TaskDecompositionAlgorithm.fs
  ├── GraphTraversalAlgorithm.fs
  ├── EmbeddingCalculator.fs
  ├── SimilarityAlgorithm.fs
  ├── IdeaGenerationAlgorithm.fs
  ├── FeedbackAnalyzer.fs
  └── SubagentEvaluationAlgorithm.fs

src/Services/AI/Libr4.AI.Domain.Algorithms/
  ├── MemoryEvictionAlgorithm.fs
  └── MemoryCompressionAlgorithm.fs
```

### Интеграция F# с C#
```csharp
// C# code calling F#
using Libr4.IDE.Domain.Algorithms;

var certainty = CertaintyCalculator.Calculate(decisionData);
```

```fsharp
// F# code exposing to C#
namespace Libr4.IDE.Domain.Algorithms

module CertaintyCalculator =
    let Calculate (data: DecisionData) : float =
        // calculation logic
```

---

## Рекомендации по интеграции Rust в проект

### Структура проекта Rust
```
obscura/  // уже существует
  ├── src/
  │   ├── code_execution.rs  // новый модуль
  │   └── ...
  └── Cargo.toml

knowledge-graph-storage/  // новый проект
  ├── src/
  │   ├── lib.rs
  │   ├── storage.rs
  │   └── graph.rs
  └── Cargo.toml

vector-index-storage/  // новый проект
  ├── src/
  │   ├── lib.rs
  │   ├── index.rs
  │   └── vector.rs
  └── Cargo.toml
```

### Интеграция Rust с C#
Использовать:
- C FFI (Foreign Function Interface)
- P/Invoke в C#
- Или через gRPC/HTTP (если изолированный сервис)

```csharp
// C# calling Rust via P/Invoke
[DllImport("libknowledge_graph.so")]
public static extern IntPtr CreateGraphStorage();

[DllImport("libknowledge_graph.so")]
public static extern void AddNode(IntPtr storage, string nodeId, byte[] data);
```

---

## Заключение

**Основной подход:**
1. **C#** для всех Domain моделей, Application сервисов, API (80% кода)
2. **F#** для алгоритмической логики, математических вычислений, graph algorithms (15% кода)
3. **Rust** для high-performance компонентов, code execution sandbox, storage (5% кода)

**Порядок реализации:**
1. Фаза 1: Все C# компоненты (Domain + Services)
2. Фаза 2: F# алгоритмы (оптимизация)
3. Фаза 3: Rust компоненты (оптимизация)

Это обеспечивает:
- Быстрый старт с C#
- Гибкость для оптимизации с F#
- Высокую производительность с Rust
- Минимальные риски интеграции
