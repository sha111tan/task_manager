using System.Collections.Generic;
using System.Threading.Tasks;
using task_manager.Models;

namespace task_manager.Services;

public interface ITaskService
{
    Task<List<TaskItem>> GetTasksForUserAsync(int userId, Role role);
    Task AddTaskAsync(User currentUser, TaskItem task);
    Task UpdateTaskAsync(User currentUser, TaskItem updatedTask);
    Task DeleteTaskAsync(User currentUser, int id);
    Task<List<User>> GetPossibleAssigneesAsync(Role currentRole);
}