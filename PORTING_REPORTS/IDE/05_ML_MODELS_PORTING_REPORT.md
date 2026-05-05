# Отчёт о портировании: ml_models.py (PURE C#/F#/RUST - NO Python!)

## 📊 Общая информация

| Параметр | Значение |
|----------|----------|
| **Исходный файл** | `ml_models.py` (32.7 KB, 925 строк) |
| **Статус** | ❌ Не портирован |
| **Целевой стек** | C# + F# + Rust (NO Python!) |

---

## 📋 Содержимое Python (что нужно переписать)

### ML Training Pipeline
```python
# PyTorch training - нужно заменить на:
# C#: TorchSharp (ограниченно) или ONNX Runtime
# Rust: tch-rs (PyTorch C++ bindings) или burn
# F#: Обёртки над C#/Rust

class ModelTrainingService:
    def train_model(self, dataset, config):
        # Training loop
        # Checkpointing
        # Evaluation
        
    def fine_tune(self, base_model, dataset):
        # LoRA fine-tuning
        # QLoRA quantization
```

---

## 🔧 Стратегия портирования БЕЗ Python

### 1. Training (Rust + tch-rs)
```rust
// Rust - tch-rs (PyTorch C++ bindings, без Python!)
use tch::{nn, Device, Tensor, nn::OptimizerConfig};

pub fn train_model(
    dataset: &Dataset,
    config: &TrainingConfig,
) -> Result<Model, TrainingError> {
    let device = Device::cuda_if_available();
    let model = create_model(&config, &device)?;
    let opt = nn::Adam::default().build(&model.variables(), config.lr)?;
    
    for epoch in 0..config.epochs {
        for batch in dataset.batches() {
            let loss = compute_loss(&model, &batch)?;
            opt.backward_step(&loss);
        }
    }
    
    Ok(model)
}
```

### 2. Inference (C# + ONNX Runtime)
```csharp
// C# - ONNX Runtime (high performance)
using Microsoft.ML.OnnxRuntime;

public class OnnxInferenceService
{
    private readonly InferenceSession _session;
    
    public OnnxInferenceService(string modelPath)
    {
        _session = new InferenceSession(modelPath);
    }
    
    public float[] RunInference(float[] input)
    {
        var tensor = new DenseTensor<float>(input, new[] { 1, input.Length });
        var inputNamed = NamedOnnxValue.CreateFromTensor("input", tensor);
        using var results = _session.Run(new[] { inputNamed });
        return results.First().AsTensor<float>().ToArray();
    }
}
```

### 3. Model Management (F#)
```fsharp
// F# - Model registry и versioning
module ModelRegistry =
    type ModelVersion = {
        Id: Guid
        ModelId: string
        Version: string
        Path: string
        Metadata: Map<string, string>
        CreatedAt: DateTimeOffset
    }
    
    let registerModel (path: string) (metadata: Map<string, string>) : Result<ModelVersion, RegistryError> =
        // Functional approach to model registry
        if File.Exists(path) then
            let version = {
                Id = Guid.NewGuid()
                ModelId = metadata["model_id"]
                Version = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss")
                Path = path
                Metadata = metadata
                CreatedAt = DateTimeOffset.UtcNow
            }
            Ok version
        else
            Error (RegistryError.ModelNotFound path)
```

---

## � Распределение по языкам

### C# (50%)
- API Controllers
- EF Core (Model registry, jobs)
- ONNX Runtime inference
- gRPC clients
- ML.NET (classical ML: clustering, regression)

### Rust (40%)
- Training service (tch-rs, burn)
- GPU-accelerated inference
- Model quantization
- Tensor operations

### F# (10%)
- Model versioning logic
- A/B testing algorithms
- Data pipeline

---

## 🛠️ Технологии (NO Python!)

| Задача | Python (было) | Новый стек |
|--------|---------------|------------|
| Training | PyTorch | Rust tch-rs |
| Inference | PyTorch | C# ONNX Runtime |
| Classical ML | scikit-learn | C# ML.NET |
| Quantization | PyTorch | Rust tract |
| Dataset mgmt | pandas | Rust polars |

---

## 📊 Оценка времени

| Компонент | C# | F# | Rust | Недели |
|-----------|----|----|------|--------|
| Training Service | 30% | 10% | 60% | 4-6 |
| Inference API | 70% | 10% | 20% | 2-3 |
| Model Registry | 50% | 40% | 10% | 2 |
| Quantization | 20% | - | 80% | 3-4 |
| **Итого** | - | - | - | **11-15 недель** |

---

## ⚠️ Риски (без Python)

1. **tch-rs ограничен** - не все PyTorch features
2. **Модели нужно конвертировать** - PyTorch → ONNX → Rust
3. **Сложнее debugging** - C++/Rust vs Python
4. **Меньше документации** - Rust ML менее популярен

---

**Статус:** � СЛОЖНО, но возможно (11-15 недель)

**Рекомендация:** Начать с inference (ONNX), затем training (tch-rs)
