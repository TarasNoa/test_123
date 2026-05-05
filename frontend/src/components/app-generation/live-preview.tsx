'use client';

import React, { useState, useEffect, useRef } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { 
  Monitor, 
  RefreshCw, 
  ExternalLink, 
  Maximize2,
  Minimize2,
  Smartphone,
  Tablet,
  Laptop,
  Settings,
  AlertCircle
} from 'lucide-react';

export type PreviewDevice = 'mobile' | 'tablet' | 'desktop';

export type PreviewStatus = 'idle' | 'building' | 'ready' | 'error';

export interface PreviewConfig {
  url?: string;
  containerId?: string;
  device: PreviewDevice;
  autoRefresh: boolean;
  refreshInterval: number;
}

interface LivePreviewProps {
  config: PreviewConfig;
  status: PreviewStatus;
  onRefresh?: () => void;
  onDeviceChange?: (device: PreviewDevice) => void;
  onToggleAutoRefresh?: (enabled: boolean) => void;
  onOpenInNewTab?: () => void;
  error?: string;
}

export function LivePreview({
  config,
  status,
  onRefresh,
  onDeviceChange,
  onToggleAutoRefresh,
  onOpenInNewTab,
  error
}: LivePreviewProps) {
  const [isFullscreen, setIsFullscreen] = useState(false);
  const iframeRef = useRef<HTMLIFrameElement>(null);

  useEffect(() => {
    if (config.autoRefresh && config.refreshInterval > 0) {
      const interval = setInterval(() => {
        if (status === 'ready') {
          onRefresh?.();
        }
      }, config.refreshInterval * 1000);

      return () => clearInterval(interval);
    }
  }, [config.autoRefresh, config.refreshInterval, status, onRefresh]);

  const getDeviceWidth = () => {
    switch (config.device) {
      case 'mobile': return '375px';
      case 'tablet': return '768px';
      case 'desktop': return '100%';
    }
  };

  const getStatusIcon = () => {
    switch (status) {
      case 'building':
        return <RefreshCw className="w-4 h-4 animate-spin text-blue-500" />;
      case 'ready':
        return <Monitor className="w-4 h-4 text-green-500" />;
      case 'error':
        return <AlertCircle className="w-4 h-4 text-red-500" />;
      default:
        return <Monitor className="w-4 h-4 text-gray-400" />;
    }
  };

  const getStatusColor = () => {
    switch (status) {
      case 'building': return 'bg-blue-500';
      case 'ready': return 'bg-green-500';
      case 'error': return 'bg-red-500';
      default: return 'bg-gray-400';
    }
  };

  return (
    <Card className={isFullscreen ? 'fixed inset-4 z-50' : ''}>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <Monitor className="w-5 h-5" />
            Live Preview
            <Badge variant="outline" className={getStatusColor()}>
              {status}
            </Badge>
          </CardTitle>
          <div className="flex gap-2">
            <Button
              size="sm"
              variant={config.device === 'mobile' ? 'default' : 'outline'}
              onClick={() => onDeviceChange?.('mobile')}
            >
              <Smartphone className="w-4 h-4" />
            </Button>
            <Button
              size="sm"
              variant={config.device === 'tablet' ? 'default' : 'outline'}
              onClick={() => onDeviceChange?.('tablet')}
            >
              <Tablet className="w-4 h-4" />
            </Button>
            <Button
              size="sm"
              variant={config.device === 'desktop' ? 'default' : 'outline'}
              onClick={() => onDeviceChange?.('desktop')}
            >
              <Laptop className="w-4 h-4" />
            </Button>
            <Button
              size="sm"
              variant="ghost"
              onClick={() => setIsFullscreen(!isFullscreen)}
            >
              {isFullscreen ? (
                <Minimize2 className="w-4 h-4" />
              ) : (
                <Maximize2 className="w-4 h-4" />
              )}
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {/* Controls */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Button
                size="sm"
                onClick={onRefresh}
                disabled={status === 'building'}
              >
                <RefreshCw className={`w-4 h-4 mr-2 ${status === 'building' ? 'animate-spin' : ''}`} />
                Refresh
              </Button>
              <Button
                size="sm"
                variant="outline"
                onClick={() => onToggleAutoRefresh?.(!config.autoRefresh)}
              >
                {config.autoRefresh ? 'Auto-refresh ON' : 'Auto-refresh OFF'}
              </Button>
              {config.autoRefresh && (
                <Badge variant="secondary">
                  {config.refreshInterval}s
                </Badge>
              )}
            </div>
            {config.url && onOpenInNewTab && (
              <Button
                size="sm"
                variant="outline"
                onClick={onOpenInNewTab}
              >
                <ExternalLink className="w-4 h-4 mr-2" />
                Open in Tab
              </Button>
            )}
          </div>

          {/* Preview Frame */}
          <div className="border rounded-lg overflow-hidden bg-white min-h-[500px] flex items-center justify-center">
            {status === 'building' && (
              <div className="text-center">
                <RefreshCw className="w-12 h-12 mx-auto mb-4 animate-spin text-blue-500" />
                <p className="text-muted-foreground">Building preview...</p>
              </div>
            )}

            {status === 'error' && (
              <div className="text-center">
                <AlertCircle className="w-12 h-12 mx-auto mb-4 text-red-500" />
                <p className="text-muted-foreground">{error || 'Failed to load preview'}</p>
              </div>
            )}

            {status === 'idle' && (
              <div className="text-center">
                <Monitor className="w-12 h-12 mx-auto mb-4 opacity-50" />
                <p className="text-muted-foreground">Preview not ready</p>
                <p className="text-sm text-muted-foreground mt-1">
                  Start a build to see live preview
                </p>
              </div>
            )}

            {status === 'ready' && (
              <div className="w-full" style={{ maxWidth: getDeviceWidth(), margin: '0 auto' }}>
                {config.url ? (
                  <iframe
                    ref={iframeRef}
                    src={config.url}
                    className="w-full h-[500px] border-0"
                    title="Live Preview"
                    sandbox="allow-same-origin allow-scripts allow-forms"
                  />
                ) : (
                  <div className="p-8 text-center">
                    <Settings className="w-12 h-12 mx-auto mb-4 opacity-50" />
                    <p className="text-muted-foreground">Configure preview URL</p>
                  </div>
                )}
              </div>
            )}
          </div>

          {/* Device Info */}
          <div className="flex items-center justify-between text-sm text-muted-foreground">
            <span>Device: {config.device}</span>
            {config.device !== 'desktop' && (
              <span>Width: {getDeviceWidth()}</span>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

export default LivePreview;
