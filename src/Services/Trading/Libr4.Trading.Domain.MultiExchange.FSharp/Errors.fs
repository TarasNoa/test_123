namespace Libr4.Trading.Domain.MultiExchange.FSharp

module ExchangeErrors =
    type ExchangeError =
        | ConnectionFailed
        | InvalidCredentials
        | InsufficientBalance
        | RateLimitExceeded
        | OrderRejected
        | ExchangeNotSupported
        | AccountNotFound

    let errorMessage = function
        | ConnectionFailed -> "Failed to connect to exchange"
        | InvalidCredentials -> "Invalid API credentials"
        | InsufficientBalance -> "Insufficient balance"
        | RateLimitExceeded -> "Rate limit exceeded"
        | OrderRejected -> "Order was rejected by exchange"
        | ExchangeNotSupported -> "Exchange not supported"
        | AccountNotFound -> "Exchange account not found"

    type ValidationResult<'T> = Result<'T, ExchangeError>
