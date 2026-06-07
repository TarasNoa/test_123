use std::collections::HashMap;
use std::ffi::{CStr, CString};
use std::fs::{self, OpenOptions};
use std::io::{self, Write};
use std::os::raw::c_char;
use std::path::Path;
use std::sync::{LazyLock, Mutex};

static WRITER_LOCK: Mutex<()> = Mutex::new(());

struct AppendStats {
    lines_written: u64,
    bytes_written: u64,
}

static STATS: LazyLock<Mutex<HashMap<String, AppendStats>>> =
    LazyLock::new(|| Mutex::new(HashMap::new()));

pub fn append_line(path: &str, line: &str) -> Result<(), String> {
    if line.is_empty() {
        return Err("line must not be empty".to_string());
    }

    let parent = Path::new(path)
        .parent()
        .ok_or_else(|| "invalid rollout path".to_string())?;
    fs::create_dir_all(parent).map_err(|e| e.to_string())?;

    let _guard = WRITER_LOCK.lock().map_err(|e| e.to_string())?;

    let mut file = OpenOptions::new()
        .create(true)
        .append(true)
        .open(path)
        .map_err(|e| e.to_string())?;

    writeln!(file, "{line}").map_err(|e| e.to_string())?;
    file.flush().map_err(|e| e.to_string())?;
    file.sync_all().map_err(|e| e.to_string())?;

    let bytes = line.len() as u64 + 1;
    if let Ok(mut stats) = STATS.lock() {
        let entry = stats.entry(path.to_string()).or_insert(AppendStats {
            lines_written: 0,
            bytes_written: 0,
        });
        entry.lines_written += 1;
        entry.bytes_written += bytes;
    }

    Ok(())
}

pub fn append_batch(path: &str, lines: &[&str]) -> Result<u32, String> {
    if lines.is_empty() {
        return Ok(0);
    }

    let parent = Path::new(path)
        .parent()
        .ok_or_else(|| "invalid rollout path".to_string())?;
    fs::create_dir_all(parent).map_err(|e| e.to_string())?;

    let _guard = WRITER_LOCK.lock().map_err(|e| e.to_string())?;

    let mut file = OpenOptions::new()
        .create(true)
        .append(true)
        .open(path)
        .map_err(|e| e.to_string())?;

    let mut written = 0u32;
    let mut total_bytes = 0u64;
    for line in lines {
        if line.is_empty() {
            continue;
        }
        writeln!(file, "{line}").map_err(|e| e.to_string())?;
        written += 1;
        total_bytes += line.len() as u64 + 1;
    }

    file.flush().map_err(|e| e.to_string())?;
    file.sync_all().map_err(|e| e.to_string())?;

    if let Ok(mut stats) = STATS.lock() {
        let entry = stats.entry(path.to_string()).or_insert(AppendStats {
            lines_written: 0,
            bytes_written: 0,
        });
        entry.lines_written += written as u64;
        entry.bytes_written += total_bytes;
    }

    Ok(written)
}

#[no_mangle]
pub extern "C" fn rollout_append_line(path: *const c_char, line: *const c_char) -> i32 {
    if path.is_null() || line.is_null() {
        return -1;
    }

    let path_str = unsafe { CStr::from_ptr(path).to_string_lossy().into_owned() };
    let line_str = unsafe { CStr::from_ptr(line).to_string_lossy().into_owned() };

    match append_line(&path_str, &line_str) {
        Ok(()) => 0,
        Err(_) => -2,
    }
}

#[no_mangle]
pub extern "C" fn rollout_append_batch_json(path: *const c_char, lines_json: *const c_char) -> i32 {
    if path.is_null() || lines_json.is_null() {
        return -1;
    }

    let path_str = unsafe { CStr::from_ptr(path).to_string_lossy().into_owned() };
    let json_str = unsafe { CStr::from_ptr(lines_json).to_string_lossy().into_owned() };

    let lines: Vec<String> = match serde_json::from_str(&json_str) {
        Ok(v) => v,
        Err(_) => return -2,
    };

    let refs: Vec<&str> = lines.iter().map(String::as_str).collect();
    match append_batch(&path_str, &refs) {
        Ok(count) => count as i32,
        Err(_) => -3,
    }
}

#[no_mangle]
pub extern "C" fn rollout_writer_last_error(out: *mut *mut c_char) -> i32 {
    if out.is_null() {
        return -1;
    }

    let message = io::Error::last_os_error().to_string();
    match CString::new(message) {
        Ok(s) => {
            unsafe { *out = s.into_raw() };
            0
        }
        Err(_) => -2,
    }
}

#[no_mangle]
pub extern "C" fn rollout_writer_free_string(s: *mut c_char) {
    if !s.is_null() {
        unsafe {
            let _ = CString::from_raw(s);
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::time::{SystemTime, UNIX_EPOCH};

    #[test]
    fn append_line_writes_jsonl() {
        let stamp = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let dir = std::env::temp_dir().join(format!("libr4-rollout-{stamp}"));
        std::fs::create_dir_all(&dir).unwrap();
        let path = dir.join("rollout.jsonl");
        let path_str = path.to_str().unwrap();

        append_line(path_str, r#"{"type":"step_start"}"#).unwrap();
        append_line(path_str, r#"{"type":"tool_use"}"#).unwrap();

        let content = std::fs::read_to_string(&path).unwrap();
        assert_eq!(content.lines().count(), 2);

        let _ = std::fs::remove_dir_all(&dir);
    }
}
