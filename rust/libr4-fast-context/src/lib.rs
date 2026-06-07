use grep_regex::RegexMatcherBuilder;
use grep_searcher::{Searcher, SearcherBuilder, Sink, SinkMatch};
use ignore::WalkBuilder;
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};
use std::ffi::{CStr, CString};
use std::fs;
use std::io;
use std::os::raw::c_char;
use std::path::Path;

const MAX_MATCHES_DEFAULT: usize = 120;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SearchHit {
    pub path: String,
    pub start_line: i32,
    pub end_line: i32,
    pub score: f64,
    pub snippet: String,
    pub match_kind: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct IndexedFile {
    pub relative_path: String,
    pub content_hash: String,
    pub size_bytes: i64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct IndexManifest {
    pub workspace_root: String,
    pub workspace_hash: String,
    pub file_count: i32,
    pub files: Vec<IndexedFile>,
}

#[derive(Debug, Deserialize)]
pub struct SearchRequest {
    pub workspace_root: String,
    pub query: String,
    #[serde(default = "default_max_matches")]
    pub max_matches: usize,
    #[serde(default)]
    pub include_tests: bool,
    pub languages: Option<Vec<String>>,
}

fn default_max_matches() -> usize {
    MAX_MATCHES_DEFAULT
}

pub fn search_workspace(
    workspace_root: &str,
    query: &str,
    max_matches: usize,
    include_tests: bool,
    languages: Option<&[String]>,
) -> Result<Vec<SearchHit>, String> {
    if query.trim().is_empty() {
        return Ok(Vec::new());
    }

    let root = Path::new(workspace_root);
    if !root.is_dir() {
        return Err(format!("workspace root does not exist: {workspace_root}"));
    }

    let matcher = RegexMatcherBuilder::new()
        .case_insensitive(true)
        .build(query)
        .map_err(|e| format!("invalid query regex: {e}"))?;

    let mut hits = Vec::new();
    let mut searcher = SearcherBuilder::new().line_number(true).build();

    let walker = WalkBuilder::new(workspace_root)
        .hidden(true)
        .git_ignore(true)
        .git_global(true)
        .git_exclude(true)
        .build();

    for entry in walker.flatten() {
        if hits.len() >= max_matches {
            break;
        }

        let path = entry.path();
        if !path.is_file() {
            continue;
        }

        let rel = path
            .strip_prefix(root)
            .map(|p| p.to_string_lossy().replace('\\', "/"))
            .unwrap_or_else(|_| path.to_string_lossy().replace('\\', "/"));

        if should_skip(&rel) {
            continue;
        }
        if !include_tests && is_test_path(&rel) {
            continue;
        }
        if let Some(langs) = languages {
            if !passes_language_filter(&rel, langs) {
                continue;
            }
        }

        let mut sink = HitSink {
            rel_path: rel,
            hits: &mut hits,
            max_matches,
        };
        let _ = searcher.search_path(&matcher, path, &mut sink);
    }

    Ok(hits)
}

pub fn build_manifest(workspace_root: &str) -> Result<IndexManifest, String> {
    let root = Path::new(workspace_root);
    if !root.is_dir() {
        return Err(format!("workspace root does not exist: {workspace_root}"));
    }

    let mut files = Vec::new();
    let walker = WalkBuilder::new(workspace_root)
        .hidden(true)
        .git_ignore(true)
        .git_global(true)
        .git_exclude(true)
        .build();

    for entry in walker.flatten() {
        let path = entry.path();
        if !path.is_file() {
            continue;
        }

        let rel = path
            .strip_prefix(root)
            .map(|p| p.to_string_lossy().replace('\\', "/"))
            .unwrap_or_else(|_| path.to_string_lossy().replace('\\', "/"));

        if should_skip(&rel) {
            continue;
        }

        let metadata = fs::metadata(path).map_err(|e| e.to_string())?;
        let hash = sha256_file(path)?;
        files.push(IndexedFile {
            relative_path: rel,
            content_hash: hash,
            size_bytes: metadata.len() as i64,
        });
    }

    files.sort_by(|a, b| a.relative_path.cmp(&b.relative_path));

    Ok(IndexManifest {
        workspace_root: workspace_root.to_string(),
        workspace_hash: hash_workspace(workspace_root),
        file_count: files.len() as i32,
        files,
    })
}

struct HitSink<'a> {
    rel_path: String,
    hits: &'a mut Vec<SearchHit>,
    max_matches: usize,
}

impl Sink for HitSink<'_> {
    type Error = io::Error;

    fn matched(&mut self, _searcher: &Searcher, mat: &SinkMatch<'_>) -> Result<bool, Self::Error> {
        if self.hits.len() >= self.max_matches {
            return Ok(false);
        }

        let line_number = mat.line_number().unwrap_or(1) as i32;
        let snippet = String::from_utf8_lossy(mat.bytes()).trim_end().to_string();
        self.hits.push(SearchHit {
            path: self.rel_path.clone(),
            start_line: line_number,
            end_line: line_number,
            score: 1.0,
            snippet,
            match_kind: "rust_fast_context".to_string(),
        });
        Ok(true)
    }
}

fn should_skip(path: &str) -> bool {
    let p = path.replace('\\', "/").to_lowercase();
    p.contains("/node_modules/")
        || p.contains("/.git/")
        || p.contains("/dist/")
        || p.contains("/.venv/")
        || p.contains("/bin/")
        || p.contains("/obj/")
}

fn is_test_path(path: &str) -> bool {
    let p = path.replace('\\', "/");
    p.contains("/test/")
        || p.contains("/tests/")
        || p.ends_with("_test.py")
        || p.ends_with(".spec.ts")
        || p.ends_with(".test.ts")
}

fn passes_language_filter(path: &str, languages: &[String]) -> bool {
    let ext = Path::new(path)
        .extension()
        .and_then(|e| e.to_str())
        .unwrap_or("")
        .trim_start_matches('.')
        .to_lowercase();

    languages
        .iter()
        .any(|l| ext == l.trim_start_matches('.').to_lowercase())
}

fn sha256_file(path: &Path) -> Result<String, String> {
    let bytes = fs::read(path).map_err(|e| e.to_string())?;
    let digest = Sha256::digest(bytes);
    Ok(hex::encode(digest))
}

fn hash_workspace(workspace_root: &str) -> String {
    let canonical = fs::canonicalize(workspace_root)
        .map(|p| p.to_string_lossy().to_string())
        .unwrap_or_else(|_| workspace_root.to_string());
    let digest = Sha256::digest(canonical.as_bytes());
    hex::encode(&digest[..8])
}

fn write_json_string(value: &impl Serialize) -> Result<CString, String> {
    let json = serde_json::to_string(value).map_err(|e| e.to_string())?;
    CString::new(json).map_err(|e| e.to_string())
}

#[no_mangle]
pub extern "C" fn fast_context_search_json(
    request_json: *const c_char,
    out_json: *mut *mut c_char,
) -> i32 {
    if request_json.is_null() || out_json.is_null() {
        return -1;
    }

    let request_str = unsafe { CStr::from_ptr(request_json).to_string_lossy() };
    let request: SearchRequest = match serde_json::from_str(&request_str) {
        Ok(r) => r,
        Err(e) => {
            if let Ok(err) = write_json_string(&serde_json::json!({ "error": e.to_string() })) {
                unsafe { *out_json = err.into_raw() };
            }
            return -2;
        }
    };

    let langs = request.languages.as_deref();
    match search_workspace(
        &request.workspace_root,
        &request.query,
        request.max_matches,
        request.include_tests,
        langs,
    ) {
        Ok(hits) => match write_json_string(&hits) {
            Ok(json) => {
                unsafe { *out_json = json.into_raw() };
                0
            }
            Err(_) => -3,
        },
        Err(e) => {
            if let Ok(err) = write_json_string(&serde_json::json!({ "error": e })) {
                unsafe { *out_json = err.into_raw() };
            }
            -4
        }
    }
}

#[no_mangle]
pub extern "C" fn fast_context_build_manifest_json(
    workspace_root: *const c_char,
    out_json: *mut *mut c_char,
) -> i32 {
    if workspace_root.is_null() || out_json.is_null() {
        return -1;
    }

    let root = unsafe { CStr::from_ptr(workspace_root).to_string_lossy().into_owned() };
    match build_manifest(&root) {
        Ok(manifest) => match write_json_string(&manifest) {
            Ok(json) => {
                unsafe { *out_json = json.into_raw() };
                0
            }
            Err(_) => -2,
        },
        Err(e) => {
            if let Ok(err) = write_json_string(&serde_json::json!({ "error": e })) {
                unsafe { *out_json = err.into_raw() };
            }
            -3
        }
    }
}

#[no_mangle]
pub extern "C" fn fast_context_free_string(s: *mut c_char) {
    if !s.is_null() {
        unsafe {
            let _ = CString::from_raw(s);
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::fs;
    use std::time::{SystemTime, UNIX_EPOCH};

    #[test]
    fn search_finds_query_in_workspace() {
        let stamp = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let root = std::env::temp_dir().join(format!("libr4-fc-{stamp}"));
        fs::create_dir_all(&root).unwrap();
        fs::write(root.join("models.py"), "class User:\n    pass\n").unwrap();

        let hits = search_workspace(root.to_str().unwrap(), "class User", 10, true, None).unwrap();
        assert!(!hits.is_empty());
        assert!(hits[0].path.contains("models.py"));

        let _ = fs::remove_dir_all(&root);
    }
}
