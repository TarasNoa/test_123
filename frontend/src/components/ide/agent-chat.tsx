'use client'

import * as React from 'react'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import {
  Send,
  Loader2,
  Play,
  Pause,
  Square,
  Bot,
  User,
  Sparkles,
  Settings2,
  Globe,
} from 'lucide-react'
import type { AppGenerationReport } from '@/lib/app-generation-api'
import { cn } from '@/lib/utils'
import { useTranslation } from '@/hooks/useTranslation'
import { MessageContent, CodeBlock } from '@/components/ui/code-block'
import { TerminalOutputCard } from './Terminal'
import { Lightbulb, Terminal, Hammer, Shield, CheckCircle2, XCircle, Cpu, Workflow, Users, ArrowRight, ChevronRight, ChevronDown, Layers } from 'lucide-react'

export interface AgentInfo {
  id: string
  name: string
  role: string
  description?: string
  status: 'idle' | 'working' | 'completed' | 'failed'
  subAgents?: AgentInfo[]
  purpose?: string // Зачем вызван этот агент
  input?: string // Что передали агенту
  output?: string // Что вернул агент
}

export interface ChatMessage {
  id: string
  role: 'user' | 'assistant' | 'system'
  content: string
  timestamp: Date
  type?: 'generation-start' | 'generation-complete' | 'generation-failed' | 'follow-up' | 'info' | 'thinking' | 'terminal-output' | 'build-start' | 'build-complete' | 'test-start' | 'test-complete' | 'security-scan' | 'agent-call'
  thinking?: string // Мысли агента (reasoning process)
  codeBlocks?: Array<{ code: string; language: string; filename?: string }>
  terminalOutput?: {
    command: string
    output: string
    exitCode?: number
    durationMs?: number
  }
  agentOrchestration?: {
    rootAgent: AgentInfo
    triggeredBy?: string // Кто вызвал (LLM, user, system)
    timestamp: string
  }
}

interface AgentChatProps {
  report: AppGenerationReport | null
  isRunning: boolean
  runId: string | null
  onStartGeneration: (prompt: string, maxIterations: number) => Promise<void>
  onFollowUp: (prompt: string) => Promise<void>
  onPause: () => void
  onResume: () => void
  onCancel: () => void
}

export function AgentChat({
  report,
  isRunning,
  runId,
  onStartGeneration,
  onFollowUp,
  onPause,
  onResume,
  onCancel,
}: AgentChatProps) {
  const [messages, setMessages] = React.useState<ChatMessage[]>([])
  const [input, setInput] = React.useState('')
  const [sending, setSending] = React.useState(false)
  const [showSettings, setShowSettings] = React.useState(false)
  const [maxIter, setMaxIter] = React.useState(20)
  const bottomRef = React.useRef<HTMLDivElement>(null)
  const textareaRef = React.useRef<HTMLTextAreaElement>(null)
  
  const { targetLanguageLabel, isTranslating, translateContent } = useTranslation()
  
  // Переводим сообщения при изменении языка
  React.useEffect(() => {
    const translateMessages = async () => {
      if (messages.length === 0) return
      
      const assistantMessages = messages.filter(m => m.role === 'assistant')
      if (assistantMessages.length === 0) return
      
      const translatedMessages = await Promise.all(
        assistantMessages.map(async (msg) => {
          const translated = await translateContent(msg.content)
          return { ...msg, content: translated }
        })
      )
      
      setMessages(prev => prev.map(msg => {
        if (msg.role === 'assistant') {
          const translated = translatedMessages.find(t => t.id === msg.id)
          return translated || msg
        }
        return msg
      }))
    }
    
    translateMessages()
  }, [targetLanguageLabel])

  const hasRun = !!runId || !!report
  const isComplete = report?.status === 'Completed'
  const isFailed = report?.status === 'Failed' || report?.status === 'Cancelled'

  React.useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, report])

  React.useEffect(() => {
    (async () => {
      if (report?.status === 'Completed' && messages.length > 0) {
        const lastMsg = messages[messages.length - 1]
        if (lastMsg.type !== 'generation-complete') {
          const fileCount = report.files?.length ?? 0
          await addAssistantMessage(
            `Генерация завершена! Создано ${fileCount} файлов. Можете редактировать код или попросите меня что-то изменить.`,
            'generation-complete'
          )
        }
      }
      if (report?.status === 'Failed' && messages.length > 0) {
        const lastMsg = messages[messages.length - 1]
        if (lastMsg.type !== 'generation-failed') {
          await addAssistantMessage(
            `Генерация завершилась с ошибкой: ${report.failureReason ?? 'Неизвестная ошибка'}. Попробуйте ещё раз или уточните требования.`,
            'generation-failed'
          )
        }
      }
    })()
  }, [report?.status])

  function addUserMessage(content: string) {
    setMessages(prev => [...prev, {
      id: `user-${Date.now()}`,
      role: 'user',
      content,
      timestamp: new Date(),
    }])
  }

  async function addAssistantMessage(content: string, type?: ChatMessage['type']) {
    // Переводим сообщение перед добавлением
    const translatedContent = await translateContent(content)
    
    setMessages(prev => [...prev, {
      id: `assistant-${Date.now()}`,
      role: 'assistant',
      content: translatedContent,
      timestamp: new Date(),
      type,
    }])
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const text = input.trim()
    if (!text || sending) return

    setInput('')
    addUserMessage(text)
    setSending(true)

    try {
      if (!hasRun) {
        await addAssistantMessage(
          'Запускаю генерацию приложения по вашему описанию...',
          'generation-start'
        )
        await onStartGeneration(text, maxIter)
      } else if (isRunning) {
        await addAssistantMessage(
          'Генерация ещё в процессе. Дождитесь завершения, а потом я смогу внести изменения.',
          'info'
        )
      } else {
        await addAssistantMessage(
          isComplete
            ? 'Принял правки. Запускаю новую итерацию с учётом вашего сообщения...'
            : isFailed
              ? 'Возобновляю проблемный запуск и пробую исправить его по вашему сообщению...'
              : 'Продолжаю работу над проектом по вашему сообщению...',
          'follow-up'
        )
        await onFollowUp(text)
      }
    } catch (err) {
      await addAssistantMessage(
        `Ошибка: ${err instanceof Error ? err.message : 'Неизвестная ошибка'}`,
        'generation-failed'
      )
    } finally {
      setSending(false)
    }
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSubmit(e)
    }
  }

  const status = report?.status
  const isPaused = status === 'Paused'

  return (
    <div className="flex h-full flex-col">
      {/* Header */}
      <div className="flex items-center justify-between border-b px-3 py-2">
        <div className="flex items-center gap-2">
          <Sparkles className="h-4 w-4 text-secondary" />
          <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            AI Agent
          </span>
        </div>
        <div className="flex items-center gap-1.5">
          {status && (
            <Badge
              variant={
                status === 'Completed' ? 'default' :
                status === 'Failed' || status === 'Cancelled' ? 'destructive' :
                'secondary'
              }
              className="text-[10px]"
            >
              {status}
            </Badge>
          )}
          {/* Индикатор языка */}
          <Badge variant="outline" className="text-[10px] gap-1">
            <Globe className="h-3 w-3" />
            {targetLanguageLabel}
          </Badge>
          {isTranslating && (
            <Loader2 className="h-3 w-3 animate-spin text-muted-foreground" />
          )}
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6"
            onClick={() => setShowSettings(!showSettings)}
          >
            <Settings2 className="h-3 w-3" />
          </Button>
        </div>
      </div>

      {/* Settings dropdown */}
      {showSettings && (
        <div className="border-b p-3 bg-muted/30 space-y-2">
          <div className="flex items-center justify-between">
            <label className="text-xs text-muted-foreground">Макс. итераций:</label>
            <input
              type="number"
              min={1}
              max={50}
              value={maxIter}
              onChange={(e) => setMaxIter(Number(e.target.value))}
              className="w-16 rounded border bg-background px-2 py-0.5 text-xs"
            />
          </div>
        </div>
      )}

      {/* Messages */}
      <ScrollArea className="flex-1">
        <div className="p-3 space-y-3">
          {messages.length === 0 && !report && (
            <div className="flex flex-col items-center justify-center py-8 text-center space-y-3">
              <div className="flex h-12 w-12 items-center justify-center rounded-full bg-secondary/10">
                <Bot className="h-6 w-6 text-secondary" />
              </div>
              <div className="space-y-1">
                <p className="text-sm font-medium">Libr4 AI Agent</p>
                <p className="text-xs text-muted-foreground max-w-[200px]">
                  {targetLanguageLabel === 'Русский' 
                    ? 'Опишите приложение, и я сгенерирую его. Потом можете давать команды для доработки.'
                    : 'Describe the application, and I will generate it. Then you can give commands for improvements.'}
                </p>
              </div>
            </div>
          )}

          {/* Report info card (when generation is active) */}
          {report?.plan && messages.length > 0 && (
            <div className="rounded-lg border bg-muted/30 p-2.5 space-y-1.5">
              <div className="flex items-center gap-2">
                <Sparkles className="h-3 w-3 text-secondary" />
                <p className="text-xs font-semibold">{report.plan.applicationName}</p>
              </div>
              {report.plan.techStack?.languages && (
                <div className="flex flex-wrap gap-1">
                  {[...report.plan.techStack.languages ?? [], ...report.plan.techStack.frameworks ?? []].map((t) => (
                    <Badge key={t} variant="outline" className="text-[10px]">{t}</Badge>
                  ))}
                </div>
              )}
              {isRunning && report.iterations && report.iterations.length > 0 && (
                <div className="text-[10px] text-muted-foreground">
                  Итерация {report.iterations.length} из {report.plan.maxIterations ?? maxIter}
                </div>
              )}
            </div>
          )}

          {/* Chat messages */}
          {messages.map((msg) => (
            <div
              key={msg.id}
              className={cn(
                'flex gap-2',
                msg.role === 'user' ? 'justify-end' : 'justify-start'
              )}
            >
              {msg.role !== 'user' && (
                <div className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-secondary/10 mt-0.5">
                  <Bot className="h-3.5 w-3.5 text-secondary" />
                </div>
              )}
              <div
                className={cn(
                  'rounded-lg px-3 py-2 text-xs max-w-[85%]',
                  msg.role === 'user'
                    ? 'bg-primary text-primary-foreground'
                    : msg.type === 'generation-failed'
                    ? 'bg-destructive/10 text-destructive border border-destructive/20'
                    : msg.type === 'generation-complete'
                    ? 'bg-primary/10 text-primary border border-primary/20'
                    : msg.type === 'thinking'
                    ? 'bg-amber-50 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800'
                    : msg.type === 'build-start' || msg.type === 'test-start'
                    ? 'bg-blue-50 dark:bg-blue-950/20 border border-blue-200 dark:border-blue-800'
                    : msg.type === 'build-complete' || msg.type === 'test-complete'
                    ? 'bg-green-50 dark:bg-green-950/20 border border-green-200 dark:border-green-800'
                    : msg.type === 'security-scan'
                    ? 'bg-purple-50 dark:bg-purple-950/20 border border-purple-200 dark:border-purple-800'
                    : 'bg-muted'
                )}
              >
                {/* Индикатор типа события */}
                {msg.type === 'build-start' && (
                  <div className="flex items-center gap-1.5 mb-2 text-blue-600 dark:text-blue-400">
                    <Hammer className="h-3 w-3 animate-pulse" />
                    <span className="text-[10px] font-medium uppercase tracking-wider">
                      {targetLanguageLabel === 'Русский' ? 'Сборка...' : 'Building...'}
                    </span>
                  </div>
                )}
                {msg.type === 'build-complete' && (
                  <div className="flex items-center gap-1.5 mb-2 text-green-600 dark:text-green-400">
                    <CheckCircle2 className="h-3 w-3" />
                    <span className="text-[10px] font-medium uppercase tracking-wider">
                      {targetLanguageLabel === 'Русский' ? 'Сборка завершена' : 'Build Complete'}
                    </span>
                  </div>
                )}
                {msg.type === 'test-start' && (
                  <div className="flex items-center gap-1.5 mb-2 text-blue-600 dark:text-blue-400">
                    <Terminal className="h-3 w-3 animate-pulse" />
                    <span className="text-[10px] font-medium uppercase tracking-wider">
                      {targetLanguageLabel === 'Русский' ? 'Тестирование...' : 'Testing...'}
                    </span>
                  </div>
                )}
                {msg.type === 'test-complete' && (
                  <div className="flex items-center gap-1.5 mb-2 text-green-600 dark:text-green-400">
                    <CheckCircle2 className="h-3 w-3" />
                    <span className="text-[10px] font-medium uppercase tracking-wider">
                      {targetLanguageLabel === 'Русский' ? 'Тесты завершены' : 'Tests Complete'}
                    </span>
                  </div>
                )}
                {msg.type === 'security-scan' && (
                  <div className="flex items-center gap-1.5 mb-2 text-purple-600 dark:text-purple-400">
                    <Shield className="h-3 w-3 animate-pulse" />
                    <span className="text-[10px] font-medium uppercase tracking-wider">
                      {targetLanguageLabel === 'Русский' ? 'Проверка безопасности...' : 'Security Scan...'}
                    </span>
                  </div>
                )}
                {/* Индикатор мыслей агента */}
                {msg.thinking && (
                  <div className="mb-2 pb-2 border-b border-amber-200/50 dark:border-amber-800/50">
                    <div className="flex items-center gap-1.5 text-amber-600 dark:text-amber-400 mb-1">
                      <Lightbulb className="h-3 w-3" />
                      <span className="text-[10px] font-medium uppercase tracking-wider">
                        {targetLanguageLabel === 'Русский' ? 'Думаю...' : 'Thinking...'}
                      </span>
                    </div>
                    <p className="text-[11px] text-amber-700/80 dark:text-amber-300/80 italic">
                      {msg.thinking}
                    </p>
                  </div>
                )}
                {/* Code blocks indicator */}
                {msg.codeBlocks && msg.codeBlocks.length > 0 && (
                  <div className="flex items-center gap-1.5 mb-2 text-muted-foreground">
                    <Terminal className="h-3 w-3" />
                    <span className="text-[10px]">
                      {msg.codeBlocks.length} {targetLanguageLabel === 'Русский' ? 'файлов' : 'files'}
                    </span>
                  </div>
                )}
                {/* Terminal output card */}
                {msg.terminalOutput && (
                  <div className="mb-2">
                    <TerminalOutputCard
                      command={msg.terminalOutput.command}
                      output={msg.terminalOutput.output}
                      exitCode={msg.terminalOutput.exitCode}
                      durationMs={msg.terminalOutput.durationMs}
                    />
                  </div>
                )}
                {/* Agent orchestration card */}
                {msg.agentOrchestration && (
                  <div className="mb-2">
                    <AgentOrchestrationCard
                      orchestration={msg.agentOrchestration}
                      targetLanguageLabel={targetLanguageLabel}
                    />
                  </div>
                )}
                {/* Основной контент с code blocks */}
                <MessageContent content={msg.content} />
              </div>
              {msg.role === 'user' && (
                <div className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-primary/10 mt-0.5">
                  <User className="h-3.5 w-3.5 text-primary" />
                </div>
              )}
            </div>
          ))}

          {/* Loading indicator during generation */}
          {isRunning && (
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <Loader2 className="h-3 w-3 animate-spin" />
              <span>
                {status === 'Planning' ? 'Планирование...' :
                 status === 'Generating' ? 'Генерация кода...' :
                 status === 'Testing' ? 'Тестирование...' :
                 'Обработка...'}
              </span>
            </div>
          )}

          {/* Quality gates */}
          {report?.qualityGates && report.qualityGates.length > 0 && (
            <div className="space-y-1">
              <p className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
                Quality Gates
              </p>
              {report.qualityGates.map((gate, i) => (
                <div key={i} className="flex items-center justify-between rounded border px-2 py-1">
                  <span className="text-[11px]">{gate.stage}</span>
                  <div className="flex items-center gap-1.5">
                    <span className="text-[11px] font-mono">{gate.score}/10</span>
                    <div className={`h-2 w-2 rounded-full ${gate.passed ? 'bg-primary' : 'bg-destructive'}`} />
                  </div>
                </div>
              ))}
            </div>
          )}

          <div ref={bottomRef} />
        </div>
      </ScrollArea>

      {/* Run controls */}
      {isRunning && (
        <div className="flex items-center gap-1 border-t px-3 py-2">
          {isPaused ? (
            <Button size="sm" variant="outline" onClick={onResume} className="text-xs h-7">
              <Play className="mr-1 h-3 w-3" /> Продолжить
            </Button>
          ) : (
            <Button size="sm" variant="outline" onClick={onPause} className="text-xs h-7">
              <Pause className="mr-1 h-3 w-3" /> Пауза
            </Button>
          )}
          <Button size="sm" variant="destructive" onClick={onCancel} className="text-xs h-7">
            <Square className="mr-1 h-3 w-3" /> Стоп
          </Button>
        </div>
      )}

      {/* Input */}
      <form onSubmit={handleSubmit} className="border-t p-2">
        <div className="relative">
          <Textarea
            ref={textareaRef}
            placeholder={
              isRunning
                ? targetLanguageLabel === 'Русский' ? 'Генерация в процессе...' : 'Generation in progress...'
                : !hasRun
                ? targetLanguageLabel === 'Русский' ? 'Опишите приложение для генерации...' : 'Describe the application...'
                : isComplete
                ? targetLanguageLabel === 'Русский' ? 'Попросите изменить код...' : 'Ask to modify the code...'
                : targetLanguageLabel === 'Русский' ? 'Введите сообщение...' : 'Type a message...'
            }
            rows={2}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            disabled={sending}
            className="resize-none text-xs pr-10 min-h-[52px]"
          />
          <Button
            type="submit"
            size="icon"
            className="absolute bottom-1.5 right-1.5 h-7 w-7"
            disabled={!input.trim() || sending}
          >
            {sending ? <Loader2 className="h-3 w-3 animate-spin" /> : <Send className="h-3 w-3" />}
          </Button>
        </div>
      </form>
    </div>
  )
}

// Компонент для отображения иерархии агентов
interface AgentOrchestrationCardProps {
  orchestration: {
    rootAgent: AgentInfo
    triggeredBy?: string
    timestamp: string
  }
  targetLanguageLabel: string
}

function AgentOrchestrationCard({ orchestration, targetLanguageLabel }: AgentOrchestrationCardProps) {
  const [expandedAgents, setExpandedAgents] = React.useState<Set<string>>(new Set([orchestration.rootAgent.id]))

  const toggleAgent = (id: string) => {
    const newSet = new Set(expandedAgents)
    if (newSet.has(id)) {
      newSet.delete(id)
    } else {
      newSet.add(id)
    }
    setExpandedAgents(newSet)
  }

  const getStatusIcon = (status: AgentInfo['status']) => {
    switch (status) {
      case 'working':
        return <div className="h-2 w-2 rounded-full bg-blue-500 animate-pulse" />
      case 'completed':
        return <CheckCircle2 className="h-3 w-3 text-green-500" />
      case 'failed':
        return <XCircle className="h-3 w-3 text-destructive" />
      default:
        return <div className="h-2 w-2 rounded-full bg-muted-foreground/30" />
    }
  }

  const renderAgent = (agent: AgentInfo, depth = 0) => {
    const isExpanded = expandedAgents.has(agent.id)
    const hasSubAgents = agent.subAgents && agent.subAgents.length > 0

    return (
      <div key={agent.id} className={depth > 0 ? 'ml-4 border-l pl-2' : ''}>
        <div
          className={cn(
            'flex items-start gap-2 py-1.5 rounded px-2 -mx-2',
            depth === 0 && 'bg-indigo-50/50 dark:bg-indigo-950/20 border border-indigo-200 dark:border-indigo-800 rounded-lg mb-1'
          )}
        >
          {hasSubAgents ? (
            <button
              onClick={() => toggleAgent(agent.id)}
              className="mt-0.5 text-muted-foreground hover:text-foreground"
            >
              {isExpanded ? (
                <ChevronDown className="h-3 w-3" />
              ) : (
                <ChevronRight className="h-3 w-3" />
              )}
            </button>
          ) : (
            <div className="w-3" />
          )}

          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-1.5">
              {getStatusIcon(agent.status)}
              <Cpu className="h-3 w-3 text-indigo-500" />
              <span className="font-medium text-xs">{agent.name}</span>
              <Badge variant="outline" className="text-[9px] h-4 px-1">
                {agent.role}
              </Badge>
            </div>

            {agent.purpose && (
              <p className="text-[10px] text-muted-foreground mt-0.5">
                <span className="italic">{targetLanguageLabel === 'Русский' ? 'Задача:' : 'Purpose:'}</span>{' '}
                {agent.purpose}
              </p>
            )}

            {agent.input && (
              <p className="text-[10px] text-muted-foreground/70 mt-0.5 truncate">
                <ArrowRight className="h-2.5 w-2.5 inline mr-0.5" />
                {agent.input}
              </p>
            )}
          </div>
        </div>

        {isExpanded && hasSubAgents && (
          <div className="mt-1">
            <div className="flex items-center gap-1 mb-1 text-[9px] text-muted-foreground uppercase tracking-wider">
              <Users className="h-2.5 w-2.5" />
              {targetLanguageLabel === 'Русский' ? 'Субагенты:' : 'Sub-agents:'}
            </div>
            {agent.subAgents!.map(sub => renderAgent(sub, depth + 1))}
          </div>
        )}
      </div>
    )
  }

  return (
    <div className="rounded-lg border border-indigo-200 dark:border-indigo-800 bg-indigo-50/30 dark:bg-indigo-950/10 overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between border-b border-indigo-200/50 dark:border-indigo-800/50 bg-indigo-100/50 dark:bg-indigo-900/20 px-3 py-2">
        <div className="flex items-center gap-2">
          <Workflow className="h-3.5 w-3.5 text-indigo-600 dark:text-indigo-400" />
          <span className="text-xs font-medium">
            {targetLanguageLabel === 'Русский' ? 'Оркестрация агентов' : 'Agent Orchestration'}
          </span>
        </div>
        {orchestration.triggeredBy && (
          <Badge variant="secondary" className="text-[9px]">
            {targetLanguageLabel === 'Русский' ? 'Вызвано:' : 'Triggered by:'} {orchestration.triggeredBy}
          </Badge>
        )}
      </div>

      {/* Agent tree */}
      <div className="p-3">
        {renderAgent(orchestration.rootAgent)}
      </div>
    </div>
  )
}
