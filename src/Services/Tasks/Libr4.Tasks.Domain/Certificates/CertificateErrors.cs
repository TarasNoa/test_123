using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Tasks.Domain.Certificates;

public static class CertificateErrors
{
    public static readonly Error CertificateNotFound = Error.NotFound("certificates.not_found", "Certificate not found");
    public static readonly Error NotCertificateOwner = Error.Forbidden("certificates.not_owner", "You are not the owner of this certificate");
    public static readonly Error AlreadyVerified = Error.Conflict("certificates.already_verified", "Certificate is already verified");
    public static readonly Error CertificateExpired = Error.Conflict("certificates.expired", "Certificate has expired");
    public static readonly Error CertificateRevoked = Error.Conflict("certificates.revoked", "Certificate has been revoked");
    public static readonly Error InvalidStatus = Error.Validation("certificates.invalid_status", "Invalid certificate status");
}
