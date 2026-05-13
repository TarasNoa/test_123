namespace Libr4.AI.Domain.LocalAI.FSharp

module LocalAIErrors =
    type LocalAIError =
        | ModelNotFound
        | ConnectionFailed
        | InferenceError
        | ModelNotRunning
        | OutOfMemory
        | InvalidFormat
        | UnsupportedBackend

    let errorMessage = function
        | ModelNotFound -> "Model not found"
        | ConnectionFailed -> "Connection to local AI failed"
        | InferenceError -> "Inference error occurred"
        | ModelNotRunning -> "Model is not running"
        | OutOfMemory -> "Out of memory"
        | InvalidFormat -> "Invalid model format"
        | UnsupportedBackend -> "Backend not supported"

    type ValidationResult<'T> = Result<'T, LocalAIError>
