'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { CheckCircle, Circle, Loader2, AlertCircle } from 'lucide-react';

export type PhaseStatus = 'pending' | 'in_progress' | 'completed' | 'failed';

export type DevelopmentPhase = 
  | 'planning'
  | 'foundation'
  | 'core'
  | 'styling'
  | 'integration'
  | 'optimization';

export interface PhaseResult {
  phase: DevelopmentPhase;
  status: PhaseStatus;
  startTime?: Date;
  endTime?: Date;
  output?: string;
  errors?: string[];
}

interface PhaseConfig {
  value: DevelopmentPhase;
  label: string;
  description: string;
  icon: React.ReactNode;
}

const phaseConfigs: PhaseConfig[] = [
  {
    value: 'planning',
    label: 'Planning',
    description: 'Analyze requirements and create development plan',
    icon: <CheckCircle className="w-4 h-4" />
  },
  {
    value: 'foundation',
    label: 'Foundation',
    description: 'Set up project structure and core dependencies',
    icon: <Circle className="w-4 h-4" />
  },
  {
    value: 'core',
    label: 'Core',
    description: 'Implement main functionality and features',
    icon: <Circle className="w-4 h-4" />
  },
  {
    value: 'styling',
    label: 'Styling',
    description: 'Apply UI design and styling',
    icon: <Circle className="w-4 h-4" />
  },
  {
    value: 'integration',
    label: 'Integration',
    description: 'Integrate components and test workflows',
    icon: <Circle className="w-4 h-4" />
  },
  {
    value: 'optimization',
    label: 'Optimization',
    description: 'Optimize performance and fix issues',
    icon: <Circle className="w-4 h-4" />
  }
];

interface PhaseVisualizationProps {
  phases: PhaseResult[];
  onPhaseClick?: (phase: DevelopmentPhase) => void;
}

export function PhaseVisualization({ phases, onPhaseClick }: PhaseVisualizationProps) {
  const getStatusIcon = (status: PhaseStatus) => {
    switch (status) {
      case 'completed':
        return <CheckCircle className="w-5 h-5 text-green-500" />;
      case 'in_progress':
        return <Loader2 className="w-5 h-5 text-blue-500 animate-spin" />;
      case 'failed':
        return <AlertCircle className="w-5 h-5 text-red-500" />;
      default:
        return <Circle className="w-5 h-5 text-gray-400" />;
    }
  };

  const getStatusColor = (status: PhaseStatus) => {
    switch (status) {
      case 'completed': return 'bg-green-500';
      case 'in_progress': return 'bg-blue-500';
      case 'failed': return 'bg-red-500';
      default: return 'bg-gray-400';
    }
  };

  const getProgress = () => {
    const completed = phases.filter(p => p.status === 'completed').length;
    const inProgress = phases.filter(p => p.status === 'in_progress').length;
    return ((completed + inProgress * 0.5) / phases.length) * 100;
  };

  const getCurrentPhaseIndex = () => {
    return phases.findIndex(p => p.status === 'in_progress');
  };

  const currentPhaseIndex = getCurrentPhaseIndex();
  const overallProgress = getProgress();

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>Development Phases</CardTitle>
          <Badge variant="outline">{Math.round(overallProgress)}% Complete</Badge>
        </div>
        <Progress value={overallProgress} className="mt-2" />
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {phaseConfigs.map((config, index) => {
            const phaseResult = phases.find(p => p.phase === config.value);
            const status = phaseResult?.status || 'pending';
            const isActive = index === currentPhaseIndex;

            return (
              <div
                key={config.value}
                className={`p-4 rounded-lg border cursor-pointer transition-colors ${
                  isActive ? 'bg-blue-50 border-blue-200' : 'hover:bg-muted/50'
                }`}
                onClick={() => onPhaseClick?.(config.value)}
              >
                <div className="flex items-start gap-3">
                  <div className="mt-1">
                    {getStatusIcon(status)}
                  </div>
                  <div className="flex-1">
                    <div className="flex items-center justify-between">
                      <h4 className="font-medium">{config.label}</h4>
                      <Badge 
                        variant={status === 'completed' ? 'default' : 'secondary'}
                        className={getStatusColor(status)}
                      >
                        {status}
                      </Badge>
                    </div>
                    <p className="text-sm text-muted-foreground mt-1">
                      {config.description}
                    </p>
                    {phaseResult?.output && (
                      <div className="mt-2 p-2 bg-muted rounded text-sm">
                        <p className="font-medium text-xs mb-1">Output:</p>
                        <p className="text-muted-foreground">{phaseResult.output}</p>
                      </div>
                    )}
                    {phaseResult?.errors && phaseResult.errors.length > 0 && (
                      <div className="mt-2 p-2 bg-red-50 border border-red-200 rounded text-sm">
                        <p className="font-medium text-xs mb-1 text-red-700">Errors:</p>
                        {phaseResult.errors.map((error, i) => (
                          <p key={i} className="text-red-600">{error}</p>
                        ))}
                      </div>
                    )}
                    {phaseResult?.startTime && (
                      <p className="text-xs text-muted-foreground mt-2">
                        Started: {new Date(phaseResult.startTime).toLocaleTimeString()}
                        {phaseResult.endTime && (
                          <> • Duration: {Math.round((new Date(phaseResult.endTime).getTime() - new Date(phaseResult.startTime).getTime()) / 1000)}s</>
                        )}
                      </p>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}

export default PhaseVisualization;
