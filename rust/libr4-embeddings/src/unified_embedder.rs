use anyhow::Result;
use std::sync::Arc;

pub enum EmbedderBackend {
    Onnx(crate::onnx_embedder::OnnxEmbedder),
    Dmr(crate::embedder::Embedder),
}

impl EmbedderBackend {
    pub async fn embed(&self, text: &str) -> Result<Vec<f32>> {
        match self {
            EmbedderBackend::Onnx(e) => e.embed(text).await,
            EmbedderBackend::Dmr(e) => e.embed(text).await,
        }
    }

    pub async fn embed_batch(&self, texts: &[&str]) -> Result<Vec<Vec<f32>>> {
        match self {
            EmbedderBackend::Onnx(e) => e.embed_batch(texts).await,
            EmbedderBackend::Dmr(e) => e.embed_batch(texts).await,
        }
    }
}

pub struct UnifiedEmbedder {
    backend: Arc<EmbedderBackend>,
}

impl UnifiedEmbedder {
    pub async fn new() -> Result<Self> {
        let backend_type = std::env::var("EMBEDDING_BACKEND")
            .unwrap_or_else(|_| "onnx".to_string())
            .to_lowercase();

        let backend = match backend_type.as_str() {
            "onnx" => {
                let model = std::env::var("ONNX_MODEL")
                    .unwrap_or_else(|_| "minilm".to_string());
                tracing::info!("Using ONNX backend with model: {}", model);
                EmbedderBackend::Onnx(crate::onnx_embedder::OnnxEmbedder::new(&model)?)
            }
            "dmr" | "http" => {
                let dmr_url = std::env::var("DMR_URL")
                    .unwrap_or_else(|_| "http://host.docker.internal:12434/engines/v1".to_string());
                let model = std::env::var("DMR_EMBEDDING_MODEL")
                    .unwrap_or_else(|_| "hf.co/nomic-ai/nomic-embed-text-v1.5-GGUF".to_string());
                tracing::info!("Using DMR backend at {}", dmr_url);
                EmbedderBackend::Dmr(crate::embedder::Embedder::new(dmr_url, model))
            }
            other => {
                anyhow::bail!("Unknown embedding backend: {}", other);
            }
        };

        Ok(Self {
            backend: Arc::new(backend),
        })
    }

    pub fn backend(&self) -> Arc<EmbedderBackend> {
        self.backend.clone()
    }
}
