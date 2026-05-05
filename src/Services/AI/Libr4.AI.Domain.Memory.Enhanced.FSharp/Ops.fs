namespace Libr4.AI.Domain.Memory.Enhanced.FSharp

open System
open System.Text.RegularExpressions
open Libr4.AI.Domain.RAGSearch.FSharp

module EntityExtractionOps =
    let private patterns = [
        (@"\b[A-Z][a-z]+ [A-Z][a-z]+\b", "person")  // John Doe
        (@"\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*\b", "concept")  // Capitalized words
        (@"\b[A-Z]{2,}\b", "acronym")  // ALL CAPS
    ]
    
    let extractEntities (content: string) : Entity list =
        let now = DateTimeOffset.UtcNow
        
        patterns
        |> List.collect (fun (pattern, entityType) ->
            try
                Regex.Matches(content, pattern)
                |> Seq.cast<Match>
                |> Seq.map (fun m ->
                    {
                        id = Guid.NewGuid()
                        name = m.Value
                        entityType = entityType
                        mentions = 1
                        firstSeenAt = now
                        lastSeenAt = now
                    })
                |> List.ofSeq
            with _ ->
                []
        )
        |> List.groupBy (fun e -> e.name.ToLower())
        |> List.map (fun (name, entities) ->
            let first = List.head entities
            { first with 
                name = name  // Normalize to lowercase
                mentions = List.length entities
            }
        )
    
    let mergeEntities (existing: Entity list) (newEntities: Entity list) : Entity list =
        let entityMap = 
            existing
            |> List.map (fun e -> (e.name.ToLower(), e))
            |> Map.ofList
        
        newEntities
        |> List.fold (fun acc (newEntity: Entity) ->
            let key = newEntity.name.ToLower()
            match Map.tryFind key entityMap with
            | Some existingEntity ->
                let merged = 
                    { existingEntity with
                        mentions = existingEntity.mentions + newEntity.mentions
                        lastSeenAt = max existingEntity.lastSeenAt newEntity.lastSeenAt
                    }
                (merged :: acc) |> List.filter (fun e -> e.name.ToLower() <> key)
            | None ->
                newEntity :: acc
        ) existing

module MemoryOps =
    let createMemory (level: MemoryLevel) (content: string) (now: DateTimeOffset) : EnhancedMemory =
        let entities = EntityExtractionOps.extractEntities content
        {
            id = Guid.NewGuid()
            level = level
            userId = None
            sessionId = None
            agentId = None
            content = content
            entities = entities
            embedding = None
            importance = 0.5
            confidence = 1.0
            relatedMemoryIds = []
            accessCount = 0
            lastAccessedAt = None
            createdAt = now
            expiresAt = match level with Session -> Some (now.AddHours(24.0)) | _ -> None
        }
    
    let accessMemory (now: DateTimeOffset) (memory: EnhancedMemory) : EnhancedMemory =
        { memory with 
            accessCount = memory.accessCount + 1
            lastAccessedAt = Some now
        }
    
    let shouldForget (memory: EnhancedMemory) (now: DateTimeOffset) : bool =
        match memory.expiresAt with
        | Some expiry -> now > expiry
        | None -> false
    
    let consolidateMemories (memories: EnhancedMemory list) (now: DateTimeOffset) : EnhancedMemory =
        if List.isEmpty memories then
            failwith "Cannot consolidate empty memory list"
        
        let allEntities = 
            memories
            |> List.collect (fun m -> m.entities)
            |> List.groupBy (fun e -> e.name.ToLower())
            |> List.map (fun (name, entities) ->
                let first = List.head entities
                { first with 
                    mentions = entities |> List.sumBy (fun e -> e.mentions)
                    lastSeenAt = entities |> List.map (fun e -> e.lastSeenAt) |> List.max
                }
            )
        
        {
            id = Guid.NewGuid()
            level = LongTerm
            userId = memories.[0].userId
            sessionId = None
            agentId = memories.[0].agentId
            content = memories |> List.map (fun m -> m.content) |> String.concat " "
            entities = allEntities
            embedding = None
            importance = memories |> List.map (fun m -> m.importance) |> List.average
            confidence = memories |> List.map (fun m -> m.confidence) |> List.average
            relatedMemoryIds = memories |> List.map (fun m -> m.id)
            accessCount = memories |> List.sumBy (fun m -> m.accessCount)
            lastAccessedAt = memories |> List.choose (fun m -> m.lastAccessedAt) |> List.tryLast
            createdAt = memories |> List.map (fun m -> m.createdAt) |> List.min
            expiresAt = None
        }
    
    let updateMemoryEmbedding (embedding: float[]) (memory: EnhancedMemory) : EnhancedMemory =
        { memory with embedding = Some embedding }

module HybridSearchOps =
    let private bm25Score (query: string) (document: string) : float =
        // Simplified BM25
        let queryTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries) |> Set.ofArray
        let docTerms = document.Split(' ', StringSplitOptions.RemoveEmptyEntries) |> Set.ofArray
        let intersection = Set.intersect queryTerms docTerms
        if Set.count queryTerms = 0 then 0.0
        else float (Set.count intersection) / float (Set.count queryTerms)
    
    let hybridSearch (query: string) (queryEmbedding: float[] option) (memories: EnhancedMemory list) (topK: int) : MemorySearchResult list =
        memories
        |> List.map (fun mem ->
            let semanticScore = 
                match mem.embedding, queryEmbedding with
                | Some emb, Some queryEmb -> RAGOps.cosineSimilarity queryEmb emb
                | _ -> 0.0
            let bm25Score = bm25Score query mem.content
            let combinedScore = 0.7 * semanticScore + 0.3 * bm25Score
            {
                memory = mem
                score = combinedScore
                rank = 0
            }
        )
        |> List.sortByDescending (fun r -> r.score)
        |> List.truncate topK
        |> List.mapi (fun idx r -> { r with rank = idx + 1 })
    
    let filterByLevel (level: MemoryLevel option) (memories: EnhancedMemory list) : EnhancedMemory list =
        match level with
        | None -> memories
        | Some l -> memories |> List.filter (fun m -> m.level = l)
    
    let filterByUser (userId: string option) (memories: EnhancedMemory list) : EnhancedMemory list =
        match userId with
        | None -> memories
        | Some uid -> memories |> List.filter (fun m -> m.userId = Some uid)
    
    let filterBySession (sessionId: string option) (memories: EnhancedMemory list) : EnhancedMemory list =
        match sessionId with
        | None -> memories
        | Some sid -> memories |> List.filter (fun m -> m.sessionId = Some sid)
    
    let filterByAgent (agentId: Guid option) (memories: EnhancedMemory list) : EnhancedMemory list =
        match agentId with
        | None -> memories
        | Some aid -> memories |> List.filter (fun m -> m.agentId = Some aid)

module MemoryStoreOps =
    // In-memory storage (can be replaced with database)
    let private memories = System.Collections.Concurrent.ConcurrentDictionary<Guid, EnhancedMemory>()
    
    let addMemory (memory: EnhancedMemory) : unit =
        memories.[memory.id] <- memory
    
    let getMemory (id: Guid) : EnhancedMemory option =
        match memories.TryGetValue(id) with
        | true, memory -> Some memory
        | false, _ -> None
    
    let updateMemory (memory: EnhancedMemory) : unit =
        memories.[memory.id] <- memory
    
    let deleteMemory (id: Guid) : unit =
        let _ = memories.TryRemove(id) |> ignore
        ()
    
    let getAllMemories () : EnhancedMemory list =
        memories.Values |> Seq.toList
    
    let search (query: MemoryQuery) : MemorySearchResult list =
        let filtered = 
            getAllMemories()
            |> HybridSearchOps.filterByLevel query.level
            |> HybridSearchOps.filterByUser query.userId
            |> HybridSearchOps.filterBySession query.sessionId
            |> HybridSearchOps.filterByAgent query.agentId
        
        HybridSearchOps.hybridSearch query.query query.queryEmbedding filtered query.topK
        |> List.filter (fun r -> 
            match query.threshold with
            | Some t -> r.score >= t
            | None -> true
        )
