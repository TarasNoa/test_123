'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { 
  FileCode, 
  Terminal, 
  Search, 
  Database, 
  GitBranch,
  CheckCircle,
  XCircle,
  Clock
} from 'lucide-react';

export type ToolStatus = 'idle' | 'running' | 'success' | 'error';

export interface ToolExecution {
  id: string;
  name: string;
  description: string;
  status: ToolStatus;
  duration?: number;
  output?: string;
  error?: string;
  icon?: React.ReactNode;
}

interface ToolVisualizationProps {
  tools: ToolExecution[];
  onRetry?: (toolId: string) => void;
  onViewOutput?: (toolId: string) => void;
}

export function ToolVisualization({ tools, onRetry, onViewOutput }: ToolVisualizationProps) {
  const getToolIcon = (name: string) => {
    const iconMap: Record<string, React.ReactNode> = {
      'file_read': <FileCode className="w-4 h-4" />,
      'file_write': <FileCode className="w-4 h-4" />,
      'terminal': <Terminal className="w-4 h-4" />,
      'search': <Search className="w-4 h-4" />,
      'database': <Database className="w-4 h-4" />,
      'git': <GitBranch className="w-4 h-4" />
    };
    return iconMap[name] || <Terminal className="w-4 h-4" />;
  };

  const getStatusIcon = (status: ToolStatus) => {
    switch (status) {
      case 'running': return <Clock className="w-4 h-4 text-yellow-500 animate-spin" />;
      case 'success': return <CheckCircle className="w-4 h-4 text-green-500" />;
      case 'error': return <XCircle className="w-4 h-4 text-red-500" />;
      default: return <Clock className="w-4 h-4 text-gray-400" />;
    }
  };

  const getStatusColor = (status: ToolStatus) => {
    switch (status) {
      case 'running': return 'bg-yellow-100 text-yellow-700 border-yellow-200';
      case 'success': return 'bg-green-100 text-green-700 border-green-200';
      case 'error': return 'bg-red-100 text-red-700 border-red-200';
      default: return 'bg-gray-100 text-gray-700 border-gray-200';
    }
  };

  const formatDuration = (ms?: number) => {
    if (!ms) return '';
    if (ms < 1000) return `${ms}ms`;
    return `${(ms / 1000).toFixed(1)}s`;
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Terminal className="w-5 h-5" />
          Tool Executions
          <Badge variant="secondary">{tools.length}</Badge>
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-2">
          {tools.map((tool) => (
            <div
              key={tool.id}
              className="flex items-center gap-3 p-3 border rounded-lg hover:bg-muted/50 transition-colors"
            >
              <div className="p-2 rounded bg-muted">
                {tool.icon || getToolIcon(tool.name)}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1">
                  <span className="font-medium">{tool.name}</span>
                  <Badge variant="outline" className={getStatusColor(tool.status)}>
                    {getStatusIcon(tool.status)}
                    <span className="ml-1">{tool.status}</span>
                  </Badge>
                  {tool.duration && (
                    <span className="text-xs text-muted-foreground">
                      {formatDuration(tool.duration)}
                    </span>
                  )}
                </div>
                <p className="text-sm text-muted-foreground truncate">
                  {tool.description}
                </p>
                {tool.output && (
                  <details className="mt-1">
                    <summary className="text-xs text-muted-foreground cursor-pointer">
                      Output
                    </summary>
                    <pre className="mt-1 text-xs bg-black/5 p-2 rounded overflow-x-auto">
                      {tool.output}
                    </pre>
                  </details>
                )}
                {tool.error && (
                  <div className="mt-1 text-xs text-red-600 dark:text-red-400">
                    {tool.error}
                  </div>
                )}
              </div>
              <div className="flex gap-1">
                {tool.status === 'error' && onRetry && (
                  <Button size="sm" variant="ghost" onClick={() => onRetry(tool.id)}>
                    Retry
                  </Button>
                )}
                {tool.output && onViewOutput && (
                  <Button size="sm" variant="ghost" onClick={() => onViewOutput(tool.id)}>
                    View
                  </Button>
                )}
              </div>
            </div>
          ))}
        </div>

        {tools.length === 0 && (
          <div className="text-center py-8 text-muted-foreground">
            <Terminal className="w-12 h-12 mx-auto mb-4 opacity-50" />
            <p>No tools executed yet</p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default ToolVisualization;
