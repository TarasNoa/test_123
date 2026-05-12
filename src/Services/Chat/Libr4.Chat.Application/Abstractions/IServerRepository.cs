using System;
using System.Collections.Generic;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Libr4.Chat.Domain.Servers;

namespace Libr4.Chat.Application.Abstractions;

public interface IServerRepository
{
    Task<Server?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Server>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Server server, CancellationToken cancellationToken = default);
    Task UpdateAsync(Server server, CancellationToken cancellationToken = default);
}
