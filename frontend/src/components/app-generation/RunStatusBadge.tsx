'use client'
import { Badge } from '@/components/ui/badge'
import { Loader2 } from 'lucide-react'
import { isRunActive } from '@/lib/app-generation-api'

const VARIANT_BY_STATUS: Record<string, 'success' | 'destructive' | 'warning' | 'info' | 'secondary'> = {
  Created: 'secondary',
  Planning: 'info',
  Generating: 'info',
  Testing: 'info',
  Completed: 'success',
  Failed: 'destructive',
  Cancelled: 'warning',
}

const LABEL_BY_STATUS: Record<string, string> = {
  Created: 'Создан',
  Planning: 'Планирование',
  Generating: 'Генерация',
  Testing: 'Тестирование',
  Completed: 'Завершён',
  Failed: 'Ошибка',
  Cancelled: 'Отменён',
}

export function RunStatusBadge({ status }: { status: string }) {
  const variant = VARIANT_BY_STATUS[status] ?? 'secondary'
  const label = LABEL_BY_STATUS[status] ?? status
  return (
    <Badge variant={variant} className="gap-1">
      {isRunActive(status) && <Loader2 className="h-3 w-3 animate-spin" />}
      {label}
    </Badge>
  )
}
