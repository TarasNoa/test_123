using System.Security.Cryptography;
using System.Text;
using Libr4.Auth.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;
using OtpNet;
using QRCoder;

namespace Libr4.Auth.Infrastructure.Services;

public sealed class TotpService : ITotpService
{
    private const string Issuer = "libr4";
    private readonly IDataProtector _protector;

    public TotpService(IDataProtectionProvider provider) =>
        _protector = provider.CreateProtector("libr4.auth.totp.v1");

    public TotpSetupResult GenerateSetup(string userEmail)
    {
        var secret = KeyGeneration.GenerateRandomKey(20);
        var base32 = Base32Encoding.ToString(secret);

        var uri = new OtpUri(OtpType.Totp, base32, userEmail, Issuer).ToString();

        using var qr = new QRCodeGenerator();
        using var data = qr.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(6);

        var encrypted = _protector.Protect(base32);
        return new TotpSetupResult(encrypted, uri, png);
    }

    public bool VerifyCode(string encryptedSecret, string code)
    {
        try
        {
            var base32 = _protector.Unprotect(encryptedSecret);
            var secret = Base32Encoding.ToBytes(base32);
            var totp = new Totp(secret);
            return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }
}
