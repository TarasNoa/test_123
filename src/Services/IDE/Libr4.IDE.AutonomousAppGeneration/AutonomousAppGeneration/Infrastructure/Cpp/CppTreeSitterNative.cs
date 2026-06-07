using System.Runtime.InteropServices;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;

internal static class CppTreeSitterNative
{
    [DllImport(CppNativeLibrary.TreeSitterLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int libr4_ts_probe();

    [DllImport(CppNativeLibrary.TreeSitterLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int libr4_ts_analyze_json(
        [MarshalAs(UnmanagedType.LPStr)] string pathHint,
        [MarshalAs(UnmanagedType.LPStr)] string sourceUtf8,
        [MarshalAs(UnmanagedType.LPStr)] string languageOverride,
        out IntPtr outJson);

    [DllImport(CppNativeLibrary.TreeSitterLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void libr4_ts_free_string(IntPtr ptr);
}
