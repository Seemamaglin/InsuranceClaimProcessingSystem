namespace InsuranceClaimSystem.API.Configuration;

public class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@insuranceclaimsystem.com";
    public string FromName { get; set; } = "Insurance Claim System";
    public bool EnableSsl { get; set; } = false;
}