using Avalonia.Controls;
using Avalonia.Threading;
using task_manager.ViewModels.Dialogs;

namespace task_manager.Views;

public partial class TaskDetailsWindow : Window
{
    public TaskDetailsWindow()
    {
        InitializeComponent();
    }

    public TaskDetailsWindow(TaskDetailsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Title = string.IsNullOrWhiteSpace(vm.CurrentTask.Title) ? "Задача" : vm.CurrentTask.Title;
        Closed += (_, _) => vm.ResultTcs.TrySetResult(false);

        _ = vm.ResultTcs.Task.ContinueWith(_ =>
            Dispatcher.UIThread.Post(Close));
    }
}
