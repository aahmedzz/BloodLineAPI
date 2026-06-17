using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Doctor.Dtos;
using BloodLineAPI.Application.Features.Doctor.Queries.GetDoctorDashboard;
using BloodLineAPI.Attributes;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Controllers.V1.System;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/[controller]")]
[ApiAudience(Audience.System)]
[Produces("application/json")]
public class DoctorController(ISender sender) : ControllerBase
{
    [HttpGet("dashboard")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await sender.Send(new GetDoctorDashboardQuery(), ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Failed to fetch dashboard data."));
        }
        return Ok(ApiResponse<DoctorDashboardDto>.Ok(result.Data!, "Success"));
    }
}
