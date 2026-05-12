using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatEntity = Libr4.Chat.Domain.Chats.Chat;

namespace Libr4.Chat.Application.Abstractions;

public interface IChatRepository
{
    Task<ChatEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ChatEntity>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(ChatEntity chat, CancellationToken cancellationToken = default);
    Task UpdateAsync(ChatEntity chat, CancellationToken cancellationToken = default);
}
