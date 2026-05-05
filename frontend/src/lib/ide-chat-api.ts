/**
 * Client for the IDE AI conversation API (/api/ai/*).
 * Supports multi-turn conversations tied to app-generation runs.
 */

import { api } from './api'

export interface ConversationDto {
  id: string
  title?: string
  createdAt: string
  updatedAt?: string
}

export interface MessageDto {
  id: string
  conversationId: string
  role: 'User' | 'Assistant' | 'System'
  content: string
  createdAt: string
  score?: number | null
}

export interface ChatResponseDto {
  messageId: string
  conversationId: string
  response: string
}

export const ideChatApi = {
  createConversation: (title?: string) =>
    api<ConversationDto>('/api/ai/conversations', {
      method: 'POST',
      body: JSON.stringify({ title }),
    }),

  listConversations: () =>
    api<ConversationDto[]>('/api/ai/conversations'),

  getMessages: (conversationId: string) =>
    api<MessageDto[]>(`/api/ai/conversations/${conversationId}/messages`),

  sendMessage: (message: string, conversationId?: string) =>
    api<ChatResponseDto>('/api/ai/chat', {
      method: 'POST',
      body: JSON.stringify({ message, conversationId }),
    }),

  scoreMessage: (messageId: string, score: number) =>
    api<void>(`/api/ai/chat/${messageId}/score`, {
      method: 'POST',
      body: JSON.stringify({ score }),
    }),
}
