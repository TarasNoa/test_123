'use client';

import React, { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { 
  MessageSquare, 
  ChevronDown, 
  ChevronRight,
  Copy,
  Check
} from 'lucide-react';

export type MessageRole = 'user' | 'assistant' | 'system';

export interface Message {
  id: string;
  role: MessageRole;
  content: string;
  timestamp: Date;
  metadata?: Record<string, any>;
}

export interface MessageGroup {
  id: string;
  title: string;
  messages: Message[];
  status?: 'pending' | 'completed' | 'failed';
  agent?: string;
}

interface MessageGroupProps {
  group: MessageGroup;
  onCopy?: (content: string) => void;
  onExpand?: (groupId: string) => void;
  isExpanded?: boolean;
}

export function MessageGroup({
  group,
  onCopy,
  onExpand,
  isExpanded = false
}: MessageGroupProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = (content: string) => {
    onCopy?.(content);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const getRoleColor = (role: MessageRole) => {
    switch (role) {
      case 'user': return 'bg-blue-100 text-blue-700';
      case 'assistant': return 'bg-green-100 text-green-700';
      case 'system': return 'bg-gray-100 text-gray-700';
    }
  };

  const getStatusColor = (status?: string) => {
    switch (status) {
      case 'completed': return 'bg-green-500';
      case 'failed': return 'bg-red-500';
      case 'pending': return 'bg-yellow-500';
      default: return '';
    }
  };

  return (
    <Card>
      <CardContent className="p-4">
        {/* Header */}
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-2">
            <MessageSquare className="w-4 h-4" />
            <h4 className="font-medium">{group.title}</h4>
            {group.agent && (
              <Badge variant="outline" className="text-xs">
                {group.agent}
              </Badge>
            )}
            {group.status && (
              <Badge 
                variant="outline" 
                className={`text-xs ${getStatusColor(group.status)}`}
              >
                {group.status}
              </Badge>
            )}
          </div>
          
          <div className="flex items-center gap-2">
            <Button
              size="sm"
              variant="ghost"
              onClick={() => onExpand?.(group.id)}
            >
              {isExpanded ? (
                <ChevronDown className="w-4 h-4" />
              ) : (
                <ChevronRight className="w-4 h-4" />
              )}
            </Button>
          </div>
        </div>

        {/* Messages */}
        {isExpanded && (
          <div className="space-y-3">
            {group.messages.map((message) => (
              <div
                key={message.id}
                className={`p-3 rounded-lg ${
                  message.role === 'user' ? 'bg-blue-50' : 'bg-green-50'
                }`}
              >
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-2">
                    <Badge variant="outline" className={getRoleColor(message.role)}>
                      {message.role}
                    </Badge>
                    <span className="text-xs text-muted-foreground">
                      {new Date(message.timestamp).toLocaleTimeString()}
                    </span>
                  </div>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => handleCopy(message.content)}
                  >
                    {copied ? (
                      <Check className="w-4 h-4" />
                    ) : (
                      <Copy className="w-4 h-4" />
                    )}
                  </Button>
                </div>
                
                <p className="text-sm whitespace-pre-wrap">{message.content}</p>
                
                {message.metadata && Object.keys(message.metadata).length > 0 && (
                  <div className="mt-2 pt-2 border-t border-black/10">
                    <p className="text-xs text-muted-foreground mb-1">Metadata:</p>
                    <div className="text-xs font-mono bg-black/5 p-2 rounded">
                      {JSON.stringify(message.metadata, null, 2)}
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default MessageGroup;
