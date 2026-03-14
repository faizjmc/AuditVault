using AirMark.AuditVault.Application.DTOs;
using AirMark.AuditVault.Application.Interfaces;
using AirMark.AuditVault.Application.Services;
using AirMark.AuditVault.Domain.Entities;
using AirMark.AuditVault.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
namespace AirMark.AuditVault.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly IConfiguration _configuration;

    public AuthServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();

        var configValues = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"]      = "test-secret-key-that-is-long-enough-32chars!",
            ["JwtSettings:Issuer"]         = "AuditVaultAPI",
            ["JwtSettings:Audience"]       = "AuditVaultClients",
            ["JwtSettings:ExpiryMinutes"]  = "60"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
    }

    private AuthService CreateService() =>
        new AuthService(_userRepoMock.Object, _configuration);

    [Fact]
    public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
    {
        var tenantId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "auditor@acme.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Auditor
        };

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("auditor@acme.com"))
            .ReturnsAsync(user);

        var service = CreateService();
        var result = await service.LoginAsync(new LoginRequest("auditor@acme.com", "Password123!"));

        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("auditor@acme.com");
        result.Role.Should().Be("Auditor");
        result.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task Login_ShouldReturnNull_WhenEmailNotFound()
    {
        _userRepoMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var service = CreateService();
        var result = await service.LoginAsync(new LoginRequest("unknown@acme.com", "Password123!"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_ShouldReturnNull_WhenPasswordIsWrong()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Email = "admin@acme.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword!"),
            Role = UserRole.Admin
        };

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("admin@acme.com"))
            .ReturnsAsync(user);

        var service = CreateService();
        var result = await service.LoginAsync(new LoginRequest("admin@acme.com", "WrongPassword!"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_ShouldReturnCorrectRole_ForAdmin()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Email = "admin@acme.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Admin
        };

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("admin@acme.com"))
            .ReturnsAsync(user);

        var service = CreateService();
        var result = await service.LoginAsync(new LoginRequest("admin@acme.com", "Password123!"));

        result.Should().NotBeNull();
        result!.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_GeneratedToken_ShouldBeValidJwt()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Email = "auditor@acme.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Auditor
        };

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("auditor@acme.com"))
            .ReturnsAsync(user);

        var service = CreateService();
        var result = await service.LoginAsync(new LoginRequest("auditor@acme.com", "Password123!"));

        // A valid JWT has 3 base64 parts separated by dots
        result!.Token.Split('.').Should().HaveCount(3);
    }
}
