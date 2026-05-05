'use client'

import * as React from 'react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { Input } from '@/components/ui/input'
import {
  Terminal,
  Plus,
  X,
  Play,
  Square,
  Trash2,
  Copy,
  ChevronDown,
  ChevronUp,
  Maximize2,
  Minimize2,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import {
  terminalApi,
  TerminalWebSocket,
  type TerminalSession,
  type CommandEntry,
  ansiToHtml,
} from '@/lib/terminal-api'

interface TerminalTab {
  id: string
  name: string
  sessionId?: string
  history: CommandEntry[]
  currentInput: string
  output: string
  isRunning: boolean
  cwd: string
}

interface TerminalPanelProps {
  workspaceId?: string
  className?: string
  onCommandOutput?: (tabId: string, output: string) => void
  externalCommands?: Array<{ tabId: string; command: string }>
}

export function TerminalPanel({
  workspaceId,
  className,
  onCommandOutput,
  externalCommands,
}: TerminalPanelProps) {
  const [tabs, setTabs] = React.useState<TerminalTab[]>([
    { id: '1', name: 'Terminal 1', history: [], currentInput: '', output: '', isRunning: false, cwd: '~' },
  ])
  const [activeTab, setActiveTab] = React.useState('1')
  const [isExpanded, setIsExpanded] = React.useState(false)
  const [isMinimized, setIsMinimized] = React.useState(false)
  const scrollRefs = React.useRef<Map<string, HTMLDivElement>>(new Map())
  const wsRef = React.useRef<Map<string, TerminalWebSocket>>(new Map())
  const nextIdRef = React.useRef(2)

  const activeTabData = tabs.find(t => t.id === activeTab)

  // Прокрутка вниз при новом выводе
  React.useEffect(() => {
    const scrollEl = scrollRefs.current.get(activeTab)
    if (scrollEl) {
      scrollEl.scrollTop = scrollEl.scrollHeight
    }
  }, [activeTab, tabs])

  // Обработка externalCommands (от агента)
  React.useEffect(() => {
    if (!externalCommands?.length) return

    externalCommands.forEach(({ tabId, command }) => {
      const tab = tabs.find(t => t.id === tabId)
      if (tab) {
        executeInTab(tabId, command)
      }
    })
  }, [externalCommands])

  const createNewTab = async () => {
    const newId = String(nextIdRef.current++)
    const newTab: TerminalTab = {
      id: newId,
      name: `Terminal ${newId}`,
      history: [],
      currentInput: '',
      output: `$ Welcome to Terminal ${newId}\n`,
      isRunning: false,
      cwd: '~',
    }

    // Если есть workspaceId, создаем сессию на сервере
    if (workspaceId) {
      try {
        const session = await terminalApi.createSession(workspaceId, {
          shell: 'Bash',
          workingDirectory: '/workspace',
        })
        newTab.sessionId = session.id
        newTab.cwd = session.workingDirectory

        // Подключаем WebSocket
        const ws = new TerminalWebSocket(window.location.origin)
        ws.connect(session.id)
        ws.subscribe(session.id, (event) => {
          setTabs(prev => prev.map(t => {
            if (t.sessionId === session.id) {
              return {
                ...t,
                output: t.output + event.data,
              }
            }
            return t
          }))
        })
        wsRef.current.set(session.id, ws)
      } catch (error) {
        console.error('Failed to create terminal session:', error)
      }
    }

    setTabs(prev => [...prev, newTab])
    setActiveTab(newId)
  }

  const closeTab = async (tabId: string, e: React.MouseEvent) => {
    e.stopPropagation()
    if (tabs.length === 1) return // Не закрываем последнюю вкладку

    const tab = tabs.find(t => t.id === tabId)
    if (tab?.sessionId) {
      // Отключаем WebSocket
      wsRef.current.get(tab.sessionId)?.disconnect()
      wsRef.current.delete(tab.sessionId)
      // Завершаем сессию на сервере
      try {
        await terminalApi.terminateSession(tab.sessionId)
      } catch {}
    }

    const newTabs = tabs.filter(t => t.id !== tabId)
    setTabs(newTabs)
    if (activeTab === tabId) {
      setActiveTab(newTabs[0].id)
    }
  }

  const executeInTab = async (tabId: string, command: string) => {
    const tab = tabs.find(t => t.id === tabId)
    if (!tab || tab.isRunning) return

    setTabs(prev => prev.map(t => 
      t.id === tabId 
        ? { ...t, isRunning: true, output: t.output + `$ ${command}\n` }
        : t
    ))

    try {
      if (tab.sessionId) {
        // Используем API сервера
        const response = await terminalApi.executeCommand({
          sessionId: tab.sessionId,
          command,
        })
        
        setTabs(prev => prev.map(t => 
          t.id === tabId 
            ? { 
                ...t, 
                isRunning: false,
                history: [...t.history, response.entry],
                output: t.output + (response.entry.output || '') + 
                  (response.entry.exitCode ? ` [Exit: ${response.entry.exitCode}]` : '') + '\n',
                cwd: response.session.workingDirectory,
              }
            : t
        ))

        onCommandOutput?.(tabId, response.entry.output || '')
      } else {
        // Локальное выполнение (fallback)
        await new Promise(resolve => setTimeout(resolve, 500))
        const mockOutput = `[Mock] Executing: ${command}\n`
        
        setTabs(prev => prev.map(t => 
          t.id === tabId 
            ? { 
                ...t, 
                isRunning: false,
                output: t.output + mockOutput,
              }
            : t
        ))

        onCommandOutput?.(tabId, mockOutput)
      }
    } catch (error) {
      setTabs(prev => prev.map(t => 
        t.id === tabId 
          ? { 
              ...t, 
              isRunning: false,
              output: t.output + `Error: ${error}\n`,
            }
          : t
      ))
    }
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!activeTabData || !activeTabData.currentInput.trim() || activeTabData.isRunning) return
    
    const command = activeTabData.currentInput
    setTabs(prev => prev.map(t => 
      t.id === activeTab ? { ...t, currentInput: '' } : t
    ))
    
    executeInTab(activeTab, command)
  }

  const clearOutput = (tabId: string) => {
    setTabs(prev => prev.map(t => 
      t.id === tabId ? { ...t, output: '' } : t
    ))
  }

  const copyOutput = (tabId: string) => {
    const tab = tabs.find(t => t.id === tabId)
    if (tab?.output) {
      navigator.clipboard.writeText(tab.output)
    }
  }

  if (isMinimized) {
    return (
      <div className={cn(
        "border-t bg-background flex items-center justify-between px-3 py-2 cursor-pointer hover:bg-muted/50",
        className
      )} onClick={() => setIsMinimized(false)}>
        <div className="flex items-center gap-2">
          <Terminal className="h-4 w-4" />
          <span className="text-xs font-medium">Terminal</span>
          {tabs.some(t => t.isRunning) && (
            <Badge variant="secondary" className="text-[10px]">Running</Badge>
          )}
        </div>
        <ChevronUp className="h-4 w-4 text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className={cn(
      "border-t bg-background flex flex-col",
      isExpanded ? "h-[50vh]" : "h-[250px]",
      className
    )}>
      {/* Header */}
      <div className="flex items-center justify-between border-b px-2 py-1">
        <div className="flex items-center gap-1">
          <Terminal className="h-3.5 w-3.5 text-muted-foreground" />
          <span className="text-xs font-medium">Terminal</span>
          <span className="text-[10px] text-muted-foreground ml-1">
            {activeTabData?.cwd}
          </span>
        </div>
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6"
            onClick={() => clearOutput(activeTab)}
            title="Clear"
          >
            <Trash2 className="h-3 w-3" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6"
            onClick={() => copyOutput(activeTab)}
            title="Copy"
          >
            <Copy className="h-3 w-3" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6"
            onClick={() => setIsExpanded(!isExpanded)}
            title={isExpanded ? "Minimize" : "Maximize"}
          >
            {isExpanded ? <Minimize2 className="h-3 w-3" /> : <Maximize2 className="h-3 w-3" />}
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6"
            onClick={() => setIsMinimized(true)}
            title="Hide"
          >
            <ChevronDown className="h-3 w-3" />
          </Button>
        </div>
      </div>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col">
        <div className="border-b bg-muted/30">
          <div className="flex items-center">
            <TabsList className="h-8 bg-transparent p-0 rounded-none">
              {tabs.map(tab => (
                <TabsTrigger
                  key={tab.id}
                  value={tab.id}
                  className={cn(
                    "h-8 rounded-none border-r px-3 text-xs gap-1.5 data-[state=active]:bg-background",
                    tab.isRunning && "text-amber-500"
                  )}
                >
                  {tab.isRunning && <div className="h-1.5 w-1.5 rounded-full bg-amber-500 animate-pulse" />}
                  {tab.name}
                  {tabs.length > 1 && (
                    <button
                      onClick={(e) => closeTab(tab.id, e)}
                      className="ml-1 hover:text-destructive"
                    >
                      <X className="h-3 w-3" />
                    </button>
                  )}
                </TabsTrigger>
              ))}
            </TabsList>
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7 ml-1"
              onClick={createNewTab}
            >
              <Plus className="h-3.5 w-3.5" />
            </Button>
          </div>
        </div>

        {tabs.map(tab => (
          <TabsContent
            key={tab.id}
            value={tab.id}
            className="flex-1 flex flex-col m-0 data-[state=inactive]:hidden"
          >
            {/* Output */}
            <ScrollArea className="flex-1">
              <div
                ref={(el) => {
                  if (el) scrollRefs.current.set(tab.id, el)
                }}
                className="p-3 font-mono text-xs whitespace-pre-wrap"
                dangerouslySetInnerHTML={{ __html: ansiToHtml(tab.output) }}
              />
            </ScrollArea>

            {/* Input */}
            <form onSubmit={handleSubmit} className="border-t p-2 flex items-center gap-2">
              <span className="text-xs text-muted-foreground font-mono select-none">
                {tab.cwd} $
              </span>
              <Input
                value={activeTab === tab.id ? activeTabData?.currentInput || '' : tab.currentInput}
                onChange={(e) => setTabs(prev => prev.map(t => 
                  t.id === tab.id ? { ...t, currentInput: e.target.value } : t
                ))}
                placeholder="Enter command..."
                className="flex-1 h-7 text-xs font-mono"
                disabled={tab.isRunning}
                spellCheck={false}
                autoComplete="off"
              />
              <Button
                type="submit"
                size="icon"
                className="h-7 w-7"
                disabled={!activeTabData?.currentInput.trim() || tab.isRunning}
              >
                {tab.isRunning ? (
                  <Square className="h-3 w-3" />
                ) : (
                  <Play className="h-3 w-3" />
                )}
              </Button>
            </form>
          </TabsContent>
        ))}
      </Tabs>
    </div>
  )
}

// Компонент для отображения терминала output в чате
export function TerminalOutputCard({
  command,
  output,
  exitCode,
  durationMs,
  className,
}: {
  command: string
  output: string
  exitCode?: number
  durationMs?: number
  className?: string
}) {
  const [isExpanded, setIsExpanded] = React.useState(false)

  return (
    <div className={cn(
      "rounded-lg border bg-muted/50 overflow-hidden",
      className
    )}>
      {/* Header */}
      <div className="flex items-center justify-between border-b bg-muted/80 px-3 py-2">
        <div className="flex items-center gap-2">
          <Terminal className="h-3.5 w-3.5 text-muted-foreground" />
          <code className="text-xs font-mono">{command}</code>
        </div>
        <div className="flex items-center gap-2">
          {exitCode !== undefined && (
            <Badge
              variant={exitCode === 0 ? "default" : "destructive"}
              className="text-[10px]"
            >
              {exitCode === 0 ? 'OK' : `Exit ${exitCode}`}
            </Badge>
          )}
          {durationMs !== undefined && (
            <span className="text-[10px] text-muted-foreground">
              {durationMs}ms
            </span>
          )}
          <Button
            variant="ghost"
            size="icon"
            className="h-5 w-5"
            onClick={() => setIsExpanded(!isExpanded)}
          >
            {isExpanded ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />}
          </Button>
        </div>
      </div>

      {/* Output */}
      {isExpanded && (
        <ScrollArea className="max-h-[200px]">
          <pre className="p-3 text-[11px] font-mono whitespace-pre-wrap">
            {output || 'No output'}
          </pre>
        </ScrollArea>
      )}
    </div>
  )
}
