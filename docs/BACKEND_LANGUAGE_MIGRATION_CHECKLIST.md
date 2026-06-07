# Backend Language Migration Checklist

> **Стек:** C# (Skeleton) · F# (Brain) · Rust (Muscle) · C++ (Muscle — точечно, low-level / экосистема)  
> **Принцип:** публичный C# API и DI не ломаем; алгоритмы → F#; performance/isolation → Rust **по умолчанию**; C++ только где Rust не лучший выбор или выигрывает зрелая C++-библиотека. Bridge + integration tests обязательны для любого native слоя.  
> **Обновлено:** 2026-06-06 · **Статус Agent Platform:** ✅ **MIGRATION COMPLETE** (Waves 1–6, CI)

## Легенда статусов

| Статус | Значение |
|--------|----------|
| `[ ]` | Не начато |
| `[~]` | В работе |
| `[x]` | Мигрировано, тесты зелёные |
| `[—]` | Остаётся на C# (orchestration) |
| `[R]` | Целевой Rust (muscle по умолчанию) |
| `[C++]` | Целевой C++ (muscle точечно — см. критерии Wave 6) |

---

## Wave 1 — Agent Platform Brain (IDE AutonomousAppGeneration)

| # | Модуль / файлы | Сейчас | Цель | Статус | Примечание |
|---|----------------|--------|------|--------|------------|
| 1.1 | `Context/RepoGraph/LanguageImportParsers.cs` | C# | **F#** | `[x]` | `LanguageImportParsers.fs` |
| 1.2 | `Context/RepoGraph/TopologicalSorter.cs` | C# | **F#** | `[x]` | `TopologicalSorter.fs` |
| 1.3 | `Context/RepoGraph/IRepoGraphBuilder.cs` (engine) | C# | **F#** + C# bridge | `[x]` | `RepoGraphEngine.fs`; models остаются C# |
| 1.4 | `Context/RepoGraph/RepoGraphModels.cs` | C# | **C#** | `[—]` | DTO для DI/interop |
| 1.5 | `Context/RepoGraph/RepoContextFormatter.cs` | C# | **C#** | `[—]` | formatting orchestration |
| 1.6 | `Context/RepoGraph/RepoGraphBatchOrdering.cs` | C# | **C#** | `[—]` | вызывает IRepoGraphBuilder |
| 1.7 | `AgentRuntime/Patching/UnifiedDiffParser.cs` | C# | **F#** | `[x]` | `UnifiedDiffParser.fs` |
| 1.8 | `AgentRuntime/Patching/PatchApplicator.cs` | C# | **F#** | `[x]` | exact/fuzzy/3-way production merge |
| 1.9 | `FastContext/FastContextFusionRanker.cs` | C# | **F#** | `[x]` | RRF fusion ranker |
| 1.10 | `AgentRuntime/Playbook/RepairPlaybookSignature.cs` | C# | **F#** | `[x]` | signature hash + keywords |

**Новый проект:** `Libr4.IDE.AutonomousAppGeneration.Algorithms.FSharp`

---

## Wave 2 — Agent Runtime State & Memory Brain

| # | Модуль | Сейчас | Цель | Статус |
|---|--------|--------|------|--------|
| 2.1 | `AgentRuntime/Core/AgentSession.cs` (turn state) | C# | **F#** state machine | `[x]` | `AgentSessionTurnMachine.fs` + bridge; parse via `AgentResponseParser.fs` |
| 2.2 | `Memory/Hermes/*` scoring/eviction | C# | **F#** | `[x]` | `HermesMemoryScoring.fs` |
| 2.3 | `Memory/Search/HybridSessionSearchService.cs` fusion | C# | **F#** | `[x]` | `ReciprocalRankFusion.fs` (RRF core) |
| 2.4 | `Context/Compaction/*` budget scoring | C# | **F#** | `[x]` | `HeuristicSemanticCompactor.fs` |
| 2.5 | `Context/Fragments/ContextFragmentManager.cs` | C# | **F#** | `[x]` | `ContextFragmentBudget.fs` |
| 2.6 | `ModelRouting/*` circuit scoring | C# | **F#** | `[x]` | `RoleModelCircuit.fs` |
| 2.7 | `MetaAgent/AgentSpecEvolutionService.cs` rules | C# | **F#** | `[x]` | `AgentSpecEvolution.fs` |

---

## Wave 3 — Rust Muscle (performance / isolation)

| # | Модуль | Сейчас | Цель | Статус |
|---|--------|--------|------|--------|
| 3.1 | `Runtime/*` sandbox execution | C# + gRPC | **Rust** `libr4-sandbox-executor` | `[x]` | `RustSandboxExecutorBridge` + `RustBackedIsolatedRuntime` |
| 3.2 | `AgentRuntime/Delegation/ProcessDelegationWorkerHost.cs` | C# | **Rust** worker isolate | `[x]` | `libr4-delegation-worker` + bridge |
| 3.3 | `FastContext/RipgrepCodeIndex.cs` | C# subprocess | **Rust** ripgrep wrapper | `[x]` | `libr4-fast-context` cdylib + bridge |
| 3.4 | `Memory/Qdrant/*` embed batch | C# HTTP | **Rust** `libr4-embeddings` gRPC | `[x]` | `RustEmbeddingsGrpcClient` (Provider=grpc) |
| 3.5 | `AgentRuntime/Rollout/*` NDJSON append | C# | **Rust** high-throughput writer | `[x]` | `libr4-rollout-writer` cdylib + bridge |
| 3.6 | Obscura browser plane | C# gRPC client | **Rust** external service | `[x]` | `BrowserAutomationGrpcClient` → `obscura/crates/browser-automation`; CDP fallback |
| 3.7 | `Gateway/AiDrivenRateLimiter.cs` | C# | **Rust** sidecar | `[x]` | `libr4-gateway-core` risk + token bucket |
| 3.8 | `Gateway/CircuitBreakerMiddleware.cs` | C# | **Rust** sidecar | `[x]` | `RustCircuitBreakerState` + bridge |

---

## Wave 4 — Остаётся C# (orchestration skeleton)

| Область | Проекты | Статус |
|---------|---------|--------|
| API / Endpoints | `*.Api`, `AutonomousAppGeneration/Api/*` | `[—]` |
| DI / Host | `DependencyInjection.cs`, `*.Host/Program.cs` | `[—]` |
| Handlers (thin) | `StartAppGenerationCommandHandler*.cs` | `[—]` |
| Pipeline stages | `Services/Pipeline/*` | `[—]` |
| EF / SQLite stores | `Persistence/*`, Fleet index, Hermes store | `[—]` |
| LLM HTTP clients | `LlmCodeGenerationService`, providers | `[—]` |
| Git / GitHub | `GitHubActionsDispatch/*`, Spaces git | `[—]` |
| Tests | `tests/Libr4.IntegrationTests` | `[—]` |

---

## Wave 5 — Прочие сервисы (не Agent Platform)

| Домен | F# (Algorithms) | Rust | C# skeleton |
|-------|-----------------|------|-------------|
| Auth | `Auth.Domain.Algorithms` `[x]` | `auth_crypto` `[x]` | Api/App/Infra `[—]` |
| Payments | 6× F# projects `[x]` | 8× crates `[x]` | `[—]` |
| Matching | `Algorithms.FSharp` `[x]` | crawler+embeddings `[x]` | `[—]` |
| Chat | 6× F# `[x]` | webrtc+media `[x]` | `[—]` |
| Tasks | 9× F# `[x]` | — | `[—]` |
| Trading | 4× F# `[x]` | — | `[—]` |
| Shared | — | WebSocket+Security bridges `[x]` | Kernel/Contracts `[—]` |

---

## Wave 6 — C++ Muscle (точечно: low-level / экосистема)

> **Не отдельный «future tooling» слой**, а те же **мышцы**, что Rust — но выбираем C++ только когда это оправдано.  
> **Rust остаётся default** для новых native модулей (cdylib, sidecar, изоляция, IO-heavy).

### Когда Rust (Wave 3), когда C++ (Wave 6)

| Критерий | **Rust** (default muscle) | **C++** (opt-in muscle) |
|----------|---------------------------|-------------------------|
| Новый native модуль «с нуля» | ✅ sandbox, delegation, rollout, gateway, embeddings | — |
| FFI / изоляция / memory safety в hot path | ✅ | — |
| Нужна **зрелая C/C++ библиотека** с официальным C API | — | ✅ tree-sitter, ONNX Runtime EP, LLVM/libclang |
| **GPU / vendor EP / CUDA-DirectML** без лишних обёрток | — | ✅ ONNX Runtime C++ API, DirectML EP |
| **Low-level** доступ к runtime/OS, где Rust-обёртки слабые или нестабильны | — | ✅ точечно, с ADR |
| Команда / CI уже покрывает Rust toolchain | ✅ | добавляем CMake/vcpkg **только** под конкретный crate |

**Правило:** перед Wave 6 пунктом — однострочное обоснование «почему не Rust» + C# bridge (P/Invoke / C API) + smoke test. Без обоснования — идём в Rust.

### Кандидаты (Wave 6 complete)

| # | Компонент | Почему C++, а не Rust | Статус |
|---|-----------|----------------------|--------|
| 6.1 | Tree-sitter / IDE parse (python, js, csharp) | официальный **C API** tree-sitter; grammar ecosystem | `[x]` | `native/cpp/libr4-tree-sitter` + `CppTreeSitterBridge` + `CppTreeSitterAnalysisSidecar` |
| 6.2 | ONNX Runtime **Direct EP** (GPU) | зрелый **C++** EP stack; меньше indirection чем Rust ORT bindings | `[x]` | `native/cpp/libr4-ort-ep` + `CppOrtEpBridge` + `CppOrtEmbeddingService` (`Memory:Embeddings:Provider=ort-cpp`) |
| 6.3 | LLVM / libclang repo analysis | **libclang** C API; industry standard для C++ AST | `[x]` | `native/cpp/libr4-libclang` + `CppLibClangBridge` + `RepoGraphLibClangAugmenter` |

**Инфра:** `native/cpp/` · `build/Libr4.CppNative.targets` · `scripts/build-cpp-native.ps1` · CI backend job (cmake + libclang-dev) — **Wave 6 complete**.

---

## Orphan F# (оформить или удалить)

| Файл | Действие | Статус |
|------|----------|--------|
| `Chat.Domain.FSharp/ChatDomain.fs` | merge в `Chat.Domain.Algorithms` | `[x]` |
| `Tasks.Domain.TasksExtended.FSharp/TaskExtended.fs` | delete (дубликат C# `TasksExtended`) | `[x]` |
| `Collaboration.Domain.Algorithms/DocumentAlgorithms.fs` | fsproj + solution | `[x]` |
| `IDE.Domain.Algorithms/ErrorClassifier.fs` | включён в fsproj | `[x]` | + `Libr4.IDE.Domain.Algorithms.Tests` 8/8 |

---

## Критерии «production ready» для каждой миграции

- [x] F#/Rust модуль с полной логикой (не stub) — Wave 1
- [x] C# bridge сохраняет публичный контракт — Wave 1
- [x] Существующие integration tests зелёные — Wave 1 (8/8)
- [x] Новые unit tests для F# модуля — `Algorithms.FSharp.Tests` 34/34 (incl. FsCheck properties)
- [x] DI registration без изменения call sites — Wave 1
- [x] Чеклист в этом файле → `[x]` — Wave 1

---

## CI / Native artifacts

| # | Задача | Статус |
|---|--------|--------|
| CI.1 | `ci.yml`: build `rust/` + `libr4-sandbox-executor` before dotnet test | `[x]` |
| CI.2 | MSBuild `build/Libr4.RustNative.targets` → copy cdylib to `$(OutputPath)` | `[x]` |
| CI.3 | `scripts/build-rust-native.ps1` local helper | `[x]` |
| CI.4 | FsCheck property tests: PatchApplicator round-trip, RRF id preservation | `[x]` |
| CI.5 | `rust-embeddings` CI job: binary + Docker image build | `[x]` |
| CI.6 | `scripts/smoke-embeddings.ps1` — local gRPC smoke (requires running service) | `[x]` |
| CI.7 | `scripts/smoke-rust-native.ps1` + `RustNativeBridgesSmokeTests` (all Wave 3 cdylibs) | `[x]` |
| CI.8 | C++ tree-sitter: `cmake` in CI + `CppTreeSitterBridgeSmokeTests` | `[x]` |
| CI.9 | C++ ORT EP: MiniLM download in CI + `CppOrtEpBridgeSmokeTests` | `[x]` |
| CI.10 | C++ libclang: `libclang-dev` in CI + `CppLibClangBridgeSmokeTests` | `[x]` |
| CI.11 | `scripts/smoke-cpp-native.ps1` — local C++ bridge smoke | `[x]` |
| CI.12 | `scripts/Run-PlatformFullSmoke.ps1` — wiring audit + platform integration + agent stack + OpenRouter live gen | `[x]` |

---

## Журнал выполнения

| Дата | Wave | Что сделано |
|------|------|-------------|
| 2026-06-06 | 1 | Создан checklist; старт Wave 1.1–1.8 (RepoGraph + Patching → F#) |
| 2026-06-06 | 1 | **Wave 1 завершена:** RepoGraph, Patching, FusionRanker, RepairPlaybookSignature → F#; bridge + 8 integration tests green |
| 2026-06-06 | 2 | Wave 2.2: HermesMemoryScoring → F# (`Memory/HermesMemoryScoring.fs`) |
| 2026-06-06 | 2 | Wave 2.3–2.7: RRF, HeuristicCompactor, ContextFragmentBudget, RoleModelCircuit, AgentSpecEvolution → F#; 21 integration tests green |
| 2026-06-06 | 2 | **Wave 2 завершена:** + AgentResponseParser/ReasoningChannelParser (2.1); 22 integration tests green |
| 2026-06-06 | 3 | Wave 3.3–3.5: `libr4-fast-context`, `libr4-rollout-writer`, `RustEmbeddingsGrpcClient` + C# bridges |
| 2026-06-06 | 3 | Wave 3.1–3.2, 3.7–3.8: sandbox shell exec, delegation worker, gateway circuit/rate-limit → Rust |
| 2026-06-06 | 3 | **Wave 3 завершена:** sandbox-executor build fix; 48 integration tests green; 3.6 Obscura gRPC plane confirmed |
| 2026-06-06 | 4 | Orphan F# cleanup: ChatDomain→Algorithms, Collaboration fsproj, ErrorClassifier enabled, TaskExtended duplicate removed |
| 2026-06-06 | 4 | **F# unit tests:** `Algorithms.FSharp.Tests` 13 tests (TopoSort, RRF, Patch, Playbook signature) |
| 2026-06-06 | 5 | CI + MSBuild: `build/Libr4.RustNative.targets` copies cdylibs; `ci.yml` builds rust workspace + sandbox; F# tests +8 (RepoGraph, imports) |
| 2026-06-06 | 6 | FsCheck properties + Wave2 unit tests (Hermes, Compactor, FragmentBudget, Circuit, AgentSpec, ResponseParser); 34/34 green |
| 2026-06-06 | 7 | `ErrorClassifierTests` 8/8; CI job `rust-embeddings` (gRPC binary + Docker) |
| 2026-06-06 | 7 | Embeddings hardening: default gRPC port 50061; optional smoke test `RustEmbeddingsGrpcClientIntegrationTests` |
| 2026-06-06 | 8 | Master plan sync (Golden Stack section); docker-compose agent profile → grpc embeddings; `smoke-embeddings.ps1` |
| 2026-06-06 | 9 | **Post-migration hardening:** `RustNativeBridgesSmokeTests` 5/5; bridge JSON snake_case fixes (fast-context, gateway risk); delegation error JSON on rc≠0 |
| 2026-06-06 | — | **Концепция Wave 6 уточнена:** C++ = muscle как Rust, opt-in при low-level / лучшей C++ экосистеме; Rust остаётся default |
| 2026-06-06 | 6.1 | **Tree-sitter C++ muscle:** `libr4_tree_sitter` cdylib, C# bridge, `CppTreeSitterAnalysisSidecar` → `IRustAnalysisSidecar`; smoke tests 3/3 |
| 2026-06-06 | 6.2 | **ONNX ORT EP C++ muscle:** `libr4_ort_ep` + `CppOrtEpBridge` + `CppOrtEmbeddingService` (`Provider=ort-cpp`); dynamic ONNX inputs; CI MiniLM smoke |
| 2026-06-06 | 6.3 | **libclang C++ muscle:** `libr4_libclang` + RepoGraph #include augmentation; F# cpp regex fallback; smoke tests; **Wave 6 complete** |

