using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Auth.Commands.CreateStaffAccount;
using BloodLineAPI.Application.Features.Auth.Commands.UpdateStaffAccount;
using BloodLineAPI.Application.Features.Auth.Commands.DeleteStaff;
using BloodLineAPI.Application.Features.Auth.Queries.GetFilteredStaff;
using BloodLineAPI.Application.Features.Dashboard.Queries.GetAdminDashboard;
using BloodLineAPI.Application.Features.Doctor.Dtos;
using BloodLineAPI.Application.Features.Doctor.Queries.GetDoctorDashboard;
using BloodLineAPI.Attributes;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BloodLineAPI.Controllers.V1.System;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/[controller]")]
[ApiAudience(Audience.System)]
[Produces("application/json")]
public class StaffController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Create a new staff account (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateStaffAccount([FromBody] CreateStaffAccountCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<Guid>.Ok(result.Data));
    }

    /// <summary>
    /// Update an existing staff account (Admin only).
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateStaffAccount(Guid id, [FromBody] UpdateStaffAccountCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { StaffId = id }, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Error == "Staff member not found.")
            {
                return NotFound(ApiResponse<object>.Fail(result.Error));
            }
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<Guid>.Ok(result.Data));
    }

    /// <summary>
    /// Fetch filtered and paginated staff list (Admin only).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedStaffResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFilteredStaff(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFilteredStaffQuery(page, limit, search, role, status);
        var result = await sender.Send(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<PaginatedStaffResult>.Ok(result.Data!, "Success"));
    }

    /// <summary>
    /// Delete / Deactivate a staff member (Admin only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteStaff(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteStaffCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Error == "Staff member not found.")
            {
                return NotFound(ApiResponse<object>.Fail(result.Error));
            }
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<object>.Ok(null!, message: "Staff deleted successfully"));
    }

    /// <summary>
    /// Retrieves the unified dashboard overview data for the Admin Dashboard.
    /// Includes summary statistics, blood inventory status, donation trends, notifications, and recent donors in a single response.
    /// </summary>
    [HttpGet("~/api/v{version:apiVersion}/system/admin/dashboard")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<GetAdminDashboardResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAdminDashboard(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAdminDashboardQuery(), cancellationToken);
        return Ok(ApiResponse<GetAdminDashboardResult>.Ok(result, "تم تحميل بيانات لوحة التحكم بنجاح"));
    }

    /// <summary>
    /// Retrieves the doctor's personal dashboard data.
    /// </summary>
    [HttpGet("~/api/v{version:apiVersion}/system/Doctor/dashboard")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDoctorDashboard(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDoctorDashboardQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error ?? "Failed to fetch dashboard data."));
        }
        return Ok(ApiResponse<DoctorDashboardDto>.Ok(result.Data!, "Success"));
    }
}

