using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace task_manager.ViewModels.Dialogs;

public partial class ConfirmDialogViewModel : ObservableObject
{
    public TaskCompletionSource<bool> ResultTcs { get; } = new();

    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string message = string.Empty;

    [RelayCommand]
    private void Yes() => ResultTcs.TrySetResult(true);

    [RelayCommand]
    private void No() => ResultTcs.TrySetResult(false);
}

