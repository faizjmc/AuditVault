using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using AirMark.AuditVault.Infrastructure.Services;
using System.IO;
namespace AirMark.AuditVault.Infrastructure.Persistence;

/// <summary>
/// Used exclusively by EF Core tools (migrations, scaffolding).
/// Provides a fully configured AppDbContext without needing the full DI container.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Load connection string from the API project's appsettings
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory()))
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly("AuditVault.Infrastructure"));

        // Provide a default tenant service for design-time — no real tenant needed
        var tenantService = new CurrentTenantService();

        return new AppDbContext(optionsBuilder.Options, tenantService);
    }
}