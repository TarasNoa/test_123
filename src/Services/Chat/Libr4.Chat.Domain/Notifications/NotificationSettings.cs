using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.Notifications;

public enum NotificationChannel
{
    InApp,
    Email,
    Push,
    SMS
}

public class NotificationSettings : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool PushEnabled { get; private set; }
    public bool SmsEnabled { get; private set; }
    public bool InAppEnabled { get; private set; }
    public string? EmailAddress { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool DailyDigest { get; private set; }
    public bool WeeklyDigest { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<NotificationPreference> _preferences = new();
    public IReadOnlyCollection<NotificationPreference> Preferences => _preferences.AsReadOnly();

    private NotificationSettings() { }

    public static NotificationSettings Create(Guid userId, string? emailAddress = null, string? phoneNumber = null)
    {
        return new NotificationSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmailEnabled = true,
            PushEnabled = true,
            SmsEnabled = false,
            InAppEnabled = true,
            EmailAddress = emailAddress,
            PhoneNumber = phoneNumber,
            DailyDigest = false,
            WeeklyDigest = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateChannel(NotificationChannel channel, bool enabled)
    {
        switch (channel)
        {
            case NotificationChannel.Email:
                EmailEnabled = enabled;
                break;
            case NotificationChannel.Push:
                PushEnabled = enabled;
                break;
            case NotificationChannel.SMS:
                SmsEnabled = enabled;
                break;
            case NotificationChannel.InApp:
                InAppEnabled = enabled;
                break;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEmailAddress(string emailAddress)
    {
        EmailAddress = emailAddress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePhoneNumber(string phoneNumber)
    {
        PhoneNumber = phoneNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDailyDigest(bool enabled)
    {
        DailyDigest = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetWeeklyDigest(bool enabled)
    {
        WeeklyDigest = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddPreference(NotificationPreference preference)
    {
        _preferences.Add(preference);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemovePreference(Guid preferenceId)
    {
        var preference = _preferences.FirstOrDefault(p => p.Id == preferenceId);
        if (preference != null)
        {
            _preferences.Remove(preference);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public bool IsChannelEnabled(NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.Email => EmailEnabled,
            NotificationChannel.Push => PushEnabled,
            NotificationChannel.SMS => SmsEnabled,
            NotificationChannel.InApp => InAppEnabled,
            _ => false
        };
    }
}

public class NotificationPreference : Entity<Guid>
{
    public Guid NotificationSettingsId { get; private set; }
    public string NotificationType { get; private set; } = string.Empty;
    public bool Enabled { get; private set; }
    public NotificationChannel PreferredChannel { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private NotificationPreference() { }

    public static NotificationPreference Create(Guid notificationSettingsId, string notificationType, bool enabled, NotificationChannel preferredChannel)
    {
        return new NotificationPreference
        {
            Id = Guid.NewGuid(),
            NotificationSettingsId = notificationSettingsId,
            NotificationType = notificationType,
            Enabled = enabled,
            PreferredChannel = preferredChannel,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateEnabled(bool enabled)
    {
        Enabled = enabled;
    }

    public void UpdateChannel(NotificationChannel channel)
    {
        PreferredChannel = channel;
    }
}
