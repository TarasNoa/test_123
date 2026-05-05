'use client';

import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { 
  FileCode, 
  Plus, 
  Minus, 
  Copy,
  Eye,
  EyeOff
} from 'lucide-react';

export type DiffChangeType = 'addition' | 'deletion' | 'modification' | 'unchanged';

export interface DiffLine {
  lineNumber: number;
  content: string;
  type: DiffChangeType;
}

export interface FileDiff {
  filePath: string;
  language?: string;
  changes: DiffLine[];
  additions: number;
  deletions: number;
  modifications: number;
}

interface StructuredDiffProps {
  diffs: FileDiff[];
  onCopy?: (content: string) => void;
  onView?: (filePath: string) => void;
}

export function StructuredDiff({ diffs, onCopy, onView }: StructuredDiffProps) {
  const [showUnchanged, setShowUnchanged] = useState(false);

  const getChangeTypeIcon = (type: DiffChangeType) => {
    switch (type) {
      case 'addition': return <Plus className="w-4 h-4 text-green-500" />;
      case 'deletion': return <Minus className="w-4 h-4 text-red-500" />;
      case 'modification': return <div className="w-4 h-4 rounded bg-yellow-500" />;
      default: return null;
    }
  };

  const getChangeTypeColor = (type: DiffChangeType) => {
    switch (type) {
      case 'addition': return 'bg-green-50 dark:bg-green-950';
      case 'deletion': return 'bg-red-50 dark:bg-red-950';
      case 'modification': return 'bg-yellow-50 dark:bg-yellow-950';
      default: return '';
    }
  };

  const getTotalStats = () => {
    return diffs.reduce(
      (acc, diff) => ({
        additions: acc.additions + diff.additions,
        deletions: acc.deletions + diff.deletions,
        modifications: acc.modifications + diff.modifications,
        files: acc.files + 1
      }),
      { additions: 0, deletions: 0, modifications: 0, files: 0 }
    );
  };

  const stats = getTotalStats();

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <FileCode className="w-5 h-5" />
            Structured Diff
            <Badge variant="secondary">{stats.files} files</Badge>
          </CardTitle>
          <div className="flex items-center gap-2">
            <Badge variant="outline" className="bg-green-50 text-green-700">
              +{stats.additions}
            </Badge>
            <Badge variant="outline" className="bg-red-50 text-red-700">
              -{stats.deletions}
            </Badge>
            <Badge variant="outline" className="bg-yellow-50 text-yellow-700">
              ~{stats.modifications}
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
            <div className="flex items-center gap-2 mb-4">
              <Button
                size="sm"
                variant="outline"
                onClick={() => setShowUnchanged(!showUnchanged)}
              >
                {showUnchanged ? <EyeOff className="w-4 h-4 mr-2" /> : <Eye className="w-4 h-4 mr-2" />}
                {showUnchanged ? 'Hide Unchanged' : 'Show Unchanged'}
              </Button>
            </div>

            {diffs.map((diff) => (
              <Card key={diff.filePath}>
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <FileCode className="w-4 h-4" />
                      <span className="font-medium">{diff.filePath}</span>
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
                      {onView && (
                        <Button size="sm" variant="ghost" onClick={() => onView(diff.filePath)}>
                          <Eye className="w-4 h-4" />
                        </Button>
                      )}
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="font-mono text-sm space-y-0">
                    {diff.changes
                      .filter(line => showUnchanged || line.type !== 'unchanged')
                      .map((line, index) => (
                        <div
                          key={index}
                          className={`flex gap-4 px-2 py-0.5 ${getChangeTypeColor(line.type)}`}
                        >
                          <span className="w-12 text-right text-muted-foreground select-none">
                            {line.lineNumber}
                          </span>
                          <div className="flex items-center gap-2 flex-1">
                            {getChangeTypeIcon(line.type)}
                            <span className={line.type === 'deletion' ? 'line-through opacity-70' : ''}>
                              {line.content}
                            </span>
                          </div>
                        </div>
                      ))}
                  </div>
                </CardContent>
              </Card>
            ))}
          </TabsContent>

          <TabsContent value="split" className="space-y-4">
            {diffs.map((diff) => (
              <Card key={diff.filePath}>
                <CardHeader>
                  <CardTitle className="text-lg">{diff.filePath}</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-2 gap-0">
                    <div className="border-r bg-red-50/30 p-2">
                      <div className="font-mono text-sm space-y-0">
                        {diff.changes
                          .filter(line => line.type === 'deletion' || line.type === 'unchanged')
                          .map((line, index) => (
                            <div key={index} className="flex gap-4 px-2 py-0.5">
                              <span className="w-12 text-right text-muted-foreground select-none">
                                {line.lineNumber}
                              </span>
                              <span className={line.type === 'deletion' ? 'line-through opacity-70' : ''}>
                                {line.content}
                              </span>
                            </div>
                          ))}
                      </div>
                    </div>
                    <div className="bg-green-50/30 p-2">
                      <div className="font-mono text-sm space-y-0">
                        {diff.changes
                          .filter(line => line.type === 'addition' || line.type === 'unchanged')
                          .map((line, index) => (
                            <div key={index} className="flex gap-4 px-2 py-0.5">
                              <span className="w-12 text-right text-muted-foreground select-none">
                                {line.lineNumber}
                              </span>
                              <span>{line.content}</span>
                            </div>
                          ))}
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </TabsContent>

          <TabsContent value="summary">
            <div className="space-y-4">
              <div className="grid grid-cols-4 gap-4">
                <Card>
                  <CardContent className="p-4">
                    <div className="text-2xl font-bold">{stats.files}</div>
                    <div className="text-sm text-muted-foreground">Files Changed</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="p-4">
                    <div className="text-2xl font-bold text-green-500">+{stats.additions}</div>
                    <div className="text-sm text-muted-foreground">Additions</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="p-4">
                    <div className="text-2xl font-bold text-red-500">-{stats.deletions}</div>
                    <div className="text-sm text-muted-foreground">Deletions</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="p-4">
                    <div className="text-2xl font-bold text-yellow-500">~{stats.modifications}</div>
                    <div className="text-sm text-muted-foreground">Modifications</div>
                  </CardContent>
                </Card>
              </div>

              <div className="space-y-2">
                <h4 className="font-medium">Changed Files</h4>
                {diffs.map((diff) => (
                  <div key={diff.filePath} className="flex items-center justify-between p-3 border rounded-lg">
                    <div className="flex items-center gap-2">
                      <FileCode className="w-4 h-4" />
                      <span className="text-sm">{diff.filePath}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant="outline" className="bg-green-50 text-green-700 text-xs">
                        +{diff.additions}
                      </Badge>
                      <Badge variant="outline" className="bg-red-50 text-red-700 text-xs">
                        -{diff.deletions}
                      </Badge>
                      <Badge variant="outline" className="bg-yellow-50 text-yellow-700 text-xs">
                        ~{diff.modifications}
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

export default StructuredDiff;
