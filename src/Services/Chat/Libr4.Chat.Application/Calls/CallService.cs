using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Domain.Calls;

namespace Libr4.Chat.Application.Calls;

public class CallService : ICallService
{
    private readonly ICallRepository _callRepository;

    public CallService(ICallRepository callRepository)
    {
        _callRepository = callRepository;
    }

    public async Task<CallDto> InitiateCallAsync(InitiateCallRequest request, Guid initiatorId, CancellationToken cancellationToken = default)
    {
        var call = Call.Initiate(request.ChatId, initiatorId, request.Type);
        await _callRepository.AddAsync(call, cancellationToken);

        return new CallDto(call.Id, call.ChatId, call.InitiatorId, call.Type, call.Status, call.StartedAt,
            call.Participants.Select(p => new CallParticipantDto(p.UserId, p.Status)).ToList());
    }

    public async Task JoinCallAsync(Guid callId, Guid userId, CancellationToken cancellationToken = default)
    {
        var call = await _callRepository.GetByIdAsync(callId, cancellationToken);
        if (call != null && call.Status == CallStatus.Ringing)
        {
            call.AddParticipant(userId);
            await _callRepository.UpdateAsync(call, cancellationToken);
        }
    }

    public async Task EndCallAsync(Guid callId, CancellationToken cancellationToken = default)
    {
        var call = await _callRepository.GetByIdAsync(callId, cancellationToken);
        if (call != null)
        {
            call.EndCall();
            await _callRepository.UpdateAsync(call, cancellationToken);
        }
    }

    public async Task<CallDto?> GetActiveCallAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        var call = await _callRepository.GetActiveByChatIdAsync(chatId, cancellationToken);
        return call == null ? null : new CallDto(call.Id, call.ChatId, call.InitiatorId, call.Type, call.Status, call.StartedAt,
            call.Participants.Select(p => new CallParticipantDto(p.UserId, p.Status)).ToList());
    }
}