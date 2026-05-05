'use client'
import { useEffect, useState } from 'react'
import Link from 'next/link'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { listRuns, isRunActive, type RunSummary } from '@/lib/app-generation-api'
import { StartRunForm } from '@/components/app-generation/StartRunForm'
import { RunStatusBadge } from '@/components/app-generation/RunStatusBadge'
import { RefreshCw } from 'lucide-react'

/**
 * Audit P2-4: top-level dashboard for autonomous app generation runs.
 *  - Start form (kicks off /start; backend returns 202)
 *  - Polled list of runs (active runs refresh every 4s, terminal every 30s)
 *  - Click a row → /app-generation/[id] detail page with checkpoints/files.
 */
export default function AppGenerationPage() {
  const [runs, setRuns] = useState<RunSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  async function load() {
    try {
      const data = await listRuns()
      setRuns(data)
      setError(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось загрузить список')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
    const interval = setInterval(() => {
      const hasActive = runs.some((r) => isRunActive(r.status))
      // Active runs: poll every 4s; otherwise every 30s.
      if (hasActive || runs.length === 0) load()
    }, 4000)
    return () => clearInterval(interval)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [runs.length])

  const sorted = [...runs].sort(
    (a, b) =>
      new Date(b.startedAt ?? 0).getTime() - new Date(a.startedAt ?? 0).getTime()
  )

  return (
    <main className="container mx-auto max-w-5xl space-y-6 py-8">
      <header className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Автономная генерация приложений</h1>
          <p className="text-muted-foreground">
            Опишите задачу, оркестратор сам спланирует, сгенерирует и проверит код.
          </p>
        </div>
      </header>

      <StartRunForm onStarted={load} />

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0">
          <CardTitle>Прогоны ({runs.length})</CardTitle>
          <Button variant="ghost" size="sm" onClick={load} disabled={loading}>
            <RefreshCw className={`mr-1 h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            Обновить
          </Button>
        </CardHeader>
        <CardContent>
          {error && <p className="text-sm text-red-600">{error}</p>}
          {loading && runs.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">Загрузка...</p>
          ) : sorted.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">
              Прогонов ещё не было. Запустите первый выше.
            </p>
          ) : (
            <ul className="divide-y">
              {sorted.map((r) => (
                <li key={r.id}>
                  <Link
                    href={`/ide/${r.id}`}
                    className="flex items-center justify-between gap-4 py-3 hover:bg-accent/50 -mx-3 px-3 rounded"
                  >
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="font-medium">
                          {r.applicationName ?? 'Без названия'}
                        </span>
                        <RunStatusBadge status={r.status} />
                      </div>
                      <div className="mt-0.5 text-xs text-muted-foreground">
                        <code className="mr-2">{r.id.slice(0, 8)}</code>
                        {r.tenantId && <span className="mr-2">tenant: {r.tenantId}</span>}
                        {r.startedAt && (
                          <span>
                            запущен {new Date(r.startedAt).toLocaleString()}
                          </span>
                        )}
                      </div>
                      {r.failureReason && (
                        <div className="mt-1 truncate text-xs text-red-600">
                          {r.failureReason}
                        </div>
                      )}
                    </div>
                    <div className="text-right text-xs text-muted-foreground">
                      {typeof r.iterations === 'number' && (
                        <span>{r.iterations} итер.</span>
                      )}
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </main>
  )
}
