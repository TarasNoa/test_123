using Libr4.Payments.Domain.Escrow;
using Libr4.Payments.Domain.Exceptions;
using Libr4.Payments.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Libr4.Payments.Infrastructure.Services;

/// <summary>
/// Real escrow service implementation with RBAC and validation
/// Replaces TODO stubs with actual business logic
/// </summary>
public class EscrowService : IEscrowService
{
    private readonly IEscrowRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<EscrowService> _logger;

    public EscrowService(
        IEscrowRepository repository,
        IUserRepository userRepository,
        ILogger<EscrowService> logger)
    {
        _repository = repository;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Release funds from escrow with real validation and RBAC
    /// </summary>
    public async Task<bool> ReleaseFundsAsync(Guid escrowId, Guid adminId, CancellationToken cancellationToken = default)
    {
        // 1. REAL VALIDATION (Instead of TODO)
        var escrow = await _repository.GetByIdAsync(escrowId, cancellationToken);
        if (escrow == null)
        {
            _logger.LogWarning("Escrow release failed: Escrow {EscrowId} not found", escrowId);
            throw new EscrowNotFoundException(escrowId);
        }

        // 2. RBAC Check (Real security)
        var admin = await _userRepository.GetByIdAsync(adminId, cancellationToken);
        if (admin == null || !admin.IsAdmin)
        {
            _logger.LogWarning("Unauthorized escrow release attempt by {AdminId} for escrow {EscrowId}", adminId, escrowId);
            return false;
        }

        // 3. Business logic validation
        if (escrow.Status != EscrowStatus.Held)
        {
            _logger.LogWarning("Cannot release escrow {EscrowId}: invalid status {Status}", escrowId, escrow.Status);
            return false;
        }

        // 4. Release funds
        escrow.Release();
        await _repository.UpdateAsync(escrow, cancellationToken);

        _logger.LogInformation("Funds released for escrow {EscrowId} by admin {AdminId}", escrowId, adminId);
        return true;
    }

    /// <summary>
    /// Refund escrow to client
    /// </summary>
    public async Task<bool> RefundAsync(Guid escrowId, Guid adminId, string reason, CancellationToken cancellationToken = default)
    {
        var escrow = await _repository.GetByIdAsync(escrowId, cancellationToken);
        if (escrow == null)
        {
            throw new EscrowNotFoundException(escrowId);
        }

        var admin = await _userRepository.GetByIdAsync(adminId, cancellationToken);
        if (admin == null || !admin.IsAdmin)
        {
            _logger.LogWarning("Unauthorized refund attempt by {AdminId} for escrow {EscrowId}", adminId, escrowId);
            return false;
        }

        if (escrow.Status != EscrowStatus.Held)
        {
            _logger.LogWarning("Cannot refund escrow {EscrowId}: invalid status {Status}", escrowId, escrow.Status);
            return false;
        }

        escrow.Refund();
        await _repository.UpdateAsync(escrow, cancellationToken);

        _logger.LogInformation("Escrow {EscrowId} refunded by admin {AdminId}. Reason: {Reason}", escrowId, adminId, reason);
        return true;
    }

    /// <summary>
    /// Create a new escrow
    /// </summary>
    public async Task<Escrow> CreateAsync(Guid taskId, Guid clientId, Guid freelancerId, decimal amount, string stripePaymentIntentId, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new DomainException("Amount must be positive");
        }

        var escrow = Escrow.Create(
            Guid.NewGuid(),
            taskId,
            clientId,
            freelancerId,
            amount,
            "USD",
            stripePaymentIntentId);

        await _repository.AddAsync(escrow, cancellationToken);

        _logger.LogInformation("Created escrow {EscrowId} for task {TaskId}, amount: {Amount}", escrow.Id, taskId, amount);
        return escrow;
    }
}

/// <summary>
/// Escrow service interface
/// </summary>
public interface IEscrowService
{
    Task<bool> ReleaseFundsAsync(Guid escrowId, Guid adminId, CancellationToken cancellationToken = default);
    Task<bool> RefundAsync(Guid escrowId, Guid adminId, string reason, CancellationToken cancellationToken = default);
    Task<Escrow> CreateAsync(Guid taskId, Guid clientId, Guid freelancerId, decimal amount, string stripePaymentIntentId, CancellationToken cancellationToken = default);
}
