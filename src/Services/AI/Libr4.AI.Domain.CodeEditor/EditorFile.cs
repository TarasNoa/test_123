using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.CodeEditor;

public enum ProjectType { WebApp, MobileApp, Api, DesktopApp, Game, Bot, Other }
public enum ProjectStatus { Planning, InDevelopment, Testing, Deployed, Maintenance, Completed, Cancelled }
public enum CodeLanguage { Python, JavaScript, TypeScript, Java, Cpp, CSharp, Go, Rust, Php, Ruby, Swift, Kotlin, Html, Css, Sql, Other }
public enum CollaboratorRole { Owner, Collaborator, Viewer }

public class CodeProject
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectType ProjectType { get; set; }
    public CodeLanguage Language { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public string? TemplateUsed { get; set; }
    public Dictionary<string, object> ProjectConfig { get; set; } = new Dictionary<string, object>();
    public bool IsPublic { get; set; }
    public List<ProjectCodeFile> Files { get; set; } = new List<ProjectCodeFile>();
    public List<CodeProjectCollaborator> Collaborators { get; set; } = new List<CodeProjectCollaborator>();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void AddCollaborator(Guid userId, CollaboratorRole role)
    {
        Collaborators.Add(new CodeProjectCollaborator
        {
            Id = Guid.NewGuid(),
            ProjectId = Id,
            UserId = userId,
            Role = role,
            AddedAt = DateTimeOffset.UtcNow
        });
    }
}

public class ProjectCodeFile
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? FileType { get; set; }
    public int Size { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void UpdateContent(string newContent, DateTimeOffset now)
    {
        Content = newContent;
        Size = newContent.Length;
        UpdatedAt = now;
    }
}

public class CodeProjectCollaborator
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public CollaboratorRole Role { get; set; } = CollaboratorRole.Collaborator;
    public Dictionary<string, object> Permissions { get; set; } = new Dictionary<string, object>();
    public DateTimeOffset AddedAt { get; set; }
}

public class EditorFile
{
    public Guid Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
