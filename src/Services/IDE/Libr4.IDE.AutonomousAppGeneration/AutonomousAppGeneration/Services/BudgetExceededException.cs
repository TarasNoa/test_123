namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed class BudgetExceededException : InvalidOperationException
{
    public BudgetExceededException(string reason)
        : base($"budget_exceeded:{reason}")
    {
        Reason = reason;
    }

    public string Reason { get; }
}
