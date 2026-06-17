using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Lab.Commands.SubmitLabTestResult;
using BloodLineAPI.Application.Features.Lab.Queries.GetLabDashboardStats;
using BloodLineAPI.Application.Features.Lab.Queries.GetLabTestById;
using BloodLineAPI.Application.Features.Lab.Queries.GetLabTests;
using BloodLineAPI.Application.Features.Lab.Queries.GetResults;
using BloodLineAPI.Application.Features.Lab.Queries.GetSamples;
using BloodLineAPI.Attributes;
using BloodLineAPI.Controllers.V1.System.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloodLineAPI.Controllers.V1.System;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/lab")]
[ApiAudience(Audience.System)]
[Authorize(Policy = "Lab")]
[Produces("application/json")]
public class LabController(ISender sender) : ControllerBase
{
    [HttpGet("tests")]
    [ProducesResponseType(typeof(ApiResponse<GetLabTestsResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLabTests(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? bloodType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetLabTestsQuery(page, limit, status, search, bloodType), cancellationToken);
        return Ok(ApiResponse<GetLabTestsResult>.Ok(result));
    }

    [HttpGet("tests/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GetLabTestByIdResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLabTestById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLabTestByIdQuery(id), cancellationToken);
        if (result is null)
            return NotFound(ApiResponse.Fail("Lab test not found."));

        return Ok(ApiResponse<GetLabTestByIdResult>.Ok(result));
    }

    [HttpPost("tests/{id:guid}/result")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitResult(
        Guid id,
        [FromBody] SubmitResultRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cmd = new SubmitLabTestResultCommand(
                id,
                request.ConfirmedBloodType,
                request.Hcv,
                request.Hbv,
                request.Syphilis,
                request.Hiv,
                request.Notes);

            var result = await sender.Send(cmd, cancellationToken);

            var payload = new
            {
                id = result.DonationAppointmentId,
                status = "completed",
                result = new
                {
                    outcome = result.Outcome,
                    confirmedBloodType = request.ConfirmedBloodType,
                    hcv = request.Hcv,
                    hbv = request.Hbv,
                    syphilis = request.Syphilis,
                    hiv = request.Hiv,
                    notes = request.Notes,
                    completedAt = result.CompletedAt,
                    completedById = result.CompletedById,
                    completedByName = result.CompletedByName
                }
            };

            return Ok(ApiResponse.Ok(payload, "تم حفظ نتيجة الفحص بنجاح"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpGet("samples")]
    [ProducesResponseType(typeof(ApiResponse<GetSamplesResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSamples(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 100,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? bloodType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetSamplesQuery(page, limit, search, status, bloodType), cancellationToken);
        return Ok(ApiResponse<GetSamplesResult>.Ok(result));
    }

    [HttpGet("results")]
    [ProducesResponseType(typeof(ApiResponse<GetResultsResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResults(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 100,
        [FromQuery] string? search = null,
        [FromQuery] string? bloodType = null,
        [FromQuery] string? outcome = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetResultsQuery(page, limit, search, bloodType, outcome), cancellationToken);
        return Ok(ApiResponse<GetResultsResult>.Ok(result));
    }

    [HttpGet("dashboard/stats")]
    [ProducesResponseType(typeof(ApiResponse<GetLabDashboardStatsResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLabDashboardStatsQuery(), cancellationToken);
        return Ok(ApiResponse<GetLabDashboardStatsResult>.Ok(result, "Dashboard statistics retrieved successfully"));
    }
}

