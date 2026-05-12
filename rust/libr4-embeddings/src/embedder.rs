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

#[cfg(test)]
mod tests {
    use super::*;
    use tokio::io::{AsyncReadExt, AsyncWriteExt};
    use tokio::net::TcpListener;

    async fn mock_dmr_server(port: u16) {
        let listener = TcpListener::bind(format!("127.0.0.1:{}", port)).await.unwrap();
        loop {
            let (mut socket, _) = listener.accept().await.unwrap();
            let mut buf = vec![0u8; 4096];
            let _n = socket.read(&mut buf).await.unwrap();

            let body = r#"{"data":[{"embedding":[0.1,0.2,0.3,0.4]}]}"#;
            let response = format!(
                "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {}\r\n\r\n{}",
                body.len(),
                body
            );
            let _ = socket.write_all(response.as_bytes()).await;
            let _ = socket.shutdown().await;
        }
    }

    #[tokio::test]
    async fn test_embed_normalizes() {
        tokio::spawn(mock_dmr_server(19999));
        tokio::time::sleep(std::time::Duration::from_millis(200)).await;

        let embedder = Embedder::new(
            "http://127.0.0.1:19999".to_string(),
            "test-model".to_string(),
        );
        let vec = embedder.embed("hello").await.unwrap();

        assert_eq!(vec.len(), 4);
        let norm = vec.iter().map(|x| x * x).sum::<f32>().sqrt();
        assert!((norm - 1.0).abs() < 1e-5, "Vector not L2-normalized: norm={}", norm);
    }

    #[tokio::test]
    async fn test_embed_batch() {
        tokio::spawn(mock_dmr_server(19998));
        tokio::time::sleep(std::time::Duration::from_millis(200)).await;

        let embedder = Embedder::new(
            "http://127.0.0.1:19998".to_string(),
            "test-model".to_string(),
        );
        let batch = embedder.embed_batch(&["a", "b"]).await.unwrap();

        assert_eq!(batch.len(), 2);
        for vec in &batch {
            assert_eq!(vec.len(), 4);
            let norm = vec.iter().map(|x| x * x).sum::<f32>().sqrt();
            assert!((norm - 1.0).abs() < 1e-5);
        }
    }

    #[tokio::test]
    async fn test_cosine_similarity_identical() {
        let a = vec![1.0, 0.0, 0.0];
        let b = vec![1.0, 0.0, 0.0];
        assert!((cosine_similarity(&a, &b) - 1.0).abs() < 1e-5);
    }

    #[tokio::test]
    async fn test_cosine_similarity_orthogonal() {
        let a = vec![1.0, 0.0];
        let b = vec![0.0, 1.0];
        assert!(cosine_similarity(&a, &b).abs() < 1e-5);
    }
}
