using AutoMapper;
using FluentAssertions;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
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

public class ClaimServiceTests
{
    private readonly Mock<IClaimRepository> _claimRepositoryMock;
    private readonly Mock<IPolicyRepository> _policyRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<INomineeRepository> _nomineeRepositoryMock;
    private readonly Mock<IClaimTypeRepository> _claimTypeRepositoryMock;
    private readonly Mock<IClaimWorkflowHistoryRepository> _workflowHistoryRepositoryMock;
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IClaimValidationService> _validationServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<ClaimService>> _loggerMock;
    private readonly AppDbContext _dbContext;
    private readonly ClaimService _claimService;

    public ClaimServiceTests()
    {
        _claimRepositoryMock = new Mock<IClaimRepository>();
        _policyRepositoryMock = new Mock<IPolicyRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _nomineeRepositoryMock = new Mock<INomineeRepository>();
        _claimTypeRepositoryMock = new Mock<IClaimTypeRepository>();
        _workflowHistoryRepositoryMock = new Mock<IClaimWorkflowHistoryRepository>();
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _validationServiceMock = new Mock<IClaimValidationService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<ClaimService>>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        _claimService = new ClaimService(
            _claimRepositoryMock.Object,
            _policyRepositoryMock.Object,
            _userRepositoryMock.Object,
            _nomineeRepositoryMock.Object,
            _claimTypeRepositoryMock.Object,
            _workflowHistoryRepositoryMock.Object,
            _documentRepositoryMock.Object,
            _validationServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _dbContext);
    }

    [Fact]
    public async Task SubmitClaim_WithActivePolicy_ShouldReturnSuccess()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var claimTypeId = Guid.NewGuid();
        var policyHolderId = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            PolicyHolderId = policyHolderId,
            PolicyTypeId = Guid.NewGuid(),
            Status = PolicyStatus.Active,
            CoverageAmount = 100000,
            RemainingCoverageAmount = 100000
        };

        var claimType = new ClaimType
        {
            Id = claimTypeId,
            IsMaturityClaim = false
        };

        var request = new SubmitClaimRequest
        {
            PolicyId = policyId,
            ClaimTypeId = claimTypeId,
            IncidentDate = DateTime.UtcNow.AddDays(-30),
            IncidentDescription = "Test claim",
            IncidentLocation = "Test location",
            ClaimedAmount = 5000,
            ClaimantType = ClaimantType.Policyholder
        };

        var validationResult = new ClaimValidationResult
        {
            IsValid = true,
            IsLateIntimation = false,
            DeductibleAmount = 0,
            CoPayPercentage = 0
        };

        var claimDetailDto = new ClaimDetailDto
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            Status = ClaimStatus.Submitted
        };

        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _validationServiceMock.Setup(x => x.ValidateSubmissionAsync(request, policyHolderId)).ReturnsAsync(validationResult);
        _claimTypeRepositoryMock.Setup(x => x.GetByIdAsync(claimTypeId)).ReturnsAsync(claimType);
        _claimRepositoryMock.Setup(x => x.CountByStatusAsync(ClaimStatus.Submitted)).ReturnsAsync(1);
        _claimRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Claim>())).ReturnsAsync((Claim c) => c);
        _workflowHistoryRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ClaimWorkflowHistory>())).ReturnsAsync((ClaimWorkflowHistory cwh) => cwh);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _claimRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync(new Claim
        {
            Id = claimDetailDto.Id,
            PolicyId = policyId,
            Status = ClaimStatus.Submitted,
            ClaimType = claimType,
            Policy = policy
        });
        _mapperMock.Setup(x => x.Map<ClaimDetailDto>(It.IsAny<Claim>())).Returns(claimDetailDto);

        // Act
        var result = await _claimService.SubmitClaimAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(ClaimStatus.Submitted);
    }

    [Fact]
    public async Task SubmitClaim_WithLapsedPolicy_ShouldReturnFailure()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var policyHolderId = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            PolicyHolderId = policyHolderId,
            Status = PolicyStatus.Lapsed
        };

        var request = new SubmitClaimRequest
        {
            PolicyId = policyId,
            ClaimTypeId = Guid.NewGuid(),
            ClaimedAmount = 5000,
            ClaimantType = ClaimantType.Policyholder
        };

        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _validationServiceMock.Setup(x => x.ValidateSubmissionAsync(request, policyHolderId))
            .ThrowsAsync(new BusinessRuleException("Policy is not active. Current status: Lapsed"));

        // Act
        var result = await _claimService.SubmitClaimAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitClaim_WithDuplicateOpenClaim_ShouldReturnFailure()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var policyHolderId = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            PolicyHolderId = policyHolderId,
            PolicyTypeId = Guid.NewGuid(),
            Status = PolicyStatus.Active
        };

        var request = new SubmitClaimRequest
        {
            PolicyId = policyId,
            ClaimTypeId = Guid.NewGuid(),
            ClaimedAmount = 5000,
            ClaimantType = ClaimantType.Policyholder
        };

        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _validationServiceMock.Setup(x => x.ValidateSubmissionAsync(request, policyHolderId))
            .ThrowsAsync(new BusinessRuleException("An open claim already exists for this policy."));

        // Act
        var result = await _claimService.SubmitClaimAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Description.Should().Contain("open claim");
    }

    [Fact]
    public async Task SubmitClaim_WithCoverageExceeded_ShouldReturnFailure()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var policyHolderId = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            PolicyHolderId = policyHolderId,
            PolicyTypeId = Guid.NewGuid(),
            Status = PolicyStatus.Active,
            RemainingCoverageAmount = 1000
        };

        var request = new SubmitClaimRequest
        {
            PolicyId = policyId,
            ClaimTypeId = Guid.NewGuid(),
            ClaimedAmount = 50000,
            ClaimantType = ClaimantType.Policyholder
        };

        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _validationServiceMock.Setup(x => x.ValidateSubmissionAsync(request, policyHolderId))
            .ThrowsAsync(new BusinessRuleException("Claimed amount exceeds remaining coverage amount of 1000."));

        // Act
        var result = await _claimService.SubmitClaimAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Description.Should().Contain("exceeds");
    }

    [Fact]
    public async Task GetClaimById_WithExistingId_ShouldReturnClaim()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var claim = new Claim
        {
            Id = claimId,
            ClaimNumber = "CLM-2026-0001",
            Status = ClaimStatus.Submitted
        };

        var claimDetailDto = new ClaimDetailDto
        {
            Id = claimId,
            Status = ClaimStatus.Submitted
        };

        _claimRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(claimId)).ReturnsAsync(claim);
        _mapperMock.Setup(x => x.Map<ClaimDetailDto>(claim)).Returns(claimDetailDto);

        // Act
        var result = await _claimService.GetClaimByIdAsync(claimId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(claimId);
    }

    [Fact]
    public async Task GetClaimById_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        _claimRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(claimId)).ReturnsAsync((Claim?)null);

        // Act
        var result = await _claimService.GetClaimByIdAsync(claimId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ClaimNotFound");
    }

    [Fact]
    public async Task UpdateStatus_WithValidTransition_ShouldReturnSuccess()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var policyId = Guid.NewGuid();

        var claim = new Claim
        {
            Id = claimId,
            PolicyId = policyId,
            Status = ClaimStatus.Submitted,
            ClaimedAmount = 5000,
            ClaimTypeId = Guid.NewGuid()
        };

        var policy = new Policy
        {
            Id = policyId,
            PolicyTypeId = Guid.NewGuid()
        };

        var request = new UpdateClaimStatusRequest
        {
            NewStatus = ClaimStatus.UnderReview,
            ChangedByUserId = Guid.NewGuid()
        };

        _claimRepositoryMock.Setup(x => x.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _claimRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Claim>())).Returns(Task.CompletedTask);
        _workflowHistoryRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ClaimWorkflowHistory>())).ReturnsAsync((ClaimWorkflowHistory cwh) => cwh);
        _validationServiceMock.Setup(x => x.CalculatePayoutAsync(It.IsAny<decimal>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(4000);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _claimService.UpdateStatusAsync(claimId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidTransition_ShouldReturnFailure()
    {
        // Arrange
        var claimId = Guid.NewGuid();

        var claim = new Claim
        {
            Id = claimId,
            Status = ClaimStatus.Submitted
        };

        var request = new UpdateClaimStatusRequest
        {
            NewStatus = ClaimStatus.Approved,
            ChangedByUserId = Guid.NewGuid()
        };

        _claimRepositoryMock.Setup(x => x.GetByIdAsync(claimId)).ReturnsAsync(claim);

        // Act
        var result = await _claimService.UpdateStatusAsync(claimId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("InvalidTransition");
    }

    [Fact]
    public async Task AutoAssignReviewer_WithMatchingSpecialization_ShouldAssignReviewer()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var claimTypeId = Guid.NewGuid();

        var claim = new Claim
        {
            Id = claimId,
            ClaimTypeId = claimTypeId,
            PolicyId = Guid.NewGuid(),
            Status = ClaimStatus.Submitted
        };

        var claimType = new ClaimType
        {
            Id = claimTypeId
        };

        var reviewer = new User
        {
            Id = reviewerId,
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.ClaimReviewer,
            Specialization = Specialization.Health,
            IsActive = true
        };

        _claimRepositoryMock.Setup(x => x.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _claimTypeRepositoryMock.Setup(x => x.GetByIdAsync(claimTypeId)).ReturnsAsync(claimType);
        _userRepositoryMock.Setup(x => x.GetUsersByRoleAsync(UserRole.ClaimReviewer)).ReturnsAsync(new List<User> { reviewer });
        _claimRepositoryMock.Setup(x => x.GetActiveClaimCountByReviewerAsync(reviewerId)).ReturnsAsync(0);
        _claimRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Claim>())).Returns(Task.CompletedTask);
        _workflowHistoryRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ClaimWorkflowHistory>())).ReturnsAsync((ClaimWorkflowHistory cwh) => cwh);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _claimService.AutoAssignReviewerAsync(claimId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        claim.AssignedReviewerId.Should().Be(reviewerId);
    }

    [Fact]
    public async Task AutoAssignReviewer_WithNoMatchingSpecialization_ShouldAssignAnyReviewer()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var claim = new Claim
        {
            Id = claimId,
            ClaimTypeId = Guid.NewGuid(),
            PolicyId = Guid.NewGuid(),
            Status = ClaimStatus.Submitted
        };

        var reviewer = new User
        {
            Id = reviewerId,
            FirstName = "Jane",
            LastName = "Smith",
            Role = UserRole.ClaimReviewer,
            Specialization = Specialization.All,
            IsActive = true
        };

        _claimRepositoryMock.Setup(x => x.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _claimTypeRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ClaimType?)null);
        _userRepositoryMock.Setup(x => x.GetUsersByRoleAsync(UserRole.ClaimReviewer)).ReturnsAsync(new List<User> { reviewer });
        _claimRepositoryMock.Setup(x => x.GetActiveClaimCountByReviewerAsync(reviewerId)).ReturnsAsync(0);
        _claimRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Claim>())).Returns(Task.CompletedTask);
        _workflowHistoryRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ClaimWorkflowHistory>())).ReturnsAsync((ClaimWorkflowHistory cwh) => cwh);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _claimService.AutoAssignReviewerAsync(claimId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveClaim_ByManager_ShouldSetApprovedStatusAndCalculatePayout()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var policyTypeId = Guid.NewGuid();
        var claimTypeId = Guid.NewGuid();

        var claim = new Claim
        {
            Id = claimId,
            PolicyId = policyId,
            ClaimTypeId = claimTypeId,
            Status = ClaimStatus.UnderReview,
            ClaimedAmount = 10000,
            CoPayPercentage = 10,
            DeductibleAmount = 500
        };

        var policy = new Policy
        {
            Id = policyId,
            PolicyTypeId = policyTypeId
        };

        var request = new UpdateClaimStatusRequest
        {
            NewStatus = ClaimStatus.Approved,
            ChangedByUserId = Guid.NewGuid()
        };

        _claimRepositoryMock.Setup(x => x.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _claimRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Claim>())).Returns(Task.CompletedTask);
        _workflowHistoryRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ClaimWorkflowHistory>())).ReturnsAsync((ClaimWorkflowHistory cwh) => cwh);
        _validationServiceMock.Setup(x => x.CalculatePayoutAsync(10000, claimTypeId, policyTypeId)).ReturnsAsync(8500);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _claimService.UpdateStatusAsync(claimId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        claim.Status.Should().Be(ClaimStatus.Approved);
        claim.FinalPayableAmount.Should().Be(8500);
    }

    [Fact]
    public async Task RejectClaim_ByManager_ShouldSetRejectedStatus()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var rejectionReason = "Insufficient documentation";

        var claim = new Claim
        {
            Id = claimId,
            Status = ClaimStatus.UnderReview,
            ClaimedAmount = 5000
        };

        var request = new UpdateClaimStatusRequest
        {
            NewStatus = ClaimStatus.Rejected,
            RejectionReason = rejectionReason,
            ChangedByUserId = Guid.NewGuid()
        };

        _claimRepositoryMock.Setup(x => x.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _claimRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Claim>())).Returns(Task.CompletedTask);
        _workflowHistoryRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ClaimWorkflowHistory>())).ReturnsAsync((ClaimWorkflowHistory cwh) => cwh);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _claimService.UpdateStatusAsync(claimId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        claim.Status.Should().Be(ClaimStatus.Rejected);
        claim.RejectionReason.Should().Be(rejectionReason);
    }

    [Fact]
    public async Task SubmitClaim_WithMaturityClaimType_ShouldCheckForDuplicate()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var claimTypeId = Guid.NewGuid();
        var policyHolderId = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            PolicyHolderId = policyHolderId,
            PolicyTypeId = Guid.NewGuid(),
            Status = PolicyStatus.Active
        };

        var request = new SubmitClaimRequest
        {
            PolicyId = policyId,
            ClaimTypeId = claimTypeId,
            ClaimedAmount = 50000,
            ClaimantType = ClaimantType.Policyholder
        };

        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _validationServiceMock.Setup(x => x.ValidateSubmissionAsync(request, policyHolderId))
            .ThrowsAsync(new BusinessRuleException("A maturity claim already exists for this policy."));

        // Act
        var result = await _claimService.SubmitClaimAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Description.Should().Contain("maturity claim");
    }

    [Fact]
    public async Task SubmitClaim_WithWaitingPeriodNotMet_ShouldReturnFailure()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var policyHolderId = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            PolicyHolderId = policyHolderId,
            PolicyTypeId = Guid.NewGuid(),
            Status = PolicyStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-5)
        };

        var request = new SubmitClaimRequest
        {
            PolicyId = policyId,
            ClaimTypeId = Guid.NewGuid(),
            IncidentDate = DateTime.UtcNow.AddDays(-3),
            ClaimedAmount = 5000,
            ClaimantType = ClaimantType.Policyholder
        };

        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _validationServiceMock.Setup(x => x.ValidateSubmissionAsync(request, policyHolderId))
            .ThrowsAsync(new BusinessRuleException("Waiting period of 30 days not completed."));

        // Act
        var result = await _claimService.SubmitClaimAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Description.Should().Contain("Waiting period");
    }

    [Fact]
    public async Task AssignReviewer_WithValidReviewer_ShouldAssignSuccessfully()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var claim = new Claim
        {
            Id = claimId,
            ClaimTypeId = Guid.NewGuid(),
            PolicyId = Guid.NewGuid(),
            Status = ClaimStatus.Submitted
        };

        var reviewer = new User
        {
            Id = reviewerId,
            FirstName = "Test",
            LastName = "Reviewer",
            Role = UserRole.ClaimReviewer,
            IsActive = true
        };

        var request = new AssignReviewerRequest
        {
            ClaimId = claimId,
            ReviewerId = reviewerId,
            AssignedByUserId = Guid.NewGuid()
        };

        _claimRepositoryMock.Setup(x => x.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(reviewerId)).ReturnsAsync(reviewer);
        _claimTypeRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ClaimType?)null);
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Policy?)null);
        _claimRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Claim>())).Returns(Task.CompletedTask);
        _workflowHistoryRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ClaimWorkflowHistory>())).ReturnsAsync((ClaimWorkflowHistory cwh) => cwh);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _claimService.AssignReviewerAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        claim.AssignedReviewerId.Should().Be(reviewerId);
    }

    [Fact]
    public async Task GetClaimByNumber_WithExistingNumber_ShouldReturnClaim()
    {
        // Arrange
        var claimNumber = "CLM-2026-0001";
        var claimId = Guid.NewGuid();

        var claim = new Claim
        {
            Id = claimId,
            ClaimNumber = claimNumber,
            PolicyId = Guid.NewGuid(),
            Status = ClaimStatus.Submitted
        };

        var claimDetailDto = new ClaimDetailDto
        {
            Id = claimId,
            Status = ClaimStatus.Submitted
        };

        _claimRepositoryMock.Setup(x => x.GetClaimByNumberAsync(claimNumber)).ReturnsAsync(claim);
        _claimRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(claimId)).ReturnsAsync(claim);
        _mapperMock.Setup(x => x.Map<ClaimDetailDto>(claim)).Returns(claimDetailDto);

        // Act
        var result = await _claimService.GetClaimByNumberAsync(claimNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetClaimByNumber_WithNonExistingNumber_ShouldReturnNotFound()
    {
        // Arrange
        var claimNumber = "CLM-2026-9999";
        _claimRepositoryMock.Setup(x => x.GetClaimByNumberAsync(claimNumber)).ReturnsAsync((Claim?)null);

        // Act
        var result = await _claimService.GetClaimByNumberAsync(claimNumber);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ClaimNotFound");
    }
}