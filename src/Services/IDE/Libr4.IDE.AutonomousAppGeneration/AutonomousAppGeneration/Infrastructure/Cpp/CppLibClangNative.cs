using System.Runtime.InteropServices;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;

internal static class CppLibClangNative
{
    [DllImport(CppNativeLibrary.LibClangLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int libr4_cl_probe();

    [DllImport(CppNativeLibrary.LibClangLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int libr4_cl_parse_repo_json(
        [MarshalAs(UnmanagedType.LPStr)] string pathHint,
        [MarshalAs(UnmanagedType.LPStr)] string sourceUtf8,
        out IntPtr outJson);

    [DllImport(CppNativeLibrary.LibClangLibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void libr4_cl_free_string(IntPtr ptr);
}
