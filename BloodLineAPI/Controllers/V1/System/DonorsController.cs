using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Donors.Queries.GetFilteredDonors;
using BloodLineAPI.Application.Features.Donors.Queries.GetDonorById;
using BloodLineAPI.Application.Features.Donors.Queries.SearchDonorByNationalId;
using BloodLineAPI.Application.Features.Donors.Commands.UpdateDonor;
using BloodLineAPI.Application.Features.Gamification.Commands.ReconcileBadges;
using BloodLineAPI.Attributes;
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
[Authorize]
[Produces("application/json")]
public class DonorsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Fetch filtered and paginated donors list (Admin and Doctor).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Doctor")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedDonorResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFilteredDonors(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? bloodType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? district = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFilteredDonorsQuery(page, limit, search, bloodType, status, district);
        var result = await sender.Send(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<PaginatedDonorResult>.Ok(result.Data!, "Success"));
    }

    /// <summary>
    /// Fetch donor by ID (Admin and Doctor).
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Doctor")]
    [ProducesResponseType(typeof(ApiResponse<FilteredDonorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDonorById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDonorByIdQuery(id), cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<FilteredDonorDto>.Ok(result.Data!, "Success"));
    }

    /// <summary>
    /// Search donor by National ID (Doctor only).
    /// </summary>
    [HttpGet("search")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<FilteredDonorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchDonorByNationalId([FromQuery] string nationalId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SearchDonorByNationalIdQuery(nationalId), cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<FilteredDonorDto>.Ok(result.Data!, "Success"));
    }

    /// <summary>
    /// Update donor profile (Admin and Doctor).
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin,Doctor")]
    [ProducesResponseType(typeof(ApiResponse<FilteredDonorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateDonor(Guid id, [FromBody] UpdateDonorRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateDonorCommand(
            id,
            request.Name,
            request.Phone,
            request.BloodType,
            request.Governorate,
            request.District,
            request.Area,
            request.NationalId,
            request.DateOfBirth
        );

        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Error == "Donor not found.")
            {
                return NotFound(ApiResponse<object>.Fail(result.Error));
            }
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<FilteredDonorDto>.Ok(result.Data!, "Success"));
    }

    /// <summary>
    /// Reconcile badges and XP for all donors (Admin only).
    /// </summary>
    [HttpPost("reconcile-badges")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ReconcileBadgesResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReconcileBadges(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReconcileBadgesCommand(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<ReconcileBadgesResultDto>.Ok(result.Data!, "Badges and XP reconciled successfully."));
    }
}

public record UpdateDonorRequest(
    string? Name = null,
    string? Phone = null,
    string? BloodType = null,
    string? Governorate = null,
    string? District = null,
    string? Area = null,
    string? NationalId = null,
    string? DateOfBirth = null);
