using BloodLineAPI.Attributes;
using BloodLineAPI.Controllers.V1.Base;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BloodLineAPI.Controllers.V1.Mobile;

[Route("api/v{version:apiVersion}/mobile/[controller]")]
[ApiAudience(Audience.Mobile)]
public class DonorsController(ISender sender) : DonorsControllerBase(sender)
{
        [HttpGet("MobileTest")]
        public async Task<IActionResult> TestMobilecall(CancellationToken cancellationToken)
        {
            return Ok("Call from mobile api...");
        }
}
