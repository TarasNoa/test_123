'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { 
  FileText, 
  FolderOpen, 
  Check,
  X,
  Plus
} from 'lucide-react';

export type ContextItemType = 'file' | 'folder' | 'url';

export interface ContextItem {
  id: string;
  type: ContextItemType;
  name: string;
  path?: string;
  url?: string;
  size?: number;
}

interface ContextRequestWidgetProps {
  requestedItems: ContextItem[];
  onApprove?: (item: ContextItem) => void;
  onDeny?: (item: ContextItem) => void;
  onApproveAll?: () => void;
  onDenyAll?: () => void;
}

export function ContextRequestWidget({
  requestedItems,
  onApprove,
  onDeny,
  onApproveAll,
  onDenyAll
}: ContextRequestWidgetProps) {
  const getItemIcon = (type: ContextItemType) => {
    switch (type) {
      case 'file': return <FileText className="w-4 h-4" />;
      case 'folder': return <FolderOpen className="w-4 h-4" />;
      case 'url': return <FileText className="w-4 h-4" />;
    }
  };

  const getItemColor = (type: ContextItemType) => {
    switch (type) {
      case 'file': return 'bg-blue-500';
      case 'folder': return 'bg-yellow-500';
      case 'url': return 'bg-purple-500';
    }
  };

  const formatSize = (bytes?: number) => {
    if (!bytes) return '';
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <FileText className="w-5 h-5" />
            Context Request
            <Badge variant="secondary">{requestedItems.length}</Badge>
          </CardTitle>
          <div className="flex gap-2">
            {onApproveAll && (
              <Button size="sm" variant="outline" onClick={onApproveAll}>
                Approve All
              </Button>
            )}
            {onDenyAll && (
              <Button size="sm" variant="outline" onClick={onDenyAll}>
                Deny All
              </Button>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-3">
          <p className="text-sm text-muted-foreground">
            AI is requesting access to the following items to provide better context:
          </p>

          {requestedItems.map((item) => (
            <div
              key={item.id}
              className="flex items-center gap-3 p-3 border rounded-lg hover:bg-muted/50 transition-colors"
            >
              <div className={`p-2 rounded ${getItemColor(item.type)} text-white`}>
                {getItemIcon(item.type)}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <span className="font-medium truncate">{item.name}</span>
                  {item.size && (
                    <Badge variant="outline" className="text-xs">
                      {formatSize(item.size)}
                    </Badge>
                  )}
                </div>
                {item.path && (
                  <p className="text-xs text-muted-foreground truncate">
                    {item.path}
                  </p>
                )}
                {item.url && (
                  <p className="text-xs text-muted-foreground truncate">
                    {item.url}
                  </p>
                )}
              </div>
              <div className="flex gap-1">
                {onApprove && (
                  <Button size="sm" variant="ghost" onClick={() => onApprove(item)}>
                    <Check className="w-4 h-4 text-green-500" />
                  </Button>
                )}
                {onDeny && (
                  <Button size="sm" variant="ghost" onClick={() => onDeny(item)}>
                    <X className="w-4 h-4 text-red-500" />
                  </Button>
                )}
              </div>
            </div>
          ))}

          {requestedItems.length === 0 && (
            <div className="text-center py-8 text-muted-foreground">
              <FileText className="w-12 h-12 mx-auto mb-4 opacity-50" />
              <p>No context requests</p>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

export default ContextRequestWidget;
