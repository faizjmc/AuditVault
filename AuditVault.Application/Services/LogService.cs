using System.Text.Json;
using AirMark.AuditVault.Application.DTOs;
using AirMark.AuditVault.Application.Interfaces;
using AirMark.AuditVault.Domain.Entities;
using Microsoft.Extensions;
using Microsoft.Extensions.Logging;

namespace AirMark.AuditVault.Application.Services;

public class LogService : ILogService
{
    private readonly ILogRepository _logRepository;
    private readonly ICurrentTenantService _currentTenant;
    private readonly ILogger<LogService> _logger;

    public LogService(
        ILogRepository logRepository,
        ICurrentTenantService currentTenant,
        ILogger<LogService> logger)
    {
        _logRepository = logRepository;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<LogResponse> CreateLogAsync(CreateLogRequest request)
    {
        var payloadJson = JsonSerializer.Serialize(request.Payload);

        var log = new AuditLog
        {
            TenantId = _currentTenant.TenantId,
            UploadedBy = _currentTenant.UserId,
            EventType = request.EventType,
            Payload = payloadJson,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _logRepository.CreateAsync(log);

        _logger.LogInformation(
            "Log {LogId} created by user {UserId} for tenant {TenantId}",
            created.Id, _currentTenant.UserId, _currentTenant.TenantId);

        return MapToResponse(created, null);
    }

    public async Task<PagedResult<LogResponse>> GetLogsAsync(int page, int limit)
    {
        if (page < 1) page = 1;
        if (limit < 1 || limit > 100) limit = 20;

        var (items, totalCount) = await _logRepository.GetByTenantAsync(
            _currentTenant.TenantId, page, limit);

        _logger.LogInformation(
            "Logs retrieved for tenant {TenantId}: page {Page}, limit {Limit}",
            _currentTenant.TenantId, page, limit);

        var responses = items.Select(l => MapToResponse(l, l.Uploader?.Email));

        return new PagedResult<LogResponse>(responses, page, limit, totalCount);
    }

    public async Task<LogResponse?> GetLogByIdAsync(Guid id)
    {
        var log = await _logRepository.GetByIdAndTenantAsync(id, _currentTenant.TenantId);

        if (log is null)
        {
            _logger.LogWarning(
                "Log {LogId} not found or does not belong to tenant {TenantId}",
                id, _currentTenant.TenantId);
            return null;
        }

        _logger.LogInformation(
            "Log {LogId} retrieved for tenant {TenantId}",
            id, _currentTenant.TenantId);

        return MapToResponse(log, log.Uploader?.Email);
    }

    private static LogResponse MapToResponse(AuditLog log, string? uploaderEmail) =>
        new(log.Id, log.TenantId, log.UploadedBy,
            uploaderEmail ?? string.Empty, log.EventType,
            log.Payload, log.CreatedAt);
}
