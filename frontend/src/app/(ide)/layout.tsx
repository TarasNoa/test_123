'use client'

import { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { useAuth } from '@/lib/auth'

export default function IdeLayout({ children }: { children: React.ReactNode }) {
  const { user, loading } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!loading && !user) router.replace('/login')
  }, [loading, user, router])

  if (loading) {
    return (
      <div className="flex h-screen items-center justify-center bg-background">
        <div className="flex flex-col items-center gap-3">
          <div className="h-10 w-10 animate-spin rounded-full border-4 border-secondary border-t-transparent" />
          <p className="text-sm text-muted-foreground">Загрузка IDE...</p>
        </div>
      </div>
    )
  }

  if (!user) return null

  return <div className="h-screen overflow-hidden bg-background">{children}</div>
}
