namespace Libr4.AI.Domain.MLModels.FSharp

open System

module MLModelOps =
    let deployModel (now: DateTimeOffset) (model: MLModel) =
        { model with status = ModelStatus.Deployed; deployedAt = Some now }

    let deprecate (model: MLModel) =
        { model with status = ModelStatus.Deprecated }

    let isProductionReady (model: MLModel) : bool =
        model.status = ModelStatus.Trained && model.accuracy >= 0.85 && model.f1Score >= 0.8

module ExperimentOps =
    let complete (metrics: Map<string, float>) (now: DateTimeOffset) (exp: ModelExperiment) =
        { exp with metrics = metrics; status = "completed"; completedAt = Some now }

module ABTestOps =
    let determineWinner (test: ABTest) : Guid option =
        if test.modelAAccuracy > test.modelBAccuracy then Some test.modelA
        elif test.modelBAccuracy > test.modelAAccuracy then Some test.modelB
        else None

    let conclude (now: DateTimeOffset) (test: ABTest) =
        { test with winnerModelId = determineWinner test; endedAt = Some now }
