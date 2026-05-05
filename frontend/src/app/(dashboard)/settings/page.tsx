'use client'

import { useState } from 'react'
import { useAuth } from '@/lib/auth'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import { Separator } from '@/components/ui/separator'
import { User, Shield, Bell, Key } from 'lucide-react'

export default function SettingsPage() {
  const { user } = useAuth()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Настройки</h1>
        <p className="text-muted-foreground">Управление аккаунтом и безопасностью</p>
      </div>

      {/* Profile */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <User className="h-5 w-5 text-primary" />
            <CardTitle>Профиль</CardTitle>
          </div>
          <CardDescription>Основная информация</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center gap-4">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-secondary text-secondary-foreground text-xl font-bold">
              {user?.displayName?.[0]?.toUpperCase() ?? '?'}
            </div>
            <div>
              <p className="font-medium">{user?.displayName}</p>
              <p className="text-sm text-muted-foreground">{user?.email}</p>
              <div className="flex gap-1 mt-1">
                {user?.roles.map((r) => (
                  <Badge key={r} variant="secondary" className="text-xs">{r}</Badge>
                ))}
              </div>
            </div>
          </div>
          <Separator />
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-muted-foreground">Email подтверждён</span>
              <div className="mt-1">
                <Badge variant={user?.emailConfirmed ? 'default' : 'destructive'}>
                  {user?.emailConfirmed ? 'Да' : 'Нет'}
                </Badge>
              </div>
            </div>
            <div>
              <span className="text-muted-foreground">Зарегистрирован</span>
              <p className="mt-1">{user?.createdAt ? new Date(user.createdAt).toLocaleDateString() : '—'}</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Security */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Shield className="h-5 w-5 text-secondary" />
            <CardTitle>Безопасность</CardTitle>
          </div>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium text-sm">Двухфакторная аутентификация</p>
              <p className="text-xs text-muted-foreground">Защитите аккаунт с помощью TOTP</p>
            </div>
            <Switch checked={user?.twoFactorEnabled} disabled />
          </div>
          <Separator />
          <div className="space-y-3">
            <p className="font-medium text-sm">Сменить пароль</p>
            <div className="space-y-2">
              <Label htmlFor="current-pw">Текущий пароль</Label>
              <Input id="current-pw" type="password" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="new-pw">Новый пароль</Label>
              <Input id="new-pw" type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} />
            </div>
            <Button disabled={!currentPassword || !newPassword}>Сменить пароль</Button>
          </div>
        </CardContent>
      </Card>

      {/* Notifications */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Bell className="h-5 w-5 text-primary" />
            <CardTitle>Уведомления</CardTitle>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {['Новые отклики на задания', 'Сообщения в чатах', 'Обновления платежей', 'AI генерация завершена'].map((item) => (
            <div key={item} className="flex items-center justify-between">
              <span className="text-sm">{item}</span>
              <Switch defaultChecked />
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  )
}
