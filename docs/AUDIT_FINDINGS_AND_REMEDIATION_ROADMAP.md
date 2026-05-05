# Libr4 Agent / MCP / Skills вЂ” РђСѓРґРёС‚ Рё РїР»Р°РЅ СѓСЃС‚СЂР°РЅРµРЅРёСЏ РґРµС„РµРєС‚РѕРІ

> **РљРѕРЅС‚РµРєСЃС‚**: РєРѕРјРїР»РµРєСЃРЅС‹Р№ Р°СѓРґРёС‚ СЃРµСЂРІРёСЃР° `Libr4.IDE.AutonomousAppGeneration` РѕС‚ 2026вЂ‘04вЂ‘27. Р¦РµР»СЊ вЂ” Р·Р°РґРѕРєСѓРјРµРЅС‚РёСЂРѕРІР°С‚СЊ **РІСЃРµ** РёР·РІРµСЃС‚РЅС‹Рµ Р±Р°РіРё, Р±РѕС‚Р»РЅРµРєРё, РЅРµРїРѕР»РЅС‹Рµ СЂРµР°Р»РёР·Р°С†РёРё Рё Р°СЂС…РёС‚РµРєС‚СѓСЂРЅС‹Рµ СЂРёСЃРєРё СЃ РїСЂРёРѕСЂРёС‚РµР·РёСЂРѕРІР°РЅРЅРѕР№ РґРѕСЂРѕР¶РЅРѕР№ РєР°СЂС‚РѕР№ СѓСЃС‚СЂР°РЅРµРЅРёСЏ. РљР°Р¶РґС‹Р№ РїСѓРЅРєС‚ СЃРѕРґРµСЂР¶РёС‚: С‚РѕС‡РЅСѓСЋ Р»РѕРєР°С†РёСЋ, СЃРёРјРїС‚РѕРј, РєРѕСЂРЅРµРІСѓСЋ РїСЂРёС‡РёРЅСѓ Рё СЃС‚СЂР°С‚РµРіРёСЋ С„РёРєСЃР°.

## 0. РЎРІРѕРґРєР°

| РЎР»РѕР№ | РљРѕР»РёС‡РµСЃС‚РІРѕ P0 | РљРѕР»РёС‡РµСЃС‚РІРѕ P1 | РљРѕР»РёС‡РµСЃС‚РІРѕ P2 | РЎРѕСЃС‚РѕСЏРЅРёРµ |
|------|---------------|---------------|---------------|-----------|
| Pipeline orchestrator | 3 | 4 | 2 | С‡Р°СЃС‚РёС‡РЅРѕ РёСЃРїСЂР°РІР»РµРЅРѕ |
| Quality gates / ReviewGate2 | 4 | 3 | 1 | С‚СЂРµР±СѓРµС‚ Roslyn-СЂРµС„Р°РєС‚РѕСЂРёРЅРіР° |
| LLM service / Code generation | 3 | 3 | 2 | hardcoded fallback РёР·Р±С‹С‚РѕС‡РµРЅ |
| Recovery cascade | 2 | 1 | 0 | thread-safety Рё decay |
| Persistence | 1 | 2 | 2 | РІ РїР°РјСЏС‚Рё; РєСЂРёС‚РёС‡РЅРѕ |
| Observability / Telemetry | 0 | 4 | 1 | РЅРµС‚ first-class РјРµС‚СЂРёРє |
| Memory consolidation | 1 | 1 | 1 | fire-and-forget |
| Tests / DX | 0 | 3 | 2 | РїРѕРєСЂС‹С‚РёРµ РЅРµРіР°С‚РёРІРЅС‹С… РїСѓС‚РµР№ СЃР»Р°Р±РѕРµ |

**РљРѕСЂРЅРµРІРѕР№ РёРЅСЃР°Р№С‚**: gates РѕС†РµРЅРёРІР°СЋС‚ **РЅР°Р»РёС‡РёРµ С‚РµРєСЃС‚РѕРІС‹С… РјР°СЂРєРµСЂРѕРІ**, Р° fallback-РёРЅР¶РµРєС‚РѕСЂ РєР»Р°РґС‘С‚ СЂРѕРІРЅРѕ СЌС‚Рё РјР°СЂРєРµСЂС‹ РІ README/SECURITY.md/OBSERVABILITY_BASELINE.md. Р’ СЂРµР·СѓР»СЊС‚Р°С‚Рµ pipeline РїСЂРѕС…РѕРґРёС‚ review2 РїСЂРё РЅРµРІР°Р»РёРґРЅРѕРј РєРѕРґРµ. РЎРµРјР°РЅС‚РёС‡РµСЃРєРёРµ РїСЂРѕРІРµСЂРєРё (Roslyn / tree-sitter) вЂ” РµРґРёРЅСЃС‚РІРµРЅРЅС‹Р№ РїСѓС‚СЊ Рє РЅР°СЃС‚РѕСЏС‰РµРјСѓ РєР°С‡РµСЃС‚РІСѓ.

---

## 1. P0 вЂ” РєСЂРёС‚РёС‡РµСЃРєРёРµ РґРµС„РµРєС‚С‹ (production blockers)

### P0-1. `Console.WriteLine` РІ production-РєРѕРґРµ РїР»Р°РЅРёСЂРѕРІС‰РёРєР°
- **Р¤Р°Р№Р»**: `libr4/src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/AutonomousAppGeneration/Infrastructure/LlmAppPlannerService.cs`
- **Р›РёРЅРёРё**: 131, 151, 195, 201
- **РЎРёРјРїС‚РѕРј**: stdout-Р»РѕРіРё С‚РµСЂСЏСЋС‚СЃСЏ РІ structured logging, Р»РѕРјР°СЋС‚ CI Рё С‚РµСЃС‚С‹, РѕР±С…РѕРґСЏС‚ С„РёР»СЊС‚СЂС‹/РјР°СЂС€СЂСѓС‚РёР·Р°С†РёСЋ.
- **РљРѕСЂРµРЅСЊ**: РѕС‚Р»Р°РґРѕС‡РЅС‹Рµ `Console.WriteLine` РѕСЃС‚Р°РІР»РµРЅС‹ РїРѕСЃР»Рµ РїРµСЂРІРѕРЅР°С‡Р°Р»СЊРЅРѕР№ РёРЅС‚РµРіСЂР°С†РёРё СЃ РїСЂРѕРІР°Р№РґРµСЂРѕРј.
- **Р¤РёРєСЃ**: СѓРґР°Р»РёС‚СЊ (РёР»Рё РїРµСЂРµРІРµСЃС‚Рё РІ `_logger.LogDebug`).
- **РЎС‚Р°С‚СѓСЃ**: вњ… РёСЃРїСЂР°РІР»РµРЅРѕ РІ СЌС‚РѕРј РєРѕРјРјРёС‚Рµ.

### P0-2. Placeholder-С‚РµСЃС‚С‹ `Assert.True(true)` РІ .NET fallback
- **Р¤Р°Р№Р»**: `libr4/src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/AutonomousAppGeneration/Infrastructure/LlmCodeGenerationService.cs`
- **Р›РёРЅРёРё**: 2057-2058 (Python), 2082-2084 (.NET)
- **РЎРёРјРїС‚РѕРј**: `MinimalFallbackProject` РіР°СЂР°РЅС‚РёСЂРѕРІР°РЅРЅРѕ РїСЂРѕРІР°Р»РёРІР°РµС‚ `test_quality_floor` РІ ReviewGate2, РєРѕС‚РѕСЂС‹Р№ СЃР°Рј Р±Р»РѕРєРёСЂСѓРµС‚ `assert True`/`assert 1==1` (СЃРј. `ReviewGate2Service.cs:171-175`). РўРѕ РµСЃС‚СЊ fallback в‡’ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРёР№ review2 fail.
- **Р¤РёРєСЃ**: Р·Р°РјРµРЅРёС‚СЊ РЅР° РјРёРЅРёРјР°Р»СЊРЅРѕ-РїРѕР»РµР·РЅС‹Рµ С‚РµСЃС‚С‹ (РёРјРїРѕСЂС‚ + Р·Р°РїСѓСЃРє, РїСЂРѕРІРµСЂРєР° startup), Р±РµР· `assert True`.
- **РЎС‚Р°С‚СѓСЃ**: вњ… РёСЃРїСЂР°РІР»РµРЅРѕ РІ СЌС‚РѕРј РєРѕРјРјРёС‚Рµ.

### P0-3. Р“РѕРЅРєР° РІ `RecoveryCascadeService` (Singleton + `Dictionary`)
- **Р¤Р°Р№Р»**: `libr4/src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/AutonomousAppGeneration/Recovery/RecoveryCascadeService.cs`
- **Р›РёРЅРёРё**: 17-18 (`_strategyFailureCounts`, `_recoveryCache`)
- **РЎРёРјРїС‚РѕРј**: РїСЂРё РїР°СЂР°Р»Р»РµР»СЊРЅС‹С… runs (Р° СЃРµСЂРІРёСЃ singleton) вЂ” `InvalidOperationException` РёР»Рё РїРѕС‚РµСЂСЏ РґР°РЅРЅС‹С… РІ `Dictionary`.
- **Р¤РёРєСЃ**: `ConcurrentDictionary` + `Interlocked` РґР»СЏ СЃС‡С‘С‚С‡РёРєРѕРІ; РґРѕР±Р°РІРёС‚СЊ decay (decrement РєР°Р¶РґС‹Рµ N РјРёРЅСѓС‚).
- **РЎС‚Р°С‚СѓСЃ**: вњ… thread-safety РёСЃРїСЂР°РІР»РµРЅРѕ; decay РґРѕР±Р°РІР»РµРЅ РІ P1-Р·Р°РґР°С‡Сѓ.

### P0-4. CTS lifetime: `using var linkedCts` СѓРЅРёС‡С‚РѕР¶Р°РµС‚СЃСЏ СЂР°РЅСЊС€Рµ РёСЃРїРѕР»СЊР·РѕРІР°РЅРёСЏ
- **Р¤Р°Р№Р»**: `libr4/src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/AutonomousAppGeneration/Handlers/StartAppGenerationCommandHandler.cs`
- **Р›РёРЅРёРё**: 223-225
- **РЎРёРјРїС‚РѕРј**: `_runControl.RegisterRun(orchestrator.Id, linkedCts)` СЃРѕС…СЂР°РЅСЏРµС‚ СЃСЃС‹Р»РєСѓ, РЅРѕ `using var` РґРёСЃРїРѕР·РёС‚ CTS РЅР° РІС‹С…РѕРґРµ РёР· `try`. Р›СЋР±РѕР№ РїРѕСЃР»РµРґСѓСЋС‰РёР№ `_runControl.CancelRun(...)` Р±СЂРѕСЃРёС‚ `ObjectDisposedException`.
- **Р¤РёРєСЃ**: СЏРІРЅРѕ СЃРѕР·РґР°С‚СЊ `linkedCts` Р±РµР· `using`; РґРёСЃРїРѕР·РёС‚СЊ РІСЂСѓС‡РЅСѓСЋ РІ `finally` РїРѕСЃР»Рµ `_runControl.CompleteRun(...)`.
- **РЎС‚Р°С‚СѓСЃ**: вњ… РёСЃРїСЂР°РІР»РµРЅРѕ.

### P0-5. РЈС‚РµС‡РєР° РїР°РјСЏС‚Рё: `InMemoryAppGenerationRepository` Р±РµР· eviction
- **Р¤Р°Р№Р»**: `libr4/src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/AutonomousAppGeneration/Infrastructure/InMemoryAppGenerationRepository.cs`
- **РЎРёРјРїС‚РѕРј**: РєР°Р¶РґС‹Р№ run С…СЂР°РЅРёС‚ РІРµСЃСЊ Files/QualityGates/Iterations РЅР°РІСЃРµРіРґР°. РќР° ~100 runs вЂ” СЃРѕС‚РЅРё РњР‘; С‡РµСЂРµР· СЃСѓС‚РєРё вЂ” OOM.
- **Р¤РёРєСЃ РєСЂР°С‚РєРѕСЃСЂРѕС‡РЅРѕ**: LRU c capacity 256 + TTL 24h. **Р”РѕР»РіРѕСЃСЂРѕС‡РЅРѕ** (P2-1): EF Core + PostgreSQL.
- **РЎС‚Р°С‚СѓСЃ**: вњ… LRU+TTL РґРѕР±Р°РІР»РµРЅ (capacity РєРѕРЅС„РёРіСѓСЂРёСЂСѓРµС‚СЃСЏ С‡РµСЂРµР· `InMemoryRepositoryOptions`).

### P0-6. Build gate non-blocking в‡’ РЅРµРІР°Р»РёРґРЅС‹Р№ РєРѕРґ РїСЂРѕС…РѕРґРёС‚ pipeline
- **Р¤Р°Р№Р»**: `libr4/src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/AutonomousAppGeneration/Handlers/StartAppGenerationCommandHandler.cs`
- **Р›РёРЅРёРё**: 655-663
- **РЎРёРјРїС‚РѕРј**: РєРѕРјРјРµРЅС‚Р°СЂРёР№ "non-blocking for safety-net verification" + `LogWarning` + `continue` РѕР·РЅР°С‡Р°РµС‚ С‡С‚Рѕ provals build'Р° РёРіРЅРѕСЂРёСЂСѓСЋС‚СЃСЏ. Р­С‚Рѕ РѕР±СЉСЏСЃРЅСЏРµС‚ С„Р°РЅС‚РѕРјРЅС‹Рµ "СѓСЃРїРµС€РЅС‹Рµ" runs СЃ РїРѕР»РѕРјР°РЅРЅС‹Рј РєРѕРґРѕРј.
- **Р¤РёРєСЃ**: РґРѕР±Р°РІРёС‚СЊ РѕРїС†РёСЋ `AutonomousQualityGateOptions.BuildGateBlockingMode` (`StrictPerPhase` / `WarnOnly`); РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ `StrictPerPhase`. WarnOnly РѕСЃС‚Р°С‘С‚СЃСЏ РґР»СЏ РѕС‚Р»Р°РґРѕС‡РЅРѕРіРѕ СЂРµР¶РёРјР° safety-net.
- **РЎС‚Р°С‚СѓСЃ**: вњ… РѕРїС†РёСЏ РґРѕР±Р°РІР»РµРЅР°; РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ blocking.

### P0-7. Silent JSON parse failures РјР°СЃРєРёСЂСѓСЋС‚ LLM-Р±Р°РіРё
- **Р¤Р°Р№Р»**: `libr4/src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/AutonomousAppGeneration/Infrastructure/LlmJsonHelpers.cs`
- **Р›РёРЅРёРё**: 154-155
- **РЎРёРјРїС‚РѕРј**: `catch { doc = null; return false; }` вЂ” РЅРµРІРѕР·РјРѕР¶РЅРѕ РґРёР°РіРЅРѕСЃС‚РёСЂРѕРІР°С‚СЊ РїРѕС‡РµРјСѓ РјРѕРґРµР»СЊ РІРµСЂРЅСѓР»Р° РЅРµРІР°Р»РёРґРЅС‹Р№ JSON.
- **Р¤РёРєСЃ**: log РЅР° `Trace`/`Debug` СЃ РїРµСЂРІС‹РјРё 200 Р±Р°Р№С‚Р°РјРё raw + exception type.
- **РЎС‚Р°С‚СѓСЃ**: вњ… РёСЃРїСЂР°РІР»РµРЅРѕ С‡РµСЂРµР· optional `ILogger`.

### P0-8. NRE-СЂРёСЃРєРё РІ `BuildDeterministicFallbackFixes`
- **Р¤Р°Р№Р»**: `libr4/src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/AutonomousAppGeneration/Infrastructure/LlmCodeGenerationService.cs`
- **Р›РёРЅРёРё**: 587-598
- **РЎРёРјРїС‚РѕРј**: `testFile.Content.Contains(...)` Р±РµР· null-check; РїР°РґР°РµС‚ РµСЃР»Рё scaffolder РїРѕР»РѕР¶РёР» РїСѓСЃС‚РѕР№ `Content`.
- **Р¤РёРєСЃ**: `(testFile.Content ?? string.Empty).Contains(...)`.
- **РЎС‚Р°С‚СѓСЃ**: вњ… РёСЃРїСЂР°РІР»РµРЅРѕ.

### P0-9. Р”СѓР±Р»РёСЂРѕРІР°РЅРёРµ `IsAspNetCorePlan` / `IsPythonPlan` / `IsNodePlan` РІ 5 С„Р°Р№Р»Р°С…
- **Р¤Р°Р№Р»С‹**:
  - `Infrastructure/GenerationStackSafetyNet.cs`
  - `Infrastructure/LlmCodeGenerationService.cs:842-870`
  - `Services/AutonomousQualityGateService.cs:473-500`
  - `Services/AutonomousCodeConsistencyValidator.cs:90-147`
- **РЎРёРјРїС‚РѕРј**: СЂР°СЃСЃРёРЅС…СЂРѕРЅ РїСЂР°РІРёР». РќР°РїСЂРёРјРµСЂ, РІ `Consistency` Python РїСЂРѕРІРµСЂСЏРµС‚СЃСЏ РїРѕ `language`, РІ `CodeGen` вЂ” РїРѕ `runtimeImage`. Р Р°СЃС€РёСЂРµРЅРёРµ РЅРѕРІРѕРіРѕ СЃС‚РµРєР° С‚СЂРµР±СѓРµС‚ РїСЂР°РІРёС‚СЊ 5 С„Р°Р№Р»РѕРІ.
- **Р¤РёРєСЃ РєСЂР°С‚РєРѕСЃСЂРѕС‡РЅРѕ**: РІРІРµРґС‘РЅ `Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackPlanHeuristics` вЂ” **single source of truth**. РЎСѓС‰РµСЃС‚РІСѓСЋС‰РёРµ 5 РєРѕРїРёР№ СЃРѕС…СЂР°РЅРµРЅС‹ РєР°Рє РґРµР»РµРіР°С‚С‹ Рє РЅРµРјСѓ (РјСЏРіРєР°СЏ РјРёРіСЂР°С†РёСЏ).
- **Р”РѕР»РіРѕСЃСЂРѕС‡РЅРѕ** (P1-9): РїРѕР»РЅР°СЏ Р·Р°РјРµРЅР° РЅР° `IStackStrategy` С‡РµСЂРµР· DI СЃ СЂРµР°Р»РёР·Р°С†РёСЏРјРё `DotNetStackStrategy`, `PythonStackStrategy`, `NodeStackStrategy`, `GoStackStrategy`, `RustStackStrategy`. РЎРј. В§ 6.3.
- **РЎС‚Р°С‚СѓСЃ**: вљ пёЏ С‡Р°СЃС‚РёС‡РЅРѕ вЂ” `StackPlanHeuristics` РґРѕР±Р°РІР»РµРЅ; РјРёРіСЂР°С†РёСЏ РІС‹Р·РѕРІРѕРІ вЂ” РѕС‚РґРµР»СЊРЅР°СЏ Р·Р°РґР°С‡Р° (P1).

---

## 2. P1 вЂ” РІС‹СЃРѕРєРёР№ РїСЂРёРѕСЂРёС‚РµС‚ (1вЂ“2 РЅРµРґРµР»Рё СЂР°Р±РѕС‚С‹)

### P1-1. ReviewGate2: substring-РїСЂРѕРІРµСЂРєРё РґР°РІР°СЋС‚ false positives
- **Р¤Р°Р№Р»**: `libr4/src/Services/IDE/Libr4.IDE.AutonomousAppGeneration/AutonomousAppGeneration/Services/ReviewGate2Service.cs`
- **РЎРёРјРїС‚РѕРјС‹**:
  - `error_handling` (line 359-368): РїСЂРѕР№РґС‘С‚ РѕС‚ Р»СЋР±РѕРіРѕ `"error"` РІ С‚РµРєСЃС‚Рµ.
  - `observability_baseline` (line 211-237): РґРѕСЃС‚Р°С‚РѕС‡РЅРѕ РїРѕРґСЃС‚СЂРѕРє `logger`, `json`, `x-request-id` РіРґРµ СѓРіРѕРґРЅРѕ.
  - `semantic_security` (line 550-551): Р»РёС‚РµСЂР°Р»С‹ `jwt` РёР»Рё `encryption` РІ README СѓРґРѕРІР»РµС‚РІРѕСЂСЏСЋС‚.
  - `auth_implementation` (`AutonomousQualityGateService.cs:399-413`): СѓРїРѕРјРёРЅР°РЅРёРµ `OAuth` РІ РєРѕРјРјРµРЅС‚Р°СЂРёРё Р·Р°СЃС‡РёС‚С‹РІР°РµС‚СЃСЏ.
- **РљРѕСЂРµРЅСЊ**: gates СЃРѕР·РґР°РЅС‹ РєР°Рє text-pattern matching, Р±РµР· AST.
- **РЎС‚СЂР°С‚РµРіРёСЏ С„РёРєСЃР°**:
  - **C# stack**: РёСЃРїРѕР»СЊР·РѕРІР°С‚СЊ **Roslyn** (`Microsoft.CodeAnalysis.CSharp`) вЂ” РїРѕСЃС‚СЂРѕРёС‚СЊ syntax tree, РёСЃРєР°С‚СЊ `[Authorize]` Р°С‚СЂРёР±СѓС‚С‹ РЅР° РјРµС‚РѕРґР°С…, `services.AddAuthentication(...)`, `app.UseAuthentication()`.
  - **Python stack**: РёСЃРїРѕР»СЊР·РѕРІР°С‚СЊ **tree-sitter** (binding `tree-sitter-python` С‡РµСЂРµР· P/Invoke РёР»Рё REST Рє PythonвЂ‘РјРёРєСЂРѕСЃРµСЂРІРёСЃСѓ) вЂ” РёСЃРєР°С‚СЊ `from fastapi.security import OAuth2PasswordBearer`, `@router.get(..., dependencies=[Depends(...)])`.
  - **Node stack**: tree-sitter-javascript / TypeScript compiler API С‡РµСЂРµР· Node-sidecar.
- **РђСЂС…РёС‚РµРєС‚СѓСЂР°**:
```csharp
public interface IArchitectureCheckRule {
    string CheckId { get; }
    Task<ArchitectureCheckResult> EvaluateAsync(IReadOnlyList<GeneratedFile> files, GenerationPlan plan, CancellationToken ct);
}

// Р РµРіРёСЃС‚СЂР°С†РёСЏ per-stack:
services.AddSingleton<IArchitectureCheckRule, AuthImplementationRule_DotNet>();
services.AddSingleton<IArchitectureCheckRule, AuthImplementationRule_Python>();
```
- **РЎС‚Р°С‚СѓСЃ**: вќЊ РЅРµ СЃРґРµР»Р°РЅРѕ (С‚СЂРµР±СѓРµС‚ РїРѕРґРєР»СЋС‡РµРЅРёСЏ tree-sitter Рё Roslyn-Р°РЅР°Р»РёР·Р°; РїР»Р°РЅ РІ В§ 6.1).

### P1-2. РҐСЂСѓРїРєР°СЏ СЂРµС‚СЂР°Р№-РєР»Р°СЃСЃРёС„РёРєР°С†РёСЏ
- **Р¤Р°Р№Р»**: `StartAppGenerationCommandHandler.cs:1180-1224`
- **РЎРёРјРїС‚РѕРј**: substring-СЌРІСЂРёСЃС‚РёРєРё `IsRetryableExecutionFailure`, `IsNonActionableInfrastructureFailure`. РџРѕР»СЊР·РѕРІР°С‚РµР»СЊСЃРєРёРµ Р»РѕРіРё РїСЂРёР»РѕР¶РµРЅРёСЏ СЃ СЃР»РѕРІРѕРј `"timeout"` РёРЅС‚РµСЂРїСЂРµС‚РёСЂСѓСЋС‚СЃСЏ РєР°Рє infra-failure. Р РµР°Р»СЊРЅС‹Р№ pip-failure СЃ РЅРµСЃС‚Р°РЅРґР°СЂС‚РЅС‹Рј СЃРѕРѕР±С‰РµРЅРёРµРј вЂ” РїСЂРѕР№РґС‘С‚ РєР°Рє deterministic build error.
- **Р¤РёРєСЃ**: РІРІРµСЃС‚Рё `IExecutionFailureClassifier` СЃ РїСЂР°РІРёР»Р°РјРё:
  - HTTP РєРѕРґС‹ (429/503/504 в‡’ retryable).
  - Exit codes (137 OOM в‡’ infra; 1/2 в‡’ deterministic).
  - РЎРёРіРЅР°С‚СѓСЂС‹ С‡РµСЂРµР· regex СЃ named groups + tracked test corpus.
- **РЎС‚Р°С‚СѓСЃ**: вќЊ РЅРµ СЃРґРµР»Р°РЅРѕ.

### P1-3. Р”РµРєРѕРјРїРѕР·РёСЂРѕРІР°С‚СЊ `StartAppGenerationCommandHandler.Handle` (~730 СЃС‚СЂРѕРє)
- **Р¤Р°Р№Р»**: `Handlers/StartAppGenerationCommandHandler.cs`
- **РЎРёРјРїС‚РѕРј**: РјРѕРЅРѕР»РёС‚РЅС‹Р№ РјРµС‚РѕРґ; 6 С‚РѕС‡РµРє РІС‹С…РѕРґР° СЃ РґСѓР±Р»РёСЂСѓСЋС‰РёРјСЃСЏ `AppGenerationResponse`-РєРѕРЅСЃС‚СЂСѓРєС‚РѕСЂРѕРј; SRP РЅР°СЂСѓС€РµРЅ; С‚РµСЃС‚РёСЂРѕРІР°С‚СЊ С†РµР»РёРєРѕРј РЅРµРІРѕР·РјРѕР¶РЅРѕ.
- **Р¤РёРєСЃ**: pipeline pattern:
```csharp
public interface IGenerationStage {
    string Name { get; }
    Task<StageOutcome> ExecuteAsync(GenerationContext ctx, CancellationToken ct);
}

// Р РµРіРёСЃС‚СЂР°С†РёСЏ:
services.AddSingleton<IGenerationStage, PlanStage>();
services.AddSingleton<IGenerationStage, GenerateStage>();
services.AddSingleton<IGenerationStage, ConsistencyStage>();
services.AddSingleton<IGenerationStage, BuildStage>();
services.AddSingleton<IGenerationStage, IterationLoopStage>();
services.AddSingleton<IGenerationStage, FinalizationStage>();

// Handler СЃРІРѕРґРёС‚СЃСЏ Рє:
foreach (var stage in _stages) {
    var outcome = await stage.ExecuteAsync(ctx, ct);
    if (outcome.IsFailure) return BuildResponse(ctx, outcome);
}
```
- **РЎС‚Р°С‚СѓСЃ**: вќЊ РЅРµ СЃРґРµР»Р°РЅРѕ (Р±РѕР»СЊС€РѕР№ СЂРµС„Р°РєС‚РѕСЂРёРЅРі; ~3-5 РґРЅРµР№ + СЂРµРіСЂРµСЃСЃ-С‚РµСЃС‚С‹).

### P1-4. OpenTelemetry first-class
- **РЎРёРјРїС‚РѕРј**: СЂР°Р·СЂРѕР·РЅРµРЅРЅС‹Рµ `_logger.LogInformation` Р±РµР· Р°РіСЂРµРіР°С†РёРё. РќРµС‚ SLI/SLO.
- **Р¤РёРєСЃ**:
```csharp
// РњРµС‚СЂРёРєРё
services.AddOpenTelemetry().WithMetrics(m => m
    .AddMeter("Libr4.AutoGen")
    .AddPrometheusExporter());

var meter = new Meter("Libr4.AutoGen", "1.0");
public static readonly Counter<long> RunsStarted = meter.CreateCounter<long>("autogen.runs.started");
public static readonly Histogram<double> GateScore = meter.CreateHistogram<double>("autogen.gate.score");
public static readonly Histogram<int> Iterations = meter.CreateHistogram<int>("autogen.iterations");
public static readonly Counter<long> FallbackUsed = meter.CreateCounter<long>("autogen.fallback.used");

// Trace
services.AddOpenTelemetry().WithTracing(t => t
    .AddSource("Libr4.AutoGen")
    .AddOtlpExporter());
```
- **Р”Р°С€Р±РѕСЂРґС‹**: gate pass rate per stage, fallback hit rate, mean review2 score, P95 LLM latency, cost per run.
- **РЎС‚Р°С‚СѓСЃ**: вќЊ РЅРµ СЃРґРµР»Р°РЅРѕ.

### P1-5. LLM provider: circuit breaker + cost cap
- **РЎРёРјРїС‚РѕРј**: `_ai.GenerateCompletionAsync` Р±РµР· rate-limit/cost-limit. РћРґРёРЅ СЃР»РѕРјР°РЅРЅС‹Р№ РїСЂРѕРІР°Р№РґРµСЂ в‡’ РІСЃРµ РєР»РёРµРЅС‚С‹ down.
- **Р¤РёРєСЃ**:
  - `Polly.CircuitBreakerPolicy` per ProviderId.
  - `IBudgetService.TryConsume(runId, tokens, costUsd)` вЂ” РѕС‚Р±СЂР°СЃС‹РІР°С‚СЊ Р·Р°РїСЂРѕСЃ РїСЂРё РїСЂРµРІС‹С€РµРЅРёРё budget.
- **РЎС‚Р°С‚СѓСЃ**: вќЊ РЅРµ СЃРґРµР»Р°РЅРѕ.

### P1-6. Memory consolidation РєР°Рє `BackgroundService`
- **Р¤Р°Р№Р»**: `Handlers/StartAppGenerationCommandHandler.cs:997-1008`
- **РЎРёРјРїС‚РѕРј**: `_ = Task.Run(...)` Р±РµР· backpressure. РџСЂРё burst вЂ” РЅРµРѕРіСЂР°РЅРёС‡РµРЅРЅР°СЏ concurrency LLM, OOM-СЂРёСЃРє.
- **Р¤РёРєСЃ**: `Channel<Guid>` (single-reader bounded queue) + `BackgroundService` consumer. РњРµС‚СЂРёРєР° `autogen.consolidation.queue_depth`.
- **РЎС‚Р°С‚СѓСЃ**: вќЊ РЅРµ СЃРґРµР»Р°РЅРѕ.

### P1-7. Property-based С‚РµСЃС‚С‹ РЅР° РёРЅРІР°СЂРёР°РЅС‚С‹ pipeline
- **РЎРёРјРїС‚РѕРј**: `dotnet test --filter` 50+ С„Р°Р№Р»РѕРІ, РЅРѕ РїРѕС‡С‚Рё РІСЃС‘ happy-path. Negative paths (cancel mid-fix, race РЅР° CTS, NRE РЅР° null Content) РЅРµ РїРѕРєСЂС‹С‚С‹.
- **Р¤РёРєСЃ**: FsCheck РёР»Рё Hedgehog (РјРѕР¶РЅРѕ РЅР° F# РґР»СЏ compactРЅРѕСЃС‚Рё). РРЅРІР°СЂРёР°РЅС‚С‹:
  - `в€Ђ plan, в€Ђ files: ReviewGate2.EvaluateComprehensive РЅРµ Р±СЂРѕСЃР°РµС‚ РёСЃРєР»СЋС‡РµРЅРёРµ`.
  - `в€Ђ fingerprint: SaveAsync; FindLatestByFingerprintAsync в‡’ РІРѕР·РІСЂР°С‰Р°РµС‚ С‚РѕС‚ Р¶Рµ runId`.
  - `Cancel during stage X в‡’ orchestrator.Status == Failed && FailureReason.Contains("cancelled")`.
- **РЎС‚Р°С‚СѓСЃ**: вќЊ РЅРµ СЃРґРµР»Р°РЅРѕ (РјРѕР¶РЅРѕ СЂРµР°Р»РёР·РѕРІР°С‚СЊ РєР°Рє F# РїСЂРѕРµРєС‚ `Libr4.IDE.AutonomousAppGeneration.PropertyTests`).

### P1-8. Templates РІРјРµСЃС‚Рѕ hardcoded fallback strings
- **Р¤Р°Р№Р»**: `LlmCodeGenerationService.cs` (~1200 СЃС‚СЂРѕРє СЌС‚Рѕ `BuildFallback*Content()` РјРµС‚РѕРґС‹)
- **РЎРёРјРїС‚РѕРј**: СЂР°СЃС€РёСЂРµРЅРёРµ РЅРѕРІРѕРіРѕ СЃС‚РµРєР° вЂ” РєРѕРїРёРїР°СЃС‚ 5+ РјРµС‚РѕРґРѕРІ; РґСѓР±Р»РёСЂРѕРІР°РЅРёРµ README/CI/Docker.
- **Р¤РёРєСЃ**: embedded resources `*.template.{cs,py,js,yml}` + Scriban РґР»СЏ РїР°СЂР°РјРµС‚СЂРёР·Р°С†РёРё (`{{ AppName }}`, `{{ Port }}`, `{{ Database }}`).
- **РЎС‚Р°С‚СѓСЃ**: вќЊ РЅРµ СЃРґРµР»Р°РЅРѕ.

### P1-9. `IStackStrategy` С‡РµСЂРµР· DI (РїРѕР»РЅР°СЏ РјРёРіСЂР°С†РёСЏ)
- РЎРј. В§ 6.3. РџРѕСЃР»Рµ РґРѕР±Р°РІР»РµРЅРёСЏ `StackPlanHeuristics` (P0-9) вЂ” СЃР»РµРґСѓСЋС‰РёР№ С€Р°Рі вЂ” РІС‹РЅРµСЃС‚Рё stack-specific Р»РѕРіРёРєСѓ (validation rules, fallback artefact builders, manifest enforcement) РІ strategy-РєР»Р°СЃСЃС‹.
- **РЎС‚Р°С‚СѓСЃ**: вќЊ РЅРµ СЃРґРµР»Р°РЅРѕ (~5-7 РґРЅРµР№).

### P1-10. Plan-level error self-healing
- РР· `ENHANCED_GENERATION_TEST_RESULTS.md`: 8/8 РёС‚РµСЂР°С†РёР№ РїСЂРѕРІР°Р»РёР»РёСЃСЊ СЃ "Syntax error: Unterminated quoted string" РІ build command. Fixer РЅРµ Р»РµС‡РёС‚ РїР»Р°РЅ.
- **Р¤РёРєСЃ**: `IPlanValidator` С€Р°Рі РґРѕ stage 2:
  - РџР°СЂСЃРёС‚СЊ `BuildCommands`/`TestCommands` С‡РµСЂРµР· shell-parser (Mono.Posix РёР»Рё СЃРїРµС†РёР°Р»РёР·РёСЂРѕРІР°РЅРЅС‹Р№ grammar).
  - Dry-run `dotnet --info` / `python --version` С‡РµСЂРµР· runtime.
  - РџСЂРё РЅРµРІР°Р»РёРґРЅРѕР№ РєРѕРјР°РЅРґРµ вЂ” `ReplanWithFixedCommands` РёР»Рё fallback РЅР° known-good per stack.
- **РЎС‚Р°С‚СѓСЃ**: вќЊ РЅРµ СЃРґРµР»Р°РЅРѕ.

---

## 3. P2 вЂ” СЃС‚СЂР°С‚РµРіРёС‡РµСЃРєРёРµ СѓР»СѓС‡С€РµРЅРёСЏ (3+ РЅРµРґРµР»СЊ)

### P2-1. Persistence: EF Core + PostgreSQL
- РўР°Р±Р»РёС†С‹: `Runs`, `Files`, `QualityGates`, `Iterations`, `Checkpoints`, `Memory`, `KnowledgeGraph`, `RecoveryCache`.
- Index РїРѕ `Fingerprint`, `Status`, `UpdatedAt`, `Tenant`.
- РњРёРіСЂР°С†РёРё С‡РµСЂРµР· `dotnet ef migrations add`.

### P2-2. Р’РµРєС‚РѕСЂРЅРѕРµ С…СЂР°РЅРёР»РёС‰Рµ РїР°РјСЏС‚Рё (pgvector / Qdrant)
- `IMemoryStore` в‡’ `EmbeddingMemoryStore` СЃ СЃРµРјР°РЅС‚РёС‡РµСЃРєРёРј retrieval.
- Embeddings РѕС‚ С‚РѕРіРѕ Р¶Рµ РїСЂРѕРІР°Р№РґРµСЂР° С‡С‚Рѕ РїР»Р°РЅРёСЂРѕРІС‰РёРє (OpenRouter `text-embedding-3-small`).

### P2-3. Multi-region / multi-tenant Р°СЂС…РёС‚РµРєС‚СѓСЂР°
- Run partitioning by tenantId.
- Federated runs (cross-region recovery).

### P2-4. Plan-level UI РґР»СЏ review/edit checkpoints
- Frontend (`libr4/frontend`): UI РґР»СЏ РїСЂРѕСЃРјРѕС‚СЂР° diff РєР°Р¶РґРѕР№ РёС‚РµСЂР°С†РёРё, manual approve/reject.

### P2-5. РџРѕР»РЅС‹Р№ LLM cost budget service (`IBudgetService`)
- Per-run / per-tenant / per-day cap.
- Estimate РїРµСЂРµРґ stage; abort РїСЂРё РїСЂРµРІС‹С€РµРЅРёРё.

### P2-6. Rust-СЃР°Р№РґРєР°СЂ РґР»СЏ CPU-bound СЃРµРјР°РЅС‚РёС‡РµСЃРєРѕРіРѕ Р°РЅР°Р»РёР·Р°
- Р”РµС‚РµРєС‚ placeholder-РєРѕРґР°, complexity-РјРµС‚СЂРёРєРё, cyclomatic complexity, test quality scoring вЂ” С‡РµСЂРµР· Rust + tree-sitter (Р±С‹СЃС‚СЂРµРµ С‡РµРј .NET).
- IPC С‡РµСЂРµР· named pipes / gRPC.

### P2-7. F# РґР»СЏ domain-rules engine
- ReviewGate2 РїСЂР°РІРёР»Р° РІС‹СЂР°Р·РёС‚СЊ РєР°Рє F# DU `Rule = StackSpecific of ... | Cross of ...`. Pattern matching РїРѕ СЃС‚СЂСѓРєС‚СѓСЂРµ stack РґР°С‘С‚ РєРѕРјРїРёР»РёСЂСѓРµРјСѓСЋ РіР°СЂР°РЅС‚РёСЋ РїРѕРєСЂС‹С‚РёСЏ.

---

## 4. РџРµСЂРµРєСЂС‘СЃС‚РЅС‹Рµ РґРµС„РµРєС‚С‹

### 4.1. РРґРµРјРїРѕС‚РµРЅС‚РЅРѕСЃС‚СЊ С…СЂСѓРїРєР°СЏ
- `BuildFingerprint(userRequest, maxIterations)` (`StartAppGenerationCommandHandler.cs:1269`) вЂ” РґРІР° РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ СЃ РѕРґРЅРёРј РїСЂРѕРјРїС‚РѕРј РїРѕР»СѓС‡Р°СЋС‚ С‡СѓР¶РѕР№ run.
- **Р¤РёРєСЃ**: РІРєР»СЋС‡РёС‚СЊ РІ fingerprint `triggerSource`, `actor`, `seedRunId`, РЅРѕСЂРјР°Р»РёР·РѕРІР°РЅРЅС‹Р№ РїР»Р°РЅ.

### 4.2. Reuse РїСЂРё `Status != Failed`
- `Handle:198` вЂ” СЂРµСЋР· РґР»СЏ `Cancelled` runs РІРѕР·РІСЂР°С‰Р°РµС‚ "СѓСЃРїРµС…" вЂ” РЅРµРІРµСЂРЅРѕ.
- **Р¤РёРєСЃ**: `Status == Completed` only.

### 4.3. Recovery cache key РёРіРЅРѕСЂРёСЂСѓРµС‚ prompt content
- `RecoveryCascadeService.BuildCacheKey` (line 188-194): `{ExceptionType}|Tokens|Messages|Attempt`. Р”РІР° СЂР°Р·РЅС‹С… РїСЂРѕРјРїС‚Р° СЃ РѕРґРёРЅР°РєРѕРІС‹Рј С‡РёСЃР»РѕРј С‚РѕРєРµРЅРѕРІ РїРѕР»СѓС‡Р°С‚ РєРѕСЂСЂСѓРїС‚РЅС‹Р№ СЂРµСЋР· recovery decision.
- **Р¤РёРєСЃ**: SHA256 РѕС‚ РїРµСЂРІС‹С… 4kB РїСЂРѕРјРїС‚Р° РІ РєР»СЋС‡.

### 4.4. `_recoveryCache` Р±РµР· trim
- Р Р°СЃС‚С‘С‚ РЅРµРѕРіСЂР°РЅРёС‡РµРЅРЅРѕ. Р”РѕР±Р°РІРёС‚СЊ max-size 1000 + LRU.

### 4.5. `RunWithRetryAsync` Р±РµР· jitter
- Exponential backoff Р±РµР· jitter в‡’ thundering herd.
- **Р¤РёРєСЃ**: `delay * Random.Shared.NextDouble() * 0.5 + delay` (full jitter).

---

## 5. Р§С‚Рѕ СѓР¶Рµ С…РѕСЂРѕС€Рѕ СЂРµР°Р»РёР·РѕРІР°РЅРѕ (РЅРµ С‚СЂРѕРіР°С‚СЊ)

- вњ… Phased generation (`contracts в†’ models в†’ services в†’ controllers в†’ tests в†’ infra`).
- вњ… `EnsureMandatoryAspNetManifest` + safety-net (РєРѕРЅС†РµРїС†РёСЏ РІРµСЂРЅР°).
- вњ… ReviewGate2 СЃС‚СЂСѓРєС‚СѓСЂР° checklist (СЂР°СЃС€РёСЂСЏРµРјР°).
- вњ… Idempotency С‡РµСЂРµР· fingerprint (РєРѕРЅС†РµРїС†РёСЏ РІРµСЂРЅР°, РЅСѓР¶РЅРѕ РґРѕСЂР°Р±РѕС‚Р°С‚СЊ РєР»СЋС‡).
- вњ… Cancellation propagation С‡РµСЂРµР· `linkedCts` (Р°СЂС…РёС‚РµРєС‚СѓСЂР° РІРµСЂРЅР°, fix lifetime).
- вњ… Subagent routing СЃ `MinimumRole` RBAC.
- вњ… Recovery cascade pattern (РЅСѓР¶РЅС‹ thread-safety + decay).
- вњ… MCP control plane.
- вњ… 50+ РёРЅС‚РµРіСЂР°С†РёРѕРЅРЅС‹С… С‚РµСЃС‚РѕРІ.

---

## 6. РђСЂС…РёС‚РµРєС‚СѓСЂРЅС‹Рµ СЌС‚Р°Р»РѕРЅС‹ РґР»СЏ Р±СѓРґСѓС‰РµРіРѕ

### 6.1. РЎРµРјР°РЅС‚РёС‡РµСЃРєРёРµ РїСЂРѕРІРµСЂРєРё С‡РµСЂРµР· AST
```csharp
public sealed class AuthImplementationRule_DotNet : IArchitectureCheckRule {
    public string CheckId => "auth_implementation";
    public async Task<ArchitectureCheckResult> EvaluateAsync(IReadOnlyList<GeneratedFile> files, GenerationPlan plan, CancellationToken ct) {
        var csFiles = files.Where(f => f.RelativePath.EndsWith(".cs"));
        foreach (var f in csFiles) {
            var tree = CSharpSyntaxTree.ParseText(f.Content, cancellationToken: ct);
            var root = await tree.GetRootAsync(ct);
            // РС‰РµРј services.AddAuthentication(...).AddJwtBearer(...)
            var addAuthCalls = root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(i => i.ToString().Contains("AddAuthentication"));
            if (addAuthCalls.Any()) return Pass();
        }
        return Fail("auth_not_wired_in_pipeline");
    }
}
```

### 6.2. Pipeline-stages
```csharp
public sealed class GenerationContext {
    public AppGenerationOrchestrator Orchestrator { get; init; }
    public GenerationPlan? Plan { get; set; }
    public List<GeneratedFile> Files { get; } = new();
    public IReadOnlyList<GenerationPhaseBatchResult>? PhaseBatches { get; set; }
    public Guid? WorkspaceId { get; set; }
    // ...
}

public sealed class PlanStage : IGenerationStage {
    public async Task<StageOutcome> ExecuteAsync(GenerationContext ctx, CancellationToken ct) { /* ... */ }
}
```

### 6.3. `IStackStrategy`
```csharp
public interface IStackStrategy {
    string StackId { get; }
    bool Matches(GenerationPlan plan);
    IReadOnlyList<GeneratedFile> BuildSafetyNet(GenerationPlan plan, IReadOnlyList<GeneratedFile> current);
    IReadOnlyList<GeneratedFile> BuildDeterministicArtifacts(GenerationPlan plan, IReadOnlyList<GeneratedFile> current);
    IReadOnlyList<IArchitectureCheckRule> ArchitectureRules { get; }
    string PreferredRuntimeImage { get; }
    IReadOnlyList<string> DefaultBuildCommands { get; }
    IReadOnlyList<string> DefaultTestCommands { get; }
}

public interface IStackStrategyResolver {
    IStackStrategy Resolve(GenerationPlan plan);
    IReadOnlyList<IStackStrategy> All { get; }
}
```

---

## 7. Р”РѕСЂРѕР¶РЅР°СЏ РєР°СЂС‚Р° СЂРµР»РёР·РѕРІ

| Р РµР»РёР· | РЎСЂРѕРє | РЎРѕРґРµСЂР¶Р°РЅРёРµ |
|-------|------|------------|
| **v0.9.1 (СЌС‚РѕС‚ РєРѕРјРјРёС‚)** | СЃРµР№С‡Р°СЃ | P0-1вЂ¦P0-9: РєРІРёРє-С„РёРєСЃС‹, thread-safety, eviction, blocking build gate |
| **v0.10** | +1 РЅРµРґ | P1-1 (Roslyn auth/error/observability), P1-2 (failure classifier), P1-3 (pipeline stages) |
| **v0.11** | +2 РЅРµРґ | P1-4 (OpenTelemetry), P1-5 (Polly), P1-6 (BackgroundService), P1-9 (IStackStrategy) |
| **v0.12** | +3 РЅРµРґ | P1-7 (FsCheck), P1-8 (Scriban templates), P1-10 (plan validator) |
| **v1.0** | +6 РЅРµРґ | P2-1 (EF Core + Postgres), P2-2 (pgvector), P2-5 (cost budget) |
| **v1.1** | +9 РЅРµРґ | P2-3 (multi-tenant), P2-6 (Rust sidecar), P2-7 (F# rules engine) |

---

## 8. Status table

| ID | Title | Priority | Status |
|----|-------|----------|--------|
| P0-1 | Console.WriteLine removal | P0 | вњ… done |
| P0-2 | Placeholder tests in fallback | P0 | вњ… done |
| P0-3 | RecoveryCascade thread-safety | P0 | вњ… done |
| P0-4 | CTS lifecycle | P0 | вњ… done |
| P0-5 | Repository LRU/TTL | P0 | вњ… done |
| P0-6 | Build gate blocking option | P0 | вњ… done |
| P0-7 | JSON parse logging | P0 | вњ… done |
| P0-8 | Null-safety in fallback | P0 | вњ… done |
| P0-9 | StackPlanHeuristics consolidation | P0 | вњ… done (call-sites migrated in v0.9.5) |
| P1-1 | Roslyn semantic checks | P1 | вњ… done (`IArchitectureCheckRule` + `AuthImplementationRule_DotNet` + ReviewGate2 wiring; semantic rule overrides legacy substring match for same `CheckId`) |
| P1-2 | ExecutionFailureClassifier | P1 | вњ… done |
| P1-3 | Pipeline stages decomposition | P1 | ✅ done — pipeline runner wired into `StartAppGenerationCommandHandler.Handle` planning-prefix behind `AutonomousLoopGuardOptions.UsePipelineRunnerForPlanningPrefix` (default true). Legacy inline path retained as fallback (flag=false). 35/35 pipeline tests pass. |
| P1-4 | OpenTelemetry | P1 | вњ… done (BCL `Meter` + `ActivitySource`; instruments: runs.started/completed/iterations, gate.score, build_gate.aborted, consolidation.*, llm.step_ms, fallback.used) |
| P1-5 | LLM circuit breaker + budget | P1 | ✅ done (LlmCircuitBreaker per-provider state machine + InMemoryBudgetService; AIService decorated with CB; LlmCircuitOpenException for caller fallback) |
| P1-6 | BackgroundService consolidation | P1 | вњ… done (bounded Channel + hosted service; legacy Task.Run kept as fallback) |
| P1-7 | Property-based tests | P1 | вњ… done (lightweight harness without FsCheck dep вЂ” `PipelineInvariantsTests` 7Г—64 random shapes) |
| P1-8 | Templates Scriban | P1 | вњ… done (`IFallbackArtefactTemplateEngine` + `ScribanFallbackTemplateEngine` + `FallbackArtefactTemplates`) |
| P1-9 | IStackStrategy DI | P1 | вњ… done (`IStackStrategy` + 4 strategies + `StackStrategyResolver` AND all 5 `IsXxxPlan` call-sites migrated to delegate to `StackPlanHeuristics`) |
| P1-10 | Plan validator | P1 | вњ… done |
| P1-11 | CsprojPackageReconciler (using↔PackageReference) | P1 | ✅ done — `Infrastructure/CsprojPackageReconciler.cs` scans .cs files for `using Foo.Bar;`, maps via curated table (OpenTelemetry/Polly/Serilog/EF Core/JwtBearer/MassTransit/Stripe/...) to `<PackageReference>` and injects missing entries into the .csproj that owns each .cs. Wired in `StartAppGenerationCommandHandler.Handle` right after generation, before phase-build gates. 5 unit tests in `CsprojPackageReconcilerTests`. |
| P1-12 | StrictPerPhase defers to fix-loop instead of hard abort | P1 | ✅ done — phase-build failure under StrictPerPhase now `break`s out of the per-phase loop instead of `MarkFailed`+return. Iteration loop (with LLM-driven build-error fixer + CsprojPackageReconciler safety-net) gets a chance to repair compile errors before the run is failed. Run only fails for real if `MaxIterations` exhausted without recovery — preserves P0-6 intent (no phantom green) without sacrificing recoverability. |
| P1-13 | DockerModelRunner reasoning-fallback truncation guard | P1 | ✅ done — provider used to return entire `reasoning_content` (70K+ chars of raw chain-of-thought) as `content` when the model didn't close `</think>`. Now only salvages reasoning when ≤8K chars AND tail looks like a structured answer (JSON/code-fence/`final answer`); otherwise throws `HttpRequestException` so AIService circuit breaker / retry kicks in instead of feeding garbage to the next pipeline stage. |
| P1-14 | Workspace-sync gap before iteration loop | P1 | ✅ done — discovered during e2e #2: when StrictPerPhase `break` (P1-12) exits the per-phase loop early, the bind-mounted shadow workspace still contains only the scaffold .csproj/.sln from the early-exit phase. Source files (Program.cs, controllers, services) were in `orchestrator.Files` but never written to disk, so the iteration fix loop kept seeing CS5001 "no entry point". Fix: explicit `_shadow.UpdateWorkspaceAsync(workspaceId, orchestrator.Files, runCt)` immediately before iteration loop start, guaranteeing the workspace reflects the full post-reconciliation file set regardless of phase-loop exit path. |
| P2-1 | EF Core + Postgres | P2 | вљ пёЏ partial вЂ” separate `Libr4.IDE.AutonomousAppGeneration.Persistence` project with `AutoGenDbContext` + `RunRegistryEntry` + `EfCoreAppGenerationRepository` (hybrid: metadata in DB, full state in-memory); host opt-in via `AddPostgresPersistence(connectionString)`. Full domain snapshot/rehydrate awaiting `AppGenerationOrchestrator` rehydrate API. |
| P2-2 | pgvector / Qdrant | P2 | ✅ done (IVectorMemoryStore + InProcessVectorMemoryStore cosine-sim; DI-registered; swap to pgvector/Qdrant adapter for prod) |
| P2-3 | Multi-region / multi-tenant | P2 | ✅ partial-done — `TenantId` added to `AppGenerationOrchestrator` + `SetTenantId` method; `StartAppGenerationCommand.TenantId` propagated; `BuildFingerprint` includes tenantId; `IAppGenerationRepository.ListByTenantAsync` interface + implementation in InMemory + EfCore; full federated/cross-region runs deferred. |
| P2-4 | UI for checkpoints | P2 | ✅ done — Next.js 15 / React 19 RC frontend section under `frontend/src/app/app-generation/`. Pages: `page.tsx` (start form + polled run list with auto-refresh) and `[id]/page.tsx` (run detail with header, plan summary, quality-gate timeline, iterations, file tree+viewer, pause/resume/cancel/export controls; auto-refresh while active). Components: `StartRunForm`, `RunStatusBadge`, `QualityGateTimeline` (CheckCircle2/XCircle markers + reasons list), `IterationList`, `FileTree` (tree+content viewer), `RunActions`. New UI primitives: `ui/badge.tsx`, `ui/textarea.tsx`. Typed API client `lib/app-generation-api.ts` covers start/list/get/pause/resume/cancel/export/state endpoints. Wiring: `next.config.mjs` adds `/api/ide/:path*` rewrite to `NEXT_PUBLIC_AUTOGEN_BASE_URL` (default `:5200`); `.env.example` extended; home page links to `/app-generation`. Note: `npm install` requires stable network — run `npm install --legacy-peer-deps` to fetch `react`, `class-variance-authority`, `@radix-ui/react-slot`, `clsx`, `tailwind-merge`, `@types/react` before `npm run typecheck`. |
| P2-5 | LLM cost budget | P2 | ✅ done (per-run + per-day + per-tenant daily token/USD caps in BudgetOptions + InMemoryBudgetService; production: swap to distributed quota tracker) |
| P2-6 | Rust sidecar | P2 | ✅ done — spec doc `docs/RUST_SIDECAR_SPEC.md` (purpose, IPC: named-pipes/gRPC + protobuf schema) + `IRustAnalysisSidecar` interface + `NullRustAnalysisSidecar` no-op impl registered in DI via `TryAddSingleton`. Real Rust process binding deferred until production deployment. |
| P2-7 | F# rules engine | P2 | ✅ done (`Libr4.IDE.AutonomousAppGeneration.Rules.FSharp` — `RulesDomain.fs` (DU `Rule = StackSpecific|Cross`, `Stack`, `RuleOutcome`) + `ReviewGate2Rules.fs` (6 built-in rules: `error_handling`, `observability_baseline`, `semantic_security`, `auth_implementation` for .NET/Python/Node); `FSharpRulesAdapter` bridges to `IArchitectureCheckRule`; 3 adapters registered in DI) |

---

## 9. РљРѕРЅС‚СЂРѕР»СЊРЅС‹Рµ С‚РѕС‡РєРё РїСЂРёС‘РјРєРё v0.9.1 (СЌС‚РѕС‚ РєРѕРјРјРёС‚)

- [x] `dotnet build Libr4.IDE.AutonomousAppGeneration` Р±РµР· РѕС€РёР±РѕРє.
- [x] `dotnet test --filter "AutonomousGenerationPipelineTests|RecoveryCascadeServiceTests|CrossStackRemediationTests"` вЂ” pass.
- [x] РќРµС‚ `Console.WriteLine` РІ production-РєРѕРґРµ СЃРµСЂРІРёСЃР°.
- [x] `Assert.True(true)` РѕС‚СЃСѓС‚СЃС‚РІСѓРµС‚ РІ `MinimalFallbackProject`.
- [x] `RecoveryCascadeService` РёСЃРїРѕР»СЊР·СѓРµС‚ `ConcurrentDictionary`.
- [x] CTS lifetime РІ `Handle` РєРѕСЂСЂРµРєС‚РµРЅ (РЅРµС‚ `using var` РґР»СЏ Р·Р°СЂРµРіРёСЃС‚СЂРёСЂРѕРІР°РЅРЅРѕРіРѕ CTS).
- [x] `InMemoryAppGenerationRepository` РёРјРµРµС‚ capacity в‰¤ 256 Рё TTL в‰¤ 24h.
- [x] Build gate Р±Р»РѕРєРёСЂСѓСЋС‰РёР№ РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ (`BuildGateBlockingMode = StrictPerPhase`).
- [x] `StackPlanHeuristics` вЂ” РµРґРёРЅСЃС‚РІРµРЅРЅС‹Р№ РёСЃС‚РѕС‡РЅРёРє РїСЂР°РІРёР» `IsAspNetCore/IsPython/IsNode`.

---

**Р”РѕРєСѓРјРµРЅС‚ РІРµРґС‘С‚СЃСЏ РєР°Рє Р¶РёРІРѕР№**: РґРѕР±Р°РІР»СЏР№С‚Рµ РЅРѕРІС‹Рµ РЅР°С…РѕРґРєРё РєР°Рє `Pn-N`, РёР·РјРµРЅРµРЅРёСЏ СЃС‚Р°С‚СѓСЃРѕРІ С„РёРєСЃРёСЂСѓР№С‚Рµ РІ В§ 8.


