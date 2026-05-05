/*
using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using MLInferenceProto;

namespace Libr4.AI.Infrastructure.ML;

/// <summary>
/// C# Bridge for Rust ML Inference Engine
/// Golden Stack: C# orchestrates, Rust performs ML inference
/// </summary>
public interface IRustMLInferenceBridge
{
    Task LoadModelAsync(string modelName, string modelPath);
    Task<float[]> InferAsync(string modelName, float[] inputs, long[] inputShape);
    Task<float[]> EmbedTextAsync(string text, string modelName);
    Task<string[]> ListModelsAsync();
}

/// <summary>
/// gRPC Bridge implementation for Rust ML Inference
/// </summary>
public class RustMLInferenceBridge : IRustMLInferenceBridge, IDisposable
{
    private readonly MLInferenceProto.MlInference.MlInferenceClient _client;
    private readonly GrpcChannel _channel;
    private readonly ILogger<RustMLInferenceBridge> _logger;

    public RustMLInferenceBridge(
        string rustServiceUrl,
        ILogger<RustMLInferenceBridge> logger)
    {
        _logger = logger;
        
        _channel = GrpcChannel.ForAddress(rustServiceUrl, new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 64 * 1024 * 1024, // 64MB for large models
            MaxSendMessageSize = 64 * 1024 * 1024,
        });
        
        _client = new MLInferenceProto.MlInference.MlInferenceClient(_channel);
        
        _logger.LogInformation("Connected to Rust ML Inference Engine at {Url}", rustServiceUrl);
    }

    public async Task LoadModelAsync(string modelName, string modelPath)
    {
        try
        {
            var request = new LoadModelRequest
            {
                ModelName = modelName,
                ModelPath = modelPath
            };

            var response = await _client.LoadModelAsync(request);
            
            if (!response.Success)
            {
                throw new MLInferenceException($"Failed to load model {modelName}: {response.Message}");
            }

            _logger.LogInformation("Loaded ML model: {ModelName} from {ModelPath}", modelName, modelPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ML model {ModelName}", modelName);
            throw new MLInferenceException($"Model loading failed: {ex.Message}", ex);
        }
    }

    public async Task<float[]> InferAsync(string modelName, float[] inputs, long[] inputShape)
    {
        try
        {
            var request = new InferenceRequest
            {
                ModelName = modelName
            };
            request.Inputs.AddRange(inputs);
            request.InputShape.AddRange(inputShape);

            var response = await _client.InferAsync(request);
            
            _logger.LogDebug(
                "ML inference completed for {ModelName} in {TimeMs}ms. Output shape: [{Shape}]",
                modelName,
                response.InferenceTimeMs,
                string.Join(",", response.OutputShape));

            return response.Outputs.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ML inference failed for model {ModelName}", modelName);
            throw new MLInferenceException($"Inference failed: {ex.Message}", ex);
        }
    }

    public async Task<float[]> EmbedTextAsync(string text, string modelName)
    {
        try
        {
            var request = new TextEmbeddingRequest
            {
                Text = text,
                ModelName = modelName
            };

            var response = await _client.EmbedTextAsync(request);
            
            _logger.LogDebug(
                "Text embedding generated using {ModelName}. Dimensions: {Dimensions}, Time: {TimeMs}ms",
                modelName,
                response.Dimensions,
                response.InferenceTimeMs);

            return response.Embedding.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text embedding failed for model {ModelName}", modelName);
            throw new MLInferenceException($"Embedding failed: {ex.Message}", ex);
        }
    }

    public async Task<string[]> ListModelsAsync()
    {
        try
        {
            var request = new ListModelsRequest();
            var response = await _client.ListModelsAsync(request);
            
            return response.ModelNames.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list ML models");
            return Array.Empty<string>();
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _logger.LogInformation("Disconnected from Rust ML Inference Engine");
    }
}

/// <summary>
/// ML Inference exception
/// </summary>
public class MLInferenceException : Exception
{
    public MLInferenceException(string message, Exception? innerException = null) 
        : base(message, innerException) { }
}

/// <summary>
/// DI Extensions
/// </summary>
public static class RustMLInferenceExtensions
{
    public static IServiceCollection AddRustMLInference(this IServiceCollection services, string rustServiceUrl)
    {
        services.AddSingleton<IRustMLInferenceBridge>(sp =>
            new RustMLInferenceBridge(
                rustServiceUrl,
                sp.GetRequiredService<ILogger<RustMLInferenceBridge>>()));

        return services;
    }
}
*/
