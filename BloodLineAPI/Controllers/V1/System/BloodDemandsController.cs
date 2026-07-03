using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.BloodDemands.Commands.CreateBloodDemand;
using BloodLineAPI.Application.Features.BloodDemands.Commands.CancelBloodDemand;
using BloodLineAPI.Application.Features.BloodDemands.Queries.GetBloodDemands;
using BloodLineAPI.Application.Features.BloodDemands.Queries.GetBloodDemandDetail;
using BloodLineAPI.Application.Features.BloodDemands.Queries.GetBloodDemandsDashboard;
using BloodLineAPI.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Controllers.V1.System
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/system/blood-demands")]
    [ApiAudience(Audience.System)]
    [Produces("application/json")]
    public class BloodDemandsController(ISender sender) : ControllerBase
    {
        /// <summary>
        /// Creates a new blood request (demand).
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "InventoryManager")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateBloodDemand(
            [FromBody] CreateBloodDemandCommand command,
            CancellationToken cancellationToken)
        {
            var id = await sender.Send(command, cancellationToken);
            return Ok(ApiResponse<Guid>.Ok(id, "تم إنشاء طلب الدم بنجاح"));
        }

        /// <summary>
        /// Retrieves counts of blood demands grouped by status for dashboard display.
        /// </summary>
        [HttpGet("dashboard")]
        [Authorize(Policy = "InventoryManager")]
        [ProducesResponseType(typeof(ApiResponse<BloodDemandsDashboardResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        {
            var result = await sender.Send(new GetBloodDemandsDashboardQuery(), cancellationToken);
            return Ok(ApiResponse<BloodDemandsDashboardResult>.Ok(result, "تم استرجاع إحصائيات طلبات الدم بنجاح"));
        }

        /// <summary>
        /// Lists blood demands with pagination, filtering, and sorting.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "InventoryManager")]
        [ProducesResponseType(typeof(ApiResponse<GetBloodDemandsResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBloodDemands(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? bloodType = null,
            [FromQuery] string? priority = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetBloodDemandsQuery(page, limit, search, status, bloodType, priority);
            var result = await sender.Send(query, cancellationToken);
            return Ok(ApiResponse<GetBloodDemandsResult>.Ok(result, "تم استرجاع طلبات الدم بنجاح"));
        }

        /// <summary>
        /// Retrieves the details of a single blood demand including its issuance history.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = "InventoryManager")]
        [ProducesResponseType(typeof(ApiResponse<BloodDemandDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBloodDemandDetail(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            var result = await sender.Send(new GetBloodDemandDetailQuery(id), cancellationToken);

            if (result == null)
            {
                return NotFound(ApiResponse.Fail("طلب الدم المطلوب غير موجود"));
            }

            return Ok(ApiResponse<BloodDemandDetailDto>.Ok(result, "تم استرجاع تفاصيل طلب الدم بنجاح"));
        }

        /// <summary>
        /// Cancels a pending blood demand or closes a partially fulfilled one.
        /// </summary>
        [HttpPost("{id:guid}/cancel")]
        [Authorize(Policy = "InventoryManager")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CancelBloodDemand(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            await sender.Send(new CancelBloodDemandCommand(id), cancellationToken);
            return Ok(ApiResponse.Ok("تم تعديل حالة الطلب بنجاح"));
        }
    }
}
