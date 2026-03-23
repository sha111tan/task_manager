using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using task_manager.Models;
using task_manager.Services;
using System;
using System.Threading.Tasks;

namespace task_manager.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty] private string login = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;

    public TaskCompletionSource<User?> LoginTcs { get; } = new TaskCompletionSource<User?>();

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;
        var user = await _authService.LoginAsync(Login, Password);

        if (user != null)
        {
            LoginTcs.TrySetResult(user); 
        }
        else
        {
            ErrorMessage = "Неверный логин или пароль";
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        LoginTcs.TrySetResult(null);
    }
}