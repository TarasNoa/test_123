using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Infrastructure.Persistence;

public interface IAppGenerationEntityRepository
{
    Task<AppGeneration?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(AppGeneration generation, CancellationToken ct = default);
    Task<IReadOnlyList<AppGeneration>> GetByUserAsync(Guid userId, int skip = 0, int take = 20, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
