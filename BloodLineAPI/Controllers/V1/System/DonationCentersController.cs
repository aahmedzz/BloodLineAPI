using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using BloodLineAPI.Application.Features.Appointments.Queries.GetDonationCenters;
using BloodLineAPI.Attributes;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Controllers.V1.System;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/donation-centers")]
[ApiAudience(Audience.System)]
[Authorize(Roles = "Admin,Doctor")]
[Produces("application/json")]
public class DonationCentersController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Fetch the main branch center details (Admin and Doctor).
    /// </summary>
    [HttpGet("main-branch")]
    [ProducesResponseType(typeof(ApiResponse<DonationCenterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMainBranch(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDonationCentersQuery(null), cancellationToken);
        var mainBranchCenter = result
            .FirstOrDefault(center => string.Equals(center.CenterType, CenterType.MainBranch.ToString(), StringComparison.OrdinalIgnoreCase));

        if (mainBranchCenter is null)
        {
            return NotFound(ApiResponse<object>.Fail("Main branch center was not found."));
        }

        return Ok(ApiResponse<DonationCenterDto>.Ok(mainBranchCenter, "Success"));
    }
}
