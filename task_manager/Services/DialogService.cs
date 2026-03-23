using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using task_manager.Models;
using task_manager.ViewModels.Dialogs;
using task_manager.Views;

namespace task_manager.Services;

public class DialogService : IDialogService
{
    private readonly IServiceProvider _services;

    public DialogService(IServiceProvider services) => _services = services;

    private static Window? GetOwner()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var vm = _services.GetRequiredService<ConfirmDialogViewModel>();
        vm.Title = title;
        vm.Message = message;

        var wnd = new ConfirmDialog(vm);
        wnd.Title = title;

        var owner = GetOwner();
        if (owner != null)
            await wnd.ShowDialog(owner);
        else
            wnd.Show();

        return await vm.ResultTcs.Task;
    }

    public async Task<TaskEditorResult?> EditTaskAsync(User currentUser, TaskItem? taskToEdit)
    {
        var vm = _services.GetRequiredService<TaskEditorViewModel>();

        var assignees = _services.GetRequiredService<ITaskService>();
        var possible = await assignees.GetPossibleAssigneesAsync(currentUser.Role);
        vm.Initialize(currentUser, taskToEdit, possible.ToArray());

        var wnd = new TaskEditorWindow(vm);
        wnd.Title = taskToEdit == null ? "Создание задачи" : "Редактирование задачи";

        var owner = GetOwner();
        if (owner != null)
            await wnd.ShowDialog(owner);
        else
            wnd.Show();

        return await vm.ResultTcs.Task;
    }

    public async Task<bool> ShowTaskDetailsAsync(User currentUser, TaskItem task)
    {
        var vm = _services.GetRequiredService<TaskDetailsViewModel>();
        await vm.InitializeAsync(currentUser, task);

        var wnd = new TaskDetailsWindow(vm);

        var owner = GetOwner();
        if (owner != null)
            await wnd.ShowDialog(owner);
        else
        {
            wnd.Show();
            var closed = new TaskCompletionSource();
            wnd.Closed += (_, _) => closed.TrySetResult();
            await closed.Task;
        }

        return vm.WasModified;
    }

    public async Task<UserEditorOutcome?> EditUserAsync(User currentUser, User? userToEdit)
    {
        var vm = _services.GetRequiredService<UserEditorViewModel>();
        vm.Initialize(currentUser, userToEdit);

        var wnd = new UserEditorWindow(vm);
        wnd.Title = userToEdit == null ? "Создание пользователя" : "Редактирование пользователя";

        var owner = GetOwner();
        if (owner != null)
            await wnd.ShowDialog(owner);
        else
            wnd.Show();

        return await vm.ResultTcs.Task;
    }
}

