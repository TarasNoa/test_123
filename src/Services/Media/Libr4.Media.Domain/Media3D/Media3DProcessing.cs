using System.Runtime.InteropServices;

namespace Libr4.Media.Domain.Media3D;

/// <summary>
/// C# wrapper for Rust 3D media processing library
/// Uses P/Invoke to call native Rust functions
/// </summary>
public static class Media3DProcessing
{
    private const string DllName = "libr4_3d_media";

    #region Geometry

    /// <summary>
    /// 3D Vector structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Vec3
    {
        public float X;
        public float Y;
        public float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    #endregion

    #region Mesh

    /// <summary>
    /// Vertex structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vec3 Position;
        public Vec3 Normal;
        public float TexCoordU;
        public float TexCoordV;
    }

    /// <summary>
    /// Triangle structure (3 vertices)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Triangle
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public Vertex[] Vertices;
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Transform structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Transform
    {
        public Vec3 Position;
        public Vec3 Rotation;
        public Vec3 Scale;
    }

    /// <summary>
    /// 4x4 Matrix structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix4x4
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] Row0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] Row1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] Row2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] Row3;
    }

    /// <summary>
    /// Calculate world matrix from transform
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void calculate_world_matrix(ref Transform transform, out Matrix4x4 matrix);

    public static Matrix4x4 CalculateWorldMatrix(Transform transform)
    {
        calculate_world_matrix(ref transform, out Matrix4x4 matrix);
        return matrix;
    }

    #endregion

    #region Compression

    /// <summary>
    /// Compress mesh data
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void compress_mesh(byte[] input_data, int input_len, byte[] output_data, out int output_len);

    /// <summary>
    /// Decompress mesh data
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void decompress_mesh(byte[] input_data, int input_len, byte[] output_data, out int output_len);

    public static byte[] CompressMesh(byte[] meshData)
    {
        if (meshData == null || meshData.Length == 0)
            return Array.Empty<byte>();

        // Estimate compressed size (in production, would be more sophisticated)
        var output = new byte[meshData.Length];
        compress_mesh(meshData, meshData.Length, output, out int outputLen);
        
        Array.Resize(ref output, outputLen);
        return output;
    }

    public static byte[] DecompressMesh(byte[] compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
            return Array.Empty<byte>();

        // Estimate decompressed size (in production, would be more sophisticated)
        var output = new byte[compressedData.Length * 2];
        decompress_mesh(compressedData, compressedData.Length, output, out int outputLen);
        
        Array.Resize(ref output, outputLen);
        return output;
    }

    #endregion

    #region Export

    /// <summary>
    /// Export mesh to OBJ format
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void export_to_obj(byte[] mesh_data, int mesh_len, byte[] output, out int output_len);

    /// <summary>
    /// Estimate export file size
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong estimate_export_size(byte[] mesh_data, int mesh_len);

    public static string ExportToObj(byte[] meshData)
    {
        if (meshData == null || meshData.Length == 0)
            return string.Empty;

        var output = new byte[meshData.Length * 100]; // Generous buffer
        export_to_obj(meshData, meshData.Length, output, out int outputLen);
        
        Array.Resize(ref output, outputLen);
        return System.Text.Encoding.UTF8.GetString(output);
    }

    public static ulong EstimateExportSize(byte[] meshData)
    {
        if (meshData == null || meshData.Length == 0)
            return 0;

        return estimate_export_size(meshData, meshData.Length);
    }

    #endregion
}
