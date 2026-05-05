'use client'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Pause, Play, Ban, Download, RefreshCw } from 'lucide-react'
import {
  pauseRun,
  resumeRun,
  cancelRun,
  exportDiagnostics,
  isRunActive,
  isRunTerminal,
} from '@/lib/app-generation-api'

/**
 * Operator action panel for an active or terminal run. Pause/Resume only
 * make sense while the run is active; Cancel only while non-terminal;
 * Export Diagnostics is always available for diagnostics extraction.
 *
 * Implements optimistic UI: clicks immediately disable buttons until the
 * request resolves, then trigger an `onChanged` callback so the parent
 * can refetch the report.
 */
export function RunActions({
  runId,
  status,
  onChanged,
}: {
  runId: string
  status: string
  onChanged: () => void
}) {
  const [busy, setBusy] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [exportPath, setExportPath] = useState<string | null>(null)

  async function run(action: string, fn: () => Promise<unknown>) {
    setBusy(action)
    setError(null)
    try {
      await fn()
      onChanged()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось выполнить действие')
    } finally {
      setBusy(null)
    }
  }

  const active = isRunActive(status)
  const terminal = isRunTerminal(status)
  const paused = status === 'Paused' // P1 paused state if backend ever exposes it

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-2">
        <Button variant="outline" size="sm" onClick={onChanged} disabled={busy !== null}>
          <RefreshCw className="mr-1 h-4 w-4" /> Обновить
        </Button>
        {active && !paused && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => run('pause', () => pauseRun(runId))}
            disabled={busy !== null}
          >
            <Pause className="mr-1 h-4 w-4" /> Пауза
          </Button>
        )}
        {(paused || (active && status === 'Paused')) && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => run('resume', () => resumeRun(runId))}
            disabled={busy !== null}
          >
            <Play className="mr-1 h-4 w-4" /> Продолжить
          </Button>
        )}
        {!terminal && (
          <Button
            variant="destructive"
            size="sm"
            onClick={() =>
              run('cancel', () =>
                cancelRun(runId, { actor: 'ui', reason: 'cancelled by operator' })
              )
            }
            disabled={busy !== null}
          >
            <Ban className="mr-1 h-4 w-4" /> Отменить
          </Button>
        )}
        <Button
          variant="outline"
          size="sm"
          onClick={() =>
            run('export', async () => {
              const result = (await exportDiagnostics(runId)) as { artifactPath?: string } | null
              setExportPath(result?.artifactPath ?? 'экспорт сохранён')
            })
          }
          disabled={busy !== null}
        >
          <Download className="mr-1 h-4 w-4" /> Экспорт диагностики
        </Button>
      </div>
      {error && <p className="text-xs text-red-600">{error}</p>}
      {exportPath && (
        <p className="text-xs text-muted-foreground">
          Артефакт: <code>{exportPath}</code>
        </p>
      )}
    </div>
  )
}
