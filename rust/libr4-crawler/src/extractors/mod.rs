pub mod hh_ru;

use crate::crawler_proto::JobListing;

pub trait Extractor: Send + Sync {
    fn source_name(&self) -> &'static str;
    fn fetch(
        &self,
        client: reqwest::Client,
        query: String,
        location: String,
        max_results: u32,
    ) -> std::pin::Pin<Box<dyn std::future::Future<Output = anyhow::Result<Vec<JobListing>>> + Send>>;
}
