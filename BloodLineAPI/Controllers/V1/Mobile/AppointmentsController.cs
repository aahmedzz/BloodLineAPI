using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Commands.CancelAppointment;
using BloodLineAPI.Application.Features.Appointments.Commands.CreateAppointment;
using BloodLineAPI.Application.Features.Appointments.Commands.RescheduleAppointment;
using BloodLineAPI.Application.Features.Appointments.Commands.SubmitHealthPreScreening;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using BloodLineAPI.Application.Features.Appointments.Queries.GetAppointmentDetails;
using BloodLineAPI.Application.Features.Appointments.Queries.GetAvailableTimeSlots;
using BloodLineAPI.Application.Features.Appointments.Queries.GetDonationCenters;
using BloodLineAPI.Application.Features.Appointments.Queries.GetDonorAppointments;
using BloodLineAPI.Attributes;
using BloodLineAPI.Controllers.V1.Mobile.Requests;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;

namespace BloodLineAPI.Controllers.V1.Mobile;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mobile/[controller]")]
[ApiAudience(Audience.Mobile)]
[Produces("application/json")]
[Authorize]
public sealed class AppointmentsController(ISender sender) : ControllerBase
{
    [HttpGet("centers")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DonationCenterDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCenters([FromQuery] string? search, CancellationToken ct)
    {
        var result = await sender.Send(new GetDonationCentersQuery(search), ct);
        return Ok(ApiResponse<IReadOnlyList<DonationCenterDto>>.Ok(result));
    }

    [HttpGet("time-slots")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TimeSlotDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimeSlots([FromQuery] Guid centerId, [FromQuery] DateTime date, CancellationToken ct)
    {
        var result = await sender.Send(new GetAvailableTimeSlotsQuery(centerId, date), ct);
        return Ok(ApiResponse<IReadOnlyList<TimeSlotDto>>.Ok(result));
    }

    [HttpPost("Create-appointement")]
    [ProducesResponseType(typeof(ApiResponse<CreateAppointmentResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        if (!TryParseStartTime(request.StartTime, out var startTime))
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid startTime format. Use HH:mm or HH:mm:ss (example: 09:00:00)."));
        }

        if (!TryParseDonationType(request.DonationType, out var donationType))
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid donationType. Use one of: whole blood, plasma, platelets."));
        }

        var command = new CreateAppointmentCommand(
            request.DonationCenterId,
            request.ScheduledDate,
            startTime,
            donationType)
        {
            DonorId = donorId
        };

        var result = await sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<CreateAppointmentResultDto>.Ok(result.Data!));
    }

    [HttpPost("health-screening")]
    [ProducesResponseType(typeof(ApiResponse<HealthPreScreeningResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitHealthScreening([FromBody] SubmitHealthPreScreeningCommand command, CancellationToken ct)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await sender.Send(command with { DonorId = donorId }, ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<HealthPreScreeningResultDto>.Ok(result.Data!));
    }

    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelAppointmentRequest request, CancellationToken ct)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await sender.Send(new CancelAppointmentCommand(id, request.Reason) { DonorId = donorId }, ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<string>.Ok(result.Data!));
    }

    [HttpPut("{id:guid}/reschedule")]
    [ProducesResponseType(typeof(ApiResponse<CreateAppointmentResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] RescheduleAppointmentRequest request, CancellationToken ct)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var command = new RescheduleAppointmentCommand(id, request.NewScheduledDate, request.NewStartTime)
        {
            DonorId = donorId
        };

        var result = await sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<CreateAppointmentResultDto>.Ok(result.Data!));
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AppointmentListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyAppointments([FromQuery] bool upcomingOnly = true, CancellationToken ct = default)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await sender.Send(new GetDonorAppointmentsQuery(upcomingOnly) { DonorId = donorId }, ct);
        return Ok(ApiResponse<IReadOnlyList<AppointmentListItemDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDetails(Guid id, CancellationToken ct)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await sender.Send(new GetAppointmentDetailsQuery(id) { DonorId = donorId }, ct);
        return Ok(ApiResponse<AppointmentDetailsDto>.Ok(result));
    }

    private bool TryGetDonorId(out Guid donorId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out donorId);
    }

    private static bool TryParseStartTime(string value, out TimeSpan startTime)
    {
        return TimeSpan.TryParseExact(
            value,
            [@"hh\:mm", @"hh\:mm\:ss", @"h\:mm", @"h\:mm\:ss"],
            CultureInfo.InvariantCulture,
            out startTime);
    }

    private static bool TryParseDonationType(string value, out DonationType donationType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            donationType = default;
            return false;
        }

        var normalized = new string(value.Where(char.IsLetterOrDigit).ToArray());
        return Enum.TryParse(normalized, ignoreCase: true, out donationType);
    }
}
