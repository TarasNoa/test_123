import { api } from './api'

export interface Wallet {
  id: string
  userId: string
  balance: number
  heldBalance: number
  currency: string
  updatedAt: string
}

export interface WalletEntry {
  id: string
  transactionId: string
  credit: number
  debit: number
  balanceAfter: number
  description: string
  createdAt: string
}

export interface Transaction {
  id: string
  userId: string
  type: 'Deposit' | 'Withdrawal' | 'Payment' | 'Refund' | 'EscrowHold' | 'EscrowRelease' | 'Fee'
  status: 'Pending' | 'Completed' | 'Failed' | 'Cancelled'
  amount: number
  currency: string
  description?: string
  createdAt: string
  completedAt?: string
}

export interface PaymentMethod {
  id: string
  type: string
  last4?: string
  brand?: string
  expMonth?: number
  expYear?: number
  isDefault: boolean
}

export interface PaymentIntentResponse {
  clientSecret: string
  paymentIntentId: string
  transactionId: string
}

export const paymentsApi = {
  // Wallet
  getWallet: () => api<Wallet>('/api/v1/wallets/my'),

  getWalletEntries: (walletId: string, page = 1, pageSize = 20) =>
    api<{ entries: WalletEntry[], totalCount: number, page: number, pageSize: number }>(
      `/api/v1/wallets/${walletId}/entries?page=${page}&pageSize=${pageSize}`
    ),

  // Transactions
  getTransactions: (params?: { type?: string, status?: string, page?: number, pageSize?: number }) => {
    const query = new URLSearchParams()
    if (params?.type) query.append('type', params.type)
    if (params?.status) query.append('status', params.status)
    if (params?.page) query.append('page', params.page.toString())
    if (params?.pageSize) query.append('pageSize', params.pageSize.toString())
    const qs = query.toString()
    return api<{ transactions: Transaction[], totalCount: number, page: number, pageSize: number }>(
      `/api/v1/payments/transactions${qs ? '?' + qs : ''}`
    )
  },

  // Payment Intents (Stripe)
  createPaymentIntent: (data: { amount: number, currency: string, taskId?: string, description?: string }) =>
    api<PaymentIntentResponse>('/api/v1/payments/intents', { method: 'POST', body: JSON.stringify(data) }),

  // Payment Methods
  getPaymentMethods: () => api<PaymentMethod[]>('/api/v1/payments/methods'),

  addPaymentMethod: (data: {
    stripePaymentMethodId: string
    last4: string
    brand: string
    expMonth: number
    expYear: number
    setAsDefault?: boolean
  }) => api<PaymentMethod>('/api/v1/payments/methods', { method: 'POST', body: JSON.stringify(data) }),
}
