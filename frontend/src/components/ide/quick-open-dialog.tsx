'use client';

import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogContent,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { 
  FileCode, 
  Folder, 
  Search,
  X,
  ArrowRight
} from 'lucide-react';

export type QuickOpenItemType = 'file' | 'folder';

export interface QuickOpenItem {
  id: string;
  type: QuickOpenItemType;
  name: string;
  path: string;
  language?: string;
  icon?: React.ReactNode;
}

interface QuickOpenDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  items: QuickOpenItem[];
  onOpenItem?: (item: QuickOpenItem) => void;
}

export function QuickOpenDialog({
  open,
  onOpenChange,
  items,
  onOpenItem
}: QuickOpenDialogProps) {
  const [query, setQuery] = useState('');
  const [selectedIndex, setSelectedIndex] = useState(0);

  useEffect(() => {
    if (open) {
      setQuery('');
      setSelectedIndex(0);
    }
  }, [open]);

  const filteredItems = items.filter(item =>
    item.name.toLowerCase().includes(query.toLowerCase()) ||
    item.path.toLowerCase().includes(query.toLowerCase())
  );

  useEffect(() => {
    setSelectedIndex(0);
  }, [query]);

  const handleKeyDown = (e: KeyboardEvent) => {
    if (!open) return;

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSelectedIndex(prev => Math.min(prev + 1, filteredItems.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSelectedIndex(prev => Math.max(prev - 1, 0));
    } else if (e.key === 'Enter' && filteredItems.length > 0) {
      onOpenItem?.(filteredItems[selectedIndex]);
    } else if (e.key === 'Escape') {
      onOpenChange(false);
    }
  };

  useEffect(() => {
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [open, filteredItems, selectedIndex, onOpenItem, onOpenChange]);

  const getItemIcon = (type: QuickOpenItemType) => {
    switch (type) {
      case 'file': return <FileCode className="w-4 h-4" />;
      case 'folder': return <Folder className="w-4 h-4" />;
    }
  };

  const getItemColor = (type: QuickOpenItemType) => {
    switch (type) {
      case 'file': return 'bg-blue-500';
      case 'folder': return 'bg-yellow-500';
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
              placeholder="Quick open file..."
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
            {filteredItems.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                <Search className="w-12 h-12 mx-auto mb-4 opacity-50" />
                <p>No files found</p>
                <p className="text-sm mt-1">Try a different search term</p>
              </div>
            ) : (
              <div className="space-y-1">
                {filteredItems.map((item, index) => (
                  <div
                    key={item.id}
                    onClick={() => onOpenItem?.(item)}
                    className={`flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-colors ${
                      index === selectedIndex ? 'bg-muted' : 'hover:bg-muted/50'
                    }`}
                  >
                    <div className={`p-2 rounded ${getItemColor(item.type)} text-white`}>
                      {item.icon || getItemIcon(item.type)}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <h4 className="font-medium">{item.name}</h4>
                        {item.language && (
                          <Badge variant="outline" className="text-xs">
                            {item.language}
                          </Badge>
                        )}
                      </div>
                      <p className="text-xs text-muted-foreground truncate">
                        {item.path}
                      </p>
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
              <span><kbd className="px-1 py-0.5 bg-muted rounded">↵</kbd> Open</span>
              <span><kbd className="px-1 py-0.5 bg-muted rounded">ESC</kbd> Close</span>
            </div>
            <span>{filteredItems.length} files</span>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

export default QuickOpenDialog;
