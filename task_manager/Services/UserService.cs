using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BCrypt.Net;
using task_manager.Database;
using task_manager.Models;

namespace task_manager.Services;

public class UserService : IUserService
{
    private readonly TaskManagerDbContext _context;

    public UserService(TaskManagerDbContext context) => _context = context;

    public async Task<List<User>> GetAllUsersAsync() 
        => await _context.Users.ToListAsync();

    public async Task<User?> GetUserByIdAsync(int id) 
        => await _context.Users.FindAsync(id);

    public async Task AddUserAsync(User user, string password)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ChangeRoleAsync(int id, Role newRole)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            user.Role = newRole;
            await _context.SaveChangesAsync();
        }
    }

    public async Task CreateUserAsync(User user)
{
    const string defaultPassword = "password";

    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
    _context.Users.Add(user);

    await _context.SaveChangesAsync();

    
}

    private static void EnsureAdmin(User currentUser)
    {
        if (currentUser.Role != Role.Admin)
            throw new InvalidOperationException("Операция доступна только администратору.");
    }

    public async Task<List<User>> GetAllUsersAsync(User currentUser)
    {
        EnsureAdmin(currentUser);
        return await _context.Users.ToListAsync();
    }

    public async Task AddUserAsync(User currentUser, User user, string password)
    {
        EnsureAdmin(currentUser);
        user.Login = user.Login.Trim();
        if (await _context.Users.AnyAsync(u => u.Login.ToLower() == user.Login.ToLower()))
            throw new InvalidOperationException("Пользователь с таким логином уже существует.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(User currentUser, User user, string? newPassword)
    {
        EnsureAdmin(currentUser);
        user.Login = user.Login.Trim();
        var existing = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (existing == null)
            return;

        if (await _context.Users.AnyAsync(u =>
                u.Id != user.Id &&
                u.Login.ToLower() == user.Login.ToLower()))
            throw new InvalidOperationException("Пользователь с таким логином уже существует.");

        existing.Login = user.Login;
        existing.Role = user.Role;
        if (!string.IsNullOrWhiteSpace(newPassword))
            existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(User currentUser, int id)
    {
        EnsureAdmin(currentUser);
        await DeleteUserAsync(id);
    }
}