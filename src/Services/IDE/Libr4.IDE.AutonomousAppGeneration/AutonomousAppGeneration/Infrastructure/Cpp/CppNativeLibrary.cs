namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;

internal static class CppNativeLibrary
{
    public const string TreeSitterLibraryName = "libr4_tree_sitter";
    public const string OrtEpLibraryName = "libr4_ort_ep";
    public const string LibClangLibraryName = "libr4_libclang";

    public static bool TryLoad<T>(Func<T> factory, out T? instance, out Exception? error)
    {
        try
        {
            instance = factory();
            error = null;
            return true;
        }
        catch (DllNotFoundException ex)
        {
            instance = default;
            error = ex;
            return false;
        }
        catch (BadImageFormatException ex)
        {
            instance = default;
            error = ex;
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            instance = default;
            error = ex;
            return false;
        }
    }
}
