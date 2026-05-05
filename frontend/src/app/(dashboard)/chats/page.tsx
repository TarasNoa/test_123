'use client'

import { useEffect, useState, useRef } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import { useAuth } from '@/lib/auth'
import { chatApi, ChatDto, MessageDto, MessageType } from '@/lib/chat-api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { MessageCircle, Users, UserPlus, Send, Loader2, ArrowLeft, Code2, FileText, Sparkles } from 'lucide-react'
import { cn } from '@/lib/utils'
import { api } from '@/lib/api'
import { startRun, listRuns } from '@/lib/app-generation-api'
import { buildIdePrefillQuery, buildIdePromptFromContext } from '@/lib/ide-handoff'
import type { MarketplaceTask } from '@/lib/marketplace'
import { profilesApi } from '@/lib/profiles-api'
import { detectPreferredLanguage, getLanguageLabel, translationApi } from '@/lib/translation-api'

export default function ChatsPage() {
  const { user } = useAuth()
  const router = useRouter()
  const searchParams = useSearchParams()
  const activeChatId = searchParams.get('chat')
  const taskIdFromSearch = searchParams.get('taskId')
  const autoOpened = searchParams.get('auto') === '1'

  const [chats, setChats] = useState<ChatDto[]>([])
  const [messages, setMessages] = useState<MessageDto[]>([])
  const [relatedTask, setRelatedTask] = useState<MarketplaceTask | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [messagesLoading, setMessagesLoading] = useState(false)
  const [newMessage, setNewMessage] = useState('')
  const [sending, setSending] = useState(false)
  const [openingIde, setOpeningIde] = useState(false)
  const [targetLanguage, setTargetLanguage] = useState('en')
  const [translatedByMessageId, setTranslatedByMessageId] = useState<Record<string, string>>({})
  const [translationLoading, setTranslationLoading] = useState(false)
  const bottomRef = useRef<HTMLDivElement>(null)
  const activeChat = chats.find((c) => c.id === activeChatId)

  useEffect(() => {
    loadChats()
  }, [user])

  useEffect(() => {
    let cancelled = false

    async function loadMyLanguage() {
      try {
        const profile = await profilesApi.getMyProfile()
        if (cancelled) return
        setTargetLanguage(detectPreferredLanguage(profile.languages, navigator.language))
      } catch {
        if (!cancelled) {
          setTargetLanguage(detectPreferredLanguage([], navigator.language))
        }
      }
    }

    if (user) {
      loadMyLanguage()
    }

    return () => {
      cancelled = true
    }
  }, [user])

  useEffect(() => {
    if (activeChatId) loadMessages(activeChatId)
  }, [activeChatId])

  useEffect(() => {
    const taskId = activeChat?.relatedTaskId ?? taskIdFromSearch
    if (!taskId) {
      setRelatedTask(null)
      return
    }

    api<MarketplaceTask>(`/tasks/${taskId}`, { auth: false })
      .then((task) => setRelatedTask(task))
      .catch(() => setRelatedTask(null))
  }, [activeChat?.relatedTaskId, taskIdFromSearch])

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  useEffect(() => {
    const candidates = messages.filter((message) =>
      message.senderId !== user?.id &&
      message.type === MessageType.Text &&
      !!message.content.trim() &&
      !translatedByMessageId[message.id]
    )

    if (candidates.length === 0 || !targetLanguage) {
      return
    }

    let cancelled = false

    async function translateMessages() {
      try {
        setTranslationLoading(true)
        const response = await translationApi.translateBatch(
          candidates.map((message) => message.content),
          targetLanguage
        )

        if (cancelled) return

        const nextMap: Record<string, string> = {}
        candidates.forEach((message, index) => {
          nextMap[message.id] = response.items[index] ?? message.content
        })

        setTranslatedByMessageId((prev) => ({ ...prev, ...nextMap }))
      } catch {
        // Ignore translation failures and keep original text.
      } finally {
        if (!cancelled) setTranslationLoading(false)
      }
    }

    translateMessages()

    return () => {
      cancelled = true
    }
  }, [messages, targetLanguage, user?.id, translatedByMessageId])

  async function loadChats() {
    try {
      setIsLoading(true)
      const data = await chatApi.getMyChats()
      setChats(data.items)
    } catch {}
    setIsLoading(false)
  }

  async function loadMessages(chatId: string) {
    try {
      setMessagesLoading(true)
      setTranslatedByMessageId({})
      const data = await chatApi.getMessages(chatId)
      setMessages(data.items)
    } catch {}
    setMessagesLoading(false)
  }

  async function handleSend(e: React.FormEvent) {
    e.preventDefault()
    if (!newMessage.trim() || !activeChatId) return
    setSending(true)
    try {
      await chatApi.sendMessage(activeChatId, newMessage.trim())
      setNewMessage('')
      await loadMessages(activeChatId)
    } catch {}
    setSending(false)
  }

  function selectChat(chatId: string) {
    router.push(`/chats?chat=${chatId}`)
  }

  const fileMessages = messages.filter((message) => message.type === MessageType.File || !!message.fileUrl || !!message.fileName)

  async function handleOpenIde() {
    if (!activeChatId || openingIde) return

    setOpeningIde(true)
    try {
      const prompt = buildIdePromptFromContext({
        task: relatedTask,
        messages,
      })

      if (fileMessages.length > 0) {
        await startRun({ userRequest: prompt, maxIterations: 20 })
        const runs = await listRuns()
        const newestRunId = runs[0]?.id
        if (newestRunId) {
          router.push(`/ide/${newestRunId}`)
          return
        }
      }

      router.push(buildIdePrefillQuery(prompt, { taskId: relatedTask?.id ?? null, chatId: activeChatId }))
    } finally {
      setOpeningIde(false)
    }
  }

  return (
    <div className="flex h-[calc(100vh-3.5rem-3rem)] rounded-lg border bg-card overflow-hidden">
      {/* Chat list */}
      <div className={cn(
        'w-full sm:w-80 shrink-0 border-r flex flex-col',
        activeChatId && 'hidden sm:flex'
      )}>
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="font-semibold">Чаты</h2>
          <Badge variant="secondary">{chats.length}</Badge>
        </div>
        <ScrollArea className="flex-1">
          {isLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
            </div>
          ) : chats.length === 0 ? (
            <div className="flex flex-col items-center gap-2 py-8 text-center px-4">
              <MessageCircle className="h-8 w-8 text-muted-foreground" />
              <p className="text-sm text-muted-foreground">Нет чатов</p>
            </div>
          ) : (
            <div className="divide-y">
              {chats.map((chat) => (
                <button
                  key={chat.id}
                  onClick={() => selectChat(chat.id)}
                  className={cn(
                    'w-full flex items-start gap-3 p-3 text-left hover:bg-accent/50 transition-colors',
                    chat.id === activeChatId && 'bg-accent'
                  )}
                >
                  <Avatar className="h-9 w-9 shrink-0">
                    <AvatarFallback className="text-xs bg-primary/10 text-primary">
                      {chat.type === 'Direct' ? <UserPlus className="h-4 w-4" /> : <Users className="h-4 w-4" />}
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2">
                      <span className="text-sm font-medium truncate">{chat.title}</span>
                      {chat.unreadCount > 0 && (
                        <Badge className="h-5 w-5 shrink-0 p-0 flex items-center justify-center text-[10px]">
                          {chat.unreadCount}
                        </Badge>
                      )}
                    </div>
                    <p className="text-xs text-muted-foreground truncate">
                      {chat.lastMessage
                        ? `${chat.lastMessage.senderName}: ${chat.lastMessage.content}`
                        : 'Нет сообщений'}
                    </p>
                  </div>
                </button>
              ))}
            </div>
          )}
        </ScrollArea>
      </div>

      {/* Messages area */}
      <div className={cn(
        'flex-1 flex flex-col',
        !activeChatId && 'hidden sm:flex'
      )}>
        {!activeChatId ? (
          <div className="flex flex-1 items-center justify-center text-muted-foreground">
            <div className="text-center space-y-2">
              <MessageCircle className="mx-auto h-10 w-10" />
              <p>Выберите чат</p>
            </div>
          </div>
        ) : (
          <>
            {/* Chat header */}
            <div className="border-b">
              <div className="flex items-center gap-3 p-3">
                <Button
                  variant="ghost"
                  size="icon"
                  className="sm:hidden"
                  onClick={() => router.push('/chats')}
                >
                  <ArrowLeft className="h-4 w-4" />
                </Button>
                <div className="flex-1 min-w-0">
                  <p className="font-medium text-sm truncate">{activeChat?.title}</p>
                  <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                    <span>{activeChat?.memberCount} участников</span>
                    <span>•</span>
                    <span>Автоперевод: {getLanguageLabel(targetLanguage)}</span>
                    {translationLoading && <Loader2 className="h-3 w-3 animate-spin" />}
                  </div>
                </div>
                <Button size="sm" variant={fileMessages.length > 0 ? 'default' : 'secondary'} onClick={handleOpenIde} disabled={openingIde}>
                  <Code2 className="mr-2 h-4 w-4" />
                  {openingIde ? 'Открываю IDE...' : 'Перейти в IDE'}
                </Button>
              </div>

              {(relatedTask || autoOpened) && (
                <div className="mx-3 mb-3 rounded-xl border border-secondary/20 bg-gradient-to-r from-secondary/15 via-background to-primary/10 p-3">
                  <div className="flex flex-col gap-2 lg:flex-row lg:items-center lg:justify-between">
                    <div className="space-y-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <Badge variant="secondary">
                          <Sparkles className="mr-1 h-3.5 w-3.5" />
                          Рабочий чат
                        </Badge>
                        {fileMessages.length > 0 && (
                          <Badge variant="outline">
                            <FileText className="mr-1 h-3.5 w-3.5" />
                            {fileMessages.length} файлов в контексте
                          </Badge>
                        )}
                      </div>
                      <p className="text-sm font-medium">
                        {relatedTask ? `Заказ: ${relatedTask.title}` : 'Заявка одобрена, можно согласовать детали с заказчиком.'}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {fileMessages.length > 0
                          ? 'Вложения уже есть в переписке, поэтому IDE можно открыть сразу без ручного описания.'
                          : 'Если файлов нет, кнопка IDE откроет экран генерации с уже заполненным prompt из заказа и чата.'}
                      </p>
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Messages */}
            <ScrollArea className="flex-1 p-4">
              {messagesLoading ? (
                <div className="flex items-center justify-center py-8">
                  <Loader2 className="h-5 w-5 animate-spin" />
                </div>
              ) : messages.length === 0 ? (
                <p className="text-center text-sm text-muted-foreground py-8">Нет сообщений</p>
              ) : (
                <div className="space-y-3">
                  {messages.map((msg) => {
                    const isMe = msg.senderId === user?.id
                    const translatedContent = !isMe ? translatedByMessageId[msg.id] : undefined
                    const hasTranslatedVersion =
                      !!translatedContent &&
                      translatedContent.trim().length > 0 &&
                      translatedContent.trim() !== msg.content.trim()
                    const displayContent = hasTranslatedVersion ? translatedContent : msg.content
                    return (
                      <div key={msg.id} className={cn('flex', isMe ? 'justify-end' : 'justify-start')}>
                        <div className={cn(
                          'max-w-[75%] rounded-lg px-3 py-2',
                          isMe ? 'bg-primary text-primary-foreground' : 'bg-muted'
                        )}>
                          {!isMe && <p className="text-xs font-medium mb-0.5 opacity-75">{msg.senderName}</p>}
                          <p className="text-sm whitespace-pre-wrap">{displayContent}</p>
                          {hasTranslatedVersion && (
                            <p className={cn(
                              'mt-2 whitespace-pre-wrap rounded-md border px-2 py-1 text-xs',
                              isMe
                                ? 'border-primary-foreground/15 text-primary-foreground/75'
                                : 'border-border text-muted-foreground'
                            )}>
                              Оригинал: {msg.content}
                            </p>
                          )}
                          {(msg.type === MessageType.File || msg.fileUrl || msg.fileName) && (
                            <a
                              href={msg.fileUrl ?? '#'}
                              target="_blank"
                              rel="noreferrer"
                              className={cn(
                                'mt-2 inline-flex items-center gap-2 rounded-lg border px-2.5 py-1.5 text-xs',
                                isMe ? 'border-primary-foreground/20 text-primary-foreground' : 'border-border text-foreground'
                              )}
                            >
                              <FileText className="h-3.5 w-3.5" />
                              {msg.fileName ?? 'Прикреплённый файл'}
                            </a>
                          )}
                          <p className={cn('text-[10px] mt-1', isMe ? 'text-primary-foreground/60' : 'text-muted-foreground')}>
                            {new Date(msg.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                          </p>
                        </div>
                      </div>
                    )
                  })}
                  <div ref={bottomRef} />
                </div>
              )}
            </ScrollArea>

            {/* Input */}
            <form onSubmit={handleSend} className="flex items-center gap-2 border-t p-3">
              <Input
                placeholder="Сообщение..."
                value={newMessage}
                onChange={(e) => setNewMessage(e.target.value)}
                disabled={sending}
                className="flex-1"
              />
              <Button type="submit" size="icon" disabled={sending || !newMessage.trim()}>
                <Send className="h-4 w-4" />
              </Button>
            </form>
          </>
        )}
      </div>
    </div>
  )
}
