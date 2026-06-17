using System.Security.Claims;
using BloodLineAPI.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BloodLineAPI.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
