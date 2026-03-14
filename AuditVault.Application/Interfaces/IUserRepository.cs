using AirMark.AuditVault.Domain.Entities;

namespace AirMark.AuditVault.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
}
