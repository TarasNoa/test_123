use std::collections::HashMap;
use serde::{Serialize, Deserialize};
use crc::{Crc, CRC_32_CKSUM};

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct OTOperation {
    pub op_type: String,
    pub position: usize,
    pub content: String,
    pub user_id: String,
    pub timestamp: u64,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct DocumentState {
    pub content: String,
    pub version: u32,
    pub checksum: u32,
}

pub struct OperationalTransform {
    document: DocumentState,
    pending_ops: Vec<OTOperation>,
}

impl OperationalTransform {
    pub fn new(content: String) -> Self {
        let checksum = Self::calculate_checksum(&content);
        let document = DocumentState {
            content,
            version: 0,
            checksum,
        };
        OperationalTransform {
            document,
            pending_ops: Vec::new(),
        }
    }

    pub fn apply_operation(&mut self, op: OTOperation) -> Result<String, String> {
        match op.op_type.as_str() {
            "insert" => {
                if op.position > self.document.content.len() {
                    return Err("Position out of bounds".to_string());
                }
                self.document.content.insert_str(op.position, &op.content);
                self.document.version += 1;
                self.document.checksum = Self::calculate_checksum(&self.document.content);
                Ok(self.document.content.clone())
            }
            "delete" => {
                let end_pos = op.position + op.content.len();
                if end_pos > self.document.content.len() {
                    return Err("Delete range out of bounds".to_string());
                }
                self.document.content.drain(op.position..end_pos);
                self.document.version += 1;
                self.document.checksum = Self::calculate_checksum(&self.document.content);
                Ok(self.document.content.clone())
            }
            "replace" => {
                let end_pos = op.position + op.content.len();
                if end_pos > self.document.content.len() {
                    return Err("Replace range out of bounds".to_string());
                }
                self.document.content.drain(op.position..end_pos);
                self.document.content.insert_str(op.position, &op.content);
                self.document.version += 1;
                self.document.checksum = Self::calculate_checksum(&self.document.content);
                Ok(self.document.content.clone())
            }
            _ => Err("Unknown operation type".to_string()),
        }
    }

    pub fn transform_operations(op1: &OTOperation, op2: &OTOperation) -> (OTOperation, OTOperation) {
        let mut t_op1 = op1.clone();
        let mut t_op2 = op2.clone();

        match (op1.op_type.as_str(), op2.op_type.as_str()) {
            ("insert", "insert") => {
                if op1.position < op2.position {
                    t_op2.position += op1.content.len();
                } else if op1.position > op2.position {
                    t_op1.position += op2.content.len();
                }
            }
            ("delete", "insert") => {
                if op2.position <= op1.position {
                    t_op1.position = op1.position.saturating_sub(op1.content.len());
                } else if op2.position < op1.position + op1.content.len() {
                    t_op1.position = op1.position;
                }
            }
            ("insert", "delete") => {
                if op1.position <= op2.position {
                    t_op2.position = op2.position.saturating_sub(op1.content.len());
                } else if op1.position < op2.position + op2.content.len() {
                    t_op2.position = op2.position;
                }
            }
            ("delete", "delete") => {
                if op1.position < op2.position {
                    t_op2.position = op2.position.saturating_sub(op1.content.len());
                } else if op1.position > op2.position {
                    let reduction = op1.position.saturating_sub(op2.position + op2.content.len());
                    t_op1.position = op2.position + reduction;
                }
            }
            _ => {}
        }

        (t_op1, t_op2)
    }

    fn calculate_checksum(content: &str) -> u32 {
        let crc = Crc::<u32>::new(&CRC_32_CKSUM);
        let mut digest = crc.digest();
        digest.update(content.as_bytes());
        digest.finalize()
    }

    pub fn get_state(&self) -> DocumentState {
        self.document.clone()
    }
}

#[no_mangle]
pub extern "C" fn create_ot() -> *mut OperationalTransform {
    let ot = OperationalTransform::new(String::new());
    Box::into_raw(Box::new(ot))
}

#[no_mangle]
pub extern "C" fn apply_operation(
    ot_ptr: *mut OperationalTransform,
    op_type: *const std::ffi::c_char,
    position: usize,
    content: *const std::ffi::c_char,
    user_id: *const std::ffi::c_char,
) -> *mut std::ffi::c_char {
    if ot_ptr.is_null() {
        return std::ptr::null_mut();
    }

    let ot = unsafe { &mut *ot_ptr };
    let op_type_str = unsafe { std::ffi::CStr::from_ptr(op_type) }
        .to_str()
        .unwrap_or("");
    let content_str = unsafe { std::ffi::CStr::from_ptr(content) }
        .to_str()
        .unwrap_or("");
    let user_id_str = unsafe { std::ffi::CStr::from_ptr(user_id) }
        .to_str()
        .unwrap_or("");

    let operation = OTOperation {
        op_type: op_type_str.to_string(),
        position,
        content: content_str.to_string(),
        user_id: user_id_str.to_string(),
        timestamp: std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_secs(),
    };

    match ot.apply_operation(operation) {
        Ok(result) => {
            std::ffi::CString::new(result).unwrap().into_raw()
        }
        Err(e) => {
            std::ffi::CString::new(format!("Error: {}", e))
                .unwrap()
                .into_raw()
        }
    }
}

#[no_mangle]
pub extern "C" fn free_ot(ot_ptr: *mut OperationalTransform) {
    if !ot_ptr.is_null() {
        unsafe {
            drop(Box::from_raw(ot_ptr));
        }
    }
}

#[no_mangle]
pub extern "C" fn free_string(s: *mut std::ffi::c_char) {
    if !s.is_null() {
        unsafe {
            drop(std::ffi::CString::from_raw(s));
        }
    }
}