using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Donations.Queries.GetFilteredDonations;
using BloodLineAPI.Application.Features.Donations.Commands.CreateDonation;
using BloodLineAPI.Application.Features.Donations.Commands.AddMedicalRecord;
using BloodLineAPI.Application.Features.Donations.Commands.ConfirmDonation;
using BloodLineAPI.Application.Features.Donations.Commands.DeleteDonation;
using BloodLineAPI.Controllers.V1.System.Requests;
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
public class DonationsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Fetch filtered and paginated donations list (Doctor only).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedDonationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFilteredDonations(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? bloodType = null,
        [FromQuery] string? donationSource = null,
        [FromQuery] string? donationStatus = null,
        [FromQuery] string? datePreset = null,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFilteredDonationsQuery(page, limit, search, bloodType, donationSource, donationStatus, datePreset, fromDate, toDate);
        var result = await sender.Send(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<PaginatedDonationResult>.Ok(result.Data!, "Success"));
    }

    /// <summary>
    /// Step 1: Register initial donation flow (Doctor only).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateDonation([FromBody] CreateDonationCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<object>.Ok(new { id = result.Data, name = command.Name }, "تم تسجيل التبرع المبدئي بنجاح"));
    }

    /// <summary>
    /// Step 2: Record medical screening results (Doctor only).
    /// </summary>
    [HttpPost("{id:guid}/medical-record")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddMedicalRecord(Guid id, [FromBody] AddMedicalRecordRequest request, CancellationToken cancellationToken)
    {
        var command = new AddMedicalRecordCommand(
            id,
            request.Status,
            request.Diseases,
            request.AdditionalData,
            request.IsAllergic,
            request.RejectionReason,
            request.DeferredUntil,
            request.DonationType,
            request.BloodType
        );

        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Error == "Donation not found.")
            {
                return NotFound(ApiResponse<object>.Fail(result.Error));
            }
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<object>.Ok(new { donationCode = result.Data! }, "تم تسجيل البيانات الطبية بنجاح"));
    }

    /// <summary>
    /// Step 3: Confirm donation and send blood bag to lab (Doctor only).
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmDonation(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmDonationCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Error == "Donation not found.")
            {
                return NotFound(ApiResponse<object>.Fail(result.Error));
            }
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<object>.Ok(null!, "تم إرسال التبرع للمختبر بنجاح"));
    }

    /// <summary>
    /// Cancel/delete an active donation (Doctor only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDonation(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteDonationCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Error == "Donation not found.")
            {
                return NotFound(ApiResponse<object>.Fail(result.Error));
            }
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<object>.Ok(null!, "تم إلغاء التبرع بنجاح"));
    }
}

