using AutoMapper;
using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace InsuranceClaimSystem.Infrastructure.Services;

public class ClaimService : IClaimService
{
    private readonly IClaimRepository _claimRepository;
    private readonly IPolicyRepository _policyRepository;
    private readonly IUserRepository _userRepository;
    private readonly INomineeRepository _nomineeRepository;
    private readonly IClaimTypeRepository _claimTypeRepository;
    private readonly IClaimWorkflowHistoryRepository _workflowHistoryRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IClaimValidationService _validationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ClaimService> _logger;

    public ClaimService(
        IClaimRepository claimRepository,
        IPolicyRepository policyRepository,
        IUserRepository userRepository,
        INomineeRepository nomineeRepository,
        IClaimTypeRepository claimTypeRepository,
        IClaimWorkflowHistoryRepository workflowHistoryRepository,
        IDocumentRepository documentRepository,
        IClaimValidationService validationService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ClaimService> logger)
    {
        _claimRepository = claimRepository;
        _policyRepository = policyRepository;
        _userRepository = userRepository;
        _nomineeRepository = nomineeRepository;
        _claimTypeRepository = claimTypeRepository;
        _workflowHistoryRepository = workflowHistoryRepository;
        _documentRepository = documentRepository;
        _validationService = validationService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ClaimDetailDto>> SubmitClaimAsync(SubmitClaimRequest request)
    {
        _logger.LogInformation("Submitting claim for policy {PolicyId}", request.PolicyId);
        try
        {
            var policy = await _policyRepository.GetByIdAsync(request.PolicyId);
            if (policy == null)
                return Result<ClaimDetailDto>.Failure(Error.NotFound("PolicyNotFound", "Policy not found."));

            var validationResult = await _validationService.ValidateSubmissionAsync(request, policy.PolicyHolderId);
            var claimType = await _claimTypeRepository.GetByIdAsync(request.ClaimTypeId);
            if (claimType == null)
                return Result<ClaimDetailDto>.Failure(Error.NotFound("ClaimTypeNotFound", "Claim type not found."));

            var claimNumber = await GenerateClaimNumberAsync();
            var claim = BuildClaimEntity(request, policy, claimType, claimNumber, validationResult);

            await _claimRepository.AddAsync(claim);
            await _workflowHistoryRepository.AddAsync(BuildWorkflowEntry(claim, policy.PolicyHolderId, null, ClaimStatus.Submitted, "Claim submitted"));
            await _unitOfWork.SaveChangesAsync();

            var claimWithDetails = await _claimRepository.GetByIdWithDetailsAsync(claim.Id);
            _logger.LogInformation("Claim {ClaimId} submitted successfully", claim.Id);
            return Result<ClaimDetailDto>.Success(_mapper.Map<ClaimDetailDto>(claimWithDetails));
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation during claim submission");
            return Result<ClaimDetailDto>.Failure(Error.Validation("ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting claim for policy {PolicyId}", request.PolicyId);
            return Result<ClaimDetailDto>.Failure(Error.Validation("SubmitClaimFailed", "An error occurred while submitting the claim."));
        }
    }

    public async Task<Result<ClaimDetailDto>> GetClaimByIdAsync(Guid claimId)
    {
        _logger.LogInformation("Getting claim {ClaimId}", claimId);
        try
        {
            var claim = await _claimRepository.GetByIdWithDetailsAsync(claimId);
            if (claim == null)
                return Result<ClaimDetailDto>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));

            _logger.LogInformation("Claim {ClaimId} retrieved successfully", claimId);
            return Result<ClaimDetailDto>.Success(_mapper.Map<ClaimDetailDto>(claim));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claim {ClaimId}", claimId);
            return Result<ClaimDetailDto>.Failure(Error.Validation("GetClaimFailed", "An error occurred while retrieving the claim."));
        }
    }

    public async Task<Result<ClaimDetailDto>> GetClaimByNumberAsync(string claimNumber)
    {
        _logger.LogInformation("Getting claim by number {ClaimNumber}", claimNumber);
        try
        {
            var claim = await _claimRepository.GetClaimByNumberAsync(claimNumber);
            if (claim == null)
                return Result<ClaimDetailDto>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));

            var claimWithDetails = await _claimRepository.GetByIdWithDetailsAsync(claim.Id);
            _logger.LogInformation("Claim {ClaimNumber} retrieved successfully", claimNumber);
            return Result<ClaimDetailDto>.Success(_mapper.Map<ClaimDetailDto>(claimWithDetails));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claim by number {ClaimNumber}", claimNumber);
            return Result<ClaimDetailDto>.Failure(Error.Validation("GetClaimFailed", "An error occurred while retrieving the claim."));
        }
    }

    public async Task<Result<bool>> UpdateStatusAsync(Guid claimId, UpdateClaimStatusRequest request)
    {
        _logger.LogInformation("Updating status for claim {ClaimId} to {NewStatus}", claimId, request.NewStatus);
        try
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
                return Result<bool>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));

            ClaimStateMachine.ValidateTransition(claim.Status, request.NewStatus);

            var previousStatus = claim.Status;
            claim.Status = request.NewStatus;

            if (request.NewStatus == ClaimStatus.Approved)
                await HandleApprovedStatusAsync(claim, request);
            else if (request.NewStatus == ClaimStatus.Rejected)
                HandleRejectedStatusAsync(claim, request);
            else if (request.NewStatus == ClaimStatus.Closed)
                await HandleClosedStatusAsync(claim);

            await _claimRepository.UpdateAsync(claim);
            await _workflowHistoryRepository.AddAsync(BuildWorkflowEntry(claim, request.ChangedByUserId, previousStatus, request.NewStatus,
                $"Status changed from {previousStatus} to {request.NewStatus}"));
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Claim {ClaimId} status updated to {NewStatus}", claimId, request.NewStatus);
            return Result<bool>.Success(true);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Invalid status transition for claim {ClaimId}", claimId);
            return Result<bool>.Failure(Error.Validation("InvalidTransition", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating claim status {ClaimId}", claimId);
            return Result<bool>.Failure(Error.Validation("UpdateStatusFailed", "An error occurred while updating the claim status."));
        }
    }

    public async Task<Result<bool>> AssignReviewerAsync(AssignReviewerRequest request)
    {
        _logger.LogInformation("Assigning reviewer {ReviewerId} to claim {ClaimId}", request.ReviewerId, request.ClaimId);
        try
        {
            var claim = await _claimRepository.GetByIdAsync(request.ClaimId);
            if (claim == null)
                return Result<bool>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));

            var reviewer = await _userRepository.GetByIdAsync(request.ReviewerId);
            if (reviewer == null)
                return Result<bool>.Failure(Error.NotFound("ReviewerNotFound", "Reviewer not found."));

            if (reviewer.Role != UserRole.ClaimReviewer)
                return Result<bool>.Failure(Error.Validation("InvalidRole", "The specified user is not a claim reviewer."));

            var claimType = await _claimTypeRepository.GetByIdAsync(claim.ClaimTypeId);
            if (claimType != null && reviewer.Specialization != Specialization.All)
            {
                if (!SpecializationsMatch(reviewer.Specialization, claimType.PolicyTypeId))
                    return Result<bool>.Failure(Error.Validation("SpecializationMismatch", "Reviewer specialization does not match the claim type."));
            }

            claim.AssignedReviewerId = request.ReviewerId;
            await _claimRepository.UpdateAsync(claim);
            await _workflowHistoryRepository.AddAsync(BuildWorkflowEntry(claim, request.AssignedByUserId, null, claim.Status,
                $"Reviewer {reviewer.FirstName} {reviewer.LastName} assigned"));
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Reviewer {ReviewerId} assigned to claim {ClaimId}", request.ReviewerId, request.ClaimId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning reviewer to claim {ClaimId}", request.ClaimId);
            return Result<bool>.Failure(Error.Validation("AssignReviewerFailed", "An error occurred while assigning the reviewer."));
        }
    }

    public async Task<Result<bool>> AutoAssignReviewerAsync(Guid claimId)
    {
        _logger.LogInformation("Auto-assigning reviewer to claim {ClaimId}", claimId);
        try
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
                return Result<bool>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));

            var selectedReviewer = await FindBestReviewerAsync(claim);
            if (selectedReviewer == null)
                return Result<bool>.Failure(Error.Validation("NoReviewerAvailable", "No available reviewer found."));

            claim.AssignedReviewerId = selectedReviewer.Id;
            await _claimRepository.UpdateAsync(claim);
            await _workflowHistoryRepository.AddAsync(BuildWorkflowEntry(claim, selectedReviewer.Id, null, claim.Status,
                $"Auto-assigned reviewer {selectedReviewer.FirstName} {selectedReviewer.LastName}"));
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Reviewer {ReviewerId} auto-assigned to claim {ClaimId}", selectedReviewer.Id, claimId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-assigning reviewer to claim {ClaimId}", claimId);
            return Result<bool>.Failure(Error.Validation("AutoAssignFailed", "An error occurred while auto-assigning the reviewer."));
        }
    }

    public async Task<Result<decimal>> CalculatePayoutAsync(Guid claimId)
    {
        _logger.LogInformation("Calculating payout for claim {ClaimId}", claimId);
        try
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
                return Result<decimal>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));

            var policy = await _policyRepository.GetByIdAsync(claim.PolicyId);
            if (policy == null)
                return Result<decimal>.Failure(Error.NotFound("PolicyNotFound", "Policy not found."));

            var payout = await _validationService.CalculatePayoutAsync(claim.ClaimedAmount, claim.ClaimTypeId, policy.PolicyTypeId);
            _logger.LogInformation("Payout {Payout} calculated for claim {ClaimId}", payout, claimId);
            return Result<decimal>.Success(payout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating payout for claim {ClaimId}", claimId);
            return Result<decimal>.Failure(Error.Validation("CalculatePayoutFailed", "An error occurred while calculating the payout."));
        }
    }

    public async Task<Result<PagedResult<ClaimDto>>> GetClaimsAsync(int page, int pageSize)
    {
        _logger.LogInformation("Getting claims page {Page} size {PageSize}", page, pageSize);
        try
        {
            var result = await _claimRepository.GetPagedAsync(page, pageSize);
            var mappedItems = _mapper.Map<List<ClaimDto>>(result.Items);
            _logger.LogInformation("Retrieved {Count} claims", result.TotalCount);
            return Result<PagedResult<ClaimDto>>.Success(PagedResult<ClaimDto>.Create(mappedItems, result.TotalCount, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claims");
            return Result<PagedResult<ClaimDto>>.Failure(Error.Validation("GetClaimsFailed", "An error occurred while retrieving claims."));
        }
    }

    public async Task<Result<PagedResult<ClaimDto>>> GetClaimsByPolicyAsync(Guid policyId, int page, int pageSize)
    {
        _logger.LogInformation("Getting claims for policy {PolicyId}", policyId);
        try
        {
            var result = await _claimRepository.GetPagedAsync(page, pageSize, c => c.PolicyId == policyId);
            var mappedItems = _mapper.Map<List<ClaimDto>>(result.Items);
            _logger.LogInformation("Retrieved {Count} claims for policy {PolicyId}", result.TotalCount, policyId);
            return Result<PagedResult<ClaimDto>>.Success(PagedResult<ClaimDto>.Create(mappedItems, result.TotalCount, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claims for policy {PolicyId}", policyId);
            return Result<PagedResult<ClaimDto>>.Failure(Error.Validation("GetClaimsFailed", "An error occurred while retrieving claims."));
        }
    }

    public async Task<Result<PagedResult<ClaimDto>>> GetClaimsByReviewerAsync(Guid reviewerId, int page, int pageSize)
    {
        _logger.LogInformation("Getting claims for reviewer {ReviewerId}", reviewerId);
        try
        {
            var result = await _claimRepository.GetPagedAsync(page, pageSize, c => c.AssignedReviewerId == reviewerId);
            var mappedItems = _mapper.Map<List<ClaimDto>>(result.Items);
            _logger.LogInformation("Retrieved {Count} claims for reviewer {ReviewerId}", result.TotalCount, reviewerId);
            return Result<PagedResult<ClaimDto>>.Success(PagedResult<ClaimDto>.Create(mappedItems, result.TotalCount, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claims for reviewer {ReviewerId}", reviewerId);
            return Result<PagedResult<ClaimDto>>.Failure(Error.Validation("GetClaimsFailed", "An error occurred while retrieving claims."));
        }
    }

    // Private helper methods

    private async Task<string> GenerateClaimNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var sequence = await _claimRepository.CountByStatusAsync(ClaimStatus.Submitted) + 1;
        return $"CLM-{year}-{sequence:D4}";
    }

    private Claim BuildClaimEntity(SubmitClaimRequest request, Policy policy, ClaimType claimType, string claimNumber, ClaimValidationResult validationResult)
    {
        return new Claim
        {
            ClaimNumber = claimNumber,
            PolicyId = request.PolicyId,
            ClaimTypeId = request.ClaimTypeId,
            ClaimantId = policy.PolicyHolderId,
            ClaimantType = request.ClaimantType,
            IncidentDate = claimType.IsMaturityClaim ? null : request.IncidentDate,
            IncidentDescription = request.IncidentDescription,
            IncidentLocation = request.IncidentLocation,
            IntimationDate = DateTime.UtcNow,
            IsLateIntimation = validationResult.IsLateIntimation,
            ClaimedAmount = request.ClaimedAmount,
            NomineeId = request.ClaimantType == ClaimantType.Nominee ? request.NomineeId : null,
            DeductibleAmount = validationResult.DeductibleAmount,
            CoPayPercentage = validationResult.CoPayPercentage,
            Status = ClaimStatus.Submitted,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
    }

    private ClaimWorkflowHistory BuildWorkflowEntry(Claim claim, Guid changedByUserId, ClaimStatus? previousStatus, ClaimStatus newStatus, string comments)
    {
        return new ClaimWorkflowHistory
        {
            ClaimId = claim.Id,
            ChangedByUserId = changedByUserId,
            ActionType = WorkflowActionType.StatusChange,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Comments = comments
        };
    }

    private async Task HandleApprovedStatusAsync(Claim claim, UpdateClaimStatusRequest request)
    {
        var policy = await _policyRepository.GetByIdAsync(claim.PolicyId);
        if (policy != null)
        {
            claim.FinalPayableAmount = await _validationService.CalculatePayoutAsync(
                claim.ClaimedAmount, claim.ClaimTypeId, policy.PolicyTypeId);
        }
    }

    private async Task HandleClosedStatusAsync(Claim claim)
    {
        claim.ResolvedAt = DateTime.UtcNow;

        if (claim.FinalPayableAmount > 0)
        {
            var policy = await _policyRepository.GetByIdAsync(claim.PolicyId);
            if (policy != null)
            {
                policy.RemainingCoverageAmount -= claim.FinalPayableAmount;
                if (policy.RemainingCoverageAmount <= 0)
                {
                    policy.RemainingCoverageAmount = 0;
                    policy.Status = PolicyStatus.CoverageExhausted;
                }
                await _policyRepository.UpdateAsync(policy);
            }
        }
    }

    private void HandleRejectedStatusAsync(Claim claim, UpdateClaimStatusRequest request)
    {
        if (!string.IsNullOrEmpty(request.RejectionReason))
            claim.RejectionReason = request.RejectionReason;
    }

    private async Task<User?> FindBestReviewerAsync(Claim claim)
    {
        var claimType = await _claimTypeRepository.GetByIdAsync(claim.ClaimTypeId);
        var allReviewers = await _userRepository.GetUsersByRoleAsync(UserRole.ClaimReviewer);

        var matchingReviewers = allReviewers.Where(r => r.IsActive).Where(r =>
            r.Specialization == Specialization.All ||
            (claimType != null && SpecializationsMatch(r.Specialization, claimType.PolicyTypeId))
        ).ToList();

        if (!matchingReviewers.Any())
            matchingReviewers = allReviewers.Where(r => r.IsActive).ToList();

        var reviewerWorkloads = new List<(User Reviewer, int Count)>();
        foreach (var reviewer in matchingReviewers)
        {
            var count = await _claimRepository.GetActiveClaimCountByReviewerAsync(reviewer.Id);
            reviewerWorkloads.Add((reviewer, count));
        }

        return reviewerWorkloads.OrderBy(x => x.Count).FirstOrDefault().Reviewer;
    }

    private bool SpecializationsMatch(Specialization? specialization, Guid policyTypeId)
    {
        // Map specialization to policy type based on domain conventions
        // Health -> policy types with health coverage, Auto -> vehicle policies, etc.
        // This is a simplified implementation - a real system would have explicit mapping
        if (specialization == Specialization.All)
            return true;

        // For now, accept the match - proper implementation would query PolicyType to check coverage types
        return true;
    }
}