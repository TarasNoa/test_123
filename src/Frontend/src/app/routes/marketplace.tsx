import { createSignal, onMount } from 'solid-js';
import { apiClient, type OrderAssistantRequest, type TaskRecommendationRequest } from '../../lib/api-client';
import { MarketplaceFeed } from '../../widgets/MarketplaceFeed';

export default function Marketplace() {
  const [orderSuggestion, setOrderSuggestion] = createSignal(null);
  const [taskRecommendations, setTaskRecommendations] = createSignal([]);
  const [loading, setLoading] = createSignal(false);

  const handleSuggestOrder = async (request: OrderAssistantRequest) => {
    setLoading(true);
    try {
      const result = await apiClient.suggestOrder(request);
      setOrderSuggestion(result);
    } catch (error) {
      console.error('Failed to suggest order:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleRecommendTasks = async (request: TaskRecommendationRequest) => {
    setLoading(true);
    try {
      const result = await apiClient.recommendTasks(request);
      setTaskRecommendations(result);
    } catch (error) {
      console.error('Failed to recommend tasks:', error);
    } finally {
      setLoading(false);
    }
  };

  onMount(async () => {
    try {
      await apiClient.checkOrderAssistantHealth();
      await apiClient.checkTaskRecommendationsHealth();
    } catch (error) {
      console.error('Health check failed:', error);
    }
  });

  return (
    <div class="marketplace-page">
      <h1>AI-Powered Freelance Marketplace</h1>
      <MarketplaceFeed
        onSuggestOrder={handleSuggestOrder}
        onRecommendTasks={handleRecommendTasks}
        orderSuggestion={orderSuggestion()}
        taskRecommendations={taskRecommendations()}
        loading={loading()}
      />
    </div>
  );
}