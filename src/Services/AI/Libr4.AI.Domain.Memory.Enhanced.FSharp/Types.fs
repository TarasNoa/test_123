namespace Libr4.AI.Domain.Memory.Enhanced.FSharp

open System

type MemoryLevel = User | Session | AgentState | LongTerm
type MemoryEventType = Add | Update | Delete | Consolidate

type Entity = {
    id: Guid
    name: string
    entityType: string  // "person", "location", "concept", "organization", etc.
    mentions: int
    firstSeenAt: DateTimeOffset
    lastSeenAt: DateTimeOffset
}

type EnhancedMemory = {
    id: Guid
    level: MemoryLevel
    userId: string option
    sessionId: string option
    agentId: Guid option
    content: string
    entities: Entity list
    embedding: float[] option
    importance: float  // 0.0 to 1.0
    confidence: float  // 0.0 to 1.0
    relatedMemoryIds: Guid list
    accessCount: int
    lastAccessedAt: DateTimeOffset option
    createdAt: DateTimeOffset
    expiresAt: DateTimeOffset option
}

type MemoryEvent = {
    id: Guid
    memoryId: Guid
    eventType: MemoryEventType
    timestamp: DateTimeOffset
    changes: string option
}

type MemoryQuery = {
    query: string
    queryEmbedding: float[] option
    level: MemoryLevel option
    userId: string option
    sessionId: string option
    agentId: Guid option
    topK: int
    threshold: float option
}

type MemorySearchResult = {
    memory: EnhancedMemory
    score: float
    rank: int
}
