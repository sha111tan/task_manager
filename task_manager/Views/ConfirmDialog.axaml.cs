using Avalonia.Controls;
using Avalonia.Threading;
using task_manager.ViewModels.Dialogs;

namespace task_manager.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(ConfirmDialogViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.ResultTcs.TrySetResult(false);

        _ = vm.ResultTcs.Task.ContinueWith(_ =>
            Dispatcher.UIThread.Post(Close));
    }
}

