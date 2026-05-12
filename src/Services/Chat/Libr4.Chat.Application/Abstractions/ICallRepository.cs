using System;
using System.Threading;
using System.Threading.Tasks;
using Libr4.Chat.Domain.Calls;

namespace Libr4.Chat.Application.Abstractions;

public interface ICallRepository
{
    Task<Call?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Call?> GetActiveByChatIdAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task AddAsync(Call call, CancellationToken cancellationToken = default);
    Task UpdateAsync(Call call, CancellationToken cancellationToken = default);
}
