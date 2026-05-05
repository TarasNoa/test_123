'use client';

import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { 
  Clock, 
  Save, 
  RotateCcw, 
  Trash2, 
  Eye,
  ChevronDown,
  ChevronRight
} from 'lucide-react';

export interface Checkpoint {
  id: string;
  name: string;
  description?: string;
  createdAt: Date;
  agentId?: string;
  sessionId?: string;
  metadata?: Record<string, any>;
}

interface CheckpointManagerProps {
  checkpoints: Checkpoint[];
  onCreateCheckpoint?: (name: string, description?: string) => void;
  onRestoreCheckpoint?: (checkpointId: string) => void;
  onDeleteCheckpoint?: (checkpointId: string) => void;
  onViewCheckpoint?: (checkpointId: string) => void;
  currentCheckpointId?: string;
}

export function CheckpointManager({ 
  checkpoints, 
  onCreateCheckpoint,
  onRestoreCheckpoint,
  onDeleteCheckpoint,
  onViewCheckpoint,
  currentCheckpointId
}: CheckpointManagerProps) {
  const [isCreating, setIsCreating] = useState(false);
  const [newCheckpointName, setNewCheckpointName] = useState('');
  const [newCheckpointDescription, setNewCheckpointDescription] = useState('');
  const [expandedCheckpoints, setExpandedCheckpoints] = useState<Set<string>>(new Set());

  const handleCreateCheckpoint = () => {
    if (newCheckpointName.trim()) {
      onCreateCheckpoint?.(newCheckpointName, newCheckpointDescription);
      setNewCheckpointName('');
      setNewCheckpointDescription('');
      setIsCreating(false);
    }
  };

  const toggleExpand = (checkpointId: string) => {
    setExpandedCheckpoints(prev => {
      const newSet = new Set(prev);
      if (newSet.has(checkpointId)) {
        newSet.delete(checkpointId);
      } else {
        newSet.add(checkpointId);
      }
      return newSet;
    });
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <Clock className="w-5 h-5" />
            Checkpoints
            <Badge variant="secondary">{checkpoints.length}</Badge>
          </CardTitle>
          <Button
            size="sm"
            onClick={() => setIsCreating(!isCreating)}
          >
            <Save className="w-4 h-4 mr-2" />
            {isCreating ? 'Cancel' : 'Create Checkpoint'}
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {isCreating && (
          <div className="mb-4 p-4 border rounded-lg space-y-3">
            <div>
              <Label htmlFor="checkpoint-name">Name</Label>
              <Input
                id="checkpoint-name"
                placeholder="Checkpoint name..."
                value={newCheckpointName}
                onChange={(e) => setNewCheckpointName(e.target.value)}
              />
            </div>
            <div>
              <Label htmlFor="checkpoint-description">Description (optional)</Label>
              <Input
                id="checkpoint-description"
                placeholder="Brief description..."
                value={newCheckpointDescription}
                onChange={(e) => setNewCheckpointDescription(e.target.value)}
              />
            </div>
            <div className="flex gap-2">
              <Button size="sm" onClick={handleCreateCheckpoint}>
                <Save className="w-4 h-4 mr-2" />
                Save
              </Button>
              <Button size="sm" variant="outline" onClick={() => setIsCreating(false)}>
                Cancel
              </Button>
            </div>
          </div>
        )}

        {checkpoints.length === 0 ? (
          <div className="text-center py-8 text-muted-foreground">
            <Clock className="w-12 h-12 mx-auto mb-4 opacity-50" />
            <p>No checkpoints yet</p>
            <p className="text-sm mt-1">Create a checkpoint to save your current state</p>
          </div>
        ) : (
          <div className="space-y-2">
            {checkpoints.map((checkpoint) => {
              const isExpanded = expandedCheckpoints.has(checkpoint.id);
              const isCurrent = checkpoint.id === currentCheckpointId;

              return (
                <div
                  key={checkpoint.id}
                  className={`p-4 rounded-lg border ${
                    isCurrent ? 'bg-blue-50 border-blue-200' : 'hover:bg-muted/50'
                  }`}
                >
                  <div className="flex items-start justify-between">
                    <div className="flex items-start gap-3 flex-1">
                      <div className="mt-1">
                        {isCurrent ? (
                          <div className="w-2 h-2 rounded-full bg-blue-500" />
                        ) : (
                          <Clock className="w-4 h-4 text-gray-400" />
                        )}
                      </div>
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <h4 className="font-medium">{checkpoint.name}</h4>
                          {isCurrent && (
                            <Badge variant="secondary" className="bg-blue-100 text-blue-700">
                              Current
                            </Badge>
                          )}
                        </div>
                        {checkpoint.description && (
                          <p className="text-sm text-muted-foreground mt-1">
                            {checkpoint.description}
                          </p>
                        )}
                        <p className="text-xs text-muted-foreground mt-2">
                          {new Date(checkpoint.createdAt).toLocaleString()}
                          {checkpoint.agentId && (
                            <> • Agent: {checkpoint.agentId}</>
                          )}
                        </p>
                      </div>
                    </div>

                    <div className="flex gap-1">
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => toggleExpand(checkpoint.id)}
                      >
                        {isExpanded ? (
                          <ChevronDown className="w-4 h-4" />
                        ) : (
                          <ChevronRight className="w-4 h-4" />
                        )}
                      </Button>
                      {onViewCheckpoint && (
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => onViewCheckpoint(checkpoint.id)}
                        >
                          <Eye className="w-4 h-4" />
                        </Button>
                      )}
                      {onRestoreCheckpoint && !isCurrent && (
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => onRestoreCheckpoint(checkpoint.id)}
                        >
                          <RotateCcw className="w-4 h-4" />
                        </Button>
                      )}
                      {onDeleteCheckpoint && (
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => onDeleteCheckpoint(checkpoint.id)}
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      )}
                    </div>
                  </div>

                  {isExpanded && checkpoint.metadata && (
                    <div className="mt-3 pt-3 border-t">
                      <p className="text-xs font-medium mb-2">Metadata:</p>
                      <div className="bg-muted p-2 rounded text-xs font-mono">
                        {JSON.stringify(checkpoint.metadata, null, 2)}
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default CheckpointManager;
