'use client';

import React, { useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { 
  GitBranch, 
  Loader2, 
  CheckCircle, 
  XCircle,
  Copy
} from 'lucide-react';

interface GitCloneModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onClone?: (repository: string, branch?: string) => Promise<void>;
}

export function GitCloneModal({ open, onOpenChange, onClone }: GitCloneModalProps) {
  const [repository, setRepository] = useState('');
  const [branch, setBranch] = useState('main');
  const [isCloning, setIsCloning] = useState(false);
  const [cloneStatus, setCloneStatus] = useState<'idle' | 'success' | 'error'>('idle');
  const [error, setError] = useState('');

  const handleClone = async () => {
    if (!repository.trim()) return;

    setIsCloning(true);
    setCloneStatus('idle');
    setError('');

    try {
      await onClone?.(repository, branch);
      setCloneStatus('success');
      setRepository('');
      setBranch('main');
      setTimeout(() => {
        onOpenChange(false);
        setCloneStatus('idle');
      }, 1500);
    } catch (err) {
      setCloneStatus('error');
      setError(err instanceof Error ? err.message : 'Failed to clone repository');
    } finally {
      setIsCloning(false);
    }
  };

  const handlePaste = async () => {
    try {
      const text = await navigator.clipboard.readText();
      setRepository(text);
    } catch (err) {
      console.error('Failed to read clipboard');
    }
  };

  const isValidUrl = (url: string) => {
    return url.startsWith('https://github.com/') || 
           url.startsWith('git@github.com:') ||
           url.startsWith('https://gitlab.com/') ||
           url.startsWith('git@gitlab.com:');
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <GitBranch className="w-5 h-5" />
            Clone Repository
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4">
          {/* Repository URL */}
          <div className="space-y-2">
            <Label htmlFor="repository">Repository URL</Label>
            <div className="flex gap-2">
              <Input
                id="repository"
                placeholder="https://github.com/username/repo"
                value={repository}
                onChange={(e) => setRepository(e.target.value)}
                disabled={isCloning}
              />
              <Button
                size="sm"
                variant="outline"
                onClick={handlePaste}
                disabled={isCloning}
              >
                <Copy className="w-4 h-4" />
              </Button>
            </div>
            {repository && !isValidUrl(repository) && (
              <p className="text-xs text-red-500">
                Please enter a valid Git repository URL
              </p>
            )}
          </div>

          {/* Branch */}
          <div className="space-y-2">
            <Label htmlFor="branch">Branch</Label>
            <Input
              id="branch"
              placeholder="main"
              value={branch}
              onChange={(e) => setBranch(e.target.value)}
              disabled={isCloning}
            />
          </div>

          {/* Status */}
          {cloneStatus === 'success' && (
            <div className="flex items-center gap-2 p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
              <CheckCircle className="w-5 h-5 text-green-500" />
              <span className="text-sm text-green-700 dark:text-green-400">
                Repository cloned successfully!
              </span>
            </div>
          )}

          {cloneStatus === 'error' && (
            <div className="flex items-start gap-2 p-3 bg-red-50 dark:bg-red-900/20 rounded-lg">
              <XCircle className="w-5 h-5 text-red-500 flex-shrink-0" />
              <div className="flex-1">
                <p className="text-sm text-red-700 dark:text-red-400">
                  Failed to clone repository
                </p>
                <p className="text-xs text-red-600 dark:text-red-500 mt-1">
                  {error}
                </p>
              </div>
            </div>
          )}

          {/* Actions */}
          <div className="flex gap-2 pt-2">
            <Button
              onClick={handleClone}
              disabled={!repository.trim() || !isValidUrl(repository) || isCloning}
              className="flex-1"
            >
              {isCloning ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Cloning...
                </>
              ) : (
                <>
                  <GitBranch className="w-4 h-4 mr-2" />
                  Clone Repository
                </>
              )}
            </Button>
            <Button
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isCloning}
            >
              Cancel
            </Button>
          </div>

          {/* Info */}
          <div className="pt-2 border-t">
            <p className="text-xs text-muted-foreground">
              Supported: GitHub, GitLab, and other Git repositories
            </p>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

export default GitCloneModal;
