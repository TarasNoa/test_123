'use client'

import type { AppGenerationReport } from '@/lib/app-generation-api'
import { Activity, FileCode, GitBranch, Cpu } from 'lucide-react'

interface StatusBarProps {
  report: AppGenerationReport | null
  activeFile: string | null
}

export function StatusBar({ report, activeFile }: StatusBarProps) {
  const fileCount = report?.files?.length ?? 0
  const iterCount = report?.iterations?.length ?? 0
  const status = report?.status ?? 'Ready'

  return (
    <div className="flex h-6 items-center justify-between border-t bg-muted/30 px-3 text-[11px] text-muted-foreground">
      <div className="flex items-center gap-4">
        <span className="flex items-center gap-1">
          <Activity className="h-3 w-3" />
          {status}
        </span>
        <span className="flex items-center gap-1">
          <FileCode className="h-3 w-3" />
          {fileCount} файлов
        </span>
        {iterCount > 0 && (
          <span className="flex items-center gap-1">
            <GitBranch className="h-3 w-3" />
            Итерация {iterCount}
          </span>
        )}
      </div>
      <div className="flex items-center gap-4">
        {activeFile && <span className="truncate max-w-[200px]">{activeFile}</span>}
        <span className="flex items-center gap-1">
          <Cpu className="h-3 w-3" /> AI Agent
        </span>
      </div>
    </div>
  )
}
