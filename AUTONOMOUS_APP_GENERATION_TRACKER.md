# Autonomous App Generation Tracker

## Цель
Довести пайплайн автономной генерации до стабильного качества не ниже 9/10 на каждом этапе:
- Plan
- Consistency
- Generation
- Build
- Execution
- Fix

## Согласованный план
- [x] Добавить quality-gates и пороги в конфиг
- [x] Добавить enforcement quality-gates в оркестратор
- [x] Добавить anti-stall guardrails (таймаут шага, адаптивный batching)
- [x] Добавить consistency-validator до build/test
- [x] Добавить отдельный build-stage gate перед full run
- [x] Добавить фазовую генерацию (contracts -> models -> services -> controllers -> tests -> infra)
- [x] Добавить compile-check после каждой фазы генерации
- [x] Усилить fixer до cross-file dependency-aware ремонта
- [x] Добавить score telemetry по этапам в report/manifest
- [x] Добавить интеграционные тесты на quality-gates и fail-fast сценарии

## Что сделано в этом цикле
- Введены `AutonomousGenerationOptions` с лимитами генерации.
- В `LlmCodeGenerationService` добавлены:
  - hard-timeout LLM шага,
  - ограничение размера manifest,
  - адаптивное дробление batch при пустом ответе.
- Добавлены:
  - `AutonomousQualityGateOptions`,
  - `IAutonomousQualityGateService` + `AutonomousQualityGateService`,
  - `IAutonomousCodeConsistencyValidator` + `AutonomousCodeConsistencyValidator`.
- В `StartAppGenerationCommandHandler` добавлены fail-fast гейты:
  - после plan,
  - после generation,
  - после consistency,
  - после build-only валидации,
  - после execution,
  - после fix.
- Добавлены интеграционные тесты:
  - fail-fast при phase compile-check (`quality_gate_build_failed`),
  - dependency-aware fixer с фильтрацией нерелевантных файлов.
- Добавлена автооценка run quality:
  - `overallScore` (0-10),
  - `verdict` (excellent/good/acceptable/needs_improvement/critical),
  - breakdown по стадиям (latest/average/evaluations/lastPassed) в report и manifest.
- Выполнен E2E прогон среднего проекта через API host:
  - runId: `fbb9ed5a-0d6a-4287-98b9-a91c44beb2cb`,
  - итог: `Failed (cancelled_by_request)` после 3 итераций,
  - зафиксировано: `totalCommands=6`, build-падения на `dotnet build --configuration Release`.
- Добавлен runtime guard против зависания SSE-стрима в `DockerModelRunnerProvider`:
  - ограничение `MaxSseLinesWithoutContent` (по умолчанию 6000),
  - выброс исключения при длительном потоке без `content/reasoning_content`.
- Прокинута отмена `cancel` до streaming layer:
  - добавлен `AICallCancellationScope` для передачи `CancellationToken` в AI provider,
  - `LlmCodeGenerationService` теперь связывает timeout + внешний `ct` для каждого LLM шага,
  - `DockerModelRunnerProvider` использует токен в `SendAsync` и чтении SSE-стрима.
- Проверка отмены в `generating` выполнена:
  - runId: `54064b9e-f209-44a4-a160-b9e29e4aab53`,
  - результат: `finished_after_cancel_at_0` (ран завершился сразу после cancel).
- Выполнен E2E прогон среднего проекта через API host:
  - runId: `b61e4878-b2bd-4732-8210-370bb013d4be`,
  - итог: `Failed (quality_gate_generation_failed: score=6; reasons=missing_entrypoint,missing_controllers)`,
  - qualityAssessment: `overallScore=8`, `plan=10 (pass)`, `generation=6 (fail)`,
  - до build/test не дошёл (`iterationCount=0`, `totalCommands=0`).
- Усилен generation hardening в `LlmCodeGenerationService`:
  - в `PlanFileManifestAsync` добавлено принудительное включение обязательных ASP.NET файлов (`Program.cs`, `Controllers/*`, `Services/*`, `Models/*`) через `EnsureMandatoryAspNetManifest`,
  - добавлен safety-net этап `EnsureMandatoryAspNetGeneratedFiles`, который детерминированно добавляет минимально рабочие `Program/Controller/Service/Model`, если LLM их не выдал,
  - safety-net файлы теперь попадают в отдельную фазу `safety-net` для дальнейших compile-check и quality gate оценок.
- Усилен `AutonomousQualityGateService` для multi-stack сценариев:
  - generation gate стал stack-aware для `.NET`/`Python`/`Node` (разные требования к entrypoint/project files),
  - проверка data layer стала stack-aware (`DbContext` для .NET, `models/database/alembic` для Python, ORM/models-маркеры для Node),
  - порог `too_few_files` стал адаптивным (для non-.NET снижён baseline с 8 до 5 файлов).

- Реализован план `docs/AGENT_MCP_SKILLS_INTEGRATION_PLAN.md` (первая волна в коде):
  - MCP control plane: `IMcpToolRegistry`, `IMcpExecutionPolicy`, `IMcpSessionRouter`, `IMcpToolInvocationService` + `McpStdioJsonRpc` (построчный JSON-RPC, совместим с Python bridge).
  - Memory plane: `IMemoryStore` / `InMemoryMemoryStore`, инжест, prune по бюджету, аудит в оркестраторе.
  - Skill runtime: `ISkillRegistry`, `ISkillRunner`, `ISkillSelectionStrategy` по стадии.
  - Task graph: `IAgentTaskGraphService`, DAG + recovery-задачи при падении гейта.
  - Security gate: `ISecurityReviewGateService` до structural quality gates.
  - Context packs: `IContextPackBuilder` с записью в память.
  - Манифест **schema 1.3** (MCP, memory, skills, task graph, security).
  - Конфиг: `AutonomousAppGeneration:AgentIntegration` в host `appsettings.json`; stdio по умолчанию выключен.
  - HTTP control plane для MCP на host: `GET /api/ide/app-generation/mcp/tools`, `POST /api/ide/app-generation/mcp/invoke` (опциональный `runId` — аудит на агрегате + `SaveAsync`); `IMcpToolInvocationService.InvokeStandaloneAsync` + лимит параллельных standalone-вызовов (`MaxConcurrentStandaloneInvocations`).
  - Тест: `McpStandalone_ShouldReturnTransportDisabled_WhenStdioOff`.

## Следующие шаги (в приоритете)
1. E2E прогон среднего проекта с фиксацией stage scores в отчете (на перезапущенном host с актуальной сборкой).
2. Усиление quality-gates эвристиками бизнес-логики (не только структурные проверки).
3. Добавить интеграционный тест на мгновенную остановку run при `cancel` в `generating`.
4. E2E: `EnableStdioTransport: true` + вызов `POST .../mcp/invoke` к реальному `libr4-agent-bridge` (или WebApplicationFactory smoke на host).
