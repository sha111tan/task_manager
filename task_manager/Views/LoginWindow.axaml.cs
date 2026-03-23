using Avalonia;
using Avalonia.Controls;
using task_manager.ViewModels;
using task_manager.Models;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;

namespace task_manager.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Opened += async (_, _) => await RunLoginFlowAsync(viewModel);
        Closed += (_, _) => viewModel.LoginTcs.TrySetResult(null);
    }

    private async Task RunLoginFlowAsync(LoginViewModel viewModel)
    {
        User? user = null;
        try
        {
            user = await viewModel.LoginTcs.Task;
        }
        catch
        {
            // игнорируем: при закрытии окна может завершиться Task
        }

        if (user == null)
        {
            Close();
            return;
        }

        var mainWindow = new MainWindow(user);
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = mainWindow;
        mainWindow.Show();
        Close();
    }
}