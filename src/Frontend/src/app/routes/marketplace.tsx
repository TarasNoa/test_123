import { createSignal } from 'solid-js';
import { apiClient } from '../../lib/api-client';

export default function Marketplace() {
  const [activeTab, setActiveTab] = createSignal('orders');
  const [description, setDescription] = createSignal('');
  const [budget, setBudget] = createSignal('');
  const [skills, setSkills] = createSignal('');
  const [orderSuggestion, setOrderSuggestion] = createSignal<any>(null);
  const [taskRecommendations, setTaskRecommendations] = createSignal<any[]>([]);
  const [loading, setLoading] = createSignal(false);

  const handleSuggestOrder = async () => {
    setLoading(true);
    try {
      const result = await apiClient.suggestOrder({
        userId: '00000000-0000-0000-0000-000000000001',
        taskTitle: description().slice(0, 50) || 'New Task',
        description: description(),
        requiredSkills: skills().split(',').map((s) => s.trim()).filter(Boolean),
        budgetMin: 0,
        budgetMax: parseFloat(budget()) || 1000,
        durationDays: 14,
        candidateFreelancers: [],
      } as any);
      setOrderSuggestion(result);
    } catch (error) {
      console.error('Failed to suggest order:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleRecommendTasks = async () => {
    setLoading(true);
    try {
      const result = await apiClient.recommendTasks({
        userProfile: {
          userId: '00000000-0000-0000-0000-000000000001',
          skills: skills().split(',').map((s) => s.trim()).filter(Boolean),
          completedTasks: 0,
          interests: [],
          averageRating: 5,
        },
        availableTasks: [],
      } as any);
      setTaskRecommendations(Array.isArray(result) ? result : []);
    } catch (error) {
      console.error('Failed to recommend tasks:', error);
    } finally {
      setLoading(false);
    }
  };

  const tabs = [
    { id: 'orders', label: 'Order Assistant' },
    { id: 'tasks', label: 'Task Recommendations' },
  ];

  return (
    <div class="flex h-screen bg-background text-foreground">
      {/* Sidebar */}
      <aside class="w-64 bg-surface border-r border-surface-3 flex flex-col">
        <div class="p-4 border-b border-surface-3">
          <h1 class="text-lg font-bold text-primary">Marketplace</h1>
          <p class="text-xs text-muted-foreground mt-1">AI-powered freelance</p>
        </div>
        <nav class="flex-1 p-2 space-y-1">
          {tabs.map((tab) => (
            <button
              onClick={() => setActiveTab(tab.id)}
              class={
                activeTab() === tab.id
                  ? 'w-full text-left px-3 py-2 rounded-md bg-surface-2 text-primary font-medium transition-colors'
                  : 'w-full text-left px-3 py-2 rounded-md text-muted-foreground hover:text-foreground hover:bg-surface-2/50 transition-colors'
              }
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </aside>

      {/* Main */}
      <main class="flex-1 p-6 overflow-auto">
        {activeTab() === 'orders' && (
          <div class="max-w-2xl mx-auto space-y-6">
            <div>
              <h2 class="text-xl font-bold mb-1">Order Assistant</h2>
              <p class="text-sm text-muted-foreground">Describe your project and get AI-powered recommendations</p>
            </div>

            <div class="p-6 bg-surface border border-surface-3 rounded-2xl space-y-4">
              <div>
                <label class="block text-sm font-medium text-muted-foreground mb-1">Project Description</label>
                <textarea
                  value={description()}
                  onInput={(e) => setDescription(e.currentTarget.value)}
                  placeholder="I need a React dashboard with real-time charts..."
                  rows={4}
                  class="w-full px-4 py-2.5 bg-surface-2 border border-surface-3 rounded-lg text-foreground placeholder:text-muted-foreground/50 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all resize-none"
                />
              </div>

              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="block text-sm font-medium text-muted-foreground mb-1">Budget ($)</label>
                  <input
                    type="number"
                    value={budget()}
                    onInput={(e) => setBudget(e.currentTarget.value)}
                    placeholder="5000"
                    class="w-full px-4 py-2.5 bg-surface-2 border border-surface-3 rounded-lg text-foreground placeholder:text-muted-foreground/50 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
                  />
                </div>
                <div>
                  <label class="block text-sm font-medium text-muted-foreground mb-1">Required Skills</label>
                  <input
                    type="text"
                    value={skills()}
                    onInput={(e) => setSkills(e.currentTarget.value)}
                    placeholder="React, TypeScript, D3.js"
                    class="w-full px-4 py-2.5 bg-surface-2 border border-surface-3 rounded-lg text-foreground placeholder:text-muted-foreground/50 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
                  />
                </div>
              </div>

              <button
                onClick={handleSuggestOrder}
                disabled={loading() || !description()}
                class="w-full py-3 bg-primary text-primary-foreground font-semibold rounded-lg hover:bg-primary/90 active:scale-[0.98] transition-all disabled:opacity-40 disabled:cursor-not-allowed"
              >
                {loading() ? 'Analyzing...' : 'Get Suggestion'}
              </button>
            </div>

            {orderSuggestion() && (
              <div class="p-6 bg-surface border border-surface-3 rounded-2xl">
                <h3 class="text-lg font-semibold text-primary mb-3">AI Suggestion</h3>
                <div class="space-y-3 text-sm">
                  <p><span class="text-muted-foreground">Suggested Price:</span> <span class="font-medium text-foreground">${orderSuggestion().suggestedPrice}</span></p>
                  <p><span class="text-muted-foreground">Timeline:</span> <span class="font-medium text-foreground">{orderSuggestion().timeline}</span></p>
                  <p><span class="text-muted-foreground">Confidence:</span> <span class="font-medium text-secondary">{(orderSuggestion().confidence * 100).toFixed(0)}%</span></p>
                  {orderSuggestion().recommendations && (
                    <div class="mt-4 p-4 bg-surface-2 rounded-lg">
                      <p class="text-muted-foreground mb-2">Recommendations:</p>
                      <ul class="list-disc list-inside space-y-1 text-muted-foreground">
                        {orderSuggestion().recommendations.map((r: string) => (
                          <li>{r}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>
        )}

        {activeTab() === 'tasks' && (
          <div class="max-w-2xl mx-auto space-y-6">
            <div>
              <h2 class="text-xl font-bold mb-1">Task Recommendations</h2>
              <p class="text-sm text-muted-foreground">Find matching tasks for your skills</p>
            </div>

            <div class="p-6 bg-surface border border-surface-3 rounded-2xl space-y-4">
              <div>
                <label class="block text-sm font-medium text-muted-foreground mb-1">What are you looking for?</label>
                <input
                  type="text"
                  value={description()}
                  onInput={(e) => setDescription(e.currentTarget.value)}
                  placeholder="React frontend development..."
                  class="w-full px-4 py-2.5 bg-surface-2 border border-surface-3 rounded-lg text-foreground placeholder:text-muted-foreground/50 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
                />
              </div>

              <button
                onClick={handleRecommendTasks}
                disabled={loading() || !description()}
                class="w-full py-3 bg-secondary text-secondary-foreground font-semibold rounded-lg hover:bg-secondary/90 active:scale-[0.98] transition-all disabled:opacity-40 disabled:cursor-not-allowed"
              >
                {loading() ? 'Searching...' : 'Find Tasks'}
              </button>
            </div>

            {taskRecommendations().length > 0 && (
              <div class="space-y-3">
                {taskRecommendations().map((task: any) => (
                  <div class="p-4 bg-surface border border-surface-3 rounded-xl hover:border-primary/30 transition-colors">
                    <div class="flex items-start justify-between">
                      <div>
                        <h3 class="font-semibold text-foreground">{task.title}</h3>
                        <p class="text-sm text-muted-foreground mt-1">{task.description}</p>
                        <div class="flex items-center gap-3 mt-3">
                          <span class="text-xs px-2 py-1 bg-primary/10 text-primary rounded-full">{task.category}</span>
                          <span class="text-xs text-muted-foreground">${task.budget}</span>
                          <span class="text-xs text-muted-foreground">{task.timeline}</span>
                        </div>
                      </div>
                      <span class="text-sm font-medium text-secondary">{task.matchScore}% match</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </main>
    </div>
  );
}