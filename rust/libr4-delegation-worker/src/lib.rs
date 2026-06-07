use serde::{Deserialize, Serialize};
use std::ffi::{CStr, CString};
use std::fs;
use std::io::Read;
use std::os::raw::c_char;
use std::path::Path;
use std::process::{Command, Stdio};
use std::thread;
use std::time::{Duration, Instant};

#[derive(Debug, Deserialize)]
pub struct DelegationJob {
    #[serde(rename = "RunId")]
    pub run_id: String,
    #[serde(rename = "DelegationId")]
    pub delegation_id: String,
    #[serde(rename = "Task")]
    pub task: String,
    #[serde(rename = "RunsRoot")]
    pub runs_root: String,
    #[serde(rename = "OutputPath")]
    pub output_path: String,
}

#[derive(Debug, Serialize)]
pub struct WorkerRunResult {
    pub exit_code: i32,
    pub stdout: String,
    pub stderr: String,
    pub timed_out: bool,
    pub duration_ms: u64,
}

#[derive(Debug, Deserialize)]
pub struct WorkerSpawnRequest {
    pub job_path: String,
    pub worker_cli_path: String,
    pub working_directory: String,
    pub timeout_seconds: u64,
    pub memory_limit_mb: u32,
    pub max_restart_attempts: u32,
}

pub fn run_delegation_job(request: &WorkerSpawnRequest) -> Result<WorkerRunResult, String> {
    let job_json = fs::read_to_string(&request.job_path).map_err(|e| e.to_string())?;
    let _job: DelegationJob = serde_json::from_str(&job_json).map_err(|e| e.to_string())?;

    if !Path::new(&request.worker_cli_path).exists() {
        return Err(format!("worker cli not found: {}", request.worker_cli_path));
    }

    let attempts = request.max_restart_attempts.saturating_add(1);
    let timeout = Duration::from_secs(request.timeout_seconds.max(1));
    let mut last_result = WorkerRunResult {
        exit_code: -1,
        stdout: String::new(),
        stderr: String::new(),
        timed_out: false,
        duration_ms: 0,
    };

    for attempt in 1..=attempts {
        match spawn_once(request, timeout) {
            Ok(result) => {
                last_result = result;
                if last_result.exit_code == 0 {
                    return Ok(last_result);
                }
            }
            Err(err) if attempt >= attempts => return Err(err),
            Err(err) => last_result.stderr = err,
        }
    }

    Ok(last_result)
}

fn spawn_once(request: &WorkerSpawnRequest, timeout: Duration) -> Result<WorkerRunResult, String> {
    let mut child = Command::new("dotnet")
        .arg(&request.worker_cli_path)
        .arg("delegation-run")
        .arg("--request")
        .arg(&request.job_path)
        .current_dir(&request.working_directory)
        .env("DELEGATE_BACKGROUND_CHILD", "1")
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .map_err(|e| format!("failed to spawn worker: {e}"))?;

    apply_memory_limit(child.id(), request.memory_limit_mb);

    let mut stdout = child.stdout.take().ok_or_else(|| "stdout unavailable".to_string())?;
    let mut stderr = child.stderr.take().ok_or_else(|| "stderr unavailable".to_string())?;

    let start = Instant::now();
    let mut out_buf = String::new();
    let mut err_buf = String::new();

    let stdout_handle = thread::spawn(move || {
        let mut buf = String::new();
        let _ = stdout.read_to_string(&mut buf);
        buf
    });
    let stderr_handle = thread::spawn(move || {
        let mut buf = String::new();
        let _ = stderr.read_to_string(&mut buf);
        buf
    });

    let mut timed_out = false;
    loop {
        if let Some(status) = child
            .try_wait()
            .map_err(|e| format!("wait failed: {e}"))?
        {
            out_buf = stdout_handle.join().unwrap_or_default();
            err_buf = stderr_handle.join().unwrap_or_default();
            return Ok(WorkerRunResult {
                exit_code: status.code().unwrap_or(-1),
                stdout: out_buf,
                stderr: err_buf,
                timed_out: false,
                duration_ms: start.elapsed().as_millis() as u64,
            });
        }

        if start.elapsed() >= timeout {
            timed_out = true;
            let _ = child.kill();
            let _ = child.wait();
            out_buf = stdout_handle.join().unwrap_or_default();
            err_buf = stderr_handle.join().unwrap_or_default();
            break;
        }

        thread::sleep(Duration::from_millis(50));
    }

    Ok(WorkerRunResult {
        exit_code: -1,
        stdout: out_buf,
        stderr: if err_buf.is_empty() {
            format!("delegation_timeout:{}s", timeout.as_secs())
        } else {
            err_buf
        },
        timed_out,
        duration_ms: start.elapsed().as_millis() as u64,
    })
}

#[cfg(windows)]
fn apply_memory_limit(pid: u32, memory_limit_mb: u32) {
    if memory_limit_mb == 0 {
        return;
    }

    use windows_sys::Win32::System::Threading::{
        OpenProcess, SetProcessWorkingSetSize, PROCESS_QUERY_INFORMATION, PROCESS_SET_QUOTA,
    };

    unsafe {
        let handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_SET_QUOTA, 0, pid);
        if handle.is_null() {
            return;
        }
        let bytes = (memory_limit_mb as i64).saturating_mul(1024 * 1024);
        let capped = bytes.min(i32::MAX as i64) as usize;
        let _ = SetProcessWorkingSetSize(handle, capped, capped);
    }
}

#[cfg(not(windows))]
fn apply_memory_limit(_pid: u32, _memory_limit_mb: u32) {}

#[no_mangle]
pub extern "C" fn delegation_run_worker_json(
    request_json: *const c_char,
    out_json: *mut *mut c_char,
) -> i32 {
    if request_json.is_null() || out_json.is_null() {
        return -1;
    }

    let request_str = unsafe { CStr::from_ptr(request_json).to_string_lossy() };
    let request: WorkerSpawnRequest = match serde_json::from_str(&request_str) {
        Ok(r) => r,
        Err(e) => {
            if let Ok(err) = CString::new(format!("{{\"error\":\"{e}\"}}")) {
                unsafe { *out_json = err.into_raw() };
            }
            return -2;
        }
    };

    match run_delegation_job(&request) {
        Ok(result) => match serde_json::to_string(&result) {
            Ok(json) => match CString::new(json) {
                Ok(c) => {
                    unsafe { *out_json = c.into_raw() };
                    0
                }
                Err(_) => -3,
            },
            Err(_) => -4,
        },
        Err(err) => {
            if let Ok(c) = CString::new(format!("{{\"error\":\"{err}\"}}")) {
                unsafe { *out_json = c.into_raw() };
            }
            -5
        }
    }
}

#[no_mangle]
pub extern "C" fn delegation_worker_free_string(s: *mut c_char) {
    if !s.is_null() {
        unsafe {
            let _ = CString::from_raw(s);
        }
    }
}
