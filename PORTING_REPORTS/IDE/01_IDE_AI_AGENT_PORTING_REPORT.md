# Отчёт о портировании: ide_ai_agent.py

## 📊 Общая информация

| Параметр | Значение |
|----------|----------|
| **Исходный файл** | `D:\Desktop\freelance_libr4-main\backend\app\api\endpoints\ide_ai_agent.py` |
| **Размер** | 297.1 KB (6,030 строк) |
| **Язык оригинала** | Python 3.11 + FastAPI |
| **Целевой язык** | C# 12 + ASP.NET Core Minimal API |
| **Сложность** | 🔴 **КРИТИЧЕСКАЯ** |
| **Оценка времени** | 3-4 недели |

---

## 📋 Что содержит оригинал

### Основные компоненты

#### 1. Celery Tasks (async background processing)
```python
@celery_app.task(bind=True, soft_time_limit=28800, time_limit=29400)
def process_agent_task(self, prompt, workspace_id, context_files, execution_context):
    """8-hour background task for agent processing"""
```
**Статус:** ❌ Не портировано
**Замена:** Hangfire + BackgroundService

#### 2. IDE Agent Workflow
- Plan-first flow (планирование перед выполнением)
- Hashline format для code edits (10x accuracy)
- Code review, refactor, test generation
- Project rules

**Статус:** ❌ Не портировано

#### 3. Service Dependencies (20+ сервисов)
| Сервис | Python файл | C# статус |
|--------|-------------|-----------|
| `ide_agent_cascade_service` | `app/services/ide_agent_cascade_service.py` | ❌ Нет |
| `ide_agent_intelligence_router_service` | `app/services/ide_agent_intelligence_router_service.py` | ❌ Нет |
| `ide_agent_web_search_service` | `app/services/ide_agent_web_search_service.py` | ❌ Нет |
| `shadow_workspace_service` | `app/services/shadow_workspace_service.py` | ❌ Нет |
| `safe_file_operations` | `app/services/safe_file_operations.py` | ❌ Нет |
| `agent_task_record_service` | `app/services/agent_task_record_service.py` | ❌ Нет |
| `agent_orchestration_run_service` | `app/services/agent_orchestration_run_service.py` | ❌ Нет |
| `autonomous_runtime_policy_service` | `app/services/autonomous_runtime_policy_service.py` | ❌ Нет |
| `task_decomposition_service` | `app/services/task_decomposition_service.py` | ❌ Нет |
| `ide_agent_senior_role_prompts` | `app/services/ide_agent_senior_role_prompts.py` | ❌ Нет |
| `ai_usage_logger` | `app/services/ai_usage_logger.py` | ❌ Нет |
| `github_template_bootstrap_service` | `app/services/github_template_bootstrap_service.py` | ❌ Нет |

---

## 📁 Структура доменной модели (C# - ✅ ДОБАВЛЕНЫ DOMAIN METHODS)

### Уже есть в C# (Domain с domain methods):
```csharp
// Libr4.AI.Domain.IDEAIAgent/IDEAgent.cs
public class AIAgent
{
    // ✅ Domain methods добавлены:
    // - SetStatus(), RecordSuccess(), RecordFailure()
    // - AddTool(), RemoveTool(), UpdateConfig()
}

public class AgentSession
{
    // ✅ Domain methods добавлены:
    // - Start(), AddStep(), AdvanceStep()
    // - Complete(), Fail(), WaitForConfirmation()
    // - RecordToolCall(), RecordTokensUsed(), AddContext()
}

public class AgentStep
{
    // ✅ Domain methods добавлены:
    // - Approve(), Execute(), SetObservation()
}

public class AgentPlan
{
    // ✅ Domain methods добавлены:
    // - AddTask(), Approve(), SetEstimatedResources()
    // - EstimatedSteps property
}
```

**Статус:** ✅ Domain methods добавлены для workflow orchestration
**Осталось:** Hashline editing logic, Task decomposition

---

## 🔧 Что нужно создать

### 1. Domain Layer (Libr4.AI.Domain.IDEAIAgent)

```csharp
// Новые сущности:
AgentTask              // Celery task equivalent
AgentPlan              // Plan-first workflow
AgentPhase             // Phase execution
HashlineEdit           // Hashline code edits
AgentExecutionContext  // Execution context
SpecContract           // Contract validation
```

### 2. Application Layer (Libr4.AI.Application)

```csharp
// Commands:
CreateAgentTaskCommand         // POST /api/v1/ai/agents/tasks
GetAgentTaskStatusQuery        // GET /api/v1/ai/agents/tasks/{id}
CancelAgentTaskCommand         // DELETE /api/v1/ai/agents/tasks/{id}
ExecuteAgentPlanCommand        // POST /api/v1/ai/agents/execute

// Handlers:
AgentTaskHandler               // Обработка 8-hour tasks
PlanExecutionHandler          // Plan-first flow
HashlineEditHandler           // Hashline format edits
```

### 3. Infrastructure Layer (Libr4.AI.Infrastructure)

```csharp
// Services (20+ новых!):
IAgentCascadeService
IAgentIntelligenceRouter
IAgentWebSearchService
IShadowWorkspaceService
ISafeFileOperationsService
IAgentTaskRecordService
IAgentOrchestrationService
IAutonomousRuntimePolicyService
ITaskDecompositionService
IGitHubTemplateBootstrapService
IAIUsageLogger

// Hangfire Jobs:
AgentBackgroundJob            // Замена Celery
AgentPlanJob                  // Plan execution
AgentValidationJob            // Validation/repair
```

### 4. API Layer (Libr4.AI.Api)

```csharp
// Endpoints (10+ новых!):
POST   /api/v1/ai/agents/tasks              // Create task
GET    /api/v1/ai/agents/tasks/{id}         // Get status
DELETE /api/v1/ai/agents/tasks/{id}         // Cancel task
POST   /api/v1/ai/agents/execute            // Execute plan
POST   /api/v1/ai/agents/review            // Code review
POST   /api/v1/ai/agents/refactor          // Refactor
POST   /api/v1/ai/agents/test-gen          // Test generation
POST   /api/v1/ai/agents/chat               // Chat with agent
```

---

## 📊 Зависимости от других модулей

| Модуль | Зависимость | Критичность |
|--------|-------------|-------------|
| CodeEditor | Работа с файлами | 🔴 Высокая |
| IDEDebug | Debugging интеграция | 🟡 Средняя |
| IDEGit | Git operations | 🟡 Средняя |
| IDELSP | LSP для code analysis | 🟡 Средняя |
| LocalAI | LLM inference | 🔴 Высокая |
| Memory | Context memory | 🟡 Средняя |
| RAGSearch | Knowledge retrieval | 🟢 Низкая |

---

## ⚠️ Технические вызовы

### 1. Celery → Hangfire
```python
# Python (Celery)
@celery_app.task(bind=True, soft_time_limit=28800)
def process_agent_task(self, ...):
    # 8 hours task
```

```csharp
// C# (Hangfire)
[AutomaticRetry(Attempts = 3)]
[Queue("ai-agent")]
[JobDisplayName("Process Agent Task")]
public async Task ProcessAgentTaskAsync(...)
{
    // JobCancellationToken для отмены
    // 8 hours max
}
```

### 2. AsyncResult → Hangfire Monitoring
```python
# Python
ar = process_agent_task.AsyncResult(task_id)
st = ar.state
```

```csharp
// C#
var job = JobStorage.Current.GetMonitoringApi().JobDetails(jobId);
var state = job.History.Last().StateName;
```

### 3. ThreadPoolExecutor → Task.WhenAll
```python
# Python
_CELERY_AR_POOL = ThreadPoolExecutor(max_workers=8)
```

```csharp
// C#
var tasks = new List<Task>();
await Task.WhenAll(tasks);
```

---

## 📝 План портирования

### Этап 1: Infrastructure (Неделя 1)
- [ ] Создать Hangfire background jobs
- [ ] Портировать ShadowWorkspaceService
- [ ] Портировать SafeFileOperationsService
- [ ] Портировать AgentTaskRecordService

### Этап 2: Domain + Application (Неделя 2)
- [ ] Расширить AIAgent домен
- [ ] Создать AgentTask, AgentPlan сущности
- [ ] Реализовать Commands/Queries
- [ ] HashlineEdit логика

### Этап 3: Services (Неделя 3)
- [ ] IdeAgentCascadeService
- [ ] IdeAgentIntelligenceRouter
- [ ] IdeAgentWebSearchService
- [ ] AgentOrchestrationService
- [ ] TaskDecompositionService

### Этап 4: API + Testing (Неделя 4)
- [ ] Endpoints
- [ ] EF Migrations
- [ ] Integration tests
- [ ] Docker compose update

---

## 🎯 Acceptance Criteria

- [ ] Создание агент-таска через API
- [ ] 8-hour background execution
- [ ] Plan-first workflow работает
- [ ] Hashline code edits
- [ ] Real-time status monitoring
- [ ] Cancel task функционал
- [ ] Code review endpoint
- [ ] Refactor endpoint
- [ ] Test generation endpoint
- [ ] Integration с CodeEditor

---

## 📁 Создаваемые файлы

### Domain (3 файла)
- `Libr4.AI.Domain.IDEAIAgent/Entities/AgentTask.cs`
- `Libr4.AI.Domain.IDEAIAgent/Entities/AgentPlan.cs`
- `Libr4.AI.Domain.IDEAIAgent/ValueObjects/HashlineEdit.cs`

### Application (6 файлов)
- `Libr4.AI.Application/Agents/Commands/CreateAgentTaskCommand.cs`
- `Libr4.AI.Application/Agents/Commands/CancelAgentTaskCommand.cs`
- `Libr4.AI.Application/Agents/Queries/GetAgentTaskStatusQuery.cs`
- `Libr4.AI.Application/Agents/Services/IAgentTaskService.cs`
- `Libr4.AI.Application/Agents/Events/AgentTaskCompletedEvent.cs`

### Infrastructure (12 файлов)
- `Libr4.AI.Infrastructure/Agents/Services/AgentCascadeService.cs`
- `Libr4.AI.Infrastructure/Agents/Services/AgentIntelligenceRouter.cs`
- `Libr4.AI.Infrastructure/Agents/Services/ShadowWorkspaceService.cs`
- `Libr4.AI.Infrastructure/Agents/Services/SafeFileOperationsService.cs`
- `Libr4.AI.Infrastructure/Agents/Services/AgentTaskRecordService.cs`
- `Libr4.AI.Infrastructure/Agents/Services/AgentOrchestrationService.cs`
- `Libr4.AI.Infrastructure/Agents/Services/TaskDecompositionService.cs`
- `Libr4.AI.Infrastructure/Agents/Hangfire/AgentBackgroundJob.cs`
- `Libr4.AI.Infrastructure/Agents/Persistence/AgentTaskConfiguration.cs`

### API (2 файла)
- `Libr4.AI.Api/Endpoints/AgentEndpoints.cs` (10+ endpoints)
- `Libr4.AI.Api/Endpoints/AgentTaskEndpoints.cs`

**Итого: ~30 новых файлов**

---

## 📌 Примечания

1. **Celery vs Hangfire:** Celery в Python имеет встроенную очередь, результаты (AsyncResult), monitoring. Hangfire требует настройки SQL/Redis storage.

2. **8-hour tasks:** В Python soft_time_limit=28800 секунд. В C# Hangfire job timeout через `JobCancellationToken`.

3. **Shadow Workspace:** Критично для safe execution. Требует file system isolation (chroot/container).

4. **Web Search:** Требует интеграции с поисковыми API (Google, Bing, DuckDuckGo).

---

**Создано:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")  
**Статус:** 🟡 ГОТОВ К ПОРТИРОВАНИЮ
