# Детальная сверка: AI Service (Python vs C#)

## Python endpoints (ai.py - найдено):
- POST /analyze-task-complexity
- POST /create-task-with-ai-analysis
- POST /analyze-application
- GET /generate-interview-questions
- GET /task-recommendations
- POST /score-skills
- POST /order-assistant
- POST /smart-assistant
- POST /suggest-level-upgrade
- GET /ping

## C# endpoints (найдено):
- GET /api/v1/ai/chats/
- POST /api/v1/ai/chats/create
- GET /api/v1/ai/chats/my
- GET /api/v1/ai/chats/{chatId}
- POST /api/v1/ai/chats/message

## ❌ Критичные расхождения:
1. **Task Analysis** - отсутствует (analyze-task-complexity)
2. **Application Analysis** - отсутствует (analyze-application)
3. **Interview Questions** - отсутствует
4. **Task Recommendations** - отсутствует
5. **Skill Scoring** - отсутствует
6. **Order Assistant** - отсутствует
7. **Smart Assistant** - отсутствует
8. **Level Upgrade** - отсутствует

## Статус: 🔴 ПОРТИРОВАНО НА ~20%

**Примечание:** C# имеет только базовый чат API, все AI-функции анализа отсутствуют.

**Создано:** 2026-04-19 01:48:45
