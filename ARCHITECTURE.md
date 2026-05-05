# libr4 — Architecture

## High-level

```
                    ┌─────────────────────┐
                    │  Next.js 15 (3000)  │
                    └──────────┬──────────┘
                               │ HTTPS + JWT (Bearer)
                    ┌──────────▼──────────┐
                    │  YARP Gateway (5000)│
                    └─┬──┬──┬──┬──┬──┬────┘
            ┌─────────┘  │  │  │  │  └────────────┐
            ▼            ▼  ▼  ▼  ▼               ▼
         Auth(5001)  Tasks  Pay Chat Trading    AI(5006)
                    (5002) (5003)(5004)(5005)   Ollama proxy
            │            │  │  │  │               │
            └───────┬────┴──┴──┴──┴───────────────┘
                    │
      ┌─────────────┼─────────────┬──────────┐
      ▼             ▼             ▼          ▼
   Postgres      Redis       RabbitMQ    Prometheus
   (per svc)   (cache)   (integration    (metrics)
                             events)
```

## Паттерны

- **Модульный микросервисный монорепо**: каждый сервис = 4 проекта (Domain / Application / Infrastructure / Api) по Clean Architecture.
- **Domain-Driven Design** (lightweight): `Entity`, `AggregateRoot`, `IDomainEvent`, `Result<T>`.
- **CQRS через MediatR**: Commands меняют состояние, Queries читают.
- **Outbox pattern** в Infrastructure (через MassTransit EF Core transactional outbox) — надёжная публикация integration events.
- **Каждый сервис владеет своей БД** (separate `libr4_auth`, `libr4_tasks`, ...).
- **Интеграция через события**: `UserRegisteredIntegrationEvent` в RabbitMQ → подписчики (Chat создаёт профиль, и т.д.).
- **API Gateway**: YARP, единая точка аутентификации через общий JWT issuer.
- **RBAC через claims**: `role` claim в JWT, policies в каждом сервисе.

## Стандартные библиотеки

Все сервисы используют `Libr4.Shared.*`:

- `Libr4.Shared.Kernel` — `Entity`, `AggregateRoot`, `Result<T>`, `Error`, `IClock`, domain event dispatcher.
- `Libr4.Shared.Contracts` — integration event DTO (shared across services).
- `Libr4.Shared.Infrastructure` — `DbContextBase` + outbox, Redis DI, MassTransit setup, OpenTelemetry.
- `Libr4.Shared.Web` — JWT Bearer, ExceptionHandlingMiddleware, ProblemDetails, health checks, Swagger, rate limit.

## Аутентификация

- **JWT HS256** (symmetric secret) для простоты dev; в prod заменить на RS256 + rotating keys.
- **Access token**: 15 min, содержит `sub`, `email`, `role`, `jti`.
- **Refresh token**: 30 дней, хранится в таблице `refresh_tokens` с `revoked_at`, `replaced_by_token`.
- **Rotation**: каждый refresh выпускает новый refresh и инвалидирует старый (cascade revocation при подозрении).
- **2FA**: TOTP (RFC 6238) через `Otp.NET`, QR-код через `QRCoder`, секрет шифруется AES-GCM через Data Protection.

## RBAC

Роли: `Admin`, `User`, `Support`, `Trader`, `Freelancer`, `Client`. Хранятся в таблице `user_roles` (many-to-many). В JWT кладутся как `role` claims.

Policies в `Libr4.Shared.Web.Auth.AuthorizationExtensions`:
- `RequireAdmin`
- `RequireAuthenticated`
- И т.д.

## Observability

- **Serilog** → Console + Seq (опционально) в dev, → OTLP в prod.
- **OpenTelemetry**: traces (ASP.NET, HttpClient, EF, MassTransit), metrics → Prometheus `/metrics`.
- **Health checks**: `/health/live`, `/health/ready` (DB + Redis + RabbitMQ).

## Async Jobs (замена Celery)

**Hangfire** для долгих задач (email, отчёты) с Redis/Postgres storage + дашбордом на `/hangfire`. Периодические задачи через `RecurringJob.AddOrUpdate`.

Для event-driven workflow — **MassTransit + RabbitMQ** (consumers).

## AI-модуль

`Libr4.AI.Api` проксирует запросы в локальный **Ollama** (`http://ollama:11434`) и в внешние **OpenAI-совместимые** API (OpenAI, Anthropic, Groq, OpenRouter) через единый контракт `/v1/chat/completions`. Никаких Python-зависимостей.

## Frontend

Next.js 15 App Router. Серверные компоненты рендерят публичные страницы, клиентские — интерактив (chat, trading UI). Auth через HttpOnly refresh cookie + in-memory access token. TanStack Query для data fetching.

## Testing

- **xUnit** — unit.
- **Testcontainers** — интеграционные (реальный Postgres/Redis/Rabbit в Docker).
- **WebApplicationFactory** — API-тесты.
- **Playwright** — e2e (frontend).

## Deployment

- **docker-compose.yml** — dev окружение (всё разом).
- **docker-compose.prod.yml** — prod-like локально.
- **infra/k8s/** — манифесты (namespace, deployments, services, ingress, secrets).
- **GitHub Actions** — `build → test → docker push → deploy` per service.
