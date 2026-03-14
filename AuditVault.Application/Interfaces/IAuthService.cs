using AirMark.AuditVault.Application.DTOs;

namespace AirMark.AuditVault.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
