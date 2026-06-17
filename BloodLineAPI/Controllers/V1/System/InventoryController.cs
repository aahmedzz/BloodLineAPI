using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Inventory.Commands.DisposeBloodBags;
using BloodLineAPI.Application.Features.Inventory.Commands.IssueBloodBags;
using BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBags;
using BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBagStats;
using BloodLineAPI.Attributes;
using BloodLineAPI.Controllers.V1.System.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloodLineAPI.Controllers.V1.System;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/inventory/blood-bags")]
[ApiAudience(Audience.System)]
[Authorize(Policy = "InventoryManager")]
[Produces("application/json")]
public class InventoryController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Lists blood bags with pagination, filtering, and sorting.
    /// Defaults to showing available and expired bags if no status filter is provided.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetBloodBagsResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBloodBags(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? bloodType = null,
        [FromQuery] string? bloodTypes = null,
        [FromQuery] string? donationType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetBloodBagsQuery(page, limit, search, bloodType, bloodTypes, donationType, status, sortBy, sortOrder),
            cancellationToken);

        return Ok(ApiResponse<GetBloodBagsResult>.Ok(result, "تم استرجاع حقائب الدم بنجاح"));
    }

    /// <summary>
    /// Retrieves aggregate counts grouped by status for dashboard display cards.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<GetBloodBagStatsResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBloodBagStatsQuery(), cancellationToken);
        return Ok(ApiResponse<GetBloodBagStatsResult>.Ok(result, "تم تحميل الإحصائيات بنجاح"));
    }

    /// <summary>
    /// Issues multiple available blood bags to a patient or hospital.
    /// Processes each bag individually; returns partial success results.
    /// </summary>
    [HttpPost("issue")]
    [ProducesResponseType(typeof(ApiResponse<IssueBloodBagsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> IssueBloodBags(
        [FromBody] IssueBloodBagsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cmd = new IssueBloodBagsCommand(
                request.BagIds,
                request.RecipientName,
                request.NationalId,
                request.Phone,
                request.Reason);

            var result = await sender.Send(cmd, cancellationToken);
            return Ok(ApiResponse<IssueBloodBagsResult>.Ok(result, "تمت معالجة طلب صرف الحقائب"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Disposes of multiple blood bags due to contamination, damage, preparation issues, or expiry.
    /// Processes each bag individually; returns partial success results.
    /// </summary>
    [HttpPost("dispose")]
    [ProducesResponseType(typeof(ApiResponse<DisposeBloodBagsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DisposeBloodBags(
        [FromBody] DisposeBloodBagsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cmd = new DisposeBloodBagsCommand(
                request.BagIds,
                request.Reason,
                request.Notes);

            var result = await sender.Send(cmd, cancellationToken);
            return Ok(ApiResponse<DisposeBloodBagsResult>.Ok(result, "تمت معالجة طلب إتلاف الحقائب"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.Fail(ex.Message));
        }
    }
}
