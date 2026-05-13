namespace Libr4.AI.Domain.LocalAI.FSharp

open System

type ModelFormat = GGUF | GGML | ONNX | SafeTensors | PyTorch
type InferenceBackend = Ollama | LlamaCpp | VLLM | TGI | OpenVINO

type LocalModel = {
    id: Guid
    name: string
    format: ModelFormat
    backend: InferenceBackend
    modelPath: string
    endpoint: string
    contextSize: int
    parametersCount: int64
    quantization: string option
    isRunning: bool
    gpuMemoryMb: int
    ramMb: int
    loadedAt: DateTimeOffset option
    createdAt: DateTimeOffset
}

type InferenceRequest = {
    id: Guid
    modelId: Guid
    prompt: string
    maxTokens: int
    temperature: float
    topP: float
    stopSequences: string list
    stream: bool
    requestedAt: DateTimeOffset
}

type InferenceResponse = {
    id: Guid
    requestId: Guid
    completion: string
    tokensGenerated: int
    latencyMs: int
    tokensPerSecond: float
    createdAt: DateTimeOffset
}
