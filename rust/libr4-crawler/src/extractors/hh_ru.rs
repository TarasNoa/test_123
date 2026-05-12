use anyhow::Result;
use serde::Deserialize;
use crate::crawler_proto::{JobListing, SalaryRange};

const HH_API_BASE: &str = "https://api.hh.ru";

#[derive(Deserialize)]
struct HhResponse {
    items: Vec<HhVacancy>,
    found: u32,
}

#[derive(Deserialize)]
struct HhVacancy {
    id: String,
    name: String,
    employer: HhEmployer,
    area: HhArea,
    salary: Option<HhSalary>,
    alternate_url: String,
    published_at: String,
}

#[derive(Deserialize)]
struct HhVacancyDetail {
    description: String,
    key_skills: Vec<HhKeySkill>,
    schedule: Option<HhSchedule>,
}

#[derive(Deserialize)]
struct HhEmployer { name: String }

#[derive(Deserialize)]
struct HhArea { name: String }

#[derive(Deserialize)]
struct HhSalary {
    from: Option<i64>,
    to: Option<i64>,
    currency: String,
}

#[derive(Deserialize)]
struct HhKeySkill { name: String }

#[derive(Deserialize)]
struct HhSchedule { id: String }

pub async fn fetch(
    client: &reqwest::Client,
    query: &str,
    max_results: u32,
) -> Result<Vec<JobListing>> {
    let url = format!(
        "{}/vacancies?text={}&per_page={}&order_by=publication_time",
        HH_API_BASE,
        urlencoding::encode(query),
        max_results.min(100)
    );

    let resp: HhResponse = client
        .get(&url)
        .header("User-Agent", "libr4-matcher/1.0 (contact@libr4.com)")
        .send()
        .await?
        .json()
        .await?;

    let jobs = futures::future::join_all(
        resp.items.iter().map(|v| fetch_detail(client, v))
    ).await;

    Ok(jobs.into_iter().flatten().collect())
}

async fn fetch_detail(client: &reqwest::Client, vacancy: &HhVacancy) -> Option<JobListing> {
    let url = format!("{}/vacancies/{}", HH_API_BASE, vacancy.id);
    let detail: HhVacancyDetail = client
        .get(&url)
        .header("User-Agent", "libr4-matcher/1.0 (contact@libr4.com)")
        .send()
        .await
        .ok()?
        .json()
        .await
        .ok()?;

    let is_remote = detail.schedule
        .as_ref()
        .map(|s| s.id == "remote")
        .unwrap_or(false);

    let skills: Vec<String> = detail.key_skills.iter().map(|s| s.name.clone()).collect();
    let description_clean = strip_html(&detail.description);

    Some(JobListing {
        id: format!("hh_{}", vacancy.id),
        source: "hh_ru".to_string(),
        source_url: vacancy.alternate_url.clone(),
        title: vacancy.name.clone(),
        company: vacancy.employer.name.clone(),
        location: vacancy.area.name.clone(),
        is_remote,
        description: detail.description.clone(),
        description_clean,
        skills,
        salary: vacancy.salary.as_ref().map(|s| SalaryRange {
            min: s.from.unwrap_or(0),
            max: s.to.unwrap_or(0),
            currency: s.currency.clone(),
            period: "month".to_string(),
        }),
        posted_at: vacancy.published_at.clone(),
        crawled_at: chrono::Utc::now().to_rfc3339(),
        metadata: std::collections::HashMap::new(),
    })
}

fn strip_html(html: &str) -> String {
    let fragment = scraper::Html::parse_fragment(html);
    fragment.root_element().text().collect::<Vec<_>>().join(" ")
}
