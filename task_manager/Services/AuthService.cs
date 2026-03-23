using task_manager.Database;
using task_manager.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace task_manager.Services;

public class AuthService : IAuthService
{
    private readonly TaskManagerDbContext _context;

    public AuthService(TaskManagerDbContext context) => _context = context;

    public async Task<User?> LoginAsync(string login, string password)  
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
        if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return user;

        return null;
    }
}