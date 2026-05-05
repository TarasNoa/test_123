using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Libr4.Payments.Infrastructure.Persistence;

public class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PaymentsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=libr4_payments;Username=libr4;Password=libr4_dev_password");
        return new PaymentsDbContext(optionsBuilder.Options, null!);
    }
}
