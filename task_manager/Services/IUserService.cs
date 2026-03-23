using System.Collections.Generic;
using System.Threading.Tasks;
using task_manager.Models;

namespace task_manager.Services;

public interface IUserService
{
    Task<List<User>> GetAllUsersAsync(User currentUser);
    Task AddUserAsync(User currentUser, User user, string password);
    Task UpdateUserAsync(User currentUser, User user, string? newPassword);
    Task DeleteUserAsync(User currentUser, int id);
}