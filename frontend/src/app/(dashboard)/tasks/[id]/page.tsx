'use client'

import { useState, useEffect } from 'react'
import { useParams, useRouter } from 'next/navigation'
import Link from 'next/link'
import { useAuth } from '@/lib/auth'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Textarea } from '@/components/ui/textarea'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'
import { api } from '@/lib/api'
import { ArrowLeft, Calendar, DollarSign, Users, Clock, Loader2, Sparkles, MessageSquare, CheckCircle2 } from 'lucide-react'
import { chatApi } from '@/lib/chat-api'
import {
  applicationStatusMeta,
  getCategoryLabel,
  taskStatusMeta,
  type MarketplaceTask as Task,
  type TaskApplication as Application,
} from '@/lib/marketplace'

export default function TaskDetailPage() {
  const params = useParams()
  const router = useRouter()
  const { user } = useAuth()
  const [task, setTask] = useState<Task | null>(null)
  const [applications, setApplications] = useState<Application[]>([])
  const [loading, setLoading] = useState(true)
  const [intro, setIntro] = useState('')
  const [workPlan, setWorkPlan] = useState('')
  const [portfolioLink, setPortfolioLink] = useState('')
  const [deliveryDays, setDeliveryDays] = useState('')
  const [proposedBudget, setProposedBudget] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [acceptingApplicationId, setAcceptingApplicationId] = useState<string | null>(null)

  const id = params.id as string
  const isOwner = user && task?.clientId === user.id

  useEffect(() => {
    loadTask()
  }, [id])

  async function loadTask() {
    try {
      const t = await api<Task>(`/tasks/${id}`, { auth: false })
      setTask(t)
      if (user) {
        try {
          const apps = await api<Application[]>(`/tasks/${id}/applications`)
          setApplications(apps)
        } catch {}
      }
    } catch {
      router.push('/tasks')
    } finally {
      setLoading(false)
    }
  }

  async function handleApply(e: React.FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setSuccessMessage(null)
    try {
      const proposal = [
        `Почему я подхожу:\n${intro.trim()}`,
        `План реализации:\n${workPlan.trim()}`,
        deliveryDays ? `Оценка срока: ${deliveryDays} дн.` : null,
        portfolioLink ? `Портфолио / кейс: ${portfolioLink.trim()}` : null,
      ]
        .filter(Boolean)
        .join('\n\n')

      await api(`/tasks/${id}/apply`, {
        method: 'POST',
        body: JSON.stringify({ proposal, proposedBudget: parseFloat(proposedBudget) }),
      })
      await loadTask()
      setIntro('')
      setWorkPlan('')
      setPortfolioLink('')
      setDeliveryDays('')
      setProposedBudget('')
      setSuccessMessage('Заявка отправлена. Когда заказчик одобрит вас, вы автоматически перейдёте в рабочий чат.')
    } catch {}
    setSubmitting(false)
  }

  async function handlePublish() {
    await api(`/tasks/${id}/publish`, { method: 'POST' })
    await loadTask()
  }

  async function handleAccept(appId: string, freelancerId: string) {
    setAcceptingApplicationId(appId)
    try {
      await api(`/tasks/${id}/applications/${appId}/accept`, { method: 'POST' })
      const chatId = await chatApi.createDirectChat(freelancerId).catch(() => null)
      if (chatId) {
        router.push(`/chats?chat=${chatId}&taskId=${id}&approvedApplication=${appId}`)
        return
      }
      await loadTask()
    } finally {
      setAcceptingApplicationId(null)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="h-6 w-6 animate-spin text-primary" />
      </div>
    )
  }

  if (!task) return null

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <Button variant="ghost" size="sm" asChild>
        <Link href="/tasks">
          <ArrowLeft className="mr-2 h-4 w-4" /> К заказам
        </Link>
      </Button>

      <Card className="border-secondary/20 bg-gradient-to-r from-secondary/15 via-background to-primary/10">
        <CardContent className="flex flex-col gap-3 p-5 md:flex-row md:items-center md:justify-between">
          <div className="space-y-1">
            <div className="inline-flex items-center gap-2 rounded-full bg-background/90 px-3 py-1 text-xs font-medium text-secondary-foreground">
              <Sparkles className="h-3.5 w-3.5" />
              Flow: Apply -&gt; Approval -&gt; Chat -&gt; IDE
            </div>
            <p className="text-sm text-muted-foreground">
              После одобрения отклика обе стороны переходят в рабочий чат, а затем из чата можно открыть IDE с контекстом заказа.
            </p>
          </div>
          <Button variant="outline" asChild>
            <Link href="/chats">
              <MessageSquare className="mr-2 h-4 w-4" />
              Открыть чаты
            </Link>
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-start justify-between gap-4">
            <div className="space-y-1">
              <CardTitle className="text-xl">{task.title}</CardTitle>
              <CardDescription className="flex flex-wrap items-center gap-3">
                <Badge variant="secondary">{getCategoryLabel(task.category)}</Badge>
                <Badge variant={taskStatusMeta[task.status]?.tone ?? 'outline'}>
                  {taskStatusMeta[task.status]?.label ?? task.status}
                </Badge>
              </CardDescription>
            </div>
            <div className="text-right shrink-0">
              <div className="text-2xl font-bold text-primary">${task.budget}</div>
              <p className="text-xs text-muted-foreground">{task.currency}</p>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="whitespace-pre-wrap text-sm leading-relaxed">{task.description}</p>

          <Separator />

          <div className="flex flex-wrap gap-4 text-sm text-muted-foreground">
            <span className="flex items-center gap-1"><Users className="h-4 w-4" /> {task.applicationCount} откликов</span>
            <span className="flex items-center gap-1"><Clock className="h-4 w-4" /> {new Date(task.createdAt).toLocaleDateString()}</span>
            {task.deadline && (
              <span className="flex items-center gap-1"><Calendar className="h-4 w-4" /> до {new Date(task.deadline).toLocaleDateString()}</span>
            )}
          </div>

          {isOwner && task.status === 'Draft' && (
            <Button onClick={handlePublish}>Опубликовать задание</Button>
          )}
        </CardContent>
      </Card>

      {/* Apply form */}
      {user && !isOwner && task.status === 'Open' && (
        <Card>
          <CardHeader>
            <CardTitle>Быстрый Apply</CardTitle>
            <CardDescription>
              Коротко объясните, почему вы подходите, и отправьте структурированную заявку.
            </CardDescription>
          </CardHeader>
          <form onSubmit={handleApply}>
            <CardContent className="space-y-4">
              {successMessage && (
                <div className="rounded-xl border border-primary/20 bg-primary/5 p-3 text-sm text-primary">
                  {successMessage}
                </div>
              )}
              <div className="space-y-2">
                <Label>Почему именно вы</Label>
                <Textarea
                  required
                  minLength={20}
                  rows={3}
                  placeholder="Коротко опишите релевантный опыт и сильные стороны."
                  value={intro}
                  onChange={(e) => setIntro(e.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label>Как вы реализуете проект</Label>
                <Textarea
                  required
                  minLength={30}
                  rows={4}
                  placeholder="Этапы, стек, что сделаете в первую очередь."
                  value={workPlan}
                  onChange={(e) => setWorkPlan(e.target.value)}
                />
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label>Срок, дней</Label>
                  <Input
                    type="number"
                    min={1}
                    placeholder="7"
                    value={deliveryDays}
                    onChange={(e) => setDeliveryDays(e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label>Предложенный бюджет ({task.currency})</Label>
                  <Input
                    type="number"
                    required
                    min={1}
                    value={proposedBudget}
                    onChange={(e) => setProposedBudget(e.target.value)}
                  />
                </div>
              </div>
              <div className="space-y-2">
                <Label>Ссылка на кейс / GitHub / портфолио</Label>
                <Input
                  placeholder="https://..."
                  value={portfolioLink}
                  onChange={(e) => setPortfolioLink(e.target.value)}
                />
              </div>
              <Button type="submit" disabled={submitting} className="w-full md:w-auto">
                {submitting ? 'Отправка...' : 'Отправить отклик'}
              </Button>
            </CardContent>
          </form>
        </Card>
      )}

      {!user && (
        <Card>
          <CardContent className="flex flex-col items-start gap-3 p-6 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <p className="font-medium">Чтобы откликнуться на заказ, нужно войти в аккаунт.</p>
              <p className="text-sm text-muted-foreground">После логина вы вернётесь к каталогу и сможете сразу отправить Apply.</p>
            </div>
            <Button asChild>
              <Link href="/login">Войти</Link>
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Applications (owner) */}
      {isOwner && applications.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Отклики ({applications.length})</CardTitle>
            <CardDescription>Одобрите фрилансера и откройте рабочий чат автоматически.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {applications.map((app) => (
              <div key={app.id} className="flex items-start justify-between gap-4 rounded-xl border p-4">
                <div className="space-y-1 flex-1 min-w-0">
                  <p className="text-sm">{app.proposal}</p>
                  <div className="flex items-center gap-3 text-xs text-muted-foreground">
                    <span className="flex items-center gap-1">
                      <DollarSign className="h-3 w-3" /> {app.proposedBudget}
                    </span>
                    <Badge variant={applicationStatusMeta[app.status]?.tone ?? 'outline'} className="text-xs">
                      {applicationStatusMeta[app.status]?.label ?? app.status}
                    </Badge>
                  </div>
                </div>
                {app.status === 'Pending' && (
                  <Button size="sm" onClick={() => handleAccept(app.id, app.freelancerId)} disabled={acceptingApplicationId === app.id}>
                    {acceptingApplicationId === app.id ? (
                      <>
                        <Loader2 className="mr-2 h-3.5 w-3.5 animate-spin" />
                        Перевожу в чат...
                      </>
                    ) : (
                      <>
                        <CheckCircle2 className="mr-2 h-3.5 w-3.5" />
                        Одобрить и открыть чат
                      </>
                    )}
                  </Button>
                )}
              </div>
            ))}
          </CardContent>
        </Card>
      )}
    </div>
  )
}
