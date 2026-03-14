using AirMark.AuditVault.Application.Interfaces;
using AirMark.AuditVault.Domain.Entities;
using AirMark.AuditVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirMark.AuditVault.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
    }

    public async Task<User> CreateAsync(User user)
    {
        user.Email = user.Email.ToLowerInvariant();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
}
