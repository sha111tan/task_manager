using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using task_manager.Models;

namespace task_manager.ViewModels.Dialogs;

public sealed record TaskEditorResult(TaskItem Task);

public partial class TaskEditorViewModel : ObservableObject
{
    public TaskCompletionSource<TaskEditorResult?> ResultTcs { get; } = new();

    public ObservableCollection<User> Assignees { get; } = new();
    public ObservableCollection<TaskStatusOption> StatusOptions { get; } = new();

    private TaskItem? _original;

    public bool CanEditFields => CurrentUser.Role is Role.Manager or Role.Admin;
    public bool CanEditAssignee => CurrentUser.Role is Role.Manager or Role.Admin;
    public bool CanEditStatus => true;

    public bool IsNew => EditingTaskId == 0;

    public User CurrentUser { get; private set; } = null!;
    public int EditingTaskId { get; private set; }

    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private User? selectedAssignee;
    [ObservableProperty] private TaskStatusOption? selectedStatusOption;

    public TaskEditorViewModel()
    {
        // Русские метки статусов для UI.
        foreach (var status in Enum.GetValues<Status>())
        {
            StatusOptions.Add(new TaskStatusOption
            {
                Value = status,
                Label = status switch
                {
                    Status.New => "Новая",
                    Status.InProgress => "В работе",
                    Status.Completed => "Завершена",
                    Status.Cancelled => "Отменена",
                    _ => status.ToString()
                }
            });
        }
    }

    public void Initialize(User currentUser, TaskItem? taskToEdit, User[] assignees)
    {
        CurrentUser = currentUser;
        _original = taskToEdit;

        Assignees.Clear();
        foreach (var a in assignees) Assignees.Add(a);

        if (taskToEdit == null)
        {
            EditingTaskId = 0;
            Title = string.Empty;
            Description = string.Empty;
            SelectedStatusOption = StatusOptions.FirstOrDefault(o => o.Value == Status.New) ?? StatusOptions.FirstOrDefault();
            SelectedAssignee = Assignees.FirstOrDefault();
            return;
        }

        EditingTaskId = taskToEdit.Id;
        Title = taskToEdit.Title;
        Description = taskToEdit.Description;
        SelectedStatusOption = StatusOptions.FirstOrDefault(o => o.Value == taskToEdit.Status) ?? StatusOptions.FirstOrDefault();
        SelectedAssignee = Assignees.FirstOrDefault(u => u.Id == taskToEdit.AssigneeId) ?? Assignees.FirstOrDefault();
    }

    [RelayCommand]
    private void Save()
    {
        if (IsNew && !CanEditFields)
            return;

        if (CanEditFields && string.IsNullOrWhiteSpace(Title))
            return;

        if (IsNew && CanEditAssignee && SelectedAssignee == null)
            return;

        var task = new TaskItem
        {
            Id = EditingTaskId,
            Title = CanEditFields ? Title.Trim() : (_original?.Title ?? Title.Trim()),
            Description = CanEditFields ? Description.Trim() : (_original?.Description ?? Description.Trim()),
            Status = SelectedStatusOption?.Value ?? Status.New,
            CreatedDate = _original?.CreatedDate ?? DateTime.UtcNow,
            AuthorId = _original?.AuthorId ?? 0,
            AssigneeId = _original?.AssigneeId ?? 0
        };

        if (CanEditAssignee && SelectedAssignee != null)
            task.AssigneeId = SelectedAssignee.Id;

        ResultTcs.TrySetResult(new TaskEditorResult(task));
    }

    [RelayCommand]
    private void Cancel() => ResultTcs.TrySetResult(null);
}

