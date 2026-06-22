using AutoMapper;
using InsuranceClaimSystem.Application.DTOs.Nominees;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Application.Common;
using Microsoft.Extensions.Logging;

namespace InsuranceClaimSystem.Infrastructure.Services;

public class NomineeService : INomineeService
{
    private readonly INomineeRepository _nomineeRepository;
    private readonly IPolicyRepository _policyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<NomineeService> _logger;

    public NomineeService(
        INomineeRepository nomineeRepository,
        IPolicyRepository policyRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<NomineeService> logger)
    {
        _nomineeRepository = nomineeRepository;
        _policyRepository = policyRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<NomineeDto>> AddNomineeAsync(Guid policyId, Guid requestUserId, NomineeRequest request)
    {
        _logger.LogInformation("Adding nominee for policy {PolicyId}", policyId);
        
        var policy = await _policyRepository.GetByIdAsync(policyId);
        if (policy == null)
            return Result<NomineeDto>.Failure(Error.NotFound("PolicyNotFound", "Policy not found."));

        // Disable any existing active nominee
        var existingNominee = await _nomineeRepository.GetActiveNomineeByPolicyIdAsync(policyId);
        if (existingNominee != null)
        {
            existingNominee.IsActive = false;
            await _nomineeRepository.UpdateAsync(existingNominee);
        }

        var nominee = new Nominee
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            PolicyHolderId = policy.PolicyHolderId,
            FullName = request.FullName,
            Relationship = request.Relationship,
            DateOfBirth = request.DateOfBirth,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            SharePercentage = request.SharePercentage,
            EncryptedAadhaar = Array.Empty<byte>(),
            AadhaarKeyReference = string.Empty,
            AadhaarMasked = string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _nomineeRepository.AddAsync(nominee);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Nominee added successfully with ID {NomineeId}", nominee.Id);
        return Result<NomineeDto>.Success(_mapper.Map<NomineeDto>(nominee));
    }

    public async Task<Result<List<NomineeDto>>> GetNomineesByPolicyAsync(Guid policyId)
    {
        var nominees = await _nomineeRepository.GetByPolicyIdAsync(policyId);
        return Result<List<NomineeDto>>.Success(_mapper.Map<List<NomineeDto>>(nominees));
    }
}
