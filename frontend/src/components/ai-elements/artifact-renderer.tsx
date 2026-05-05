'use client';

import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { 
  FileCode, 
  FileImage, 
  FileText, 
  Download, 
  Copy,
  Eye,
  ExternalLink,
  Code,
  File
} from 'lucide-react';

export type ArtifactType = 'code' | 'image' | 'text' | 'web' | 'file';

export interface Artifact {
  id: string;
  type: ArtifactType;
  name: string;
  content?: string;
  url?: string;
  language?: string;
  size?: number;
  createdAt: Date;
  metadata?: Record<string, any>;
}

interface ArtifactRendererProps {
  artifacts: Artifact[];
  onDownload?: (artifact: Artifact) => void;
  onCopy?: (artifact: Artifact) => void;
  onPreview?: (artifact: Artifact) => void;
}

export function ArtifactRenderer({ 
  artifacts, 
  onDownload, 
  onCopy, 
  onPreview 
}: ArtifactRendererProps) {
  const [selectedArtifact, setSelectedArtifact] = useState<Artifact | null>(
    artifacts.length > 0 ? artifacts[0] : null
  );

  if (artifacts.length === 0) {
    return (
      <Card>
        <CardContent className="p-8 text-center text-muted-foreground">
          <File className="w-12 h-12 mx-auto mb-4 opacity-50" />
          <p>No artifacts generated yet</p>
        </CardContent>
      </Card>
    );
  }

  const getArtifactIcon = (type: ArtifactType) => {
    switch (type) {
      case 'code': return <Code className="w-4 h-4" />;
      case 'image': return <FileImage className="w-4 h-4" />;
      case 'text': return <FileText className="w-4 h-4" />;
      case 'web': return <ExternalLink className="w-4 h-4" />;
      default: return <File className="w-4 h-4" />;
    }
  };

  const getArtifactTypeColor = (type: ArtifactType) => {
    switch (type) {
      case 'code': return 'bg-blue-500';
      case 'image': return 'bg-green-500';
      case 'text': return 'bg-gray-500';
      case 'web': return 'bg-purple-500';
      default: return 'bg-gray-400';
    }
  };

  const renderArtifactContent = (artifact: Artifact) => {
    switch (artifact.type) {
      case 'code':
        return (
          <div className="relative">
            <pre className="p-4 bg-muted rounded-lg overflow-x-auto text-sm">
              <code>{artifact.content || '// No content'}</code>
            </pre>
            {artifact.language && (
              <Badge className="absolute top-2 right-2" variant="secondary">
                {artifact.language}
              </Badge>
            )}
          </div>
        );

      case 'image':
        return (
          <div className="flex items-center justify-center p-4 bg-muted rounded-lg">
            {artifact.url ? (
              <img 
                src={artifact.url} 
                alt={artifact.name} 
                className="max-w-full max-h-96 object-contain"
              />
            ) : (
              <p className="text-muted-foreground">No image URL provided</p>
            )}
          </div>
        );

      case 'text':
        return (
          <div className="p-4 bg-muted rounded-lg">
            <p className="whitespace-pre-wrap text-sm">{artifact.content || 'No content'}</p>
          </div>
        );

      case 'web':
        return (
          <div className="p-4 bg-muted rounded-lg">
            {artifact.url ? (
              <a 
                href={artifact.url} 
                target="_blank" 
                rel="noopener noreferrer"
                className="flex items-center gap-2 text-blue-500 hover:underline"
              >
                <ExternalLink className="w-4 h-4" />
                {artifact.url}
              </a>
            ) : (
              <p className="text-muted-foreground">No URL provided</p>
            )}
          </div>
        );

      default:
        return (
          <div className="p-4 bg-muted rounded-lg">
            <p className="text-muted-foreground">Artifact type: {artifact.type}</p>
          </div>
        );
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <FileCode className="w-5 h-5" />
            Generated Artifacts
            <Badge variant="secondary">{artifacts.length}</Badge>
          </CardTitle>
        </div>
      </CardHeader>
      <CardContent>
        <Tabs defaultValue="list" className="w-full">
          <TabsList>
            <TabsTrigger value="list">List View</TabsTrigger>
            <TabsTrigger value="preview">Preview</TabsTrigger>
          </TabsList>

          <TabsContent value="list" className="space-y-2">
            {artifacts.map((artifact) => (
              <div
                key={artifact.id}
                className="flex items-center gap-3 p-3 rounded-lg border hover:bg-muted/50 cursor-pointer transition-colors"
                onClick={() => setSelectedArtifact(artifact)}
              >
                <div className={`p-2 rounded ${getArtifactTypeColor(artifact.type)} text-white`}>
                  {getArtifactIcon(artifact.type)}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="font-medium truncate">{artifact.name}</p>
                  <p className="text-xs text-muted-foreground">
                    {artifact.size && `${(artifact.size / 1024).toFixed(1)} KB • `}
                    {new Date(artifact.createdAt).toLocaleTimeString()}
                  </p>
                </div>
                <div className="flex gap-2">
                  {onPreview && artifact.type !== 'text' && (
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={(e) => {
                        e.stopPropagation();
                        onPreview(artifact);
                      }}
                    >
                      <Eye className="w-4 h-4" />
                    </Button>
                  )}
                  {onCopy && (
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={(e) => {
                        e.stopPropagation();
                        onCopy(artifact);
                      }}
                    >
                      <Copy className="w-4 h-4" />
                    </Button>
                  )}
                  {onDownload && (
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={(e) => {
                        e.stopPropagation();
                        onDownload(artifact);
                      }}
                    >
                      <Download className="w-4 h-4" />
                    </Button>
                  )}
                </div>
              </div>
            ))}
          </TabsContent>

          <TabsContent value="preview">
            {selectedArtifact && (
              <div className="space-y-4">
                <div className="flex items-center gap-2">
                  <div className={`p-2 rounded ${getArtifactTypeColor(selectedArtifact.type)} text-white`}>
                    {getArtifactIcon(selectedArtifact.type)}
                  </div>
                  <div>
                    <p className="font-medium">{selectedArtifact.name}</p>
                    <p className="text-xs text-muted-foreground">
                      Type: {selectedArtifact.type}
                      {selectedArtifact.language && ` • Language: ${selectedArtifact.language}`}
                    </p>
                  </div>
                </div>
                {renderArtifactContent(selectedArtifact)}
              </div>
            )}
          </TabsContent>
        </Tabs>
      </CardContent>
    </Card>
  );
}

export default ArtifactRenderer;
