using System;
using System.Collections.Generic;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Domain.Servers;

namespace Libr4.Chat.Application.Servers;

public class ServerService : IServerService
{
    private readonly IServerRepository _serverRepository;

    public ServerService(IServerRepository serverRepository)
    {
        _serverRepository = serverRepository;
    }

    public async Task<List<ServerDto>> GetUserServersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var servers = await _serverRepository.GetByUserIdAsync(userId, cancellationToken);
        return servers.Select(s => new ServerDto(
            s.Id,
            s.Name,
            s.OwnerId,
            s.Channels.Select(c => new ChannelDto(c.Id, c.Name, c.Type)).ToList(),
            s.Members.Select(m => new ServerMemberDto(m.UserId, m.Role)).ToList(),
            s.CreatedAt)).ToList();
    }

    public async Task<ServerDto> CreateServerAsync(CreateServerRequest request, Guid ownerId, CancellationToken cancellationToken = default)
    {
        var server = Server.Create(request.Name, ownerId);
        server.AddMember(ownerId, ServerRole.Owner);
        await _serverRepository.AddAsync(server, cancellationToken);

        return new ServerDto(server.Id, server.Name, server.OwnerId, new List<ChannelDto>(), 
            server.Members.Select(m => new ServerMemberDto(m.UserId, m.Role)).ToList(), server.CreatedAt);
    }

    public async Task AddChannelAsync(CreateChannelRequest request, CancellationToken cancellationToken = default)
    {
        var server = await _serverRepository.GetByIdAsync(request.ServerId, cancellationToken);
        if (server == null) throw new InvalidOperationException("Server not found");

        server.AddChannel(request.Name, request.Type);
        await _serverRepository.UpdateAsync(server, cancellationToken);
    }

    public async Task ScheduleCallAsync(ScheduleCallRequest request, Guid organizerId, CancellationToken cancellationToken = default)
    {
        var server = await _serverRepository.GetByIdAsync(request.ServerId, cancellationToken);
        if (server == null) throw new InvalidOperationException("Server not found");

        server.ScheduleCall(request.Title, request.ScheduledAt, request.Type);
        await _serverRepository.UpdateAsync(server, cancellationToken);
    }
}