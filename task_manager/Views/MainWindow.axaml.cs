using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;
using task_manager.Models;
using task_manager.ViewModels;

namespace task_manager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    public MainWindow(User currentUser)
    {
        InitializeComponent();           
        var app = (App)Avalonia.Application.Current!;
        if (app.Host is null)
            throw new InvalidOperationException("Host не инициализирован. Проверьте создание App.");

        var vm = app.Host.Services.GetRequiredService<MainWindowViewModel>();
        DataContext = vm;
        Opened += async (_, _) => await vm.InitializeAsync(currentUser);
    }

}