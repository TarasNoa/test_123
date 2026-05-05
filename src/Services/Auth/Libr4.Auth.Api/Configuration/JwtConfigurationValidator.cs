namespace Libr4.Auth.Api.Configuration;

/// <summary>
/// Validates JWT configuration on startup to prevent hardcoded secrets in production.
/// </summary>
public static class JwtConfigurationValidator
{
    private static readonly string[] DevKeys = new[]
    {
        "dev-super-secret-change-me-to-random-64-bytes-dev-super-secret-change-me",
        "your-secret-key",
        "secret",
        "dev-secret",
        "test-key",
        "1234567890"
    };

    public static void Validate(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var signingKey = configuration["Jwt:SigningKey"];
        var securityJwtKey = configuration["Security:JwtKey"];
        var watermarkKey = configuration["Security:WatermarkKey"];
        
        // In production, reject development keys
        if (!environment.IsDevelopment())
        {
            if (string.IsNullOrWhiteSpace(signingKey))
            {
                throw new InvalidOperationException(
                    "JWT SigningKey is not configured. " +
                    "Please set a secure key via environment variable JWT__SigningKey. " +
                    "Use: dotnet user-secrets init && dotnet user-secrets set \"Jwt:SigningKey\" \"<your-64-char-key>\"");
            }

            if (DevKeys.Any(k => signingKey.Equals(k, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "JWT SigningKey is using a development default. " +
                    "In production, you MUST use a cryptographically secure key of at least 64 bytes. " +
                    "Generate one with: openssl rand -base64 48");
            }

            if (signingKey.Length < 32)
            {
                throw new InvalidOperationException(
                    $"JWT SigningKey is too short ({signingKey.Length} chars). " +
                    "Minimum required: 32 characters. Recommended: 64+ characters.");
            }

            // Validate Security:JwtKey
            if (string.IsNullOrWhiteSpace(securityJwtKey) || securityJwtKey == "GENERATE_RANDOM_64_CHAR_STRING")
            {
                throw new InvalidOperationException(
                    "Security:JwtKey is not configured. " +
                    "Please set a secure key via environment variable Security__JwtKey. " +
                    "Generate one with: openssl rand -base64 48");
            }

            if (securityJwtKey.Length < 32)
            {
                throw new InvalidOperationException(
                    $"Security:JwtKey is too short ({securityJwtKey.Length} chars). " +
                    "Minimum required: 32 characters. Recommended: 64+ characters.");
            }

            // Validate Security:WatermarkKey
            if (string.IsNullOrWhiteSpace(watermarkKey) || watermarkKey == "GENERATE_RANDOM_32_CHAR_STRING")
            {
                throw new InvalidOperationException(
                    "Security:WatermarkKey is not configured. " +
                    "Please set a secure key via environment variable Security__WatermarkKey. " +
                    "Generate one with: openssl rand -base64 24");
            }

            if (watermarkKey.Length < 16)
            {
                throw new InvalidOperationException(
                    $"Security:WatermarkKey is too short ({watermarkKey.Length} chars). " +
                    "Minimum required: 16 characters. Recommended: 32+ characters.");
            }
        }

        // Log warning in development if using default
        if (environment.IsDevelopment() && DevKeys.Any(k => signingKey?.Equals(k, StringComparison.OrdinalIgnoreCase) == true))
        {
            Console.WriteLine("⚠️  WARNING: Using development JWT key. DO NOT use in production!");
        }

        if (environment.IsDevelopment() && (securityJwtKey == "GENERATE_RANDOM_64_CHAR_STRING" || watermarkKey == "GENERATE_RANDOM_32_CHAR_STRING"))
        {
            Console.WriteLine("⚠️  WARNING: Using placeholder Security keys. DO NOT use in production!");
        }
    }
}
