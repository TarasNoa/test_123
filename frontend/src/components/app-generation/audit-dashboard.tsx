'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { 
  Shield, 
  Gauge, 
  Search, 
  AlertTriangle,
  CheckCircle,
  XCircle,
  AlertCircle
} from 'lucide-react';

export type AuditCategory = 'accessibility' | 'performance' | 'seo' | 'best_practices';

export interface AuditResult {
  category: AuditCategory;
  score: number;
  passCount: number;
  failCount: number;
  warnings: string[];
  errors: string[];
  passed: string[];
}

interface AuditDashboardProps {
  results: AuditResult[];
  onRunAudit?: (categories?: AuditCategory[]) => void;
  isRunning?: boolean;
  lastRun?: Date;
}

export function AuditDashboard({ 
  results, 
  onRunAudit, 
  isRunning = false,
  lastRun
}: AuditDashboardProps) {
  const getScoreColor = (score: number) => {
    if (score >= 90) return 'text-green-500';
    if (score >= 70) return 'text-yellow-500';
    return 'text-red-500';
  };

  const getScoreVariant = (score: number) => {
    if (score >= 90) return 'default';
    if (score >= 70) return 'secondary';
    return 'destructive';
  };

  const getCategoryIcon = (category: AuditCategory) => {
    switch (category) {
      case 'accessibility': return <Shield className="w-4 h-4" />;
      case 'performance': return <Gauge className="w-4 h-4" />;
      case 'seo': return <Search className="w-4 h-4" />;
      case 'best_practices': return <CheckCircle className="w-4 h-4" />;
    }
  };

  const getCategoryLabel = (category: AuditCategory) => {
    switch (category) {
      case 'accessibility': return 'Accessibility';
      case 'performance': return 'Performance';
      case 'seo': return 'SEO';
      case 'best_practices': return 'Best Practices';
    }
  };

  const getOverallScore = () => {
    if (results.length === 0) return 0;
    const total = results.reduce((sum, r) => sum + r.score, 0);
    return Math.round(total / results.length);
  };

  const overallScore = getOverallScore();

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <Shield className="w-5 h-5" />
            Audit Dashboard
          </CardTitle>
          <div className="flex items-center gap-3">
            {lastRun && (
              <span className="text-sm text-muted-foreground">
                Last run: {new Date(lastRun).toLocaleString()}
              </span>
            )}
            <Button
              size="sm"
              onClick={() => onRunAudit?.()}
              disabled={isRunning}
            >
              {isRunning ? 'Running...' : 'Run Audit'}
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <Tabs defaultValue="overview" className="w-full">
          <TabsList>
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="details">Details</TabsTrigger>
          </TabsList>

          <TabsContent value="overview">
            <div className="space-y-6">
              {/* Overall Score */}
              <div className="text-center p-6 bg-muted rounded-lg">
                <div className={`text-6xl font-bold ${getScoreColor(overallScore)}`}>
                  {overallScore}
                </div>
                <p className="text-muted-foreground mt-2">Overall Score</p>
              </div>

              {/* Category Scores */}
              <div className="grid grid-cols-2 gap-4">
                {results.map((result) => (
                  <Card key={result.category}>
                    <CardContent className="p-4">
                      <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center gap-2">
                          {getCategoryIcon(result.category)}
                          <span className="font-medium">{getCategoryLabel(result.category)}</span>
                        </div>
                        <Badge variant={getScoreVariant(result.score)}>
                          {result.score}
                        </Badge>
                      </div>
                      <Progress value={result.score} className="mb-2" />
                      <div className="flex justify-between text-xs text-muted-foreground">
                        <span>{result.passCount} passed</span>
                        <span>{result.failCount} failed</span>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>

              {results.length === 0 && (
                <div className="text-center py-8 text-muted-foreground">
                  <Shield className="w-12 h-12 mx-auto mb-4 opacity-50" />
                  <p>No audit results yet</p>
                  <p className="text-sm mt-1">Run an audit to see performance metrics</p>
                </div>
              )}
            </div>
          </TabsContent>

          <TabsContent value="details">
            <div className="space-y-4">
              {results.map((result) => (
                <Card key={result.category}>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2 text-lg">
                      {getCategoryIcon(result.category)}
                      {getCategoryLabel(result.category)}
                      <Badge variant={getScoreVariant(result.score)}>
                        {result.score}
                      </Badge>
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      {/* Passed */}
                      {result.passed.length > 0 && (
                        <div>
                          <div className="flex items-center gap-2 mb-2">
                            <CheckCircle className="w-4 h-4 text-green-500" />
                            <span className="font-medium text-sm">Passed ({result.passed.length})</span>
                          </div>
                          <ul className="space-y-1 ml-6">
                            {result.passed.map((item, i) => (
                              <li key={i} className="text-sm text-muted-foreground">
                                • {item}
                              </li>
                            ))}
                          </ul>
                        </div>
                      )}

                      {/* Warnings */}
                      {result.warnings.length > 0 && (
                        <div>
                          <div className="flex items-center gap-2 mb-2">
                            <AlertTriangle className="w-4 h-4 text-yellow-500" />
                            <span className="font-medium text-sm">Warnings ({result.warnings.length})</span>
                          </div>
                          <ul className="space-y-1 ml-6">
                            {result.warnings.map((item, i) => (
                              <li key={i} className="text-sm text-yellow-700">
                                • {item}
                              </li>
                            ))}
                          </ul>
                        </div>
                      )}

                      {/* Errors */}
                      {result.errors.length > 0 && (
                        <div>
                          <div className="flex items-center gap-2 mb-2">
                            <XCircle className="w-4 h-4 text-red-500" />
                            <span className="font-medium text-sm">Errors ({result.errors.length})</span>
                          </div>
                          <ul className="space-y-1 ml-6">
                            {result.errors.map((item, i) => (
                              <li key={i} className="text-sm text-red-700">
                                • {item}
                              </li>
                            ))}
                          </ul>
                        </div>
                      )}

                      {result.passed.length === 0 && result.warnings.length === 0 && result.errors.length === 0 && (
                        <p className="text-sm text-muted-foreground">No details available</p>
                      )}
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          </TabsContent>
        </Tabs>
      </CardContent>
    </Card>
  );
}

export default AuditDashboard;
