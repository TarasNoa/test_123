using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;
using static Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.CompileErrorAnalyzer;

namespace Libr4.IntegrationTests.IDE;

public sealed class CompileErrorIntelligenceTests
{
    private static GenerationPlan JavaBankPlan() =>
        StackPlanHeuristics.AlignJavaReactFullStackPlan(
            new GenerationPlan(
                "BankCore",
                "banking",
                StackPlanHeuristics.CreateJavaReactFullStackTechStack(),
                Array.Empty<GenerationPhase>(),
                Array.Empty<string>(),
                "eclipse-temurin:21-jdk",
                Array.Empty<string>(),
                Array.Empty<string>(),
                6),
            "java react banking");

    [Fact]
    public void Analyze_MavenCannotFindSymbol_MissingClass_UserService()
    {
        var plan = JavaBankPlan();
        var files = new List<GeneratedFile>
        {
            new("backend/pom.xml", "xml", "<project><modelVersion>4.0.0</modelVersion></project>"),
            new(
                "backend/src/main/java/com/bankcore/controller/TransactionController.java",
                "java",
                """
                package com.bankcore.controller;
                public class TransactionController {
                    private final UserService userService;
                }
                """)
        };

        var buildLog = """
            [ERROR] backend/src/main/java/com/bankcore/controller/TransactionController.java:[4,19] cannot find symbol
              symbol:   class UserService
              location: package com.bankcore.service
            """;

        var root = new ErrorReport(
            "CompileError",
            "cannot find symbol",
            "fix",
            "backend/src/main/java/com/bankcore/controller/TransactionController.java",
            4);

        var analysis = CompileErrorAnalyzer.Analyze(root, buildLog, files, plan);

        analysis.Should().NotBeNull();
        analysis!.Kind.Should().Be(CompileFixKind.MissingClass);
        analysis.SymbolName.Should().Be("UserService");
        analysis.ExpectedPackage.Should().Be("com.bankcore.service");
        analysis.TargetFilePath.Should().Be("backend/src/main/java/com/bankcore/service/UserService.java");
    }

    [Fact]
    public void JavaCompileSymbolRemediation_CreatesMissingUserService()
    {
        var plan = JavaBankPlan();
        var files = new List<GeneratedFile>
        {
            new("backend/pom.xml", "xml", "<project><modelVersion>4.0.0</modelVersion></project>"),
            new(
                "backend/src/main/java/com/bankcore/controller/TransactionController.java",
                "java",
                "package com.bankcore.controller; public class TransactionController {}")
        };

        var analysis = new CompileErrorAnalysis(
            CompileFixKind.MissingClass,
            "UserService",
            "class",
            "com.bankcore.service",
            "backend/src/main/java/com/bankcore/controller/TransactionController.java",
            4,
            "backend/src/main/java/com/bankcore/service/UserService.java",
            "create",
            "test");

        JavaCompileSymbolRemediation.Apply(files, plan, analysis).Should().Be(1);
        files.Should().Contain(f =>
            f.RelativePath.Equals("backend/src/main/java/com/bankcore/service/UserService.java", StringComparison.OrdinalIgnoreCase));
        files.First(f => f.RelativePath.Contains("UserService")).Content!
            .Should().Contain("package com.bankcore.service")
            .And.Contain("@Service")
            .And.Contain("public class UserService");
    }

    [Fact]
    public void Analyze_WrongImport_DetectsIncorrectFqcn()
    {
        var plan = JavaBankPlan();
        var files = new List<GeneratedFile>
        {
            new(
                "backend/src/main/java/com/bankcore/service/UserService.java",
                "java",
                "package com.bankcore.service; public class UserService {}"),
            new(
                "backend/src/main/java/com/bankcore/controller/TransactionController.java",
                "java",
                """
                package com.bankcore.controller;
                import com.bank.service.UserService;
                public class TransactionController {
                    private final UserService userService;
                }
                """)
        };

        var buildLog = """
            [ERROR] backend/src/main/java/com/bankcore/controller/TransactionController.java:[5,19] cannot find symbol
              symbol:   class UserService
              location: package com.bankcore.service
            """;

        var root = new ErrorReport(
            "CompileError",
            buildLog,
            "fix",
            "backend/src/main/java/com/bankcore/controller/TransactionController.java");

        var analysis = CompileErrorAnalyzer.Analyze(root, buildLog, files, plan);

        analysis.Should().NotBeNull();
        analysis!.Kind.Should().BeOneOf(CompileFixKind.WrongImport, CompileFixKind.MissingImport);
    }

    [Fact]
    public void JavaCompileSymbolRemediation_FixesWrongImport()
    {
        var plan = JavaBankPlan();
        var files = new List<GeneratedFile>
        {
            new(
                "backend/src/main/java/com/bankcore/service/UserService.java",
                "java",
                "package com.bankcore.service; public class UserService {}"),
            new(
                "backend/src/main/java/com/bankcore/controller/TransactionController.java",
                "java",
                """
                package com.bankcore.controller;
                import com.bank.service.UserService;
                public class TransactionController {}
                """)
        };

        var analysis = new CompileErrorAnalysis(
            CompileFixKind.WrongImport,
            "UserService",
            "class",
            "com.bankcore.service",
            "backend/src/main/java/com/bankcore/controller/TransactionController.java",
            null,
            "backend/src/main/java/com/bankcore/controller/TransactionController.java",
            "fix import",
            "test");

        JavaCompileSymbolRemediation.Apply(files, plan, analysis).Should().Be(1);
        files.First(f => f.RelativePath.Contains("TransactionController")).Content!
            .Should().Contain("import com.bankcore.service.UserService;")
            .And.NotContain("import com.bank.service.UserService;");
    }

    [Fact]
    public void Analyze_PackageMismatch_WhenDeclarationDiffersFromPath()
    {
        var plan = JavaBankPlan();
        var files = new List<GeneratedFile>
        {
            new(
                "backend/src/main/java/com/bankcore/service/UserService.java",
                "java",
                "package com.bank.services; public class UserService {}")
        };

        var buildLog = """
            [ERROR] backend/src/main/java/com/bankcore/service/UserService.java:[1,1] cannot find symbol
              symbol:   class UserService
              location: package com.bankcore.service
            """;

        var root = new ErrorReport("CompileError", buildLog, "fix", files[0].RelativePath);
        var analysis = CompileErrorAnalyzer.Analyze(root, buildLog, files, plan);

        analysis.Should().NotBeNull();
        analysis!.Kind.Should().Be(CompileFixKind.PackageMismatch);
        analysis.ExpectedPackage.Should().Be("com.bankcore.service");
    }

    [Fact]
    public void CompileRepairPlanner_UsesMissingClassCategory()
    {
        var plan = JavaBankPlan();
        var files = new List<GeneratedFile>
        {
            new(
                "backend/src/main/java/com/bankcore/controller/TransactionController.java",
                "java",
                "package com.bankcore.controller; public class TransactionController { UserService s; }")
        };

        var logs = new List<ConsoleLogEntry>
        {
            new(DateTime.UtcNow, "stderr", """
                [ERROR] backend/src/main/java/com/bankcore/controller/TransactionController.java:[1,50] cannot find symbol
                  symbol:   class UserService
                  location: package com.bankcore.service
                """)
        };
        var execution = new ExecutionResult(false, 1, TimeSpan.Zero, logs);
        var errors = new List<ErrorReport>
        {
            new("CompileError", "cannot find symbol class UserService", "create", files[0].RelativePath)
        };

        var repair = CompileRepairPlanner.BuildPlan(execution, files, errors, plan);

        repair.RootCauseCategory.Should().Be("missing_class");
        repair.SymbolAnalysis.Should().NotBeNull();
        repair.SymbolAnalysis!.Kind.Should().Be(CompileFixKind.MissingClass);
    }

    [Fact]
    public void CompileSymbolRecovery_AppliesBeforeLlm()
    {
        var plan = JavaBankPlan();
        var files = new List<GeneratedFile>
        {
            new(
                "backend/src/main/java/com/bankcore/controller/TransactionController.java",
                "java",
                "package com.bankcore.controller; public class TransactionController { UserService s; }")
        };

        var logs = new List<ConsoleLogEntry>
        {
            new(DateTime.UtcNow, "stderr", """
                [ERROR] backend/src/main/java/com/bankcore/controller/TransactionController.java:[1,50] cannot find symbol
                  symbol:   class UserService
                  location: package com.bankcore.service
                """)
        };
        var execution = new ExecutionResult(false, 1, TimeSpan.Zero, logs);
        var errors = new List<ErrorReport>
        {
            new("CompileError", "cannot find symbol", "fix", files[0].RelativePath)
        };
        var repair = CompileRepairPlanner.BuildPlan(execution, files, errors, plan);
        var blob = string.Join('\n', logs.Select(l => l.Message));

        var patches = CompileSymbolRecovery.TryApply(files, plan, repair, blob);

        patches.Should().NotBeEmpty();
        patches.Should().Contain(p => p.RelativePath.Contains("UserService", StringComparison.OrdinalIgnoreCase));
        CompileSymbolRecovery.ShouldPreferDeterministic(repair.SymbolAnalysis).Should().BeTrue();
    }

    [Fact]
    public void RecoveryRootCauseMapper_MissingClass_MapsToMissingType()
    {
        RecoveryRootCauseMapper.FromCompileFixKind(CompileFixKind.MissingClass)
            .Should().Be(RecoveryRootCauseCategory.MissingType);
        RecoveryRootCauseMapper.FromPlannerCategory("missing_class")
            .Should().Be(RecoveryRootCauseCategory.MissingType);
        RecoveryRootCauseMapper.FromPlannerCategory("wrong_import")
            .Should().Be(RecoveryRootCauseCategory.Imports);
    }
}
