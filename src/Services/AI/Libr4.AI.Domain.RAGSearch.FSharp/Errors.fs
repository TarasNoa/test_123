namespace Libr4.AI.Domain.RAGSearch.FSharp

module RAGErrors =
    type RAGError =
        | IndexNotFound
        | SearchFailed
        | EmbeddingError
        | InvalidDimensions
        | DocumentNotFound
        | BackendUnavailable

    let errorMessage = function
        | IndexNotFound -> "Vector index not found"
        | SearchFailed -> "Search operation failed"
        | EmbeddingError -> "Error generating embedding"
        | InvalidDimensions -> "Invalid embedding dimensions"
        | DocumentNotFound -> "Document not found"
        | BackendUnavailable -> "Vector backend unavailable"

    type ValidationResult<'T> = Result<'T, RAGError>
