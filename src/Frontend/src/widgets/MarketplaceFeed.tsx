import { For } from 'solid-js';
import { type OrderAssistantResult, type TaskRecommendationResult } from '../lib/api-client';

interface MarketplaceFeedProps {
  onSuggestOrder: (request: any) => void;
  onRecommendTasks: (request: any) => void;
  orderSuggestion: OrderAssistantResult | null;
  taskRecommendations: TaskRecommendationResult[];
  loading: boolean;
}

export function MarketplaceFeed(props: MarketplaceFeedProps) {
  return (
    <div class="marketplace-feed">
      <div class="order-assistant-section">
        <h2>Order Assistant</h2>
        <button onClick={() => props.onSuggestOrder({
          userId: 'user-123',
          taskTitle: 'Build a web app',
          description: 'Need a React app with backend',
          requiredSkills: ['React', 'Node.js'],
          budgetMin: 1000,
          budgetMax: 5000,
          durationDays: 30,
          candidateFreelancers: [
            { id: 'freelancer-1', name: 'Alice', skills: ['React', 'TypeScript'], rating: 4.5, completedTasks: 25 },
            { id: 'freelancer-2', name: 'Bob', skills: ['Node.js', 'Python'], rating: 4.0, completedTasks: 15 },
          ],
        })}>
          Suggest Order
        </button>
        {props.loading && <p>Loading...</p>}
        {props.orderSuggestion && (
          <div class="suggestion-result">
            <p>Budget: ${props.orderSuggestion.suggestedBudget}</p>
            <p>Duration: {props.orderSuggestion.suggestedDuration} days</p>
            <p>Confidence: {(props.orderSuggestion.confidence * 100).toFixed(1)}%</p>
            <p>Reason: {props.orderSuggestion.reason}</p>
            <ul>
              <For each={props.orderSuggestion.recommendedFreelancers}>
                {(freelancer) => <li>{freelancer}</li>}
              </For>
            </ul>
          </div>
        )}
      </div>

      <div class="task-recommendations-section">
        <h2>Task Recommendations</h2>
        <button onClick={() => props.onRecommendTasks({
          userProfile: {
            userId: 'user-123',
            skills: ['React', 'TypeScript'],
            interests: ['Web Development', 'UI/UX'],
            averageRating: 4.2,
            completedTasks: 10,
          },
          availableTasks: [
            { taskId: 'task-1', title: 'Build API', category: 'Backend', requiredSkills: ['Node.js'], estimatedHours: 40, description: 'REST API' },
            { taskId: 'task-2', title: 'Design UI', category: 'UI/UX', requiredSkills: ['Figma'], estimatedHours: 20, description: 'Mobile app design' },
          ],
        })}>
          Recommend Tasks
        </button>
        {props.loading && <p>Loading...</p>}
        <ul>
          <For each={props.taskRecommendations}>
            {(rec) => (
              <li>
                <strong>{rec.title}</strong> (Score: {(rec.matchScore * 100).toFixed(1)}%)
                <br />Reason: {rec.reason}
                <br />Matching Skills: {rec.matchingSkills.join(', ')}
              </li>
            )}
          </For>
        </ul>
      </div>
    </div>
  );
}