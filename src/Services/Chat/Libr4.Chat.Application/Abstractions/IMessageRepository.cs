using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Libr4.Chat.Domain.Messages;

namespace Libr4.Chat.Application.Abstractions;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Message>> GetByChatIdAsync(Guid chatId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(Message message, CancellationToken cancellationToken = default);
    Task UpdateAsync(Message message, CancellationToken cancellationToken = default);
}
