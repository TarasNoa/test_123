use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::ffi::{CStr, CString};
use std::os::raw::c_char;
use std::sync::{LazyLock, Mutex};
use std::time::{Duration, Instant, SystemTime, UNIX_EPOCH};

const FAILURE_THRESHOLD: usize = 5;
const SUCCESS_THRESHOLD: i32 = 3;
const OPEN_DURATION_SECS: u64 = 30;
const SAMPLING_DURATION_SECS: u64 = 60;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
enum CircuitStatus {
    Closed = 0,
    Open = 1,
    HalfOpen = 2,
}

struct CircuitState {
    status: CircuitStatus,
    consecutive_failures: i32,
    consecutive_successes: i32,
    recent_failures: Vec<u64>,
    last_state_change: u64,
    last_failure: Option<u64>,
    last_success: Option<u64>,
}

static CIRCUITS: LazyLock<Mutex<HashMap<String, CircuitState>>> =
    LazyLock::new(|| Mutex::new(HashMap::new()));

struct TokenBucket {
    capacity: f64,
    tokens: f64,
    refill_per_sec: f64,
    last_refill: Instant,
}

static BUCKETS: LazyLock<Mutex<HashMap<String, TokenBucket>>> =
    LazyLock::new(|| Mutex::new(HashMap::new()));

fn now_secs() -> u64 {
    SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap_or_default()
        .as_secs()
}

pub fn circuit_is_open(key: &str) -> bool {
    let mut map = CIRCUITS.lock().unwrap();
    let state = map.entry(key.to_string()).or_insert_with(|| CircuitState {
        status: CircuitStatus::Closed,
        consecutive_failures: 0,
        consecutive_successes: 0,
        recent_failures: Vec::new(),
        last_state_change: now_secs(),
        last_failure: None,
        last_success: None,
    });

    if state.status == CircuitStatus::Open {
        let elapsed = now_secs().saturating_sub(state.last_state_change);
        if elapsed >= OPEN_DURATION_SECS {
            state.status = CircuitStatus::HalfOpen;
            state.last_state_change = now_secs();
            state.consecutive_successes = 0;
            state.consecutive_failures = 0;
        }
    }

    state.status == CircuitStatus::Open
}

pub fn circuit_record_success(key: &str) {
    let mut map = CIRCUITS.lock().unwrap();
    let state = map.entry(key.to_string()).or_insert_with(|| CircuitState {
        status: CircuitStatus::Closed,
        consecutive_failures: 0,
        consecutive_successes: 0,
        recent_failures: Vec::new(),
        last_state_change: now_secs(),
        last_failure: None,
        last_success: None,
    });

    state.consecutive_successes += 1;
    state.consecutive_failures = 0;
    state.last_success = Some(now_secs());

    if state.status == CircuitStatus::HalfOpen && state.consecutive_successes >= SUCCESS_THRESHOLD {
        state.status = CircuitStatus::Closed;
        state.last_state_change = now_secs();
        state.consecutive_successes = 0;
    }

    let cutoff = now_secs().saturating_sub(SAMPLING_DURATION_SECS);
    state.recent_failures.retain(|ts| *ts > cutoff);
}

pub fn circuit_record_failure(key: &str) {
    let mut map = CIRCUITS.lock().unwrap();
    let state = map.entry(key.to_string()).or_insert_with(|| CircuitState {
        status: CircuitStatus::Closed,
        consecutive_failures: 0,
        consecutive_successes: 0,
        recent_failures: Vec::new(),
        last_state_change: now_secs(),
        last_failure: None,
        last_success: None,
    });

    state.consecutive_failures += 1;
    state.consecutive_successes = 0;
    state.recent_failures.push(now_secs());
    state.last_failure = Some(now_secs());

    let cutoff = now_secs().saturating_sub(SAMPLING_DURATION_SECS);
    let failures_in_window = state.recent_failures.iter().filter(|ts| **ts > cutoff).count();

    if state.status != CircuitStatus::Open && failures_in_window >= FAILURE_THRESHOLD {
        state.status = CircuitStatus::Open;
        state.last_state_change = now_secs();
    }
}

pub fn rate_limit_allow(key: &str, capacity: f64, refill_per_sec: f64, cost: f64) -> bool {
    let mut map = BUCKETS.lock().unwrap();
    let bucket = map.entry(key.to_string()).or_insert_with(|| TokenBucket {
        capacity,
        tokens: capacity,
        refill_per_sec,
        last_refill: Instant::now(),
    });

    bucket.capacity = capacity;
    bucket.refill_per_sec = refill_per_sec;

    let elapsed = bucket.last_refill.elapsed().as_secs_f64();
    if elapsed > 0.0 {
        bucket.tokens = (bucket.tokens + elapsed * bucket.refill_per_sec).min(bucket.capacity);
        bucket.last_refill = Instant::now();
    }

    if bucket.tokens >= cost {
        bucket.tokens -= cost;
        true
    } else {
        false
    }
}

#[derive(Debug, Deserialize)]
pub struct RiskFeatures {
    pub request_count: f32,
    pub error_rate: f32,
    pub unique_paths: f32,
    pub time_window: f32,
    pub burstiness: f32,
    pub recent_violations: f32,
}

#[derive(Debug, Serialize)]
pub struct RiskDecision {
    pub risk_score: f32,
    pub action: String,
    pub limit_per_second: f32,
    pub ban_seconds: u64,
}

pub fn evaluate_risk(features: &RiskFeatures) -> RiskDecision {
    let predicted_attack = features.error_rate >= 0.6
        && features.burstiness >= 0.8
        && features.request_count >= 100.0;

    let base_score = if predicted_attack { 0.7_f32 } else { 0.3_f32 };
    let history_factor = (features.recent_violations * 0.1).min(0.3);
    let burst_factor = features.burstiness * 0.2;
    let score = (base_score + history_factor + burst_factor).min(1.0);

    if score >= 0.9 {
        RiskDecision {
            risk_score: score,
            action: "ban".to_string(),
            limit_per_second: 0.0,
            ban_seconds: 3600,
        }
    } else if score >= 0.7 {
        RiskDecision {
            risk_score: score,
            action: "strict".to_string(),
            limit_per_second: 1.0,
            ban_seconds: 0,
        }
    } else if score >= 0.5 {
        RiskDecision {
            risk_score: score,
            action: "throttle".to_string(),
            limit_per_second: 5.0,
            ban_seconds: 0,
        }
    } else {
        RiskDecision {
            risk_score: score,
            action: "allow".to_string(),
            limit_per_second: 100.0,
            ban_seconds: 0,
        }
    }
}

#[no_mangle]
pub extern "C" fn gateway_circuit_is_open(key: *const c_char) -> bool {
    if key.is_null() {
        return false;
    }
    let key_str = unsafe { CStr::from_ptr(key).to_string_lossy() };
    circuit_is_open(&key_str)
}

#[no_mangle]
pub extern "C" fn gateway_circuit_record_success(key: *const c_char) {
    if key.is_null() {
        return;
    }
    let key_str = unsafe { CStr::from_ptr(key).to_string_lossy() };
    circuit_record_success(&key_str);
}

#[no_mangle]
pub extern "C" fn gateway_circuit_record_failure(key: *const c_char) {
    if key.is_null() {
        return;
    }
    let key_str = unsafe { CStr::from_ptr(key).to_string_lossy() };
    circuit_record_failure(&key_str);
}

#[no_mangle]
pub extern "C" fn gateway_rate_limit_allow(
    key: *const c_char,
    capacity: f64,
    refill_per_sec: f64,
    cost: f64,
) -> bool {
    if key.is_null() {
        return true;
    }
    let key_str = unsafe { CStr::from_ptr(key).to_string_lossy() };
    rate_limit_allow(&key_str, capacity, refill_per_sec, cost)
}

#[no_mangle]
pub extern "C" fn gateway_evaluate_risk_json(
    features_json: *const c_char,
    out_json: *mut *mut c_char,
) -> i32 {
    if features_json.is_null() || out_json.is_null() {
        return -1;
    }

    let json = unsafe { CStr::from_ptr(features_json).to_string_lossy() };
    let features: RiskFeatures = match serde_json::from_str(&json) {
        Ok(f) => f,
        Err(e) => {
            if let Ok(err) = CString::new(format!("{{\"error\":\"{e}\"}}")) {
                unsafe { *out_json = err.into_raw() };
            }
            return -2;
        }
    };

    let decision = evaluate_risk(&features);
    match serde_json::to_string(&decision) {
        Ok(out) => match CString::new(out) {
            Ok(c) => {
                unsafe { *out_json = c.into_raw() };
                0
            }
            Err(_) => -3,
        },
        Err(_) => -4,
    }
}

#[no_mangle]
pub extern "C" fn gateway_core_free_string(s: *mut c_char) {
    if !s.is_null() {
        unsafe {
            let _ = CString::from_raw(s);
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn circuit_opens_after_failures() {
        let key = "test-order";
        for _ in 0..5 {
            circuit_record_failure(key);
        }
        assert!(circuit_is_open(key));
        circuit_record_success(key);
        circuit_record_success(key);
        circuit_record_success(key);
        assert!(!circuit_is_open(key));
    }
}
