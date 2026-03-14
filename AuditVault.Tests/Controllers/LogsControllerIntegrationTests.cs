using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AirMark.AuditVault.Application.DTOs;
using AirMark.AuditVault.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AirMark.AuditVault.Tests.Controllers;

/// <summary>
/// Integration tests that spin up the full API pipeline in-memory.
/// These test the complete request → middleware → controller → service flow.
/// </summary>
public class LogsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    // Seed data IDs from the migration
    private static readonly Guid AcmeTenantId  = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid GlobexTenantId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AcmeAuditorId  = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid AcmeAdminId    = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid GlobexAuditorId = new("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public LogsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithToken(Guid userId, Guid tenantId, string role)
    {
        var client = _factory.CreateClient();
        var token = JwtTestFactory.GenerateToken(userId, tenantId, role);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ── POST /logs ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostLogs_ShouldReturn201_WhenAuditorUploadsLog()
    {
        var client = CreateClientWithToken(AcmeAuditorId, AcmeTenantId, "Auditor");
        var payload = new { EventType = "UserLogin", Payload = new { userId = "test-123" } };

        var response = await client.PostAsJsonAsync("/logs", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<LogResponse>();
        body.Should().NotBeNull();
        body!.TenantId.Should().Be(AcmeTenantId);
        body.EventType.Should().Be("UserLogin");
    }

    [Fact]
    public async Task PostLogs_ShouldReturn403_WhenAdminTriesToUpload()
    {
        var client = CreateClientWithToken(AcmeAdminId, AcmeTenantId, "Admin");
        var payload = new { EventType = "UserLogin", Payload = new { userId = "test-123" } };

        var response = await client.PostAsJsonAsync("/logs", payload);

        // Admins cannot POST — RBAC must reject with 403
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostLogs_ShouldReturn401_WhenNoTokenProvided()
    {
        var client = _factory.CreateClient();
        var payload = new { EventType = "UserLogin", Payload = new { } };

        var response = await client.PostAsJsonAsync("/logs", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostLogs_ShouldReturn400_WhenEventTypeIsMissing()
    {
        var client = CreateClientWithToken(AcmeAuditorId, AcmeTenantId, "Auditor");
        var payload = new { EventType = "", Payload = new { userId = "test" } };

        var response = await client.PostAsJsonAsync("/logs", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /logs ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLogs_ShouldReturn200_WithPaginatedResults()
    {
        var client = CreateClientWithToken(AcmeAuditorId, AcmeTenantId, "Auditor");

        var response = await client.GetAsync("/logs?page=1&limit=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("page").GetInt32().Should().Be(1);
        body.GetProperty("limit").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task GetLogs_ShouldReturn200_ForAdminRole()
    {
        var client = CreateClientWithToken(AcmeAdminId, AcmeTenantId, "Admin");

        var response = await client.GetAsync("/logs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLogs_ShouldReturn401_WhenUnauthenticated()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/logs");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLogs_ShouldReturn400_WhenLimitExceeds100()
    {
        var client = CreateClientWithToken(AcmeAuditorId, AcmeTenantId, "Auditor");

        var response = await client.GetAsync("/logs?page=1&limit=500");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /logs/{id} ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLogById_ShouldReturn404_ForNonExistentLog()
    {
        var client = CreateClientWithToken(AcmeAuditorId, AcmeTenantId, "Auditor");

        var response = await client.GetAsync($"/logs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetLogById_ShouldReturn401_WhenUnauthenticated()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/logs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Tenant Isolation ───────────────────────────────────────────────────────

    [Fact]
    public async Task TenantIsolation_AcmeUserCannotSeeGlobexLogs()
    {
        // Step 1: Globex auditor creates a log
        var globexClient = CreateClientWithToken(GlobexAuditorId, GlobexTenantId, "Auditor");
        var createResponse = await globexClient.PostAsJsonAsync("/logs",
            new { EventType = "GlobexEvent", Payload = new { secret = "globex-data" } });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdLog = await createResponse.Content.ReadFromJsonAsync<LogResponse>();

        // Step 2: Acme auditor tries to access Globex's log by ID
        var acmeClient = CreateClientWithToken(AcmeAuditorId, AcmeTenantId, "Auditor");
        var getResponse = await acmeClient.GetAsync($"/logs/{createdLog!.Id}");

        // Must return 404 — cross-tenant access is not permitted
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
