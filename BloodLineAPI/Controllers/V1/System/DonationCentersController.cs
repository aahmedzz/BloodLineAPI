using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonationCenters.Dtos;
using BloodLineAPI.Application.Features.DonationCenters.Queries.GetMainBranchSettings;
using BloodLineAPI.Application.Features.DonationCenters.Commands.UpdateMainBranchSettings;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using BloodLineAPI.Application.Features.DonorEligibility.Queries.GetCooldownSettings;
using BloodLineAPI.Application.Features.DonorEligibility.Commands.UpdateCooldownSettings;
using BloodLineAPI.Application.Features.Campaigns.Commands.CreateCampaign;
using BloodLineAPI.Application.Features.Campaigns.Commands.UpdateCampaign;
using BloodLineAPI.Application.Features.Campaigns.Commands.DeleteCampaign;
using BloodLineAPI.Application.Features.Campaigns.Commands.CompleteCampaign;
using BloodLineAPI.Application.Features.Campaigns.Queries.GetCampaignsList;
using BloodLineAPI.Application.Features.Campaigns.Queries.GetCampaignAppointments;
using BloodLineAPI.Application.Features.Campaigns.Dtos;
using BloodLineAPI.Controllers.V1.System.Requests;
using BloodLineAPI.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BloodLineAPI.Controllers.V1.System;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/donation-centers")]
[ApiAudience(Audience.System)]
[Produces("application/json")]
public class DonationCentersController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Fetch the main branch center details (Admin and Doctor).
    /// </summary>
    [HttpGet("main-branch")]
    [Authorize(Roles = "Admin,Doctor")]
    [ProducesResponseType(typeof(ApiResponse<MainBranchSettingsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMainBranch(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMainBranchSettingsQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(result.Error ?? "Main branch center was not found."));
        }

        return Ok(ApiResponse<MainBranchSettingsResult>.Ok(result.Data!, "Success"));
    }

    /// <summary>
    /// Update the main branch center details (Admin only).
    /// </summary>
    [HttpPut("main-branch")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<MainBranchSettingsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMainBranch(
        [FromBody] UpdateMainBranchSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateMainBranchSettingsCommand(
            request.Name,
            request.Location,
            request.AddressDetails,
            request.SupportedDonationTypes,
            request.SlotDurationMinutes,
            request.MaxDonorsPerSlot,
            request.WeeklyHours,
            request.Exclusions,
            request.Version
        );

        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Failed to update main branch settings."));
        }

        return Ok(ApiResponse<MainBranchSettingsResult>.Ok(result.Data!, "Main branch settings updated successfully."));
    }

    #region Campaigns Endpoints (Consolidated)

    [HttpGet("~/api/v{version:apiVersion}/system/campaigns")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedCampaignsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCampaigns([FromQuery] GetCampaignsListQuery query, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Failed to get campaigns."));
        }
        return Ok(ApiResponse<PaginatedCampaignsResult>.Ok(result.Data!, "Success"));
    }

    [HttpPost("~/api/v{version:apiVersion}/system/campaigns")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<CampaignDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Failed to create campaign."));
        }
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CampaignDto>.Ok(result.Data!, "تم إنشاء الحملة بنجاح"));
    }

    [HttpPatch("~/api/v{version:apiVersion}/system/campaigns/{id}")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<CampaignDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCampaign(string id, [FromBody] UpdateCampaignCommand command, CancellationToken ct)
    {
        var updatedCommand = command with { Id = id };
        var result = await sender.Send(updatedCommand, ct);
        if (!result.IsSuccess)
        {
            if (result.Error != null && result.Error.StartsWith("FORBIDDEN:", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(result.Error.Replace("FORBIDDEN:", "").Trim()));
            }
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Failed to update campaign."));
        }
        return Ok(ApiResponse<CampaignDto>.Ok(result.Data!, "تم تحديث بيانات الحملة بنجاح"));
    }

    [HttpDelete("~/api/v{version:apiVersion}/system/campaigns/{id}")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCampaign(string id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCampaignCommand(id), ct);
        if (!result.IsSuccess)
        {
            if (result.Error != null && result.Error.StartsWith("FORBIDDEN:", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(result.Error.Replace("FORBIDDEN:", "").Trim()));
            }
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Failed to delete campaign."));
        }
        return Ok(ApiResponse.Ok(null, "تم حذف الحملة بنجاح"));
    }

    [HttpPost("~/api/v{version:apiVersion}/system/campaigns/{id}/complete")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<CampaignDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteCampaign(string id, CancellationToken ct)
    {
        var result = await sender.Send(new CompleteCampaignCommand(id), ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }
        return Ok(ApiResponse<CampaignDto>.Ok(result.Data!, "تم إنهاء الحملة بنجاح"));
    }

    [HttpGet("~/api/v{version:apiVersion}/system/campaigns/{id}/appointments")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CampaignAppointmentSlotDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAppointments(string id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCampaignAppointmentsQuery(id), ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }
        return Ok(ApiResponse<IReadOnlyList<CampaignAppointmentSlotDto>>.Ok(result.Data!, "Success"));
    }

    #endregion

    #region Doctor Settings Endpoints

    /// <summary>
    /// Retrieve donation eligibility cooldown settings (Doctor only).
    /// </summary>
    [HttpGet("~/api/v{version:apiVersion}/system/doctor/settings/cooldown")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<CooldownSettingsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCooldownSettings(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCooldownSettingsQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Failed to retrieve settings."));
        }

        return Ok(ApiResponse<CooldownSettingsResult>.Ok(result.Data!, "Eligibility settings retrieved successfully."));
    }

    /// <summary>
    /// Update donation eligibility cooldown settings (Doctor only).
    /// </summary>
    [HttpPut("~/api/v{version:apiVersion}/system/doctor/settings/cooldown")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<CooldownSettingsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCooldownSettings(
        [FromBody] UpdateCooldownSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCooldownSettingsCommand(
            request.WholeBloodMaleDays,
            request.WholeBloodFemaleDays,
            request.PlasmaDays,
            request.PlateletsDays,
            request.DefaultScreeningLockoutDays);

        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Failed to update settings."));
        }

        return Ok(ApiResponse<CooldownSettingsResult>.Ok(result.Data!, "Eligibility settings updated successfully."));
    }

    #endregion
}

public record UpdateCooldownSettingsRequest(
    int WholeBloodMaleDays,
    int WholeBloodFemaleDays,
    int PlasmaDays,
    int PlateletsDays,
    int DefaultScreeningLockoutDays);
