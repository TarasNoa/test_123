using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Libr4.Shared.Infrastructure.Messaging;

public interface ICommandHandler<in TCommand> where TCommand : class
{
    Task Handle(TCommand command);
}

public interface ICommandHandler<in TCommand, TResult> where TCommand : class
{
    Task<TResult> Handle(TCommand command);
}

public interface ICommandBus
{
    Task SendAsync<TCommand>(TCommand command) where TCommand : class;
    Task<TResult> SendAsync<TCommand, TResult>(TCommand command) where TCommand : class;
}

public class CommandBus : ICommandBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandBus> _logger;

    public CommandBus(IServiceProvider serviceProvider, ILogger<CommandBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SendAsync<TCommand>(TCommand command) where TCommand : class
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(typeof(TCommand));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            _logger.LogWarning($"No handler found for command: {typeof(TCommand).Name}");
            return;
        }

        var handleMethod = handlerType.GetMethod("Handle");
        if (handleMethod != null)
        {
            await (Task)handleMethod.Invoke(handler, new object[] { command })!;
            _logger.LogInformation($"Command handled: {typeof(TCommand).Name}");
        }
    }

    public async Task<TResult> SendAsync<TCommand, TResult>(TCommand command) where TCommand : class
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(typeof(TCommand), typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            _logger.LogWarning($"No handler found for command: {typeof(TCommand).Name}");
            return default!;
        }

        var handleMethod = handlerType.GetMethod("Handle");
        if (handleMethod != null)
        {
            var result = await (Task<TResult>)handleMethod.Invoke(handler, new object[] { command })!;
            _logger.LogInformation($"Command handled with result: {typeof(TCommand).Name}");
            return result;
        }

        return default!;
    }
}