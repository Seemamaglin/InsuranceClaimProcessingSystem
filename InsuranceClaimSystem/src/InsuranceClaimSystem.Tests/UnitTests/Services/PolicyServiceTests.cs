using AutoMapper;
using FluentAssertions;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Policies;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services;

public class PolicyServiceTests
{
    private readonly Mock<IPolicyRepository> _policyRepositoryMock;
    private readonly Mock<IPolicyTypeRepository> _policyTypeRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<PolicyService>> _loggerMock;
    private readonly PolicyService _policyService;

    public PolicyServiceTests()
    {
        _policyRepositoryMock = new Mock<IPolicyRepository>();
        _policyTypeRepositoryMock = new Mock<IPolicyTypeRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<PolicyService>>();

        _policyService = new PolicyService(
            _policyRepositoryMock.Object,
            _policyTypeRepositoryMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreatePolicy_WithValidData_ShouldReturnPolicyResponse()
    {
        // Arrange
        var policyHolderId = Guid.NewGuid();
        var policyTypeId = Guid.NewGuid();

        var policyHolder = new User { Id = policyHolderId, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        var policyType = new PolicyType { Id = policyTypeId, TypeName = "Health", IsActive = true };

        var request = new CreatePolicyRequest
        {
            PolicyHolderId = policyHolderId,
            PolicyTypeId = policyTypeId,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            CoverageAmount = 500000,
            PremiumAmount = 5000,
            PremiumFrequency = PremiumFrequency.Annually
        };

        var policy = new Policy { Id = Guid.NewGuid(), PolicyNumber = "POL-2026-0001", PolicyHolderId = policyHolderId, PolicyTypeId = policyTypeId, Status = PolicyStatus.PendingApproval, CoverageAmount = 500000 };
        var policyResponse = new PolicyResponse { Id = policy.Id, PolicyNumber = policy.PolicyNumber, Status = PolicyStatus.PendingApproval };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(policyHolderId)).ReturnsAsync(policyHolder);
        _policyTypeRepositoryMock.Setup(x => x.GetByIdAsync(policyTypeId)).ReturnsAsync(policyType);
        _policyRepositoryMock.Setup(x => x.CountByStatusAsync(PolicyStatus.PendingApproval)).ReturnsAsync(0);
        _policyRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Policy>())).ReturnsAsync((Policy p) => p);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(policy);
        _mapperMock.Setup(x => x.Map<PolicyResponse>(policy)).Returns(policyResponse);

        // Act
        var result = await _policyService.CreatePolicyAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PolicyNumber.Should().Be("POL-2026-0001");
    }

    [Fact]
    public async Task CreatePolicy_WithInvalidHolder_ShouldReturnNotFound()
    {
        // Arrange
        var request = new CreatePolicyRequest { PolicyHolderId = Guid.NewGuid(), PolicyTypeId = Guid.NewGuid(), CoverageAmount = 500000 };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act
        var result = await _policyService.CreatePolicyAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PolicyHolderNotFound");
    }

    [Fact]
    public async Task CreatePolicy_WithInvalidType_ShouldReturnNotFound()
    {
        // Arrange
        var policyHolder = new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" };
        var request = new CreatePolicyRequest { PolicyHolderId = policyHolder.Id, PolicyTypeId = Guid.NewGuid(), CoverageAmount = 500000 };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(policyHolder.Id)).ReturnsAsync(policyHolder);
        _policyTypeRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PolicyType?)null);

        // Act
        var result = await _policyService.CreatePolicyAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PolicyTypeNotFound");
    }

    [Fact]
    public async Task ApprovePolicy_WithPendingPolicy_ShouldSetActive()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.Date;
        var policy = new Policy { Id = policyId, Status = PolicyStatus.PendingApproval, StartDate = startDate, EndDate = startDate.AddYears(1), PremiumFrequency = PremiumFrequency.Monthly };
        var policyResponse = new PolicyResponse { Id = policyId, Status = PolicyStatus.Active };

        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<PolicyResponse>(policy)).Returns(policyResponse);

        // Act
        var result = await _policyService.ApprovePolicyAsync(policyId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        policy.Status.Should().Be(PolicyStatus.Active);
    }

    [Fact]
    public async Task RejectPolicy_WithPendingPolicy_ShouldSetRejected()
    {
        // Arrange
        var policy = new Policy { Id = Guid.NewGuid(), Status = PolicyStatus.PendingApproval };
        var policyResponse = new PolicyResponse { Id = policy.Id, Status = PolicyStatus.Rejected };
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policy.Id)).ReturnsAsync(policy);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<PolicyResponse>(policy)).Returns(policyResponse);

        // Act
        var result = await _policyService.RejectPolicyAsync(policy.Id, "Incomplete docs");

        // Assert
        result.IsSuccess.Should().BeTrue();
        policy.Status.Should().Be(PolicyStatus.Rejected);
    }

    [Fact]
    public async Task GetPolicyById_WithExistingId_ShouldReturnPolicy()
    {
        // Arrange
        var policy = new Policy { Id = Guid.NewGuid(), PolicyNumber = "POL-2026-0001", Status = PolicyStatus.Active };
        var policyResponse = new PolicyResponse { Id = policy.Id, PolicyNumber = policy.PolicyNumber };
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policy.Id)).ReturnsAsync(policy);
        _mapperMock.Setup(x => x.Map<PolicyResponse>(policy)).Returns(policyResponse);

        // Act
        var result = await _policyService.GetPolicyByIdAsync(policy.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(policy.Id);
    }

    [Fact]
    public async Task GetPolicyById_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Policy?)null);

        // Act
        var result = await _policyService.GetPolicyByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PolicyNotFound");
    }

    [Fact]
    public async Task DeletePolicy_WithNoClaims_ShouldReturnSuccess()
    {
        // Arrange
        var policy = new Policy { Id = Guid.NewGuid(), Status = PolicyStatus.Active, Claims = new List<Claim>() };
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policy.Id)).ReturnsAsync(policy);
        _policyRepositoryMock.Setup(x => x.DeleteAsync(policy.Id)).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _policyService.DeletePolicyAsync(policy.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePolicy_WithExistingClaims_ShouldReturnFailure()
    {
        // Arrange
        var policy = new Policy { Id = Guid.NewGuid(), Status = PolicyStatus.Active, Claims = new List<Claim> { new Claim { Id = Guid.NewGuid(), IsDeleted = false } } };
        _policyRepositoryMock.Setup(x => x.GetByIdAsync(policy.Id)).ReturnsAsync(policy);

        // Act
        var result = await _policyService.DeletePolicyAsync(policy.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PolicyHasClaims");
    }

    [Fact]
    public async Task GetPolicyTypes_ShouldReturnAllActiveTypes()
    {
        // Arrange
        var policyTypes = new List<PolicyType>
        {
            new PolicyType { Id = Guid.NewGuid(), TypeName = "Health", IsActive = true },
            new PolicyType { Id = Guid.NewGuid(), TypeName = "Life", IsActive = true }
        };
        var dtos = policyTypes.Select(pt => new PolicyTypeDto { Id = pt.Id, TypeName = pt.TypeName }).ToList();

        _policyTypeRepositoryMock.Setup(x => x.GetActivePolicyTypesAsync()).ReturnsAsync(policyTypes);
        _mapperMock.Setup(x => x.Map<IEnumerable<PolicyTypeDto>>(policyTypes)).Returns(dtos);

        // Act
        var result = await _policyService.GetPolicyTypesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}