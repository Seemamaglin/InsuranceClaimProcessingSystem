using InsuranceClaimSystem.Application.DTOs.Policies;
using InsuranceClaimSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaimSystem.API.Controllers;

[ApiController]
[Route("api/policies")]
public class PolicyController : ControllerBase
{
    private readonly IPolicyService _policyService;
    private readonly ILogger<PolicyController> _logger;

    public PolicyController(IPolicyService policyService, ILogger<PolicyController> logger)
    {
        _policyService = policyService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(CreatePolicy));
        var result = await _policyService.CreatePolicyAsync(request);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(CreatePolicy), result.Error.Code);
            return result.Error.Code switch
            {
                "PolicyHolderNotFound" => NotFound(result.Error),
                "PolicyTypeNotFound" => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(CreatePolicy));
        return CreatedAtAction(nameof(GetPolicyById), new { id = result.Value.Id }, result.Value);
    }

    [HttpGet]
    [Authorize(Policy = "StaffOnly")]
    public async Task<IActionResult> GetPolicies([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("API: {Action} called", nameof(GetPolicies));
        var result = await _policyService.GetPoliciesAsync(page, pageSize);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(GetPolicies), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(GetPolicies));
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetPolicyById(Guid id)
    {
        _logger.LogInformation("API: {Action} called", nameof(GetPolicyById));
        var result = await _policyService.GetPolicyByIdAsync(id);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(GetPolicyById), result.Error.Code);
            return result.Error.Code switch
            {
                "PolicyNotFound" => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(GetPolicyById));
        return Ok(result.Value);
    }

    [HttpGet("number/{policyNumber}")]
    [Authorize]
    public async Task<IActionResult> GetPolicyByNumber(string policyNumber)
    {
        _logger.LogInformation("API: {Action} called", nameof(GetPolicyByNumber));
        var result = await _policyService.GetPolicyByNumberAsync(policyNumber);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(GetPolicyByNumber), result.Error.Code);
            return result.Error.Code switch
            {
                "PolicyNotFound" => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(GetPolicyByNumber));
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePolicy(Guid id, [FromBody] UpdatePolicyRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(UpdatePolicy));
        request.PolicyId = id;
        var result = await _policyService.UpdatePolicyAsync(request);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(UpdatePolicy), result.Error.Code);
            return result.Error.Code switch
            {
                "PolicyNotFound" => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(UpdatePolicy));
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeletePolicy(Guid id)
    {
        _logger.LogInformation("API: {Action} called", nameof(DeletePolicy));
        var result = await _policyService.DeletePolicyAsync(id);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(DeletePolicy), result.Error.Code);
            return result.Error.Code switch
            {
                "PolicyNotFound" => NotFound(result.Error),
                "PolicyHasClaims" => Conflict(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(DeletePolicy));
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ApprovePolicy(Guid id)
    {
        _logger.LogInformation("API: {Action} called", nameof(ApprovePolicy));
        var result = await _policyService.ApprovePolicyAsync(id);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(ApprovePolicy), result.Error.Code);
            return result.Error.Code switch
            {
                "PolicyNotFound" => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(ApprovePolicy));
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RejectPolicy(Guid id, [FromBody] RejectPolicyRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(RejectPolicy));
        var result = await _policyService.RejectPolicyAsync(id, request.Reason);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(RejectPolicy), result.Error.Code);
            return result.Error.Code switch
            {
                "PolicyNotFound" => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(RejectPolicy));
        return Ok(result.Value);
    }

    [HttpGet("~/api/policy-types")]
    [Authorize]
    public async Task<IActionResult> GetPolicyTypes()
    {
        _logger.LogInformation("API: {Action} called", nameof(GetPolicyTypes));
        var result = await _policyService.GetPolicyTypesAsync();
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(GetPolicyTypes), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(GetPolicyTypes));
        return Ok(result.Value);
    }
}