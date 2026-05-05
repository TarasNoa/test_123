'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { 
  FileCode, 
  Plus, 
  Minus, 
  ArrowRight,
  Copy,
  Download
} from 'lucide-react';

export interface DiffLine {
  lineNumber: number;
  content: string;
  type: 'addition' | 'deletion' | 'unchanged' | 'header';
}

export interface FileDiff {
  fileName: string;
  filePath: string;
  language?: string;
  additions: number;
  deletions: number;
  lines: DiffLine[];
}

interface AdvancedDiffProps {
  diffs: FileDiff[];
  onCopy?: (diff: FileDiff) => void;
  onDownload?: (diff: FileDiff) => void;
  showLineNumbers?: boolean;
}

export function AdvancedDiff({ 
  diffs, 
  onCopy, 
  onDownload,
  showLineNumbers = true 
}: AdvancedDiffProps) {
  const getLineTypeColor = (type: DiffLine['type']) => {
    switch (type) {
      case 'addition': return 'bg-green-50 dark:bg-green-950';
      case 'deletion': return 'bg-red-50 dark:bg-red-950';
      case 'header': return 'bg-gray-100 dark:bg-gray-800 font-medium';
      default: return '';
    }
  };

  const getLineTypeIcon = (type: DiffLine['type']) => {
    switch (type) {
      case 'addition': return <Plus className="w-4 h-4 text-green-500" />;
      case 'deletion': return <Minus className="w-4 h-4 text-red-500" />;
      case 'header': return null;
      default: return null;
    }
  };

  const renderDiffLine = (line: DiffLine, index: number) => {
    const lineTypeColor = getLineTypeColor(line.type);
    const icon = getLineTypeIcon(line.type);

    return (
      <div
        key={index}
        className={`flex gap-2 ${lineTypeColor} px-4 py-0.5 font-mono text-sm`}
      >
        {showLineNumbers && line.type !== 'header' && (
          <span className="w-12 text-right text-muted-foreground select-none">
            {line.lineNumber}
          </span>
        )}
        <div className="flex items-center gap-2 flex-1">
          {icon && <span className="w-4">{icon}</span>}
          <span className={line.type === 'deletion' ? 'line-through opacity-70' : ''}>
            {line.content}
          </span>
        </div>
      </div>
    );
  };

  const getTotalStats = () => {
    return diffs.reduce(
      (acc, diff) => ({
        additions: acc.additions + diff.additions,
        deletions: acc.deletions + diff.deletions,
        files: acc.files + 1
      }),
      { additions: 0, deletions: 0, files: 0 }
    );
  };

  const stats = getTotalStats();

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <FileCode className="w-5 h-5" />
            Code Changes
            <Badge variant="secondary">{stats.files} files</Badge>
          </CardTitle>
          <div className="flex gap-2">
            <Badge variant="outline" className="bg-green-50 text-green-700">
              +{stats.additions}
            </Badge>
            <Badge variant="outline" className="bg-red-50 text-red-700">
              -{stats.deletions}
            </Badge>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <Tabs defaultValue="unified" className="w-full">
          <TabsList>
            <TabsTrigger value="unified">Unified View</TabsTrigger>
            <TabsTrigger value="split">Split View</TabsTrigger>
            <TabsTrigger value="summary">Summary</TabsTrigger>
          </TabsList>

          <TabsContent value="unified" className="space-y-4">
            {diffs.map((diff, diffIndex) => (
              <div key={diffIndex} className="border rounded-lg overflow-hidden">
                <div className="flex items-center justify-between p-3 bg-muted border-b">
                  <div className="flex items-center gap-2">
                    <FileCode className="w-4 h-4" />
                    <span className="font-medium">{diff.fileName}</span>
                    {diff.language && (
                      <Badge variant="outline" className="text-xs">
                        {diff.language}
                      </Badge>
                    )}
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant="outline" className="bg-green-50 text-green-700">
                      +{diff.additions}
                    </Badge>
                    <Badge variant="outline" className="bg-red-50 text-red-700">
                      -{diff.deletions}
                    </Badge>
                    {onCopy && (
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => onCopy(diff)}
                      >
                        <Copy className="w-4 h-4" />
                      </Button>
                    )}
                    {onDownload && (
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => onDownload(diff)}
                      >
                        <Download className="w-4 h-4" />
                      </Button>
                    )}
                  </div>
                </div>
                <div className="bg-background max-h-96 overflow-y-auto">
                  {diff.lines.map((line, lineIndex) => renderDiffLine(line, lineIndex))}
                </div>
              </div>
            ))}
          </TabsContent>

          <TabsContent value="split" className="space-y-4">
            {diffs.map((diff, diffIndex) => (
              <div key={diffIndex} className="border rounded-lg overflow-hidden">
                <div className="flex items-center justify-between p-3 bg-muted border-b">
                  <div className="flex items-center gap-2">
                    <FileCode className="w-4 h-4" />
                    <span className="font-medium">{diff.fileName}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant="outline" className="bg-green-50 text-green-700">
                      +{diff.additions}
                    </Badge>
                    <Badge variant="outline" className="bg-red-50 text-red-700">
                      -{diff.deletions}
                    </Badge>
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-0 max-h-96 overflow-y-auto">
                  <div className="border-r bg-red-50/30">
                    {diff.lines
                      .filter(line => line.type === 'deletion' || line.type === 'unchanged')
                      .map((line, lineIndex) => renderDiffLine(line, lineIndex))}
                  </div>
                  <div className="bg-green-50/30">
                    {diff.lines
                      .filter(line => line.type === 'addition' || line.type === 'unchanged')
                      .map((line, lineIndex) => renderDiffLine(line, lineIndex))}
                  </div>
                </div>
              </div>
            ))}
          </TabsContent>

          <TabsContent value="summary">
            <div className="space-y-4">
              <div className="grid grid-cols-3 gap-4">
                <Card>
                  <CardContent className="p-4">
                    <div className="text-2xl font-bold text-green-500">
                      +{stats.additions}
                    </div>
                    <div className="text-sm text-muted-foreground">Total Additions</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="p-4">
                    <div className="text-2xl font-bold text-red-500">
                      -{stats.deletions}
                    </div>
                    <div className="text-sm text-muted-foreground">Total Deletions</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="p-4">
                    <div className="text-2xl font-bold">{stats.files}</div>
                    <div className="text-sm text-muted-foreground">Files Changed</div>
                  </CardContent>
                </Card>
              </div>

              <div className="space-y-2">
                <h4 className="font-medium">Changed Files</h4>
                {diffs.map((diff, index) => (
                  <div
                    key={index}
                    className="flex items-center justify-between p-3 rounded-lg border"
                  >
                    <div className="flex items-center gap-2">
                      <FileCode className="w-4 h-4" />
                      <span className="text-sm">{diff.fileName}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant="outline" className="bg-green-50 text-green-700 text-xs">
                        +{diff.additions}
                      </Badge>
                      <Badge variant="outline" className="bg-red-50 text-red-700 text-xs">
                        -{diff.deletions}
                      </Badge>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </TabsContent>
        </Tabs>
      </CardContent>
    </Card>
  );
}

export default AdvancedDiff;
