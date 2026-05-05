'use client'

import { useEffect, useState } from 'react'
import Link from 'next/link'
import { useAuth } from '@/lib/auth'
import { paymentsApi, Wallet, WalletEntry } from '@/lib/payments-api'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { Wallet as WalletIcon, ArrowUpRight, ArrowDownLeft, Lock, RefreshCw, Loader2 } from 'lucide-react'

export default function WalletPage() {
  const { user } = useAuth()
  const [wallet, setWallet] = useState<Wallet | null>(null)
  const [entries, setEntries] = useState<WalletEntry[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (user) loadWallet()
  }, [user])

  async function loadWallet() {
    try {
      setIsLoading(true)
      setError('')
      const w = await paymentsApi.getWallet()
      setWallet(w)
      const e = await paymentsApi.getWalletEntries(w.id, 1, 10)
      setEntries(e.entries)
    } catch {
      setError('Не удалось загрузить кошелёк')
    } finally {
      setIsLoading(false)
    }
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="h-6 w-6 animate-spin text-primary" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex flex-col items-center gap-4 py-20">
        <p className="text-destructive">{error}</p>
        <Button onClick={loadWallet} variant="outline">
          <RefreshCw className="mr-2 h-4 w-4" /> Повторить
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Кошелёк</h1>
          <p className="text-muted-foreground">Баланс и история операций</p>
        </div>
        <Button variant="outline" asChild>
          <Link href="/transactions">Все транзакции</Link>
        </Button>
      </div>

      {wallet && (
        <>
          <div className="grid gap-4 md:grid-cols-3">
            <Card className="border-primary/20">
              <CardHeader className="pb-2">
                <CardDescription className="flex items-center gap-2">
                  <WalletIcon className="h-4 w-4" /> Доступно
                </CardDescription>
                <CardTitle className="text-3xl text-primary">
                  ${wallet.balance.toFixed(2)}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <Button className="w-full">Пополнить</Button>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-2">
                <CardDescription className="flex items-center gap-2">
                  <Lock className="h-4 w-4" /> Эскроу
                </CardDescription>
                <CardTitle className="text-3xl text-secondary">
                  ${wallet.heldBalance.toFixed(2)}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">Заморожено в заданиях</p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-2">
                <CardDescription>Итого</CardDescription>
                <CardTitle className="text-3xl">
                  ${(wallet.balance + wallet.heldBalance).toFixed(2)}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">{wallet.currency}</p>
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Последние операции</CardTitle>
            </CardHeader>
            <CardContent>
              {entries.length === 0 ? (
                <p className="text-center text-muted-foreground py-6">Нет операций</p>
              ) : (
                <div className="space-y-2">
                  {entries.map((entry) => (
                    <div key={entry.id} className="flex items-center justify-between rounded-lg border p-3 hover:bg-muted/30 transition-colors">
                      <div className="flex items-center gap-3">
                        <div className={`flex h-9 w-9 items-center justify-center rounded-full ${entry.credit > 0 ? 'bg-primary/10 text-primary' : 'bg-destructive/10 text-destructive'}`}>
                          {entry.credit > 0 ? <ArrowDownLeft className="h-4 w-4" /> : <ArrowUpRight className="h-4 w-4" />}
                        </div>
                        <div>
                          <p className="text-sm font-medium">{entry.description || 'Операция'}</p>
                          <p className="text-xs text-muted-foreground">{new Date(entry.createdAt).toLocaleString()}</p>
                        </div>
                      </div>
                      <div className="text-right">
                        <p className={`text-sm font-semibold ${entry.credit > 0 ? 'text-primary' : 'text-destructive'}`}>
                          {entry.credit > 0 ? '+' : '-'}${(entry.credit || entry.debit).toFixed(2)}
                        </p>
                        <p className="text-xs text-muted-foreground">${entry.balanceAfter.toFixed(2)}</p>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </>
      )}
    </div>
  )
}
