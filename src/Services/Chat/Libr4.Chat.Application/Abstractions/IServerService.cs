using System;
using System.Collections.Generic;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Libr4.Chat.Domain.Calls;
using Libr4.Chat.Domain.Servers;

namespace Libr4.Chat.Application.Abstractions;

public record ServerDto(
    Guid Id,
    string Name,
    Guid OwnerId,
    List<ChannelDto> Channels,
    List<ServerMemberDto> Members,
    DateTimeOffset CreatedAt);

public record ChannelDto(Guid Id, string Name, ChannelType Type);
public record ServerMemberDto(Guid UserId, ServerRole Role);

public record CreateServerRequest(string Name);
public record CreateChannelRequest(Guid ServerId, string Name, ChannelType Type);
public record ScheduleCallRequest(Guid ServerId, string Title, DateTimeOffset ScheduledAt, CallType Type);

public interface IServerService
{
    Task<List<ServerDto>> GetUserServersAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ServerDto> CreateServerAsync(CreateServerRequest request, Guid ownerId, CancellationToken cancellationToken = default);
    Task AddChannelAsync(CreateChannelRequest request, CancellationToken cancellationToken = default);
    Task ScheduleCallAsync(ScheduleCallRequest request, Guid organizerId, CancellationToken cancellationToken = default);
}