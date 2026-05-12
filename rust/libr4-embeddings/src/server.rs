use std::sync::Arc;
use std::time::Instant;
use tonic::{Request, Response, Status};
use tokio_stream::wrappers::ReceiverStream;
use tracing::{info, warn};

use crate::embeddings_proto::{
    embedding_service_server::EmbeddingService,
    EmbedBatchRequest, EmbedBatchResponse,
    EmbedRequest, EmbedResponse,
    EmbeddingModel,
    SimilarityRequest, SimilarityResponse,
};
use crate::embedder::{cosine_similarity, Embedder};

pub struct EmbeddingServiceImpl {
    embedder: Arc<Embedder>,
}

impl EmbeddingServiceImpl {
    pub fn new(embedder: Embedder) -> Self {
        Self {
            embedder: Arc::new(embedder),
        }
    }
}

#[tonic::async_trait]
impl EmbeddingService for EmbeddingServiceImpl {
    async fn embed(
        &self,
        request: Request<EmbedRequest>,
    ) -> Result<Response<EmbedResponse>, Status> {
        let req = request.into_inner();
        let t = Instant::now();

        let embedding = self
            .embedder
            .embed(&req.text)
            .map_err(|e| Status::internal(e.to_string()))?;

        let dims = embedding.len() as i32;
        Ok(Response::new(EmbedResponse {
            embedding,
            dimensions: dims,
            model: req.model,
            inference_ms: t.elapsed().as_secs_f32() * 1000.0,
        }))
    }

    async fn embed_batch(
        &self,
        request: Request<EmbedBatchRequest>,
    ) -> Result<Response<EmbedBatchResponse>, Status> {
        let req = request.into_inner();
        let t = Instant::now();

        let texts: Vec<&str> = req.texts.iter().map(|s| s.as_str()).collect();
        let embeddings = self
            .embedder
            .embed_batch(&texts)
            .map_err(|e| Status::internal(e.to_string()))?;

        let responses: Vec<EmbedResponse> = embeddings
            .into_iter()
            .map(|emb| {
                let dims = emb.len() as i32;
                EmbedResponse {
                    embedding: emb,
                    dimensions: dims,
                    model: req.model,
                    inference_ms: 0.0,
                }
            })
            .collect();

        Ok(Response::new(EmbedBatchResponse {
            embeddings: responses,
            total_ms: t.elapsed().as_secs_f32() * 1000.0,
        }))
    }

    type EmbedStreamStream = ReceiverStream<Result<EmbedResponse, Status>>;

    async fn embed_stream(
        &self,
        request: Request<tonic::Streaming<EmbedRequest>>,
    ) -> Result<Response<Self::EmbedStreamStream>, Status> {
        let (tx, rx) = tokio::sync::mpsc::channel(64);
        let embedder = self.embedder.clone();
        let mut stream = request.into_inner();

        tokio::spawn(async move {
            while let Ok(Some(req)) = stream.message().await {
                let result = embedder
                    .embed(&req.text)
                    .map(|emb| {
                        let dims = emb.len() as i32;
                        EmbedResponse {
                            embedding: emb,
                            dimensions: dims,
                            model: req.model,
                            inference_ms: 0.0,
                        }
                    })
                    .map_err(|e| Status::internal(e.to_string()));

                if tx.send(result).await.is_err() {
                    break;
                }
            }
        });

        Ok(Response::new(ReceiverStream::new(rx)))
    }

    async fn similarity(
        &self,
        request: Request<SimilarityRequest>,
    ) -> Result<Response<SimilarityResponse>, Status> {
        let req = request.into_inner();
        let cos = cosine_similarity(&req.vector_a, &req.vector_b);
        let dot: f32 = req.vector_a.iter().zip(req.vector_b.iter()).map(|(a, b)| a * b).sum();
        Ok(Response::new(SimilarityResponse {
            cosine_similarity: cos,
            dot_product: dot,
        }))
    }
}
