using AutoMapper;
using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Domain.Exceptions;
using InsuranceClaimSystem.Infrastructure.Data;
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
    private readonly AppDbContext _dbContext;

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
        ILogger<ClaimService> logger,
        AppDbContext dbContext)
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
        _dbContext = dbContext;
    }

    public async Task<Result<ClaimDetailDto>> SubmitClaimAsync(SubmitClaimRequest request)
    {
        try
        {
            var policy = await _policyRepository.GetByIdAsync(request.PolicyId);
            if (policy == null)
            {
                return Result<ClaimDetailDto>.Failure(Error.NotFound("PolicyNotFound", "Policy not found."));
            }

            // Validate submission
            var validationResult = await _validationService.ValidateSubmissionAsync(request, policy.PolicyHolderId);

            // Get claim type
            var claimType = await _claimTypeRepository.GetByIdAsync(request.ClaimTypeId);
            if (claimType == null)
            {
                return Result<ClaimDetailDto>.Failure(Error.NotFound("ClaimTypeNotFound", "Claim type not found."));
            }

            // Generate claim number
            var year = DateTime.UtcNow.Year;
            var sequence = await _claimRepository.CountByStatusAsync(ClaimStatus.Submitted) + 1;
            var claimNumber = $"CLM-{year}-{sequence:D4}";

            // Create claim entity
            var claim = new Claim
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

            await _claimRepository.AddAsync(claim);

            // Create workflow history entry
            var workflowEntry = new ClaimWorkflowHistory
            {
                ClaimId = claim.Id,
                ChangedByUserId = policy.PolicyHolderId,
                ActionType = WorkflowActionType.StatusChange,
                PreviousStatus = null,
                NewStatus = ClaimStatus.Submitted,
                Comments = "Claim submitted"
            };
            await _workflowHistoryRepository.AddAsync(workflowEntry);

            await _unitOfWork.SaveChangesAsync();

            var claimWithDetails = await _claimRepository.GetByIdWithDetailsAsync(claim.Id);
            return Result<ClaimDetailDto>.Success(_mapper.Map<ClaimDetailDto>(claimWithDetails));
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation during claim submission");
            return Result<ClaimDetailDto>.Failure(Error.Validation("ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting claim");
            return Result<ClaimDetailDto>.Failure(Error.Validation("SubmitClaimFailed", "An error occurred while submitting the claim."));
        }
    }

    public async Task<Result<ClaimDetailDto>> GetClaimByIdAsync(Guid claimId)
    {
        try
        {
            var claim = await _claimRepository.GetByIdWithDetailsAsync(claimId);
            if (claim == null)
            {
                return Result<ClaimDetailDto>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));
            }

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
        try
        {
            var claim = await _claimRepository.GetClaimByNumberAsync(claimNumber);
            if (claim == null)
            {
                return Result<ClaimDetailDto>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));
            }

            var claimWithDetails = await _claimRepository.GetByIdWithDetailsAsync(claim.Id);
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
        try
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
            {
                return Result<bool>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));
            }

            // Validate state transition
            ClaimStateMachine.ValidateTransition(claim.Status, request.NewStatus);

            var previousStatus = claim.Status;
            claim.Status = request.NewStatus;

            if (request.NewStatus == ClaimStatus.Approved)
            {
                // Calculate final payable amount
                var policy = await _policyRepository.GetByIdAsync(claim.PolicyId);
                if (policy != null)
                {
                    claim.FinalPayableAmount = await _validationService.CalculatePayoutAsync(
                        claim.ClaimedAmount, claim.ClaimTypeId, policy.PolicyTypeId);
                }
            }

            if (request.NewStatus == ClaimStatus.Rejected && !string.IsNullOrEmpty(request.RejectionReason))
            {
                claim.RejectionReason = request.RejectionReason;
            }

            if (request.NewStatus == ClaimStatus.Closed)
            {
                claim.ResolvedAt = DateTime.UtcNow;

                // Decrement policy remaining coverage
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

            await _claimRepository.UpdateAsync(claim);

            // Create workflow history
            var workflowEntry = new ClaimWorkflowHistory
            {
                ClaimId = claim.Id,
                ChangedByUserId = request.ChangedByUserId,
                ActionType = WorkflowActionType.StatusChange,
                PreviousStatus = previousStatus,
                NewStatus = request.NewStatus,
                Comments = $"Status changed from {previousStatus} to {request.NewStatus}"
            };
            await _workflowHistoryRepository.AddAsync(workflowEntry);

            await _unitOfWork.SaveChangesAsync();
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
        try
        {
            var claim = await _claimRepository.GetByIdAsync(request.ClaimId);
            if (claim == null)
            {
                return Result<bool>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));
            }

            var reviewer = await _userRepository.GetByIdAsync(request.ReviewerId);
            if (reviewer == null)
            {
                return Result<bool>.Failure(Error.NotFound("ReviewerNotFound", "Reviewer not found."));
            }

            if (reviewer.Role != UserRole.ClaimReviewer)
            {
                return Result<bool>.Failure(Error.Validation("InvalidRole", "The specified user is not a claim reviewer."));
            }

            // Verify specialization matches
            var claimType = await _claimTypeRepository.GetByIdAsync(claim.ClaimTypeId);
            if (claimType != null && reviewer.Specialization != Specialization.All)
            {
                var policy = await _policyRepository.GetByIdAsync(claim.PolicyId);
                if (policy != null && !SpecializationsMatch(reviewer.Specialization, claimType.PolicyTypeId))
                {
                    return Result<bool>.Failure(Error.Validation("SpecializationMismatch", "Reviewer specialization does not match the claim type."));
                }
            }

            claim.AssignedReviewerId = request.ReviewerId;
            await _claimRepository.UpdateAsync(claim);

            // Create workflow history
            var workflowEntry = new ClaimWorkflowHistory
            {
                ClaimId = claim.Id,
                ChangedByUserId = request.AssignedByUserId,
                ActionType = WorkflowActionType.Assignment,
                Comments = $"Reviewer {reviewer.FirstName} {reviewer.LastName} assigned"
            };
            await _workflowHistoryRepository.AddAsync(workflowEntry);

            await _unitOfWork.SaveChangesAsync();
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
        try
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
            {
                return Result<bool>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));
            }

            var claimType = await _claimTypeRepository.GetByIdAsync(claim.ClaimTypeId);
            
            // Find reviewers with matching specialization first
            var matchingReviewers = new List<User>();
            var allReviewers = await _userRepository.GetUsersByRoleAsync(UserRole.ClaimReviewer);

            foreach (var reviewer in allReviewers.Where(r => r.IsActive))
            {
                if (reviewer.Specialization == Specialization.All)
                {
                    matchingReviewers.Add(reviewer);
                }
                else if (claimType != null && SpecializationsMatch(reviewer.Specialization, claimType.PolicyTypeId))
                {
                    matchingReviewers.Add(reviewer);
                }
            }

            if (!matchingReviewers.Any())
            {
                // Fall back to any available reviewer
                matchingReviewers = allReviewers.Where(r => r.IsActive).ToList();
            }

            // Sort by active claim count (workload balancing)
            var reviewerWorkloads = new List<(User Reviewer, int Count)>();
            foreach (var reviewer in matchingReviewers)
            {
                var count = await _claimRepository.GetActiveClaimCountByReviewerAsync(reviewer.Id);
                reviewerWorkloads.Add((reviewer, count));
            }

            var selectedReviewer = reviewerWorkloads.OrderBy(x => x.Count).First().Reviewer;

            claim.AssignedReviewerId = selectedReviewer.Id;
            await _claimRepository.UpdateAsync(claim);

            var workflowEntry = new ClaimWorkflowHistory
            {
                ClaimId = claim.Id,
                ChangedByUserId = selectedReviewer.Id,
                ActionType = WorkflowActionType.Assignment,
                Comments = $"Auto-assigned reviewer {selectedReviewer.FirstName} {selectedReviewer.LastName}"
            };
            await _workflowHistoryRepository.AddAsync(workflowEntry);

            await _unitOfWork.SaveChangesAsync();
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
        try
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
            {
                return Result<decimal>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));
            }

            var policy = await _policyRepository.GetByIdAsync(claim.PolicyId);
            if (policy == null)
            {
                return Result<decimal>.Failure(Error.NotFound("PolicyNotFound", "Policy not found."));
            }

            var payout = await _validationService.CalculatePayoutAsync(
                claim.ClaimedAmount, claim.ClaimTypeId, policy.PolicyTypeId);

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
        try
        {
            var result = await _claimRepository.GetPagedAsync(page, pageSize);
            var mappedItems = _mapper.Map<List<ClaimDto>>(result.Items);
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
        try
        {
            var result = await _claimRepository.GetPagedAsync(page, pageSize, c => c.PolicyId == policyId);
            var mappedItems = _mapper.Map<List<ClaimDto>>(result.Items);
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
        try
        {
            var result = await _claimRepository.GetPagedAsync(page, pageSize, c => c.AssignedReviewerId == reviewerId);
            var mappedItems = _mapper.Map<List<ClaimDto>>(result.Items);
            return Result<PagedResult<ClaimDto>>.Success(PagedResult<ClaimDto>.Create(mappedItems, result.TotalCount, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting claims for reviewer {ReviewerId}", reviewerId);
            return Result<PagedResult<ClaimDto>>.Failure(Error.Validation("GetClaimsFailed", "An error occurred while retrieving claims."));
        }
    }

    private bool SpecializationsMatch(Specialization? specialization, Guid policyTypeId)
    {
        // Map policy type to specialization based on naming conventions or additional data
        // For now, simplified matching logic
        return true; // Accept all for now, would need policy type to have specialization info
    }
}