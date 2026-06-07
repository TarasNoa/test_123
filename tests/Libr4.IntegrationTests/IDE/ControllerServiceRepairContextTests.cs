using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ControllerServiceRepairContextTests
{
    [Fact]
    public void TryResolveAlignmentPair_FindsAccountControllerAndService()
    {
        var controller = new GeneratedFile(
            "backend/src/main/java/com/generated/banking/web/AccountController.java",
            "java",
            "class AccountController { void x() { service.getAccountByNumber(); } }");
        var service = new GeneratedFile(
            "backend/src/main/java/com/generated/banking/service/AccountService.java",
            "java",
            "class AccountService { void getAccountByAccountNumber() {} }");
        var files = new[] { controller, service };
        var errors = new[]
        {
            new ErrorReport("CompileError", "cannot find symbol getAccountByNumber", string.Empty, controller.RelativePath)
        };

        var ok = ControllerServiceRepairContext.TryResolveAlignmentPair(errors, files, out var pair);

        ok.Should().BeTrue();
        pair!.Controller.RelativePath.Should().Contain("AccountController");
        pair.Service.RelativePath.Should().Contain("AccountService");
    }
}
