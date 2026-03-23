using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using task_manager.Models;
using task_manager.Services;

namespace task_manager.ViewModels.Dialogs;

public abstract record UserEditorOutcome;

public sealed record UserEditorSaved(User User, string? NewPassword) : UserEditorOutcome;

public sealed record UserEditorDeleted(int UserId) : UserEditorOutcome;

public partial class UserEditorViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;

    public TaskCompletionSource<UserEditorOutcome?> ResultTcs { get; } = new();

    public ObservableCollection<Role> Roles { get; } = new(Enum.GetValues<Role>());

    public bool IsEdit => EditingUserId != 0;

    public User CurrentUser { get; private set; } = null!;
    public int EditingUserId { get; private set; }

    [ObservableProperty] private string login = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private Role selectedRole = Role.User;
    [ObservableProperty] private string errorMessage = string.Empty;

    public UserEditorViewModel(IDialogService dialogService) => _dialogService = dialogService;

    public void Initialize(User currentUser, User? userToEdit)
    {
        CurrentUser = currentUser;
        ErrorMessage = string.Empty;

        if (userToEdit == null)
        {
            EditingUserId = 0;
            Login = string.Empty;
            Password = string.Empty;
            SelectedRole = Role.User;
            DeleteCommand.NotifyCanExecuteChanged();
            return;
        }

        EditingUserId = userToEdit.Id;
        Login = userToEdit.Login;
        Password = string.Empty;
        SelectedRole = userToEdit.Role;
        DeleteCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Login))
        {
            ErrorMessage = "Логин обязателен.";
            return;
        }

        if (!IsEdit && string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Для нового пользователя требуется пароль.";
            return;
        }

        var user = new User
        {
            Id = EditingUserId,
            Login = Login.Trim(),
            Role = SelectedRole
        };

        var newPassword = string.IsNullOrWhiteSpace(Password) ? null : Password;
        ResultTcs.TrySetResult(new UserEditorSaved(user, newPassword));
    }

    private bool CanDeleteUser() => EditingUserId != 0;

    [RelayCommand(CanExecute = nameof(CanDeleteUser))]
    private async Task DeleteAsync()
    {
        if (!CanDeleteUser()) return;
        var ok = await _dialogService.ConfirmAsync(
            "Удалить пользователя",
            $"Удалить пользователя «{Login}»?");
        if (!ok) return;
        ResultTcs.TrySetResult(new UserEditorDeleted(EditingUserId));
    }

    [RelayCommand]
    private void Cancel() => ResultTcs.TrySetResult(null);
}

