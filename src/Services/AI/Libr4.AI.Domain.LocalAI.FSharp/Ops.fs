namespace Libr4.AI.Domain.LocalAI.FSharp

open System

module LocalAIOps =
    let startModel (now: DateTimeOffset) (model: LocalModel) =
        { model with isRunning = true; loadedAt = Some now }

    let stopModel (model: LocalModel) =
        { model with isRunning = false; loadedAt = None }

    let calculateTokensPerSecond (tokens: int) (latencyMs: int) : float =
        if latencyMs > 0 then float tokens * 1000.0 / float latencyMs else 0.0

    let estimateMemoryNeeded (parametersCount: int64) (quantization: string option) : int =
        let bytesPerParam =
            match quantization with
            | Some "int4" -> 0.5
            | Some "int8" -> 1.0
            | Some "fp16" -> 2.0
            | _ -> 4.0
        int (float parametersCount * bytesPerParam / 1024.0 / 1024.0)

module InferenceOps =
    let createRequest (modelId: Guid) (prompt: string) (now: DateTimeOffset) : InferenceRequest =
        {
            id = Guid.NewGuid()
            modelId = modelId
            prompt = prompt
            maxTokens = 2048
            temperature = 0.7
            topP = 0.9
            stopSequences = []
            stream = false
            requestedAt = now
        }

    let createResponse (requestId: Guid) (completion: string) (tokens: int) (latency: int) (now: DateTimeOffset) : InferenceResponse =
        {
            id = Guid.NewGuid()
            requestId = requestId
            completion = completion
            tokensGenerated = tokens
            latencyMs = latency
            tokensPerSecond = LocalAIOps.calculateTokensPerSecond tokens latency
            createdAt = now
        }
