import { z } from 'zod';
import { config } from './config';

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

export const userDtoSchema = z.object({
  id: z.string().uuid(),
  email: z.string(),
  displayName: z.string(),
  roles: z.array(z.string()),
  emailConfirmed: z.boolean(),
  twoFactorEnabled: z.boolean(),
  createdAt: z.string().datetime(),
  role: z.string().nullable().optional(),
  phone: z.string().nullable().optional(),
  country: z.string().nullable().optional(),
  city: z.string().nullable().optional(),
  companyName: z.string().nullable().optional(),
  industry: z.string().nullable().optional(),
  companySize: z.string().nullable().optional(),
  website: z.string().nullable().optional(),
  skills: z.array(z.string()).nullable().optional(),
  experience: z.string().nullable().optional(),
  hourlyRate: z.number().nullable().optional(),
  specialization: z.string().nullable().optional(),
  linkedInUrl: z.string().nullable().optional(),
  cvUrl: z.string().nullable().optional(),
  avatarUrl: z.string().nullable().optional(),
  coverUrl: z.string().nullable().optional(),
  bio: z.string().nullable().optional(),
  rating: z.number().nullable().optional(),
  totalEarnings: z.number().nullable().optional(),
  totalSpent: z.number().nullable().optional(),
  completedTasks: z.number().int().nullable().optional(),
  isFreelancer: z.boolean().nullable().optional(),
  isClient: z.boolean().nullable().optional(),
});

export const userProjectDtoSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  description: z.string(),
  category: z.string(),
  status: z.string(),
  budget: z.number().nullable().optional(),
  currency: z.string(),
  progress: z.number().int(),
  teamSize: z.number().int(),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime(),
});

export const userPortfolioItemDtoSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  description: z.string(),
  itemType: z.string(),
  status: z.string(),
  tags: z.array(z.string()),
  skillsUsed: z.array(z.string()),
  client: z.string().nullable().optional(),
  projectUrl: z.string().nullable().optional(),
  githubUrl: z.string().nullable().optional(),
  liveUrl: z.string().nullable().optional(),
  completionDate: z.string().datetime().nullable().optional(),
  viewCount: z.number().int(),
  likeCount: z.number().int(),
  featured: z.boolean(),
  createdAt: z.string().datetime(),
});

export const userStatsDtoSchema = z.object({
  totalProjects: z.number().int(),
  completedProjects: z.number().int(),
  inProgressProjects: z.number().int(),
  totalTasks: z.number().int(),
  completedTasks: z.number().int(),
  totalEarnings: z.number(),
  totalSpent: z.number(),
  averageRating: z.number(),
  portfolioItemsCount: z.number().int(),
  reviewsCount: z.number().int(),
});

export const postDtoSchema = z.object({
  id: z.string().uuid(),
  authorId: z.string().uuid(),
  content: z.string(),
  title: z.string().nullable().optional(),
  tags: z.array(z.string()),
  mediaUrls: z.array(z.string()),
  createdAt: z.string().datetime(),
  updatedAt: z.string().datetime().nullable().optional(),
  likeCount: z.number().int(),
  commentCount: z.number().int(),
  viewCount: z.number().int(),
  isLikedByCurrentUser: z.boolean(),
});

export const postCommentDtoSchema = z.object({
  id: z.string().uuid(),
  userId: z.string().uuid(),
  content: z.string(),
  createdAt: z.string().datetime(),
});

export type MetricDto = z.infer<typeof metricDtoSchema>;
export type DashboardDto = z.infer<typeof dashboardDtoSchema>;
export type ChatDto = z.infer<typeof chatDtoSchema>;
export type MessageDto = z.infer<typeof messageDtoSchema>;
export type UserDto = z.infer<typeof userDtoSchema>;
export type UserProjectDto = z.infer<typeof userProjectDtoSchema>;
export type UserPortfolioItemDto = z.infer<typeof userPortfolioItemDtoSchema>;
export type UserStatsDto = z.infer<typeof userStatsDtoSchema>;
export type PostDto = z.infer<typeof postDtoSchema>;
export type PostCommentDto = z.infer<typeof postCommentDtoSchema>;

export const createMetricRequestSchema = z.object({
  name: z.string().min(1),
  type: z.string().min(1),
  value: z.number(),
  labels: z.record(z.string()).optional().default({}),
});

export const createDashboardRequestSchema = z.object({
  title: z.string().min(1),
  description: z.string().default(''),
  ownerId: z.string().uuid(),
});

export const createChatRequestSchema = z.object({
  name: z.string().min(1),
  type: z.string().default('Direct'),
  participantIds: z.array(z.string().uuid()),
});

export type CreateMetricRequest = z.infer<typeof createMetricRequestSchema>;
export type CreateDashboardRequest = z.infer<typeof createDashboardRequestSchema>;
export type CreateChatRequest = z.infer<typeof createChatRequestSchema>;

export const loginRequestSchema = z.object({
  email: z.string().email(),
  password: z.string().min(1),
});

export const registerRequestSchema = z.object({
  email: z.string().email(),
  displayName: z.string().min(2).max(64),
  password: z.string().min(8),
  role: z.enum(['client', 'company', 'freelancer']),
  phone: z.string().optional(),
  country: z.string().optional(),
  city: z.string().optional(),
  companyName: z.string().optional(),
  industry: z.string().optional(),
  companySize: z.string().optional(),
  website: z.string().optional(),
  skills: z.string().optional(),
  experience: z.string().optional(),
  hourlyRate: z.string().optional(),
  specialization: z.string().optional(),
  linkedInUrl: z.string().optional(),
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

  constructor(baseUrl: string = config.apiBaseUrl) {
    this.baseUrl = baseUrl;
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    const token = localStorage.getItem('accessToken');
    const response = await fetch(url, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...options.headers,
      },
    });

    if (!response.ok) {
      throw new Error(`API request failed: ${response.status} ${response.statusText}`);
    }

    return response.json();
  }

  async externalAuth(provider: string, data: { providerUserId: string; email: string; displayName?: string; avatarUrl?: string }): Promise<AuthResponse> {
    return this.request(`/api/v1/auth/external/${provider}`, {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async suggestOrder(request: OrderAssistantRequest): Promise<OrderAssistantResult> {
    return this.request('/api/v1/ai/order-assistant/suggest', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async recommendTasks(request: TaskRecommendationRequest): Promise<TaskRecommendationResult[]> {
    return this.request('/api/v1/ai/task-recommendations/recommend', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async checkOrderAssistantHealth(): Promise<string> {
    return this.request('/api/v1/ai/order-assistant/health');
  }

  async checkTaskRecommendationsHealth(): Promise<string> {
    return this.request('/api/v1/ai/task-recommendations/health');
  }

  async generateCode(request: GenerateCodeRequest): Promise<LLMResponse> {
    return this.request('/api/v1/ai/llm/generate-code', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async explainCode(request: ExplainCodeRequest): Promise<LLMResponse> {
    return this.request('/api/v1/ai/llm/explain-code', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getEmbeddings(request: EmbeddingsRequest): Promise<LLMResponse> {
    return this.request('/api/v1/ai/llm/embeddings', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getMetrics(name?: string, from?: string, to?: string): Promise<MetricDto[]> {
    const params = new URLSearchParams();
    if (name) params.append('name', name);
    if (from) params.append('from', from);
    if (to) params.append('to', to);
    return this.request(`/api/v1/analytics/metrics?${params}`);
  }

  async createMetric(request: CreateMetricRequest): Promise<MetricDto> {
    return this.request('/api/v1/analytics/metrics', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getDashboards(ownerId: string): Promise<DashboardDto[]> {
    return this.request(`/api/v1/analytics/dashboards?ownerId=${ownerId}`);
  }

  async createDashboard(request: CreateDashboardRequest): Promise<DashboardDto> {
    return this.request('/api/v1/analytics/dashboards', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async login(request: LoginRequest): Promise<AuthResponse> {
    return this.request('/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async register(request: RegisterRequest): Promise<AuthResponse> {
    return this.request('/api/v1/auth/register', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async uploadCv(file: File): Promise<{ cvUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    const url = `${this.baseUrl}/api/v1/auth/cv`;
    const token = localStorage.getItem('accessToken');
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: formData,
    });
    if (!response.ok) {
      throw new Error(`CV upload failed: ${response.status} ${response.statusText}`);
    }
    return response.json();
  }

  async refreshToken(refreshToken: string): Promise<AuthResponse> {
    return this.request('/api/v1/auth/refresh', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    });
  }

  async logout(refreshToken: string): Promise<{ message: string }> {
    return this.request('/api/v1/auth/logout', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    });
  }

  async getUserChats(): Promise<ChatDto[]> {
    return this.request('/api/v1/chat/chats');
  }

  async createChat(request: CreateChatRequest): Promise<ChatDto> {
    return this.request('/api/v1/chat/chats', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getChatMessages(chatId: string, page?: number, pageSize?: number): Promise<MessageDto[]> {
    const params = new URLSearchParams();
    if (page) params.append('page', page.toString());
    if (pageSize) params.append('pageSize', pageSize.toString());
    return this.request(`/api/v1/chat/chats/${chatId}/messages?${params}`);
  }

  /* ─── Dashboard ─── */
  async getMe(): Promise<UserDto> {
    return this.request('/api/v1/auth/me');
  }

  async getMyProjects(): Promise<UserProjectDto[]> {
    return this.request('/api/v1/tasks/my/projects');
  }

  async getMyPortfolio(): Promise<UserPortfolioItemDto[]> {
    return this.request('/api/v1/tasks/my/portfolio');
  }

  async getMyStats(): Promise<UserStatsDto> {
    return this.request('/api/v1/tasks/my/stats');
  }

  /* ─── Posts ─── */
  async getFeed(page?: number, pageSize?: number): Promise<PostDto[]> {
    const params = new URLSearchParams();
    if (page) params.append('page', page.toString());
    if (pageSize) params.append('pageSize', pageSize.toString());
    return this.request(`/api/v1/tasks/posts/feed?${params}`);
  }

  async getMyPosts(): Promise<PostDto[]> {
    return this.request('/api/v1/tasks/posts/my');
  }

  async createPost(content: string, title?: string, tags?: string[], mediaUrls?: string[]): Promise<PostDto> {
    return this.request('/api/v1/tasks/posts', {
      method: 'POST',
      body: JSON.stringify({ content, title, tags, mediaUrls }),
    });
  }

  async likePost(postId: string): Promise<void> {
    return this.request(`/api/v1/tasks/posts/${postId}/like`, { method: 'POST' });
  }

  async addComment(postId: string, content: string): Promise<PostCommentDto> {
    return this.request(`/api/v1/tasks/posts/${postId}/comment`, {
      method: 'POST',
      body: JSON.stringify({ content }),
    });
  }

  /* ─── Uploads ─── */
  async uploadAvatar(file: File): Promise<{ avatarUrl: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.request('/api/v1/auth/avatar', {
      method: 'POST',
      body: form,
    });
  }

  async uploadCover(file: File): Promise<{ coverUrl: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.request('/api/v1/auth/cover', {
      method: 'POST',
      body: form,
    });
  }
}

export const apiClient = new ApiClient();
