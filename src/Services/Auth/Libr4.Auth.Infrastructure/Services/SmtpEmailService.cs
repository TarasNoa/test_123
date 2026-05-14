using System.Net;
using System.Net.Mail;
using Libr4.Auth.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Libr4.Auth.Infrastructure.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpClient _smtp;
    private readonly string _from;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration cfg, ILogger<SmtpEmailService> logger)
    {
        _logger = logger;
        _from = cfg["Email:From"] ?? "noreply@libr4.com";
        _smtp = new SmtpClient(cfg["Email:SmtpHost"] ?? "localhost")
        {
            Port = cfg.GetValue("Email:SmtpPort", 587),
            Credentials = new NetworkCredential(
                cfg["Email:Username"],
                cfg["Email:Password"]),
            EnableSsl = cfg.GetValue("Email:EnableSsl", true),
        };
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            var msg = new MailMessage(_from, to, subject, htmlBody) { IsBodyHtml = true };
            await _smtp.SendMailAsync(msg, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email to {To} (subject: {Subject})", to, subject);
        }
    }
}
