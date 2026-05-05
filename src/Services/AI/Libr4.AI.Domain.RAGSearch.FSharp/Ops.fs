namespace Libr4.AI.Domain.RAGSearch.FSharp

open System

module RAGOps =
    let cosineSimilarity (a: float[]) (b: float[]) : float =
        if Array.length a <> Array.length b then 0.0
        else
            let dotProduct = Array.zip a b |> Array.sumBy (fun (x, y) -> x * y)
            let magA = sqrt (Array.sumBy (fun x -> x * x) a)
            let magB = sqrt (Array.sumBy (fun x -> x * x) b)
            if magA = 0.0 || magB = 0.0 then 0.0 else dotProduct / (magA * magB)

    let searchDocuments (queryEmbedding: float[]) (docs: Document list) (topK: int) : SearchResult list =
        docs
        |> List.map (fun doc -> doc, cosineSimilarity queryEmbedding doc.embedding)
        |> List.sortByDescending snd
        |> List.truncate topK
        |> List.mapi (fun idx (doc, score) -> {
            documentId = doc.id
            score = score
            rank = idx + 1
            snippet = if String.length doc.content > 200 then doc.content.Substring(0, 200) + "..." else doc.content
            metadata = doc.metadata
        })

module IndexOps =
    let addDocument (doc: Document) (index: VectorIndex) : VectorIndex =
        { index with documentCount = index.documentCount + 1 }

    let createIndex (name: string) (backend: VectorBackend) (dims: int) (now: DateTimeOffset) : VectorIndex =
        {
            id = Guid.NewGuid()
            name = name
            backend = backend
            dimensions = dims
            documentCount = 0
            embeddingModel = "text-embedding-ada-002"
            distanceMetric = "cosine"
            createdAt = now
        }
