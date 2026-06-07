import { createMemo, type Component } from 'solid-js';
import type { RunUsageSummary } from '../services/runSession';

export const RunUsageMeter: Component<{ usage: RunUsageSummary | null }> = (props) => {
  const formattedCost = createMemo(() => {
    const cost = props.usage?.costUsd ?? 0;
    return cost < 0.01 ? '$0.00' : `$${cost.toFixed(4)}`;
  });

  return (
    <div class="flex flex-wrap items-center gap-3 text-[10px] font-mono text-muted-foreground">
      <span title="Agent steps">
        steps {(props.usage?.stepCount ?? 0).toLocaleString()}
      </span>
      <span title="Tool calls">
        tools {(props.usage?.toolCallCount ?? 0).toLocaleString()}
      </span>
      <span title="Total tokens">
        tokens {(props.usage?.totalTokens ?? 0).toLocaleString()}
      </span>
      <span title="Estimated cost" class="text-secondary">
        {formattedCost()}
      </span>
    </div>
  );
};

export default RunUsageMeter;
