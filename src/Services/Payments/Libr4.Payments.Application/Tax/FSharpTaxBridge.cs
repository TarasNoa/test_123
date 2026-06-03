using Libr4.Payments.Domain.TaxManagement.FSharp;

namespace Libr4.Payments.Application.Tax;

/// <summary>
/// Delegates tax rate logic to F# domain module (single source of truth).
/// </summary>
internal static class FSharpTaxBridge
{
    public static decimal GetRatePercent(TaxType taxType) => TaxOps.getTaxRate(taxType);

    public static decimal CalculateTaxAmount(decimal amount, TaxType taxType)
        => TaxOps.calculateTax(amount, TaxOps.getTaxRate(taxType));

    public static TaxType MapJurisdictionToTaxType(string jurisdiction)
    {
        return jurisdiction?.ToLowerInvariant() switch
        {
            "ru" or "russia" or "uk" or "de" or "germany" or "eu" => TaxType.VAT,
            "us" or "usa" => TaxType.Sales,
            "in" or "india" => TaxType.GST,
            "ca" or "canada" => TaxType.PST,
            _ => TaxType.VAT
        };
    }
}
