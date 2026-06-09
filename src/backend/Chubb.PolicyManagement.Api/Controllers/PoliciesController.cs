using Chubb.PolicyManagement.Application.DTOs;
using Chubb.PolicyManagement.Application.Interfaces;
using Chubb.PolicyManagement.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.PolicyManagement.Api.Controllers;

/// <summary>
/// Manages insurance policies for the Chubb APAC platform.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class PoliciesController(IPolicyService policyService) : ControllerBase
{
    /// <summary>
    /// Returns a paginated, filtered, and sorted list of policies. 
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PolicyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPolicies(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string sort = "createdAt,desc",
        [FromQuery] string? status = null,
        [FromQuery] string? lineOfBusiness = null,
        [FromQuery] string? region = null,
        [FromQuery] DateOnly? effectiveDateFrom = null,
        [FromQuery] DateOnly? effectiveDateTo = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) return BadRequest(new ProblemDetails { Title = "Invalid page", Detail = "Page must be >= 1.", Status = 400 });
        if (size < 1 || size > 100) return BadRequest(new ProblemDetails { Title = "Invalid size", Detail = "Size must be between 1 and 100.", Status = 400 });

        var query = new PolicyFilterQuery
        {
            Page = page,
            Size = size,
            Sort = sort,
            Status = status,
            LineOfBusiness = lineOfBusiness,
            Region = region,
            EffectiveDateFrom = effectiveDateFrom,
            EffectiveDateTo = effectiveDateTo,
            Search = search
        };

        var result = await policyService.GetPoliciesAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single policy by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPolicyById(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await policyService.GetPolicyByIdAsync(id, cancellationToken);
        return Ok(policy);
    }

    /// <summary>
    /// Flags multiple policies for review in bulk.
    /// </summary>
    [HttpPatch("flag")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkFlagPolicies(
        [FromBody] BulkFlagRequest request,
        CancellationToken cancellationToken = default)
    {
        await policyService.BulkFlagPoliciesAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Returns aggregated statistics across all policies.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(PolicySummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var summary = await policyService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }
}
