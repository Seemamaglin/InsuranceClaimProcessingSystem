using System.Linq.Expressions;
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
    private readonly INotificationService _notificationService;
    private readonly INomineeRepository _nomineeRepository;

    public PolicyService(
        IPolicyRepository policyRepository,
        IPolicyTypeRepository policyTypeRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PolicyService> logger,
        INotificationService notificationService,
        INomineeRepository nomineeRepository)
    {
        _policyRepository = policyRepository;
        _policyTypeRepository = policyTypeRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _notificationService = notificationService;
        _nomineeRepository = nomineeRepository;
    }

    public async Task<Result<PolicyTypeDto>> CreatePolicyTypeAsync(CreatePolicyTypeRequest request)
    {
        try
        {
            var policyType = new PolicyType
            {
                TypeName = request.TypeName,
                Description = request.Description,
                DefaultBenefitType = request.DefaultBenefitType,
                AllowsNomineeClaim = request.AllowsNomineeClaim,
                AllowsThirdPartyClaim = request.AllowsThirdPartyClaim,
                DefaultCoverageAmount = request.DefaultCoverageAmount,
                IsActive = true
            };

            await _policyTypeRepository.AddAsync(policyType);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("PolicyType created: {TypeName}", policyType.TypeName);

            return Result<PolicyTypeDto>.Success(_mapper.Map<PolicyTypeDto>(policyType));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating policy type");
            return Result<PolicyTypeDto>.Failure(Error.Validation("CreatePolicyTypeFailed", "An error occurred while creating the policy type."));
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

    public async Task<Result<PolicyResponse>> ApplyForPolicyAsync(Guid policyHolderId, ApplyForPolicyRequest request)
    {
        try
        {
            var policyHolder = await _userRepository.GetByIdAsync(policyHolderId);
            if (policyHolder == null)
                return Result<PolicyResponse>.Failure(Error.NotFound("PolicyHolderNotFound", "Policy holder not found."));

            if (policyHolder.RegistrationStatus != RegistrationStatus.Approved)
            {
                return Result<PolicyResponse>.Failure(Error.Unauthorized("AccountNotApproved", "Your account must be fully approved by an administrator before you can apply for a policy."));
            }

            var policyType = await _policyTypeRepository.GetByIdAsync(request.PolicyTypeId);
            if (policyType == null)
                return Result<PolicyResponse>.Failure(Error.NotFound("PolicyTypeNotFound", "Policy type not found."));

            if (request.CoverageAmount > policyType.DefaultCoverageAmount)
            {
                return Result<PolicyResponse>.Failure(Error.Validation("CoverageLimitExceeded", $"The requested coverage amount exceeds the limit of {policyType.DefaultCoverageAmount} for this policy type."));
            }

            if (policyType.AllowsNomineeClaim)
            {
                if (request.Nominees == null || !request.Nominees.Any())
                {
                    return Result<PolicyResponse>.Failure(Error.Validation("NomineeRequired", "A nominee is required for this policy type."));
                }
                if (request.Nominees.Count > 10)
                {
                    return Result<PolicyResponse>.Failure(Error.Validation("TooManyNominees", "You can specify a maximum of 10 nominees."));
                }
                var totalShare = request.Nominees.Sum(n => n.SharePercentage);
                if (totalShare != 100)
                {
                    return Result<PolicyResponse>.Failure(Error.Validation("InvalidSharePercentage", "The total share percentage of all nominees must equal exactly 100."));
                }
            }
            else
            {
                if (request.Nominees != null && request.Nominees.Any())
                {
                    return Result<PolicyResponse>.Failure(Error.Validation("NomineesNotAllowed", "Nominees cannot be added to this policy type."));
                }
            }

            var existingPolicies = await _policyRepository.GetByPolicyHolderIdAsync(policyHolderId);
            var duplicatePolicy = existingPolicies.FirstOrDefault(p => p.PolicyTypeId == request.PolicyTypeId && 
                (p.Status == PolicyStatus.PendingApproval || p.Status == PolicyStatus.Active));
            
            if (duplicatePolicy != null)
            {
                if (duplicatePolicy.Status == PolicyStatus.PendingApproval)
                {
                    return Result<PolicyResponse>.Failure(Error.Validation("DuplicatePolicyType", "Your previous policy of the same policy type is not approved yet."));
                }
                else
                {
                    return Result<PolicyResponse>.Failure(Error.Validation("DuplicatePolicyType", "Your previous policy of the same policy type is already active."));
                }
            }

            // Fixed: Count ALL policies created this year, regardless of their status.
            var policiesThisYear = await _policyRepository.GetPagedAsync(1, 1, p => p.CreatedAt.Year == DateTime.UtcNow.Year);
            var sequence = policiesThisYear.TotalCount + 1;
            
            // Add a random 4-character suffix to completely guarantee uniqueness even if deleted
            var randomSuffix = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
            var policyNumber = $"POL-{DateTime.UtcNow.Year}-{sequence:D4}-{randomSuffix}";

            var gracePeriodDays = request.PremiumFrequency == PremiumFrequency.Monthly ? 15 : 30;

            var nextPremiumDate = request.PremiumFrequency switch
            {
                PremiumFrequency.Monthly => request.StartDate.AddMonths(1),
                PremiumFrequency.Quarterly => request.StartDate.AddMonths(3),
                PremiumFrequency.HalfYearly => request.StartDate.AddMonths(6),
                PremiumFrequency.Annually => request.StartDate.AddYears(1),
                _ => request.StartDate.AddMonths(1)
            };

            int paymentsPerYear = request.PremiumFrequency switch
            {
                PremiumFrequency.Monthly => 12,
                PremiumFrequency.Quarterly => 4,
                PremiumFrequency.HalfYearly => 2,
                PremiumFrequency.Annually => 1,
                _ => 1
            };
            decimal yearlyPremium = request.PremiumAmount * paymentsPerYear;
            double estimatedYears = yearlyPremium > 0 ? (double)(request.CoverageAmount / yearlyPremium) : 0;
            var calculatedEndDate = request.StartDate.AddDays(estimatedYears * 365.25);

            var policy = new Policy
            {
                PolicyNumber = policyNumber,
                PolicyHolderId = policyHolderId,
                PolicyTypeId = request.PolicyTypeId,
                Status = PolicyStatus.PendingApproval,
                StartDate = request.StartDate,
                EndDate = calculatedEndDate,
                CoverageAmount = request.CoverageAmount,
                RemainingCoverageAmount = request.CoverageAmount,
                PremiumAmount = request.PremiumAmount,
                PremiumFrequency = request.PremiumFrequency,
                GracePeriodDays = gracePeriodDays,
                LastPremiumPaidDate = DateTime.UtcNow,
                NextPremiumDueDate = nextPremiumDate
            };

            await _policyRepository.AddAsync(policy);
            await _unitOfWork.SaveChangesAsync();

            if (request.Nominees != null && request.Nominees.Any())
            {
                foreach (var nr in request.Nominees)
                {
                    var nominee = new Nominee
                    {
                        PolicyId = policy.Id,
                        FullName = nr.FullName,
                        Relationship = nr.Relationship,
                        DateOfBirth = nr.DateOfBirth,
                        ContactPhone = nr.ContactPhone,
                        ContactEmail = nr.ContactEmail,
                        SharePercentage = nr.SharePercentage,
                        IsActive = true
                    };
                    await _nomineeRepository.AddAsync(nominee);
                }
                await _unitOfWork.SaveChangesAsync();
            }
            
            _logger.LogInformation("Policy application submitted: {PolicyNumber} by holder {PolicyHolderId}", policyNumber, policyHolderId);

            var createdPolicy = await _policyRepository.GetByIdAsync(policy.Id);
            return Result<PolicyResponse>.Success(_mapper.Map<PolicyResponse>(createdPolicy));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying for policy by holder {PolicyHolderId}", policyHolderId);
            return Result<PolicyResponse>.Failure(Error.Validation("ApplyForPolicyFailed", "An error occurred while submitting the policy application."));
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

            if (policy.Status != PolicyStatus.PendingApproval)
            {
                return Result<PolicyResponse>.Failure(
                    Error.Validation("InvalidPolicyStatus", "Policy can only be approved when it is pending approval."));
            }

            policy.Status = PolicyStatus.Active;

            policy.NextPremiumDueDate = CalculateNextPremiumDueDate(policy.StartDate, policy.PremiumFrequency);

            await _unitOfWork.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                policy.PolicyHolderId,
                "Policy Approved",
                $"Your policy {policy.PolicyNumber} has been approved and is now active.",
                NotificationType.StatusChanged,
                NotificationChannel.InApp);

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

            if (policy.Status != PolicyStatus.PendingApproval)
            {
                return Result<PolicyResponse>.Failure(
                    Error.Validation("InvalidPolicyStatus", "Policy can only be rejected when it is pending approval."));
            }

            policy.Status = PolicyStatus.Rejected;
            policy.RejectionReason = reason;

            await _unitOfWork.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                policy.PolicyHolderId,
                "Policy Rejected",
                $"Your policy application {policy.PolicyNumber} has been rejected. Reason: {reason ?? "None provided."}",
                NotificationType.StatusChanged,
                NotificationChannel.InApp);

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

    public async Task<Result<PagedResult<PolicyResponse>>> GetPoliciesAsync(int page, int pageSize, PolicyStatus? status = null)
    {
        _logger.LogInformation("Getting policies page {Page} size {PageSize} status {Status}", page, pageSize, status);
        try
        {
            var predicate = status.HasValue
                ? (Expression<Func<Policy, bool>>)(x => x.Status == status.Value)
                : x => true;

            var pagedPolicies = await _policyRepository.GetPagedAsync(page, pageSize, predicate);
            var policyResponses = pagedPolicies.Items.Select(p => _mapper.Map<PolicyResponse>(p)).ToList();

            var result = PagedResult<PolicyResponse>.Create(policyResponses, pagedPolicies.TotalCount, page, pageSize);
            _logger.LogInformation("Retrieved {Count} policies", pagedPolicies.TotalCount);
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