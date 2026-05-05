# Детальная сверка: Chat Service (Python vs C#)

## Python endpoints (chats.py):
- GET /chats - List user chats (line 80)
- POST /chats - Create chat (line 161)
- GET /chats/{id} - Get by ID (line 252)
- GET /chats/{id}/messages - Get messages (line 289)
- POST /chats/{id}/messages - Send message (line 371)
- GET /chats/task/{task_id} - Task chats (line 435)
- DELETE /chats/{id} - Delete chat (line 459)
- POST /chats/{id}/upload - Upload file (line 501)
- GET /chats/files/{name} - Download file (line 558)

## C# endpoints (проверено):
- GET /api/v1/chat/my
- GET /api/v1/chat/{chatId}
- POST /api/v1/chat/direct
- POST /api/v1/chat/group
- POST /api/v1/chat/{chatId}/join
- POST /api/v1/chat/{chatId}/leave

## Статус: � ДОПОЛНЕНО ML-ФУНКЦИИ

### ✅ Выполненные улучшения (2026-04-19):
- ✅ Sentiment Analysis (SentimentScore, SentimentLabel)
- ✅ Spam Detection (IsSpam, SpamScore)
- ✅ Conflict Detection (IsConflictDetected)
- ✅ Professional Tone Assessment (ProfessionalToneScore)
- ✅ Domain methods: SetSentimentAnalysis(), SetSpamDetection(), SetConflictDetection(), SetProfessionalTone()

**Файл:** `Libr4.Chat.Domain/Messages/Message.cs`

**Создано:** 2026-04-19 01:45:52
