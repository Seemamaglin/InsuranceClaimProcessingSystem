using InsuranceClaimSystem.Application.DTOs.Nominees;
using InsuranceClaimSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InsuranceClaimSystem.API.Controllers;

[ApiController]
[Route("api/policies/{policyId:guid}/nominees")]
[Authorize]
public class NomineesController : ControllerBase
{
    private readonly INomineeService _nomineeService;
    private readonly ILogger<NomineesController> _logger;

    public NomineesController(INomineeService nomineeService, ILogger<NomineesController> logger)
    {
        _nomineeService = nomineeService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "PolicyHolderOnly")]
    public async Task<IActionResult> AddNominee(Guid policyId, [FromBody] NomineeRequest request)
    {
        _logger.LogInformation("API: AddNominee called for policy {PolicyId}", policyId);
        
        var requestUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(requestUserIdStr, out var requestUserId))
            return Unauthorized();

        var result = await _nomineeService.AddNomineeAsync(policyId, requestUserId, request);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpGet]
    [Authorize(Policy = "PolicyHolderOnly")]
    public async Task<IActionResult> GetNominees(Guid policyId)
    {
        var result = await _nomineeService.GetNomineesByPolicyAsync(policyId);
        if (result.IsFailure)
            return BadRequest(result.Error);
            
        return Ok(result.Value);
    }
}
