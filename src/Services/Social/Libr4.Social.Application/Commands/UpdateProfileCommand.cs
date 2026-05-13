using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Social.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.Social.Application.Commands;

public class UpdateProfileCommand
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Location { get; set; }
}

public class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand>
{
    private readonly ISocialNetworkRepository _repository;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;

    public UpdateProfileCommandHandler(ISocialNetworkRepository repository, ILogger<UpdateProfileCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(UpdateProfileCommand command)
    {
        var network = await _repository.GetByUserIdAsync(command.UserId);
        if (network == null)
            throw new InvalidOperationException("User network not found");

        network.UpdateProfile(command.Name, command.Bio, command.ProfileImageUrl, command.Location);
        await _repository.UpdateAsync(network);

        _logger.LogInformation("Profile updated for user {UserId}", command.UserId);
    }
}
