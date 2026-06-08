using InsuranceClaimSystem.Application.Interfaces.External;

namespace InsuranceClaimSystem.Infrastructure.Services;

public class AadhaarMaskingService : IAadhaarMaskingService
{
    public string MaskAadhaar(string aadhaarNumber)
    {
        if (string.IsNullOrWhiteSpace(aadhaarNumber))
            return "XXXX-XXXX-0000";

        // Remove all non-digit characters
        var digitsOnly = new string(aadhaarNumber.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length < 4)
            return "XXXX-XXXX-0000";

        var last4 = digitsOnly[^4..];
        return $"XXXX-XXXX-{last4}";
    }
}