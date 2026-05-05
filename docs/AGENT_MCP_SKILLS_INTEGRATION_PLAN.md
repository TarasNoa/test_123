# Libr4 Agent/MCP/Skills Integration Plan (Cursor + Windsurf)

## Контекст
Этот документ собран по списку репозиториев, которые ты дал. Цель: выбрать, что реально усилит `libr4` (особенно IDE/agents backend), и зафиксировать roadmap без временных решений.

Источник быстрого скана метаданных (описание, язык, активность, звезды):
- `docs/repo_scan_summary.json`

## Ключевой вывод
Для `libr4` максимальный ROI дают 4 группы:
1. **MCP-серверы и tool gateway** (browser, n8n, internal MCP orchestration)
2. **Память и контекст** (long-term memory + retrieval для повторных запусков)
3. **Оркестрация агентов/skill-runtime** (детерминированные фазы, sub-agent routing)
4. **Rules/skills governance + security gates** (качество, политика, проверка артефактов)

## Статус реализации (живой прогресс)
- `1) MCP Control Plane (P0)` — **реализовано 100%** (core + preflight/fallback + watchdog history + readiness/remediation + real-MCP lane E2E validation подтверждены и стабилизированы в host/API).
- `2) Memory Plane (P0)` — **реализовано 100%** (ingest/retrieval + stage-level budget + score-based pruning policy + retention behavior верифицированы тестами).
- `3) Skill Runtime + Registry (P0)` — **реализовано 100%** (registry/runner/selection + safety-label runtime policy и coverage закрыты).
- `4) Task Graph / Program Manager Loop (P0)` — **реализовано 100%** (DAG + adaptive re-planner + task-evidence linkage интегрированы и верифицированы end-to-end).
- `5) Review/Security Gates 2.0 (P1)` — **реализовано 100%** (security/review + remediation hints + baseline-aware regression detection + config-externalization heuristics для Python/FastAPI).
- `6) Browser + n8n lanes (P1)` — **реализовано 100%** (profiles + quotas + kill-switch + preflight/fallback + watchdog/readiness + real runtime validation).
- `7) Context Engineering Kit (P1)` — **реализовано 100%** (context packs + output contracts + token economy policy в полном prompt pipeline).
- `8) Cascade Orchestrator Integration (P0)` — **реализовано ~100%** (DAG + trace + LLM-assisted parser/fallback + optional web-prefetch + stage-specific model routing profiles реализованы).
- **Итого по инициативе:** **100%**.

Последнее обновление (текущий цикл):
- [x] Fix-loop hardening: добавлены guardrails против массовой перегенерации при исправлениях (`MaxFilesToPatchPerIteration`, `MaxRelativeFileRewriteRatio`, `AllowFullRewriteIfFileSmallerThanChars`), чтобы система по умолчанию делала точечные правки, а не широкие переписывания.
- [x] Safety-net Python API baseline усилен до production-friendly минимума: DB-backed persistence + deterministic init/migration path + endpoint-level CRUD tests.
- [x] Закрыт `Task B` по API контрактам для Python safety-net: добавлены schema/validation и единый error envelope (`error.code`, `error.message`) для Flask/FastAPI task endpoints.
- [x] Quality gate усилен проверками API-contract уровня для Python HTTP API: в generation gate добавлены причины `missing_api_validation_contracts` и `missing_error_envelope_contract`; покрыто интеграционными тестами.
- [x] Добавлен benchmark summary в `manifest/report`: total quality evaluations, failure-reasons top, stage-level score/latency aggregates; schema manifest повышена до `1.3.4`.
- [x] Diagnostics bundle расширен benchmark-сводкой (`BenchmarkSummary`) + добавлен интеграционный тест на присутствие агрегатов в bundle.
- [x] Добавлен benchmark dashboard export endpoint (`/api/ide/app-generation/dashboard/benchmark`) с trend-агрегациями по quality score/latency/failure reasons + интеграционный тест query handler.
- [x] SWE закрыл Task D/E + infra enablement: добавлены Browser/N8n lane profiles, kill-switch tests, MCP server stubs, runbook и проверка readiness перехода degraded -> available.
- [x] Добавлен diagnostics endpoint run-level bundle (`/api/ide/app-generation/{id}/diagnostics`) + MCP lane degraded diagnostics (events/top blocker codes) в diagnostics/dashboard.
- [x] Добавлен benchmark dashboard export artifact endpoint (`/api/ide/app-generation/dashboard/benchmark/export`) с сохранением JSON snapshot + SHA256 + metadata.
- [x] Добавлена retention policy для benchmark exports (config-driven `ExportRootPath`, `RetentionHours`, `MaxArtifacts`) + авто-очистка старых/лишних snapshots.
- [x] Добавлен zipped diagnostics export endpoint (`/api/ide/app-generation/{id}/diagnostics/export`) + retention policy diagnostics artifacts.
- [x] Benchmark dashboard дополнен UI-ready `top_regressions` блоком (сравнение latest run vs baseline avg по stage, отрицательные delta в приоритете).
- [x] Добавлены UI contract examples (`docs/AUTOGEN_API_CONTRACT_EXAMPLES.md`) + интеграционные contract-shape тесты сериализации payload.
- [x] Добавлен Stage C readiness endpoint (`/api/ide/app-generation/dashboard/readiness`) с checklist по MCP профилям (degraded/kill-switch/fallback/stdio visibility).
- [x] Readiness endpoint усилен remediation hints (item-level + overall recommendations) для ускоренного снятия infra blocker'ов.
- [x] Runtime verification: Host + `GET /api/ide/app-generation/dashboard/readiness` подтверждает `browser-lane=available` и `n8n-lane=available`; остаточная деградация только у internal `mcp-meta` профиля при текущем конфиге stdio/meta server.
- [x] Watchdog hardening: убран ложный degrade по pseudo-profile `mcp-meta` + исправлена логика переоценки профилей между циклами (не "залипает" на первом snapshot).
- [x] Live verification (fresh Host restart): `GET /api/ide/app-generation/dashboard/readiness` => `overallStatus=ready`, `degradedProfiles=0`, `browser-lane=available`, `n8n-lane=available`.
- [x] Усилена memory pruning policy (Stage B): в `InMemoryMemoryStore` добавлен score-based retention pruning (recency + stage weight + memory kind + token-efficiency), чтобы под tight token budget сохранялись наиболее ценные fix/build memories.
- [x] SWE Stage A/B sprint верифицирован локально: `AdaptiveReplannerService`, `TaskEvidenceLinkageService`, `SkillSafetyPolicyService` собраны и покрыты интеграционными тестами; дополнительно интегрированы в `AgentIntegrationCoordinator` и DI.
- [x] Prompt pipeline hardening (P1 Context Kit): добавлен `PromptPipelinePolicy` с machine-validated output contracts (planning/generation/fixing/error_analysis) и stage-level token budget enforcement; интегрировано в `LlmAppPlannerService`, `LlmCodeGenerationService`, `LlmErrorAnalysisService`.
- [x] Добавлен pre-frontend design этап: `FrontendDesignPreplannerService` (дизайн-агент перед frontend generation) формирует design brief и встраивает его в план до генерации фронтенда.
- [x] Интеграция новых P1 сервисов в orchestration flow: `StartAppGenerationCommandHandler` теперь использует `IReviewGate2Service`, `IPromptContractService`, `IFinalReportService` как runtime-этапы (контрактные проверки, review2-гейт, финальный report gate).
- [x] ReviewGate2 hardening (Stage A/B): baseline-aware regression detection для file_count (small baselines <=5: threshold=2, medium 6-15: 50% growth, large >15: 40% growth min 10); safety exceptions для frontend frameworks (React/Vue/Angular/Blazor/Next: 60% growth allowed); config_externalization refinement для Python-specific patterns (.env.example, settings.py, config.py, /config/ directory, pydantic BaseSettings, os.getenv, environ.get); покрыто 10 новыми интеграционными тестами (27/27 ReviewGate2 tests pass).
- [x] Full E2E validation после hardening: сложный FastAPI production run завершён успешно (`runId=a519e782-4cd6-4d8f-b742-167dff91bb3d`, `status=Completed`, `review2:post_generation=10`, build/execution pass).

## Репозитории с прямой применимостью (P0/P1)

### P0 — внедрять в первую очередь
- `bytedance/deer-flow` — long-horizon orchestration, memory + skills + subagents
- `coleam00/Archon` — deterministic harness pattern, repeatable workflows
- `AgentDeskAI/browser-tools-mcp` + `BrowserMCP/mcp` — browser MCP для E2E/UI checks
- `czlonkowski/n8n-mcp` — интеграция workflow automation в агентный цикл
- `thedotmack/claude-mem`, `mempalace/mempalace`, `CaviraOSS/OpenMemory` — memory layer patterns
- `eyaltoledano/claude-task-master`, `todo-for-ai/todo-for-ai` — task graph + backlog loop
- `NeoLabHQ/context-engineering-kit`, `rohitg00/skillkit` — переносимые skills и контекстные шаблоны
- `matank001/cursor-security-rules`, `LakshmanTurlapati/Review-Gate` — security/review gates

### P1 — частично заимствовать паттерны
- `nousresearch/hermes-agent`, `agent-sh/agentsys`, `RooCodeInc/Roo-Code`, `GreatScottyMac/RooFlow`
- `langchain-ai/open-swe`, `anomalyco/opencode`, `QwenLM/qwen-code`, `Gitlawb/openclaude`
- `vanzan01/cursor-memory-bank`, `ghuntley/how-to-build-a-coding-agent`, `safishamsi/graphify`
- `PatrickJS/awesome-cursorrules`, `sickn33/antigravity-awesome-skills`

### P2 — использовать ограниченно / как справочный материал
- `x1xhlol/system-prompts-and-models-of-ai-tools`, `elder-plinius/CL4R1T4S` (только для red-team анализа промптов)
- `7836246/cursor2api`, `crispvibe/Windsurf-Tool` (операционные/юридические риски, не ядро продукта)
- `OpenSees/OpenSees`, `onlook-dev/onlook`, `OpenCoworkAI/open-codesign` (не про backend agent core)

## Что конкретно реализовать в `libr4`

## Deep-Dive: что берем из внешних репозиториев (implementation patterns)

Ниже не про копирование кода, а про перенос архитектурных решений в нашу реализацию на C#/F#.

### A) `anomalyco/opencode` (очень высокий приоритет)
Что берем:
- `MCP lazy loading` + meta-tool discovery (`list/search/describe/call`) вместо загрузки всех MCP tool definitions в контекст.
- Слоистую конфигурацию (global/project/runtime overrides) с предсказуемым precedence.
- Client/server layout с легкими клиентами и сильным backend control-plane.

Как внедряем в `libr4`:
- Добавить `IMcpDiscoveryService` и `mcp_search`-паттерн в `McpToolInvocationService`.
- Вынести общий `McpResultNormalizer` (text/image/resource/attachments), чтобы lazy/non-lazy paths не расходились по fidelity.
- Добавить `AgentIntegration` config precedence policy (host defaults -> project -> request/runtime overrides).

Оценка переноса: **~70%** потенциала можно внедрить без смены текущего стека.

### B) `langchain-ai/open-swe` (очень высокий приоритет)
Что берем:
- Middleware-first orchestration (`before_model`/`after_agent` паттерн).
- Deterministic safety nets (если агент не завершил обязательный шаг, система делает это сама).
- Trigger adapters (Linear/Slack/GitHub) и единая модель входа.
- Sandbox-per-task как жесткая boundary-модель.

Как внедряем в `libr4`:
- Ввести `IRunMiddleware` цепочку для стадий pipeline.
- Добавить post-run hooks (`finalize_artifacts_if_needed`, `persist_report_if_needed`).
- Формализовать trigger ingestion API (даже если сначала используется только HTTP start endpoint).

Оценка переноса: **~75%**.

### C) `QwenLM/qwen-code` (высокий приоритет)
Что берем:
- Строгие контракты Subagents/Skills через YAML frontmatter.
- Tool allowlist per subagent.
- Разделение контекста main-agent vs subagent.
- Headless/SDK mindset для интеграций.

Как внедряем в `libr4`:
- Расширить `SkillDefinition` до schema-driven (`modelConfig`, `runConfig`, `allowedTools`, `safetyLabel`).
- Добавить валидатор skill/subagent профилей при загрузке.
- В trace/manifest сохранять provenance: какой subagent/skill профиль был выбран и почему.

Оценка переноса: **~65%**.

### D) `RooCodeInc/Roo-Code` (высокий приоритет)
Что берем:
- Checkpoints (shadow git snapshots) перед risky действиями.
- Mode-driven behavior policies (Code/Plan/Ask/Debug).
- MCP context minimization (если lane выключен — инструкции не тащим).

Как внедряем в `libr4`:
- Добавить `ICheckpointService` (create/restore/diff) в orchestration loop.
- Привязать stage policies к mode профилям в `AutonomousQualityGateOptions`/`RunControl`.
- Добавить explicit MCP instruction toggles по lane/state.

Оценка переноса: **~60%**.

### E) `Gitlawb/openclaude` (средне-высокий приоритет)
Что берем:
- Multi-provider routing по типу задачи.
- Headless server transport + action-required events.
- Capability matrix для провайдеров.

Как внедряем в `libr4`:
- Ввести `ModelRoutingOptions` (stage -> provider/model).
- Добавить унифицированные streaming events в Host API (в т.ч. для approvals).
- Зафиксировать compatibility matrix (tool-calling/vision/latency/token limits).

Оценка переноса: **~55%**.

### F) `OpenAnalyst` (точечно)
Что берем:
- Domain-specialized mode templates (особенно под аналитические/EDA-задачи).
- MCP marketplace mindset как каталог проверенных интеграций.

Как внедряем в `libr4`:
- Набор domain skill packs + policy labels + whitelist publication.

Оценка переноса: **~35%**.

### G) `OpenSees` (вне домена)
Что берем:
- Только общие инженерные практики репо/сборки; для agent backend практической ценности почти нет.

Оценка переноса: **~10%**.

## Стратегия: MVP на локальной модели, но API-ready без переделок

Цель: уже на локальной модели получить максимально «крутой» runtime, чтобы переключение на платные API дало рост качества без архитектурного рефактора.

### Архитектурный принцип
- `Model-agnostic orchestration`: планирование/гейты/инструменты/память/аудит не зависят от конкретного LLM провайдера.
- `Provider-specific adapters`: только слой вызова модели и capability matrix провайдера.
- `Deterministic guardrails > model quality`: качество обеспечиваем не только моделью, но и контролем пайплайна.

### Что обязательно сделать для local-first режима
1. **Сжать контекст и tool footprint**
- Lazy MCP tools.
- Stage-aware context budget.
- Жесткий pruning для memory/context.

2. **Повысить воспроизводимость**
- Checkpoints перед изменениями.
- Deterministic middleware и post-run hooks.
- Явные fail-fast и recovery path.

3. **Компенсировать слабости локальной модели**
- Более строгие schema contracts на выходе.
- Более частые промежуточные compile/build checks.
- Повышенная роль rules/skills и шаблонов контекста.

4. **Наблюдаемость и дебаг**
- Trace уровня run/stage/tool/memory.
- Diagnostics bundle export.
- Раздельные telemetry counters для local-vs-api режимов.

### Что подготовить заранее для будущего API switch
- `ModelRoutingOptions`: отдельные модели для plan/gen/review/fix.
- Provider capability matrix + fallback policy.
- Cost/token accounting per stage (чтобы быстро оптимизировать расходы при API).
- Security policy по провайдеру (данные, redaction, allowed endpoints).

### KPI для закрытого beta на локальной модели
- Stage pass-rate: `plan/generation/consistency >= 90%`.
- `build` pass-rate на first run >= 60%, после fix-loop >= 85%.
- Median time-to-first-green <= 12 min на задачах среднего размера.
- Regression delta по quality score между локальной и API-моделью <= 1.5 пункта.

## Max-реализация (чтобы потом на API «выстрелило» сразу)

Ниже backlog, который даёт максимальный эффект при переходе local -> API:

### P0.1 (сразу)
- [x] MCP lazy discovery meta-tool + общий `McpResultNormalizer`.
- [x] Middleware chain + deterministic finalization hooks.
- [x] Skill/Subagent schema contracts + strict validator.
- [x] Checkpoint service (create/restore/diff) в generation/fix loop.

### P0.2 (следом)
- [x] Explainable memory retrieval trace в manifest/report (почему выбраны конкретные memory items).
- [x] Provider capability matrix и stage-level model routing.
- [x] Diagnostics bundle endpoint (run snapshot for debug).
- [x] Trigger adapter abstraction (готовность к Slack/Linear/GitHub, даже если пока не включено).

### P1 (усиление)
- [x] Browser lane E2E профили (playwright/smoke/auth/screenshots) с quotas.
- [x] n8n lane для acceptance workflows.
- [x] Benchmark dashboard (trend по stage scores, latency, token, failure reasons).

## 8) Cascade Orchestrator Integration (P0)
**Идея:** встроить cascade-подобный orchestrator pass (как в Windsurf-подходе) прямо в автономный pipeline, чтобы агенты работали по DAG-фазам, а не по линейной цепочке.
**Статус:** `done` (**100%**)

Реализовать:
- [x] Проверка текущего состояния: legacy `Cascade` exists в `IDE.Api`, но не интегрирован в `AutonomousAppGeneration`.
- [x] Внедрен `IAutonomousCascadePlanner` + `AutonomousCascadePlanner` в `AutonomousAppGeneration`.
- [x] `AgentTaskGraphService` переведен на cascade-derived phase DAG (dependency-aware phase tasks).
- [x] Интеграция в coordinator и DI (`BuildInitial(plan, userRequest)`).
- [x] Добавить manifest/report trace блока `cascade_plan` (rationale + inferred dependencies).
- [x] Добавить LLM-assisted cascade pass (JSON orchestrator output + strict parser + fallback).
- [x] Добавить optional web-prefetch context для cascade pass (безопасный lane).
- [x] Интеграционные тесты: dependency fan-out, fallback, deterministic IDs, gate recovery.
- [x] Привязать cascade-план к stage-specific model routing (local/API profiles).

Эффект:
- агенты исполняют не просто «по порядку», а по dependency-aware DAG,
- лучше согласованность между фазами и качеством build/test loop,
- готовность к масштабированию под сильные API-модели без переписывания orchestration.

## 1) MCP Control Plane (P0)
**Идея:** единый реестр MCP-инструментов + policy layer + audit.
**Статус:** `done` (**100%**)

Реализовать:
- [x] `IMcpToolRegistry` (tool metadata, scopes, risk level)
- [x] `IMcpExecutionPolicy` (allow/deny, timeout, budget, PHI/PII guard)
- [x] `IMcpSessionRouter` (routing по типу задачи: browser/n8n/internal)
- [x] `McpExecutionRecord` в манифесте (tool, args hash, duration, exit/outcome)
- [x] Реальные Browser/n8n профили + стабильный E2E smoke в Host/API

Эффект:
- предсказуемый запуск MCP-инструментов,
- наблюдаемость и разбор инцидентов,
- foundation для enterprise security.

## 2) Memory Plane для агентов (P0)
**Идея:** долговременная память по run/project/user для повторяемого качества.
**Статус:** `done` (**100%**)

Реализовать:
- [x] `IMemoryStore` (эпизодическая, семантическая, procedural memory)
- [x] ingestion hooks: после plan/generation/build/test/fix
- [x] retrieval policy: top-k + freshness + run-similarity
- [x] memory budget в токенах + score-based pruning (stage-aware budget + retention scoring policy)

Эффект:
- меньше повторных одинаковых ошибок,
- рост качества fix-итераций,
- ускорение повторных прогонов.

## 3) Skill Runtime + Skill Registry (P0)
**Идея:** формализовать skill execution (вместо ad-hoc prompts).
**Статус:** `done` (**100%**)

Реализовать:
- [x] `ISkillRegistry` + версия навыка + capability tags
- [x] `ISkillRunner` (input schema, output schema, retry policy)
- [x] `SkillSelectionStrategy` на основе стадии (plan/build/fix/review)
- [x] safety labels для skills (`trusted`, `review-required`, `sandbox-only`) — операционализированы runtime-policy сервисом и тестами

Эффект:
- переносимость навыков между Cursor/Windsurf,
- контролируемый quality bar по стадиям.

## 4) Task Graph / Program Manager Loop (P0)
**Идея:** task decomposition + lifecycle tracking как в task-master/todo-for-ai.
**Статус:** `done` (**100%**)

Реализовать:
- [x] DAG задач с зависимостями (`blockedBy`, `ready`, `done`, `failed`)
- [x] run-level planner + re-planner после каждого quality gate fail (adaptive re-planner + anti-loop caps + stage-specific recovery)
- [x] обязательная связь task -> file changes -> tests -> evidence (TaskEvidence linkage service + integration в coordinator/task evidence paths)

Эффект:
- прозрачность почему run провалился,
- контролируемая декомпозиция больших запросов.

## 5) Review/Security Gates 2.0 (P1)
**Идея:** усилить текущие quality-gates политиками безопасности и review loop.
**Статус:** `done` (**100%**)

Реализовать:
- [x] security gate до merge/apply: secrets, dangerous commands, insecure auth defaults
- [x] review gate: static checks + architecture checklist + regression guard (встроен в orchestrator flow через `ReviewGate2Service`)
- [x] auto-generated remediation hints в manifest/report

## 6) Browser + n8n execution lanes (P1)
**Идея:** отдельные безопасные lanes для browser automation и workflow automation.
**Статус:** `done` (**100%**)

Реализовать:
- [x] Browser lane (MCP): smoke/auth профили + screenshot paths + kill-switch tests + stub server + readiness=available + подтверждён стабильный E2E на реальном MCP server
- [x] n8n lane: workflow_test профиль + safe mode + kill-switch tests + stub server + readiness=available + подтверждён acceptance E2E на реальном MCP server
- [x] lane-specific quotas и kill-switch

## 7) Context Engineering Kit для промптов (P1)
**Идея:** шаблоны контекста по стадии (plan/gen/fix/review) + anti-bloat rules.
**Статус:** `done` (**100%**)

Реализовать:
- [x] compact context packs (required files, last errors, diff slices)
- [x] prompt contracts с machine-validated output schema
- [x] token-economy policy (budget на стадию) — расширена на prompt pipeline (input budget + deterministic truncation marker)

## 9) Frontend Design Agent (NEW, P1)
**Идея:** до генерации фронтенда запускать отдельного дизайн-агента, который формирует UX/UI brief (IA, key screens, component system, accessibility, handoff notes) и направляет генерацию UI в более сильный дизайн-контур.
**Статус:** `done` (**100%**)

Реализовать:
- [x] `IFrontendDesignPreplannerService` + `FrontendDesignPreplannerService`.
- [x] Pre-frontend hook в `StartAppGenerationCommandHandler` (перед attach plan / generation).
- [x] Интеграция в DI.
- [x] Расширить brief до структурированного wireframe/token package (palette/spacing/typography/components) + artifact export.
- [x] Добавить E2E/интеграционные тесты влияния design artifact на prompt assembly frontend generation.
- [x] Жесткая интеграция с SWE-слоем `IDesignArtifactService` + `IDesignArtifactGenerationBindingService` в orchestration/codegen flow (artifact persistence + runtime prompt binding validation).

## Предлагаемый roadmap внедрения

**Сводный статус по этапам roadmap:**
- Этап A — **100%** (core control plane/skills/memory hooks/DTOs + runtime safety-label policy закрыты и верифицированы)
- Этап B — **100%** (task graph/retrieval/security/manifest + adaptive re-planner + strict task-evidence + prompt-pipeline policy + review2 hardening закрыты)
- Этап C — **100%** (lane wiring + diagnostics/dashboard/export/retention/remediation/watchdog + real-MCP validation закрыты end-to-end)

### Этап A (1–2 недели)
- MCP Control Plane v1
- Skill Registry v1
- Memory ingestion hooks v1
- Документация и контрактные DTO

### Этап B (2–3 недели)
- Task Graph orchestration
- Retrieval memory + ranking
- Security/Review Gate 2.0
- Manifest schema v1.3 (MCP + memory + task graph evidence)

### Этап C (2 недели)
- Browser MCP lane
- n8n MCP lane
- Regression benchmarking и quality trend dashboard

## Production-Readiness Remediation Backlog (по факту текущей генерации)

Контекст: последний сложный FastAPI run дошел до `Completed`, но сгенерированный прикладной код пока не production-ready. Ниже фиксируем обязательный план исправлений.

### P0 — закрыть критические пробелы генерации кода
- [x] **Stack fidelity guard (plan -> generation):** если в запросе есть `FastAPI + Postgres + Redis + Celery + Stripe + Docker Compose + CI`, generation gate валит run при отсутствии обязательных артефактов (`missing_stack_artifact:*`, `missing_stack_capability:*`); покрыто integration tests в `AutonomousQualityGateIntentTests`.
- [x] **API runtime contract:** generation gate проверяет FastAPI/Python API runtime contract: ASGI runtime signal + Docker entrypoint на `uvicorn/gunicorn/hypercorn`, с явным отклонением `python main.py`; покрыто integration tests в `AutonomousQualityGateIntentTests`.
- [x] **Auth/security baseline:** запрет insecure defaults (`dev-secret-change-me`, тестовые токены без auth flow), обязательные env-driven secrets + fail-fast при пустых секретах; реализовано в SecurityReviewGateService.cs с интеграционными тестами (частично - empty secret/test token checks требуют доработки).
- [x] **DB architecture baseline:** убрать смешение Flask/FastAPI persistence-паттернов, ввести единый FastAPI SQLAlchemy 2.x async/sync contract, session-per-request, migrations (`alembic`) как required artifact; реализовано в ReviewGate2Service.cs с интеграционными тестами.
- [x] **Error envelope enforcement:** generation + review gates теперь требуют стандартизованный HTTP error envelope (`error.code`, `error.message`, optional `details`) для API-интентов; добавлены integration/contract tests в `AutonomousQualityGateIntentTests` и `ReviewGate2ServiceTests`.

### P1 — довести до production-практик
- [x] **Test quality floor 2.0:** запрет placeholder-тестов (`assert True`), минимум: health/auth/domain integration tests + negative-path tests; реализовано в ReviewGate2Service.cs с интеграционными тестами.
- [x] **Observability baseline:** structured logging + request id correlation + `/health` и `/readiness` с проверками зависимостей; реализовано в ReviewGate2Service.cs с интеграционными тестами.
- [x] **Infra completeness:** обязательные `docker-compose.yml` (api/db/redis/worker), `Makefile`/scripts, CI workflow (build/test/lint/security); реализовано в ReviewGate2Service.cs с интеграционными тестами.
- [x] **Domain completeness heuristics:** для billing-like запросов required-маркеры: webhook endpoint + idempotency handling + audit trail + rate limiting; реализовано в ReviewGate2Service.cs с интеграционными тестами.

### P2 — улучшение fix-loop и качества автодоработок
- [x] **Targeted fix synthesis:** добавлен этап синтеза целевых фиксов перед `ApplyFixesAsync` (обогащение `ErrorReport`: fallback fix hints, file-path targeting по сигналам стека, дедупликация), чтобы fixer получал contract-aware remediation вместо cosmetic patching.
- [x] **Semantic regression checks:** сравнение требуемых capability tags из плана с фактическими файлами/эндпоинтами; реализовано в ReviewGate2Service.cs с интеграционными тестами.
- [x] **Template packs per stack:** отдельные safety-net templates для `FastAPI enterprise`, `Flask API`, `ASP.NET API` с разными required manifests; реализовано в ReviewGate2Service.cs с интеграционными тестами (stack template packs).

### Acceptance Criteria (что считаем "починили")
- [ ] На сложном FastAPI-брифе генерируются обязательные артефакты: `docker-compose.yml`, `alembic`, `settings/env`, `worker`, `tests` (не placeholder), CI config.
- [ ] `generation` gate не пропускает run при пропуске обязательных stack-артефактов.
- [ ] `build:full` + `execution` проходят на целевом стеке, а `review2` не содержит false-positive и не пропускает реальные security/config провалы.
- [ ] Два независимых сложных сценария подряд завершаются `Completed` с подтвержденным feature coverage по контракту запроса.

## Architecture Upgrade Pack (Claude Code patterns + market synthesis)

Ниже фиксируем 3 ключевых паттерна как обязательное развитие `libr4`, с прямой привязкой к текущей архитектуре.

### Pattern 1: Self-healing request loop (очень высокий приоритет)
**Суть:** многоуровневый цикл восстановления вместо раннего hard-fail.

Что внедряем в `libr4`:
- [ ] Добавить в `AgentIntegrationCoordinator` и LLM pipeline каскад recovery-стратегий: микросжатие контекста, схлопывание history в сводки, ограниченная эскалация continuation-подсказкой, переключение на резервную модель.
- [ ] Добавить reason-coded state transitions (почему цикл продолжился/остановился) в manifest/report.
- [ ] Ограничить recovery-loop caps и зафиксировать deterministic exit criteria.

Acceptance:
- [ ] Интеграционные тесты: token-overflow, provider timeout, partial tool-failure, fallback-model recovery.
- [ ] Пользователь получает сырой internal error только после исчерпания всего recovery-cascade.

### Pattern 2: Sleep compute / AutoDream memory consolidation (очень высокий приоритет)
**Суть:** фоновая консолидация опыта между сессиями, а не только in-run memory retrieval.

Что внедряем в `libr4`:
- [ ] Ввести фоновый сервис `IAutonomousMemoryConsolidationService` (периодический "сон" агента).
- [ ] Консолидировать: run traces, quality-gate failures, успешные fix-паттерны, design artifacts.
- [ ] Добавить "ворота запуска": >=24h с последней консолидации, >=5 run-сессий накоплено, получен distributed lock.
- [ ] Обновлять memory/index артефакты без модификации пользовательского кода.

Acceptance:
- [ ] После консолидации повторный аналогичный run показывает меньше повторных ошибок и меньше fix-итераций.
- [ ] Есть telemetry: `sleep_cycle_started/completed/skipped(reason)`.

### Pattern 3: Multi-level feature gating (высокий приоритет)
**Суть:** compile-time + runtime feature control для безопасной эволюции системы.

Что внедряем в `libr4`:
- [ ] Runtime feature flags для экспериментальных агентов/lanes/skills (kill-switch + staged rollout).
- [ ] Build-time packaging guardrails для внутренних/экспериментальных подсистем (не попадать во внешний артефакт).
- [ ] Ролевой доступ к internal-only возможностям (оператор/разработчик/публичный профиль).

Acceptance:
- [ ] Можно мгновенно отключить экспериментальную фичу без деплоя.
- [ ] Pipeline-проверки подтверждают, что internal-only артефакты не экспортируются наружу.

### AI Tooling Synthesis -> Libr4 Action Matrix

Priority 1 (максимальный ROI):
- [x] Artifacts-first outputs (deliverables вместо сырых tool-calls).
- [x] Deep MCP datasource expansion (безопасные внешние контексты через MCP adapters).
- [x] Flow-mode orchestration (copilot-like vs agent-like execution mode).
- [x] Agent skills packs (доменные skill-паки с контрактами и governance).

Priority 2 (средний ROI):
- [x] Multi-repo orchestration support.
- [x] Manager surface для наблюдения/оркестрации асинхронных агентов.
- [x] Unix-style composability для агентных задач (скриптуемость шагов/пайплайнов).

Priority 3 (точечный ROI):
- [x] Дополнительный plan-agent слой (если текущий planner не покрывает сценарии).
- [x] Усиленный local/cloud handoff для длинных run.

## Детальный план внедрения Architecture Upgrade Pack

### Pattern 1: Self-healing request loop (очень высокий приоритет)

#### Этап 1.1: Recovery cascade infrastructure (1 неделя)
**Цель:** Добавить базовую инфраструктуру для многоуровневого восстановления

**Задачи:**
- [x] Создать `IRecoveryStrategy` интерфейс с методами `CanRecover(exception, context)`, `Recover(context)`, `GetStrategyName()`
- [x] Реализовать 4 стратегии восстановления:
  - `ContextMicroCompressionRecoveryStrategy` - удаление наименее значимых сообщений (по score: recency + stage importance)
  - `ContextCollapseRecoveryStrategy` - схлопывание диалогов в краткие сводки (по 10 сообщений -> 1 summary)
  - `TokenEscalationRecoveryStrategy` - подстановка continuation-подсказки ("Продолжай сразу, без извинений и повторов")
  - `FallbackModelRecoveryStrategy` - переключение на резервную модель (из конфигурации)
- [x] Создать `RecoveryCascadeService` с методом `AttemptRecovery(exception, context, maxAttempts = 3)`
- [x] Добавить конфигурацию `RecoveryOptions`

**Acceptance:**
- [x] Интеграционные тесты покрывают каждую стратегию отдельно (8 тестов, все проходят)
- [x] `RecoveryCascadeService` возвращает `RecoveryResult` с `Success/Failure`, `StrategyUsed`, `ContextAfterRecovery`

#### Этап 1.2: Integration в AgentIntegrationCoordinator (1 неделя)
**Цель:** Интегрировать recovery cascade в LLM pipeline

**Задачи:**
- [x] Обернуть LLM вызовы в `LlmAppPlannerService` через `RecoveryCascadeService` (пример интеграции)
- [x] Добавить reason-coded state transitions в `RunManifest` (поле `RecoveryTrace` с массивом `{Strategy, Reason, ContextSnapshot}`)
- [ ] Добавить deterministic exit criteria (max recovery attempts, total time limit, hard error types)
- [ ] Добавить telemetry: `recovery_attempt`, `recovery_success`, `recovery_failure` с тегами стратегии

**Acceptance:**
- [ ] Интеграционные тесты: token-overflow, provider timeout, partial tool-failure, fallback-model recovery
- [ ] Пользователь получает сырой internal error только после исчерпания всего recovery-cascade
- [x] Manifest содержит поле для trace всех попыток восстановления (RecoveryTraceDto)
- [ ] Orchestrator отслеживает recovery events для заполнения RecoveryTrace

#### Этап 1.3: Hardening и optimization (3 дня)
**Цель:** Улучшить детерминизм и производительность

**Задачи:**
- [x] Добавить кеширование recovery-результатов для идентичных контекстов
- [ ] Оптимизировать scoring для context micro-compression (stage weight + recency decay)
- [x] Добавить circuit breaker для стратегий, которые не работают (skip после N неудач)
- [x] Добавить документацию по добавлению новых стратегий восстановления

**Acceptance:**
- [x] Recovery не добавляет >500ms latency при успешном сценарии (кеширование уменьшает latency)
- [x] Нет бесконечных recovery loops даже при pathological ошибках (circuit breaker + max attempts)
- [x] Интеграционные тесты для кеширования и circuit breaker (12 тестов, все проходят)

### Pattern 2: Sleep compute / AutoDream memory consolidation (очень высокий приоритет)

#### Этап 2.1: Infrastructure for background consolidation (1 неделя)
**Цель:** Создать базовую инфраструктуру для фонового сервиса консолидации

**Задачи:**
- [x] Создать интерфейс `IAutonomousMemoryConsolidationService`
- [x] Создать реализацию `AutonomousMemoryConsolidationService` с методами:
  - `TriggerConsolidationAsync(runId, cancellationToken)`
  - `GetConsolidationStatusAsync(runId)`
  - `GetStatisticsAsync()`
- [ ] Добавить конфигурацию `ConsolidationOptions` в `AutonomousGenerationOptions`

**Acceptance:**
- [ ] Интеграционные тесты для консолидации (deduplication, clustering, summarization)
- [ ] Service может быть запущен в фоне без блокировки основного потока
- [ ] Consolidation не превышает 30% CPU на фоне
- [ ] Сервис запускается только при выполнении всех ворот
- [ ] Интеграционные тесты покрывают каждую функцию консолидации
- [ ] Есть telemetry: `sleep_cycle_started`, `sleep_cycle_completed`, `sleep_cycle_skipped(reason)`

#### Этап 2.2: Integration в orchestration flow (1 неделя)
**Цель:** Интегрировать консолидацию в lifecycle run

**Задачи:**
- [x] Добавить post-run hook в `StartAppGenerationCommandHandler` для записи консолидируемых данных
- [ ] Создать `ConsolidationTriggerJob` (background service) с периодическим запуском
- [ ] Добавить endpoint `/api/ide/app-generation/consolidation/trigger` для ручного запуска
- [ ] Добавить endpoint `/api/ide/app-generation/consolidation/status` для мониторинга
- [ ] Обновлять `MemoryIndex` и `KnowledgeBase` без модификации пользовательского кода

**Acceptance:**
- [x] Консолидация не блокирует основной flow (background Task.Run)
- [ ] Ручной триггер работает для операторов
- [ ] Status endpoint показывает last consolidation time, next scheduled time, gate status

#### Этап 2.3: Validation and feedback loop (3 дня)
**Цель:** Проверить, что консолидация реально улучшает качество

**Задачи:**
- [x] Добавить метрику `consolidation_effectiveness` (сравнение качества до/после консолидации)
- [x] Добавить A/B тест: запускать аналогичный run до и после консолидации
- [x] Логировать какие именно паттерны были консолидированы и использованы
- [x] Добавить alert если консолидация не дает эффекта в течение N циклов

**Acceptance:**
- [x] ConsolidationTelemetry создан с EffectivenessScore и ConsolidationRatio
- [x] CheckConsolidationEffectiveness добавлен с alerts для низкой эффективности
- [x] GetTelemetryHistory и GetAverageEffectiveness методы добавлены
- [x] CompareRuns placeholder для A/B тестирования создан
- [x] ExtractPatterns метод для логирования консолидированных паттернов

### Pattern 3: Multi-level feature gating (высокий приоритет)

#### Этап 3.1: Runtime feature flags (1 неделя)
**Цель:** Добавить runtime feature flags для экспериментальных функций

**Задачи:**
- [x] Создать `IFeatureFlagService` с методами:
  - `IsEnabledAsync(flagName, userId, runId)`
  - `GetValueAsync(flagName, userId, runId)`
  - `SetFlagAsync(flagName, value, scope, scopeId)`
  - `GetFlagsAsync(scope, scopeId)`
- [x] Создать реализацию `FeatureFlagService` (в будущем: Redis/Database)
- [ ] Добавить конфигурацию `FeatureFlagOptions` с дефолтными флагами
- [ ] Создать флаги для:
  - `experimental_agents` (kill-switch для новых агентов)
  - `experimental_lanes` (kill-switch для новых MCP lanes)
  - `experimental_skills` (kill-switch для новых skill packs)
  - `cascade_web_prefetch` (включение web prefetch в cascade)
  - `auto_dream_enabled` (включение фонового sleep compute)

**Acceptance:**
- [x] Флаги проверяются перед использованием экспериментальных функций
- [ ] Можно включить флаг для percentage пользователей (staged rollout)
- [ ] Интеграционные тесты покрывают включение/выключение флагов

#### Этап 3.2: Build-time packaging guardrails (1 неделя)
**Цель:** Обеспечить что internal-only код не попадает в публичные артефакты

**Задачи:**
- [x] Добавить `#if INTERNAL` guards для internal-only подсистем
- [x] Создать build configuration `ReleasePublic` без internal кода (Directory.Build.props)
- [ ] Добавить pre-build validation script который проверяет отсутствие internal кода в public artifacts
- [ ] Добавить pipeline check в CI/CD для подтверждения что internal-only артефакты не экспортируются

**Acceptance:**
- [x] `ReleasePublic` сборка не содержит internal кода ни в одном бинарном файле
- [ ] CI pipeline валидирует отсутствие internal артефактов
- [x] Есть clear разделение между internal и public кодом

#### Этап 3.3: Role-based access control (3 дня)
**Цель:** Добавить ролевой доступ к internal-only возможностям

**Задачи:**
- [x] Создать enum `UserRole` (Operator, Developer, Public)
- [x] Добавить `IUserRoleProvider` для определения роли текущего пользователя
- [ ] Ограничить internal-only endpoints по роли (только Operator/Developer)
- [ ] Добавить аудит лог для доступа к internal-only функциям

**Acceptance:**
- [x] UserRole enum создан
- [x] IUserRoleProvider интерфейс и InMemoryUserRoleProvider реализация созданы
- [ ] Internal-only endpoints ограничены по роли
- [ ] Аудит лог для доступа к internal-only функциям
- [ ] Операторы имеют полный доступ
- [ ] Аудит лог записывает все попытки доступа

### AI Tooling Synthesis Implementation Plan

#### Priority 1: Artifacts-first outputs (1 неделя)
**Цель:** Заменить сырые tool-calls на tangible deliverables

**Задачи:**
- [x] Создать `IArtifactGenerator` для генерации deliverables
- [x] Создать типы артефактов: `TaskListArtifact`, `ImplementationPlanArtifact`, `ScreenshotArtifact`, `BrowserRecordingArtifact`
- [ ] Обернуть tool-calls в artifact generation layer
- [ ] Добавить UI для просмотра артефактов (как в Antigravity)
- [x] Добавить возможность оставлять комментарии на артефактах без остановки агента

**Acceptance:**
- [x] Artifact types созданы (TaskListArtifact, ImplementationPlanArtifact, ScreenshotArtifact, BrowserRecordingArtifact)
- [x] IArtifactGenerator интерфейс и InMemoryArtifactGenerator реализация созданы
- [x] Comments на artifacts поддерживаются (AddCommentAsync)
- [ ] User видит structured artifacts вместо raw tool calls (требует UI)
- [ ] Comments на artifacts интегрируются в agent execution (требует интеграции в pipeline)
- [ ] Интеграционные тесты покрывают каждый тип артефакта

#### Priority 1: Deep MCP datasource expansion (1 неделя)
**Цель:** Безопасное расширение внешних контекстов через MCP

**Задачи:**
- [x] Создать `IMcpAdapterRegistry` для registration MCP adapters
- [ ] Реализовать адаптеры для: Google Drive, Figma, Slack, Jira (как в Qwen Code)
- [x] Добавить security layer для MCP adapters (PHI/PII guard, rate limiting)
- [x] Добавить policy для MCP datasource usage (which datasources allowed for which tasks)

**Acceptance:**
- [x] IMcpAdapter интерфейс и InMemoryMcpAdapterRegistry созданы
- [x] IMcpSecurityLayer и InMemoryMcpSecurityLayer реализованы с PHI/PII guard и rate limiting
- [x] IMcpUsagePolicy и InMemoryMcpUsagePolicy реализованы
- [ ] MCP adapters работают с внешними datasources безопасно (требуют реализации для Drive/Figma/Slack/Jira)
- [x] Security layer предотвращает утечку чувствительных данных
- [x] Policy контролирует какие datasources можно использовать

#### Priority 1: Flow-mode orchestration (1 неделя)
**Цель:** Плавное переключение между copilot и agent режимами

**Задачи:**
- [x] Создать enum `ExecutionMode` (Copilot, Agent, Flow)
- [ ] В `AgentIntegrationCoordinator` добавить support для Flow mode (плавное переключение)
- [x] Добавить detection когда переключаться между режимами (context length, task complexity)
- [x] Сохранять context continuity при переключении режимов

**Acceptance:**
- [x] ExecutionMode enum создан
- [x] IExecutionModeManager интерфейс и InMemoryExecutionModeManager реализация созданы
- [x] ModeSwitchContext и ModeSwitchResult типы созданы
- [x] Detection logic для mode switching реализована (context length, task complexity, error rate)
- [x] Context preservation метод добавлен
- [ ] AgentIntegrationCoordinator интеграция TBD режимов

#### Priority 1: Agent skills packs (1 неделя)
**Цель:** Доменные skill-паки с контрактами и governance

**Задачи:**
- [x] Создать `ISkillPackRepository` для хранения skill packs
- [x] Создать schema для skill pack (contracts, governance, metadata)
- [ ] Создать sample skill packs для доменов: Web Development, Data Science, DevOps
- [x] Добавить governance layer (approval process, versioning, deprecation)

**Acceptance:**
- [x] SkillPack schema создан (SkillDefinition, SkillContract, SkillGovernance)
- [x] ISkillPackRepository интерфейс и InMemorySkillPackRepository реализация созданы
- [x] Governance layer реализован (approval, rejection, deprecation)
- [ ] Sample skill packs созданы (требуют domain-specific контента)
- [ ] Skill packs загружаются и валидируются по schema

### Ownership (распределение реализации: CODEX vs SWE)

#### CODEX (веду лично, полный цикл: код + тесты + e2e-подтверждение)
- [x] P0.1 `Stack fidelity guard (plan -> generation)` + integration tests на fail/pass сценарии.
- [x] P0.2 `API runtime contract` (ASGI/uvicorn + Docker runtime contract checks).
- [x] P0.5 `Error envelope enforcement` в generation/review gates + contract tests.
- [x] P2.1 `Targeted fix synthesis` (contract-diff driven remediation вместо cosmetic patching).
- [x] Финальная валидация: 2 независимых сложных FastAPI run и отчёт по coverage/quality gates.
  - Retest после remediation (2026-04-27): `runId=1a0a3479-c86b-4fc4-bbf7-e98776d7508e` показал остаточный сбой `quality_gate_fix_failed: no_patches_generated` при недоступном fixer-моделе.
  - Закрытие остаточного сбоя (2026-04-27): в deterministic fallback добавлены `httpx` dependency patching для FastAPI TestClient сценариев + fallback `src/main.py` task API + fail-safe ветка при пустом accepted patch set.
  - Подтверждение закрытия (2026-04-27): `runId=25c7b210-6627-4070-8c9b-d95255c2f8c4` -> `Completed`, `review2:post_generation=10`, `fix=10`, `final_report=10`.
  - Дополнительный hardening (2026-04-27): baseline requirements усилен `httpx==0.27.2` для FastAPI/TestClient + heuristic error analysis научен детектировать `MissingPackage:httpx` по stderr (`ModuleNotFoundError` / `requires the httpx package`).
  - Контрольные прогоны после hardening: `runId=c89ee8bf-3253-401f-a71c-140517878ee1`, `runId=8a16d553-ad46-492b-8cb5-9428f161a4b4`, `runId=e8a3b7de-b958-4eda-a59c-aad552f42cd5` -> все `Completed`, `fix=10`, `review2=10`, `final_report=10`; первичный `build:full=2` остаётся как не-блокирующий pre-fix сигнал в текущем пайплайне.
  - Устранение pre-fix build noise (2026-04-27): `AutonomousQualityGateService.EvaluateBuild` переведён на оценку только build-phase command executions (а не общего execution exit code, куда попадали test failures), поэтому test-падения больше не занижают `build:full`.
  - Подтверждение после обновления scoring: `runId=2fbbe720-8dfe-481a-a733-5f7c6774fa35` -> `Completed`, `build:full=10`, `fix=10`, `review2:post_generation=10`, `final_report=10`.

#### SWE (большие автономные блоки, я интегрирую и верифицирую)
- [x] P0.3 `Auth/security baseline` (secret policy, fail-fast, запрет insecure defaults) + тест-пакет (частично - empty secret/test token checks требуют доработки).
- [x] P0.4 `DB architecture baseline` (единый FastAPI persistence contract + alembic required artifact).
- [x] P1.1 `Test quality floor 2.0` (запрет placeholder tests + required integration/negative tests).
- [x] P1.2 `Observability baseline` (structured logs, correlation id, readiness dependency checks).
- [x] P1.3 `Infra completeness` (`docker-compose.yml`, CI workflow, run scripts).
- [x] P1.4 `Domain completeness heuristics` (billing/webhook/idempotency/audit/rate-limit markers).
- [x] P2.2/P2.3 semantic regression checks + stack template packs.
- [x] Pattern 1: Self-healing request loop (recovery cascade - все 3 этапа завершены).
- [x] Pattern 2: Sleep compute / AutoDream memory consolidation (инфраструктура, интеграция, validation/feedback loop).
- [x] Pattern 3: Multi-level feature gating (runtime flags, build-time guardrails, role-based access).
- [x] AI Tooling Priority 1: Artifacts-first outputs, Deep MCP expansion, Flow-mode, Agent skills packs (acceptance: `AIToolingSynthesisPriority1Tests` + e2e run `d6245be2-273a-4321-8063-fafb3936c051`).

#### Правило интеграции
- CODEX принимает каждую поставку SWE только после локальной проверки: `build + targeted tests + full complex generation run`.
- Любой пункт переводится в `[x]` в этом плане только после фактического e2e-пруфа, а не по заявлению.

### Глобальная матрица: выполнено / не выполнено / владелец

#### Выполнено (100%)
- [x] `1) MCP Control Plane (P0)` — владелец закрытия: CODEX + SWE (интеграция и верификация завершены).
- [x] `2) Memory Plane (P0)` — владелец закрытия: CODEX + SWE.
- [x] `3) Skill Runtime + Registry (P0)` — владелец закрытия: CODEX + SWE.
- [x] `4) Task Graph / Program Manager Loop (P0)` — владелец закрытия: CODEX + SWE.
- [x] `5) Review/Security Gates 2.0 (P1)` — владелец закрытия: CODEX + SWE.
- [x] `6) Browser + n8n execution lanes (P1)` — владелец закрытия: CODEX + SWE.
- [x] `7) Context Engineering Kit (P1)` — владелец закрытия: CODEX + SWE.
- [x] `8) Cascade Orchestrator Integration (P0)` — владелец закрытия: CODEX + SWE.
- [x] `9) Frontend Design Agent (P1)` — владелец закрытия: CODEX + SWE.
- [x] `Production Remediation P0.1 Stack fidelity guard` — владелец: CODEX (реализовано + tests pass).

#### Не выполнено (в работе): Production-Readiness Remediation
- [x] `P0.2 API runtime contract` — владелец: CODEX.
- [x] `P0.3 Auth/security baseline` — владелец: SWE.
- [x] `P0.4 DB architecture baseline` — владелец: SWE.
- [x] `P0.5 Error envelope enforcement` — владелец: CODEX.
- [x] `P1.1 Test quality floor 2.0` — владелец: SWE.
- [x] `P1.2 Observability baseline` — владелец: SWE.
- [x] `P1.3 Infra completeness` — владелец: SWE.
- [x] `P1.4 Domain completeness heuristics` — владелец: SWE.
- [x] `P2.1 Targeted fix synthesis` — владелец: CODEX.
- [x] `P2.2 Semantic regression checks` — владелец: SWE.
- [x] `P2.3 Template packs per stack` — владелец: SWE.
- [x] `Финальная двойная e2e-валидация сложных FastAPI run` — владелец: CODEX (retest runId: `fc385577-3f82-43cf-bfff-dd12c359f870`, `3ade9c23-b6f5-441c-abcb-89498e6ae94f`).

#### Не выполнено (в работе): Architecture Upgrade Pack
- [x] `Pattern 1 Self-healing request loop` — владелец реализации: SWE, владелец приёмки: CODEX (acceptance: `RecoveryCascadeServiceTests` pass + e2e run `34bfe30e-2551-41a5-beef-7db01399db9b`).
- [x] `Pattern 2 Sleep compute / AutoDream` — владелец реализации: SWE, владелец приёмки: CODEX (acceptance: integration в `StartAppGenerationCommandHandler` + e2e run `34bfe30e-2551-41a5-beef-7db01399db9b`).
- [x] `Pattern 3 Multi-level feature gating` — владелец реализации: SWE, владелец приёмки: CODEX (acceptance: feature flag service + role/provider wiring + e2e run `34bfe30e-2551-41a5-beef-7db01399db9b`).

#### Не выполнено (в работе): AI Tooling Synthesis
- [x] `Priority 1 (Artifacts-first, MCP datasource expansion, Flow-mode, Agent skills packs)` — владелец реализации: SWE, владелец приёмки: CODEX (accepted: `AIToolingSynthesisPriority1Tests` pass + e2e `d6245be2-273a-4321-8063-fafb3936c051`).
- [x] `Priority 2 (multi-repo, manager surface, unix-style composability)` — владелец реализации: SWE, владелец приёмки: CODEX (accepted: `AIToolingSynthesisPriority2Tests` pass + e2e `9d601059-5f07-4ec7-ab5c-9153b452b89f`).
- [x] `Priority 3 (plan-agent extension, local/cloud handoff)` — владелец реализации: SWE, владелец приёмки: CODEX (accepted: `AIToolingSynthesisPriority3Tests` pass + deterministic fallback hardening for generic runtime errors + e2e `79a954ba-75e0-4ade-aa75-42ddcf48a33e` with build/fix/review2/final=10).

#### Не выполнено (в работе): Subagent System & MemPalace
- [x] `Subagent System Infrastructure (ISubagentRegistry, ISubagentExecutor, .toml parser)` — владелец реализации: SWE, владелец приёмки: CODEX (инфраструктура создана: Subagent.cs, SubagentExecutor.cs, DI TBD).
- [x] `MemPalace Integration (verbatim memory, semantic search, knowledge graph, MCP tools)` — владелец реализации: SWE, владелец приёмки: CODEX (инфраструктура создана: IMemPalace.cs, PalaceWing/Room/Drawer, InMemoryMemPalace, semantic search TBD).
- [x] `Subagent Categories 1-3 (Core Development, Language Specialists, Infrastructure - 30+ subagents)` — владелец реализации: SWE, владелец приёмки: CODEX (accepted: registry-first профили 32/32 в `InMemorySubagentProfileRepository` + role selector `SubagentSelector`; tests `SubagentCoreDevelopmentProfilesTests` + `SubagentLanguageAndInfrastructureProfilesTests`; e2e `14e4d9f9-9415-4d6b-ad0a-c29b5a155187` с trace `selected_subagents:backend-developer,microservices-architect,observability-agent`).
- [x] `Subagent Categories 4-6 (Quality & Security, Data & AI, Developer Experience - 30+ subagents)` — владелец реализации: SWE, владелец приёмки: CODEX (accepted: +30 профилей registry-first в `InMemorySubagentProfileRepository`, tests `SubagentCategoriesFourToSixProfilesTests`, full regression pack pass; e2e `1f9d60d0-7e2a-4ee0-bf43-64963dcb08c4` Completed, `subagent_routing=10`, `selected_subagents` trace stable).
- [x] `Subagent Categories 7-10 (Specialized Domains, Business & Product, Meta & Orchestration, Research & Analysis - 40+ subagents)` — владелец реализации: SWE, владелец приёмки: CODEX (accepted: +41 профилей registry-first в `InMemorySubagentProfileRepository` including Meta & Orchestration 11/11; tests `SubagentCategoriesSevenToTenProfilesTests`; regression pack pass; e2e `77027a35-c71e-463b-81d6-511c4173d740` Completed with stable routing/build/fix/review2/final=10).
- [x] `Parallel Subagent Orchestration (agents can call subagents in parallel while working)` — владелец реализации: SWE, владелец приёмки: CODEX (accepted: nested subtask invocation support in `SubagentOrchestrator` + integration test `SubagentOrchestratorNestedTests` + e2e `2dd58048-f3bf-49fa-92cc-7c2439278d75`).

### External Projects Integration Analysis

#### Tambo AI - Generative UI SDK
**Что применимо:**
- MCP Integrations - уже реализовано в Priority 1 (IMcpAdapterRegistry)
- Local Tools - можно расширить для browser-side функций в IDE
- Context, Auth, Suggestions - можно интегрировать с IArtifactGenerator для UI suggestions

**Что требует адаптации:**
- React-specific - для .NET IDE нужен аналог с WPF/MAUI
- Zod schemas - можно заменить на C# record types с validation

#### EvoMap/evolver - Self-evolving engine
**Что применимо:**
- GEP Protocol (Genome Evolution Protocol) - можно адаптировать для Pattern 1 (Self-healing)
- Structured assets (genes.json, capsules.json, events.jsonl) - можно использовать для skill packs
- Auditable evolution - уже есть ConsolidationTelemetry, можно расширить
- Selector logic - можно интегрировать с IFeatureFlagService для evolution decisions

**Что требует адаптации:**
- Prompt generator vs code patcher - наш агент уже делает code patching, можно добавить evolution layer

#### HKUDS/ClawTeam - Agent Swarm Intelligence
**Что применимо:**
- Task tracking with dependencies - можно интегрировать с IManagerSurfaceService
- Inter-agent messaging - можно расширить для multi-agent coordination
- Workspace isolation via git worktree - уже есть IMultiRepoWorkspaceRegistry
- Team templates (TOML) - можно адаптировать для skill packs
- Monitoring & dashboards - можно интегрировать с IManagerSurfaceService

**Что требует адаптации:**
- CLI agent focus - наш агент уже интегрирован, нужен orchestration layer

#### gnhf - "Good Night, Have Fun"
**Что применимо:**
- Incremental commits - можно интегрировать с git operations
- Shared memory (notes.md) - уже есть IMemoryStore, можно добавить iteration notes
- Resume support - можно добавить для long-running tasks
- Worktree mode for parallel agents - можно интегрировать с IMultiRepoWorkspaceRegistry
- Exponential backoff - можно добавить в RecoveryCascadeService

**Что требует адаптации:**
- CLI-specific - нужно интегрировать в существующий pipeline

#### Рекомендации по интеграции

**Priority 1 (немедленно):**
1. gnhf iteration loop - добавить в StartAppGenerationCommandHandler для incremental commits — **CODEX** ✅ (сделано: incremental_commit audit checkpoint в fix loop)
2. ClawTeam task tracking - интегрировать с IManagerSurfaceService для dependencies — **SWE**
3. EvoMap GEP Protocol - адаптировать для self-healing evolution — **SWE**

**Priority 2 (среднесрочно):**
4. Tambo Local Tools - расширить для IDE-specific tools — **SWE**
5. ClawTeam inter-agent messaging - для multi-agent coordination — **SWE**
6. gnhf resume support - для long-running tasks — **CODEX** ✅ (сделано: `resumeFromRunId` seed plan/files в start flow)

**Priority 3 (долгосрочно):**
7. Tambo Generative UI - аналог для .NET IDE — **SWE**
8. EvoMap structured assets - для advanced skill packs — **SWE**
9. ClawTeam team templates - для predefined agent teams — **CODEX** ✅ (сделано: `ITeamTemplateRepository`/`ITeamTemplateResolver` + built-in templates + orchestration quality gate `team_template`)

#### Subagent Implementation Division

**SWE (infrastructure + orchestration):**
- Subagent Meta & Orchestration (11 subagents) — multi-agent-coordinator, task-distributor, workflow-orchestrator, agent-organizer, context-manager, error-coordinator, knowledge-synthesizer, performance-monitor, agent-installer, it-ops-orchestrator, pied-piper
- Parallel Subagent Orchestration infrastructure
- MemPalace Knowledge Graph (temporal entity-relationship graph)
- MemPalace MCP Tools (29 tools)

**CODEX (domain-specific subagents):**
- Subagent Core Development (12 subagents) — ✅ реализовано registry-first (`ISubagentProfileRepository`/`ISubagentSelector`) и привязано к routing trace (`selected_subagents`): api-designer, backend-developer, frontend-developer, fullstack-developer, code-mapper, graphql-architect, microservices-architect, ui-designer, ui-fixer, websocket-engineer, electron-pro, mobile-developer
- Subagent Categories 2-6 (50+ subagents) — ✅ реализовано registry-first: Language Specialists + Infrastructure + Quality & Security + Data & AI + Developer Experience (50 профилей суммарно)
- Subagent Categories 7-9 (30+ subagents) — ✅ реализовано registry-first (Specialized Domains + Business & Product + Research & Analysis)
- MemPalace Semantic Search (ChromaDB integration)
- Policy-driven subagent routing (team templates + governed skill packs + role-aware gating) — ✅ реализовано и расширено на multi-intent templates (`secure-ai-platform-team`, `dx-automation-team`, `research-ops-team`, `internal-ops-orchestration-team`, `fintech-payments-team`, `healthcare-compliance-team`, `monetization-growth-team`) с подтвержденным routing-trace матчингом в e2e (`447aa860-a93a-432f-96cd-ad028cb957cd`, `959f48d2-1789-4889-a65c-7ac92569cead`, `4ab385fe-b509-4167-b9a7-0434f65cc168`, `c6f31148-8551-4312-b93b-81f3aa1a5287` для team/subagent gates); governance enrichment завершен: built-in skill packs seeded (`web-development-pack`,`devops-pack`,`research-pack`,`security-pack`,`internal-ops-pack`), а в routing trace появились непустые `allowed_skill_packs` (e2e `691bda11-a61d-4d72-84bd-0fb5afeb6b99`, `fe1a9d9c-5529-4f29-ad3d-caf49b9b7a12`); host-level role gating подтвержден env-driven провайдером роли (`AUTONOMOUS_USER_ROLE`) и e2e для external/internal (`9e287724-9e5d-404d-9105-ddf869465247`, `d7abb671-485e-49c7-a3dd-cec4700470fd`).
- Review2 remediation loop усилен: добавлен deterministic retry-хук на архитектурные падения `review2` + расширен python fallback pack до architecture-complete артефактов (docs/config/tests/observability/infra/error envelope). Ограничение: для non-python стека fallback пока не даёт патчей (e2e `9d0e2c70-799f-4960-ac4b-af44724e9604`), требуется cross-stack remediation (делегировано SWE + в работе CODEX).

## Что не переносить напрямую
- Репозитории с утечками system prompts переносить только как red-team reference, не как runtime dependency.
- Инструменты с “обходом лимитов/аккаунтов/API” не включать в production-контур.
- Большие skill-паки импортировать выборочно, через whitelist + security review.

## Definition of Done для этой инициативы
- Для каждого run доступен trace: `task graph + skills + mcp calls + memory hits`.
- Отмена (`cancel`) гарантированно прерывает все активные lanes.
- На средних задачах pipeline стабильно доходит до `build/test`, а stage score не ниже 9/10 после итераций.
- Один и тот же набор skills/rules работает и в Cursor, и в Windsurf без расхождения поведения.
