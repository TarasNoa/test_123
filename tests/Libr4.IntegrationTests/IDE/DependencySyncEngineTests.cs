using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class DependencySyncEngineTests
{
    [Fact]
    public void SyncPackageJsonDependencies_AddsMissingRuntimePackage()
    {
        var files = new List<GeneratedFile>
        {
            new("backend/package.json", "json", """{"name":"api","dependencies":{"express":"^4.21.0"}}"""),
            new("backend/src/auth.js", "javascript", """
                import jwt from "jsonwebtoken";
                import dotenv from "dotenv";
                """)
        };

        DependencySyncEngine.SyncPackageJsonDependencies(files).Should().Be(1);
        files[0].Content.Should().Contain("jsonwebtoken");
        files[0].Content.Should().Contain("dotenv");
    }

    [Fact]
    public void Classifier_MapsCannotFindModuleToMissingDependency()
    {
        var errors = new[]
        {
            new ErrorReport("BuildError", "Cannot find module 'jsonwebtoken'", "backend/src/auth.js")
        };

        var classified = RepairErrorClassifier.Classify(errors, "Cannot find module 'jsonwebtoken'");
        classified[0].Class.Should().Be(RepairErrorClassifier.RepairErrorClass.MissingDependency);
        classified[0].Tier.Should().Be(RepairErrorClassifier.RepairTier.Level1BuildManifest);
        RepairErrorClassifier.ShouldSkipLlmFixer(classified).Should().BeTrue();
    }
}
