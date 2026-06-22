using InsuranceClaimSystem.Application.Interfaces.External;
using InsuranceClaimSystem.Domain.Exceptions;
using InsuranceClaimSystem.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Stripe;

namespace InsuranceClaimSystem.Infrastructure.Services.Payment;

public class StripePaymentService : IStripeService
{
    public StripePaymentService(IOptions<StripeSettings> stripeSettings)
    {
        var settings = stripeSettings.Value;
        StripeConfiguration.ApiKey = settings.SecretKey;
    }

    public async Task<StripePaymentIntent> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        string idempotencyKey,
        Dictionary<string, string> metadata)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency,
                Metadata = metadata
            };

            // MOCK: Return a fake intent for local testing instead of hitting Stripe API
            return await Task.FromResult(new StripePaymentIntent
            {
                Id = "pi_mock_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                ClientSecret = "secret_mock_" + Guid.NewGuid().ToString("N"),
                Amount = amount,
                Currency = currency,
                Status = "requires_payment_method"
            });
        }
        catch (StripeException ex)
        {
            throw new BusinessRuleException($"Stripe: {ex.Message}");
        }
    }

    public async Task<StripePaymentConfirmation> ConfirmPaymentAsync(string paymentIntentId)
    {
        try
        {
            // MOCK: Return a fake confirmation for local testing instead of hitting Stripe API
            return await Task.FromResult(new StripePaymentConfirmation
            {
                Id = paymentIntentId,
                Status = "succeeded",
                FailureMessage = null,
                Amount = 300000m // Mocked amount
            });
        }
        catch (StripeException ex)
        {
            throw new BusinessRuleException($"Stripe: {ex.Message}");
        }
    }
}
