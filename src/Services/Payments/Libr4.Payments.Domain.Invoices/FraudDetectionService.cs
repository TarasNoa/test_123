using Microsoft.ML;
using Microsoft.ML.Data;
using Libr4.Payments.Domain.Invoices;

namespace Libr4.Payments.Domain.Invoices;

public class FraudDetectionService
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    // private readonly IFraudHistoryRepository? _fraudHistory;  // temporarily disabled

    /// <summary>
    /// Creates service without fraud history (uses default values).
    /// </summary>
    public FraudDetectionService()
    {
        _mlContext = new MLContext();
    }

    /// <summary>
    /// Creates service with fraud history repository for enhanced detection (temporarily disabled).
    /// </summary>
    // public FraudDetectionService(IFraudHistoryRepository fraudHistory)
    // {
    //     _mlContext = new MLContext();
    //     _fraudHistory = fraudHistory;
    // }

    public class FraudFeatures
    {
        public float Amount { get; set; }
        public float IssuerId { get; set; }
        public float RecipientId { get; set; }
        public float DaysUntilDue { get; set; }
        public float IssuerAge { get; set; }
        public float PreviousFraudCount { get; set; }
    }

    public class FraudPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool IsFraud { get; set; }

        public float Probability { get; set; }
        public float Score { get; set; }
    }

    /// <summary>
    /// Detects fraud using rule-based analysis.
    /// Note: For database-backed fraud history, use DetectFraudAsync.
    /// </summary>
    [Obsolete("Use DetectFraudAsync for database integration")]
    public FraudAnalysisResult DetectFraud(Invoice invoice, Guid issuerId, Guid recipientId)
    {
        // Feature extraction
        var features = new FraudFeatures
        {
            Amount = (float)invoice.Total,
            IssuerId = (float)issuerId.GetHashCode(),
            RecipientId = (float)recipientId.GetHashCode(),
            DaysUntilDue = (float)(invoice.DueDate - DateTimeOffset.UtcNow).Days,
            IssuerAge = (float)(DateTimeOffset.UtcNow - invoice.CreatedAt).TotalDays,
            PreviousFraudCount = 0f
        };

        return AnalyzeFraud(invoice, features);
    }

    /// <summary>
    /// Detects fraud with async database lookup for fraud history.
    /// </summary>
    public async Task<FraudAnalysisResult> DetectFraudAsync(
        Invoice invoice, 
        Guid issuerId, 
        Guid recipientId, 
        CancellationToken ct = default)
    {
        // var previousFraudCount = _fraudHistory != null 
        //     ? await _fraudHistory.GetFraudCountAsync(issuerId, ct)
        //     : 0;
        var previousFraudCount = 0;  // temporarily disabled

        var features = new FraudFeatures
        {
            Amount = (float)invoice.Total,
            IssuerId = (float)issuerId.GetHashCode(),
            RecipientId = (float)recipientId.GetHashCode(),
            DaysUntilDue = (float)(invoice.DueDate - DateTimeOffset.UtcNow).Days,
            IssuerAge = (float)(DateTimeOffset.UtcNow - invoice.CreatedAt).TotalDays,
            PreviousFraudCount = previousFraudCount
        };

        return AnalyzeFraud(invoice, features);
    }

    private FraudAnalysisResult AnalyzeFraud(Invoice invoice, FraudFeatures features)
    {
        // Rule-based fraud detection
        var riskScore = CalculateRiskScore(invoice, features);
        var isFraud = riskScore > 0.75f;
        var primaryReason = GetPrimaryReason(riskScore, invoice, features);

        return new FraudAnalysisResult
        {
            IsFraud = isFraud,
            RiskScore = riskScore,
            PrimaryReason = primaryReason,
            Factors = GetRiskFactors(invoice, features)
        };
    }

    private float CalculateRiskScore(Invoice invoice, FraudFeatures features)
    {
        var score = 0f;

        // High amount risk
        if (invoice.Total > 10000m)
            score += 0.3f;
        else if (invoice.Total > 5000m)
            score += 0.15f;

        // Due date risk
        if (features.DaysUntilDue < 0)
            score += 0.2f;
        else if (features.DaysUntilDue < 7)
            score += 0.1f;

        // New issuer risk
        if (features.IssuerAge < 30)
            score += 0.15f;

        // Recipient risk (if same as issuer)
        if (invoice.IssuerId == invoice.RecipientId)
            score += 0.4f;

        // Previous fraud history risk
        if (features.PreviousFraudCount > 0)
            score += Math.Min(features.PreviousFraudCount * 0.15f, 0.45f);

        return Math.Min(score, 1f);
    }

    private string GetPrimaryReason(float riskScore, Invoice invoice, FraudFeatures features)
    {
        if (invoice.IssuerId == invoice.RecipientId)
            return "Self-invoicing detected";

        if (features.PreviousFraudCount >= 3)
            return "Multiple previous fraud incidents";

        if (features.PreviousFraudCount > 0)
            return "Previous fraud history detected";

        if (invoice.Total > 10000m)
            return "Unusually high invoice amount";

        if (features.DaysUntilDue < 0)
            return "Invoice is already overdue";

        if (features.IssuerAge < 30)
            return "New account with high-value invoice";

        return "Suspicious invoice pattern";
    }

    private List<string> GetRiskFactors(Invoice invoice, FraudFeatures features)
    {
        var factors = new List<string>();

        if (invoice.Total > 5000m)
            factors.Add($"High invoice amount: ${invoice.Total}");
        
        if (features.DaysUntilDue < 0)
            factors.Add("Invoice overdue");
        else if (features.DaysUntilDue < 7)
            factors.Add("Due date approaching");
        
        if (features.IssuerAge < 30)
            factors.Add("New account (< 30 days)");
        
        if (invoice.IssuerId == invoice.RecipientId)
            factors.Add("Self-invoicing");

        if (features.PreviousFraudCount > 0)
            factors.Add($"Previous fraud history: {features.PreviousFraudCount} incidents");

        return factors;
    }

    public PaymentPredictionResult PredictPaymentProbability(Invoice invoice)
    {
        var score = 0.7f; // Base score
        var factors = new List<string>();

        // Amount factor
        if (invoice.Total > 5000m)
        {
            score -= 0.1f;
            factors.Add("Large invoice amount");
        }

        // Due date factor
        var daysUntilDue = (invoice.DueDate - DateTimeOffset.UtcNow).Days;
        if (daysUntilDue < 0)
        {
            score -= 0.3f;
            factors.Add("Invoice overdue");
        }
        else if (daysUntilDue < 3)
        {
            score -= 0.1f;
            factors.Add("Due date approaching");
        }

        // Status factor
        if (invoice.Status == InvoiceStatus.Overdue)
        {
            score -= 0.2f;
            factors.Add("Invoice marked as overdue");
        }

        var probability = Math.Max(Math.Min(score, 1f), 0f);

        string overdueRisk;
        int estimatedDays;

        if (probability >= 0.75f)
        {
            overdueRisk = "low";
            estimatedDays = 5;
        }
        else if (probability >= 0.5f)
        {
            overdueRisk = "medium";
            estimatedDays = 10;
        }
        else
        {
            overdueRisk = "high";
            estimatedDays = 20;
        }

        return new PaymentPredictionResult
        {
            PaymentProbability = (float)Math.Round(probability, 2),
            OverdueRisk = overdueRisk,
            EstimatedPaymentDays = estimatedDays,
            Factors = factors
        };
    }
}

public class FraudAnalysisResult
{
    public bool IsFraud { get; set; }
    public float RiskScore { get; set; }
    public string PrimaryReason { get; set; } = string.Empty;
    public List<string> Factors { get; set; } = new();
}

public class PaymentPredictionResult
{
    public float PaymentProbability { get; set; }
    public string OverdueRisk { get; set; } = string.Empty;
    public int EstimatedPaymentDays { get; set; }
    public List<string> Factors { get; set; } = new();
}
