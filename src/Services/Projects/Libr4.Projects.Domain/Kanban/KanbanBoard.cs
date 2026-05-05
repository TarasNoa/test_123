using Libr4.Shared.Kernel.Domain;

namespace Libr4.Projects.Domain.Kanban;

public enum CardStatus
{
    Backlog,
    Todo,
    InProgress,
    Review,
    Done,
    Archived
}

public enum CardPriority
{
    Low,
    Medium,
    High,
    Critical
}

public class KanbanBoard : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public Guid ProjectId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<KanbanColumn> _columns = new();
    public IReadOnlyCollection<KanbanColumn> Columns => _columns.AsReadOnly();

    private readonly List<KanbanCard> _cards = new();
    public IReadOnlyCollection<KanbanCard> Cards => _cards.AsReadOnly();

    private KanbanBoard() { }

    public static KanbanBoard Create(string name, Guid projectId)
    {
        var board = new KanbanBoard
        {
            Id = Guid.NewGuid(),
            Name = name,
            ProjectId = projectId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create default columns
        board.AddColumn(KanbanColumn.Create(board.Id, "Backlog", 0));
        board.AddColumn(KanbanColumn.Create(board.Id, "To Do", 1));
        board.AddColumn(KanbanColumn.Create(board.Id, "In Progress", 2));
        board.AddColumn(KanbanColumn.Create(board.Id, "Review", 3));
        board.AddColumn(KanbanColumn.Create(board.Id, "Done", 4));

        return board;
    }

    public void AddColumn(KanbanColumn column)
    {
        _columns.Add(column);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveColumn(Guid columnId)
    {
        var column = _columns.FirstOrDefault(c => c.Id == columnId);
        if (column != null)
        {
            _columns.Remove(column);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddCard(KanbanCard card)
    {
        _cards.Add(card);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveCard(Guid cardId)
    {
        var card = _cards.FirstOrDefault(c => c.Id == cardId);
        if (card != null)
        {
            _cards.Remove(card);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MoveCard(Guid cardId, Guid targetColumnId, int newPosition = -1)
    {
        var card = _cards.FirstOrDefault(c => c.Id == cardId);
        if (card != null)
        {
            card.MoveToColumn(targetColumnId, newPosition);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

public class KanbanColumn : Entity<Guid>
{
    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private KanbanColumn() { }

    public static KanbanColumn Create(Guid boardId, string name, int order)
    {
        return new KanbanColumn
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Name = name,
            Order = order,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public void UpdateOrder(int order)
    {
        Order = order;
    }
}

public class KanbanCard : Entity<Guid>
{
    public Guid BoardId { get; private set; }
    public Guid ColumnId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CardStatus Status { get; private set; }
    public CardPriority Priority { get; private set; }
    public int Position { get; private set; }
    public Guid? AssignedToId { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<KanbanLabel> _labels = new();
    public IReadOnlyCollection<KanbanLabel> Labels => _labels.AsReadOnly();

    private readonly List<KanbanComment> _comments = new();
    public IReadOnlyCollection<KanbanComment> Comments => _comments.AsReadOnly();

    private KanbanCard() { }

    public static KanbanCard Create(Guid boardId, Guid columnId, string title, CardPriority priority = CardPriority.Medium)
    {
        return new KanbanCard
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            ColumnId = columnId,
            Title = title,
            Status = CardStatus.Backlog,
            Priority = priority,
            Position = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MoveToColumn(Guid columnId, int newPosition = -1)
    {
        ColumnId = columnId;
        if (newPosition >= 0)
        {
            Position = newPosition;
        }
        UpdateStatus();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        Title = title;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPriority(CardPriority priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignTo(Guid? userId)
    {
        AssignedToId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDueDate(DateTime? dueDate)
    {
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddLabel(KanbanLabel label)
    {
        _labels.Add(label);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveLabel(Guid labelId)
    {
        var label = _labels.FirstOrDefault(l => l.Id == labelId);
        if (label != null)
        {
            _labels.Remove(label);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddComment(KanbanComment comment)
    {
        _comments.Add(comment);
        UpdatedAt = DateTime.UtcNow;
    }

    private void UpdateStatus()
    {
        Status = ColumnId switch
        {
            var id when id.ToString().Contains("Backlog") => CardStatus.Backlog,
            var id when id.ToString().Contains("Todo") => CardStatus.Todo,
            var id when id.ToString().Contains("Progress") => CardStatus.InProgress,
            var id when id.ToString().Contains("Review") => CardStatus.Review,
            var id when id.ToString().Contains("Done") => CardStatus.Done,
            _ => CardStatus.Backlog
        };
    }
}

public class KanbanLabel : Entity<Guid>
{
    public Guid CardId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = "#000000";

    private KanbanLabel() { }

    public static KanbanLabel Create(Guid cardId, string name, string color = "#000000")
    {
        return new KanbanLabel
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            Name = name,
            Color = color
        };
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public void UpdateColor(string color)
    {
        Color = color;
    }
}

public class KanbanComment : Entity<Guid>
{
    public Guid CardId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private KanbanComment() { }

    public static KanbanComment Create(Guid cardId, Guid authorId, string content)
    {
        return new KanbanComment
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            AuthorId = authorId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateContent(string content)
    {
        Content = content;
    }
}
