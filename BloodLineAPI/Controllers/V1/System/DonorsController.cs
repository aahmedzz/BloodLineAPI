using BloodLineAPI.Attributes;
using BloodLineAPI.Controllers.V1.Base;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BloodLineAPI.Controllers.V1.System;

[Route("api/v{version:apiVersion}/sys/[controller]")]
[ApiAudience(Audience.System)]
public class DonorsController(ISender sender) : DonorsControllerBase(sender)
{

    [HttpGet("SystemTest")]
    public async Task<IActionResult> TestSystemcall(CancellationToken cancellationToken)
    {
        return Ok("Call from system api...");
    }
}
