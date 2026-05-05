'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { 
  MoreVertical, 
  Play, 
  Pause, 
  Settings,
  Trash2,
  Clock,
  GitBranch,
  Globe
} from 'lucide-react';

export type AppStatus = 'running' | 'stopped' | 'error' | 'building';
export type AppVisibility = 'public' | 'private';

export interface App {
  id: string;
  name: string;
  description?: string;
  status: AppStatus;
  visibility: AppVisibility;
  repository?: string;
  branch?: string;
  lastDeployed?: Date;
  url?: string;
  createdAt: Date;
}

interface AppCardProps {
  app: App;
  onDeploy?: (appId: string) => void;
  onStop?: (appId: string) => void;
  onConfigure?: (appId: string) => void;
  onDelete?: (appId: string) => void;
  onOpen?: (url: string) => void;
}

export function AppCard({
  app,
  onDeploy,
  onStop,
  onConfigure,
  onDelete,
  onOpen
}: AppCardProps) {
  const getStatusIcon = () => {
    switch (app.status) {
      case 'running': return <Play className="w-4 h-4 text-green-500" />;
      case 'stopped': return <Pause className="w-4 h-4 text-gray-500" />;
      case 'error': return <div className="w-4 h-4 rounded-full bg-red-500" />;
      case 'building': return <Clock className="w-4 h-4 text-yellow-500 animate-spin" />;
    }
  };

  const getStatusColor = () => {
    switch (app.status) {
      case 'running': return 'bg-green-500';
      case 'stopped': return 'bg-gray-500';
      case 'error': return 'bg-red-500';
      case 'building': return 'bg-yellow-500';
    }
  };

  const getVisibilityIcon = () => {
    return app.visibility === 'public' ? <Globe className="w-4 h-4" /> : <GitBranch className="w-4 h-4" />;
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <div className="flex items-center gap-2 mb-1">
              <CardTitle className="text-lg">{app.name}</CardTitle>
              <Badge variant="outline" className={getStatusColor()}>
                {app.status}
              </Badge>
              <Badge variant="outline" className="flex items-center gap-1">
                {getVisibilityIcon()}
                {app.visibility}
              </Badge>
            </div>
            {app.description && (
              <p className="text-sm text-muted-foreground">{app.description}</p>
            )}
          </div>
          <Button size="sm" variant="ghost">
            <MoreVertical className="w-4 h-4" />
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-3">
          {/* Repository Info */}
          {app.repository && (
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <GitBranch className="w-4 h-4" />
              <span className="font-mono">{app.repository}</span>
              {app.branch && <span>• {app.branch}</span>}
            </div>
          )}

          {/* Last Deployed */}
          {app.lastDeployed && (
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <Clock className="w-4 h-4" />
              <span>Last deployed: {new Date(app.lastDeployed).toLocaleString()}</span>
            </div>
          )}

          {/* Actions */}
          <div className="flex gap-2 pt-2">
            {app.status === 'stopped' && onDeploy && (
              <Button size="sm" onClick={() => onDeploy(app.id)}>
                <Play className="w-4 h-4 mr-2" />
                Deploy
              </Button>
            )}
            {app.status === 'running' && onStop && (
              <Button size="sm" variant="outline" onClick={() => onStop(app.id)}>
                <Pause className="w-4 h-4 mr-2" />
                Stop
              </Button>
            )}
            {app.url && onOpen && (
              <Button size="sm" variant="outline" onClick={() => onOpen(app.url!)}>
                <Globe className="w-4 h-4 mr-2" />
                Open
              </Button>
            )}
            {onConfigure && (
              <Button size="sm" variant="ghost" onClick={() => onConfigure(app.id)}>
                <Settings className="w-4 h-4" />
              </Button>
            )}
            {onDelete && (
              <Button size="sm" variant="ghost" onClick={() => onDelete(app.id)}>
                <Trash2 className="w-4 h-4" />
              </Button>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

export default AppCard;
