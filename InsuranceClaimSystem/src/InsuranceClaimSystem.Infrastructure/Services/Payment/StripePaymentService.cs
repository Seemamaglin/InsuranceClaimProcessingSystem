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

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = idempotencyKey
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options, requestOptions);

            return new StripePaymentIntent
            {
                Id = intent.Id,
                ClientSecret = intent.ClientSecret,
                Amount = amount,
                Currency = currency,
                Status = intent.Status
            };
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
            var service = new PaymentIntentService();
            var intent = await service.ConfirmAsync(paymentIntentId);

            return new StripePaymentConfirmation
            {
                Id = paymentIntentId,
                Status = intent.Status,
                FailureMessage = intent.LastPaymentError?.Message,
                Amount = intent.Amount / 100m
            };
        }
        catch (StripeException ex)
        {
            throw new BusinessRuleException($"Stripe: {ex.Message}");
        }
    }
}
