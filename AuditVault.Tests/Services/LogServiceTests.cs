using AirMark.AuditVault.Application.DTOs;
using AirMark.AuditVault.Application.Interfaces;
using AirMark.AuditVault.Application.Services;
using AirMark.AuditVault.Domain.Entities;
using AirMark.AuditVault.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
namespace AirMark.AuditVault.Tests.Services;

public class LogServiceTests
{
    private readonly Mock<ILogRepository> _logRepoMock;
    private readonly Mock<ILogger<LogService>> _loggerMock;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ICurrentTenantService _tenantService;
    
    public LogServiceTests()
    {
        _logRepoMock = new Mock<ILogRepository>();
        _loggerMock = new Mock<ILogger<LogService>>();
        _tenantService = new FakeTenantService(_tenantId, _userId, "Auditor");
    }

    private LogService CreateService() =>
        new LogService(_logRepoMock.Object, _tenantService, _loggerMock.Object);

    // ── CreateLog ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateLog_ShouldAssignTenantIdFromContext_NotFromRequest()
    {
        // Arrange
        var request = new CreateLogRequest("UserLogin", new { userId = "123" });
        var service = CreateService();

        _logRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<AuditLog>()))
            .ReturnsAsync((AuditLog log) => log);

        // Act
        var result = await service.CreateLogAsync(request);

        // Assert — TenantId must come from the JWT context, never from caller
        _logRepoMock.Verify(r => r.CreateAsync(
            It.Is<AuditLog>(l => l.TenantId == _tenantId)), Times.Once);

        result.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task CreateLog_ShouldAssignUploadedByFromContext()
    {
        var request = new CreateLogRequest("FileUpload", new { file = "report.pdf" });
        var service = CreateService();

        _logRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<AuditLog>()))
            .ReturnsAsync((AuditLog log) => log);

        var result = await service.CreateLogAsync(request);

        result.UploadedBy.Should().Be(_userId);
    }

    [Fact]
    public async Task CreateLog_ShouldSerializePayloadToJson()
    {
        var request = new CreateLogRequest("DataExport", new { recordCount = 42 });
        var service = CreateService();

        _logRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<AuditLog>()))
            .ReturnsAsync((AuditLog log) => log);

        var result = await service.CreateLogAsync(request);

        result.Payload.Should().Contain("recordCount");
        result.Payload.Should().Contain("42");
    }

    [Fact]
    public async Task CreateLog_ShouldSetEventType()
    {
        var request = new CreateLogRequest("SecurityAlert", new { severity = "high" });
        var service = CreateService();

        _logRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<AuditLog>()))
            .ReturnsAsync((AuditLog log) => log);

        var result = await service.CreateLogAsync(request);

        result.EventType.Should().Be("SecurityAlert");
    }

    // ── GetLogs ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLogs_ShouldReturnOnlyLogsForCurrentTenant()
    {
        var service = CreateService();
        var logs = new List<AuditLog>
        {
            new() { Id = Guid.NewGuid(), TenantId = _tenantId, EventType = "Login",
                    Payload = "{}", UploadedBy = _userId, CreatedAt = DateTime.UtcNow,
                    Uploader = new User { Email = "auditor@acme.com" } }
        };

        _logRepoMock
            .Setup(r => r.GetByTenantAsync(_tenantId, 1, 20))
            .ReturnsAsync((logs, 1));

        var result = await service.GetLogsAsync(1, 20);

        result.Items.Should().HaveCount(1);
        result.Items.First().TenantId.Should().Be(_tenantId);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLogs_ShouldClampLimitAbove100()
    {
        var service = CreateService();

        _logRepoMock
            .Setup(r => r.GetByTenantAsync(_tenantId, 1, 20))
            .ReturnsAsync((new List<AuditLog>(), 0));

        // Passing limit=500 should be clamped to 20 (default)
        await service.GetLogsAsync(1, 500);

        _logRepoMock.Verify(r => r.GetByTenantAsync(_tenantId, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetLogs_ShouldNormalizePage_WhenBelowOne()
    {
        var service = CreateService();

        _logRepoMock
            .Setup(r => r.GetByTenantAsync(_tenantId, 1, 20))
            .ReturnsAsync((new List<AuditLog>(), 0));

        await service.GetLogsAsync(0, 20);

        _logRepoMock.Verify(r => r.GetByTenantAsync(_tenantId, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetLogs_ShouldReturnCorrectPaginationMetadata()
    {
        var service = CreateService();
        var logs = Enumerable.Range(1, 5).Select(i => new AuditLog
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, EventType = "Event",
            Payload = "{}", UploadedBy = _userId, CreatedAt = DateTime.UtcNow,
            Uploader = new User { Email = "test@test.com" }
        }).ToList();

        _logRepoMock
            .Setup(r => r.GetByTenantAsync(_tenantId, 2, 5))
            .ReturnsAsync((logs, 25));

        var result = await service.GetLogsAsync(2, 5);

        result.Page.Should().Be(2);
        result.Limit.Should().Be(5);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(5);
    }

    // ── GetLogById ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLogById_ShouldReturnLog_WhenBelongsToTenant()
    {
        var logId = Guid.NewGuid();
        var service = CreateService();
        var log = new AuditLog
        {
            Id = logId, TenantId = _tenantId, EventType = "Login",
            Payload = "{}", UploadedBy = _userId, CreatedAt = DateTime.UtcNow,
            Uploader = new User { Email = "auditor@acme.com" }
        };

        _logRepoMock
            .Setup(r => r.GetByIdAndTenantAsync(logId, _tenantId))
            .ReturnsAsync(log);

        var result = await service.GetLogByIdAsync(logId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(logId);
        result.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task GetLogById_ShouldReturnNull_WhenLogNotFound()
    {
        var logId = Guid.NewGuid();
        var service = CreateService();

        _logRepoMock
            .Setup(r => r.GetByIdAndTenantAsync(logId, _tenantId))
            .ReturnsAsync((AuditLog?)null);

        var result = await service.GetLogByIdAsync(logId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLogById_ShouldReturnNull_WhenLogBelongsToDifferentTenant()
    {
        // This simulates the global query filter returning null for cross-tenant access
        var logId = Guid.NewGuid();
        var service = CreateService();

        _logRepoMock
            .Setup(r => r.GetByIdAndTenantAsync(logId, _tenantId))
            .ReturnsAsync((AuditLog?)null);  // DB filter returns null — not found for this tenant

        var result = await service.GetLogByIdAsync(logId);

        result.Should().BeNull();
    }
}
