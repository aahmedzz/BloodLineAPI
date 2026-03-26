using Asp.Versioning;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Auth.Commands.LoginMobileUser;
using BloodLineAPI.Application.Features.Auth.Commands.RefreshToken;
using BloodLineAPI.Application.Features.Auth.Commands.RegisterMobileUser;
using BloodLineAPI.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;   

namespace BloodLineAPI.Controllers.V1.Mobile;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mobile/[controller]")]
[ApiAudience(Audience.Mobile)]
public class AuthController(ISender sender, IJwtGenerator jwtGenerator) : ControllerBase
{
    private readonly ISender _sender = sender;
   

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterMobileUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(result.Data);
    }
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginMobileUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
        }
        return Ok(result.Data);
    }
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(result.Data);
    }
}
