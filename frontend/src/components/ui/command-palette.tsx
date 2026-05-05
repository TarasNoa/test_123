'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandShortcut,
} from '@/components/ui/command';
import { 
  Search,
  FileCode,
  MessageSquare,
  Settings,
  Terminal,
  BookOpen,
  GitBranch,
  Zap,
  Plus,
  History
} from 'lucide-react';

export interface CommandPaletteItem {
  id: string;
  label: string;
  icon?: React.ReactNode;
  shortcut?: string;
  action: () => void;
  category?: string;
}

interface CommandPaletteProps {
  items: CommandPaletteItem[];
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  recentCommands?: CommandPaletteItem[];
}

export function CommandPalette({ 
  items, 
  open: controlledOpen, 
  onOpenChange,
  recentCommands = []
}: CommandPaletteProps) {
  const [open, setOpen] = useState(controlledOpen || false);
  const [searchQuery, setSearchQuery] = useState('');

  // Handle keyboard shortcut (Ctrl/Cmd + K)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        setOpen(true);
      }
      if (e.key === 'Escape') {
        setOpen(false);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  // Sync with controlled open prop
  useEffect(() => {
    if (controlledOpen !== undefined) {
      setOpen(controlledOpen);
    }
  }, [controlledOpen]);

  const handleOpenChange = (newOpen: boolean) => {
    setOpen(newOpen);
    onOpenChange?.(newOpen);
  };

  const executeCommand = useCallback((item: CommandPaletteItem) => {
    item.action();
    setOpen(false);
    setSearchQuery('');
  }, []);

  // Group items by category
  const groupedItems = React.useMemo(() => {
    const groups: Record<string, CommandPaletteItem[]> = {};
    
    items.forEach(item => {
      const category = item.category || 'General';
      if (!groups[category]) {
        groups[category] = [];
      }
      groups[category].push(item);
    });

    return groups;
  }, [items]);

  return (
    <CommandDialog open={open} onOpenChange={handleOpenChange}>
      <CommandInput 
        placeholder="Type a command or search..." 
        value={searchQuery}
        onValueChange={setSearchQuery}
      />
      <CommandList>
        <CommandEmpty>No results found.</CommandEmpty>

        {/* Recent Commands */}
        {recentCommands.length > 0 && (
          <CommandGroup heading="Recent">
            {recentCommands.slice(0, 5).map((item) => (
              <CommandItem
                key={item.id}
                onSelect={() => executeCommand(item)}
              >
                {item.icon || <History className="w-4 h-4" />}
                <span>{item.label}</span>
                {item.shortcut && (
                  <CommandShortcut>{item.shortcut}</CommandShortcut>
                )}
              </CommandItem>
            ))}
          </CommandGroup>
        )}

        {/* Grouped Commands */}
        {Object.entries(groupedItems).map(([category, categoryItems]) => (
          <CommandGroup key={category} heading={category}>
            {categoryItems
              .filter(item => 
                item.label.toLowerCase().includes(searchQuery.toLowerCase())
              )
              .map((item) => (
                <CommandItem
                  key={item.id}
                  onSelect={() => executeCommand(item)}
                >
                  {item.icon || <Search className="w-4 h-4" />}
                  <span>{item.label}</span>
                  {item.shortcut && (
                    <CommandShortcut>{item.shortcut}</CommandShortcut>
                  )}
                </CommandItem>
              ))}
          </CommandGroup>
        ))}
      </CommandList>
    </CommandDialog>
  );
}

// Default command items for common IDE actions
export const defaultCommandItems: CommandPaletteItem[] = [
  {
    id: 'new-chat',
    label: 'New Chat',
    icon: <MessageSquare className="w-4 h-4" />,
    shortcut: '⌘N',
    category: 'Chat',
    action: () => console.log('New chat')
  },
  {
    id: 'new-file',
    label: 'New File',
    icon: <FileCode className="w-4 h-4" />,
    shortcut: '⌘⇧N',
    category: 'File',
    action: () => console.log('New file')
  },
  {
    id: 'open-terminal',
    label: 'Open Terminal',
    icon: <Terminal className="w-4 h-4" />,
    shortcut: '⌘`',
    category: 'Terminal',
    action: () => console.log('Open terminal')
  },
  {
    id: 'open-settings',
    label: 'Open Settings',
    icon: <Settings className="w-4 h-4" />,
    shortcut: '⌘,',
    category: 'Settings',
    action: () => console.log('Open settings')
  },
  {
    id: 'toggle-mode-flash',
    label: 'Switch to Flash Mode',
    icon: <Zap className="w-4 h-4" />,
    category: 'Agent Mode',
    action: () => console.log('Switch to flash mode')
  },
  {
    id: 'toggle-mode-thinking',
    label: 'Switch to Thinking Mode',
    icon: <BookOpen className="w-4 h-4" />,
    category: 'Agent Mode',
    action: () => console.log('Switch to thinking mode')
  },
  {
    id: 'create-workflow',
    label: 'Create New Workflow',
    icon: <GitBranch className="w-4 h-4" />,
    category: 'Workflow',
    action: () => console.log('Create workflow')
  },
  {
    id: 'quick-action',
    label: 'Quick Action',
    icon: <Plus className="w-4 h-4" />,
    shortcut: '⌘⇧A',
    category: 'Actions',
    action: () => console.log('Quick action')
  }
];

export default CommandPalette;
