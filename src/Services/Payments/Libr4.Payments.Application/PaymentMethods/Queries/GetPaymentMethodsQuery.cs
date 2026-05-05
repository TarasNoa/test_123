using Libr4.Payments.Application.Abstractions;
using Libr4.Payments.Application.Dtos;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Payments.Application.PaymentMethods.Queries;

public record GetPaymentMethodsQuery(Guid UserId) : IRequest<Result<List<PaymentMethodDto>>>;

public class GetPaymentMethodsHandler : IRequestHandler<GetPaymentMethodsQuery, Result<List<PaymentMethodDto>>>
{
    private readonly IPaymentsDbContext _dbContext;

    public GetPaymentMethodsHandler(IPaymentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<PaymentMethodDto>>> Handle(GetPaymentMethodsQuery request, CancellationToken ct)
    {
        var methods = await _dbContext.PaymentMethods
            .Where(pm => pm.UserId == request.UserId)
            .OrderByDescending(pm => pm.IsDefault)
            .ThenBy(pm => pm.CreatedAt)
            .ToListAsync(ct);

        var dtos = methods.Select(pm => new PaymentMethodDto(
            pm.Id,
            pm.Type.ToString(),
            pm.Last4,
            pm.Brand,
            pm.ExpMonth,
            pm.ExpYear,
            pm.IsDefault)).ToList();

        return Result.Success(dtos);
    }
}
