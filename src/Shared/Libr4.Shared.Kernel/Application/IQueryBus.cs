using System.Threading;
using System.Threading.Tasks;

namespace Libr4.Shared.Kernel.Application;

public interface IQueryBus
{
    Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}

public interface IQuery<TResult>
{
}
