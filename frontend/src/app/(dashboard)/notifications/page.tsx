'use client'

import { useEffect, useState } from 'react'
import { useAuth } from '@/lib/auth'
import { chatApi, NotificationDto } from '@/lib/chat-api'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Bell, CheckCheck, Loader2 } from 'lucide-react'

export default function NotificationsPage() {
  const { user } = useAuth()
  const [notifications, setNotifications] = useState<NotificationDto[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (user) load()
  }, [user])

  async function load() {
    try {
      const data = await chatApi.getNotifications(false, 1, 50)
      setNotifications(data.items)
    } catch {}
    setLoading(false)
  }

  async function markAllRead() {
    await chatApi.markAllAsRead()
    await load()
  }

  async function markRead(id: string) {
    await chatApi.markAsRead(id)
    await load()
  }

  const unreadCount = notifications.filter((n) => !n.isRead).length

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Уведомления</h1>
          <p className="text-muted-foreground">{unreadCount} непрочитанных</p>
        </div>
        {unreadCount > 0 && (
          <Button variant="outline" size="sm" onClick={markAllRead}>
            <CheckCheck className="mr-2 h-4 w-4" /> Прочитать все
          </Button>
        )}
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-6 w-6 animate-spin text-primary" />
        </div>
      ) : notifications.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-4 py-12">
            <Bell className="h-10 w-10 text-muted-foreground" />
            <p className="text-muted-foreground">Нет уведомлений</p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-2">
          {notifications.map((n) => (
            <Card key={n.id} className={`transition-all ${!n.isRead ? 'border-primary/20 bg-primary/5' : ''}`}>
              <CardContent className="flex items-start gap-3 p-4">
                <div className={`mt-1 h-2 w-2 rounded-full shrink-0 ${n.isRead ? 'bg-transparent' : 'bg-primary'}`} />
                <div className="flex-1 min-w-0 space-y-1">
                  <div className="flex items-center gap-2">
                    <p className="text-sm font-medium">{n.title}</p>
                    <Badge variant="outline" className="text-[10px]">{n.type}</Badge>
                  </div>
                  <p className="text-sm text-muted-foreground">{n.message}</p>
                  <p className="text-xs text-muted-foreground">{new Date(n.createdAt).toLocaleString()}</p>
                </div>
                {!n.isRead && (
                  <Button variant="ghost" size="sm" onClick={() => markRead(n.id)} className="shrink-0">
                    Прочитано
                  </Button>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
