using AirMark.AuditVault.Application.DTOs;
using AirMark.AuditVault.Application.Validators;
using FluentAssertions;
using Xunit;
namespace AirMark.AuditVault.Tests.Services;

public class ValidatorTests
{
    // ── CreateLogRequest ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateLogValidator_ShouldPass_WithValidRequest()
    {
        var validator = new CreateLogRequestValidator();
        var request = new CreateLogRequest("UserLogin", new { userId = "123" });

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateLogValidator_ShouldFail_WhenEventTypeIsEmpty()
    {
        var validator = new CreateLogRequestValidator();
        var request = new CreateLogRequest("", new { userId = "123" });

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EventType");
    }

    [Fact]
    public async Task CreateLogValidator_ShouldFail_WhenEventTypeExceeds100Chars()
    {
        var validator = new CreateLogRequestValidator();
        var request = new CreateLogRequest(new string('A', 101), new { });

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EventType");
    }

    [Fact]
    public async Task CreateLogValidator_ShouldFail_WhenPayloadIsNull()
    {
        var validator = new CreateLogRequestValidator();
        var request = new CreateLogRequest("Login", null!);

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Payload");
    }

    // ── LoginRequest ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginValidator_ShouldPass_WithValidCredentials()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest("user@example.com", "SecurePass1!");

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task LoginValidator_ShouldFail_WithInvalidEmail()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest("not-an-email", "Password123!");

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task LoginValidator_ShouldFail_WithShortPassword()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest("user@example.com", "abc");

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task LoginValidator_ShouldFail_WithEmptyEmail()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest("", "Password123!");

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
