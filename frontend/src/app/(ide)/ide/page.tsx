'use client'

import * as React from 'react'
import Link from 'next/link'
import { useRouter, useSearchParams } from 'next/navigation'
import { Code2, ArrowLeft, Loader2, Sparkles, Clock, ExternalLink } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { startRun, listRuns, type RunSummary } from '@/lib/app-generation-api'

export default function IdeLandingPage() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [prompt, setPrompt] = React.useState('')
  const [loading, setLoading] = React.useState(false)
  const [recentRuns, setRecentRuns] = React.useState<RunSummary[]>([])
  const [loadingRuns, setLoadingRuns] = React.useState(true)
  const taskId = searchParams.get('taskId')
  const chatId = searchParams.get('chatId')

  React.useEffect(() => {
    listRuns()
      .then((runs) => setRecentRuns(runs.slice(0, 10)))
      .catch(() => {})
      .finally(() => setLoadingRuns(false))
  }, [])

  React.useEffect(() => {
    const prefill = searchParams.get('prefill')
    if (prefill) setPrompt(prefill)
  }, [searchParams])

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    if (!prompt.trim() || loading) return
    setLoading(true)
    try {
      await startRun({ userRequest: prompt.trim(), maxIterations: 20 })
      const runs = await listRuns()
      if (runs.length > 0) {
        const latest = runs[0]
        router.push(`/ide/${latest.id}`)
      }
    } catch (err) {
      setLoading(false)
    }
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      handleCreate(e)
    }
  }

  const statusColors: Record<string, string> = {
    Completed: 'bg-primary/10 text-primary',
    Failed: 'bg-destructive/10 text-destructive',
    Cancelled: 'bg-muted text-muted-foreground',
    Generating: 'bg-secondary/10 text-secondary',
    Planning: 'bg-secondary/10 text-secondary',
    Testing: 'bg-secondary/10 text-secondary',
    Created: 'bg-muted text-muted-foreground',
  }

  return (
    <div className="flex h-screen flex-col bg-background">
      <div className="flex h-10 items-center gap-2 border-b bg-muted/30 px-4">
        <Button variant="ghost" size="sm" className="h-7 gap-1.5" asChild>
          <Link href="/dashboard">
            <ArrowLeft className="h-3.5 w-3.5" />
            <span className="text-xs">Назад</span>
          </Link>
        </Button>
      </div>

      <div className="flex flex-1 overflow-hidden">
        {/* Left — New project */}
        <div className="flex flex-1 flex-col items-center justify-center p-8">
          <div className="w-full max-w-lg space-y-6">
            <div className="flex flex-col items-center gap-3 text-center">
              <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-gradient-to-br from-secondary to-primary text-white">
                <Code2 className="h-7 w-7" />
              </div>
              <h1 className="text-2xl font-bold">Libr4 IDE</h1>
              <p className="text-sm text-muted-foreground max-w-sm">
                Опишите приложение — AI-агенты сгенерируют код.
                Потом работайте как в IDE: просите доработки, правьте файлы.
              </p>
            </div>

            {(taskId || chatId) && (
              <Card className="border-secondary/20 bg-secondary/10">
                <CardContent className="p-4 text-sm text-muted-foreground">
                  {taskId && <p>Контекст заказа подключён: prompt уже собран на основе карточки заказа.</p>}
                  {chatId && <p className={taskId ? 'mt-1' : ''}>Контекст чата тоже подключён: в описание подмешаны договорённости из переписки.</p>}
                </CardContent>
              </Card>
            )}

            <form onSubmit={handleCreate} className="space-y-3">
              <Textarea
                placeholder="Опишите приложение, которое нужно создать...&#10;&#10;Например: REST API на ASP.NET Core для управления задачами с авторизацией JWT и PostgreSQL"
                rows={5}
                value={prompt}
                onChange={(e) => setPrompt(e.target.value)}
                onKeyDown={handleKeyDown}
                disabled={loading}
                className="resize-none text-sm"
              />
              <Button
                type="submit"
                className="w-full bg-gradient-to-r from-secondary to-primary hover:opacity-90"
                disabled={!prompt.trim() || loading}
              >
                {loading ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Создание...
                  </>
                ) : (
                  <>
                    <Sparkles className="mr-2 h-4 w-4" />
                    Создать проект
                  </>
                )}
              </Button>
              <p className="text-[11px] text-center text-muted-foreground">
                Ctrl+Enter для быстрого запуска
              </p>
            </form>
          </div>
        </div>

        {/* Right — Recent projects */}
        <div className="w-80 shrink-0 border-l overflow-y-auto p-4 space-y-3">
          <h2 className="text-sm font-semibold flex items-center gap-2">
            <Clock className="h-4 w-4 text-muted-foreground" />
            Последние проекты
          </h2>

          {loadingRuns && (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
            </div>
          )}

          {!loadingRuns && recentRuns.length === 0 && (
            <p className="text-xs text-muted-foreground py-4 text-center">
              Пока нет проектов. Создайте первый!
            </p>
          )}

          {recentRuns.map((run) => (
            <Link key={run.id} href={`/ide/${run.id}`} className="block">
              <Card className="hover:border-primary/50 transition-colors cursor-pointer">
                <CardContent className="p-3 space-y-1.5">
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-medium truncate flex-1">
                      {run.applicationName ?? 'Без названия'}
                    </span>
                    <ExternalLink className="h-3 w-3 text-muted-foreground shrink-0 ml-2" />
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge
                      variant="outline"
                      className={`text-[10px] ${statusColors[run.status] ?? ''}`}
                    >
                      {run.status}
                    </Badge>
                    {run.startedAt && (
                      <span className="text-[10px] text-muted-foreground">
                        {new Date(run.startedAt).toLocaleDateString('ru-RU', {
                          day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit',
                        })}
                      </span>
                    )}
                  </div>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      </div>
    </div>
  )
}
