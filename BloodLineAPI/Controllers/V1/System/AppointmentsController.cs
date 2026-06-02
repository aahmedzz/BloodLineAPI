using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using BloodLineAPI.Application.Features.Appointments.Queries.GetSystemAppointments;
using BloodLineAPI.Application.Features.Appointments.Queries.GetAppointmentStats;
using BloodLineAPI.Application.Features.Appointments.Queries.GetSystemAppointmentById;
using BloodLineAPI.Application.Features.Appointments.Commands.SystemCancelAppointment;
using BloodLineAPI.Application.Features.Appointments.Commands.MarkAppointmentNoShow;
using BloodLineAPI.Attributes;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Controllers.V1.System;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/[controller]")]
[ApiAudience(Audience.System)]
[Authorize(Roles = "Doctor")]
[Produces("application/json")]
public class AppointmentsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Fetch dynamic timeline slots list for the doctor dashboard.
    /// </summary>
    [HttpGet("slots")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedAppointmentsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSlots(
        [FromQuery] Guid? centerId,
        [FromQuery] DateTime? date,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? status,
        [FromQuery] Guid? campaignId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var query = new GetSystemAppointmentsQuery(centerId, date, dateFrom, dateTo, status, campaignId, page, limit);
        var result = await sender.Send(query, ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }
        return Ok(ApiResponse<PaginatedAppointmentsResult>.Ok(result.Data!, "Success"));
    }

    /// <summary>
    /// Fetch detailed information for a single appointment by ID.
    /// </summary>
    [HttpGet("slots/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SystemAppointmentDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSlotById(
        Guid id,
        CancellationToken ct = default)
    {
        var query = new GetSystemAppointmentByIdQuery(id);
        var result = await sender.Send(query, ct);
        if (!result.IsSuccess)
        {
            if (result.Error == "Appointment not found.")
            {
                return NotFound(ApiResponse<object>.Fail(result.Error));
            }
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }
        return Ok(ApiResponse<SystemAppointmentDetailsDto>.Ok(result.Data!, "Slot retrieved successfully"));
    }

    /// <summary>
    /// Fetch aggregated appointment statistics for the dashboard cards.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStats(
        [FromQuery] Guid? centerId,
        [FromQuery] DateTime? date,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken ct = default)
    {
        var query = new GetAppointmentStatsQuery(centerId, date, dateFrom, dateTo);
        var result = await sender.Send(query, ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }
        return Ok(ApiResponse<AppointmentStatsDto>.Ok(result.Data!, "Success"));
    }

    /// <summary>
    /// Cancel an appointment (staff/doctor bypasses grace periods).
    /// </summary>
    [HttpPost("slots/{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] SystemCancelRequest request,
        CancellationToken ct = default)
    {
        var command = new SystemCancelAppointmentCommand(id, request.Reason);
        var result = await sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.Error == "DonationAppointment not found.")
            {
                return NotFound(ApiResponse<object>.Fail(result.Error));
            }
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }
        return Ok(ApiResponse<string>.Ok(result.Data!, "Appointment cancelled successfully."));
    }

    /// <summary>
    /// Manually mark an appointment as a no-show (missed).
    /// </summary>
    [HttpPost("slots/{id:guid}/no-show")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MarkNoShow(
        Guid id,
        CancellationToken ct = default)
    {
        var command = new MarkAppointmentNoShowCommand(id);
        var result = await sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.Error == "DonationAppointment not found.")
            {
                return NotFound(ApiResponse<object>.Fail(result.Error));
            }
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }
        return Ok(ApiResponse<string>.Ok(result.Data!, "Appointment marked as no-show successfully."));
    }
}

public sealed record SystemCancelRequest(string? Reason);
