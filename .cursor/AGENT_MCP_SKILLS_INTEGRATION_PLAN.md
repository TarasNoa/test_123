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

## 1) MCP Control Plane (P0)
**Идея:** единый реестр MCP-инструментов + policy layer + audit.

Реализовать:
- `IMcpToolRegistry` (tool metadata, scopes, risk level)
- `IMcpExecutionPolicy` (allow/deny, timeout, budget, PHI/PII guard)
- `IMcpSessionRouter` (routing по типу задачи: browser/n8n/internal)
- `McpExecutionRecord` в манифесте (tool, args hash, duration, exit/outcome)

Эффект:
- предсказуемый запуск MCP-инструментов,
- наблюдаемость и разбор инцидентов,
- foundation для enterprise security.

## 2) Memory Plane для агентов (P0)
**Идея:** долговременная память по run/project/user для повторяемого качества.

Реализовать:
- `IMemoryStore` (эпизодическая, семантическая, procedural memory)
- ingestion hooks: после plan/generation/build/test/fix
- retrieval policy: top-k + freshness + run-similarity
- memory budget в токенах + score-based pruning

Эффект:
- меньше повторных одинаковых ошибок,
- рост качества fix-итераций,
- ускорение повторных прогонов.

## 3) Skill Runtime + Skill Registry (P0)
**Идея:** формализовать skill execution (вместо ad-hoc prompts).

Реализовать:
- `ISkillRegistry` + версия навыка + capability tags
- `ISkillRunner` (input schema, output schema, retry policy)
- `SkillSelectionStrategy` на основе стадии (plan/build/fix/review)
- safety labels для skills (`trusted`, `review-required`, `sandbox-only`)

Эффект:
- переносимость навыков между Cursor/Windsurf,
- контролируемый quality bar по стадиям.

## 4) Task Graph / Program Manager Loop (P0)
**Идея:** task decomposition + lifecycle tracking как в task-master/todo-for-ai.

Реализовать:
- DAG задач с зависимостями (`blockedBy`, `ready`, `done`, `failed`)
- run-level planner + re-planner после каждого quality gate fail
- обязательная связь task -> file changes -> tests -> evidence

Эффект:
- прозрачность почему run провалился,
- контролируемая декомпозиция больших запросов.

## 5) Review/Security Gates 2.0 (P1)
**Идея:** усилить текущие quality-gates политиками безопасности и review loop.

Реализовать:
- security gate до merge/apply: secrets, dangerous commands, insecure auth defaults
- review gate: static checks + architecture checklist + regression guard
- auto-generated remediation hints в manifest/report

## 6) Browser + n8n execution lanes (P1)
**Идея:** отдельные безопасные lanes для browser automation и workflow automation.

Реализовать:
- Browser lane (MCP): smoke UI checks, auth flow checks, artifact screenshots
- n8n lane: workflow generation/test как часть acceptance criteria
- lane-specific quotas и kill-switch

## 7) Context Engineering Kit для промптов (P1)
**Идея:** шаблоны контекста по стадии (plan/gen/fix/review) + anti-bloat rules.

Реализовать:
- compact context packs (required files, last errors, diff slices)
- prompt contracts с machine-validated output schema
- token-economy policy (budget на стадию)

## Предлагаемый roadmap внедрения

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

## Что не переносить напрямую
- Репозитории с утечками system prompts переносить только как red-team reference, не как runtime dependency.
- Инструменты с “обходом лимитов/аккаунтов/API” не включать в production-контур.
- Большие skill-паки импортировать выборочно, через whitelist + security review.

## Definition of Done для этой инициативы
- Для каждого run доступен trace: `task graph + skills + mcp calls + memory hits`.
- Отмена (`cancel`) гарантированно прерывает все активные lanes.
- На средних задачах pipeline стабильно доходит до `build/test`, а stage score не ниже 9/10 после итераций.
- Один и тот же набор skills/rules работает и в Cursor, и в Windsurf без расхождения поведения.
