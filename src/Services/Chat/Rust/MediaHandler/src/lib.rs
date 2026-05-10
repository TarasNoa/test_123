use std::collections::HashMap;
use tokio::sync::Mutex;
use webrtc::api::APIBuilder;
use webrtc::peer_connection::RTCPeerConnection;
use webrtc::ice_transport::ice_candidate::RTCIceCandidate;

lazy_static::lazy_static! {
    static ref PEER_CONNECTIONS: Mutex<HashMap<String, RTCPeerConnection>> = Mutex::new(HashMap::new());
}

#[no_mangle]
pub extern "C" fn create_peer_connection(room_id: *const std::ffi::c_char) -> *mut std::ffi::c_char {
    let room_id = unsafe { std::ffi::CStr::from_ptr(room_id) }.to_str().unwrap();
    let api = APIBuilder::new().build();

    tokio::runtime::Runtime::new().unwrap().block_on(async {
        let peer_connection = api.new_peer_connection(Default::default()).await.unwrap();
        PEER_CONNECTIONS.lock().await.insert(room_id.to_string(), peer_connection);
        std::ffi::CString::new(room_id).unwrap().into_raw()
    })
}

#[no_mangle]
pub extern "C" fn add_ice_candidate(room_id: *const std::ffi::c_char, candidate: *const std::ffi::c_char) {
    let room_id = unsafe { std::ffi::CStr::from_ptr(room_id) }.to_str().unwrap();
    let candidate_str = unsafe { std::ffi::CStr::from_ptr(candidate) }.to_str().unwrap();

    tokio::runtime::Runtime::new().unwrap().block_on(async {
        if let Some(pc) = PEER_CONNECTIONS.lock().await.get_mut(room_id) {
            let ice_candidate: RTCIceCandidate = serde_json::from_str(candidate_str).unwrap();
            pc.add_ice_candidate(ice_candidate).await.unwrap();
        }
    });
}

#[no_mangle]
pub extern "C" fn create_offer(room_id: *const std::ffi::c_char) -> *mut std::ffi::c_char {
    let room_id = unsafe { std::ffi::CStr::from_ptr(room_id) }.to_str().unwrap();

    tokio::runtime::Runtime::new().unwrap().block_on(async {
        if let Some(pc) = PEER_CONNECTIONS.lock().await.get_mut(room_id) {
            let offer = pc.create_offer(None).await.unwrap();
            pc.set_local_description(offer.clone()).await.unwrap();
            let offer_str = serde_json::to_string(&offer).unwrap();
            std::ffi::CString::new(offer_str).unwrap().into_raw()
        } else {
            std::ptr::null_mut()
        }
    })
}

#[no_mangle]
pub extern "C" fn handle_answer(room_id: *const std::ffi::c_char, answer: *const std::ffi::c_char) {
    let room_id = unsafe { std::ffi::CStr::from_ptr(room_id) }.to_str().unwrap();
    let answer_str = unsafe { std::ffi::CStr::from_ptr(answer) }.to_str().unwrap();

    tokio::runtime::Runtime::new().unwrap().block_on(async {
        if let Some(pc) = PEER_CONNECTIONS.lock().await.get_mut(room_id) {
            let answer: webrtc::peer_connection::RTCSessionDescription = serde_json::from_str(answer_str).unwrap();
            pc.set_remote_description(answer).await.unwrap();
        }
    });
}

#[no_mangle]
pub extern "C" fn close_connection(room_id: *const std::ffi::c_char) {
    let room_id = unsafe { std::ffi::CStr::from_ptr(room_id) }.to_str().unwrap();

    tokio::runtime::Runtime::new().unwrap().block_on(async {
        PEER_CONNECTIONS.lock().await.remove(room_id);
    });
}