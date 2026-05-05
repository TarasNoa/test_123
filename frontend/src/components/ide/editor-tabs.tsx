'use client'

import { X } from 'lucide-react'
import { cn } from '@/lib/utils'
import { ScrollArea, ScrollBar } from '@/components/ui/scroll-area'

interface Tab {
  path: string
  name: string
}

interface EditorTabsProps {
  tabs: Tab[]
  activeTab: string
  onSelect: (path: string) => void
  onClose: (path: string) => void
}

export function EditorTabs({ tabs, activeTab, onSelect, onClose }: EditorTabsProps) {
  if (tabs.length === 0) return null

  return (
    <ScrollArea className="w-full">
      <div className="flex border-b bg-muted/30">
        {tabs.map((tab) => (
          <button
            key={tab.path}
            onClick={() => onSelect(tab.path)}
            className={cn(
              'group flex items-center gap-1.5 border-r px-3 py-1.5 text-xs shrink-0 transition-colors',
              tab.path === activeTab
                ? 'bg-background text-foreground border-b-2 border-b-primary'
                : 'text-muted-foreground hover:bg-background/50'
            )}
          >
            <span className="truncate max-w-[120px]">{tab.name}</span>
            <span
              role="button"
              onClick={(e) => {
                e.stopPropagation()
                onClose(tab.path)
              }}
              className="ml-1 rounded p-0.5 opacity-0 group-hover:opacity-100 hover:bg-muted transition-opacity"
            >
              <X className="h-3 w-3" />
            </span>
          </button>
        ))}
      </div>
      <ScrollBar orientation="horizontal" />
    </ScrollArea>
  )
}
