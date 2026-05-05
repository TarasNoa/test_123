using Microsoft.AspNetCore.Mvc;
using Libr4.Collaboration.Application.Commands;
using Libr4.Collaboration.Application.Queries;
using Libr4.Collaboration.Domain;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateCollaborationRoomCommand).Assembly));

// Register repository (will be implemented in Infrastructure)
builder.Services.AddScoped<ICollaborationRoomRepository, InMemoryCollaborationRoomRepository>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");

// Create collaboration room
app.MapPost("/api/collaboration/rooms", async (CreateCollaborationRoomCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { roomId = result.Value })
        : Results.BadRequest(new { error = result.Error.Message });
});

// Join collaboration room
app.MapPost("/api/collaboration/rooms/{roomId}/join", async (Guid roomId, JoinCollaborationRoomCommand command, IMediator mediator) =>
{
    command = command with { RoomId = roomId };
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { sessionId = result.Value })
        : Results.BadRequest(new { error = result.Error.Message });
});

// Leave collaboration room
app.MapPost("/api/collaboration/rooms/{roomId}/leave", async (Guid roomId, LeaveCollaborationRoomCommand command, IMediator mediator) =>
{
    command = command with { RoomId = roomId };
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok()
        : Results.BadRequest(new { error = result.Error.Message });
});

// Send message to room
app.MapPost("/api/collaboration/rooms/{roomId}/messages", async (Guid roomId, SendMessageCommand command, IMediator mediator) =>
{
    command = command with { RoomId = roomId };
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { messageId = result.Value })
        : Results.BadRequest(new { error = result.Error.Message });
});

// Get room messages
app.MapGet("/api/collaboration/rooms/{roomId}/messages", async (Guid roomId, IMediator mediator, int limit = 50, Guid? before = null) =>
{
    var query = new GetRoomMessagesQuery(roomId, limit, before);
    var result = await mediator.Send(query);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Error.Message });
});

// Start video call
app.MapPost("/api/collaboration/rooms/{roomId}/video-call", async (Guid roomId, StartVideoCallCommand command, IMediator mediator) =>
{
    command = command with { RoomId = roomId };
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { callId = result.Value })
        : Results.BadRequest(new { error = result.Error.Message });
});

// Share file in room
app.MapPost("/api/collaboration/rooms/{roomId}/files", async (Guid roomId, ShareFileCommand command, IMediator mediator) =>
{
    command = command with { RoomId = roomId };
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { fileId = result.Value })
        : Results.BadRequest(new { error = result.Error.Message });
});

// Get room details
app.MapGet("/api/collaboration/rooms/{roomId}", async (Guid roomId, IMediator mediator) =>
{
    var query = new GetCollaborationRoomQuery(roomId);
    var result = await mediator.Send(query);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.NotFound(new { error = result.Error.Message });
});

// Get user's rooms
app.MapGet("/api/collaboration/rooms", async (Guid userId, IMediator mediator, string? roomType = null, int limit = 20) =>
{
    var roomTypeEnum = roomType != null ? Enum.Parse<RoomType>(roomType, true) : (RoomType?)null;
    var query = new GetUserRoomsQuery(userId, roomTypeEnum, limit);
    var result = await mediator.Send(query);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Error.Message });
});

app.Run();

// In-memory repository for development (will be replaced with EF Core implementation)
public class InMemoryCollaborationRoomRepository : ICollaborationRoomRepository
{
    private readonly Dictionary<Guid, CollaborationRoom> _rooms = new();

    public Task<CollaborationRoom?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_rooms.TryGetValue(id, out var room) ? room : null);
    }

    public Task<CollaborationRoom?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return GetByIdAsync(id, cancellationToken);
    }

    public Task<List<CollaborationRoom>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rooms = _rooms.Values.Where(r => r.Sessions.Any(s => s.UserId == userId && s.IsActive)).ToList();
        return Task.FromResult(rooms);
    }

    public Task<List<CollaborationRoom>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var rooms = _rooms.Values.Where(r => r.TaskId == taskId).ToList();
        return Task.FromResult(rooms);
    }

    public Task<List<CollaborationRoom>> GetActiveRoomsAsync(CancellationToken cancellationToken = default)
    {
        var rooms = _rooms.Values.Where(r => r.Status == RoomStatus.Active).ToList();
        return Task.FromResult(rooms);
    }

    public Task<List<CollaborationRoom>> GetPublicRoomsAsync(CancellationToken cancellationToken = default)
    {
        var rooms = _rooms.Values.Where(r => r.IsPublic && r.Status == RoomStatus.Active).ToList();
        return Task.FromResult(rooms);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_rooms.ContainsKey(id));
    }

    public Task AddAsync(CollaborationRoom room, CancellationToken cancellationToken = default)
    {
        _rooms[room.Id] = room;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CollaborationRoom room, CancellationToken cancellationToken = default)
    {
        _rooms[room.Id] = room;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CollaborationRoom room, CancellationToken cancellationToken = default)
    {
        _rooms.Remove(room.Id);
        return Task.CompletedTask;
    }
}
