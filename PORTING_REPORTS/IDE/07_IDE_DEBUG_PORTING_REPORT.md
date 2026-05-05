# Отчёт о портировании: ide_debug.py

## 📊 Общая информация

| Параметр | Значение |
|----------|----------|
| **Исходный файл** | `ide_debug.py` (16 KB, 516 строк) |
| **C# статус** | ⚠️ Domain только |

---

## ✅ Domain в C#

```csharp
// Libr4.AI.Domain.IDEDebug/Breakpoint.cs
public class Breakpoint
public class DebugSession
public class StackFrame
```

---

## ❌ Нет в C#

### Debug Adapter Protocol (DAP)
```python
# Python использует debugpy / pydevd
# DAP implementation for VSCode integration
```

### Debug Features
```python
# - Breakpoint management
# - Step over/into/out
# - Variable inspection
# - Watch expressions
# - Call stack navigation
# - Conditional breakpoints
```

---

## 🔧 C# Реализация

### Debug Adapter
```csharp
// Microsoft.VisualStudio.Shared.VSCodeDebugProtocol
public class DebugAdapter
{
    public async Task StartDebuggingAsync(string workspacePath)
    public async Task SetBreakpointAsync(string file, int line)
    public async Task StepOverAsync()
    public async Task GetVariablesAsync(int frameId)
}
```

### Language-specific Debuggers
```csharp
// C#: vsdbg (VS Code debugger)
// Python: debugpy
// Node: node-debug2
// Java: java-debug
```

---

**Статус:** 🟡 Нужна DAP реализация
