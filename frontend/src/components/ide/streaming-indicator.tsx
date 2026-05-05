'use client';

import React from 'react';
import { Badge } from '@/components/ui/badge';
import { 
  Loader2, 
  CheckCircle, 
  AlertCircle, 
  Clock,
  Zap
} from 'lucide-react';

export type StreamingStatus = 'idle' | 'connecting' | 'streaming' | 'completed' | 'error';

interface StreamingIndicatorProps {
  status: StreamingStatus;
  tokensPerSecond?: number;
  totalTokens?: number;
  error?: string;
  showDetails?: boolean;
}

export function StreamingIndicator({
  status,
  tokensPerSecond,
  totalTokens,
  error,
  showDetails = true
}: StreamingIndicatorProps) {
  const getStatusIcon = () => {
    switch (status) {
      case 'connecting':
        return <Loader2 className="w-4 h-4 animate-spin text-yellow-500" />;
      case 'streaming':
        return <Loader2 className="w-4 h-4 animate-spin text-blue-500" />;
      case 'completed':
        return <CheckCircle className="w-4 h-4 text-green-500" />;
      case 'error':
        return <AlertCircle className="w-4 h-4 text-red-500" />;
      default:
        return <Clock className="w-4 h-4 text-gray-400" />;
    }
  };

  const getStatusText = () => {
    switch (status) {
      case 'connecting': return 'Connecting...';
      case 'streaming': return 'Streaming...';
      case 'completed': return 'Completed';
      case 'error': return 'Error';
      default: return 'Idle';
    }
  };

  const getStatusColor = () => {
    switch (status) {
      case 'connecting': return 'bg-yellow-100 text-yellow-700 border-yellow-200';
      case 'streaming': return 'bg-blue-100 text-blue-700 border-blue-200';
      case 'completed': return 'bg-green-100 text-green-700 border-green-200';
      case 'error': return 'bg-red-100 text-red-700 border-red-200';
      default: return 'bg-gray-100 text-gray-700 border-gray-200';
    }
  };

  return (
    <div className="flex items-center gap-2">
      <Badge 
        variant="outline" 
        className={`flex items-center gap-2 ${getStatusColor()}`}
      >
        {getStatusIcon()}
        <span>{getStatusText()}</span>
      </Badge>
      
      {showDetails && (status === 'streaming' || status === 'completed') && (
        <>
          {tokensPerSecond !== undefined && tokensPerSecond > 0 && (
            <Badge variant="outline" className="flex items-center gap-1">
              <Zap className="w-3 h-3" />
              {tokensPerSecond.toFixed(1)} t/s
            </Badge>
          )}
          
          {totalTokens !== undefined && totalTokens > 0 && (
            <Badge variant="secondary">
              {totalTokens.toLocaleString()} tokens
            </Badge>
          )}
        </>
      )}
      
      {status === 'error' && error && (
        <Badge variant="destructive" className="max-w-xs truncate">
          {error}
        </Badge>
      )}
    </div>
  );
}

export default StreamingIndicator;
