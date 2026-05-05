namespace Libr4.AI.Domain.RAGSearch.FSharp

open System

type VectorBackend = Pgvector | Qdrant | Pinecone | Weaviate | Chroma
type SearchMode = Semantic | Hybrid | Keyword

type Document = {
    id: Guid
    title: string
    content: string
    embedding: float[]
    metadata: Map<string, obj>
    tags: string list
    source: string
    indexedAt: DateTimeOffset
}

type VectorIndex = {
    id: Guid
    name: string
    backend: VectorBackend
    dimensions: int
    documentCount: int
    embeddingModel: string
    distanceMetric: string
    createdAt: DateTimeOffset
}

type SearchQuery = {
    id: Guid
    userId: Guid
    queryText: string
    mode: SearchMode
    topK: int
    threshold: float
    filters: Map<string, obj>
    createdAt: DateTimeOffset
}

type SearchResult = {
    documentId: Guid
    score: float
    rank: int
    snippet: string
    metadata: Map<string, obj>
}
