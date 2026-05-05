'use client'

import { ScrollArea } from '@/components/ui/scroll-area'
import { Badge } from '@/components/ui/badge'
import type { AppGenerationReport } from '@/lib/app-generation-api'

interface OutputPanelProps {
  report: AppGenerationReport | null
}

export function OutputPanel({ report }: OutputPanelProps) {
  const errors = report?.outstandingErrors ?? []
  const iterations = report?.iterations ?? []
  const lastIter = iterations[iterations.length - 1]

  return (
    <div className="flex h-full flex-col">
      <div className="flex items-center justify-between border-b px-3 py-1.5">
        <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Output</span>
        {errors.length > 0 && (
          <Badge variant="destructive" className="text-[10px]">{errors.length} ошибок</Badge>
        )}
      </div>
      <ScrollArea className="flex-1">
        <div className="p-2 font-mono text-[11px] space-y-1">
          {report?.plan && (
            <div className="text-muted-foreground">[plan] {report.plan.applicationName} — {report.plan.techStack?.languages?.join(', ')}</div>
          )}
          {iterations.map((iter, i) => (
            <div key={i} className={iter.errors?.length ? 'text-destructive' : 'text-primary'}>
              [iter #{iter.number}] {iter.status ?? 'running'}
              {iter.errors?.map((e, j) => (
                <div key={j} className="pl-4 text-destructive/80">{e.message}</div>
              ))}
            </div>
          ))}
          {errors.length > 0 && (
            <div className="pt-2 border-t">
              {errors.map((e, i) => (
                <div key={i} className="text-destructive">[error] {e.message}</div>
              ))}
            </div>
          )}
          {report?.status === 'Completed' && (
            <div className="text-primary font-semibold pt-2">Build succeeded. {report.files?.length ?? 0} files generated.</div>
          )}
          {!report && (
            <div className="text-muted-foreground py-4 text-center">Запустите генерацию для просмотра вывода</div>
          )}
        </div>
      </ScrollArea>
    </div>
  )
}
