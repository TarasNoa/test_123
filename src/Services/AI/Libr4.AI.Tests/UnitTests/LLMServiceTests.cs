// Улучшить тесты с NSubstitute
using NSubstitute;

[Fact]
public async Task GenerateCodeAsync_RetriesOnFailure()
{
    var providerMock = Substitute.For<ILLMProvider>();
    providerMock.GenerateTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromException<string>(new Exception("API Error")));

    var service = new LLMService(providerMock, Substitute.For<ILLMProvider>());

    await Assert.ThrowsAsync<Exception>(() => service.GenerateCodeAsync("test"));
    await providerMock.Received(3).GenerateTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()); // 2 retries + 1 initial
}