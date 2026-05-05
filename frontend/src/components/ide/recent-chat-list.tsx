'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { 
  MessageSquare, 
  Search, 
  Clock, 
  Star,
  Trash2,
  Plus
} from 'lucide-react';

export interface ChatSession {
  id: string;
  title: string;
  preview: string;
  createdAt: Date;
  lastModified: Date;
  messageCount: number;
  isFavorite?: boolean;
  agent?: string;
  tags?: string[];
}

interface RecentChatListProps {
  sessions: ChatSession[];
  onSelectSession?: (session: ChatSession) => void;
  onNewChat?: () => void;
  onDeleteSession?: (sessionId: string) => void;
  onToggleFavorite?: (sessionId: string) => void;
  searchQuery?: string;
  onSearchChange?: (query: string) => void;
}

export function RecentChatList({
  sessions,
  onSelectSession,
  onNewChat,
  onDeleteSession,
  onToggleFavorite,
  searchQuery = '',
  onSearchChange
}: RecentChatListProps) {
  const filteredSessions = sessions.filter(session =>
    session.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
    session.preview.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const sortedSessions = [...filteredSessions].sort((a, b) => 
    new Date(b.lastModified).getTime() - new Date(a.lastModified).getTime()
  );

  const getTimeAgo = (date: Date) => {
    const seconds = Math.floor((new Date().getTime() - date.getTime()) / 1000);
    if (seconds < 60) return 'just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    if (seconds < 604800) return `${Math.floor(seconds / 86400)}d ago`;
    return new Date(date).toLocaleDateString();
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <MessageSquare className="w-5 h-5" />
            Recent Chats
            <Badge variant="secondary">{sessions.length}</Badge>
          </CardTitle>
          <Button size="sm" onClick={onNewChat}>
            <Plus className="w-4 h-4 mr-2" />
            New Chat
          </Button>
        </div>
        
        {/* Search Bar */}
        <div className="relative mt-4">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
          <input
            type="text"
            placeholder="Search chats..."
            value={searchQuery}
            onChange={(e) => onSearchChange?.(e.target.value)}
            className="w-full pl-9 pr-4 py-2 border rounded-md"
          />
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-2 max-h-[600px] overflow-y-auto">
          {sortedSessions.map((session) => (
            <div
              key={session.id}
              className="p-3 rounded-lg border hover:bg-muted/50 cursor-pointer transition-colors group"
              onClick={() => onSelectSession?.(session)}
            >
              <div className="flex items-start justify-between gap-2">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <h4 className="font-medium truncate">{session.title}</h4>
                    {session.isFavorite && (
                      <Star className="w-4 h-4 fill-yellow-400 text-yellow-400 flex-shrink-0" />
                    )}
                  </div>
                  
                  <p className="text-sm text-muted-foreground line-clamp-2 mb-2">
                    {session.preview}
                  </p>
                  
                  <div className="flex items-center gap-3 text-xs text-muted-foreground">
                    <div className="flex items-center gap-1">
                      <Clock className="w-3 h-3" />
                      {getTimeAgo(session.lastModified)}
                    </div>
                    <span>{session.messageCount} messages</span>
                    {session.agent && (
                      <Badge variant="outline" className="text-xs">
                        {session.agent}
                      </Badge>
                    )}
                  </div>
                  
                  {session.tags && session.tags.length > 0 && (
                    <div className="flex flex-wrap gap-1 mt-2">
                      {session.tags.map((tag) => (
                        <Badge key={tag} variant="secondary" className="text-xs">
                          {tag}
                        </Badge>
                      ))}
                    </div>
                  )}
                </div>
                
                <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={(e) => {
                      e.stopPropagation();
                      onToggleFavorite?.(session.id);
                    }}
                  >
                    <Star className={`w-4 h-4 ${session.isFavorite ? 'fill-yellow-400 text-yellow-400' : ''}`} />
                  </Button>
                  {onDeleteSession && (
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={(e) => {
                        e.stopPropagation();
                        onDeleteSession(session.id);
                      }}
                    >
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
        
        {sortedSessions.length === 0 && (
          <div className="text-center py-8 text-muted-foreground">
            <MessageSquare className="w-12 h-12 mx-auto mb-4 opacity-50" />
            <p>No recent chats</p>
            <p className="text-sm mt-1">Start a new conversation to get started</p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default RecentChatList;
