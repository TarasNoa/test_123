# Getting Started

Пошаговая инструкция, как поднять проект с нуля.

## 0. Установка предпосылок

```powershell
# .NET 8 SDK (Windows)
winget install --id Microsoft.DotNet.SDK.8 -e
# EF Core CLI (для миграций)
dotnet tool install --global dotnet-ef --version 8.0.10
```

Node.js 20+ и Docker Desktop у тебя уже есть.

Проверь:
```powershell
dotnet --version   # 8.0.x
node --version     # v20+
docker --version   # 20+
```

## 1. Подготовить окружение

```powershell
cd C:\Users\user\Desktop\libr4
Copy-Item .env.example .env
Copy-Item frontend\.env.example frontend\.env.local
```

## 2. Поднять инфраструктуру (Postgres, Redis, RabbitMQ, Prometheus, Grafana)

```powershell
docker compose -f docker-compose.infra.yml up -d
```

Проверить:
```powershell
docker ps
```
Должны быть запущены: `libr4-postgres`, `libr4-redis`, `libr4-rabbitmq`, `libr4-prometheus`, `libr4-grafana`.

## 3. Собрать solution

```powershell
dotnet restore libr4.sln
dotnet build libr4.sln
```

## 4. Создать первую миграцию Auth

```powershell
cd src\Services\Auth\Libr4.Auth.Api
dotnet ef migrations add InitialCreate --project ..\Libr4.Auth.Infrastructure --startup-project . --context AuthDbContext --output-dir ..\Libr4.Auth.Infrastructure\Persistence\Migrations
cd ..\..\..\..
```

Migration будет применена автоматически при старте Auth.Api в Development.

## 5. Запустить backend

В разных терминалах:

```powershell
# Gateway
dotnet run --project src\Gateway\Libr4.Gateway

# Auth
dotnet run --project src\Services\Auth\Libr4.Auth.Api

# (опционально) остальные скелеты
dotnet run --project src\Services\Tasks\Libr4.Tasks.Api
dotnet run --project src\Services\Payments\Libr4.Payments.Api
dotnet run --project src\Services\Chat\Libr4.Chat.Api
dotnet run --project src\Services\Trading\Libr4.Trading.Api
dotnet run --project src\Services\AI\Libr4.AI.Api
```

Проверить:
- `http://localhost:5000/health/ready` — Gateway
- `http://localhost:5001/swagger` — Auth API
- `http://localhost:5001/metrics` — Prometheus метрики

## 6. Запустить frontend

```powershell
cd frontend
npm install
npm run dev
```

Открыть `http://localhost:3000`.

## 7. Проверка end-to-end

1. Перейти на `/register`
2. Создать аккаунт (email, имя, пароль ≥8 символов c буквами и цифрами)
3. Автоматически попадёшь в `/dashboard` с JWT
4. Открой DevTools → Application → Local Storage — должны лежать `libr4.accessToken` и `libr4.refreshToken`
5. Выйти → `/login` → снова войти → токены обновятся

## 8. Полный стек через Docker

Когда guбoucль протестируется локально:
```powershell
docker compose up -d --build
```

Открыть:
- `http://localhost:3000` — frontend
- `http://localhost:5000` — gateway
- `http://localhost:9090` — Prometheus
- `http://localhost:3001` — Grafana (admin/admin)
- `http://localhost:15672` — RabbitMQ UI (guest/guest)

## Что дальше

См. [`MIGRATION_PLAN.md`](MIGRATION_PLAN.md) — план по Session 2..7 (Tasks, Payments, Chat, Trading, AI agents, Production).
