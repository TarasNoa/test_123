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

    let dmr_url = std::env::var("DMR_URL")
        .unwrap_or_else(|_| "http://host.docker.internal:12434/engines/v1".to_string());
    let model = std::env::var("DMR_EMBEDDING_MODEL")
        .unwrap_or_else(|_| "docker.io/ai/nomic-embed-text:latest".to_string());

    info!("Connecting to DMR at {} using model {}", dmr_url, model);
    let embedder = embedder::Embedder::new(dmr_url, model);
    info!("DMR embedder ready");

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
