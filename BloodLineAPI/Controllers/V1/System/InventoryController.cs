using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Inventory.Commands.DisposeBloodBags;
using BloodLineAPI.Application.Features.Inventory.Commands.IssueBloodBags;
using BloodLineAPI.Application.Features.Inventory.Commands.UpdateInventoryThresholds;
using BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBags;
using BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBagStats;
using BloodLineAPI.Application.Features.Inventory.Queries.GetOutflowHistory;
using BloodLineAPI.Application.Features.Inventory.Queries.GetOutflowDetail;
using BloodLineAPI.Application.Features.Inventory.Queries.ExportOutflowPdf;
using BloodLineAPI.Application.Features.Inventory.Queries.GetInventoryAnalytics;
using BloodLineAPI.Application.Features.Inventory.Queries.GetInventoryThresholds;
using BloodLineAPI.Application.Features.Inventory.Queries.GetInventoryDashboard;
using BloodLineAPI.Attributes;
using BloodLineAPI.Controllers.V1.System.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Controllers.V1.System;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/inventory")]
[ApiAudience(Audience.System)]
[Authorize(Policy = "InventoryManager")]
[Produces("application/json")]
public class InventoryController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Lists blood bags with pagination, filtering, and sorting.
    /// Defaults to showing available and expired bags if no status filter is provided.
    /// </summary>
    [HttpGet("blood-bags")]
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
    [HttpGet("blood-bags/stats")]
    [ProducesResponseType(typeof(ApiResponse<GetBloodBagStatsResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBloodBagStatsQuery(), cancellationToken);
        return Ok(ApiResponse<GetBloodBagStatsResult>.Ok(result, "تم تحميل الإحصائيات بنجاح"));
    }

    /// <summary>
    /// Fetches all computed statistics, alerts, stock levels, indicators, and recent activities for the Inventory Dashboard.
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<GetInventoryDashboardResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInventoryDashboardQuery(), cancellationToken);
        return Ok(ApiResponse<GetInventoryDashboardResult>.Ok(result, "تم تحميل بيانات لوحة المخزون بنجاح"));
    }

    /// <summary>
    /// Fetches all computed statistics, chart data, alerts, and table records for the Inventory Analytics page.
    /// </summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(ApiResponse<GetInventoryAnalyticsResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalytics(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInventoryAnalyticsQuery(), cancellationToken);
        return Ok(ApiResponse<GetInventoryAnalyticsResult>.Ok(result, "تم تحميل تحليلات المخزون بنجاح"));
    }

    /// <summary>
    /// Fetch the safety minimum stock levels required for each blood type.
    /// </summary>
    [HttpGet("thresholds")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, int>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThresholds(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInventoryThresholdsQuery(), cancellationToken);
        return Ok(ApiResponse<Dictionary<string, int>>.Ok(result, "تم استرجاع الحد الأدنى للمخزون بنجاح"));
    }

    /// <summary>
    /// Update the safety minimum stock levels required for each blood type.
    /// </summary>
    [HttpPut("thresholds")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateThresholds(
        [FromBody] UpdateInventoryThresholdsRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new UpdateInventoryThresholdsCommand(request.Thresholds);
        var result = await sender.Send(cmd, cancellationToken);
        return Ok(ApiResponse<Dictionary<string, int>>.Ok(result, "تم تحديث الحد الأدنى للمخزون بنجاح"));
    }

    /// <summary>
    /// Issues multiple available blood bags to a patient or hospital.
    /// Processes each bag individually; returns partial success results.
    /// </summary>
    [HttpPost("blood-bags/issue")]
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
    [HttpPost("blood-bags/dispose")]
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

    /// <summary>
    /// Retrieves a paginated list of inventory outflow history (issued and disposed actions).
    /// </summary>
    [HttpGet("outflow")]
    [ProducesResponseType(typeof(ApiResponse<GetOutflowHistoryResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutflowHistory(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? actionType = null,
        [FromQuery] string? bloodType = null,
        [FromQuery] string? performedById = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetOutflowHistoryQuery(page, limit, search, actionType, bloodType, performedById);
        var result = await sender.Send(query, cancellationToken);
        return Ok(ApiResponse<GetOutflowHistoryResult>.Ok(result, "تم استرجاع سجل الحركة بنجاح"));
    }

    /// <summary>
    /// Retrieves full details of a single outflow record (either issued or disposed).
    /// </summary>
    [HttpGet("outflow/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GetOutflowDetailResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOutflowDetail(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetOutflowDetailQuery(id), cancellationToken);

        if (result == null)
        {
            return NotFound(ApiResponse.Fail("السجل المطلوب غير موجود"));
        }

        return Ok(ApiResponse<GetOutflowDetailResult>.Ok(result, "تم استرجاع تفاصيل الحركة بنجاح"));
    }

    /// <summary>
    /// Exports the filtered outflow history as a formatted PDF report.
    /// </summary>
    [HttpGet("outflow/export")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportReport(
        [FromQuery] string? search = null,
        [FromQuery] string? actionType = null,
        [FromQuery] string? bloodType = null,
        [FromQuery] string? performedById = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ExportOutflowPdfQuery(search, actionType, bloodType, performedById);
        var pdfBytes = await sender.Send(query, cancellationToken);

        var filename = $"outflow_report_{DateTime.UtcNow:yyyy-MM-dd}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }
}
