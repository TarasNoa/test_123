using Microsoft.FSharp.Core;

namespace Libr4.Tasks.Domain.TimeTracking.FSharp;

/// <summary>
/// C# wrapper for F# Units of Measure financial calculations
/// Provides compile-time safe financial operations via F# layer
/// </summary>
public static class FinancialCalculator
{
    // ============================================================================
    // BILLING CALCULATIONS - Safe money calculations
    // ============================================================================

    /// <summary>
    /// Calculate invoice with full unit safety (via F#)
    /// </summary>
    /// <param name="hoursWorked">Hours worked (e.g., 40)</param>
    /// <param name="hourlyRate">Hourly rate in currency (e.g., 1500)</param>
    /// <param name="discountPercent">Discount percentage (e.g., 10)</param>
    /// <param name="taxPercent">Tax percentage (e.g., 20)</param>
    /// <returns>Invoice breakdown with all calculations</returns>
    public static InvoiceResult CalculateInvoice(
        double hoursWorked,
        double hourlyRate,
        double discountPercent,
        double taxPercent)
    {
        // Call F# function that enforces unit safety internally
        var result = Billing.calculateInvoice<float>(
            hoursWorked * 1.0,
            hourlyRate * 1.0,
            discountPercent * 1.0,
            taxPercent * 1.0
        );

        return new InvoiceResult
        {
            HoursWorked = result.HoursWorked,
            HourlyRate = result.HourlyRate,
            Subtotal = result.Subtotal,
            Discount = result.Discount,
            TaxableAmount = result.TaxableAmount,
            Tax = result.Tax,
            Total = result.Total
        };
    }

    /// <summary>
    /// Calculate earnings from hours worked
    /// </summary>
    public static double CalculateEarnings(double hoursWorked, double hourlyRate)
    {
        return Billing.calculateEarnings<float>(hoursWorked * 1.0, hourlyRate * 1.0);
    }

    /// <summary>
    /// Calculate earnings from minutes worked
    /// </summary>
    public static double CalculateEarningsFromMinutes(double minutesWorked, double hourlyRate)
    {
        return Billing.calculateEarningsFromMinutes<float>(minutesWorked * 1.0, hourlyRate * 1.0);
    }

    /// <summary>
    /// Apply discount to amount
    /// </summary>
    public static DiscountResult ApplyDiscount(double amount, double discountPercent)
    {
        var result = Billing.applyDiscount<float>(amount * 1.0, discountPercent * 1.0);

        return new DiscountResult
        {
            Original = result.Original,
            Discount = result.Discount,
            Final = result.Final,
            DiscountPercentage = discountPercent
        };
    }

    /// <summary>
    /// Apply tax to amount
    /// </summary>
    public static TaxResult ApplyTax(double amount, double taxPercent)
    {
        var result = Billing.applyTax<float>(amount * 1.0, taxPercent * 1.0);

        return new TaxResult
        {
            Subtotal = result.Subtotal,
            Tax = result.Tax,
            Total = result.Total,
            TaxPercentage = taxPercent
        };
    }

    /// <summary>
    /// Calculate commission
    /// </summary>
    public static double CalculateCommission(double amount, double commissionPercent)
    {
        return Billing.calculateCommission<float>(amount * 1.0, commissionPercent * 1.0);
    }

    // ============================================================================
    // RATE CONVERSIONS
    // ============================================================================

    /// <summary>
    /// Convert hourly rate to daily rate (8 hours = 1 day)
    /// </summary>
    public static double HourlyToDaily(double hourlyRate)
    {
        return RateConversions.toDaily(hourlyRate * 1.0);
    }

    /// <summary>
    /// Convert hourly rate to weekly earnings (40 hours = 1 week)
    /// </summary>
    public static double HourlyToWeekly(double hourlyRate)
    {
        return RateConversions.toWeekly(hourlyRate * 1.0);
    }

    /// <summary>
    /// Convert hourly rate to monthly earnings (160 hours = 1 month)
    /// </summary>
    public static double HourlyToMonthly(double hourlyRate)
    {
        return RateConversions.toMonthly(hourlyRate * 1.0);
    }

    /// <summary>
    /// Convert minutes to hours
    /// </summary>
    public static double MinutesToHours(double minutes)
    {
        return RateConversions.minutesToHours(minutes * 1.0);
    }

    /// <summary>
    /// Convert hours to minutes
    /// </summary>
    public static double HoursToMinutes(double hours)
    {
        return RateConversions.hoursToMinutes(hours * 1.0);
    }

    /// <summary>
    /// Convert days to hours (1 day = 8 hours)
    /// </summary>
    public static double DaysToHours(double days)
    {
        return RateConversions.daysToHours(days * 1.0);
    }

    // ============================================================================
    // ESCROW - Payment safety
    // ============================================================================

    /// <summary>
    /// Create escrow amount with conditions
    /// </summary>
    public static EscrowInfo CreateEscrow(double amount, string[] conditions)
    {
        var fsharpConditions = conditions.Select(c =>
            Escrow.ReleaseCondition.NewMilestoneCompleted(c)
        ).ToList();

        var escrow = Escrow.createEscrow(amount * 1.0, fsharpConditions);

        return new EscrowInfo
        {
            Amount = escrow.Amount,
            HeldSince = escrow.HeldSince,
            Status = escrow.Status.ToString(),
            Conditions = conditions
        };
    }

    /// <summary>
    /// Check if escrow can be released
    /// </summary>
    public static bool CanReleaseEscrow(EscrowInfo escrow, bool[] conditionsMet)
    {
        var fsharpEscrow = Escrow.createEscrow(escrow.Amount * 1.0, new List<Escrow.ReleaseCondition>());
        var fsharpConditions = conditionsMet.ToList();

        return Escrow.canRelease(fsharpEscrow, fsharpConditions);
    }

    // ============================================================================
    // VALIDATION - Safe parsing
    // ============================================================================

    /// <summary>
    /// Validate money amount (must be non-negative)
    /// </summary>
    public static ValidationResult<double> ValidateMoney(decimal value)
    {
        var result = Validation.parseMoney<float>(value);

        return result.IsValid
            ? ValidationResult<double>.Valid(result.ValidValue)
            : ValidationResult<double>.Invalid(result.ErrorValue);
    }

    /// <summary>
    /// Validate hourly rate (must be positive)
    /// </summary>
    public static ValidationResult<double> ValidateHourlyRate(decimal value)
    {
        var result = Validation.parseHourlyRate<float>(value);

        return result.IsValid
            ? ValidationResult<double>.Valid(result.ValidValue)
            : ValidationResult<double>.Invalid(result.ErrorValue);
    }

    /// <summary>
    /// Validate hours (must be non-negative)
    /// </summary>
    public static ValidationResult<double> ValidateHours(double value)
    {
        var result = Validation.parseHours(value);

        return result.IsValid
            ? ValidationResult<double>.Valid(result.ValidValue)
            : ValidationResult<double>.Invalid(result.ErrorValue);
    }

    /// <summary>
    /// Validate percentage (must be 0-100)
    /// </summary>
    public static ValidationResult<double> ValidatePercentage(double value)
    {
        var result = Validation.parsePercent(value);

        return result.IsValid
            ? ValidationResult<double>.Valid(result.ValidValue)
            : ValidationResult<double>.Invalid(result.ErrorValue);
    }
}

// ============================================================================
// C# DATA TYPES
// ============================================================================

/// <summary>
/// Invoice calculation result
/// </summary>
public class InvoiceResult
{
    public double HoursWorked { get; set; }
    public double HourlyRate { get; set; }
    public double Subtotal { get; set; }
    public double Discount { get; set; }
    public double TaxableAmount { get; set; }
    public double Tax { get; set; }
    public double Total { get; set; }
}

/// <summary>
/// Discount calculation result
/// </summary>
public class DiscountResult
{
    public double Original { get; set; }
    public double Discount { get; set; }
    public double Final { get; set; }
    public double DiscountPercentage { get; set; }
}

/// <summary>
/// Tax calculation result
/// </summary>
public class TaxResult
{
    public double Subtotal { get; set; }
    public double Tax { get; set; }
    public double Total { get; set; }
    public double TaxPercentage { get; set; }
}

/// <summary>
/// Escrow information
/// </summary>
public class EscrowInfo
{
    public double Amount { get; set; }
    public DateTime HeldSince { get; set; }
    public string Status { get; set; } = string.Empty;
    public string[] Conditions { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Validation result wrapper
/// </summary>
public class ValidationResult<T>
{
    public bool IsValid { get; }
    public T Value { get; }
    public string Error { get; }

    private ValidationResult(bool isValid, T value, string error)
    {
        IsValid = isValid;
        Value = value;
        Error = error;
    }

    public static ValidationResult<T> Valid(T value) => new(true, value, string.Empty);
    public static ValidationResult<T> Invalid(string error) => new(false, default!, error);
}
