'use client'

import { useEffect, useMemo, useState } from 'react'
import Link from 'next/link'
import { useAuth } from '@/lib/auth'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { Loader2, Mail, ShieldCheck, Wallet, Sparkles, Briefcase, Code2 } from 'lucide-react'
import { paymentsApi, type Transaction, type Wallet as WalletData } from '@/lib/payments-api'
import { api } from '@/lib/api'
import { listRuns, type RunSummary } from '@/lib/app-generation-api'
import { buildSkillMetrics, type TaskApplication } from '@/lib/marketplace'

export default function ProfilePage() {
  const { user } = useAuth()
  const [wallet, setWallet] = useState<WalletData | null>(null)
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [applications, setApplications] = useState<TaskApplication[]>([])
  const [runs, setRuns] = useState<RunSummary[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false

    async function loadProfile() {
      try {
        const [walletData, transactionData, applicationData, runData] = await Promise.all([
          paymentsApi.getWallet().catch(() => null),
          paymentsApi.getTransactions({ page: 1, pageSize: 20 }).catch(() => null),
          api<TaskApplication[]>('/tasks/my/applications').catch(() => []),
          listRuns().catch(() => []),
        ])

        if (cancelled) return
        setWallet(walletData)
        setTransactions(transactionData?.transactions ?? [])
        setApplications(applicationData)
        setRuns(runData)
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    loadProfile()
    return () => {
      cancelled = true
    }
  }, [])

  const skillMetrics = useMemo(() => buildSkillMetrics(user, applications, runs), [user, applications, runs])
  const completedRuns = runs.filter((run) => run.status === 'Completed').length
  const acceptedApplications = applications.filter((app) => app.status === 'Accepted').length
  const profileStrength = Math.min(100, 45 + completedRuns * 8 + acceptedApplications * 7 + (user?.emailConfirmed ? 12 : 0))
  const totalIncome = transactions
    .filter((transaction) => transaction.status === 'Completed' && transaction.amount > 0)
    .reduce((sum, transaction) => sum + transaction.amount, 0)

  if (loading) {
    return (
      <div className="flex items-center justify-center py-16">
        <Loader2 className="h-6 w-6 animate-spin text-primary" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-2 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Профиль</h1>
          <p className="text-muted-foreground">Личный профиль, навыки, проекты и рабочая статистика.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" asChild>
            <Link href="/settings">Настройки</Link>
          </Button>
          <Button asChild className="bg-gradient-to-r from-secondary to-primary text-secondary-foreground hover:opacity-90">
            <Link href="/ide">
              <Code2 className="mr-2 h-4 w-4" />
              Перейти в IDE
            </Link>
          </Button>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1.1fr,1.4fr]">
        <Card>
          <CardHeader>
            <CardTitle>Личная карточка</CardTitle>
            <CardDescription>Как вас видят заказчики внутри платформы</CardDescription>
          </CardHeader>
          <CardContent className="space-y-5">
            <div className="flex items-center gap-4">
              <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-secondary text-secondary-foreground text-xl font-bold">
                {user?.displayName?.[0]?.toUpperCase() ?? '?'}
              </div>
              <div className="min-w-0">
                <p className="text-lg font-semibold">{user?.displayName}</p>
                <p className="truncate text-sm text-muted-foreground">{user?.email}</p>
              </div>
            </div>

            <div className="space-y-2">
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Сила профиля</span>
                <span className="font-medium">{profileStrength}%</span>
              </div>
              <Progress value={profileStrength} className="h-2.5" />
            </div>

            <div className="space-y-3 rounded-xl border p-4">
              <div className="flex items-center gap-2 text-sm">
                <Mail className="h-4 w-4 text-primary" />
                <span>Email</span>
                <Badge variant={user?.emailConfirmed ? 'default' : 'destructive'} className="ml-auto">
                  {user?.emailConfirmed ? 'Подтверждён' : 'Не подтверждён'}
                </Badge>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <ShieldCheck className="h-4 w-4 text-primary" />
                <span>2FA</span>
                <Badge variant={user?.twoFactorEnabled ? 'default' : 'outline'} className="ml-auto">
                  {user?.twoFactorEnabled ? 'Включено' : 'Выключено'}
                </Badge>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <Wallet className="h-4 w-4 text-primary" />
                <span>Баланс</span>
                <span className="ml-auto font-medium">${wallet?.balance?.toFixed(2) ?? '0.00'}</span>
              </div>
            </div>

            <div className="flex flex-wrap gap-2">
              {user?.roles.map((role) => (
                <Badge key={role} variant="secondary">{role}</Badge>
              ))}
            </div>
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Навыки и специализация</CardTitle>
              <CardDescription>Оценка на основе заявок, завершённых проектов и AI-активности</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-4 md:grid-cols-2">
              {skillMetrics.map((metric) => (
                <div key={metric.key} className="rounded-xl border p-4">
                  <div className="mb-3 flex items-center justify-between">
                    <div>
                      <p className="font-medium">{metric.label}</p>
                      <p className="text-xs text-muted-foreground">{metric.hint}</p>
                    </div>
                    <Badge variant="secondary">{metric.value}/10</Badge>
                  </div>
                  <Progress value={metric.value * 10} className="h-2" />
                </div>
              ))}
            </CardContent>
          </Card>

          <div className="grid gap-4 md:grid-cols-3">
            <Card>
              <CardHeader className="pb-2">
                <CardDescription>Доход</CardDescription>
                <CardTitle>${totalIncome.toFixed(2)}</CardTitle>
              </CardHeader>
              <CardContent className="text-xs text-muted-foreground">Завершённые поступления по аккаунту</CardContent>
            </Card>
            <Card>
              <CardHeader className="pb-2">
                <CardDescription>Одобрено заявок</CardDescription>
                <CardTitle>{acceptedApplications}</CardTitle>
              </CardHeader>
              <CardContent className="text-xs text-muted-foreground">
                <span className="inline-flex items-center gap-1">
                  <Briefcase className="h-3.5 w-3.5" />
                  Активные рабочие сделки
                </span>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="pb-2">
                <CardDescription>AI-проекты</CardDescription>
                <CardTitle>{completedRuns}</CardTitle>
              </CardHeader>
              <CardContent className="text-xs text-muted-foreground">
                <span className="inline-flex items-center gap-1">
                  <Sparkles className="h-3.5 w-3.5" />
                  Завершённые генерации в IDE
                </span>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </div>
  )
}
