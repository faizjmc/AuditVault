namespace AirMark.AuditVault.Application.DTOs;

// Auth
public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string Email, string Role, Guid TenantId);

// Logs
public record CreateLogRequest(string EventType, object Payload);

public record LogResponse(
    Guid Id,
    Guid TenantId,
    Guid UploadedBy,
    string UploaderEmail,
    string EventType,
    string Payload,
    DateTime CreatedAt);

// Pagination
public record PagedResult<T>(
    IEnumerable<T> Items,
    int Page,
    int Limit,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Limit);
}
