use sha2::{Sha256, Digest};
use rand::Rng;
use base64::{Engine as _, engine::general_purpose};

#[no_mangle]
pub extern "C" fn generate_salt() -> *mut std::ffi::c_char {
    let mut rng = rand::thread_rng();
    let salt: [u8; 16] = rng.gen();
    let salt_b64 = general_purpose::STANDARD.encode(&salt);
    std::ffi::CString::new(salt_b64).unwrap().into_raw()
}

#[no_mangle]
pub extern "C" fn hash_password(password: *const std::ffi::c_char, salt: *const std::ffi::c_char) -> *mut std::ffi::c_char {
    let password = unsafe { std::ffi::CStr::from_ptr(password) }.to_str().unwrap();
    let salt = unsafe { std::ffi::CStr::from_ptr(salt) }.to_str().unwrap();
    let salt_bytes = general_purpose::STANDARD.decode(salt).unwrap();

    let mut hasher = Sha256::new();
    hasher.update(password.as_bytes());
    hasher.update(&salt_bytes);
    let hash = hasher.finalize();
    let hash_b64 = general_purpose::STANDARD.encode(&hash);
    std::ffi::CString::new(hash_b64).unwrap().into_raw()
}

#[no_mangle]
pub extern "C" fn verify_password(password: *const std::ffi::c_char, salt: *const std::ffi::c_char, hash: *const std::ffi::c_char) -> bool {
    let password = unsafe { std::ffi::CStr::from_ptr(password) }.to_str().unwrap();
    let salt = unsafe { std::ffi::CStr::from_ptr(salt) }.to_str().unwrap();
    let expected_hash = unsafe { std::ffi::CStr::from_ptr(hash) }.to_str().unwrap();

    let computed_hash = unsafe { std::ffi::CStr::from_ptr(hash_password(password.as_ptr() as *const std::ffi::c_char, salt.as_ptr() as *const std::ffi::c_char)) }.to_str().unwrap();
    computed_hash == expected_hash
}

#[no_mangle]
pub extern "C" fn free_string(s: *mut std::ffi::c_char) {
    unsafe {
        if !s.is_null() {
            drop(std::ffi::CString::from_raw(s));
        }
    }
}