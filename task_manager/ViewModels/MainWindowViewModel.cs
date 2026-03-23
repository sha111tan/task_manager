using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using task_manager.Models;
using task_manager.Services;
using task_manager.ViewModels.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace task_manager.ViewModels;

/// <summary>Вариант в комбобоксе фильтра по исполнителю (null = все).</summary>
public sealed class AssigneeFilterOption
{
    public int? AssigneeId { get; }
    public string Label { get; }

    public AssigneeFilterOption(int? assigneeId, string label)
    {
        AssigneeId = assigneeId;
        Label = label;
    }
}

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ITaskService _taskService;
    private readonly IUserService _userService;
    private readonly IDialogService _dialogService;
    private User? _currentUser;

    private List<TaskItem> _tasksRaw = new();
    private List<TaskItem> _allTasksRaw = new();
    private List<User> _allUsersRaw = new();

    // Общие коллекции
    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<TaskItem> AllTasks { get; } = new();

    // Kanban (для "Мои / Назначенные задачи")
    public ObservableCollection<TaskItem> NewTasks { get; } = new();
    public ObservableCollection<TaskItem> InProgressTasks { get; } = new();
    public ObservableCollection<TaskItem> CompletedTasks { get; } = new();
    public ObservableCollection<TaskItem> CancelledTasks { get; } = new();

    // Kanban (для "Все задачи" - admin)
    public ObservableCollection<TaskItem> AllNewTasks { get; } = new();
    public ObservableCollection<TaskItem> AllInProgressTasks { get; } = new();
    public ObservableCollection<TaskItem> AllCompletedTasks { get; } = new();
    public ObservableCollection<TaskItem> AllCancelledTasks { get; } = new();

    public ObservableCollection<User> AllUsers { get; } = new();
    public ObservableCollection<User> PossibleAssignees { get; } = new();
    public ObservableCollection<AssigneeFilterOption> AssigneeFilterOptions { get; } = new();

    [ObservableProperty] private TaskItem? selectedTask;
    [ObservableProperty] private User? selectedUser;
    [ObservableProperty] private string searchText = string.Empty;      // поиск пользователей (админ)
    [ObservableProperty] private string taskSearchText = string.Empty;  // поиск задач (админ)
    [ObservableProperty] private string operationError = string.Empty;

    [ObservableProperty] private DateTimeOffset? filterDateFrom;
    [ObservableProperty] private DateTimeOffset? filterDateTo;
    [ObservableProperty] private AssigneeFilterOption? selectedAssigneeFilter;

    // Фильтры и права
    public bool IsAdmin => _currentUser?.Role == Role.Admin;
    public bool IsManagerOrAdmin => _currentUser?.Role == Role.Manager || IsAdmin;
    public bool CanCreateTask => IsManagerOrAdmin;
    public bool CanEditTask => IsManagerOrAdmin;
    public bool CanChangeStatus => _currentUser != null;
    public bool CanDeleteTask => IsManagerOrAdmin;

    public MainWindowViewModel(ITaskService taskService, IUserService userService, IDialogService dialogService)
    {
        _taskService = taskService;
        _userService = userService;
        _dialogService = dialogService;
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SelectTask(TaskItem? task)
    {
        if (task == null || _currentUser == null) return;
        SelectedTask = task;
        var changed = await _dialogService.ShowTaskDetailsAsync(_currentUser, task);
        if (changed)
            await LoadDataAsync();
    }

    public async Task InitializeAsync(User user)
    {
        _currentUser = user;
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(IsManagerOrAdmin));
        OnPropertyChanged(nameof(CanCreateTask));
        OnPropertyChanged(nameof(CanEditTask));
        OnPropertyChanged(nameof(CanChangeStatus));
        OnPropertyChanged(nameof(CanDeleteTask));

        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (_currentUser == null) return;

        OperationError = string.Empty;
        _tasksRaw = await _taskService.GetTasksForUserAsync(_currentUser.Id, _currentUser.Role);
        ApplyTaskKanban();

        if (IsAdmin)
        {
            _allUsersRaw = await _userService.GetAllUsersAsync(_currentUser);
            ApplyUserFilters();

            _allTasksRaw = await _taskService.GetTasksForUserAsync(0, Role.Admin);
            ApplyAllTasksKanban();
        }

        PossibleAssignees.Clear();
        var assignees = await _taskService.GetPossibleAssigneesAsync(_currentUser.Role);
        foreach (var a in assignees) PossibleAssignees.Add(a);

        RebuildAssigneeFilterOptions();
    }

    partial void OnSearchTextChanged(string value) => ApplyUserFilters();

    partial void OnTaskSearchTextChanged(string value) => ApplyFiltersToKanban();

    partial void OnFilterDateFromChanged(DateTimeOffset? value) => ApplyFiltersToKanban();

    partial void OnFilterDateToChanged(DateTimeOffset? value) => ApplyFiltersToKanban();

    partial void OnSelectedAssigneeFilterChanged(AssigneeFilterOption? value) => ApplyFiltersToKanban();

    private void RebuildAssigneeFilterOptions()
    {
        int? prevId = SelectedAssigneeFilter?.AssigneeId;
        AssigneeFilterOptions.Clear();
        AssigneeFilterOptions.Add(new AssigneeFilterOption(null, "Все"));
        foreach (var u in PossibleAssignees.OrderBy(x => x.Login, StringComparer.OrdinalIgnoreCase))
            AssigneeFilterOptions.Add(new AssigneeFilterOption(u.Id, u.Login));

        SelectedAssigneeFilter = prevId.HasValue
            ? AssigneeFilterOptions.FirstOrDefault(o => o.AssigneeId == prevId)
            : AssigneeFilterOptions[0];
        if (SelectedAssigneeFilter == null)
            SelectedAssigneeFilter = AssigneeFilterOptions[0];
    }

    private void ApplyFiltersToKanban()
    {
        ApplyTaskKanban();
        if (IsAdmin)
            ApplyAllTasksKanban();
    }

    private bool TaskMatchesFilters(TaskItem t)
    {
        if (!IsManagerOrAdmin)
            return true;

        if (!string.IsNullOrWhiteSpace(TaskSearchText))
        {
            var s = TaskSearchText.Trim();
            if (!t.Title.Contains(s, StringComparison.OrdinalIgnoreCase) &&
                !t.Description.Contains(s, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (SelectedAssigneeFilter?.AssigneeId is int assigneeId && t.AssigneeId != assigneeId)
            return false;

        var localDate = t.CreatedDate.ToLocalTime().Date;

        if (FilterDateFrom.HasValue && localDate < FilterDateFrom.Value.Date)
            return false;
        if (FilterDateTo.HasValue && localDate > FilterDateTo.Value.Date)
            return false;

        return true;
    }

    private void ApplyTaskKanban()
    {
        Tasks.Clear();
        NewTasks.Clear();
        InProgressTasks.Clear();
        CompletedTasks.Clear();
        CancelledTasks.Clear();

        foreach (var t in _tasksRaw.Where(TaskMatchesFilters))
        {
            Tasks.Add(t);
            switch (t.Status)
            {
                case Status.New:
                    NewTasks.Add(t);
                    break;
                case Status.InProgress:
                    InProgressTasks.Add(t);
                    break;
                case Status.Completed:
                    CompletedTasks.Add(t);
                    break;
                case Status.Cancelled:
                    CancelledTasks.Add(t);
                    break;
            }
        }
    }

    private void ApplyUserFilters()
    {
        AllUsers.Clear();
        IEnumerable<User> query = _allUsersRaw;

        if (!string.IsNullOrWhiteSpace(SearchText))
            query = query.Where(u => u.Login.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var u in query) AllUsers.Add(u);
    }

    private void ApplyAllTasksKanban()
    {
        AllTasks.Clear();
        AllNewTasks.Clear();
        AllInProgressTasks.Clear();
        AllCompletedTasks.Clear();
        AllCancelledTasks.Clear();

        foreach (var t in _allTasksRaw.Where(TaskMatchesFilters))
        {
            AllTasks.Add(t);
            switch (t.Status)
            {
                case Status.New:
                    AllNewTasks.Add(t);
                    break;
                case Status.InProgress:
                    AllInProgressTasks.Add(t);
                    break;
                case Status.Completed:
                    AllCompletedTasks.Add(t);
                    break;
                case Status.Cancelled:
                    AllCancelledTasks.Add(t);
                    break;
            }
        }
    }

    [RelayCommand]
    private async Task NewTaskAsync()
    {
        if (_currentUser == null || !CanCreateTask) return;

        try
        {
            OperationError = string.Empty;
            var result = await _dialogService.EditTaskAsync(_currentUser, null);
            if (result == null) return;

            if (result.Task.AssigneeId == 0)
                return;

            var task = new TaskItem
            {
                Title = result.Task.Title,
                Description = result.Task.Description,
                AssigneeId = result.Task.AssigneeId,
                Status = result.Task.Status
            };

            await _taskService.AddTaskAsync(_currentUser, task);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            OperationError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task EditTaskAsync()
    {
        if (_currentUser == null || SelectedTask == null) return;

        try
        {
            OperationError = string.Empty;
            var result = await _dialogService.EditTaskAsync(_currentUser, SelectedTask);
            if (result == null) return;

            // Для User сервис сам ограничит изменение только статусом.
            await _taskService.UpdateTaskAsync(_currentUser, result.Task);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            OperationError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ChangeStatusAsync()
    {
        await EditTaskAsync();
    }

    [RelayCommand]
    private async Task DeleteTaskAsync()
    {
        if (_currentUser == null || SelectedTask == null) return;

        try
        {
            OperationError = string.Empty;
            var ok = await _dialogService.ConfirmAsync("Удалить задачу", $"Удалить задачу \"{SelectedTask.Title}\"?");
            if (!ok) return;

            await _taskService.DeleteTaskAsync(_currentUser, SelectedTask.Id);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            OperationError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task NewUserAsync()
    {
        if (_currentUser == null || !IsAdmin) return;

        try
        {
            OperationError = string.Empty;
            var outcome = await _dialogService.EditUserAsync(_currentUser, null);
            await ApplyUserEditorOutcomeAsync(outcome, isNewUser: true);
        }
        catch (Exception ex)
        {
            OperationError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task OpenUserEditorAsync(User? user)
    {
        if (_currentUser == null || !IsAdmin || user == null) return;

        try
        {
            OperationError = string.Empty;
            SelectedUser = user;
            var outcome = await _dialogService.EditUserAsync(_currentUser, user);
            await ApplyUserEditorOutcomeAsync(outcome, isNewUser: false);
        }
        catch (Exception ex)
        {
            OperationError = ex.Message;
        }
    }

    private async Task ApplyUserEditorOutcomeAsync(UserEditorOutcome? outcome, bool isNewUser)
    {
        if (outcome == null || _currentUser == null) return;

        switch (outcome)
        {
            case UserEditorSaved saved:
                if (isNewUser)
                {
                    if (string.IsNullOrWhiteSpace(saved.NewPassword))
                        return;
                    await _userService.AddUserAsync(_currentUser, saved.User, saved.NewPassword);
                    SearchText = string.Empty;
                }
                else
                    await _userService.UpdateUserAsync(_currentUser, saved.User, saved.NewPassword);
                break;
            case UserEditorDeleted deleted:
                await _userService.DeleteUserAsync(_currentUser, deleted.UserId);
                break;
        }

        await LoadDataAsync();

        if (isNewUser && outcome is UserEditorSaved s)
            SelectedUser = AllUsers.FirstOrDefault(u => u.Login.Equals(s.User.Login, StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    private async Task EditUserAsync()
    {
        if (SelectedUser == null) return;
        await OpenUserEditorAsync(SelectedUser);
    }

    [RelayCommand]
    private async Task DeleteUserAsync()
    {
        if (_currentUser == null || !IsAdmin || SelectedUser == null) return;

        try
        {
            OperationError = string.Empty;
            var ok = await _dialogService.ConfirmAsync("Удалить пользователя", $"Удалить пользователя \"{SelectedUser.Login}\"?");
            if (!ok) return;

            await _userService.DeleteUserAsync(_currentUser, SelectedUser.Id);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            OperationError = ex.Message;
        }
    }
}
