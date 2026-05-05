'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { 
  FileCode, 
  FileText, 
  Image as ImageIcon, 
  Download,
  Eye,
  Copy,
  Trash2,
  Search,
  Folder
} from 'lucide-react';

export type ArtifactFileType = 'code' | 'text' | 'image' | 'other';

export interface ArtifactFile {
  id: string;
  name: string;
  type: ArtifactFileType;
  size: number;
  createdAt: Date;
  modifiedAt: Date;
  path: string;
  content?: string;
}

interface ArtifactFileListProps {
  files: ArtifactFile[];
  onDownload?: (file: ArtifactFile) => void;
  onView?: (file: ArtifactFile) => void;
  onCopy?: (file: ArtifactFile) => void;
  onDelete?: (file: ArtifactFile) => void;
  searchQuery?: string;
  onSearchChange?: (query: string) => void;
}

export function ArtifactFileList({
  files,
  onDownload,
  onView,
  onCopy,
  onDelete,
  searchQuery = '',
  onSearchChange
}: ArtifactFileListProps) {
  const getFileIcon = (type: ArtifactFileType) => {
    switch (type) {
      case 'code': return <FileCode className="w-4 h-4" />;
      case 'text': return <FileText className="w-4 h-4" />;
      case 'image': return <ImageIcon className="w-4 h-4" />;
      default: return <FileText className="w-4 h-4" />;
    }
  };

  const getFileTypeColor = (type: ArtifactFileType) => {
    switch (type) {
      case 'code': return 'bg-blue-500';
      case 'text': return 'bg-gray-500';
      case 'image': return 'bg-green-500';
      default: return 'bg-gray-400';
    }
  };

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  };

  const filteredFiles = files.filter(file =>
    file.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    file.path.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const filesByType: Record<ArtifactFileType, ArtifactFile[]> = {
    code: filteredFiles.filter(f => f.type === 'code'),
    text: filteredFiles.filter(f => f.type === 'text'),
    image: filteredFiles.filter(f => f.type === 'image'),
    other: filteredFiles.filter(f => f.type === 'other')
  };

  const renderFileList = (fileList: ArtifactFile[]) => {
    if (fileList.length === 0) {
      return (
        <div className="text-center py-8 text-muted-foreground">
          <Folder className="w-12 h-12 mx-auto mb-4 opacity-50" />
          <p>No files found</p>
        </div>
      );
    }

    return (
      <div className="space-y-2">
        {fileList.map((file) => (
          <div
            key={file.id}
            className="flex items-center gap-3 p-3 rounded-lg border hover:bg-muted/50 transition-colors"
          >
            <div className={`p-2 rounded ${getFileTypeColor(file.type)} text-white`}>
              {getFileIcon(file.type)}
            </div>
            <div className="flex-1 min-w-0">
              <p className="font-medium truncate">{file.name}</p>
              <p className="text-xs text-muted-foreground truncate">
                {file.path}
              </p>
              <div className="flex items-center gap-2 mt-1 text-xs text-muted-foreground">
                <span>{formatFileSize(file.size)}</span>
                <span>•</span>
                <span>{new Date(file.modifiedAt).toLocaleDateString()}</span>
              </div>
            </div>
            <div className="flex gap-1">
              {file.type !== 'other' && onView && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => onView(file)}
                >
                  <Eye className="w-4 h-4" />
                </Button>
              )}
              {onCopy && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => onCopy(file)}
                >
                  <Copy className="w-4 h-4" />
                </Button>
              )}
              {onDownload && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => onDownload(file)}
                >
                  <Download className="w-4 h-4" />
                </Button>
              )}
              {onDelete && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => onDelete(file)}
                >
                  <Trash2 className="w-4 h-4" />
                </Button>
              )}
            </div>
          </div>
        ))}
      </div>
    );
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <Folder className="w-5 h-5" />
            Artifact Files
            <Badge variant="secondary">{files.length}</Badge>
          </CardTitle>
          
          {/* Search Bar */}
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
            <input
              type="text"
              placeholder="Search files..."
              value={searchQuery}
              onChange={(e) => onSearchChange?.(e.target.value)}
              className="w-64 pl-9 pr-4 py-2 border rounded-md text-sm"
            />
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <Tabs defaultValue="all" className="w-full">
          <TabsList>
            <TabsTrigger value="all">All ({filteredFiles.length})</TabsTrigger>
            <TabsTrigger value="code">Code ({filesByType.code.length})</TabsTrigger>
            <TabsTrigger value="text">Text ({filesByType.text.length})</TabsTrigger>
            <TabsTrigger value="image">Images ({filesByType.image.length})</TabsTrigger>
            <TabsTrigger value="other">Other ({filesByType.other.length})</TabsTrigger>
          </TabsList>

          <TabsContent value="all" className="mt-4">
            {renderFileList(filteredFiles)}
          </TabsContent>

          <TabsContent value="code" className="mt-4">
            {renderFileList(filesByType.code)}
          </TabsContent>

          <TabsContent value="text" className="mt-4">
            {renderFileList(filesByType.text)}
          </TabsContent>

          <TabsContent value="image" className="mt-4">
            {renderFileList(filesByType.image)}
          </TabsContent>

          <TabsContent value="other" className="mt-4">
            {renderFileList(filesByType.other)}
          </TabsContent>
        </Tabs>
      </CardContent>
    </Card>
  );
}

export default ArtifactFileList;
