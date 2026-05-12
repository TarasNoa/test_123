# Libr4 Backend Services Status Report

Generated: 2026-05-12

## Summary

Multiple backend services have startup/runtime issues preventing the full E2E test (`Run-FullFlowE2E.ps1`) from passing. This document catalogs the known problems per service.

---

## Auth API (`http://localhost:5001`) — WORKING

- **Status**: Starts and runs correctly.
- **Notes**: Publishes `UserRegisteredIntegrationEvent` to RabbitMQ.

---

## Payments API (`http://localhost:5003`) — PARTIALLY WORKING

- **Status**: Starts, RabbitMQ consumer works, basic wallet endpoints work.
- **Known Issue**: `CreateEscrowCommand` handler requires wallet auto-creation + balance credit logic that does not crash on `SaveChangesAsync`.
  - `DbUpdateConcurrencyException` was thrown when using `Wallet.Credit()` / `Wallet.Hold()` domain methods because EF Core cannot track changes to the `readonly List<WalletEntry> _entries` backing field.
  - Current workaround uses `_dbContext.Entry(clientWallet).Property(...).CurrentValue` to mutate wallet state directly.
  - **Root Cause Needed**: Proper domain-driven fix for `Wallet._entries` collection tracking without bypassing domain encapsulation.
- **StripeService**: Uses a fallback `try/catch` around `PaymentIntentService.CreateAsync` because `Stripe__SecretKey` is a placeholder (`sk_test_placeholder`).

---

## Chat API (`http://localhost:5004`) — PARTIALLY WORKING

- **Status**: Starts and serves core chat endpoints.
- **Known Issue**: `MapServerEndpoints` and `MapCodeShareEndpoints` are commented out in `Program.cs` because their injected services (`IServerService`, `ICodeSnippetService`, etc.) are missing DI registrations or implementations.
  - `ServerEndpoints.cs` endpoints reference `IServerService`, `ICallService`, `IMediaService` which exist but are not wired in DI.
  - `CodeShareEndpoints.cs` references `ICodeSnippetService` — interface exists but no implementation was found.
  - `FileEndpoints.cs` references `IFileStorageService` — interface exists but no implementation was found.
- **Body Inference Error**: Previously threw `InvalidOperationException: Body was inferred but the method does not allow inferred body parameters.` when unregistered service interfaces were used as Minimal API handler parameters.

---

## AI API (`http://localhost:5006`) — NOT STARTING

- **Status**: Crashes during DI container validation on startup.
- **Missing DI Registrations**:
  - `ICurrentUser` — fixed by adding `builder.Services.AddLibr4CurrentUser()` in `Program.cs`.
  - `IHarnessEnvironment` — referenced by `ReactionEngine`, `HarnessHook`; commented out in DI.
  - `CodeExtractor` — referenced by `CodebaseMapper`; no registration found.
  - `IEnhancedMemory` — referenced by `EnhancedMemoryWithGraph`; no registration found.
- **Lifetime Mismatches**:
  - `IAgentService` registered as Singleton but consumes scoped `DbContextOptions<AIDbContext>`.
  - `GitIntegrationHook` registered as Singleton but consumes scoped `IGitIntegrationService`.
- **Provider Configuration**: Needs to use Docker Model Runner (DMR) instead of Ollama. DMR provider class exists (`AI.Providers.DockerModelRunnerProvider`) but is not selected by default.

---

## Matching API (`http://localhost:5010`) — NOT STARTED

- **Status**: Not yet started or tested.
- **Notes**: Needs build + runtime verification.

---

## Tasks API (`http://localhost:5002`) — ASSUMED WORKING

- **Status**: Was fixed in earlier sessions (category enum validation, `TaskStatus` ambiguity).
- **Notes**: Not re-verified in this session.

---

## E2E Test Script

- **File**: `tests/e2e/Run-FullFlowE2E.ps1`
- **Current Blockers**:
  1. Chat API must have all endpoints available (server/code-share endpoints need proper DI).
  2. AI API must start (missing DI registrations + lifetime mismatches).
  3. Matching API must start.
  4. Payments API escrow creation needs a clean domain fix (not `Entry().CurrentValue` hacks).

---

## Files with Temporary/Commented Changes

- `src/Services/Payments/Libr4.Payments.Application/Escrow/Commands/CreateEscrowCommand.cs` — uses `Entry().Property().CurrentValue` workaround.
- `src/Services/Chat/Libr4.Chat.Api/Program.cs` — `MapServerEndpoints`, `MapCodeShareEndpoints`, `MapFileEndpoints` commented out.
- `src/Services/AI/Libr4.AI.Api/Program.cs` — added `AddLibr4CurrentUser()`.
- `src/Services/AI/Libr4.AI.Infrastructure/DependencyInjection.cs` — multiple scoped/singleton registrations commented out.
- `src/Services/AI/Libr4.AI.Application/DependencyInjection.cs` — `IAgentService`, `IOrderAssistantService`, `ITaskRecommendationService` commented out.
