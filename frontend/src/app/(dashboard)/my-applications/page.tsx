'use client'

import { useState, useEffect } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { useAuth } from '@/lib/auth'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { api } from '@/lib/api'
import { Briefcase, DollarSign, Calendar, Loader2, MessageSquare, Code2, Sparkles } from 'lucide-react'
import { chatApi } from '@/lib/chat-api'
import { buildIdePrefillQuery, buildIdePromptFromContext, getApplicationRedirectKey } from '@/lib/ide-handoff'
import {
  applicationStatusMeta,
  getCategoryLabel,
  type MarketplaceTask,
  type TaskApplication as Application,
} from '@/lib/marketplace'

export default function MyApplicationsPage() {
  const { user } = useAuth()
  const router = useRouter()
  const [applications, setApplications] = useState<Application[]>([])
  const [tasksById, setTasksById] = useState<Record<string, MarketplaceTask>>({})
  const [loading, setLoading] = useState(true)
  const [busyApplicationId, setBusyApplicationId] = useState<string | null>(null)

  useEffect(() => {
    if (user) loadApplications()
  }, [user])

  useEffect(() => {
    if (applications.length === 0 || typeof window === 'undefined') return

    const accepted = applications.find((application) => {
      if (application.status !== 'Accepted') return false
      return !window.sessionStorage.getItem(getApplicationRedirectKey(application.id))
    })

    if (!accepted) return

    openChatForApplication(accepted, true).catch(() => {})
  }, [applications, tasksById])

  async function loadApplications() {
    try {
      const data = await api<Application[]>('/tasks/my/applications')
      setApplications(data)
      const taskPairs = await Promise.all(
        data.map(async (application) => {
          try {
            const task = await api<MarketplaceTask>(`/tasks/${application.taskId}`, { auth: false })
            return [application.taskId, task] as const
          } catch {
            return null
          }
        })
      )
      setTasksById(
        taskPairs.reduce<Record<string, MarketplaceTask>>((acc, pair) => {
          if (pair) acc[pair[0]] = pair[1]
          return acc
        }, {})
      )
    } catch {}
    setLoading(false)
  }

  async function handleWithdraw(applicationId: string) {
    await api(`/tasks/my/applications/${applicationId}/withdraw`, { method: 'POST' })
    await loadApplications()
  }

  async function openChatForApplication(application: Application, autoRedirect = false) {
    const task = tasksById[application.taskId]
    if (!task) return

    setBusyApplicationId(application.id)
    try {
      const chatId = await chatApi.createDirectChat(task.clientId)
      if (typeof window !== 'undefined') {
        window.sessionStorage.setItem(getApplicationRedirectKey(application.id), '1')
      }
      router.push(`/chats?chat=${chatId}&taskId=${task.id}${autoRedirect ? '&auto=1' : ''}`)
    } finally {
      setBusyApplicationId(null)
    }
  }

  function openIdeWithPrefill(application: Application) {
    const task = tasksById[application.taskId]
    const prompt = buildIdePromptFromContext({ task, application })
    router.push(buildIdePrefillQuery(prompt, { taskId: application.taskId }))
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Мои заявки</h1>
          <p className="text-muted-foreground">Следите за откликами, переходите в чат после approval и запускайте IDE из рабочего контекста.</p>
        </div>
        <Button variant="outline" asChild>
          <Link href="/tasks">К каталогу заказов</Link>
        </Button>
      </div>

      <Card className="border-secondary/20 bg-gradient-to-r from-secondary/15 via-background to-primary/10">
        <CardContent className="flex flex-col gap-2 p-5 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="font-medium">Когда заказчик одобрит заявку, вы сразу попадёте в чат.</p>
            <p className="text-sm text-muted-foreground">Если вы вернулись позже, отсюда можно вручную открыть чат или сразу передать контекст в IDE.</p>
          </div>
          <Badge variant="secondary" className="w-fit">
            <Sparkles className="mr-1 h-3.5 w-3.5" />
            Auto chat handoff
          </Badge>
        </CardContent>
      </Card>

      {loading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-6 w-6 animate-spin text-primary" />
        </div>
      ) : applications.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-4 py-12">
            <Briefcase className="h-10 w-10 text-muted-foreground" />
            <p className="text-muted-foreground">Вы ещё не откликались на задания</p>
            <Button asChild>
              <Link href="/tasks">Найти задания</Link>
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {applications.map((app) => {
            const st = applicationStatusMeta[app.status] ?? { label: app.status, tone: 'outline' as const }
            const task = tasksById[app.taskId]
            return (
              <Card key={app.id} className="transition-all hover:shadow-sm">
                <CardHeader className="pb-3">
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <CardTitle className="text-base">
                        {task?.title ?? `Заказ #${app.taskId.slice(0, 8)}`}
                      </CardTitle>
                      {task && (
                        <p className="mt-1 text-xs text-muted-foreground">
                          {getCategoryLabel(task.category)} · бюджет {task.budget} {task.currency}
                        </p>
                      )}
                    </div>
                    <Badge variant={st.tone}>{st.label}</Badge>
                  </div>
                </CardHeader>
                <CardContent className="space-y-3">
                  <p className="text-sm text-muted-foreground line-clamp-2">{app.proposal}</p>
                  <div className="flex flex-wrap items-center gap-4 text-xs text-muted-foreground">
                    <span className="flex items-center gap-1">
                      <DollarSign className="h-3 w-3" /> {app.proposedBudget}
                    </span>
                    <span className="flex items-center gap-1">
                      <Calendar className="h-3 w-3" /> {new Date(app.submittedAt).toLocaleDateString()}
                    </span>
                  </div>
                  <div className="flex flex-wrap gap-2 pt-1">
                    <Button size="sm" variant="outline" asChild>
                      <Link href={`/tasks/${app.taskId}`}>Перейти к заданию</Link>
                    </Button>
                    {app.status === 'Accepted' && (
                      <>
                        <Button
                          size="sm"
                          onClick={() => openChatForApplication(app)}
                          disabled={busyApplicationId === app.id || !task}
                        >
                          <MessageSquare className="mr-2 h-4 w-4" />
                          {busyApplicationId === app.id ? 'Открываю чат...' : 'Открыть чат'}
                        </Button>
                        <Button size="sm" variant="secondary" onClick={() => openIdeWithPrefill(app)}>
                          <Code2 className="mr-2 h-4 w-4" />
                          Перейти в IDE
                        </Button>
                      </>
                    )}
                    {app.status === 'Pending' && (
                      <Button size="sm" variant="destructive" onClick={() => handleWithdraw(app.id)}>
                        Отозвать
                      </Button>
                    )}
                  </div>
                </CardContent>
              </Card>
            )
          })}
        </div>
      )}
    </div>
  )
}
