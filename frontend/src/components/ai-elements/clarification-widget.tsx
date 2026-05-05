'use client';

import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { 
  HelpCircle, 
  Send, 
  X,
  CheckCircle
} from 'lucide-react';

export interface Clarification {
  id: string;
  question: string;
  context?: string;
  timestamp: Date;
  status: 'pending' | 'answered' | 'dismissed';
  answer?: string;
}

interface ClarificationWidgetProps {
  clarifications: Clarification[];
  onAnswer?: (id: string, answer: string) => void;
  onDismiss?: (id: string) => void;
}

export function ClarificationWidget({ clarifications, onAnswer, onDismiss }: ClarificationWidgetProps) {
  const [answers, setAnswers] = useState<Record<string, string>>({});

  const handleAnswer = (id: string) => {
    if (answers[id] && answers[id].trim()) {
      onAnswer?.(id, answers[id]);
      setAnswers(prev => {
        const newAnswers = { ...prev };
        delete newAnswers[id];
        return newAnswers;
      });
    }
  };

  const pendingClarifications = clarifications.filter(c => c.status === 'pending');

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <HelpCircle className="w-5 h-5" />
          Clarifications
          <Badge variant="secondary">{pendingClarifications.length}</Badge>
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {pendingClarifications.length === 0 && clarifications.filter(c => c.status === 'answered').length > 0 ? (
            <div className="text-center py-4 text-muted-foreground">
              <CheckCircle className="w-8 h-8 mx-auto mb-2 text-green-500" />
              <p>All clarifications answered</p>
            </div>
          ) : pendingClarifications.length === 0 ? (
            <div className="text-center py-4 text-muted-foreground">
              <HelpCircle className="w-8 h-8 mx-auto mb-2 opacity-50" />
              <p>No pending clarifications</p>
            </div>
          ) : (
            pendingClarifications.map((clarification) => (
              <div key={clarification.id} className="p-4 border rounded-lg space-y-3">
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      <Badge variant="outline" className="bg-yellow-100 text-yellow-700">
                        Pending
                      </Badge>
                      <span className="text-xs text-muted-foreground">
                        {new Date(clarification.timestamp).toLocaleTimeString()}
                      </span>
                    </div>
                    <p className="font-medium mb-1">{clarification.question}</p>
                    {clarification.context && (
                      <p className="text-sm text-muted-foreground mb-2">
                        Context: {clarification.context}
                      </p>
                    )}
                  </div>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => onDismiss?.(clarification.id)}
                  >
                    <X className="w-4 h-4" />
                  </Button>
                </div>

                <Textarea
                  placeholder="Provide clarification..."
                  value={answers[clarification.id] || ''}
                  onChange={(e) => setAnswers(prev => ({ ...prev, [clarification.id]: e.target.value }))}
                  className="min-h-[80px]"
                />

                <div className="flex justify-end">
                  <Button
                    size="sm"
                    onClick={() => handleAnswer(clarification.id)}
                    disabled={!answers[clarification.id]?.trim()}
                  >
                    <Send className="w-4 h-4 mr-2" />
                    Send Answer
                  </Button>
                </div>
              </div>
            ))
          )}

          {/* Answered History */}
          {clarifications.filter(c => c.status === 'answered').length > 0 && (
            <details className="pt-4 border-t">
              <summary className="text-sm font-medium cursor-pointer mb-2">
                Answered History ({clarifications.filter(c => c.status === 'answered').length})
              </summary>
              <div className="space-y-2 mt-2">
                {clarifications.filter(c => c.status === 'answered').map((c) => (
                  <div key={c.id} className="p-3 bg-muted rounded-lg">
                    <p className="text-sm font-medium">{c.question}</p>
                    {c.answer && (
                      <p className="text-sm text-muted-foreground mt-1">
                        A: {c.answer}
                      </p>
                    )}
                  </div>
                ))}
              </div>
            </details>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

export default ClarificationWidget;
