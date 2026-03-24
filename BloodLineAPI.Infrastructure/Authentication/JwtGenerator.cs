using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BloodLineAPI.Infrastructure.Authentication;

public class JwtGenerator : IJwtGenerator
{
    private readonly string _secret;
    private readonly string? _issuer;
    private readonly string? _audience;
    private readonly int _expirationMinutes;

    public JwtGenerator(IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");

        _secret = jwtSettings["Secret"]
                  ?? throw new InvalidOperationException("Jwt Secret is not configured.");
        _issuer = jwtSettings["Issuer"];
        _audience = jwtSettings["Audience"];
        _expirationMinutes = int.TryParse(jwtSettings["ExpiryMinutes"], out var minutes) && minutes > 0
            ? minutes
            : 60;
    }

    public string GenerateToken(User user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
