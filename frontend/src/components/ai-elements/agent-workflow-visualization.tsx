'use client';

import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { 
  CheckCircle, 
  Circle, 
  Loader2, 
  AlertCircle,
  ChevronRight,
  ChevronDown,
  FileText,
  Code,
  MessageSquare,
  GitBranch
} from 'lucide-react';

export type WorkflowStepStatus = 'pending' | 'in_progress' | 'completed' | 'failed';

export type WorkflowStepType = 
  | 'triage'
  | 'spec_writing'
  | 'implementation'
  | 'code_review'
  | 'testing'
  | 'deployment';

export interface WorkflowStep {
  id: string;
  type: WorkflowStepType;
  status: WorkflowStepStatus;
  title: string;
  description?: string;
  startTime?: Date;
  endTime?: Date;
  output?: string;
  errors?: string[];
  agent?: string;
  subSteps?: WorkflowStep[];
}

interface WorkflowStepConfig {
  type: WorkflowStepType;
  label: string;
  icon: React.ReactNode;
  color: string;
}

const stepConfigs: WorkflowStepConfig[] = [
  {
    type: 'triage',
    label: 'Triage',
    icon: <MessageSquare className="w-4 h-4" />,
    color: 'bg-purple-500'
  },
  {
    type: 'spec_writing',
    label: 'Spec Writing',
    icon: <FileText className="w-4 h-4" />,
    color: 'bg-blue-500'
  },
  {
    type: 'implementation',
    label: 'Implementation',
    icon: <Code className="w-4 h-4" />,
    color: 'bg-green-500'
  },
  {
    type: 'code_review',
    label: 'Code Review',
    icon: <GitBranch className="w-4 h-4" />,
    color: 'bg-orange-500'
  },
  {
    type: 'testing',
    label: 'Testing',
    icon: <CheckCircle className="w-4 h-4" />,
    color: 'bg-cyan-500'
  },
  {
    type: 'deployment',
    label: 'Deployment',
    icon: <Circle className="w-4 h-4" />,
    color: 'bg-red-500'
  }
];

interface AgentWorkflowVisualizationProps {
  steps: WorkflowStep[];
  onStepClick?: (step: WorkflowStep) => void;
  showSubSteps?: boolean;
}

export function AgentWorkflowVisualization({ 
  steps, 
  onStepClick,
  showSubSteps = true 
}: AgentWorkflowVisualizationProps) {
  const [expandedSteps, setExpandedSteps] = useState<Set<string>>(new Set());

  const getStatusIcon = (status: WorkflowStepStatus) => {
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

  const getStatusColor = (status: WorkflowStepStatus) => {
    switch (status) {
      case 'completed': return 'bg-green-500';
      case 'in_progress': return 'bg-blue-500';
      case 'failed': return 'bg-red-500';
      default: return 'bg-gray-400';
    }
  };

  const toggleExpand = (stepId: string) => {
    setExpandedSteps(prev => {
      const newSet = new Set(prev);
      if (newSet.has(stepId)) {
        newSet.delete(stepId);
      } else {
        newSet.add(stepId);
      }
      return newSet;
    });
  };

  const getStepConfig = (type: WorkflowStepType) => {
    return stepConfigs.find(c => c.type === type) || stepConfigs[0];
  };

  const getOverallProgress = () => {
    const completed = steps.filter(s => s.status === 'completed').length;
    const inProgress = steps.filter(s => s.status === 'in_progress').length;
    return ((completed + inProgress * 0.5) / steps.length) * 100;
  };

  const renderStep = (step: WorkflowStep, depth: number = 0) => {
    const config = getStepConfig(step.type);
    const isExpanded = expandedSteps.has(step.id);
    const hasSubSteps = step.subSteps && step.subSteps.length > 0;

    return (
      <div key={step.id} className="relative">
        {/* Connector line */}
        {depth > 0 && (
          <div className="absolute left-6 top-0 w-px h-full bg-gray-200" />
        )}
        
        <div
          className={`flex items-start gap-3 p-4 rounded-lg border cursor-pointer transition-colors ${
            step.status === 'in_progress' ? 'bg-blue-50 border-blue-200' : 'hover:bg-muted/50'
          } ${depth > 0 ? 'ml-6' : ''}`}
          onClick={() => onStepClick?.(step)}
        >
          <div className="mt-1">
            {getStatusIcon(step.status)}
          </div>
          <div className="flex-1">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <div className={`p-1.5 rounded ${config.color} text-white`}>
                  {config.icon}
                </div>
                <h4 className="font-medium">{step.title}</h4>
                {step.agent && (
                  <Badge variant="outline" className="text-xs">
                    {step.agent}
                  </Badge>
                )}
              </div>
              <div className="flex items-center gap-2">
                <Badge 
                  variant={step.status === 'completed' ? 'default' : 'secondary'}
                  className={getStatusColor(step.status)}
                >
                  {step.status}
                </Badge>
                {hasSubSteps && (
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={(e) => {
                      e.stopPropagation();
                      toggleExpand(step.id);
                    }}
                  >
                    {isExpanded ? (
                      <ChevronDown className="w-4 h-4" />
                    ) : (
                      <ChevronRight className="w-4 h-4" />
                    )}
                  </Button>
                )}
              </div>
            </div>
            
            {step.description && (
              <p className="text-sm text-muted-foreground mt-1">
                {step.description}
              </p>
            )}

            {step.output && (
              <div className="mt-2 p-2 bg-muted rounded text-sm">
                <p className="font-medium text-xs mb-1">Output:</p>
                <p className="text-muted-foreground">{step.output}</p>
              </div>
            )}

            {step.errors && step.errors.length > 0 && (
              <div className="mt-2 p-2 bg-red-50 border border-red-200 rounded text-sm">
                <p className="font-medium text-xs mb-1 text-red-700">Errors:</p>
                {step.errors.map((error, i) => (
                  <p key={i} className="text-red-600">{error}</p>
                ))}
              </div>
            )}

            {step.startTime && (
              <p className="text-xs text-muted-foreground mt-2">
                Started: {new Date(step.startTime).toLocaleTimeString()}
                {step.endTime && (
                  <> • Duration: {Math.round((new Date(step.endTime).getTime() - new Date(step.startTime).getTime()) / 1000)}s</>
                )}
              </p>
            )}
          </div>
        </div>

        {showSubSteps && hasSubSteps && isExpanded && (
          <div className="mt-2 space-y-2">
            {step.subSteps?.map(subStep => renderStep(subStep, depth + 1))}
          </div>
        )}
      </div>
    );
  };

  const overallProgress = getOverallProgress();

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <GitBranch className="w-5 h-5" />
            Agent Workflow
            <Badge variant="secondary">{steps.length} steps</Badge>
          </CardTitle>
          <Badge variant="outline">{Math.round(overallProgress)}% Complete</Badge>
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-2">
          {steps.map(step => renderStep(step))}
        </div>
      </CardContent>
    </Card>
  );
}

export default AgentWorkflowVisualization;
