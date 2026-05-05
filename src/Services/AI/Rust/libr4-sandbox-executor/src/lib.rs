use serde::{Deserialize, Serialize};
use std::process::{Command, Stdio};
use std::time::{Duration, Instant};
use std::io::{self, Read, Write};
use std::fs;
use std::path::PathBuf;
use std::env;

#[derive(Debug, Serialize, Deserialize)]
pub struct SandboxConfig {
    pub timeout_ms: u64,
    pub max_output_bytes: usize,
    pub project_root: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ExecResult {
    pub stdout: String,
    pub stderr: String,
    pub exit_code: i32,
    pub timed_out: bool,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct ExecutorError {
    pub message: String,
}

pub struct PolyglotExecutor {
    config: SandboxConfig,
    temp_dir: PathBuf,
}

impl PolyglotExecutor {
    pub fn new(config: SandboxConfig) -> Result<Self, ExecutorError> {
        let temp_dir = env::temp_dir();
        
        // Ensure project root exists
        if !PathBuf::from(&config.project_root).exists() {
            return Err(ExecutorError {
                message: format!("Project root does not exist: {}", config.project_root),
            });
        }
        
        Ok(Self {
            config,
            temp_dir,
        })
    }
    
    pub fn execute(&self, language: &str, code: &str) -> Result<ExecResult, ExecutorError> {
        match language.to_lowercase().as_str() {
            "csharp" | "c#" => self.execute_csharp(code),
            "fsharp" | "f#" => self.execute_fsharp(code),
            "python" => self.execute_python(code),
            "shell" | "bash" => self.execute_shell(code),
            _ => Err(ExecutorError {
                message: format!("Unsupported language: {}", language),
            }),
        }
    }
    
    fn execute_csharp(&self, code: &str) -> Result<ExecResult, ExecutorError> {
        let script_path = self.temp_dir.join("script.csx");
        
        fs::write(&script_path, code)
            .map_err(|e| ExecutorError {
                message: format!("Failed to write script: {}", e),
            })?;
        
        let start = Instant::now();
        let output = Command::new("dotnet")
            .args(&["script", script_path.to_str().unwrap()])
            .current_dir(&self.config.project_root)
            .stdout(Stdio::piped())
            .stderr(Stdio::piped())
            .timeout(Duration::from_millis(self.config.timeout_ms))
            .output();
        
        let timed_out = start.elapsed() > Duration::from_millis(self.config.timeout_ms);
        
        match output {
            Ok(result) => {
                let stdout = String::from_utf8_lossy(&result.stdout).to_string();
                let stderr = String::from_utf8_lossy(&result.stderr).to_string();
                let exit_code = result.status.code().unwrap_or(-1);
                
                // Truncate output if too large
                let stdout = self.truncate_output(stdout);
                let stderr = self.truncate_output(stderr);
                
                Ok(ExecResult {
                    stdout,
                    stderr,
                    exit_code,
                    timed_out,
                })
            }
            Err(e) if e.kind() == io::ErrorKind::TimedOut => Ok(ExecResult {
                stdout: String::new(),
                stderr: "Execution timed out".to_string(),
                exit_code: -1,
                timed_out: true,
            }),
            Err(e) => Err(ExecutorError {
                message: format!("Execution failed: {}", e),
            }),
        }
    }
    
    fn execute_fsharp(&self, code: &str) -> Result<ExecResult, ExecutorError> {
        let script_path = self.temp_dir.join("script.fsx");
        
        fs::write(&script_path, code)
            .map_err(|e| ExecutorError {
                message: format!("Failed to write script: {}", e),
            })?;
        
        let start = Instant::now();
        let output = Command::new("dotnet")
            .args(&["fsi", script_path.to_str().unwrap()])
            .current_dir(&self.config.project_root)
            .stdout(Stdio::piped())
            .stderr(Stdio::piped())
            .timeout(Duration::from_millis(self.config.timeout_ms))
            .output();
        
        let timed_out = start.elapsed() > Duration::from_millis(self.config.timeout_ms);
        
        match output {
            Ok(result) => {
                let stdout = String::from_utf8_lossy(&result.stdout).to_string();
                let stderr = String::from_utf8_lossy(&result.stderr).to_string();
                let exit_code = result.status.code().unwrap_or(-1);
                
                let stdout = self.truncate_output(stdout);
                let stderr = self.truncate_output(stderr);
                
                Ok(ExecResult {
                    stdout,
                    stderr,
                    exit_code,
                    timed_out,
                })
            }
            Err(e) if e.kind() == io::ErrorKind::TimedOut => Ok(ExecResult {
                stdout: String::new(),
                stderr: "Execution timed out".to_string(),
                exit_code: -1,
                timed_out: true,
            }),
            Err(e) => Err(ExecutorError {
                message: format!("Execution failed: {}", e),
            }),
        }
    }
    
    fn execute_python(&self, code: &str) -> Result<ExecResult, ExecutorError> {
        let script_path = self.temp_dir.join("script.py");
        
        fs::write(&script_path, code)
            .map_err(|e| ExecutorError {
                message: format!("Failed to write script: {}", e),
            })?;
        
        let start = Instant::now();
        let output = Command::new("python")
            .arg(script_path.to_str().unwrap())
            .current_dir(&self.config.project_root)
            .stdout(Stdio::piped())
            .stderr(Stdio::piped())
            .timeout(Duration::from_millis(self.config.timeout_ms))
            .output();
        
        let timed_out = start.elapsed() > Duration::from_millis(self.config.timeout_ms);
        
        match output {
            Ok(result) => {
                let stdout = String::from_utf8_lossy(&result.stdout).to_string();
                let stderr = String::from_utf8_lossy(&result.stderr).to_string();
                let exit_code = result.status.code().unwrap_or(-1);
                
                let stdout = self.truncate_output(stdout);
                let stderr = self.truncate_output(stderr);
                
                Ok(ExecResult {
                    stdout,
                    stderr,
                    exit_code,
                    timed_out,
                })
            }
            Err(e) if e.kind() == io::ErrorKind::TimedOut => Ok(ExecResult {
                stdout: String::new(),
                stderr: "Execution timed out".to_string(),
                exit_code: -1,
                timed_out: true,
            }),
            Err(e) => Err(ExecutorError {
                message: format!("Execution failed: {}", e),
            }),
        }
    }
    
    fn execute_shell(&self, code: &str) -> Result<ExecResult, ExecutorError> {
        let (shell, arg) = if cfg!(windows) {
            ("cmd", "/C")
        } else {
            ("sh", "-c")
        };
        
        let start = Instant::now();
        let output = Command::new(shell)
            .args(&[arg, code])
            .current_dir(&self.config.project_root)
            .stdout(Stdio::piped())
            .stderr(Stdio::piped())
            .timeout(Duration::from_millis(self.config.timeout_ms))
            .output();
        
        let timed_out = start.elapsed() > Duration::from_millis(self.config.timeout_ms);
        
        match output {
            Ok(result) => {
                let stdout = String::from_utf8_lossy(&result.stdout).to_string();
                let stderr = String::from_utf8_lossy(&result.stderr).to_string();
                let exit_code = result.status.code().unwrap_or(-1);
                
                let stdout = self.truncate_output(stdout);
                let stderr = self.truncate_output(stderr);
                
                Ok(ExecResult {
                    stdout,
                    stderr,
                    exit_code,
                    timed_out,
                })
            }
            Err(e) if e.kind() == io::ErrorKind::TimedOut => Ok(ExecResult {
                stdout: String::new(),
                stderr: "Execution timed out".to_string(),
                exit_code: -1,
                timed_out: true,
            }),
            Err(e) => Err(ExecutorError {
                message: format!("Execution failed: {}", e),
            }),
        }
    }
    
    fn truncate_output(&self, output: String) -> String {
        if output.len() > self.config.max_output_bytes {
            format!("{}... [truncated at {} bytes]", 
                &output[..self.config.max_output_bytes], 
                self.config.max_output_bytes)
        } else {
            output
        }
    }
}

// FFI exports for C# interop
use std::ffi::{CString, CStr};
use std::os::raw::{c_char, c_int};

#[no_mangle]
pub extern "C" fn executor_create(
    timeout_ms: u64,
    max_output_bytes: usize,
    project_root: *const c_char,
) -> *mut PolyglotExecutor {
    let project_root_str = unsafe { CStr::from_ptr(project_root).to_string_lossy().into_owned() };
    
    let config = SandboxConfig {
        timeout_ms,
        max_output_bytes,
        project_root: project_root_str,
    };
    
    match PolyglotExecutor::new(config) {
        Ok(executor) => Box::into_raw(Box::new(executor)),
        Err(_) => std::ptr::null_mut(),
    }
}

#[no_mangle]
pub extern "C" fn executor_execute(
    executor: *mut PolyglotExecutor,
    language: *const c_char,
    code: *const c_char,
    out_stdout: *mut *mut c_char,
    out_stderr: *mut *mut c_char,
    out_exit_code: *mut c_int,
    out_timed_out: *mut bool,
) -> c_int {
    if executor.is_null() {
        return -1;
    }
    
    let executor_ref = unsafe { &mut *executor };
    let language_str = unsafe { CStr::from_ptr(language).to_string_lossy().into_owned() };
    let code_str = unsafe { CStr::from_ptr(code).to_string_lossy().into_owned() };
    
    match executor_ref.execute(&language_str, &code_str) {
        Ok(result) => {
            unsafe {
                *out_stdout = CString::new(result.stdout).unwrap().into_raw();
                *out_stderr = CString::new(result.stderr).unwrap().into_raw();
                *out_exit_code = result.exit_code;
                *out_timed_out = result.timed_out;
            }
            0
        }
        Err(_) => -1,
    }
}

#[no_mangle]
pub extern "C" fn executor_free_string(s: *mut c_char) {
    if !s.is_null() {
        unsafe {
            let _ = CString::from_raw(s);
        }
    }
}

#[no_mangle]
pub extern "C" fn executor_destroy(executor: *mut PolyglotExecutor) {
    if !executor.is_null() {
        unsafe {
            let _ = Box::from_raw(executor);
        }
    }
}
