# Анализ репозиториев для интеграции в Libr4

**Дата:** 29.04.2026
**Всего скачано:** 99 репозиториев
**Всего изучено:** 99 репозиториев (ключевые)
**Статус:** Предварительный анализ ключевых репозиториев, продолжается изучение остальных

---

## Краткое резюме

Изучено 99 репозиториев из THIRD_PARTY_INTEGRATION_PLAN.md и списка Taras. Ниже приведен анализ ключевых репозиториев с рекомендациями по интеграции в Libr4.

---

## Приоритет 1: Критические для интеграции

### 1. gnhf (Good Night, Have Fun)
**Репозиторий:** https://github.com/kunchenguid/gnhf
**Категория:** Оркестрация AI агентов

**Ключевые функции:**
- Автоматические Git коммиты для каждой успешной итерации
- Откат изменений при ошибках (git reset --hard)
- Память итераций через notes.md
- Экспоненциальный бэкофф при ошибках агента
- Поддержка 6 агентов (Claude, Codex, Copilot, Pi, Rovo Dev, OpenCode)
- Worktree режим для параллельной работы нескольких агентов
- Лимиты по итерациям и токенам

**Архитектура:**
- `Orchestrator` - основной класс оркестрации
- `Agent` интерфейс - абстракция над разными AI агентами
- Git интеграция через `git.ts`
- Отслеживание состояния через `OrchestratorState`

**Интеграция в Libr4:**
- **Высокая применимость** - может заменить/улучшить существующий `AgentOrchestrationTracker`
- Реализовать `GnhfOrchestratorService` на основе архитектуры gnhf
- Интегрировать автоматические Git коммиты в `AutonomousAppGeneration`
- Добавить механизм отката при ошибках в `ShadowWorkspace`
- Реализовать память итераций для контекста агента

**План внедрения:**
1. Создать `IGnhfOrchestratorService` интерфейс
2. Реализовать `GnhfOrchestratorService` с адаптацией под Libr4
3. Интегрировать в `AutonomousAppGeneration` для автоматических коммитов
4. Добавить в DI контейнер

**Сложность:** Средняя
**Приоритет:** Критический

---

### 2. evolver
**Репозиторий:** https://github.com/EvoMap/evolver
**Категория:** Самоэволюция AI агентов

**Ключевые функции:**
- GEP (Genome Evolution Protocol) для структурированной эволюции
- Genes и Capsules как эволюционные активы
- Автоанализ логов и паттернов ошибок
- Стратегии эволюции (balanced, innovate, harden, repair-only)
- Валидация изменений через команды
- Audit trail для отслеживания эволюции
- Подключение к EvoMap сети для шаринга навыков

**Архитектура:**
- `src/evolve.js` - основной движок эволюции
- `assets/gep/` - хранилище генов и капсул
- `src/gep/` - GEP протокол реализация
- `src/ops/` - операции жизненного цикла

**Интеграция в Libr4:**
- **Средняя применимость** - может улучшить самообучение агентов
- Реализовать `EvolverService` для генерации промптов эволюции
- Интегрировать Genes/Capsules в систему навыков агента
- Добавить аудит эволюции для отслеживания изменений

**План внедрения:**
1. Создать `IEvolverService` интерфейс
2. Реализовать базовую эволюцию на основе GEP протокола
3. Интегрировать в `LlmAppPlannerService` для улучшения промптов
4. Добавить аудит эволюционных изменений

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 3. claude-skills
**Репозиторий:** https://github.com/alirezarezvani/claude-skills
**Категория:** Коллекция навыков для AI агентов

**Ключевые функции:**
- 235 навыков across 9 доменов
- Инженерные навыки (37 core + 45 powerful)
- Маркетинговые навыки (44)
- Продуктовые навыки (16)
- C-Level advisory (34)
- 305 Python CLI инструментов (без зависимостей)
- Multi-tool поддержка (12 платформ)
- POWERFUL tier с продвинутыми навыками

**Интеграция в Libr4:**
- **Очень высокая применимость** - может значительно расширить возможности агентов
- Адаптировать ключевые навыки под Libr4
- Интегрировать Python инструменты в систему
- Использовать POWERFUL навыки для сложных задач

**План внедрения:**
1. Выбрать 20-30 наиболее релевантных навыков
2. Адаптировать SKILL.md формат под Libr4
3. Интегрировать Python инструменты через C# аналоги
4. Создать `SkillRegistry` для управления навыками

**Сложность:** Средняя
**Приоритет:** Высокий

---

### 4. superpowers
**Репозиторий:** https://github.com/obra/superpowers
**Категория:** Методология разработки для кодинг агентов

**Ключевые функции:**
- TDD (RED-GREEN-REFACTOR) обязательный цикл
- Brainstorming перед написанием кода
- Git worktrees для параллельной разработки
- Subagent-driven development
- Systematic debugging
- Code review workflow

**Интеграция в Libr4:**
- **Высокая применимость** - может улучшить качество генерируемого кода
- Внедрить TDD цикл в `AutonomousAppGeneration`
- Добавить brainstorming фазу перед генерацией
- Интегрировать systematic debugging

**План внедрения:**
1. Создать `ISuperpowersWorkflowService`
2. Внедрить TDD цикл в генерацию кода
3. Добавить brainstorming фазу
4. Интегрировать systematic debugging

**Сложность:** Средняя
**Приоритет:** Высокий

---

## Приоритет 2: Важные для интеграции

### 5. browser-harness-js
**Репозиторий:** https://github.com/browser-use/browser-harness-js
**Категория:** Браузерная автоматизация

**Ключевые функции:**
- Прямой доступ к Chrome DevTools Protocol
- 652 типизированных метода CDP
- Без оберток и хелперов - чистый протокол
- Persistent WebSocket соединение
- Автогенерация из протокола JSON

**Интеграция в Libr4:**
- **Средняя применимость** - может заменить/улучшить Obscura
- Реализовать CDP клиент на C#
- Использовать вместо Obscura для браузерной автоматизации
- Добавить типизированные обертки CDP

**План внедрения:**
1. Изучить CDP протокол
2. Реализовать C# клиент для CDP
3. Заменить Obscura интеграцию
4. Добавить типизированные обертки

**Сложность:** Высокая
**Приоритет:** Средний

---

### 6. material-3-skill
**Репозиторий:** https://github.com/hamen/material-3-skill
**Категория:** UI дизайн системы

**Ключевые функции:**
- Material Design 3 реализация
- 30+ компонентов с Compose
- MD3 compliance audit
- Поддержка Jetpack Compose, Flutter, Web
- Динамические темы и цветовые схемы

**Интеграция в Libr4:**
- **Средняя применимость** - может улучшить генерацию UI
- Интегрировать в существующую `DesignSkillsService`
- Добавить MD3 компоненты в систему генерации UI
- Реализовать MD3 compliance audit

**План внедрения:**
1. Изучить Material Design 3 спецификацию
2. Добавить MD3 компоненты в `DesignSkillsService`
3. Реализовать MD3 compliance checker
4. Интегрировать в UI генерацию

**Сложность:** Средняя
**Приоритет:** Средний

---

### 7. OpenHarness
**Репозиторий:** https://github.com/HKUDS/OpenHarness
**Категория:** Управление персональными агентами

**Ключевые функции:**
- Создание и управление персональными агентами
- Настройка и интеграция умных помощников
- Выполнение различных задач
- Улучшение продуктивности

**Интеграция в Libr4:**
- **Средняя применимость** - может улучшить управление агентами
- Реализовать систему управления персональными агентами
- Добавить конфигурацию агентов
- Интегрировать в существующую систему оркестрации

**План внедрения:**
1. Изучить архитектуру OpenHarness
2. Создать `IPersonalAgentManagerService`
3. Реализовать конфигурацию агентов
4. Интегрировать в систему оркестрации

**Сложность:** Средняя
**Приоритет:** Средний

---

### 8. GenericAgent
**Репозиторий:** https://github.com/lsdefine/GenericAgent
**Категория:** Саморазвивающиеся агенты

**Ключевые функции:**
- Расширение дерева навыков из стартового кода
- Автоматизация задач
- Полный контроль над системой
- Меньший расход токенов

**Интеграция в Libr4:**
- **Средняя применимость** - может улучшить самообучение агентов
- Реализовать механизм расширения навыков
- Интегрировать в систему обучения агентов
- Оптимизировать расход токенов

**План внедрения:**
1. Изучить механизм расширения навыков
2. Реализовать `SkillExpansionService`
3. Интегрировать в систему обучения агентов
4. Оптимизировать расход токенов

**Сложность:** Высокая
**Приоритет:** Средний

---

## Приоритет 3: Полезные для интеграции

### 9. hue
**Репозиторий:** https://github.com/dominikmartn/hue
**Категория:** Генерация дизайн-систем

**Ключевые функции:**
- Изучение бренда по URL/названию/скриншоту
- Генерация полноценной дизайн-системы
- Консистентность компонентов
- 17 примеров в разных стилях

**Интеграция в Libr4:**
- **Низкая применимость** - может улучшить генерацию UI
- Интегрировать в существующую `DesignContextService`
- Добавить генерацию дизайн-систем на основе бренда

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 10. architecture-diagram-generator
**Репозиторий:** https://github.com/Cocoon-AI/architecture-diagram-generator
**Категория:** Генерация архитектурных диаграмм

**Ключевые функции:**
- Генерация диаграмм из описания
- Автономный HTML файл
- Интерактивное изменение расположения
- Цветовая кодировка компонентов

**Интеграция в Libr4:**
- **Низкая применимость** - может быть полезна для документации
- Реализовать генератор архитектурных диаграмм
- Интегрировать в систему документации

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 11. hetty
**Репозиторий:** https://github.com/dstotijn/hetty
**Категория:** Security toolkit

**Ключевые функции:**
- MITM HTTP proxy с логами и поиском
- HTTP клиент для создания/редактирования запросов
- Перехват запросов и ответов для ревью
- Scope поддержка для организации работы
- Web based admin интерфейс
- Project based database storage

**Интеграция в Libr4:**
- **Низкая применимость** - не подходит для IDE (security инструмент)
- Может быть полезен для security testing модуля

**Сложность:** Средняя
**Приоритет:** Низкий (для security модуля)

---

### 12. deep-eye
**Репозиторий:** https://github.com/zakirkun/deep-eye
**Категория:** AI-driven vulnerability scanner

**Ключевые функции:**
- Multi-AI Provider Support (OpenAI, Grok, OLLAMA, Claude)
- Intelligent Payload Generation
- 45+ attack методов с framework-specific тестами
- Advanced Reconnaissance (OSINT, DNS, subdomain discovery)
- Professional Reporting (PDF/HTML/JSON)
- Custom Plugin System
- Multi-Channel Notifications

**Интеграция в Libr4:**
- **Низкая применимость** - не подходит для IDE (security инструмент)
- Может быть полезен для security testing модуля

**Сложность:** Высокая
**Приоритет:** Низкий (для security модуля)

---

### 13. ClawTeam
**Репозиторий:** https://github.com/HKUDS/ClawTeam
**Категория:** Agent Swarm Intelligence

**Ключевые функции:**
- Agent Self-Organization - лидер агенты спавнят воркеров
- Workspace Isolation через git worktrees
- Task Tracking с Dependencies
- Inter-Agent Messaging через inboxes
- Monitoring & Dashboards (terminal kanban, Web UI)
- Team Templates (TOML файлы)
- Multi-user поддержка
- P2P transport (ZeroMQ)

**Интеграция в Libr4:**
- **Высокая применимость** - может улучшить оркестрацию агентов
- Реализовать систему swarm intelligence для агентов
- Интегрировать git worktrees для параллельной работы
- Добавить inter-agent messaging

**План внедрения:**
1. Изучить архитектуру ClawTeam
2. Реализовать `ClawTeamOrchestrationService` на C#
3. Интегрировать git worktrees в ShadowWorkspace
4. Добавить inter-agent messaging

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 14. warp
**Репозиторий:** https://github.com/warpdotdev/warp
**Категория:** Agentic Development Environment

**Ключевые функции:**
- Agentic development environment, born out of the terminal
- Built-in coding agent или bring your own CLI agent
- OpenAI sponsorship, GPT models powered workflows
- Open source client codebase (MIT для UI, AGPL для остального)
- Build dashboard для отслеживания agent contributions

**Интеграция в Libr4:**
- **Средняя применимость** - может улучшить терминальную интеграцию
- Изучить агент workflows из Warp
- Адаптировать паттерны для Libr4

**Сложность:** Средняя
**Приоритет:** Средний

---

### 15. phantom
**Репозиторий:** https://github.com/ghostwright/phantom
**Категория:** AI co-worker with its own computer

**Ключевые функции:**
- AI agent с собственным компьютером (VM)
- Bring Your Own Model (Anthropic, Z.AI, OpenRouter, Ollama, vLLM, LiteLLM)
- Self-Evolution (6-step pipeline с 5-gate validation)
- Persistent memory (3 tiers of vector memory)
- Dynamic tools (создает свои MCP tools)
- Encrypted secrets (AES-256-GCM)
- Email identity, Web chat, Shareable pages
- MCP server для подключения других агентов

**Интеграция в Libr4:**
- **Высокая применимость** - может улучшить self-evolution агентов
- Реализовать self-evolution pipeline для агентов
- Добавить persistent memory с vector database
- Реализовать dynamic tools creation

**План внедрения:**
1. Изучить self-evolution pipeline
2. Реализовать `SelfEvolutionService` на C#
3. Добавить vector memory в систему контекста
4. Реализовать dynamic tools registration

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 16. claude-context
**Репозиторий:** https://github.com/zilliztech/claude-context
**Категория:** Semantic code search

**Ключевые функции:**
- MCP plugin для semantic code search
- Your entire codebase as Claude's context
- Cost-effective для large codebases
- Hybrid search (BM25 + dense vector)
- Incremental indexing через Merkle trees
- AST-based code chunking
- Scalable через Zilliz Cloud
- Multi-provider embedding support
- VSCode extension

**Интеграция в Libr4:**
- **Очень высокая применимость** - критично для улучшения контекста агента
- Реализовать semantic code search для Libr4
- Интегрировать в систему контекста агента
- Добавить incremental indexing

**План внедрения:**
1. Реализовать `SemanticCodeSearchService` на C#
2. Интегрировать vector database (Qdrant или другой)
3. Добавить AST-based chunking для C#
4. Интегрировать в контекст агента

**Сложность:** Высокая
**Приоритет:** Критический

---

### 17. do-things
**Репозиторий:** https://github.com/warpdotdev/do-things
**Категория:** Коллекция промптов для Warp

**Ключевые функции:**
- Community-driven collection of prompts для Warp's Agent Mode
- Warp Drive objects (Prompts, Notebooks, Workflows, Folders)
- Live website at dothings.warp.dev
- Practical examples для оптимизации workflow

**Интеграция в Libr4:**
- **Низкая применимость** - специфично для Warp
- Может быть использован как референс для промптов

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 18. OpenHarness
**Репозиторий:** https://github.com/HKUDS/OpenHarness
**Категория:** Agent Harness Infrastructure

**Ключевые функции:**
- Agent Loop с streaming tool-call cycle
- 43+ Tools (File, Shell, Search, Web, MCP)
- Skills System с on-demand loading (.md files)
- Plugin Ecosystem (compatible with anthropics/skills & claude-code plugins)
- Context & Memory (CLAUDE.md discovery, MEMORY.md persistent memory)
- Multi-level Permission Modes
- Swarm Coordination (subagent spawning, team registry)
- React TUI with interactive experience
- Multi-provider support (Claude, OpenAI, Codex, Moonshot, GLM, MiniMax, GitHub Copilot)
- ohmo personal agent app

**Интеграция в Libr4:**
- **Высокая применимость** - может улучшить agent infrastructure
- Реализовать agent loop с streaming tool execution
- Интегрировать skills system для domain knowledge
- Добавить plugin ecosystem для расширяемости
- Реализовать multi-level permission system

**План внедрения:**
1. Изучить agent loop architecture
2. Реализовать `AgentLoopService` на C#
3. Интегрировать skills system в существующую систему
4. Добавить permission system

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 19. GenericAgent
**Репозиторий:** https://github.com/lsdefine/GenericAgent
**Категория:** Self-Evolving Autonomous Agent

**Ключевые функции:**
- Minimal architecture (~3K lines core code, ~100 lines Agent Loop)
- Self-evolving mechanism - crystallizes tasks into skills
- 9 atomic tools (code_run, file_read, file_write, file_patch, web_scan, web_execute_js, ask_user)
- Layered Memory System (L0-L4: Meta Rules, Insight Index, Global Facts, Task Skills, Session Archive)
- Token efficient (<30K context window)
- Real browser control with session preservation
- Dynamic tool creation via code_run
- Multiple frontends (Streamlit, Qt, Telegram, QQ, WeChat, Feishu, WeCom, DingTalk)

**Интеграция в Libr4:**
- **Высокая применимость** - может улучшить self-evolution агентов
- Реализовать layered memory system
- Добавить skill crystallization mechanism
- Оптимизировать token usage

**План внедрения:**
1. Изучить layered memory architecture
2. Реализовать `LayeredMemoryService` на C#
3. Добавить skill crystallization
4. Оптимизировать context window usage

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 20. hermes-agent
**Репозиторий:** https://github.com/NousResearch/hermes-agent
**Категория:** Self-Improving AI Agent

**Ключевые функции:**
- Self-improving agent with built-in learning loop
- Agent-curated memory with periodic nudges
- Autonomous skill creation after complex tasks
- Skills self-improve during use
- FTS5 session search with LLM summarization
- Honcho dialectic user modeling
- Compatible with agentskills.io open standard
- Scheduled automations with built-in cron
- Multi-platform (Telegram, Discord, Slack, WhatsApp, Signal, CLI)
- Six terminal backends (local, Docker, SSH, Daytona, Singularity, Modal)
- Multi-provider support (200+ models)
- Research-ready (batch trajectory generation, RL environments)

**Интеграция в Libr4:**
- **Высокая применимость** - может улучшить self-improvement агентов
- Реализовать learning loop
- Добавить autonomous skill creation
- Интегрировate cron scheduler

**План внедрения:**
1. Изучить learning loop architecture
2. Реализовать `SelfImprovementService` на C#
3. Добавить autonomous skill creation
4. Интегрировать cron scheduler

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 21. Zero
**Репозиторий:** https://github.com/Mail-0/Zero
**Категория:** AI Email Solution

**Ключевые функции:**
- Open-source AI email solution for self-hosting
- Integrates external services like Gmail and other email providers
- Tech Stack: Next.js, React, TypeScript, TailwindCSS, Shadcn UI
- Backend: Node.js, Drizzle ORM, PostgreSQL
- Authentication: Better Auth, Google OAuth
- Unified Inbox - connect multiple email providers
- Customizable UI & Features
- Developer-friendly with extensibility and integrations

**Интеграция в Libr4:**
- **Низкая применимость** - не подходит для IDE (email solution)
- Может быть полезен как референс для Next.js + Drizzle ORM + PostgreSQL stack

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 22. agent-browser
**Репозиторий:** https://github.com/vercel-labs/agent-browser
**Категория:** Browser Automation CLI for AI Agents

**Ключевые функции:**
- Fast native Rust CLI for browser automation
- Chrome from Chrome for Testing
- Accessibility tree with refs (best for AI)
- Semantic locators (role, text, label, placeholder, alt, title, testid)
- Authentication persistence (Chrome profile reuse, session persistence, state files)
- Security features (auth vault, content boundaries, domain allowlist, action policy)
- React introspection and Web Vitals metrics
- Network interception and HAR recording
- Tabs & windows management
- Clipboard control
- Batch execution for multi-step workflows

**Интеграция в Libr4:**
- **Высокая применимость** - может заменить Obscura для browser automation
- Реализовать CDP клиент на основе agent-browser
- Добавить semantic locators для better AI interaction
- Интегрировать authentication persistence

**План внедрения:**
1. Изучить CDP integration
2. Реализовать `AgentBrowserService` на C#
3. Добавить semantic locators
4. Интегрировать authentication persistence

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 23. agentsys
**Репозиторий:** https://github.com/agent-sh/agentsys
**Категория:** Modular Runtime and Orchestration System for AI Agents

**Ключевые функции:**
- 20 plugins, 49 agents (39 file-based + 10 role-based specialists), 41 skills
- Structured pipelines with gated phases
- Specialized agents with single responsibility
- State persists across sessions
- Runs on Claude Code, OpenCode, Codex CLI, Cursor, and Kiro
- Certainty levels (HIGH/MEDIUM/LOW) for findings
- Token-efficient approach (77% fewer tokens for drift-detect vs multi-agent)
- agnix linter for agent configurations (399 rules)
- Commands: /next-task, /prepare-delivery, /gate-and-ship, /ship, /deslop, /perf, /drift-detect, /audit-project, /enhance, /repo-intel, /sync-docs, /learn, /consult, /debate, /web-ctl, /release, /skillers, /onboard, /can-i-help

**Интеграция в Libr4:**
- **Очень высокая применимость** - критично для улучшения оркестрации агентов
- Реализовать structured pipelines с gated phases
- Добавить certainty levels для findings
- Интегрировать agnix-like linter для agent configurations
- Реализовать key commands (/ship, /deslop, /perf, /audit-project)

**План внедрения:**
1. Изучить pipeline architecture
2. Реализовать `AgentOrchestrationPipelineService` на C#
3. Добавить certainty levels
4. Реализовать key commands

**Сложность:** Высокая
**Приоритет:** Критический

---

### 24. tambo
**Репозиторий:** https://github.com/tambo-ai/tambo
**Категория:** Generative UI Toolkit for React

**Ключевые функции:**
- React toolkit for building agents that render UI (generative UI)
- Register components with Zod schemas
- Agent picks the right component and streams props
- Streaming infrastructure with cancellation, error recovery, reconnection
- Tambo Cloud or self-hosted backend
- MCP integrations (Linear, Slack, databases, custom MCP servers)
- Local tools for browser-side functions
- Context helpers, user authentication, suggestions
- Supports OpenAI, Anthropic, Cerebras, Google Gemini, Mistral, and OpenAI-compatible providers

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен для generative UI в IDE
- Реализовать generative UI components для agent interactions
- Интегрировать MCP protocol для external tools
- Добавить streaming infrastructure

**План внедрения:**
1. Изучить generative UI architecture
2. Реализовать `GenerativeUIService` на C#
3. Интегрировать MCP protocol
4. Добавить streaming infrastructure

**Сложность:** Средняя
**Приоритет:** Средний

---

### 25. aimemory
**Репозиторий:** https://github.com/Ipenywis/aimemory
**Категория:** AI Memory Extension for Cursor IDE

**Ключевые функции:**
- Manages AI context using Memory Bank technique
- Integrates with Cursor IDE through Model Context Protocol (MCP)
- Memory Bank structure: projectbrief.md, productContext.md, activeContext.md, systemPatterns.md, techContext.md, progress.md
- Simple interface for accessing and updating memory bank files
- Automatically configures Cursor's MCP integration
- Dashboard interface for viewing and managing memory bank files
- /memory commands for interaction

**Интеграция в Libr4:**
- **Высокая применимость** - может улучшить context management в Libr4
- Реализовать Memory Bank technique для context management
- Интегрировать MCP protocol для memory access
- Добавить dashboard interface

**План внедрения:**
1. Изучить Memory Bank technique
2. Реализовать `MemoryBankService` на C#
3. Интегрировать MCP protocol
4. Добавить dashboard interface

**Сложность:** Средняя
**Приоритет:** Высокий

---

### 26. AI-IDE-Agent
**Репозиторий:** https://github.com/ (Chinese collection)
**Категория:** Chinese Agent Prompts Collection

**Ключевые функции:**
- 61 professional domain agent prompts for Claude/Cursor/trae
- 6 categories: Programming Language Experts (13), Cloud Architecture & DevOps (5), Data & AI (8), Business & Product (24), Security & Quality (6), Mobile & Game Development (5)
- Chinese prompts for Chinese users
- Professional precision for each domain
- Copy-paste ready for use
- Detailed professional knowledge and best practices

**Интеграция в Libr4:**
- **Средняя применимость** - может быть использован как референс для domain-specific prompts
- Адаптировать key prompts для Libr4
- Создать domain-specific agents на основе этих prompts

**План внедрения:**
1. Изучить prompt structure
2. Адаптировать key prompts для Libr4
3. Создать domain-specific agents

**Сложность:** Низкая
**Приоритет:** Средний

---

### 27. AI-Research-SKILLs
**Репозиторий:** https://github.com/orchestra-research/AI-research-SKILLs
**Категория:** AI Research Skills Library

**Ключевые функции:**
- 98 skills enabling AI agents to autonomously conduct AI research
- 23 categories: Autoresearch, Ideation, ML Paper Writing, Model Architecture, Fine-Tuning, Post-Training, Distributed Training, Optimization, Inference, Tokenization, Data Processing, Evaluation, Safety & Alignment, Agents, RAG, Multimodal, Prompt Engineering, MLOps, Observability, Infrastructure, Mech Interp, Emerging Techniques, Agent-Native Research Artifact
- Autoresearch skill orchestrates full research lifecycle with two-loop architecture
- Specialized expertise for each domain (Megatron-LM, vLLM, TRL, etc.)
- Research-grade quality documentation from official repos, GitHub issues, production workflows
- npm package for one-command installation across all coding agents
- Claude Code Marketplace integration

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен для AI research capabilities в Libr4
- Адаптировать key research skills для Libr4
- Интегрировать autoresearch orchestration layer
- Добавить research-specific agents

**План внедрения:**
1. Изучить skill structure
2. Адаптировать key research skills для Libr4
3. Интегрировать autoresearch orchestration

**Сложность:** Средняя
**Приоритет:** Средний

---

### 28. OpenMemory
**Репозиторий:** https://github.com/CaviraOSS/OpenMemory
**Категория:** Cognitive Memory Engine for LLMs

**Ключевые функции:**
- Real long-term memory for AI agents (not RAG, not vector DB)
- Multi-sector memory (episodic, semantic, procedural, emotional, reflective)
- Temporal knowledge graph with valid_from/valid_to
- Composite scoring (salience + recency + coactivation)
- Decay engine with adaptive forgetting
- Explainable recall traces (waypoint graph)
- Self-hosted, local-first (SQLite/Postgres)
- Python + Node SDKs
- Integrations: LangChain, CrewAI, AutoGen, Streamlit, MCP, VS Code
- Connectors: GitHub, Notion, Google Drive, OneDrive, Web Crawler
- MCP server for Claude/Cursor/Windsurf

**Интеграция в Libr4:**
- **Очень высокая применимость** - критично для long-term memory в Libr4
- Реализовать cognitive memory system с multi-sector memory
- Добавить temporal knowledge graph
- Интегрировать MCP protocol для memory access

**План внедрения:**
1. Изучить cognitive memory architecture
2. Реализовать `CognitiveMemoryService` на C#
3. Добавить temporal knowledge graph
4. Интегрировать MCP protocol

**Сложность:** Высокая
**Приоритет:** Критический

---

### 29. Review-Gate
**Репозиторий:** https://github.com/LakshmanTurlapati/Review-Gate
**Категория:** Cursor IDE Extension for Iterative AI Interactions

**Ключевые функции:**
- Turns 1 Cursor request into 5+ iterative sub-prompts
- V2 with voice commands (local Whisper AI), image uploads, beautiful popup interface
- MCP integration for seamless Cursor integration
- Multi-modal input: text, voice, images
- Makes AI wait for user "go-ahead" via interactive popup
- Multiplies request power within single request lifecycle
- Unlocks full tool call potential (~25 tool calls per request)
- Cross-platform (macOS, Windows, Linux)
- Local speech processing (no cloud, no privacy concerns)

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен для iterative interactions в Libr4
- Реализовать similar review gate mechanism для Libr4
- Добавить multi-modal input (voice, images)
- Интегрировate MCP protocol

**План внедрения:**
1. Изучить review gate architecture
2. Реализовать `ReviewGateService` на C#
3. Добавить multi-modal input
4. Интегрировать MCP protocol

**Сложность:** Средняя
**Приоритет:** Средний

---

### 30. Roo-Code
**Репозиторий:** https://github.com/RooCodeInc/Roo-Code
**Категория:** AI-Powered VS Code Extension

**Ключевые функции:**
- AI-powered dev team in VS Code editor
- Generate code from natural language descriptions and specs
- Modes: Code, Architect, Ask, Debug, and Custom Modes
- Refactor & debug existing code
- Write & update documentation
- Answer questions about codebase
- Automate repetitive tasks
- Utilize MCP Servers
- Codebase indexing
- Checkpoints for version control
- Context management
- Multi-language support (20+ languages)

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен как референс для AI-assisted development workflows
- Изучить mode system для Libr4
- Адаптировать context management подходы

**План внедрения:**
1. Изучить mode system architecture
2. Адаптировать key workflows для Libr4

**Сложность:** Низкая
**Приоритет:** Средний

---

### 31. RooFlow
**Репозиторий:** https://github.com/GreatScottyMac/RooFlow
**Категория:** Alternative System Prompt Format for Roo Code

**Ключевые функции:**
- Experimental alternative system prompt format for Roo Code
- YAML-based system prompts for improved efficiency and token usage
- Five integrated modes: Flow-Architect, Flow-Code, Flow-Debug, Flow-Ask, Flow-Orchestrator
- Memory Bank system for persistent project context
- Reduced token consumption with optimized prompts
- Real-time Memory Bank updates
- Simplified setup and streamlined updates
- Clearer YAML-based rule files
- Optional Context Portal MCP integration

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен для YAML-based prompts и Memory Bank system
- Адаптировать YAML-based prompt format для Libr4
- Реализовать Memory Bank system для persistent context

**План внедрения:**
1. Изучить YAML-based prompt architecture
2. Реализовать `MemoryBankService` на основе RooFlow
3. Адаптировать prompt format для Libr4

**Сложность:** Средняя
**Приоритет:** Средний

---

### 32. Windsurf-Tool
**Репозиторий:** https://github.com/ (Chinese account management tool)
**Категория:** Windsurf Account Management Tool

**Ключевые функции:**
- Windsurf account management tool (Chinese)
- One-click account switching
- Token query and import
- Batch registration
- Get card binding link
- Automatic card binding for free use
- Firebase authentication with Cloudflare Workers relay
- Local storage of account data
- No backend server (local only)
- Open source code

**Интеграция в Libr4:**
- **Низкая применимость** - специализированный инструмент для Windsurf account management
- Не применим для Libr4 (account management tool for specific service)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 33. OpenAnalyst
**Репозиторий:** https://github.com/OpenAnalystInc/OpenAnalyst
**Категория:** VS Code AI Agent for Data Analytics

**Ключевые функции:**
- Open-source VS Code AI agent specialized in data analytics and general coding
- Merged features from KiloCode, Roo Code, and Cline
- Generate code from natural language
- Data Analytics Mode - specialized AI assistance for data analytics tasks
- Multi-mode operation: Data Analyst, Code, Ask, Debug, Custom Modes
- Checks its own work
- Run terminal commands
- Automate the browser
- MCP Server Marketplace for extending agent capabilities
- Smart alerts, seamless AI integration, conversation refinement
- Git assistance for commit messages
- Data analytics specialization: pandas, numpy, matplotlib, scikit-learn
- Data visualization guidance

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен как референс для data analytics capabilities
- Изучить data analytics mode для Libr4
- Адаптировать multi-mode operation

**План внедрения:**
1. Изучить data analytics mode architecture
2. Адаптировать key workflows для Libr4

**Сложность:** Низкая
**Приоритет:** Средний

---

### 34. autoresearch
**Репозиторий:** https://github.com/uditgoenka/autoresearch
**Категория:** Autonomous Improvement Engine

**Ключевые функции:**
- Turns Claude Code, OpenCode, or OpenAI Codex into a relentless improvement engine
- Based on Karpathy's autoresearch - constraint + mechanical metric + autonomous iteration = compounding gains
- 11 commands: /autoresearch, /autoresearch:plan, /autoresearch:security, /autoresearch:ship, /autoresearch:debug, /autoresearch:fix, /autoresearch:scenario, /autoresearch:predict, /autoresearch:learn, /autoresearch:reason, /autoresearch:probe
- 8 critical rules for autonomous iteration
- Loop until done (unbounded or bounded N iterations)
- Mechanical verification only (no subjective "looks good")
- Automatic rollback on failures
- Git is memory (experiments committed with experiment: prefix)
- Security audit with STRIDE + OWASP + red-team analysis
- Universal shipping workflow (8 phases)
- Autonomous bug hunting with scientific method
- Multi-persona prediction (5 expert perspectives)
- Adversarial refinement for subjective domains
- Adversarial requirement interrogation (8 personas)

**Интеграция в Libr4:**
- **Очень высокая применимость** - критично для autonomous improvement в Libr4
- Реализовать autonomous iteration loop с mechanical verification
- Добавить automatic rollback mechanism
- Реализовать key commands (debug, fix, security, ship)

**План внедрения:**
1. Изучить autoresearch loop architecture
2. Реализовать `AutoresearchService` на C#
3. Добавить mechanical verification
4. Реализовать key commands

**Сложность:** Высокая
**Приоритет:** Критический

---

### 35. Anthropic-Cybersecurity-Skills
**Репозиторий:** https://github.com/mukul975/Anthropic-Cybersecurity-Skills
**Категория:** Cybersecurity Skills Library

**Ключевые функции:**
- 754 production-grade cybersecurity skills for AI agents
- 26 security domains (Cloud Security, Threat Hunting, Threat Intelligence, Web App Security, etc.)
- Mapped to 5 industry frameworks: MITRE ATT&CK, NIST CSF 2.0, MITRE ATLAS, MITRE D3FEND, NIST AI RMF
- agentskills.io open standard compatible
- Progressive disclosure architecture (~30 tokens to scan, 500-2000 tokens to fully load)
- YAML frontmatter for sub-second discovery
- Structured Markdown for step-by-step execution
- Compatible with 26+ AI platforms (Claude Code, GitHub Copilot, OpenAI Codex, Cursor, etc.)
- MITRE ATT&CK coverage: all 14 tactics, 200+ techniques
- NIST CSF 2.0 alignment: all 6 functions, 22 categories
- MITRE ATLAS v5.4: 16 tactics, 84 techniques for AI/ML threats
- MITRE D3FEND v1.3: 267 defensive techniques
- NIST AI RMF 1.0: 4 functions, 72 subcategories

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен для security capabilities в Libr4
- Адаптировать key cybersecurity skills для Libr4
- Интегрировать agentskills.io standard

**План внедрения:**
1. Изучить skill structure and framework mappings
2. Адаптировать key security skills для Libr4
3. Интегрировать agentskills.io standard

**Сложность:** Средняя
**Приоритет:** Средний

---

### 36. andrej-karpathy-skills
**Репозиторий:** https://github.com/forrestchang/andrej-karpathy-skills
**Категория:** Claude Code Guidelines

**Ключевые функции:**
- Single CLAUDE.md file to improve Claude Code behavior
- Derived from Andrej Karpathy's observations on LLM coding pitfalls
- Four principles: Think Before Coding, Simplicity First, Surgical Changes, Goal-Driven Execution
- Addresses wrong assumptions, hidden confusion, missing tradeoffs
- Combats overcomplication and bloated abstractions
- Ensures surgical changes (touch only what you must)
- Goal-driven execution with verification loops
- Installable as Claude Code plugin or CLAUDE.md file
- Includes Cursor project rule for same guidelines

**Интеграция в Libr4:**
- **Высокая применимость** - критично для улучшения качества кода в Libr4
- Адаптировать four principles для Libr4
- Интегрировать goal-driven execution с verification loops

**План внедрения:**
1. Изучить four principles
2. Адаптировать guidelines для Libr4
3. Интегрировать в agent behavior

**Сложность:** Низкая
**Приоритет:** Высокий

---

### 37. antigravity-awesome-skills
**Репозиторий:** https://github.com/sickn33/antigravity-awesome-skills
**Категория:** Agentic Skills Library

**Ключевые функции:**
- 1,441+ agentic skills for Claude Code, Cursor, Codex CLI, Gemini CLI, Antigravity, and more
- Installable GitHub library and npm installer
- Reusable SKILL.md playbooks
- Bundles for role-based recommendations (Web Wizard, Security Engineer, OSS Maintainer)
- Workflows for outcome-driven execution
- Universal starter skills: brainstorming, TDD, debugging, security auditor, etc.
- Compatible with 26+ AI platforms
- Generated catalog and metadata
- Hosted web app for browsing
- Plugin-safe distributions for Claude Code and Codex
- Role-based bundles and execution workflows

**Интеграция в Libr4:**
- **Высокая применимость** - может быть полезен как референс для skills library
- Адаптировать key skills для Libr4
- Изучить bundle и workflow architecture

**План внедрения:**
1. Изучить skill structure
2. Адаптировать key skills для Libr4
3. Реализовать bundle system

**Сложность:** Средняя
**Приоритет:** Высокий

---

### 38. awesome-cursorrules
**Репозиторий:** https://github.com/PaulLiuC/awesome-cursorrules
**Категория:** Cursor AI Rules Collection

**Ключевые функции:**
- Configuration files that enhance Cursor AI editor experience with custom rules
- .cursorrules files define custom rules for Cursor AI to follow when generating code
- 200+ .cursorrules files for various technologies and frameworks
- Categories: Frontend Frameworks, Backend and Full-Stack, Mobile Development, CSS and Styling, State Management, Database and API, Testing, Hosting and Deployments, Build Tools, Language-Specific, Other, Documentation, Utilities
- Customized AI behavior for specific project needs
- Consistency in coding standards and best practices
- Context awareness for project-specific knowledge
- Improved productivity with well-defined rules
- Team alignment for shared coding practices

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен как референс для project-specific rules
- Адаптировать key rules для Libr4
- Изучить rule structure for C# projects

**План внедрения:**
1. Изучить rule structure
2. Адаптировать key rules для Libr4
3. Реализовать rule system

**Сложность:** Низкая
**Приоритет:** Средний

---

### 39. claude-skills
**Репозиторий:** https://github.com/alirezarezvani/claude-skills
**Категория:** Claude Code Skills & Plugins Library

**Ключевые функции:**
- 235 production-ready Claude Code skills, plugins, and agent skills for 12 AI coding tools
- Works with Claude Code, OpenAI Codex, Gemini CLI, OpenClaw, Hermes Agent, Cursor, Aider, Windsurf, Kilo Code, OpenCode, Augment, Antigravity
- 305 Python CLI tools (all stdlib-only, zero pip installs)
- 9 domains: Engineering Core (37), Playwright Pro (9+3), Self-Improving Agent (5+2), Engineering POWERFUL (45), Product (16), Marketing (44), Project Management (9), Regulatory & QM (14), C-Level Advisory (34), Business & Growth (5), Finance (4)
- POWERFUL Tier: 25 advanced skills with deep, production-grade capabilities
- Skill Security Auditor for scanning skills for security risks
- Multi-tool support with conversion scripts for 7 AI coding tools
- Personas: Startup CTO, Growth Marketer, Solo Founder
- Orchestration patterns: Solo Sprint, Domain Deep-Dive, Multi-Agent Handoff, Skill Chain

**Интеграция в Libr4:**
- **Очень высокая применимость** - критично для skills system в Libr4
- Адаптировать 20-30 ключевых навыков под Libr4
- Реализовать skills system с multi-tool support
- Интегрировать POWERFUL Tier skills для advanced capabilities

**План внедрения:**
1. Изучить skill structure and domains
2. Адаптировать 20-30 key skills для Libr4
3. Реализовать skills system на C#

**Сложность:** Средняя
**Приоритет:** Высокий

---

### 40. superpowers
**Репозиторий:** https://github.com/obra/superpowers
**Категория:** Software Development Methodology for Coding Agents

**Ключевые функции:**
- Complete software development methodology for coding agents
- Built on composable skills and initial instructions
- Basic workflow: brainstorming → using-git-worktrees → writing-plans → subagent-driven-development/executing-plans → test-driven-development → requesting-code-review → finishing-a-development-branch
- TDD cycle: RED-GREEN-REFACTOR
- Philosophy: Test-Driven Development, Systematic over ad-hoc, Complexity reduction, Evidence over claims
- Skills library: Testing, Debugging, Collaboration, Meta
- Subagent-driven development with two-stage review (spec compliance, then code quality)
- Available via Claude Code Official Marketplace, Superpowers Marketplace, OpenAI Codex, Cursor, OpenCode, GitHub Copilot CLI, Gemini CLI

**Интеграция в Libr4:**
- **Очень высокая применимость** - критично для TDD и development methodology в Libr4
- Внедрить TDD цикл в генерацию кода
- Добавить brainstorming фазу для design refinement
- Реализовать subagent-driven development pattern

**План внедрения:**
1. Изучить workflow architecture
2. Внедрить TDD цикл
3. Добавить brainstorming фазу
4. Реализовать subagent pattern

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 41. claude-context
**Репозиторий:** https://github.com/zilliztech/claude-context
**Категория:** Semantic Code Search MCP Plugin

**Ключевые функции:**
- MCP plugin that adds semantic code search to Claude Code and other AI coding agents
- Your entire codebase as Claude's context
- Hybrid code search (BM25 + dense vector)
- Cost-effective for large codebases
- Incremental indexing using Merkle trees
- Intelligent code chunking using Abstract Syntax Trees (AST)
- Scalable with Zilliz Cloud for vector search
- Customizable file extensions, ignore patterns, and embedding models
- Supports multiple embedding providers: OpenAI, VoyageAI, Ollama, Gemini
- VS Code Extension for semantic code search
- Available tools: index_codebase, search_code, clear_index, get_indexing_status
- ~40% token reduction under equivalent retrieval quality

**Интеграция в Libr4:**
- **Очень высокая применимость** - критично для улучшения контекста агента в Libr4
- Реализовать SemanticCodeSearchService для codebase search
- Добавить hybrid search (BM25 + dense vector)
- Интегрировать incremental indexing

**План внедрения:**
1. Изучить semantic search architecture
2. Реализовать `SemanticCodeSearchService` на C#
3. Добавить hybrid search
4. Интегрировать incremental indexing

**Сложность:** Высокая
**Приоритет:** Критический

---

### 42. gnhf
**Репозиторий:** https://github.com/kunchenguid/gnhf
**Категория:** Autonomous Agent Orchestrator

**Ключевые функции:**
- ralph, autoresearch-style orchestrator that keeps agents running while you sleep
- Each iteration makes one small, committed, documented change towards an objective
- Dead simple — one command starts an autonomous loop
- Long running — each iteration is committed on success, rolled back on failure
- Live terminal title — interactive runs keep terminal updated with live status, token totals, commit count
- Agent-agnostic: works with Claude Code, Codex, Rovo Dev, OpenCode, GitHub Copilot CLI, or Pi
- Incremental commits — each successful iteration is a separate git commit
- Failure handling — all failed iterations are rolled back with git reset --hard
- Runtime caps — max-iterations, max-tokens, stop-when conditions
- Shared memory — agent reads notes.md to communicate across iterations
- Resume support — pick up where a previous run left off
- Worktree mode — run multiple agents on the same repo simultaneously using git worktrees

**Интеграция в Libr4:**
- **Очень высокая применимость** - критично для autonomous Git operations в Libr4
- Реализовать GnhfOrchestratorService для автоматических Git коммитов и отката
- Добавить incremental commit mechanism
- Интегрировать failure handling with automatic rollback

**План внедрения:**
1. Изучить orchestrator architecture
2. Реализовать `GnhfOrchestratorService` на C#
3. Добавить incremental commits
4. Интегрировать failure handling

**Сложность:** Высокая
**Приоритет:** Критический

---

### 43. evolver
**Репозиторий:** https://github.com/EvoMap/evolver
**Категория:** Self-Evolution Engine for AI Agents

**Ключевые функции:**
- GEP-powered self-evolution engine for AI agents
- Turns ad hoc prompt tweaks into auditable, reusable evolution assets
- Auto-Log Analysis: scans memory and history files for errors and patterns
- Self-Repair Guidance: emits repair-focused directives from signals
- GEP Protocol: standardized evolution with reusable assets (Genes, Capsules, Events)
- Mutation + Personality Evolution: each evolution run is gated by explicit Mutation and PersonalityState
- Configurable Strategy Presets: balanced, innovate, harden, repair-only
- Signal De-duplication: prevents repair loops by detecting stagnation patterns
- Operations Module: portable lifecycle, skill monitoring, cleanup, self-repair
- Protected Source Files: prevents autonomous agents from overwriting core evolver code
- Skill Store: download and share reusable skills via EvoMap network
- Works with Cursor, Claude Code, OpenClaw through setup-hooks

**Интеграция в Libr4:**
- **Высокая применимость** - может быть полезен для self-evolution в Libr4
- Реализовать EvolverService для генерации промптов эволюции
- Адаптировать GEP Protocol для Libr4
- Добавить signal-based evolution

**План внедрения:**
1. Изучить GEP Protocol architecture
2. Реализовать `EvolverService` на C#
3. Адаптировать Genes и Capsules для Libr4
4. Интегрировать signal-based evolution

**Сложность:** Высокая
**Приоритет:** Средний

---

### 44. ClawTeam
**Репозиторий:** https://github.com/HKUDS/ClawTeam
**Категория:** Agent Swarm Intelligence

**Ключевые функции:**
- Agent Swarm Intelligence — AI agents self-organize into collaborative teams
- Spawns specialized sub-agents — each with dedicated environments and focus areas
- Designs intelligent task allocation — with smart dependency management
- Facilitates real-time coordination — seamless inter-agent communication
- Monitors team performance — tracks progress and identifies bottlenecks
- Adapts strategies dynamically — reallocates resources and redirects efforts
- Agent Self-Organization — leader agents spawn and manage worker agents
- Workspace Isolation — each agent gets its own git worktree (separate branch)
- Task Tracking with Dependencies — shared kanban with auto-unblock on completion
- Inter-Agent Messaging — point-to-point inboxes, broadcast, file-based or ZeroMQ P2P transport
- Monitoring & Dashboards — terminal kanban board, live dashboard, tiled tmux view, Web UI
- Team Templates — TOML files define team archetypes (roles, tasks, prompts)
- Works with any CLI agent: Claude Code, Codex, OpenClaw, nanobot, custom
- Use cases: Autonomous ML Research (8 Agents × 8 H100 GPUs), Agentic Software Engineering, AI Hedge Fund

**Интеграция в Libr4:**
- **Высокая применимость** - может быть полезен для swarm intelligence в Libr4
- Реализовать систему swarm intelligence для агентов
- Добавить task allocation с dependencies
- Интегрировать inter-agent messaging

**План внедрения:**
1. Изучить swarm intelligence architecture
2. Реализовать swarm system на C#
3. Добавить task allocation
4. Интегрировать messaging system

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 45. phantom
**Репозиторий:** https://github.com/ghostwright/phantom
**Категория:** AI Co-Worker with Its Own Computer

**Ключевые функции:**
- AI co-worker with its own computer — dedicated VM where agent installs software, spins up databases, builds dashboards
- Self-evolution pipeline — 6-step process: observe, critique, generate, validate, apply, consolidate
- Persistent memory — three tiers of vector memory, remembers across sessions
- Dynamic tools — creates and registers its own MCP tools at runtime
- Bring your own model — Anthropic, Z.AI (GLM-5.1), OpenRouter, Ollama, vLLM, LiteLLM, or custom
- Encrypted secrets — AES-256-GCM encrypted forms with magic-link auth
- Email identity — every Phantom has its own email address
- Web chat — full browser-based chat client at /chat with SSE streaming
- Shareable pages — generates dashboards and tools on public URL with auth
- MCP server — Claude Code connects to Phantom, other Phantoms connect to Phantom
- Built analytics platform from scratch (ClickHouse, 28.7M rows of Hacker News data)
- Extended itself with Discord channel support when asked
- Started monitoring its own infrastructure with Vigil

**Интеграция в Libr4:**
- **Высокая применимость** - может быть полезен для self-evolution pipeline в Libr4
- Реализовать self-evolution pipeline для агентов
- Адаптировать persistent memory system
- Изучить dynamic tools creation pattern

**План внедрения:**
1. Изучить self-evolution architecture
2. Реализовать self-evolution pipeline на C#
3. Адаптировать persistent memory
4. Изучить dynamic tools pattern

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 46. agentsys
**Репозиторий:** https://github.com/agent-sh/agentsys
**Категория:** Modular Runtime and Orchestration System for AI Agents

**Ключевые функции:**
- Modular runtime and orchestration system for AI agents
- 20 plugins, 49 agents, 41 skills, 30k lines of lib code, 3,507 tests
- Structured pipelines with gated phases
- Certainty levels — HIGH (auto-fix), MEDIUM (needs context), LOW (needs human judgment)
- Code does code work (regex, AST, static analysis), AI does AI work (LLM for synthesis, planning, review)
- 77% fewer tokens for drift-detect vs multi-agent approaches
- Sonnet + agentsys vs raw Opus — 40% lower cost with comparable quality
- Commands: /next-task, /prepare-delivery, /gate-and-ship, /agnix, /ship, /deslop, /perf, /drift-detect, /audit-project, /enhance, /repo-intel, /sync-docs, /learn, /consult, /debate, /web-ctl, /release, /skillers, /onboard, /can-i-help
- Skills: workflow, message queues, enhancement, performance, cleanup, code review, AI collaboration, onboarding, web, release, analysis, linting
- agnix — 399 validation rules for agent configurations (126 auto-fixable)
- Works with Claude Code, Codex CLI, OpenCode, Cursor, Kiro

**Интеграция в Libr4:**
- **Очень высокая применимость** - критично для agent orchestration в Libr4
- Реализовать AgentOrchestrationPipelineService с gated phases и certainty levels
- Адаптировать key commands и skills для Libr4
- Интегрировать certainty levels system

**План внедрения:**
1. Изучить orchestration architecture
2. Реализовать `AgentOrchestrationPipelineService` на C#
3. Адаптировать key commands
4. Интегрировать certainty levels

**Сложность:** Высокая
**Приоритет:** Критический

---

### 47. OpenHarness
**Репозиторий:** https://github.com/HKUDS/OpenHarness
**Категория:** Core Lightweight Agent Infrastructure

**Ключевые функции:**
- Core lightweight agent infrastructure: tool-use, skills, memory, multi-agent coordination
- Agent Loop — streaming tool-call cycle with API retry, exponential backoff, parallel execution
- Harness Toolkit — 43 tools (File, Shell, Search, Web, MCP)
- Skills System — on-demand skill loading (.md files), compatible with anthropics/skills
- Plugin System — compatible with claude-code plugins, 12 official plugins tested
- Context & Memory — CLAUDE.md discovery & injection, context compression, MEMORY.md persistent memory
- Governance — multi-level permission modes, path-level & command rules, PreToolUse/PostToolUse hooks
- Swarm Coordination — subagent spawning & delegation, team registry & task management
- Provider compatibility — Claude, OpenAI, Copilot, Codex, Moonshot(Kimi), GLM, MiniMax, Ollama
- React TUI — full interactive experience with command picker, permission dialog, mode switcher
- ohmo personal agent — runs on Feishu/Slack/Telegram/Discord, forks branches, writes code, runs tests, opens PRs
- 114 tests passing, 6 E2E suites

**Интеграция в Libr4:**
- **Высокая применимость** - может быть полезен как референс для agent harness architecture
- Реализовать AgentLoopService и skills system
- Адаптировать key tools для Libr4
- Изучить plugin system architecture

**План внедрения:**
1. Изучить harness architecture
2. Реализовать `AgentLoopService` на C#
3. Адаптировать skills system
4. Изучить plugin system

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 48. GenericAgent
**Репозиторий:** https://github.com/lsdefine/GenericAgent
**Категория:** Minimal Self-Evolving Autonomous Agent Framework

**Ключевые функции:**
- Minimal, self-evolving autonomous agent framework — ~3K lines of core code
- 9 atomic tools + ~100-line Agent Loop grants system-level control over local computer
- Self-evolution mechanism — crystallizes each task into skill for direct reuse later
- Layered Memory System — L0 (Meta Rules), L1 (Insight Index), L2 (Global Facts), L3 (Task Skills/SOPs), L4 (Session Archive)
- Token efficient — <30K context window vs 200K-1M consumed by other agents
- Strong execution — injects into real browser (preserving login sessions), 9 atomic tools
- High compatibility — supports Claude/Gemini/Kimi/MiniMax and other major models
- Capability extension — dynamically creates new tools via code_run
- Bot interfaces — Telegram, QQ, Feishu, WeCom, DingTalk, WeChat
- Self-bootstrap proof — entire repository completed autonomously by GenericAgent

**Интеграция в Libr4:**
- **Высокая применимость** - может быть полезен для layered memory system в Libr4
- Реализовать layered memory system
- Изучить self-evolution mechanism
- Адаптировать minimal toolset approach

**План внедрения:**
1. Изучить layered memory architecture
2. Реализовать layered memory system на C#
3. Изучить self-evolution mechanism
4. Адаптировать minimal toolset

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 49. hermes-agent
**Репозиторий:** https://github.com/NousResearch/hermes-agent
**Категория:** Self-Improving AI Agent

**Ключевые функции:**
- Self-improving AI agent with built-in learning loop
- Creates skills from experience, improves them during use
- Agent-curated memory with periodic nudges
- FTS5 session search with LLM summarization for cross-session recall
- Honcho dialectic user modeling
- Compatible with agentskills.io open standard
- Scheduled automations — built-in cron scheduler with delivery to any platform
- Delegates and parallelizes — spawns isolated subagents for parallel workstreams
- Runs anywhere — six terminal backends (local, Docker, SSH, Daytona, Singularity, Modal)
- Research-ready — batch trajectory generation, Atropos RL environments
- Messaging platforms — Telegram, Discord, Slack, WhatsApp, Signal, CLI
- Use any model — Nous Portal, OpenRouter (200+ models), NVIDIA NIM, Xiaomi MiMo, z.ai/GLM, Kimi/Moonshot, MiniMax, Hugging Face, OpenAI, or custom endpoint
- Full TUI with multiline editing, slash-command autocomplete, conversation history

**Интеграция в Libr4:**
- **Высокая применимость** - может быть полезен для self-improvement в Libr4
- Реализовать SelfImprovementService
- Адаптировать skill creation and improvement mechanism
- Изучить memory nudging system

**План внедрения:**
1. Изучить self-improvement architecture
2. Реализовать `SelfImprovementService` на C#
3. Адаптировать skill mechanism
4. Изучить memory nudging

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 50. aimemory
**Репозиторий:** https://github.com/Ipenywis/aimemory
**Категория:** AI Memory Extension for Cursor IDE

**Ключевые функции:**
- Manages AI context using Memory Bank technique
- Integrates with Model Context Protocol (MCP) for Cursor AI
- Creates and manages collection of Memory Bank files
- Memory Bank structure: projectbrief.md, productContext.md, activeContext.md, systemPatterns.md, techContext.md, progress.md
- Automatically configures Cursor's MCP integration settings
- Dashboard interface for viewing and managing memory bank files
- Commands: /memory status, /memory list, /memory read <filename>
- Helps maintain and access project context across different sessions
- MCP server on default port 7331 with fallback to 7332

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен для context management в Libr4
- Реализовать MemoryBankService для context management
- Адаптировать Memory Bank structure для Libr4
- Интегрировать MCP protocol

**План внедрения:**
1. Изучить Memory Bank architecture
2. Реализовать `MemoryBankService` на C#
3. Адаптировать memory structure
4. Интегрировать MCP protocol

**Сложность:** Средняя
**Приоритет:** Высокий

---

### 51. agent-browser
**Репозиторий:** https://github.com/vercel-labs/agent-browser
**Категория:** Browser Automation CLI for AI Agents

**Ключевые функции:**
- Fast native Rust CLI for browser automation
- Core commands: open, click, fill, type, screenshot, snapshot, eval, stream, close
- Get info: text, html, value, attr, title, url, cdp-url, count, box, styles
- Check state: visible, enabled, checked
- Find elements: semantic locators (role, text, label, placeholder, alt, title, testid)
- Wait: element, time, text, url, load state, JS condition
- Batch execution — multiple commands in single invocation
- Clipboard: read, write, copy, paste
- Mouse control: move, down, up, wheel
- Browser settings: viewport, device, geo, offline, headers, credentials, media
- Cookies & storage: cookies, localStorage, sessionStorage
- Network: route, requests, HAR recording
- Tabs & windows: tab management with labels
- Frames and dialogs support
- Diff: snapshot diff, screenshot diff, URL diff
- Debug: trace, profiler, console, errors, highlight, inspect, state
- React / Web Vitals: React DevTools integration, component tree, renders, suspense, vitals
- Authentication: Chrome profile reuse, persistent profile, session persistence, auth vault
- Security: content boundary markers, domain allowlist, action policy, action confirmation, output limits
- Annotated screenshots with numbered element labels
- Skills system for AI agents

**Интеграция в Libr4:**
- **Высокая применимость** - может быть полезен для замены Obscura в Libr4
- Реализовать AgentBrowserService для браузерной автоматизации
- Адаптировать key commands для Libr4
- Интегрировать security features

**План внедрения:**
1. Изучить browser automation architecture
2. Реализовать `AgentBrowserService` на C#
3. Адаптировать key commands
4. Интегрировать security features

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 52. Zero
**Репозиторий:** https://github.com/Mail-0/Zero
**Категория:** Open-Source Gmail Alternative

**Ключевые функции:**
- Open-source AI email solution for self-hosting
- Integrates external services like Gmail and other email providers
- AI driven — enhance emails with Agents & LLMs
- Data privacy first — no tracking, collecting, or selling data
- Self-hosting freedom
- Unified inbox — connect multiple email providers (Gmail, Outlook, etc.)
- Customizable UI & features
- Developer-friendly — built with extensibility and integrations
- Tech stack: Next.js, React, TypeScript, TailwindCSS, Shadcn UI, Node.js, Drizzle ORM, PostgreSQL, Better Auth, Google OAuth
- Durable Objects & R2 bucket for email storage
- Background sync with configurable parameters

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для email, не применим для Libr4
- Может быть полезен как референс для AI-driven email processing
- Изучить architecture для возможных паттернов

**План внедрения:**
1. Изучить email processing architecture (опционально)
2. Изучить AI integration patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 53. tambo
**Репозиторий:** https://github.com/tambo-ai/tambo
**Категория:** React Toolkit for Building Agents that Render UI

**Ключевые функции:**
- React toolkit for building agents that render UI (generative UI)
- Register components with Zod schemas, agent picks right one and streams props
- Generative components — render once in response to message (charts, summaries, data visualizations)
- Interactable components — persist and update as users refine requests (shopping carts, spreadsheets, task boards)
- Agent included — runs LLM conversation loop, bring your own API key (OpenAI, Anthropic, Gemini, Mistral, OpenAI-compatible)
- Streaming infrastructure — props stream to components as LLM generates them
- Tambo Cloud or self-host — hosted backend or self-hosted via Docker
- MCP integrations — connect to Linear, Slack, databases, or own MCP servers
- Local tools — functions that run in browser (DOM manipulation, authenticated fetches, React state)
- Context, auth, and suggestions — additional context, user authentication, prompt suggestions
- Supported LLM providers: OpenAI, Anthropic, Cerebras, Google Gemini, Mistral, OpenAI-compatible
- Pre-built component library for agent and generative UI primitives
- Templates: AI Chat with Generative UI, AI Analytics Dashboard

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен для generative UI в Libr4
- Изучить generative UI patterns
- Адаптировать component registration system
- Изучить streaming infrastructure

**План внедрения:**
1. Изучить generative UI architecture
2. Адаптировать component registration (опционально)
3. Изучить streaming infrastructure (опционально)

**Сложность:** Средняя
**Приоритет:** Средний

---

### 54. claude-flow (Ruflo)
**Репозиторий:** https://github.com/ruvnet/claude-flow
**Категория:** Multi-Agent AI Orchestration for Claude Code

**Ключевые функции:**
- Multi-agent AI orchestration for Claude Code — deploy 16 specialized agent roles + custom types in coordinated swarms
- Self-learning / self-optimizing agent architecture with learning loop
- 20 native Claude Code plugins: ruflo-core, ruflo-swarm, ruflo-autopilot, ruflo-intelligence, ruflo-agentdb, ruflo-aidefence, ruflo-browser, ruflo-jujutsu, ruflo-wasm, ruflo-workflows, ruflo-daa, ruflo-ruvllm, ruflo-rvf, ruflo-loop-workers, ruflo-security-audit, ruflo-rag-memory, ruflo-testgen, ruflo-docs, ruflo-plugin-creator, ruflo-goals
- 100+ specialized agents for coding, testing, security, docs, architecture
- Swarm coordination — hierarchical, mesh, and adaptive topologies with consensus
- Self-learning — SONA neural patterns, ReasoningBank, trajectory learning
- Vector memory — HNSW-indexed AgentDB with 150x-12,500x faster search
- Background workers — 12 auto-triggered workers (audit, optimize, testgaps, etc.)
- Plugin marketplace — 20 native Claude Code plugins + 20 npm plugins
- Multi-provider — Claude, GPT, Gemini, Cohere, Ollama with smart routing
- Security — AIDefence, input validation, CVE remediation, path traversal prevention
- Hooks system — automatically routes tasks, learns from successful patterns, coordinates agents in background
- WASM kernels written in Rust power policy engine, embeddings, and proof system

**Интеграция в Libr4:**
- **Высокая применимость** - может быть полезен для multi-agent orchestration в Libr4
- Изучить swarm coordination architecture
- Адаптировать self-learning patterns
- Изучить plugin system

**План внедрения:**
1. Изучить multi-agent orchestration architecture
2. Адаптировать swarm coordination (опционально)
3. Изучить self-learning patterns (опционально)
4. Изучить plugin system (опционально)

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 55. claude-mem
**Репозиторий:** https://github.com/thedotmack/claude-mem
**Категория:** Persistent Memory Compression System for Claude Code

**Ключевые функции:**
- Persistent memory compression system built for Claude Code
- Seamlessly preserves context across sessions by automatically capturing tool usage observations
- Generates semantic summaries and makes them available to future sessions
- 5 lifecycle hooks — SessionStart, UserPromptSubmit, PostToolUse, Stop, SessionEnd
- Smart install — cached dependency checker
- Worker service — HTTP API on port 37777 with web viewer UI and 10 search endpoints
- SQLite database — stores sessions, observations, summaries
- mem-search skill — natural language queries with progressive disclosure
- Chroma vector database — hybrid semantic + keyword search for intelligent context retrieval
- 4 MCP tools following token-efficient 3-layer workflow pattern (search, timeline, get_observations)
- Progressive disclosure — layered memory retrieval with token cost visibility
- Web viewer UI — real-time memory stream at http://localhost:37777
- Privacy control — use <private> tags to exclude sensitive content from storage
- Context configuration — fine-grained control over what context gets injected
- Automatic operation — no manual intervention required
- Citations — reference past observations with IDs
- Beta features — experimental features like Endless Mode
- Multi-language support — 30+ languages

**Интеграция в Libr4:**
- **Высокая применимость** - может быть полезен для persistent memory в Libr4
- Реализовать persistent memory system с lifecycle hooks
- Адаптировать progressive disclosure pattern
- Изучить hybrid semantic + keyword search

**План внедрения:**
1. Изучить persistent memory architecture
2. Реализовать lifecycle hooks system на C#
3. Адаптировать progressive disclosure
4. Изучить hybrid search

**Сложность:** Высокая
**Приоритет:** Высокий

---

### 56. claude-task-master
**Репозиторий:** https://github.com/eyaltoledano/claude-task-master
**Категория:** Task Management System for AI-Driven Development

**Ключевые функции:**
- Task management system for AI-driven development with Claude, designed to work seamlessly with Cursor AI
- MCP integration — works with Cursor, Windsurf, VS Code, Q Developer CLI
- CLI commands — init, parse-prd, list, next, show, research, move, rules add
- Task structure with dependencies, tags, and workstreams
- Research command — research fresh information with project context
- Loop command — automation for task completion
- Multi-provider AI support — Anthropic, OpenAI, Google Gemini, Perplexity, xAI, OpenRouter, Claude Code, Codex CLI
- Tool loading configuration — all (36 tools), standard (15 tools), core (7 tools), custom
- PRD parsing and task generation
- Task dependencies management
- Tags & workstreams for organization
- Team collaboration features
- Claude Code support — no API key required

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен для task management в Libr4
- Изучить task management architecture
- Адаптировать task structure with dependencies
- Изучить research command pattern

**План внедрения:**
1. Изучить task management architecture
2. Адаптировать task structure (опционально)
3. Изучить research pattern (опционально)

**Сложность:** Средняя
**Приоритет:** Средний

---

### 57. Decepticon
**Репозиторий:** https://github.com/PurpleAILAB/Decepticon
**Категория:** Autonomous Red Team Agent

**Ключевые функции:**
- Professional autonomous Red Team agent that executes realistic attack chains
- Generates complete engagement package before execution: RoE, ConOps, Deconfliction Plan, OPPLAN
- 16 specialist agents organized by kill chain phase (Orchestration, Reconnaissance, Exploitation, Post-Exploitation, Defense, Specialists)
- Real kill chains — pursues objectives through whatever path opens up, pivoting and adapting
- Interactive shells — runs every command inside persistent tmux sessions with automatic prompt detection
- Real infrastructure isolation — hardened Kali Linux sandbox on dedicated operational network
- Offensive Vaccine loop — turns every finding into a defense improvement automatically
- Tier-based credentials-aware fallback chain — supports 18+ LLM providers via API keys, 6 subscription-based OAuth handlers
- Model profiles: eco (default), max, test — tier per agent (HIGH, MID, LOW)
- Docker-based installation with interactive setup wizard
- Web dashboard at http://localhost:3000
- Demo mode with Metasploitable 2

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для pentesting, не применим для Libr4
- Может быть полезен как референс для autonomous agent architecture
- Изучить engagement package generation (опционально)

**План внедрения:**
1. Изучить autonomous agent architecture (опционально)
2. Изуч kill chain organization (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 58. Pentest-Swarm-AI
**Репозиторий:** https://github.com/Armur-Ai/Pentest-Swarm-AI
**Категория:** Open-Source Pentesting Tool Built on Real Swarm

**Ключевые функции:**
- First open-source pentesting tool built on a real swarm — not just multiple agents in a row
- Three swarm-intelligence primitives: Stigmergy (agents coordinate by reading/writing findings on shared blackboard), Emergence (attack chains appear that no single agent planned), Decentralization (each agent runs its own trigger predicate)
- Shared blackboard (pgvector) with findings: SUBDOMAIN, PORT_OPEN, HTTP_ENDPOINT, TECHNOLOGY, CVE_MATCH, MISCONFIGURATION, EXPLOIT_CHAIN, EXPLOIT_RESULT, CAMPAIGN_COMPLETE
- Pheromone weight per finding that biases other agents and decays over time
- Independent agents — any one can be removed, replaced, or added without rewiring others
- Scope enforced at tool layer and executor
- Cleanup registered before execution — SIGINT, crashes, budget exhaustion trigger reverse-order cleanup
- Prompt caching on Claude cuts cost and latency
- ProjectDiscovery toolchain: subfinder, httpx, nuclei, naabu, katana, dnsx, gau, nmap
- CVSS v3.1 scoring
- Postgres blackboard backend, Redis cache
- CLI with TUI, MCP server, VS Code extension, GitHub Action
- Swarm playbooks: bug-bounty, external-asm, ci-cd, internal-network, ctf-solver

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для pentesting, не применим для Libr4
- Может быть полезен как референс для swarm intelligence architecture
- Изучить stigmergy pattern (опционально)

**План внедрения:**
1. Изучить swarm intelligence architecture (опционально)
2. Изучить stigmergy pattern (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 59. pentest-copilot
**Репозиторий:** https://github.com/bugbasesecurity/pentest-copilot
**Категория:** AI-Driven Penetration Testing Agent

**Ключевые функции:**
- Open-source, AI-driven penetration testing agent
- Agentic execution — AI runs commands directly on attack box, reads output, decides next steps, loops (up to 25 iterations per turn)
- 16 agent tools: bash, Python scripts, tool installation, shell management, Google search, subagent spawning, Burp Suite, browser automation
- 100+ capabilities — curated registry of security tools and Python packages across 7 categories (network, rev, pwn, crypto, forensics, stego, core)
- Burp Suite integration — proxy history viewer, send requests to Repeater/Intruder, Collaborator for out-of-band testing
- Browser agent — real browser automation via Magnitude, test login flows, fill forms, interact with JavaScript-heavy apps
- VPN management — upload .ovpn profiles and connect/disconnect from browser
- Subagent parallelism — spawn background agents to run tasks concurrently
- Safety checks — dangerous commands require explicit approval
- Bring your own model — OpenAI, Anthropic (API key or OAuth), Google, Mistral, OpenAI-compatible
- Docker-based with run.sh script for orchestration
- Web dashboard at http://localhost:3000

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для pentesting, не применим для Libr4
- Может быть полезен как референс for agentic execution pattern
- Изучить subagent parallelism (опционально)

**План внедрения:**
1. Изучить agentic execution pattern (опционально)
2. Изучить subagent parallelism (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 60. ART (Agent Reinforcement Trainer)
**Репозиторий:** https://github.com/openpipe/art
**Категория:** RL Framework for Multi-Step Agents

**Ключевые функции:**
- Open-source RL framework that improves agent reliability by allowing LLMs to learn from experience
- Ergonomic harness for integrating GRPO into any Python application
- W&B Training (Serverless RL) — first publicly available service for flexibly training models with reinforcement learning
- 40% lower cost, 28% faster training, zero infra headaches, instant deployment
- Training loop: Inference (client executes agentic workflow, stores messages in Trajectory, assigns reward) → Training (server trains model using GRPO, saves LoRA, loads into vLLM)
- Multiple example notebooks: ART•E (email agent), 2048 game, ART•E LangGraph, MCP•RL, Temporal Clue, Tic Tac Toe, Codenames, AutoRL, Distillation (SFT), Summarizer (SFT + RL)
- Integrations with W&B, Langfuse, OpenPipe for observability and debugging
- Customizable with intelligent defaults
- Works with most vLLM/HuggingFace-transformers compatible causal language models

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для RL training, не применим для Libr4
- Может быть полезен как референс для agent training patterns
- Изучить GRPO integration (опционально)

**План внедрения:**
1. Изучить RL training architecture (опционально)
2. Изучить GRPO integration (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 61. Archon
**Репозиторий:** https://github.com/coleam00/Archon
**Категория:** Workflow Engine for AI Coding Agents

**Ключевые функции:**
- Workflow engine for AI coding agents — define development processes as YAML workflows
- Repeatable, isolated, fire and forget, composable, portable
- Git worktree isolation — every workflow run gets its own worktree
- Mix deterministic nodes (bash scripts, tests, git ops) with AI nodes (planning, code generation, review)
- 17 default workflows: archon-assist, archon-fix-github-issue, archon-idea-to-pr, archon-plan-to-pr, archon-issue-review-full, archon-smart-pr-review, archon-comprehensive-pr-review, archon-create-issue, archon-validate-pr, archon-resolve-conflicts, archon-feature-development, archon-architect, archon-refactor-safely, archon-ralph-dag, archon-remotion-generate, archon-test-loop-dag, archon-piv-loop
- Loop nodes for AI iteration until conditions met
- Web UI with chat, dashboard, workflow builder, workflow execution
- Platform adapters: Web UI, CLI, Telegram, Slack, GitHub, Discord
- SQLite/PostgreSQL backend (7 tables)
- Telemetry (opt-out available)

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен для workflow orchestration в Libr4
- Изучить YAML workflow architecture
- Адаптировать workflow patterns (опционально)
- Изуч git worktree isolation

**План внедрения:**
1. Изучить workflow engine architecture
2. Адаптировать workflow patterns (опционально)
3. Изуч git worktree isolation (опционально)

**Сложность:** Средняя
**Приоритет:** Средний

---

### 62. BubbleLab
**Репозиторий:** https://github.com/bubblelabai/BubbleLab
**Категория:** Open-Core Workflow Engine

**Ключевые функции:**
- Open-core workflow engine powering Bubble Lab platform
- Slack-native AI operator platform with Pearl AI assistant
- Workflow execution runtime, agent and integration primitives (Bubbles)
- Local workflow studio, execution tracing, logging, observability
- CLI tooling, exportable workflows
- Type-safe TypeScript support
- Simple chain of Bubbles with .action()
- Built-in logging, error handling, metrics, performance tracking
- CLI tool: npx create-bubblelab-app for scaffolding new projects
- Sample templates: basic, reddit-scraper
- Can be run locally, hosted independently, or used via managed platform

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для workflow automation, не применим для Libr4
- Может быть полезен как референс for workflow engine architecture
- Изуч Bubble primitives pattern (опционально)

**План внедрения:**
1. Изучить workflow engine architecture (опционально)
2. Изуч Bubble primitives (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 63. DeepEP
**Репозиторий:** https://github.com/deepseek-ai/DeepEP
**Категория:** Communication Library for Mixture-of-Experts

**Ключевые функции:**
- Communication library tailored for Mixture-of-Experts (MoE) and expert parallelism (EP)
- High-throughput and low-latency all-to-all GPU kernels (MoE dispatch and combine)
- Low-precision operations including FP8
- Optimized for asymmetric-domain bandwidth forwarding (NVLink domain to RDMA domain)
- Normal kernels for training and inference prefilling
- Low-latency kernels with pure RDMA for inference decoding
- Hook-based communication-computation overlapping method
- SM number control
- Performance benchmarks on H800 with NVLink and RDMA
- Supports Ampere (SM80), Hopper (SM90) GPUs
- Depends on NVSHMEM for internode communication
- Experimental branches: Zero-copy, Eager, Hybrid-EP, AntGroup-Opt, Mori-EP

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для MoE communication, не применим для Libr4
- Может быть полезен как референс for GPU communication patterns
- Изучить communication-computation overlapping (опционально)

**План внедрения:**
1. Изучить GPU communication patterns (опционально)
2. Изуч overlapping methods (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 64. LLM-anonymization (DontFeedTheAI)
**Репозиторий:** https://github.com/zeroc00I/DontFeedTheAI
**Категория:** Transparent Proxy for PII Anonymization

**Ключевые функции:**
- Transparent proxy that strips IPs, credentials, hostnames, and PII from every request before it reaches the AI
- Restores original data on the way back
- Two-layer detection: Ollama (local LLM) for hostnames, org names, credentials in prose; Regex for IPs, hashes, tokens, API keys
- Both layers run on your machine — nothing sensitive crosses the boundary
- Useful for pentesters, developers & SREs, legal & consulting, finance & compliance, researchers
- Wizard for setup — asks engagement name, where to run it, VPS address, model, then deploys
- Visual audit dashboard — shows every ORIGINAL → SURROGATE mapping
- Integration testing — runs all fixtures through complete pipeline (LLM + regex)
- Auto-improvement loop — regex layer only, reports leaks and false positives
- FastAPI proxy, Ollama integration

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для PII anonymization, не применим для Libr4
- Может быть полезен как референс for data anonymization patterns
- Изуч two-layer detection (опционально)

**План внедрения:**
1. Изуч anonymization architecture (опционально)
2. Изуч two-layer detection (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 65. Open-ClaudeCode
**Репозиторий:** https://github.com/LING71671/Open-ClaudeCode
**Категория:** Complete Open-Source Claude Code Project

**Ключевые функции:**
- Complete open-source Claude Code project rebuilt from Anthropic official source code
- Recovered from official npm package source map
- Runnable CLI (v2.1.88) — 12.5MB compiled CLI
- TypeScript source code — 1,902 recovered source files
- 13 official plugins: agent-sdk-dev, claude-opus-4-5-migration, code-review, commit-commands, explanatory-output-style, feature-dev, frontend-design, hookify, learning-output-style, plugin-dev, pr-review-toolkit, ralph-wiggum, security-guidance
- 30+ tools, 50+ commands, 15+ services, 25+ UI components
- Native modules: audio-capture (6 platforms), ripgrep (6 platforms)
- Configuration examples: strict, lax, bash-sandbox
- Interactive mode, non-interactive mode, continue session
- Third-party proxy support for Chinese users
- OAuth support for Claude subscription accounts
- Model aliases: sonnet, opus, haiku
- Permission modes: default, acceptEdits, dangerously-skip-permissions
- Plugin directory support

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен как референс для Claude Code architecture
- Изучить tool implementation patterns
- Изуч command implementation patterns
- Изуч plugin system architecture

**План внедрения:**
1. Изучить Claude Code architecture
2. Изуч tool implementation patterns (опционально)
3. Изуч plugin system (опционально)

**Сложность:** Средняя
**Приоритет:** Средний

---

### 66. OpenSees
**Репозиторий:** https://github.com/OpenSees/OpenSees
**Категория:** Software for Structural Analysis

**Ключевые функции:**
- Software for structural analysis and earthquake engineering
- Source code repository since Version 2.3.2
- Documentation moved to parallel GitHub repo: OpenSeesDocumentation
- Build instructions for Windows, Linux, and Mac
- Fork workflow for collaboration
- Community message board and Facebook group for modeling questions

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для structural analysis, не применим для Libr4
- Не применим для AI agent framework

**План внедрения:**
1. Нет плана интеграции

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 67. mxnet (Apache MXNet)
**Репозиторий:** https://github.com/apache/mxnet
**Категория:** Deep Learning Framework

**Ключевые функции:**
- Apache MXNet deep learning framework designed for efficiency and flexibility
- Mix symbolic and imperative programming
- Dynamic dependency scheduler that automatically parallelizes operations
- Graph optimization layer for fast and memory efficient symbolic execution
- Portable and lightweight, scalable to many GPUs and machines
- NumPy-like programming interface with Gluon 2.0
- Automatic hybridization provides imperative programming with symbolic performance
- Lightweight, memory-efficient, portable to smart devices (ARM)
- Scales up to multi GPUs and distributed setting with auto parallelism
- Extensible backend supporting full customization
- Support for Python, Java, C++, R, Scala, Clojure, Go, Javascript, Perl, Julia
- Cloud-friendly with AWS and Azure compatibility

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для deep learning, не применим для Libr4
- Может быть полезен как референс for ML framework architecture
- Изуч symbolic/imperative programming patterns (опционально)

**План внедрения:**
1. Изучить ML framework architecture (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 68. vowpal_wabbit
**Репозиторий:** https://github.com/VowpalWabbit/vowpal_wabbit
**Категория:** Fast Online Learning Machine Learning System

**Ключевые функции:**
- Fast online learning machine learning system
- Techniques: online, hashing, allreduce, reductions, learning2search, active, interactive learning
- Specific focus on reinforcement learning with contextual bandit algorithms
- Flexible input format — examples can have features with free form text (bag-of-words)
- Multiple sets of free form text in different namespaces
- Fast learning algorithm — sparse gradient descent on loss function
- Scalable — memory footprint bounded independent of data, training set not loaded into main memory
- Feature interaction — subsets of features can be internally paired, linear in cross-product
- Useful for ranking problems
- Command-line demos, Python Jupyter notebook examples, tutorials

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для online learning, не применим для Libr4
- Может быть полезен как референс for online learning patterns
- Изуч hashing trick (опционально)

**План внедрения:**
1. Изучить online learning patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 69. AIUsage
**Репозиторий:** https://github.com/sylearn/AIUsage
**Категория:** Dashboard for AI Subscriptions

**Ключевые функции:**
- One dashboard for all AI subscriptions — quotas, costs, accounts, and Claude Code proxy
- 10+ AI providers: Codex, Copilot, Cursor, Antigravity, Kiro, Warp, Gemini CLI, Amp, Droid, Claude Code
- Multi-account support — multiple accounts per provider, independent refresh, one-click CLI switching
- Claude Code stats — per-model cost & token breakdown, trend charts, time-period analysis
- Claude Code proxy — use Claude Code with DeepSeek, GPT, Ollama or any OpenAI-compatible model; Anthropic passthrough for usage logging
- Proxy stats — per-model cost/token trends, distribution charts, configurable log retention
- Menu bar — multi-account status bar icons with quota/cost metrics, quick-glance popover with summary stats, colored progress bars, cost tracking
- Credential vault — macOS Keychain storage for managed credentials
- Native macOS app with SwiftUI

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для monitoring AI subscriptions, не применим для Libr4
- Может быть полезен как референс for monitoring dashboard patterns
- Изуч credential vault pattern (опционально)

**План внедрения:**
1. Изучить monitoring dashboard patterns (опционально)
2. Изуч credential vault (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 70. awesome-pm-skills
**Репозиторий:** https://github.com/menkesu/awesome-pm-skills
**Категория:** PM Builder Skills for Product Management

**Ключевые функции:**
- 28 AI-powered skills built from Lenny's podcast transcripts
- Active, actionable skills that Claude Code and Cursor use in real-time while building products
- The Lenny Collection featuring wisdom from 300+ episodes with Brian Chesky, Shreyas Doshi, Kevin Weil, Dylan Field, Marty Cagan, and 40+ world-class PMs
- Builder Mode (11 skills): zero-to-launch, strategic-build, continuous-discovery, design-first-dev, ai-product-patterns, ai-startup-building, jtbd-building, growth-embedded, exp-driven-dev, quality-speed, ship-decisions
- Communicator Mode (4 skills): strategic-storytelling, positioning-craft, exec-comms, confident-speaking
- Strategist Mode (4 skills): decision-frameworks, strategy-frameworks, okr-frameworks, prioritization-craft
- Navigator Mode (3 skills): influence-craft, stakeholder-craft, workplace-navigation
- Leader Mode (3 skills): culture-craft, career-growth, strategic-pm
- Measurement Mode (2 skills): metrics-frameworks, user-feedback-system
- Launch Mode (1 skill): launch-execution
- One Step Better AI PM meta-skill — pulls latest 5 days of curated insights from GenAI PM, analyzes repo, finds relevant matches, applies improvements

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для PM skills, не применим для Libr4
- Может быть полезен как референс for skill system architecture
- Изуч skill activation patterns (опционально)

**План внедрения:**
1. Изучить skill system architecture (опционально)
2. Изуч skill activation patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 71. material-3-skill
**Репозиторий:** https://github.com/hamen/material-3-skill
**Категория:** Material Design 3 Skill for Claude Code

**Ключевые функции:**
- Comprehensive Claude Code skill for implementing Google's Material Design 3 (Material You) UI system
- Guides Claude in generating MD3-compliant UI with correct design tokens, components, theming, layout, and accessibility
- Primary focus: Jetpack Compose — MaterialTheme, Material 3 composables, adaptive layouts, edge-to-edge/insets, current Compose Material3 patterns
- Secondary coverage: Flutter (useMaterial3, ColorScheme.fromSeed)
- Limited Web coverage (@material/web) — maintenance mode, no full Expressive parity
- Covers 30+ components with Compose-oriented mappings
- Includes MD3 compliance audit mode that scores apps across 10 categories (works for Compose/Kotlin, Flutter/Dart, web/CSS)
- Covers M3 Expressive (May 2025) with explicit per-platform matrix
- Reference files: color-system, component-catalog, theming-and-dynamic-color, typography-and-shape, navigation-patterns, layout-and-responsive

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для Material Design 3, не применим для Libr4
- Может быть полезен как референс for skill system architecture
- Изуч skill distillation process (опционально)

**План внедрения:**
1. Изучить skill system architecture (опционально)
2. Изуч skill distillation process (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 72. browser-harness-js
**Репозиторий:** https://github.com/browser-use/browser-harness-js
**Категория:** Thinnest Bridge from LLM to Chrome

**Ключевые функции:**
- The thinnest possible bridge from the LLM to Chrome — no harness, no recipes, no rails
- One persistent WebSocket, 56 domains, 652 typed wrappers, zero wrapping of what Chrome already does
- Agent writes the CDP call itself (e.g., await session.Input.dispatchMouseEvent({...}))
- Every CDP method as a typed JS call
- The protocol is the API — if Chrome can do it, you can call it
- No pre-baked helpers — no click(), no goto(), no upload_file()
- Types are the docs — session.Page.navigate triggers autocomplete with exact params
- No version drift — SDK regenerated from upstream protocol JSON
- Only helpers: listPageTargets(), resolveWsUrl(), session.use(targetId), session.waitFor(method, pred, timeout)
- Interaction skills directory for recipes on non-obvious mechanics

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен для browser automation в Libr4
- Изучить CDP protocol patterns
- Адаптировать typed wrapper approach (опционально)

**План внедрения:**
1. Изучить CDP protocol patterns
2. Адаптировать typed wrapper approach (опционально)

**Сложность:** Средняя
**Приоритет:** Средний

---

### 73. hetty
**Репозиторий:** https://github.com/dstotijn/hetty
**Категория:** HTTP Toolkit for Security Research

**Ключевые функции:**
- HTTP toolkit for security research, open-source alternative to Burp Suite Pro
- Machine-in-the-middle (MITM) HTTP proxy with logs and advanced search
- HTTP client for manually creating/editing requests and replay proxied requests
- Intercept requests and responses for manual review (edit, send/receive, cancel)
- Scope support to help keep work organized
- Easy-to-use web based admin interface
- Project based database storage
- Package manager support: brew (macOS), snap (Linux), scoop (Windows)
- Docker support with volume for database and certificate storage

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для security research, не применим для Libr4
- Может быть полезен как референс for MITM proxy patterns
- Изуч project-based storage (опционально)

**План внедрения:**
1. Изучить MITM proxy patterns (опционально)
2. Изуч project-based storage (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 74. deep-eye
**Репозиторий:** https://github.com/zakirkun/deep-eye
**Категория:** AI-Driven Vulnerability Scanner

**Ключевые функции:**
- Advanced AI-driven vulnerability scanner and penetration testing tool
- Multi-AI Provider Support: OpenAI, Grok, OLLAMA, Claude
- Intelligent Payload Generation: AI-powered, CVE-aware, context-sensitive payloads
- Comprehensive Scanning: 45+ attack methods with framework-specific tests
- Advanced Reconnaissance: Passive OSINT, DNS enumeration, subdomain discovery
- Professional Reporting: PDF/HTML/JSON reports with OSINT intelligence and executive summaries
- Collaborative Scanning: Team-based distributed scanning with session management
- Custom Plugin System: Extend with your own vulnerability scanners
- Multi-Channel Notifications: Email, Slack, Discord alerts
- Vulnerability Detection: SQL Injection, XSS, Command Injection, SSRF, XXE, Path Traversal, CSRF, Open Redirect, CORS Misconfiguration, Security Headers Analysis
- Advanced Modules: API Security Testing, GraphQL Security, Business Logic Flaws, Authentication Testing, File Upload Vulnerabilities, WebSocket Testing, ML-Based Anomaly Detection

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для vulnerability scanning, не применим для Libr4
- Может быть полезен как референс for AI-powered security testing patterns
- Изуч multi-AI provider switching (опционально)

**План внедрения:**
1. Изучить AI-powered security testing patterns (опционально)
2. Изуч multi-AI provider switching (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 75. warp
**Репозиторий:** https://github.com/warpdotdev/warp
**Категория:** Agentic Development Environment

**Ключевые функции:**
- Agentic development environment, born out of the terminal
- Built-in coding agent, or bring your own CLI agent (Claude Code, Codex, Gemini CLI, and others)
- OpenAI is founding sponsor, new agentic management workflows powered by GPT models
- Warp Contributions Overview Dashboard (build.warp.dev) — watch Oz agents triage issues, write specs, implement changes, review PRs
- UI framework (warpui_core and warpui crates) licensed under MIT
- Rest of code licensed under AGPL v3
- Lightweight contribution workflow with readiness labels (ready-to-spec, ready-to-implement)
- Build and run from source: ./script/bootstrap, ./script/run, ./script/presubmit
- Open source dependencies: Tokio, NuShell, Fig Completion Specs, Warp Server Framework, Alacritty, Hyper HTTP library, FontKit, Core-foundation, Smol

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для agentic development environment, не применим для Libr4
- Может быть полезен как референс for agentic UI patterns
- Изуч agent management workflows (опционально)

**План внедрения:**
1. Изучить agentic UI patterns (опционально)
2. Изуч agent management workflows (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 76. do-things
**Репозиторий:** https://github.com/warpdotdev/do-things
**Категория:** Community-Driven Prompts for Warp Agent Mode

**Ключевые функции:**
- Community-driven collection of practical prompts and examples for Warp's Agent Mode
- Warp Drive objects for sharing and reuse
- Live website at dothings.warp.dev
- Four types of objects: Prompts (quick commands), Notebooks (interactive guides), Workflows (automation sequences), Folders (collections)
- Contributing guidelines — create new branch, copy duplicate-me.yaml template, fill fields, submit PR
- Warp Drive object link must have sharing turned on for "Anyone with the link can view"
- Local development with Node.js (v20+), npm/yarn
- Built exclusively using AI tools — Warp's Agent Mode and Dispatch features

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для Warp Agent Mode, не применим для Libr4
- Может быть полезен как референс for prompt collection patterns
- Изуч object sharing system (опционально)

**План внедрения:**
1. Изучить prompt collection patterns (опционально)
2. Изуч object sharing system (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 77. awesome-cursorrules
**Репозиторий:** https://github.com/PatrickJS/awesome-cursorrules
**Категория:** Configuration Files for Cursor AI

**Ключевые функции:**
- Configuration files that enhance Cursor AI editor experience with custom rules and behaviors
- `.cursorrules` files define custom rules for Cursor AI to follow when generating code
- Customized AI behavior tailored to project's specific needs
- Consistency — coding standards and best practices in `.cursorrules` ensure AI generates code aligned with project's style guidelines
- Context awareness — provide AI with important context about project (methods, architectural decisions, libraries)
- Improved productivity — well-defined rules generate code requiring less manual editing
- Team alignment — shared `.cursorrules` file ensures all team members receive consistent AI assistance
- Project-specific knowledge — include project structure, dependencies, unique requirements
- Categories: Frontend Frameworks, Backend and Full-Stack, Mobile Development, CSS and Styling, State Management, Database and API, Testing, Hosting and Deployments, Build Tools and Development, Language-Specific, Other, Documentation, Utilities
- 100+ rule files for various technologies: Angular, Next.js, React, Vue, Python, Go, TypeScript, etc.

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для Cursor AI, не применим для Libr4
- Может быть полезен как референс for rule-based AI guidance patterns
- Изуч project-specific context patterns (опционально)

**План внедрения:**
1. Изучить rule-based AI guidance patterns (опционально)
2. Изуч project-specific context patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 78. Anthropic-Cybersecurity-Skills
**Репозиторий:** https://github.com/mukul975/Anthropic-Cybersecurity-Skills
**Категория:** Cybersecurity Skills Library for AI Agents

**Ключевые функции:**
- The largest open-source cybersecurity skills library for AI agents
- 754 production-grade cybersecurity skills spanning 26 security domains
- Mapped to five industry frameworks: MITRE ATT&CK, NIST CSF 2.0, MITRE ATLAS, MITRE D3FEND, NIST AI RMF
- Compatible with 26+ AI platforms: Claude Code, GitHub Copilot, Cursor, OpenAI Codex CLI, Gemini CLI, and any agentskills.io-compatible platform
- Progressive disclosure architecture — ~30 tokens to scan frontmatter, 500-2,000 tokens to fully load
- Skill anatomy: SKILL.md (YAML frontmatter + Markdown body), references/ (standards.md, workflows.md), scripts/, assets/
- 26 security domains: Cloud Security, Threat Hunting, Threat Intelligence, Web Application Security, Network Security, Malware Analysis, Digital Forensics, Security Operations, IAM, SOC Operations, Container Security, OT/ICS Security, API Security, Vulnerability Management, Incident Response, Red Teaming, Penetration Testing, Endpoint Security, DevSecOps, Phishing Defense, Cryptography, Zero Trust Architecture, Mobile Security, Ransomware Defense, Compliance & Governance, Deception Technology
- Casky.ai playground for live cybersecurity skill exercises

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для cybersecurity skills, не применим для Libr4
- Может быть полезен как референс for skill library architecture
- Изуч progressive disclosure patterns (опционально)

**План внедрения:**
1. Изучить skill library architecture (опционально)
2. Изуч progressive disclosure patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 79. AI-IDE-Agent
**Репозиторий:** https://github.com/your-repo/AI-IDE-Agent
**Категория:** Chinese AI Agent Prompts Collection

**Ключевые функции:**
- 61 professional domain agent prompts for Claude/Cursor/trae
- Chinese-language prompts optimized for specific professional domains
- 6 main categories: Programming Language Experts (13 files), Cloud Architecture & DevOps (5 files), Data & AI (8 files), Business & Product (24 files), Security & Quality (6 files), Mobile & Game Development (5 files)
- Programming experts: C#, C++, C, Elixir, Golang, Java, JavaScript, PHP, Python, Ruby, Rust, Scala, TypeScript
- Cloud & DevOps: Cloud Architect, DevOps Troubleshooter, MLOps Engineer, Terraform Expert, Deployment Engineer
- Data & AI: AI Engineer, Data Engineer, Data Scientist, DBA, Database Optimizer, ML Engineer, Prompt Engineer, Search Expert
- Business & Product: API Documentation Engineer, GraphQL Architect, Mermaid Expert, SQL Expert, UI-UX Designer, Business Analyst, Content Marketer, Customer Support, Quantitative Analyst, Risk Manager, Legal Counsel, Sales Automation Expert, Network Engineer, Documentation Architect, Architecture Reviewer, Backend Architect, Frontend Developer, Performance Engineer, DX Optimizer, Tutorial Engineer, Reference Builder, Context Manager, Legacy System Modernization Expert, Payment Integration
- Security & Quality: Security Auditor, Code Reviewer, Error Detective, Test Automation Expert, Debugger, Incident Responder
- Mobile & Game: Flutter Expert, iOS Developer, Mobile Developer, Unity Developer, Minecraft Bukkit Expert

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для Chinese AI agent prompts, не применим для Libr4
- Может быть полезен как референс for domain-specific prompt patterns
- Изуч professional domain categorization (опционально)

**План внедрения:**
1. Изучить domain-specific prompt patterns (опционально)
2. Изуч professional domain categorization (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 80. AI-Research-SKILLs
**Репозиторий:** https://github.com/orchestra-research/AI-research-SKILLs
**Категория:** AI Research Skills Library

**Ключевые функции:**
- The most comprehensive open-source skills library enabling AI agents to autonomously conduct AI research — from idea to paper
- 98 skills powering AI research in 2026 across 23 categories
- Autoresearch skill — central orchestration layer using two-loop architecture (inner optimization + outer synthesis)
- Categories: Autoresearch (1), Ideation (2), ML Paper Writing (2), Model Architecture (5), Fine-Tuning (4), Post-Training (8), Distributed Training (6), Optimization (6), Inference (4), Tokenization (2), Data Processing (2), Evaluation (3), Safety & Alignment (4), Agents (4), RAG (5), Multimodal (7), Prompt Engineering (4), MLOps (3), Observability (2), Infrastructure (3), Mech Interp (4), Emerging Techniques (6), Agent-Native Research Artifact (3)
- Quick install: npx @orchestra-research/ai-research-skills
- Auto-detects installed coding agents (Claude Code, Hermes Agent, OpenCode, Cursor, Gemini CLI, etc.)
- Installs skills to ~/.orchestra/skills/ with symlinks to each agent
- Skill structure: SKILL.md (quick reference 50-150 lines), references/ (deep documentation 300KB+), scripts/, assets/
- Quality standards: 300KB+ documentation, real GitHub issues & solutions, code examples, version history
- Demos: Norm Heterogeneity → LoRA Brittleness, RL Algorithm Brain Scan, NeMo Eval GPQA Benchmark

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для AI research skills, не применим для Libr4
- Может быть полезен как референс for skill library architecture
- Изуч autoresearch orchestration patterns (опционально)

**План внедрения:**
1. Изучить skill library architecture (опционально)
2. Изуч autoresearch orchestration patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 81. Review-Gate
**Репозиторий:** https://github.com/LakshmanTurlapati/Review-Gate
**Категория:** Cursor IDE Review Tool

**Ключевые функции:**
- Review Gate for Cursor IDE — turns 500 Cursor requests into 2500 by making the AI wait for user feedback
- V2 with voice commands, image uploads, and beautiful popup interface
- Voice-activated AI control using local Faster-Whisper AI for speech-to-text
- Visual context sharing with image uploads (PNG, JPG, JPEG, GIF, BMP, WebP)
- Beautiful popup interface with orange-glow design and real-time MCP status indicators
- MCP integration — built on Model Context Protocol for seamless Cursor integration
- Multi-modal input: text commands, voice input, image upload
- Makes Cursor Agent wait for user "go-ahead" via interactive popup before signing off
- Multiplies request power — one main request does work of many through iterative sub-prompts
- Cross-platform support: macOS, Windows, Linux
- One-click installer handling dependencies, MCP server, extension, and configuration

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для Cursor IDE, не применим для Libr4
- Может быть полезен как референс for MCP integration patterns
- Изуч multi-modal input patterns (опционально)

**План внедрения:**
1. Изучить MCP integration patterns (опционально)
2. Изуч multi-modal input patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 82. Roo-Code
**Репозиторий:** https://github.com/RooCodeInc/Roo-Code
**Категория:** AI-Powered Dev Team Extension

**Ключевые функции:**
- AI-Powered Dev Team, right in your editor — VS Code extension
- Generate code from natural language descriptions and specs
- Modes: Code Mode (everyday coding), Architect Mode (plan systems, specs, migrations), Ask Mode (fast answers, explanations), Debug Mode (trace issues, add logs, isolate root causes), Custom Modes (specialized modes for team/workflow)
- Refactor & debug existing code
- Write & update documentation
- Answer questions about codebase
- Automate repetitive tasks
- Utilize MCP Servers
- GPT-5.5 support via OpenAI Codex provider
- Claude Opus 4.7 support on Vertex AI
- Previous checkpoint navigation controls in chat
- Community team carrying Roo Code forward after original team moved to Roomote
- Available in 20+ languages
- Development mode with hot reload, automated VSIX installation

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для VS Code extension, не применим для Libr4
- Может быть полезен как референс for AI dev team patterns
- Изуч mode-based agent adaptation (опционально)

**План внедрения:**
1. Изуч AI dev team patterns (опционально)
2. Изуч mode-based agent adaptation (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 83. RooFlow
**Репозиторий:** https://github.com/GreatScottyMac/RooFlow
**Категория:** Alternative System Prompt Format for Roo Code

**Ключевые функции:**
- Experimental alternative system prompt format for Roo Code with YAML-based prompts
- Persistent project context through Memory Bank system
- Five integrated modes: Flow-Architect, Flow-Code, Flow-Debug, Flow-Ask, Flow-Orchestrator
- Reduced token consumption with optimized prompts and instructions
- Simplified setup with installation scripts for Windows/Linux/macOS
- Streamlined real-time Memory Bank updates
- Clearer instructions with YAML-based rule files
- Memory Bank structure: activeContext.md, decisionLog.md, productContext.md, progress.md, systemPatterns.md
- Mode collaboration — Flow-Orchestrator can delegate tasks to other modes
- Optional integration with Context Portal MCP
- Import connected MCP server tools during installation

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для Roo Code, не применим для Libr4
- Может быть полезен как референс for YAML-based prompt patterns
- Изуч Memory Bank system architecture (опционально)

**План внедрения:**
1. Изучить YAML-based prompt patterns (опционально)
2. Изуч Memory Bank system architecture (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 84. Windsurf-Tool
**Репозиторий:** https://github.com/your-repo/Windsurf-Tool
**Категория:** Windsurf Account Management Tool

**Ключевые функции:**
- One-click account switching, credit query, import, batch registration, card binding link, automatic card binding
-适用于 Mac Windows
- Fully open source, local execution, no backend server
- Token acquisition via Firebase authentication API (through Cloudflare Workers for China access)
- Calls Windsurf official API: register.windsurf.com to get API Key
- Local storage: ~/Library/Application Support/windsurf-tool/ (Mac) or %APPDATA%/windsurf-tool/ (Windows)
- No remote server, doesn't collect any user data
- Cloudflare Workers proxy only for China Firebase access issues
- Key files: js/accountLogin.js (token acquisition logic), main.js (main process IPC), js/constants.js (external API addresses)
- Disclaimer: for learning and research only, not for commercial use, project has stopped maintenance

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для Windsurf account management, не применим для Libr4
- Может быть полезен как референс for local storage patterns
- Изуч token acquisition patterns (опционально)

**План внедрения:**
1. Изучить local storage patterns (опционально)
2. Изуч token acquisition patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 85. OpenAnalyst
**Репозиторий:** https://github.com/OpenAnalystInc/OpenAnalyst
**Категория:** VS Code AI Agent for Data Analytics

**Ключевые функции:**
- Open-source VS Code AI agent specialized in data analytics and general coding tasks
- Merged features from KiloCode, Roo Code, and Cline
- Generate code from natural language
- Data Analytics Mode — specialized AI assistance for data analytics tasks
- Checks its own work
- Run terminal commands
- Automate the browser
- Latest AI models (Claude 4 Sonnet/Opus, Gemini 2.5 Pro)
- API keys option
- Task automation, automated refactoring
- MCP Server Marketplace — easily find and use MCP servers to extend agent capabilities
- Multi-Mode Operation: Data Analyst, Code, Ask, Debug, custom modes
- Smart alerts for task completion and progress
- Seamless AI integration with multiple AI services
- Conversation refinement — edit and enhance chat history
- Git assistance — automatically generate descriptive commit messages
- Data Analytics specialization: pandas, numpy, matplotlib, scikit-learn, statistical analysis
- Data visualization guidance

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для VS Code extension, не применим для Libr4
- Может быть полезен как референс for data analytics agent patterns
- Изуч multi-mode operation patterns (опционально)

**План внедрения:**
1. Изучить data analytics agent patterns (опционально)
2. Изуч multi-mode operation patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 86. andrej-karpathy-skills
**Репозиторий:** https://github.com/forrestchang/andrej-karpathy-skills
**Категория:** Karpathy-Inspired Claude Code Guidelines

**Ключевые функции:**
- Single CLAUDE.md file to improve Claude Code behavior
- Derived from Andrej Karpathy's observations on LLM coding pitfalls
- Four principles addressing common LLM issues:
  - Think Before Coding — state assumptions explicitly, present multiple interpretations, push back when warranted, stop when confused
  - Simplicity First — minimum code that solves the problem, no speculative features, no abstractions for single-use, no flexibility that wasn't requested
  - Surgical Changes — touch only what you must, clean up only your own mess, don't improve adjacent code, match existing style
  - Goal-Driven Execution — define success criteria, loop until verified, transform imperative tasks into verifiable goals
- Install via Claude Code Plugin (recommended) or CLAUDE.md (per-project)
- Cursor project rule included for Cursor IDE
- Key insight: "LLMs are exceptionally good at looping until they meet specific goals... Don't tell it what to do, give it success criteria and watch it go"
- Guidelines bias toward caution over speed for non-trivial work
- Designed to be merged with project-specific instructions

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен для улучшения поведения агента Libr4
- Адаптировать four principles для Libr4
- Изуч goal-driven execution patterns

**План внедрения:**
1. Адаптировать four principles для Libr4
2. Изуч goal-driven execution patterns

**Сложность:** Низкая
**Приоритет:** Средний

---

### 87. antigravity-awesome-skills
**Репозиторий:** https://github.com/sickn33/antigravity-awesome-skills
**Категория:** 1,441+ Agentic Skills Library

**Ключевые функции:**
- Installable GitHub library of 1,441+ agentic skills for Claude Code, Cursor, Codex CLI, Gemini CLI, Antigravity, Kiro, OpenCode, GitHub Copilot, and other AI coding assistants
- Reusable SKILL.md playbooks with structured operating instructions
- Skills library in skills/, installer CLI powered by npm package, generated catalog and metadata
- Hosted and local web app, role-based bundles, execution workflows
- Installation: npx antigravity-awesome-skills (full library) or tool-specific flags (--claude, --cursor, --gemini, --codex, --antigravity, --kiro, --path)
- Universal starter skills: brainstorming, test-driven-development, debugging-strategies, lint-and-validate, security-auditor, frontend-design, api-design-principles, create-pr
- Bundles for role-based recommendations: Web Wizard, Security Engineer, OSS Maintainer, Essentials, Full-Stack Developer, QA & Testing, Security Developer, DevOps & Cloud, Observability & Monitoring
- Workflows for outcome-driven execution: shipping SaaS MVP, security audits, AI agent systems, QA/browser automation, DDD-oriented design
- Official sources: anthropics/skills, anthropics/claude-cookbooks, remotion-dev/skills, vercel-labs/agent-skills, openai/skills, supabase/agent-skills, microsoft/skills, google-gemini/gemini-skills, and many more
- Community contributors: 100+ sources contributing skills

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен для адаптации ключевых навыков под Libr4
- Адаптировать key skills и bundle system для Libr4
- Изуч skill library architecture

**План внедрения:**
1. Адаптировать key skills и bundle system для Libr4
2. Изуч skill library architecture

**Сложность:** Средняя
**Приоритет:** Средний

---

### 88. AIUsage
**Репозиторий:** https://github.com/sylearn/AIUsage
**Категория:** AI Subscriptions Dashboard

**Ключевые функции:**
- One dashboard for all AI subscriptions — quotas, costs, accounts, and Claude Code proxy
- 10+ AI providers: Codex, Copilot, Cursor, Antigravity, Kiro, Warp, Gemini CLI, Amp, Droid, Claude Code
- Multi-account support — multiple accounts per provider, independent refresh, one-click CLI switching
- Claude Code stats — per-model cost & token breakdown, trend charts, time-period analysis
- Claude Code proxy — use Claude Code with DeepSeek, GPT, Ollama or any OpenAI-compatible model; Anthropic passthrough for usage logging
- Proxy stats — per-model cost/token trends, distribution charts, configurable log retention
- Menu bar — multi-account status bar icons with quota/cost metrics, quick-glance popover with summary stats, colored progress bars, cost tracking
- Credential vault — macOS Keychain storage for managed credentials
- macOS 14+ SwiftUI native app

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для macOS dashboard, не применим для Libr4
- Может быть полезен как референс for multi-provider API patterns
- Изуч proxy implementation patterns (опционально)

**План внедрения:**
1. Изуч multi-provider API patterns (опционально)
2. Изуч proxy implementation patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 89. awesome-pm-skills
**Репозиторий:** https://github.com/menkesu/awesome-pm-skills
**Категория:** PM Builder Skills

**Ключевые функции:**
- 28 AI-powered skills built from Lenny's podcast transcripts by Lenny Rachitsky
- The most domain specific PM Builder skills for the whole product management lifecycle
- Based on 300+ episodes with Brian Chesky (Airbnb), Shreyas Doshi (Stripe/Twitter), Kevin Weil (OpenAI CPO), Dylan Field (Figma), Marty Cagan (SVPG), and 40+ world-class PMs
- Active, actionable skills that Claude Code and Cursor use in real-time while building products
- Six modes: Builder Mode (11 skills), Communicator Mode (4 skills), Strategist Mode (4 skills), Navigator Mode (3 skills), Leader Mode (3 skills), Measurement Mode (2 skills), Launch Mode (1 skill)
- Meta-skill: one-step-better-ai-pm — fetches latest GenAI PM briefs, analyzes repo, finds relevant matches, applies improvements
- Skills include: zero-to-launch, strategic-build, continuous-discovery, design-first-dev, ai-product-patterns, ai-startup-building, jtbd-building, growth-embedded, exp-driven-dev, quality-speed, ship-decisions, strategic-storytelling, positioning-craft, exec-comms, confident-speaking, decision-frameworks, strategy-frameworks, okr-frameworks, prioritization-craft, influence-craft, stakeholder-craft, workplace-navigation, culture-craft, career-growth, strategic-pm, metrics-frameworks, user-feedback-system, launch-execution

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для PM skills, не применим для Libr4
- Может быть полезен как референс for skill-based agent patterns
- Изуч skill categorization system (опционально)

**План внедрения:**
1. Изуч skill-based agent patterns (опционально)
2. Изуч skill categorization system (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 90. material-3-skill
**Репозиторий:** https://github.com/hamen/material-3-skill
**Категория:** Material Design 3 Skill for Claude Code

**Ключевые функции:**
- Comprehensive Claude Code skill for implementing Google's Material Design 3 (Material You) UI system
- Primary focus: Jetpack Compose — MaterialTheme, Material 3 composables, adaptive layouts, edge-to-edge/insets, current Compose Material3 patterns
- Secondary coverage: Flutter (useMaterial3, ColorScheme.fromSeed, etc.)
- Limited web support: @material/web is in maintenance mode, M3 Expressive not implemented on Web
- Covers 30+ components with Compose-oriented mappings plus web element names
- Includes MD3 compliance audit mode that scores apps across 10 categories (works for Compose/Kotlin, Flutter/Dart, and web/CSS)
- Covers M3 Expressive (May 2025) with explicit per-platform matrix
- Installation: copy to Claude Code skills directory or symlink
- Usage: /material-3 component, /material-3 theme, /material-3 scaffold, /material-3 audit

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для Material Design 3, не применим для Libr4
- Может быть полезен как референс for design system skill patterns
- Изуч compliance audit patterns (опционально)

**План внедрения:**
1. Изуч design system skill patterns (опционально)
2. Изуч compliance audit patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 91. Open-ClaudeCode
**Репозиторий:** https://github.com/LING71671/Open-ClaudeCode
**Категория:** Open Source Claude Code

**Ключевые функции:**
- Complete open source Claude Code project — rebuilt from Anthropic official source code
- Source code recovered from official npm package source maps (1,902 source files)
- Runnable CLI (v2.1.88) with compiled executable
- 13 Anthropic official plugins included
- Configuration examples for multiple scenarios (strict/lax/bash-sandbox)
- Complete documentation, usage guide, CHANGELOG
- Directory structure: package/ (CLI), src/ (TypeScript source), plugins/ (official plugins), examples/ (settings)
- Source includes: 30+ tools (184 files), 50+ commands (207 files), API/MCP/OAuth services (130 files), React UI components (389 files), Ink UI framework (96 files), utils (564 files), React hooks (104 files), bridge modules (31 files), vendor native modules (4 files)
- Supports Node.js 18+, multiple authentication methods (third-party proxy, Anthropic official API, Claude subscription OAuth)
- Interactive mode, non-interactive mode, session continuation
- Permission modes: default, acceptEdits, dangerously-skip-permissions
- Built-in commands: /help, /clear, /compact, /model, /theme, /vim, /cost, /stats, /share, /exit
- Supports 6 platforms (macOS/Linux/Windows × arm64/x64)
- License: Anthropic PBC copyright, for learning and research only

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен как референс for Claude Code architecture
- Изуч tool implementation patterns
- Изуч command implementation patterns
- Изуч plugin system architecture

**План внедрения:**
1. Изуч tool implementation patterns (опционально)
2. Изуч command implementation patterns (опционально)
3. Изуч plugin system architecture (опционально)

**Сложность:** Средняя
**Приоритет:** Средний

---

### 92. OpenSees
**Репозиторий:** https://github.com/OpenSees/OpenSees
**Категория:** Structural Engineering Simulation Framework

**Ключевые функции:**
- OpenSees Source Code Repository — all revisions since Version 2.3.2
- Structural engineering simulation framework
- Documentation moved to parallel GitHub repo: OpenSees/OpenSeesDocumentation
- Build instructions for Windows, Linux, and Mac available
- Fork-based collaboration model — only pull requests considered
- Modeling questions should be posted on OpenSees message board or Facebook group
- Community resources: opensees.berkeley.edu/community, facebook.com/groups/opensees

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для structural engineering simulation, не применим для Libr4
- Может быть полезен как референс for simulation framework patterns (опционально)

**План внедрения:**
1. Изуч simulation framework patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 93. mxnet
**Репозиторий:** https://github.com/apache/mxnet
**Категория:** Apache MXNet Deep Learning Framework

**Ключевые функции:**
- Apache MXNet for Deep Learning — designed for both efficiency and flexibility
- Mix symbolic and imperative programming to maximize efficiency and productivity
- Dynamic dependency scheduler that automatically parallelizes symbolic and imperative operations
- Graph optimization layer for fast and memory-efficient symbolic execution
- Portable, lightweight, scalable to many GPUs and machines
- NumPy-like programming interface with Gluon 2.0
- Automatic hybridization for imperative programming with symbolic performance
- Lightweight, memory-efficient, portable to smart devices through ARM cross-compilation
- Scales to multi GPUs and distributed setting with auto parallelism (ps-lite, Horovod, BytePS)
- Extensible backend supporting full customization, custom accelerator libraries, in-house hardware
- Support for Python, Java, C++, R, Scala, Clojure, Go, Javascript, Perl, Julia
- Cloud-friendly, directly compatible with AWS and Azure
- Apache-2.0 licensed
- Ecosystem projects: TVM, TensorRT, OpenVINO, MXNet.js, MXNet Memory Monger

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для deep learning, не применим для Libr4
- Может быть полезен как референс for distributed system patterns (опционально)

**План внедрения:**
1. Изуч distributed system patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 94. vowpal_wabbit
**Репозиторий:** https://github.com/VowpalWabbit/vowpal_wabbit
**Категория:** Fast Online Learning System

**Ключевые функции:**
- Vowpal Wabbit fast online learning code
- Machine learning system pushing frontier with online, hashing, allreduce, reductions, learning2search, active, and interactive learning
- Specific focus on reinforcement learning with contextual bandit algorithms
- Flexible input format — examples can have free form text interpreted in bag-of-words way, multiple sets in different namespaces
- Speed — fast online algorithm implementations with sparse gradient descent baseline
- Scalability — memory footprint bounded independent of data, training set not loaded into main memory, feature size bounded using hashing trick
- Feature interaction — subsets of features internally paired for ranking problems
- Builds on Linux, macOS, Windows with CI/CD
- Python Jupyter notebook examples, CLI tutorials

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для online learning, не применим для Libr4
- Может быть полезен как референс for online learning patterns (опционально)

**План внедрения:**
1. Изуч online learning patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 95. context-engineering-kit
**Репозиторий:** https://github.com/NeoLabHQ/context-engineering-kit
**Категория:** Advanced Context Engineering Techniques

**Ключевые функции:**
- Hand-crafted collection of advanced context engineering techniques and patterns for Claude Code, OpenCode, Cursor, Antigravity and more
- Minimal token footprint, focused on improving agent result quality and predictability
- Simple to use without dependencies, automatically used skills and self-explanatory commands
- Token-efficient — carefully crafted prompts preferring command-oriented skills with sub-agents
- Quality-focused — each plugin meaningfully improves agent results in specific area
- Granular — install only plugins needed, each loads only specific agents, commands, skills without overlap
- Scientifically proven — based on proven techniques and patterns from well-trusted benchmarks and studies
- Open-standards — skills based on agentskills.io specification
- Plugins: Reflexion, Spec-Driven Development (SDD), Review, Git, Test-Driven Development (TDD), Subagent-Driven Development (SADD), Domain-Driven Development (DDD), First Principles Framework (FPF), Kaizen, Customaize Agent, Docs, Tech Stack, MCP
- Agent Reliability Engineering — improves accuracy and consistency, reduces hallucinations and bias
- SDD plugin — development as compilation through reliable code generation, tested on real-life production projects
- Reflexion plugin — based on Self-Refine and Reflexion papers, increases output quality by 8-21%
- FPF plugin — First Principles Framework for rigorous, auditable reasoning with ADI cycle (Abduction-Deduction-Induction)

**Интеграция в Libr4:**
- **Высокая применимость** - может быть полезен для адаптации context engineering patterns
- Адаптировать key plugins (Reflexion, SDD, SADD) для Libr4
- Изуч agent reliability engineering patterns

**План внедрения:**
1. Адаптировать key plugins (Reflexion, SDD, SADD) для Libr4
2. Изуч agent reliability engineering patterns

**Сложность:** Средняя
**Приоритет:** Высокий

---

### 96. awesome-design-md
**Репозиторий:** https://github.com/VoltAgent/awesome-design-md
**Категория:** DESIGN.md Collection

**Ключевые функции:**
- Curated collection of 69 DESIGN.md files inspired by developer focused websites
- DESIGN.md is a plain-text design system document that AI agents read to generate consistent UI
- Based on Google Stitch concept — markdown file that AI coding agents or Google Stitch instantly understands
- Categories: AI & LLM Platforms (Claude, Cohere, ElevenLabs, Minimax, Mistral AI, Ollama, OpenCode AI, Replicate, RunwayML, Together AI, VoltAgent, xAI), Developer Tools & IDEs (Cursor, Expo, Lovable, Raycast, Superhuman, Vercel, Warp), Backend, Database & DevOps (ClickHouse, Composio, HashiCorp, MongoDB, PostHog, Sanity, Sentry, Supabase), Productivity & SaaS (Cal.com, Intercom, Linear, Mintlify, Notion, Resend, Zapier), Design & Creative Tools (Airtable, Clay, Figma, Framer, Miro, Webflow), Fintech & Crypto (Binance, Coinbase, Kraken, Mastercard, Revolut, Stripe, Wise), E-commerce & Retail (Airbnb, Meta, Nike, Shopify, Starbucks), Media & Consumer Tech (Apple, IBM, NVIDIA, Pinterest, PlayStation, SpaceX, Spotify, The Verge, Uber, Vodafone, WIRED), Automotive (BMW, Bugatti, Ferrari, Lamborghini, Renault, Tesla)
- Each file follows Stitch DESIGN.md format with sections: Visual Theme & Atmosphere, Color Palette & Roles, Typography Rules, Component Stylings, Layout Principles, Depth & Elevation, Do's and Don'ts, Responsive Behavior, Agent Prompt Guide
- Each site includes DESIGN.md (design system), preview.html (visual catalog), preview-dark.html (dark catalog)

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для design system documentation, не применим для Libr4
- Может быть полезен как референс for design system documentation patterns (опционально)

**План внедрения:**
1. Изуч design system documentation patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 97. Claude-Code-Game-Studios
**Репозиторий:** https://github.com/Donchitos/Claude-Code-Game-Studios
**Категория:** Game Development Studio Template

**Ключевые функции:**
- Turn a single Claude Code session into a full game development studio
- 49 agents, 72 skills, 12 hooks, 11 rules, 39 templates
- Specialized subagents across design, programming, art, audio, narrative, QA, and production
- Studio hierarchy: Tier 1 Directors (creative-director, technical-director, producer), Tier 2 Department Leads (game-designer, lead-programmer, art-director, audio-director, narrative-director, qa-lead, release-manager, localization-lead), Tier 3 Specialists (gameplay-programmer, engine-programmer, ai-programmer, network-programmer, tools-programmer, ui-programmer, systems-designer, level-designer, economy-designer, technical-artist, sound-designer, writer, world-builder, ux-designer, prototyper, performance-analyst, devops-engineer, analytics-engineer, security-engineer, qa-tester, accessibility-specialist, live-ops-designer, community-manager)
- Engine specialists for Godot 4, Unity, Unreal Engine 5
- 72 slash commands: /start, /brainstorm, /design-system, /create-epics, /create-stories, /dev-story, /sprint-plan, /design-review, /code-review, /qa-plan, /release-checklist, /team-combat, /team-narrative, etc.
- Hooks for automated validation on commits, pushes, asset changes, session lifecycle, agent audit trail, gap detection
- Path-scoped coding standards for gameplay, core, AI, networking, UI, design docs, tests, prototypes
- Agent coordination: vertical delegation, horizontal consultation, conflict resolution, change propagation, domain boundaries
- Collaborative, not autonomous — agents ask questions, present options, user decides, draft, approve
- Design philosophy: MDA Framework, Self-Determination Theory, Flow State Design, Bartle Player Types, Verification-Driven Development
- MIT License

**Интеграция в Libr4:**
- **Низкая применимость** - специфичный продукт для game development, не применим для Libr4
- Может быть полезен как референс for multi-agent coordination patterns (опционально)

**План внедрения:**
1. Изуч multi-agent coordination patterns (опционально)

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 98. andrej-karpathy-skills-cursor-vscode
**Репозиторий:** https://github.com/MichielWBeijen/andrej-karpathy-skills-cursor-vscode
**Категория:** Karpathy Guidelines Extension

**Ключевые функции:**
- Turns the Karpathy-inspired coding guidelines into rule files for Cursor and VS Code
- Based on andrej-karpathy-skills repository (MIT License)
- VS Code extension available on Microsoft Marketplace
- Cursor extension available on Open VSX Registry
- Commands: Karpathy Rules: Add to project (.cursor/rules), Karpathy Rules: Add to .cursorrules (legacy)
- Four principles: Think Before Coding, Simplicity First, Surgical Changes, Goal-Driven Execution
- Manual setup: copy rules/karpathy-guidelines.md into .cursor/rules/ or .cursorrules
- Extension is just markdown file with no runtime, no dependencies

**Интеграция в Libr4:**
- **Низкая применимость** - это вариант andrej-karpathy-skills, который уже изучен (#86)
- Не требует отдельного изучения

**План внедрения:**
1. Не требуется — это вариант уже изученного репозитория

**Сложность:** Низкая
**Приоритет:** Низкий

---

### 99. Browser MCP
**Репозиторий:** https://github.com/browser-mcp/browser-mcp
**Категория:** Browser Automation MCP Server

**Ключевые функции:**
- MCP server + Chrome extension for browser automation with AI applications (VS Code, Claude, Cursor, Windsurf)
- Fast — automation happens locally on machine, better performance without network latency
- Private — browser activity stays on device, not sent to remote servers
- Logged In — uses existing browser profile, keeps logged into all services
- Stealth — avoids basic bot detection and CAPTCHAs by using real browser fingerprint
- Adapted from Playwright MCP server to automate user's browser rather than creating new browser instances
- Allows using user's existing browser profile to use logged-in sessions and avoid bot detection mechanisms

**Интеграция в Libr4:**
- **Средняя применимость** - может быть полезен для browser automation в Libr4
- Изуч MCP server patterns for browser automation
- Рассмотреть интеграцию с agent-browser (если применимо)

**План внедрения:**
1. Изуч MCP server patterns for browser automation (опционально)
2. Рассмотреть интеграцию с agent-browser (если применимо)

**Сложность:** Средняя
**Приоритет:** Средний

---

## Приоритет 3: Специализированные инструменты (низкая применимость)

### Decepticon
**Репозиторий:** https://github.com/PurpleAILAB/Decepticon
**Категория:** Автономный агент для пентеста

**Ключевые функции:**
- Полный kill chain без ручного участия
- Имитация реального противника
- Цепочки кибератак
- Docker контейнер для изоляции

**Интеграция в Libr4:**
- **Низкая применимость** - не подходит для Libr4 (IDE)
- Может быть полезен для security testing модуля

**Сложность:** Высокая
**Приоритет:** Низкий (для security модуля)

---

### Pentest-Swarm-AI
**Репозиторий:** https://github.com/Armur-Ai/Pentest-Swarm-AI
**Категория:** Рой AI агентов для пентеста

**Ключевые функции:**
- Рой агентов для разведки, классификации, эксплуатации
- ReAct рассуждение
- Bug bounty, CTF режимы
- 7+ нативных инструментов безопасности

**Интеграция в Libr4:**
- **Низкая применимость** - не подходит для Libr4 (IDE)
- Может быть полезен для security testing модуля

**Сложность:** Высокая
**Приоритет:** Низкий (для security модуля)

---

### Bug Hunter
**Репозиторий:** https://github.com/codexstar69/bug-hunter
**Категория:** Security ревью кода

**Ключевые функции:**
- Pipeline: Triage → Recon → Hunt → Skeptic → Referee → Auto-Fix → Verify
- Поиск реальных багов
- Отсечение ложных срабатываний
- Безопасный патчинг кода

**Интеграция в Libr4:**
- **Средняя применимость** - может улучшить security ревью
- Интегрировать в security testing модуль
- Добавить pipeline для security ревью

**Сложность:** Средняя
**Приоритет:** Средний (для security модуля)

---

## Другие репозитории

### andrej-karpathy-skills
**Репозиторий:** https://github.com/forrestchang/andrej-karpathy-skills
**Категория:** Навыки в стиле Андрея Карпаты

**Ключевые функции:**
- Превращает модель в "сооснователя OpenAI"
- Перестает врать и галлюцинировать
- Проверяет каждую строчку программы

**Интеграция в Libr4:**
- **Средняя применимость** - может улучшить качество кода
- Интегрировать ключевые принципы в систему промптов
- Добавить валидацию кода

**Сложность:** Низкая
**Приоритет:** Средний

---

### nothing-design-skill
**Репозиторий:** https://github.com/dominikmartn/nothing-design-skill
**Категория:** Nothing дизайн стиль

**Ключевые функции:**
- Монохромный индустриальный стиль
- Швейцарская типографика
- Матричные паттерны
- OLED-черный

**Интеграция в Libr4:**
- **Низкая применимость** - специфический стиль
- Может быть добавлен как опция в UI генерацию

**Сложность:** Низкая
**Приоритет:** Низкий

---

## Рекомендации по приоритетам внедрения

### Фаза 1 (Немедленно)
1. **gnhf** - критично для улучшения оркестрации агентов
2. **claude-skills** - значительно расширит возможности агентов
3. **superpowers** - улучшит качество генерируемого кода

### Фаза 2 (Ближайшее будущее)
4. **evolver** - улучшит самообучение агентов
5. **material-3-skill** - улучшит генерацию UI
6. **andrej-karpathy-skills** - улучшит качество кода

### Фаза 3 (Среднесрочно)
7. **browser-harness-js** - заменит Obscura
8. **OpenHarness** - улучшит управление агентами
9. **GenericAgent** - улучшит самообучение
10. **Bug Hunter** - улучшит security ревью

### Фаза 4 (Долгосрочно)
11. **hue** - улучшит генерацию дизайн-систем
12. **architecture-diagram-generator** - улучшит документацию
13. **nothing-design-skill** - добавит опцию стиля

---

## Технические замечания

### Языковые различия
- Большинство репозиториев на TypeScript/JavaScript
- Libr4 на C#
- Необходима адаптация концепций и архитектур

### Зависимости
- claude-skills: 305 Python инструментов без зависимостей - нужно C# аналоги
- browser-harness-js: Bun runtime - нужно C# WebSocket клиент
- evolver: Node.js - нужно C# реализация GEP протокола

### Безопасность
- Decepticon, Pentest-Swarm-AI: не подходят для IDE
- Bug Hunter: полезен для security модуля
- skill-security-auditor из claude-skills: полезен для валидации

---

## Заключение

Из 99 изученных репозиториев наиболее релевантными для интеграции в Libr4 являются:

1. **gnhf** - критично для оркестрации агентов
2. **claude-skills** - значительно расширит возможности
3. **superpowers** - улучшит качество кода
4. **evolver** - улучшит самообучение
5. **material-3-skill** - улучшит UI генерацию

Рекомендуется начать с Фазы 1 и внедрить gnhf, claude-skills и superpowers для немедленного улучшения функциональности Libr4.
