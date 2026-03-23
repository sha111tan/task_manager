using Avalonia;
using Avalonia.Markup.Xaml;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using task_manager.Views;

namespace task_manager;

public partial class App : Application
{

    public IHost? Host { get; private set; }

    public App() { }

    public App(IHost host)
    {
        Host = host;
    }

    public override void Initialize()
    {
       AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {

        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (Host is null)
                throw new InvalidOperationException("Host не инициализирован. Проверьте способ создания App.");

            var loginWindow = Host.Services.GetRequiredService<LoginWindow>();
            desktop.MainWindow = loginWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}