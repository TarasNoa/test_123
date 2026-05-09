import { Component } from "solid-js";
import { colors, spacing } from "../shared/ui/tokens";

interface OpportunityItem {
  id: string;
  title: string;
  description: string;
  type: "ai" | "automation" | "integration";
  reward: string;
  difficulty: "easy" | "medium" | "hard";
}

interface MarketplaceFeedProps {
  opportunities: OpportunityItem[];
  onOpportunityClick?: (id: string) => void;
}

/**
 * Marketplace Feed Widget
 * 
 * AI opportunity feed with:
 * - Opportunity cards
 * - Type badges (ai, automation, integration)
 * - Reward display
 * - Difficulty indicator
 */
export const MarketplaceFeed: Component<MarketplaceFeedProps> = (props) => {
  const getTypeColor = () => {
    return {
      ai: colors.turquoise,
      automation: colors.purple,
      integration: colors.info,
    };
  };

  const getDifficultyColor = () => {
    return {
      easy: colors.success,
      medium: colors.warning,
      hard: colors.error,
    };
  };

  return (
    <div class="space-y-3">
      {props.opportunities.map((opp) => (
        <div
          class="p-4 rounded-lg cursor-pointer transition-all hover:shadow-md"
          style={{
            "background-color": colors.surface2,
            border: `1px solid ${colors.border}`,
          }}
          onClick={() => props.onOpportunityClick?.(opp.id)}
        >
          <div class="flex items-start justify-between mb-2">
            <h3 class="text-sm font-semibold" style={{ color: colors.text }}>
              {opp.title}
            </h3>
            <span
              class="text-xs px-2 py-1 rounded"
              style={{
                "background-color": "rgba(53, 224, 208, 0.12)",
                color: getTypeColor()[opp.type],
              }}
            >
              {opp.type}
            </span>
          </div>
          
          <p class="text-sm mb-3" style={{ color: colors.textMuted }}>
            {opp.description}
          </p>
          
          <div class="flex items-center justify-between">
            <span class="text-xs font-medium" style={{ color: colors.turquoise }}>
              {opp.reward}
            </span>
            <span
              class="text-xs px-2 py-1 rounded"
              style={{
                "background-color": "rgba(245, 158, 11, 0.12)",
                color: getDifficultyColor()[opp.difficulty],
              }}
            >
              {opp.difficulty}
            </span>
          </div>
        </div>
      ))}
    </div>
  );
};
