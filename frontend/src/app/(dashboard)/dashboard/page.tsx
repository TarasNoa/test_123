'use client'

import { useEffect, useMemo, useState } from 'react'
import Link from 'next/link'
import { useAuth } from '@/lib/auth'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Progress } from '@/components/ui/progress'
import {
  Briefcase,
  Code2,
  MessageSquare,
  Wallet,
  TrendingUp,
  Plus,
  ArrowRight,
  Bot,
  CheckCircle,
  Clock,
  Loader2,
  Sparkles,
  Star,
  UserCircle2,
} from 'lucide-react'
import { paymentsApi, type Transaction, type Wallet as WalletData } from '@/lib/payments-api'
import { api } from '@/lib/api'
import { chatApi } from '@/lib/chat-api'
import { listRuns, type RunSummary } from '@/lib/app-generation-api'
import {
  buildRecommendedTasks,
  buildSkillMetrics,
  getCategoryLabel,
  getTaskSummary,
  taskStatusMeta,
  type MarketplaceTask,
  type TaskApplication,
} from '@/lib/marketplace'

export default function DashboardPage() {
  const { user } = useAuth()
  const [wallet, setWallet] = useState<WalletData | null>(null)
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [tasks, setTasks] = useState<MarketplaceTask[]>([])
  const [applications, setApplications] = useState<TaskApplication[]>([])
  const [runs, setRuns] = useState<RunSummary[]>([])
  const [unreadNotifications, setUnreadNotifications] = useState(0)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false

    async function loadDashboard() {
      try {
        const [walletData, transactionsData, taskList, applicationList, notifications, runList] = await Promise.all([
          paymentsApi.getWallet().catch(() => null),
          paymentsApi.getTransactions({ page: 1, pageSize: 20 }).catch(() => null),
          api<MarketplaceTask[]>('/tasks', { auth: false }).catch(() => []),
          api<TaskApplication[]>('/tasks/my/applications').catch(() => []),
          chatApi.getNotifications(true, 1, 20).catch(() => null),
          listRuns().catch(() => []),
        ])

        if (cancelled) return
        setWallet(walletData)
        setTransactions(transactionsData?.transactions ?? [])
        setTasks(taskList)
        setApplications(applicationList)
        setUnreadNotifications(notifications?.items.length ?? 0)
        setRuns(runList)
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    loadDashboard()
    return () => {
      cancelled = true
    }
  }, [])

  const skillMetrics = useMemo(() => buildSkillMetrics(user, applications, runs), [user, applications, runs])
  const recommendedTasks = useMemo(() => buildRecommendedTasks(tasks, user), [tasks, user])
  const latestProjects = useMemo(() => runs.slice(0, 3), [runs])
  const acceptedApplications = applications.filter((app) => app.status === 'Accepted')
  const pendingApplications = applications.filter((app) => app.status === 'Pending')
  const income = transactions
    .filter((transaction) => transaction.status === 'Completed' && transaction.amount > 0)
    .reduce((sum, transaction) => sum + transaction.amount, 0)
  const activeProjects = acceptedApplications.length + runs.filter((run) => run.status === 'Generating' || run.status === 'Testing' || run.status === 'Fixing').length
  const profileProgress = Math.min(
    100,
    35 +
      (user?.emailConfirmed ? 20 : 0) +
      Math.min(skillMetrics.length * 8, 20) +
      Math.min(latestProjects.length * 8, 16) +
      Math.min(applications.length * 3, 9)
  )

  const quickActions = [
    { href: '/tasks', icon: Briefcase, label: 'Найти заказ', color: 'bg-primary text-primary-foreground' },
    { href: '/my-applications', icon: Sparkles, label: 'Мои заявки', color: 'bg-secondary text-secondary-foreground' },
    { href: '/chats', icon: MessageSquare, label: 'Открыть чат', color: 'bg-primary/10 text-primary' },
    { href: '/ide', icon: Code2, label: 'Перейти в IDE', color: 'bg-muted text-foreground' },
  ]

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div className="space-y-1">
          <h1 className="text-2xl font-bold tracking-tight">
            Привет, {user?.displayName?.split(' ')[0]}
          </h1>
          <p className="text-muted-foreground">
            Дашборд фрилансера: уровень навыков, доход, проекты и AI-рекомендации по заказам.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" asChild>
            <Link href="/profile">
              <UserCircle2 className="mr-2 h-4 w-4" />
              Профиль
            </Link>
          </Button>
          <Button asChild className="bg-gradient-to-r from-secondary to-primary text-secondary-foreground hover:opacity-90">
            <Link href="/tasks">
              <Sparkles className="mr-2 h-4 w-4" />
              Посмотреть рекомендованные заказы
            </Link>
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        {quickActions.map((action) => (
          <Link
            key={action.href}
            href={action.href}
            className="group flex flex-col items-center gap-2 rounded-xl border p-4 transition-all hover:shadow-md hover:border-primary/20"
          >
            <div className={`flex h-10 w-10 items-center justify-center rounded-lg ${action.color} transition-transform group-hover:scale-110`}>
              <action.icon className="h-5 w-5" />
            </div>
            <span className="text-sm font-medium">{action.label}</span>
          </Link>
        ))}
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Баланс</CardTitle>
            <Wallet className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-primary">
              ${wallet?.balance?.toFixed(2) ?? '—'}
            </div>
            <p className="text-xs text-muted-foreground">
              {wallet?.currency ?? 'USD'} · доступно сейчас
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Активные проекты</CardTitle>
            <Briefcase className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{activeProjects}</div>
            <p className="text-xs text-muted-foreground">
              {acceptedApplications.length} одобрено, {pendingApplications.length} ждут ответа
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Уведомления</CardTitle>
            <MessageSquare className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{unreadNotifications}</div>
            <p className="text-xs text-muted-foreground">Непрочитанных сообщений и событий</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Доход</CardTitle>
            <Bot className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">${income.toFixed(2)}</div>
            <p className="text-xs text-muted-foreground">{transactions.length} операций в истории</p>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle>Уровни по направлениям</CardTitle>
                <CardDescription>Оценка профиля на основе активности, ролей и AI-проектов</CardDescription>
              </div>
              <Button variant="ghost" size="sm" asChild>
                <Link href="/profile">
                  В профиль <ArrowRight className="ml-1 h-3 w-3" />
                </Link>
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            {loading ? (
              <div className="flex items-center justify-center py-10">
                <Loader2 className="h-5 w-5 animate-spin text-primary" />
              </div>
            ) : (
              <div className="grid gap-4 md:grid-cols-2">
                {skillMetrics.map((metric) => (
                  <div key={metric.key} className="rounded-xl border bg-muted/20 p-4">
                    <div className="mb-3 flex items-center justify-between">
                      <div>
                        <p className="font-medium">{metric.label}</p>
                        <p className="text-xs text-muted-foreground">{metric.hint}</p>
                      </div>
                      <Badge variant="secondary">{metric.value}/10</Badge>
                    </div>
                    <Progress value={metric.value * 10} className="h-2.5" />
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Профиль и доход</CardTitle>
            <CardDescription>Краткая сводка по аккаунту и последним результатам</CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            <div className="flex items-center gap-3">
              <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-secondary text-secondary-foreground font-bold text-lg">
                {user?.displayName?.[0]?.toUpperCase() ?? '?'}
              </div>
              <div className="min-w-0">
                <p className="font-semibold">{user?.displayName}</p>
                <p className="truncate text-sm text-muted-foreground">{user?.email}</p>
              </div>
            </div>

            <div className="grid gap-3">
              <div className="rounded-lg border p-3">
                <p className="text-xs text-muted-foreground">Профиль заполнен</p>
                <div className="mt-2 flex items-center justify-between text-sm">
                  <span>Готовность к новым заказам</span>
                  <span className="font-medium">{profileProgress}%</span>
                </div>
                <Progress value={profileProgress} className="mt-2 h-2" />
              </div>

              <div className="rounded-lg border p-3">
                <p className="text-xs text-muted-foreground">Доход</p>
                <p className="mt-1 text-2xl font-semibold">${income.toFixed(2)}</p>
                <p className="text-xs text-muted-foreground">
                  {acceptedApplications.length} одобренных проектов и {runs.length} AI-run'ов
                </p>
              </div>
            </div>

            <div className="space-y-2">
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Роли</span>
                <div className="flex gap-1">
                  {user?.roles.map((r) => (
                    <Badge key={r} variant="secondary" className="text-xs">{r}</Badge>
                  ))}
                </div>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Email</span>
                <Badge variant={user?.emailConfirmed ? 'default' : 'destructive'}>
                  {user?.emailConfirmed ? 'Подтверждён' : 'Не подтверждён'}
                </Badge>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">На платформе</span>
                <span>{user?.createdAt ? new Date(user.createdAt).toLocaleDateString('ru-RU') : '—'}</span>
              </div>
            </div>

            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm">
                <CheckCircle className="h-4 w-4 text-primary" />
                <span>Последние проекты</span>
              </div>
              {latestProjects.length === 0 ? (
                <p className="text-sm text-muted-foreground">Пока нет AI-проектов. Начните с IDE.</p>
              ) : (
                latestProjects.map((project) => (
                  <div key={project.id} className="rounded-lg border p-3">
                    <div className="flex items-center justify-between gap-2">
                      <p className="truncate text-sm font-medium">{project.applicationName ?? 'AI проект'}</p>
                      <Badge variant="outline">{project.status}</Badge>
                    </div>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {project.startedAt ? new Date(project.startedAt).toLocaleString('ru-RU') : 'Без даты'}
                    </p>
                  </div>
                ))
              )}
            </div>

            <Button variant="outline" className="w-full" asChild>
              <Link href="/profile">Открыть полный профиль</Link>
            </Button>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.5fr,1fr]">
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle>3 рекомендованных заказа</CardTitle>
                <CardDescription>Подборка с учётом навыков, роли и текущей активности</CardDescription>
              </div>
              <Button variant="ghost" size="sm" asChild>
                <Link href="/tasks">
                  Все заказы <ArrowRight className="ml-1 h-3 w-3" />
                </Link>
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            {loading ? (
              <div className="flex items-center justify-center py-10">
                <Loader2 className="h-5 w-5 animate-spin text-primary" />
              </div>
            ) : recommendedTasks.length === 0 ? (
              <div className="rounded-xl border border-dashed p-8 text-center text-sm text-muted-foreground">
                Пока нет открытых заказов для рекомендаций. Перейдите в каталог и создайте свой флоу вручную.
              </div>
            ) : (
              <div className="space-y-4">
                {recommendedTasks.map((task, index) => {
                  const status = taskStatusMeta[task.status] ?? taskStatusMeta.Open
                  return (
                    <div key={task.id} className="rounded-2xl border p-4">
                      <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
                        <div className="space-y-2">
                          <div className="flex flex-wrap items-center gap-2">
                            <Badge variant="secondary" className="gap-1">
                              <Star className="h-3 w-3" />
                              #{index + 1} Match {task.matchScore}%
                            </Badge>
                            <Badge variant={status.tone}>{status.label}</Badge>
                            <Badge variant="outline">{getCategoryLabel(task.category)}</Badge>
                          </div>
                          <div>
                            <h3 className="text-lg font-semibold">{task.title}</h3>
                            <p className="mt-1 text-sm text-muted-foreground line-clamp-3">{task.description}</p>
                          </div>
                          <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                            {task.reasons.map((reason) => (
                              <span key={reason} className="rounded-full bg-accent px-2.5 py-1 text-accent-foreground">
                                {reason}
                              </span>
                            ))}
                          </div>
                        </div>
                        <div className="w-full max-w-xs shrink-0 space-y-3 rounded-xl bg-muted/40 p-4">
                          <div>
                            <p className="text-xs text-muted-foreground">Краткая сводка</p>
                            <p className="mt-1 whitespace-pre-line text-sm">{getTaskSummary(task)}</p>
                          </div>
                          <div className="flex gap-2">
                            <Button className="flex-1" asChild>
                              <Link href={`/tasks/${task.id}`}>Apply / Открыть</Link>
                            </Button>
                            <Button variant="outline" asChild>
                              <Link href="/tasks">Каталог</Link>
                            </Button>
                          </div>
                        </div>
                      </div>
                    </div>
                  )
                })}
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Последние события</CardTitle>
            <CardDescription>Что сейчас происходит по аккаунту</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {[
              {
                icon: CheckCircle,
                color: 'text-primary',
                title: 'Профиль активен',
                description: user?.emailConfirmed ? 'Email подтверждён, можно откликаться на заказы.' : 'Подтвердите email для большего доверия заказчиков.',
              },
              {
                icon: Clock,
                color: 'text-muted-foreground',
                title: 'Заявки в обработке',
                description: `${pendingApplications.length} заявок ждут ответа от заказчиков.`,
              },
              {
                icon: TrendingUp,
                color: 'text-secondary',
                title: 'AI-проекты',
                description: `${runs.length} запусков IDE, ${runs.filter((run) => run.status === 'Completed').length} завершено успешно.`,
              },
            ].map((item) => (
              <div key={item.title} className="flex gap-3">
                <item.icon className={`mt-0.5 h-5 w-5 shrink-0 ${item.color}`} />
                <div>
                  <p className="text-sm font-medium">{item.title}</p>
                  <p className="text-sm text-muted-foreground">{item.description}</p>
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
