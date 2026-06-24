using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using InsuranceClaimSystem.Domain.Exceptions;
using InsuranceClaimSystem.Infrastructure.Configuration;
using InsuranceClaimSystem.Infrastructure.Services.Payment;
using Microsoft.Extensions.Options;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services
{
    public class StripePaymentServiceTests
    {
        private readonly StripePaymentService _stripePaymentService;
        private readonly StripeSettings _stripeSettings;

        public StripePaymentServiceTests()
        {
            _stripeSettings = new StripeSettings
            {
                SecretKey = "sk_test_mock_key",
                PublishableKey = "pk_test_mock_key",
                WebhookSecret = "whsec_mock_key"
            };

            var optionsMock = Options.Create(_stripeSettings);
            _stripePaymentService = new StripePaymentService(optionsMock);
        }

        [Fact]
        public async Task CreatePaymentIntentAsync_ReturnsMockedPaymentIntent()
        {
            // Arrange
            decimal amount = 500m;
            string currency = "usd";
            string idempotencyKey = Guid.NewGuid().ToString();
            var metadata = new Dictionary<string, string> { { "PolicyId", Guid.NewGuid().ToString() } };

            // Act
            var result = await _stripePaymentService.CreatePaymentIntentAsync(amount, currency, idempotencyKey, metadata);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().StartWith("pi_mock_");
            result.ClientSecret.Should().StartWith("secret_mock_");
            result.Amount.Should().Be(amount);
            result.Currency.Should().Be(currency);
            result.Status.Should().Be("requires_payment_method");
        }

        [Fact]
        public async Task ConfirmPaymentAsync_ReturnsMockedConfirmation()
        {
            // Arrange
            string paymentIntentId = "pi_mock_12345";

            // Act
            var result = await _stripePaymentService.ConfirmPaymentAsync(paymentIntentId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(paymentIntentId);
            result.Status.Should().Be("succeeded");
            result.FailureMessage.Should().BeNull();
            result.Amount.Should().Be(300000m);
        }
    }
}
