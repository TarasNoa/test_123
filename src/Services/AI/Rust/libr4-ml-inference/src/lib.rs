use serde::{Deserialize, Serialize};
use std::{
    ffi::{c_char, CStr, CString},
    os::raw::c_void,
};

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct InferenceEnvelope {
    #[serde(rename = "type")]
    inference_type: String,
    request: serde_json::Value,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct FreelancerProfile {
    name: String,
    skills: Vec<String>,
    rating: f32,
    completed_tasks: i32,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct OrderAssistantRequest {
    required_skills: Vec<String>,
    budget_min: i32,
    budget_max: i32,
    duration_days: i32,
    candidate_freelancers: Vec<FreelancerProfile>,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct TaskBrief {
    task_id: String,
    title: String,
    category: String,
    required_skills: Vec<String>,
    estimated_hours: i32,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct UserProfileSummary {
    skills: Vec<String>,
    interests: Vec<String>,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
struct OrderAssistantResult {
    suggested_budget: i32,
    suggested_duration: i32,
    recommended_freelancers: Vec<String>,
    confidence: f32,
    reason: String,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
struct TaskRecommendationResult {
    task_id: String,
    title: String,
    match_score: f32,
    matching_skills: Vec<String>,
    reason: String,
}

fn normalize_strings(items: Vec<String>) -> Vec<String> {
    items
        .into_iter()
        .filter(|s| !s.trim().is_empty())
        .map(|s| s.trim().to_lowercase())
        .collect()
}

fn suggest_order(req: OrderAssistantRequest) -> OrderAssistantResult {
    let required_skills = normalize_strings(req.required_skills);
    let candidates = req
        .candidate_freelancers
        .into_iter()
        .map(|candidate| {
            let skills = normalize_strings(candidate.skills);
            let matched: Vec<String> = required_skills
                .iter()
                .filter(|required| skills.iter().any(|skill| skill == required))
                .cloned()
                .collect();

            let score =
                matched.len() as f32 * 2.0 + candidate.rating * 1.2 + (candidate.completed_tasks.min(20) as f32) * 0.05;

            (candidate.name, matched, score)
        })
        .collect::<Vec<_>>();

    let ranked: Vec<_> = candidates
        .into_iter()
        .filter(|(_, _, score)| *score > 0.0)
        .collect();

    let recommended: Vec<String> = ranked
        .iter()
        .take(3)
        .map(|(name, _, _)| name.clone())
        .collect();

    let total_match_count: i32 = ranked.iter().map(|(_, matched, _)| matched.len() as i32).sum();
    let skill_coverage = if required_skills.is_empty() || ranked.is_empty() {
        0.0
    } else {
        let denominator = (required_skills.len() as f32) * (ranked.len() as f32);
        (total_match_count as f32 / denominator).min(1.0)
    };

    let budget = ((req.budget_min as f32)
        + (req.budget_max - req.budget_min) as f32 * skill_coverage)
        .round() as i32
        .clamp(req.budget_min, req.budget_max);

    let duration = ((req.duration_days as f32) * (0.7_f32.max(1.0 - skill_coverage * 0.25))).round() as i32;
    let duration = duration.max(1);

    OrderAssistantResult {
        suggested_budget: budget,
        suggested_duration: duration,
        recommended_freelancers: recommended,
        confidence: (0.35 + skill_coverage * 0.55 + if skill_coverage > 0.0 { 0.1 } else { 0.0 }) as f32,
        reason: if skill_coverage > 0.0 {
            "Подбор заказа выполнен на основе навыков исполнителей и рейтингов.".into()
        } else {
            "Недостаточно совпадений, расчет выполнен на основе доступных данных.".into()
        },
    }
}

fn recommend_tasks(
    profile: UserProfileSummary,
    tasks: Vec<TaskBrief>,
) -> Vec<TaskRecommendationResult> {
    let user_skills = normalize_strings(profile.skills);
    let user_interests = normalize_strings(profile.interests);

    let mut results: Vec<TaskRecommendationResult> = tasks
        .into_iter()
        .map(|task| {
            let required_skills = normalize_strings(task.required_skills);
            let matching_skills: Vec<String> = user_skills
                .iter()
                .filter(|skill| required_skills.iter().any(|required| required == *skill))
                .cloned()
                .collect();

            let interest_matches = user_interests
                .iter()
                .filter(|interest| task.category.to_lowercase().contains(interest))
                .count();

            let score = (matching_skills.len() as f32) * 2.0
                + (interest_matches as f32) * 1.5
                + (user_skills.len().min(20) as f32) * 0.05
                + (user_interests.len().min(10) as f32) * 0.05;

            TaskRecommendationResult {
                task_id: task.task_id,
                title: task.title,
                match_score: (score / 10.0).min(1.0),
                matching_skills,
                reason: if matching_skills.is_empty() {
                    "Рекомендуется на основе интересов и категории.".into()
                } else {
                    "Задача хорошо подходит по навыкам.".into()
                },
            }
        })
        .collect();

    results.sort_by(|a, b| b.match_score.partial_cmp(&a.match_score).unwrap());
    results
}

#[no_mangle]
pub extern "C" fn libr4_ml_run_inference(input_json: *const c_char) -> *mut c_char {
    if input_json.is_null() {
        return CString::new("").unwrap().into_raw();
    }

    let c_str = unsafe { CStr::from_ptr(input_json) };
    let json_str = match c_str.to_str() {
        Ok(text) => text,
        Err(_) => return CString::new("").unwrap().into_raw(),
    };

    let payload: InferenceEnvelope = match serde_json::from_str(json_str) {
        Ok(payload) => payload,
        Err(_) => {
            return CString::new("{\"error\":\"invalid request\"}").unwrap().into_raw();
        }
    };

    let response = match payload.inference_type.as_str() {
        "orderAssistant" => {
            let request: OrderAssistantRequest = match serde_json::from_value(payload.request) {
                Ok(value) => value,
                Err(_) => {
                    return CString::new("{\"error\":\"invalid order assistant request\"}")
                        .unwrap()
                        .into_raw();
                }
            };

            serde_json::to_string(&suggest_order(request)).unwrap_or_else(|_| "{\"error\":\"serialization failed\"}".into())
        }

        "taskRecommendations" => {
            #[derive(Deserialize)]
            struct TaskRecommendationDto {
                user_profile: UserProfileSummary,
                available_tasks: Vec<TaskBrief>,
            }

            let request: TaskRecommendationDto = match serde_json::from_value(payload.request) {
                Ok(value) => value,
                Err(_) => {
                    return CString::new("{\"error\":\"invalid task recommendation request\"}")
                        .unwrap()
                        .into_raw();
                }
            };

            serde_json::to_string(&recommend_tasks(request.user_profile, request.available_tasks))
                .unwrap_or_else(|_| "{\"error\":\"serialization failed\"}".into())
        }

        _ => "{\"error\":\"unsupported inference type\"}\"".into(),
    };

    CString::new(response).unwrap().into_raw()
}

#[no_mangle]
pub extern "C" fn libr4_ml_free_string(ptr: *mut c_char) {
    if ptr.is_null() {
        return;
    }
    unsafe {
        CString::from_raw(ptr);
    }
}