using System.Threading;
using System.Threading.Tasks;

namespace Libr4.Shared.Kernel.Application;

public static class QueryBusExtensions
{
    public static Task<TResult> SendAsync<TQuery, TResult>(this IQueryBus queryBus, TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        return queryBus.Send<TResult>(query, cancellationToken);
    }
}
