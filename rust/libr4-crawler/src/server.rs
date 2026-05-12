use tonic::{Request, Response, Status};
use tokio_stream::wrappers::ReceiverStream;
use tracing::{info, warn};

use crate::crawler_proto::{
    crawler_service_server::CrawlerService,
    CheckSourceRequest, CheckSourceResponse,
    FetchJobsRequest, FetchJobsResponse,
    JobListing, SearchJobsRequest,
};
use crate::rate_limiter::DomainRateLimiter;
use crate::extractors::hh_ru;

pub struct CrawlerServiceImpl {
    http_client: reqwest::Client,
    rate_limiter: DomainRateLimiter,
}

impl CrawlerServiceImpl {
    pub fn new() -> Self {
        let http_client = reqwest::Client::builder()
            .use_rustls_tls()
            .timeout(std::time::Duration::from_secs(30))
            .build()
            .expect("Failed to build HTTP client");

        Self {
            http_client,
            rate_limiter: DomainRateLimiter::new(),
        }
    }
}

#[tonic::async_trait]
impl CrawlerService for CrawlerServiceImpl {
    async fn fetch_jobs(
        &self,
        request: Request<FetchJobsRequest>,
    ) -> Result<Response<FetchJobsResponse>, Status> {
        let req = request.into_inner();
        info!("FetchJobs: source={}, query={}", req.source, req.query);

        self.rate_limiter.wait(&req.source).await;

        let jobs = match req.source.as_str() {
            "hh_ru" => hh_ru::fetch(&self.http_client, &req.query, req.max_results as u32)
                .await
                .unwrap_or_else(|e| {
                    warn!("hh_ru fetch failed: {}", e);
                    vec![]
                }),
            src => {
                warn!("Unknown source: {}", src);
                vec![]
            }
        };

        let total = jobs.len() as i32;
        Ok(Response::new(FetchJobsResponse {
            total_found: total,
            source: req.source.clone(),
            jobs,
            error: String::new(),
        }))
    }

    type StreamJobsStream = ReceiverStream<Result<JobListing, Status>>;

    async fn stream_jobs(
        &self,
        request: Request<FetchJobsRequest>,
    ) -> Result<Response<Self::StreamJobsStream>, Status> {
        let req = request.into_inner();
        let (tx, rx) = tokio::sync::mpsc::channel(64);
        let client = self.http_client.clone();

        tokio::spawn(async move {
            let jobs = match req.source.as_str() {
                "hh_ru" => hh_ru::fetch(&client, &req.query, req.max_results as u32)
                    .await
                    .unwrap_or_default(),
                _ => vec![],
            };
            for job in jobs {
                if tx.send(Ok(job)).await.is_err() {
                    break;
                }
            }
        });

        Ok(Response::new(ReceiverStream::new(rx)))
    }

    type SearchJobsStream = ReceiverStream<Result<JobListing, Status>>;

    async fn search_jobs(
        &self,
        request: Request<SearchJobsRequest>,
    ) -> Result<Response<Self::SearchJobsStream>, Status> {
        let req = request.into_inner();
        let (tx, rx) = tokio::sync::mpsc::channel(64);
        let client = self.http_client.clone();
        let query = req.query.clone();
        let max = req.max_per_source as u32;

        tokio::spawn(async move {
            for source in &req.sources {
                let jobs = match source.as_str() {
                    "hh_ru" => hh_ru::fetch(&client, &query, max).await.unwrap_or_default(),
                    _ => vec![],
                };
                for job in jobs {
                    if tx.send(Ok(job)).await.is_err() {
                        return;
                    }
                }
            }
        });

        Ok(Response::new(ReceiverStream::new(rx)))
    }

    async fn check_source(
        &self,
        request: Request<CheckSourceRequest>,
    ) -> Result<Response<CheckSourceResponse>, Status> {
        let source = request.into_inner().source;
        let available = matches!(source.as_str(), "hh_ru" | "remote_ok");
        Ok(Response::new(CheckSourceResponse {
            source,
            available,
            rate_limit_remaining: 100,
            error: String::new(),
        }))
    }
}
