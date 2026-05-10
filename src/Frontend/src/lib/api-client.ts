import { z } from 'zod';

export interface AgentEvent {
  status: string;
  timestamp: string;
  data?: string;
}

type Listener = (events: AgentEvent[]) => void;

// In-memory список событий. Обновляется из RealtimeService.
const eventLog: AgentEvent[] = [];
const listeners: Set<Listener> = new Set();

export const agentApi = {
  subscribeToEvents(callback: Listener): () => void {
    listeners.add(callback);
    // Сразу отдаём накопленные события если есть
    if (eventLog.length > 0) {
      callback([...eventLog]);
    }
    return () => listeners.delete(callback);
  },

  pushEvent(event: AgentEvent): void {
    eventLog.unshift(event);      // новые события сверху
    if (eventLog.length > 200) eventLog.pop(); // ограничение буфера
    listeners.forEach(fn => fn([...eventLog]));
  },
};

export const freelancerProfileSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  skills: z.array(z.string()),
  rating: z.number().min(0).max(5),
  completedTasks: z.number().int().min(0),
});

export const orderAssistantRequestSchema = z.object({
  userId: z.string().uuid(),
  taskTitle: z.string().min(1),
  description: z.string().optional(),
  requiredSkills: z.array(z.string()),
  budgetMin: z.number().int().min(0),
  budgetMax: z.number().int().min(0),
  durationDays: z.number().int().min(1),
  candidateFreelancers: z.array(freelancerProfileSchema),
});

export const orderAssistantResultSchema = z.object({
  suggestedBudget: z.number().int(),
  suggestedDuration: z.number().int(),
  recommendedFreelancers: z.array(z.string()),
  confidence: z.number().min(0).max(1),
  reason: z.string(),
});

export const taskBriefSchema = z.object({
  taskId: z.string().uuid(),
  title: z.string(),
  category: z.string(),
  requiredSkills: z.array(z.string()),
  estimatedHours: z.number().int().min(0),
  description: z.string(),
});

export const userProfileSummarySchema = z.object({
  userId: z.string().uuid(),
  skills: z.array(z.string()),
  interests: z.array(z.string()),
  averageRating: z.number().min(0).max(5),
  completedTasks: z.number().int().min(0),
});

export const taskRecommendationRequestSchema = z.object({
  userProfile: userProfileSummarySchema,
  availableTasks: z.array(taskBriefSchema),
});

export const taskRecommendationResultSchema = z.object({
  taskId: z.string().uuid(),
  title: z.string(),
  matchScore: z.number().min(0).max(1),
  matchingSkills: z.array(z.string()),
  reason: z.string(),
});

export type FreelancerProfile = z.infer<typeof freelancerProfileSchema>;
export type OrderAssistantRequest = z.infer<typeof orderAssistantRequestSchema>;
export type OrderAssistantResult = z.infer<typeof orderAssistantResultSchema>;
export type TaskBrief = z.infer<typeof taskBriefSchema>;
export type UserProfileSummary = z.infer<typeof userProfileSummarySchema>;
export type TaskRecommendationRequest = z.infer<typeof taskRecommendationRequestSchema>;
export type TaskRecommendationResult = z.infer<typeof taskRecommendationResultSchema>;

export const generateCodeRequestSchema = z.object({
  prompt: z.string().min(1),
});

export const explainCodeRequestSchema = z.object({
  code: z.string().min(1),
});

export const embeddingsRequestSchema = z.object({
  text: z.string().min(1),
});

export const llmResponseSchema = z.object({
  generatedCode: z.string().optional(),
  explanation: z.string().optional(),
  embeddings: z.array(z.number()).optional(),
});

export type GenerateCodeRequest = z.infer<typeof generateCodeRequestSchema>;
export type ExplainCodeRequest = z.infer<typeof explainCodeRequestSchema>;
export type EmbeddingsRequest = z.infer<typeof embeddingsRequestSchema>;
export type LLMResponse = z.infer<typeof llmResponseSchema>;

export const metricDtoSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  type: z.string(),
  value: z.number(),
  timestamp: z.string().datetime(),
  labels: z.record(z.string()),
});

export const dashboardDtoSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  description: z.string(),
  ownerId: z.string().uuid(),
  widgets: z.array(z.object({
    id: z.string().uuid(),
    type: z.string(),
    config: z.string(),
  })),
  createdAt: z.string().datetime(),
});

export const chatDtoSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  type: z.string(),
  creatorId: z.string().uuid(),
  participants: z.array(z.object({
    userId: z.string().uuid(),
    role: z.string(),
  })),
  createdAt: z.string().datetime(),
});

export const messageDtoSchema = z.object({
  id: z.string().uuid(),
  chatId: z.string().uuid(),
  senderId: z.string().uuid(),
  content: z.string(),
  type: z.string(),
  timestamp: z.string().datetime(),
  attachments: z.array(z.object({
    fileName: z.string(),
    url: z.string(),
    size: z.number(),
  })),
});

export type MetricDto = z.infer<typeof metricDtoSchema>;
export type DashboardDto = z.infer<typeof dashboardDtoSchema>;
export type ChatDto = z.infer<typeof chatDtoSchema>;
export type MessageDto = z.infer<typeof messageDtoSchema>;

export const loginRequestSchema = z.object({
  email: z.string().email(),
  password: z.string().min(1),
});

export const registerRequestSchema = z.object({
  email: z.string().email(),
  username: z.string().min(3),
  password: z.string().min(8),
});

export const authResponseSchema = z.object({
  accessToken: z.string(),
  refreshToken: z.string(),
  expiresAt: z.string().datetime(),
});

export type LoginRequest = z.infer<typeof loginRequestSchema>;
export type RegisterRequest = z.infer<typeof registerRequestSchema>;
export type AuthResponse = z.infer<typeof authResponseSchema>;

class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string = '/api') {
    this.baseUrl = baseUrl;
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    const response = await fetch(url, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
    });

    if (!response.ok) {
      throw new Error(`API request failed: ${response.status} ${response.statusText}`);
    }

    return response.json();
  }

  async suggestOrder(request: OrderAssistantRequest): Promise<OrderAssistantResult> {
    return this.request('/ai/order-assistant/suggest', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async recommendTasks(request: TaskRecommendationRequest): Promise<TaskRecommendationResult[]> {
    return this.request('/ai/task-recommendations/recommend', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async checkOrderAssistantHealth(): Promise<string> {
    return this.request('/ai/order-assistant/health');
  }

  async checkTaskRecommendationsHealth(): Promise<string> {
    return this.request('/ai/task-recommendations/health');
  }

  async generateCode(request: GenerateCodeRequest): Promise<LLMResponse> {
    return this.request('/ai/llm/generate-code', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async explainCode(request: ExplainCodeRequest): Promise<LLMResponse> {
    return this.request('/ai/llm/explain-code', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getEmbeddings(request: EmbeddingsRequest): Promise<LLMResponse> {
    return this.request('/ai/llm/embeddings', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getMetrics(name?: string, from?: string, to?: string): Promise<MetricDto[]> {
    const params = new URLSearchParams();
    if (name) params.append('name', name);
    if (from) params.append('from', from);
    if (to) params.append('to', to);
    return this.request(`/analytics/metrics?${params}`);
  }

  async createMetric(request: CreateMetricRequest): Promise<MetricDto> {
    return this.request('/analytics/metrics', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getDashboards(ownerId: string): Promise<DashboardDto[]> {
    return this.request(`/analytics/dashboards?ownerId=${ownerId}`);
  }

  async createDashboard(request: CreateDashboardRequest): Promise<DashboardDto> {
    return this.request('/analytics/dashboards', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async login(request: LoginRequest): Promise<AuthResponse> {
    return this.request('/auth/login', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async register(request: RegisterRequest): Promise<AuthResponse> {
    return this.request('/auth/register', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async refreshToken(refreshToken: string): Promise<AuthResponse> {
    return this.request('/auth/refresh', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    });
  }

  async logout(refreshToken: string): Promise<{ message: string }> {
    return this.request('/auth/logout', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    });
  }

  async getUserChats(): Promise<ChatDto[]> {
    return this.request('/chat/chats');
  }

  async createChat(request: CreateChatRequest): Promise<ChatDto> {
    return this.request('/chat/chats', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getChatMessages(chatId: string, page?: number, pageSize?: number): Promise<MessageDto[]> {
    const params = new URLSearchParams();
    if (page) params.append('page', page.toString());
    if (pageSize) params.append('pageSize', pageSize.toString());
    return this.request(`/chat/chats/${chatId}/messages?${params}`);
  }
}

export const apiClient = new ApiClient();
