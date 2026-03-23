using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using task_manager.Models;
using task_manager.Services;

namespace task_manager.ViewModels.Dialogs;

public partial class TaskDetailsViewModel : ObservableObject
{
    private readonly ITaskService _taskService;
    private User? _user;

    public TaskCompletionSource<bool> ResultTcs { get; } = new();

    public bool WasModified { get; private set; }

    public IReadOnlyList<TaskStatusOption> StatusOptions { get; } =
    [
        new() { Value = Status.New, Label = "Новая" },
        new() { Value = Status.InProgress, Label = "В работе" },
        new() { Value = Status.Completed, Label = "Завершена" },
        new() { Value = Status.Cancelled, Label = "Отменена" }
    ];
    public ObservableCollection<User> PossibleAssignees { get; } = new();
    public bool CanManageAssigneeAndDelete => _user?.Role is Role.Manager or Role.Admin;

    /// <summary>Поля названия и описания только для чтения (роль User).</summary>
    public bool IsTitleDescriptionReadOnly => !CanManageAssigneeAndDelete;

    [ObservableProperty] private TaskItem currentTask = null!;
    [ObservableProperty] private string editableTitle = string.Empty;
    [ObservableProperty] private string editableDescription = string.Empty;
    [ObservableProperty] private TaskStatusOption? selectedStatusOption;
    [ObservableProperty] private User? selectedAssignee;
    [ObservableProperty] private string operationError = string.Empty;

    public TaskDetailsViewModel(ITaskService taskService) => _taskService = taskService;

    public async Task InitializeAsync(User user, TaskItem task)
    {
        _user = user;
        WasModified = false;
        OperationError = string.Empty;
        CurrentTask = task;
        EditableTitle = task.Title;
        EditableDescription = task.Description;
        SelectedStatusOption = StatusOptions.First(o => o.Value == task.Status);
        PossibleAssignees.Clear();
        var assignees = await _taskService.GetPossibleAssigneesAsync(user.Role);
        foreach (var assignee in assignees)
            PossibleAssignees.Add(assignee);
        SelectedAssignee = PossibleAssignees.FirstOrDefault(a => a.Id == task.AssigneeId);
        OnPropertyChanged(nameof(CanManageAssigneeAndDelete));
        OnPropertyChanged(nameof(IsTitleDescriptionReadOnly));
        SaveChangesCommand.NotifyCanExecuteChanged();
        DeleteTaskCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSaveChanges))]
    private async Task SaveChangesAsync()
    {
        if (_user == null || CurrentTask == null || SelectedStatusOption == null) return;

        if (CanManageAssigneeAndDelete && string.IsNullOrWhiteSpace(EditableTitle))
        {
            OperationError = "Укажите название задачи.";
            return;
        }

        var prevStatus = CurrentTask.Status;
        var prevAssigneeId = CurrentTask.AssigneeId;
        var prevAssignee = CurrentTask.Assignee;
        var prevTitle = CurrentTask.Title;
        var prevDesc = CurrentTask.Description;

        OperationError = string.Empty;

        try
        {
            CurrentTask.Status = SelectedStatusOption.Value;

            if (CanManageAssigneeAndDelete)
            {
                CurrentTask.Title = EditableTitle.Trim();
                CurrentTask.Description = EditableDescription ?? string.Empty;
                if (SelectedAssignee != null)
                {
                    CurrentTask.AssigneeId = SelectedAssignee.Id;
                    CurrentTask.Assignee = SelectedAssignee;
                }
            }

            await _taskService.UpdateTaskAsync(_user, CurrentTask);
            WasModified = true;
            ResultTcs.TrySetResult(true);
            SaveChangesCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            CurrentTask.Status = prevStatus;
            CurrentTask.AssigneeId = prevAssigneeId;
            CurrentTask.Assignee = prevAssignee;
            CurrentTask.Title = prevTitle;
            CurrentTask.Description = prevDesc;
            EditableTitle = prevTitle;
            EditableDescription = prevDesc;
            SelectedStatusOption = StatusOptions.First(o => o.Value == prevStatus);
            SelectedAssignee = PossibleAssignees.FirstOrDefault(a => a.Id == prevAssigneeId);
            OperationError = ex.Message;
            SaveChangesCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanSaveChanges()
    {
        if (_user == null || CurrentTask == null) return false;
        if (SelectedStatusOption == null) return false;

        if (SelectedStatusOption.Value != CurrentTask.Status)
            return true;

        if (CanManageAssigneeAndDelete)
        {
            if (string.IsNullOrWhiteSpace(EditableTitle))
                return false;

            if (EditableTitle.Trim() != CurrentTask.Title)
                return true;

            if ((EditableDescription ?? string.Empty) != CurrentTask.Description)
                return true;

            if (SelectedAssignee != null && SelectedAssignee.Id != CurrentTask.AssigneeId)
                return true;
        }

        return false;
    }

    partial void OnEditableTitleChanged(string value) =>
        SaveChangesCommand.NotifyCanExecuteChanged();

    partial void OnEditableDescriptionChanged(string value) =>
        SaveChangesCommand.NotifyCanExecuteChanged();

    partial void OnSelectedStatusOptionChanged(TaskStatusOption? value) =>
        SaveChangesCommand.NotifyCanExecuteChanged();

    partial void OnSelectedAssigneeChanged(User? value) =>
        SaveChangesCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanDeleteTask))]
    private async Task DeleteTaskAsync()
    {
        if (_user == null || CurrentTask == null) return;
        try
        {
            OperationError = string.Empty;
            await _taskService.DeleteTaskAsync(_user, CurrentTask.Id);
            WasModified = true;
            ResultTcs.TrySetResult(true);
        }
        catch (Exception ex)
        {
            OperationError = ex.Message;
        }
    }

    private bool CanDeleteTask() => CanManageAssigneeAndDelete && CurrentTask != null;

    [RelayCommand]
    private void Close() => ResultTcs.TrySetResult(true);
}
