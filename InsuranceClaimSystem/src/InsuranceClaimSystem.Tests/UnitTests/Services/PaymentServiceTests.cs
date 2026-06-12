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

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<IClaimRepository> _claimRepositoryMock;
    private readonly Mock<IPolicyRepository> _policyRepositoryMock;
    private readonly Mock<IClaimWorkflowHistoryRepository> _workflowHistoryRepositoryMock;
    private readonly Mock<IStripeService> _stripeServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _claimRepositoryMock = new Mock<IClaimRepository>();
        _policyRepositoryMock = new Mock<IPolicyRepository>();
        _workflowHistoryRepositoryMock = new Mock<IClaimWorkflowHistoryRepository>();
        _stripeServiceMock = new Mock<IStripeService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<PaymentService>>();

        _paymentService = new PaymentService(
            _paymentRepositoryMock.Object,
            _claimRepositoryMock.Object,
            _policyRepositoryMock.Object,
            _workflowHistoryRepositoryMock.Object,
            _stripeServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_WithApprovedClaim_ShouldCreateIntent()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var paymentIntentId = "pi_test_123";

        var claim = CreateApprovedClaim(claimId, policyId, 50000m);
        var stripePaymentIntent = new StripePaymentIntent { Id = paymentIntentId, Status = "requires_payment_method" };

        _claimRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(claimId)).ReturnsAsync(claim);
        _paymentRepositoryMock.Setup(x => x.GetByClaimIdAsync(claimId)).ReturnsAsync(new List<ClaimPayment>());
        _stripeServiceMock.Setup(x => x.CreatePaymentIntentAsync(
            It.IsAny<decimal>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(stripePaymentIntent);
        _paymentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ClaimPayment>())).ReturnsAsync((ClaimPayment p) => p);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _paymentService.CreatePaymentIntentAsync(claimId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(paymentIntentId);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_WithUnapprovedClaim_ShouldReturnFailure()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var claim = new Claim
        {
            Id = claimId,
            ClaimNumber = "CLM-2026-0001",
            PolicyId = Guid.NewGuid(),
            Status = ClaimStatus.Submitted,
            FinalPayableAmount = 50000m,
            PaymentRecipientType = PaymentRecipientType.PolicyHolder,
            RecipientName = "John Doe",
            RecipientAccountNumber = "1234567890",
            RecipientBankName = "Test Bank"
        };

        _claimRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(claimId)).ReturnsAsync(claim);

        // Act
        var result = await _paymentService.CreatePaymentIntentAsync(claimId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ClaimNotApproved");
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_WithExistingCompletedPayment_ShouldReturnConflict()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var claim = CreateApprovedClaim(claimId, Guid.NewGuid(), 50000m);
        var existingPayments = new List<ClaimPayment>
        {
            new ClaimPayment
            {
                Id = Guid.NewGuid(),
                ClaimId = claimId,
                PaymentStatus = ClaimPaymentStatus.Completed
            }
        };

        _claimRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(claimId)).ReturnsAsync(claim);
        _paymentRepositoryMock.Setup(x => x.GetByClaimIdAsync(claimId)).ReturnsAsync(existingPayments);

        // Act
        var result = await _paymentService.CreatePaymentIntentAsync(claimId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentAlreadyCompleted");
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_WithZeroAmount_ShouldReturnFailure()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var claim = new Claim
        {
            Id = claimId,
            ClaimNumber = "CLM-2026-0001",
            PolicyId = Guid.NewGuid(),
            Status = ClaimStatus.Approved,
            FinalPayableAmount = 0,
            PaymentRecipientType = PaymentRecipientType.PolicyHolder,
            RecipientName = "John Doe",
            RecipientAccountNumber = "1234567890",
            RecipientBankName = "Test Bank"
        };

        _claimRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(claimId)).ReturnsAsync(claim);
        _paymentRepositoryMock.Setup(x => x.GetByClaimIdAsync(claimId)).ReturnsAsync(new List<ClaimPayment>());

        // Act
        var result = await _paymentService.CreatePaymentIntentAsync(claimId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("InvalidAmount");
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_WithNonExistingClaim_ShouldReturnNotFound()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        _claimRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(claimId)).ReturnsAsync((Claim?)null);

        // Act
        var result = await _paymentService.CreatePaymentIntentAsync(claimId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ClaimNotFound");
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WithSuccessfulPayment_ShouldCloseClaim()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var paymentIntentId = "pi_test_123";
        var policyId = Guid.NewGuid();

        var claim = new Claim
        {
            Id = claimId,
            PolicyId = policyId,
            Status = ClaimStatus.Approved,
            FinalPayableAmount = 50000m,
            AssignedManagerId = Guid.NewGuid()
        };

        var claimPayment = new ClaimPayment
        {
            Id = Guid.NewGuid(),
            ClaimId = claimId,
            StripePaymentIntentId = paymentIntentId,
            PaymentStatus = ClaimPaymentStatus.Pending,
            Amount = 50000m
        };

        var policy = new Policy
        {
            Id = policyId,
            RemainingCoverageAmount = 100000m
        };

        var stripeConfirmation = new StripePaymentConfirmation
        {
            Id = paymentIntentId,
            Status = "succeeded"
        };

        _claimRepositoryMock.Setup(x => x.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _paymentRepositoryMock.Setup(x => x.GetByClaimIdAsync(claimId)).ReturnsAsync(new List<ClaimPayment> { claimPayment });
        _stripeServiceMock.Setup(x => x.ConfirmPaymentAsync(paymentIntentId)).ReturnsAsync(stripeConfirmation);
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _paymentRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ClaimPayment>())).Returns(Task.CompletedTask);
        _workflowHistoryRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ClaimWorkflowHistory>())).ReturnsAsync((ClaimWorkflowHistory h) => h);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _paymentService.ConfirmPaymentAsync(claimId, paymentIntentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        claim.Status.Should().Be(ClaimStatus.Closed);
        claimPayment.PaymentStatus.Should().Be(ClaimPaymentStatus.Completed);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WithNonExistingClaim_ShouldReturnNotFound()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        _claimRepositoryMock.Setup(x => x.GetByIdAsync(claimId)).ReturnsAsync((Claim?)null);

        // Act
        var result = await _paymentService.ConfirmPaymentAsync(claimId, "pi_test_123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ClaimNotFound");
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WithAlreadyCompletedPayment_ShouldReturnConflict()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var paymentIntentId = "pi_test_123";

        var claim = new Claim { Id = claimId, Status = ClaimStatus.Approved };
        var claimPayment = new ClaimPayment
        {
            Id = Guid.NewGuid(),
            ClaimId = claimId,
            StripePaymentIntentId = paymentIntentId,
            PaymentStatus = ClaimPaymentStatus.Completed
        };

        _claimRepositoryMock.Setup(x => x.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _paymentRepositoryMock.Setup(x => x.GetByClaimIdAsync(claimId)).ReturnsAsync(new List<ClaimPayment> { claimPayment });

        // Act
        var result = await _paymentService.ConfirmPaymentAsync(claimId, paymentIntentId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentAlreadyCompleted");
    }

    [Fact]
    public async Task ConfirmPaymentAsync_WithFailedStripeConfirmation_ShouldReturnFailure()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var paymentIntentId = "pi_test_123";

        var claim = new Claim { Id = claimId, Status = ClaimStatus.Approved };
        var claimPayment = new ClaimPayment
        {
            Id = Guid.NewGuid(),
            ClaimId = claimId,
            StripePaymentIntentId = paymentIntentId,
            PaymentStatus = ClaimPaymentStatus.Pending,
            Amount = 50000m
        };

        var stripeConfirmation = new StripePaymentConfirmation
        {
            Id = paymentIntentId,
            Status = "failed",
            FailureMessage = "Card declined"
        };

        _claimRepositoryMock.Setup(x => x.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _paymentRepositoryMock.Setup(x => x.GetByClaimIdAsync(claimId)).ReturnsAsync(new List<ClaimPayment> { claimPayment });
        _stripeServiceMock.Setup(x => x.ConfirmPaymentAsync(paymentIntentId)).ReturnsAsync(stripeConfirmation);
        _paymentRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<ClaimPayment>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _paymentService.ConfirmPaymentAsync(claimId, paymentIntentId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PaymentFailed");
    }

    [Fact]
    public async Task GetPaymentByClaimIdAsync_WithExistingPayment_ShouldReturnPayment()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var processedAt = DateTime.UtcNow;

        var payments = new List<ClaimPayment>
        {
            new ClaimPayment
            {
                Id = paymentId,
                ClaimId = claimId,
                Amount = 50000m,
                PaymentMethod = PaymentMethod.BankTransfer,
                PaymentStatus = ClaimPaymentStatus.Completed,
                ProcessedAt = processedAt
            }
        };

        _paymentRepositoryMock.Setup(x => x.GetByClaimIdAsync(claimId)).ReturnsAsync(payments);

        // Act
        var result = await _paymentService.GetPaymentByClaimIdAsync(claimId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(paymentId);
        result.Value.Amount.Should().Be(50000m);
        result.Value.PaymentStatus.Should().Be(ClaimPaymentStatus.Completed);
    }

    private static Claim CreateApprovedClaim(Guid claimId, Guid policyId, decimal finalPayableAmount)
    {
        return new Claim
        {
            Id = claimId,
            ClaimNumber = "CLM-2026-0001",
            PolicyId = policyId,
            Status = ClaimStatus.Approved,
            FinalPayableAmount = finalPayableAmount,
            PaymentRecipientType = PaymentRecipientType.PolicyHolder,
            RecipientName = "John Doe",
            RecipientAccountNumber = "1234567890",
            RecipientBankName = "Test Bank"
        };
    }
}