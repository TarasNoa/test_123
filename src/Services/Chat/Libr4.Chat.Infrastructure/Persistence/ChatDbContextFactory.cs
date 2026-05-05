using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Libr4.Chat.Infrastructure.Persistence;

public class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ChatDbContext>();
        var connectionString = "Host=localhost;Port=5432;Database=libr4_chat;Username=libr4;Password=libr4_dev_password";
        optionsBuilder.UseNpgsql(connectionString);
        return new ChatDbContext(optionsBuilder.Options);
    }
}
