using Asp.Versioning;
using BloodLineAPI.Application.Features.Auth.Commands.RegisterMobileUser;
using BloodLineAPI.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BloodLineAPI.Controllers.V1.Mobile;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mobile/[controller]")]
[ApiAudience(Audience.Mobile)]
public class AuthController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterMobileUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(new { Error = result.Error });
        }
        
        return Ok(new { Token = result.Data });
    }
}
