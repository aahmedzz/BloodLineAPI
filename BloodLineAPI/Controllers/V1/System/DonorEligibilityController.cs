using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using BloodLineAPI.Application.Features.DonorEligibility.Queries.GetEligibleDonors;
using BloodLineAPI.Application.Features.DonorEligibility.Queries.GetEligibilityStats;
using BloodLineAPI.Application.Features.DonorEligibility.Commands.SendEmergencyNotifications;
using BloodLineAPI.Application.Features.DonorEligibility.Queries.GetEmergencyNotificationPreview;
using BloodLineAPI.Application.Features.DonorEligibility.Queries.ExportFailedDonorsPdf;
using BloodLineAPI.Attributes;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Controllers.V1.System;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/donors/eligibility")]
[ApiAudience(Audience.System)]
[Produces("application/json")]
public class DonorEligibilityController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Retrieve a paginated, searchable, and filtered list of donors with eligibility metrics (Doctor role).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedEligibilityResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEligibleDonors(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? bloodType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? district = null,
        [FromQuery] string? gender = null,
        [FromQuery] bool? hasMobileApp = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEligibleDonorsQuery(page, limit, search, bloodType, status, district, gender, hasMobileApp);
        var result = await sender.Send(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Failed to fetch eligible donors."));
        }

        return Ok(ApiResponse<PaginatedEligibilityResult>.Ok(result.Data!, "Success"));
    }

    /// <summary>
    /// Fetch statistical aggregates for eligibility status and blood type breakdowns (Doctor role).
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<EligibilityStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEligibilityStats(CancellationToken cancellationToken = default)
    {
        var query = new GetEligibilityStatsQuery();
        var result = await sender.Send(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Failed to fetch eligibility statistics."));
        }

        return Ok(ApiResponse<EligibilityStatsDto>.Ok(result.Data!, "Success"));
    }

    /// <summary>
    /// Bulk send emergency notifications to eligible or soon-to-be-eligible donors (Doctor role).
    /// </summary>
    [HttpPost("notifications")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<SendBulkNotificationResultDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendEmergencyNotifications(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new SendEmergencyNotificationsCommand(
            request.SelectionMode,
            request.DonorIds,
            request.Filters,
            request.ExcludedDonorIds);
        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Failed to process notifications."));
        }

        // Return 202 Accepted per requirements
        return Accepted(ApiResponse<SendBulkNotificationResultDto>.Ok(result.Data!, "Notifications processed successfully."));
    }

    /// <summary>
    /// Export the list of failed emergency notification recipients to a formatted PDF report (Doctor role).
    /// </summary>
    [HttpGet("notifications/export-failed-pdf/{appealId}")]
    [Authorize(Roles = "Doctor")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportFailedDonorsPdf(
        [FromRoute] Guid appealId,
        CancellationToken cancellationToken = default)
    {
        var query = new ExportFailedDonorsPdfQuery(appealId);
        var pdfBytes = await sender.Send(query, cancellationToken);
        var filename = $"failed_donors_report_{DateTime.UtcNow:yyyy-MM-dd}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    /// <summary>
    /// Preview the emergency notification title, message, and target recipient count (Doctor role).
    /// </summary>
    [HttpPost("notifications/preview")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<NotificationPreviewResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEmergencyNotificationPreview(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEmergencyNotificationPreviewQuery(
            request.SelectionMode,
            request.DonorIds,
            request.Filters,
            request.ExcludedDonorIds);
        var result = await sender.Send(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Failed to generate notification preview."));
        }

        return Ok(ApiResponse<NotificationPreviewResponseDto>.Ok(result.Data!, "Notification preview generated successfully."));
    }
}

public class SendNotificationRequest
{
    [JsonPropertyName("selectionMode")]
    public string? SelectionMode { get; set; }

    [JsonPropertyName("donorIds")]
    public List<Guid>? DonorIds { get; set; }

    [JsonPropertyName("filters")]
    public DonorEligibilityFiltersDto? Filters { get; set; }

    [JsonPropertyName("excludedDonorIds")]
    public List<Guid>? ExcludedDonorIds { get; set; }
}
