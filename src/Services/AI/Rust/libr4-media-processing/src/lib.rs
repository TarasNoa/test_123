use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum ImageFormat {
    PNG,
    JPEG,
    WebP,
    GIF,
    BMP,
    TIFF,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum VideoFormat {
    MP4,
    WebM,
    AVI,
    MOV,
    MKV,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum GenerationModel {
    StableDiffusion,
    StableDiffusionXL,
    Flux,
    DallE3,
    Midjourney,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum GenerationStatus {
    Pending,
    Generating,
    Completed,
    Failed,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Image {
    pub id: String,
    pub path: String,
    pub format: ImageFormat,
    pub width: u32,
    pub height: u32,
    pub file_size: u64,
    pub color_space: String,
    pub has_transparency: bool,
    pub created_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Video {
    pub id: String,
    pub path: String,
    pub format: VideoFormat,
    pub duration_seconds: u32,
    pub width: u32,
    pub height: u32,
    pub fps: u32,
    pub bitrate: u32,
    pub codec: String,
    pub audio_track: Option<String>,
    pub file_size: u64,
    pub created_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ImageGeneration {
    pub id: String,
    pub user_id: String,
    pub prompt: String,
    pub negative_prompt: Option<String>,
    pub model: GenerationModel,
    pub width: u32,
    pub height: u32,
    pub steps: u32,
    pub guidance_scale: f32,
    pub seed: Option<i64>,
    pub status: GenerationStatus,
    pub result_image_id: Option<String>,
    pub cost: f64,
    pub generation_time_ms: u32,
    pub created_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ImageProcessingJob {
    pub id: String,
    pub source_image_id: String,
    pub operation: String, 
    pub parameters: HashMap<String, String>,
    pub result_image_id: Option<String>,
    pub status: GenerationStatus,
    pub created_at: u64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct VideoProcessingJob {
    pub id: String,
    pub source_video_id: String,
    pub operation: String, 
    pub parameters: HashMap<String, String>,
    pub result_video_id: Option<String>,
    pub status: GenerationStatus,
    pub progress_percent: f32,
    pub created_at: u64,
}

impl Image {
    pub fn aspect_ratio(&self) -> f32 {
        if self.height > 0 {
            self.width as f32 / self.height as f32
        } else {
            0.0
        }
    }

    pub fn megapixels(&self) -> f32 {
        (self.width * self.height) as f32 / 1_000_000.0
    }
}

impl Video {
    pub fn total_frames(&self) -> u32 {
        self.duration_seconds * self.fps
    }
}
