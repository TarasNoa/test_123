import { api } from './api'

export enum AssetType { Crypto = 0, Stock = 1, Forex = 2, Commodity = 3 }
export enum OrderType { Market = 0, Limit = 1, StopLoss = 2, TakeProfit = 3 }
export enum OrderSide { Buy = 0, Sell = 1 }
export enum OrderStatus { Pending = 0, Open = 1, Filled = 2, PartiallyFilled = 3, Cancelled = 4, Rejected = 5, Expired = 6 }
export enum TimeInForce { GTC = 0, IOC = 1, FOK = 2 }

export interface AssetPriceDto {
  assetId: string
  symbol: string
  price: number
  bid: number | null
  ask: number | null
  volume24h: number | null
  change24h: number | null
  timestamp: string
}

export interface OrderDto {
  id: string
  assetId: string
  assetSymbol: string
  type: OrderType
  side: OrderSide
  status: OrderStatus
  quantity: number
  price: number | null
  stopPrice: number | null
  filledQuantity: number
  averageFillPrice: number | null
  createdAt: string
  executedAt: string | null
}

export interface PortfolioPositionDto {
  assetId: string
  assetSymbol: string
  quantity: number
  averageEntryPrice: number
  currentPrice: number | null
  marketValue: number | null
  unrealizedPnl: number | null
}

export interface PortfolioDto {
  id: string
  name: string
  isDefault: boolean
  createdAt: string
  positions: PortfolioPositionDto[]
}

export const tradingApi = {
  getPrice: (symbol: string) =>
    api<AssetPriceDto>(`/market/price/${symbol}`),

  getTopAssets: (limit = 20) =>
    api<AssetPriceDto[]>(`/market/top?limit=${limit}`),

  getMyOrders: (status?: OrderStatus, page = 1, pageSize = 20) => {
    const params = new URLSearchParams()
    if (status !== undefined) params.append('status', status.toString())
    params.append('page', page.toString())
    params.append('pageSize', pageSize.toString())
    return api<{ items: OrderDto[]; totalCount: number; page: number; pageSize: number }>(
      `/orders/my?${params.toString()}`
    )
  },

  createOrder: (request: {
    assetId: string
    type: OrderType
    side: OrderSide
    quantity: number
    price?: number
    stopPrice?: number
    timeInForce?: TimeInForce
    expiresAt?: string
  }) =>
    api<string>('/orders/create', {
      method: 'POST',
      body: JSON.stringify(request),
    }),

  cancelOrder: (orderId: string) =>
    api<void>(`/orders/${orderId}/cancel`, { method: 'POST' }),

  getMyPortfolio: () =>
    api<PortfolioDto>('/portfolio/my'),
}

export function formatPrice(price: number | null | undefined): string {
  if (price === null || price === undefined) return '—'
  return price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 8 })
}

export function formatChange(change: number | null | undefined): string {
  if (change === null || change === undefined) return '—'
  const sign = change >= 0 ? '+' : ''
  return `${sign}${change.toFixed(2)}%`
}

export function getOrderStatusText(status: OrderStatus): string {
  const map: Record<OrderStatus, string> = {
    [OrderStatus.Pending]: 'Ожидание', [OrderStatus.Open]: 'Открыт', [OrderStatus.Filled]: 'Исполнен',
    [OrderStatus.PartiallyFilled]: 'Частично', [OrderStatus.Cancelled]: 'Отменён',
    [OrderStatus.Rejected]: 'Отклонён', [OrderStatus.Expired]: 'Истёк',
  }
  return map[status] || 'Unknown'
}

export function getOrderSideText(side: OrderSide): string {
  return side === OrderSide.Buy ? 'Покупка' : 'Продажа'
}

export function getOrderTypeText(type: OrderType): string {
  const map: Record<OrderType, string> = {
    [OrderType.Market]: 'Рыночный', [OrderType.Limit]: 'Лимитный',
    [OrderType.StopLoss]: 'Стоп-лосс', [OrderType.TakeProfit]: 'Тейк-профит',
  }
  return map[type] || 'Unknown'
}
