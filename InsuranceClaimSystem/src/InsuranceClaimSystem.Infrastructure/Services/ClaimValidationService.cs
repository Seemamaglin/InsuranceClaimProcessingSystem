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
    private readonly IPolicyBenefitRuleRepository _benefitRuleRepository;
    private readonly IPolicyTypeRepository _policyTypeRepository;
    private readonly ILogger<ClaimValidationService> _logger;

    public ClaimValidationService(
        IPolicyRepository policyRepository,
        IClaimRepository claimRepository,
        INomineeRepository nomineeRepository,
        IClaimTypeRepository claimTypeRepository,
        IPolicyBenefitRuleRepository benefitRuleRepository,
        IPolicyTypeRepository policyTypeRepository,
        ILogger<ClaimValidationService> logger)
    {
        _policyRepository = policyRepository;
        _claimRepository = claimRepository;
        _nomineeRepository = nomineeRepository;
        _claimTypeRepository = claimTypeRepository;
        _benefitRuleRepository = benefitRuleRepository;
        _policyTypeRepository = policyTypeRepository;
        _logger = logger;
    }

    public async Task<ClaimValidationResult> ValidateSubmissionAsync(SubmitClaimRequest dto, Guid policyHolderId)
    {
        _logger.LogInformation("Validating claim submission for policy {PolicyId}", dto.PolicyId);
        var result = new ClaimValidationResult { IsValid = true };

        try
        {
            var policy = await _policyRepository.GetByIdAsync(dto.PolicyId);
            await ValidatePolicyAsync(policy, dto, result);

            await ValidateClaimTypeAsync(dto);

            await ValidateClaimantAsync(dto, policy!);

            await ValidateCoverageAsync(dto, policy, result);

            _logger.LogInformation("Claim validation completed successfully for policy {PolicyId}", dto.PolicyId);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Claim validation failed for policy {PolicyId}", dto.PolicyId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating claim submission for policy {PolicyId}", dto.PolicyId);
            throw new BusinessRuleException("An error occurred during claim validation.");
        }

        return result;
    }

    public async Task<decimal> CalculatePayoutAsync(decimal claimedAmount, Guid claimTypeId, Guid policyTypeId)
    {
        _logger.LogInformation("Calculating payout: amount={Amount}, claimType={ClaimTypeId}, policyType={PolicyTypeId}",
            claimedAmount, claimTypeId, policyTypeId);
        try
        {
            var benefitRule = await _benefitRuleRepository.GetActiveRuleAsync(policyTypeId, claimTypeId);

            if (benefitRule == null)
                throw new BusinessRuleException("No benefit rule found for payout calculation.");

            // If SubLimitAmount is 0, it means there is no sub-limit, so use the full claimed amount.
            var amountAfterSubLimit = benefitRule.SubLimitAmount > 0 
                ? Math.Min(claimedAmount, benefitRule.SubLimitAmount) 
                : claimedAmount;
                
            var amountAfterCoPay = amountAfterSubLimit * (1 - benefitRule.CoPayPercent / 100);
            var finalAmount = amountAfterCoPay - benefitRule.DeductibleAmount;

            var payout = Math.Max(0, finalAmount);
            _logger.LogInformation("Payout calculated: {Payout}", payout);
            return payout;
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Payout calculation failed");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating payout");
            throw new BusinessRuleException("An error occurred during payout calculation.");
        }
    }

    // Private helper methods

    private async Task ValidatePolicyAsync(Policy? policy, SubmitClaimRequest dto, ClaimValidationResult result)
    {
        if (policy == null)
            throw new BusinessRuleException("Policy not found.");

        var validPolicyStatuses = new[] { PolicyStatus.Active, PolicyStatus.GracePeriod };
        if (!validPolicyStatuses.Contains(policy.Status))
            throw new BusinessRuleException($"Policy is not active. Current status: {policy.Status}");

        if (await _claimRepository.HasOpenClaimAsync(dto.PolicyId))
            throw new BusinessRuleException("An open claim already exists for this policy.");

        var claimType = await _claimTypeRepository.GetByIdAsync(dto.ClaimTypeId);
        if (claimType != null && claimType.IsMaturityClaim && await _claimRepository.HasMaturityClaimAsync(dto.PolicyId))
            throw new BusinessRuleException("A maturity claim already exists for this policy.");

        var benefitRule = await _benefitRuleRepository.GetActiveRuleAsync(policy.PolicyTypeId, dto.ClaimTypeId);

        if (benefitRule == null)
            throw new BusinessRuleException("No benefit rule found for this claim type and policy type combination.");

        result.DeductibleAmount = benefitRule.DeductibleAmount;
        result.CoPayPercentage = benefitRule.CoPayPercent;

        await CheckWaitingPeriodAsync(dto, policy, claimType, benefitRule);
        await CheckIntimationDeadlineAsync(dto, benefitRule, result);
    }

    private async Task ValidateClaimTypeAsync(SubmitClaimRequest dto)
    {
        var claimType = await _claimTypeRepository.GetByIdAsync(dto.ClaimTypeId);
        if (claimType == null)
            throw new BusinessRuleException("Claim type not found.");
    }

    private async Task ValidateClaimantAsync(SubmitClaimRequest dto, Policy policy)
    {
        var claimType = await _claimTypeRepository.GetByIdAsync(dto.ClaimTypeId);
        if (claimType != null && claimType.TypeName.Contains("Death", StringComparison.OrdinalIgnoreCase))
        {
            if (dto.ClaimantType != ClaimantType.Nominee)
                throw new BusinessRuleException("A Death policy claim can only be filed by a registered Nominee, not the Policyholder.");
        }

        if (dto.ClaimantType == ClaimantType.Nominee)
        {
            var nominee = await _nomineeRepository.GetActiveNomineeByPolicyIdAsync(dto.PolicyId);
            if (nominee == null)
                throw new BusinessRuleException("No active nominee found for this policy.");

            if (dto.NomineeId.HasValue && nominee.Id != dto.NomineeId.Value)
                throw new BusinessRuleException("Specified nominee is not valid for this policy.");

            var policyType = await _policyTypeRepository.GetByIdAsync(policy.PolicyTypeId);

            if (policyType == null || !policyType.AllowsNomineeClaim)
                throw new BusinessRuleException("Nominee claims are not allowed for this policy type.");
        }
        else if (dto.ClaimantType == ClaimantType.ThirdParty)
        {
            var policyType = await _policyTypeRepository.GetByIdAsync(policy.PolicyTypeId);

            if (policyType == null || !policyType.AllowsThirdPartyClaim)
                throw new BusinessRuleException("Third party claims are not allowed for this policy type.");
        }
    }

    private Task ValidateCoverageAsync(SubmitClaimRequest dto, Policy policy, ClaimValidationResult result)
    {
        if (dto.ClaimedAmount > policy.RemainingCoverageAmount)
            throw new BusinessRuleException($"Claimed amount exceeds remaining coverage amount of {policy.RemainingCoverageAmount}.");

        return Task.CompletedTask;
    }

    private Task CheckWaitingPeriodAsync(SubmitClaimRequest dto, Policy policy, ClaimType? claimType, PolicyBenefitRule benefitRule)
    {
        if (dto.IncidentDate.HasValue)
        {
            var daysSincePolicyStart = (dto.IncidentDate.Value - policy.StartDate).Days;
            if (daysSincePolicyStart < benefitRule.WaitingPeriodDays)
            {
                throw new BusinessRuleException($"Incident occurred during the {benefitRule.WaitingPeriodDays}-day waiting period.");
            }
        }
        return Task.CompletedTask;
    }

    private Task CheckIntimationDeadlineAsync(SubmitClaimRequest dto, PolicyBenefitRule benefitRule, ClaimValidationResult result)
    {
        if (!dto.IncidentDate.HasValue || benefitRule.IntimationDeadlineDays <= 0)
            return Task.CompletedTask;

        var daysSinceIncident = (DateTime.UtcNow - dto.IncidentDate.Value).Days;
        if (daysSinceIncident > benefitRule.IntimationDeadlineDays)
            result.IsLateIntimation = true;

        return Task.CompletedTask;
    }
}