using System;

namespace task_manager.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string DescriptionPreview
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Description))
                return string.Empty;
            const int max = 50;
            var s = Description.Trim();
            return s.Length <= max ? s : s[..max] + "...";
        }
    }

    public string StatusDisplayRu => Status switch
    {
        Status.New => "Новая",
        Status.InProgress => "В работе",
        Status.Completed => "Завершена",
        Status.Cancelled => "Отменена",
        _ => Status.ToString()
    };
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public Status Status { get; set; } = Status.New;

    public int AuthorId { get; set; }
    public User? Author { get; set; }

    public int AssigneeId { get; set; }
    public User? Assignee { get; set; }
}