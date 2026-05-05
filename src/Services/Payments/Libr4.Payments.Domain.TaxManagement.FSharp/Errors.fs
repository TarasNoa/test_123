namespace Libr4.Payments.Domain.TaxManagement.FSharp

module TaxErrors =
    type TaxError =
        | InvalidTaxRate
        | FormNotFound
        | ReportNotFound
        | InvalidTaxType
        | CalculationFailed

    let errorMessage = function
        | InvalidTaxRate -> "Invalid tax rate"
        | FormNotFound -> "Tax form not found"
        | ReportNotFound -> "Tax report not found"
        | InvalidTaxType -> "Invalid tax type"
        | CalculationFailed -> "Tax calculation failed"

    type ValidationResult<'T> = Result<'T, TaxError>

    let validateRate (rate: decimal) : ValidationResult<decimal> =
        if rate >= 0m && rate <= 100m then Ok rate else Error InvalidTaxRate
