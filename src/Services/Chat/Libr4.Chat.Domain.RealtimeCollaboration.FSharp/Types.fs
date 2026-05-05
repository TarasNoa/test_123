namespace Libr4.Chat.Domain.RealtimeCollaboration.FSharp

open System

type OperationType = Insert | Delete | Update
type ConflictResolution = ClientWins | ServerWins | Merge

type Operation = {
    id: Guid
    documentId: Guid
    userId: Guid
    opType: OperationType
    position: int
    content: string option
    timestamp: DateTimeOffset
    version: int
}

type CRDTState = {
    documentId: Guid
    content: string
    version: int
    lastModified: DateTimeOffset
    operations: Operation list
}

type ConflictEvent = {
    id: Guid
    documentId: Guid
    operation1: Operation
    operation2: Operation
    resolution: ConflictResolution
    resolvedAt: DateTimeOffset
}
