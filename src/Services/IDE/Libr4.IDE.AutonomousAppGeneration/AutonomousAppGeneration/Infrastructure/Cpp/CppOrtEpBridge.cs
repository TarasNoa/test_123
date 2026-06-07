using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;

/// <summary>Wave 6.2: ONNX Runtime Direct EP via C++ muscle layer.</summary>
internal static class CppOrtEpBridge
{
    private static bool? _available;

    public static bool IsAvailable
    {
        get
        {
            if (_available.HasValue)
                return _available.Value;

            _available = CppNativeLibrary.TryLoad(
                () => CppOrtEpNative.libr4_ort_probe() == 0,
                out var ok,
                out _);
            _available = ok;
            return _available.Value;
        }
    }

    public static bool TryListProviders(ILogger logger, out IReadOnlyList<string> providers)
    {
        providers = Array.Empty<string>();
        if (!IsAvailable)
            return false;

        try
        {
            if (CppOrtEpNative.libr4_ort_list_providers_json(out var jsonPtr) != 0 || jsonPtr == IntPtr.Zero)
                return false;

            var json = Marshal.PtrToStringUTF8(jsonPtr) ?? "[]";
            CppOrtEpNative.libr4_ort_free_string(jsonPtr);
            providers = JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
            return providers.Count > 0;
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            _available = false;
            logger.LogDebug(ex, "[CppOrtEp] native library unavailable");
            return false;
        }
    }

    public static bool TryCreateSession(
        string modelPath,
        string executionProvider,
        ILogger logger,
        out IntPtr session)
    {
        session = IntPtr.Zero;
        if (!IsAvailable || string.IsNullOrWhiteSpace(modelPath))
            return false;

        try
        {
            var rc = CppOrtEpNative.libr4_ort_session_create(
                modelPath,
                executionProvider ?? string.Empty,
                out session);
            return rc == 0 && session != IntPtr.Zero;
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            _available = false;
            logger.LogDebug(ex, "[CppOrtEp] session create failed — native unavailable");
            return false;
        }
    }

    public static void DestroySession(IntPtr session)
    {
        if (session != IntPtr.Zero && IsAvailable)
            CppOrtEpNative.libr4_ort_session_destroy(session);
    }

    public static bool TryBertEmbed(
        IntPtr session,
        long[] inputIds,
        long[] attentionMask,
        long[] tokenTypeIds,
        int batch,
        int seqLen,
        ILogger logger,
        out float[] embeddings,
        out int hiddenDim)
    {
        embeddings = Array.Empty<float>();
        hiddenDim = 0;

        if (session == IntPtr.Zero || !IsAvailable)
            return false;

        try
        {
            var rc = CppOrtEpNative.libr4_ort_bert_embed(
                session,
                inputIds,
                attentionMask,
                tokenTypeIds,
                batch,
                seqLen,
                out var ptr,
                out hiddenDim);

            if (rc != 0 || ptr == IntPtr.Zero || hiddenDim <= 0)
                return false;

            var length = batch * hiddenDim;
            embeddings = new float[length];
            Marshal.Copy(ptr, embeddings, 0, length);
            CppOrtEpNative.libr4_ort_free_floats(ptr);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[CppOrtEp] bert embed failed");
            return false;
        }
    }
}
