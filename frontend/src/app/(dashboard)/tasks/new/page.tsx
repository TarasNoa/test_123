'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { api } from '@/lib/api'
import { ArrowLeft } from 'lucide-react'

const categories = [
  { value: 'Development', label: 'Разработка' },
  { value: 'Design', label: 'Дизайн' },
  { value: 'Marketing', label: 'Маркетинг' },
  { value: 'Writing', label: 'Тексты' },
  { value: 'DataEntry', label: 'Ввод данных' },
  { value: 'Translation', label: 'Перевод' },
  { value: 'Other', label: 'Другое' },
]

export default function NewTaskPage() {
  const router = useRouter()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState({
    title: '',
    description: '',
    category: 'Development',
    budget: '',
    currency: 'USD',
    deadline: '',
  })

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const result = await api<{ id: string }>('/tasks', {
        method: 'POST',
        body: JSON.stringify({ ...form, budget: parseFloat(form.budget) }),
      })
      router.push(`/tasks/${result.id}`)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to create task')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="mx-auto max-w-2xl space-y-4">
      <Button variant="ghost" size="sm" asChild>
        <Link href="/tasks">
          <ArrowLeft className="mr-2 h-4 w-4" /> Назад к заказам
        </Link>
      </Button>

      <Card>
        <CardHeader>
          <CardTitle>Создать заказ</CardTitle>
          <CardDescription>Опишите проект, чтобы быстро получить заявки от подходящих фрилансеров</CardDescription>
        </CardHeader>
        <form onSubmit={onSubmit}>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="title">Название</Label>
              <Input
                id="title"
                required
                minLength={10}
                maxLength={200}
                placeholder="Например: Разработать лендинг для стартапа"
                value={form.title}
                onChange={(e) => setForm({ ...form, title: e.target.value })}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="description">Описание</Label>
              <Textarea
                id="description"
                required
                minLength={50}
                maxLength={5000}
                rows={6}
                placeholder="Подробное описание задачи, требования, технологии..."
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Категория</Label>
                <Select value={form.category} onValueChange={(v: string) => setForm({ ...form, category: v })}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {categories.map((c) => (
                      <SelectItem key={c.value} value={c.value}>{c.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="deadline">Дедлайн</Label>
                <Input
                  id="deadline"
                  type="datetime-local"
                  value={form.deadline}
                  onChange={(e) => setForm({ ...form, deadline: e.target.value })}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="budget">Бюджет</Label>
                <Input
                  id="budget"
                  type="number"
                  required
                  min={1}
                  placeholder="1000"
                  value={form.budget}
                  onChange={(e) => setForm({ ...form, budget: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="currency">Валюта</Label>
                <Input
                  id="currency"
                  required
                  maxLength={3}
                  value={form.currency}
                  onChange={(e) => setForm({ ...form, currency: e.target.value.toUpperCase() })}
                />
              </div>
            </div>

            {error && <div className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error}</div>}
          </CardContent>
          <CardFooter className="flex justify-between">
            <Button variant="outline" asChild>
              <Link href="/tasks">Отмена</Link>
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? 'Создание...' : 'Создать заказ'}
            </Button>
          </CardFooter>
        </form>
      </Card>
    </div>
  )
}
