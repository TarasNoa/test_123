using System;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Collaboration.Domain;

public class Whiteboard : Entity<Guid>
{
    public Guid RoomId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public List<DrawingElement> Elements { get; private set; } = new();
    public DrawingToolState CurrentToolState { get; private set; } = new();
    public DateTimeOffset CreatedAt { get; private set; }

    private Whiteboard() { }

    public static Whiteboard Create(Guid roomId, string name)
    {
        return new Whiteboard
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddElement(DrawingElement element)
    {
        Elements.Add(element);
    }

    public void UpdateElement(Guid elementId, DrawingElement updatedElement)
    {
        var index = Elements.FindIndex(e => e.Id == elementId);
        if (index >= 0)
        {
            Elements[index] = updatedElement;
        }
    }

    public void RemoveElement(Guid elementId)
    {
        Elements.RemoveAll(e => e.Id == elementId);
    }

    public void ClearAll()
    {
        Elements.Clear();
    }
}

public record DrawingElement(Guid Id, string Type, double X, double Y, double Width, double Height, string Color, string StrokeWidth, string? Text, DateTimeOffset CreatedAt);

public class DrawingToolState
{
    public string CurrentTool { get; set; } = "pen"; // pen, eraser, line, rectangle, circle, text
    public string Color { get; set; } = "#000000";
    public string StrokeWidth { get; set; } = "2";
    public int Opacity { get; set; } = 100;
}