using System.Security.Claims;
using AirMark.AuditVault.Application.Interfaces;

namespace AirMark.AuditVault.API.Middleware;

/// <summary>
/// Runs after JWT authentication. Reads tenantId, userId, and role from
/// the validated JWT claims and populates the scoped ICurrentTenantService.
/// This ensures the DbContext global query filter is always set correctly.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? context.User.FindFirstValue("sub");

            var tenantIdClaim = context.User.FindFirstValue("tenantId");
            var roleClaim = context.User.FindFirstValue(ClaimTypes.Role);

            if (Guid.TryParse(userIdClaim, out var userId) &&
                Guid.TryParse(tenantIdClaim, out var tenantId) &&
                !string.IsNullOrEmpty(roleClaim))
            {
                tenantService.SetFromClaims(tenantId, userId, roleClaim);

                _logger.LogDebug(
                    "Tenant context set — TenantId: {TenantId}, UserId: {UserId}, Role: {Role}",
                    tenantId, userId, roleClaim);
            }
            else
            {
                _logger.LogWarning("Authenticated request missing required claims.");
            }
        }

        await _next(context);
    }
}
