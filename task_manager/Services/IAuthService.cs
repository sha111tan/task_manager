using task_manager.Models;
using System.Threading.Tasks;

namespace task_manager.Services;

public interface IAuthService
{
    Task<User?> LoginAsync(string login, string password);
}