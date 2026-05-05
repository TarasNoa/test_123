# Отчёт: ide_runner.py

## Статус
- **Файл**: 16.2 KB, 422 строк
- **C#**: ⚠️ Domain только (RunConfig, RunResult)

## ❌ Нет в C#
- Code execution engine
- Docker sandbox
- Runtime management (Node, Python, .NET, Java)
- Resource limits (CPU, memory)
- Timeout handling
- Output streaming

## 🔧 Нужно
```csharp
// Docker.DotNet
public interface ICodeExecutionService
{
    Task<RunResult> ExecuteAsync(RunConfig config);
    Task StreamOutputAsync(Guid runId, IOutputStream stream);
}

// Runtime providers
INodeRuntime, IPythonRuntime, IDotNetRuntime, IJavaRuntime
```

## API
```
POST /api/v1/ai/runner/execute
GET  /api/v1/ai/runner/runs/{id}
GET  /api/v1/ai/runner/runs/{id}/output
```

**Статус**: 🟡 Нужен Docker execution
