using InsuranceClaimSystem.Application.Interfaces.External;
using InsuranceClaimSystem.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace InsuranceClaimSystem.Infrastructure.Services.Email;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpSettings> smtpSettings, ILogger<SmtpEmailService> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = isHtml ? body : null,
            TextBody = isHtml ? System.Net.WebUtility.HtmlEncode(body) : body
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        var secureSocketOptions = _smtpSettings.EnableSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, secureSocketOptions);

        if (!string.IsNullOrEmpty(_smtpSettings.Username))
        {
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Email sent to {To} with subject {Subject}", to, subject);
    }

    public async Task SendTemplatedEmailAsync(string toEmail, string toName, EmailTemplate template, Dictionary<string, string> placeholders)
    {
        var html = template switch
        {
            EmailTemplate.EmailVerification => "Click here to verify: {{VerificationLink}}",
            EmailTemplate.ForgotPassword => "Reset your password: {{ResetLink}}",
            EmailTemplate.RegistrationApproved => "Your account is approved. Username: {{Username}}",
            EmailTemplate.ClaimApproved => "Your claim {{ClaimNumber}} has been approved. Amount: {{Amount}}",
            EmailTemplate.ClaimRejected => "Your claim {{ClaimNumber}} was rejected. Reason: {{Reason}}",
            _ => ""
        };

        foreach (var (key, value) in placeholders)
        {
            html = html.Replace($"{{{{{key}}}}}", value);
        }

        await SendEmailAsync(toEmail, template.ToString(), html, isHtml: true);
    }
}
