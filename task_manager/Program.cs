using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Avalonia;
using Avalonia.ReactiveUI;

#if DEBUG
using HotAvalonia;
#endif

namespace task_manager;

class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Database.TaskManagerDbContext>();
        await db.InitializeAsync();

        BuildAvaloniaApp(host)
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp(IHost host)
    {
        return AppBuilder.Configure(() => new App(host))
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI()
        #if DEBUG
            .UseHotReload()
        #endif
            ;
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // БД
        services.AddTransient<Database.TaskManagerDbContext>();

        // Сервисы
        services.AddTransient<Services.IAuthService, Services.AuthService>();
        services.AddTransient<Services.IUserService, Services.UserService>();
        services.AddTransient<Services.ITaskService, Services.TaskService>();
        services.AddTransient<Services.IDialogService, Services.DialogService>();

        // ViewModels
        services.AddTransient<ViewModels.LoginViewModel>();
        services.AddTransient<ViewModels.MainWindowViewModel>();
        services.AddTransient<ViewModels.Dialogs.ConfirmDialogViewModel>();
        services.AddTransient<ViewModels.Dialogs.TaskEditorViewModel>();
        services.AddTransient<ViewModels.Dialogs.TaskDetailsViewModel>();
        services.AddTransient<ViewModels.Dialogs.UserEditorViewModel>();

        // Окна
        services.AddTransient<Views.LoginWindow>();
        services.AddTransient<Views.ConfirmDialog>();
        services.AddTransient<Views.TaskEditorWindow>();
        services.AddTransient<Views.UserEditorWindow>();
    }
}