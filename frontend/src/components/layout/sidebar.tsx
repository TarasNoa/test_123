'use client'

import * as React from 'react'
import Link from 'next/link'
import { usePathname } from 'next/navigation'
import {
  LayoutDashboard,
  Briefcase,
  Code2,
  MessageSquare,
  Wallet,
  TrendingUp,
  Settings,
  Bell,
  Bot,
  UserCircle2,
  FileText,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Tooltip, TooltipContent, TooltipTrigger, TooltipProvider } from '@/components/ui/tooltip'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'

const navItems = [
  { href: '/dashboard', icon: LayoutDashboard, label: 'Дашборд' },
  { href: '/profile', icon: UserCircle2, label: 'Профиль' },
  { href: '/tasks', icon: Briefcase, label: 'Заказы' },
  { href: '/my-applications', icon: FileText, label: 'Мои заявки' },
  { href: '/ide', icon: Code2, label: 'IDE', accent: true },
  { href: '/chats', icon: MessageSquare, label: 'Чаты' },
  { href: '/ai', icon: Bot, label: 'AI Чат' },
  { href: '/wallet', icon: Wallet, label: 'Кошелёк' },
  { href: '/trading', icon: TrendingUp, label: 'Трейдинг' },
  { href: '/notifications', icon: Bell, label: 'Уведомления' },
  { href: '/settings', icon: Settings, label: 'Настройки' },
]

interface SidebarProps {
  collapsed: boolean
  onToggle: () => void
}

export function Sidebar({ collapsed, onToggle }: SidebarProps) {
  const pathname = usePathname()

  return (
    <TooltipProvider delayDuration={0}>
      <aside
        className={cn(
          'fixed left-0 top-0 z-40 flex h-screen flex-col border-r bg-sidebar transition-all duration-300',
          collapsed ? 'w-16' : 'w-60'
        )}
      >
        <div className={cn('flex h-14 items-center border-b px-3', collapsed ? 'justify-center' : 'justify-between')}>
          {!collapsed && (
            <Link href="/dashboard" className="flex items-center gap-2">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground font-bold text-sm">
                L4
              </div>
              <span className="text-lg font-bold tracking-tight">Libr4</span>
            </Link>
          )}
          {collapsed && (
            <Link href="/dashboard">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground font-bold text-sm">
                L4
              </div>
            </Link>
          )}
          <Button
            variant="ghost"
            size="icon"
            className={cn('h-7 w-7 shrink-0', collapsed && 'absolute -right-3 top-4 z-50 rounded-full border bg-background shadow-sm')}
            onClick={onToggle}
          >
            {collapsed ? <ChevronRight className="h-3.5 w-3.5" /> : <ChevronLeft className="h-3.5 w-3.5" />}
          </Button>
        </div>

        <ScrollArea className="flex-1 py-2">
          <nav className="flex flex-col gap-1 px-2">
            {navItems.map((item) => {
              const isActive = pathname === item.href || pathname.startsWith(item.href + '/')
              const link = (
                <Link
                  key={item.href}
                  href={item.href}
                  className={cn(
                    'group flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                    isActive
                      ? 'bg-sidebar-accent text-sidebar-primary'
                      : 'text-sidebar-foreground/70 hover:bg-sidebar-accent hover:text-sidebar-accent-foreground',
                    item.accent && !isActive && 'text-secondary hover:text-secondary',
                    collapsed && 'justify-center px-0'
                  )}
                >
                  <item.icon className={cn('h-5 w-5 shrink-0', item.accent && !isActive && 'text-secondary')} />
                  {!collapsed && <span>{item.label}</span>}
                </Link>
              )

              if (collapsed) {
                return (
                  <Tooltip key={item.href}>
                    <TooltipTrigger asChild>{link}</TooltipTrigger>
                    <TooltipContent side="right">{item.label}</TooltipContent>
                  </Tooltip>
                )
              }
              return link
            })}
          </nav>
        </ScrollArea>

        <Separator />
        <div className={cn('p-3', collapsed && 'flex justify-center')}>
          {!collapsed && (
            <p className="text-xs text-muted-foreground">
              Libr4 Platform v0.1
            </p>
          )}
        </div>
      </aside>
    </TooltipProvider>
  )
}
