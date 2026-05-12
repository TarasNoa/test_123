use serde::{Deserialize, Serialize};
use uuid::Uuid;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SalaryRange {
    pub min: i64,
    pub max: i64,
    pub currency: String,
    pub period: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct JobListing {
    pub id: String,
    pub source: String,
    pub source_url: String,
    pub title: String,
    pub company: String,
    pub location: String,
    pub is_remote: bool,
    pub description: String,
    pub description_clean: String,
    pub skills: Vec<String>,
    pub salary: Option<SalaryRange>,
    pub posted_at: String,
    pub crawled_at: String,
}
