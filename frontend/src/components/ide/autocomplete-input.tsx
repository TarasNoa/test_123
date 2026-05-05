'use client';

import React, { useState, useRef, useEffect } from 'react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { 
  Command,
  File,
  HelpCircle,
  History,
  Slash,
  AtSign
} from 'lucide-react';

export type TriggerType = 'file' | 'help' | 'history' | 'mode' | 'slash' | 'mention';

export interface Trigger {
  type: TriggerType;
  prefix: string;
  icon: React.ReactNode;
  suggestions: string[];
}

interface AutocompleteInputProps {
  value: string;
  onChange: (value: string) => void;
  triggers?: Trigger[];
  onTrigger?: (type: TriggerType, query: string) => void;
  placeholder?: string;
  disabled?: boolean;
}

const defaultTriggers: Trigger[] = [
  {
    type: 'file',
    prefix: '@',
    icon: <File className="w-4 h-4" />,
    suggestions: ['src/components', 'src/pages', 'src/utils', 'public/assets']
  },
  {
    type: 'help',
    prefix: '?',
    icon: <HelpCircle className="w-4 h-4" />,
    suggestions: ['help', 'commands', 'shortcuts', 'docs']
  },
  {
    type: 'history',
    prefix: '!',
    icon: <History className="w-4 h-4" />,
    suggestions: ['last command', 'recent files', 'clipboard']
  },
  {
    type: 'mode',
    prefix: '/',
    icon: <Slash className="w-4 h-4" />,
    suggestions: ['flash', 'thinking', 'pro', 'ultra']
  },
  {
    type: 'slash',
    prefix: '\\',
    icon: <Command className="w-4 h-4" />,
    suggestions: ['deploy', 'build', 'test', 'lint']
  },
  {
    type: 'mention',
    prefix: '@',
    icon: <AtSign className="w-4 h-4" />,
    suggestions: ['@assistant', '@system', '@user']
  }
];

export function AutocompleteInput({
  value,
  onChange,
  triggers = defaultTriggers,
  onTrigger,
  placeholder = 'Type @ for files, ? for help...',
  disabled = false
}: AutocompleteInputProps) {
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [activeTrigger, setActiveTrigger] = useState<Trigger | null>(null);
  const [filteredSuggestions, setFilteredSuggestions] = useState<string[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const suggestionsRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (suggestionsRef.current && !suggestionsRef.current.contains(event.target as Node)) {
        setShowSuggestions(false);
        setActiveTrigger(null);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value;
    onChange(newValue);

    // Check for triggers
    const lastChar = newValue.slice(-1);
    const trigger = triggers.find(t => t.prefix === lastChar);

    if (trigger) {
      setActiveTrigger(trigger);
      setFilteredSuggestions(trigger.suggestions);
      setShowSuggestions(true);
      setSelectedIndex(0);
    } else if (activeTrigger) {
      // Filter suggestions based on query after trigger
      const triggerIndex = newValue.lastIndexOf(activeTrigger.prefix);
      const query = newValue.slice(triggerIndex + 1);
      const filtered = activeTrigger.suggestions.filter(s =>
        s.toLowerCase().includes(query.toLowerCase())
      );
      setFilteredSuggestions(filtered);
      setShowSuggestions(filtered.length > 0);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (!showSuggestions) return;

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSelectedIndex(prev => Math.min(prev + 1, filteredSuggestions.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSelectedIndex(prev => Math.max(prev - 1, 0));
    } else if (e.key === 'Enter' && filteredSuggestions.length > 0) {
      e.preventDefault();
      selectSuggestion(filteredSuggestions[selectedIndex]);
    } else if (e.key === 'Escape') {
      setShowSuggestions(false);
      setActiveTrigger(null);
    }
  };

  const selectSuggestion = (suggestion: string) => {
    if (!activeTrigger) return;

    const triggerIndex = value.lastIndexOf(activeTrigger.prefix);
    const newValue = value.slice(0, triggerIndex + 1) + suggestion;
    onChange(newValue);
    setShowSuggestions(false);
    setActiveTrigger(null);
    onTrigger?.(activeTrigger.type, suggestion);
  };

  return (
    <div className="relative">
      <Input
        ref={inputRef}
        value={value}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        placeholder={placeholder}
        disabled={disabled}
        className="pr-10"
      />

      {showSuggestions && activeTrigger && filteredSuggestions.length > 0 && (
        <div
          ref={suggestionsRef}
          className="absolute top-full left-0 right-0 mt-1 bg-background border rounded-lg shadow-lg z-50 max-h-60 overflow-y-auto"
        >
          <div className="p-2 border-b flex items-center gap-2">
            {activeTrigger.icon}
            <span className="text-sm font-medium capitalize">
              {activeTrigger.type}
            </span>
          </div>
          {filteredSuggestions.map((suggestion, index) => (
            <div
              key={suggestion}
              onClick={() => selectSuggestion(suggestion)}
              className={`px-3 py-2 cursor-pointer transition-colors ${
                index === selectedIndex ? 'bg-muted' : 'hover:bg-muted/50'
              }`}
            >
              <span className="text-sm">{suggestion}</span>
            </div>
          ))}
        </div>
      )}

      {/* Trigger hints */}
      {value === '' && (
        <div className="absolute right-3 top-1/2 -translate-y-1/2 flex gap-1">
          {triggers.slice(0, 3).map((trigger) => (
            <Badge key={trigger.type} variant="outline" className="text-xs">
              {trigger.prefix}
            </Badge>
          ))}
        </div>
      )}
    </div>
  );
}

export default AutocompleteInput;
