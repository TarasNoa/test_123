namespace Libr4.Auth.Application.Abstractions;

public interface ITotpService
{
    /// <summary>Generates a fresh base32 secret and returns its encrypted form for persistence plus otpauth URI.</summary>
    TotpSetupResult GenerateSetup(string userEmail);

    /// <summary>Verifies a 6-digit code against an encrypted stored secret.</summary>
    bool VerifyCode(string encryptedSecret, string code);
}

public sealed record TotpSetupResult(string EncryptedSecret, string OtpAuthUri, byte[] QrPng);
