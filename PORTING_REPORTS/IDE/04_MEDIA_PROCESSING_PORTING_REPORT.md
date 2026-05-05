# Отчёт о портировании: media_processing.py

## 📊 Общая информация

| Параметр | Значение |
|----------|----------|
| **Исходный файл** | `D:\Desktop\freelance_libr4-main\backend\app\api\endpoints\media_processing.py` |
| **Размер** | 99.2 KB (2,558 строк) |
| **Язык оригинала** | Python 3.11 + FastAPI + PyTorch |
| **Целевой язык** | C# / Rust |
| **Сложность** | 🔴 **КРИТИЧЕСКАЯ** |
| **Оценка времени** | 3-4 недели |

---

## 📋 Что содержит оригинал

### Image Generation (Stable Diffusion)
```python
# PyTorch + diffusers
from diffusers import StableDiffusionPipeline, DiffusionPipeline
import torch

# SD 1.5, SDXL, Flux
models = {
    "sd-1.5": "runwayml/stable-diffusion-v1-5",
    "sdxl": "stabilityai/stable-diffusion-xl-base-1.0",
    "flux": "black-forest-labs/FLUX.1-schnell"
}

pipe = StableDiffusionPipeline.from_pretrained(model_id, torch_dtype=torch.float16)
pipe = pipe.to("cuda")

image = pipe(prompt, num_inference_steps=50).images[0]
```

### Video Generation
```python
# AnimateDiff, SVD (Stable Video Diffusion)
from diffusers import AnimateDiffPipeline

# Motion + image → video
video_frames = pipe(
    prompt,
    num_frames=16,
    num_inference_steps=25
).frames
```

### Audio Generation
```python
# Whisper (STT), ElevenLabs (TTS), MusicGen, Suno
import whisper
model = whisper.load_model("base")
result = model.transcribe(audio_file)

# MusicGen
from transformers import AutoProcessor, MusicgenForConditionalGeneration
```

### 3D Generation
```python
# TripoSR, ShapE
triposr = TripoSR.from_pretrained("stabilityai/TripoSR")
mesh = triposr(image, output_size=1024)
```

---

## ❌ Почему НЕ C# для Media (только Rust!)

### Проблема C# / Python:
- PyTorch C# bindings (TorchSharp) - ограничены
- ONNX Runtime - только inference
- Отсутствие diffusers экосистемы

### Решение: PURE RUST ✅

```
Rust ML Ecosystem (2024):
├── candle (HuggingFace in Rust) - нативный Rust
├── tract-onnx - ONNX inference
├── tch-rs - PyTorch C++ bindings
├── burn - ML framework (Rust)
├── llm-rs - LLM inference
└── whisper-rs - OpenAI Whisper

Преимущества:
- Нативная производительность
- Нет Python overhead
- CUDA через tch-rs (libtorch C++)
- candle - чистый Rust для transformer
```

---

## ✅ Решение: Pure Rust (NO Python!)

### Архитектура:

```
┌─────────────────────────────────────────┐
│           C# AI API Service             │
│  (Gateway, Auth, Routing, Billing)    │
└──────────────┬──────────────────────────┘
               │ gRPC / HTTP
               ▼
┌─────────────────────────────────────────┐
│      Rust Media Service                 │
│  (candle + tract-onnx + tch-rs)         │
│                                         │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐   │
│  │   SD    │ │  Video  │ │  Audio  │   │
│  │ Candle  │ │Candle   │ │Whisper │   │
│  └─────────┘ └─────────┘ └─────────┘   │
└─────────────────────────────────────────┘
               │
               ▼ CUDA
        ┌─────────────┐
        │  GPU Server │
        │  (RTX 4090) │
        └─────────────┘
```

### Вариант B: C# + ONNX Runtime

```
C# API
  │
  ▼ gRPC
┌────────────────────────────────────┐
│  Rust Media Service (libr4-media)  │
│  ├── Image: Stable Diffusion FFI   │
│  ├── Video: AnimateDiff FFI        │
│  └── Audio: Whisper (Rust)         │
│                                    │
│  (вызов Python через PyO3/FFI)     │
└────────────────────────────────────┘
```

---

## 🔧 Rust модули (уже начаты)

### Существующие скелеты:
```
src/Services/AI/Rust/
├── libr4-media-processing/
│   └── src/lib.rs (structs only)
├── libr4-media-3d/
│   └── src/lib.rs (structs only)
└── libr4-audio/
    └── src/lib.rs (structs only)
```

**Проблема:** Только структуры, нет реализации!

### Что нужно добавить в Rust:
```rust
// libr4-media-processing
use pyo3::prelude::*;
use tch::{nn, Device, Tensor}; // Rust Torch bindings

#[pyfunction]
fn generate_image_sdxl(prompt: &str, steps: i64) -> PyResult<Vec<u8>> {
    // Загрузка Python pipeline через PyO3
    Python::with_gil(|py| {
        let diffusers = py.import("diffusers")?;
        let pipe = diffusers.getattr("StableDiffusionXLPipeline")?;
        // ...
    })
}
```

---

## 📁 Что нужно создать

### C# Domain:
```csharp
ImageGenerationRequest
ImageGenerationResult
VideoGenerationRequest
AudioGenerationRequest
GenerationJob
```

### C# Application:
```csharp
GenerateImageCommand → calls Rust gRPC
GenerateVideoCommand → calls Rust gRPC
GenerateAudioCommand → calls Rust gRPC
GetGenerationJobQuery
```

### Rust Infrastructure (НОВОЕ!):
```rust
// src/Services/AI/Rust/libr4-media-processing/src/
pub mod image_generation;    // candle + Stable Diffusion
pub mod video_generation;    // candle + AnimateDiff
pub mod text_to_image;       // Flux, SDXL

use candle_core::{Device, Tensor};
use candle_transformers::models::stable_diffusion;

pub fn generate_image_sdxl(
    prompt: &str,
    model_path: &str,
) -> Result<Vec<u8>, MediaError> {
    let device = Device::cuda_if_available(0)?;
    let pipeline = StableDiffusionPipeline::new(
        model_path,
        &device
    )?;
    let image = pipeline.generate(prompt, 50)?;
    Ok(image.encode_png()?)
}
```

### C# Infrastructure:
```csharp
// gRPC client to Rust Media Service
public class RustMediaClient : IMediaClient
{
    private readonly MediaGrpc.MediaGrpcClient _grpc;
    
    public async Task<ImageResult> GenerateImageAsync(
        string prompt, string model)
    {
        var request = new GenerateImageRequest { Prompt = prompt };
        return await _grpc.GenerateImageAsync(request);
    }
}
```

### API:
```csharp
// Endpoints:
POST   /api/v1/ai/media/images              // Generate image
GET    /api/v1/ai/media/images/{id}         // Get result
POST   /api/v1/ai/media/videos              // Generate video
GET    /api/v1/ai/media/videos/{id}         // Get result
POST   /api/v1/ai/media/audio               // Generate audio
POST   /api/v1/ai/media/transcribe          // STT
POST   /api/v1/ai/media/tts                 // TTS
GET    /api/v1/ai/media/jobs/{id}           // Job status
```

---

## 🛠️ Технические детали (PURE RUST)

### Rust Media Service (НОВОЕ!)
```rust
// Cargo.toml
[dependencies]
candle-core = "0.3"
candle-transformers = "0.3"
candle-nn = "0.3"
tract-onnx = "0.21"
tch = "0.13"  # PyTorch C++ bindings
whisper-rs = "0.8"
tokio = { version = "1", features = ["full"] }
tonic = "0.10"  # gRPC

[features]
default = ["cuda"]
cuda = ["candle-core/cuda", "tch/cuda"]
```

### Rust gRPC Server
```rust
// src/server.rs
use tonic::{transport::Server, Request, Response, Status};

pub mod media {
    tonic::include_proto!("media");
}

#[derive(Debug, Default)]
pub struct MediaService;

#[tonic::async_trait]
impl media::media_server::Media for MediaService {
    async fn generate_image(
        &self,
        request: Request<GenerateImageRequest>,
    ) -> Result<Response<GenerateImageResponse>, Status> {
        let req = request.into_inner();
        
        let image = image_generation::generate_sdxl(&req.prompt)
            .map_err(|e| Status::internal(e.to_string()))?;
            
        Ok(Response::new(GenerateImageResponse {
            image_data: image,
            format: "png".to_string(),
        }))
    }
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let addr = "[::1]:50051".parse()?;
    let service = MediaService::default();
    
    Server::builder()
        .add_service(media::media_server::MediaServer::new(service))
        .serve(addr)
        .await?;
        
    Ok(())
}
```

### C# gRPC Client
```csharp
// GrpcMediaClient.cs
public class GrpcMediaClient : IMediaClient
{
    private readonly MediaGrpc.MediaGrpcClient _client;
    
    public async Task<byte[]> GenerateImageAsync(string prompt)
    {
        var request = new GenerateImageRequest { Prompt = prompt };
        var response = await _client.GenerateImageAsync(request);
        return response.ImageData.ToByteArray();
    }
}
```

### Docker Compose (Rust GPU)
```yaml
media-service:
  image: libr4-media-rust:latest
  build:
    context: ./src/Services/AI/Rust/libr4-media-service
    dockerfile: Dockerfile.gpu
  runtime: nvidia
  environment:
    - CUDA_VISIBLE_DEVICES=0
    - RUST_LOG=info
  volumes:
    - models-cache:/models
```

---

## 📊 Рекомендации (NO Python!)

### Чистый Rust ✅
- **candle** - HuggingFace models in pure Rust
- **tch-rs** - PyTorch C++ bindings (no Python!)
- **tract-onnx** - ONNX inference
- **whisper-rs** - Speech recognition

### Почему Rust, а не Python:
- Нативная производительность (no GIL!)
- Меньше memory overhead
- Безопасность типов
- Легче deployment (один бинарник)
- CUDA через libtorch C++ (без Python)

---

## 🎯 Acceptance Criteria

- [ ] Rust gRPC media service (candle + tch-rs)
- [ ] Stable Diffusion (SD, SDXL, Flux) на Rust
- [ ] Video generation на Rust
- [ ] Audio (Whisper, TTS) на Rust
- [ ] C# gRPC client
- [ ] Job tracking
- [ ] GPU поддержка через CUDA

---

**Вывод:** Полный порт на Rust (candle/tch-rs) БЕЗ Python!

**Статус:** � ТРЕБУЕТ РАЗРАБОТКИ (сложно, но возможно)

**Время:** 6-8 недель для полного Rust Media Service
