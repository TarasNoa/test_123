namespace Libr4.Trading.Domain.RealCustody.FSharp

module CustodyErrors =
    type CustodyError =
        | KYCNotVerified
        | KYCExpired
        | InsufficientCustody
        | WithdrawalLocked
        | MultiSigRequired
        | AmountExceedsLimit
        | WalletNotFound
        | UnauthorizedAccess

    let errorMessage = function
        | KYCNotVerified -> "KYC verification required"
        | KYCExpired -> "KYC verification has expired"
        | InsufficientCustody -> "Insufficient custody balance"
        | WithdrawalLocked -> "Withdrawal is locked"
        | MultiSigRequired -> "Multi-signature approval required"
        | AmountExceedsLimit -> "Amount exceeds KYC level limit"
        | WalletNotFound -> "Cold wallet not found"
        | UnauthorizedAccess -> "Unauthorized access attempt"

    type ValidationResult<'T> = Result<'T, CustodyError>
