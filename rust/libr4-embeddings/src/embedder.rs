use anyhow::Result;
use serde::{Deserialize, Serialize};

#[derive(Serialize)]
struct EmbeddingRequest<'a> {
    model: &'a str,
    input: &'a str,
}

#[derive(Deserialize)]
struct EmbeddingResponse {
    data: Vec<EmbeddingData>,
}

#[derive(Deserialize)]
struct EmbeddingData {
    embedding: Vec<f32>,
}

pub struct Embedder {
    client: reqwest::Client,
    endpoint: String,
    model: String,
}

impl Embedder {
    pub fn new(endpoint: String, model: String) -> Self {
        let client = reqwest::Client::builder()
            .timeout(std::time::Duration::from_secs(60))
            .build()
            .expect("Failed to build HTTP client");
        Self { client, endpoint, model }
    }

    pub async fn embed(&self, text: &str) -> Result<Vec<f32>> {
        let mut batch = self.embed_batch(&[text]).await?;
        Ok(batch.remove(0))
    }

    pub async fn embed_batch(&self, texts: &[&str]) -> Result<Vec<Vec<f32>>> {
        let mut results = Vec::with_capacity(texts.len());
        for text in texts {
            let req_body = EmbeddingRequest {
                model: &self.model,
                input: text,
            };

            let response: EmbeddingResponse = self
                .client
                .post(format!("{}/embeddings", self.endpoint))
                .json(&req_body)
                .send()
                .await?
                .error_for_status()?
                .json()
                .await?;

            if let Some(first) = response.data.into_iter().next() {
                results.push(l2_normalize(first.embedding));
            } else {
                anyhow::bail!("DMR returned empty embedding for text: {}", text);
            }
        }
        Ok(results)
    }
}

fn l2_normalize(mut v: Vec<f32>) -> Vec<f32> {
    let norm: f32 = v.iter().map(|x| x * x).sum::<f32>().sqrt();
    if norm > 1e-8 {
        v.iter_mut().for_each(|x| *x /= norm);
    }
    v
}

pub fn cosine_similarity(a: &[f32], b: &[f32]) -> f32 {
    let dot: f32 = a.iter().zip(b.iter()).map(|(x, y)| x * y).sum();
    let na: f32 = a.iter().map(|x| x * x).sum::<f32>().sqrt();
    let nb: f32 = b.iter().map(|x| x * x).sum::<f32>().sqrt();
    let denom = na * nb;
    if denom < 1e-8 { 0.0 } else { dot / denom }
}
