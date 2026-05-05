'use client';

import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { 
  Check, 
  X, 
  Plus, 
  Clock,
  AlertCircle,
  ChevronDown,
  ChevronRight
} from 'lucide-react';

export type TodoPriority = 'low' | 'medium' | 'high' | 'urgent';

export type TodoStatus = 'pending' | 'in_progress' | 'completed' | 'blocked';

export interface TodoItem {
  id: string;
  title: string;
  description?: string;
  status: TodoStatus;
  priority: TodoPriority;
  createdAt: Date;
  dueDate?: Date;
  assignee?: string;
  tags?: string[];
  subtasks?: TodoItem[];
}

interface TodoListProps {
  todos: TodoItem[];
  onAddTodo?: (title: string, description?: string) => void;
  onUpdateStatus?: (todoId: string, status: TodoStatus) => void;
  onDeleteTodo?: (todoId: string) => void;
  onAddSubtask?: (parentId: string, title: string) => void;
}

export function TodoList({
  todos,
  onAddTodo,
  onUpdateStatus,
  onDeleteTodo,
  onAddSubtask
}: TodoListProps) {
  const [isAdding, setIsAdding] = useState(false);
  const [newTodoTitle, setNewTodoTitle] = useState('');
  const [newTodoDescription, setNewTodoDescription] = useState('');
  const [expandedTodos, setExpandedTodos] = useState<Set<string>>(new Set());

  const handleAddTodo = () => {
    if (newTodoTitle.trim()) {
      onAddTodo?.(newTodoTitle, newTodoDescription);
      setNewTodoTitle('');
      setNewTodoDescription('');
      setIsAdding(false);
    }
  };

  const toggleExpand = (todoId: string) => {
    setExpandedTodos(prev => {
      const newSet = new Set(prev);
      if (newSet.has(todoId)) {
        newSet.delete(todoId);
      } else {
        newSet.add(todoId);
      }
      return newSet;
    });
  };

  const getStatusIcon = (status: TodoStatus) => {
    switch (status) {
      case 'completed': return <Check className="w-4 h-4 text-green-500" />;
      case 'in_progress': return <Clock className="w-4 h-4 text-blue-500" />;
      case 'blocked': return <AlertCircle className="w-4 h-4 text-red-500" />;
      default: return <X className="w-4 h-4 text-gray-400" />;
    }
  };

  const getPriorityColor = (priority: TodoPriority) => {
    switch (priority) {
      case 'urgent': return 'bg-red-500';
      case 'high': return 'bg-orange-500';
      case 'medium': return 'bg-yellow-500';
      default: return 'bg-gray-400';
    }
  };

  const renderTodo = (todo: TodoItem, depth: number = 0) => {
    const isExpanded = expandedTodos.has(todo.id);
    const hasSubtasks = todo.subtasks && todo.subtasks.length > 0;

    return (
      <div key={todo.id} className="relative">
        {depth > 0 && (
          <div className="absolute left-4 top-0 w-px h-full bg-gray-200" />
        )}
        
        <div
          className={`flex items-start gap-3 p-3 rounded-lg border hover:bg-muted/50 transition-colors ${depth > 0 ? 'ml-8' : ''}`}
        >
          <div className="mt-1 cursor-pointer" onClick={() => onUpdateStatus?.(todo.id, todo.status === 'completed' ? 'pending' : 'completed')}>
            {getStatusIcon(todo.status)}
          </div>
          <div className="flex-1">
            <div className="flex items-center gap-2 mb-1">
              <h4 className={`font-medium ${todo.status === 'completed' ? 'line-through text-muted-foreground' : ''}`}>
                {todo.title}
              </h4>
              <Badge variant="outline" className={getPriorityColor(todo.priority)}>
                {todo.priority}
              </Badge>
              <Badge variant={todo.status === 'completed' ? 'default' : 'secondary'}>
                {todo.status}
              </Badge>
            </div>
            
            {todo.description && (
              <p className="text-sm text-muted-foreground mb-2">
                {todo.description}
              </p>
            )}
            
            <div className="flex items-center gap-3 text-xs text-muted-foreground">
              {todo.dueDate && (
                <span>Due: {new Date(todo.dueDate).toLocaleDateString()}</span>
              )}
              {todo.assignee && (
                <span>Assignee: {todo.assignee}</span>
              )}
            </div>
            
            {todo.tags && todo.tags.length > 0 && (
              <div className="flex flex-wrap gap-1 mt-2">
                {todo.tags.map((tag) => (
                  <Badge key={tag} variant="secondary" className="text-xs">
                    {tag}
                  </Badge>
                ))}
              </div>
            )}
          </div>
          
          <div className="flex gap-1">
            {hasSubtasks && (
              <Button
                size="sm"
                variant="ghost"
                onClick={() => toggleExpand(todo.id)}
              >
                {isExpanded ? (
                  <ChevronDown className="w-4 h-4" />
                ) : (
                  <ChevronRight className="w-4 h-4" />
                )}
              </Button>
            )}
            {onAddSubtask && (
              <Button
                size="sm"
                variant="ghost"
                onClick={() => {
                  const title = prompt('Enter subtask title:');
                  if (title) onAddSubtask(todo.id, title);
                }}
              >
                <Plus className="w-4 h-4" />
              </Button>
            )}
            {onDeleteTodo && (
              <Button
                size="sm"
                variant="ghost"
                onClick={() => onDeleteTodo(todo.id)}
              >
                <X className="w-4 h-4" />
              </Button>
            )}
          </div>
        </div>
        
        {hasSubtasks && isExpanded && (
          <div className="mt-2 space-y-2">
            {todo.subtasks?.map(subtask => renderTodo(subtask, depth + 1))}
          </div>
        )}
      </div>
    );
  };

  const sortedTodos = [...todos].sort((a, b) => {
    const priorityOrder = { urgent: 0, high: 1, medium: 2, low: 3 };
    return priorityOrder[a.priority] - priorityOrder[b.priority];
  });

  const stats = {
    total: todos.length,
    completed: todos.filter(t => t.status === 'completed').length,
    inProgress: todos.filter(t => t.status === 'in_progress').length,
    blocked: todos.filter(t => t.status === 'blocked').length
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <Check className="w-5 h-5" />
            Tasks
            <Badge variant="secondary">{stats.total}</Badge>
          </CardTitle>
          <Button size="sm" onClick={() => setIsAdding(!isAdding)}>
            <Plus className="w-4 h-4 mr-2" />
            {isAdding ? 'Cancel' : 'Add Task'}
          </Button>
        </div>
        
        {/* Stats */}
        <div className="flex gap-2 mt-4">
          <Badge variant="outline" className="bg-green-50 text-green-700">
            {stats.completed} completed
          </Badge>
          <Badge variant="outline" className="bg-blue-50 text-blue-700">
            {stats.inProgress} in progress
          </Badge>
          {stats.blocked > 0 && (
            <Badge variant="outline" className="bg-red-50 text-red-700">
              {stats.blocked} blocked
            </Badge>
          )}
        </div>
      </CardHeader>
      <CardContent>
        {isAdding && (
          <div className="mb-4 p-4 border rounded-lg space-y-3">
            <Input
              placeholder="Task title..."
              value={newTodoTitle}
              onChange={(e) => setNewTodoTitle(e.target.value)}
            />
            <Input
              placeholder="Description (optional)..."
              value={newTodoDescription}
              onChange={(e) => setNewTodoDescription(e.target.value)}
            />
            <div className="flex gap-2">
              <Button size="sm" onClick={handleAddTodo}>
                <Check className="w-4 h-4 mr-2" />
                Add
              </Button>
              <Button size="sm" variant="outline" onClick={() => setIsAdding(false)}>
                Cancel
              </Button>
            </div>
          </div>
        )}
        
        <div className="space-y-2">
          {sortedTodos.map(todo => renderTodo(todo))}
        </div>
        
        {todos.length === 0 && (
          <div className="text-center py-8 text-muted-foreground">
            <Check className="w-12 h-12 mx-auto mb-4 opacity-50" />
            <p>No tasks yet</p>
            <p className="text-sm mt-1">Add tasks to track your progress</p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default TodoList;
