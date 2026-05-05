using System.Runtime.InteropServices;

namespace Libr4.Media.Domain.Audio;

/// <summary>
/// C# wrapper for Rust audio processing library
/// Uses P/Invoke to call native Rust functions
/// </summary>
public static class AudioProcessing
{
    private const string DllName = "libr4_audio_processing";

    #region Compression

    /// <summary>
    /// Compress audio data using RLE (Run-Length Encoding)
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void compress_audio(byte[] input_data, int input_len, byte[] output_data, out int output_len);

    /// <summary>
    /// Decompress audio data using RLE
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void decompress_audio(byte[] input_data, int input_len, byte[] output_data, out int output_len);

    /// <summary>
    /// Calculate compression ratio
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern double compression_ratio(ulong original_size, ulong compressed_size);

    public static byte[] CompressAudio(byte[] audioData)
    {
        if (audioData == null || audioData.Length == 0)
            return Array.Empty<byte>();

        // Maximum compressed size is 2x original for RLE
        var output = new byte[audioData.Length * 2];
        compress_audio(audioData, audioData.Length, output, out int outputLen);
        
        Array.Resize(ref output, outputLen);
        return output;
    }

    public static byte[] DecompressAudio(byte[] compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
            return Array.Empty<byte>();

        // Maximum decompressed size is 255x compressed for RLE
        var output = new byte[compressedData.Length * 255];
        decompress_audio(compressedData, compressedData.Length, output, out int outputLen);
        
        Array.Resize(ref output, outputLen);
        return output;
    }

    public static double CalculateCompressionRatio(ulong originalSize, ulong compressedSize)
    {
        return compression_ratio(originalSize, compressedSize);
    }

    #endregion

    #region Codec

    /// <summary>
    /// Encode audio samples to PCM format
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void encode_pcm(short[] samples, int sample_count, byte[] output, out int output_len);

    /// <summary>
    /// Decode audio samples from PCM format
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void decode_pcm(byte[] encoded, int encoded_len, short[] samples, out int sample_count);

    public static byte[] EncodePcm(short[] samples)
    {
        if (samples == null || samples.Length == 0)
            return Array.Empty<byte>();

        var output = new byte[samples.Length * 2];
        encode_pcm(samples, samples.Length, output, out int outputLen);
        
        Array.Resize(ref output, outputLen);
        return output;
    }

    public static short[] DecodePcm(byte[] encoded)
    {
        if (encoded == null || encoded.Length == 0 || encoded.Length % 2 != 0)
            return Array.Empty<short>();

        var samples = new short[encoded.Length / 2];
        decode_pcm(encoded, encoded.Length, samples, out int sampleCount);
        
        Array.Resize(ref samples, sampleCount);
        return samples;
    }

    #endregion

    #region Analysis

    /// <summary>
    /// Calculate RMS (Root Mean Square) amplitude
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern double calculate_rms(short[] samples, int sample_count);

    /// <summary>
    /// Calculate audio duration in seconds
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern double calculate_duration(ulong sample_count, uint sample_rate);

    public static double CalculateRms(short[] samples)
    {
        if (samples == null || samples.Length == 0)
            return 0.0;

        return calculate_rms(samples, samples.Length);
    }

    public static double CalculateDuration(ulong sampleCount, uint sampleRate)
    {
        return calculate_duration(sampleCount, sampleRate);
    }

    #endregion

    #region Conversion

    /// <summary>
    /// Convert sample rate using linear interpolation
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void resample(short[] input_samples, int input_len, uint from_rate, uint to_rate, short[] output_samples, out int output_len);

    /// <summary>
    /// Convert stereo to mono by averaging channels
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void stereo_to_mono(short[] stereo_samples, int stereo_len, short[] mono_samples, out int mono_len);

    public static short[] Resample(short[] samples, uint fromRate, uint toRate)
    {
        if (samples == null || samples.Length == 0)
            return Array.Empty<short>();

        // Estimate output size
        var ratio = (double)fromRate / toRate;
        var outputLen = (int)Math.Ceiling(samples.Length / ratio);
        var output = new short[outputLen];
        
        resample(samples, samples.Length, fromRate, toRate, output, out int actualLen);
        
        Array.Resize(ref output, actualLen);
        return output;
    }

    public static short[] StereoToMono(short[] stereoSamples)
    {
        if (stereoSamples == null || stereoSamples.Length == 0 || stereoSamples.Length % 2 != 0)
            return Array.Empty<short>();

        var monoSamples = new short[stereoSamples.Length / 2];
        stereo_to_mono(stereoSamples, stereoSamples.Length, monoSamples, out int monoLen);
        
        Array.Resize(ref monoSamples, monoLen);
        return monoSamples;
    }

    #endregion
}
