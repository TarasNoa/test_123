using Xunit;
using Moq;
using Libr4.Chat.Application.Chats;
using Libr4.Chat.Domain.Chats;
using Microsoft.Extensions.Logging;

namespace Libr4.Chat.Tests.UnitTests;

public class ChatServiceTests
{
    private readonly Mock<IChatRepository> _chatRepositoryMock;
    private readonly Mock<IMessageRepository> _messageRepositoryMock;
    private readonly Mock<ILogger<ChatService>> _loggerMock;
    private readonly ChatService _service;

    public ChatServiceTests()
    {
        _chatRepositoryMock = new Mock<IChatRepository>();
        _messageRepositoryMock = new Mock<IMessageRepository>();
        _loggerMock = new Mock<ILogger<ChatService>>();
        _service = new ChatService(_chatRepositoryMock.Object, _messageRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateChatAsync_CreatesChatSuccessfully()
    {
        // Arrange
        var request = new CreateChatRequest("Test Chat", ChatType.Group, new List<Guid> { Guid.NewGuid() });
        var creatorId = Guid.NewGuid();

        // Act
        var result = await _service.CreateChatAsync(request, creatorId);

        // Assert
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Type, result.Type);
        _chatRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Chat>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // More tests...
}