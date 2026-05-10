using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Chat.Domain.Servers.Events;

namespace Libr4.Chat.Domain.Servers;

public class Server : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public Guid OwnerId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public List<Channel> Channels { get; private set; } = new();
    public List<ServerMember> Members { get; private set; } = new();
    public List<Role> Roles { get; private set; } = new();
    public List<ScheduledCall> ScheduledCalls { get; private set; } = new();
    public List<Task> Tasks { get; private set; } = new();
    public ServerSettings Settings { get; private set; } = new();

    private Server() { }

    public static Server Create(string name, Guid ownerId)
    {
        var server = new Server
        {
            Id = Guid.NewGuid(),
            Name = name,
            OwnerId = ownerId,
            CreatedAt = DateTimeOffset.UtcNow,
            Settings = new ServerSettings()
        };

        server.RaiseDomainEvent(new ServerCreatedEvent(server.Id, name, ownerId, server.CreatedAt));
        return server;
    }

    public void AddChannel(string channelName, ChannelType type, string description = "", bool isPrivate = false)
    {
        var channel = new Channel(Guid.NewGuid(), channelName, type, Id, description, isPrivate);
        Channels.Add(channel);
        RaiseDomainEvent(new ChannelAddedEvent(Id, channel.Id, channelName, type));
    }

    public void AddMember(Guid userId, ServerRole role = ServerRole.Member)
    {
        if (!Members.Any(m => m.UserId == userId))
        {
            Members.Add(new ServerMember(userId, role));
            RaiseDomainEvent(new MemberAddedEvent(Id, userId, role));
        }
    }

    public void CreateRole(string name, List<string> permissions)
    {
        var role = new Role(Guid.NewGuid(), name, permissions);
        Roles.Add(role);
        RaiseDomainEvent(new RoleCreatedEvent(Id, role.Id, name));
    }

    public void ScheduleCall(string title, DateTimeOffset scheduledAt, CallType type, string description = "", List<Guid> invitedUsers = null)
    {
        var scheduledCall = ScheduledCall.Create(Id, title, scheduledAt, type, OwnerId, description, invitedUsers ?? new());
        ScheduledCalls.Add(scheduledCall);
        RaiseDomainEvent(new CallScheduledEvent(Id, scheduledCall.Id, title, scheduledAt, type));
    }

    public void CreateTask(string title, string description, Guid assigneeId, DateTimeOffset? dueDate, TaskPriority priority)
    {
        var task = new Task(Guid.NewGuid(), Id, title, description, assigneeId, dueDate, priority);
        Tasks.Add(task);
        RaiseDomainEvent(new TaskCreatedEvent(Id, task.Id, title, assigneeId));
    }

    public void SetWelcomeMessage(string message)
    {
        Settings.WelcomeMessage = message;
        RaiseDomainEvent(new WelcomeMessageSetEvent(Id, message));
    }

    public void UpdateMemberPermissions(Guid userId, List<string> permissions)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            member.UpdatePermissions(permissions);
            RaiseDomainEvent(new MemberPermissionsUpdatedEvent(Id, userId));
        }
    }
}

public enum ChannelType { Text, Voice, Video, Announcement }
public enum ServerRole { Owner, Admin, Moderator, Member }
public enum TaskPriority { Low, Medium, High, Urgent }

public record Channel(Guid Id, string Name, ChannelType Type, Guid ServerId, string Description, bool IsPrivate);

public record Role(Guid Id, string Name, List<string> Permissions);

public record ServerMember
{
    public Guid UserId { get; set; }
    public ServerRole Role { get; set; }
    public List<string> Permissions { get; set; } = new();
    public DateTimeOffset JoinedAt { get; set; }
    public string? Nickname { get; set; }

    public ServerMember() { }
    public ServerMember(Guid userId, ServerRole role)
    {
        UserId = userId;
        Role = role;
        JoinedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePermissions(List<string> permissions)
    {
        Permissions = permissions;
    }
};

public class ServerSettings
{
    public string WelcomeMessage { get; set; } = string.Empty;
    public bool RequireVerification { get; set; }
    public int VerificationLevel { get; set; } // 0-4 (Discord style)
    public List<string> BannedWords { get; set; } = new();
    public bool EnableScreenShare { get; set; } = true;
}

public record Task(Guid Id, Guid ServerId, string Title, string Description, Guid AssigneeId, DateTimeOffset? DueDate, TaskPriority Priority)
{
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public List<string> Labels { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum TaskStatus { Todo, InProgress, Review, Done }