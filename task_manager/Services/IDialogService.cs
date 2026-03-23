using System.Threading.Tasks;
using task_manager.Models;
using task_manager.ViewModels.Dialogs;

namespace task_manager.Services;

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message);

    Task<TaskEditorResult?> EditTaskAsync(User currentUser, TaskItem? taskToEdit);

    Task<bool> ShowTaskDetailsAsync(User currentUser, TaskItem task);

    Task<UserEditorOutcome?> EditUserAsync(User currentUser, User? userToEdit);
}

