'use client';

import React from 'react';
import { Badge } from '@/components/ui/badge';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { 
  Zap, 
  Brain, 
  Cpu, 
  Sparkles
} from 'lucide-react';

export type AgentMode = 'flash' | 'thinking' | 'pro' | 'ultra';

interface ModeHoverGuideProps {
  currentMode: AgentMode;
  children: React.ReactNode;
}

interface ModeConfig {
  value: AgentMode;
  label: string;
  description: string;
  icon: React.ReactNode;
  color: string;
  capabilities: string[];
  bestFor: string[];
  limitations: string[];
}

const modeConfigs: ModeConfig[] = [
  {
    value: 'flash',
    label: 'Flash',
    description: 'Quick responses for simple tasks',
    icon: <Zap className="w-4 h-4" />,
    color: 'bg-yellow-500',
    capabilities: ['Fast responses', 'Simple tasks', 'Low latency'],
    bestFor: ['Quick fixes', 'Simple questions', 'Small edits'],
    limitations: ['Limited reasoning', 'Not for complex problems']
  },
  {
    value: 'thinking',
    label: 'Thinking',
    description: 'Deep reasoning for complex problems',
    icon: <Brain className="w-4 h-4" />,
    color: 'bg-blue-500',
    capabilities: ['Chain of thought', 'Deep reasoning', 'Problem solving'],
    bestFor: ['Complex debugging', 'Architecture decisions', 'Code reviews'],
    limitations: ['Slower responses', 'Higher token usage']
  },
  {
    value: 'pro',
    label: 'Pro',
    description: 'Professional quality for production',
    icon: <Cpu className="w-4 h-4" />,
    color: 'bg-green-500',
    capabilities: ['High quality', 'Production ready', 'Best practices'],
    bestFor: ['Production code', 'Refactoring', 'Best practices'],
    limitations: ['Takes longer', 'More verbose']
  },
  {
    value: 'ultra',
    label: 'Ultra',
    description: 'Maximum capability for complex workflows',
    icon: <Sparkles className="w-4 h-4" />,
    color: 'bg-purple-500',
    capabilities: ['Full capability', 'Complex workflows', 'Multi-agent'],
    bestFor: ['Complex projects', 'Multi-file changes', 'System architecture'],
    limitations: ['Highest cost', 'Slowest responses']
  }
];

export function ModeHoverGuide({ currentMode, children }: ModeHoverGuideProps) {
  const config = modeConfigs.find(m => m.value === currentMode) || modeConfigs[0];

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          {children}
        </TooltipTrigger>
        <TooltipContent className="w-80 p-4">
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <div className={`p-2 rounded ${config.color} text-white`}>
                {config.icon}
              </div>
              <div>
                <h4 className="font-medium">{config.label} Mode</h4>
                <p className="text-xs text-muted-foreground">{config.description}</p>
              </div>
            </div>
            
            <div>
              <span className="text-sm font-medium">Best For:</span>
              <ul className="mt-1 space-y-1">
                {config.bestFor.map((item) => (
                  <li key={item} className="text-xs text-muted-foreground flex items-center gap-2">
                    <div className="w-1 h-1 rounded-full bg-green-500" />
                    {item}
                  </li>
                ))}
              </ul>
            </div>
            
            <div>
              <span className="text-sm font-medium">Limitations:</span>
              <ul className="mt-1 space-y-1">
                {config.limitations.map((item) => (
                  <li key={item} className="text-xs text-muted-foreground flex items-center gap-2">
                    <div className="w-1 h-1 rounded-full bg-red-500" />
                    {item}
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}

export default ModeHoverGuide;
