'use client';

import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogContent,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { 
  Search, 
  FileCode, 
  FileText, 
  MessageSquare,
  Command,
  X,
  ArrowRight
} from 'lucide-react';

export type SearchResultType = 'file' | 'message' | 'command' | 'setting';

export interface SearchResult {
  id: string;
  type: SearchResultType;
  title: string;
  description?: string;
  path?: string;
  shortcut?: string;
  icon?: React.ReactNode;
}

interface GlobalSearchDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  results: SearchResult[];
  onSearch?: (query: string) => void;
  onSelectResult?: (result: SearchResult) => void;
}

export function GlobalSearchDialog({
  open,
  onOpenChange,
  results,
  onSearch,
  onSelectResult
}: GlobalSearchDialogProps) {
  const [query, setQuery] = useState('');
  const [selectedIndex, setSelectedIndex] = useState(0);

  useEffect(() => {
    if (open) {
      setQuery('');
      setSelectedIndex(0);
    }
  }, [open]);

  useEffect(() => {
    onSearch?.(query);
  }, [query, onSearch]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!open) return;

      if (e.key === 'ArrowDown') {
        e.preventDefault();
        setSelectedIndex(prev => Math.min(prev + 1, results.length - 1));
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        setSelectedIndex(prev => Math.max(prev - 1, 0));
      } else if (e.key === 'Enter' && results.length > 0) {
        onSelectResult?.(results[selectedIndex]);
      } else if (e.key === 'Escape') {
        onOpenChange(false);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [open, results, selectedIndex, onSelectResult, onOpenChange]);

  const getResultIcon = (type: SearchResultType) => {
    switch (type) {
      case 'file': return <FileCode className="w-4 h-4" />;
      case 'message': return <MessageSquare className="w-4 h-4" />;
      case 'command': return <Command className="w-4 h-4" />;
      case 'setting': return <FileText className="w-4 h-4" />;
      default: return <Search className="w-4 h-4" />;
    }
  };

  const getResultColor = (type: SearchResultType) => {
    switch (type) {
      case 'file': return 'bg-blue-500';
      case 'message': return 'bg-green-500';
      case 'command': return 'bg-purple-500';
      case 'setting': return 'bg-gray-500';
      default: return 'bg-gray-400';
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <div className="space-y-4">
          {/* Search Input */}
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
            <Input
              placeholder="Search files, messages, commands..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              className="pl-9 h-12 text-lg"
              autoFocus
            />
            <kbd className="absolute right-3 top-1/2 -translate-y-1/2 px-2 py-1 bg-muted rounded text-xs">
              ESC
            </kbd>
          </div>

          {/* Results */}
          <div className="max-h-96 overflow-y-auto">
            {results.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                <Search className="w-12 h-12 mx-auto mb-4 opacity-50" />
                <p>No results found</p>
                <p className="text-sm mt-1">Try a different search term</p>
              </div>
            ) : (
              <div className="space-y-1">
                {results.map((result, index) => (
                  <div
                    key={result.id}
                    onClick={() => onSelectResult?.(result)}
                    className={`flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-colors ${
                      index === selectedIndex ? 'bg-muted' : 'hover:bg-muted/50'
                    }`}
                  >
                    <div className={`p-2 rounded ${getResultColor(result.type)} text-white`}>
                      {result.icon || getResultIcon(result.type)}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <h4 className="font-medium">{result.title}</h4>
                        {result.shortcut && (
                          <kbd className="px-1.5 py-0.5 bg-muted rounded text-xs">
                            {result.shortcut}
                          </kbd>
                        )}
                      </div>
                      {result.description && (
                        <p className="text-sm text-muted-foreground truncate">
                          {result.description}
                        </p>
                      )}
                      {result.path && (
                        <p className="text-xs text-muted-foreground truncate">
                          {result.path}
                        </p>
                      )}
                    </div>
                    {index === selectedIndex && (
                      <ArrowRight className="w-4 h-4 text-muted-foreground" />
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Footer */}
          <div className="flex items-center justify-between text-xs text-muted-foreground pt-2 border-t">
            <div className="flex gap-4">
              <span><kbd className="px-1 py-0.5 bg-muted rounded">↑↓</kbd> Navigate</span>
              <span><kbd className="px-1 py-0.5 bg-muted rounded">↵</kbd> Select</span>
              <span><kbd className="px-1 py-0.5 bg-muted rounded">ESC</kbd> Close</span>
            </div>
            <span>{results.length} results</span>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

export default GlobalSearchDialog;
