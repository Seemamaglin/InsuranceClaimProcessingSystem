namespace InsuranceClaimSystem.Application.Interfaces.External;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendTemplatedEmailAsync(string toEmail, string toName, EmailTemplate template, Dictionary<string, string> placeholders);
}
