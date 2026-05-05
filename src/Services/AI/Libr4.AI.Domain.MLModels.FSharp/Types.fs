namespace Libr4.AI.Domain.MLModels.FSharp

open System

type ModelStatus = Training | Trained | Deployed | Deprecated | Failed
type ModelFramework = PyTorch | TensorFlow | ONNX | JAX | XGBoost | SKLearn

type MLModel = {
    id: Guid
    name: string
    version: string
    framework: ModelFramework
    accuracy: float
    precision: float
    recall: float
    f1Score: float
    status: ModelStatus
    trainingDataSize: int
    parameters: int64
    modelSize: int64
    createdBy: Guid
    createdAt: DateTimeOffset
    deployedAt: DateTimeOffset option
}

type ModelExperiment = {
    id: Guid
    modelId: Guid
    name: string
    hyperparameters: Map<string, obj>
    metrics: Map<string, float>
    status: string
    startedAt: DateTimeOffset
    completedAt: DateTimeOffset option
}

type ABTest = {
    id: Guid
    modelA: Guid
    modelB: Guid
    trafficSplit: float
    modelAConversions: int
    modelBConversions: int
    modelAAccuracy: float
    modelBAccuracy: float
    winnerModelId: Guid option
    startedAt: DateTimeOffset
    endedAt: DateTimeOffset option
}
