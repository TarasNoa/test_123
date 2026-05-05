namespace Libr4.Payments.Domain.TaxManagement.FSharp;

/// <summary>
/// C# wrapper for F# Tax calculation with Units of Measure
/// Provides compile-time safe tax operations
/// </summary>
public static class TaxCalculator
{
    // ============================================================================
    // BASIC TAX CALCULATIONS
    // ============================================================================

    /// <summary>
    /// Calculate tax amount from income
    /// </summary>
    /// <param name="income">Taxable income amount</param>
    /// <param name="taxRatePercent">Tax rate in percent (e.g., 13 for 13%)</param>
    /// <returns>Tax amount</returns>
    public static double CalculateTax(double income, double taxRatePercent)
    {
        return TaxCalculation.calculateTax<float>(income * 1.0, taxRatePercent * 1.0f);
    }

    /// <summary>
    /// Calculate effective tax rate from paid tax and income
    /// </summary>
    public static double CalculateEffectiveRate(double taxPaid, double income)
    {
        if (income <= 0) return 0;
        return (taxPaid / income) * 100.0;
    }

    /// <summary>
    /// Apply deductible to income (reduces taxable amount)
    /// </summary>
    public static double ApplyDeductible(double income, double deductible)
    {
        return Math.Max(0, income - deductible);
    }

    // ============================================================================
    // VAT / GST CALCULATIONS
    // ============================================================================

    /// <summary>
    /// Calculate VAT and gross price from net price
    /// </summary>
    /// <param name="netPrice">Price without VAT</param>
    /// <param name="vatRatePercent">VAT rate (e.g., 20 for 20%)</param>
    /// <returns>VAT calculation result</returns>
    public static VatResult CalculateVAT(double netPrice, double vatRatePercent)
    {
        var result = TaxCalculation.calculateVAT<float>(netPrice * 1.0, vatRatePercent * 1.0f);

        return new VatResult
        {
            NetPrice = result.NetPrice,
            VATAmount = result.VAT,
            GrossPrice = result.GrossPrice,
            VatRate = vatRatePercent
        };
    }

    /// <summary>
    /// Extract VAT from gross price (reverse calculation)
    /// </summary>
    public static VatExtractionResult ExtractVAT(double grossPrice, double vatRatePercent)
    {
        var result = TaxCalculation.extractVAT<float>(grossPrice * 1.0, vatRatePercent * 1.0f);

        return new VatExtractionResult
        {
            NetPrice = result.NetPrice,
            VATAmount = result.VAT,
            VatRate = vatRatePercent
        };
    }

    // ============================================================================
    // WITHHOLDING TAX
    // ============================================================================

    /// <summary>
    /// Calculate tax withholding from payment
    /// </summary>
    public static WithholdingResult CalculateWithholding(double grossPayment, double withholdingRatePercent)
    {
        var result = TaxCalculation.calculateWithholding<float>(grossPayment * 1.0, withholdingRatePercent * 1.0f);

        return new WithholdingResult
        {
            GrossPayment = result.GrossPayment,
            WithheldTax = result.WithheldTax,
            NetPayment = result.NetPayment,
            WithholdingRate = withholdingRatePercent
        };
    }

    // ============================================================================
    // PROGRESSIVE TAX (for jurisdictions like US)
    // ============================================================================

    /// <summary>
    /// Calculate progressive tax using tax brackets
    /// </summary>
    /// <param name="income">Total taxable income</param>
    /// <param name="brackets">Array of (threshold, rate) pairs</param>
    /// <returns>Total tax amount</returns>
    public static double CalculateProgressiveTax(double income, (double threshold, double rate)[] brackets)
    {
        if (income <= 0 || brackets == null || brackets.Length == 0)
            return 0;

        // Convert to F# list of tuples with units
        var fsharpBrackets = brackets.Select(b =>
            Microsoft.FSharp.Core.FSharpTuple.Create(
                b.threshold * 1.0,
                b.rate * 1.0f
            )
        ).ToList();

        return TaxCalculation.calculateProgressiveTax<float>(income * 1.0, fsharpBrackets);
    }

    /// <summary>
    /// US Federal tax brackets (2024, simplified)
    /// </summary>
    public static (double threshold, double rate)[] GetUSFederalBrackets()
    {
        return new[]
        {
            (11600.0, 10.0),
            (47150.0, 12.0),
            (100525.0, 22.0),
            (191950.0, 24.0),
            (243725.0, 32.0),
            (609350.0, 35.0),
            (double.PositiveInfinity, 37.0)
        };
    }

    // ============================================================================
    // JURISDICTION-SPECIFIC RULES
    // ============================================================================

    /// <summary>
    /// Get tax rate for jurisdiction
    /// </summary>
    public static double GetTaxRate(string jurisdiction)
    {
        return jurisdiction?.ToLower() switch
        {
            "russia" or "ru" => 13.0,
            "usa" or "us" => 24.0,  // Simplified federal only
            "uk" => 20.0,
            "germany" or "de" => 19.0,
            _ => 20.0  // Default
        };
    }

    /// <summary>
    /// Get VAT rate for jurisdiction
    /// </summary>
    public static double? GetVATRate(string jurisdiction)
    {
        return jurisdiction?.ToLower() switch
        {
            "russia" or "ru" => 20.0,
            "usa" or "us" => null,  // No federal VAT
            "uk" => 20.0,
            "germany" or "de" => 19.0,
            _ => 20.0
        };
    }

    // ============================================================================
    // VALIDATION
    // ============================================================================

    /// <summary>
    /// Validate tax rate (must be 0-100%)
    /// </summary>
    public static bool ValidateTaxRate(double rate, out string error)
    {
        if (rate < 0)
        {
            error = "Tax rate cannot be negative";
            return false;
        }
        if (rate > 100)
        {
            error = "Tax rate cannot exceed 100%";
            return false;
        }
        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Validate income amount
    /// </summary>
    public static bool ValidateIncome(double income, out string error)
    {
        if (income < 0)
        {
            error = "Income cannot be negative";
            return false;
        }
        error = string.Empty;
        return true;
    }
}

// ============================================================================
// RESULT CLASSES
// ============================================================================

/// <summary>
/// VAT calculation result
/// </summary>
public class VatResult
{
    public double NetPrice { get; set; }
    public double VATAmount { get; set; }
    public double GrossPrice { get; set; }
    public double VatRate { get; set; }
}

/// <summary>
/// VAT extraction result (reverse calculation)
/// </summary>
public class VatExtractionResult
{
    public double NetPrice { get; set; }
    public double VATAmount { get; set; }
    public double VatRate { get; set; }
}

/// <summary>
/// Tax withholding result
/// </summary>
public class WithholdingResult
{
    public double GrossPayment { get; set; }
    public double WithheldTax { get; set; }
    public double NetPayment { get; set; }
    public double WithholdingRate { get; set; }
}
