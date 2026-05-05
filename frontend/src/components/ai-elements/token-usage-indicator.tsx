"use client";

import { AlertTriangleIcon, InfoIcon } from "lucide-react";
import { Card } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";

export type TokenUsageProps = {
  currentTokens: number;
  maxTokens: number;
  cost?: number;
  showWarning?: boolean;
  warningThreshold?: number;
};

export function TokenUsageIndicator({
  currentTokens,
  maxTokens,
  cost,
  showWarning = true,
  warningThreshold = 0.8,
}: TokenUsageProps) {
  const percentage = (currentTokens / maxTokens) * 100;
  const isNearLimit = percentage >= warningThreshold * 100;
  const isOverLimit = percentage >= 100;

  const getProgressColor = () => {
    if (isOverLimit) return "bg-red-500";
    if (isNearLimit) return "bg-yellow-500";
    return "bg-blue-500";
  };

  const getIcon = () => {
    if (isOverLimit || isNearLimit) return <AlertTriangleIcon className="h-4 w-4 text-yellow-500" />;
    return <InfoIcon className="h-4 w-4 text-blue-500" />;
  };

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <Card className="px-3 py-2 flex items-center gap-2 hover:bg-muted/50 transition-colors cursor-pointer">
            {getIcon()}
            <div className="flex flex-col">
              <div className="flex items-center gap-2">
                <span className="text-xs font-medium">
                  {currentTokens.toLocaleString()} / {maxTokens.toLocaleString()} tokens
                </span>
                {cost !== undefined && (
                  <span className="text-xs text-muted-foreground">
                    ${cost.toFixed(4)}
                  </span>
                )}
              </div>
              <Progress
                value={percentage}
                className={`h-1 w-24 ${getProgressColor()}`}
              />
            </div>
          </Card>
        </TooltipTrigger>
        <TooltipContent>
          <div className="space-y-1">
            <div className="text-sm font-medium">Token Usage</div>
            <div className="text-xs text-muted-foreground">
              {percentage.toFixed(1)}% of context window
            </div>
            {isNearLimit && (
              <div className="text-xs text-yellow-500">
                Warning: Approaching token limit
              </div>
            )}
            {isOverLimit && (
              <div className="text-xs text-red-500">
                Error: Token limit exceeded
              </div>
            )}
          </div>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}
