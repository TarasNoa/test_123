using Libr4.AI.Application;
using Libr4.AI.Application.Abstractions;
using Libr4.Shared.Kernel.Results;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Libr4.AI.Tests.UnitTests;

public class LLMServiceTests
{
    [Fact]
    public async Task GenerateCodeAsync_UsesPrimaryProvider()
    {
        var factoryMock = Substitute.For<ILLMProviderFactory>();
        var providerMock = Substitute.For<ILLMProvider>();
        factoryMock.GetProvider(Arg.Any<string>()).Returns(providerMock);

        var config = new ConfigurationBuilder().Build();
        var service = new LLMService(factoryMock, config);

        providerMock.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(new ChatCompletionResponse("code", 10))));

        var result = await service.GenerateCodeAsync("test");

        Assert.NotNull(result);
        await providerMock.Received(1).CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>());
    }
}