"use client";

import { useState, useEffect } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useAuth } from "@/lib/auth";
import {
  tradingApi,
  AssetPriceDto,
  PortfolioDto,
  OrderDto,
  OrderType,
  OrderSide,
  OrderStatus,
  TimeInForce,
  formatPrice,
  formatChange,
  getOrderStatusText,
  getOrderSideText,
  getOrderTypeText,
} from "@/lib/trading-api";
import { TrendingUp, TrendingDown, Wallet, List, Activity } from "lucide-react";

export default function TradingPage() {
  const { user } = useAuth();
  const [loading, setLoading] = useState(true);
  const [topAssets, setTopAssets] = useState<AssetPriceDto[]>([]);
  const [portfolio, setPortfolio] = useState<PortfolioDto | null>(null);
  const [orders, setOrders] = useState<OrderDto[]>([]);

  const [symbol, setSymbol] = useState("BTC");
  const [orderType, setOrderType] = useState<OrderType>(OrderType.Market);
  const [side, setSide] = useState<OrderSide>(OrderSide.Buy);
  const [quantity, setQuantity] = useState("");
  const [price, setPrice] = useState("");
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    loadData();
  }, []);

  async function loadData() {
    try {
      setLoading(true);
      const [assets, port, myOrders] = await Promise.all([
        tradingApi.getTopAssets(20),
        tradingApi.getMyPortfolio(),
        tradingApi.getMyOrders(undefined, 1, 10),
      ]);
      setTopAssets(assets);
      setPortfolio(port);
      setOrders(myOrders.items);
    } catch (error) {
      console.error("Failed to load trading data:", error);
    } finally {
      setLoading(false);
    }
  }

  async function handleSubmitOrder(e: React.FormEvent) {
    e.preventDefault();
    if (!quantity || isNaN(parseFloat(quantity))) return;

    setSubmitting(true);
    try {
      // Find asset by symbol from top assets
      const asset = topAssets.find(
        (a) => a.symbol.toUpperCase() === symbol.toUpperCase()
      );
      if (!asset) {
        alert(`Asset ${symbol} not found`);
        return;
      }

      await tradingApi.createOrder({
        assetId: asset.assetId,
        type: orderType,
        side: side,
        quantity: parseFloat(quantity),
        price: price ? parseFloat(price) : undefined,
        timeInForce: TimeInForce.GTC,
      });

      // Reload data
      await loadData();
      setQuantity("");
      setPrice("");
    } catch (error) {
      console.error("Failed to create order:", error);
      alert("Failed to create order");
    } finally {
      setSubmitting(false);
    }
  }

  async function handleCancelOrder(orderId: string) {
    try {
      await tradingApi.cancelOrder(orderId);
      await loadData();
    } catch (error) {
      console.error("Failed to cancel order:", error);
    }
  }

  const totalPortfolioValue =
    portfolio?.positions.reduce(
      (sum, pos) => sum + (pos.marketValue || 0),
      0
    ) || 0;

  const totalUnrealizedPnl =
    portfolio?.positions.reduce(
      (sum, pos) => sum + (pos.unrealizedPnl || 0),
      0
    ) || 0;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Трейдинг</h1>
        <p className="text-muted-foreground">Paper trading, портфель, ордера</p>
      </div>

      <Tabs defaultValue="market" className="space-y-6">
        <TabsList>
          <TabsTrigger value="market">
            <Activity className="w-4 h-4 mr-2" />
            Market
          </TabsTrigger>
          <TabsTrigger value="portfolio">
            <Wallet className="w-4 h-4 mr-2" />
            Portfolio
          </TabsTrigger>
          <TabsTrigger value="orders">
            <List className="w-4 h-4 mr-2" />
            Orders
          </TabsTrigger>
        </TabsList>

        <TabsContent value="market" className="space-y-6">
          <div className="grid md:grid-cols-3 gap-6">
            {/* Order Form */}
            <Card className="md:col-span-1">
              <CardHeader>
                <CardTitle>New Order</CardTitle>
              </CardHeader>
              <CardContent>
                <form onSubmit={handleSubmitOrder} className="space-y-4">
                  <div>
                    <label className="text-sm font-medium">Symbol</label>
                    <Input
                      value={symbol}
                      onChange={(e) =>
                        setSymbol(e.target.value.toUpperCase())
                      }
                      placeholder="BTC"
                    />
                  </div>

                  <div className="flex gap-2">
                    <Button
                      type="button"
                      variant={side === OrderSide.Buy ? "default" : "outline"}
                      className="flex-1"
                      onClick={() => setSide(OrderSide.Buy)}
                    >
                      Buy
                    </Button>
                    <Button
                      type="button"
                      variant={side === OrderSide.Sell ? "default" : "outline"}
                      className="flex-1"
                      onClick={() => setSide(OrderSide.Sell)}
                    >
                      Sell
                    </Button>
                  </div>

                  <div>
                    <label className="text-sm font-medium">Order Type</label>
                    <select
                      value={orderType}
                      onChange={(e) =>
                        setOrderType(Number(e.target.value) as OrderType)
                      }
                      className="w-full p-2 border rounded"
                    >
                      <option value={OrderType.Market}>Market</option>
                      <option value={OrderType.Limit}>Limit</option>
                      <option value={OrderType.StopLoss}>Stop Loss</option>
                    </select>
                  </div>

                  <div>
                    <label className="text-sm font-medium">Quantity</label>
                    <Input
                      type="number"
                      step="0.00000001"
                      value={quantity}
                      onChange={(e) => setQuantity(e.target.value)}
                      placeholder="0.00"
                      required
                    />
                  </div>

                  {orderType !== OrderType.Market && (
                    <div>
                      <label className="text-sm font-medium">
                        {orderType === OrderType.StopLoss
                          ? "Stop Price"
                          : "Limit Price"}
                      </label>
                      <Input
                        type="number"
                        step="0.01"
                        value={price}
                        onChange={(e) => setPrice(e.target.value)}
                        placeholder="0.00"
                      />
                    </div>
                  )}

                  <Button
                    type="submit"
                    className="w-full"
                    disabled={submitting}
                  >
                    {submitting
                      ? "Submitting..."
                      : `${side === OrderSide.Buy ? "Buy" : "Sell"} ${symbol}`}
                  </Button>
                </form>
              </CardContent>
            </Card>

            {/* Market Overview */}
            <Card className="md:col-span-2">
              <CardHeader>
                <CardTitle>Top Cryptocurrencies</CardTitle>
              </CardHeader>
              <CardContent>
                {loading ? (
                  <div className="text-center py-8">Loading...</div>
                ) : (
                  <div className="space-y-2">
                    {topAssets.slice(0, 10).map((asset) => (
                      <div
                        key={asset.symbol}
                        className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-lg cursor-pointer"
                        onClick={() => setSymbol(asset.symbol)}
                      >
                        <div className="flex items-center gap-3">
                          <div className="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center font-bold text-blue-600">
                            {asset.symbol[0]}
                          </div>
                          <div>
                            <div className="font-medium">{asset.symbol}</div>
                            <div className="text-sm text-gray-500">
                              Vol: {formatPrice(asset.volume24h)}
                            </div>
                          </div>
                        </div>
                        <div className="text-right">
                          <div className="font-medium">
                            ${formatPrice(asset.price)}
                          </div>
                          <div
                            className={`text-sm ${
                              (asset.change24h || 0) >= 0
                                ? "text-green-600"
                                : "text-red-600"
                            }`}
                          >
                            {(asset.change24h || 0) >= 0 ? (
                              <TrendingUp className="w-3 h-3 inline mr-1" />
                            ) : (
                              <TrendingDown className="w-3 h-3 inline mr-1" />
                            )}
                            {formatChange(asset.change24h)}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="portfolio">
          <div className="grid md:grid-cols-4 gap-6">
            <Card>
              <CardHeader>
                <CardTitle className="text-sm">Total Value</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  ${formatPrice(totalPortfolioValue)}
                </div>
              </CardContent>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle className="text-sm">Unrealized PnL</CardTitle>
              </CardHeader>
              <CardContent>
                <div
                  className={`text-2xl font-bold ${
                    totalUnrealizedPnl >= 0 ? "text-green-600" : "text-red-600"
                  }`}
                >
                  {totalUnrealizedPnl >= 0 ? "+" : ""}
                  ${formatPrice(Math.abs(totalUnrealizedPnl))}
                </div>
              </CardContent>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle className="text-sm">Positions</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {portfolio?.positions.length || 0}
                </div>
              </CardContent>
            </Card>
          </div>

          <Card className="mt-6">
            <CardHeader>
              <CardTitle>Positions</CardTitle>
            </CardHeader>
            <CardContent>
              {portfolio?.positions.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  No positions yet. Start trading!
                </div>
              ) : (
                <div className="space-y-2">
                  {portfolio?.positions.map((pos) => (
                    <div
                      key={pos.assetId}
                      className="flex items-center justify-between p-3 border rounded-lg"
                    >
                      <div>
                        <div className="font-medium">{pos.assetSymbol}</div>
                        <div className="text-sm text-gray-500">
                          {pos.quantity.toFixed(8)} @ $
                          {formatPrice(pos.averageEntryPrice)}
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="font-medium">
                          ${formatPrice(pos.marketValue)}
                        </div>
                        {pos.unrealizedPnl !== null && (
                          <div
                            className={`text-sm ${
                              pos.unrealizedPnl >= 0
                                ? "text-green-600"
                                : "text-red-600"
                            }`}
                          >
                            {pos.unrealizedPnl >= 0 ? "+" : ""}
                            {formatPrice(pos.unrealizedPnl)}
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="orders">
          <Card>
            <CardHeader>
              <CardTitle>Recent Orders</CardTitle>
            </CardHeader>
            <CardContent>
              {orders.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  No orders yet
                </div>
              ) : (
                <div className="space-y-2">
                  {orders.map((order) => (
                    <div
                      key={order.id}
                      className="flex items-center justify-between p-3 border rounded-lg"
                    >
                      <div>
                        <div className="flex items-center gap-2">
                          <Badge
                            variant={
                              order.side === OrderSide.Buy
                                ? "default"
                                : "secondary"
                            }
                          >
                            {getOrderSideText(order.side)}
                          </Badge>
                          <span className="font-medium">
                            {order.assetSymbol}
                          </span>
                          <Badge variant="outline">
                            {getOrderTypeText(order.type)}
                          </Badge>
                        </div>
                        <div className="text-sm text-gray-500 mt-1">
                          {order.filledQuantity.toFixed(8)} /{" "}
                          {order.quantity.toFixed(8)} filled @{" "}
                          {formatPrice(order.averageFillPrice)}
                        </div>
                      </div>
                      <div className="text-right flex items-center gap-3">
                        <Badge
                          variant={
                            order.status === OrderStatus.Filled
                              ? "default"
                              : order.status === OrderStatus.Open
                              ? "secondary"
                              : "destructive"
                          }
                        >
                          {getOrderStatusText(order.status)}
                        </Badge>
                        {(order.status === OrderStatus.Open ||
                          order.status === OrderStatus.Pending) && (
                          <Button
                            size="sm"
                            variant="destructive"
                            onClick={() => handleCancelOrder(order.id)}
                          >
                            Cancel
                          </Button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
