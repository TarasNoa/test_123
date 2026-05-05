namespace Libr4.Chat.Domain.FSharp

open System

/// Chat Domain Types
type MessageStatus =
    | Sending
    | Sent of sentAt: DateTime
    | Delivered of deliveredAt: DateTime
    | Read of readAt: DateTime
    | Failed of error: string

type MessageType =
    | Text
    | Image of url: string
    | File of fileName: string * url: string
    | Voice of duration: int // seconds
    | System

type ChatMessage = {
    Id: Guid
    Content: string
    Type: MessageType
    SenderId: Guid
    ReceiverId: Guid option // None for group messages
    ChannelId: Guid
    Status: MessageStatus
    ReplyToMessageId: Guid option
    EditedAt: DateTime option
    CreatedAt: DateTime
}

type ChannelType =
    | Direct // 1:1
    | Group // Many:many
    | Broadcast // 1:many (read-only for others)

type Channel = {
    Id: Guid
    Name: string option
    Type: ChannelType
    ParticipantIds: Guid list
    AdminIds: Guid list
    LastMessageAt: DateTime option
    CreatedAt: DateTime
}

type UserPresence =
    | Online
    | Away of since: DateTime
    | Offline of lastSeen: DateTime option
    | DoNotDisturb

type ChatUser = {
    Id: Guid
    Username: string
    DisplayName: string
    AvatarUrl: string option
    Presence: UserPresence
    StatusMessage: string option
}

/// Chat Operations
module ChatOperations =
    let createMessage content senderId channelId messageType =
        {
            Id = Guid.NewGuid()
            Content = content
            Type = messageType
            SenderId = senderId
            ReceiverId = None
            ChannelId = channelId
            Status = Sending
            ReplyToMessageId = None
            EditedAt = None
            CreatedAt = DateTime.UtcNow
        }
    
    let createDirectMessage content senderId receiverId =
        {
            Id = Guid.NewGuid()
            Content = content
            Type = Text
            SenderId = senderId
            ReceiverId = Some receiverId
            ChannelId = Guid.NewGuid() // Will be replaced with actual channel
            Status = Sending
            ReplyToMessageId = None
            EditedAt = None
            CreatedAt = DateTime.UtcNow
        }
    
    let markAsSent message =
        { message with Status = Sent DateTime.UtcNow }
    
    let markAsDelivered message =
        { message with Status = Delivered DateTime.UtcNow }
    
    let markAsRead message =
        { message with Status = Read DateTime.UtcNow }
    
    let markAsFailed error message =
        { message with Status = Failed error }
    
    let editMessage newContent message =
        { message with Content = newContent; EditedAt = Some DateTime.UtcNow }
    
    let createReply content senderId channelId replyToMessageId =
        {
            Id = Guid.NewGuid()
            Content = content
            Type = Text
            SenderId = senderId
            ReceiverId = None
            ChannelId = channelId
            Status = Sending
            ReplyToMessageId = Some replyToMessageId
            EditedAt = None
            CreatedAt = DateTime.UtcNow
        }
    
    let createChannel channelType name participantIds =
        {
            Id = Guid.NewGuid()
            Name = name
            Type = channelType
            ParticipantIds = participantIds
            AdminIds = [participantIds |> List.head] // First participant is admin
            LastMessageAt = None
            CreatedAt = DateTime.UtcNow
        }
    
    let addParticipant userId channel =
        if channel.ParticipantIds |> List.contains userId then
            channel
        else
            { channel with ParticipantIds = userId :: channel.ParticipantIds }
    
    let removeParticipant userId channel =
        { channel with ParticipantIds = channel.ParticipantIds |> List.filter (fun id -> id <> userId) }
    
    let updateLastMessage channel =
        { channel with LastMessageAt = Some DateTime.UtcNow }
    
    let setUserOnline user =
        { user with Presence = Online }
    
    let setUserAway user =
        { user with Presence = Away DateTime.UtcNow }
    
    let setUserOffline user =
        { user with Presence = Offline (Some DateTime.UtcNow) }
    
    let setDoNotDisturb user =
        { user with Presence = DoNotDisturb }
    
    let getUnreadMessages userId messages =
        messages 
        |> List.filter (fun m -> m.SenderId <> userId)
        |> List.filter (fun m -> match m.Status with | Read _ -> false | _ -> true)
    
    let formatMessagePreview (message: ChatMessage) maxLength =
        let text = match message.Type with
            | Text -> message.Content
            | Image _ -> "[Image]"
            | File (name, _) -> sprintf "[File: %s]" name
            | Voice duration -> sprintf "[Voice: %ds]" duration
            | System -> message.Content
        
        if text.Length > maxLength then
            text.Substring(0, maxLength - 3) + "..."
        else
            text
