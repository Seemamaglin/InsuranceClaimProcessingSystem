using InsuranceClaimSystem.Application.DTOs.Policies;
using InsuranceClaimSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaimSystem.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PolicyController : ControllerBase
{
    private readonly IPolicyService _policyService;

    public PolicyController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyRequest request)
    {
        var result = await _policyService.CreatePolicyAsync(request);
        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "PolicyHolderNotFound" => NotFound(result.Error),
                "PolicyTypeNotFound" => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        return CreatedAtAction(nameof(GetPolicyById), new { id = result.Value.Id }, result.Value);
    }

    [HttpGet]
    [Authorize(Policy = "StaffOnly")]
    public async Task<IActionResult> GetPolicies([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _policyService.GetPoliciesAsync(page, pageSize);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetPolicyById(Guid id)
    {
        var result = await _policyService.GetPolicyByIdAsync(id);
        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "PolicyNotFound" => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        return Ok(result.Value);
    }

    [HttpGet("number/{policyNumber}")]
    [Authorize]
    public async Task<IActionResult> GetPolicyByNumber(string policyNumber)
    {
        var result = await _policyService.GetPolicyByNumberAsync(policyNumber);
        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "PolicyNotFound" => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePolicy(Guid id, [FromBody] UpdatePolicyRequest request)
    {
        request.PolicyId = id;
        var result = await _policyService.UpdatePolicyAsync(request);
        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "PolicyNotFound" => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeletePolicy(Guid id)
    {
        var result = await _policyService.DeletePolicyAsync(id);
        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "PolicyNotFound" => NotFound(result.Error),
                "PolicyHasClaims" => Conflict(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ApprovePolicy(Guid id)
    {
        var result = await _policyService.ApprovePolicyAsync(id);
        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "PolicyNotFound" => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RejectPolicy(Guid id, [FromBody] RejectPolicyRequest request)
    {
        var result = await _policyService.RejectPolicyAsync(id, request.Reason);
        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "PolicyNotFound" => NotFound(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        return Ok(result.Value);
    }

    [HttpGet("~/api/v1/policy-types")]
    [Authorize]
    public async Task<IActionResult> GetPolicyTypes()
    {
        var result = await _policyService.GetPolicyTypesAsync();
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return Ok(result.Value);
    }
}