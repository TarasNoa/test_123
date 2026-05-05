/**
 * Terminal API client for executing commands in shadow workspaces.
 * Supports multiple terminal sessions with tabs.
 */

import { api } from './api'

export type ShellType = 'Bash' | 'Zsh' | 'Fish' | 'PowerShell' | 'Cmd'
export type SessionStatus = 'Active' | 'Idle' | 'Terminated'

export interface TerminalSession {
  id: string
  userId: string
  shell: ShellType
  workingDirectory: string
  environmentVariables: Record<string, string>
  status: SessionStatus
  rows: number
  cols: number
  createdAt: string
  lastActivityAt: string
  terminatedAt?: string
}

export interface CommandEntry {
  id: string
  sessionId: string
  command: string
  output?: string
  exitCode?: number
  durationMs: number
  executedAt: string
}

export interface CreateSessionRequest {
  shell?: ShellType
  workingDirectory?: string
  environmentVariables?: Record<string, string>
  rows?: number
  cols?: number
}

export interface ExecuteCommandRequest {
  sessionId: string
  command: string
  workingDirectory?: string
}

export interface ExecuteCommandResponse {
  entry: CommandEntry
  session: TerminalSession
}

export interface TerminalOutputEvent {
  type: 'output' | 'error' | 'exit'
  sessionId: string
  data: string
  exitCode?: number
}

// HTTP API
export const terminalApi = {
  // Создать новую сессию терминала
  createSession: (workspaceId: string, req?: CreateSessionRequest) =>
    api<TerminalSession>(`/api/ide/terminal/sessions`, {
      method: 'POST',
      body: JSON.stringify({ workspaceId, ...req }),
    }),

  // Получить список сессий
  listSessions: (workspaceId?: string) =>
    api<TerminalSession[]>(`/api/ide/terminal/sessions${workspaceId ? `?workspaceId=${workspaceId}` : ''}`),

  // Получить сессию по ID
  getSession: (sessionId: string) =>
    api<TerminalSession>(`/api/ide/terminal/sessions/${sessionId}`),

  // Выполнить команду
  executeCommand: (req: ExecuteCommandRequest) =>
    api<ExecuteCommandResponse>(`/api/ide/terminal/execute`, {
      method: 'POST',
      body: JSON.stringify(req),
    }),

  // Получить историю команд сессии
  getHistory: (sessionId: string) =>
    api<CommandEntry[]>(`/api/ide/terminal/sessions/${sessionId}/history`),

  // Завершить сессию
  terminateSession: (sessionId: string) =>
    api<void>(`/api/ide/terminal/sessions/${sessionId}/terminate`, {
      method: 'POST',
    }),

  // Изменить размер терминала
  resize: (sessionId: string, rows: number, cols: number) =>
    api<void>(`/api/ide/terminal/sessions/${sessionId}/resize`, {
      method: 'POST',
      body: JSON.stringify({ rows, cols }),
    }),
}

// WebSocket для real-time output
export class TerminalWebSocket {
  private ws: WebSocket | null = null
  private reconnectAttempts = 0
  private maxReconnectAttempts = 5
  private reconnectDelay = 1000
  private listeners: Map<string, Set<(event: TerminalOutputEvent) => void>> = new Map()

  constructor(private baseUrl: string) {}

  connect(sessionId: string) {
    const wsUrl = this.baseUrl.replace(/^http/, 'ws') + `/ws/terminal/${sessionId}`
    this.ws = new WebSocket(wsUrl)

    this.ws.onopen = () => {
      this.reconnectAttempts = 0
      console.log('[TerminalWS] Connected:', sessionId)
    }

    this.ws.onmessage = (event) => {
      const data: TerminalOutputEvent = JSON.parse(event.data)
      this.notifyListeners(data.sessionId, data)
    }

    this.ws.onclose = () => {
      console.log('[TerminalWS] Closed:', sessionId)
      this.attemptReconnect(sessionId)
    }

    this.ws.onerror = (error) => {
      console.error('[TerminalWS] Error:', error)
    }
  }

  private attemptReconnect(sessionId: string) {
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++
      setTimeout(() => {
        console.log(`[TerminalWS] Reconnecting... (${this.reconnectAttempts})`)
        this.connect(sessionId)
      }, this.reconnectDelay * this.reconnectAttempts)
    }
  }

  subscribe(sessionId: string, callback: (event: TerminalOutputEvent) => void) {
    if (!this.listeners.has(sessionId)) {
      this.listeners.set(sessionId, new Set())
    }
    this.listeners.get(sessionId)!.add(callback)

    return () => {
      this.listeners.get(sessionId)?.delete(callback)
    }
  }

  private notifyListeners(sessionId: string, event: TerminalOutputEvent) {
    this.listeners.get(sessionId)?.forEach(cb => cb(event))
  }

  sendInput(sessionId: string, input: string) {
    if (this.ws?.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify({ type: 'input', sessionId, data: input }))
    }
  }

  disconnect() {
    this.ws?.close()
    this.ws = null
  }
}

// Utility для цветового форматирования ANSI в HTML (упрощенно)
export function ansiToHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    // ANSI colors (упрощенная версия)
    .replace(/\x1b\[31m/g, '<span class="text-red-500">')
    .replace(/\x1b\[32m/g, '<span class="text-green-500">')
    .replace(/\x1b\[33m/g, '<span class="text-yellow-500">')
    .replace(/\x1b\[34m/g, '<span class="text-blue-500">')
    .replace(/\x1b\[0m/g, '</span>')
    .replace(/\n/g, '<br/>')
}

// Форматирование вывода команды с временем выполнения
export function formatCommandOutput(entry: CommandEntry): string {
  const lines: string[] = []
  lines.push(`$ ${entry.command}`)
  if (entry.output) {
    lines.push(entry.output)
  }
  if (entry.exitCode !== undefined && entry.exitCode !== 0) {
    lines.push(`[Exit code: ${entry.exitCode}]`)
  }
  lines.push(`[${entry.durationMs}ms]`)
  return lines.join('\n')
}
