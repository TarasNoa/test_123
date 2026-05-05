'use client';

import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { 
  DropdownMenu, 
  DropdownMenuContent, 
  DropdownMenuItem, 
  DropdownMenuTrigger 
} from '@/components/ui/dropdown-menu';
import { Badge } from '@/components/ui/badge';
import { 
  Zap, 
  Brain, 
  Cpu, 
  Sparkles,
  ChevronDown 
} from 'lucide-react';

export type AgentMode = 'flash' | 'thinking' | 'pro' | 'ultra';

interface ModeSelectorProps {
  currentMode: AgentMode;
  onModeChange: (mode: AgentMode) => void;
  disabled?: boolean;
}

interface ModeConfig {
  value: AgentMode;
  label: string;
  description: string;
  icon: React.ReactNode;
  color: string;
  capabilities: string[];
}

const modeConfigs: ModeConfig[] = [
  {
    value: 'flash',
    label: 'Flash',
    description: 'Quick responses for simple tasks',
    icon: <Zap className="w-4 h-4" />,
    color: 'bg-yellow-500',
    capabilities: ['Fast responses', 'Simple tasks', 'Low latency']
  },
  {
    value: 'thinking',
    label: 'Thinking',
    description: 'Deep reasoning for complex problems',
    icon: <Brain className="w-4 h-4" />,
    color: 'bg-blue-500',
    capabilities: ['Chain of thought', 'Deep reasoning', 'Problem solving']
  },
  {
    value: 'pro',
    label: 'Pro',
    description: 'Professional quality for production',
    icon: <Cpu className="w-4 h-4" />,
    color: 'bg-green-500',
    capabilities: ['High quality', 'Production ready', 'Best practices']
  },
  {
    value: 'ultra',
    label: 'Ultra',
    description: 'Maximum capability for complex workflows',
    icon: <Sparkles className="w-4 h-4" />,
    color: 'bg-purple-500',
    capabilities: ['Full capability', 'Complex workflows', 'Multi-agent']
  }
];

export function ModeSelector({ 
  currentMode, 
  onModeChange, 
  disabled = false 
}: ModeSelectorProps) {
  const currentConfig = modeConfigs.find(m => m.value === currentMode) || modeConfigs[0];

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button 
          variant="outline" 
          disabled={disabled}
          className="gap-2"
        >
          <div className={`w-2 h-2 rounded-full ${currentConfig.color}`} />
          {currentConfig.icon}
          {currentConfig.label}
          <ChevronDown className="w-4 h-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-80">
        {modeConfigs.map((config) => (
          <DropdownMenuItem
            key={config.value}
            onClick={() => onModeChange(config.value)}
            className="flex-col items-start p-4"
          >
            <div className="flex items-center gap-2 w-full">
              <div className={`w-2 h-2 rounded-full ${config.color}`} />
              {config.icon}
              <span className="font-medium">{config.label}</span>
              {config.value === currentMode && (
                <Badge variant="secondary" className="ml-auto">Active</Badge>
              )}
            </div>
            <p className="text-sm text-muted-foreground mt-1">
              {config.description}
            </p>
            <div className="flex flex-wrap gap-1 mt-2">
              {config.capabilities.map((cap) => (
                <Badge key={cap} variant="outline" className="text-xs">
                  {cap}
                </Badge>
              ))}
            </div>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

export default ModeSelector;
