namespace Libr4.Payments.Domain.EarlyPayment.FSharp

module EarlyPaymentErrors =
    type EarlyPaymentError =
        | RequestNotFound
        | LoanNotFound
        | InvalidAmount
        | InvalidDiscount
        | LoanAlreadyRepaid

    let errorMessage = function
        | RequestNotFound -> "Factor request not found"
        | LoanNotFound -> "Loan not found"
        | InvalidAmount -> "Invalid amount"
        | InvalidDiscount -> "Invalid discount rate"
        | LoanAlreadyRepaid -> "Loan already repaid"

    type ValidationResult<'T> = Result<'T, EarlyPaymentError>
