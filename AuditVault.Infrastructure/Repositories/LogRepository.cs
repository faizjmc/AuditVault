using AirMark.AuditVault.Application.Interfaces;
using AirMark.AuditVault.Domain.Entities;
using AirMark.AuditVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirMark.AuditVault.Infrastructure.Repositories;

public class LogRepository : ILogRepository
{
    private readonly AppDbContext _context;

    public LogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog> CreateAsync(AuditLog log)
    {
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetByTenantAsync(
        Guid tenantId, int page, int limit)
    {
        // Note: The global query filter in AppDbContext already restricts to current tenant.
        // The tenantId parameter here is redundant but kept for explicitness and testability.
        var query = _context.AuditLogs
            .Include(l => l.Uploader)
            .OrderByDescending(l => l.CreatedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<AuditLog?> GetByIdAndTenantAsync(Guid id, Guid tenantId)
    {
        // Global query filter ensures tenant isolation — only logs belonging
        // to the current tenant will be returned regardless of the id provided.
        return await _context.AuditLogs
            .Include(l => l.Uploader)
            .FirstOrDefaultAsync(l => l.Id == id);
    }
}
