using FluentAssertions;
using InsuranceClaimSystem.Application.Interfaces.External;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services;

public class PremiumPaymentServiceTests
{
    private readonly Mock<IPolicyRepository> _policyRepositoryMock;
    private readonly Mock<IPolicyPaymentRepository> _policyPaymentRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IStripeService> _stripeServiceMock;
    private readonly Mock<ILogger<PremiumPaymentService>> _loggerMock;
    private readonly PremiumPaymentService _premiumPaymentService;

    public PremiumPaymentServiceTests()
    {
        _policyRepositoryMock = new Mock<IPolicyRepository>();
        _policyPaymentRepositoryMock = new Mock<IPolicyPaymentRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _stripeServiceMock = new Mock<IStripeService>();
        _loggerMock = new Mock<ILogger<PremiumPaymentService>>();

        _premiumPaymentService = new PremiumPaymentService(
            _policyRepositoryMock.Object,
            _policyPaymentRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _stripeServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RecordFirstPremiumAsync_WithValidPolicy_ShouldCreatePayment()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var stripePaymentIntentId = "pi_test_123";
        var amount = 5000m;

        var policy = new Policy
        {
            Id = policyId,
            PolicyNumber = "POL-2026-0001",
            Status = PolicyStatus.Active,
            PremiumFrequency = PremiumFrequency.Monthly,
            NextPremiumDueDate = DateTime.UtcNow.AddMonths(1)
        };

        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _policyPaymentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<PolicyPayment>()))
            .ReturnsAsync((PolicyPayment p) => p);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _premiumPaymentService.RecordFirstPremiumAsync(policyId, amount, stripePaymentIntentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PolicyId.Should().Be(policyId);
        result.Value.Amount.Should().Be(amount);
        result.Value.Status.Should().Be(PolicyPaymentStatus.Paid);
        policy.LastPremiumPaidDate.Should().NotBeNull();
        policy.NextPremiumDueDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task RecordFirstPremiumAsync_WithNonExistingPolicy_ShouldReturnNotFound()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync((Policy?)null);

        // Act
        var result = await _premiumPaymentService.RecordFirstPremiumAsync(policyId, 5000m, "pi_test_123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PolicyNotFound");
    }

    [Fact]
    public async Task RecordPremiumPaymentAsync_WithValidPolicy_ShouldCreatePayment()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var stripePaymentIntentId = "pi_test_456";
        var amount = 5000m;

        var policy = new Policy
        {
            Id = policyId,
            PolicyNumber = "POL-2026-0002",
            Status = PolicyStatus.Active,
            PremiumFrequency = PremiumFrequency.Quarterly,
            NextPremiumDueDate = DateTime.UtcNow.AddMonths(3)
        };

        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _policyPaymentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<PolicyPayment>()))
            .ReturnsAsync((PolicyPayment p) => p);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _premiumPaymentService.RecordPremiumPaymentAsync(policyId, amount, stripePaymentIntentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PolicyId.Should().Be(policyId);
        result.Value.Amount.Should().Be(amount);
        result.Value.Status.Should().Be(PolicyPaymentStatus.Paid);
        result.Value.StripePaymentIntentId.Should().Be(stripePaymentIntentId);
    }

    [Fact]
    public async Task RecordPremiumPaymentAsync_WithNonExistingPolicy_ShouldReturnNotFound()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync((Policy?)null);

        // Act
        var result = await _premiumPaymentService.RecordPremiumPaymentAsync(policyId, 5000m, "pi_test_123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PolicyNotFound");
    }

    [Fact]
    public async Task GetLastPaymentAsync_WithExistingPayment_ShouldReturnPayment()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var paymentDate = DateTime.UtcNow.AddDays(-30);

        var payment = new PolicyPayment
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            Amount = 5000m,
            PaymentDate = paymentDate,
            Status = PolicyPaymentStatus.Paid,
            StripePaymentIntentId = "pi_test_789"
        };

        _policyPaymentRepositoryMock.Setup(x => x.GetLastPaymentAsync(policyId)).ReturnsAsync(payment);

        // Act
        var result = await _premiumPaymentService.GetLastPaymentAsync(policyId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(payment.Id);
        result.Value.Amount.Should().Be(5000m);
        result.Value.StripePaymentIntentId.Should().Be("pi_test_789");
    }

    [Fact]
    public async Task GetLastPaymentAsync_WithNoPayment_ShouldReturnNotFound()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        _policyPaymentRepositoryMock.Setup(x => x.GetLastPaymentAsync(policyId)).ReturnsAsync((PolicyPayment?)null);

        // Act
        var result = await _premiumPaymentService.GetLastPaymentAsync(policyId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentNotFound");
    }
}