using AirMark.AuditVault.Application.Interfaces;
using AirMark.AuditVault.Domain.Entities;
using AirMark.AuditVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AirMark.AuditVault.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ICurrentTenantService _tenantService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenantService tenantService)
        : base(options)
    {
        _tenantService = tenantService;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Tenant ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(255);
            e.HasIndex(t => t.Name).IsUnique();
            e.Property(t => t.CreatedAt)
             .HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()");
        });

        // ── User ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(255);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).HasConversion<string>();

            e.Property(u => u.CreatedAt)
             .HasColumnType("datetime2")
             .HasDefaultValueSql("GETUTCDATE()");

            e.HasOne(u => u.Tenant)
             .WithMany(t => t.Users)
             .HasForeignKey(u => u.TenantId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── AuditLog ──────────────────────────────────────────────────────────
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.EventType).IsRequired().HasMaxLength(100);
            e.Property(l => l.Payload).IsRequired().HasColumnType("nvarchar(max)");
            e.HasIndex(l => l.TenantId);
            e.HasIndex(l => l.CreatedAt);

            e.HasOne(l => l.Tenant)
             .WithMany(t => t.Logs)
             .HasForeignKey(l => l.TenantId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(l => l.Uploader)
             .WithMany(u => u.UploadedLogs)
             .HasForeignKey(l => l.UploadedBy)
             .OnDelete(DeleteBehavior.Restrict);

            // ⚠️ KEY SECURITY FEATURE: Global query filter — automatically scopes
            // ALL AuditLog queries to the current tenant. It is impossible to
            // accidentally retrieve logs from another tenant.
            e.HasQueryFilter(l => l.TenantId == _tenantService.TenantId);
        });

        // ── Seed Data ─────────────────────────────────────────────────────────
        SeedData(modelBuilder);
    }

    //private static void SeedData(ModelBuilder modelBuilder)
    //{
    //    var tenant1Id = new Guid("11111111-1111-1111-1111-111111111111");
    //    var tenant2Id = new Guid("22222222-2222-2222-2222-222222222222");

    //    modelBuilder.Entity<Tenant>().HasData(
    //        new Tenant { Id = tenant1Id, Name = "CCN Pte Ltd" },
    //        new Tenant { Id = tenant2Id, Name = "DHL Corp" }
    //    );

    //    // Password for all seed users: "Password123!"
    //    var hash = BCrypt.Net.BCrypt.HashPassword("Password123!");

    //    modelBuilder.Entity<User>().HasData(
    //        new User
    //        {
    //            Id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
    //            TenantId = tenant1Id,
    //            Email = "admin@acme.com",
    //            PasswordHash = hash,
    //            Role = UserRole.Admin
    //        },
    //        new User
    //        {
    //            Id = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
    //            TenantId = tenant1Id,
    //            Email = "auditor@acme.com",
    //            PasswordHash = hash,
    //            Role = UserRole.Auditor
    //        },
    //        new User
    //        {
    //            Id = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
    //            TenantId = tenant2Id,
    //            Email = "admin@globex.com",
    //            PasswordHash = hash,
    //            Role = UserRole.Admin
    //        },
    //        new User
    //        {
    //            Id = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
    //            TenantId = tenant2Id,
    //            Email = "auditor@globex.com",
    //            PasswordHash = hash,
    //            Role = UserRole.Auditor
    //        }
    //    );
    //}
    private static void SeedData(ModelBuilder modelBuilder)
    {
        var tenant1Id = new Guid("11111111-1111-1111-1111-111111111111");
        var tenant2Id = new Guid("22222222-2222-2222-2222-222222222222");

        var now = DateTime.UtcNow;

        modelBuilder.Entity<Tenant>().HasData(
            new Tenant { Id = tenant1Id, Name = "CCN Pte Ltd", CreatedAt = now },
            new Tenant { Id = tenant2Id, Name = "DHL Corp", CreatedAt = now }
        );

        // Generate hash at migration creation time — guaranteed to match Password123!
        var hash = BCrypt.Net.BCrypt.HashPassword("Password123!", workFactor: 11);

        modelBuilder.Entity<User>().HasData(
            new User { Id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), TenantId = tenant1Id, Email = "admin@ccn.com", PasswordHash = hash, Role = UserRole.Admin, CreatedAt = now },
            new User { Id = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), TenantId = tenant1Id, Email = "auditor@ccn.com", PasswordHash = hash, Role = UserRole.Auditor, CreatedAt = now },
            new User { Id = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), TenantId = tenant2Id, Email = "admin@dhl.com", PasswordHash = hash, Role = UserRole.Admin, CreatedAt = now },
            new User { Id = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), TenantId = tenant2Id, Email = "auditor@dhl.com", PasswordHash = hash, Role = UserRole.Auditor, CreatedAt = now }
        );
    }
}
