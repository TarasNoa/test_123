'use client';

import React, { useRef, useEffect, useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { 
  MessageSquare, 
  User, 
  Bot,
  Copy,
  Check
} from 'lucide-react';

export type MessageRole = 'user' | 'assistant' | 'system';

export interface VirtualMessage {
  id: string;
  role: MessageRole;
  content: string;
  timestamp: Date;
  tokens?: number;
  metadata?: Record<string, any>;
}

interface VirtualizedMessageListProps {
  messages: VirtualMessage[];
  onLoadMore?: () => void;
  hasMore?: boolean;
  isLoading?: boolean;
  onCopy?: (content: string) => void;
  className?: string;
}

export function VirtualizedMessageList({
  messages,
  onLoadMore,
  hasMore = false,
  isLoading = false,
  onCopy,
  className = ''
}: VirtualizedMessageListProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const sentinelRef = useRef<HTMLDivElement>(null);
  const [copiedId, setCopiedId] = useState<string | null>(null);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !isLoading) {
          onLoadMore?.();
        }
      },
      { threshold: 0.1 }
    );

    if (sentinelRef.current) {
      observer.observe(sentinelRef.current);
    }

    return () => observer.disconnect();
  }, [hasMore, isLoading, onLoadMore]);

  const handleCopy = (id: string, content: string) => {
    onCopy?.(content);
    setCopiedId(id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const getRoleIcon = (role: MessageRole) => {
    switch (role) {
      case 'user': return <User className="w-4 h-4" />;
      case 'assistant': return <Bot className="w-4 h-4" />;
      case 'system': return <MessageSquare className="w-4 h-4" />;
    }
  };

  const getRoleColor = (role: MessageRole) => {
    switch (role) {
      case 'user': return 'bg-blue-100 text-blue-700 border-blue-200';
      case 'assistant': return 'bg-green-100 text-green-700 border-green-200';
      case 'system': return 'bg-gray-100 text-gray-700 border-gray-200';
    }
  };

  return (
    <div ref={containerRef} className={`space-y-2 ${className}`}>
      {messages.map((message) => (
        <Card key={message.id} className={`border ${getRoleColor(message.role)}`}>
          <CardContent className="p-4">
            <div className="flex items-start gap-3">
              <div className="mt-1">
                {getRoleIcon(message.role)}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-2">
                    <Badge variant="outline" className={getRoleColor(message.role)}>
                      {message.role}
                    </Badge>
                    <span className="text-xs text-muted-foreground">
                      {new Date(message.timestamp).toLocaleTimeString()}
                    </span>
                    {message.tokens !== undefined && (
                      <Badge variant="secondary" className="text-xs">
                        {message.tokens} tokens
                      </Badge>
                    )}
                  </div>
                  <button
                    onClick={() => handleCopy(message.id, message.content)}
                    className="text-muted-foreground hover:text-foreground transition-colors"
                  >
                    {copiedId === message.id ? (
                      <Check className="w-4 h-4" />
                    ) : (
                      <Copy className="w-4 h-4" />
                    )}
                  </button>
                </div>
                
                <p className="text-sm whitespace-pre-wrap break-words">
                  {message.content}
                </p>
                
                {message.metadata && Object.keys(message.metadata).length > 0 && (
                  <details className="mt-2">
                    <summary className="text-xs text-muted-foreground cursor-pointer">
                      Metadata
                    </summary>
                    <pre className="mt-1 text-xs bg-black/5 p-2 rounded overflow-x-auto">
                      {JSON.stringify(message.metadata, null, 2)}
                    </pre>
                  </details>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
      
      {hasMore && (
        <div ref={sentinelRef} className="py-4 text-center text-muted-foreground">
          {isLoading ? 'Loading more messages...' : 'Scroll to load more'}
        </div>
      )}
      
      {messages.length === 0 && (
        <Card>
          <CardContent className="p-8 text-center text-muted-foreground">
            <MessageSquare className="w-12 h-12 mx-auto mb-4 opacity-50" />
            <p>No messages yet</p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

export default VirtualizedMessageList;
