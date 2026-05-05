namespace Libr4.Shared.Contracts.MemPalace;

/// <summary>
/// Represents a wing in the memory palace (people, projects, etc.).
/// </summary>
public record PalaceWing
{
    /// <summary>
    /// Unique identifier for the wing.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Wing name (e.g., "project-x", "user-alice").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Wing type (person, project, etc.).
    /// </summary>
    public WingType Type { get; init; }

    /// <summary>
    /// Description of the wing.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Rooms in the wing.
    /// </summary>
    public List<PalaceRoom> Rooms { get; init; } = new();

    /// <summary>
    /// When the wing was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Metadata about the wing.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Type of wing.
/// </summary>
public enum WingType
{
    Person,
    Project,
    Team,
    Organization,
    Custom
}

/// <summary>
/// Represents a room in the memory palace (topic).
/// </summary>
public record PalaceRoom
{
    /// <summary>
    /// Unique identifier for the room.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Room name (topic).
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of the room.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Drawers in the room (actual content).
    /// </summary>
    public List<PalaceDrawer> Drawers { get; init; } = new();

    /// <summary>
    /// When the room was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Metadata about the room.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Represents a drawer in the memory palace (verbatim content).
/// </summary>
public record PalaceDrawer
{
    /// <summary>
    /// Unique identifier for the drawer.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Drawer name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Verbatim content.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Content type (conversation, file, note, etc.).
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// When the content was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Source of the content.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Metadata about the drawer.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Search result from the memory palace.
/// </summary>
public record PalaceSearchResult
{
    /// <summary>
    /// Drawer ID.
    /// </summary>
    public string DrawerId { get; init; } = string.Empty;

    /// <summary>
    /// Room ID.
    /// </summary>
    public string RoomId { get; init; } = string.Empty;

    /// <summary>
    /// Wing ID.
    /// </summary>
    public string WingId { get; init; } = string.Empty;

    /// <summary>
    /// Content snippet.
    /// </summary>
    public string Snippet { get; init; } = string.Empty;

    /// <summary>
    /// Relevance score (0-1).
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Interface for MemPalace memory system.
/// </summary>
public interface IMemPalace
{
    /// <summary>
    /// Creates a new wing.
    /// </summary>
    /// <param name="name">Wing name.</param>
    /// <param name="type">Wing type.</param>
    /// <param name="description">Wing description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created wing.</returns>
    Task<PalaceWing> CreateWingAsync(
        string name,
        WingType type,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a wing by ID.
    /// </summary>
    /// <param name="wingId">Wing ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The wing, or null if not found.</returns>
    Task<PalaceWing?> GetWingAsync(
        string wingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a wing by name.
    /// </summary>
    /// <param name="name">Wing name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The wing, or null if not found.</returns>
    Task<PalaceWing?> GetWingByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new room in a wing.
    /// </summary>
    /// <param name="wingId">Wing ID.</param>
    /// <param name="name">Room name.</param>
    /// <param name="description">Room description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created room.</returns>
    Task<PalaceRoom> CreateRoomAsync(
        string wingId,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds content to a room (creates a drawer).
    /// </summary>
    /// <param name="roomId">Room ID.</param>
    /// <param name="name">Drawer name.</param>
    /// <param name="content">Verbatim content.</param>
    /// <param name="contentType">Content type.</param>
    /// <param name="source">Content source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created drawer.</returns>
    Task<PalaceDrawer> AddContentAsync(
        string roomId,
        string name,
        string content,
        string contentType,
        string? source = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the memory palace semantically.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="wingId">Optional wing ID to scope search.</param>
    /// <param name="roomId">Optional room ID to scope search.</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of search results.</returns>
    Task<IReadOnlyList<PalaceSearchResult>> SearchAsync(
        string query,
        string? wingId = null,
        string? roomId = null,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all wings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all wings.</returns>
    Task<IReadOnlyList<PalaceWing>> GetAllWingsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mines content from a directory into the palace.
    /// </summary>
    /// <param name="directoryPath">Path to the directory.</param>
    /// <param name="wingName">Name of the wing to create/use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of items mined.</returns>
    // Task<int> MineDirectoryAsync(
    //     string directoryPath,
    //     string wingName,
    //     CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of MemPalace.
/// </summary>
public class InMemoryMemPalace : IMemPalace
{
    private readonly Dictionary<string, PalaceWing> _wings = new();
    private readonly Dictionary<string, PalaceRoom> _rooms = new();
    private readonly Dictionary<string, PalaceDrawer> _drawers = new();

    public Task<PalaceWing> CreateWingAsync(
        string name,
        WingType type,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var wing = new PalaceWing
        {
            Name = name,
            Type = type,
            Description = description ?? $"Wing for {name}"
        };

        _wings[wing.Id] = wing;
        return Task.FromResult(wing);
    }

    public Task<PalaceWing?> GetWingAsync(
        string wingId,
        CancellationToken cancellationToken = default)
    {
        _wings.TryGetValue(wingId, out var wing);
        return Task.FromResult(wing);
    }

    public Task<PalaceWing?> GetWingByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var wing = _wings.Values.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(wing);
    }

    public Task<PalaceRoom> CreateRoomAsync(
        string wingId,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (!_wings.TryGetValue(wingId, out var wing))
        {
            throw new ArgumentException($"Wing with ID {wingId} not found", nameof(wingId));
        }

        var room = new PalaceRoom
        {
            Name = name,
            Description = description ?? $"Room for {name}"
        };

        _rooms[room.Id] = room;
        
        var updatedWing = wing with { Rooms = wing.Rooms.Concat(new[] { room }).ToList() };
        _wings[wingId] = updatedWing;

        return Task.FromResult(room);
    }

    public Task<PalaceDrawer> AddContentAsync(
        string roomId,
        string name,
        string content,
        string contentType,
        string? source = null,
        CancellationToken cancellationToken = default)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            throw new ArgumentException($"Room with ID {roomId} not found", nameof(roomId));
        }

        var drawer = new PalaceDrawer
        {
            Name = name,
            Content = content,
            ContentType = contentType,
            Source = source
        };

        _drawers[drawer.Id] = drawer;

        var updatedRoom = room with { Drawers = room.Drawers.Concat(new[] { drawer }).ToList() };
        _rooms[roomId] = updatedRoom;

        return Task.FromResult(drawer);
    }

    public Task<IReadOnlyList<PalaceSearchResult>> SearchAsync(
        string query,
        string? wingId = null,
        string? roomId = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var results = new List<PalaceSearchResult>();
        var lowerQuery = query.ToLowerInvariant();

        var drawersToSearch = _drawers.Values.AsEnumerable();
        
        if (wingId != null)
        {
            var wing = _wings.GetValueOrDefault(wingId);
            if (wing != null)
            {
                var roomIds = wing.Rooms.Select(r => r.Id).ToHashSet();
                drawersToSearch = drawersToSearch.Where(d => roomIds.Contains(_rooms.Values.FirstOrDefault(r => r.Drawers.Any(dr => dr.Id == d.Id))?.Id ?? string.Empty));
            }
        }

        if (roomId != null)
        {
            var room = _rooms.GetValueOrDefault(roomId);
            if (room != null)
            {
                var drawerIds = room.Drawers.Select(d => d.Id).ToHashSet();
                drawersToSearch = drawersToSearch.Where(d => drawerIds.Contains(d.Id));
            }
        }

        foreach (var drawer in drawersToSearch)
        {
            if (drawer.Content.ToLowerInvariant().Contains(lowerQuery))
            {
                var room = _rooms.Values.FirstOrDefault(r => r.Drawers.Any(d => d.Id == drawer.Id));
                var wing = _wings.Values.FirstOrDefault(w => w.Rooms.Any(r => r.Id == room?.Id));

                if (room != null && wing != null)
                {
                    results.Add(new PalaceSearchResult
                    {
                        DrawerId = drawer.Id,
                        RoomId = room.Id,
                        WingId = wing.Id,
                        Snippet = drawer.Content.Substring(0, Math.Min(200, drawer.Content.Length)),
                        Score = 1.0 // Simple matching for now
                    });
                }
            }
        }

        return Task.FromResult<IReadOnlyList<PalaceSearchResult>>(
            results.Take(limit).ToList().AsReadOnly());
    }

    public Task<IReadOnlyList<PalaceWing>> GetAllWingsAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PalaceWing>>(_wings.Values.ToList().AsReadOnly());
    }

    private string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".cs" => "text/csharp",
            ".fs" => "text/fsharp",
            ".js" => "text/javascript",
            ".ts" => "text/typescript",
            ".py" => "text/python",
            ".rs" => "text/rust",
            ".java" => "text/java",
            ".go" => "text/golang",
            ".md" => "text/markdown",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".yaml" or ".yml" => "application/yaml",
            ".txt" => "text/plain",
            _ => "text/plain"
        };
    }
}
