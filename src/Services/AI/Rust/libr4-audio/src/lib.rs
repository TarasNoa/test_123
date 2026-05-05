use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum AudioFormat {
    MP3,
    WAV,
    OGG,
    FLAC,
    AAC,
    Opus,
    M4A,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum TTSModel {
    ElevenLabs,
    OpenAITTS,
    Coqui,
    Bark,
    XTTS,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum STTModel {
    Whisper,
    WhisperLarge,
    DeepSpeech,
    Wav2Vec,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum MusicGenerationModel {
    MusicGen,
    AudioLDM,
    Jukebox,
    Suno,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Audio {
    pub id: String,
    pub path: String,
    pub format: AudioFormat,
    pub duration_seconds: u32,
    pub sample_rate: u32,
    pub bitrate: u32,
    pub channels: u8,
    pub file_size: u64,
    pub created_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TTSRequest {
    pub id: String,
    pub user_id: String,
    pub text: String,
    pub voice: String,
    pub language: String,
    pub model: TTSModel,
    pub speed: f32,
    pub pitch: f32,
    pub status: String,
    pub result_audio_id: Option<String>,
    pub processing_time_ms: u32,
    pub created_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct STTRequest {
    pub id: String,
    pub user_id: String,
    pub audio_id: String,
    pub model: STTModel,
    pub language: Option<String>,
    pub status: String,
    pub transcription: Option<String>,
    pub confidence: f32,
    pub word_timestamps: Vec<WordTimestamp>,
    pub processing_time_ms: u32,
    pub created_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WordTimestamp {
    pub word: String,
    pub start_seconds: f32,
    pub end_seconds: f32,
    pub confidence: f32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MusicGeneration {
    pub id: String,
    pub user_id: String,
    pub prompt: String,
    pub model: MusicGenerationModel,
    pub duration_seconds: u32,
    pub genre: Option<String>,
    pub tempo: Option<u32>,
    pub key: Option<String>,
    pub status: String,
    pub result_audio_id: Option<String>,
    pub created_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct VoiceClone {
    pub id: String,
    pub user_id: String,
    pub name: String,
    pub sample_audio_ids: Vec<String>,
    pub model_path: String,
    pub quality_score: f32,
    pub is_trained: bool,
    pub created_at: u64,
}

impl Audio {
    pub fn bitrate_mbps(&self) -> f64 {
        self.bitrate as f64 / 1_000_000.0
    }

    pub fn is_stereo(&self) -> bool {
        self.channels >= 2
    }
}
