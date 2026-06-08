namespace InsuranceClaimSystem.Application.Interfaces.External;

public interface IAadhaarMaskingService
{
    string MaskAadhaar(string aadhaarNumber);
}