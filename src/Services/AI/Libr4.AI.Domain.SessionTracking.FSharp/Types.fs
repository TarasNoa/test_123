namespace Libr4.AI.Domain.SessionTracking.FSharp

open System

type SessionEventType = ToolUse | ToolResult | Message | SessionStart | SessionEnd

type SessionEvent = {
    id: Guid
    sessionId: string
    eventType: SessionEventType
    timestamp: DateTimeOffset
    data: string
    metadata: Map<string, string>
}

type Session = {
    id: string  // SHA-256 hash of project path
    projectPath: string
    userId: string option
    agentId: Guid option
    createdAt: DateTimeOffset
    lastAccessedAt: DateTimeOffset
    messageCount: int
}

type SessionSearchResult = {
    sessionId: string
    projectPath: string
    lastAccessedAt: DateTimeOffset
    relevanceScore: float
}
