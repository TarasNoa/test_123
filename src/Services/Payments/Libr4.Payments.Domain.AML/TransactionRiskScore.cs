using Libr4.Shared.Kernel.Domain;

namespace Libr4.Payments.Domain.AML;

public class TransactionRiskScore : AggregateRoot<Guid>
{
    public Guid TransactionId { get; private set; }
    public Guid UserId { get; private set; }

    // Risk scores (0-100)
    public float OverallRiskScore { get; private set; }
    public float? AmlRiskScore { get; private set; }
    public float? FraudRiskScore { get; private set; }
    public float? VelocityRiskScore { get; private set; }
    public float? PatternRiskScore { get; private set; }

    // Risk level
    public RiskLevel RiskLevel { get; private set; }

    // Risk factors
    public List<string> RiskFactors { get; private set; } = new();

    // AI analysis
    public string? AiModelVersion { get; private set; }
    public float? AiConfidence { get; private set; }
    public string? AiReasoning { get; private set; }

    // Decision
    public bool RequiresManualReview { get; private set; }
    public bool IsAutoApproved { get; private set; }
    public bool IsBlocked { get; private set; }

    // Monitoring
    public string MonitoringLevel { get; private set; } = "standard";

    public DateTimeOffset CreatedAt { get; private set; }

    private TransactionRiskScore() { }

    public static TransactionRiskScore Create(
        Guid transactionId,
        Guid userId,
        float overallRiskScore,
        RiskLevel riskLevel,
        DateTimeOffset now)
    {
        return new TransactionRiskScore
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            UserId = userId,
            OverallRiskScore = overallRiskScore,
            RiskLevel = riskLevel,
            CreatedAt = now
        };
    }

    public void UpdateRiskScores(
        float? amlRiskScore,
        float? fraudRiskScore,
        float? velocityRiskScore,
        float? patternRiskScore,
        DateTimeOffset now)
    {
        AmlRiskScore = amlRiskScore;
        FraudRiskScore = fraudRiskScore;
        VelocityRiskScore = velocityRiskScore;
        PatternRiskScore = patternRiskScore;
        CreatedAt = now;
    }

    public void AddRiskFactor(string factor)
    {
        if (!RiskFactors.Contains(factor))
            RiskFactors.Add(factor);
    }

    public void SetAiAnalysis(string modelVersion, float confidence, string reasoning, DateTimeOffset now)
    {
        AiModelVersion = modelVersion;
        AiConfidence = confidence;
        AiReasoning = reasoning;
        CreatedAt = now;
    }

    public void SetManualReview(bool requiresReview, DateTimeOffset now)
    {
        RequiresManualReview = requiresReview;
        CreatedAt = now;
    }

    public void SetAutoApproved(bool approved, DateTimeOffset now)
    {
        IsAutoApproved = approved;
        CreatedAt = now;
    }

    public void SetBlocked(bool blocked, DateTimeOffset now)
    {
        IsBlocked = blocked;
        CreatedAt = now;
    }

    public void SetMonitoringLevel(string level, DateTimeOffset now)
    {
        MonitoringLevel = level;
        CreatedAt = now;
    }
}
