'use client'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Sparkles } from 'lucide-react'
import { startRun } from '@/lib/app-generation-api'

/**
 * Form for kicking off a new autonomous app generation run.
 *
 * Backend `POST /api/ide/app-generation/start` returns 202 Accepted with a
 * polling hint — there is no synchronous run id, so after submission the form
 * triggers `onStarted` to refresh the list and the user clicks the new run.
 */
export function StartRunForm({ onStarted }: { onStarted: () => void }) {
  const [request, setRequest] = useState('')
  const [maxIterations, setMaxIterations] = useState(6)
  const [tenantId, setTenantId] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!request.trim()) {
      setError('Опишите задачу для генератора')
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      await startRun({
        userRequest: request.trim(),
        maxIterations,
        tenantId: tenantId.trim() || undefined,
      })
      setRequest('')
      // Give the backend a moment to persist the orchestrator before refreshing.
      setTimeout(onStarted, 1000)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось запустить генерацию')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Sparkles className="h-5 w-5" /> Запустить генерацию
        </CardTitle>
        <CardDescription>
          Опишите задачу естественным языком — оркестратор сгенерирует план, код, прогонит сборку и тесты.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={onSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="userRequest">Описание задачи</Label>
            <Textarea
              id="userRequest"
              value={request}
              onChange={(e) => setRequest(e.target.value)}
              placeholder="Например: Build a multi-tenant e-commerce platform in ASP.NET Core 8 with PostgreSQL, JWT auth, Stripe payments, RabbitMQ events..."
              rows={6}
              disabled={submitting}
              required
            />
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="maxIterations">Лимит итераций</Label>
              <Input
                id="maxIterations"
                type="number"
                min={1}
                max={20}
                value={maxIterations}
                onChange={(e) => setMaxIterations(Number(e.target.value))}
                disabled={submitting}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="tenantId">Tenant ID (опционально)</Label>
              <Input
                id="tenantId"
                value={tenantId}
                onChange={(e) => setTenantId(e.target.value)}
                placeholder="например, acme-prod"
                disabled={submitting}
              />
            </div>
          </div>
          {error && <p className="text-sm text-red-600">{error}</p>}
          <Button type="submit" disabled={submitting}>
            {submitting ? 'Отправка...' : 'Запустить'}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
