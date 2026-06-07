use serde::{Deserialize, Serialize};
use std::process::{Command, Stdio};
use std::time::Duration;
use std::sync::mpsc;
use std::thread;
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
    
    fn run_prepared_command(&self, cmd: Command) -> Result<ExecResult, ExecutorError> {
        let mut result = run_command_with_timeout(cmd, self.config.timeout_ms)?;
        result.stdout = self.truncate_output(result.stdout);
        result.stderr = self.truncate_output(result.stderr);
        Ok(result)
    }
    
    fn execute_csharp(&self, code: &str) -> Result<ExecResult, ExecutorError> {
        let script_path = self.temp_dir.join("script.csx");
        
        fs::write(&script_path, code)
            .map_err(|e| ExecutorError {
                message: format!("Failed to write script: {}", e),
            })?;
        
        let mut cmd = Command::new("dotnet");
        cmd.args(["script", script_path.to_str().unwrap()])
            .current_dir(&self.config.project_root)
            .stdout(Stdio::piped())
            .stderr(Stdio::piped());
        self.run_prepared_command(cmd)
    }

    fn execute_fsharp(&self, code: &str) -> Result<ExecResult, ExecutorError> {
        let script_path = self.temp_dir.join("script.fsx");
        
        fs::write(&script_path, code)
            .map_err(|e| ExecutorError {
                message: format!("Failed to write script: {}", e),
            })?;
        
        let mut cmd = Command::new("dotnet");
        cmd.args(["fsi", script_path.to_str().unwrap()])
            .current_dir(&self.config.project_root)
            .stdout(Stdio::piped())
            .stderr(Stdio::piped());
        self.run_prepared_command(cmd)
    }
    
    fn execute_python(&self, code: &str) -> Result<ExecResult, ExecutorError> {
        let script_path = self.temp_dir.join("script.py");
        
        fs::write(&script_path, code)
            .map_err(|e| ExecutorError {
                message: format!("Failed to write script: {}", e),
            })?;
        
        let mut cmd = Command::new("python");
        cmd.arg(script_path.to_str().unwrap())
            .current_dir(&self.config.project_root)
            .stdout(Stdio::piped())
            .stderr(Stdio::piped());
        self.run_prepared_command(cmd)
    }
    
    fn execute_shell(&self, code: &str) -> Result<ExecResult, ExecutorError> {
        let (shell, arg) = if cfg!(windows) {
            ("cmd", "/C")
        } else {
            ("sh", "-c")
        };
        
        let mut cmd = Command::new(shell);
        cmd.args([arg, code])
            .current_dir(&self.config.project_root)
            .stdout(Stdio::piped())
            .stderr(Stdio::piped());
        self.run_prepared_command(cmd)
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

use std::ffi::{CString, CStr};
use std::os::raw::{c_char, c_int};

fn run_command_with_timeout(mut cmd: Command, timeout_ms: u64) -> Result<ExecResult, ExecutorError> {
    let (tx, rx) = mpsc::sync_channel(1);
    thread::spawn(move || {
        let result = cmd.output();
        let _ = tx.send(result);
    });

    match rx.recv_timeout(Duration::from_millis(timeout_ms)) {
        Ok(Ok(output)) => Ok(ExecResult {
            stdout: String::from_utf8_lossy(&output.stdout).to_string(),
            stderr: String::from_utf8_lossy(&output.stderr).to_string(),
            exit_code: output.status.code().unwrap_or(-1),
            timed_out: false,
        }),
        Ok(Err(e)) => Err(ExecutorError {
            message: format!("Execution failed: {e}"),
        }),
        Err(mpsc::RecvTimeoutError::Timeout) => Ok(ExecResult {
            stdout: String::new(),
            stderr: "Execution timed out".to_string(),
            exit_code: -1,
            timed_out: true,
        }),
        Err(mpsc::RecvTimeoutError::Disconnected) => Err(ExecutorError {
            message: "Execution worker disconnected".to_string(),
        }),
    }
}

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

#[no_mangle]
pub extern "C" fn executor_run_shell(
    project_root: *const c_char,
    command: *const c_char,
    timeout_ms: u64,
    max_output_bytes: usize,
    out_stdout: *mut *mut c_char,
    out_stderr: *mut *mut c_char,
    out_exit_code: *mut c_int,
    out_timed_out: *mut bool,
) -> c_int {
    if project_root.is_null() || command.is_null() {
        return -1;
    }

    let root = unsafe { CStr::from_ptr(project_root).to_string_lossy().into_owned() };
    let cmd = unsafe { CStr::from_ptr(command).to_string_lossy().into_owned() };

    let config = SandboxConfig {
        timeout_ms,
        max_output_bytes,
        project_root: root,
    };

    match PolyglotExecutor::new(config) {
        Ok(executor) => match executor.execute("shell", &cmd) {
            Ok(result) => {
                unsafe {
                    *out_stdout = CString::new(result.stdout).unwrap().into_raw();
                    *out_stderr = CString::new(result.stderr).unwrap().into_raw();
                    *out_exit_code = result.exit_code;
                    *out_timed_out = result.timed_out;
                }
                0
            }
            Err(_) => -2,
        },
        Err(_) => -3,
    }
}
