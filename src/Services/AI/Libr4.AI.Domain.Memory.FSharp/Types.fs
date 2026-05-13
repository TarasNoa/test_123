namespace Libr4.AI.Domain.Memory.FSharp

open System

type MemoryType = ShortTerm | LongTerm | Episodic | Semantic
type MemoryImportance = Low | Normal | High | Critical

type Memory = {
    id: Guid
    agentId: Guid
    memoryType: MemoryType
    content: string
    context: string option
    tags: string list
    importance: MemoryImportance
    confidence: float
    relatedMemoryIds: Guid list
    accessCount: int
    lastAccessedAt: DateTimeOffset option
    timestamp: DateTimeOffset
    expiresAt: DateTimeOffset option
}

type MemoryConsolidation = {
    id: Guid
    agentId: Guid
    memoriesConsolidated: Guid list
    consolidatedMemoryId: Guid
    consolidatedAt: DateTimeOffset
}

type MemoryQuery = {
    id: Guid
    agentId: Guid
    query: string
    memoryTypes: MemoryType list
    retrievedMemories: Guid list
    queriedAt: DateTimeOffset
}
