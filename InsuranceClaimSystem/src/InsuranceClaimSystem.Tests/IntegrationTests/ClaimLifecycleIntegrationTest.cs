using AutoMapper;
using FluentAssertions;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Application.DTOs.Policies;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Infrastructure.Repositories;
using InsuranceClaimSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InsuranceClaimSystem.Tests.IntegrationTests;

public class ClaimLifecycleIntegrationTest : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ClaimService _claimService;
    private readonly PolicyService _policyService;
    private readonly IMapper _mapper;

    public ClaimLifecycleIntegrationTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Claim, ClaimDetailDto>();
            cfg.CreateMap<Claim, ClaimDto>();
            cfg.CreateMap<Policy, PolicyResponse>();
            cfg.CreateMap<PolicyType, PolicyTypeDto>();
        });
        _mapper = config.CreateMapper();

        var claimRepository = new ClaimRepository(_context);
        var policyRepository = new PolicyRepository(_context);
        var userRepository = new UserRepository(_context);
        var nomineeRepository = new NomineeRepository(_context);
        var claimTypeRepository = new ClaimTypeRepository(_context);
        var workflowHistoryRepository = new ClaimWorkflowHistoryRepository(_context);
        var documentRepository = new DocumentRepository(_context);
        var policyTypeRepository = new PolicyTypeRepository(_context);
        var unitOfWork = new UnitOfWork(_context);

        var validationLogger = new Mock<ILogger<ClaimValidationService>>();
        var validationService = new ClaimValidationService(policyRepository, claimRepository, nomineeRepository, claimTypeRepository, _context, validationLogger.Object);

        var claimLogger = new Mock<ILogger<ClaimService>>();
        _claimService = new ClaimService(claimRepository, policyRepository, userRepository, nomineeRepository, claimTypeRepository, workflowHistoryRepository, documentRepository, validationService, unitOfWork, _mapper, claimLogger.Object);

        var policyLogger = new Mock<ILogger<PolicyService>>();
        _policyService = new PolicyService(policyRepository, policyTypeRepository, userRepository, unitOfWork, _mapper, policyLogger.Object);
    }

    [Fact]
    public async Task FullClaimLifecycle_ShouldCompleteSuccessfully()
    {
        // 1. Create policyholder user
        var policyHolder = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1),
            Email = "john.doe@example.com",
            Username = "johndoe",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
            PhoneNumber = "1234567890",
            Role = UserRole.PolicyHolder,
            RegistrationStatus = RegistrationStatus.PendingApproval,
            IsActive = true
        };
        _context.Users.Add(policyHolder);

        // 2. Create policy type
        var policyType = new PolicyType
        {
            Id = Guid.NewGuid(),
            TypeName = "Health Insurance",
            Description = "Health insurance",
            IsActive = true,
            AllowsThirdPartyClaim = false
        };
        _context.PolicyTypes.Add(policyType);

        // Create claim type
        var claimType = new ClaimType
        {
            Id = Guid.NewGuid(),
            TypeName = "Hospitalization",
            PolicyTypeId = policyType.Id,
            IsMaturityClaim = false
        };
        _context.ClaimTypes.Add(claimType);

        // Create benefit rule
        var benefitRule = new PolicyBenefitRule
        {
            Id = Guid.NewGuid(),
            PolicyTypeId = policyType.Id,
            ClaimTypeId = claimType.Id,
            IsActive = true,
            WaitingPeriodDays = 0,
            SubLimitAmount = 50000,
            CoPayPercent = 10,
            DeductibleAmount = 500
        };
        _context.PolicyBenefitRules.Add(benefitRule);

        // Create reviewer
        var reviewer = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Reviewer",
            DateOfBirth = new DateTime(1985, 5, 15),
            Email = "jane.reviewer@example.com",
            Username = "janereviewer",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
            PhoneNumber = "9876543210",
            Role = UserRole.ClaimReviewer,
            Specialization = Specialization.All,
            RegistrationStatus = RegistrationStatus.PendingApproval,
            IsActive = true
        };
        _context.Users.Add(reviewer);
        await _context.SaveChangesAsync();

        // 3. Create and approve policy
        var createResult = await _policyService.CreatePolicyAsync(new CreatePolicyRequest
        {
            PolicyHolderId = policyHolder.Id,
            PolicyTypeId = policyType.Id,
            StartDate = DateTime.UtcNow.Date.AddDays(-30),
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            CoverageAmount = 100000,
            PremiumAmount = 5000,
            PremiumFrequency = PremiumFrequency.Monthly
        });
        createResult.IsSuccess.Should().BeTrue();

        var policyId = createResult.Value!.Id;
        var approveResult = await _policyService.ApprovePolicyAsync(policyId);
        approveResult.IsSuccess.Should().BeTrue();

        var approvedPolicy = await _context.Policies.FindAsync(policyId);
        approvedPolicy!.RemainingCoverageAmount.Should().Be(100000);

        // 4. Submit claim
        var submitResult = await _claimService.SubmitClaimAsync(new SubmitClaimRequest
        {
            PolicyId = policyId,
            ClaimTypeId = claimType.Id,
            IncidentDate = DateTime.UtcNow.AddDays(-10),
            IncidentDescription = "Hospitalization",
            IncidentLocation = "City Hospital",
            ClaimedAmount = 10000,
            ClaimantType = ClaimantType.Policyholder
        });
        submitResult.IsSuccess.Should().BeTrue();

        var claimId = submitResult.Value!.Id;
        var submittedClaim = await _context.Claims.FindAsync(claimId);
        submittedClaim!.Status.Should().Be(ClaimStatus.Submitted);

        // 5. Auto-assign reviewer
        var autoAssignResult = await _claimService.AutoAssignReviewerAsync(claimId);
        autoAssignResult.IsSuccess.Should().BeTrue();

        // 6. Update to UnderReview
        var updateResult = await _claimService.UpdateStatusAsync(claimId, new UpdateClaimStatusRequest { NewStatus = ClaimStatus.UnderReview, ChangedByUserId = reviewer.Id });
        updateResult.IsSuccess.Should().BeTrue();

        var claimUnderReview = await _context.Claims.FindAsync(claimId);
        claimUnderReview!.Status.Should().Be(ClaimStatus.UnderReview);

        // 7. Approve claim
        var approveClaimResult = await _claimService.UpdateStatusAsync(claimId, new UpdateClaimStatusRequest { NewStatus = ClaimStatus.Approved, ChangedByUserId = reviewer.Id });
        approveClaimResult.IsSuccess.Should().BeTrue();

        var approvedClaim = await _context.Claims.FindAsync(claimId);
        approvedClaim!.Status.Should().Be(ClaimStatus.Approved);
        approvedClaim.FinalPayableAmount.Should().BeGreaterThan(0);

        // 8. Close claim
        var closeResult = await _claimService.UpdateStatusAsync(claimId, new UpdateClaimStatusRequest { NewStatus = ClaimStatus.Closed, ChangedByUserId = reviewer.Id });
        closeResult.IsSuccess.Should().BeTrue();

        var closedClaim = await _context.Claims.FindAsync(claimId);
        closedClaim!.Status.Should().Be(ClaimStatus.Closed);
        closedClaim.ResolvedAt.Should().NotBeNull();

        // 9. Verify coverage decremented
        var finalPolicy = await _context.Policies.FindAsync(policyId);
        finalPolicy!.RemainingCoverageAmount.Should().BeLessThan(100000);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}