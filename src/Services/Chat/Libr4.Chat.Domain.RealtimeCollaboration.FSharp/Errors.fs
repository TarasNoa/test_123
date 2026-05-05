namespace Libr4.Chat.Domain.RealtimeCollaboration.FSharp

module CollaborationErrors =
    type CollaborationError =
        | DocumentNotFound
        | OperationFailed
        | ConflictResolutionFailed
        | InvalidOperation
        | VersionMismatch

    let errorMessage = function
        | DocumentNotFound -> "Document not found"
        | OperationFailed -> "Operation failed"
        | ConflictResolutionFailed -> "Conflict resolution failed"
        | InvalidOperation -> "Invalid operation"
        | VersionMismatch -> "Version mismatch"

    type ValidationResult<'T> = Result<'T, CollaborationError>
