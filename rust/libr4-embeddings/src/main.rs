mod embedder;
mod server;

use tonic::transport::Server;
use tracing::info;
use tracing_subscriber::{layer::SubscriberExt, util::SubscriberInitExt};

pub mod embeddings_proto {
    tonic::include_proto!("embeddings");
}

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    tracing_subscriber::registry()
        .with(tracing_subscriber::EnvFilter::new(
            std::env::var("RUST_LOG").unwrap_or_else(|_| "info".to_string()),
        ))
        .with(tracing_subscriber::fmt::layer())
        .init();

    let model_dir = std::env::var("MODEL_PATH").unwrap_or_else(|_| "./models".to_string());
    let model_path = format!("{}/minilm-l6-v2.onnx", model_dir);
    let tokenizer_path = format!("{}/tokenizer.json", model_dir);

    info!("Loading embedding model from {}", model_path);
    let embedder = embedder::Embedder::load(&model_path, &tokenizer_path)?;
    info!("Model loaded successfully");

    let port = std::env::var("GRPC_PORT").unwrap_or_else(|_| "50061".to_string());
    let addr = format!("0.0.0.0:{}", port).parse()?;

    info!("libr4-embeddings gRPC listening on {}", addr);

    Server::builder()
        .add_service(
            embeddings_proto::embedding_service_server::EmbeddingServiceServer::new(
                server::EmbeddingServiceImpl::new(embedder),
            ),
        )
        .serve(addr)
        .await?;

    Ok(())
}
