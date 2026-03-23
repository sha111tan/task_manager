using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using task_manager.Database;
using task_manager.Models;

namespace task_manager.Services;

public class TaskService : ITaskService
{
    private readonly TaskManagerDbContext _context;

    public TaskService(TaskManagerDbContext context) => _context = context;

    public async Task<List<TaskItem>> GetTasksForUserAsync(int userId, Role role)
    {
        IQueryable<TaskItem> query = _context.Tasks
            .Include(t => t.Author)
            .Include(t => t.Assignee);

        if (role == Role.User)
        {
            query = query.Where(t => t.AssigneeId == userId);
        }
        else if (role == Role.Manager)
        {
            query = query.Where(t => t.AuthorId == userId);
        }

        return await query.ToListAsync();
    }

    public async Task AddTaskAsync(User currentUser, TaskItem task)
    {
        if (currentUser.Role is not (Role.Manager or Role.Admin))
            throw new InvalidOperationException("Недостаточно прав для создания задачи.");

        if (task.AssigneeId == 0)
            throw new InvalidOperationException("Исполнитель должен быть задан.");

        if (currentUser.Role == Role.Manager)
        {
            var assignee = await _context.Users.FirstOrDefaultAsync(u => u.Id == task.AssigneeId);
            if (assignee == null)
                throw new InvalidOperationException("Некорректный исполнитель.");

            if (assignee.Role != Role.User)
                throw new InvalidOperationException("Руководитель может назначать только сотрудников.");
        }
        else 
        {
            var assignee = await _context.Users.FirstOrDefaultAsync(u => u.Id == task.AssigneeId);
            if (assignee == null)
                throw new InvalidOperationException("Некорректный исполнитель.");
        }

        task.AuthorId = currentUser.Id;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(User currentUser, TaskItem updatedTask)
    {
        var existing = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == updatedTask.Id);
        if (existing == null)
            return;

        if (currentUser.Role == Role.User)
        {
            if (existing.AssigneeId != currentUser.Id)
                throw new InvalidOperationException("Недостаточно прав для изменения этой задачи.");

            existing.Status = updatedTask.Status;
        }
        else if (currentUser.Role == Role.Manager)
        {
            if (existing.AuthorId != currentUser.Id)
                throw new InvalidOperationException("Недостаточно прав для изменения этой задачи.");

            if (updatedTask.AssigneeId == 0)
                throw new InvalidOperationException("Исполнитель должен быть задан.");

            var assignee = await _context.Users.FirstOrDefaultAsync(u => u.Id == updatedTask.AssigneeId);
            if (assignee == null)
                throw new InvalidOperationException("Некорректный исполнитель.");

            if (assignee.Role != Role.User)
                throw new InvalidOperationException("Руководитель может назначать только сотрудников.");

            existing.Title = updatedTask.Title;
            existing.Description = updatedTask.Description;
            existing.AssigneeId = updatedTask.AssigneeId;
            existing.Status = updatedTask.Status;
        }
        else 
        {
            if (updatedTask.AssigneeId == 0)
                throw new InvalidOperationException("Исполнитель должен быть задан.");

            var assignee = await _context.Users.FirstOrDefaultAsync(u => u.Id == updatedTask.AssigneeId);
            if (assignee == null)
                throw new InvalidOperationException("Некорректный исполнитель.");

            existing.Title = updatedTask.Title;
            existing.Description = updatedTask.Description;
            existing.AssigneeId = updatedTask.AssigneeId;
            existing.Status = updatedTask.Status;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteTaskAsync(User currentUser, int id)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (task != null)
        {
            if (currentUser.Role == Role.User)
                throw new InvalidOperationException("Недостаточно прав для удаления задачи.");

            if (currentUser.Role == Role.Manager && task.AuthorId != currentUser.Id)
                throw new InvalidOperationException("Недостаточно прав для удаления этой задачи.");

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<User>> GetPossibleAssigneesAsync(Role currentRole)
    {
        IQueryable<User> query = _context.Users;

        if (currentRole == Role.Manager)
            query = query.Where(u => u.Role == Role.User);

        return await query.ToListAsync();
    }
}