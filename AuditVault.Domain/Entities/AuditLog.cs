namespace AirMark.AuditVault.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UploadedBy { get; set; }
    public string Payload { get; set; } = string.Empty;  // JSON stored as string (SQL Server compatible)
    public string EventType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
    public User Uploader { get; set; } = null!;
}
