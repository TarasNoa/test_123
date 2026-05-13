using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Application.Calls;
using Libr4.Chat.Application.Chats;
using Libr4.Chat.Application.Files;
using Libr4.Chat.Application.Messages;

namespace Libr4.Chat.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddChatApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ICallService, CallService>();

        return services;
    }
}
