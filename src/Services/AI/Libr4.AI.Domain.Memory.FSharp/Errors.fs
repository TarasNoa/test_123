namespace Libr4.AI.Domain.Memory.FSharp

module MemoryErrors =
    type MemoryError =
        | StorageFailed
        | RetrievalFailed
        | CorruptedData
        | MemoryNotFound
        | ConsolidationFailed

    let errorMessage = function
        | StorageFailed -> "Failed to store memory"
        | RetrievalFailed -> "Failed to retrieve memory"
        | CorruptedData -> "Memory data is corrupted"
        | MemoryNotFound -> "Memory not found"
        | ConsolidationFailed -> "Memory consolidation failed"

    type ValidationResult<'T> = Result<'T, MemoryError>
