using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Libr4.Trading.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
