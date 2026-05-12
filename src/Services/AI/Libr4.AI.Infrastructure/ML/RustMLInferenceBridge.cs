using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
    private const string NativeLibraryName = "libr4_ml_inference";

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr libr4_ml_run_inference(string inputJson);

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void libr4_ml_free_string(IntPtr ptr);

    private Task<string> RunInferenceAsync(string requestJson)
    {
        return Task.Run(() =>
        {
            var resultPtr = libr4_ml_run_inference(requestJson);
            if (resultPtr == IntPtr.Zero)
                return string.Empty;
            try
            {
                return Marshal.PtrToStringAnsi(resultPtr) ?? string.Empty;
            }
            finally
            {
                libr4_ml_free_string(resultPtr);
            }
        });
    }

    public async Task LoadModelAsync(string modelName, string modelPath)
    {
        var request = System.Text.Json.JsonSerializer.Serialize(new { action = "load_model", model_name = modelName, model_path = modelPath });
        await RunInferenceAsync(request);
    }

    public async Task<float[]> InferAsync(string modelName, float[] inputs, long[] inputShape)
    {
        var request = System.Text.Json.JsonSerializer.Serialize(new { action = "infer", model_name = modelName, inputs, input_shape = inputShape });
        var json = await RunInferenceAsync(request);
        if (string.IsNullOrEmpty(json)) return Array.Empty<float>();
        return System.Text.Json.JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();
    }

    public async Task<float[]> EmbedTextAsync(string text, string modelName)
    {
        var request = System.Text.Json.JsonSerializer.Serialize(new { action = "embed", text, model_name = modelName });
        var json = await RunInferenceAsync(request);
        if (string.IsNullOrEmpty(json)) return Array.Empty<float>();
        return System.Text.Json.JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();
    }

    public async Task<string[]> ListModelsAsync()
    {
        var request = System.Text.Json.JsonSerializer.Serialize(new { action = "list_models" });
        var json = await RunInferenceAsync(request);
        if (string.IsNullOrEmpty(json)) return Array.Empty<string>();
        return System.Text.Json.JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
    }

    public void Dispose()
    {
        // nothing to dispose, but keep pattern for future native handles
    }
}
