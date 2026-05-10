using System;
using System.Threading;
using System.Threading.Tasks;
using Libr4.Shared.Kernel.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Libr4.Shared.Infrastructure.Messaging;

public class QueryBus : IQueryBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueryBus> _logger;

    public QueryBus(IServiceProvider serviceProvider, ILogger<QueryBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>)
            .MakeGenericType(query.GetType(), typeof(TResult));

        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            _logger.LogError($"No handler registered for query type: {query.GetType().Name}");
            throw new InvalidOperationException($"No handler registered for query type: {query.GetType().Name}");
        }

        var method = handlerType.GetMethod("HandleAsync", new[] { query.GetType(), typeof(CancellationToken) });

        if (method == null)
        {
            _logger.LogError($"Handler does not implement HandleAsync method: {handlerType.Name}");
            throw new InvalidOperationException($"Handler does not implement HandleAsync method: {handlerType.Name}");
        }

        var result = (Task<TResult>)method.Invoke(handler, new object[] { query, cancellationToken });

        return await result;
    }
}

public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
