using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class JavaSpringCompileRemediationTests
{
    [Fact]
    public void Apply_RepairsRepositoryMethodsDeclaredOutsideInterface()
    {
        var plan = new GenerationPlan(
            "Bank",
            "banking",
            StackPlanHeuristics.CreateJavaReactFullStackTechStack(null),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "eclipse-temurin:21-jdk",
            new[] { "cd backend && mvn -B -ntp -DskipTests package" },
            Array.Empty<string>(),
            5);

        var files = new List<GeneratedFile>
        {
            new(
                "backend/src/main/java/com/generated/banking/repository/AccountRepository.java",
                "java",
                """
                package com.generated.banking.repository;

                import com.generated.banking.model.Account;
                import org.springframework.data.jpa.repository.JpaRepository;
                import org.springframework.stereotype.Repository;

                    java.util.Optional<Account> findByAccountNumber(String accountNumber);
                    java.util.List<Account> findByUserId(Long userId);
                    boolean existsByAccountNumber(String accountNumber);

                @Repository
                public interface AccountRepository extends JpaRepository<Account, Long> {
                }
                """),
            new("frontend/package.json", "json", """{"name":"bank-frontend","dependencies":{"react":"^18.0.0"}}"""),
            new(
                "backend/pom.xml",
                "xml",
                """
                <project>
                  <modelVersion>4.0.0</modelVersion>
                  <groupId>com.generated</groupId>
                  <artifactId>backend</artifactId>
                  <parent>
                    <groupId>org.springframework.boot</groupId>
                    <artifactId>spring-boot-starter-parent</artifactId>
                    <version>3.3.5</version>
                  </parent>
                </project>
                """)
        };

        var changed = JavaSpringCompileRemediation.Apply(files, plan, Array.Empty<ErrorReport>());
        changed.Should().BeGreaterThan(0);

        var repo = files.Single(f => f.RelativePath.EndsWith("AccountRepository.java", StringComparison.OrdinalIgnoreCase));
        repo.Content.Should().Contain("public interface AccountRepository");
        repo.Content.Should().Contain("findByAccountNumber(String accountNumber);");
        repo.Content.Should().NotMatchRegex(@"import[\s\S]*findByAccountNumber[\s\S]*@Repository");
    }
}
