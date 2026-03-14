namespace AirMark.AuditVault.Application.Interfaces;

public interface ICurrentTenantService
{
    Guid TenantId { get; }
    Guid UserId { get; }
    string Role { get; }
    void SetFromClaims(Guid tenantId, Guid userId, string role);
}
