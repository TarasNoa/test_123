using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Deterministic full-repo file plan for Java+React banking (per-file generation, no duplicate paths).
/// </summary>
public static class JavaReactExpandedFileManifest
{
    public static IReadOnlyList<PlannedFileEntry> AllForPlan(GenerationPlan plan)
    {
        if (StackPlanHeuristics.Classify(plan) != StackKind.JavaReactFullStack)
            return Array.Empty<PlannedFileEntry>();

        return BackendEntries()
            .Concat(DatabaseEntries())
            .Concat(FrontendEntries())
            .Concat(DevOpsEntries())
            .GroupBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    public static IReadOnlyList<PlannedFileEntry> ForPhase(AgentPhase phase) =>
        phase switch
        {
            AgentPhase.Backend => BackendEntries(),
            AgentPhase.Frontend => FrontendEntries(),
            AgentPhase.Database => DatabaseEntries(),
            AgentPhase.DevOps => DevOpsEntries(),
            AgentPhase.Documentation => new[] { Entry("README.md", AgentPhase.Documentation, "Root README for developers.", "tech-writer") }.ToList(),
            _ => Array.Empty<PlannedFileEntry>()
        };

    public static IReadOnlySet<string> MinimalSpinePaths { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "backend/pom.xml",
        "backend/src/main/java/com/generated/banking/BankingApplication.java",
        "backend/src/main/resources/application.yml",
        "frontend/package.json",
        "frontend/tsconfig.json",
        "frontend/vite.config.ts",
        "frontend/index.html",
        "frontend/src/main.tsx"
    };

    private static IReadOnlyList<PlannedFileEntry> BackendEntries() => new[]
    {
        Entry("backend/pom.xml", AgentPhase.Backend, "Maven: Spring Boot 3.2+, web, validation, security, data-jpa, flyway, test.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/BankingApplication.java", AgentPhase.Backend, "Spring Boot main class.", "java-spring"),
        Entry("backend/src/main/resources/application.yml", AgentPhase.Backend, "Server, datasource H2, JPA, Flyway, logging.", "java-spring"),
        Entry("backend/src/main/resources/application-dev.yml", AgentPhase.Backend, "Dev profile overrides.", "java-spring"),
        Entry("backend/src/main/resources/logback-spring.xml", AgentPhase.Backend, "Structured logging pattern.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/config/SecurityConfig.java", AgentPhase.Backend, "Spring Security: permit health, protect API.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/config/WebConfig.java", AgentPhase.Backend, "CORS for frontend dev server.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/config/CorrelationIdFilter.java", AgentPhase.Backend, "Request correlation id filter.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/model/Account.java", AgentPhase.Backend, "JPA entity Account.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/model/Transaction.java", AgentPhase.Backend, "JPA entity Transaction.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/model/User.java", AgentPhase.Backend, "JPA entity User (auth).", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/dto/AccountDto.java", AgentPhase.Backend, "DTO for account responses.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/dto/TransferRequest.java", AgentPhase.Backend, "DTO transfer request.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/dto/TransferResponse.java", AgentPhase.Backend, "DTO transfer response.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/dto/PaymentRequest.java", AgentPhase.Backend, "DTO payment request.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/dto/AuthTokenRequest.java", AgentPhase.Backend, "DTO auth token request.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/dto/ErrorResponse.java", AgentPhase.Backend, "Standard API error body.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/repository/AccountRepository.java", AgentPhase.Backend, "JPA repository accounts.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/repository/TransactionRepository.java", AgentPhase.Backend, "JPA repository transactions.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/service/AccountService.java", AgentPhase.Backend, "Account business logic.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/service/TransferService.java", AgentPhase.Backend, "Transfer logic with validation.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/service/PaymentService.java", AgentPhase.Backend, "Payment processing logic.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/service/AuthService.java", AgentPhase.Backend, "Token issuance logic.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/web/AccountController.java", AgentPhase.Backend, "REST /api/accounts.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/web/TransferController.java", AgentPhase.Backend, "REST /api/transfers.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/web/PaymentController.java", AgentPhase.Backend, "REST /api/payments.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/web/AuthController.java", AgentPhase.Backend, "POST /api/auth/token.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/web/HealthController.java", AgentPhase.Backend, "Health/readiness endpoints.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/web/GlobalExceptionHandler.java", AgentPhase.Backend, "@ControllerAdvice mapping.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/exception/ResourceNotFoundException.java", AgentPhase.Backend, "404 domain exception.", "java-spring"),
        Entry("backend/src/main/java/com/generated/banking/exception/InsufficientFundsException.java", AgentPhase.Backend, "422 domain exception.", "java-spring"),
        Entry("backend/src/test/java/com/generated/banking/BankingApiTests.java", AgentPhase.Backend, "WebApplicationFactory HTTP tests.", "java-spring"),
        Entry("backend/src/test/java/com/generated/banking/service/TransferServiceTest.java", AgentPhase.Backend, "Unit tests TransferService.", "java-spring"),
        Entry("backend/src/test/java/com/generated/banking/service/AccountServiceTest.java", AgentPhase.Backend, "Unit tests AccountService.", "java-spring")
    };

    private static IReadOnlyList<PlannedFileEntry> DatabaseEntries() => new[]
    {
        Entry("backend/src/main/resources/db/migration/V1__init_schema.sql", AgentPhase.Database, "Flyway: accounts, transactions, users.", "java-spring"),
        Entry("backend/src/main/resources/db/migration/V2__seed_data.sql", AgentPhase.Database, "Flyway: demo seed rows.", "java-spring")
    };

    private static IReadOnlyList<PlannedFileEntry> FrontendEntries() => new[]
    {
        Entry("frontend/package.json", AgentPhase.Frontend, "React 18 + TS + Vite + Vitest + RTL.", "typescript"),
        Entry("frontend/tsconfig.json", AgentPhase.Frontend, "TS strict config.", "typescript"),
        Entry("frontend/tsconfig.node.json", AgentPhase.Frontend, "TS config for Vite.", "typescript"),
        Entry("frontend/vite.config.ts", AgentPhase.Frontend, "Vite + proxy to backend.", "typescript"),
        Entry("frontend/vitest.config.ts", AgentPhase.Frontend, "Vitest config.", "typescript"),
        Entry("frontend/index.html", AgentPhase.Frontend, "HTML shell.", "typescript"),
        Entry("frontend/.env.example", AgentPhase.Frontend, "VITE_API_BASE example.", "typescript"),
        Entry("frontend/src/main.tsx", AgentPhase.Frontend, "React entry.", "typescript"),
        Entry("frontend/src/App.tsx", AgentPhase.Frontend, "App shell + routes.", "typescript"),
        Entry("frontend/src/App.css", AgentPhase.Frontend, "Global app styles.", "typescript"),
        Entry("frontend/src/styles/global.css", AgentPhase.Frontend, "Design tokens / layout.", "typescript"),
        Entry("frontend/src/api/types.ts", AgentPhase.Frontend, "API TypeScript types.", "typescript"),
        Entry("frontend/src/api/client.ts", AgentPhase.Frontend, "fetch wrappers accounts/transfers/auth.", "typescript"),
        Entry("frontend/src/context/AuthContext.tsx", AgentPhase.Frontend, "Auth token context.", "typescript"),
        Entry("frontend/src/hooks/useAccounts.ts", AgentPhase.Frontend, "Hook load accounts.", "typescript"),
        Entry("frontend/src/hooks/useAuth.ts", AgentPhase.Frontend, "Hook login/token.", "typescript"),
        Entry("frontend/src/components/Layout.tsx", AgentPhase.Frontend, "Page layout chrome.", "typescript"),
        Entry("frontend/src/components/Header.tsx", AgentPhase.Frontend, "App header/nav.", "typescript"),
        Entry("frontend/src/components/AccountList.tsx", AgentPhase.Frontend, "Accounts table/cards.", "typescript"),
        Entry("frontend/src/components/TransferForm.tsx", AgentPhase.Frontend, "Transfer form UI.", "typescript"),
        Entry("frontend/src/components/PaymentForm.tsx", AgentPhase.Frontend, "Payment form UI.", "typescript"),
        Entry("frontend/src/pages/AccountsPage.tsx", AgentPhase.Frontend, "Accounts page.", "typescript"),
        Entry("frontend/src/pages/TransfersPage.tsx", AgentPhase.Frontend, "Transfers page.", "typescript"),
        Entry("frontend/src/pages/LoginPage.tsx", AgentPhase.Frontend, "Login page.", "typescript"),
        Entry("frontend/src/App.test.tsx", AgentPhase.Frontend, "App smoke test.", "typescript"),
        Entry("frontend/src/api/client.test.ts", AgentPhase.Frontend, "API client unit test.", "typescript")
    };

    private static IReadOnlyList<PlannedFileEntry> DevOpsEntries() => new[]
    {
        Entry("docker-compose.yml", AgentPhase.DevOps, "Backend + frontend services.", "generic-devops"),
        Entry("backend/Dockerfile", AgentPhase.DevOps, "Multi-stage Java 21 image.", "generic-devops"),
        Entry("frontend/Dockerfile", AgentPhase.DevOps, "Node build + nginx serve.", "generic-devops"),
        Entry(".gitignore", AgentPhase.DevOps, "Java/Node/IDE ignores.", "generic-devops"),
        Entry("README.md", AgentPhase.DevOps, "Project overview, run instructions.", "tech-writer")
    };

    private static PlannedFileEntry Entry(string path, AgentPhase phase, string description, string role) =>
        new(path, phase, description, role);
}

public sealed record PlannedFileEntry(string Path, AgentPhase Phase, string Description, string ImplementerRole);
