using System.Runtime.InteropServices;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;

internal static class CppOrtEpNative
{
    [DllImport(CppNativeLibrary.OrtEpLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int libr4_ort_probe();

    [DllImport(CppNativeLibrary.OrtEpLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int libr4_ort_list_providers_json(out IntPtr outJson);

    [DllImport(CppNativeLibrary.OrtEpLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int libr4_ort_session_create(
        [MarshalAs(UnmanagedType.LPStr)] string modelPath,
        [MarshalAs(UnmanagedType.LPStr)] string epPreference,
        out IntPtr outSession);

    [DllImport(CppNativeLibrary.OrtEpLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void libr4_ort_session_destroy(IntPtr session);

    [DllImport(CppNativeLibrary.OrtEpLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int libr4_ort_bert_embed(
        IntPtr session,
        long[] inputIds,
        long[] attentionMask,
        long[] tokenTypeIds,
        int batch,
        int seqLen,
        out IntPtr outEmbeddings,
        out int outHiddenDim);

    [DllImport(CppNativeLibrary.OrtEpLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void libr4_ort_free_string(IntPtr ptr);

    [DllImport(CppNativeLibrary.OrtEpLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void libr4_ort_free_floats(IntPtr ptr);
}
