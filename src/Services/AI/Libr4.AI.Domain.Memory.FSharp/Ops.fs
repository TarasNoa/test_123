namespace Libr4.AI.Domain.Memory.FSharp

open System

module MemoryOps =
    let storeMemory (agentId: Guid) (content: string) (memType: MemoryType) (now: DateTimeOffset) : Memory =
        {
            id = Guid.NewGuid()
            agentId = agentId
            memoryType = memType
            content = content
            context = None
            tags = []
            importance = MemoryImportance.Normal
            confidence = 1.0
            relatedMemoryIds = []
            accessCount = 0
            lastAccessedAt = None
            timestamp = now
            expiresAt = match memType with ShortTerm -> Some (now.AddHours(24.0)) | _ -> None
        }

    let access (now: DateTimeOffset) (memory: Memory) : Memory =
        { memory with accessCount = memory.accessCount + 1; lastAccessedAt = Some now }

    let shouldForget (memory: Memory) (now: DateTimeOffset) : bool =
        match memory.expiresAt with
        | Some expiry -> now > expiry
        | None -> false

    let promoteToLongTerm (memory: Memory) : Memory =
        { memory with memoryType = MemoryType.LongTerm; expiresAt = None }

module ConsolidationOps =
    let consolidate (agentId: Guid) (memoryIds: Guid list) (consolidatedId: Guid) (now: DateTimeOffset) : MemoryConsolidation =
        {
            id = Guid.NewGuid()
            agentId = agentId
            memoriesConsolidated = memoryIds
            consolidatedMemoryId = consolidatedId
            consolidatedAt = now
        }
