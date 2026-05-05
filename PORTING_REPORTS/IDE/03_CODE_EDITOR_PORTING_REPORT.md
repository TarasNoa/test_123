# Отчёт о портировании: code_editor.py + code_editor_enhanced.py

## 📊 Общая информация

| Параметр | Значение |
|----------|----------|
| **Исходные файлы** | `code_editor.py` + `code_editor_enhanced.py` |
| **Общий размер** | 60.4 KB (1,819 строк) |
| **Язык оригинала** | Python 3.11 + FastAPI |
| **Целевой язык** | C# 12 + ASP.NET Core |
| **Сложность** | 🟡 **ВЫСОКАЯ** |
| **Оценка времени** | 1.5-2 недели |

---

## ✅ Что УЖЕ есть в C#

### Domain Models (скелет):
```csharp
// Libr4.AI.Domain.CodeEditor/EditorFile.cs
public class CodeProject
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public ProjectType ProjectType { get; set; }
    public CodeLanguage Language { get; set; }
    public List<ProjectCodeFile> Files { get; set; }
    public List<CodeProjectCollaborator> Collaborators { get; set; }
    
    public void AddCollaborator(Guid userId, CollaboratorRole role)
    // ТОЛЬКО этот метод!
}

public class ProjectCodeFile
{
    public Guid Id { get; set; }
    public string Path { get; set; }
    public string Content { get; set; }
    
    public void UpdateContent(string newContent, DateTimeOffset now)
    // ТОЛЬКО этот метод!
}
```

**Оценка:** 10% функционала

---

## ❌ Что НЕТ в C# (Python оригинал)

### 1. Docker Execution Engine
```python
# Python (code_editor.py)
from app.services.docker_execution_service import docker_execution_service

# Docker-isolated execution (safe sandbox)
result = await docker_execution_service.execute_code(
    project_id=project_id,
    code=code,
    language=language,
    timeout=30
)
```
**Статус:** ❌ Не портировано  
**C# альтернатива:** Docker.DotNet + container isolation

### 2. Personal AI Assistant per User
```python
# Каждый пользователь имеет ПЕРСОНАЛЬНОГО AI агента
personal_ai = await ai_assistant_service.get_or_create_personal_assistant(
    user_id=current_user.id,
    project_context=project
)
```
**Статус:** ❌ Не портировано

### 3. Collaborative Editing (Operational Transform)
```python
from app.services.collaborative_editing_service import collaborative_editing_service
# OT algorithm для real-time collaborative editing
```
**Статус:** ❌ Не портировано  
**C# альтернатива:** SignalR + OT library или Yjs server port

### 4. Real-time Preview
```python
# Live preview для web projects
preview_service.start_preview_server(project_id, port)
```
**Статус:** ❌ Не портировано

### 5. Terminal Access
```python
# WebSocket-based terminal
from app.services.terminal_service import terminal_service
# PTY (pseudo-terminal) через Docker
```
**Статус:** ❌ Частично (есть TerminalSession домен)

### 6. Dependency Installation
```python
# Auto-install dependencies
await dependency_service.install_requirements(project_id, requirements_txt)
# pip, npm, etc.
```
**Статус:** ❌ Не портировано

### 7. Code Quality Analysis (AST, ML)
```python
# AST analysis
ast_analysis = await code_intelligence_service.analyze_ast(file_content)

# ML-based quality prediction
quality_score = await ml_service.predict_code_quality(code)
```
**Статус:** ❌ Не портировано

### 8. Security Vulnerability Scanning
```python
from app.services.security_scanning_service import security_scanning_service
vulnerabilities = await security_scanning_service.scan_code(code)
```
**Статус:** ❌ Не портировано

### 9. Refactoring Suggestions
```python
suggestions = await refactoring_service.suggest_refactoring(code, cursor_position)
```
**Статус:** ❌ Не портировано

### 10. Bug Prediction
```python
bug_probability = await ml_service.predict_bugs(code)
```
**Статус:** ❌ Не портировано

### 11. Test Generation
```python
test_code = await test_generation_service.generate_tests(source_code)
```
**Статус:** ❌ Не портировано

### 12. Documentation Generation
```python
docs = await doc_generation_service.generate_documentation(code)
```
**Статус:** ❌ Не портировано

---

## 🔧 Что нужно создать

### 1. Application Layer

```csharp
// Commands:
CreateCodeProjectCommand           // POST /api/v1/editor/projects
CreateProjectCodeFileCommand       // POST /api/v1/editor/projects/{id}/files
UpdateProjectCodeFileCommand       // PUT /api/v1/editor/projects/{id}/files/{fileId}
DeleteProjectCodeFileCommand       // DELETE /api/v1/editor/projects/{id}/files/{fileId}
ExecuteCodeCommand                 // POST /api/v1/editor/projects/{id}/execute
GetCodeAISuggestionsCommand        // POST /api/v1/editor/projects/{id}/ai-suggestions
StartCollaborationCommand          // POST /api/v1/editor/projects/{id}/collaborate
StartPreviewCommand                // POST /api/v1/editor/projects/{id}/preview
InstallDependenciesCommand         // POST /api/v1/editor/projects/{id}/dependencies
AnalyzeCodeQualityCommand          // POST /api/v1/editor/projects/{id}/analyze
ScanSecurityCommand                // POST /api/v1/editor/projects/{id}/security-scan
GenerateRefactoringCommand         // POST /api/v1/editor/projects/{id}/refactor
GenerateTestsCommand               // POST /api/v1/editor/projects/{id}/generate-tests
GenerateDocsCommand                // POST /api/v1/editor/projects/{id}/generate-docs

// Queries:
GetCodeProjectQuery                // GET /api/v1/editor/projects/{id}
GetMyCodeProjectsQuery             // GET /api/v1/editor/projects
GetProjectCodeFileQuery            // GET /api/v1/editor/projects/{id}/files/{fileId}
GetProjectCollaboratorsQuery       // GET /api/v1/editor/projects/{id}/collaborators
```

### 2. Infrastructure Services

```csharp
// Docker Execution:
IDockerExecutionService            // Container-based code execution
ISandboxService                    // Isolated environment

// AI:
IPersonalAIAssistantService        // Per-user AI agent
IAISuggestionService               // Code suggestions

// Collaboration:
ICollaborativeEditingService       // OT algorithm
ICollaborationSessionService       // Session management

// Preview:
IPreviewServerService              // Live preview
IWebServerManager                  // Dynamic port allocation

// Terminal:
ITerminalService                   // WebSocket PTY
IDockerTerminalService             // Docker-based terminal

// Dependencies:
IDependencyInstallationService     // pip/npm/etc
IPackageManagerService             // Package management

// Analysis:
ICodeQualityAnalysisService        // Quality metrics
ISecurityScanningService           // Vulnerability scan
IRefactoringService                // Refactoring suggestions
IBugPredictionService              // ML bug prediction
ITestGenerationService             // Auto test generation
IDocumentationGenerationService    // Auto doc generation
```

### 3. SignalR Hubs

```csharp
// Real-time collaboration hub
public class CodeEditorHub : Hub
{
    public async Task JoinProject(string projectId)
    public async Task LeaveProject(string projectId)
    public async Task SendEdit(string projectId, string fileId, EditOperation operation)
    public async Task SendCursorPosition(string projectId, string fileId, Position position)
    public async Task SendSelection(string projectId, string fileId, Range range)
}
```

---

## 📊 Зависимости

| Модуль | Зависимость | Критичность |
|--------|-------------|-------------|
| IDEAIAgent | Personal AI assistant | 🔴 Высокая |
| CodeIntelligence | AST analysis | 🔴 Высокая |
| IDEDebug | Execution debugging | 🟡 Средняя |
| IDERunner | Code execution | 🔴 Высокая |
| Terminal | Terminal access | 🟡 Средняя |
| Docker | Containerization | 🔴 Высокая |

---

## 📝 План портирования

### Этап 1: CRUD + Docker (Неделя 1)
- [ ] Create/Update/Delete projects and files
- [ ] Docker execution service
- [ ] Sandbox environment

### Этап 2: AI Integration (Неделя 1-2)
- [ ] Personal AI assistant
- [ ] AI suggestions endpoint

### Этап 3: Real-time Features (Неделя 2)
- [ ] SignalR collaborative editing
- [ ] OT algorithm
- [ ] WebSocket terminal

### Этап 4: Analysis Features (Неделя 2)
- [ ] Code quality analysis
- [ ] Security scanning
- [ ] Refactoring suggestions
- [ ] Test generation

---

## 🎯 Acceptance Criteria

- [ ] Create multi-file project
- [ ] Add/remove files
- [ ] Edit files with auto-save
- [ ] Docker-isolated code execution
- [ ] Real-time collaboration (2+ users)
- [ ] AI code suggestions
- [ ] Live preview (for web)
- [ ] Terminal access via WebSocket
- [ ] Dependency installation
- [ ] Code quality analysis
- [ ] Security vulnerability scan
- [ ] Refactoring suggestions
- [ ] Bug prediction
- [ ] Test generation
- [ ] Documentation generation

---

**Статус:** 🟡 ГОТОВ К ПОРТИРОВАНИЮ
