using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Libr4.Chat.Domain.Calls;

namespace Libr4.Chat.Application.Abstractions;

public record CallDto(
    Guid Id,
    Guid ChatId,
    Guid InitiatorId,
    CallType Type,
    CallStatus Status,
    DateTimeOffset StartedAt,
    List<CallParticipantDto> Participants);

public record CallParticipantDto(Guid UserId, CallParticipantStatus Status);

public record InitiateCallRequest(Guid ChatId, CallType Type);

public interface ICallService
{
    Task<CallDto> InitiateCallAsync(InitiateCallRequest request, Guid initiatorId, CancellationToken cancellationToken = default);
    Task JoinCallAsync(Guid callId, Guid userId, CancellationToken cancellationToken = default);
    Task EndCallAsync(Guid callId, CancellationToken cancellationToken = default);
    Task<CallDto?> GetActiveCallAsync(Guid chatId, CancellationToken cancellationToken = default);
}