namespace InsuranceClaimSystem.Application.Interfaces.External;

public interface IStripeService
{
    Task<StripePaymentIntent> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        string idempotencyKey,
        Dictionary<string, string> metadata);

    Task<StripePaymentConfirmation> ConfirmPaymentAsync(string paymentIntentId);
}

public class StripePaymentIntent
{
    public string Id { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class StripePaymentConfirmation
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FailureMessage { get; set; }
    public decimal Amount { get; set; }
}
