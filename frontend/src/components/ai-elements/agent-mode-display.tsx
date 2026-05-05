'use client';

import React from 'react';
import { Badge } from '@/components/ui/badge';
import { 
  Zap, 
  Brain, 
  Cpu, 
  Sparkles
} from 'lucide-react';

export type AgentMode = 'flash' | 'thinking' | 'pro' | 'ultra';

interface AgentModeDisplayProps {
  mode: AgentMode;
  showLabel?: boolean;
  size?: 'sm' | 'md' | 'lg';
}

interface ModeConfig {
  value: AgentMode;
  label: string;
  icon: React.ReactNode;
  color: string;
  bgColor: string;
}

const modeConfigs: ModeConfig[] = [
  {
    value: 'flash',
    label: 'Flash',
    icon: <Zap className="w-4 h-4" />,
    color: 'text-yellow-500',
    bgColor: 'bg-yellow-100 dark:bg-yellow-900/20'
  },
  {
    value: 'thinking',
    label: 'Thinking',
    icon: <Brain className="w-4 h-4" />,
    color: 'text-blue-500',
    bgColor: 'bg-blue-100 dark:bg-blue-900/20'
  },
  {
    value: 'pro',
    label: 'Pro',
    icon: <Cpu className="w-4 h-4" />,
    color: 'text-green-500',
    bgColor: 'bg-green-100 dark:bg-green-900/20'
  },
  {
    value: 'ultra',
    label: 'Ultra',
    icon: <Sparkles className="w-4 h-4" />,
    color: 'text-purple-500',
    bgColor: 'bg-purple-100 dark:bg-purple-900/20'
  }
];

export function AgentModeDisplay({ mode, showLabel = true, size = 'md' }: AgentModeDisplayProps) {
  const config = modeConfigs.find(m => m.value === mode) || modeConfigs[0];

  const sizeClasses = {
    sm: 'px-2 py-1 text-xs',
    md: 'px-3 py-1.5 text-sm',
    lg: 'px-4 py-2 text-base'
  };

  return (
    <Badge variant="outline" className={`${config.bgColor} ${config.color} ${sizeClasses[size]} flex items-center gap-2`}>
      {config.icon}
      {showLabel && <span>{config.label}</span>}
    </Badge>
  );
}

export default AgentModeDisplay;
