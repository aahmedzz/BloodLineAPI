using System;
using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Campaigns.Commands.CreateCampaign;
using BloodLineAPI.Application.Features.Campaigns.Commands.UpdateCampaign;
using BloodLineAPI.Application.Features.Campaigns.Commands.DeleteCampaign;
using BloodLineAPI.Application.Features.Campaigns.Commands.CompleteCampaign;
using BloodLineAPI.Application.Features.Campaigns.Queries.GetCampaignsList;
using BloodLineAPI.Application.Features.Campaigns.Queries.GetCampaignAppointments;
using BloodLineAPI.Application.Features.Campaigns.Dtos;
using BloodLineAPI.Attributes;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Controllers.V1.System;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/[controller]")]
[ApiAudience(Audience.System)]
[Produces("application/json")]
public class CampaignsController(ISender sender) : ControllerBase
{
    [HttpGet]
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

    [HttpPost]
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

    [HttpPatch("{id}")]
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

    [HttpDelete("{id}")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
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

    [HttpPost("{id}/complete")]
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

    [HttpGet("{id}/appointments")]
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
}
