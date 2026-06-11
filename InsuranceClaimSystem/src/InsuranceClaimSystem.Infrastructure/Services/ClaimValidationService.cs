using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Domain.Exceptions;
using InsuranceClaimSystem.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace InsuranceClaimSystem.Infrastructure.Services;

public class ClaimValidationService : IClaimValidationService
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IClaimRepository _claimRepository;
    private readonly INomineeRepository _nomineeRepository;
    private readonly IClaimTypeRepository _claimTypeRepository;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ClaimValidationService> _logger;

    public ClaimValidationService(
        IPolicyRepository policyRepository,
        IClaimRepository claimRepository,
        INomineeRepository nomineeRepository,
        IClaimTypeRepository claimTypeRepository,
        AppDbContext dbContext,
        ILogger<ClaimValidationService> logger)
    {
        _policyRepository = policyRepository;
        _claimRepository = claimRepository;
        _nomineeRepository = nomineeRepository;
        _claimTypeRepository = claimTypeRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ClaimValidationResult> ValidateSubmissionAsync(SubmitClaimRequest dto, Guid policyHolderId)
    {
        var result = new ClaimValidationResult { IsValid = true };

        try
        {
            // 1. Policy exists and active
            var policy = await _policyRepository.GetByIdAsync(dto.PolicyId);
            if (policy == null)
            {
                throw new BusinessRuleException("Policy not found.");
            }

            var validPolicyStatuses = new[] { PolicyStatus.Active, PolicyStatus.GracePeriod };
            if (!validPolicyStatuses.Contains(policy.Status))
            {
                throw new BusinessRuleException($"Policy is not active. Current status: {policy.Status}");
            }

            // 2. Only one open claim
            if (await _claimRepository.HasOpenClaimAsync(dto.PolicyId))
            {
                throw new BusinessRuleException("An open claim already exists for this policy.");
            }

            // 3. Get claim type and check maturity claim duplicate
            var claimType = await _claimTypeRepository.GetByIdAsync(dto.ClaimTypeId);
            if (claimType == null)
            {
                throw new BusinessRuleException("Claim type not found.");
            }

            if (claimType.IsMaturityClaim && await _claimRepository.HasMaturityClaimAsync(dto.PolicyId))
            {
                throw new BusinessRuleException("A maturity claim already exists for this policy.");
            }

            // 4. Get benefit rule for waiting period and coverage amount
            var benefitRule = await _dbContext.PolicyBenefitRules
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.PolicyTypeId == policy.PolicyTypeId 
                    && r.ClaimTypeId == dto.ClaimTypeId && r.IsActive);

            if (benefitRule == null)
            {
                throw new BusinessRuleException("No benefit rule found for this claim type and policy type combination.");
            }

            result.DeductibleAmount = benefitRule.DeductibleAmount;
            result.CoPayPercentage = benefitRule.CoPayPercent;

            // 4. Waiting period check
            if (!claimType.IsMaturityClaim && dto.IncidentDate.HasValue)
            {
                var daysSinceStart = (dto.IncidentDate.Value - policy.StartDate).Days;
                if (daysSinceStart < benefitRule.WaitingPeriodDays)
                {
                    throw new BusinessRuleException($"Waiting period of {benefitRule.WaitingPeriodDays} days not completed.");
                }
            }

            // 5. Coverage amount check
            if (dto.ClaimedAmount > policy.RemainingCoverageAmount)
            {
                throw new BusinessRuleException($"Claimed amount exceeds remaining coverage amount of {policy.RemainingCoverageAmount}.");
            }

            // 6. Nominee validation
            if (dto.ClaimantType == ClaimantType.Nominee)
            {
                var nominee = await _nomineeRepository.GetActiveNomineeByPolicyIdAsync(dto.PolicyId);
                if (nominee == null)
                {
                    throw new BusinessRuleException("No active nominee found for this policy.");
                }
                if (dto.NomineeId.HasValue && nominee.Id != dto.NomineeId.Value)
                {
                    throw new BusinessRuleException("Specified nominee is not valid for this policy.");
                }
            }

            // 7. Third-party validation
            if (dto.ClaimantType == ClaimantType.ThirdParty)
            {
                var policyType = await _dbContext.PolicyTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(pt => pt.Id == policy.PolicyTypeId);

                if (policyType == null || !policyType.AllowsThirdPartyClaim)
                {
                    throw new BusinessRuleException("Third party claims are not allowed for this policy type.");
                }
            }

            //nominee polic type validation
            if (dto.ClaimantType ==  ClaimantType.Nominee)
            {
                var policyType = await _dbContext.PolicyTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(pt => pt.Id == policy.PolicyTypeId);

                if (policyType == null || !policyType.AllowsNomineeClaim)
                {
                    throw new BusinessRuleException("Nominee claims are not allowed for this policy type.");
                }
            }

            // 8. Intimation deadline - mark as late but allow submission
            if (dto.IncidentDate.HasValue && benefitRule.IntimationDeadlineDays > 0)
            {
                var daysSinceIncident = (DateTime.UtcNow - dto.IncidentDate.Value).Days;
                if (daysSinceIncident > benefitRule.IntimationDeadlineDays)
                {
                    result.IsLateIntimation = true;
                }
            }
        }
        catch (BusinessRuleException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating claim submission");
            throw new BusinessRuleException("An error occurred during claim validation.");
        }

        return result;
    }

    public async Task<decimal> CalculatePayoutAsync(decimal claimedAmount, Guid claimTypeId, Guid policyTypeId)
    {
        try
        {
            var benefitRule = await _dbContext.PolicyBenefitRules
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.PolicyTypeId == policyTypeId 
                    && r.ClaimTypeId == claimTypeId && r.IsActive);

            if (benefitRule == null)
            {
                throw new BusinessRuleException("No benefit rule found for payout calculation.");
            }

            // Formula: MIN(claimedAmount, rule.SubLimitAmount) * (1 - rule.CoPayPercent / 100) - rule.DeductibleAmount
            var amountAfterSubLimit = Math.Min(claimedAmount, benefitRule.SubLimitAmount);
            var amountAfterCoPay = amountAfterSubLimit * (1 - benefitRule.CoPayPercent / 100);
            var finalAmount = amountAfterCoPay - benefitRule.DeductibleAmount;

            return Math.Max(0, finalAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating payout");
            throw new BusinessRuleException("An error occurred during payout calculation.");
        }
    }
}