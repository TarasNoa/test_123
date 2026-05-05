"use client";

import { ChevronDownIcon, ChevronRightIcon, FileIcon, FolderIcon } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { ScrollArea } from "@/components/ui/scroll-area";

export type ContextFile = {
  path: string;
  size: number;
  lastModified: Date;
  relevanceScore: number;
};

export type ContextDirectory = {
  path: string;
  files: ContextFile[];
  subdirectories: ContextDirectory[];
  relevanceScore: number;
};

export type ContextVisualizationProps = {
  directories: ContextDirectory[];
  totalTokens: number;
  maxTokens: number;
  defaultExpanded?: boolean;
};

export function ContextVisualization({
  directories,
  totalTokens,
  maxTokens,
  defaultExpanded = false,
}: ContextVisualizationProps) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);

  const percentage = (totalTokens / maxTokens) * 100;
  const isNearLimit = percentage >= 80;

  return (
    <Card className="border-l-4 border-l-purple-500 bg-muted/50">
      <div className="p-4">
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-2">
            <div className="font-semibold text-sm">Context Used</div>
            <Badge variant={isNearLimit ? "destructive" : "secondary"}>
              {totalTokens.toLocaleString()} / {maxTokens.toLocaleString()} tokens
            </Badge>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setIsExpanded(!isExpanded)}
            className="h-6 px-2"
          >
            {isExpanded ? (
              <ChevronDownIcon className="h-4 w-4" />
            ) : (
              <ChevronRightIcon className="h-4 w-4" />
            )}
          </Button>
        </div>

        {isExpanded && (
          <ScrollArea className="h-[400px]">
            <div className="space-y-2">
              {directories.map((dir, index) => (
                <ContextDirectoryItem
                  key={`${dir.path}-${index}`}
                  directory={dir}
                  level={0}
                />
              ))}
            </div>
          </ScrollArea>
        )}
      </div>
    </Card>
  );
}

function ContextDirectoryItem({
  directory,
  level,
}: {
  directory: ContextDirectory;
  level: number;
}) {
  const [isExpanded, setIsExpanded] = useState(true);

  const relevanceColor = getRelevanceColor(directory.relevanceScore);

  return (
    <div className="ml-[{level * 16}px]">
      <div
        className="flex items-center gap-2 py-1 px-2 hover:bg-muted rounded cursor-pointer"
        style={{ marginLeft: `${level * 16}px` }}
        onClick={() => setIsExpanded(!isExpanded)}
      >
        <Button variant="ghost" size="sm" className="h-4 w-4 p-0">
          {isExpanded ? (
            <ChevronDownIcon className="h-3 w-3" />
          ) : (
            <ChevronRightIcon className="h-3 w-3" />
          )}
        </Button>
        <FolderIcon className="h-4 w-4 text-muted-foreground" />
        <span className="text-sm font-medium">{directory.path}</span>
        <Badge variant="outline" className={`ml-auto ${relevanceColor}`}>
          {(directory.relevanceScore * 100).toFixed(0)}%
        </Badge>
      </div>

      {isExpanded && (
        <div className="space-y-1">
          {directory.files.map((file) => (
            <ContextFileItem key={file.path} file={file} level={level + 1} />
          ))}
          {directory.subdirectories.map((subdir, index) => (
            <ContextDirectoryItem
              key={`${subdir.path}-${index}`}
              directory={subdir}
              level={level + 1}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function ContextFileItem({ file, level }: { file: ContextFile; level: number }) {
  const relevanceColor = getRelevanceColor(file.relevanceScore);

  return (
    <div
      className="flex items-center gap-2 py-1 px-2 hover:bg-muted rounded"
      style={{ marginLeft: `${level * 16}px` }}
    >
      <div className="w-4" />
      <FileIcon className="h-4 w-4 text-muted-foreground" />
      <span className="text-sm truncate flex-1">{file.path}</span>
      <Badge variant="outline" className={`text-xs ${relevanceColor}`}>
        {(file.relevanceScore * 100).toFixed(0)}%
      </Badge>
      <span className="text-xs text-muted-foreground">
        {file.size.toLocaleString()} bytes
      </span>
    </div>
  );
}

function getRelevanceColor(score: number): string {
  if (score >= 0.8) return "bg-green-100 text-green-700";
  if (score >= 0.5) return "bg-yellow-100 text-yellow-700";
  return "bg-gray-100 text-gray-700";
}
