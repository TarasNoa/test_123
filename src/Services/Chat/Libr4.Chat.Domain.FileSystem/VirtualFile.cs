using System;
using System.Collections.Generic;

namespace Libr4.Chat.Domain.FileSystem;

public enum FileType { Document, Image, Video, Audio, Code, Archive, Other }

public class VirtualFile
{
    public Guid Id { get; set; }
    public Guid ParentFolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public FileType Type { get; set; }
    public long SizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class VirtualFolder
{
    public Guid Id { get; set; }
    public Guid? ParentFolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public List<Guid> ChildFolderIds { get; set; } = [];
    public List<Guid> FileIds { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class FileVersion
{
    public Guid Id { get; set; }
    public Guid FileId { get; set; }
    public int VersionNumber { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public Guid UploadedBy { get; set; }
    public string ChangeDescription { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
