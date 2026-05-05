namespace Libr4.IDE.Application.DTOs;

public record CodeSessionDto(
    Guid Id,
    string Title,
    string Description,
    string Language,
    string ProjectId,
    Guid CreatorId,
    DateTime CreatedAt,
    DateTime? LastActivityAt,
    bool IsActive,
    List<CodeFileDto> Files,
    List<ParticipantDto> Participants
);

public record CodeFileDto(
    Guid Id,
    string FileName,
    string Content,
    string Language,
    DateTime CreatedAt,
    DateTime? ModifiedAt
);

public record ParticipantDto(
    Guid Id,
    Guid UserId,
    string Role,
    DateTime JoinedAt,
    DateTime? LeftAt,
    bool IsActive
);

public record GenerateCodeRequest(
    string Prompt,
    string Language
);

public record GenerateCodeResponse(
    string GeneratedCode,
    string Explanation,
    string Language,
    double Confidence
);

public record DebugCodeRequest(
    string Code,
    string Language,
    string ErrorMessage
);

public record DebugCodeResponse(
    List<DebuggingSuggestionDto> Issues
);

public record DebuggingSuggestionDto(
    string Issue,
    string SuggestedFix,
    int? LineNumber,
    string Severity
);

public record CompleteCodeRequest(
    string Code,
    string Language,
    int CursorPosition
);

public record CompleteCodeResponse(
    List<string> Completions,
    string Context,
    double Confidence
);

public record OptimizeCodeRequest(
    string Code,
    string Language
);

public record OptimizeCodeResponse(
    string OptimizedCode,
    List<string> Improvements,
    string PerformanceGain
);

public record ExplainCodeRequest(
    string Code,
    string Language
);

public record ExplainCodeResponse(
    string Explanation,
    List<string> KeyPoints,
    string Complexity
);
