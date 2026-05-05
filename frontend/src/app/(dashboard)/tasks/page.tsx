'use client'

import { useState, useEffect, useMemo } from 'react'
import Link from 'next/link'
import { useAuth } from '@/lib/auth'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { api } from '@/lib/api'
import { Plus, Search, Calendar, Users, DollarSign, Loader2, Sparkles, ArrowRight } from 'lucide-react'
import {
  buildRecommendedTasks,
  getCategoryLabel,
  taskStatusMeta,
  type MarketplaceTask,
} from '@/lib/marketplace'

const categories = ['Development', 'Design', 'Marketing', 'Writing', 'DataEntry', 'Translation', 'Other']

export default function TasksPage() {
  const { user } = useAuth()
  const [tasks, setTasks] = useState<MarketplaceTask[]>([])
  const [loading, setLoading] = useState(true)
  const [category, setCategory] = useState('')
  const [search, setSearch] = useState('')

  useEffect(() => {
    loadTasks()
  }, [category])

  async function loadTasks() {
    try {
      const params = new URLSearchParams()
      if (category) params.append('category', category)
      const data = await api<MarketplaceTask[]>(`/tasks?${params.toString()}`, { auth: false })
      setTasks(data)
    } catch {
      // ignore
    } finally {
      setLoading(false)
    }
  }

  const filtered = search
    ? tasks.filter((t) => t.title.toLowerCase().includes(search.toLowerCase()))
    : tasks
  const recommendedTasks = useMemo(() => buildRecommendedTasks(tasks, user), [tasks, user])

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Заказы и проекты</h1>
          <p className="text-muted-foreground">
            Выберите рекомендованный заказ или откройте карточку и отправьте заявку.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" asChild>
            <Link href="/my-applications">Мои заявки</Link>
          </Button>
          {user && (
            <Button asChild>
              <Link href="/tasks/new">
                <Plus className="mr-2 h-4 w-4" /> Создать заказ
              </Link>
            </Button>
          )}
        </div>
      </div>

      <Card className="border-secondary/20 bg-gradient-to-r from-secondary/15 via-background to-primary/10">
        <CardContent className="grid gap-4 p-5 lg:grid-cols-[1.3fr,1fr] lg:items-center">
          <div className="space-y-2">
            <div className="inline-flex items-center gap-2 rounded-full bg-background/80 px-3 py-1 text-xs font-medium text-secondary-foreground">
              <Sparkles className="h-3.5 w-3.5" />
              AI-подборка под ваш профиль
            </div>
            <h2 className="text-lg font-semibold">Сначала берите самые релевантные проекты</h2>
            <p className="text-sm text-muted-foreground">
              Ниже собраны заказы по навыкам, ролям и текущей активности. Откройте заказ, заполните форму Apply и отправьте заявку заказчику.
            </p>
          </div>
          <div className="grid gap-3 sm:grid-cols-3 lg:grid-cols-1">
            {recommendedTasks.length === 0 ? (
              <div className="rounded-xl border bg-background/80 p-4 text-sm text-muted-foreground">
                Пока нет открытых заказов для подборки.
              </div>
            ) : (
              recommendedTasks.map((task) => (
                <Link key={task.id} href={`/tasks/${task.id}`} className="rounded-xl border bg-background/80 p-4 transition-colors hover:border-primary/40">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <p className="truncate font-medium">{task.title}</p>
                      <p className="mt-1 line-clamp-2 text-xs text-muted-foreground">{task.description}</p>
                    </div>
                    <Badge variant="secondary">{task.matchScore}%</Badge>
                  </div>
                </Link>
              ))
            )}
          </div>
        </CardContent>
      </Card>

      <div className="flex flex-col gap-3 sm:flex-row">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Поиск по названию..."
            className="pl-9"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        <div className="flex gap-2 flex-wrap">
          <Button size="sm" variant={category === '' ? 'default' : 'outline'} onClick={() => setCategory('')}>
            Все
          </Button>
          {categories.map((c) => (
            <Button key={c} size="sm" variant={category === c ? 'default' : 'outline'} onClick={() => setCategory(c)}>
              {getCategoryLabel(c)}
            </Button>
          ))}
        </div>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-6 w-6 animate-spin text-primary" />
        </div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <p className="text-muted-foreground">Заданий не найдено</p>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {filtered.map((task) => (
            <Link key={task.id} href={`/tasks/${task.id}`}>
              <Card className="h-full transition-all hover:shadow-md hover:border-primary/20 cursor-pointer">
                <CardHeader className="pb-3">
                  <div className="flex items-start justify-between gap-2">
                    <CardTitle className="text-base line-clamp-2">{task.title}</CardTitle>
                    <Badge variant={taskStatusMeta[task.status]?.tone ?? 'outline'} className="shrink-0">
                      {taskStatusMeta[task.status]?.label ?? task.status}
                    </Badge>
                  </div>
                  <Badge variant="secondary" className="w-fit text-xs">{getCategoryLabel(task.category)}</Badge>
                </CardHeader>
                <CardContent className="space-y-3">
                  <p className="text-sm text-muted-foreground line-clamp-3">{task.description}</p>
                  <div className="flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                    <span className="flex items-center gap-1">
                      <DollarSign className="h-3 w-3" />
                      {task.budget} {task.currency}
                    </span>
                    <span className="flex items-center gap-1">
                      <Users className="h-3 w-3" />
                      {task.applicationCount} откликов
                    </span>
                    {task.deadline && (
                      <span className="flex items-center gap-1">
                        <Calendar className="h-3 w-3" />
                        {new Date(task.deadline).toLocaleDateString()}
                      </span>
                    )}
                  </div>
                  <div className="flex items-center justify-between pt-1">
                    <span className="text-xs text-muted-foreground">
                      {new Date(task.createdAt).toLocaleDateString('ru-RU')}
                    </span>
                    <span className="inline-flex items-center gap-1 text-xs font-medium text-primary">
                      Открыть и Apply
                      <ArrowRight className="h-3 w-3" />
                    </span>
                  </div>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}
