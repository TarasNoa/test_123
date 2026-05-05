using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#if RUST_SECURITY_GRPC
using Grpc.Net.Client;
using SecurityProto;
#endif

namespace Libr4.Shared.Infrastructure.Security;

/// <summary>
/// C# Bridge for Rust Security Core (gRPC)
/// Golden Stack: C# calls Rust for crypto operations
/// </summary>
public interface IRustSecurityBridge
{
    Task<byte[]> EncryptAsync(byte[] plaintext, byte[] key);
    Task<byte[]> DecryptAsync(byte[] ciphertext, byte[] key);
    Task<string> HashPasswordAsync(string password, byte[] salt);
    Task<bool> VerifyPasswordAsync(string password, byte[] salt, string hash);
    Task<SecureToken> GenerateTokenAsync(string userId, string[] permissions, long expirySeconds);
    Task<TokenValidationResult> VerifyTokenAsync(byte[] tokenData);
}

/// <summary>
/// Secure token representation
/// </summary>
public record SecureToken(
    string TokenId,
    byte[] TokenData,
    long ExpiresAt);

/// <summary>
/// Token validation result
/// </summary>
public record TokenValidationResult(
    bool Valid,
    string UserId,
    string[] Permissions);

#if RUST_SECURITY_GRPC
/// <summary>
/// gRPC Bridge implementation — active when RUST_SECURITY_GRPC is defined
/// and SecurityProto is generated from security.proto
/// </summary>
public class RustSecurityBridge : IRustSecurityBridge, IDisposable
{
    private readonly SecurityProto.Security.SecurityClient _client;
    private readonly GrpcChannel _channel;
    private readonly ILogger<RustSecurityBridge> _logger;

    public RustSecurityBridge(
        string rustServiceUrl,
        ILogger<RustSecurityBridge> logger)
    {
        _logger = logger;
        
        _channel = GrpcChannel.ForAddress(rustServiceUrl, new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 16 * 1024 * 1024, // 16MB
            MaxSendMessageSize = 16 * 1024 * 1024,
        });
        
        _client = new SecurityProto.Security.SecurityClient(_channel);
        
        _logger.LogInformation("Connected to Rust Security Core at {Url}", rustServiceUrl);
    }

    public async Task<byte[]> EncryptAsync(byte[] plaintext, byte[] key)
    {
        try
        {
            var request = new EncryptRequest
            {
                Plaintext = Google.Protobuf.ByteString.CopyFrom(plaintext),
                Key = Google.Protobuf.ByteString.CopyFrom(key)
            };

            var response = await _client.EncryptAsync(request);
            
            var result = new byte[response.Nonce.Length + response.Tag.Length + response.Ciphertext.Length];
            response.Nonce.CopyTo(result, 0);
            response.Tag.CopyTo(result, response.Nonce.Length);
            response.Ciphertext.CopyTo(result, response.Nonce.Length + response.Tag.Length);
            
            _logger.LogDebug("Encrypted {Length} bytes using Rust Security Core", plaintext.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed via Rust Security Core");
            throw new SecurityException("Encryption failed", ex);
        }
    }

    public async Task<byte[]> DecryptAsync(byte[] ciphertext, byte[] key)
    {
        try
        {
            int nonceLength = 12;
            int tagLength = 16;
            
            var nonce = ciphertext[0..nonceLength];
            var tag = ciphertext[nonceLength..(nonceLength + tagLength)];
            var encryptedData = ciphertext[(nonceLength + tagLength)..];

            var request = new DecryptRequest
            {
                Nonce = Google.Protobuf.ByteString.CopyFrom(nonce),
                Tag = Google.Protobuf.ByteString.CopyFrom(tag),
                Ciphertext = Google.Protobuf.ByteString.CopyFrom(encryptedData),
                Key = Google.Protobuf.ByteString.CopyFrom(key)
            };

            var response = await _client.DecryptAsync(request);
            
            _logger.LogDebug("Decrypted {Length} bytes using Rust Security Core", response.Plaintext.Length);
            
            return response.Plaintext.ToByteArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed via Rust Security Core");
            throw new SecurityException("Decryption failed", ex);
        }
    }

    public async Task<string> HashPasswordAsync(string password, byte[] salt)
    {
        try
        {
            var request = new HashPasswordRequest
            {
                Password = password,
                Salt = Google.Protobuf.ByteString.CopyFrom(salt)
            };

            var response = await _client.HashPasswordAsync(request);
            
            _logger.LogDebug("Password hashed using Rust Security Core");
            
            return response.Hash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password hashing failed via Rust Security Core");
            throw new SecurityException("Password hashing failed", ex);
        }
    }

    public async Task<bool> VerifyPasswordAsync(string password, byte[] salt, string hash)
    {
        try
        {
            var request = new VerifyPasswordRequest
            {
                Password = password,
                Salt = Google.Protobuf.ByteString.CopyFrom(salt),
                Hash = hash
            };

            var response = await _client.VerifyPasswordAsync(request);
            
            _logger.LogDebug("Password verified using Rust Security Core: {Valid}", response.Valid);
            
            return response.Valid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password verification failed via Rust Security Core");
            return false;
        }
    }

    public async Task<SecureToken> GenerateTokenAsync(string userId, string[] permissions, long expirySeconds)
    {
        try
        {
            var request = new GenerateTokenRequest
            {
                UserId = userId,
                ExpirySeconds = expirySeconds
            };
            request.Permissions.AddRange(permissions);

            var response = await _client.GenerateTokenAsync(request);
            
            _logger.LogDebug("Token generated for user {UserId} using Rust Security Core", userId);
            
            return new SecureToken(
                response.TokenId,
                response.TokenData.ToByteArray(),
                response.ExpiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token generation failed via Rust Security Core");
            throw new SecurityException("Token generation failed", ex);
        }
    }

    public async Task<TokenValidationResult> VerifyTokenAsync(byte[] tokenData)
    {
        try
        {
            var request = new VerifyTokenRequest
            {
                TokenData = Google.Protobuf.ByteString.CopyFrom(tokenData)
            };

            var response = await _client.VerifyTokenAsync(request);
            
            return new TokenValidationResult(
                response.Valid,
                response.UserId,
                response.Permissions.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token verification failed via Rust Security Core");
            return new TokenValidationResult(false, string.Empty, Array.Empty<string>());
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _logger.LogInformation("Disconnected from Rust Security Core");
    }
}
#endif

/// <summary>
/// Security exception
/// </summary>
public class SecurityException : Exception
{
    public SecurityException(string message, Exception? innerException = null) 
        : base(message, innerException) { }
}

/// <summary>
/// DI Extension
/// </summary>
public static class RustSecurityExtensions
{
    public static IServiceCollection AddRustSecurity(this IServiceCollection services, string rustServiceUrl)
    {
#if RUST_SECURITY_GRPC
        services.AddSingleton<IRustSecurityBridge>(sp => 
            new RustSecurityBridge(
                rustServiceUrl,
                sp.GetRequiredService<ILogger<RustSecurityBridge>>()));
#else
        services.AddSingleton<IRustSecurityBridge, StubRustSecurityBridge>();
#endif
        return services;
    }
}

#if !RUST_SECURITY_GRPC
/// <summary>
/// Placeholder bridge — used until security.proto is generated and RUST_SECURITY_GRPC is defined
/// </summary>
internal class StubRustSecurityBridge : IRustSecurityBridge
{
    private readonly ILogger<StubRustSecurityBridge> _logger;

    public StubRustSecurityBridge(ILogger<StubRustSecurityBridge> logger)
    {
        _logger = logger;
        _logger.LogWarning("Using StubRustSecurityBridge — define RUST_SECURITY_GRPC and generate SecurityProto to enable Rust gRPC bridge");
    }

    public Task<byte[]> EncryptAsync(byte[] plaintext, byte[] key)
        => Task.FromException<byte[]>(new SecurityException("Rust Security gRPC not configured. Define RUST_SECURITY_GRPC and generate proto."));

    public Task<byte[]> DecryptAsync(byte[] ciphertext, byte[] key)
        => Task.FromException<byte[]>(new SecurityException("Rust Security gRPC not configured."));

    public Task<string> HashPasswordAsync(string password, byte[] salt)
        => Task.FromException<string>(new SecurityException("Rust Security gRPC not configured."));

    public Task<bool> VerifyPasswordAsync(string password, byte[] salt, string hash)
        => Task.FromException<bool>(new SecurityException("Rust Security gRPC not configured."));

    public Task<SecureToken> GenerateTokenAsync(string userId, string[] permissions, long expirySeconds)
        => Task.FromException<SecureToken>(new SecurityException("Rust Security gRPC not configured."));

    public Task<TokenValidationResult> VerifyTokenAsync(byte[] tokenData)
        => Task.FromResult(new TokenValidationResult(false, string.Empty, Array.Empty<string>()));
}
#endif
