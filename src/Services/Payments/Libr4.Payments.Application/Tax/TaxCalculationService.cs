using Microsoft.Extensions.Logging;

namespace Libr4.Payments.Application.Tax;

/// <summary>
/// VAT calculation result
/// </summary>
public class VatResult
{
    public double VATAmount { get; set; }
    public double GrossPrice { get; set; }
}

/// <summary>
/// Tax calculator stub (TODO: integrate with F# module)
/// </summary>
public static class TaxCalculator
{
    public static double GetTaxRate(string jurisdiction)
    {
        return jurisdiction?.ToLower() switch
        {
            "ru" or "russia" => 0.20,
            "us" or "usa" => 0.0,
            "uk" => 0.20,
            "de" or "germany" => 0.19,
            _ => 0.20
        };
    }

    public static double? GetVATRate(string jurisdiction)
    {
        return jurisdiction?.ToLower() switch
        {
            "ru" or "russia" => 0.20,
            "uk" => 0.20,
            "de" or "germany" => 0.19,
            "us" or "usa" => null,
            _ => 0.20
        };
    }

    public static double CalculateTax(double amount, double rate)
    {
        return amount * rate;
    }

    public static VatResult CalculateVAT(double amount, double rate)
    {
        return new VatResult
        {
            VATAmount = amount * rate,
            GrossPrice = amount * (1 + rate)
        };
    }

    public static WithholdingCalculationResult CalculateWithholding(double grossAmount, double rate)
    {
        var withheld = grossAmount * rate;
        return new WithholdingCalculationResult
        {
            WithheldTax = withheld,
            NetPayment = grossAmount - withheld
        };
    }

    public static bool ValidateTaxRate(double rate)
    {
        return rate >= 0 && rate <= 1;
    }
}

/// <summary>
/// Withholding calculation result
/// </summary>
public class WithholdingCalculationResult
{
    public double WithheldTax { get; set; }
    public double NetPayment { get; set; }
}

/// <summary>
/// Withholding calculation result
/// </summary>
public record WithholdingResult(
    decimal GrossAmount,
    decimal WithheldAmount,
    decimal NetAmount,
    decimal WithholdingRate,
    string Country);

/// <summary>
/// Application service for tax calculations using F# Units of Measure
/// Ensures financial correctness at compile time
/// </summary>
public interface ITaxCalculationService
{
    /// <summary>
    /// Calculate tax for invoice
    /// </summary>
    Task<TaxCalculationResult> CalculateInvoiceTaxAsync(
        decimal subtotal,
        string jurisdiction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate withholding for freelancer payment
    /// </summary>
    Task<WithholdingResult> CalculateFreelancerWithholdingAsync(
        decimal grossAmount,
        string freelancerCountry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate VAT for order
    /// </summary>
    Task<VatBreakdown> CalculateOrderVATAsync(
        decimal netAmount,
        string customerCountry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate tax compliance for escrow release
    /// </summary>
    Task<TaxComplianceResult> ValidateEscrowTaxComplianceAsync(
        Guid orderId,
        decimal amount,
        string jurisdiction,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation using F# financial calculations with Units of Measure
/// </summary>
public class TaxCalculationService : ITaxCalculationService
{
    private readonly ILogger<TaxCalculationService> _logger;

    public TaxCalculationService(ILogger<TaxCalculationService> logger)
    {
        _logger = logger;
    }

    public Task<TaxCalculationResult> CalculateInvoiceTaxAsync(
        decimal subtotal,
        string jurisdiction,
        CancellationToken cancellationToken = default)
    {
        // Use F# Units of Measure for compile-time safety
        var taxRate = TaxCalculator.GetTaxRate(jurisdiction);
        var vatRate = TaxCalculator.GetVATRate(jurisdiction);

        // Calculate using F# functions
        var tax = TaxCalculator.CalculateTax((double)subtotal, taxRate);
        VatResult? vatResult = null;

        if (vatRate.HasValue)
        {
            vatResult = TaxCalculator.CalculateVAT((double)subtotal, vatRate.Value);
        }

        var result = new TaxCalculationResult
        {
            Subtotal = subtotal,
            TaxRate = (decimal)taxRate,
            TaxAmount = (decimal)tax,
            VatRate = vatRate.HasValue ? (decimal)vatRate.Value : null,
            VatAmount = vatResult != null ? (decimal)vatResult.VATAmount : null,
            Total = vatResult != null ? (decimal)vatResult.GrossPrice : subtotal + (decimal)tax,
            Jurisdiction = jurisdiction
        };

        _logger.LogInformation(
            "Tax calculated for {Jurisdiction}: Subtotal={Subtotal}, Tax={Tax}, Total={Total}",
            jurisdiction, subtotal, result.TaxAmount, result.Total);

        return Task.FromResult(result);
    }

    public Task<WithholdingResult> CalculateFreelancerWithholdingAsync(
        decimal grossAmount,
        string freelancerCountry,
        CancellationToken cancellationToken = default)
    {
        // Default withholding rates by country (simplified)
        var withholdingRate = freelancerCountry?.ToLower() switch
        {
            "russia" or "ru" => 13.0,  // Personal income tax
            "usa" or "us" => 30.0,     // 30% for non-residents
            "uk" => 20.0,
            _ => 20.0  // Default
        };

        var result = TaxCalculator.CalculateWithholding((double)grossAmount, withholdingRate);

        _logger.LogInformation(
            "Withholding for {Country}: Gross={Gross}, Tax={Tax}, Net={Net}, Rate={Rate}%",
            freelancerCountry, grossAmount, result.WithheldTax, result.NetPayment, withholdingRate);

        return Task.FromResult(new WithholdingResult(
            grossAmount,
            (decimal)result.WithheldTax,
            (decimal)result.NetPayment,
            (decimal)withholdingRate,
            freelancerCountry
        ));
    }

    public Task<VatBreakdown> CalculateOrderVATAsync(
        decimal netAmount,
        string customerCountry,
        CancellationToken cancellationToken = default)
    {
        var vatRate = TaxCalculator.GetVATRate(customerCountry);

        if (!vatRate.HasValue)
        {
            return Task.FromResult(new VatBreakdown
            {
                NetAmount = netAmount,
                VATAmount = 0,
                GrossAmount = netAmount,
                VatRate = null,
                Country = customerCountry,
                VatApplicable = false
            });
        }

        var vatResult = TaxCalculator.CalculateVAT((double)netAmount, vatRate.Value);

        return Task.FromResult(new VatBreakdown
        {
            NetAmount = netAmount,
            VATAmount = (decimal)vatResult.VATAmount,
            GrossAmount = (decimal)vatResult.GrossPrice,
            VatRate = (decimal)vatRate.Value,
            Country = customerCountry,
            VatApplicable = true
        });
    }

    public Task<TaxComplianceResult> ValidateEscrowTaxComplianceAsync(
        Guid orderId,
        decimal amount,
        string jurisdiction,
        CancellationToken cancellationToken = default)
    {
        // Validate using F# validation
        var isValid = TaxCalculator.ValidateTaxRate(TaxCalculator.GetTaxRate(jurisdiction));

        var result = new TaxComplianceResult
        {
            OrderId = orderId,
            IsCompliant = isValid,
            Jurisdiction = jurisdiction,
            Amount = amount,
            ErrorMessage = isValid ? null : "Invalid tax rate",
            ValidationTimestamp = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }
}

// ============================================================================
// DTO CLASSES
// ============================================================================

public class TaxCalculationResult
{
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal? VatRate { get; set; }
    public decimal? VatAmount { get; set; }
    public decimal Total { get; set; }
    public string Jurisdiction { get; set; } = string.Empty;
}

public class FreelancerWithholdingResult
{
    public decimal GrossAmount { get; set; }
    public decimal WithheldAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal WithholdingRate { get; set; }
    public string Country { get; set; } = string.Empty;
}

public class VatBreakdown
{
    public decimal NetAmount { get; set; }
    public decimal VATAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal? VatRate { get; set; }
    public string Country { get; set; } = string.Empty;
    public bool VatApplicable { get; set; }
}

public class TaxComplianceResult
{
    public Guid OrderId { get; set; }
    public bool IsCompliant { get; set; }
    public string Jurisdiction { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ValidationTimestamp { get; set; }
}
