'use client'

import { useEffect, useState } from 'react'
import { useAuth } from '@/lib/auth'
import { paymentsApi, Transaction } from '@/lib/payments-api'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { ArrowDownLeft, ArrowUpRight, Loader2, Filter } from 'lucide-react'

const typeLabels: Record<string, string> = {
  Deposit: 'Пополнение',
  Withdrawal: 'Вывод',
  Payment: 'Оплата',
  Refund: 'Возврат',
  EscrowHold: 'Эскроу',
  EscrowRelease: 'Разблокировка',
  Fee: 'Комиссия',
}

const statusConfig: Record<string, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
  Completed: { label: 'Выполнено', variant: 'default' },
  Pending: { label: 'В обработке', variant: 'secondary' },
  Failed: { label: 'Ошибка', variant: 'destructive' },
  Cancelled: { label: 'Отменено', variant: 'outline' },
}

export default function TransactionsPage() {
  const { user } = useAuth()
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [filter, setFilter] = useState({ type: '', status: '' })
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    if (user) loadTransactions()
  }, [user, filter])

  async function loadTransactions() {
    try {
      setIsLoading(true)
      const data = await paymentsApi.getTransactions(filter)
      setTransactions(data.transactions)
    } finally {
      setIsLoading(false)
    }
  }

  const isIncome = (type: string) => ['Refund', 'Deposit', 'EscrowRelease'].includes(type)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Транзакции</h1>
        <p className="text-muted-foreground">История всех платёжных операций</p>
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <CardTitle>Список транзакций</CardTitle>
            <div className="flex gap-2">
              <Select value={filter.type} onValueChange={(v: string) => setFilter((f) => ({ ...f, type: v }))}>
                <SelectTrigger className="w-[150px]">
                  <SelectValue placeholder="Тип" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">Все типы</SelectItem>
                  <SelectItem value="Deposit">Пополнение</SelectItem>
                  <SelectItem value="Payment">Оплата</SelectItem>
                  <SelectItem value="Refund">Возврат</SelectItem>
                  <SelectItem value="EscrowHold">Эскроу</SelectItem>
                </SelectContent>
              </Select>
              <Select value={filter.status} onValueChange={(v: string) => setFilter((f) => ({ ...f, status: v }))}>
                <SelectTrigger className="w-[150px]">
                  <SelectValue placeholder="Статус" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">Все статусы</SelectItem>
                  <SelectItem value="Completed">Выполнено</SelectItem>
                  <SelectItem value="Pending">В обработке</SelectItem>
                  <SelectItem value="Failed">Ошибка</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <CardDescription>
            {isLoading ? 'Загрузка...' : `${transactions.length} транзакций`}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-primary" />
            </div>
          ) : transactions.length === 0 ? (
            <p className="text-center text-muted-foreground py-8">Транзакций не найдено</p>
          ) : (
            <div className="space-y-2">
              {transactions.map((tx) => {
                const st = statusConfig[tx.status] ?? { label: tx.status, variant: 'outline' as const }
                const income = isIncome(tx.type)
                return (
                  <div key={tx.id} className="flex items-center justify-between rounded-lg border p-4 hover:bg-muted/30 transition-colors">
                    <div className="flex items-center gap-3">
                      <div className={`flex h-9 w-9 items-center justify-center rounded-full ${income ? 'bg-primary/10 text-primary' : 'bg-destructive/10 text-destructive'}`}>
                        {income ? <ArrowDownLeft className="h-4 w-4" /> : <ArrowUpRight className="h-4 w-4" />}
                      </div>
                      <div className="space-y-0.5">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-medium">{typeLabels[tx.type] ?? tx.type}</span>
                          <Badge variant={st.variant} className="text-xs">{st.label}</Badge>
                        </div>
                        <p className="text-xs text-muted-foreground">
                          {tx.description || 'Без описания'} &middot; {new Date(tx.createdAt).toLocaleString()}
                        </p>
                      </div>
                    </div>
                    <span className={`text-sm font-bold ${income ? 'text-primary' : 'text-destructive'}`}>
                      {income ? '+' : '-'}${tx.amount.toFixed(2)} {tx.currency}
                    </span>
                  </div>
                )
              })}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
