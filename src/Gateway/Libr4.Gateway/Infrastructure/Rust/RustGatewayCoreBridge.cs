using System.Runtime.InteropServices;
using System.Text.Json;

namespace Libr4.Gateway.Infrastructure.Rust;

/// <summary>Wave 3.7/3.8: Rust gateway core (circuit breaker + rate limiter + risk scoring).</summary>
public static class RustGatewayCoreBridge
{
    private static bool? _available;

    private static readonly JsonSerializerOptions SnakeCaseJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    [DllImport("libr4_gateway_core", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool gateway_circuit_is_open([MarshalAs(UnmanagedType.LPStr)] string key);

    [DllImport("libr4_gateway_core", CallingConvention = CallingConvention.Cdecl)]
    private static extern void gateway_circuit_record_success([MarshalAs(UnmanagedType.LPStr)] string key);

    [DllImport("libr4_gateway_core", CallingConvention = CallingConvention.Cdecl)]
    private static extern void gateway_circuit_record_failure([MarshalAs(UnmanagedType.LPStr)] string key);

    [DllImport("libr4_gateway_core", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool gateway_rate_limit_allow(
        [MarshalAs(UnmanagedType.LPStr)] string key,
        double capacity,
        double refillPerSec,
        double cost);

    [DllImport("libr4_gateway_core", CallingConvention = CallingConvention.Cdecl)]
    private static extern int gateway_evaluate_risk_json(
        [MarshalAs(UnmanagedType.LPStr)] string featuresJson,
        out IntPtr outJson);

    [DllImport("libr4_gateway_core", CallingConvention = CallingConvention.Cdecl)]
    private static extern void gateway_core_free_string(IntPtr s);

    public static bool IsAvailable
    {
        get
        {
            if (_available.HasValue)
                return _available.Value;

            try
            {
                _ = gateway_circuit_is_open("__probe__");
                _available = true;
            }
            catch (DllNotFoundException)
            {
                _available = false;
            }
            catch (BadImageFormatException)
            {
                _available = false;
            }
            catch (EntryPointNotFoundException)
            {
                _available = false;
            }

            return _available.Value;
        }
    }

    public static bool IsCircuitOpen(string key) =>
        IsAvailable && gateway_circuit_is_open(key);

    public static void RecordCircuitSuccess(string key)
    {
        if (IsAvailable)
            gateway_circuit_record_success(key);
    }

    public static void RecordCircuitFailure(string key)
    {
        if (IsAvailable)
            gateway_circuit_record_failure(key);
    }

    public static bool AllowRequest(string key, double capacity, double refillPerSec, double cost = 1.0) =>
        !IsAvailable || gateway_rate_limit_allow(key, capacity, refillPerSec, cost);

    public static RustRiskDecision? EvaluateRisk(RustRiskFeatures features)
    {
        if (!IsAvailable)
            return null;

        var json = JsonSerializer.Serialize(features, SnakeCaseJson);
        var rc = gateway_evaluate_risk_json(json, out var jsonPtr);
        if (jsonPtr == IntPtr.Zero)
            return null;

        var resultJson = Marshal.PtrToStringUTF8(jsonPtr) ?? "{}";
        gateway_core_free_string(jsonPtr);
        if (rc != 0)
            return null;

        return JsonSerializer.Deserialize<RustRiskDecision>(resultJson, SnakeCaseJson);
    }
}

public sealed record RustRiskFeatures(
    float RequestCount,
    float ErrorRate,
    float UniquePaths,
    float TimeWindow,
    float Burstiness,
    float RecentViolations);

public sealed record RustRiskDecision(
    float RiskScore,
    string Action,
    float LimitPerSecond,
    ulong BanSeconds);
