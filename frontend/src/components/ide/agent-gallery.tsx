'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { 
  Bot,
  Plus,
  Settings,
  Star,
  Clock,
  Code,
  Search,
  Filter
} from 'lucide-react';

export interface Agent {
  id: string;
  name: string;
  description: string;
  avatar?: string;
  specialization: string;
  capabilities: string[];
  lastUsed?: Date;
  isFavorite?: boolean;
  isCustom?: boolean;
}

interface AgentGalleryProps {
  agents: Agent[];
  onCreateAgent?: () => void;
  onSelectAgent?: (agent: Agent) => void;
  onConfigureAgent?: (agent: Agent) => void;
  onToggleFavorite?: (agentId: string) => void;
  searchQuery?: string;
  onSearchChange?: (query: string) => void;
}

export function AgentGallery({
  agents,
  onCreateAgent,
  onSelectAgent,
  onConfigureAgent,
  onToggleFavorite,
  searchQuery = '',
  onSearchChange
}: AgentGalleryProps) {
  const filteredAgents = agents.filter(agent =>
    agent.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    agent.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
    agent.specialization.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const getSpecializationColor = (spec: string) => {
    const colors: Record<string, string> = {
      'Code': 'bg-blue-500',
      'Data Analysis': 'bg-green-500',
      'Architecture': 'bg-purple-500',
      'Debugging': 'bg-red-500',
      'Testing': 'bg-yellow-500',
      'Documentation': 'bg-gray-500'
    };
    return colors[spec] || 'bg-gray-400';
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <Bot className="w-5 h-5" />
            Agent Gallery
            <Badge variant="secondary">{agents.length}</Badge>
          </CardTitle>
          <Button size="sm" onClick={onCreateAgent}>
            <Plus className="w-4 h-4 mr-2" />
            Create Agent
          </Button>
        </div>
        
        {/* Search Bar */}
        <div className="relative mt-4">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
          <input
            type="text"
            placeholder="Search agents..."
            value={searchQuery}
            onChange={(e) => onSearchChange?.(e.target.value)}
            className="w-full pl-9 pr-4 py-2 border rounded-md"
          />
        </div>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredAgents.map((agent) => (
            <Card
              key={agent.id}
              className="cursor-pointer hover:shadow-lg transition-shadow"
              onClick={() => onSelectAgent?.(agent)}
            >
              <CardContent className="p-4">
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center text-white font-medium">
                      {agent.name.charAt(0).toUpperCase()}
                    </div>
                    <div>
                      <h4 className="font-medium">{agent.name}</h4>
                      <Badge 
                        variant="outline" 
                        className={getSpecializationColor(agent.specialization)}
                      >
                        {agent.specialization}
                      </Badge>
                    </div>
                  </div>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={(e) => {
                      e.stopPropagation();
                      onToggleFavorite?.(agent.id);
                    }}
                  >
                    <Star 
                      className={`w-4 h-4 ${agent.isFavorite ? 'fill-yellow-400 text-yellow-400' : ''}`} 
                    />
                  </Button>
                </div>
                
                <p className="text-sm text-muted-foreground mb-3 line-clamp-2">
                  {agent.description}
                </p>
                
                <div className="flex flex-wrap gap-1 mb-3">
                  {agent.capabilities.slice(0, 3).map((cap) => (
                    <Badge key={cap} variant="secondary" className="text-xs">
                      {cap}
                    </Badge>
                  ))}
                  {agent.capabilities.length > 3 && (
                    <Badge variant="secondary" className="text-xs">
                      +{agent.capabilities.length - 3}
                    </Badge>
                  )}
                </div>
                
                <div className="flex items-center justify-between text-xs text-muted-foreground">
                  {agent.lastUsed && (
                    <div className="flex items-center gap-1">
                      <Clock className="w-3 h-3" />
                      {new Date(agent.lastUsed).toLocaleDateString()}
                    </div>
                  )}
                  {agent.isCustom && (
                    <Badge variant="outline" className="text-xs">
                      Custom
                    </Badge>
                  )}
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={(e) => {
                      e.stopPropagation();
                      onConfigureAgent?.(agent);
                    }}
                  >
                    <Settings className="w-3 h-3" />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
        
        {filteredAgents.length === 0 && (
          <div className="text-center py-8 text-muted-foreground">
            <Bot className="w-12 h-12 mx-auto mb-4 opacity-50" />
            <p>No agents found</p>
            <p className="text-sm mt-1">Create your first agent to get started</p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default AgentGallery;
