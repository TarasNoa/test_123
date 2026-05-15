namespace Libr4.IDE.Api;

public record SaveFileRequest(Guid SessionId, string Path, string Content);
