namespace Libr4.Shared.Contracts.FSharp

open System

/// Memory Palace Domain Types
type WingType =
    | Person
    | Project
    | Team
    | Organization
    | Custom of string

type PalaceWing = {
    Id: string
    Name: string
    Type: WingType
    Description: string
    Rooms: PalaceRoom list
    CreatedAt: DateTime
    Metadata: Map<string, string>
}

and PalaceRoom = {
    Id: string
    Name: string
    Description: string
    Drawers: PalaceDrawer list
    CreatedAt: DateTime
    Metadata: Map<string, string>
}

and PalaceDrawer = {
    Id: string
    Name: string
    Content: string
    ContentType: string
    Source: string option
    CreatedAt: DateTime
    Metadata: Map<string, string>
}

type PalaceSearchResult = {
    DrawerId: string
    RoomId: string
    WingId: string
    Snippet: string
    Score: double
}

/// Memory Palace Operations
module MemoryPalace =
    let createWing name wingType description =
        {
            Id = Guid.NewGuid().ToString()
            Name = name
            Type = wingType
            Description = description
            Rooms = []
            CreatedAt = DateTime.UtcNow
            Metadata = Map.empty
        }
    
    let createRoom name description =
        {
            Id = Guid.NewGuid().ToString()
            Name = name
            Description = description
            Drawers = []
            CreatedAt = DateTime.UtcNow
            Metadata = Map.empty
        }
    
    let createDrawer name content contentType source =
        {
            Id = Guid.NewGuid().ToString()
            Name = name
            Content = content
            ContentType = contentType
            Source = source
            CreatedAt = DateTime.UtcNow
            Metadata = Map.empty
        }
    
    let addRoomToWing wing room =
        { wing with Rooms = room :: wing.Rooms }
    
    let addDrawerToRoom room drawer =
        { room with Drawers = drawer :: room.Drawers }
    
    let searchContent query wing =
        let lowerQuery = query.ToLowerInvariant()
        
        wing.Rooms
        |> List.collect (fun room ->
            room.Drawers
            |> List.filter (fun drawer ->
                drawer.Content.ToLowerInvariant().Contains(lowerQuery))
            |> List.map (fun drawer ->
                {
                    DrawerId = drawer.Id
                    RoomId = room.Id
                    WingId = wing.Id
                    Snippet = drawer.Content.Substring(0, min 200 drawer.Content.Length)
                    Score = 1.0
                }))
