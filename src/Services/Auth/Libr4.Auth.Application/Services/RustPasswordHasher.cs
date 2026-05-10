using System.Runtime.InteropServices;

namespace Libr4.Auth.Application.Services;

public class RustPasswordHasher : IPasswordHasher
{
    [DllImport("auth_crypto.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr generate_salt();

    [DllImport("auth_crypto.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr hash_password(IntPtr password, IntPtr salt);

    [DllImport("auth_crypto.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool verify_password(IntPtr password, IntPtr salt, IntPtr hash);

    [DllImport("auth_crypto.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void free_string(IntPtr s);

    public string Hash(string password)
    {
        var saltPtr = generate_salt();
        var salt = Marshal.PtrToStringAnsi(saltPtr)!;
        free_string(saltPtr);

        var passwordPtr = Marshal.StringToHGlobalAnsi(password);
        var saltPtr2 = Marshal.StringToHGlobalAnsi(salt);
        var hashPtr = hash_password(passwordPtr, saltPtr2);
        var hash = Marshal.PtrToStringAnsi(hashPtr)!;
        free_string(hashPtr);
        Marshal.FreeHGlobal(passwordPtr);
        Marshal.FreeHGlobal(saltPtr2);

        return $"{salt}:{hash}";
    }

    public bool Verify(string password, string hashedPassword)
    {
        var parts = hashedPassword.Split(':');
        if (parts.Length != 2) return false;

        var salt = parts[0];
        var hash = parts[1];

        var passwordPtr = Marshal.StringToHGlobalAnsi(password);
        var saltPtr = Marshal.StringToHGlobalAnsi(salt);
        var hashPtr = Marshal.StringToHGlobalAnsi(hash);
        var result = verify_password(passwordPtr, saltPtr, hashPtr);
        Marshal.FreeHGlobal(passwordPtr);
        Marshal.FreeHGlobal(saltPtr);
        Marshal.FreeHGlobal(hashPtr);

        return result;
    }
}