using task_manager.Models;

namespace task_manager.ViewModels.Dialogs;

public sealed class TaskStatusOption
{
    public required Status Value { get; init; }
    public required string Label { get; init; }
}
