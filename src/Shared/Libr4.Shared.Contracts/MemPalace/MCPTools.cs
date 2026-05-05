namespace Libr4.Shared.Contracts.MemPalace;

/// <summary>
/// MCP tool definitions for MemPalace.
/// Based on mempalaceofficial.com/reference/mcp-tools
/// </summary>
public static class MemPalaceMCPTools
{
    /// <summary>
    /// Creates all MCP tool definitions.
    /// </summary>
    /// <returns>List of MCP tool definitions.</returns>
    public static List<MCPToolDefinition> CreateAll()
    {
        return new List<MCPToolDefinition>
        {
            // Palace reads/writes
            CreatePalaceRead(),
            CreatePalaceWrite(),
            CreatePalaceListWings(),
            CreatePalaceListRooms(),
            CreatePalaceListDrawers(),
            
            // Knowledge graph operations
            CreateKGAddEntity(),
            CreateKGAddRelationship(),
            CreateKGQuery(),
            CreateKGInvalidate(),
            CreateKGTimeline(),
            
            // Cross-wing navigation
            CreateCrossWingSearch(),
            CreateCrossWingNavigate(),
            
            // Drawer management
            CreateDrawerCreate(),
            CreateDrawerUpdate(),
            CreateDrawerDelete(),
            CreateDrawerRead(),
            
            // Agent diaries
            CreateAgentDiaryCreate(),
            CreateAgentDiaryRead(),
            CreateAgentDiaryUpdate(),
            
            // Palace management
            CreatePalaceInit(),
            CreatePalaceExport(),
            CreatePalaceImport(),
            CreatePalaceStats(),
            
            // Advanced operations
            CreatePalaceMine(),
            CreatePalaceWakeUp(),
            CreatePalaceCompact(),
            CreatePalaceBackup(),
            CreatePalaceRestore(),
            CreatePalaceConfigure()
        };
    }

    private static MCPToolDefinition CreatePalaceRead()
    {
        return new MCPToolDefinition
        {
            Name = "palace_read",
            Description = "Read content from a specific drawer in the memory palace",
            InputSchema = new Dictionary<string, string>
            {
                ["wing_id"] = "string - ID of the wing",
                ["room_id"] = "string - ID of the room",
                ["drawer_id"] = "string - ID of the drawer"
            }
        };
    }

    private static MCPToolDefinition CreatePalaceWrite()
    {
        return new MCPToolDefinition
        {
            Name = "palace_write",
            Description = "Write content to a drawer in the memory palace",
            InputSchema = new Dictionary<string, string>
            {
                ["wing_id"] = "string - ID of the wing",
                ["room_id"] = "string - ID of the room",
                ["drawer_id"] = "string - ID of the drawer (optional, creates new if not provided)",
                ["name"] = "string - Name of the drawer",
                ["content"] = "string - Verbatim content to store",
                ["content_type"] = "string - Type of content (conversation, file, note, etc.)",
                ["source"] = "string - Source of the content (optional)"
            }
        };
    }

    private static MCPToolDefinition CreatePalaceListWings()
    {
        return new MCPToolDefinition
        {
            Name = "palace_list_wings",
            Description = "List all wings in the memory palace",
            InputSchema = new Dictionary<string, string>
            {
                ["wing_type"] = "string - Filter by wing type (optional)"
            }
        };
    }

    private static MCPToolDefinition CreatePalaceListRooms()
    {
        return new MCPToolDefinition
        {
            Name = "palace_list_rooms",
            Description = "List all rooms in a wing",
            InputSchema = new Dictionary<string, string>
            {
                ["wing_id"] = "string - ID of the wing"
            }
        };
    }

    private static MCPToolDefinition CreatePalaceListDrawers()
    {
        return new MCPToolDefinition
        {
            Name = "palace_list_drawers",
            Description = "List all drawers in a room",
            InputSchema = new Dictionary<string, string>
            {
                ["wing_id"] = "string - ID of the wing",
                ["room_id"] = "string - ID of the room"
            }
        };
    }

    private static MCPToolDefinition CreateKGAddEntity()
    {
        return new MCPToolDefinition
        {
            Name = "kg_add_entity",
            Description = "Add an entity to the knowledge graph",
            InputSchema = new Dictionary<string, string>
            {
                ["name"] = "string - Name of the entity",
                ["entity_type"] = "string - Type of entity (person, project, concept, etc.)",
                ["properties"] = "string - JSON string of entity properties"
            }
        };
    }

    private static MCPToolDefinition CreateKGAddRelationship()
    {
        return new MCPToolDefinition
        {
            Name = "kg_add_relationship",
            Description = "Add a relationship between entities in the knowledge graph",
            InputSchema = new Dictionary<string, string>
            {
                ["from_entity_id"] = "string - ID of the source entity",
                ["to_entity_id"] = "string - ID of the target entity",
                ["relationship_type"] = "string - Type of relationship (works_on, related_to, depends_on, etc.)",
                ["valid_from"] = "string - ISO timestamp for validity start (optional)",
                ["valid_to"] = "string - ISO timestamp for validity end (optional)",
                ["properties"] = "string - JSON string of relationship properties (optional)"
            }
        };
    }

    private static MCPToolDefinition CreateKGQuery()
    {
        return new MCPToolDefinition
        {
            Name = "kg_query",
            Description = "Query the knowledge graph for entities and relationships",
            InputSchema = new Dictionary<string, string>
            {
                ["entity_type"] = "string - Filter by entity type (optional)",
                ["property_filters"] = "string - JSON string of property filters (optional)",
                ["traverse_relationship_type"] = "string - Relationship type to traverse (optional)",
                ["max_depth"] = "number - Maximum depth for traversal (default: 1)",
                ["include_related"] = "boolean - Whether to include related entities (default: false)"
            }
        };
    }

    private static MCPToolDefinition CreateKGInvalidate()
    {
        return new MCPToolDefinition
        {
            Name = "kg_invalidate",
            Description = "Invalidate a relationship in the knowledge graph (sets ValidTo to current time)",
            InputSchema = new Dictionary<string, string>
            {
                ["relationship_id"] = "string - ID of the relationship to invalidate"
            }
        };
    }

    private static MCPToolDefinition CreateKGTimeline()
    {
        return new MCPToolDefinition
        {
            Name = "kg_timeline",
            Description = "Get timeline events for an entity or within a time range",
            InputSchema = new Dictionary<string, string>
            {
                ["entity_id"] = "string - ID of the entity (optional)",
                ["from"] = "string - ISO timestamp for range start (optional)",
                ["to"] = "string - ISO timestamp for range end (optional)"
            }
        };
    }

    private static MCPToolDefinition CreateCrossWingSearch()
    {
        return new MCPToolDefinition
        {
            Name = "cross_wing_search",
            Description = "Search across multiple wings in the memory palace",
            InputSchema = new Dictionary<string, string>
            {
                ["query"] = "string - Search query",
                ["wing_ids"] = "string - JSON array of wing IDs to search (optional, searches all if not provided)",
                ["limit"] = "number - Maximum number of results (default: 10)"
            }
        };
    }

    private static MCPToolDefinition CreateCrossWingNavigate()
    {
        return new MCPToolDefinition
        {
            Name = "cross_wing_navigate",
            Description = "Navigate between wings through relationships",
            InputSchema = new Dictionary<string, string>
            {
                ["start_wing_id"] = "string - ID of the starting wing",
                ["relationship_type"] = "string - Type of relationship to follow",
                ["max_depth"] = "number - Maximum depth of navigation (default: 3)"
            }
        };
    }

    private static MCPToolDefinition CreateDrawerCreate()
    {
        return new MCPToolDefinition
        {
            Name = "drawer_create",
            Description = "Create a new drawer in a room",
            InputSchema = new Dictionary<string, string>
            {
                ["wing_id"] = "string - ID of the wing",
                ["room_id"] = "string - ID of the room",
                ["name"] = "string - Name of the drawer",
                ["content"] = "string - Initial content for the drawer",
                ["content_type"] = "string - Type of content",
                ["source"] = "string - Source of the content (optional)"
            }
        };
    }

    private static MCPToolDefinition CreateDrawerUpdate()
    {
        return new MCPToolDefinition
        {
            Name = "drawer_update",
            Description = "Update an existing drawer",
            InputSchema = new Dictionary<string, string>
            {
                ["drawer_id"] = "string - ID of the drawer",
                ["name"] = "string - New name (optional)",
                ["content"] = "string - New content (optional)",
                ["content_type"] = "string - New content type (optional)"
            }
        };
    }

    private static MCPToolDefinition CreateDrawerDelete()
    {
        return new MCPToolDefinition
        {
            Name = "drawer_delete",
            Description = "Delete a drawer from the memory palace",
            InputSchema = new Dictionary<string, string>
            {
                ["drawer_id"] = "string - ID of the drawer to delete"
            }
        };
    }

    private static MCPToolDefinition CreateDrawerRead()
    {
        return new MCPToolDefinition
        {
            Name = "drawer_read",
            Description = "Read the full content of a drawer",
            InputSchema = new Dictionary<string, string>
            {
                ["drawer_id"] = "string - ID of the drawer"
            }
        };
    }

    private static MCPToolDefinition CreateAgentDiaryCreate()
    {
        return new MCPToolDefinition
        {
            Name = "agent_diary_create",
            Description = "Create a diary for a specialist agent",
            InputSchema = new Dictionary<string, string>
            {
                ["agent_name"] = "string - Name of the agent",
                ["agent_type"] = "string - Type of the agent",
                ["wing_id"] = "string - ID of the wing (optional, creates new if not provided)"
            }
        };
    }

    private static MCPToolDefinition CreateAgentDiaryRead()
    {
        return new MCPToolDefinition
        {
            Name = "agent_diary_read",
            Description = "Read an agent's diary",
            InputSchema = new Dictionary<string, string>
            {
                ["agent_name"] = "string - Name of the agent"
            }
        };
    }

    private static MCPToolDefinition CreateAgentDiaryUpdate()
    {
        return new MCPToolDefinition
        {
            Name = "agent_diary_update",
            Description = "Update an agent's diary with a new entry",
            InputSchema = new Dictionary<string, string>
            {
                ["agent_name"] = "string - Name of the agent",
                ["entry"] = "string - Diary entry content",
                ["entry_type"] = "string - Type of entry (thought, action, observation, etc.)"
            }
        };
    }

    private static MCPToolDefinition CreatePalaceInit()
    {
        return new MCPToolDefinition
        {
            Name = "palace_init",
            Description = "Initialize a new memory palace",
            InputSchema = new Dictionary<string, string>
            {
                ["name"] = "string - Name of the palace",
                ["path"] = "string - Path where the palace will be stored"
            }
        };
    }

    private static MCPToolDefinition CreatePalaceExport()
    {
        return new MCPToolDefinition
        {
            Name = "palace_export",
            Description = "Export a memory palace to a file",
            InputSchema = new Dictionary<string, string>
            {
                ["format"] = "string - Export format (json, jsonl)",
                ["path"] = "string - Path where to export"
            }
        };
    }

    private static MCPToolDefinition CreatePalaceImport()
    {
        return new MCPToolDefinition
        {
            Name = "palace_import",
            Description = "Import a memory palace from a file",
            InputSchema = new Dictionary<string, string>
            {
                ["path"] = "string - Path of the file to import"
            }
        };
    }

    private static MCPToolDefinition CreatePalaceStats()
    {
        return new MCPToolDefinition
        {
            Name = "palace_stats",
            Description = "Get statistics about the memory palace",
            InputSchema = new Dictionary<string, string>()
        };
    }

    private static MCPToolDefinition CreatePalaceMine()
    {
        return new MCPToolDefinition
        {
            Name = "palace_mine",
            Description = "Mine content from a directory into the palace",
            InputSchema = new Dictionary<string, string>
            {
                ["directory_path"] = "string - Path to the directory",
                ["wing_name"] = "string - Name of the wing to create/use",
                ["mode"] = "string - Mining mode (files, convos, both)"
            }
        };
    }

    private static MCPToolDefinition CreatePalaceWakeUp()
    {
        return new MCPToolDefinition
        {
            Name = "palace_wake_up",
            Description = "Load context from the palace for a new session",
            InputSchema = new Dictionary<string, string>
            {
                ["query"] = "string - Query to retrieve relevant context",
                ["limit"] = "number - Maximum number of results (default: 10)"
            }
        };
    }

    private static MCPToolDefinition CreatePalaceCompact()
    {
        return new MCPToolDefinition
        {
            Name = "palace_compact",
            Description = "Compact the memory palace to optimize storage",
            InputSchema = new Dictionary<string, string>()
        };
    }

    private static MCPToolDefinition CreatePalaceBackup()
    {
        return new MCPToolDefinition
        {
            Name = "palace_backup",
            Description = "Create a backup of the memory palace",
            InputSchema = new Dictionary<string, string>
            {
                ["path"] = "string - Path where to save the backup"
            }
        };
    }

    private static MCPToolDefinition CreatePalaceRestore()
    {
        return new MCPToolDefinition
        {
            Name = "palace_restore",
            Description = "Restore a memory palace from a backup",
            InputSchema = new Dictionary<string, string>
            {
                ["path"] = "string - Path of the backup file"
            }
        };
    }

    private static MCPToolDefinition CreatePalaceConfigure()
    {
        return new MCPToolDefinition
        {
            Name = "palace_configure",
            Description = "Configure memory palace settings",
            InputSchema = new Dictionary<string, string>
            {
                ["settings"] = "string - JSON string of configuration settings"
            }
        };
    }
}

/// <summary>
/// MCP tool definition.
/// </summary>
public record MCPToolDefinition
{
    /// <summary>
    /// Tool name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Tool description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Input schema.
    /// </summary>
    public Dictionary<string, string> InputSchema { get; init; } = new();
}
