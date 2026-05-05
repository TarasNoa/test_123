namespace Libr4.Payments.Domain.P2PLending.FSharp

module P2PLendingErrors =
    type P2PLendingError =
        | RequestNotFound
        | LoanNotFound
        | InsufficientFunds
        | InvalidCreditScore
        | LoanAlreadyRepaid

    let errorMessage = function
        | RequestNotFound -> "Loan request not found"
        | LoanNotFound -> "Loan not found"
        | InsufficientFunds -> "Insufficient funds"
        | InvalidCreditScore -> "Invalid credit score"
        | LoanAlreadyRepaid -> "Loan already repaid"

    type ValidationResult<'T> = Result<'T, P2PLendingError>
