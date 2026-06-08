using FluentAssertions;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Domain.Exceptions;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services;

public class ClaimValidationServiceTests
{
    private readonly Mock<IPolicyRepository> _policyRepositoryMock;
    private readonly Mock<IClaimRepository> _claimRepositoryMock;
    private readonly Mock<INomineeRepository> _nomineeRepositoryMock;
    private readonly Mock<IClaimTypeRepository> _claimTypeRepositoryMock;
    private readonly Mock<ILogger<ClaimValidationService>> _loggerMock;
    private readonly AppDbContext _dbContext;
    private readonly ClaimValidationService _validationService;

    public ClaimValidationServiceTests()
    {
        _policyRepositoryMock = new Mock<IPolicyRepository>();
        _claimRepositoryMock = new Mock<IClaimRepository>();
        _nomineeRepositoryMock = new Mock<INomineeRepository>();
        _claimTypeRepositoryMock = new Mock<IClaimTypeRepository>();
        _loggerMock = new Mock<ILogger<ClaimValidationService>>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        _validationService = new ClaimValidationService(
            _policyRepositoryMock.Object,
            _claimRepositoryMock.Object,
            _nomineeRepositoryMock.Object,
            _claimTypeRepositoryMock.Object,
            _dbContext,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ValidateSubmission_WithActivePolicy_ShouldPass()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var policyTypeId = Guid.NewGuid();
        var claimTypeId = Guid.NewGuid();
        var policyHolderId = Guid.NewGuid();

        var policy = new Policy { Id = policyId, PolicyHolderId = policyHolderId, PolicyTypeId = policyTypeId, Status = PolicyStatus.Active, StartDate = DateTime.UtcNow.AddDays(-60), RemainingCoverageAmount = 100000 };
        var claimType = new ClaimType { Id = claimTypeId, IsMaturityClaim = false };
        var benefitRule = new PolicyBenefitRule { PolicyTypeId = policyTypeId, ClaimTypeId = claimTypeId, IsActive = true, WaitingPeriodDays = 30, SubLimitAmount = 50000, CoPayPercent = 10, DeductibleAmount = 500 };

        var request = new SubmitClaimRequest { PolicyId = policyId, ClaimTypeId = claimTypeId, IncidentDate = DateTime.UtcNow.AddDays(-30), ClaimedAmount = 10000, ClaimantType = ClaimantType.Policyholder };

        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _claimRepositoryMock.Setup(x => x.HasOpenClaimAsync(policyId)).ReturnsAsync(false);
        _claimTypeRepositoryMock.Setup(x => x.GetByIdAsync(claimTypeId)).ReturnsAsync(claimType);
        _claimRepositoryMock.Setup(x => x.HasMaturityClaimAsync(policyId)).ReturnsAsync(false);
        _dbContext.PolicyBenefitRules.Add(benefitRule);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.ValidateSubmissionAsync(request, policyHolderId);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSubmission_WithLapsedPolicy_ShouldFail()
    {
        // Arrange
        var policy = new Policy { Id = Guid.NewGuid(), Status = PolicyStatus.Lapsed };
        var request = new SubmitClaimRequest { PolicyId = policy.Id, ClaimTypeId = Guid.NewGuid(), ClaimedAmount = 5000 };
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policy.Id)).ReturnsAsync(policy);

        // Act
        var act = async () => await _validationService.ValidateSubmissionAsync(request, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task ValidateSubmission_WithDuplicateOpenClaim_ShouldFail()
    {
        // Arrange
        var policy = new Policy { Id = Guid.NewGuid(), PolicyHolderId = Guid.NewGuid(), PolicyTypeId = Guid.NewGuid(), Status = PolicyStatus.Active };
        var request = new SubmitClaimRequest { PolicyId = policy.Id, ClaimTypeId = Guid.NewGuid(), ClaimedAmount = 5000 };
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policy.Id)).ReturnsAsync(policy);
        _claimRepositoryMock.Setup(x => x.HasOpenClaimAsync(policy.Id)).ReturnsAsync(true);

        // Act
        var act = async () => await _validationService.ValidateSubmissionAsync(request, policy.PolicyHolderId);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CalculatePayout_WithCoPay_ShouldDeductCoPay()
    {
        // Arrange
        var policyTypeId = Guid.NewGuid();
        var claimTypeId = Guid.NewGuid();
        var benefitRule = new PolicyBenefitRule { PolicyTypeId = policyTypeId, ClaimTypeId = claimTypeId, IsActive = true, SubLimitAmount = 100000, CoPayPercent = 20, DeductibleAmount = 0 };
        _dbContext.PolicyBenefitRules.Add(benefitRule);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.CalculatePayoutAsync(10000, claimTypeId, policyTypeId);

        // Assert
        result.Should().Be(8000); // 10000 * 0.8
    }

    [Fact]
    public async Task CalculatePayout_WithDeductible_ShouldDeductDeductible()
    {
        // Arrange
        var policyTypeId = Guid.NewGuid();
        var claimTypeId = Guid.NewGuid();
        var benefitRule = new PolicyBenefitRule { PolicyTypeId = policyTypeId, ClaimTypeId = claimTypeId, IsActive = true, SubLimitAmount = 100000, CoPayPercent = 0, DeductibleAmount = 1000 };
        _dbContext.PolicyBenefitRules.Add(benefitRule);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.CalculatePayoutAsync(10000, claimTypeId, policyTypeId);

        // Assert
        result.Should().Be(9000); // 10000 - 1000
    }

    [Fact]
    public async Task CalculatePayout_WithAmountAboveSubLimit_ShouldUseSubLimit()
    {
        // Arrange
        var policyTypeId = Guid.NewGuid();
        var claimTypeId = Guid.NewGuid();
        var benefitRule = new PolicyBenefitRule { PolicyTypeId = policyTypeId, ClaimTypeId = claimTypeId, IsActive = true, SubLimitAmount = 5000, CoPayPercent = 10, DeductibleAmount = 0 };
        _dbContext.PolicyBenefitRules.Add(benefitRule);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.CalculatePayoutAsync(10000, claimTypeId, policyTypeId);

        // Assert
        result.Should().Be(4500); // min(10000, 5000) * 0.9
    }
}