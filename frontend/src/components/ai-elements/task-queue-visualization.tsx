'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { 
  Loader2, 
  CheckCircle, 
  Clock, 
  AlertCircle,
  Play,
  Pause,
  X
} from 'lucide-react';

export type TaskStatus = 'pending' | 'running' | 'completed' | 'failed' | 'cancelled';

export type TaskPriority = 'low' | 'medium' | 'high' | 'urgent';

export interface QueuedTask {
  id: string;
  title: string;
  description?: string;
  status: TaskStatus;
  priority: TaskPriority;
  agent?: string;
  createdAt: Date;
  startedAt?: Date;
  completedAt?: Date;
  progress?: number;
  output?: string;
  errors?: string[];
}

interface TaskQueueVisualizationProps {
  tasks: QueuedTask[];
  onCancelTask?: (taskId: string) => void;
  onRetryTask?: (taskId: string) => void;
  onPauseTask?: (taskId: string) => void;
  onResumeTask?: (taskId: string) => void;
}

export function TaskQueueVisualization({ 
  tasks, 
  onCancelTask,
  onRetryTask,
  onPauseTask,
  onResumeTask
}: TaskQueueVisualizationProps) {
  const getStatusIcon = (status: TaskStatus) => {
    switch (status) {
      case 'running':
        return <Loader2 className="w-4 h-4 animate-spin text-blue-500" />;
      case 'completed':
        return <CheckCircle className="w-4 h-4 text-green-500" />;
      case 'failed':
        return <AlertCircle className="w-4 h-4 text-red-500" />;
      case 'cancelled':
        return <X className="w-4 h-4 text-gray-500" />;
      default:
        return <Clock className="w-4 h-4 text-gray-400" />;
    }
  };

  const getStatusColor = (status: TaskStatus) => {
    switch (status) {
      case 'running': return 'bg-blue-500';
      case 'completed': return 'bg-green-500';
      case 'failed': return 'bg-red-500';
      case 'cancelled': return 'bg-gray-500';
      default: return 'bg-gray-400';
    }
  };

  const getPriorityColor = (priority: TaskPriority) => {
    switch (priority) {
      case 'urgent': return 'bg-red-500';
      case 'high': return 'bg-orange-500';
      case 'medium': return 'bg-yellow-500';
      default: return 'bg-gray-400';
    }
  };

  const getQueueStats = () => {
    return {
      total: tasks.length,
      pending: tasks.filter(t => t.status === 'pending').length,
      running: tasks.filter(t => t.status === 'running').length,
      completed: tasks.filter(t => t.status === 'completed').length,
      failed: tasks.filter(t => t.status === 'failed').length
    };
  };

  const stats = getQueueStats();

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <Loader2 className="w-5 h-5" />
            Task Queue
            <Badge variant="secondary">{stats.total}</Badge>
          </CardTitle>
          <div className="flex gap-2">
            <Badge variant="outline">{stats.pending} pending</Badge>
            <Badge variant="outline" className="bg-blue-50 text-blue-700">
              {stats.running} running
            </Badge>
            <Badge variant="outline" className="bg-green-50 text-green-700">
              {stats.completed} completed
            </Badge>
            {stats.failed > 0 && (
              <Badge variant="outline" className="bg-red-50 text-red-700">
                {stats.failed} failed
              </Badge>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent>
        {tasks.length === 0 ? (
          <div className="text-center py-8 text-muted-foreground">
            <Clock className="w-12 h-12 mx-auto mb-4 opacity-50" />
            <p>No tasks in queue</p>
          </div>
        ) : (
          <div className="space-y-3">
            {tasks.map((task) => (
              <div
                key={task.id}
                className="p-4 rounded-lg border hover:bg-muted/50 transition-colors"
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-3 flex-1">
                    <div className="mt-1">
                      {getStatusIcon(task.status)}
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <h4 className="font-medium">{task.title}</h4>
                        <Badge 
                          variant="outline" 
                          className={getPriorityColor(task.priority)}
                        >
                          {task.priority}
                        </Badge>
                        <Badge 
                          variant={task.status === 'completed' ? 'default' : 'secondary'}
                          className={getStatusColor(task.status)}
                        >
                          {task.status}
                        </Badge>
                      </div>
                      
                      {task.description && (
                        <p className="text-sm text-muted-foreground mb-2">
                          {task.description}
                        </p>
                      )}

                      {task.agent && (
                        <p className="text-xs text-muted-foreground">
                          Agent: {task.agent}
                        </p>
                      )}

                      {task.progress !== undefined && task.status === 'running' && (
                        <div className="mt-2">
                          <div className="flex justify-between text-xs mb-1">
                            <span>Progress</span>
                            <span>{task.progress}%</span>
                          </div>
                          <div className="h-2 bg-muted rounded-full overflow-hidden">
                            <div 
                              className="h-full bg-blue-500 transition-all"
                              style={{ width: `${task.progress}%` }}
                            />
                          </div>
                        </div>
                      )}

                      {task.output && task.status === 'completed' && (
                        <div className="mt-2 p-2 bg-green-50 border border-green-200 rounded text-sm">
                          <p className="font-medium text-xs mb-1 text-green-700">Output:</p>
                          <p className="text-green-800">{task.output}</p>
                        </div>
                      )}

                      {task.errors && task.errors.length > 0 && task.status === 'failed' && (
                        <div className="mt-2 p-2 bg-red-50 border border-red-200 rounded text-sm">
                          <p className="font-medium text-xs mb-1 text-red-700">Errors:</p>
                          {task.errors.map((error, i) => (
                            <p key={i} className="text-red-600">{error}</p>
                          ))}
                        </div>
                      )}

                      <p className="text-xs text-muted-foreground mt-2">
                        Created: {new Date(task.createdAt).toLocaleString()}
                        {task.startedAt && (
                          <> • Started: {new Date(task.startedAt).toLocaleString()}</>
                        )}
                        {task.completedAt && (
                          <> • Completed: {new Date(task.completedAt).toLocaleString()}</>
                        )}
                      </p>
                    </div>
                  </div>

                  <div className="flex gap-1">
                    {task.status === 'running' && onPauseTask && (
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => onPauseTask(task.id)}
                      >
                        <Pause className="w-4 h-4" />
                      </Button>
                    )}
                    {task.status === 'pending' && onResumeTask && (
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => onResumeTask(task.id)}
                      >
                        <Play className="w-4 h-4" />
                      </Button>
                    )}
                    {task.status === 'failed' && onRetryTask && (
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => onRetryTask(task.id)}
                      >
                        <Loader2 className="w-4 h-4" />
                      </Button>
                    )}
                    {(task.status === 'pending' || task.status === 'running') && onCancelTask && (
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => onCancelTask(task.id)}
                      >
                        <X className="w-4 h-4" />
                      </Button>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default TaskQueueVisualization;
