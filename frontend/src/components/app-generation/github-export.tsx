'use client';

import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { 
  Github, 
  Upload, 
  Check, 
  AlertCircle, 
  Loader2,
  GitBranch,
  Lock,
  Globe
} from 'lucide-react';

export type Visibility = 'public' | 'private';

export interface GitHubExportConfig {
  owner: string;
  repo: string;
  branch?: string;
  visibility: Visibility;
  description?: string;
  autoInit?: boolean;
}

interface GitHubExportProps {
  config: GitHubExportConfig;
  onExport?: (config: GitHubExportConfig) => void;
  onConfigChange?: (config: GitHubExportConfig) => void;
  isExporting?: boolean;
  exportStatus?: 'idle' | 'success' | 'error';
  exportError?: string;
  exportUrl?: string;
}

export function GitHubExport({
  config,
  onExport,
  onConfigChange,
  isExporting = false,
  exportStatus = 'idle',
  exportError,
  exportUrl
}: GitHubExportProps) {
  const [localConfig, setLocalConfig] = useState<GitHubExportConfig>(config);

  const handleConfigChange = (updates: Partial<GitHubExportConfig>) => {
    const newConfig = { ...localConfig, ...updates };
    setLocalConfig(newConfig);
    onConfigChange?.(newConfig);
  };

  const handleExport = () => {
    if (localConfig.owner && localConfig.repo) {
      onExport?.(localConfig);
    }
  };

  const isValid = localConfig.owner.trim() && localConfig.repo.trim();

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Github className="w-5 h-5" />
          GitHub Export
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {/* Repository Info */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label htmlFor="owner">Owner</Label>
              <Input
                id="owner"
                placeholder="username or org"
                value={localConfig.owner}
                onChange={(e) => handleConfigChange({ owner: e.target.value })}
              />
            </div>
            <div>
              <Label htmlFor="repo">Repository</Label>
              <Input
                id="repo"
                placeholder="repository-name"
                value={localConfig.repo}
                onChange={(e) => handleConfigChange({ repo: e.target.value })}
              />
            </div>
          </div>

          {/* Branch */}
          <div>
            <Label htmlFor="branch">Branch (optional)</Label>
            <div className="relative">
              <GitBranch className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
              <Input
                id="branch"
                placeholder="main"
                className="pl-9"
                value={localConfig.branch || ''}
                onChange={(e) => handleConfigChange({ branch: e.target.value })}
              />
            </div>
          </div>

          {/* Description */}
          <div>
            <Label htmlFor="description">Description (optional)</Label>
            <Input
              id="description"
              placeholder="Brief description of the project"
              value={localConfig.description || ''}
              onChange={(e) => handleConfigChange({ description: e.target.value })}
            />
          </div>

          {/* Visibility */}
          <div>
            <Label>Visibility</Label>
            <div className="flex gap-2 mt-2">
              <Button
                type="button"
                variant={localConfig.visibility === 'public' ? 'default' : 'outline'}
                onClick={() => handleConfigChange({ visibility: 'public' })}
                className="flex-1"
              >
                <Globe className="w-4 h-4 mr-2" />
                Public
              </Button>
              <Button
                type="button"
                variant={localConfig.visibility === 'private' ? 'default' : 'outline'}
                onClick={() => handleConfigChange({ visibility: 'private' })}
                className="flex-1"
              >
                <Lock className="w-4 h-4 mr-2" />
                Private
              </Button>
            </div>
          </div>

          {/* Auto Initialize */}
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="autoInit"
              checked={localConfig.autoInit || false}
              onChange={(e) => handleConfigChange({ autoInit: e.target.checked })}
              className="w-4 h-4"
            />
            <Label htmlFor="autoInit" className="cursor-pointer">
              Initialize with README
            </Label>
          </div>

          {/* Export Button */}
          <Button
            onClick={handleExport}
            disabled={!isValid || isExporting}
            className="w-full"
          >
            {isExporting ? (
              <>
                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                Exporting...
              </>
            ) : (
              <>
                <Upload className="w-4 h-4 mr-2" />
                Export to GitHub
              </>
            )}
          </Button>

          {/* Status Messages */}
          {exportStatus === 'success' && exportUrl && (
            <div className="p-3 bg-green-50 border border-green-200 rounded-lg">
              <div className="flex items-center gap-2 text-green-700">
                <Check className="w-5 h-5" />
                <span className="font-medium">Export successful!</span>
              </div>
              <a
                href={exportUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="text-sm text-green-600 hover:underline mt-1 block"
              >
                {exportUrl}
              </a>
            </div>
          )}

          {exportStatus === 'error' && exportError && (
            <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
              <div className="flex items-center gap-2 text-red-700">
                <AlertCircle className="w-5 h-5" />
                <span className="font-medium">Export failed</span>
              </div>
              <p className="text-sm text-red-600 mt-1">{exportError}</p>
            </div>
          )}

          {/* Preview */}
          {isValid && (
            <div className="p-3 bg-muted rounded-lg">
              <p className="text-sm text-muted-foreground">
                Repository will be created at:
              </p>
              <p className="text-sm font-mono mt-1">
                github.com/{localConfig.owner}/{localConfig.repo}
              </p>
              <Badge variant="outline" className="mt-2">
                {localConfig.visibility}
              </Badge>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

export default GitHubExport;
