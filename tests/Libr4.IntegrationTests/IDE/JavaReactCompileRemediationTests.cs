using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class JavaReactCompileRemediationTests
{
    [Fact]
    public void Apply_AddsFindByUserId_JwtArtifacts_AndNormalizesSetterNames()
    {
        var files = new List<GeneratedFile>
        {
            new(
                "backend/src/main/java/com/generated/app/repository/AccountRepository.java",
                "java",
                """
                package com.generated.app.repository;
                import com.generated.app.model.Account;
                import org.springframework.data.jpa.repository.JpaRepository;
                public interface AccountRepository extends JpaRepository<Account, Long> {
                    java.util.Optional<Account> findByAccountNumber(String accountNumber);
                }
                """),
            new(
                "backend/src/main/java/com/generated/app/BillingApplication.java",
                "java",
                """
                package com.generated.app;
                import org.springframework.boot.autoconfigure.SpringBootApplication;
                @SpringBootApplication
                public class BillingApplication {}
                """),
            new(
                "backend/src/main/java/com/generated/app/service/TransferService.java",
                "java",
                """
                package com.generated.app.service;
                public class TransferService {
                    void run() { transaction.setFromAccount(acc); accountRepository.findByUserId(1L); }
                }
                """),
            new(
                "backend/src/main/java/com/generated/app/service/AuthService.java",
                "java",
                """
                package com.generated.app.service;
                import com.generated.app.dto.AuthTokenResponse;
                import com.generated.app.security.JwtTokenProvider;
                public class AuthService {
                    private final JwtTokenProvider jwtTokenProvider;
                    public AuthTokenResponse login() { return new AuthTokenResponse("t","r","Bearer",1L,1L,"u",java.util.Set.of()); }
                }
                """),
            new(
                "backend/src/main/java/com/generated/app/model/User.java",
                "java",
                """
                package com.generated.app.model;
                public class User {
                    public String getUsername() { return "u"; }
                }
                """)
        };

        var plan = JavaReactPlan();
        var errors = new[]
        {
            new ErrorReport("CompileError", "cannot find symbol findByUserId", string.Empty, "AccountService.java"),
            new ErrorReport("CompileError", "cannot find symbol JwtTokenProvider", string.Empty, "AuthService.java"),
            new ErrorReport("CompileError", "cannot find symbol setFromAccount", string.Empty, "TransferService.java")
        };

        var changed = StackArtifactRecoveryRouter.ApplyCompileRecovery(files, plan, errors);

        changed.Should().BeGreaterThan(0);
        files.Should().Contain(f => f.RelativePath.Contains("JwtTokenProvider"));
        files.Should().Contain(f => f.RelativePath.Contains("AuthTokenResponse"));
        files.Single(f => f.RelativePath.Contains("AccountRepository")).Content
            .Should().Contain("findByUserId");
        files.Single(f => f.RelativePath.Contains("TransferService")).Content
            .Should().Contain("setSourceAccount")
            .And.NotContain("setFromAccount");
    }

    [Fact]
    public void Apply_SimplifiesBrokenRepositories_AndAlignsAuthController()
    {
        var files = new List<GeneratedFile>
        {
            new(
                "backend/src/main/java/com/generated/app/repository/OrderRepository.java",
                "java",
                """
                package com.generated.app.repository;
                import com.generated.app.model.Order;
                import org.springframework.data.jpa.repository.JpaRepository;
                public interface OrderRepository extends JpaRepository<Order, Long> {
                    java.util.List<Order> findByStatus(Order.OrderStatus status);
                }
                """),
            new(
                "backend/src/main/java/com/generated/app/service/PaymentService.java",
                "java",
                """
                package com.generated.app.service;
                public class PaymentService {
                    void run() {
                        request.fromAccountNumber();
                        transaction.setFromAccount(a);
                        transaction.setType("PAYMENT");
                    }
                }
                """),
            new(
                "backend/src/main/java/com/generated/app/config/SecurityConfig.java",
                "java",
                """
                package com.generated.app.config;
                public class SecurityConfig {
                    private final JwtAuthenticationFilter jwtAuthenticationFilter;
                }
                """),
            new(
                "backend/src/main/java/com/generated/app/service/AuthService.java",
                "java",
                "package com.generated.app.service; public class AuthService { private final UserRepository userRepository; }"),
            new(
                "backend/src/main/java/com/generated/app/web/AuthController.java",
                "java",
                """
                package com.generated.app.web;
                import com.generated.app.dto.AuthTokenRequest;
                import com.generated.app.service.AuthService;
                public class AuthController {
                    private final AuthService authService;
                    public AuthController(AuthService authService) { this.authService = authService; }
                    public Object getToken(AuthTokenRequest request) {
                        return authService.authenticate(request.username(), request.password());
                    }
                }
                """)
        };

        var changed = StackArtifactRecoveryRouter.ApplyCompileRecovery(files, JavaReactPlan(), Array.Empty<ErrorReport>());

        changed.Should().BeGreaterThan(0);
        files.Single(f => f.RelativePath.Contains("OrderRepository")).Content
            .Should().NotContain("Order.OrderStatus");
        files.Should().Contain(f => f.RelativePath.Contains("UserRepository"));
        files.Should().Contain(f => f.RelativePath.Contains("JwtAuthenticationFilter"));
        files.Single(f => f.RelativePath.Contains("PaymentService")).Content
            .Should().Contain("sourceAccountNumber")
            .And.Contain("setSourceAccount")
            .And.Contain("setTransactionType");
        files.Single(f => f.RelativePath.Contains("AuthController")).Content
            .Should().Contain("authService.authenticate(request)")
            .And.NotContain("request.password()");
    }

    private static GenerationPlan JavaReactPlan() =>
        new(
            "BillingApp",
            "Java Spring Boot backend + React TypeScript frontend",
            new TechStack(
                ["Java", "TypeScript"],
                ["Spring Boot", "React"],
                ["PostgreSQL"],
                [],
                "fullstack"),
            [],
            [],
            "eclipse-temurin:21-jdk",
            ["cd backend && mvn -q package"],
            [],
            6);
}
