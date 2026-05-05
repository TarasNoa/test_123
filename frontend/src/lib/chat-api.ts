import { api } from './api'

export enum ChatType {
  Direct = 'Direct',
  Group = 'Group',
  TaskRelated = 'TaskRelated'
}

export enum ChatMemberRole {
  Member = 'Member',
  Admin = 'Admin',
  Owner = 'Owner'
}

export enum MessageType {
  Text = 'Text',
  Image = 'Image',
  File = 'File',
  System = 'System'
}

export enum MessageStatus {
  Sent = 'Sent',
  Delivered = 'Delivered',
  Read = 'Read'
}

export interface ChatDto {
  id: string
  title: string
  type: ChatType
  relatedTaskId?: string
  createdAt: string
  isArchived: boolean
  memberCount: number
  unreadCount: number
  lastMessage?: MessageDto
}

export interface ChatMemberDto {
  id: string
  userId: string
  role: ChatMemberRole
  joinedAt: string
  lastReadAt?: string
}

export interface ChatDetailDto {
  id: string
  title: string
  type: ChatType
  relatedTaskId?: string
  createdAt: string
  isArchived: boolean
  members: ChatMemberDto[]
}

export interface MessageDto {
  id: string
  chatId: string
  senderId: string
  senderName: string
  content: string
  type: MessageType
  status: MessageStatus
  sentAt: string
  editedAt?: string
  isDeleted: boolean
  fileUrl?: string
  fileName?: string
  fileSize?: number
  replyToMessageId?: string
}

export interface NotificationDto {
  id: string
  type: string
  title: string
  message: string
  priority: string
  isRead: boolean
  createdAt: string
  readAt?: string
  actionUrl?: string
  relatedEntityId?: string
  relatedEntityType?: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export const chatApi = {
  // Chats
  getMyChats: (page = 1, pageSize = 20) =>
    api<PagedResult<ChatDto>>(`/api/v1/chats/my?page=${page}&pageSize=${pageSize}`),

  getChat: (chatId: string) =>
    api<ChatDetailDto>(`/api/v1/chats/${chatId}`),

  createDirectChat: (otherUserId: string) =>
    api<string>(`/api/v1/chats/direct`, {
      method: 'POST',
      body: JSON.stringify({ otherUserId })
    }),

  createGroupChat: (title: string, memberIds: string[], relatedTaskId?: string) =>
    api<string>(`/api/v1/chats/group`, {
      method: 'POST',
      body: JSON.stringify({ title, memberIds, relatedTaskId })
    }),

  joinChat: (chatId: string) =>
    api<void>(`/api/v1/chats/${chatId}/join`, { method: 'POST' }),

  leaveChat: (chatId: string) =>
    api<void>(`/api/v1/chats/${chatId}/leave`, { method: 'POST' }),

  // Messages
  getMessages: (chatId: string, page = 1, pageSize = 50) =>
    api<PagedResult<MessageDto>>(`/api/v1/messages/chat/${chatId}?page=${page}&pageSize=${pageSize}`),

  sendMessage: (chatId: string, content: string, type = MessageType.Text, replyToId?: string, fileUrl?: string, fileName?: string, fileSize?: number) =>
    api<string>(`/api/v1/messages/send`, {
      method: 'POST',
      body: JSON.stringify({ chatId, content, type, replyToMessageId: replyToId, fileUrl, fileName, fileSize })
    }),

  editMessage: (messageId: string, newContent: string) =>
    api<void>(`/api/v1/messages/${messageId}`, {
      method: 'PUT',
      body: JSON.stringify({ newContent })
    }),

  deleteMessage: (messageId: string) =>
    api<void>(`/api/v1/messages/${messageId}`, { method: 'DELETE' }),

  // Notifications
  getNotifications: (unreadOnly = false, page = 1, pageSize = 20) =>
    api<PagedResult<NotificationDto>>(`/api/v1/notifications/my?unreadOnly=${unreadOnly}&page=${page}&pageSize=${pageSize}`),

  markAsRead: (notificationId: string) =>
    api<void>(`/api/v1/notifications/${notificationId}/read`, { method: 'POST' }),

  markAllAsRead: () =>
    api<{ markedAsRead: number }>(`/api/v1/notifications/read-all`, { method: 'POST' }),
}
