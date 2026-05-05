using FluentValidation;
using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Application.Dtos;
using Libr4.Payments.Domain.PaymentMethods;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Payments.Application.PaymentMethods.Commands;

public record AddPaymentMethodCommand(
    Guid UserId,
    string StripePaymentMethodId,
    string Last4,
    string Brand,
    int ExpMonth,
    int ExpYear,
    bool SetAsDefault = false) : IRequest<Result<PaymentMethodDto>>;

public class AddPaymentMethodValidator : AbstractValidator<AddPaymentMethodCommand>
{
    public AddPaymentMethodValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.StripePaymentMethodId).NotEmpty();
        RuleFor(x => x.Last4).NotEmpty().Length(4);
        RuleFor(x => x.Brand).NotEmpty();
        RuleFor(x => x.ExpMonth).GreaterThanOrEqualTo(1).LessThanOrEqualTo(12);
        RuleFor(x => x.ExpYear).GreaterThan(2023);
    }
}

public class AddPaymentMethodHandler : IRequestHandler<AddPaymentMethodCommand, Result<PaymentMethodDto>>
{
    private readonly IPaymentsDbContext _dbContext;

    public AddPaymentMethodHandler(IPaymentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaymentMethodDto>> Handle(AddPaymentMethodCommand request, CancellationToken ct)
    {
        // If setting as default, remove default from other cards
        if (request.SetAsDefault)
        {
            var existingDefaults = await _dbContext.PaymentMethods
                .Where(pm => pm.UserId == request.UserId && pm.IsDefault)
                .ToListAsync(ct);

            foreach (var pm in existingDefaults)
            {
                pm.RemoveDefault();
            }
        }

        var paymentMethod = PaymentMethod.CreateCard(
            Guid.NewGuid(),
            request.UserId,
            request.StripePaymentMethodId,
            request.Last4,
            request.Brand,
            request.ExpMonth,
            request.ExpYear,
            request.SetAsDefault);

        _dbContext.PaymentMethods.Add(paymentMethod);
        await _dbContext.SaveChangesAsync(ct);

        var dto = new PaymentMethodDto(
            paymentMethod.Id,
            paymentMethod.Type.ToString(),
            paymentMethod.Last4,
            paymentMethod.Brand,
            paymentMethod.ExpMonth,
            paymentMethod.ExpYear,
            paymentMethod.IsDefault);

        return Result.Success(dto);
    }
}
