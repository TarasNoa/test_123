"use client";

import { ChevronDownIcon, ChevronRightIcon } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

export type ChainOfThoughtStep = {
  id: string;
  stepNumber: number;
  description: string;
  reasoning: string;
  timestamp: Date;
  status: "pending" | "in-progress" | "completed" | "failed";
};

export type ChainOfThoughtProps = {
  steps: ChainOfThoughtStep[];
  defaultExpanded?: boolean;
};

export function ChainOfThought({ steps, defaultExpanded = false }: ChainOfThoughtProps) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);

  if (steps.length === 0) {
    return null;
  }

  const completedSteps = steps.filter(s => s.status === "completed").length;
  const totalSteps = steps.length;
  const progress = totalSteps > 0 ? (completedSteps / totalSteps) * 100 : 0;

  return (
    <Card className="border-l-4 border-l-blue-500 bg-muted/50">
      <div className="p-4">
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-2">
            <div className="font-semibold text-sm">Chain of Thought</div>
            <div className="text-xs text-muted-foreground">
              {completedSteps}/{totalSteps} steps
            </div>
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

        {/* Progress bar */}
        <div className="w-full bg-secondary h-1.5 rounded-full mb-3">
          <div
            className="bg-blue-500 h-1.5 rounded-full transition-all duration-300"
            style={{ width: `${progress}%` }}
          />
        </div>

        {isExpanded && (
          <div className="space-y-2">
            {steps.map((step) => (
              <ChainOfThoughtStep key={step.id} step={step} />
            ))}
          </div>
        )}
      </div>
    </Card>
  );
}

function ChainOfThoughtStep({ step }: { step: ChainOfThoughtStep }) {
  const statusColors = {
    pending: "text-muted-foreground",
    "in-progress": "text-blue-500",
    completed: "text-green-500",
    failed: "text-red-500",
  };

  const statusIcons = {
    pending: "○",
    "in-progress": "◐",
    completed: "●",
    failed: "✕",
  };

  return (
    <div className="flex gap-3 text-sm">
      <div className="flex-shrink-0 w-6 h-6 flex items-center justify-center">
        <span className={statusColors[step.status]}>{statusIcons[step.status]}</span>
      </div>
      <div className="flex-1 min-w-0">
        <div className="font-medium">{step.description}</div>
        {step.reasoning && (
          <div className="text-muted-foreground text-xs mt-1">
            {step.reasoning}
          </div>
        )}
      </div>
      <div className="text-xs text-muted-foreground flex-shrink-0">
        {step.timestamp.toLocaleTimeString()}
      </div>
    </div>
  );
}
