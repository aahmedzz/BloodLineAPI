using System.Security.Claims;
using BloodLineAPI.Domain.Entities.Users;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IJwtGenerator
{
    string GenerateToken(User user, IEnumerable<string> roles);
    string GenerateTemporaryToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
