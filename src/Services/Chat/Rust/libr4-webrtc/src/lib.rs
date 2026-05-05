use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum CallType { Audio, Video, ScreenShare }

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum CallStatus { Initiating, Ringing, Connected, OnHold, Ended, Failed }

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WebRTCCall {
    pub id: String,
    pub initiator_id: String,
    pub recipient_id: String,
    pub call_type: CallType,
    pub status: CallStatus,
    pub started_at: u64,
    pub ended_at: Option<u64>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ICECandidate {
    pub id: String,
    pub call_id: String,
    pub candidate: String,
    pub sdp_mid: String,
    pub sdp_mline_index: u32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SDPOffer {
    pub id: String,
    pub call_id: String,
    pub from: String,
    pub sdp: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SDPAnswer {
    pub id: String,
    pub call_id: String,
    pub from: String,
    pub sdp: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CallStatistics {
    pub call_id: String,
    pub duration_seconds: u64,
    pub packet_loss: f64,
    pub latency_ms: u32,
    pub bandwidth_mbps: f64,
}

impl WebRTCCall {
    pub fn new(initiator_id: String, recipient_id: String, call_type: CallType) -> Self {
        WebRTCCall {
            id: uuid::Uuid::new_v4().to_string(),
            initiator_id,
            recipient_id,
            call_type,
            status: CallStatus::Initiating,
            started_at: std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap()
                .as_secs(),
            ended_at: None,
        }
    }

    pub fn connect(&mut self) {
        self.status = CallStatus::Connected;
    }

    pub fn end(&mut self) {
        self.status = CallStatus::Ended;
        self.ended_at = Some(
            std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap()
                .as_secs(),
        );
    }

    pub fn duration(&self) -> u64 {
        let end = self.ended_at.unwrap_or_else(|| {
            std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap()
                .as_secs()
        });
        end - self.started_at
    }
}
