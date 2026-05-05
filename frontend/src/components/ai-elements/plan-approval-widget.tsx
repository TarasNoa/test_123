'use client';

import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Textarea } from '@/components/ui/textarea';
import { 
  CheckCircle, 
  XCircle, 
  Edit,
  FileText,
  Clock
} from 'lucide-react';

export interface PlanStep {
  id: string;
  title: string;
  description: string;
  estimatedTime?: string;
}

export interface Plan {
  id: string;
  title: string;
  description: string;
  steps: PlanStep[];
  estimatedTotalTime?: string;
  createdAt: Date;
}

interface PlanApprovalWidgetProps {
  plan: Plan;
  onApprove?: (feedback?: string) => void;
  onReject?: (feedback: string) => void;
  onEdit?: (plan: Plan) => void;
}

export function PlanApprovalWidget({ plan, onApprove, onReject, onEdit }: PlanApprovalWidgetProps) {
  const [feedback, setFeedback] = useState('');
  const [showFeedback, setShowFeedback] = useState(false);

  const handleApprove = () => {
    onApprove?.(feedback || undefined);
    setFeedback('');
    setShowFeedback(false);
  };

  const handleReject = () => {
    if (feedback.trim()) {
      onReject?.(feedback);
      setFeedback('');
      setShowFeedback(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <FileText className="w-5 h-5" />
            Plan Approval
          </CardTitle>
          <Badge variant="outline" className="bg-yellow-100 text-yellow-700">
            Pending
          </Badge>
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {/* Plan Overview */}
          <div>
            <h3 className="font-semibold mb-2">{plan.title}</h3>
            <p className="text-sm text-muted-foreground mb-2">{plan.description}</p>
            <div className="flex items-center gap-4 text-xs text-muted-foreground">
              <div className="flex items-center gap-1">
                <Clock className="w-3 h-3" />
                <span>{plan.estimatedTotalTime || 'Unknown duration'}</span>
              </div>
              <span>{plan.steps.length} steps</span>
              <span>Created: {new Date(plan.createdAt).toLocaleString()}</span>
            </div>
          </div>

          {/* Plan Steps */}
          <div className="space-y-2">
            <h4 className="font-medium text-sm">Proposed Steps</h4>
            <div className="space-y-2">
              {plan.steps.map((step, index) => (
                <div key={step.id} className="flex gap-3 p-2 border rounded-lg">
                  <div className="flex-shrink-0 w-6 h-6 rounded-full bg-primary text-primary-foreground flex items-center justify-center text-xs">
                    {index + 1}
                  </div>
                  <div className="flex-1">
                    <p className="text-sm font-medium">{step.title}</p>
                    <p className="text-xs text-muted-foreground">{step.description}</p>
                    {step.estimatedTime && (
                      <p className="text-xs text-muted-foreground mt-1">
                        Est: {step.estimatedTime}
                      </p>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Feedback Input */}
          {showFeedback && (
            <div className="space-y-2">
              <label className="text-sm font-medium">Feedback (Optional)</label>
              <Textarea
                placeholder="Provide feedback for approval/rejection..."
                value={feedback}
                onChange={(e) => setFeedback(e.target.value)}
                className="min-h-[80px]"
              />
            </div>
          )}

          {/* Actions */}
          <div className="flex flex-col gap-2">
            <div className="flex gap-2">
              <Button onClick={handleApprove} className="flex-1">
                <CheckCircle className="w-4 h-4 mr-2" />
                Approve Plan
              </Button>
              <Button variant="outline" onClick={() => setShowFeedback(!showFeedback)}>
                {showFeedback ? 'Hide Feedback' : 'Add Feedback'}
              </Button>
            </div>
            {showFeedback && (
              <Button variant="destructive" onClick={handleReject} disabled={!feedback.trim()}>
                <XCircle className="w-4 h-4 mr-2" />
                Reject with Feedback
              </Button>
            )}
            {onEdit && (
              <Button variant="ghost" onClick={() => onEdit(plan)}>
                <Edit className="w-4 h-4 mr-2" />
                Edit Plan
              </Button>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

export default PlanApprovalWidget;
