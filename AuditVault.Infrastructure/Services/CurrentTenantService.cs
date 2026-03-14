using AirMark.AuditVault.Application.Interfaces;

namespace AirMark.AuditVault.Infrastructure.Services;

/// <summary>
/// Scoped service that holds the current authenticated user's tenant context.
/// Populated by TenantResolutionMiddleware on every request.
/// The DbContext global query filter reads from this to enforce tenant isolation.
/// </summary>
public class CurrentTenantService : ICurrentTenantService
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = string.Empty;

    public void SetFromClaims(Guid tenantId, Guid userId, string role)
    {
        TenantId = tenantId;
        UserId = userId;
        Role = role;
    }
}
