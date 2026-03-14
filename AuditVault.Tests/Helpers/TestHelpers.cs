using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AirMark.AuditVault.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace AirMark.AuditVault.Tests.Helpers;

/// <summary>
/// A controllable implementation of ICurrentTenantService for unit tests.
/// </summary>
public class FakeTenantService : ICurrentTenantService
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = string.Empty;

    public FakeTenantService(Guid tenantId, Guid userId, string role)
    {
        TenantId = tenantId;
        UserId = userId;
        Role = role;
    }

    public void SetFromClaims(Guid tenantId, Guid userId, string role)
    {
        TenantId = tenantId;
        UserId = userId;
        Role = role;
    }
}

/// <summary>
/// Generates valid JWT tokens for integration tests.
/// </summary>
public static class JwtTestFactory
{
    private const string SecretKey = "dev-only-secret-key-change-in-production-32chars!";

    public static string GenerateToken(Guid userId, Guid tenantId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("tenantId", tenantId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "AuditVaultAPI",
            audience: "AuditVaultClients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
