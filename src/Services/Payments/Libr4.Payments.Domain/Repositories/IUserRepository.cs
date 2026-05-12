namespace Libr4.Payments.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

public class User
{
    public Guid Id { get; set; }
    public bool IsAdmin { get; set; }
}
