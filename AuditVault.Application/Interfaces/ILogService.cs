using AirMark.AuditVault.Application.DTOs;

namespace AirMark.AuditVault.Application.Interfaces;

public interface ILogService
{
    Task<LogResponse> CreateLogAsync(CreateLogRequest request);
    Task<PagedResult<LogResponse>> GetLogsAsync(int page, int limit);
    Task<LogResponse?> GetLogByIdAsync(Guid id);
}
