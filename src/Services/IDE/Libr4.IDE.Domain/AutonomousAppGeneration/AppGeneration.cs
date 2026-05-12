namespace Libr4.IDE.Domain.AutonomousAppGeneration;

public class AppGeneration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Status { get; set; } = "pending";
    public string? ErrorMessage { get; set; }
    public string ConfigurationJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
