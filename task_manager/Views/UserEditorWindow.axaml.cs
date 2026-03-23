using Avalonia.Controls;
using Avalonia.Threading;
using task_manager.ViewModels.Dialogs;

namespace task_manager.Views;

public partial class UserEditorWindow : Window
{
    public UserEditorWindow()
    {
        InitializeComponent();
    }
    public UserEditorWindow(UserEditorViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        Closed += (_, _) => vm.ResultTcs.TrySetResult(null);
        _ = vm.ResultTcs.Task.ContinueWith(_ =>
            Dispatcher.UIThread.Post(Close));
    }
}

