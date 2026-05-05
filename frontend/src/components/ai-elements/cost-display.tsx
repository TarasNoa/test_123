'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { 
  DollarSign, 
  TrendingUp, 
  TrendingDown,
  Info,
  BarChart3
} from 'lucide-react';

export interface CostData {
  totalCost: number;
  tokenCost: number;
  apiCalls: number;
  tokensUsed: number;
  averageCostPerToken: number;
  periodStart: Date;
  periodEnd: Date;
}

interface CostDisplayProps {
  data: CostData;
  onViewDetails?: () => void;
  showTrend?: 'up' | 'down' | 'neutral';
  trendPercentage?: number;
}

export function CostDisplay({
  data,
  onViewDetails,
  showTrend = 'neutral',
  trendPercentage = 0
}: CostDisplayProps) {
  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(value);
  };

  const formatNumber = (value: number) => {
    return new Intl.NumberFormat('en-US').format(value);
  };

  const getTrendIcon = () => {
    switch (showTrend) {
      case 'up': return <TrendingUp className="w-4 h-4 text-red-500" />;
      case 'down': return <TrendingDown className="w-4 h-4 text-green-500" />;
      default: return <BarChart3 className="w-4 h-4 text-gray-500" />;
    }
  };

  const getTrendColor = () => {
    switch (showTrend) {
      case 'up': return 'text-red-500';
      case 'down': return 'text-green-500';
      default: return 'text-gray-500';
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <DollarSign className="w-5 h-5" />
            Cost Overview
          </CardTitle>
          <Button size="sm" variant="outline" onClick={onViewDetails}>
            <Info className="w-4 h-4 mr-2" />
            Details
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {/* Total Cost */}
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">Total Cost</p>
              <p className="text-3xl font-bold">{formatCurrency(data.totalCost)}</p>
            </div>
            {showTrend !== 'neutral' && (
              <div className={`flex items-center gap-1 ${getTrendColor()}`}>
                {getTrendIcon()}
                <span className="text-sm font-medium">
                  {Math.abs(trendPercentage).toFixed(1)}%
                </span>
              </div>
            )}
          </div>

          {/* Stats Grid */}
          <div className="grid grid-cols-2 gap-4">
            <div className="p-3 bg-muted rounded-lg">
              <p className="text-xs text-muted-foreground mb-1">Tokens Used</p>
              <p className="text-xl font-semibold">{formatNumber(data.tokensUsed)}</p>
            </div>
            <div className="p-3 bg-muted rounded-lg">
              <p className="text-xs text-muted-foreground mb-1">API Calls</p>
              <p className="text-xl font-semibold">{formatNumber(data.apiCalls)}</p>
            </div>
            <div className="p-3 bg-muted rounded-lg">
              <p className="text-xs text-muted-foreground mb-1">Token Cost</p>
              <p className="text-xl font-semibold">{formatCurrency(data.tokenCost)}</p>
            </div>
            <div className="p-3 bg-muted rounded-lg">
              <p className="text-xs text-muted-foreground mb-1">Avg Cost/Token</p>
              <p className="text-xl font-semibold">
                {formatCurrency(data.averageCostPerToken)}
              </p>
            </div>
          </div>

          {/* Period Info */}
          <div className="pt-2 border-t">
            <p className="text-xs text-muted-foreground">
              Period: {new Date(data.periodStart).toLocaleDateString()} - {new Date(data.periodEnd).toLocaleDateString()}
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

export default CostDisplay;
