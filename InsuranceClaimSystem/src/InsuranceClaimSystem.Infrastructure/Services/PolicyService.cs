using AutoMapper;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Policies;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace InsuranceClaimSystem.Infrastructure.Services;

public class PolicyService : IPolicyService
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IPolicyTypeRepository _policyTypeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PolicyService> _logger;

    public PolicyService(
        IPolicyRepository policyRepository,
        IPolicyTypeRepository policyTypeRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PolicyService> logger)
    {
        _policyRepository = policyRepository;
        _policyTypeRepository = policyTypeRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<PolicyResponse>> CreatePolicyAsync(CreatePolicyRequest request)
    {
        try
        {
            var policyHolder = await _userRepository.GetByIdAsync(request.PolicyHolderId);
            if (policyHolder == null)
            {
                return Result<PolicyResponse>.Failure(
                    Error.NotFound("PolicyHolderNotFound", "Policy holder not found."));
            }

            var policyType = await _policyTypeRepository.GetByIdAsync(request.PolicyTypeId);
            if (policyType == null)
            {
                return Result<PolicyResponse>.Failure(
                    Error.NotFound("PolicyTypeNotFound", "Policy type not found."));
            }

            var policyCount = await _policyRepository.CountByStatusAsync(PolicyStatus.PendingApproval);
            var sequenceNumber = (policyCount + 1).ToString("D4");
            var policyNumber = $"POL-{DateTime.UtcNow.Year}-{sequenceNumber}";

            var gracePeriodDays = request.PremiumFrequency switch
            {
                PremiumFrequency.Monthly => 15,
                _ => 30
            };

            var policy = new Policy
            {
                PolicyNumber = policyNumber,
                PolicyHolderId = request.PolicyHolderId,
                PolicyTypeId = request.PolicyTypeId,
                Status = PolicyStatus.PendingApproval,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CoverageAmount = request.CoverageAmount,
                RemainingCoverageAmount = request.CoverageAmount,
                PremiumAmount = request.PremiumAmount,
                PremiumFrequency = request.PremiumFrequency,
                GracePeriodDays = gracePeriodDays,
                RowVersion = Guid.NewGuid().ToByteArray()
            };

            await _policyRepository.AddAsync(policy);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Policy created successfully: {PolicyNumber}", policyNumber);

            var createdPolicy = await _policyRepository.GetByIdAsync(policy.Id);
            return Result<PolicyResponse>.Success(_mapper.Map<PolicyResponse>(createdPolicy));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating policy");
            return Result<PolicyResponse>.Failure(
                Error.Validation("CreatePolicyFailed", "An error occurred while creating the policy."));
        }
    }

    public async Task<Result<PolicyResponse>> GetPolicyByIdAsync(Guid policyId)
    {
        try
        {
            var policy = await _policyRepository.GetByIdAsync(policyId);
            if (policy == null)
            {
                return Result<PolicyResponse>.Failure(
                    Error.NotFound("PolicyNotFound", "Policy not found."));
            }

            return Result<PolicyResponse>.Success(_mapper.Map<PolicyResponse>(policy));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving policy {PolicyId}", policyId);
            return Result<PolicyResponse>.Failure(
                Error.Validation("GetPolicyFailed", "An error occurred while retrieving the policy."));
        }
    }

    public async Task<Result<PolicyResponse>> GetPolicyByNumberAsync(string policyNumber)
    {
        try
        {
            var policy = await _policyRepository.GetByPolicyNumberAsync(policyNumber);
            if (policy == null)
            {
                return Result<PolicyResponse>.Failure(
                    Error.NotFound("PolicyNotFound", "Policy not found."));
            }

            return Result<PolicyResponse>.Success(_mapper.Map<PolicyResponse>(policy));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving policy by number {PolicyNumber}", policyNumber);
            return Result<PolicyResponse>.Failure(
                Error.Validation("GetPolicyFailed", "An error occurred while retrieving the policy."));
        }
    }

    public async Task<Result<PolicyResponse>> UpdatePolicyAsync(UpdatePolicyRequest request)
    {
        try
        {
            var policy = await _policyRepository.GetByIdAsync(request.PolicyId);
            if (policy == null)
            {
                return Result<PolicyResponse>.Failure(
                    Error.NotFound("PolicyNotFound", "Policy not found."));
            }

            if (request.CoverageAmount.HasValue)
                policy.CoverageAmount = request.CoverageAmount.Value;

            if (request.PremiumAmount.HasValue)
                policy.PremiumAmount = request.PremiumAmount.Value;

            if (request.EndDate.HasValue)
                policy.EndDate = request.EndDate.Value;

            if (request.PremiumFrequency.HasValue)
                policy.PremiumFrequency = request.PremiumFrequency.Value;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Policy updated successfully: {PolicyId}", policy.Id);

            return Result<PolicyResponse>.Success(_mapper.Map<PolicyResponse>(policy));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating policy {PolicyId}", request.PolicyId);
            return Result<PolicyResponse>.Failure(
                Error.Validation("UpdatePolicyFailed", "An error occurred while updating the policy."));
        }
    }

    public async Task<Result<bool>> DeletePolicyAsync(Guid policyId)
    {
        try
        {
            var policy = await _policyRepository.GetByIdAsync(policyId);
            if (policy == null)
            {
                return Result<bool>.Failure(
                    Error.NotFound("PolicyNotFound", "Policy not found."));
            }

            if (policy.Claims != null && policy.Claims.Any(c => !c.IsDeleted))
            {
                return Result<bool>.Failure(
                    Error.Conflict("PolicyHasClaims", "Cannot delete policy with existing claims."));
            }

            await _policyRepository.DeleteAsync(policyId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Policy deleted successfully: {PolicyId}", policyId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting policy {PolicyId}", policyId);
            return Result<bool>.Failure(
                Error.Validation("DeletePolicyFailed", "An error occurred while deleting the policy."));
        }
    }

    public async Task<Result<PolicyResponse>> ApprovePolicyAsync(Guid policyId)
    {
        try
        {
            var policy = await _policyRepository.GetByIdAsync(policyId);
            if (policy == null)
            {
                return Result<PolicyResponse>.Failure(
                    Error.NotFound("PolicyNotFound", "Policy not found."));
            }

            policy.Status = PolicyStatus.Active;

            policy.NextPremiumDueDate = CalculateNextPremiumDueDate(policy.StartDate, policy.PremiumFrequency);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Policy approved: {PolicyId}", policyId);

            return Result<PolicyResponse>.Success(_mapper.Map<PolicyResponse>(policy));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving policy {PolicyId}", policyId);
            return Result<PolicyResponse>.Failure(
                Error.Validation("ApprovePolicyFailed", "An error occurred while approving the policy."));
        }
    }

    public async Task<Result<PolicyResponse>> RejectPolicyAsync(Guid policyId, string? reason)
    {
        try
        {
            var policy = await _policyRepository.GetByIdAsync(policyId);
            if (policy == null)
            {
                return Result<PolicyResponse>.Failure(
                    Error.NotFound("PolicyNotFound", "Policy not found."));
            }

            policy.Status = PolicyStatus.Rejected;
            policy.RejectionReason = reason;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Policy rejected: {PolicyId}", policyId);

            return Result<PolicyResponse>.Success(_mapper.Map<PolicyResponse>(policy));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting policy {PolicyId}", policyId);
            return Result<PolicyResponse>.Failure(
                Error.Validation("RejectPolicyFailed", "An error occurred while rejecting the policy."));
        }
    }

    public async Task<Result<PagedResult<PolicyResponse>>> GetPoliciesAsync(int page, int pageSize)
    {
        try
        {
            var pagedPolicies = await _policyRepository.GetPagedAsync(page, pageSize);
            var policyResponses = pagedPolicies.Items.Select(p => _mapper.Map<PolicyResponse>(p)).ToList();

            var result = PagedResult<PolicyResponse>.Create(policyResponses, pagedPolicies.TotalCount, page, pageSize);
            return Result<PagedResult<PolicyResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged policies");
            return Result<PagedResult<PolicyResponse>>.Failure(
                Error.Validation("GetPoliciesFailed", "An error occurred while retrieving policies."));
        }
    }

    public async Task<Result<PagedResult<PolicyResponse>>> GetPoliciesByHolderAsync(Guid policyHolderId, int page, int pageSize)
    {
        try
        {
            var policies = await _policyRepository.GetByPolicyHolderIdAsync(policyHolderId);
            var policyList = policies.ToList();
            var totalCount = policyList.Count;
            var pagedItems = policyList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => _mapper.Map<PolicyResponse>(p))
                .ToList();

            var result = PagedResult<PolicyResponse>.Create(pagedItems, totalCount, page, pageSize);
            return Result<PagedResult<PolicyResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving policies for holder {PolicyHolderId}", policyHolderId);
            return Result<PagedResult<PolicyResponse>>.Failure(
                Error.Validation("GetPoliciesFailed", "An error occurred while retrieving policies."));
        }
    }

    public async Task<Result<IEnumerable<PolicyTypeDto>>> GetPolicyTypesAsync()
    {
        try
        {
            var policyTypes = await _policyTypeRepository.GetActivePolicyTypesAsync();
            var dtos = policyTypes.Select(pt => _mapper.Map<PolicyTypeDto>(pt));

            return Result<IEnumerable<PolicyTypeDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving policy types");
            return Result<IEnumerable<PolicyTypeDto>>.Failure(
                Error.Validation("GetPolicyTypesFailed", "An error occurred while retrieving policy types."));
        }
    }

    private static DateTime CalculateNextPremiumDueDate(DateTime startDate, PremiumFrequency frequency)
    {
        return frequency switch
        {
            PremiumFrequency.Monthly => startDate.AddMonths(1),
            PremiumFrequency.Quarterly => startDate.AddMonths(3),
            PremiumFrequency.HalfYearly => startDate.AddMonths(6),
            PremiumFrequency.Annually => startDate.AddYears(1),
            _ => startDate.AddMonths(1)
        };
    }
}