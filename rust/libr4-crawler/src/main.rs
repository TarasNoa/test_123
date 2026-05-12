mod server;
mod rate_limiter;
mod extractors;

use tonic::transport::Server;
use tracing::info;
use tracing_subscriber::{layer::SubscriberExt, util::SubscriberInitExt};

pub mod crawler_proto {
    tonic::include_proto!("crawler");
}

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    tracing_subscriber::registry()
        .with(tracing_subscriber::EnvFilter::new(
            std::env::var("RUST_LOG").unwrap_or_else(|_| "info".to_string()),
        ))
        .with(tracing_subscriber::fmt::layer())
        .init();

    let port = std::env::var("GRPC_PORT").unwrap_or_else(|_| "50060".to_string());
    let addr = format!("0.0.0.0:{}", port).parse()?;

    info!("libr4-crawler gRPC listening on {}", addr);

    Server::builder()
        .add_service(crawler_proto::crawler_service_server::CrawlerServiceServer::new(
            server::CrawlerServiceImpl::new(),
        ))
        .serve(addr)
        .await?;

    Ok(())
}
