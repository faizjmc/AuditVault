using AirMark.AuditVault.Domain.Entities;

namespace AirMark.AuditVault.Application.Interfaces;

public interface ILogRepository
{
    Task<AuditLog> CreateAsync(AuditLog log);
    Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetByTenantAsync(Guid tenantId, int page, int limit);
    Task<AuditLog?> GetByIdAndTenantAsync(Guid id, Guid tenantId);
}
