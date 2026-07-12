using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Infrastructure.Identity;

/// <summary>
/// Concrete <see cref="ITokenService"/> adapter that issues HS256-signed JWTs.
/// Claim set is frozen per Decision #2 (EP02 engineering addenda): "sub"
/// (User.Id), "email" (User.Email.Value), "name" (User.Name). Do not add
/// claims beyond this set. Issuer/Audience/Secret/ExpirySeconds are sourced
/// exclusively from <see cref="JwtOptions"/> (env-var backed) — never read a
/// signing secret from a literal.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
            new Claim("name", user.Name),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(_options.ExpirySeconds),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
