'use client'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import type { IterationCycle } from '@/lib/app-generation-api'

export function IterationList({ iterations }: { iterations: IterationCycle[] }) {
  if (!iterations || iterations.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Итерации</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">Итераций ещё не было.</p>
        </CardContent>
      </Card>
    )
  }
  return (
    <Card>
      <CardHeader>
        <CardTitle>Итерации ({iterations.length})</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {iterations.map((it) => (
          <div key={it.id ?? it.number} className="rounded-md border p-3">
            <div className="flex items-center gap-2">
              <span className="font-medium">Итерация {it.number}</span>
              {it.status && <Badge variant="outline">{it.status}</Badge>}
              {it.startedAt && (
                <span className="text-xs text-muted-foreground">
                  {new Date(it.startedAt).toLocaleTimeString()}
                </span>
              )}
            </div>
            {it.errors && it.errors.length > 0 && (
              <ul className="mt-2 space-y-1">
                {it.errors.slice(0, 5).map((err, i) => (
                  <li key={i} className="text-sm">
                    {err.code && (
                      <code className="mr-2 rounded bg-muted px-1.5 py-0.5 text-xs">{err.code}</code>
                    )}
                    <span className="text-muted-foreground">{err.message}</span>
                  </li>
                ))}
                {it.errors.length > 5 && (
                  <li className="text-xs text-muted-foreground">…и ещё {it.errors.length - 5}</li>
                )}
              </ul>
            )}
          </div>
        ))}
      </CardContent>
    </Card>
  )
}
