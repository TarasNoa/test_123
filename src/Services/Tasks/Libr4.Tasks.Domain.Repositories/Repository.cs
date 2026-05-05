using System;
using System.Collections.Generic;

namespace Libr4.Tasks.Domain.Repositories;

/// <summary>Repository visibility enumeration</summary>
public enum RepositoryVisibility
{
    Private,      // Только владелец
    ClientView,   // Клиент может просматривать
    Public        // Публичный
}

/// <summary>Repository status enumeration</summary>
public enum RepositoryStatus
{
    Active,       // Активный
    Archived,     // Архивирован
    Locked        // Заблокирован до оплаты
}

/// <summary>Access level enumeration</summary>
public enum AccessLevel
{
    None,         // Нет доступа
    View,         // Только просмотр
    Download,     // Скачивание разрешено
    Edit,         // Редактирование
    Admin         // Полный доступ
}

/// <summary>Repository view action enumeration</summary>
public enum RepositoryViewAction
{
    View,
    DownloadAttempt,
    CloneAttempt
}

/// <summary>Repository aggregate root</summary>
public class Repository
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Владелец (фрилансер)
    public Guid OwnerId { get; set; }
    
    // Связь с проектом/задачей
    public Guid? TaskId { get; set; }
    
    // Настройки
    public RepositoryVisibility Visibility { get; set; } = RepositoryVisibility.Private;
    public RepositoryStatus Status { get; set; } = RepositoryStatus.Active;
    
    // Статистика
    public long SizeBytes { get; set; }
    public int FilesCount { get; set; }
    public int CommitsCount { get; set; }
    
    // Защита
    public bool RequiresPayment { get; set; } = true;
    public decimal? PaymentAmount { get; set; } // В копейках
    public bool IsPaid { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    
    // Метаданные
    public string? Language { get; set; }
    public string? Framework { get; set; }
    public List<string> Tags { get; set; } = [];
    
    // Временные метки
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastCommitAt { get; set; }
    
    // Связи
    public List<RepositoryFile> Files { get; set; } = [];
    public List<Commit> Commits { get; set; } = [];
    public List<RepositoryAccess> AccessGrants { get; set; } = [];
    public List<Branch> Branches { get; set; } = [];

    public void MarkAsPaid(DateTimeOffset now)
    {
        IsPaid = true;
        PaidAt = now;
        Status = RepositoryStatus.Active;
        UpdatedAt = now;
    }

    public void Lock(DateTimeOffset now)
    {
        Status = RepositoryStatus.Locked;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        Status = RepositoryStatus.Archived;
        UpdatedAt = now;
    }

    public void UpdateStatistics(long sizeBytes, int filesCount, int commitsCount, DateTimeOffset now)
    {
        SizeBytes = sizeBytes;
        FilesCount = filesCount;
        CommitsCount = commitsCount;
        UpdatedAt = now;
    }

    public void UpdateLastCommit(DateTimeOffset commitTime)
    {
        LastCommitAt = commitTime;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Repository file entity</summary>
public class RepositoryFile
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    
    // Информация о файле
    public string Path { get; set; } = string.Empty;  // Путь в репозитории
    public string Name { get; set; } = string.Empty;
    public string? Extension { get; set; }
    
    // Содержимое
    public string? Content { get; set; }  // Для текстовых файлов
    public string? ContentHash { get; set; }  // SHA256
    public string? StoragePath { get; set; }  // Путь в S3/файловой системе
    
    // Метаданные
    public long SizeBytes { get; set; }
    public string? Language { get; set; }
    public bool IsBinary { get; set; }
    
    // Версионирование
    public string Branch { get; set; } = "main";
    public Guid? CommitId { get; set; }
    
    // Временные метки
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Commit entity</summary>
public class Commit
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    
    // Информация о коммите
    public string Message { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    
    // Ветка
    public string Branch { get; set; } = "main";
    public Guid? ParentCommitId { get; set; }
    
    // Изменения
    public int FilesChanged { get; set; }
    public int Insertions { get; set; }
    public int Deletions { get; set; }
    public Dictionary<string, object> Changes { get; set; } = [];  // Детали изменений
    
    // Временная метка
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Branch entity</summary>
public class Branch
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsProtected { get; set; }
    
    // Последний коммит
    public Guid? LastCommitId { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Repository access grant entity</summary>
public class RepositoryAccess
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    
    // Пользователь
    public Guid UserId { get; set; }
    
    // Уровень доступа
    public AccessLevel AccessLevel { get; set; } = AccessLevel.View;
    
    // Условия доступа
    public bool RequiresPayment { get; set; } = true;
    public bool PaymentVerified { get; set; }
    public Guid? PaymentId { get; set; }
    
    // Ограничения
    public bool CanDownload { get; set; }
    public bool CanClone { get; set; }
    public bool CanViewHistory { get; set; } = true;
    
    // Временные ограничения
    public DateTimeOffset? ExpiresAt { get; set; }
    
    // Метаданные
    public Guid? GrantedById { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsExpired() =>
        ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;

    public bool HasAccess() =>
        !IsExpired() && AccessLevel != AccessLevel.None;

    public void VerifyPayment(Guid paymentId, DateTimeOffset now)
    {
        PaymentVerified = true;
        PaymentId = paymentId;
        UpdatedAt = now;
    }
}

/// <summary>Repository view history entity</summary>
public class RepositoryView
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    public Guid UserId { get; set; }
    
    // Что просматривалось
    public string? FilePath { get; set; }
    public RepositoryViewAction Action { get; set; }
    
    // IP и метаданные
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    
    // Результат
    public bool Success { get; set; } = true;
    public string? BlockedReason { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Download token entity</summary>
public class DownloadToken
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    public Guid UserId { get; set; }
    
    public string Token { get; set; } = string.Empty;
    
    // Ограничения
    public int MaxDownloads { get; set; } = 1;
    public int DownloadsCount { get; set; }
    
    // Срок действия
    public DateTimeOffset ExpiresAt { get; set; }
    
    // Что можно скачать
    public List<string>? AllowedFiles { get; set; }  // Список файлов или null для всех
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }

    public bool IsExpired() =>
        DateTimeOffset.UtcNow > ExpiresAt;

    public bool CanDownload() =>
        !IsExpired() && DownloadsCount < MaxDownloads;

    public void RecordDownload(DateTimeOffset now)
    {
        DownloadsCount++;
        UsedAt = now;
    }

    public bool IsFileAllowed(string filePath) =>
        AllowedFiles == null || AllowedFiles.Contains(filePath);
}
