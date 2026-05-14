use anyhow::Result;
use fastembed::{EmbeddingModel, TextEmbedding, TextInitOptions};
use std::sync::Mutex;
use tracing::{info, warn};

pub struct OnnxEmbedder {
    model: Mutex<TextEmbedding>,
    model_name: String,
}

impl OnnxEmbedder {
    pub fn new(model_name: &str) -> Result<Self> {
        let model = match model_name {
            "minilm" | "all-MiniLM-L6-v2" => EmbeddingModel::AllMiniLML6V2,
            "e5" | "multilingual-e5-small" => EmbeddingModel::MultilingualE5Small,
            "bge" | "bge-small-en-v1.5" => EmbeddingModel::BGESmallENV15,
            "nomic" | "nomic-embed-text-v1" => EmbeddingModel::NomicEmbedTextV15,
            _ => {
                warn!("Unknown model '{}', falling back to all-MiniLM-L6-v2", model_name);
                EmbeddingModel::AllMiniLML6V2
            }
        };

        info!("Loading ONNX embedding model: {:?}", model);
        let embedding = TextEmbedding::try_new(
            TextInitOptions::new(model).with_show_download_progress(true),
        )?;

        info!("ONNX embedder ready");
        Ok(Self {
            model: Mutex::new(embedding),
            model_name: model_name.to_string(),
        })
    }

    pub async fn embed(&self, text: &str) -> Result<Vec<f32>> {
        let batch = self.embed_batch(&[text]).await?;
        Ok(batch.into_iter().next().unwrap_or_default())
    }

    pub async fn embed_batch(&self, texts: &[&str]) -> Result<Vec<Vec<f32>>> {
        let texts_owned: Vec<String> = texts.iter().map(|t| t.to_string()).collect();
        let mut model = self.model.lock().unwrap();
        let embeddings = model.embed(texts_owned, None)?;
        Ok(embeddings.into_iter().map(l2_normalize).collect())
    }
}

fn l2_normalize(mut v: Vec<f32>) -> Vec<f32> {
    let norm: f32 = v.iter().map(|x| x * x).sum::<f32>().sqrt();
    if norm > 1e-8 {
        v.iter_mut().for_each(|x| *x /= norm);
    }
    v
}

#[cfg(test)]
mod tests {
    use super::*;

    #[tokio::test]
    async fn test_onnx_embed_minilm() {
        let embedder = OnnxEmbedder::new("minilm").expect("Failed to load model");
        let vec = embedder.embed("hello world").await.expect("Embed failed");
        assert!(!vec.is_empty(), "Embedding should not be empty");
        let norm = vec.iter().map(|x| x * x).sum::<f32>().sqrt();
        assert!((norm - 1.0).abs() < 1e-5, "Vector not L2-normalized: norm={}", norm);
    }

    #[tokio::test]
    async fn test_onnx_embed_batch() {
        let embedder = OnnxEmbedder::new("minilm").expect("Failed to load model");
        let batch = embedder.embed_batch(&["hello", "world"]).await.expect("Batch embed failed");
        assert_eq!(batch.len(), 2);
        for vec in &batch {
            assert!(!vec.is_empty());
        }
    }
}
