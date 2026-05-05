'use client'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { CheckCircle2, XCircle } from 'lucide-react'
import type { QualityGateSnapshot } from '@/lib/app-generation-api'

/**
 * Vertical timeline of quality-gate snapshots. Each gate shows stage, score,
 * pass/fail icon, and the list of `reasons` that justified the verdict.
 *
 * P2-4 of audit roadmap: this is the primary "checkpoint" affordance —
 * operators use it to see at-a-glance which stage failed and why.
 */
export function QualityGateTimeline({ gates }: { gates: QualityGateSnapshot[] }) {
  if (!gates || gates.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Контрольные точки</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">Пока нет данных по проверкам качества.</p>
        </CardContent>
      </Card>
    )
  }

  // Sort by evaluation time so the visual order matches execution order.
  const sorted = [...gates].sort(
    (a, b) => new Date(a.evaluatedAtUtc).getTime() - new Date(b.evaluatedAtUtc).getTime()
  )

  return (
    <Card>
      <CardHeader>
        <CardTitle>Контрольные точки ({sorted.length})</CardTitle>
      </CardHeader>
      <CardContent>
        <ol className="relative space-y-4 border-l border-border pl-6">
          {sorted.map((gate, idx) => (
            <li key={`${gate.stage}-${idx}`} className="relative">
              <span
                className={`absolute -left-[31px] flex h-5 w-5 items-center justify-center rounded-full ${
                  gate.passed ? 'bg-green-100 dark:bg-green-900/40' : 'bg-red-100 dark:bg-red-900/40'
                }`}
                aria-hidden
              >
                {gate.passed ? (
                  <CheckCircle2 className="h-4 w-4 text-green-700 dark:text-green-400" />
                ) : (
                  <XCircle className="h-4 w-4 text-red-700 dark:text-red-400" />
                )}
              </span>
              <div className="flex flex-wrap items-baseline gap-2">
                <span className="font-medium">{gate.stage}</span>
                <Badge variant={gate.passed ? 'success' : 'destructive'}>score {gate.score}</Badge>
                <span className="text-xs text-muted-foreground">
                  {new Date(gate.evaluatedAtUtc).toLocaleString()}
                </span>
              </div>
              {gate.reasons && gate.reasons.length > 0 && (
                <ul className="mt-1 list-disc pl-5 text-sm text-muted-foreground">
                  {gate.reasons.map((reason, i) => (
                    <li key={i} className="break-words">
                      <code className="text-xs">{reason}</code>
                    </li>
                  ))}
                </ul>
              )}
            </li>
          ))}
        </ol>
      </CardContent>
    </Card>
  )
}
