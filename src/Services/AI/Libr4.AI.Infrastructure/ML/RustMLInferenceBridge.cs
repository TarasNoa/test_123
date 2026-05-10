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

    public Task<string> RunInferenceAsync(string requestJson)
    {
        return Task.Run(() =>
        {
            var resultPtr = libr4_ml_run_inference(requestJson);
            if (resultPtr == IntPtr.Zero)
            {
                return string.Empty;
            }

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

    public void Dispose()
    {
        // nothing to dispose, but keep pattern for future native handles
    }
}
